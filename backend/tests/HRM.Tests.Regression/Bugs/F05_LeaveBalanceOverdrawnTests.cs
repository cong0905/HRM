using HRM.BLL.Services;
using HRM.Common.Constants;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Moq;
using Xunit;

namespace HRM.Tests.Regression.Bugs;

[Trait("Category", "RegressionBug")]
public class F05_LeaveBalanceOverdrawnTests
{
    /// <summary>
    /// F05.1: Kiểm tra ngoại lệ khi duyệt đơn nghỉ phép vượt quá số ngày phép còn lại.
    ///
    /// Nghiệp vụ đúng (T00 Sign-off / OPEN-05):
    /// - Khi nhân viên chỉ còn 1 ngày phép nhưng đơn xin nghỉ 3 ngày phép năm,
    ///   hàm ApproveAsync BẮT BUỘC phải ném InvalidOperationException và từ chối thao tác.
    ///
    /// Hành vi lỗi ở baseline:
    /// - DonNghiPhepService.ApproveAsync không kiểm tra điều kiện so.SoNgayConLai >= them trước khi duyệt.
    /// - Không có ngoại lệ nào được ném ra -> BUG REPRODUCED.
    /// </summary>
    [Fact]
    public async Task ApproveLeave_WhenDaysRequestedExceedsRemainingBalance_MustThrowInvalidOperationException()
    {
        // 1. Arrange: Quỹ phép còn 1 ngày (cấp 12, đã dùng 11), đơn xin nghỉ 3 ngày
        var donNghiPhepRepoMock = new Mock<IDonNghiPhepRepository>();
        var soNgayPhepRepoMock = new Mock<ISoNgayPhepRepository>();
        var loaiNghiPhepRepoMock = new Mock<IRepository<LoaiNghiPhep>>();
        var chamCongRepoMock = new Mock<IChamCongRepository>();

        const int maNhanVien = 100;
        const int maDonPhep = 55;
        const int nguoiDuyet = 1;
        const int nam = 2026;

        var soNgayPhep = new SoNgayPhep
        {
            MaNhanVien = maNhanVien,
            Nam = nam,
            TongSoNgayPhep = 12,
            SoNgayDaSuDung = 11 // Còn lại: 1 ngày
        };

        var donNghiPhep = new DonNghiPhep
        {
            MaDonPhep = maDonPhep,
            MaNhanVien = maNhanVien,
            MaLoaiPhep = 1, // MaLoaiPhepNam
            NgayBatDau = new DateTime(2026, 6, 1),
            NgayKetThuc = new DateTime(2026, 6, 3),
            SoNgayNghi = 3m,
            LyDo = "Việc cá nhân",
            TrangThai = DonNghiPhepTrangThai.ChoDuyet
        };

        donNghiPhepRepoMock.Setup(r => r.GetByIdAsync(maDonPhep)).ReturnsAsync(donNghiPhep);
        chamCongRepoMock.Setup(r => r.ExistsAnyAsync(maNhanVien, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);
        soNgayPhepRepoMock.Setup(r => r.EnsureAsync(maNhanVien, nam)).ReturnsAsync(soNgayPhep);

        var service = new DonNghiPhepService(
            donNghiPhepRepoMock.Object,
            soNgayPhepRepoMock.Object,
            loaiNghiPhepRepoMock.Object,
            chamCongRepoMock.Object);

        // 2 & 3. Act & Assert: Phải ném ngoại lệ InvalidOperationException
        // Trên baseline: Không ném ngoại lệ -> Test thất bại như kỳ vọng của baseline bug
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(maDonPhep, nguoiDuyet));
    }

    /// <summary>
    /// F05.2: Kiểm tra hậu quả về trạng thái thực thể và lệnh ghi repository khi duyệt quá hạn mức.
    ///
    /// Nghiệp vụ đúng (T00 Sign-off / OPEN-05):
    /// - Khi không đủ số ngày phép, đơn KHÔNG được chuyển sang trạng thái "Đã duyệt".
    /// - Quỹ phép KHÔNG được phép trừ (số ngày đã dùng giữ nguyên 11, không thành 14 làm âm quỹ thành -2).
    /// - Không được thực hiện lệnh ghi repository cập nhật đơn thành Đã duyệt hoặc trừ quỹ phép.
    ///
    /// Hành vi lỗi ở baseline:
    /// - Baseline âm thầm chuyển don.TrangThai = "Đã duyệt", gọi repository UpdateAsync(don).
    /// - Đồng thời cộng so.SoNgayDaSuDung += 3 (thành 14, quỹ còn lại -2) và gọi UpdateAsync(so) -> BUG REPRODUCED.
    ///
    /// Lưu ý kiểm thử: Test này kiểm tra logic nghiệp vụ tầng dịch vụ và lệnh gọi repository thông qua mock.
    /// Test không tuyên bố fake repository đã chứng minh hành vi transaction rollback hay concurrency ở tầng SQL Server.
    /// </summary>
    [Fact]
    public async Task ApproveLeave_WhenDaysRequestedExceedsRemainingBalance_MustNotMutateStatusAndMustNotDeductBalance()
    {
        // 1. Arrange
        var donNghiPhepRepoMock = new Mock<IDonNghiPhepRepository>();
        var soNgayPhepRepoMock = new Mock<ISoNgayPhepRepository>();
        var loaiNghiPhepRepoMock = new Mock<IRepository<LoaiNghiPhep>>();
        var chamCongRepoMock = new Mock<IChamCongRepository>();

        const int maNhanVien = 100;
        const int maDonPhep = 55;
        const int nguoiDuyet = 1;
        const int nam = 2026;

        var soNgayPhep = new SoNgayPhep
        {
            MaNhanVien = maNhanVien,
            Nam = nam,
            TongSoNgayPhep = 12,
            SoNgayDaSuDung = 11 // Còn lại 1 ngày
        };

        var donNghiPhep = new DonNghiPhep
        {
            MaDonPhep = maDonPhep,
            MaNhanVien = maNhanVien,
            MaLoaiPhep = 1,
            NgayBatDau = new DateTime(2026, 6, 1),
            NgayKetThuc = new DateTime(2026, 6, 3),
            SoNgayNghi = 3m,
            LyDo = "Việc cá nhân",
            TrangThai = DonNghiPhepTrangThai.ChoDuyet
        };

        donNghiPhepRepoMock.Setup(r => r.GetByIdAsync(maDonPhep)).ReturnsAsync(donNghiPhep);
        chamCongRepoMock.Setup(r => r.ExistsAnyAsync(maNhanVien, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);
        soNgayPhepRepoMock.Setup(r => r.EnsureAsync(maNhanVien, nam)).ReturnsAsync(soNgayPhep);

        var service = new DonNghiPhepService(
            donNghiPhepRepoMock.Object,
            soNgayPhepRepoMock.Object,
            loaiNghiPhepRepoMock.Object,
            chamCongRepoMock.Object);

        // 2. Act: Gọi duyệt (bắt ngoại lệ nếu có để kiểm tra trạng thái thực thể sau thao tác)
        try
        {
            await service.ApproveAsync(maDonPhep, nguoiDuyet);
        }
        catch (InvalidOperationException)
        {
            // Ngoại lệ là mong muốn theo nghiệp vụ
        }

        // 3. Assert: Kiểm tra các hậu quả
        // Trên baseline, đơn bị đổi thành Đã duyệt -> Assert này sẽ FAIL trên baseline
        Assert.NotEqual(DonNghiPhepTrangThai.DaDuyet, donNghiPhep.TrangThai);

        // Kiểm tra quỹ phép không bị trừ lố
        Assert.Equal(11, soNgayPhep.SoNgayDaSuDung);
        Assert.True(soNgayPhep.SoNgayConLai >= 0, $"Quỹ phép bị âm ({soNgayPhep.SoNgayConLai})!");

        // Kiểm tra không có lời gọi ghi repository trái phép
        donNghiPhepRepoMock.Verify(r => r.UpdateAsync(It.Is<DonNghiPhep>(d => d.TrangThai == DonNghiPhepTrangThai.DaDuyet)), Times.Never);
        soNgayPhepRepoMock.Verify(r => r.UpdateAsync(It.Is<SoNgayPhep>(s => s.SoNgayDaSuDung > 11)), Times.Never);
    }
}
