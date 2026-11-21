import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Drawer,
  Table,
  Tag,
  Typography,
} from "antd";
import { EyeOutlined, ReloadOutlined } from "@ant-design/icons";
import { displayDecimal } from "../shared/display";
import { payrollApi, type PayslipResponse } from "./api";
import { PayrollFeedback } from "./PayrollFeedback";

export function MyPayslips() {
  const [page, setPage] = useState(1);
  const [selectedPayslipId, setSelectedPayslipId] = useState<string | null>(null);

  // Query own published payslips list (Server strictly returns published payslips for the authenticated employee)
  const payslipsQuery = useQuery({
    queryKey: ["my-payslips", page],
    queryFn: ({ signal }) => payrollApi.myPayslips({ page, pageSize: 20 }, signal),
    retry: false,
  });

  // Query detail for the selected payslip using the OWN detail endpoint /api/v1/me/payslips/{id}
  const detailQuery = useQuery({
    queryKey: ["my-payslip-detail", selectedPayslipId],
    queryFn: ({ signal }) => payrollApi.myPayslipDetail(selectedPayslipId!, signal),
    enabled: !!selectedPayslipId,
    retry: false,
  });

  const detail = detailQuery.data;

  return (
    <Card className="my-payslips-card">
      <div className="payroll-section-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Phiếu lương của tôi
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Chỉ hiển thị các phiếu lương đã được công bố chính thức. Số liệu được lưu giữ bất biến theo bản chụp tính toán.
          </Typography.Text>
        </div>

        <Button icon={<ReloadOutlined />} onClick={() => void payslipsQuery.refetch()}>
          Tải lại
        </Button>
      </div>

      <PayrollFeedback error={payslipsQuery.error} />

      <div className="business-table" role="region" aria-label="Danh sách phiếu lương của tôi" tabIndex={0}>
        <Table<PayslipResponse>
          rowKey="id"
          dataSource={payslipsQuery.data?.items ?? []}
          loading={payslipsQuery.isLoading}
          pagination={{
            current: page,
            pageSize: 20,
            total: payslipsQuery.data?.totalCount,
            onChange: setPage,
            showSizeChanger: false,
          }}
          columns={[
            {
              title: "Mã phiếu",
              dataIndex: "id",
              render: (id: string) => <code>{id.slice(0, 8)}…</code>,
            },
            {
              title: "Số tiền thực nhận (Net)",
              dataIndex: "amount",
              render: (amount: string) => (
                <strong>{displayDecimal(amount)} VND</strong>
              ),
            },
            {
              title: "Trạng thái",
              dataIndex: "status",
              render: (status: string) => <Tag color="success">{status === "Published" ? "Đã công bố" : status}</Tag>,
            },
            {
              title: "Ngày công bố",
              dataIndex: "publishedAtUtc",
              render: (date: string | null) => (date ? new Date(date).toLocaleString("vi-VN") : "—"),
            },
            {
              title: "Thao tác",
              render: (_, record) => (
                <Button
                  size="small"
                  type="primary"
                  icon={<EyeOutlined />}
                  onClick={() => setSelectedPayslipId(record.id)}
                >
                  Xem chi tiết
                </Button>
              ),
            },
          ]}
        />
      </div>

      {/* Drawer: Immutable detail of the selected payslip */}
      <Drawer
        open={!!selectedPayslipId}
        title={detail ? `Chi tiết phiếu lương tháng ${detail.periodCode}` : "Chi tiết phiếu lương"}
        width={650}
        onClose={() => setSelectedPayslipId(null)}
      >
        <PayrollFeedback error={detailQuery.error} />

        {detailQuery.isLoading ? (
          <p>Đang tải chi tiết phiếu lương bất biến...</p>
        ) : detail ? (
          <div>
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
              message="Tính bất biến của phiếu lương"
              description="Phiếu lương lưu giữ số liệu bất biến theo thời điểm công bố. Mọi thay đổi về mức lương hay chính sách ở các tháng sau tuyệt đối không làm thay đổi các phiếu lương đã công bố trước đó."
            />

            <Descriptions
              size="small"
              bordered
              column={1}
              style={{ marginBottom: 16 }}
              items={[
                { key: "period", label: "Kỳ lương", children: detail.periodCode },
                { key: "run", label: "Bản chạy tính toán", children: `RunVersion ${detail.runVersion}` },
                {
                  key: "published",
                  label: "Thời điểm công bố",
                  children: detail.publishedAtUtc ? new Date(detail.publishedAtUtc).toLocaleString("vi-VN") : "—",
                },
                { key: "gross", label: "Tổng thu nhập (Gross)", children: `${displayDecimal(detail.calculation.gross)} ${detail.currency}` },
                { key: "std", label: "Lương công chuẩn", children: `${displayDecimal(detail.calculation.standardPay)} ${detail.currency}` },
                { key: "ot", label: "Tiền làm thêm giờ (OT)", children: `${displayDecimal(detail.calculation.overtimePay)} ${detail.currency}` },
                { key: "round", label: "Điều chỉnh làm tròn", children: `${displayDecimal(detail.calculation.roundingAdjustment)} ${detail.currency}` },
                { key: "net", label: "Lương thực nhận (Net)", children: <strong style={{ fontSize: 16, color: "#1890ff" }}>{displayDecimal(detail.calculation.net)} {detail.currency}</strong> },
              ]}
            />

            <Typography.Title level={5} style={{ marginTop: 16 }}>
              Chi tiết các khoản thu nhập & giảm trừ (Lines)
            </Typography.Title>
            <Table
              size="small"
              rowKey="code"
              dataSource={detail.calculation.lines}
              pagination={false}
              columns={[
                { title: "Mục lương", dataIndex: "code" },
                {
                  title: `Số tiền (${detail.currency})`,
                  dataIndex: "amount",
                  render: displayDecimal,
                },
              ]}
            />

            <Typography.Title level={5} style={{ marginTop: 24 }}>
              Dữ liệu ngày công được hưởng lương (Days)
            </Typography.Title>
            <Table
              size="small"
              rowKey="date"
              dataSource={detail.days}
              pagination={{ pageSize: 7, showSizeChanger: false }}
              columns={[
                { title: "Ngày", dataIndex: "date" },
                { title: "Phút có mặt", dataIndex: "attendanceMinutes", render: displayDecimal },
                {
                  title: "Hoàn tất ca",
                  dataIndex: "checkoutComplete",
                  render: (v: boolean) => (v ? <Tag color="green">Đủ giờ</Tag> : <Tag color="orange">Thiếu giờ ra</Tag>),
                },
                {
                  title: "Nghỉ phép hưởng lương",
                  dataIndex: "paidLeaveApproved",
                  render: (v: boolean) => (v ? <Tag color="blue">Có phép</Tag> : "—"),
                },
                { title: "Phút OT", dataIndex: "approvedOvertimeMinutes", render: displayDecimal },
              ]}
            />

            {detail.notice && (
              <p style={{ marginTop: 16, fontSize: 12, color: "#8c8c8c" }}>
                Ghi chú chính sách: {detail.notice}
              </p>
            )}
          </div>
        ) : null}
      </Drawer>
    </Card>
  );
}
