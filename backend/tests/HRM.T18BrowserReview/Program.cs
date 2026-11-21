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
var output = Environment.GetEnvironmentVariable("HRM_DEMO_OUTPUT") ?? Path.Combine(root,"docs/verification/t18"); Directory.CreateDirectory(output);
var temp = Directory.CreateTempSubdirectory("HRM-T18-browser-").FullName;
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
    await using var harness = new AuthHarness(fixture.ConnectionString,testSender:false, configure: services =>
        services.AddScoped<IAuditRepository>(provider => new T18FaultAudit(new AuditRepository(provider.GetRequiredService<HrmDbContext>()), provider.GetRequiredService<HrmDbContext>(), Path.Combine(output,"rollback-flush.json"))));
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
    var suffix = Guid.NewGuid().ToString("N")[..10];
    var password="Fixture-only!A9-"+Guid.NewGuid().ToString("N");
    var actors = new Dictionary<string,string>();
    await using(var scope=harness.Factory.Services.CreateAsyncScope())
    {
        var users=scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach(var role in new[]{HrmRoles.Employee,HrmRoles.Hr,HrmRoles.Payroll,HrmRoles.SystemAdmin})
        {
            if(!await roles.RoleExistsAsync(role))
            {var created=await roles.CreateAsync(new(){Id=Guid.NewGuid(),Name=role});if(!created.Succeeded)throw new InvalidOperationException("Role seed failed");}
            var username="t18-"+role.ToLowerInvariant()+"-"+suffix;
            var user=new HrmUser{Id=Guid.NewGuid(),UserName=username,Email=username+"@example.invalid",IsActive=true};
            var result=await users.CreateAsync(user,password);if(!result.Succeeded)throw new InvalidOperationException("User seed failed");
            result=await users.AddToRoleAsync(user,role);if(!result.Succeeded)throw new InvalidOperationException("Role assignment seed failed");actors[role]=username;
        }
    }
    var employeeIds=new Dictionary<string,Guid>();
    await using(var scope=harness.Factory.Services.CreateAsyncScope())
    {
        var db=scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var hr=await users.FindByNameAsync(actors[HrmRoles.Hr]);
        if(!(await users.AddToRoleAsync(hr!,HrmRoles.Employee)).Succeeded)throw new InvalidOperationException("HR employee role seed failed");
        foreach(var pair in new[]{("HR2",HrmRoles.Hr),("EmployeeNoBalance",HrmRoles.Employee)})
        {
            var name="t18-"+pair.Item1.ToLowerInvariant()+"-"+suffix;
            var user=new HrmUser{Id=Guid.NewGuid(),UserName=name,Email=name+"@example.invalid",IsActive=true};
            if(!(await users.CreateAsync(user,password)).Succeeded || !(await users.AddToRoleAsync(user,pair.Item2)).Succeeded)throw new InvalidOperationException("Additional actor seed failed");
            actors[pair.Item1]=name;
        }
        foreach(var key in new[]{HrmRoles.Employee,HrmRoles.Hr,"HR2","EmployeeNoBalance"})
        {
            var user=(await users.FindByNameAsync(actors[key]))!;
            var profile=new EmployeeProfile{Id=Guid.NewGuid(),UserId=user.Id,FullName="T18 "+key,EmployeeCode=$"T18-{employeeIds.Count+1:D3}",CitizenId=$"t18-citizen-{employeeIds.Count+1:D3}",Email=user.Email!,Phone="0901234567",Address="Địa chỉ thử T18",DateOfBirth=new(2000,1,1),Gender="Nam",HireDate=new(2026,1,1),CreatedAtUtc=harness.Clock.GetUtcNow()};
            db.EmployeeProfiles.Add(profile);employeeIds[key]=profile.Id;
            if(key!="EmployeeNoBalance")db.LeaveBalances.Add(new(){Id=Guid.NewGuid(),EmployeeId=profile.Id,Year=2026,AllocatedDays=key==HrmRoles.Employee?3m:12m,UsedDays=0m,UpdatedAtUtc=harness.Clock.GetUtcNow()});
        }
        var hr2=(await users.FindByNameAsync(actors["HR2"]))!;
        foreach(var actorId in new[]{hr!.Id,hr2.Id})foreach(var target in employeeIds.Values)
            db.HrEmployeeAssignments.Add(new(){Id=Guid.NewGuid(),HrUserId=actorId,EmployeeId=target,AssignedAtUtc=harness.Clock.GetUtcNow()});
        var definition=new WorkCalendarDefinition("T18-fixture","Asia/Ho_Chi_Minh",new(8,30),new(17,30),[new(new(12,0),new(13,0))],[DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday],[]);
        var calendar=new WorkCalendarVersion{Id=Guid.NewGuid(),EffectiveFrom=new(2026,1,1),DefinitionJson=JsonSerializer.Serialize(definition),CreatedAtUtc=harness.Clock.GetUtcNow()};db.WorkCalendarVersions.Add(calendar);
        foreach(var (day,closed) in new[]{(new DateOnly(2026,11,13),false),(new DateOnly(2026,10,30),true)})
            db.AttendanceSummaries.Add(new(){Id=Guid.NewGuid(),EmployeeId=employeeIds[HrmRoles.Employee],BusinessDate=day,CheckInUtc=new(day.ToDateTime(new TimeOnly(8,30)),TimeSpan.FromHours(7)),CheckOutUtc=closed?new DateTimeOffset(day.ToDateTime(new TimeOnly(17,30)),TimeSpan.FromHours(7)):null,CalendarVersionId=calendar.Id,CalendarDefinitionJson=calendar.DefinitionJson,EligibleMinutes=closed?480m:0m,Phase=closed?"Complete":"Open",UpdatedAtUtc=harness.Clock.GetUtcNow()});
        db.PayrollPeriods.Add(new(){Id=Guid.NewGuid(),PeriodCode="2026-10",Status="Locked",CreatedAtUtc=harness.Clock.GetUtcNow()});
        db.LeaveRecords.Add(new(){Id=Guid.NewGuid(),EmployeeId=employeeIds[HrmRoles.Employee],StartDate=new(2026,10,29),EndDate=new(2026,10,29),WorkingDays=1m,Status="Pending",Reason="T18 locked fixture",CreatedAtUtc=harness.Clock.GetUtcNow()});
        await db.SaveChangesAsync();
    }
    var advanceFile=Path.Combine(temp,"advance-minutes");
    var secretState=Path.Combine(temp,"browser-fixture.json");
    File.WriteAllText(secretState,JsonSerializer.Serialize(new{apiOrigin=$"https://localhost:{port}",certPath,keyPath,actors,password,employeeIds,advanceFile}));
    var ready=new{status="READY",apiOrigin=$"https://localhost:{port}",fixtureFile=secretState,databaseName=fixture.DatabaseName,stopFile=Path.Combine(temp,"stop"),expireFile=Path.Combine(temp,"expire")};
    File.WriteAllText(Path.Combine(output,"browser-host.json"),JsonSerializer.Serialize(ready,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("READY: real Kestrel HTTPS and owned SQL fixture; credentials stored only in temporary fixture file.");
    var duration = int.TryParse(Environment.GetEnvironmentVariable("HRM_DEMO_MINUTES"), out var requestedMinutes) ? Math.Clamp(requestedMinutes, 1, 240) : 25;
    var deadline=DateTimeOffset.UtcNow.AddMinutes(duration);var expired=false;
    while(DateTimeOffset.UtcNow<deadline && !File.Exists(ready.stopFile))
    {
        if(!expired && File.Exists(ready.expireFile)){harness.Clock.Advance(TimeSpan.FromDays(8));expired=true;Console.WriteLine("Test clock advanced; existing sessions expired.");}
        if(File.Exists(advanceFile))
        {
            var text=File.ReadAllText(advanceFile);File.Delete(advanceFile);
            if(int.TryParse(text,out var minutes)&&minutes is >0 and <=1440)harness.Clock.Advance(TimeSpan.FromMinutes(minutes));
        }
        await Task.Delay(200);
    }
    await using(var db=fixture.CreateContext())
    {
        var workerId=employeeIds[HrmRoles.Employee];
        var balance=await db.LeaveBalances.SingleAsync(b=>b.EmployeeId==workerId&&b.Year==2026);
        var leaves=await db.LeaveRecords.OrderBy(l=>l.StartDate).Select(l=>new{l.EmployeeId,l.StartDate,l.EndDate,l.Status,l.Reason}).ToListAsync();
        var audit=await db.AuditEvents.Where(a=>a.Action.StartsWith("leaves.")||a.Action.StartsWith("attendance.")).Select(a=>new{a.Action,a.EntityId,a.Reason}).ToListAsync();
        var debitCount=await db.LeaveLedgerEntries.CountAsync(l=>l.EmployeeId==workerId&&l.Action=="Debit");
        var attendance=await db.AttendanceSummaries.Where(a=>a.EmployeeId==workerId).Select(a=>new{a.BusinessDate,a.Phase,a.CheckInUtc,a.CheckOutUtc,a.EligibleMinutes}).ToListAsync();
        var ot=await db.AttendanceOvertimeEntries.SumAsync(a=>(decimal?)a.Minutes)??0m;
        File.WriteAllText(Path.Combine(output,"browser-sql-state.json"),JsonSerializer.Serialize(new{balance=new{balance.AllocatedDays,balance.UsedDays,balance.RemainingDays},debitCount,leaves,audit,attendance,overtimeMinutes=ot},new JsonSerializerOptions{WriteIndented=true}));
    }
}
finally
{
    await fixture.DisposeAsync();
    var resolved=Path.GetFullPath(temp);var tempRoot=Path.GetFullPath(Path.GetTempPath());
    if(!resolved.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(resolved).StartsWith("HRM-T18-browser-",StringComparison.Ordinal))
        throw new InvalidOperationException("Temporary cleanup target outside owned path.");
    Directory.Delete(resolved,true);
    Console.WriteLine("STOPPED: owned SQL database and temporary certificate/fixture credentials cleaned.");
}


sealed class T18FaultAudit(AuditRepository inner,HrmDbContext db,string observationFile) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry,CancellationToken token)
    {
        await inner.RecordAsync(entry,token);
        if(entry.Action=="leaves.approved"&&entry.Reason=="T18_TEST_ROLLBACK")
        {
            var id=Guid.Parse(entry.EntityId);
            var row=await db.LeaveRecords.AsNoTracking().SingleAsync(l=>l.Id==id,token);
            var balance=await db.LeaveBalances.AsNoTracking().SingleAsync(b=>b.EmployeeId==row.EmployeeId&&b.Year==row.StartDate.Year,token);
            File.WriteAllText(observationFile,JsonSerializer.Serialize(new{phase="after-real-sql-flush-before-rollback",row.Status,balance.UsedDays}));
            throw new InvalidOperationException("Test-only fault after real SQL audit flush");
        }
    }
    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters query,CancellationToken token)=>inner.ListAsync(query,token);
}
