import { defineConfig, devices } from "@playwright/test";

/**
 * Playwright E2E configuration for HRM Web SPA.
 * Acceptance tests require a live backend with authentic SQL test accounts.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.E2E_BASE_URL || "https://localhost:3000",
    ignoreHTTPSErrors: true, // For local dev self-signed HTTPS certificates
    trace: "on-first-retry",
    video: "retain-on-failure",
  },
  projects: [
    {
      name: "Desktop Chrome",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "Mobile Chrome 375px",
      use: {
        ...devices["Pixel 5"],
        viewport: { width: 375, height: 667 },
      },
    },
  ],
});
