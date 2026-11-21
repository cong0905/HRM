using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Departments;
using HRM.Next.Domain.Leave;
using HRM.Next.Domain.Payroll;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRM.Ops.Migration;

public sealed class MigrationEngine(LegacyDataReader reader, HrmDbContext db)
{
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    public async Task<MigrationMappingLedger> ExecuteMigrationAsync(CancellationToken token = default)
    {
        var ledger = new MigrationMappingLedger();

        // 1. Read legacy data
        var departments = await reader.ReadDepartmentsAsync(token);
        var positions = await reader.ReadPositionsAsync(token);
        var employees = await reader.ReadEmployeesAsync(token);
        var accounts = await reader.ReadAccountsAsync(token);
        var attendances = await reader.ReadAttendanceAsync(token);
        var leaveRequests = await reader.ReadLeaveRequestsAsync(token);
        var leaveBalances = await reader.ReadLeaveBalancesAsync(token);
        var payrolls = await reader.ReadPayrollAsync(token);

        // Pre-validate to skip corrupt records
        var validator = new MigrationValidator(reader);
        var dryRun = await validator.ValidateAsync("LegacySource", "TargetNext", token);
        if (!dryRun.CanProceed)
        {
            throw new InvalidOperationException($"Cannot execute migration: {dryRun.Issues.Count(i => i.Severity == IssueSeverity.Error)} validation errors found in legacy data. Run dry-run for details.");
        }

        // Ensure roles exist in AspNetRoles
        await EnsureRolesExistAsync(token);

        // Ensure baseline WorkCalendarVersion exists
        var calendarVersion = await EnsureCalendarVersionExistsAsync(token);

        // 2. Migrate Departments
        foreach (var dept in departments)
        {
            var deptId = DeterministicGuid.Create(DeterministicGuid.DepartmentNamespace, $"Dept_{dept.MaPhongBan}");
            ledger.Departments[dept.MaPhongBan] = deptId;

            var existing = await db.Departments.FindAsync([deptId], token);
            if (existing is null)
            {
                var createdAt = dept.NgayTao.HasValue
                    ? new DateTimeOffset(dept.NgayTao.Value, VnOffset).ToUniversalTime()
                    : DateTimeOffset.UtcNow;

                db.Departments.Add(CreateDepartment(deptId, dept.TenPhongBan.Trim(), createdAt));
            }
        }
        await db.SaveChangesAsync(token);

        // 3. Migrate Positions
        foreach (var pos in positions)
        {
            var posId = DeterministicGuid.Create(DeterministicGuid.PositionNamespace, $"Pos_{pos.MaChucVu}");
            ledger.Positions[pos.MaChucVu] = posId;

            var existing = await db.Positions.FindAsync([posId], token);
            if (existing is null)
            {
                db.Positions.Add(new Position
                {
                    Id = posId,
                    Name = pos.TenChucVu.Trim(),
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(token);

        // 4. Migrate Accounts & Employees
        var accountByEmp = accounts.ToDictionary(a => a.MaNhanVien);

        foreach (var emp in employees)
        {
            var empId = DeterministicGuid.Create(DeterministicGuid.EmployeeNamespace, $"Emp_{emp.MaNhanVien}");
            var userId = DeterministicGuid.Create(DeterministicGuid.UserNamespace, $"User_{emp.MaNhanVien}");
            ledger.Employees[emp.MaNhanVien] = empId;
            ledger.Accounts[emp.MaNhanVien] = userId;

            // 4a. Identity User
            var existingUser = await db.Users.FindAsync([userId], token);
            if (existingUser is null)
            {
                var username = accountByEmp.TryGetValue(emp.MaNhanVien, out var acc)
                    ? acc.TenDangNhap.Trim()
                    : (emp.Email?.Split('@')[0] ?? $"emp_{emp.MaNhanVien}");

                var email = !string.IsNullOrWhiteSpace(emp.Email) ? emp.Email.Trim() : $"{username}@example.com";

                // IMPORTANT: Inactive imported account has PasswordHash = null, IsActive = false
                // No default password is set (safe account hash handling)
                var user = new HrmUser
                {
                    Id = userId,
                    UserName = username,
                    NormalizedUserName = username.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    IsActive = false, // Unactivated until user goes through activation invitation
                    PasswordHash = null, // Strictly null: no BCrypt transfer, no default password
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };
                db.Users.Add(user);

                // Role mapping
                var roleName = MapLegacyRole(acc?.VaiTro);
                var role = await db.Roles.SingleAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), token);
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });

                // Activation outbox message
                db.ActivationOutboxMessages.Add(new ActivationOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Email = email,
                    Purpose = "activation",
                    Status = "Pending",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            // 4b. Employee Profile
            var existingProfile = await db.EmployeeProfiles.FindAsync([empId], token);
            if (existingProfile is null)
            {
                var createdAt = emp.NgayTao.HasValue
                    ? new DateTimeOffset(emp.NgayTao.Value, VnOffset).ToUniversalTime()
                    : DateTimeOffset.UtcNow;

                var empCode = !string.IsNullOrWhiteSpace(emp.MaNV)
                    ? emp.MaNV.Trim()
                    : $"NV{emp.NgayVaoLam.Year:D4}{emp.MaNhanVien:D4}";

                var deptId = emp.MaPhongBan.HasValue && ledger.Departments.TryGetValue(emp.MaPhongBan.Value, out var dId) ? dId : (Guid?)null;
                var posId = emp.MaChucVu.HasValue && ledger.Positions.TryGetValue(emp.MaChucVu.Value, out var pId) ? pId : (Guid?)null;

                var profile = new EmployeeProfile
                {
                    Id = empId,
                    UserId = userId,
                    EmployeeCode = empCode,
                    FullName = emp.HoTen.Trim(),
                    CitizenId = emp.CCCD?.Trim() ?? $"CCCD_{emp.MaNhanVien:D6}",
                    Email = emp.Email?.Trim() ?? $"{empCode.ToLowerInvariant()}@example.com",
                    Phone = !string.IsNullOrWhiteSpace(emp.SoDienThoai) ? emp.SoDienThoai.Trim() : "0900000000",
                    Address = !string.IsNullOrWhiteSpace(emp.DiaChi) ? emp.DiaChi.Trim() : "Hà Nội",
                    DateOfBirth = DateOnly.FromDateTime(emp.NgaySinh),
                    Gender = emp.GioiTinh,
                    MaritalStatus = emp.TinhTrangHonNhan,
                    DepartmentId = deptId,
                    PositionId = posId,
                    HireDate = DateOnly.FromDateTime(emp.NgayVaoLam),
                    TerminationDate = emp.NgayNghiViec.HasValue ? DateOnly.FromDateTime(emp.NgayNghiViec.Value) : null,
                    Status = MapLegacyEmployeeStatus(emp.TrangThai),
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                };
                db.EmployeeProfiles.Add(profile);

                // Initial SalaryHistoryEntry
                var salaryId = DeterministicGuid.Create(DeterministicGuid.SalaryHistoryNamespace, $"Salary_{emp.MaNhanVien}");
                db.SalaryHistoryEntries.Add(new SalaryHistoryEntry
                {
                    Id = salaryId,
                    EmployeeId = empId,
                    MonthlyBase = emp.MucLuong,
                    EffectiveFrom = DateOnly.FromDateTime(emp.NgayVaoLam),
                    EffectiveTo = null,
                    Reason = "Chuyển đổi dữ liệu từ hệ thống legacy",
                    CreatedBy = userId,
                    CreatedAtUtc = createdAt
                });
            }
        }
        await db.SaveChangesAsync(token);

        // 5. Migrate Attendance
        foreach (var att in attendances)
        {
            if (!ledger.Employees.TryGetValue(att.MaNhanVien, out var empId)) continue;

            var attId = DeterministicGuid.Create(DeterministicGuid.AttendanceNamespace, $"Att_{att.MaChamCong}");
            ledger.Attendance[att.MaChamCong] = attId;

            var existing = await db.AttendanceSummaries.FindAsync([attId], token);
            if (existing is null)
            {
                var bizDate = DateOnly.FromDateTime(att.NgayChamCong);
                var checkInLocal = att.GioVao.HasValue ? att.NgayChamCong.Date.Add(att.GioVao.Value) : att.NgayChamCong.Date.AddHours(8).AddMinutes(30);
                var checkInUtc = new DateTimeOffset(checkInLocal, VnOffset).ToUniversalTime();

                DateTimeOffset? checkOutUtc = null;
                if (att.GioRa.HasValue)
                {
                    var checkOutLocal = att.NgayChamCong.Date.Add(att.GioRa.Value);
                    checkOutUtc = new DateTimeOffset(checkOutLocal, VnOffset).ToUniversalTime();
                }

                decimal eligibleMinutes = (att.TongGioLam ?? 8.0m) * 60m;
                decimal lateMinutes = (att.TrangThai?.Contains("Đi muộn") == true) ? 15m : 0m;
                decimal earlyLeaveMinutes = (att.TrangThai?.Contains("Về sớm") == true) ? 15m : 0m;

                var summary = new AttendanceSummary
                {
                    Id = attId,
                    EmployeeId = empId,
                    BusinessDate = bizDate,
                    CheckInUtc = checkInUtc,
                    CheckOutUtc = checkOutUtc,
                    CalendarVersionId = calendarVersion.Id,
                    CalendarDefinitionJson = calendarVersion.DefinitionJson,
                    EligibleMinutes = eligibleMinutes,
                    LateMinutes = lateMinutes,
                    EarlyLeaveMinutes = earlyLeaveMinutes,
                    Phase = checkOutUtc.HasValue ? "Closed" : "Open",
                    UpdatedAtUtc = checkOutUtc ?? checkInUtc
                };
                db.AttendanceSummaries.Add(summary);

                // Append audit events
                var inEventId = DeterministicGuid.Create(DeterministicGuid.AttendanceNamespace, $"Event_In_{att.MaChamCong}");
                db.AttendanceEvents.Add(new AttendanceEvent
                {
                    Id = inEventId,
                    AttendanceId = attId,
                    ActorId = ledger.Accounts[att.MaNhanVien],
                    Kind = "CheckIn",
                    OccurredAtUtc = checkInUtc,
                    EffectiveCheckInUtc = checkInUtc,
                    EffectiveCheckOutUtc = null
                });

                if (checkOutUtc.HasValue)
                {
                    var outEventId = DeterministicGuid.Create(DeterministicGuid.AttendanceNamespace, $"Event_Out_{att.MaChamCong}");
                    db.AttendanceEvents.Add(new AttendanceEvent
                    {
                        Id = outEventId,
                        AttendanceId = attId,
                        ActorId = ledger.Accounts[att.MaNhanVien],
                        Kind = "CheckOut",
                        OccurredAtUtc = checkOutUtc.Value,
                        EffectiveCheckInUtc = checkInUtc,
                        EffectiveCheckOutUtc = checkOutUtc
                    });
                }
            }
        }
        await db.SaveChangesAsync(token);

        // 6. Migrate Leave Balances & Requests
        foreach (var bal in leaveBalances)
        {
            if (!ledger.Employees.TryGetValue(bal.MaNhanVien, out var empId)) continue;

            var balId = DeterministicGuid.Create(DeterministicGuid.LeaveBalanceNamespace, $"LeaveBal_{bal.MaNhanVien}_{bal.Nam}");
            var existing = await db.LeaveBalances.FindAsync([balId], token);
            if (existing is null)
            {
                db.LeaveBalances.Add(new LeaveBalance
                {
                    Id = balId,
                    EmployeeId = empId,
                    Year = bal.Nam,
                    AllocatedDays = bal.TongSoNgayPhep,
                    UsedDays = bal.SoNgayDaSuDung,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                });

                // Initial grant ledger entry
                var ledgerId = DeterministicGuid.Create(DeterministicGuid.LeaveLedgerNamespace, $"Ledger_Grant_{bal.MaNhanVien}_{bal.Nam}");
                db.LeaveLedgerEntries.Add(new LeaveLedgerEntry
                {
                    Id = ledgerId,
                    EmployeeId = empId,
                    Year = bal.Nam,
                    Action = "Grant",
                    Days = bal.TongSoNgayPhep,
                    BalanceBefore = 0m,
                    BalanceAfter = bal.TongSoNgayPhep,
                    Reason = $"Quỹ phép ban đầu năm {bal.Nam} (Legacy)",
                    ActorId = ledger.Accounts[bal.MaNhanVien],
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }
        await db.SaveChangesAsync(token);

        foreach (var req in leaveRequests)
        {
            if (!ledger.Employees.TryGetValue(req.MaNhanVien, out var empId)) continue;

            var reqId = DeterministicGuid.Create(DeterministicGuid.LeaveNamespace, $"LeaveReq_{req.MaDonPhep}");
            ledger.LeaveRecords[req.MaDonPhep] = reqId;

            var existing = await db.LeaveRecords.FindAsync([reqId], token);
            if (existing is null)
            {
                Guid? approverEmpId = req.NguoiPheDuyet.HasValue && ledger.Employees.TryGetValue(req.NguoiPheDuyet.Value, out var apId)
                    ? apId : null;

                DateTimeOffset? decidedAtUtc = req.NgayPheDuyet.HasValue
                    ? new DateTimeOffset(req.NgayPheDuyet.Value, VnOffset).ToUniversalTime()
                    : null;

                var createdAt = req.NgayTao.HasValue
                    ? new DateTimeOffset(req.NgayTao.Value, VnOffset).ToUniversalTime()
                    : DateTimeOffset.UtcNow;

                db.LeaveRecords.Add(new LeaveRecord
                {
                    Id = reqId,
                    EmployeeId = empId,
                    StartDate = DateOnly.FromDateTime(req.NgayBatDau),
                    EndDate = DateOnly.FromDateTime(req.NgayKetThuc),
                    WorkingDays = req.SoNgayNghi,
                    Reason = req.LyDo.Trim(),
                    Status = MapLegacyLeaveStatus(req.TrangThai),
                    ApproverId = approverEmpId,
                    DecidedAtUtc = decidedAtUtc,
                    DecisionReason = req.LyDoTuChoi,
                    CreatedAtUtc = createdAt
                });
            }
        }
        await db.SaveChangesAsync(token);

        // 7. Migrate Payroll & Snapshots (Preserve history without recomputing)
        var payrollGroups = payrolls.GroupBy(p => $"{p.Nam:D4}-{p.Thang:D2}");

        foreach (var group in payrollGroups)
        {
            var periodCode = group.Key;
            var parts = periodCode.Split('-');
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);

            var periodId = DeterministicGuid.Create(DeterministicGuid.PayrollPeriodNamespace, $"Period_{periodCode}");
            ledger.PayrollPeriods[periodCode] = periodId;

            var batchId = DeterministicGuid.Create(DeterministicGuid.PayrollBatchNamespace, $"Batch_{periodCode}_v1");

            var existingPeriod = await db.PayrollPeriods.FindAsync([periodId], token);
            if (existingPeriod is null)
            {
                var periodCreatedAt = new DateTimeOffset(new DateTime(year, month, 1, 0, 0, 0), VnOffset).ToUniversalTime();
                var periodClosedAt = new DateTimeOffset(new DateTime(year, month, DateTime.DaysInMonth(year, month), 18, 0, 0), VnOffset).ToUniversalTime();

                // Step 1: Insert Period in Draft state without batches to prevent EF Core circular dependency cycle
                var period = new PayrollPeriodRecord
                {
                    Id = periodId,
                    PeriodCode = periodCode,
                    Status = "Draft",
                    CreatedAtUtc = periodCreatedAt
                };
                db.PayrollPeriods.Add(period);
                await db.SaveChangesAsync(token);

                // Step 2: Insert Batch, Snapshots, and Payslips referencing the committed Period
                var adminUserId = ledger.Accounts.Values.FirstOrDefault();
                if (adminUserId == Guid.Empty)
                {
                    adminUserId = await db.Users.Select(u => u.Id).FirstOrDefaultAsync(token);
                }

                var batch = new PayrollCalculationBatch
                {
                    Id = batchId,
                    PeriodId = periodId,
                    Version = 1,
                    CapturedAtUtc = periodClosedAt,
                    CreatedBy = adminUserId,
                    InputHash = ComputeInputHash(group.ToList())
                };
                db.PayrollCalculationBatches.Add(batch);

                // Snapshots and Payslips for each employee in period
                foreach (var legacyPay in group)
                {
                    if (!ledger.Employees.TryGetValue(legacyPay.MaNhanVien, out var empId)) continue;

                    var snapId = DeterministicGuid.Create(DeterministicGuid.PayrollSnapshotNamespace, $"Snap_{legacyPay.MaBangLuong}");
                    var slipId = DeterministicGuid.Create(DeterministicGuid.PayslipNamespace, $"Slip_{legacyPay.MaBangLuong}");
                    ledger.Payslips[legacyPay.MaBangLuong] = slipId;

                    decimal net = legacyPay.LuongThucNhan ?? (legacyPay.LuongCoBan + legacyPay.TongPhuCap - legacyPay.BHXH - legacyPay.BHYT - legacyPay.BHTN - legacyPay.ThueTNCN);

                    var snapshot = new PayrollCalculationSnapshot
                    {
                        Id = snapId,
                        BatchId = batchId,
                        EmployeeId = empId,
                        Net = net,
                        CanPublish = true,
                        InputHash = ComputeSha256($"{legacyPay.MaBangLuong}:{net}"),
                        InputJson = JsonSerializer.Serialize(new
                        {
                            legacyPay.MaBangLuong,
                            legacyPay.MaNhanVien,
                            legacyPay.Thang,
                            legacyPay.Nam,
                            legacyPay.LuongCoBan,
                            legacyPay.TongPhuCap,
                            legacyPay.SoNgayLamViec,
                            legacyPay.SoGioLamThem,
                            legacyPay.TienLamThem,
                            legacyPay.TongThuong,
                            legacyPay.TongPhat,
                            legacyPay.BHXH,
                            legacyPay.BHYT,
                            legacyPay.BHTN,
                            legacyPay.ThueTNCN
                        }),
                        SourceJson = "LegacyMigrationSource",
                        OutputJson = JsonSerializer.Serialize(new
                        {
                            PolicyVersion = "LEGACY_PRE_MIGRATION",
                            StandardPay = legacyPay.LuongCoBan,
                            OvertimePay = legacyPay.TienLamThem,
                            Allowance = legacyPay.TongPhuCap,
                            Bonus = legacyPay.TongThuong,
                            Gross = legacyPay.TongThuNhap ?? (legacyPay.LuongCoBan + legacyPay.TongPhuCap + legacyPay.TienLamThem + legacyPay.TongThuong),
                            TotalDeductions = legacyPay.TongKhauTru ?? (legacyPay.BHXH + legacyPay.BHYT + legacyPay.BHTN + legacyPay.ThueTNCN + legacyPay.TongPhat),
                            Net = net
                        })
                    };
                    db.PayrollCalculationSnapshots.Add(snapshot);

                    var payslip = new PayslipRecord
                    {
                        Id = slipId,
                        PeriodId = periodId,
                        EmployeeId = empId,
                        SnapshotId = snapId,
                        Amount = net,
                        Status = "Locked",
                        PublishedAtUtc = periodClosedAt
                    };
                    db.Payslips.Add(payslip);
                }
                await db.SaveChangesAsync(token);

                // Step 3: Link active batch and seal period as Locked
                period.CalculatedBy = adminUserId;
                period.ApprovedBy = adminUserId;
                period.PublishedBy = adminUserId;
                period.LockedBy = adminUserId;
                period.CalculatedAtUtc = periodClosedAt;
                period.ApprovedAtUtc = periodClosedAt;
                period.PublishedAtUtc = periodClosedAt;
                period.LockedAtUtc = periodClosedAt;
                period.ActiveBatchId = batchId;
                period.ApprovedBatchId = batchId;
                period.PublishedBatchId = batchId;
                period.Status = "Locked";
                await db.SaveChangesAsync(token);
            }
        }
        await db.SaveChangesAsync(token);

        return ledger;
    }

    private static Department CreateDepartment(Guid id, string name, DateTimeOffset createdAt)
    {
        var dept = Department.Create(name, createdAt);
        typeof(Department).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(dept, id);
        return dept;
    }

    private async Task EnsureRolesExistAsync(CancellationToken token)
    {
        string[] roles = ["SystemAdmin", "HR", "Payroll", "Employee"];
        foreach (var role in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.NormalizedName == role.ToUpperInvariant(), token))
            {
                db.Roles.Add(new IdentityRole<Guid>
                {
                    Id = DeterministicGuid.Create(DeterministicGuid.UserNamespace, $"Role_{role}"),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString("D")
                });
            }
        }
        await db.SaveChangesAsync(token);
    }

    private async Task<WorkCalendarVersion> EnsureCalendarVersionExistsAsync(CancellationToken token)
    {
        var existing = await db.WorkCalendarVersions.OrderBy(v => v.EffectiveFrom).FirstOrDefaultAsync(token);
        if (existing is not null) return existing;

        var calDef = new WorkCalendarDefinition(
            "standard_office_v1",
            "Asia/Ho_Chi_Minh",
            new TimeOnly(8, 30),
            new TimeOnly(17, 30),
            [new WorkBreak(new TimeOnly(12, 0), new TimeOnly(13, 0))],
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            []
        );

        var calVersion = new WorkCalendarVersion
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            EffectiveFrom = new DateOnly(2020, 1, 1),
            DefinitionJson = JsonSerializer.Serialize(calDef),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.WorkCalendarVersions.Add(calVersion);
        await db.SaveChangesAsync(token);
        return calVersion;
    }

    private static string MapLegacyRole(string? legacyRole) => legacyRole switch
    {
        "Admin" => "SystemAdmin",
        "Phòng Nhân sự" or "HR" => "HR",
        "Quản lý" or "Manager" => "HR",
        "KeToan" => "Payroll",
        _ => "Employee"
    };

    private static string MapLegacyEmployeeStatus(string legacyStatus) => legacyStatus switch
    {
        "Đang làm việc" => "DangLamViec",
        "Nghỉ việc" => "DaNghiViec",
        "Nghỉ phép dài hạn" => "NghiPhepDaiHan",
        "Thử việc" => "ThuViec",
        _ => "DangLamViec"
    };

    private static string MapLegacyLeaveStatus(string legacyStatus) => legacyStatus switch
    {
        "Đã duyệt" => "Approved",
        "Từ chối" => "Rejected",
        "Đã hủy" => "Cancelled",
        _ => "Pending"
    };

    private static string ComputeInputHash(List<LegacyPayroll> payrolls)
    {
        var sb = new StringBuilder();
        foreach (var p in payrolls.OrderBy(p => p.MaBangLuong))
            sb.Append($"{p.MaBangLuong}:{p.MaNhanVien}:{p.LuongCoBan}:{p.LuongThucNhan};");
        return ComputeSha256(sb.ToString());
    }

    private static string ComputeSha256(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
