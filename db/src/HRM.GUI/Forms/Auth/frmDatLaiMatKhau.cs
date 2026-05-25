using HRM.BLL.Interfaces;

namespace HRM.GUI.Forms.Auth;

public class frmDatLaiMatKhau : Form
{
    private readonly IAuthService _authService;
    private TextBox txtToken;
    private TextBox txtPassword;
    private TextBox txtConfirm;
    private Button btnReset;
    private Label lblStatus;

    public frmDatLaiMatKhau(IAuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    // Optional: allow pre-filling token from dev flow
    public string? TokenValue
    {
        get => txtToken?.Text;
        set
        {
            if (txtToken != null && value != null) txtToken.Text = value;
        }
    }

    private void InitializeComponent()
    {
        Text = "Đặt lại mật khẩu";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ClientSize = new Size(420, 220);

        var lblToken = new Label { Text = "Mã OTP (6 số):", Location = new Point(20, 20), AutoSize = true };
        txtToken = new TextBox { Location = new Point(20, 45), Width = 370 };
        var lblPass = new Label { Text = "Mật khẩu mới:", Location = new Point(20, 80), AutoSize = true };
        txtPassword = new TextBox { Location = new Point(20, 105), Width = 200, PasswordChar = '●' };
        var lblConfirm = new Label { Text = "Xác nhận mật khẩu:", Location = new Point(230, 80), AutoSize = true };
        txtConfirm = new TextBox { Location = new Point(230, 105), Width = 160, PasswordChar = '●' };

        btnReset = new Button { Text = "Đặt lại", Location = new Point(20, 145), Width = 100 };
        lblStatus = new Label { Location = new Point(20, 180), AutoSize = true };

        btnReset.Click += async (s, e) => await BtnReset_Click();

        Controls.Add(lblToken);
        Controls.Add(txtToken);
        Controls.Add(lblPass);
        Controls.Add(txtPassword);
        Controls.Add(lblConfirm);
        Controls.Add(txtConfirm);
        Controls.Add(btnReset);
        Controls.Add(lblStatus);
    }

    private async Task BtnReset_Click()
    {
        btnReset.Enabled = false;
        lblStatus.ForeColor = Color.Black;
        lblStatus.Text = "Đang xử lý...";

        var token = txtToken.Text?.Trim();
        var pass = txtPassword.Text ?? string.Empty;
        var conf = txtConfirm.Text ?? string.Empty;

        if (pass != conf)
        {
            lblStatus.ForeColor = Color.Red;
            lblStatus.Text = "Mật khẩu xác nhận không khớp.";
            btnReset.Enabled = true;
            return;
        }

        var ok = await _authService.ResetPasswordWithTokenAsync(token ?? string.Empty, pass);
        if (ok)
        {
            lblStatus.ForeColor = Color.Green;
            lblStatus.Text = "Đổi mật khẩu thành công.";
        }
        else
        {
            lblStatus.ForeColor = Color.Red;
            lblStatus.Text = "Token không hợp lệ hoặc đã hết hạn.";
        }

        btnReset.Enabled = true;
    }
}
