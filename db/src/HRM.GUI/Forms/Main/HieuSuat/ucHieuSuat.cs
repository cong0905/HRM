using System.Globalization;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.HieuSuat
{
    public partial class ucHieuSuat : UserControl
    {
        private readonly IHieuSuatService _hieuSuatService;
        private readonly INhanVienService _nhanVienService;
        private readonly UserSessionDTO? _session;

        public ucHieuSuat() : this(null) { }

        public ucHieuSuat(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _hieuSuatService = null!;
                _nhanVienService = null!;
                return;
            }
            _hieuSuatService = Program.ServiceProvider.GetRequiredService<IHieuSuatService>();
            _nhanVienService = Program.ServiceProvider.GetRequiredService<INhanVienService>();
            Load += async (_, _) => await LoadView();
        }

        private async Task LoadView()
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
                Location = new Point(710, 59),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;

            var btnKyDanhGia = new Button
            {
                Text = "🗂️ Kỳ đánh giá",
                Location = new Point(600, 59),
                Size = new Size(105, 28),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnKyDanhGia.FlatAppearance.BorderSize = 0;

            var btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(790, 59),
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
                Location = new Point(900, 59),
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
                Location = new Point(980, 59),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;

            var dgv = UIHelper.CreateStyledDataGridView("dgvHieuSuat");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(Width - 40, Height - 120);

            List<KyDanhGiaDTO> kyDanhGiaItems = new();
            var isReloadingKy = false;
            var isLoadingGrid = false;

            async Task ReloadKyDanhGiaAsync(int selectedKy = 0)
            {
                isReloadingKy = true;
                kyDanhGiaItems = await _hieuSuatService.GetKyDanhGiaAsync();
                var kyDataSource = new List<LookupItem> { new() { Value = 0, Text = "--- Tất cả ---" } };
                kyDataSource.AddRange(kyDanhGiaItems.Select(k => new LookupItem
                {
                    Value = k.MaKyDanhGia,
                    Text = $"{k.TenKyDanhGia} ({k.NgayBatDau:dd/MM/yyyy} - {k.NgayKetThuc:dd/MM/yyyy})"
                }));

                cboKyDanhGia.DataSource = null;
                cboKyDanhGia.DataSource = kyDataSource;
                cboKyDanhGia.DisplayMember = nameof(LookupItem.Text);
                cboKyDanhGia.ValueMember = nameof(LookupItem.Value);

                cboKyDanhGia.SelectedValue = kyDataSource.Any(x => x.Value == selectedKy) ? selectedKy : 0;
                isReloadingKy = false;
            }

            await ReloadKyDanhGiaAsync();

            async Task LoadGridAsync()
            {
                if (isReloadingKy || isLoadingGrid) return;
                isLoadingGrid = true;
                try
                {
                    var keyword = txtSearch.Text.Trim();
                    var selectedKy = GetComboSelectedIntId(cboKyDanhGia);

                    var data = selectedKy == 0
                        ? await _hieuSuatService.GetAllAsync()
                        : await _hieuSuatService.GetByKyDanhGiaAsync(selectedKy);

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        var kw = keyword.ToLower();
                        data = data.Where(x => (x.TenNhanVien ?? string.Empty).ToLower().Contains(kw)).ToList();
                    }

                    dgv.DataSource = null;
                    dgv.DataSource = data;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải dữ liệu hiệu suất: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    isLoadingGrid = false;
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
                            col.Visible = false;
                            break;
                        case "TenNhanVien": col.HeaderText = "Nhân viên"; col.MinimumWidth = 150; break;
                        case "TenKyDanhGia": col.HeaderText = "Kỳ đánh giá"; col.MinimumWidth = 170; break;
                        case "DiemKPI": col.HeaderText = "Điểm KPI"; col.DefaultCellStyle.Format = "N2"; break;
                        case "TyLeHoanThanhDeadline": col.HeaderText = "% Deadline"; col.DefaultCellStyle.Format = "N2"; break;
                        case "SoGioLamViec": col.HeaderText = "Giờ làm việc"; col.DefaultCellStyle.Format = "N2"; break;
                        case "NgayDanhGia": col.HeaderText = "Ngày đánh giá"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                        case "KetQuaCongViec": col.HeaderText = "Kết quả công việc"; col.MinimumWidth = 180; break;
                        case "TrangThaiHoanThanh": col.HeaderText = "Tiến độ"; col.MinimumWidth = 150; break;
                        case "HeSoLuongHieuSuat": col.HeaderText = "HS lương"; col.DefaultCellStyle.Format = "P0"; break;
                        case "LuongDuKien": col.HeaderText = "Lương dự kiến"; col.DefaultCellStyle.Format = "N0"; col.MinimumWidth = 140; break;
                    }
                }
            };

            txtSearch.TextChanged += async (_, _) =>
            {
                if (isReloadingKy) return;
                await LoadGridAsync();
            };

            cboKyDanhGia.SelectedIndexChanged += async (_, _) =>
            {
                if (isReloadingKy) return;
                await LoadGridAsync();
            };

            btnReset.Click += async (_, _) =>
            {
                txtSearch.Text = string.Empty;
                cboKyDanhGia.SelectedValue = 0;
                await LoadGridAsync();
            };

            btnKyDanhGia.Click += async (_, _) =>
            {
                using var dlg = HieuSuatDialogs.BuildKyDanhGiaManagerDialog(_hieuSuatService);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    var selected = GetComboSelectedIntId(cboKyDanhGia);
                    await ReloadKyDanhGiaAsync(selected);
                    await LoadGridAsync();
                }
            };

            btnAdd.Click += async (_, _) =>
            {
                var nhanVien = await _nhanVienService.GetAllAsync();
                if (nhanVien.Count == 0 || kyDanhGiaItems.Count == 0)
                {
                    MessageBox.Show("Cần có dữ liệu Nhân viên và Kỳ đánh giá trước khi thêm hiệu suất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!HieuSuatDialogs.TryShowHieuSuatEditor(nhanVien, kyDanhGiaItems, null, out var dto)) return;
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
                if (dgv.SelectedRows[0].DataBoundItem is not HieuSuatDTO selected) return;

                var nhanVien = await _nhanVienService.GetAllAsync();
                if (!HieuSuatDialogs.TryShowHieuSuatEditor(nhanVien, kyDanhGiaItems, selected, out var dto)) return;

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
                if (dgv.SelectedRows[0].DataBoundItem is not HieuSuatDTO selected) return;

                var confirm = MessageBox.Show(
                    $"Xóa bản ghi hiệu suất của [{selected.TenNhanVien}] ở kỳ [{selected.TenKyDanhGia}]?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

            Controls.Add(lblTitle);
            Controls.Add(txtSearch);
            Controls.Add(lblKy);
            Controls.Add(cboKyDanhGia);
            Controls.Add(btnKyDanhGia);
            Controls.Add(btnReset);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(dgv);

            await LoadGridAsync();
        }

        /// <summary>WinForms <see cref="ComboBox.SelectedValue"/> khi bind có thể là <see cref="int"/>, <see cref="long"/>, v.v. — dùng chung để lọc kỳ.</summary>
        private static int GetComboSelectedIntId(ComboBox cbo)
        {
            try
            {
                var v = cbo.SelectedValue;
                if (v is null or DBNull) return 0;
                return Convert.ToInt32(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        internal sealed class LookupItem
        {
            public int Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }
    }
}
