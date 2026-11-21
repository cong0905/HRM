import json,os,subprocess,sys,tempfile,unittest
from pathlib import Path
import xml.etree.ElementTree as ET
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from verify_t15 import PAYSLIP_CASES,verify_payslip_matrix

class T15EvidenceTests(unittest.TestCase):
    def write(self,path,missing=None,skip=None):
        root=ET.Element('TestRun',xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010');rows=ET.SubElement(root,'Results')
        for name,count in PAYSLIP_CASES.items():
            if name==missing:continue
            for i in range(count):ET.SubElement(rows,'UnitTestResult',testName=f'Suite.{name}({i})',outcome='NotExecuted' if name==skip else 'Passed')
        ET.ElementTree(root).write(path,encoding='utf-8',xml_declaration=True)
    def test_complete_payroll_matrix_is_accepted(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write(p);self.assertEqual(36,sum(verify_payslip_matrix(p).values()))
    def test_missing_salary_race_blocks_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write(p,missing='Payslip_ExportRejectsStoredAmountThatDiffersFromPublishedSnapshot')
            with self.assertRaises(ValueError):verify_payslip_matrix(p)
    def test_skipped_golden_case_blocks_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write(p,skip='Payslip_ExportAuditFailure_DoesNotReturnFileOrPersistPartialAudit')
            with self.assertRaises(ValueError):verify_payslip_matrix(p)
    def test_no_sql_records_not_run_and_blocks_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            output=Path(tmp)/'no-sql';env=os.environ.copy();env.pop('HRM_T05_SQLSERVER',None)
            c=subprocess.run([sys.executable,'-B',str(Path(__file__).resolve().parents[1]/'verify_t15.py'),'--output',str(output)],env=env,capture_output=True,text=True,timeout=20)
            self.assertEqual(2,c.returncode);self.assertIn('NOT_RUN',c.stderr);data=json.loads((output/'result.json').read_text());self.assertEqual('NOT_RUN',data['status']);self.assertEqual(0,data['executed'])
