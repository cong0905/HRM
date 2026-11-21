import { DataCacheManager } from "../src/storage/cache";
import { logout } from "../src/api/auth";
import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("Cache Isolation & Logout Wipe - Bảo mật phân tách dữ liệu tài khoản", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(async () => {
    jest.restoreAllMocks();
    SecureStoreAdapter.setMockMode(true);
    SecureStoreAdapter.resetMockStore();
    await SessionManager.resetForTesting();
    DataCacheManager.clearAll();
    process.env.EXPO_PUBLIC_API_URL = "https://api.example.invalid";
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test("Logout xóa sạch toàn bộ dữ liệu đã cache (Wipe cache on logout)", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "token_user_1",
      refreshToken: "refresh_user_1",
      expiresIn: 3600,
    });

    DataCacheManager.setActiveUser("user-1", SessionManager.getEpoch());
    DataCacheManager.set("payroll_me_payslips", { privateSlip: "15,000,000 VND" });
    DataCacheManager.set("personal_me_profile", { fullName: "Nguyen Van A" });

    expect(DataCacheManager.size()).toBe(2);
    expect(DataCacheManager.get("payroll_me_payslips")).toEqual({ privateSlip: "15,000,000 VND" });

    globalThis.fetch = jest.fn(async () => new Response(null, { status: 204 }));

    // Thực hiện logout
    await logout();

    // Toàn bộ cache phải bị xóa sạch, kích thước về 0
    expect(DataCacheManager.size()).toBe(0);
    expect(DataCacheManager.get("payroll_me_payslips")).toBeNull();
    expect(DataCacheManager.get("personal_me_profile")).toBeNull();
  });

  test("Chuyển đổi tài khoản (Two-Account Isolation) không để lộ dữ liệu người trước", async () => {
    // 1. User A đăng nhập và có cache
    DataCacheManager.setActiveUser("user-A-id", 1);
    DataCacheManager.set("payroll_me_payslips", { sensitive: "User A Salary" });

    expect(DataCacheManager.get("payroll_me_payslips")).toEqual({ sensitive: "User A Salary" });

    // 2. Chuyển sang User B (hoặc logout và login user B)
    DataCacheManager.setActiveUser("user-B-id", 2);

    // Dữ liệu của User A tuyệt đối không được truy xuất bởi User B
    expect(DataCacheManager.get("payroll_me_payslips")).toBeNull();
  });

  test("Cache hết hạn TTL tự động bị loại bỏ", async () => {
    DataCacheManager.setActiveUser("user-test", 1);
    // Set với TTL 10ms
    DataCacheManager.set("temp_key", "temporary_value", 10);

    expect(DataCacheManager.get("temp_key")).toBe("temporary_value");

    // Đợi 25ms
    await new Promise((resolve) => setTimeout(resolve, 25));

    expect(DataCacheManager.get("temp_key")).toBeNull();
  });
});
