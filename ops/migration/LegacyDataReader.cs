using Microsoft.Data.SqlClient;

namespace HRM.Ops.Migration;

public sealed class LegacyDataReader(string connectionString)
{
    private SqlConnection CreateConnection() => new(connectionString);

    public async Task<List<LegacyDepartment>> ReadDepartmentsAsync(CancellationToken token = default)
    {
        var list = new List<LegacyDepartment>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaPhongBan, TenPhongBan, MoTaChucNang, NgayThanhLap, DiaDiemLamViec, NganSach, TrangThai, MaTruongPhong, NgayTao FROM dbo.PhongBan";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyDepartment(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            ));
        }
        return list;
    }

    public async Task<List<LegacyPosition>> ReadPositionsAsync(CancellationToken token = default)
    {
        var list = new List<LegacyPosition>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaChucVu, TenChucVu, MoTa, CapBac FROM dbo.ChucVu";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyPosition(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetInt32(3)
            ));
        }
        return list;
    }

    public async Task<List<LegacyEmployee>> ReadEmployeesAsync(CancellationToken token = default)
    {
        var list = new List<LegacyEmployee>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT MaNhanVien, MaNV, HoTen, NgaySinh, GioiTinh, CCCD, DiaChi, SoDienThoai, 
                                   Email, TinhTrangHonNhan, MaPhongBan, MaChucVu, NgayVaoLam, MucLuong, 
                                   TrangThai, NgayNghiViec, NgayTao 
                            FROM dbo.NhanVien";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyEmployee(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetInt32(10),
                reader.IsDBNull(11) ? null : reader.GetInt32(11),
                reader.GetDateTime(12),
                reader.GetDecimal(13),
                reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                reader.IsDBNull(16) ? null : reader.GetDateTime(16)
            ));
        }
        return list;
    }

    public async Task<List<LegacyAccount>> ReadAccountsAsync(CancellationToken token = default)
    {
        var list = new List<LegacyAccount>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai, LanDangNhapCuoi, NgayTao FROM dbo.TaiKhoan";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyAccount(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            ));
        }
        return list;
    }

    public async Task<List<LegacyAttendance>> ReadAttendanceAsync(CancellationToken token = default)
    {
        var list = new List<LegacyAttendance>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaChamCong, MaNhanVien, NgayChamCong, GioVao, GioRa, TongGioLam, GioLamThem, HinhThuc, TrangThai, GhiChu FROM dbo.ChamCong";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyAttendance(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDateTime(2),
                reader.IsDBNull(3) ? null : reader.GetTimeSpan(3),
                reader.IsDBNull(4) ? null : reader.GetTimeSpan(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)
            ));
        }
        return list;
    }

    public async Task<List<LegacyLeaveRequest>> ReadLeaveRequestsAsync(CancellationToken token = default)
    {
        var list = new List<LegacyLeaveRequest>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaDonPhep, MaNhanVien, MaLoaiPhep, NgayBatDau, NgayKetThuc, SoNgayNghi, LyDo, TrangThai, NguoiPheDuyet, NgayPheDuyet, LyDoTuChoi, NgayTao FROM dbo.DonNghiPhep";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyLeaveRequest(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetDateTime(3),
                reader.GetDateTime(4),
                reader.GetDecimal(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetDateTime(11)
            ));
        }
        return list;
    }

    public async Task<List<LegacyLeaveBalance>> ReadLeaveBalancesAsync(CancellationToken token = default)
    {
        var list = new List<LegacyLeaveBalance>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MaNhanVien, Nam, TongSoNgayPhep, SoNgayDaSuDung, PhepNamCuConLai FROM dbo.SoNgayPhep";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyLeaveBalance(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4)
            ));
        }
        return list;
    }

    public async Task<List<LegacyPayroll>> ReadPayrollAsync(CancellationToken token = default)
    {
        var list = new List<LegacyPayroll>();
        await using var conn = CreateConnection();
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT MaBangLuong, MaNhanVien, Thang, Nam, LuongCoBan, TongPhuCap, SoNgayLamViec,
                                   SoGioLamThem, TienLamThem, TongThuong, TongPhat, BHXH, BHYT, BHTN, ThueTNCN,
                                   TongThuNhap, TongKhauTru, LuongThucNhan, NgayTinhLuong, TrangThai
                            FROM dbo.BangLuong";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            list.Add(new LegacyPayroll(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetDecimal(4),
                reader.GetDecimal(5),
                reader.GetInt32(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9),
                reader.GetDecimal(10),
                reader.GetDecimal(11),
                reader.GetDecimal(12),
                reader.GetDecimal(13),
                reader.GetDecimal(14),
                reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                reader.IsDBNull(17) ? null : reader.GetDecimal(17),
                reader.IsDBNull(18) ? null : reader.GetDateTime(18),
                reader.GetString(19)
            ));
        }
        return list;
    }
}
