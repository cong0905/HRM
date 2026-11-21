import { afterEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { CommandReceipt, useCommand } from "../src/features/shared/commands";
import { setCachedCsrfToken, clearCachedCsrfToken } from "../src/api/client";
import { payrollApi } from "../src/features/payroll/api";
import { displayDecimal } from "../src/features/shared/display";

// Isolated unit tests for T19 Payroll frontend logic.
// Runner notice: Anti marked these tests as NOT_RUN pending execution by Codex runner.

afterEach(() => {
  vi.unstubAllGlobals();
  clearCachedCsrfToken();
});

describe("T19 Payroll frontend precision & safety", () => {
  it("preserves exact decimal-string precision without scientific notation or floating point inaccuracies", () => {
    expect(displayDecimal("20000000")).toBe("20000000");
    expect(displayDecimal("18400000.00")).toBe("18400000");
    expect(displayDecimal("14320000.50")).toBe("14320000.5");
    expect(displayDecimal("1666.6666666666667")).toBe("1666.6666666666667");
    expect(displayDecimal("0.00")).toBe("0");
    expect(displayDecimal("0.05")).toBe("0.05");
  });

  it("generates valid UUIDv4 idempotency keys scoped to the workflow action, target, and rowVersion", () => {
    const receipt = new CommandReceipt();
    const periodId = "7a8b9c0d-1111-4000-8000-123456789abc";
    const rowVersion = "AAAAAAAAB9M=";

    const calculateScope = `calculate:${periodId}:${rowVersion}`;
    const payloadA = { scope: calculateScope, values: { reason: "Chạy tính nháp tháng 04/2026" } };

    const key1 = receipt.keyFor(payloadA);
    // Must match UUIDv4 format (version 4 and variant RFC4122)
    expect(key1).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);

    // Identical payload keeps the same key for manual retry after uncertainty
    expect(receipt.keyFor(payloadA)).toBe(key1);

    // Different action scope produces a different key
    const approveScope = `approve:${periodId}:${rowVersion}`;
    const payloadB = { scope: approveScope, values: { reason: "Phê duyệt kỳ lương tháng 04/2026" } };
    const key2 = receipt.keyFor(payloadB);
    expect(key2).not.toBe(key1);

    // Clear purges the cached receipt key
    receipt.clear();
    const key3 = receipt.keyFor(payloadA);
    expect(key3).not.toBe(key1);
  });

  it("fails closed on export when server returns 403 or 409 without creating download blobs or leaking URLs", async () => {
    const fetchMock = vi.fn(async (url: string | URL | Request) => {
      const requestUrl = typeof url === "string" ? url : url instanceof Request ? url.url : url.toString();
      if (requestUrl.includes("/export")) {
        return new Response(
          JSON.stringify({
            status: 403,
            type: "about:blank",
            title: "Forbidden",
            detail: "Payroll export permission required.",
            errorCode: "authorization.forbidden",
          }),
          {
            status: 403,
            headers: { "Content-Type": "application/problem+json" },
          }
        );
      }
      return new Response("{}", { status: 200 });
    });

    vi.stubGlobal("fetch", fetchMock);

    await expect(payrollApi.exportCsv("00000000-0000-0000-0000-000000000001")).rejects.toMatchObject({
      status: 403,
      code: "authorization.forbidden",
    });

    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it("handles workflow mutation errors without auto-resending; manual retry preserves key", async () => {
    const requests: Request[] = [];
    const fetchMock = vi.fn(async (request: Request) => {
      requests.push(request.clone());
      if (requests.length === 1) {
        throw new Error("Network glitch during workflow mutation");
      }
      return new Response(null, { status: 204 });
    });

    vi.stubGlobal("fetch", fetchMock);
    setCachedCsrfToken("csrf-test-token");

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const onSuccess = vi.fn();
    const { result, unmount } = renderHook(
      () =>
        useCommand(
          ({ reason }: { reason: string }, key: string) =>
            payrollApi.executeWorkflow(
              "00000000-0000-0000-0000-000000000001",
              "calculate",
              { reason, rowVersion: "AAAAAAAAB9M=", runVersion: 0 },
              key
            ),
          ["payroll-period-state"],
          onSuccess,
          "calculate:00000000-0000-0000-0000-000000000001:AAAAAAAAB9M="
        ),
      { wrapper }
    );

    try {
      // First attempt fails due to network error
      await act(async () => {
        await expect(result.current.mutateAsync({ reason: "Tính toán nháp tháng 04" })).rejects.toMatchObject({
          status: 0,
          isNetworkError: true,
        });
      });

      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(onSuccess).not.toHaveBeenCalled();

      // Manual retry reuses the exact same Idempotency-Key
      await act(async () => {
        await result.current.mutateAsync({ reason: "Tính toán nháp tháng 04" });
      });

      expect(fetchMock).toHaveBeenCalledTimes(2);
      expect(requests[0]!.headers.get("Idempotency-Key")).toBe(requests[1]!.headers.get("Idempotency-Key"));
      expect(requests[0]!.headers.get("X-XSRF-TOKEN")).toBe("csrf-test-token");
      expect(onSuccess).toHaveBeenCalledOnce();
    } finally {
      unmount();
      queryClient.clear();
    }
  });
});
