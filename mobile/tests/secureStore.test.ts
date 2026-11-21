import { SecureStoreAdapter } from "../src/storage/secureStore";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("SecureStoreAdapter - Quản lý lưu trữ bí mật phiên", () => {
  beforeEach(() => {
    jest.restoreAllMocks();
    SecureStoreAdapter.setMockMode(true);
    SecureStoreAdapter.resetMockStore();
    process.env.EXPO_PUBLIC_API_URL = "https://api.example.invalid";
  });

  test("Lưu trữ và đọc cặp token phiên an toàn", async () => {
    const session = await SecureStoreAdapter.saveSession({
      tokenType: "Bearer",
      accessToken: "opaque_access_token_abc123",
      refreshToken: "opaque_refresh_token_xyz789",
      expiresIn: 900,
    });

    expect(session.tokenType).toBe("Bearer");
    expect(session.accessToken).toBe("opaque_access_token_abc123");
    expect(session.refreshToken).toBe("opaque_refresh_token_xyz789");
    expect(session.expiresAt).toBeGreaterThan(Date.now());

    const retrieved = await SecureStoreAdapter.getSession();
    expect(retrieved).not.toBeNull();
    expect(retrieved?.accessToken).toBe("opaque_access_token_abc123");
    expect(retrieved?.refreshToken).toBe("opaque_refresh_token_xyz789");
    expect(retrieved?.tokenType).toBe("Bearer");
  });

  test("Xác thực TokenPair runtime chặt chẽ (Fail on invalid tokenType or expiresIn)", () => {
    // Sai tokenType
    expect(() =>
      SecureStoreAdapter.validateTokenPair({
        tokenType: "Basic",
        accessToken: "abc",
        refreshToken: "xyz",
        expiresIn: 900,
      })
    ).toThrow("tokenType bắt buộc phải là 'Bearer'");

    // expiresIn không phải số dương hữu hạn
    expect(() =>
      SecureStoreAdapter.validateTokenPair({
        tokenType: "Bearer",
        accessToken: "abc",
        refreshToken: "xyz",
        expiresIn: -10,
      })
    ).toThrow("expiresIn bắt buộc phải là số dương hữu hạn");

    expect(() =>
      SecureStoreAdapter.validateTokenPair({
        tokenType: "Bearer",
        accessToken: "abc",
        refreshToken: "xyz",
        expiresIn: Infinity,
      })
    ).toThrow("expiresIn bắt buộc phải là số dương hữu hạn");

    // accessToken rỗng
    expect(() =>
      SecureStoreAdapter.validateTokenPair({
        tokenType: "Bearer",
        accessToken: "",
        refreshToken: "xyz",
        expiresIn: 900,
      })
    ).toThrow("accessToken không được để trống");
  });

  test("clearSession xóa sạch token khỏi kho lưu trữ", async () => {
    await SecureStoreAdapter.saveSession({
      tokenType: "Bearer",
      accessToken: "token_to_delete",
      refreshToken: "refresh_to_delete",
      expiresIn: 300,
    });

    await SecureStoreAdapter.clearSession();
    const retrieved = await SecureStoreAdapter.getSession();
    expect(retrieved).toBeNull();
  });

  test("Quản lý và tăng thế hệ phiên (Epoch counter)", async () => {
    const initial = await SecureStoreAdapter.getSessionEpoch();
    expect(initial).toBe(0);

    const next1 = await SecureStoreAdapter.incrementSessionEpoch();
    expect(next1).toBe(1);

    const next2 = await SecureStoreAdapter.incrementSessionEpoch();
    expect(next2).toBe(2);

    const current = await SecureStoreAdapter.getSessionEpoch();
    expect(current).toBe(2);
  });
});
