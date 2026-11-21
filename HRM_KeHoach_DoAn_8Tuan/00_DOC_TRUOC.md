# Bộ kế hoạch HRM — 8 tuần, một người kiểm duyệt và AI code

Phiên bản1.0,09/11/2026. Mobile đã chọn React Native; Android là nền tảng nghiệm thu trước. Bộ tài liệu này phục vụ giao việc, chưa triển khai hay sửa code sản phẩm. Thời hạn8 tuần là mục tiêu có điều kiện, không phải lời bảo đảm tốc độ AI.

## Đọc theo nhu cầu

1. [Báo cáo nâng cấp](01_BAO_CAO_NANG_CAP.md): lựa chọn kiến trúc, scope,8 tuần, điều kiện ra người dùng thật.
2. [Hiện trạng repository](07_HIEN_TRANG_REPOSITORY.md): cấu trúc5 project, auth, nghiệp vụ, database, code, test, deployment.
3. [Đối chiếu nguồn](06_DOI_CHIEU_NGUON.md):22 phát hiện có file/dòng ghim commit và mức kiểm chứng.
4. [Đặc tả MVP](02_DAC_TA_MVP.md): quy tắc, quyền, màn hình, API, mô hình dữ liệu và Definition of Done.
5. [Backlog](03_BACKLOG.md):29 task lõi +7 nhóm sau đồ án; mỗi task có file riêng trong tasks.
6. [Prompt giao AI](04_PROMPT_GIAO_AI.md): prompt triển khai, review, biên bản và cách bạn nghiệm thu.
7. [Lộ trình nghiệm thu](05_LO_TRINH_NGHIEM_THU.md): mốc tuần, cắt scope khi trễ,12 kịch bản,5 demo và10 câu hỏi bảo vệ.
8. [backlog.json](backlog.json): dữ liệu task cho AI đọc, không thay quyết định nghiệm thu của bạn.

## Bắt đầu

Đọc báo cáo và đặc tả, rồi dùng prompt khởi động T00. Khi bạn đã duyệt scope/chính sách demo, giao T01. Sau đó lấy task có dependencies đã đạt; không giao cả36 mục một lượt. Mỗi task chỉ coi hoàn thành khi bạn kiểm thử được, có evidence và hiểu phần cốt lõi.

## Phạm vi đề xuất

Backend ASP.NET Core .NET10 + SQL Server; web React TypeScript; app React Native cho nhân viên. Core: tài khoản/quyền, nhân sự, chấm công, nghỉ phép, lương. Hoãn SaaS/microservices, face/GPS, offline công, payroll pháp lý tự động, chatbot và tuyển dụng/đào tạo đầy đủ.

## Độ tin cậy

Audit trên cong0905/HRM tại commit8b1236573f99c28f4cd486831cb30635a01bd061. Có build, kiểm EF/migration offline, harness service và dependency scan. Chưa xác minh UI, SQL Server thật, SMTP thật hoặc Gemini thật. Các JSON trong evidence là kết quả audit baseline, **không phải** kết quả của kế hoạch chưa được triển khai. Những đường dẫn backend/web/mobile/ops/docs trong task là đề xuất cho code tương lai.

Ước lượng giờ trong backlog là giờ bạn kiểm duyệt, chưa gồm toàn bộ thời gian học, sửa lỗi tích hợp và viết báo cáo. Sau tuần1 phải ước lượng lại. Bản bảo vệ và bản phát hành thật có cổng nghiệm thu riêng.
