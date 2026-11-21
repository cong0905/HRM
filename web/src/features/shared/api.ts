import { hrmClient } from "../../api/client";
import { ApiClientError, type components, type ProblemDetails } from "../../api/types";

export type Schema<K extends keyof components["schemas"]> = components["schemas"][K];
export type Employee = Schema<"EmployeeProfileResponse">;
export type EmployeePatch = Schema<"UpdateEmployeeRequest">;

export function unwrap<T>(result: { data?: T; error?: unknown; response: Response }): T {
  if (!result.response.ok) {
    const problem = result.error as ProblemDetails | undefined;
    throw new ApiClientError({ status: result.response.status, problem,
      message: problem?.detail || problem?.title || "Không thể hoàn tất yêu cầu.",
      traceId: result.response.headers.get("x-trace-id") || problem?.traceId || undefined });
  }
  return result.data as T;
}

export const personnelApi = {
  list: async (query: { page: number; pageSize: number; name?: string; departmentId?: string; status?: string }, signal?: AbortSignal) => {
    const cleanParams: {
      page: number;
      pageSize: number;
      name?: string;
      departmentId?: string;
      status?: string;
    } = {
      page: query.page,
      pageSize: query.pageSize,
    };
    if (query.name && query.name.trim()) cleanParams.name = query.name.trim();
    if (query.departmentId) cleanParams.departmentId = query.departmentId;
    if (query.status && (query.status === "DangLamViec" || query.status === "DaNghiViec")) {
      cleanParams.status = query.status;
    }
    return unwrap(await hrmClient.GET("/api/v1/employees", { params: { query: cleanParams }, signal }));
  },
  get: async (id: string, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/employees/{id}", { params: { path: { id } }, signal })),
  me: async (signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/me/profile", { signal })),
  create: async (body: Schema<"CreateEmployeeRequest">) => unwrap(await hrmClient.POST("/api/v1/employees", { body })),
  update: async (id: string, body: EmployeePatch) => unwrap(await hrmClient.PATCH("/api/v1/employees/{id}", { params: { path: { id } }, body })),
  offboard: async (id: string, body: Schema<"OffboardEmployeeRequest">) => unwrap(await hrmClient.POST("/api/v1/employees/{id}/offboard", { params: { path: { id } }, body })),
};

const editableFields = ["fullName", "phone", "address", "dateOfBirth", "gender", "maritalStatus", "departmentId", "positionId", "hireDate"] as const;
export function employeeChanges(before: Employee, values: Partial<Employee>, reason: string): EmployeePatch {
  const changes: Record<string, unknown> = { reason: reason.trim(), rowVersion: before.rowVersion };
  for (const key of editableFields) {
    const val = values[key];
    if (val !== undefined) {
      const normalizedVal = typeof val === "string" ? val.trim() : val;
      const beforeVal = typeof before[key] === "string" ? (before[key] as string).trim() : before[key];
      if (normalizedVal !== beforeVal) {
        changes[key] = normalizedVal === "" && (key === "maritalStatus" || key === "departmentId" || key === "positionId") ? null : normalizedVal;
      }
    }
  }
  return changes as EmployeePatch;
}

export const organizationApi = {
  list: async (kind: "departments" | "positions", page: number, signal?: AbortSignal) => kind === "departments"
    ? unwrap(await hrmClient.GET("/api/v1/departments", { params: { query: { page, pageSize: 20 } }, signal }))
    : unwrap(await hrmClient.GET("/api/v1/positions", { params: { query: { page, pageSize: 20 } }, signal })),
  save: async (kind: "departments" | "positions", id: string | undefined, body: Schema<"OrganizationWriteRequest">) => {
    if (kind === "departments") return id
      ? unwrap(await hrmClient.PUT("/api/v1/departments/{id}", { params: { path: { id } }, body }))
      : unwrap(await hrmClient.POST("/api/v1/departments", { body }));
    return id ? unwrap(await hrmClient.PUT("/api/v1/positions/{id}", { params: { path: { id } }, body }))
      : unwrap(await hrmClient.POST("/api/v1/positions", { body }));
  },
  remove: async (kind: "departments" | "positions", id: string, body: Schema<"OrganizationDeleteRequest">) => kind === "departments"
    ? unwrap(await hrmClient.DELETE("/api/v1/departments/{id}", { params: { path: { id } }, body }))
    : unwrap(await hrmClient.DELETE("/api/v1/positions/{id}", { params: { path: { id } }, body })),
};

export const calendarApi = {
  now: async (signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/work-calendars/now", { signal })),
  month: async (year: number, month: number, signal?: AbortSignal) => unwrap(await hrmClient.GET("/api/v1/work-calendars/months/{year}/{month}", { params: { path: { year, month } }, signal })),
  preview: async (body: Schema<"PreviewCalendarRequest">) => unwrap(await hrmClient.POST("/api/v1/work-calendars/preview", { body })),
  create: async (body: Schema<"CreateCalendarVersionRequest">) => unwrap(await hrmClient.POST("/api/v1/work-calendars", { body })),
};
