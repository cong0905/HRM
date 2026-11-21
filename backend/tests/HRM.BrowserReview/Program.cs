using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HRM.Next.Application.Authorization;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Tests.Integration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Test harness only. Real API/auth, Kestrel HTTPS, owned SQL GUID database.
var root = ApiFactory.RepoRoot;
var output = Path.Combine(root,"docs/verification/t16-review"); Directory.CreateDirectory(output);
var temp = Directory.CreateTempSubdirectory("HRM-T16-browser-").FullName;
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
        foreach(var role in new[]{HrmRoles.Employee,HrmRoles.Payroll,HrmRoles.SystemAdmin})
        {
            if(!await roles.RoleExistsAsync(role))
            {var created=await roles.CreateAsync(new(){Id=Guid.NewGuid(),Name=role});if(!created.Succeeded)throw new InvalidOperationException("Role seed failed");}
            var username="t16-"+role.ToLowerInvariant()+"-"+suffix;
            var user=new HrmUser{Id=Guid.NewGuid(),UserName=username,Email=username+"@example.invalid",IsActive=true};
            var result=await users.CreateAsync(user,password);if(!result.Succeeded)throw new InvalidOperationException("User seed failed");
            result=await users.AddToRoleAsync(user,role);if(!result.Succeeded)throw new InvalidOperationException("Role assignment seed failed");actors[role]=username;
        }
    }
    var secretState=Path.Combine(temp,"browser-fixture.json");
    File.WriteAllText(secretState,JsonSerializer.Serialize(new{apiOrigin=$"https://localhost:{port}",certPath,keyPath,actors,password}));
    var ready=new{status="READY",apiOrigin=$"https://localhost:{port}",fixtureFile=secretState,databaseName=fixture.DatabaseName,stopFile=Path.Combine(temp,"stop"),expireFile=Path.Combine(temp,"expire")};
    File.WriteAllText(Path.Combine(output,"browser-host.json"),JsonSerializer.Serialize(ready,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("READY: real Kestrel HTTPS and owned SQL fixture; credentials stored only in temporary fixture file.");
    var deadline=DateTimeOffset.UtcNow.AddMinutes(25);var expired=false;
    while(DateTimeOffset.UtcNow<deadline && !File.Exists(ready.stopFile))
    {
        if(!expired && File.Exists(ready.expireFile)){harness.Clock.Advance(TimeSpan.FromDays(8));expired=true;Console.WriteLine("Test clock advanced; existing sessions expired.");}
        await Task.Delay(500);
    }
}
finally
{
    await fixture.DisposeAsync();
    var resolved=Path.GetFullPath(temp);var tempRoot=Path.GetFullPath(Path.GetTempPath());
    if(!resolved.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(resolved).StartsWith("HRM-T16-browser-",StringComparison.Ordinal))
        throw new InvalidOperationException("Temporary cleanup target outside owned path.");
    Directory.Delete(resolved,true);
    Console.WriteLine("STOPPED: owned SQL database and temporary certificate/fixture credentials cleaned.");
}
