using HRM.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Auth;

public class frmQuenMatKhau : Form
{
    private readonly IAuthService _authService;
    private TextBox txtEmail;
    private Button btnSend;
    private Label lblStatus;
    private Label lblToken;

    public frmQuenMatKhau(IAuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Quên mật khẩu";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ClientSize = new Size(400, 170);

        var lbl = new Label { Text = "Nhập email đã đăng ký:", Location = new Point(20, 20), AutoSize = true };
        txtEmail = new TextBox { Location = new Point(20, 50), Width = 350 };
        btnSend = new Button { Text = "Gửi liên kết", Location = new Point(20, 90), Width = 120 };
        lblStatus = new Label { Location = new Point(20, 125), AutoSize = true, ForeColor = Color.Green };
        lblToken = new Label { Location = new Point(20, 145), AutoSize = true, ForeColor = Color.Blue };

        btnSend.Click += async (s, e) => await BtnSend_Click();

        Controls.Add(lbl);
        Controls.Add(txtEmail);
        Controls.Add(btnSend);
        Controls.Add(lblStatus);
    }

    private async Task BtnSend_Click()
    {
        btnSend.Enabled = false;
        lblStatus.ForeColor = Color.Black;
        lblStatus.Text = "Đang gửi...";
        var email = txtEmail.Text?.Trim();
        var token = await _authService.SendPasswordResetAsync(email ?? string.Empty);
        if (token == null)
        {
            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = "Mã xác nhận (OTP) gồm 6 chữ số đã được gửi đến email của bạn.";
            lblToken.Text = string.Empty;
        }
        else
        {
            // Dev mode: show token and open reset form
            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = "Token tạo thành công (dev mode).";
            lblToken.Text = $"Mã OTP: {token}";
        }
        
        // Open the reset form so the user can enter the OTP and new password
        var resetForm = Program.ServiceProvider.GetRequiredService<Forms.Auth.frmDatLaiMatKhau>();
        resetForm.ShowDialog(this);
        btnSend.Enabled = true;
    }

    // For display only
    public void SetTokenVisible(bool visible)
    {
        lblToken.Visible = visible;
    }
}
