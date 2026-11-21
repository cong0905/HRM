# ĐỒ ÁN TỐT NGHIỆP ĐẠI HỌC

**ĐỀ TÀI:**
### HIỆN ĐẠI HÓA HỆ THỐNG QUẢN LÝ NHÂN SỰ, CHẤM CÔNG VÀ TIỀN LƯƠNG (HRM) THEO KIẾN TRÚC MODULAR MONOLITH VÀ BẢO MẬT ĐA NỀN TẢNG

---

- **Ngành**: Công nghệ Thông tin / Kỹ thuật Phần mềm
- **Sinh viên thực hiện**: [Họ và tên Sinh viên]
- **Mã số sinh viên**: [MSSV]
- **Giảng viên hướng dẫn**: [Họ và tên Giảng viên]
- **Năm thực hiện**: 2026

---

## LỜI CAM ĐOAN VÀ CÔNG BỐ VAI TRÒ AI

Em xin cam đoan đây là công trình nghiên cứu và phát triển phần mềm được thực hiện dựa trên nền tảng đề tài thực tế và yêu cầu chuẩn mực của ngành. Các số liệu, kết quả kiểm thử, kịch bản diễn tập và bằng chứng thực thi trong báo cáo này hoàn toàn trung thực, được trích xuất trực tiếp từ các bài kiểm thử thực tế trên hệ thống máy chủ cơ sở dữ liệu và mã nguồn dự án.

### Công bố minh bạch về việc sử dụng Trí tuệ Nhân tạo (AI Transparency Disclosure):
Theo đúng tinh thần liêm chính học thuật và quy định của nhà trường về việc ứng dụng AI trong học tập và nghiên cứu:
1. **Phạm vi hỗ trợ của AI**: Trí tuệ nhân tạo (mô hình ngôn ngữ lớn) được sử dụng như một cộng sự hỗ trợ kỹ thuật (AI Pair-Programmer / Swarm Assistant) trong các công việc:
   - Hỗ trợ rà soát các trường hợp biên (edge cases) và đề xuất các bài kiểm thử tự động (Unit / Integration tests).
   - Hỗ trợ sinh mã khung (boilerplate code), migration scripts, và cấu hình Dockerfile/CI pipeline.
   - Hỗ trợ định dạng tài liệu, vẽ sơ đồ Mermaid và tổng hợp các báo cáo đối soát số liệu.
2. **Vai trò và trách nhiệm của sinh viên (Tác giả)**:
   - Tác giả trực tiếp phân tích yêu cầu nghiệp vụ, xác định các bất biến cốt lõi (Invariants), thiết kế cấu trúc thực thể, ma trận phân quyền và thiết lập các ngưỡng an toàn tài chính.
   - Tác giả trực tiếp thiết lập môi trường, kiểm duyệt từng dòng mã nguồn, thực thi và thẩm định các bộ kiểm thử tự động trên SQL Server thật.
   - Tác giả chịu trách nhiệm hoàn toàn về tính đúng đắn của toàn bộ mã nguồn, kiến trúc hệ thống, độ chính xác của số liệu và khả năng tự trình bày, phản biện độc lập trước Hội đồng đánh giá mà không phụ thuộc vào công cụ AI.

---

## MỤC LỤC TỔNG QUAN BÁO CÁO

- **Chương 1: Tổng quan và Hiện trạng hệ thống legacy**
  - Giới thiệu đề tài, bối cảnh thực tế.
  - Phân tích hiện trạng hệ thống WinForms client-server cũ và các rủi ro kỹ thuật.
  - Mục tiêu, phạm vi và phương pháp tiếp cận trong 8 tuần.
- **Chương 2: Phân tích Yêu cầu và Ma trận Nghiệp vụ**
  - Yêu cầu chức năng và phi chức năng.
  - Ma trận phân quyền 4 vai trò (Employee, HR, Payroll, SystemAdmin).
  - Các ràng buộc bất biến (Invariants) không được vi phạm.
  - Danh mục phạm vi hoàn thành và các tính năng hoãn lại (Out of Scope / Deferred).
- **Chương 3: Thiết kế Kiến trúc và Cơ sở dữ liệu**
  - Kiến trúc phần mềm Modular Monolith và Clean Architecture 4 tầng.
  - Mô hình bảo mật Session-based kết hợp Cookie & Bearer Token Adapter.
  - Thiết kế Cơ sở dữ liệu quan hệ (ERD 5 thực thể chính, Indexing, Concurrency control).
- **Chương 4: Hiện thực các Phân hệ Cốt lõi**
  - Phân hệ Tài khoản, Xác thực & Thu hồi quyền tức thời (Revocation).
  - Phân hệ Quản lý Nhân sự & Mã nhân viên tự động.
  - Phân hệ Chấm công, Máy chủ thời gian & Cơ chế Chống bấm lặp (Idempotency).
  - Phân hệ Nghỉ phép & Giao dịch trừ quỹ đồng thời an toàn (Concurrency Locking).
  - Phân hệ Tiền lương, Snapshot bất biến & Công bố phiếu lương.
  - Phân hệ Thông báo Transactional Outbox & Background Worker.
  - Ứng dụng Web SPA (React/Vite) và Mobile App (React Native/Android).
- **Chương 5: Kiểm thử, Đối soát Di chuyển Dữ liệu và Đo tải**
  - Chiến lược kiểm thử đa tầng (Multi-tier Testing Strategy).
  - Kết quả kiểm thử tích hợp trên SQL Server thật (Real DB Integration Tests).
  - Di chuyển dữ liệu Legacy (T23) với sai lệch tài chính 0.00 VND và bảo toàn dữ liệu 100%.
  - Kết quả đo tải (T26) với 30 Virtual Users đạt chuẩn SLA và diễn tập phục hồi thảm họa (RTO < 1s).
- **Chương 6: Kết luận và Hướng phát triển**
  - Đóng góp chính của đề tài.
  - Hạn chế và các bài học kinh nghiệm sâu sắc.
  - Lộ trình phát triển tiếp theo sau giai đoạn MVP.
