using HRM.BLL.Interfaces;

namespace HRM.GUI.Forms.Main
{
    public partial class frmThemPhongVan : Form
    {
        private readonly IPhongVanService _phongVanService;
        private readonly INhanVienService _nhanVienService;
        private readonly IUngVienService _ungVienService;
        public frmThemPhongVan(IPhongVanService phongVanService,
        INhanVienService nhanVienService,
        IUngVienService ungVienService)
        {

            InitializeComponent();
            _phongVanService = phongVanService;
            _nhanVienService = nhanVienService;
            _ungVienService = ungVienService;
            this.Load += frmThemPhongVan_LoadAsync;
            btnLuu.Click += BtnLuu_ClickAsync;

        }

        private async void BtnLuu_ClickAsync(object? sender, EventArgs e)
        {
            try
            {
                // 1. Kiểm tra xem người dùng đã chọn đủ chưa
                if (cbMaUngVien.SelectedValue == null || (int)cbMaUngVien.SelectedValue == 0 ||
                    cbNguoiPV.SelectedValue == null || (int)cbNguoiPV.SelectedValue == 0)
                {
                    MessageBox.Show("Vui lòng chọn Ứng viên và Người phỏng vấn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(cbVongPhongVan.Text, out int vongPV))
                {
                    MessageBox.Show("Vui lòng chọn hoặc nhập Vòng phỏng vấn hợp lệ (số nguyên)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. Gom dữ liệu để chuẩn bị lưu
                var phongVanMoi = new HRM.Domain.Entities.PhongVan
                {
                    MaUngVien = (int)cbMaUngVien.SelectedValue,
                    NguoiPhongVan = (int)cbNguoiPV.SelectedValue,

                    // Ép kiểu chữ từ combobox sang số nguyên
                    VongPhongVan = vongPV,

                    NgayPhongVan = dtpNgayPhongvan.Value, // Nhớ kiểm tra lại đúng tên công cụ trên form của bạn
                    DiaDiem = txtDiaDiem.Text,
                    TrangThai = cbTrangThai.Text,
                    KetQua = cbKetQua.Text,
                    NhanXet = txtNhanXet.Text
                    // Lưu ý: Nếu có ô KetQua thì thêm vào đây
                };

                // 3. Gọi BLL để lưu xuống Database
                bool isSuccess = await _phongVanService.AddPhongVanAsync(phongVanMoi);

                if (isSuccess)
                {
                    MessageBox.Show("Thêm lịch phỏng vấn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                var lstUngVien = await _ungVienService.GetAllUngVienAsync();
                var ungVienData = new List<dynamic> { new { MaUngVien = 0, HoTen = "--- Chọn ứng viên ---" } };
                ungVienData.AddRange(lstUngVien.Select(uv => new { uv.MaUngVien, uv.HoTen }));

                cbMaUngVien.DataSource = ungVienData;
                cbMaUngVien.DisplayMember = "HoTen";
                cbMaUngVien.ValueMember = "MaUngVien";
                cbMaUngVien.SelectedIndexChanged += CbMaUngVien_SelectedIndexChanged;

                cbVongPhongVan.Items.Clear();
                cbVongPhongVan.Items.AddRange(new object[] { "1", "2", "3" });
                cbVongPhongVan.SelectedIndex = 0;

                cbTrangThai.Items.Clear();
                cbTrangThai.Items.AddRange(new object[] { "Đã lên lịch", "Đã phỏng vấn", "Đã hủy" });
                cbTrangThai.SelectedIndex = 0;

                cbKetQua.Items.Clear();
                cbKetQua.Items.AddRange(new object[] { "", "Đạt", "Không đạt", "Chờ kết quả" });
                cbKetQua.SelectedIndex = 0;

                // === 2. TẢI DANH SÁCH NGƯỜI PHỎNG VẤN NHƯ BÌNH THƯỜNG ===
                var lstTatCaNhanVien = await _nhanVienService.GetAllAsync();
                var lstNhanSu = lstTatCaNhanVien
                    .Where(nv => nv.TenPhongBan != null && nv.TenPhongBan.Contains("Nhân sự"))
                    .ToList();

                lstNhanSu.Insert(0, new HRM.Common.DTOs.NhanVienDTO
                {
                    MaNhanVien = 0,
                    HoTen = "--- Chọn người phỏng vấn ---"
                });

                cbNguoiPV.DataSource = lstNhanSu;
                cbNguoiPV.DisplayMember = "HoTen";
                cbNguoiPV.ValueMember = "MaNhanVien";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải ComboBox: " + ex.Message);
            }

        }

        private async void CbMaUngVien_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbMaUngVien.SelectedValue == null || !int.TryParse(cbMaUngVien.SelectedValue.ToString(), out int maUngVien) || maUngVien == 0)
            {
                cbVongPhongVan.Items.Clear();
                cbVongPhongVan.Items.AddRange(new object[] { "1", "2", "3" });
                if (cbVongPhongVan.Items.Count > 0) cbVongPhongVan.SelectedIndex = 0;
                cbVongPhongVan.Enabled = true;
                btnLuu.Enabled = true;
                return;
            }

            try
            {
                var tatCaLich = await _phongVanService.GetAllAsync();
                var lichCuaUngVien = tatCaLich.Where(x => x.MaUngVien == maUngVien).ToList();

                int maxRound = 0;
                foreach (var lich in lichCuaUngVien)
                {
                    if (int.TryParse(lich.VongPhongVan, out int round))
                    {
                        if (round > maxRound) maxRound = round;
                    }
                }

                cbVongPhongVan.Items.Clear();
                
                if (maxRound >= 3)
                {
                    cbVongPhongVan.Items.Add("Đã qua 3 vòng");
                    cbVongPhongVan.SelectedIndex = 0;
                    cbVongPhongVan.Enabled = false;
                    btnLuu.Enabled = false;
                }
                else
                {
                    for (int i = maxRound + 1; i <= 3; i++)
                    {
                        cbVongPhongVan.Items.Add(i.ToString());
                    }
                    if (cbVongPhongVan.Items.Count > 0) cbVongPhongVan.SelectedIndex = 0;
                    cbVongPhongVan.Enabled = true;
                    btnLuu.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy vòng phỏng vấn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void frmThemPhongVan_Load(object sender, EventArgs e)
        {

        }
    }
}
