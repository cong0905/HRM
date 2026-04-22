using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.TinTuyenDung
{
    public partial class ucTinTuyenDung : UserControl
    {
        private readonly ITinTuyenDungService _tinTuyenDungService;
        private UserSessionDTO _session;

        // 2. Thêm tham số vào hàm khởi tạo
        public ucTinTuyenDung(UserSessionDTO session)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;

            // Nhận dữ liệu session truyền vào và gán cho biến cục bộ
            _session = session;

            _tinTuyenDungService = Program.ServiceProvider.GetRequiredService<ITinTuyenDungService>();
            this.Load += async (s, e) => await LoadTinTuyenDungView();
        }
        public ucTinTuyenDung()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            _tinTuyenDungService = Program.ServiceProvider.GetRequiredService<ITinTuyenDungService>();
            this.Load += async (s, e) => await LoadTinTuyenDungView();
        }

        private async Task LoadTinTuyenDungView()
        {
            var lblTitle = new Label
            {
                Text = "📝 Danh sách Tin Tuyển Dụng", // Đổi icon và text
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            var txtSearch = new TextBox
            {
                Location = new Point(20, 60),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Nhập mã tin, vị trí tuyển dụng..." // Đổi gợi ý
            };
            var btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(330, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0;

            var btnAdd = new Button { Text = "➕ Thêm mới", Location = new Point(440, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnEdit = new Button { Text = "✏️ Sửa", Location = new Point(550, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
            btnEdit.FlatAppearance.BorderSize = 0;

            var btnDelete = new Button { Text = "🗑️ Xóa", Location = new Point(640, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên") };
            btnDelete.FlatAppearance.BorderSize = 0;

            var dgv = UIHelper.CreateStyledDataGridView("dgvTinTuyenDung"); // Đổi tên control
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(this.Width - 40, this.Height - 120);

            dgv.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {

                        case "MaHienThi": col.HeaderText = "Mã Tin"; col.MinimumWidth = 80; break;
                        case "ViTriTuyenDung": col.HeaderText = "Vị trí tuyển dụng"; col.MinimumWidth = 200; break;
                        case "SoLuongCanTuyen": col.HeaderText = "Số lượng"; col.MinimumWidth = 80; break;
                        case "MoTaCongViec": col.HeaderText = "Mô tả công Việc"; col.MinimumWidth = 100; break;
                        case "TenPhongBan": col.HeaderText = "Phòng ban"; col.MinimumWidth = 150; break;
                        case "YeuCauUngVien": col.HeaderText = "Yêu cầu ứng viên"; col.MinimumWidth = 100; break;
                        case "ThoiHanNhanHoSo": col.HeaderText = "Hạn nộp hồ sơ"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                        case "MucLuong": col.HeaderText = "Mức lương (VNĐ)"; col.MinimumWidth = 150; break;
                        case "TrangThai": col.HeaderText = "Trạng Thái"; break;
                        case "MaTinTuyenDung": col.Visible = false; break;
                        default: col.Visible = false; break;
                    }
                }
            };

            btnAdd.Click += async (s, e) =>
            {
                try
                {
                    var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.TinTuyenDung.frmThemTinTuyenDung>();
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                    }
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Lỗi: Chưa đăng ký frmThemTinTuyenDung trong file Program.cs!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnEdit.Click += async (s, e) =>
            {
                if (dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0)
                {
                    try
                    {
                        int idDuocChon = Convert.ToInt32(dgv.CurrentRow.Cells["MaTinTuyenDung"].Value);
                        var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.TinTuyenDung.frmSuaTinTuyenDung>();
                        frm.MaTinCachSua = idDuocChon;

                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Có lỗi khi mở Form Sửa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng click chọn một tin tuyển dụng trong danh sách trước khi bấm Sửa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            };

            btnDelete.Click += async (s, e) =>
            {
                if (dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0)
                {
                    string tenViTri = dgv.CurrentRow.Cells["ViTriTuyenDung"].Value?.ToString() ?? "tin này";
                    int idCachXoa = Convert.ToInt32(dgv.CurrentRow.Cells["MaTinTuyenDung"].Value);

                    var xacNhan = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa vĩnh viễn {tenViTri} không?\nHành động này không thể hoàn tác!",
                        "Cảnh báo xóa",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (xacNhan == DialogResult.Yes)
                    {
                        try
                        {
                            bool isSuccess = await _tinTuyenDungService.DeleteTinTuyenDungAsync(idCachXoa);

                            if (isSuccess)
                            {
                                MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                dgv.DataSource = await _tinTuyenDungService.GetAllAsync();
                            }
                            else
                            {
                                MessageBox.Show("Xóa thất bại! Dữ liệu này có thể đang bị ràng buộc ở nơi khác.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi hệ thống khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng click chọn một tin tuyển dụng cần xóa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            };

            object rawData = null;
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtSearch);
            this.Controls.Add(btnSearch);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(dgv);
            try
            {
                var data = await _tinTuyenDungService.GetAllAsync();
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
