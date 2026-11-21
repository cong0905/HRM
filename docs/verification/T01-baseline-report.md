# Báo cáo Kiểm thử Hồi quy và Đóng băng Baseline (T01)

- **Mã nhiệm vụ**: T01 — Đóng băng baseline và dựng kiểm thử hồi quy
- **Trạng thái**: **VERIFIED** (Chủ dự án đã nghiệm thu thực tế ngày 10/11/2026)
- **Ngày cập nhật**: 10/11/2026 (Phiên bản v2.1: Đồng bộ Release configuration, Default-Deny tuyệt đối, 13 tests)
- **Nhánh làm việc**: `task/T01`
- **Baseline Git Tag**: `baseline-05254b3` tại commit `05254b306f1f882b118575f50c5c81b486f40c6c`
- **Mục tiêu**: Đóng băng hiện trạng, bảo toàn tài liệu và mã nguồn, thiết lập khung kiểm thử độc lập, cơ chế điều phối tự động ghi nhận exit code và bộ thẩm định manifest theo nguyên tắc Mặc định từ chối (Default-Deny).

---

## 1. Môi trường Thực thi & Đối chiếu Git

### 1.1 Thông tin Git & Đối chiếu Thực tế
- **Commit Local Baseline**: `05254b306f1f882b118575f50c5c81b486f40c6c`
- **Git Tag bảo toàn**: `baseline-05254b3` tại HEAD của commit local.
- **Nhánh làm việc**: `task/T01`.
- **Hiện trạng Working Tree**: Các thư mục và file mới (`.github/`, `backend/`, `docs/`, `scripts/`, `HRM_KeHoach_DoAn_8Tuan/`, `graphify-out/`) hiện ở trạng thái **untracked** trong working tree. Chưa thực hiện `git commit` hay `git push` để giữ nguyên vẹn lịch sử Git gốc.
- **Đối chiếu Remote (`origin/main`)**:
  * Remote ref `origin/main` hiện trỏ tới commit `8b1236573f99c28f4cd486831cb30635a01bd061` (trước 9 commit so với local baseline `05254b3`).
  * Tuân thủ cam kết: **Không pull, merge, rebase, reset hoặc push**.

### 1.2 Môi trường SDK & Runtime
- **.NET SDK đang sử dụng**: `9.0.304`
- **Python Runtime**: `3.12.9`
- **Runtimes .NET đã cài đặt**:
  * `Microsoft.AspNetCore.App 8.0.19` và `9.0.8`
  * `Microsoft.NETCore.App 8.0.19` và `9.0.8`
  * `Microsoft.WindowsDesktop.App 8.0.19` và `9.0.8`

### 1.3 Phân định Rõ ràng giữa Legacy và Backend Mới
- **Phạm vi kiểm thử hiện tại**: Bộ test hồi quy `HRM.Tests.Regression` tham chiếu **trực tiếp các assembly của legacy code** (`HRM.BLL`, `HRM.DAL`, `HRM.Domain`, `HRM.Common` trong `db/src/`).
- **Ranh giới tiến hóa**: Việc sửa lỗi hoặc thiết kế lại nghiệp vụ trong Backend mới (.NET 10 LTS) ở các task tiếp theo **sẽ không tự động làm các test hồi quy của legacy xanh**.
- **Kế hoạch kiểm thử backend mới**: Khi chuyển đổi từng use case ở các task sau (T03, T06, T11, T13, T14), sẽ xây dựng bộ test nghiệm thu (acceptance tests) riêng cho backend mới, kèm bảng ánh xạ chi tiết: `Mã Finding -> Triển khai Backend mới -> Test nghiệm thu`.

---

## 2. Kết quả Build & Quét Lỗ hổng Thư viện

### 2.1 Build Solution & Regression Suite với Cấu hình `Release`
- **Đồng bộ cấu hình**: CI workflow và script điều phối cục bộ đều thực thi đồng bộ trên cấu hình **`Release`**.
- **Lệnh thực hiện**:
  * `dotnet build db/src/HRM.sln --no-restore --configuration Release`
  * `dotnet build backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --configuration Release`
- **Exit code**: `0` (Build Succeeded).
- **Mã nguồn sản phẩm (`db/src`)**: Giữ nguyên trạng thái gốc 100%, không thay đổi.

### 2.2 Quét Lỗ hổng Thư viện (Vulnerability Scan)
- **Lệnh thực hiện**: `dotnet list "db/src/HRM.sln" package --vulnerable --include-transitive`
- **Exit code**: `0`
- **Phát hiện**: Gói `System.Text.Json 8.0.0` có **2 cảnh báo bảo mật mức độ High**: `GHSA-hh2w-p6rv-4g7w` và `GHSA-8g4q-xg66-9fp4`. Hướng xử lý: nâng cấp trong task hạ tầng T04.

---

## 3. Khám phá Kiến trúc Mã nguồn bằng Graphify

- **Lệnh thực thi**: `python -m graphify update "d:\dotnet project"`
- **Exit code**: `0`
- **Số liệu đồ thị tri thức**: **2,763 nodes**, **5,089 edges**, **185 communities**. Lưu tại [graphify-out/graph.json](file:///D:/dotnet%20project/graphify-out/graph.json).

---

## 4. Dự án Kiểm thử Hồi quy `HRM.Tests.Regression`

### 4.1 Cấu trúc & Nguyên tắc An toàn
- Vị trí: `backend/tests/HRM.Tests.Regression/` (Target: `net8.0`).
- Tham chiếu: `HRM.BLL`, `HRM.DAL`, `HRM.Domain`, `HRM.Common`.
- Sử dụng EF Core `UseInMemoryDatabase` và Moq fake repositories. Tuyệt đối **không kết nối SQL Server thật**, **không gửi SMTP thật**, và **không chạy HRM.GUI**.

### 4.2 Bộ Test Khung (Category: `HarnessCheck`) — KỲ VỌNG: PASS (5/5)
Xác minh môi trường test, in-memory DbContext, mock service và năng lực sinh schema offline độc lập của EF Core:
- **Số lượng test**: **5 tests**.
- **Exit code**: `0`.
- **Kết quả**: **5 Passed, 0 Failed, 0 Skipped (100% XANH)**.
  1. `HarnessCheck_InMemoryDbContext_CanPersistAndRetrieveData`: EF Core In-Memory lưu và truy vấn Entity bình thường.
  2. `HarnessCheck_HieuSuatService_WithKnownScores_ReturnsExpectedBonus`: Kiểm tra logic phân cấp điểm hiệu suất (96 -> 0.25, 86 -> 0.15, 55 -> 0.00).
  3. `HarnessCheck_AuthService_LoginAsync_WhenUserNotFound_ReturnsNull`: Tên đăng nhập không tồn tại trả về `null`.
  4. `HarnessCheck_AuthService_LoginAsync_WhenPasswordIncorrect_ReturnsNull`: Người dùng tồn tại nhưng mật khẩu sai trả về `null`.
  5. `EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong`: Xác minh độc lập năng lực sinh schema offline của EF Core `IMigrator.GenerateScript(null, null)` tạo lệnh `ALTER TABLE [ChamCong] ADD [Hwid]` trên bảng `ChamCong` mà không bị phụ thuộc vào tên migration history cụ thể.

---

## 5. Bằng chứng Thực nghiệm Lỗi Baseline (Category: `RegressionBug`) — KỲ VỌNG: FAIL (8/8)

Bộ test gồm **8 test cases** độc lập bao phủ 5 nhóm phát hiện (F01, F02, F04, F05, F06). Tất cả các test đều assert theo **nghiệp vụ đúng**. Khi chạy trên mã nguồn baseline chưa sửa, cả 8 test đều thất bại chính xác do các lỗi legacy:
- **Số lượng test**: **8 tests**.
- **Exit code**: `1` (Bắt buộc exit code 1; runner trả mã thoát khác 1 sẽ bị điều phối từ chối ngay lập tức).
- **Kết quả**: **0 Passed, 8 Failed, 0 Skipped (100% RED)**.

### Bảng Ma trận Ánh xạ 5 Phát hiện sang 8 Test Cases Thất bại Được phép

| Mã Finding | Tên Test Case Đầy đủ | Fixture Đầu vào | Kỳ vọng Nghiệp vụ Đúng | Kết quả Thực tế Baseline | Dấu hiệu Assertion Đặc thù (Manifest Substrings) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **F01.1 (Metadata)** | `HRM.Tests.Regression.Bugs.F01_MigrationHwidTests.AllMigrationClasses_MustHaveValidMigrationAttribute` | Quét assembly `HRM.DAL` tìm class kế thừa `Migration`. | Mọi migration class đều phải có `[MigrationAttribute]`. | `AddHwidToChamCong` thiếu attribute (EF discovery bỏ qua migration). | `LỖ HỔNG F01 (Metadata): Migration class 'AddHwidToChamCong' thiếu [MigrationAttribute]` |
| **F01.3 (Operations)** | `HRM.Tests.Regression.Bugs.F01_MigrationHwidTests.AddHwidToChamCong_MigrationOperations_MustContainAddHwidColumn` | Phân tích các thao tác trong phương thức `Up()` của class `AddHwidToChamCong`. | Migration `AddHwidToChamCong` phải chứa `AddColumnOperation` cho cột `Hwid` trên bảng `ChamCong`. | Phương thức `Up()` chỉ cập nhật seed data `NhanVien`/`PhongBan`, hoàn toàn thiếu thao tác thêm cột Hwid. | `LỖ HỔNG F01 (Operations): Migration 'AddHwidToChamCong' không có thao tác AddColumn('Hwid') trên bảng 'ChamCong'!` |
| **F01.4 (SQL Tài liệu)** | `HRM.Tests.Regression.Bugs.F01_MigrationHwidTests.DatabaseDocumentationScript_MustDefineHwidColumnOnChamCongTable` | Đọc file script tài liệu `db/docs/HRM_Database.sql`. | Khối `CREATE TABLE ChamCong` phải có định nghĩa cột `Hwid`. | File script tài liệu không hề định nghĩa cột `Hwid` trong bảng `ChamCong`. | `LỖ HỔNG F01 (SQL Tài liệu): Cột 'Hwid' hoàn toàn không có trong định nghĩa bảng ChamCong của file script tài liệu db/docs/HRM_Database.sql!` |
| **F02** | `HRM.Tests.Regression.Bugs.F02_SalaryHistoryOverwriteTests.DongBoBangLuongTheoNhanVien_WhenSalaryUpdated_MustNotOverwritePastPayrollRecords` | Bảng lương Tháng 1/2026 có `LuongCoBan = 10.000.000m`. Nhân viên tăng lương lên `15.000.000m` vào Tháng 5. | Bảng lương quá khứ của kỳ đã đóng là bất biến (`10.000.000m`). | `BangLuongService.DongBoBangLuongTheoNhanVienAsync` cập nhật đè lương Tháng 1 thành `15.000.000m`. | `Assert.Equal() Failure`, `Expected: 10000000`, `Actual:   15000000` |
| **F04** | `HRM.Tests.Regression.Bugs.F04_NullKpiPenaltyTests.TinhBonusHieuSuat_WhenDiemTongIsNull_MustNotPenalizeNegativeTenPercent` | Điểm tổng hiệu suất `diemTong = null` (nhân viên chưa đánh giá). | Chưa đánh giá thì không phạt; hệ số là `0.0m`. | `HieuSuatService.TinhBonusHieuSuat(null)` mặc định null thành `0m` và phạt `-0.10m`. | `Assert.Equal() Failure`, `Expected: 0.0`, `Actual:   -0.10` |
| **F05 (Ngoại lệ)** | `HRM.Tests.Regression.Bugs.F05_LeaveBalanceOverdrawnTests.ApproveLeave_WhenDaysRequestedExceedsRemainingBalance_MustThrowInvalidOperationException` | Quỹ còn 1 ngày (`Tong=12, DaDung=11`). Đơn xin nghỉ 3 ngày phép năm. | Bắt buộc ném `InvalidOperationException` từ chối duyệt. | `DonNghiPhepService.ApproveAsync` duyệt trơn tru không ném lỗi. | `Assert.Throws() Failure: No exception was thrown`, `Expected: typeof(System.InvalidOperationException)` |
| **F05 (Hậu quả)** | `HRM.Tests.Regression.Bugs.F05_LeaveBalanceOverdrawnTests.ApproveLeave_WhenDaysRequestedExceedsRemainingBalance_MustNotMutateStatusAndMustNotDeductBalance` | Quỹ còn 1 ngày, duyệt đơn 3 ngày. | Đơn không thành `DaDuyet`, quỹ không âm, không gọi repo update trái phép. | Đơn bị đổi thành `DaDuyet`, quỹ thành `-2`, gọi repo update. | `Assert.NotEqual() Failure: Strings are equal`, `Expected: Not "Đã duyệt"`, `Actual:       "Đã duyệt"` |
| **F06** | `HRM.Tests.Regression.Bugs.F06_TokenPurposeTests.ResetPasswordWithToken_WhenTokenPurposeIsVerifyEmail_MustBeRejectedAndNotMutatePassword` | Token có `Purpose = "VerifyEmail"`, tài khoản có mật khẩu hash ban đầu. | Trả về `false`, không đổi hash mật khẩu, không gọi update repo. | `AuthService.ResetPasswordWithTokenAsync` trả về `true`, ghi đè hash mật khẩu mới, gọi update. | `LỖ HỔNG F06: Hệ thống chấp nhận token VerifyEmail để reset mật khẩu! Kỳ vọng phải trả về false.` |

*Ghi chú kỹ thuật về F05*: Test F05 kiểm tra logic nghiệp vụ tầng dịch vụ và các lời gọi repository thông qua mock; test không tuyên bố kiểm thử đơn vị với fake repository đã chứng minh hành vi transaction rollback hay concurrency ở tầng SQL Server.

---

## 6. Script Điều phối Thực thi & Bộ Thẩm định Default-Deny

### 6.1 Script Điều phối Thực thi (`scripts/run_and_verify_baseline.py`)
- **Tạo thư mục kết quả riêng bằng UUID**: Mỗi lượt chạy tự động sinh thư mục độc lập `docs/verification/runs/run_<timestamp>_<uuid>/`, từ chối tái sử dụng thư mục đã có file `.trx`.
- **Đồng bộ Build Configuration**: Nhận tham số `--configuration` (mặc định: `Release`), truyền đồng bộ vào cả hai lệnh `dotnet test` (Harness và Regression).
- **Kiểm soát Exit Code chặt chẽ**:
  * Bước 1 (HarnessCheck): Ghi nhận exit code ngay lập tức; bắt buộc bằng `0`.
  * Bước 2 (RegressionBug): Ghi nhận exit code ngay lập tức; bắt buộc bằng `1`. Nếu runner trả exit code khác 1 (ví dụ 0 do nuốt lỗi hoặc 2 do crash), script lập tức dừng và báo lỗi.
- **Thực thi Validator**: Gọi trực tiếp bộ thẩm định `verify_baseline_trx` theo nguyên tắc Default-Deny.
- **Lưu vết Evidence trên mọi nhánh**: Lưu `run-evidence.json` vào thư mục run và cập nhật `latest-run-evidence.json` trên cả nhánh thành công lẫn nhánh thất bại.

### 6.2 Bộ Thẩm định Mặc định Từ chối (`scripts/verify_baseline_trx.py`)
Được thiết kế theo nguyên tắc **Default-Deny tuyệt đối**:
1. **Kiểm tra Manifest**: Bắt buộc khai báo cả `expected_harness_tests` (5 test) và `expected_regression_tests` (8 test). Yêu cầu mọi substring nhận diện lỗi phải là chuỗi không rỗng.
2. **Kiểm tra ResultSummary & Counters**: Bắt buộc file TRX phải có thẻ `ResultSummary` và `Counters` hợp lệ với `total > 0`. Phát hiện lỗi hạ tầng runner (`Aborted`, `Error`, `Timeout`) lập tức từ chối.
3. **Kiểm tra Trùng lặp**: Bắt trùng lặp `testName` trong TRX trước khi chuyển sang cấu trúc dữ liệu dict.
4. **Quy tắc Outcome Default-Deny**:
   * Harness: Chỉ chấp nhận `Passed`. Outcome khác `Passed` -> Từ chối ngay.
   * Regression: Bất kỳ outcome nào **khác `Failed`** (bao gồm `UnexpectedOutcome`, `Passed`, `Skipped`, `Timeout`, `Aborted`, `Inconclusive`) đều bị từ chối ngay lập tức.
5. **Kiểm tra Khớp Dấu hiệu Đặc thù**: Bắt buộc thông điệp lỗi thực tế phải chứa đầy đủ mọi chuỗi đặc thù trong `expected_error_substrings`.

### 6.3 Kiểm thử Đơn vị cho Bộ Thẩm định (`scripts/tests/test_verify_baseline_trx.py`)
Đã xây dựng bộ unit test gồm **14 test cases** kiểm chứng toàn diện nguyên tắc Default-Deny:
- Lệnh chạy: `python -m unittest scripts/tests/test_verify_baseline_trx.py`
- **Exit code**: `0` (`Ran 14 tests in 0.038s. OK`).
- **Danh sách 14 kịch bản đã kiểm chứng**:
  1. `test_exact_manifest_match_passes`: Đúng manifest 100% -> PASS (exit 0).
  2. `test_missing_regression_test_fails`: Regression thiếu test -> FAIL (exit 1).
  3. `test_different_error_cause_fails`: Lỗi do NullReferenceException -> FAIL (exit 1).
  4. `test_skipped_test_fails`: Có test bị skipped -> FAIL (exit 1).
  5. `test_missing_trx_fails`: File TRX không tồn tại -> FAIL (exit 1).
  6. `test_unexpected_pass_fails`: Test bất ngờ PASS -> FAIL (exit 1).
  7. `test_harness_missing_one_test_fails`: Kịch bản (a) - Harness thiếu test -> FAIL (exit 1).
  8. `test_outcome_timeout_or_unrecognized_fails`: Kịch bản (b) - Outcome Timeout/Aborted -> FAIL (exit 1).
  9. `test_run_summary_aborted_fails_even_if_subtests_match`: Kịch bản (c) - Run summary Aborted -> FAIL (exit 1).
  10. `test_duplicate_results_in_trx_fails`: Kịch bản (d) - Trùng lặp testName trong TRX -> FAIL (exit 1).
  11. `test_invalid_manifest_duplicate_fails`: Kịch bản (e) - Manifest trùng lặp hoặc không hợp lệ -> FAIL (exit 1).
  12. `test_assertion_same_type_but_wrong_value_fails`: Kịch bản (f) - Cùng loại Assert.Equal nhưng sai actual value -> FAIL (exit 1).
  13. `test_unexpected_outcome_string_fails`: Outcome là chuỗi lạ `'UnexpectedOutcome'` -> BỊ CHẶN (exit 1).
  14. `test_missing_summary_or_counters_fails`: TRX thiếu `ResultSummary` hoặc `Counters` -> BỊ CHẶN (exit 1).

### 6.4 File Cấu hình CI Workflow (`.github/workflows/ci.yml`)
- Đã đồng bộ [.github/workflows/ci.yml](file:///D:/dotnet%20project/.github/workflows/ci.yml).
- Tích hợp chạy unit tests cho validator trước (`14/14 tests PASS`).
- Bước kiểm thử chính gọi script điều phối:
  `python scripts/run_and_verify_baseline.py --configuration Release --test-project backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --manifest docs/verification/baseline-allowed-failures.json`
- Artifacts (TRX, logs, evidence JSON) luôn được upload kể cả khi pipeline thất bại (`if: always()`).
- **Trạng thái thực tế**: Workflow file đã kiểm tra cục bộ; **chưa chạy trên GitHub Actions từ xa** do chưa thực hiện `git push`.

---

## 7. Bằng chứng Chạy Thực nghiệm của Lượt Chạy Mới Nhất

Lượt chạy điều phối thực nghiệm với cấu hình `Release`:
- **Lệnh thực thi**: `python scripts/run_and_verify_baseline.py --configuration Release`
- **Exit code**: `0`
- **Thư mục kết quả độc lập**: [docs/verification/runs/run_20261110_172646_570a9fec/](file:///D:/dotnet%20project/docs/verification/runs/run_20261110_172646_570a9fec/)
- **Chi tiết evidence**: [docs/verification/latest-run-evidence.json](file:///D:/dotnet%20project/docs/verification/latest-run-evidence.json)
- **Báo cáo TRX**:
  * [test-harness.trx](file:///D:/dotnet%20project/docs/verification/runs/run_20261110_172646_570a9fec/test-harness.trx): **5 tests Passed, 0 Failed, 0 Skipped** (Exit code: 0).
  * [test-regression-baseline.trx](file:///D:/dotnet%20project/docs/verification/runs/run_20261110_172646_570a9fec/test-regression-baseline.trx): **8 tests Failed đúng assertion, 0 Passed, 0 Skipped** (Exit code: 1).
- **Kết quả Thẩm định Manifest**: Exit code: 0 (`SUCCESS_BASELINE_MATCHED`).

---

## 8. Hướng dẫn Chủ Dự án Tái hiện Độc lập (10–15 phút)

Chủ dự án có thể mở PowerShell tại thư mục `D:\dotnet project` và thực hiện tuần tự để kiểm chứng độc lập trên môi trường Release mà không phụ thuộc `bin/Debug` có sẵn:

```powershell
# 1. Kiểm tra trạng thái Git và mã nguồn sản phẩm (db/src không đổi)
git status
git tag -l "baseline-*"

# 2. Build Release đồng bộ
dotnet build db/src/HRM.sln --no-restore --configuration Release
dotnet build backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --configuration Release

# 3. Chạy 18 unit tests kiểm thử bộ thẩm định và điều phối (Bắt buộc PASS 18/18)
python -m unittest discover -s scripts/tests

# 4. Chạy script điều phối kiểm thử và thẩm định baseline với cấu hình Release (Bắt buộc EXIT CODE 0)
python scripts/run_and_verify_baseline.py --configuration Release

# 5. Kiểm tra bằng chứng lượt chạy vừa sinh
Get-Content docs/verification/latest-run-evidence.json
```

---

## 9. Trạng thái Bàn giao & Nghiệm thu Chính thức

- **Trạng thái T01**: **`VERIFIED`** (Đã được Chủ dự án nghiệm thu thực tế ngày **10/11/2026**; đã đồng bộ trong [tasks/T01.md](file:///D:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/tasks/T01.md) và [backlog.json](file:///D:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/backlog.json)).
- **Kết quả kiểm chứng cuối cùng**:
  * Cấu hình thực thi: **`Release`**.
  * `HarnessCheck`: **5 PASS**, 0 FAIL, 0 SKIPPED (Exit code: 0).
  * `RegressionBug`: **8 FAIL** đúng assertion manifest lỗi baseline, 0 PASS, 0 SKIPPED (Exit code: 1).
  * `Bộ công cụ Python`: **18 PASS** / 18 tests (Exit code: 0).
  * `Orchestrator`: **Exit code 0** (`SUCCESS_BASELINE_MATCHED`).
  * **Cảnh báo bản chất**: **Đây là baseline khớp danh mục lỗi đã biết, tuyệt đối KHÔNG PHẢI sản phẩm đã hết lỗi**.
- **Đính chính kỹ thuật F01**:
  * Chuỗi migration EF Core hiện tại trong `HRM.DAL` **đã tạo cột `Hwid`** trên bảng `ChamCong` thông qua migration `20260502105319_AddChamCongHwid` (đã chứng minh bằng test độc lập `EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong` đạt PASS).
  * Phân biệt rõ ràng với:
    * Class `20260504161102_AddHwidToChamCong` thiếu metadata `[MigrationAttribute]` và phương thức `Up()` không chứa thao tác thêm cột.
    * File script SQL tài liệu `db/docs/HRM_Database.sql` thiếu cột `Hwid`.
  * **Không coi việc thêm lại cột `Hwid` là yêu cầu sửa lỗi** (tránh thêm trùng cột gây lỗi schema).
- **Lưu ý bắt buộc cho T02**:
  * Phải đối chiếu chuỗi migration và schema thực tế trước khi đề xuất bất kỳ thay đổi nào.
  * Tuyệt đối không thêm trùng cột `Hwid` và không sửa đổi các class migration lịch sử chỉ nhằm mục đích làm xanh test phụ thuộc tên class.
- **Ranh giới an toàn**:
  * Giữ nguyên 100% mã nguồn sản phẩm `db/src` so với baseline commit `05254b3`.
  * Không chạy migration, không kết nối database thật.
  * Không thực hiện `git commit` hoặc `git push`.
  * Chưa bắt đầu nhiệm vụ T02.
