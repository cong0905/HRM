using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;

namespace HRM.GUI.Forms.Main.HieuSuat;

public sealed partial class frmHieuSuat : UserControl
{
    private readonly INhanVienService _nhanVienService;
    private readonly IHieuSuatService _hieuSuatService;

    private Label _lblTitle = null!;
    private TextBox _txtSearch = null!;
    private Label _lblKy = null!;
    private ComboBox _cboKyDanhGia = null!;
    private Button _btnReset = null!;
    private Button _btnKyDanhGia = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnExport = null!;
    private DataGridView _dgv = null!;

    // --- Chart controls ---
    private TabControl _tabControl = null!;
    private TabPage _tabDanhSach = null!;
    private TabPage _tabBieuDo = null!;
    internal Panel _chartColumn = null!;
    internal Panel _chartPie = null!;
    internal Panel _chartLine = null!;

    // --- Chart Data ---
    private readonly List<ColumnChartItem> _columnChartData = new();
    private int _pieChartTotal;
    private int _pieXuatSac;
    private int _pieTot;
    private int _pieKha;
    private int _pieTb;
    private readonly List<LineChartItem> _lineChartData = new();

    internal sealed class ColumnChartItem
    {
        public string TenNhanVien { get; set; } = string.Empty;
        public decimal DiemKPI { get; set; }
        public decimal TyLeHoanThanhDeadline { get; set; }
        public decimal DiemChuyenCan { get; set; }
    }

    internal sealed class LineChartItem
    {
        public string TenKyDanhGia { get; set; } = string.Empty;
        public double DiemTB { get; set; }
    }

    private List<KyDanhGiaDTO> _kyDanhGiaItems = new();
    private bool _isReloadingKy;
    private bool _isLoadingGrid;

    public frmHieuSuat(INhanVienService nhanVienService, IHieuSuatService hieuSuatService)
    {
        _nhanVienService = nhanVienService;
        _hieuSuatService = hieuSuatService;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Load += frmHieuSuat_Load;
    }

    private async void frmHieuSuat_Load(object? sender, EventArgs e)
    {
        Load -= frmHieuSuat_Load;
        BuildLayout();
        WireEvents();
        await ReloadKyDanhGiaAsync();
        await LoadGridAsync();
    }

    private void BuildLayout()
    {
        _lblTitle = UIHelper.CreateModuleTitleLabel("📈 Quản lý hiệu suất", new Point(20, 15));
        _txtSearch = UIHelper.CreateSearchTextBox(new Point(20, 60), new Size(260, 25), "Tên nhân viên / phòng ban...");
        _lblKy = UIHelper.CreateFilterLabel("Kỳ đánh giá:", new Point(290, 63));
        _cboKyDanhGia = UIHelper.CreateFilterComboBox(new Point(370, 60), new Size(220, 25));

        _btnKyDanhGia = UIHelper.CreateActionButton(
            "🗂️ Kỳ đánh giá",
            new Point(600, 59),
            new Size(105, 28),
            Color.FromArgb(52, 152, 219));

        _btnReset = UIHelper.CreateActionButton(
            "🔄 Reset",
            new Point(710, 59),
            new Size(70, 28),
            Color.FromArgb(149, 165, 166));

        _btnAdd = UIHelper.CreateActionButton(
            "➕ Thêm mới",
            new Point(790, 59),
            new Size(100, 28),
            Color.FromArgb(46, 204, 113));

        _btnEdit = UIHelper.CreateActionButton(
            "✏️ Sửa",
            new Point(900, 59),
            new Size(70, 28),
            Color.FromArgb(241, 196, 15));

        _btnDelete = UIHelper.CreateActionButton(
            "🗑️ Xóa",
            new Point(980, 59),
            new Size(70, 28),
            Color.FromArgb(231, 76, 60));

        _btnExport = UIHelper.CreateActionButton(
            "⬇️ Xuất CSV",
            new Point(1060, 59),
            new Size(90, 28),
            Color.FromArgb(52, 152, 219));

        // ---- TabControl ----
        _tabControl = new TabControl
        {
            Location = new Point(20, 100),
            Size = new Size(Width - 40, Height - 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
        };

        // Tab 1: Danh sách (chứa DataGridView)
        _tabDanhSach = new TabPage("📋 Danh sách");
        _tabDanhSach.BackColor = Color.White;
        _tabDanhSach.Padding = new Padding(0);

        _dgv = UIHelper.CreateStyledDataGridView("dgvHieuSuat");
        _dgv.Dock = DockStyle.Fill;
        _tabDanhSach.Controls.Add(_dgv);

        // Tab 2: Biểu đồ thống kê (chứa 3 charts)
        _tabBieuDo = new TabPage("📊 Biểu đồ thống kê");
        _tabBieuDo.BackColor = Color.FromArgb(245, 247, 252);
        _tabBieuDo.Padding = new Padding(10);

        var chartLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
        };
        chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        _chartColumn = CreateColumnChart();
        _chartPie = CreatePieChart();
        _chartLine = CreateLineChart();

        chartLayout.Controls.Add(_chartColumn, 0, 0);  // Row 0, Col 0
        chartLayout.Controls.Add(_chartPie, 1, 0);      // Row 0, Col 1
        chartLayout.Controls.Add(_chartLine, 0, 1);      // Row 1, Col 0 — span 2 cols
        chartLayout.SetColumnSpan(_chartLine, 2);

        _tabBieuDo.Controls.Add(chartLayout);

        _tabControl.TabPages.Add(_tabDanhSach);
        _tabControl.TabPages.Add(_tabBieuDo);

        Controls.Add(_lblTitle);
        Controls.Add(_txtSearch);
        Controls.Add(_lblKy);
        Controls.Add(_cboKyDanhGia);
        Controls.Add(_btnKyDanhGia);
        Controls.Add(_btnReset);
        Controls.Add(_btnAdd);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDelete);
        Controls.Add(_btnExport);
        Controls.Add(_tabControl);
    }

    // ===================== Chart Factory Methods =====================

    private Panel CreateColumnChart()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(5),
        };
        panel.Paint += ColumnChart_Paint;
        return panel;
    }

    private Panel CreatePieChart()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(5),
        };
        panel.Paint += PieChart_Paint;
        return panel;
    }

    private Panel CreateLineChart()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(5),
        };
        panel.Paint += LineChart_Paint;
        return panel;
    }

    // ===================== GDI+ Paint Handlers =====================

    private void ColumnChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var panel = (Panel)sender!;
        g.Clear(Color.White);

        using (var borderPen = new Pen(Color.FromArgb(225, 230, 238), 1))
        {
            g.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
        }

        using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
        {
            var titleText = _columnChartData.Count == 0 
                ? "So sánh chỉ số nhân viên (không có dữ liệu)" 
                : $"So sánh chỉ số nhân viên (Top {_columnChartData.Count})";
            var titleSize = g.MeasureString(titleText, titleFont);
            g.DrawString(titleText, titleFont, new SolidBrush(Color.FromArgb(44, 62, 80)), (panel.Width - titleSize.Width) / 2, 10);
        }

        if (_columnChartData.Count == 0) return;

        int paddingLeft = 45;
        int paddingRight = 15;
        int paddingTop = 35;
        int paddingBottom = 55;

        int chartW = panel.Width - paddingLeft - paddingRight;
        int chartH = panel.Height - paddingTop - paddingBottom;
        if (chartW <= 0 || chartH <= 0) return;

        using (var gridPen = new Pen(Color.FromArgb(240, 240, 240), 1))
        using (var labelFont = new Font("Segoe UI", 8))
        using (var labelBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
        {
            for (int i = 0; i <= 5; i++)
            {
                int pct = i * 20;
                float y = paddingTop + chartH - ((float)pct / 100f * chartH);
                g.DrawLine(gridPen, paddingLeft, y, paddingLeft + chartW, y);
                g.DrawString($"{pct}%", labelFont, labelBrush, 8, y - 6);
            }
        }

        int count = _columnChartData.Count;
        float groupGap = 10f;
        float totalGaps = groupGap * (count - 1);
        float availableWidth = chartW - totalGaps;
        float groupWidth = availableWidth / count;

        float barWidth = groupWidth / 3.4f;
        float barGap = (groupWidth - (barWidth * 3)) / 2;

        using (var kpiBrush = new SolidBrush(Color.FromArgb(52, 152, 219)))
        using (var deadlineBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
        using (var ccBrush = new SolidBrush(Color.FromArgb(230, 126, 34)))
        using (var valFont = new Font("Segoe UI", 7, FontStyle.Bold))
        using (var nameFont = new Font("Segoe UI", 7.5f))
        using (var textBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
        {
            for (int i = 0; i < count; i++)
            {
                var item = _columnChartData[i];
                float groupX = paddingLeft + (i * (groupWidth + groupGap));

                // KPI
                float hKpi = (float)(item.DiemKPI / 100m) * chartH;
                if (hKpi < 0) hKpi = 0;
                if (hKpi > chartH) hKpi = chartH;
                float yKpi = paddingTop + chartH - hKpi;
                g.FillRectangle(kpiBrush, groupX, yKpi, barWidth, hKpi);

                string kpiText = item.DiemKPI.ToString("0");
                var kpiTextSize = g.MeasureString(kpiText, valFont);
                g.DrawString(kpiText, valFont, textBrush, groupX + (barWidth - kpiTextSize.Width) / 2, yKpi - 10);

                // Deadline
                float xDl = groupX + barWidth + barGap;
                float hDl = (float)(item.TyLeHoanThanhDeadline / 100m) * chartH;
                if (hDl < 0) hDl = 0;
                if (hDl > chartH) hDl = chartH;
                float yDl = paddingTop + chartH - hDl;
                g.FillRectangle(deadlineBrush, xDl, yDl, barWidth, hDl);

                string dlText = item.TyLeHoanThanhDeadline.ToString("0");
                var dlTextSize = g.MeasureString(dlText, valFont);
                g.DrawString(dlText, valFont, textBrush, xDl + (barWidth - dlTextSize.Width) / 2, yDl - 10);

                // ChuyenCan
                float xCc = groupX + (barWidth * 2) + (barGap * 2);
                float hCc = (float)(item.DiemChuyenCan / 100m) * chartH;
                if (hCc < 0) hCc = 0;
                if (hCc > chartH) hCc = chartH;
                float yCc = paddingTop + chartH - hCc;
                g.FillRectangle(ccBrush, xCc, yCc, barWidth, hCc);

                string ccText = item.DiemChuyenCan.ToString("0");
                var ccTextSize = g.MeasureString(ccText, valFont);
                g.DrawString(ccText, valFont, textBrush, xCc + (barWidth - ccTextSize.Width) / 2, yCc - 10);

                // Rotated name
                string name = TruncateName(item.TenNhanVien, 10);
                var state = g.Save();
                float labelX = groupX + groupWidth / 2;
                float labelY = paddingTop + chartH + 5;
                g.TranslateTransform(labelX, labelY);
                g.RotateTransform(-25);
                var nameSize = g.MeasureString(name, nameFont);
                g.DrawString(name, nameFont, textBrush, -nameSize.Width, -nameSize.Height / 2);
                g.Restore(state);
            }
        }

        // Legend
        using (var legendFont = new Font("Segoe UI", 8))
        using (var kpiBrush = new SolidBrush(Color.FromArgb(52, 152, 219)))
        using (var deadlineBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
        using (var ccBrush = new SolidBrush(Color.FromArgb(230, 126, 34)))
        {
            int legendY = panel.Height - 18;
            int itemWidth = 90;
            int startX = (panel.Width - (itemWidth * 3)) / 2;

            g.FillRectangle(kpiBrush, startX, legendY + 1, 10, 10);
            g.DrawString("Điểm KPI", legendFont, Brushes.Black, startX + 14, legendY - 1);

            g.FillRectangle(deadlineBrush, startX + itemWidth, legendY + 1, 10, 10);
            g.DrawString("% Deadline", legendFont, Brushes.Black, startX + itemWidth + 14, legendY - 1);

            g.FillRectangle(ccBrush, startX + itemWidth * 2, legendY + 1, 10, 10);
            g.DrawString("Chuyên cần", legendFont, Brushes.Black, startX + itemWidth * 2 + 14, legendY - 1);
        }
    }

    private void PieChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var panel = (Panel)sender!;
        g.Clear(Color.White);

        using (var borderPen = new Pen(Color.FromArgb(225, 230, 238), 1))
        {
            g.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
        }

        using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
        {
            var titleText = _pieChartTotal == 0 ? "Phân loại nhân viên (không có dữ liệu)" : "Phân loại nhân viên";
            var titleSize = g.MeasureString(titleText, titleFont);
            g.DrawString(titleText, titleFont, new SolidBrush(Color.FromArgb(44, 62, 80)), (panel.Width - titleSize.Width) / 2, 10);
        }

        if (_pieChartTotal == 0) return;

        int chartSize = Math.Min(panel.Height - 65, (panel.Width / 2) - 15);
        if (chartSize <= 0) return;

        int cx = chartSize / 2 + 10;
        int cy = panel.Height / 2 + 5;
        var rect = new Rectangle(cx - chartSize / 2, cy - chartSize / 2, chartSize, chartSize);
        int innerSize = (int)(chartSize * 0.55);
        var innerRect = new Rectangle(cx - innerSize / 2, cy - innerSize / 2, innerSize, innerSize);

        var categories = new[]
        {
            ("Xuất sắc (≥90)", _pieXuatSac, Color.FromArgb(155, 89, 182)),
            ("Tốt (75-89)", _pieTot, Color.FromArgb(26, 188, 156)),
            ("Khá (60-74)", _pieKha, Color.FromArgb(243, 156, 18)),
            ("Trung bình (<60)", _pieTb, Color.FromArgb(231, 76, 60)),
        };

        float startAngle = -90f;
        for (int i = 0; i < categories.Length; i++)
        {
            var cat = categories[i];
            if (cat.Item2 == 0) continue;
            float sweepAngle = (float)cat.Item2 / _pieChartTotal * 360f;
            using var brush = new SolidBrush(cat.Item3);
            g.FillPie(brush, rect, startAngle, sweepAngle);
            startAngle += sweepAngle;
        }

        using (var whiteBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(whiteBrush, innerRect);
        }

        using (var centerFont = new Font("Segoe UI", 13, FontStyle.Bold))
        {
            var totalStr = _pieChartTotal.ToString();
            var sz = g.MeasureString(totalStr, centerFont);
            g.DrawString(totalStr, centerFont, Brushes.Black, cx - sz.Width / 2, cy - sz.Height / 2 - 8);

            using (var nvFont = new Font("Segoe UI", 7.5f))
            {
                var nvSz = g.MeasureString("nhân viên", nvFont);
                g.DrawString("nhân viên", nvFont, Brushes.Gray, cx - nvSz.Width / 2, cy - nvSz.Height / 2 + 8);
            }
        }

        int legendX = cx + chartSize / 2 + 15;
        int legendY = cy - (categories.Length * 18) / 2;
        using var legendFont = new Font("Segoe UI", 8);

        for (int i = 0; i < categories.Length; i++)
        {
            var cat = categories[i];
            using var colorBrush = new SolidBrush(cat.Item3);
            g.FillRectangle(colorBrush, legendX, legendY + 2, 10, 10);

            int pct = _pieChartTotal > 0 ? (int)Math.Round((double)cat.Item2 / _pieChartTotal * 100) : 0;
            g.DrawString($"{cat.Item1}: {cat.Item2} ({pct}%)", legendFont, Brushes.Black, legendX + 14, legendY - 1);
            legendY += 18;
        }
    }

    private void LineChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var panel = (Panel)sender!;
        g.Clear(Color.White);

        using (var borderPen = new Pen(Color.FromArgb(225, 230, 238), 1))
        {
            g.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
        }

        using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
        {
            var titleText = _lineChartData.Count == 0 ? "Xu hướng điểm trung bình (không có dữ liệu)" : "Xu hướng điểm trung bình qua các kỳ";
            var titleSize = g.MeasureString(titleText, titleFont);
            g.DrawString(titleText, titleFont, new SolidBrush(Color.FromArgb(44, 62, 80)), (panel.Width - titleSize.Width) / 2, 10);
        }

        if (_lineChartData.Count == 0) return;

        int paddingX = 45;
        int paddingY = 30;
        int chartW = panel.Width - paddingX * 2;
        int chartH = panel.Height - paddingY * 2;
        if (chartW <= 0 || chartH <= 0) return;

        using (var gridPen = new Pen(Color.FromArgb(240, 240, 240), 1))
        using (var axisFont = new Font("Segoe UI", 8))
        using (var labelBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
        {
            for (int i = 0; i <= 5; i++)
            {
                int pct = i * 20;
                float y = paddingY + chartH - ((float)pct / 100f * chartH);
                g.DrawLine(gridPen, paddingX, y, paddingX + chartW, y);
                g.DrawString($"{pct}%", axisFont, labelBrush, 10, y - 6);
            }
        }

        int count = _lineChartData.Count;
        var points = new PointF[count];
        using (var nameFont = new Font("Segoe UI", 8))
        using (var textBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
        {
            for (int i = 0; i < count; i++)
            {
                var item = _lineChartData[i];
                float x = paddingX;
                if (count > 1)
                {
                    x = paddingX + ((float)i / (count - 1) * chartW);
                }
                else
                {
                    x = paddingX + (chartW / 2f);
                }

                float yNorm = (float)(item.DiemTB / 100.0);
                if (yNorm < 0) yNorm = 0;
                if (yNorm > 1) yNorm = 1;
                float y = paddingY + chartH - (yNorm * chartH);
                points[i] = new PointF(x, y);

                var truncatedKy = TruncateName(item.TenKyDanhGia, 12);
                var kySize = g.MeasureString(truncatedKy, nameFont);
                g.DrawString(truncatedKy, nameFont, textBrush, x - kySize.Width / 2, paddingY + chartH + 6);
            }
        }

        if (points.Length >= 2)
        {
            var fillPts = new List<PointF>(points)
            {
                new PointF(points[^1].X, paddingY + chartH),
                new PointF(points[0].X, paddingY + chartH)
            };
            using (var fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(paddingX, paddingY, chartW, chartH),
                Color.FromArgb(50, 41, 128, 185), Color.FromArgb(5, 41, 128, 185), 90f))
            {
                g.FillPolygon(fillBrush, fillPts.ToArray());
            }
        }

        if (points.Length >= 2)
        {
            using (var linePen = new Pen(Color.FromArgb(41, 128, 185), 2.5f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
            {
                g.DrawLines(linePen, points);
            }
        }

        using (var dotBrush = new SolidBrush(Color.White))
        using (var dotPen = new Pen(Color.FromArgb(41, 128, 185), 2f))
        using (var valFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
        using (var valBrush = new SolidBrush(Color.FromArgb(44, 62, 80)))
        {
            for (int i = 0; i < points.Length; i++)
            {
                var pt = points[i];
                var item = _lineChartData[i];

                g.FillEllipse(dotBrush, pt.X - 4, pt.Y - 4, 8, 8);
                g.DrawEllipse(dotPen, pt.X - 4, pt.Y - 4, 8, 8);

                string valStr = item.DiemTB.ToString("0.0") + "%";
                var valSize = g.MeasureString(valStr, valFont);
                g.DrawString(valStr, valFont, valBrush, pt.X - valSize.Width / 2, pt.Y - 15);
            }
        }
    }

    internal sealed class LookupItem
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
