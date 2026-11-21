import createClient from "openapi-fetch";
import type { paths } from "./generated/api.js";

// Shared by web/mobile; schema-derived paths/responses prevent hand-maintained endpoint types.
export function createHrmClient(baseUrl: string, fetcher: typeof globalThis.fetch = globalThis.fetch) {
  return createClient<paths>({ baseUrl, fetch: fetcher, credentials: "include" });
}

export type { paths, components, operations } from "./generated/api.js";
