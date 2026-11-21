import { hrmClient, resolveApiUrl, secureFetch } from "../../api/client";
import { ApiClientError, type ProblemDetails } from "../../api/types";
import { unwrap, type Schema } from "../shared/api";

export type PayrollWorkflowState = Schema<"PayrollWorkflowState">;
export type PayrollWorkflowRequest = Schema<"PayrollWorkflowRequest">;
export type CreatePayrollPeriodRequest = Schema<"CreatePayrollPeriodRequest">;
export type PayrollBatchResponse = Schema<"PayrollBatchResponse">;
export type CapturePayrollRequest = Schema<"CapturePayrollRequest">;
export type PayrollSnapshotResponse = Schema<"PayrollSnapshotResponse">;
export type PayrollCalculationOutput = Schema<"PayrollCalculationOutput">;
export type PayrollAdjustmentResponse = Schema<"PayrollAdjustmentResponse">;
export type AddPayrollAdjustmentRequest = Schema<"AddPayrollAdjustmentRequest">;
export type VoidPayrollAdjustmentRequest = Schema<"VoidPayrollAdjustmentRequest">;
export type SalaryHistoryResponse = Schema<"SalaryHistoryResponse">;
export type AppendSalaryRequest = Schema<"AppendSalaryRequest">;
export type PayslipResponse = Schema<"PayslipResponse">;
export type PayslipDetailResponse = Schema<"PayslipDetailResponse">;
export type PayrollReconciliationResponse = Schema<"PayrollReconciliationResponse">;
export type PagedResultOfPayslipResponse = Schema<"PagedResultOfPayslipResponse">;

export type PagedPayrollPeriods = Schema<"PagedResultOfPayrollWorkflowState">;

export const payrollApi = {
  // Periods queries & creation
  listPeriods: async (
    query: { page: number; pageSize: number; year?: number; status?: string },
    signal?: AbortSignal
  ): Promise<PagedPayrollPeriods> => {
    return unwrap(await hrmClient.GET("/api/v1/payroll-periods", { params: { query }, signal }));
  },

  getPeriodState: async (id: string, signal?: AbortSignal): Promise<PayrollWorkflowState> =>
    unwrap(await hrmClient.GET("/api/v1/payroll-periods/{id}", { params: { path: { id } }, signal })),

  createPeriod: async (body: CreatePayrollPeriodRequest, key: string): Promise<PayrollWorkflowState> =>
    unwrap(
      await hrmClient.POST("/api/v1/payroll-periods", {
        params: { header: { "Idempotency-Key": key } },
        body,
      })
    ),

  // Workflow mutations
  executeWorkflow: async (
    id: string,
    operation: "calculate" | "approve" | "publish" | "lock",
    body: PayrollWorkflowRequest,
    key: string
  ): Promise<void> => {
    const response = await hrmClient.POST("/api/v1/payroll-periods/{id}/workflow/{operation}", {
      params: { path: { id, operation }, header: { "Idempotency-Key": key } },
      body,
    });
    if (!response.response.ok) {
      const problem = response.error as ProblemDetails | undefined;
      throw new ApiClientError({
        status: response.response.status,
        problem,
        message: problem?.detail || problem?.title || `Không thể thực hiện thao tác ${operation}.`,
        traceId: response.response.headers.get("x-trace-id") || problem?.traceId || undefined,
      });
    }
  },

  // Snapshots & Calculation
  getSnapshot: async (
    periodId: string,
    version?: number,
    signal?: AbortSignal
  ): Promise<PayrollBatchResponse> =>
    unwrap(
      await hrmClient.GET("/api/v1/payroll-periods/{periodId}/snapshots", {
        params: { path: { periodId }, query: version ? { version } : {} },
        signal,
      })
    ),

  captureSnapshot: async (
    periodId: string,
    body: CapturePayrollRequest,
    key: string
  ): Promise<PayrollBatchResponse> =>
    unwrap(
      await hrmClient.POST("/api/v1/payroll-periods/{periodId}/snapshots", {
        params: { path: { periodId }, header: { "Idempotency-Key": key } },
        body,
      })
    ),

  // Items & Reconciliation
  getItems: async (
    id: string,
    page: number,
    pageSize = 20,
    signal?: AbortSignal
  ): Promise<PagedResultOfPayslipResponse> =>
    unwrap(
      await hrmClient.GET("/api/v1/payroll-periods/{id}/items", {
        params: { path: { id }, query: { page, pageSize } },
        signal,
      })
    ),

  reconcile: async (id: string, signal?: AbortSignal): Promise<PayrollReconciliationResponse> =>
    unwrap(
      await hrmClient.GET("/api/v1/payroll-periods/{id}/reconciliation", {
        params: { path: { id } },
        signal,
      })
    ),

  exportCsv: async (id: string, maxRows = 500, signal?: AbortSignal) => {
    const url = resolveApiUrl(`/api/v1/payroll-periods/${encodeURIComponent(id)}/export?maxRows=${maxRows}`);
    const response = await secureFetch(url, { method: "GET", signal });
    if (!response.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = (await response.json()) as ProblemDetails;
      } catch {
        // response was not JSON ProblemDetails
      }
      throw new ApiClientError({
        status: response.status,
        problem,
        message: problem?.detail || problem?.title || "Không thể xuất file bảng lương CSV.",
        traceId: response.headers.get("x-trace-id") || problem?.traceId || undefined,
      });
    }
    const contentDisposition = response.headers.get("content-disposition");
    let fileName = "payroll-export.csv";
    if (contentDisposition) {
      const match = contentDisposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)["']?/i);
      if (match && match[1]) {
        fileName = decodeURIComponent(match[1]);
      }
    }
    const blob = await response.blob();
    return { blob, fileName };
  },

  // Manual Adjustments
  getAdjustments: async (periodId: string, signal?: AbortSignal): Promise<PayrollAdjustmentResponse[]> =>
    unwrap(
      await hrmClient.GET("/api/v1/payroll-periods/{periodId}/adjustments", {
        params: { path: { periodId } },
        signal,
      })
    ),

  addAdjustment: async (
    periodId: string,
    body: AddPayrollAdjustmentRequest,
    key: string
  ): Promise<PayrollAdjustmentResponse> =>
    unwrap(
      await hrmClient.POST("/api/v1/payroll-periods/{periodId}/adjustments", {
        params: { path: { periodId }, header: { "Idempotency-Key": key } },
        body,
      })
    ),

  voidAdjustment: async (
    id: string,
    body: VoidPayrollAdjustmentRequest,
    key: string
  ): Promise<PayrollAdjustmentResponse> =>
    unwrap(
      await hrmClient.POST("/api/v1/payroll-adjustments/{id}/void", {
        params: { path: { id }, header: { "Idempotency-Key": key } },
        body,
      })
    ),

  // Salary History
  getSalaryHistory: async (employeeId: string, signal?: AbortSignal): Promise<SalaryHistoryResponse[]> =>
    unwrap(
      await hrmClient.GET("/api/v1/employees/{employeeId}/salary-history", {
        params: { path: { employeeId } },
        signal,
      })
    ),

  appendSalary: async (
    employeeId: string,
    body: AppendSalaryRequest,
    key: string
  ): Promise<SalaryHistoryResponse> =>
    unwrap(
      await hrmClient.POST("/api/v1/employees/{employeeId}/salary-history", {
        params: { path: { employeeId }, header: { "Idempotency-Key": key } },
        body,
      })
    ),

  // Employee Own Payslips
  myPayslips: async (
    query: { page: number; pageSize: number },
    signal?: AbortSignal
  ): Promise<PagedResultOfPayslipResponse> =>
    unwrap(await hrmClient.GET("/api/v1/me/payslips", { params: { query }, signal })),

  myPayslipDetail: async (id: string, signal?: AbortSignal): Promise<PayslipDetailResponse> =>
    unwrap(await hrmClient.GET("/api/v1/me/payslips/{id}", { params: { path: { id } }, signal })),

  // Admin Payslip Detail
  payslipDetail: async (id: string, signal?: AbortSignal): Promise<PayslipDetailResponse> =>
    unwrap(await hrmClient.GET("/api/v1/payslips/{id}", { params: { path: { id } }, signal })),
};
