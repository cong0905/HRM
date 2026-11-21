import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import React, { useState } from "react";
import { render, screen, waitFor, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider, useAuth } from "../src/context/AuthContext";
import { App } from "../src/App";

let activeQueryClient: QueryClient | null = null;

function renderAppWithInitialRoute(
  initialEntries: string[],
  userProfile: {
    id: string;
    username: string;
    roles: string[];
    permissions: string[];
  } | null
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });
  activeQueryClient = queryClient;

  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = input instanceof Request ? input.url : input.toString();
    if (url.includes("/api/v1/access/me")) {
      if (!userProfile) {
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
        new Response(JSON.stringify(userProfile), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        })
      );
    }
    return Promise.reject(new Error(`Unhandled URL: ${url}`));
  });
  vi.stubGlobal("fetch", fetchMock);

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <AuthProvider>
          <App />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

// Consumer component to test Session Cache regressions
const SessionCacheProbe: React.FC = () => {
  const { isAuthenticated, retryBootstrap, logout } = useAuth();
  const [logoutErrorStatus, setLogoutErrorStatus] = useState<number | null>(null);

  const handleLogout = async () => {
    try {
      await logout();
    } catch (err: unknown) {
      if (err && typeof err === "object" && "status" in err) {
        setLogoutErrorStatus((err as { status: number }).status);
      } else {
        setLogoutErrorStatus(-1);
      }
    }
  };

  return (
    <div>
      <div data-testid="is-authenticated">{isAuthenticated ? "yes" : "no"}</div>
      <div data-testid="logout-error-status">{logoutErrorStatus !== null ? String(logoutErrorStatus) : "none"}</div>
      <button onClick={() => retryBootstrap()} data-testid="retry-bootstrap-btn">
        Retry Bootstrap
      </button>
      <button onClick={handleLogout} data-testid="logout-btn">
        Logout
      </button>
    </div>
  );
};

describe("Permission-Based Navigation and Route Protection Tests", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(async () => {
    if (activeQueryClient) {
      await activeQueryClient.cancelQueries();
      activeQueryClient.clear();
      activeQueryClient = null;
    }
    vi.unstubAllGlobals();
  });

  it("unauthenticated user trying to access /personnel is redirected to /login", async () => {
    renderAppWithInitialRoute(["/personnel"], null);

    await waitFor(() => {
      expect(screen.getByText("Đăng nhập HRM Next")).toBeInTheDocument();
      expect(screen.getByLabelText("Tên đăng nhập")).toBeInTheDocument();
    });
  });

  it("SystemAdmin without HR/Payroll permissions DOES NOT see Personnel or Payroll menu items", async () => {
    // Literal permissions from backend HrmPermissions.RolePermissions[HrmRoles.SystemAdmin]
    const systemAdminUser = {
      id: "admin-id-1",
      username: "sysadmin",
      roles: ["SystemAdmin"],
      permissions: [
        "calendar.read",
        "calendar.manage",
        "departments.read",
        "departments.manage",
        "accounts.manage",
        "accounts.assign_roles",
        "hr.scope_manage",
        "audit.read",
      ],
    };

    renderAppWithInitialRoute(["/"], systemAdminUser);

    await waitFor(() => {
      expect(screen.getByText("Chào mừng trở lại, sysadmin!")).toBeInTheDocument();
    });

    const navigation = screen.getByLabelText("Điều hướng chính");
    expect(navigation.querySelector('a[href="/"]')).toHaveTextContent("Tổng quan");
    expect(navigation.querySelector('a[href="/admin"]')).toHaveTextContent("Quản trị");

    // SystemAdmin must not inherit HR or Payroll navigation by default.
    expect(navigation.querySelector('a[href="/personnel"]')).not.toBeInTheDocument();
    expect(navigation.querySelector('a[href="/payroll"]')).not.toBeInTheDocument();
    expect(screen.queryByText("Hồ sơ Nhân sự")).not.toBeInTheDocument();
    expect(screen.queryByText("Bảng lương")).not.toBeInTheDocument();
  });

  it("User with employees.read sees Personnel menu item", async () => {
    const hrUser = {
      id: "hr-id-1",
      username: "hr_manager",
      roles: ["Hr"],
      permissions: ["employees.read", "employees.manage", "departments.read"],
    };

    renderAppWithInitialRoute(["/"], hrUser);

    await waitFor(() => {
      expect(screen.getByText("Chào mừng trở lại, hr_manager!")).toBeInTheDocument();
    });

    expect(screen.getByLabelText("Điều hướng chính").querySelector('a[href="/personnel"]')).toHaveTextContent("Nhân sự");
  });

  it("renders 403 Forbidden screen when user attempts direct URL to unauthorized route", async () => {
    // User only has attendance permission, but navigates to /personnel
    const attendanceUser = {
      id: "emp-id-1",
      username: "attendance_clerk",
      roles: ["Employee"],
      permissions: ["attendance.read_own"],
    };

    renderAppWithInitialRoute(["/personnel"], attendanceUser);

    await waitFor(() => {
      expect(screen.getByText("403 - Không có quyền truy cập")).toBeInTheDocument();
    });
  });

  it("renders the personnel API failure without fake employees on an authorized route", async () => {
    const hrUser = {
      id: "hr-id-1",
      username: "hr_specialist",
      roles: ["Hr"],
      permissions: ["employees.read"],
    };

    renderAppWithInitialRoute(["/personnel"], hrUser);

    await waitFor(() => {
      expect(screen.getAllByText("Đã xảy ra sự cố").length).toBeGreaterThan(0);
      expect(screen.getByRole("region", { name: "Danh sách nhân viên" })).toBeInTheDocument();
    });

    // Verification: Absolutely NO fake data counters or fake tables
    expect(screen.queryByText("Tổng số nhân viên: 150")).not.toBeInTheDocument();
    expect(screen.queryByText("Nguyễn Văn A")).not.toBeInTheDocument();
  // This route now mounts the full AntD register instead of the former placeholder.
  // Real layout/performance is checked in Playwright; allow the jsdom table mount to complete.
  }, 15000);

  // --- REGRESSION CHECKS FOR CODEX REVIEW FINDINGS 3 & 4 ---

  it("Regression Finding 3: purges private cache when bootstrap discovers the old session expired", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    let isExpired = false;

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        if (isExpired) {
          return Promise.resolve(
            new Response(
              JSON.stringify({ status: 401, title: "Unauthorized", errorCode: "auth.unauthorized", traceId: "t-3" }),
              { status: 401, headers: { "Content-Type": "application/problem+json" } }
            )
          );
        }
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: "fixture-user",
              username: "fixture",
              roles: ["Employee"],
              permissions: ["payroll.read_own"],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          )
        );
      }
      return Promise.reject(new Error("Unhandled"));
    });
    vi.stubGlobal("fetch", fetchMock);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuthProvider>
            <SessionCacheProbe />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId("is-authenticated").textContent).toBe("yes");
    });

    // Set private data in cache
    queryClient.setQueryData(["private", "salary"], { privateAmount: "12000000" });
    expect(queryClient.getQueryData(["private", "salary"])).toBeDefined();

    // Session expires
    isExpired = true;
    await act(async () => {
      screen.getByTestId("retry-bootstrap-btn").click();
    });

    await waitFor(() => {
      expect(screen.getByTestId("is-authenticated").textContent).toBe("no");
    });

    // Finding 3 requirement: private cache must be purged!
    expect(queryClient.getQueryData(["private", "salary"])).toBeUndefined();
  });

  it("Regression Finding 4: clears local private cache but reports a failed server logout instead of claiming success", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: "fixture-user",
              username: "fixture",
              roles: ["Employee"],
              permissions: ["payroll.read_own"],
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
        // Server fails with 503
        return Promise.resolve(
          new Response(
            JSON.stringify({
              status: 503,
              title: "Service Unavailable",
              errorCode: "server.unavailable",
              traceId: "t-logout-fail",
            }),
            { status: 503, headers: { "Content-Type": "application/problem+json" } }
          )
        );
      }
      return Promise.reject(new Error("Unhandled"));
    });
    vi.stubGlobal("fetch", fetchMock);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuthProvider>
            <SessionCacheProbe />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId("is-authenticated").textContent).toBe("yes");
    });

    queryClient.setQueryData(["private", "salary"], { privateAmount: "12000000" });

    // Trigger logout which will reject with 503
    await act(async () => {
      screen.getByTestId("logout-btn").click();
    });

    await waitFor(() => {
      expect(screen.getByTestId("logout-error-status").textContent).toBe("503");
    });

    // Finding 4 requirement: local private cache purged and failure reported
    expect(queryClient.getQueryData(["private", "salary"])).toBeUndefined();
  });
});
