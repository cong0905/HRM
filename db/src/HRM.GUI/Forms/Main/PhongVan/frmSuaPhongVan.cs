using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using System.Data;

namespace HRM.GUI.Forms.Main.PhongVan
{

    public partial class frmSuaPhongVan : Form
    {
        private readonly IPhongVanService _phongVanService;
        private readonly INhanVienService _nhanVienService;
        private readonly IUngVienService _ungVienService;
        public int MaPhongVanCachSua { get; set; }

        public frmSuaPhongVan(IPhongVanService phongVanService, INhanVienService nhanVienService, IUngVienService ungVienService)
        {
            InitializeComponent();
            _phongVanService = phongVanService;
            _nhanVienService = nhanVienService;
            _ungVienService = ungVienService;
            this.Load += frmSuaPhongVan_LoadAsync;
            btnLuu.Click += BtnLuu_Click;
        }

        private async void frmSuaPhongVan_LoadAsync(object sender, EventArgs e)
        {
            try
            {
                var lstUngVien = await _ungVienService.GetAllUngVienAsync();
                var ungVienData = new List<dynamic> { new { MaUngVien = 0, HoTen = "--- Chọn ứng viên ---" } };
                ungVienData.AddRange(lstUngVien.Select(uv => new { uv.MaUngVien, uv.HoTen }));

                cbMaUngVien.DataSource = ungVienData;
                cbMaUngVien.DisplayMember = "HoTen";
                cbMaUngVien.ValueMember = "MaUngVien";

                cbVongPhongVan.Items.Clear();
                cbVongPhongVan.Items.AddRange(new object[] { "1", "2", "3" });

                cbTrangThai.Items.Clear();
                cbTrangThai.Items.AddRange(new object[] { "Đã lên lịch", "Đã phỏng vấn", "Đã hủy" });

                cbKetQua.Items.Clear();
                cbKetQua.Items.AddRange(new object[] { "", "Đạt", "Không đạt", "Chờ kết quả" });

                var lstTatCaNhanVien = await _nhanVienService.GetAllAsync();
                var lstNhanSu = lstTatCaNhanVien.Where(nv => nv.TenPhongBan != null && nv.TenPhongBan.Contains("Nhân sự")).ToList();
                lstNhanSu.Insert(0, new HRM.Common.DTOs.NhanVienDTO { MaNhanVien = 0, HoTen = "--- Chọn người phỏng vấn ---" });

                cbNguoiPV.DataSource = lstNhanSu;
                cbNguoiPV.DisplayMember = "HoTen";
                cbNguoiPV.ValueMember = "MaNhanVien";

                var phongVanCanSua = await _phongVanService.GetPhongVanByIdAsync(MaPhongVanCachSua);
                if (phongVanCanSua == null)
                {
                    MessageBox.Show("Không tìm thấy lịch phỏng vấn cần sửa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                cbMaUngVien.SelectedValue = phongVanCanSua.MaUngVien;
                cbNguoiPV.SelectedValue = phongVanCanSua.NguoiPhongVan ?? 0;
                cbVongPhongVan.Text = phongVanCanSua.VongPhongVan.ToString();
                dtpNgayPhongvan.Value = phongVanCanSua.NgayPhongVan;
                txtDiaDiem.Text = phongVanCanSua.DiaDiem ?? string.Empty;
                cbTrangThai.Text = phongVanCanSua.TrangThai;
                cbKetQua.Text = phongVanCanSua.KetQua ?? string.Empty;
                txtNhanXet.Text = phongVanCanSua.NhanXet ?? string.Empty;
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
                var pvCanSua = await _phongVanService.GetPhongVanByIdAsync(MaPhongVanCachSua);
                if (pvCanSua == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu gốc trong Database!", "Lỗi");
                    return;
                }

                if (!int.TryParse(cbVongPhongVan.Text, out int vongPV))
                {
                    MessageBox.Show("Vui lòng chọn hoặc nhập Vòng phỏng vấn hợp lệ (số nguyên)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ghi đè thông tin mới từ Form vào Entity
                pvCanSua.MaUngVien = (int)cbMaUngVien.SelectedValue;
                pvCanSua.NguoiPhongVan = (int)cbNguoiPV.SelectedValue;
                pvCanSua.VongPhongVan = vongPV;
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

