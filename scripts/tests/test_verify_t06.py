import sys
import tempfile
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_t06 import AUTH_CASES, verify_auth_matrix


class AuthEvidenceGateTests(unittest.TestCase):
    def document(self):
        root = ET.Element('{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}TestRun')
        for case in AUTH_CASES:
            for i in range(6 if case.startswith('CommittedRevocation_') else 1):
                ET.SubElement(root, '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult',
                              testName='Tests.' + case + f'({i})', outcome='Passed')
        return root

    def check(self, root):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'auth.trx'
            ET.ElementTree(root).write(path, encoding='utf-8')
            return verify_auth_matrix(path)

    def test_complete_auth_matrix_is_accepted(self):
        self.assertEqual(sum(self.check(self.document()).values()), 23)

    def test_missing_revoke_transport_case_is_rejected(self):
        root = self.document()
        target = next(r for r in root if 'CommittedRevocation_' in r.get('testName', ''))
        root.remove(target)
        with self.assertRaisesRegex(ValueError, 'CommittedRevocation_'):
            self.check(root)

    def test_skipped_reset_security_case_is_rejected(self):
        root = self.document()
        next(r for r in root if 'ResetTokens_' in r.get('testName', '')).set('outcome', 'NotExecuted')
        with self.assertRaisesRegex(ValueError, 'ResetTokens_'):
            self.check(root)


if __name__ == '__main__':
    unittest.main()
