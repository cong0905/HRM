using System;
using System.Windows.Forms;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.GUI.Forms.Main
{
    public partial class frmSuaNhanVien : Form
    {
        private readonly INhanVienService _nhanVienService;
        private readonly IPhongBanService _phongBanService;
        private readonly IRepository<ChucVu> _chucVuRepo;
        private readonly int _maNV;
        private readonly NhanVienDTO _nhanVien;

        public frmSuaNhanVien(INhanVienService nhanVienService, IPhongBanService phongBanService, IRepository<ChucVu> chucVuRepo, NhanVienDTO nhanVien)
        {
            _nhanVienService = nhanVienService;
            _phongBanService = phongBanService;
            _chucVuRepo = chucVuRepo;
            _maNV = nhanVien.MaNhanVien;
            _nhanVien = nhanVien;
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // Load Phòng ban
                var phongBans = await _phongBanService.GetAllAsync();
                cboPhongBan.DataSource = phongBans;
                cboPhongBan.DisplayMember = "TenPhongBan";
                cboPhongBan.ValueMember = "MaPhongBan";

                // Load Chức vụ
                var chucVus = await _chucVuRepo.GetAllAsync();
                cboChucVu.DataSource = chucVus;
                cboChucVu.DisplayMember = "TenChucVu";
                cboChucVu.ValueMember = "MaChucVu";

                // Đổ dữ liệu cũ
                txtHoTen.Text = _nhanVien.HoTen;
                dtpNgaySinh.Value = _nhanVien.NgaySinh < dtpNgaySinh.MinDate ? dtpNgaySinh.MinDate : _nhanVien.NgaySinh;
                cboGioiTinh.Text = _nhanVien.GioiTinh;
                txtCCCD.Text = _nhanVien.CCCD;
                txtSoDienThoai.Text = _nhanVien.SoDienThoai;
                txtEmail.Text = _nhanVien.Email;
                txtMucLuong.Text = _nhanVien.MucLuong.ToString("N0");
                dtpNgayVaoLam.Value = _nhanVien.NgayVaoLam < dtpNgayVaoLam.MinDate ? dtpNgayVaoLam.MinDate : _nhanVien.NgayVaoLam;
                cboTrangThai.Text = _nhanVien.TrangThai;

                // Chọn phòng ban hiện tại
                if (_nhanVien.MaPhongBan.HasValue)
                    cboPhongBan.SelectedValue = _nhanVien.MaPhongBan.Value;
                else
                    cboPhongBan.SelectedIndex = -1;

                // Chọn chức vụ hiện tại
                if (_nhanVien.MaChucVu.HasValue)
                    cboChucVu.SelectedValue = _nhanVien.MaChucVu.Value;
                else
                    cboChucVu.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

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
                    MaPhongBan = cboPhongBan.SelectedValue as int?,
                    MaChucVu = cboChucVu.SelectedValue as int?,
                    MucLuong = decimal.TryParse(txtMucLuong.Text.Trim().Replace(",", ""), out var luong) ? luong : 0,
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
