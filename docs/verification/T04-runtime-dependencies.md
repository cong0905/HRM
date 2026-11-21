# T04 — Hướng dẫn kiểm tra runtime và dependency

**Trạng thái: VERIFIED**, Codex nghiệm thu theo ủy quyền chủ dự án ngày 11/11/2026. [Biên bản](../decisions/T04-signoff.md).

**Cập nhật sau yêu cầu sửa hết cảnh báo:** Anti đã sửa source, Codex review và kiểm chứng độc lập: **0 warning, 0 error** ở hai full builds. Gate mới nhất: `runs/warnings-cleanup-final/result.json` PASS. Xem [kết quả trước/sau](warnings-cleanup/verification.md). Các số liệu 51 warnings bên dưới mô tả thời điểm nghiệm thu T04 trước lượt sửa này.

Gate `runs/T04-acceptance/result.json` đã **PASS**: build solution/test, restore locked và audit sạch; 3 runtime compatibility + 8 characterization + 5 harness PASS; 8 regression FAIL đúng manifest; 42 Python tests PASS. Graph code cập nhật thành công sau chỉnh sửa. Có 51 warning compiler được giao owner, không suppress hoặc coi là đã sửa.

## Bản triển khai

- SDK pin: **10.0.404**. Runtime: **10.0.15**. EF Core/Extensions: **10.0.15**.
- Năm project sản phẩm và regression suite đều đã nâng .NET 10; WinForms vẫn build trên Windows.
- Phiên bản tập trung tại `Directory.Packages.props`; sáu `packages.lock.json` được giữ cùng source.
- Quyết định, lý do chọn package và ranh giới: [ADR-0007](../adr/0007-runtime-va-dependency-dotnet10.md).

## Chạy lại trên máy Windows

Tại thư mục gốc repository:

```powershell
powershell -File tools/Install-Dotnet.ps1
python -B scripts/verify_t04.py --dotnet .dotnet/dotnet.exe
```

Máy đã cài đúng SDK trên PATH hoặc runner CI:

```powershell
python -B scripts/verify_t04.py
```

Script tạo thư mục evidence mới mỗi lần, không ghi đè lượt cũ. Có thể dùng `--output <thu-muc-moi>`. CI dùng chính script này và actions/setup-dotnet đọc global.json; không có phiên bản SDK thứ hai viết tay trong YAML. Workflow GitHub chưa được chạy từ thay đổi local khi chưa publish source; không coi local PASS là một GitHub run đã thành công.

## Cách đọc evidence

Lượt đầy đủ trước khi bổ sung test runtime riêng: `runs/T04-final/result.json` PASS. Lượt nghiệm thu gồm test runtime riêng: `runs/T04-acceptance/`.

| Bằng chứng | Ý nghĩa |
|---|---|
| `result.json` | Lệnh, exit code, SDK, SHA256 source/package inputs, trạng thái gate |
| `restore-*-locked.*.log` | Restore thực sự thành công với lockfile, audit bật |
| `build-*.stdout.log` | Build toàn bộ solution và regression suite trên Release |
| `solution-resolved.json`, `tests-resolved.json` | Tất cả dependency trực tiếp/bắc cầu và phiên bản resolved |
| `solution-vulnerable.json`, `tests-vulnerable.json` | Kết quả audit theo nguồn NuGet tại lúc chạy |
| `runtime-compatibility/*.trx` | Process .NET10, EF10, System.Text.Json10; model/FK/index và sinh migration SQL offline |
| `characterization/*.trx` | 8 ca mapping/partial update/tracking T03 |
| `baseline/run-evidence.json` và TRX | 5 harness PASS; 8 lỗi legacy FAIL đúng manifest |
| `python-verifiers.*.log` | Tests của các bộ thẩm định, gồm 9 ca gate audit T04 |
| `warning-register.json` | Mỗi warning compiler và task chịu trách nhiệm |

Trước nâng cấp: `t04/before-vulnerabilities.json` ghi System.Text.Json 8.0.0 và hai advisory. Metadata SDK đã lưu ở `t04/dotnet-10-release-metadata.json`. Không thêm direct System.Text.Json; shared runtime cung cấp bản 10.0.15.

## Giới hạn được giữ rõ

Các test baseline RED là lỗi nghiệp vụ chưa sửa, thuộc task khác. T04 không thay quy tắc tiền/quyền/phép. Không kiểm UI trực quan, không chạy SQL Server migration, không gửi email, không tạo API T05 và không deploy. Compiler warnings còn lại có owner trong register; không tắt nullable hoặc audit.
