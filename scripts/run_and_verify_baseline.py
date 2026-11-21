#!/usr/bin/env python3
"""
scripts/run_and_verify_baseline.py

Script điều phối thực thi kiểm thử và thẩm định baseline TRX:
1. Khởi tạo thư mục kết quả riêng biệt cho mỗi lượt chạy (bằng UUID/timestamp, từ chối thư mục có TRX cũ).
2. Đồng bộ build configuration (Debug/Release) cho cả hai lệnh dotnet test.
3. Chạy HarnessCheck: ghi nhận exit code ngay lập tức; bắt buộc exit code 0.
4. Chạy RegressionBug: ghi nhận exit code ngay lập tức; bắt buộc exit code 1 (do lỗi baseline đã biết). Mọi exit code khác 1 đều bị từ chối ngay.
5. Kiểm tra sự tồn tại của TRX và logs của lượt chạy hiện tại.
6. Thực thi bộ thẩm định verify_baseline_trx.py theo manifest được chỉ định theo nguyên tắc Default-Deny.
7. Ghi file run-evidence.json lưu vết chi tiết trên CẢ nhánh thành công và thất bại.
8. Trả exit code 0 CHỈ KHI toàn bộ điều kiện baseline và thẩm định đạt chuẩn; trả exit 1 khi có bất kỳ sai lệch nào.
"""

import argparse
import datetime
import glob
import json
import os
import shutil
import subprocess
import sys
import uuid
from verify_baseline_trx import verify


def run_command(cmd, log_path):
    """Chạy lệnh, lưu log vào file và trả về (exit_code, stdout_str, stderr_str)."""
    print(f"--> Thực thi: {' '.join(cmd)}")
    result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8", errors="replace")

    with open(log_path, "w", encoding="utf-8") as f:
        f.write(f"COMMAND: {' '.join(cmd)}\n")
        f.write(f"EXIT CODE: {result.returncode}\n")
        f.write("=== STDOUT ===\n")
        f.write(result.stdout)
        f.write("\n=== STDERR ===\n")
        f.write(result.stderr)

    return result.returncode, result.stdout, result.stderr


def write_evidence(evidence, run_dir, repo_root):
    """Lưu vết run-evidence.json vào thư mục run và thư mục docs/verification/latest-run-evidence.json."""
    try:
        evidence_path = os.path.join(run_dir, "run-evidence.json")
        with open(evidence_path, "w", encoding="utf-8") as f:
            json.dump(evidence, f, indent=2, ensure_ascii=False)

        docs_verif_dir = os.path.join(repo_root, "docs", "verification")
        latest_path = os.path.join(docs_verif_dir, "latest-run-evidence.json")
        with open(latest_path, "w", encoding="utf-8") as f:
            json.dump(evidence, f, indent=2, ensure_ascii=False)
    except Exception as e:
        print(f"[CẢNH BÁO] Không thể lưu file evidence: {e}")


def main():
    parser = argparse.ArgumentParser(description="Điều phối chạy test và thẩm định manifest baseline.")
    parser.add_argument("--test-project", default="backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj", help="Đường dẫn file csproj kiểm thử")
    parser.add_argument("--manifest", default="docs/verification/baseline-allowed-failures.json", help="Đường dẫn file manifest lỗi được phép")
    parser.add_argument("--configuration", "-c", default="Release", help="Build configuration (Debug/Release, mặc định: Release)")
    parser.add_argument("--results-dir", default=None, help="Thư mục lưu kết quả (mặc định tạo thư mục mới bằng UUID)")

    args = parser.parse_args()

    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    test_project = os.path.abspath(os.path.join(repo_root, args.test_project))
    manifest_path = os.path.abspath(os.path.join(repo_root, args.manifest))
    configuration = args.configuration.strip()

    # 1. Khởi tạo thư mục kết quả an toàn (tránh ghi đè hoặc dùng lại TRX cũ)
    if args.results_dir:
        run_dir = os.path.abspath(args.results_dir)
        if os.path.exists(run_dir):
            existing_trx = glob.glob(os.path.join(run_dir, "*.trx"))
            if existing_trx:
                print(f"[LỖI TỪ CHỐI] Thư mục kết quả '{run_dir}' đã chứa file TRX cũ ({len(existing_trx)} files)! Không được phép tái sử dụng.")
                sys.exit(1)
        os.makedirs(run_dir, exist_ok=True)
    else:
        run_id = f"run_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}_{uuid.uuid4().hex[:8]}"
        run_dir = os.path.abspath(os.path.join(repo_root, "docs", "verification", "runs", run_id))
        os.makedirs(run_dir, exist_ok=False)

    print("=" * 70)
    print("BẮT ĐẦU ĐIỀU PHỐI KIỂM THỬ VÀ THẨM ĐỊNH BASELINE")
    print(f"Thời gian: {datetime.datetime.now().isoformat()}")
    print(f"Configuration: {configuration}")
    print(f"Thư mục kết quả lượt chạy: {run_dir}")
    print("=" * 70)

    evidence = {
        "timestamp": datetime.datetime.now().isoformat(),
        "configuration": configuration,
        "run_dir": run_dir,
        "test_project": test_project,
        "manifest_path": manifest_path,
        "steps": {},
        "overall_status": "INITIALIZING"
    }

    # 2. Bước 1: Chạy HarnessCheck (Bắt buộc exit code 0)
    harness_trx_name = "test-harness.trx"
    harness_trx_path = os.path.join(run_dir, harness_trx_name)
    harness_log_path = os.path.join(run_dir, "harness.log")

    cmd_harness = [
        "dotnet", "test", test_project,
        "--configuration", configuration,
        "--no-build",
        "--filter", "Category=HarnessCheck",
        "--logger", f"trx;LogFileName={harness_trx_name}",
        "--results-directory", run_dir
    ]

    harness_code, harness_out, harness_err = run_command(cmd_harness, harness_log_path)
    print(f"--> [HarnessCheck] Exit code: {harness_code}")
    evidence["steps"]["harness"] = {
        "exit_code": harness_code,
        "trx_path": harness_trx_path,
        "log_path": harness_log_path
    }

    if harness_code != 0:
        print(f"[THẤT BẠI NGHIÊM TRỌNG] HarnessCheck thất bại với exit code {harness_code}! Bắt buộc dừng điều phối.")
        evidence["overall_status"] = "FAILED_HARNESS"
        write_evidence(evidence, run_dir, repo_root)
        sys.exit(1)

    # 3. Bước 2: Chạy RegressionBug (Bắt buộc exit code 1 do bộ lỗi baseline)
    reg_trx_name = "test-regression-baseline.trx"
    reg_trx_path = os.path.join(run_dir, reg_trx_name)
    reg_log_path = os.path.join(run_dir, "regression.log")

    cmd_regression = [
        "dotnet", "test", test_project,
        "--configuration", configuration,
        "--no-build",
        "--filter", "Category=RegressionBug",
        "--logger", f"trx;LogFileName={reg_trx_name}",
        "--results-directory", run_dir
    ]

    reg_code, reg_out, reg_err = run_command(cmd_regression, reg_log_path)
    print(f"--> [RegressionBug] Exit code: {reg_code} (ghi nhận ngay lập tức)")
    evidence["steps"]["regression"] = {
        "exit_code": reg_code,
        "trx_path": reg_trx_path,
        "log_path": reg_log_path
    }

    # Bắt buộc exit code phải bằng 1 với suite baseline lỗi đã biết hiện tại
    if reg_code != 1:
        print(f"[LỖI ĐIỀU PHỐI] RegressionBug exit code là {reg_code}, không phải 1! (Yêu cầu nghiêm ngặt: với baseline có lỗi đã biết, regression runner phải trả về 1). Bắt buộc dừng điều phối.")
        evidence["overall_status"] = f"ABNORMAL_REGRESSION_EXIT_{reg_code}"
        write_evidence(evidence, run_dir, repo_root)
        sys.exit(1)

    # 4. Kiểm tra sự tồn tại của TRX artifacts
    if not os.path.exists(harness_trx_path):
        print(f"[LỖI] Không tìm thấy file TRX của Harness tại {harness_trx_path}")
        evidence["overall_status"] = "MISSING_HARNESS_TRX"
        write_evidence(evidence, run_dir, repo_root)
        sys.exit(1)

    if not os.path.exists(reg_trx_path):
        print(f"[LỖI] Không tìm thấy file TRX của Regression tại {reg_trx_path}")
        evidence["overall_status"] = "MISSING_REGRESSION_TRX"
        write_evidence(evidence, run_dir, repo_root)
        sys.exit(1)

    # 5. Bước 3: Chạy bộ thẩm định verify_baseline_trx
    print("\n--> [Thẩm định Manifest] Khởi chạy bộ xác thực Default-Deny...")
    validator_code = verify(harness_trx_path, reg_trx_path, manifest_path)
    print(f"--> [Thẩm định Manifest] Exit code: {validator_code}")
    evidence["steps"]["validation"] = {
        "exit_code": validator_code
    }

    # Cập nhật kết quả mới nhất vào thư mục docs/verification/
    docs_verif_dir = os.path.join(repo_root, "docs", "verification")
    try:
        shutil.copyfile(harness_trx_path, os.path.join(docs_verif_dir, "test-harness.trx"))
        shutil.copyfile(reg_trx_path, os.path.join(docs_verif_dir, "test-regression-baseline.trx"))
    except Exception as e:
        print(f"Cảnh báo khi sao chép bản mới nhất vào docs/verification: {e}")

    # Ghi nhận trạng thái tổng kết
    if validator_code == 0:
        evidence["overall_status"] = "SUCCESS_BASELINE_MATCHED"
        print("\n[ĐIỀU PHỐI HOÀN TẤT THÀNH CÔNG] Baseline khớp hoàn toàn manifest lỗi đã biết.")
    else:
        evidence["overall_status"] = "FAILED_VALIDATION_MISMATCH"
        print("\n[ĐIỀU PHỐI THẤT BẠI] Kết quả kiểm thử không khớp manifest hoặc có lỗi ngoài dự kiến.")

    write_evidence(evidence, run_dir, repo_root)

    # Trả mã thoát theo kết quả thẩm định
    sys.exit(validator_code)


if __name__ == "__main__":
    main()
