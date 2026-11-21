using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Tests.Regression.Characterization;

// Characterize both paths: NoTracking alone does not make every update fail.
// InMemory verifies EF tracking behavior, not SQL transactions or concurrency.
[Trait("Category", "Characterization")]
[Trait("Finding", "F11")]
public class F11_TrackingCharacterizationTests
{
    private static HrmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new HrmDbContext(options);
    }

    private static async Task SeedEmployee(HrmDbContext db)
    {
        db.NhanVien.Add(new NhanVien
        {
            MaNhanVien = 100,
            HoTen = "Nhan vien kiem thu",
            DiaChi = "Dia chi ban dau",
            NgaySinh = new DateTime(1999, 1, 1),
            NgayVaoLam = new DateTime(2026, 1, 1),
            MucLuong = 10_000_000m,
            TrangThai = "Đang làm việc"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task DetachedQuery_WhenOnlySaveChangesCalled_DoesNotPersistMutation()
    {
        using var db = CreateContext();
        await SeedEmployee(db);
        var employee = await db.NhanVien.SingleAsync(x => x.MaNhanVien == 100);
        Assert.Equal(EntityState.Detached, db.Entry(employee).State);

        employee.DiaChi = "Dia chi da sua";
        var written = await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.NhanVien.SingleAsync(x => x.MaNhanVien == 100);
        Assert.Equal(0, written);
        Assert.Equal("Dia chi ban dau", persisted.DiaChi);
    }

    [Fact]
    public async Task DetachedQuery_WhenRepositoryUpdateCalled_PersistsMutation()
    {
        using var db = CreateContext();
        await SeedEmployee(db);
        var employee = await db.NhanVien.SingleAsync(x => x.MaNhanVien == 100);
        Assert.Equal(EntityState.Detached, db.Entry(employee).State);

        employee.DiaChi = "Dia chi da sua";
        var repository = new NhanVienRepository(db);
        await repository.UpdateAsync(employee);
        db.ChangeTracker.Clear();

        var persisted = await db.NhanVien.SingleAsync(x => x.MaNhanVien == 100);
        Assert.Equal("Dia chi da sua", persisted.DiaChi);
    }
}
