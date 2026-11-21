namespace HRM.Next.Application.Authorization;

public static class HrmPolicies
{
    public const string CalendarRead = "Calendar.Read";
    public const string CalendarManage = "Calendar.Manage";
    public const string DepartmentsRead = "Departments.Read";
    public const string DepartmentsManage = "Departments.Manage";
    public const string AttendanceReadAll = "Attendance.ReadAll";
    public const string AttendanceManage = "Attendance.Manage";
    public const string EmployeesRead = "Employees.Read";
    public const string EmployeesManage = "Employees.Manage";
    public const string LeavesApprove = "Leaves.Approve";
    public const string LeavesReadAll = "Leaves.ReadAll";
    public const string PayrollRead = "Payroll.Read";
    public const string PayrollCalculate = "Payroll.Calculate";
    public const string PayrollApprove = "Payroll.Approve";
    public const string PayrollPublish = "Payroll.Publish";
    public const string PayrollExport = "Payroll.Export";
    public const string PayrollHistoryManage = "Payroll.HistoryManage";
    public const string AccountsManage = "Accounts.Manage";
    public const string AccountsAssignRoles = "Accounts.AssignRoles";
    public const string HrScopeManage = "HrScopeManage";
    public const string AuditRead = "Audit.Read";
}

