using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRM.Next.Infrastructure.Persistence;

public sealed class HrmDbContextFactory : IDesignTimeDbContextFactory<HrmDbContext>
{
    public HrmDbContext CreateDbContext(string[] args)
    {
        var settings = new DatabaseOptions
        {
            ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Hrm") ?? string.Empty
        };
        var validation = new DatabaseOptionsValidator().Validate(null, settings);
        if (validation.Failed)
            throw new InvalidOperationException(string.Join(" ", validation.Failures));
        return new HrmDbContext(new DbContextOptionsBuilder<HrmDbContext>()
            .UseSqlServer(settings.ConnectionString, sql => sql.UseCompatibilityLevel(160)).Options);
    }
}
