using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main;

public partial class frmMain : Form
{
    private readonly INhanVienService _nhanVienService;
    private readonly IPhongBanService _phongBanService;
    private UserSessionDTO? _session;
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    private Label lblWelcome = null!;

    public frmMain(INhanVienService nhanVienService, IPhongBanService phongBanService)
    {
        _nhanVienService = nhanVienService;
        _phongBanService = phongBanService;
        InitializeComponent();
    }

    public void SetSession(UserSessionDTO session)
    {
        _session = session;
        lblWelcome.Text = $"Xin chào, {session.HoTen} ({session.VaiTro})";
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Text = "HRM - Hệ thống Quản lý Nhân sự";
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 242, 245);

        // ===== SIDEBAR =====
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = Color.FromArgb(30, 45, 80)
        };

        var lblLogo = new Label
        {
            Text = "🏢 HRM System",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSidebar.Controls.Add(lblLogo);

        // Menu buttons
        string[] menuItems = {
            "📊 Tổng quan",
            "👥 Nhân viên",
            "🏗️ Phòng ban",
            "⏰ Chấm công",
            "📋 Nghỉ phép",
            "💰 Lương",
            "📈 Báo cáo"
        };

        int yPos = 70;
        foreach (var item in menuItems)
        {
            var btn = new Button
            {
                Text = item,
                Dock = DockStyle.None,
                Location = new Point(0, yPos),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 210, 220),
                BackColor = Color.FromArgb(30, 45, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 70, 110);
            btn.Click += MenuButton_Click;
            pnlSidebar.Controls.Add(btn);
            yPos += 45;
        }

        // ===== HEADER =====
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        lblWelcome = new Label
        {
            Text = "Xin chào!",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(60, 60, 60),
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(10, 12, 0, 0)
        };

        var btnLogout = new Button
        {
            Text = "🚪 Đăng xuất",
            Dock = DockStyle.Right,
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(180, 50, 50),
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Click += (s, e) =>
        {
            this.Close();
        };

        pnlHeader.Controls.Add(lblWelcome);
        pnlHeader.Controls.Add(btnLogout);

        // ===== CONTENT =====
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 242, 245),
            Padding = new Padding(20)
        };

        // Dashboard mặc định
        var lblDashboard = new Label
        {
            Text = "📊 Tổng quan hệ thống\n\nChọn chức năng từ menu bên trái để bắt đầu.",
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.FromArgb(80, 80, 80),
            AutoSize = true,
            Location = new Point(30, 30)
        };
        pnlContent.Controls.Add(lblDashboard);

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlSidebar);

        this.ResumeLayout();
    }

    private async void MenuButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;

        pnlContent.Controls.Clear();

        if (btn.Text.Contains("Nhân viên"))
        {
            await LoadNhanVienView();
        }
        else if (btn.Text.Contains("Phòng ban"))
        {
            await LoadPhongBanView();
        }
        else
        {
            pnlContent.Controls.Add(new Label
            {
                Text = $"Module {btn.Text} - Đang phát triển...",
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(30, 30)
            });
        }
    }

    private async Task LoadNhanVienView()
    {
        var lblTitle = new Label
        {
            Text = "👥 Danh sách nhân viên",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(10, 10)
        };

        var dgv = new DataGridView
        {
            Name = "dgvNhanVien",
            Location = new Point(10, 50),
            Size = new Size(pnlContent.Width - 60, pnlContent.Height - 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9),
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 60, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            EnableHeadersVisualStyles = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 248, 255)
            }
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(dgv);

        try
        {
            var data = await _nhanVienService.GetAllAsync();
            dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadPhongBanView()
    {
        var lblTitle = new Label
        {
            Text = "🏗️ Danh sách phòng ban",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(10, 10)
        };

        var dgv = new DataGridView
        {
            Name = "dgvPhongBan",
            Location = new Point(10, 50),
            Size = new Size(pnlContent.Width - 60, pnlContent.Height - 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9),
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 60, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            },
            EnableHeadersVisualStyles = false
        };

        pnlContent.Controls.Add(lblTitle);
        pnlContent.Controls.Add(dgv);

        try
        {
            var data = await _phongBanService.GetAllAsync();
            dgv.DataSource = data;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
