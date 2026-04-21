using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main
{
    public partial class frmThemNhanVien : Form
    {
        private readonly INhanVienService _nhanVienService;

        public frmThemNhanVien(INhanVienService nhanVienService)
        {
            _nhanVienService = nhanVienService;
            InitializeComponent();
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
                    NgayVaoLam = dtpNgayVaoLam.Value,
                    MucLuong = 0, // Mặc định
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

        private void frmThemNhanVien_Load(object sender, EventArgs e)
        {

        }

        private void txtHoTen_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
