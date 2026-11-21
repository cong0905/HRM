import { afterEach, describe, expect, it, vi } from "vitest";
import { employeeChanges, personnelApi, type Employee } from "../src/features/shared/api";
import { setCachedCsrfToken, clearCachedCsrfToken } from "../src/api/client";

const original: Employee = { id: "11111111-1111-1111-1111-111111111111", userId: "22222222-2222-2222-2222-222222222222", employeeCode: "FIXTURE", status: "DangLamViec", fullName: "Fixture employee", citizenId: "fixture-citizen", phone: "0901234567", address: "Original address", departmentId: null, rowVersion: "AAAAAAAAB9E=", gender: "Nam", positionId: null };
afterEach(() => { vi.unstubAllGlobals(); clearCachedCsrfToken(); });
describe("T17 personnel transport invariants", () => {
  it("sends only changed editable properties and preserves omitted/null differences", () => {
    expect(employeeChanges(original, { ...original, phone: "0911111111", citizenId: "Injected", userId: "Injected" }, "Change phone"))
      .toEqual({ phone: "0911111111", reason: "Change phone", rowVersion: original.rowVersion });
    expect(employeeChanges({ ...original, departmentId: "department" }, { departmentId: null }, "Clear department"))
      .toEqual({ departmentId: null, reason: "Clear department", rowVersion: original.rowVersion });
    expect(employeeChanges(original, {}, "No extra fields")).toEqual({ reason: "No extra fields", rowVersion: original.rowVersion });
  });
  it("forwards bounded server pagination and filters rather than loading all employees", async () => {
    const fetch = vi.fn(async () => new Response(JSON.stringify({ items: [], totalCount: 45, page: 2, pageSize: 20 }), { headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetch);
    await personnelApi.list({ page: 2, pageSize: 20, name: "Tên & dấu", status: "DangLamViec" });
    const request = fetch.mock.calls[0] as unknown as [Request];
    const url = new URL(request[0].url);
    expect(url.searchParams.get("page")).toBe("2");
    expect(url.searchParams.get("pageSize")).toBe("20");
    expect(url.searchParams.get("name")).toBe("Tên & dấu");
    expect(url.searchParams.get("status")).toBe("DangLamViec");
    expect(fetch).toHaveBeenCalledTimes(1);
  });
  it("does not resend a conflicting mutation and preserves traceId for review", async () => {
    setCachedCsrfToken("fixture-csrf");
    const fetch = vi.fn(async () => new Response(JSON.stringify({ title: "Conflict", status: 409, errorCode: "resource.conflict", traceId: "fixture-trace" }), { status: 409, headers: { "Content-Type": "application/problem+json" } }));
    vi.stubGlobal("fetch", fetch);
    await expect(personnelApi.update(original.id, employeeChanges(original, { phone: "0911111111" }, "Fixture")))
      .rejects.toMatchObject({ status: 409, isConflict: true, traceId: "fixture-trace" });
    expect(fetch).toHaveBeenCalledTimes(1);
    const request = fetch.mock.calls[0] as unknown as [Request];
    expect(request[0].headers.get("X-XSRF-TOKEN")).toBe("fixture-csrf");
    expect(await request[0].json()).toEqual({ phone: "0911111111", reason: "Fixture", rowVersion: original.rowVersion });
  });
});
