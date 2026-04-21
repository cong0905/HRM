namespace HRM.GUI.Forms.Main.TinTuyenDung
{
    partial class frmSuaTinTuyenDung
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
            txtSoLuongCanTuyen = new NumericUpDown();
            txtLuongMax = new NumericUpDown();
            txtLuongMin = new NumericUpDown();
            btnLuu = new Button();
            txtDiaDiem = new TextBox();
            dateNhanHoSo = new DateTimePicker();
            cbPhongBan = new ComboBox();
            txtViTriTuyenDung = new TextBox();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            txtYeuCau = new RichTextBox();
            label4 = new Label();
            txtMoTaCongViec = new RichTextBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            lblPhongVan = new Label();
            label11 = new Label();
            cbTrangThai = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)txtSoLuongCanTuyen).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMin).BeginInit();
            SuspendLayout();
            // 
            // txtSoLuongCanTuyen
            // 
            txtSoLuongCanTuyen.Location = new Point(173, 382);
            txtSoLuongCanTuyen.Margin = new Padding(3, 2, 3, 2);
            txtSoLuongCanTuyen.Name = "txtSoLuongCanTuyen";
            txtSoLuongCanTuyen.Size = new Size(335, 23);
            txtSoLuongCanTuyen.TabIndex = 67;
            // 
            // txtLuongMax
            // 
            txtLuongMax.Location = new Point(173, 457);
            txtLuongMax.Margin = new Padding(3, 2, 3, 2);
            txtLuongMax.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            txtLuongMax.Name = "txtLuongMax";
            txtLuongMax.Size = new Size(335, 23);
            txtLuongMax.TabIndex = 66;
            txtLuongMax.ThousandsSeparator = true;
            // 
            // txtLuongMin
            // 
            txtLuongMin.Location = new Point(173, 421);
            txtLuongMin.Margin = new Padding(3, 2, 3, 2);
            txtLuongMin.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 });
            txtLuongMin.Name = "txtLuongMin";
            txtLuongMin.Size = new Size(335, 23);
            txtLuongMin.TabIndex = 65;
            txtLuongMin.ThousandsSeparator = true;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(191, 623);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 63;
            btnLuu.Text = "Lưu thay đổi";
            btnLuu.UseVisualStyleBackColor = false;
            btnLuu.Click += btnLuu_Click;
            // 
            // txtDiaDiem
            // 
            txtDiaDiem.Location = new Point(170, 533);
            txtDiaDiem.Margin = new Padding(3, 2, 3, 2);
            txtDiaDiem.Name = "txtDiaDiem";
            txtDiaDiem.Size = new Size(339, 23);
            txtDiaDiem.TabIndex = 62;
            // 
            // dateNhanHoSo
            // 
            dateNhanHoSo.Location = new Point(170, 492);
            dateNhanHoSo.Margin = new Padding(3, 2, 3, 2);
            dateNhanHoSo.Name = "dateNhanHoSo";
            dateNhanHoSo.Size = new Size(188, 23);
            dateNhanHoSo.TabIndex = 61;
            // 
            // cbPhongBan
            // 
            cbPhongBan.FormattingEnabled = true;
            cbPhongBan.Location = new Point(170, 144);
            cbPhongBan.Margin = new Padding(3, 2, 3, 2);
            cbPhongBan.Name = "cbPhongBan";
            cbPhongBan.Size = new Size(339, 23);
            cbPhongBan.TabIndex = 60;
            // 
            // txtViTriTuyenDung
            // 
            txtViTriTuyenDung.Location = new Point(170, 108);
            txtViTriTuyenDung.Margin = new Padding(3, 2, 3, 2);
            txtViTriTuyenDung.Name = "txtViTriTuyenDung";
            txtViTriTuyenDung.Size = new Size(339, 23);
            txtViTriTuyenDung.TabIndex = 59;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(45, 569);
            label10.Name = "label10";
            label10.Size = new Size(60, 15);
            label10.TabIndex = 58;
            label10.Text = "Trạng thái";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(45, 535);
            label9.Name = "label9";
            label9.Size = new Size(101, 15);
            label9.TabIndex = 57;
            label9.Text = "Địa điểm làm việc";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(45, 498);
            label8.Name = "label8";
            label8.Size = new Size(116, 15);
            label8.TabIndex = 56;
            label8.Text = "Thời hạn nhận hồ sơ";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(45, 462);
            label7.Name = "label7";
            label7.Size = new Size(98, 15);
            label7.TabIndex = 55;
            label7.Text = "Mức lương tối đa";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(45, 423);
            label6.Name = "label6";
            label6.Size = new Size(112, 15);
            label6.TabIndex = 54;
            label6.Text = "Mức lương tối thiểu";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(45, 384);
            label5.Name = "label5";
            label5.Size = new Size(109, 15);
            label5.TabIndex = 53;
            label5.Text = "Số lượng cần tuyển";
            // 
            // txtYeuCau
            // 
            txtYeuCau.Location = new Point(170, 282);
            txtYeuCau.Margin = new Padding(3, 2, 3, 2);
            txtYeuCau.Name = "txtYeuCau";
            txtYeuCau.Size = new Size(339, 91);
            txtYeuCau.TabIndex = 52;
            txtYeuCau.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(45, 284);
            label4.Name = "label4";
            label4.Size = new Size(97, 15);
            label4.TabIndex = 51;
            label4.Text = "Yêu cầu ứng viên";
            // 
            // txtMoTaCongViec
            // 
            txtMoTaCongViec.Location = new Point(170, 178);
            txtMoTaCongViec.Margin = new Padding(3, 2, 3, 2);
            txtMoTaCongViec.Name = "txtMoTaCongViec";
            txtMoTaCongViec.Size = new Size(339, 91);
            txtMoTaCongViec.TabIndex = 50;
            txtMoTaCongViec.Text = "";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(45, 180);
            label3.Name = "label3";
            label3.Size = new Size(95, 15);
            label3.TabIndex = 49;
            label3.Text = "Mô tả công việc ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(45, 144);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 48;
            label2.Text = "Phòng ban:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(45, 111);
            label1.Name = "label1";
            label1.Size = new Size(98, 15);
            label1.TabIndex = 47;
            label1.Text = "Vị trí tuyển dụng:";
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(183, 58);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(0, 30);
            lblPhongVan.TabIndex = 46;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            label11.ForeColor = Color.FromArgb(41, 128, 185);
            label11.Location = new Point(170, 28);
            label11.Name = "label11";
            label11.Size = new Size(211, 30);
            label11.TabIndex = 68;
            label11.Text = "Sửa tin tuyển dụng";
            // 
            // cbTrangThai
            // 
            cbTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Items.AddRange(new object[] { "Mở", "Đóng" });
            cbTrangThai.Location = new Point(170, 569);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(174, 23);
            cbTrangThai.TabIndex = 69;
            // 
            // frmSuaTinTuyenDung
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(583, 720);
            Controls.Add(cbTrangThai);
            Controls.Add(label11);
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
            Margin = new Padding(3, 2, 3, 2);
            Name = "frmSuaTinTuyenDung";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmSuaTuyenDung";
            Load += frmSuaTinTuyenDung_Load_1;
            ((System.ComponentModel.ISupportInitialize)txtSoLuongCanTuyen).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtLuongMin).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NumericUpDown txtSoLuongCanTuyen;
        private NumericUpDown txtLuongMax;
        private NumericUpDown txtLuongMin;
        private Button btnLuu;
        private TextBox txtDiaDiem;
        private DateTimePicker dateNhanHoSo;
        private ComboBox cbPhongBan;
        private TextBox txtViTriTuyenDung;
        private Label label10;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private RichTextBox txtYeuCau;
        private Label label4;
        private RichTextBox txtMoTaCongViec;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label lblPhongVan;
        private Label label11;
        private ComboBox cbTrangThai;
    }
}