-- AP-01 local-only fixture schema. The catalog is intentionally loaded in a
-- second seed file because the Supabase CLI prepares configured files separately.

create table public.p11_ap01_test_fixture (
  fixture_id text primary key,
  rule_ids text[] not null,
  description text not null,
  data jsonb not null
);
