using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.NghiPhep
{
    public partial class ucNghiPhep : UserControl
    {
        private readonly IDonNghiPhepService _donNghiPhepService;
        private readonly UserSessionDTO? _session;

        public ucNghiPhep() : this(null) { }

        public ucNghiPhep(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _donNghiPhepService = null!;
                return;
            }
            _donNghiPhepService = Program.ServiceProvider.GetRequiredService<IDonNghiPhepService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
        {
            if (_session == null) return;

            if (UIHelper.IsAdmin(_session))
                await LoadAdminViewAsync();
            else
                await LoadEmployeeViewAsync();
        }

        private async Task LoadAdminViewAsync()
        {
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
                Width = Math.Max(400, Width - 40),
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
                BackColor = BackColor
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BackColor
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
                BackColor = BackColor,
                Padding = new Padding(0, 2, 0, 0)
            };

            var dgvAll = UIHelper.CreateChamCongHistoryGrid("dgvDonNghiPhepAdmin");
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
                Size = new Size(Width - 40, Math.Max(240, ClientSize.Height - adminGridTop - adminGridBottomPad)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = cardBorder,
                Padding = new Padding(1)
            };
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            dgvAll.DataBindingComplete += (_, _) => UIHelper.ApplyDonNghiPhepGridColumns(dgvAll);

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
                    await _donNghiPhepService.ApproveAsync(dto.MaDonPhep, _session!.MaNhanVien);
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
                var ld = UIHelper.PromptSingleLine(FindForm()!, "Từ chối đơn", "Lý do từ chối:");
                if (string.IsNullOrWhiteSpace(ld)) return;
                try
                {
                    await _donNghiPhepService.RejectAsync(dto.MaDonPhep, _session!.MaNhanVien, ld);
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

            Controls.Add(pnlHeader);
            Controls.Add(flowFilter);
            Controls.Add(pnlGridOuter);

            await ReloadAsync();
        }

        private async Task LoadEmployeeViewAsync()
        {
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
                Size = new Size(Width - 40, Math.Max(220, ClientSize.Height - myGridTop - 20)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = cardBorder,
                Padding = new Padding(1)
            };
            var pnlMineInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var dgvMine = UIHelper.CreateChamCongHistoryGrid("dgvDonCuaToi");
            dgvMine.Dock = DockStyle.Fill;

            dgvMine.DataBindingComplete += (_, _) => UIHelper.ApplyDonNghiPhepGridColumns(dgvMine);

            pnlMineInner.Controls.Add(dgvMine);
            pnlMineOuter.Controls.Add(pnlMineInner);

            async Task ReloadMineAsync()
            {
                try
                {
                    var list = await _donNghiPhepService.GetByNhanVienAsync(_session!.MaNhanVien);
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
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                    if (cbo.SelectedItem is not LoaiNghiPhepDTO sel) return;
                    await _donNghiPhepService.CreateAsync(_session!.MaNhanVien, sel.MaLoaiPhep, dtpTu.Value, dtpDen.Value, txtLyDo.Text);
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
                if (dgvMine.SelectedRows[0].DataBoundItem is not DonNghiPhepDTO dto) return;
                if (dto.TrangThai != DonNghiPhepTrangThai.ChoDuyet)
                {
                    MessageBox.Show("Chỉ hủy được đơn đang chờ duyệt.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var lyDo = UIHelper.PromptSingleLine(FindForm()!, "Hủy đơn nghỉ", "Lý do hủy (tùy chọn):");
                if (lyDo == null) return;
                try
                {
                    await _donNghiPhepService.CancelAsync(dto.MaDonPhep, _session!.MaNhanVien, lyDo);
                    MessageBox.Show("Đã hủy đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await ReloadMineAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(btnTaoDon);
            Controls.Add(btnHuyDon);
            Controls.Add(pnlMineOuter);

            await ReloadMineAsync();
        }
    }
}
