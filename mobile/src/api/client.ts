import { getAppConfig } from "../config/env";
import { SessionManager } from "./session";
import { ApiClientError } from "./errors";
import type { ProblemDetails } from "./types";
import { withAbort } from './timeout';

export interface RequestOptions extends RequestInit {
  skipAuth?: boolean;
  timeoutMs?: number;
}

/**
 * Kiểm tra xem request có phải là thao tác an toàn (Safe/Idempotent) để tự động retry hay không.
 */
function isSafeToRetry(method: string, headers: Headers): boolean {
  const upperMethod = method.toUpperCase();
  if (["GET", "HEAD", "OPTIONS"].includes(upperMethod)) {
    return true;
  }
  if (headers.has("Idempotency-Key") || headers.has("idempotency-key")) {
    return true;
  }
  return false;
}

/**
 * Phân giải và kiểm tra nguồn gốc URL trước khi gửi yêu cầu mạng.
 * Chặn hoàn toàn yêu cầu gửi đến origin khác để không rò rỉ Bearer token.
 */
function resolveAndValidateUrl(path: string, apiOrigin: string, apiBaseUrl: string): string {
  let target: URL;
  try {
    if (path.startsWith("http://") || path.startsWith("https://")) {
      target = new URL(path);
    } else {
      const normalizedPath = path.startsWith("/") ? path : `/${path}`;
      target = new URL(`${apiBaseUrl}${normalizedPath}`);
    }
  } catch {
    throw new ApiClientError({
      message: "Đường dẫn yêu cầu không hợp lệ.",
      status: 0,
      code: "url.invalid",
    });
  }

  // Chặn nghiêm ngặt: Nguồn gốc bắt buộc phải trùng khớp với apiOrigin đã cấu hình
  if (target.origin !== apiOrigin || target.username || target.password) {
    throw new ApiClientError({
      message: "Yêu cầu đến nguồn gốc máy chủ khác không được phép.",
      status: 0,
      code: "security.cross_origin_blocked",
    });
  }

  // Chặn giao thức không an toàn
  if (target.protocol !== "https:") {
    throw new ApiClientError({
      message: "Chỉ được phép kết nối qua giao thức bảo mật HTTPS.",
      status: 0,
      code: "security.https_required",
    });
  }

  return target.toString();
}

/**
 * Mobile Secure Fetch Adapter.
 * Đảm nhiệm:
 * 1. Chặn URL ngoài origin trước khi fetch.
 * 2. Đính kèm Bearer token từ SessionManager.
 * 3. Chặn rò rỉ dữ liệu khi đổi tài khoản (Epoch Guard sau mỗi await).
 * 4. Xử lý 401 Single-Flight refresh và invalidate credentials nếu retry lần 2 vẫn 401.
 * 5. Timeout bao trọn toàn bộ chu kỳ: fetch + body decode + refresh + retry.
 */
export async function mobileFetch<T = unknown>(
  path: string,
  options: RequestOptions = {}
): Promise<T> {
  const config = getAppConfig();
  const targetUrl = resolveAndValidateUrl(path, config.apiOrigin, config.apiBaseUrl);

  const isAuthRoute =
    targetUrl.includes("/api/v1/auth/mobile/login") ||
    targetUrl.includes("/api/v1/auth/mobile/refresh") ||
    targetUrl.includes("/api/v1/auth/forgot-password") ||
    targetUrl.includes("/api/v1/auth/reset-password") ||
    targetUrl.includes("/api/v1/auth/activate");

  const method = (options.method || "GET").toUpperCase();
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && typeof options.body === "string" && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  // BƯỚC 1: Chụp Epoch và AccessToken tại thời điểm bắt đầu request
  const requestEpoch = SessionManager.getEpoch();
  const initialSession = SessionManager.getSession();
  const requestAccessToken = initialSession?.accessToken;

  // Đính kèm Bearer token nếu không skipAuth và không phải auth routes công khai
  if (!options.skipAuth && !isAuthRoute && requestAccessToken) {
    headers.set("Authorization", `Bearer ${requestAccessToken}`);
  }

  // BƯỚC 2: Thiết lập Timeout bao trọn toàn bộ chu kỳ (kết hợp với caller signal nếu có)
  const timeoutMs = options.timeoutMs ?? config.timeoutMs;
  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), timeoutMs);
  const callerAborted = () => timeoutController.abort();

  if (options.signal) {
    if (options.signal.aborted) {
      timeoutController.abort();
    } else {
      options.signal.addEventListener("abort", callerAborted, { once: true });
    }
  }

  let response: Response;
  try {
    try {
      response = await withAbort(fetch(targetUrl, {
        ...options,
        method,
        headers,
        signal: timeoutController.signal,
      }), timeoutController.signal);
    } catch (err) {
      if (timeoutController.signal.aborted) {
        throw new ApiClientError({
          message: "Hết thời gian chờ phản hồi từ máy chủ (Timeout). Vui lòng thử lại.",
          status: 0,
          code: "network.timeout",
          isNetworkError: true,
        });
      }
      throw new ApiClientError({
        message: "Lỗi kết nối mạng. Vui lòng kiểm tra lại đường truyền của bạn.",
        status: 0,
        code: "network.offline",
        isNetworkError: true,
      });
    }

    // EPOCH GUARD 1: Kiểm tra sau await fetch
    if (SessionManager.getEpoch() !== requestEpoch) {
      throw new ApiClientError({
        message: "Phiên làm việc đã thay đổi trong lúc chờ phản hồi.",
        status: 401,
        code: "auth.session_changed",
      });
    }

    // Xử lý 401 Unauthorized
    if (response.status === 401) {
      // Nếu là route auth hoặc skipAuth -> không refresh, trả lỗi ngay
      if (isAuthRoute || options.skipAuth) {
        let problem: ProblemDetails | undefined;
        try {
          problem = (await response.json()) as ProblemDetails;
        } catch {
          // ignore
        }
        throw new ApiClientError({
          message: problem?.detail || "Yêu cầu xác thực không hợp lệ.",
          status: 401,
          code: problem?.errorCode || "auth.invalid",
          problem,
        });
      }

      // Delayed 401 của tài khoản cũ không được can thiệp vào tài khoản mới
      if (SessionManager.getEpoch() !== requestEpoch) {
        throw new ApiClientError({
          message: "Phiên làm việc đã thay đổi.",
          status: 401,
          code: "auth.session_changed",
        });
      }

      // Kiểm tra xem token đã được làm mới bởi request khác trong lúc chờ chưa
      let validSession = SessionManager.getSession();
      if (!validSession || validSession.accessToken === requestAccessToken) {
        // Cần thực hiện làm mới Single-flight
        validSession = await withAbort(SessionManager.refreshSingleFlight(), timeoutController.signal);
      }

      // EPOCH GUARD 2: Kiểm tra sau await refresh
      if (SessionManager.getEpoch() !== requestEpoch || !validSession) {
        throw new ApiClientError({
          message: "Phiên làm việc đã hết hạn hoặc bị thu hồi trên máy chủ.",
          status: 401,
          code: "auth.session_expired",
        });
      }

      // Kiểm tra tính an toàn khi retry
      if (!isSafeToRetry(method, headers)) {
        throw new ApiClientError({
          message:
            "Phiên làm việc đã được làm mới. Vui lòng bấm thử lại thao tác để đảm bảo an toàn giao dịch.",
          status: 401,
          code: "auth.reauthenticated_mutation_requires_retry",
        });
      }

      // Retry duy nhất 1 lần với Access Token mới
      const retryHeaders = new Headers(headers);
      retryHeaders.set("Authorization", `Bearer ${validSession.accessToken}`);

      let retryResponse: Response;
      try {
        retryResponse = await withAbort(fetch(targetUrl, {
          ...options,
          method,
          headers: retryHeaders,
          signal: timeoutController.signal,
        }), timeoutController.signal);
      } catch (retryErr) {
        throw new ApiClientError({
          message: "Lỗi mạng khi thử lại yêu cầu.",
          status: 0,
          code: "network.offline",
          isNetworkError: true,
        });
      }

      // EPOCH GUARD 3: Kiểm tra sau await retry
      if (SessionManager.getEpoch() !== requestEpoch) {
        throw new ApiClientError({
          message: "Phiên làm việc đã thay đổi trong lúc thử lại.",
          status: 401,
          code: "auth.session_changed",
        });
      }

      // Nếu retry lần 2 mà VẪN bị 401 -> Hủy toàn bộ credentials ngay lập tức!
      if (retryResponse.status === 401) {
        await SessionManager.invalidateSession();
        SessionManager.notifySessionExpired();
        throw new ApiClientError({
          message: "Phiên làm việc không còn hiệu lực trên máy chủ.",
          status: 401,
          code: "auth.session_expired",
        });
      }

      return await withAbort(handleResponse<T>(retryResponse, requestEpoch), timeoutController.signal);
    }

    return await withAbort(handleResponse<T>(response, requestEpoch), timeoutController.signal);
  } finally {
    clearTimeout(timeoutId);
    options.signal?.removeEventListener('abort', callerAborted);
  }
}

/**
 * Xử lý giải mã phản hồi và guard Epoch sau khi đọc body.
 */
async function handleResponse<T>(response: Response, requestEpoch: number): Promise<T> {
  if (response.status === 204) {
    if (SessionManager.getEpoch() !== requestEpoch) {
      throw new ApiClientError({
        message: "Phiên làm việc đã thay đổi.",
        status: 401,
        code: "auth.session_changed",
      });
    }
    return null as unknown as T;
  }

  const contentType = response.headers.get("content-type") || "";
  const isJson = contentType.includes("application/json") || contentType.includes("application/problem+json");

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    if (isJson) {
      try {
        problem = (await response.json()) as ProblemDetails;
      } catch {
        // ignore
      }
    }

    if (SessionManager.getEpoch() !== requestEpoch) {
      throw new ApiClientError({
        message: "Phiên làm việc đã thay đổi.",
        status: 401,
        code: "auth.session_changed",
      });
    }

    const traceId = response.headers.get("x-trace-id") || problem?.traceId;
    const errorCode = problem?.errorCode;

    throw new ApiClientError({
      message: problem?.detail || problem?.title || `Yêu cầu thất bại với mã lỗi HTTP ${response.status}`,
      status: response.status,
      code: errorCode,
      traceId: traceId || undefined,
      problem,
    });
  }

  let resultData: T;
  if (isJson) {
    resultData = (await response.json()) as T;
  } else {
    resultData = (await response.text()) as unknown as T;
  }

  // EPOCH GUARD 4: Kiểm tra sau await đọc body response
  if (SessionManager.getEpoch() !== requestEpoch) {
    throw new ApiClientError({
      message: "Phiên làm việc đã thay đổi trong lúc đọc dữ liệu.",
      status: 401,
      code: "auth.session_changed",
    });
  }

  return resultData;
}
