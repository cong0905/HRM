import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Alert, Button, Form, Input, Modal, Select, Space, Table, Tabs, Tag, Typography } from "antd";
import { useAuth } from "../../context/AuthContext";
import { ApiClientError } from "../../api/types";
import { calendarApi, personnelApi } from "../shared/api";
import { Feedback } from "../shared/Feedback";
import { EmployeePicker } from "../shared/EmployeePicker";
import { PeriodNotice, usePeriodGuard } from "../shared/PeriodGuard";
import { useCommand } from "../shared/commands";
import { attendanceApi, businessInstant, businessTimeInput, type Attendance } from "./api";
import { useEmployeeLabels } from "../shared/EmployeeLabels";
import { displayDecimal } from "../shared/display";

const labels: Record<string, string> = { MissingCheckout: "Thiếu giờ ra", Corrected: "Đã điều chỉnh", Complete: "Đủ giờ vào/ra", CheckedIn: "Đang trong ca", Incomplete: "Điều chỉnh chưa có giờ ra" };
const time = (value?: string | null) => value ? new Date(value).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" }) : "—";
type Edit = { checkInUtc: string; checkOutUtc?: string; minutes?: string; reason: string };

function RowActions({ row, selfId, identityKnown, onEdit }: { row: Attendance; selfId?: string; identityKnown: boolean; onEdit: (row: Attendance, mode: "adjust" | "overtime") => void }) {
  const guard = usePeriodGuard(row.businessDate);
  const isSelf = row.employeeId === selfId;
  if (guard.closed) return <Tag>Kỳ đã khóa · chỉ xem</Tag>;
  return <Space direction="vertical"><Space wrap>
    <Button disabled={!guard.canWrite || !identityKnown || isSelf} onClick={() => onEdit(row, "adjust")}>Điều chỉnh</Button>
    <Button disabled={!guard.canWrite || !identityKnown || isSelf || !row.checkOutUtc || Number(row.confirmedOvertimeMinutes) > 0} onClick={() => onEdit(row, "overtime")}>Xác nhận OT</Button>
  </Space>{isSelf && <span>Không tự điều chỉnh công.</span>}<Feedback error={guard.error} retry={() => void guard.refetch()} /></Space>;
}

function AttendanceRegister({ team, onNotice }: { team: boolean; onNotice: (text: string) => void }) {
  const { hasPermission } = useAuth();
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<{ employeeId?: string; from?: string; to?: string; status?: string }>({});
  const [selected, setSelected] = useState<{ row: Attendance; mode: "adjust" | "overtime" }>();
  const [form] = Form.useForm<Edit>();
  const own = useQuery({ queryKey: ["my-profile"], queryFn: ({ signal }) => personnelApi.me(signal), enabled: team && hasPermission("attendance.manage"), retry: false });
  const noProfile = own.error instanceof ApiClientError && own.error.status === 404 && own.error.code === "employee.not_found";
  const query = useQuery({ queryKey: ["attendance", team ? "team" : "mine", page, filters], queryFn: ({ signal }) => team
    ? attendanceApi.team({ page, pageSize: 20, ...filters }, signal) : attendanceApi.mine(page, signal), retry: false });
  const names = useEmployeeLabels(team && !query.isError ? query.data?.items.map(row => row.employeeId) || [] : []);
  const guard = usePeriodGuard(selected?.row.businessDate);
  const command = useCommand(async (values: Edit, key) => {
    const row = selected!.row;
    if (selected!.mode === "overtime") return attendanceApi.overtime(row, { minutes: values.minutes!, reason: values.reason, rowVersion: row.rowVersion }, key);
    return attendanceApi.adjust(row, { checkInUtc: businessInstant(values.checkInUtc, row.checkInUtc),
      checkOutUtc: values.checkOutUtc ? businessInstant(values.checkOutUtc, row.checkOutUtc) : null, reason: values.reason, rowVersion: row.rowVersion }, key);
  }, ["attendance", "period-state"], () => { onNotice("Đã lưu quyết định điều chỉnh công trên máy chủ."); setSelected(undefined); }, `${selected?.mode}:${selected?.row.id}:${selected?.row.rowVersion}`);
  const reload = useMutation({ mutationFn: () => attendanceApi.get(selected!.row.employeeId, selected!.row.businessDate),
    onSuccess: row => { setSelected(previous => previous && { ...previous, row }); form.setFieldsValue({ checkInUtc: businessTimeInput(row.checkInUtc), checkOutUtc: businessTimeInput(row.checkOutUtc), reason: "", minutes: "" }); command.reset(); } });
  const edit = (row: Attendance, mode: "adjust" | "overtime") => { command.reset(); reload.reset(); form.resetFields(); form.setFieldsValue({ checkInUtc: businessTimeInput(row.checkInUtc), checkOutUtc: businessTimeInput(row.checkOutUtc) }); setSelected({ row, mode }); };
  const conflict = command.error instanceof ApiClientError && command.error.isConflict;
  return <>
    {team && <Form name="attendance-filters" layout="inline" className="business-filters" onFinish={v => { setPage(1); setFilters({ employeeId: v.employeeId || undefined, from: v.from || undefined, to: v.to || undefined, status: v.status || undefined }); }}>
      {hasPermission("employees.read") && <Form.Item name="employeeId" label="Nhân viên"><EmployeePicker /></Form.Item>}
      <Form.Item name="from" label="Từ ngày"><Input type="date" /></Form.Item><Form.Item name="to" label="Đến ngày"><Input type="date" /></Form.Item>
      <Form.Item name="status" label="Trạng thái công"><Select allowClear className="status-filter" options={Object.entries(labels).map(([value, label]) => ({ value, label }))} /></Form.Item><Button htmlType="submit">Lọc công</Button>
    </Form>}
    <Feedback error={query.error || names.error} retry={() => { void query.refetch(); void names.refetch(); }} />
    {team && hasPermission("attendance.manage") && !noProfile && <Feedback error={own.error} retry={() => void own.refetch()} />}
    <div className="business-table" role="region" aria-label={team ? "Công trong phạm vi quản lý" : "Lịch sử công của tôi"} tabIndex={0}><Table<Attendance> rowKey="id" dataSource={query.isError ? [] : query.data?.items} loading={query.isFetching} scroll={{ x: team ? 1100 : 850 }} pagination={{ current: page, pageSize: 20, total: query.data?.totalCount, showSizeChanger: false, onChange: setPage }} columns={[
      { title: "Ngày công", dataIndex: "businessDate" }, ...(team ? [{ title: "Nhân viên", render: (_: unknown, row: Attendance) => names.labels.get(row.employeeId) || row.employeeId }] : []),
      { title: "Giờ vào", dataIndex: "checkInUtc", render: time }, { title: "Giờ ra", dataIndex: "checkOutUtc", render: time },
      { title: "Trạng thái", render: (_, row) => <Tag color={row.status === "MissingCheckout" ? "orange" : "blue"}>{labels[row.status] || row.status}</Tag> },
      { title: "Phút công", dataIndex: "eligibleMinutes", render: displayDecimal }, { title: "Muộn / sớm (phút)", render: (_, row) => `${displayDecimal(row.lateMinutes)} / ${displayDecimal(row.earlyLeaveMinutes)}` },
      { title: "OT đã xác nhận (phút)", dataIndex: "confirmedOvertimeMinutes", render: displayDecimal },
      ...(team && hasPermission("attendance.manage") ? [{ title: "Thao tác", render: (_: unknown, row: Attendance) => <RowActions row={row} selfId={own.isError ? undefined : own.data?.id} identityKnown={own.isSuccess || noProfile} onEdit={edit} /> }] : []),
    ]} /></div>
    <Modal open={!!selected} title={`${selected?.mode === "adjust" ? "Điều chỉnh công" : "Xác nhận OT"} · ${selected?.row.businessDate || ""}`} footer={null} destroyOnClose closable={!command.isPending} maskClosable={!command.isPending} onCancel={() => { if (!command.isPending) setSelected(undefined); }}>
      <p>Giờ nhập theo Việt Nam (UTC+07:00). Giờ ngoài ca không tự trở thành OT.</p><PeriodNotice guard={guard} />
      <Feedback error={command.error || reload.error} />
      {conflict && <Alert type="warning" message="Bản nhập được giữ lại" description={<><p>Tải dữ liệu mới trước khi quyết định lại. Thao tác này thay bản nhập bằng dữ liệu mới trên máy chủ.</p><Button loading={reload.isPending} onClick={() => reload.mutate()}>Dùng bản mới của ngày công</Button></>} />}
      <Form name="attendance-adjustment" form={form} layout="vertical" onFinish={v => command.mutate(v)} disabled={command.isPending}>
        {selected?.mode === "adjust" ? <><Form.Item name="checkInUtc" label="Giờ vào thực tế" rules={[{ required: true }]}><Input type="datetime-local" step="0.001" /></Form.Item>
          <Form.Item name="checkOutUtc" label="Giờ ra thực tế (có thể để trống)"><Input type="datetime-local" step="0.001" /></Form.Item></>
          : <Form.Item name="minutes" label="Số phút OT xác nhận" rules={[{ required: true }, { pattern: /^(?:\d+)(?:\.\d+)?$/, message: "Nhập số phút dương, tối đa 1440." }]}><Input inputMode="decimal" maxLength={20} /></Form.Item>}
        <Form.Item name="reason" label="Lý do điều chỉnh" rules={[{ required: true, min: 10, whitespace: true, message: "Nhập lý do ít nhất 10 ký tự." }]}><Input.TextArea maxLength={500} /></Form.Item>
        <Button type="primary" htmlType="submit" loading={command.isPending} disabled={!guard.canWrite || conflict}>Lưu quyết định công</Button>
      </Form>
    </Modal>
  </>;
}

function OwnClock({ onNotice }: { onNotice: (value: string) => void }) {
  const [note, setNote] = useState("");
  const clock = useQuery({ queryKey: ["business-time"], queryFn: ({ signal }) => calendarApi.now(signal), staleTime: 0, retry: false });
  const today = useQuery({ queryKey: ["attendance", "today"], queryFn: ({ signal }) => attendanceApi.today(signal), retry: false });
  const missing = today.error instanceof ApiClientError && today.error.status === 404 && today.error.code === "attendance.not_found";
  const guard = usePeriodGuard(clock.data?.businessDate);
  const check = useCommand(({ mode, note }: { mode: "in" | "out"; note: string }, key) => attendanceApi.check(mode, note, key), ["attendance"], () => onNotice("Đã ghi nhận chấm công theo giờ máy chủ."));
  const ready = guard.canWrite && !check.isPending && (today.isSuccess || missing);
  return <section className="clock-panel"><Typography.Title level={3}>Chấm công hôm nay</Typography.Title>
    <p>Ngày nghiệp vụ: {clock.data?.businessDate || "Đang tải…"} · Giờ máy chủ: {time(clock.data?.serverTimeUtc)}</p>
    <Feedback error={clock.error || (!missing && today.error) || check.error} retry={() => { void today.refetch(); void clock.refetch(); }} /><PeriodNotice guard={guard} />
    <label>Ghi chú chấm công<Input.TextArea value={note} onChange={e => setNote(e.target.value)} maxLength={200} disabled={check.isPending} /></label>
    <Space wrap><Button type="primary" loading={check.isPending && check.variables?.mode === "in"} disabled={!ready || !!today.data} onClick={() => check.mutate({ mode: "in", note })}>Vào ca</Button>
      <Button loading={check.isPending && check.variables?.mode === "out"} disabled={!ready || !today.data || !!today.data.checkOutUtc} onClick={() => check.mutate({ mode: "out", note })}>Ra ca</Button></Space>
    <p>{today.data ? `Giờ vào: ${time(today.data.checkInUtc)} · Giờ ra: ${time(today.data.checkOutUtc)}` : missing ? "Chưa có lần vào ca hôm nay." : "Đang tải trạng thái công."}</p>
  </section>;
}

export function AttendancePage() {
  const { hasPermission } = useAuth();
  const [notice, setNotice] = useState("");
  const canTeam = hasPermission("attendance.read_all");
  return <section className="business-page"><Typography.Title level={2}>Chấm công</Typography.Title><Typography.Text type="secondary">Công tính theo lịch có phiên bản và giờ máy chủ. Dữ liệu kỳ đã khóa chỉ được xem.</Typography.Text>
    {notice && <Alert type="success" showIcon message={notice} closable onClose={() => setNotice("")} />}
    <Tabs defaultActiveKey={canTeam ? "team" : "mine"} destroyInactiveTabPane items={[
      ...(hasPermission("attendance.read_own") ? [{ key: "mine", label: "Công của tôi", children: <><OwnClock onNotice={setNotice} /><AttendanceRegister team={false} onNotice={setNotice} /></> }] : []),
      ...(canTeam ? [{ key: "team", label: "Quản lý công", children: <AttendanceRegister team onNotice={setNotice} /> }] : []),
    ]} />
  </section>;
}
