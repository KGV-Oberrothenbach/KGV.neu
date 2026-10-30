-- Serienbrief G3: erster metadata-basierter Referenzexport.
-- Empfängerpriorität: aktiver Hauptpächter, sonst genau eine aktuelle Legacy-Belegung.
create or replace function public.rpc_export_zaehler_eichfaelligkeit(p_medium text default null)
returns table (mitglied_id bigint, parzelle_id bigint, serienbrief_empfaenger_status text, vorname text, nachname text, parzelle text, medium text, zaehler_id bigint, zaehlernummer text, rfid text, zaehler_status text, eingebaut_am date, eichdatum date, eichfaellig_am date, tage_bis_faellig integer, faelligkeitsstatus text)
language sql stable security invoker set search_path = public, pg_temp as $$
    select recipient.mitglied_id, e.parzelle_id, recipient.status, m.vorname, m.name,
        concat_ws(' ', e.anlage, e.garten_nr),
        case e.medium::text when 'strom' then 'Strom' when 'wasser' then 'Wasser' else e.medium::text end,
        e.id, e.zaehlernummer, case e.medium::text when 'wasser' then p.rfid_wasser else p.rfid_strom end,
        case e.status::text when 'aktiv' then 'Aktiv' else e.status::text end,
        e.eingebaut_am, e.eichdatum, e.eichfaellig_am, e.tage_bis_faellig,
        case e.eichstatus when 'ueberfaellig' then 'Überfällig' when 'bald_faellig' then 'Bald fällig' else e.eichstatus end
    from public.v_zaehler_eichstatus e
    join public.parzelle p on p.id = e.parzelle_id
    left join lateral (
        select pv.id, exists(select 1 from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id=pv.id and b.gueltig_ab<=current_date and (b.gueltig_bis is null or b.gueltig_bis>=current_date)) as hat_aktive_beteiligung,
          (select b.mitglied_id from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id=pv.id and b.rolle='hauptpaechter' and b.gueltig_ab<=current_date and (b.gueltig_bis is null or b.gueltig_bis>=current_date) limit 1) as hauptpaechter_mitglied_id
        from public.pachtverhaeltnis pv where pv.parzelle_id=e.parzelle_id and pv.beendet_am is null
    ) current_lease on true
    left join lateral (
        select count(*)::integer as anzahl, min(pb.mitglied_id) as mitglied_id from public.parzellen_belegung pb
        where pb.parzelle_id=e.parzelle_id and pb.von_datum<=current_date and (pb.bis_datum is null or pb.bis_datum>=current_date)
    ) legacy on true
    cross join lateral (
        select case when coalesce(current_lease.hat_aktive_beteiligung,false) and current_lease.hauptpaechter_mitglied_id is not null then current_lease.hauptpaechter_mitglied_id when not coalesce(current_lease.hat_aktive_beteiligung,false) and legacy.anzahl=1 then legacy.mitglied_id else null end as mitglied_id,
          case when coalesce(current_lease.hat_aktive_beteiligung,false) and current_lease.hauptpaechter_mitglied_id is not null then 'hauptpaechter' when coalesce(current_lease.hat_aktive_beteiligung,false) then 'nicht_ermittelbar' when legacy.anzahl=1 then 'legacy_eindeutig' when legacy.anzahl>1 then 'mehrdeutig' else 'nicht_ermittelbar' end as status
    ) recipient
    left join public.mitglied m on m.id=recipient.mitglied_id
    where e.eichstatus in ('ueberfaellig','bald_faellig') and (nullif(btrim(p_medium),'') is null or e.medium::text=lower(btrim(p_medium)))
    order by case when nullif(btrim(e.garten_nr),'') is null then 1 else 0 end, substring(e.garten_nr from '^[[:space:]]*([0-9]+)')::integer nulls last, e.garten_nr, e.anlage, e.medium, e.zaehlernummer, e.id;
$$;
grant execute on function public.rpc_export_zaehler_eichfaelligkeit(text) to authenticated;

update public.app_export_definition set serienbrief_erlaubt=true, serienbrief_mitglied_id_feld='mitglied_id', serienbrief_parzelle_id_feld='parzelle_id' where export_key='zaehler_eichfaelligkeit';
insert into public.app_export_column_definition (export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte,serienbrief_freigegeben,serienbrief_gruppe) values
 ('zaehler_eichfaelligkeit','vorname','Vorname','Vorname',12,false,false,true,'Mitglied'),
 ('zaehler_eichfaelligkeit','nachname','Nachname','Nachname',14,false,false,true,'Mitglied')
on conflict(export_key,column_key) do update set label_kurz=excluded.label_kurz,label_lang=excluded.label_lang,sortierung=excluded.sortierung,serienbrief_freigegeben=excluded.serienbrief_freigegeben,serienbrief_gruppe=excluded.serienbrief_gruppe;
update public.app_export_column_definition set serienbrief_freigegeben=true, serienbrief_gruppe=case column_key when 'parzelle' then 'Parzelle' else 'Zähler' end where export_key='zaehler_eichfaelligkeit' and column_key in ('parzelle','medium','zaehlernummer','eichdatum','eichfaellig_am','faelligkeitsstatus');
comment on function public.rpc_export_zaehler_eichfaelligkeit(text) is 'Eichfälligkeitsexport mit Serienbriefempfänger: aktiver Hauptpächter vor eindeutiger Legacy-Belegung; Konflikte bleiben mit Status und NULL mitglied_id sichtbar.';
