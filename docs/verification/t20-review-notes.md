# T20 — ghi chú kiểm tra độc lập đang thực hiện

Ngày 15/11/2026, chủ dự án ủy quyền Codex theo dõi Anti, kiểm tra và nghiệm thu T20 khi đủ tiêu chí. Đây chưa phải biên bản chấp nhận.

## Bằng chứng đã chạy

- Backend auth: 8/8 PASS, 0 skip. Filter gồm CommittedRevocation_RejectsExistingCookieBearerAndRefresh, ApprovedAuthLifetimesAndSevenDayRefreshExpiry_AreEnforced và WebCookie_CsrfAndLogout_AreEnforced (substring khớp tổng 8 tests). TRX: t20/t20-auth.trx.
- SQL Server 16, database GUID riêng do SqlDatabaseFixture tạo; t20/sql-auth-ledger.json xác nhận cleanupCompleted=true. Không dùng HRM_System.
- Lượt đầu trong sandbox không kết nối testhost, bị abort trước khi chạy; lượt tiếp theo ngoài sandbox đạt. Không gộp hai lượt thành PASS.

## Cần kiểm lại trên bản Anti bàn giao

- Epoch hiện thay đổi sau await storage; invalidate/setSession và lưu refresh có thể đua nhau. Cần tái hiện deferred writes, logout khi login/refresh/restore/access fetch đang bay, đổi account và delayed 401 của account trước.
- Single-flight có await getSession trước khi gán inflightRefresh; cần test nhiều callers khi memory session chưa nạp và delayed 401 sau refresh trước đã xong.
- client.ts nhận URL tuyệt đối ngoài API origin và gắn bearer; cần chặn origin khác trước fetch. Không replay mutation chỉ vì caller đặt header nếu contract không đảm bảo idempotency.
- Response retry 401 cần hủy phiên, che user. skipAuth endpoints không được kích refresh cũ. Request timeout phải gồm refresh/retry và kết hợp caller signal.
- SecureStore không nên fallback tự động sang memory ở production; test mock phải tách biệt. Kiểm partial writes và đọc/ghi session nhất quán.
- AuthContext cần bảo vệ generation cho bootstrap/login/access/changePassword; permission check chỉ hợp lệ khi authenticated; background/resume phải xác minh lại server.
- logout phải vô hiệu hóa local ngay, gửi revoke với captured token. Không coi mọi 401 là server đã xác nhận revoke: client có thể tự tạo 401 sau refresh nhưng chưa revoke.
- API URL mặc định hiện là hostname placeholder; cần fail rõ khi chưa cấu hình, không hứa staging tồn tại. Parse URL thật, chặn userinfo và thông tin bí mật trong error.
- Dependencies Expo52 do Anti đề xuất, chưa kiểm tương thích và chưa xác nhận support; cần doctor/typecheck và kiểm Android build.

## Cổng nghiệm thu còn thiếu

Chưa có Android SDK/emulator tại các vị trí phổ biến và adb không có trên PATH. Đã hỏi chủ dự án chọn emulator riêng trong dự án hoặc điện thoại USB. Chưa có bằng chứng Android, video, device logs hoặc HTTPS staging đáng tin cậy; không được VERIFIED chỉ từ code/unit tests.

## Cập nhật review độc lập

- Anti đã xử lý 5 lỗi ban đầu. Codex tiếp tục sửa serialization của ghi/xóa session, lưu phiên thành một record mã hóa nhất quán, guard login/restore, không đọc counter persisted đè generation trong process, revalidate trước khi mở nội dung khi resume, timeout gồm cả body và cancellation.
- Logout chỉ nhận serverConfirmed khi server trả 204; 401 có thể chỉ do access token hết hạn. Nếu storage delete thất bại vẫn gửi revoke bằng captured token và báo localCleared=false cho UI. Không nuốt lỗi xóa rồi tuyên bố đã xóa sạch.
- Unit cuối 35/35 PASS, 0 pending; typecheck PASS. Bằng chứng `t20/unit-final.json`. Đây là isolated unit tests, không thay native E2E.
- Đã tải và kiểm checksum emulator/platform-tools/image chính thức vào cache riêng; WHPX sẵn có, không bật Windows feature/cài driver. Android9/API28 x86_64, AVD HRM_T20_API28, serial emulator-5580.
- Expo Go52 đã thực chạy API HTTPS thật + SQL GUID riêng, khôi phục sau force-stop, web-cookie/CSRF logout-all đưa app về login; native SecureStore có ciphertext và được xóa sau revoke; scan log không thấy fixture password/token pattern. Video và XML/PNG trong t20/. Không tính các thao tác UI automation thất bại là PASS.
- Kết quả cuối: APK com.hrmnext.mobile đạt 8/8 native checks trong một lượt fixture sạch; SQL/temp/API/Metro/emulator đã dọn/dừng và CA test gỡ khỏi emulator. VERIFIED kỹ thuật theo `../decisions/T20-delegated-technical-acceptance.md`. Không cộng các lượt harness chưa đạt; mô phỏng offline dùng cold restart sau khi ngắt reverse để đóng kết nối keep-alive.
