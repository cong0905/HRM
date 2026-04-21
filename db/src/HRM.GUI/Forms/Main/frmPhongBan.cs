using System;
using System.Drawing;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main
{
    public class frmPhongBan : Form
    {
        private readonly IPhongBanService _phongBanService;
        private readonly PhongBanDTO? _dto;

        private TextBox txtTenPhongBan;
        private TextBox txtMoTa;
        private TextBox txtDiaDiem;
        private ComboBox cboTrangThai;
        private Button btnLuu;
        private Button btnDong;

        public frmPhongBan(IPhongBanService phongBanService, PhongBanDTO? dto = null)
        {
            _phongBanService = phongBanService;
            _dto = dto;

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Text = _dto == null ? "Thêm Phòng Ban Mới" : "Sửa Thông Tin Phòng Ban";
            this.Size = new Size(500, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var title = new Label
            {
                Text = this.Text,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(title);

            int y = 60, dy = 45;

            this.Controls.Add(new Label { Text = "Tên phòng ban:", Location = new Point(20, y + 3), AutoSize = true });
            txtTenPhongBan = new TextBox { Location = new Point(140, y), Width = 300, Font = new Font("Segoe UI", 10) };
            this.Controls.Add(txtTenPhongBan);

            y += dy;
            this.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(20, y + 3), AutoSize = true });
            txtMoTa = new TextBox { Location = new Point(140, y), Width = 300, Font = new Font("Segoe UI", 10) };
            this.Controls.Add(txtMoTa);

            y += dy;
            this.Controls.Add(new Label { Text = "Địa điểm:", Location = new Point(20, y + 3), AutoSize = true });
            txtDiaDiem = new TextBox { Location = new Point(140, y), Width = 300, Font = new Font("Segoe UI", 10) };
            this.Controls.Add(txtDiaDiem);

            y += dy;
            this.Controls.Add(new Label { Text = "Trạng thái:", Location = new Point(20, y + 3), AutoSize = true });
            cboTrangThai = new ComboBox 
            { 
                Location = new Point(140, y), 
                Width = 300, 
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrangThai.Items.AddRange(new[] { "Hoạt động", "Tạm ngừng", "Đã giải thể" });
            cboTrangThai.SelectedIndex = 0;
            this.Controls.Add(cboTrangThai);

            y += dy + 15;
            btnLuu = new Button
            {
                Text = "💾 Lưu",
                Location = new Point(220, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLuu.FlatAppearance.BorderSize = 0;
            btnLuu.Click += BtnLuu_Click;
            this.Controls.Add(btnLuu);

            btnDong = new Button
            {
                Text = "✖ Đóng",
                Location = new Point(340, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDong.FlatAppearance.BorderSize = 0;
            btnDong.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnDong);

            this.AcceptButton = btnLuu;
            this.CancelButton = btnDong;
        }

        private void LoadData()
        {
            if (_dto != null)
            {
                txtTenPhongBan.Text = _dto.TenPhongBan;
                txtMoTa.Text = _dto.MoTaChucNang;
                txtDiaDiem.Text = _dto.DiaDiemLamViec;
                cboTrangThai.Text = _dto.TrangThai;
            }
        }

        private async void BtnLuu_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenPhongBan.Text))
            {
                MessageBox.Show("Tên phòng ban không được để trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnLuu.Enabled = false;

                if (_dto == null) // Thêm mới
                {
                    var dto = new PhongBanDTO
                    {
                        TenPhongBan = txtTenPhongBan.Text.Trim(),
                        MoTaChucNang = txtMoTa.Text.Trim(),
                        DiaDiemLamViec = txtDiaDiem.Text.Trim(),
                        TrangThai = cboTrangThai.Text
                    };
                    await _phongBanService.CreateAsync(dto);
                }
                else // Cập nhật
                {
                    _dto.TenPhongBan = txtTenPhongBan.Text.Trim();
                    _dto.MoTaChucNang = txtMoTa.Text.Trim();
                    _dto.DiaDiemLamViec = txtDiaDiem.Text.Trim();
                    _dto.TrangThai = cboTrangThai.Text;

                    await _phongBanService.UpdateAsync(_dto);
                }

                MessageBox.Show("Lưu phòng ban thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {(ex.InnerException?.Message ?? ex.Message)}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }
    }
}
