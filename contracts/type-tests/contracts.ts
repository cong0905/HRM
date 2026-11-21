import { createHrmClient } from "../src/client.js";
import type { components } from "../src/client.js";

function checkGeneratedTypes(client: ReturnType<typeof createHrmClient>) {
  client.GET("/api/v1/departments", { params: { query: { page: 1, pageSize: 20 } } });
  // @ts-expect-error Unknown routes must not be accepted by the generated client.
  client.GET("/api/v1/not-a-real-operation");
  // @ts-expect-error A string page must not satisfy the integer API contract.
  client.GET("/api/v1/departments", { params: { query: { page: "one" } } });
  const money: components["schemas"]["DecimalString"] = "9007199254740993.12";
  // @ts-expect-error Monetary values must remain strings, not IEEE754 numbers.
  const lossyMoney: components["schemas"]["DecimalString"] = 9007199254740993.12;
  const timestamp: components["schemas"]["SystemInfo"]["serverTimeUtc"] = "2026-11-11T03:04:05Z";
  client.POST("/api/v1/auth/mobile/login", { body: { username: "sample", password: "entered-by-user" } });
  // @ts-expect-error Auth payloads come from the schema and require a string password.
  client.POST("/api/v1/auth/mobile/login", { body: { username: "sample", password: 123 } });
  const refresh: components["schemas"]["RefreshRequest"] = { refreshToken: "opaque" };
  // @ts-expect-error A refresh request cannot omit its credential.
  const missingRefresh: components["schemas"]["RefreshRequest"] = {};
  // @ts-expect-error Custom UTC converter must still generate a string schema.
  const invalidTimestamp: components["schemas"]["SystemInfo"]["serverTimeUtc"] = 42;
  client.PATCH("/api/v1/employees/{id}", {
    params: { path: { id: "synthetic-id" } },
    body: { phone: "0901234567", reason: "Contact update", rowVersion: "base64-version" }
  });
  const clearOptional: components["schemas"]["UpdateEmployeeRequest"] = {
    maritalStatus: null, reason: "Clear optional field", rowVersion: "base64-version"
  };
  // @ts-expect-error Profile updates cannot set account roles.
  const invalidEmployeeRole: components["schemas"]["UpdateEmployeeRequest"] = { reason: "Denied field", role: "SystemAdmin" };
  // @ts-expect-error Offboarding requires a concurrency token.
  const missingOffboardVersion: components["schemas"]["OffboardEmployeeRequest"] = { terminationDate: "2026-11-11", reason: "Departure" };
  client.POST("/api/v1/employees/{id}/offboard", {
    params: { path: { id: "synthetic-id" } },
    body: { terminationDate: "2026-11-11", reason: "Departure", rowVersion: "base64-version" }
  });
  // @ts-expect-error Scoped profile updates require the original concurrency version.
  const missingPatchVersion: components["schemas"]["UpdateEmployeeRequest"] = { phone: "0901234567", reason: "Missing version" };
  // @ts-expect-error Required address cannot be explicitly cleared; omission preserves its value.
  const clearRequiredAddress: components["schemas"]["UpdateEmployeeRequest"] = { address: null, reason: "Invalid clear", rowVersion: "base64-version" };
  void clearOptional; void invalidEmployeeRole; void missingOffboardVersion;
  void missingPatchVersion; void clearRequiredAddress;
  client.GET("/api/v1/work-calendars/months/{year}/{month}", { params: { path: { year: 2026, month: 4 } } });
  // @ts-expect-error Calendar month parameters are integers, not strings.
  client.GET("/api/v1/work-calendars/months/{year}/{month}", { params: { path: { year: "2026", month: 4 } } });
  const calendarMinutes: components["schemas"]["CalendarMonthResponse"]["standardMinutes"] = "9600";
  // @ts-expect-error Exact minute totals use the decimal-string contract.
  const lossyCalendarMinutes: components["schemas"]["CalendarMonthResponse"]["standardMinutes"] = 9600;
  const snapshotPeriod: components["schemas"]["CalendarPeriodResponse"]["payrollPeriodId"] = "uuid";
  void calendarMinutes; void lossyCalendarMinutes; void snapshotPeriod;
  client.POST("/api/v1/attendance/me/check-in", { params: { header: { "Idempotency-Key": "uuid-v4" } }, body: { note: "Arrive", clientTimeUtc: "2099-01-01T00:00:00Z" } });
  // @ts-expect-error Attendance commands require a durable idempotency key header.
  client.POST("/api/v1/attendance/me/check-in", { body: {} });
  // @ts-expect-error An employee cannot choose a target user through its own check-in payload.
  const forgedAttendanceActor: components["schemas"]["AttendanceCommandRequest"] = { userId: "another-user" };
  void forgedAttendanceActor;
  const attendanceMinutes: components["schemas"]["AttendanceDayResponse"]["eligibleMinutes"] = "390";
  // @ts-expect-error Overtime minutes use the exact decimal-string contract.
  const numericOvertime: components["schemas"]["ConfirmOvertimeRequest"] = { minutes: 90, reason: "HR confirmation", rowVersion: "base64-version" };
  void attendanceMinutes; void numericOvertime;
  client.POST("/api/v1/employees/{employeeId}/salary-history", { params: { path: { employeeId: "uuid" }, header: { "Idempotency-Key": "uuid-v4" } }, body: { effectiveFrom: "2026-01-01", monthlyBase: "12000000.00", reason: "Salary decision" } });
  // @ts-expect-error Salary commands require the durable key header.
  client.POST("/api/v1/employees/{employeeId}/salary-history", { params: { path: { employeeId: "uuid" } }, body: { effectiveFrom: "2026-01-01", monthlyBase: "12000000", reason: "Salary decision" } });
  // @ts-expect-error Salary inputs cannot use lossy JS number money.
  const numericSalary: components["schemas"]["AppendSalaryRequest"] = { effectiveFrom: "2026-01-01", monthlyBase: 12000000, reason: "Denied money type" };
  // @ts-expect-error A salary DTO cannot forge the actor or target account.
  const forgedSalaryActor: components["schemas"]["AppendSalaryRequest"] = { effectiveFrom: "2026-01-01", monthlyBase: "12000000", reason: "Denied actor field", actorId: "uuid" };
  const exactPayrollNet: components["schemas"]["PayrollCalculationOutput"]["net"] = "10740000";
  // @ts-expect-error Snapshot net retains the decimal-string contract.
  const lossyPayrollNet: components["schemas"]["PayrollCalculationOutput"]["net"] = 10740000;
  // @ts-expect-error New snapshot capture endpoints require idempotency headers too.
  client.POST("/api/v1/payroll-periods/{periodId}/snapshots", { params: { path: { periodId: "uuid" } }, body: { reason: "Capture snapshots" } });
  void numericSalary; void forgedSalaryActor; void exactPayrollNet; void lossyPayrollNet;
  client.POST("/api/v1/payroll-periods/{id}/workflow/{operation}", { params: { path: { id: "uuid", operation: "calculate" }, header: { "Idempotency-Key": "uuid-v4" } }, body: { reason: "Versioned calculation", rowVersion: "base64-eight-bytes", runVersion: 0 } });
  // @ts-expect-error Workflow commands require the version token.
  const missingWorkflowVersion: components["schemas"]["PayrollWorkflowRequest"] = { reason: "Missing version", runVersion: 0 };
  // @ts-expect-error Run versions are integers, not strings.
  const textRunVersion: components["schemas"]["PayrollWorkflowRequest"] = { reason: "Wrong version type", rowVersion: "base64", runVersion: "1" };
  // @ts-expect-error Typed workflow endpoint requires durable key headers.
  client.POST("/api/v1/payroll-periods/{id}/workflow/{operation}", { params: { path: { id: "uuid", operation: "publish" } }, body: { reason: "Publish versioned payroll", rowVersion: "base64", runVersion: 1 } });
  const workflowRowVersion: components["schemas"]["PayrollWorkflowState"]["rowVersion"] = "base64";
  void missingWorkflowVersion; void textRunVersion; void workflowRowVersion;
  client.GET("/api/v1/me/payslips/{id}", { params: { path: { id: "uuid" } } });
  client.GET("/api/v1/payroll-periods/{id}/reconciliation", { params: { path: { id: "uuid" } } });
  client.GET("/api/v1/payroll-periods/{id}/export", { params: { path: { id: "uuid" }, query: { maxRows: 1000 } } });
  const reconciledMoney: components["schemas"]["PayrollReconciliationResponse"]["difference"] = "0";
  // @ts-expect-error Monetary reconciliation stays an exact decimal string.
  const numericDifference: components["schemas"]["PayrollReconciliationResponse"]["difference"] = 0;
  // @ts-expect-error Private source JSON is excluded from the payslip projection.
  const privatePayslipSource: components["schemas"]["PayslipDetailResponse"]["sourceJson"] = "private";
  // @ts-expect-error Export limits are integer query parameters.
  client.GET("/api/v1/payroll-periods/{id}/export", { params: { path: { id: "uuid" }, query: { maxRows: "all" } } });
  void reconciledMoney; void numericDifference; void privatePayslipSource;
  return { money, lossyMoney, timestamp, invalidTimestamp, refresh, missingRefresh };
}
void checkGeneratedTypes;
