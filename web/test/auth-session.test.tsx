import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import React from "react";
import { render, screen, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider, useAuth } from "../src/context/AuthContext";
import { ApiClientError } from "../src/api/types";
import {
  parseErrorMessage,
  clearCachedCsrfToken,
  setCachedCsrfToken,
  getCachedCsrfToken,
  secureFetch,
} from "../src/api/client";

// Test helper component displaying auth state
const TestAuthConsumer: React.FC = () => {
  const { user, isAuthenticated, isLoading, isError, error, login, logout, changePassword, retryBootstrap } = useAuth();

  return (
    <div>
      <div data-testid="auth-loading">{isLoading ? "loading" : "idle"}</div>
      <div data-testid="auth-status">{isAuthenticated ? "authenticated" : "unauthenticated"}</div>
      <div data-testid="auth-username">{user?.username || "none"}</div>
      <div data-testid="auth-error">{isError ? (error as Error)?.message : "no-error"}</div>

      <button
        onClick={() => login({ username: "admin", password: "Password@123456" })}
        data-testid="login-btn"
      >
        Login
      </button>

      <button onClick={() => logout()} data-testid="logout-btn">
        Logout
      </button>

      <button
        onClick={() =>
          changePassword({
            currentPassword: "Password@123456",
            newPassword: "NewPassword@123456",
          })
        }
        data-testid="change-password-btn"
      >
        Change Password
      </button>

      <button onClick={() => retryBootstrap()} data-testid="retry-btn">
        Retry
      </button>
    </div>
  );
};

let activeQueryClient: QueryClient | null = null;

function renderWithProviders(
  ui: React.ReactElement,
  queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  })
) {
  activeQueryClient = queryClient;
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AuthProvider>{ui}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("Authentication & Session Management Tests (Isolated Unit/Component)", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    clearCachedCsrfToken();
  });

  afterEach(async () => {
    if (activeQueryClient) {
      await activeQueryClient.cancelQueries();
      activeQueryClient.clear();
      activeQueryClient = null;
    }
    vi.unstubAllGlobals();
  });

  it("handles 401 on /api/v1/access/me as normal anonymous visitor without infinite retry loops", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({ title: "Unauthorized", status: 401, errorCode: "auth.unauthorized", traceId: "t-1" }),
            {
              status: 401,
              headers: { "Content-Type": "application/problem+json" },
            }
          )
        );
      }
      return Promise.reject(new Error(`Unexpected call to ${url}`));
    });
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<TestAuthConsumer />);

    expect(screen.getByTestId("auth-loading").textContent).toBe("loading");

    await waitFor(() => {
      expect(screen.getByTestId("auth-loading").textContent).toBe("idle");
    });

    expect(screen.getByTestId("auth-status").textContent).toBe("unauthenticated");
    expect(screen.getByTestId("auth-username").textContent).toBe("none");
    expect(screen.getByTestId("auth-error").textContent).toBe("no-error");

    const accessCalls = fetchMock.mock.calls.filter(([input]) => {
      const u = input instanceof Request ? input.url : input.toString();
      return u.includes("/api/v1/access/me");
    });
    expect(accessCalls.length).toBe(1);
  });

  it("completes full login sequence: CSRF fetch -> web/login -> fresh CSRF refresh -> bootstrap access", async () => {
    const user = userEvent.setup();
    let currentSessionUser: { id: string; username: string; roles: string[]; permissions: string[] } | null = null;
    let csrfTokenCounter = 1;

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = input instanceof Request ? input.url : input.toString();
      const method = (init?.method || (input instanceof Request ? input.method : "GET")).toUpperCase();

      if (url.includes("/api/v1/access/me")) {
        if (!currentSessionUser) {
          return Promise.resolve(
            new Response(
              JSON.stringify({
                status: 401,
                errorCode: "authentication.required",
                traceId: "test-anon",
                title: "Unauthorized",
              }),
              {
                status: 401,
                headers: { "Content-Type": "application/problem+json" },
              }
            )
          );
        }
        return Promise.resolve(
          new Response(JSON.stringify(currentSessionUser), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          })
        );
      }

      if (url.includes("/api/v1/auth/csrf")) {
        const tokenValue = `token-${csrfTokenCounter++}`;
        return Promise.resolve(
          new Response(JSON.stringify({ requestToken: tokenValue }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          })
        );
      }

      if (url.includes("/api/v1/auth/web/login") && method === "POST") {
        const headers = new Headers(input instanceof Request ? input.headers : init?.headers);
        expect(headers.get("X-XSRF-TOKEN")).toBeTruthy();

        currentSessionUser = {
          id: "11111111-1111-1111-1111-111111111111",
          username: "admin",
          roles: ["SystemAdmin"],
          permissions: ["accounts.manage"],
        };
        return Promise.resolve(new Response(null, { status: 204 }));
      }

      return Promise.reject(new Error(`Unhandled URL: ${url}`));
    });

    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<TestAuthConsumer />);

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("unauthenticated");
    });

    await act(async () => {
      await user.click(screen.getByTestId("login-btn"));
    });

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("authenticated");
      expect(screen.getByTestId("auth-username").textContent).toBe("admin");
    });

    const csrfCalls = fetchMock.mock.calls.filter(([input]) => {
      const u = input instanceof Request ? input.url : input.toString();
      return u.includes("/api/v1/auth/csrf");
    });
    expect(csrfCalls.length).toBeGreaterThanOrEqual(2);
  });

  it("cancels queries and clears cache on logout so previous session PII cannot repopulate", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false, gcTime: 0 } },
    });

    const cancelSpy = vi.spyOn(queryClient, "cancelQueries");

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: "user-id-99",
              username: "employee_alice",
              roles: ["Employee"],
              permissions: ["employees.read"],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          )
        );
      }
      if (url.includes("/api/v1/auth/csrf")) {
        return Promise.resolve(
          new Response(JSON.stringify({ requestToken: "mock-csrf" }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          })
        );
      }
      if (url.includes("/api/v1/auth/logout")) {
        return Promise.resolve(new Response(null, { status: 204 }));
      }
      return Promise.reject(new Error(`Unhandled: ${url}`));
    });
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<TestAuthConsumer />, queryClient);

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("authenticated");
    });

    await act(async () => {
      await user.click(screen.getByTestId("logout-btn"));
    });

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("unauthenticated");
      expect(screen.getByTestId("auth-username").textContent).toBe("none");
    });

    expect(cancelSpy).toHaveBeenCalled();
    expect(getCachedCsrfToken()).toBeNull();
  });

  it("handles password change 204 by invalidating session and cache", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false, gcTime: 0 } },
    });

    queryClient.setQueryData(["private", "salary"], { amount: "50000000" });
    expect(queryClient.getQueryData(["private", "salary"])).toBeDefined();

    let isAuthenticatedSession = true;

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        if (!isAuthenticatedSession) {
          return Promise.resolve(
            new Response(
              JSON.stringify({ title: "Unauthorized", status: 401, errorCode: "auth.unauthorized" }),
              { status: 401, headers: { "Content-Type": "application/problem+json" } }
            )
          );
        }
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: "user-id-99",
              username: "employee_alice",
              roles: ["Employee"],
              permissions: [],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          )
        );
      }
      if (url.includes("/api/v1/auth/csrf")) {
        return Promise.resolve(
          new Response(JSON.stringify({ requestToken: "mock-csrf" }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          })
        );
      }
      if (url.includes("/api/v1/auth/change-password")) {
        isAuthenticatedSession = false;
        return Promise.resolve(new Response(null, { status: 204 }));
      }
      return Promise.reject(new Error(`Unhandled: ${url}`));
    });
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<TestAuthConsumer />, queryClient);

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("authenticated");
    });

    await act(async () => {
      await user.click(screen.getByTestId("change-password-btn"));
    });

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("unauthenticated");
    });
    expect(queryClient.getQueryData(["private", "salary"])).toBeUndefined();
  });

  it("distinguishes network errors from logout and allows retry without wiping state", async () => {
    let networkErrorActive = true;

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        if (networkErrorActive) {
          return Promise.reject(new TypeError("Failed to fetch"));
        }
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: "user-1",
              username: "bob",
              roles: [],
              permissions: [],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          )
        );
      }
      return Promise.reject(new Error("Unhandled"));
    });
    vi.stubGlobal("fetch", fetchMock);

    renderWithProviders(<TestAuthConsumer />);

    // Allow sufficient timeout (8s) for TanStack Query's intentional 2 retries with exponential backoff (~3s total)
    await waitFor(
      () => {
        expect(screen.getByTestId("auth-error").textContent).not.toBe("no-error");
      },
      { timeout: 8000 }
    );

    expect(screen.getByTestId("auth-error").textContent).toContain("Lỗi kết nối");

    networkErrorActive = false;
    const user = userEvent.setup();
    await act(async () => {
      await user.click(screen.getByTestId("retry-btn"));
    });

    await waitFor(() => {
      expect(screen.getByTestId("auth-status").textContent).toBe("authenticated");
      expect(screen.getByTestId("auth-username").textContent).toBe("bob");
    });
  });

  it("correctly extracts 409 conflict messages and traceId from ProblemDetails", () => {
    const error409 = new ApiClientError({
      message: "Default conflict message",
      status: 409,
      code: "concurrency.conflict",
      traceId: "trace-xyz-123",
      problem: {
        title: "Bản ghi bị xung đột phiên bản",
        detail: "Dữ liệu nhân viên vừa được cập nhật bởi quản trị viên khác. Vui lòng tải lại trang.",
        errorCode: "concurrency.conflict",
        traceId: "trace-xyz-123",
      },
    });

    const parsed = parseErrorMessage(error409);
    expect(parsed.isConflict).toBe(true);
    expect(parsed.message).toBe("Dữ liệu nhân viên vừa được cập nhật bởi quản trị viên khác. Vui lòng tải lại trang.");
    expect(parsed.traceId).toBe("trace-xyz-123");
  });

  // --- REGRESSION CHECKS FOR CODEX REVIEW FINDINGS 1 & 2 ---

  it("Regression Finding 1: preserves Request method/content headers and adds CSRF to generated-client POST", async () => {
    setCachedCsrfToken("review-xsrf");
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    const request = new Request("https://localhost/api/v1/auth/web/login", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Existing": "preserved" },
      body: JSON.stringify({ username: "fixture", password: "synthetic-only" }),
    });

    await secureFetch(request);

    const [input, init] = fetchMock.mock.calls[0]!;
    const forwarded = new Request(input, init);
    expect(forwarded.method).toBe("POST");
    expect(forwarded.headers.get("X-XSRF-TOKEN")).toBe("review-xsrf");
    expect(forwarded.headers.get("Content-Type")).toBe("application/json");
    expect(forwarded.headers.get("X-Existing")).toBe("preserved");
    expect(await forwarded.json()).toEqual({ username: "fixture", password: "synthetic-only" });
  });

  it("Regression Finding 2: recognizes actual backend errorCode csrf.invalid without replaying the mutation", async () => {
    setCachedCsrfToken("stale-xsrf");
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            status: 400,
            title: "Invalid CSRF",
            errorCode: "csrf.invalid",
            traceId: "review-trace",
          }),
          { status: 400, headers: { "Content-Type": "application/problem+json" } }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ requestToken: "fresh-xsrf" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        })
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(secureFetch("https://localhost/api/v1/auth/web/login", { method: "POST" })).rejects.toMatchObject({
      code: "csrf.invalid",
      traceId: "review-trace",
    });

    expect(
      fetchMock.mock.calls.filter(([input, init]) => {
        const m = (init?.method || (input instanceof Request ? input.method : "GET")).toUpperCase();
        return m === "POST";
      })
    ).toHaveLength(1);
  });
});
