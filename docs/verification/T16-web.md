# T16 — Anti triển khai khung web, Codex review độc lập

Ngày13/11/2026. **VERIFIED kỹ thuật theo quyền chủ dự án đã giao.** Chủ dự án yêu cầu “bảo anti làm phần tiếp theo còn m sẽ kiểm tra nhé”. Không ghi nhận human UAT hoặc remote CI.

## Kết quả cuối đã chạy

- **Backend gate388/388 PASS**:383 hồi quy+5 ca SQL access/permissions mới. Giữ đủ matrices T05–T15; build.NET10.0.404 Release0warning/0error, locked restore, audit, contracts drift và legacy gate đạt. **72 Python verifier tests PASS**. Thiếu SQL ghi NOT_RUN/0 executed/exit2.
- **Web gate PASS**: npm ci từ lockfile, audit **0 vulnerabilities**, typecheck app lẫn Vite/Playwright config, ESLint thực, **20/20 component tests**, production build. Source SHA-256 backend và web cuối khớp từng gate.
- **6/6 ca Playwright do Anti viết PASS,0skip**, chạy Chrome desktop thật qua API HTTPS/SQL fixture; không route.fulfill fake success. Cấu hình review dùng Chrome cài sẵn; bộ mặc định có thêm mobile project chưa chạy nguyên bộ đó, viewport375 được kiểm riêng.
- **6 regression độc lập Codex PASS**:2 client/CSRF+4 session/cache, gồm5 lỗi có RED→GREEN và1 guard late response. **12 kiểm tra browser độc lập PASS**, thêm lượt visual375px/drawer260px ổn định.
- SQL Server thật,12 migrations cũ, không migration mới. **9 database GUID** của toàn bộ review/rerun/browser đã cleanupCompleted=true; truy vấn metadata theo tên xác nhận **0 còn lại**. Host Vite/Kestrel đã dừng; cert PEM/key và tài khoản/password fixture tạm đã xóa cùng thư mục tạm.
- Source db/, backend cũ và toàn bộ file test cũ giữ nguyên; chỉ thêm endpoint AccessController, tests/harness và các tích hợp T16. Legacy gate vẫn khớp8 lỗi baseline đã biết, không tuyên bố đã sửa chúng. Graphify cập nhật13006 nodes. CI backend/web đã cấu hình, **remote GitHub CI chưa chạy**.

## Phân công có bằng chứng

Anti thực viết endpoint permission, React/TypeScript/Vite/Router/TanStack Query/Ant Design, auth forms, permission navigation, test component/Playwright, cấu hình và README tại web/. Conversation CLI thật: `7693dcaf-8a08-4f4c-af04-0b81b369d0b9`. Sau bản triển khai đầu có **4 vòng sửa theo findings Codex**. Các native turn đều exit0 và log ghi file targets trong đúng web/, AccessController.cs và Anti report.

Lượt CLI đầu exit0/SUCCESS nhưng rỗng vì command headless bị từ chối; không coi đó là triển khai thành công. Đã tiếp tục bằng native file tools, không cấp lại/bypass quyền terminal. Anti ghi tests NOT_RUN; Codex thực chạy độc lập. Xem `docs/verification/t16-implementation/` execution/logs/collaboration-check.json.

Codex viết tests SQL/regressions độc lập, harness Kestrel+SQL+cert tạm, sinh contracts từ backend thật, lockfile/CI/gate và review. Codex sửa đúng1 dòng import defineConfig sang vitest/config cho typing config kết hợp; luồng UI/auth do Anti sửa theo review. Không gắn tên Anti cho phần runner của Codex.

## Hành vi T16

- `GET /api/v1/access/me`: authenticated identity/username/roles/permissions từ ISecurityContext, Cache-Control:no-store; không trả profile/CCCD/ngân hàng/lương, không thêm quyền. SystemAdmin không tự có HR/Payroll. OpenAPI/client sinh lại, DTO alias theo schema.
- Cookie web HttpOnly/Secure qua HTTPS same-origin proxy. CSRF lấy từ auth/csrf, header X-XSRF-TOKEN trên mutation kể cả generated-client Request; token mới sau login, không localStorage/sessionStorage/bearer token web. CSRF invalid không tự replay POST.
- Login/logout/forgot/reset/activate/change-password, loading/error/retry/ProblemDetails errorCode+traceId/409; permission guards và menu cùng key server thật.401 bootstrap không retry loop,403 không tự logout, lỗi mạng có retry hữu hạn. Logout/password change/expired session hủy dữ liệu private; late response không repopulate cache.
- Forgot phản ánh deliveryMode thật; not-configured không ghi email đã gửi. Reset code được giữ memory và dọn khỏi URL. Các nghiệp vụ T17+ chỉ có placeholder ghi rõ chưa triển khai, không fake bảng dữ liệu hoặc counters.
- Field labels/autocomplete, keyboard tab/Enter/Escape, reload giữ cookie, private/public viewport375 và drawer260 đã kiểm qua Chrome thật. Ảnh sau khi animation hoàn tất được xem trực quan.

## Findings đã đóng

1. Generated openapi-fetch truyền Request nhưng wrapper chỉ đọc init.method/headers: POST mất CSRF/Content-Type. Test độc lập RED2 client; sửa giữ Request method/body/headers/signal và merge overrides.
2. Đọc nhầm code thay cho errorCode khiến csrf.invalid/trace sai: sửa theo ProblemDetails schema, refresh cho lần thử kế tiếp, không replay mutation.
3. Bootstrap401 vẫn giữ private cache; logout503 bị nuốt thành success. Hai ca session RED; sửa purge cache và báo đúng server logout chưa xác nhận, không giả cookie đã hủy.
4. **Observer sau đổi mật khẩu:** clear/remove cả auth query rồi setData tạo query mới, UI có thể vẫn authenticated. Ca độc lập RED; sửa giữ query auth đang được quan sát và setnull, chỉ bỏ private queries. Browser thực xác nhận đổi mật khẩu204→cookie/session mất hiệu lực→login.
5. Permission hrm.* tự đặt không tồn tại server: thay key thật employees.*, attendance.*, payroll.*, accounts.*; kiểm server roles SQL và UI SystemAdmin/Payroll/Employee thật.
6. Dependency cũ có critical/high; cập nhật exact Vitest5.1.0, Router7.18.5, ESLint10.14.0 và các bản tương thích, override minimatch11.0.8. Không audit-force/ignore. [Tinypool advisory](https://github.com/advisories/GHSA-85c8-ppgw-ccpr), [minimatch advisory](https://github.com/advisories/GHSA-g3hv-75wg-m249). Compatibility đã kiểm bằng lint/typecheck/tests/build thật; override cần theo dõi khi upstream cập nhật range.
7. Lint ban đầu chỉ alias tsc, props AntD sai và unused imports; thêm ESLint thật và sửa lỗi. Tests ban đầu chưa mô hình đúng Request/ProblemDetails/async DOM event và jsdom AbortSignal khác realm; sửa test adapters/fixtures, giữ assertions và không giảm bảo mật sản phẩm.

## Những lỗi thuộc runner Codex, đã phân biệt rõ

Contracts lần đầu sinh từ binary cũ chưa có AccessController nên backend run đầu fail ở drift dù388 tests đạt. Đã build/sinh lại, rerun gate; bản cuối T16-backend-final-v3 PASS và hash hiện tại khớp. TLS harness Windows cần import self-signed PFX để Schannel dùng key được; đã thêm probe401 trước READY. Script browser mở rộng phải chờ bootstrap/animation và bắt Promise đúng; đã sửa script, giữ các run lỗi, không coi timeout script là lỗi UI. Các lượt thử đổi mật khẩu dùng fixture mới hoặc cập nhật credential trong temp, không reset tài khoản sản phẩm để test xanh.

## Bằng chứng và cách chạy lại

- `docs/verification/runs/T16-backend-final-v3/result.json`, integration TRX, SQL ledger, contracts và legacy logs.
- `docs/verification/t16-review/web-final-v3/result.json`: web hashes và6 bước ci/audit/typecheck/lint/component/build.
- `client-review-final.json`, `session-review-final.json`, RED logs, `playwright-live-result.json` cùng6 JSON riêng, `browser-independent.json`12 checks, `browser-visual.json`, cleanup-check.json, completion-check.json.
- `backend/tests/HRM.BrowserReview/`: test-only Kestrel/SQL harness, không fake identity; CLI dừng qua stopFile hoặc25phút timeout, cleanup GUID DB/temp. Credentials test chỉ nằm file temp được chỉ bởi browser-host.json khi host đang chạy.
- `docs/decisions/T16-web.md`, `web/README.md`, `docs/ai-handoff/T16-anti-web.md` và các review-revise*.md.

Từ D:\dotnet project, dùng output mới cho backend:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t16.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T16-owner-rerun
cd web
npm ci --ignore-scripts
npm audit
npm run typecheck
npm run lint
npm test
npm run build
```

Browser: dựng API HTTPS và Vite proxy bằng cert riêng theo web/README.md, cấp tài khoản fixture qua E2E_BASE_URL/E2E_ADMIN_USERNAME/E2E_ADMIN_PASSWORD, rồi chạy Playwright. Bằng chứng review này đã thực chạy6 desktop cases và kiểm375px riêng; không coi skip vì thiếu credential là đạt E2E.

## Giới hạn

Build web có cảnh báo chunk minified>500kB; chưa tối ưu lazy-loading hoặc load/performance production. Chỉ kiểm keyboard/labels và viewport đã nêu, chưa audit WCAG đầy đủ/all devices. SMTP/PDF/mail thật/mobile app/business screens T17+ chưa hoàn tất từ T16. Không remote CI, deploy, commit/push hoặc human UAT. Không dùng HRM_System. VERIFIED là nghiệm thu kỹ thuật theo quyền chủ dự án đã giao, không ghi chủ dự án đã tự thao tác/review.
