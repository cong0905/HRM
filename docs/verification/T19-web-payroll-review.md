# T19 — Review độc lập web lương và dashboard

Kiểm chứng ngày 14–15/11/2026 (Asia/Saigon). Trạng thái hiện tại: **VERIFIED về kỹ thuật theo ủy quyền ngày 15/11/2026**. Quyết định riêng nằm trong `docs/decisions/T19-delegated-technical-acceptance.md`; không phải human UAT.

## Phạm vi và ranh giới

Review phần Anti bàn giao trong `docs/verification/T19-anti-report.md`; không đồng bộ database web với desktop, không thay dữ liệu `HRM_System`, không triển khai T20. T18 vẫn READY_FOR_REVIEW. Build và demo trước đây chỉ là bước khởi chạy, không được dùng thay cho kiểm chứng nghiệp vụ T19.

## Sửa lỗi sau review

- Bỏ ép `hrmClient as unknown` ở GET danh sách kỳ, dùng DTO và route generated thực tế.
- Form tạo kỳ dùng năm/tháng dạng số; có lọc năm và phân trang danh sách kỳ.
- Tra tên nhân viên theo trang hiện tại (tối đa 30 ID cho snapshot + phiếu phát hành, 10 ID cho điều chỉnh), phù hợp giới hạn 100 ID của API bulk.
- Mỗi form có tên riêng để tránh trùng ID `reason` và liên kết label sai khi đổi tab.
- Hiển thị lỗi tải CSV, bản chạy so sánh và đối soát; dashboard không diễn giải lỗi attendance thành “Chưa vào ca”. Retry dashboard chỉ gọi query được cấp quyền.
- Chặn nút duyệt/công bố khi bản chụp có lỗi hoặc chưa tải được. Cổng backend vẫn quyết định kết quả cuối cùng.
- Cho sửa đầu vào kỳ Approved theo đúng backend T14, chỉ khóa đầu vào khi Published/Locked. Nếu đầu vào thay đổi, phải tính và duyệt lại.
- Modal giữ nguyên kỳ/RowVersion/runVersion được chọn khi mở. Xung đột không tự chuyển quyết định sang bản chạy mới; cần thao tác “Tải bản mới để đối chiếu”.

## Kiểm chứng

- SQL thật: **398/398 PASS, 0 skip**, `t19/t19-sql.trx`. Bao gồm 395 ca nền và ba ca T19 mới kiểm quyền, filter/phân trang/version, GET/reload không ghi số liệu, audit hoặc receipt. Ledger SQL xác nhận đã dọn database GUID do fixture tạo.
- Vitest: **33/33 PASS, 0 skip**, chạy lại trên mã cuối ngày 15/11 (`t19/vitest.json`). Build, typecheck và lint đã PASS trên cùng mã; build còn cảnh báo bundle >500 kB.
- Playwright: **6/6 PASS, 0 skip, 0 retry** trong một lượt SQL sạch ngày 15/11 (`t19/playwright.json`, `t19/browser-run.json`). Không cộng kết quả rời từ các lượt thất bại. Lượt xanh trước khi chỉnh thời điểm chụp ảnh được lưu riêng trong `t19/green-20261115-initial/`.
- Sáu ca thật bao phủ quyền HR/Admin/Employee; tạo kỳ và lỗi thiếu lương; nhập lịch sử lương/điều chỉnh qua UI; tính tay công + phép; chống tự duyệt; input stale và RowVersion stale; đối chiếu run; duyệt/công bố bởi người khác; phiếu cá nhân/IDOR; tăng lương tháng sau; từ chối ghi lại kỳ đóng; tải CSV/403; khóa; thiếu giờ ra và net âm; dashboard; đi qua màn hình công/phép; viewport 375px.
- Đã kiểm tra trực quan ảnh `t19/payroll-desktop.png` (1440px) và `t19/payroll-mobile.png` (375px). Ảnh desktop chờ sidebar kết thúc transition; bảng rộng cuộn trong vùng riêng, không làm tràn toàn trang mobile.
- Đối soát SQL sau browser: kỳ 2026-04 Locked, ActiveBatchId = ApprovedBatchId = PublishedBatchId; hai phiếu Published tổng Net **12.001.102 VND**. Hai batch tháng 4 giữ độc lập. Kỳ tháng 6 có dữ liệu chặn và chưa Published.
- Runner xác nhận database GUID đã dọn, file fixture/cert tạm không còn, cổng test 3001 đã đóng. Các lượt thử không dùng `HRM_System` hoặc database demo 3000.
- Fixture clock lấy UTC hiện tại + 1 phút để cookie không hết hạn do ngày cố định. Dữ liệu tính lương vẫn cố định tháng 4 và tháng 6/2026. Thời hạn chờ chỉ tăng trong harness/test (Vitest 15 giây, thao tác browser 30 giây, login response 60 giây); không nới auth/rate limit sản phẩm. Lượt SQL 398 ca được chạy ngày 14/11; controller/backend nghiệp vụ không đổi trong vòng sửa UI ngày 15/11.
- Fixtures: hai hồ sơ nhân viên giả, tài khoản Employee/HR/PayrollCalculator/PayrollApprover/SystemAdmin riêng; lịch T00 tháng 4; công đủ và phép hưởng lương; tháng 6 có ca thiếu giờ ra; số tiền thập phân giữ chuỗi. Không lưu credential fixture vào báo cáo.
- Mốc tính tay: nhân viên Một công/phép đủ tháng, lương 12.000.000 + thưởng 0,50 → Net 12.000.001; nhân viên Hai không có công, thưởng 1.000,50 → Net 1.001. Run 1 tổng 12.001.002. Thêm thưởng 100 cho Một, run 2 tổng 12.001.102. Chỉ làm tròn ở backend theo fixture đã chốt.

## Chạy lại

```powershell
.\.dotnet\dotnet.exe build backend/tests/HRM.T19BrowserReview -c Release -m:1 -nr:false
python -B scripts/verify_t19_browser.py
```

Runner cần SQL Server local Windows Authentication và Edge; cổng 3001 phải trống. Nó tạo API/SQL GUID riêng, đọc credential/cert từ file temp, chạy `web/playwright.t19.config.ts`, đóng Vite/host và ghi `t19/browser-run.json`. Không tái sử dụng môi trường demo cổng 3000. Harness dừng sớm bằng stopFile và có TTL 90 phút.

## Giới hạn

Chưa human UAT, remote CI/deploy, thử tải, mobile native hay đồng bộ desktop. Bundle vẫn có cảnh báo chunk lớn hơn 500 kB. Web và desktop còn là hai luồng dữ liệu riêng. Chủ dự án quyết định VERIFIED theo hợp đồng task.

## Kết luận kiểm chứng trước nghiệm thu

T19 đủ bằng chứng kỹ thuật để **READY_FOR_REVIEW**. Các tiêu chí checkbox trong task được đối chiếu bằng kiểm thử tự động, không thay thế human UAT hay quyết định VERIFIED của chủ dự án. T18 giữ READY_FOR_REVIEW. Không chuyển T20.

Graphify đã cập nhật graph code (14.399 node, 24.954 cạnh). Phiên bản parser đang cài báo trích xuất một phần `MyPayslips.tsx`; file này đã được kiểm bằng source trực tiếp, TypeScript, lint và browser. Không suy ra graph đủ mọi quan hệ hoặc semantic docs đã được tái tạo từ việc lệnh update thành công.

Bản tổng hợp máy đọc và SHA-256 mã nguồn: [t19/result.json](t19/result.json).

Kiểm tra working tree toàn cục còn trailing whitespace tại `db/src/HRM.GUI/Forms/Main/UngVien/ucUngVien.cs:81`, thuộc thay đổi legacy có sẵn trước lượt này; không sửa file đó ngoài phạm vi T19. Scope tracked của T19 qua `git diff --check`.

## Quyết định nghiệm thu — 15/11/2026

Chủ dự án yêu cầu “nghiệm thu giúp t”. Codex đã kiểm tra lại mã nguồn không đổi so với bằng chứng test, đối soát đủ tiêu chí và chuyển riêng T19 sang **VERIFIED về kỹ thuật**. Bản manifest trước nghiệm thu được giữ nguyên tại `t19-acceptance/tested-result.json`; biên bản và acceptedSourceSha256 ở `../decisions/T19-delegated-technical-acceptance.md` và `t19-acceptance/result.json`. T18 vẫn READY_FOR_REVIEW; các giới hạn human UAT, remote CI, deploy và đồng bộ database không thay đổi.
