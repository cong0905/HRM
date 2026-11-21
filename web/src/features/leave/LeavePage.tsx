import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Alert, Button, Descriptions, Form, Input, Modal, Select, Space, Table, Tabs, Tag, Typography } from "antd";
import { useAuth } from "../../context/AuthContext";
import { ApiClientError } from "../../api/types";
import { calendarApi, personnelApi, type Schema } from "../shared/api";
import { EmployeePicker } from "../shared/EmployeePicker";
import { Feedback } from "../shared/Feedback";
import { PeriodNotice, usePeriodGuard } from "../shared/PeriodGuard";
import { useCommand } from "../shared/commands";
import { leaveApi, type Leave } from "./api";
import { LeaveCalendar } from "./LeaveCalendar";
import { useEmployeeLabels } from "../shared/EmployeeLabels";
import { displayDecimal } from "../shared/display";

const statuses: Record<string, string> = { Pending: "Chờ duyệt", Approved: "Đã duyệt", Rejected: "Từ chối", Cancelled: "Đã hủy" };
const actions = { approve: "Duyệt đơn", reject: "Từ chối đơn", cancel: "Hủy đơn" };
type Mode = keyof typeof actions;

function Balance({ rows }: { rows?: Schema<"LeaveBalanceResponse">[] }) {
  return rows?.length ? <Space direction="vertical" className="full-width">{rows.map(row => <Descriptions key={row.id} size="small" bordered column={{ xs: 1, sm: 3 }} items={[
    { key: "allocated", label: `Quỹ năm ${row.year}`, children: displayDecimal(row.allocatedDays) }, { key: "used", label: "Đã dùng (ngày)", children: displayDecimal(row.usedDays) }, { key: "remaining", label: "Còn lại (ngày)", children: displayDecimal(row.remainingDays) },
  ]} />)}</Space> : <Alert type="info" message="Chưa được cấp quỹ cho năm đã chọn. Liên hệ HR trước khi gửi đơn." />;
}

function LeaveComposer({ onNotice }: { onNotice: (message: string) => void }) {
  const [form] = Form.useForm<Schema<"CreateLeaveRequest">>();
  const start: string | undefined = Form.useWatch("startDate", form), end: string | undefined = Form.useWatch("endDate", form);
  const [showCalendar, setShowCalendar] = useState(false);
  const clock = useQuery({ queryKey: ["business-time"], queryFn: ({ signal }) => calendarApi.now(signal), retry: false });
  const year = Number(start?.slice(0, 4) || clock.data?.businessDate.slice(0, 4) || 0);
  const balance = useQuery({ queryKey: ["leave-balances", "mine", year], queryFn: ({ signal }) => leaveApi.ownBalances(year, signal), enabled: year >= 2000 && year <= 2100, retry: false });
  const guard = usePeriodGuard(start, end);
  const command = useCommand((values: Schema<"CreateLeaveRequest">, key) => leaveApi.create(values, key), ["leaves", "leave-balances"], () => { form.resetFields(); setShowCalendar(false); onNotice("Đã gửi đơn nghỉ phép. Quỹ chỉ bị trừ khi đơn được duyệt."); });
  return <section className="leave-composer"><Typography.Title level={3}>Gửi đơn nghỉ phép</Typography.Title>
    <Alert type="info" showIcon message="Chỉ hỗ trợ ngày nguyên trong cùng một năm" description="Đơn qua năm chưa được hỗ trợ; hãy tạo đơn riêng cho từng năm. Máy chủ kiểm tra quỹ, lịch làm việc, trùng đơn và kỳ đã khóa." />
    <Feedback error={clock.error || balance.error} retry={() => { void clock.refetch(); void balance.refetch(); }} />
    {balance.isSuccess && <Balance rows={balance.data} />}
    <Form name="leave-composer" form={form} layout="vertical" onFinish={v => command.mutate(v)} disabled={command.isPending} onFinishFailed={({ errorFields }) => form.scrollToField(errorFields[0]?.name, { focus: true })}>
      <div className="form-grid"><Form.Item name="startDate" label="Nghỉ từ ngày" rules={[{ required: true }]}><Input type="date" min="2000-01-01" max="2100-12-31" /></Form.Item>
        <Form.Item name="endDate" label="Nghỉ đến ngày" dependencies={["startDate"]} rules={[{ required: true }, { validator: (_, value: string) => !value || !start || (value >= start && value.slice(0, 4) === start.slice(0, 4)) ? Promise.resolve() : Promise.reject(new Error("Ngày kết thúc phải từ ngày bắt đầu và cùng năm.")) }]}><Input type="date" min="2000-01-01" max="2100-12-31" /></Form.Item></div>
      <Form.Item name="reason" label="Lý do nghỉ phép" rules={[{ required: true, whitespace: true }]}><Input.TextArea maxLength={500} /></Form.Item>
      <PeriodNotice guard={guard} /><Feedback error={command.error} />
      <Space wrap><Button type="primary" htmlType="submit" loading={command.isPending} disabled={!guard.canWrite || !balance.isSuccess}>Gửi đơn nghỉ phép</Button>
        <Button disabled={!start || !end || start > end || start.slice(0, 4) !== end.slice(0, 4)} onClick={() => setShowCalendar(v => !v)}>Xem lịch ngày nghỉ</Button></Space>
    </Form>
    {showCalendar && start && end && <LeaveCalendar key={`${start}-${end}`} start={start} end={end} />}
  </section>;
}

function LeaveActions({ row, team, selfId, identityKnown, onAction }: { row: Leave; team: boolean; selfId?: string; identityKnown: boolean; onAction: (row: Leave, mode: Mode) => void }) {
  const { hasPermission } = useAuth();
  const guard = usePeriodGuard(row.startDate, row.endDate);
  const self = row.employeeId === selfId;
  if (row.status !== "Pending") return <span>Đã xử lý</span>;
  if (guard.closed) return <Tag>Kỳ đã khóa · chỉ xem</Tag>;
  return <Space direction="vertical"><Space wrap>
    {!team && hasPermission("leaves.cancel_own") && <Button disabled={!guard.canWrite} onClick={() => onAction(row, "cancel")}>Hủy đơn</Button>}
    {team && hasPermission("leaves.approve") && <><Button disabled={!guard.canWrite || !identityKnown || self} onClick={() => onAction(row, "approve")}>Duyệt đơn</Button><Button danger disabled={!guard.canWrite || !identityKnown || self} onClick={() => onAction(row, "reject")}>Từ chối đơn</Button></>}
  </Space>{team && self && <span>Không được tự duyệt hoặc từ chối đơn của mình.</span>}<Feedback error={guard.error} retry={() => void guard.refetch()} /></Space>;
}

function LeaveRegister({ team, onNotice }: { team: boolean; onNotice: (message: string) => void }) {
  const { hasPermission } = useAuth();
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<{ employeeId?: string; status?: string; year?: number }>({ status: "Pending" });
  const [selected, setSelected] = useState<{ row: Leave; mode: Mode }>();
  const [calendar, setCalendar] = useState<Leave>();
  const [form] = Form.useForm<{ reason: string }>();
  const own = useQuery({ queryKey: ["my-profile"], queryFn: ({ signal }) => personnelApi.me(signal), retry: false });
  const noProfile = own.error instanceof ApiClientError && own.error.status === 404 && own.error.code === "employee.not_found";
  const query = useQuery({ queryKey: ["leaves", team ? "team" : "mine", page, filters], queryFn: ({ signal }) => team ? leaveApi.team({ page, pageSize: 20, ...filters }, signal) : leaveApi.mine(page, signal), retry: false });
  const names = useEmployeeLabels(team && !query.isError ? query.data?.items.map(row => row.employeeId) || [] : []);
  const guard = usePeriodGuard(selected?.row.startDate, selected?.row.endDate);
  const command = useCommand(({ reason }: { reason: string }, key) => leaveApi.decide(selected!.mode, selected!.row.id, { reason, rowVersion: selected!.row.rowVersion }, key), ["leaves", "leave-balances", "attendance", "period-state"], () => {
    onNotice("Máy chủ đã ghi nhận quyết định đối với đơn nghỉ phép."); setSelected(undefined);
  }, `${selected?.mode}:${selected?.row.id}:${selected?.row.rowVersion}`);
  const latest = useMutation({ mutationFn: () => leaveApi.get(selected!.row.id), onSuccess: row => { setSelected(previous => previous && { ...previous, row }); command.reset(); } });
  const balance = useQuery({ queryKey: ["leave-balances", selected?.row.employeeId, selected?.row.startDate.slice(0, 4)], enabled: !!selected && team,
    queryFn: ({ signal }) => leaveApi.balances(selected!.row.employeeId, Number(selected!.row.startDate.slice(0, 4)), signal), retry: false });
  const open = (row: Leave, mode: Mode) => { form.resetFields(); command.reset(); latest.reset(); setSelected({ row, mode }); };
  const conflict = command.error instanceof ApiClientError && command.error.isConflict;
  return <>
    {team && <Form name="leave-team-filters" layout="inline" initialValues={{ status: "Pending" }} className="business-filters" onFinish={v => { setPage(1); setFilters({ employeeId: v.employeeId || undefined, status: v.status || undefined, year: v.year ? Number(v.year) : undefined }); }}>
      {hasPermission("employees.read") && <Form.Item name="employeeId" label="Nhân viên"><EmployeePicker /></Form.Item>}
      <Form.Item name="status" label="Trạng thái đơn"><Select allowClear className="status-filter" options={Object.entries(statuses).map(([value, label]) => ({ value, label }))} /></Form.Item>
      <Form.Item name="year" label="Năm"><Input type="number" min={2000} max={2100} /></Form.Item><Button htmlType="submit">Lọc đơn</Button>
    </Form>}
    <Feedback error={query.error || (!noProfile && own.error) || names.error} retry={() => { void query.refetch(); void own.refetch(); void names.refetch(); }} />
    <div className="business-table" role="region" aria-label={team ? "Hàng đợi duyệt nghỉ phép" : "Đơn nghỉ phép của tôi"} tabIndex={0}><Table<Leave> rowKey="id" dataSource={query.isError ? [] : query.data?.items} loading={query.isFetching} scroll={{ x: 1000 }} pagination={{ current: page, pageSize: 20, total: query.data?.totalCount, showSizeChanger: false, onChange: setPage }} columns={[
      { title: "Khoảng nghỉ", render: (_, row) => <Button type="link" onClick={() => setCalendar(row)}>{row.startDate} → {row.endDate}</Button> },
      ...(team ? [{ title: "Nhân viên", render: (_: unknown, row: Leave) => names.labels.get(row.employeeId) || row.employeeId }] : []),
      { title: "Ngày phép", dataIndex: "workingDays", render: displayDecimal }, { title: "Lý do", dataIndex: "reason" },
      { title: "Trạng thái", render: (_, row) => <Tag color={row.status === "Pending" ? "orange" : "blue"}>{statuses[row.status] || row.status}</Tag> },
      { title: "Quyết định", render: (_, row) => row.decisionReason || row.cancellationReason || "—" },
      { title: "Thao tác", render: (_, row) => <LeaveActions row={row} team={team} selfId={own.isError ? undefined : own.data?.id} identityKnown={own.isSuccess || noProfile} onAction={open} /> },
    ]} /></div>
    <Modal open={!!selected} title={selected ? actions[selected.mode] : "Xử lý đơn"} footer={null} onCancel={() => { if (!command.isPending) setSelected(undefined); }} closable={!command.isPending} maskClosable={!command.isPending} destroyOnClose>
      <p>{selected?.row.startDate} → {selected?.row.endDate} · {selected?.row.workingDays} ngày phép · {statuses[selected?.row.status || ""]}</p>
      <PeriodNotice guard={guard} /><Feedback error={command.error || latest.error || balance.error} />{team && balance.isSuccess && <Balance rows={balance.data} />}
      {conflict && <Alert type="warning" message="Quyết định chưa được ghi nhận từ thao tác này" description={<><p>Bản nhập của bạn vẫn giữ nguyên. Đơn có thể đã được xử lý ở phiên khác; tải quyết định thật từ máy chủ trước khi tiếp tục.</p><Button loading={latest.isPending} onClick={() => latest.mutate()}>Tải trạng thái đơn mới nhất</Button></>} />}
      <Form name={team ? "leave-team-decision" : "leave-own-decision"} form={form} layout="vertical" onFinish={v => command.mutate(v)} disabled={command.isPending}>
        <Form.Item name="reason" label="Lý do quyết định" rules={[{ required: true, whitespace: true }]}><Input.TextArea maxLength={500} /></Form.Item>
        <Button type="primary" danger={selected?.mode !== "approve"} htmlType="submit" loading={command.isPending} disabled={!guard.canWrite || conflict || selected?.row.status !== "Pending"}>{selected ? actions[selected.mode] : "Xác nhận"}</Button>
      </Form>
    </Modal>
    <Modal title="Chi tiết lịch ngày nghỉ" open={!!calendar} footer={null} width={800} onCancel={() => setCalendar(undefined)} destroyOnClose>{calendar && <LeaveCalendar key={calendar.id} start={calendar.startDate} end={calendar.endDate} />}</Modal>
  </>;
}

export function LeavePage() {
  const { hasPermission } = useAuth();
  const [notice, setNotice] = useState("");
  const team = hasPermission("leaves.read_all") || hasPermission("leaves.approve");
  return <section className="business-page"><Typography.Title level={2}>Nghỉ phép</Typography.Title><Typography.Text type="secondary">Quỹ, trạng thái đơn và quyết định đều lấy từ máy chủ.</Typography.Text>
    {notice && <Alert type="success" showIcon message={notice} closable onClose={() => setNotice("")} />}
    <Tabs defaultActiveKey={team ? "team" : "mine"} destroyInactiveTabPane items={[
      ...(hasPermission("leaves.read_own") ? [{ key: "mine", label: "Phép của tôi", children: <>{hasPermission("leaves.create_own") && <LeaveComposer onNotice={setNotice} />}<LeaveRegister team={false} onNotice={setNotice} /></> }] : []),
      ...(team ? [{ key: "team", label: "Hàng đợi duyệt", children: <LeaveRegister team onNotice={setNotice} /> }] : []),
    ]} />
  </section>;
}
