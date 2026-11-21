import { mobileFetch } from "../src/api/client";
import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";
import { ApiClientError } from "../src/api/errors";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("mobileFetch - Client mạng, bảo vệ nguồn gốc và retry an toàn", () => {
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

  test("Chặn tuyệt đối yêu cầu gửi đến nguồn gốc HTTPS khác (Cross-Origin Protection)", async () => {
    const fetcher = jest.fn();
    globalThis.fetch = fetcher;

    await expect(mobileFetch("https://other.example.invalid/api/v1/access/me")).rejects.toThrow();
    // Fetcher không bao giờ được gọi
    expect(fetcher).not.toHaveBeenCalled();
  });

  test("Đính kèm Bearer token vào Authorization header cho request nghiệp vụ", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "my_secret_bearer_token",
      refreshToken: "my_refresh_token",
      expiresIn: 900,
    });

    let capturedHeaders: Headers | undefined;
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      capturedHeaders = new Headers(init?.headers);
      return new Response(JSON.stringify({ success: true }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });

    await mobileFetch("/api/v1/access/me");

    expect(capturedHeaders).toBeDefined();
    expect(capturedHeaders?.get("Authorization")).toBe("Bearer my_secret_bearer_token");
  });

  test("Response trả về sau khi đổi tài khoản bị hủy, không rò rỉ dữ liệu cũ", async () => {
    let resolvePendingFetch!: (value: Response) => void;
    const fetchPromise = new Promise<Response>((done) => {
      resolvePendingFetch = done;
    });

    globalThis.fetch = jest.fn(async () => fetchPromise);

    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "token_account_A",
      refreshToken: "refresh_A",
      expiresIn: 900,
    });

    // 1. Gửi request của tài khoản A
    const requestPromise = mobileFetch("/api/v1/access/me");

    // 2. Tài khoản A đăng xuất và tài khoản B đăng nhập
    await SessionManager.invalidateSession();
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "token_account_B",
      refreshToken: "refresh_B",
      expiresIn: 900,
    });

    // 3. Phản hồi của tài khoản A mới hoàn tất từ mạng
    resolvePendingFetch(
      new Response(
        JSON.stringify({ id: "account-A-id", username: "userA", permissions: ["private_data"] }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      )
    );

    // Kiểm chứng: Request của tài khoản A phải bị reject do Epoch thay đổi
    await expect(requestPromise).rejects.toThrow();
  });

  test("Retry lần 2 sau khi refresh vẫn bị 401 -> Hủy toàn bộ credentials", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "expired_token",
      refreshToken: "valid_refresh",
      expiresIn: 10,
    });

    globalThis.fetch = jest
      .fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({}), { status: 401 }))
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            tokenType: "Bearer",
            accessToken: "new_token",
            refreshToken: "new_refresh",
            expiresIn: 900,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        )
      )
      .mockResolvedValueOnce(new Response(JSON.stringify({}), { status: 401 }));

    await expect(mobileFetch("/api/v1/access/me")).rejects.toThrow();

    // Toàn bộ session trong SessionManager và SecureStore bị xóa sạch
    expect(SessionManager.getSession()).toBeNull();
    expect(await SecureStoreAdapter.getSession()).toBeNull();
  });

  test("Gặp 401 trên mutation POST không có Idempotency-Key -> Không tự ý replay", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "expired_token",
      refreshToken: "valid_refresh",
      expiresIn: 10,
    });

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      const url = input.toString();
      if (url.includes("/api/v1/auth/mobile/refresh")) {
        return new Response(
          JSON.stringify({
            tokenType: "Bearer",
            accessToken: "new_token",
            refreshToken: "new_refresh",
            expiresIn: 900,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }
      return new Response(JSON.stringify({ status: 401, errorCode: "auth.invalid" }), {
        status: 401,
        headers: { "Content-Type": "application/problem+json" },
      });
    });

    await expect(
      mobileFetch("/api/v1/leave-requests", {
        method: "POST",
        body: JSON.stringify({ reason: "Nghi phep" }),
      })
    ).rejects.toMatchObject({ code: "auth.reauthenticated_mutation_requires_retry" });
  });

  test("Gặp 401 trên mutation POST CÓ Idempotency-Key -> Retry an toàn 1 lần", async () => {
    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "expired_token",
      refreshToken: "valid_refresh",
      expiresIn: 10,
    });

    let mutationCallCount = 0;
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = input.toString();
      if (url.includes("/api/v1/auth/mobile/refresh")) {
        return new Response(
          JSON.stringify({
            tokenType: "Bearer",
            accessToken: "new_token",
            refreshToken: "new_refresh",
            expiresIn: 900,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }

      if (url.includes("/api/v1/attendance/me/check-in")) {
        mutationCallCount++;
        const headers = new Headers(init?.headers);
        if (headers.get("Authorization") === "Bearer expired_token") {
          return new Response(
            JSON.stringify({ status: 401, errorCode: "auth.invalid" }),
            { status: 401, headers: { "Content-Type": "application/problem+json" } }
          );
        }
        return new Response(JSON.stringify({ success: true, timestamp: "2026-11-15T08:30:00Z" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }

      return new Response("Not found", { status: 404 });
    });

    const res = await mobileFetch<{ success: boolean }>("/api/v1/attendance/me/check-in", {
      method: "POST",
      headers: {
        "Idempotency-Key": "d82f7c00-4b4e-4b68-9ce2-bc17f40cf9a1",
      },
      body: JSON.stringify({}),
    });

    expect(mutationCallCount).toBe(2);
    expect(res.success).toBe(true);
  });
});
