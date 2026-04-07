namespace HRM.Common.DTOs
{
    public class RegisterDTO
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public int MaNhanVien { get; set; }
    }           
}
