# Ghi nhận review T03 của Codex trong chat — 11/11/2026

Các ghi nhận này dựa trên bản nháp đang được Anti viết; cần kiểm tra đã sửa trước khi kết luận.

**Kết quả cuối:** Cả ba ghi nhận dưới đây đã được xử lý ở bản sửa Anti. Codex build/test độc lập: 6/6 characterization PASS; baseline 5 PASS và 8 FAIL đúng manifest; Graphify update exit 0. Xem `T03-codex-checks.md`. Giữ phần dưới làm dấu vết review bản nháp.

1. Test `F10_PartialUpdateCharacterizationTests.cs` đang truyền 5 tham số cho `NhanVienService`, nhưng constructor hiện tại trong `db/src/HRM.BLL/Services/NhanVienService.cs` chỉ nhận INhanVienRepository, ITaiKhoanService, IBangLuongService. Không sửa service sản phẩm để chiều test; sửa test theo constructor thật và chạy build.
2. `docs/legacy-transfer-map.md` đang dẫn `20260505060254_AddChamCongHwid.cs` không tồn tại trong checkout hiện tại. `docs/verification/T02-schema-audit.md` đã xác định migration hợp lệ `20260502105319_AddChamCongHwid.cs` có tạo Hwid; file thừa là `20260504161102_AddHwidToChamCong.cs`. Phân biệt phát hiện snapshot cũ với bằng chứng hiện tại, không sao chép đường dẫn cũ rồi khẳng định hiện trạng.
3. F01/F08 đang ghi BACKEND_MỚI_ĐÃ_SỬA nhưng T02 chỉnh schema legacy/EF baseline, không tạo backend mới. Phải ghi rõ phạm vi EF đã kiểm chứng, backend nghiệp vụ mới chưa triển khai; SQL legacy thuần túy vẫn UNVERIFIED.

Bằng chứng baseline độc lập: xem `T03-codex-checks.md`, 5/5 HarnessCheck PASS. Kết quả này không chứng minh test T03 mới đã chạy.
