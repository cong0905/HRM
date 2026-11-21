# Codex giao việc — Antigravity thực hiện — Codex review

## Cách dùng

1. Dừng các lượt Anti khác đang sửa cùng dự án.
2. Nhờ Codex viết một file giao việc có phạm vi và tiêu chí nghiệm thu cụ thể.
3. Chạy `Start-Bridge.ps1` và nhập đường dẫn file đó, hoặc dùng PowerShell:
   Có thể bấm đúp `BAT_DAU.cmd`, nhập thư mục dự án và đường dẫn file giao việc.
   Bấm đúp `DUNG.cmd` để yêu cầu dừng.

```powershell
& 'D:\dotnet project\tools\hrm-ai-bridge\Start-Bridge.ps1' -TaskFile 'D:\dotnet project\docs\ai-handoff\next-task.md'
```

Đường dẫn next-task.md là ví dụ, chưa có nhiệm vụ T03 được kích hoạt.
Terminal hiển thị PLANNING → IMPLEMENTING → REVIEWING.
Tối đa 3 lượt Anti thực hiện. Khi đạt, dừng ở READY_FOR_USER_REVIEW.
Bạn quyết định VERIFIED; chương trình không tự chuyển task tiếp theo.

## Xem kết quả và dừng

- `latest-run.json`: đường dẫn lượt chạy gần nhất.
- `runs/<run>/state.json`: trạng thái, vòng hiện tại, lỗi nếu có.
- `anti-*.report.md`: báo cáo Anti.
- `review-*.json`: kết luận và yêu cầu sửa của Codex.
- Các file `*.stdout.log`, `*.stderr.log`, `*.execution.json`: bằng chứng lệnh.
- Chạy `Stop-Bridge.ps1` hoặc tạo file rỗng `STOP` cạnh bridge.py để dừng.
  Dừng không hoàn tác các chỉnh sửa đã có. Xem trạng thái và diff trước khi chạy lại.
- Nếu máy tắt đột ngột, có thể còn RUNNING.lock. Xác nhận không còn tiến trình
  bridge/agent đang chạy trước khi xóa lock thủ công. Không tự xóa lock của lượt khác.

## Phạm vi tự động hóa

Đây là các phiên CLI riêng, không phải điều khiển chat Antigravity đang mở hoặc
đánh thức cuộc chat Codex desktop hiện tại. Mỗi lần chạy đọc CONTEXT.md và task file.
Anti tiếp tục đúng conversation_id của vòng hiện tại; không dùng --continue để
tránh lấy nhầm cuộc hội thoại. Reviewer là Codex CLI với tài khoản ChatGPT đã đăng nhập.
Model dùng mặc định CLI; không cam kết trùng model của chat desktop.

Reviewer chỉ đọc; Anti được phép ghi bằng write_file trong workspace được chọn.
Cho phép lệnh trực tiếp dotnet build/test/restore, git status/diff và python -m unittest
với đối số đơn giản, không shell chaining. Không tự cho phép SQL hoặc script tùy ý.
Không bỏ qua sandbox hay bật dangerously-skip-permissions. Các lệnh test/shell
chưa được tài khoản cho phép có thể bị chặn; khi đó vòng lặp dừng NEEDS_USER,
không giả vờ đã chạy test. Đây là giới hạn quyền thực thi, không phải lỗi bàn giao.
Chủ dự án đã cấp quyền ngày 11/11/2026: Graphify query/path/explain/update và
`Get-ChildItem -Path . -Depth 2` có allow-rule riêng, cùng sandbox.
Graphify MCP (`mcp(graphify/*)`) và `graphify --help` cũng được cấp quyền.
Tài khoản hiện tại không hỗ trợ `--mode auto`; các lệnh khác vẫn cần duyệt và bị từ chối
khi headless không thể hỏi.
Quy tắc bằng lời không thay thế cơ chế phân quyền của các CLI.

Đối số test cho phép dấu `=`/`~` để dùng filter. Có thể tiếp tục đúng conversation
của cùng task bằng `run --conversation <id>`; không dùng ID của task khác.
Bằng chứng file ưu tiên file mới thay đổi trước khi áp dụng giới hạn dung lượng.

Khi công cụ shell của reviewer không khả dụng, bộ điều phối tự đọc file văn bản
đã đổi/untracked có giới hạn dung lượng và gửi nội dung cùng SHA256 cho Codex.
Không gửi .env, appsettings, auth/credential files, runtime hoặc thư mục runs.
Đây là review tĩnh trên bằng chứng file được đọc trực tiếp, không phải Codex tự chạy
test. File quá lớn/thiếu được ghi rõ; reviewer phải BLOCKED nếu thiếu bằng chứng
thiết yếu. Không tự tuyên bố các runtime checks đã được kiểm chứng độc lập.

Không tự commit/push, không tự chạy migration lên HRM_System, không triển khai.
Không khởi chạy thường trực lúc mở máy. Các lượt CLI sử dụng hạn mức của tài khoản.

## Công cụ

- Python tiêu chuẩn, không có thư viện Python bên ngoài.
- Antigravity: `%USERPROFILE%\.gemini\bin\agy.exe` hoặc agy trên PATH.
- Codex CLI riêng: `runtime/node_modules/@openai/codex-win32-x64/.../codex.exe`,
  cài từ gói chính thức @openai/codex 0.167.0; không sửa Codex toàn máy.
- Config Codex riêng không nạp config.toml desktop để tránh công cụ Core không
  chạy được từ CLI. Vẫn dùng đăng nhập hiện có, không sao chép token.

```powershell
python -B bridge.py doctor
python -B -m unittest -v test_bridge
```

Tài liệu cơ sở:
- https://antigravity.google/docs/cli/headless/
- https://developers.openai.com/cookbook/examples/codex/build_iterative_repair_loops_with_codex
