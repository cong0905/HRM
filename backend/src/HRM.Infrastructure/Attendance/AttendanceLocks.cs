using System.Globalization;
using HRM.Next.Application.Common;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Attendance;

public static class AttendanceLocks
{
    private static async Task AcquireAsync(HrmDbContext db,string resource,string mode,CancellationToken token)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode={mode}, @LockOwner=N'Transaction', @LockTimeout=10000; IF @r<0 THROW 51010, 'Attendance lock busy.', 1;",token);
    }
    public static Task EmployeeAsync(HrmDbContext db,Guid employeeId,CancellationToken token) =>
        AcquireAsync(db,"HRM.Employee.Operation."+employeeId.ToString("N"),"Exclusive",token);
    public static Task MonthAsync(HrmDbContext db,DateOnly date,bool exclusive,CancellationToken token) =>
        AcquireAsync(db,"HRM.Payroll.Month."+date.ToString("yyyy-MM",CultureInfo.InvariantCulture),exclusive?"Exclusive":"Shared",token);
    public static Task PublicationAsync(HrmDbContext db,string periodCode,CancellationToken token)
    {
        if(periodCode.Length>=7 && (periodCode.Length==7 || periodCode[7] is '-' or '_') &&
            DateOnly.TryParseExact(periodCode[..7]+"-01","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))
            return MonthAsync(db,date,true,token);
        return Task.CompletedTask;
    }
}
