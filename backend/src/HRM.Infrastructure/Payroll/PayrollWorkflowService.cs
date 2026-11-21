using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Attendance;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Payroll;

public sealed class PayrollWorkflowService(HrmDbContext db,ISecurityContext security,
    IPayrollCalculationService calculations,IAuditRepository audit,TimeProvider clock) : IPayrollWorkflowService
{
    private void Require(string permission)
    {if(!security.IsAuthenticated||!security.CurrentUserId.HasValue||!security.HasPermission(permission))throw new PayrollAccessDeniedException();}
    private static string Reason(string raw)
    {if(string.IsNullOrWhiteSpace(raw)||raw.Trim().Length is <10 or >500)throw new RequestValidationException("payroll.reason_required","A reason of10..500 characters is required.");return AuditMasker.SanitizeFreeText(raw.Trim());}
    private static Guid Key(string? raw)
    {if(raw is null||raw.Length!=36||!Guid.TryParseExact(raw,"D",out var key)||raw[14]!='4'||!"89abAB".Contains(raw[19]))throw new RequestValidationException("idempotency.invalid_key","A UUIDv4 key is required for financial workflow commands.");return key;}
    private static string Hash(object x)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(x)))).ToLowerInvariant();
    private async Task LockReceiptAsync(string op,Guid key,CancellationToken t)
    {var resource=$"HRM.Payroll.Workflow.{security.CurrentUserId:N}.{op}.{key:N}";await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51015,'Payroll workflow key busy.',1;",t);}
    private async Task<PayrollInputReceipt?> ReplayAsync(string op,Guid key,string hash,CancellationToken t)
    {var receipt=await db.PayrollInputReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==security.CurrentUserId&&x.Operation==op&&x.Key==key,t);if(receipt is not null&&receipt.RequestHash!=hash)throw new ResourceConflictException("idempotency.payload_mismatch","Workflow payload differs for this key.");return receipt;}
    private void Receipt(string op,Guid key,string hash,object response)=>db.PayrollInputReceipts.Add(new(){Id=Guid.NewGuid(),UserId=security.CurrentUserId!.Value,Operation=op,Key=key,RequestHash=hash,ResponseJson=JsonSerializer.Serialize(response),CreatedAtUtc=clock.GetUtcNow()});
    private async Task<PayrollPeriodRecord> PeriodAsync(Guid id,CancellationToken t)=>await db.PayrollPeriods.FromSqlInterpolated($"SELECT * FROM payroll.PayrollPeriods WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}").AsTracking().SingleOrDefaultAsync(t)??throw new PayrollNotFoundException("payroll.period_not_found");
    private async Task<int> VersionAsync(Guid id,CancellationToken t)=>await db.PayrollCalculationBatches.Where(x=>x.PeriodId==id).Select(x=>(int?)x.Version).MaxAsync(t)??0;
    private static PayrollWorkflowState View(PayrollPeriodRecord p,int version)=>new(p.Id,p.PeriodCode,p.Status,Convert.ToBase64String(p.RowVersion),version,p.ActiveBatchId,p.ApprovedBatchId,p.PublishedBatchId,p.CalculatedBy,p.ApprovedBy,p.PublishedBy,p.LockedAtUtc);
    private Task AuditAsync(string action,PayrollPeriodRecord p,string reason,object payload,CancellationToken t)=>audit.RecordAsync(new AuditEvent{Id=Guid.NewGuid(),OccurredAtUtc=clock.GetUtcNow(),ActorId=security.CurrentUserId,ActorUsername=security.CurrentUsername,TraceId=Activity.Current?.Id??Guid.NewGuid().ToString(),Action="payroll.workflow_"+action,EntityName="PayrollPeriod",EntityId=p.Id.ToString(),Reason=reason,Payload=AuditMasker.MaskObject(payload)},t);
    public async Task<PayrollWorkflowState> StateAsync(Guid id,CancellationToken t)
    {Require(HrmPermissions.PayrollReadAll);var p=await db.PayrollPeriods.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,t)??throw new PayrollNotFoundException("payroll.period_not_found");return View(p,await VersionAsync(id,t));}
    public async Task<PayrollWorkflowState> CreateAsync(CreatePayrollPeriodRequest request,string rawKey,CancellationToken t)
    {
        Require(HrmPermissions.PayrollCalculate);var reason=Reason(request.Reason);var key=Key(rawKey);if(request.Year is <2000 or >2100||request.Month is <1 or >12)throw new RequestValidationException("payroll.invalid_month","Year/month outside supported range.");
        const string op="workflow.create";var hash=Hash(request);await using var tx=await db.Database.BeginTransactionAsync(t);await LockReceiptAsync(op,key,t);await PayrollDataLocks.SourceAsync(db,true,t);
        var replay=await ReplayAsync(op,key,hash,t);if(replay is not null){await tx.CommitAsync(t);return JsonSerializer.Deserialize<PayrollWorkflowState>(replay.ResponseJson)!;}
        var code=$"{request.Year:D4}-{request.Month:D2}";if(await db.PayrollPeriods.AnyAsync(x=>x.PeriodCode==code,t))throw new ResourceConflictException("payroll.period_exists","The month already exists.");
        var p=new PayrollPeriodRecord{Id=Guid.NewGuid(),PeriodCode=code,Status="Draft",SnapshotRequired=true,CreatedAtUtc=clock.GetUtcNow()};db.PayrollPeriods.Add(p);await db.SaveChangesAsync(t);var response=View(p,0);Receipt(op,key,hash,response);await AuditAsync("created",p,reason,new{periodId=p.Id},t);await tx.CommitAsync(t);return response;
    }
    public async Task ExecuteAsync(Guid id,string action,PayrollWorkflowRequest request,string rawKey,CancellationToken t)
    {
        var permission=action switch{"calculate"=>HrmPermissions.PayrollCalculate,"approve"=>HrmPermissions.PayrollApprove,"publish" or "lock"=>HrmPermissions.PayrollPublish,_=>throw new RequestValidationException("payroll.invalid_action","Unknown workflow action.")};Require(permission);
        var reason=Reason(request.Reason);var key=Key(rawKey);var op="workflow."+action;var hash=Hash(new{id,request});await using var tx=await db.Database.BeginTransactionAsync(t);await LockReceiptAsync(op,key,t);await PayrollDataLocks.SourceAsync(db,true,t);
        if(await ReplayAsync(op,key,hash,t) is not null){await tx.CommitAsync(t);return;}
        var p=await PeriodAsync(id,t);var version=await VersionAsync(id,t);
        if(string.IsNullOrWhiteSpace(request.RowVersion)||request.RunVersion<0)throw new RequestValidationException("payroll.version_required","RowVersion and run version required.");
        byte[] expected;try{expected=Convert.FromBase64String(request.RowVersion);}catch(FormatException){throw new RequestValidationException("payroll.invalid_version","Invalid RowVersion.");}
        if(expected.Length!=8)throw new RequestValidationException("payroll.invalid_version","RowVersion must be8 bytes.");
        if(!expected.SequenceEqual(p.RowVersion)||request.RunVersion!=version)throw new ResourceConflictException("payroll.version_conflict","Period/run changed; reload state.");
        if(!DateOnly.TryParseExact(p.PeriodCode+"-01","yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var month)||month.Year is <2000 or >2100)throw new RequestValidationException("payroll.month_required","Real workflow requires a supported yyyy-MM month.");
        await AttendanceLocks.MonthAsync(db,month,true,t);
        var before=p.Status;var beforeBatch=p.ActiveBatchId;
        if(action=="calculate")
        {
            if(p.Status is not ("Draft" or "Calculated" or "Approved"))throw new ResourceConflictException("payroll.period_immutable","Published/locked period cannot be calculated.");
            // Clear old approval only within this transaction; capture failure restores all old state.
            p.Status="Calculated";p.ApprovedBy=null;p.ApprovedAtUtc=null;p.ApprovedBatchId=null;p.SnapshotRequired=true;
            await calculations.CaptureAsync(id,new CapturePayrollRequest(request.Reason),null,t);
            version=await VersionAsync(id,t);
        }
        else if(action=="approve")
        {
            if(p.Status!="Calculated")throw new ResourceConflictException("payroll.period_not_calculated","Only a calculated run may be approved.");
            if(!p.CalculatedBy.HasValue||p.CalculatedBy==security.CurrentUserId)throw new PayrollAccessDeniedException();
            await calculations.EnsurePublishableAsync(id,t);if(!p.ActiveBatchId.HasValue)throw new ResourceConflictException("payroll.snapshot_missing","Active run required.");
            p.Status="Approved";p.ApprovedBy=security.CurrentUserId;p.ApprovedAtUtc=clock.GetUtcNow();p.ApprovedBatchId=p.ActiveBatchId;
        }
        else if(action=="publish")
        {
            if(p.Status!="Approved"||!p.CalculatedBy.HasValue||!p.ApprovedBy.HasValue||!p.ActiveBatchId.HasValue||p.ApprovedBatchId!=p.ActiveBatchId)throw new ResourceConflictException("payroll.run_not_approved","The active run must be approved first.");
            if(p.CalculatedBy==security.CurrentUserId)throw new PayrollAccessDeniedException();
            await calculations.EnsurePublishableAsync(id,t);
            var slips=await db.Payslips.AsTracking().Where(x=>x.PeriodId==id).ToListAsync(t);var snapshots=await db.PayrollCalculationSnapshots.Where(x=>x.BatchId==p.ActiveBatchId).Select(x=>new{x.Id,x.EmployeeId,x.Net}).ToListAsync(t);
            if(slips.Count==0||slips.Count!=snapshots.Count||slips.Any(x=>!snapshots.Any(snapshot=>snapshot.Id==x.SnapshotId && snapshot.EmployeeId==x.EmployeeId && snapshot.Net==x.Amount)))throw new ResourceConflictException("payroll.run_items_mismatch","Payslips must belong to the approved run.");
            p.Status="Published";p.PublishedBy=security.CurrentUserId;p.PublishedAtUtc=clock.GetUtcNow();p.PublishedBatchId=p.ActiveBatchId;
            foreach(var slip in slips){slip.Status="Published";slip.PublishedAtUtc=p.PublishedAtUtc;}
        }
        else
        {
            if(p.Status!="Published"||!p.PublishedBatchId.HasValue||p.PublishedBatchId!=p.ActiveBatchId)throw new ResourceConflictException("payroll.period_not_published","Only a published active run may be locked.");
            p.Status="Locked";p.LockedBy=security.CurrentUserId;p.LockedAtUtc=clock.GetUtcNow();
        }
        await db.SaveChangesAsync(t);Receipt(op,key,hash,new{periodId=id,runVersion=version,batchId=p.ActiveBatchId,status=p.Status});
        await AuditAsync(action,p,reason,new{periodId=id,before,after=p.Status,beforeBatch,activeBatch=p.ActiveBatchId,runVersion=version},t);
        await tx.CommitAsync(t);
    }
}
