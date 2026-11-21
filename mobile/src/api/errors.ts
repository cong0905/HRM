import type { ProblemDetails } from "./types";

/**
 * Lớp lỗi chuẩn hóa cho toàn bộ client API Mobile.
 * Phân tích và trích xuất ProblemDetails, errorCode, traceId, và cờ trạng thái.
 */
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
  }
}

/**
 * Chuyển đổi mã lỗi hệ thống sang thông điệp tiếng Việt thân thiện, rõ nghĩa.
 */
export function getFriendlyErrorMessage(error: unknown, fallback = "Đã xảy ra lỗi không xác định."): {
  message: string;
  traceId?: string;
  code?: string;
  isNetworkError: boolean;
  isRateLimited: boolean;
} {
  if (error instanceof ApiClientError) {
    let friendly = error.message;

    switch (error.code) {
      case "auth.invalid":
        friendly = "Tên đăng nhập hoặc mật khẩu không chính xác.";
        break;
      case "auth.rate_limited":
        friendly = "Bạn đã thao tác quá nhiều lần liên tiếp. Vui lòng chờ 60 giây trước khi thử lại.";
        break;
      case "auth.reset_invalid":
        friendly = "Mã xác thực đặt lại mật khẩu không hợp lệ hoặc đã hết hiệu lực.";
        break;
      case "auth.activation_invalid":
        friendly = "Mã xác thực kích hoạt tài khoản không hợp lệ hoặc đã hết hiệu lực.";
        break;
      case "auth.password_invalid":
        friendly = "Mật khẩu hiện tại không chính xác hoặc mật khẩu mới chưa thỏa mãn yêu cầu bảo mật.";
        break;
      case "authentication.required":
      case "auth.unauthorized":
      case "auth.session_expired":
        friendly = "Phiên làm việc đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.";
        break;
      case "authorization.forbidden":
        friendly = "Bạn không có quyền thực hiện thao tác này.";
        break;
      case "network.offline":
      case "network.error":
        friendly = "Lỗi kết nối mạng. Vui lòng kiểm tra kết nối Internet của thiết bị.";
        break;
      case "concurrency.conflict":
        friendly = error.problem?.detail || "Dữ liệu đang bị xung đột hoặc đã được cập nhật ở nơi khác.";
        break;
    }

    if (error.status === 409 && error.problem?.detail) {
      friendly = error.problem.detail;
    }

    return {
      message: friendly,
      traceId: error.traceId,
      code: error.code,
      isNetworkError: error.isNetworkError,
      isRateLimited: error.isRateLimited,
    };
  }

  if (error instanceof Error) {
    return {
      message: error.message || fallback,
      isNetworkError: false,
      isRateLimited: false,
    };
  }

  return {
    message: fallback,
    isNetworkError: false,
    isRateLimited: false,
  };
}
