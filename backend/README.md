# Backend HRM Next — T05

Nền tảng API .NET 10, SQL Server 2022 và hợp đồng TypeScript dùng chung cho web/mobile.
Trạng thái T05 và bằng chứng: [hồ sơ nghiệm thu](../docs/verification/T05-foundation.md).

## Cấu trúc

| Project | Vai trò / phụ thuộc |
|---|---|
| `src/HRM.Domain` | Entity và bất biến; không phụ thuộc EF/ASP.NET |
| `src/HRM.Application` | Port truy vấn, repository, unit of work, phân trang; chỉ phụ thuộc Domain |
| `src/HRM.Infrastructure` | SQL Server, DbContext, migrations và implementation các port |
| `src/HRM.Api` | HTTP, DI composition, ProblemDetails, JSON, OpenAPI, health |

Assembly dùng tiền tố `HRM.Next` để phân biệt với backend WinForms legacy ở `db/src`.
`HRM.Next.slnx` chứa bốn project sản phẩm và integration suite; CI còn chạy gate legacy riêng.

## Chạy trên Windows

Từ thư mục gốc repository, cần SDK đúng `global.json` và SQL Server 2022 có quyền tạo database dev riêng.
Nếu chưa có SDK, dùng `tools/Install-Dotnet.ps1`; `tools/dotnet.cmd` ưu tiên SDK cục bộ.

```powershell
if (Test-Path .dotnet/dotnet.exe) {
  $env:DOTNET_ROOT=(Resolve-Path .dotnet).Path
  $env:PATH="$env:DOTNET_ROOT;$env:PATH"
}
tools/dotnet.cmd restore backend/HRM.Next.slnx --locked-mode
tools/dotnet.cmd build backend/HRM.Next.slnx -c Release --no-restore
tools/dotnet.cmd tool restore
$env:ConnectionStrings__Hrm='Server=.;Database=HRM_Next_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
tools/dotnet.cmd ef database update --project backend/src/HRM.Infrastructure --startup-project backend/src/HRM.Api
$env:ASPNETCORE_ENVIRONMENT='Development'
tools/dotnet.cmd run --project backend/src/HRM.Api --no-launch-profile -- --urls http://127.0.0.1:5080
```

`ef database update` chủ động tạo/nâng cấp **HRM_Next_Dev**, cần kiểm tra tên DB trước khi chạy.
Connection string chỉ nằm trong biến môi trường hoặc secret store; không ghi vào source.
API và design-time factory đều từ chối cấu hình thiếu server/database hoặc tên DB không bắt đầu `HRM_Next_`.
Không dùng lại `HRM_System`; không tự migrate/seed khi API khởi động. `TrustServerCertificate=True`
trong ví dụ dành cho SQL Server local; môi trường triển khai cần chứng chỉ tin cậy và cấu hình riêng.

| URL | Kết quả mong đợi |
|---|---|
| `http://127.0.0.1:5080/health/live` | 200 dù SQL chưa sẵn sàng |
| `http://127.0.0.1:5080/health/ready` | 200 khi kết nối được DB và đã áp dụng đủ migrations; 503 nếu chưa sẵn sàng |
| `http://127.0.0.1:5080/api/v1/system/info` | version, giờ UTC từ TimeProvider, múi giờ nghiệp vụ |
| `http://127.0.0.1:5080/openapi/v1.json` | OpenAPI trong Development; Production mặc định không public schema |
| `http://127.0.0.1:5080/api/v1/departments` | 401 trước T06; không nhận token/header tự chế |

T05 chưa có login/reset/session hay màn hình nghiệp vụ. Read route phòng ban được khóa mặc định,
chờ Identity ở T06 và scope quyền ở T07. Test identity và các route `/_tests` chỉ ở assembly test,
không được tham chiếu bởi API sản phẩm. Không có UI cần nghiệm thu trong task này.

## Kiểm chứng đầy đủ

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t05.py --dotnet .dotnet/dotnet.exe
```

Cần Node 24/npm, Python 3.12+ và quyền CREATE DATABASE trên SQL Server test local.
Fixture tạo một DB `HRM_Next_Test_T05_<GUID>`, migrate, chạy SQL thật rồi chỉ xóa DB do chính nó tạo.
Không có InMemory fallback hoặc skip khi thiếu SQL. Log, TRX, SQL ledger, audit và hash đầu vào
được lưu trong thư mục mới dưới `docs/verification/runs/T05-*`; gate thất bại nếu input đổi giữa lượt chạy.
Lệnh đầy đủ còn kiểm tra cả legacy và các lỗi baseline đã định danh, không thay đổi DB legacy.

CI chia Windows legacy và Linux backend/SQL thành hai job, job backend phụ thuộc Windows đã đạt.
`--ci-backend-only` chỉ ghi `PASS_BACKEND_ONLY`; không thay thế bằng chứng full gate local.
CI spin up SQL Server 2022 bằng Docker với mật khẩu ngẫu nhiên, ghi image ID và SQL version,
không sử dụng dịch vụ hay dữ liệu sản xuất.

## Migration / phục hồi

Migration đầu `20261111094103_InitialFoundation` chỉ tạo schema `personnel`, bảng `Departments`
và lịch sử EF trong DB riêng. Không import hay nâng cấp bảng legacy; chuyển dữ liệu thuộc T23.
T05 kiểm tra DB trống và downgrade/upgrade trên DB test, không phải diễn tập migrate dữ liệu doanh nghiệp.
Trước thay migration ở môi trường có dữ liệu, sao lưu và review SQL script; ưu tiên migration forward
sửa lỗi để giữ dữ liệu. `ef database update 0` chỉ dùng cho DB thử có thể bỏ: Down xóa bảng Departments.
Không chạy Down trên DB có dữ liệu cần giữ. Readiness vẫn 503 cho đến khi migration hoàn tất.

Quy ước transaction, tiền/ngày và giới hạn nền tảng: [ADR-0008](../docs/adr/0008-backend-foundation-contracts.md).

## T06 — Identity và phiên

T06 thay handler khóa mặc định bằng Identity thật; tài khoản đăng nhập được vẫn cần permission để gọi
business API (T07), vì vậy departments trả401 khi chưa login và403 cho account chưa có permission.
Các mô tả “chưa có login” ở phần T05 bên trên là snapshot trước T06.

Áp dụng migration mới bằng lệnh EF ở trên lên **DB dev riêng**. `IdentitySessions` tạo bảng Identity và
AccountSessions, không nâng cấp/import account hay token legacy. Down về InitialFoundation giữ Departments
nhưng **xóa toàn bộ tài khoản/phiên HRM Next**; chỉ dùng trên DB test bỏ được. Môi trường có dữ liệu cần backup
và migration forward sửa lỗi; không tự chạy Down. Test đã kiểm upgrade/down với dữ liệu phòng ban tồn tại.

Auth cần HTTPS. Khi chạy dev, kiểm tra/tạo chứng chỉ ASP.NET Core dev theo SDK, rồi chạy:

```powershell
tools/dotnet.cmd dev-certs https --check
# Nếu chưa có chứng chỉ tin cậy, chủ máy chạy: tools/dotnet.cmd dev-certs https --trust
tools/dotnet.cmd run --project backend/src/HRM.Api --no-launch-profile -- --urls https://localhost:5081
```

Không có public đăng ký hoặc password mặc định. Tài khoản được workflow tin cậy T08 gọi
`AccountService.InviteAsync`, tự đặt mật khẩu qua lời mời 24h; quyền/admin bootstrap thuộc T07.
T06 chưa có SMTP: forgot-password trả202 với `deliveryMode=not-configured`, nói rõ không gửi email.
Sender `test-only` của integration suite không phải mailbox/SMTP sản phẩm.

| Route dưới `/api/v1/auth` | Chức năng |
|---|---|
| GET csrf | Request token chống CSRF; lấy lại sau login |
| POST web/login | Cookie Secure/HttpOnly/Strict, bắt buộc X-XSRF-TOKEN |
| POST mobile/login, mobile/refresh | Opaque bearer 15 phút, refresh quay vòng đến hạn tuyệt đối 7 ngày |
| GET me | ID và username của phiên hiện tại |
| POST forgot-password, reset-password, activate | Phản hồi không enumeration; reset 15 phút/activation purpose riêng |
| POST change-password | Yêu cầu mật khẩu cũ; thu hồi mọi phiên khi đổi thành công |
| POST logout, logout-all | Thu hồi phiên hiện tại / mọi phiên |

Unsafe request có cookie phải có `X-XSRF-TOKEN`; bearer-only không dùng cookie thì không cần CSRF.
Lockout5 lần sai/15 phút, rate20 auth request/IP/phút. Không log credentials hoặc reset code.
`DataProtection__KeyDirectory` tùy chọn cho keyring riêng; staging cần key protection/ACL/shared keys phù hợp.
Xem [ADR-0009](../docs/adr/0009-identity-sessions-reset.md) và [bằng chứng T06](../docs/verification/T06-identity.md).

Chạy full T06 gate bằng cùng env SQL test, chỉ đổi entrypoint:

```powershell
python -B scripts/verify_t06.py --dotnet .dotnet/dotnet.exe
```

Gate xác nhận đủ ma trận auth, SQL thật, contract/TS/client, audit và legacy; CI Linux chạy entrypoint
này với `--ci-backend-only`, phụ thuộc Windows legacy PASS. Không sử dụng auth giả cho các ca T06.
