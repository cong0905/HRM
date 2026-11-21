#!/usr/bin/env python3
"""
scripts/verify_baseline_trx.py

Bộ thẩm định báo cáo kết quả kiểm thử TRX của baseline theo nguyên tắc MẶC ĐỊNH TỪ CHỐI (Default-Deny):
- Khai báo đầy đủ danh mục Harness (chỉ chấp nhận Passed) và Regression (chỉ chấp nhận Failed đúng assertion).
- Kiểm tra chặt chẽ: mọi outcome != 'Failed' trong regression đều bị từ chối ngay lập tức.
- Kiểm tra ResultSummary, Counters cấp runner; bắt buộc có ResultSummary và Counters hợp lệ.
- Bắt trùng lặp testName trong TRX và Manifest trước khi xử lý tập hợp.
- Yêu cầu các điều kiện nhận diện lỗi trong manifest phải là chuỗi không rỗng và phải khớp chính xác.
"""

import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET

TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def validate_manifest(manifest_path):
    """Đọc và kiểm tra tính toàn vẹn của Manifest theo nguyên tắc default-deny."""
    if not os.path.exists(manifest_path):
        raise ValueError(f"Không tìm thấy file manifest: {manifest_path}")

    with open(manifest_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    if not isinstance(data, dict):
        raise ValueError("Nội dung manifest phải là một JSON object.")

    harness_list = data.get("expected_harness_tests")
    regression_list = data.get("expected_regression_tests")

    if not harness_list or not isinstance(harness_list, list) or len(harness_list) == 0:
        raise ValueError("Manifest thiếu hoặc rỗng danh sách 'expected_harness_tests'.")

    if not regression_list or not isinstance(regression_list, list) or len(regression_list) == 0:
        raise ValueError("Manifest thiếu hoặc rỗng danh sách 'expected_regression_tests'.")

    # Kiểm tra tính hợp lệ và trùng lặp trong Harness
    harness_names = []
    for item in harness_list:
        if not isinstance(item, dict):
            raise ValueError("Mỗi mục trong 'expected_harness_tests' phải là một JSON object.")
        name = item.get("test_name", "").strip()
        expected_outcome = item.get("expected_outcome", "").strip()
        if not name:
            raise ValueError("Có test trong 'expected_harness_tests' thiếu trường 'test_name'.")
        if expected_outcome != "Passed":
            raise ValueError(f"Harness test '{name}' có expected_outcome '{expected_outcome}' không hợp lệ. Phải là 'Passed'.")
        if name in harness_names:
            raise ValueError(f"Trùng lặp test_name trong 'expected_harness_tests': '{name}'.")
        harness_names.append(name)

    # Kiểm tra tính hợp lệ và trùng lặp trong Regression
    reg_names = []
    for item in regression_list:
        if not isinstance(item, dict):
            raise ValueError("Mỗi mục trong 'expected_regression_tests' phải là một JSON object.")
        name = item.get("test_name", "").strip()
        fid = item.get("finding_id", "").strip()
        expected_outcome = item.get("expected_outcome", "").strip()
        substrings = item.get("expected_error_substrings")

        if not name:
            raise ValueError("Có test trong 'expected_regression_tests' thiếu 'test_name'.")
        if not fid:
            raise ValueError(f"Test '{name}' trong manifest thiếu 'finding_id'.")
        if expected_outcome != "Failed":
            raise ValueError(f"Test '{name}' có expected_outcome '{expected_outcome}' không hợp lệ. Phải là 'Failed'.")
        if not substrings or not isinstance(substrings, list) or len(substrings) == 0:
            raise ValueError(f"Test '{name}' thiếu điều kiện nhận diện lỗi 'expected_error_substrings'.")

        # Kiểm tra từng substring không được là chuỗi rỗng
        for sub in substrings:
            if not isinstance(sub, str) or not sub.strip():
                raise ValueError(f"Test '{name}' có điều kiện nhận diện lỗi rỗng hoặc không phải chuỗi.")

        if name in reg_names:
            raise ValueError(f"Trùng lặp test_name trong 'expected_regression_tests': '{name}'.")
        reg_names.append(name)

    return harness_list, regression_list


def parse_trx(file_path):
    """Phân tích file TRX, kiểm tra bắt buộc ResultSummary, Counters và bắt trùng lặp test result."""
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"Không tìm thấy file TRX: {file_path}")

    tree = ET.parse(file_path)
    root = tree.getroot()

    # 1. Kiểm tra ResultSummary & Counters bắt buộc
    summary_elem = root.find("t:ResultSummary", TRX_NS)
    if summary_elem is None:
        raise ValueError(f"File TRX '{file_path}' không có thẻ ResultSummary hợp lệ.")

    run_outcome = summary_elem.attrib.get("outcome", "").strip()
    if not run_outcome:
        raise ValueError(f"Thẻ ResultSummary trong '{file_path}' thiếu thuộc tính 'outcome'.")

    counters_elem = summary_elem.find("t:Counters", TRX_NS)
    if counters_elem is None:
        raise ValueError(f"File TRX '{file_path}' không có thẻ Counters trong ResultSummary.")

    counters = {}
    for k, v in counters_elem.attrib.items():
        try:
            counters[k] = int(v)
        except ValueError:
            counters[k] = v

    if counters.get("total", 0) <= 0:
        raise ValueError(f"File TRX '{file_path}' ghi nhận total test = {counters.get('total')}.")

    # 2. Trích xuất UnitTestResult và kiểm tra trùng lặp trước khi lập dict
    results = []
    seen_names = []
    duplicate_names = []

    unit_test_results = root.findall(".//t:UnitTestResult", TRX_NS)
    for utr in unit_test_results:
        test_name = utr.attrib.get("testName", "").strip()
        outcome = utr.attrib.get("outcome", "").strip()
        duration = utr.attrib.get("duration", "").strip()

        if test_name in seen_names:
            duplicate_names.append(test_name)
        else:
            seen_names.append(test_name)

        err_msg = ""
        err_stack = ""

        out_elem = utr.find("t:Output", TRX_NS)
        if out_elem is not None:
            err_elem = out_elem.find("t:ErrorInfo", TRX_NS)
            if err_elem is not None:
                msg_elem = err_elem.find("t:Message", TRX_NS)
                if msg_elem is not None and msg_elem.text:
                    err_msg = msg_elem.text.strip()
                stack_elem = err_elem.find("t:StackTrace", TRX_NS)
                if stack_elem is not None and stack_elem.text:
                    err_stack = stack_elem.text.strip()

        results.append({
            "test_name": test_name,
            "outcome": outcome,
            "duration": duration,
            "error_message": err_msg,
            "stack_trace": err_stack,
        })

    return {
        "run_outcome": run_outcome,
        "counters": counters,
        "duplicate_names": duplicate_names,
        "results": results
    }


def verify(harness_trx_path, regression_trx_path, manifest_path):
    errors = []

    # 1. Đọc và thẩm định Manifest theo Default-Deny
    print(f"[1/3] Đang thẩm định Manifest: {manifest_path}...")
    try:
        expected_harness, expected_regression = validate_manifest(manifest_path)
    except Exception as e:
        print(f"  [LỖI MANIFEST KHÔNG HỢP LỆ]: {e}")
        return 1

    expected_harness_names = {x["test_name"] for x in expected_harness}
    expected_reg_map = {x["test_name"]: x for x in expected_regression}
    print(f"  [OK] Manifest hợp lệ: {len(expected_harness)} harness tests, {len(expected_regression)} regression tests.")

    # 2. Thẩm định Harness TRX
    print(f"\n[2/3] Đang thẩm định Harness TRX: {harness_trx_path}...")
    try:
        harness_data = parse_trx(harness_trx_path)
    except Exception as e:
        print(f"  [LỖI ĐỌC HARNESS TRX]: {e}")
        return 1

    if harness_data["duplicate_names"]:
        for d in harness_data["duplicate_names"]:
            errors.append(f"Harness TRX chứa kết quả trùng lặp cho test: '{d}'")

    run_outcome = harness_data["run_outcome"]
    if run_outcome not in ("Completed", "Passed"):
        errors.append(f"Harness run_outcome không hợp lệ: '{run_outcome}'. Chỉ chấp nhận 'Completed' hoặc 'Passed'.")

    counters = harness_data["counters"]
    if counters.get("failed", 0) > 0 or counters.get("aborted", 0) > 0 or counters.get("error", 0) > 0 or counters.get("timeout", 0) > 0:
        errors.append(f"Harness run có lỗi cấp runner hoặc test thất bại: counters={counters}")

    harness_results = harness_data["results"]
    actual_harness_names = [r["test_name"] for r in harness_results]

    missing_harness = expected_harness_names - set(actual_harness_names)
    if missing_harness:
        for m in sorted(missing_harness):
            errors.append(f"Harness TRX thiếu test dự kiến: '{m}'")

    extra_harness = set(actual_harness_names) - expected_harness_names
    if extra_harness:
        for ex in sorted(extra_harness):
            errors.append(f"Harness TRX chứa test ngoài manifest: '{ex}'")

    for r in harness_results:
        outcome = r["outcome"]
        # Default-deny: Bất kỳ outcome nào != "Passed" đều là lỗi
        if outcome != "Passed":
            errors.append(f"Harness test '{r['test_name']}' không Passed (outcome='{outcome}', msg='{r['error_message']}')")

    if not any("Harness" in e for e in errors):
        print(f"  [OK] Harness đạt chuẩn: {len(harness_results)}/{len(expected_harness)} Passed (100% Xanh).")

    # 3. Thẩm định Regression TRX
    print(f"\n[3/3] Đang thẩm định Regression TRX: {regression_trx_path}...")
    try:
        reg_data = parse_trx(regression_trx_path)
    except Exception as e:
        print(f"  [LỖI ĐỌC REGRESSION TRX]: {e}")
        return 1

    if reg_data["duplicate_names"]:
        for d in reg_data["duplicate_names"]:
            errors.append(f"Regression TRX chứa kết quả trùng lặp cho test: '{d}'")

    reg_run_outcome = reg_data["run_outcome"]
    if reg_run_outcome in ("Aborted", "Error", "Timeout"):
        errors.append(f"Regression run bị hủy hoặc lỗi hạ tầng runner: run_outcome='{reg_run_outcome}'")

    reg_counters = reg_data["counters"]
    if reg_counters.get("aborted", 0) > 0 or reg_counters.get("timeout", 0) > 0 or reg_counters.get("error", 0) > 0:
        errors.append(f"Regression run phát hiện sự cố runner (aborted/timeout/error > 0): counters={reg_counters}")

    reg_results = reg_data["results"]
    actual_reg_names = [r["test_name"] for r in reg_results]

    missing_reg = set(expected_reg_map.keys()) - set(actual_reg_names)
    if missing_reg:
        for m in sorted(missing_reg):
            errors.append(f"Regression TRX thiếu test dự kiến: '{m}'")

    extra_reg = set(actual_reg_names) - set(expected_reg_map.keys())
    if extra_reg:
        for ex in sorted(extra_reg):
            errors.append(f"Regression TRX chứa test ngoài manifest: '{ex}'")

    for r in reg_results:
        name = r["test_name"]
        outcome = r["outcome"]
        error_msg = r["error_message"]
        stack_trace = r["stack_trace"]
        combined_text = f"{error_msg}\n{stack_trace}"

        exp = expected_reg_map.get(name)
        if not exp:
            continue

        fid = exp.get("finding_id", "N/A")

        # NGUYÊN TẮC DEFAULT-DENY TUYỆT ĐỐI:
        # Bất kỳ outcome nào != "Failed" đều bị từ chối ngay lập tức!
        if outcome != "Failed":
            if outcome == "Passed":
                errors.append(f"UNEXPECTED PASS tại {fid} - '{name}': Test này bất ngờ thành công trên baseline!")
                print(f"  [LỖI UNEXPECTED PASS] {fid} - '{name}' bất ngờ Passed!")
            else:
                errors.append(f"OUTCOME BẤT HỢP LỆ tại {fid} - '{name}': outcome='{outcome}'. Chỉ chấp nhận 'Failed'.")
                print(f"  [LỖI OUTCOME BẤT HỢP LỆ] {fid} - '{name}': outcome='{outcome}'!")
            continue

        # Kiểm tra chi tiết các substring đặc thù của assertion failure
        expected_substrings = exp.get("expected_error_substrings", [])
        missing_substrings = [sub for sub in expected_substrings if sub not in combined_text]

        if missing_substrings:
            errors.append(
                f"SAI NGUYÊN NHÂN LỖI tại {fid} - '{name}': Không tìm thấy các dấu hiệu đặc thù: {missing_substrings}. "
                f"Lỗi thực tế nhận được: '{error_msg}'"
            )
            print(f"  [LỖI NGUYÊN NHÂN] {fid} - '{name}': Thiếu dấu hiệu {missing_substrings}")
        else:
            print(f"  [OK] {fid} - '{name}': FAIL đúng nguyên nhân assertion dự kiến.")

    # 4. Tổng kết
    print("\n" + "=" * 68)
    if errors:
        print(f"KẾT QUẢ THẨM ĐỊNH THẤT BẠI: Phát hiện {len(errors)} vi phạm theo nguyên tắc Default-Deny:")
        for idx, err in enumerate(errors, 1):
            print(f"  {idx}. {err}")
        print("=" * 68)
        return 1
    else:
        print("KẾT LUẬN: BASELINE KHỚP HOÀN TOÀN DANH MỤC LỖI ĐÃ BIẾT TRONG MANIFEST.")
        print(f"  - HarnessCheck: {len(expected_harness)}/{len(expected_harness)} Passed (100% Xanh).")
        print(f"  - RegressionBug: {len(expected_regression)}/{len(expected_regression)} Failed đúng assertion.")
        print("  - Không có test skipped, không có test bất ngờ pass hoặc lỗi hạ tầng runner.")
        print("  - CẢNH BÁO: ĐÂY LÀ KẾT QUẢ KHỚP LỖI CŨ, KHÔNG PHẢI TẤT CẢ TEST ĐÃ XANH.")
        print("=" * 68)
        return 0


def main():
    parser = argparse.ArgumentParser(description="Thẩm định báo cáo TRX baseline theo nguyên tắc Default-Deny.")
    parser.add_argument("--harness-trx", required=True, help="Đường dẫn file TRX harness check")
    parser.add_argument("--regression-trx", required=True, help="Đường dẫn file TRX regression baseline")
    parser.add_argument("--manifest", required=True, help="Đường dẫn file JSON manifest các lỗi được phép")

    args = parser.parse_args()
    sys.exit(verify(args.harness_trx, args.regression_trx, args.manifest))


if __name__ == "__main__":
    main()
