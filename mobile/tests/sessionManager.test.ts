import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";
import { ApiClientError } from "../src/api/errors";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("SessionManager - Quản lý phiên, Single-flight refresh và chống Race condition", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(async () => {
    jest.restoreAllMocks();
    SecureStoreAdapter.setMockMode(true);
    SecureStoreAdapter.resetMockStore();
    await SessionManager.resetForTesting();
    process.env.EXPO_PUBLIC_API_URL = "https://api.example.invalid";
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test("Nhiều 401 đồng thời chỉ gửi đúng 1 request làm mới phiên (Single-flight)", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "old_access_token",
      refreshToken: "valid_refresh_token",
      expiresIn: 300,
    });

    let refreshCallCount = 0;
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url.includes("/api/v1/auth/mobile/refresh")) {
        refreshCallCount++;
        await new Promise((resolve) => setTimeout(resolve, 30));
        return new Response(
          JSON.stringify({
            tokenType: "Bearer",
            accessToken: "new_refreshed_access_token",
            refreshToken: "new_refreshed_refresh_token",
            expiresIn: 900,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }
      return new Response("Not found", { status: 404 });
    });

    const results = await Promise.all([
      SessionManager.refreshSingleFlight(),
      SessionManager.refreshSingleFlight(),
      SessionManager.refreshSingleFlight(),
      SessionManager.refreshSingleFlight(),
      SessionManager.refreshSingleFlight(),
    ]);

    expect(refreshCallCount).toBe(1);

    for (const res of results) {
      expect(res).not.toBeNull();
      expect(res?.accessToken).toBe("new_refreshed_access_token");
      expect(res?.refreshToken).toBe("new_refreshed_refresh_token");
    }

    const current = SessionManager.getSession();
    expect(current?.accessToken).toBe("new_refreshed_access_token");
  });

  test("Refresh callers dùng chung 1 request kể cả khi phiên mới chỉ nằm trong storage", async () => {
    await SessionManager.resetForTesting();
    await SecureStoreAdapter.saveSession({
      tokenType: "Bearer",
      accessToken: "storage_access",
      refreshToken: "storage_refresh",
      expiresIn: 900,
    });

    const fetcher = jest.fn(async () =>
      new Response(
        JSON.stringify({
          tokenType: "Bearer",
          accessToken: "new_access",
          refreshToken: "new_refresh",
          expiresIn: 900,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      )
    );
    globalThis.fetch = fetcher;

    await Promise.all([SessionManager.refreshSingleFlight(), SessionManager.refreshSingleFlight()]);
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  test("Refresh bị rejected (401/thu hồi) -> Xóa sạch phiên và thông báo hết hạn", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "expired_access",
      refreshToken: "revoked_refresh",
      expiresIn: 100,
    });

    let sessionExpiredCalled = false;
    SessionManager.subscribeSessionExpired(() => {
      sessionExpiredCalled = true;
    });

    globalThis.fetch = jest.fn(async () => {
      return new Response(
        JSON.stringify({
          status: 401,
          errorCode: "auth.invalid",
          detail: "Phiên làm việc đã bị thu hồi.",
        }),
        { status: 401, headers: { "Content-Type": "application/problem+json" } }
      );
    });

    const result = await SessionManager.refreshSingleFlight();

    expect(result).toBeNull();
    expect(sessionExpiredCalled).toBe(true);

    expect(SessionManager.getSession()).toBeNull();
    const stored = await SecureStoreAdapter.getSession();
    expect(stored).toBeNull();
  });

  test("Lỗi mạng offline trong lúc refresh -> Không xóa SecureStore, giữ nguyên refresh token", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "access_token",
      refreshToken: "keep_this_refresh_token",
      expiresIn: 300,
    });

    let sessionExpiredCalled = false;
    SessionManager.subscribeSessionExpired(() => {
      sessionExpiredCalled = true;
    });

    globalThis.fetch = jest.fn(async () => {
      throw new Error("Network request failed - Offline");
    });

    await expect(SessionManager.refreshSingleFlight()).rejects.toThrow(ApiClientError);

    expect(sessionExpiredCalled).toBe(false);

    const stored = await SecureStoreAdapter.getSession();
    expect(stored).not.toBeNull();
    expect(stored?.refreshToken).toBe("keep_this_refresh_token");
  });

  test("Logout trong lúc ghi SecureStore bị delay -> Epoch mismatch hủy kết quả, không hồi sinh phiên", async () => {
    const pair = {
      tokenType: "Bearer",
      accessToken: "initial_access",
      refreshToken: "initial_refresh",
      expiresIn: 900,
    };
    await SessionManager.setSession(pair);

    let resolveSave!: () => void;
    const savePromise = new Promise<void>((done) => {
      resolveSave = done;
    });

    const originalSave = SecureStoreAdapter.saveSession.bind(SecureStoreAdapter);
    jest.spyOn(SecureStoreAdapter, "saveSession").mockImplementation(async (params) => {
      await savePromise;
      return originalSave(params);
    });

    globalThis.fetch = jest.fn(async () =>
      new Response(
        JSON.stringify({
          tokenType: "Bearer",
          accessToken: "refreshed_zombie",
          refreshToken: "initial_refresh",
          expiresIn: 900,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      )
    );

    const refreshTask = SessionManager.refreshSingleFlight();
    // Đăng xuất trong lúc saveSession đang bị hoãn
    await SessionManager.invalidateSession();

    // Cho phép saveSession hoàn tất
    resolveSave();
    const refreshResult = await refreshTask;

    expect(refreshResult).toBeNull();
    expect(SessionManager.getSession()).toBeNull();
    expect(await SecureStoreAdapter.getSession()).toBeNull();
  });
});
