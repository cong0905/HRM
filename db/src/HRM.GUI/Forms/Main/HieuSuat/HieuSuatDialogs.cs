using System.Globalization;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main.HieuSuat
{
    /// <summary>
    /// Tách các hộp thoại Thêm/Sửa hiệu suất và Quản lý kỳ đánh giá ra khỏi UserControl
    /// để file ucHieuSuat.cs tập trung vào logic hiển thị danh sách.
    /// </summary>
    internal static class HieuSuatDialogs
    {
        public static bool TryShowHieuSuatEditor(
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
            var cboNhanVien = new ComboBox { Location = new Point(20, 40), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblKy = new Label { Text = "Kỳ đánh giá", AutoSize = true, Location = new Point(290, 20) };
            var cboKy = new ComboBox { Location = new Point(290, 40), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblKpi = new Label { Text = "Điểm KPI", AutoSize = true, Location = new Point(20, 80) };
            var txtKpi = new TextBox { Location = new Point(20, 100), Size = new Size(120, 25) };

            var lblDeadline = new Label { Text = "% Deadline", AutoSize = true, Location = new Point(160, 80) };
            var txtDeadline = new TextBox { Location = new Point(160, 100), Size = new Size(120, 25) };

            var lblSoGio = new Label { Text = "Số giờ làm", AutoSize = true, Location = new Point(300, 80) };
            var txtSoGio = new TextBox { Location = new Point(300, 100), Size = new Size(120, 25) };

            var lblKetQua = new Label { Text = "Kết quả công việc", AutoSize = true, Location = new Point(20, 140) };
            var txtKetQua = new TextBox
            {
                Location = new Point(20, 160),
                Size = new Size(520, 130),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnSave = new Button { Text = "Lưu", Location = new Point(380, 350), Size = new Size(75, 30), DialogResult = DialogResult.None };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(465, 350), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

            var nhanVienItems = nhanVienData
                .Select(x => new ucHieuSuat.LookupItem { Value = x.MaNhanVien, Text = $"{x.HoTen} ({x.MaNV})" })
                .ToList();
            cboNhanVien.DataSource = nhanVienItems;
            cboNhanVien.DisplayMember = nameof(ucHieuSuat.LookupItem.Text);
            cboNhanVien.ValueMember = nameof(ucHieuSuat.LookupItem.Value);

            var kyItems = kyDanhGiaData
                .Select(x => new ucHieuSuat.LookupItem { Value = x.MaKyDanhGia, Text = x.TenKyDanhGia })
                .ToList();
            cboKy.DataSource = kyItems;
            cboKy.DisplayMember = nameof(ucHieuSuat.LookupItem.Text);
            cboKy.ValueMember = nameof(ucHieuSuat.LookupItem.Value);

            if (current != null)
            {
                cboNhanVien.SelectedValue = current.MaNhanVien;
                cboKy.SelectedValue = current.MaKyDanhGia;
                txtKpi.Text = current.DiemKPI?.ToString(CultureInfo.CurrentCulture);
                txtDeadline.Text = current.TyLeHoanThanhDeadline?.ToString(CultureInfo.CurrentCulture);
                txtSoGio.Text = current.SoGioLamViec?.ToString(CultureInfo.CurrentCulture);
                txtKetQua.Text = current.KetQuaCongViec;
            }

            btnSave.Click += (_, _) =>
            {
                var maNhanVien = GetComboSelectedIntId(cboNhanVien);
                if (maNhanVien <= 0)
                {
                    MessageBox.Show("Vui lòng chọn nhân viên.");
                    return;
                }
                var maKy = GetComboSelectedIntId(cboKy);
                if (maKy <= 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá.");
                    return;
                }
                if (!TryParseNullableDecimal(txtKpi.Text, out var diemKpi)
                    || !TryParseNullableDecimal(txtDeadline.Text, out var tyLe)
                    || !TryParseNullableDecimal(txtSoGio.Text, out var soGio))
                {
                    MessageBox.Show("Các trường số (KPI, % Deadline, Số giờ làm) không hợp lệ.");
                    return;
                }

                pendingResult = new HieuSuatDTO
                {
                    MaHieuSuat = current?.MaHieuSuat ?? 0,
                    MaNhanVien = maNhanVien,
                    MaKyDanhGia = maKy,
                    DiemKPI = diemKpi,
                    TyLeHoanThanhDeadline = tyLe,
                    SoGioLamViec = soGio,
                    NgayDanhGia = current?.NgayDanhGia == default ? DateTime.Today : current!.NgayDanhGia,
                    KetQuaCongViec = string.IsNullOrWhiteSpace(txtKetQua.Text) ? null : txtKetQua.Text.Trim(),
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
            dlg.Controls.Add(lblSoGio);
            dlg.Controls.Add(txtSoGio);
            dlg.Controls.Add(lblKetQua);
            dlg.Controls.Add(txtKetQua);
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

        public static bool TryShowKyDanhGiaEditor(KyDanhGiaDTO? current, out KyDanhGiaDTO result)
        {
            result = new KyDanhGiaDTO();
            KyDanhGiaDTO? pending = null;

            using var dlg = new Form
            {
                Text = current == null ? "Thêm kỳ đánh giá" : "Sửa kỳ đánh giá",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(420, 220)
            };

            var lblTen = new Label { Text = "Tên kỳ đánh giá", AutoSize = true, Location = new Point(20, 20) };
            var txtTen = new TextBox { Location = new Point(20, 40), Size = new Size(380, 25) };

            var lblTu = new Label { Text = "Ngày bắt đầu", AutoSize = true, Location = new Point(20, 80) };
            var dtpTu = new DateTimePicker { Location = new Point(20, 100), Size = new Size(180, 25), Format = DateTimePickerFormat.Short };

            var lblDen = new Label { Text = "Ngày kết thúc", AutoSize = true, Location = new Point(220, 80) };
            var dtpDen = new DateTimePicker { Location = new Point(220, 100), Size = new Size(180, 25), Format = DateTimePickerFormat.Short };

            var btnSave = new Button { Text = "Lưu", Location = new Point(245, 160), Size = new Size(75, 30), DialogResult = DialogResult.None };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(325, 160), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

            if (current != null)
            {
                txtTen.Text = current.TenKyDanhGia;
                dtpTu.Value = current.NgayBatDau == default ? DateTime.Today : current.NgayBatDau;
                dtpDen.Value = current.NgayKetThuc == default ? DateTime.Today : current.NgayKetThuc;
            }
            else
            {
                dtpTu.Value = DateTime.Today;
                dtpDen.Value = DateTime.Today;
            }

            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(txtTen.Text))
                {
                    MessageBox.Show("Tên kỳ đánh giá không được để trống.");
                    return;
                }
                if (dtpTu.Value.Date > dtpDen.Value.Date)
                {
                    MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                    return;
                }

                pending = new KyDanhGiaDTO
                {
                    MaKyDanhGia = current?.MaKyDanhGia ?? 0,
                    TenKyDanhGia = txtTen.Text.Trim(),
                    NgayBatDau = dtpTu.Value.Date,
                    NgayKetThuc = dtpDen.Value.Date
                };

                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(lblTen);
            dlg.Controls.Add(txtTen);
            dlg.Controls.Add(lblTu);
            dlg.Controls.Add(dtpTu);
            dlg.Controls.Add(lblDen);
            dlg.Controls.Add(dtpDen);
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);

            var ok = dlg.ShowDialog() == DialogResult.OK;
            if (ok && pending != null)
            {
                result = pending;
                return true;
            }
            return false;
        }

        /// <summary>Hộp thoại Quản lý kỳ đánh giá (CRUD trên 1 lưới).</summary>
        public static Form BuildKyDanhGiaManagerDialog(IHieuSuatService hieuSuatService)
        {
            var dlg = new Form
            {
                Text = "Quản lý kỳ đánh giá",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(760, 430)
            };

            var dgvKy = new DataGridView
            {
                Location = new Point(15, 15),
                Size = new Size(730, 340),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            var btnThemKy = new Button { Text = "➕ Thêm", Location = new Point(15, 370), Size = new Size(90, 32) };
            var btnSuaKy = new Button { Text = "✏️ Sửa", Location = new Point(110, 370), Size = new Size(90, 32) };
            var btnXoaKy = new Button { Text = "🗑️ Xóa", Location = new Point(205, 370), Size = new Size(90, 32) };
            var btnDong = new Button { Text = "Đóng", Location = new Point(655, 370), Size = new Size(90, 32), DialogResult = DialogResult.OK };

            async Task LoadKyGridAsync()
            {
                var periods = await hieuSuatService.GetKyDanhGiaAsync();
                dgvKy.DataSource = periods;

                foreach (DataGridViewColumn col in dgvKy.Columns)
                {
                    switch (col.DataPropertyName)
                    {
                        case "MaKyDanhGia": col.HeaderText = "Mã kỳ"; col.FillWeight = 20; break;
                        case "TenKyDanhGia": col.HeaderText = "Tên kỳ"; col.FillWeight = 45; break;
                        case "NgayBatDau": col.HeaderText = "Từ ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                        case "NgayKetThuc": col.HeaderText = "Đến ngày"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; col.FillWeight = 20; break;
                    }
                }
            }

            btnThemKy.Click += async (_, _) =>
            {
                if (!TryShowKyDanhGiaEditor(null, out var dto)) return;
                try
                {
                    await hieuSuatService.CreateKyDanhGiaAsync(dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            btnSuaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (dgvKy.SelectedRows[0].DataBoundItem is not KyDanhGiaDTO currentKy) return;
                if (!TryShowKyDanhGiaEditor(currentKy, out var dto)) return;
                try
                {
                    await hieuSuatService.UpdateKyDanhGiaAsync(currentKy.MaKyDanhGia, dto);
                    await LoadKyGridAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            btnXoaKy.Click += async (_, _) =>
            {
                if (dgvKy.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn kỳ đánh giá cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (dgvKy.SelectedRows[0].DataBoundItem is not KyDanhGiaDTO currentKy) return;

                if (MessageBox.Show($"Xóa kỳ đánh giá [{currentKy.TenKyDanhGia}]?", "Xác nhận",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                try
                {
                    await hieuSuatService.DeleteKyDanhGiaAsync(currentKy.MaKyDanhGia);
                    await LoadKyGridAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            dlg.Controls.Add(dgvKy);
            dlg.Controls.Add(btnThemKy);
            dlg.Controls.Add(btnSuaKy);
            dlg.Controls.Add(btnXoaKy);
            dlg.Controls.Add(btnDong);

            dlg.Shown += async (_, _) => await LoadKyGridAsync();
            return dlg;
        }

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

        private static bool TryParseNullableDecimal(string input, out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(input)) return true;

            var text = input.Trim();
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                value = parsed;
                return true;
            }
            return false;
        }
    }
}
