namespace HRM.GUI.Forms.Main
{
    partial class frmThemNhanVien
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
            lblTitle = new Label();
            lblHoTen = new Label();
            txtHoTen = new TextBox();
            lblNgaySinh = new Label();
            dtpNgaySinh = new DateTimePicker();
            lblGioiTinh = new Label();
            cboGioiTinh = new ComboBox();
            lblCCCD = new Label();
            txtCCCD = new TextBox();
            lblDienThoai = new Label();
            txtSoDienThoai = new TextBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            lblNgayVaoLam = new Label();
            dtpNgayVaoLam = new DateTimePicker();
            btnLuu = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(41, 128, 185);
            lblTitle.Location = new Point(80, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(315, 37);
            lblTitle.TabIndex = 15;
            lblTitle.Text = "THÊM NHÂN VIÊN MỚI";
            // 
            // lblHoTen
            // 
            lblHoTen.AutoSize = true;
            lblHoTen.Location = new Point(30, 80);
            lblHoTen.Name = "lblHoTen";
            lblHoTen.Size = new Size(57, 20);
            lblHoTen.TabIndex = 14;
            lblHoTen.Text = "Họ tên:";
            // 
            // txtHoTen
            // 
            txtHoTen.Location = new Point(120, 77);
            txtHoTen.Name = "txtHoTen";
            txtHoTen.Size = new Size(200, 27);
            txtHoTen.TabIndex = 13;
            txtHoTen.TextChanged += txtHoTen_TextChanged;
            // 
            // lblNgaySinh
            // 
            lblNgaySinh.AutoSize = true;
            lblNgaySinh.Location = new Point(30, 120);
            lblNgaySinh.Name = "lblNgaySinh";
            lblNgaySinh.Size = new Size(77, 20);
            lblNgaySinh.TabIndex = 12;
            lblNgaySinh.Text = "Ngày sinh:";
            // 
            // dtpNgaySinh
            // 
            dtpNgaySinh.Format = DateTimePickerFormat.Short;
            dtpNgaySinh.Location = new Point(120, 117);
            dtpNgaySinh.Name = "dtpNgaySinh";
            dtpNgaySinh.Size = new Size(200, 27);
            dtpNgaySinh.TabIndex = 11;
            // 
            // lblGioiTinh
            // 
            lblGioiTinh.AutoSize = true;
            lblGioiTinh.Location = new Point(30, 160);
            lblGioiTinh.Name = "lblGioiTinh";
            lblGioiTinh.Size = new Size(68, 20);
            lblGioiTinh.TabIndex = 10;
            lblGioiTinh.Text = "Giới tính:";
            // 
            // cboGioiTinh
            // 
            cboGioiTinh.DropDownStyle = ComboBoxStyle.DropDownList;
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGioiTinh.Location = new Point(120, 157);
            cboGioiTinh.Name = "cboGioiTinh";
            cboGioiTinh.Size = new Size(200, 28);
            cboGioiTinh.TabIndex = 9;
            // 
            // lblCCCD
            // 
            lblCCCD.AutoSize = true;
            lblCCCD.Location = new Point(30, 200);
            lblCCCD.Name = "lblCCCD";
            lblCCCD.Size = new Size(71, 20);
            lblCCCD.TabIndex = 8;
            lblCCCD.Text = "Số CCCD:";
            // 
            // txtCCCD
            // 
            txtCCCD.Location = new Point(120, 197);
            txtCCCD.Name = "txtCCCD";
            txtCCCD.Size = new Size(200, 27);
            txtCCCD.TabIndex = 7;
            // 
            // lblDienThoai
            // 
            lblDienThoai.AutoSize = true;
            lblDienThoai.Location = new Point(30, 240);
            lblDienThoai.Name = "lblDienThoai";
            lblDienThoai.Size = new Size(81, 20);
            lblDienThoai.TabIndex = 6;
            lblDienThoai.Text = "Điện thoại:";
            // 
            // txtSoDienThoai
            // 
            txtSoDienThoai.Location = new Point(120, 237);
            txtSoDienThoai.Name = "txtSoDienThoai";
            txtSoDienThoai.Size = new Size(200, 27);
            txtSoDienThoai.TabIndex = 5;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(30, 280);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(49, 20);
            lblEmail.TabIndex = 4;
            lblEmail.Text = "Email:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(120, 277);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(200, 27);
            txtEmail.TabIndex = 3;
            // 
            // lblNgayVaoLam
            // 
            lblNgayVaoLam.AutoSize = true;
            lblNgayVaoLam.Location = new Point(30, 320);
            lblNgayVaoLam.Name = "lblNgayVaoLam";
            lblNgayVaoLam.Size = new Size(66, 20);
            lblNgayVaoLam.TabIndex = 2;
            lblNgayVaoLam.Text = "Vào làm:";
            // 
            // dtpNgayVaoLam
            // 
            dtpNgayVaoLam.Format = DateTimePickerFormat.Short;
            dtpNgayVaoLam.Location = new Point(120, 317);
            dtpNgayVaoLam.Name = "dtpNgayVaoLam";
            dtpNgayVaoLam.Size = new Size(200, 27);
            dtpNgayVaoLam.TabIndex = 1;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(120, 370);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 0;
            btnLuu.Text = "Thêm Nhân Viên";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // frmThemNhanVien
            // 
            BackColor = Color.White;
            ClientSize = new Size(429, 450);
            Controls.Add(btnLuu);
            Controls.Add(dtpNgayVaoLam);
            Controls.Add(lblNgayVaoLam);
            Controls.Add(txtEmail);
            Controls.Add(lblEmail);
            Controls.Add(txtSoDienThoai);
            Controls.Add(lblDienThoai);
            Controls.Add(txtCCCD);
            Controls.Add(lblCCCD);
            Controls.Add(cboGioiTinh);
            Controls.Add(lblGioiTinh);
            Controls.Add(dtpNgaySinh);
            Controls.Add(lblNgaySinh);
            Controls.Add(txtHoTen);
            Controls.Add(lblHoTen);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "frmThemNhanVien";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Thêm Nhân Viên";
            Load += frmThemNhanVien_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblHoTen;
        private System.Windows.Forms.TextBox txtHoTen;
        private System.Windows.Forms.Label lblNgaySinh;
        private System.Windows.Forms.DateTimePicker dtpNgaySinh;
        private System.Windows.Forms.Label lblGioiTinh;
        private System.Windows.Forms.ComboBox cboGioiTinh;
        private System.Windows.Forms.Label lblCCCD;
        private System.Windows.Forms.TextBox txtCCCD;
        private System.Windows.Forms.Label lblDienThoai;
        private System.Windows.Forms.TextBox txtSoDienThoai;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label lblNgayVaoLam;
        private System.Windows.Forms.DateTimePicker dtpNgayVaoLam;
        private System.Windows.Forms.Button btnLuu;
    }
}
