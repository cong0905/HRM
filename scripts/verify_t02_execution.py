"""
Script kiểm thử tự động và đối soát toàn diện phục vụ nghiệm thu T02.
Tuân thủ nghiêm ngặt:
- Chỉ thao tác trên các database test có tên riêng (tiền tố được chuẩn hóa, mặc định HRM_Next_Test_T02_v7_*).
- Tuyệt đối KHÔNG xóa, không ghi đè bất kỳ database nào đã tồn tại; kiểm tra sys.databases trước khi tạo.
- Tuyệt đối KHÔNG can thiệp HRM_System.
- Đối soát tự động từng bản ghi theo khóa chính và các cột nghiệp vụ trước/sau migration.
- Snapshot SQL giữ nguyên NULL qua FOR JSON PATH, INCLUDE_NULL_VALUES.
- Đối soát số thực/tiền tệ bằng Decimal, không chuyển qua float.
- Báo cáo và kết quả tổng được suy ra hoàn toàn từ dữ liệu thực tế, không dùng nhãn cứng.
- Kiểm tra tính bảo toàn dữ liệu/schema/history sau khi migration bị chặn và khi chạy idempotency.
- Chụp và đối soát metadata index/FK qua sys.indexes và sys.foreign_keys (không dùng OBJECT_ID cho index).
- Ghi log thực thi chi tiết gồm stdout/stderr, lệnh, database đích, exit code, metadata ra docs/verification/ và JSON.
"""

import os
import re
import sys
import json
import decimal
import subprocess
import datetime
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
SERVER = "localhost"
DEFAULT_PREFIX = "HRM_Next_Test_T02_v7"

# Danh mục Migration ID chuẩn
MIGRATIONS_BASELINE = [
    "20260331144555_InitialCreate",
    "20260421120929_SimplifyHieuSuatAndRemoveEvaluator",
    "20260421121936_RemoveHieuSuatRankingAndUiFields",
    "20260502105319_AddChamCongHwid",
    "20260517153408_AddPasswordResetTokens",
    "20260525112936_RedesignHieuSuatFormulas"
]

MIGRATIONS_BASELINE_SIMULATED = sorted(MIGRATIONS_BASELINE + ["20260504161102_AddHwidToChamCong"])

MIGRATIONS_FRESH_ALL = sorted(MIGRATIONS_BASELINE + ["20261110141626_FixLegacySchemaAndConstraints"])

MIGRATIONS_UPGRADE_ALL = sorted(MIGRATIONS_BASELINE_SIMULATED + ["20261110141626_FixLegacySchemaAndConstraints"])


def validate_prefix(prefix):
    """Giới hạn prefix vào ký tự an toàn (A-Z, a-z, 0-9, _) để tránh injection."""
    if not isinstance(prefix, str) or not re.match(r"^[A-Za-z0-9_]{3,50}$", prefix):
        raise ValueError(
            f"Tiền tố database '{prefix}' không hợp lệ! "
            f"Chỉ cho phép 3-50 ký tự gồm chữ cái, chữ số và dấu gạch dưới."
        )
    return prefix


def run_cmd(cmd, cwd=REPO_ROOT):
    """Thực thi lệnh shell và thu thập kết quả."""
    proc = subprocess.run(
        cmd,
        shell=True,
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace"
    )
    return proc.returncode, proc.stdout.strip(), proc.stderr.strip()


def run_sql(query, db="master", check=True):
    """Chạy câu lệnh SQL qua sqlcmd có cờ -I (Quoted Identifiers) và -b (Báo lỗi exit code)."""
    escaped_query = query.replace('"', '""')
    cmd = f'sqlcmd -S {SERVER} -E -d {db} -I -b -Q "{escaped_query}"'
    code, out, err = run_cmd(cmd)
    if check and code != 0:
        raise RuntimeError(f"Lỗi thực thi SQL trên database '{db}' (Exit code: {code}): {err or out}\nQuery: {query}")
    return code, out, err


def run_sql_file(file_path, db):
    """Chạy file SQL qua sqlcmd có cờ -I và -b."""
    cmd = f'sqlcmd -S {SERVER} -E -d {db} -I -b -i "{file_path}"'
    code, out, err = run_cmd(cmd)
    if code != 0:
        raise RuntimeError(f"Lỗi thực thi file SQL '{file_path}' trên database '{db}' (Exit code: {code}): {err or out}")
    return code, out, err


def query_json(query, db):
    """
    Chạy câu lệnh SQL có FOR JSON PATH, INCLUDE_NULL_VALUES và parse JSON.
    Dùng parse_float=decimal.Decimal để bảo toàn độ chính xác của tiền tệ và số thực.
    Bắt buộc giữ các trường NULL để so sánh đối soát chính xác hai chiều.
    """
    json_query = f"SET NOCOUNT ON; {query} FOR JSON PATH, INCLUDE_NULL_VALUES;"
    escaped = json_query.replace('"', '""')
    cmd = f'sqlcmd -S {SERVER} -E -d {db} -I -b -w 65535 -y 0 -Q "{escaped}"'
    code, out, err = run_cmd(cmd)
    if code != 0:
        raise RuntimeError(f"Lỗi truy vấn JSON trên DB {db} (Exit code: {code}): {err or out}\nQuery: {json_query}")
    raw = "".join(line.rstrip("\r\n") for line in out.splitlines()).strip()
    if not raw:
        return []
    try:
        return json.loads(raw, strict=False, parse_float=decimal.Decimal)
    except json.JSONDecodeError as ex:
        raise RuntimeError(f"Không thể parse JSON từ kết quả sqlcmd trên DB {db}: {raw}") from ex


def check_db_exists(db_name):
    """Kiểm tra sự tồn tại của database trong sys.databases."""
    query = f"SELECT COUNT(*) FROM sys.databases WHERE name = N'{db_name}';"
    code, out, err = run_sql(query, db="master", check=True)
    for line in out.splitlines():
        line = line.strip()
        if line.isdigit():
            return int(line) > 0
    return False


def create_db(db_name):
    """Tạo database mới nếu chưa tồn tại. Nếu đã tồn tại, báo lỗi không ghi đè."""
    if check_db_exists(db_name):
        raise RuntimeError(f"Database '{db_name}' ĐÃ TỒN TẠI! Tuân thủ nguyên tắc không ghi đè hoặc xóa database có sẵn.")
    run_sql(f"CREATE DATABASE [{db_name}];", db="master", check=True)
    print(f"  [OK] Đã tạo database test mới: {db_name}")


def run_ef_update(db_name, target_migration=None):
    """Chạy dotnet ef database update tới database đích."""
    conn_str = f"Server={SERVER};Database={db_name};Trusted_Connection=True;TrustServerCertificate=True;"
    migration_arg = f" {target_migration}" if target_migration else ""
    cmd = (
        f'dotnet ef database update{migration_arg} '
        f'--project "db/src/HRM.DAL/HRM.DAL.csproj" '
        f'--startup-project "db/src/HRM.GUI/HRM.GUI.csproj" '
        f'--connection "{conn_str}"'
    )
    code, out, err = run_cmd(cmd)
    return cmd, code, out, err


def get_migration_history(db_name):
    """Lấy danh sách các MigrationId hiện có trong dbo.__EFMigrationsHistory theo thứ tự."""
    query = "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
    code, out, err = run_sql(query, db_name, check=True)
    history = []
    lines = [line.strip() for line in out.splitlines()]
    for line in lines:
        if line and not line.startswith("---") and line != "MigrationId" and not line.endswith("rows affected)"):
            history.append(line)
    return history


def snapshot_all_tables(db_name):
    """
    Lấy dữ liệu chi tiết của 6 bảng nghiệp vụ.
    Bảo toàn NULL thông qua INCLUDE_NULL_VALUES.
    Parse tiền tệ bằng Decimal.
    Không xuất mật khẩu (MatKhauHash) hoặc token bí mật (Token).
    """
    snapshot = {}

    # 1. PhongBan
    q_pb = (
        "SELECT MaPhongBan, TenPhongBan, MoTaChucNang, "
        "CONVERT(NVARCHAR(33), NgayTao, 126) AS NgayTao, "
        "CONVERT(NVARCHAR(33), NgayCapNhat, 126) AS NgayCapNhat "
        "FROM dbo.PhongBan ORDER BY MaPhongBan"
    )
    snapshot["PhongBan"] = {row["MaPhongBan"]: row for row in query_json(q_pb, db_name)}

    # 2. NhanVien
    q_nv = (
        "SELECT MaNhanVien, HoTen, "
        "CONVERT(NVARCHAR(33), NgaySinh, 126) AS NgaySinh, "
        "GioiTinh, CCCD, DiaChi, SoDienThoai, Email, "
        "MaPhongBan, MaChucVu, "
        "CONVERT(NVARCHAR(33), NgayVaoLam, 126) AS NgayVaoLam, "
        "MucLuong, TrangThai, "
        "CONVERT(NVARCHAR(33), NgayTao, 126) AS NgayTao, "
        "CONVERT(NVARCHAR(33), NgayCapNhat, 126) AS NgayCapNhat "
        "FROM dbo.NhanVien ORDER BY MaNhanVien"
    )
    snapshot["NhanVien"] = {row["MaNhanVien"]: row for row in query_json(q_nv, db_name)}

    # 3. TaiKhoan (bỏ qua MatKhauHash)
    q_tk = (
        "SELECT MaTaiKhoan, MaNhanVien, TenDangNhap, VaiTro, TrangThai, "
        "CONVERT(NVARCHAR(33), LanDangNhapCuoi, 126) AS LanDangNhapCuoi, "
        "CONVERT(NVARCHAR(33), NgayTao, 126) AS NgayTao "
        "FROM dbo.TaiKhoan ORDER BY MaTaiKhoan"
    )
    snapshot["TaiKhoan"] = {row["MaTaiKhoan"]: row for row in query_json(q_tk, db_name)}

    # 4. BangLuong (giữ toàn bộ tiền tệ dưới dạng Decimal)
    q_bl = (
        "SELECT MaBangLuong, MaNhanVien, Thang, Nam, LuongCoBan, TongPhuCap, "
        "SoNgayLamViec, SoGioLamThem, TienLamThem, TongThuong, TongPhat, "
        "BHXH, BHYT, BHTN, ThueTNCN, TongThuNhap, TongKhauTru, LuongThucNhan, "
        "CONVERT(NVARCHAR(33), NgayTinhLuong, 126) AS NgayTinhLuong, TrangThai "
        "FROM dbo.BangLuong ORDER BY MaBangLuong"
    )
    snapshot["BangLuong"] = {row["MaBangLuong"]: row for row in query_json(q_bl, db_name)}

    # 5. ChamCong
    q_cc = (
        "SELECT MaChamCong, MaNhanVien, "
        "CONVERT(NVARCHAR(33), NgayChamCong, 126) AS NgayChamCong, "
        "CONVERT(NVARCHAR(33), GioVao, 126) AS GioVao, "
        "CONVERT(NVARCHAR(33), GioRa, 126) AS GioRa, "
        "TongGioLam, GioLamThem, HinhThuc, TrangThai, GhiChu, Hwid "
        "FROM dbo.ChamCong ORDER BY MaChamCong"
    )
    snapshot["ChamCong"] = {row["MaChamCong"]: row for row in query_json(q_cc, db_name)}

    # 6. PasswordResetTokens (bỏ qua Token bí mật)
    q_prt = (
        "SELECT Id, MaTaiKhoan, Email, Purpose, "
        "CONVERT(NVARCHAR(33), CreatedAt, 126) AS CreatedAt, "
        "CONVERT(NVARCHAR(33), ExpiresAt, 126) AS ExpiresAt, IsUsed "
        "FROM dbo.PasswordResetTokens ORDER BY Id"
    )
    snapshot["PasswordResetTokens"] = {row["Id"]: row for row in query_json(q_prt, db_name)}

    return snapshot


def snapshot_single_table(db_name, table_name, pk_col, columns, extra_filter=""):
    """Snapshot một bảng cụ thể cho các kịch bản kiểm chứng chốt chặn."""
    cols_sql = ", ".join(columns)
    where_sql = f"WHERE {extra_filter}" if extra_filter else ""
    query = f"SELECT {cols_sql} FROM dbo.{table_name} {where_sql} ORDER BY {pk_col}"
    rows = query_json(query, db_name)
    return {row[pk_col]: row for row in rows}


def snapshot_schema_metadata(db_name):
    """
    Chụp metadata chi tiết của các index, foreign key và cột liên quan.
    Kiểm tra index qua sys.indexes và object_id của bảng đích (không dùng OBJECT_ID cho index).
    """
    metadata = {}

    # 1. Indexes trên dbo.TaiKhoan
    q_idx_tk = (
        "SELECT name, is_unique, type_desc "
        "FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TaiKhoan') AND name IS NOT NULL ORDER BY name"
    )
    metadata["indexes_TaiKhoan"] = query_json(q_idx_tk, db_name)

    # 2. Indexes trên dbo.PasswordResetTokens
    q_idx_prt = (
        "SELECT name, is_unique, type_desc "
        "FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PasswordResetTokens') AND name IS NOT NULL ORDER BY name"
    )
    metadata["indexes_PasswordResetTokens"] = query_json(q_idx_prt, db_name)

    # 3. Foreign Keys trên dbo.PasswordResetTokens
    q_fk_prt = (
        "SELECT fk.name AS FkName, pc.name AS ColName, OBJECT_NAME(fk.referenced_object_id) AS RefTable, rc.name AS RefCol "
        "FROM sys.foreign_keys fk "
        "JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id "
        "JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id "
        "JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id "
        "WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PasswordResetTokens') "
        "ORDER BY fk.name, pc.name"
    )
    metadata["foreign_keys_PasswordResetTokens"] = query_json(q_fk_prt, db_name)

    # 4. Cột trên dbo.PasswordResetTokens
    q_cols_prt = (
        "SELECT name, max_length, is_nullable "
        "FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PasswordResetTokens') ORDER BY name"
    )
    metadata["columns_PasswordResetTokens"] = query_json(q_cols_prt, db_name)

    # 5. Cột trên dbo.ChamCong
    q_cols_cc = (
        "SELECT name, max_length, is_nullable "
        "FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ChamCong') ORDER BY name"
    )
    metadata["columns_ChamCong"] = query_json(q_cols_cc, db_name)

    return metadata


def reconcile_schema_metadata(schema_pre, schema_post):
    """
    So sánh metadata schema trước và sau của một database.
    Phát hiện bất kỳ sự xuất hiện, biến mất hoặc thay đổi cấu trúc của index, FK hoặc cột.
    """
    diffs = {}
    for key in ["indexes_TaiKhoan", "indexes_PasswordResetTokens", "foreign_keys_PasswordResetTokens", "columns_PasswordResetTokens", "columns_ChamCong"]:
        val_pre = schema_pre.get(key, [])
        val_post = schema_post.get(key, [])
        if val_pre != val_post:
            diffs[key] = {
                "pre": val_pre,
                "post": val_post
            }
    return {
        "match": (len(diffs) == 0),
        "diffs": diffs
    }


def reconcile_table_data(pre_rows, post_rows):
    """
    Đối soát dữ liệu giữa bản trước (pre) và bản sau (post) của một bảng.
    - pre_rows: dict {pk: {col: val}}
    - post_rows: dict {pk: {col: val}}
    Xử lý toàn diện mọi tình huống:
    - Thêm bản ghi mới (added_in_post)
    - Mất bản ghi (missing_in_post)
    - NULL -> Giá trị
    - Giá trị -> NULL
    - Thay đổi giá trị (kể cả Decimal chính xác tuyệt đối)
    - Cột mới xuất hiện hoặc cột bị mất giữa hai snapshot
    """
    pre_keys = set(pre_rows.keys())
    post_keys = set(post_rows.keys())

    missing_in_post = sorted(list(pre_keys - post_keys))
    added_in_post = sorted(list(post_keys - pre_keys))

    differing_ids = []
    field_diffs = []

    for pk in sorted(pre_keys & post_keys):
        row_pre = pre_rows[pk]
        row_post = post_rows[pk]

        all_cols = sorted(set(row_pre.keys()) | set(row_post.keys()))
        row_diffs = {}

        for col in all_cols:
            val_pre = row_pre.get(col, None)
            val_post = row_post.get(col, None)

            if val_pre != val_post:
                val_pre_str = str(val_pre) if isinstance(val_pre, decimal.Decimal) else val_pre
                val_post_str = str(val_post) if isinstance(val_post, decimal.Decimal) else val_post
                row_diffs[col] = {"pre": val_pre_str, "post": val_post_str}

        if row_diffs:
            differing_ids.append(pk)
            field_diffs.append({"pk": pk, "diffs": row_diffs})

    diff_count = len(differing_ids) + len(missing_in_post) + len(added_in_post)
    status = "MATCH" if diff_count == 0 else "DIFF"

    return {
        "total_records_checked": len(pre_keys | post_keys),
        "differing_records_count": diff_count,
        "differing_ids": differing_ids,
        "missing_in_post": missing_in_post,
        "added_in_post": added_in_post,
        "field_diffs": field_diffs,
        "status": status
    }


def reconcile_all_snapshots(snap_pre, snap_post):
    """Đối soát toàn bộ 6 bảng nghiệp vụ giữa hai snapshot."""
    reconciliation = {}
    for table_name in ["PhongBan", "NhanVien", "TaiKhoan", "BangLuong", "ChamCong", "PasswordResetTokens"]:
        pre_rows = snap_pre.get(table_name, {})
        post_rows = snap_post.get(table_name, {})
        reconciliation[table_name] = reconcile_table_data(pre_rows, post_rows)
    return reconciliation


def verify_schema_integrity(db_name, expected_migrations):
    """
    Kiểm tra tính toàn vẹn của schema sau khi áp dụng migration.
    Kiểm tra tường minh bảng/cột đích cho cả FK và Unique Index qua sys.indexes (không dùng OBJECT_ID cho index).
    Đối chiếu chính xác danh sách migration IDs mong đợi.
    Tuyệt đối không bỏ qua lỗi (mọi lỗi truy vấn sẽ ném exception, không thành False/None).
    """
    checks = {}

    # 1. Unique index đúng bảng dbo.TaiKhoan và đúng cột TenDangNhap
    q1 = (
        "SELECT COUNT(*) FROM sys.indexes i "
        "JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id "
        "JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id "
        "WHERE i.object_id = OBJECT_ID(N'dbo.TaiKhoan') "
        "AND i.name = N'IX_TaiKhoan_TenDangNhap' "
        "AND i.is_unique = 1 AND c.name = N'TenDangNhap' AND ic.key_ordinal = 1 "
        "AND (SELECT COUNT(*) FROM sys.index_columns ic2 WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0) = 1;"
    )
    code1, out1, _ = run_sql(q1, db_name, check=True)
    has_unique = any(line.strip() == "1" for line in out1.splitlines())
    checks["unique_index_TenDangNhap"] = has_unique

    # 2. Khóa ngoại dbo.PasswordResetTokens(MaTaiKhoan) trỏ chính xác tới dbo.TaiKhoan(MaTaiKhoan)
    q2 = (
        "SELECT COUNT(*) FROM sys.foreign_keys fk "
        "JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id "
        "JOIN sys.columns pc ON fkc.parent_object_id = pc.object_id AND fkc.parent_column_id = pc.column_id "
        "JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id "
        "WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PasswordResetTokens') "
        "AND pc.name = N'MaTaiKhoan' "
        "AND fk.referenced_object_id = OBJECT_ID(N'dbo.TaiKhoan') "
        "AND rc.name = N'MaTaiKhoan';"
    )
    code2, out2, _ = run_sql(q2, db_name, check=True)
    has_fk = any(line.strip() == "1" for line in out2.splitlines())
    checks["foreign_key_MaTaiKhoan"] = has_fk

    # 3. Cột shadow TaiKhoanMaTaiKhoan không còn tồn tại
    q3 = "SELECT CASE WHEN COL_LENGTH(N'dbo.PasswordResetTokens', N'TaiKhoanMaTaiKhoan') IS NOT NULL THEN 1 ELSE 0 END;"
    code3, out3, _ = run_sql(q3, db_name, check=True)
    shadow_exists = any(line.strip() == "1" for line in out3.splitlines())
    checks["shadow_column_dropped"] = not shadow_exists

    # 4. Cột Hwid tồn tại trên ChamCong
    q4 = "SELECT CASE WHEN COL_LENGTH(N'dbo.ChamCong', N'Hwid') IS NOT NULL THEN 1 ELSE 0 END;"
    code4, out4, _ = run_sql(q4, db_name, check=True)
    has_hwid = any(line.strip() == "1" for line in out4.splitlines())
    checks["hwid_column_exists"] = has_hwid

    # 5. Đối chiếu danh sách MigrationId chính xác
    actual_history = get_migration_history(db_name)
    checks["actual_migrations"] = actual_history
    checks["expected_migrations"] = expected_migrations
    checks["migrations_match"] = (actual_history == expected_migrations)

    return checks


def evaluate_overall_success(results_log):
    """
    Đánh giá kết quả tổng thể một cách nghiêm ngặt:
    - Mọi kịch bản (FreshInstall, UpgradeValid, 4 Blocked Cases) phải passed.
    - Idempotency trong UpgradeValid phải passed (cả exit code, schema đối soát, và đối soát 0 lệch).
    """
    cases = results_log.get("cases", {})
    if not cases:
        return False
    for case_name, case_data in cases.items():
        if not case_data.get("passed", False):
            return False
    return True


def main():
    prefix = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_PREFIX
    validate_prefix(prefix)
    print(f"=== KHỞI ĐỘNG BỘ KIỂM THỬ VÀ ĐỐI SOÁT T02 (Run Prefix: {prefix}) ===")
    start_time = datetime.datetime.now().isoformat()

    db_fresh = f"{prefix}_Fresh"
    db_upgrade = f"{prefix}_UpgradeValid"
    db_block_shadow = f"{prefix}_Block_ShadowOnly"
    db_block_diff = f"{prefix}_Block_DifferingIds"
    db_block_orphan = f"{prefix}_Block_OrphanToken"
    db_block_dup = f"{prefix}_Block_DuplicateUser"

    target_dbs = [db_fresh, db_upgrade, db_block_shadow, db_block_diff, db_block_orphan, db_block_dup]

    # Kiểm tra an toàn: Không được trùng với DB hiện có
    print("\n[Bước 1/6] Kiểm tra an toàn trước khi tạo database...")
    for db in target_dbs:
        if check_db_exists(db):
            print(f"[LỖI AN TOÀN] Database '{db}' đã tồn tại! Dừng ngay lập tức để tránh ghi đè.")
            sys.exit(1)
    print("  [OK] Toàn bộ 6 tên database test mới đều khả dụng và độc lập.")

    fixture_simulated_path = REPO_ROOT / "scripts" / "test_fixtures" / "fixture_simulated_legacy.sql"
    if not fixture_simulated_path.exists():
        print(f"[LỖI] Không tìm thấy file fixture: {fixture_simulated_path}")
        sys.exit(1)

    results_log = {
        "start_time": start_time,
        "run_prefix": prefix,
        "server": SERVER,
        "cases": {},
        "all_passed": False
    }

    # -------------------------------------------------------------
    # Case 1: Fresh Install
    # -------------------------------------------------------------
    print(f"\n[Bước 2/6] Kiểm thử Fresh Install trên: {db_fresh}")
    create_db(db_fresh)
    cmd_fresh, code_fresh, out_fresh, err_fresh = run_ef_update(db_fresh)
    schema_fresh = verify_schema_integrity(db_fresh, expected_migrations=MIGRATIONS_FRESH_ALL)
    schema_meta_fresh = snapshot_schema_metadata(db_fresh)
    case_fresh_ok = (
        code_fresh == 0 and
        schema_fresh["unique_index_TenDangNhap"] and
        schema_fresh["foreign_key_MaTaiKhoan"] and
        schema_fresh["shadow_column_dropped"] and
        schema_fresh["hwid_column_exists"] and
        schema_fresh["migrations_match"]
    )
    results_log["cases"]["FreshInstall"] = {
        "db": db_fresh,
        "cmd": cmd_fresh,
        "exit_code": code_fresh,
        "stdout": out_fresh,
        "stderr": err_fresh,
        "schema_checks": schema_fresh,
        "schema_metadata": schema_meta_fresh,
        "passed": case_fresh_ok
    }
    print(f"  --> Exit code: {code_fresh}, Schema OK: {case_fresh_ok}")

    # -------------------------------------------------------------
    # Case 2: Upgrade Valid & Automated PK Reconciliation (with 2-run Idempotency check)
    # -------------------------------------------------------------
    print(f"\n[Bước 3/6] Kiểm thử Upgrade Valid & Đối soát Khóa chính trên: {db_upgrade}")
    create_db(db_upgrade)
    print("  -> Triển khai 6 migration baseline...")
    cmd_b, code_b, out_b, err_b = run_ef_update(db_upgrade, target_migration="20260525112936_RedesignHieuSuatFormulas")
    if code_b != 0:
        raise RuntimeError(f"Lỗi dựng migration baseline trên {db_upgrade} (Exit code: {code_b}): {err_b or out_b}")

    print("  -> Nạp fixture mô phỏng dữ liệu lịch sử...")
    f_code, f_out, f_err = run_sql_file(fixture_simulated_path, db_upgrade)

    # Chụp SNAPSHOT 1: TRƯỚC MIGRATION
    print("  -> Chụp snapshot dữ liệu TRƯỚC migration (giữ NULL, giữ Decimal)...")
    snapshot_pre = snapshot_all_tables(db_upgrade)
    schema_meta_pre = snapshot_schema_metadata(db_upgrade)

    # Chạy migration T02 lần 1
    print("  -> Thực thi migration T02 (FixLegacySchemaAndConstraints)...")
    cmd_upg, code_upg, out_upg, err_upg = run_ef_update(db_upgrade)
    print(f"  --> Exit code lần 1: {code_upg}")

    # Chụp SNAPSHOT 2: SAU MIGRATION LẦN 1
    print("  -> Chụp snapshot dữ liệu SAU migration lần 1...")
    snapshot_post = snapshot_all_tables(db_upgrade)
    schema_upg = verify_schema_integrity(db_upgrade, expected_migrations=MIGRATIONS_UPGRADE_ALL)
    schema_meta_post = snapshot_schema_metadata(db_upgrade)

    # Đối soát tự động Lần 1
    print("  -> Thực hiện đối soát tự động theo khóa chính từng bảng (Lần 1)...")
    reconciliation = reconcile_all_snapshots(snapshot_pre, snapshot_post)

    total_diffs = sum(r["differing_records_count"] for r in reconciliation.values())
    first_run_ok = (
        code_upg == 0 and
        total_diffs == 0 and
        schema_upg["shadow_column_dropped"] and
        schema_upg["migrations_match"]
    )

    # Chạy migration lần 2 để kiểm tra Idempotency
    print("  -> Kiểm tra tính bất biến (Idempotency - chạy lại migration lần 2)...")
    cmd_idem, code_idem, out_idem, err_idem = run_ef_update(db_upgrade)
    print(f"  --> Exit code lần 2: {code_idem}")

    # Chụp SNAPSHOT 3: SAU MIGRATION LẦN 2
    snapshot_post_idem = snapshot_all_tables(db_upgrade)
    schema_idem = verify_schema_integrity(db_upgrade, expected_migrations=MIGRATIONS_UPGRADE_ALL)
    schema_meta_idem = snapshot_schema_metadata(db_upgrade)

    reconcile_idem = reconcile_all_snapshots(snapshot_post, snapshot_post_idem)
    idem_diffs = sum(r["differing_records_count"] for r in reconcile_idem.values())

    # So sánh toàn bộ các kiểm tra schema trước/sau lần chạy 2
    schema_checks_match = (
        schema_upg["unique_index_TenDangNhap"] == schema_idem["unique_index_TenDangNhap"] and
        schema_upg["foreign_key_MaTaiKhoan"] == schema_idem["foreign_key_MaTaiKhoan"] and
        schema_upg["shadow_column_dropped"] == schema_idem["shadow_column_dropped"] and
        schema_upg["hwid_column_exists"] == schema_idem["hwid_column_exists"] and
        schema_upg["migrations_match"] == schema_idem["migrations_match"]
    )
    schema_meta_recon = reconcile_schema_metadata(schema_meta_post, schema_meta_idem)

    idempotent_ok = (
        code_idem == 0 and
        "No migrations were applied" in out_idem and
        idem_diffs == 0 and
        schema_checks_match and
        schema_meta_recon["match"] and
        schema_idem["migrations_match"]
    )

    case_upg_ok = (first_run_ok and idempotent_ok)

    results_log["cases"]["UpgradeValid"] = {
        "db": db_upgrade,
        "cmd": cmd_upg,
        "exit_code": code_upg,
        "stdout": out_upg,
        "stderr": err_upg,
        "baseline_scaffolding": {"cmd": cmd_b, "exit_code": code_b, "stdout": out_b, "stderr": err_b},
        "fixture": {"exit_code": f_code, "stdout": f_out, "stderr": f_err},
        "schema_checks": schema_upg,
        "schema_pre": schema_meta_pre,
        "schema_post": schema_meta_post,
        "reconciliation": reconciliation,
        "total_diffs": total_diffs,
        "passed": case_upg_ok,
        "idempotency": {
            "cmd": cmd_idem,
            "exit_code": code_idem,
            "stdout": out_idem,
            "stderr": err_idem,
            "diffs_between_runs": idem_diffs,
            "schema_checks_match": schema_checks_match,
            "schema_meta_match": schema_meta_recon["match"],
            "schema_meta_diffs": schema_meta_recon["diffs"],
            "passed": idempotent_ok
        }
    }
    print(f"  --> Tổng số bản ghi lệch lần 1: {total_diffs}, Idempotency OK: {idempotent_ok}, Kết quả: {case_upg_ok}")

    # Helper function cho các kịch bản kiểm chứng chốt chặn (Blocked Cases)
    def run_blocked_case(case_name, db_name, inject_sql, expected_error_code, target_ident, table_to_verify, pk_col, cols_to_verify):
        print(f"\n[Kiểm thử Chốt chặn: {case_name}] trên: {db_name}")
        create_db(db_name)
        cmd_b, code_b, out_b, err_b = run_ef_update(db_name, target_migration="20260525112936_RedesignHieuSuatFormulas")
        if code_b != 0:
            raise RuntimeError(f"Lỗi dựng migration baseline trên {db_name}: {err_b or out_b}")
        run_sql_file(fixture_simulated_path, db_name)

        # Nạp dữ liệu vi phạm
        run_sql(inject_sql, db_name, check=True)

        # 1. Chụp trạng thái DỮ LIỆU, SCHEMA và HISTORY ngay TRƯỚC migration
        data_pre = snapshot_single_table(db_name, table_to_verify, pk_col, cols_to_verify)
        schema_meta_pre = snapshot_schema_metadata(db_name)
        history_pre = get_migration_history(db_name)

        # 2. Chạy migration (kỳ vọng bị chặn)
        cmd_m, code_m, out_m, err_m = run_ef_update(db_name)
        err_content = f"{out_m}\n{err_m}"
        error_matched = (code_m != 0 and expected_error_code in err_content)
        target_reported = (target_ident in err_content)

        # 3. Chụp trạng thái SAU thất bại
        data_post = snapshot_single_table(db_name, table_to_verify, pk_col, cols_to_verify)
        schema_meta_post = snapshot_schema_metadata(db_name)
        history_post = get_migration_history(db_name)

        # 4. Đối soát bảo toàn sau thất bại
        data_recon = reconcile_table_data(data_pre, data_post)
        data_preserved = (data_recon["differing_records_count"] == 0)
        schema_recon = reconcile_schema_metadata(schema_meta_pre, schema_meta_post)
        schema_preserved = schema_recon["match"]
        history_preserved = (history_pre == history_post and history_post == MIGRATIONS_BASELINE_SIMULATED)

        rollback_verified = (data_preserved and schema_preserved and history_preserved)
        case_passed = (error_matched and target_reported and rollback_verified)

        # Trích xuất mã lỗi thực tế nhận diện
        actual_error_found = expected_error_code if expected_error_code in err_content else "Không tìm thấy"

        results_log["cases"][case_name] = {
            "db": db_name,
            "cmd": cmd_m,
            "exit_code": code_m,
            "stdout": out_m,
            "stderr": err_m,
            "expected_error": expected_error_code,
            "actual_error": actual_error_found,
            "error_matched": error_matched,
            "target_ident": target_ident,
            "target_reported": target_reported,
            "rollback_verified": rollback_verified,
            "data_diffs": data_recon["differing_records_count"],
            "schema_match": schema_preserved,
            "schema_diffs": schema_recon["diffs"],
            "history_match": history_preserved,
            "schema_pre": schema_meta_pre,
            "schema_post": schema_meta_post,
            "passed": case_passed
        }
        print(f"  --> Exit code: {code_m}, Lỗi {expected_error_code} chặn đúng: {error_matched}, Báo mục tiêu: {target_reported}, Bảo toàn sau thất bại: {rollback_verified}")
        return case_passed

    # -------------------------------------------------------------
    # Case 3: Chốt chặn Shadow Only (THROW 51001)
    # -------------------------------------------------------------
    inject_shadow_only = (
        "SET IDENTITY_INSERT dbo.PasswordResetTokens ON; "
        "INSERT INTO dbo.PasswordResetTokens (Id, MaTaiKhoan, Email, Token, Purpose, CreatedAt, ExpiresAt, IsUsed, TaiKhoanMaTaiKhoan) "
        "VALUES (20, NULL, 'shadow_only@example.com', 'tok20', 'ResetPassword', SYSUTCDATETIME(), DATEADD(day, 1, SYSUTCDATETIME()), 0, 1); "
        "SET IDENTITY_INSERT dbo.PasswordResetTokens OFF;"
    )
    cols_prt_full = ["Id", "MaTaiKhoan", "Email", "Purpose", "CONVERT(NVARCHAR(33), CreatedAt, 126) AS CreatedAt", "CONVERT(NVARCHAR(33), ExpiresAt, 126) AS ExpiresAt", "IsUsed", "TaiKhoanMaTaiKhoan"]
    run_blocked_case("Block_ShadowOnly", db_block_shadow, inject_shadow_only, "51001", "20", "PasswordResetTokens", "Id", cols_prt_full)

    # -------------------------------------------------------------
    # Case 4: Chốt chặn Differing IDs (THROW 51002)
    # -------------------------------------------------------------
    inject_diff_ids = (
        "SET IDENTITY_INSERT dbo.PasswordResetTokens ON; "
        "INSERT INTO dbo.PasswordResetTokens (Id, MaTaiKhoan, Email, Token, Purpose, CreatedAt, ExpiresAt, IsUsed, TaiKhoanMaTaiKhoan) "
        "VALUES (30, 1, 'diff_ids@example.com', 'tok30', 'ResetPassword', SYSUTCDATETIME(), DATEADD(day, 1, SYSUTCDATETIME()), 0, 2); "
        "SET IDENTITY_INSERT dbo.PasswordResetTokens OFF;"
    )
    run_blocked_case("Block_DifferingIds", db_block_diff, inject_diff_ids, "51002", "30", "PasswordResetTokens", "Id", cols_prt_full)

    # -------------------------------------------------------------
    # Case 5: Chốt chặn Orphan Token (THROW 51003)
    # -------------------------------------------------------------
    inject_orphan = (
        "SET IDENTITY_INSERT dbo.PasswordResetTokens ON; "
        "INSERT INTO dbo.PasswordResetTokens (Id, MaTaiKhoan, Email, Token, Purpose, CreatedAt, ExpiresAt, IsUsed, TaiKhoanMaTaiKhoan) "
        "VALUES (40, 99999, 'orphan@example.com', 'tok40', 'ResetPassword', SYSUTCDATETIME(), DATEADD(day, 1, SYSUTCDATETIME()), 0, NULL); "
        "SET IDENTITY_INSERT dbo.PasswordResetTokens OFF;"
    )
    run_blocked_case("Block_OrphanToken", db_block_orphan, inject_orphan, "51003", "40", "PasswordResetTokens", "Id", cols_prt_full)

    # -------------------------------------------------------------
    # Case 6: Chốt chặn Duplicate Username (THROW 51004)
    # -------------------------------------------------------------
    inject_dup_user = (
        "UPDATE dbo.TaiKhoan SET TenDangNhap = N'duplicate_user' WHERE MaTaiKhoan = 2; "
        "SET IDENTITY_INSERT dbo.TaiKhoan ON; "
        "INSERT INTO dbo.TaiKhoan (MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai, NgayTao) "
        "VALUES (8, 8, N'duplicate_user', 'hash8', 'NhanVien', 'Hoạt động', SYSUTCDATETIME()); "
        "SET IDENTITY_INSERT dbo.TaiKhoan OFF;"
    )
    cols_tk = ["MaTaiKhoan", "MaNhanVien", "TenDangNhap", "VaiTro", "TrangThai", "CONVERT(NVARCHAR(33), LanDangNhapCuoi, 126) AS LanDangNhapCuoi", "CONVERT(NVARCHAR(33), NgayTao, 126) AS NgayTao"]
    run_blocked_case("Block_DuplicateUser", db_block_dup, inject_dup_user, "51004", "duplicate_user", "TaiKhoan", "MaTaiKhoan", cols_tk)

    # -------------------------------------------------------------
    # Đánh giá kết quả tổng và ghi nhận báo cáo
    # -------------------------------------------------------------
    all_passed = evaluate_overall_success(results_log)
    results_log["all_passed"] = all_passed

    print("\n[Bước 5/6] Ghi log bằng chứng thực thi ra docs/verification/...")
    runs_dir = REPO_ROOT / "docs" / "verification" / "runs"
    runs_dir.mkdir(parents=True, exist_ok=True)
    json_log_path = runs_dir / f"t02_verification_log_{prefix}.json"
    with open(json_log_path, "w", encoding="utf-8") as f:
        json.dump(results_log, f, indent=2, ensure_ascii=False)

    md_evidence_path = REPO_ROOT / "docs" / "verification" / "t02-execution-evidence.md"
    md_content = generate_markdown_evidence(results_log, prefix)
    with open(md_evidence_path, "w", encoding="utf-8") as f:
        f.write(md_content)

    print(f"  [OK] Đã lưu file log JSON: {json_log_path}")
    print(f"  [OK] Đã cập nhật file Markdown bằng chứng: {md_evidence_path}")

    print("\n=== KẾT QUẢ KIỂM THỬ T02 TOÀN DIỆN ===")
    if all_passed:
        print("[THÀNH CÔNG] Toàn bộ các kịch bản kiểm thử T02 đều ĐẠT CHUẨN!")
        return 0
    else:
        print("[THẤT BẠI] Có kịch bản không đạt chuẩn! Vui lòng kiểm tra log.")
        return 1


def generate_markdown_evidence(log_data, prefix):
    """
    Sinh tài liệu Markdown từ kết quả thực tế.
    Bỏ hoàn toàn nhãn cứng: mọi mô tả thành công/lỗi đều lấy động từ kết quả.
    Nếu chưa có bằng chứng, ghi rõ CHƯA KIỂM CHỨNG.
    """
    cases = log_data.get("cases", {})
    upg = cases.get("UpgradeValid", {})
    recon = upg.get("reconciliation", {})
    all_passed = log_data.get("all_passed", False)
    overall_status_str = "**ĐẠT TOÀN DIỆN (PASS)**" if all_passed else "**THẤT BẠI (FAIL)**"

    def case_status(c_name):
        c = cases.get(c_name)
        if c is None:
            return "**CHƯA KIỂM CHỨNG**"
        return "**PASS**" if c.get("passed", False) else "**FAIL**"

    def table_status(t_name):
        if t_name not in recon:
            return "**CHƯA KIỂM CHỨNG**"
        r = recon[t_name]
        diff_cnt = r.get("differing_records_count", -1)
        if diff_cnt == 0:
            return "**KHỚP (0 LỆCH)**"
        elif diff_cnt > 0:
            return f"**LỆCH ({diff_cnt} BẢN GHI)**"
        return "**CHƯA KIỂM CHỨNG**"

    fresh = cases.get("FreshInstall", {})
    upg_idem = upg.get("idempotency", {})
    b_shadow = cases.get("Block_ShadowOnly", {})
    b_diff = cases.get("Block_DifferingIds", {})
    b_orphan = cases.get("Block_OrphanToken", {})
    b_dup = cases.get("Block_DuplicateUser", {})

    total_checked_all = sum(r.get("total_records_checked", 0) for r in recon.values())
    total_diffs_all = sum(r.get("differing_records_count", 0) for r in recon.values())

    # Mô tả động cho Fresh Install
    fresh_code = fresh.get("exit_code", -1)
    if fresh_code == 0:
        fresh_err_desc = "Không phát hiện lỗi (Thành công)"
    elif fresh_code > 0:
        fresh_err_desc = f"Lỗi thực thi (Exit code {fresh_code})"
    else:
        fresh_err_desc = "CHƯA KIỂM CHỨNG"

    sc_fresh = fresh.get("schema_checks", {})
    if sc_fresh:
        ui_f = "Có" if sc_fresh.get("unique_index_TenDangNhap") else "Không có"
        fk_f = "Có" if sc_fresh.get("foreign_key_MaTaiKhoan") else "Không có"
        sh_f = "Đã drop" if sc_fresh.get("shadow_column_dropped") else "Chưa drop"
        hw_f = "Có" if sc_fresh.get("hwid_column_exists") else "Không có"
        hm_f = "Khớp" if sc_fresh.get("migrations_match") else "Không khớp"
        cnt_f = len(sc_fresh.get("actual_migrations", []))
        fresh_schema_desc = f"Unique Index: {ui_f}<br>FK MaTaiKhoan: {fk_f}<br>Shadow column: {sh_f}<br>Cột Hwid: {hw_f}<br>History: {hm_f} ({cnt_f} migration)"
    else:
        fresh_schema_desc = "CHƯA KIỂM CHỨNG"

    # Mô tả động cho Upgrade Valid
    upg_code = upg.get("exit_code", -1)
    if upg_code == 0:
        upg_err_desc = "Không phát hiện lỗi (Thành công)"
    elif upg_code > 0:
        upg_err_desc = f"Lỗi thực thi (Exit code {upg_code})"
    else:
        upg_err_desc = "CHƯA KIỂM CHỨNG"

    sc_upg = upg.get("schema_checks", {})
    if sc_upg:
        total_d = upg.get("total_diffs", -1)
        d_str = f"{total_d} diffs" if total_d >= 0 else "CHƯA KIỂM CHỨNG"
        hm_u = "Khớp" if sc_upg.get("migrations_match") else "Không khớp"
        cnt_u = len(sc_upg.get("actual_migrations", []))
        upg_schema_desc = f"Áp dụng migration: Exit {upg_code}; Đối soát dữ liệu: {d_str}; History: {hm_u} ({cnt_u} migration)"
    else:
        upg_schema_desc = "CHƯA KIỂM CHỨNG"

    # Mô tả động cho Idempotency
    if upg_idem:
        stdout_txt = upg_idem.get("stdout", "")
        if "No migrations were applied" in stdout_txt:
            idem_err_desc = "Thông báo: 'No migrations were applied'"
        else:
            idem_err_desc = f"Thông báo bất thường (Exit {upg_idem.get('exit_code', -1)})"
        idem_d = upg_idem.get("diffs_between_runs", -1)
        idem_d_str = f"{idem_d} lệch dữ liệu" if idem_d >= 0 else "CHƯA KIỂM CHỨNG"
        sc_m = "Khớp" if upg_idem.get("schema_checks_match") else "Không khớp"
        sm_m = "Khớp" if upg_idem.get("schema_meta_match") else "Không khớp"
        idem_schema_desc = f"Đối soát trước/sau lần 2: {idem_d_str}; Schema checks: {sc_m}; Schema metadata: {sm_m}"
    else:
        idem_err_desc = "CHƯA KIỂM CHỨNG"
        idem_schema_desc = "CHƯA KIỂM CHỨNG"

    # Hàm mô tả động cho Blocked Cases
    def blocked_case_desc(b_case):
        if not b_case:
            return "CHƯA KIỂM CHỨNG", "CHƯA KIỂM CHỨNG"
        exp = b_case.get("expected_error", "N/A")
        act = b_case.get("actual_error", "CHƯA PHÁT HIỆN")
        t_rep = "Đã báo" if b_case.get("target_reported") else "Chưa báo"
        t_id = b_case.get("target_ident", "N/A")
        err_str = f"Mong đợi: {exp}<br>Thực tế: {act} ({t_rep} mục tiêu: {t_id})"

        rb = "Đạt" if b_case.get("rollback_verified") else "Không bảo toàn"
        d_diff = b_case.get("data_diffs", -1)
        d_str = f"{d_diff} lệch dữ liệu" if d_diff >= 0 else "CHƯA KIỂM CHỨNG"
        sm = "Khớp" if b_case.get("schema_match") else "Không khớp"
        hm = "Khớp" if b_case.get("history_match") else "Không khớp"
        schema_str = f"Bảo toàn sau thất bại: {rb}<br>Dữ liệu: {d_str}<br>Schema metadata: {sm}<br>History: {hm}"
        return err_str, schema_str

    err_s, sch_s = blocked_case_desc(b_shadow)
    err_d, sch_d = blocked_case_desc(b_diff)
    err_o, sch_o = blocked_case_desc(b_orphan)
    err_u, sch_u = blocked_case_desc(b_dup)

    content = f"""# Bằng Chứng Thực Thi & Đối Soát Dữ Liệu T02 (Run: {prefix})

- **Thời gian thực hiện**: {log_data.get("start_time", "N/A")}
- **Máy chủ kiểm thử**: {log_data.get("server", "localhost")} (SQL Server Developer Edition)
- **Tiền tố database test**: `{prefix}_*`
- **Kết quả tổng thể**: {overall_status_str}
- **Nguyên tắc an toàn**: Toàn bộ database test được tạo mới độc lập; không can thiệp, không đọc/ghi và không thay đổi database sản phẩm `HRM_System`.
- **Trạng thái nhiệm vụ**: **READY_FOR_REVIEW** (Chờ chủ dự án nghiệm thu)

---

## 1. Bảng Tổng Hợp Kết Quả Thực Thi 6 Kịch Bản Kiểm Thử

| Kịch Bản | Database Đích | Lệnh Thực Thi | Exit Code | Lỗi SQL Mong Đợi / Thực Tế | Kiểm Tra Schema & History Sau Chạy | Kết Quả |
| :--- | :--- | :--- | :---: | :--- | :--- | :---: |
| **1. Fresh Install** | `{fresh.get("db", "N/A")}` | `dotnet ef database update` | `{fresh.get("exit_code", -1)}` | {fresh_err_desc} | {fresh_schema_desc} | {case_status("FreshInstall")} |
| **2. Upgrade Valid** | `{upg.get("db", "N/A")}` | `dotnet ef database update` | `{upg.get("exit_code", -1)}` | {upg_err_desc} | {upg_schema_desc} | {case_status("UpgradeValid")} |
| **- Idempotency** | `{upg.get("db", "N/A")}` | `dotnet ef database update` (Lần 2) | `{upg_idem.get("exit_code", -1)}` | {idem_err_desc} | {idem_schema_desc} | {"**PASS**" if upg_idem.get("passed", False) else "**FAIL**"} |
| **3. Block ShadowOnly** | `{b_shadow.get("db", "N/A")}` | `dotnet ef database update` | `{b_shadow.get("exit_code", -1)}` | {err_s} | {sch_s} | {case_status("Block_ShadowOnly")} |
| **4. Block DifferingIds** | `{b_diff.get("db", "N/A")}` | `dotnet ef database update` | `{b_diff.get("exit_code", -1)}` | {err_d} | {sch_d} | {case_status("Block_DifferingIds")} |
| **5. Block OrphanToken** | `{b_orphan.get("db", "N/A")}` | `dotnet ef database update` | `{b_orphan.get("exit_code", -1)}` | {err_o} | {sch_o} | {case_status("Block_OrphanToken")} |
| **6. Block DuplicateUser**| `{b_dup.get("db", "N/A")}` | `dotnet ef database update` | `{b_dup.get("exit_code", -1)}` | {err_u} | {sch_u} | {case_status("Block_DuplicateUser")} |

---

## 2. Đối Soát Tự Động Từng Bản Ghi Theo Khóa Chính (Upgrade Valid)

Phép so sánh tự động đối soát từng cột của từng dòng dữ liệu giữa **TRƯỚC MIGRATION** và **SAU MIGRATION** trên database `{upg.get("db", "N/A")}`.
Bao gồm kiểm tra chặt chẽ `PhongBan` và các trường `NgayTao` và `NgayCapNhat` từng bị ảnh hưởng bởi lỗi `UpdateData` trước đây (đối soát tiền tệ qua Decimal chính xác):

| Bảng Kiểm Tra | Khóa Chính (PK) | Các Cột Được Kiểm Tra | Tổng Bản Ghi | Số Bản Ghi Lệch | Danh Sách ID Lệch | Trạng Thái Thực Tế |
| :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| **`dbo.PhongBan`** | `MaPhongBan` | `TenPhongBan`, `MoTaChucNang`, `NgayTao`, `NgayCapNhat` | {recon.get("PhongBan", {}).get("total_records_checked", 0)} | {recon.get("PhongBan", {}).get("differing_records_count", 0)} | {recon.get("PhongBan", {}).get("differing_ids", [])} | {table_status("PhongBan")} |
| **`dbo.NhanVien`** | `MaNhanVien` | `HoTen`, `NgaySinh`, `GioiTinh`, `CCCD`, `DiaChi`, `SoDienThoai`, `Email`, `MaPhongBan`, `MaChucVu`, `NgayVaoLam`, `MucLuong`, `TrangThai`, `NgayTao`, `NgayCapNhat` | {recon.get("NhanVien", {}).get("total_records_checked", 0)} | {recon.get("NhanVien", {}).get("differing_records_count", 0)} | {recon.get("NhanVien", {}).get("differing_ids", [])} | {table_status("NhanVien")} |
| **`dbo.TaiKhoan`** | `MaTaiKhoan` | `MaNhanVien`, `TenDangNhap`, `VaiTro`, `TrangThai`, `LanDangNhapCuoi`, `NgayTao` *(Mật khẩu được bỏ qua không log)* | {recon.get("TaiKhoan", {}).get("total_records_checked", 0)} | {recon.get("TaiKhoan", {}).get("differing_records_count", 0)} | {recon.get("TaiKhoan", {}).get("differing_ids", [])} | {table_status("TaiKhoan")} |
| **`dbo.BangLuong`** | `MaBangLuong` | `MaNhanVien`, `Thang`, `Nam`, `LuongCoBan`, `TongPhuCap`, `SoNgayLamViec`, `SoGioLamThem`, `TienLamThem`, `TongThuong`, `TongPhat`, `BHXH`, `BHYT`, `BHTN`, `ThueTNCN`, `TongThuNhap`, `TongKhauTru`, `LuongThucNhan`, `NgayTinhLuong`, `TrangThai` | {recon.get("BangLuong", {}).get("total_records_checked", 0)} | {recon.get("BangLuong", {}).get("differing_records_count", 0)} | {recon.get("BangLuong", {}).get("differing_ids", [])} | {table_status("BangLuong")} |
| **`dbo.ChamCong`** | `MaChamCong` | `MaNhanVien`, `NgayChamCong`, `GioVao`, `GioRa`, `TongGioLam`, `GioLamThem`, `HinhThuc`, `TrangThai`, `GhiChu`, `Hwid` | {recon.get("ChamCong", {}).get("total_records_checked", 0)} | {recon.get("ChamCong", {}).get("differing_records_count", 0)} | {recon.get("ChamCong", {}).get("differing_ids", [])} | {table_status("ChamCong")} |
| **`dbo.PasswordResetTokens`** | `Id` | `MaTaiKhoan`, `Email`, `Purpose`, `CreatedAt`, `ExpiresAt`, `IsUsed` *(Chuỗi token bí mật được bỏ qua không log)* | {recon.get("PasswordResetTokens", {}).get("total_records_checked", 0)} | {recon.get("PasswordResetTokens", {}).get("differing_records_count", 0)} | {recon.get("PasswordResetTokens", {}).get("differing_ids", [])} | {table_status("PasswordResetTokens")} |

- **Tổng số bản ghi kiểm tra trên toàn bộ 6 bảng**: {total_checked_all} bản ghi.
- **Tổng số bản ghi phát hiện sai lệch**: **{total_diffs_all} bản ghi**.

---

## 3. Tuyên Bố Phạm Vi Hỗ Trợ & Ranh Giới Kỹ Thuật

> [!IMPORTANT]
> **Phạm vi hỗ trợ đã được kiểm chứng**:
> - Các database có chuỗi schema dựng từ Entity Framework Core và migration history tương thích với khảo sát.
> - Các database có cấu trúc cột shadow `TaiKhoanMaTaiKhoan` và thiếu unique index trên `TenDangNhap`.
> - Dữ liệu trong các fixture test (`fixture_simulated_legacy.sql`) là **dữ liệu mô phỏng độc lập**, tuyệt đối **không phải snapshot từ database thật `HRM_System`**.
>
> **Ranh giới chưa kiểm chứng (UNVERIFIED)**:
> - Đường chuyển đổi trực tiếp từ database dựng thuần túy bằng file SQL script legacy (`db/docs/HRM_Database.sql`) **chưa được thực hiện và chưa được kiểm chứng**, vì script SQL legacy hoàn toàn thiếu các bảng/cột phát sinh sau này (`PasswordResetTokens`, `Hwid` trên `ChamCong`, kiểu dữ liệu khác biệt).
> - Fixture mô phỏng thêm dòng migration history trong database test **chỉ là mô phỏng lịch sử EF**, tuyệt đối **không được coi là kiểm thử thay thế** cho đường nâng cấp từ database tạo thuần túy bằng SQL legacy.

---

## 4. Ghi Chú Kỹ Thuật Về Phương Thức `Down()` Của Migration

> [!WARNING]
> Phương thức `Down()` của migration `20261110141626_FixLegacySchemaAndConstraints.cs`:
> - Khi rollback, `Down()` chỉ phục hồi **cấu trúc cột** (`migrationBuilder.AddColumn<int>("TaiKhoanMaTaiKhoan", ...)`), tái lập Foreign Key và xóa Unique Index trên `TenDangNhap`.
> - `Down()` **KHÔNG TỰ PHỤC HỒI các giá trị shadow data** đã bị xóa khi cột bị DROP ở phương thức `Up()`. Toàn bộ giá trị trong cột `TaiKhoanMaTaiKhoan` sau khi rollback bằng `Down()` sẽ nhận giá trị `NULL`.
> - Tuyệt đối **không được mô tả `Down()` là cơ chế rollback dữ liệu đầy đủ**. Nếu muốn khôi phục dữ liệu shadow, bắt buộc phải restore từ bản backup database trước khi chạy migration.
"""
    return content


if __name__ == "__main__":
    sys.exit(main())
