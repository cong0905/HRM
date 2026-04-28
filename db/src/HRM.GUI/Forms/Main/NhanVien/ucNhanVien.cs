using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.NhanVien
{
    public partial class ucNhanVien : UserControl
    {
        private readonly INhanVienService _nhanVienService;
        private readonly IPhongBanService _phongBanService;
        private readonly UserSessionDTO? _session;

        public ucNhanVien() : this(null) { }

        public ucNhanVien(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _nhanVienService = null!;
                _phongBanService = null!;
                return;
            }
            _nhanVienService = Program.ServiceProvider.GetRequiredService<INhanVienService>();
            _phongBanService = Program.ServiceProvider.GetRequiredService<IPhongBanService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
        {
            var lblTitle = new Label
            {
                Text = "👥 Danh sách nhân viên",
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
                PlaceholderText = "Nhập tên nhân viên, mã, SĐT, email..."
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

            var dgv = UIHelper.CreateStyledDataGridView("dgvNhanVien");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(Width - 40, Height - 120);

            btnAdd.Click += async (_, _) =>
            {
                var frm = Program.ServiceProvider.GetRequiredService<frmThemNhanVien>();
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    dgv.DataSource = await _nhanVienService.GetAllAsync();
                }
            };

            btnEdit.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn nhân viên cần sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var dto = (NhanVienDTO)dgv.SelectedRows[0].DataBoundItem;
                var chucVuRepo = Program.ServiceProvider.GetRequiredService<HRM.DAL.Repositories.IRepository<HRM.Domain.Entities.ChucVu>>();
                var frm = new frmSuaNhanVien(_nhanVienService, _phongBanService, chucVuRepo, dto);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    dgv.DataSource = await _nhanVienService.GetAllAsync();
                }
            };

            btnDelete.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn nhân viên cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var dto = (NhanVienDTO)dgv.SelectedRows[0].DataBoundItem;
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa nhân viên [{dto.HoTen}] không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                try
                {
                    await _nhanVienService.DeleteAsync(dto.MaNhanVien);
                    MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgv.DataSource = await _nhanVienService.GetAllAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dgv.DataBindingComplete += (_, _) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {
                        case "MaNV": col.HeaderText = "Mã Nhân Viên"; col.DisplayIndex = 0; break;
                        case "HoTen": col.HeaderText = "Họ Tên"; col.MinimumWidth = 150; col.DisplayIndex = 1; break;
                        case "TenPhongBan": col.HeaderText = "Phòng Ban"; col.MinimumWidth = 120; col.DisplayIndex = 2; break;
                        case "TenChucVu": col.HeaderText = "Chức Vụ"; col.MinimumWidth = 120; col.DisplayIndex = 3; break;
                        case "MucLuong":
                            col.HeaderText = "Mức Lương";
                            col.DefaultCellStyle.Format = "N0";
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            col.DisplayIndex = 4;
                            break;
                        case "TrangThai": col.HeaderText = "Trạng Thái"; col.DisplayIndex = 5; break;
                        case "SoDienThoai": col.HeaderText = "Số Điện Thoại"; break;
                        case "Email": col.HeaderText = "Email"; col.MinimumWidth = 150; break;
                        case "NgaySinh": col.HeaderText = "Ngày Sinh"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                        case "GioiTinh": col.HeaderText = "Giới Tính"; break;
                        case "NgayVaoLam": col.HeaderText = "Ngày Vào Làm"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                        case "CCCD": col.HeaderText = "Số CCCD"; break;
                        case "DiaChi": col.HeaderText = "Địa Chỉ"; col.MinimumWidth = 200; break;
                        case "TinhTrangHonNhan": col.HeaderText = "Hôn Nhân"; break;
                        case "MaNhanVien":
                        case "MaPhongBan":
                        case "MaChucVu":
                        case "AnhDaiDien":
                            col.Visible = false;
                            break;
                    }
                }
            };

            btnSearch.Click += async (_, _) =>
            {
                try
                {
                    var keyword = txtSearch.Text.Trim();
                    var data = string.IsNullOrEmpty(keyword)
                        ? await _nhanVienService.GetAllAsync()
                        : await _nhanVienService.SearchAsync(keyword);
                    dgv.DataSource = data;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            txtSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

            Controls.Add(lblTitle);
            Controls.Add(txtSearch);
            Controls.Add(btnSearch);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(dgv);

            try
            {
                dgv.DataSource = await _nhanVienService.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
