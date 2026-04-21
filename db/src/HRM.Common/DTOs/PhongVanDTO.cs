namespace HRM.Common.DTOs
{
    public class PhongVanDTO
    {
        public int MaPhongVan { get; set; }
        public int MaUngVien { get; set; }
        public string TenUngVien { get; set; }

        public string VongPhongVan { get; set; }
        public DateTime NgayPhongVan { get; set; }
        public string DiaDiem { get; set; }
        public string NguoiPhongVan { get; set; }
        public string KetQua { get; set; }
        public string DiemDanhGia { get; set; }
        public string TrangThai { get; set; }
        public string MaHienThi => $"PV{MaPhongVan:D4}";

        public string NhanXet { get; set; }
    }
}
