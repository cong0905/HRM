import { Alert } from "antd";
import { useQuery } from "@tanstack/react-query";
import { hrmClient } from "../../api/client";
import { unwrap } from "./api";
import { Feedback } from "./Feedback";

export function periodMonths(start?: string, end?: string): [number, number][] {
  if (!start || !/^\d{4}-\d{2}-\d{2}$/.test(start) || !/^\d{4}-\d{2}-\d{2}$/.test(end || start)) return [];
  if (start > (end || start) || start.slice(0, 4) !== (end || start).slice(0, 4)) return [];
  const year = Number(start.slice(0, 4)), first = Number(start.slice(5, 7)), last = Number((end || start).slice(5, 7));
  if (year < 1900 || year > 9998 || first < 1 || last > 12) return [];
  return Array.from({ length: last - first + 1 }, (_, i) => [year, first + i]);
}

export function usePeriodGuard(start?: string, end?: string) {
  const months = periodMonths(start, end);
  const query = useQuery({ queryKey: ["period-state", months], enabled: months.length > 0, staleTime: 0, retry: false,
    queryFn: async ({ signal }) => Promise.all(months.map(async ([year, month]) => unwrap(await hrmClient.GET("/api/v1/work-calendars/months/{year}/{month}/editability", { params: { path: { year, month } }, signal })))) });
  const closed = query.data?.some(p => p.isClosed) ?? false;
  return { ...query, closed, canWrite: months.length > 0 && query.isSuccess && !query.isFetching && !closed };
}

export function PeriodNotice({ guard }: { guard: ReturnType<typeof usePeriodGuard> }) {
  if (guard.isError) return <Feedback error={guard.error} retry={() => void guard.refetch()} />;
  if (guard.closed) return <Alert type="warning" showIcon message="Kỳ lương đã công bố hoặc khóa" description="Chỉ được xem dữ liệu. Không thể thay đổi công hoặc phép thuộc kỳ này." />;
  return null;
}
