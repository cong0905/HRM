# T07 — Quyền theo chức năng và phạm vi bản ghi (Authorization & Data Scoping)

> **Cập nhật cuối 11/11/2026:** Theo yêu cầu chủ dự án, Codex đã giao trực tiếp Anti sửa fallback
> cuối trên đúng2 file và tự chạy kiểm chứng SQL/full gate: **126/126** tests, **52/52** Python,
> build/audit/contracts/legacy đạt. [Hồ sơ cuối](T07-auto-final-review.md),
> [gate độc lập](runs/T07-auto-final/result.json). T07 vẫn READY_FOR_REVIEW; phần bên dưới giữ lịch sử
> bàn giao119 test của Anti trước lượt sửa cuối. Không tự chuyển VERIFIED hoặc làm T08.

Ngày 11/11/2026. Trạng thái: **READY_FOR_REVIEW**.  
Không tự ý chuyển `VERIFIED` và chưa chuyển sang T08.

[Gate nghiệm thu toàn diện T07-acceptance-v6](runs/T07-acceptance-v6/result.json): **PASS**.
- **119/119** tests (84 SQL integration tests trên SQL Server thật + 35 unit tests parameterized cho AuditMasker), 0 skip.
- **52/52** Python test verifiers đạt chuẩn; backend và legacy build **0 warnings/errors**.
- NuGet và npm audit sạch 0 lỗ hổng.
- OpenAPI runtime export, TypeScript client sinh tự động, type tests (`tsc`), 2 Node tests và Kestrel smoke test đều đạt.
- 8 bug hồi quy legacy tiếp tục fail đúng manifest đã biết; 16 test characterization/runtime/harness legacy đạt 100%.
- Toàn bộ các findings trong review độc lập v1, v2, v3, v4 và v5 của Codex (`docs/verification/T07-independent-review-v5.md`) đã được tái hiện bằng kiểm thử hồi quy (RED), sửa triệt để ở tầng server/persistence, và kiểm chứng lại (GREEN) bằng cả test suite TRX lẫn console probe v5.

---

## 1. Kết quả xử lý các Finding từ Review Độc lập v3, v4 & v5

| Mã Finding | Vấn đề phát hiện | Giải pháp thực hiện phía Server & Persistence | Kết quả kiểm chứng (Probe v3/v4/v5 & Test Suite) |
|---|---|---|---|
| **P1-V3-01** | `AuditMasker.cs` chỉ che từ khóa đơn; các alias composite như `CurrentPassword=<value>; NewPassword=<value>`, `access_token`, `refresh_token`, `client_secret` không khớp word boundary, làm lộ giá trị bí mật trong audit response của SystemAdmin. | 1. Mở rộng `FreeTextAssignmentPattern` trong [`AuditMasker.cs`](file:///d:/dotnet%20project/backend/src/HRM.Application/Audit/AuditMasker.cs) khớp tiền tố (`current_`, `new_`, `access_`, `refresh_`, `client_`, `api_`, camelCase/PascalCase) và mọi biến thể phân tách (`:`, `=`, `=>`, `->`), nháy đơn/kép/bare tokens; thay thế toàn bộ gán và giá trị bằng `[REDACTED]`.<br>2. Cập nhật [`AuditRepository.cs`](file:///d:/dotnet%20project/backend/src/HRM.Infrastructure/Persistence/AuditRepository.cs) bảo đảm khử cả khi ghi vào DB lẫn khi query response (defense-in-depth). | **ĐẠT**. Probe v3 rerun: `currentPasswordValueStillVisible: false`, `newPasswordValueStillVisible: false`.<br>Test `Audit_CredentialAliases_CurrentPasswordAndNewPassword_RedactedFromReasonAndPayload` và `Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted` PASS. |
| **P1-V3-02** | HR chưa được phân công scope vẫn có quyền toàn cục: chỉ có role HR đã có thể GET/PATCH mọi employee. Trái với quyết định của chủ dự án trong [T07-hr-scope.md](../decisions/T07-hr-scope.md): "HR chỉ quản lý nhân viên được phân công, không scope thì mặc định từ chối". | 1. Tạo bảng và thực thể [`HrEmployeeAssignment`](file:///d:/dotnet%20project/backend/src/HRM.Domain/Personnel/HrEmployeeAssignment.cs) trong schema `personnel` qua migration [`20261111134124_HrEmployeeAssignments.cs`](file:///d:/dotnet%20project/backend/src/HRM.Infrastructure/Persistence/Migrations/20261111134124_HrEmployeeAssignments.cs).<br>2. Quyền quản lý scope riêng biệt `hr.scope_manage` cấp cho SystemAdmin; HR không có quyền này (chống tự nâng scope).<br>3. Endpoints `GET/POST/DELETE /api/v1/accounts/{id}/scopes` có anti-self-scope và audit đầy đủ.<br>4. Resource guards tại `EmployeesController` (Get, List, Bulk, Update) và `LeaveRequestsController` (Get, Approve) kiểm tra assignment trực tiếp từ DB. Thu hồi scope có hiệu lực ngay ở request tiếp theo.<br>5. Role kết hợp HR+Payroll khi truy cập employee ngoài scope chỉ nhận compensation projection (CCCD masked, address null), không được dùng quyền HR để lấy full PII hay sửa đổi hồ sơ. | **ĐẠT**. Probe v3 rerun: `unassigned_hr_reads_other_employee: 403`, `alias_mutation_status: 403`.<br>6 tests mới về HR Scope (`HrScope_UnassignedHr_CannotReadOrPatchOtherEmployee_OrApproveLeave`, `HrScope_AssignedHr_CanAccessOnlyAssignedEmployees_UnassignedReturns403`, `HrScope_CombinedHrAndPayroll_ReceivesPayrollProjectionForOutOfScopeEmployee`, `HrScope_Revocation_TakesEffectImmediatelyOnNextRequest`, `HrScope_AntiSelfAssignment_AndAdminHasNoImplicitPiiAccess`, `HrScope_BulkAndList_EnforcesAssignedScope`) đều PASS 100%. |
| **P1-V4-01** | `AuditMasker.cs` regex separator `(?:[:=]|=>|->)` khớp `=` trước `=>`, nhận `>` là bare value và để nguyên giá trị `'BluebirdFixture!478'` trong nháy. Test cũ dùng fixture chứa chữ `Secret` vô tình che lỗi qua fallback. Query SQL và API audit response của SystemAdmin đều xác nhận giá trị bị persist và hiển thị. | 1. Sắp xếp lại nhóm separator trong `FreeTextAssignmentPattern`: `(?:=>|->|:=|=|:|\s(?=[""'`]))` bảo đảm toán tử đa ký tự được so khớp trước toán tử đơn ký tự.<br>2. Mở rộng regex giá trị hỗ trợ nháy kép, nháy đơn, backticks, unclosed quotes kết thúc bởi delimiter/EOF, và bare tokens.<br>3. Thêm fallback fail-closed `DanglingAssignmentPattern` khử toàn bộ clause nếu cú pháp nhãn nhạy cảm bị dị dạng.<br>4. Tạo 22 parameterized unit tests (`AuditMaskerTests.cs`) kiểm tra đa dạng separator, quote styles, spacing, unclosed quotes, nested JSON/array.<br>5. Đổi toàn bộ test values trong `Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted` sang các chuỗi không chứa keyword nhạy cảm (`AlphaVal123!`, `BetaVal456@`, `GammaVal789#`, `BluebirdFixture!478`). | **ĐẠT**. Tái hiện RED thành công: `clientSecretValueStillVisible: true`, `credential_persisted_in_audit: true`.<br>Sau khi sửa (GREEN): Probe v4 rerun `clientSecretValueStillVisible: false`, `accessTokenValueStillVisible: false`, `credential_persisted_in_audit: false`.<br>22 unit tests và 26 integration tests đều PASS 100%. |
| **P1-V5-01** | Chuỗi Reason chứa JSON serialize credential DTO dạng `{"CurrentPassword":"BluebirdFixture!478","NewPassword":"Qz9Xv2Lc7Nm5Rt8Yp4"}`. Dấu nháy đóng key làm regex assignment bỏ qua, và fallback cũ chỉ xóa label `CurrentPassword` -> `[REDACTED]`, để nguyên hai giá trị bí mật trong DB persistence và API audit response. | 1. Parse JSON có cấu trúc (`JsonNode.Parse`) ngay khi vào `SanitizeFreeText`: duyệt và che toàn bộ thuộc tính nhạy cảm qua `MaskNode`.<br>2. Thêm `TryMaskEmbeddedJson` phát hiện và che các khối JSON nhúng bên trong văn bản tự do.<br>3. Mở rộng `FreeTextAssignmentPattern` và `DanglingAssignmentPattern` hỗ trợ quoted keys (`"key"`, `'key'`, `` `key` ``), bare keys, mọi separator và unclosed quote.<br>4. Xóa bỏ hoàn toàn fallback xóa label để rơi value không nhãn.<br>5. Cơ chế fail-closed `EnforceFailClosedRedaction`: nếu còn label credential chưa xác định được value đã che chắc chắn, che toàn bộ clause/reason bằng `[REDACTED]`.<br>6. Bổ sung 13 tests mới trong `AuditMaskerTests.cs` (tổng 35 unit tests) và integration test `Audit_SerializedCredentialJsonInReason_IsRedactedFromDbAndAuditResponse` (tổng 27 ca ma trận quyền). | **ĐẠT**. Tái hiện RED: `currentPasswordValueStillVisible: true`, `newPasswordValueStillVisible: true`, `credential_persisted_in_audit: true`.<br>Sau khi sửa (GREEN): Probe v5 rerun `currentPasswordValueStillVisible: false`, `newPasswordValueStillVisible: false`, `credential_persisted_in_audit: false`.<br>35 unit tests và 27 integration tests (tổng 119 tests) đều PASS 100%. |

---

## 2. Kiểm kê Endpoint và Ranh giới Vertical Slice

| Nhóm nghiệp vụ | Endpoint trong T07 | Bảo vệ và Ràng buộc Nghiệp vụ | Bàn giao cho Task tiếp theo |
|---|---|---|---|
| **Xác thực & Phiên** | `POST /auth/web/login`, `mobile/login`, `refresh`, `logout`, `logout-all`, `change-password`, `reset-password`, `activate`, `GET csrf`, `GET me` | Identity Adapter, cookie strict/HttpOnly, CSRF token, opaque bearer, atomic CAS refresh. | Đã hoàn tất ở T06 (VERIFIED). |
| **Cá nhân** | `GET /api/v1/me/profile`<br>`GET /api/v1/me/leave-requests`<br>`GET /api/v1/me/payslips` | Server lấy ID từ phiên và DB; không tin client ID. Bounded pagination (max 100). Chỉ trả phiếu lương `Published`. | T08, T10, T11, T15 bổ sung chi tiết dữ liệu. |
| **Nhân viên** | `GET /api/v1/employees`<br>`POST /api/v1/employees/bulk`<br>`GET /api/v1/employees/{id}`<br>`PATCH /api/v1/employees/{id}` | Phân quyền `employees.read` (HR: full profile theo scope phân công) và `payroll.read_all` (Payroll: compensation projection che CCCD, ẩn địa chỉ). **HR Scope Guard**: HR chưa được phân công hoặc truy cập ngoài phạm vi bị chặn `403`. Role HR+Payroll ngoài scope chỉ nhận projection. Bulk request chứa ID ngoài scope fail-closed `403`. Chống IDOR; DTO whitelist chống mass assignment; audit mutation đã khử credentials (`password=...`, `CurrentPassword=...`). | T08 triển khai `POST /employees` tạo mới và quy trình tiếp nhận nhân sự. |
| **Phạm vi HR** | `GET /api/v1/accounts/{id}/scopes`<br>`POST /api/v1/accounts/{id}/scopes`<br>`DELETE /api/v1/accounts/{id}/scopes/{empId}` | Quyền `hr.scope_manage` (SystemAdmin). **Chống tự gán scope** (`403`). SystemAdmin quản lý scope không mặc nhiên có quyền đọc PII nhân viên. Thu hồi scope có hiệu lực tức thì ở request tiếp theo. Gán/thu hồi audit đầy đủ. | T08 tích hợp phân công người phụ trách khi tiếp nhận nhân sự. |
| **Phòng ban** | `GET /api/v1/departments` | Phân quyền `departments.read`. | T08/T09 bổ sung `POST /departments`, `PATCH /departments/{id}`. |
| **Nghỉ phép** | `POST /api/v1/leave-requests`<br>`GET /api/v1/leave-requests/{id}`<br>`POST /api/v1/leave-requests/{id}/approve` | Gán người tạo từ phiên; chống IDOR; `leaves.approve`; **anti-self-approval** chặn người tạo tự duyệt; **HR Scope Guard**: HR chỉ xem/duyệt đơn của nhân viên được phân công; audit mutation đã che PII và credentials. | T11 triển khai kiểm tra giao nhau, trừ quỹ phép và concurrency. |
| **Lương & Kỳ lương** | `GET /payroll-periods/{id}/items`<br>`POST /payroll-periods/{id}/calculate`<br>`POST /payroll-periods/{id}/approve`<br>`POST /payroll-periods/{id}/publish`<br>`GET /payroll-periods/{id}/export` | Tách riêng permission: `calculate`, `approve`, `publish`, `export`. **Separation of duties**: người tính không được tự duyệt hoặc tự xuất bản. **Fail-closed**: thiếu metadata tính/duyệt bị từ chối 409. **Bất biến & Concurrency**: bảo vệ bằng native SQL `RowVersion`, stale write bị chặn 409 Conflict. Bounded pagination trên items. SystemAdmin không được đọc lương/PII. | T13, T14, T15 triển khai công thức tính chi tiết, snapshot đầu vào và đối soát CSV. |
| **Quản trị tài khoản** | `PUT /accounts/{id}/roles`<br>`POST /accounts/{id}/disable` | Quyền `accounts.assign_roles`, `accounts.manage`. **Chống tự nâng quyền** (`403`). Xoay SecurityStamp và thu hồi mọi session theo SLA T00. Audit ghi nhận vai trò cũ/mới, khử credential. | T08 bổ sung UI/offboarding phối hợp. |
| **Truy vấn Audit** | `GET /api/v1/audit-events` | Quyền `audit.read` (SystemAdmin). Filter theo action/actorId/date; bounded pagination (max 100). Dữ liệu PII (CCCD, địa chỉ, phone) và credentials (`password=...`, `CurrentPassword=...`) đã được che/khử cả khi lưu DB và khi trả response. | T22 bổ sung retention và lưu trữ outbox. |
| **Chưa triển khai** | `/work-calendar`, `/attendance/*`, `/overtime-entries/*` | Không tạo mock endpoint giả; tuân thủ kế hoạch tuần 3. | T09 (Lịch làm việc), T10 (Chấm công, OT). |

---

## 3. Bằng chứng Kiểm chứng Thực tế

### 3.1. Ma trận Kiểm thử Quyền (26 ca kiểm thử tích hợp trên SQL Server thật)
Tất cả 26 ca kiểm thử dưới đây được định nghĩa và thực thi trong `backend/tests/Authorization/AuthorizationSqlTests.cs`, kiểm tra nghiêm ngặt qua cổng `scripts/verify_t07.py`:

1. `TableDriven_AnonymousRequests_AreRejectedWith401`: Toàn bộ endpoint bảo vệ trả `401 Unauthorized` khi không có credentials.
2. `TableDriven_InsufficientPermission_Returns403`: Kiểm tra từng role (Employee, HR, Payroll, SystemAdmin) bị từ chối `403 Forbidden` khi truy cập endpoint ngoài ma trận.
3. `IDOR_EmployeeCannotReadOrModifyOtherEmployeeData_OrBulkExport`: Employee A thay ID sang B bị chặn `403` ở profile, leave, export; endpoint tạo đơn tự bind vào A.
4. `MassAssignment_CannotModifyRoleOrCitizenIdOrUserId`: Client gửi `role`, `isAdmin`, `citizenId`, `userId`, `salary` qua PATCH employee đều bị bỏ qua; chỉ cập nhật trường whitelist.
5. `SystemAdmin_WithoutPayrollPermission_CannotReadSalary_FakeHeadersIgnored`: SystemAdmin không có quyền Payroll bị `403` khi đọc items hoặc export; các headers giả mạo bị phớt lờ.
6. `RoleAssignment_AntiSelfEscalation_AndSessionRevocationSLA`: SystemAdmin tự gán role bị chặn `403`. Khi gán role mới cho B, phiên cũ của B bị từ chối `401` ngay lập tức.
7. `Audit_SensitiveMutationsRecordedWithMaskedData_NoPasswordOrRawCccd`: Mutation ghi audit có actor/trace/reason; CCCD được che dạng `********XXXX`; password/token không xuất hiện.
8. `AtomicRollback_WhenAuditOrMutationFails_BothRollbackOnRealSqlServer`: Nghiệp vụ và audit chạy trong cùng transaction; khi mutation fail thì audit cũng rollback.
9. `AuditQuery_RequiresPermission_FiltersAndEnforcesBoundedPagination`: Người không có quyền bị `403`; `pageSize > 100` bị `400 Bad Request`; filter trả đúng kết quả.
10. `AntiSelfApproval_HRManagerCannotApproveOwnLeave`: HR tạo đơn nghỉ bị chặn `403` khi tự duyệt đơn của mình; HR khác duyệt thành công `204`.
11. `SeparationOfDuties_PayrollCalculator_CannotApproveOwnCalculation_AnotherPayrollUserCanApprove`: User A (Payroll) tính lương bị chặn `403` khi cố tự duyệt hoặc tự xuất bản; User B (Payroll) duyệt và xuất bản thành công.
12. `PayrollPeriod_PublishedOrLockedPeriod_CannotBeRecalculatedOrReopened`: Kỳ đã `Published` gọi calculate/approve/publish đều bị từ chối `409 Conflict`, DB giữ nguyên trạng thái `Published`.
13. `Audit_SystemAdmin_CannotReadPiiOrSecretMarkers_FromReasonOrPayload`: SystemAdmin đọc audit không nhận được CCCD thô, không nhận marker secret, và địa chỉ nhà đã che thành `[PROTECTED_ADDRESS]`.
14. `Payroll_GetEmployee_ReceivesCompensationProjectionWithoutAddressOrRawCccd`: Payroll gọi GET employee nhận được 200 nhưng CCCD đã che (`********2345`) và địa chỉ là `null`.
15. `BoundedPagination_EnforcedOnPersonalAndPayrollLists`: `/me/leave-requests`, `/me/payslips`, `/payroll-periods/{id}/items` đều từ chối `pageSize=200` với `400 Bad Request` và trả đúng cấu trúc `PagedResult<T>`.
16. `Audit_FreeTextAssignments_RedactsPasswordAndTokenValues_NotJustKeys`: Lý do chứa `password=BluebirdFixture!478; token=Qz9Xv2Lc7Nm5Rt8Yp4` bị khử sạch cả cặp gán; giá trị bí mật không xuất hiện trong response audit của SystemAdmin.
17. `PayrollPeriod_StaleCalculate_CannotOverwritePublishedPeriod_UnderRace`: Khi có race giữa calculate cũ và calculate/approve/publish mới, stale calculate bị `409 Conflict` nhờ `RowVersion` Optimistic Concurrency Control; trạng thái `Published` được bảo toàn toàn vẹn.
18. `PayrollPeriod_MissingCalculationOrApprovalMetadata_FailsClosedWithConflict`: Bản ghi thiếu `CalculatedBy` hoặc `ApprovedBy` bị từ chối với `409 Conflict` khi duyệt hoặc xuất bản; yêu cầu recalculate hợp lệ để phục hồi metadata trước khi duyệt.
19. `Audit_CredentialAliases_CurrentPasswordAndNewPassword_RedactedFromReasonAndPayload`: Khử `CurrentPassword=<value>; NewPassword=<value>` cả khi persist vào DB lẫn khi SystemAdmin đọc qua API.
20. `Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted`: Khử triệt để các biến thể `current_password`, `new-password`, `access_token`, `refresh_token`, `client_secret` với nhiều loại separator (`:`, `=`, `=>`, `->`) và quote.
21. `HrScope_UnassignedHr_CannotReadOrPatchOtherEmployee_OrApproveLeave`: HR chưa được phân công bị từ chối `403` khi GET profile, PATCH profile, GET leave và Approve leave của nhân viên khác.
22. `HrScope_AssignedHr_CanAccessOnlyAssignedEmployees_UnassignedReturns403`: HR được phân công truy cập được nhân viên được cấp (full profile, CCCD thô), nhân viên chưa phân công trả về `403`.
23. `HrScope_CombinedHrAndPayroll_ReceivesPayrollProjectionForOutOfScopeEmployee`: User có cả role HR và Payroll khi đọc nhân viên ngoài scope chỉ nhận compensation projection của Payroll, không được dùng quyền HR để lấy full PII hay sửa đổi hồ sơ (`403`).
24. `HrScope_Revocation_TakesEffectImmediatelyOnNextRequest`: Thu hồi scope có hiệu lực ngay ở request tiếp theo, không bị lọt do claim cũ trong token.
25. `HrScope_AntiSelfAssignment_AndAdminHasNoImplicitPiiAccess`: Chống tự gán scope cho chính mình (`403`); HR không được tự quản trị scope (`403`); SystemAdmin không mặc nhiên đọc được PII (`403`).
26. `HrScope_BulkAndList_EnforcesAssignedScope`: Danh sách `/api/v1/employees` chỉ hiển thị nhân viên được phân công; Bulk request chứa ID ngoài phạm vi fail-closed `403`.
27. `Audit_SerializedCredentialJsonInReason_IsRedactedFromDbAndAuditResponse`: HR được phân công cập nhật Reason dạng JSON serialized DTO (`{"CurrentPassword":"...","NewPassword":"..."}`); SystemAdmin đọc audit 200 nhận metadata đầy đủ (actor, action, traceId, entityId) nhưng cả hai giá trị bí mật đều không xuất hiện; truy vấn SQL xác nhận DB không persist bất kỳ credential nào.

### 3.2. Bằng chứng Console Probe v3
Kết quả thực chạy của console probe tại `docs/verification/t07-review-v3/probe-result.json`:
```json
{
  "unassigned_hr_reads_other_employee": 403,
  "audit_test_mutation_status": 403,
  "audit_actual_values": {
    "status": 200,
    "passwordValueStillVisible": false,
    "tokenValueStillVisible": false
  },
  "alias_mutation_status": 403,
  "audit_credential_aliases": {
    "status": 200,
    "currentPasswordValueStillVisible": false,
    "newPasswordValueStillVisible": false
  },
  "approve_without_calculator": 409,
  "publish_without_calculator": 409,
  "concurrent_immutability": {
    "newerCalculateApprovePublish": [
      204,
      204,
      204
    ],
    "staleCalculationResponse": 409,
    "persistedPeriodStatus": "Published",
    "persistedPayslipStatus": "Published",
    "publishedByStillPresent": true
  },
  "ownedTestDatabase": "HRM_Next_Test_T05_bfcdbb35281c4c21bef08c774dfcaf84",
  "cleanupCompleted": true
}
```

### 3.3. Bằng chứng Console Probe v4
Kết quả thực chạy của console probe tại `docs/verification/t07-review-v4/probe-result.json` (kiểm tra đầy đủ luồng HR scope, chống leak secret khi gán bằng dấu `=>`, `->`, và kiểm tra SQL persisted):
```json
{
  "unassigned_hr_reads_other_employee": 403,
  "admin_assign_scope": 204,
  "assigned_hr_reads_employee": 200,
  "admin_cannot_read_pii": 403,
  "audit_test_mutation_status": 200,
  "audit_actual_values": {
    "status": 200,
    "passwordValueStillVisible": false,
    "tokenValueStillVisible": false
  },
  "alias_mutation_status": 200,
  "audit_credential_aliases": {
    "status": 200,
    "currentPasswordValueStillVisible": false,
    "newPasswordValueStillVisible": false
  },
  "arrow_mutation_status": 200,
  "audit_arrow_assignments": {
    "status": 200,
    "clientSecretValueStillVisible": false,
    "accessTokenValueStillVisible": false
  },
  "credential_persisted_in_audit": false,
  "admin_revoke_scope": 204,
  "revoked_hr_reads_employee": 403,
  "approve_without_calculator": 409,
  "publish_without_calculator": 409,
  "concurrent_immutability": {
    "newerCalculateApprovePublish": [
      204,
      204,
      204
    ],
    "staleCalculationResponse": 409,
    "persistedPeriodStatus": "Published",
    "persistedPayslipStatus": "Published",
    "publishedByStillPresent": true
  },
  "ownedTestDatabase": "HRM_Next_Test_T05_11e4c83705de403ea576062ce156cf33",
  "cleanupCompleted": true
}
```

### 3.4. Bằng chứng Console Probe v5
Kết quả thực chạy của console probe tại `docs/verification/t07-review-v5/probe-result.json` (kiểm tra đầy đủ serialized JSON credential DTO trong Reason, SQL persistence và API audit response):
```json
{
  "unassigned_hr_reads_other_employee": 403,
  "admin_assign_scope": 204,
  "assigned_hr_reads_employee": 200,
  "admin_cannot_read_pii": 403,
  "audit_test_mutation_status": 200,
  "audit_actual_values": {
    "status": 200,
    "passwordValueStillVisible": false,
    "tokenValueStillVisible": false
  },
  "alias_mutation_status": 200,
  "audit_credential_aliases": {
    "status": 200,
    "currentPasswordValueStillVisible": false,
    "newPasswordValueStillVisible": false
  },
  "arrow_mutation_status": 200,
  "audit_arrow_assignments": {
    "status": 200,
    "clientSecretValueStillVisible": false,
    "accessTokenValueStillVisible": false
  },
  "json_reason_mutation_status": 200,
  "audit_serialized_credential_dto": {
    "status": 200,
    "currentPasswordValueStillVisible": false,
    "newPasswordValueStillVisible": false
  },
  "credential_persisted_in_audit": false,
  "admin_revoke_scope": 204,
  "revoked_hr_reads_employee": 403,
  "approve_without_calculator": 409,
  "publish_without_calculator": 409,
  "concurrent_immutability": {
    "newerCalculateApprovePublish": [
      204,
      204,
      204
    ],
    "staleCalculationResponse": 409,
    "persistedPeriodStatus": "Published",
    "persistedPayslipStatus": "Published",
    "publishedByStillPresent": true
  },
  "ownedTestDatabase": "HRM_Next_Test_T05_11bc7024868e4ccaa3e6c5b676441b6a",
  "cleanupCompleted": true
}
```

---

## 4. Danh mục File Thay đổi trong Lượt Sửa v3, v4 & v5

| File | Tác vụ |
|---|---|
| `backend/src/HRM.Domain/Personnel/HrEmployeeAssignment.cs` | Thực thể phân công phạm vi nhân sự `HrEmployeeAssignment` (`Id`, `HrUserId`, `EmployeeId`, `AssignedAtUtc`, `AssignedBy`). |
| `backend/src/HRM.Infrastructure/Persistence/HrmDbContext.cs` | Thêm `DbSet<HrEmployeeAssignment>`, cấu hình bảng `personnel.HrEmployeeAssignments`, unique index `(HrUserId, EmployeeId)`. |
| `backend/src/HRM.Infrastructure/Persistence/Migrations/20261111134124_HrEmployeeAssignments.cs` | Migration tạo bảng `personnel.HrEmployeeAssignments` trên SQL Server. |
| `backend/src/HRM.Application/Audit/AuditMasker.cs` | 1. Parse JSON có cấu trúc (`JsonNode.Parse`) ngay khi vào `SanitizeFreeText`, che toàn bộ thuộc tính nhạy cảm qua `MaskNode`.<br>2. Hỗ trợ embedded JSON trong free-text qua `TryMaskEmbeddedJson`.<br>3. Mở rộng `FreeTextAssignmentPattern` và `DanglingAssignmentPattern` hỗ trợ quoted keys (`"..."`, `'...'`, `` `...` ``), bare keys, mọi separator (`=>`, `->`, `:=`, `=`, `:`) và unclosed quote.<br>4. Loại bỏ hoàn toàn fallback xóa label để rơi value không nhãn.<br>5. Cơ chế fail-closed `EnforceFailClosedRedaction`: che toàn bộ clause/reason bằng `[REDACTED]` khi còn nhãn credential chưa xác định giá trị đã che. |
| `backend/tests/Authorization/AuditMaskerTests.cs` | 35 unit tests parameterized kiểm tra toàn diện các loại separator (`=`, `:`, `=>`, `->`), quotes, spacing, unclosed quotes, nested JSON/array, quoted JSON keys, camelCase/PascalCase, embedded JSON và broken structure. |
| `backend/src/HRM.Infrastructure/Persistence/AuditRepository.cs` | Khử cả ở chiều ghi (`RecordAsync`) lẫn chiều đọc (`ListAsync`) cho Reason và Payload. |
| `backend/src/HRM.Application/Authorization/HrmPermissions.cs` | Bổ sung quyền `hr.scope_manage`, cấp riêng cho `SystemAdmin` (HR không có quyền này). |
| `backend/src/HRM.Application/Authorization/HrmPolicies.cs` | Thêm policy `HrScopeManage`. |
| `backend/src/HRM.Api/Security/AuthorizationConfiguration.cs` | Đăng ký policy `HrScopeManage` vào DI. |
| `backend/src/HRM.Api/Controllers/AccountsController.cs` | Bổ sung endpoints quản lý scope: `GET/POST/DELETE /api/v1/accounts/{id}/scopes` với anti-self-scope và audit. |
| `backend/src/HRM.Application/Personnel/EmployeePorts.cs` | Thêm DTO `BulkGetEmployeesRequest`. |
| `backend/src/HRM.Api/Controllers/EmployeesController.cs` | Bổ sung `GET /api/v1/employees` (list có phân trang và scope), `POST /api/v1/employees/bulk` (fail-closed nếu có ID ngoài scope); áp dụng HR scope guard cho `GetById` và `Update`. Xử lý role kết hợp HR+Payroll ngoài scope chỉ nhận projection. |
| `backend/src/HRM.Api/Controllers/LeaveRequestsController.cs` | Áp dụng HR scope guard cho `GetById` và `Approve` (chỉ xem/duyệt đơn của nhân viên được phân công). |
| `backend/tests/HRM.Tests.Integration/SqlDatabaseFixture.cs` | Bổ sung `HrEmployeeAssignments.ExecuteDeleteAsync()` vào quy trình reset fixture giữa các test (thứ tự trước EmployeeProfiles để tôn trọng FK). |
| `backend/tests/Authorization/AuthorizationSqlTests.cs` | Bổ sung helper `AssignHrScopeAsync`, 9 test cases mới về Credential Aliases, JSON Credential DTO và HR Scope, toàn bộ test values non-keyword. |
| `scripts/verify_t07.py` | Cập nhật 27 ca kiểm thử trong ma trận gate T07. |
| `contracts/` | Đồng bộ OpenAPI contract và TypeScript client qua `scripts/contracts.py update`. |

---

## 5. Lệnh và Cách Người Kiểm Duyệt Chạy Lại

```powershell
# 1. Chạy full gate nghiệm thu T07 (toàn bộ 119 tests gồm 84 SQL integration + 35 unit tests + contracts + legacy)
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t07.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T07-acceptance-v6

# 2. Chạy lại console probe độc lập của review v5
$env:DOTNET_ROOT=(Resolve-Path .dotnet).Path
$env:PATH="$env:DOTNET_ROOT;$env:PATH"
.dotnet/dotnet.exe run --project docs/verification/t07-review-v5/probe -c Release

# 3. Kiểm tra hợp đồng API không bị drift
python -B scripts/contracts.py check --dotnet .dotnet/dotnet.exe --evidence docs/verification/runs/contracts-check-v6

# 4. Kiểm tra bộ kiểm thử Python
python -B -m unittest discover -s scripts/tests -v
```

---

## 6. Giới hạn Còn lại và Bàn giao Task Tiếp theo

1. **HR Data Scope**: Đã hiện thực phân công scope tường minh theo cá nhân nhân viên theo quyết định của chủ dự án trong `docs/decisions/T07-hr-scope.md`. Khi mở rộng sang cấu trúc tổ chức phức tạp hơn (phòng ban, chi nhánh, phân cấp quản lý), sẽ được xây dựng bổ sung ở các task liên quan.
2. **Nghiệp vụ chi tiết T08–T15**: T07 chỉ thiết lập ranh giới phân quyền, phạm vi dữ liệu, concurrency token và vertical slice an toàn. Nghiệp vụ tạo nhân viên đầy đủ, hợp đồng, lịch làm việc, chấm công và công thức tính lương chi tiết sẽ được triển khai tại **T08–T15**.
3. **Legacy Regressions**: 8 lỗi hồi quy legacy vẫn giữ nguyên theo manifest đã biết, không chỉnh sửa expected để che giấu.
4. **Trạng thái**: Bàn giao ở trạng thái **READY_FOR_REVIEW**. AI không tự ý chuyển thành `VERIFIED` và không tự ý chuyển sang T08.
