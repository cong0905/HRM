import { mkdir, readFile, writeFile } from "node:fs/promises";
import openapiTS, { astToString } from "openapi-typescript";

const schema = new URL("../openapi.v1.json", import.meta.url);
const output = new URL("../src/generated/api.ts", import.meta.url);
const source = "// Generated from contracts/openapi.v1.json. Do not edit by hand.\n" + astToString(await openapiTS(schema));
if (process.argv.includes("--check")) {
  const current = await readFile(output, "utf8");
  if (current !== source) {
    console.error("Generated TypeScript client types have drifted. Run npm run generate and review the diff.");
    process.exitCode = 1;
  }
} else {
  await mkdir(new URL("../src/generated/", import.meta.url), { recursive: true });
  await writeFile(output, source, "utf8");
}
