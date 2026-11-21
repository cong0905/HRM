using HRM.Next.Application.Payroll;
using HRM.Next.Infrastructure.Payroll;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Attendance;
using HRM.Next.Infrastructure.Attendance;
using HRM.Next.Application.Calendar;
using HRM.Next.Infrastructure.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Departments;
using HRM.Next.Application.Leave;
using HRM.Next.Infrastructure.Leave;
using HRM.Next.Infrastructure.Notifications;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Hrm") ?? string.Empty)
            .ValidateOnStart();
        services.AddDbContext<HrmDbContext>((provider, options) =>
        {
            var settings = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(settings.ConnectionString, sql =>
                { sql.UseCompatibilityLevel(160); sql.CommandTimeout(15); })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
        services.AddScoped<IPayrollReadService, PayrollReadService>();
        services.AddScoped<IPayrollWorkflowService, PayrollWorkflowService>();
        services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<BusinessClock>();
        services.AddScoped<IWorkCalendarService, WorkCalendarService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<DepartmentStore>();
        services.AddScoped<IDepartmentQueries>(provider => provider.GetRequiredService<DepartmentStore>());
        services.AddScoped<IDepartmentRepository>(provider => provider.GetRequiredService<DepartmentStore>());
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Email notifications (T22)
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName);
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddSingleton<DevSinkInbox>();
        services.AddScoped<EmailOutboxService>();

        var deliveryMode = configuration[$"{EmailOptions.SectionName}:DeliveryMode"] ?? "dev-sink";
        if (deliveryMode.Equals("smtp", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, DevSinkEmailSender>();

        return services;
    }
}
