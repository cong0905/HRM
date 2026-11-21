# Biểu đồ Trình tự Duyệt Đơn Nghỉ phép Đồng thời (Sequence Diagram - Concurrent Leave Approval)

Tài liệu này mô tả chi tiết quy trình xử lý giao dịch khi **hai Quản lý HR cùng thao tác duyệt đơn nghỉ phép đồng thời** trên hệ thống HRM, chứng minh cơ chế bảo vệ quỹ phép không bị âm và bảo toàn tính toàn vẹn giao dịch bằng cách kết hợp:
1. **Khóa bi quan tại mức hàng cơ sở dữ liệu (`UPDLOCK, ROWLOCK`)**.
2. **Khóa lạc quan thông qua thẻ kiểm soát phiên bản (`RowVersion`)**.
3. **Ràng buộc toàn vẹn cứng tại cơ sở dữ liệu (`CHECK (RemainingDays >= 0)`)**.
4. **Sổ cái biến động lịch sử (Append-Only `LeaveBalanceLedger`)**.

---

## 1. Biểu đồ Trình tự Xử lý (Mermaid Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor HR1 as "Quản trị HR 1 (Browser A)"
    actor HR2 as "Quản trị HR 2 (Browser B)"
    participant API as "HRM.Api (LeaveController)"
    participant App as "HRM.Application (ApproveLeaveHandler)"
    participant DB as "SQL Server 2022 (HrmDbContext)"
    participant Outbox as "Transactional Outbox (EmailQueue)"

    Note over HR1, HR2: Tình huống: Nhân viên còn lại 1.0 ngày phép tồn.<br/>Cả hai Quản lý HR đồng thời mở giao diện và bấm 'Phê duyệt' 2 đơn khác nhau (mỗi đơn 1.0 ngày).

    par Yêu cầu Phê duyệt từ HR 1
        HR1->>+API: POST /api/v1/leave/requests/{id1}/approve<br/>[Cookie HttpOnly, Anti-CSRF Header]
        API->>+App: Handle(ApproveLeaveCommand { RequestId: id1, ApproverId })
        App->>+DB: BEGIN TRANSACTION (Isolation: ReadCommitted)
        App->>DB: SELECT * FROM LeaveBalances WITH (UPDLOCK, ROWLOCK)<br/>WHERE EmployeeId = @empId AND Year = @year
        Note right of DB: DB cấp Exclusive/Update Lock cho HR 1.<br/>RemainingDays = 1.0, RowVersion = 0x0001
    and Yêu cầu Phê duyệt từ HR 2 (Gần như đồng thời)
        HR2->>+API: POST /api/v1/leave/requests/{id2}/approve<br/>[Cookie HttpOnly, Anti-CSRF Header]
        API->>+App: Handle(ApproveLeaveCommand { RequestId: id2, ApproverId })
        App->>+DB: BEGIN TRANSACTION (Isolation: ReadCommitted)
        App->>DB: SELECT * FROM LeaveBalances WITH (UPDLOCK, ROWLOCK)<br/>WHERE EmployeeId = @empId AND Year = @year
        Note right of DB: DB phát hiện hàng đang giữ UPDLOCK bởi HR 1.<br/>HR 2 bị chặn (WAIT LOCK) trên dòng dữ liệu này.
    end

    %% HR 1 Tiếp tục xử lý
    Note over App, DB: HR 1 kiểm tra điều kiện nghiệp vụ:<br/>RemainingDays (1.0) >= RequestedDays (1.0) -> HỢP LỆ!
    App->>DB: UPDATE LeaveBalances<br/>SET RemainingDays = 0.0, UsedDays = UsedDays + 1.0<br/>WHERE Id = @balanceId AND RowVersion = 0x0001
    App->>DB: INSERT INTO LeaveBalanceLedgers (LeaveRecordId, DaysChanged, BalanceBefore, BalanceAfter)<br/>VALUES (id1, -1.0, 1.0, 0.0)
    App->>DB: UPDATE LeaveRecords SET Status = 'Approved', ApprovedBy = @hr1<br/>WHERE Id = id1
    App->>DB: INSERT INTO EmailOutboxMessages (Recipient, Template, Status)<br/>VALUES (empEmail, 'LeaveApprovalNotice', 'Queued')
    App->>DB: COMMIT TRANSACTION
    Note right of DB: Giao dịch HR 1 Commit thành công.<br/>RowVersion mới = 0x0002, RemainingDays = 0.0.<br/>Khóa UPDLOCK được giải phóng!
    DB-->>-App: Commit Success
    App-->>-API: Result.Success()
    API-->>-HR1: 200 OK { message: "Duyệt đơn thành công", remainingDays: 0.0 }

    %% HR 2 Thức dậy sau khi khóa giải phóng
    Note over App, DB: Khóa được nhả, truy vấn của HR 2 hoàn tất và đọc dữ liệu mới nhất:<br/>RemainingDays = 0.0, RowVersion = 0x0002
    Note over App, DB: HR 2 kiểm tra nghiệp vụ:<br/>RemainingDays (0.0) < RequestedDays (1.0) -> KHÔNG ĐỦ SỐ DƯ!
    Note right of App: Domain Guard phát hiện vi phạm bất biến nghiệp vụ.<br/>Hoặc nếu cố tình UPDATE, Ràng buộc DB Check (RemainingDays >= 0) sẽ nổ lỗi.
    App->>DB: ROLLBACK TRANSACTION
    Note right of DB: Toàn bộ giao dịch của HR 2 bị hủy bỏ.<br/>Không có bản ghi partial update nào được lưu lại.
    DB-->>-App: Rollback Completed
    App-->>-API: Result.Failure(LeaveErrors.InsufficientBalance)
    API-->>-HR2: 400 Bad Request / 409 Conflict<br/>{ code: "INSUFFICIENT_LEAVE_BALANCE", message: "Số dư ngày phép của nhân viên đã hết (0 ngày)." }
```

---

## 2. Các Tầng Phòng Ngự Chống Lỗi Đồng Thời (Concurrency Safeguards)

Hệ thống triển khai mô hình phòng vệ 4 lớp đối với phân hệ nghỉ phép:

| Lớp phòng ngự | Cơ chế kỹ thuật | Trách nhiệm |
|---|---|---|
| **1. Application Domain Guard** | Kiểm tra số dư trước khi ghi (`balance.RemainingDays >= request.Days`) | Ngăn chặn các yêu cầu không hợp lệ ngay tại tầng ứng dụng |
| **2. Concurrency Lock** | `WITH (UPDLOCK, ROWLOCK)` trong EF Core / SQL Server | Tuần tự hóa các giao dịch ghi cạnh tranh trên cùng một nhân viên |
| **3. Optimistic Token** | Cột `RowVersion` kiểu `timestamp/rowversion` | Bắt lỗi tranh chấp phiên bản (Lost Update) giữa 2 trình duyệt |
| **4. Database Constraint** | `CHECK (RemainingDays >= 0)` | Lưới an toàn cuối cùng cấp CSDL, bảo đảm quỹ phép tuyệt đối không bị âm |

---

## 3. Liên kết Tài liệu Liên quan

- [Sơ đồ ERD](file:///d:/dotnet%20project/docs/diagrams/erd-core-entities.md)
- [Báo cáo Kiểm thử T24 - Kịch bản Hai trình duyệt duyệt đồng thời](file:///d:/dotnet%20project/docs/verification/T24-e2e-security-report.md)
- [Kịch bản Demo Bảo vệ](file:///d:/dotnet%20project/docs/demo/demo-scenarios.md)
