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

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form config
        this.Text = "HRM - Đăng nhập hệ thống";
        this.Size = new Size(420, 340);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(245, 245, 250);

        // Header
        var lblTitle = new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ NHÂN SỰ",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = false,
            Size = new Size(380, 40),
            Location = new Point(10, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Username
        var lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(50, 80), AutoSize = true, Font = new Font("Segoe UI", 10) };
        var txtUser = new TextBox { Name = "txtUsername", Location = new Point(50, 105), Size = new Size(300, 30), Font = new Font("Segoe UI", 11) };

        // Password
        var lblPass = new Label { Text = "Mật khẩu:", Location = new Point(50, 145), AutoSize = true, Font = new Font("Segoe UI", 10) };
        var txtPass = new TextBox { Name = "txtPassword", Location = new Point(50, 170), Size = new Size(300, 30), Font = new Font("Segoe UI", 11), PasswordChar = '●' };

        // Login button
        var btnLogin = new Button
        {
            Text = "ĐĂNG NHẬP",
            Location = new Point(50, 220),
            Size = new Size(300, 40),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(30, 60, 120),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += async (s, e) => await BtnLogin_Click(txtUser.Text, txtPass.Text);

        // Status label
        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "",
            Location = new Point(50, 270),
            Size = new Size(300, 20),
            ForeColor = Color.Red,
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Enter key support
        txtPass.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) await BtnLogin_Click(txtUser.Text, txtPass.Text);
        };

        this.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUser, lblPass, txtPass, btnLogin, lblStatus });
        this.AcceptButton = btnLogin;
        this.ResumeLayout();
    }

    private async Task BtnLogin_Click(string username, string password)
    {
        var lblStatus = this.Controls.Find("lblStatus", false).FirstOrDefault() as Label;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (lblStatus != null) lblStatus.Text = "Vui lòng nhập đầy đủ thông tin!";
            return;
        }

        try
        {
            var session = await _authService.LoginAsync(new LoginDTO
            {
                TenDangNhap = username,
                MatKhau = password
            });

            if (session == null)
            {
                if (lblStatus != null) lblStatus.Text = "Sai tên đăng nhập hoặc mật khẩu!";
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
            if (lblStatus != null) lblStatus.Text = $"Lỗi: {ex.Message}";
        }
    }
}
