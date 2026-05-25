namespace HRM.GUI.Forms.Main.PhongVan
{
    partial class frmSuaPhongVan
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
            lblTenUngVien = new Label();
            label9 = new Label();
            btnLuu = new Button();
            cbMaUngVien = new ComboBox();
            txtNhanXet = new RichTextBox();
            cbTrangThai = new ComboBox();
            cbKetQua = new ComboBox();
            cbNguoiPV = new ComboBox();
            txtDiaDiem = new TextBox();
            dtpNgayPhongvan = new DateTimePicker();
            cbVongPhongVan = new ComboBox();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            lblHoTen = new Label();
            lblPhongVan = new Label();
            SuspendLayout();
            // 
            // lblTenUngVien
            // 
            lblTenUngVien.AutoSize = true;
            lblTenUngVien.Font = new Font("Times New Roman", 9.75F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblTenUngVien.ForeColor = Color.Red;
            lblTenUngVien.Location = new Point(207, 183);
            lblTenUngVien.Name = "lblTenUngVien";
            lblTenUngVien.Size = new Size(0, 19);
            lblTenUngVien.TabIndex = 62;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(62, 183);
            label9.Name = "label9";
            label9.Size = new Size(93, 20);
            label9.TabIndex = 61;
            label9.Text = "Tên ứng viên";
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(145, 681);
            btnLuu.Margin = new Padding(3, 4, 3, 4);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(229, 53);
            btnLuu.TabIndex = 60;
            btnLuu.Text = "Lưu thay đổi";
            btnLuu.UseVisualStyleBackColor = false;
            // 
            // cbMaUngVien
            // 
            cbMaUngVien.FormattingEnabled = true;
            cbMaUngVien.Location = new Point(192, 132);
            cbMaUngVien.Margin = new Padding(3, 4, 3, 4);
            cbMaUngVien.Name = "cbMaUngVien";
            cbMaUngVien.Size = new Size(207, 28);
            cbMaUngVien.TabIndex = 59;
            // 
            // txtNhanXet
            // 
            txtNhanXet.Enabled = false;
            txtNhanXet.Location = new Point(192, 553);
            txtNhanXet.Name = "txtNhanXet";
            txtNhanXet.Size = new Size(241, 120);
            txtNhanXet.TabIndex = 58;
            txtNhanXet.Text = "";
            // 
            // cbTrangThai
            // 
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Location = new Point(192, 499);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(207, 28);
            cbTrangThai.TabIndex = 57;
            // 
            // cbKetQua
            // 
            cbKetQua.Enabled = false;
            cbKetQua.FormattingEnabled = true;
            cbKetQua.Location = new Point(192, 447);
            cbKetQua.Name = "cbKetQua";
            cbKetQua.Size = new Size(207, 28);
            cbKetQua.TabIndex = 56;
            // 
            // cbNguoiPV
            // 
            cbNguoiPV.FormattingEnabled = true;
            cbNguoiPV.Location = new Point(192, 392);
            cbNguoiPV.Name = "cbNguoiPV";
            cbNguoiPV.Size = new Size(207, 28);
            cbNguoiPV.TabIndex = 55;
            // 
            // txtDiaDiem
            // 
            txtDiaDiem.Location = new Point(192, 340);
            txtDiaDiem.Name = "txtDiaDiem";
            txtDiaDiem.Size = new Size(207, 27);
            txtDiaDiem.TabIndex = 54;
            // 
            // dtpNgayPhongvan
            // 
            dtpNgayPhongvan.Format = DateTimePickerFormat.Short;
            dtpNgayPhongvan.Location = new Point(192, 291);
            dtpNgayPhongvan.Name = "dtpNgayPhongvan";
            dtpNgayPhongvan.Size = new Size(207, 27);
            dtpNgayPhongvan.TabIndex = 53;
            // 
            // cbVongPhongVan
            // 
            cbVongPhongVan.FormattingEnabled = true;
            cbVongPhongVan.Location = new Point(192, 233);
            cbVongPhongVan.Name = "cbVongPhongVan";
            cbVongPhongVan.Size = new Size(207, 28);
            cbVongPhongVan.TabIndex = 52;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(56, 553);
            label8.Name = "label8";
            label8.Size = new Size(71, 20);
            label8.TabIndex = 51;
            label8.Text = "Nhận xét:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(56, 503);
            label7.Name = "label7";
            label7.Size = new Size(78, 20);
            label7.TabIndex = 50;
            label7.Text = "Trạng thái:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(56, 457);
            label6.Name = "label6";
            label6.Size = new Size(63, 20);
            label6.TabIndex = 49;
            label6.Text = "Kết quả:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(56, 396);
            label5.Name = "label5";
            label5.Size = new Size(128, 20);
            label5.TabIndex = 48;
            label5.Text = "Người phỏng vấn:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(56, 344);
            label4.Name = "label4";
            label4.Size = new Size(73, 20);
            label4.TabIndex = 47;
            label4.Text = "Địa điểm:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(56, 299);
            label3.Name = "label3";
            label3.Size = new Size(121, 20);
            label3.TabIndex = 46;
            label3.Text = "Ngày phỏng vấn:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(56, 237);
            label2.Name = "label2";
            label2.Size = new Size(120, 20);
            label2.TabIndex = 45;
            label2.Text = "Vòng phỏng vấn:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(56, 136);
            label1.Name = "label1";
            label1.Size = new Size(94, 20);
            label1.TabIndex = 44;
            label1.Text = "Mã ứng viên:";
            // 
            // lblHoTen
            // 
            lblHoTen.AutoSize = true;
            lblHoTen.Location = new Point(56, 136);
            lblHoTen.Name = "lblHoTen";
            lblHoTen.Size = new Size(0, 20);
            lblHoTen.TabIndex = 43;
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(157, 71);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(208, 37);
            lblPhongVan.TabIndex = 42;
            lblPhongVan.Text = "Sửa phỏng vấn";
            // 
            // frmSuaPhongVan
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(490, 807);
            Controls.Add(lblTenUngVien);
            Controls.Add(label9);
            Controls.Add(btnLuu);
            Controls.Add(cbMaUngVien);
            Controls.Add(txtNhanXet);
            Controls.Add(cbTrangThai);
            Controls.Add(cbKetQua);
            Controls.Add(cbNguoiPV);
            Controls.Add(txtDiaDiem);
            Controls.Add(dtpNgayPhongvan);
            Controls.Add(cbVongPhongVan);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblHoTen);
            Controls.Add(lblPhongVan);
            Margin = new Padding(3, 4, 3, 4);
            Name = "frmSuaPhongVan";
            Text = "frmSuaPhongVan";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTenUngVien;
        private Label label9;
        private Button btnLuu;
        private ComboBox cbMaUngVien;
        private RichTextBox txtNhanXet;
        private ComboBox cbTrangThai;
        private ComboBox cbKetQua;
        private ComboBox cbNguoiPV;
        private TextBox txtDiaDiem;
        private DateTimePicker dtpNgayPhongvan;
        private ComboBox cbVongPhongVan;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label lblHoTen;
        private Label lblPhongVan;
    }
}