using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.NhanVien
{
    public partial class ucNhanVien : UserControl
    {
        private readonly UserSessionDTO? _session;

        private DataGridView? _dgv;
        private TextBox? _txtSearch;
        private ComboBox? _cboPhongBan;
        private ComboBox? _cboTrangThai;
        private ComboBox? _cboGioiTinh;
        private Label? _lblCount;
        private System.Windows.Forms.Timer? _debounceTimer;
        private bool _isLoading; // chặn event khi đang khởi tạo

        public ucNhanVien() : this(null) { }

        public ucNhanVien(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime()) return;
            Load += async (_, _) => await BuildView();
        }

        private async Task BuildView()
        {
            _isLoading = true;
            var isAdmin = UIHelper.IsAdmin(_session);

            // ── Tiêu đề ──────────────────────────────────────────────────────
            Controls.Add(new Label
            {
                Text = "👥 Danh sách nhân viên",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = new Point(20, 14)
            });

            // ── Thanh tìm kiếm & bộ lọc ──────────────────────────────────────
            var pnlFilter = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(Width - 40, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _txtSearch = new TextBox
            {
                Location = new Point(0, 5),
                Size = new Size(250, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍  Tên, mã NV, SĐT, email, CCCD..."
            };
            _txtSearch.TextChanged += OnFilterChanged;
            _txtSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) e.SuppressKeyPress = true; };

            _cboPhongBan = new ComboBox
            {
                Location = new Point(260, 5),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cboPhongBan.SelectedIndexChanged += OnFilterChanged;

            _cboTrangThai = new ComboBox
            {
                Location = new Point(430, 5),
                Size = new Size(140, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cboTrangThai.Items.AddRange(new[] { "Tất cả", "Đang làm việc", "Nghỉ phép", "Nghỉ việc" });
            _cboTrangThai.SelectedIndex = 0;
            _cboTrangThai.SelectedIndexChanged += OnFilterChanged;

            _cboGioiTinh = new ComboBox
            {
                Location = new Point(580, 5),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cboGioiTinh.Items.AddRange(new[] { "Tất cả", "Nam", "Nữ", "Khác" });
            _cboGioiTinh.SelectedIndex = 0;
            _cboGioiTinh.SelectedIndexChanged += OnFilterChanged;

            var btnReset = UIHelper.CreateActionButton("↺ Làm mới", new Point(700, 4),
                new Size(88, 30), Color.FromArgb(150, 160, 180));
            btnReset.Click += async (_, _) =>
            {
                _isLoading = true;
                _txtSearch.Clear();
                _cboPhongBan.SelectedIndex = 0;
                _cboTrangThai.SelectedIndex = 0;
                _cboGioiTinh.SelectedIndex = 0;
                _isLoading = false;
                await ApplyFilter();
            };

            _lblCount = new Label
            {
                Location = new Point(798, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 110, 130)
            };

            pnlFilter.Controls.AddRange(new Control[]
            {
                _txtSearch, _cboPhongBan, _cboTrangThai, _cboGioiTinh, btnReset, _lblCount
            });
            Controls.Add(pnlFilter);

            // ── Nút hành động ─────────────────────────────────────────────────
            var pnlActions = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(Width - 40, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            if (isAdmin)
            {
                var btnAdd = UIHelper.CreateActionButton("➕ Thêm mới", new Point(0, 3),
                    new Size(110, 30), Color.FromArgb(46, 204, 113));
                btnAdd.Click += async (_, _) =>
                {
                    var frm = Program.ServiceProvider.GetRequiredService<frmThemNhanVien>();
                    if (frm.ShowDialog() == DialogResult.OK)
                        await ApplyFilter();
                };

                var btnEdit = UIHelper.CreateActionButton("✏️ Sửa", new Point(120, 3),
                    new Size(90, 30), Color.FromArgb(241, 196, 15));
                btnEdit.Click += async (_, _) =>
                {
                    if (_dgv?.SelectedRows.Count == 0)
                    {
                        MessageBox.Show("Vui lòng chọn nhân viên cần sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var dto = (NhanVienDTO)_dgv.SelectedRows[0].DataBoundItem;
                    using var scope = Program.ServiceProvider.CreateScope();
                    var nhanVienSvc = scope.ServiceProvider.GetRequiredService<INhanVienService>();
                    var phongBanSvc = scope.ServiceProvider.GetRequiredService<IPhongBanService>();
                    var chucVuRepo = scope.ServiceProvider.GetRequiredService<HRM.DAL.Repositories.IRepository<HRM.Domain.Entities.ChucVu>>();
                    var frm = new frmSuaNhanVien(nhanVienSvc, phongBanSvc, chucVuRepo, dto);
                    if (frm.ShowDialog() == DialogResult.OK)
                        await ApplyFilter();
                };

                var btnDelete = UIHelper.CreateActionButton("🗑️ Xóa", new Point(220, 3),
                    new Size(90, 30), Color.FromArgb(231, 76, 60));
                btnDelete.Click += async (_, _) =>
                {
                    if (_dgv?.SelectedRows.Count == 0)
                    {
                        MessageBox.Show("Vui lòng chọn nhân viên cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var dto = (NhanVienDTO)_dgv.SelectedRows[0].DataBoundItem;
                    if (MessageBox.Show($"Xóa nhân viên [{dto.HoTen}]?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;
                    try
                    {
                        using var scope = Program.ServiceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<INhanVienService>();
                        await svc.DeleteAsync(dto.MaNhanVien);
                        MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await ApplyFilter();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                pnlActions.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });
            }

            Controls.Add(pnlActions);

            // ── DataGridView ───────────────────────────────────────────────────
            _dgv = UIHelper.CreateStyledDataGridView("dgvNhanVien");
            _dgv.Location = new Point(20, 143);
            _dgv.Size = new Size(Width - 40, Height - 163);
            _dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _dgv.DataBindingComplete += ApplyColumnHeaders;
            Controls.Add(_dgv);

            // ── Debounce timer cho tìm kiếm real-time ─────────────────────────
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _debounceTimer.Tick += async (_, _) =>
            {
                _debounceTimer.Stop();
                await ApplyFilter();
            };

            // ── Nạp dữ liệu ban đầu ───────────────────────────────────────────
            await LoadComboBoxData();
            _isLoading = false;
            await ApplyFilter();
        }

        private async Task LoadComboBoxData()
        {
            try
            {
                using var scope = Program.ServiceProvider.CreateScope();
                var phongBanSvc = scope.ServiceProvider.GetRequiredService<IPhongBanService>();
                var phongBans = await phongBanSvc.GetAllAsync();

                _cboPhongBan!.Items.Clear();
                _cboPhongBan.Items.Add("Tất cả phòng ban");
                foreach (var pb in phongBans)
                    _cboPhongBan.Items.Add(pb);
                _cboPhongBan.DisplayMember = "TenPhongBan";
                _cboPhongBan.SelectedIndex = 0;
            }
            catch { /* Không block load nếu lỗi phòng ban */ }
        }

        private void OnFilterChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return; // Không tìm kiếm khi đang khởi tạo giao diện

            if (sender is TextBox)
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
            else
            {
                _debounceTimer?.Stop();
                _ = ApplyFilter();
            }
        }

        private async Task ApplyFilter()
        {
            try
            {
                var keyword = _txtSearch?.Text.Trim();

                int? maPhongBan = null;
                if (_cboPhongBan?.SelectedItem is PhongBanDTO pb)
                    maPhongBan = pb.MaPhongBan;

                var trangThai = _cboTrangThai?.SelectedItem?.ToString();
                var gioiTinh = _cboGioiTinh?.SelectedItem?.ToString();

                // Tạo scope riêng → DbContext riêng → không bao giờ xung đột
                using var scope = Program.ServiceProvider.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<INhanVienService>();
                var data = await svc.FilterAsync(keyword, maPhongBan, trangThai, gioiTinh);

                if (_dgv != null)
                    _dgv.DataSource = data;

                if (_lblCount != null)
                    _lblCount.Text = $"Tìm thấy: {data.Count} nhân viên";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyColumnHeaders(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (_dgv == null) return;
            foreach (DataGridViewColumn col in _dgv.Columns)
            {
                col.MinimumWidth = 90;
                switch (col.DataPropertyName)
                {
                    case "MaNV": col.HeaderText = "Mã NV"; col.DisplayIndex = 0; col.Width = 90; break;
                    case "HoTen": col.HeaderText = "Họ và Tên"; col.MinimumWidth = 150; col.DisplayIndex = 1; break;
                    case "TenPhongBan": col.HeaderText = "Phòng Ban"; col.MinimumWidth = 120; col.DisplayIndex = 2; break;
                    case "TenChucVu": col.HeaderText = "Chức Vụ"; col.MinimumWidth = 120; col.DisplayIndex = 3; break;
                    case "MucLuong":
                        col.HeaderText = "Mức Lương";
                        col.DefaultCellStyle.Format = "N0";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        col.DisplayIndex = 4;
                        break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; col.DisplayIndex = 5; break;
                    case "SoDienThoai": col.HeaderText = "SĐT"; break;
                    case "Email": col.HeaderText = "Email"; col.MinimumWidth = 150; break;
                    case "NgaySinh": col.HeaderText = "Ngày Sinh"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "GioiTinh": col.HeaderText = "Giới Tính"; break;
                    case "NgayVaoLam": col.HeaderText = "Ngày Vào Làm"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "CCCD": col.HeaderText = "CCCD"; break;
                    case "DiaChi": col.HeaderText = "Địa Chỉ"; col.MinimumWidth = 160; break;
                    case "TinhTrangHonNhan": col.HeaderText = "Hôn Nhân"; break;
                    case "MaNhanVien":
                    case "MaPhongBan":
                    case "MaChucVu":
                    case "AnhDaiDien":
                        col.Visible = false;
                        break;
                }
            }
        }
    }
}
