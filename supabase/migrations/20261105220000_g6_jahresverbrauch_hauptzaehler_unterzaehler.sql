-- G6: lesbarer Jahresverbrauch je Hauptzähler-Messstelle und zugeordneten
-- Unterzählern. Hauptzähler haben keine Parzelle; Unterzähler werden allein
-- über parzelle."Anlage" Nord/Süd zugeordnet.

begin;

create or replace function public.rpc_hauptzaehler_jahresverbrauch_segmente(p_jahr integer)
returns table (
  jahr integer,
  medium text,
  anlagenbereich text,
  zaehler_id bigint,
  zaehlernummer text,
  von_datum date,
  bis_datum date,
  start_ablesung_id bigint,
  end_ablesung_id bigint,
  startstand numeric,
  endstand numeric,
  verbrauch numeric,
  berechenbar boolean,
  fehlercode text,
  fehlertext text
)
language plpgsql stable security definer set search_path to 'public', 'pg_temp'
as $$
#variable_conflict use_column
begin
  if p_jahr is null or p_jahr not between 2000 and 2100 then
    raise exception 'Das Abrechnungsjahr ist ungültig.' using errcode = '22023';
  end if;
  if not public.has_effective_permission(32) then
    raise exception 'Für die Verbrauchsauswertung fehlt CanReadMeters.' using errcode = '42501';
  end if;

  return query
  with parameter as (
    select p_jahr as jahr, make_date(p_jahr, 1, 1) as jahresbeginn,
           make_date(p_jahr, 12, 31) as jahresende
  ), approved as (
    select a.id, a.zaehler_id, a.ablesedatum::date as datum, a.stand,
           lower(a.art::text) as art
    from public.zaehler_ablesung a
    where a.freigegeben or lower(coalesce(a.pruefstatus, '')) = 'freigegeben'
  ), meters as (
    select z.id as zaehler_id, z.medium::text as meter_medium,
           z.anlagenbereich, z.zaehlernummer, z.eingebaut_am, z.ausgebaut_am,
           p.jahr, p.jahresbeginn, p.jahresende
    from public.zaehler z cross join parameter p
    where z.ist_hauptzaehler and z.parzelle_id is null
      and z.eingebaut_am <= p.jahresende
      and (z.ausgebaut_am is null or z.ausgebaut_am >= p.jahresbeginn)
  ), bounds as (
    select m.*,
      prev.ok_count as prev_ok_count, prev.id as prev_id, prev.datum as prev_datum, prev.stand as prev_stand,
      prev_raw.raw_count as prev_raw_count,
      install.ok_count as install_ok_count, install.id as install_id, install.datum as install_datum, install.stand as install_stand,
      install_raw.raw_count as install_raw_count,
      current.ok_count as current_ok_count, current.id as current_id, current.datum as current_datum, current.stand as current_stand,
      current_raw.raw_count as current_raw_count,
      removal.ok_count as removal_ok_count, removal.id as removal_id, removal.datum as removal_datum, removal.stand as removal_stand,
      removal_raw.raw_count as removal_raw_count
    from meters m
    left join lateral (select count(*)::integer ok_count, min(a.id) id, min(a.datum) datum, min(a.stand) stand
      from approved a where a.zaehler_id=m.zaehler_id and a.art='jea' and extract(year from a.datum)::integer=m.jahr-1) prev on true
    left join lateral (select count(*)::integer raw_count from public.zaehler_ablesung a
      where a.zaehler_id=m.zaehler_id and lower(a.art::text)='jea' and extract(year from a.ablesedatum)::integer=m.jahr-1) prev_raw on true
    left join lateral (select count(*)::integer ok_count, min(a.id) id, min(a.datum) datum, min(a.stand) stand
      from approved a where a.zaehler_id=m.zaehler_id and a.art='einbau' and a.datum=m.eingebaut_am) install on true
    left join lateral (select count(*)::integer raw_count from public.zaehler_ablesung a
      where a.zaehler_id=m.zaehler_id and lower(a.art::text)='einbau' and a.ablesedatum::date=m.eingebaut_am) install_raw on true
    left join lateral (select count(*)::integer ok_count, min(a.id) id, min(a.datum) datum, min(a.stand) stand
      from approved a where a.zaehler_id=m.zaehler_id and a.art='jea' and extract(year from a.datum)::integer=m.jahr) current on true
    left join lateral (select count(*)::integer raw_count from public.zaehler_ablesung a
      where a.zaehler_id=m.zaehler_id and lower(a.art::text)='jea' and extract(year from a.ablesedatum)::integer=m.jahr) current_raw on true
    left join lateral (select count(*)::integer ok_count, min(a.id) id, min(a.datum) datum, min(a.stand) stand
      from approved a where a.zaehler_id=m.zaehler_id and a.art='ausbau' and a.datum=m.ausgebaut_am) removal on true
    left join lateral (select count(*)::integer raw_count from public.zaehler_ablesung a
      where a.zaehler_id=m.zaehler_id and lower(a.art::text)='ausbau' and a.ablesedatum::date=m.ausgebaut_am) removal_raw on true
  ), selected as (
    select b.*,
      (b.eingebaut_am >= b.jahresbeginn) as starts_with_install,
      (b.ausgebaut_am between b.jahresbeginn and b.jahresende) as ends_with_removal
    from bounds b
  ), resolved as (
    select s.*,
      case when starts_with_install then install_id else prev_id end as start_ablesung_id,
      case when starts_with_install then install_datum else prev_datum end as von_datum,
      case when starts_with_install then install_stand else prev_stand end as startstand,
      case when ends_with_removal then removal_id else current_id end as end_ablesung_id,
      case when ends_with_removal then removal_datum else current_datum end as bis_datum,
      case when ends_with_removal then removal_stand else current_stand end as endstand,
      case when starts_with_install then install_ok_count else prev_ok_count end as start_ok_count,
      case when starts_with_install then install_raw_count else prev_raw_count end as start_raw_count,
      case when ends_with_removal then removal_ok_count else current_ok_count end as end_ok_count,
      case when ends_with_removal then removal_raw_count else current_raw_count end as end_raw_count
    from selected s
  ), classified as (
    select r.*,
      case when start_ok_count = 1 then null
           when start_ok_count > 1 then 'ABLESUNG_UNEINDEUTIG'
           when start_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN'
           else 'STARTABLESUNG_FEHLT' end as start_fehlercode,
      case when end_ok_count = 1 then null
           when end_ok_count > 1 then 'ABLESUNG_UNEINDEUTIG'
           when end_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN'
           else 'ENDABLESUNG_FEHLT' end as end_fehlercode
    from resolved r
  )
  select c.jahr, c.meter_medium, c.anlagenbereich, c.zaehler_id, c.zaehlernummer,
    c.von_datum, c.bis_datum, c.start_ablesung_id, c.end_ablesung_id, c.startstand, c.endstand,
    case when c.start_fehlercode is null and c.end_fehlercode is null and c.endstand >= c.startstand
      then c.endstand-c.startstand end,
    c.start_fehlercode is null and c.end_fehlercode is null and c.startstand is not null
      and c.endstand is not null and c.endstand >= c.startstand,
    coalesce(c.start_fehlercode, c.end_fehlercode,
      case when c.endstand < c.startstand then 'NEGATIVER_VERBRAUCH' end),
    case when c.start_fehlercode='STARTABLESUNG_FEHLT' then 'Die erforderliche Startablesung fehlt.'
         when c.end_fehlercode='ENDABLESUNG_FEHLT' then 'Die erforderliche Endablesung fehlt.'
         when c.start_fehlercode='ABLESUNG_NICHT_FREIGEGEBEN' or c.end_fehlercode='ABLESUNG_NICHT_FREIGEGEBEN' then 'Eine erforderliche Ablesung ist nicht freigegeben.'
         when c.start_fehlercode='ABLESUNG_UNEINDEUTIG' or c.end_fehlercode='ABLESUNG_UNEINDEUTIG' then 'Mehrere freigegebene Ablesungen passen zur Segmentgrenze.'
         when c.endstand < c.startstand then 'Der Endstand ist kleiner als der Startstand.' end
  from classified c
  order by c.meter_medium, c.anlagenbereich, c.von_datum, c.zaehlernummer;
end;
$$;

create or replace function public.rpc_jahresverbrauch_haupt_und_unterzaehler(p_jahr integer)
returns table (
  jahr integer,
  medium text,
  anlagenbereich text,
  hauptzaehler_verbrauch numeric,
  unterzaehler_verbrauch numeric,
  hauptzaehler_berechenbar boolean,
  unterzaehler_berechenbar boolean,
  hauptzaehler_fehlercode text,
  hauptzaehler_fehlertext text,
  unterzaehler_fehlercode text,
  unterzaehler_fehlertext text
)
language plpgsql stable security definer set search_path to 'public', 'pg_temp'
as $$
#variable_conflict use_column
begin
  if p_jahr is null or p_jahr not between 2000 and 2100 then
    raise exception 'Das Abrechnungsjahr ist ungültig.' using errcode = '22023';
  end if;
  if not public.has_effective_permission(32) then
    raise exception 'Für die Verbrauchsauswertung fehlt CanReadMeters.' using errcode = '42501';
  end if;

  return query
  with combinations as (
    select * from (values ('strom'::text, 'Nord'::text), ('strom', 'Süd'), ('wasser', 'Nord'), ('wasser', 'Süd')) v(medium, anlagenbereich)
  ), hz_segments as (
    select * from public.rpc_hauptzaehler_jahresverbrauch_segmente(p_jahr)
  ), hz as (
    select hs.medium as segment_medium, hs.anlagenbereich as segment_bereich, count(*)::integer segment_count,
      bool_and(hs.berechenbar) as berechenbar,
      case when bool_and(hs.berechenbar) then sum(hs.verbrauch) end as verbrauch,
      string_agg(distinct hs.fehlercode, ';' order by hs.fehlercode) filter (where hs.fehlercode is not null) as fehlercode,
      string_agg(distinct hs.fehlertext, ' ' order by hs.fehlertext) filter (where hs.fehlertext is not null) as fehlertext
    from hz_segments hs group by hs.medium, hs.anlagenbereich
  ), uz_meters as (
    select z.id, z.medium::text as medium, p."Anlage" as anlagenbereich
    from public.zaehler z join public.parzelle p on p.id=z.parzelle_id
    where not z.ist_hauptzaehler and z.eingebaut_am <= make_date(p_jahr,12,31)
      and (z.ausgebaut_am is null or z.ausgebaut_am >= make_date(p_jahr,1,1))
      and p."Anlage" in ('Nord','Süd')
  ), uz_segments as (
    select lower(s.medium) as segment_medium, p."Anlage" as segment_bereich,
           s.berechenbar, s.verbrauch, s.fehlercode, s.fehlertext
    from public.rpc_jahresverbrauch_segmente(p_jahr) s
    join public.parzelle p on p.id=s.parzelle_id
  ), uz as (
    select us.segment_medium, us.segment_bereich, count(*)::integer segment_count,
      bool_and(us.berechenbar) as berechenbar,
      case when bool_and(us.berechenbar) then sum(us.verbrauch) end as verbrauch,
      string_agg(distinct us.fehlercode, ';' order by us.fehlercode) filter (where us.fehlercode is not null) as fehlercode,
      string_agg(distinct us.fehlertext, ' ' order by us.fehlertext) filter (where us.fehlertext is not null) as fehlertext
    from uz_segments us group by us.segment_medium, us.segment_bereich
  ), uz_counts as (
    select um.medium as segment_medium, um.anlagenbereich as segment_bereich, count(*)::integer meter_count
    from uz_meters um group by um.medium, um.anlagenbereich
  )
  select p_jahr, c.medium, c.anlagenbereich,
    h.verbrauch, u.verbrauch,
    coalesce(h.berechenbar, false),
    coalesce(u.berechenbar, false),
    coalesce(h.fehlercode, 'HAUPTZAEHLER_FEHLT'),
    coalesce(h.fehlertext, 'Für diese Messstelle existiert im Abrechnungsjahr kein Hauptzählersegment.'),
    case when coalesce(uc.meter_count,0)=0 then 'UNTERZAEHLER_FEHLEN'
         else coalesce(u.fehlercode, 'UNTERZAEHLER_UNVOLLSTAENDIG') end,
    case when coalesce(uc.meter_count,0)=0 then 'Für Anlage und Medium existiert im Abrechnungsjahr kein Unterzähler.'
         else coalesce(u.fehlertext, 'Unterzählersegmente sind unvollständig.') end
  from combinations c
  left join hz h on h.segment_medium=c.medium and h.segment_bereich=c.anlagenbereich
  left join uz u on u.segment_medium=c.medium and u.segment_bereich=c.anlagenbereich
  left join uz_counts uc on uc.segment_medium=c.medium and uc.segment_bereich=c.anlagenbereich
  order by c.medium, c.anlagenbereich;
end;
$$;

revoke all on function public.rpc_hauptzaehler_jahresverbrauch_segmente(integer) from public;
revoke all on function public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer) from public;
grant execute on function public.rpc_hauptzaehler_jahresverbrauch_segmente(integer) to authenticated;
grant execute on function public.rpc_jahresverbrauch_haupt_und_unterzaehler(integer) to authenticated;

commit;
