using HRM.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.UngVien
{
    public partial class frmThemUngVien : Form
    {
        private readonly IUngVienService _ungVienService;
        private readonly ITinTuyenDungService _tinTuyenDungService;
        public frmThemUngVien()
        {
            _ungVienService = Program.ServiceProvider.GetRequiredService<IUngVienService>();
            _tinTuyenDungService = Program.ServiceProvider.GetRequiredService<ITinTuyenDungService>();
            this.Load += FrmThemUngVien_Load;
            InitializeComponent();
        }

        private async void FrmThemUngVien_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Lấy danh sách Tin tuyển dụng từ Service
                var lstTin = await _tinTuyenDungService.GetAllAsync();

                // 2. Nạp vào ComboBox
                if (lstTin != null && lstTin.Count > 0)
                {
                    cbVitriTuyenDung.DataSource = lstTin;
                    cbVitriTuyenDung.DisplayMember = "ViTriTuyenDung";
                    cbVitriTuyenDung.ValueMember = "MaTinTuyenDung";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách vị trí tuyển dụng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenUngVien.Text)) // Đổi txtHoTen thành tên ô TextBox của bạn
            {
                MessageBox.Show("Vui lòng nhập Tên ứng viên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenUngVien.Focus();
                return;
            }

            if (cbVitriTuyenDung.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Vị trí tuyển dụng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 2. GOM DỮ LIỆU TỪ GIAO DIỆN VÀO ĐỐI TƯỢNG UNGVIEN
                var newUV = new HRM.Domain.Entities.UngVien
                {
                    HoTen = txtTenUngVien.Text.Trim(),
                    MaTinTuyenDung = Convert.ToInt32(cbVitriTuyenDung.SelectedValue), // Lấy ID đang ngầm giấu bên dưới
                    SoDienThoai = txtSoDienThoai.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    DuongDanCV = txtDuongDanCV.Text.Trim(), // Đổi txtCV thành tên TextBox đường dẫn CV của bạn
                    PhanLoai = cbPhanLoai.SelectedItem?.ToString() ?? "Chưa phân loại",
                    TrangThai = cbTrangThai.SelectedItem?.ToString() ?? "Chờ phỏng vấn",
                    NgayNop = dateNgayNop.Value,
                    GhiChu = txtGhiChu.Text.Trim()
                };

                // 3. GỌI SERVICE ĐỂ ĐẨY XUỐNG DATABASE
                await _ungVienService.AddUngVienAsync(newUV);

                // 4. THÔNG BÁO VÀ ĐÓNG FORMD
                MessageBox.Show("Thêm ứng viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Dòng này CỰC KỲ QUAN TRỌNG: Nó báo cho Form Danh Sách biết là "Tôi đã thêm thành công, hãy tải lại bảng đi!"
                this.DialogResult = DialogResult.OK;
                this.Close(); // Đóng form thêm
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra khi lưu dữ liệu: {ex.InnerException?.Message ?? ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
