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

            var dgv = UIHelper.CreateStyledDataGridView("dgvThuongPhatLuong");
            dgv.ReadOnly = true;
            dgv.Location = new Point(20, 88);
            dgv.Size = new Size(Width - 40, Height - 108);
            dgv.AutoGenerateColumns = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ConfigureThuongPhatColumns(dgv, isAdmin);
            dgv.DataBindingComplete += (_, _) => ApplyThuongPhatColumnHeaders(dgv);

            async Task ReloadAsync()
            {
                try
                {
                    var thang = (int)numThang.Value;
                    var nam = (int)numNam.Value;
                    var list = await _bangLuongService.GetBangLuongAsync(thang, nam, isAdmin, _session.MaNhanVien);
                    dgv.DataSource = null;
                    if (dgv.Columns.Count == 0)
                        ConfigureThuongPhatColumns(dgv, isAdmin);
                    dgv.DataSource = list;
                    ApplyThuongPhatColumnHeaders(dgv);
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
            Controls.Add(dgv);

            await ReloadAsync();
        }

        private static void ConfigureThuongPhatColumns(DataGridView dgv, bool isAdmin)
        {
            dgv.Columns.Clear();

            dgv.Columns.Add(HiddenCol("MaBangLuong"));

            if (isAdmin)
                dgv.Columns.Add(TextCol("MaNhanVien", "Mã NV", 70, readOnly: true, fillWeight: 55));

            dgv.Columns.Add(TextCol("TenNhanVien", "Nhân viên", 160, readOnly: true, fillWeight: 120));
            dgv.Columns.Add(TextCol("Thang", "Tháng", 50, readOnly: true, fillWeight: 45));
            dgv.Columns.Add(TextCol("Nam", "Năm", 55, readOnly: true, fillWeight: 50));
            dgv.Columns.Add(TextCol("DiemHieuSuat", "Điểm KPI", 70, readOnly: true, fillWeight: 55));
            dgv.Columns.Add(MoneyCol("TongThuong", "Thưởng (VNĐ)", 90, readOnly: true));
            dgv.Columns.Add(MoneyCol("TongPhat", "Phạt (VNĐ)", 90, readOnly: true));
            dgv.Columns.Add(MoneyCol("ThucNhanThuongPhat", "Thực nhận (Thưởng - phạt)", 120, readOnly: true, fillWeight: 110));
        }

        private static void ApplyThuongPhatColumnHeaders(DataGridView dgv)
        {
            var visibleProps = new HashSet<string>(StringComparer.Ordinal)
            {
                "MaBangLuong", "MaNhanVien", "TenNhanVien", "Thang", "Nam", "DiemHieuSuat",
                "TongThuong", "TongPhat", "ThucNhanThuongPhat"
            };

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                var prop = col.DataPropertyName ?? col.Name;
                if (!visibleProps.Contains(prop))
                {
                    col.Visible = false;
                    continue;
                }

                if (prop == "DiemHieuSuat")
                    col.HeaderText = "Điểm KPI";
                if (prop == "ThucNhanThuongPhat")
                    col.HeaderText = "Thực nhận (Thưởng - phạt)";
            }
        }

        private static DataGridViewTextBoxColumn HiddenCol(string propertyName) =>
            new()
            {
                Name = propertyName,
                DataPropertyName = propertyName,
                Visible = false
            };

        private static DataGridViewTextBoxColumn TextCol(
            string propertyName,
            string headerText,
            int minimumWidth,
            bool readOnly,
            int fillWeight = 80) =>
            new()
            {
                Name = propertyName,
                DataPropertyName = propertyName,
                HeaderText = headerText,
                MinimumWidth = minimumWidth,
                FillWeight = fillWeight,
                ReadOnly = readOnly
            };

        private static DataGridViewTextBoxColumn MoneyCol(
            string propertyName,
            string headerText,
            int minimumWidth,
            bool readOnly,
            int fillWeight = 90) =>
            new()
            {
                Name = propertyName,
                DataPropertyName = propertyName,
                HeaderText = headerText,
                MinimumWidth = minimumWidth,
                FillWeight = fillWeight,
                ReadOnly = readOnly,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" }
            };
    }
}
