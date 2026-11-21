namespace HRM.Next.Application.Authorization;

public static class HrmPermissions
{
    public const string CalendarRead = "calendar.read";
    public const string CalendarManage = "calendar.manage";

    // Departments
    public const string DepartmentsRead = "departments.read";
    public const string DepartmentsManage = "departments.manage";

    // Employees
    public const string EmployeesRead = "employees.read";
    public const string EmployeesManage = "employees.manage";

    // Attendance
    public const string AttendanceReadOwn = "attendance.read_own";
    public const string AttendanceReadAll = "attendance.read_all";
    public const string AttendanceManage = "attendance.manage";

    // Leaves
    public const string LeavesCreateOwn = "leaves.create_own";
    public const string LeavesReadOwn = "leaves.read_own";
    public const string LeavesCancelOwn = "leaves.cancel_own";
    public const string LeavesReadAll = "leaves.read_all";
    public const string LeavesApprove = "leaves.approve";

    // Payroll (Separated per spec)
    public const string PayrollReadOwn = "payroll.read_own";
    public const string PayrollReadAll = "payroll.read_all";
    public const string PayrollCalculate = "payroll.calculate";
    public const string PayrollApprove = "payroll.approve";
    public const string PayrollPublish = "payroll.publish";
    public const string PayrollExport = "payroll.export";
    public const string PayrollHistoryManage = "payroll.history_manage";

    // Administration
    public const string AccountsManage = "accounts.manage";
    public const string AccountsAssignRoles = "accounts.assign_roles";
    public const string HrScopeManage = "hr.scope_manage";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [HrmRoles.Employee] = [
                CalendarRead,
                AttendanceReadOwn,
                LeavesCreateOwn,
                LeavesReadOwn,
                LeavesCancelOwn,
                PayrollReadOwn
            ],
            [HrmRoles.Hr] = [
                CalendarRead,
                CalendarManage,
                DepartmentsRead,
                DepartmentsManage,
                EmployeesRead,
                EmployeesManage,
                AttendanceReadAll,
                AttendanceManage,
                LeavesReadAll,
                LeavesApprove
            ],
            [HrmRoles.Payroll] = [
                CalendarRead,
                DepartmentsRead,
                AttendanceReadAll,
                LeavesReadAll,
                PayrollReadAll,
                PayrollCalculate,
                PayrollApprove,
                PayrollPublish,
                PayrollExport,
                PayrollHistoryManage
            ],
            [HrmRoles.SystemAdmin] = [
                CalendarRead,
                CalendarManage,
                DepartmentsRead,
                DepartmentsManage,
                AccountsManage,
                AccountsAssignRoles,
                HrScopeManage,
                AuditRead
            ]
        };

    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (RolePermissions.TryGetValue(role, out var permissions))
            {
                foreach (var p in permissions)
                    result.Add(p);
            }
        }
        return result.ToList();
    }
}

