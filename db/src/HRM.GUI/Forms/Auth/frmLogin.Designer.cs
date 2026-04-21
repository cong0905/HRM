namespace HRM.GUI.Forms.Auth;

partial class frmLogin
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblUser = new Label();
        txtUsername = new TextBox();
        lblPass = new Label();
        txtPassword = new TextBox();
        btnLogin = new Button();
        lblStatus = new Label();
        btnShowPassword = new Button();
        SuspendLayout();

        // 
        // lblTitle
        // 
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(30, 60, 120);
        lblTitle.Location = new Point(12, 20);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(378, 40);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "HỆ THỐNG QUẢN LÝ NHÂN SỰ";
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;

        // 
        // lblUser
        // 
        lblUser.AutoSize = true;
        lblUser.Font = new Font("Segoe UI", 10F);
        lblUser.Location = new Point(50, 80);
        lblUser.Name = "lblUser";
        lblUser.Size = new Size(111, 23);
        lblUser.TabIndex = 1;
        lblUser.Text = "Tên đăng nhập:";

        // 
        // txtUsername
        // 
        txtUsername.Font = new Font("Segoe UI", 11F);
        txtUsername.Location = new Point(50, 105);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(300, 32);
        txtUsername.TabIndex = 2;

        // 
        // lblPass
        // 
        lblPass.AutoSize = true;
        lblPass.Font = new Font("Segoe UI", 10F);
        lblPass.Location = new Point(50, 145);
        lblPass.Name = "lblPass";
        lblPass.Size = new Size(86, 23);
        lblPass.TabIndex = 3;
        lblPass.Text = "Mật khẩu:";

        // 
        // txtPassword
        // 
        txtPassword.Font = new Font("Segoe UI", 11F);
        txtPassword.Location = new Point(50, 170);
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '●';
        txtPassword.Size = new Size(240, 32);
        txtPassword.TabIndex = 4;
        txtPassword.KeyDown += txtPassword_KeyDown;

        // 
        // btnShowPassword
        // 
        btnShowPassword.Cursor = Cursors.Hand;
        btnShowPassword.FlatAppearance.BorderSize = 1;
        btnShowPassword.FlatAppearance.BorderColor = Color.DarkGray;
        btnShowPassword.FlatStyle = FlatStyle.Flat;
        btnShowPassword.Font = new Font("Segoe UI", 9F);
        btnShowPassword.Location = new Point(295, 170);
        btnShowPassword.Name = "btnShowPassword";
        btnShowPassword.Size = new Size(55, 29);
        btnShowPassword.TabIndex = 7;
        btnShowPassword.Text = "Hiện";
        btnShowPassword.UseVisualStyleBackColor = true;
        btnShowPassword.Click += btnShowPassword_Click;

        // 
        // btnLogin
        // 
        btnLogin.BackColor = Color.FromArgb(30, 60, 120);
        btnLogin.Cursor = Cursors.Hand;
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.FlatStyle = FlatStyle.Flat;
        btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(50, 220);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(300, 40);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "ĐĂNG NHẬP";
        btnLogin.UseVisualStyleBackColor = false;
        btnLogin.Click += btnLogin_Click;

        // 
        // lblStatus
        // 
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.ForeColor = Color.Red;
        lblStatus.Location = new Point(50, 270);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(300, 23);
        lblStatus.TabIndex = 6;
        lblStatus.TextAlign = ContentAlignment.MiddleCenter;

        // 
        // frmLogin
        // 
        AcceptButton = btnLogin;
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 245, 250);
        ClientSize = new Size(402, 313);
        Controls.Add(lblTitle);
        Controls.Add(lblUser);
        Controls.Add(txtUsername);
        Controls.Add(lblPass);
        Controls.Add(txtPassword);
        Controls.Add(btnShowPassword);
        Controls.Add(btnLogin);
        Controls.Add(lblStatus);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "frmLogin";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HRM - Đăng nhập hệ thống";
        ResumeLayout(false);
        PerformLayout();
    }

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Button btnShowPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;
    private Label lblTitle = null!;
    private Label lblUser = null!;
    private Label lblPass = null!;
}
