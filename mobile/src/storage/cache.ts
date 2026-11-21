/**
 * Quản lý bộ nhớ đệm dữ liệu ứng dụng Mobile (In-Memory & Session-Scoped Cache).
 * Đảm bảo:
 * 1. Cách ly hoàn toàn giữa các tài khoản người dùng (User & Session Epoch Bound).
 * 2. Xóa sạch mọi dữ liệu đã cache khi đăng xuất (Wipe Cache on Logout).
 * 3. Ngăn ngừa rò rỉ phiếu lương hoặc thông tin cá nhân của người dùng trước.
 */

interface CacheEntry<T> {
  data: T;
  timestamp: number;
  userId?: string;
  epoch: number;
}

export class DataCacheManager {
  private static store: Map<string, CacheEntry<unknown>> = new Map();
  private static currentUserId: string | null = null;
  private static currentEpoch = 0;

  public static setActiveUser(userId: string | null, epoch: number): void {
    if (this.currentUserId !== userId || this.currentEpoch !== epoch) {
      // Đổi tài khoản hoặc đổi epoch -> tự động dọn sạch cache của tài khoản cũ
      this.clearAll();
      this.currentUserId = userId;
      this.currentEpoch = epoch;
    }
  }

  public static set<T>(key: string, data: T, ttlMs = 5 * 60 * 1000): void {
    this.store.set(key, {
      data,
      timestamp: Date.now() + ttlMs,
      userId: this.currentUserId ?? undefined,
      epoch: this.currentEpoch,
    });
  }

  public static get<T>(key: string): T | null {
    const entry = this.store.get(key) as CacheEntry<T> | undefined;
    if (!entry) return null;

    // Kiểm tra epoch và userId guard
    if (entry.epoch !== this.currentEpoch) {
      this.store.delete(key);
      return null;
    }

    if (this.currentUserId && entry.userId && entry.userId !== this.currentUserId) {
      this.store.delete(key);
      return null;
    }

    // Kiểm tra TTL
    if (Date.now() > entry.timestamp) {
      this.store.delete(key);
      return null;
    }

    return entry.data;
  }

  public static remove(key: string): void {
    this.store.delete(key);
  }

  /**
   * Xóa toàn bộ dữ liệu đệm khỏi bộ nhớ.
   * Bắt buộc gọi khi đăng xuất, đổi phiên, hoặc thu hồi token.
   */
  public static clearAll(): void {
    this.store.clear();
  }

  public static size(): number {
    return this.store.size;
  }
}
