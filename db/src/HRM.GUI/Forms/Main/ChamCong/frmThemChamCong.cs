using System.Globalization;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main.ChamCong;

public class frmThemChamCong : Form
{
    private readonly IChamCongService _chamCongService;
    private readonly INhanVienService _nhanVienService;
    private readonly ComboBox _cboNhanVien;
    private readonly DateTimePicker _dtpNgay;
    private readonly TextBox _txtGioVao;
    private readonly TextBox _txtGioRa;
    private readonly ComboBox _cboTrangThai;
    private readonly TextBox _txtHinhThuc;
    private readonly TextBox _txtGhiChu;
    private List<NhanVienDTO> _nhanViens = new();

    public frmThemChamCong(IChamCongService chamCongService, INhanVienService nhanVienService)
    {
        _chamCongService = chamCongService;
        _nhanVienService = nhanVienService;

        Text = "Thêm chấm công bù (Admin)";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(510, 430);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;

        var pad = 16;
        var y = pad;

        Controls.Add(new Label
        {
            Text = "Nhân viên",
            Location = new Point(pad, y + 3),
            AutoSize = true
        });
        _cboNhanVien = new ComboBox
        {
            Location = new Point(160, y),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        Controls.Add(_cboNhanVien);
        y += 36;

        Controls.Add(new Label { Text = "Ngày chấm công", Location = new Point(pad, y + 3), AutoSize = true });
        _dtpNgay = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(160, y),
            Width = 180,
            Value = DateTime.Today
        };
        Controls.Add(_dtpNgay);
        y += 36;

        Controls.Add(new Label { Text = "Giờ vào (HH:mm)", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGioVao = new TextBox
        {
            Location = new Point(160, y),
            Width = 120,
            Text = "08:30"
        };
        Controls.Add(_txtGioVao);
        y += 36;

        Controls.Add(new Label { Text = "Giờ ra (HH:mm)", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGioRa = new TextBox
        {
            Location = new Point(160, y),
            Width = 120,
            Text = "17:30"
        };
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
            "Bình thường", "Đi muộn", "Về sớm", "Đi muộn và về sớm", "Công tác"
        });
        _cboTrangThai.Text = "Bình thường";
        Controls.Add(_cboTrangThai);
        y += 36;

        Controls.Add(new Label { Text = "Hình thức", Location = new Point(pad, y + 3), AutoSize = true });
        _txtHinhThuc = new TextBox
        {
            Location = new Point(160, y),
            Width = 320,
            Text = "Admin nhập bù"
        };
        Controls.Add(_txtHinhThuc);
        y += 36;

        Controls.Add(new Label { Text = "Ghi chú", Location = new Point(pad, y + 3), AutoSize = true });
        _txtGhiChu = new TextBox
        {
            Location = new Point(160, y),
            Width = 320,
            Height = 78,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        Controls.Add(_txtGhiChu);
        y += 96;

        var btnLuu = new Button
        {
            Text = "Thêm",
            Location = new Point(292, y),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLuu.FlatAppearance.BorderSize = 0;
        btnLuu.Click += async (_, _) => await SaveAsync(btnLuu);

        var btnHuy = new Button
        {
            Text = "Hủy",
            Location = new Point(390, y),
            Size = new Size(90, 32),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(btnLuu);
        Controls.Add(btnHuy);
        CancelButton = btnHuy;

        Load += async (_, _) => await LoadNhanVienAsync();
    }

    private async Task LoadNhanVienAsync()
    {
        try
        {
            _nhanViens = (await _nhanVienService.GetAllAsync())
                .OrderBy(x => x.HoTen)
                .ToList();

            _cboNhanVien.DisplayMember = nameof(NhanVienDTO.HoTen);
            _cboNhanVien.ValueMember = nameof(NhanVienDTO.MaNhanVien);
            _cboNhanVien.DataSource = _nhanViens;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được danh sách nhân viên: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
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
        if (_cboNhanVien.SelectedItem is not NhanVienDTO nv)
        {
            MessageBox.Show("Vui lòng chọn nhân viên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

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
            var dto = new ChamCongAdminUpdateDTO
            {
                NgayChamCong = _dtpNgay.Value.Date,
                GioVao = gioVao,
                GioRa = gioRa,
                TrangThai = _cboTrangThai.Text,
                HinhThuc = _txtHinhThuc.Text,
                GhiChu = _txtGhiChu.Text
            };

            await _chamCongService.AddByAdminAsync(nv.MaNhanVien, dto);
            MessageBox.Show("Đã thêm chấm công bù thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
