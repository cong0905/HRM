using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.TaiKhoan
{
    public partial class ucTaiKhoan : UserControl
    {
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly UserSessionDTO? _session;
        private System.Windows.Forms.Timer? _searchTimer;

        public ucTaiKhoan() : this(null) { }

        public ucTaiKhoan(UserSessionDTO? session)
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            _session = session;
            if (UIHelper.IsDesignTime())
            {
                _taiKhoanService = null!;
                return;
            }
            _taiKhoanService = Program.ServiceProvider.GetRequiredService<ITaiKhoanService>();
            Load += async (_, _) => await LoadView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchTimer?.Stop();
                _searchTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async Task LoadView()
        {
            var lblTitle = new Label
            {
                Text = "🔑 Quản lý tài khoản",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var txtSearch = new TextBox
            {
                Location = new Point(20, 60),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Nhập tên đăng nhập hoặc Tên chủ sở hữu..."
            };

            var lblVaiTro = new Label { Text = "Vai trò:", Location = new Point(280, 63), AutoSize = true, Font = new Font("Segoe UI", 9) };
            var cboVaiTro = new ComboBox
            {
                Location = new Point(330, 60),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboVaiTro.Items.AddRange(new[] { "--- Tất cả ---", "Admin", "Quản trị viên", "Quản lý", "Nhân viên" });
            cboVaiTro.SelectedIndex = 0;

            var lblTrangThai = new Label { Text = "Trạng thái:", Location = new Point(460, 63), AutoSize = true, Font = new Font("Segoe UI", 9) };
            var cboTrangThai = new ComboBox
            {
                Location = new Point(530, 60),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrangThai.Items.AddRange(new[] { "--- Tất cả ---", "Hoạt động", "Đã khóa" });
            cboTrangThai.SelectedIndex = 0;

            var btnReset = new Button
            {
                Text = "🔄 Reset",
                Location = new Point(660, 59),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;

            var btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(740, 59),
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
                Location = new Point(850, 59),
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
                Location = new Point(930, 59),
                Size = new Size(70, 28),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;

            var dgv = UIHelper.CreateStyledDataGridView("dgvTaiKhoan");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(Width - 40, Height - 120);

            dgv.DataBindingComplete += (_, _) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {
                        case "TenDangNhap": col.HeaderText = "Tên Đăng Nhập"; col.MinimumWidth = 150; break;
                        case "TenNhanVien": col.HeaderText = "Chủ Sở Hữu"; col.MinimumWidth = 180; break;
                        case "VaiTro": col.HeaderText = "Vai Trò"; break;
                        case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                        case "NgayTao": col.HeaderText = "Ngày Tạo"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                        case "LanDangNhapCuoi": col.HeaderText = "Đăng Nhập Cuối"; col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"; break;
                        case "MaTaiKhoan":
                        case "MaNhanVien":
                            col.Visible = false;
                            break;
                    }
                }
            };

            _searchTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchTimer.Tick += async (_, _) =>
            {
                _searchTimer.Stop();
                var kw = txtSearch.Text.Trim();
                var role = cboVaiTro.SelectedItem?.ToString();
                var status = cboTrangThai.SelectedItem?.ToString();
                dgv.DataSource = await _taiKhoanService.SearchAsync(kw, role, status);
            };

            void TriggerSearch()
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }

            txtSearch.TextChanged += (_, _) => TriggerSearch();
            cboVaiTro.SelectedIndexChanged += (_, _) => TriggerSearch();
            cboTrangThai.SelectedIndexChanged += (_, _) => TriggerSearch();

            btnReset.Click += async (_, _) =>
            {
                txtSearch.Text = string.Empty;
                cboVaiTro.SelectedIndex = 0;
                cboTrangThai.SelectedIndex = 0;
                dgv.DataSource = await _taiKhoanService.GetAllAsync();
            };

            btnAdd.Click += async (_, _) =>
            {
                var frm = Program.ServiceProvider.GetRequiredService<Forms.Auth.frmTaoTaiKhoan>();
                frm.ShowDialog();
                dgv.DataSource = await _taiKhoanService.GetAllAsync();
            };

            btnEdit.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var dto = (TaiKhoanDTO)dgv.SelectedRows[0].DataBoundItem;
                var frm = new Forms.Auth.frmSuaTaiKhoan(_taiKhoanService, dto);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    dgv.DataSource = await _taiKhoanService.GetAllAsync();
                }
            };

            btnDelete.Click += async (_, _) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var dto = (TaiKhoanDTO)dgv.SelectedRows[0].DataBoundItem;

                if (_session != null && _session.MaNhanVien == dto.MaNhanVien)
                {
                    MessageBox.Show("Bạn không thể tự xóa tài khoản đang đăng nhập của chính mình!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"Xóa tài khoản [{dto.TenDangNhap}]?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                try
                {
                    await _taiKhoanService.DeleteAsync(dto.MaTaiKhoan);
                    dgv.DataSource = await _taiKhoanService.GetAllAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(txtSearch);
            Controls.Add(lblVaiTro);
            Controls.Add(cboVaiTro);
            Controls.Add(lblTrangThai);
            Controls.Add(cboTrangThai);
            Controls.Add(btnReset);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(dgv);

            try
            {
                dgv.DataSource = await _taiKhoanService.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }
    }
}
