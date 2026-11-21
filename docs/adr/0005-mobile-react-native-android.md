# ADR-0005: Xây dựng ứng dụng Mobile bằng React Native, Nghiệm thu Android trước

> **Trạng thái:** `CONFIRMED` (Đã được Chủ dự án chính thức ký duyệt — Quyết định `OPEN-12`)  
> **Ngày quyết định:** 10/11/2026  
> **Tác giả:** Chủ dự án & AI Coding Assistant

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Một trong những mục tiêu trọng tâm của đồ án tốt nghiệp là cung cấp ứng dụng di động cho nhân viên (Employee Self-Service - ESS) để:
- Chấm công online hàng ngày theo giờ máy chủ.
- Tra cứu lịch sử công, số phút đi muộn / về sớm.
- Nộp đơn nghỉ phép và theo dõi tiến trình phê duyệt.
- Xem phiếu lương cá nhân của các kỳ đã công bố.

Yêu cầu đồ án:
- **Chủ dự án đã chọn framework React Native.**
- Cần xác định phạm vi nền tảng mục tiêu nghiệm thu trong 8 tuần để bảo đảm tính khả thi và hoàn thành đúng hạn.

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Khung công nghệ đã chốt:** Chủ dự án đã chọn React Native.
2. **Chia sẻ tri thức và hợp đồng dữ liệu với Web:** Web (React) và Mobile (React Native) đều sử dụng TypeScript, dùng chung DTOs, API contracts và logic validation.
3. **Tối ưu hóa nguồn lực kiểm thử trong 8 tuần:** Việc kiểm thử và đóng gói trên nhiều hệ điều hành đòi hỏi phân bổ thời gian lớn cho môi trường và thiết bị. Cần tập trung nghiệm thu trọn vẹn trên một nền tảng trước để đảm bảo tính hoàn chỉnh của đồ án.

---

## 3. Quyết định (Decision Outcome)

1. **Framework di động:** Sử dụng **React Native** (sử dụng Expo hoặc React Native CLI theo cấu hình tối ưu).
2. **Nền tảng mục tiêu nghiệm thu trước:** **Hệ điều hành Android**.
   - Phiên bản Android tối thiểu sẽ được xác định căn cứ theo **bản tương thích của React Native/Expo được chọn và thiết bị/emulator thực tế có sẵn khi cấu hình ở Task T20**; chưa cứng nhắc gán cố định Android 10 từ T00.
   - Các bài kiểm thử, bằng chứng hình ảnh (evidence) và video demo bảo vệ đồ án sẽ được thực hiện trên môi trường Android (thiết bị thật hoặc Android Studio Emulator).
3. **Phạm vi chức năng Mobile:** Dành riêng cho vai trò **Employee (Nhân viên)** với các màn hình:
   - Đăng nhập (tự kích hoạt qua thư mời) / Đổi mật khẩu.
   - Trang chủ cá nhân (Dashboard).
   - Chấm công Check-in / Check-out (giờ Server, chống gửi trùng qua `Idempotency-Key`).
   - Quản lý nghỉ phép (Xem quỹ phép, nộp đơn, hủy đơn `Pending`).
   - Phiếu lương cá nhân (Xem chi tiết thu nhập, phụ cấp, khấu trừ các kỳ đã công bố và đã khóa).
4. **Tuyên bố về nền tảng iOS:**
   - **Đồ án tạm hoãn nền tảng iOS; KHÔNG tuyên bố đã hỗ trợ, kiểm thử hoặc phát hành trên iOS trong 8 tuần.**
   - Mã nguồn React Native được thiết kế theo chuẩn đa nền tảng để sẵn sàng cho việc mở rộng sang iOS sau đồ án (Task P05).

---

## 4. Quản lý Phiên và Lưu trữ An toàn trên Mobile (Mobile Security)

- **Tuyệt đối không dùng `AsyncStorage` để lưu trữ token xác thực hoặc dữ liệu nhạy cảm** (theo khuyến cáo chính thức của [React Native Security Documentation](https://reactnative.dev/docs/security)).
- Sử dụng **`expo-secure-store`** hoặc thư viện **`react-native-keychain`** (sử dụng Android Keystore an toàn) để lưu trữ `Bearer Access Token` và `Refresh Token`.
- Xử lý hết phiên hoặc tài khoản bị khóa (`401 Unauthorized`): Xóa token khỏi Keystore và điều hướng người dùng về màn hình Đăng nhập ngay lập tức.

---

## 5. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):
- Đảm bảo hoàn thành sản phẩm di động chạy thực tế trước hạn bảo vệ đồ án.
- Tốc độ phát triển nhanh, giao diện native mượt mà.
- Trả lời trung thực, minh bạch trước hội đồng chấm thi về phạm vi nền tảng đã kiểm chứng.

### Điểm hạn chế và Thách thức (Cons & Mitigations):
- Không kiểm thử trên môi trường iOS trong 8 tuần.
  - *Giải pháp:* Trình bày rõ trong báo cáo tốt nghiệp rằng kiến trúc đã chọn React Native để sẵn sàng đa nền tảng; việc nghiệm thu trước trên Android là quyết định kỹ thuật có chủ đích nhằm tập trung nguồn lực kiểm thử chất lượng lõi.
