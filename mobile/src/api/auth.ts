import { mobileFetch } from "./client";
import { SessionManager } from "./session";
import { DataCacheManager } from "../storage/cache";
import { getAppConfig } from "../config/env";
import { withAbort } from './timeout';
import type {
  LoginRequest,
  RefreshRequest,
  ForgotRequest,
  ResetRequest,
  ChangePasswordRequest,
  TokenPair,
  CurrentUserAccessResponse,
  DeliveryResponse,
} from "./types";

/**
 * Đăng nhập ứng dụng Mobile bằng tài khoản và mật khẩu.
 * Sử dụng route POST /api/v1/auth/mobile/login.
 */
export async function mobileLogin(params: LoginRequest): Promise<TokenPair> {
  const clearing = SessionManager.invalidateSession();
  const loginEpoch = SessionManager.getEpoch();
  await clearing;
  const result = await mobileFetch<TokenPair>("/api/v1/auth/mobile/login", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(params),
  });

  if (!await SessionManager.setSession(result, loginEpoch)) throw new Error('Phiên đăng nhập đã thay đổi.');
  return result;
}

/**
 * Làm mới phiên bằng Refresh Token.
 * Sử dụng route POST /api/v1/auth/mobile/refresh.
 */
export async function mobileRefresh(params: RefreshRequest): Promise<TokenPair> {
  const epoch = SessionManager.getEpoch();
  const result = await mobileFetch<TokenPair>("/api/v1/auth/mobile/refresh", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(params),
  });

  if (!await SessionManager.setSession(result, epoch)) throw new Error('Phiên đăng nhập đã thay đổi.');
  return result;
}

/**
 * Gửi yêu cầu quên mật khẩu.
 * Nhận về deliveryMode thật từ máy chủ.
 */
export async function forgotPassword(params: ForgotRequest): Promise<DeliveryResponse> {
  return await mobileFetch<DeliveryResponse>("/api/v1/auth/forgot-password", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(params),
  });
}

/**
 * Đặt lại mật khẩu bằng userId và mã xác thực nhận từ kênh email thử nghiệm.
 */
export async function resetPassword(params: ResetRequest): Promise<void> {
  await mobileFetch<void>("/api/v1/auth/reset-password", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(params),
  });
}

/**
 * Kích hoạt tài khoản mới bằng userId và mã kích hoạt.
 */
export async function activateAccount(params: ResetRequest): Promise<void> {
  await mobileFetch<void>("/api/v1/auth/activate", {
    method: "POST",
    skipAuth: true,
    body: JSON.stringify(params),
  });
}

/**
 * Đổi mật khẩu cho người dùng hiện hành.
 * Server backend tự động thu hồi mọi phiên đang hoạt động của tài khoản.
 */
export async function changePassword(params: ChangePasswordRequest): Promise<void> {
  await mobileFetch<void>("/api/v1/auth/change-password", {
    method: "POST",
    body: JSON.stringify(params),
  });

  await SessionManager.invalidateSession();
}

/**
 * Đăng xuất an toàn:
 * 1. Chụp token trước khi xóa.
 * 2. Invalidate Epoch và xóa SecureStore cục bộ NGAY LẬP TỨC.
 * 3. Gửi request thu hồi đến server bằng token đã chụp (không chờ network mới xóa).
 * 4. Trả về serverConfirmed chuẩn xác (không đánh dấu true bừa bãi).
 */
export async function logout(): Promise<{ serverConfirmed: boolean; localCleared: boolean; error?: unknown }> {
  const current = SessionManager.getSession();
  const tokenToRevoke = current?.accessToken;

  // XÓA CỤC BỘ NGAY LẬP TỨC
  let localCleared = true;
  let localError: unknown;
  try {
    DataCacheManager.clearAll();
    await SessionManager.invalidateSession();
  }
  catch (error) { localCleared = false; localError = error; }

  if (!tokenToRevoke) {
    return { serverConfirmed: true, localCleared, error: localError };
  }

  let serverConfirmed = false;
  let serverError: unknown = null;
  let timeoutId: ReturnType<typeof setTimeout> | undefined;

  try {
    const config = getAppConfig();
    const logoutUrl = `${config.apiBaseUrl}/api/v1/auth/logout`;

    const controller = new AbortController();
    timeoutId = setTimeout(() => controller.abort(), 8000);

    const response = await withAbort(fetch(logoutUrl, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${tokenToRevoke}`,
        Accept: "application/json",
      },
      signal: controller.signal,
    }), controller.signal);

    // A 401 may mean only the access token expired; it does not prove refresh revocation.
    if (response.status === 204) {
      serverConfirmed = true;
    } else {
      serverConfirmed = false;
      serverError = new Error(`Máy chủ trả mã lỗi ${response.status} khi đăng xuất.`);
    }
  } catch (err) {
    serverConfirmed = false;
    serverError = err;
  } finally {
    clearTimeout(timeoutId);
  }

  return { serverConfirmed, localCleared, error: localError || serverError };
}

/**
 * Lấy danh tính, quyền hạn và vai trò thực tế của người dùng từ máy chủ.
 * Route: GET /api/v1/access/me.
 */
export async function fetchCurrentAccess(): Promise<CurrentUserAccessResponse> {
  return await mobileFetch<CurrentUserAccessResponse>("/api/v1/access/me", {
    method: "GET",
  });
}
