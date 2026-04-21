namespace HRM.Common.DTOs
{
    public class TinTuyenDungDTO
    {
        public int MaTinTuyenDung { get; set; }
        public string MaHienThi => $"TD{MaTinTuyenDung:D3}";

        public string ViTriTuyenDung { get; set; } = null!;
        public int SoLuongCanTuyen { get; set; }
        public DateTime ThoiHanNhanHoSo { get; set; }
        public string? TrangThai { get; set; }
        public string MucLuong => $"{MucLuongMin?.ToString("N0")} - {MucLuongMax?.ToString("N0")}";

        public decimal? MucLuongMin { get; set; }
        public decimal? MucLuongMax { get; set; }
        public string? MoTaCongViec { get; set; }
        public string? YeuCauUngVien { get; set; }
        public string? TenPhongBan { get; set; }
    }
}
