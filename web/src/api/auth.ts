import { hrmClient, refreshCsrfToken, clearCachedCsrfToken } from "./client";
import { ApiClientError, type ProblemDetails, type DeliveryResponse } from "./types";

export interface LoginParams {
  username: string;
  password: string;
}

export interface ForgotPasswordParams {
  email: string;
}

export interface ResetPasswordParams {
  userId: string;
  code: string;
  newPassword: string;
}

export interface ChangePasswordParams {
  currentPassword: string;
  newPassword: string;
}

/**
 * Web cookie-based login.
 * Guarantees CSRF header is attached, and immediately refreshes CSRF token upon 204
 * to attach to the authenticated session.
 */
export async function loginApi(params: LoginParams): Promise<void> {
  const result = await hrmClient.POST("/api/v1/auth/web/login", {
    body: {
      username: params.username,
      password: params.password,
    },
  });

  if (result.error) {
    const status = result.response.status;
    const problem = result.error as ProblemDetails & { code?: string };
    const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
    const errCode = problem?.errorCode || problem?.code || (status === 401 ? "auth.invalid" : undefined);
    throw new ApiClientError({
      message: problem?.detail || (status === 401 ? "Tên đăng nhập hoặc mật khẩu không chính xác." : "Đăng nhập không thành công."),
      status,
      code: errCode,
      traceId: traceId || undefined,
      problem,
    });
  }

  // Refresh CSRF token immediately after sign-in to attach to the authenticated session
  await refreshCsrfToken();
}

/**
 * Logout the current web session.
 * Propagates server errors (e.g. 503) so callers know if server failed to revoke session.
 * 401 is treated as idempotent since session is already gone.
 */
export async function logoutApi(): Promise<void> {
  try {
    const result = await hrmClient.POST("/api/v1/auth/logout");
    if (result.error && result.response.status !== 401) {
      const problem = result.error as ProblemDetails & { code?: string };
      const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
      const errCode = problem?.errorCode || problem?.code || "auth.logout_failed";
      throw new ApiClientError({
        message: problem?.detail || "Máy chủ không thể hoàn tất quá trình đăng xuất.",
        status: result.response.status,
        code: errCode,
        traceId: traceId || undefined,
        problem,
      });
    }
  } finally {
    clearCachedCsrfToken();
  }
}

/**
 * Request password reset instructions.
 * Returns the actual deliveryMode from backend.
 */
export async function forgotPasswordApi(params: ForgotPasswordParams): Promise<DeliveryResponse> {
  const result = await hrmClient.POST("/api/v1/auth/forgot-password", {
    body: {
      email: params.email,
    },
  });

  if (result.error) {
    const status = result.response.status;
    const problem = result.error as ProblemDetails & { code?: string };
    const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
    const errCode = problem?.errorCode || problem?.code;
    throw new ApiClientError({
      message: problem?.detail || "Không thể gửi yêu cầu đặt lại mật khẩu.",
      status,
      code: errCode,
      traceId: traceId || undefined,
      problem,
    });
  }

  const data = result.data as DeliveryResponse;
  return {
    deliveryMode: data?.deliveryMode || "unknown",
    message: data?.message || "Yêu cầu đã được tiếp nhận.",
  };
}

/**
 * Reset password using verification code and user ID.
 */
export async function resetPasswordApi(params: ResetPasswordParams): Promise<void> {
  const result = await hrmClient.POST("/api/v1/auth/reset-password", {
    body: {
      userId: params.userId,
      code: params.code,
      newPassword: params.newPassword,
    },
  });

  if (result.error) {
    const status = result.response.status;
    const problem = result.error as ProblemDetails & { code?: string };
    const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
    const errCode = problem?.errorCode || problem?.code || (status === 400 ? "auth.reset_invalid" : undefined);
    throw new ApiClientError({
      message: problem?.detail || (status === 400 ? "Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." : "Đặt lại mật khẩu thất bại."),
      status,
      code: errCode,
      traceId: traceId || undefined,
      problem,
    });
  }
}

/**
 * Activate new account using activation code.
 */
export async function activateAccountApi(params: ResetPasswordParams): Promise<void> {
  const result = await hrmClient.POST("/api/v1/auth/activate", {
    body: {
      userId: params.userId,
      code: params.code,
      newPassword: params.newPassword,
    },
  });

  if (result.error) {
    const status = result.response.status;
    const problem = result.error as ProblemDetails & { code?: string };
    const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
    const errCode = problem?.errorCode || problem?.code || (status === 400 ? "auth.activation_invalid" : undefined);
    throw new ApiClientError({
      message: problem?.detail || (status === 400 ? "Mã kích hoạt không hợp lệ hoặc đã hết hạn." : "Kích hoạt tài khoản thất bại."),
      status,
      code: errCode,
      traceId: traceId || undefined,
      problem,
    });
  }
}

/**
 * Change password for currently authenticated user.
 */
export async function changePasswordApi(params: ChangePasswordParams): Promise<void> {
  const result = await hrmClient.POST("/api/v1/auth/change-password", {
    body: {
      currentPassword: params.currentPassword,
      newPassword: params.newPassword,
    },
  });

  if (result.error) {
    const status = result.response.status;
    const problem = result.error as ProblemDetails & { code?: string };
    const traceId = result.response.headers.get("x-trace-id") || problem?.traceId;
    const errCode = problem?.errorCode || problem?.code || (status === 400 ? "auth.password_invalid" : undefined);
    throw new ApiClientError({
      message: problem?.detail || "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.",
      status,
      code: errCode,
      traceId: traceId || undefined,
      problem,
    });
  }

  clearCachedCsrfToken();
}
