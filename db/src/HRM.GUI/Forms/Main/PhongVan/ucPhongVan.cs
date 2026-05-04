using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.PhongVan
{

    public partial class ucPhongVan : UserControl
    {
        private readonly IPhongVanService _phongVanService;
        private UserSessionDTO _session;

        public ucPhongVan(UserSessionDTO session)
        {
            InitializeComponent();
            _phongVanService = Program.ServiceProvider.GetRequiredService<IPhongVanService>();
            _session = session;
            this.Load += async (s, e) => await LoadPhongVanView();
        }

        private async Task LoadPhongVanView()
        {
            var lblTitle = new Label
            {
                Text = "🎤 Danh sách phỏng vấn",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var txtSearch = new TextBox
            {
                Location = new Point(20, 60),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Nhập mã, ứng viên, địa điểm..."
            };

            var btnSearch = new Button
            {
                Text = "🔍 Tìm kiếm",
                Location = new Point(330, 59),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;

            var btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(440, 59),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên" || _session?.VaiTro == "HR")
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            bool isManager = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên" || _session?.VaiTro == "HR");

            var btnEdit = new Button
            {
                Text = "✏️ Sửa",
                Location = new Point(550, 59),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(241, 196, 15),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = isManager
            };
            btnEdit.FlatAppearance.BorderSize = 0;

            var btnDelete = new Button
            {
                Text = "🗑️ Xóa",
                Location = new Point(640, 59),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = isManager
            };
            btnDelete.FlatAppearance.BorderSize = 0;

            var dgv = UIHelper.CreateStyledDataGridView("dgvPhongVan");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(this.Width - 40, this.Height - 120);

            dgv.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {
                        case "MaHienThi": col.HeaderText = "Mã PV"; col.MinimumWidth = 80; break;
                        case "MaPhongVan": col.Visible = false; break;
                        case "MaUngVien": col.HeaderText = "Mã Ứng Viên"; break;
                        case "TenUngVien": col.HeaderText = "Tên Ứng Viên"; col.MinimumWidth = 180; break;
                        case "VongPhongVan": col.HeaderText = "Vòng PV"; break;
                        case "NgayPhongVan": col.HeaderText = "Ngày Phỏng Vấn"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                        case "DiaDiem": col.HeaderText = "Địa Điểm"; col.MinimumWidth = 200; break;
                        case "NguoiPhongVan": col.HeaderText = "Người Phỏng Vấn"; col.MinimumWidth = 150; break;
                        case "NhanXet": col.HeaderText = "Nhận xét"; col.MinimumWidth = 150; break;
                        case "KetQua": col.HeaderText = "Kết Quả"; break;
                        case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                        case "NguoiPhongVanId": col.Visible = false; break;
                        default: col.Visible = false; break;
                    }
                }
            };

            btnSearch.Click += async (s, e) =>
            {
                try
                {
                    var keyword = txtSearch.Text.Trim();
                    dgv.DataSource = string.IsNullOrWhiteSpace(keyword)
                        ? await _phongVanService.GetAllAsync()
                        : await _phongVanService.SearchAsync(keyword);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tìm kiếm dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnAdd.Click += async (s, e) =>
            {
                try
                {
                    var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.frmThemPhongVan>();
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        dgv.DataSource = await _phongVanService.GetAllAsync();
                    }
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Lỗi: Chưa đăng ký frmThemPhongVan trong file Program.cs!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnEdit.Click += async (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0)
                {
                    MessageBox.Show("Vui lòng chọn một lịch phỏng vấn trước khi bấm Sửa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }

                try
                {
                    int maPhongVan = Convert.ToInt32(dgv.CurrentRow.Cells["MaPhongVan"].Value);
                    var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.PhongVan.frmSuaPhongVan>();
                    frm.MaPhongVanCachSua = maPhongVan;

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        dgv.DataSource = await _phongVanService.GetAllAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi khi mở Form Sửa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnDelete.Click += async (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0)
                {
                    MessageBox.Show("Vui lòng chọn một lịch phỏng vấn cần xóa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }

                try
                {
                    int maPhongVan = Convert.ToInt32(dgv.CurrentRow.Cells["MaPhongVan"].Value);
                    string tenUngVien = dgv.CurrentRow.Cells["TenUngVien"].Value?.ToString() ?? "lịch phỏng vấn này";

                    var xacNhan = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa lịch phỏng vấn của {tenUngVien} không?",
                        "Cảnh báo xóa",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (xacNhan != DialogResult.Yes)
                        return;

                    bool isSuccess = await _phongVanService.DeletePhongVanAsync(maPhongVan);
                    if (isSuccess)
                    {
                        MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dgv.DataSource = await _phongVanService.GetAllAsync();
                    }
                    else
                    {
                        MessageBox.Show("Xóa thất bại! Dữ liệu có thể đang bị ràng buộc ở nơi khác.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hệ thống khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(txtSearch);
            this.Controls.Add(btnSearch);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(dgv);

            try
            {
                var data = await _phongVanService.GetAllAsync();
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
