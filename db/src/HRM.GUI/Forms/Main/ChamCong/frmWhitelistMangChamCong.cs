using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main.ChamCong;

public class frmWhitelistMangChamCong : Form
{
    private readonly IChamCongService _service;
    private readonly TextBox _txtRule;
    private readonly TextBox _txtGhiChu;
    private readonly DataGridView _dgv;

    public frmWhitelistMangChamCong(IChamCongService service)
    {
        _service = service;

        Text = "Whitelist mạng chấm công (Admin)";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(760, 460);
        BackColor = Color.White;

        var lblHuongDan = new Label
        {
            Text = "Nhập IP hoặc CIDR (vd: 192.168.1.10 hoặc 192.168.1.0/24)",
            Location = new Point(16, 16),
            AutoSize = true,
            ForeColor = Color.FromArgb(70, 80, 95)
        };
        Controls.Add(lblHuongDan);

        _txtRule = new TextBox
        {
            Location = new Point(16, 42),
            Width = 260,
            PlaceholderText = "Rule whitelist"
        };
        Controls.Add(_txtRule);

        _txtGhiChu = new TextBox
        {
            Location = new Point(286, 42),
            Width = 250,
            PlaceholderText = "Ghi chú (tùy chọn)"
        };
        Controls.Add(_txtGhiChu);

        var btnThem = new Button
        {
            Text = "Thêm rule",
            Location = new Point(546, 40),
            Size = new Size(92, 30),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnThem.FlatAppearance.BorderSize = 0;
        btnThem.Click += async (_, _) => await AddRuleAsync();
        Controls.Add(btnThem);

        var btnXoa = new Button
        {
            Text = "Xóa rule",
            Location = new Point(646, 40),
            Size = new Size(92, 30),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnXoa.FlatAppearance.BorderSize = 0;
        btnXoa.Click += async (_, _) => await RemoveRuleAsync();
        Controls.Add(btnXoa);

        _dgv = new DataGridView
        {
            Location = new Point(16, 84),
            Size = new Size(722, 320),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White
        };
        _dgv.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn col in _dgv.Columns)
            {
                switch (col.DataPropertyName)
                {
                    case "MaWhitelist":
                        col.Visible = false;
                        break;
                    case "Rule":
                        col.HeaderText = "Rule";
                        col.FillWeight = 130;
                        break;
                    case "GhiChu":
                        col.HeaderText = "Ghi chú";
                        col.FillWeight = 120;
                        break;
                    case "NgayTao":
                        col.HeaderText = "Ngày tạo";
                        col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                        col.FillWeight = 90;
                        break;
                }
            }
        };
        Controls.Add(_dgv);

        var btnDong = new Button
        {
            Text = "Đóng",
            Location = new Point(646, 414),
            Size = new Size(92, 32),
            DialogResult = DialogResult.OK
        };
        Controls.Add(btnDong);

        Load += async (_, _) => await LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        var data = await _service.GetWhitelistAsync();
        _dgv.DataSource = null;
        _dgv.DataSource = data;
    }

    private async Task AddRuleAsync()
    {
        try
        {
            await _service.AddWhitelistAsync(new ChamCongWhitelistCreateDTO
            {
                Rule = _txtRule.Text,
                GhiChu = _txtGhiChu.Text
            });
            _txtRule.Clear();
            _txtGhiChu.Clear();
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task RemoveRuleAsync()
    {
        if (_dgv.SelectedRows.Count == 0 ||
            _dgv.SelectedRows[0].DataBoundItem is not ChamCongWhitelistDTO selected)
        {
            MessageBox.Show("Vui lòng chọn rule cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Xóa rule \"{selected.Rule}\" khỏi whitelist?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            await _service.RemoveWhitelistAsync(selected.MaWhitelist);
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
