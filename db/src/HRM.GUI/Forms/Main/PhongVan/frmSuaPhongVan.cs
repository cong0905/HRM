using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using System.Data;

namespace HRM.GUI.Forms.Main.PhongVan
{

    public partial class frmSuaPhongVan : Form
    {
        private IPhongVanService _phongVanService;
        private INhanVienService _nhanVienService;
        private readonly PhongVanDTO _duLieuCu;

        public frmSuaPhongVan(IPhongVanService phongVanService, INhanVienService nhanVienService, PhongVanDTO duLieuCu)
        {
            InitializeComponent();
            _phongVanService = phongVanService;
            _nhanVienService = nhanVienService;
            _duLieuCu = duLieuCu;
            this.Load += frmSuaPhongVan_LoadAsync;
            btnLuu.Click += BtnLuu_Click;
        }

        private async void frmSuaPhongVan_LoadAsync(object sender, EventArgs e)
        {
            try
            {
                // --- TẢI DANH SÁCH (Giống Form Thêm) ---
                var lstUngVienAo = new List<dynamic>
                {
                    new { MaUngVien = 0, HoTen = "--- Chọn mã ứng viên ---" },
                    new { MaUngVien = 4, HoTen = "Phùng Thanh Độ" }
                };
                cbMaUngVien.DataSource = lstUngVienAo;
                cbMaUngVien.DisplayMember = "HoTen";
                cbMaUngVien.ValueMember = "MaUngVien";

                var lstTatCaNhanVien = await _nhanVienService.GetAllAsync();
                var lstNhanSu = lstTatCaNhanVien.Where(nv => nv.TenPhongBan != null && nv.TenPhongBan.Contains("Nhân sự")).ToList();
                lstNhanSu.Insert(0, new Domain.Entities.NhanVienDTO { MaNhanVien = 0, HoTen = "--- Chọn người phỏng vấn ---" });

                cbNguoiPV.DataSource = lstNhanSu;
                cbNguoiPV.DisplayMember = "HoTen";
                cbNguoiPV.ValueMember = "MaNhanVien";

                // --- KHÁC BIỆT: ĐỔ DỮ LIỆU CŨ LÊN CÁC Ô ---
                cbMaUngVien.SelectedValue = _duLieuCu.MaUngVien;
                cbNguoiPV.SelectedValue = _duLieuCu.NguoiPhongVan;
                cbVongPhongVan.Text = _duLieuCu.VongPhongVan;
                dtpNgayPhongvan.Value = _duLieuCu.NgayPhongVan;
                txtDiaDiem.Text = _duLieuCu.DiaDiem;
                cbTrangThai.Text = _duLieuCu.TrangThai;

                // Đổ thêm dữ liệu đánh giá
                cbKetQua.Text = _duLieuCu.KetQua;
                txtNhanXet.Text = _duLieuCu.NhanXet;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // 3. Nút Lưu (Cập nhật)
        private async void BtnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                if ((int)cbMaUngVien.SelectedValue == 0 || (int)cbNguoiPV.SelectedValue == 0)
                {
                    MessageBox.Show("Vui lòng chọn Ứng viên và Người phỏng vấn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Gọi DB lấy lại bản ghi cũ để Entity Framework theo dõi
                var pvCanSua = await _phongVanService.GetPhongVanByIdAsync(_duLieuCu.MaPhongVan);
                if (pvCanSua == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu gốc trong Database!", "Lỗi");
                    return;
                }

                // Ghi đè thông tin mới từ Form vào Entity
                pvCanSua.MaUngVien = (int)cbMaUngVien.SelectedValue;
                pvCanSua.NguoiPhongVan = (int)cbNguoiPV.SelectedValue;
                pvCanSua.VongPhongVan = int.Parse(cbVongPhongVan.Text);
                pvCanSua.NgayPhongVan = dtpNgayPhongvan.Value;
                pvCanSua.DiaDiem = txtDiaDiem.Text;
                pvCanSua.TrangThai = cbTrangThai.Text;
                pvCanSua.KetQua = cbKetQua.Text;
                pvCanSua.NhanXet = txtNhanXet.Text;

                // Gọi hàm UPDATE
                bool isSuccess = await _phongVanService.UpdatePhongVanAsync(pvCanSua);
                if (isSuccess)
                {
                    MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

