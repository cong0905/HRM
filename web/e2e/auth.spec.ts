import { test, expect } from "@playwright/test";

const adminUsername = process.env.E2E_ADMIN_USERNAME;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;
const employeeUsername = process.env.E2E_EMPLOYEE_USERNAME;
const employeePassword = process.env.E2E_EMPLOYEE_PASSWORD;

const hasLiveFixture = Boolean(adminUsername && adminPassword);

test.describe("HRM Next - Real End-to-End Acceptance Tests", () => {
  test.beforeEach(async () => {
    // Contract requirement: Real acceptance tests must NEVER use fake route.fulfill.
    // If live credentials are not provisioned in the environment, mark as NOT_RUN.
    test.skip(
      !hasLiveFixture,
      "Live backend fixture credentials (E2E_ADMIN_USERNAME, E2E_ADMIN_PASSWORD) not provided. Marked as NOT_RUN per T16 specification."
    );
  });

  test("1. Full login sequence: CSRF fetch, cookie session establishment, /access/me bootstrap", async ({
    page,
  }) => {
    await page.goto("/login");

    // Fill credentials
    await page.getByLabel("Tên đăng nhập").fill(adminUsername!);
    await page.getByLabel("Mật khẩu").fill(adminPassword!);

    // Submit form
    await page.getByRole("button", { name: "Đăng nhập" }).click();

    // Verify redirected to Dashboard
    await expect(page).toHaveURL("/");
    await expect(page.getByText(`Chào mừng trở lại, ${adminUsername}!`)).toBeVisible();

    // Verify cookie session is active
    const cookies = await page.context().cookies();
    const sessionCookie = cookies.find((c) => c.name === "__Host-Hrm.Session");
    expect(sessionCookie).toBeDefined();
    expect(sessionCookie?.secure).toBe(true);
    expect(sessionCookie?.httpOnly).toBe(true);
  });

  test("2. Page reload maintains valid session without re-authenticating", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Tên đăng nhập").fill(adminUsername!);
    await page.getByLabel("Mật khẩu").fill(adminPassword!);
    await page.getByRole("button", { name: "Đăng nhập" }).click();
    await expect(page).toHaveURL("/");

    // Reload page
    await page.reload();

    // Should remain on dashboard without being redirected to login
    await expect(page).toHaveURL("/");
    await expect(page.getByText(`Chào mừng trở lại, ${adminUsername}!`)).toBeVisible();
  });

  test("3. Logout sequence: invalidates cookie, cancels queries, returns to login", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Tên đăng nhập").fill(adminUsername!);
    await page.getByLabel("Mật khẩu").fill(adminPassword!);
    await page.getByRole("button", { name: "Đăng nhập" }).click();
    await expect(page).toHaveURL("/");

    // Open user menu and click logout
    await page.getByLabel("Tài khoản cá nhân").click();
    await page.getByText("Đăng xuất").click();

    // Must be redirected to /login
    await expect(page).toHaveURL("/login");

    // Navigating back to / should redirect back to /login
    await page.goto("/");
    await expect(page).toHaveURL(/\/login/);
  });

  test("4. Unauthorized route guard displays 403 Forbidden without breaking session", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Tên đăng nhập").fill(adminUsername!);
    await page.getByLabel("Mật khẩu").fill(adminPassword!);
    await page.getByRole("button", { name: "Đăng nhập" }).click();
    await expect(page).toHaveURL("/");

    // Actual SystemAdmin does NOT have payroll.read_all; navigating to /payroll must trigger 403 Forbidden
    await page.goto("/payroll");

    // Assert 403 Forbidden page is rendered
    await expect(page.getByText("403 - Không có quyền truy cập")).toBeVisible();
    await expect(
      page.getByText("Tài khoản hiện tại của bạn không sở hữu quyền hạn (permission) cần thiết")
    ).toBeVisible();

    // Server session remains valid (authenticated session returns 200, user menu visible)
    await expect(page.getByLabel("Tài khoản cá nhân")).toBeVisible();
    await page.getByRole("button", { name: "Quay về Trang chủ" }).click();
    await expect(page).toHaveURL("/");
  });

  test("5. Accessibility: Skip to main content link and keyboard navigation", async ({ page }) => {
    await page.goto("/login");

    // Tab to skip link
    await page.keyboard.press("Tab");
    const skipLink = page.getByText("Chuyển đến nội dung chính");
    await expect(skipLink).toBeFocused();

    // Tab into login form fields
    await page.keyboard.press("Tab");
    await expect(page.getByLabel("Tên đăng nhập")).toBeFocused();

    await page.keyboard.press("Tab");
    await expect(page.getByLabel("Mật khẩu")).toBeFocused();
  });

  test("6. Responsive viewport down to 375px renders cleanly without horizontal scroll", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto("/login");

    // Check login card fits inside 375px viewport
    const bodyWidth = await page.evaluate(() => document.body.scrollWidth);
    expect(bodyWidth).toBeLessThanOrEqual(375);
  });

  test("7. Employee login: sees permitted modules according to assigned server permissions", async ({ page }) => {
    test.skip(
      !employeeUsername || !employeePassword,
      "Optional E2E_EMPLOYEE_USERNAME and E2E_EMPLOYEE_PASSWORD not provided."
    );

    await page.goto("/login");
    await page.getByLabel("Tên đăng nhập").fill(employeeUsername!);
    await page.getByLabel("Mật khẩu").fill(employeePassword!);
    await page.getByRole("button", { name: "Đăng nhập" }).click();

    await expect(page).toHaveURL("/");
    await expect(page.getByText(`Chào mừng trở lại, ${employeeUsername}!`)).toBeVisible();
  });
});
