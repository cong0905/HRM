using System.Net;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Next.Tests.Integration;
public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task Workflow_OutputMustMatchApprovedSnapshot_BeforePublication()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");
        // Simulates a faulty writer changing only the draft projection, not the immutable run.
        await using(var db=sql.CreateContext()){var slip=await db.Payslips.AsTracking().SingleAsync();slip.Amount=1m;await db.SaveChangesAsync();}
        using(var publish=await T14SendAsync(s,"publish")){var problem=await ProblemAsync(publish,HttpStatusCode.Conflict);Assert.Equal("payroll.run_items_mismatch",problem.GetProperty("errorCode").GetString());}
        await using var verify=sql.CreateContext();Assert.Equal("Approved",(await verify.PayrollPeriods.SingleAsync()).Status);Assert.Equal("Draft",(await verify.Payslips.SingleAsync()).Status);Assert.Equal(0,await verify.AuditEvents.CountAsync(x=>x.Action=="payroll.workflow_publish"));
    }
}
