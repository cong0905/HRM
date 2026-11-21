using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HRM.Next.Application.Authorization;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using HRM.Next.Domain.Personnel;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Leave;
using HRM.Next.Domain.Payroll;
using HRM.Next.Domain.Audit;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Common;
using Microsoft.EntityFrameworkCore;
using HRM.Next.Tests.Integration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Test harness only. Real API/auth, Kestrel HTTPS, owned SQL GUID database.
var root = ApiFactory.RepoRoot;
var output = Path.Combine(root,"docs/verification/t19"); Directory.CreateDirectory(output);
var temp = Directory.CreateTempSubdirectory("HRM-T19-browser-").FullName;
var fixture = new SqlDatabaseFixture();
try
{
    Environment.SetEnvironmentVariable("HRM_T05_SQL_LEDGER",Path.Combine(output,"sql-browser.json"));
    await fixture.InitializeAsync();
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest("CN=localhost",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
    var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback);
    request.CertificateExtensions.Add(san.Build());
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,false));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,false));
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection {new Oid("1.3.6.1.5.5.7.3.1")},false));
    using var generatedCert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddDays(1));
    // Schannel needs a usable imported private key; do not use the ephemeral CreateSelfSigned handle directly.
    var certificatePassword=Guid.NewGuid().ToString("N");
    using var cert=X509CertificateLoader.LoadPkcs12(generatedCert.Export(X509ContentType.Pfx,certificatePassword),certificatePassword,X509KeyStorageFlags.Exportable);
    var certPath=Path.Combine(temp,"localhost.pem"); var keyPath=Path.Combine(temp,"localhost-key.pem");
    File.WriteAllText(certPath,cert.ExportCertificatePem()); File.WriteAllText(keyPath,rsa.ExportPkcs8PrivateKeyPem());
    using var listener = new TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
    await using var harness = new AuthHarness(fixture.ConnectionString,testSender:false);
    harness.Clock.Advance(DateTimeOffset.UtcNow.AddMinutes(1) - harness.Clock.GetUtcNow());
    harness.Factory.UseKestrel(options=>options.ListenLocalhost(port,listen=>listen.UseHttps(cert)));
    harness.Factory.StartServer();
    using(var handler=new HttpClientHandler { ServerCertificateCustomValidationCallback=(_,certificate,_,_)=>certificate?.Thumbprint==cert.Thumbprint })
    using(var probe=new HttpClient(handler))
    {
        try { var response=await probe.GetAsync($"https://localhost:{port}/api/v1/access/me");if(response.StatusCode!=HttpStatusCode.Unauthorized)throw new InvalidOperationException("HTTPS probe did not get401"); }
        catch
        {
            File.WriteAllText(Path.Combine(output,"tls-diagnostic.log"),string.Join(Environment.NewLine,harness.Logs.Messages.Where(x=>new[]{"TLS","SSL","certificate","Kestrel","listening"}.Any(term=>x.Contains(term,StringComparison.OrdinalIgnoreCase)))));
            throw;
        }
    }

    var password="Fixture-only!A9-"+Guid.NewGuid().ToString("N");
    var actors=new Dictionary<string,string>(); var ids=new Dictionary<string,Guid>();
    var employeeIds=new Dictionary<string,Guid>(); var names=new Dictionary<string,string>();
    await using(var scope=harness.Factory.Services.CreateAsyncScope())
    {
        var users=scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var db=scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        foreach(var role in new[]{HrmRoles.Employee,HrmRoles.Hr,HrmRoles.Payroll,HrmRoles.SystemAdmin})
            if(!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new(){Id=Guid.NewGuid(),Name=role});
        foreach(var (actor,role) in new[]{("Employee",HrmRoles.Employee),("EmployeeOther",HrmRoles.Employee),("HR",HrmRoles.Hr),("PayrollCalculator",HrmRoles.Payroll),("PayrollApprover",HrmRoles.Payroll),("SystemAdmin",HrmRoles.SystemAdmin)})
        {
            var name="t19-"+actor.ToLowerInvariant()+"-"+Guid.NewGuid().ToString("N")[..8];
            var user=new HrmUser{Id=Guid.NewGuid(),UserName=name,Email=name+"@example.invalid",IsActive=true};
            if(!(await users.CreateAsync(user,password)).Succeeded || !(await users.AddToRoleAsync(user,role)).Succeeded)throw new InvalidOperationException("Actor seed failed");
            actors[actor]=name;ids[actor]=user.Id;
        }
        using var calendarDoc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/fixtures/work-calendars.demo.json")));
        var item=calendarDoc.RootElement.GetProperty("fixtures").EnumerateArray().Single(x=>x.GetProperty("definition").GetProperty("fixtureId").GetString()=="FIXTURE-CAL-2026-04-A");
        var definition=item.GetProperty("definition").Deserialize<HRM.Next.Domain.Calendar.WorkCalendarDefinition>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var calendar=new WorkCalendarVersion{Id=Guid.NewGuid(),EffectiveFrom=new(2026,4,1),DefinitionJson=JsonSerializer.Serialize(definition),CreatedAtUtc=harness.Clock.GetUtcNow()};
        db.WorkCalendarVersions.Add(calendar);
        foreach(var (actor,name) in new[]{("Employee","T19 Nguyễn Một"),("EmployeeOther","T19 Trần Hai")})
        {
            var profile=new EmployeeProfile{Id=Guid.NewGuid(),UserId=ids[actor],EmployeeCode=actor=="Employee"?"NV000001":"NV000002",FullName=name,HireDate=new(2026,1,1),Status="DangLamViec",CreatedAtUtc=harness.Clock.GetUtcNow(),UpdatedAtUtc=harness.Clock.GetUtcNow()};
            db.EmployeeProfiles.Add(profile);employeeIds[actor]=profile.Id;names[actor]=name;
            db.HrEmployeeAssignments.Add(new(){Id=Guid.NewGuid(),HrUserId=ids["HR"],EmployeeId=profile.Id,AssignedAtUtc=harness.Clock.GetUtcNow()});
            db.LeaveBalances.Add(new(){Id=Guid.NewGuid(),EmployeeId=profile.Id,Year=2026,AllocatedDays=12m,UsedDays=actor=="Employee"?1m:0m,UpdatedAtUtc=harness.Clock.GetUtcNow()});
        }
        var paidDay=Enumerable.Range(1,30).Select(i=>new DateOnly(2026,4,i)).First(d=>CalendarMath.StandardMinutes(definition,d)>0m);
        var leaveId=Guid.NewGuid();
        db.LeaveRecords.Add(new(){Id=leaveId,EmployeeId=employeeIds["Employee"],StartDate=paidDay,EndDate=paidDay,WorkingDays=1m,Status="Approved",Reason="T19 paid leave hand-calculated fixture",DecisionReason="T19 seeded approved leave",DecidedAtUtc=harness.Clock.GetUtcNow(),CreatedAtUtc=harness.Clock.GetUtcNow()});
        db.LeaveLedgerEntries.Add(new(){Id=Guid.NewGuid(),EmployeeId=employeeIds["Employee"],LeaveRequestId=leaveId,Year=2026,Action="Debit",Days=1m,BalanceBefore=12m,BalanceAfter=11m,Reason="T19 approved fixture debit",ActorId=ids["HR"],CreatedAtUtc=harness.Clock.GetUtcNow()});
        foreach(var day in Enumerable.Range(1,30).Select(i=>new DateOnly(2026,4,i)).Where(d=>HRM.Next.Domain.Calendar.CalendarMath.StandardMinutes(definition,d)>0m && d!=paidDay))
        {
            var start=new DateTimeOffset(day.ToDateTime(new TimeOnly(1,30)),TimeSpan.Zero);
            db.AttendanceSummaries.Add(new(){Id=Guid.NewGuid(),EmployeeId=employeeIds["Employee"],BusinessDate=day,CheckInUtc=start,CheckOutUtc=start.AddHours(9),CalendarVersionId=calendar.Id,CalendarDefinitionJson=calendar.DefinitionJson,EligibleMinutes=480m,Phase="Complete",UpdatedAtUtc=start.AddHours(9)});
        }
        var june=new DateOnly(2026,6,1);var juneStart=new DateTimeOffset(june.ToDateTime(new TimeOnly(1,30)),TimeSpan.Zero);
        db.AttendanceSummaries.Add(new(){Id=Guid.NewGuid(),EmployeeId=employeeIds["Employee"],BusinessDate=june,CheckInUtc=juneStart,CheckOutUtc=null,CalendarVersionId=calendar.Id,CalendarDefinitionJson=calendar.DefinitionJson,EligibleMinutes=0m,Phase="Open",UpdatedAtUtc=juneStart});
        await db.SaveChangesAsync();
    }
    var secret=Path.Combine(temp,"browser-fixture.json");
    File.WriteAllText(secret,JsonSerializer.Serialize(new{apiOrigin=$"https://localhost:{port}",certPath,keyPath,actors,password,employeeIds,names}));
    var stopFile=Path.Combine(temp,"stop");
    File.WriteAllText(Path.Combine(output,"browser-host.json"),JsonSerializer.Serialize(new{status="READY",apiOrigin=$"https://localhost:{port}",fixtureFile=secret,databaseName=fixture.DatabaseName,stopFile},new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("READY: T19 real API/auth + isolated SQL fixture; temporary credentials not logged.");
    var deadline=DateTimeOffset.UtcNow.AddMinutes(90);
    while(DateTimeOffset.UtcNow<deadline && !File.Exists(stopFile)) await Task.Delay(300);
    await using(var db=fixture.CreateContext())
    {
        var periods=await db.PayrollPeriods.Select(x=>new{x.Id,x.PeriodCode,x.Status,x.ActiveBatchId,x.ApprovedBatchId,x.PublishedBatchId}).ToListAsync();
        var batches=await db.PayrollCalculationBatches.Select(x=>new{x.Id,x.PeriodId,x.Version,x.InputHash}).ToListAsync();
        var slips=await db.Payslips.Select(x=>new{x.Id,x.PeriodId,x.EmployeeId,x.Amount,x.Status,x.SnapshotId}).ToListAsync();
        var snapshots=await db.PayrollCalculationSnapshots.Select(x=>new{x.Id,x.EmployeeId,x.Net,x.CanPublish}).ToListAsync();
        File.WriteAllText(Path.Combine(output,"browser-sql-state.json"),JsonSerializer.Serialize(new{periods,batches,slips,snapshots},new JsonSerializerOptions{WriteIndented=true}));
    }
}
finally
{
    await fixture.DisposeAsync();
    var resolved=Path.GetFullPath(temp);
    if(!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(resolved).StartsWith("HRM-T19-browser-",StringComparison.Ordinal))throw new InvalidOperationException("Cleanup outside owned temp");
    Directory.Delete(resolved,true);
    Console.WriteLine("STOPPED: T19 owned SQL database and temporary credentials cleaned.");
}
