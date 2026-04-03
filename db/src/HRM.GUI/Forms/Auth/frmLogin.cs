using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Auth;

public partial class frmLogin : Form
{
    private readonly IAuthService _authService;

    public frmLogin(IAuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private async void btnLogin_Click(object? sender, EventArgs e)
    {
        await ExecuteLogin();
    }

    private async void txtPassword_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            await ExecuteLogin();
        }
    }

    private async Task ExecuteLogin()
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            lblStatus.Text = "Vui lòng nhập đầy đủ thông tin!";
            return;
        }

        try
        {
            btnLogin.Enabled = false;
            lblStatus.Text = "Đang kiểm tra...";

            var session = await _authService.LoginAsync(new LoginDTO
            {
                TenDangNhap = txtUsername.Text,
                MatKhau = txtPassword.Text
            });

            if (session == null)
            {
                lblStatus.Text = "Sai tên đăng nhập hoặc mật khẩu!";
                btnLogin.Enabled = true;
                return;
            }

            // Lưu session, mở form chính
            this.Hide();
            var frmMain = Program.ServiceProvider.GetRequiredService<Forms.Main.frmMain>();
            frmMain.SetSession(session);
            frmMain.FormClosed += (s, e) => this.Close();
            frmMain.Show();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
            btnLogin.Enabled = true;
        }
    }
}
