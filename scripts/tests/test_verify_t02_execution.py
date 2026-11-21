"""
Unit tests cho bộ công cụ kiểm chứng verify_t02_execution.py (chạy độc lập không cần SQL Server).
Chứng minh:
1. Phát hiện NULL -> Giá trị (kể cả khi key bị omit do thiếu INCLUDE_NULL_VALUES).
2. Phát hiện Giá trị -> NULL.
3. Phát hiện thêm bản ghi mới trong post.
4. Phát hiện mất bản ghi trong post.
5. Phát hiện thay đổi giá trị.
6. Đối soát chính xác độ lệch tiền tệ bằng Decimal.
7. Dữ liệu thực sự giống nhau được báo MATCH và 0 lệch.
8. Phát hiện index xuất hiện hoặc biến mất qua reconcile_schema_metadata.
9. Phát hiện thay đổi khóa ngoại qua reconcile_schema_metadata.
10. Dữ liệu lệch khiến Markdown xuất nhãn FAIL / LỆCH, không bao giờ xuất PASS cứng.
11. Báo cáo Markdown khi có case thất bại không chứa bất kỳ mô tả thành công sai nào.
12. Bảng chưa có đối soát hiển thị CHƯA KIỂM CHỨNG.
13. Idempotency thất bại khiến kết quả tổng thể thất bại.
14. Kiểm tra an toàn tiền tố database (chặn injection).
"""

import unittest
import decimal
import sys
from pathlib import Path

# Thêm thư mục scripts vào sys.path để import
SCRIPTS_DIR = Path(__file__).resolve().parent.parent
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

from verify_t02_execution import (
    reconcile_table_data,
    reconcile_schema_metadata,
    reconcile_all_snapshots,
    evaluate_overall_success,
    generate_markdown_evidence,
    validate_prefix
)


class TestVerifyT02Execution(unittest.TestCase):

    def test_detects_null_to_value_with_explicit_none(self):
        """Chứng minh: Khi trường trước là None, trường sau có giá trị -> Phát hiện lệch."""
        pre = {1: {"MaNhanVien": 1, "Email": None}}
        post = {1: {"MaNhanVien": 1, "Email": "user1@example.com"}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertIn(1, res["differing_ids"])
        self.assertEqual(res["status"], "DIFF")
        self.assertIn("Email", res["field_diffs"][0]["diffs"])
        self.assertIsNone(res["field_diffs"][0]["diffs"]["Email"]["pre"])
        self.assertEqual(res["field_diffs"][0]["diffs"]["Email"]["post"], "user1@example.com")

    def test_detects_null_to_value_when_pre_key_is_missing(self):
        """Chứng minh: Tái hiện lỗi cũ - nếu trường trước không có key (do NULL bị omit), trường sau có giá trị -> Vẫn phải phát hiện lệch."""
        pre = {1: {"MaNhanVien": 1}}  # Khuyết key Email
        post = {1: {"MaNhanVien": 1, "Email": "user1@example.com"}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertIn(1, res["differing_ids"])
        self.assertEqual(res["status"], "DIFF")
        self.assertIn("Email", res["field_diffs"][0]["diffs"])

    def test_detects_value_to_null(self):
        """Chứng minh: Khi trường trước có giá trị, trường sau bị biến thành NULL -> Phát hiện lệch."""
        pre = {1: {"MaNhanVien": 1, "Email": "user1@example.com"}}
        post = {1: {"MaNhanVien": 1, "Email": None}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertIn(1, res["differing_ids"])
        self.assertEqual(res["status"], "DIFF")
        self.assertEqual(res["field_diffs"][0]["diffs"]["Email"]["pre"], "user1@example.com")
        self.assertIsNone(res["field_diffs"][0]["diffs"]["Email"]["post"])

    def test_detects_added_record_in_post(self):
        """Chứng minh: Phát hiện thêm bản ghi mới trong post."""
        pre = {1: {"Id": 1, "Name": "A"}}
        post = {1: {"Id": 1, "Name": "A"}, 2: {"Id": 2, "Name": "B"}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertEqual(res["added_in_post"], [2])
        self.assertEqual(res["status"], "DIFF")

    def test_detects_missing_record_in_post(self):
        """Chứng minh: Phát hiện bản ghi bị mất trong post."""
        pre = {1: {"Id": 1, "Name": "A"}, 2: {"Id": 2, "Name": "B"}}
        post = {1: {"Id": 1, "Name": "A"}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertEqual(res["missing_in_post"], [2])
        self.assertEqual(res["status"], "DIFF")

    def test_detects_value_change(self):
        """Chứng minh: Phát hiện giá trị trường bị thay đổi."""
        pre = {1: {"MaNhanVien": 1, "HoTen": "Nguyễn Văn A"}}
        post = {1: {"MaNhanVien": 1, "HoTen": "Nguyễn Văn B"}}
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["differing_records_count"], 1)
        self.assertIn(1, res["differing_ids"])
        self.assertEqual(res["status"], "DIFF")

    def test_detects_decimal_currency_precision(self):
        """Chứng minh: Đối soát tiền tệ qua Decimal bảo toàn độ chính xác, phát hiện sai lệch nhỏ nhất."""
        pre = {1: {"MaBangLuong": 1, "LuongThucNhan": decimal.Decimal("50000000.00")}}
        post_diff = {1: {"MaBangLuong": 1, "LuongThucNhan": decimal.Decimal("50000000.01")}}
        post_match = {1: {"MaBangLuong": 1, "LuongThucNhan": decimal.Decimal("50000000.00")}}

        res_diff = reconcile_table_data(pre, post_diff)
        self.assertEqual(res_diff["differing_records_count"], 1)
        self.assertEqual(res_diff["status"], "DIFF")

        res_match = reconcile_table_data(pre, post_match)
        self.assertEqual(res_match["differing_records_count"], 0)
        self.assertEqual(res_match["status"], "MATCH")

    def test_identical_data_reports_match(self):
        """Chứng minh: Khi dữ liệu hai bản hoàn toàn khớp nhau -> Báo MATCH và 0 lệch."""
        pre = {
            1: {"MaPhongBan": 1, "TenPhongBan": "Ban Giám đốc", "NgayTao": "2026-05-25T18:29:36.0848326", "MoTa": None},
            2: {"MaPhongBan": 2, "TenPhongBan": "Phòng Kỹ thuật", "NgayTao": "2026-05-25T18:29:36.0848326", "MoTa": "Mô tả"}
        }
        post = {
            1: {"MaPhongBan": 1, "TenPhongBan": "Ban Giám đốc", "NgayTao": "2026-05-25T18:29:36.0848326", "MoTa": None},
            2: {"MaPhongBan": 2, "TenPhongBan": "Phòng Kỹ thuật", "NgayTao": "2026-05-25T18:29:36.0848326", "MoTa": "Mô tả"}
        }
        res = reconcile_table_data(pre, post)
        self.assertEqual(res["total_records_checked"], 2)
        self.assertEqual(res["differing_records_count"], 0)
        self.assertEqual(res["differing_ids"], [])
        self.assertEqual(res["status"], "MATCH")

    def test_detects_index_appearing(self):
        """Chứng minh: Phát hiện index mới xuất hiện trong post (ví dụ IX_TaiKhoan_TenDangNhap bị tạo sai trong rollback)."""
        pre_schema = {
            "indexes_TaiKhoan": [{"name": "IX_TaiKhoan_MaNhanVien", "is_unique": False, "type_desc": "NONCLUSTERED"}]
        }
        post_schema = {
            "indexes_TaiKhoan": [
                {"name": "IX_TaiKhoan_MaNhanVien", "is_unique": False, "type_desc": "NONCLUSTERED"},
                {"name": "IX_TaiKhoan_TenDangNhap", "is_unique": True, "type_desc": "NONCLUSTERED"}
            ]
        }
        res = reconcile_schema_metadata(pre_schema, post_schema)
        self.assertFalse(res["match"])
        self.assertIn("indexes_TaiKhoan", res["diffs"])
        self.assertEqual(len(res["diffs"]["indexes_TaiKhoan"]["pre"]), 1)
        self.assertEqual(len(res["diffs"]["indexes_TaiKhoan"]["post"]), 2)

    def test_detects_index_disappearing(self):
        """Chứng minh: Phát hiện index bị mất trong post."""
        pre_schema = {
            "indexes_TaiKhoan": [
                {"name": "IX_TaiKhoan_MaNhanVien", "is_unique": False, "type_desc": "NONCLUSTERED"},
                {"name": "PK_TaiKhoan", "is_unique": True, "type_desc": "CLUSTERED"}
            ]
        }
        post_schema = {
            "indexes_TaiKhoan": [
                {"name": "PK_TaiKhoan", "is_unique": True, "type_desc": "CLUSTERED"}
            ]
        }
        res = reconcile_schema_metadata(pre_schema, post_schema)
        self.assertFalse(res["match"])
        self.assertIn("indexes_TaiKhoan", res["diffs"])

    def test_detects_foreign_key_change(self):
        """Chứng minh: Phát hiện khóa ngoại bị thay đổi cấu trúc."""
        pre_schema = {
            "foreign_keys_PasswordResetTokens": [
                {"FkName": "FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan", "ColName": "TaiKhoanMaTaiKhoan", "RefTable": "TaiKhoan", "RefCol": "MaTaiKhoan"}
            ]
        }
        post_schema = {
            "foreign_keys_PasswordResetTokens": [
                {"FkName": "FK_PasswordResetTokens_TaiKhoan_MaTaiKhoan", "ColName": "MaTaiKhoan", "RefTable": "TaiKhoan", "RefCol": "MaTaiKhoan"}
            ]
        }
        res = reconcile_schema_metadata(pre_schema, post_schema)
        self.assertFalse(res["match"])
        self.assertIn("foreign_keys_PasswordResetTokens", res["diffs"])

    def test_markdown_generation_with_failed_case_has_no_false_success_descriptions(self):
        """
        Chứng minh: Khi kịch bản thất bại, Markdown báo cáo KHÔNG được chứa bất kỳ mô tả thành công sai nào
        (không có 'Không có lỗi', '0 diffs', 'Bảo toàn sau thất bại: Đạt', 'KHỚP (0 LỆCH)').
        """
        mock_log = {
            "start_time": "2026-11-10T22:00:00",
            "server": "localhost",
            "all_passed": False,
            "cases": {
                "FreshInstall": {
                    "db": "HRM_Fresh_Fail",
                    "exit_code": 1,
                    "passed": False,
                    "schema_checks": {
                        "unique_index_TenDangNhap": False,
                        "foreign_key_MaTaiKhoan": False,
                        "shadow_column_dropped": False,
                        "hwid_column_exists": False,
                        "migrations_match": False,
                        "actual_migrations": []
                    }
                },
                "UpgradeValid": {
                    "db": "HRM_Upg_Fail",
                    "exit_code": 1,
                    "total_diffs": 5,
                    "passed": False,
                    "schema_checks": {"migrations_match": False, "actual_migrations": []},
                    "reconciliation": {
                        "PhongBan": {"total_records_checked": 6, "differing_records_count": 3, "differing_ids": [1, 2, 3]},
                        "NhanVien": {"total_records_checked": 10, "differing_records_count": 2, "differing_ids": [4, 5]}
                    },
                    "idempotency": {
                        "exit_code": 1,
                        "stdout": "Migration failed on rerun",
                        "diffs_between_runs": 2,
                        "schema_checks_match": False,
                        "schema_meta_match": False,
                        "passed": False
                    }
                },
                "Block_ShadowOnly": {
                    "db": "HRM_Block_Fail",
                    "exit_code": 0,  # Sai: đáng lẽ phải exit code 1
                    "expected_error": "51001",
                    "actual_error": "CHƯA PHÁT HIỆN",
                    "target_ident": "20",
                    "target_reported": False,
                    "rollback_verified": False,
                    "data_diffs": 1,
                    "schema_match": False,
                    "history_match": False,
                    "passed": False
                }
            }
        }
        md = generate_markdown_evidence(mock_log, "HRM_Test_Fail")

        # 1. Kết quả tổng phải là THẤT BẠI
        self.assertIn("**THẤT BẠI (FAIL)**", md)

        # 2. Không được chứa 'Không phát hiện lỗi (Thành công)'
        self.assertNotIn("Không phát hiện lỗi (Thành công)", md)

        # 3. Không được chứa 'Bảo toàn sau thất bại: Đạt'
        self.assertNotIn("Bảo toàn sau thất bại: Đạt", md)
        self.assertIn("Bảo toàn sau thất bại: Không bảo toàn", md)

        # 4. Không được chứa '0 diffs'
        self.assertNotIn("0 diffs", md)
        self.assertIn("5 diffs", md)

        # 5. Bảng PhongBan và NhanVien không được có 'KHỚP (0 LỆCH)'
        self.assertNotIn("**KHỚP (0 LỆCH)**", md)
        self.assertIn("**LỆCH (3 BẢN GHI)**", md)
        self.assertIn("**LỆCH (2 BẢN GHI)**", md)

        # 6. Các ca thất bại phải có nhãn **FAIL**
        self.assertIn("| `HRM_Fresh_Fail` | `dotnet ef database update` | `1` | Lỗi thực thi (Exit code 1) |", md)
        self.assertIn("| `HRM_Block_Fail` | `dotnet ef database update` | `0` | Mong đợi: 51001<br>Thực tế: CHƯA PHÁT HIỆN (Chưa báo mục tiêu: 20) |", md)

    def test_markdown_generation_unverified_table_shows_chua_kiem_chung(self):
        """Chứng minh: Khi chưa có dữ liệu đối soát cho bảng, báo cáo xuất CHƯA KIỂM CHỨNG."""
        mock_log = {
            "all_passed": False,
            "cases": {
                "UpgradeValid": {
                    "reconciliation": {}  # Rỗng, chưa đối soát
                }
            }
        }
        md = generate_markdown_evidence(mock_log, "HRM_Test")
        self.assertIn("**CHƯA KIỂM CHỨNG**", md)

    def test_idempotency_failure_causes_overall_failure(self):
        """Chứng minh: Khi Idempotency thất bại, kết quả tổng thể bị đánh rớt (all_passed = False)."""
        mock_log = {
            "cases": {
                "FreshInstall": {"passed": True},
                "UpgradeValid": {
                    "passed": False,  # Idempotency hỏng làm UpgradeValid hỏng
                    "idempotency": {"passed": False}
                },
                "Block_ShadowOnly": {"passed": True},
                "Block_DifferingIds": {"passed": True},
                "Block_OrphanToken": {"passed": True},
                "Block_DuplicateUser": {"passed": True}
            }
        }
        self.assertFalse(evaluate_overall_success(mock_log))

    def test_prefix_validation_rejects_unsafe_names(self):
        """Chứng minh: Bộ lọc prefix từ chối các chuỗi nguy hiểm hoặc có nguy cơ SQL/Shell injection."""
        self.assertEqual(validate_prefix("HRM_Next_Test_T02_v7"), "HRM_Next_Test_T02_v7")
        self.assertEqual(validate_prefix("Run_20261110"), "Run_20261110")

        with self.assertRaises(ValueError):
            validate_prefix("test; DROP DATABASE master;")
        with self.assertRaises(ValueError):
            validate_prefix("test name with spaces")
        with self.assertRaises(ValueError):
            validate_prefix("test' OR '1'='1")
        with self.assertRaises(ValueError):
            validate_prefix("ab")


if __name__ == "__main__":
    unittest.main()
