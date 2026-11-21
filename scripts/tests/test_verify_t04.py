"""Security gate tests: missing audit evidence must never become a green result."""
import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_t04 import inspect_packages


class T04AuditGateTests(unittest.TestCase):
    def report(self, framework=None):
        return {'projects': [{'path': 'sample.csproj', 'frameworks': [framework or {
            'framework': 'net10.0',
            'topLevelPackages': [{'id': 'Example', 'resolvedVersion': '1.0.0'}]}]}]}

    def test_clean_resolved_report_passes(self):
        self.assertEqual(1, inspect_packages(self.report(), require_resolved=True))

    def test_transitive_vulnerability_is_rejected(self):
        report = self.report()
        report['projects'][0]['frameworks'][0]['transitivePackages'] = [{
            'id': 'System.Text.Json', 'resolvedVersion': '8.0.0',
            'vulnerabilities': [{'severity': 'High', 'advisoryurl': 'https://example.invalid/advisory'}]}]
        with self.assertRaisesRegex(ValueError, 'Vulnerabilities'):
            inspect_packages(report)

    def test_missing_projects_is_rejected(self):
        with self.assertRaises(ValueError):
            inspect_packages({})

    def test_missing_framework_is_rejected(self):
        with self.assertRaises(ValueError):
            inspect_packages({'projects': [{'path': 'sample', 'frameworks': []}]})

    def test_unsupported_target_framework_is_rejected(self):
        with self.assertRaises(ValueError):
            inspect_packages(self.report({'framework': 'net8.0'}))

    def test_missing_resolved_dependencies_is_rejected(self):
        with self.assertRaises(ValueError):
            inspect_packages(self.report({'framework': 'net10.0'}), require_resolved=True)

    def test_empty_vulnerability_list_is_valid(self):
        self.assertEqual(1, inspect_packages(self.report({'framework': 'net10.0'})))

    def test_error_from_audit_source_is_rejected(self):
        report = self.report()
        report['logs'] = [{'level': 'error', 'message': 'Source unavailable'}]
        with self.assertRaises(ValueError):
            inspect_packages(report)

    def test_package_without_version_is_rejected(self):
        with self.assertRaises(ValueError):
            inspect_packages(self.report({'framework': 'net10.0', 'topLevelPackages': [{'id': 'Example'}]}))


if __name__ == '__main__':
    unittest.main()
