using System.Xml.Linq;
using HRM.Next.Api.Controllers;
using HRM.Next.Domain.Departments;
using HRM.Next.Application.Common;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class StartupAndArchitectureTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Server=.;Database=HRM_System;Integrated Security=True")]
    [InlineData("Server=.;Database=master;Integrated Security=True")]
    [InlineData("NotAKeyword=DO_NOT_EXPOSE")]
    public void MissingOrUnsafeDatabaseConfiguration_FailsStartupWithoutEchoingInput(string connection)
    {
        using var factory = new ApiFactory(connection, testIdentity: false, probes: false);
        var error = Assert.ThrowsAny<Exception>(() => { using var client = factory.CreateClient(); });
        Assert.DoesNotContain("DO_NOT_EXPOSE", error.ToString());
    }

    [Fact]
    public void ProjectDependencyBoundaries_AreEnforced()
    {
        var expected = new Dictionary<string, string[]>
        {
            ["HRM.Domain"] = [],
            ["HRM.Application"] = ["HRM.Domain"],
            ["HRM.Infrastructure"] = ["HRM.Application", "HRM.Domain"],
            ["HRM.Api"] = ["HRM.Application", "HRM.Infrastructure"]
        };
        foreach (var (name, dependencies) in expected)
        {
            var project = XDocument.Load(Path.Combine(ApiFactory.RepoRoot, "backend/src", name, name + ".csproj"));
            var actual = project.Descendants("ProjectReference").Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value)).Order().ToArray();
            Assert.Equal(dependencies.Order(), actual);
            if (name is "HRM.Domain" or "HRM.Application") Assert.Empty(project.Descendants("PackageReference"));
        }
        Assert.DoesNotContain(typeof(Department).Assembly.GetReferencedAssemblies(), name => name.Name?.StartsWith("Microsoft.EntityFrameworkCore") == true);
        Assert.DoesNotContain(typeof(IUnitOfWork).Assembly.GetReferencedAssemblies(), name => name.Name?.StartsWith("HRM.Next.Infrastructure") == true);
        Assert.All(typeof(DepartmentsController).GetConstructors().SelectMany(x => x.GetParameters()), parameter =>
            Assert.False(parameter.ParameterType.Namespace?.StartsWith("HRM.Next.Infrastructure") == true));
    }
}
