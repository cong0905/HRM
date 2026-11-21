import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Alert, Input, Typography } from "antd";
import { calendarApi } from "../shared/api";
import { Feedback } from "../shared/Feedback";

export function LeaveCalendar({ start, end }: { start: string; end: string }) {
  const [chosen, setChosen] = useState("");
  const month = chosen || start.slice(0, 7);
  const [year = 0, number = 0] = month.split("-").map(Number);
  const query = useQuery({ queryKey: ["leave-calendar", month], queryFn: ({ signal }) => calendarApi.month(year, number, signal), enabled: !!month, retry: false });
  return <section aria-label="Lịch ngày nghỉ"><Typography.Title level={4}>Lịch ngày nghỉ</Typography.Title>
    <label>Tháng xem lịch<Input type="month" value={month} onChange={e => setChosen(e.target.value)} /></label>
    <p>Khoảng đơn: {start} → {end}. Ngày được tính phép theo lịch máy chủ; không phải mọi ngày trong khoảng đều trừ quỹ.</p>
    <Feedback error={query.error} retry={() => void query.refetch()} />
    {query.isFetching && <p role="status">Đang tải lịch…</p>}
    {query.data && !query.isError && <div className="leave-calendar-grid">{query.data.days.map(day => {
      const requested = day.date >= start && day.date <= end;
      const working = Number(day.standardMinutes) > 0;
      return <div key={day.date} className={`leave-calendar-day ${requested ? "requested" : ""}`}><time dateTime={day.date}>{day.date}</time>
        <span>{requested ? working ? "Ngày phép theo lịch" : "Trong đơn · ngày nghỉ" : working ? "Ngày làm việc" : "Ngày nghỉ"}</span></div>;
    })}</div>}
    <Alert type="info" message="Lịch này mô tả ngày trong khoảng đơn đã chọn, không bao gồm đơn của người khác." />
  </section>;
}
