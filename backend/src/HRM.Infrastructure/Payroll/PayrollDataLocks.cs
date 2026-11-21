using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Payroll;

public static class PayrollDataLocks
{
    public static Task SourceAsync(HrmDbContext db,bool exclusive,CancellationToken token)
    {
        var mode=exclusive?"Exclusive":"Shared";
        return db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'HRM.Payroll.SalaryInputs', @LockMode={mode}, @LockOwner=N'Transaction', @LockTimeout=15000; IF @r<0 THROW 51013, 'Payroll inputs lock busy.', 1;",token);
    }
}
