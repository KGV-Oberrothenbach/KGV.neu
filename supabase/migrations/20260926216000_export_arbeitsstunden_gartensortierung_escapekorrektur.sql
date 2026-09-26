-- PostgreSQL-RegEx: Leerzeichen über die POSIX-Zeichenklasse statt Backslash-Escaping erkennen.
do $$
declare
  funktion_sql text;
begin
  select pg_get_functiondef(p.oid) into funktion_sql
  from pg_proc p join pg_namespace n on n.oid = p.pronamespace
  where n.nspname = 'public' and p.proname = 'rpc_export_arbeitsstunden_uebersicht'
    and pg_get_function_identity_arguments(p.oid) = 'p_jahr integer, p_stunden_offen boolean, p_stunden_fertig boolean, p_wartungsvertraege boolean, p_ansicht text';

  if funktion_sql is null then raise exception 'Arbeitsstunden-Exportfunktion wurde nicht gefunden.'; end if;
  funktion_sql := replace(funktion_sql, '\\s', '[[:space:]]');
  execute funktion_sql;
end $$;
