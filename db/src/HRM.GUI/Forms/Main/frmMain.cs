using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace HRM.GUI.Forms.Main;

public partial class frmMain : Form
{
    /// <summary>Đặt khi người dùng bấm Đăng xuất — form đăng nhập sẽ hiện lại thay vì thoát app.</summary>
    public bool ClosedForRelogin { get; private set; }

    private readonly INhanVienService _nhanVienService;
    private readonly IPhongBanService _phongBanService;
    private readonly IChamCongService _chamCongService;
    private readonly IHieuSuatService _hieuSuatService;
    private UserSessionDTO? _session;
    private System.Windows.Forms.Timer? _searchTimer;

    public frmMain(
        INhanVienService nhanVienService,
        IPhongBanService phongBanService,
        IChamCongService chamCongService,
        IHieuSuatService hieuSuatService)
    {
        _nhanVienService = nhanVienService;
        _phongBanService = phongBanService;
        _chamCongService = chamCongService;
        _hieuSuatService = hieuSuatService;
        InitializeComponent();
        // Không gọi SetupMenu() ở đây nữa vì chưa có thông tin _session
    }

    public void SetSession(UserSessionDTO session)
    {
        _session = session;
        lblWelcome.Text = $"Xin chào, {session.HoTen} ({session.VaiTro})";
        
        // Bước 1: Gọi hàm tạo Menu SAU KHI đã có thông tin phiên đăng nhập
        SetupMenu();
    }

    private void SetupMenu()
    {
        // Xóa các nút menu cũ nếu có (tránh bị trùng lặp)
        pnlSidebar.Controls.Clear();

        // Bước 2: Tạo danh sách các chức năng cơ bản ai cũng thấy
        var menuItems = new List<string> { 
            "📊 Tổng quan"
        };

        // Bước 3: Kiểm tra quyền, nếu là Admin/Quản lý thì mới thêm chức năng Quản lý
        string vaiTro = _session?.VaiTro ?? "";
        if (vaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
            vaiTro.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase))
        {
            menuItems.Add("👥 Nhân viên");
            menuItems.Add("🏗️ Phòng ban");
            menuItems.Add("📈 Hiệu suất");
            menuItems.Add("🔑 Tài khoản");
        }

        // Thêm các chức năng của cá nhân
        menuItems.Add("⏰ Chấm công");
        menuItems.Add("📋 Nghỉ phép");
        menuItems.Add("💰 Lương");

        int yPos = 70;
        foreach (var item in menuItems)
        {
            var btn = new Button
            {
                Text = item,
                Dock = DockStyle.None,
                Location = new Point(0, yPos),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 210, 220),
                BackColor = Color.FromArgb(30, 45, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 70, 110);
            btn.Click += MenuButton_Click;
            pnlSidebar.Controls.Add(btn);
            yPos += 45;
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
        else if (btn.Text.Contains("Hiệu suất"))
        {
            await LoadHieuSuatView();
        }
        else if (btn.Text.Contains("Chấm công"))
        {
            await LoadChamCongView();
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
            ColumnHeadersHeight = 42,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = new DataGridViewRow { Height = 38 },
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(230, 233, 238),
            EnableHeadersVisualStyles = false
        };

        var headerBg = Color.FromArgb(236, 240, 244);
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = headerBg,
            ForeColor = Color.FromArgb(55, 65, 80),
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 8, 0),
            SelectionBackColor = headerBg,
            SelectionForeColor = Color.FromArgb(55, 65, 80)
        };

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(50, 55, 65),
            SelectionBackColor = Color.FromArgb(232, 242, 252),
            SelectionForeColor = Color.FromArgb(30, 35, 45),
            Font = new Font("Segoe UI", 10f),
            Padding = new Padding(12, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 249, 252)
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
                    case "MaNV": col.HeaderText = "Mã NV"; col.DisplayIndex = 0; break;
                    case "HoTen": col.HeaderText = "Họ Tên"; col.MinimumWidth = 150; col.DisplayIndex = 1; break;
                    case "TenPhongBan": col.HeaderText = "Phòng Ban"; col.MinimumWidth = 120; col.DisplayIndex = 2; break;
                    case "TenChucVu": col.HeaderText = "Chức Vụ"; col.MinimumWidth = 120; col.DisplayIndex = 3; break;
                    case "MucLuong": col.HeaderText = "Mức Lương"; col.DefaultCellStyle.Format = "N0"; col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; col.DisplayIndex = 4; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; col.DisplayIndex = 5; break;
                    case "SoDienThoai": col.HeaderText = "SĐT"; break;
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

        var dgv = CreateStyledDataGridView("dgvPhongBan");
        
        dgv.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 100;
                switch (col.DataPropertyName)
                {
                    case "MaPhongBan": col.HeaderText = "Mã Phòng Ban"; break;
                    case "TenPhongBan": col.HeaderText = "Tên Phòng Ban"; col.MinimumWidth = 200; col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; break;
                    case "MoTa": col.HeaderText = "Mô Tả"; col.MinimumWidth = 250; col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; break;
                    case "TruongPhongId": col.HeaderText = "Mã Trưởng Phòng"; break;
                    case "Id": col.Visible = false; break;
                }
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(dgv);

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

        dgv.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            var name = dgv.Columns[e.ColumnIndex].DataPropertyName;
            if (name is "GioVao" or "GioRa" && e.Value is TimeSpan t)
            {
                e.Value = t.ToString(@"hh\:mm");
                e.FormattingApplied = true;
            }
        };

        dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 80;
                switch (col.DataPropertyName)
                {
                    case "MaChamCong":
                        col.Visible = false;
                        break;
                    case "MaNhanVien":
                        col.HeaderText = "Mã NV";
                        col.Visible = isAdmin;
                        break;
                    case "TenNhanVien":
                        col.HeaderText = "Nhân viên";
                        col.MinimumWidth = 140;
                        break;
                    case "NgayChamCong":
                        col.HeaderText = "Ngày";
                        col.DefaultCellStyle.Format = "dd/MM/yyyy";
                        break;
                    case "GioVao":
                        col.HeaderText = "Giờ vào";
                        break;
                    case "GioRa":
                        col.HeaderText = "Giờ ra";
                        break;
                    case "TongGioLam":
                        col.HeaderText = "Tổng giờ";
                        col.DefaultCellStyle.Format = "N2";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        break;
                    case "HinhThuc":
                        col.HeaderText = "Hình thức";
                        break;
                    case "TrangThai":
                        col.HeaderText = "Trạng thái";
                        break;
                    case "GhiChu":
                        col.HeaderText = "Ghi chú";
                        col.MinimumWidth = 100;
                        col.Visible = isAdmin;
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 120;
                        break;
                }
            }
        };

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

        btnReset.Click += (s, e) =>
        {
            txtSearch.Text = "";
            cboVaiTro.SelectedIndex = 0;
            cboTrangThai.SelectedIndex = 0;
        };

        btnAdd.Click += async (s, e) =>
        {
            var frm = Program.ServiceProvider.GetRequiredService<Forms.Auth.frmTaoTaiKhoan>();
            if (frm.ShowDialog() == DialogResult.OK) {}
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
                catch(Exception ex)
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
            Location = new Point(600, 59),
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
            Location = new Point(680, 59),
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
            Location = new Point(790, 59),
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
            Location = new Point(870, 59),
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

        var kyDanhGiaItems = await _hieuSuatService.GetKyDanhGiaAsync();
        var kyDataSource = new List<LookupItem> { new() { Value = 0, Text = "--- Tất cả ---" } };
        kyDataSource.AddRange(kyDanhGiaItems.Select(k => new LookupItem
        {
            Value = k.MaKyDanhGia,
            Text = $"{k.TenKyDanhGia} ({k.NgayBatDau:dd/MM/yyyy} - {k.NgayKetThuc:dd/MM/yyyy})"
        }));
        cboKyDanhGia.DataSource = kyDataSource;
        cboKyDanhGia.DisplayMember = nameof(LookupItem.Text);
        cboKyDanhGia.ValueMember = nameof(LookupItem.Value);

        async Task LoadGridAsync()
        {
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
                        (x.TenNhanVien ?? string.Empty).ToLower().Contains(kw) ||
                        (x.TenPhongBan ?? string.Empty).ToLower().Contains(kw) ||
                        (x.TenChucVu ?? string.Empty).ToLower().Contains(kw)).ToList();
                }

                dgv.DataSource = null;
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu hiệu suất: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    case "NguoiDanhGia":
                        col.Visible = false;
                        break;
                    case "TenNhanVien": col.HeaderText = "Nhân viên"; col.MinimumWidth = 150; break;
                    case "TenKyDanhGia": col.HeaderText = "Kỳ đánh giá"; col.MinimumWidth = 170; break;
                    case "DiemKPI": col.HeaderText = "Điểm KPI"; col.DefaultCellStyle.Format = "N2"; break;
                    case "TyLeHoanThanhDeadline": col.HeaderText = "% Deadline"; col.DefaultCellStyle.Format = "N2"; break;
                    case "SoGioLamViec": col.HeaderText = "Giờ làm việc"; col.DefaultCellStyle.Format = "N2"; break;
                    case "XepHang": col.HeaderText = "Xếp hạng"; break;
                    case "NgayDanhGia": col.HeaderText = "Ngày đánh giá"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "TenNguoiDanhGia": col.HeaderText = "Người đánh giá"; col.MinimumWidth = 140; break;
                    case "TenPhongBan": col.HeaderText = "Phòng ban"; col.MinimumWidth = 120; break;
                    case "TenChucVu": col.HeaderText = "Chức vụ"; col.MinimumWidth = 120; break;
                    case "KetQuaCongViec": col.HeaderText = "Kết quả công việc"; col.MinimumWidth = 180; break;
                    case "GhiChu": col.HeaderText = "Ghi chú"; col.MinimumWidth = 150; break;
                }
            }
        };

        txtSearch.TextChanged += async (_, _) => await LoadGridAsync();
        cboKyDanhGia.SelectedIndexChanged += async (_, _) => await LoadGridAsync();

        btnReset.Click += async (_, _) =>
        {
            txtSearch.Text = string.Empty;
            cboKyDanhGia.SelectedValue = 0;
            await LoadGridAsync();
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
            ClientSize = new Size(560, 470)
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

        var lblXepHang = new Label { Text = "Xếp hạng", AutoSize = true, Location = new Point(440, 80) };
        var txtXepHang = new TextBox { Location = new Point(440, 100), Size = new Size(100, 25) };

        var lblNgay = new Label { Text = "Ngày đánh giá", AutoSize = true, Location = new Point(20, 140) };
        var dtpNgay = new DateTimePicker
        {
            Location = new Point(20, 160),
            Size = new Size(180, 25),
            Format = DateTimePickerFormat.Short,
            Value = current?.NgayDanhGia == default ? DateTime.Today : current!.NgayDanhGia
        };

        var lblNguoiDanhGia = new Label { Text = "Người đánh giá", AutoSize = true, Location = new Point(220, 140) };
        var cboNguoiDanhGia = new ComboBox
        {
            Location = new Point(220, 160),
            Size = new Size(320, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblKetQua = new Label { Text = "Kết quả công việc", AutoSize = true, Location = new Point(20, 200) };
        var txtKetQua = new TextBox
        {
            Location = new Point(20, 220),
            Size = new Size(520, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        var lblGhiChu = new Label { Text = "Ghi chú", AutoSize = true, Location = new Point(20, 320) };
        var txtGhiChu = new TextBox
        {
            Location = new Point(20, 340),
            Size = new Size(520, 70),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        var btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(380, 425),
            Size = new Size(75, 30),
            DialogResult = DialogResult.None
        };
        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(465, 425),
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

        var nguoiDanhGiaItems = new List<LookupItem> { new() { Value = 0, Text = "-- Không chọn --" } };
        nguoiDanhGiaItems.AddRange(nhanVienItems);
        cboNguoiDanhGia.DataSource = nguoiDanhGiaItems;
        cboNguoiDanhGia.DisplayMember = nameof(LookupItem.Text);
        cboNguoiDanhGia.ValueMember = nameof(LookupItem.Value);

        if (current != null)
        {
            cboNhanVien.SelectedValue = current.MaNhanVien;
            cboKy.SelectedValue = current.MaKyDanhGia;
            cboNguoiDanhGia.SelectedValue = current.NguoiDanhGia ?? 0;
            txtKpi.Text = current.DiemKPI?.ToString(CultureInfo.CurrentCulture);
            txtDeadline.Text = current.TyLeHoanThanhDeadline?.ToString(CultureInfo.CurrentCulture);
            txtSoGio.Text = current.SoGioLamViec?.ToString(CultureInfo.CurrentCulture);
            txtXepHang.Text = current.XepHang;
            txtKetQua.Text = current.KetQuaCongViec;
            txtGhiChu.Text = current.GhiChu;
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
                XepHang = string.IsNullOrWhiteSpace(txtXepHang.Text) ? null : txtXepHang.Text.Trim(),
                NguoiDanhGia = cboNguoiDanhGia.SelectedValue is int val && val > 0 ? val : null,
                NgayDanhGia = dtpNgay.Value.Date,
                KetQuaCongViec = string.IsNullOrWhiteSpace(txtKetQua.Text) ? null : txtKetQua.Text.Trim(),
                GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim()
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
        dlg.Controls.Add(lblXepHang);
        dlg.Controls.Add(txtXepHang);
        dlg.Controls.Add(lblNgay);
        dlg.Controls.Add(dtpNgay);
        dlg.Controls.Add(lblNguoiDanhGia);
        dlg.Controls.Add(cboNguoiDanhGia);
        dlg.Controls.Add(lblKetQua);
        dlg.Controls.Add(txtKetQua);
        dlg.Controls.Add(lblGhiChu);
        dlg.Controls.Add(txtGhiChu);
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
