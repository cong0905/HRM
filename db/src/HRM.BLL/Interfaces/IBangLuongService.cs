using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IBangLuongService
{
    /// <summary>Danh sách bảng lương theo tháng/năm. Admin: toàn bộ; nhân viên: chỉ bản thân.</summary>
    Task<List<BangLuongDTO>> GetBangLuongAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap);

    /// <summary>Tính lương cho mọi nhân viên đang làm việc, ghi đè bảng lương tháng đó. Giữ thưởng/phạt đã nhập trước đó.</summary>
    Task<int> TinhVaLuuBangLuongThangAsync(int thang, int nam);

    /// <summary>Cập nhật thưởng/phạt thủ công và tính lại tổng thu nhập, thuế, thực nhận.</summary>
    Task CapNhatThuongPhatVaTinhLaiAsync(int maBangLuong, decimal tongThuong, decimal tongPhat);

    /// <summary>Cập nhật BHXH/BHYT/BHTN/thuế TNCN thủ công và tính lại tổng khấu trừ, thực nhận.</summary>
    Task CapNhatKhoanKhauTruVaTinhLaiAsync(int maBangLuong, decimal bhxh, decimal bhyt, decimal bhtn, decimal thueTncn);

    /// <summary>Đồng bộ LuongCoBan từ MucLuong hiện tại của nhân viên và tính lại các khoản liên quan.</summary>
    Task DongBoBangLuongTheoNhanVienAsync(int maNhanVien);
}
