"""T12 SQL invariants gate. No SQL configuration is NOT_RUN and exits nonzero."""
import argparse
import json
import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

INVARIANT_CASES = {
    'T12_ScopedKeys_CrossActorAndOperation_CanonicalHashMismatch': 1,
    'T12_Retention_RestartAnd400DayRetry_KeepOriginalReceipt': 1,
    'T12_FaultAtCommit_Attendance_RollbackOrDurableReplay': 2,
    'T12_FaultAtCommit_Leave_RollbackOrDurableReplay': 2,
    'T12_ApprovedFullDayLeave_BlocksCheckIn': 1,
    'T12_PendingLeaveAllowsWork_ApprovalRequiresZeroCorrection_ThenBlocksPaidWorkAndOt': 1,
    'T12_CheckInVersusApprove_Seeded10Races_ExactlyOneWins': 1,
    'T12_NonWorkingDayWithinApprovedSpan_EarnsNoStandardOrAutomaticOt': 1,
    'T12_Offboard_BlocksBothModules_AndPreservesLedger': 1,
    'T12_Idor_CrossModuleLinks_DoNotExposeOtherEmployees': 1,
    'T12_PhysicalConstraints_NegativeControlDetectsRemoval': 3,
    'T12_AuditFailure_MustRollbackAlreadyFlushedLeaveMutation': 1,
}

def verify_invariant_matrix(path):
    results=ET.parse(path).findall('.//t:UnitTestResult',NS)
    counts={}
    for case,minimum in INVARIANT_CASES.items():
        matches=[r for r in results if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing, failing or skipped T12 invariant: {case}')
        counts[case]=len(matches)
    return counts

def run():
    if not os.environ.get('HRM_T05_SQLSERVER','').strip():
        parser=argparse.ArgumentParser(add_help=False);parser.add_argument('--output')
        args,_=parser.parse_known_args()
        if args.output:
            output=Path(args.output).resolve();output.mkdir(parents=True,exist_ok=False)
            (output/'result.json').write_text(json.dumps({'status':'NOT_RUN','task':'T12','executed':0,
                'reason':'HRM_T05_SQLSERVER is required; real SQL tests cannot be substituted or skipped.'},indent=2),encoding='utf-8')
        print('NOT_RUN: configure HRM_T05_SQLSERVER; T12 gate is blocked.',file=sys.stderr)
        return 2
    return main(task='T12')

if __name__=='__main__':
    raise SystemExit(run())
