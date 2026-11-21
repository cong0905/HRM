# Hợp đồng API và client TypeScript

Nguồn hợp đồng là OpenAPI được export từ Kestrel chạy **API sản phẩm**, không từ test probe.
`openapi.v1.json` và `src/generated/api.ts` được commit cùng source API; không sửa tay file generated.
Client `src/client.ts` dùng `openapi-fetch`, có thể truyền `fetch` của web hoặc React Native.

```typescript
import { createHrmClient } from './src/client.js';

const api = createHrmClient('http://127.0.0.1:5080');
const { data, error, response } = await api.GET('/api/v1/departments', {
  params: { query: { page: 1, pageSize: 20, sort: 'name' } },
});
// Trước T06: response.status === 401; error có errorCode và traceId.
```

`credentials: include` có sẵn cho web; cách cấp credentials phiên thật được quyết định ở T06.
Không có token mẫu hay cơ chế bỏ qua xác thực trong client.

## Sinh lại và phát hiện drift

Từ root, build API Release trước; cần Node 24/npm và SDK đúng `global.json`:

```powershell
npm --prefix contracts ci --ignore-scripts
tools/dotnet.cmd build backend/HRM.Next.slnx -c Release
python -B scripts/contracts.py update --dotnet .dotnet/dotnet.exe --evidence docs/verification/t05/manual-update
python -B scripts/contracts.py check --dotnet .dotnet/dotnet.exe --evidence docs/verification/t05/manual-check
```

`update` export schema thật, chuẩn hóa JSON, sinh TypeScript rồi kiểm tra compile/client.
`check` so sánh schema runtime với schema đã lưu, so generated client, compile type tests,
chạy hai test client và gọi API qua HTTP Kestrel thật. API export dùng cấu hình SQL giả tường minh
và chỉ gọi các route không cần DB; đây không phải bằng chứng integration SQL.
Gate T05 chạy SQL thật riêng, sau đó thực hiện `check`. Các test âm chứng minh đổi type hoặc thêm route
khi chưa cập nhật contract sẽ bị từ chối; type tests bắt sai route, sai query type và số tiền kiểu number.

## Quy ước

| Dữ liệu | JSON / TypeScript | Quy tắc |
|---|---|---|
| page, pageSize | number nguyên | mặc định 1/20; size 1–100; offset có giới hạn int |
| sort | string | whitelist `name`, `-name`, `createdAt`, `-createdAt` |
| DecimalString | string | thập phân invariant, không exponent; server từ chối mất chính xác hoặc vượt decimal .NET |
| LocalDate | string | `YYYY-MM-DD`; không chuyển thành Date theo timezone |
| UtcTimestamp | string | ISO8601 có offset khi nhận; trả UTC đuôi `Z` |
| Lỗi HTTP | ProblemDetails | `errorCode`, `traceId`; không trả SQL/stack |

Không đổi DecimalString sang JavaScript Number để tính tiền. Schema primitive chung phục vụ các task
nghiệp vụ sau; T05 chưa có API lương hoặc công thức làm tròn tiền. Format OpenAPI không thay thế runtime
validation: một giá trị string vẫn có thể bị server từ chối. Kiểm chính xác/nghiệp vụ thực hiện phía server.

## Auth T06

Schema đã có các route `/api/v1/auth`; client sinh lại từ runtime, không viết tay kiểu login/token.
Auth chỉ chấp nhận HTTPS. Web lấy csrf, gửi `X-XSRF-TOKEN` cho login/unsafe request và lấy lại token sau login:

```typescript
const api = createHrmClient('https://localhost:5081');
const csrf = await api.GET('/api/v1/auth/csrf');
if (!csrf.data) throw new Error('Không lấy được CSRF token');
await api.POST('/api/v1/auth/web/login', {
  headers: { 'X-XSRF-TOKEN': csrf.data.requestToken },
  body: { username, password }, // giá trị người dùng nhập; không có mật khẩu mặc định
});
// GET csrf lại sau login rồi gửi token mới cho request thay đổi trạng thái.
```

Mobile dùng POST mobile/login và mobile/refresh; gửi `Authorization: Bearer <accessToken>` cho API.
Token opaque chuẩn framework, không decode như JWT, lưu Secure Storage ở T20. Refresh chỉ một request
tại một thời điểm; refresh cũ trả401 sau rotation. Logout/change/reset/disable có thể làm token còn hạn trả401.
Không tự retry vô hạn khi401; reset password không trả token trong response. Trước T07 user đăng nhập vẫn
không tự có permission đọc phòng ban. Các đoạn “trước T06” ở phần đầu là mô tả trạng thái tại T05.
