-- Korrigiert die Sortierung nach der führenden Gartennummer; Leerwerte bleiben zuletzt.
do $$
declare
  funktion_sql text;
begin
  select pg_get_functiondef(p.oid) into funktion_sql
  from pg_proc p
  join pg_namespace n on n.oid = p.pronamespace
  where n.nspname = 'public'
    and p.proname = 'rpc_export_arbeitsstunden_uebersicht'
    and pg_get_function_identity_arguments(p.oid) = 'p_jahr integer, p_stunden_offen boolean, p_stunden_fertig boolean, p_wartungsvertraege boolean, p_ansicht text';

  if funktion_sql is null then
    raise exception 'Arbeitsstunden-Exportfunktion wurde nicht gefunden.';
  end if;

  funktion_sql := replace(
    funktion_sql,
    'order by nullif(garten_nr, '''') nulls last, substring(garten_nr from ''^\\s*([0-9]+)'')::integer nulls last,',
    'order by case when nullif(garten_nr, '''') is null then 1 else 0 end, substring(garten_nr from ''^\\s*([0-9]+)'')::integer nulls last,'
  );

  if position('case when nullif(garten_nr, '''') is null then 1 else 0 end' in funktion_sql) = 0 then
    raise exception 'Erwartete Sortierformel wurde nicht gefunden.';
  end if;

  execute funktion_sql;
end $$;
