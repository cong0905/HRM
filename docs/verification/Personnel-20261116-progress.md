# ACK & Tiến độ Hoàn thiện Luồng Nhân viên (Personnel) - 2026-11-16

- **Người thực hiện:** Antigravity
- **Thời gian:** 2026-11-16
- **Trạng thái:** HOÀN THÀNH SỬA FINDINGS (Sẵn sàng bàn giao cho Codex kiểm tra)

## 1. ACK & Phạm vi tiếp nhận
- Tiếp tục đúng conversation và phạm vi nhiệm vụ Personnel-20261116. Không mở rộng.
- Không tạo agent; không chạy lệnh terminal/npm/dotnet/SQL trong lượt này. Codex sẽ chạy kiểm tra sau khi bàn giao.
- Khắc phục triệt để 6 Finding bắt buộc:
  1. Fix Typecheck & Lint: Gỡ unused imports/variables, dùng đúng type contract, sửa any tại `shared/api.ts` và test lines 131/136; sửa assertion `not.toHaveProperty`; sửa provider test với auth/organization stub nhất quán.
  2. Phân biệt 409 conflict: Chỉ coi là conflict cần so sánh khi UPDATE có baseline và mã lỗi `resource.conflict`. Với Create bị 409 (duplicate email/username/CCCD), giữ form cho sửa và gửi lại, không gọi `get(baseline!.id)` khi baseline rỗng. Thêm test create 409 duplicate_email -> sửa -> submit thành công.
  3. Bảo vệ dữ liệu nhạy cảm khi 401/403: Ẩn editor/profile/comparison và tắt mutation khi bị từ chối quyền; purge cache liên quan; không tạo vòng lặp refetch. Khi list 403, đóng/ẩn drawer và modal chứa PII. Khi offboard 403, ẩn PII và chặn gửi lại. Thêm test regression với preloaded cache và thu hồi scope giữa phiên.
  4. Phân quyền Profile khi truy vấn danh mục: Chỉ gọi `organizationApi.list` khi có quyền `departments.read` / `positions.read`, không retry 403, không hiển thị tên danh mục cache khi query lỗi quyền.
  5. Giữ nguyên contract accessibility/flow theo browser suite `web/e2e/personnel.spec.ts`: Nhãn "CCCD", "Lý do thay đổi", success text "Đã lưu hồ sơ nhân viên.", default tab mở hồ sơ HR là tab edit.
  6. Xử lý ngày tháng theo múi giờ Việt Nam: Dùng `Asia/Ho_Chi_Minh` cho ngày hiện tại thay vì `toISOString` UTC, không đặt quy tắc tuyển dụng/tuổi mới.

## 2. Trạng thái các bước
- [x] Bước 1: Cập nhật file progress ghi nhận tiếp nhận findings.
- [x] Bước 2: Sửa `web/src/features/shared/api.ts` (xóa `any`, type chặt chẽ `cleanParams`).
- [x] Bước 3: Sửa `web/src/features/personnel/PersonnelFeedback.tsx` (xóa unused import `React`).
- [x] Bước 4: Sửa `web/src/features/personnel/PersonnelPage.tsx`:
  - Khắc phục ngày sinh dùng giờ `Asia/Ho_Chi_Minh` (`getTodayVn`).
  - Khôi phục chính xác các nhãn và text theo hợp đồng E2E: nhãn `CCCD`, `Lý do thay đổi`, text `Đã lưu hồ sơ nhân viên.`.
  - Thiết lập default tab `edit` cho HR khi mở Drawer chi tiết để giữ flow E2E, đồng thời duy trì tab `details`.
  - Phân quyền `departments.read` trong `Profile` trước khi query danh mục, `retry: false`, không dùng cache khi query lỗi.
  - Phân biệt 409 conflict so sánh chỉ khi UPDATE có `baseline` và code `resource.conflict`. Khi Create 409 duplicate, giữ form, cho sửa và re-submit, không gọi `get(baseline!.id)`.
  - Ẩn hoàn toàn PII trong DOM & state khi 403/401, không tạo vòng lặp refetch (dùng `setQueryData(null)`).
  - Khi list 403, tự động đóng Drawer và Modal chứa PII.
  - Khi offboard 403, ẩn PII khỏi Modal và chặn gửi lại.
- [x] Bước 5: Cập nhật `web/test/personnel-20261116.test.tsx`:
  - Xóa toàn bộ `any`, dùng `Awaited<ReturnType<typeof personnelApi.create>>`.
  - Xóa unused variable/parameter (`userProfile`, `mockEmployeeOffboarded` được sử dụng đầy đủ).
  - Sử dụng `not.toHaveProperty` cho các trường không thuộc `EmployeePatch`.
  - Sửa `getByText` với non-null assertions để tránh `string | undefined`.
  - Stub đầy đủ auth, csrf, organization trong provider để kiểm soát 100% request.
  - Bổ sung ca kiểm thử: Create nhận 409 `identity.duplicate_email` -> sửa email -> submit thành công (khẳng định `personnelApi.get` không được gọi).
  - Bổ sung ca kiểm thử: Preloaded cached data bị thu hồi scope giữa phiên (khẳng định PII bị ẩn, drawer đóng, không loop refetch).
  - Bổ sung ca kiểm thử: Profile không gọi API phòng ban khi người dùng là Employee thiếu quyền `departments.read`.
- [x] Bước 6: Cập nhật báo cáo bàn giao `docs/verification/Personnel-20261116-report.md`.
