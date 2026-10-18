# HRM · Human Resource Management

Ứng dụng quản lý nhân sự trên **Windows**, xây dựng bằng **C# / .NET 8 WinForms**, **Entity Framework Core** và **SQL Server**. Dự án tổ chức theo nhiều tầng để tách giao diện, nghiệp vụ và truy cập dữ liệu.

[Bắt đầu](#chạy-trên-windows) · [Kiến trúc](#kiến-trúc) · [Tài liệu](#tài-liệu)

## Phạm vi chức năng

- Đăng nhập, quản lý tài khoản và xử lý yêu cầu đặt lại mật khẩu.
- Quản lý nhân viên, phòng ban và thông tin nhân sự.
- Chấm công, đơn nghỉ phép, bảng lương và hiệu suất.
- Quản lý tin tuyển dụng, ứng viên và lịch phỏng vấn.
- Dashboard tổng hợp; chatbot hỗ trợ qua Gemini khi có cấu hình API.

Đây là ứng dụng desktop: cần Windows và SQL Server để chạy. Các chức năng email và AI cần dịch vụ bên ngoài được cấu hình riêng.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Giao diện | .NET 8 WinForms |
| Dữ liệu | EF Core 8, SQL Server, migrations |
| Tổ chức | N-Tier, Repository, Service, DTO, Dependency Injection |
| Mật khẩu | BCrypt.Net-Next |
| Tích hợp | Gemini API, SMTP tùy chọn |

## Kiến trúc

```text
HRM.GUI       Giao diện WinForms, khởi tạo Dependency Injection
HRM.BLL       Service và quy tắc nghiệp vụ
HRM.DAL       DbContext, repository, migrations
HRM.Domain    Entity nghiệp vụ
HRM.Common    DTO, helper và tiện ích dùng chung
```

Luồng nghiệp vụ chính đi từ form → service → repository → SQL Server. Mã nguồn solution nằm trong `db/src/`.

## Chạy trên Windows

Chuẩn bị **.NET 8 SDK**, **SQL Server / SQL Server LocalDB**; nếu dùng Visual Studio 2022, cài workload **.NET desktop development**.

```powershell
git clone https://github.com/cong0905/HRM.git
cd HRM
Copy-Item db/src/HRM.GUI/appsettings.json.example db/src/HRM.GUI/appsettings.json
dotnet restore db/src/HRM.sln
```

Sửa `ConnectionStrings:DefaultConnection` trong file vừa tạo để trỏ đến SQL Server đang chạy. Ví dụ với LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HRM_System;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

```powershell
dotnet run --project db/src/HRM.GUI/HRM.GUI.csproj
```

Hoặc mở `db/src/HRM.sln`, chọn `HRM.GUI` làm Startup Project và nhấn **F5**. Khi khởi động, `Program.cs` gọi `Database.Migrate()`; tài khoản SQL cần quyền tạo/cập nhật database. Nếu kết nối thất bại, ứng dụng hiển thị thông báo và dừng.

## Cấu hình tùy chọn

| Khóa | Mục đích |
| --- | --- |
| `Gemini:ApiKey`, `Gemini:BaseUrl` | Gọi dịch vụ AI; xem file cấu hình mẫu |
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Pass`, `Smtp:EnableSsl` | Gửi email đặt lại mật khẩu |

Khi chưa có SMTP, chương trình đăng ký `NullEmailSender`; không kỳ vọng nhận được email thật. Chỉ lưu khóa và mật khẩu thật trong cấu hình cục bộ.

## Kiểm tra bản build

```powershell
dotnet build db/src/HRM.sln
```

Sau đó kiểm tra trên giao diện: đăng nhập → nhân viên/phòng ban → chấm công/nghỉ phép → bảng lương. Lệnh build không thay thế kiểm thử các luồng nghiệp vụ hoặc tích hợp thực tế.

## Tài liệu

- [Tài liệu chi tiết](db/README.md)
- [Hướng dẫn chạy dự án](db/HuongDanChayDuAn.md)
- [Điểm khởi động và cấu hình dịch vụ](db/src/HRM.GUI/Program.cs)

**Tác giả:** [Nguyễn Đức Công](https://github.com/cong0905)
