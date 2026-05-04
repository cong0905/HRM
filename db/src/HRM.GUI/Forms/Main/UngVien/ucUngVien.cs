using HRM.BLL.Interfaces;
using HRM.Common.DTOs; // Nhớ đảm bảo bạn có DTO này
using HRM.GUI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.UngVien
{
    public partial class ucUngVien : UserControl
    {
        private readonly IUngVienService _ungVienService;
        private UserSessionDTO _session;

        // Hàm khởi tạo nhận Session
        public ucUngVien(UserSessionDTO session)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            _session = session;
            _ungVienService = Program.ServiceProvider.GetRequiredService<IUngVienService>();

            this.Load += async (s, e) => await LoadUngVienView();
        }

        public ucUngVien()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            _ungVienService = Program.ServiceProvider.GetRequiredService<IUngVienService>();
            this.Load += async (s, e) => await LoadUngVienView();
        }

        private async Task LoadUngVienView()
        {
            // 1. Tiêu đề
            var lblTitle = new Label
            {
                Text = "👥 Quản lý Danh sách Ứng Viên",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // 2. Ô Tìm kiếm
            var txtSearch = new TextBox
            {
                Location = new Point(20, 60),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Nhập tên, email ứng viên..."
            };

            var btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(330, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0;

            // 3. Phân quyền nút bấm dựa vào _session
            bool isManager = (_session?.VaiTro == "Admin" || _session?.VaiTro == "Quản trị viên" || _session?.VaiTro == "HR");

            var btnAdd = new Button { Text = "➕ Thêm mới", Location = new Point(440, 59), Size = new Size(100, 28), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = isManager };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnEdit = new Button { Text = "✏️ Sửa", Location = new Point(550, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = isManager };
            btnEdit.FlatAppearance.BorderSize = 0;

            var btnDelete = new Button { Text = "🗑️ Xóa", Location = new Point(640, 59), Size = new Size(80, 28), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Visible = isManager };
            btnDelete.FlatAppearance.BorderSize = 0;

            // 4. Bảng Dữ liệu (Dùng UIHelper)
            var dgv = UIHelper.CreateStyledDataGridView("dgvUngVien");
            dgv.Location = new Point(20, 100);
            dgv.Size = new Size(this.Width - 40, this.Height - 120);

            // Mapping cột theo cấu trúc DB bạn vừa gửi
            dgv.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.MinimumWidth = 100;
                    switch (col.DataPropertyName)
                    {
                        case "HoTen": col.HeaderText = "Họ Tên"; col.MinimumWidth = 150; break;
                        case "Email": col.HeaderText = "Email"; col.MinimumWidth = 180; break;
                        case "SoDienThoai": col.HeaderText = "SĐT"; col.MinimumWidth = 100; break;
                        case "PhanLoai": col.HeaderText = "Phân loại"; col.MinimumWidth = 120; break;
                        case "TrangThai": col.HeaderText = "Trạng thái"; col.MinimumWidth = 120; break;
                        case "NgayNop": col.HeaderText = "Ngày nộp"; col.DefaultCellStyle.Format = "dd/MM/yyyy"; break;
                        case "GhiChu": col.HeaderText = "Ghi chú"; col.MinimumWidth = 200; break;
                        case "MaUngVien":
                        case "MaTinTuyenDung":
                        case "DuongDanThuXinViec":
                            col.Visible = false; break;
                        default: col.Visible = false; break;
                        case "DuongDanCV":
                            col.HeaderText = "Link CV";
                            col.MinimumWidth = 200;
                            col.DefaultCellStyle.ForeColor = Color.Blue;
                            col.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Underline);
                            col.DefaultCellStyle.SelectionForeColor = Color.Blue;
                            break;
                    }
                }
            };
            dgv.CellFormatting += (s, e) =>
            {
                if (dgv.Columns[e.ColumnIndex].DataPropertyName == "TrangThai" && e.Value != null)
                {
                    string status = e.Value.ToString();
                    if (status.Contains("Đậu") || status.Contains("Trúng tuyển")) e.CellStyle.ForeColor = Color.Green;
                    else if (status.Contains("Từ chối") || status.Contains("Rớt")) e.CellStyle.ForeColor = Color.Red;
                    else if (status.Contains("Chờ")) e.CellStyle.ForeColor = Color.Orange;
                    e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                }
            };

            btnSearch.Click += async (s, e) =>
            {
                try
                {
                    var keyword = txtSearch.Text.Trim();
                    dgv.DataSource = string.IsNullOrWhiteSpace(keyword)
                        ? await _ungVienService.GetAllUngVienAsync()
                        : await _ungVienService.SearchUngVienAsync(keyword);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tìm kiếm dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // 5. Logic các nút bấm
            btnAdd.Click += async (s, e) =>
            {
                try
                {
                    // LƯU Ý: Phải tạo form này trước (frmThemUngVien)
                    var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.UngVien.frmThemUngVien>();
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        dgv.DataSource = await _ungVienService.GetAllUngVienAsync();
                    }
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Lỗi: Chưa đăng ký frmThemUngVien trong file Program.cs!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        int idDuocChon = Convert.ToInt32(dgv.CurrentRow.Cells["MaUngVien"].Value);

                        var frm = Program.ServiceProvider.GetRequiredService<Forms.Main.UngVien.frmSuaUngVien>();
                        frm.MaUngVienCachSua = idDuocChon;

                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            dgv.DataSource = await _ungVienService.GetAllUngVienAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Có lỗi khi mở Form Sửa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng click chọn một ứng viên trong danh sách trước khi bấm Sửa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            };

            btnDelete.Click += async (s, e) =>
            {
                if (dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0)
                {
                    string tenUV = dgv.CurrentRow.Cells["HoTen"].Value?.ToString() ?? "ứng viên này";
                    int idCachXoa = Convert.ToInt32(dgv.CurrentRow.Cells["MaUngVien"].Value);

                    var xacNhan = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa vĩnh viễn {tenUV} không?\nHành động này không thể hoàn tác!",
                        "Cảnh báo xóa",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (xacNhan == DialogResult.Yes)
                    {
                        try
                        {
                            // Đảm bảo trong IUngVienService có hàm xóa tương ứng
                            bool isSuccess = await _ungVienService.DeleteUngVienAsync(idCachXoa);

                            if (isSuccess)
                            {
                                MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                dgv.DataSource = await _ungVienService.GetAllUngVienAsync();
                            }
                            else
                            {
                                MessageBox.Show("Xóa thất bại! Dữ liệu ứng viên này có thể đang bị ràng buộc với Lịch phỏng vấn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("Vui lòng click chọn một ứng viên cần xóa!", "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            };

            dgv.CellContentClick += (s, e) =>
            {
                // Kiểm tra xem có click đúng vào dòng chứa dữ liệu và cột "DuongDanCV" hay không
                if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].DataPropertyName == "DuongDanCV")
                {
                    // Lấy giá trị của ô vừa click
                    string linkCV = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();

                    // Nếu có link hợp lệ (Bắt đầu bằng http hoặc https)
                    if (!string.IsNullOrEmpty(linkCV) && (linkCV.StartsWith("http://") || linkCV.StartsWith("https://")))
                    {
                        // BƯỚC THÊM MỚI: Hiển thị hộp thoại xác nhận
                        var xacNhan = MessageBox.Show(
                            $"Bạn chuẩn bị mở một liên kết ngoài ứng dụng:\n\n{linkCV}\n\nBạn có muốn tiếp tục mở bằng trình duyệt không?",
                            "Cảnh báo bảo mật",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2 // Đưa focus mặc định vào nút No để tránh bấm Enter nhầm
                        );

                        // Nếu người dùng chọn Yes thì mới chạy code mở link
                        if (xacNhan == DialogResult.Yes)
                        {
                            try
                            {
                                var psi = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = linkCV,
                                    UseShellExecute = true // Ép mở bằng trình duyệt mặc định
                                };
                                System.Diagnostics.Process.Start(psi);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Không thể mở link: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        // Thông báo nếu link không hợp lệ (không phải http/https)
                        MessageBox.Show("Đường dẫn này không hợp lệ hoặc không phải là liên kết web!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            // Add các control vào UserControl
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtSearch);
            this.Controls.Add(btnSearch);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(dgv);

            // Load dữ liệu lần đầu
            try
            {
                var data = await _ungVienService.GetAllUngVienAsync();
                dgv.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu Ứng viên: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}