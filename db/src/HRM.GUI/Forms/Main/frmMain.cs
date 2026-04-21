using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main;

public partial class frmMain : Form
{
    private readonly INhanVienService _nhanVienService;
    private readonly IPhongBanService _phongBanService;
    private readonly IPhongVanService _phongVanService;
    private readonly ITinTuyenDungService _tinTuyenDungService;
    private UserSessionDTO? _session;
    private bool isTuyenDungExpanded = false;

    public frmMain(INhanVienService nhanVienService, IPhongBanService phongBanService, IPhongVanService phongVanService, ITinTuyenDungService tinTuyenDungService)
    {
        _nhanVienService = nhanVienService;
        _phongBanService = phongBanService;
        _phongVanService = phongVanService;
        _tinTuyenDungService = tinTuyenDungService;
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
        // Xóa các nút menu cũ nếu có
        pnlSidebar.Controls.Clear();

        int yPos = 70; // Vị trí Y bắt đầu

        // === HÀM TẠO NÚT NHANH (Thay thế cho vòng lặp foreach) ===
        void TaoNutMenu(string text, bool isSubMenu = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(0, yPos),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                // Nếu là menu con thì chữ nhỏ hơn 1 chút
                Font = new Font("Segoe UI", isSubMenu ? 9 : 10),
                // Nếu là menu con thì màu chữ và nền nhạt/đậm hơn để phân biệt
                ForeColor = isSubMenu ? Color.FromArgb(180, 190, 200) : Color.FromArgb(200, 210, 220),
                BackColor = isSubMenu ? Color.FromArgb(40, 55, 90) : Color.FromArgb(30, 45, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                // Nếu là menu con thì thụt lề vào 40px thay vì 20px
                Padding = new Padding(isSubMenu ? 40 : 20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 70, 110);
            btn.Click += MenuButton_Click;

            pnlSidebar.Controls.Add(btn);
            yPos += 45; // Tự động cộng tọa độ Y cho nút tiếp theo
        }

        // === BẮT ĐẦU VẼ MENU ===
        TaoNutMenu("📊 Tổng quan");

        // Kiểm tra quyền
        string vaiTro = _session?.VaiTro ?? "";
        if (vaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            vaiTro.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase))
        {
            TaoNutMenu("👥 Nhân viên");
            TaoNutMenu("🏗️ Phòng ban");

            // --- ĐOẠN MENU SỔ XUỐNG ---
            // Đổi icon mũi tên dựa theo trạng thái mở hay đóng
            string icon = isTuyenDungExpanded ? "▼" : "▶";
            TaoNutMenu($"🤝 Tuyển dụng {icon}");

            // Chỉ vẽ 3 nút con này ra nếu cờ đang là TRUE
            if (isTuyenDungExpanded)
            {
                TaoNutMenu("📝 Tin tuyển dụng", true); // true = là menu con
                TaoNutMenu("🧑‍🎓 Ứng viên", true);
                TaoNutMenu("🎤 Phỏng vấn", true);
            }
            // ---------------------------

            TaoNutMenu("📈 Báo cáo");
            TaoNutMenu("🔑 Tài khoản");
        }

        // Các chức năng chung ai cũng thấy
        TaoNutMenu("⏰ Chấm công");
        TaoNutMenu("📋 Nghỉ phép");
        TaoNutMenu("💰 Lương");
    }

    private void btnLogout_Click(object? sender, EventArgs e)
    {
        this.Close();
    }

    private async void MenuButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string choice = btn.Text;
        if (btn.Text.Contains("Tuyển dụng") && !btn.Text.Contains("Tin")) // Tránh nhầm với "Tin tuyển dụng"
        {
            isTuyenDungExpanded = !isTuyenDungExpanded; // Đảo trạng thái cờ
            SetupMenu(); // Gọi lại hàm vẽ menu để cập nhật các nút con
            return; // Cực kỳ quan trọng: Lệnh này giúp dừng hàm ngay lập tức, không chạy xuống dưới
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
        else if (btn.Text.Contains("Phỏng vấn"))
        {
            await LoadPhongVanView();
        }
        else if (btn.Text.Contains("Tin tuyển dụng"))
        {
            await LoadTinTuyenDungView();
        }
        else if (btn.Text.Contains("Ứng viên"))
        {
            // await LoadUngVienView(); // Để dành cho form Ứng viên
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

            var frm = new Forms.Main.frmSuaNhanVien(_nhanVienService, dto);
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
                    case "MaNV": col.HeaderText = "Mã Nhân Viên"; break;
                    case "HoTen": col.HeaderText = "Họ Tên"; col.MinimumWidth = 150; break;
                    case "NgaySinh": col.HeaderText = "Ngày Sinh"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "GioiTinh": col.HeaderText = "Giới Tính"; break;
                    case "CCCD": col.HeaderText = "CCCD"; break;
                    case "DiaChi": col.HeaderText = "Địa Chỉ"; col.MinimumWidth = 200; break;
                    case "SoDienThoai": col.HeaderText = "SĐT"; break;
                    case "Email": col.HeaderText = "Email"; col.MinimumWidth = 150; break;
                    case "TinhTrangHonNhan": col.HeaderText = "Hôn Nhân"; break;
                    case "NgayVaoLam": col.HeaderText = "Ngày Vào Làm"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "MucLuongCoBan": col.HeaderText = "Lương CB"; col.DefaultCellStyle.Format = "N0"; col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; break;
                    case "TrangThaiZ": col.HeaderText = "Trạng Thái"; break;
                    // Có thể ẩn các cột Id ngoại lai
                    case "MaPhongBan": col.Visible = false; break;
                    case "MaChucVu": col.Visible = false; break;
                    case "AnhDaiDien": col.Visible = false; break;
                    case "Id": col.Visible = false; break;
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

        var txtSearch = new TextBox
        {
            Location = new Point(20, 60),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Nhập tên đăng nhập hoặc Tên chủ sở hữu..."
        };

        var btnSearch = new Button
        {
            Text = "🔍 Tìm",
            Location = new Point(330, 59),
            Size = new Size(80, 28),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;

        var btnAdd = new Button
        {
            Text = "➕ Thêm mới",
            Location = new Point(420, 59),
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
            Location = new Point(530, 59),
            Size = new Size(80, 28),
            BackColor = Color.FromArgb(241, 196, 15),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnEdit.FlatAppearance.BorderSize = 0;

        var btnDelete = new Button
        {
            Text = "🗑️ Xóa",
            Location = new Point(620, 59),
            Size = new Size(80, 28),
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

        var taiKhoanService = Program.ServiceProvider.GetRequiredService<ITaiKhoanService>(); // Gọi từ DI

        btnSearch.Click += async (s, e) =>
        {
            var keyword = txtSearch.Text.Trim();
            dgv.DataSource = string.IsNullOrEmpty(keyword)
                ? await taiKhoanService.GetAllAsync()
                : await taiKhoanService.SearchAsync(keyword);
        };
        txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSearch.PerformClick(); };

        btnAdd.Click += async (s, e) =>
        {
            var frm = Program.ServiceProvider.GetRequiredService<Forms.Auth.frmTaoTaiKhoan>();
            if (frm.ShowDialog() == DialogResult.OK) // Mặc dù frmTaoTaiKhoan k set result nhưng k sao, ta refresh hết
            {
            }
            // Refresh auto
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
        pnlContent.Controls.Add(btnSearch);
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
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên")
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
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên")
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
            Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên")
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var dgv = CreateStyledDataGridView("dgvPhongVan");
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

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
                    case "VongPhongVan": col.HeaderText = "Vòng PV"; break;
                    case "NgayPhongVan": col.HeaderText = "Ngày Phỏng Vấn"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                    case "DiaDiem": col.HeaderText = "Địa Điểm"; col.MinimumWidth = 200; break;
                    case "NguoiPhongVan": col.HeaderText = "Người Phỏng Vấn"; col.MinimumWidth = 150; break;
                    case "NhanXet": col.HeaderText = "Nhận xét"; col.MinimumWidth = 150; break;
                    case "KetQua": col.HeaderText = "Kết Quả"; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                    case "CauHoiPhongVan": col.Visible = false; break;

                    case "UngVien": col.Visible = false; break;
                    case "NguoiPhongVanNav": col.Visible = false; break;
                }
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

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(btnSearch);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

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

    private async Task LoadTinTuyenDungView()
    {
        // 1. SỬA TIÊU ĐỀ & ICON
        var lblTitle = new Label
        {
            Text = "📝 Danh sách Tin Tuyển Dụng", // Đổi icon và text
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 2. SỬA GỢI Ý Ô TÌM KIẾM
        var txtSearch = new TextBox
        {
            Location = new Point(20, 60),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Nhập mã tin, vị trí tuyển dụng..." // Đổi gợi ý
        };

        // Các nút bấm giữ nguyên hoàn toàn (rất tiện lợi)
        var btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(330, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        btnSearch.FlatAppearance.BorderSize = 0;

        var btnAdd = new Button { Text = "➕ Thêm mới", Location = new Point(440, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
        btnAdd.FlatAppearance.BorderSize = 0;

        var btnEdit = new Button { Text = "✏️ Sửa", Location = new Point(550, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
        btnEdit.FlatAppearance.BorderSize = 0;

        var btnDelete = new Button { Text = "🗑️ Xóa", Location = new Point(640, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
        btnDelete.FlatAppearance.BorderSize = 0;

        // 3. SỬA CẤU HÌNH BẢNG & CỘT DỮ LIỆU DTO
        var dgv = CreateStyledDataGridView("dgvTinTuyenDung"); // Đổi tên control
        dgv.Location = new Point(20, 100);
        dgv.Size = new Size(pnlContent.Width - 40, pnlContent.Height - 120);

        dgv.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.MinimumWidth = 100;
                switch (col.DataPropertyName)
                {

                    case "MaHienThi": col.HeaderText = "Mã Tin"; col.MinimumWidth = 80; break;
                    case "ViTriTuyenDung": col.HeaderText = "Vị trí tuyển dụng"; col.MinimumWidth = 200; break;
                    case "SoLuongCanTuyen": col.HeaderText = "Số lượng"; col.MinimumWidth = 80; break;
                    case "MoTaCongViec": col.HeaderText = "Mô tả công Việc"; col.MinimumWidth = 100; break;
                    case "TenPhongBan": col.HeaderText = "Phòng ban"; col.MinimumWidth = 150; break;
                    case "YeuCauUngVien": col.HeaderText = "Yêu cầu ứng viên"; col.MinimumWidth = 100; break;
                    case "ThoiHanNhanHoSo": col.HeaderText = "Hạn nộp hồ sơ"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                    case "MucLuong": col.HeaderText = "Mức lương (VNĐ)"; col.MinimumWidth = 150; break;
                    case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                    case "MaTinTuyenDung": col.Visible = false; break;
                    default: col.Visible = false; break;
                }
            }
        };

        // 4. SỬA FORM GỌI LÊN KHI BẤM NÚT "THÊM"
        btnAdd.Click += async (s, e) =>
        {
            try
            {
                // Đổi frmThemPhongVan thành frmThemTinTuyenDung
                var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.TinTuyenDung.frmThemTinTuyenDung>();
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    // Cập nhật lại list sau khi thêm
                    dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                }
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Lỗi: Chưa đăng ký frmThemTinTuyenDung trong file Program.cs!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // BẮT SỰ KIỆN CHO NÚT SỬA
        btnEdit.Click += async (s, e) =>
        {
            if (dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0)
            {
                try
                {
                    int idDuocChon = Convert.ToInt32(dgv.CurrentRow.Cells["MaTinTuyenDung"].Value);
                    var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.TinTuyenDung.frmSuaTinTuyenDung>();
                    frm.MaTinCachSua = idDuocChon;

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi khi mở Form Sửa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng click chọn một tin tuyển dụng trong danh sách trước khi bấm Sửa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        };

        btnDelete.Click += async (s, e) =>
        {
            // 1. Kiểm tra xem có dòng nào đang được chọn không
            if (dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0)
            {
                // Lấy Tên vị trí (để hiện lên thông báo cho thân thiện) và ID (để xóa)
                string tenViTri = dgv.CurrentRow.Cells["ViTriTuyenDung"].Value?.ToString() ?? "tin này";
                int idCachXoa = Convert.ToInt32(dgv.CurrentRow.Cells["MaTinTuyenDung"].Value);

                // 2. Hiện hộp thoại cảnh báo (Confirmation)
                var xacNhan = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa vĩnh viễn {tenViTri} không?\nHành động này không thể hoàn tác!",
                    "Cảnh báo xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2); // Mặc định focus vào nút No cho an toàn

                // 3. Nếu người dùng bấm Yes thì mới tiến hành xóa
                if (xacNhan == DialogResult.Yes)
                {
                    try
                    {
                        // GỌI SERVICE ĐỂ XÓA (Bạn nhớ đổi tên hàm DeleteAsync cho khớp với tên trong file IService của bạn nhé)
                        bool isSuccess = await _tinTuyenDungService.DeleteTinTuyenDungAsync(idCachXoa);

                        if (isSuccess)
                        {
                            MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Xóa xong thì tải lại lưới dữ liệu
                            dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                        }
                        else
                        {
                            MessageBox.Show("Xóa thất bại! Dữ liệu này có thể đang bị ràng buộc ở nơi khác.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi hệ thống khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng click chọn một tin tuyển dụng cần xóa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        };

        // Add UI vào panel
        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(txtSearch);
        pnlContent.Controls.Add(btnSearch);
        pnlContent.Controls.Add(btnAdd);
        pnlContent.Controls.Add(btnEdit);
        pnlContent.Controls.Add(btnDelete);
        pnlContent.Controls.Add(dgv);

        // 5. SỬA HÀM LẤY DỮ LIỆU TỪ SERVICE
        try
        {
            // Đổi _phongVanService thành _tinTuyenDungService
            var data = await _tinTuyenDungService.GetAllAsync();
            dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
