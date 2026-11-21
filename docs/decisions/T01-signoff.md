# Biên bản Nghiệm thu Nhiệm vụ T01 (T01 Sign-off Record)

> **Dự án:** Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần  
> **Nhiệm vụ:** T01 — Đóng băng baseline và dựng kiểm thử hồi quy  
> **Ngày phê duyệt:** 10/11/2026  
> **Người phê duyệt:** Chủ dự án (Sinh viên thực hiện đồ án)  
> **Đơn vị hỗ trợ:** AI Coding Assistant (Antigravity)  
> **Trạng thái nhiệm vụ:** `VERIFIED`  
> **Baseline Commit:** `05254b306f1f882b118575f50c5c81b486f40c6c` (Tag: `baseline-05254b3`)

---

## 1. Tuyên bố Nghiệm thu & Kết quả Thực tế

Chủ dự án chính thức nghiệm thu hoàn thành nhiệm vụ **T01** với toàn bộ các tiêu chí kiểm chứng đã thực hiện và xác minh thực tế:

1. **Cấu hình thực thi đồng bộ**: Đã cấu hình và kiểm chứng đồng bộ trên cấu hình **`Release`** cho cả build legacy solution, regression test suite và runner orchestrator.
2. **Số lượng test kiểm thử**:
   - `HarnessCheck`: **5 PASS**, 0 FAIL, 0 SKIPPED (Exit code 0).
   - `RegressionBug`: **8 FAIL** đúng assertion manifest lỗi baseline, 0 PASS, 0 SKIPPED (Exit code 1).
   - `Bộ công cụ Python`: **18 PASS** / 18 tests (Exit code 0).
   - `Orchestrator`: **Exit code 0** (Trạng thái tổng thể: `SUCCESS_BASELINE_MATCHED`).
3. **Bản chất kết quả**:
   - **Đây là kết quả chứng minh baseline khớp danh mục lỗi đã biết trong manifest, tuyệt đối KHÔNG PHẢI sản phẩm đã hết lỗi.**
   - Bộ test hồi quy đóng vai trò chốt chặn phát hiện hồi quy và bảo vệ tính toàn vẹn nghiệp vụ cho quá trình tái cấu trúc ở các task tiếp theo.

---

## 2. Đính chính Kỹ thuật Quan trọng về Phát hiện F01

- Chuỗi migration EF Core hiện tại trong `HRM.DAL` **thực tế đã tạo cột `Hwid`** trên bảng `ChamCong` thông qua migration `20260502105319_AddChamCongHwid` (đã chứng minh bằng test độc lập `EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong` đạt PASS khi sinh DDL offline qua `IMigrator`).
- Cần phân biệt rõ ràng hiện tượng trên với:
  1. Class migration `20260504161102_AddHwidToChamCong` bị thiếu metadata `[MigrationAttribute]` và phương thức `Up()` không hề chứa thao tác thêm cột (chỉ cập nhật seed data).
  2. File script SQL tài liệu `db/docs/HRM_Database.sql` thiếu cột `Hwid` trong định nghĩa bảng `ChamCong`.
- **Quyết định kỹ thuật**: **Không coi việc thêm lại cột `Hwid` là yêu cầu sửa lỗi** trong tương lai để tránh nguy cơ thêm trùng cột gây lỗi schema cơ sở dữ liệu.

---

## 3. Lưu ý Bắt buộc cho Nhiệm vụ T02 (Thiết kế Schema & Migration)

1. Phải đối chiếu chuỗi migration hiện có và schema thực tế được sinh ra trước khi đề xuất bất kỳ thay đổi nào.
2. Tuyệt đối không thêm trùng cột `Hwid` vào bảng `ChamCong`.
3. Không tùy tiện sửa đổi các class migration lịch sử chỉ nhằm mục đích làm xanh test phụ thuộc tên class; mục tiêu hàng đầu là schema cuối cùng phải đúng đắn, an toàn và có thể tái tạo được.

---

## 4. Cam kết An toàn & Kế hoạch Tiếp theo

- **Mã nguồn sản phẩm (`db/src`)**: Giữ nguyên vẹn 100% so với baseline commit `05254b3`.
- **Cơ sở dữ liệu**: Không chạy migration, không kết nối database thật.
- **Git**: Không thực hiện `git commit` hay `git push`.
- **Chưa bắt đầu T02**: Nhiệm vụ T02 chỉ được bắt đầu khi có chỉ lệnh cụ thể từ chủ dự án.
