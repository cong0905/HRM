import { test, expect, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixtureFile = process.env.HRM_T17_FIXTURE_FILE;
const fixture = (fixtureFile ? JSON.parse(fs.readFileSync(fixtureFile, "utf8")) : { actors: {}, password: "" }) as { actors: Record<string, string>; password: string };
test.beforeEach(() => { test.skip(!fixtureFile, "T17 NOT_RUN: no owned SQL browser fixture supplied."); });
const evidence = path.resolve("../docs/verification/t17");
async function login(page: Page, role: string) {
  await page.goto("/login");
  await page.getByLabel("Tên đăng nhập", { exact: true }).fill(fixture.actors[role]!);
  await page.getByLabel("Mật khẩu", { exact: true }).fill(fixture.password);
  await page.getByRole("button", { name: /Đăng nhập$/ }).click();
  await expect(page).toHaveURL("/");
}
async function fillEmployee(page: Page, name: string, suffix: string) {
  await page.getByLabel("Họ và tên", { exact: true }).fill(name);
  await page.getByLabel("Số điện thoại", { exact: true }).fill("0901234567");
  await page.getByLabel("Địa chỉ", { exact: true }).fill("Địa chỉ mẫu T17");
  await page.getByLabel("Ngày sinh", { exact: true }).fill("2000-01-02");
  await page.getByLabel("Giới tính", { exact: true }).fill("Nam");
  await page.getByLabel("Ngày vào làm", { exact: true }).fill("2026-01-01");
  await page.getByLabel("CCCD", { exact: true }).fill("t17-" + suffix);
  await page.getByLabel("Email", { exact: true }).fill(suffix + "@example.invalid");
  await page.getByLabel("Lý do thay đổi", { exact: true }).fill("Kiểm thử T17");
}

test("HR creates employee, patches only changes, compares a genuine SQL rowversion conflict", async ({ page, browser }) => {
  await login(page, "HR");
  const listRequests: URL[] = [];
  page.on("request", r => { const url = new URL(r.url()); if (url.pathname === "/api/v1/employees" && r.method() === "GET") listRequests.push(url); });
  await page.goto("/personnel");
  await page.getByRole("button", { name: "Thêm nhân viên" }).click();
  const suffix = "web" + Date.now(); const name = "Hồ sơ thử " + suffix;
  await fillEmployee(page, name, suffix);
  const createdResponse = page.waitForResponse(r => r.request().method() === "POST" && new URL(r.url()).pathname === "/api/v1/employees");
  await page.getByRole("button", { name: "Tạo nhân viên", exact: true }).click();
  const created = await (await createdResponse).json() as { profile: { id: string } };
  await expect(page.getByText(/Đã tạo NV/)).toBeVisible();
  const administrator = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  try {
    const admin = await administrator.newPage(); await login(admin, "SystemAdmin"); await admin.goto("/admin");
    await admin.getByRole("row").filter({ hasText: fixture.actors.HR }).getByRole("button", { name: "Cấp phạm vi", exact: true }).click();
    await admin.getByLabel("Mã hồ sơ cần cấp", { exact: true }).fill(created.profile.id);
    await admin.getByLabel("Lý do thay đổi", { exact: true }).fill("Explicit fixture HR assignment");
    await admin.getByRole("button", { name: "Xác nhận thay đổi", exact: true }).click();
    await expect(admin.getByText("Đã cấp phạm vi hồ sơ cho HR.")).toBeVisible();
  } finally { await administrator.close(); }
  await page.reload();
  await page.getByRole("button", { name, exact: true }).click();
  await expect(page.getByLabel("Số điện thoại", { exact: true })).toHaveValue("0901234567");
  const other = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  const otherPage = await other.newPage();
  try {
    await login(otherPage, "HR"); await otherPage.goto("/personnel");
    await otherPage.getByRole("button", { name, exact: true }).click();
    await expect(otherPage.getByLabel("Địa chỉ", { exact: true })).toHaveValue("Địa chỉ mẫu T17");
    await page.getByLabel("Số điện thoại", { exact: true }).fill("0911111111");
    await page.getByLabel("Lý do thay đổi", { exact: true }).fill("Đổi điện thoại");
    const patchPromise = page.waitForRequest(r => r.method() === "PATCH" && r.url().includes("/employees/"));
    await page.getByRole("button", { name: "Lưu thay đổi", exact: true }).click();
    const patch = await patchPromise;
    expect(Object.keys(patch.postDataJSON()).sort()).toEqual(["phone", "reason", "rowVersion"]);
    await expect(page.getByText("Đã lưu hồ sơ nhân viên.")).toBeVisible();
    await otherPage.getByLabel("Địa chỉ", { exact: true }).fill("Bản nhập chưa lưu");
    await otherPage.getByLabel("Lý do thay đổi", { exact: true }).fill("Sửa từ phiên bản cũ");
    const conflict = otherPage.waitForResponse(r => r.request().method() === "PATCH");
    await otherPage.getByRole("button", { name: "Lưu thay đổi", exact: true }).click();
    expect((await conflict).status()).toBe(409);
    await expect(otherPage.getByLabel("Địa chỉ", { exact: true })).toHaveValue("Bản nhập chưa lưu");
    await otherPage.getByRole("button", { name: "Tải bản mới để so sánh" }).click();
    await expect(otherPage.getByRole("region", { name: "So sánh hồ sơ" }).getByText("0911111111", { exact: true })).toBeVisible();
    await otherPage.screenshot({ path: path.join(evidence, "personnel-conflict.png"), fullPage: true });
    await otherPage.getByRole("button", { name: "Dùng bản mới và nhập lại thay đổi" }).click();
    await expect(otherPage.getByLabel("Số điện thoại", { exact: true })).toHaveValue("0911111111");
    await expect(otherPage.getByLabel("Địa chỉ", { exact: true })).toHaveValue("Địa chỉ mẫu T17");
    await otherPage.getByLabel("Địa chỉ", { exact: true }).fill("Địa chỉ mới sau so sánh");
    await otherPage.getByLabel("Lý do thay đổi", { exact: true }).fill("Đã đối chiếu");
    await otherPage.getByRole("button", { name: "Lưu thay đổi", exact: true }).click();
    await expect(otherPage.getByText("Đã lưu hồ sơ nhân viên.")).toBeVisible();
  } finally { await other.close(); }
  expect(listRequests.length).toBeGreaterThan(0);
  expect(listRequests.every(u => u.searchParams.get("pageSize") === "20")).toBe(true);
  await page.screenshot({ path: path.join(evidence, "personnel-desktop.png"), fullPage: true });
});

test("Employee sees own profile, cannot open HR or admin, fits 375px", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await login(page, "Employee"); await page.goto("/my-profile");
  await expect(page.getByText("Nhân viên kiểm thử T17", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Lưu thay đổi" })).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: path.join(evidence, "own-profile-mobile.png"), fullPage: true });
  await page.goto("/personnel"); await expect(page.getByText("403 - Không có quyền truy cập")).toBeVisible();
  await page.goto("/admin"); await expect(page.getByText("403 - Không có quyền truy cập")).toBeVisible();
});

test("HR previews calendar using server math and saves an immutable version", async ({ page }) => {
  await login(page, "HR"); await page.goto("/calendar");
  await page.getByLabel("Tên phiên bản", { exact: true }).fill("Lịch thử T17");
  await page.getByLabel("Áp dụng từ ngày", { exact: true }).fill("2026-11-01");
  await page.getByLabel("Giờ bắt đầu", { exact: true }).fill("08:00");
  await page.getByLabel("Giờ kết thúc", { exact: true }).fill("17:00");
  for (const day of ["Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu"]) await page.getByLabel(day, { exact: true }).check();
  await page.getByRole("button", { name: "Thêm giờ nghỉ" }).click();
  await page.getByLabel("Nghỉ từ", { exact: true }).fill("12:00"); await page.getByLabel("Nghỉ đến", { exact: true }).fill("13:00");
  await page.getByLabel("Ngày nghỉ lễ", { exact: true }).fill("2026-11-20");
  await page.getByLabel("Lý do tạo phiên bản", { exact: true }).fill("Fixture calendar");
  await page.getByRole("button", { name: "Xem trước trên máy chủ" }).click();
  await expect(page.getByRole("region", { name: "Xem trước lịch đề xuất" }).getByText("9600", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Lưu phiên bản lịch" }).click();
  await expect(page.getByText(/Đã tạo phiên bản Lịch thử T17/)).toBeVisible();
  await page.getByLabel("Tháng áp dụng", { exact: true }).fill("2026-11");
  await expect(page.getByText("9600", { exact: true })).toBeVisible();
  await page.screenshot({ path: path.join(evidence, "calendar-desktop.png"), fullPage: true });
});

test("HR manages organization records and SystemAdmin sees account roles and audit", async ({ page, browser }) => {
  await login(page, "HR"); await page.goto("/organization");
  const name = "Phòng mẫu " + Date.now();
  await page.getByRole("button", { name: "Thêm phòng ban" }).click();
  await page.getByLabel("Tên", { exact: true }).fill(name); await page.getByLabel("Lý do thay đổi", { exact: true }).fill("Fixture");
  await page.getByRole("button", { name: "Lưu thay đổi", exact: true }).click(); await expect(page.getByText(name, { exact: true })).toBeVisible();
  const row = page.getByRole("row").filter({ hasText: name }); await row.getByRole("button", { name: "Sửa", exact: true }).click();
  await page.getByLabel("Tên", { exact: true }).fill(name + " mới"); await page.getByLabel("Lý do thay đổi", { exact: true }).fill("Rename fixture");
  await page.getByRole("button", { name: "Lưu thay đổi", exact: true }).click(); await expect(page.getByText(name + " mới", { exact: true })).toBeVisible();
  await page.getByRole("row").filter({ hasText: name + " mới" }).getByRole("button", { name: "Xóa", exact: true }).click();
  await page.getByLabel("Lý do thay đổi", { exact: true }).fill("Delete fixture"); await page.getByRole("button", { name: "Xác nhận xóa", exact: true }).click();
  await expect(page.getByText(name + " mới", { exact: true })).toHaveCount(0);
  const context = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  try { const admin = await context.newPage(); await login(admin, "SystemAdmin"); await admin.goto("/admin");
    await expect(admin.getByRole("heading", { name: "Tài khoản và vai trò" })).toBeVisible();
    const self = admin.getByRole("row").filter({ hasText: fixture.actors.SystemAdmin }); await expect(self.getByRole("button", { name: "Đổi vai trò" })).toBeDisabled();
    await admin.goto("/audit"); await expect(admin.getByRole("cell", { name: "departments.created", exact: true })).toBeVisible();
    await admin.screenshot({ path: path.join(evidence, "audit-desktop.png"), fullPage: true });
  } finally { await context.close(); }
});

test("HR personnel fits mobile and landscape with keyboard-accessible drawer", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 }); await page.emulateMedia({ reducedMotion: "reduce" });
  await login(page, "HR"); await page.goto("/personnel");
  await expect(page.getByRole("region", { name: "Danh sách nhân viên" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole("button", { name: "Thêm nhân viên" }).click();
  await expect(page.getByLabel("Họ và tên", { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: path.join(evidence, "personnel-drawer-mobile.png"), fullPage: false });
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.setViewportSize({ width: 667, height: 375 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test("Offboarding from HR web revokes existing web and mobile sessions in real backend", async ({ page, browser }) => {
  const context = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  const mobileContext = await browser.newContext({ ignoreHTTPSErrors: true, baseURL: process.env.E2E_BASE_URL || "https://localhost:3000" });
  try {
    const employee = await context.newPage(); await login(employee, "Employee");
    const mobile = await mobileContext.request.post("/api/v1/auth/mobile/login", { data: { username: fixture.actors.Employee, password: fixture.password } });
    expect(mobile.status()).toBe(200); const tokens = await mobile.json() as { accessToken: string; refreshToken: string };
    const token = tokens.accessToken;
    expect((await mobileContext.request.get("/api/v1/me/profile", { headers: { Authorization: `Bearer ${token}` } })).status()).toBe(200);
    await login(page, "HR"); await page.goto("/personnel");
    await page.getByRole("row").filter({ hasText: "Nhân viên kiểm thử T17" }).getByRole("button", { name: "Nghỉ việc", exact: true }).click();
    await page.getByLabel("Ngày nghỉ việc", { exact: true }).fill("2026-11-14"); await page.getByLabel("Lý do nghỉ việc", { exact: true }).fill("T17 isolated fixture offboard");
    await page.getByRole("button", { name: "Xác nhận nghỉ việc", exact: true }).click(); await expect(page.getByText(/Đã ghi nhận nghỉ việc/)).toBeVisible();
    const revokedCookie = await context.request.get("/api/v1/me/profile"); expect(revokedCookie.status()).toBe(401);
    const revokedBearer = await mobileContext.request.get("/api/v1/me/profile", { headers: { Authorization: `Bearer ${token}` } }); expect(revokedBearer.status()).toBe(401);
    expect((await mobileContext.request.post("/api/v1/auth/mobile/refresh", { data: { refreshToken: tokens.refreshToken } })).status()).toBe(401);
    await employee.goto("/my-profile"); await expect(employee).toHaveURL(/\/login/);
  } finally { await context.close(); await mobileContext.close(); }
});
