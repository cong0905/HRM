namespace HRM.GUI.Forms.Main;

partial class frmMain
{
    private System.ComponentModel.IContainer components = null;

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
        pnlSidebar = new Panel();
        lblLogo = new Label();
        pnlHeader = new Panel();
        lblWelcome = new Label();
        btnDoiMatKhau = new Button();
        btnLogout = new Button();
        pnlContent = new Panel();
        lblDashboard = new Label();
        pnlSidebar.SuspendLayout();
        pnlHeader.SuspendLayout();
        pnlContent.SuspendLayout();
        SuspendLayout();
        // 
        // pnlSidebar
        // 
        pnlSidebar.BackColor = Color.FromArgb(30, 45, 80);
        pnlSidebar.Controls.Add(lblLogo);
        pnlSidebar.Dock = DockStyle.Left;
        pnlSidebar.Location = new Point(0, 0);
        pnlSidebar.Margin = new Padding(3, 2, 3, 2);
        pnlSidebar.Name = "pnlSidebar";
        pnlSidebar.Size = new Size(192, 525);
        pnlSidebar.TabIndex = 0;
        // 
        // lblLogo
        // 
        lblLogo.Dock = DockStyle.Top;
        lblLogo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblLogo.ForeColor = Color.White;
        lblLogo.Location = new Point(0, 0);
        lblLogo.Name = "lblLogo";
        lblLogo.Size = new Size(192, 45);
        lblLogo.TabIndex = 0;
        lblLogo.Text = "🏢 HRM System";
        lblLogo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pnlHeader
        // 
        pnlHeader.BackColor = Color.White;
        pnlHeader.Controls.Add(lblWelcome);
        pnlHeader.Controls.Add(btnDoiMatKhau);
        pnlHeader.Controls.Add(btnLogout);
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Location = new Point(192, 0);
        pnlHeader.Margin = new Padding(3, 2, 3, 2);
        pnlHeader.Name = "pnlHeader";
        pnlHeader.Size = new Size(858, 38);
        pnlHeader.TabIndex = 1;
        // 
        // lblWelcome
        // 
        lblWelcome.AutoSize = true;
        lblWelcome.Font = new Font("Segoe UI", 11F);
        lblWelcome.Location = new Point(18, 11);
        lblWelcome.Name = "lblWelcome";
        lblWelcome.Size = new Size(70, 20);
        lblWelcome.TabIndex = 0;
        lblWelcome.Text = "Xin chào!";
        // 
        // btnDoiMatKhau
        // 
        btnDoiMatKhau.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDoiMatKhau.BackColor = Color.White;
        btnDoiMatKhau.FlatStyle = FlatStyle.Flat;
        btnDoiMatKhau.Location = new Point(630, 8);
        btnDoiMatKhau.Margin = new Padding(3, 2, 3, 2);
        btnDoiMatKhau.Name = "btnDoiMatKhau";
        btnDoiMatKhau.Size = new Size(105, 22);
        btnDoiMatKhau.TabIndex = 2;
        btnDoiMatKhau.Text = "🔑 Đổi mật khẩu";
        btnDoiMatKhau.UseVisualStyleBackColor = false;
        btnDoiMatKhau.Click += btnDoiMatKhau_Click;
        // 
        // btnLogout
        // 
        btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnLogout.BackColor = Color.White;
        btnLogout.FlatStyle = FlatStyle.Flat;
        btnLogout.Location = new Point(744, 8);
        btnLogout.Margin = new Padding(3, 2, 3, 2);
        btnLogout.Name = "btnLogout";
        btnLogout.Size = new Size(105, 22);
        btnLogout.TabIndex = 1;
        btnLogout.Text = "🚪 Đăng xuất";
        btnLogout.UseVisualStyleBackColor = false;
        btnLogout.Click += btnLogout_Click;
        // 
        // pnlContent
        // 
        pnlContent.BackColor = Color.FromArgb(240, 242, 245);
        pnlContent.Controls.Add(lblDashboard);
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Location = new Point(192, 38);
        pnlContent.Margin = new Padding(3, 2, 3, 2);
        pnlContent.Name = "pnlContent";
        pnlContent.Size = new Size(858, 487);
        pnlContent.TabIndex = 2;
        
        // 
        // lblDashboard
        // 
        lblDashboard.AutoSize = true;
        lblDashboard.Font = new Font("Segoe UI", 13F);
        lblDashboard.Location = new Point(26, 22);
        lblDashboard.Name = "lblDashboard";
        lblDashboard.Size = new Size(373, 75);
        lblDashboard.TabIndex = 0;
        lblDashboard.Text = "📊 Tổng quan hệ thống\r\n\r\nChọn chức năng từ menu bên trái để bắt đầu.";
        // 
        // frmMain
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1050, 525);
        Controls.Add(pnlContent);
        Controls.Add(pnlHeader);
        Controls.Add(pnlSidebar);
        Margin = new Padding(3, 2, 3, 2);
        Name = "frmMain";
        Text = "HRM - Hệ thống Quản lý Nhân sự";
        pnlSidebar.ResumeLayout(false);
        pnlHeader.ResumeLayout(false);
        pnlHeader.PerformLayout();
        pnlContent.ResumeLayout(false);
        pnlContent.PerformLayout();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlSidebar;
    private System.Windows.Forms.Panel pnlContent;
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblWelcome;
    private System.Windows.Forms.Button btnLogout;
    private System.Windows.Forms.Button btnDoiMatKhau;
    private System.Windows.Forms.Label lblLogo;
    private System.Windows.Forms.Label lblDashboard;
}
