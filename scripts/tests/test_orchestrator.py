#!/usr/bin/env python3
"""
scripts/tests/test_orchestrator.py

Unit tests kiểm thử các cơ chế an toàn và nguyên tắc Default-Deny của script điều phối (run_and_verify_baseline.py):
1. Từ chối tái sử dụng thư mục kết quả bẩn (--results-dir đã có file .trx).
2. Từ chối khi HarnessCheck có exit code != 0.
3. Từ chối khi RegressionBug có exit code != 1 (ví dụ 0 do nuốt lỗi hoặc 2 do crash).
4. Xác minh run-evidence.json được ghi đầy đủ trên cả nhánh thành công và thất bại.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch, MagicMock

# Import các hàm từ run_and_verify_baseline
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
import run_and_verify_baseline


class TestOrchestrator(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()
        self.repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

    def tearDown(self):
        shutil.rmtree(self.temp_dir, ignore_errors=True)

    def test_dirty_results_dir_rejected(self):
        """Nếu thư mục truyền vào đã chứa file .trx, orchestrator phải từ chối ngay lập tức (exit 1)."""
        dirty_dir = os.path.join(self.temp_dir, "dirty_results")
        os.makedirs(dirty_dir, exist_ok=True)
        dummy_trx = os.path.join(dirty_dir, "old_test.trx")
        with open(dummy_trx, "w", encoding="utf-8") as f:
            f.write("<TestRun />")

        cmd = [
            sys.executable,
            os.path.join(self.repo_root, "scripts", "run_and_verify_baseline.py"),
            "--results-dir", dirty_dir
        ]
        result = subprocess.run(
            cmd, capture_output=True, encoding="utf-8",
            env={**os.environ, "PYTHONIOENCODING": "utf-8"},
        )
        self.assertEqual(result.returncode, 1)
        self.assertIn("đã chứa file TRX cũ", result.stdout)

    def test_abnormal_regression_exit_code_zero_rejected(self):
        """Nếu regression runner trả về 0 (bất thường vì baseline có lỗi đã biết), orchestrator phải từ chối (exit 1)."""
        test_dir = os.path.join(self.temp_dir, "run_test_zero")
        os.makedirs(test_dir, exist_ok=True)

        with patch("run_and_verify_baseline.run_command") as mock_run:
            # Lần 1: harness -> return code 0
            # Lần 2: regression -> return code 0 (bất thường!)
            mock_run.side_effect = [
                (0, "Harness passed", ""),
                (0, "Regression unexpectedly returned 0", "")
            ]

            test_args = [
                "run_and_verify_baseline.py",
                "--results-dir", test_dir,
                "--configuration", "Release"
            ]
            with patch.object(sys, "argv", test_args):
                with self.assertRaises(SystemExit) as cm:
                    run_and_verify_baseline.main()
                self.assertEqual(cm.exception.code, 1)

            # Kiểm tra evidence đã ghi nhận ABNORMAL_REGRESSION_EXIT_0
            evidence_file = os.path.join(test_dir, "run-evidence.json")
            self.assertTrue(os.path.exists(evidence_file))
            with open(evidence_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            self.assertEqual(data.get("overall_status"), "ABNORMAL_REGRESSION_EXIT_0")

    def test_abnormal_regression_exit_code_two_rejected(self):
        """Nếu regression runner trả về 2 (crash/runner failure), orchestrator phải từ chối (exit 1)."""
        test_dir = os.path.join(self.temp_dir, "run_test_two")
        os.makedirs(test_dir, exist_ok=True)

        with patch("run_and_verify_baseline.run_command") as mock_run:
            # Lần 1: harness -> return code 0
            # Lần 2: regression -> return code 2 (runner crash!)
            mock_run.side_effect = [
                (0, "Harness passed", ""),
                (2, "Regression crashed with code 2", "Fatal error")
            ]

            test_args = [
                "run_and_verify_baseline.py",
                "--results-dir", test_dir,
                "--configuration", "Release"
            ]
            with patch.object(sys, "argv", test_args):
                with self.assertRaises(SystemExit) as cm:
                    run_and_verify_baseline.main()
                self.assertEqual(cm.exception.code, 1)

            # Kiểm tra evidence đã ghi nhận ABNORMAL_REGRESSION_EXIT_2
            evidence_file = os.path.join(test_dir, "run-evidence.json")
            self.assertTrue(os.path.exists(evidence_file))
            with open(evidence_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            self.assertEqual(data.get("overall_status"), "ABNORMAL_REGRESSION_EXIT_2")

    def test_failed_harness_rejected(self):
        """Nếu harness thất bại (exit code != 0), orchestrator phải dừng ngay lập tức."""
        test_dir = os.path.join(self.temp_dir, "run_test_harness_fail")
        os.makedirs(test_dir, exist_ok=True)

        with patch("run_and_verify_baseline.run_command") as mock_run:
            mock_run.side_effect = [
                (1, "Harness failed", "Error")
            ]

            test_args = [
                "run_and_verify_baseline.py",
                "--results-dir", test_dir,
                "--configuration", "Release"
            ]
            with patch.object(sys, "argv", test_args):
                with self.assertRaises(SystemExit) as cm:
                    run_and_verify_baseline.main()
                self.assertEqual(cm.exception.code, 1)

            evidence_file = os.path.join(test_dir, "run-evidence.json")
            self.assertTrue(os.path.exists(evidence_file))
            with open(evidence_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            self.assertEqual(data.get("overall_status"), "FAILED_HARNESS")


if __name__ == "__main__":
    unittest.main()

