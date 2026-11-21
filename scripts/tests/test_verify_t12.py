import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import json
import xml.etree.ElementTree as ET
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from verify_t12 import INVARIANT_CASES, verify_invariant_matrix

class T12EvidenceTests(unittest.TestCase):
    def write_trx(self,path,missing=None,skip=None):
        root=ET.Element('TestRun',xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
        results=ET.SubElement(root,'Results')
        for name,count in INVARIANT_CASES.items():
            if name==missing:continue
            for i in range(count):
                ET.SubElement(results,'UnitTestResult',testName=f'Suite.{name}({i})',outcome='NotExecuted' if name==skip else 'Passed')
        ET.ElementTree(root).write(path,encoding='utf-8',xml_declaration=True)
    def test_complete_real_run_matrix_shape_is_accepted(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write_trx(p);self.assertEqual(16,sum(verify_invariant_matrix(p).values()))
    def test_missing_invariant_blocks_acceptance(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write_trx(p,missing='T12_FaultAtCommit_Leave_RollbackOrDurableReplay')
            with self.assertRaises(ValueError):verify_invariant_matrix(p)
    def test_skipped_constraint_control_blocks_acceptance(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp)/'tests.trx';self.write_trx(p,skip='T12_PhysicalConstraints_NegativeControlDetectsRemoval')
            with self.assertRaises(ValueError):verify_invariant_matrix(p)
    def test_missing_sql_is_not_run_and_exits_nonzero(self):
        with tempfile.TemporaryDirectory() as tmp:
            output=Path(tmp)/'not-run';env=os.environ.copy();env.pop('HRM_T05_SQLSERVER',None)
            p=subprocess.run([sys.executable,'-B',str(Path(__file__).resolve().parents[1]/'verify_t12.py'),'--output',str(output)],env=env,capture_output=True,text=True,timeout=20)
            self.assertEqual(2,p.returncode);self.assertIn('NOT_RUN',p.stderr)
            data=json.loads((output/'result.json').read_text());self.assertEqual('NOT_RUN',data['status']);self.assertEqual(0,data['executed'])

if __name__=='__main__':unittest.main()
