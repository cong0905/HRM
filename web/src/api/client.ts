import { createHrmClient } from "hrm-api-contracts";
import { ApiClientError, type ProblemDetails, type CurrentUserAccessResponse } from "./types";

let memoryCsrfToken: string | null = null;

export function getCachedCsrfToken(): string | null {
  return memoryCsrfToken;
}

export function setCachedCsrfToken(token: string | null): void {
  memoryCsrfToken = token;
}

export function clearCachedCsrfToken(): void {
  memoryCsrfToken = null;
}

/**
 * Returns the base origin for API requests.
 * Uses window.location.origin in browsers/jsdom to guarantee same-origin requests,
 * or fallback to http://localhost:3000 to prevent TypeError "Invalid URL" in Node/undici.
 */
export function getBaseUrl(): string {
  if (typeof window !== "undefined" && window.location?.origin && window.location.origin !== "null") {
    return window.location.origin;
  }
  return "http://localhost:3000";
}

/**
 * Resolves a relative URL with the application base origin.
 */
export function resolveApiUrl(path: string): string {
  if (path.startsWith("/")) {
    return `${getBaseUrl()}${path}`;
  }
  return path;
}

/**
 * Fetch and cache a fresh CSRF token from GET /api/v1/auth/csrf.
 */
export async function refreshCsrfToken(): Promise<string> {
  const url = resolveApiUrl("/api/v1/auth/csrf");
  try {
    const response = await fetch(url, {
      method: "GET",
      credentials: "include",
      headers: {
        Accept: "application/json",
      },
    });

    if (!response.ok) {
      throw new ApiClientError({
        message: "Không thể lấy mã bảo mật CSRF từ hệ thống.",
        status: response.status,
        code: "csrf.fetch_failed",
      });
    }

    const data = (await response.json()) as { requestToken: string };
    if (!data.requestToken) {
      throw new ApiClientError({
        message: "Máy chủ không trả về mã CSRF hợp lệ.",
        status: 500,
        code: "csrf.empty_token",
      });
    }

    memoryCsrfToken = data.requestToken;
    return memoryCsrfToken;
  } catch (error) {
    if (error instanceof ApiClientError) throw error;
    throw new ApiClientError({
      message: "Không thể kết nối đến máy chủ để lấy mã bảo mật.",
      status: 0,
      code: "network.error",
      isNetworkError: true,
    });
  }
}

/**
 * Custom fetch implementation preserving Request method, headers, signal, and body,
 * injecting X-XSRF-TOKEN for mutations, handling errorCode csrf.invalid without auto-resending.
 */
export const secureFetch: typeof globalThis.fetch = async (input, init) => {
  // Determine HTTP method from init override or input Request
  const method = (init?.method || (input instanceof Request ? input.method : "GET")).toUpperCase();

  // Extract and merge headers from input and init
  const headers = new Headers(input instanceof Request ? input.headers : undefined);
  if (init?.headers) {
    const initHeaders = new Headers(init.headers);
    initHeaders.forEach((val, key) => headers.set(key, val));
  }

  // Attach CSRF header for state-changing mutations
  const isMutation = !["GET", "HEAD", "OPTIONS"].includes(method);
  if (isMutation) {
    if (!memoryCsrfToken) {
      await refreshCsrfToken();
    }
    if (memoryCsrfToken) {
      headers.set("X-XSRF-TOKEN", memoryCsrfToken);
    }
  }

  // Construct outgoing Request ensuring method, headers, signal, credentials, and body are preserved
  let req: Request;
  if (input instanceof Request) {
    req = new Request(input, {
      ...init,
      headers,
      credentials: "include",
    });
  } else {
    const resolvedUrl = resolveApiUrl(input.toString());
    req = new Request(resolvedUrl, {
      ...init,
      method,
      headers,
      credentials: "include",
    });
  }

  let response: Response;
  try {
    response = await fetch(req);
  } catch {
    throw new ApiClientError({
      message: "Lỗi kết nối mạng. Vui lòng kiểm tra lại đường truyền của bạn.",
      status: 0,
      code: "network.offline",
      isNetworkError: true,
    });
  }

  // Handle CSRF token invalidation (400 csrf.invalid / errorCode)
  if (response.status === 400) {
    const clone = response.clone();
    try {
      const data = (await clone.json()) as unknown;
      if (typeof data === "object" && data !== null) {
        const problem = data as ProblemDetails & { code?: string };
        const errorCode = problem.errorCode || problem.code || "";
        if (errorCode === "csrf.invalid") {
          clearCachedCsrfToken();
          // Pre-fetch a fresh token for user's next manual attempt; do NOT auto-resend mutation!
          try {
            await refreshCsrfToken();
          } catch {
            // Ignore background refresh failure
          }
          const traceId = response.headers.get("x-trace-id") || problem.traceId || undefined;
          throw new ApiClientError({
            message: "Mã bảo mật đã hết hạn hoặc không khớp. Hệ thống đã làm mới mã, vui lòng gửi lại thao tác.",
            status: 400,
            code: "csrf.invalid",
            traceId,
            problem,
          });
        }
      }
    } catch (e) {
      if (e instanceof ApiClientError) throw e;
      // If not JSON, fall through to normal error handling
    }
  }

  return response;
};

// Initialize shared HRM client with absolute same-origin baseURL
export const hrmClient = createHrmClient(getBaseUrl(), secureFetch);

/**
 * Normalizes error messages and extracts errorCode and traceId.
 */
export function parseErrorMessage(error: unknown, fallback = "Đã xảy ra lỗi không xác định."): {
  message: string;
  traceId?: string;
  code?: string;
  isConflict: boolean;
  isRateLimited: boolean;
  isNetworkError: boolean;
} {
  if (error instanceof ApiClientError) {
    let friendlyMessage = error.message;

    switch (error.code) {
      case "auth.invalid":
        friendlyMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
        break;
      case "auth.rate_limited":
        friendlyMessage = "Bạn đã thử quá nhiều lần liên tiếp. Vui lòng đợi 60 giây rồi thử lại.";
        break;
      case "auth.reset_invalid":
        friendlyMessage = "Mã xác thực đặt lại mật khẩu không hợp lệ hoặc đã hết hiệu lực.";
        break;
      case "auth.activation_invalid":
        friendlyMessage = "Mã xác thực kích hoạt tài khoản không hợp lệ hoặc đã hết hiệu lực.";
        break;
      case "auth.password_invalid":
        friendlyMessage = "Mật khẩu hiện tại không chính xác hoặc mật khẩu mới chưa đủ độ phức tạp.";
        break;
      case "csrf.invalid":
        friendlyMessage = "Phiên thao tác bảo mật đã hết hạn. Đã tự động cập nhật mã mới, vui lòng bấm thử lại.";
        break;
      case "concurrency.conflict":
        friendlyMessage = error.problem?.detail || error.problem?.title || "Dữ liệu đang bị xung đột hoặc đang được chỉnh sửa bởi người khác.";
        break;
    }

    if (error.status === 409) {
      friendlyMessage = error.problem?.detail || error.problem?.title || friendlyMessage;
    }

    return {
      message: friendlyMessage,
      traceId: error.traceId,
      code: error.code,
      isConflict: error.isConflict,
      isRateLimited: error.isRateLimited,
      isNetworkError: error.isNetworkError,
    };
  }

  if (error instanceof Error) {
    return {
      message: error.message || fallback,
      isConflict: false,
      isRateLimited: false,
      isNetworkError: false,
    };
  }

  return {
    message: fallback,
    isConflict: false,
    isRateLimited: false,
    isNetworkError: false,
  };
}

/**
 * Fetch current authenticated user access (Id, Username, Roles, Permissions).
 * Forwards AbortSignal from TanStack Query.
 */
export async function fetchCurrentAccess(signal?: AbortSignal): Promise<CurrentUserAccessResponse> {
  const result = await hrmClient.GET("/api/v1/access/me", { signal });
  if (result.response?.status === 401) {
    throw new ApiClientError({ message: "Chưa đăng nhập", status: 401, code: "auth.unauthorized" });
  }
  if (result.error || !result.data) {
    const status = result.response?.status ?? 500;
    const problem = (typeof result.error === "object" ? result.error : null) as (ProblemDetails & { code?: string }) | null;
    const traceId = result.response?.headers?.get("x-trace-id") || problem?.traceId || undefined;
    throw new ApiClientError({
      message: problem?.detail || problem?.title || `Lỗi tải thông tin truy cập (HTTP ${status})`,
      status,
      code: problem?.errorCode || problem?.code || "auth.fetch_failed",
      traceId,
      problem: problem ?? undefined,
    });
  }
  return result.data;
}
