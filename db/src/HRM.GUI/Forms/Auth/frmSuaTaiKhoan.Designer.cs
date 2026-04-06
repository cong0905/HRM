namespace HRM.GUI.Forms.Auth
{
    partial class frmSuaTaiKhoan
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblTenDangNhap = new System.Windows.Forms.Label();
            this.txtTenDangNhap = new System.Windows.Forms.TextBox();
            this.lblVaiTro = new System.Windows.Forms.Label();
            this.cboVaiTro = new System.Windows.Forms.ComboBox();
            this.lblTrangThai = new System.Windows.Forms.Label();
            this.cboTrangThai = new System.Windows.Forms.ComboBox();
            this.lblMatKhauMoi = new System.Windows.Forms.Label();
            this.txtMatKhauMoi = new System.Windows.Forms.TextBox();
            this.btnLuu = new System.Windows.Forms.Button();
            this.lblGhiChuMatKhau = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.lblTitle.Location = new System.Drawing.Point(50, 20);
            this.lblTitle.Size = new System.Drawing.Size(240, 30);
            this.lblTitle.Text = "SỬA TÀI KHOẢN";

            // lblTenDangNhap
            this.lblTenDangNhap.AutoSize = true;
            this.lblTenDangNhap.Location = new System.Drawing.Point(30, 80);
            this.lblTenDangNhap.Text = "Tên đăng nhập:";
            // txtTenDangNhap
            this.txtTenDangNhap.Location = new System.Drawing.Point(135, 77);
            this.txtTenDangNhap.Size = new System.Drawing.Size(180, 23);
            this.txtTenDangNhap.ReadOnly = true; // Không được sửa username

            // lblVaiTro
            this.lblVaiTro.AutoSize = true;
            this.lblVaiTro.Location = new System.Drawing.Point(30, 120);
            this.lblVaiTro.Text = "Vai trò:";
            // cboVaiTro
            this.cboVaiTro.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVaiTro.Items.AddRange(new object[] { "Admin", "Nhân viên" });
            this.cboVaiTro.Location = new System.Drawing.Point(135, 117);
            this.cboVaiTro.Size = new System.Drawing.Size(180, 23);

            // lblTrangThai
            this.lblTrangThai.AutoSize = true;
            this.lblTrangThai.Location = new System.Drawing.Point(30, 160);
            this.lblTrangThai.Text = "Trạng thái:";
            // cboTrangThai
            this.cboTrangThai.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTrangThai.Items.AddRange(new object[] { "Hoạt động", "Đình chỉ" });
            this.cboTrangThai.Location = new System.Drawing.Point(135, 157);
            this.cboTrangThai.Size = new System.Drawing.Size(180, 23);

            // lblMatKhauMoi
            this.lblMatKhauMoi.AutoSize = true;
            this.lblMatKhauMoi.Location = new System.Drawing.Point(30, 200);
            this.lblMatKhauMoi.Text = "Mật khẩu mới:";
            // txtMatKhauMoi
            this.txtMatKhauMoi.Location = new System.Drawing.Point(135, 197);
            this.txtMatKhauMoi.Size = new System.Drawing.Size(180, 23);
            this.txtMatKhauMoi.UseSystemPasswordChar = true;

            // lblGhiChuMatKhau
            this.lblGhiChuMatKhau.AutoSize = true;
            this.lblGhiChuMatKhau.Location = new System.Drawing.Point(135, 225);
            this.lblGhiChuMatKhau.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblGhiChuMatKhau.ForeColor = System.Drawing.Color.Gray;
            this.lblGhiChuMatKhau.Text = "(Bỏ trống nếu không đổi mật khẩu)";

            // btnLuu
            this.btnLuu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(196)))), ((int)(((byte)(15)))));
            this.btnLuu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLuu.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLuu.ForeColor = System.Drawing.Color.White;
            this.btnLuu.Location = new System.Drawing.Point(135, 260);
            this.btnLuu.Size = new System.Drawing.Size(180, 40);
            this.btnLuu.Text = "Lưu Thay Đổi";
            this.btnLuu.UseVisualStyleBackColor = false;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);

            // frmSuaTaiKhoan
            this.ClientSize = new System.Drawing.Size(350, 330);
            this.Controls.Add(this.btnLuu);
            this.Controls.Add(this.lblGhiChuMatKhau);
            this.Controls.Add(this.txtMatKhauMoi);
            this.Controls.Add(this.lblMatKhauMoi);
            this.Controls.Add(this.cboTrangThai);
            this.Controls.Add(this.lblTrangThai);
            this.Controls.Add(this.cboVaiTro);
            this.Controls.Add(this.lblVaiTro);
            this.Controls.Add(this.txtTenDangNhap);
            this.Controls.Add(this.lblTenDangNhap);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Thay Đổi Phân Quyền";
            this.BackColor = System.Drawing.Color.White;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblTenDangNhap;
        private System.Windows.Forms.TextBox txtTenDangNhap;
        private System.Windows.Forms.Label lblVaiTro;
        private System.Windows.Forms.ComboBox cboVaiTro;
        private System.Windows.Forms.Label lblTrangThai;
        private System.Windows.Forms.ComboBox cboTrangThai;
        private System.Windows.Forms.Label lblMatKhauMoi;
        private System.Windows.Forms.TextBox txtMatKhauMoi;
        private System.Windows.Forms.Label lblGhiChuMatKhau;
        private System.Windows.Forms.Button btnLuu;
    }
}
