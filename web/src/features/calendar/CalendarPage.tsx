import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Checkbox, Form, Input, Space, Table, Typography } from "antd";
import { useAuth } from "../../context/AuthContext";
import { calendarApi, type Schema } from "../shared/api";
import { Feedback } from "../shared/Feedback";

const { Title, Text } = Typography;
type Definition = Schema<"WorkCalendarDefinition">;
type CalendarForm = { effectiveFrom: string; fixtureId: string; shiftStart: string; shiftEnd: string; breaks: { start: string; end: string }[]; workingDays: Definition["workingDays"]; holidays: string; reason: string };
const weekdays: Definition["workingDays"] = [1, 2, 3, 4, 5, 6, 0];
const labels = ["Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy", "Chủ nhật"];
const definition = (v: CalendarForm): Definition => ({ fixtureId: v.fixtureId, timeZone: "Asia/Ho_Chi_Minh", shiftStart: v.shiftStart.length === 5 ? v.shiftStart + ":00" : v.shiftStart,
  shiftEnd: v.shiftEnd.length === 5 ? v.shiftEnd + ":00" : v.shiftEnd, workingDays: v.workingDays,
  breaks: (v.breaks || []).map(b => ({ start: b.start.length === 5 ? b.start + ":00" : b.start, end: b.end.length === 5 ? b.end + ":00" : b.end })),
  holidays: v.holidays.split(/[\s,]+/).filter(Boolean) });

function CalendarTable({ data }: { data: Schema<"CalendarMonthResponse"> }) {
  return <><p><strong>{data.standardDays}</strong> ngày làm việc · <strong>{data.standardMinutes}</strong> phút tiêu chuẩn</p>
    <div className="business-table" role="region" aria-label="Ngày và giờ làm việc" tabIndex={0}><Table rowKey="date" dataSource={data.days} size="small" scroll={{ x: 620 }} pagination={{ pageSize: 10, showSizeChanger: false }} columns={[
      { title: "Ngày", dataIndex: "date" }, { title: "Phút tiêu chuẩn", dataIndex: "standardMinutes" },
      { title: "Ca làm", render: (_, d) => `${d.definition.shiftStart} – ${d.definition.shiftEnd}` },
      { title: "Phiên bản", render: (_, d) => <span title={d.versionId}>{d.definition.fixtureId}</span> },
    ]} /></div></>;
}

export function CalendarPage() {
  const { hasPermission } = useAuth();
  const client = useQueryClient();
  const [month, setMonth] = useState("");
  const [notice, setNotice] = useState("");
  const [form] = Form.useForm<CalendarForm>();
  const now = useQuery({ queryKey: ["business-time"], queryFn: ({ signal }) => calendarApi.now(signal), retry: false });
  const chosenMonth = month || now.data?.businessDate.slice(0, 7) || "";
  const [year = 0, monthNumber = 0] = chosenMonth.split("-").map(Number);
  const current = useQuery({ queryKey: ["calendar", chosenMonth], queryFn: ({ signal }) => calendarApi.month(year, monthNumber, signal), enabled: !!chosenMonth, retry: false });
  const preview = useMutation({ mutationFn: async () => {
    const values = await form.validateFields();
    const [y = 0, m = 0] = values.effectiveFrom.split("-").map(Number);
    const request = { year: y, month: m, definition: definition(values) };
    return { data: await calendarApi.preview(request), values, fingerprint: JSON.stringify(values) };
  } });
  const save = useMutation({ mutationFn: async () => {
    const values = await form.validateFields();
    if (!preview.data || preview.data.fingerprint !== JSON.stringify(values)) throw new Error("Xem trước lại sau khi thay đổi biểu mẫu.");
    return calendarApi.create({ effectiveFrom: values.effectiveFrom, definition: definition(values), reason: values.reason });
  }, onSuccess: result => { setNotice(`Đã tạo phiên bản ${result.definition.fixtureId}, áp dụng từ ${result.effectiveFrom}.`); preview.reset(); void client.invalidateQueries({ queryKey: ["calendar"] }); } });
  return <section className="business-page"><Title level={2}>Lịch làm việc</Title><Text type="secondary">Múi giờ Asia/Ho_Chi_Minh · ngày nghiệp vụ theo máy chủ {now.data?.businessDate || "—"}</Text>
    <Feedback error={now.error} retry={() => void now.refetch()} />
    <div className="business-filters"><label>Tháng áp dụng <Input aria-label="Tháng áp dụng" type="month" min="1900-01" max="9998-12" value={chosenMonth} onChange={e => setMonth(e.target.value)} /></label></div>
    <Feedback error={current.error} retry={() => void current.refetch()} />
    {current.isFetching && <p role="status">Đang tải lịch…</p>}{current.data && !current.isError && <CalendarTable data={current.data} />}
    {hasPermission("calendar.manage") && <section className="calendar-editor"><Title level={3}>Tạo phiên bản lịch</Title>
      <Alert type="info" showIcon message="Lịch đã lưu được giữ nguyên" description="Tạo phiên bản có ngày bắt đầu mới. Máy chủ sẽ từ chối thay đổi ảnh hưởng kỳ lương đã công bố hoặc khóa." />
      {notice && <Alert type="success" message={notice} showIcon />}
      <Form form={form} layout="vertical" onValuesChange={() => { preview.reset(); save.reset(); }} initialValues={{ breaks: [], holidays: "", workingDays: [] }} onFinish={() => preview.mutate()} disabled={save.isPending || preview.isPending}>
        <div className="form-grid"><Form.Item name="fixtureId" label="Tên phiên bản" rules={[{ required: true, whitespace: true }]}><Input maxLength={100} /></Form.Item>
          <Form.Item name="effectiveFrom" label="Áp dụng từ ngày" rules={[{ required: true }]}><Input type="date" min="1900-01-01" max="9998-12-31" /></Form.Item>
          <Form.Item name="shiftStart" label="Giờ bắt đầu" rules={[{ required: true }]}><Input type="time" /></Form.Item>
          <Form.Item name="shiftEnd" label="Giờ kết thúc" rules={[{ required: true }]}><Input type="time" /></Form.Item></div>
        <Form.Item name="workingDays" label="Ngày làm việc trong tuần" rules={[{ required: true, type: "array", min: 1 }]}><Checkbox.Group options={weekdays.map((value, i) => ({ value, label: labels[i] }))} /></Form.Item>
        <Form.List name="breaks">{(items, { add, remove }) => <><Title level={5}>Giờ nghỉ trong ca</Title>{items.map(item => <Space key={item.key} align="start" wrap>
          <Form.Item name={[item.name, "start"]} label="Nghỉ từ" rules={[{ required: true }]}><Input type="time" /></Form.Item>
          <Form.Item name={[item.name, "end"]} label="Nghỉ đến" rules={[{ required: true }]}><Input type="time" /></Form.Item><Button onClick={() => remove(item.name)}>Xóa giờ nghỉ</Button>
        </Space>)}<Button onClick={() => add()} disabled={items.length >= 8}>Thêm giờ nghỉ</Button></>}</Form.List>
        <Form.Item name="holidays" label="Ngày nghỉ lễ" extra="Nhập ngày dạng YYYY-MM-DD, mỗi ngày một dòng." rules={[{ validator: (_, value: string) => !value || value.split(/[\s,]+/).filter(Boolean).every(s => /^\d{4}-\d{2}-\d{2}$/.test(s)) ? Promise.resolve() : Promise.reject(new Error("Nhập ngày dạng YYYY-MM-DD.")) }]}><Input.TextArea rows={3} /></Form.Item>
        <Form.Item name="reason" label="Lý do tạo phiên bản" rules={[{ required: true, whitespace: true }]}><Input.TextArea maxLength={500} /></Form.Item>
        <Feedback error={preview.error || save.error} />
        <Space wrap><Button htmlType="submit" loading={preview.isPending}>Xem trước trên máy chủ</Button><Button type="primary" disabled={!preview.data} loading={save.isPending} onClick={() => save.mutate()}>Lưu phiên bản lịch</Button></Space>
      </Form>
      {preview.data && <section aria-label="Xem trước lịch đề xuất"><Title level={4}>Xem trước phiên bản đề xuất</Title><p>Toàn bộ tháng giả định dùng phiên bản này. Khi lưu, ngày trước ngày áp dụng vẫn giữ lịch cũ.</p><CalendarTable data={preview.data.data} /></section>}
    </section>}
  </section>;
}
