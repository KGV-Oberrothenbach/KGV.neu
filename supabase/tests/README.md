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

PDF cases cover one/two/long membership numbers, multiple pages, crop boxes, page rotation, both text lines, original streams, valid output and failure-before-write behavior. A failed status confirmation returns an error after writing; cross-phase retry remains separate G11b3 work.

The historical upload marker is backend-owned. General member access, upload, delete, and soft-archive privileges are not granted by Bit 24. The historical archive is the persisted contract document plus its relational registration; scoped document links support reopening after reload. Direct stamp-status confirmation is service-role only.

Apply `20261030150000_g11b2_historical_document_access.sql` before deploying the updated Edge Function and clients. Both `index.ts` and `historical-contract-pdf.ts` belong to that function. No deployment is performed by these tests.

The registration trigger also blocks turning an unrelated ordinary document into a historical one to gain read access. New sources require a backend-marked upload owned by the caller; existing registered historical contracts remain supported.
