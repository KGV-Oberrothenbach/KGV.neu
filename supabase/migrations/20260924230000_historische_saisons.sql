-- Historische Saisons für die nachträgliche Erfassung langjähriger Pachtverhältnisse.
-- Die Finanzwerte sind bewusst eine Kopie der Saison 2025 und müssen vor einer
-- abrechnungsrelevanten Verwendung fachlich geprüft bzw. korrigiert werden.

insert into public.saison
    (id, jahr, pflichtstunden_soll, euro_pro_fehlstunde, pacht_pro_qm, mitgliedsbeitrag,
     mitgliedsbeitrag_nebenmitglied, aufnahmegebuehr, gebuehr_bauantrag, bemerkung, is_demo)
select jahre.jahr,
       jahre.jahr,
       vorlage.pflichtstunden_soll,
       vorlage.euro_pro_fehlstunde,
       vorlage.pacht_pro_qm,
       vorlage.mitgliedsbeitrag,
       vorlage.mitgliedsbeitrag_nebenmitglied,
       vorlage.aufnahmegebuehr,
       vorlage.gebuehr_bauantrag,
       'Historische Saison automatisch ergänzt. Finanz-, Beitrags- und Pflichtstundenwerte entsprechen der Vorlage 2025 und müssen vor abrechnungsrelevanter Verwendung geprüft werden.',
       vorlage.is_demo
from generate_series(1976, 2024) as jahre(jahr)
cross join (
    select pflichtstunden_soll, euro_pro_fehlstunde, pacht_pro_qm, mitgliedsbeitrag,
           mitgliedsbeitrag_nebenmitglied, aufnahmegebuehr, gebuehr_bauantrag, is_demo
    from public.saison
    where jahr = 2025
    order by id
    limit 1
) as vorlage
where not exists (
    select 1
    from public.saison vorhanden
    where vorhanden.id = jahre.jahr
       or vorhanden.jahr = jahre.jahr
);

-- Erwartete Kontrolle: 1976 bis 2026, insgesamt 51 Saisons.
