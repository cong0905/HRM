using System.Net;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration;
public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task Payslip_ExportRejectsStoredAmountThatDiffersFromPublishedSnapshot()
    {
        await using var s = await T14StartAsync();
        foreach (var action in new[] { "calculate", "approve", "publish" }) await T14OkAsync(s, action);
        // Fault injection only: bypass the production EF immutability guard to model storage corruption.
        await using (var db = sql.CreateContext())
            await db.Payslips.Where(x => x.PeriodId == s.PeriodId).ExecuteUpdateAsync(x => x.SetProperty(p => p.Amount, 1m));
        using var client = await BearerClientAsync(s.Seed.Harness, s.Reviewer);
        using var response = await client.GetAsync($"/api/v1/payroll-periods/{s.PeriodId}/export");
        var problem = await ProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("payroll.run_items_mismatch", problem.GetProperty("errorCode").GetString());
        await using var verify = sql.CreateContext();
        Assert.False(await verify.AuditEvents.AnyAsync(x => x.Action == "payroll.exported"));
    }
}
