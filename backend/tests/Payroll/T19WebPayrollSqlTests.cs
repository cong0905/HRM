using System.Net;
using System.Net.Http.Json;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Next.Tests.Integration;
public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task T19_PeriodList_CompanyPayrollReadOnly_RejectsOtherRoles()
    {
        await using var s=await T14StartAsync();
        foreach(var actor in new[]{s.Seed.Worker,s.Seed.Hr,s.Seed.Admin})
        {using var denied=await BearerClientAsync(s.Seed.Harness,actor);using var response=await denied.GetAsync("/api/v1/payroll-periods");await ProblemAsync(response,HttpStatusCode.Forbidden);}
        using var client=await BearerClientAsync(s.Seed.Harness,s.Calculator);
        var result=await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods");
        Assert.Equal(s.PeriodId,Assert.Single(result!.Items).Id);
    }
    [Fact]
    public async Task T19_PeriodList_ValidatesFiltersOffset_AndPaginatesDeterministically()
    {
        await using var s=await T14StartAsync();
        await using(var db=sql.CreateContext())
        {db.PayrollPeriods.AddRange(new PayrollPeriodRecord{Id=Guid.NewGuid(),PeriodCode="2027-01",Status="Draft",CreatedAtUtc=DateTimeOffset.UtcNow},new PayrollPeriodRecord{Id=Guid.NewGuid(),PeriodCode="2026-12",Status="Locked",CreatedAtUtc=DateTimeOffset.UtcNow});await db.SaveChangesAsync();}
        using var client=await BearerClientAsync(s.Seed.Harness,s.Calculator);
        foreach(var query in new[]{"page=0","pageSize=101","year=1999","status=arbitrary","page=2147483647&pageSize=100"})
        {using var bad=await client.GetAsync("/api/v1/payroll-periods?"+query);await ProblemAsync(bad,HttpStatusCode.BadRequest);}
        var first=(await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods?pageSize=1"))!;
        var second=(await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods?pageSize=1&page=2"))!;
        Assert.Equal(3,first.TotalCount);Assert.Equal("2027-01",Assert.Single(first.Items).PeriodCode);Assert.Equal("2026-12",Assert.Single(second.Items).PeriodCode);
        var filtered=(await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods?year=2026&status=%20locked%20"))!;
        Assert.Equal(1,filtered.TotalCount);Assert.Equal("2026-12",Assert.Single(filtered.Items).PeriodCode);
        Assert.Empty((await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods?page=999"))!.Items);
    }
    [Fact]
    public async Task T19_PeriodList_TracksRunVersion_AndGetReloadHasNoFinancialWrites()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T13ManualAsync(s,"Bonus","0.50");await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");
        using var client=await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        await using var db=sql.CreateContext();
        var before=new{batches=await db.PayrollCalculationBatches.CountAsync(),audit=await db.AuditEvents.CountAsync(),receipts=await db.PayrollInputReceipts.CountAsync(),amount=await db.Payslips.Select(x=>x.Amount).SingleAsync()};
        for(var i=0;i<3;i++)
        {var list=(await client.GetFromJsonAsync<PagedResult<PayrollWorkflowState>>("/api/v1/payroll-periods"))!;var item=Assert.Single(list.Items);Assert.Equal(2,item.RunVersion);Assert.Equal("Published",item.Status);Assert.Equal((await T14StateAsync(s)).RowVersion,item.RowVersion);}
        var after=new{batches=await db.PayrollCalculationBatches.CountAsync(),audit=await db.AuditEvents.CountAsync(),receipts=await db.PayrollInputReceipts.CountAsync(),amount=await db.Payslips.Select(x=>x.Amount).SingleAsync()};
        Assert.Equal(before,after);Assert.Equal(12000001m,after.amount);
    }
}
