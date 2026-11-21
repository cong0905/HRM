# Hướng dẫn Thiết lập & Kiểm chứng HRM Mobile (T20)

Tài liệu hướng dẫn cấu hình, cài đặt, vận hành và kiểm chứng ứng dụng HRM Mobile (React Native / Expo TypeScript) trên hệ điều hành Android, tập trung vào kiến trúc Khung điều hướng và Quản lý phiên an toàn (Safe Session).

Ngày 15/11/2026, T20 đã VERIFIED kỹ thuật theo ủy quyền: 35 unit, 8 backend auth SQL và 8 native checks trên APK cài trực tiếp PASS. Xem `docs/verification/T20-mobile-session-review.md` và `docs/decisions/T20-delegated-technical-acceptance.md`. Môi trường Android nằm riêng trong `.cache/android-t20/`; SDK/cache được giữ, emulator/API/Metro và SQL fixture đã dừng/dọn. APK kiểm thử x86_64 tại `outputs/hrm-mobile-t20/HRM-T20-test-x86_64.apk` dùng signing test và URL fixture đã dọn, không phải bản phát hành sản phẩm.

Native project `mobile/android/` được sinh bằng `npx expo prebuild --platform android --no-install` và được ignore; cấu hình nguồn nằm ở `mobile/app.json` và `mobile/plugins/`. Để chạy với API thật, đặt `EXPO_PUBLIC_API_URL` tới HTTPS staging hợp lệ, cấu hình `ANDROID_HOME`, `ANDROID_USER_HOME`, `JAVA_HOME` cho process rồi chạy Expo. Không dùng URL example.invalid cho native E2E hoặc suy rằng có sẵn một staging server công khai.

---

## 1. Tổng quan & Phạm vi Task T20

- **Mục tiêu:** Xây dựng khung ứng dụng di động cho Android, kết nối trực tiếp với Backend API qua hợp đồng số thực tế (`contracts/`), lưu trữ bí mật phiên an toàn qua `expo-secure-store`, loại bỏ hoàn toàn `AsyncStorage`, xử lý cơ chế làm mới phiên `Single-Flight` chống race condition với `Epoch Generation`, và bảo vệ giao diện theo phân quyền máy chủ.
- **Phạm vi task T20:** Chỉ triển khai khung điều hướng, phiên an toàn, màn hình định danh/tài khoản (Đăng nhập, Quên mật khẩu, Đặt lại mật khẩu, Kích hoạt tài khoản, Đổi mật khẩu, Tổng quan, Cài đặt tài khoản). Các màn hình nghiệp vụ chi tiết (Chấm công, Đơn nghỉ phép, Phiếu lương) thuộc phạm vi Task T21.
- **Ranh giới thực thi:** 
  - Antigravity phụ trách triển khai mã nguồn trong `mobile/**`.
  - Codex đảm nhận vai trò Runner và Reviewer độc lập: quản lý dependencies, sinh `package-lock.json`, chạy unit tests, build và ghi nhận bằng chứng Android runtime.
  - Việc backend auth đã PASS 8/8 trên database SQL Server riêng là bằng chứng phía máy chủ, **không được coi là bằng chứng E2E của ứng dụng Android**.

---

## 2. Danh mục Phụ thuộc & Quản lý Tương thích (Dependencies & Compatibility)

`package-lock.json` đã được Codex sinh thực tế trong thư mục `mobile/`. Các phiên bản thư viện cốt lõi được cấu hình:

| Thư viện | Phiên bản | Mục đích sử dụng | Ghi chú tương thích |
| :--- | :--- | :--- | :--- |
| `expo` | `~52.0.0` | Framework nền tảng di động | Baseline đề xuất |
| `react` | `18.3.1` | Thư viện giao diện chính | Đồng bộ phiên bản React của Web |
| `react-native` | `0.76.9` | Runtime di động chuẩn Expo SDK 52 | Codex đã hiệu chỉnh từ 0.76.7 lên 0.76.9 |
| `react-native-safe-area-context` | `4.12.0` | Xử lý vùng an toàn (Safe Area) màn hình | Codex đã hạ từ 5.11.1 về 4.12.0 cho Expo 52 |
| `expo-secure-store` | `~14.0.1` | Lưu trữ Access/Refresh Token an toàn | Đã kiểm ciphertext/xóa phiên trên emulator; chưa thiết bị vật lý |
| `expo-asset` | `~11.0.5` | Quản lý tài nguyên hình ảnh/font | Codex bổ sung |
| `hrm-api-contracts` | `file:../contracts` | Hợp đồng DTO & Schema từ Backend | Tích hợp qua tsconfig path mapping |

---

## 3. Cấu hình Môi trường & HTTPS Bắt buộc

### 3.1. Ràng buộc bảo mật mạng
- **Chỉ sử dụng HTTPS:** Adapter từ chối HTTP và URL ngoài API origin. Plugin `mobile/plugins/withHrmNetworkSecurity.cjs` sinh `android:usesCleartextTraffic="false"` trong manifest; không dùng thuộc tính Android config không được Expo hỗ trợ. SecureStore plugin loại bí mật khỏi Android backup. Không tắt TLS validation.
- **Xác thực cấu hình URL nghiêm ngặt:** Biến môi trường `EXPO_PUBLIC_API_URL` được kiểm tra chặt chẽ:
  - Bắt buộc giao thức `https:`.
  - Không chứa userinfo (username/password).
  - Không chứa query string hoặc fragment.
  - Không sử dụng default hostname staging giả lập. Nếu thiếu cấu hình, ứng dụng báo lỗi rõ ràng yêu cầu người dùng thiết lập.
- **Bảo vệ Cross-Origin:** `mobileFetch` kiểm tra nguồn gốc (origin) trước khi thực hiện cuộc gọi mạng. Nếu URL đích khác với `apiOrigin` đã cấu hình, yêu cầu bị chặn ngay lập tức để ngăn ngừa rò rỉ Bearer token.
- **Không in thông tin nhạy cảm vào lỗi:** Các URL có thể chứa tham số nhạy cảm không được in thô vào thông báo lỗi.

### 3.2. Cấu hình biến môi trường
Thiết lập biến môi trường khi chạy:
```bash
# Đối với môi trường kiểm thử Unit Tests:
EXPO_PUBLIC_API_URL=https://api.example.invalid

# Đối với môi trường Staging/Dev thật:
EXPO_PUBLIC_API_URL=https://<your-staging-api-host>
```

---

## 4. Hướng dẫn Chạy Kiểm thử & Nghiệm thu cho Codex (Runner)

### 4.1. Chạy toàn bộ Unit Tests
```bash
cd "D:/dotnet project/mobile"
npm test
```
*Các bộ test bao gồm:*
- `tests/codex-session-review.test.ts`: Các regression độc lập gồm cross-origin, tài khoản cũ, retry 401, logout/login/storage race, restore generation, stalled body và caller cancellation.
- `tests/secureStore.test.ts`: Kiểm tra lưu trữ an toàn, fail-closed khi thiếu native module, xác thực runtime TokenPair.
- `tests/sessionManager.test.ts`: Kiểm tra Single-Flight refresh, 401 rejected, lỗi mạng offline, hủy inflight refresh khi logout.
- `tests/apiClient.test.ts`: Kiểm tra đính kèm Bearer token, retry an toàn cho GET/Idempotency-Key, chặn replay mutation, chặn refresh loop, chặn HTTP không an toàn.
- `tests/authContext.test.ts`: Unit test luồng dịch vụ xác thực/restore/đổi tài khoản bằng mock, không phải test mount React AuthProvider hoặc native E2E.

### 4.2. Kiểm tra TypeScript Typecheck
```bash
cd "D:/dotnet project/mobile"
npm run typecheck
```

### 4.3. Khởi chạy Ứng dụng trên Android
```bash
cd "D:/dotnet project/mobile"
npm run android
```

---

## 5. Kịch bản Kiểm chứng Nghiệm thu Bắt buộc (Cổng nghiệm thu Android)

Code hoàn thành chưa đồng nghĩa với `VERIFIED`. Việc nghiệm thu bắt buộc phải có bằng chứng chạy thực tế trên Android Emulator hoặc thiết bị thật:

### Kịch bản 1: Đăng nhập thành công với Opaque Bearer Token
1. Khởi động app trên thiết bị Android/Emulator.
2. Tại màn hình Đăng nhập, nhập tài khoản hợp lệ.
3. Bấm "Đăng nhập".
4. **Kiểm tra kết quả:**
   - Ứng dụng gọi `POST /api/v1/auth/mobile/login`.
   - Nhận về `TokenPair` gồm `accessToken`, `refreshToken`, `expiresIn`, `tokenType: "Bearer"`.
   - Token được lưu vào `SecureStore`.
   - Ứng dụng gọi `GET /api/v1/access/me` để lấy danh tính, vai trò và danh sách quyền hạn.
   - Chuyển hướng vào màn hình Tổng quan mà không bị flash giao diện.

### Kịch bản 2: Khôi phục phiên khi mở lại App (Session Restore)
1. Sau khi đăng nhập, thoát hoàn toàn ứng dụng (vuốt tắt app).
2. Mở lại ứng dụng HRM Mobile.
3. **Kiểm tra kết quả:**
   - Ứng dụng hiển thị `LoadingScreen` ("Đang khôi phục phiên làm việc...").
   - Đọc token từ `SecureStore`, gọi `GET /api/v1/access/me` để xác thực lại với máy chủ.
   - Màn hình chính xuất hiện ngay sau khi xác nhận. Không có khoảnh khắc nào bị flash màn hình Đăng nhập.

### Kịch bản 3: Thu hồi phiên từ xa từ Web (Device Revoke from Web)
1. Ứng dụng Mobile đang ở trạng thái đã đăng nhập.
2. Trên Web hoặc qua Postman/curl, thực hiện đổi mật khẩu tài khoản (`POST /api/v1/auth/change-password`) hoặc thu hồi phiên (`POST /api/v1/auth/logout-all`).
3. Trên Mobile, thực hiện một thao tác bất kỳ hoặc đưa app từ background trở lại màn hình chính (resume).
4. **Kiểm tra kết quả:**
   - Máy chủ trả về `401 Unauthorized`.
   - Mobile kích hoạt `SessionManager.refreshSingleFlight()` gọi `POST /api/v1/auth/mobile/refresh`.
   - Máy chủ kiểm tra phiên thấy đã bị thu hồi -> Trả về `401 Unauthorized`.
   - Mobile xóa sạch token trong `SecureStore`, tăng Epoch, dọn dẹp state và che dữ liệu riêng tư.
   - Chuyển về màn hình `LoginScreen` với thông báo phiên đã hết hạn hoặc bị thu hồi.

### Kịch bản 4: Đổi tài khoản cách ly Cache
1. Đăng nhập bằng Tài khoản A.
2. Bấm "Đăng xuất" tại màn hình Tài khoản.
3. Đăng nhập bằng Tài khoản B.
4. **Kiểm tra kết quả:** Dữ liệu của Tài khoản A đã bị dọn sạch hoàn toàn, không có bất kỳ thông tin nào của Tài khoản A xuất hiện trên giao diện Tài khoản B.

### Kịch bản 5: Kiểm tra Mất mạng (Offline Resilience)
1. Bật chế độ máy bay (Airplane mode).
2. Mở ứng dụng (đang có phiên hợp lệ).
3. **Kiểm tra kết quả:**
   - Ứng dụng hiển thị `NetworkErrorBanner` và màn hình "Tạm dừng kết nối máy chủ".
   - Token trong `SecureStore` **KHÔNG** bị xóa (phân biệt rạch ròi giữa lỗi mạng và phiên bị thu hồi).
   - Tắt chế độ máy bay và bấm "Thử lại kết nối" -> Ứng dụng kết nối lại bình thường.

---

## 6. Hướng dẫn Kiểm tra Log Che giấu (Masked Logs Verification)

Khi chạy `adb logcat`, sử dụng bộ lọc để rà soát rò rỉ bí mật:
```bash
adb logcat *:S ReactNative:V ReactNativeJS:V | grep -E "token|password|Bearer"
```
**Yêu cầu đạt:** Không có bất kỳ dòng log nào chứa Access Token, Refresh Token, mật khẩu thô của người dùng hay chuỗi token bảo vệ của ASP.NET Data Protection.
