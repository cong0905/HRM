using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using System.Windows.Forms;

namespace HRM.GUI.Forms.Auth;

public partial class frmTaoTaiKhoan : Form
{
    private readonly IAuthService _authService;

    public frmTaoTaiKhoan(IAuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private void frmTaoTaiKhoan_Load(object sender, EventArgs e)
    {
        cboVaiTro.SelectedIndex = 1; // Mặc định là Nhân viên
    }

    private async void btnTaoMoi_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTenDangNhap.Text) || string.IsNullOrWhiteSpace(txtMatKhau.Text))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!int.TryParse(txtMaNhanVien.Text, out int maNhanVien))
        {
            MessageBox.Show("Mã nhân viên phải là số!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnTaoMoi.Enabled = false;

            var dto = new RegisterDTO
            {
                TenDangNhap = txtTenDangNhap.Text.Trim(),
                MatKhau = txtMatKhau.Text,
                VaiTro = cboVaiTro.Text,
                MaNhanVien = maNhanVien
            };

            bool success = await _authService.RegisterAsync(dto);
            if (success)
            {
                MessageBox.Show("Tạo tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
        catch (Exception ex)
        {
            string error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            MessageBox.Show($"Lỗi chi tiết: {error}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTaoMoi.Enabled = true;
        }
    }
}
