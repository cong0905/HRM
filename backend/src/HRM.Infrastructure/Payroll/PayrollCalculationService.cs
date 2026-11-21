using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Attendance;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Payroll;

public sealed class PayrollCalculationService(HrmDbContext db,ISecurityContext security,
    IWorkCalendarService calendars,TimeProvider clock,IAuditRepository audit) : IPayrollCalculationService
{
    private static readonly string[] Kinds=["Allowance","Bonus","OtherDeduction","DemoInsurance","DemoTax"];
    private void Require(string permission)
    {if(!security.IsAuthenticated || !security.CurrentUserId.HasValue || !security.HasPermission(permission))throw new PayrollAccessDeniedException();}
    private static string Reason(string raw)
    {if(string.IsNullOrWhiteSpace(raw)||raw.Trim().Length is <10 or >500)throw new RequestValidationException("payroll.reason_required","A reason of10..500 characters is required.");return AuditMasker.SanitizeFreeText(raw.Trim());}
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static Guid Key(string raw)
    {if(raw.Length!=36 || !Guid.TryParseExact(raw,"D",out var key)||raw[14]!='4'||!"89abAB".Contains(raw[19]))throw new RequestValidationException("idempotency.invalid_key","UUIDv4 key required.");return key;}
    private static bool Month(string code,out DateOnly day)=>DateOnly.TryParseExact(code+"-01","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day);
    private static bool PeriodMonth(string code,out DateOnly day)=>Month(code.Length>=7?code[..7]:code,out day);
    private async Task ReceiptLockAsync(string op,Guid key,CancellationToken t)
    {var resource=$"HRM.Payroll.Receipt.{security.CurrentUserId:N}.{op}.{key:N}";await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51014,'Payroll receipt busy.',1;",t);}
    private async Task<T?> ReplayAsync<T>(string op,Guid key,string hash,CancellationToken t) where T:class
    {var row=await db.PayrollInputReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==security.CurrentUserId && x.Operation==op && x.Key==key,t);if(row is null)return null;if(row.RequestHash!=hash)throw new ResourceConflictException("idempotency.payload_mismatch","Key payload differs.");return JsonSerializer.Deserialize<T>(row.ResponseJson)!;}
    private void Receipt<T>(string op,Guid key,string hash,T response)=>db.PayrollInputReceipts.Add(new(){Id=Guid.NewGuid(),UserId=security.CurrentUserId!.Value,Operation=op,Key=key,RequestHash=hash,ResponseJson=JsonSerializer.Serialize(response),CreatedAtUtc=clock.GetUtcNow()});
    private Task AuditAsync(string action,Guid id,string reason,CancellationToken t)=>audit.RecordAsync(new AuditEvent{Id=Guid.NewGuid(),OccurredAtUtc=clock.GetUtcNow(),ActorId=security.CurrentUserId,ActorUsername=security.CurrentUsername,TraceId=Activity.Current?.Id??Guid.NewGuid().ToString(),Action=action,EntityName="Payroll",EntityId=id.ToString(),Reason=reason,Payload=AuditMasker.MaskObject(new{resourceId=id})},t);
    private static SalaryHistoryResponse Salary(SalaryHistoryEntry x)=>new(x.Id,x.EmployeeId,x.EffectiveFrom,x.EffectiveTo,x.MonthlyBase,x.Reason,x.CreatedAtUtc,Convert.ToBase64String(x.RowVersion));
    private static PayrollAdjustmentResponse Adjustment(PayrollManualAdjustment x)=>new(x.Id,x.PeriodId,x.EmployeeId,x.Kind,x.Amount,x.Reason,x.VoidedAtUtc,x.VoidReason,Convert.ToBase64String(x.RowVersion));
    private async Task<PayrollPeriodRecord> PeriodAsync(Guid id,CancellationToken t)=>await db.PayrollPeriods.FromSqlInterpolated($"SELECT * FROM [payroll].[PayrollPeriods] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsTracking().SingleOrDefaultAsync(t)??throw new PayrollNotFoundException("payroll.period_not_found");
    private static void Open(PayrollPeriodRecord period)
    {if(period.Status is "Published" or "Locked")throw new ResourceConflictException("payroll.period_immutable","Published/locked inputs are immutable.");}

    public async Task<SalaryHistoryResponse> AppendSalaryAsync(Guid employeeId,AppendSalaryRequest request,string key,CancellationToken t)
    {
        Require(HrmPermissions.PayrollHistoryManage);var reason=Reason(request.Reason);var uuid=Key(key);
        if(request.EffectiveFrom.Year is <2000 or >2100 || request.MonthlyBase is <=0m or >1000000000000m)throw new RequestValidationException("payroll.invalid_salary","Invalid salary/date.");
        if(decimal.Round(request.MonthlyBase,2)!=request.MonthlyBase)throw new RequestValidationException("payroll.money_precision","Money input supports at most two decimal places; no silent rounding.");
        var hash=Hash(JsonSerializer.Serialize(new{employeeId,request}));await using var tx=await db.Database.BeginTransactionAsync(t);
        await ReceiptLockAsync("salary.append",uuid,t);await PayrollDataLocks.SourceAsync(db,false,t);
        var replay=await ReplayAsync<SalaryHistoryResponse>("salary.append",uuid,hash,t);if(replay is not null){await tx.CommitAsync(t);return replay;}
        await AttendanceLocks.EmployeeAsync(db,employeeId,t);
        if(!await db.EmployeeProfiles.AnyAsync(x=>x.Id==employeeId,t))throw new PayrollNotFoundException("employee.not_found");
        // Shared source lock serializes this range check with calculate/approve/publish.
        var closed=await db.PayrollPeriods.AsNoTracking().Where(x=>x.Status=="Published"||x.Status=="Locked").Select(x=>x.PeriodCode).ToListAsync(t);
        if(closed.Any(code=>PeriodMonth(code,out var day)&&day.AddMonths(1).AddDays(-1)>=request.EffectiveFrom))throw new ResourceConflictException("payroll.salary_closed_period","Effective date changes a closed period.");
        var previous=await db.SalaryHistoryEntries.AsTracking().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.EffectiveFrom).FirstOrDefaultAsync(t);
        if(previous is not null)
        {if(request.EffectiveFrom<=previous.EffectiveFrom)throw new ResourceConflictException("payroll.salary_effective_order","Append dates must increase; overlapping/duplicate history is forbidden.");if(!previous.EffectiveTo.HasValue||previous.EffectiveTo>=request.EffectiveFrom)previous.EffectiveTo=request.EffectiveFrom.AddDays(-1);}
        if(previous is not null)await db.SaveChangesAsync(t);
        var row=new SalaryHistoryEntry{Id=Guid.NewGuid(),EmployeeId=employeeId,EffectiveFrom=request.EffectiveFrom,MonthlyBase=request.MonthlyBase,Reason=reason,CreatedBy=security.CurrentUserId!.Value,CreatedAtUtc=clock.GetUtcNow()};db.SalaryHistoryEntries.Add(row);await db.SaveChangesAsync(t);
        var response=Salary(row);Receipt("salary.append",uuid,hash,response);await AuditAsync("payroll.salary_appended",row.Id,reason,t);await tx.CommitAsync(t);return response;
    }
    public async Task<IReadOnlyList<SalaryHistoryResponse>> SalaryHistoryAsync(Guid employeeId,CancellationToken t)
    {Require(HrmPermissions.PayrollReadAll);return (await db.SalaryHistoryEntries.AsNoTracking().Where(x=>x.EmployeeId==employeeId).OrderBy(x=>x.EffectiveFrom).ToListAsync(t)).Select(Salary).ToList();}

    public async Task<PayrollAdjustmentResponse> AddAdjustmentAsync(Guid periodId,AddPayrollAdjustmentRequest request,string key,CancellationToken t)
    {
        Require(HrmPermissions.PayrollCalculate);var reason=Reason(request.Reason);var uuid=Key(key);
        if(!Kinds.Contains(request.Kind)||request.Amount is <=0m or >1000000000000m)throw new RequestValidationException("payroll.invalid_adjustment","Invalid kind/positive demo amount.");
        if(decimal.Round(request.Amount,2)!=request.Amount)throw new RequestValidationException("payroll.money_precision","Money input supports at most two decimal places; no silent rounding.");
        var hash=Hash(JsonSerializer.Serialize(new{periodId,request}));await using var tx=await db.Database.BeginTransactionAsync(t);await ReceiptLockAsync("adjustment.add",uuid,t);await PayrollDataLocks.SourceAsync(db,false,t);
        var replay=await ReplayAsync<PayrollAdjustmentResponse>("adjustment.add",uuid,hash,t);if(replay is not null){await tx.CommitAsync(t);return replay;}
        await AttendanceLocks.EmployeeAsync(db,request.EmployeeId,t);var period=await PeriodAsync(periodId,t);Open(period);
        if(!Month(period.PeriodCode,out var day))throw new RequestValidationException("payroll.month_required","Real payroll requires yyyy-MM.");
        await AttendanceLocks.MonthAsync(db,day,false,t);
        var employee=await db.EmployeeProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==request.EmployeeId,t)??throw new PayrollNotFoundException("employee.not_found");
        if(!employee.HireDate.HasValue || employee.HireDate>day.AddMonths(1).AddDays(-1) || (employee.TerminationDate.HasValue && employee.TerminationDate<day))
            throw new ResourceConflictException("payroll.employee_outside_period","Manual adjustment requires a known employment interval overlapping the period.");
        var row=new PayrollManualAdjustment{Id=Guid.NewGuid(),PeriodId=periodId,EmployeeId=request.EmployeeId,Kind=request.Kind,Amount=request.Amount,Reason=reason,CreatedAtUtc=clock.GetUtcNow(),CreatedBy=security.CurrentUserId!.Value};db.PayrollManualAdjustments.Add(row);period.SnapshotRequired=true;await db.SaveChangesAsync(t);
        var response=Adjustment(row);Receipt("adjustment.add",uuid,hash,response);await AuditAsync("payroll.adjustment_added",row.Id,reason,t);await tx.CommitAsync(t);return response;
    }
    public async Task<PayrollAdjustmentResponse> VoidAdjustmentAsync(Guid id,VoidPayrollAdjustmentRequest request,string key,CancellationToken t)
    {
        Require(HrmPermissions.PayrollCalculate);var reason=Reason(request.Reason);var uuid=Key(key);var hash=Hash(JsonSerializer.Serialize(new{id,request}));
        await using var tx=await db.Database.BeginTransactionAsync(t);await ReceiptLockAsync("adjustment.void",uuid,t);await PayrollDataLocks.SourceAsync(db,false,t);
        var replay=await ReplayAsync<PayrollAdjustmentResponse>("adjustment.void",uuid,hash,t);if(replay is not null){await tx.CommitAsync(t);return replay;}
        var route=await db.PayrollManualAdjustments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,t)??throw new PayrollNotFoundException("payroll.adjustment_not_found");await AttendanceLocks.EmployeeAsync(db,route.EmployeeId,t);
        var period=await PeriodAsync(route.PeriodId,t);Open(period);if(Month(period.PeriodCode,out var day))await AttendanceLocks.MonthAsync(db,day,false,t);
        var row=await db.PayrollManualAdjustments.AsTracking().SingleAsync(x=>x.Id==id,t);
        if(row.VoidedAtUtc.HasValue||Convert.ToBase64String(row.RowVersion)!=request.RowVersion)throw new ResourceConflictException("payroll.adjustment_conflict","Adjustment changed or was already voided.");
        row.VoidedAtUtc=clock.GetUtcNow();row.VoidReason=reason;await db.SaveChangesAsync(t);var response=Adjustment(row);Receipt("adjustment.void",uuid,hash,response);await AuditAsync("payroll.adjustment_voided",id,reason,t);await tx.CommitAsync(t);return response;
    }
    public async Task<IReadOnlyList<PayrollAdjustmentResponse>> AdjustmentsAsync(Guid periodId,CancellationToken t)
    {Require(HrmPermissions.PayrollReadAll);return (await db.PayrollManualAdjustments.AsNoTracking().Where(x=>x.PeriodId==periodId).OrderBy(x=>x.CreatedAtUtc).ThenBy(x=>x.Id).ToListAsync(t)).Select(Adjustment).ToList();}

    private sealed record Planned(Guid EmployeeId,PayrollCalculationInput Input,string SourceJson,string Hash);
    private async Task<List<Planned>> PlanAsync(PayrollPeriodRecord period,bool captureCalendar,CancellationToken t)
    {
        if(!Month(period.PeriodCode,out var month))throw new RequestValidationException("payroll.month_required","Real payroll requires yyyy-MM.");
        var end=month.AddMonths(1).AddDays(-1);var pinned=await calendars.ReadSnapshotAsync(period.Id,t);
        if(pinned is null&&captureCalendar)pinned=await calendars.CaptureSnapshotAsync(period.Id,t);
        if(pinned is null)throw new ResourceConflictException("payroll.calendar_missing","Pinned calendar required.");
        var calendar=pinned.Calendar.Days.Select(d=>new PayrollCalendarDay(d.Date,d.StandardMinutes,d.Definition.Holidays.Contains(d.Date)?PayrollDayKind.Holiday:d.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday?PayrollDayKind.Weekend:PayrollDayKind.Weekday)).ToList();
        // Employment overlap, deliberately NOT current account active/status.
        var employees=await db.EmployeeProfiles.FromSqlRaw("SELECT * FROM personnel.EmployeeProfiles WITH (HOLDLOCK)").AsNoTracking().Where(x=>(x.HireDate==null||x.HireDate<=end)&&(x.TerminationDate==null||x.TerminationDate>=month)).OrderBy(x=>x.Id).ToListAsync(t);
        if(employees.Count==0)throw new DomainRuleException("payroll.no_employees","No employees overlap this period.");
        var employeeIds=employees.Select(x=>x.Id).ToList();
        if(await db.PayrollManualAdjustments.AnyAsync(x=>x.PeriodId==period.Id && x.VoidedAtUtc==null && !employeeIds.Contains(x.EmployeeId),t))
            throw new DomainRuleException("payroll.adjustment_outside_employment","Active adjustments exist for an employee outside this period; resolve them before calculation.");
        var plan=new List<Planned>();
        foreach(var employee in employees)
        {
            if(!employee.HireDate.HasValue)throw new DomainRuleException("payroll.hire_date_missing","Employee hire date required.");
            var salaries=await db.SalaryHistoryEntries.AsNoTracking().Where(x=>x.EmployeeId==employee.Id&&x.EffectiveFrom<=end&&(x.EffectiveTo==null||x.EffectiveTo>=month)).OrderBy(x=>x.EffectiveFrom).ToListAsync(t);
            var punches=await db.AttendanceSummaries.AsNoTracking().Where(x=>x.EmployeeId==employee.Id&&x.BusinessDate>=month&&x.BusinessDate<=end).OrderBy(x=>x.BusinessDate).ToListAsync(t);
            var punchIds=punches.Select(x=>x.Id).ToList();var ot=await db.AttendanceOvertimeEntries.AsNoTracking().Where(x=>punchIds.Contains(x.AttendanceId)).OrderBy(x=>x.Id).ToListAsync(t);
            var leaves=await db.LeaveRecords.AsNoTracking().Where(x=>x.EmployeeId==employee.Id&&x.Status=="Approved"&&x.StartDate<=end&&x.EndDate>=month).OrderBy(x=>x.Id).ToListAsync(t);
            var manual=await db.PayrollManualAdjustments.AsNoTracking().Where(x=>x.EmployeeId==employee.Id&&x.PeriodId==period.Id&&x.VoidedAtUtc==null).OrderBy(x=>x.Id).ToListAsync(t);
            decimal Sum(string kind)=>manual.Where(x=>x.Kind==kind).Sum(x=>x.Amount);
            var facts=calendar.Select(day=>{var punch=punches.SingleOrDefault(x=>x.BusinessDate==day.Date);return new PayrollDayInput(day.Date,punch?.EligibleMinutes??0m,punch is null||punch.CheckOutUtc.HasValue,day.StandardMinutes>0m&&leaves.Any(l=>l.StartDate<=day.Date&&l.EndDate>=day.Date),punch is null?0m:ot.Where(x=>x.AttendanceId==punch.Id).Sum(x=>x.Minutes));}).ToList();
            var input=new PayrollCalculationInput(month.Year,month.Month,employee.HireDate.Value,employee.TerminationDate,calendar,salaries.Select(x=>new PayrollSalaryRate(x.EffectiveFrom,x.EffectiveTo,x.MonthlyBase)).ToList(),facts,new(Sum("Allowance"),Sum("Bonus"),Sum("OtherDeduction"),Sum("DemoInsurance"),Sum("DemoTax")),new("POL-2026-V1"));
            var source=JsonSerializer.Serialize(new{employee.Id,employee.HireDate,employee.TerminationDate,employee.RowVersion,calendarSnapshotId=pinned.PayrollPeriodId,calendar=pinned.Calendar,salaries=salaries.Select(x=>new{x.Id,x.RowVersion}),punches=punches.Select(x=>new{x.Id,x.RowVersion}),ot=ot.Select(x=>new{x.Id,x.Minutes}),leaves=leaves.Select(x=>new{x.Id,x.RowVersion}),manual=manual.Select(x=>new{x.Id,x.RowVersion}),input});
            plan.Add(new(employee.Id,input,source,Hash(source)));
        }
        return plan;
    }
    private static string PlanHash(IEnumerable<Planned> plan)=>Hash(string.Join("|",plan.Select(x=>x.EmployeeId.ToString("N")+":"+x.Hash)));
    private async Task<PayrollBatchResponse?> ReadCoreAsync(Guid periodId,int? version,CancellationToken t)
    {
        var query=db.PayrollCalculationBatches.AsNoTracking().Where(x=>x.PeriodId==periodId);if(version.HasValue)query=query.Where(x=>x.Version==version.Value);
        var batch=await query.OrderByDescending(x=>x.Version).FirstOrDefaultAsync(t);if(batch is null)return null;
        var rows=await db.PayrollCalculationSnapshots.AsNoTracking().Where(x=>x.BatchId==batch.Id).OrderBy(x=>x.EmployeeId).ToListAsync(t);
        return new(batch.Id,periodId,batch.Version,batch.CapturedAtUtc,batch.InputHash,rows.Select(x=>new PayrollSnapshotResponse(x.Id,x.EmployeeId,JsonSerializer.Deserialize<PayrollCalculationInput>(x.InputJson)!,JsonSerializer.Deserialize<PayrollCalculationOutput>(x.OutputJson)!,x.InputHash)).ToList());
    }
    public async Task<PayrollBatchResponse?> ReadBatchAsync(Guid periodId,int? version,CancellationToken t)
    {Require(HrmPermissions.PayrollReadAll);return await ReadCoreAsync(periodId,version,t);}
    public async Task<bool> RequiresSnapshotsAsync(Guid periodId,CancellationToken t)=>
        await db.PayrollPeriods.AnyAsync(x=>x.Id==periodId&&x.SnapshotRequired,t)||await db.SalaryHistoryEntries.AnyAsync(t);

    public async Task<PayrollBatchResponse> CaptureAsync(Guid periodId,CapturePayrollRequest request,string? key,CancellationToken t)
    {
        Require(HrmPermissions.PayrollCalculate);var reason=Reason(request.Reason);var uuid=key is null?Guid.Empty:Key(key);var requestHash=Hash(JsonSerializer.Serialize(new{periodId,request}));
        await using var owned=db.Database.CurrentTransaction is null?await db.Database.BeginTransactionAsync(t):null;
        if(uuid!=Guid.Empty)await ReceiptLockAsync("payroll.capture",uuid,t);await PayrollDataLocks.SourceAsync(db,true,t);
        if(uuid!=Guid.Empty){var replay=await ReplayAsync<PayrollBatchResponse>("payroll.capture",uuid,requestHash,t);if(replay is not null){if(owned is not null)await owned.CommitAsync(t);return replay;}}
        var period=await PeriodAsync(periodId,t);Open(period);if(period.Status=="Approved")throw new ResourceConflictException("payroll.period_already_approved","Approved period must not be recalculated.");
        if(!Month(period.PeriodCode,out var month))throw new RequestValidationException("payroll.month_required","Real payroll requires yyyy-MM.");await AttendanceLocks.MonthAsync(db,month,true,t);
        var plan=await PlanAsync(period,true,t);var version=(await db.PayrollCalculationBatches.Where(x=>x.PeriodId==periodId).Select(x=>(int?)x.Version).MaxAsync(t)??0)+1;
        var batch=new PayrollCalculationBatch{Id=Guid.NewGuid(),PeriodId=periodId,Version=version,CreatedBy=security.CurrentUserId!.Value,CapturedAtUtc=clock.GetUtcNow(),InputHash=PlanHash(plan)};db.PayrollCalculationBatches.Add(batch);
        var slips=await db.Payslips.AsTracking().Where(x=>x.PeriodId==periodId).ToListAsync(t);
        foreach(var item in plan)
        {
            var result=PayrollCalculator.Calculate(item.Input);
            if(result.Net<=-10000000000000000m || result.Net>=10000000000000000m)throw new DomainRuleException("payroll.output_out_of_range","Calculated net exceeds the persisted payslip money range.");
            var snapshot=new PayrollCalculationSnapshot{Id=Guid.NewGuid(),BatchId=batch.Id,EmployeeId=item.EmployeeId,InputJson=JsonSerializer.Serialize(item.Input),SourceJson=item.SourceJson,OutputJson=JsonSerializer.Serialize(result),InputHash=item.Hash,Net=result.Net,CanPublish=result.CanPublish};db.PayrollCalculationSnapshots.Add(snapshot);
            var slip=slips.SingleOrDefault(x=>x.EmployeeId==item.EmployeeId);
            if(slip is null){slip=new PayslipRecord{Id=Guid.NewGuid(),PeriodId=periodId,EmployeeId=item.EmployeeId};db.Payslips.Add(slip);}
            slip.Amount=result.Net;slip.SnapshotId=snapshot.Id;slip.Status="Draft";slip.PublishedAtUtc=null;
        }
        db.Payslips.RemoveRange(slips.Where(x=>plan.All(p=>p.EmployeeId!=x.EmployeeId)));period.SnapshotRequired=true;
        period.ActiveBatchId=batch.Id;period.ApprovedBatchId=null;
        period.Status="Calculated";period.CalculatedBy=security.CurrentUserId;period.CalculatedAtUtc=batch.CapturedAtUtc;period.ApprovedBy=null;period.ApprovedAtUtc=null;
        await db.SaveChangesAsync(t);var response=(await ReadCoreAsync(periodId,version,t))!;
        if(uuid!=Guid.Empty)Receipt("payroll.capture",uuid,requestHash,response);await AuditAsync("payroll.snapshot_created",batch.Id,reason,t);
        if(owned is not null)await owned.CommitAsync(t);return response;
    }
    public async Task EnsurePublishableAsync(Guid periodId,CancellationToken t)
    {
        var period=await PeriodAsync(periodId,t);
        if(await db.Payslips.AnyAsync(x=>x.PeriodId==periodId&&x.Amount<0m,t))throw new ResourceConflictException("payroll.negative_net","Negative net blocks approval/publication.");
        if(!period.SnapshotRequired && !await db.SalaryHistoryEntries.AnyAsync(t))return; // Preserve T07/T09 metadata-only records only when no real salary source exists.
        var batch=await ReadCoreAsync(periodId,null,t)??throw new ResourceConflictException("payroll.snapshot_missing","Calculate a real snapshot first.");
        if(period.ActiveBatchId!=batch.Id)throw new ResourceConflictException("payroll.active_run_mismatch","Active run does not match the latest immutable batch.");
        if(batch.Snapshots.Count==0||batch.Snapshots.Any(x=>!x.Output.CanPublish))throw new ResourceConflictException("payroll.calculation_blocked","Resolve negative net or incomplete attendance before approval/publication.");
        if(!Month(period.PeriodCode,out var month))throw new ResourceConflictException("payroll.month_required","Real payroll requires yyyy-MM.");
        await AttendanceLocks.MonthAsync(db,month,true,t);
        if(batch.InputHash!=PlanHash(await PlanAsync(period,false,t)))throw new ResourceConflictException("payroll.inputs_changed","Inputs changed; calculate again before approval/publication.");
    }
}
