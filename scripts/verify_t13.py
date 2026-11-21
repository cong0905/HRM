"""T13 payroll gate: all T05-T12 matrices plus golden/unit/SQL payroll cases."""
import argparse,json,os,sys
from pathlib import Path
import xml.etree.ElementTree as ET
from verify_t05 import main,NS

PAYROLL_CASES = {'Payroll_Golden_T00_PayrollCases_MatchIndependentNumbers': 8, 'Payroll_MissingCheckout_CreditsZeroAndBlocksUntilCorrected': 1, 'Payroll_EmploymentJoinOrLeaveMidMonth_UsesCompanyDenominator': 2, 'Payroll_VariableShift_CapsDirtyAttendancePlusLeaveAt420': 1, 'Payroll_PrecisionAndRoundingAdjustment_NoIntermediateMoneyRounding': 1, 'Payroll_NegativeUnroundedNet_BlocksEvenWhenDisplayIsZero': 2, 'Payroll_ConfirmedOvertime_AppliesExplicitDayKindOnly': 3, 'Payroll_InvalidOrMissingInputs_FailClosed': 8, 'Payroll_PerformanceOff_IsNeutralRegardlessOfScore': 3, 'Payroll_FullMonthDeductedExactly_NetZeroIsPublishable': 4, 'PayrollSql_SalaryAppend_ClosesPreviousAndConcurrentDateIsUnique': 1, 'PayrollSql_PermissionBoundary_HrAdminEmployeeCannotReadOrWriteSalary': 1, 'PayrollSql_SalaryIdempotency_ConcurrentReplayAndPayloadMismatch': 1, 'PayrollSql_Golden01_ActualFactsAndManuals_CreatePublishedOwnPayslip': 1, 'PayrollSql_Golden09_LockedSnapshotRemains10740000_AfterFutureSalary': 1, 'PayrollSql_Golden10_NegativeNetBlocksWorkflow_ThenVoidAndReplaceDeduction': 1, 'PayrollSql_MissingSalaryOrHireDate_RollsBackEntireBatch': 2, 'PayrollSql_Golden02_MissingCheckoutBlocksUntilAuditedCorrection': 1, 'PayrollSql_ChangedInputs_BlockBothApproveAndPublish': 1, 'PayrollSql_ImmutabilityGuards_ProtectSnapshotSalaryManualAndPublishedSlip': 1, 'PayrollSql_SalaryAuditFailure_RollsBackOldClosureNewRateAndReceipt': 1, 'PayrollSql_CaptureAuditFailure_RollsBackCalendarBatchSlipsAndState': 2, 'PayrollSql_Golden05_InsufficientLeaveSubmission_Is422WithoutMutation': 1, 'PayrollSql_Golden06_BothDeterministicApprovalOrders_NeverOverdraw': 2, 'PayrollSql_ConcurrentCaptureSameKey_OneBatchAndIdenticalReplay': 1, 'PayrollSql_ExactMoneyContract_RejectsUnknownNumericAndMissingKey': 1, 'PayrollSql_CaptureVersusSalaryEdit_IsEntireOldOrNewVersion': 1, 'PayrollSql_ReadSnapshotsAndPayslips_AreReadOnly': 1, 'PayrollSql_SalaryPhysicalConstraints_RejectDuplicateDateSecondOpenAndInvalidRange': 1, 'PayrollSql_OffboardedMidMonthEmployee_IsIncludedWithProratedPay': 1, 'PayrollSql_DirectSnapshotCapture_RecordsActualCalculatorAndPreventsSelfApproval': 1}

def verify_payroll_matrix(path):
    rows=ET.parse(path).findall('.//t:UnitTestResult',NS);counts={}
    for case,minimum in PAYROLL_CASES.items():
        matches=[r for r in rows if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing, skipped or failing T13 case: {case}')
        counts[case]=len(matches)
    return counts

def run():
    if not os.environ.get('HRM_T05_SQLSERVER','').strip():
        parser=argparse.ArgumentParser(add_help=False);parser.add_argument('--output');args,_=parser.parse_known_args()
        if args.output:
            p=Path(args.output).resolve();p.mkdir(parents=True,exist_ok=False)
            (p/'result.json').write_text(json.dumps({'status':'NOT_RUN','task':'T13','executed':0,'reason':'Real SQL Server is required; no skip or InMemory substitute.'},indent=2),encoding='utf-8')
        print('NOT_RUN: configure HRM_T05_SQLSERVER; T13 gate blocked.',file=sys.stderr);return 2
    return main(task='T13')

if __name__=='__main__':raise SystemExit(run())
