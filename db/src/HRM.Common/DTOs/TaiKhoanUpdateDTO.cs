namespace HRM.Common.DTOs;

public class TaiKhoanUpdateDTO
{
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? MatKhauMoi { get; set; } // Nếu không rỗng thì sẽ reset mật khẩu
}
