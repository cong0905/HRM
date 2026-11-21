# HRM Next - Giao diện Web SPA (React + TypeScript + Vite)

Dự án Frontend Web SPA quản lý nhân sự và tiền lương thuộc kiến trúc HRM Next (nhiệm vụ T16).

---

## 1. Công nghệ Sử dụng & Phiên bản Khóa Chính xác

- **Khung ứng dụng:** React 18 (`18.3.1`) + TypeScript (`5.7.3`) + Vite (`6.4.6`).
- **Thư viện Giao diện (UI):** Ant Design (`5.24.2`) + `@ant-design/icons` (`5.6.1`).
- **Quản lý Truy vấn & Bộ nhớ đệm (Server State):** TanStack Query (`5.66.0`).
- **Điều hướng (Routing):** React Router DOM (`7.18.5`).
- **Kiểm tra Quy chuẩn Mã nguồn (Linter):** ESLint (`10.14.0`) với Flat Config (`@eslint/js: 10.0.1`, `typescript-eslint: 8.72.2`).
- **API Client:** Shared contracts từ `contracts/src/client.ts` (`openapi-fetch: 0.17.0`) hỗ trợ typed DTO, CSRF header và ProblemDetails chuẩn schema.
- **Kiểm thử Đơn vị & Tích hợp:** Vitest (`5.1.0`) + React Testing Library (`16.2.0`) + JSDOM (`26.0.0`).
- **Kiểm thử E2E (End-to-End):** Playwright (`1.65.0`).
- **Môi trường chạy:** Node.js `>=24`.

### Rationale cho Overrides (`package.json`)
- **`minimatch: 11.0.8`:** Xử lý lỗ hổng bảo mật `GHSA-g3hv-75wg-m249` (ảnh hưởng minimatch `>=7, <11.0.1` trong nhánh phụ thuộc của `@typescript-eslint/typescript-estree`). Phiên bản 11.0.8 tương thích hoàn toàn với Node.js `>=24` và đã được kiểm chứng hoạt động ổn định với parser của TypeScript-ESLint.

---

## 2. Thiết lập Môi trường và Bảo mật (HTTPS, Cookie, CSRF)

Backend HRM Next sử dụng Cookie phiên làm việc `__Host-Hrm.Session` có gắn các cờ bảo mật:
- `Secure: true` (chỉ truyền tải qua kết nối HTTPS an toàn).
- `HttpOnly: true` (chống đánh cắp cookie bằng mã độc XSS phía client).
- `SameSite: Strict` (ngăn chặn tấn công CSRF chéo trang).

### Cấu hình Proxy và Biến Môi trường

Vite dev server proxy toàn bộ yêu cầu `/api` sang máy chủ Backend API:

| Biến Môi trường | Ý nghĩa | Mặc định |
| :--- | :--- | :--- |
| `HRM_API_ORIGIN` | Địa chỉ gốc của Backend API ASP.NET Core | `https://localhost:5001` |
| `HRM_WEB_CERT` | Đường dẫn tệp chứng chỉ HTTPS cho Vite dev server (tuỳ chọn) | _(trống)_ |
| `HRM_WEB_KEY` | Đường dẫn tệp khóa riêng tư HTTPS cho Vite dev server (tuỳ chọn) | _(trống)_ |

> **Lưu ý bảo mật:** Tuyệt đối không commit chứng chỉ hoặc tệp khóa bí mật (`*.crt`, `*.key`, `*.pfx`) vào kho lưu trữ git.

---

## 3. Cài đặt và Khởi chạy

### Cài đặt gói phụ thuộc (Dependencies)
Codex/Người phát triển thực hiện cài đặt:
```bash
cd web
npm install
```

### Chạy môi trường Phát triển (Dev Server)
```bash
npm run dev
```
Truy cập tại: `http://localhost:3000` (hoặc `https://localhost:3000` nếu đã cấu hình `HRM_WEB_CERT`/`HRM_WEB_KEY`).

### Kiểm tra Kiểu dữ liệu & Biên dịch (Typecheck & Build)
Lệnh kiểm tra kiểu dữ liệu biên dịch cả cấu hình ứng dụng (`tsconfig.json`) và cấu hình công cụ Node (`tsconfig.node.json`):
```bash
npm run typecheck
npm run lint
npm run build
```

---

## 4. Kiểm thử Tự động (Testing)

### 1. Component & Unit Tests (Vitest)
Các ca kiểm thử độc lập kiểm tra vòng đời phiên làm việc, CSRF token refresh, điều hướng theo quyền, xử lý xung đột 409, lỗi mạng và biểu mẫu tiếng Việt:
```bash
npm test
```
*Ghi chú kỹ thuật:* `test/setup.ts` sử dụng `transferableAbortController` từ `node:util` để pair realm giữa JSDOM và Node/undici native `Request`, đảm bảo `new Request(url, { signal })` hoạt động nhất quán mà không phá vỡ AbortSignal trong production.

### 2. End-to-End Acceptance Tests (Playwright)
Kiểm thử chấp nhận E2E với hệ thống Backend và cơ sở dữ liệu thật (không dùng route.fulfill giả lập):
```bash
# Cung cấp thông tin tài khoản kiểm thử từ môi trường
export E2E_BASE_URL="https://localhost:3000"
export E2E_ADMIN_USERNAME="admin"
export E2E_ADMIN_PASSWORD="Password@123456"

# Tuỳ chọn tài khoản nhân viên kiểm tra phân quyền:
export E2E_EMPLOYEE_USERNAME="employee_alice"
export E2E_EMPLOYEE_PASSWORD="Password@123456"

npm run test:e2e
```
*Ghi chú: Nếu biến môi trường tài khoản chưa được thiết lập, các test spec E2E sẽ tự động đánh dấu là `NOT_RUN` (Skip) để tránh gây sai lệch kết quả kiểm thử.*
# T17: nhân sự và lịch

Các trang `/personnel`, `/my-profile`, `/organization`, `/calendar`, `/admin`, `/audit` gọi API thật qua generated client và lớp CSRF hiện có. Sau khi HR tạo nhân viên, SystemAdmin dùng mã hồ sơ trong kết quả tạo để **Cấp phạm vi** cho HR tại trang quản trị; hồ sơ chưa được cấp sẽ không xuất hiện trong danh sách HR.

Hai migration T17 thêm danh mục chức vụ và FK chống xóa danh mục đang dùng. Áp dụng migration theo quy trình môi trường riêng trước khi dùng UI mới; không trỏ migration vào database legacy.

Kiểm thử browser T17 dùng harness riêng, tài khoản giả và database GUID tự dọn. Từ repository root:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\.dotnet\dotnet.exe run --project backend/tests/HRM.T17BrowserReview -c Release
```

Harness ghi `docs/verification/t17/browser-host.json`. File này chỉ chứa đường dẫn đến fixture tạm; không sao chép mật khẩu vào báo cáo. Trong terminal khác, đọc `fixtureFile` để cấu hình `HRM_API_ORIGIN`, `HRM_WEB_CERT`, `HRM_WEB_KEY`, chạy Vite HTTPS theo hướng dẫn bên dưới. Đặt `HRM_T17_FIXTURE_FILE` tới file fixture tạm rồi chạy từ `web`:

```powershell
npx playwright test --config playwright.t17.config.ts
```

Config chuyên T17 báo NOT_RUN/lỗi khi chưa có fixture; suite không dùng route.fulfill hoặc backend giả. Dùng Edge đã cài với profile thử riêng. Tạo file `stopFile` được harness công bố để dừng, xóa database sở hữu và chứng chỉ/tài khoản tạm; harness cũng có timeout 25 phút. Chỉ thao tác stopFile thuộc thư mục tạm `HRM-T17-browser-*` do lượt chạy tạo.

# T18: công, phép và hàng đợi duyệt

`/attendance` có công của tôi và công trong phạm vi HR/Payroll theo permission thật. `/leave` có quỹ, gửi/hủy đơn, lịch ngày nghỉ và hàng đợi duyệt/từ chối. Các lệnh gửi Idempotency-Key, giữ khóa trong bộ nhớ khi thử lại cùng nội dung sau phản hồi không chắc chắn. UI dùng rowVersion và kiểm kỳ đóng; API vẫn là nơi quyết định quyền/quỹ/tính nhất quán.

Browser T18 dùng fixture sạch cho mỗi lượt đầy đủ vì các kịch bản có trừ quỹ và chấm công. Từ repository root:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\.dotnet\dotnet.exe run --project backend/tests/HRM.T18BrowserReview -c Release
```

Đọc `docs/verification/t18/browser-host.json`, dùng `fixtureFile` để lấy cấu hình Vite HTTPS như phần T17. Đặt `HRM_T18_FIXTURE_FILE` tới file tạm này rồi chạy từ `web`:

```powershell
npx playwright test --config playwright.t18.config.ts
```

Harness chỉ dùng database GUID và actor giả, quỹ Worker 3 ngày để kiểm ca thiếu quỹ (không đổi chính sách demo sản phẩm), clock cố định và fault audit riêng cho phép thử rollback. Cookie từ đăng nhập thật được dùng lại trong bộ nhớ suite, không tắt giới hạn auth 20 request/phút. Tạo stopFile đã công bố để xuất `browser-sql-state.json` và tự dọn database/chứng chỉ/credential tạm; timeout 25 phút cũng dọn tự động. Không ghi credential vào báo cáo hoặc dùng harness với dữ liệu sản phẩm.



### Kiểm chứng T19: lương và dashboard

Từ thư mục repository, build `backend/tests/HRM.T19BrowserReview` bằng SDK `.dotnet/dotnet.exe`, rồi chạy `python -B scripts/verify_t19_browser.py`. Cần SQL Server local, Windows Authentication, Edge và cổng 3001 trống. Runner tạo fixture SQL/API/HTTPS riêng, chạy sáu ca Playwright qua tài khoản thật rồi dọn tài nguyên do nó tạo; không sử dụng demo cổng 3000.

Báo cáo và giới hạn: `docs/verification/T19-web-payroll-review.md`; kết quả: `docs/verification/t19/result.json`. T19 đã VERIFIED về kỹ thuật theo ủy quyền ngày 15/11/2026; biên bản tại `docs/decisions/T19-delegated-technical-acceptance.md`. Human UAT vẫn chưa thực hiện.
