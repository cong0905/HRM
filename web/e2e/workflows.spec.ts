import { test, expect, type Page, type Browser, type BrowserContext } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixtureFile = process.env.HRM_T18_FIXTURE_FILE;
const fixture = (fixtureFile ? JSON.parse(fs.readFileSync(fixtureFile, "utf8")) : { actors: {}, password: "", employeeIds: {}, advanceFile: "" }) as { actors: Record<string, string>; password: string; employeeIds: Record<string, string>; advanceFile: string };
const evidence = path.resolve("../docs/verification/t18");
// Reuse real authenticated fixture cookies in memory; repeated UI logins would exhaust
// the unchanged production 20/minute auth limit across these same-IP workflow cases.
const cookieCache = new Map<string, Awaited<ReturnType<BrowserContext["cookies"]>>>();
test.describe.configure({ mode: "serial" });
test.beforeEach(() => { test.skip(!fixtureFile, "T18 NOT_RUN: no SQL fixture."); });
async function login(page: Page, actor: string) {
  const cached = cookieCache.get(actor);
  if (cached) {
    await page.context().addCookies(cached); await page.goto("/"); await expect(page).toHaveURL("/"); return;
  }
  await page.goto("/login"); await page.getByLabel("Tên đăng nhập", { exact: true }).fill(fixture.actors[actor]!);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(fixture.password); await page.getByRole("button", { name: /Đăng nhập$/ }).click(); await expect(page).toHaveURL("/");
  cookieCache.set(actor, await page.context().cookies());
}
async function actor(browser: Browser, role: string): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  const page = await context.newPage(); await login(page, role); return { context, page };
}
async function fillLeave(page: Page, start: string, end: string, reason: string) {
  await page.getByLabel("Nghỉ từ ngày", { exact: true }).fill(start); await page.getByLabel("Nghỉ đến ngày", { exact: true }).fill(end);
  await page.getByLabel("Lý do nghỉ phép", { exact: true }).fill(reason);
}
async function create(page: Page, start: string, end: string, reason: string) {
  await fillLeave(page, start, end, reason);
  const response = page.waitForResponse(r => r.request().method() === "POST" && new URL(r.url()).pathname === "/api/v1/leave-requests");
  await page.getByRole("button", { name: "Gửi đơn nghỉ phép", exact: true }).click();
  const result = await response; expect(result.status()).toBe(201);
  return await result.json() as { id: string; rowVersion: string };
}
const rowFor = (page: Page, reason: string) => page.getByRole("row").filter({ hasText: reason });
async function openDecision(page: Page, reason: string, action = "Duyệt đơn") {
  await rowFor(page, reason).getByRole("button", { name: action, exact: true }).click();
  await page.getByLabel("Lý do quyết định", { exact: true }).fill("T18 reviewed by HR");
}
async function submitDecision(page: Page, action = "Duyệt đơn") {
  const response = page.waitForResponse(r => r.request().method() === "POST" && /\/leave-requests\/[^/]+\/(approve|reject|cancel)$/.test(new URL(r.url()).pathname));
  await page.getByRole("dialog").getByRole("button", { name: action, exact: true }).click(); return await response;
}
async function authenticatedPost(page: Page, url: string, data: unknown) {
  const csrf = await (await page.request.get("/api/v1/auth/csrf")).json() as { requestToken: string };
  return page.request.post(url, { headers: { "X-XSRF-TOKEN": csrf.requestToken, "Idempotency-Key": crypto.randomUUID() }, data });
}

test("Two HR browsers cannot debit the same leave twice; a later request fails when balance changed", async ({ page, browser }) => {
  test.setTimeout(180000);
  await login(page, "Employee"); await page.goto("/leave");
  const first = await create(page, "2026-11-17", "2026-11-18", "T18 first two days");
  const second = await create(page, "2026-11-19", "2026-11-20", "T18 second two days");
  const hr1 = await actor(browser, "HR"), hr2 = await actor(browser, "HR2");
  try {
    await hr1.page.goto("/leave"); await hr2.page.goto("/leave");
    await openDecision(hr1.page, "T18 first two days"); await openDecision(hr2.page, "T18 first two days");
    expect((await submitDecision(hr1.page)).status()).toBe(204);
    const conflict = await submitDecision(hr2.page); expect(conflict.status()).toBe(409);
    await expect(hr2.page.getByLabel("Lý do quyết định", { exact: true })).toHaveValue("T18 reviewed by HR");
    await hr2.page.getByRole("button", { name: "Tải trạng thái đơn mới nhất" }).click();
    await expect(hr2.page.getByRole("dialog").getByText(/Đã duyệt/)).toBeVisible();
    await expect(hr2.page.getByRole("dialog").getByRole("button", { name: "Duyệt đơn", exact: true })).toBeDisabled();
    await hr2.page.keyboard.press("Escape");
    await openDecision(hr1.page, "T18 second two days");
    const insufficient = await submitDecision(hr1.page); expect((await insufficient.json()).errorCode).toBe("leave.insufficient_balance");
    await expect(hr1.page.getByText(/Quỹ phép không đủ cho khoảng ngày đã chọn/)).toBeVisible();
    await expect(hr1.page.getByLabel("Lý do quyết định", { exact: true })).toHaveValue("T18 reviewed by HR");
    const balances = await (await page.request.get("/api/v1/me/leave-balances?year=2026")).json() as { usedDays: string; remainingDays: string }[];
    expect(balances[0]!.usedDays).toMatch(/^2(?:\.0+)?$/); expect(balances[0]!.remainingDays).toMatch(/^1(?:\.0+)?$/);
    const state = await (await page.request.get(`/api/v1/leave-requests/${second.id}`)).json(); expect(state.status).toBe("Pending");
    expect((await (await page.request.get(`/api/v1/leave-requests/${first.id}`)).json()).status).toBe("Approved");
    await hr1.page.screenshot({ path: path.join(evidence, "leave-balance-failure.png"), fullPage: true });
  } finally { await hr1.context.close(); await hr2.context.close(); }
});

test("Employee cancels Pending and HR rejects another request with server state reflected", async ({ page, browser }) => {
  await login(page, "Employee"); await page.goto("/leave");
  const cancelled = await create(page, "2026-11-23", "2026-11-23", "T18 cancellation candidate");
  await openDecision(page, "T18 cancellation candidate", "Hủy đơn"); expect((await submitDecision(page, "Hủy đơn")).status()).toBe(204);
  await expect(rowFor(page, "T18 cancellation candidate").getByText("Đã hủy", { exact: true })).toBeVisible();
  const rejected = await create(page, "2026-11-24", "2026-11-24", "T18 rejection candidate");
  const hr = await actor(browser, "HR2");
  try {
    await hr.page.goto("/leave"); await openDecision(hr.page, "T18 rejection candidate", "Từ chối đơn"); expect((await submitDecision(hr.page, "Từ chối đơn")).status()).toBe(204);
    await page.reload(); await expect(rowFor(page, "T18 rejection candidate").getByText("Từ chối", { exact: true })).toBeVisible();
    expect((await (await page.request.get(`/api/v1/leave-requests/${cancelled.id}`)).json()).status).toBe("Cancelled");
    expect((await (await page.request.get(`/api/v1/leave-requests/${rejected.id}`)).json()).status).toBe("Rejected");
  } finally { await hr.context.close(); }
});

test("HR cannot approve own request in the interface or real API", async ({ page }) => {
  await login(page, "HR"); await page.goto("/leave"); await page.getByRole("tab", { name: "Phép của tôi", exact: true }).click();
  const own = await create(page, "2026-11-25", "2026-11-25", "T18 self approval candidate");
  await page.getByRole("tab", { name: "Hàng đợi duyệt", exact: true }).click();
  const row = rowFor(page, "T18 self approval candidate"); await expect(row.getByRole("button", { name: "Duyệt đơn", exact: true })).toBeDisabled();
  await expect(row.getByRole("button", { name: "Từ chối đơn", exact: true })).toBeDisabled();
  const denied = await authenticatedPost(page, `/api/v1/leave-requests/${own.id}/approve`, { reason: "T18 forbidden self approval", rowVersion: own.rowVersion });
  expect(denied.status()).toBe(403); expect((await denied.json()).errorCode).toBe("leave.self_approval_forbidden");
});

test("After-flush SQL audit failure rolls back and UI retains the decision without success", async ({ page, browser }) => {
  await login(page, "Employee"); await page.goto("/leave"); const request = await create(page, "2026-11-26", "2026-11-26", "T18 rollback candidate");
  const hr = await actor(browser, "HR");
  try {
    await hr.page.goto("/leave"); await openDecision(hr.page, "T18 rollback candidate");
    await hr.page.getByLabel("Lý do quyết định", { exact: true }).fill("T18_TEST_ROLLBACK");
    expect((await submitDecision(hr.page)).status()).toBe(500);
    await expect(hr.page.getByLabel("Lý do quyết định", { exact: true })).toHaveValue("T18_TEST_ROLLBACK");
    await expect(hr.page.getByText("Máy chủ đã ghi nhận quyết định đối với đơn nghỉ phép.", { exact: true })).toHaveCount(0);
    expect((await (await page.request.get(`/api/v1/leave-requests/${request.id}`)).json()).status).toBe("Pending");
    const balance = await (await page.request.get("/api/v1/me/leave-balances?year=2026")).json(); expect(balance[0].usedDays).toMatch(/^2(?:\.0+)?$/);
    const observed = JSON.parse(fs.readFileSync(path.join(evidence, "rollback-flush.json"), "utf8"));
    expect(observed).toMatchObject({ phase: "after-real-sql-flush-before-rollback", Status: "Approved", UsedDays: 3 });
    await hr.page.screenshot({ path: path.join(evidence, "leave-rollback.png"), fullPage: true });
  } finally { await hr.context.close(); }
});

test("Attendance records server time, HR corrects missing checkout and confirms OT; locked data stays read only", async ({ page, browser }) => {
  await login(page, "Employee"); await page.goto("/attendance");
  const incoming = page.waitForResponse(r => r.url().includes("/attendance/me/check-in") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Vào ca", exact: true }).click(); expect((await incoming).status()).toBe(200);
  const now = await (await page.request.get("/api/v1/work-calendars/now")).json(); fs.writeFileSync(fixture.advanceFile, "5");
  await expect.poll(async () => Date.parse((await (await page.request.get("/api/v1/work-calendars/now")).json()).serverTimeUtc), { timeout: 10000 }).toBeGreaterThan(Date.parse(now.serverTimeUtc));
  const outgoing = page.waitForResponse(r => r.url().includes("/attendance/me/check-out") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Ra ca", exact: true }).click(); expect((await outgoing).status()).toBe(200);
  const hr = await actor(browser, "HR2");
  try {
    await hr.page.goto("/attendance"); await expect(hr.page.getByRole("heading", { name: "Chấm công", exact: true })).toBeVisible();
    const missing = hr.page.getByRole("row").filter({ hasText: "2026-11-13" }); await expect(missing.getByText("Thiếu giờ ra", { exact: true })).toBeVisible();
    await missing.getByRole("button", { name: "Điều chỉnh", exact: true }).click();
    await hr.page.getByLabel("Giờ ra thực tế (có thể để trống)", { exact: true }).fill("2026-11-13T17:30:00.000");
    await hr.page.getByLabel("Lý do điều chỉnh", { exact: true }).fill("T18 complete missing checkout");
    const adjusted = hr.page.waitForResponse(r => /\/adjust$/.test(new URL(r.url()).pathname) && r.request().method() === "POST");
    await hr.page.getByRole("button", { name: "Lưu quyết định công", exact: true }).click(); expect((await adjusted).status()).toBe(200);
    await expect(missing.getByText("Đã điều chỉnh", { exact: true })).toBeVisible();
    await missing.getByRole("button", { name: "Xác nhận OT", exact: true }).click();
    await hr.page.getByLabel("Số phút OT xác nhận", { exact: true }).fill("30"); await hr.page.getByLabel("Lý do điều chỉnh", { exact: true }).fill("T18 authorized thirty minutes");
    const ot = hr.page.waitForResponse(r => /\/overtime$/.test(new URL(r.url()).pathname) && r.request().method() === "POST");
    await hr.page.getByRole("button", { name: "Lưu quyết định công", exact: true }).click(); expect((await ot).status()).toBe(200);
    await expect(missing.getByRole("cell", { name: /^30(?:\.0+)?$/ })).toBeVisible();
    const closed = hr.page.getByRole("row").filter({ hasText: "2026-10-30" }); await expect(closed.getByText("Kỳ đã khóa · chỉ xem", { exact: true })).toBeVisible();
    await expect(closed.getByRole("button", { name: "Điều chỉnh", exact: true })).toHaveCount(0);
    await hr.page.screenshot({ path: path.join(evidence, "attendance-desktop.png"), fullPage: true });
    await hr.page.goto("/leave"); const closedLeave = rowFor(hr.page, "T18 locked fixture"); await expect(closedLeave.getByText("Kỳ đã khóa · chỉ xem", { exact: true })).toBeVisible();
    await expect(closedLeave.getByRole("button", { name: "Duyệt đơn", exact: true })).toHaveCount(0);
  } finally { await hr.context.close(); }
});

test("Balance and overlap failures retain forms; cross-year validation and mobile calendar remain usable", async ({ page, browser }) => {
  await page.setViewportSize({ width: 375, height: 667 }); await page.emulateMedia({ reducedMotion: "reduce" });
  await login(page, "Employee"); await page.goto("/leave"); await fillLeave(page, "2026-11-17", "2026-11-17", "T18 overlap must fail");
  const overlap = page.waitForResponse(r => r.request().method() === "POST" && new URL(r.url()).pathname === "/api/v1/leave-requests");
  await page.getByRole("button", { name: "Gửi đơn nghỉ phép", exact: true }).click(); expect((await (await overlap).json()).errorCode).toBe("leave.date_overlap");
  await expect(page.getByLabel("Lý do nghỉ phép", { exact: true })).toHaveValue("T18 overlap must fail");
  await page.getByRole("button", { name: "Xem lịch ngày nghỉ", exact: true }).click(); await expect(page.getByRole("region", { name: "Lịch ngày nghỉ" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: path.join(evidence, "leave-mobile.png"), fullPage: true });
  await fillLeave(page, "2026-12-31", "2027-01-01", "Cross year"); await expect(page.getByText("Ngày kết thúc phải từ ngày bắt đầu và cùng năm.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Gửi đơn nghỉ phép", exact: true })).toBeDisabled();
  const noBalance = await actor(browser, "EmployeeNoBalance");
  try {
    await noBalance.page.goto("/leave"); await fillLeave(noBalance.page, "2026-11-27", "2026-11-27", "T18 no quota");
    const failed = noBalance.page.waitForResponse(r => r.request().method() === "POST" && new URL(r.url()).pathname === "/api/v1/leave-requests");
    await noBalance.page.getByRole("button", { name: "Gửi đơn nghỉ phép", exact: true }).click(); expect((await (await failed).json()).errorCode).toBe("leave.insufficient_balance");
    await expect(noBalance.page.getByText(/Quỹ phép không đủ cho khoảng ngày đã chọn/)).toBeVisible();
    await expect(noBalance.page.getByLabel("Lý do nghỉ phép", { exact: true })).toHaveValue("T18 no quota");
  } finally { await noBalance.context.close(); }
});
