CREATE OR REPLACE FUNCTION public.validate_wartungsvertrag_zuordnung()
RETURNS trigger
LANGUAGE plpgsql
AS $$
declare
    v_mitglied_exists boolean;
    v_max_aktive_zuordnungen integer;
    v_spitzenbelegung integer;
begin
    select true
      into v_mitglied_exists
    from public.mitglied m
    where m.id = new.hauptmitglied_id;

    if v_mitglied_exists is distinct from true then
        raise exception 'Mitglied % existiert nicht.', new.hauptmitglied_id;
    end if;

    select w.max_aktive_zuordnungen
      into v_max_aktive_zuordnungen
    from public.wartungsvertraege w
    where w.id = new.wartungsvertrag_id;

    if v_max_aktive_zuordnungen is null then
        raise exception 'Wartungsvertrag % existiert nicht.', new.wartungsvertrag_id;
    end if;

    if exists (
        select 1
        from public.wartungsvertrag_zuordnungen z
        where z.wartungsvertrag_id = new.wartungsvertrag_id
          and z.hauptmitglied_id = new.hauptmitglied_id
          and z.id <> coalesce(new.id, -1)
          and daterange(
                z.gueltig_ab,
                coalesce(z.gueltig_bis, 'infinity'::date),
                '[]'
              )
              &&
              daterange(
                new.gueltig_ab,
                coalesce(new.gueltig_bis, 'infinity'::date),
                '[]'
              )
    ) then
        raise exception
            'Das Mitglied % hat den Wartungsvertrag % im angegebenen Zeitraum bereits zugeordnet.',
            new.hauptmitglied_id,
            new.wartungsvertrag_id;
    end if;

    with existing_rows as (
        select z.gueltig_ab, z.gueltig_bis
        from public.wartungsvertrag_zuordnungen z
        where z.wartungsvertrag_id = new.wartungsvertrag_id
          and z.id <> coalesce(new.id, -1)
          and daterange(
                z.gueltig_ab,
                coalesce(z.gueltig_bis, 'infinity'::date),
                '[]'
              )
              &&
              daterange(
                new.gueltig_ab,
                coalesce(new.gueltig_bis, 'infinity'::date),
                '[]'
              )
    ),
    candidate_dates as (
        select new.gueltig_ab as d
        union
        select new.gueltig_bis
        where new.gueltig_bis is not null
        union
        select e.gueltig_ab
        from existing_rows e
        where e.gueltig_ab between new.gueltig_ab and coalesce(new.gueltig_bis, 'infinity'::date)
        union
        select e.gueltig_bis
        from existing_rows e
        where e.gueltig_bis is not null
          and e.gueltig_bis between new.gueltig_ab and coalesce(new.gueltig_bis, 'infinity'::date)
    )
    select coalesce(
               max(
                   (
                       select count(*)
                       from public.wartungsvertrag_zuordnungen z
                       where z.wartungsvertrag_id = new.wartungsvertrag_id
                         and z.id <> coalesce(new.id, -1)
                         and c.d between z.gueltig_ab and coalesce(z.gueltig_bis, 'infinity'::date)
                   ) + 1
               ),
               1
           )
      into v_spitzenbelegung
    from candidate_dates c;

    if v_spitzenbelegung > v_max_aktive_zuordnungen then
        raise exception
            'Maximale Anzahl aktiver Zuordnungen (% ) für Wartungsvertrag % würde überschritten.',
            v_max_aktive_zuordnungen,
            new.wartungsvertrag_id;
    end if;

    return new;
end;
$$;

CREATE OR REPLACE FUNCTION public.fn_berechne_pflichtstunden_status(p_mitglied_id bigint, p_saison_id bigint)
RETURNS TABLE(hauptmitglied_id bigint, saison_id bigint, saison_jahr integer, regelgrund text, ist_befreit boolean, hat_wartungsvertrag boolean, altersbefreit boolean, eintritt_im_saisonjahr boolean, eintritt_zweites_halbjahr boolean, pflichtstunden_soll numeric, geleistete_stunden numeric, offene_stunden numeric, euro_pro_fehlstunde numeric, fehlbetrag numeric)
LANGUAGE plpgsql
STABLE
AS $$
declare
    v_hauptmitglied_id bigint;
    v_mitglied record;
    v_saison record;
    v_saison_start date;
    v_saison_ende date;
    v_regelgrund text := 'standard';
    v_ist_befreit boolean := false;
    v_hat_wartungsvertrag boolean := false;
    v_altersbefreit boolean := false;
    v_eintritt_im_saisonjahr boolean := false;
    v_eintritt_zweites_halbjahr boolean := false;
    v_pflichtstunden_soll numeric := 0;
    v_geleistete_stunden numeric := 0;
begin
    v_hauptmitglied_id := public.get_hauptmitglied_id(p_mitglied_id);

    if v_hauptmitglied_id is null then
        raise exception 'Mitglied % nicht gefunden.', p_mitglied_id;
    end if;

    select *
      into v_mitglied
    from public.mitglied
    where id = p_mitglied_id;

    select *
      into v_saison
    from public.saison
    where id = p_saison_id;

    if not found then
        raise exception 'Saison % nicht gefunden.', p_saison_id;
    end if;

    if v_saison.jahr is null then
        raise exception 'Saison % hat kein Jahr gesetzt.', p_saison_id;
    end if;

    v_saison_start := make_date(v_saison.jahr, 1, 1);
    v_saison_ende  := make_date(v_saison.jahr, 12, 31);

    v_pflichtstunden_soll := coalesce(v_saison.pflichtstunden_soll, 0);

    v_eintritt_im_saisonjahr :=
        extract(year from v_mitglied.mitglied_seit)::int = v_saison.jahr;

    v_eintritt_zweites_halbjahr :=
        v_eintritt_im_saisonjahr
        and v_mitglied.mitglied_seit >= make_date(v_saison.jahr, 7, 1);

    if v_mitglied.mitglied_seit > v_saison_ende
       or (v_mitglied.mitglied_ende is not null and v_mitglied.mitglied_ende < v_saison_start)
    then
        v_pflichtstunden_soll := 0;
        v_ist_befreit := true;
        v_regelgrund := 'keine_aktive_mitgliedschaft';
    else
        if v_mitglied.geburtsdatum is not null then
            v_altersbefreit :=
                case v_mitglied.arbeitsstunden_altersregel_typ
                    when 'frau75' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 75
                    when 'mann80' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 80
                    else false
                end;
        else
            v_altersbefreit := false;
        end if;

        select exists (
            select 1
            from public.wartungsvertrag_zuordnungen z
            join public.wartungsvertraege w
              on w.id = z.wartungsvertrag_id
            where z.hauptmitglied_id = p_mitglied_id
              and w.aktiv = true
              and w.befreit_von_pflichtstunden = true
              and z.gueltig_ab <= v_saison_ende
              and (z.gueltig_bis is null or z.gueltig_bis >= v_saison_start)
        )
        into v_hat_wartungsvertrag;

        if v_hat_wartungsvertrag then
            v_pflichtstunden_soll := 0;
            v_ist_befreit := true;
            v_regelgrund := 'wartungsvertrag';
        elsif v_altersbefreit then
            v_pflichtstunden_soll := 0;
            v_ist_befreit := true;
            v_regelgrund := 'altersbefreiung';
        elsif v_eintritt_zweites_halbjahr then
            v_pflichtstunden_soll := round(v_pflichtstunden_soll / 2.0, 2);
            v_regelgrund := 'eintritt_2_halbjahr';
        else
            v_regelgrund := 'standard';
        end if;
    end if;

    select coalesce(sum(a.stunden), 0)
      into v_geleistete_stunden
    from public.arbeitsstunde a
    join public.mitglied m
      on m.id = a.mitglied_id
    where a.saison_id = p_saison_id
      and a.freigegeben = true
      and coalesce(m.hauptmitglied_id, m.id) = v_hauptmitglied_id;

    return query
    select
        v_hauptmitglied_id,
        p_saison_id,
        v_saison.jahr,
        v_regelgrund,
        v_ist_befreit,
        v_hat_wartungsvertrag,
        v_altersbefreit,
        v_eintritt_im_saisonjahr,
        v_eintritt_zweites_halbjahr,
        v_pflichtstunden_soll,
        v_geleistete_stunden,
        greatest(v_pflichtstunden_soll - v_geleistete_stunden, 0),
        coalesce(v_saison.euro_pro_fehlstunde, 0),
        greatest(v_pflichtstunden_soll - v_geleistete_stunden, 0) * coalesce(v_saison.euro_pro_fehlstunde, 0);
end;
$$;

CREATE OR REPLACE VIEW public.v_pflichtstunden_uebersicht AS
 SELECT s.id AS saison_id,
    s.jahr AS jahr,
    s.jahr AS saison_jahr,
    m.id AS mitglied_id,
    coalesce(m.hauptmitglied_id, m.id) AS hauptmitglied_id,
    m.name,
    m.vorname,
    x.regelgrund,
    x.ist_befreit,
    x.hat_wartungsvertrag,
    x.altersbefreit,
    x.eintritt_im_saisonjahr,
    x.eintritt_zweites_halbjahr,
    x.pflichtstunden_soll,
    x.geleistete_stunden,
    x.offene_stunden,
    x.euro_pro_fehlstunde,
    x.fehlbetrag
   FROM (public.saison s
     CROSS JOIN public.mitglied m
     CROSS JOIN LATERAL public.fn_berechne_pflichtstunden_status(m.id, s.id::bigint) x(hauptmitglied_id, saison_id, saison_jahr, regelgrund, ist_befreit, hat_wartungsvertrag, altersbefreit, eintritt_im_saisonjahr, eintritt_zweites_halbjahr, pflichtstunden_soll, geleistete_stunden, offene_stunden, euro_pro_fehlstunde, fehlbetrag));
