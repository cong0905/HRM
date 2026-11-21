import { useQuery } from "@tanstack/react-query";
import { hrmClient } from "../../api/client";
import { useAuth } from "../../context/AuthContext";
import { unwrap } from "./api";

/** Resolve only employee IDs on the current server page, under the existing bulk-read scope checks. */
export function useEmployeeLabels(employeeIds: string[]) {
  const { hasAnyPermission } = useAuth();
  const ids = [...new Set(employeeIds)].sort();
  const allowed = hasAnyPermission(["employees.read", "payroll.read_all"]);
  const query = useQuery({ queryKey: ["employee-labels", ids], enabled: ids.length > 0 && allowed, staleTime: 0, retry: false,
    queryFn: async ({ signal }) => unwrap(await hrmClient.POST("/api/v1/employees/bulk", { body: { employeeIds: ids }, signal })) });
  return { ...query, labels: new Map(query.isError ? [] : query.data?.map(row => [row.id, `${row.fullName} · ${row.employeeCode}`] as const)) };
}
