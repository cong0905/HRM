"""T15 gate: all T05-T14 matrices plus payslip/reconciliation/CSV cases."""
import argparse,json,os,sys
from pathlib import Path
import xml.etree.ElementTree as ET
from verify_t05 import main,NS

PAYSLIP_CASES = {'Payslip_DetailAndSummary_ReconcileAllEmployeesAndSignedRoundingLines': 1, 'Payslip_OwnerCannotReadCalculatedOrApprovedDetail': 2, 'Payslip_OwnerPublishedAndLockedDetail_PreventsIdor': 1, 'Payslip_NonPayrollRolesCannotReadReconcileOrExport': 3, 'Payslip_MultipleRoles_DoNotGrantExportToHrEmployee': 1, 'Payslip_CsvFormulaNamesAndCodes_AreTextCells': 4, 'Payslip_CsvUtf8QuotesNewlinesAndAudit_ExcludePrivateFieldsAndMoneyPayload': 1, 'Payslip_InvalidExportLimits_AreBadRequests': 3, 'Payslip_DraftExportIsNotAStatement': 1, 'Payslip_ExportAuditFailure_DoesNotReturnFileOrPersistPartialAudit': 1, 'Payslip_DetailAndReconciliationQueries_DoNotWrite': 1, 'Payslip_HistoricalMoneyUsesPublishedSnapshot_AfterSalaryAndNameChange': 1, 'Payslip_ConcurrentPublishAndExport_ReturnsWholePublishedRunOrConflict': 1, 'Payslip_MissingResources_AreNotFound': 1, 'Payslip_MetadataOnlyPeriodCannotPretendToReconcile': 1, 'Payslip_ExportRejectsStoredAmountThatDiffersFromPublishedSnapshot': 1, 'Payslip_CsvUntrustedText_IsQuotedAndFormulaSafe': 12}

def verify_payslip_matrix(path):
    rows=ET.parse(path).findall('.//t:UnitTestResult',NS);counts={}
    for case,minimum in PAYSLIP_CASES.items():
        matches=[r for r in rows if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing, skipped or failing T15 case: {case}')
        counts[case]=len(matches)
    return counts

def run():
    if not os.environ.get('HRM_T05_SQLSERVER','').strip():
        parser=argparse.ArgumentParser(add_help=False);parser.add_argument('--output');args,_=parser.parse_known_args()
        if args.output:
            p=Path(args.output).resolve();p.mkdir(parents=True,exist_ok=False)
            (p/'result.json').write_text(json.dumps({'status':'NOT_RUN','task':'T15','executed':0,'reason':'Real SQL Server is required; no skip or InMemory substitute.'},indent=2),encoding='utf-8')
        print('NOT_RUN: configure HRM_T05_SQLSERVER; T15 gate blocked.',file=sys.stderr);return 2
    return main(task='T15')

if __name__=='__main__':raise SystemExit(run())
