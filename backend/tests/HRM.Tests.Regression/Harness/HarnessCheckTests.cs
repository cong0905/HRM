using HRM.BLL.Interfaces;
using HRM.BLL.Services;
using HRM.Common.DTOs;
using HRM.Common.Helpers;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HRM.Tests.Regression.Harness;

[Trait("Category", "HarnessCheck")]
public class HarnessCheckTests
{
    [Fact]
    public async Task HarnessCheck_InMemoryDbContext_CanPersistAndRetrieveData()
    {
        var options = new DbContextOptionsBuilder<HrmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new HrmDbContext(options);

        var nv = new NhanVien
        {
            MaNhanVien = 1,
            HoTen = "Nguyen Van A",
            Email = "nva@example.com",
            MucLuong = 10_000_000m,
            NgayVaoLam = new DateTime(2025, 1, 1),
            TrangThai = "DangLamViec"
        };

        db.NhanVien.Add(nv);
        await db.SaveChangesAsync();

        var retrieved = await db.NhanVien.FirstOrDefaultAsync(x => x.MaNhanVien == 1);
        Assert.NotNull(retrieved);
        Assert.Equal("Nguyen Van A", retrieved.HoTen);
    }

    [Fact]
    public void HarnessCheck_HieuSuatService_WithKnownScores_ReturnsExpectedBonus()
    {
        // Kiểm tra các ngưỡng điểm bình thường hoạt động đúng logic switch-case
        var bonusHigh = HieuSuatService.TinhBonusHieuSuat(96m);
        var bonusMid = HieuSuatService.TinhBonusHieuSuat(86m);
        var bonusNeutral = HieuSuatService.TinhBonusHieuSuat(55m);

        Assert.Equal(0.25m, bonusHigh);
        Assert.Equal(0.15m, bonusMid);
        Assert.Equal(0.00m, bonusNeutral);
    }

    [Fact]
    public async Task HarnessCheck_AuthService_LoginAsync_WhenUserNotFound_ReturnsNull()
    {
        // Tình huống 1: Tên đăng nhập không tồn tại trong hệ thống
        var taiKhoanRepoMock = new Mock<ITaiKhoanRepository>();
        var tokenRepoMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();

        taiKhoanRepoMock.Setup(r => r.GetByUsernameAsync("non_existent_user"))
            .ReturnsAsync((TaiKhoan?)null);

        var authService = new AuthService(taiKhoanRepoMock.Object, tokenRepoMock.Object, emailSenderMock.Object);

        var result = await authService.LoginAsync(new LoginDTO
        {
            TenDangNhap = "non_existent_user",
            MatKhau = "AnyPassword123!@"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task HarnessCheck_AuthService_LoginAsync_WhenPasswordIncorrect_ReturnsNull()
    {
        // Tình huống 2: Người dùng tồn tại nhưng nhập sai mật khẩu (hash BCrypt thật)
        var taiKhoanRepoMock = new Mock<ITaiKhoanRepository>();
        var tokenRepoMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();

        const string correctPassword = "CorrectPassword123!@";
        const string wrongPassword = "WrongPassword456!@";
        var realBcryptHash = PasswordHelper.HashPassword(correctPassword);

        var existingUser = new TaiKhoan
        {
            MaTaiKhoan = 10,
            MaNhanVien = 100,
            TenDangNhap = "existing_user",
            MatKhauHash = realBcryptHash,
            NhanVien = new NhanVien
            {
                MaNhanVien = 100,
                HoTen = "Tran Thi Test",
                Email = "test@company.local"
            }
        };

        taiKhoanRepoMock.Setup(r => r.GetByUsernameAsync("existing_user"))
            .ReturnsAsync(existingUser);

        var authService = new AuthService(taiKhoanRepoMock.Object, tokenRepoMock.Object, emailSenderMock.Object);

        var result = await authService.LoginAsync(new LoginDTO
        {
            TenDangNhap = "existing_user",
            MatKhau = wrongPassword
        });

        // Đăng nhập thất bại, trả về null và không cập nhật ngày đăng nhập
        Assert.Null(result);
        taiKhoanRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TaiKhoan>()), Times.Never);
    }
}
