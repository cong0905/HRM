import { useState } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Divider,
  Drawer,
  Form,
  Input,
  InputNumber,
  Pagination,
  Modal,
  Select,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
} from "antd";
import {
  CalculatorOutlined,
  CheckCircleOutlined,
  DownloadOutlined,
  EyeOutlined,
  LockOutlined,
  PlusOutlined,
  ReloadOutlined,
  SendOutlined,
} from "@ant-design/icons";
import { useAuth } from "../../context/AuthContext";
import { useCommand } from "../shared/commands";
import { useEmployeeLabels } from "../shared/EmployeeLabels";
import { displayDecimal } from "../shared/display";
import { payrollApi, type PayrollWorkflowState, type PayrollSnapshotResponse } from "./api";
import { PayrollFeedback } from "./PayrollFeedback";

const blockingLabels: Record<string, string> = {
  "payroll.incomplete_attendance": "Có ngày công thiếu giờ ra; cần hoàn tất hoặc điều chỉnh giờ công rồi tính lại.",
  "payroll.negative_net": "Lương thực nhận âm; cần kiểm tra các khoản giảm trừ rồi tính lại.",
};

const statusColors: Record<string, string> = {
  Draft: "default",
  Calculated: "processing",
  Approved: "warning",
  Published: "success",
  Locked: "magenta",
};

const statusLabels: Record<string, string> = {
  Draft: "Bản nháp",
  Calculated: "Đã tính nháp",
  Approved: "Đã phê duyệt",
  Published: "Đã công bố",
  Locked: "Đã khóa vĩnh viễn",
};

const stepIndexMap: Record<string, number> = {
  Draft: 0,
  Calculated: 1,
  Approved: 2,
  Published: 3,
  Locked: 4,
};

export function PayrollWorkflowWizard({ onNotice }: { onNotice: (msg: string) => void }) {
  const { user, hasPermission } = useAuth();

  // Periods list state
  const [periodPage, setPeriodPage] = useState(1);
  const [yearFilter, setYearFilter] = useState<number | undefined>();
  const [statusFilter, setStatusFilter] = useState<string | undefined>();
  const [selectedPeriodId, setSelectedPeriodId] = useState<string>();

  // Compare run state
  const [compareVersion, setCompareVersion] = useState<number | undefined>();

  // Modals state
  const [createPeriodOpen, setCreatePeriodOpen] = useState(false);
  const [actionModal, setActionModal] = useState<{
    open: boolean;
    operation: "calculate" | "approve" | "publish" | "lock";
    title: string;
    expected?: PayrollWorkflowState;
  }>({ open: false, operation: "calculate", title: "" });

  const [activeSnapshotDrawer, setActiveSnapshotDrawer] = useState<PayrollSnapshotResponse | null>(null);

  // Forms
  const [createForm] = Form.useForm<{ year: number; month: number; reason: string }>();
  const [workflowForm] = Form.useForm<{ reason: string }>();

  // Query periods list
  const periodsQuery = useQuery({
    queryKey: ["payroll-periods", periodPage, yearFilter, statusFilter],
    queryFn: ({ signal }) =>
      payrollApi.listPeriods({ page: periodPage, pageSize: 20, year: yearFilter, status: statusFilter }, signal),
    retry: false,
  });

  // Default select first period if none selected
  const periodsList = periodsQuery.data?.items ?? [];
  const effectivePeriodId = selectedPeriodId ?? (periodsList.length > 0 ? periodsList[0]!.id : undefined);

  // Query selected period state
  const stateQuery = useQuery({
    queryKey: ["payroll-period-state", effectivePeriodId],
    queryFn: ({ signal }) => payrollApi.getPeriodState(effectivePeriodId!, signal),
    enabled: !!effectivePeriodId,
    retry: false,
  });

  const periodState = stateQuery.data;

  // Query snapshot for the selected period
  const snapshotQuery = useQuery({
    queryKey: ["payroll-snapshot", effectivePeriodId, periodState?.runVersion],
    queryFn: ({ signal }) => payrollApi.getSnapshot(effectivePeriodId!, undefined, signal),
    enabled: !!effectivePeriodId && (periodState?.runVersion ?? 0) > 0,
    retry: false,
  });

  // Query previous snapshot for run comparison
  const compareSnapshotQuery = useQuery({
    queryKey: ["payroll-snapshot-compare", effectivePeriodId, compareVersion],
    queryFn: ({ signal }) => payrollApi.getSnapshot(effectivePeriodId!, compareVersion, signal),
    enabled: !!effectivePeriodId && !!compareVersion && compareVersion > 0 && compareVersion < (periodState?.runVersion ?? 0),
    retry: false,
  });

  // Query items for the selected period
  const [itemsPage, setItemsPage] = useState(1);
  const itemsQuery = useQuery({
    queryKey: ["payroll-period-items", effectivePeriodId, itemsPage],
    queryFn: ({ signal }) => payrollApi.getItems(effectivePeriodId!, itemsPage, 20, signal),
    enabled: !!effectivePeriodId && (periodState?.status === "Published" || periodState?.status === "Locked"),
    retry: false,
  });

  // Query reconciliation for the selected period
  const reconcileQuery = useQuery({
    queryKey: ["payroll-reconcile", effectivePeriodId, periodState?.runVersion],
    queryFn: ({ signal }) => payrollApi.reconcile(effectivePeriodId!, signal),
    enabled: !!effectivePeriodId && (periodState?.status === "Published" || periodState?.status === "Locked"),
    retry: false,
  });

  // Employee labels for snapshot table
  const [snapshotPage, setSnapshotPage] = useState(1);
  const currentSnapshotPage = Math.min(snapshotPage, Math.max(1, Math.ceil((snapshotQuery.data?.snapshots.length ?? 0) / 10)));
  const snapshotEmployees = [
    ...(snapshotQuery.data?.snapshots.slice((currentSnapshotPage - 1) * 10, currentSnapshotPage * 10).map(s => s.employeeId) ?? []),
    ...(itemsQuery.data?.items.map(s => s.employeeId) ?? []),
  ];
  const employeeNames = useEmployeeLabels(snapshotEmployees);

  // Command: Create new period
  const createPeriodCommand = useCommand(
    (values: { year: number; month: number; reason: string }, key) =>
      payrollApi.createPeriod({ year: values.year, month: values.month, reason: values.reason }, key),
    ["payroll-periods"],
    (result) => {
      onNotice(`Đã tạo kỳ lương ${result.periodCode} ở trạng thái Bản nháp.`);
      setCreatePeriodOpen(false);
      createForm.resetFields();
      setSelectedPeriodId(result.id);
      setCompareVersion(undefined);
      setItemsPage(1);
      setSnapshotPage(1);
      setActiveSnapshotDrawer(null);
    },
    "payroll:period:create"
  );

  // Command: Execute workflow action (calculate, approve, publish, lock)
  const workflowCommand = useCommand(
    (values: { reason: string }, key) => {
      const expected = actionModal.expected;
      if (!expected) throw new Error("Chưa chọn kỳ lương.");
      return payrollApi.executeWorkflow(
        expected.id,
        actionModal.operation,
        {
          reason: values.reason,
          rowVersion: expected.rowVersion,
          runVersion: expected.runVersion,
        },
        key
      );
    },
    ["payroll-period-state", "payroll-periods", "payroll-snapshot", "payroll-period-items", "payroll-reconcile"],
    () => {
      const messages: Record<string, string> = {
        calculate: "Đã hoàn thành tính toán nháp cho kỳ lương trên máy chủ.",
        approve: "Kỳ lương đã được phê duyệt thành công.",
        publish: "Bảng lương đã được công bố chính thức cho nhân viên.",
        lock: "Kỳ lương đã được khóa vĩnh viễn.",
      };
      onNotice(messages[actionModal.operation] || "Thao tác thành công.");
      setActionModal((prev) => ({ ...prev, open: false }));
      workflowForm.resetFields();
    },
    `${actionModal.operation}:${actionModal.expected?.id}:${actionModal.expected?.rowVersion}`
  );

  // Export CSV mutation
  const exportMutation = useMutation({
    mutationFn: async () => {
      if (!effectivePeriodId) return;
      return await payrollApi.exportCsv(effectivePeriodId);
    },
    onSuccess: (res) => {
      if (!res) return;
      const { blob, fileName } = res;
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = objectUrl;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(objectUrl);
      onNotice(`Đã tải xuống file CSV: ${fileName}`);
    },
  });

  // Separation of duties checks
  const isCalculator = Boolean(user?.id && periodState?.calculatedBy && user.id === periodState.calculatedBy);
  const currentStep = periodState ? stepIndexMap[periodState.status] ?? 0 : 0;

  // Aggregate stats from snapshot outputs
  const snapshots = snapshotQuery.data?.snapshots ?? [];
  const anyBlocking = snapshots.some((s) => s.output.blockingReasons.length > 0);
  const allBlockingReasons = Array.from(new Set(snapshots.flatMap((s) => s.output.blockingReasons)));

  return (
    <div className="payroll-wizard-container">
      {/* Period Selection & Filter Bar */}
      <div className="payroll-control-bar">
        <Space wrap style={{ marginBottom: 16 }}>
          <span>Kỳ lương:</span>
          <Select
            aria-label="Kỳ lương"
            style={{ width: 180 }}
            placeholder="Chọn kỳ lương"
            value={effectivePeriodId}
            onChange={(val) => {
              setSelectedPeriodId(val);
              setCompareVersion(undefined);
              setItemsPage(1);
              setSnapshotPage(1);
              setActiveSnapshotDrawer(null);
            }}
            loading={periodsQuery.isLoading}
            options={periodsList.map((p) => ({
              value: p.id,
              label: `${p.periodCode} (${statusLabels[p.status] || p.status})`,
            }))}
          />

          <InputNumber aria-label="Lọc năm" placeholder="Lọc năm" min={2000} max={2100} value={yearFilter} onChange={value => { setPeriodPage(1); setYearFilter(value ?? undefined); }} />
          <Select
            aria-label="Lọc trạng thái"
            allowClear
            style={{ width: 140 }}
            placeholder="Lọc trạng thái"
            value={statusFilter}
            onChange={(val) => {
              setPeriodPage(1);
              setStatusFilter(val);
            }}
            options={[
              { value: "Draft", label: "Bản nháp" },
              { value: "Calculated", label: "Đã tính nháp" },
              { value: "Approved", label: "Đã phê duyệt" },
              { value: "Published", label: "Đã công bố" },
              { value: "Locked", label: "Đã khóa" },
            ]}
          />

          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              void periodsQuery.refetch();
              void stateQuery.refetch();
              void snapshotQuery.refetch();
              void reconcileQuery.refetch();
            }}
          >
            Tải lại
          </Button>

          {hasPermission("payroll.calculate") && (
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreatePeriodOpen(true)}>
              Tạo kỳ lương mới
            </Button>
          )}
        </Space>
      </div>

      <Pagination current={periodPage} pageSize={20} total={periodsQuery.data?.totalCount ?? 0} onChange={setPeriodPage} showSizeChanger={false} hideOnSinglePage />
      <PayrollFeedback error={periodsQuery.error || stateQuery.error} />
      <PayrollFeedback error={exportMutation.error} />
      <PayrollFeedback error={compareSnapshotQuery.error} retry={() => void compareSnapshotQuery.refetch()} />

      {periodState ? (
        <Card className="payroll-period-card" style={{ marginBottom: 24 }}>
          {/* Header & Steps Indicator */}
          <div className="payroll-section-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
            <div>
              <Typography.Title level={4} style={{ margin: 0 }}>
                Kỳ lương tháng {periodState.periodCode}{" "}
                <Tag color={statusColors[periodState.status]}>
                  {statusLabels[periodState.status] || periodState.status}
                </Tag>
              </Typography.Title>
              <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                Bản chạy (RunVersion): <strong>{periodState.runVersion}</strong> · RowVersion:{" "}
                <code>{periodState.rowVersion}</code>
              </Typography.Text>
            </div>

            {/* Workflow Action Buttons */}
            <Space wrap>
              {hasPermission("payroll.calculate") &&
                (periodState.status === "Draft" ||
                  periodState.status === "Calculated" ||
                  periodState.status === "Approved") && (
                  <Button
                    type="primary"
                    icon={<CalculatorOutlined />}
                    onClick={() =>
                      setActionModal({
                        open: true,
                        expected: periodState,
                        operation: "calculate",
                        title: "Tính toán nháp kỳ lương",
                      })
                    }
                  >
                    Tính toán nháp
                  </Button>
                )}

              {hasPermission("payroll.approve") && periodState.status === "Calculated" && (
                <Button
                  style={{ backgroundColor: isCalculator ? undefined : "#faad14", color: "#fff" }}
                  icon={<CheckCircleOutlined />}
                  disabled={isCalculator || anyBlocking || !snapshotQuery.isSuccess}
                  onClick={() =>
                    setActionModal({
                      open: true,
                      expected: periodState,
                      operation: "approve",
                      title: "Phê duyệt kỳ lương",
                    })
                  }
                >
                  Phê duyệt
                </Button>
              )}

              {hasPermission("payroll.publish") && periodState.status === "Approved" && (
                <Button
                  type="primary"
                  style={{ backgroundColor: isCalculator ? undefined : "#52c41a", borderColor: "#52c41a" }}
                  icon={<SendOutlined />}
                  disabled={isCalculator || anyBlocking || !snapshotQuery.isSuccess}
                  onClick={() =>
                    setActionModal({
                      open: true,
                      expected: periodState,
                      operation: "publish",
                      title: "Công bố bảng lương cho nhân viên",
                    })
                  }
                >
                  Công bố
                </Button>
              )}

              {hasPermission("payroll.publish") && periodState.status === "Published" && (
                <Button
                  danger
                  icon={<LockOutlined />}
                  onClick={() =>
                    setActionModal({
                      open: true,
                      expected: periodState,
                      operation: "lock",
                      title: "Khóa vĩnh viễn kỳ lương",
                    })
                  }
                >
                  Khóa kỳ lương
                </Button>
              )}

              {hasPermission("payroll.export") &&
                (periodState.status === "Approved" ||
                  periodState.status === "Published" ||
                  periodState.status === "Locked") && (
                  <Button
                    icon={<DownloadOutlined />}
                    loading={exportMutation.isPending}
                    onClick={() => exportMutation.mutate()}
                  >
                    Xuất CSV
                  </Button>
                )}
            </Space>
          </div>

          {/* Anti-self-approval notice if applicable */}
          {isCalculator && (periodState.status === "Calculated" || periodState.status === "Approved") && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
              message="Quy tắc phân tách quyền hạn (Separation of duties)"
              description="Bạn là người thực hiện tính toán nháp kỳ lương này. Hệ thống cấm tự phê duyệt hoặc công bố kỳ lương do chính mình tính. Cần tài khoản kế toán khác để thẩm định và phê duyệt."
            />
          )}

          {/* Lifecycle Steps */}
          <Steps
            current={currentStep}
            size="small"
            style={{ marginBottom: 24, marginTop: 12 }}
            items={[
              { title: "Khởi tạo", description: "Bản nháp" },
              { title: "Tính toán nháp", description: `Run ${periodState.runVersion}` },
              { title: "Phê duyệt", description: periodState.approvedBy ? "Đã duyệt" : "Chờ duyệt" },
              { title: "Công bố", description: periodState.publishedBy ? "Đã công bố" : "Chưa công bố" },
              { title: "Khóa", description: periodState.lockedAtUtc ? "Đã khóa" : "Chưa khóa" },
            ]}
          />

          <Divider style={{ margin: "16px 0" }} />

          {/* Snapshot and Calculations Section */}
          <section className="payroll-snapshot-section">
            <div className="payroll-section-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
              <Typography.Title level={5} style={{ margin: 0 }}>
                Dữ liệu bản chụp tính toán (Snapshot)
              </Typography.Title>

              {periodState.runVersion > 1 && (
                <Space>
                  <span>Đối chiếu với bản chạy trước:</span>
                  <Select
                    aria-label="Đối chiếu bản chạy"
                    style={{ width: 140 }}
                    allowClear
                    placeholder="Chọn bản chạy"
                    value={compareVersion}
                    onChange={setCompareVersion}
                    options={Array.from({ length: periodState.runVersion - 1 }, (_, i) => ({
                      value: i + 1,
                      label: `Bản chạy ${i + 1}`,
                    }))}
                  />
                </Space>
              )}
            </div>

            {periodState.runVersion === 0 ? (
              <Alert
                type="info"
                showIcon
                message="Chưa kiểm dữ liệu / Chưa có bản chụp tính toán (Snapshot)"
                description="Bảng lương chưa được chạy tính toán nháp trên máy chủ. Bấm nút 'Tính toán nháp' phía trên để máy chủ kiểm tra dữ liệu công, quỹ phép và mốc lương."
              />
            ) : snapshotQuery.isLoading ? (
              <p>Đang tải bản chụp tính toán từ máy chủ...</p>
            ) : snapshotQuery.isError ? (
              <PayrollFeedback error={snapshotQuery.error} retry={() => void snapshotQuery.refetch()} />
            ) : snapshotQuery.data ? (
              <>
                {/* Meta details */}
                <Descriptions
                  size="small"
                  bordered
                  column={{ xs: 1, sm: 2, md: 3 }}
                  style={{ marginBottom: 16 }}
                  items={[
                    { key: "hash", label: "Mã băm dữ liệu (InputHash)", children: <code>{snapshotQuery.data.inputHash.slice(0, 16)}…</code> },
                    { key: "captured", label: "Thời điểm ghi nhận", children: new Date(snapshotQuery.data.capturedAtUtc).toLocaleString("vi-VN") },
                    { key: "count", label: "Số nhân viên trong kỳ", children: snapshots.length },
                  ]}
                />

                {/* Blocking reasons alert */}
                {anyBlocking && (
                  <Alert
                    type="error"
                    showIcon
                    style={{ marginBottom: 16 }}
                    message="Bảng lương có điều kiện chặn (Không thể công bố)"
                    description={
                      <ul style={{ paddingLeft: 20, margin: 0 }}>
                        {allBlockingReasons.map((reason, idx) => (
                          <li key={idx}>{blockingLabels[reason] || reason}</li>
                        ))}
                      </ul>
                    }
                  />
                )}

                {/* Run Comparison Banner if active */}
                {compareVersion && compareSnapshotQuery.data && (
                  <Alert
                    type="info"
                    showIcon
                    style={{ marginBottom: 16 }}
                    message={`Đang hiển thị đối chiếu cạnh nhau: Bản chạy ${periodState.runVersion} (Hiện tại) vs Bản chạy ${compareVersion} (Cũ)`}
                    description="Các số liệu được lấy trực tiếp từ bản chụp bất biến của máy chủ; không tự tính lại công thức ở trình duyệt."
                  />
                )}

                {/* Employee calculation table */}
                <div className="business-table" role="region" aria-label="Danh sách tính toán nhân viên" tabIndex={0}>
                  <Table<PayrollSnapshotResponse>
                    rowKey="id"
                    dataSource={snapshots}
                    pagination={{ current: currentSnapshotPage, onChange: setSnapshotPage, pageSize: 10, showSizeChanger: false }}
                    scroll={{ x: 950 }}
                    columns={[
                      {
                        title: "Nhân viên",
                        render: (_, record) =>
                          employeeNames.labels.get(record.employeeId) || record.employeeId,
                      },
                      {
                        title: "Công chuẩn (VND)",
                        render: (_, record) => {
                          const currentVal = displayDecimal(record.output.standardPay);
                          if (!compareVersion || !compareSnapshotQuery.data) return currentVal;
                          const prev = compareSnapshotQuery.data.snapshots.find(
                            (s) => s.employeeId === record.employeeId
                          );
                          const prevVal = prev ? displayDecimal(prev.output.standardPay) : "—";
                          return <span>{currentVal} <small style={{ color: "#8c8c8c" }}>({prevVal})</small></span>;
                        },
                      },
                      {
                        title: "Tiền OT (VND)",
                        render: (_, record) => {
                          const currentVal = displayDecimal(record.output.overtimePay);
                          if (!compareVersion || !compareSnapshotQuery.data) return currentVal;
                          const prev = compareSnapshotQuery.data.snapshots.find(
                            (s) => s.employeeId === record.employeeId
                          );
                          const prevVal = prev ? displayDecimal(prev.output.overtimePay) : "—";
                          return <span>{currentVal} <small style={{ color: "#8c8c8c" }}>({prevVal})</small></span>;
                        },
                      },
                      {
                        title: "Tổng Gross (VND)",
                        render: (_, record) => {
                          const currentVal = displayDecimal(record.output.gross);
                          if (!compareVersion || !compareSnapshotQuery.data) return currentVal;
                          const prev = compareSnapshotQuery.data.snapshots.find(
                            (s) => s.employeeId === record.employeeId
                          );
                          const prevVal = prev ? displayDecimal(prev.output.gross) : "—";
                          return <span>{currentVal} <small style={{ color: "#8c8c8c" }}>({prevVal})</small></span>;
                        },
                      },
                      {
                        title: "Thực nhận Net (VND)",
                        render: (_, record) => {
                          const currentVal = displayDecimal(record.output.net);
                          if (!compareVersion || !compareSnapshotQuery.data) return <strong>{currentVal}</strong>;
                          const prev = compareSnapshotQuery.data.snapshots.find(
                            (s) => s.employeeId === record.employeeId
                          );
                          const prevVal = prev ? displayDecimal(prev.output.net) : "—";
                          return <strong>{currentVal} <small style={{ color: "#8c8c8c" }}>({prevVal})</small></strong>;
                        },
                      },
                      {
                        title: "Trạng thái",
                        render: (_, record) =>
                          record.output.canPublish ? (
                            <Tag color="success">Hợp lệ</Tag>
                          ) : (
                            <Tag color="error">Có lỗi chặn ({record.output.blockingReasons.length})</Tag>
                          ),
                      },
                      {
                        title: "Thao tác",
                        render: (_, record) => (
                          <Button
                            size="small"
                            icon={<EyeOutlined />}
                            onClick={() => setActiveSnapshotDrawer(record)}
                          >
                            Chi tiết
                          </Button>
                        ),
                      },
                    ]}
                  />
                </div>
              </>
            ) : null}
          </section>

          {/* Reconciliation & Published Items (when published/locked) */}
          {(periodState.status === "Published" || periodState.status === "Locked") && (
            <>
              <Divider style={{ margin: "24px 0 16px 0" }} />
              <section className="payroll-reconciliation-section">
                <Typography.Title level={5} style={{ marginBottom: 12 }}>
                  Đối soát dữ liệu phiếu lương máy chủ
                </Typography.Title>

                <PayrollFeedback error={reconcileQuery.error || itemsQuery.error} />
                {reconcileQuery.data && (
                  <Descriptions
                    size="small"
                    bordered
                    column={{ xs: 1, sm: 2, md: 4 }}
                    style={{ marginBottom: 16 }}
                    items={[
                      { key: "count", label: "Tổng số phiếu", children: reconcileQuery.data.payslipCount },
                      { key: "slipNet", label: "Tổng Net phiếu lương", children: `${displayDecimal(reconcileQuery.data.payslipNet)} ${reconcileQuery.data.currency}` },
                      { key: "snapNet", label: "Tổng Net snapshot", children: `${displayDecimal(reconcileQuery.data.snapshotNet)} ${reconcileQuery.data.currency}` },
                      {
                        key: "diff",
                        label: "Chênh lệch đối soát",
                        children: (
                          <Tag color={reconcileQuery.data.difference === "0" || reconcileQuery.data.difference === "0.00" ? "success" : "error"}>
                            {displayDecimal(reconcileQuery.data.difference)} {reconcileQuery.data.currency}
                          </Tag>
                        ),
                      },
                    ]}
                  />
                )}

                <div className="business-table" role="region" aria-label="Danh sách phiếu lương phát hành" tabIndex={0}>
                  <Table
                    rowKey="id"
                    dataSource={itemsQuery.data?.items ?? []}
                    loading={itemsQuery.isLoading}
                    pagination={{
                      current: itemsPage,
                      pageSize: 20,
                      total: itemsQuery.data?.totalCount,
                      onChange: setItemsPage,
                      showSizeChanger: false,
                    }}
                    columns={[
                      {
                        title: "Nhân viên",
                        render: (_, record) =>
                          employeeNames.labels.get(record.employeeId) || record.employeeId,
                      },
                      {
                        title: "Số tiền thực nhận (VND)",
                        dataIndex: "amount",
                        render: displayDecimal,
                      },
                      {
                        title: "Trạng thái",
                        dataIndex: "status",
                        render: (status) => <Tag color="blue">{status}</Tag>,
                      },
                      {
                        title: "Ngày công bố",
                        dataIndex: "publishedAtUtc",
                        render: (date) => (date ? new Date(date).toLocaleString("vi-VN") : "—"),
                      },
                    ]}
                  />
                </div>
              </section>
            </>
          )}
        </Card>
      ) : (
        <Card>
          <p>Chưa có kỳ lương nào được chọn hoặc danh sách kỳ trống. Vui lòng tạo kỳ lương mới.</p>
        </Card>
      )}

      {/* Modal: Create new period */}
      <Modal
        open={createPeriodOpen}
        title="Tạo kỳ lương mới"
        onCancel={() => {
          if (!createPeriodCommand.isPending) {
            setCreatePeriodOpen(false);
            createForm.resetFields();
          }
        }}
        footer={null}
        destroyOnClose
      >
        <PayrollFeedback error={createPeriodCommand.error} />
        <Form
          name="payroll-create"
          form={createForm}
          layout="vertical"
          onFinish={(v) => createPeriodCommand.mutate(v)}
          disabled={createPeriodCommand.isPending}
        >
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
            <Form.Item
              name="year"
              label="Năm"
              initialValue={new Date().getFullYear()}
              rules={[{ required: true, message: "Chọn năm từ 2000 đến 2100" }]}
            >
              <InputNumber min={2000} max={2100} precision={0} className="full-width" />
            </Form.Item>
            <Form.Item
              name="month"
              label="Tháng"
              initialValue={new Date().getMonth() + 1}
              rules={[{ required: true, message: "Chọn tháng từ 1 đến 12" }]}
            >
              <InputNumber min={1} max={12} precision={0} className="full-width" />
            </Form.Item>
          </div>

          <Form.Item
            name="reason"
            label="Lý do khởi tạo kỳ lương"
            rules={[
              { required: true, message: "Nhập lý do tạo kỳ lương" },
              { min: 10, message: "Lý do phải có ít nhất 10 ký tự" },
            ]}
          >
            <Input.TextArea rows={3} maxLength={500} placeholder="Nhập lý do tạo kỳ lương (tối thiểu 10 ký tự)" />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={createPeriodCommand.isPending} block>
            Tạo kỳ lương
          </Button>
        </Form>
      </Modal>

      {/* Modal: Workflow Confirmation */}
      <Modal
        open={actionModal.open}
        title={actionModal.title}
        onCancel={() => {
          if (!workflowCommand.isPending) {
            setActionModal((prev) => ({ ...prev, open: false }));
            workflowForm.resetFields();
          }
        }}
        footer={null}
        destroyOnClose
      >
        <div style={{ marginBottom: 16 }}>
          <p>
            Kỳ lương: <strong>{periodState?.periodCode}</strong> · Bản chạy đang xác nhận: <strong>{actionModal.expected?.runVersion}</strong>
          </p>
          <p style={{ color: "#8c8c8c", fontSize: 13 }}>
            Kiểm tra kỳ và bản chạy trước khi xác nhận. Nếu dữ liệu đã thay đổi, tải lại và đối chiếu trước khi thực hiện.
          </p>
        </div>

        <PayrollFeedback error={workflowCommand.error} />
        {periodState && actionModal.expected && periodState.rowVersion !== actionModal.expected.rowVersion && (
          <Alert type="warning" showIcon message="Kỳ lương đã thay đổi; cần đối chiếu bản mới" description={
            <Button onClick={() => setActionModal(previous => ({ ...previous, expected: periodState }))}>
              Tải bản mới để đối chiếu
            </Button>
          } />
        )}

        <Form
          name="payroll-workflow"
          form={workflowForm}
          layout="vertical"
          onFinish={(v) => workflowCommand.mutate(v)}
          disabled={workflowCommand.isPending}
        >
          <Form.Item
            name="reason"
            label="Lý do thực hiện thao tác"
            rules={[
              { required: true, message: "Nhập lý do thực hiện thao tác" },
              { min: 10, message: "Lý do phải có ít nhất 10 ký tự theo quy định hệ thống" },
            ]}
          >
            <Input.TextArea
              rows={3}
              maxLength={500}
              placeholder="Nhập lý do thực hiện thao tác (tối thiểu 10 ký tự để lưu vết kiểm toán)"
            />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={workflowCommand.isPending} block>
            Xác nhận thực hiện
          </Button>
        </Form>
      </Modal>

      {/* Drawer: Employee snapshot detailed lines and days */}
      <Drawer
        open={!!activeSnapshotDrawer}
        title={`Chi tiết tính toán · ${
          activeSnapshotDrawer ? employeeNames.labels.get(activeSnapshotDrawer.employeeId) || activeSnapshotDrawer.employeeId : ""
        }`}
        width={650}
        onClose={() => setActiveSnapshotDrawer(null)}
      >
        {activeSnapshotDrawer && (
          <div>
            <Descriptions
              size="small"
              bordered
              column={1}
              style={{ marginBottom: 16 }}
              items={[
                { key: "gross", label: "Tổng thu nhập (Gross)", children: `${displayDecimal(activeSnapshotDrawer.output.gross)} VND` },
                { key: "std", label: "Lương công chuẩn", children: `${displayDecimal(activeSnapshotDrawer.output.standardPay)} VND` },
                { key: "ot", label: "Tiền làm thêm giờ (OT)", children: `${displayDecimal(activeSnapshotDrawer.output.overtimePay)} VND` },
                { key: "round", label: "Điều chỉnh làm tròn", children: `${displayDecimal(activeSnapshotDrawer.output.roundingAdjustment)} VND` },
                { key: "net", label: "Lương thực nhận (Net)", children: <strong>{displayDecimal(activeSnapshotDrawer.output.net)} VND</strong> },
              ]}
            />

            <Typography.Title level={5} style={{ marginTop: 16 }}>
              Các khoản chi tiết (Lines)
            </Typography.Title>
            <Table
              size="small"
              rowKey="code"
              dataSource={activeSnapshotDrawer.output.lines}
              pagination={false}
              columns={[
                { title: "Mã khoản", dataIndex: "code" },
                { title: "Số tiền (VND)", dataIndex: "amount", render: displayDecimal },
              ]}
            />

            <Typography.Title level={5} style={{ marginTop: 24 }}>
              Dữ liệu ngày công trong kỳ (Days Input)
            </Typography.Title>
            <Table
              size="small"
              rowKey="date"
              dataSource={activeSnapshotDrawer.input.days}
              pagination={{ pageSize: 7, showSizeChanger: false }}
              columns={[
                { title: "Ngày", dataIndex: "date" },
                { title: "Phút có mặt", dataIndex: "attendanceMinutes", render: displayDecimal },
                {
                  title: "Quẹt đủ",
                  dataIndex: "checkoutComplete",
                  render: (v) => (v ? <Tag color="green">Đủ</Tag> : <Tag color="orange">Thiếu ra</Tag>),
                },
                {
                  title: "Phép hưởng lương",
                  dataIndex: "paidLeaveApproved",
                  render: (v) => (v ? <Tag color="blue">Có phép</Tag> : "—"),
                },
                { title: "Phút OT", dataIndex: "approvedOvertimeMinutes", render: displayDecimal },
              ]}
            />
          </div>
        )}
      </Drawer>
    </div>
  );
}
