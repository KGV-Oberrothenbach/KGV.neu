// P11-T07-P runner.  It intentionally talks to the running local PostgreSQL
// service; it does not inspect migration source or simulate signatures/status.
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const db = process.env.P11_LOCAL_DB_URL;
if (!db) throw new Error("P11_LOCAL_DB_URL is required (local Supabase PostgreSQL)");
const fixture = fileURLToPath(new URL("./p11-ap04d-signed-change.e2e.sql", import.meta.url));
// GitHub's Linux runner provides psql directly.  On Windows Docker Desktop the
// same client lives in the local Supabase PostgreSQL container.
const result = process.platform === "win32"
  ? spawnSync("docker", ["exec", "-i", "supabase_db_03_Arbeitsstand", "psql", db.replace("127.0.0.1:54322", "127.0.0.1:5432"), "-v", "ON_ERROR_STOP=1"], { encoding: "utf8", input: await (await import("node:fs/promises")).readFile(fixture) })
  : spawnSync("psql", [db, "-v", "ON_ERROR_STOP=1", "-f", fixture], { encoding: "utf8" });
if (result.status !== 0) throw new Error(`P11-T07-P failed:\n${result.stdout}\n${result.stderr}`);
console.log("P11-T07-P: prepared signed-change RPC path passed against local Supabase.");
