using HRM.Next.Domain;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Departments;
using HRM.Next.Domain.Leave;
using HRM.Next.Domain.Notifications;
using HRM.Next.Domain.Payroll;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.Next.Infrastructure.Persistence;

public sealed class HrmDbContext(DbContextOptions<HrmDbContext> options) : IdentityDbContext<HrmUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<AttendanceSummary> AttendanceSummaries => Set<AttendanceSummary>();
    public DbSet<AttendanceEvent> AttendanceEvents => Set<AttendanceEvent>();
    public DbSet<AttendanceAuditLog> AttendanceAuditLogs => Set<AttendanceAuditLog>();
    public DbSet<AttendanceOvertimeEntry> AttendanceOvertimeEntries => Set<AttendanceOvertimeEntry>();
    public DbSet<AttendanceIdempotencyRecord> AttendanceIdempotencyRecords => Set<AttendanceIdempotencyRecord>();
    public DbSet<WorkCalendarVersion> WorkCalendarVersions => Set<WorkCalendarVersion>();
    public DbSet<CalendarPeriodSnapshot> CalendarPeriodSnapshots => Set<CalendarPeriodSnapshot>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<ActivationOutboxMessage> ActivationOutboxMessages => Set<ActivationOutboxMessage>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<HrEmployeeAssignment> HrEmployeeAssignments => Set<HrEmployeeAssignment>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveLedgerEntry> LeaveLedgerEntries => Set<LeaveLedgerEntry>();
    public DbSet<LeaveIdempotencyRecord> LeaveIdempotencyRecords => Set<LeaveIdempotencyRecord>();
    public DbSet<SalaryHistoryEntry> SalaryHistoryEntries => Set<SalaryHistoryEntry>();
    public DbSet<PayrollManualAdjustment> PayrollManualAdjustments => Set<PayrollManualAdjustment>();
    public DbSet<PayrollCalculationBatch> PayrollCalculationBatches => Set<PayrollCalculationBatch>();
    public DbSet<PayrollCalculationSnapshot> PayrollCalculationSnapshots => Set<PayrollCalculationSnapshot>();
    public DbSet<PayrollInputReceipt> PayrollInputReceipts => Set<PayrollInputReceipt>();
    public DbSet<PayrollPeriodRecord> PayrollPeriods => Set<PayrollPeriodRecord>();
    public DbSet<PayslipRecord> Payslips => Set<PayslipRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // The unique year key plus serializable key-range lock protects first insert and increments.
    // Called inside provisioning transactions; standalone fixture inserts may leave harmless code gaps.
    public async Task<string> GenerateEmployeeCodeAsync(int year, CancellationToken token)
    {
        await using var ownedTransaction = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(token) : null;
        await using var command = Database.GetDbConnection().CreateCommand();
        command.Transaction = Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = @"DECLARE @issued TABLE ([Value] int);
UPDATE [personnel].[EmployeeCodeCounters] WITH (UPDLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue] INTO @issued
WHERE [Year] = @year AND [LastValue] < 9999;
IF NOT EXISTS (SELECT 1 FROM @issued)
BEGIN
    IF EXISTS (SELECT 1 FROM [personnel].[EmployeeCodeCounters] WITH (UPDLOCK,HOLDLOCK) WHERE [Year] = @year)
        THROW 51008, 'Employee code range exhausted for this year.', 1;
    INSERT INTO [personnel].[EmployeeCodeCounters] ([Year],[LastValue]) VALUES (@year,1);
    INSERT INTO @issued VALUES (1);
END;
SELECT [Value] FROM @issued;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@year";
        parameter.Value = year;
        command.Parameters.Add(parameter);
        try
        {
            var value = Convert.ToInt32(await command.ExecuteScalarAsync(token));
            if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
            return $"NV{year:D4}{value:D4}";
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 51008)
        {
            throw new DomainRuleException("employees.code_exhausted", "Employee code limit of 9999 reached for this year.");
        }
    }

    private void EnsureCalendarImmutability()
    {
        foreach(var entry in ChangeTracker.Entries<PayrollPeriodRecord>())
        {
            if(entry.State is not (EntityState.Modified or EntityState.Deleted))continue;
            var original=entry.OriginalValues.GetValue<string>(nameof(PayrollPeriodRecord.Status));
            if(original=="Locked" || (original=="Published" && (entry.State==EntityState.Deleted || entry.Entity.Status!="Locked" || entry.Properties.Any(p=>p.IsModified && p.Metadata.Name is not (nameof(PayrollPeriodRecord.Status) or nameof(PayrollPeriodRecord.LockedBy) or nameof(PayrollPeriodRecord.LockedAtUtc))))))
                throw new DomainRuleException("payroll.period_immutable","Published data may only be sealed Locked; Locked periods are immutable.");
        }
        if (ChangeTracker.Entries<PayrollCalculationSnapshot>().Any(e=>e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<PayrollCalculationBatch>().Any(e=>e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<PayrollInputReceipt>().Any(e=>e.State is EntityState.Modified or EntityState.Deleted))
            throw new DomainRuleException("payroll.snapshot_immutable", "Payroll snapshots, batches and receipts are append-only.");
        foreach(var entry in ChangeTracker.Entries<SalaryHistoryEntry>())
            if(entry.State==EntityState.Deleted || (entry.State==EntityState.Modified && entry.Properties.Any(p=>p.IsModified && p.Metadata.Name!=nameof(SalaryHistoryEntry.EffectiveTo))))
                throw new DomainRuleException("payroll.salary_immutable", "Append a new salary decision; do not overwrite history.");
        foreach(var entry in ChangeTracker.Entries<PayrollManualAdjustment>())
            if(entry.State==EntityState.Deleted || (entry.State==EntityState.Modified && entry.Properties.Any(p=>p.IsModified && p.Metadata.Name!=nameof(PayrollManualAdjustment.VoidedAtUtc) && p.Metadata.Name!=nameof(PayrollManualAdjustment.VoidReason))))
                throw new DomainRuleException("payroll.adjustment_immutable", "Void and replace adjustments; preserve original amounts.");
        foreach(var entry in ChangeTracker.Entries<PayslipRecord>())
            if((entry.State is EntityState.Modified or EntityState.Deleted) && entry.OriginalValues.GetValue<string>(nameof(PayslipRecord.Status)) is "Published" or "Locked")
                throw new DomainRuleException("payroll.payslip_immutable", "Published payslips are immutable.");

        if (ChangeTracker.Entries<AttendanceEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<AttendanceAuditLog>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<AttendanceOvertimeEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<AttendanceIdempotencyRecord>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new DomainRuleException("attendance.immutable", "Attendance evidence and receipts are append-only.");
        if (ChangeTracker.Entries<WorkCalendarVersion>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<CalendarPeriodSnapshot>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new DomainRuleException("calendar.immutable", "Calendar versions and snapshots are append-only.");
        if (ChangeTracker.Entries<LeaveLedgerEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<LeaveIdempotencyRecord>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new DomainRuleException("leave.immutable", "Leave ledger entries and idempotency records are append-only.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureCalendarImmutability();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureCalendarImmutability();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureCalendarImmutability();
        foreach (var entry in ChangeTracker.Entries<EmployeeProfile>())
        {
            if (entry.State == EntityState.Modified && entry.Property(e => e.EmployeeCode).IsModified)
                throw new DomainRuleException("employees.code_immutable", "Employee code cannot be changed.");
            if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.EmployeeCode))
                entry.Entity.EmployeeCode = await GenerateEmployeeCodeAsync(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Year, cancellationToken);
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var attendance = modelBuilder.Entity<AttendanceSummary>();
        attendance.ToTable("AttendanceSummaries", "attendance");
        attendance.HasKey(x => x.Id);
        attendance.Property(x => x.Id).ValueGeneratedNever();
        attendance.HasIndex(x => new { x.EmployeeId, x.BusinessDate }).IsUnique();
        attendance.Property(x => x.CheckInUtc).HasColumnType("datetimeoffset(7)");
        attendance.Property(x => x.CheckOutUtc).HasColumnType("datetimeoffset(7)");
        attendance.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        attendance.Property(x => x.EligibleMinutes).HasColumnType("decimal(20,10)");
        attendance.Property(x => x.LateMinutes).HasColumnType("decimal(20,10)");
        attendance.Property(x => x.EarlyLeaveMinutes).HasColumnType("decimal(20,10)");
        attendance.Property(x => x.Phase).HasMaxLength(20).IsRequired();
        attendance.Property(x => x.CalendarDefinitionJson).IsRequired();
        attendance.Property(x => x.RowVersion).IsRowVersion();
        attendance.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        attendance.HasOne<WorkCalendarVersion>().WithMany().HasForeignKey(x => x.CalendarVersionId).OnDelete(DeleteBehavior.Restrict);
        var attendanceEvent = modelBuilder.Entity<AttendanceEvent>();
        attendanceEvent.ToTable("AttendanceEvents", "attendance");
        attendanceEvent.HasKey(x => x.Id);
        attendanceEvent.Property(x => x.Id).ValueGeneratedNever();
        attendanceEvent.Property(x => x.Kind).HasMaxLength(30).IsRequired();
        attendanceEvent.Property(x => x.OccurredAtUtc).HasColumnType("datetimeoffset(7)");
        attendanceEvent.Property(x => x.EffectiveCheckInUtc).HasColumnType("datetimeoffset(7)");
        attendanceEvent.Property(x => x.EffectiveCheckOutUtc).HasColumnType("datetimeoffset(7)");
        attendanceEvent.HasOne<AttendanceSummary>().WithMany().HasForeignKey(x => x.AttendanceId).OnDelete(DeleteBehavior.Restrict);
        attendanceEvent.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        attendanceEvent.HasIndex(x => new { x.AttendanceId, x.OccurredAtUtc });
        var attendanceAudit = modelBuilder.Entity<AttendanceAuditLog>();
        attendanceAudit.ToTable("AttendanceAuditLogs", "attendance");
        attendanceAudit.HasKey(x => x.Id);
        attendanceAudit.Property(x => x.Id).ValueGeneratedNever();
        attendanceAudit.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        attendanceAudit.Property(x => x.RecordedAtUtc).HasColumnType("datetimeoffset(7)");
        attendanceAudit.Property(x => x.BeforeCheckInUtc).HasColumnType("datetimeoffset(7)");
        attendanceAudit.Property(x => x.BeforeCheckOutUtc).HasColumnType("datetimeoffset(7)");
        attendanceAudit.Property(x => x.AfterCheckInUtc).HasColumnType("datetimeoffset(7)");
        attendanceAudit.Property(x => x.AfterCheckOutUtc).HasColumnType("datetimeoffset(7)");
        attendanceAudit.Property(x => x.BeforeEligibleMinutes).HasColumnType("decimal(20,10)");
        attendanceAudit.Property(x => x.AfterEligibleMinutes).HasColumnType("decimal(20,10)");
        attendanceAudit.HasOne<AttendanceSummary>().WithMany().HasForeignKey(x => x.AttendanceId).OnDelete(DeleteBehavior.Restrict);
        attendanceAudit.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        var overtime = modelBuilder.Entity<AttendanceOvertimeEntry>();
        overtime.ToTable("AttendanceOvertimeEntries", "attendance");
        overtime.HasKey(x => x.Id);
        overtime.Property(x => x.Id).ValueGeneratedNever();
        overtime.Property(x => x.Minutes).HasColumnType("decimal(20,10)");
        overtime.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        overtime.Property(x => x.ConfirmedAtUtc).HasColumnType("datetimeoffset(7)");
        overtime.HasIndex(x => x.AttendanceId).IsUnique();
        overtime.HasOne<AttendanceSummary>().WithMany().HasForeignKey(x => x.AttendanceId).OnDelete(DeleteBehavior.Restrict);
        overtime.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.Restrict);
        var receipt = modelBuilder.Entity<AttendanceIdempotencyRecord>();
        receipt.ToTable("AttendanceIdempotencyRecords", "attendance");
        receipt.HasKey(x => x.Id);
        receipt.Property(x => x.Id).ValueGeneratedNever();
        receipt.Property(x => x.Operation).HasMaxLength(50).IsRequired();
        receipt.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        receipt.Property(x => x.ResponseJson).IsRequired();
        receipt.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        receipt.HasIndex(x => new { x.UserId, x.Operation, x.Key }).IsUnique();
        receipt.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        var calendar = modelBuilder.Entity<WorkCalendarVersion>();
        calendar.ToTable("WorkCalendarVersions", "calendar");
        calendar.HasKey(x => x.Id);
        calendar.Property(x => x.Id).ValueGeneratedNever();
        calendar.Property(x => x.DefinitionJson).IsRequired();
        calendar.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        calendar.HasIndex(x => x.EffectiveFrom).IsUnique();
        calendar.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        var calendarSnapshot = modelBuilder.Entity<CalendarPeriodSnapshot>();
        calendarSnapshot.ToTable("CalendarPeriodSnapshots", "calendar");
        calendarSnapshot.HasKey(x => x.Id);
        calendarSnapshot.Property(x => x.Id).ValueGeneratedNever();
        calendarSnapshot.Property(x => x.ContentJson).IsRequired();
        calendarSnapshot.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        calendarSnapshot.HasIndex(x => x.PayrollPeriodId).IsUnique();
        calendarSnapshot.HasOne<PayrollPeriodRecord>().WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HrmUser>().HasIndex(user => user.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        var session = modelBuilder.Entity<AccountSession>();
        session.ToTable("AccountSessions", "identity");
        session.HasKey(x => x.Id);
        session.Property(x => x.SecurityStamp).HasMaxLength(256).IsRequired();
        session.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        session.HasIndex(x => new { x.UserId, x.Revoked });

        var department = modelBuilder.Entity<Department>();
        department.ToTable("Departments", "personnel");
        department.HasKey(x => x.Id);
        department.Property(x => x.Id).ValueGeneratedNever();
        department.Property(x => x.Name).HasMaxLength(100).IsRequired();
        department.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        department.HasIndex(x => new { x.Name, x.Id });
        department.Property(x => x.Name).IsConcurrencyToken();
        var position = modelBuilder.Entity<Position>();
        position.ToTable("Positions", "personnel");
        position.HasKey(x => x.Id);
        position.Property(x => x.Id).ValueGeneratedNever();
        position.Property(x => x.Name).HasMaxLength(100).IsRequired().IsConcurrencyToken();
        position.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        position.HasIndex(x => new { x.Name, x.Id });

        var counter = modelBuilder.Entity<EmployeeCodeCounter>();
        counter.ToTable("EmployeeCodeCounters", "personnel");
        counter.HasKey(x => x.Year);
        counter.Property(x => x.Year).ValueGeneratedNever();

        var employee = modelBuilder.Entity<EmployeeProfile>();
        employee.ToTable("EmployeeProfiles", "personnel");
        employee.HasKey(x => x.Id);
        employee.Property(x => x.Id).ValueGeneratedNever();
        employee.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
        employee.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        employee.Property(x => x.CitizenId).HasMaxLength(20).IsRequired();
        employee.Property(x => x.Email).HasMaxLength(256);
        employee.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        employee.Property(x => x.Address).HasMaxLength(256).IsRequired();
        employee.Property(x => x.DateOfBirth).HasColumnType("date");
        employee.Property(x => x.Gender).HasMaxLength(20);
        employee.Property(x => x.MaritalStatus).HasMaxLength(50);
        employee.Property(x => x.HireDate).HasColumnType("date");
        employee.Property(x => x.TerminationDate).HasColumnType("date");
        employee.Property(x => x.Status).HasMaxLength(50).IsRequired();
        employee.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        employee.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        employee.Property(x => x.RowVersion).IsRowVersion();
        employee.HasIndex(x => x.EmployeeCode).IsUnique();
        employee.HasIndex(x => x.UserId).IsUnique();
        employee.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        employee.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        employee.HasOne<Position>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);

        var outbox = modelBuilder.Entity<ActivationOutboxMessage>();
        outbox.ToTable("ActivationOutboxMessages", "personnel");
        outbox.HasKey(x => x.Id);
        outbox.Property(x => x.Id).ValueGeneratedNever();
        outbox.Property(x => x.Email).HasMaxLength(256).IsRequired();
        outbox.Property(x => x.Purpose).HasMaxLength(50).IsRequired();
        outbox.Property(x => x.Status).HasMaxLength(50).IsRequired();
        outbox.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        outbox.Property(x => x.DispatchedAtUtc).HasColumnType("datetimeoffset(7)");
        outbox.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        outbox.HasIndex(x => x.UserId);
        outbox.HasIndex(x => x.Status);

        var emailOutbox = modelBuilder.Entity<EmailOutboxMessage>();
        emailOutbox.ToTable("EmailOutboxMessages", "notifications");
        emailOutbox.HasKey(x => x.Id);
        emailOutbox.Property(x => x.Id).ValueGeneratedNever();
        emailOutbox.Property(x => x.To).HasMaxLength(256).IsRequired();
        emailOutbox.Property(x => x.Subject).HasMaxLength(256).IsRequired();
        emailOutbox.Property(x => x.Body).IsRequired();
        emailOutbox.Property(x => x.Purpose).HasMaxLength(50).IsRequired();
        emailOutbox.Property(x => x.Status).HasMaxLength(30).IsRequired();
        emailOutbox.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        emailOutbox.Property(x => x.NextAttemptAtUtc).HasColumnType("datetimeoffset(7)");
        emailOutbox.Property(x => x.SentAtUtc).HasColumnType("datetimeoffset(7)");
        emailOutbox.Property(x => x.ExpiresAtUtc).HasColumnType("datetimeoffset(7)");
        emailOutbox.Property(x => x.LastError).HasMaxLength(1000);
        emailOutbox.Property(x => x.IdempotencyKey).HasMaxLength(128);
        emailOutbox.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        emailOutbox.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

        var hrScope = modelBuilder.Entity<HrEmployeeAssignment>();
        hrScope.ToTable("HrEmployeeAssignments", "personnel");
        hrScope.HasKey(x => x.Id);
        hrScope.Property(x => x.Id).ValueGeneratedNever();
        hrScope.Property(x => x.AssignedAtUtc).HasColumnType("datetimeoffset(7)");
        hrScope.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.HrUserId).OnDelete(DeleteBehavior.Restrict);
        hrScope.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        hrScope.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.AssignedBy).OnDelete(DeleteBehavior.Restrict);
        hrScope.HasIndex(x => new { x.HrUserId, x.EmployeeId }).IsUnique();
        hrScope.HasIndex(x => x.HrUserId);
        hrScope.HasIndex(x => x.EmployeeId);

        var leave = modelBuilder.Entity<LeaveRecord>();
        leave.ToTable("LeaveRecords", "personnel");
        leave.HasKey(x => x.Id);
        leave.Property(x => x.Id).ValueGeneratedNever();
        leave.Property(x => x.WorkingDays).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        leave.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        leave.Property(x => x.Status).HasMaxLength(20).IsRequired();
        leave.Property(x => x.DecisionReason).HasMaxLength(500);
        leave.Property(x => x.DecidedAtUtc).HasColumnType("datetimeoffset(7)");
        leave.Property(x => x.CancelledAtUtc).HasColumnType("datetimeoffset(7)");
        leave.Property(x => x.CancellationReason).HasMaxLength(500);
        leave.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        leave.Property(x => x.RowVersion).IsRowVersion();
        leave.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        leave.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.ApproverId).OnDelete(DeleteBehavior.Restrict);
        leave.HasIndex(x => new { x.EmployeeId, x.Status });

        var leaveBalance = modelBuilder.Entity<LeaveBalance>();
        leaveBalance.ToTable("LeaveBalances", "personnel", t =>
        {
            t.HasCheckConstraint("CK_LeaveBalances_UsedDays_NonNegative", "[UsedDays] >= 0");
            t.HasCheckConstraint("CK_LeaveBalances_NoOverdraft", "[AllocatedDays] >= [UsedDays]");
        });
        leaveBalance.HasKey(x => x.Id);
        leaveBalance.Property(x => x.Id).ValueGeneratedNever();
        leaveBalance.Property(x => x.AllocatedDays).HasColumnType("decimal(18,2)");
        leaveBalance.Property(x => x.UsedDays).HasColumnType("decimal(18,2)");
        leaveBalance.Ignore(x => x.RemainingDays);
        leaveBalance.Property(x => x.RowVersion).IsRowVersion();
        leaveBalance.Property(x => x.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        leaveBalance.HasIndex(x => new { x.EmployeeId, x.Year }).IsUnique();
        leaveBalance.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        var leaveLedger = modelBuilder.Entity<LeaveLedgerEntry>();
        leaveLedger.ToTable("LeaveLedgerEntries", "personnel");
        leaveLedger.HasKey(x => x.Id);
        leaveLedger.Property(x => x.Id).ValueGeneratedNever();
        leaveLedger.Property(x => x.Action).HasMaxLength(50).IsRequired();
        leaveLedger.Property(x => x.Days).HasColumnType("decimal(18,2)");
        leaveLedger.Property(x => x.BalanceBefore).HasColumnType("decimal(18,2)");
        leaveLedger.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
        leaveLedger.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        leaveLedger.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        leaveLedger.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        leaveLedger.HasOne<LeaveRecord>().WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        leaveLedger.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        leaveLedger.HasIndex(x => new { x.LeaveRequestId, x.Action }).IsUnique().HasFilter("[LeaveRequestId] IS NOT NULL");
        leaveLedger.HasIndex(x => new { x.EmployeeId, x.Year, x.Action }).IsUnique().HasFilter("[Action] = N'Grant'");
        leaveLedger.HasIndex(x => new { x.EmployeeId, x.Year });

        var leaveIdemp = modelBuilder.Entity<LeaveIdempotencyRecord>();
        leaveIdemp.ToTable("LeaveIdempotencyRecords", "personnel");
        leaveIdemp.HasKey(x => x.Id);
        leaveIdemp.Property(x => x.Id).ValueGeneratedNever();
        leaveIdemp.Property(x => x.Operation).HasMaxLength(50).IsRequired();
        leaveIdemp.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        leaveIdemp.Property(x => x.ResponseJson).IsRequired();
        leaveIdemp.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        leaveIdemp.HasIndex(x => new { x.UserId, x.Operation, x.Key }).IsUnique();
        leaveIdemp.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        var salary = modelBuilder.Entity<SalaryHistoryEntry>();
        salary.ToTable("SalaryHistory", "payroll", t=>{t.HasCheckConstraint("CK_Salary_Positive","[MonthlyBase]>0");t.HasCheckConstraint("CK_Salary_Range","[EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]");});
        salary.HasKey(x=>x.Id);salary.Property(x=>x.Id).ValueGeneratedNever();salary.Property(x=>x.MonthlyBase).HasColumnType("decimal(20,2)");salary.Property(x=>x.Reason).HasMaxLength(500);salary.Property(x=>x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");salary.Property(x=>x.RowVersion).IsRowVersion();salary.HasIndex(x=>new{x.EmployeeId,x.EffectiveFrom}).IsUnique();salary.HasIndex(x=>x.EmployeeId).IsUnique().HasFilter("[EffectiveTo] IS NULL");
        salary.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);salary.HasOne<HrmUser>().WithMany().HasForeignKey(x=>x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        var manual=modelBuilder.Entity<PayrollManualAdjustment>();
        manual.ToTable("ManualAdjustments","payroll",t=>t.HasCheckConstraint("CK_Adjustment_Positive","[Amount]>0"));manual.HasKey(x=>x.Id);manual.Property(x=>x.Id).ValueGeneratedNever();manual.Property(x=>x.Kind).HasMaxLength(30);manual.Property(x=>x.Reason).HasMaxLength(500);manual.Property(x=>x.VoidReason).HasMaxLength(500);manual.Property(x=>x.Amount).HasColumnType("decimal(20,2)");manual.Property(x=>x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");manual.Property(x=>x.VoidedAtUtc).HasColumnType("datetimeoffset(7)");manual.Property(x=>x.RowVersion).IsRowVersion();manual.HasIndex(x=>new{x.PeriodId,x.EmployeeId});
        manual.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);manual.HasOne<PayrollPeriodRecord>().WithMany().HasForeignKey(x=>x.PeriodId).OnDelete(DeleteBehavior.Restrict);manual.HasOne<HrmUser>().WithMany().HasForeignKey(x=>x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        var batch=modelBuilder.Entity<PayrollCalculationBatch>();batch.ToTable("CalculationBatches","payroll");batch.HasKey(x=>x.Id);batch.Property(x=>x.Id).ValueGeneratedNever();batch.Property(x=>x.InputHash).HasMaxLength(64);batch.Property(x=>x.CapturedAtUtc).HasColumnType("datetimeoffset(7)");batch.HasIndex(x=>new{x.PeriodId,x.Version}).IsUnique();batch.HasOne<PayrollPeriodRecord>().WithMany().HasForeignKey(x=>x.PeriodId).OnDelete(DeleteBehavior.Restrict);batch.HasOne<HrmUser>().WithMany().HasForeignKey(x=>x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        var snapshot=modelBuilder.Entity<PayrollCalculationSnapshot>();snapshot.ToTable("CalculationSnapshots","payroll");snapshot.HasKey(x=>x.Id);snapshot.Property(x=>x.Id).ValueGeneratedNever();snapshot.Property(x=>x.Net).HasColumnType("decimal(20,2)");snapshot.Property(x=>x.InputHash).HasMaxLength(64);snapshot.HasIndex(x=>new{x.BatchId,x.EmployeeId}).IsUnique();snapshot.HasOne<PayrollCalculationBatch>().WithMany().HasForeignKey(x=>x.BatchId).OnDelete(DeleteBehavior.Restrict);snapshot.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        var payrollReceipt=modelBuilder.Entity<PayrollInputReceipt>();payrollReceipt.ToTable("InputReceipts","payroll");payrollReceipt.HasKey(x=>x.Id);payrollReceipt.Property(x=>x.Id).ValueGeneratedNever();payrollReceipt.Property(x=>x.Operation).HasMaxLength(50);payrollReceipt.Property(x=>x.RequestHash).HasMaxLength(64);payrollReceipt.Property(x=>x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");payrollReceipt.HasIndex(x=>new{x.UserId,x.Operation,x.Key}).IsUnique();payrollReceipt.HasOne<HrmUser>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);

        var period = modelBuilder.Entity<PayrollPeriodRecord>();
        period.ToTable("PayrollPeriods", "payroll");
        period.HasKey(x => x.Id);
        period.Property(x => x.Id).ValueGeneratedNever();
        period.Property(x => x.PeriodCode).HasMaxLength(20).IsRequired();
        period.Property(x => x.Status).HasMaxLength(20).IsRequired();
        period.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        period.Property(x => x.CalculatedAtUtc).HasColumnType("datetimeoffset(7)");
        period.Property(x => x.ApprovedAtUtc).HasColumnType("datetimeoffset(7)");
        period.Property(x => x.PublishedAtUtc).HasColumnType("datetimeoffset(7)");
        period.Property(x => x.RowVersion).IsRowVersion();
        period.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.CalculatedBy).OnDelete(DeleteBehavior.Restrict);
        period.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        period.HasOne<HrmUser>().WithMany().HasForeignKey(x => x.PublishedBy).OnDelete(DeleteBehavior.Restrict);
        period.HasIndex(x => x.PeriodCode).IsUnique();
        period.Property(x=>x.LockedAtUtc).HasColumnType("datetimeoffset(7)");
        period.HasOne<HrmUser>().WithMany().HasForeignKey(x=>x.LockedBy).OnDelete(DeleteBehavior.Restrict);
        period.HasOne<PayrollCalculationBatch>().WithMany().HasForeignKey(x=>x.ActiveBatchId).OnDelete(DeleteBehavior.Restrict);
        period.HasOne<PayrollCalculationBatch>().WithMany().HasForeignKey(x=>x.ApprovedBatchId).OnDelete(DeleteBehavior.Restrict);
        period.HasOne<PayrollCalculationBatch>().WithMany().HasForeignKey(x=>x.PublishedBatchId).OnDelete(DeleteBehavior.Restrict);


        var payslip = modelBuilder.Entity<PayslipRecord>();
        payslip.ToTable("Payslips", "payroll");
        payslip.HasKey(x => x.Id);
        payslip.Property(x => x.Id).ValueGeneratedNever();
        payslip.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        payslip.Property(x => x.Status).HasMaxLength(20).IsRequired();
        payslip.Property(x => x.PublishedAtUtc).HasColumnType("datetimeoffset(7)");
        payslip.HasOne<PayrollPeriodRecord>().WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Cascade);
        payslip.HasOne<PayrollCalculationSnapshot>().WithMany().HasForeignKey(x=>x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        payslip.HasOne<EmployeeProfile>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        payslip.HasIndex(x => new { x.PeriodId, x.EmployeeId }).IsUnique();

        var audit = modelBuilder.Entity<AuditEvent>();
        audit.ToTable("AuditEvents", "audit");
        audit.HasKey(x => x.Id);
        audit.Property(x => x.Id).ValueGeneratedNever();
        audit.Property(x => x.TraceId).HasMaxLength(128).IsRequired();
        audit.Property(x => x.Action).HasMaxLength(100).IsRequired();
        audit.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        audit.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
        audit.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        audit.Property(x => x.ActorUsername).HasMaxLength(256);
        audit.Property(x => x.OccurredAtUtc).HasColumnType("datetimeoffset(7)");
        audit.HasIndex(x => x.OccurredAtUtc);
        audit.HasIndex(x => new { x.Action, x.OccurredAtUtc });
        audit.HasIndex(x => new { x.ActorId, x.OccurredAtUtc });
    }
}
