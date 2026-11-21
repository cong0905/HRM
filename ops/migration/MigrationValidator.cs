namespace HRM.Ops.Migration;

public sealed class MigrationValidator(LegacyDataReader reader)
{
    public async Task<DryRunReport> ValidateAsync(string legacyDbName, string targetDbName, CancellationToken token = default)
    {
        var report = new DryRunReport
        {
            LegacyDatabase = legacyDbName,
            TargetDatabase = targetDbName
        };

        var departments = await reader.ReadDepartmentsAsync(token);
        var positions = await reader.ReadPositionsAsync(token);
        var employees = await reader.ReadEmployeesAsync(token);
        var accounts = await reader.ReadAccountsAsync(token);
        var attendances = await reader.ReadAttendanceAsync(token);
        var leaveRequests = await reader.ReadLeaveRequestsAsync(token);
        var leaveBalances = await reader.ReadLeaveBalancesAsync(token);
        var payrolls = await reader.ReadPayrollAsync(token);

        report.SourceTableCounts["PhongBan"] = departments.Count;
        report.SourceTableCounts["ChucVu"] = positions.Count;
        report.SourceTableCounts["NhanVien"] = employees.Count;
        report.SourceTableCounts["TaiKhoan"] = accounts.Count;
        report.SourceTableCounts["ChamCong"] = attendances.Count;
        report.SourceTableCounts["DonNghiPhep"] = leaveRequests.Count;
        report.SourceTableCounts["SoNgayPhep"] = leaveBalances.Count;
        report.SourceTableCounts["BangLuong"] = payrolls.Count;

        var deptIds = departments.Select(d => d.MaPhongBan).ToHashSet();
        var posIds = positions.Select(p => p.MaChucVu).ToHashSet();

        // 1. Employee validations
        var employeeIds = new HashSet<int>();
        var seenCccd = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var validEmployees = new List<LegacyEmployee>();

        foreach (var emp in employees)
        {
            bool hasError = false;

            // CCCD duplicate check
            if (!string.IsNullOrWhiteSpace(emp.CCCD))
            {
                var cleanCccd = emp.CCCD.Trim();
                if (seenCccd.TryGetValue(cleanCccd, out var existingId))
                {
                    report.Issues.Add(new ValidationIssue(
                        "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "DUPLICATE_CCCD",
                        $"Trùng lặp CCCD '{cleanCccd}' giữa nhân viên {emp.MaNhanVien} và {existingId}."
                    ));
                    hasError = true;
                }
                else
                {
                    seenCccd[cleanCccd] = emp.MaNhanVien;
                }
            }

            // Email duplicate check
            if (!string.IsNullOrWhiteSpace(emp.Email))
            {
                var cleanEmail = emp.Email.Trim().ToLowerInvariant();
                if (seenEmails.TryGetValue(cleanEmail, out var existingId))
                {
                    report.Issues.Add(new ValidationIssue(
                        "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "DUPLICATE_EMAIL",
                        $"Trùng lặp Email '{cleanEmail}' giữa nhân viên {emp.MaNhanVien} và {existingId}."
                    ));
                    hasError = true;
                }
                else
                {
                    seenEmails[cleanEmail] = emp.MaNhanVien;
                }
            }

            // Department FK orphan check
            if (emp.MaPhongBan.HasValue && !deptIds.Contains(emp.MaPhongBan.Value))
            {
                report.Issues.Add(new ValidationIssue(
                    "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "ORPHAN_DEPARTMENT",
                    $"Nhân viên {emp.MaNhanVien} tham chiếu Phòng ban không tồn tại (MaPhongBan = {emp.MaPhongBan.Value})."
                ));
                hasError = true;
            }

            // Position FK orphan check
            if (emp.MaChucVu.HasValue && !posIds.Contains(emp.MaChucVu.Value))
            {
                report.Issues.Add(new ValidationIssue(
                    "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "ORPHAN_POSITION",
                    $"Nhân viên {emp.MaNhanVien} tham chiếu Chức vụ không tồn tại (MaChucVu = {emp.MaChucVu.Value})."
                ));
                hasError = true;
            }

            // Salary positive invariant
            if (emp.MucLuong <= 0)
            {
                report.Issues.Add(new ValidationIssue(
                    "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "INVALID_SALARY",
                    $"Mức lương nhân viên {emp.MaNhanVien} không hợp lệ (MucLuong = {emp.MucLuong} <= 0)."
                ));
                hasError = true;
            }

            // Dates check
            if (emp.NgayNghiViec.HasValue && emp.NgayNghiViec.Value < emp.NgayVaoLam)
            {
                report.Issues.Add(new ValidationIssue(
                    "NhanVien", emp.MaNhanVien, IssueSeverity.Error, "INVALID_DATES",
                    $"Ngày nghỉ việc ({emp.NgayNghiViec:yyyy-MM-dd}) nhỏ hơn ngày vào làm ({emp.NgayVaoLam:yyyy-MM-dd})."
                ));
                hasError = true;
            }

            if (!hasError)
            {
                validEmployees.Add(emp);
                employeeIds.Add(emp.MaNhanVien);
            }
        }

        report.ValidTableCounts["NhanVien"] = validEmployees.Count;
        report.ExcludedTableCounts["NhanVien"] = employees.Count - validEmployees.Count;

        // 2. Account validations
        var seenUsernames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var validAccounts = new List<LegacyAccount>();

        foreach (var acc in accounts)
        {
            bool hasError = false;
            var cleanUsername = acc.TenDangNhap.Trim();

            // Duplicate username (case-insensitive & trimmed)
            if (seenUsernames.TryGetValue(cleanUsername, out var existingId))
            {
                report.Issues.Add(new ValidationIssue(
                    "TaiKhoan", acc.MaTaiKhoan, IssueSeverity.Error, "DUPLICATE_USERNAME",
                    $"Tên đăng nhập '{cleanUsername}' bị trùng lặp giữa MaTaiKhoan {acc.MaTaiKhoan} và {existingId}."
                ));
                hasError = true;
            }
            else
            {
                seenUsernames[cleanUsername] = acc.MaTaiKhoan;
            }

            // Orphan employee check
            if (!employeeIds.Contains(acc.MaNhanVien))
            {
                report.Issues.Add(new ValidationIssue(
                    "TaiKhoan", acc.MaTaiKhoan, IssueSeverity.Error, "ORPHAN_EMPLOYEE_ACCOUNT",
                    $"Tài khoản {acc.MaTaiKhoan} tham chiếu nhân viên không tồn tại hoặc bị loại (MaNhanVien = {acc.MaNhanVien})."
                ));
                hasError = true;
            }

            if (!hasError) validAccounts.Add(acc);
        }

        report.ValidTableCounts["TaiKhoan"] = validAccounts.Count;
        report.ExcludedTableCounts["TaiKhoan"] = accounts.Count - validAccounts.Count;

        // 3. Attendance validations
        var validAttendances = new List<LegacyAttendance>();
        foreach (var att in attendances)
        {
            bool hasError = false;
            if (!employeeIds.Contains(att.MaNhanVien))
            {
                report.Issues.Add(new ValidationIssue(
                    "ChamCong", att.MaChamCong, IssueSeverity.Error, "ORPHAN_ATTENDANCE",
                    $"Bản ghi chấm công {att.MaChamCong} tham chiếu nhân viên không hợp lệ (MaNhanVien = {att.MaNhanVien})."
                ));
                hasError = true;
            }

            if (att.GioVao.HasValue && att.GioRa.HasValue && att.GioRa.Value < att.GioVao.Value)
            {
                report.Issues.Add(new ValidationIssue(
                    "ChamCong", att.MaChamCong, IssueSeverity.Error, "INVALID_ATTENDANCE_TIME",
                    $"Giờ ra ({att.GioRa}) nhỏ hơn giờ vào ({att.GioVao}) cho bản ghi {att.MaChamCong} (ca qua đêm không hỗ trợ trong MVP)."
                ));
                hasError = true;
            }

            if (!hasError) validAttendances.Add(att);
        }

        report.ValidTableCounts["ChamCong"] = validAttendances.Count;
        report.ExcludedTableCounts["ChamCong"] = attendances.Count - validAttendances.Count;

        // 4. Leave validations
        var validLeaves = new List<LegacyLeaveRequest>();
        foreach (var leave in leaveRequests)
        {
            bool hasError = false;
            if (!employeeIds.Contains(leave.MaNhanVien))
            {
                report.Issues.Add(new ValidationIssue(
                    "DonNghiPhep", leave.MaDonPhep, IssueSeverity.Error, "ORPHAN_LEAVE",
                    $"Đơn nghỉ phép {leave.MaDonPhep} tham chiếu nhân viên không hợp lệ (MaNhanVien = {leave.MaNhanVien})."
                ));
                hasError = true;
            }

            if (leave.NgayKetThuc < leave.NgayBatDau)
            {
                report.Issues.Add(new ValidationIssue(
                    "DonNghiPhep", leave.MaDonPhep, IssueSeverity.Error, "INVALID_LEAVE_DATES",
                    $"Ngày kết thúc ({leave.NgayKetThuc:yyyy-MM-dd}) nhỏ hơn ngày bắt đầu ({leave.NgayBatDau:yyyy-MM-dd}) ở đơn {leave.MaDonPhep}."
                ));
                hasError = true;
            }

            if (leave.NgayBatDau.Year != leave.NgayKetThuc.Year)
            {
                report.Issues.Add(new ValidationIssue(
                    "DonNghiPhep", leave.MaDonPhep, IssueSeverity.Error, "CROSS_YEAR_LEAVE",
                    $"Đơn nghỉ phép {leave.MaDonPhep} kéo dài qua năm ({leave.NgayBatDau.Year} -> {leave.NgayKetThuc.Year}) không hỗ trợ trong MVP."
                ));
                hasError = true;
            }

            if (!hasError) validLeaves.Add(leave);
        }

        report.ValidTableCounts["DonNghiPhep"] = validLeaves.Count;
        report.ExcludedTableCounts["DonNghiPhep"] = leaveRequests.Count - validLeaves.Count;

        // 5. Leave balance validations
        var validBalances = new List<LegacyLeaveBalance>();
        foreach (var bal in leaveBalances)
        {
            bool hasError = false;
            if (!employeeIds.Contains(bal.MaNhanVien))
            {
                report.Issues.Add(new ValidationIssue(
                    "SoNgayPhep", bal.MaNhanVien, IssueSeverity.Error, "ORPHAN_LEAVE_BALANCE",
                    $"Sổ ngày phép tham chiếu nhân viên không hợp lệ (MaNhanVien = {bal.MaNhanVien})."
                ));
                hasError = true;
            }

            if (bal.SoNgayDaSuDung < 0)
            {
                report.Issues.Add(new ValidationIssue(
                    "SoNgayPhep", bal.MaNhanVien, IssueSeverity.Error, "NEGATIVE_USED_DAYS",
                    $"Số ngày phép đã sử dụng ({bal.SoNgayDaSuDung}) âm cho nhân viên {bal.MaNhanVien}."
                ));
                hasError = true;
            }

            if (bal.SoNgayDaSuDung > bal.TongSoNgayPhep)
            {
                report.Issues.Add(new ValidationIssue(
                    "SoNgayPhep", bal.MaNhanVien, IssueSeverity.Error, "LEAVE_OVERDRAFT",
                    $"Số ngày phép đã dùng ({bal.SoNgayDaSuDung}) vượt quá tổng quỹ ({bal.TongSoNgayPhep}) cho nhân viên {bal.MaNhanVien}."
                ));
                hasError = true;
            }

            if (!hasError) validBalances.Add(bal);
        }

        report.ValidTableCounts["SoNgayPhep"] = validBalances.Count;
        report.ExcludedTableCounts["SoNgayPhep"] = leaveBalances.Count - validBalances.Count;

        // 6. Payroll validations
        var validPayrolls = new List<LegacyPayroll>();
        foreach (var pay in payrolls)
        {
            bool hasError = false;
            if (!employeeIds.Contains(pay.MaNhanVien))
            {
                report.Issues.Add(new ValidationIssue(
                    "BangLuong", pay.MaBangLuong, IssueSeverity.Error, "ORPHAN_PAYROLL",
                    $"Bảng lương {pay.MaBangLuong} tham chiếu nhân viên không hợp lệ (MaNhanVien = {pay.MaNhanVien})."
                ));
                hasError = true;
            }

            if (!hasError) validPayrolls.Add(pay);
        }

        report.ValidTableCounts["BangLuong"] = validPayrolls.Count;
        report.ExcludedTableCounts["BangLuong"] = payrolls.Count - validPayrolls.Count;

        report.ValidTableCounts["PhongBan"] = departments.Count;
        report.ExcludedTableCounts["PhongBan"] = 0;
        report.ValidTableCounts["ChucVu"] = positions.Count;
        report.ExcludedTableCounts["ChucVu"] = 0;

        return report;
    }
}
