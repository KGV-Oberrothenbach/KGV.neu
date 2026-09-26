-- Mitglieder ohne Parzellenbelegung bleiben im Export sichtbar, werden aber nach den Gärten sortiert.
create or replace function public.rpc_export_arbeitsstunden_uebersicht(
    p_jahr integer default null, p_stunden_offen boolean default false,
    p_stunden_fertig boolean default false, p_wartungsvertraege boolean default false,
    p_ansicht text default 'zusammenfassung'
)
returns table (
    mitglied_id bigint, garten_nr text, nachname text, vorname text, jahr integer,
    pflichtstunden_soll numeric, geleistete_stunden numeric, offene_stunden numeric,
    hat_wartungsvertrag boolean, wartungsvertraege text, status text, regelgrund text,
    zeilentyp text, leistendes_mitglied text, stunden_dieses_mitglieds numeric,
    datum date, taetigkeit text, sortierung integer
)
language sql stable security invoker as $$
  with basis as (
    select distinct on (v.hauptmitglied_id, v.saison_id)
      v.hauptmitglied_id as mitglied_id, m.name as nachname, m.vorname,
      v.saison_jahr as jahr, v.saison_id,
      coalesce(v.pflichtstunden_soll, 0) as pflichtstunden_soll,
      coalesce(v.geleistete_stunden, 0) as geleistete_stunden,
      coalesce(v.offene_stunden, 0) as offene_stunden,
      coalesce(v.hat_wartungsvertrag, false) as hat_wartungsvertrag,
      coalesce(v.regelgrund, '') as regelgrund
    from public.v_pflichtstunden_uebersicht v join public.mitglied m on m.id = v.hauptmitglied_id
    where m.aktiv and not coalesce(m.is_demo, false) and (p_jahr is null or v.saison_jahr = p_jahr)
    order by v.hauptmitglied_id, v.saison_id desc
  ), gaerten as (
    select b.mitglied_id, b.jahr, coalesce(string_agg(distinct p.garten_nr, ', ' order by p.garten_nr), '') as garten_nr
    from basis b left join public.parzellen_belegung pb on pb.mitglied_id = b.mitglied_id
      and pb.von_datum <= make_date(b.jahr, 12, 31) and (pb.bis_datum is null or pb.bis_datum >= make_date(b.jahr, 1, 1))
    left join public.parzelle p on p.id = pb.parzelle_id group by b.mitglied_id, b.jahr
  ), summary as (
    select b.*, g.garten_nr, coalesce(string_agg(w.titel, ', ' order by w.titel), '') as wartungsvertraege
    from basis b left join gaerten g on g.mitglied_id=b.mitglied_id and g.jahr=b.jahr
    left join public.wartungsvertrag_zuordnungen z on z.hauptmitglied_id=b.mitglied_id
      and z.gueltig_ab <= make_date(b.jahr,12,31) and (z.gueltig_bis is null or z.gueltig_bis >= make_date(b.jahr,1,1))
    left join public.wartungsvertraege w on w.id=z.wartungsvertrag_id
    group by b.mitglied_id,b.nachname,b.vorname,b.jahr,b.saison_id,b.pflichtstunden_soll,b.geleistete_stunden,b.offene_stunden,b.hat_wartungsvertrag,b.regelgrund,g.garten_nr
  ), filtered as (
    select * from summary where not (p_stunden_offen or p_stunden_fertig or p_wartungsvertraege)
      or (p_stunden_offen and not hat_wartungsvertrag and offene_stunden > 0)
      or (p_stunden_fertig and not hat_wartungsvertrag and pflichtstunden_soll > 0 and offene_stunden <= 0)
      or (p_wartungsvertraege and hat_wartungsvertrag)
  ), detail as (
    select f.mitglied_id, a.mitglied_id as leistender_id, a.datum::date, a.art_der_arbeit, a.stunden,
      trim(concat_ws(' ', lm.vorname, lm.name)) as leistendes_mitglied
    from filtered f join public.arbeitsstunde a on a.saison_id=f.saison_id join public.mitglied lm on lm.id=a.mitglied_id
    where a.freigegeben and coalesce(lm.hauptmitglied_id,lm.id)=f.mitglied_id
  ), rows as (
    select f.*, 'Zusammenfassung'::text as zeilentyp, ''::text as leistendes_mitglied,
      null::numeric as stunden_dieses_mitglieds, null::date as datum, ''::text as taetigkeit, 0 as sortierung from filtered f
    union all
    select f.*, 'Davon Mitglied'::text, d.leistendes_mitglied, sum(d.stunden), null::date, ''::text, 1
    from filtered f join detail d on d.mitglied_id=f.mitglied_id
    where lower(coalesce(p_ansicht,'zusammenfassung')) in ('pro_mitglied','einzeln')
    group by f.mitglied_id,f.garten_nr,f.nachname,f.vorname,f.jahr,f.saison_id,f.pflichtstunden_soll,f.geleistete_stunden,f.offene_stunden,f.hat_wartungsvertrag,f.wartungsvertraege,f.regelgrund,d.leistendes_mitglied
    union all
    select f.*, 'Einzelbuchung'::text, d.leistendes_mitglied, d.stunden, d.datum, coalesce(d.art_der_arbeit,''), 2
    from filtered f join detail d on d.mitglied_id=f.mitglied_id
    where lower(coalesce(p_ansicht,'zusammenfassung')) = 'einzeln'
  )
  select mitglied_id,garten_nr,nachname,vorname,jahr,pflichtstunden_soll,geleistete_stunden,offene_stunden,
    hat_wartungsvertrag,wartungsvertraege,
    case when hat_wartungsvertrag then 'Wartungsvertrag - befreit' when pflichtstunden_soll>0 and offene_stunden>0 then 'Stunden offen' when pflichtstunden_soll>0 then 'Stunden fertig' else 'Befreit' end,
    regelgrund,zeilentyp,leistendes_mitglied,stunden_dieses_mitglieds,datum,taetigkeit,sortierung
  from rows order by nullif(garten_nr, '') nulls last, garten_nr, nachname, vorname, sortierung, leistendes_mitglied, datum;
$$;
