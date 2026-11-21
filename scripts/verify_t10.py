"""T10 SQL attendance gate, preserving all T05-T09 required matrices."""
import xml.etree.ElementTree as ET
from verify_t05 import main,NS

ATTENDANCE_CASES = {'Attendance_Metrics_CountOnlyWorkingIntervals': 5, 'Attendance_MissingCheckout_AndHoliday_DoNotEarnStandardMinutes': 1, 'Attendance_ServerClock_IgnoresClientTimestamp_AndPinsCalendar': 1, 'Attendance_ConcurrentSameKeyCheckIn_ReplaysOneDurableEvent': 1, 'Attendance_ConcurrentDifferentKeys_OneCheckInOneConflict': 1, 'Attendance_KeyPayloadMismatch_RejectsButJsonOrderIsCanonical': 1, 'Attendance_KeysAreScopedToActorAndOperation': 1, 'Attendance_CheckoutBeforeCheckin_AndInvalidKeys_FailWithoutWrites': 1, 'Attendance_IncompleteDay_IsMissingAfterMidnight_AndReplayKeepsOriginal': 1, 'Attendance_ConcurrentCheckout_ReplaysOnce_AndNeverAutoApprovesOvertime': 1, 'Attendance_HrCorrection_PreservesOriginalEventsAndDedicatedAudit': 1, 'Attendance_ConcurrentHrCorrection_OneSuccessOneConflict_AndNoOpBumpsVersion': 1, 'Attendance_ScopeAndPayrollProjection_PreventIdorAndPrivilegeEscalation': 1, 'Attendance_OvertimeIsExplicitScopedAndIdempotent_WithNoDoubleCredit': 1, 'Attendance_PublishedMonth_BlocksNewAndAdjustedWork_ButAllowsOriginalReplay': 1, 'Attendance_Offboarding_RejectsNewWorkAndPreservesPriorHistory': 1, 'Attendance_AuditFailure_RollsBackSummaryEventsAndReceipt_ThenSameKeyCanRetry': 1, 'Attendance_OriginalEventsAndReceipts_AreAppendOnly_OnSyncAndAsyncSave': 1, 'Attendance_ReasonValidationAndSanitization_DoNotLeakCredentialText': 1, 'Attendance_HrCannotAdjustOrApproveOvertimeForSelf_EvenWithScope': 1, 'Attendance_TimeReversalAndInvalidCorrection_RejectWithoutNewEvidence': 1, 'Attendance_PinnedDayCalendar_SurvivesConfigurationChangeDuringPresence': 1, 'Attendance_HrCanCorrectHistoricalDayAfterOffboarding_ButCannotCreateNewWork': 1, 'Attendance_HrClearedCheckout_CannotBeOverwrittenByEmployee': 1, 'Attendance_ConcurrentOffboardingAndCheckIn_NoUsableWriteAfterCommit': 1, 'Attendance_ConcurrentPublishAndCorrection_CloseBoundaryIsEnforced': 1}

def verify_attendance_matrix(path):
    results=ET.parse(path).findall('.//t:UnitTestResult',NS)
    counts={}
    for case,minimum in ATTENDANCE_CASES.items():
        matches=[r for r in results if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing or failing attendance case: {case}')
        counts[case]=len(matches)
    return counts

if __name__=='__main__':
    raise SystemExit(main(task='T10'))
