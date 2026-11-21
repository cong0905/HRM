using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;
public sealed partial class FoundationSqlTests
{
    private static async Task<Guid> T15SlipAsync(T13Scenario s)
    { await using var db = s.Sql.CreateContext(); return await db.Payslips.Where(x => x.EmployeeId == s.Seed.Profile.Id && x.PeriodId == s.PeriodId).Select(x => x.Id).SingleAsync(); }
    private static async Task T15PublishAsync(T13Scenario s)
    { foreach (var action in new[] { "calculate", "approve", "publish" }) await T14OkAsync(s, action); }
    private static string T15Export(T13Scenario s) => $"/api/v1/payroll-periods/{s.PeriodId}/export";
    private static string T15Reconcile(T13Scenario s) => $"/api/v1/payroll-periods/{s.PeriodId}/reconciliation";
    private static async Task T15LabelAsync(T13Scenario s, string name, string? code = null)
    {
        await using var db = s.Sql.CreateContext(); var p = await db.EmployeeProfiles.AsTracking().SingleAsync(x => x.Id == s.Seed.Profile.Id);
        p.FullName = name; p.CitizenId = "T15-PRIVATE-CITIZEN"; p.Address = "T15-PRIVATE-ADDRESS"; await db.SaveChangesAsync();
        // Test-only imported legacy label: production employee codes remain immutable.
        if (code is not null) await db.EmployeeProfiles.Where(x=>x.Id==p.Id).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.EmployeeCode,code));
    }
    private static List<string[]> T15Csv(string text)
    {
        var rows = new List<string[]>(); var cells = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        text = text.TrimStart('\ufeff');
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
            else if (c == ',' && !quoted) { cells.Add(cell.ToString()); cell.Clear(); }
            else if (c == '\r' && !quoted && i + 1 < text.Length && text[i + 1] == '\n')
            { cells.Add(cell.ToString()); rows.Add(cells.ToArray()); cells.Clear(); cell.Clear(); i++; }
            else cell.Append(c);
        }
        Assert.False(quoted); Assert.Empty(cells); Assert.Equal(0, cell.Length); return rows;
    }

    [Fact]
    public async Task Payslip_DetailAndSummary_ReconcileAllEmployeesAndSignedRoundingLines()
    {
        await using var s = await T14StartAsync(); await T13ManualAsync(s, "Bonus", "0.50");
        var other = await CreateUserWithRolesAsync(s.Seed.Harness, "t15-second", HrmRoles.Employee);
        var profile = await CreateEmployeeProfileAsync(s.Seed.Harness, other, "Nguyễn Thị Hai", "T15-second");
        await using (var db = sql.CreateContext()) { var p = await db.EmployeeProfiles.AsTracking().SingleAsync(x => x.Id == profile.Id); p.HireDate = new(2026,1,1); await db.SaveChangesAsync(); }
        using (var salary = await AttPostAsync(s.Seed.Harness, s.Calculator, $"/api/v1/employees/{profile.Id}/salary-history", new { effectiveFrom = "2026-01-01", monthlyBase = "9000000", reason = "T15 explicit second salary" }, Guid.NewGuid())) Assert.Equal(HttpStatusCode.Created, salary.StatusCode);
        using (var bonus = await AttPostAsync(s.Seed.Harness, s.Calculator, $"/api/v1/payroll-periods/{s.PeriodId}/adjustments", new { employeeId = profile.Id, kind = "Bonus", amount = "1000.50", reason = "T15 explicit second bonus" }, Guid.NewGuid())) Assert.Equal(HttpStatusCode.Created, bonus.StatusCode);
        await T15PublishAsync(s); using var client = await BearerClientAsync(s.Seed.Harness, s.Reviewer);
        var summary = (await client.GetFromJsonAsync<PayrollReconciliationResponse>(T15Reconcile(s)))!;
        Assert.Equal(2, summary.PayslipCount); Assert.Equal(12001002m, summary.PayslipNet); Assert.Equal(summary.SnapshotNet, summary.PayslipNet); Assert.Equal(0m, summary.Difference);
        var detail = (await client.GetFromJsonAsync<PayslipDetailResponse>($"/api/v1/payslips/{await T15SlipAsync(s)}"))!;
        Assert.Equal(12000001m, detail.Calculation.Net); Assert.Equal(detail.Calculation.Net, detail.Calculation.Lines.Sum(x => x.Amount)); Assert.Equal(summary.BatchId, detail.BatchId); Assert.Equal(summary.RunVersion, detail.RunVersion);
        Assert.Equal("POL-2026-V1", detail.Policy.Version); Assert.Equal("VND", detail.Currency); Assert.NotEmpty(detail.Calendar); Assert.NotEmpty(detail.Days);
        using (var bounded = await client.GetAsync(T15Export(s)+"?maxRows=1"))
        { var problem = await ProblemAsync(bounded,HttpStatusCode.Conflict); Assert.Equal("payroll.export_limit_exceeded",problem.GetProperty("errorCode").GetString()); }
        await using (var db = sql.CreateContext()) Assert.False(await db.AuditEvents.AnyAsync(x=>x.Action=="payroll.exported"));
        using var export = await client.GetAsync(T15Export(s)); Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var rows = T15Csv(await export.Content.ReadAsStringAsync()); var netColumn = Array.IndexOf(rows[0], "Net");
        Assert.Equal(summary.PayslipNet, rows.Skip(1).Sum(row => decimal.Parse(row[netColumn], CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Payslip_OwnerCannotReadCalculatedOrApprovedDetail(bool approved)
    {
        await using var s = await T14StartAsync(); await T14OkAsync(s,"calculate"); if (approved) await T14OkAsync(s,"approve");
        var id = await T15SlipAsync(s); using var owner = await BearerClientAsync(s.Seed.Harness,s.Seed.Worker);
        using var response = await owner.GetAsync($"/api/v1/me/payslips/{id}"); await ProblemAsync(response,HttpStatusCode.NotFound);
        using var finance = await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        using var preview = await finance.GetAsync($"/api/v1/payslips/{id}"); Assert.Equal(HttpStatusCode.OK,preview.StatusCode);
    }

    [Fact]
    public async Task Payslip_OwnerPublishedAndLockedDetail_PreventsIdor()
    {
        await using var s = await T14StartAsync(); await T15PublishAsync(s); var id = await T15SlipAsync(s);
        using var owner = await BearerClientAsync(s.Seed.Harness,s.Seed.Worker);
        foreach (var locked in new[] {false,true})
        {
            if (locked) await T14OkAsync(s,"lock");
            using var r = await owner.GetAsync($"/api/v1/me/payslips/{id}"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        }
        var other = await CreateUserWithRolesAsync(s.Seed.Harness,"t15-outsider",HrmRoles.Employee); using var outsider = await BearerClientAsync(s.Seed.Harness,other);
        using (var r = await outsider.GetAsync($"/api/v1/me/payslips/{id}")) await ProblemAsync(r,HttpStatusCode.NotFound);
        using (var r = await owner.GetAsync($"/api/v1/payslips/{id}")) await ProblemAsync(r,HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Employee")] [InlineData("HR")] [InlineData("SystemAdmin")]
    public async Task Payslip_NonPayrollRolesCannotReadReconcileOrExport(string role)
    {
        await using var s = await T14StartAsync(); var actor = await CreateUserWithRolesAsync(s.Seed.Harness,"t15-denied",role); using var client = await BearerClientAsync(s.Seed.Harness,actor);
        foreach (var route in new[] {T15Export(s),T15Reconcile(s),$"/api/v1/payslips/{Guid.NewGuid()}"})
        { using var r = await client.GetAsync(route); await ProblemAsync(r,HttpStatusCode.Forbidden); }
    }

    [Fact]
    public async Task Payslip_MultipleRoles_DoNotGrantExportToHrEmployee()
    {
        await using var s = await T14StartAsync(); await T15PublishAsync(s);
        await using (var scope = s.Seed.Harness.Factory.Services.CreateAsyncScope())
        { var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<HrmUser>>(); Assert.True((await manager.AddToRoleAsync(s.Seed.Worker,HrmRoles.Hr)).Succeeded); }
        using var client = await BearerClientAsync(s.Seed.Harness,s.Seed.Worker);
        using (var own = await client.GetAsync($"/api/v1/me/payslips/{await T15SlipAsync(s)}")) Assert.Equal(HttpStatusCode.OK,own.StatusCode);
        using var export = await client.GetAsync(T15Export(s)); await ProblemAsync(export,HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("=1+1")] [InlineData("+1+1")] [InlineData("-1+1")] [InlineData("@SUM(1)")]
    public async Task Payslip_CsvFormulaNamesAndCodes_AreTextCells(string name)
    {
        await using var s = await T14StartAsync(); await T15LabelAsync(s,name,"=1+1"); await T15PublishAsync(s);
        using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer); using var r = await client.GetAsync(T15Export(s)); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var rows = T15Csv(await r.Content.ReadAsStringAsync()); Assert.Equal(2,rows.Count); Assert.Equal("'"+name,rows[1][2]); Assert.Equal("'=1+1",rows[1][1]);
    }

    [Fact]
    public async Task Payslip_CsvUtf8QuotesNewlinesAndAudit_ExcludePrivateFieldsAndMoneyPayload()
    {
        await using var s = await T14StartAsync(); const string name = "Nguyễn, \"Công\"\r\nĐỗ"; await T15LabelAsync(s,name); await T15PublishAsync(s);
        using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer); using var r = await client.GetAsync(T15Export(s)); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Equal("utf-8",r.Content.Headers.ContentType!.CharSet); Assert.StartsWith("payroll-",r.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        var bytes = await r.Content.ReadAsByteArrayAsync(); Assert.Equal(new byte[] {239,187,191},bytes.Take(3)); var text = Encoding.UTF8.GetString(bytes); var rows = T15Csv(text); Assert.Equal(name,rows[1][2]); Assert.All(rows,row=>Assert.Equal(rows[0].Length,row.Length));
        Assert.DoesNotContain("T15-PRIVATE",text); Assert.DoesNotContain("Citizen",text); Assert.DoesNotContain("Bank",text);
        await using var db = sql.CreateContext(); var entry = await db.AuditEvents.SingleAsync(x=>x.Action=="payroll.exported");
        Assert.Equal(s.Reviewer.Id,entry.ActorId); Assert.DoesNotContain(name,entry.Payload); Assert.DoesNotContain("12000000",entry.Payload); Assert.DoesNotContain("T15-PRIVATE",entry.Payload); Assert.Contains("exportedRows",entry.Payload);
    }

    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(1001)]
    public async Task Payslip_InvalidExportLimits_AreBadRequests(int maxRows)
    {
        await using var s = await T14StartAsync(); using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        using var r = await client.GetAsync(T15Export(s)+$"?maxRows={maxRows}"); await ProblemAsync(r,HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Payslip_DraftExportIsNotAStatement()
    {
        await using var s = await T14StartAsync(); await T14OkAsync(s,"calculate"); using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        using var r = await client.GetAsync(T15Export(s)); var p = await ProblemAsync(r,HttpStatusCode.Conflict); Assert.Equal("payroll.period_not_published",p.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Payslip_ExportAuditFailure_DoesNotReturnFileOrPersistPartialAudit()
    {
        await using var s = await T14StartAsync(); await T15PublishAsync(s); var probe = new T13AuditProbe();
        await using var faulty = new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository>(sp=>new T13FaultAudit(sp.GetRequiredService<HrmDbContext>(),probe,"payroll.exported")));
        using var client = await BearerClientAsync(faulty,s.Reviewer); using var r = await client.GetAsync(T15Export(s)); await ProblemAsync(r,HttpStatusCode.InternalServerError); Assert.Equal(1,probe.Calls);
        await using var db = sql.CreateContext(); Assert.False(await db.AuditEvents.AnyAsync(x=>x.Action=="payroll.exported")); Assert.Equal("Published",(await db.PayrollPeriods.SingleAsync()).Status);
    }

    [Fact]
    public async Task Payslip_DetailAndReconciliationQueries_DoNotWrite()
    {
        await using var s = await T14StartAsync(); await T15PublishAsync(s); var id = await T15SlipAsync(s); var observer = new QueryObserver();
        await using var reader = new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(observer)));
        using var finance = await BearerClientAsync(reader,s.Reviewer); using var owner = await BearerClientAsync(reader,s.Seed.Worker); observer.Reset();
        foreach (var route in new[] {$"/api/v1/payslips/{id}",T15Reconcile(s)}) { using var r = await finance.GetAsync(route); Assert.Equal(HttpStatusCode.OK,r.StatusCode); }
        using (var r = await owner.GetAsync($"/api/v1/me/payslips/{id}")) Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Equal(0,observer.SaveCalls); Assert.DoesNotContain(observer.Commands,x=>x.Contains("INSERT",StringComparison.OrdinalIgnoreCase)||x.Contains("UPDATE",StringComparison.OrdinalIgnoreCase)||x.Contains("DELETE",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Payslip_HistoricalMoneyUsesPublishedSnapshot_AfterSalaryAndNameChange()
    {
        await using var s = await T14StartAsync(); await T15PublishAsync(s); await T14OkAsync(s,"lock"); await T13SalaryAsync(s,"99000000",new(2026,6,1)); await T15LabelAsync(s,"Tên hiện tại");
        using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer); var d = (await client.GetFromJsonAsync<PayslipDetailResponse>($"/api/v1/payslips/{await T15SlipAsync(s)}"))!;
        Assert.Equal(12000000m,d.Calculation.Net); Assert.Equal(1,d.RunVersion); using var r = await client.GetAsync(T15Export(s)); Assert.Equal(HttpStatusCode.OK,r.StatusCode); Assert.Contains("Tên hiện tại",await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Payslip_ConcurrentPublishAndExport_ReturnsWholePublishedRunOrConflict()
    {
        await using var s = await T14StartAsync(); await T14OkAsync(s,"calculate"); await T14OkAsync(s,"approve"); using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        var publish = T14SendAsync(s,"publish"); var export = client.GetAsync(T15Export(s)); await Task.WhenAll(publish,export); using var p = await publish; using var e = await export; Assert.Equal(HttpStatusCode.NoContent,p.StatusCode);
        Assert.Contains(e.StatusCode,new[] {HttpStatusCode.OK,HttpStatusCode.Conflict}); if (e.StatusCode==HttpStatusCode.OK) { var rows = T15Csv(await e.Content.ReadAsStringAsync()); Assert.Equal("Published",rows[1][Array.IndexOf(rows[0],"Status")]); }
    }

    [Fact]
    public async Task Payslip_MissingResources_AreNotFound()
    {
        await using var s = await T14StartAsync(); using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer);
        foreach (var route in new[] {$"/api/v1/payslips/{Guid.NewGuid()}",$"/api/v1/payroll-periods/{Guid.NewGuid()}/reconciliation",$"/api/v1/payroll-periods/{Guid.NewGuid()}/export"}) { using var r = await client.GetAsync(route); await ProblemAsync(r,HttpStatusCode.NotFound); }
    }

    [Fact]
    public async Task Payslip_MetadataOnlyPeriodCannotPretendToReconcile()
    {
        await using var s = await T13StartAsync(); using var client = await BearerClientAsync(s.Seed.Harness,s.Reviewer); using var r = await client.GetAsync(T15Reconcile(s)); await ProblemAsync(r,HttpStatusCode.Conflict);
    }
}
