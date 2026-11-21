# ADR-0007 — SDK .NET 10, EF Core 10 và dependency có khóa

Ngày: 11/11/2026. Quyết định kỹ thuật do Codex thực hiện theo ủy quyền của chủ dự án cho T04. Phạm vi nghiệm thu được ghi riêng trong biên bản T04 sau khi gate hoàn tất.

## Bối cảnh

ADR-0002 đã chọn .NET 10 LTS cho backend. Checkout trước T04 còn sáu project target .NET 8, wildcard EF `8.0.*`, không có global.json/lockfile. Máy có SDK 9.0.304, chưa có SDK 10. Scan trước nâng cấp xác nhận System.Text.Json 8.0.0 bắc cầu trong DAL/BLL/GUI có hai advisory GHSA-hh2w-p6rv-4g7w và GHSA-8g4q-xg66-9fp4.

Theo [chính sách Microsoft, cập nhật 10/11/2026](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), .NET 8/9 đã hết hỗ trợ, .NET 10 LTS còn hỗ trợ tới 14/11/2028 và cần theo bản vá đang hỗ trợ. Metadata chính thức tại lúc triển khai chỉ định SDK **10.0.404**, runtime **10.0.15**.

## Quyết định

1. Pin SDK **10.0.404**, rollForward disable và không chấp nhận prerelease trong global.json. Dùng runtime .NET/ASP.NET Core/WindowsDesktop **10.0.15** của SDK này. Cài SDK cục bộ `.dotnet/` từ archive Microsoft, kiểm SHA512; không thay SDK hệ thống.
2. Bốn thư viện `HRM.Common`, `HRM.Domain`, `HRM.DAL`, `HRM.BLL` và regression tests target **net10.0**. WinForms target **net10.0-windows**. Không tách một nhánh EF8 vì build hiện tại cho phép nâng đồng bộ; không phải duy trì hai phiên bản model/provider cùng lúc.
3. EF Core/provider/design/InMemory cùng **10.0.15**; Microsoft.Extensions Configuration/DI cùng **10.0.15**. Chuyển EF PowerShell Tools package sang manifest CLI `dotnet-ef` **10.0.15**, giữ Design ở các project cần design-time/startup. Microsoft.NET.Test.Sdk **18.11.1** đã được kiểm chứng với net10; giữ xUnit 2.9.2, runner 2.8.2, Moq 4.20.72 và BCrypt.Net-Next 4.1.0 vì không cần đổi để đạt T04.
4. [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) đặt phiên bản ở Directory.Packages.props. Sáu packages.lock.json khóa cả cây bắc cầu. Luồng kiểm tra/CI chỉ dùng `restore --locked-mode`; khi chủ động đổi dependency mới dùng restore --force-evaluate rồi review lock diff.
5. Nguồn package/audit duy nhất là nuget.org qua HTTPS trong NuGet.Config. NuGetAudit=true, Mode=all, Level=low. NU1900–NU1905 và lỗi downgrade/conflict được đưa thành error; không dùng NoWarn hoặc audit suppression. Script gate kiểm tra cả báo cáo resolved và vulnerable, từ chối dữ liệu thiếu hoặc lỗi nguồn audit.
6. Không thêm direct System.Text.Json để che dependency bắc cầu. Nhánh cũ Microsoft.Extensions.Configuration.Json 8.0.0 yêu cầu System.Text.Json 8.0.0 trong nuspec. Nâng dependency nguồn và runtime lên 10: System.Text.Json không còn là package NuGet trong resolved graph, được cung cấp bởi shared framework 10.0.15. Test runtime xác minh assembly major 10; audit package và bản vá runtime là hai loại bằng chứng riêng.
7. Năm property runtime WinForms được đánh dấu DesignerSerializationVisibility.Hidden/Browsable(false) để sửa WFO1000 đúng nghĩa, gồm TokenValue. Không suppress analyzer. Xem [hướng dẫn WFO1000 của Microsoft](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/compiler-messages/wfo1000).
8. CI Windows đọc chính global.json và chạy cùng `scripts/verify_t04.py` với local. Script kiểm SDK chính xác, locked restore, build solution/test Release không incremental, tool version, audit, runtime compatibility, characterization, baseline và Python verifier tests. File nguồn/package được hash trước và sau để phát hiện thay đổi trong lúc kiểm tra.

## Phạm vi backend và giới hạn

Directory.Build.props cung cấp default net10.0 cho project mới; thư viện tái sử dụng đã build/test trên net10.0. **Chưa tạo API sản phẩm**: controller/contract/hosting thuộc T05. Không gọi việc có thư mục backend/tests là API đã tồn tại. T04 nghiệm thu nền tảng cho backend mới, không nghiệm thu chức năng T05.

Migration SQL được sinh offline dưới EF10 và model được kiểm tra lại các ràng buộc T02. Không chạy migration vào database. Bằng chứng SQL Server T02 trước đây thuộc EF8; phải diễn tập lại trên database test trước khi sử dụng đường migration EF10 với dữ liệu thật. InMemory và script offline không thay bằng chứng transaction/concurrency SQL Server.

## Cảnh báo và bảo trì

**Cập nhật cùng ngày sau nghiệm thu:** 51 compiler warnings đã được sửa theo yêu cầu chủ dự án, full build solution và test project hiện đạt **0 warning, 0 error**. Gate mới nhất và phạm vi kiểm chứng tại [warnings-cleanup/verification.md](../verification/warnings-cleanup/verification.md). Đoạn sau lưu hiện trạng tại thời điểm quyết định T04 ban đầu.

51 cảnh báo compiler legacy được giữ nguyên hiển thị (nullable và một field không dùng); không phải cảnh báo package/advisory. `warning-register.json` của lượt nghiệm thu liệt kê từng file/dòng/code/message và task phụ trách T06/T08/T10/T13/P01/P07. Các lỗi hành vi legacy còn lại được quản lý bằng manifest baseline, không biến test RED thành xanh bằng cách bỏ test.

Khi SDK/package có bản vá mới: cập nhật global.json/CPM/tool manifest có chủ đích, cài SDK tương ứng, restore --force-evaluate, review cả sáu lockfile rồi chạy lại gate. Không suy từ audit sạch tại một thời điểm rằng không cần cập nhật trong tương lai.
