namespace HRM.GUI.Forms.Main.UngVien
{
    partial class frmSuaUngVien
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dateNgayNop = new DateTimePicker();
            label9 = new Label();
            btnLuu = new Button();
            txtGhiChu = new RichTextBox();
            label8 = new Label();
            cbTrangThai = new ComboBox();
            label7 = new Label();
            cbPhanLoai = new ComboBox();
            label6 = new Label();
            txtDuongDanCV = new TextBox();
            label5 = new Label();
            txtEmail = new TextBox();
            label4 = new Label();
            txtSoDienThoai = new TextBox();
            label3 = new Label();
            cbVitriTuyenDung = new ComboBox();
            label2 = new Label();
            txtTenUngVien = new TextBox();
            label1 = new Label();
            lblPhongVan = new Label();
            SuspendLayout();
            // 
            // dateNgayNop
            // 
            dateNgayNop.Location = new Point(235, 474);
            dateNgayNop.Name = "dateNgayNop";
            dateNgayNop.Size = new Size(200, 23);
            dateNgayNop.TabIndex = 73;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(99, 474);
            label9.Name = "label9";
            label9.Size = new Size(62, 15);
            label9.TabIndex = 72;
            label9.Text = "Ngày nộp:";
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(213, 703);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 71;
            btnLuu.Text = "Lưu thay đổi";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // txtGhiChu
            // 
            txtGhiChu.Location = new Point(232, 519);
            txtGhiChu.Name = "txtGhiChu";
            txtGhiChu.Size = new Size(264, 156);
            txtGhiChu.TabIndex = 70;
            txtGhiChu.Text = "";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(99, 519);
            label8.Name = "label8";
            label8.Size = new Size(51, 15);
            label8.TabIndex = 69;
            label8.Text = "Ghi chú:";
            // 
            // cbTrangThai
            // 
            cbTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Items.AddRange(new object[] { "Mới nộp", "Chờ phỏng vấn", "Trúng tuyển", "Trượt" });
            cbTrangThai.Location = new Point(235, 415);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(261, 23);
            cbTrangThai.TabIndex = 68;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(99, 415);
            label7.Name = "label7";
            label7.Size = new Size(63, 15);
            label7.TabIndex = 67;
            label7.Text = "Trạng thái:";
            // 
            // cbPhanLoai
            // 
            cbPhanLoai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPhanLoai.FormattingEnabled = true;
            cbPhanLoai.Items.AddRange(new object[] { "Tự ứng tuyển", "Nội bộ giới thiệu", "Nền tảng tuyển dụng", "Headhunter" });
            cbPhanLoai.Location = new Point(235, 356);
            cbPhanLoai.Name = "cbPhanLoai";
            cbPhanLoai.Size = new Size(261, 23);
            cbPhanLoai.TabIndex = 66;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(99, 356);
            label6.Name = "label6";
            label6.Size = new Size(56, 15);
            label6.TabIndex = 65;
            label6.Text = "Phân loại";
            // 
            // txtDuongDanCV
            // 
            txtDuongDanCV.Location = new Point(235, 304);
            txtDuongDanCV.Margin = new Padding(3, 2, 3, 2);
            txtDuongDanCV.Name = "txtDuongDanCV";
            txtDuongDanCV.Size = new Size(261, 23);
            txtDuongDanCV.TabIndex = 64;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(99, 312);
            label5.Name = "label5";
            label5.Size = new Size(87, 15);
            label5.TabIndex = 63;
            label5.Text = "Đường dẫn CV:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(235, 251);
            txtEmail.Margin = new Padding(3, 2, 3, 2);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(261, 23);
            txtEmail.TabIndex = 62;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(99, 251);
            label4.Name = "label4";
            label4.Size = new Size(39, 15);
            label4.TabIndex = 61;
            label4.Text = "Email:";
            // 
            // txtSoDienThoai
            // 
            txtSoDienThoai.Location = new Point(235, 206);
            txtSoDienThoai.Margin = new Padding(3, 2, 3, 2);
            txtSoDienThoai.Name = "txtSoDienThoai";
            txtSoDienThoai.Size = new Size(261, 23);
            txtSoDienThoai.TabIndex = 60;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(99, 206);
            label3.Name = "label3";
            label3.Size = new Size(79, 15);
            label3.TabIndex = 59;
            label3.Text = "Số điện thoại:";
            // 
            // cbVitriTuyenDung
            // 
            cbVitriTuyenDung.FormattingEnabled = true;
            cbVitriTuyenDung.Location = new Point(235, 152);
            cbVitriTuyenDung.Name = "cbVitriTuyenDung";
            cbVitriTuyenDung.Size = new Size(261, 23);
            cbVitriTuyenDung.TabIndex = 58;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(99, 152);
            label2.Name = "label2";
            label2.Size = new Size(98, 15);
            label2.TabIndex = 57;
            label2.Text = "Vị trí tuyển dụng:";
            // 
            // txtTenUngVien
            // 
            txtTenUngVien.Location = new Point(235, 101);
            txtTenUngVien.Margin = new Padding(3, 2, 3, 2);
            txtTenUngVien.Name = "txtTenUngVien";
            txtTenUngVien.Size = new Size(261, 23);
            txtTenUngVien.TabIndex = 56;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(99, 109);
            label1.Name = "label1";
            label1.Size = new Size(78, 15);
            label1.TabIndex = 55;
            label1.Text = "Tên ứng viên:";
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(213, 39);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(149, 30);
            lblPhongVan.TabIndex = 54;
            lblPhongVan.Text = "Sửa ứng viên";
            // 
            // frmSuaUngVien
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(595, 782);
            Controls.Add(dateNgayNop);
            Controls.Add(label9);
            Controls.Add(btnLuu);
            Controls.Add(txtGhiChu);
            Controls.Add(label8);
            Controls.Add(cbTrangThai);
            Controls.Add(label7);
            Controls.Add(cbPhanLoai);
            Controls.Add(label6);
            Controls.Add(txtDuongDanCV);
            Controls.Add(label5);
            Controls.Add(txtEmail);
            Controls.Add(label4);
            Controls.Add(txtSoDienThoai);
            Controls.Add(label3);
            Controls.Add(cbVitriTuyenDung);
            Controls.Add(label2);
            Controls.Add(txtTenUngVien);
            Controls.Add(label1);
            Controls.Add(lblPhongVan);
            Name = "frmSuaUngVien";
            Text = "frmSuaUngVien";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DateTimePicker dateNgayNop;
        private Label label9;
        private Button btnLuu;
        private RichTextBox txtGhiChu;
        private Label label8;
        private ComboBox cbTrangThai;
        private Label label7;
        private ComboBox cbPhanLoai;
        private Label label6;
        private TextBox txtDuongDanCV;
        private Label label5;
        private TextBox txtEmail;
        private Label label4;
        private TextBox txtSoDienThoai;
        private Label label3;
        private ComboBox cbVitriTuyenDung;
        private Label label2;
        private TextBox txtTenUngVien;
        private Label label1;
        private Label lblPhongVan;
    }
}