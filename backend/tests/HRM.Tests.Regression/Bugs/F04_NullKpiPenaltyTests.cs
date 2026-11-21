using HRM.BLL.Services;
using Xunit;

namespace HRM.Tests.Regression.Bugs;

[Trait("Category", "RegressionBug")]
public class F04_NullKpiPenaltyTests
{
    /// <summary>
    /// F04: Nhân viên chưa đánh giá hiệu suất hoặc có điểm KPI null bị phạt hệ số -10% (-0.10).
    ///
    /// Nghiệp vụ đúng (T00 Sign-off / OPEN-03):
    /// - Khi nhân viên chưa được đánh giá hiệu suất (diemTong == null), hệ số thưởng/phạt
    ///   phải là trung tính (0.0m), KHÔNG ĐƯỢC tự động phạt -10%.
    /// - "Không có KPI không bị phạt."
    ///
    /// Hành vi lỗi ở baseline:
    /// - HieuSuatService.TinhBonusHieuSuat(decimal? diemTong) thực hiện:
    ///     var diemXepLoai = diemTong ?? 0m;
    ///   Sau đó 0m rơi vào nhánh mặc định: _ => -0.10m.
    /// - Kết quả: TinhBonusHieuSuat(null) trả về -0.10m -> BUG REPRODUCED.
    ///
    /// Lưu ý: Test này CHỨNG MINH LỖI CŨ, KỲ VỌNG THẤT BẠI TRÊN BASELINE.
    /// KHÔNG ĐƯỢC SỬA ASSERTION ĐỂ TEST XANH GIAN LẬN.
    /// </summary>
    [Fact]
    public void TinhBonusHieuSuat_WhenDiemTongIsNull_MustNotPenalizeNegativeTenPercent()
    {
        // 1. Arrange: Điểm hiệu suất tổng là null (nhân viên mới vào, chưa có đánh giá hoặc kỳ chưa chốt)
        decimal? diemTong = null;

        // 2. Act: Gọi hàm tính thưởng/phạt hiệu suất của baseline
        var heSoBonus = HieuSuatService.TinhBonusHieuSuat(diemTong);

        // 3. Assert: Nghiệp vụ yêu cầu trung tính (0.0m).
        // Trên baseline, hàm trả về -0.10m (-10%) -> Test này sẽ FAIL trên baseline.
        Assert.Equal(0.0m, heSoBonus);
    }
}

