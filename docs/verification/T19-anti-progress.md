# T19 Anti Progress Tracker

- **Task**: T19 - Web Lương (Payroll) và Dashboard
- **Assignee**: Antigravity (Anti)
- **Coordinator / Runner**: Codex
- **Status**: READY_FOR_REVIEW (Hoàn tất phần việc của Anti, chuyển giao cho Codex runner)
- **Timestamp**: 2026-11-14

## Scope of Ownership
- **Allowed write files**:
  - `backend/src/HRM.Api/Controllers/PayrollQueriesController.cs` (ĐÃ HOÀN TẤT & ĐÓNG BĂNG)
  - `docs/ai-handoff/T19-API_READY.md` (ĐÃ TẠO)
  - `docs/verification/T19-anti-progress.md` (ĐÃ HOÀN TẤT)
  - `docs/verification/T19-anti-report.md` (ĐÃ HOÀN TẤT)
  - `web/src/features/payroll/**` (ĐÃ TẠO: `api.ts`, `PayrollFeedback.tsx`, `PayrollWorkflowWizard.tsx`, `PayrollAdjustments.tsx`, `MyPayslips.tsx`, `PayrollPage.tsx`)
  - `web/src/features/dashboard/**` (ĐÃ TẠO: `DashboardMetrics.tsx`)
  - `web/src/App.tsx`, `web/src/pages/DashboardPage.tsx`, `web/src/components/layout/AppLayout.tsx` (ĐÃ TÍCH HỢP)
  - `web/src/styles/product.css` (ĐÃ BỔ SUNG LỚP CSS)
  - `web/test/t19-*.test.tsx`, `web/e2e/payroll.spec.ts` (ĐÃ VIẾT - ĐÁNH DẤU NOT_RUN)
- **Strict Boundaries**:
  - T18 giữ nguyên trạng thái `READY_FOR_REVIEW`.
  - Không chạy terminal/npm/dotnet/git/SQL (Codex là runner).
  - Không sửa DB sản phẩm, không gửi email, không đụng credentials/.env/appsettings.
  - Không tự nhận VERIFIED, không mock E2E thành công.
  - Không sửa generated client bằng tay; Codex sinh client từ API.

## Roadmap & Progress Checklist
- [x] Step 1: Ghi ACK và khởi tạo `docs/verification/T19-anti-progress.md`
- [x] Step 2: Đọc các file đặc tả & source chính theo thứ tự chỉ định
- [x] Step 3: Tạo `backend/src/HRM.Api/Controllers/PayrollQueriesController.cs` và thông báo qua `docs/ai-handoff/T19-API_READY.md` (API_READY)
- [x] Step 4: Xây dựng UI T19 (`web/src/features/payroll/**`, `web/src/features/dashboard/**`, tích hợp App.tsx, DashboardPage.tsx, AppLayout.tsx, product.css)
- [x] Step 5: Viết tests UI T19 (`web/test/t19-payroll.test.tsx`, `web/e2e/payroll.spec.ts`) đánh dấu NOT_RUN (chờ Codex chạy runner)
- [x] Step 6: Tổng kết và xuất báo cáo `docs/verification/T19-anti-report.md` (READY_FOR_REVIEW)
