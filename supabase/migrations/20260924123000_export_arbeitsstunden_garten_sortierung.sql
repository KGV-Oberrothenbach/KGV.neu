-- Arbeitsstunden-PDF: Gartennummer für die fachliche Sortierung bereitstellen.
DROP FUNCTION IF EXISTS public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean);

CREATE FUNCTION public.rpc_export_arbeitsstunden_uebersicht(
    p_jahr integer DEFAULT NULL,
    p_stunden_offen boolean DEFAULT false,
    p_stunden_fertig boolean DEFAULT false,
    p_wartungsvertraege boolean DEFAULT false
)
RETURNS TABLE (
    mitglied_id bigint,
    garten_nr text,
    nachname text,
    vorname text,
    jahr integer,
    pflichtstunden_soll numeric,
    geleistete_stunden numeric,
    offene_stunden numeric,
    hat_wartungsvertrag boolean,
    wartungsvertraege text,
    status text,
    regelgrund text
)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
  WITH basis AS (
    SELECT DISTINCT ON (v.hauptmitglied_id, v.saison_id)
      v.hauptmitglied_id AS mitglied_id,
      m.name AS nachname,
      m.vorname,
      v.saison_jahr AS jahr,
      coalesce(v.pflichtstunden_soll, 0) AS pflichtstunden_soll,
      coalesce(v.geleistete_stunden, 0) AS geleistete_stunden,
      coalesce(v.offene_stunden, 0) AS offene_stunden,
      coalesce(v.hat_wartungsvertrag, false) AS hat_wartungsvertrag,
      coalesce(v.regelgrund, '') AS regelgrund
    FROM public.v_pflichtstunden_uebersicht v
    JOIN public.mitglied m ON m.id = v.hauptmitglied_id
    WHERE m.aktiv = true
      AND m.is_demo = false
      AND (p_jahr IS NULL OR v.saison_jahr = p_jahr)
    ORDER BY v.hauptmitglied_id, v.saison_id DESC
  ), gaerten AS (
    SELECT
      b.mitglied_id,
      b.jahr,
      coalesce(string_agg(DISTINCT p.garten_nr, ', ' ORDER BY p.garten_nr), '') AS garten_nr
    FROM basis b
    LEFT JOIN public.parzellen_belegung pb
      ON pb.mitglied_id = b.mitglied_id
      AND pb.von_datum <= make_date(b.jahr, 12, 31)
      AND (pb.bis_datum IS NULL OR pb.bis_datum >= make_date(b.jahr, 1, 1))
    LEFT JOIN public.parzelle p ON p.id = pb.parzelle_id
    GROUP BY b.mitglied_id, b.jahr
  ), liste AS (
    SELECT b.*, g.garten_nr, coalesce(string_agg(w.titel, ', ' ORDER BY w.titel), '') AS wartungsvertraege
    FROM basis b
    LEFT JOIN gaerten g ON g.mitglied_id = b.mitglied_id AND g.jahr = b.jahr
    LEFT JOIN public.wartungsvertrag_zuordnungen z
      ON z.hauptmitglied_id = b.mitglied_id
      AND z.gueltig_ab <= make_date(b.jahr, 12, 31)
      AND (z.gueltig_bis IS NULL OR z.gueltig_bis >= make_date(b.jahr, 1, 1))
    LEFT JOIN public.wartungsvertraege w ON w.id = z.wartungsvertrag_id
    GROUP BY b.mitglied_id, b.nachname, b.vorname, b.jahr, b.pflichtstunden_soll,
      b.geleistete_stunden, b.offene_stunden, b.hat_wartungsvertrag, b.regelgrund, g.garten_nr
  )
  SELECT mitglied_id, garten_nr, nachname, vorname, jahr, pflichtstunden_soll, geleistete_stunden,
    offene_stunden, hat_wartungsvertrag, wartungsvertraege,
    CASE WHEN hat_wartungsvertrag THEN 'Wartungsvertrag - befreit'
         WHEN pflichtstunden_soll > 0 AND offene_stunden > 0 THEN 'Stunden offen'
         WHEN pflichtstunden_soll > 0 THEN 'Stunden fertig'
         ELSE 'Befreit' END,
    regelgrund
  FROM liste
  WHERE NOT (p_stunden_offen OR p_stunden_fertig OR p_wartungsvertraege)
     OR (p_stunden_offen AND NOT hat_wartungsvertrag AND offene_stunden > 0)
     OR (p_stunden_fertig AND NOT hat_wartungsvertrag AND pflichtstunden_soll > 0 AND offene_stunden <= 0)
     OR (p_wartungsvertraege AND hat_wartungsvertrag)
  ORDER BY garten_nr, nachname, vorname;
$$;

GRANT EXECUTE ON FUNCTION public.rpc_export_arbeitsstunden_uebersicht(integer, boolean, boolean, boolean) TO authenticated;

UPDATE public.app_export_definition
SET standard_sortierung = 'garten_nr'
WHERE export_key = 'arbeitsstunden_uebersicht';

DELETE FROM public.app_export_column_definition WHERE export_key = 'arbeitsstunden_uebersicht';
INSERT INTO public.app_export_column_definition
  (export_key, column_key, label_kurz, label_lang, sortierung, standard_sichtbar, ist_sortierspalte)
VALUES
  ('arbeitsstunden_uebersicht', 'garten_nr', 'Garten', 'Gartennummer', 10, true, true),
  ('arbeitsstunden_uebersicht', 'nachname', 'Name', 'Nachname', 20, true, true),
  ('arbeitsstunden_uebersicht', 'vorname', 'Vorname', 'Vorname', 30, true, false),
  ('arbeitsstunden_uebersicht', 'jahr', 'Saison', 'Saison', 40, true, false),
  ('arbeitsstunden_uebersicht', 'pflichtstunden_soll', 'Soll', 'Pflichtstunden Soll', 50, true, false),
  ('arbeitsstunden_uebersicht', 'geleistete_stunden', 'Geleistet', 'Geleistete Stunden', 60, true, false),
  ('arbeitsstunden_uebersicht', 'offene_stunden', 'Offen', 'Offene Stunden', 70, true, false),
  ('arbeitsstunden_uebersicht', 'status', 'Status', 'Status', 80, true, false),
  ('arbeitsstunden_uebersicht', 'wartungsvertraege', 'Wartungsvertrag', 'Wartungsverträge', 90, true, false),
  ('arbeitsstunden_uebersicht', 'regelgrund', 'Regelgrund', 'Befreiungsgrund', 100, false, false);
