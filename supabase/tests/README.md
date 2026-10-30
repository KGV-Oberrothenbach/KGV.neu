# Historical registration SQL regressions

Run with Node 22.13+ (Node 24 recommended):

```powershell
npm ci --prefix supabase/tests
npm test --prefix supabase/tests
```

The pinned PGlite dependency starts an ephemeral in-memory PostgreSQL. No connection URL, Supabase account, production database, Docker daemon or existing database is used.

The fixture supplies minimal synthetic prerequisites and auth functions. Tests apply the real lease-schema, G6a, G6b2 and G7 migrations, reproduce G11's original SQL ambiguity, then apply the G11b1 forward migration twice. Calls run as authenticated, not as service_role. Scenarios cover A–I, period splitting, successor preservation, multiple parcels, scope, explicit status/end commands, rollback and helper grants.

This is real SQL/PLpgSQL execution with synthetic prerequisites. It is **not** a full fresh Supabase migration, Supabase Auth/RLS end-to-end test, Edge/Storage test or production-schema migration validation. Those remain separate acceptance work.

For an isolated external tool installation the runner also accepts KGV_PGLITE_MODULE as an absolute file URL pointing to @electric-sql/pglite's module entry.

## G11b2 permission, Edge and PDF coverage

The same command now includes historical document permissions, the actual Edge handler with synthetic Supabase/Google boundaries, and real pdf-lib 1.17.1 rendering. Production network calls are disabled in the Edge harness. SQL tests load the real central role/grant/revocation functions and execute authenticated context/RLS checks on PGlite. Fixtures are minimal and synthetic.

PDF cases cover one/two/long membership numbers, multiple pages, crop boxes, page rotation, both text lines, original streams, valid output and failure-before-write behavior. A failed status confirmation returns an error after writing. G11b3 below protects the cross-phase retry.

The historical upload marker is backend-owned. General member access, upload, delete, and soft-archive privileges are not granted by Bit 24. The historical archive is the persisted contract document plus its relational registration; scoped document links support reopening after reload. Direct stamp-status confirmation is service-role only.

Apply `20261030150000_g11b2_historical_document_access.sql` before deploying the updated Edge Function and clients. Both `index.ts` and `historical-contract-pdf.ts` belong to that function. No deployment is performed by these tests.

The registration trigger also blocks turning an unrelated ordinary document into a historical one to gain read access. New sources require a backend-marked upload owned by the caller; existing registered historical contracts remain supported.

## G11b3 operation idempotency

Apply 20261030160000_g11b3_historical_contract_idempotency.sql before the Edge Function and both clients. No migration/deployment is executed against an existing database by these tests. The club boundary is the existing single-club database, augmented by actor ownership and demo scope.

The normal test command covers persistent UUID operations, upload hashes, JSON request equality, SQL replay results, sequence/relational/audit/member-write counts, upload and stamp races, permission revocation, unknown network outcomes and authenticated PDF markers. The Edge harness replaces external boundaries, but the Drive-create/409 test also exercises the actual multipart upload implementation against a synthetic HTTP transport. Web/MAUI source contracts are in KGV.Web/services/documents/historical-contract-idempotency.contract.test.mjs; .NET HTTP tests execute serialization and replay transport. These are not runtime UI/E2E tests.

PGlite queues calls on one embedded backend. For actual PostgreSQL row-lock contention, use the additional native Windows runner. It always creates a new temporary cluster with synthetic data, binds only to 127.0.0.1 on a random port, accepts no database URL, and stops/removes its cluster in finally.

```powershell
$toolsPath = Join-Path $env:TEMP "kgv-postgres-tools"
npm install --no-save --ignore-scripts --prefix $toolsPath @embedded-postgres/windows-x64@17.10.0-beta.17 pg@8.16.3
$env:KGV_TEST_PG_TOOLS = $toolsPath
node --test supabase/tests/historical-contract-concurrency.mjs
```

The binaries are an isolated test tool, not an application dependency. PostgreSQL 17.10 was used for G11b3 with two independent TCP connections and explicit verification that the second connection waits on a database lock. The regular test suite remains portable without native binaries. A Docker daemon was unavailable; a complete local Supabase/Auth/Storage stack and Android runtime acceptance remain open.

Upload attempts retain ownership of an ambiguously created Drive object through the persisted operation; they do not delete a file another retry may already be using. Abandoned unfinished operations retain that recovery record. PDF stamping reuses the same Drive file; the 5-minute backend-only lease recovers after an Edge crash. The PDF carries a document/number-bound persistent random marker so a lost write/status response cannot draw the stamp again. Completed database results are read without repeating member mutations.

The generated Drive ID retry contract follows Google's documentation: https://developers.google.com/workspace/drive/api/guides/manage-uploads#use_a_pre-generated_id_to_upload_files . Existing generated filename/member-folder conventions remain in use.
