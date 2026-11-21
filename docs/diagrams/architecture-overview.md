# Sơ đồ Kiến trúc Tổng thể Hệ thống HRM (Architecture Overview)

Tài liệu này mô tả kiến trúc tổng thể của hệ thống **HRM (Human Resource Management)** theo mô hình **Modular Monolith** kết hợp **Clean Architecture (Hexagonal Architecture)**. Hệ thống phân tách rõ ràng giữa các tầng trách nhiệm, bảo đảm tính bất biến của dữ liệu tài chính, an ninh phòng vệ chiều sâu (Defense-in-Depth) và khả năng kiểm thử độc lập.

---

## 1. Sơ đồ Kiến trúc Khối (High-Level Block Diagram)

```mermaid
flowchart TD
    subgraph Clients["Tầng Ứng dụng Khách (Presentation Clients)"]
        WebSPA["Web Admin SPA\n(React 19 / TypeScript / Vite)\nCookie HttpOnly + Anti-CSRF"]
        MobileApp["Mobile App Nhân viên\n(React Native / Expo 54)\nBearer Token + SecureStore"]
    end

    subgraph Ingress["Tầng Biên giới Mạng (Network & Gateway)"]
        ReverseProxy["Reverse Proxy (Nginx Staging / Production)\nTLS 1.3 Termination (HTTPS 443)\nSame-Origin Routing & Security Headers"]
    end

    subgraph BackendCore["Tầng Xử lý Trung tâm (Backend Modular Monolith - ASP.NET Core / .NET 10)"]
        subgraph ApiLayer["HRM.Api (Tầng Tiếp nhận & Phân phối)"]
            Controllers["Controllers / Endpoints\n(/api/v1/auth, /employees, /leave, /payroll)"]
            Middlewares["Middlewares Chống xâm nhập\n(SecurityStamp Revocation, RateLimiting, ExceptionFilter)"]
            BackgroundWorkers["Background Workers\n(EmailOutboxWorker, AttendanceSweep)"]
        end

        subgraph AppLayer["HRM.Application (Tầng Ứng dụng & Luồng Nghiệp vụ)"]
            UseCases["Commands & Queries (CQRS)\n(SubmitLeave, ApproveLeave, CalculatePayrollBatch)"]
            Validators["FluentValidation\n(OverdraftCheck, DateRangeValidation, PII Guard)"]
            Interfaces["Domain Interfaces / Ports\n(IUnitOfWork, IEmailSender, ICurrentUser)"]
        end

        subgraph DomainLayer["HRM.Domain (Tầng Miền Nghiệp vụ Cốt lõi - Pure C#)"]
            Entities["Domain Entities & Aggregates\n(EmployeeProfile, LeaveBalance, PayrollSnapshot)"]
            Invariants["Bất biến Nghiệp vụ (Business Invariants)\n(Balance >= 0, LockedSnapshot = ReadOnly)"]
            DomainEvents["Domain Events\n(LeaveApprovedEvent, PayrollPublishedEvent)"]
        end

        subgraph InfraLayer["HRM.Infrastructure (Tầng Hạ tầng & Tích hợp Kỹ thuật)"]
            EfDbContext["Entity Framework Core (HrmDbContext)\nQueryTrackingBehavior.NoTracking mặc định\nRowVersion Concurrency Token"]
            EmailSender["Email Sender (DevSink / SmtpClient)\nExponential Backoff & Token Redaction"]
            FileStorage["Local File Store / Evidence Vault\n(Salary Export CSV with Formula Escaping)"]
        end
    end

    subgraph DataStorage["Tầng Lưu trữ & Dịch vụ Ngoài (Data & External Services)"]
        SqlServer["Cơ sở Dữ liệu Quan hệ (SQL Server 2022)\nRow-level Locks (UPDLOCK, ROWLOCK)\nCheck Constraints & Unique Indexes"]
        SmtpService["Dịch vụ Thư điện tử (SMTP / MailHog)\nTransactional Email Dispatch"]
    end

    %% Client requests
    WebSPA -->|"HTTPS / Cookie (Same-Origin)"| ReverseProxy
    MobileApp -->|"HTTPS / Bearer Auth"| ReverseProxy

    %% Gateway to API
    ReverseProxy -->|"HTTP (Internal Docker Network)"| Middlewares
    Middlewares --> Controllers
    Controllers --> UseCases

    %% Clean Architecture Dependencies
    UseCases --> Entities
    UseCases --> Interfaces
    InfraLayer -.->|"Implements Ports"| Interfaces
    BackgroundWorkers -->|"Pulls Outbox Messages"| EfDbContext

    %% Data access
    EfDbContext -->|"TCP / TDS Protocol"| SqlServer
    EmailSender -->|"SMTP (Port 1025/587)"| SmtpService
```

---

## 2. Mô tả Trách nhiệm 4 Tầng Clean Architecture

Hệ thống tuân thủ nghiêm ngặt nguyên lý **Dependency Inversion Principle (DIP)**: Tầng bên trong không bao giờ phụ thuộc trực tiếp vào tầng bên ngoài.

```mermaid
flowchart LR
    Domain["1. Domain\n(Pure C#, Zero Dependencies)"]
    Application["2. Application\n(Use Cases, DTOs, Interfaces)"]
    Infrastructure["3. Infrastructure\n(EF Core, SQL Server, SMTP)"]
    Api["4. Api / Presentation\n(Controllers, Middlewares, DI)"]

    Application -->|"Phụ thuộc vào"| Domain
    Infrastructure -->|"Hiện thực Interfaces của"| Application
    Infrastructure -->|"Phụ thuộc vào"| Domain
    Api -->|"Gọi Use Cases của"| Application
    Api -->|"Cấu hình DI cho"| Infrastructure
```

### 2.1. HRM.Domain (Tầng Miền Nghiệp vụ Cốt lõi)
- **Đặc điểm**: Mã nguồn C# thuần túy (.NET 10), tuyệt đối không phụ thuộc vào ASP.NET Core, EF Core, UI hay bất kỳ thư viện ngoài nào.
- **Trách nhiệm**:
  - Định nghĩa thực thể (Entities), tập hợp thực thể (Aggregates) và giá trị đối tượng (Value Objects).
  - Đảm bảo các **bất biến nghiệp vụ cốt lõi (Business Invariants)**:
    - `LeaveBalance`: Quỹ phép không được âm (`AvailableDays >= 0`).
    - `PayrollCalculationSnapshot`: Bất biến sau khi phê duyệt và khóa kỳ (`Status == Locked` thì cấm cập nhật).
    - `IdempotencyKey`: Chống trùng lặp sự kiện chấm công (`AttendanceEvent`).

### 2.2. HRM.Application (Tầng Ứng dụng & Điều phối Nghiệp vụ)
- **Đặc điểm**: Chứa toàn bộ các Use Case nghiệp vụ, tổ chức theo mô hình Command/Query.
- **Trách nhiệm**:
  - Nhận yêu cầu từ Controllers thông qua DTO chuẩn.
  - Phối hợp truy xuất dữ liệu thông qua giao diện `IUnitOfWork` và các Repository Interfaces.
  - Áp dụng kiểm tra hợp lệ dữ liệu (FluentValidation).
  - Kích hoạt ghi nhận Transactional Outbox khi có sự kiện cần gửi thông báo ra bên ngoài (email kích hoạt, email reset mật khẩu).

### 2.3. HRM.Infrastructure (Tầng Hạ tầng Kỹ thuật)
- **Đặc điểm**: Đóng gói toàn bộ chi tiết công nghệ, thư viện bên thứ ba và giao tiếp thiết bị/mạng.
- **Trách nhiệm**:
  - Hiện thực `HrmDbContext` (EF Core) trên SQL Server 2022.
  - Áp dụng cấu hình cơ sở dữ liệu: Kiểu dữ liệu, chỉ mục duy nhất (Unique Index), khóa lạc quan (`RowVersion`), chuyển đổi múi giờ UTC.
  - Hiện thực gửi email (`SmtpEmailSender`, `DevSinkEmailSender`) có mặt nạ bảo mật (Masking Log).

### 2.4. HRM.Api (Tầng Tiếp nhận & Phân phối API)
- **Đặc điểm**: Điểm khởi đầu của ứng dụng (Entrypoint), lưu trữ Kestrel Web Server và Dependency Injection Container.
- **Trách nhiệm**:
  - Định tuyến RESTful API, kiểm tra xác thực người dùng (Authentication) và phân quyền vai trò (RBAC Authorization).
  - Chuỗi Middlewares an ninh:
    - Kiểm tra `SecurityStamp` để thu hồi phiên tức thời khi tài khoản bị khóa hoặc đổi mật khẩu.
    - Chống Brute Force (`RateLimitingMiddleware` 20 req/phút).
    - Kiểm tra Anti-CSRF Token (`X-XSRF-TOKEN`).
  - Đăng ký và vận hành tiến trình nền `EmailOutboxWorker`.

---

## 3. Kiến trúc Bảo mật Phòng vệ Chiều sâu (Defense-in-Depth)

Hệ thống áp dụng kiến trúc an ninh đa tầng:

```mermaid
flowchart TD
    Request["Request từ Trình duyệt / Ứng dụng Di động"] --> Nginx["1. Nginx Gateway:\nHTTPS TLS 1.3, CSP, HSTS, SameSite Cookie Proxying"]
    Nginx --> RateLimit["2. Rate Limiting Middleware:\nChống tấn công từ chối dịch vụ & dò mật khẩu"]
    RateLimit --> AuthValidation["3. Authentication & SecurityStamp Middleware:\nKiểm tra JWT/Cookie, đối chiếu SecurityStamp trong Database"]
    AuthValidation --> RBAC["4. RBAC Authorization Handler:\nKiểm tra Quyền (Employee, HR, Payroll, SystemAdmin)"]
    RBAC --> Scoping["5. Record Scoping / IDOR Protection:\nNhân viên chỉ truy cập dữ liệu chính mình (/me hoặc UserId match)"]
    Scoping --> ConcurrencyTx["6. Database Concurrency & Constraints:\nUPDLOCK, ROWLOCK, Check Constraints, RowVersion Optimistic Lock"]
```

---

## 4. Liên kết Tài liệu Liên quan

- [ERD Thực thể Cốt lõi](file:///d:/dotnet%20project/docs/diagrams/erd-core-entities.md)
- [Sequence Diagram Duyệt Nghỉ phép Đồng thời](file:///d:/dotnet%20project/docs/diagrams/sequence-leave-approval.md)
- [Sequence Diagram Tính Lương Snapshot](file:///d:/dotnet%20project/docs/diagrams/sequence-payroll-calculation.md)
- [Sequence Diagram Transactional Outbox](file:///d:/dotnet%20project/docs/diagrams/sequence-transactional-outbox.md)
- [Sequence Diagram Thu hồi Phiên Đăng nhập](file:///d:/dotnet%20project/docs/diagrams/sequence-auth-revocation.md)
- [Báo cáo Kiểm thử & An ninh T24](file:///d:/dotnet%20project/docs/verification/T24-e2e-security-report.md)
