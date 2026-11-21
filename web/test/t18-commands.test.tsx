import { afterEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { CommandReceipt, useCommand } from "../src/features/shared/commands";
import { setCachedCsrfToken, clearCachedCsrfToken } from "../src/api/client";
import { attendanceApi, businessInstant, businessTimeInput } from "../src/features/attendance/api";
import { periodMonths } from "../src/features/shared/PeriodGuard";

afterEach(() => { vi.unstubAllGlobals(); clearCachedCsrfToken(); });
describe("T18 command safety", () => {
  it("retains original server precision when an HR edit leaves a timestamp unchanged", () => {
    const original = "2026-11-13T01:30:45.1234567+00:00";
    expect(businessTimeInput(original)).toBe("2026-11-13T08:30:45.123");
    expect(businessInstant(businessTimeInput(original), original)).toBe(original);
    expect(businessInstant("2026-11-13T17:30:00.000")).toBe("2026-11-13T17:30:00.000+07:00");
  });
  it("uses separate receipts for different targets and does not carry one across successful commands", () => {
    const receipt = new CommandReceipt();
    const input = { scope: "approve:leave-a:version1", values: { reason: "Reviewed" } };
    const first = receipt.keyFor(input);
    expect(first).toMatch(/^[\da-f]{8}-[\da-f]{4}-4[\da-f]{3}-[89ab][\da-f]{3}-[\da-f]{12}$/i);
    expect(receipt.keyFor(input)).toBe(first);
    expect(receipt.keyFor({ ...input, scope: "approve:leave-b:version1" })).not.toBe(first);
    receipt.clear(); expect(receipt.keyFor(input)).not.toBe(first);
  });
  it("checks every affected payroll month and rejects unsupported cross-year ranges", () => {
    expect(periodMonths("2026-11-30", "2026-12-02")).toEqual([[2026, 11], [2026, 12]]);
    expect(periodMonths("2026-12-31", "2027-01-01")).toEqual([]);
    expect(periodMonths("2026-11-20", "2026-11-19")).toEqual([]);
    expect(periodMonths()).toEqual([]);
  });
  it("does not auto resend a network-failed attendance command; manual retry uses the same key and no client timestamp", async () => {
    const requests: Request[] = [];
    const fetch = vi.fn(async (request: Request) => {
      requests.push(request.clone());
      if (requests.length === 1) throw new Error("Lost connection after server may have committed");
      return new Response(JSON.stringify({ id: "fixture-attendance" }), { headers: { "Content-Type": "application/json" } });
    });
    vi.stubGlobal("fetch", fetch); setCachedCsrfToken("fixture-csrf");
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const wrapper = ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    const success = vi.fn();
    const { result, unmount } = renderHook(() => useCommand(({ note }: { note: string }, key) => attendanceApi.check("in", note, key), ["attendance"], success, "self:check-in"), { wrapper });
    try {
      await act(async () => { await expect(result.current.mutateAsync({ note: "Arrival" })).rejects.toMatchObject({ status: 0 }); });
      expect(fetch).toHaveBeenCalledTimes(1); expect(success).not.toHaveBeenCalled();
      await act(async () => { await result.current.mutateAsync({ note: "Arrival" }); });
      expect(fetch).toHaveBeenCalledTimes(2);
      expect(requests[0]!.headers.get("Idempotency-Key")).toBe(requests[1]!.headers.get("Idempotency-Key"));
      expect(requests[0]!.headers.get("X-XSRF-TOKEN")).toBe("fixture-csrf");
      expect(await requests[0]!.json()).toEqual({ note: "Arrival" });
      expect(success).toHaveBeenCalledOnce();
    } finally { unmount(); client.clear(); }
  });
});
