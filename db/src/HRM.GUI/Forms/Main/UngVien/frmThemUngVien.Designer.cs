namespace HRM.GUI.Forms.Main.UngVien
{
    partial class frmThemUngVien
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
            lblPhongVan = new Label();
            label1 = new Label();
            txtTenUngVien = new TextBox();
            label2 = new Label();
            sqlCommand1 = new Microsoft.Data.SqlClient.SqlCommand();
            cbVitriTuyenDung = new ComboBox();
            label3 = new Label();
            txtSoDienThoai = new TextBox();
            label4 = new Label();
            txtEmail = new TextBox();
            label5 = new Label();
            txtDuongDanCV = new TextBox();
            label6 = new Label();
            cbPhanLoai = new ComboBox();
            label7 = new Label();
            cbTrangThai = new ComboBox();
            label8 = new Label();
            txtGhiChu = new RichTextBox();
            btnLuu = new Button();
            label9 = new Label();
            dateNgayNop = new DateTimePicker();
            SuspendLayout();
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(171, 27);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(174, 30);
            lblPhongVan.TabIndex = 20;
            lblPhongVan.Text = "Thêm ứng viên ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(57, 97);
            label1.Name = "label1";
            label1.Size = new Size(78, 15);
            label1.TabIndex = 22;
            label1.Text = "Tên ứng viên:";
            // 
            // txtTenUngVien
            // 
            txtTenUngVien.Location = new Point(193, 89);
            txtTenUngVien.Margin = new Padding(3, 2, 3, 2);
            txtTenUngVien.Name = "txtTenUngVien";
            txtTenUngVien.Size = new Size(261, 23);
            txtTenUngVien.TabIndex = 36;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(57, 140);
            label2.Name = "label2";
            label2.Size = new Size(98, 15);
            label2.TabIndex = 37;
            label2.Text = "Vị trí tuyển dụng:";
            // 
            // sqlCommand1
            // 
            sqlCommand1.CommandTimeout = 30;
            sqlCommand1.EnableOptimizedParameterBinding = false;
            // 
            // cbVitriTuyenDung
            // 
            cbVitriTuyenDung.FormattingEnabled = true;
            cbVitriTuyenDung.Location = new Point(193, 140);
            cbVitriTuyenDung.Name = "cbVitriTuyenDung";
            cbVitriTuyenDung.Size = new Size(261, 23);
            cbVitriTuyenDung.TabIndex = 38;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(57, 194);
            label3.Name = "label3";
            label3.Size = new Size(79, 15);
            label3.TabIndex = 39;
            label3.Text = "Số điện thoại:";
            // 
            // txtSoDienThoai
            // 
            txtSoDienThoai.Location = new Point(193, 194);
            txtSoDienThoai.Margin = new Padding(3, 2, 3, 2);
            txtSoDienThoai.Name = "txtSoDienThoai";
            txtSoDienThoai.Size = new Size(261, 23);
            txtSoDienThoai.TabIndex = 40;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(57, 239);
            label4.Name = "label4";
            label4.Size = new Size(39, 15);
            label4.TabIndex = 41;
            label4.Text = "Email:";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(193, 239);
            txtEmail.Margin = new Padding(3, 2, 3, 2);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(261, 23);
            txtEmail.TabIndex = 42;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(57, 300);
            label5.Name = "label5";
            label5.Size = new Size(87, 15);
            label5.TabIndex = 43;
            label5.Text = "Đường dẫn CV:";
            // 
            // txtDuongDanCV
            // 
            txtDuongDanCV.Location = new Point(193, 292);
            txtDuongDanCV.Margin = new Padding(3, 2, 3, 2);
            txtDuongDanCV.Name = "txtDuongDanCV";
            txtDuongDanCV.Size = new Size(261, 23);
            txtDuongDanCV.TabIndex = 44;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(57, 344);
            label6.Name = "label6";
            label6.Size = new Size(56, 15);
            label6.TabIndex = 45;
            label6.Text = "Phân loại";
            // 
            // cbPhanLoai
            // 
            cbPhanLoai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPhanLoai.FormattingEnabled = true;
            cbPhanLoai.Items.AddRange(new object[] { "Tự ứng tuyển", "Nội bộ giới thiệu", "Nền tảng tuyển dụng", "Headhunter" });
            cbPhanLoai.Location = new Point(193, 344);
            cbPhanLoai.Name = "cbPhanLoai";
            cbPhanLoai.Size = new Size(261, 23);
            cbPhanLoai.TabIndex = 46;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(57, 403);
            label7.Name = "label7";
            label7.Size = new Size(63, 15);
            label7.TabIndex = 47;
            label7.Text = "Trạng thái:";
            // 
            // cbTrangThai
            // 
            cbTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Items.AddRange(new object[] { "Chờ phỏng vấn", "Đang phỏng vấn", "Trúng tuyển", "Trượt" });
            cbTrangThai.Location = new Point(193, 403);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(261, 23);
            cbTrangThai.TabIndex = 48;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(57, 507);
            label8.Name = "label8";
            label8.Size = new Size(51, 15);
            label8.TabIndex = 49;
            label8.Text = "Ghi chú:";
            // 
            // txtGhiChu
            // 
            txtGhiChu.Location = new Point(190, 507);
            txtGhiChu.Name = "txtGhiChu";
            txtGhiChu.Size = new Size(264, 156);
            txtGhiChu.TabIndex = 50;
            txtGhiChu.Text = "";
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(171, 691);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 51;
            btnLuu.Text = "Thêm Ứng viên ";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(57, 462);
            label9.Name = "label9";
            label9.Size = new Size(62, 15);
            label9.TabIndex = 52;
            label9.Text = "Ngày nộp:";
            // 
            // dateNgayNop
            // 
            dateNgayNop.Location = new Point(193, 462);
            dateNgayNop.Name = "dateNgayNop";
            dateNgayNop.Size = new Size(200, 23);
            dateNgayNop.TabIndex = 53;
            // 
            // frmThemUngVien
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 796);
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
            Name = "frmThemUngVien";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmThemUngVien";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPhongVan;
        private Label label1;
        private TextBox txtTenUngVien;
        private Label label2;
        private Microsoft.Data.SqlClient.SqlCommand sqlCommand1;
        private ComboBox cbVitriTuyenDung;
        private Label label3;
        private TextBox txtSoDienThoai;
        private Label label4;
        private TextBox txtEmail;
        private Label label5;
        private TextBox txtDuongDanCV;
        private Label label6;
        private ComboBox cbPhanLoai;
        private Label label7;
        private ComboBox cbTrangThai;
        private Label label8;
        private RichTextBox txtGhiChu;
        private Button btnLuu;
        private Label label9;
        private DateTimePicker dateNgayNop;
    }
}