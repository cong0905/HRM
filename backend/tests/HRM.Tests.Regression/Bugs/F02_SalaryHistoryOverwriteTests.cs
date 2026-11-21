using HRM.BLL.Services;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HRM.Tests.Regression.Bugs;

[Trait("Category", "RegressionBug")]
public class F02_SalaryHistoryOverwriteTests
{
    /// <summary>
    /// F02: Khi sửa mức lương hiện tại của nhân viên, hệ thống tự động cập nhật đè lên bảng lương quá khứ.
    ///
    /// Nghiệp vụ đúng (T00 Sign-off / SalaryHistory rules):
    /// - Lịch sử lương của các kỳ đã khóa/công bố trong quá khứ là bất biến (immutable).
    /// - Khi cập nhật mức lương mới cho nhân viên, các bản ghi BangLuong của các tháng trước
    ///   (ví dụ Tháng 1/2026 với LuongCoBan = 10.000.000 VNĐ) PHẢI được giữ nguyên.
    ///
    /// Hành vi lỗi ở baseline:
    /// - BangLuongService.DongBoBangLuongTheoNhanVienAsync truy vấn toàn bộ bản ghi của nhân viên:
    ///     var rows = await _db.BangLuong.Where(b => b.MaNhanVien == maNhanVien).ToListAsync();
    ///   và duyệt qua tất cả, cập nhật b.LuongCoBan = nv.MucLuong (15.000.000 VNĐ).
    /// - Kết quả: Bảng lương kỳ quá khứ bị ghi đè hoàn toàn bằng mức lương mới -> BUG REPRODUCED.
    ///
    /// Lưu ý: Test này CHỨNG MINH LỖI CŨ, KỲ VỌNG THẤT BẠI TRÊN BASELINE.
    /// KHÔNG ĐƯỢC SỬA ASSERTION ĐỂ TEST XANH GIAN LẬN.
    /// </summary>
    [Fact]
    public async Task DongBoBangLuongTheoNhanVien_WhenSalaryUpdated_MustNotOverwritePastPayrollRecords()
    {
        // 1. Arrange: Khởi tạo database in-memory cô lập
        var options = new DbContextOptionsBuilder<HrmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new HrmDbContext(options);

        const int maNhanVien = 1;
        const decimal luongCuThang1 = 10_000_000m;
        const decimal luongMoiThang5 = 15_000_000m;

        // Bảng lương lịch sử Tháng 1/2026 (đã chi trả)
        var pastPayrollThang1 = new BangLuong
        {
            MaBangLuong = 101,
            MaNhanVien = maNhanVien,
            Thang = 1,
            Nam = 2026,
            LuongCoBan = luongCuThang1,
            SoNgayLamViec = 22,
            LuongThucNhan = luongCuThang1,
            TrangThai = "DaThanhToan",
            NgayTinhLuong = new DateTime(2026, 1, 31)
        };
        db.BangLuong.Add(pastPayrollThang1);

        // Nhân viên được tăng lương lên 15.000.000 vào Tháng 5/2026
        var nhanVienMoi = new NhanVien
        {
            MaNhanVien = maNhanVien,
            HoTen = "Tran Van B",
            Email = "tvb@example.com",
            MucLuong = luongMoiThang5,
            NgayVaoLam = new DateTime(2025, 1, 1),
            TrangThai = "DangLamViec"
        };
        db.NhanVien.Add(nhanVienMoi);
        await db.SaveChangesAsync();

        var nhanVienRepoMock = new Mock<INhanVienRepository>();
        nhanVienRepoMock.Setup(r => r.GetByIdAsync(maNhanVien))
            .ReturnsAsync(nhanVienMoi);

        var bangLuongRepoMock = new Mock<IBangLuongRepository>();
        var chamCongRepoMock = new Mock<IChamCongRepository>();

        var bangLuongService = new BangLuongService(
            bangLuongRepoMock.Object,
            nhanVienRepoMock.Object,
            chamCongRepoMock.Object,
            db);

        // 2. Act: Thực hiện đồng bộ bảng lương theo nhân viên
        await bangLuongService.DongBoBangLuongTheoNhanVienAsync(maNhanVien);

        // 3. Assert: Kiểm tra bản ghi bảng lương Tháng 1/2026
        // Nghiệp vụ yêu cầu: Bảng lương quá khứ PHẢI giữ nguyên LuongCoBan = 10.000.000m
        // Trên baseline: Code cập nhật đè lên thành 15.000.000m -> Test sẽ FAIL trên baseline.
        var pastPayrollAfterSync = await db.BangLuong.FirstAsync(b => b.MaBangLuong == 101);
        Assert.Equal(luongCuThang1, pastPayrollAfterSync.LuongCoBan);
    }
}
