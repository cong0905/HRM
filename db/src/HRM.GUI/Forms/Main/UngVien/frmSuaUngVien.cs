using HRM.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.GUI.Forms.Main.UngVien
{
    public partial class frmSuaUngVien : Form
    {
        private readonly IUngVienService _ungVienService;
        private readonly ITinTuyenDungService _tinTuyenDungService;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int MaUngVienCachSua { get; set; }
        private HRM.Domain.Entities.UngVien? _ungVienHienTai;
        public frmSuaUngVien()
        {
            InitializeComponent();
            _ungVienService = Program.ServiceProvider.GetRequiredService<IUngVienService>();
            _tinTuyenDungService = Program.ServiceProvider.GetRequiredService<ITinTuyenDungService>();
            this.Load += FrmSuaUngVien_Load;
        }

        private async void FrmSuaUngVien_Load(object? sender, EventArgs e)
        {
            try
            {
                var lstTin = await _tinTuyenDungService.GetAllAsync();
                cbVitriTuyenDung.DataSource = lstTin;
                cbVitriTuyenDung.DisplayMember = "ViTriTuyenDung";
                cbVitriTuyenDung.ValueMember = "MaTinTuyenDung";
                _ungVienHienTai = await _ungVienService.GetByIdAsync(MaUngVienCachSua);
                if (_ungVienHienTai == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin ứng viên cần sửa!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                txtTenUngVien.Text = _ungVienHienTai.HoTen;
                txtEmail.Text = _ungVienHienTai.Email;
                txtSoDienThoai.Text = _ungVienHienTai.SoDienThoai;
                txtDuongDanCV.Text = _ungVienHienTai.DuongDanCV;
                txtGhiChu.Text = _ungVienHienTai.GhiChu;
                cbVitriTuyenDung.SelectedValue = _ungVienHienTai.MaTinTuyenDung;
                cbPhanLoai.SelectedItem = _ungVienHienTai.PhanLoai;
                cbTrangThai.SelectedItem = _ungVienHienTai.TrangThai;
                if (_ungVienHienTai.NgayNop.Year > 2000)
                {
                    dateNgayNop.Value = _ungVienHienTai.NgayNop;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnLuu_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenUngVien.Text))
            {
                MessageBox.Show("Vui lòng nhập Tên ứng viên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenUngVien.Focus();
                return;
            }

            if (_ungVienHienTai == null)
            {
                MessageBox.Show("Không tìm thấy thông tin ứng viên cần sửa!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (cbVitriTuyenDung.SelectedValue is not int maTin || maTin <= 0)
            {
                MessageBox.Show("Vui lòng chọn Vị trí tuyển dụng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _ungVienHienTai.HoTen = txtTenUngVien.Text.Trim();
                _ungVienHienTai.Email = txtEmail.Text.Trim();
                _ungVienHienTai.SoDienThoai = txtSoDienThoai.Text.Trim();
                _ungVienHienTai.DuongDanCV = txtDuongDanCV.Text.Trim();
                _ungVienHienTai.GhiChu = txtGhiChu.Text.Trim();

                _ungVienHienTai.MaTinTuyenDung = maTin;
                _ungVienHienTai.PhanLoai = cbPhanLoai.SelectedItem?.ToString() ?? _ungVienHienTai.PhanLoai;
                _ungVienHienTai.TrangThai = cbTrangThai.SelectedItem?.ToString() ?? _ungVienHienTai.TrangThai;
                _ungVienHienTai.NgayNop = dateNgayNop.Value;

                await _ungVienService.UpdateUngVienAsync(_ungVienHienTai);
                MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra khi cập nhật: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

