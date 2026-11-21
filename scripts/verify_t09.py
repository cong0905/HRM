"""T09 calendar gate, including every required T08/auth/authorization case."""
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

CALENDAR_CASES = {
    'Calendar_UtcBoundary_MapsToVietnamDate': 4,
    'Calendar_EligibleMinutes_ClipsShiftAndSubtractsOnlyBreakOverlap': 7,
    'Calendar_IncompleteAndWeekendHoliday_DoNotCreateStandardWork': 1,
    'Calendar_StandardMonth_MatchesApprovedFixturesAndLeapYear': 4,
    'Calendar_AprilDemoWithoutHolidays_Has22Days': 1,
    'Calendar_InvalidShiftBreaksAndAttendance_FailClosed': 1,
    'Calendar_VersionEffectiveDates_AndMonthlyFixtures_AreConsistent': 1,
    'Calendar_ConcurrentVersionCreation_OneCreatedOneConflict_AndNoDefaultCoverage': 1,
    'Calendar_Authorization_SeparatesConfigurationFromPayrollAndEmployeeRead': 1,
    'Calendar_PayrollCalculationPinsSnapshot_AndClosedMonthSurvivesFutureVersion': 1,
    'Calendar_ConcurrentSnapshotCapture_IsDurablyIdempotent': 1,
    'Calendar_AuditFailure_RollsBackConfigurationAndCalculationSnapshot': 1,
    'Calendar_ImmutableVersionsAndSnapshots_RejectTrackedMutation': 1,
    'Calendar_InvalidRequestsAndMissingSnapshots_FailClosed': 1,
    'Calendar_Clock_UsesInjectedUtcClock_AndIgnoresClientTime': 1,
}

def verify_calendar_matrix(path):
    results=ET.parse(path).findall('.//t:UnitTestResult',NS)
    counts={}
    for case, minimum in CALENDAR_CASES.items():
        matches=[r for r in results if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing or failing calendar case: {case}')
        counts[case]=len(matches)
    return counts

if __name__=='__main__':
    raise SystemExit(main(task='T09'))
