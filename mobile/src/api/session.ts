import { SecureStoreAdapter, StoredSession } from "../storage/secureStore";
import { DataCacheManager } from "../storage/cache";
import { getAppConfig } from "../config/env";
import { ApiClientError } from "./errors";
import type { TokenPair, ProblemDetails } from "./types";
import { withAbort } from './timeout';

export type SessionExpiredListener = () => void;

/**
 * Trình quản lý phiên an toàn cho Mobile (Safe Session Manager).
 * Đảm bảo:
 * 1. Single-flight refresh: Nhiều request 401 đồng thời chỉ gửi 1 yêu cầu làm mới.
 * 2. Inflight Promise được gán ĐỒNG BỘ (synchronously) trước bất kỳ await nào.
 * 3. Invalidate Session tăng Epoch ĐỒNG BỘ trước bất kỳ await nào để ngăn race condition.
 * 4. Guard Epoch trước và sau các điểm await (fetch, body parse, saveSession, clearSession).
 * 5. Ngăn chặn tuyệt đối zombie token hồi sinh phiên sau khi đã đăng xuất.
 */
export class SessionManager {
  private static inflightRefresh: Promise<StoredSession | null> | null = null;
  private static inflightRefreshEpoch: number | null = null;
  private static sessionEpoch = 0;
  private static onSessionExpiredCallbacks: Set<SessionExpiredListener> = new Set();
  private static currentSession: StoredSession | null = null;
  private static storageTail: Promise<unknown> = Promise.resolve();

  private static serialize<T>(operation: () => Promise<T>): Promise<T> {
    const next = SessionManager.storageTail.then(operation, operation);
    SessionManager.storageTail = next.catch(() => undefined);
    return next;
  }

  public static subscribeSessionExpired(listener: SessionExpiredListener): () => void {
    SessionManager.onSessionExpiredCallbacks.add(listener);
    return () => {
      SessionManager.onSessionExpiredCallbacks.delete(listener);
    };
  }

  public static notifySessionExpired(): void {
    for (const callback of SessionManager.onSessionExpiredCallbacks) {
      try {
        callback();
      } catch {
        // Tránh callback lỗi làm gãy luồng chính
      }
    }
  }

  public static async initialize(): Promise<StoredSession | null> {
    const epoch = SessionManager.sessionEpoch;
    return SessionManager.serialize(async () => {
      const stored = await SecureStoreAdapter.getSession();
      if (SessionManager.sessionEpoch !== epoch) return null;
      SessionManager.currentSession = stored;
      return stored;
    });
  }

  public static getSession(): StoredSession | null {
    return SessionManager.currentSession;
  }

  public static getEpoch(): number {
    return SessionManager.sessionEpoch;
  }

  /**
   * Thiết lập phiên mới có kiểm tra guard thế hệ Epoch.
   */
  public static async setSession(tokens: TokenPair, expectedEpoch?: number): Promise<StoredSession | null> {
    const targetEpoch = expectedEpoch !== undefined ? expectedEpoch : SessionManager.sessionEpoch;

    // Guard trước await
    if (SessionManager.sessionEpoch !== targetEpoch) {
      return null;
    }

    return SessionManager.serialize(async () => {
    if (SessionManager.sessionEpoch !== targetEpoch) return null;
    const saved = await SecureStoreAdapter.saveSession({
      tokenType: tokens.tokenType || "Bearer",
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      expiresIn: tokens.expiresIn,
    });

    // Guard sau await: Kiểm tra xem có invalidateSession nào xảy ra trong lúc đang ghi không
    if (SessionManager.sessionEpoch !== targetEpoch) {
      // Đã có invalidation trong lúc ghi storage -> xóa ngay dữ liệu vừa ghi, không hồi sinh phiên
      await SecureStoreAdapter.clearSession();
      SessionManager.currentSession = null;
      return null;
    }

    SessionManager.currentSession = saved;
    return saved;
    });
  }

  /**
   * Vô hiệu hóa phiên hiện hành ngay lập tức.
   * Tăng Epoch ĐỒNG BỘ trước bất kỳ await nào để triệt tiêu mọi Promise đang bay.
   */
  public static async invalidateSession(): Promise<void> {
    // BƯỚC 1: Tăng Epoch đồng bộ ngay lập tức trước khi chạm vào bất kỳ await nào
    ++SessionManager.sessionEpoch;
    SessionManager.currentSession = null;
    SessionManager.inflightRefresh = null;
    SessionManager.inflightRefreshEpoch = null;

    // Xóa sạch bộ nhớ đệm cache dữ liệu nghiệp vụ
    DataCacheManager.clearAll();

    SessionManager.notifySessionExpired();
    // Serialize with earlier writes so an old write cannot erase a new account.
    await SessionManager.serialize(() => SecureStoreAdapter.clearSession());
  }

  public static async resetForTesting(): Promise<void> {
    SessionManager.sessionEpoch = 0;
    SessionManager.currentSession = null;
    SessionManager.inflightRefresh = null;
    SessionManager.inflightRefreshEpoch = null;
    SessionManager.onSessionExpiredCallbacks.clear();
    DataCacheManager.clearAll();
    await SecureStoreAdapter.clearSession();
  }

  /**
   * Cơ chế Refresh Single-Flight gắn chặt với Epoch của phiên.
   */
  public static refreshSingleFlight(): Promise<StoredSession | null> {
    const startEpoch = SessionManager.sessionEpoch;

    // Tái sử dụng request đang bay nếu cùng một Epoch
    if (SessionManager.inflightRefresh && SessionManager.inflightRefreshEpoch === startEpoch) {
      return SessionManager.inflightRefresh;
    }

    // GÁN INFLIGHT PROMISE ĐỒNG BỘ NGAY LẬP TỨC TRƯỚC BẤT KỲ AWAIT NÀO
    SessionManager.inflightRefreshEpoch = startEpoch;

    const refreshPromise = Promise.resolve().then(async (): Promise<StoredSession | null> => {
      let timeoutId: ReturnType<typeof setTimeout> | undefined;
      try {
        // Đọc phiên từ bộ nhớ hoặc storage
        let current = SessionManager.currentSession;
        if (!current) {
          current = await SessionManager.serialize(() => SecureStoreAdapter.getSession());
        }

        // Guard sau khi đọc storage
        if (SessionManager.sessionEpoch !== startEpoch) {
          return null;
        }

        if (!current?.refreshToken) {
          await SessionManager.invalidateSession();
          SessionManager.notifySessionExpired();
          return null;
        }

        const config = getAppConfig();
        const refreshUrl = `${config.apiBaseUrl}/api/v1/auth/mobile/refresh`;

        // Timeout riêng cho refresh request (10 giây)
        const controller = new AbortController();
        timeoutId = setTimeout(() => controller.abort(), 10000);

        let response: Response;
        try {
          response = await withAbort(fetch(refreshUrl, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Accept: "application/json",
            },
            body: JSON.stringify({
              refreshToken: current.refreshToken,
            }),
            signal: controller.signal,
          }), controller.signal);
        } catch (fetchErr) {
          // Lỗi mạng tạm thời hoặc timeout
          throw new ApiClientError({
            message: "Không thể kết nối máy chủ để làm mới phiên làm việc.",
            status: 0,
            code: "network.offline",
            isNetworkError: true,
          });
        }

        // Guard sau fetch
        if (SessionManager.sessionEpoch !== startEpoch) {
          return null;
        }

        if (!response.ok) {
          const status = response.status;
          let problem: ProblemDetails | undefined;
          try {
            problem = (await withAbort(response.json(), controller.signal)) as ProblemDetails;
          } catch {
            // Không phải JSON
          }

          // Guard sau body parse lỗi
          if (SessionManager.sessionEpoch !== startEpoch) {
            return null;
          }

          if (status === 401 || status === 400) {
            // Server từ chối: phiên đã bị thu hồi hoặc token không hợp lệ
            await SessionManager.invalidateSession();
            SessionManager.notifySessionExpired();
            return null;
          }

          throw new ApiClientError({
            message: problem?.detail || `Làm mới phiên thất bại (HTTP ${status}).`,
            status,
            code: problem?.errorCode || "auth.refresh_failed",
            problem,
          });
        }

        const data = (await withAbort(response.json(), controller.signal)) as TokenPair;

        // Guard sau body parse thành công
        if (SessionManager.sessionEpoch !== startEpoch) {
          return null;
        }

        if (!data.accessToken || !data.refreshToken) {
          await SessionManager.invalidateSession();
          SessionManager.notifySessionExpired();
          return null;
        }

        // Lưu phiên mới với guard Epoch chặt chẽ
        const newSession = await SessionManager.setSession(data, startEpoch);
        return newSession;
      } finally {
        clearTimeout(timeoutId);
        // Chỉ dọn dẹp inflightRefresh nếu chính promise này đang là inflight của epoch đó
        if (SessionManager.inflightRefresh === refreshPromise) {
          SessionManager.inflightRefresh = null;
          SessionManager.inflightRefreshEpoch = null;
        }
      }
    });

    SessionManager.inflightRefresh = refreshPromise;
    return refreshPromise;
  }
}
