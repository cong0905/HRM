using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.TongQuan
{
    /// <summary>
    /// Module Dashboard (Tổng quan) - Hiển thị thống kê, biểu đồ và hoạt động hệ thống.
    /// Giao diện được xây dựng hoàn toàn bằng code (Dynamic UI) để linh hoạt trong bố cục.
    /// </summary>
    public partial class ucTongQuan : UserControl
    {
        private readonly UserSessionDTO? _session;

        // --- Khai báo các biến lưu trữ dữ liệu thống kê ---
        private DashboardSummaryDTO? _summary;          // Dữ liệu tổng hợp (4 thẻ trên cùng)
        private List<PhongBanThongKeDTO>? _phongBanData; // Dữ liệu nhân viên theo phòng ban (Biểu đồ tròn)
        private List<TangTruongNhanSuDTO>? _tangTruongData; // Dữ liệu tăng trưởng (Biểu đồ đường)
        private List<HoatDongGanDayDTO>? _hoatDongData;  // Danh sách các hoạt động mới nhất
        private List<ThongBaoDashboardDTO>? _thongBaoData; // Các thông báo hệ thống

        // Container chính chứa toàn bộ các thành phần (Dạng cuộn dọc)
        private FlowLayoutPanel _mainContainer = null!;
        private bool _dashboardBuilt;

        // Bảng màu cho biểu đồ (Sử dụng mã màu hiện đại)
        private static readonly Color[] ChartColors = new[]
        {
            Color.FromArgb(41, 128, 185),   // Xanh dương đậm
            Color.FromArgb(46, 204, 113),   // Xanh lá
            Color.FromArgb(243, 156, 18),   // Cam
            Color.FromArgb(155, 89, 182),   // Tím
            Color.FromArgb(231, 76, 60),    // Đỏ
            Color.FromArgb(52, 152, 219),   // Xanh dương nhạt
            Color.FromArgb(26, 188, 156),   // Xanh ngọc
            Color.FromArgb(241, 196, 15),   // Vàng
        };

        public ucTongQuan() : this(null) { }

        public ucTongQuan(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 243, 247); // Màu nền xám nhạt hiện đại
            _session = session;

            // Nếu đang ở chế độ Design của Visual Studio thì không chạy logic lấy dữ liệu (tránh lỗi)
            if (UIHelper.IsDesignTime()) return;

            // Tải dữ liệu và xây dựng Dashboard khi UserControl được load
            Load += async (_, _) => await BuildDashboardAsync();
            SizeChanged += (_, _) => ApplyDashboardWidths();
            VisibleChanged += (_, _) =>
            {
                if (Visible && _dashboardBuilt)
                    ApplyDashboardWidths();
            };
        }

        /// <summary>
        /// Logic chính để lấy dữ liệu từ Service và vẽ giao diện Dashboard
        /// </summary>
        private async Task BuildDashboardAsync()
        {
            if (_dashboardBuilt) return;

            try
            {
                // Gọi Service để lấy dữ liệu đồng thời từ Database
                using var scope = Program.ServiceProvider.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IDashboardService>();
                
                _summary = await svc.GetSummaryAsync();
                _phongBanData = await svc.GetNhanVienTheoPhongBanAsync();
                _tangTruongData = await svc.GetTangTruongNhanSuAsync(6); // Lấy 6 tháng gần nhất
                _hoatDongData = await svc.GetHoatDongGanDayAsync(5);    // Lấy 5 hoạt động mới nhất
                _thongBaoData = await svc.GetThongBaoAsync();
            }
            catch (Exception ex)
            {
                Controls.Clear();
                Controls.Add(new Label
                {
                    Text = $"Lỗi tải dữ liệu Dashboard: {ex.Message}",
                    ForeColor = Color.Red,
                    AutoSize = true,
                    Location = new Point(20, 20)
                });
                return;
            }

            Controls.Clear();

            // Khởi tạo container chính dạng FlowLayoutPanel (cuộn dọc tự động)
            _mainContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(15, 15, 15, 30)
            };
            Controls.Add(_mainContainer);
            _mainContainer.Resize += (_, _) => ApplyDashboardWidths();

            // 1. Thêm TIÊU ĐỀ
            var lblTitle = new Label
            {
                Text = "Tổng quan hệ thống",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 55, 95),
                AutoSize = true,
                Margin = new Padding(5, 5, 5, 20)
            };
            _mainContainer.Controls.Add(lblTitle);

            // 2. Thêm HÀNG THỐNG KÊ NHANH (4 Card)
            _mainContainer.Controls.Add(CreateSummaryCards());

            // 3. Thêm HÀNG BIỂU ĐỒ (Tròn & Đường)
            _mainContainer.Controls.Add(CreateChartsRow());

            // 4. Thêm HÀNG DƯỚI CÙNG (Hoạt động, Lịch, Thông báo)
            _mainContainer.Controls.Add(CreateBottomRow());

            // 5. Thêm FOOTER
            var lblFooter = new Label
            {
                Text = $"© {DateTime.Now.Year} HRM System. All rights reserved.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(140, 150, 160),
                AutoSize = true,
                Margin = new Padding(5, 20, 5, 0)
            };
            _mainContainer.Controls.Add(lblFooter);

            _dashboardBuilt = true;
            ApplyDashboardWidths();
            BeginInvoke(ApplyDashboardWidths);
        }

        private int GetDashboardContentWidth()
        {
            var pad = _mainContainer?.Padding ?? new Padding(15);
            var w = ClientSize.Width;
            if (w < 200 && Parent is Control parent)
                w = parent.ClientSize.Width;
            if (w < 200)
                w = 900;
            return Math.Max(300, w - pad.Horizontal - 10);
        }

        private void ApplyDashboardWidths()
        {
            if (_mainContainer == null || _mainContainer.IsDisposed || !_dashboardBuilt)
                return;

            var rowWidth = GetDashboardContentWidth();
            _mainContainer.SuspendLayout();
            foreach (Control c in _mainContainer.Controls)
                c.Width = rowWidth;
            _mainContainer.ResumeLayout(true);
        }

        /// <summary>
        /// Tạo hàng chứa 4 thẻ thống kê nhanh (Nhân viên, Đi làm, Nghỉ phép, Đi muộn)
        /// </summary>
        private TableLayoutPanel CreateSummaryCards()
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 4,
                Height = 130,
                Margin = new Padding(0, 0, 0, 15)
            };
            // Chia đều 4 cột (mỗi cột 25%)
            for (int i = 0; i < 4; i++)
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // Định nghĩa dữ liệu cho từng thẻ
            var cards = new[]
            {
                new { Icon = "👥", Title = "Tổng nhân viên", Value = _summary!.TongNhanVien.ToString(),
                      SubText = $"↑ {_summary.PhanTramTangNV}% so với tháng trước", SubColor = Color.FromArgb(46, 204, 113),
                      AccentColor = Color.FromArgb(41, 128, 185) },
                new { Icon = "✅", Title = "Đi làm hôm nay", Value = _summary.DiLamHomNay.ToString(),
                      SubText = $"{_summary.PhanTramDiLam}% tổng nhân viên", SubColor = Color.FromArgb(46, 204, 113),
                      AccentColor = Color.FromArgb(46, 204, 113) },
                new { Icon = "📋", Title = "Nghỉ phép hôm nay", Value = _summary.NghiPhepHomNay.ToString(),
                      SubText = $"{(_summary.ChenhLechNghiPhep >= 0 ? "↑" : "↓")} {Math.Abs(_summary.ChenhLechNghiPhep)} so với hôm qua",
                      SubColor = _summary.ChenhLechNghiPhep >= 0 ? Color.FromArgb(231, 76, 60) : Color.FromArgb(46, 204, 113),
                      AccentColor = Color.FromArgb(243, 156, 18) },
                new { Icon = "⏰", Title = "Đi muộn hôm nay", Value = _summary.DiMuonHomNay.ToString(),
                      SubText = $"{(_summary.ChenhLechDiMuon >= 0 ? "↑" : "↓")} {Math.Abs(_summary.ChenhLechDiMuon)} so với hôm qua",
                      SubColor = _summary.ChenhLechDiMuon >= 0 ? Color.FromArgb(231, 76, 60) : Color.FromArgb(46, 204, 113),
                      AccentColor = Color.FromArgb(231, 76, 60) },
            };

            for (int i = 0; i < 4; i++)
            {
                var card = cards[i];
                var pnl = CreateRoundedPanel(); // Tạo panel bo góc
                pnl.Dock = DockStyle.Fill;
                pnl.Margin = new Padding(i == 0 ? 0 : 7, 0, i == 3 ? 0 : 8, 0);

                // Thanh màu nhấn ở đỉnh thẻ
                var accentBar = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = card.AccentColor };
                pnl.Controls.Add(accentBar);

                // Biểu tượng Emoji lớn làm mờ ở góc
                var lblIcon = new Label
                {
                    Text = card.Icon,
                    Font = new Font("Segoe UI Emoji", 24),
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    ForeColor = Color.FromArgb(180, 190, 200)
                };
                pnl.Controls.Add(lblIcon);
                pnl.Resize += (s, e) => { lblIcon.Location = new Point(pnl.Width - lblIcon.Width - 15, 15); };

                // Tiêu đề thẻ
                pnl.Controls.Add(new Label
                {
                    Text = card.Title,
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(120, 130, 140),
                    Location = new Point(15, 15),
                    AutoSize = true
                });

                // Giá trị số lớn
                pnl.Controls.Add(new Label
                {
                    Text = card.Value,
                    Font = new Font("Segoe UI", 28, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 50, 80),
                    Location = new Point(12, 40),
                    AutoSize = true
                });

                // Dòng chú thích nhỏ bên dưới (vd: % tăng trưởng)
                pnl.Controls.Add(new Label
                {
                    Text = card.SubText,
                    Font = new Font("Segoe UI", 8),
                    ForeColor = card.SubColor,
                    Location = new Point(15, pnl.Height - 30),
                    AutoSize = true,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
                });

                tlp.Controls.Add(pnl, i, 0);
            }

            return tlp;
        }

        /// <summary>
        /// Tạo hàng chứa 2 biểu đồ chính (Tròn và Đường)
        /// </summary>
        private TableLayoutPanel CreateChartsRow()
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 2,
                Height = 320,
                Margin = new Padding(0, 0, 0, 15)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Biểu đồ TRÒN (Donut)
            var pnlDonut = CreateRoundedPanel();
            pnlDonut.Dock = DockStyle.Fill;
            pnlDonut.Margin = new Padding(0, 0, 7, 0);

            pnlDonut.Controls.Add(new Label
            {
                Text = "Nhân viên theo phòng ban",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 55, 95),
                Location = new Point(15, 15),
                AutoSize = true
            });

            var donutCanvas = new Panel { BackColor = Color.White };
            pnlDonut.Controls.Add(donutCanvas);
            pnlDonut.Resize += (s, e) =>
            {
                donutCanvas.Location = new Point(15, 50);
                donutCanvas.Size = new Size(pnlDonut.Width - 30, pnlDonut.Height - 65);
                donutCanvas.Invalidate(); // Yêu cầu vẽ lại khi thay đổi kích thước
            };
            donutCanvas.Paint += DonutChart_Paint;

            tlp.Controls.Add(pnlDonut, 0, 0);

            // Biểu đồ ĐƯỜNG (Line)
            var pnlLine = CreateRoundedPanel();
            pnlLine.Dock = DockStyle.Fill;
            pnlLine.Margin = new Padding(8, 0, 0, 0);

            pnlLine.Controls.Add(new Label
            {
                Text = "Tăng trưởng nhân sự (6 tháng gần nhất)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 55, 95),
                Location = new Point(15, 15),
                AutoSize = true
            });

            var lineCanvas = new Panel { BackColor = Color.White };
            pnlLine.Controls.Add(lineCanvas);
            pnlLine.Resize += (s, e) =>
            {
                lineCanvas.Location = new Point(15, 50);
                lineCanvas.Size = new Size(pnlLine.Width - 30, pnlLine.Height - 65);
                lineCanvas.Invalidate();
            };
            lineCanvas.Paint += LineChart_Paint;

            tlp.Controls.Add(pnlLine, 1, 0);

            return tlp;
        }

        /// <summary>
        /// Tạo hàng dưới cùng gồm: Hoạt động gần đây, Lịch và Thông báo
        /// </summary>
        private TableLayoutPanel CreateBottomRow()
        {
            var tlp = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 3,
                Height = 360,
                Margin = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            // Cột 1: HOẠT ĐỘNG GẦN ĐÂY
            var pnlActivity = CreateRoundedPanel();
            pnlActivity.Dock = DockStyle.Fill;
            pnlActivity.Margin = new Padding(0, 0, 7, 0);
            pnlActivity.Controls.Add(new Label { Text = "Hoạt động gần đây", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(25, 55, 95), Location = new Point(15, 15), AutoSize = true });

            var activityScroll = new Panel { AutoScroll = true, BackColor = Color.White, Location = new Point(10, 50) };
            pnlActivity.Controls.Add(activityScroll);
            pnlActivity.Resize += (s, e) => { activityScroll.Size = new Size(pnlActivity.Width - 20, pnlActivity.Height - 60); };

            int ay = 0;
            if (_hoatDongData != null)
            {
                foreach (var hd in _hoatDongData)
                {
                    var itemPanel = new Panel { Location = new Point(5, ay), Height = 55, BackColor = Color.White };
                    var avatar = new Panel { Location = new Point(0, 5), Size = new Size(36, 36) };
                    avatar.Paint += (s, e) => {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(41, 128, 185)), 0, 0, 35, 35);
                        var initials = hd.TenNhanVien.Length > 0 ? hd.TenNhanVien[0].ToString() : "?";
                        using var font = new Font("Segoe UI", 12, FontStyle.Bold);
                        var sz = e.Graphics.MeasureString(initials, font);
                        e.Graphics.DrawString(initials, font, Brushes.White, (35 - sz.Width) / 2, (35 - sz.Height) / 2);
                    };
                    itemPanel.Controls.Add(avatar);
                    itemPanel.Controls.Add(new Label { Text = hd.TenNhanVien, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(45, 3), AutoSize = true });
                    itemPanel.Controls.Add(new Label { Text = $"{hd.MoTa}  ·  {hd.ThoiGian}", Font = new Font("Segoe UI", 8), ForeColor = Color.Gray, Location = new Point(45, 22), AutoSize = true });
                    activityScroll.Controls.Add(itemPanel);
                    ay += 55;
                }
            }

            // Cột 2: LỊCH VÀ SỰ KIỆN
            var pnlCalendar = CreateRoundedPanel();
            pnlCalendar.Dock = DockStyle.Fill;
            pnlCalendar.Margin = new Padding(8, 0, 7, 0);
            pnlCalendar.Controls.Add(new Label { Text = "Lịch & Sự kiện", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(25, 55, 95), Location = new Point(15, 15), AutoSize = true });
            var calendar = new MonthCalendar { MaxSelectionCount = 1, ShowTodayCircle = true, BackColor = Color.White };
            pnlCalendar.Controls.Add(calendar);
            pnlCalendar.Resize += (s, e) => { calendar.Location = new Point((pnlCalendar.Width - calendar.Width) / 2, 50); };

            // Cột 3: THÔNG BÁO HỆ THỐNG
            var pnlNotif = CreateRoundedPanel();
            pnlNotif.Dock = DockStyle.Fill;
            pnlNotif.Margin = new Padding(8, 0, 0, 0);
            pnlNotif.Controls.Add(new Label { Text = "Thông báo", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(25, 55, 95), Location = new Point(15, 15), AutoSize = true });
            
            var notifScroll = new Panel { AutoScroll = true, BackColor = Color.White, Location = new Point(10, 50) };
            pnlNotif.Controls.Add(notifScroll);
            pnlNotif.Resize += (s, e) => { notifScroll.Size = new Size(pnlNotif.Width - 20, pnlNotif.Height - 60); };

            int ny = 0;
            if (_thongBaoData != null)
            {
                foreach (var tb in _thongBaoData)
                {
                    var row = new Panel { Location = new Point(5, ny), Height = 45, BackColor = Color.White };
                    row.Controls.Add(new Label { Text = tb.Icon, Font = new Font("Segoe UI Emoji", 14), Location = new Point(0, 5), AutoSize = true });
                    row.Controls.Add(new Label { Text = tb.NoiDung, Font = new Font("Segoe UI", 9), Location = new Point(35, 10), AutoSize = true });
                    notifScroll.Controls.Add(row);
                    ny += 45;
                }
            }
            
            tlp.Controls.Add(pnlActivity, 0, 0);
            tlp.Controls.Add(pnlCalendar, 1, 0);
            tlp.Controls.Add(pnlNotif, 2, 0);

            return tlp;
        }

        // --- CÁC HÀM VẼ BIỂU ĐỒ BẰNG GDI+ ---

        // 1. Vẽ biểu đồ tròn (Donut Chart) - Thống kê nhân viên theo phòng ban
        private void DonutChart_Paint(object? sender, PaintEventArgs e)
        {
            if (_phongBanData == null || _phongBanData.Count == 0) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var panel = (Panel)sender!;
            int chartSize = Math.Min(panel.Height - 20, (panel.Width / 2) - 20);
            if (chartSize <= 0) return;

            int cx = chartSize / 2 + 10;
            int cy = panel.Height / 2;
            var rect = new Rectangle(cx - chartSize / 2, cy - chartSize / 2, chartSize, chartSize);
            int innerSize = (int)(chartSize * 0.55);
            var innerRect = new Rectangle(cx - innerSize / 2, cy - innerSize / 2, innerSize, innerSize);

            float startAngle = -90f;
            for (int i = 0; i < _phongBanData.Count; i++)
            {
                var pb = _phongBanData[i];
                float sweepAngle = (float)pb.PhanTram / 100f * 360f;
                using var brush = new SolidBrush(ChartColors[i % ChartColors.Length]);
                g.FillPie(brush, rect, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            using var whiteBrush = new SolidBrush(Color.White);
            g.FillEllipse(whiteBrush, innerRect);

            using var centerFont = new Font("Segoe UI", 16, FontStyle.Bold);
            var total = _phongBanData.Sum(p => p.SoNhanVien).ToString();
            var sz = g.MeasureString(total, centerFont);
            g.DrawString(total, centerFont, Brushes.Black, cx - sz.Width / 2, cy - sz.Height / 2);

            int legendX = cx + chartSize / 2 + 25;
            int legendY = cy - (_phongBanData.Count * 24) / 2;
            if (legendY < 10) legendY = 10;
            using var legendFont = new Font("Segoe UI", 8.5f);

            for (int i = 0; i < _phongBanData.Count && i < 6; i++)
            {
                var pb = _phongBanData[i];
                using var colorBrush = new SolidBrush(ChartColors[i % ChartColors.Length]);
                g.FillRectangle(colorBrush, legendX, legendY + 2, 12, 12);
                g.DrawString($"{pb.TenPhongBan}  {pb.SoNhanVien} ({pb.PhanTram}%)",
                    legendFont, Brushes.Black, legendX + 18, legendY);
                legendY += 24;
            }
        }

        // 2. Vẽ biểu đồ đường (Line Chart) - Thống kê tăng trưởng 6 tháng
        private void LineChart_Paint(object? sender, PaintEventArgs e)
        {
            if (_tangTruongData == null || _tangTruongData.Count < 2) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var panel = (Panel)sender!;
            int paddingX = 40;
            int paddingY = 30;
            int chartW = panel.Width - paddingX * 2;
            int chartH = panel.Height - paddingY * 2;
            if (chartW <= 0 || chartH <= 0) return;

            int maxVal = _tangTruongData.Max(t => t.SoNhanVien);
            int minVal = _tangTruongData.Min(t => t.SoNhanVien);
            int range = Math.Max(maxVal - minVal, 1);
            int yMin = Math.Max(0, minVal - (int)(range * 0.15));
            int yMax = maxVal + (int)(range * 0.15);
            int yRange = Math.Max(yMax - yMin, 1);

            using var gridPen = new Pen(Color.FromArgb(235, 238, 242), 1);
            using var axisFont = new Font("Segoe UI", 8);

            for (int i = 0; i <= 4; i++)
            {
                int yy = paddingY + chartH - (i * chartH / 4);
                g.DrawLine(gridPen, paddingX, yy, paddingX + chartW, yy);
                int val = yMin + (i * yRange / 4);
                g.DrawString(val.ToString(), axisFont, Brushes.Gray, 5, yy - 8);
            }

            var points = new PointF[_tangTruongData.Count];
            for (int i = 0; i < _tangTruongData.Count; i++)
            {
                float x = paddingX + (float)i / (_tangTruongData.Count - 1) * chartW;
                float yNorm = (float)(_tangTruongData[i].SoNhanVien - yMin) / yRange;
                float yy = paddingY + chartH - yNorm * chartH;
                points[i] = new PointF(x, yy);

                var monthSize = g.MeasureString(_tangTruongData[i].TenThang, axisFont);
                g.DrawString(_tangTruongData[i].TenThang, axisFont, Brushes.Gray,
                    x - monthSize.Width / 2, paddingY + chartH + 8);
            }

            if (points.Length >= 2)
            {
                var fillPts = new List<PointF>(points)
                {
                    new PointF(points[^1].X, paddingY + chartH),
                    new PointF(points[0].X, paddingY + chartH)
                };
                using var fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(paddingX, paddingY, chartW, chartH),
                    Color.FromArgb(60, 41, 128, 185), Color.FromArgb(5, 41, 128, 185), 90f);
                g.FillPolygon(fillBrush, fillPts.ToArray());
            }

            using var linePen = new Pen(Color.FromArgb(41, 128, 185), 2.5f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            g.DrawLines(linePen, points);

            using var dotBrush = new SolidBrush(Color.White);
            using var dotPen = new Pen(Color.FromArgb(41, 128, 185), 2f);
            using var valFont = new Font("Segoe UI", 8, FontStyle.Bold);
            foreach (var pt in points)
            {
                g.FillEllipse(dotBrush, pt.X - 5, pt.Y - 5, 10, 10);
                g.DrawEllipse(dotPen, pt.X - 5, pt.Y - 5, 10, 10);

                var valStr = _tangTruongData[Array.IndexOf(points, pt)].SoNhanVien.ToString();
                var valSize = g.MeasureString(valStr, valFont);
                g.DrawString(valStr, valFont, Brushes.Black, pt.X - valSize.Width / 2, pt.Y - 20);
            }
        }

        /// <summary>
        /// Hàm tiện ích tạo Panel với hiệu ứng bo góc và viền nhạt
        /// </summary>
        private static Panel CreateRoundedPanel()
        {
            var pnl = new Panel { BackColor = Color.White };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(225, 230, 238), 1);
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                int radius = 10;
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                pnl.Region = new Region(path); // Cắt panel theo đường bo góc
                e.Graphics.DrawPath(pen, path); // Vẽ viền
            };
            return pnl;
        }
    }
}
