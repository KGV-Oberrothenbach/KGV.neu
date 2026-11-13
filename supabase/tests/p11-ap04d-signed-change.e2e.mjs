// P11-T07-P runner.  It intentionally talks to the running local PostgreSQL
// service; it does not inspect migration source or simulate signatures/status.
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const db = process.env.P11_LOCAL_DB_URL;
if (!db) throw new Error("P11_LOCAL_DB_URL is required (local Supabase PostgreSQL)");
const fixture = fileURLToPath(new URL("./p11-ap04d-signed-change.e2e.sql", import.meta.url));
const result = spawnSync("psql", [db, "-v", "ON_ERROR_STOP=1", "-f", fixture], { encoding: "utf8" });
if (result.status !== 0) throw new Error(`P11-T07-P failed:\n${result.stdout}\n${result.stderr}`);
console.log("P11-T07-P: prepared signed-change RPC path passed against local Supabase.");
