using HRM.BLL.Interfaces;

namespace HRM.GUI.Forms.Main.TinTuyenDung
{
    public partial class frmSuaTinTuyenDung : Form
    {
        public int MaTinCachSua { get; set; }
        private readonly ITinTuyenDungService _tinTuyenDungService;
        private readonly IPhongBanService _phongBanService;
        public frmSuaTinTuyenDung(ITinTuyenDungService tinTuyenDungService, IPhongBanService phongBanService)
        {
            _tinTuyenDungService = tinTuyenDungService;
            _phongBanService = phongBanService;
            InitializeComponent();
            this.Load += frmSuaTinTuyenDung_Load;

        }

        private async void frmSuaTinTuyenDung_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Load danh sách phòng ban vào ComboBox trước (để nó có sẵn danh sách chọn)
                await LoadComboBoxPhongBan();

                // 2. Gọi Service để lấy đúng cái "Tin Tuyển Dụng" có ID là MaTinCachSua
                var tinTuyenDung = await _tinTuyenDungService.GetByIdAsync(this.MaTinCachSua);

                if (tinTuyenDung != null)
                {
                    // 3. LOGIC MAPPING: Đổ từng thuộc tính vào đúng Control trên giao diện
                    txtViTriTuyenDung.Text = tinTuyenDung.ViTriTuyenDung;
                    txtMoTaCongViec.Text = tinTuyenDung.MoTaCongViec;
                    txtYeuCau.Text = tinTuyenDung.YeuCauUngVien;
                    txtDiaDiem.Text = tinTuyenDung.DiadiemLamViec;
                    cbTrangThai.Text = tinTuyenDung.TrangThai?.Trim();

                    // Với NumericUpDown dùng thuộc tính .Value
                    txtSoLuongCanTuyen.Value = tinTuyenDung.SoLuongCanTuyen;
                    txtLuongMin.Value = tinTuyenDung.MucLuongMin ?? 0;
                    txtLuongMax.Value = tinTuyenDung.MucLuongMax ?? 0;

                    // Với DateTimePicker dùng thuộc tính .Value
                    dateNhanHoSo.Value = tinTuyenDung.ThoiHanNhanHoSo ?? DateTime.Now;

                    // Với ComboBox chọn đúng Phòng ban cũ theo ID
                    cbPhongBan.SelectedValue = tinTuyenDung.MaPhongBan;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải thông tin tin tuyển dụng: " + ex.Message);
            }
        }

        private async Task LoadComboBoxPhongBan()
        {
            try
            {
                // Lấy danh sách từ DB thông qua Service
                var dsPhongBan = await _phongBanService.GetAllAsync();

                // Đổ vào ComboBox
                cbPhongBan.DataSource = dsPhongBan;
                cbPhongBan.DisplayMember = "TenPhongBan"; // Tên hiển thị ra ngoài (có thể cần chỉnh cho khớp với Entity của bạn)
                cbPhongBan.ValueMember = "MaPhongBan";    // ID ngầm bên trong
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách phòng ban: " + ex.Message);
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra dữ liệu đầu vào (Validation)
            if (string.IsNullOrWhiteSpace(txtViTriTuyenDung.Text))
            {
                MessageBox.Show("Vui lòng không để trống vị trí tuyển dụng!", "Cảnh báo");
                return;
            }
            if (cbPhongBan.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn phòng ban!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtSoLuongCanTuyen.Value <= 0)
            {
                MessageBox.Show("Số lượng tuyển dụng phải lớn hơn 0!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cbTrangThai.SelectedIndex == -1 || string.IsNullOrWhiteSpace(cbTrangThai.Text))
            {
                MessageBox.Show("Vui lòng chọn trạng thái!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnLuu.Enabled = false; // Chặn bấm liên tục

                // 2. Lấy lại dữ liệu cũ từ Database để cập nhật
                var tinCanSua = await _tinTuyenDungService.GetByIdAsync(this.MaTinCachSua);

                if (tinCanSua != null)
                {
                    // 3. MAPPING: Gán giá trị mới từ giao diện vào Object
                    tinCanSua.ViTriTuyenDung = txtViTriTuyenDung.Text.Trim();
                    tinCanSua.MaPhongBan = (int)cbPhongBan.SelectedValue;
                    tinCanSua.SoLuongCanTuyen = (int)txtSoLuongCanTuyen.Value;
                    tinCanSua.MucLuongMin = txtLuongMin.Value;
                    tinCanSua.MucLuongMax = txtLuongMax.Value;
                    tinCanSua.ThoiHanNhanHoSo = dateNhanHoSo.Value;
                    tinCanSua.MoTaCongViec = txtMoTaCongViec.Text.Trim();
                    tinCanSua.YeuCauUngVien = txtYeuCau.Text.Trim();
                    tinCanSua.TrangThai = cbTrangThai.Text;

                    // 4. Gọi Service để lưu xuống Database
                    bool isSuccess = await _tinTuyenDungService.UpdateTinTuyenDungAsync(tinCanSua);

                    if (isSuccess)
                    {
                        MessageBox.Show("Cập nhật tin tuyển dụng thành công!", "Thông báo");

                        // DÒNG CỰC KỲ QUAN TRỌNG để màn hình chính biết mà load lại bảng
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Cập nhật thất bại, vui lòng kiểm tra lại!", "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi");
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }

        private void frmSuaTinTuyenDung_Load_1(object sender, EventArgs e)
        {

        }
    }
}
