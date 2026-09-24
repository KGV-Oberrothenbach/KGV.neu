-- Ausschließlich für die DemoDB.
-- Bildet die Fachlogik der Originaldatenbank nach:
-- Vorstand ist ein befreiender Wartungsvertrag, der Verein selbst nicht.

do $$
declare
    v_vorstand_vertrag_id bigint;
    v_d01_mitglied_id bigint;
    v_d02_mitglied_id bigint;
begin
    select b.mitglied_id
      into v_d01_mitglied_id
    from public.parzellen_belegung b
    join public.parzelle p on p.id = b.parzelle_id
    where p.garten_nr = 'D01'
      and (b.bis_datum is null or b.bis_datum >= current_date)
    order by b.von_datum desc nulls last, b.id desc
    limit 1;

    select b.mitglied_id
      into v_d02_mitglied_id
    from public.parzellen_belegung b
    join public.parzelle p on p.id = b.parzelle_id
    where p.garten_nr = 'D02'
      and (b.bis_datum is null or b.bis_datum >= current_date)
    order by b.von_datum desc nulls last, b.id desc
    limit 1;

    if v_d01_mitglied_id is null or v_d02_mitglied_id is null then
        raise exception 'Demo-Testdaten benötigen die aktiven Belegungen D01 und D02.';
    end if;

    select id
      into v_vorstand_vertrag_id
    from public.wartungsvertraege
    where titel = 'Vorstand'
    order by id
    limit 1;

    if v_vorstand_vertrag_id is null then
        insert into public.wartungsvertraege
            (titel, beschreibung, bereich, max_aktive_zuordnungen, befreit_von_pflichtstunden, aktiv, bemerkung, is_demo)
        values
            ('Vorstand', 'Befreiung von Pflichtstunden für Vorstandsmitglieder.', 'Vorstand/Bauausschuß', 7, true, true,
             'Demo-Abbild der Vorstand-Regel aus der Originaldatenbank.', true)
        returning id into v_vorstand_vertrag_id;
    else
        update public.wartungsvertraege
        set befreit_von_pflichtstunden = true,
            aktiv = true,
            is_demo = true
        where id = v_vorstand_vertrag_id;
    end if;

    -- D01: Andreas Bräuer, Admin und Vorstand; daher keine Pflichtstunden.
    update public.mitglied
    set role = 'admin',
        aktiv = true,
        ist_kgv = false
    where id = v_d01_mitglied_id;

    insert into public.wartungsvertrag_zuordnungen
        (wartungsvertrag_id, hauptmitglied_id, gueltig_ab, gueltig_bis, bemerkung, is_demo)
    select v_vorstand_vertrag_id, v_d01_mitglied_id, date '2026-01-01', null,
           'Demo: Vorstand/Admin auf D01 – von Pflichtstunden befreit.', true
    where not exists (
        select 1
        from public.wartungsvertrag_zuordnungen z
        where z.wartungsvertrag_id = v_vorstand_vertrag_id
          and z.hauptmitglied_id = v_d01_mitglied_id
          and z.gueltig_bis is null
    );

    -- D02: fiktives KGV-Mitglied für Leerstand und allgemeine Flächen.
    -- Es erhält bewusst keinen Wartungsvertrag – genau wie das Vereinsmitglied im Original.
    update public.mitglied
    set name = 'Kleingartenverein Oberrothenbach e.V.',
        vorname = '.',
        role = 'user',
        aktiv = true,
        ist_kgv = true,
        bemerkung = 'Demo: Verein für Leerstands- und Allgemeinflächen-Berechnungen.'
    where id = v_d02_mitglied_id;
end;
$$;

-- Kontrolle: D01 erscheint als Vorstand/Wartungsvertrag, D02 als KGV-Mitglied ohne Vertrag.
select p.garten_nr,
       m.vorname,
       m.name,
       m.role,
       m.ist_kgv,
       w.titel as wartungsvertrag,
       w.befreit_von_pflichtstunden
from public.parzelle p
join public.parzellen_belegung b on b.parzelle_id = p.id
join public.mitglied m on m.id = b.mitglied_id
left join public.wartungsvertrag_zuordnungen z
       on z.hauptmitglied_id = coalesce(m.hauptmitglied_id, m.id)
      and z.gueltig_ab <= current_date
      and (z.gueltig_bis is null or z.gueltig_bis >= current_date)
left join public.wartungsvertraege w on w.id = z.wartungsvertrag_id
where p.garten_nr in ('D01', 'D02')
  and (b.bis_datum is null or b.bis_datum >= current_date)
order by p.garten_nr, w.titel;
