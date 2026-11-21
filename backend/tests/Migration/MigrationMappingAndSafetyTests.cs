using HRM.Ops.Migration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationMappingAndSafetyTests(MigrationDualDatabaseFixture fixture) : IClassFixture<MigrationDualDatabaseFixture>
{
    private static readonly string FixturesDir = MigrationDualDatabaseFixture.FixturesDir;

    private async Task SetupCleanLegacyAndMigrateAsync()
    {
        await fixture.ResetNextDatabaseAsync();

        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var cleanSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_clean_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(cleanSql);

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();
        var engine = new MigrationEngine(reader, nextDb);
        await engine.ExecuteMigrationAsync();
    }

    [Fact]
    public async Task AccountHashHandling_ImportedAccounts_HaveNullHash_Inactive_AndNoDefaultPassword()
    {
        await SetupCleanLegacyAndMigrateAsync();

        await using var db = fixture.CreateNextContext();
        var users = await db.Users.ToListAsync();

        Assert.NotEmpty(users);

        foreach (var user in users)
        {
            // CRITICAL REQUIREMENT:
            // 1. PasswordHash MUST be null (no BCrypt migrated into ASP.NET Identity, no plaintext)
            Assert.Null(user.PasswordHash);

            // 2. Imported account must be inactive pending activation
            Assert.False(user.IsActive, $"User {user.UserName} should be inactive upon migration.");

            // 3. Activation outbox message must exist
            var outbox = await db.ActivationOutboxMessages.FirstOrDefaultAsync(o => o.UserId == user.Id);
            Assert.NotNull(outbox);
            Assert.Equal("activation", outbox.Purpose);
            Assert.Equal("Pending", outbox.Status);
        }
    }

    [Fact]
    public async Task RoleMapping_LegacyRoles_AreMappedToNextRoles()
    {
        await SetupCleanLegacyAndMigrateAsync();

        await using var db = fixture.CreateNextContext();

        // Check admin user
        var adminUser = await db.Users.SingleAsync(u => u.UserName == "admin_legacy");
        var adminRoles = await (from ur in db.UserRoles
                                join r in db.Roles on ur.RoleId equals r.Id
                                where ur.UserId == adminUser.Id
                                select r.Name).ToListAsync();
        Assert.Contains("SystemAdmin", adminRoles);

        // Check HR user
        var hrUser = await db.Users.SingleAsync(u => u.UserName == "hr_legacy");
        var hrRoles = await (from ur in db.UserRoles
                             join r in db.Roles on ur.RoleId equals r.Id
                             where ur.UserId == hrUser.Id
                             select r.Name).ToListAsync();
        Assert.Contains("HR", hrRoles);

        // Check staff user
        var staffUser = await db.Users.SingleAsync(u => u.UserName == "tech_legacy");
        var staffRoles = await (from ur in db.UserRoles
                                join r in db.Roles on ur.RoleId equals r.Id
                                where ur.UserId == staffUser.Id
                                select r.Name).ToListAsync();
        Assert.Contains("Employee", staffRoles);
    }

    [Fact]
    public async Task TimezoneConversion_LocalVietnamToUtc_IsPreservedAccurately()
    {
        await SetupCleanLegacyAndMigrateAsync();

        await using var db = fixture.CreateNextContext();
        var summaries = await db.AttendanceSummaries.ToListAsync();

        Assert.NotEmpty(summaries);

        // In legacy_clean_seed.sql:
        // Record 1: 2026-04-01 GioVao = '08:30:00', GioRa = '17:30:00' in Vietnam (+07:00)
        // Expected UTC: 2026-04-01 01:30:00 UTC and 2026-04-01 10:30:00 UTC
        var att1 = summaries.First(s => s.BusinessDate == new DateOnly(2026, 4, 1) && s.LateMinutes == 0m);

        Assert.Equal(TimeSpan.Zero, att1.CheckInUtc.Offset);
        Assert.Equal(1, att1.CheckInUtc.Hour);
        Assert.Equal(30, att1.CheckInUtc.Minute);

        Assert.NotNull(att1.CheckOutUtc);
        Assert.Equal(TimeSpan.Zero, att1.CheckOutUtc.Value.Offset);
        Assert.Equal(10, att1.CheckOutUtc.Value.Hour);
        Assert.Equal(30, att1.CheckOutUtc.Value.Minute);
    }

    [Fact]
    public async Task StatusMapping_EmployeeAndLeaveStatus_MapCorrectly()
    {
        await SetupCleanLegacyAndMigrateAsync();

        await using var db = fixture.CreateNextContext();

        // Employee 4 in legacy was 'Nghỉ việc' -> DaNghiViec
        var emp4 = await db.EmployeeProfiles.SingleAsync(e => e.Email == "resigned@example.com");
        Assert.Equal("DaNghiViec", emp4.Status);
        Assert.NotNull(emp4.TerminationDate);

        // Employee 1 was 'Đang làm việc' -> DangLamViec
        var emp1 = await db.EmployeeProfiles.SingleAsync(e => e.Email == "director@example.com");
        Assert.Equal("DangLamViec", emp1.Status);
        Assert.Null(emp1.TerminationDate);

        // Leave request 1 was 'Đã duyệt' -> Approved
        var leave1 = await db.LeaveRecords.SingleAsync(l => l.WorkingDays == 1.0m && l.StartDate == new DateOnly(2026, 4, 10));
        Assert.Equal("Approved", leave1.Status);
        Assert.NotNull(leave1.ApproverId);
        Assert.NotNull(leave1.DecidedAtUtc);
    }
}
