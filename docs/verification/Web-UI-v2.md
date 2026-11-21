# Web UI v2 — Áp dụng thiết kế vào HRM React

Ngày kiểm chứng: 14/11/2026. Phạm vi: giao diện web, không thay đổi logic API/backend hoặc trạng thái nghiệm thu các task.

## Đã áp dụng

- Khung ứng dụng: menu sáng rộng 216px, màu xanh #3157D5, nền trắng, đường phân cách nhẹ và tên menu ngắn.
- Tổng quan: danh sách lối tắt theo quyền và thông tin tài khoản dễ đọc; bỏ UUID, danh sách mã quyền và thông tin cookie/CSRF khỏi nội dung chính.
- Đăng nhập, quên/đặt lại/kích hoạt mật khẩu dùng chung AuthFrame và CSS; form đổi mật khẩu đồng bộ phong cách. Giữ nguyên xử lý form, xác thực và thông báo lỗi.
- Desktop/mobile: header, drawer, tên tài khoản dài, nút đổi mật khẩu và vùng nội dung được kiểm tra ở 1440px và 375px. Thêm liên kết bỏ qua điều hướng và focus rõ ràng.
- Trang nghiệp vụ chưa tích hợp giữ thông báo đang hoàn thiện, không đưa các dòng nhân viên/số liệu minh họa của Stitch vào ứng dụng thật.

Thiết kế tham chiếu: Stitch Product UI v2, project `3656751257367590154`. Hướng dẫn cho các trang tiếp theo ở `web/DESIGN.md`; kiểu dùng chung ở `web/src/styles/product.css`, token Ant Design ở `web/src/main.tsx`.

## Kiểm chứng trên bản mã cuối

| Kiểm tra | Kết quả |
|---|---|
| npm run typecheck | PASS |
| npm run lint | PASS |
| npm run build | PASS |
| npm test -- --reporter=dot | 20/20 PASS |
| Chrome, API thật, fixture SQL riêng | 21 kiểm tra PASS, 0 page errors |
| Hash auth/API/router/dependencies | 7/7 tệp không thay đổi |
| graphify update . | PASS; 13.030 nút, 22.728 cạnh, 797 nhóm |

Log, JSON, ảnh và diff: `docs/verification/runs/Web-UI-v2/`. Các kiểm tra browser gồm redirect khi chưa đăng nhập, đăng nhập/đăng xuất bằng API thật, phân quyền SystemAdmin/Payroll, drawer mobile, form reset xóa token khỏi URL, không tràn ngang và tên tài khoản dài không bị xuống từng ký tự.

Các lượt component test gặp timeout 5 giây lặp lại ở ca SystemAdmin sau khi chuyển assertion sang tìm role trên toàn trang; chạy riêng file phân quyền đạt 7/7. Đã sửa assertion để truy vấn trực tiếp vùng điều hướng được đặt nhãn, kiểm tra đủ các link được phép và bị cấm với tên menu mới; lần cuối chạy đầy đủ đạt 20/20. Không nâng timeout hoặc bỏ assertion. Build còn cảnh báo bundle lớn hơn 500KB; một số test phiên làm việc còn cảnh báo act của môi trường test. Các cảnh báo này không được che đi.

## Giới hạn

Đây là áp dụng phong cách vào các màn hiện có, không phải hoàn tất CRUD nhân sự/lịch/công/phép/lương của T17–T19. Chưa có danh sách nhân viên thao tác với dữ liệu thật trên web. Chưa chạy lại full gate backend, CI từ xa hoặc deploy vì không đổi backend. Các thay đổi cũ trong repo được giữ nguyên.

Chrome mở bản demo sử dụng database GUID do test harness tạo; database đó tồn tại trong thời gian demo và được harness tự dọn khi dừng/hết hạn. Không dùng database HRM_System. Không coi demo đang mở là bằng chứng cleanup đã hoàn tất.

## Bằng chứng và đối chiếu

- `checks.json`: mã thoát của bốn bước kiểm tra và đường dẫn log.
- `result.json`: 21 kết quả Chrome và database fixture được dùng.
- `preservation-check.json`: hash đối chiếu client, auth, AuthContext, ProtectedRoute, router và dependency manifests.
- `changes.json`, `ui-v2.patch`: tệp web thay đổi, hash và diff so với bản trước khi áp dụng.
- Ảnh: `dashboard-1440.png`, `dashboard-375.png`, `login-375.png`, `menu-375.png`, `payroll-1440.png`.

Bản sao nguyên gốc được giữ tại `C:\Users\nguye\Documents\Codex\2026-11-12\l-m\work\hrm-ui-v2\before`.
