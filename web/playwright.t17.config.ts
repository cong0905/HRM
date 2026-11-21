import { defineConfig } from "@playwright/test";

if (!process.env.HRM_T17_FIXTURE_FILE) throw new Error("T17 NOT_RUN: start the owned SQL harness and set HRM_T17_FIXTURE_FILE.");

export default defineConfig({
  testDir: "./e2e", testMatch: "personnel.spec.ts", fullyParallel: false, workers: 1,
  timeout: 60000, expect: { timeout: 10000 }, retries: 0,
  reporter: [["list"], ["json", { outputFile: "../docs/verification/t17/playwright.json" }]],
  use: { channel: "msedge", baseURL: process.env.E2E_BASE_URL || "https://localhost:3000", ignoreHTTPSErrors: true, trace: "retain-on-failure", screenshot: "only-on-failure" },
  outputDir: "../docs/verification/t17/playwright-artifacts",
});
