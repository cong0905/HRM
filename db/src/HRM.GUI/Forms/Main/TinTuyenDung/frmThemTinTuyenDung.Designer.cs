namespace HRM.GUI.Forms.Main.TinTuyenDung
{
    partial class frmThemTinTuyenDung
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
            label2 = new Label();
            label3 = new Label();
            txtMoTaCongViec = new RichTextBox();
            label4 = new Label();
            txtYeuCau = new RichTextBox();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            cbPhongBan = new ComboBox();
            dateNhanHoSo = new DateTimePicker();
            txtViTriTuyenDung = new TextBox();
            txtDiaDiem = new TextBox();
            btnLuu = new Button();
            txtLuongMin = new NumericUpDown();
            txtLuongMax = new NumericUpDown();
            txtSoLuongCanTuyen = new NumericUpDown();
            cbTrangThai = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)txtLuongMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtSoLuongCanTuyen).BeginInit();
            SuspendLayout();
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(199, 14);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(236, 30);
            lblPhongVan.TabIndex = 19;
            lblPhongVan.Text = "Thêm tin tuyển dụng ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(61, 67);
            label1.Name = "label1";
            label1.Size = new Size(98, 15);
            label1.TabIndex = 21;
            label1.Text = "Vị trí tuyển dụng:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(61, 100);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 22;
            label2.Text = "Phòng ban:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(61, 136);
            label3.Name = "label3";
            label3.Size = new Size(95, 15);
            label3.TabIndex = 23;
            label3.Text = "Mô tả công việc ";
            // 
            // txtMoTaCongViec
            // 
            txtMoTaCongViec.Location = new Point(186, 134);
            txtMoTaCongViec.Margin = new Padding(3, 2, 3, 2);
            txtMoTaCongViec.Name = "txtMoTaCongViec";
            txtMoTaCongViec.Size = new Size(339, 91);
            txtMoTaCongViec.TabIndex = 24;
            txtMoTaCongViec.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(61, 240);
            label4.Name = "label4";
            label4.Size = new Size(97, 15);
            label4.TabIndex = 25;
            label4.Text = "Yêu cầu ứng viên";
            // 
            // txtYeuCau
            // 
            txtYeuCau.Location = new Point(186, 238);
            txtYeuCau.Margin = new Padding(3, 2, 3, 2);
            txtYeuCau.Name = "txtYeuCau";
            txtYeuCau.Size = new Size(339, 91);
            txtYeuCau.TabIndex = 26;
            txtYeuCau.Text = "";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(61, 340);
            label5.Name = "label5";
            label5.Size = new Size(109, 15);
            label5.TabIndex = 27;
            label5.Text = "Số lượng cần tuyển";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(61, 379);
            label6.Name = "label6";
            label6.Size = new Size(112, 15);
            label6.TabIndex = 29;
            label6.Text = "Mức lương tối thiểu";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(61, 418);
            label7.Name = "label7";
            label7.Size = new Size(98, 15);
            label7.TabIndex = 30;
            label7.Text = "Mức lương tối đa";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(61, 454);
            label8.Name = "label8";
            label8.Size = new Size(116, 15);
            label8.TabIndex = 31;
            label8.Text = "Thời hạn nhận hồ sơ";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(61, 491);
            label9.Name = "label9";
            label9.Size = new Size(101, 15);
            label9.TabIndex = 32;
            label9.Text = "Địa điểm làm việc";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(61, 525);
            label10.Name = "label10";
            label10.Size = new Size(60, 15);
            label10.TabIndex = 33;
            label10.Text = "Trạng thái";
            // 
            // cbPhongBan
            // 
            cbPhongBan.FormattingEnabled = true;
            cbPhongBan.Location = new Point(186, 100);
            cbPhongBan.Margin = new Padding(3, 2, 3, 2);
            cbPhongBan.Name = "cbPhongBan";
            cbPhongBan.Size = new Size(339, 23);
            cbPhongBan.TabIndex = 36;
            // 
            // dateNhanHoSo
            // 
            dateNhanHoSo.Location = new Point(186, 448);
            dateNhanHoSo.Margin = new Padding(3, 2, 3, 2);
            dateNhanHoSo.Name = "dateNhanHoSo";
            dateNhanHoSo.Size = new Size(188, 23);
            dateNhanHoSo.TabIndex = 39;
            // 
            // txtViTriTuyenDung
            // 
            txtViTriTuyenDung.Location = new Point(186, 64);
            txtViTriTuyenDung.Margin = new Padding(3, 2, 3, 2);
            txtViTriTuyenDung.Name = "txtViTriTuyenDung";
            txtViTriTuyenDung.Size = new Size(339, 23);
            txtViTriTuyenDung.TabIndex = 35;
            // 
            // txtDiaDiem
            // 
            txtDiaDiem.Location = new Point(186, 489);
            txtDiaDiem.Margin = new Padding(3, 2, 3, 2);
            txtDiaDiem.Name = "txtDiaDiem";
            txtDiaDiem.Size = new Size(339, 23);
            txtDiaDiem.TabIndex = 40;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(207, 579);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 41;
            btnLuu.Text = "Thêm tin tuyển dụng";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // txtLuongMin
            // 
            txtLuongMin.Location = new Point(189, 377);
            txtLuongMin.Margin = new Padding(3, 2, 3, 2);
            txtLuongMin.Name = "txtLuongMin";
            txtLuongMin.Size = new Size(335, 23);
            txtLuongMin.TabIndex = 43;
            // 
            // txtLuongMax
            // 
            txtLuongMax.Location = new Point(189, 413);
            txtLuongMax.Margin = new Padding(3, 2, 3, 2);
            txtLuongMax.Name = "txtLuongMax";
            txtLuongMax.Size = new Size(335, 23);
            txtLuongMax.TabIndex = 44;
            // 
            // txtSoLuongCanTuyen
            // 
            txtSoLuongCanTuyen.Location = new Point(189, 338);
            txtSoLuongCanTuyen.Margin = new Padding(3, 2, 3, 2);
            txtSoLuongCanTuyen.Name = "txtSoLuongCanTuyen";
            txtSoLuongCanTuyen.Size = new Size(335, 23);
            txtSoLuongCanTuyen.TabIndex = 45;
            // 
            // cbTrangThai
            // 
            cbTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Items.AddRange(new object[] { "Mở", "Đóng" });
            cbTrangThai.Location = new Point(186, 525);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(188, 23);
            cbTrangThai.TabIndex = 46;
            // 
            // frmThemTinTuyenDung
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(623, 640);
            Controls.Add(cbTrangThai);
            Controls.Add(txtSoLuongCanTuyen);
            Controls.Add(txtLuongMax);
            Controls.Add(txtLuongMin);
            Controls.Add(btnLuu);
            Controls.Add(txtDiaDiem);
            Controls.Add(dateNhanHoSo);
            Controls.Add(cbPhongBan);
            Controls.Add(txtViTriTuyenDung);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(txtYeuCau);
            Controls.Add(label4);
            Controls.Add(txtMoTaCongViec);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblPhongVan);
            Name = "frmThemTinTuyenDung";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmThemTinTuyenDung";
            ((System.ComponentModel.ISupportInitialize)txtLuongMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtSoLuongCanTuyen).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPhongVan;
        private Label label1;
        private Label label2;
        private Label label3;
        private RichTextBox txtMoTaCongViec;
        private Label label4;
        private RichTextBox txtYeuCau;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private ComboBox cbPhongBan;
        private DateTimePicker dateNhanHoSo;
        private TextBox txtViTriTuyenDung;
        private TextBox txtDiaDiem;
        private Button btnLuu;
        private NumericUpDown txtLuongMin;
        private NumericUpDown txtLuongMax;
        private NumericUpDown txtSoLuongCanTuyen;
        private ComboBox cbTrangThai;
    }
}