import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";
import { mobileLogin, fetchCurrentAccess, logout } from "../src/api/auth";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("Auth Flow & Account Switch - Bảo mật danh tính và cách ly cache", () => {
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

  test("Đăng nhập tài khoản A -> Lưu token và truy vấn quyền hạn thành công", async () => {
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();

      if (url.includes("/api/v1/auth/mobile/login")) {
        return new Response(
          JSON.stringify({
            tokenType: "Bearer",
            accessToken: "token_user_a",
            refreshToken: "refresh_user_a",
            expiresIn: 900,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }

      if (url.includes("/api/v1/access/me")) {
        return new Response(
          JSON.stringify({
            id: "user-a-guid",
            username: "nhanvienA",
            roles: ["Employee"],
            permissions: ["attendance.read_own", "leaves.read_own"],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }

      return new Response("Not found", { status: 404 });
    });

    const tokens = await mobileLogin({ username: "nhanvienA", password: "Password@12345" });
    expect(tokens.accessToken).toBe("token_user_a");

    const access = await fetchCurrentAccess();
    expect(access.username).toBe("nhanvienA");
    expect(access.permissions).toContain("attendance.read_own");
    expect(access.permissions).not.toContain("leaves.approve");
  });

  test("Đổi tài khoản từ User A sang User B -> Xóa sạch cache, không để lộ dữ liệu", async () => {
    let currentUserInBackend = "A";

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();

      if (url.includes("/api/v1/auth/mobile/login")) {
        if (currentUserInBackend === "A") {
          return new Response(
            JSON.stringify({
              tokenType: "Bearer",
              accessToken: "token_user_a",
              refreshToken: "refresh_user_a",
              expiresIn: 900,
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        } else {
          return new Response(
            JSON.stringify({
              tokenType: "Bearer",
              accessToken: "token_user_b",
              refreshToken: "refresh_user_b",
              expiresIn: 900,
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
      }

      if (url.includes("/api/v1/auth/logout")) {
        return new Response(null, { status: 204 });
      }

      if (url.includes("/api/v1/access/me")) {
        if (currentUserInBackend === "A") {
          return new Response(
            JSON.stringify({
              id: "user-a-guid",
              username: "nhanvienA",
              roles: ["Employee"],
              permissions: ["attendance.read_own"],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        } else {
          return new Response(
            JSON.stringify({
              id: "user-b-guid",
              username: "quanlyB",
              roles: ["HR"],
              permissions: ["attendance.read_all", "leaves.approve"],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
      }

      return new Response("Not found", { status: 404 });
    });

    // 1. User A đăng nhập
    await mobileLogin({ username: "nhanvienA", password: "Password@12345" });
    const accessA = await fetchCurrentAccess();
    expect(accessA.username).toBe("nhanvienA");

    // 2. User A đăng xuất
    const logoutRes = await logout();
    expect(logoutRes.serverConfirmed).toBe(true);

    // Kiểm tra: Bộ nhớ phiên và SecureStore đã bị xóa sạch
    expect(SessionManager.getSession()).toBeNull();
    const storedAfterLogout = await SecureStoreAdapter.getSession();
    expect(storedAfterLogout).toBeNull();

    // 3. User B đăng nhập trên cùng thiết bị
    currentUserInBackend = "B";
    await mobileLogin({ username: "quanlyB", password: "Password@12345" });
    const accessB = await fetchCurrentAccess();

    expect(accessB.username).toBe("quanlyB");
    expect(accessB.roles).toContain("HR");
    expect(accessB.permissions).toContain("leaves.approve");
    expect(accessB.id).not.toBe(accessA.id);
  });

  test("Khôi phục phiên (Restore) khi mở lại app có phiên hợp lệ", async () => {
    await SecureStoreAdapter.saveSession({
      tokenType: "Bearer",
      accessToken: "persisted_token",
      refreshToken: "persisted_refresh",
      expiresIn: 600,
    });

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url.includes("/api/v1/access/me")) {
        return new Response(
          JSON.stringify({
            id: "persisted-user-id",
            username: "persistedUser",
            roles: ["Employee"],
            permissions: ["calendar.read"],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }
      return new Response("Not found", { status: 404 });
    });

    const restored = await SessionManager.initialize();
    expect(restored).not.toBeNull();
    expect(restored?.accessToken).toBe("persisted_token");

    const access = await fetchCurrentAccess();
    expect(access.username).toBe("persistedUser");
  });
});
