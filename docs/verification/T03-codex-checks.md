# Kiểm tra độc lập của Codex — T03

**Quyết định hiện tại: VERIFIED.** Codex đã nghiệm thu theo ủy quyền trực tiếp của chủ dự án ngày 11/11/2026, sau khi đối chiếu hash và TRX với artifact hiện tại. Xem [T03-signoff.md](../decisions/T03-signoff.md). Các dòng READY_FOR_REVIEW bên dưới là trạng thái trước quyết định nghiệm thu, được giữ làm lịch sử kiểm chứng.

## Kết quả hiện tại — hoàn thiện T03 ngày 11/11/2026

- **8/8 Characterization PASS**: 6 ca F10 (mapping/partial update/đồng bộ lương) và 2 ca F11 (tracking).
- Test F10 đã `ChangeTracker.Clear()` trước khi đọc lại dữ liệu lưu và trước truy vấn mapping: không dùng entity đang nằm sẵn trong tracker để khẳng định dữ liệu đã lưu/quan hệ đã được load.
- Test F11 chỉ ra: entity detached + SaveChanges không ghi thay đổi; đi qua `NhanVienRepository.UpdateAsync` có attach/update thì lưu được. `Program.cs` hiện tại không bật NoTracking toàn cục. Không coi phát hiện snapshot cũ là lỗi của mọi đường cập nhật hiện tại.
- Baseline: **5/5 HarnessCheck PASS; 8/8 RegressionBug FAIL đúng manifest**, bộ thẩm định exit 0; không skip hoặc lỗi runner mới.
- `graphify update .` exit 0, graph code hiện có 3387 nodes và 6230 edges. Không chạy lại semantic extraction Markdown.
- Chỉ cập nhật test, tài liệu T03 và graph; không sửa service/migration sản phẩm hoặc các task đang làm khác.

Lệnh đã chạy, đều exit 0 ở cấp test T03/bộ thẩm định:

```powershell
dotnet test backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore --configuration Debug --filter Category=Characterization --logger trx --results-directory docs/verification/runs/T03-completion-characterization
python -B scripts/run_and_verify_baseline.py --configuration Debug --results-dir docs/verification/runs/T03-completion-baseline
graphify update .
```

TRX: `runs/T03-completion-characterization/nguye_MSI_2026-11-11_12_38_13.trx`.
Baseline: `runs/T03-completion-baseline/run-evidence.json` và hai file TRX cùng thư mục.
Tóm tắt kiểm tra máy đọc: `runs/T03-completion-summary.json`.

**Giới hạn:** EF InMemory kiểm tra mapping/tracking, không chứng minh transaction hoặc concurrency trên SQL Server. Các quy tắc sửa nghiệp vụ/API mới vẫn thuộc task phụ trách trong transfer map. Trạng thái T03: READY_FOR_REVIEW, chờ chủ dự án VERIFIED.

Các mục bên dưới là dấu vết các lượt kiểm tra trước đó (khi chỉ có 6 test T03).

Ngày 11/11/2026, Codex trong chat trực tiếp chạy (không phải lời báo của Antigravity):

```powershell
dotnet test backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore --filter Category=HarnessCheck --logger trx --results-directory docs/verification/runs/T03-codex-baseline
```

Exit code 0. Passed 5, Failed 0, Skipped 0, Total 5. TRX:
`docs/verification/runs/T03-codex-baseline/nguye_MSI_2026-11-11_12_18_17.trx`.

Đây chỉ là kiểm tra baseline, chưa phải kiểm tra characterization tests T03 chưa được tạo tại thời điểm chạy. Phiên sandbox Anti trước đó bị VSTest abort do testhost không kết nối sau 90 giây; không thay kết quả đó bằng kết quả độc lập này trong báo cáo Anti.

Bridge sau cập nhật quyền: `python -B -m unittest -v test_bridge` tại `tools/hrm-ai-bridge` exit 0, 9/9 tests PASS (mô phỏng provider; không chứng minh kết nối model).

## Kiểm chứng bản sửa cuối

```powershell
dotnet test backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj --no-restore --filter Category=Characterization --logger trx --results-directory docs/verification/runs/T03-codex-final
python -B scripts/run_and_verify_baseline.py --configuration Debug --results-dir docs/verification/runs/T03-codex-regression-final
graphify update .
```

Tất cả 3 lệnh exit 0. Lệnh đầu build source hiện tại và chạy 6/6 characterization tests PASS, không skip. TRX: `runs/T03-codex-final/nguye_MSI_2026-11-11_12_31_17.trx`.

Lệnh thứ hai thẩm định 5/5 HarnessCheck PASS và 8/8 RegressionBug FAIL đúng manifest (lỗi legacy đã biết, chưa được sửa ở T03); không có test skip, không có lỗi runner. Evidence máy đọc: `runs/T03-codex-regression-final/run-evidence.json`.

Graph code cập nhật thành công: 3377 nodes, 6200 edges; Markdown semantic extraction không được chạy lại.

Ba ghi nhận trong `T03-codex-review-notes.md` đã được xử lý: constructor 3 tham số build được, migration F01 được đối chiếu checkout hiện tại, F01/F08 có phạm vi schema EF rõ ràng. Thẻ T03 và hai backlog đã đồng bộ READY_FOR_REVIEW, chưa VERIFIED.
