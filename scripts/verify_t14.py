"""T14 payroll gate: all T05-T12 matrices plus golden/unit/SQL payroll cases."""
import argparse,json,os,sys
from pathlib import Path
import xml.etree.ElementTree as ET
from verify_t05 import main,NS

WORKFLOW_CASES = {'Workflow_CreateRealPeriod_IdempotentAndNoMetadataFallbackWithoutSalary': 1, 'Workflow_FullLifecycle_BindsRunAndLockedEmployeeCanStillReadPublishedSlip': 1, 'Workflow_ConcurrentSameKeyCalculate_CreatesExactlyOneRun': 1, 'Workflow_ConcurrentDifferentKeySameVersion_OnlyOneCalculationCommits': 1, 'Workflow_DuplicatePublish_OnePublishedRunAndOneAudit': 2, 'Workflow_RecalculateApproved_ClearsOnlyOldApprovalAndOldReplayDoesNotApproveNewRun': 1, 'Workflow_RecalculateAuditFault_RestoresApprovedRunAndAllAmounts': 1, 'Workflow_OneMissingEmployee_DoesNotReplaceAnyPartOfApprovedRun': 1, 'Workflow_PublishAuditFault_RollsBackSlipsMetadataAndReceiptThenRetrySucceeds': 1, 'Workflow_LostCompletionAfterPublishCommit_ReplaysWithoutSecondPublication': 1, 'Workflow_SourceChanges_BlockApproveOrPublishUntilNewCalculation': 6, 'Workflow_CalculateVersusSalaryEdit_ProducesWholeVersionAndStaleCheckIsEnforced': 1, 'Workflow_InvalidTransitions_FailWithoutAnotherRun': 6, 'Workflow_InvalidVersionOrKey_IsRejectedWithoutRun': 4, 'Workflow_SeparationOfDuties_CalculatorCannotApproveOrPublishOwnRun': 1, 'Workflow_PermissionBoundary_DeniesNonPayrollActors': 3, 'Workflow_GetStateItemsAndPublishedOwnProjection_DoNotWriteOrExposeOtherPayslips': 1, 'Workflow_PublishedAndLockedPeriodGuards_BlockTrackedFinancialMutation': 1, 'Workflow_OutputMustMatchApprovedSnapshot_BeforePublication': 1}

def verify_workflow_matrix(path):
    rows=ET.parse(path).findall('.//t:UnitTestResult',NS);counts={}
    for case,minimum in WORKFLOW_CASES.items():
        matches=[r for r in rows if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing, skipped or failing T14 case: {case}')
        counts[case]=len(matches)
    return counts

def run():
    if not os.environ.get('HRM_T05_SQLSERVER','').strip():
        parser=argparse.ArgumentParser(add_help=False);parser.add_argument('--output');args,_=parser.parse_known_args()
        if args.output:
            p=Path(args.output).resolve();p.mkdir(parents=True,exist_ok=False)
            (p/'result.json').write_text(json.dumps({'status':'NOT_RUN','task':'T14','executed':0,'reason':'Real SQL Server is required; no skip or InMemory substitute.'},indent=2),encoding='utf-8')
        print('NOT_RUN: configure HRM_T05_SQLSERVER; T14 gate blocked.',file=sys.stderr);return 2
    return main(task='T14')

if __name__=='__main__':raise SystemExit(run())
