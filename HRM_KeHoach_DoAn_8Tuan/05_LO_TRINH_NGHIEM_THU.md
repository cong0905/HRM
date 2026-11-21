# Lộ trình 8 tuần và tiêu chí nghiệm thu

Đây là ngân sách đề xuất cho một người kiểm duyệt với AI hỗ trợ. Thời gian AI sinh code thường ngắn hơn thời gian làm rõ nghiệp vụ, chạy môi trường và phát hiện lỗi. Lịch phải được cập nhật bằng kết quả cuối tuần, không đo bằng số task AI tự báo xong.

## Nhịp làm việc

- Mỗi ngày: chọn một task đủ phụ thuộc; xem scope trước; chạy kết quả; thử ít nhất một trường hợp sai; lưu quyết định.
- Mỗi lần bàn giao: dành 10–15 phút chạy lại bước AI hướng dẫn; task tiền/quyền cần review kỹ hơn theo ngân sách backlog.
- Mỗi tuần: một buổi demo 30–60 phút, cập nhật báo cáo/ERD/ảnh/test; một buổi ước lượng lại và cắt phần trang trí nếu trễ.
- Tuần7–8: ưu tiên sửa lỗi tích hợp, tài liệu, rehearsals; không tiêu hết dự phòng vào tính năng mới.

## Cổng theo tuần

| Mốc | Phải chứng minh | Nếu chưa đạt |
|---|---|---|
| Cuối tuần1 | Build baseline, DB test dựng/nâng cấp được, fixture và dependency path rõ | Dừng thêm UI; sửa môi trường/DB trước |
| Cuối tuần2 | Cookie/bearer hoạt động, revoke thật, data scope và hồ sơ đúng | Không nối app vào auth tạm hoặc tự viết token để kịp |
| Cuối tuần3 | Công không trùng, phép không âm khi duyệt đồng thời, lịch chung | Giảm dashboard/export, giữ thời gian cho transaction |
| Cuối tuần4 | Công thức fixture đúng, kỳ chốt bất biến, HR web nhân sự dùng được | Chốt mobile tối thiểu hồ sơ+phép; không nhân đôi lỗi lương sang mobile |
| Cuối tuần5 | Web trọn luồng, Android đăng nhập thực | Bỏ biểu đồ/PDF/push; ưu tiên app ổn định |
| Cuối tuần6 | Mobile scope đã chốt dùng API thật, email test thật, migration rehearsal | Đóng scope, đưa phần chưa xong vào known gaps; không gắn nhãn hoàn thành |
| Cuối tuần7 | Staging + E2E + backup restore có evidence | NO-GO pilot; bản bảo vệ chỉ tuyên bố phạm vi đã kiểm chứng |
| Tuần8 | UAT, freeze, tài liệu khớp code, video dự phòng, tự giải thích được | Chỉ bugfix; trao đổi giảng viên nếu thiếu yêu cầu bắt buộc |

## Thứ tự cắt khi trễ

1. Hoãn mọi P01–P07; không mở lại chatbot/tuyển dụng/đào tạo vì thấy dễ làm UI.
2. Giảm dashboard về vài số chính; export CSV thay PDF; không push/in-app chat.
3. Android trước, không hứa iOS; app nhân viên không có trang HR/Payroll.
4. Nếu vẫn trễ cuối tuần4: đề nghị giảng viên/chủ dự án chốt mobile chỉ hồ sơ+phép, giữ công/lương đầy đủ trên web. Cập nhật T21/G3/G4/test tương ứng trước khi gọi PASS.
5. Nếu backend tiền/quyền chưa đúng: không dùng dữ liệu thật; giảm phạm vi đề tài được duyệt. Không bỏ kiểm quyền, transaction, backup hoặc bịa kết quả test để giữ số tính năng.

## Ma trận truy vết nghiệm thu

| ID yêu cầu | Kịch bản thực hiện | Task | Kết quả bắt buộc |
|---|---|---|---|
| R01 | A đổi URL/ID thành hồ sơ/phiếu lương B | T07,T24 | 403/404 theo policy; không lộ dữ liệu |
| R02 | Đăng nhập web+mobile, khóa account rồi gọi tiếp/refresh | T06,T20,T24 | Bị chặn theo SLA revoke đã chốt; không nhận session mới |
| R03 | Hai HR duyệt hai đơn tổng lớn hơn quỹ | T11,T12 | Không âm quỹ, quyết định thất bại không để lại partial update |
| R04 | Check-in timeout sau commit rồi retry | T10,T21 | Một event; app tra lại và hiển thị trạng thái đã xác nhận |
| R05 | Đổi lương hiệu lực tháng sau, mở phiếu tháng đã khóa | T13,T14,T19 | Số tiền cũ không đổi |
| R06 | Tính kỳ100 nhân viên, gây lỗi ở giữa | T14 | Không mất kỳ hợp lệ trước đó; không publish nửa bảng |
| R07 | Tính lương đủ công/thiếu checkout/ngày lễ/paid leave | T09,T13 | Khớp fixture tính tay, không tính kép giờ |
| R08 | User gửi form sửa điện thoại không gửi địa chỉ | T08,T17 | Địa chỉ giữ nguyên, version conflict không overwrite |
| R09 | Reset sai purpose, hết hạn, replay, SMTP fail | T06,T22 | Reset thất bại đúng; không nói mail đã gửi khi chưa gửi |
| R10 | Tạo DB trống, upgrade legacy test, import lại | T02,T23 | Schema đúng, không mất/nhân đôi dữ liệu, đối soát được |
| R11 | Export lương người khác hoặc tên chứa công thức | T15,T24 | Chặn quyền; CSV an toàn khi mở bằng bảng tính |
| R12 | Backup rồi restore sang DB khác | T26 | Dữ liệu/tổng tiền khớp; thời gian/RPO đo được |

## Năm luồng demo bảo vệ

1. HR tạo nhân viên → nhân viên đăng nhập Android → xem hồ sơ đúng scope.
2. Nhân viên check-in/out → web thấy dữ liệu → thử gửi lại không trùng.
3. Nhân viên gửi phép → HR duyệt → quỹ đổi đúng; thử duyệt lại/thiếu quỹ.
4. Payroll tính nháp → reviewer khác duyệt → công bố → app xem phiếu; đổi lương kỳ sau không đổi phiếu cũ.
5. Thử truy cập người khác bị chặn → xem audit hợp lệ → trình evidence migration/restore và test concurrency.

Demo chỉ dùng dữ liệu giả, cài sẵn build đã freeze, có video dự phòng. Không cần live gọi Gemini hoặc dịch vụ ngoài không thuộc đề tài. Thời lượng đề xuất 10–15 phút, điều chỉnh theo quy định trường.

## Mười câu hỏi cần tự trả lời khi bảo vệ

1. Vì sao WinForms hiện tại chưa dùng trực tiếp làm backend cho web/mobile?
2. Domain, Application, Infrastructure và API khác nhau ở trách nhiệm nào?
3. Cookie web và bearer mobile được lưu, kiểm tra, thu hồi ra sao?
4. Vì sao ẩn nút chưa đủ ngăn đọc phiếu lương người khác?
5. Hai người duyệt cùng lúc: transaction/constraint nào ngăn phép âm?
6. Làm sao phân biệt retry sau timeout với yêu cầu tạo công mới?
7. Thay lương giữa tháng được tính thế nào, vì sao kỳ khóa không đổi?
8. Test fake repository khác test SQL Server ở điểm nào?
9. Restore dữ liệu khác rollback mã nguồn ra sao?
10. Tính năng nào đã làm, chưa làm, AI hỗ trợ phần nào, bằng chứng nghiệm thu ở đâu?

## Điều kiện dùng thật

Đạt bản bảo vệ không tự đạt production. Trước pilot cần P04 và review chính sách tiền từ P03 nếu dùng kết quả để trả lương. Thông tin pháp lý phải được tra lại ở thời điểm áp dụng; báo cáo này không đánh giá tuân thủ cụ thể. Cần người chịu trách nhiệm vận hành và HR, quyền dữ liệu rõ, account thật không dùng chung, backup đã restore, cảnh báo lỗi và hỗ trợ. Chỉ mở phạm vi đã được xác nhận; không thu thập GPS/CCCD thêm vì “sau này có thể cần”.
