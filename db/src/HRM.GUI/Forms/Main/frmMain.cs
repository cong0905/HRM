using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Forms.Main.BangLuong;
using HRM.GUI.Forms.Main.ChamCong;
using HRM.GUI.Forms.Main.HieuSuat;
using HRM.GUI.Forms.Main.NghiPhep;
using HRM.GUI.Forms.Main.NhanVien;
using HRM.GUI.Forms.Main.PhongBan;
using HRM.GUI.Forms.Main.PhongVan;
using HRM.GUI.Forms.Main.TaiKhoan;
using HRM.GUI.Forms.Main.TinTuyenDung;
using HRM.GUI.Forms.Main.UngVien;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace HRM.GUI.Forms.Main;

public partial class frmMain : Form
{
    public bool ClosedForRelogin { get; private set; }

    private readonly ITaiKhoanService _taiKhoanService;
    private UserSessionDTO? _session;
    private bool isTuyenDungExpanded = false;
    private bool isLuongExpanded = false;

    public frmMain(ITaiKhoanService taiKhoanService)
    {
        _taiKhoanService = taiKhoanService;
        InitializeComponent();
    }

    private UserControl? currentModule = null;

    private void ShowModule(UserControl newModule)
    {
        if (currentModule != null)
        {
            pnlContent.Controls.Remove(currentModule);
            currentModule.Dispose();
        }

        currentModule = newModule;
        currentModule.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(currentModule);
        currentModule.BringToFront();
    }

    public void SetSession(UserSessionDTO session)
    {
        _session = session;
        lblWelcome.Text = $"Xin chào, {session.HoTen} ({session.VaiTro})";
        SetupMenu();
    }

    private void SetupMenu()
    {
        pnlSidebar.Controls.Clear();
        int yPos = 70;

        void TaoNutMenu(string text, bool isSubMenu = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(0, yPos),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", isSubMenu ? 9 : 10),
                ForeColor = isSubMenu ? Color.FromArgb(180, 190, 200) : Color.FromArgb(200, 210, 220),
                BackColor = isSubMenu ? Color.FromArgb(40, 55, 90) : Color.FromArgb(30, 45, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(isSubMenu ? 40 : 20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 70, 110);
            btn.Click += MenuButton_Click;

            pnlSidebar.Controls.Add(btn);
            yPos += 45;
        }

        TaoNutMenu("📊 Tổng quan");

        string vaiTro = _session?.VaiTro ?? "";
        bool isAdmin = vaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                       vaiTro.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);

        if (isAdmin)
        {
            TaoNutMenu("👥 Nhân viên");
            TaoNutMenu("🏗️ Phòng ban");

            string icon = isTuyenDungExpanded ? "▼" : "▶";
            TaoNutMenu($"🤝 Tuyển dụng {icon}");

            if (isTuyenDungExpanded)
            {
                TaoNutMenu("📝 Tin tuyển dụng", true);
                TaoNutMenu("🧑‍🎓 Ứng viên", true);
                TaoNutMenu("🎤 Phỏng vấn", true);
            }

            TaoNutMenu("📈 Báo cáo");
            TaoNutMenu("📈 Hiệu suất");
            TaoNutMenu("🔑 Tài khoản");
        }

        TaoNutMenu("⏰ Chấm công");
        TaoNutMenu("📋 Nghỉ phép");

        string iconLuong = isLuongExpanded ? "▼" : "▶";
        TaoNutMenu($"💰 Lương {iconLuong}");
        if (isLuongExpanded)
        {
            TaoNutMenu("📊 Bảng lương", true);
            TaoNutMenu("📋 Thưởng phạt", true);
        }
    }

    private void btnLogout_Click(object? sender, EventArgs e)
    {
        ClosedForRelogin = true;
        Close();
    }

    private void btnDoiMatKhau_Click(object? sender, EventArgs e)
    {
        if (_session == null) return;
        using var f = new frmDoiMatKhau(_taiKhoanService, _session);
        f.ShowDialog();
    }

    private void MenuButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        if (_session == null)
        {
            MessageBox.Show("Phiên đăng nhập đã hết hạn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string text = btn.Text;

        // Xử lý Expand/Collapse menu
        if (text.Contains("Tuyển dụng") && (text.Contains("▶") || text.Contains("▼")))
        {
            isTuyenDungExpanded = !isTuyenDungExpanded;
            SetupMenu();
            return;
        }
        if (text.Contains("Lương") && (text.Contains("▶") || text.Contains("▼")))
        {
            isLuongExpanded = !isLuongExpanded;
            SetupMenu();
            return;
        }

        // Chuyển module
        UserControl? uc = null;
        if (text.Contains("Tổng quan")) uc = null; // Dashboard - Đang phát triển
        else if (text.Contains("Nhân viên")) uc = new ucNhanVien(_session);
        else if (text.Contains("Phòng ban")) uc = new ucPhongBan(_session);
        else if (text.Contains("Tài khoản")) uc = new ucTaiKhoan(_session);
        else if (text.Contains("Chấm công")) uc = new ucChamCong(_session);
        else if (text.Contains("Nghỉ phép")) uc = new ucNghiPhep(_session);
        else if (text.Contains("Bảng lương")) uc = new ucBangLuong(_session);
        else if (text.Contains("Thưởng phạt")) uc = new ucThuongPhat(_session);
        else if (text.Contains("Tin tuyển dụng")) uc = new ucTinTuyenDung(_session);
        else if (text.Contains("Ứng viên")) uc = new ucUngVien(_session);
        else if (text.Contains("Phỏng vấn")) uc = new ucPhongVan(_session);
        else if (text.Contains("Hiệu suất")) uc = new ucHieuSuat(_session);

        if (uc != null)
        {
            ShowModule(uc);
        }
        else if (text.Contains("Tổng quan") || text.Contains("Báo cáo"))
        {
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(new Label
            {
                Text = $"Module {text} đang được phát triển...",
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(30, 30)
            });
        }
    }
}
