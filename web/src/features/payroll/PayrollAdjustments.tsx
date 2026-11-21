import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Card,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
} from "antd";
import { DeleteOutlined, PlusOutlined, ReloadOutlined } from "@ant-design/icons";
import { useAuth } from "../../context/AuthContext";
import { useCommand } from "../shared/commands";
import { EmployeePicker } from "../shared/EmployeePicker";
import { useEmployeeLabels } from "../shared/EmployeeLabels";
import { displayDecimal } from "../shared/display";
import { payrollApi, type PayrollAdjustmentResponse, type SalaryHistoryResponse } from "./api";
import { PayrollFeedback } from "./PayrollFeedback";

const adjustmentKinds = [
  { value: "Allowance", label: "Phụ cấp demo (Allowance)" },
  { value: "Bonus", label: "Thưởng demo (Bonus)" },
  { value: "OtherDeduction", label: "Khấu trừ khác demo (OtherDeduction)" },
  { value: "DemoInsurance", label: "Bảo hiểm demo (DemoInsurance)" },
  { value: "DemoTax", label: "Thuế TNCN demo (DemoTax)" },
];

const kindLabels: Record<string, string> = {
  Allowance: "Phụ cấp demo",
  Bonus: "Thưởng demo",
  OtherDeduction: "Khấu trừ khác",
  DemoInsurance: "Bảo hiểm demo",
  DemoTax: "Thuế TNCN demo",
};

export function PayrollAdjustments({ onNotice }: { onNotice: (msg: string) => void }) {
  const { hasPermission } = useAuth();

  // Tab 1: Adjustments state
  const [selectedPeriodId, setSelectedPeriodId] = useState<string>();
  const [addAdjustmentOpen, setAddAdjustmentOpen] = useState(false);
  const [voidModal, setVoidModal] = useState<{ open: boolean; adjustment?: PayrollAdjustmentResponse }>({
    open: false,
  });

  const [addForm] = Form.useForm<{
    employeeId: string;
    kind: string;
    amount: string;
    reason: string;
  }>();
  const [voidForm] = Form.useForm<{ reason: string }>();

  // Tab 2: Salary History state
  const [historyEmployeeId, setHistoryEmployeeId] = useState<string>();
  const [appendSalaryOpen, setAppendSalaryOpen] = useState(false);
  const [salaryForm] = Form.useForm<{
    effectiveFrom: string;
    monthlyBase: string;
    reason: string;
  }>();

  // Periods query for adjustment selection
  const periodsQuery = useQuery({
    queryKey: ["payroll-periods", "adjustments"],
    queryFn: ({ signal }) => payrollApi.listPeriods({ page: 1, pageSize: 50 }, signal),
    retry: false,
  });

  const periods = periodsQuery.data?.items ?? [];
  const activePeriodId = selectedPeriodId ?? (periods.length > 0 ? periods[0]!.id : undefined);
  const activePeriod = periods.find((p) => p.id === activePeriodId);
  const isPeriodClosed =
    activePeriod?.status === "Published" ||
    activePeriod?.status === "Locked";

  // Adjustments query for active period
  const adjustmentsQuery = useQuery({
    queryKey: ["payroll-adjustments", activePeriodId],
    queryFn: ({ signal }) => payrollApi.getAdjustments(activePeriodId!, signal),
    enabled: !!activePeriodId,
    retry: false,
  });

  const adjustments = adjustmentsQuery.data ?? [];
  const [adjustmentPage, setAdjustmentPage] = useState(1);
  const currentAdjustmentPage = Math.min(adjustmentPage, Math.max(1, Math.ceil(adjustments.length / 10)));
  const adjustmentEmployees = adjustments.slice((currentAdjustmentPage - 1) * 10, currentAdjustmentPage * 10).map(a => a.employeeId);
  const adjustmentEmployeeLabels = useEmployeeLabels(adjustmentEmployees);

  // Salary history query
  const salaryHistoryQuery = useQuery({
    queryKey: ["payroll-salary-history", historyEmployeeId],
    queryFn: ({ signal }) => payrollApi.getSalaryHistory(historyEmployeeId!, signal),
    enabled: !!historyEmployeeId && hasPermission("payroll.history_manage"),
    retry: false,
  });

  const salaryEntries = salaryHistoryQuery.data ?? [];

  // Command: Add Adjustment
  const addAdjustmentCommand = useCommand(
    (values: { employeeId: string; kind: string; amount: string; reason: string }, key) => {
      if (!activePeriodId) throw new Error("Chưa chọn kỳ lương.");
      return payrollApi.addAdjustment(
        activePeriodId,
        {
          employeeId: values.employeeId,
          kind: values.kind,
          amount: values.amount,
          reason: values.reason,
        },
        key
      );
    },
    ["payroll-adjustments", "payroll-period-state", "payroll-snapshot", "payroll-periods"],
    () => {
      onNotice("Đã thêm khoản điều chỉnh vào kỳ lương thành công.");
      setAddAdjustmentOpen(false);
      addForm.resetFields();
    },
    `adj:add:${activePeriodId}`
  );

  // Command: Void Adjustment
  const voidAdjustmentCommand = useCommand(
    (values: { reason: string }, key) => {
      if (!voidModal.adjustment) throw new Error("Chưa chọn khoản điều chỉnh.");
      return payrollApi.voidAdjustment(
        voidModal.adjustment.id,
        {
          reason: values.reason,
          rowVersion: voidModal.adjustment.rowVersion,
        },
        key
      );
    },
    ["payroll-adjustments", "payroll-period-state", "payroll-snapshot", "payroll-periods"],
    () => {
      onNotice("Đã hủy bỏ khoản điều chỉnh thành công.");
      setVoidModal({ open: false });
      voidForm.resetFields();
    },
    `adj:void:${voidModal.adjustment?.id}:${voidModal.adjustment?.rowVersion}`
  );

  // Command: Append Salary
  const appendSalaryCommand = useCommand(
    (values: { effectiveFrom: string; monthlyBase: string; reason: string }, key) => {
      if (!historyEmployeeId) throw new Error("Chưa chọn nhân viên.");
      return payrollApi.appendSalary(
        historyEmployeeId,
        {
          effectiveFrom: values.effectiveFrom,
          monthlyBase: values.monthlyBase,
          reason: values.reason,
        },
        key
      );
    },
    ["payroll-salary-history", "payroll-period-state", "payroll-snapshot"],
    (res) => {
      onNotice(`Đã thiết lập mức lương mới hiệu lực từ ${res.effectiveFrom}.`);
      setAppendSalaryOpen(false);
      salaryForm.resetFields();
    },
    `salary:append:${historyEmployeeId}`
  );

  return (
    <Card className="payroll-adjustments-card">
      <Tabs
        defaultActiveKey="adjustments"
        items={[
          {
            key: "adjustments",
            label: "Khoản cộng / trừ thủ công (Manual Adjustments)",
            children: (
              <div>
                <div className="payroll-section-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                  <Space wrap>
                    <span>Kỳ lương:</span>
                    <Select
                      style={{ width: 180 }}
                      aria-label="Kỳ lương điều chỉnh"
                      placeholder="Chọn kỳ lương"
                      value={activePeriodId}
                      onChange={setSelectedPeriodId}
                      options={periods.map((p) => ({
                        value: p.id,
                        label: `${p.periodCode} (${p.status})`,
                      }))}
                    />
                    <Button icon={<ReloadOutlined />} onClick={() => { void periodsQuery.refetch(); void adjustmentsQuery.refetch(); }}>
                      Tải lại
                    </Button>
                  </Space>

                  {hasPermission("payroll.calculate") && (
                    <Button
                      type="primary"
                      icon={<PlusOutlined />}
                      disabled={!activePeriodId || isPeriodClosed}
                      onClick={() => setAddAdjustmentOpen(true)}
                    >
                      Thêm khoản điều chỉnh
                    </Button>
                  )}
                </div>

                {isPeriodClosed && (
                  <Alert
                    type="warning"
                    showIcon
                    style={{ marginBottom: 16 }}
                    message={`Kỳ lương ${activePeriod?.periodCode} đang ở trạng thái '${activePeriod?.status}'`}
                    description="Kỳ lương đã công bố hoặc khóa; không thể thêm hoặc hủy điều chỉnh."
                  />
                )}

                <PayrollFeedback error={periodsQuery.error || adjustmentsQuery.error} />

                <div className="business-table" role="region" aria-label="Danh sách khoản điều chỉnh" tabIndex={0}>
                  <Table<PayrollAdjustmentResponse>
                    rowKey="id"
                    dataSource={adjustments}
                    loading={adjustmentsQuery.isLoading}
                    pagination={{ current: currentAdjustmentPage, onChange: setAdjustmentPage, pageSize: 10, showSizeChanger: false }}
                    columns={[
                      {
                        title: "Nhân viên",
                        render: (_, record) =>
                          adjustmentEmployeeLabels.labels.get(record.employeeId) || record.employeeId,
                      },
                      {
                        title: "Khoản điều chỉnh",
                        dataIndex: "kind",
                        render: (kind) => kindLabels[kind] || kind,
                      },
                      {
                        title: "Số tiền (VND)",
                        dataIndex: "amount",
                        render: displayDecimal,
                      },
                      {
                        title: "Lý do",
                        dataIndex: "reason",
                      },
                      {
                        title: "Trạng thái",
                        render: (_, record) =>
                          record.voidedAtUtc ? (
                            <Tag color="default">
                              Đã hủy ({record.voidReason || "Hủy bỏ"})
                            </Tag>
                          ) : (
                            <Tag color="success">Đang áp dụng</Tag>
                          ),
                      },
                      {
                        title: "Thao tác",
                        render: (_, record) =>
                          !record.voidedAtUtc &&
                          hasPermission("payroll.calculate") &&
                          !isPeriodClosed ? (
                            <Button
                              danger
                              size="small"
                              icon={<DeleteOutlined />}
                              onClick={() => setVoidModal({ open: true, adjustment: record })}
                            >
                              Hủy bỏ
                            </Button>
                          ) : null,
                      },
                    ]}
                  />
                </div>
              </div>
            ),
          },
          {
            key: "salary-history",
            label: "Lịch sử mức lương (Salary History)",
            children: (
              <div>
                {!hasPermission("payroll.history_manage") ? (
                  <Alert
                    type="warning"
                    showIcon
                    message="Không có quyền quản lý lịch sử lương"
                    description="Tài khoản của bạn cần được cấp quyền 'payroll.history_manage' để xem và thiết lập mốc lương cho nhân viên."
                  />
                ) : (
                  <div>
                    <div className="payroll-section-heading" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                      <Space wrap>
                        <span>Nhân viên:</span>
                        <div style={{ width: 300 }}>
                          <EmployeePicker
                            value={historyEmployeeId}
                            onChange={(id) => setHistoryEmployeeId(id)}
                          />
                        </div>
                        <Button
                          icon={<ReloadOutlined />}
                          disabled={!historyEmployeeId}
                          onClick={() => void salaryHistoryQuery.refetch()}
                        >
                          Tải lại
                        </Button>
                      </Space>

                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        disabled={!historyEmployeeId}
                        onClick={() => setAppendSalaryOpen(true)}
                      >
                        Thiết lập mức lương mới
                      </Button>
                    </div>

                    <PayrollFeedback error={salaryHistoryQuery.error} />

                    <div className="business-table" role="region" aria-label="Lịch sử mức lương nhân viên" tabIndex={0}>
                      <Table<SalaryHistoryResponse>
                        rowKey="id"
                        dataSource={salaryEntries}
                        loading={salaryHistoryQuery.isLoading}
                        pagination={false}
                        columns={[
                          {
                            title: "Hiệu lực từ ngày",
                            dataIndex: "effectiveFrom",
                          },
                          {
                            title: "Hiệu lực đến ngày",
                            dataIndex: "effectiveTo",
                            render: (date) => (date ? date : <Tag color="green">Đang áp dụng</Tag>),
                          },
                          {
                            title: "Mức lương cơ bản (VND)",
                            dataIndex: "monthlyBase",
                            render: displayDecimal,
                          },
                          {
                            title: "Lý do",
                            dataIndex: "reason",
                          },
                          {
                            title: "Thời điểm ghi nhận",
                            dataIndex: "createdAtUtc",
                            render: (date) => new Date(date).toLocaleString("vi-VN"),
                          },
                        ]}
                      />
                    </div>
                  </div>
                )}
              </div>
            ),
          },
        ]}
      />

      {/* Modal: Add Adjustment */}
      <Modal
        open={addAdjustmentOpen}
        title={`Thêm khoản điều chỉnh · Kỳ ${activePeriod?.periodCode || ""}`}
        onCancel={() => {
          if (!addAdjustmentCommand.isPending) {
            setAddAdjustmentOpen(false);
            addForm.resetFields();
          }
        }}
        footer={null}
        destroyOnClose
      >
        <PayrollFeedback error={addAdjustmentCommand.error} />
        <Form
          name="payroll-adjustment-add"
          form={addForm}
          layout="vertical"
          onFinish={(v) => addAdjustmentCommand.mutate(v)}
          disabled={addAdjustmentCommand.isPending}
        >
          <Form.Item name="employeeId" label="Nhân viên" rules={[{ required: true, message: "Chọn nhân viên" }]}>
            <EmployeePicker />
          </Form.Item>

          <Form.Item name="kind" label="Loại điều chỉnh" rules={[{ required: true, message: "Chọn loại điều chỉnh" }]}>
            <Select options={adjustmentKinds} placeholder="Chọn loại điều chỉnh demo" />
          </Form.Item>

          <Form.Item
            name="amount"
            label="Số tiền (VND)"
            rules={[
              { required: true, message: "Nhập số tiền" },
              {
                pattern: /^(?:\d+)(?:\.\d{1,2})?$/,
                message: "Số tiền phải là số dương, tối đa 2 chữ số thập phân (không hỗ trợ làm tròn sớm)",
              },
            ]}
          >
            <Input inputMode="decimal" placeholder="Ví dụ: 1000000 hoặc 500000.50" />
          </Form.Item>

          <Form.Item
            name="reason"
            label="Lý do điều chỉnh"
            rules={[
              { required: true, message: "Nhập lý do điều chỉnh" },
              { min: 10, message: "Lý do phải có ít nhất 10 ký tự" },
            ]}
          >
            <Input.TextArea rows={3} maxLength={500} placeholder="Nhập lý do điều chỉnh (tối thiểu 10 ký tự)" />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={addAdjustmentCommand.isPending} block>
            Lưu khoản điều chỉnh
          </Button>
        </Form>
      </Modal>

      {/* Modal: Void Adjustment */}
      <Modal
        open={voidModal.open}
        title="Hủy bỏ khoản điều chỉnh"
        onCancel={() => {
          if (!voidAdjustmentCommand.isPending) {
            setVoidModal({ open: false });
            voidForm.resetFields();
          }
        }}
        footer={null}
        destroyOnClose
      >
        <p>
          Hủy khoản: <strong>{kindLabels[voidModal.adjustment?.kind || ""] || voidModal.adjustment?.kind}</strong> · Số tiền:{" "}
          <strong>{displayDecimal(voidModal.adjustment?.amount || "0")} VND</strong>
        </p>

        <PayrollFeedback error={voidAdjustmentCommand.error} />

        <Form
          name="payroll-adjustment-void"
          form={voidForm}
          layout="vertical"
          onFinish={(v) => voidAdjustmentCommand.mutate(v)}
          disabled={voidAdjustmentCommand.isPending}
        >
          <Form.Item
            name="reason"
            label="Lý do hủy bỏ"
            rules={[
              { required: true, message: "Nhập lý do hủy bỏ" },
              { min: 10, message: "Lý do hủy phải có ít nhất 10 ký tự" },
            ]}
          >
            <Input.TextArea rows={3} maxLength={500} placeholder="Nhập lý do hủy bỏ khoản điều chỉnh" />
          </Form.Item>

          <Button danger type="primary" htmlType="submit" loading={voidAdjustmentCommand.isPending} block>
            Xác nhận hủy khoản điều chỉnh
          </Button>
        </Form>
      </Modal>

      {/* Modal: Append Salary */}
      <Modal
        open={appendSalaryOpen}
        title="Thiết lập mức lương mới"
        onCancel={() => {
          if (!appendSalaryCommand.isPending) {
            setAppendSalaryOpen(false);
            salaryForm.resetFields();
          }
        }}
        footer={null}
        destroyOnClose
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Quy tắc tính bất biến của mốc lương"
          description="Mức lương mới sẽ tự động khép khoảng áp dụng của mốc lương liền trước. Ngày hiệu lực phải sau ngày của mốc cũ và không được thuộc kỳ lương đã công bố hoặc khóa."
        />

        <PayrollFeedback error={appendSalaryCommand.error} />

        <Form
          name="payroll-salary"
          form={salaryForm}
          layout="vertical"
          onFinish={(v) => appendSalaryCommand.mutate(v)}
          disabled={appendSalaryCommand.isPending}
        >
          <Form.Item
            name="effectiveFrom"
            label="Ngày bắt đầu hiệu lực"
            rules={[{ required: true, message: "Chọn ngày bắt đầu hiệu lực" }]}
          >
            <Input type="date" min="2000-01-01" max="2100-12-31" />
          </Form.Item>

          <Form.Item
            name="monthlyBase"
            label="Mức lương cơ bản (VND)"
            rules={[
              { required: true, message: "Nhập mức lương cơ bản" },
              {
                pattern: /^(?:\d+)(?:\.\d{1,2})?$/,
                message: "Mức lương phải là số dương, tối đa 2 chữ số thập phân",
              },
            ]}
          >
            <Input inputMode="decimal" placeholder="Ví dụ: 20000000" />
          </Form.Item>

          <Form.Item
            name="reason"
            label="Lý do điều chỉnh lương"
            rules={[
              { required: true, message: "Nhập lý do điều chỉnh lương" },
              { min: 10, message: "Lý do phải có ít nhất 10 ký tự" },
            ]}
          >
            <Input.TextArea rows={3} maxLength={500} placeholder="Quyết định nâng lương số... (tối thiểu 10 ký tự)" />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={appendSalaryCommand.isPending} block>
            Lưu mức lương mới
          </Button>
        </Form>
      </Modal>
    </Card>
  );
}
