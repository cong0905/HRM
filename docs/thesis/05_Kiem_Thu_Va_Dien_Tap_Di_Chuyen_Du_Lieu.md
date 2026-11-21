# CHƯƠNG 5: KIỂM THỬ, ĐỐI SOÁT DI CHUYỂN DỮ LIỆU VÀ ĐO TẢI

## 5.1. Chiến lược Kiểm thử Đa tầng (Multi-Tier Testing Strategy)

Hệ thống áp dụng phương pháp kiểm thử nghiêm ngặt theo mô hình Kim tự tháp kiểm thử (Testing Pyramid), kết hợp giữa kiểm thử đơn vị, kiểm thử tích hợp trên cơ sở dữ liệu thật và kiểm thử luồng người dùng (E2E):

```
                     ┌──────────────────┐
                     │   E2E Browser /  │
                     │  Mobile Reviews  │
                     │  (Playwright /   │
                     │   Jest Mobile)   │
                     └────────┬─────────┘
                              │
                    ┌─────────▼──────────┐
                    │ Real SQL Server    │
                    │ Integration Tests  │
                    │ (SqlDatabaseFixture│
                    │  xUnit + EF Core)  │
                    └─────────┬──────────┘
                              │
                   ┌──────────▼──────────┐
                   │ Unit Tests (Math,   │
                   │ Logic, Invariants)  │
                   └─────────────────────┘
```

1. **Kiểm thử Đơn vị (Unit Tests)**: Kiểm tra các thuật toán tính toán độc lập không phụ thuộc DB: `AttendanceMathTests`, `LeaveMathTests`, `CalendarMathTests`, `PayrollCalculatorTests`, `AuditMaskerTests`.
2. **Kiểm thử Tích hợp Cơ sở Dữ liệu Thật (Real SQL Server Integration Tests)**:
   - Toàn bộ các bài kiểm thử liên quan đến giao dịch, phân quyền, khóa đồng thời và tính bất biến **tuyệt đối không sử dụng InMemory Database** (do InMemory không hỗ trợ RowVersion, Transaction Lock, Stored Procedures hay Check Constraints).
   - Sử dụng fixture `SqlDatabaseFixture`: tự động tạo một cơ sở dữ liệu vật lý riêng biệt với tên ngẫu nhiên `HRM_Next_Test_T05_{GUID}` trên SQL Server 2022 thật, chạy toàn bộ chuỗi migrations, thực thi bài test và xóa sạch sau khi hoàn tất.
3. **Kiểm thử Giao diện & Trình duyệt E2E**: Chạy tự động với Playwright mô phỏng thao tác của người dùng trên Web và Jest/React Native Testing Library trên Mobile.

---

## 5.2. Kết quả Kiểm thử Tích hợp Cốt lõi (Integration Test Results)

Hệ thống đã trải qua hơn 200 kịch bản kiểm thử tự động với tỷ lệ đạt 100% (PASS):

| Nhóm kiểm thử | Số lượng ca test | Kết quả | Hành vi then chốt được xác minh |
|---|:---:|:---:|---|
| **Identity & Auth (`FoundationSqlTests`)** | 20 test cases | **100% PASS** | Cấp Cookie/Bearer, xoay vòng Refresh Token nguyên tử, khóa tài khoản sau 5 lần sai mật khẩu, giới hạn IP rate limit, thu hồi phiên tức thời khi đổi pass. |
| **Nhân sự (`T08EmployeeLifecycleSqlTests`)** | 21 test cases | **100% PASS** | Sinh mã nhân viên `NV2026xxxx` duy nhất khi tạo đồng thời, kiểm tra trùng username/email, phân quyền HR Scope, offboarding nguyên tử. |
| **Chấm công (`T10AttendanceSqlTests`)** | 36 test cases | **100% PASS** | Đồng hồ server clock, chống gửi trùng bằng `Idempotency-Key`, kiểm toán before/after khi sửa công, duyệt OT, bất biến công - phép (`WorkLeaveInvariant`). |
| **Nghỉ phép (`T11LeaveSqlTests`)** | 48 test cases | **100% PASS** | Duyệt đồng thời không âm quỹ (`CK_LeaveBalances_NoOverdraft`), cấm tự duyệt đơn của mình (`ApproverId != EmployeeId`), sổ cái biến động phép. |
| **Tiền lương (`T13PayrollSqlTests`, `T14`, `T15`)** | 35 test cases | **100% PASS** | Tính lương phân đoạn giữa tháng, snapshot bất biến không thể sửa/xóa khi đã Locked, xuất CSV an toàn chống Formula Injection. |
| **Email Outbox (`EmailOutboxTests`)** | 8 test cases | **100% PASS** | Fake SMTP socket nhận thư thật, transient error tính backoff lũy thừa, permanent error dừng retry, token quá hạn bị hủy trước khi gửi, che giấu token trong log. |
| **Web Component & E2E Workflows** | 48 unit + 6 E2E | **100% PASS** | Hai trình duyệt cùng mở duyệt 1 đơn phép (1 thành công, 1 nhận 409 Conflict), injection lỗi DB tự rollback form, kỳ khóa chỉ xem. |
| **Mobile App Tests (`mobile/tests/`)** | 49 test cases | **100% PASS** | Check-in/out gửi `Idempotency-Key`, mất mạng báo lỗi rõ ràng không ghi cache giả, xóa sạch 100% cache RAM khi đăng xuất (`cacheIsolation`). |

---

## 5.3. Di chuyển Dữ liệu và Đối soát Toàn vẹn (T23 Data Migration)

Công cụ di chuyển dữ liệu chuyên dụng `ops/migration/` (`HrmMigration.exe`) đã thực thi diễn tập chuyển đổi dữ liệu từ cơ sở dữ liệu legacy sang cơ sở dữ liệu mới với các kết quả xuất sắc:

### 1. Bảng đối soát số lượng bản ghi (Record Count Reconciliation):
Toàn bộ dữ liệu lịch sử được chuyển đổi đầy đủ, không thất thoát bất kỳ bản ghi nào:

| Thực thể dữ liệu | Số lượng Legacy | Số lượng Hệ thống Mới | Chênh lệch (Variance) | Đánh giá |
|---|:---:|:---:|:---:|:---:|
| Phòng ban (`Departments`) | 3 | 3 | **0** | **Khớp 100%** |
| Chức vụ (`Positions`) | 3 | 3 | **0** | **Khớp 100%** |
| Hồ sơ Nhân viên (`EmployeeProfiles`) | 5 | 5 | **0** | **Khớp 100%** |
| Tài khoản Người dùng (`AspNetUsers`) | 5 | 5 | **0** | **Khớp 100%** |
| Chấm công tổng hợp (`AttendanceSummaries`) | 6 | 6 | **0** | **Khớp 100%** |
| Đơn nghỉ phép (`LeaveRecords`) | 3 | 3 | **0** | **Khớp 100%** |
| Quỹ phép năm (`LeaveBalances`) | 5 | 5 | **0** | **Khớp 100%** |
| Bảng tính lương (`CalculationSnapshots`) | 4 | 4 | **0** | **Khớp 100%** |
| Phiếu lương nhân viên (`Payslips`) | 4 | 4 | **0** | **Khớp 100%** |

### 2. Đối soát Tài chính Tuyệt đối (Financial Zero-Drift):
So sánh tổng số tiền thực nhận của kỳ lương lịch sử 2026-04 giữa hệ thống cũ và hệ thống mới:
- **Tổng thực nhận hệ thống cũ (`LuongThucNhan`)**: `108,795,454.55 VND`
- **Tổng thực nhận hệ thống mới (`Net`)**: `108,795,454.55 VND`
- **Chênh lệch tài chính**: **`0.00 VND`** (Dung sai sai lệch tuyệt đối bằng 0).

### 3. Tính An toàn Tài khoản và Tính Tất định (Idempotency):
- **An toàn tài khoản**: 100% tài khoản di chuyển có `PasswordHash = NULL`, `IsActive = false`, sinh 5 bản ghi `ActivationOutboxMessages` chờ kích hoạt an toàn. Tuyệt đối không kế thừa mật khẩu MD5/BCrypt cũ và không gán mật khẩu mặc định.
- **Tính tất định (Idempotent UUIDv5 RFC 4122)**: Mã khóa chính mới được sinh từ Namespace UUID kết hợp khóa tự nhiên của bản ghi cũ. Khi chạy lại lệnh migrate nhiều lần hoặc bị ngắt quãng giữa chừng, công cụ tự động nhận diện và cập nhật mà không sinh bản ghi trùng lặp.

---

## 5.4. Kết quả Đo tải Năng lực và Diễn tập Phục hồi Thảm họa (T26 Benchmark & DR)

### 1. Kết quả Đo tải với 30 Virtual Users đồng thời (`ops/load-tests/`):
Môi trường thử nghiệm thực tế: Intel Core i5-12450H (12 luồng), 16GB RAM, SQL Server 2022, tập dữ liệu 500 nhân sự, 12 kỳ lương lịch sử, 5,000 sự kiện công:

| Kịch bản kiểm thử tải | Số requests | Tốc độ xử lý (RPS) | Độ trễ p50 | Độ trễ p95 | Tỷ lệ lỗi (Error %) | SLA p95 < 1s |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Auth Login & Session Check** | 300 | **775.8 req/s** | 3.8 ms | **226.3 ms** | **0.0%** | **ĐẠT** |
| **Employee Search & Paging** | 600 | **376.4 req/s** | 33.2 ms | **129.8 ms** | **0.0%** | **ĐẠT** |
| **Personal Attendance Dashboard** | 600 | **1,131.5 req/s** | 18.1 ms | **68.8 ms** | **0.0%** | **ĐẠT** |
| **Leave Balances & Ledger Query** | 600 | **1,069.1 req/s** | 24.6 ms | **43.0 ms** | **0.0%** | **ĐẠT** |

- **Tính toán bảng lương hàng loạt 500 nhân viên**: Hoàn thành trong **0.10 giây** (~5,000 phiếu lương/giây) nhờ xử lý nạp dữ liệu một lần (Batch In-Memory Mapping) và lưu trữ Bulk Transaction, loại bỏ hoàn toàn vấn đề N+1 Query.
- **Kiểm tra DMV Missing Index**: Không có cảnh báo chỉ mục thiếu nghiêm trọng nào trên toàn bộ cơ sở dữ liệu.

### 2. Diễn tập Phục hồi Thảm họa (Disaster Recovery Rehearsal):
- Kịch bản: Tự động sao lưu bằng `backup.py` với thuật toán nén `COMPRESSION` và kiểm tra `CHECKSUM`. Thực hiện khôi phục sang một database hoàn toàn mới bằng `restore.py`.
- **RTO thực tế (Thời gian phục hồi dịch vụ)**: **0.96 giây** (Vượt xa cam kết mục tiêu RTO < 4 giờ).
- **RPO thực tế (Mức độ mất mát dữ liệu tối đa)**: **32.4 giây / 0.009 giờ** (Vượt xa cam kết mục tiêu RPO < 24 giờ).
- **Kiểm tra toàn vẹn vật lý**: Lệnh `DBCC CHECKDB` hoàn thành với **0 allocation errors và 0 consistency errors**. Đối soát số dòng khớp 100% trên tất cả các bảng.
