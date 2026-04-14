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
            lblTitle = new System.Windows.Forms.Label();
            lblHoTen = new System.Windows.Forms.Label();
            txtHoTen = new System.Windows.Forms.TextBox();
            lblNgaySinh = new System.Windows.Forms.Label();
            dtpNgaySinh = new System.Windows.Forms.DateTimePicker();
            lblGioiTinh = new System.Windows.Forms.Label();
            cboGioiTinh = new System.Windows.Forms.ComboBox();
            lblCCCD = new System.Windows.Forms.Label();
            txtCCCD = new System.Windows.Forms.TextBox();
            lblDienThoai = new System.Windows.Forms.Label();
            txtSoDienThoai = new System.Windows.Forms.TextBox();
            lblEmail = new System.Windows.Forms.Label();
            txtEmail = new System.Windows.Forms.TextBox();
            lblPhongBan = new System.Windows.Forms.Label();
            cboPhongBan = new System.Windows.Forms.ComboBox();
            lblChucVu = new System.Windows.Forms.Label();
            cboChucVu = new System.Windows.Forms.ComboBox();
            lblMucLuong = new System.Windows.Forms.Label();
            txtMucLuong = new System.Windows.Forms.TextBox();
            lblNgayVaoLam = new System.Windows.Forms.Label();
            dtpNgayVaoLam = new System.Windows.Forms.DateTimePicker();
            btnLuu = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(41, 128, 185);
            lblTitle.Location = new System.Drawing.Point(80, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(315, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "THÊM NHÂN VIÊN MỚI";
            // 
            // lblHoTen
            // 
            lblHoTen.AutoSize = true;
            lblHoTen.Location = new System.Drawing.Point(30, 80);
            lblHoTen.Name = "lblHoTen";
            lblHoTen.Size = new System.Drawing.Size(57, 20);
            lblHoTen.TabIndex = 1;
            lblHoTen.Text = "Họ tên:";
            // 
            // txtHoTen
            // 
            txtHoTen.Location = new System.Drawing.Point(130, 77);
            txtHoTen.Name = "txtHoTen";
            txtHoTen.Size = new System.Drawing.Size(220, 27);
            txtHoTen.TabIndex = 2;
            // 
            // lblNgaySinh
            // 
            lblNgaySinh.AutoSize = true;
            lblNgaySinh.Location = new System.Drawing.Point(30, 120);
            lblNgaySinh.Name = "lblNgaySinh";
            lblNgaySinh.Size = new System.Drawing.Size(77, 20);
            lblNgaySinh.TabIndex = 3;
            lblNgaySinh.Text = "Ngày sinh:";
            // 
            // dtpNgaySinh
            // 
            dtpNgaySinh.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpNgaySinh.Location = new System.Drawing.Point(130, 117);
            dtpNgaySinh.Name = "dtpNgaySinh";
            dtpNgaySinh.Size = new System.Drawing.Size(220, 27);
            dtpNgaySinh.TabIndex = 4;
            // 
            // lblGioiTinh
            // 
            lblGioiTinh.AutoSize = true;
            lblGioiTinh.Location = new System.Drawing.Point(30, 160);
            lblGioiTinh.Name = "lblGioiTinh";
            lblGioiTinh.Size = new System.Drawing.Size(68, 20);
            lblGioiTinh.TabIndex = 5;
            lblGioiTinh.Text = "Giới tính:";
            // 
            // cboGioiTinh
            // 
            cboGioiTinh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGioiTinh.Location = new System.Drawing.Point(130, 157);
            cboGioiTinh.Name = "cboGioiTinh";
            cboGioiTinh.Size = new System.Drawing.Size(220, 28);
            cboGioiTinh.TabIndex = 6;
            // 
            // lblCCCD
            // 
            lblCCCD.AutoSize = true;
            lblCCCD.Location = new System.Drawing.Point(30, 200);
            lblCCCD.Name = "lblCCCD";
            lblCCCD.Size = new System.Drawing.Size(71, 20);
            lblCCCD.TabIndex = 7;
            lblCCCD.Text = "Số CCCD:";
            // 
            // txtCCCD
            // 
            txtCCCD.Location = new System.Drawing.Point(130, 197);
            txtCCCD.Name = "txtCCCD";
            txtCCCD.Size = new System.Drawing.Size(220, 27);
            txtCCCD.TabIndex = 8;
            // 
            // lblDienThoai
            // 
            lblDienThoai.AutoSize = true;
            lblDienThoai.Location = new System.Drawing.Point(30, 240);
            lblDienThoai.Name = "lblDienThoai";
            lblDienThoai.Size = new System.Drawing.Size(81, 20);
            lblDienThoai.TabIndex = 9;
            lblDienThoai.Text = "Điện thoại:";
            // 
            // txtSoDienThoai
            // 
            txtSoDienThoai.Location = new System.Drawing.Point(130, 237);
            txtSoDienThoai.Name = "txtSoDienThoai";
            txtSoDienThoai.Size = new System.Drawing.Size(220, 27);
            txtSoDienThoai.TabIndex = 10;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new System.Drawing.Point(30, 280);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new System.Drawing.Size(49, 20);
            lblEmail.TabIndex = 11;
            lblEmail.Text = "Email:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new System.Drawing.Point(130, 277);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new System.Drawing.Size(220, 27);
            txtEmail.TabIndex = 12;
            // 
            // lblPhongBan
            // 
            lblPhongBan.AutoSize = true;
            lblPhongBan.Location = new System.Drawing.Point(30, 320);
            lblPhongBan.Name = "lblPhongBan";
            lblPhongBan.Size = new System.Drawing.Size(78, 20);
            lblPhongBan.TabIndex = 13;
            lblPhongBan.Text = "Phòng ban:";
            // 
            // cboPhongBan
            // 
            cboPhongBan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboPhongBan.Location = new System.Drawing.Point(130, 317);
            cboPhongBan.Name = "cboPhongBan";
            cboPhongBan.Size = new System.Drawing.Size(220, 28);
            cboPhongBan.TabIndex = 14;
            // 
            // lblChucVu
            // 
            lblChucVu.AutoSize = true;
            lblChucVu.Location = new System.Drawing.Point(30, 360);
            lblChucVu.Name = "lblChucVu";
            lblChucVu.Size = new System.Drawing.Size(65, 20);
            lblChucVu.TabIndex = 15;
            lblChucVu.Text = "Chức vụ:";
            // 
            // cboChucVu
            // 
            cboChucVu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboChucVu.Location = new System.Drawing.Point(130, 357);
            cboChucVu.Name = "cboChucVu";
            cboChucVu.Size = new System.Drawing.Size(220, 28);
            cboChucVu.TabIndex = 16;
            // 
            // lblMucLuong
            // 
            lblMucLuong.AutoSize = true;
            lblMucLuong.Location = new System.Drawing.Point(30, 400);
            lblMucLuong.Name = "lblMucLuong";
            lblMucLuong.Size = new System.Drawing.Size(82, 20);
            lblMucLuong.TabIndex = 17;
            lblMucLuong.Text = "Mức lương:";
            // 
            // txtMucLuong
            // 
            txtMucLuong.Location = new System.Drawing.Point(130, 397);
            txtMucLuong.Name = "txtMucLuong";
            txtMucLuong.Size = new System.Drawing.Size(220, 27);
            txtMucLuong.TabIndex = 18;
            // 
            // lblNgayVaoLam
            // 
            lblNgayVaoLam.AutoSize = true;
            lblNgayVaoLam.Location = new System.Drawing.Point(30, 440);
            lblNgayVaoLam.Name = "lblNgayVaoLam";
            lblNgayVaoLam.Size = new System.Drawing.Size(66, 20);
            lblNgayVaoLam.TabIndex = 19;
            lblNgayVaoLam.Text = "Vào làm:";
            // 
            // dtpNgayVaoLam
            // 
            dtpNgayVaoLam.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpNgayVaoLam.Location = new System.Drawing.Point(130, 437);
            dtpNgayVaoLam.Name = "dtpNgayVaoLam";
            dtpNgayVaoLam.Size = new System.Drawing.Size(220, 27);
            dtpNgayVaoLam.TabIndex = 20;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnLuu.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnLuu.ForeColor = System.Drawing.Color.White;
            btnLuu.Location = new System.Drawing.Point(130, 490);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new System.Drawing.Size(220, 40);
            btnLuu.TabIndex = 21;
            btnLuu.Text = "Thêm Nhân Viên";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += new System.EventHandler(btnLuu_Click);
            // 
            // frmThemNhanVien
            // 
            BackColor = System.Drawing.Color.White;
            ClientSize = new System.Drawing.Size(400, 560);
            Controls.Add(btnLuu);
            Controls.Add(dtpNgayVaoLam);
            Controls.Add(lblNgayVaoLam);
            Controls.Add(txtMucLuong);
            Controls.Add(lblMucLuong);
            Controls.Add(cboChucVu);
            Controls.Add(lblChucVu);
            Controls.Add(cboPhongBan);
            Controls.Add(lblPhongBan);
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
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "frmThemNhanVien";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Thêm Nhân Viên";
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
        private System.Windows.Forms.Label lblPhongBan;
        private System.Windows.Forms.ComboBox cboPhongBan;
        private System.Windows.Forms.Label lblChucVu;
        private System.Windows.Forms.ComboBox cboChucVu;
        private System.Windows.Forms.Label lblMucLuong;
        private System.Windows.Forms.TextBox txtMucLuong;
        private System.Windows.Forms.Label lblNgayVaoLam;
        private System.Windows.Forms.DateTimePicker dtpNgayVaoLam;
        private System.Windows.Forms.Button btnLuu;
    }
}
