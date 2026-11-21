using System.Net;
using System.Text.Json;
using HRM.Next.Application.Authorization;
using Xunit;

namespace HRM.Next.Tests.Integration;
public sealed partial class FoundationSqlTests
{
    [Theory]
    [InlineData("Employee")]
    [InlineData("HR")]
    [InlineData("Payroll")]
    [InlineData("SystemAdmin")]
    public async Task Access_ServerPermissionsMatchAuthenticatedRole_WithoutPrivateData(string role)
    {
        await sql.ResetAsync(); await using var harness=new AuthHarness(sql.ConnectionString);
        var user=await CreateUserWithRolesAsync(harness,"t16-access",role);
        using var client=await BearerClientAsync(harness,user);
        using var response=await client.GetAsync("/api/v1/access/me");Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var text=await response.Content.ReadAsStringAsync();using var document=JsonDocument.Parse(text);var body=document.RootElement;
        Assert.Equal(user.Id,body.GetProperty("id").GetGuid());Assert.Equal(user.UserName,body.GetProperty("username").GetString());
        Assert.Contains(role,body.GetProperty("roles").EnumerateArray().Select(x=>x.GetString()));
        var expected=HrmPermissions.GetPermissionsForRoles([role]).Order().ToArray();var actual=body.GetProperty("permissions").EnumerateArray().Select(x=>x.GetString()).Order().ToArray();Assert.Equal(expected,actual);
        Assert.Equal(new[]{"id","permissions","roles","username"},body.EnumerateObject().Select(x=>x.Name).Order().ToArray());
        Assert.DoesNotContain("password",text,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("citizen",text,StringComparison.OrdinalIgnoreCase);
        if(role==HrmRoles.SystemAdmin)Assert.DoesNotContain(HrmPermissions.PayrollExport,actual);
    }
    [Fact]
    public async Task Access_AnonymousCannotGetPermissions()
    {
        await sql.ResetAsync();await using var harness=new AuthHarness(sql.ConnectionString);using var client=harness.Client();
        using var response=await client.GetAsync("/api/v1/access/me");await ProblemAsync(response,HttpStatusCode.Unauthorized);
    }
}
