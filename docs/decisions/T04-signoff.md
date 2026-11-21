# T04 — Quyết định nghiệm thu runtime và dependency

**Quyết định: VERIFIED trong phạm vi T04, ngày 11/11/2026.**

**Phụ lục sau nghiệm thu cùng ngày:** Theo yêu cầu “kêu anti sửa hết đi”, Antigravity sửa 51 warnings và Codex kiểm chứng lại source cuối: **0 Warning(s), 0 Error(s)** cho solution và test project; gate `docs/verification/runs/warnings-cleanup-final/result.json` PASS. Chi tiết tại [warnings-cleanup/verification.md](../verification/warnings-cleanup/verification.md). Giữ bảng 51 warnings dưới đây làm lịch sử thời điểm nghiệm thu ban đầu, không phải hiện trạng mới nhất.

Người quyết định: Codex, được chủ dự án ủy quyền qua mục tiêu “hãy quyết dihj thay t đến khi có bản T04 đạt chuẩn nhất”. Quyết định dựa trên kiểm tra source hiện tại và lệnh thực chạy; không ghi nhận chủ dự án đã tự chạy test, không cấp quyền nghiệm thu tự động cho task ngoài mục tiêu.

## Đối chiếu từng yêu cầu

| Yêu cầu T04 | Bằng chứng hiện tại | Kết luận |
|---|---|---|
| Runtime được hỗ trợ, thư viện tái sử dụng build được | global.json pin SDK 10.0.404; runtime 10.0.15; bốn thư viện, WinForms và test suite target .NET10. Build solution và test Release exit 0 | Đạt |
| Xác nhận SDK/EF tương thích, có ADR | ADR-0007; EF/design/provider/InMemory 10.0.15; dotnet-ef 10.0.15; 3 runtime compatibility tests PASS, gồm model T02 và SQL migration offline | Đạt |
| Dependency có phiên bản cố định, restore có kiểm soát | CPM; sáu packages.lock.json có version/contentHash; locked restore hai entry point exit 0; SDK không roll-forward/prerelease | Đạt |
| Hai advisory System.Text.Json không còn ảnh hưởng resolved graph | Audit trước ghi 8.0.0 ở DAL/BLL/GUI; audit cuối solution và suite không báo vulnerability ở cả direct/transitive. Không thêm direct System.Text.Json; runtime cung cấp assembly 10.0.15 | Đạt |
| Legacy build được hoặc tách có lý do | Giữ WinForms build được trên net10.0-windows; sửa đúng 5 property serialization, không suppress WFO1000 | Đạt |
| Không tắt nullable/audit, warning quan trọng có owner | Nullable enable; audit all/low; lỗi audit/nguồn audit không bị bỏ qua. 51 warning còn hiển thị và có register chi tiết: P01 23, T06 13, P07 1, T08 14 | Đạt trong chính sách warning của T04 |
| Build/regression/audit/resolved versions/CI cùng SDK pin | Gate `scripts/verify_t04.py` PASS trên source cuối; 8 characterization PASS, 3 runtime PASS, 5 harness PASS; 8 RegressionBug FAIL khớp manifest; 42 Python tests PASS. CI đọc global.json và chạy chính gate này, gate từ chối SDK khác | Đạt cấu hình CI và kiểm chứng local; chưa có GitHub run remote |

## Hồ sơ đối chiếu

- Gate cuối: `docs/verification/runs/T04-acceptance/result.json` (PASS, 15 bước exit 0).
- Đã đối chiếu lại toàn bộ SHA256 input trong result.json với source/package hiện tại: khớp. Gate tự kiểm trước/sau chạy để không nhận kết quả của source thay đổi giữa chừng.
- Các báo cáo resolved/vulnerable, TRX, baseline evidence và warning register nằm cùng thư mục lượt gate, không chỉ là báo cáo bằng lời của implementer.
- Metadata và checksum SDK: `docs/verification/t04/dotnet-10-release-metadata.json`, `sdk-installation.json`; SDK chỉ cài trong workspace.
- Hướng dẫn chạy lại: [T04-runtime-dependencies.md](../verification/T04-runtime-dependencies.md).

## Ranh giới không mở rộng

1. T04 chuẩn bị nền tảng backend .NET10; API/contract/hosting sản phẩm vẫn thuộc T05, chưa được triển khai bởi quyết định này.
2. Không nghiệm thu lại SQL Server thật dưới EF10. Tests model và sinh SQL offline không chứng minh transaction/concurrency hoặc nâng cấp DB thật. T02 có bằng chứng lịch sử riêng; phải diễn tập trên DB test trước khi dùng migration EF10 với dữ liệu thật.
3. 8 lỗi nghiệp vụ legacy còn nguyên theo manifest. Đây là điều kiện baseline có chủ đích, không phải “tất cả test xanh”, không bị bỏ/xóa để qua gate.
4. 51 warning compiler legacy được giao owner, chưa coi là đã sửa. Đây không phải warning NuGet/advisory hoặc cảnh báo runtime hết hỗ trợ.
5. Workflow GitHub mới chưa chạy remote vì source chưa được publish. Đã kiểm chứng entry point đầy đủ local và đối chiếu YAML dùng đúng global.json; không bịa trạng thái CI remote. Không commit/push/deploy hoặc thay dữ liệu thật trong T04.

Đồng bộ VERIFIED trong thẻ T04 và hai backlog theo ủy quyền của chủ dự án. Chỉ kết luận đạt các yêu cầu T04 trong phạm vi trên, không chứng nhận hệ thống đủ điều kiện production.
