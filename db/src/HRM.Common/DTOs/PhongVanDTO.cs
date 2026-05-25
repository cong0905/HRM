namespace HRM.Common.DTOs
{
    public class PhongVanDTO
    {
        public int MaPhongVan { get; set; }
        public int MaUngVien { get; set; }
        public string TenUngVien { get; set; } = string.Empty;

        public int? NguoiPhongVanId { get; set; }

        public string VongPhongVan { get; set; } = string.Empty;
        public DateTime NgayPhongVan { get; set; }
        public string DiaDiem { get; set; } = string.Empty;
        public string NguoiPhongVan { get; set; } = string.Empty;
        public string KetQua { get; set; } = string.Empty;
        public string DiemDanhGia { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string MaHienThi => $"PV{MaPhongVan:D4}";

        public string NhanXet { get; set; } = string.Empty;
    }
}
