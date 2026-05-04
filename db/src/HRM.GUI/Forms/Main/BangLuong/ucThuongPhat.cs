using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.BangLuong
{
    /// <summary>Màn hình riêng: chỉ nhập thưởng/phạt (cùng dữ liệu bảng lương đã tính).</summary>
    public partial class ucThuongPhat : UserControl
    {
        private readonly IBangLuongService _bangLuongService;
        private readonly UserSessionDTO? _session;

        public ucThuongPhat() : this(null) { }

        public ucThuongPhat(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _bangLuongService = null!;
                return;
            }
            _bangLuongService = Program.ServiceProvider.GetRequiredService<IBangLuongService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
        {
            if (_session == null) return;

            var isAdmin = UIHelper.IsAdmin(_session);
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
                Minimum = 1, Maximum = 12, Value = now.Month,
                Location = new Point(75, 48), Width = 55
            };
            var lblNam = new Label { Text = "Năm:", Location = new Point(150, 52), AutoSize = true };
            var numNam = new NumericUpDown
            {
                Minimum = 2000, Maximum = 2100, Value = now.Year,
                Location = new Point(195, 48), Width = 75
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
                Text = isAdmin
                    ? "Sửa trực tiếp cột Thưởng / Phạt, nhấn Enter — hệ thống tính lại thực nhận."
                    : string.Empty,
                Visible = isAdmin,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(30, 100, 160),
                AutoSize = true,
                Location = new Point(20, 82)
            };

            var dgv = UIHelper.CreateStyledDataGridView("dgvThuongPhatLuong");
            if (isAdmin)
            {
                dgv.ReadOnly = false;
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv.EditMode = DataGridViewEditMode.EditOnEnter;
            }
            dgv.Location = new Point(20, isAdmin ? 108 : 88);
            dgv.Size = new Size(Width - 40, Height - (isAdmin ? 128 : 108));
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
                        return BangLuongFormat.ParseMoney(dgv.Rows[e.RowIndex].Cells[c.Index].Value);
                    }
                    return 0m;
                }

                var thuong = GetCell("TongThuong");
                var phat = GetCell("TongPhat");

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
                    MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            btnReload.Click += async (_, _) => await ReloadAsync();
            void OnThangNamChanged(object? s, EventArgs e) => _ = ReloadAsync();
            numThang.ValueChanged += OnThangNamChanged;
            numNam.ValueChanged += OnThangNamChanged;

            Controls.Add(lblTitle);
            Controls.Add(lblThang);
            Controls.Add(numThang);
            Controls.Add(lblNam);
            Controls.Add(numNam);
            Controls.Add(btnReload);
            if (isAdmin) Controls.Add(lblEditHint);
            Controls.Add(dgv);

            await ReloadAsync();
        }
    }
}
