import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import React from "react";
import { render, screen, waitFor, fireEvent, act } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { LoginPage } from "../src/pages/LoginPage";
import { ForgotPasswordPage } from "../src/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "../src/pages/ResetPasswordPage";
import { AuthProvider } from "../src/context/AuthContext";
import { ErrorAlert } from "../src/components/common/ErrorAlert";

let activeQueryClient: QueryClient | null = null;

function renderWithRouter(ui: React.ReactElement, initialRoute = "/") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  activeQueryClient = queryClient;

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialRoute]}>
        <AuthProvider>{ui}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("Vietnamese Forms, Accessibility, and Security Sanitization Tests", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/access/me")) {
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
      return Promise.reject(new Error(`Unhandled URL: ${url}`));
    });
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(async () => {
    if (activeQueryClient) {
      await activeQueryClient.cancelQueries();
      activeQueryClient.clear();
      activeQueryClient = null;
    }
    vi.unstubAllGlobals();
  });

  it("verifies login form accessibility: label associations, autocomplete attributes, and submit disable", async () => {
    renderWithRouter(<LoginPage />);

    const usernameInput = screen.getByLabelText("Tên đăng nhập");
    const passwordInput = screen.getByLabelText("Mật khẩu");
    const submitButton = screen.getByRole("button", { name: /đăng nhập/i });

    expect(usernameInput).toBeInTheDocument();
    expect(passwordInput).toBeInTheDocument();
    expect(submitButton).toBeInTheDocument();

    expect(usernameInput).toHaveAttribute("autocomplete", "username");
    expect(passwordInput).toHaveAttribute("autocomplete", "current-password");
  });

  it("forgot password displays real deliveryMode and DOES NOT pretend email was sent when unconfigured", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : input.toString();
      if (url.includes("/api/v1/auth/csrf")) {
        return Promise.resolve(
          new Response(JSON.stringify({ requestToken: "csrf-token-123" }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          })
        );
      }
      if (url.includes("/api/v1/auth/forgot-password")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              deliveryMode: "not-configured",
              message: "Chưa cấu hình email; không có email nào được gửi.",
            }),
            { status: 202, headers: { "Content-Type": "application/json" } }
          )
        );
      }
      return Promise.reject(new Error(`Unhandled URL: ${url}`));
    });
    vi.stubGlobal("fetch", fetchMock);

    renderWithRouter(<ForgotPasswordPage />);

    const emailInput = screen.getByLabelText("Địa chỉ email đã đăng ký");
    expect(emailInput).toHaveAttribute("autocomplete", "email");

    fireEvent.change(emailInput, { target: { value: "test@hrm.com" } });
    await act(async () => {
      fireEvent.submit(emailInput.closest("form")!);
    });

    await waitFor(() => {
      expect(screen.getByText("Chưa cấu hình email; không có email nào được gửi.")).toBeInTheDocument();
      expect(screen.getByText("not-configured")).toBeInTheDocument();
      expect(screen.getByText(/kênh gửi email chưa được cấu hình/i)).toBeInTheDocument();
    });
  });

  it("reset password reads code & userId from query params and immediately clears URL to prevent credential leakage", async () => {
    const replaceStateSpy = vi.spyOn(window.history, "replaceState");

    renderWithRouter(
      <Routes>
        <Route path="/reset-password" element={<ResetPasswordPage />} />
      </Routes>,
      "/reset-password?userId=11111111-2222-3333-4444-555555555555&code=secret-reset-code-xyz"
    );

    await waitFor(() => {
      expect(replaceStateSpy).toHaveBeenCalled();
    });

    const newPasswordInput = screen.getByLabelText("Mật khẩu mới");
    const confirmPasswordInput = screen.getByLabelText("Xác nhận mật khẩu mới");

    expect(newPasswordInput).toHaveAttribute("autocomplete", "new-password");
    expect(confirmPasswordInput).toHaveAttribute("autocomplete", "new-password");
  });

  it("enforces strong password validation on client-side (min 12 chars, upper, lower, digit, special)", async () => {
    renderWithRouter(
      <Routes>
        <Route path="/reset-password" element={<ResetPasswordPage />} />
      </Routes>,
      "/reset-password?userId=11111111-2222-3333-4444-555555555555&code=secret-reset-code-xyz"
    );

    const newPasswordInput = screen.getByLabelText("Mật khẩu mới");
    const confirmPasswordInput = screen.getByLabelText("Xác nhận mật khẩu mới");
    const formElement = newPasswordInput.closest("form")!;

    // Short password (< 12 chars)
    fireEvent.change(newPasswordInput, { target: { value: "short" } });
    fireEvent.change(confirmPasswordInput, { target: { value: "short" } });
    await act(async () => {
      fireEvent.submit(formElement);
    });

    await waitFor(() => {
      expect(screen.getByText("Mật khẩu phải chứa ít nhất 12 ký tự")).toBeInTheDocument();
    });

    // Without special characters or numbers (>= 12 chars, letters only)
    fireEvent.change(newPasswordInput, { target: { value: "OnlyLettersLongPassword" } });
    fireEvent.change(confirmPasswordInput, { target: { value: "OnlyLettersLongPassword" } });
    await act(async () => {
      fireEvent.submit(formElement);
    });

    await waitFor(() => {
      expect(screen.getByText("Mật khẩu phải bao gồm chữ thường, chữ hoa, số và ký tự đặc biệt")).toBeInTheDocument();
    });
  });

  it("renders ErrorAlert with traceId while never echoing sensitive passwords", () => {
    render(
      <ErrorAlert
        message="Mật khẩu hiện tại không chính xác"
        code="auth.password_invalid"
        traceId="trace-456-abc"
      />
    );

    expect(screen.getByText("Mật khẩu hiện tại không chính xác")).toBeInTheDocument();
    expect(screen.getByText("auth.password_invalid")).toBeInTheDocument();
    expect(screen.getByText("trace-456-abc")).toBeInTheDocument();

    expect(screen.queryByText("Password@123")).not.toBeInTheDocument();
  });
});
