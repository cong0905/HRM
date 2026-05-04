using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.GUI.Forms.Main
{
    public partial class frmThemNhanVien : Form
    {
        private readonly INhanVienService _nhanVienService;
        private readonly IPhongBanService _phongBanService;
        private readonly IRepository<ChucVu> _chucVuRepo;

        public frmThemNhanVien(INhanVienService nhanVienService, IPhongBanService phongBanService, IRepository<ChucVu> chucVuRepo)
        {
            _nhanVienService = nhanVienService;
            _phongBanService = phongBanService;
            _chucVuRepo = chucVuRepo;
            InitializeComponent();
        }

        private async Task LoadComboBoxData()
        {
            try
            {
                // Load Phòng ban
                var phongBans = await _phongBanService.GetAllAsync();
                cboPhongBan.DataSource = phongBans;
                cboPhongBan.DisplayMember = "TenPhongBan";
                cboPhongBan.ValueMember = "MaPhongBan";
                cboPhongBan.SelectedIndex = -1;

                // Load Chức vụ
                var chucVus = await _chucVuRepo.GetAllAsync();
                cboChucVu.DataSource = chucVus;
                cboChucVu.DisplayMember = "TenChucVu";
                cboChucVu.ValueMember = "MaChucVu";
                cboChucVu.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnLuu.Enabled = false;

                var dto = new NhanVienCreateDTO
                {
                    HoTen = txtHoTen.Text.Trim(),
                    NgaySinh = dtpNgaySinh.Value,
                    GioiTinh = cboGioiTinh.Text,
                    SoDienThoai = txtSoDienThoai.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    CCCD = txtCCCD.Text.Trim(),
                    MaPhongBan = cboPhongBan.SelectedValue as int?,
                    MaChucVu = cboChucVu.SelectedValue as int?,
                    MucLuong = decimal.TryParse(txtMucLuong.Text.Trim(), out var luong) ? luong : 0,
                    NgayVaoLam = dtpNgayVaoLam.Value,
                    TrangThai = "Đang làm việc"
                };

                await _nhanVienService.CreateAsync(dto);
                MessageBox.Show("Thêm nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Lỗi: {error}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }

        public HRM.Domain.Entities.UngVien? UngVienTruocDo { get; set; }

        private async void frmThemNhanVien_Load(object sender, EventArgs e)
        {
            await LoadComboBoxData();

            if (UngVienTruocDo != null)
            {
                txtHoTen.Text = UngVienTruocDo.HoTen;
                txtEmail.Text = UngVienTruocDo.Email;
                txtSoDienThoai.Text = UngVienTruocDo.SoDienThoai;
                
                if (UngVienTruocDo.TinTuyenDung != null && UngVienTruocDo.TinTuyenDung.MaPhongBan.HasValue)
                {
                    cboPhongBan.SelectedValue = UngVienTruocDo.TinTuyenDung.MaPhongBan.Value;
                }
            }
        }

        private void txtHoTen_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
