# ADR-0008: Nền tảng backend, transaction và hợp đồng dùng chung

- Ngày: 2026-11-11.
- Trạng thái: triển khai trong T05, READY_FOR_REVIEW; không thay chữ ký nghiệm thu của chủ dự án.
- Căn cứ: phạm vi modular monolith, .NET 10, DB riêng và quy ước API đã chốt ở T00/đặc tả MVP.

## Quyết định

1. Bốn project Domain → Application → Infrastructure/Api; Domain không tham chiếu framework,
   Application chỉ phụ thuộc Domain. Api tham chiếu Infrastructure tại composition root, controller
   nghiệp vụ nhận port Application. Assembly `HRM.Next.*` tách với legacy.
2. DbContext scoped từng request; default NoTracking. Query dùng projection DTO, AsNoTracking,
   cancellation và phân trang có giới hạn. Command cần sửa entity phải đọc bằng AsTracking rõ ràng.
   Repository chỉ thêm/tìm entity; không tự SaveChanges hoặc mở transaction.
3. `IUnitOfWork.ExecuteAsync` là ranh giới một use case: mở transaction, thực hiện các bước,
   SaveChanges rồi commit; lỗi/cancellation rollback và clear tracker. Có SaveChanges tường minh
   để flush giữa các bước nhưng chưa commit. Không hỗ trợ nested transaction. Không bật retry tự động
   cho write: replay cần idempotency thiết kế ở task nghiệp vụ, không tự chạy lại một phần transaction.
4. Connection string bắt buộc cấu hình ngoài source, tên DB bắt đầu `HRM_Next_`. Không fallback DB
   legacy, không tự create/migrate/seed lúc startup. Guard tên không thay thế phân quyền SQL;
   tài khoản triển khai phải chỉ được quyền trên DB cần dùng. Migration đầu chỉ tạo Departments để
   kiểm tra hạ tầng và contract; schema nhân sự/lương đầy đủ thuộc các task tiếp theo.
5. `/health/live` độc lập SQL; `/health/ready` kết nối thật và từ chối khi còn pending migrations,
   timeout giới hạn và không trả exception text. TimeProvider được DI để điều khiển giờ trong test;
   cancellation truyền từ HTTP xuống EF và transaction. Cleanup rollback không dùng token đã hủy.
6. API `/api/v1`; danh sách trả items/page/pageSize/totalCount/totalPages. Mặc định 20, tối đa 100,
   page bắt đầu 1; chỉ chấp nhận sort whitelist, có Id làm khóa ổn định khi trùng giá trị sort.
7. Chọn **chuỗi decimal invariant** cho số tiền, không dùng float/minor-unit trong cùng contract.
   JSON converter không chấp nhận number, exponent, tràn hoặc làm tròn ngầm để vừa .NET decimal.
   Client giữ string; công thức, currency và làm tròn nghiệp vụ do task lương quyết định theo quy tắc đã duyệt.
   DateOnly dùng YYYY-MM-DD; timestamp yêu cầu offset đầu vào, chuẩn hóa UTC đuôi Z khi trả.
   Constructor parameter bắt buộc phải có, không âm thầm coi amount thiếu là zero.
8. ProblemDetails chứa errorCode/traceId; lỗi input 400, danh tính/quyền 401/403, xung đột 409,
   bất biến domain 422, lỗi không dự kiến 500 với nội dung chung. Không đưa SQL/stack/connection
   string vào response. T05 khóa mặc định business route, chưa cài Identity/session/row-level permissions;
   thay thế handler fail-closed ở T06, kiểm scope dữ liệu ở T07.
9. OpenAPI từ runtime thật, generated TypeScript dùng openapi-typescript và openapi-fetch đã lock.
   CI kiểm schema drift, generated drift, type tests và HTTP smoke. Chỉ expose schema mặc định trong
   Development; Production cần cấu hình tường minh. Không phát minh endpoint lương để minh họa scalar;
   DecimalString/LocalDate/UtcTimestamp là schema primitive dùng chung.

## Bằng chứng và hệ quả

Integration suite dùng SQL Server thật, DB GUID do fixture sở hữu; test identity/probe chỉ ở test
assembly. Kiểm rollback sau hai INSERT đã flush, commit thành công, tracked update, cancel rollback,
read không SaveChanges và không tracking, 24 request đồng thời, health, config và JSON conventions.
Lưu ledger migrations/SQL version/cleanup và hash source để kết quả có thể đối chiếu.

Windows CI giữ gate legacy; Linux CI chạy SQL container riêng và backend/contract gate, phụ thuộc
Windows job đã đạt. T05 chưa triển khai retry/idempotency, migration dữ liệu cũ, production TLS,
telemetry hoàn chỉnh, UI hoặc tác vụ T06 trở đi. Đây là nền tảng để tiếp tục, không phải cam kết production-ready.

Tham khảo: [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions),
[ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0),
[OpenAPI TypeScript](https://openapi-ts.dev/introduction).
