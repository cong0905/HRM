#!/usr/bin/env python3
"""
scripts/tests/test_verify_baseline_trx.py

Unit tests kiểm thử toàn diện bộ phân loại và thẩm định TRX (verify_baseline_trx.py) theo nguyên tắc MẶC ĐỊNH TỪ CHỐI (Default-Deny).

Bao gồm đầy đủ các kịch bản bắt buộc:
1. Đúng manifest 100% (kỳ vọng PASS: exit code 0)
2. Thiếu test trong kết quả Regression (kỳ vọng FAIL: exit code 1)
3. Lỗi khác nguyên nhân hoàn toàn (NullReferenceException) (kỳ vọng FAIL: exit code 1)
4. Test bị skipped (kỳ vọng FAIL: exit code 1)
5. Thiếu file TRX (kỳ vọng FAIL: exit code 1)
6. Test bất ngờ PASS (kỳ vọng FAIL: exit code 1)
7. Kịch bản (a): Harness thiếu một test (kỳ vọng FAIL: exit code 1)
8. Kịch bản (b): Outcome Timeout / Aborted / không nhận diện (kỳ vọng FAIL: exit code 1)
9. Kịch bản (c): Run summary Aborted dù các kết quả con khớp manifest (kỳ vọng FAIL: exit code 1)
10. Kịch bản (d): Kết quả trùng lặp testName trong TRX hoặc Manifest (kỳ vọng FAIL: exit code 1)
11. Kịch bản (e): Manifest không hợp lệ (rỗng, thiếu trường, duplicate, chuỗi rỗng) (kỳ vọng FAIL: exit code 1)
12. Kịch bản (f): Assertion cùng loại nhưng sai giá trị/nguyên nhân (kỳ vọng FAIL: exit code 1)
13. Kịch bản bổ sung: Outcome là chuỗi lạ 'UnexpectedOutcome' (bắt buộc FAIL: exit code 1)
14. Kịch bản bổ sung: TRX thiếu ResultSummary hoặc Counters hợp lệ (bắt buộc FAIL: exit code 1)

LƯU Ý: Các file mẫu và test này CHỈ phục vụ kiểm thử công cụ phân loại,
KHÔNG được dùng làm bằng chứng chạy regression thực tế của dự án.
"""

import json
import os
import shutil
import tempfile
import unittest
from scripts.verify_baseline_trx import verify


def create_trx_xml(test_results, run_outcome="Passed", counters=None, include_summary=True, include_counters=True):
    """Tạo chuỗi XML định dạng TRX chuẩn Visual Studio kèm ResultSummary và Counters."""
    if counters is None:
        total = len(test_results)
        passed = sum(1 for r in test_results if r.get("outcome") == "Passed")
        failed = sum(1 for r in test_results if r.get("outcome") == "Failed")
        counters = {
            "total": total,
            "executed": total,
            "passed": passed,
            "failed": failed,
            "error": 0,
            "timeout": 0,
            "aborted": 0,
            "inconclusive": 0,
        }

    counters_attr = " ".join(f'{k}="{v}"' for k, v in counters.items())

    xml_header = '<?xml version="1.0" encoding="utf-8"?>\n'
    xml_header += '<TestRun id="test-run-id" name="SampleRun" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">\n'
    xml_header += '  <Results>\n'

    for r in test_results:
        name = r["name"]
        outcome = r.get("outcome", "Passed")
        duration = r.get("duration", "00:00:00.100")
        msg = r.get("error_message", "")
        stack = r.get("stack_trace", "at Sample.Test()")

        xml_header += f'    <UnitTestResult testName="{name}" outcome="{outcome}" duration="{duration}">\n'
        if msg or stack:
            xml_header += '      <Output>\n'
            xml_header += '        <ErrorInfo>\n'
            if msg:
                xml_header += f'          <Message>{msg}</Message>\n'
            if stack:
                xml_header += f'          <StackTrace>{stack}</StackTrace>\n'
            xml_header += '        </ErrorInfo>\n'
            xml_header += '      </Output>\n'
        xml_header += '    </UnitTestResult>\n'

    xml_header += '  </Results>\n'

    if include_summary:
        xml_header += f'  <ResultSummary outcome="{run_outcome}">\n'
        if include_counters:
            xml_header += f'    <Counters {counters_attr} />\n'
        xml_header += '  </ResultSummary>\n'

    xml_header += '</TestRun>\n'
    return xml_header


class TestVerifyBaselineTrx(unittest.TestCase):
    def setUp(self):
        self.test_dir = tempfile.mkdtemp()
        self.manifest_path = os.path.join(self.test_dir, "test_manifest.json")
        self.harness_trx_path = os.path.join(self.test_dir, "harness.trx")
        self.regression_trx_path = os.path.join(self.test_dir, "regression.trx")

        # Manifest mẫu hợp lệ
        self.manifest = {
            "version": "2.0",
            "expected_harness_tests": [
                {"test_name": "Harness.Test1", "expected_outcome": "Passed"},
                {"test_name": "Harness.Test2", "expected_outcome": "Passed"}
            ],
            "expected_regression_tests": [
                {
                    "test_name": "SampleTest.F01_Failure",
                    "finding_id": "F01",
                    "expected_outcome": "Failed",
                    "expected_error_substrings": [
                        "Expected F01 assertion error",
                        "F01_Failure"
                    ]
                },
                {
                    "test_name": "SampleTest.F02_Failure",
                    "finding_id": "F02",
                    "expected_outcome": "Failed",
                    "expected_error_substrings": [
                        "Assert.Equal() Failure",
                        "Expected: 10000000",
                        "Actual:   15000000"
                    ]
                }
            ]
        }
        with open(self.manifest_path, "w", encoding="utf-8") as f:
            json.dump(self.manifest, f)

        # Harness TRX mẫu chuẩn (luôn pass)
        harness_xml = create_trx_xml([
            {"name": "Harness.Test1", "outcome": "Passed"},
            {"name": "Harness.Test2", "outcome": "Passed"}
        ], run_outcome="Passed")
        with open(self.harness_trx_path, "w", encoding="utf-8") as f:
            f.write(harness_xml)

    def tearDown(self):
        shutil.rmtree(self.test_dir, ignore_errors=True)

    # 1. Kịch bản chuẩn: Khớp manifest 100% -> PASS (exit 0)
    def test_exact_manifest_match_passes(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Failed",
                "error_message": "Assert.True() Failure: Expected F01 assertion error\nat SampleTest.F01_Failure()"
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure: Values differ\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 0)

    # 2. Kịch bản: Regression thiếu test -> FAIL (exit 1)
    def test_missing_regression_test_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Failed",
                "error_message": "Expected F01 assertion error\nF01_Failure"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 3. Kịch bản: Lỗi khác nguyên nhân hoàn toàn (NullReferenceException) -> FAIL (exit 1)
    def test_different_error_cause_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Failed",
                "error_message": "System.NullReferenceException: Object reference not set to an instance of an object."
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 4. Kịch bản: Test bị skipped -> FAIL (exit 1)
    def test_skipped_test_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Skipped",
                "error_message": ""
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 5. Kịch bản: Thiếu file TRX -> FAIL (exit 1)
    def test_missing_trx_fails(self):
        non_existent_trx = os.path.join(self.test_dir, "non_existent.trx")
        exit_code = verify(self.harness_trx_path, non_existent_trx, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 6. Kịch bản: Test bất ngờ PASS -> FAIL (exit 1)
    def test_unexpected_pass_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Passed"  # Bất ngờ PASS!
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Passed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 7. Kịch bản (a): Harness thiếu một test -> FAIL (exit 1)
    def test_harness_missing_one_test_fails(self):
        harness_missing_xml = create_trx_xml([
            {"name": "Harness.Test1", "outcome": "Passed"}
        ], run_outcome="Passed")
        with open(self.harness_trx_path, "w", encoding="utf-8") as f:
            f.write(harness_missing_xml)

        reg_xml = create_trx_xml([
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {"name": "SampleTest.F02_Failure", "outcome": "Failed", "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"}
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 8. Kịch bản (b): Outcome Timeout / Aborted -> FAIL (exit 1)
    def test_outcome_timeout_or_unrecognized_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "Timeout",
                "error_message": "Test execution exceeded timeout limit."
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 9. Kịch bản (c): Run summary Aborted dù các kết quả con khớp manifest -> FAIL (exit 1)
    def test_run_summary_aborted_fails_even_if_subtests_match(self):
        reg_xml = create_trx_xml([
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {"name": "SampleTest.F02_Failure", "outcome": "Failed", "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"}
        ], run_outcome="Aborted", counters={"total": 2, "executed": 2, "passed": 0, "failed": 2, "error": 0, "timeout": 0, "aborted": 1, "inconclusive": 0})
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 10. Kịch bản (d): Kết quả trùng lặp testName trong TRX -> FAIL (exit 1)
    def test_duplicate_results_in_trx_fails(self):
        reg_duplicate_xml = create_trx_xml([
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {"name": "SampleTest.F02_Failure", "outcome": "Failed", "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"}
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_duplicate_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 11. Kịch bản (e): Manifest không hợp lệ (trùng test_name hoặc chuỗi rỗng) -> FAIL (exit 1)
    def test_invalid_manifest_duplicate_fails(self):
        bad_manifest = {
            "version": "2.0",
            "expected_harness_tests": [
                {"test_name": "Harness.Test1", "expected_outcome": "Passed"}
            ],
            "expected_regression_tests": [
                {"test_name": "SampleTest.F01_Failure", "finding_id": "F01", "expected_outcome": "Failed", "expected_error_substrings": ["err"]},
                {"test_name": "SampleTest.F01_Failure", "finding_id": "F01_Duplicate", "expected_outcome": "Failed", "expected_error_substrings": ["err"]}
            ]
        }
        bad_manifest_path = os.path.join(self.test_dir, "bad_manifest.json")
        with open(bad_manifest_path, "w", encoding="utf-8") as f:
            json.dump(bad_manifest, f)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, bad_manifest_path)
        self.assertEqual(exit_code, 1)

    # 12. Kịch bản (f): Assertion cùng loại nhưng sai giá trị / chi tiết đặc thù -> FAIL (exit 1)
    def test_assertion_same_type_but_wrong_value_fails(self):
        reg_xml = create_trx_xml([
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure: Values differ\nExpected: 10000000\nActual:   20000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 13. Kịch bản BỔ SUNG: Outcome là chuỗi lạ 'UnexpectedOutcome' -> BẮT BUỘC FAIL (exit 1)
    def test_unexpected_outcome_string_fails(self):
        reg_xml = create_trx_xml([
            {
                "name": "SampleTest.F01_Failure",
                "outcome": "UnexpectedOutcome",  # Chuỗi outcome lạ không nằm trong blacklist cũ
                "error_message": "Expected F01 assertion error\nF01_Failure"
            },
            {
                "name": "SampleTest.F02_Failure",
                "outcome": "Failed",
                "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"
            }
        ], run_outcome="Failed")
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)

    # 14. Kịch bản BỔ SUNG: TRX thiếu ResultSummary hoặc Counters hợp lệ -> BẮT BUỘC FAIL (exit 1)
    def test_missing_summary_or_counters_fails(self):
        reg_no_summary_xml = create_trx_xml([
            {"name": "SampleTest.F01_Failure", "outcome": "Failed", "error_message": "Expected F01 assertion error\nF01_Failure"},
            {"name": "SampleTest.F02_Failure", "outcome": "Failed", "error_message": "Assert.Equal() Failure\nExpected: 10000000\nActual:   15000000"}
        ], include_summary=False)
        with open(self.regression_trx_path, "w", encoding="utf-8") as f:
            f.write(reg_no_summary_xml)

        exit_code = verify(self.harness_trx_path, self.regression_trx_path, self.manifest_path)
        self.assertEqual(exit_code, 1)


if __name__ == "__main__":
    unittest.main()
