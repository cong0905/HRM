# Báo cáo Khảo sát & Kế hoạch Chuẩn bị Schema Legacy (T02)

- **Mã nhiệm vụ**: T02 — Sửa đường dựng schema và ràng buộc dữ liệu legacy
- **Trạng thái**: **READY_FOR_REVIEW** (Đã hoàn tất khảo sát, sửa Fluent API tối thiểu, tạo migration EF mới và kiểm chứng thành công trên 4 database test riêng; `HRM_System` hoàn toàn không bị can thiệp)
- **Ngày hoàn tất**: 10/11/2026 (Phiên bản cập nhật v2.0 sau kiểm chứng thực nghiệm)
- **Nhánh làm việc**: `task/T02` (bảo toàn 100% commit và working tree)
- **Baseline Git Tag**: `baseline-05254b3` tại commit `05254b306f1f882b118575f50c5c81b486f40c6c`
- **Mục tiêu giai đoạn**: Xác định chính xác các điểm lệch schema legacy giữa Code, EF Migrations và SQL Script tài liệu; thiết lập bộ công cụ preflight chỉ đọc và kế hoạch kiểm chứng SQL Server độc lập trước khi sửa đổi.

---

## 1. Hiện trạng Git & Bảo toàn Mã nguồn

1. **Trạng thái Git**:
   - Nhánh hiện tại: `task/T02`.
   - Commit gốc: `05254b306f1f882b118575f50c5c81b486f40c6c` (Tag: `baseline-05254b3`).
   - Toàn bộ các thay đổi và tài liệu từ T00, T01 được bảo toàn nguyên vẹn.
   - Cam kết: **Không commit, push, reset, checkout bỏ thay đổi hoặc sửa tag baseline**.
2. **Mã nguồn sản phẩm (`db/src`)**:
   - Giữ nguyên 100% trạng thái gốc; không sửa đổi bất kỳ file C#, file cấu hình hay migration nào trong giai đoạn này.

---

## 2. Bảng Đối chiếu Toàn diện Hệ thống Schema Legacy

Đối chiếu chi tiết giữa 6 nguồn thông tin schema của dự án:
1. **Entity C#**: `HRM.Domain/Entities/`
2. **EF Configuration**: `HRM.DAL/Configurations/`
3. **EF Migrations nhận diện**: 6 migration có metadata hợp lệ trong `HRM.DAL/Migrations/`
4. **EF Model Snapshot**: `HRM.DAL/Migrations/HrmDbContextModelSnapshot.cs`
5. **SQL sinh Offline từ EF**: `docs/verification/ef_generated_schema.sql` (sinh qua EF Core CLI)
6. **Script SQL Tài liệu Legacy**: `db/docs/HRM_Database.sql`

| Thành phần Schema | Entity C# Domain | EF Configuration C# | EF Model Snapshot | SQL Sinh Offline EF Core | Script Tài liệu Legacy (`HRM_Database.sql`) | Đánh giá & Rủi ro |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Cột `Hwid` trên bảng `ChamCong`** | Có (`public string? Hwid`) tại [ChamCong.cs](file:///D:/dotnet%20project/db/src/HRM.Domain/Entities/ChamCong.cs#L16) | Có (`HasMaxLength(128)`) tại [ChamCongConfiguration.cs](file:///D:/dotnet%20project/db/src/HRM.DAL/Configurations/ChamCongConfiguration.cs#L18) | Có (`Property<string>("Hwid")`) tại line 157 Snapshot | Có: sinh lệnh `ALTER TABLE [ChamCong] ADD [Hwid] nvarchar(128) NULL;` tại line 927 | **KHÔNG CÓ** (Bảng `ChamCong` thiếu hẳn cột `Hwid`) | **ĐÃ CHỨNG MINH TĨNH**: Chuỗi migration EF đã tạo `Hwid`. Tài liệu legacy bị lạc hậu. Tuyệt đối **không thêm lại cột Hwid** trong T02. |
| **Bảng `PasswordResetTokens`** | Có Entity tại [PasswordResetToken.cs](file:///D:/dotnet%20project/db/src/HRM.Domain/Entities/PasswordResetToken.cs) | **KHÔNG CÓ** class cấu hình trong `Configurations/` | Có table mapping tại line 978 Snapshot | Có lệnh `CREATE TABLE [PasswordResetTokens]` tại line 988 | **KHÔNG CÓ** (Bảng hoàn toàn vắng bóng trong script SQL) | **ĐÃ CHỨNG MINH TĨNH**: Bảng được bổ sung bằng EF migration sau này, tài liệu SQL legacy không được cập nhật. |
| **Khóa ngoại Token -> Tài khoản** | Thuộc tính `MaTaiKhoan` và navigation `TaiKhoan` | Mặc định theo quy ước EF (không cấu hình tường minh) | Sinh shadow property `TaiKhoanMaTaiKhoan` tại line 1014 | Tạo FK trên `[TaiKhoanMaTaiKhoan]`, cột `[MaTaiKhoan]` không có FK | **KHÔNG CÓ** | **LỖI THIẾT KẾ ĐÃ KHẢO SÁT**: Code AuthService ghi vào `MaTaiKhoan` không có FK. Cột shadow `TaiKhoanMaTaiKhoan` có FK nhưng bị bỏ trống trong luồng code này. |
| **Ràng buộc Unique `TenDangNhap`** | Thuộc tính `TenDangNhap` string | Chỉ cấu hình `IsRequired().HasMaxLength(50)` | Chỉ có Index non-unique `IX_TaiKhoan_MaNhanVien`, **không có Unique** | Không có câu lệnh tạo Unique Index cho `TenDangNhap` | Có khai báo: `TenDangNhap VARCHAR(50) NOT NULL UNIQUE` | **ĐÃ CHỨNG MINH TĨNH**: EF Core bỏ sót Unique Index. Script tài liệu khai báo Unique nhưng EF migration không hề tạo. |
| **Kiểu dữ liệu `TenDangNhap`** | `string` | `nvarchar(50)` | `nvarchar(50)` | `nvarchar(50)` | `VARCHAR(50)` | Lệch kiểu Unicode (`nvarchar` vs `varchar`). EF dùng `nvarchar(50)`. |
| **Ràng buộc Unique `MaNhanVien` trên `TaiKhoan`** | Thuộc tính `MaNhanVien` int | Cấu hình quan hệ N-1 (`HasOne.WithMany`) | Index non-unique `IX_TaiKhoan_MaNhanVien` | Index non-unique `IX_TaiKhoan_MaNhanVien` | Khai báo `MaNhanVien INT NOT NULL UNIQUE` | Script tài liệu coi là quan hệ 1-1 (`UNIQUE`), trong khi EF coi là N-1. |

---

## 3. Bốn Trọng tâm Khảo sát & Giới hạn Xác minh

### 3.1 Trọng tâm 1: Cột `Hwid` trên bảng `ChamCong`
- **Vị trí code**:
  * [HRM.Domain/Entities/ChamCong.cs:16](file:///D:/dotnet%20project/db/src/HRM.Domain/Entities/ChamCong.cs#L16): `public string? Hwid { get; set; }`
  * [HRM.DAL/Configurations/ChamCongConfiguration.cs:18](file:///D:/dotnet%20project/db/src/HRM.DAL/Configurations/ChamCongConfiguration.cs#L18): `builder.Property(e => e.Hwid).HasMaxLength(128);`
  * [HRM.DAL/Migrations/20260502105319_AddChamCongHwid.cs:14](file:///D:/dotnet%20project/db/src/HRM.DAL/Migrations/20260502105319_AddChamCongHwid.cs#L14): Thao tác `AddColumn<string>("Hwid", "ChamCong", maxLength: 128)`.
- **Bằng chứng thực nghiệm**:
  * File script DDL offline [ef_generated_schema.sql](file:///D:/dotnet%20project/docs/verification/ef_generated_schema.sql) dòng 927: `ALTER TABLE [ChamCong] ADD [Hwid] nvarchar(128) NULL;`
  * Test hồi quy T01 `EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong` đạt **PASS**.
- **Giải mã class migration thừa `20260504161102_AddHwidToChamCong.cs`**:
  * Class này kế thừa `Migration` nhưng **thiếu `[MigrationAttribute]`**, EF Core discovery bỏ qua.
  * Phương thức `Up()` của class này chỉ chứa mã cập nhật seed data `NhanVien`/`PhongBan`, hoàn toàn **không có thao tác thêm cột Hwid**.
- **Kết luận**:
  * Chuỗi migration EF Core hợp lệ **đã chứa thao tác tạo cột Hwid**.
  * **Tuyệt đối KHÔNG thêm lệnh tạo cột Hwid lần thứ hai** trong các migration sửa đổi tiếp theo (tránh lỗi duplicate column).
  * Không sửa class lịch sử cũ `AddHwidToChamCong`.

### 3.2 Trọng tâm 2: Quan hệ và Khóa ngoại của Token với Tài khoản (`PasswordResetTokens`)
- **Vị trí code**:
  * [HRM.Domain/Entities/PasswordResetToken.cs:7-17](file:///D:/dotnet%20project/db/src/HRM.Domain/Entities/PasswordResetToken.cs#L7-L17): Có `int? MaTaiKhoan` và `TaiKhoan? TaiKhoan`.
  * [HRM.DAL/Migrations/20260517153408_AddPasswordResetTokens.cs:20-36](file:///D:/dotnet%20project/db/src/HRM.DAL/Migrations/20260517153408_AddPasswordResetTokens.cs#L20-L36): Tạo cột `MaTaiKhoan` (int NULL), cột shadow `TaiKhoanMaTaiKhoan` (int NULL), và FK `FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan` đặt trên cột shadow.
  * [HRM.BLL/Services/AuthService.cs:67-76](file:///D:/dotnet%20project/db/src/HRM.BLL/Services/AuthService.cs#L67-L76): Mã nguồn chỉ gán `MaTaiKhoan = taiKhoan.MaTaiKhoan`, không gán `TaiKhoan = taiKhoan`.
- **Đánh giá mức độ chắc chắn theo đúng giới hạn khảo sát**:
  * *Trong phạm vi mã nguồn C# đã khảo sát*: Luồng tạo token qua `AuthService` chỉ gán giá trị cho trường `MaTaiKhoan`, không gán object `TaiKhoan` hay shadow property `TaiKhoanMaTaiKhoan`. Vì vậy, với mọi bản ghi sinh ra từ luồng `AuthService` này, giá trị cột shadow `TaiKhoanMaTaiKhoan` sẽ là `NULL`, còn cột `MaTaiKhoan` có giá trị nhưng không được bảo vệ bởi khóa ngoại cơ sở dữ liệu.
  * *Ranh giới dữ liệu thực tế*: Chưa thể khẳng định cột shadow luôn luôn là NULL trên mọi database lịch sử (phòng trường hợp từng có code cũ hoặc script nạp trực tiếp vào cột shadow). Tình trạng phân bố dữ liệu thực tế bắt buộc phải được kiểm chứng bằng script preflight trên SQL Server.

### 3.3 Trọng tâm 3: Unique Constraint cho Username và Phân tích Collation
- **Vị trí code**:
  * [HRM.DAL/Configurations/ChinhSachHeThongConfiguration.cs:18](file:///D:/dotnet%20project/db/src/HRM.DAL/Configurations/ChinhSachHeThongConfiguration.cs#L18): `builder.Property(t => t.TenDangNhap).IsRequired().HasMaxLength(50);` (Hoàn toàn thiếu `IsUnique()`).
  * [HRM.DAL/Migrations/20260331144555_InitialCreate.cs:1063-1066](file:///D:/dotnet%20project/db/src/HRM.DAL/Migrations/20260331144555_InitialCreate.cs#L1063-L1066): Chỉ có index non-unique cho `MaNhanVien`.
- **Phân biệt Collation Database và Collation Cột**:
  * **Collation của Database** (ví dụ: `SQL_Latin1_General_CP1_CI_AS` hoặc `Vietnamese_CI_AS`) là thiết lập mặc định của cơ sở dữ liệu.
  * **Collation của cột `TenDangNhap`** là thuộc tính trực tiếp quyết định hành vi so sánh chuỗi của cột đó. Nếu không khai báo riêng, cột kế thừa collation của database; nếu từng được định nghĩa với collation riêng biệt, quy tắc so sánh sẽ theo collation của chính cột đó.
- **Phân biệt 2 Cấp độ Kiểm tra Trùng lặp Username**:
  1. **Kiểm tra theo quy tắc Unique Index dự kiến** (`GROUP BY TenDangNhap HAVING COUNT(*) > 1`):
     - Đây là điều kiện trực tiếp: Nếu có bản ghi trùng theo collation của cột, câu lệnh `CREATE UNIQUE INDEX` khi chạy migration sẽ **thất bại ngay lập tức** (Lỗi 2601 / 1505).
  2. **Kiểm tra bổ sung chuẩn hóa UPPER/TRIM** (`GROUP BY UPPER(RTRIM(LTRIM(TenDangNhap))) HAVING COUNT(*) > 1`):
     - Đây là kiểm tra dự phòng an toàn: Phát hiện các tài khoản chỉ khác nhau bởi khoảng trắng thừa hoặc khác biệt chữ hoa/thường (đặc biệt quan trọng nếu collation của cột là Case-Sensitive `_CS`, nơi SQL Server cho phép nhưng tầng ứng dụng so sánh không phân biệt hoa/thường có thể gây nhầm lẫn tài khoản).

### 3.4 Trọng tâm 4: Các Trạng thái Dữ liệu Nguy cơ Gây Lỗi khi Bổ sung Ràng buộc
Trước khi thêm các ràng buộc toàn vẹn vào database legacy đang có dữ liệu, các tình huống sau có thể làm migration thất bại:
1. **Dữ liệu Username bị trùng lặp**: Trùng theo collation của cột hoặc trùng dạng hoa/thường.
2. **Dữ liệu Token mồ côi (`Orphaned Tokens`)**: Bản ghi `PasswordResetTokens` có `MaTaiKhoan` trỏ đến ID tài khoản không còn tồn tại trong `dbo.TaiKhoan`.
3. **Lệch giá trị giữa `MaTaiKhoan` và `TaiKhoanMaTaiKhoan`** (xem chi tiết mục 4 bên dưới).

---

## 4. Bảng Quyết định Xử lý Dữ liệu Token Trước khi Bỏ Cột Shadow

Khi loại bỏ cột shadow `TaiKhoanMaTaiKhoan` và chuẩn hóa khóa ngoại trên cột `MaTaiKhoan`, dữ liệu hiện hữu phải được phân loại và xử lý theo ma trận 5 trạng thái:

| Trạng thái Dữ liệu | Điều kiện Nhận diện | Đánh giá & Rủi ro | Hướng Xử lý Chuyển đổi | Quyết định Thực thi |
| :--- | :--- | :--- | :--- | :--- |
| **Trạng thái 1: Hai ID bằng nhau** | `MaTaiKhoan IS NOT NULL`<br>`AND TaiKhoanMaTaiKhoan IS NOT NULL`<br>`AND MaTaiKhoan = TaiKhoanMaTaiKhoan` | Dữ liệu hoàn toàn nhất quán giữa business property và shadow FK. | Giữ nguyên giá trị `MaTaiKhoan`, an toàn drop khóa ngoại cũ và drop cột shadow `TaiKhoanMaTaiKhoan`. | **TỰ ĐỘNG CHUYỂN ĐỔI** trong migration script. |
| **Trạng thái 2: Chỉ `MaTaiKhoan` có giá trị** | `MaTaiKhoan IS NOT NULL`<br>`AND TaiKhoanMaTaiKhoan IS NULL` | Trạng thái dự kiến từ luồng code `AuthService` đã khảo sát. | Kiểm tra `MaTaiKhoan` có tồn tại trong `dbo.TaiKhoan` hay không. Nếu hợp lệ: Giữ nguyên `MaTaiKhoan` để trỏ FK mới. Nếu không tồn tại: Xử lý theo Trạng thái 5 (mồ côi). | **TỰ ĐỘNG CHUYỂN ĐỔI** (nếu tài khoản tồn tại). |
| **Trạng thái 3: Chỉ shadow FK có giá trị** | `MaTaiKhoan IS NULL`<br>`AND TaiKhoanMaTaiKhoan IS NOT NULL` | Có thể phát sinh nếu dữ liệu được nạp qua EF navigation mà không gán foreign key ID. | Cần sao chép giá trị từ `TaiKhoanMaTaiKhoan` sang `MaTaiKhoan` trước khi drop cột shadow. | **DỪNG LẠI BÁO CÁO**: Cần xác nhận của chủ dự án trước khi backfill, không tự ý sửa dữ liệu ngầm. |
| **Trạng thái 4: Hai ID khác nhau** | `MaTaiKhoan IS NOT NULL`<br>`AND TaiKhoanMaTaiKhoan IS NOT NULL`<br>`AND MaTaiKhoan <> TaiKhoanMaTaiKhoan` | **Xung đột dữ liệu nghiêm trọng**: Không thể xác định token thực sự thuộc về tài khoản nào. | **Tuyệt đối không tự ý gán token sang tài khoản khác** hoặc tự chọn một trong hai ID. | **DỪNG LẬP TỨC VÀ BÁO CÁO**: Xuất danh sách Token ID và hai giá trị Account ID tương ứng để người kiểm duyệt quyết định thủ công. |
| **Trạng thái 5: Token mồ côi hoặc Cả hai đều NULL** | `(MaTaiKhoan IS NULL AND TaiKhoanMaTaiKhoan IS NULL)`<br>HOẶC `MaTaiKhoan NOT IN (SELECT MaTaiKhoan FROM dbo.TaiKhoan)` | Token không liên kết tài khoản nào (ví dụ token VerifyEmail độc lập) hoặc tài khoản liên kết đã bị xóa. | Giữ thuộc tính `MaTaiKhoan` là nullable (`int?`). Với token có ID mồ côi không tồn tại trong `TaiKhoan`, lệnh tạo FK sẽ bị lỗi nếu không xử lý trước. | **BÁO CÁO DANH SÁCH MỒ CÔI**: Không tự ý xóa token; chủ dự án duyệt danh sách trước khi chạy migration (xóa token hết hạn hoặc set null). |

### Phân tích Lựa chọn Delete Behavior theo Nghiệp vụ (Chưa mặc định Cascade)
- **`Cascade`**: Khi một tài khoản bị xóa, toàn bộ token liên quan bị xóa tự động.
  * *Rủi ro*: Có thể làm mất dấu vết kiểm toán (audit trail) nếu tài khoản bị xóa trong quá trình điều tra sự cố bảo mật.
- **`Restrict` / `NoAction`**: Ngăn chặn xóa tài khoản nếu tài khoản đó vẫn còn token liên kết trong cơ sở dữ liệu.
  * *Ưu điểm*: An toàn cao, buộc quy trình nghiệp vụ phải xử lý hoặc thu hồi token trước khi xóa tài khoản.
- **`SetNull`**: Khi xóa tài khoản, token được giữ lại nhưng trường `MaTaiKhoan` chuyển thành NULL.
- **Quyết định cho T02**: Trong đề xuất sửa schema, **chưa áp dụng mặc định `DeleteBehavior.Cascade`**; tạm thời giữ hành vi an toàn mặc định của EF Core (`Restrict`/`NoAction`) cho đến khi có quyết định nghiệp vụ chính thức từ chủ dự án.

---

## 5. Vai trò và Rủi ro của `db/fix_db_history.sql`

- **Vị trí file**: [db/fix_db_history.sql](file:///D:/dotnet%20project/db/fix_db_history.sql)
- **Nội dung thực tế**:
  Chèn bản ghi `20260331144555_InitialCreate` vào bảng `[dbo].[__EFMigrationsHistory]`.
- **Đánh giá khách quan về sự tồn tại của dòng migration history**:
  * Việc có một dòng `20260331144555_InitialCreate` trong `__EFMigrationsHistory` là **hoàn toàn bình thường và bắt buộc** đối với bất kỳ database nào được tạo đúng chuẩn qua EF Core.
  * Sự tồn tại của dòng migration đó **chưa đủ để khẳng định là fake history**.
  * Chỉ được coi là "fake history" khi đối chiếu thấy: Có dòng `InitialCreate` trong bảng lịch sử nhưng cấu trúc các bảng thực tế lại sai lệch sâu sắc so với DDL của `InitialCreate` (ví dụ: tạo bằng script `HRM_Database.sql` thiếu cột `Hwid`, sai kiểu dữ liệu, thiếu bảng con).
- **Rủi ro của việc lạm dụng script can thiệp lịch sử**:
  * Bỏ qua dữ liệu seed chuẩn của hệ thống.
  * Gây gãy các migration tiếp theo khi database hiện tại thiếu cột/bảng mà các migration sau giả định là đã có.
  * **Quy tắc T02**: Không hướng dẫn chèn lịch sử thủ công; phải thực hiện kiểm tra đối chiếu schema (schema reconciliation) trước khi đồng bộ.

---

## 6. Script SQL Preflight Chỉ Đọc (`scripts/preflight_schema_check.sql`)

Script [scripts/preflight_schema_check.sql](file:///D:/dotnet%20project/scripts/preflight_schema_check.sql) đã được chuẩn hóa toàn diện:
1. **Định danh schema rõ ràng**: Toàn bộ đối tượng đều dùng tiền tố `dbo.` (`dbo.TaiKhoan`, `dbo.ChamCong`, `dbo.PasswordResetTokens`, `dbo.__EFMigrationsHistory`).
2. **Chống lỗi biên dịch khi thiếu bảng/cột**:
   - Sử dụng `OBJECT_ID(N'dbo.TableName', N'U')` để kiểm tra bảng.
   - Với các truy vấn phụ thuộc cột tùy chọn (`Hwid` trên `dbo.ChamCong`, `TaiKhoanMaTaiKhoan` trên `dbo.PasswordResetTokens`), script sử dụng `sp_executesql` với chuỗi SQL động chỉ đọc tĩnh, được bảo vệ sau lệnh kiểm tra `COL_LENGTH(...) IS NOT NULL`. Cơ chế này ngăn chặn hoàn toàn lỗi compile-time `Invalid column name` khi chạy trên database legacy chưa có các cột này.
3. **An toàn & Bảo mật**:
   - Không chứa bất kỳ tham số đầu vào tùy ý nào (tránh SQL injection).
   - Chỉ đọc 100% (SELECT only), không có câu lệnh ghi.
   - Tuyệt đối không truy vấn nội dung cột `Token` hoặc `MatKhauHash`.
4. **Ghi chú trạng thái**: Script đã được kiểm tra cú pháp tĩnh; **chưa được kiểm chứng thực thi trên SQL Server thật** do chưa tạo môi trường database test.

---

## 7. Kế hoạch Kiểm thử SQL Server Độc lập

### 7.1 Hiện trạng Công cụ Cục bộ & Khẳng định Fixture
- **Công cụ có sẵn trên máy**: Đã xác nhận có `SQLCMD.EXE` và `SqlLocalDB.exe` trong PATH (chưa khởi động dịch vụ).
- **Khẳng định về dữ liệu**: Repository **hoàn toàn không có snapshot database thật** (`.bak`/`.mdf`). Toàn bộ thử nghiệm nâng cấp sẽ được thực hiện trên **Synthetic Test Fixtures (Fixture Tự Tạo)**, tuyệt đối không gọi là snapshot thật.

### 7.2 Hai Luồng Chuyển đổi Schema Độc lập
Kế hoạch kiểm thử tách bạch rõ ràng giữa 2 trường hợp xuất phát điểm:
- **Luồng A (EF Migration Upgrade Path)**: Nâng cấp cơ sở dữ liệu đã có lịch sử EF Core hợp lệ (chạy tuần tự từ migration `InitialCreate` đến migration mới nhất).
- **Luồng B (Legacy Schema Reconciliation Path)**: Chuyển đổi cơ sở dữ liệu xuất phát từ script SQL legacy (`HRM_Database.sql`).
  * *Hướng xử lý*: Không chèn history giả mạo. Thực hiện kiểm tra schema diff bằng script preflight, bổ sung các cột/bảng còn thiếu (như cột `Hwid` và bảng `PasswordResetTokens`) bằng script DDL chuyển tiếp rõ ràng, sau đó mới đồng bộ bảng lịch sử một cách minh bạch.

### 7.3 Danh mục Migration Hợp lệ của Phiên bản
Không hard-code một con số cố định ("đủ 7 migrations"); đối chiếu chính xác theo danh sách migration hợp lệ trong assembly của phiên bản triển khai:
- Phiên bản baseline hiện tại: **6 migrations hợp lệ** (`InitialCreate`, `SimplifyHieuSuatAndRemoveEvaluator`, `RemoveHieuSuatRankingAndUiFields`, `AddChamCongHwid`, `AddPasswordResetTokens`, `RedesignHieuSuatFormulas`).
- Khi T02 tạo thêm migration sửa schema: Số lượng sẽ là **7 migrations hợp lệ**.

### 7.4 Kiểm tra Bảo toàn Dữ liệu Nghiệp vụ (Data Integrity Beyond Row Counts)
Ngoài việc so sánh số lượng dòng (`COUNT(*)`), kế hoạch kiểm thử bổ sung các tiêu chí kiểm tra giá trị nghiệp vụ trước và sau nâng cấp:
1. **Dữ liệu Nhân viên (`dbo.NhanVien`)**:
   - Tổng quỹ lương cơ bản: `SUM(MucLuong)`.
   - Danh sách mã định danh `MaNhanVien` và ngày vào làm `NgayVaoLam`.
   - Số lượng nhân viên theo từng trạng thái `TrangThai`.
2. **Dữ liệu Chấm công (`dbo.ChamCong`)**:
   - Tổng số giờ làm việc: `SUM(TongGioLam)`.
   - Tổng số giờ làm thêm: `SUM(GioLamThem)`.
   - Số lượng bản ghi theo hình thức và trạng thái chấm công.
   - Các giá trị `Hwid` đã có không bị ghi đè thành NULL.
3. **Dữ liệu Lương (`dbo.BangLuong`)**:
   - Tổng thực lĩnh: `SUM(ThucLinh)`.
   - Tổng lương cơ bản: `SUM(LuongCoBan)`.
   - Phân bố theo từng kỳ `Thang`/`Nam`.

---

## 8. Đề xuất Phương án Sửa Nhỏ Nhất (Minimal Fix Proposal)

### 8.1 Sửa Code (Thực hiện ở bước tiếp theo)
1. **Cấu hình Fluent API cho `PasswordResetToken`**:
   - Thêm class cấu hình trỏ FK tường minh vào `MaTaiKhoan`:
     ```csharp
     builder.ToTable("PasswordResetTokens");
     builder.HasKey(x => x.Id);
     builder.HasOne(x => x.TaiKhoan)
            .WithMany()
            .HasForeignKey(x => x.MaTaiKhoan)
            .OnDelete(DeleteBehavior.Restrict); // Chưa mặc định Cascade
     ```
   - **Lưu ý về độ dài cột**: Trong baseline hiện tại, `Token` và `Purpose` đều là `nvarchar(max)`. **Không tự ý thu hẹp độ dài** thành `HasMaxLength(256)` hay `HasMaxLength(50)` trong phương án sửa FK này để tránh rủi ro truncate dữ liệu. Nếu cần tối ưu độ dài, sẽ lập đề xuất riêng kèm bước preflight kiểm tra độ dài dữ liệu thực tế.
2. **Bổ sung Unique Index cho `TenDangNhap`**:
   - Trong `ChinhSachHeThongConfiguration.cs`:
     ```csharp
     builder.HasIndex(t => t.TenDangNhap).IsUnique();
     ```
3. **Tạo Migration Mới (Không sửa migration lịch sử cũ)**:
   - Tạo migration bổ sung (ví dụ: `FixPasswordResetTokenFkAndUniqueUsername`):
     * Drop constraint FK cũ trên shadow column `TaiKhoanMaTaiKhoan`.
     * Drop shadow column `TaiKhoanMaTaiKhoan`.
     * Tạo constraint FK mới trên `MaTaiKhoan`.
     * Tạo Unique Index `IX_TaiKhoan_TenDangNhap`.
   - **Tuyệt đối không thêm cột `Hwid`** vào migration mới.

### 8.2 Sửa Tài liệu
1. Cập nhật file [db/docs/HRM_Database.sql](file:///D:/dotnet%20project/db/docs/HRM_Database.sql) để phản ánh đúng cột `Hwid`, bảng `PasswordResetTokens` và kiểu dữ liệu `NVARCHAR`.
2. Ghi chú rõ ràng trong [db/fix_db_history.sql](file:///D:/dotnet%20project/db/fix_db_history.sql) về rủi ro và giới hạn áp dụng.

### 8.3 Những việc Còn phải Kiểm chứng trên Database Test Riêng
1. Khởi tạo môi trường database test riêng biệt (`HRM_Next_Test`) theo đúng quyết định `OPEN-02` (không thử nghiệm sửa đổi trên `HRM_System`).
2. Thực thi 2 luồng nâng cấp trên `HRM_Next_Test` (Luồng A từ EF migrations và Luồng B từ synthetic legacy fixture).
3. Đối chiếu số dòng và checksum giá trị nghiệp vụ (`MucLuong`, `TongGioLam`, `LuongThucNhan`) trước và sau nâng cấp.

---

## 9. Bằng chứng Khảo sát Thực tế trên Database `HRM_System` (Localhost)

Được sự phê duyệt của Chủ dự án, quá trình khảo sát **CHỈ ĐỌC (SELECT ONLY)** đã được thực hiện trực tiếp trên database `HRM_System` thông qua `sqlcmd -S localhost -d HRM_System -E -W -i scripts/preflight_schema_check.sql`:

### 9.1 Thông tin Server & Database Đích
- **Server**: `MSI` (Microsoft SQL Server 2022 RTM Developer Edition - 64-bit).
- **Database**: `HRM_System`.
- **Collation của Database**: `Vietnamese_CI_AS` (Case-Insensitive, Accent-Sensitive).
- **Trạng thái thực thi**: **Exit code 0**, hoàn toàn không có thao tác ghi hay sửa đổi schema/dữ liệu.

### 9.2 Kết quả Khảo sát Bảng `dbo.TaiKhoan`
- **Số lượng tài khoản**: 7 tài khoản có tên đăng nhập phân biệt (gồm các tài khoản demo dạng chữ cái và địa chỉ email; không trích xuất thông tin định danh cá nhân cụ thể).
- **Collation của cột `TenDangNhap`**: `Vietnamese_CI_AS`.
- **Kiểm tra trùng lặp (Collation)**: **0 trùng lặp** (Không có tài khoản nào trùng tên theo collation của cột).
- **Kiểm tra bổ sung hoa/thường (UPPER/TRIM)**: **0 xung đột** (Không có tài khoản nào chỉ khác biệt về chữ hoa/thường hoặc khoảng trắng).
- **Kiểm tra liên kết nhân viên (`MaNhanVien`)**: **0 trùng lặp** (Mỗi nhân viên chỉ gắn tối đa 1 tài khoản; các ID nhân viên liên kết đều duy nhất).
- **Đánh giá thực nghiệm**: Chưa phát hiện xung đột trong dữ liệu tại thời điểm khảo sát; cần kiểm chứng migration khi bổ sung Unique Index.

### 9.3 Kết quả Khảo sát Bảng `dbo.ChamCong` và Cột `Hwid`
- **Tổng số bản ghi**: 8 bản ghi.
- **Thực trạng cột `Hwid`**:
  * Đã tồn tại thực tế trên bảng `dbo.ChamCong` (kiểu `nvarchar(128)`).
  * 4 bản ghi (tháng 04/2026): `Hwid = NULL` (tạo trước thời điểm có tính năng Hwid).
  * 4 bản ghi (tháng 05/2026): Đã ghi nhận giá trị Hwid định dạng GUID hợp lệ (từ migration `AddChamCongHwid`).
- **Đánh giá thực nghiệm**: Cột `Hwid` đã tồn tại và có dữ liệu trong schema thực tế của database. Bác bỏ giả thuyết cần thêm lại cột `Hwid`; việc cố gắng tạo lại cột này trong migration mới sẽ làm lỗi pipeline do trùng cột.

### 9.4 Kết quả Khảo sát Bảng `dbo.PasswordResetTokens`
- **Tổng số bản ghi**: Đúng **1 bản ghi** duy nhất:
  * Thuộc tính kỹ thuật: `Id = 1`, `MaTaiKhoan = 4`, email demo, `Purpose = ResetPassword`, `IsUsed = 0`.
  * `TaiKhoanMaTaiKhoan = NULL`.
- **Khóa ngoại hiện có**: `FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan` đặt trên cột shadow `TaiKhoanMaTaiKhoan` (OnDelete: `NO_ACTION`).
- **Đánh giá theo Ma trận 5 Trạng thái**:
  * Bản ghi này thuộc **Trạng thái 2** (`MaTaiKhoan IS NOT NULL AND TaiKhoanMaTaiKhoan IS NULL`).
  * `MaTaiKhoan = 4` trỏ đúng vào một tài khoản hợp lệ đang tồn tại trong `dbo.TaiKhoan` (không phải token mồ côi).
  * **Đánh giá chuyển đổi**: Chưa phát hiện xung đột trong dữ liệu tại thời điểm khảo sát; cần kiểm chứng migration khi chuyển khóa ngoại sang `MaTaiKhoan` và drop cột shadow.

### 9.5 Kết quả Đối chiếu Lịch sử Migration (`dbo.__EFMigrationsHistory`)
Trong database `HRM_System`, bảng lịch sử ghi nhận **7 dòng migration**:
1. `20260331144555_InitialCreate` (8.0.25)
2. `20260421120929_SimplifyHieuSuatAndRemoveEvaluator` (8.0.26)
3. `20260421121936_RemoveHieuSuatRankingAndUiFields` (8.0.26)
4. `20260502105319_AddChamCongHwid` (8.0.26)
5. `20260504161102_AddHwidToChamCong` (8.0.26)
6. `20260517153408_AddPasswordResetTokens` (8.0.27)
7. `20260525112936_RedesignHieuSuatFormulas` (8.0.27)
- **Ghi nhận chênh lệch**: Bảng `__EFMigrationsHistory` của `HRM_System` có 7 dòng history trong khi assembly hiện tại chỉ nhận diện 6 migration (do class `20260504161102_AddHwidToChamCong.cs` thiếu metadata `[MigrationAttribute]`). Hiện chưa kết luận nguyên nhân dòng lịch sử dư này nếu không có thêm bằng chứng lịch sử commit/triển khai. Do đó, migration mới của T02 cần được kiểm chứng độc lập trên cả database có 6 dòng chuẩn và fixture mô phỏng 7 dòng history.

### 9.6 Giá trị Nghiệp vụ Cơ sở của `HRM_System` (Dùng đối chiếu không suy giảm)
- **Nhân viên (`dbo.NhanVien`)**: 10 bản ghi; Tổng mức lương cơ bản: `1,603,900,000.00` VND.
- **Chấm công (`dbo.ChamCong`)**: 8 bản ghi; Tổng giờ làm: `1.56` giờ; Tổng giờ OT: `0.00` giờ.
- **Bảng lương (`dbo.BangLuong`)**: 10 bản ghi; Tổng lương thực nhận (`LuongThucNhan`): `139,939,539.00` VND; Tổng lương cơ bản: `1,603,900,000.00` VND.
- **Tài khoản (`dbo.TaiKhoan`)**: 7 bản ghi.
- **Nghỉ phép (`dbo.DonNghiPhep`)**: 3 bản ghi.
- **Phòng ban (`dbo.PhongBan`)**: 6 bản ghi.

---

## 10. Bằng chứng Kiểm chứng Thực nghiệm trên Bộ Database Test Mới (`HRM_Next_Test_T02_v2_*`)

Theo yêu cầu nghiêm ngặt của dự án, bản sửa đổi migration không được kiểm chứng lại trên các database test cũ đã ghi nhận migration ID tương tự. Toàn bộ quá trình kiểm chứng được thực hiện trên **6 cơ sở dữ liệu test hoàn toàn mới** với tiền tố riêng `HRM_Next_Test_T02_v2_*`.

### 10.1 Quản lý Bản Sửa & Loại bỏ Triệt để `UpdateData` Ngoài Phạm vi
1. **Gỡ bỏ bản nháp cũ**: Sử dụng `dotnet ef migrations remove` để hoàn nguyên sạch sẽ migration dự thảo cũ và model snapshot.
2. **Xử lý gốc rễ thời gian động của seed data**:
   - Trong Entity models (`NhanVien.cs`, `PhongBan.cs`, `TaiKhoan.cs`), thuộc tính `NgayTao`/`NgayCapNhat` có giá trị mặc định là `DateTime.Now`. Khi khối `builder.HasData(...)` không khai báo ngày cố định, EF Core sẽ gán thời gian hiện tại mỗi lần sinh migration và tạo ra các lệnh `UpdateData` không mong muốn.
   - Khắc phục triệt để: Gán giá trị ngày cố định trong `NhanVienConfiguration.cs`, `PhongBanConfiguration.cs`, `ChinhSachHeThongConfiguration.cs` khớp 100% với snapshot migration trước (`20260525112936_RedesignHieuSuatFormulas`).
3. **Sinh migration chuẩn sạch**:
   - Scaffold migration mới [`20261110141626_FixLegacySchemaAndConstraints.cs`](file:///D:/dotnet%20project/db/src/HRM.DAL/Migrations/20261110141626_FixLegacySchemaAndConstraints.cs).
   - Kết quả: **Hoàn toàn KHÔNG CÓ bất kỳ lệnh `UpdateData` nào** trong cả phương thức `Up()` và `Down()`.
4. **Cài đặt chốt chặn bảo vệ chủ động trong đường thực thi migration (`migrationBuilder.Sql`)**:
   - Trước mọi thao tác thay đổi schema (`DropForeignKey`, `DropColumn`, `CreateIndex`), migration thực thi khối T-SQL kiểm tra dữ liệu:
     * Chặn nếu `MaTaiKhoan IS NULL AND TaiKhoanMaTaiKhoan IS NOT NULL` -> `THROW 51001` kèm danh sách Token ID.
     * Chặn nếu `MaTaiKhoan <> TaiKhoanMaTaiKhoan` (kể cả cả hai tài khoản đều tồn tại) -> `THROW 51002` kèm danh sách Token ID.
     * Chặn nếu `MaTaiKhoan` mồ côi (không tồn tại trong `dbo.TaiKhoan`) -> `THROW 51003` kèm danh sách Token ID.
     * Chặn nếu `TenDangNhap` trùng lặp trong `dbo.TaiKhoan` -> `THROW 51004` kèm danh sách username trùng lặp.
   - Toàn bộ khối lệnh chạy trong giao dịch của EF Core. Nếu gặp lỗi, lệnh ném exception làm dừng và rollback toàn bộ, bảo vệ DB ngay cả khi người dùng chạy trực tiếp `dotnet ef database update`.

---

### 10.2 Danh sách 6 Database Test Mới & Kết quả Thực thi

| Tên Database Test Mới | Mục đích & Tình huống Kiểm thử | Thao tác Thực hiện | Mã lỗi / Exit Code | Trạng thái Dữ liệu, Schema & History Sau Thất bại |
| :--- | :--- | :--- | :--- | :--- |
| **`HRM_Next_Test_T02_v2_Fresh`** | Fresh Install từ database trống từ đầu | `dotnet ef database update` | **Exit 0** (7 migration áp dụng thành công) | Đạt chuẩn schema: Unique Index tồn tại, FK trên `MaTaiKhoan` tồn tại, cột shadow `TaiKhoanMaTaiKhoan` không tồn tại, `Hwid` tồn tại trên `ChamCong`. |
| **`HRM_Next_Test_T02_v2_UpgradeValid`** | Upgrade từ snapshot baseline + mô phỏng 7 history + dữ liệu nghiệp vụ hợp lệ | `dotnet ef database update` | **Exit 0** (Áp dụng migration mới sạch sẽ) | Xem mục 10.3 bên dưới về đối soát từng bản ghi theo khóa chính. |
| **`HRM_Next_Test_T02_v2_Block_ShadowOnly`** | Token chỉ có shadow FK (`MaTaiKhoan=NULL, TaiKhoanMaTaiKhoan=1`, Token ID 20) | `dotnet ef database update` | **Exit 1** (SQL Error **51001**: *Token co TaiKhoanMaTaiKhoan nhung MaTaiKhoan bi NULL. Cac Token ID can xu ly: 20*) | **Không thay đổi**: `TaiKhoanMaTaiKhoan` còn nguyên, Token ID 20 còn nguyên, `__EFMigrationsHistory` không ghi nhận migration mới. |
| **`HRM_Next_Test_T02_v2_Block_DifferingIds`** | Token có hai ID khác nhau nhưng đều hợp lệ (`MaTaiKhoan=1, TaiKhoanMaTaiKhoan=2`, Token ID 30) | `dotnet ef database update` | **Exit 1** (SQL Error **51002**: *Token co MaTaiKhoan khac voi TaiKhoanMaTaiKhoan. Cac Token ID can xu ly: 30*) | **Không thay đổi**: Cột shadow còn nguyên, Token ID 30 còn nguyên, `__EFMigrationsHistory` không ghi nhận migration mới. |
| **`HRM_Next_Test_T02_v2_Block_OrphanToken`** | Token mồ côi (`MaTaiKhoan=99999`, Token ID 40) | `dotnet ef database update` | **Exit 1** (SQL Error **51003**: *Token co MaTaiKhoan mo coi khong ton tai trong dbo.TaiKhoan. Cac Token ID can xu ly: 40*) | **Không thay đổi**: Cột shadow còn nguyên, Token ID 40 còn nguyên, `__EFMigrationsHistory` không ghi nhận migration mới. |
| **`HRM_Next_Test_T02_v2_Block_DuplicateUser`** | Username trùng lặp (`duplicate_user`, 2 tài khoản) | `dotnet ef database update` | **Exit 1** (SQL Error **51004**: *Phat hien TenDangNhap trung lap trong dbo.TaiKhoan. Cac TenDangNhap can xu ly: duplicate_user (2 lan)*) | **Không thay đổi**: Unique index không được tạo, 2 tài khoản còn nguyên, `__EFMigrationsHistory` không ghi nhận migration mới. |

---

### 10.3 Đối soát Từng Bản ghi theo Khóa Chính (`HRM_Next_Test_T02_v2_UpgradeValid`)

Toàn bộ các bảng nghiệp vụ được truy vấn chi tiết từng dòng theo khóa chính trước và sau nâng cấp migration:

#### 1. Bảng `dbo.NhanVien` (10 bản ghi):
| MaNhanVien | HoTen | MucLuong Trước / Sau | NgayTao Trước / Sau | NgayCapNhat Trước / Sau | Kết quả Đối soát |
| :---: | :--- | :--- | :--- | :--- | :---: |
| 1 | Quản trị viên | 50,000,000.00 / 50,000,000.00 | 2026-05-25T18:29:36.0848326 / 2026-05-25T18:29:36.0848326 | 2026-05-25T18:29:36.0848342 / 2026-05-25T18:29:36.0848342 | **Khớp 100%** |
| 2..10 | Nhân viên 2..10 | 13,000,000 .. 25,000,000 | Giữ nguyên chính xác từng tick thời gian | Giữ nguyên chính xác từng tick thời gian | **Khớp 100%** |

#### 2. Bảng `dbo.BangLuong` (10 bản ghi):
| MaBangLuong | LuongCoBan Trước / Sau | TongPhuCap Trước / Sau | LuongThucNhan Trước / Sau | NgayTinhLuong Trước / Sau | Kết quả Đối soát |
| :---: | :--- | :--- | :--- | :--- | :---: |
| 1 | 50,000,000.00 / 50,000,000.00 | 1,000,000.00 / 1,000,000.00 | 45,250,000.00 / 45,250,000.00 | 2026-04-30 / 2026-04-30 | **Khớp 100%** |
| 2..10 | 13,000,000 .. 25,000,000 | 1,000,000.00 / 1,000,000.00 | 12,135,000 .. 22,875,000 | 2026-04-30 / 2026-04-30 | **Khớp 100%** |

#### 3. Bảng `dbo.ChamCong` (8 bản ghi):
| MaChamCong | TongGioLam Trước / Sau | GioLamThem Trước / Sau | Hwid Trước / Sau | Kết quả Đối soát |
| :---: | :--- | :--- | :--- | :---: |
| 1..4 | 8.00 / 8.00 | 0.00 / 0.00 | `NULL` / `NULL` | **Khớp 100%** |
| 5..8 | 8.00 .. 8.50 | 0.00 .. 0.50 | Giữ nguyên định dạng GUID (11111111.. .. 44444444..) | **Khớp 100%** |

#### 4. Bảng `dbo.TaiKhoan` (7 bản ghi - không xuất mật khẩu ra log/báo cáo):
| MaTaiKhoan | MaNhanVien | TenDangNhap | VaiTro | TrangThai | NgayTao Trước / Sau | Kết quả Đối soát |
| :---: | :---: | :--- | :--- | :--- | :--- | :---: |
| 1 | 1 | admin | Admin | Hoạt động | 2026-05-25T18:29:36.0679318 / 2026-05-25T18:29:36.0679318 | **Khớp 100%** |
| 2..7 | 2..7 | mock_user_* | HR / Manager / NhanVien / KeToan | Hoạt động | Giữ nguyên chính xác từng tick thời gian | **Khớp 100%** |

#### 5. Bảng `dbo.PasswordResetTokens` (1 bản ghi - không xuất chuỗi token ra log/báo cáo):
| Id | MaTaiKhoan | Purpose | CreatedAt Trước / Sau | ExpiresAt Trước / Sau | IsUsed | Kết quả Đối soát |
| :---: | :---: | :--- | :--- | :--- | :---: | :---: |
| 1 | 4 | ResetPassword | 2026-11-10T14:18:17.1790829 / 2026-11-10T14:18:17.1790829 | 2026-11-11T14:18:17.1790829 / 2026-11-11T14:18:17.1790829 | 0 / 0 | **Khớp 100%** |

---

### 10.4 Kiểm chứng Tính Bất biến khi Chạy lại (Idempotency)
- Chạy lại lệnh nâng cấp lần 2 trên `HRM_Next_Test_T02_v2_UpgradeValid`:
  ```powershell
  dotnet ef database update --project db/src/HRM.DAL/HRM.DAL.csproj --startup-project db/src/HRM.GUI/HRM.GUI.csproj --connection "Server=localhost;Database=HRM_Next_Test_T02_v2_UpgradeValid;Trusted_Connection=True;TrustServerCertificate=True;"
  ```
- Kết quả: **Exit code 0**. EF Core thông báo: *"No migrations were applied. The database is already up to date."*

---

### 10.5 Báo cáo Thực tế về Database `HRM_System` (Phân biệt Rõ ràng)
- **Phân biệt bản chất**: Dữ liệu kiểm chứng ở mục 10.2 và 10.3 được thực hiện trên **các fixture mô phỏng độc lập** (`HRM_Next_Test_T02_v5_*`), **hoàn toàn không phải snapshot từ `HRM_System`**.
- **Phạm vi đối chiếu trên `HRM_System`**: Chỉ thực hiện các lệnh truy vấn metadata và số liệu tổng hợp chỉ đọc (`SELECT COUNT(*)` và `SELECT MigrationId`).
- **Thao tác đã thực hiện**: **Không có bất kỳ thao tác ghi (INSERT/UPDATE/DELETE), thay đổi schema (DDL) hay lệnh migration nào được chạy lên cơ sở dữ liệu `HRM_System`.**
- **Kết quả đối chiếu lại sau toàn bộ quy trình**:
  * `dbo.__EFMigrationsHistory`: Đúng 7 dòng lịch sử như khi khảo sát ban đầu.
  * `dbo.NhanVien`: 10 dòng.
  * `dbo.TaiKhoan`: 7 dòng.
  * `dbo.ChamCong`: 8 dòng.
  * `dbo.BangLuong`: 10 dòng.
  * `dbo.PasswordResetTokens`: 1 dòng.

---

### 10.6 Tuyên Bố Phạm Vi Hỗ Trợ & Ranh Giới Kỹ Thuật

> [!IMPORTANT]
> **Phạm vi hỗ trợ đã được kiểm chứng**:
> - Các cơ sở dữ liệu có chuỗi schema dựng từ Entity Framework Core và migration history tương thích với khảo sát.
> - Các database có cấu trúc cột shadow `TaiKhoanMaTaiKhoan` và thiếu unique index trên `TenDangNhap`.
>
> **Ranh giới chưa kiểm chứng (UNVERIFIED)**:
> - Đường chuyển đổi trực tiếp từ database dựng thuần túy bằng file SQL script legacy (`db/docs/HRM_Database.sql`) **chưa được thực hiện và chưa được kiểm chứng**, vì script SQL legacy hoàn toàn thiếu các bảng/cột phát sinh sau này (`PasswordResetTokens`, `Hwid` trên `ChamCong`, kiểu dữ liệu khác biệt).
> - Fixture mô phỏng thêm dòng migration history trong database test **chỉ là mô phỏng lịch sử EF**, tuyệt đối **không được coi là kiểm thử thay thế** cho đường nâng cấp từ database tạo thuần túy bằng SQL legacy.

---

### 10.7 Ghi Chú Kỹ Thuật Về Phương Thức `Down()` Của Migration

> [!WARNING]
> Phương thức `Down()` của migration `20261110141626_FixLegacySchemaAndConstraints.cs`:
> - Khi rollback, `Down()` chỉ phục hồi **cấu trúc cột** (`migrationBuilder.AddColumn<int>("TaiKhoanMaTaiKhoan", ...)`), tái lập Foreign Key và xóa Unique Index trên `TenDangNhap`.
> - `Down()` **KHÔNG TỰ PHỤC HỒI các giá trị shadow data** đã bị xóa khi cột bị DROP ở phương thức `Up()`. Toàn bộ giá trị trong cột `TaiKhoanMaTaiKhoan` sau khi rollback bằng `Down()` sẽ nhận giá trị `NULL`.
> - Tuyệt đối **không được mô tả `Down()` là cơ chế rollback dữ liệu đầy đủ**. Nếu muốn khôi phục dữ liệu shadow, bắt buộc phải restore từ bản backup database trước khi chạy migration.

---

## 11. Bằng chứng Build & Kiểm thử Hồi quy Baseline

Toàn bộ giải pháp đã được build mới từ source hiện tại trước khi thực thi regression tests với cờ `--no-build`:

1. **Build Solution Release**:
   - Lệnh: `dotnet build db/src/HRM.sln --configuration Release`
   - **Exit code**: `0` (0 Error, 0 Warning).
2. **Build Test Project Release**:
   - Lệnh: `dotnet build backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --configuration Release`
   - **Exit code**: `0` (0 Error, 0 Warning).
3. **Bộ Kiểm thử Đơn vị Công cụ Python (Validator & T02 Verifier)**:
   - Lệnh: `python -m unittest discover -s scripts/tests`
   - **Exit code**: `0` (**30/30 PASS**: 18 test bộ điều phối validator + 12 test bộ đối soát và kiểm chứng T02 không phụ thuộc SQL Server).
4. **Bộ Điều phối Kiểm thử Baseline Hồi quy**:
   - Lệnh: `python scripts/run_and_verify_baseline.py --configuration Release`
   - **Exit code**: `0`.
   - **HarnessCheck**: 5/5 PASSED (100% xanh).
   - **RegressionBug**: 8/8 FAILED đúng nguyên nhân assertion dự kiến trong manifest (không sửa manifest để che sai lệch).
5. **Bộ Kiểm thử & Đối Soát Tự Động T02**:
   - Lệnh: `python scripts/verify_t02_execution.py HRM_Next_Test_T02_v6`
   - **Exit code**: `0` (6/6 kịch bản ĐẠT CHUẨN, bao gồm kiểm tra tính bảo toàn dữ liệu/schema sau khi rollback thất bại và đối soát hai chiều trước/sau lần chạy thứ hai của idempotency).
   - Chi tiết bằng chứng và bảng đối soát từng dòng: [t02-execution-evidence.md](file:///d:/dotnet%20project/docs/verification/t02-execution-evidence.md).
   - File log máy đọc: [t02_verification_log_HRM_Next_Test_T02_v6.json](file:///d:/dotnet%20project/docs/verification/runs/t02_verification_log_HRM_Next_Test_T02_v6.json).

---

## 12. Trạng thái Bàn giao & Cam kết Tuân thủ

- **Trạng thái nhiệm vụ T02**: **`READY_FOR_REVIEW`** (Chờ Chủ dự án kiểm duyệt; AI không tự ý chuyển thành `VERIFIED`).
- **Cam kết phạm vi**:
  * Không commit, không push git, bảo toàn nguyên vẹn working tree trên nhánh `task/T02`.
  * Không thay đổi cơ sở dữ liệu `HRM_System`.
  * Chưa bắt đầu nhiệm vụ T03.
