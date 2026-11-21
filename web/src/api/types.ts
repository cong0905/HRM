import type { components, paths } from "hrm-api-contracts";

export type ProblemDetails = components["schemas"]["ProblemDetails"];
export type DeliveryResponse = components["schemas"]["DeliveryResponse"];

export type CurrentUserAccessResponse = components["schemas"]["CurrentUserAccessResponse"];

export interface CsrfResponse {
  requestToken: string;
}

export class ApiClientError extends Error {
  public readonly status: number;
  public readonly code: string;
  public readonly traceId?: string;
  public readonly problem?: ProblemDetails;
  public readonly isNetworkError: boolean;
  public readonly isConflict: boolean;
  public readonly isUnauthorized: boolean;
  public readonly isForbidden: boolean;
  public readonly isRateLimited: boolean;
  public readonly isCsrfError: boolean;

  constructor(params: {
    message: string;
    status: number;
    code?: string;
    traceId?: string;
    problem?: ProblemDetails;
    isNetworkError?: boolean;
  }) {
    super(params.message);
    this.name = "ApiClientError";
    this.status = params.status;
    // Normalize errorCode from ProblemDetails
    this.code =
      params.code ||
      params.problem?.errorCode ||
      (params.status === 0 ? "network.error" : "unknown.error");
    this.traceId = params.traceId || params.problem?.traceId || undefined;
    this.problem = params.problem;
    this.isNetworkError = Boolean(params.isNetworkError || params.status === 0);
    this.isConflict = params.status === 409;
    this.isUnauthorized = params.status === 401;
    this.isForbidden = params.status === 403;
    this.isRateLimited = params.status === 429;
    this.isCsrfError = params.status === 400 && this.code === "csrf.invalid";
  }
}

export type { paths, components };
