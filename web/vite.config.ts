/// <reference types="vitest" />
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";
import fs from "node:fs";

const apiOrigin = process.env.HRM_API_ORIGIN || "https://127.0.0.1:5001";
const certPath = process.env.HRM_WEB_CERT;
const keyPath = process.env.HRM_WEB_KEY;
const hasHttpsCerts = Boolean(certPath && keyPath && fs.existsSync(certPath) && fs.existsSync(keyPath));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
      "hrm-api-contracts": path.resolve(__dirname, "../contracts/src/client.ts"),
    },
  },
  server: {
    host: true,
    port: 3000,
    strictPort: true,
    https: hasHttpsCerts
      ? {
          cert: fs.readFileSync(certPath!),
          key: fs.readFileSync(keyPath!),
        }
      : undefined,
    proxy: {
      "/api": {
        target: apiOrigin,
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./test/setup.ts"],
    include: ["test/**/*.{test,spec}.{ts,tsx}"],
    fileParallelism: false,
  },
});
