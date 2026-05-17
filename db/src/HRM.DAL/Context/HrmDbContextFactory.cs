using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HRM.DAL.Context;

public class HrmDbContextFactory : IDesignTimeDbContextFactory<HrmDbContext>
{
    private const string FallbackConnection =
        "Server=.;Database=HRM_System;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public HrmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HrmDbContext>();
        optionsBuilder.UseSqlServer(ResolveConnectionString());
        return new HrmDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var cwd = Directory.GetCurrentDirectory();
        var searchRoots = new[]
        {
            cwd,
            Path.GetFullPath(Path.Combine(cwd, "..", "HRM.GUI")),
            Path.GetFullPath(Path.Combine(cwd, "HRM.GUI")),
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in searchRoots)
        {
            var path = Path.Combine(root, "appsettings.json");
            if (!File.Exists(path))
                continue;

            var config = new ConfigurationBuilder()
                .SetBasePath(root)
                .AddJsonFile("appsettings.json")
                .Build();
            var cs = config.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;
        }

        return FallbackConnection;
    }
}
