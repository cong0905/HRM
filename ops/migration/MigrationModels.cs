using System.Text.Json.Serialization;

namespace HRM.Ops.Migration;

public enum IssueSeverity { Warning, Error }

public sealed record ValidationIssue(
    string Table,
    object? RecordId,
    IssueSeverity Severity,
    string RuleCode,
    string Message,
    string? Details = null
);

public sealed class DryRunReport
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string LegacyDatabase { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public Dictionary<string, int> SourceTableCounts { get; set; } = [];
    public Dictionary<string, int> ValidTableCounts { get; set; } = [];
    public Dictionary<string, int> ExcludedTableCounts { get; set; } = [];
    public List<ValidationIssue> Issues { get; set; } = [];
    public bool CanProceed => !Issues.Any(i => i.Severity == IssueSeverity.Error);
}

public sealed class MigrationMappingLedger
{
    public Dictionary<int, Guid> Departments { get; set; } = [];
    public Dictionary<int, Guid> Positions { get; set; } = [];
    public Dictionary<int, Guid> Employees { get; set; } = [];
    public Dictionary<int, Guid> Accounts { get; set; } = [];
    public Dictionary<int, Guid> Attendance { get; set; } = [];
    public Dictionary<int, Guid> LeaveRecords { get; set; } = [];
    public Dictionary<string, Guid> PayrollPeriods { get; set; } = [];
    public Dictionary<int, Guid> Payslips { get; set; } = [];
}

public sealed record TableReconciliation(
    string TableName,
    int LegacyCount,
    int NextCount,
    int ExcludedCount,
    int Discrepancy,
    bool Matched
);

public sealed record PayrollPeriodReconciliation(
    string PeriodCode,
    int LegacyRecordCount,
    int NextRecordCount,
    decimal LegacyNetTotal,
    decimal NextNetTotal,
    decimal NetDiscrepancy,
    decimal Tolerance,
    bool Matched
);

public sealed record LeaveBalanceReconciliation(
    int TotalEmployeesWithBalance,
    decimal TotalAllocatedDaysLegacy,
    decimal TotalAllocatedDaysNext,
    decimal TotalUsedDaysLegacy,
    decimal TotalUsedDaysNext,
    bool Matched
);

public sealed class ReconciliationReport
{
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SourceDatabase { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public List<TableReconciliation> Tables { get; set; } = [];
    public List<PayrollPeriodReconciliation> PayrollPeriods { get; set; } = [];
    public LeaveBalanceReconciliation? LeaveBalances { get; set; }
    public List<string> ExcludedLegacyIdsWithReason { get; set; } = [];
    public bool Passed => Tables.All(t => t.Matched) && PayrollPeriods.All(p => p.Matched) && (LeaveBalances?.Matched ?? true);
}

// Raw legacy models for strong-typed reading
public sealed record LegacyDepartment(
    int MaPhongBan,
    string TenPhongBan,
    string? MoTaChucNang,
    DateTime? NgayThanhLap,
    string? DiaDiemLamViec,
    decimal? NganSach,
    string TrangThai,
    int? MaTruongPhong,
    DateTime? NgayTao
);

public sealed record LegacyPosition(
    int MaChucVu,
    string TenChucVu,
    string? MoTa,
    int CapBac
);

public sealed record LegacyEmployee(
    int MaNhanVien,
    string? MaNV,
    string HoTen,
    DateTime NgaySinh,
    string? GioiTinh,
    string? CCCD,
    string? DiaChi,
    string? SoDienThoai,
    string? Email,
    string? TinhTrangHonNhan,
    int? MaPhongBan,
    int? MaChucVu,
    DateTime NgayVaoLam,
    decimal MucLuong,
    string TrangThai,
    DateTime? NgayNghiViec,
    DateTime? NgayTao
);

public sealed record LegacyAccount(
    int MaTaiKhoan,
    int MaNhanVien,
    string TenDangNhap,
    string MatKhauHash,
    string VaiTro,
    string TrangThai,
    DateTime? LanDangNhapCuoi,
    DateTime? NgayTao
);

public sealed record LegacyAttendance(
    int MaChamCong,
    int MaNhanVien,
    DateTime NgayChamCong,
    TimeSpan? GioVao,
    TimeSpan? GioRa,
    decimal? TongGioLam,
    decimal? GioLamThem,
    string? HinhThuc,
    string? TrangThai,
    string? GhiChu
);

public sealed record LegacyLeaveRequest(
    int MaDonPhep,
    int MaNhanVien,
    int MaLoaiPhep,
    DateTime NgayBatDau,
    DateTime NgayKetThuc,
    decimal SoNgayNghi,
    string LyDo,
    string TrangThai,
    int? NguoiPheDuyet,
    DateTime? NgayPheDuyet,
    string? LyDoTuChoi,
    DateTime? NgayTao
);

public sealed record LegacyLeaveBalance(
    int MaNhanVien,
    int Nam,
    int TongSoNgayPhep,
    int SoNgayDaSuDung,
    int PhepNamCuConLai
);

public sealed record LegacyPayroll(
    int MaBangLuong,
    int MaNhanVien,
    int Thang,
    int Nam,
    decimal LuongCoBan,
    decimal TongPhuCap,
    int SoNgayLamViec,
    decimal SoGioLamThem,
    decimal TienLamThem,
    decimal TongThuong,
    decimal TongPhat,
    decimal BHXH,
    decimal BHYT,
    decimal BHTN,
    decimal ThueTNCN,
    decimal? TongThuNhap,
    decimal? TongKhauTru,
    decimal? LuongThucNhan,
    DateTime? NgayTinhLuong,
    string TrangThai
);
