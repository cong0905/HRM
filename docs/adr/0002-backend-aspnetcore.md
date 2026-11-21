# ADR-0002: Lựa chọn ASP.NET Core (.NET 10 LTS) làm nền tảng Backend API

> **Trạng thái:** `CONFIRMED` (Đã được Chủ dự án chính thức ký duyệt — Quyết định `OPEN-01`)  
> **Ngày quyết định:** 10/11/2026  
> **Tác giả:** Chủ dự án & AI Coding Assistant

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Dự án HRM kế thừa mã nguồn C# và định nghĩa thực thể Entity Framework Core trên nền tảng .NET 8 (xây dựng cho WinForms cũ):
- .NET 8 là phiên bản LTS sẽ chính thức hết hạn hỗ trợ tiêu chuẩn (End of Support) vào ngày **10/11/2026** (theo [Chính sách hỗ trợ của Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy)). Do đó, .NET 8 chỉ được coi là baseline legacy của dự án cũ.
- Hệ thống cần chuyển dịch từ Desktop sang RESTful Web API để cung cấp dịch vụ cho Web SPA (React) và Mobile App (React Native).
- Việc chuẩn bị môi trường SDK (.NET 10) là công tác kỹ thuật cần thực hiện theo từng task triển khai; không tự ý hạ thấp kiến trúc thiết kế dài hạn của sản phẩm và chưa tự nâng toàn bộ code legacy trong Task T00.

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Bảo tồn đầu tư mã nguồn cũ:** Giữ lại ngôn ngữ C# và các thuật toán tính toán đã được kiểm nghiệm, tránh việc phải viết lại bằng framework ngôn ngữ khác.
2. **Hiệu năng và bảo mật:** ASP.NET Core cung cấp hiệu năng xử lý cao, tích hợp sẵn DI (Dependency Injection), Authentication, Authorization và OpenAPI.
3. **Vòng đời hỗ trợ dài hạn (Support Lifecycle):** Nâng cấp lên .NET 10 LTS để bảo đảm hệ thống có thể tiếp tục thử nghiệm và mở rộng sau đồ án mà không bị cảnh báo hết hạn hỗ trợ từ nền tảng.

---

## 3. Quyết định (Decision Outcome)

**Chốt lựa chọn:**
- Xây dựng tầng API tập trung mới bằng **ASP.NET Core Minimal APIs / Controllers**.
- **Backend mới dùng .NET 10 LTS**. .NET 8 chỉ phục vụ mục tiêu tham chiếu baseline legacy.
- Sử dụng **OpenAPI (Swagger / Scalar)** làm hợp đồng API chuẩn mực (Contract-first) để sinh mã nguồn TypeScript client tự động cho Web và Mobile.
- Chuẩn bị môi trường .NET 10 SDK theo từng task (bắt đầu từ Task T04/T05); **chưa tự ý nâng toàn bộ code legacy trong Task T00**.

---

## 4. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):
- Kế thừa lại định nghĩa Entity, DTO và logic tính toán đã viết bằng C#.
- Sẵn sàng cho việc vận hành và nâng cấp lâu dài sau khi tốt nghiệp.
- Khắc phục các cảnh báo lỗ hổng bảo mật của gói thư viện cũ.

### Điểm hạn chế và Thách thức (Cons & Mitigations):
- Cần cài đặt SDK .NET 10 trên máy phát triển và runner CI.
  - *Giải pháp:* Cấu hình tệp `global.json` để pin chính xác phiên bản SDK sử dụng trong dự án mới.
