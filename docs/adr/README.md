# Architectural Decision Records (ADR) — Nhật ký Quyết định Kiến trúc

> **Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần**  
> Thư mục này ghi lại toàn bộ các quyết định kiến trúc phần mềm quan trọng, bối cảnh lựa chọn, các phương án thay thế được cân nhắc và những đánh đổi kỹ thuật.

---

## 1. Trạng thái Quyết định (Decision Status Legend)

- **`CONFIRMED` (Đã được Chủ dự án chính thức ký duyệt):** Quyết định đã được sinh viên (chủ dự án) chốt trực tiếp cho bản đồ án tốt nghiệp tại mốc T00.
- **`ACCEPTED` (Đã nghiệm thu):** Quyết định đã được thực thi và kiểm chứng thực tế qua mã nguồn và bài kiểm thử ở các task triển khai.
- **`DEFERRED` (Tạm hoãn):** Quyết định được dời lại xem xét sau giai đoạn đồ án 8 tuần.

---

## 2. Danh mục Quyết định Kiến trúc (ADR Index)

| Mã ADR | Tiêu đề quyết định | Trạng thái | Ngày chốt | Tóm tắt nội dung |
| :--- | :--- | :---: | :---: | :--- |
| **[ADR-0001](0001-kien-truc-modular-monolith.md)** | Lựa chọn Kiến trúc Modular Monolith cho Backend | `CONFIRMED` | 10/11/2026 | Tổ chức Backend thành một ứng dụng duy nhất nhưng phân tách các module nghiệp vụ rõ ràng (Domain, Application, Infrastructure, Api); bác bỏ Microservices. |
| **[ADR-0002](0002-backend-aspnetcore.md)** | Lựa chọn ASP.NET Core (.NET 10 LTS) làm nền tảng Backend API | `CONFIRMED` | 10/11/2026 | Kế thừa nền tảng C# từ dự án cũ; chốt .NET 10 LTS làm kiến trúc đích cho backend mới; .NET 8 chỉ là baseline legacy. Chuẩn bị SDK theo task, chưa tự nâng legacy trong T00. |
| **[ADR-0003](0003-database-sqlserver.md)** | Quản lý Cơ sở dữ liệu SQL Server và Phân tách Database mới | `CONFIRMED` | 10/11/2026 | Tiếp tục dùng SQL Server và EF Core Code-First; thiết lập cơ sở dữ liệu mới (`HRM_Next_Dev` và `HRM_Next_Test`), giữ nguyên DB cũ làm đối chiếu. Chưa tạo DB trong T00. |
| **[ADR-0004](0004-web-react-typescript.md)** | Xây dựng ứng dụng Web SPA bằng React, TypeScript và Vite | `CONFIRMED` | 10/11/2026 | Xây dựng Web Portal quản trị và cá nhân bằng React 18+ và TypeScript, dùng Vite làm công cụ build siêu tốc; chia sẻ kiểu dữ liệu với Mobile. |
| **[ADR-0005](0005-mobile-react-native-android.md)** | Xây dựng ứng dụng Mobile bằng React Native, Android nghiệm thu trước | `CONFIRMED` | 10/11/2026 | Sử dụng React Native; Android là nền tảng nghiệm thu ưu tiên hàng đầu trong 8 tuần; phiên bản Android tối thiểu căn cứ theo độ tương thích thực tế ở T20; hoãn iOS. |
| **[ADR-0006](0006-xac-thuc-phan-quyen-identity.md)** | Xác thực tập trung bằng ASP.NET Core Identity, Phân quyền Server | `CONFIRMED` | 10/11/2026 | Dùng Identity tiêu chuẩn; Cookie HttpOnly + Anti-CSRF cho Web; Bearer + Refresh Token trong Secure Storage cho Mobile; kiểm tra trực tiếp server-side để thu hồi quyền truy cập. |

---

## 3. Quy tắc cập nhật ADR

**Bổ sung 11/11/2026:** [ADR-0007 — Runtime và dependency .NET10](0007-runtime-va-dependency-dotnet10.md) đã được Codex quyết định theo ủy quyền T04: SDK 10.0.404, EF/Extensions 10.0.15, CPM/lockfile, giữ WinForms build được; phạm vi và bằng chứng tại [T04-signoff.md](../decisions/T04-signoff.md).

**T05, 11/11/2026:** [ADR-0008 — Backend foundation và contracts](0008-backend-foundation-contracts.md)
ghi ranh giới project/use case, tracking, DB riêng, chuỗi decimal, ngày UTC và client generation.
Đã có bằng chứng full gate; đang READY_FOR_REVIEW, chưa thay chữ ký chủ dự án.

**T06, 11/11/2026:** [ADR-0009 — Identity và session lifecycle](0009-identity-sessions-reset.md)
chi tiết hóa ADR-0006: framework opaque bearer 15 phút/refresh tuyệt đối 7 ngày, reset 15 phút,
cookie/CSRF, SQL-backed immediate revocation, rotation chống replay và sender chưa cấu hình.
T05 đã có [signoff theo ủy quyền](../decisions/T05-signoff.md); trạng thái ADR-0008 ở trên là lịch sử bàn giao ban đầu.

1. Mỗi khi có sự thay đổi lớn về công nghệ hoặc mô hình xử lý dữ liệu, bắt buộc phải tạo mới hoặc cập nhật tài liệu ADR tương ứng.
2. Không chỉnh sửa nội dung lịch sử của các ADR đã được ký duyệt (`CONFIRMED`/`ACCEPTED`) ngoại trừ việc bổ sung ghi chú cập nhật hoặc liên kết tới ADR thay thế mới.
3. Chi tiết ký duyệt và đối chiếu các quyết định nghiệp vụ được lưu tại [docs/decisions/T00-signoff.md](../decisions/T00-signoff.md).
