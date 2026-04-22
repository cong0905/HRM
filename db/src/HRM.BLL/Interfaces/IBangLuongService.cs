using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IBangLuongService
{
    /// <summary>Danh sách bảng lương theo tháng/năm. Admin: toàn bộ; nhân viên: chỉ bản thân.</summary>
    Task<List<BangLuongDTO>> GetBangLuongAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap);

    /// <summary>Tính lương cho mọi nhân viên đang làm việc, ghi đè bảng lương tháng đó. Thưởng/phạt = 0 (nhập sau trên lưới).</summary>
    Task<int> TinhVaLuuBangLuongThangAsync(int thang, int nam);

    /// <summary>Cập nhật thưởng/phạt thủ công và tính lại tổng thu nhập, thuế, thực nhận.</summary>
    Task CapNhatThuongPhatVaTinhLaiAsync(int maBangLuong, decimal tongThuong, decimal tongPhat);
}
