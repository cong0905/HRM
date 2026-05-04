using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;

namespace HRM.GUI.Forms.Main.HieuSuat;

public sealed partial class frmHieuSuat : UserControl
{
    private readonly INhanVienService _nhanVienService;
    private readonly IHieuSuatService _hieuSuatService;

    private Label _lblTitle = null!;
    private TextBox _txtSearch = null!;
    private Label _lblKy = null!;
    private ComboBox _cboKyDanhGia = null!;
    private Button _btnReset = null!;
    private Button _btnKyDanhGia = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private DataGridView _dgv = null!;

    private List<KyDanhGiaDTO> _kyDanhGiaItems = new();
    private bool _isReloadingKy;
    private bool _isLoadingGrid;

    public frmHieuSuat(INhanVienService nhanVienService, IHieuSuatService hieuSuatService)
    {
        _nhanVienService = nhanVienService;
        _hieuSuatService = hieuSuatService;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Load += frmHieuSuat_Load;
    }

    private async void frmHieuSuat_Load(object? sender, EventArgs e)
    {
        Load -= frmHieuSuat_Load;
        BuildLayout();
        WireEvents();
        await ReloadKyDanhGiaAsync();
        await LoadGridAsync();
    }

    private void BuildLayout()
    {
        _lblTitle = UIHelper.CreateModuleTitleLabel("📈 Quản lý hiệu suất", new Point(20, 15));
        _txtSearch = UIHelper.CreateSearchTextBox(new Point(20, 60), new Size(260, 25), "Tên nhân viên / phòng ban...");
        _lblKy = UIHelper.CreateFilterLabel("Kỳ đánh giá:", new Point(290, 63));
        _cboKyDanhGia = UIHelper.CreateFilterComboBox(new Point(370, 60), new Size(220, 25));

        _btnKyDanhGia = UIHelper.CreateActionButton(
            "🗂️ Kỳ đánh giá",
            new Point(600, 59),
            new Size(105, 28),
            Color.FromArgb(52, 152, 219));

        _btnReset = UIHelper.CreateActionButton(
            "🔄 Reset",
            new Point(710, 59),
            new Size(70, 28),
            Color.FromArgb(149, 165, 166));

        _btnAdd = UIHelper.CreateActionButton(
            "➕ Thêm mới",
            new Point(790, 59),
            new Size(100, 28),
            Color.FromArgb(46, 204, 113));

        _btnEdit = UIHelper.CreateActionButton(
            "✏️ Sửa",
            new Point(900, 59),
            new Size(70, 28),
            Color.FromArgb(241, 196, 15));

        _btnDelete = UIHelper.CreateActionButton(
            "🗑️ Xóa",
            new Point(980, 59),
            new Size(70, 28),
            Color.FromArgb(231, 76, 60));

        _dgv = UIHelper.CreateStyledDataGridView("dgvHieuSuat");
        _dgv.Location = new Point(20, 100);
        _dgv.Size = new Size(Width - 40, Height - 120);

        Controls.Add(_lblTitle);
        Controls.Add(_txtSearch);
        Controls.Add(_lblKy);
        Controls.Add(_cboKyDanhGia);
        Controls.Add(_btnKyDanhGia);
        Controls.Add(_btnReset);
        Controls.Add(_btnAdd);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDelete);
        Controls.Add(_dgv);

        Resize += (_, _) =>
        {
            if (_dgv == null) return;
            _dgv.Size = new Size(Math.Max(200, Width - 40), Math.Max(200, Height - 120));
        };
    }

    internal sealed class LookupItem
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
