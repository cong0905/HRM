import { test } from "node:test";
import assert from "node:assert/strict";
import { createHrmClient } from "../dist/client.js";

test("typed client encodes bounded pagination query without hand-built URLs", async () => {
  let captured;
  const client = createHrmClient("https://hrm.invalid", async (request) => {
    captured = request;
    return Response.json({ items: [], page: 2, pageSize: 20, totalCount: 0, totalPages: 0 });
  });
  const result = await client.GET("/api/v1/departments", { params: { query: { page: 2, pageSize: 20, sort: "-name" } } });
  assert.equal(new URL(captured.url).pathname, "/api/v1/departments");
  assert.equal(new URL(captured.url).searchParams.get("page"), "2");
  assert.equal(new URL(captured.url).searchParams.get("sort"), "-name");
  assert.equal(captured.credentials, "include");
  assert.equal(result.data.page, 2);
});

test("ProblemDetails remains an error result with traceId", async () => {
  const client = createHrmClient("https://hrm.invalid", async () => Response.json({
    status: 401, title: "Unauthorized", errorCode: "authentication.required", traceId: "test-trace"
  }, { status: 401, headers: { "Content-Type": "application/problem+json" } }));
  const result = await client.GET("/api/v1/departments");
  assert.equal(result.response.status, 401);
  assert.equal(result.data, undefined);
  assert.equal(result.error.traceId, "test-trace");
});
