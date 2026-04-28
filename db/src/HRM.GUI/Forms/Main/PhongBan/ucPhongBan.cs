using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.PhongBan
{
    public partial class ucPhongBan : UserControl
    {
        private readonly IPhongBanService _phongBanService;
        private readonly UserSessionDTO? _session;

        public ucPhongBan() : this(null) { }

        public ucPhongBan(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _phongBanService = null!;
                return;
            }
            _phongBanService = Program.ServiceProvider.GetRequiredService<IPhongBanService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
        {
            var lblTitle = new Label
            {
                Text = "🏗️ Danh sách phòng ban",
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
                PlaceholderText = "Nhập tên phòng ban, mô tả, địa điểm..."
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

            var isAdmin = UIHelper.IsAdmin(_session);

            var btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(440, 59),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = isAdmin
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnEdit = new Button
            {
                Text = "✏️ Sửa",
                Location = new Point(550, 59),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(241, 196, 15),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = isAdmin
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
                Visible = isAdmin
            };
            btnDelete.FlatAppearance.BorderSize = 0;

            var dgv = UIHelper.CreateStyledDataGridView("dgvPhongBan");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(Width - 40, Height - 120);

            dgv.DataBindingComplete += (_, _) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {
                        case "MaPhongBan": col.HeaderText = "Mã PB"; col.Visible = false; break;
                        case "TenPhongBan": col.HeaderText = "Tên Phòng Ban"; col.MinimumWidth = 150; break;
                        case "MoTaChucNang":
                            col.HeaderText = "Mô Tả Chức Năng";
                            col.MinimumWidth = 200;
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                            break;
                        case "DiaDiemLamViec": col.HeaderText = "Địa Điểm"; col.MinimumWidth = 120; break;
                        case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                        case "SoNhanVien":
                            col.HeaderText = "Số Nhân Viên";
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                    }
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(txtSearch);
            Controls.Add(btnSearch);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(dgv);

            btnSearch.Click += async (_, _) =>
            {
                try
                {
                    var kw = txtSearch.Text.Trim();
                    var data = string.IsNullOrEmpty(kw)
                        ? await _phongBanService.GetAllAsync()
                        : await _phongBanService.SearchAsync(kw);
                    dgv.DataSource = data;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            txtSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

            btnAdd.Click += async (_, _) =>
            {
                using var frm = new frmPhongBan(_phongBanService);
                if (frm.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    dgv.DataSource = await _phongBanService.GetAllAsync();
                }
            };

            btnEdit.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                if (dgv.SelectedRows[0].DataBoundItem is not PhongBanDTO dto) return;

                using var frm = new frmPhongBan(_phongBanService, dto);
                if (frm.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    dgv.DataSource = await _phongBanService.GetAllAsync();
                }
            };

            btnDelete.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                if (dgv.SelectedRows[0].DataBoundItem is not PhongBanDTO dto) return;

                if (dto.SoNhanVien > 0)
                {
                    MessageBox.Show($"Không thể xóa phòng ban có nhân viên ({dto.SoNhanVien} người).", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng ban '{dto.TenPhongBan}' không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                try
                {
                    await _phongBanService.DeleteAsync(dto.MaPhongBan);
                    dgv.DataSource = await _phongBanService.GetAllAsync();
                    MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            try
            {
                dgv.DataSource = await _phongBanService.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
