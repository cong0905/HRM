# WORK_COMPLETE: T19 Web Tính lương, Công bố và Dashboard

- **Task**: T19 - Web tính lương, công bố và dashboard
- **Assignee**: Antigravity (Anti)
- **Coordinator / Runner**: Codex
- **Status**: READY_FOR_REVIEW
- **Timestamp**: 2026-11-14
- **Dependency Status**: T14, T15, T16, T17 đã VERIFIED. T18 giữ nguyên trạng thái `READY_FOR_REVIEW` (đã bàn giao độc lập, tách riêng với quyết định nghiệm thu của chủ dự án). Không tự nâng trạng thái T18 thành VERIFIED.

---

## 1. Danh sách file được tạo mới và cập nhật (Tuân thủ quyền sở hữu)

### Backend (Đã hoàn tất & đóng băng cho Codex runner)
- `backend/src/HRM.Api/Controllers/PayrollQueriesController.cs` (MỚI)
  - Endpoint `GET /api/v1/payroll-periods`
  - Operation: `ListPayrollPeriods`
  - Policy: `[Authorize(Policy = HrmPolicies.PayrollRead)]` (yêu cầu quyền `payroll.read_all`)
  - Parameters: `page` (default 1), `pageSize` (default 20), `year` (optional 2000-2100), `status` (allowlist: Draft, Calculated, Approved, Published, Locked).
  - Implementation: `AsNoTracking`, lọc prefix `yyyy-` cho year, validate status, `LongCountAsync` trước phân trang, sắp xếp `PeriodCode DESC, Id ASC`. Chỉ nạp page IDs rồi query max version từ `PayrollCalculationBatches`, map sang `PagedResult<PayrollWorkflowState>`.
  - Không sinh snapshot, không gọi audit/mutation/calculator trong GET.

### AI Handoff & Tracking
- `docs/ai-handoff/T19-API_READY.md` (MỚI): Bàn giao route/DTO của controller đã đóng băng để Codex build backend và sinh contract client.
- `docs/verification/T19-anti-progress.md` (MỚI / CẬP NHẬT): Ghi nhận toàn bộ tiến độ từng bước.
- `docs/verification/T19-anti-report.md` (MỚI): Báo cáo nghiệm thu hoàn chỉnh của Anti.

### Web UI
- `web/src/features/payroll/api.ts` (MỚI):
  - Định nghĩa DTOs từ `contracts/src/generated/api.ts` (`Schema<...>`).
  - Toàn bộ phương thức API cho danh sách kỳ, workflow (`calculate`, `approve`, `publish`, `lock`), snapshot, đối soát, chi tiết phiếu cá nhân/quản trị, xuất CSV an toàn, điều chỉnh thủ công và lịch sử lương.
- `web/src/features/payroll/PayrollFeedback.tsx` (MỚI):
  - Xử lý thông báo lỗi chuẩn hóa từ ProblemDetails cho module lương (`cannot_self_approve`, `calculator_cannot_publish`, `period_immutable`, `salary_closed_period`, `version_conflict`, ...).
- `web/src/features/payroll/PayrollWorkflowWizard.tsx` (MỚI):
  - Thanh chọn kỳ lương & modal tạo kỳ mới (`POST /api/v1/payroll-periods`).
  - Hiển thị trạng thái máy chủ và thanh tiến trình Steps (Draft -> Calculated -> Approved -> Published -> Locked).
  - Nút bấm workflow: Tính toán nháp, Phê duyệt, Công bố, Khóa vĩnh viễn.
  - **Quy tắc phân tách quyền hạn (Separation of duties)**: Nếu `user.id === period.calculatedBy`, nút duyệt và công bố bị khóa kèm cảnh báo rõ ràng.
  - Modal xác nhận mọi thao tác: yêu cầu nhập lý do tối thiểu 10 ký tự, truyền `rowVersion`, `runVersion`, sinh UUIDv4 `Idempotency-Key` qua `useCommand` có phân vùng scope.
  - Bảng chụp tính toán (Snapshot): hiển thị `InputHash`, thời điểm ghi nhận, danh sách lý do chặn (`blockingReasons`), số liệu server tổng hợp (`StandardPay`, `OvertimePay`, `Gross`, `Net`, `RoundingAdjustment`).
  - Định dạng tiền tệ bằng chuỗi thập phân chính xác qua `displayDecimal`, **tuyệt đối không dùng Number/parseFloat/toFixed**.
  - Tính năng đối chiếu run: chọn bản chạy cũ để so sánh trực tiếp số liệu server cạnh nhau theo từng nhân viên.
  - Đối soát (`reconciliation`) và danh sách phiếu lương phát hành (`items`).
  - Xuất CSV (`export`): chỉ hiển thị với quyền `payroll.export`, tách riêng xử lý lỗi ProblemDetails (không tải response 403/409 thành file CSV, giải phóng `URL.revokeObjectURL`).
- `web/src/features/payroll/PayrollAdjustments.tsx` (MỚI):
  - Form thêm khoản cộng/trừ thủ công (`Allowance`, `Bonus`, `OtherDeduction`, `DemoInsurance`, `DemoTax`), nhập số tiền decimal string > 0, lý do >= 10 ký tự.
  - Chặn sửa đổi khi kỳ lương đã `Approved`, `Published`, `Locked`.
  - Hủy khoản điều chỉnh (Void) với lý do >= 10 ký tự và kiểm tra `rowVersion`.
  - Quản lý lịch sử mức lương (`SalaryHistory`) yêu cầu quyền riêng `payroll.history_manage`: chọn nhân viên, xem lịch sử, thiết lập mốc lương mới với ngày bắt đầu hiệu lực. Không ghi đè lịch sử cũ, cấm chọn ngày ảnh hưởng kỳ đã công bố/khóa.
- `web/src/features/payroll/MyPayslips.tsx` (MỚI):
  - Nhánh riêng cho nhân viên tra cứu phiếu của chính mình (`GET /api/v1/me/payslips` và `GET /api/v1/me/payslips/{id}`).
  - Chỉ hiển thị phiếu Published của chính chủ; không dùng API quản trị để lọc client.
  - Xem chi tiết bản chụp bất biến: lương chuẩn, tiền OT, các khoản chi tiết (Lines), ngày công thực tế (Days). Không tính lương lại ở client; đổi lương tháng sau không làm đổi phiếu tháng trước.
- `web/src/features/payroll/PayrollPage.tsx` (MỚI):
  - Điểm vào không gian làm việc `/payroll`.
  - Phân nhánh nghiêm ngặt theo permission: nếu chỉ có `payroll.read_own`, chỉ hiển thị `MyPayslips`. Nếu có `payroll.read_all`, hiển thị đầy đủ các tab quản trị kỳ lương và điều chỉnh.
- `web/src/features/dashboard/DashboardMetrics.tsx` (MỚI):
  - Thay thế placeholder bằng query thật theo quyền người dùng.
  - Payroll admin: hiển thị `totalCount` kỳ lương qua `listPeriods({ page: 1, pageSize: 1 })`. Không hiển thị/fake tổng tiền lương.
  - Nhân viên: hiển thị `totalCount` phiếu Published riêng qua `myPayslips({ page: 1, pageSize: 1 })` và trạng thái ca làm việc hôm nay qua `attendanceApi.today()`.
  - HR: hiển thị `totalCount` đơn nghỉ phép chờ duyệt trong phạm vi qua `leaveApi.team({ status: "Pending" })`.
  - Tài khoản không có quyền lương tuyệt đối không gửi request query bảng lương.
- `web/src/pages/DashboardPage.tsx` (CẬP NHẬT):
  - Tích hợp `DashboardMetrics`, giữ nguyên lời chào, thông tin tài khoản và danh sách phân hệ.
- `web/src/App.tsx` (CẬP NHẬT):
  - Thay `PlaceholderPage` của route `/payroll` bằng `PayrollPage` với `requiredAnyPermission={["payroll.read_all", "payroll.read_own"]}`.
- `web/src/styles/product.css` (CẬP NHẬT):
  - Bổ sung các lớp CSS cho `.payroll-control-bar`, `.payroll-period-card`, `.metric-card`, `.dashboard-metrics-section` và responsive breakpoint 575px mà không thay/xóa style cũ.

### Tests
- `web/test/t19-payroll.test.tsx` (MỚI):
  - Unit tests cô lập kiểm tra độ chính xác chuỗi số thập phân (`displayDecimal`), sinh khóa idempotency theo scope, xử lý lỗi export CSV không rò rỉ URL, và cơ chế thử lại thủ công không gửi trùng tự động.
  - Trạng thái kiểm thử: **`NOT_RUN`** (chờ Codex runner thực thi).
- `web/e2e/payroll.spec.ts` (MỚI):
  - E2E Playwright tests mô phỏng các ca nghiệp vụ: nhân viên chỉ thấy phiếu của mình, phân tách quyền hạn (người tính không được tự duyệt), và chặn tải CSV khi không có quyền export.
  - Trạng thái kiểm thử: **`NOT_RUN`** (bỏ qua an toàn khi chưa nạp fixture SQL).

---

## 2. Trạng thái Kiểm thử (Verification & Test Execution)

Theo đúng quy định giao việc: Anti không chạy các lệnh terminal/npm/dotnet/git/SQL trong sandbox; Codex đóng vai trò là runner độc lập. Do đó:

| Bộ kiểm thử | File | Trạng thái thực tế | Ghi chú |
| :--- | :--- | :---: | :--- |
| **Backend Controller Build** | `backend/src/HRM.Api/Controllers/PayrollQueriesController.cs` | **NOT_RUN** | Chờ Codex chạy `dotnet build` |
| **Contract Generation** | `contracts/` | **NOT_RUN** | Chờ Codex chạy `npm run generate` |
| **Frontend Unit Tests** | `web/test/t19-payroll.test.tsx` | **NOT_RUN** | Đã viết mã test hoàn chỉnh; chờ Codex chạy `npm test` |
| **Frontend E2E Tests** | `web/e2e/payroll.spec.ts` | **NOT_RUN** | Đã viết mã test hoàn chỉnh; chờ Codex chạy harness |
| **SQL Fixture Harness** | Integration SQL Tests | **NOT_RUN** | Codex sở hữu |

*Lưu ý: Không ghi "100%" hoặc "PASS" cho bất kỳ kiểm tra nào chưa được runner thực thi.*

---

## 3. Rủi ro, Giới hạn và Hướng dẫn cho Codex Runner

### Rủi ro & Giới hạn
1. **Contract Regeneration**: Khi Codex chạy lệnh sinh contract (`npm run generate` trong `contracts/`), endpoint `GET /api/v1/payroll-periods` sẽ được tự động cập nhật vào `contracts/src/generated/api.ts` với operation `ListPayrollPeriods`. Phương thức `payrollApi.listPeriods` trong `web/src/features/payroll/api.ts` đã được thiết kế đúng signature của DTO này.
2. **Anti-Self-Approval**: Backend thực hiện kiểm tra `period.CalculatedBy.Value == security.CurrentUserId` và trả mã `403` nếu vi phạm. Giao diện người dùng đã bổ sung kiểm tra phía client để disable nút bấm và hiển thị cảnh báo chi tiết, bảo đảm tính fail-closed.
3. **Tính Bất biến**: Mọi chi tiết phiếu lương được đọc từ bảng chụp snapshot máy chủ. Trình duyệt không thực hiện bất kỳ phép tính lương hay làm tròn trung gian nào.

### Hướng dẫn để Codex kiểm chứng
1. Build backend:
   ```powershell
   dotnet build backend/src/HRM.Api/HRM.Api.csproj
   ```
2. Sinh lại contract client:
   ```powershell
   npm --prefix contracts run generate
   ```
3. Chạy unit tests frontend:
   ```powershell
   npm --prefix web run test -- web/test/t19-payroll.test.tsx
   ```
4. Kiểm tra TypeScript compile:
   ```powershell
   npm --prefix web run typecheck
   ```
5. Chạy Playwright E2E với fixture test (khi runner đã chuẩn bị DB test):
   ```powershell
   npm --prefix web run test:e2e -- web/e2e/payroll.spec.ts
   ```
