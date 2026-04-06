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
            var result = new List<TaiKhoanDTO>();
            
            // Map từ từ với thông tin Nhân viên
            foreach (var t in entities)
            {
                var nv = await _nhanVienRepo.GetByIdAsync(t.MaNhanVien);
                result.Add(new TaiKhoanDTO
                {
                    MaTaiKhoan = t.MaTaiKhoan,
                    TenDangNhap = t.TenDangNhap,
                    VaiTro = t.VaiTro,
                    TrangThai = t.TrangThai,
                    LanDangNhapCuoi = t.LanDangNhapCuoi,
                    NgayTao = t.NgayTao,
                    MaNhanVien = t.MaNhanVien,
                    TenNhanVien = nv?.HoTen ?? "Không xác định"
                });
            }
            return result;
        }

        public async Task<List<TaiKhoanDTO>> SearchAsync(string keyword)
        {
            var all = await GetAllAsync();
            keyword = keyword.ToLower();
            return all.Where(t => t.TenDangNhap.ToLower().Contains(keyword) || 
                                  (t.TenNhanVien != null && t.TenNhanVien.ToLower().Contains(keyword))).ToList();
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

        public async Task DeleteAsync(int id)
        {
            var tk = await _taiKhoanRepo.GetByIdAsync(id);
            if (tk == null) throw new Exception("Không tìm thấy tài khoản!");

            await _taiKhoanRepo.DeleteAsync(tk);
        }
    }
}
