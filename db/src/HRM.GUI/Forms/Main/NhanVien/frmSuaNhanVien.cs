using System;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Main
{
    public partial class frmSuaNhanVien : Form
    {
        private readonly INhanVienService _nhanVienService;
        private readonly int _maNV;

        public frmSuaNhanVien(INhanVienService nhanVienService, NhanVienDTO nhanVien)
        {
            _nhanVienService = nhanVienService;
            _maNV = nhanVien.MaNhanVien;
            InitializeComponent();

            // Đổ dữ liệu cũ
            txtHoTen.Text = nhanVien.HoTen;
            dtpNgaySinh.Value = nhanVien.NgaySinh < dtpNgaySinh.MinDate ? dtpNgaySinh.MinDate : nhanVien.NgaySinh;
            cboGioiTinh.Text = nhanVien.GioiTinh;
            txtCCCD.Text = nhanVien.CCCD;
            txtSoDienThoai.Text = nhanVien.SoDienThoai;
            txtEmail.Text = nhanVien.Email;
            dtpNgayVaoLam.Value = nhanVien.NgayVaoLam < dtpNgayVaoLam.MinDate ? dtpNgayVaoLam.MinDate : nhanVien.NgayVaoLam;
            cboTrangThai.Text = nhanVien.TrangThai;
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnLuu.Enabled = false;

                var dto = new NhanVienCreateDTO
                {
                    HoTen = txtHoTen.Text.Trim(),
                    NgaySinh = dtpNgaySinh.Value,
                    GioiTinh = cboGioiTinh.Text,
                    SoDienThoai = txtSoDienThoai.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    CCCD = txtCCCD.Text.Trim(),
                    NgayVaoLam = dtpNgayVaoLam.Value,
                    TrangThai = string.IsNullOrEmpty(cboTrangThai.Text) ? "Đang làm việc" : cboTrangThai.Text
                };

                await _nhanVienService.UpdateAsync(_maNV, dto);
                MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Lỗi: {error}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }

        private void frmSuaNhanVien_Load(object sender, EventArgs e)
        {

        }
    }
}
