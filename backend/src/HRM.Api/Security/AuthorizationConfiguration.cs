using HRM.Next.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HRM.Next.Api.Security;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddHrmAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

            options.AddPolicy(HrmPolicies.CalendarRead, p => p.RequireClaim("permission", HrmPermissions.CalendarRead));
            options.AddPolicy(HrmPolicies.CalendarManage, p => p.RequireClaim("permission", HrmPermissions.CalendarManage));
            options.AddPolicy(HrmPolicies.DepartmentsRead, p => p.RequireClaim("permission", HrmPermissions.DepartmentsRead));
            options.AddPolicy(HrmPolicies.DepartmentsManage, p => p.RequireClaim("permission", HrmPermissions.DepartmentsManage));
            options.AddPolicy(HrmPolicies.AttendanceReadAll, p => p.RequireClaim("permission", HrmPermissions.AttendanceReadAll));
            options.AddPolicy(HrmPolicies.AttendanceManage, p => p.RequireClaim("permission", HrmPermissions.AttendanceManage));
            options.AddPolicy(HrmPolicies.EmployeesRead, p => p.RequireClaim("permission", HrmPermissions.EmployeesRead));
            options.AddPolicy(HrmPolicies.EmployeesManage, p => p.RequireClaim("permission", HrmPermissions.EmployeesManage));
            options.AddPolicy(HrmPolicies.LeavesApprove, p => p.RequireClaim("permission", HrmPermissions.LeavesApprove));
            options.AddPolicy(HrmPolicies.LeavesReadAll, p => p.RequireClaim("permission", HrmPermissions.LeavesReadAll));
            options.AddPolicy(HrmPolicies.PayrollRead, p => p.RequireClaim("permission", HrmPermissions.PayrollReadAll));
            options.AddPolicy(HrmPolicies.PayrollCalculate, p => p.RequireClaim("permission", HrmPermissions.PayrollCalculate));
            options.AddPolicy(HrmPolicies.PayrollApprove, p => p.RequireClaim("permission", HrmPermissions.PayrollApprove));
            options.AddPolicy(HrmPolicies.PayrollPublish, p => p.RequireClaim("permission", HrmPermissions.PayrollPublish));
            options.AddPolicy(HrmPolicies.PayrollExport, p => p.RequireClaim("permission", HrmPermissions.PayrollExport));
            options.AddPolicy(HrmPolicies.PayrollHistoryManage, p => p.RequireClaim("permission", HrmPermissions.PayrollHistoryManage));
            options.AddPolicy(HrmPolicies.AccountsManage, p => p.RequireClaim("permission", HrmPermissions.AccountsManage));
            options.AddPolicy(HrmPolicies.AccountsAssignRoles, p => p.RequireClaim("permission", HrmPermissions.AccountsAssignRoles));
            options.AddPolicy(HrmPolicies.HrScopeManage, p => p.RequireClaim("permission", HrmPermissions.HrScopeManage));
            options.AddPolicy(HrmPolicies.AuditRead, p => p.RequireClaim("permission", HrmPermissions.AuditRead));
        });
        return services;
    }
}

