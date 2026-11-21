import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from contracts import canonical, check_contract


class ContractDriftTests(unittest.TestCase):
    def test_property_order_does_not_create_false_drift(self):
        check_contract(canonical({'paths': {}, 'openapi': '3.1.1'}), canonical({'openapi': '3.1.1', 'paths': {}}))

    def test_added_endpoint_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'OpenAPI drift'):
            check_contract(canonical({'paths': {}}), canonical({'paths': {'/new': {}}}))

    def test_changed_type_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'OpenAPI drift'):
            check_contract(canonical({'amount': {'type': 'string'}}), canonical({'amount': {'type': 'number'}}))

    def test_empty_checked_in_contract_is_rejected(self):
        with self.assertRaises(RuntimeError):
            check_contract('', canonical({'paths': {}}))


if __name__ == '__main__':
    unittest.main()
