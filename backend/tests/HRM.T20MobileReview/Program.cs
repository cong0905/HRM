using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HRM.Next.Application.Authorization;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Tests.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

// Test-only: owned SQL GUID fixture; one-off CA trusted only inside owned emulator.
var output = Path.Combine(ApiFactory.RepoRoot, "docs/verification/t20");
Directory.CreateDirectory(output);
var temp = Directory.CreateTempSubdirectory("HRM-T20-mobile-").FullName;
var fixture = new SqlDatabaseFixture();
try
{
    Environment.SetEnvironmentVariable("HRM_T05_SQL_LEDGER", Path.Combine(output, "sql-mobile-ledger.json"));
    await fixture.InitializeAsync();
    using var caKey = RSA.Create(2048);
    var caRequest = new CertificateRequest("CN=HRM T20 Disposable Fixture CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
    using var ca = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
    using var serverKey = RSA.Create(2048);
    var serverRequest = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var san = new SubjectAlternativeNameBuilder();
    san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback); san.AddIpAddress(IPAddress.Parse("10.0.2.2"));
    serverRequest.CertificateExtensions.Add(san.Build());
    serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
    serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
    using var signed = serverRequest.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-4), DateTimeOffset.UtcNow.AddHours(23), RandomNumberGenerator.GetBytes(16));
    using var leaf = signed.CopyWithPrivateKey(serverKey);
    var pfxPassword = Guid.NewGuid().ToString("N");
    using var certificate = X509CertificateLoader.LoadPkcs12(leaf.Export(X509ContentType.Pfx, pfxPassword), pfxPassword, X509KeyStorageFlags.Exportable);
    var caHash = BitConverter.ToUInt32(MD5.HashData(ca.SubjectName.RawData), 0).ToString("x8");
    var caPath = Path.Combine(temp, caHash + ".0");
    File.WriteAllText(caPath, ca.ExportCertificatePem());
    var requestedPort = int.TryParse(Environment.GetEnvironmentVariable("HRM_T20_API_PORT"), out var configuredPort) ? configuredPort : 0;
    using var listener = new TcpListener(IPAddress.Loopback, requestedPort);
    listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
    await using var harness = new AuthHarness(fixture.ConnectionString);
    harness.Clock.Advance(DateTimeOffset.UtcNow.AddMinutes(1) - harness.Clock.GetUtcNow());
    harness.Factory.UseKestrel(options => options.ListenLocalhost(port, listen => listen.UseHttps(certificate)));
    harness.Factory.StartServer();
    var password = "T20!" + Guid.NewGuid().ToString("N") + "aA";
    var actors = new Dictionary<string, string>();
    await using (var scope = harness.Factory.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        foreach (var actor in new[] { "Employee", "EmployeeOther" })
        {
            var name = "t20-" + actor.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N")[..6];
            var user = new HrmUser { Id = Guid.NewGuid(), UserName = name, Email = name + "@example.invalid", IsActive = true };
            if (!(await users.CreateAsync(user, password)).Succeeded || !(await users.AddToRoleAsync(user, HrmRoles.Employee)).Succeeded)
                throw new InvalidOperationException("Owned actor setup failed");
            actors[actor] = name;
        }
    }
    var secret = Path.Combine(temp, "mobile-fixture.json");
    File.WriteAllText(secret, JsonSerializer.Serialize(new { apiOrigin = $"https://localhost:{port}", port, caPath, caHash, actors, password }));
    var stopFile = Path.Combine(temp, "stop");
    File.WriteAllText(Path.Combine(output, "mobile-host.json"), JsonSerializer.Serialize(new { status = "READY", fixtureFile = secret, stopFile, databaseName = fixture.DatabaseName }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("READY: T20 real HTTPS API + isolated SQL; fixture credentials not logged.");
    var deadline = DateTimeOffset.UtcNow.AddMinutes(120);
    while (DateTimeOffset.UtcNow < deadline && !File.Exists(stopFile)) await Task.Delay(300);
    await using var db = fixture.CreateContext();
    var sessions = await db.AccountSessions.Select(s => new { s.Revoked }).ToListAsync();
    File.WriteAllText(Path.Combine(output, "mobile-session-summary.json"), JsonSerializer.Serialize(new { totalSessions = sessions.Count, revoked = sessions.Count(s => s.Revoked) }));
}
finally
{
    await fixture.DisposeAsync();
    var resolved = Path.GetFullPath(temp);
    if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("HRM-T20-mobile-", StringComparison.Ordinal))
        throw new InvalidOperationException("Cleanup outside owned temp");
    Directory.Delete(resolved, true);
    Console.WriteLine("STOPPED: T20 owned SQL database and temporary credentials cleaned.");
}
