using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.ChamCong
{
    public partial class ucChamCong : UserControl
    {
        private readonly IChamCongService _chamCongService;
        private readonly UserSessionDTO? _session;

        public ucChamCong() : this(null) { }

        public ucChamCong(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _chamCongService = null!;
                return;
            }
            _chamCongService = Program.ServiceProvider.GetRequiredService<IChamCongService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
        {
            if (_session == null) return;

            var isAdmin = UIHelper.IsAdmin(_session);
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
                Width = Math.Max(480, Width - 40),
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
                const int pad = 18;
                const int btnY = 44;
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
                Size = new Size(Width - 40, Math.Max(200, ClientSize.Height - histTop - 16)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = cardBorder,
                Padding = new Padding(1)
            };

            var pnlHistInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

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

            var dgv = UIHelper.CreateChamCongHistoryGrid("dgvChamCong");

            pnlHistInner.Controls.Add(dgv);
            pnlHistInner.Controls.Add(pnlFilter);
            pnlHistInner.Controls.Add(pnlHistHead);
            pnlHistOuter.Controls.Add(pnlHistInner);

            UIHelper.WireChamCongHistoryTimeCellFormatting(dgv);
            dgv.DataBindingComplete += (_, _) => UIHelper.ApplyChamCongHistoryColumns(dgv, isAdmin);

            async Task LoadGridAsync()
            {
                try
                {
                    List<ChamCongDTO> data = isAdmin
                        ? await _chamCongService.GetAllInPeriodAsync(dtpTu.Value, dtpDen.Value)
                        : await _chamCongService.GetHistoryAsync(_session.MaNhanVien, dtpTu.Value, dtpDen.Value);
                    dgv.DataSource = null;
                    dgv.DataSource = data;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            async Task TryOpenChamCongEditAsync(ChamCongDTO? rowDto = null)
            {
                if (!isAdmin) return;

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
                if (dto == null) return;

                using var dlg = new frmSuaChamCong(_chamCongService, dto);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    await LoadGridAsync();
            }

            if (isAdmin)
            {
                dgv.CellDoubleClick += async (_, e) =>
                {
                    if (e.RowIndex < 0) return;
                    if (dgv.Rows[e.RowIndex].DataBoundItem is not ChamCongDTO dto) return;
                    await TryOpenChamCongEditAsync(dto);
                };
                btnSuaChamCong!.Click += async (_, _) => await TryOpenChamCongEditAsync();
            }

            async Task RefreshTodayAsync()
            {
                if (isAdmin) return;
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

            Controls.Add(lblModuleTitle);
            Controls.Add(pnlQuickOuter);
            Controls.Add(pnlHistOuter);

            await RefreshTodayAsync();
            await LoadGridAsync();
        }
    }
}
