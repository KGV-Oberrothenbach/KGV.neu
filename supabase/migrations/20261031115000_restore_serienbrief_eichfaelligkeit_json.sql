-- Generischen Export-Wrapper nach Erweiterung der Eichfälligkeits-RPC wiederherstellen.

create or replace function public.rpc_export_zaehler_eichfaelligkeit_json(
  p_parameters jsonb default '{}'::jsonb
)
returns setof jsonb
language sql
stable
security invoker
set search_path = public, pg_temp
as $$
  select to_jsonb(r)
  from public.rpc_export_zaehler_eichfaelligkeit(
    nullif(p_parameters ->> 'p_medium', '')
  ) r;
$$;

-- Direkter Zugriff auf die fachliche Export-RPC bleibt gesperrt.
-- Zugriff erfolgt über den bestehenden generischen Export-Gateway.
revoke all on function public.rpc_export_zaehler_eichfaelligkeit(text)
from public, authenticated;

comment on function public.rpc_export_zaehler_eichfaelligkeit_json(jsonb) is
  'Generischer JSON-Wrapper für den Eichfälligkeitsexport einschließlich Serienbrief-Empfängerdaten.';
