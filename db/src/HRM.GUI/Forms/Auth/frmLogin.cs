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

    private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is not Forms.Main.frmMain main)
        {
            Close();
            return;
        }

        main.FormClosed -= OnMainFormClosed;

        if (main.ClosedForRelogin)
        {
            txtPassword.Clear();
            lblStatus.Text = string.Empty;
            btnLogin.Enabled = true;
            Show();
            return;
        }

        Close();
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
    private void btnShowPassword_Click(object? sender, EventArgs e)
    {
        if (txtPassword.PasswordChar == '●')
        {
            txtPassword.PasswordChar = '\0';
            btnShowPassword.Text = "Ẩn"; // Hide password text
        }
        else
        {
            txtPassword.PasswordChar = '●';
            btnShowPassword.Text = "Hiện"; // Show password text
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

            this.Hide();
            var frmMain = Program.ServiceProvider.GetRequiredService<Forms.Main.frmMain>();
            frmMain.SetSession(session);
            frmMain.FormClosed += OnMainFormClosed;
            frmMain.Show();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Lỗi: {ex.Message}";
            btnLogin.Enabled = true;
        }
    }
}
