namespace HRM.GUI.Forms.Main
{
    partial class frmSuaNhanVien
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
            lblTrangThai = new Label();
            cboTrangThai = new ComboBox();
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
            lblTitle.Size = new Size(186, 30);
            lblTitle.TabIndex = 17;
            lblTitle.Text = "SỬA NHÂN VIÊN";
            // 
            // lblHoTen
            // 
            lblHoTen.AutoSize = true;
            lblHoTen.Location = new Point(30, 80);
            lblHoTen.Name = "lblHoTen";
            lblHoTen.Size = new Size(46, 15);
            lblHoTen.TabIndex = 16;
            lblHoTen.Text = "Họ tên:";
            // 
            // txtHoTen
            // 
            txtHoTen.Location = new Point(120, 77);
            txtHoTen.Name = "txtHoTen";
            txtHoTen.Size = new Size(200, 23);
            txtHoTen.TabIndex = 15;
            // 
            // lblNgaySinh
            // 
            lblNgaySinh.AutoSize = true;
            lblNgaySinh.Location = new Point(30, 120);
            lblNgaySinh.Name = "lblNgaySinh";
            lblNgaySinh.Size = new Size(63, 15);
            lblNgaySinh.TabIndex = 14;
            lblNgaySinh.Text = "Ngày sinh:";
            // 
            // dtpNgaySinh
            // 
            dtpNgaySinh.Format = DateTimePickerFormat.Short;
            dtpNgaySinh.Location = new Point(120, 117);
            dtpNgaySinh.Name = "dtpNgaySinh";
            dtpNgaySinh.Size = new Size(200, 23);
            dtpNgaySinh.TabIndex = 13;
            // 
            // lblGioiTinh
            // 
            lblGioiTinh.AutoSize = true;
            lblGioiTinh.Location = new Point(30, 160);
            lblGioiTinh.Name = "lblGioiTinh";
            lblGioiTinh.Size = new Size(55, 15);
            lblGioiTinh.TabIndex = 12;
            lblGioiTinh.Text = "Giới tính:";
            // 
            // cboGioiTinh
            // 
            cboGioiTinh.DropDownStyle = ComboBoxStyle.DropDownList;
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGioiTinh.Location = new Point(120, 157);
            cboGioiTinh.Name = "cboGioiTinh";
            cboGioiTinh.Size = new Size(200, 23);
            cboGioiTinh.TabIndex = 11;
            // 
            // lblCCCD
            // 
            lblCCCD.AutoSize = true;
            lblCCCD.Location = new Point(30, 200);
            lblCCCD.Name = "lblCCCD";
            lblCCCD.Size = new Size(58, 15);
            lblCCCD.TabIndex = 10;
            lblCCCD.Text = "Số CCCD:";
            // 
            // txtCCCD
            // 
            txtCCCD.Location = new Point(120, 197);
            txtCCCD.Name = "txtCCCD";
            txtCCCD.Size = new Size(200, 23);
            txtCCCD.TabIndex = 9;
            // 
            // lblDienThoai
            // 
            lblDienThoai.AutoSize = true;
            lblDienThoai.Location = new Point(30, 240);
            lblDienThoai.Name = "lblDienThoai";
            lblDienThoai.Size = new Size(64, 15);
            lblDienThoai.TabIndex = 8;
            lblDienThoai.Text = "Điện thoại:";
            // 
            // txtSoDienThoai
            // 
            txtSoDienThoai.Location = new Point(120, 237);
            txtSoDienThoai.Name = "txtSoDienThoai";
            txtSoDienThoai.Size = new Size(200, 23);
            txtSoDienThoai.TabIndex = 7;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(30, 280);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(39, 15);
            lblEmail.TabIndex = 6;
            lblEmail.Text = "Email:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(120, 277);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(200, 23);
            txtEmail.TabIndex = 5;
            // 
            // lblNgayVaoLam
            // 
            lblNgayVaoLam.AutoSize = true;
            lblNgayVaoLam.Location = new Point(30, 320);
            lblNgayVaoLam.Name = "lblNgayVaoLam";
            lblNgayVaoLam.Size = new Size(52, 15);
            lblNgayVaoLam.TabIndex = 4;
            lblNgayVaoLam.Text = "Vào làm:";
            // 
            // dtpNgayVaoLam
            // 
            dtpNgayVaoLam.Format = DateTimePickerFormat.Short;
            dtpNgayVaoLam.Location = new Point(120, 317);
            dtpNgayVaoLam.Name = "dtpNgayVaoLam";
            dtpNgayVaoLam.Size = new Size(200, 23);
            dtpNgayVaoLam.TabIndex = 3;
            // 
            // lblTrangThai
            // 
            lblTrangThai.AutoSize = true;
            lblTrangThai.Location = new Point(30, 360);
            lblTrangThai.Name = "lblTrangThai";
            lblTrangThai.Size = new Size(63, 15);
            lblTrangThai.TabIndex = 2;
            lblTrangThai.Text = "Trạng thái:";
            // 
            // cboTrangThai
            // 
            cboTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTrangThai.Items.AddRange(new object[] { "Đang làm việc", "Đã nghỉ việc", "Đình chỉ" });
            cboTrangThai.Location = new Point(120, 357);
            cboTrangThai.Name = "cboTrangThai";
            cboTrangThai.Size = new Size(200, 23);
            cboTrangThai.TabIndex = 1;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(120, 410);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 0;
            btnLuu.Text = "Lưu Thay Đổi";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // frmSuaNhanVien
            // 
            BackColor = Color.White;
            ClientSize = new Size(380, 480);
            Controls.Add(btnLuu);
            Controls.Add(cboTrangThai);
            Controls.Add(lblTrangThai);
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
            Name = "frmSuaNhanVien";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sửa Nhân Viên";
            Load += frmSuaNhanVien_Load;
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
        private System.Windows.Forms.Label lblTrangThai;
        private System.Windows.Forms.ComboBox cboTrangThai;
        private System.Windows.Forms.Button btnLuu;
    }
}
