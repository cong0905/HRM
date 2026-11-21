import { hrmClient } from "../../api/client";
import { unwrap, type Schema } from "../shared/api";

export type Attendance = Schema<"AttendanceDayResponse">;
export const attendanceApi = {
  mine: async (page: number, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/attendance/me/history", { params: { query: { page, pageSize: 20 } }, signal })),
  today: async (signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/attendance/me/today", { signal })),
  team: async (query: { page: number; pageSize: number; employeeId?: string; from?: string; to?: string; status?: string }, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/attendance", { params: { query }, signal })),
  get: async (employeeId: string, day: string) => unwrap(await hrmClient.GET("/api/v1/attendance/employees/{employeeId}/days/{day}", { params: { path: { employeeId, day } } })),
  check: async (mode: "in" | "out", note: string, key: string) => mode === "in"
    ? unwrap(await hrmClient.POST("/api/v1/attendance/me/check-in", { params: { header: { "Idempotency-Key": key } }, body: { note } }))
    : unwrap(await hrmClient.POST("/api/v1/attendance/me/check-out", { params: { header: { "Idempotency-Key": key } }, body: { note } })),
  adjust: async (row: Attendance, body: Schema<"AdjustAttendanceRequest">, key: string) => unwrap(await hrmClient.POST("/api/v1/attendance/employees/{employeeId}/days/{day}/adjust", { params: { path: { employeeId: row.employeeId, day: row.businessDate }, header: { "Idempotency-Key": key } }, body })),
  overtime: async (row: Attendance, body: Schema<"ConfirmOvertimeRequest">, key: string) => unwrap(await hrmClient.POST("/api/v1/attendance/employees/{employeeId}/days/{day}/overtime", { params: { path: { employeeId: row.employeeId, day: row.businessDate }, header: { "Idempotency-Key": key } }, body })),
};

export function businessTimeInput(instant?: string | null): string {
  return instant ? new Date(Date.parse(instant) + 7 * 60 * 60 * 1000).toISOString().slice(0, 23) : "";
}
export function businessInstant(value: string, original?: string | null): string {
  if (original && value === businessTimeInput(original)) return original;
  return value + "+07:00";
}
