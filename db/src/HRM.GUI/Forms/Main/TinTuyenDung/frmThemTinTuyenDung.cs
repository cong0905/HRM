using HRM.BLL.Interfaces;

namespace HRM.GUI.Forms.Main.TinTuyenDung
{
    public partial class frmThemTinTuyenDung : Form
    {
        private readonly ITinTuyenDungService _tinTuyenDungService;
        private readonly IPhongBanService _phongBanService;
        public frmThemTinTuyenDung(ITinTuyenDungService tinTuyenDungService, IPhongBanService phongBanService)
        {
            _tinTuyenDungService = tinTuyenDungService;
            _phongBanService = phongBanService;
            InitializeComponent();
            this.Load += frmThemTinTuyenDung_Load;
        }

        private async Task LoadComboBoxPhongBan()
        {
            try
            {
                // Giả sử hàm GetAllAsync của bạn trả về List các Phòng ban
                var dsPhongBan = await _phongBanService.GetAllAsync();

                cbPhongBan.DataSource = dsPhongBan;

                // Tên thuộc tính hiển thị cho người dùng thấy (Ví dụ: "Phòng Nhân sự")
                cbPhongBan.DisplayMember = "TenPhongBan";

                // Giá trị thực tế sẽ lưu vào DB (Ví dụ: 1, 2, 3...)
                cbPhongBan.ValueMember = "MaPhongBan";

                cbPhongBan.SelectedIndex = -1; // Để trống lúc mới hiện form cho đẹp
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load phòng ban: " + ex.Message);
            }
        }

        // Gọi hàm này trong sự kiện Load của Form
        private async void frmThemTinTuyenDung_Load(object sender, EventArgs e)
        {
            await LoadComboBoxPhongBan();
            txtLuongMin.Maximum = 1000000000;
            txtLuongMax.Maximum = 1000000000;
            txtLuongMin.ThousandsSeparator = true;
            txtLuongMax.ThousandsSeparator = true;
            cbTrangThai.SelectedIndex = -1;


        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtViTriTuyenDung.Text))
            {
                MessageBox.Show("Vui lòng nhập vị trí tuyển dụng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtLuongMin.Value > txtLuongMax.Value)
            {
                MessageBox.Show("Mức lương tối thiểu không thể lớn hơn mức lương tối đa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cbTrangThai.SelectedIndex == -1)
            {
                MessageBox.Show("Vui lòng chọn trạng thái!");
                return;
            }
            try
            {
                btnLuu.Enabled = false;
                var entity = new Domain.Entities.TinTuyenDung
                {
                    ViTriTuyenDung = txtViTriTuyenDung.Text.Trim(),
                    SoLuongCanTuyen = (int)txtSoLuongCanTuyen.Value,
                    MucLuongMin = txtLuongMin.Value,
                    MucLuongMax = txtLuongMax.Value,
                    ThoiHanNhanHoSo = dateNhanHoSo.Value,
                    DiadiemLamViec = txtDiaDiem.Text.Trim(),
                    MoTaCongViec = txtMoTaCongViec.Text.Trim(),
                    YeuCauUngVien = txtYeuCau.Text.Trim(),
                    MaPhongBan = (int)cbPhongBan.SelectedValue,
                    NguoiTao = 1,
                    TrangThai = cbTrangThai.Text,
                    NgayDang = DateTime.Now

                };
                bool isSuccess = await _tinTuyenDungService.AddTinTuyenDungAsync(entity);
                if (isSuccess)
                {
                    MessageBox.Show("Thêm tin tuyển dụng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK; // Tín hiệu báo cho Form Main load lại lưới DataGridView
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Thêm thất bại. Vui lòng thử lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi râu ria (nếu có)
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Lỗi hệ thống: {error}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Mở khóa nút lưu dù thành công hay thất bại
                btnLuu.Enabled = true;
            }


        }
    }
}
