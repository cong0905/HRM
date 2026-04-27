namespace HRM.GUI.Forms.Main
{
    partial class frmThemPhongVan
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
            lblHoTen = new Label();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            cbVongPhongVan = new ComboBox();
            dtpNgayPhongvan = new DateTimePicker();
            txtDiaDiem = new TextBox();
            cbNguoiPV = new ComboBox();
            cbKetQua = new ComboBox();
            cbTrangThai = new ComboBox();
            txtNhanXet = new RichTextBox();
            cbMaUngVien = new ComboBox();
            btnLuu = new Button();
            SuspendLayout();
            // 
            // lblPhongVan
            // 
            lblPhongVan.AutoSize = true;
            lblPhongVan.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblPhongVan.ForeColor = Color.FromArgb(41, 128, 185);
            lblPhongVan.Location = new Point(135, 38);
            lblPhongVan.Name = "lblPhongVan";
            lblPhongVan.Size = new Size(190, 30);
            lblPhongVan.TabIndex = 18;
            lblPhongVan.Text = "Thêm Phỏng Vấn";
            // 
            // lblHoTen
            // 
            lblHoTen.AutoSize = true;
            lblHoTen.Location = new Point(47, 87);
            lblHoTen.Name = "lblHoTen";
            lblHoTen.Size = new Size(0, 15);
            lblHoTen.TabIndex = 19;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(47, 87);
            label1.Name = "label1";
            label1.Size = new Size(75, 15);
            label1.TabIndex = 20;
            label1.Text = "Tên ứng viên";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(47, 136);
            label2.Name = "label2";
            label2.Size = new Size(97, 15);
            label2.TabIndex = 21;
            label2.Text = "Vòng phỏng vấn:";
            label2.Click += label2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(47, 183);
            label3.Name = "label3";
            label3.Size = new Size(98, 15);
            label3.TabIndex = 22;
            label3.Text = "Ngày phỏng vấn:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(47, 223);
            label4.Name = "label4";
            label4.Size = new Size(57, 15);
            label4.TabIndex = 23;
            label4.Text = "Địa điểm:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(47, 266);
            label5.Name = "label5";
            label5.Size = new Size(103, 15);
            label5.TabIndex = 24;
            label5.Text = "Người phỏng vấn:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(47, 307);
            label6.Name = "label6";
            label6.Size = new Size(50, 15);
            label6.TabIndex = 25;
            label6.Text = "Kết quả:";
            label6.Click += label6_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(47, 349);
            label7.Name = "label7";
            label7.Size = new Size(63, 15);
            label7.TabIndex = 26;
            label7.Text = "Trạng thái:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(47, 390);
            label8.Name = "label8";
            label8.Size = new Size(57, 15);
            label8.TabIndex = 27;
            label8.Text = "Nhận xét:";
            // 
            // cbVongPhongVan
            // 
            cbVongPhongVan.FormattingEnabled = true;
            cbVongPhongVan.Location = new Point(166, 133);
            cbVongPhongVan.Margin = new Padding(3, 2, 3, 2);
            cbVongPhongVan.Name = "cbVongPhongVan";
            cbVongPhongVan.Size = new Size(182, 23);
            cbVongPhongVan.TabIndex = 30;
            // 
            // dtpNgayPhongvan
            // 
            dtpNgayPhongvan.Format = DateTimePickerFormat.Short;
            dtpNgayPhongvan.Location = new Point(166, 177);
            dtpNgayPhongvan.Margin = new Padding(3, 2, 3, 2);
            dtpNgayPhongvan.Name = "dtpNgayPhongvan";
            dtpNgayPhongvan.Size = new Size(182, 23);
            dtpNgayPhongvan.TabIndex = 31;
            // 
            // txtDiaDiem
            // 
            txtDiaDiem.Location = new Point(166, 220);
            txtDiaDiem.Margin = new Padding(3, 2, 3, 2);
            txtDiaDiem.Name = "txtDiaDiem";
            txtDiaDiem.Size = new Size(182, 23);
            txtDiaDiem.TabIndex = 32;
            // 
            // cbNguoiPV
            // 
            cbNguoiPV.FormattingEnabled = true;
            cbNguoiPV.Location = new Point(166, 263);
            cbNguoiPV.Margin = new Padding(3, 2, 3, 2);
            cbNguoiPV.Name = "cbNguoiPV";
            cbNguoiPV.Size = new Size(182, 23);
            cbNguoiPV.TabIndex = 33;
            // 
            // cbKetQua
            // 
            cbKetQua.FormattingEnabled = true;
            cbKetQua.Location = new Point(166, 307);
            cbKetQua.Margin = new Padding(3, 2, 3, 2);
            cbKetQua.Name = "cbKetQua";
            cbKetQua.Size = new Size(182, 23);
            cbKetQua.TabIndex = 34;
            // 
            // cbTrangThai
            // 
            cbTrangThai.FormattingEnabled = true;
            cbTrangThai.Location = new Point(166, 349);
            cbTrangThai.Margin = new Padding(3, 2, 3, 2);
            cbTrangThai.Name = "cbTrangThai";
            cbTrangThai.Size = new Size(182, 23);
            cbTrangThai.TabIndex = 35;
            // 
            // txtNhanXet
            // 
            txtNhanXet.Location = new Point(166, 390);
            txtNhanXet.Margin = new Padding(3, 2, 3, 2);
            txtNhanXet.Name = "txtNhanXet";
            txtNhanXet.Size = new Size(211, 91);
            txtNhanXet.TabIndex = 37;
            txtNhanXet.Text = "";
            // 
            // cbMaUngVien
            // 
            cbMaUngVien.FormattingEnabled = true;
            cbMaUngVien.Location = new Point(166, 84);
            cbMaUngVien.Name = "cbMaUngVien";
            cbMaUngVien.Size = new Size(182, 23);
            cbMaUngVien.TabIndex = 38;
            // 
            // btnLuu
            // 
            btnLuu.BackColor = Color.FromArgb(41, 128, 185);
            btnLuu.FlatStyle = FlatStyle.Flat;
            btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLuu.ForeColor = Color.White;
            btnLuu.Location = new Point(125, 496);
            btnLuu.Name = "btnLuu";
            btnLuu.Size = new Size(200, 40);
            btnLuu.TabIndex = 39;
            btnLuu.Text = "Thêm Phỏng Vấn";
            btnLuu.UseVisualStyleBackColor = false;
            // 
            // frmThemPhongVan
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(453, 548);
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
            Margin = new Padding(3, 2, 3, 2);
            Name = "frmThemPhongVan";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Thêm phỏng vấn";
            Load += frmThemPhongVan_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPhongVan;
        private Label lblHoTen;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private TextBox txtHoTen;
        private ComboBox cbVongPhongVan;
        private DateTimePicker dtpNgayPhongvan;
        private TextBox txtDiaDiem;
        private ComboBox cbNguoiPV;
        private ComboBox cbKetQua;
        private ComboBox cbTrangThai;
        private RichTextBox txtNhanXet;
        private ComboBox cbMaUngVien;
        private Button btnLuu;
    }
}