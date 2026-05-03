using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Common.DTOs;
using HRM.GUI.Forms.Main.PhongVan;
using HRM.GUI.Forms.Main.HieuSuat;
using HRM.GUI.Forms.Main.TinTuyenDung;
using HRM.GUI.Forms.Main.UngVien;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace HRM.GUI.Forms.Main;

public partial class frmMain : Form
{
    /// <summary>Đặt khi người dùng bấm Đăng xuất — form đăng nhập sẽ hiện lại thay vì thoát app.</summary>
    public bool ClosedForRelogin { get; private set; }

    private readonly INhanVienService _nhanVienService;
    private readonly IPhongBanService _phongBanService;
    private readonly IPhongVanService _phongVanService;

    private readonly IChamCongService _chamCongService;
    private readonly IDonNghiPhepService _donNghiPhepService;
    private readonly IHieuSuatService _hieuSuatService;
    private readonly IBangLuongService _bangLuongService;
    private UserSessionDTO? _session;
    private System.Windows.Forms.Timer? _searchTimer;
    private bool isTuyenDungExpanded = false;
    private bool isLuongExpanded = false;

    public frmMain(
        INhanVienService nhanVienService,
        IPhongBanService phongBanService,
        IChamCongService chamCongService,
        IDonNghiPhepService donNghiPhepService,
        IHieuSuatService hieuSuatService,
        IPhongVanService phongVanService,
        ITinTuyenDungService tinTuyenDungService,
        IBangLuongService bangLuongService)
    {
        _nhanVienService = nhanVienService;
        _phongBanService = phongBanService;
        _chamCongService = chamCongService;
        _donNghiPhepService = donNghiPhepService;
        _hieuSuatService = hieuSuatService;
        _phongVanService = phongVanService;
        _bangLuongService = bangLuongService;
        InitializeComponent();
    }

    private UserControl? currentModule = null;

    private void ShowModule(UserControl newModule)
    {
        if (currentModule != null)
        {
            pnlContent.Controls.Remove(currentModule);
            currentModule.Dispose();
        }

        currentModule = newModule;
        currentModule.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(currentModule);
        currentModule.BringToFront();
    }
    public void SetSession(UserSessionDTO session)
    {
        _session = session;
        lblWelcome.Text = $"Xin chào, {session.HoTen} ({session.VaiTro})";

        SetupMenu();
    }

    private void SetupMenu()
    {
        pnlSidebar.Controls.Clear();

        int yPos = 70;

        void TaoNutMenu(string text, bool isSubMenu = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(0, yPos),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", isSubMenu ? 9 : 10),
                ForeColor = isSubMenu ? Color.FromArgb(180, 190, 200) : Color.FromArgb(200, 210, 220),
                BackColor = isSubMenu ? Color.FromArgb(40, 55, 90) : Color.FromArgb(30, 45, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(isSubMenu ? 40 : 20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 70, 110);
            btn.Click += MenuButton_Click;

            pnlSidebar.Controls.Add(btn);
            yPos += 45;
        }

        TaoNutMenu("📊 Tổng quan");

        string vaiTro = _session?.VaiTro ?? "";
        if (vaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            vaiTro.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase))
        {
            TaoNutMenu("👥 Nhân viên");
            TaoNutMenu("🏗️ Phòng ban");

            string icon = isTuyenDungExpanded ? "▼" : "▶";
            TaoNutMenu($"🤝 Tuyển dụng {icon}");

            if (isTuyenDungExpanded)
            {
                TaoNutMenu("📝 Tin tuyển dụng", true);
                TaoNutMenu("🧑‍🎓 Ứng viên", true);
                TaoNutMenu("🎤 Phỏng vấn", true);
            }

            TaoNutMenu("📈 Báo cáo");
            TaoNutMenu("📈 Hiệu suất");
            TaoNutMenu("🔑 Tài khoản");
        }

        TaoNutMenu("⏰ Chấm công");
        TaoNutMenu("📋 Nghỉ phép");

        string iconLuong = isLuongExpanded ? "▼" : "▶";
        TaoNutMenu($"💰 Lương {iconLuong}");
        if (isLuongExpanded)
        {
            TaoNutMenu("📊 Bảng lương", true);
            TaoNutMenu("📋 Thưởng phạt", true);
        }
    }

    private void btnLogout_Click(object? sender, EventArgs e)
    {
        ClosedForRelogin = true;
        Close();
    }

    private async void MenuButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string choice = btn.Text;
        if (btn.Text.Contains("Tuyển dụng") && !btn.Text.Contains("Tin"))
        {
            isTuyenDungExpanded = !isTuyenDungExpanded;
            SetupMenu();
            return;
        }

        if (btn.Text.StartsWith("💰 Lương") && !btn.Text.Contains("Bảng") && !btn.Text.Contains("Thưởng"))
        {
            isLuongExpanded = !isLuongExpanded;
            SetupMenu();
            return;
        }

        pnlContent.Controls.Clear();

        if (btn.Text.Contains("Nhân viên"))
        {
            await LoadNhanVienView();
        }
        else if (btn.Text.Contains("Phòng ban"))
        {
            await LoadPhongBanView();
        }
        else if (btn.Text.Contains("Tài khoản"))
        {
            await LoadTaiKhoanView();
        }
        else if (btn.Text.Contains("Chấm công"))
        {
            await LoadChamCongView();
        }
        else if (btn.Text.Contains("Nghỉ phép"))
        {
            await LoadNghiPhepView();
        }
        else if (btn.Text.Contains("Thưởng phạt"))
        {
            await LoadThuongPhatBangLuongView();
        }
        else if (btn.Text.Contains("Bảng lương"))
        {
            await LoadBangLuongView();
        }
        else if (btn.Text.Contains("Phỏng vấn"))
        {
            ShowModule(new ucPhongVan(this._session));
        }
        else if (btn.Text.Contains("Tin tuyển dụng"))
        {
            if (_session == null)
            {
                MessageBox.Show("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowModule(new ucTinTuyenDung(_session));
        }
        else if (btn.Text.Contains("Ứng viên"))
        {
            ShowModule(new ucUngVien(this._session));
        }
        else if (btn.Text.Contains("Hiệu suất"))
        {
            ShowModule(new frmHieuSuat(_nhanVienService, _hieuSuatService));
        }
        else
        {
            pnlContent.Controls.Add(new Label
            {
                Text = $"Module {btn.Text} - Đang phát triển...",
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(30, 30)
            });
        }
    }

    private DataGridView CreateStyledDataGridView(string name)
    {
        var dgv = new DataGridView
        {
            Name = name,
            Location = new Point(20, 60),
            Size = new Size(pnlContent.Width - 40, pnlContent.Height - 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            ColumnHeadersHeight = 45,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = new DataGridViewRow { Height = 40 },
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(230, 230, 230),
            EnableHeadersVisualStyles = false
        };

        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            SelectionBackColor = Color.FromArgb(41, 128, 185)
        };

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 60, 60),
            SelectionBackColor = Color.FromArgb(226, 239, 252),
            SelectionForeColor = Color.FromArgb(30, 30, 30),
            Font = new Font("Segoe UI", 10),
            Padding = new Padding(5, 0, 5, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 252, 255)
        };

        return dgv;
    }

    /// <summary>Lưới chấm công / nghỉ phép — cùng phong cách header xanh như bảng nhân viên.</summary>
    private static DataGridView CreateChamCongHistoryGrid(string name)
    {
        var dgv = new DataGridView
        {
            Name = name,
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            ColumnHeadersHeight = 45,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = new DataGridViewRow { Height = 40 },
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(230, 230, 230),
            EnableHeadersVisualStyles = false
        };

        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            SelectionBackColor = Color.FromArgb(41, 128, 185),
            SelectionForeColor = Color.White
        };

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 60, 60),
            SelectionBackColor = Color.FromArgb(226, 239, 252),
            SelectionForeColor = Color.FromArgb(30, 30, 30),
            Font = new Font("Segoe UI", 10),
            Padding = new Padding(5, 0, 5, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 252, 255)
        };

        return dgv;
    }

    private async Task LoadNhanVienView()
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

        var btnAdd = new Button
        {
            Text = "➕ Thêm mới",
            Location = new Point(440, 59),
            Size = new Size(100, 28),
            BackColor = Color.FromArgb(46, 204, 113), // Màu xanh lá cây
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") // Phân quyền
        };
        btnAdd.FlatAppearance.BorderSize = 0;

        var btnEdit = new Button
        {
            Text = "✏️ Sửa",
            Location = new Point(550, 59),
            Size = new Size(80, 28),
            BackColor = Color.FromArgb(241, 196, 15), // Màu vàng cam
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên")
        };
        btnEdit.FlatAppearance.BorderSize = 0;

        var btnDelete = new Button
        {
            Text = "🗑️ Xóa",
            Location = new Point(640, 59),
            Size = new Size(80, 28),
            BackColor = Color.FromArgb(231, 76, 60), // Màu đỏ
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên")
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var dgv = CreateStyledDataGridView("dgvNhanVien");
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

        btnAdd.Click += async (s, e) =>
        {
            var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.frmThemNhanVien>();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                dgv.DataSource = await _nhanVienService.GetAllAsync();
            }
        };

        btnEdit.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên cần sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgv.SelectedRows[0];
            var dto = (HRM.Common.DTOs.NhanVienDTO)row.DataBoundItem;

            var frm = new Forms.Main.frmSuaNhanVien(_nhanVienService, _phongBanService,
                Program.ServiceProvider.GetRequiredService<HRM.DAL.Repositories.IRepository<HRM.Domain.Entities.ChucVu>>(), dto);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                dgv.DataSource = await _nhanVienService.GetAllAsync();
            }
        };

        btnDelete.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgv.SelectedRows[0];
            var dto = (HRM.Common.DTOs.NhanVienDTO)row.DataBoundItem;

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa nhân viên [{dto.HoTen}] không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
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
            }
        };

        dgv.DataBindingComplete += (s, e) =>
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
                    case "MucLuong": col.HeaderText = "Mức Lương"; col.DefaultCellStyle.Format = "N0"; col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; col.DisplayIndex = 4; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; col.DisplayIndex = 5; break;
                    case "SoDienThoai": col.HeaderText = "Số Điện Thoại"; break;
                    case "Email": col.HeaderText = "Email"; col.MinimumWidth = 150; break;
                    case "NgaySinh": col.HeaderText = "Ngày Sinh"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "GioiTinh": col.HeaderText = "Giới Tính"; break;
                    case "NgayVaoLam": col.HeaderText = "Ngày Vào Làm"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "CCCD": col.HeaderText = "Số CCCD"; break;
                    case "DiaChi": col.HeaderText = "Địa Chỉ"; col.MinimumWidth = 200; break;
                    case "TinhTrangHonNhan": col.HeaderText = "Hôn Nhân"; break;
                    // Ẩn các cột bổ trợ hoặc ID
                    case "MaNhanVien": col.Visible = false; break;
                    case "MaPhongBan": col.Visible = false; break;
                    case "MaChucVu": col.Visible = false; break;
                    case "AnhDaiDien": col.Visible = false; break;
                }
            }
        };

        btnSearch.Click += async (s, e) =>
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

        // Nhấn Enter cũng chạy tìm kiếm
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(btnSearch);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

        try
        {
            var data = await _nhanVienService.GetAllAsync();
            dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadPhongBanView()
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

        var btnAdd = new Button
        {
            Text = "➕ Thêm mới",
            Location = new Point(440, 59),
            Size = new Size(100, 28),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = IsAdminSession()
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
            Visible = IsAdminSession()
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
            Visible = IsAdminSession()
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var dgv = CreateStyledDataGridView("dgvPhongBan");
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

        dgv.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 100;
                switch (col.DataPropertyName)
                {
                    case "MaPhongBan": col.HeaderText = "Mã PB"; col.Visible = false; break; // Thường ẩn ID
                    case "TenPhongBan": col.HeaderText = "Tên Phòng Ban"; col.MinimumWidth = 150; break;
                    case "MoTaChucNang": col.HeaderText = "Mô Tả Chức Năng"; col.MinimumWidth = 200; col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; break;
                    case "DiaDiemLamViec": col.HeaderText = "Địa Điểm"; col.MinimumWidth = 120; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                    case "SoNhanVien": col.HeaderText = "Số Nhân Viên"; col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; break;
                }
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(btnSearch);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

        // Events
        btnSearch.Click += async (s, e) =>
        {
            try
            {
                var kw = txtSearch.Text.Trim();
                var data = string.IsNullOrEmpty(kw) ? await _phongBanService.GetAllAsync() : await _phongBanService.SearchAsync(kw);
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

        btnAdd.Click += async (s, e) =>
        {
            using var frm = new Forms.Main.frmPhongBan(_phongBanService);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                dgv.DataSource = await _phongBanService.GetAllAsync();
            }
        };

        btnEdit.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            var dto = dgv.SelectedRows[0].DataBoundItem as HRM.Common.DTOs.PhongBanDTO;
            if (dto == null) return;

            using var frm = new Forms.Main.frmPhongBan(_phongBanService, dto);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                dgv.DataSource = await _phongBanService.GetAllAsync();
            }
        };

        btnDelete.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            var dto = dgv.SelectedRows[0].DataBoundItem as HRM.Common.DTOs.PhongBanDTO;
            if (dto == null) return;

            if (dto.SoNhanVien > 0)
            {
                MessageBox.Show($"Không thể xóa phòng ban có nhân viên ({dto.SoNhanVien} người).", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng ban '{dto.TenPhongBan}' không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
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
            }
        };

        try
        {
            var data = await _phongBanService.GetAllAsync();
            dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadBangLuongView()
    {
        if (_session == null) return;

        var isAdmin = IsAdminSession();
        var now = DateTime.Now;

        var lblTitle = new Label
        {
            Text = isAdmin ? "Bảng lương — quản trị" : "Bảng lương của tôi",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 55, 95),
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblThang = new Label { Text = "Tháng:", Location = new Point(20, 52), AutoSize = true };
        var numThang = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 12,
            Value = now.Month,
            Location = new Point(75, 48),
            Width = 55
        };
        var lblNam = new Label { Text = "Năm:", Location = new Point(150, 52), AutoSize = true };
        var numNam = new NumericUpDown
        {
            Minimum = 2000,
            Maximum = 2100,
            Value = now.Year,
            Location = new Point(195, 48),
            Width = 75
        };

        var btnTinh = new Button
        {
            Text = "🧮 Tính lương tháng",
            Location = new Point(300, 45),
            Size = new Size(160, 32),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Visible = isAdmin
        };
        btnTinh.FlatAppearance.BorderSize = 0;

        var btnReload = new Button
        {
            Text = "🔄 Tải lại",
            Location = new Point(470, 45),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var lblHint = new Label
        {
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 100, 110),
            AutoSize = true,
            Location = new Point(20, 82),
            MaximumSize = new Size(pnlContent.Width - 40, 0)
        };

        var dgv = CreateStyledDataGridView("dgvBangLuong");
        if (isAdmin)
        {
            dgv.ReadOnly = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.EditMode = DataGridViewEditMode.EditOnEnter;
        }
        dgv.Location = new Point(20, 118);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 138);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

        dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.ReadOnly = true;
                switch (col.DataPropertyName)
                {
                    case "MaBangLuong": col.HeaderText = "Mã BL"; col.Visible = false; break;
                    case "MaNhanVien": col.HeaderText = "Mã NV"; col.Visible = isAdmin; break;
                    case "TenNhanVien": col.HeaderText = "Nhân viên"; col.MinimumWidth = 140; break;
                    case "Thang": col.HeaderText = "Tháng"; break;
                    case "Nam": col.HeaderText = "Năm"; break;
                    case "LuongCoBan": col.HeaderText = "Lương CB"; col.DefaultCellStyle.Format = "N0"; break;
                    case "TongPhuCap": col.HeaderText = "Phụ cấp"; col.DefaultCellStyle.Format = "N0"; break;
                    case "SoNgayLamViec": col.HeaderText = "Ngày công"; break;
                    case "SoGioLamThem": col.HeaderText = "Giờ OT"; col.DefaultCellStyle.Format = "N2"; break;
                    case "TienLamThem": col.HeaderText = "Tiền OT"; col.DefaultCellStyle.Format = "N0"; break;
                    case "TongThuong": col.HeaderText = "Thưởng (TC)"; col.DefaultCellStyle.Format = "N0"; break;
                    case "TongPhat": col.HeaderText = "Phạt (TC)"; col.DefaultCellStyle.Format = "N0"; break;
                    case "BHXH": col.HeaderText = "BHXH"; col.DefaultCellStyle.Format = "N0"; break;
                    case "BHYT": col.HeaderText = "BHYT"; col.DefaultCellStyle.Format = "N0"; break;
                    case "BHTN": col.HeaderText = "BHTN"; col.DefaultCellStyle.Format = "N0"; break;
                    case "ThueTNCN": col.HeaderText = "Thuế TNCN"; col.DefaultCellStyle.Format = "N0"; break;
                    case "TongThuNhap": col.HeaderText = "Tổng thu nhập"; col.DefaultCellStyle.Format = "N0"; break;
                    case "TongKhauTru": col.HeaderText = "Tổng khấu trừ"; col.DefaultCellStyle.Format = "N0"; break;
                    case "LuongThucNhan": col.HeaderText = "Thực nhận"; col.DefaultCellStyle.Format = "N0"; col.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold); break;
                    case "NgayTinhLuong": col.HeaderText = "Ngày tính"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                    case "TrangThai": col.Visible = false; break;
                }

                if (isAdmin && (col.DataPropertyName == "TongThuong" || col.DataPropertyName == "TongPhat"))
                    col.ReadOnly = false;
            }
        };

        var savingThuongPhat = false;
        static decimal ParseBangLuongMoney(object? v)
        {
            if (v == null || Convert.IsDBNull(v)) return 0m;
            if (v is decimal d) return d;
            var s = Convert.ToString(v, CultureInfo.CurrentCulture)?.Replace("\u00A0", "").Trim();
            if (string.IsNullOrEmpty(s)) return 0m;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var x)) return x;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out x)) return x;
            return 0m;
        }

        dgv.CellEndEdit += async (_, e) =>
        {
            if (!isAdmin || savingThuongPhat || e.RowIndex < 0) return;
            var prop = dgv.Columns[e.ColumnIndex].DataPropertyName;
            if (prop != "TongThuong" && prop != "TongPhat") return;
            if (dgv.Rows[e.RowIndex].DataBoundItem is not BangLuongDTO dto) return;

            decimal? cellVal(string name)
            {
                foreach (DataGridViewColumn c in dgv.Columns)
                {
                    if (c.DataPropertyName != name) continue;
                    return ParseBangLuongMoney(dgv.Rows[e.RowIndex].Cells[c.Index].Value);
                }
                return null;
            }

            var thuong = cellVal("TongThuong") ?? dto.TongThuong;
            var phat = cellVal("TongPhat") ?? dto.TongPhat;

            savingThuongPhat = true;
            try
            {
                await _bangLuongService.CapNhatThuongPhatVaTinhLaiAsync(dto.MaBangLuong, thuong, phat);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không lưu được thưởng/phạt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await ReloadAsync();
            }
            finally
            {
                savingThuongPhat = false;
            }
        };

        async Task ReloadAsync()
        {
            try
            {
                var thang = (int)numThang.Value;
                var nam = (int)numNam.Value;
                var list = await _bangLuongService.GetBangLuongAsync(thang, nam, isAdmin, _session.MaNhanVien);
                dgv.DataSource = null;
                dgv.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bảng lương: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        btnReload.Click += async (_, _) => await ReloadAsync();
        btnTinh.Click += async (_, _) =>
        {
            if (!isAdmin) return;
            var thang = (int)numThang.Value;
            var nam = (int)numNam.Value;
            if (MessageBox.Show(
                    $"Tính lại lương cho mọi nhân viên đang làm việc — tháng {thang}/{nam}?\nDữ liệu bảng lương tháng này sẽ được ghi đè.\nThưởng/phạt thủ công sẽ về 0 — cần nhập lại trên lưới.",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                var n = await _bangLuongService.TinhVaLuuBangLuongThangAsync(thang, nam);
                MessageBox.Show($"Đã tính và lưu {n} bản ghi.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(lblThang);
        pnlContent.Controls.Add(numThang);
        pnlContent.Controls.Add(lblNam);
        pnlContent.Controls.Add(numNam);
        pnlContent.Controls.Add(btnTinh);
        pnlContent.Controls.Add(btnReload);
        pnlContent.Controls.Add(lblHint);
        pnlContent.Controls.Add(dgv);

        await ReloadAsync();
    }

    /// <summary>Màn hình riêng: chỉ nhập thưởng/phạt (cùng dữ liệu bảng lương đã tính).</summary>
    private async Task LoadThuongPhatBangLuongView()
    {
        if (_session == null) return;

        var isAdmin = IsAdminSession();
        var now = DateTime.Now;

        var lblTitle = new Label
        {
            Text = isAdmin ? "Thưởng / phạt" : "Thưởng / phạt trên lương của tôi",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 55, 95),
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblThang = new Label { Text = "Tháng:", Location = new Point(20, 52), AutoSize = true };
        var numThang = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 12,
            Value = now.Month,
            Location = new Point(75, 48),
            Width = 55
        };
        var lblNam = new Label { Text = "Năm:", Location = new Point(150, 52), AutoSize = true };
        var numNam = new NumericUpDown
        {
            Minimum = 2000,
            Maximum = 2100,
            Value = now.Year,
            Location = new Point(195, 48),
            Width = 75
        };

        var btnReload = new Button
        {
            Text = "🔄 Tải lại",
            Location = new Point(300, 45),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var lblEditHint = new Label
        {
            Visible = isAdmin,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(30, 100, 160),
            AutoSize = true,
            Location = new Point(20, 82)
        };

        var dgv = CreateStyledDataGridView("dgvThuongPhatLuong");
        if (isAdmin)
        {
            dgv.ReadOnly = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.EditMode = DataGridViewEditMode.EditOnEnter;
        }
        dgv.Location = new Point(20, isAdmin ? 108 : 88);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - (isAdmin ? 128 : 108));
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.ReadOnly = true;
                switch (col.DataPropertyName)
                {
                    case "MaBangLuong": col.HeaderText = "Mã BL"; col.Visible = false; break;
                    case "MaNhanVien": col.HeaderText = "Mã NV"; col.Visible = isAdmin; break;
                    case "TenNhanVien": col.HeaderText = "Nhân viên"; col.MinimumWidth = 160; break;
                    case "Thang": col.HeaderText = "Tháng"; col.FillWeight = 50; break;
                    case "Nam": col.HeaderText = "Năm"; col.FillWeight = 50; break;
                    case "TongThuong": col.HeaderText = "Thưởng (VNĐ)"; col.DefaultCellStyle.Format = "N0"; col.FillWeight = 90; break;
                    case "TongPhat": col.HeaderText = "Phạt (VNĐ)"; col.DefaultCellStyle.Format = "N0"; col.FillWeight = 90; break;
                    case "LuongThucNhan": col.HeaderText = "Thực nhận (sau thuế)"; col.DefaultCellStyle.Format = "N0"; col.FillWeight = 100; break;
                    case "TrangThai": col.Visible = false; break;
                    default: col.Visible = false; break;
                }

                if (isAdmin && (col.DataPropertyName == "TongThuong" || col.DataPropertyName == "TongPhat"))
                    col.ReadOnly = false;
            }
        };

        var savingThuongPhat = false;
        static decimal ParseTpMoney(object? v)
        {
            if (v == null || Convert.IsDBNull(v)) return 0m;
            if (v is decimal d) return d;
            var s = Convert.ToString(v, CultureInfo.CurrentCulture)?.Replace("\u00A0", "").Trim();
            if (string.IsNullOrEmpty(s)) return 0m;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var x)) return x;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out x)) return x;
            return 0m;
        }

        dgv.CellEndEdit += async (_, e) =>
        {
            if (!isAdmin || savingThuongPhat || e.RowIndex < 0) return;
            var prop = dgv.Columns[e.ColumnIndex].DataPropertyName;
            if (prop != "TongThuong" && prop != "TongPhat") return;
            if (dgv.Rows[e.RowIndex].DataBoundItem is not BangLuongDTO dto) return;

            decimal GetCell(string name)
            {
                foreach (DataGridViewColumn c in dgv.Columns)
                {
                    if (c.DataPropertyName != name) continue;
                    return ParseTpMoney(dgv.Rows[e.RowIndex].Cells[c.Index].Value);
                }
                return 0m;
            }

            var thuong = GetCell("TongThuong");
            var phat = GetCell("TongPhat");

            savingThuongPhat = true;
            try
            {
                await _bangLuongService.CapNhatThuongPhatVaTinhLaiAsync(dto.MaBangLuong, thuong, phat);
                await ReloadTpAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không lưu được thưởng/phạt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await ReloadTpAsync();
            }
            finally
            {
                savingThuongPhat = false;
            }
        };

        async Task ReloadTpAsync()
        {
            try
            {
                var thang = (int)numThang.Value;
                var nam = (int)numNam.Value;
                var list = await _bangLuongService.GetBangLuongAsync(thang, nam, isAdmin, _session.MaNhanVien);
                dgv.DataSource = null;
                dgv.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        btnReload.Click += async (_, _) => await ReloadTpAsync();

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(lblThang);
        pnlContent.Controls.Add(numThang);
        pnlContent.Controls.Add(lblNam);
        pnlContent.Controls.Add(numNam);
        pnlContent.Controls.Add(btnReload);
        if (isAdmin)
            pnlContent.Controls.Add(lblEditHint);
        pnlContent.Controls.Add(dgv);

        await ReloadTpAsync();
    }

    private bool IsAdminSession()
    {
        var v = _session?.VaiTro ?? "";
        return v.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadChamCongView()
    {
        if (_session == null) return;

        var isAdmin = IsAdminSession();
        var cardBorder = Color.FromArgb(215, 222, 232);

        var lblModuleTitle = new Label
        {
            Text = isAdmin
                ? "Hệ thống chấm công HRM — Quản trị (toàn công ty)"
                : "Hệ thống chấm công HRM",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 55, 95),
            AutoSize = true,
            Location = new Point(20, 12)
        };

        const int quickTop = 46;
        const int quickHeight = 146;
        var histTop = isAdmin ? quickTop : quickTop + quickHeight + 14;

        var pnlQuickOuter = new Panel
        {
            Location = new Point(20, quickTop),
            Height = quickHeight,
            Width = Math.Max(480, pnlContent.Width - 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = cardBorder,
            Padding = new Padding(1)
        };

        var pnlQuick = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        var lblQuickSection = new Label
        {
            Text = "Thao tác nhanh",
            Location = new Point(18, 12),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 80)
        };

        var lblClock = new Label
        {
            Text = DateTime.Now.ToString("HH:mm:ss"),
            Location = new Point(18, 40),
            AutoSize = true,
            ForeColor = Color.FromArgb(30, 55, 90)
        };
        try
        {
            lblClock.Font = new Font("Consolas", 28f, FontStyle.Bold);
        }
        catch
        {
            lblClock.Font = new Font("Segoe UI", 26f, FontStyle.Bold);
        }

        var lblStatus = new Label
        {
            Text = "Đang tải...",
            Location = new Point(18, 96),
            Height = 52,
            AutoSize = false,
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(80, 88, 100)
        };

        var btnCheckIn = new Button
        {
            Text = "👍\r\nVÀO CA",
            Size = new Size(76, 44),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(2)
        };
        btnCheckIn.FlatAppearance.BorderSize = 0;

        var btnTanCa = new Button
        {
            Text = "⏱\r\nTAN CA",
            Size = new Size(76, 44),
            BackColor = Color.FromArgb(230, 126, 52),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(2)
        };
        btnTanCa.FlatAppearance.BorderSize = 0;

        var lblQuickFooter = new Label
        {
            Text = "Vào trước 08:30, ra sau 17:00",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = Color.FromArgb(140, 148, 160)
        };

        void LayoutQuickCard()
        {
            var pad = 18;
            var btnY = 44;
            btnTanCa.Location = new Point(pnlQuick.Width - pad - btnTanCa.Width, btnY);
            btnCheckIn.Location = new Point(btnTanCa.Left - 8 - btnCheckIn.Width, btnY);
            lblStatus.Width = Math.Max(220, btnCheckIn.Left - 28);
            lblQuickFooter.Location = new Point(pnlQuick.Width - lblQuickFooter.Width - pad, pnlQuick.Height - 22);
        }

        pnlQuick.Resize += (_, _) => LayoutQuickCard();

        pnlQuickOuter.Visible = !isAdmin;

        if (!isAdmin)
        {
            var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (_, _) =>
            {
                if (lblClock.IsDisposed)
                {
                    clockTimer.Stop();
                    clockTimer.Dispose();
                    return;
                }

                lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            clockTimer.Start();
        }

        pnlQuick.Controls.Add(lblQuickSection);
        pnlQuick.Controls.Add(lblClock);
        pnlQuick.Controls.Add(lblStatus);
        pnlQuick.Controls.Add(btnCheckIn);
        pnlQuick.Controls.Add(btnTanCa);
        pnlQuick.Controls.Add(lblQuickFooter);
        pnlQuickOuter.Controls.Add(pnlQuick);
        LayoutQuickCard();

        var pnlHistOuter = new Panel
        {
            Location = new Point(20, histTop),
            Size = new Size(pnlContent.Width - 40, Math.Max(200, pnlContent.ClientSize.Height - histTop - 16)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = cardBorder,
            Padding = new Padding(1)
        };

        var pnlHistInner = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        var pnlHistHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 0)
        };
        var lblHistTitle = new Label
        {
            Text = isAdmin ? "Lịch sử & Tra cứu — Toàn công ty" : "Lịch sử & Tra cứu",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 80),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            BackColor = Color.Transparent
        };
        pnlHistHead.Controls.Add(lblHistTitle);

        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        pnlFilter.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(232, 236, 242));
            e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
        };

        var flowFilter = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        var lblTu = new Label
        {
            Text = "Từ ngày",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 110, 125),
            Margin = new Padding(0, 8, 6, 0)
        };
        var dtpTu = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 128,
            Font = new Font("Segoe UI", 9.5f),
            Margin = new Padding(0, 4, 18, 0),
            Value = DateTime.Today.AddDays(-30)
        };

        var lblDen = new Label
        {
            Text = "Đến ngày",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 110, 125),
            Margin = new Padding(0, 8, 6, 0)
        };
        var dtpDen = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 128,
            Font = new Font("Segoe UI", 9.5f),
            Margin = new Padding(0, 4, 18, 0),
            Value = DateTime.Today
        };

        var btnLoadHistory = new Button
        {
            Text = "🔄  Tải lịch sử",
            Size = new Size(132, 32),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Margin = new Padding(4, 4, 0, 0)
        };
        btnLoadHistory.FlatAppearance.BorderSize = 0;
        var btnLoadBase = Color.FromArgb(41, 128, 185);
        var btnLoadHover = Color.FromArgb(52, 152, 219);
        btnLoadHistory.MouseEnter += (_, _) => btnLoadHistory.BackColor = btnLoadHover;
        btnLoadHistory.MouseLeave += (_, _) => btnLoadHistory.BackColor = btnLoadBase;

        Button? btnSuaChamCong = null;
        if (isAdmin)
        {
            var btnSuaBase = Color.FromArgb(241, 196, 15);
            var btnSuaHover = Color.FromArgb(243, 209, 73);
            btnSuaChamCong = new Button
            {
                Text = "✏️  Sửa bản ghi",
                Size = new Size(132, 32),
                BackColor = btnSuaBase,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Margin = new Padding(8, 4, 0, 0)
            };
            btnSuaChamCong.FlatAppearance.BorderSize = 0;
            btnSuaChamCong.MouseEnter += (_, _) => btnSuaChamCong!.BackColor = btnSuaHover;
            btnSuaChamCong.MouseLeave += (_, _) => btnSuaChamCong!.BackColor = btnSuaBase;
        }

        flowFilter.Controls.Add(lblTu);
        flowFilter.Controls.Add(dtpTu);
        flowFilter.Controls.Add(lblDen);
        flowFilter.Controls.Add(dtpDen);
        flowFilter.Controls.Add(btnLoadHistory);
        if (btnSuaChamCong != null)
            flowFilter.Controls.Add(btnSuaChamCong);
        pnlFilter.Controls.Add(flowFilter);

        var dgv = CreateChamCongHistoryGrid("dgvChamCong");

        pnlHistInner.Controls.Add(dgv);
        pnlHistInner.Controls.Add(pnlFilter);
        pnlHistInner.Controls.Add(pnlHistHead);
        pnlHistOuter.Controls.Add(pnlHistInner);

        WireChamCongHistoryTimeCellFormatting(dgv);
        dgv.DataBindingComplete += (_, _) => ApplyChamCongHistoryColumns(dgv, isAdmin);

        async Task TryOpenChamCongEditAsync(ChamCongDTO? rowDto = null)
        {
            if (!isAdmin)
                return;

            var dto = rowDto;
            if (dto == null)
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn một dòng chấm công cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                dto = dgv.SelectedRows[0].DataBoundItem as ChamCongDTO;
            }

            if (dto == null)
                return;

            using var dlg = new frmSuaChamCong(_chamCongService, dto);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadGridAsync();
        }

        if (isAdmin)
        {
            dgv.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (dgv.Rows[e.RowIndex].DataBoundItem is not ChamCongDTO dto)
                    return;
                await TryOpenChamCongEditAsync(dto);
            };
            btnSuaChamCong!.Click += async (_, _) => await TryOpenChamCongEditAsync();
        }

        async Task RefreshTodayAsync()
        {
            if (isAdmin)
                return;

            try
            {
                if (await _chamCongService.HasApprovedLeaveOnDateAsync(_session.MaNhanVien, DateTime.Today))
                {
                    lblStatus.Text = "Hôm nay: Bạn có đơn nghỉ phép đã duyệt — không cần chấm công vào/tan ca.";
                    btnCheckIn.Enabled = false;
                    btnTanCa.Enabled = false;
                    return;
                }

                var today = await _chamCongService.GetTodayAsync(_session.MaNhanVien);
                if (today == null)
                {
                    lblStatus.Text = "Hôm nay: Bạn chưa chấm công. Nhấn \"VÀO CA\" để bắt đầu làm việc.";
                    btnCheckIn.Enabled = true;
                    btnTanCa.Enabled = false;
                }
                else if (today.GioRa == null)
                {
                    var vao = today.GioVao?.ToString(@"hh\:mm") ?? "--";
                    lblStatus.Text = $"Hôm nay: Đã vào ca lúc {vao}. Trạng thái: {today.TrangThai}.\r\nNhấn \"TAN CA\" khi kết thúc làm việc.";
                    btnCheckIn.Enabled = false;
                    btnTanCa.Enabled = true;
                }
                else
                {
                    var vao = today.GioVao?.ToString(@"hh\:mm") ?? "--";
                    var ra = today.GioRa?.ToString(@"hh\:mm") ?? "--";
                    var tong = today.TongGioLam.HasValue ? $"{today.TongGioLam:N2} giờ" : "--";
                    lblStatus.Text = $"Hôm nay: Đã hoàn tất chấm công. Vào {vao} — Ra {ra} — Tổng {tong}. Trạng thái: {today.TrangThai}.";
                    btnCheckIn.Enabled = false;
                    btnTanCa.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi: " + ex.Message;
            }
        }

        async Task LoadGridAsync()
        {
            try
            {
                List<ChamCongDTO> data;
                if (isAdmin)
                    data = await _chamCongService.GetAllInPeriodAsync(dtpTu.Value, dtpDen.Value);
                else
                    data = await _chamCongService.GetHistoryAsync(_session.MaNhanVien, dtpTu.Value, dtpDen.Value);
                dgv.DataSource = null;
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        btnCheckIn.Click += async (_, _) =>
        {
            try
            {
                var result = await _chamCongService.CheckInAsync(_session.MaNhanVien);
                if (result == null)
                    MessageBox.Show("Hôm nay bạn đã chấm công vào rồi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"Chấm vào thành công lúc {result.GioVao?.ToString(@"hh\:mm")}. Trạng thái: {result.TrangThai}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshTodayAsync();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnTanCa.Click += async (_, _) =>
        {
            try
            {
                var result = await _chamCongService.CheckOutAsync(_session.MaNhanVien);
                if (result == null)
                    MessageBox.Show("Không thể tan ca: chưa có bản ghi vào hôm nay hoặc đã chấm ra.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show($"Tan ca thành công. Tổng giờ làm: {result.TongGioLam:N2}. Trạng thái: {result.TrangThai}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshTodayAsync();
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnLoadHistory.Click += async (_, _) => await LoadGridAsync();

        pnlContent.Controls.Add(lblModuleTitle);
        pnlContent.Controls.Add(pnlQuickOuter);
        pnlContent.Controls.Add(pnlHistOuter);

        await RefreshTodayAsync();
        await LoadGridAsync();
    }

    /// <summary>Admin / Quản trị: chỉ tra cứu toàn bộ đơn và Duyệt / Từ chối đơn chờ duyệt.</summary>
    private async Task LoadNghiPhepAdminViewAsync()
    {
        if (_session == null) return;

        var cardBorder = Color.FromArgb(215, 222, 232);

        var lblTitle = new Label
        {
            Text = "📋 Duyệt & tra cứu nghỉ phép (Quản trị)",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 55, 95),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var chkTheoNgay = new CheckBox
        {
            Text = "Lọc theo khoảng ngày nghỉ",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.25f),
            Margin = new Padding(0, 6, 16, 4)
        };
        var lblTu = new Label { Text = "Từ", AutoSize = true, Font = new Font("Segoe UI Semibold", 9f), Margin = new Padding(0, 10, 6, 0) };
        var dtpTu = new DateTimePicker
        {
            Width = 118,
            Enabled = false,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.25f),
            Margin = new Padding(0, 4, 12, 0),
            Value = DateTime.Today.AddMonths(-1)
        };
        var lblDen = new Label { Text = "Đến", AutoSize = true, Font = new Font("Segoe UI Semibold", 9f), Margin = new Padding(0, 10, 6, 0) };
        var dtpDen = new DateTimePicker
        {
            Width = 118,
            Enabled = false,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.25f),
            Margin = new Padding(0, 4, 12, 0),
            Value = DateTime.Today
        };
        var lblKw = new Label { Text = "Nhân viên", AutoSize = true, Font = new Font("Segoe UI Semibold", 9f), Margin = new Padding(0, 10, 6, 0) };
        var txtKeyword = new TextBox
        {
            Width = 280,
            Font = new Font("Segoe UI", 9.25f),
            Margin = new Padding(0, 4, 12, 0),
            PlaceholderText = "Tên, mã NV, mã hệ thống..."
        };

        chkTheoNgay.CheckedChanged += (_, _) =>
        {
            dtpTu.Enabled = dtpDen.Enabled = chkTheoNgay.Checked;
        };

        var flowFilter = new FlowLayoutPanel
        {
            Location = new Point(20, 60),
            Width = Math.Max(400, pnlContent.Width - 40),
            Height = 96,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        flowFilter.Controls.Add(chkTheoNgay);
        flowFilter.Controls.Add(lblTu);
        flowFilter.Controls.Add(dtpTu);
        flowFilter.Controls.Add(lblDen);
        flowFilter.Controls.Add(dtpDen);
        flowFilter.Controls.Add(lblKw);
        flowFilter.Controls.Add(txtKeyword);

        var btnTim = new Button
        {
            Text = "🔍 Tra cứu",
            Size = new Size(118, 32),
            Margin = new Padding(0, 2, 8, 0),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold)
        };
        btnTim.FlatAppearance.BorderSize = 0;

        var btnReset = new Button
        {
            Text = "🔄 Xóa lọc",
            Size = new Size(104, 32),
            Margin = new Padding(0, 2, 0, 0),
            BackColor = Color.FromArgb(236, 240, 244),
            ForeColor = Color.FromArgb(55, 65, 80),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.25f)
        };
        btnReset.FlatAppearance.BorderSize = 1;
        btnReset.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 220);

        flowFilter.Controls.Add(btnTim);
        flowFilter.Controls.Add(btnReset);

        const int adminHeaderH = 52;
        const int adminFlowTop = 60;
        const int adminFlowH = 96;
        const int adminGridTop = adminFlowTop + adminFlowH + 8;
        const int adminGridBottomPad = 20;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = adminHeaderH,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = pnlContent.BackColor
        };
        var tblHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = pnlContent.BackColor
        };
        tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238f));
        tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var flowApprove = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = pnlContent.BackColor,
            Padding = new Padding(0, 2, 0, 0)
        };

        var dgvAll = CreateChamCongHistoryGrid("dgvDonNghiPhepAdmin");
        dgvAll.Dock = DockStyle.Fill;

        var btnDuyet = new Button
        {
            Text = "✔ Duyệt",
            Size = new Size(110, 32),
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        btnDuyet.FlatAppearance.BorderSize = 0;
        var btnTuChoi = new Button
        {
            Text = "✖ Từ chối",
            Size = new Size(110, 32),
            Margin = new Padding(0),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        btnTuChoi.FlatAppearance.BorderSize = 0;
        flowApprove.Controls.Add(btnDuyet);
        flowApprove.Controls.Add(btnTuChoi);
        tblHeader.Controls.Add(lblTitle, 0, 0);
        tblHeader.Controls.Add(flowApprove, 1, 0);
        pnlHeader.Controls.Add(tblHeader);

        var pnlGridOuter = new Panel
        {
            Location = new Point(20, adminGridTop),
            Size = new Size(pnlContent.Width - 40, Math.Max(240, pnlContent.ClientSize.Height - adminGridTop - adminGridBottomPad)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = cardBorder,
            Padding = new Padding(1)
        };
        var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        dgvAll.DataBindingComplete += (_, _) => ApplyDonNghiPhepGridColumns(dgvAll);

        void UpdateActionButtons()
        {
            btnDuyet.Enabled = false;
            btnTuChoi.Enabled = false;
            if (dgvAll.SelectedRows.Count == 0) return;
            if (dgvAll.SelectedRows[0].DataBoundItem is not DonNghiPhepDTO dto) return;
            if (dto.TrangThai == DonNghiPhepTrangThai.ChoDuyet)
            {
                btnDuyet.Enabled = true;
                btnTuChoi.Enabled = true;
            }
        }

        dgvAll.SelectionChanged += (_, _) => UpdateActionButtons();

        async Task ReloadAsync()
        {
            DateTime? tu = chkTheoNgay.Checked ? dtpTu.Value.Date : null;
            DateTime? den = chkTheoNgay.Checked ? dtpDen.Value.Date : null;
            var kw = txtKeyword.Text.Trim();
            if (kw.Length == 0) kw = null;
            try
            {
                var data = await _donNghiPhepService.GetAllTraCuuAsync(kw, tu, den);
                dgvAll.DataSource = null;
                dgvAll.DataSource = data;
                UpdateActionButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        btnTim.Click += async (_, _) => await ReloadAsync();
        btnReset.Click += async (_, _) =>
        {
            txtKeyword.Clear();
            chkTheoNgay.Checked = false;
            await ReloadAsync();
        };

        btnDuyet.Click += async (_, _) =>
        {
            if (dgvAll.SelectedRows.Count == 0 || dgvAll.SelectedRows[0].DataBoundItem is not DonNghiPhepDTO dto)
                return;
            if (dto.TrangThai != DonNghiPhepTrangThai.ChoDuyet) return;
            if (MessageBox.Show($"Duyệt đơn nghỉ của {dto.TenNhanVien ?? "NV"}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                await _donNghiPhepService.ApproveAsync(dto.MaDonPhep, _session.MaNhanVien);
                MessageBox.Show("Đã duyệt đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnTuChoi.Click += async (_, _) =>
        {
            if (dgvAll.SelectedRows.Count == 0 || dgvAll.SelectedRows[0].DataBoundItem is not DonNghiPhepDTO dto)
                return;
            if (dto.TrangThai != DonNghiPhepTrangThai.ChoDuyet) return;
            var ld = PromptSingleLine(this, "Từ chối đơn", "Lý do từ chối:");
            if (string.IsNullOrWhiteSpace(ld)) return;
            try
            {
                await _donNghiPhepService.RejectAsync(dto.MaDonPhep, _session.MaNhanVien, ld);
                MessageBox.Show("Đã từ chối đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        pnlGridInner.Controls.Add(dgvAll);
        pnlGridOuter.Controls.Add(pnlGridInner);

        pnlContent.Controls.Add(pnlHeader);
        pnlContent.Controls.Add(flowFilter);
        pnlContent.Controls.Add(pnlGridOuter);

        await ReloadAsync();
    }

    private async Task LoadNghiPhepView()
    {
        if (_session == null) return;
        if (IsAdminSession())
        {
            await LoadNghiPhepAdminViewAsync();
            return;
        }

        var cardBorder = Color.FromArgb(215, 222, 232);

        var lblTitle = new Label
        {
            Text = "📋 Quản lý nghỉ phép",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 55, 95),
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var btnTaoDon = new Button
        {
            Text = "➕ Tạo đơn nghỉ",
            Size = new Size(140, 34),
            Location = new Point(20, 46),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
        };
        btnTaoDon.FlatAppearance.BorderSize = 0;

        var btnHuyDon = new Button
        {
            Text = "✖ Hủy đơn (chờ duyệt)",
            Size = new Size(180, 34),
            Location = new Point(168, 46),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
        };
        btnHuyDon.FlatAppearance.BorderSize = 0;

        const int myGridTop = 92;
        var pnlMineOuter = new Panel
        {
            Location = new Point(20, myGridTop),
            Size = new Size(pnlContent.Width - 40, Math.Max(220, pnlContent.ClientSize.Height - myGridTop - 20)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = cardBorder,
            Padding = new Padding(1)
        };
        var pnlMineInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var dgvMine = CreateChamCongHistoryGrid("dgvDonCuaToi");
        dgvMine.Dock = DockStyle.Fill;

        dgvMine.DataBindingComplete += (_, _) => ApplyDonNghiPhepGridColumns(dgvMine);

        pnlMineInner.Controls.Add(dgvMine);
        pnlMineOuter.Controls.Add(pnlMineInner);

        async Task ReloadMineAsync()
        {
            try
            {
                var list = await _donNghiPhepService.GetByNhanVienAsync(_session.MaNhanVien);
                dgvMine.DataSource = null;
                dgvMine.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải đơn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        btnTaoDon.Click += async (_, _) =>
        {
            try
            {
                var loai = await _donNghiPhepService.GetLoaiNghiPhepAsync();
                using var dlg = new Form
                {
                    Text = "Tạo đơn nghỉ phép",
                    Width = 440,
                    Height = 320,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var lblL = new Label { Text = "Loại nghỉ", Left = 16, Top = 16, Width = 120 };
                var cbo = new ComboBox
                {
                    Left = 16,
                    Top = 40,
                    Width = 392,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = nameof(LoaiNghiPhepDTO.TenLoaiPhep),
                    ValueMember = nameof(LoaiNghiPhepDTO.MaLoaiPhep),
                    DataSource = loai
                };
                var lblA = new Label { Text = "Từ ngày", Left = 16, Top = 78, Width = 100 };
                var dtpTu = new DateTimePicker { Left = 16, Top = 100, Width = 180, Format = DateTimePickerFormat.Short };
                var lblB = new Label { Text = "Đến ngày", Left = 220, Top = 78, Width = 100 };
                var dtpDen = new DateTimePicker { Left = 220, Top = 100, Width = 188, Format = DateTimePickerFormat.Short };
                var lblR = new Label { Text = "Lý do", Left = 16, Top = 136, Width = 100 };
                var txtLyDo = new TextBox { Left = 16, Top = 158, Width = 392, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical };
                var btnOk = new Button { Text = "Gửi đơn", DialogResult = DialogResult.OK, Left = 220, Top = 252, Width = 92 };
                var btnCancel = new Button { Text = "Đóng", DialogResult = DialogResult.Cancel, Left = 316, Top = 252, Width = 92 };
                dlg.Controls.AddRange(new Control[] { lblL, cbo, lblA, dtpTu, lblB, dtpDen, lblR, txtLyDo, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                if (cbo.SelectedItem is not LoaiNghiPhepDTO sel)
                    return;
                await _donNghiPhepService.CreateAsync(_session.MaNhanVien, sel.MaLoaiPhep, dtpTu.Value, dtpDen.Value, txtLyDo.Text);
                MessageBox.Show("Đã gửi đơn nghỉ phép.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await ReloadMineAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể tạo đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        btnHuyDon.Click += async (_, _) =>
        {
            if (dgvMine.SelectedRows.Count == 0)
            {
                MessageBox.Show("Chọn một đơn để hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (dgvMine.SelectedRows[0].DataBoundItem is not DonNghiPhepDTO dto)
                return;
            if (dto.TrangThai != "Chờ duyệt")
            {
                MessageBox.Show("Chỉ hủy được đơn đang chờ duyệt.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var lyDo = PromptSingleLine(this, "Hủy đơn nghỉ", "Lý do hủy (tùy chọn):");
            if (lyDo == null) return;
            try
            {
                await _donNghiPhepService.CancelAsync(dto.MaDonPhep, _session.MaNhanVien, lyDo);
                MessageBox.Show("Đã hủy đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await ReloadMineAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(btnTaoDon);
        pnlContent.Controls.Add(btnHuyDon);
        pnlContent.Controls.Add(pnlMineOuter);

        await ReloadMineAsync();
    }

    /// <summary>Trả về null nếu người dùng đóng hộp thoại; chuỗi rỗng nếu bấm OK không nhập gì.</summary>
    private static string? PromptSingleLine(IWin32Window owner, string title, string caption)
    {
        using var f = new Form
        {
            Text = title,
            Width = 440,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lbl = new Label { Text = caption, Left = 14, Top = 14, Width = 400, AutoSize = false, Height = 36 };
        var txt = new TextBox { Left = 14, Top = 50, Width = 392 };
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 230, Top = 86, Width = 80 };
        var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 318, Top = 86, Width = 80 };
        f.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
        f.AcceptButton = btnOk;
        f.CancelButton = btnCancel;
        return f.ShowDialog(owner) == DialogResult.OK ? txt.Text : null;
    }

    private async Task LoadTaiKhoanView()
    {
        var lblTitle = new Label
        {
            Text = "🔑 Quản lý tài khoản",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // Tìm kiếm & Bộ lọc
        var txtSearch = new TextBox
        {
            Location = new Point(20, 60),
            Size = new Size(250, 25),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Nhập tên đăng nhập hoặc Tên chủ sở hữu..."
        };

        var lblVaiTro = new Label { Text = "Vai trò:", Location = new Point(280, 63), AutoSize = true, Font = new Font("Segoe UI", 9) };
        var cboVaiTro = new ComboBox
        {
            Location = new Point(330, 60),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboVaiTro.Items.AddRange(new[] { "--- Tất cả ---", "Admin", "Quản trị viên", "Quản lý", "Nhân viên" });
        cboVaiTro.SelectedIndex = 0;

        var lblTrangThai = new Label { Text = "Trạng thái:", Location = new Point(460, 63), AutoSize = true, Font = new Font("Segoe UI", 9) };
        var cboTrangThai = new ComboBox
        {
            Location = new Point(530, 60),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboTrangThai.Items.AddRange(new[] { "--- Tất cả ---", "Hoạt động", "Đã khóa" });
        cboTrangThai.SelectedIndex = 0;

        var btnReset = new Button
        {
            Text = "🔄 Reset",
            Location = new Point(660, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnReset.FlatAppearance.BorderSize = 0;

        var btnAdd = new Button
        {
            Text = "➕ Thêm mới",
            Location = new Point(740, 59),
            Size = new Size(100, 28),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAdd.FlatAppearance.BorderSize = 0;

        var btnEdit = new Button
        {
            Text = "✏️ Sửa",
            Location = new Point(850, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(241, 196, 15),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnEdit.FlatAppearance.BorderSize = 0;

        var btnDelete = new Button
        {
            Text = "🗑️ Xóa",
            Location = new Point(930, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var dgv = CreateStyledDataGridView("dgvTaiKhoan");
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

        dgv.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 100;
                switch (col.DataPropertyName)
                {
                    case "TenDangNhap": col.HeaderText = "Tên Đăng Nhập"; col.MinimumWidth = 150; break;
                    case "TenNhanVien": col.HeaderText = "Chủ Sở Hữu"; col.MinimumWidth = 180; break;
                    case "VaiTro": col.HeaderText = "Vai Trò"; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                    case "NgayTao": col.HeaderText = "Ngày Tạo"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                    case "LanDangNhapCuoi": col.HeaderText = "Đăng Nhập Cuối"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                    case "MaTaiKhoan": col.Visible = false; break;
                    case "MaNhanVien": col.Visible = false; break;
                }
            }
        };

        var taiKhoanService = Program.ServiceProvider.GetRequiredService<ITaiKhoanService>();

        // Debounce Logic
        _searchTimer?.Stop();
        _searchTimer?.Dispose();
        _searchTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            var kw = txtSearch.Text.Trim();
            var role = cboVaiTro.SelectedItem?.ToString();
            var status = cboTrangThai.SelectedItem?.ToString();
            dgv.DataSource = await taiKhoanService.SearchAsync(kw, role, status);
        };

        void TriggerSearch()
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        txtSearch.TextChanged += (s, e) => TriggerSearch();
        cboVaiTro.SelectedIndexChanged += (s, e) => TriggerSearch();
        cboTrangThai.SelectedIndexChanged += (s, e) => TriggerSearch();

        btnReset.Click += async (s, e) =>
        {
            txtSearch.Text = "";
            cboVaiTro.SelectedIndex = 0;
            cboTrangThai.SelectedIndex = 0;
            dgv.DataSource = await taiKhoanService.GetAllAsync();
        };

        btnAdd.Click += async (s, e) =>
        {
            var frm = Program.ServiceProvider.GetRequiredService<Forms.Auth.frmTaoTaiKhoan>();
            if (frm.ShowDialog() == DialogResult.OK) { }
            dgv.DataSource = await taiKhoanService.GetAllAsync();
        };

        btnEdit.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            var dto = (HRM.Common.DTOs.TaiKhoanDTO)dgv.SelectedRows[0].DataBoundItem;

            var frm = new Forms.Auth.frmSuaTaiKhoan(taiKhoanService, dto);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                dgv.DataSource = await taiKhoanService.GetAllAsync();
            }
        };

        btnDelete.Click += async (s, e) =>
        {
            if (dgv.SelectedRows.Count == 0) return;
            var dto = (HRM.Common.DTOs.TaiKhoanDTO)dgv.SelectedRows[0].DataBoundItem;

            if (_session != null && _session.MaNhanVien == dto.MaNhanVien)
            {
                MessageBox.Show("Bạn không thể tự xóa tài khoản đang đăng nhập của chính mình!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa tài khoản [{dto.TenDangNhap}]?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    await taiKhoanService.DeleteAsync(dto.MaTaiKhoan);
                    dgv.DataSource = await taiKhoanService.GetAllAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(lblVaiTro);
        pnlContent.Controls.Add(cboVaiTro);
        pnlContent.Controls.Add(lblTrangThai);
        pnlContent.Controls.Add(cboTrangThai);
        pnlContent.Controls.Add(btnReset);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

        try
        {
            dgv.DataSource = await taiKhoanService.GetAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}");
        }
    }





    private async Task LoadHieuSuatView()
    {
        var lblTitle = new Label
        {
            Text = "📈 Quản lý hiệu suất",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var txtSearch = new TextBox
        {
            Location = new Point(20, 60),
            Size = new Size(260, 25),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Tên nhân viên / phòng ban..."
        };

        var lblKy = new Label
        {
            Text = "Kỳ đánh giá:",
            Location = new Point(290, 63),
            AutoSize = true,
            Font = new Font("Segoe UI", 9)
        };

        var cboKyDanhGia = new ComboBox
        {
            Location = new Point(370, 60),
            Size = new Size(220, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        var btnReset = new Button
        {
            Text = "🔄 Reset",
            Location = new Point(710, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnReset.FlatAppearance.BorderSize = 0;

        var btnKyDanhGia = new Button
        {
            Text = "🗂️ Kỳ đánh giá",
            Location = new Point(600, 59),
            Size = new Size(105, 28),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnKyDanhGia.FlatAppearance.BorderSize = 0;

        var btnAdd = new Button
        {
            Text = "➕ Thêm mới",
            Location = new Point(790, 59),
            Size = new Size(100, 28),
            BackColor = Color.FromArgb(46, 204, 113),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAdd.FlatAppearance.BorderSize = 0;

        var btnEdit = new Button
        {
            Text = "✏️ Sửa",
            Location = new Point(900, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(241, 196, 15),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnEdit.FlatAppearance.BorderSize = 0;

        var btnDelete = new Button
        {
            Text = "🗑️ Xóa",
            Location = new Point(980, 59),
            Size = new Size(70, 28),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var dgv = CreateStyledDataGridView("dgvHieuSuat");
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

        List<KyDanhGiaDTO> kyDanhGiaItems = new();
        var isReloadingKy = false;
        var isLoadingGrid = false;

        async Task ReloadKyDanhGiaAsync(int selectedKy = 0)
        {
            isReloadingKy = true;
            kyDanhGiaItems = await _hieuSuatService.GetKyDanhGiaAsync();
            var kyDataSource = new List<LookupItem> { new() { Value = 0, Text = "--- Tất cả ---" } };
            kyDataSource.AddRange(kyDanhGiaItems.Select(k => new LookupItem
            {
                Value = k.MaKyDanhGia,
                Text = $"{k.TenKyDanhGia} ({k.NgayBatDau:dd/MM/yyyy} - {k.NgayKetThuc:dd/MM/yyyy})"
            }));

            cboKyDanhGia.DataSource = null;
            cboKyDanhGia.DataSource = kyDataSource;
            cboKyDanhGia.DisplayMember = nameof(LookupItem.Text);
            cboKyDanhGia.ValueMember = nameof(LookupItem.Value);

            if (kyDataSource.Any(x => x.Value == selectedKy))
                cboKyDanhGia.SelectedValue = selectedKy;
            else
                cboKyDanhGia.SelectedValue = 0;

            isReloadingKy = false;
        }

        await ReloadKyDanhGiaAsync();

        async Task LoadGridAsync()
        {
            if (isReloadingKy || isLoadingGrid)
                return;

            isLoadingGrid = true;
            try
            {
                var keyword = txtSearch.Text.Trim();
                var selectedKy = cboKyDanhGia.SelectedValue is int id ? id : 0;

                var data = selectedKy == 0
                    ? await _hieuSuatService.GetAllAsync()
                    : await _hieuSuatService.GetByKyDanhGiaAsync(selectedKy);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLower();
                    data = data.Where(x =>
                        (x.TenNhanVien ?? string.Empty).ToLower().Contains(kw)).ToList();
                }

                dgv.DataSource = null;
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu hiệu suất: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isLoadingGrid = false;
            }
        }

        dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 95;
                switch (col.DataPropertyName)
                {
                    case "MaHieuSuat":
                    case "MaNhanVien":
                    case "MaKyDanhGia":
                        col.Visible = false;
                        break;
                    case "TenNhanVien": col.HeaderText = "Nhân viên"; col.MinimumWidth = 150; break;
                    case "TenKyDanhGia": col.HeaderText = "Kỳ đánh giá"; col.MinimumWidth = 170; break;
                    case "DiemKPI": col.HeaderText = "Điểm KPI"; col.DefaultCellStyle.Format = "N2"; break;
                    case "TyLeHoanThanhDeadline": col.HeaderText = "% Deadline"; col.DefaultCellStyle.Format = "N2"; break;
                    case "SoGioLamViec": col.HeaderText = "Giờ làm việc"; col.DefaultCellStyle.Format = "N2"; break;
                    case "NgayDanhGia": col.HeaderText = "Ngày đánh giá"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "KetQuaCongViec": col.HeaderText = "Kết quả công việc"; col.MinimumWidth = 180; break;
                    case "TrangThaiHoanThanh": col.HeaderText = "Tiến độ"; col.MinimumWidth = 150; break;
                    case "HeSoLuongHieuSuat": col.HeaderText = "HS lương"; col.DefaultCellStyle.Format = "P0"; break;
                    case "LuongDuKien": col.HeaderText = "Lương dự kiến"; col.DefaultCellStyle.Format = "N0"; col.MinimumWidth = 140; break;
                }
            }
        };

        txtSearch.TextChanged += async (_, _) =>
        {
            if (isReloadingKy) return;
            await LoadGridAsync();
        };

        cboKyDanhGia.SelectedIndexChanged += async (_, _) =>
        {
            if (isReloadingKy) return;
            await LoadGridAsync();
        };

        btnReset.Click += async (_, _) =>
        {
            txtSearch.Text = string.Empty;
            cboKyDanhGia.SelectedValue = 0;
            await LoadGridAsync();
        };

        btnKyDanhGia.Click += async (_, _) =>
        {
            using var dlg = new Form
            {
                Text = "Quản lý kỳ đánh giá",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(760, 430)
            };

            var dgvKy = new DataGridView
            {
                Location = new Point(15, 15),
                Size = new Size(730, 340),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            var btnThemKy = new Button { Text = "➕ Thêm", Location = new Point(15, 370), Size = new Size(90, 32) };
            var btnSuaKy = new Button { Text = "✏️ Sửa", Location = new Point(110, 370), Size = new Size(90, 32) };
            var btnXoaKy = new Button { Text = "🗑️ Xóa", Location = new Point(205, 370), Size = new Size(90, 32) };
            var btnDong = new Button { Text = "Đóng", Location = new Point(655, 370), Size = new Size(90, 32), DialogResult = DialogResult.OK };

            async Task LoadKyGridAsync()
            {
                var periods = await _hieuSuatService.GetKyDanhGiaAsync();
                dgvKy.DataSource = periods;

                foreach (DataGridViewColumn col in dgvKy.Columns)
                {
                    switch (col.DataPropertyName)
                    {
                        case "MaKyDanhGia": col.HeaderText = "Mã kỳ"; col.FillWeight = 20; break;
                        case "TenKyDanhGia": col.HeaderText = "Tên kỳ"; col.FillWeight = 45; break;
                        case "NgayBatDau": col.HeaderText = "Từ ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                        case "NgayKetThuc": col.HeaderText = "Đến ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                    }
                }
            }

            btnThemKy.Click += async (_, _) =>
            {
                if (!TryShowKyDanhGiaEditor(null, out var dto))
                    return;

                try
                {
                    await _hieuSuatService.CreateKyDanhGiaAsync(dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSuaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentKy = dgvKy.SelectedRows[0].DataBoundItem as KyDanhGiaDTO;
                if (currentKy == null) return;

                if (!TryShowKyDanhGiaEditor(currentKy, out var dto))
                    return;

                try
                {
                    await _hieuSuatService.UpdateKyDanhGiaAsync(currentKy.MaKyDanhGia, dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnXoaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentKy = dgvKy.SelectedRows[0].DataBoundItem as KyDanhGiaDTO;
                if (currentKy == null) return;

                var confirm = MessageBox.Show(
                    $"Xóa kỳ đánh giá [{currentKy.TenKyDanhGia}]?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                try
                {
                    await _hieuSuatService.DeleteKyDanhGiaAsync(currentKy.MaKyDanhGia);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(dgvKy);
            dlg.Controls.Add(btnThemKy);
            dlg.Controls.Add(btnSuaKy);
            dlg.Controls.Add(btnXoaKy);
            dlg.Controls.Add(btnDong);

            await LoadKyGridAsync();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var selected = cboKyDanhGia.SelectedValue is int val ? val : 0;
                await ReloadKyDanhGiaAsync(selected);
                await LoadGridAsync();
            }
        };

        btnKyDanhGia.Click += async (_, _) =>
        {
            using var dlg = new Form
            {
                Text = "Quản lý kỳ đánh giá",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(760, 430)
            };

            var dgvKy = new DataGridView
            {
                Location = new Point(15, 15),
                Size = new Size(730, 340),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            var btnThemKy = new Button { Text = "➕ Thêm", Location = new Point(15, 370), Size = new Size(90, 32) };
            var btnSuaKy = new Button { Text = "✏️ Sửa", Location = new Point(110, 370), Size = new Size(90, 32) };
            var btnXoaKy = new Button { Text = "🗑️ Xóa", Location = new Point(205, 370), Size = new Size(90, 32) };
            var btnDong = new Button { Text = "Đóng", Location = new Point(655, 370), Size = new Size(90, 32), DialogResult = DialogResult.OK };

            async Task LoadKyGridAsync()
            {
                var periods = await _hieuSuatService.GetKyDanhGiaAsync();
                dgvKy.DataSource = periods;

                foreach (DataGridViewColumn col in dgvKy.Columns)
                {
                    switch (col.DataPropertyName)
                    {
                        case "MaKyDanhGia": col.HeaderText = "Mã kỳ"; col.FillWeight = 20; break;
                        case "TenKyDanhGia": col.HeaderText = "Tên kỳ"; col.FillWeight = 45; break;
                        case "NgayBatDau": col.HeaderText = "Từ ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                        case "NgayKetThuc": col.HeaderText = "Đến ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                    }
                }
            }

            btnThemKy.Click += async (_, _) =>
            {
                if (!TryShowKyDanhGiaEditor(null, out var dto))
                    return;

                try
                {
                    await _hieuSuatService.CreateKyDanhGiaAsync(dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSuaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentKy = dgvKy.SelectedRows[0].DataBoundItem as KyDanhGiaDTO;
                if (currentKy == null) return;

                if (!TryShowKyDanhGiaEditor(currentKy, out var dto))
                    return;

                try
                {
                    await _hieuSuatService.UpdateKyDanhGiaAsync(currentKy.MaKyDanhGia, dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnXoaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentKy = dgvKy.SelectedRows[0].DataBoundItem as KyDanhGiaDTO;
                if (currentKy == null) return;

                var confirm = MessageBox.Show(
                    $"Xóa kỳ đánh giá [{currentKy.TenKyDanhGia}]?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                try
                {
                    await _hieuSuatService.DeleteKyDanhGiaAsync(currentKy.MaKyDanhGia);
                    await LoadKyGridAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(dgvKy);
            dlg.Controls.Add(btnThemKy);
            dlg.Controls.Add(btnSuaKy);
            dlg.Controls.Add(btnXoaKy);
            dlg.Controls.Add(btnDong);

            await LoadKyGridAsync();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var selected = cboKyDanhGia.SelectedValue is int val ? val : 0;
                await ReloadKyDanhGiaAsync(selected);
                await LoadGridAsync();
            }
        };

        btnAdd.Click += async (_, _) =>
        {
            var nhanVien = await _nhanVienService.GetAllAsync();
            if (nhanVien.Count == 0 || kyDanhGiaItems.Count == 0)
            {
                MessageBox.Show("Cần có dữ liệu Nhân viên và Kỳ đánh giá trước khi thêm hiệu suất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryShowHieuSuatEditor(nhanVien, kyDanhGiaItems, null, out var dto))
                return;

            try
            {
                await _hieuSuatService.CreateAsync(dto);
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnEdit.Click += async (_, _) =>
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn bản ghi cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgv.SelectedRows[0].DataBoundItem as HieuSuatDTO;
            if (selected == null) return;

            var nhanVien = await _nhanVienService.GetAllAsync();
            if (!TryShowHieuSuatEditor(nhanVien, kyDanhGiaItems, selected, out var dto))
                return;

            try
            {
                await _hieuSuatService.UpdateAsync(selected.MaHieuSuat, dto);
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        btnDelete.Click += async (_, _) =>
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn bản ghi cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgv.SelectedRows[0].DataBoundItem as HieuSuatDTO;
            if (selected == null) return;

            var confirm = MessageBox.Show(
                $"Xóa bản ghi hiệu suất của [{selected.TenNhanVien}] ở kỳ [{selected.TenKyDanhGia}]?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                await _hieuSuatService.DeleteAsync(selected.MaHieuSuat);
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(lblKy);
        pnlContent.Controls.Add(cboKyDanhGia);
        pnlContent.Controls.Add(btnKyDanhGia);
        pnlContent.Controls.Add(btnReset);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

        await LoadGridAsync();
    }

    private static bool TryShowHieuSuatEditor(
        IReadOnlyList<NhanVienDTO> nhanVienData,
        IReadOnlyList<KyDanhGiaDTO> kyDanhGiaData,
        HieuSuatDTO? current,
        out HieuSuatDTO result)
    {
        result = new HieuSuatDTO();
        HieuSuatDTO? pendingResult = null;

        using var dlg = new Form
        {
            Text = current == null ? "Thêm hiệu suất" : "Sửa hiệu suất",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(560, 395)
        };

        var lblNhanVien = new Label { Text = "Nhân viên", AutoSize = true, Location = new Point(20, 20) };
        var cboNhanVien = new ComboBox
        {
            Location = new Point(20, 40),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblKy = new Label { Text = "Kỳ đánh giá", AutoSize = true, Location = new Point(290, 20) };
        var cboKy = new ComboBox
        {
            Location = new Point(290, 40),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblKpi = new Label { Text = "Điểm KPI", AutoSize = true, Location = new Point(20, 80) };
        var txtKpi = new TextBox { Location = new Point(20, 100), Size = new Size(120, 25) };

        var lblDeadline = new Label { Text = "% Deadline", AutoSize = true, Location = new Point(160, 80) };
        var txtDeadline = new TextBox { Location = new Point(160, 100), Size = new Size(120, 25) };

        var lblSoGio = new Label { Text = "Số giờ làm", AutoSize = true, Location = new Point(300, 80) };
        var txtSoGio = new TextBox { Location = new Point(300, 100), Size = new Size(120, 25) };

        var lblKetQua = new Label { Text = "Kết quả công việc", AutoSize = true, Location = new Point(20, 140) };
        var txtKetQua = new TextBox
        {
            Location = new Point(20, 160),
            Size = new Size(520, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        var btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(380, 350),
            Size = new Size(75, 30),
            DialogResult = DialogResult.None
        };
        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(465, 350),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

        var nhanVienItems = nhanVienData
            .Select(x => new LookupItem { Value = x.MaNhanVien, Text = $"{x.HoTen} ({x.MaNV})" })
            .ToList();
        cboNhanVien.DataSource = nhanVienItems;
        cboNhanVien.DisplayMember = nameof(LookupItem.Text);
        cboNhanVien.ValueMember = nameof(LookupItem.Value);

        var kyItems = kyDanhGiaData
            .Select(x => new LookupItem { Value = x.MaKyDanhGia, Text = x.TenKyDanhGia })
            .ToList();
        cboKy.DataSource = kyItems;
        cboKy.DisplayMember = nameof(LookupItem.Text);
        cboKy.ValueMember = nameof(LookupItem.Value);

        if (current != null)
        {
            cboNhanVien.SelectedValue = current.MaNhanVien;
            cboKy.SelectedValue = current.MaKyDanhGia;
            txtKpi.Text = current.DiemKPI?.ToString(CultureInfo.CurrentCulture);
            txtDeadline.Text = current.TyLeHoanThanhDeadline?.ToString(CultureInfo.CurrentCulture);
            txtSoGio.Text = current.SoGioLamViec?.ToString(CultureInfo.CurrentCulture);
            txtKetQua.Text = current.KetQuaCongViec;
        }

        btnSave.Click += (_, _) =>
        {
            if (cboNhanVien.SelectedValue is not int maNhanVien || maNhanVien <= 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên.");
                return;
            }

            if (cboKy.SelectedValue is not int maKy || maKy <= 0)
            {
                MessageBox.Show("Vui lòng chọn kỳ đánh giá.");
                return;
            }

            if (!TryParseNullableDecimal(txtKpi.Text, out var diemKpi)
                || !TryParseNullableDecimal(txtDeadline.Text, out var tyLe)
                || !TryParseNullableDecimal(txtSoGio.Text, out var soGio))
            {
                MessageBox.Show("Các trường số (KPI, % Deadline, Số giờ làm) không hợp lệ.");
                return;
            }

            pendingResult = new HieuSuatDTO
            {
                MaHieuSuat = current?.MaHieuSuat ?? 0,
                MaNhanVien = maNhanVien,
                MaKyDanhGia = maKy,
                DiemKPI = diemKpi,
                TyLeHoanThanhDeadline = tyLe,
                SoGioLamViec = soGio,
                NgayDanhGia = current?.NgayDanhGia == default ? DateTime.Today : current!.NgayDanhGia,
                KetQuaCongViec = string.IsNullOrWhiteSpace(txtKetQua.Text) ? null : txtKetQua.Text.Trim(),
            };

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.Add(lblNhanVien);
        dlg.Controls.Add(cboNhanVien);
        dlg.Controls.Add(lblKy);
        dlg.Controls.Add(cboKy);
        dlg.Controls.Add(lblKpi);
        dlg.Controls.Add(txtKpi);
        dlg.Controls.Add(lblDeadline);
        dlg.Controls.Add(txtDeadline);
        dlg.Controls.Add(lblSoGio);
        dlg.Controls.Add(txtSoGio);
        dlg.Controls.Add(lblKetQua);
        dlg.Controls.Add(txtKetQua);
        dlg.Controls.Add(btnSave);
        dlg.Controls.Add(btnCancel);

        var ok = dlg.ShowDialog() == DialogResult.OK;
        if (ok && pendingResult != null)
        {
            result = pendingResult;
            return true;
        }

        return false;
    }

    private static bool TryShowKyDanhGiaEditor(KyDanhGiaDTO? current, out KyDanhGiaDTO result)
    {
        result = new KyDanhGiaDTO();
        KyDanhGiaDTO? pending = null;

        using var dlg = new Form
        {
            Text = current == null ? "Thêm kỳ đánh giá" : "Sửa kỳ đánh giá",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(420, 220)
        };

        var lblTen = new Label { Text = "Tên kỳ đánh giá", AutoSize = true, Location = new Point(20, 20) };
        var txtTen = new TextBox { Location = new Point(20, 40), Size = new Size(380, 25) };

        var lblTu = new Label { Text = "Ngày bắt đầu", AutoSize = true, Location = new Point(20, 80) };
        var dtpTu = new DateTimePicker
        {
            Location = new Point(20, 100),
            Size = new Size(180, 25),
            Format = DateTimePickerFormat.Short
        };

        var lblDen = new Label { Text = "Ngày kết thúc", AutoSize = true, Location = new Point(220, 80) };
        var dtpDen = new DateTimePicker
        {
            Location = new Point(220, 100),
            Size = new Size(180, 25),
            Format = DateTimePickerFormat.Short
        };

        var btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(245, 160),
            Size = new Size(75, 30),
            DialogResult = DialogResult.None
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(325, 160),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

        if (current != null)
        {
            txtTen.Text = current.TenKyDanhGia;
            dtpTu.Value = current.NgayBatDau == default ? DateTime.Today : current.NgayBatDau;
            dtpDen.Value = current.NgayKetThuc == default ? DateTime.Today : current.NgayKetThuc;
        }
        else
        {
            dtpTu.Value = DateTime.Today;
            dtpDen.Value = DateTime.Today;
        }

        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Tên kỳ đánh giá không được để trống.");
                return;
            }

            if (dtpTu.Value.Date > dtpDen.Value.Date)
            {
                MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                return;
            }

            pending = new KyDanhGiaDTO
            {
                MaKyDanhGia = current?.MaKyDanhGia ?? 0,
                TenKyDanhGia = txtTen.Text.Trim(),
                NgayBatDau = dtpTu.Value.Date,
                NgayKetThuc = dtpDen.Value.Date
            };

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.Add(lblTen);
        dlg.Controls.Add(txtTen);
        dlg.Controls.Add(lblTu);
        dlg.Controls.Add(dtpTu);
        dlg.Controls.Add(lblDen);
        dlg.Controls.Add(dtpDen);
        dlg.Controls.Add(btnSave);
        dlg.Controls.Add(btnCancel);

        var ok = dlg.ShowDialog() == DialogResult.OK;
        if (ok && pending != null)
        {
            result = pending;
            return true;
        }

        return false;
    }

    private static bool TryParseNullableDecimal(string input, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input)) return true;

        var text = input.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private sealed class LookupItem
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

}

