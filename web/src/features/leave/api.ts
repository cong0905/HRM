import { hrmClient } from "../../api/client";
import { unwrap, type Schema } from "../shared/api";

export type Leave = Schema<"LeaveRecordResponse">;
export const leaveApi = {
  mine: async (page: number, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/me/leave-requests", { params: { query: { page, pageSize: 20 } }, signal })),
  team: async (query: { page: number; pageSize: number; employeeId?: string; year?: number; status?: string }, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/leave-requests", { params: { query }, signal })),
  ownBalances: async (year: number, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/me/leave-balances", { params: { query: { year } }, signal })),
  balances: async (employeeId: string, year: number, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/leave-balances", { params: { query: { employeeId, year } }, signal })),
  create: async (body: Schema<"CreateLeaveRequest">, key: string) => unwrap(await hrmClient.POST("/api/v1/leave-requests", { params: { header: { "Idempotency-Key": key } }, body })),
  get: async (id: string) => unwrap(await hrmClient.GET("/api/v1/leave-requests/{id}", { params: { path: { id } } })),
  decide: async (mode: "approve" | "reject" | "cancel", id: string, body: Schema<"ApproveLeaveRequest">, key: string) => {
    const params = { path: { id }, header: { "Idempotency-Key": key } };
    if (mode === "approve") return unwrap(await hrmClient.POST("/api/v1/leave-requests/{id}/approve", { params, body }));
    if (mode === "reject") return unwrap(await hrmClient.POST("/api/v1/leave-requests/{id}/reject", { params, body }));
    return unwrap(await hrmClient.POST("/api/v1/leave-requests/{id}/cancel", { params, body }));
  },
};
