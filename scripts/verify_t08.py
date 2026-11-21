"""T08 employee profile and account lifecycle gate: complete SQL personnel matrix plus T07 authorization, auth, contracts, and legacy gate."""
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

T08_CASES = (
    'Employee_Create_WithInactiveAccount_AndFixedEmployeeRole_Success',
    'Employee_Create_DuplicateCodeOrEmailOrUsername_FailsWithConflict',
    'Employee_Create_InvalidDates_Returns400',
    'Employee_Create_InvalidDepartment_Returns400',
    'Employee_Create_AuditFailure_RollsBackAllChanges',
    'Employee_Create_SenderNotConfigured_ReportsPendingNotSent',
    'Employee_Create_DoesNotAutoAssignHrScope_RequiresAdminAssignment',
    'Employee_CodeGeneration_ConcurrentRequests_ProduceUniqueCodes',
    'Employee_CodeGeneration_Exceeds9999_FailsWithClearError',
    'Employee_Patch_AbsentVsNullVsValue',
    'Employee_Patch_ConcurrencyRowVersion_OneSuccessOneConflict',
    'Employee_Offboarding_RevokesSessions_DisablesUser_AndSetsDaNghiViec',
    'Employee_Offboarded_InactiveInvitedUser_CannotActivateOrResetEvenWithToken',
    'Employee_Offboarding_RetainsPayslipAndLeaveHistory_PreventsLeaveApprovalAfterTermination',
    'Employee_Offboarded_CannotBePatchedOrOffboardedAgain',
    'Employee_Create_InvalidIdentityUsername_RollsBackProfileAccountAndOutbox',
    'Employee_Create_SenderFailure_PreservesCommittedWorkflowAndReportsPendingRetry',
    'Employee_Patch_NullRequiredAndInvalidDates_DoNotMutateProfile',
    'Employee_Patch_TrueConcurrentRequests_OneSuccessOneConflict',
    'Employee_Offboarding_RejectsCookieBearerRefreshAndRetainsSessionAudit',
    'Employee_Migration_ExistingProfilesBackfilledAndYearCounterContinues',
)


def verify_t08_matrix(path):
    results = ET.parse(path).findall('.//t:UnitTestResult', NS)
    counts = {}
    for case in T08_CASES:
        matched = [r for r in results if ('.' + case) in r.get('testName', '')]
        if len(matched) < 1 or any(r.get('outcome') != 'Passed' for r in matched):
            raise ValueError(f'Missing or failing T08 case: {case}')
        counts[case] = len(matched)
    return counts


if __name__ == '__main__':
    raise SystemExit(main(task='T08'))
