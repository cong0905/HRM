import * as ExpoSecureStore from "expo-secure-store";

export interface StoredSession {
  tokenType: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: number; // Client timestamp epoch milliseconds
}

export interface SaveSessionInput {
  tokenType?: string;
  accessToken?: string;
  refreshToken?: string;
  expiresIn?: number;
}

const STORAGE_KEYS = {
  SESSION: "hrm_secure_session_v1",
  ACCESS_TOKEN: "hrm_secure_access_token",
  REFRESH_TOKEN: "hrm_secure_refresh_token",
  TOKEN_TYPE: "hrm_secure_token_type",
  EXPIRES_AT: "hrm_secure_expires_at",
  SESSION_EPOCH: "hrm_secure_session_epoch",
} as const;

/**
 * Bộ nhớ tạm in-memory CHỈ DÙNG trong môi trường kiểm thử (Jest mock mode).
 * Tuyệt đối không fallback memory trong production.
 */
const inMemoryTestStore = new Map<string, string>();

function isExpoSecureStoreAvailable(): boolean {
  return (
    typeof ExpoSecureStore !== "undefined" &&
    typeof ExpoSecureStore.getItemAsync === "function" &&
    typeof ExpoSecureStore.setItemAsync === "function" &&
    typeof ExpoSecureStore.deleteItemAsync === "function"
  );
}

/**
 * Lớp trừu tượng truy cập Secure Storage, bảo vệ bí mật phiên.
 * Trong production: Fail-closed nếu native module không khả dụng.
 */
export class SecureStoreAdapter {
  private static mockEnabled = false;

  /**
   * Chế độ mock phục vụ unit test tách biệt.
   */
  public static setMockMode(enabled: boolean): void {
    SecureStoreAdapter.mockEnabled = enabled;
    if (!enabled) {
      inMemoryTestStore.clear();
    }
  }

  public static resetMockStore(): void {
    inMemoryTestStore.clear();
  }

  private static async setItem(key: string, value: string): Promise<void> {
    if (SecureStoreAdapter.mockEnabled) {
      inMemoryTestStore.set(key, value);
      return;
    }

    if (!isExpoSecureStoreAvailable()) {
      throw new Error(
        "SecureStore native module không khả dụng. Không thể lưu trữ an toàn bí mật phiên (Fail-Closed)."
      );
    }

    await ExpoSecureStore.setItemAsync(key, value, {
      keychainAccessible: ExpoSecureStore.AFTER_FIRST_UNLOCK,
    });
  }

  private static async getItem(key: string): Promise<string | null> {
    if (SecureStoreAdapter.mockEnabled) {
      return inMemoryTestStore.get(key) ?? null;
    }

    if (!isExpoSecureStoreAvailable()) {
      throw new Error(
        "SecureStore native module không khả dụng. Không thể truy xuất bí mật phiên (Fail-Closed)."
      );
    }

    return await ExpoSecureStore.getItemAsync(key);
  }

  private static async deleteItem(key: string): Promise<void> {
    if (SecureStoreAdapter.mockEnabled) {
      inMemoryTestStore.delete(key);
      return;
    }

    if (!isExpoSecureStoreAvailable()) {
      throw new Error(
        "SecureStore native module không khả dụng. Không thể xóa bí mật phiên (Fail-Closed)."
      );
    }

    await ExpoSecureStore.deleteItemAsync(key);
  }

  /**
   * Xác thực chặt chẽ cặp token trước khi ghi vào SecureStore.
   */
  public static validateTokenPair(params: SaveSessionInput): {
    tokenType: string;
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
  } {
    const tokenType = (params.tokenType || "").trim();
    if (!tokenType || tokenType.toLowerCase() !== "bearer") {
      throw new Error("TokenPair không hợp lệ: tokenType bắt buộc phải là 'Bearer'.");
    }

    const accessToken = (params.accessToken || "").trim();
    if (!accessToken) {
      throw new Error("TokenPair không hợp lệ: accessToken không được để trống.");
    }

    const refreshToken = (params.refreshToken || "").trim();
    if (!refreshToken) {
      throw new Error("TokenPair không hợp lệ: refreshToken không được để trống.");
    }

    const expiresIn = params.expiresIn;
    if (typeof expiresIn !== "number" || !Number.isFinite(expiresIn) || expiresIn <= 0) {
      throw new Error("TokenPair không hợp lệ: expiresIn bắt buộc phải là số dương hữu hạn.");
    }

    return {
      tokenType: "Bearer",
      accessToken,
      refreshToken,
      expiresIn,
    };
  }

  /**
   * Lưu trữ cặp token phiên an toàn sau khi đã xác thực runtime.
   */
  public static async saveSession(params: SaveSessionInput): Promise<StoredSession> {
    const validated = SecureStoreAdapter.validateTokenPair(params);
    const expiresAt = Date.now() + validated.expiresIn * 1000;

    const session = {
      tokenType: validated.tokenType,
      accessToken: validated.accessToken,
      refreshToken: validated.refreshToken,
      expiresAt,
    };
    await SecureStoreAdapter.setItem(STORAGE_KEYS.SESSION, JSON.stringify(session));
    return session;
  }

  /**
   * Đọc phiên đã lưu từ SecureStore.
   */
  public static async getSession(): Promise<StoredSession | null> {
    const raw = await SecureStoreAdapter.getItem(STORAGE_KEYS.SESSION);
    if (!raw) return null;
    try {
      const value: unknown = JSON.parse(raw);
      if (!value || typeof value !== 'object') return null;
      const stored = value as Record<string, unknown>;
      if (stored.tokenType !== 'Bearer' || typeof stored.accessToken !== 'string' ||
          !stored.accessToken || typeof stored.refreshToken !== 'string' || !stored.refreshToken ||
          typeof stored.expiresAt !== 'number' || !Number.isFinite(stored.expiresAt) || stored.expiresAt <= 0) return null;
      return stored as unknown as StoredSession;
    } catch { return null; }
  }

  /**
   * Xóa toàn bộ token phiên khỏi SecureStore khi đăng xuất hoặc phiên bị thu hồi.
   */
  public static async clearSession(): Promise<void> {
    await Promise.all([
      SecureStoreAdapter.deleteItem(STORAGE_KEYS.SESSION),
      SecureStoreAdapter.deleteItem(STORAGE_KEYS.TOKEN_TYPE),
      SecureStoreAdapter.deleteItem(STORAGE_KEYS.ACCESS_TOKEN),
      SecureStoreAdapter.deleteItem(STORAGE_KEYS.REFRESH_TOKEN),
      SecureStoreAdapter.deleteItem(STORAGE_KEYS.EXPIRES_AT),
    ]);
  }

  /**
   * Quản lý Epoch thế hệ phiên để chống race condition.
   */
  public static async getSessionEpoch(): Promise<number> {
    const epochStr = await SecureStoreAdapter.getItem(STORAGE_KEYS.SESSION_EPOCH);
    if (!epochStr) return 0;
    const epoch = parseInt(epochStr, 10);
    return isNaN(epoch) ? 0 : epoch;
  }

  public static async incrementSessionEpoch(): Promise<number> {
    const current = await SecureStoreAdapter.getSessionEpoch();
    const next = current + 1;
    await SecureStoreAdapter.setItem(STORAGE_KEYS.SESSION_EPOCH, next.toString());
    return next;
  }
}
