# T20 — Review độc lập khung Android và phiên an toàn

Ngày kiểm chứng: 15/11/2026, Asia/Saigon. Chủ dự án đã ủy quyền Codex kiểm tra, sửa và nghiệm thu riêng T20 tại `docs/decisions/T20-delegated-technical-review.md`.

Trạng thái: **VERIFIED về kỹ thuật theo ủy quyền**. Quyết định: `docs/decisions/T20-delegated-technical-acceptance.md`.

## Phạm vi đã triển khai

Anti tạo khung Expo/React Native TypeScript, màn hình đăng nhập/quên/reset/kích hoạt/đổi mật khẩu, shell tổng quan/tài khoản, SecureStore và adapter dùng DTO generated. Không triển khai nghiệp vụ T21. Backend nghiệp vụ, web và generated contracts không thuộc phần sửa sản phẩm T20.

Codex kiểm tra độc lập và sửa các vấn đề sau:

- Chặn bearer đi sang origin khác; public auth routes không đính kèm credential hoặc kích refresh của tài khoản hiện tại.
- Bỏ kết quả request cũ khi logout/đổi tài khoản; generation tăng đồng bộ, các thao tác đọc/ghi/xóa phiên được xếp hàng và kiểm generation. Một record SecureStore tránh cặp token bị ghi lệch nhau.
- Single-flight được công bố trước await; không đọc lại token đang bị logout xóa, không làm sống lại phiên hoặc xóa nhầm phiên tài khoản mới. Cold restore không lấy counter persisted ghi đè generation trong process.
- Request chỉ retry một lần sau refresh; retry vẫn 401 thì hủy phiên. Timeout/caller cancellation bao gồm body decode và chờ refresh/retry, không chỉ chờ response headers.
- Logout che dữ liệu ngay và dùng captured token để thu hồi server. HTTP401 không được coi là bằng chứng refresh token đã bị thu hồi. Nếu xóa storage thất bại vẫn thử server revoke và báo rõ trạng thái xóa cục bộ.
- Resume xác minh lại trước khi mở dữ liệu riêng. Native Back/safe area hoạt động cho shell hai tab. Bỏ bảng permission/UUID và nhãn implementation khỏi giao diện tài khoản.
- Căn React Native0.76.9 và safe-area4.12.0 theo Expo52; thêm expo-asset, bỏ dependency không dùng, sinh lockfile thật. Plugin sinh manifest cấm cleartext; SecureStore plugin loại secret khỏi Android backup.

## Kiểm chứng đã chạy

| Cổng | Kết quả và bằng chứng |
| --- | --- |
| TypeScript | PASS, `npm run typecheck` trên mã cuối |
| Unit | 35/35 PASS, 0 pending/skip; `t20/unit-final.json`. Bao phủ race ghi/xóa/refresh/login, account isolation, timeout/cancellation, public password routes và lỗi logout |
| Backend auth SQL | 8/8 PASS, 0 skip; `t20/t20-auth.trx`. Kiểm cookie/CSRF/logout, hạn refresh và revoke do disable/inactive/stamp/password/reset/revoke-all |
| Build APK | PASS; `t20/android-build.log`. Build đầu cold: 37m24s; build cập nhật bundle cuối: 3m26s. Không coi build thành công là native E2E |
| Native APK | 8/8 PASS trong một lượt hoàn chỉnh trên SQL fixture sạch, `t20/apk-run.json`; các lượt trước chưa đạt đã lưu riêng, không cộng kết quả rời |
| SQL cleanup | Auth và native fixture cleanupCompleted=true; temp/CA/API/Metro/emulator được xác nhận dọn/dừng tại `t20/resource-cleanup.json` |

Unit tests dùng mock và không thay native E2E. `authContext.test.ts` kiểm luồng dịch vụ, không phải test mount React AuthProvider.

## Môi trường và bằng chứng Android

- AVD `HRM_T20_API28`, serial `emulator-5580`, Android9/API28 x86_64, WHPX sẵn có, headless 480×800. SDK/AVD/cache ở `.cache/android-t20/`; không bật Windows feature hoặc cài driver.
- APK package `com.hrmnext.mobile`, release bundle, ký bằng key test của template, chỉ ABI x86_64. Không phải bản phát hành sản phẩm. APK nhúng URL fixture HTTPS localhost; fixture được xóa sau kiểm.
- API Kestrel/Identity thật, SQL Server16 với database GUID do fixture tạo và tự dọn. Không dùng `HRM_System`, không gửi email thật. Root CA một lần chỉ được tin bên trong emulator; adapter/app không tắt TLS validation.
- Runner xác minh bytecode trong APK khớp bundle generated cuối và chứa thay đổi logout mới; SHA-256 APK/bundle trong `t20/apk-run.json`.
- Aapt đọc manifest thật trong APK: cleartext=false, package/ABI đúng và XML backup loại SecureStore; resource paths release đã đổi tên được tra qua bảng resources, không suy từ tên source. Bằng chứng `t20/apk-manifest-audit.json`.
- PNG/XML: `t20/apk-authenticated.*`, `apk-restored.*`, `apk-revoked.*`, `apk-other-account.*`, `apk-offline.*`, `apk-online-restored.*`, `apk-logout-offline.*`. XML che password; video ở `t20/apk-*.mp4`.
- Native storage audit đọc ciphertext bên trong emulator, chỉ lưu kết quả kiểm, không lưu token/ciphertext. Device log scan chạy trong bộ nhớ, chỉ lưu kết quả pattern scan; không khẳng định đây là kiểm tra mọi dạng rò rỉ có thể có.
- Fault injection offline phải ngắt adb reverse **và cold restart app** để đóng HTTPS keep-alive cũ; chỉ xóa forward chưa chứng minh mất kết nối. Logout offline được kiểm từ màn khôi phục phiên không kết nối được server. Các lượt harness đọc XML cũ/nhãn Android viết hoa hoặc còn keep-alive được giữ riêng, không được tính là kết quả native xanh.

## Chạy lại

`docs/mobile-setup.md` mô tả môi trường. Các runner chính:

```powershell
python -B scripts/prepare_t20_android.py --runtime --api 28
python -B scripts/prepare_t20_android.py --command-tools
./scripts/start_t20_emulator.ps1
python -B scripts/t20_android_build.py sdk
```

Trong `mobile/`, chạy `npm ci --ignore-scripts`, rồi `npx expo prebuild --platform android --no-install` để tái sinh native project (thư mục `mobile/android/` là generated và được ignore). Build/chạy `backend/tests/HRM.T20MobileReview` với `HRM_T05_SQLSERVER` chỉ tới SQL test. Sau marker READY, chạy `python -B scripts/t20_device_review.py prepare`, `python -B scripts/t20_android_build.py build`, rồi `python -B scripts/verify_t20_apk.py` từ repository root. Runner native chỉ nhắm emulator-5580 và xóa dữ liệu **package test** trước lượt chạy. Dừng host qua stopFile trong metadata để dọn SQL/temp. Không tái dùng credential/cert đã hết vòng đời.

## Giới hạn

Chưa human UAT, remote CI/deploy, Android ARM64 vật lý, iOS hoặc signing sản phẩm. Expo52/RN0.76 là baseline đã pin của lượt này; kết quả không xác nhận đây là phiên bản framework mới nhất hoặc còn được hỗ trợ. Toolchain còn cảnh báo SDK XML/deprecation/Hermes từ dependency; không nâng Gradle hoặc framework ngoài T20. T18 giữ READY_FOR_REVIEW; T19 giữ VERIFIED; chưa bắt đầu T21.

Graphify cập nhật code graph (15.107 node, 26.219 cạnh), còn parser warning cũ với `web/src/features/payroll/MyPayslips.tsx`, ngoài scope T20. Không diễn giải code update thành đã tái tạo semantic docs.

Manifest evidence `t20/result.json` có SHA-256 của 42 file nguồn/cấu hình. Manifest chấp nhận riêng tại `t20-acceptance/result.json`. APK kiểm thử được giữ tại `outputs/hrm-mobile-t20/HRM-T20-test-x86_64.apk`; signing test, URL fixture đã dọn, không phải bản release sản phẩm.
