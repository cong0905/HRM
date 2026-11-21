# Biểu đồ Trình tự Tính Lương Snapshot và Đóng Băng Kỳ Lương (Sequence Diagram - Payroll Calculation & Immutability)

Tài liệu này mô tả chi tiết chu trình tính toán lương, kiểm soát phân tách trách nhiệm (Segregation of Duties), tạo bản chụp số liệu bất biến (**Immutable Snapshot**) và niêm phong kỳ lương (**Locked Period**), đảm bảo nguyên tắc: **Bất kỳ thay đổi nào về mức lương cơ bản hoặc chấm công trong tương lai đều không làm thay đổi phiếu lương của kỳ đã đóng**.

---

## 1. Biểu đồ Trình tự Xử lý (Mermaid Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor PayrollClerk as "Chuyên viên Lương (User A)"
    actor HRManager as "Trưởng phòng Nhân sự (User B)"
    actor Employee as "Nhân viên (Mobile App)"
    participant API as "HRM.Api (PayrollController)"
    participant App as "HRM.Application (PayrollEngine)"
    participant DB as "SQL Server 2022 (HrmDbContext)"

    %% Giai đoạn 1: Khởi tạo và Tính lương nháp
    Note over PayrollClerk, DB: GIAI ĐOẠN 1: TÍNH TOÁN LƯƠNG NHÁP (DRAFT CALCULATION)
    PayrollClerk->>+API: POST /api/v1/payroll/periods/{periodId}/calculate<br/>[Cookie HttpOnly, Anti-CSRF]
    API->>+App: Handle(CalculatePayrollBatchCommand { PeriodId, CalculatedBy: UserA })
    App->>DB: Kiểm tra trạng thái kỳ lương: Status == 'Draft' AND IsLocked == false
    App->>DB: Đọc bảng công đã khóa đến ngày chốt (CutOffDate)
    App->>DB: Đọc mức lương cơ bản đang có hiệu lực tại ngày chốt từ SalaryHistory
    App->>DB: Đọc số ngày nghỉ phép hưởng lương (Paid Leave) từ LeaveRecords
    
    Note over App: Động cơ tính toán lương (Payroll Calculation Engine):<br/>- Tính tổng giờ công chuẩn & làm thêm (OT 1.5x / 2.0x)<br/>- Tính GrossPay, các khoản trích nộp bắt buộc, NetPay<br/>- Sinh mã kiểm tra Checksum (SHA-256) cho từng bản chụp
    
    App->>+DB: BEGIN TRANSACTION
    App->>DB: INSERT INTO PayrollBatches (PeriodId, BatchNumber, Status, CreatedBy)<br/>VALUES (periodId, 'BATCH-2026-10', 'Calculated', UserA)
    App->>DB: INSERT INTO PayrollCalculationSnapshots (BatchId, EmployeeId, BaseSalarySnapshot, GrossPay, NetPay, Checksum)<br/>[Tạo 500 bản ghi Snapshot bất biến trong 0.10s]
    App->>DB: INSERT INTO Payslips (SnapshotId, EmployeeId, NetSalary, Status)<br/>VALUES (..., 'Draft')
    App->>DB: COMMIT TRANSACTION
    DB-->>-App: Commit Thành công
    App-->>-API: Result.Success(BatchSummary)
    API-->>-PayrollClerk: 200 OK { batchId, totalEmployees: 500, totalGross, totalNet }

    %% Giai đoạn 2: Phê duyệt phân tách trách nhiệm (Segregation of Duties)
    Note over PayrollClerk, HRManager: GIAI ĐOẠN 2: PHÊ DUYỆT PHÂN TÁCH TRÁCH NHIỆM (TWO-PERSON RULE)
    PayrollClerk->>+API: POST /api/v1/payroll/batches/{batchId}/approve<br/>(User A cố tình tự duyệt bảng lương do mình tính)
    API->>+App: Handle(ApprovePayrollBatchCommand { BatchId, ApproverId: UserA })
    Note right of App: Kiểm tra nguyên tắc phân tách quyền lực:<br/>ApproverId == CreatedBy (User A == User A) -> TỪ CHỐI!
    App-->>-API: Result.Failure("Người lập bảng lương không được tự phê duyệt")
    API-->>-PayrollClerk: 403 Forbidden { code: "SELF_APPROVAL_FORBIDDEN" }

    HRManager->>+API: POST /api/v1/payroll/batches/{batchId}/approve<br/>(User B là Trưởng phòng Nhân sự duyệt)
    API->>+App: Handle(ApprovePayrollBatchCommand { BatchId, ApproverId: UserB })
    App->>DB: UPDATE PayrollBatches SET Status = 'Approved', ApprovedBy = UserB
    App-->>-API: Result.Success()
    API-->>-HRManager: 200 OK { message: "Phê duyệt bảng lương thành công" }

    %% Giai đoạn 3: Công bố & Niêm phong kỳ lương (Lock & Publish)
    Note over HRManager, DB: GIAI ĐOẠN 3: CÔNG BỐ VÀ NIÊM PHONG KỲ LƯƠNG (LOCK & PUBLISH)
    HRManager->>+API: POST /api/v1/payroll/periods/{periodId}/publish
    API->>+App: Handle(PublishPayrollPeriodCommand)
    App->>+DB: BEGIN TRANSACTION
    App->>DB: UPDATE PayrollPeriodRecords SET Status = 'Locked', IsLocked = true, LockedAtUtc = GETUTCDATE()<br/>WHERE Id = periodId
    App->>DB: UPDATE Payslips SET Status = 'Published' WHERE BatchId = batchId
    App->>DB: COMMIT TRANSACTION
    DB-->>-App: Commit Thành công
    App-->>-API: Result.Success()
    API-->>-HRManager: 200 OK { message: "Kỳ lương đã được khóa bất biến và công bố" }

    %% Giai đoạn 4: Kiểm chứng tính bất biến khi có thay đổi lương sau này
    Note over PayrollClerk, Employee: GIAI ĐOẠN 4: KIỂM CHỨNG TÍNH BẤT BIẾN KHI CÓ THAY ĐỔI LƯƠNG TRUY THU
    PayrollClerk->>+API: POST /api/v1/employees/{empId}/salary<br/>[Tăng lương nhân viên từ 20,000,000 VND lên 25,000,000 VND từ tháng sau]
    API->>+App: AppendSalaryHistoryCommand
    App->>DB: INSERT INTO SalaryHistories (EmployeeId, BaseSalary, EffectiveDate)<br/>VALUES (empId, 25000000, '2026-11-01')
    App-->>-API: Success
    API-->>-PayrollClerk: 200 OK

    Employee->>+API: GET /api/v1/payroll/me/payslips/{periodId}<br/>[Mobile App truy vấn phiếu lương tháng 10]
    API->>+App: GetEmployeePayslipQuery { EmployeeId: empId, PeriodId }
    App->>DB: SELECT * FROM Payslips p JOIN PayrollCalculationSnapshots s ON p.SnapshotId = s.Id<br/>WHERE p.EmployeeId = @empId AND s.BatchId = @batchId
    Note right of App: Dữ liệu đọc trực tiếp từ PayrollCalculationSnapshots:<br/>BaseSalarySnapshot = 20,000,000 VND (Không hề đổi!)<br/>Mức lương mới 25,000,000 VND chỉ có hiệu lực cho kỳ kế tiếp.
    App-->>-API: PayslipDetailsDTO (NetPay chuẩn)
    API-->>-Employee: 200 OK { slipNumber, netSalary: 18,500,000 VND, baseSalary: 20,000,000 VND }
```

---

## 2. Các Nguyên tắc Thiết kế Bất biến (Immutability Invariants)

1. **Snapshot-First Architecture**: Bảng `PayrollCalculationSnapshot` ghi lại trạng thái tĩnh của toàn bộ các yếu tố đầu vào (công chuẩn, giờ OT, ngày nghỉ hưởng lương, mức lương danh nghĩa) tại thời điểm chốt sổ.
2. **Two-Person Rule (Phân tách quyền)**: Người khởi tạo lệnh tính lương (`CalculatedBy`) bị cấm duyệt bảng lương (`ApprovedBy`). Điều kiện được kiểm soát bằng Domain Invariant:
   ```csharp
   if (batch.CreatedByUserId == currentUserId)
       throw new BusinessRuleValidationException("Người tính lương không được tự phê duyệt bảng lương.");
   ```
3. **Database Write Protection**: Khi `IsLocked == true`, Trigger trên bảng `PayrollPeriodRecords` và `PayrollCalculationSnapshots` sẽ hủy bỏ (RAISERROR) bất kỳ câu lệnh `UPDATE` hoặc `DELETE` nào tác động lên kỳ lương đã khóa.
4. **Không tính đè lương cũ (Append-Only Salary History)**: Biến động lương được ghi nối tiếp vào `SalaryHistories`. Tính lương của bất kỳ kỳ nào luôn lấy theo mốc thời gian:
   ```sql
   SELECT TOP 1 BaseSalary FROM SalaryHistories 
   WHERE EmployeeId = @empId AND EffectiveDate <= @cutOffDate 
   ORDER BY EffectiveDate DESC, CreatedAtUtc DESC;
   ```

---

## 3. Liên kết Tài liệu Liên quan

- [Sơ đồ ERD Thực thể Cốt lõi](file:///d:/dotnet%20project/docs/diagrams/erd-core-entities.md)
- [Báo cáo Thử nghiệm Tải T26 (500 nhân sự trong 0.10 giây)](file:///d:/dotnet%20project/docs/benchmarks/t26-performance-report.md)
- [Kịch bản Demo Bảo vệ](file:///d:/dotnet%20project/docs/demo/demo-scenarios.md)
