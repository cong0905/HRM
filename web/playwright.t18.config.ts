import { defineConfig } from "@playwright/test";
if (!process.env.HRM_T18_FIXTURE_FILE) throw new Error("T18 NOT_RUN: provide an owned SQL browser fixture.");
export default defineConfig({ testDir: "./e2e", testMatch: "workflows.spec.ts", workers: 1, fullyParallel: false, retries: 0,
  timeout: 90000, expect: { timeout: 10000 }, reporter: [["list"], ["json", { outputFile: "../docs/verification/t18/playwright.json" }]],
  use: { channel: "msedge", baseURL: process.env.E2E_BASE_URL || "https://localhost:3000", ignoreHTTPSErrors: true, trace: "retain-on-failure", screenshot: "only-on-failure" },
  outputDir: "../docs/verification/t18/playwright-artifacts" });
