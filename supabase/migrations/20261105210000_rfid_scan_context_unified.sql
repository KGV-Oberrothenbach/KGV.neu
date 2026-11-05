-- G4: gemeinsamer RFID-Resolver für Parzellen- und Hauptzähler-Messstellen.

begin;

create or replace function public.resolve_rfid_scan_context(p_rfid_tag_uid text)
returns table(
  parzelle_id bigint,
  ist_hauptzaehler boolean,
  anlagenbereich text,
  anlage text,
  garten_nr text,
  medium text,
  rfid_tag_uid text,
  aktiver_zaehler_id bigint,
  zaehlernummer text,
  eichdatum date,
  eichfaellig_am date,
  eingebaut_am date,
  ausgebaut_am date,
  status text
)
language plpgsql security definer set search_path to 'public', 'pg_temp'
as $$
declare v_uid text := upper(btrim(coalesce(p_rfid_tag_uid, '')));
begin
  if not public.has_effective_permission(32) then
    raise exception 'Für die RFID-Auflösung fehlt CanReadMeters.' using errcode = '42501';
  end if;
  if v_uid = '' then return; end if;

  return query
  select v.parzelle_id::bigint, false, null::text, v.anlage, v.garten_nr,
         v.medium::text, v.rfid_tag_uid, v.aktiver_zaehler_id::bigint,
         v.zaehlernummer, v.eichdatum, v.eichfaellig_am, v.eingebaut_am,
         v.ausgebaut_am, v.status::text
  from public.v_rfid_scan_context v
  where upper(btrim(v.rfid_tag_uid)) = v_uid
  union all
  select null::bigint, true, h.anlagenbereich, h.anlagenbereich,
         null::text, h.medium, h.rfid_tag_uid, z.id,
         z.zaehlernummer, z.eichdatum, z.eichfaellig_am, z.eingebaut_am,
         z.ausgebaut_am, z.status::text
  from public.hauptzaehler_messstelle h
  left join public.zaehler z on z.ist_hauptzaehler = true
    and z.medium::text = h.medium and z.anlagenbereich = h.anlagenbereich
    and z.status = 'aktiv' and z.ausgebaut_am is null
  where upper(btrim(h.rfid_tag_uid)) = v_uid;
end;
$$;

revoke all on function public.resolve_rfid_scan_context(text) from public;
grant execute on function public.resolve_rfid_scan_context(text) to authenticated;

commit;
