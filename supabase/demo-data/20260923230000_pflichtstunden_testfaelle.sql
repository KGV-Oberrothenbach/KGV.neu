-- Ausschließlich für die DemoDB.
-- Reproduzierbare Testfälle für die zentrale Pflichtstunden-Auswertung 2026.
-- Die E-Mail-Adressen dienen nur als stabile technische Kennung für ein erneutes Ausführen.

do $$
declare
    v_saison_id bigint;
    v_wartungsvertrag_id bigint;
    v_member_ids bigint[];
    v_wartung_member_id bigint;
begin
    select id into v_saison_id
    from public.saison
    where jahr = 2026;

    if v_saison_id is null then
        raise exception 'Demo-Testdaten benötigen die Saison 2026.';
    end if;

    -- Grenzwerte: Die aktuelle Fachlogik wertet nach Kalenderjahr aus.
    -- Daher ist 1951/1946 bereits befreit, 1952/1947 noch nicht.
    insert into public.mitglied
        (name, vorname, email, geburtsdatum, mitglied_seit, aktiv, role, arbeitsstunden_altersregel_typ, bemerkung)
    select 'Demo Pflichtstunden', x.vorname, x.email, x.geburtsdatum, date '2020-01-01', true, 'user', x.altersregel, x.bemerkung
    from (values
        ('Frau 75', 'demo.pflicht.frau75@invalid.example', date '1951-12-31', 'frau75', 'Testfall: Altersbefreiung Frau ab Kalenderjahr 75'),
        ('Frau 74', 'demo.pflicht.frau74@invalid.example', date '1952-01-01', 'frau75', 'Testfall: Frau vor Altersgrenze'),
        ('Mann 80', 'demo.pflicht.mann80@invalid.example', date '1946-12-31', 'mann80', 'Testfall: Altersbefreiung Mann ab Kalenderjahr 80'),
        ('Mann 79', 'demo.pflicht.mann79@invalid.example', date '1947-01-01', 'mann80', 'Testfall: Mann vor Altersgrenze'),
        ('Ohne Geburtsdatum', 'demo.pflicht.ohne-geburt@invalid.example', null::date, 'mann80', 'Testfall: Altersregel gesetzt, Geburtsdatum fehlt'),
        ('Wartungsvertrag', 'demo.pflicht.wartung@invalid.example', date '1980-06-15', 'keine', 'Testfall: Wartungsvertrag mit dennoch erfassten Stunden'),
        ('Stunden offen', 'demo.pflicht.offen@invalid.example', date '1980-06-15', 'keine', 'Testfall: 0 von 8 Stunden'),
        ('Stunden teilweise', 'demo.pflicht.teilweise@invalid.example', date '1980-06-15', 'keine', 'Testfall: teilweise erledigte Stunden'),
        ('Stunden fertig', 'demo.pflicht.fertig@invalid.example', date '1980-06-15', 'keine', 'Testfall: vollständig erledigte Stunden')
    ) as x(vorname, email, geburtsdatum, altersregel, bemerkung)
    where not exists (
        select 1
        from public.mitglied m
        where m.email = x.email
    );

    -- Die Tabelle besitzt keine eindeutige E-Mail-Constraint. Bestehende Demo-Testfälle
    -- werden deshalb bei jedem Lauf auf den definierten Zustand zurückgesetzt.
    update public.mitglied
    set name = 'Demo Pflichtstunden',
        aktiv = true,
        role = 'user',
        mitglied_seit = date '2020-01-01',
        hauptmitglied_id = null
    where email like 'demo.pflicht.%@invalid.example';

    select array_agg(id order by id)
      into v_member_ids
    from public.mitglied
    where email in (
        'demo.pflicht.frau75@invalid.example',
        'demo.pflicht.frau74@invalid.example',
        'demo.pflicht.mann80@invalid.example',
        'demo.pflicht.mann79@invalid.example',
        'demo.pflicht.ohne-geburt@invalid.example',
        'demo.pflicht.wartung@invalid.example',
        'demo.pflicht.offen@invalid.example',
        'demo.pflicht.teilweise@invalid.example',
        'demo.pflicht.fertig@invalid.example'
    );

    delete from public.arbeitsstunde
    where saison_id = v_saison_id
      and mitglied_id = any(v_member_ids)
      and art_der_arbeit = 'Demo-Pflichtstunden-Test';

    insert into public.arbeitsstunde
        (mitglied_id, saison_id, datum, stunden, art_der_arbeit, freigegeben, status)
    select m.id, v_saison_id, date '2026-05-10', x.stunden, 'Demo-Pflichtstunden-Test', true, 'genehmigt'
    from public.mitglied m
    join (values
        ('demo.pflicht.wartung@invalid.example', 3.00::numeric),
        ('demo.pflicht.teilweise@invalid.example', 3.50::numeric),
        ('demo.pflicht.fertig@invalid.example', 8.00::numeric)
    ) as x(email, stunden) on x.email = m.email;

    select id into v_wartung_member_id
    from public.mitglied
    where email = 'demo.pflicht.wartung@invalid.example';

    select id into v_wartungsvertrag_id
    from public.wartungsvertraege
    where titel = 'Demo: Pflichtstunden-Testvertrag';

    if v_wartungsvertrag_id is null then
        insert into public.wartungsvertraege
            (titel, beschreibung, bereich, max_aktive_zuordnungen, befreit_von_pflichtstunden, aktiv, bemerkung)
        values
            ('Demo: Pflichtstunden-Testvertrag', 'Ausschließlich für reproduzierbare Pflichtstunden-Tests.', 'Demo', 1, true, true, 'Mitglied leistet dennoch 3 Stunden.')
        returning id into v_wartungsvertrag_id;
    end if;

    delete from public.wartungsvertrag_zuordnungen
    where wartungsvertrag_id = v_wartungsvertrag_id
      and hauptmitglied_id = v_wartung_member_id;

    insert into public.wartungsvertrag_zuordnungen
        (wartungsvertrag_id, hauptmitglied_id, gueltig_ab, gueltig_bis, bemerkung)
    values
        (v_wartungsvertrag_id, v_wartung_member_id, date '2026-01-01', null, 'Demo-Testfall 2026');
end;
$$;

-- Erwartete Kontrolle nach dem Einspielen:
-- altersbefreiung: 2, wartungsvertrag: 1, standard: 6.
select regelgrund,
       count(*) as faelle,
       sum(pflichtstunden_soll) as sollstunden,
       sum(geleistete_stunden) as geleistete_stunden,
       sum(offene_stunden) as offene_stunden
from public.v_pflichtstunden_uebersicht
where saison_jahr = 2026
  and hauptmitglied_id in (
      select id
      from public.mitglied
      where email like 'demo.pflicht.%@invalid.example'
  )
group by regelgrund
order by regelgrund;
