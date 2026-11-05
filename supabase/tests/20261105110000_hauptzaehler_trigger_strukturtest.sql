-- G3.5-Strukturtest – berechtigungsfrei, nur gegen Demo-/Testdatenbanken.
-- Prüft den ausgerollten Hauptzähler-Triggervertrag ohne app_user-Kontext.
-- Keine Datenänderung; die Transaktion dient als explizite Schutzgrenze.

begin;

do $$
declare
  v_count integer;
begin
  select count(*) into v_count
  from public.hauptzaehler_messstelle
  where (medium, anlagenbereich) in (
    ('strom', 'Nord'), ('strom', 'Süd'), ('wasser', 'Nord'), ('wasser', 'Süd')
  );
  if v_count <> 4 then
    raise exception 'Der Strukturtest erwartet genau vier kanonische Hauptzähler-Messstellen, gefunden: %.', v_count;
  end if;

  if exists (
    select 1
    from public.hauptzaehler_messstelle
    where medium not in ('strom', 'wasser')
       or anlagenbereich not in ('Nord', 'Süd')
  ) then
    raise exception 'Hauptzähler-Messstellen enthalten nicht kanonische Werte.';
  end if;

  if to_regclass('public.ux_zaehler_aktiver_hauptzaehler_messstelle') is null then
    raise exception 'Der Partial-Unique-Index für aktive Hauptzähler fehlt.';
  end if;

  if to_regprocedure('public.execute_hauptzaehler_meter_change(bigint,public.zaehler_medium,text,date,numeric,text,text,date,date,numeric,text)') is null then
    raise exception 'Die Hauptzählerwechsel-RPC fehlt.';
  end if;

  if not exists (
    select 1 from pg_trigger
    where tgrelid = 'public.zaehler'::regclass
      and not tgisinternal
      and tgfoid = 'public.trg_zaehler_validate_medium_allowed()'::regprocedure
  ) or not exists (
    select 1 from pg_trigger
    where tgrelid = 'public.zaehler'::regclass
      and not tgisinternal
      and tgfoid = 'public.trg_zaehler_validate_parzelle_rfid()'::regprocedure
  ) then
    raise exception 'Mindestens eine Hauptzähler-relevante Zählervalidierung fehlt.';
  end if;

  if not exists (
    select 1 from pg_policies
    where schemaname = 'public'
      and tablename = 'hauptzaehler_messstelle'
      and policyname = 'hauptzaehler_messstelle_select_can_read_meters'
  ) then
    raise exception 'Die RLS-Lesepolicy für Hauptzähler-Messstellen fehlt.';
  end if;
end;
$$;

rollback;
