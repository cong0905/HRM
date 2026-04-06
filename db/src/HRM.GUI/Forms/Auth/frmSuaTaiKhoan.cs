using System;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;

namespace HRM.GUI.Forms.Auth
{
    public partial class frmSuaTaiKhoan : Form
    {
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly TaiKhoanDTO _taiKhoan;

        public frmSuaTaiKhoan(ITaiKhoanService taiKhoanService, TaiKhoanDTO taiKhoan)
        {
            _taiKhoanService = taiKhoanService;
            _taiKhoan = taiKhoan;
            InitializeComponent();
            
            // Map dữ liệu cũ
            txtTenDangNhap.Text = _taiKhoan.TenDangNhap;
            cboVaiTro.Text = _taiKhoan.VaiTro;
            cboTrangThai.Text = _taiKhoan.TrangThai;
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                btnLuu.Enabled = false;

                var dto = new TaiKhoanUpdateDTO
                {
                    VaiTro = cboVaiTro.Text,
                    TrangThai = cboTrangThai.Text,
                    MatKhauMoi = string.IsNullOrWhiteSpace(txtMatKhauMoi.Text) ? null : txtMatKhauMoi.Text
                };

                await _taiKhoanService.UpdateAsync(_taiKhoan.MaTaiKhoan, dto);
                
                MessageBox.Show("Cập nhật tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Lỗi cập nhật: {error}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLuu.Enabled = true;
            }
        }
    }
}
