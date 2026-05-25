using System.Globalization;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main.HieuSuat;

public sealed partial class frmHieuSuat
{
    private void WireEvents()
    {
        _dgv.DataBindingComplete += (_, _) => ApplyGridColumns();

        _txtSearch.TextChanged += async (_, _) =>
        {
            if (_isReloadingKy) return;
            await LoadGridAsync();
        };

        _cboKyDanhGia.SelectedIndexChanged += async (_, _) =>
        {
            if (_isReloadingKy) return;
            await LoadGridAsync();
        };

        _btnReset.Click += async (_, _) =>
        {
            _txtSearch.Text = string.Empty;
            _cboKyDanhGia.SelectedValue = 0;
            await LoadGridAsync();
        };

        _btnKyDanhGia.Click += async (_, _) =>
        {
            using var frm = new frmKyDanhGiaManager(_hieuSuatService);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                var selected = _cboKyDanhGia.SelectedValue is int val ? val : 0;
                await ReloadKyDanhGiaAsync(selected);
                await LoadGridAsync();
            }
        };

        _btnAdd.Click += async (_, _) =>
        {
            var nhanVien = await _nhanVienService.GetAllAsync();
            if (nhanVien.Count == 0 || _kyDanhGiaItems.Count == 0)
            {
                MessageBox.Show("Cần có dữ liệu Nhân viên và Kỳ đánh giá trước khi thêm hiệu suất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryShowHieuSuatEditor(nhanVien, _kyDanhGiaItems, null, out var dto))
                return;

            try
            {
                await _hieuSuatService.CreateOrUpdateAsync(dto);
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _btnEdit.Click += async (_, _) =>
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn bản ghi cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_dgv.SelectedRows[0].DataBoundItem is not HieuSuatDTO selected)
                return;

            var nhanVien = await _nhanVienService.GetAllAsync();
            if (!TryShowHieuSuatEditor(nhanVien, _kyDanhGiaItems, selected, out var dto))
                return;

            try
            {
                if (selected.MaHieuSuat <= 0)
                {
                    // Bản ghi placeholder (chưa lưu DB), tạo mới hoặc cập nhật
                    await _hieuSuatService.CreateOrUpdateAsync(dto);
                }
                else
                {
                    // Bản ghi đã tồn tại trong DB, cập nhật bình thường
                    await _hieuSuatService.UpdateAsync(selected.MaHieuSuat, dto);
                }
                await LoadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _btnDelete.Click += async (_, _) =>
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn bản ghi cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_dgv.SelectedRows[0].DataBoundItem is not HieuSuatDTO selected)
                return;

            if (selected.MaHieuSuat <= 0)
            {
                MessageBox.Show("Bản ghi này chưa được lưu, không cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Xóa bản ghi hiệu suất của [{selected.TenNhanVien}] ở kỳ [{selected.TenKyDanhGia}]?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

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

        _btnExport.Click += async (_, _) =>
        {
            await ExportAllToCsvAsync();
        };
    }

    private async Task ReloadKyDanhGiaAsync(int selectedKy = 0)
    {
        _isReloadingKy = true;
        try
        {
            _kyDanhGiaItems = await _hieuSuatService.GetKyDanhGiaAsync();
            var kyDataSource = new List<LookupItem> { new() { Value = 0, Text = "--- Tất cả ---" } };
            kyDataSource.AddRange(_kyDanhGiaItems.Select(k => new LookupItem
            {
                Value = k.MaKyDanhGia,
                Text = $"{k.TenKyDanhGia} ({k.NgayBatDau:dd/MM/yyyy} - {k.NgayKetThuc:dd/MM/yyyy})"
            }));

            _cboKyDanhGia.DataSource = null;
            _cboKyDanhGia.DataSource = kyDataSource;
            _cboKyDanhGia.DisplayMember = nameof(LookupItem.Text);
            _cboKyDanhGia.ValueMember = nameof(LookupItem.Value);
            _cboKyDanhGia.SelectedValue = kyDataSource.Any(x => x.Value == selectedKy) ? selectedKy : 0;
        }
        finally
        {
            _isReloadingKy = false;
        }
    }

    private async Task LoadGridAsync()
    {
        if (_isReloadingKy || _isLoadingGrid)
            return;

        _isLoadingGrid = true;
        try
        {
            var keyword = _txtSearch.Text.Trim();
            var selectedKy = _cboKyDanhGia.SelectedValue is int id ? id : 0;

            var data = selectedKy == 0
                ? await _hieuSuatService.GetAllAsync()
                : await _hieuSuatService.GetByKyDanhGiaAsync(selectedKy);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                data = data.Where(x => (x.TenNhanVien ?? string.Empty).ToLower().Contains(kw)).ToList();
            }

            _dgv.DataSource = null;
            _dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu hiệu suất: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoadingGrid = false;
        }
    }

    private void ApplyGridColumns()
    {
        foreach (DataGridViewColumn col in _dgv.Columns)
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
                case "DiemKPI": col.HeaderText = "Điểm KPI"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "TyLeHoanThanhDeadline": col.HeaderText = "% Deadline"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "SoGioLamViec": col.HeaderText = "Số giờ làm"; col.DefaultCellStyle.Format = "N2"; break;
                case "DiemChuyenCan": col.HeaderText = "Điểm CC"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "TyLeDiLam": col.HeaderText = "TỷLệĐiLàm"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "TyLeGioLam": col.HeaderText = "TỷLệGiờLàm"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "TyLeDungGio": col.HeaderText = "TỷLệĐúngGiờ"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "NgayDanhGia": col.HeaderText = "Ngày đánh giá"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                case "NhanXetCuaQuanLy": col.HeaderText = "Nhận xét"; col.MinimumWidth = 180; break;
                case "TrangThaiHoanThanh": col.HeaderText = "Tiến độ"; col.MinimumWidth = 150; break;
                case "DiemHieuSuatTong": col.HeaderText = "Tổng điểm"; col.DefaultCellStyle.Format = "0.00'%'"; break;
                case "HeSoLuongHieuSuat": col.HeaderText = "Bonus"; col.DefaultCellStyle.Format = "P0"; break;
                case "LuongDuKien": col.HeaderText = "Lương dự kiến"; col.DefaultCellStyle.Format = "N0"; col.MinimumWidth = 140; break;
            }
        }
    }

    private static bool TryShowHieuSuatEditor(
        IReadOnlyList<NhanVienDTO> nhanVienData,
        IReadOnlyList<KyDanhGiaDTO> kyDanhGiaData,
        HieuSuatDTO? current,
        out HieuSuatDTO result)
    {
        result = new HieuSuatDTO();
        HieuSuatDTO? pendingResult = null;

        using var dlg = new Form
        {
            Text = current == null ? "Thêm hiệu suất" : "Sửa hiệu suất",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(560, 395)
        };

        var lblNhanVien = new Label { Text = "Nhân viên", AutoSize = true, Location = new Point(20, 20) };
        var cboNhanVien = new ComboBox
        {
            Location = new Point(20, 40),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblKy = new Label { Text = "Kỳ đánh giá", AutoSize = true, Location = new Point(290, 20) };
        var cboKy = new ComboBox
        {
            Location = new Point(290, 40),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblKpi = new Label { Text = "Điểm KPI", AutoSize = true, Location = new Point(20, 80) };
        var txtKpi = new TextBox
        {
            Location = new Point(20, 100),
            Size = new Size(120, 25),
        };

        var lblDeadline = new Label { Text = "% Deadline", AutoSize = true, Location = new Point(160, 80) };
        var txtDeadline = new TextBox
        {
            Location = new Point(160, 100),
            Size = new Size(120, 25),
        };

        var lblChuyenCan = new Label { Text = "Điểm chuyên cần (auto)", AutoSize = true, Location = new Point(300, 80) };
        var txtChuyenCan = new TextBox
        {
            Location = new Point(300, 100),
            Size = new Size(150, 25),
            ReadOnly = true,
            BackColor = Color.FromArgb(245, 247, 250)
        };

        var lblNhanXet = new Label { Text = "Nhận xét của quản lý", AutoSize = true, Location = new Point(20, 140) };
        var txtNhanXet = new TextBox
        {
            Location = new Point(20, 160),
            Size = new Size(520, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        var btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(380, 350),
            Size = new Size(75, 30),
            DialogResult = DialogResult.None
        };
        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(465, 350),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

        var nhanVienItems = nhanVienData
            .Select(x => new LookupItem { Value = x.MaNhanVien, Text = $"{x.HoTen} ({x.MaNV})" })
            .ToList();
        cboNhanVien.DataSource = nhanVienItems;
        cboNhanVien.DisplayMember = nameof(LookupItem.Text);
        cboNhanVien.ValueMember = nameof(LookupItem.Value);

        var kyItems = kyDanhGiaData
            .Select(x => new LookupItem { Value = x.MaKyDanhGia, Text = x.TenKyDanhGia })
            .ToList();
        cboKy.DataSource = kyItems;
        cboKy.DisplayMember = nameof(LookupItem.Text);
        cboKy.ValueMember = nameof(LookupItem.Value);

        if (current != null)
        {
            cboNhanVien.SelectedValue = current.MaNhanVien;
            cboKy.SelectedValue = current.MaKyDanhGia;
            txtKpi.Text = current.DiemKPI?.ToString(CultureInfo.CurrentCulture);
            txtDeadline.Text = current.TyLeHoanThanhDeadline?.ToString(CultureInfo.CurrentCulture);
            txtChuyenCan.Text = current.DiemChuyenCan?.ToString(CultureInfo.CurrentCulture);
            txtNhanXet.Text = current.NhanXetCuaQuanLy;
        }

        btnSave.Click += (_, _) =>
        {
            if (cboNhanVien.SelectedValue is not int maNhanVien || maNhanVien <= 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên.");
                return;
            }

            if (cboKy.SelectedValue is not int maKy || maKy <= 0)
            {
                MessageBox.Show("Vui lòng chọn kỳ đánh giá.");
                return;
            }

            decimal? kpiVal = null, deadlineVal = null;

            if (decimal.TryParse(txtKpi.Text, out var k)) kpiVal = k;
            if (decimal.TryParse(txtDeadline.Text, out var d)) deadlineVal = d;

            pendingResult = new HieuSuatDTO
            {
                MaHieuSuat = current?.MaHieuSuat ?? 0,
                MaNhanVien = maNhanVien,
                MaKyDanhGia = maKy,
                DiemKPI = kpiVal,
                TyLeHoanThanhDeadline = deadlineVal,
                NhanXetCuaQuanLy = txtNhanXet.Text,
                NgayDanhGia = current?.NgayDanhGia == default ? DateTime.Today : current!.NgayDanhGia,
            };

            dlg.DialogResult = DialogResult.OK;
            dlg.Close();
        };

        dlg.Controls.Add(lblNhanVien);
        dlg.Controls.Add(cboNhanVien);
        dlg.Controls.Add(lblKy);
        dlg.Controls.Add(cboKy);
        dlg.Controls.Add(lblKpi);
        dlg.Controls.Add(txtKpi);
        dlg.Controls.Add(lblDeadline);
        dlg.Controls.Add(txtDeadline);
        dlg.Controls.Add(lblChuyenCan);
        dlg.Controls.Add(txtChuyenCan);
        dlg.Controls.Add(lblNhanXet);
        dlg.Controls.Add(txtNhanXet);
        dlg.Controls.Add(btnSave);
        dlg.Controls.Add(btnCancel);

        var ok = dlg.ShowDialog() == DialogResult.OK;
        if (ok && pendingResult != null)
        {
            result = pendingResult;
            return true;
        }

        return false;
    }

    private async Task ExportAllToCsvAsync()
    {
        try
        {
            var data = await _hieuSuatService.GetAllAsync();
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "HieuSuat_All.csv",
                Title = "Lưu file CSV"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var props = typeof(HieuSuatDTO).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetGetMethod() != null)
                .ToArray();

            using var writer = new StreamWriter(sfd.FileName, false, Encoding.UTF8);
            // header
            writer.WriteLine(string.Join(",", props.Select(p => EscapeCsv(p.Name))));

            foreach (var item in data)
            {
                var values = props.Select(p => EscapeCsv(p.GetValue(item)?.ToString() ?? string.Empty));
                writer.WriteLine(string.Join(",", values));
            }

            MessageBox.Show("Xuất CSV thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất CSV: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
