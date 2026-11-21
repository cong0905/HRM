import sys
import tempfile
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_t07 import AUTHORIZATION_CASES, verify_authorization_matrix


class AuthorizationEvidenceGateTests(unittest.TestCase):
    def document(self):
        root = ET.Element('{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}TestRun')
        for case in AUTHORIZATION_CASES:
            ET.SubElement(root, '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult',
                          testName='Tests.' + case, outcome='Passed')
        return root

    def check(self, root):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'auth.trx'
            ET.ElementTree(root).write(path, encoding='utf-8')
            return verify_authorization_matrix(path)

    def test_complete_authorization_matrix_is_accepted(self):
        self.assertEqual(sum(self.check(self.document()).values()), len(AUTHORIZATION_CASES))

    def test_missing_case_is_rejected(self):
        root = self.document()
        target = next(r for r in root if 'IDOR_EmployeeCannotReadOrModifyOtherEmployeeData_' in r.get('testName', ''))
        root.remove(target)
        with self.assertRaisesRegex(ValueError, 'IDOR_EmployeeCannotReadOrModifyOtherEmployeeData_'):
            self.check(root)

    def test_skipped_case_is_rejected(self):
        root = self.document()
        next(r for r in root if 'SystemAdmin_WithoutPayrollPermission_' in r.get('testName', '')).set('outcome', 'NotExecuted')
        with self.assertRaisesRegex(ValueError, 'SystemAdmin_WithoutPayrollPermission_'):
            self.check(root)


if __name__ == '__main__':
    unittest.main()

