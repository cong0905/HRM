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
        this.pnlSidebar = new System.Windows.Forms.Panel();
        this.lblLogo = new System.Windows.Forms.Label();
        this.pnlHeader = new System.Windows.Forms.Panel();
        this.lblWelcome = new System.Windows.Forms.Label();
        this.btnLogout = new System.Windows.Forms.Button();
        this.pnlContent = new System.Windows.Forms.Panel();
        this.lblDashboard = new System.Windows.Forms.Label();
        this.pnlSidebar.SuspendLayout();
        this.pnlHeader.SuspendLayout();
        this.pnlContent.SuspendLayout();
        this.SuspendLayout();
        // 
        // pnlSidebar
        // 
        this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(30, 45, 80);
        this.pnlSidebar.Controls.Add(this.lblLogo);
        this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
        this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
        this.pnlSidebar.Name = "pnlSidebar";
        this.pnlSidebar.Size = new System.Drawing.Size(220, 700);
        this.pnlSidebar.TabIndex = 0;
        // 
        // lblLogo
        // 
        this.lblLogo.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblLogo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        this.lblLogo.ForeColor = System.Drawing.Color.White;
        this.lblLogo.Location = new System.Drawing.Point(0, 0);
        this.lblLogo.Name = "lblLogo";
        this.lblLogo.Size = new System.Drawing.Size(220, 60);
        this.lblLogo.TabIndex = 0;
        this.lblLogo.Text = "🏢 HRM System";
        this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // pnlHeader
        // 
        this.pnlHeader.BackColor = System.Drawing.Color.White;
        this.pnlHeader.Controls.Add(this.lblWelcome);
        this.pnlHeader.Controls.Add(this.btnLogout);
        this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlHeader.Location = new System.Drawing.Point(220, 0);
        this.pnlHeader.Name = "pnlHeader";
        this.pnlHeader.Size = new System.Drawing.Size(980, 50);
        this.pnlHeader.TabIndex = 1;
        // 
        // lblWelcome
        // 
        this.lblWelcome.AutoSize = true;
        this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.lblWelcome.Location = new System.Drawing.Point(20, 15);
        this.lblWelcome.Name = "lblWelcome";
        this.lblWelcome.Size = new System.Drawing.Size(89, 25);
        this.lblWelcome.TabIndex = 0;
        this.lblWelcome.Text = "Xin chào!";
        // 
        // btnLogout
        // 
        this.btnLogout.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
        this.btnLogout.BackColor = System.Drawing.Color.White;
        this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnLogout.Location = new System.Drawing.Point(850, 10);
        this.btnLogout.Name = "btnLogout";
        this.btnLogout.Size = new System.Drawing.Size(120, 30);
        this.btnLogout.TabIndex = 1;
        this.btnLogout.Text = "🚪 Đăng xuất";
        this.btnLogout.UseVisualStyleBackColor = false;
        this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
        // 
        // pnlContent
        // 
        this.pnlContent.BackColor = System.Drawing.Color.FromArgb(240, 242, 245);
        this.pnlContent.Controls.Add(this.lblDashboard);
        this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlContent.Location = new System.Drawing.Point(220, 50);
        this.pnlContent.Name = "pnlContent";
        this.pnlContent.Size = new System.Drawing.Size(980, 650);
        this.pnlContent.TabIndex = 2;
        // 
        // lblDashboard
        // 
        this.lblDashboard.AutoSize = true;
        this.lblDashboard.Font = new System.Drawing.Font("Segoe UI", 13F);
        this.lblDashboard.Location = new System.Drawing.Point(30, 30);
        this.lblDashboard.Name = "lblDashboard";
        this.lblDashboard.Size = new System.Drawing.Size(437, 60);
        this.lblDashboard.TabIndex = 0;
        this.lblDashboard.Text = "📊 Tổng quan hệ thống\r\n\r\nChọn chức năng từ menu bên trái để bắt đầu.";
        // 
        // frmMain
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1200, 700);
        this.Controls.Add(this.pnlContent);
        this.Controls.Add(this.pnlHeader);
        this.Controls.Add(this.pnlSidebar);
        this.Name = "frmMain";
        this.Text = "HRM - Hệ thống Quản lý Nhân sự";
        this.pnlSidebar.ResumeLayout(false);
        this.pnlHeader.ResumeLayout(false);
        this.pnlHeader.PerformLayout();
        this.pnlContent.ResumeLayout(false);
        this.pnlContent.PerformLayout();
        this.ResumeLayout(false);
    }

    private System.Windows.Forms.Panel pnlSidebar;
    private System.Windows.Forms.Panel pnlContent;
    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblWelcome;
    private System.Windows.Forms.Button btnLogout;
    private System.Windows.Forms.Label lblLogo;
    private System.Windows.Forms.Label lblDashboard;
}
