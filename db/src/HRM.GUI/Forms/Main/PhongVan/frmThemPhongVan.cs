using HRM.BLL.Interfaces;

namespace HRM.GUI.Forms.Main
{
    public partial class frmThemPhongVan : Form
    {
        private readonly IPhongVanService _phongVanService;
        private readonly INhanVienService _nhanVienService;
        //private readonly IUngVienService _ungVienService;
        public frmThemPhongVan(IPhongVanService phongVanService,
        INhanVienService nhanVienService)
        //IUngVienService ungVienService)
        {

            InitializeComponent();
            _phongVanService = phongVanService;
            _nhanVienService = nhanVienService;
            //_ungVienService = ungVienService;
            this.Load += frmThemPhongVan_LoadAsync;
            btnLuu.Click += BtnLuu_ClickAsync;

        }

        private async void BtnLuu_ClickAsync(object? sender, EventArgs e)
        {
            try
            {
                // 1. Kiểm tra xem người dùng đã chọn đủ chưa
                if (cbMaUngVien.SelectedValue == null || cbNguoiPV.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn Ứng viên và Người phỏng vấn!", "Cảnh báo");
                    return;
                }

                // 2. Gom dữ liệu để chuẩn bị lưu
                var phongVanMoi = new HRM.Domain.Entities.PhongVan
                {
                    MaUngVien = (int)cbMaUngVien.SelectedValue,
                    NguoiPhongVan = (int)cbNguoiPV.SelectedValue,

                    // Ép kiểu chữ từ combobox sang số nguyên
                    VongPhongVan = int.Parse(cbVongPhongVan.Text),

                    NgayPhongVan = dtpNgayPhongvan.Value, // Nhớ kiểm tra lại đúng tên công cụ trên form của bạn
                    DiaDiem = txtDiaDiem.Text,
                    TrangThai = cbTrangThai.Text,
                    NhanXet = txtNhanXet.Text
                    // Lưu ý: Nếu có ô KetQua thì thêm vào đây
                };

                // 3. Gọi BLL để lưu xuống Database
                bool isSuccess = await _phongVanService.AddPhongVanAsync(phongVanMoi);

                if (isSuccess)
                {
                    MessageBox.Show("Thêm lịch phỏng vấn thành công!", "Thông báo");

                    // Dòng này cực kỳ quan trọng: Báo cho Form Main biết là đã thành công để nó tải lại bảng
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void frmThemPhongVan_LoadAsync(object sender, EventArgs e)
        {
            try
            {
                // === 1. TẠO DỮ LIỆU ỨNG VIÊN GIẢ (MOCK DATA) ===
                // Sửa số 1, số 2 thành đúng cái ID bạn đang có dưới Database
                var lstUngVienAo = new List<dynamic>
        {
            new { MaUngVien = "--- Chọn mã ứng viên ---" },
            new { MaUngVien = 4 },

        };

                cbMaUngVien.DataSource = lstUngVienAo;
                cbMaUngVien.DisplayMember = "MaUngVien";
                cbMaUngVien.ValueMember = "MaUngVien";
                cbMaUngVien.SelectedIndexChanged += CbMaUngVien_SelectedIndexChanged;

                // === 2. TẢI DANH SÁCH NGƯỜI PHỎNG VẤN NHƯ BÌNH THƯỜNG ===
                var lstTatCaNhanVien = await _nhanVienService.GetAllAsync();
                var lstNhanSu = lstTatCaNhanVien
                    .Where(nv => nv.TenPhongBan != null && nv.TenPhongBan.Contains("Nhân sự"))
                    .ToList();

                lstNhanSu.Insert(0, new HRM.Domain.Entities.NhanVienDTO
                {
                    MaNhanVien = 0,
                    HoTen = "--- Chọn người phỏng vấn ---"
                });

                cbNguoiPV.DataSource = lstNhanSu;
                cbNguoiPV.DisplayMember = "HoTen";
                //cboNguoiPV.ValueMember = "MaNhanVien";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải ComboBox: " + ex.Message);
            }

        }

        private void CbMaUngVien_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbMaUngVien.SelectedItem != null)
            {
                // 1. DÙNG CHO HIỆN TẠI (Dữ liệu ảo kiểu dynamic):
                dynamic selectedItem = cbMaUngVien.SelectedItem;
                lblTenUngVien.Text = selectedItem.HoTen;

                // ==========================================
                // 2. DÙNG CHO TƯƠNG LAI (Khi có Service thật):
                // Khi bạn làm xong module Ứng Viên, hãy xóa 2 dòng trên và mở khóa 2 dòng dưới này nhé:
                // var ungVien = (HRM.Domain.Entities.UngVien)cbMaUngVien.SelectedItem;
                // lblTenUngVien.Text = "👤 Tên: " + ungVien.HoTen;
            }
            else
            {
                // Nếu không chọn gì thì xóa trắng label
                lblTenUngVien.Text = "";
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }
    }
}
