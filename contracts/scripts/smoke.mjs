import assert from "node:assert/strict";
import { createHrmClient } from "../dist/client.js";

const baseUrl = process.argv[2];
if (!baseUrl?.startsWith("http://127.0.0.1:")) throw new Error("Smoke test requires a local ephemeral API URL.");
const client = createHrmClient(baseUrl);
const info = await client.GET("/api/v1/system/info");
assert.equal(info.response.status, 200);
assert.equal(info.data.apiVersion, "v1");
assert.match(info.data.serverTimeUtc, /Z$/);
const live = await client.GET("/health/live");
assert.equal(live.response.status, 200);
const protectedRead = await client.GET("/api/v1/departments");
assert.equal(protectedRead.response.status, 401);
assert.equal(protectedRead.error.errorCode, "authentication.required");
assert.ok(protectedRead.error.traceId);
console.log("Generated contract client PASS against real Kestrel HTTP: system info, liveness, fail-closed ProblemDetails.");
