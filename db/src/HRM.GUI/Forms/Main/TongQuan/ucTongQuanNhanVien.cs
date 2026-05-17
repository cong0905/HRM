using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.TongQuan
{
    public partial class ucTongQuanNhanVien : UserControl
    {
        private readonly UserSessionDTO? _session;
        private FlowLayoutPanel _mainContainer = null!;

        // Bảng màu chuẩn
        private readonly Color _bgLight = Color.FromArgb(245, 247, 250);
        private readonly Color _textPrimary = Color.FromArgb(30, 40, 50);
        private readonly Color _textSecondary = Color.FromArgb(120, 130, 140);
        private readonly Color _primaryBlue = Color.FromArgb(41, 128, 185);
        private readonly Color _successGreen = Color.FromArgb(46, 204, 113);
        private readonly Color _warningOrange = Color.FromArgb(243, 156, 18);
        private readonly Color _dangerRed = Color.FromArgb(231, 76, 60);

        public ucTongQuanNhanVien() : this(null) { }

        public ucTongQuanNhanVien(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = _bgLight;
            _session = session;

            if (UIHelper.IsDesignTime()) return;

            Load += async (s, e) => await BuildDashboardAsync();
        }

        private async Task BuildDashboardAsync()
        {
            _mainContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20)
            };
            Controls.Add(_mainContainer);

            _mainContainer.Resize += (s, e) =>
            {
                foreach (Control c in _mainContainer.Controls)
                {
                    c.Width = _mainContainer.ClientSize.Width - 40;
                }
            };

            // 1. Thêm Header
            var pnlHeader = new Panel { Height = 70, Margin = new Padding(0, 0, 0, 15) };
            var lblHello = new Label
            {
                Text = $"Xin chào, {(_session?.HoTen ?? "Nhân viên")} 👋",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = _textPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSub = new Label
            {
                Text = "Chúc bạn một ngày làm việc hiệu quả!",
                Font = new Font("Segoe UI", 10),
                ForeColor = _textSecondary,
                AutoSize = true,
                Location = new Point(2, 35)
            };
            pnlHeader.Controls.Add(lblHello);
            pnlHeader.Controls.Add(lblSub);
            _mainContainer.Controls.Add(pnlHeader);

            // Lấy thông tin cá nhân và dữ liệu liên quan
            NhanVienDTO? nvDto = null;
            ChamCongDTO? todayCC = null;
            SoNgayPhepDTO? soPhep = null;
            BangLuongDTO? bangLuong = null;
            HieuSuatDTO? kpi = null;
            List<DonNghiPhepDTO> donPheps = new List<DonNghiPhepDTO>();
            List<ChamCongDTO> chamCongsThang = new List<ChamCongDTO>();

            if (_session != null)
            {
                try
                {
                    var nvService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.INhanVienService>();
                    var ccService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.IChamCongService>();
                    var phepService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.IDonNghiPhepService>();
                    var luongService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.IBangLuongService>();
                    var hsService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.IHieuSuatService>();

                    int maNV = _session.MaNhanVien;
                    var now = DateTime.Now;

                    nvDto = await nvService.GetByIdAsync(maNV);
                    todayCC = await ccService.GetTodayAsync(maNV);
                    soPhep = await phepService.GetSoNgayPhepAsync(maNV, now.Year);
                    
                    var dsLuong = await luongService.GetBangLuongAsync(now.Month, now.Year, false, maNV);
                    bangLuong = dsLuong.FirstOrDefault();

                    var dsKpi = await hsService.GetByNhanVienAsync(maNV);
                    kpi = dsKpi.OrderByDescending(x => x.MaHieuSuat).FirstOrDefault();

                    var allPheps = await phepService.GetByNhanVienAsync(maNV);
                    donPheps = allPheps.OrderByDescending(x => x.NgayBatDau).Take(4).ToList();

                    var startOfMonth = new DateTime(now.Year, now.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                    chamCongsThang = await ccService.GetHistoryAsync(maNV, startOfMonth, endOfMonth);
                }
                catch { }
            }

            // 2. Thêm HÀNG TRÊN (Profile + 4 Cards)
            _mainContainer.Controls.Add(CreateTopRow(nvDto, todayCC, soPhep, bangLuong, kpi));

            // 3. Thêm HÀNG GIỮA (Lịch, Thông báo, Đơn phép)
            _mainContainer.Controls.Add(CreateMiddleRow(donPheps));

            // 4. Thêm HÀNG DƯỚI (Chấm công tháng + Quick Actions)
            _mainContainer.Controls.Add(CreateBottomRow(chamCongsThang));
        }

        /// <summary>
        /// Tạo hàng trên cùng: Profile (30%) + 4 Cards (70%)
        /// </summary>
        private TableLayoutPanel CreateTopRow(NhanVienDTO? nv, ChamCongDTO? todayCC, SoNgayPhepDTO? soPhep, BangLuongDTO? bangLuong, HieuSuatDTO? kpi)
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Height = 220,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));

            // Cột trái: Profile Card
            var pnlProfile = CreateRoundedPanel();
            pnlProfile.Dock = DockStyle.Fill;
            pnlProfile.Margin = new Padding(0, 0, 10, 0);

            // Avatar
            var avatar = new Panel { Size = new Size(100, 100), Location = new Point(20, 30) };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(220, 230, 245)), 0, 0, 99, 99);
                var initials = (_session?.HoTen ?? "N").Length > 0 ? (_session?.HoTen ?? "N")[0].ToString() : "N";
                using var font = new Font("Segoe UI", 36, FontStyle.Bold);
                var sz = e.Graphics.MeasureString(initials, font);
                e.Graphics.DrawString(initials, font, new SolidBrush(_primaryBlue), (100 - sz.Width) / 2, (100 - sz.Height) / 2);
            };
            pnlProfile.Controls.Add(avatar);

            // Tên và Role
            pnlProfile.Controls.Add(new Label { Text = _session?.HoTen ?? "Nguyễn Văn A", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(140, 30), AutoSize = true });
            
            var badgeRole = new Label { Text = _session?.VaiTro ?? "Nhân viên", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = _primaryBlue, BackColor = Color.FromArgb(230, 240, 250), Location = new Point(280, 33), AutoSize = true, Padding = new Padding(5, 2, 5, 2) };
            pnlProfile.Controls.Add(badgeRole);

            pnlProfile.Controls.Add(new Label { Text = nv?.TenChucVu ?? "Nhân viên", Font = new Font("Segoe UI", 10), ForeColor = _textSecondary, Location = new Point(140, 60), AutoSize = true });

            // Thông tin liên hệ
            int y = 90;
            var infos = new[] {
                ("🆔", $"NV{_session?.MaNhanVien:D5}"),
                ("🏢", _session?.TenPhongBan ?? "Phòng ban"),
                ("✉️", string.IsNullOrEmpty(nv?.Email) ? "Chưa cập nhật" : nv.Email),
                ("📞", string.IsNullOrEmpty(nv?.SoDienThoai) ? "Chưa cập nhật" : nv.SoDienThoai)
            };
            foreach (var info in infos)
            {
                pnlProfile.Controls.Add(new Label { Text = info.Item1, Font = new Font("Segoe UI Emoji", 9), Location = new Point(140, y), AutoSize = true, ForeColor = _textSecondary });
                pnlProfile.Controls.Add(new Label { Text = info.Item2, Font = new Font("Segoe UI", 9), Location = new Point(165, y), AutoSize = true, ForeColor = _textSecondary });
                y += 22;
            }

            // Nút Xem hồ sơ
            var btnProfile = new Button
            {
                Text = "Xem hồ sơ cá nhân  →",
                FlatStyle = FlatStyle.Flat,
                ForeColor = _primaryBlue,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Height = 35
            };
            btnProfile.FlatAppearance.BorderColor = Color.FromArgb(220, 230, 240);
            btnProfile.Click += async (s, e) => {
                if (_session != null)
                {
                    try
                    {
                        var nvService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.INhanVienService>();
                        var pbService = Program.ServiceProvider.GetRequiredService<HRM.BLL.Interfaces.IPhongBanService>();
                        var cvRepo = Program.ServiceProvider.GetRequiredService<HRM.DAL.Repositories.IRepository<HRM.Domain.Entities.ChucVu>>();
                        
                        var nvDto = await nvService.GetByIdAsync(_session.MaNhanVien);
                        if (nvDto != null)
                        {
                            using var frm = new HRM.GUI.Forms.Main.frmSuaNhanVien(nvService, pbService, cvRepo, nvDto);
                            frm.Text = "Hồ sơ cá nhân";
                            
                            var btnLuu = frm.Controls.Find("btnLuu", true).FirstOrDefault();
                            if (btnLuu != null) btnLuu.Visible = false;

                            var lblTitle = frm.Controls.Find("lblTitle", true).FirstOrDefault() as Label;
                            if (lblTitle != null) lblTitle.Text = "THÔNG TIN NHÂN VIÊN";

                            void DisableControls(Control.ControlCollection controls)
                            {
                                foreach (Control c in controls)
                                {
                                    if (c is TextBox txt) txt.ReadOnly = true;
                                    else if (c is ComboBox cbo) cbo.Enabled = false;
                                    else if (c is DateTimePicker dtp) dtp.Enabled = false;
                                    
                                    if (c.Controls.Count > 0) DisableControls(c.Controls);
                                }
                            }
                            DisableControls(frm.Controls);

                            frm.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy thông tin cá nhân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi tải hồ sơ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            pnlProfile.Controls.Add(btnProfile);
            pnlProfile.Resize += (s, e) => { btnProfile.Location = new Point(20, pnlProfile.Height - 50); btnProfile.Width = pnlProfile.Width - 40; };

            tlp.Controls.Add(pnlProfile, 0, 0);

            // Cột phải: 4 Status Cards (Grid 1x4)
            var tlpCards = new TableLayoutPanel
            {
                RowCount = 1, ColumnCount = 4, Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0)
            };
            for (int i = 0; i < 4; i++) tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Map Real Data to Cards
            string checkInTime = todayCC?.GioVao?.ToString(@"hh\:mm") ?? "--:--";
            string checkInStatus = todayCC != null ? "✅ Bạn đã check-in" : "Chưa check-in";
            Color checkInColor = todayCC != null ? _successGreen : _textSecondary;

            string phepLeft = soPhep?.SoNgayConLai.ToString() ?? "0";
            string phepTotal = soPhep?.TongSoNgayPhep.ToString() ?? "0";
            
            string thucLinh = bangLuong?.LuongThucNhan?.ToString("N0") ?? "0";
            string ngayLuong = bangLuong?.NgayTinhLuong.ToString("dd/MM/yyyy") ?? "--/--/----";

            string kpiDiem = kpi?.DiemKPI != null ? $"{kpi.DiemKPI}/100" : "N/A";
            string kpiRate = kpi?.TrangThaiHoanThanh ?? "Chưa đánh giá";

            var c1 = CreateMiniCard("Chấm công hôm nay", checkInTime, "Giờ check-in", checkInStatus, checkInColor, "Xem chi tiết", () => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.ChamCong.ucChamCong(_session)));
            var c2 = CreateMiniCard("Ngày phép còn lại", phepLeft, "Ngày", $"Tổng: {phepTotal} ngày/năm", _primaryBlue, "Xin nghỉ phép", () => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.NghiPhep.ucNghiPhep(_session)));
            var c3 = CreateMiniCard("Lương tháng này", thucLinh, "Thực lĩnh", $"Ngày trả: {ngayLuong}", _textPrimary, "Xem bảng lương", () => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.BangLuong.ucBangLuong(_session)));
            var c4 = CreateMiniCard("KPI hiện tại", kpiDiem, "Điểm", kpiRate, _warningOrange, "Xem chi tiết", () => MessageBox.Show("Tính năng KPI đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information));

            c1.Margin = new Padding(0, 0, 10, 0);
            c2.Margin = new Padding(5, 0, 5, 0);
            c3.Margin = new Padding(5, 0, 5, 0);
            c4.Margin = new Padding(10, 0, 0, 0);

            tlpCards.Controls.Add(c1, 0, 0);
            tlpCards.Controls.Add(c2, 1, 0);
            tlpCards.Controls.Add(c3, 2, 0);
            tlpCards.Controls.Add(c4, 3, 0);

            tlp.Controls.Add(tlpCards, 1, 0);

            return tlp;
        }

        private Panel CreateMiniCard(string title, string mainValue, string unit, string subText, Color valColor, string btnText, Action onClick)
        {
            var pnl = CreateRoundedPanel();
            pnl.Dock = DockStyle.Fill;
            pnl.Padding = new Padding(15);

            pnl.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });
            
            var lblMain = new Label { Text = mainValue, Font = new Font("Segoe UI", mainValue.Length > 8 ? 18 : 22, FontStyle.Bold), ForeColor = valColor, Location = new Point(12, 45), AutoSize = true };
            pnl.Controls.Add(lblMain);
            
            pnl.Controls.Add(new Label { Text = unit, Font = new Font("Segoe UI", 9), ForeColor = _textSecondary, Location = new Point(15, 85), AutoSize = true });

            pnl.Controls.Add(new Label { Text = subText, Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(15, 120), AutoSize = true });

            var btn = new Button
            {
                Text = btnText,
                FlatStyle = FlatStyle.Flat,
                ForeColor = _primaryBlue,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                Height = 32
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(220, 230, 240);
            btn.Click += (s, e) => onClick?.Invoke();
            pnl.Controls.Add(btn);
            pnl.Resize += (s, e) => { btn.Location = new Point(15, pnl.Height - 47); btn.Width = pnl.Width - 30; };

            return pnl;
        }

        /// <summary>
        /// Tạo hàng giữa: Lịch làm việc (33%), Thông báo (33%), Đơn nghỉ phép (33%)
        /// </summary>
        private TableLayoutPanel CreateMiddleRow(List<DonNghiPhepDTO> donPheps)
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 3,
                Height = 280,
                Margin = new Padding(0, 0, 0, 20)
            };
            for (int i = 0; i < 3; i++) tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            // Cột 1: Lịch làm việc
            var pnlCal = CreateRoundedPanel();
            pnlCal.Dock = DockStyle.Fill;
            pnlCal.Margin = new Padding(0, 0, 10, 0);
            pnlCal.Controls.Add(new Label { Text = "Lịch làm việc hôm nay", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });
            
            // Mock Data Lịch
            string[] times = { "08:30 - 10:00", "10:30 - 12:00", "13:30 - 15:30", "16:00 - 17:30" };
            string[] tasks = { "Họp nhóm kinh doanh", "Gọi khách hàng, tư vấn", "Báo cáo doanh số tuần", "Chăm sóc khách hàng cũ" };
            string[] locs = { "Phòng họp A", "Online", "Phòng họp B", "Online" };
            Color[] colors = { _primaryBlue, _dangerRed, _successGreen, _textSecondary };

            int cy = 50;
            for(int i=0; i<4; i++)
            {
                var pnlItem = new Panel { Location = new Point(15, cy), Height = 35, Width = 300 };
                var line = new Panel { BackColor = colors[i], Width = 3, Height = 20, Location = new Point(0, 0) };
                pnlItem.Controls.Add(line);
                pnlItem.Controls.Add(new Label { Text = times[i], Font = new Font("Segoe UI", 9), ForeColor = _textSecondary, Location = new Point(10, 2), AutoSize = true });
                pnlItem.Controls.Add(new Label { Text = tasks[i], Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(100, 2), AutoSize = true });
                pnlItem.Controls.Add(new Label { Text = locs[i], Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(250, 4), AutoSize = true });
                
                pnlCal.Controls.Add(pnlItem);
                pnlCal.Resize += (s, e) => { pnlItem.Width = pnlCal.Width - 30; pnlItem.Controls[3].Location = new Point(pnlItem.Width - 70, 4); };
                cy += 40;
            }

            var btnCal = new Button { Text = "Xem lịch tuần", FlatStyle = FlatStyle.Flat, ForeColor = _primaryBlue, BackColor = Color.White, Height = 35 };
            btnCal.FlatAppearance.BorderColor = Color.FromArgb(220, 230, 240);
            btnCal.Click += (s, e) => MessageBox.Show("Tính năng Lịch làm việc đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            pnlCal.Controls.Add(btnCal);
            pnlCal.Resize += (s, e) => { btnCal.Location = new Point(15, pnlCal.Height - 50); btnCal.Width = pnlCal.Width - 30; };
            tlp.Controls.Add(pnlCal, 0, 0);

            // Cột 2: Thông báo
            var pnlNotif = CreateRoundedPanel();
            pnlNotif.Dock = DockStyle.Fill;
            pnlNotif.Margin = new Padding(5, 0, 5, 0);
            pnlNotif.Controls.Add(new Label { Text = "Thông báo công ty", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });
            
            var lblViewAll = new Label { Text = "Xem tất cả", Font = new Font("Segoe UI", 9), ForeColor = _primaryBlue, AutoSize = true, Cursor = Cursors.Hand };
            lblViewAll.Click += (s, e) => MessageBox.Show("Tính năng Thông báo công ty đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            pnlNotif.Controls.Add(lblViewAll);
            pnlNotif.Resize += (s, e) => { lblViewAll.Location = new Point(pnlNotif.Width - 80, 18); };

            int ny = 50;
            string[] nTitles = { "Thông báo nghỉ lễ 30/04 - 01/05", "Đào tạo kỹ năng bán hàng tháng 5", "Khảo sát mức độ hài lòng nhân viên" };
            string[] nSubs = { "Công ty thông báo lịch nghỉ lễ 30/04 - 01/05...", "Phòng Nhân sự thông báo khóa đào tạo...", "Quý Anh/Chị vui lòng tham gia khảo sát..." };
            string[] nDates = { "20/04/2024", "18/04/2024", "15/04/2024" };

            for(int i=0; i<3; i++)
            {
                var pnlItem = new Panel { Location = new Point(15, ny), Height = 55, Width = 300 };
                var badge = new Label { Text = "MỚI", Font = new Font("Segoe UI", 7, FontStyle.Bold), ForeColor = _successGreen, BackColor = Color.FromArgb(230, 250, 235), AutoSize = true, Location = new Point(0, 5), Padding = new Padding(3) };
                pnlItem.Controls.Add(badge);
                pnlItem.Controls.Add(new Label { Text = nTitles[i], Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(45, 2), AutoSize = true });
                pnlItem.Controls.Add(new Label { Text = nSubs[i], Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(45, 22), AutoSize = true });
                pnlItem.Controls.Add(new Label { Text = nDates[i], Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(230, 4), AutoSize = true });

                pnlNotif.Controls.Add(pnlItem);
                pnlNotif.Resize += (s, e) => { pnlItem.Width = pnlNotif.Width - 30; pnlItem.Controls[3].Location = new Point(pnlItem.Width - 70, 4); };
                ny += 60;
            }
            tlp.Controls.Add(pnlNotif, 1, 0);

            // Cột 3: Đơn nghỉ phép
            var pnlLeave = CreateRoundedPanel();
            pnlLeave.Dock = DockStyle.Fill;
            pnlLeave.Margin = new Padding(10, 0, 0, 0);
            pnlLeave.Controls.Add(new Label { Text = "Đơn nghỉ phép của tôi", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });
            
            var lblViewAll2 = new Label { Text = "Xem tất cả", Font = new Font("Segoe UI", 9), ForeColor = _primaryBlue, AutoSize = true, Cursor = Cursors.Hand };
            lblViewAll2.Click += (s, e) => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.NghiPhep.ucNghiPhep(_session));
            pnlLeave.Controls.Add(lblViewAll2);
            pnlLeave.Resize += (s, e) => { lblViewAll2.Location = new Point(pnlLeave.Width - 80, 18); };

            int ly = 50;
            if (donPheps.Count == 0)
            {
                pnlLeave.Controls.Add(new Label { Text = "Bạn chưa có đơn xin nghỉ phép nào.", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = _textSecondary, Location = new Point(15, ly), AutoSize = true });
            }
            else
            {
                foreach (var dp in donPheps)
                {
                    var pnlItem = new Panel { Location = new Point(15, ly), Height = 30, Width = 300 };
                    pnlItem.Controls.Add(new Label { Text = "🕒", Font = new Font("Segoe UI Emoji", 8), ForeColor = _warningOrange, Location = new Point(0, 2), AutoSize = true });
                    
                    var lblType = new Label { Text = dp.TenLoaiPhep ?? "Nghỉ phép", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(25, 2), AutoSize = false, Width = 100 };
                    pnlItem.Controls.Add(lblType);
                    
                    string dateStr = dp.NgayBatDau == dp.NgayKetThuc ? dp.NgayBatDau.ToString("dd/MM") : $"{dp.NgayBatDau:dd/MM} - {dp.NgayKetThuc:dd/MM}";
                    pnlItem.Controls.Add(new Label { Text = dateStr, Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(130, 4), AutoSize = true });
                    
                    Color statusColor = _textSecondary;
                    Color statusBgColor = Color.FromArgb(240, 240, 240);
                    if (dp.TrangThai == "Đã duyệt") { statusColor = _successGreen; statusBgColor = Color.FromArgb(230, 250, 235); }
                    else if (dp.TrangThai == "Từ chối" || dp.TrangThai == "Đã hủy") { statusColor = _dangerRed; statusBgColor = Color.FromArgb(255, 235, 235); }
                    else { statusColor = _warningOrange; statusBgColor = Color.FromArgb(255, 245, 230); }

                    var stBadge = new Label { Text = dp.TrangThai, Font = new Font("Segoe UI", 8), ForeColor = statusColor, BackColor = statusBgColor, AutoSize = true, Location = new Point(240, 2), Padding = new Padding(3, 1, 3, 1) };
                    pnlItem.Controls.Add(stBadge);

                    pnlLeave.Controls.Add(pnlItem);
                    pnlLeave.Resize += (s, e) => { pnlItem.Width = pnlLeave.Width - 30; stBadge.Location = new Point(pnlItem.Width - 60, 2); };
                    ly += 35;
                }
            }

            var btnLeave = new Button { Text = "Tạo đơn nghỉ phép", FlatStyle = FlatStyle.Flat, ForeColor = _primaryBlue, BackColor = Color.White, Height = 35 };
            btnLeave.FlatAppearance.BorderColor = Color.FromArgb(220, 230, 240);
            btnLeave.Click += (s, e) => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.NghiPhep.ucNghiPhep(_session));
            pnlLeave.Controls.Add(btnLeave);
            pnlLeave.Resize += (s, e) => { btnLeave.Location = new Point(15, pnlLeave.Height - 50); btnLeave.Width = pnlLeave.Width - 30; };
            tlp.Controls.Add(pnlLeave, 2, 0);

            return tlp;
        }

        /// <summary>
        /// Tạo hàng dưới: Chấm công tháng (75%) + Thao tác nhanh (25%)
        /// </summary>
        private TableLayoutPanel CreateBottomRow(List<ChamCongDTO> chamCongsThang)
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Height = 320,
                Margin = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Cột 1: Chấm công tháng
            var pnlMonth = CreateRoundedPanel();
            pnlMonth.Dock = DockStyle.Fill;
            pnlMonth.Margin = new Padding(0, 0, 10, 0);

            pnlMonth.Controls.Add(new Label { Text = $"Chấm công tháng {DateTime.Now.Month}/{DateTime.Now.Year}", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });
            
            // Tính toán chỉ số chấm công tháng thực tế
            int ngayCong = chamCongsThang.Count(x => x.GioVao.HasValue);
            int diMuon = chamCongsThang.Count(x => x.TrangThai != null && x.TrangThai.Contains("Đi muộn"));
            int veSom = chamCongsThang.Count(x => x.TrangThai != null && x.TrangThai.Contains("Về sớm"));
            // (Chưa tính nghỉ phép/không phép vì cần map với bảng đơn nghỉ phép, tạm để 0 hoặc lấy từ donPheps)
            
            var flpStats = new FlowLayoutPanel { Location = new Point(15, 50), Height = 70, WrapContents = false };
            pnlMonth.Controls.Add(flpStats);
            pnlMonth.Resize += (s, e) => { flpStats.Width = pnlMonth.Width - 30; };

            string[] sTitles = { "Ngày công", "Đi muộn", "Về sớm" };
            string[] sVals = { ngayCong.ToString(), diMuon.ToString(), veSom.ToString() };
            Color[] sColors = { _primaryBlue, _warningOrange, _successGreen };

            for (int i = 0; i < 3; i++)
            {
                int idx = i; // Tạo biến cục bộ để tránh lỗi closure capture trong lambda
                var sPnl = new Panel { Width = 90, Height = 60, Margin = new Padding(0, 0, 10, 0) };
                sPnl.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(Color.FromArgb(10, sColors[idx]));
                    e.Graphics.FillRoundedRectangle(brush, 0, 0, 89, 59, 5);
                };
                sPnl.Controls.Add(new Label { Text = sVals[idx], Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = sColors[idx], Location = new Point(5, 5), AutoSize = true });
                sPnl.Controls.Add(new Label { Text = sTitles[idx], Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, Location = new Point(8, 35), AutoSize = true });
                flpStats.Controls.Add(sPnl);
            }

            // Timeline 15 ngày
            var pnlTimeline = new Panel { Location = new Point(15, 130), Height = 120 };
            pnlMonth.Controls.Add(pnlTimeline);
            pnlMonth.Resize += (s, e) => { pnlTimeline.Width = pnlMonth.Width - 30; };

            pnlTimeline.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int days = 15;
                float step = (pnlTimeline.Width - 40) / (float)max(days - 1, 1);
                
                // Vẽ đường ngang đứt nét
                using var pen = new Pen(Color.FromArgb(230, 235, 240), 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                g.DrawLine(pen, 20, 60, pnlTimeline.Width - 20, 60);

                // Vẽ các node (15 ngày gần nhất từ chamCongsThang)
                using var fontDay = new Font("Segoe UI", 9, FontStyle.Bold);
                using var fontDate = new Font("Segoe UI", 8);
                
                var recentDays = chamCongsThang.OrderByDescending(x => x.NgayChamCong).Take(15).OrderBy(x => x.NgayChamCong).ToList();
                int actualDays = recentDays.Count;
                if (actualDays == 0) return;

                step = (pnlTimeline.Width - 40) / (float)Math.Max(actualDays - 1, 1);

                for (int i = 0; i < actualDays; i++)
                {
                    float x = 20 + i * step;
                    var cc = recentDays[i];
                    
                    string dow = cc.NgayChamCong.ToString("ddd"); // T2, T3...
                    string dateStr = cc.NgayChamCong.ToString("dd");
                    
                    var szDow = g.MeasureString(dow, fontDay);
                    g.DrawString(dow, fontDay, Brushes.Gray, x - szDow.Width / 2, 10);
                    
                    var szDate = g.MeasureString(dateStr, fontDate);
                    g.DrawString(dateStr, fontDate, Brushes.Black, x - szDate.Width / 2, 30);

                    // Vẽ icon trạng thái
                    Color cColor = Color.FromArgb(220, 230, 240);
                    string icon = "−";
                    Color iconColor = Color.Gray;

                    if (cc.GioVao.HasValue) 
                    { 
                        if (cc.TrangThai != null && cc.TrangThai.Contains("Đi muộn")) { cColor = Color.FromArgb(255, 245, 230); icon = "L"; iconColor = _warningOrange; }
                        else { cColor = Color.FromArgb(230, 250, 235); icon = "✓"; iconColor = _successGreen; }
                    }
                    else if (cc.TrangThai != null && cc.TrangThai.Contains("Phép"))
                    {
                        cColor = Color.FromArgb(230, 240, 255); icon = "✈"; iconColor = _primaryBlue;
                    }

                    g.FillEllipse(new SolidBrush(cColor), x - 12, 60 - 12, 24, 24);
                    var szIcon = g.MeasureString(icon, fontDate);
                    g.DrawString(icon, fontDate, new SolidBrush(iconColor), x - szIcon.Width / 2, 60 - szIcon.Height / 2);
                }
            };

            // Ghi chú (Legend) cho Timeline
            var flpLegend = new FlowLayoutPanel { Location = new Point(15, 260), Height = 30, WrapContents = false };
            pnlMonth.Controls.Add(flpLegend);
            pnlMonth.Resize += (s, e) => { flpLegend.Width = pnlMonth.Width - 30; };

            var legends = new[] {
                ("✓", "Đúng giờ", _successGreen),
                ("L", "Đi muộn", _warningOrange),
                ("✈", "Nghỉ phép", _primaryBlue),
                ("−", "Nghỉ/Không dữ liệu", Color.Gray)
            };

            foreach (var lg in legends)
            {
                var lblIcon = new Label { Text = lg.Item1, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = lg.Item3, AutoSize = true, Margin = new Padding(10, 5, 2, 0) };
                var lblText = new Label { Text = lg.Item2, Font = new Font("Segoe UI", 8), ForeColor = _textSecondary, AutoSize = true, Margin = new Padding(0, 7, 10, 0) };
                flpLegend.Controls.Add(lblIcon);
                flpLegend.Controls.Add(lblText);
            }

            tlp.Controls.Add(pnlMonth, 0, 0);

            // Cột 2: Thao tác nhanh
            var pnlQuick = CreateRoundedPanel();
            pnlQuick.Dock = DockStyle.Fill;
            pnlQuick.Margin = new Padding(10, 0, 0, 0);
            pnlQuick.Controls.Add(new Label { Text = "Thao tác nhanh", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(15, 15), AutoSize = true });

            var flpQ = new FlowLayoutPanel { Location = new Point(15, 50), Dock = DockStyle.Bottom, Height = 250, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlQuick.Controls.Add(flpQ);
            pnlQuick.Resize += (s, e) => { flpQ.Width = pnlQuick.Width - 30; };

            var actions = new[] {
                ("✅", "Check-in", Color.FromArgb(230, 250, 235), _successGreen, new Action(() => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.ChamCong.ucChamCong(_session)))),
                ("📋", "Xin nghỉ phép", Color.FromArgb(230, 240, 255), _primaryBlue, new Action(() => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.NghiPhep.ucNghiPhep(_session)))),
                ("💰", "Xem bảng lương", Color.FromArgb(245, 235, 255), Color.Purple, new Action(() => (this.ParentForm as frmMain)?.ShowModule(new HRM.GUI.Forms.Main.BangLuong.ucBangLuong(_session)))),
                ("🎯", "Xem KPI", Color.FromArgb(255, 245, 230), _warningOrange, new Action(() => MessageBox.Show("Tính năng KPI đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information)))
            };

            foreach (var a in actions)
            {
                var pnlA = new Panel { Width = 200, Height = 45, Margin = new Padding(0, 0, 0, 10), Cursor = Cursors.Hand };
                pnlA.Click += (s, e) => a.Item5?.Invoke();

                var iconBox = new Label { Text = a.Item1, Font = new Font("Segoe UI Emoji", 12), BackColor = a.Item3, ForeColor = a.Item4, AutoSize = false, Size = new Size(32, 32), Location = new Point(5, 6), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
                iconBox.Paint += (s, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; };
                iconBox.Click += (s, e) => a.Item5?.Invoke();
                
                var lblText = new Label { Text = a.Item2, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = _textPrimary, Location = new Point(45, 13), AutoSize = true, Cursor = Cursors.Hand };
                lblText.Click += (s, e) => a.Item5?.Invoke();

                pnlA.Controls.Add(iconBox);
                pnlA.Controls.Add(lblText);
                
                pnlA.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(230, 235, 240), 1);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawRoundedRectangle(pen, 0, 0, pnlA.Width - 1, pnlA.Height - 1, 5);
                };
                flpQ.Controls.Add(pnlA);
                flpQ.Resize += (s, e) => { pnlA.Width = flpQ.Width; };
            }

            tlp.Controls.Add(pnlQuick, 1, 0);

            return tlp;
        }

        private static Panel CreateRoundedPanel()
        {
            var pnl = new Panel { BackColor = Color.White };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(225, 230, 238), 1);
                e.Graphics.DrawRoundedRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1, 8);
            };
            return pnl;
        }

        private int max(int a, int b) => a > b ? a : b;
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
        }
    }
}
