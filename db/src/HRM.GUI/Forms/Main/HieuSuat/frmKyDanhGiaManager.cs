using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main.HieuSuat;

public sealed class frmKyDanhGiaManager : Form
{
    private readonly IHieuSuatService _hieuSuatService;
    private readonly DataGridView _dgvKy;

    public frmKyDanhGiaManager(IHieuSuatService hieuSuatService)
    {
        _hieuSuatService = hieuSuatService;

        Text = "Quản lý kỳ đánh giá";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(760, 430);

        _dgvKy = new DataGridView
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

        btnThemKy.Click += async (_, _) => await HandleAddAsync();
        btnSuaKy.Click += async (_, _) => await HandleEditAsync();
        btnXoaKy.Click += async (_, _) => await HandleDeleteAsync();

        Controls.Add(_dgvKy);
        Controls.Add(btnThemKy);
        Controls.Add(btnSuaKy);
        Controls.Add(btnXoaKy);
        Controls.Add(btnDong);

        Shown += async (_, _) => await LoadKyGridAsync();
    }

    private async Task HandleAddAsync()
    {
        if (!TryShowKyDanhGiaEditor(null, out var dto))
            return;

        try
        {
            await _hieuSuatService.CreateKyDanhGiaAsync(dto);
            await LoadKyGridAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task HandleEditAsync()
    {
        if (_dgvKy.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn kỳ đánh giá cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_dgvKy.SelectedRows[0].DataBoundItem is not KyDanhGiaDTO currentKy)
            return;

        if (!TryShowKyDanhGiaEditor(currentKy, out var dto))
            return;

        try
        {
            await _hieuSuatService.UpdateKyDanhGiaAsync(currentKy.MaKyDanhGia, dto);
            await LoadKyGridAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task HandleDeleteAsync()
    {
        if (_dgvKy.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn kỳ đánh giá cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_dgvKy.SelectedRows[0].DataBoundItem is not KyDanhGiaDTO currentKy)
            return;

        var confirm = MessageBox.Show(
            $"Xóa kỳ đánh giá [{currentKy.TenKyDanhGia}]?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _hieuSuatService.DeleteKyDanhGiaAsync(currentKy.MaKyDanhGia);
            await LoadKyGridAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadKyGridAsync()
    {
        var periods = await _hieuSuatService.GetKyDanhGiaAsync();
        _dgvKy.DataSource = periods;

        foreach (DataGridViewColumn col in _dgvKy.Columns)
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

    private static bool TryShowKyDanhGiaEditor(KyDanhGiaDTO? current, out KyDanhGiaDTO result)
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
        var dtpTu = new DateTimePicker
        {
            Location = new Point(20, 100),
            Size = new Size(180, 25),
            Format = DateTimePickerFormat.Short
        };

        var lblDen = new Label { Text = "Ngày kết thúc", AutoSize = true, Location = new Point(220, 80) };
        var dtpDen = new DateTimePicker
        {
            Location = new Point(220, 100),
            Size = new Size(180, 25),
            Format = DateTimePickerFormat.Short
        };

        var btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(245, 160),
            Size = new Size(75, 30),
            DialogResult = DialogResult.None
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(325, 160),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

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
}
