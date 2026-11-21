# T20 — Nghiệm thu kỹ thuật theo ủy quyền

Ngày 15/11/2026 (Asia/Saigon), Codex chấp nhận riêng **T20 VERIFIED về kỹ thuật** theo chỉ đạo của chủ dự án: “khi nào làm xong m sẽ kiểm tra và duyệt giúp t nhé”. Quyền review/nghiệm thu đã ghi tại `T20-delegated-technical-review.md`.

## Căn cứ chấp nhận

- Anti bàn giao triển khai và lượt sửa; Codex kiểm tra độc lập, tái hiện lỗi rồi sửa bổ sung session/storage/account isolation, timeout/cancellation và logout. Không lấy lời tự báo WORK_COMPLETE làm bằng chứng đạt.
- Typecheck PASS; 35/35 unit tests PASS, 0 pending; 8/8 backend auth SQL PASS, 0 skip. Kiểm cả thu hồi do password/reset/disable/inactive/security stamp/revoke-all, cookie/CSRF và hạn refresh.
- APK cài trực tiếp `com.hrmnext.mobile` đã đạt **8/8 checks trong một lượt hoàn chỉnh trên SQL fixture sạch**. Đăng nhập HTTPS thật; SecureStore ciphertext; force-stop/cold restore; revoke bằng web cookie + CSRF chặn app; tài khoản thứ hai không nhận danh tính cũ; offline che dữ liệu; retry lấy lại đúng tài khoản; logout offline xóa kho và báo server chưa xác nhận; log scan không thấy credential pattern.
- Android9/API28 x86_64 trên AVD HRM_T20_API28, WHPX. Không cộng các kết quả rời từ các lượt harness chưa đạt. Fault injection đóng process sau khi ngắt adb reverse để loại HTTPS keep-alive; không giảm assertion khi giả lập mất mạng chưa đúng.
- Manifest thực trong APK cấm cleartext và loại SecureStore khỏi backup. Bundle trong APK khớp bundle generated cuối. APK SHA-256: `d04f652b59dfa064e6b4492e7866f08c78269fa4b7f4f8e3fda8f1d420dac2f6`.
- Đã đối chiếu lại SHA-256 của 42 file nguồn/cấu hình với evidence trước khi ký quyết định. Generated API contract không đổi so với baseline giao T20.
- SQL GUID của các fixture đã dọn; credential/cert tạm đã xóa; CA test gỡ khỏi emulator; API/Metro/emulator đã dừng. SDK/cache riêng trong dự án được giữ theo lựa chọn của chủ dự án. Không dùng HRM_System, không gửi email thật.

## Bằng chứng

- Báo cáo: `docs/verification/T20-mobile-session-review.md`.
- Manifest kết quả không sửa sau kiểm: `docs/verification/t20/result.json`; unit/TRX/native run và PNG/XML/video cùng thư mục.
- Manifest chấp nhận, SHA evidence và acceptedSourceSha256: `docs/verification/t20-acceptance/result.json`.
- Manifest/backup audit: `docs/verification/t20/apk-manifest-audit.json`; cleanup: `docs/verification/t20/resource-cleanup.json`.
- APK kiểm thử: `outputs/hrm-mobile-t20/HRM-T20-test-x86_64.apk`.

## Giới hạn của quyết định

Đây là nghiệm thu kỹ thuật được ủy quyền, không phải human UAT, remote CI/deploy, iOS/Android ARM64 vật lý hoặc phát hành Play Store. APK dùng signing test, chỉ x86_64 và URL HTTPS fixture đã được dọn; không phải bản phát hành kết nối dữ liệu sản phẩm. Expo52/RN0.76 là baseline đã pin của lượt này, không xác nhận là phiên bản mới nhất hoặc còn được hỗ trợ.

T18 giữ READY_FOR_REVIEW; T19 giữ VERIFIED. T21 chưa bắt đầu. Quyết định không mở rộng quyền nghiệm thu sang task khác.
