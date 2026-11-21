# Tiến độ triển khai T20 - Mobile Shell & Safe Session

- **Người thực hiện**: Antigravity
- **Thời gian nhận**: 2026-11-15 13:10 (ICT)
- **Cập nhật nhận review từ Codex**: 2026-11-15 13:25 (ICT)
- **Trạng thái**: IN_PROGRESS (ACK Review Fixes)

## 1. Nhiệm vụ & Phạm vi
- **Nhiệm vụ**: Xử lý toàn bộ 5/5 regression failures từ test của Codex (`mobile/tests/codex-session-review.test.ts`), các vấn đề bảo mật nguồn gốc HTTPS, cô lập tài khoản, Epoch guards sau mọi điểm await, Single-flight refresh storage check, đồng bộ invalidation và dọn dẹp storage, timeout bao trọn vòng đời request, fail-closed SecureStore production, và cập nhật tài liệu báo cáo chuẩn xác (không tuyên bố ảo).
- **Phạm vi ghi**:
  - `mobile/**`
  - `docs/mobile-setup.md`
  - `docs/verification/T20-anti-progress.md`
  - `docs/verification/T20-anti-report.md`
- **Quy tắc nghiêm ngặt**:
  - Không sửa backend, web, generated contracts, task files.
  - Không sửa hay xóa `mobile/tests/codex-session-review.test.ts`.
  - Không gọi terminal/npm/dotnet/git/SQL (Codex độc lập làm runner).
  - Không tự ý chuyển trạng thái VERIFIED hay chuyển T21.

## 2. Các điểm cần xử lý chi tiết
- **Fix 1 (Origin check)**: Chặn URL ngoài API origin hoặc chỉ chấp nhận relative path đã kiểm tra trước khi fetch. Validate config bằng URL thật (không userinfo/query/fragment), không default hostname staging giả. Không in raw URL chứa bí mật vào lỗi.
- **Fix 2 (Account switch & delayed response)**: Capture epoch tại đầu request, kiểm tra epoch sau mỗi await (fetch/body parsing/refresh/retry) trước khi trả dữ liệu và trước khi áp dụng lỗi. Delayed 401 của tài khoản cũ không được làm mới hoặc thu hồi tài khoản mới.
- **Fix 3 (Second 401 & credentials invalidation)**: Khi retry sau refresh vẫn bị 401 -> Invalidate session đúng generation và thông báo xóa credentials/che user. `skipAuth` cho forgot/reset/activate không gắn bearer và không kích hoạt refresh.
- **Fix 4 (Logout during delayed refresh write)**: Invalidate epoch đồng bộ (synchronous) TRƯỚC bất kỳ await nào; serialize storage write/clear với check generation trước và sau await. Không để clear cũ xóa session mới, hoặc write cũ ghi đè session mới. Login/restore/setSession cũng cần guards.
- **Fix 5 (Concurrent refresh when session in storage)**: Publish inflight promise trước bất kỳ await nào, bound theo generation. `finally` của refresh A không được xóa inflight của refresh B. Delayed 401 sau khi refresh đã xong sử dụng ngay token mới thay vì khởi tạo refresh thứ hai không cần thiết.
- **Fix khác**:
  - SecureStore fail-closed khi native module không khả dụng trong production (chỉ mock trong test).
  - Kiểm tra runtime TokenPair (tokenType Bearer, expiresIn hữu hạn dương).
  - Logout xóa session ngay, chụp token trước để gọi server logout; giữ thông báo server chưa xác nhận trên LoginScreen/Root.
  - Timeout bao trọn toàn bộ vòng đời (body decode, refresh, retry), kết hợp signal của caller, refresh fetch cũng có timeout.
  - AuthContext epoch guards, che user khi unauthenticated/verifying; AppState background che nội dung, resume xác minh lại server.
  - UI tinh gọn theo web/DESIGN.md: bỏ các nhãn kỹ thuật (T20/T21, Epoch, atomic CAS...), MainNavigator đơn giản hóa 2 tab Tổng quan / Tài khoản có safe area đáy chuẩn.
  - Mock native module trong Jest config/setup cho `expo-secure-store`.
  - Báo cáo cập nhật chính xác (chưa chạy terminal, Codex quản lý lockfile và chạy test, chưa có Android evidence).

## 3. Tiến độ thực hiện vòng sửa
- **Bước 1**: [HOÀN TẤT] Ghi nhận ACK vòng sửa.
- **Bước 2**: [HOÀN TẤT] Sửa `env.ts`, `secureStore.ts`, `session.ts`, `client.ts`, `auth.ts`, `AuthContext.tsx`.
- **Bước 3**: [HOÀN TẤT] Tinh giản UI và sửa `MainNavigator.tsx`, components, screens.
- **Bước 4**: [HOÀN TẤT] Bổ sung Jest mock native và unit tests mở rộng.
- **Bước 5**: [HOÀN TẤT] Cập nhật `docs/mobile-setup.md` và `docs/verification/T20-anti-report.md`.

## 4. Trạng thái kết quả
- Toàn bộ 5 lỗi hồi quy và các khuyến nghị review từ Codex đã được xử lý triệt để.
- Báo cáo kết thúc với trạng thái `WORK_COMPLETE` đã được cập nhật tại `docs/verification/T20-anti-report.md`.
- File `mobile/tests/codex-session-review.test.ts` được giữ nguyên vẹn 100%.
- Bàn giao cho Codex (Runner) chạy `npm test`, `npm run typecheck`, build Android và lưu bằng chứng runtime (video/login/logout/revoke).
