"""T11 SQL leave gate, preserving all T05-T10 required matrices."""
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

LEAVE_CASES = {
    'LeaveReview_HalfDayPayload_MustNotSilentlyCreateFullDay': 1,
    'LeaveReview_ReplayAfterScopeRevocation_MustReauthorize': 1,
    'LeaveReview_DisabledEmployee_MustNotReceiveFreshApproval': 1,
    'LeaveReview_PublicationMonthLock_MustSerializeApproval': 1,
    'LeaveReview_ConcurrentSameKeyApproval_MustReplayWithout500': 1,
    'LeaveReview_ZeroDayLegacyRequest_MustNotApproveWithoutDebit': 1,
    'Leave_WorkingDates_ExcludeWeekendAndHoliday_FromQuotaDeduction': 1,
    'Leave_CrossYearAndNoWorkingDays_RejectedWithClearErrors': 1,
    'Leave_Overlap_PendingOrApproved_RejectsWithConflict': 1,
    'Leave_Create_ChecksBalanceAtSubmission_PendingDoesNotDeductQuota': 1,
    'Leave_GrantQuota_CreatesBalanceAndLedger_DuplicateGrantFails': 1,
    'Leave_Approval_RecheckBalance_FailsWhenInsufficient_RollsBackAll': 1,
    'Leave_ConcurrentApprovals_TotalExceedsQuota_15Races_NeverOverdrawsOrDoubleDebits': 1,
    'Leave_ApproveRetry_SameIdempotencyKey_ReplaysWithoutSecondDeduction': 1,
    'Leave_ApproveAlreadyApproved_DifferentKey_Returns409Conflict_WithoutSecondDeduction': 1,
    'Leave_AuditFailureAfterSqlFlush_RollsBackStatusBalanceLedgerAndAudit': 1,
    'Leave_CancelPending_ByOwner_DoesNotChangeQuota': 1,
    'Leave_CancelApproved_RejectedAsUnsupportedInMvp': 1,
    'Leave_RejectPending_ByHr_SetsRejectedWithoutQuotaDeduction': 1,
    'Leave_AntiSelfApproval_HrCannotApproveOwnLeave': 1,
    'Leave_ScopeViolation_UnassignedHrCannotApproveOrAccess': 1,
    'Leave_OffboardedEmployee_CannotSubmitOrApprovePastTermination': 1,
    'Leave_ClosedPayrollPeriod_BlocksSubmissionAndApproval': 1,
    'Leave_StaleRowVersion_Returns409Conflict': 1,
    'Leave_IdempotencyKeyMismatch_Returns409Conflict': 1,
    'Leave_LedgerAndIdempotency_AreAppendOnly': 1,
    'Leave_PersonalEndpoints_AllowEmployeeToQueryOwnBalancesAndRequests': 1,
    'ValidateDates_WhenStartBeforeOrEqualEndInSameYear_Succeeds': 1,
    'ValidateDates_WhenCrossYear_ThrowsDomainRuleException': 1,
    'ValidateDates_WhenEndPrecedesStart_ThrowsDomainRuleException': 1,
    'CalculateWorkingDays_FullWorkWeek_ReturnsFiveDays': 1,
    'CalculateWorkingDays_SpanningWeekend_ExcludesWeekend': 1,
    'CalculateWorkingDays_WeekendOnly_ReturnsZeroDays': 1,
    'CalculateWorkingDays_WithHolidays_ExcludesHolidays': 1,
    'IsOverlapping_CorrectlyDetectsCollisions': 1,
}

def verify_leave_matrix(path):
    results = ET.parse(path).findall('.//t:UnitTestResult', NS)
    counts = {}
    for case, minimum in LEAVE_CASES.items():
        matches = [r for r in results if ('.' + case) in r.get('testName', '')]
        if len(matches) < minimum or any(r.get('outcome') != 'Passed' for r in matches):
            raise ValueError(f'Missing or failing leave case: {case}')
        counts[case] = len(matches)
    return counts

if __name__ == '__main__':
    raise SystemExit(main(task='T11'))
