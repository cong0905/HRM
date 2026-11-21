# Báo Cáo Đo Tải & Năng Lực Hệ Thống (Load Testing & Capacity Benchmark) — T26

- **Thời gian đo**: `2026-11-19 15:09:54 UTC`
- **Môi trường**: `MSI` (Microsoft Windows 10.0.26200, 12 Cores)
- **Cơ sở dữ liệu**: `Microsoft SQL Server 2022 (RTM) - 16.0.1000.6 (X64) `
- **Quy mô dữ liệu**: `500` nhân viên, `12` kỳ lương lịch sử, `5000` bản ghi công
- **Mức tải kiểm thử**: **30 Concurrent Virtual Users (Workers)**
- **Đánh giá tổng thể**: **ĐẠT TOÀN DIỆN (SLA p95 < 1s, Error < 1%)**

## 1. Kết Quả Đo Tải Các Kịch Bản Trọng Yếu (30 Concurrency)

| Kịch bản kiểm thử | Tổng Request | Throughput (RPS) | p50 (ms) | p95 (ms) | p99 (ms) | Tỷ lệ lỗi (%) | Mục tiêu p95 < 1s | Mục tiêu Err < 1% | Đánh giá |
|---|---|---|---|---|---|---|---|---|---|
| `Auth_Login_And_Verify` | 300 | 775.8 | 3.8 | 226.3 | 253.8 | 0.0% | Đạt | Đạt | **ĐẠT** |
| `Employees_Search_And_Paging` | 600 | 376.4 | 33.2 | 129.8 | 1,305.0 | 0.0% | Đạt | Đạt | **ĐẠT** |
| `Personal_Attendance_Dashboard` | 600 | 1,131.5 | 18.1 | 68.8 | 197.4 | 0.0% | Đạt | Đạt | **ĐẠT** |
| `Leave_Balances_And_Ledger` | 600 | 1,069.1 | 24.6 | 43.0 | 79.7 | 0.0% | Đạt | Đạt | **ĐẠT** |

## 2. Năng Lực Tính Toán Kỳ Lương Hàng Loạt (Payroll Batch Calculation)

- **Số lượng nhân viên tính toán đồng thời**: `500` hồ sơ
- **Thời gian hoàn tất toàn bộ kỳ lương**: **`0.10` giây**
- **Tốc độ xử lý bình quân**: `5,097.1` phiếu lương/giây
- **Đánh giá**: Vượt xa yêu cầu nghiệp vụ thực tế (mục tiêu tính kỳ < 60 giây).

## 3. Khảo Sát Chỉ Mục & Phân Tích Truy Vấn SQL (Query & Index Insights)

- No critical missing indexes detected; all primary foreign keys and query columns are properly indexed.

## 4. Kết Luận Nghiệm Thu Kỹ Thuật

- [x] Mục tiêu p95 < 1s và Error Rate < 1% đạt 100% trên tất cả các kịch bản với 30 concurrent workers.
- [x] Thời gian tính lương hàng loạt cho 500 nhân sự hoàn tất dưới 5 giây, bảo đảm tính toán chính xác và toàn vẹn dữ liệu.
- [x] Không nới lỏng phạm vi phân quyền hay bỏ qua bảo mật để đạt throughput.
