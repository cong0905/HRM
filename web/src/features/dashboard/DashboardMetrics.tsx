import { useQuery } from "@tanstack/react-query";
import { Card, Col, Row, Statistic, Tag } from "antd";
import {
  CalendarOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  DollarCircleOutlined,
} from "@ant-design/icons";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { ApiClientError } from "../../api/types";
import { payrollApi } from "../payroll/api";
import { attendanceApi } from "../attendance/api";
import { Feedback } from "../shared/Feedback";
import { leaveApi } from "../leave/api";

export function DashboardMetrics() {
  const { hasPermission, hasAnyPermission } = useAuth();

  const canReadPayrollAll = hasPermission("payroll.read_all");
  const canReadPayrollOwn = hasPermission("payroll.read_own");
  const canReadAttendanceOwn = hasPermission("attendance.read_own");
  const canApproveLeaves = hasAnyPermission(["leaves.approve", "leaves.read_all"]);

  // 1. Payroll admin query: real totalCount of payroll periods
  const payrollPeriodsQuery = useQuery({
    queryKey: ["dashboard-metric-payroll-periods"],
    queryFn: ({ signal }) => payrollApi.listPeriods({ page: 1, pageSize: 1 }, signal),
    enabled: canReadPayrollAll,
    retry: false,
    staleTime: 60 * 1000,
  });

  // 2. Employee query: real totalCount of own published payslips
  const myPayslipsQuery = useQuery({
    queryKey: ["dashboard-metric-my-payslips"],
    queryFn: ({ signal }) => payrollApi.myPayslips({ page: 1, pageSize: 1 }, signal),
    enabled: canReadPayrollOwn,
    retry: false,
    staleTime: 60 * 1000,
  });

  // 3. Employee query: today's attendance status
  const todayAttendanceQuery = useQuery({
    queryKey: ["dashboard-metric-today-attendance"],
    queryFn: async ({ signal }) => {
      try {
        return await attendanceApi.today(signal);
      } catch (err) {
        if (err instanceof ApiClientError && err.status === 404) {
          return null;
        }
        throw err;
      }
    },
    enabled: canReadAttendanceOwn,
    retry: false,
    staleTime: 30 * 1000,
  });

  // 4. HR query: real totalCount of pending leave requests in scope
  const pendingLeavesQuery = useQuery({
    queryKey: ["dashboard-metric-pending-leaves"],
    queryFn: ({ signal }) => leaveApi.team({ page: 1, pageSize: 1, status: "Pending" }, signal),
    enabled: canApproveLeaves,
    retry: false,
    staleTime: 60 * 1000,
  });

  const cards = [];

  // Card: Payroll Periods
  if (canReadPayrollAll) {
    cards.push(
      <Col xs={24} sm={12} md={6} key="payroll-periods">
        <Link to="/payroll" style={{ textDecoration: "none" }}>
          <Card hoverable className="metric-card">
            <Statistic
              title="Tổng số kỳ lương"
              value={payrollPeriodsQuery.isSuccess ? payrollPeriodsQuery.data?.totalCount : "—"}
              loading={payrollPeriodsQuery.isLoading}
              prefix={<CalendarOutlined style={{ color: "#3157D5", marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: "#626B77" }}>
              Quản lý và tính toán kỳ lương
            </div>
          </Card>
        </Link>
      </Col>
    );
  }

  // Card: Published Payslips
  if (canReadPayrollOwn) {
    cards.push(
      <Col xs={24} sm={12} md={6} key="my-payslips">
        <Link to="/payroll" style={{ textDecoration: "none" }}>
          <Card hoverable className="metric-card">
            <Statistic
              title="Phiếu lương đã công bố"
              value={myPayslipsQuery.isSuccess ? myPayslipsQuery.data?.totalCount : "—"}
              loading={myPayslipsQuery.isLoading}
              prefix={<DollarCircleOutlined style={{ color: "#52c41a", marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: "#626B77" }}>
              Phiếu lương chính thức của bạn
            </div>
          </Card>
        </Link>
      </Col>
    );
  }

  // Card: Today Attendance
  if (canReadAttendanceOwn) {
    let attendanceText = "Chưa vào ca";
    let statusColor = "default";
    if (todayAttendanceQuery.data) {
      if (todayAttendanceQuery.data.checkOutUtc) {
        attendanceText = "Đã hoàn thành ca";
        statusColor = "success";
      } else {
        attendanceText = "Đang trong ca";
        statusColor = "processing";
      }
    }

    cards.push(
      <Col xs={24} sm={12} md={6} key="today-attendance">
        <Link to="/attendance" style={{ textDecoration: "none" }}>
          <Card hoverable className="metric-card">
            <div style={{ fontSize: 14, color: "#626B77", marginBottom: 4 }}>Chấm công hôm nay</div>
            <div style={{ fontSize: 20, fontWeight: 600, color: "#20252B", display: "flex", alignItems: "center" }}>
              <ClockCircleOutlined style={{ color: "#1890ff", marginRight: 8 }} />
              <Tag color={statusColor} style={{ fontSize: 14, padding: "2px 8px" }}>
                {todayAttendanceQuery.isLoading ? "Đang tải…" : todayAttendanceQuery.isError ? "Không tải được dữ liệu" : attendanceText}
              </Tag>
            </div>
            <div style={{ marginTop: 8, fontSize: 12, color: "#626B77" }}>
              Theo dõi giờ vào / ra ca
            </div>
          </Card>
        </Link>
      </Col>
    );
  }

  // Card: Pending Leave Requests
  if (canApproveLeaves) {
    cards.push(
      <Col xs={24} sm={12} md={6} key="pending-leaves">
        <Link to="/leave" style={{ textDecoration: "none" }}>
          <Card hoverable className="metric-card">
            <Statistic
              title="Đơn nghỉ phép chờ duyệt"
              value={pendingLeavesQuery.isSuccess ? pendingLeavesQuery.data?.totalCount : "—"}
              loading={pendingLeavesQuery.isLoading}
              prefix={<CheckCircleOutlined style={{ color: "#fa8c16", marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: "#626B77" }}>
              Đơn nghỉ phép trong phạm vi quản lý
            </div>
          </Card>
        </Link>
      </Col>
    );
  }

  if (cards.length === 0) return null;

  return (
    <section className="dashboard-metrics-section" style={{ marginBottom: 24 }} aria-label="Số liệu nghiệp vụ">
      <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 12, color: "#20252B" }}>Số liệu tổng quan</h2>
      <Feedback error={payrollPeriodsQuery.error || myPayslipsQuery.error || todayAttendanceQuery.error || pendingLeavesQuery.error} retry={() => {
        if (canReadPayrollAll) void payrollPeriodsQuery.refetch();
        if (canReadPayrollOwn) void myPayslipsQuery.refetch();
        if (canReadAttendanceOwn) void todayAttendanceQuery.refetch();
        if (canApproveLeaves) void pendingLeavesQuery.refetch();
      }} />
      <Row gutter={[16, 16]}>{cards}</Row>
    </section>
  );
}
