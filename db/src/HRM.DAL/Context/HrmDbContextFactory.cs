using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRM.DAL.Context;

public class HrmDbContextFactory : IDesignTimeDbContextFactory<HrmDbContext>
{
    public HrmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HrmDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=HRM_System;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new HrmDbContext(optionsBuilder.Options);
    }
}
