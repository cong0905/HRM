# T12 — Hợp đồng bất biến liên module

Quyết định: `docs/decisions/T12-invariants.md`. Đây là bản đồ yêu cầu/kiểm chứng; kết quả chạy được ghi riêng trong `T12-invariants.md` và thư mục run, không suy PASS từ bảng này.

| Bất biến | Kiểm chứng bắt buộc |
|---|---|
| Receipt tách actor/operation, hash payload canonical | T12_ScopedKeys_CrossActorAndOperation_CanonicalHashMismatch |
| Không expiry/reuse key trong MVP, host khác vẫn đọc receipt sau400 ngày | T12_Retention_RestartAnd400DayRetry_KeepOriginalReceipt |
| Trước commit rollback event/summary/receipt; sau commit retry không ghi thêm | T12_FaultAtCommit_Attendance_RollbackOrDurableReplay (false,true) |
| Leave status/balance/debit/receipt cùng transaction, mất completion không trừ lần hai | T12_FaultAtCommit_Leave_RollbackOrDurableReplay (false,true) |
| Lỗi audit sau flush rollback trạng thái/quỹ/ledger | T12_AuditFailure_MustRollbackAlreadyFlushedLeaveMutation |
| Phép Approved nguyên ngày chặn hiện diện chuẩn | T12_ApprovedFullDayLeave_BlocksCheckIn |
| Pending chưa tiêu quỹ; sửa0 phút trước approve, sau đó không khôi phục paid minutes/OT | T12_PendingLeaveAllowsWork_ApprovalRequiresZeroCorrection_ThenBlocksPaidWorkAndOt |
| Race check-in/approve cùng ngày chỉ một phía thắng | T12_CheckInVersusApprove_Seeded10Races_ExactlyOneWins, seed0..9 mỗi lượt suite |
| Ngày không làm việc trong khoảng phép không sinh công chuẩn hoặc OT tự động | T12_NonWorkingDayWithinApprovedSpan_EarnsNoStandardOrAutomaticOt |
| Offboard chặn cả2 module, giữ nguyên ledger | T12_Offboard_BlocksBothModules_AndPreservesLedger |
| Liên kết employee/leave/ledger không mở IDOR | T12_Idor_CrossModuleLinks_DoNotExposeOtherEmployees |
| SQL unique summary/debit và CHECK balance thực sự chặn dữ liệu sai | T12_PhysicalConstraints_NegativeControlDetectsRemoval (attendance,debit,balance) |

16 lượt SQL mới. Cổng `scripts/verify_t12.py` yêu cầu ít nhất255 backend tests cùng đầy đủ matrices T05–T12, không fail/skip. 4 Python verifier tests kiểm complete/missing/skipped matrix và NOT_RUN khi thiếu SQL. CI job backend-sql-contract gọi gate T12 với --ci-backend-only, phụ thuộc Windows legacy gate.

## Kiểm tra độ nhạy

Physical negative controls: trên database GUID do fixture tạo, SQL sai phải thất bại với schema gốc; tạm bỏ riêng một index/CHECK, cùng SQL phải thành công. Rollback cả DDL/data, xác nhận trạng thái gốc. Không đụng database HRM_System.

Mutation transaction: script review tạm chèn commit trước audit ở approve; test audit failure phải thấy Approved còn lưu và fail, chứng minh ranh giới transaction có ý nghĩa. Source được restore byte-for-byte trong finally; chỉ chấp nhận các lần GREEN/full gate sau restore. Log `docs/verification/t12-review/mutation-evidence.json`, TRX mutant giữ riêng, không đưa vào số PASS.

## Chạy lại và giới hạn

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t12.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T12-owner-rerun
```

Seed request cố định `HRM-T12-seed-12026:<case>`; các account/DB vẫn có GUID riêng để cô lập. Khi chạy riêng T12 dùng --filter FullyQualifiedName~T12_; chạy ít nhất2 lượt suite rồi full gate cho nghiệm thu. Fixture dùng SQL Server thật, áp dụng migrations và xóa DB sở hữu sau lượt test. Thiếu cấu hình SQL: NOT_RUN/0 executed/exit2, không substitute InMemory hoặc skip. Chưa xác nhận remote CI hay production UAT; chưa kiểm formula lương T13/T14 hoặc UI. Dữ liệu lịch sử có sẵn không được tự sửa.
