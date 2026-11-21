# Báo cáo Triển khai & Khắc phục Rà soát Nhiệm vụ T16 — Vòng 4 (Anti ➔ Codex)

- **Tác giả:** Antigravity (Anti) - Trực tiếp triển khai mã nguồn tại `D:/dotnet project`
- **Người rà soát / Điều phối:** Codex (Reviewer & Runner độc lập)
- **Nhiệm vụ:** T16 — Khung web React và API client
- **Trạng thái đề xuất:** `READY_FOR_REVIEW` *(Tuân thủ nghiêm ngặt quy tắc: Không tự xác nhận `VERIFIED` hay tuyên bố UAT)*
- **Thời điểm cập nhật:** 13/11/2026 (Phiên bản khắc phục sau rà soát Vòng 4)

---

## 1. Tóm tắt Nội dung Khắc phục Rà soát Vòng 4 (Sửa tối thiểu đúng nguyên nhân gốc)

Toàn bộ chỉnh sửa được thực hiện bằng **công cụ tệp nguyên bản (native file tools)**, không gọi lệnh terminal/shell, không thay đổi backend hay hợp đồng contracts đã sinh:

### 1.1. Sửa Lỗi Production trong `changePassword` ([AuthContext.tsx](file:///D:/dotnet%20project/web/src/context/AuthContext.tsx))
- **Nguyên nhân gốc:** Lệnh `queryClient.removeQueries()` không predicate và `queryClient.clear()` đã xóa sạch query `["access", "me"]` đang có observer active của `AuthProvider`. Lệnh `setQueryData(AUTH_QUERY_KEY, null)` sau đó tạo một query mới không còn observer cũ, khiến giao diện vẫn giữ trạng thái authenticated sau phản hồi 204.
- **Khắc phục:** Đồng bộ cách xử lý chuẩn như `logout`:
  ```typescript
  await queryClient.cancelQueries();
  queryClient.removeQueries({
    predicate: (query) => query.queryKey[0] !== "access",
  });
  clearCachedCsrfToken();
  queryClient.setQueryData(AUTH_QUERY_KEY, null);
  ```
  Giữ nguyên observer cho auth query để nhận `null` ngay lập tức, trong khi toàn bộ dữ liệu riêng tư bị hủy bỏ.
- **Cập nhật bài kiểm thử ([test/auth-session.test.tsx](file:///D:/dotnet%20project/web/test/auth-session.test.tsx)):** Bỏ assertion phụ thuộc `clearSpy`, thay bằng khẳng định dữ liệu riêng tư `queryClient.getQueryData(["private", "salary"])` trở thành `undefined` và `auth-status` chuyển thành `unauthenticated`.

### 1.2. Chuẩn hóa Giao thức 401 Fixture cho `openapi-fetch` Client
- **Nguyên nhân gốc:** `renderAppWithInitialRoute` (khi `userProfile == null`) trong [test/navigation-permissions.test.tsx](file:///D:/dotnet%20project/web/test/navigation-permissions.test.tsx), `beforeEach` trong [test/form-validation-accessibility.test.tsx](file:///D:/dotnet%20project/web/test/form-validation-accessibility.test.tsx), và ca login sequence trong [test/auth-session.test.tsx](file:///D:/dotnet%20project/web/test/auth-session.test.tsx) trả về `new Response(null, { status: 401 })`. Client `openapi-fetch` phân tích cú pháp JSON mặc định gặp lỗi `SyntaxError: Unexpected end of JSON input`, khiến bootstrap bị hiểu nhầm là lỗi mạng/lỗi không mong muốn.
- **Khắc phục:** Trả về đối tượng `ProblemDetails` chuẩn theo đúng giao thức máy chủ thực tế:
  ```typescript
  new Response(
    JSON.stringify({
      status: 401,
      errorCode: "authentication.required",
      traceId: "test-anon",
      title: "Unauthorized",
    }),
    {
      status: 401,
      headers: { "Content-Type": "application/problem+json" },
    }
  )
  ```

### 1.3. Khắc phục Kiểm thử Biểu mẫu ([test/form-validation-accessibility.test.tsx](file:///D:/dotnet%20project/web/test/form-validation-accessibility.test.tsx))
- **Quên mật khẩu (Forgot Password):** Sử dụng `fireEvent.change` cho trường email và phát trực tiếp sự kiện `fireEvent.submit(emailInput.closest("form")!)` trong `await act(async () => { ... })`. Loại bỏ hiện tượng timeout 5s do mô phỏng bàn phím chậm trong JSDOM.
- **Xác thực mật khẩu mạnh (Password Validation):** Điền cả trường mật khẩu mới và xác nhận mật khẩu, phát sự kiện submit trực tiếp lên form `fireEvent.submit(formElement)` trong `act`. Xác thực kiểm tra chính xác cả 2 quy tắc độ dài (min 12) và độ phức tạp ký tự mà không kích hoạt gọi API thừa.

### 1.4. Quản lý Vòng đời Query Client & Tránh Xung đột Tài nguyên JSDOM
- **Dọn dẹp sau mỗi ca kiểm thử:** Bổ sung việc hủy bỏ và dọn sạch query client trong `afterEach` ở cả 3 tệp kiểm thử để tránh rò rỉ trạng thái hoặc yêu cầu ngầm giữa các ca kiểm thử.
- **Tuần tự hóa bộ kiểm thử JSDOM ([vite.config.ts](file:///D:/dotnet%20project/web/vite.config.ts)):** Bổ sung `fileParallelism: false` trong mục `test`, giữ nguyên dòng `import { defineConfig } from "vitest/config";` do Codex thiết lập.

---

## 2. Danh mục Tệp đã Cập nhật trong Vòng 4

| STT | Tệp Cập nhật | Thay đổi Chính |
| :--- | :--- | :--- |
| 1 | `web/src/context/AuthContext.tsx` | Sửa `changePassword`: dùng predicate giữ auth query, hủy cache riêng tư, `setQueryData(AUTH_QUERY_KEY, null)` kích hoạt observer. |
| 2 | `web/test/auth-session.test.tsx` | Thay `clearSpy` bằng assert private cache undefined; sửa fixture 401 login thành ProblemDetails JSON; cleanup query client trong `afterEach`. |
| 3 | `web/test/navigation-permissions.test.tsx` | Đổi fixture 401 anonymous thành ProblemDetails JSON; cleanup query client trong `afterEach`. |
| 4 | `web/test/form-validation-accessibility.test.tsx` | Đổi fixture 401 thành ProblemDetails JSON; dùng `fireEvent.submit` trên form cho forgot password và password validation; cleanup query client trong `afterEach`. |
| 5 | `web/vite.config.ts` | Bổ sung `fileParallelism: false` trong cấu hình test; giữ nguyên `import { defineConfig } from "vitest/config";`. |
| 6 | `docs/verification/T16-anti-report.md` | Báo cáo chi tiết này. |

---

## 3. Trạng thái Thực thi (Tests NOT_RUN bởi Anti)

- **Tuân thủ chính sách sandbox:** Toàn bộ lệnh shell/test được đánh dấu là **`NOT_RUN`** bởi Anti. Anti không chạy shell hay terminal.
- **Duy trì phạm vi:** Giữ nguyên toàn bộ 20 ca kiểm thử, không nới lỏng strict, không đổi dependencies/package.json, không sửa backend hay `contracts/`.
- Sẵn sàng để Codex runner thực thi độc lập (`npm run typecheck`, `npm run lint`, `npm test`).
