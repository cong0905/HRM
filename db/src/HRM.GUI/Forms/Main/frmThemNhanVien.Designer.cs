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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblHoTen = new System.Windows.Forms.Label();
            this.txtHoTen = new System.Windows.Forms.TextBox();
            this.lblNgaySinh = new System.Windows.Forms.Label();
            this.dtpNgaySinh = new System.Windows.Forms.DateTimePicker();
            this.lblGioiTinh = new System.Windows.Forms.Label();
            this.cboGioiTinh = new System.Windows.Forms.ComboBox();
            this.lblCCCD = new System.Windows.Forms.Label();
            this.txtCCCD = new System.Windows.Forms.TextBox();
            this.lblDienThoai = new System.Windows.Forms.Label();
            this.txtSoDienThoai = new System.Windows.Forms.TextBox();
            this.lblEmail = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.lblNgayVaoLam = new System.Windows.Forms.Label();
            this.dtpNgayVaoLam = new System.Windows.Forms.DateTimePicker();
            this.btnLuu = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.lblTitle.Location = new System.Drawing.Point(80, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(240, 30);
            this.lblTitle.Text = "THÊM NHÂN VIÊN MỚI";

            // lblHoTen
            this.lblHoTen.AutoSize = true;
            this.lblHoTen.Location = new System.Drawing.Point(30, 80);
            this.lblHoTen.Text = "Họ tên:";
            // txtHoTen
            this.txtHoTen.Location = new System.Drawing.Point(120, 77);
            this.txtHoTen.Size = new System.Drawing.Size(200, 23);

            // lblNgaySinh
            this.lblNgaySinh.AutoSize = true;
            this.lblNgaySinh.Location = new System.Drawing.Point(30, 120);
            this.lblNgaySinh.Text = "Ngày sinh:";
            // dtpNgaySinh
            this.dtpNgaySinh.Location = new System.Drawing.Point(120, 117);
            this.dtpNgaySinh.Size = new System.Drawing.Size(200, 23);
            this.dtpNgaySinh.Format = System.Windows.Forms.DateTimePickerFormat.Short;

            // lblGioiTinh
            this.lblGioiTinh.AutoSize = true;
            this.lblGioiTinh.Location = new System.Drawing.Point(30, 160);
            this.lblGioiTinh.Text = "Giới tính:";
            // cboGioiTinh
            this.cboGioiTinh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ" });
            this.cboGioiTinh.Location = new System.Drawing.Point(120, 157);
            this.cboGioiTinh.Size = new System.Drawing.Size(200, 23);

            // lblCCCD
            this.lblCCCD.AutoSize = true;
            this.lblCCCD.Location = new System.Drawing.Point(30, 200);
            this.lblCCCD.Text = "Số CCCD:";
            // txtCCCD
            this.txtCCCD.Location = new System.Drawing.Point(120, 197);
            this.txtCCCD.Size = new System.Drawing.Size(200, 23);

            // lblDienThoai
            this.lblDienThoai.AutoSize = true;
            this.lblDienThoai.Location = new System.Drawing.Point(30, 240);
            this.lblDienThoai.Text = "Điện thoại:";
            // txtSoDienThoai
            this.txtSoDienThoai.Location = new System.Drawing.Point(120, 237);
            this.txtSoDienThoai.Size = new System.Drawing.Size(200, 23);

            // lblEmail
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new System.Drawing.Point(30, 280);
            this.lblEmail.Text = "Email:";
            // txtEmail
            this.txtEmail.Location = new System.Drawing.Point(120, 277);
            this.txtEmail.Size = new System.Drawing.Size(200, 23);

            // lblNgayVaoLam
            this.lblNgayVaoLam.AutoSize = true;
            this.lblNgayVaoLam.Location = new System.Drawing.Point(30, 320);
            this.lblNgayVaoLam.Text = "Vào làm:";
            // dtpNgayVaoLam
            this.dtpNgayVaoLam.Location = new System.Drawing.Point(120, 317);
            this.dtpNgayVaoLam.Size = new System.Drawing.Size(200, 23);
            this.dtpNgayVaoLam.Format = System.Windows.Forms.DateTimePickerFormat.Short;

            // btnLuu
            this.btnLuu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.btnLuu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLuu.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLuu.ForeColor = System.Drawing.Color.White;
            this.btnLuu.Location = new System.Drawing.Point(120, 370);
            this.btnLuu.Size = new System.Drawing.Size(200, 40);
            this.btnLuu.Text = "Thêm Nhân Viên";
            this.btnLuu.UseVisualStyleBackColor = false;
            this.btnLuu.Click += new System.EventHandler(this.btnLuu_Click);

            // frmThemNhanVien
            this.ClientSize = new System.Drawing.Size(380, 450);
            this.Controls.Add(this.btnLuu);
            this.Controls.Add(this.dtpNgayVaoLam);
            this.Controls.Add(this.lblNgayVaoLam);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.txtSoDienThoai);
            this.Controls.Add(this.lblDienThoai);
            this.Controls.Add(this.txtCCCD);
            this.Controls.Add(this.lblCCCD);
            this.Controls.Add(this.cboGioiTinh);
            this.Controls.Add(this.lblGioiTinh);
            this.Controls.Add(this.dtpNgaySinh);
            this.Controls.Add(this.lblNgaySinh);
            this.Controls.Add(this.txtHoTen);
            this.Controls.Add(this.lblHoTen);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Thêm Nhân Viên";
            this.BackColor = System.Drawing.Color.White;
            this.ResumeLayout(false);
            this.PerformLayout();
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
