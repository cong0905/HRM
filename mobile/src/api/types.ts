import type { components, paths } from "hrm-api-contracts";

/**
 * Tái sử dụng trực tiếp các kiểu dữ liệu từ hợp đồng sinh tự động (generated contracts).
 * Không sao chép thủ công DTO hoặc định nghĩa tuyến đường.
 */
export type TokenPair = components["schemas"]["TokenPair"];
export type LoginRequest = components["schemas"]["LoginRequest"];
export type RefreshRequest = components["schemas"]["RefreshRequest"];
export type ForgotRequest = components["schemas"]["ForgotRequest"];
export type ResetRequest = components["schemas"]["ResetRequest"];
export type ChangePasswordRequest = components["schemas"]["ChangePasswordRequest"];
export type CurrentUserAccessResponse = components["schemas"]["CurrentUserAccessResponse"];
export type ProblemDetails = components["schemas"]["ProblemDetails"];
export type DeliveryResponse = components["schemas"]["DeliveryResponse"];

// Employee Profile
export type EmployeeProfileResponse = components["schemas"]["EmployeeProfileResponse"];

// Attendance
export type AttendanceDayResponse = components["schemas"]["AttendanceDayResponse"];
export type AttendanceCommandRequest = components["schemas"]["AttendanceCommandRequest"];
export type PagedResultOfAttendanceDayResponse = components["schemas"]["PagedResultOfAttendanceDayResponse"];

// Leave
export type LeaveBalanceResponse = components["schemas"]["LeaveBalanceResponse"];
export type LeaveRecordResponse = components["schemas"]["LeaveRecordResponse"];
export type CreateLeaveRequest = components["schemas"]["CreateLeaveRequest"];
export type CancelLeaveRequest = components["schemas"]["CancelLeaveRequest"];
export type PagedResultOfLeaveRecordResponse = components["schemas"]["PagedResultOfLeaveRecordResponse"];

// Payroll & Payslips
export type PayslipResponse = components["schemas"]["PayslipResponse"];
export type PayslipDetailResponse = components["schemas"]["PayslipDetailResponse"];
export type PagedResultOfPayslipResponse = components["schemas"]["PagedResultOfPayslipResponse"];
export type PayrollCalculationOutput = components["schemas"]["PayrollCalculationOutput"];
export type PayrollLine = components["schemas"]["PayrollLine"];
export type PayrollManualAmounts = components["schemas"]["PayrollManualAmounts"];
export type PayrollPolicy = components["schemas"]["PayrollPolicy"];
export type PayrollCalendarDay = components["schemas"]["PayrollCalendarDay"];
export type PayrollDayInput = components["schemas"]["PayrollDayInput"];

export type { paths, components };
