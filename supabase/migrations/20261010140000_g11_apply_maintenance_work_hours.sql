-- G11.1b: the central G7 calculation owns maintenance-contract credits.
DROP VIEW IF EXISTS public.v_pflichtstunden_uebersicht;
DROP FUNCTION IF EXISTS public.fn_berechne_pflichtstunden_status(bigint, bigint);

CREATE FUNCTION public.fn_berechne_pflichtstunden_status(p_mitglied_id bigint, p_saison_id bigint)
RETURNS TABLE(hauptmitglied_id bigint, saison_id bigint, saison_jahr integer, regelgrund text, ist_befreit boolean, hat_wartungsvertrag boolean, wartungsvertrag_gutschrift_stunden numeric, altersbefreit boolean, eintritt_im_saisonjahr boolean, eintritt_zweites_halbjahr boolean, pflichtstunden_soll numeric, geleistete_stunden numeric, offene_stunden numeric, euro_pro_fehlstunde numeric, fehlbetrag numeric)
LANGUAGE plpgsql STABLE AS $$
declare
    v_hauptmitglied_id bigint; v_mitglied record; v_saison record; v_saison_start date; v_saison_ende date;
    v_regelgrund text := 'standard'; v_ist_befreit boolean := false; v_hat_wartungsvertrag boolean := false;
    v_hat_befreienden_wartungsvertrag boolean := false; v_altersbefreit boolean := false;
    v_eintritt_im_saisonjahr boolean := false; v_eintritt_zweites_halbjahr boolean := false;
    v_pflichtstunden_soll numeric := 0; v_geleistete_stunden numeric := 0; v_wartungsvertrag_gutschrift_stunden numeric := 0;
begin
    v_hauptmitglied_id := public.get_hauptmitglied_id(p_mitglied_id);
    if v_hauptmitglied_id is null then raise exception 'Mitglied % nicht gefunden.', p_mitglied_id; end if;
    select * into v_mitglied from public.mitglied where id = p_mitglied_id;
    select * into v_saison from public.saison where id = p_saison_id;
    if not found then raise exception 'Saison % nicht gefunden.', p_saison_id; end if;
    if v_saison.jahr is null then raise exception 'Saison % hat kein Jahr gesetzt.', p_saison_id; end if;
    v_saison_start := make_date(v_saison.jahr, 1, 1); v_saison_ende := make_date(v_saison.jahr, 12, 31);
    v_pflichtstunden_soll := coalesce(v_saison.pflichtstunden_soll, 0);
    v_eintritt_im_saisonjahr := extract(year from v_mitglied.mitglied_seit)::int = v_saison.jahr;
    v_eintritt_zweites_halbjahr := v_eintritt_im_saisonjahr and v_mitglied.mitglied_seit >= make_date(v_saison.jahr, 7, 1);

    select exists (
        select 1 from public.wartungsvertrag_zuordnungen z join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
        where z.hauptmitglied_id = v_hauptmitglied_id and w.aktiv = true
          and z.gueltig_ab <= v_saison_ende and (z.gueltig_bis is null or z.gueltig_bis >= v_saison_start)
    ) into v_hat_wartungsvertrag;
    select exists (
        select 1 from public.wartungsvertrag_zuordnungen z join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
        where z.hauptmitglied_id = v_hauptmitglied_id and w.aktiv = true and w.befreit_von_pflichtstunden = true
          and z.gueltig_ab <= v_saison_ende and (z.gueltig_bis is null or z.gueltig_bis >= v_saison_start)
    ) into v_hat_befreienden_wartungsvertrag;
    select coalesce(sum(credit.arbeitsstunden_gutschrift), 0) into v_wartungsvertrag_gutschrift_stunden
    from (
        select distinct w.id, w.arbeitsstunden_gutschrift
        from public.wartungsvertrag_zuordnungen z join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
        where z.hauptmitglied_id = v_hauptmitglied_id and w.aktiv = true
          and z.gueltig_ab <= v_saison_ende and (z.gueltig_bis is null or z.gueltig_bis >= v_saison_start)
    ) credit;

    if v_mitglied.mitglied_seit > v_saison_ende or (v_mitglied.mitglied_ende is not null and v_mitglied.mitglied_ende < v_saison_start) then
        v_pflichtstunden_soll := 0; v_ist_befreit := true; v_regelgrund := 'keine_aktive_mitgliedschaft';
    else
        if v_mitglied.geburtsdatum is not null then
            v_altersbefreit := case v_mitglied.arbeitsstunden_altersregel_typ when 'frau75' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 75 when 'mann80' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 80 else false end;
        end if;
        if v_hat_befreienden_wartungsvertrag then
            v_pflichtstunden_soll := 0; v_ist_befreit := true; v_regelgrund := 'wartungsvertrag';
        elsif v_altersbefreit then
            v_pflichtstunden_soll := 0; v_ist_befreit := true; v_regelgrund := 'altersbefreiung';
        elsif v_eintritt_zweites_halbjahr then
            v_pflichtstunden_soll := round(v_pflichtstunden_soll / 2.0, 2); v_regelgrund := 'eintritt_2_halbjahr';
        end if;
    end if;
    if v_ist_befreit or v_pflichtstunden_soll <= 0 then v_wartungsvertrag_gutschrift_stunden := 0;
    else v_wartungsvertrag_gutschrift_stunden := least(greatest(v_wartungsvertrag_gutschrift_stunden, 0), v_pflichtstunden_soll); end if;
    select coalesce(sum(a.stunden), 0) into v_geleistete_stunden from public.arbeitsstunde a join public.mitglied m on m.id = a.mitglied_id
    where a.saison_id = p_saison_id and a.freigegeben = true and coalesce(m.hauptmitglied_id, m.id) = v_hauptmitglied_id;
    return query select v_hauptmitglied_id, p_saison_id, v_saison.jahr, v_regelgrund, v_ist_befreit, v_hat_wartungsvertrag, v_wartungsvertrag_gutschrift_stunden, v_altersbefreit, v_eintritt_im_saisonjahr, v_eintritt_zweites_halbjahr, v_pflichtstunden_soll, v_geleistete_stunden, greatest(v_pflichtstunden_soll - v_geleistete_stunden - v_wartungsvertrag_gutschrift_stunden, 0), coalesce(v_saison.euro_pro_fehlstunde, 0), greatest(v_pflichtstunden_soll - v_geleistete_stunden - v_wartungsvertrag_gutschrift_stunden, 0) * coalesce(v_saison.euro_pro_fehlstunde, 0);
end;
$$;

CREATE VIEW public.v_pflichtstunden_uebersicht AS
SELECT s.id AS saison_id, s.jahr, s.jahr AS saison_jahr, m.id AS mitglied_id, coalesce(m.hauptmitglied_id, m.id) AS hauptmitglied_id, m.name, m.vorname,
  x.regelgrund, x.ist_befreit, x.hat_wartungsvertrag, x.wartungsvertrag_gutschrift_stunden, x.altersbefreit, x.eintritt_im_saisonjahr, x.eintritt_zweites_halbjahr, x.pflichtstunden_soll, x.geleistete_stunden, x.offene_stunden, x.euro_pro_fehlstunde, x.fehlbetrag
FROM public.saison s CROSS JOIN public.mitglied m CROSS JOIN LATERAL public.fn_berechne_pflichtstunden_status(m.id, s.id::bigint) x;

GRANT ALL ON TABLE public.v_pflichtstunden_uebersicht TO authenticated, service_role;
GRANT ALL ON FUNCTION public.fn_berechne_pflichtstunden_status(bigint, bigint) TO anon, authenticated, service_role;

-- The export uses the central result and no longer treats every maintenance contract as an exemption.
DROP FUNCTION IF EXISTS public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean, text);
CREATE FUNCTION public.rpc_export_arbeitsstunden_uebersicht(p_jahr integer default null, p_stunden_offen boolean default false, p_stunden_fertig boolean default false, p_wartungsvertraege boolean default false, p_ansicht text default 'zusammenfassung')
RETURNS TABLE(mitglied_id bigint, garten_nr text, nachname text, vorname text, jahr integer, pflichtstunden_soll numeric, geleistete_stunden numeric, wartungsvertrag_gutschrift_stunden numeric, offene_stunden numeric, hat_wartungsvertrag boolean, wartungsvertraege text, status text, regelgrund text, zeilentyp text, leistendes_mitglied text, stunden_dieses_mitglieds numeric, datum date, taetigkeit text, sortierung integer)
LANGUAGE sql STABLE SECURITY INVOKER AS $$
  with basis as (
    select distinct on (v.hauptmitglied_id, v.saison_id) v.hauptmitglied_id as mitglied_id, m.name as nachname, m.vorname, v.saison_jahr as jahr, v.saison_id, coalesce(v.pflichtstunden_soll,0) pflichtstunden_soll, coalesce(v.geleistete_stunden,0) geleistete_stunden, coalesce(v.wartungsvertrag_gutschrift_stunden,0) wartungsvertrag_gutschrift_stunden, coalesce(v.offene_stunden,0) offene_stunden, coalesce(v.hat_wartungsvertrag,false) hat_wartungsvertrag, coalesce(v.regelgrund,'') regelgrund
    from public.v_pflichtstunden_uebersicht v join public.mitglied m on m.id=v.hauptmitglied_id where m.aktiv and not coalesce(m.is_demo,false) and (p_jahr is null or v.saison_jahr=p_jahr) order by v.hauptmitglied_id,v.saison_id desc
  ), summary as (
    select b.*, ''::text garten_nr, coalesce(string_agg(distinct w.titel, ', ' order by w.titel),'') wartungsvertraege from basis b left join public.wartungsvertrag_zuordnungen z on z.hauptmitglied_id=b.mitglied_id and z.gueltig_ab<=make_date(b.jahr,12,31) and (z.gueltig_bis is null or z.gueltig_bis>=make_date(b.jahr,1,1)) left join public.wartungsvertraege w on w.id=z.wartungsvertrag_id group by b.mitglied_id,b.nachname,b.vorname,b.jahr,b.saison_id,b.pflichtstunden_soll,b.geleistete_stunden,b.wartungsvertrag_gutschrift_stunden,b.offene_stunden,b.hat_wartungsvertrag,b.regelgrund
  ), filtered as (select * from summary where not(p_stunden_offen or p_stunden_fertig or p_wartungsvertraege) or (p_stunden_offen and offene_stunden>0) or (p_stunden_fertig and pflichtstunden_soll>0 and offene_stunden<=0) or (p_wartungsvertraege and hat_wartungsvertrag))
  select mitglied_id,garten_nr,nachname,vorname,jahr,pflichtstunden_soll,geleistete_stunden,wartungsvertrag_gutschrift_stunden,offene_stunden,hat_wartungsvertrag,wartungsvertraege,case when regelgrund='wartungsvertrag' then 'Wartungsvertrag – befreit' when pflichtstunden_soll>0 and offene_stunden>0 then 'Stunden offen' when pflichtstunden_soll>0 then 'Stunden fertig' else 'Befreit' end,regelgrund,'Zusammenfassung','',null::numeric,null::date,'',0 from filtered order by nachname,vorname;
$$;
GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean, text) TO authenticated;

INSERT INTO public.app_export_column_definition (export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte)
VALUES ('arbeitsstunden_uebersicht','wartungsvertrag_gutschrift_stunden','WV-Gutschrift','Wartungsvertrag-Gutschrift',55,true,false)
ON CONFLICT (export_key,column_key) DO UPDATE SET label_kurz=excluded.label_kurz,label_lang=excluded.label_lang,sortierung=excluded.sortierung,standard_sichtbar=excluded.standard_sichtbar,ist_sortierspalte=excluded.ist_sortierspalte;
