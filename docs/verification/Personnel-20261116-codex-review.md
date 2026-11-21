# Personnel — Codex review, 16/11/2026

Trạng thái: **READY_FOR_REVIEW**; chưa VERIFIED, chưa human UAT.

## Kết quả triển khai

Antigravity triển khai theo `docs/ai-handoff/Personnel-20261116.md`, sau đó nhận một vòng sửa theo `Personnel-20261116-revise1.md`, cùng conversation `3a6c8388-873b-49ee-8769-c82c92e8cb9b`. Codex review source, chạy checks và hoàn thiện các lỗi còn lại về giữ thông báo quyền, đóng drawer/modal và xóa cache trên toàn trang, hủy truy vấn đang chạy, cùng fixture kiểm thử.

- Danh sách dùng phân trang/lọc server; tham số rỗng được loại bỏ, có đặt lại bộ lọc.
- Form thêm/sửa giữ dữ liệu khi lỗi, kiểm tra ngày theo Asia/Ho_Chi_Minh và contract hiện có, khóa nút lưu khi đang gửi.
- Lỗi 409 trùng email/CCCD/username khi tạo mới cho sửa và gửi lại; chỉ update `resource.conflict` mới bắt đối chiếu phiên bản. Không tự retry mutation.
- Drawer có tab chi tiết và sửa, giữ mặc định sửa cho HR; nhân viên đã nghỉ việc chỉ đọc. Hồ sơ cá nhân không gọi danh mục khi thiếu permission.
- Khi GET/update/compare/offboard nhận 401/403, giao diện chặn thao tác, giữ thông báo lỗi và ẩn PII; đóng các overlay có PII khi lỗi truyền lên trang. Các cache employees/detail/labels/picker được xóa dữ liệu và hủy truy vấn đang chạy để tránh điền lại dữ liệu cũ.

## Kiểm chứng Codex thực chạy

- Build harness `backend/tests/HRM.T17BrowserReview`: PASS, 0 warning, 0 error. Không sửa backend trong nhiệm vụ này.
- Web typecheck: PASS sau sửa cuối. Lint: PASS sau sửa cuối.
- Production build: PASS. Bundle vẫn cảnh báo chunk >500 kB (khoảng 1.36 MB trước gzip).
- Toàn bộ unit/component: **48/48 PASS**, 8 file, 0 skip. Bằng chứng `personnel-20261116-dispatch/web-tests-accepted.json`.
- `graphify update .`: PASS, exit0; graph cuối 15.214 nodes / 26.389 edges. Công cụ cảnh báo parser cho file payroll `MyPayslips.tsx` không thuộc thay đổi này; không diễn giải graph là bằng chứng build (typecheck/build được chạy riêng).
- Test có warning `act(...)` ở auth hiện có và warning AntD useForm khi đóng overlay; không coi đây là audit accessibility hoàn chỉnh.

Các lượt thất bại được giữ: baseline 33/33; bản Anti đầu 38/44, typecheck/lint FAIL; lượt tiếp 43/48 và targeted 17/18. Codex sửa mock fetch bị ghi đè, test nhập ngày bị validator chặn trước khi tới server, trigger refetch thật, và assertion cache chấp nhận mục đã được GC xóa. Năm ca UI phức tạp có timeout riêng 20 giây vì runtime thực 5–10 giây; không đổi timeout toàn suite hoặc bỏ assertion.

## Browser thật

Runner `scripts/verify_personnel_20261116.py` tái sử dụng bốn ca hiện có cùng một ca thu hồi scope mới. Dùng Edge, Kestrel HTTPS, SQL database GUID và tài khoản `example.invalid` tạm. Không mock API, không đổi policy, không gửi email thật. Runner tự dừng server và dọn fixture.

Hai lượt đầu đều 3 PASS/2 FAIL; xem `playwright-review1.json`, `playwright-review2.json` và `browser-run-review*.json` trong thư mục evidence. Lượt đầu hết thời gian ở ca nhiều phiên; lượt hai timeout đăng nhập 10 giây, kéo theo ca scope không còn target sau offboard. Runner cuối chờ đăng nhập 30 giây, đặt timeout riêng 120 giây cho hai ca nhiều phiên, kiểm scope trước offboard và phục hồi phân công fixture để các ca độc lập. Giữ nguyên các điều kiện kiểm tra.

Kết quả lượt cuối: **5/5 PASS**, 0 skip, cùng một lượt chạy. `personnel-20261116-dispatch/playwright.json` và `browser-run.json` ghi PASS, exitCode0, cleanupCompleted=true. Các ca:

1. HR tạo mới; GET hồ sơ chưa được phân công nhận403; quản trị cấp scope; PATCH chỉ trường thay đổi; hai phiên HR tạo xung đột rowVersion SQL thật; giữ bản nhập, đối chiếu và lưu lại thành công.
2. Employee chỉ xem hồ sơ riêng; direct URL nhân sự/quản trị bị chặn; viewport375px.
3. Danh sách và drawer nhân viên không tràn ngang ở375px/landscape; Escape đóng drawer.
4. Đang mở editor thì quản trị thu hồi toàn bộ scope của HR. PATCH nhận403; editor và tên nhân viên cũ biến mất; GET detail403, list200 với items rỗng. Cấp lại scope trong fixture trước ca offboard để các ca không phụ thuộc tác dụng phụ này.
5. Offboard bằng UI HR: cookie web, bearer mobile và refresh token trước đó đều bị từ chối401; bearer đã được kiểm200 trước khi offboard.

Đã xem ảnh trực tiếp drawer mobile và màn đối chiếu xung đột. Ảnh trong `personnel-20261116-dispatch/`: `personnel-drawer-mobile.png`, `personnel-conflict.png`, `scope-revoked.png`. Dữ liệu đều là fixture.

## Giới hạn

Không remote CI, deploy, migration lên database sản phẩm, email thật hoặc human UAT. Không tự thay trạng thái task/backlog thành VERIFIED. HR không có phân công nhận danh sách rỗng; GET/PATCH nhân viên ngoài phân công phải nhận403. Không diễn giải danh sách rỗng thành lỗi403 của endpoint list.
