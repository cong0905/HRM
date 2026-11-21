using HRM.BLL.Interfaces;
using HRM.BLL.Services;
using HRM.Common.Helpers;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Moq;
using Xunit;

namespace HRM.Tests.Regression.Bugs;

[Trait("Category", "RegressionBug")]
public class F06_TokenPurposeTests
{
    /// <summary>
    /// F06: ResetPasswordWithTokenAsync không kiểm tra Purpose của token.
    /// Nếu một token được cấp cho mục đích "VerifyEmail" (hoặc mục đích khác không phải ResetPassword),
    /// hệ thống vẫn chấp nhận cho phép reset mật khẩu tài khoản.
    ///
    /// Nghiệp vụ đúng (T00 Sign-off):
    /// - Token reset mật khẩu phải có Purpose == "ResetPassword".
    /// - Token có Purpose == "VerifyEmail" hoặc mục đích khác BẮT BUỘC phải bị từ chối (trả về false).
    /// - Mật khẩu hash của tài khoản KHÔNG ĐƯỢC PHÉP thay đổi.
    /// - Không được thực hiện bất kỳ lệnh ghi cập nhật nào lên repository của Tài khoản và Token.
    ///
    /// Hành vi lỗi ở baseline:
    /// - AuthService.ResetPasswordWithTokenAsync bỏ qua kiểm tra token.Purpose.
    /// - Trả về true, ghi đè MatKhauHash bằng mật khẩu mới, gọi UpdateAsync lên cả TaiKhoan và Token -> BUG REPRODUCED.
    ///
    /// Lưu ý kiểm thử: Test gọi trực tiếp AuthService thật của mã nguồn legacy; không sao chép logic sang test.
    /// Test này CHỨNG MINH LỖI CŨ, KỲ VỌNG THẤT BẠI TRÊN BASELINE.
    /// </summary>
    [Fact]
    public async Task ResetPasswordWithToken_WhenTokenPurposeIsVerifyEmail_MustBeRejectedAndNotMutatePassword()
    {
        // 1. Arrange: Fixture đầu vào
        var taiKhoanRepoMock = new Mock<ITaiKhoanRepository>();
        var tokenRepoMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();

        const string oldPassword = "OldPassword123!@";
        var initialHash = PasswordHelper.HashPassword(oldPassword);

        var testUser = new TaiKhoan
        {
            MaTaiKhoan = 1,
            MaNhanVien = 101,
            TenDangNhap = "nhanvien01",
            MatKhauHash = initialHash
        };

        // Token này được cấp với mục đích VerifyEmail (không phải ResetPassword)
        var verifyEmailToken = new PasswordResetToken
        {
            Id = 1,
            MaTaiKhoan = 1,
            Email = "nhanvien01@company.local",
            Token = "verify-email-token-uuid-12345",
            Purpose = "VerifyEmail",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };

        tokenRepoMock.Setup(r => r.GetByTokenAsync("verify-email-token-uuid-12345"))
            .ReturnsAsync(verifyEmailToken);

        taiKhoanRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(testUser);

        // Khởi tạo AuthService thật của legacy
        var authService = new AuthService(taiKhoanRepoMock.Object, tokenRepoMock.Object, emailSenderMock.Object);

        // 2. Act: Gọi phương thức ResetPasswordWithTokenAsync của service thật
        var newValidPassword = "NewStrongPassword123!@";
        var resetResult = await authService.ResetPasswordWithTokenAsync(verifyEmailToken.Token, newValidPassword);

        // 3. Assert:
        // A. Thao tác phải bị từ chối
        // Trên baseline, hàm trả về true -> Assert này sẽ FAIL trên baseline
        Assert.False(resetResult, "LỖ HỔNG F06: Hệ thống chấp nhận token VerifyEmail để reset mật khẩu! Kỳ vọng phải trả về false.");

        // B. Hash mật khẩu cũ không được phép bị biến động
        Assert.Equal(initialHash, testUser.MatKhauHash);

        // C. Không được gọi repository ghi đè tài khoản
        taiKhoanRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TaiKhoan>()), Times.Never,
            "LỖ HỔNG F06: Thao tác ghi repository TaiKhoanRepo.UpdateAsync đã bị gọi trái phép!");

        // D. Không được cập nhật trạng thái token
        tokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PasswordResetToken>()), Times.Never,
            "LỖ HỔNG F06: Thao tác ghi repository TokenRepo.UpdateAsync đã bị gọi trái phép!");
    }
}
