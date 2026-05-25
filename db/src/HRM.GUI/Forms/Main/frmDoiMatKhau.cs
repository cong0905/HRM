using System;
using System.Drawing;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main
{
    public class frmDoiMatKhau : Form
    {
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly UserSessionDTO _session;

        private TextBox txtMatKhauMoi;
        private TextBox txtXacNhan;
        private Button btnLuu;
        private Button btnHuy;

        public frmDoiMatKhau(ITaiKhoanService taiKhoanService, UserSessionDTO session)
        {
            _taiKhoanService = taiKhoanService;
            _session = session;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đổi mật khẩu";
            this.Size = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = "ĐỔI MẬT KHẨU",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblMoi = new Label { Text = "Mật khẩu mới:", Left = 30, Top = 60, Width = 120 };
            txtMatKhauMoi = new TextBox { Left = 160, Top = 57, Width = 180, UseSystemPasswordChar = true };

            var lblXacNhan = new Label { Text = "Xác nhận:", Left = 30, Top = 100, Width = 120 };
            txtXacNhan = new TextBox { Left = 160, Top = 97, Width = 180, UseSystemPasswordChar = true };

            btnLuu = new Button
            {
                Text = "Lưu thay đổi",
                Left = 100,
                Top = 150,
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLuu.FlatAppearance.BorderSize = 0;
            btnLuu.Click += BtnLuu_Click;

            btnHuy = new Button
            {
                Text = "Hủy",
                Left = 230,
                Top = 150,
                Width = 80,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnHuy.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblTitle, lblMoi, txtMatKhauMoi, lblXacNhan, txtXacNhan, btnLuu, btnHuy });
            this.AcceptButton = btnLuu;
        }

        private async void BtnLuu_Click(object? sender, EventArgs e)
        {
            var p1 = txtMatKhauMoi.Text;
            var p2 = txtXacNhan.Text;

            if (string.IsNullOrWhiteSpace(p1))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu mới", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (p1 != p2)
            {
                MessageBox.Show("Xác nhận mật khẩu không khớp", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                await _taiKhoanService.ChangePasswordAsync(_session.MaTaiKhoan, p1);
                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
