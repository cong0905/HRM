using System.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using HRM.DAL.Context;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace HRM.Tests.Regression.Runtime;

[Trait("Category", "RuntimeCompatibility")]
public class RuntimeCompatibilityTests
{
    // Deliberately invalid SQL endpoint: these checks must work without a database.
    private static HrmDbContext CreateContext() => new(new DbContextOptionsBuilder<HrmDbContext>()
        .UseSqlServer("Server=invalid.example;Database=T04OfflineOnly;Integrated Security=True;Connect Timeout=1")
        .Options);

    [Fact]
    public void ProcessAndLoadedDependencies_UseDotnet10AndEf10()
    {
        Assert.StartsWith(".NET 10.", RuntimeInformation.FrameworkDescription);
        Assert.Equal(10, typeof(DbContext).Assembly.GetName().Version!.Major);
        Assert.Equal(10, typeof(SqlServerDbContextOptionsExtensions).Assembly.GetName().Version!.Major);
        Assert.Equal(10, typeof(JsonSerializer).Assembly.GetName().Version!.Major);
    }

    [Fact]
    public void Ef10Model_PreservesT02AccountAndResetTokenConstraints()
    {
        using var db = CreateContext();
        var account = db.Model.FindEntityType(typeof(TaiKhoan))!;
        Assert.Contains(account.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(TaiKhoan.TenDangNhap) }));
        var token = db.Model.FindEntityType(typeof(PasswordResetToken))!;
        Assert.Null(token.FindProperty("TaiKhoanMaTaiKhoan"));
        var fk = Assert.Single(token.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(TaiKhoan));
        Assert.Equal(new[] { nameof(PasswordResetToken.MaTaiKhoan) }, fk.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Fact]
    public void Ef10SqlServer_GeneratesExistingMigrationChainWithoutConnecting()
    {
        using var db = CreateContext();
        Assert.Contains(db.Database.GetMigrations(), name => name.EndsWith("_FixLegacySchemaAndConstraints"));
        var script = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("ALTER TABLE [ChamCong] ADD [Hwid]", script);
        Assert.Contains("CREATE UNIQUE INDEX [IX_TaiKhoan_TenDangNhap]", script);
        Assert.Contains("FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan]", script);
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}
