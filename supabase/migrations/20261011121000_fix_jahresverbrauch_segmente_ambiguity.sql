-- Read-only Verbrauchssegmente für eine Saison. Es werden keine
-- Jahresabschluss-Snapshots geschrieben; alle Grenzen sind reale, freigegebene Ablesungen.
create or replace function public.rpc_jahresverbrauch_segmente(p_jahr integer)
returns table (
    jahr integer,
    parzelle_id bigint,
    parzelle text,
    mitglied_id bigint,
    pachtbezug text,
    medium text,
    zaehler_id bigint,
    zaehlernummer text,
    von_datum date,
    bis_datum date,
    start_ablesung_id bigint,
    end_ablesung_id bigint,
    startstand numeric,
    endstand numeric,
    verbrauch numeric,
    grund text,
    berechenbar boolean,
    fehlercode text,
    fehlertext text
)
language sql
stable
security invoker
as $$
with parameter as (
    select p_jahr as jahr,
           make_date(p_jahr, 1, 1) as jahresbeginn,
           make_date(p_jahr, 12, 31) as jahresende
), approved_readings as (
    select a.id, a.zaehler_id, a.ablesedatum::date as datum, a.stand, lower(a.art::text) as art
    from public.zaehler_ablesung a
    where a.freigegeben or lower(coalesce(a.pruefstatus::text, '')) = 'freigegeben'
), meters as (
    select z.id as zaehler_id, z.parzelle_id, z.medium::text as medium, z.zaehlernummer,
           z.eingebaut_am, z.ausgebaut_am, p.jahr, p.jahresbeginn, p.jahresende
    from public.zaehler z
    cross join parameter p
    where z.eingebaut_am <= p.jahresende
      and (z.ausgebaut_am is null or z.ausgebaut_am >= p.jahresbeginn)
), meter_bounds as (
    select m.*,
           previous_jea.id as previous_jea_id, previous_jea.datum as previous_jea_datum,
           previous_jea.stand as previous_jea_stand, previous_jea_raw.count as previous_jea_raw_count,
           installation.id as installation_id, installation.datum as installation_datum,
           installation.stand as installation_stand, installation_raw.count as installation_raw_count,
           current_jea.id as current_jea_id, current_jea.datum as current_jea_datum,
           current_jea.stand as current_jea_stand, current_jea_raw.count as current_jea_raw_count,
           removal.id as removal_id, removal.datum as removal_datum,
           removal.stand as removal_stand, removal_raw.count as removal_raw_count,
           previous_jea_any.datum as previous_jea_any_datum
    from meters m
    left join lateral (
        select a.id, a.datum, a.stand from approved_readings a
        where a.zaehler_id = m.zaehler_id and a.art = 'jea'
          and extract(year from a.datum)::integer = m.jahr - 1
        order by a.datum desc, a.id desc limit 1
    ) previous_jea on true
    left join lateral (
        select count(*)::integer as count from public.zaehler_ablesung a
        where a.zaehler_id = m.zaehler_id and lower(a.art::text) = 'jea'
          and extract(year from a.ablesedatum)::integer = m.jahr - 1
    ) previous_jea_raw on true
    left join lateral (
        select max(a.ablesedatum::date) as datum from public.zaehler_ablesung a
        where a.zaehler_id = m.zaehler_id and lower(a.art::text) = 'jea'
          and extract(year from a.ablesedatum)::integer = m.jahr - 1
    ) previous_jea_any on true
    left join lateral (
        select a.id, a.datum, a.stand from approved_readings a
        where a.zaehler_id = m.zaehler_id and a.art = 'einbau' and a.datum = m.eingebaut_am
        order by a.id desc limit 1
    ) installation on true
    left join lateral (
        select count(*)::integer as count from public.zaehler_ablesung a
        where a.zaehler_id = m.zaehler_id and lower(a.art::text) = 'einbau'
          and a.ablesedatum::date = m.eingebaut_am
    ) installation_raw on true
    left join lateral (
        select a.id, a.datum, a.stand from approved_readings a
        where a.zaehler_id = m.zaehler_id and a.art = 'jea'
          and extract(year from a.datum)::integer = m.jahr
        order by a.datum desc, a.id desc limit 1
    ) current_jea on true
    left join lateral (
        select count(*)::integer as count from public.zaehler_ablesung a
        where a.zaehler_id = m.zaehler_id and lower(a.art::text) = 'jea'
          and extract(year from a.ablesedatum)::integer = m.jahr
    ) current_jea_raw on true
    left join lateral (
        select a.id, a.datum, a.stand from approved_readings a
        where a.zaehler_id = m.zaehler_id and a.art = 'ausbau' and a.datum = m.ausgebaut_am
        order by a.id desc limit 1
    ) removal on true
    left join lateral (
        select count(*)::integer as count from public.zaehler_ablesung a
        where a.zaehler_id = m.zaehler_id and lower(a.art::text) = 'ausbau'
          and a.ablesedatum::date = m.ausgebaut_am
    ) removal_raw on true
), meter_periods as (
    select b.*,
           case when b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
                then b.installation_id else b.previous_jea_id end as start_ablesung_id,
           case when b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
                then b.installation_datum else b.previous_jea_datum end as start_datum,
           case when b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
                then b.installation_stand else b.previous_jea_stand end as startstand,
           case when b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
                then case when b.installation_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN' else 'STARTABLESUNG_FEHLT' end
                else case when b.previous_jea_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN' else 'STARTABLESUNG_FEHLT' end end as start_fehlercode,
           case when b.ausgebaut_am between b.jahresbeginn and b.jahresende then b.removal_id else b.current_jea_id end as end_ablesung_id,
           case when b.ausgebaut_am between b.jahresbeginn and b.jahresende then b.removal_datum else b.current_jea_datum end as end_datum,
           case when b.ausgebaut_am between b.jahresbeginn and b.jahresende then b.removal_stand else b.current_jea_stand end as endstand,
           case when b.ausgebaut_am between b.jahresbeginn and b.jahresende
                then case when b.removal_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN' else 'ENDABLESUNG_FEHLT' end
                else case when b.current_jea_raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN' else 'ENDABLESUNG_FEHLT' end end as end_fehlercode,
           coalesce(
             case when b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
                  then b.installation_datum else b.previous_jea_datum end,
             b.eingebaut_am,
             b.jahresbeginn
           ) as segment_start_datum,
           coalesce(
             case when b.ausgebaut_am between b.jahresbeginn and b.jahresende then b.removal_datum else b.current_jea_datum end,
             b.ausgebaut_am,
             b.jahresende
           ) as segment_end_datum,
           b.eingebaut_am > coalesce(b.previous_jea_any_datum, make_date(b.jahr - 1, 12, 31))
             or b.ausgebaut_am between b.jahresbeginn and b.jahresende as zaehlerwechsel
    from meter_bounds b
), tenant_event_dates as (
    select distinct mp.zaehler_id, mp.segment_start_datum as window_start,
           mp.segment_end_datum as window_end, event_datum
    from meter_periods mp
    cross join lateral (
        select pb.von_datum as event_datum from public.parzellen_belegung pb
        where pb.parzelle_id = mp.parzelle_id
        union
        select pb.bis_datum + 1 from public.parzellen_belegung pb
        where pb.parzelle_id = mp.parzelle_id and pb.bis_datum is not null
    ) events
    where events.event_datum > mp.segment_start_datum and events.event_datum < mp.segment_end_datum
), tenant_boundaries as (
    select e.zaehler_id, e.event_datum,
           approved.count as approved_count, approved.reading_id, approved.stand,
           raw.count as raw_count
    from tenant_event_dates e
    left join lateral (
        select count(*)::integer as count, min(a.id) as reading_id, min(a.stand) as stand
        from approved_readings a
        where a.zaehler_id = e.zaehler_id and a.datum in (e.event_datum - 1, e.event_datum)
    ) approved on true
    left join lateral (
        select count(*)::integer as count from public.zaehler_ablesung a
        where a.zaehler_id = e.zaehler_id and a.ablesedatum::date in (e.event_datum - 1, e.event_datum)
    ) raw on true
), boundary_points as (
    select mp.*, mp.segment_start_datum as datum, mp.start_ablesung_id as ablesung_id, mp.startstand as stand,
           case when mp.start_ablesung_id is null then mp.start_fehlercode end as fehlercode, 'start'::text as art
    from meter_periods mp
    union all
    select mp.*, tb.event_datum, tb.reading_id, tb.stand,
           case when tb.approved_count = 1 then null
                when tb.approved_count > 1 then 'ABLESUNG_UNEINDEUTIG'
                when tb.raw_count > 0 then 'ABLESUNG_NICHT_FREIGEGEBEN'
                else 'WECHSELABLESUNG_FEHLT' end, 'paechterwechsel'
    from meter_periods mp join tenant_boundaries tb on tb.zaehler_id = mp.zaehler_id
    union all
    select mp.*, mp.segment_end_datum, mp.end_ablesung_id, mp.endstand,
           case when mp.end_ablesung_id is null then mp.end_fehlercode end, 'ende'
    from meter_periods mp
), segments as (
    select bp.*, lead(bp.datum) over w as bis_datum, lead(bp.ablesung_id) over w as segment_end_ablesung_id,
           lead(bp.stand) over w as segment_endstand, lead(bp.fehlercode) over w as segment_end_fehlercode,
           lead(bp.art) over w as segment_end_art
    from boundary_points bp
    window w as (partition by bp.zaehler_id order by bp.datum,
                 case bp.art when 'start' then 0 when 'paechterwechsel' then 1 else 2 end)
), resolved as (
    select s.*, assignment.mitglied_id, assignment.pachtbezug, assignment.count as belegung_count
    from segments s
    left join lateral (
        select count(*)::integer as count, min(pb.mitglied_id) as mitglied_id,
               min(concat_ws(' ', m.vorname, m.name)) as pachtbezug
        from public.parzellen_belegung pb
        join public.mitglied m on m.id = pb.mitglied_id
        where pb.parzelle_id = s.parzelle_id
          and pb.von_datum <= s.datum
          and (pb.bis_datum is null or pb.bis_datum >= s.datum)
    ) assignment on true
    where s.bis_datum is not null
)
select r.jahr,
       r.parzelle_id,
       concat_ws(' ', p."Anlage", p.garten_nr) as parzelle,
       case when coalesce(r.belegung_count, 0) = 1 then r.mitglied_id else null end as mitglied_id,
       case when coalesce(r.belegung_count, 0) = 1 then r.pachtbezug else 'Leerstand' end as pachtbezug,
       case r.medium when 'strom' then 'Strom' when 'wasser' then 'Wasser' else r.medium end as medium,
       r.zaehler_id, r.zaehlernummer, r.datum as von_datum, r.bis_datum,
       r.ablesung_id as start_ablesung_id, r.segment_end_ablesung_id as end_ablesung_id, r.stand as startstand, r.segment_endstand as endstand,
       case when r.fehlercode is null and r.segment_end_fehlercode is null
                 and coalesce(r.belegung_count, 0) <= 1 and r.segment_endstand >= r.stand
            then r.segment_endstand - r.stand else null end as verbrauch,
       case when r.zaehlerwechsel and (r.art = 'paechterwechsel' or r.segment_end_art = 'paechterwechsel') then 'zaehlerwechsel_und_paechterwechsel'
            when r.zaehlerwechsel then 'zaehlerwechsel'
            when r.art = 'paechterwechsel' or r.segment_end_art = 'paechterwechsel' then 'paechterwechsel'
            else 'normal' end as grund,
       r.fehlercode is null and r.segment_end_fehlercode is null and coalesce(r.belegung_count, 0) <= 1
         and r.stand is not null and r.segment_endstand is not null and r.segment_endstand >= r.stand as berechenbar,
       case when coalesce(r.belegung_count, 0) > 1 then 'BELEGUNG_UNEINDEUTIG'
            when r.fehlercode is not null then r.fehlercode
            when r.segment_end_fehlercode is not null then r.segment_end_fehlercode
            when r.stand is not null and r.segment_endstand is not null and r.segment_endstand < r.stand then 'NEGATIVER_VERBRAUCH'
            else null end as fehlercode,
       case when coalesce(r.belegung_count, 0) > 1 then 'Mehrere Belegungen sind am Segmentbeginn aktiv.'
            when r.fehlercode = 'STARTABLESUNG_FEHLT' then 'Die erforderliche Startablesung fehlt.'
            when r.fehlercode = 'ENDABLESUNG_FEHLT' or r.segment_end_fehlercode = 'ENDABLESUNG_FEHLT' then 'Die erforderliche Endablesung fehlt.'
            when r.fehlercode = 'WECHSELABLESUNG_FEHLT' or r.segment_end_fehlercode = 'WECHSELABLESUNG_FEHLT' then 'Die erforderliche Übergangsablesung fehlt.'
            when r.fehlercode = 'ABLESUNG_NICHT_FREIGEGEBEN' or r.segment_end_fehlercode = 'ABLESUNG_NICHT_FREIGEGEBEN' then 'Eine erforderliche Ablesung ist nicht freigegeben.'
            when r.fehlercode = 'ABLESUNG_UNEINDEUTIG' or r.segment_end_fehlercode = 'ABLESUNG_UNEINDEUTIG' then 'Mehrere freigegebene Ablesungen passen zur Übergangsgrenze.'
            when r.stand is not null and r.segment_endstand is not null and r.segment_endstand < r.stand then 'Der Endstand ist kleiner als der Startstand.'
            else null end as fehlertext
from resolved r
join public.parzelle p on p.id = r.parzelle_id
order by parzelle, medium, zaehlernummer, von_datum;
$$;

grant execute on function public.rpc_jahresverbrauch_segmente(integer) to authenticated;
