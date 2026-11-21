# Báo cáo Bằng chứng Thực thi Nhiệm vụ T03 (T03 Verification Evidence)

> **Dự án:** Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần  
> **Nhiệm vụ:** T03 — Khoanh vùng lỗi legacy và lập bản đồ chuyển nghiệp vụ  
> **Ngày thực hiện:** 11/11/2026  
> **Người thực hiện:** AI Assistant (Antigravity) | **Độc lập kiểm duyệt:** Codex / Chủ dự án  
> **Trạng thái hiện tại:** `VERIFIED` (Codex nghiệm thu theo ủy quyền trực tiếp của chủ dự án ngày 11/11/2026; xem [biên bản](../decisions/T03-signoff.md))  
> **Baseline Commit:** `05254b306f1f882b118575f50c5c81b486f40c6c` (Tag: `baseline-05254b3`)  
> **Nhánh Git:** `task/T02`  

**Kết quả hiện tại sau hoàn thiện T03:** Codex đã kiểm chứng **8/8 characterization PASS**, **5/5 HarnessCheck PASS**, **8 lỗi RegressionBug đúng manifest** và cập nhật graph code thành công. Đã bổ sung 2 test tracking F11, củng cố 6 test F10 bằng đọc lại sau khi clear tracker và sửa mô tả F11 theo source hiện tại. Chi tiết lệnh/TRX/giới hạn tại [T03-codex-checks.md](T03-codex-checks.md). Những số liệu 6 test và ghi chú chờ chạy ở các mục lịch sử dưới đây mô tả bản bàn giao Anti trước khi Codex hoàn thiện.

---

## 1. Mục tiêu và Tiêu chuẩn Nghiệm thu (Acceptance Checklist)

| Tiêu chuẩn Nghiệm thu (theo tasks/T03.md) | Trạng thái | Bằng chứng Thực tế / Vị trí Đối soát |
| :--- | :---: | :--- |
| **1. Mỗi F01–F14 có task chịu trách nhiệm; không có lỗi nào bị đánh dấu đã sửa chỉ vì được ghi tài liệu** | **PASS** | Bảng phân bổ chi tiết tại Mục 2 của [docs/legacy-transfer-map.md](file:///d:/dotnet%20project/docs/legacy-transfer-map.md). Từng lỗi từ F01 đến F14 đều được gán rõ task chịu trách nhiệm (T02, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T17, T22, P01, P02). |
| **2. Bảng mapping phân biệt rõ chưa sửa legacy, đã sửa backend mới và chưa kiểm chứng** | **PASS** | [docs/legacy-transfer-map.md](file:///d:/dotnet%20project/docs/legacy-transfer-map.md) sử dụng hệ thống nhãn nhất quán: `LEGACY_CHƯA_SỬA`, `SCHEMA_BASELINE_EF_ĐÃ_KIỂM_CHỨNG (T02)` (chỉ áp dụng cho tầng schema EF baseline T02, không phải backend nghiệp vụ mới), và `CHƯA_KIỂM_CHỨNG (UNVERIFIED)` (đối với đường SQL legacy thuần túy `HRM_Database.sql`). |
| **3. Cảnh báo an toàn dữ liệu: luồng reset và tính lại lương legacy không được dùng với dữ liệu thật** | **PASS** | Tuyên bố rủi ro nổi bật tại Mục 1.2 của [docs/legacy-transfer-map.md](file:///d:/dotnet%20project/docs/legacy-transfer-map.md), chỉ rõ `TinhVaLuuBangLuongThangAsync` xóa trắng dữ liệu kỳ cũ trước khi tính mới mà không có transaction; `ResetPasswordWithTokenAsync` không kiểm tra purpose. Ghi rõ bắt buộc phải re-scope/hotfix nếu dùng thật. |
| **4. Characterization tests cho mapping và partial update gọi code thực, phân biệt vắng/null** | **PASS (Mã nguồn)**<br>*Chờ Codex chạy test* | File kiểm thử [F10_PartialUpdateCharacterizationTests.cs](file:///d:/dotnet%20project/backend/tests/HRM.Tests.Regression/Characterization/F10_PartialUpdateCharacterizationTests.cs) gồm **đủ 6 test case** (5 test service thực thi + 1 test ngữ nghĩa POCO DTO), gọi trực tiếp `NhanVienService.UpdateAsync`, `GetByIdAsync` và `NhanVienCreateDTO`. Khắc phục triệt để lỗi compile CS1729 ban đầu bằng cách dùng đúng constructor 3 tham số. |
| **5. Không mở rộng ngoài phạm vi, không sang T04/T05, không sửa migration/schema T02, không sửa DB** | **PASS** | Toàn bộ migration và schema giữ nguyên trạng; không kết nối SQL Server; không tạo backend T05; không commit, không push. |

---

## 2. Danh mục File Thay đổi (Files Changed)

| STT | Đường dẫn File | Loại thay đổi | Mục đích & Nội dung |
| :---: | :--- | :---: | :--- |
| 1 | `docs/legacy-transfer-map.md` | **Tạo mới / Cập nhật** | Bản đồ chuyển giao 6 nhóm nghiệp vụ: DTO nhân viên, tracking, lương, công, phép, tuyển dụng; phân bổ trách nhiệm và test cho F01–F14; cập nhật đường dẫn migration F01 chính xác (`20260502105319`), phân biệt snapshot cũ và hiện trạng audit T02; định nghĩa đúng phạm vi schema EF baseline cho F01/F08. |
| 2 | `backend/tests/HRM.Tests.Regression/Characterization/F10_PartialUpdateCharacterizationTests.cs` | **Tạo mới / Cập nhật** | Bộ kiểm thử đặc trưng hành vi gồm **6 tests**: sửa constructor về 3 tham số (`repo`, `taiKhoan`, `bangLuong`), bổ sung test service thực cho trường chủ ý gán null (Test 5), và test đọc `GetByIdAsync` xác nhận `MapToDTO` giữ nguyên địa chỉ/hôn nhân cùng tên phòng ban/chức vụ (Test 6). |
| 3 | `docs/verification/T03-transfer-map-evidence.md` | **Tạo mới / Cập nhật** | Báo cáo bằng chứng thực thi, ghi nhận lỗi compile CS1729 ban đầu, dẫn kết quả kiểm tra độc lập của Codex, ghi nhận hạn chế runner và đặt trạng thái bàn giao `IN_PROGRESS`. |
| 4 | `HRM_KeHoach_DoAn_8Tuan/tasks/T03.md` | **Cập nhật** | Đặt trạng thái thẻ nhiệm vụ là `IN_PROGRESS (Chờ Codex xác minh test và cập nhật Graphify)`. |
| 5 | `HRM_KeHoach_DoAn_8Tuan/03_BACKLOG.md` | **Cập nhật** | Cập nhật bảng backlog Markdown cho T03 sang `IN_PROGRESS (Chờ Codex)`. |
| 6 | `HRM_KeHoach_DoAn_8Tuan/backlog.json` | **Cập nhật** | Cập nhật trường `status` của task T03 trong file JSON máy đọc sang `IN_PROGRESS`. |

---

## 3. Ghi nhận Lỗi Compile CS1729 Ban đầu & Bản Sửa Kỹ thuật

- **Hiện tượng:**
  Trong bản nháp ban đầu của file `F10_PartialUpdateCharacterizationTests.cs`, Antigravity đã khởi tạo `NhanVienService` với 5 tham số:
  ```csharp
  // LỖI CS1729 BAN ĐẦU:
  var service = new NhanVienService(repo, mockTaiKhoan.Object, mockBangLuong.Object, mockChamCong.Object, mockDonNghiPhep.Object);
  ```
  Codex trong chat đã trực tiếp build và phát hiện lỗi biên dịch:
  `CS1729: 'NhanVienService' does not contain a constructor that takes 5 arguments.`
- **Nguyên nhân:**
  Mã nguồn thực tế tại [`db/src/HRM.BLL/Services/NhanVienService.cs:15-23`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/NhanVienService.cs#L15-L23) chỉ có constructor nhận 3 tham số:
  ```csharp
  public NhanVienService(
      INhanVienRepository repo,
      ITaiKhoanService taiKhoanService,
      IBangLuongService bangLuongService)
  ```
- **Bản sửa:**
  Đã loại bỏ hoàn toàn 2 mock repository thừa (`mockChamCong`, `mockDonNghiPhep`) và điều chỉnh toàn bộ các điểm khởi tạo `NhanVienService` trong test về đúng 3 tham số chuẩn. Tuyệt đối **không sửa đổi service sản phẩm** để chiều theo test.

---

## 4. Chi tiết Bộ Kiểm thử Đặc trưng Hành vi (Characterization Tests F10 — Đủ 6 Tests)

File mã nguồn: [`backend/tests/HRM.Tests.Regression/Characterization/F10_PartialUpdateCharacterizationTests.cs`](file:///d:/dotnet%20project/backend/tests/HRM.Tests.Regression/Characterization/F10_PartialUpdateCharacterizationTests.cs)

| STT | Phương thức Test | Phân loại | Hành vi Được Chứng minh & Ý nghĩa Nghiệp vụ |
| :---: | :--- | :---: | :--- |
| **1** | `UpdateAsync_WhenOptionalFieldsOmittedInDto_DestructivelyOverwritesExistingDataWithNull` | Service thực | Khi client chỉ sửa Họ tên & SĐT (không truyền `DiaChi` và `TinhTrangHonNhan`), service legacy tự động ghi đè `entity.DiaChi = null` và `entity.TinhTrangHonNhan = null`. **Tái hiện chính xác Bug F10.** |
| **2** | `DtoSemantics_CannotDistinguish_BetweenOmittedField_AndIntentionalNullDeletion` | Ngữ nghĩa POCO | So sánh hai đối tượng `NhanVienCreateDTO`: một đối tượng không gán trường (omitted), một đối tượng cố ý gán `null`. Cả hai đều có giá trị thuộc tính là `null`. **Chứng minh giới hạn kiến trúc của DTO legacy.** |
| **3** | `UpdateAsync_WhenValueTypesOmittedInDto_CorruptsEntityWithClrDefaults` | Service thực | Khi client bỏ qua các trường non-nullable value type (`NgaySinh`, `MucLuong`), DTO nhận giá trị CLR default (`DateTime.MinValue`, `0m`), làm hỏng dữ liệu DB sau khi update. |
| **4** | `UpdateAsync_WhenSalaryModified_InadvertentlyTriggersHistoricalPayrollSync_F02` | Service thực | Khi sửa mức lương nhân viên, `UpdateAsync` tự động gọi `DongBoBangLuongTheoNhanVienAsync(id)`, gây tác dụng phụ làm biến động số liệu các bảng lương đã khóa trong quá khứ (**F02**). |
| **5** | `UpdateAsync_WhenOptionalFieldsExplicitlySetToNull_PersistsNullInDatabase` | Service thực *(Mới)* | Client cố ý gán `DiaChi = null` và `TinhTrangHonNhan = null` để xóa; service thực thi lưu `null` vào DB. Chứng minh service xử lý Ca 5 (chủ ý xóa) và Ca 1 (vắng mặt ngoài ý muốn) hoàn toàn giống hệt nhau. |
| **6** | `GetByIdAsync_WhenEmployeeHasFullDetailsAndRelations_PreservesAddressMaritalStatusAndNavNames` | Service thực *(Mới)* | Đọc nhân viên qua `GetByIdAsync`, xác nhận `MapToDTO` giữ nguyên vẹn `DiaChi`, `TinhTrangHonNhan` và lấy đúng `TenPhongBan`, `TenChucVu` từ navigation properties. Chứng minh tầng đọc không bị lỗi, lỗi rơi trường phát sinh tại tầng ghi `UpdateAsync`. |

---

## 5. Bằng chứng Kiểm tra Độc lập của Codex và Tình trạng Thực thi

### 5.1. Bằng chứng Baseline Độc lập của Codex (từ `docs/verification/T03-codex-checks.md`)
Codex trong chat đã trực tiếp chạy lệnh:
```powershell
dotnet test backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore --filter Category=HarnessCheck --logger trx --results-directory docs/verification/runs/T03-codex-baseline
```
- **Kết quả:** Exit code `0`. Passed: `5`, Failed: `0`, Skipped: `0`, Total: `5`.
- **TRX File:** `docs/verification/runs/T03-codex-baseline/nguye_MSI_2026-11-11_12_18_17.trx`.
- **Lưu ý ranh giới:** Kết quả này chỉ kiểm chứng bộ test `HarnessCheck` baseline, không phải là kết quả chạy của 6 test characterization T03 mới.

### 5.2. Tình trạng Thực thi trong Môi trường Anti
- Tiến trình VSTest chạy kiểm thử trong môi trường runner Anti bị timeout kết nối sau 90 giây (`vstest.console process failed to connect to testhost process after 90 seconds`).
- Theo chỉ thị của Chủ dự án, Antigravity **không chạy lại vô hạn hoặc giả định nguyên nhân hạ tầng đã được chứng minh**, mà bàn giao toàn bộ mã nguồn test sạch lỗi compile để Codex trong chat chạy kiểm thử độc lập ngay sau bàn giao.
- Tiến trình `graphify update .` chạy nền trong Anti chưa hoàn tất; ghi nhận chính xác là **đang chờ Codex chạy cập nhật Graphify trực tiếp ngoài chat**, tuyệt đối không giả vờ đã cập nhật xong.

---

## 6. Ranh giới Kỹ thuật Chưa Kiểm chứng (Unverified)

1. **Ranh giới SQL Legacy Thuần túy (`HRM_Database.sql`):** Tiếp tục ở trạng thái **`UNVERIFIED`** theo Biên bản nghiệm thu T02 ([T02-signoff.md](file:///d:/dotnet%20project/docs/decisions/T02-signoff.md)), được quản lý tại hạng mục riêng `T02-LEGACY-SQL` (Tuần 6).
2. **Backend Nghiệp vụ Mới Chưa Triển khai:** Việc hoàn thành bản đồ chuyển giao T03 không có nghĩa là backend mới đã được xây dựng. Các use case mới sẽ được triển khai tuần tự từ T05 đến T15.
3. **Mã nguồn Sản phẩm Giữ nguyên:** Toàn bộ thư mục `db/src` được giữ nguyên vẹn 100%.

---

## 7. Kế hoạch Bàn giao & Các Bước Tiếp theo của Codex

1. **Trạng thái Bàn giao:** Nhiệm vụ T03 hiện ở trạng thái **`IN_PROGRESS`**.
2. **Thao tác Tiếp theo của Codex trong chat:**
   - Thực thi build kiểm tra:
     ```powershell
     dotnet build backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore
     ```
   - Chạy 6 test đặc trưng hành vi F10:
     ```powershell
     dotnet test backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore --filter Category=Characterization
     ```
   - Chạy cập nhật Graphify trực tiếp:
     ```powershell
     graphify update .
     ```
   - Codex đã chạy các bước này và chuyển T03 sang `READY_FOR_REVIEW`. Chỉ chủ dự án được nghiệm thu `VERIFIED`.

## 7. Kết quả kiểm chứng cuối của Codex trong chat (11/11/2026)

- `dotnet test ... --no-restore --filter Category=Characterization --logger trx --results-directory docs/verification/runs/T03-codex-final`: exit **0**, **6/6 PASS**, không skip. Lệnh này đã build thành công source test cuối. TRX và lệnh đầy đủ tại `T03-codex-checks.md`.
- `python -B scripts/run_and_verify_baseline.py --configuration Debug --results-dir docs/verification/runs/T03-codex-regression-final`: exit **0** của bộ thẩm định; **5/5 HarnessCheck PASS**, **8/8 RegressionBug FAIL đúng assertion trong manifest**. Không có lỗi bất ngờ. Không mô tả đây là mọi test xanh hoặc các lỗi legacy đã sửa.
- `graphify update .`: exit **0**, graph code được cập nhật (3377 nodes, 6200 edges). Lệnh này cập nhật graph code; không khẳng định semantic extraction mới cho toàn bộ Markdown.
- Anti CLI hết thời gian ở lượt sửa cuối; trạng thái bridge là `ERROR_NEEDS_USER`, không phải ACCEPT của CLI reviewer. Trạng thái T03 hiện tại dựa trên review và lệnh thực chạy của Codex trong chat này.
