# CHƯƠNG 6: KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

## 6.1. Các Kết quả Đạt được của Đề tài

Trải qua lộ trình nghiên cứu, thiết kế và phát triển nghiêm ngặt trong 8 tuần, đề tài **Hiện đại hóa Hệ thống Quản lý Nhân sự, Chấm công và Tiền lương (HRM)** đã hoàn thành xuất sắc toàn bộ các mục tiêu đặt ra:

1. **Hiện đại hóa Toàn diện Kiến trúc Hệ thống**:
   - Chuyển đổi thành công từ ứng dụng desktop WinForms cũ kỹ sang kiến trúc **Modular Monolith** trên nền tảng **.NET 10** hiện đại, tuân thủ nghiêm ngặt các nguyên lý của **Clean Architecture** (phân tầng rõ ràng: Domain, Application, Infrastructure, Api).
   - Xây dựng thành công giao diện quản trị **Web SPA** (React 18 + TypeScript + Vite) hiện đại, trực quan, hỗ trợ đầy đủ các phân hệ nghiệp vụ cho HR, Kế toán và Admin.
   - Xây dựng thành công ứng dụng di động **Mobile App** (React Native / Android) dành riêng cho nhân viên với các chức năng tự phục vụ (Self-Service): chấm công online, theo dõi lịch sử công, nộp và hủy đơn nghỉ phép, tra cứu phiếu lương.
2. **Giải quyết Triệt để các Bài toán Xung đột Đồng thời (Concurrency Safety)**:
   - Áp dụng thành công cơ chế khóa lạc quan (`RowVersion`) và khóa độc quyền SQL (`UPDLOCK, ROWLOCK`) kết hợp SQL Check Constraint (`CK_LeaveBalances_NoOverdraft`), loại bỏ hoàn toàn nguy cơ âm quỹ phép khi duyệt đồng thời.
   - Bảo vệ tính chính xác của thời gian chấm công bằng đồng hồ chuẩn máy chủ (`Server Clock`) và cơ chế chống bấm lặp bằng mã định danh giao dịch (`Idempotency-Key`).
   - Thiết lập cơ chế **Snapshot bất biến** cho bảng tính lương: các kỳ lương đã công bố và đã khóa được niêm phong vĩnh viễn, ngăn chặn hoàn toàn việc sửa đổi hay làm sai lệch sổ cái tài chính lịch sử.
3. **Mô hình Bảo mật Phiên Đa nền tảng và Kiểm soát Thu hồi Tức thì**:
   - Tách biệt rõ ràng danh tính xác thực (`HrmUser`) và hồ sơ nhân sự (`EmployeeProfile`), triệt tiêu nguy cơ lỗ hổng IDOR bằng cách luôn xác thực quyền hạn từ claim phiên máy chủ.
   - Triển khai thành công kiến trúc xác thực lai: Web dùng Cookie `HttpOnly, Secure, SameSite=Strict` kết hợp bộ lọc chống CSRF; Mobile dùng cặp Bearer Token và Rotating Refresh Token nguyên tử.
   - Tính năng **Thu hồi phiên tức thời (Instant Revocation)**: vô hiệu hóa toàn bộ token/cookie đang lưu hành ngay lập tức khi người dùng đổi mật khẩu, đăng xuất từ xa hoặc khi nhân sự bị offboard.
4. **Hạ tầng Vận hành Tin cậy và Di chuyển Dữ liệu Hoàn hảo**:
   - Triển khai mô hình **Transactional Outbox** và tiến trình xử lý nền (`EmailOutboxWorker`) với cơ chế Exponential Backoff và Dead-letter status, đảm bảo email được gửi tin cậy mà không phá vỡ giao dịch nghiệp vụ hay rò rỉ token trong log.
   - Di chuyển dữ liệu thành công từ cơ sở dữ liệu cũ với **tỷ lệ khớp số lượng bản ghi 100%** và **sai lệch tài chính tuyệt đối 0.00 VND**.
   - Diễn tập sao lưu và phục hồi thảm họa đạt chỉ số ấn tượng: **RTO thực tế 0.96 giây** và **RPO thực tế 32.4 giây**, sẵn sàng cho môi trường staging thực tế.

---

## 6.2. Các Hạn chế Còn tồn tại (Limitations & Known Gaps)

Với tinh thần khách quan và trung thực trong nghiên cứu khoa học, nhóm tác giả nhìn nhận hệ thống hiện tại còn một số giới hạn sau:
1. **Phạm vi Ca làm việc**: Hệ thống hiện chỉ hỗ trợ ca làm việc ban ngày chuẩn (08:30–17:30); chưa hỗ trợ ca làm việc qua đêm (Overnight shifts) hoặc ca gãy phức tạp.
2. **Chính sách Thuế và Bảo hiểm**: Do tính chất phức tạp và thường xuyên thay đổi của các biểu thuế thu nhập cá nhân lũy tiến và chính sách đóng bảo hiểm xã hội bắt buộc, phiên bản MVP hiện tại sử dụng cơ chế nhập số tiền phụ cấp/khấu trừ cấu hình tĩnh được HR xác nhận, chưa tự động tính thuế lũy tiến theo luật thuế hiện hành.
3. **Nền tảng Thiết bị Di động**: Đề tài mới tập trung kiểm thử và hoàn thiện gói cài đặt cho nền tảng Android. Ứng dụng iOS chưa được biên dịch và thử nghiệm trên thiết bị thật do yêu cầu chứng chỉ nhà phát triển của Apple.
4. **Quy mô Thử nghiệm Tải**: Kết quả đo tải (775+ RPS, p95 < 230ms) được thực hiện trên môi trường máy chủ cục bộ với 30 Virtual Users đồng thời và 500 nhân sự. Hệ thống chưa được kiểm chứng ở quy mô hàng chục nghìn nhân sự phân tán trên hạ tầng Cloud phân tán nhiều cụm (Multi-node clusters).

---

## 6.3. Bài học Kinh nghiệm khi Ứng dụng AI

Trong quá trình thực hiện đồ án với sự hỗ trợ của công cụ AI (Mô hình ngôn ngữ lớn), nhóm tác giả đã đúc kết được những bài học quan trọng:
1. **AI là công cụ tăng tốc, con người là người quyết định**: AI có khả năng sinh mã boilerplate, cấu hình và kịch bản test rất nhanh, nhưng hoàn toàn có thể tạo ra mã nguồn "trông có vẻ đúng" nhưng vi phạm các ràng buộc nghiệp vụ ngầm (ví dụ: vô tình quên `.AsTracking()` khi truy vấn EF Core, hoặc sinh token mà không phân tách mục đích). Người kỹ sư phần mềm phải hiểu sâu sắc bản chất vấn đề, tự mình thiết kế kiến trúc và đọc hiểu từng dòng mã.
2. **Sức mạnh của Kiểm thử Thực tế (Evidence-based Verification)**: Không bao giờ tin tưởng vào tuyên bố "đã hoàn thành" nếu không có bài test thực chạy trên môi trường thật (Real SQL Server, Fake SMTP socket, Browser E2E). Kiểm thử tự động chính là bằng chứng xác thực duy nhất chứng minh hệ thống hoạt động đúng.
3. **Kỷ luật Giữ vững Phạm vi (Scope Management)**: Việc AI đề xuất thêm nhiều tính năng hấp dẫn (như chatbot AI, push notification, nhận diện khuôn mặt) rất dễ làm dự án bị trễ hạn hoặc phá vỡ các phân hệ cốt lõi. Kỷ luật tập trung vào "Tiền và Quyền" (toàn vẹn dữ liệu, giao dịch đồng thời và an toàn bảo mật) là yếu tố quyết định sự thành công của đề tài.

---

## 6.4. Hướng Phát triển Tiếp theo (Future Work)

Sau khi hoàn thành giai đoạn MVP, hệ thống có thể tiếp tục mở rộng theo các hướng sau:
1. **Hoàn thiện Động cơ Thuế và Bảo hiểm tự động**: Xây dựng module tính thuế TNCN theo biểu thuế lũy tiến từng phần và tự động trích nộp BHXH/BHYT/BHTN theo mức lương đóng bảo hiểm thực tế.
2. **Mở rộng Đa ca và Chấm công Sinh trắc học**: Bổ sung hỗ trợ ca gãy, ca đêm và tích hợp API đồng bộ dữ liệu trực tiếp từ các máy chấm công vân tay / khuôn mặt tại văn phòng.
3. **Phát hành iOS và Tích hợp Thông báo Đẩy**: Đóng gói ứng dụng cho nền tảng iOS trên Apple App Store và tích hợp dịch vụ Firebase Cloud Messaging (FCM) để gửi thông báo duyệt đơn nghỉ phép theo thời gian thực.
4. **Kiến trúc Triển khai Đám mây (Cloud Native)**: Đưa hệ thống lên hạ tầng Kubernetes (K8s) với cơ chế tự động mở rộng (Auto-scaling), tích hợp Azure SQL Database / AWS RDS và quản lý bí mật qua HashiCorp Vault.
