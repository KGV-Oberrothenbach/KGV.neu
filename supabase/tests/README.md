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
