# ADR-0004: Xây dựng ứng dụng Web SPA bằng React, TypeScript và Vite

> **Trạng thái:** `PROPOSED` (Đề xuất chờ Chủ dự án xác nhận)  
> **Ngày quyết định:** 09/11/2026  
> **Tác giả:** AI Coding Assistant / Nhóm phát triển HRM

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Phiên bản Desktop WinForms hiện tại chỉ chạy được trên hệ điều hành Windows và đòi hỏi phải cài đặt trực tiếp trên từng máy tính người dùng. Để hệ thống có thể tiếp cận mọi đối tượng (HR, Kế toán, Quản trị viên, Nhân viên) từ trình duyệt web hiện đại, dự án cần xây dựng một ứng dụng Web Frontend hiện đại, thân thiện và bảo mật.

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Tính tương thích công nghệ với Mobile:** Mobile đã chọn React Native. Việc chọn **React** cho Web giúp tận dụng tối đa kỹ năng lập trình (React Hooks, State Management, Component Lifecycle) và chia sẻ kiểu dữ liệu TypeScript (DTO interfaces) giữa Web và Mobile.
2. **Trải nghiệm người dùng mượt mà (SPA):** Giao diện quản lý nhân sự, chấm công và bảng lương cần các thao tác tìm kiếm, lọc, phân trang, hiển thị modal nhanh chóng mà không phải tải lại toàn bộ trang web.
3. **Tốc độ phát triển và Build:** Cần một công cụ build hiện đại, khởi động tức thì và Hot Module Replacement (HMR) mượt mà trong thời gian làm đồ án gấp rút 8 tuần.

---

## 3. Các Phương án đã Cân nhắc (Considered Options)

1. **Phương án A: Server-Side Rendering với ASP.NET Core Razor Pages / Blazor**
   - _Ưu điểm:_ Viết hoàn toàn bằng C#.
   - _Nhược điểm:_ Không tận dụng được kỹ năng và tài nguyên giao diện chung với Mobile React Native; Blazor WebAssembly tải lần đầu nặng.
2. **Phương án B: Next.js (React Framework)**
   - _Ưu điểm:_ Hỗ trợ SSR/SSG tốt cho SEO.
   - _Nhược điểm:_ Ứng dụng HRM là phần mềm nội bộ (Internal Dashboard), không cần SEO công khai. Next.js làm phức tạp hóa mô hình hosting và xác thực cookie session cùng origin.
3. **Phương án C (Được chọn): Single Page Application (SPA) với React, TypeScript và Vite**
   - Bộ công cụ tinh gọn, mạnh mẽ, hiệu năng build cao nhất hiện nay.

---

## 4. Quyết định (Decision Outcome)

**Lựa chọn Phương án C:**

- Công nghệ: **React 18+**, ngôn ngữ **TypeScript**.
- Công cụ đóng gói (Build Tool): **Vite**.
- Quản lý trạng thái và Server Cache: **TanStack Query (React Query)** để quản lý caching API, tự động làm mới dữ liệu và xử lý loading/error states.
- Giao diện (UI Framework): **Tailwind CSS** kết hợp thư viện component gọn nhẹ (như shadcn/ui hoặc Ant Design) để đảm bảo giao diện chuyên nghiệp, responsive trên mọi kích thước màn hình.
- HTTP Client: **Axios / Ky** được cấu hình tự động gửi kèm Cookie xác thực và mã chống CSRF (`X-XSRF-TOKEN`).

---

## 5. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):

- Đồng bộ hệ sinh thái kỹ thuật: Cùng dùng React & TypeScript với Mobile App.
- Khởi động môi trường phát triển chỉ trong 1 giây nhờ Vite.
- Kiểm thử tự động giao diện dễ dàng với Vitest và Playwright.

### Điểm hạn chế và Thách thức (Cons & Mitigations):

- SPA cần cấu hình Reverse Proxy hoặc CORS phù hợp khi giao tiếp với Backend API.
  - _Giải pháp:_ Trong môi trường phát triển và Staging, cấu hình Vite Proxy hoặc deploy cùng domain để Web và Backend giao tiếp qua HTTPS cùng origin, bảo vệ tuyệt đối Cookie HttpOnly chống XSS.
