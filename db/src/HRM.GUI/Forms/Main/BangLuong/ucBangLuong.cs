using System.Globalization;
using System.Reflection;
using System.Text;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.BangLuong
{
    public partial class ucBangLuong : UserControl
    {
        private readonly IBangLuongService _bangLuongService;
        private readonly UserSessionDTO? _session;

        /// <summary>Chỉ dành cho WinForms Designer (yêu cầu constructor không tham số).</summary>
        public ucBangLuong() : this(null) { }

        public ucBangLuong(UserSessionDTO? session)
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
                Text = isAdmin ? "Bảng lương — quản trị" : "Bảng lương của tôi",
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
                Location = new Point(isAdmin ? 470 : 300, 45),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnExportCsv = new Button
            {
                Text = "⬇️ Xuất CSV",
                Location = new Point(isAdmin ? 580 : 410, 45),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportCsv.FlatAppearance.BorderSize = 0;

            List<BangLuongDTO>? currentList = null;

            var dgv = UIHelper.CreateStyledDataGridView("dgvBangLuong");
            if (isAdmin)
            {
                dgv.ReadOnly = false;
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv.EditMode = DataGridViewEditMode.EditOnEnter;
            }
            dgv.Location = new Point(20, 88);
            dgv.Size = new Size(Width - 40, Height - 108);
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
                        case "LuongCoBan": col.HeaderText = "Lương cơ bản"; col.DefaultCellStyle.Format = "N0"; break;
                        case "DiemHieuSuat":
                        case "HeSoLuongHieuSuat":
                            col.Visible = false;
                            break;
                        case "LuongCoBanSauHieuSuat":
                            col.Visible = false;
                            break;
                        case "TongPhuCap": col.HeaderText = "Phụ cấp"; col.DefaultCellStyle.Format = "N0"; break;
                        case "SoNgayLamViec": col.HeaderText = "Ngày công"; break;
                        case "SoGioLamThem": col.HeaderText = "Giờm làm thêm"; col.DefaultCellStyle.Format = "N2"; break;
                        case "TienLamThem": col.HeaderText = "Tiền làm thêm"; col.DefaultCellStyle.Format = "N0"; break;
                        case "TongThuong": col.HeaderText = "Thưởng "; col.DefaultCellStyle.Format = "N0"; break;
                        case "TongPhat": col.HeaderText = "Phạt "; col.DefaultCellStyle.Format = "N0"; break;
                        case "BHXH": col.HeaderText = "BHXH"; col.DefaultCellStyle.Format = "N0"; break;
                        case "BHYT": col.HeaderText = "BHYT"; col.DefaultCellStyle.Format = "N0"; break;
                        case "BHTN": col.HeaderText = "BHTN"; col.DefaultCellStyle.Format = "N0"; break;
                        case "ThueTNCN": col.HeaderText = "Thuế TNCN"; col.DefaultCellStyle.Format = "N0"; break;
                        case "TongThuNhap": col.HeaderText = "Tổng thu nhập"; col.DefaultCellStyle.Format = "N0"; break;
                        case "TongKhauTru": col.HeaderText = "Tổng khấu trừ"; col.DefaultCellStyle.Format = "N0"; break;
                        case "LuongThucNhan":
                            col.HeaderText = "Thực nhận";
                            col.DefaultCellStyle.Format = "N0";
                            col.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
                            break;
                        case "NgayTinhLuong": col.HeaderText = "Ngày tính"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                        case "TrangThai":
                        case "ThucNhanThuongPhat":
                            col.Visible = false;
                            break;
                        default:
                            col.Visible = false;
                            break;
                    }

                    if (isAdmin && col.DataPropertyName is "BHXH" or "BHYT" or "BHTN" or "ThueTNCN")
                        col.ReadOnly = false;
                }
            };

            var savingGrid = false;

            dgv.CellEndEdit += async (_, e) =>
            {
                if (!isAdmin || savingGrid || e.RowIndex < 0) return;
                var prop = dgv.Columns[e.ColumnIndex].DataPropertyName;
                if (prop is not ("BHXH" or "BHYT" or "BHTN" or "ThueTNCN")) return;
                if (dgv.Rows[e.RowIndex].DataBoundItem is not BangLuongDTO dto) return;

                decimal CellValue(string name)
                {
                    foreach (DataGridViewColumn c in dgv.Columns)
                    {
                        if (c.DataPropertyName != name) continue;
                        return BangLuongFormat.ParseMoney(dgv.Rows[e.RowIndex].Cells[c.Index].Value);
                    }
                    return 0m;
                }

                savingGrid = true;
                try
                {
                    await _bangLuongService.CapNhatKhoanKhauTruVaTinhLaiAsync(
                        dto.MaBangLuong,
                        CellValue("BHXH"),
                        CellValue("BHYT"),
                        CellValue("BHTN"),
                        CellValue("ThueTNCN"));
                    await ReloadAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không lưu được thay đổi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await ReloadAsync();
                }
                finally
                {
                    savingGrid = false;
                }
            };

            async Task ReloadAsync()
            {
                try
                {
                    var thang = (int)numThang.Value;
                    var nam = (int)numNam.Value;
                    var list = await _bangLuongService.GetBangLuongAsync(thang, nam, isAdmin, _session.MaNhanVien);
                    currentList = list;
                    dgv.DataSource = null;
                    dgv.DataSource = list;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải bảng lương: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            btnReload.Click += async (_, _) => await ReloadAsync();
            btnExportCsv.Click += (_, _) =>
            {
                var thang = (int)numThang.Value;
                var nam = (int)numNam.Value;
                BangLuongCsvExporter.Export(currentList, isAdmin, thang, nam);
            };
            void OnThangNamChanged(object? s, EventArgs e) => _ = ReloadAsync();
            numThang.ValueChanged += OnThangNamChanged;
            numNam.ValueChanged += OnThangNamChanged;

            btnTinh.Click += async (_, _) =>
            {
                if (!isAdmin) return;
                var thang = (int)numThang.Value;
                var nam = (int)numNam.Value;
                if (MessageBox.Show(
                        $"Tính lại lương cho mọi nhân viên đang làm việc — tháng {thang}/{nam}?\n"
                        + "Lương theo hệ số hiệu suất; thưởng/phạt tự động (điểm ≥100 thưởng, <100 phạt).\n"
                        + "BHXH/BHYT/BHTN/thuế đã nhập sẽ được giữ.",
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

            Controls.Add(lblTitle);
            Controls.Add(lblThang);
            Controls.Add(numThang);
            Controls.Add(lblNam);
            Controls.Add(numNam);
            Controls.Add(btnTinh);
            Controls.Add(btnReload);
            Controls.Add(btnExportCsv);
            Controls.Add(dgv);

            await ReloadAsync();
        }
    }

    /// <summary>Hàm parse số tiền cho lưới Bảng lương — tách ra để ucThuongPhat dùng chung.</summary>
    internal static class BangLuongFormat
    {
        public static decimal ParseMoney(object? v)
        {
            if (v == null || Convert.IsDBNull(v)) return 0m;
            if (v is decimal d) return d;
            var s = Convert.ToString(v, CultureInfo.CurrentCulture)?.Replace("\u00A0", "").Trim();
            if (string.IsNullOrEmpty(s)) return 0m;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var x)) return x;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out x)) return x;
            return 0m;
        }
    }

    internal static class BangLuongCsvExporter
    {
        private static readonly (string Property, string Header)[] Columns =
        {
            ("MaNhanVien", "Mã NV"),
            ("TenNhanVien", "Nhân viên"),
            ("Thang", "Tháng"),
            ("Nam", "Năm"),
            ("LuongCoBan", "Lương cơ bản"),
            ("TongPhuCap", "Phụ cấp"),
            ("SoNgayLamViec", "Ngày công"),
            ("SoGioLamThem", "Giờ làm thêm"),
            ("TienLamThem", "Tiền làm thêm"),
            ("TongThuong", "Thưởng"),
            ("TongPhat", "Phạt"),
            ("BHXH", "BHXH"),
            ("BHYT", "BHYT"),
            ("BHTN", "BHTN"),
            ("ThueTNCN", "Thuế TNCN"),
            ("TongThuNhap", "Tổng thu nhập"),
            ("TongKhauTru", "Tổng khấu trừ"),
            ("LuongThucNhan", "Thực nhận"),
            ("NgayTinhLuong", "Ngày tính")
        };

        public static void Export(IReadOnlyList<BangLuongDTO>? data, bool includeMaNv, int thang, int nam)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để xuất. Hãy tải bảng lương trước.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV (*.csv)|*.csv",
                    FileName = $"BangLuong_{thang:D2}_{nam}.csv",
                    Title = "Lưu bảng lương CSV"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var cols = Columns.Where(c => includeMaNv || c.Property != "MaNhanVien").ToArray();
                var props = cols.ToDictionary(
                    c => c.Property,
                    c => typeof(BangLuongDTO).GetProperty(c.Property, BindingFlags.Public | BindingFlags.Instance)
                        ?? throw new InvalidOperationException($"Thuộc tính {c.Property} không tồn tại."));

                using var writer = new StreamWriter(sfd.FileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                writer.WriteLine(string.Join(",", cols.Select(c => EscapeCsv(c.Header))));

                foreach (var row in data)
                {
                    var cells = cols.Select(c => EscapeCsv(FormatValue(props[c.Property].GetValue(row))));
                    writer.WriteLine(string.Join(",", cells));
                }

                MessageBox.Show($"Đã xuất {data.Count} dòng.\n{sfd.FileName}", "Xuất CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất CSV: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FormatValue(object? value)
        {
            if (value == null) return string.Empty;
            return value switch
            {
                DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                decimal d => d.ToString(CultureInfo.InvariantCulture),
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string EscapeCsv(string s)
        {
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            {
                s = s.Replace("\"", "\"\"");
                return '"' + s + '"';
            }
            return s;
        }
    }
}
