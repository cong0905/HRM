using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.Common.Helpers;
using HRM.DAL.Repositories;

namespace HRM.BLL.Services
{
    public class TaiKhoanService : ITaiKhoanService
    {
        private readonly ITaiKhoanRepository _taiKhoanRepo;
        private readonly INhanVienRepository _nhanVienRepo;

        public TaiKhoanService(ITaiKhoanRepository taiKhoanRepo, INhanVienRepository nhanVienRepo)
        {
            _taiKhoanRepo = taiKhoanRepo;
            _nhanVienRepo = nhanVienRepo;
        }

        public async Task<List<TaiKhoanDTO>> GetAllAsync()
        {
            var entities = await _taiKhoanRepo.GetAllAsync();

            // Tải tất cả nhân viên một lần (tránh N+1 query)
            var allNhanVien = await _nhanVienRepo.GetAllAsync();
            var nvDict = allNhanVien.ToDictionary(nv => nv.MaNhanVien, nv => nv.HoTen);

            return entities.Select(t => new TaiKhoanDTO
            {
                MaTaiKhoan = t.MaTaiKhoan,
                TenDangNhap = t.TenDangNhap,
                VaiTro = t.VaiTro,
                TrangThai = t.TrangThai,
                LanDangNhapCuoi = t.LanDangNhapCuoi,
                NgayTao = t.NgayTao,
                MaNhanVien = t.MaNhanVien,
                TenNhanVien = nvDict.TryGetValue(t.MaNhanVien, out var hoTen) ? hoTen : "Không xác định"
            }).ToList();
        }

        public async Task<List<TaiKhoanDTO>> SearchAsync(string? keyword, string? role = null, string? status = null)
        {
            var data = await GetAllAsync();
            var query = data.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(t => t.TenDangNhap.ToLower().Contains(kw) || 
                                         (t.TenNhanVien != null && t.TenNhanVien.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(role) && !role.Contains("Tất cả"))
            {
                query = query.Where(t => t.VaiTro == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Contains("Tất cả"))
            {
                query = query.Where(t => t.TrangThai == status);
            }

            return query.ToList();
        }

        public async Task CreateAsync(RegisterDTO dto)
        {
            var existing = await _taiKhoanRepo.FindAsync(t => t.TenDangNhap == dto.TenDangNhap);
            if (existing.Any()) throw new Exception("Tên đăng nhập đã tồn tại!");

            var tk = new Domain.Entities.TaiKhoan
            {
                MaNhanVien = dto.MaNhanVien,
                TenDangNhap = dto.TenDangNhap,
                MatKhauHash = PasswordHelper.HashPassword(dto.MatKhau),
                VaiTro = string.IsNullOrWhiteSpace(dto.VaiTro) ? "Nhân viên" : dto.VaiTro,
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };

            await _taiKhoanRepo.AddAsync(tk);
        }

        public async Task UpdateAsync(int id, TaiKhoanUpdateDTO dto)
        {
            var tk = await _taiKhoanRepo.GetByIdAsync(id);
            if (tk == null) throw new Exception("Không tìm thấy tài khoản!");

            tk.VaiTro = dto.VaiTro;
            tk.TrangThai = dto.TrangThai;

            if (!string.IsNullOrEmpty(dto.MatKhauMoi))
            {
                tk.MatKhauHash = PasswordHelper.HashPassword(dto.MatKhauMoi);
            }

            await _taiKhoanRepo.UpdateAsync(tk);
        }

        public async Task ChangePasswordAsync(int maTaiKhoan, string matKhauMoi)
        {
            var tk = await _taiKhoanRepo.GetByIdAsync(maTaiKhoan);
            if (tk == null) throw new Exception("Không tìm thấy tài khoản!");

            tk.MatKhauHash = PasswordHelper.HashPassword(matKhauMoi);
            await _taiKhoanRepo.UpdateAsync(tk);
        }

        public async Task DeleteAsync(int id)
        {
            var tk = await _taiKhoanRepo.GetByIdAsync(id);
            if (tk == null) throw new Exception("Không tìm thấy tài khoản!");

            await _taiKhoanRepo.DeleteAsync(tk);
        }
    }
}
