# Báo cáo Triển khai Task T20 — Mobile Shell & Safe Session (Hiệu chỉnh sau Review)

**Trạng thái thực thi:** `WORK_COMPLETE`  
**Người thực hiện:** Antigravity  
**Người kiểm duyệt & Runner độc lập:** Codex  
**Thời gian cập nhật:** 2026-11-15 13:32 (ICT)  

---

## 1. Tóm tắt Kết quả Xử lý Yêu cầu Review của Codex

Toàn bộ 5 lỗi hồi quy phát hiện trong `mobile/tests/codex-session-review.test.ts` và các điểm kiến trúc đã được xử lý triệt để:

### 1.1. Xử lý 5 lỗi hồi quy cốt lõi
1. **Chặn Cross-Origin & Validate URL API (`env.ts`, `client.ts`):**
   - `mobileFetch` kiểm tra nguồn gốc URL nghiêm ngặt (`target.origin === apiOrigin`). Chặn ngay lập tức trước khi gọi `fetch` nếu URL hướng sang origin khác, ngăn chặn rò rỉ Bearer token sang máy chủ lạ.
   - Bỏ default hostname staging giả lập. Biến môi trường `EXPO_PUBLIC_API_URL` được kiểm tra chặt chẽ: bắt buộc giao thức `https:`, không userinfo, không query, không fragment; báo lỗi rõ ràng nếu thiếu cấu hình. Không in raw URL có thể chứa bí mật vào lỗi.
2. **Epoch Guards chống rò rỉ dữ liệu khi đổi tài khoản (`client.ts`):**
   - Chụp `requestEpoch` và `requestAccessToken` ngay đầu `mobileFetch`.
   - Kiểm tra `SessionManager.getEpoch() === requestEpoch` sau mọi điểm `await` (sau fetch, sau body decode, sau refresh, sau retry).
   - Nếu phản hồi mạng của tài khoản A trả về sau khi người dùng đã logout hoặc switch sang tài khoản B, request của A bị từ chối ngay lập tức, không làm rò rỉ dữ liệu riêng tư và không kích hoạt refresh/revoke đối với tài khoản B.
3. **Invalidate Credentials khi Retry lần 2 vẫn gặp 401 (`client.ts`):**
   - Khi request nhận 401, client thực hiện refresh Single-Flight thành công rồi retry lần 2.
   - Nếu lần retry thứ 2 này máy chủ vẫn trả về 401, client xác định phiên đã bị thu hồi hoàn toàn, lập tức gọi `SessionManager.invalidateSession()`, phát sự kiện xóa credentials, che dữ liệu người dùng và đưa app về trạng thái chưa đăng nhập.
   - Các route xác thực `skipAuth` (forgot/reset/activate) không gắn Bearer và không kích hoạt refresh.
4. **Invalidate Epoch đồng bộ & Chống hồi sinh phiên khi Logout (`session.ts`, `secureStore.ts`):**
   - `SessionManager.invalidateSession()` tăng Epoch ĐỒNG BỘ (synchronously) trước bất kỳ lệnh `await` nào, triệt tiêu tức thì mọi Promise đang bay.
   - `setSession` và `saveSession` có guard Epoch trước và sau `await`: nếu có lệnh đăng xuất xảy ra trong lúc storage đang ghi, dữ liệu vừa ghi sẽ bị xóa sạch ngay lập tức và không hồi sinh phiên chết.
5. **Publish Inflight Promise đồng bộ cho Single-Flight Refresh (`session.ts`):**
   - `SessionManager.refreshSingleFlight()` gán và trả về `inflightRefresh` đồng bộ ngay lập tức trước bất kỳ `await` nào (kể cả khi session ban đầu mới chỉ nằm trong storage).
   - Hai lời gọi refresh đồng thời luôn dùng chung đúng 1 request mạng tới `/api/v1/auth/mobile/refresh`.
   - Khối `finally` được ràng buộc theo instance của Promise, không xóa nhầm inflight của thế hệ Epoch mới.
   - Nếu token đã được làm mới bởi request khác trong lúc chờ, request tiếp theo tái sử dụng token mới để retry mà không tạo refresh thừa.

### 1.2. Các cải tiến bảo mật và giao diện đi kèm
- **Fail-Closed SecureStore:** Trong môi trường production, nếu native module không khả dụng thì hệ thống ném lỗi bảo vệ, không tự ý fallback memory. Giữ mock mode riêng phục vụ unit test tách biệt.
- **Xác thực TokenPair runtime:** Kiểm tra chặt chẽ `tokenType === "Bearer"`, `accessToken`, `refreshToken` không rỗng và `expiresIn` là số dương hữu hạn trước khi lưu.
- **Quy trình Logout chuẩn xác:** Chụp token trước khi dọn dẹp bộ nhớ; invalidate local ngay lập tức; gọi `POST /api/v1/auth/logout` bằng token đã chụp; thông báo server chưa xác nhận được lưu giữ trên màn hình Đăng nhập sau khi màn hình cũ unmount.
- **Timeout toàn diện:** Áp dụng timeout bao trọn toàn bộ chu kỳ (fetch, body decode, refresh, retry), kết hợp với caller `AbortSignal`. Bổ sung timeout cho refresh fetch.
- **Bảo mật biểu mẫu:** Tự động xóa sạch mật khẩu và mã xác thực khỏi component state khi unmount hoặc sau khi gửi thành công.
- **Bảo vệ AppState:** Khi app chuyển sang background (App Switcher), giao diện hiển thị màn che riêng tư. Khi resume trở lại, app tự động xác minh lại phiên với server để phát hiện kịp thời các trường hợp tài khoản bị thu hồi từ Web.
- **Tinh giản UI theo web/DESIGN.md:**
  - Loại bỏ hoàn toàn các thông tin kỹ thuật (T20/T21, Epoch, CAS, Keystore).
  - Thu gọn `MainNavigator` thành 2 tab tinh gọn cho T20: **Tổng quan** và **Tài khoản**, tích hợp `useSafeAreaInsets` cho vùng đáy an toàn.
  - Các module chưa có hiển thị thông báo ngắn: "Tính năng đang được hoàn thiện theo lộ trình."

---

## 2. Danh mục File Đã Chỉnh Sửa Trong Đợt Review

- [mobile/src/config/env.ts](file:///D:/dotnet%20project/mobile/src/config/env.ts): Xác thực URL API nghiêm ngặt, bỏ staging hostname giả, validate protocol/userinfo/query/fragment.
- [mobile/src/storage/secureStore.ts](file:///D:/dotnet%20project/mobile/src/storage/secureStore.ts): Fail-closed production, runtime validation TokenPair.
- [mobile/src/api/session.ts](file:///D:/dotnet%20project/mobile/src/api/session.ts): Invalidate epoch đồng bộ, publish inflight promise trước await, Epoch guards đa điểm.
- [mobile/src/api/client.ts](file:///D:/dotnet%20project/mobile/src/api/client.ts): Chặn cross-origin, Epoch guards sau các await, invalidate khi retry lần 2 gặp 401, timeout toàn diện.
- [mobile/src/api/auth.ts](file:///D:/dotnet%20project/mobile/src/api/auth.ts): Chụp token trước khi clear cục bộ, logout với timeout và ghi nhận serverConfirmed chuẩn xác.
- [mobile/src/context/AuthContext.tsx](file:///D:/dotnet%20project/mobile/src/context/AuthContext.tsx): Epoch guards, bảo vệ AppState background/resume, lưu giữ serverRevocationNotice.
- [mobile/src/screens/auth/LoginScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/auth/LoginScreen.tsx): Hiển thị serverRevocationNotice, xóa mật khẩu khi unmount.
- [mobile/src/screens/auth/ResetPasswordScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/auth/ResetPasswordScreen.tsx): Xóa nhạy cảm khi unmount/submit.
- [mobile/src/screens/auth/ActivateAccountScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/auth/ActivateAccountScreen.tsx): Xóa nhạy cảm khi unmount/submit.
- [mobile/src/screens/main/ChangePasswordScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/main/ChangePasswordScreen.tsx): Xóa nhạy cảm khi unmount/submit.
- [mobile/src/screens/main/OverviewScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/main/OverviewScreen.tsx): Bỏ nhãn kỹ thuật, giao diện gọn theo web/DESIGN.md.
- [mobile/src/screens/main/AccountSettingsScreen.tsx](file:///D:/dotnet%20project/mobile/src/screens/main/AccountSettingsScreen.tsx): Tinh giản tài khoản/quyền và đổi mật khẩu/đăng xuất.
- [mobile/src/navigation/MainNavigator.tsx](file:///D:/dotnet%20project/mobile/src/navigation/MainNavigator.tsx): Rút gọn 2 tab Tổng quan / Tài khoản có safe area insets đáy.
- [mobile/src/navigation/RootNavigator.tsx](file:///D:/dotnet%20project/mobile/src/navigation/RootNavigator.tsx): Thêm màn che riêng tư khi app ở chế độ background.
- [mobile/tests/secureStore.test.ts](file:///D:/dotnet%20project/mobile/tests/secureStore.test.ts): Bổ sung mock native và test validate TokenPair.
- [mobile/tests/sessionManager.test.ts](file:///D:/dotnet%20project/mobile/tests/sessionManager.test.ts): Bổ sung test single-flight khi session chỉ ở storage và delay storage write.
- [mobile/tests/apiClient.test.ts](file:///D:/dotnet%20project/mobile/tests/apiClient.test.ts): Bổ sung test chặn cross-origin, response trễ khi đổi account, retry lần 2 gặp 401.
- [mobile/tests/authContext.test.ts](file:///D:/dotnet%20project/mobile/tests/authContext.test.ts): Bổ sung mock native và kiểm thử cách ly tài khoản.
- [docs/mobile-setup.md](file:///D:/dotnet%20project/docs/mobile-setup.md): Cập nhật thông tin package-lock thực tế do Codex sinh, bỏ tuyên bố ảo, nêu rõ quy trình nghiệm thu.
- [docs/verification/T20-anti-progress.md](file:///D:/dotnet%20project/docs/verification/T20-anti-progress.md): Ghi nhận ACK và tiến trình sửa lỗi.

---

## 3. Trạng thái Kiểm thử (NOT_RUN) & Cổng Nghiệm thu Bắt buộc

- **Mã nguồn đã hoàn tất sửa đổi:** `WORK_COMPLETE`.
- **Tình trạng chạy lệnh terminal/npm trong sandbox của Antigravity:** `NOT_RUN` (Tuân thủ nghiêm quy định không tự ý thực thi shell/npm/git trong sandbox).
- **Trách nhiệm của Codex (Runner):**
  1. Chạy `npm test` trong thư mục `mobile/`:
     - Kiểm chứng 5/5 bài test hồi quy trong [mobile/tests/codex-session-review.test.ts](file:///D:/dotnet%20project/mobile/tests/codex-session-review.test.ts) đều chuyển sang **PASS**.
     - Kiểm chứng toàn bộ các test suites khác đều **PASS**.
  2. Chạy `npm run typecheck` để đảm bảo không có lỗi kiểu TypeScript.
  3. Build và chạy ứng dụng trên Android Emulator hoặc thiết bị Android thật (`npm run android`).
  4. Thực hiện các kịch bản nghiệm thu tại [docs/mobile-setup.md](file:///D:/dotnet%20project/docs/mobile-setup.md#5-kịch-bản-kiểm-chứng-nghiệm-thu-bắt-buộc-cổng-nghiệm-thu-android):
     - Đăng nhập thành công và khôi phục phiên (Session Restore) không flash màn hình đăng nhập.
     - Kiểm tra thu hồi phiên (Revoke from Web): Đổi mật khẩu từ Web hoặc gọi `logout-all` -> Xác nhận Mobile bị từ chối 401, xóa SecureStore và quay về Login.
     - Lưu lại video / log evidence làm bằng chứng nghiệm thu thực tế.
- **Ranh giới:** Không tự ý chuyển trạng thái task sang `VERIFIED`. Không chuyển sang Task T21.
