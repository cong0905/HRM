using System.Globalization;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main;

public class frmSuaChamCong : Form
{
    private readonly IChamCongService _service;
    private readonly ChamCongDTO _dto;
    private readonly DateTimePicker _dtpNgay;
    private readonly TextBox _txtGioVao;
    private readonly TextBox _txtGioRa;
    private readonly ComboBox _cboTrangThai;
    private readonly TextBox _txtHinhThuc;
    private readonly TextBox _txtGhiChu;

    public frmSuaChamCong(IChamCongService service, ChamCongDTO dto)
    {
        _service = service;
        _dto = dto;

        Text = "Sửa bản ghi chấm công";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 400);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;

        var pad = 16;
        var y = pad;

        Controls.Add(new Label
        {
            Text = $"Nhân viên: {dto.TenNhanVien ?? "(Mã " + dto.MaNhanVien + ")"}",
            Location = new Point(pad, y),
            Width = 400,
            AutoSize = false,
            Height = 22,
            ForeColor = Color.FromArgb(45, 55, 70)
        });
        y += 30;

        Controls.Add(new Label { Text = "Ngày chấm công", Location = new Point(pad, y + 3), AutoSize = true });
        _dtpNgay = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(160, y),
            Width = 200,
            Value = dto.NgayChamCong.Date
        };
        Controls.Add(_dtpNgay);
        y += 36;

        Controls.Add(new Label { Text = "Giờ vào", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGioVao = new TextBox
        {
            Location = new Point(160, y),
            Width = 100
        };
        _txtGioVao.Text = dto.GioVao.HasValue ? dto.GioVao.Value.ToString(@"hh\:mm") : "";
        Controls.Add(_txtGioVao);
        y += 36;

        Controls.Add(new Label { Text = "Giờ ra", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGioRa = new TextBox
        {
            Location = new Point(160, y),
            Width = 100
        };
        _txtGioRa.Text = dto.GioRa.HasValue ? dto.GioRa.Value.ToString(@"hh\:mm") : "";
        Controls.Add(_txtGioRa);
        y += 36;

        Controls.Add(new Label { Text = "Trạng thái", Location = new Point(pad, y + 3), AutoSize = true });
        _cboTrangThai = new ComboBox
        {
            Location = new Point(160, y),
            Width = 240,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        _cboTrangThai.Items.AddRange(new object[]
        {
            "Bình thường", "Đi muộn", "Về sớm", "Đi muộn và về sớm", "Nghỉ phép", "Công tác"
        });
        _cboTrangThai.Text = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Bình thường" : dto.TrangThai;
        Controls.Add(_cboTrangThai);
        y += 36;

        Controls.Add(new Label { Text = "Hình thức", Location = new Point(pad, y + 3), AutoSize = true });
        _txtHinhThuc = new TextBox { Location = new Point(160, y), Width = 240, Text = dto.HinhThuc ?? "" };
        Controls.Add(_txtHinhThuc);
        y += 36;

        Controls.Add(new Label { Text = "Ghi chú", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGhiChu = new TextBox
        {
            Location = new Point(160, y),
            Width = 240,
            Height = 72,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = dto.GhiChu ?? ""
        };
        Controls.Add(_txtGhiChu);
        y += 84;

        var btnLuu = new Button
        {
            Text = "Lưu",
            Location = new Point(200, y),
            Size = new Size(96, 32),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.None
        };
        btnLuu.FlatAppearance.BorderSize = 0;
        btnLuu.Click += async (_, _) => await SaveAsync(btnLuu);

        var btnHuy = new Button
        {
            Text = "Hủy",
            Location = new Point(304, y),
            Size = new Size(96, 32),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnLuu);
        Controls.Add(btnHuy);

        CancelButton = btnHuy;
    }

    private static bool TryParseHm(string? s, out TimeSpan? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(s))
            return true;

        var t = s.Trim();
        if (TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out var ts))
        {
            if (ts < TimeSpan.Zero || ts >= TimeSpan.FromDays(1))
                return false;
            value = ts;
            return true;
        }

        foreach (var fmt in new[] { "HH:mm", "H:mm", @"hh\:mm", @"h\:mm" })
        {
            if (TimeSpan.TryParseExact(t, fmt, CultureInfo.InvariantCulture, out ts))
            {
                if (ts < TimeSpan.Zero || ts >= TimeSpan.FromDays(1))
                    return false;
                value = ts;
                return true;
            }
        }

        return false;
    }

    private async Task SaveAsync(Button btnLuu)
    {
        if (!TryParseHm(_txtGioVao.Text, out var gioVao))
        {
            MessageBox.Show("Giờ vào không hợp lệ. Dùng định dạng HH:mm (vd: 08:30).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryParseHm(_txtGioRa.Text, out var gioRa))
        {
            MessageBox.Show("Giờ ra không hợp lệ. Dùng định dạng HH:mm (vd: 17:30).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnLuu.Enabled = false;
            var update = new ChamCongAdminUpdateDTO
            {
                NgayChamCong = _dtpNgay.Value.Date,
                GioVao = gioVao,
                GioRa = gioRa,
                HinhThuc = _txtHinhThuc.Text,
                TrangThai = _cboTrangThai.Text,
                GhiChu = _txtGhiChu.Text
            };

            await _service.UpdateByAdminAsync(_dto.MaChamCong, update);
            MessageBox.Show("Đã cập nhật chấm công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnLuu.Enabled = true;
        }
    }
}
