-- AP-01.2 / Entwurf A – Fachnummern: Quellbezug, Spaltenrechte, Views.
-- GitHub-Ausgangscommit: 94e125c1bcd86992fec084e1f084e04e4de8db0a
-- NUR ENTWURF. Nicht ausgeführt; zuerst SQL und Demo-Funktionstests freigeben.
-- Kein Ändern existierender Reservierungen, Schlüssel, Sequenzen oder Trigger.
BEGIN;

-- Fail closed if a different policy setup has appeared since the read-only audit.
DO $preflight$
BEGIN
  IF to_regprocedure('public.current_mitglied_id()') IS NULL
     OR to_regprocedure('public.has_effective_permission(bigint)') IS NULL THEN
    RAISE EXCEPTION 'AP-01.2: Identitäts-/Rechtefunktionen fehlen.';
  END IF;
  IF EXISTS (
    SELECT 1 FROM pg_policies
    WHERE schemaname='public' AND tablename='fachnummer_reservierung'
  ) THEN
    RAISE EXCEPTION 'AP-01.2: Unerwartete bestehende RLS-Policy. Vorher manuell prüfen.';
  END IF;
END;
$preflight$;

-- Dedicated predicate returns only a yes/no decision; it never returns a
-- number, audit identity, member row, document or draft. SECURITY DEFINER is
-- needed to check source ownership without granting clients direct access to
-- sensitive source tables. A real auth.uid() is mandatory on every path.
CREATE FUNCTION public.ap012_can_read_fachnummer_reservierung(
    p_art text,
    p_quelle_art text,
    p_quelle_id bigint
)
RETURNS boolean
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, public, pg_temp
AS $function$
DECLARE
  v_mitglied_id bigint;
BEGIN
  IF auth.uid() IS NULL THEN
    RETURN false;
  END IF;

  -- Effective (not role-name-based) permission: honours personal revocations.
  IF public.has_effective_permission(4::bigint) THEN
    RETURN true;
  END IF;

  v_mitglied_id := public.current_mitglied_id();
  IF v_mitglied_id IS NULL THEN
    RETURN false;
  END IF;

  IF p_quelle_art = 'kommunikationsvorgang'
     AND p_art IN ('mitgliedsantrag','pachtvertrag','wartungsvertrag') THEN
    RETURN EXISTS (
      SELECT 1
      FROM public.kommunikationsvorgang kv
      WHERE kv.id = p_quelle_id AND (
        kv.mitglied_id = v_mitglied_id
        OR (
          p_art = 'pachtvertrag' AND EXISTS (
            -- Second tenants: only if they really belong to this very lease
            -- relationship, not merely to the same parcel at another time.
            SELECT 1
            FROM public.dokument d
            JOIN public.pachtvertragsfassung pf ON pf.dokument_id = d.id
            JOIN public.pachtverhaeltnis_beteiligung b
              ON b.pachtverhaeltnis_id = pf.pachtverhaeltnis_id
            WHERE d.source_kommunikationsvorgang_id = kv.id
              AND b.mitglied_id = v_mitglied_id
              -- Do not show a later replacement to a former tenant merely
              -- because they were linked to this lease relationship once.
              AND b.gueltig_ab <= COALESCE(pf.wirksam_ab, pf.vertragsdatum, pf.created_at::date)
              AND (b.gueltig_bis IS NULL OR b.gueltig_bis >=
                   COALESCE(pf.wirksam_ab, pf.vertragsdatum, pf.created_at::date))
          )
        )
      )
    );
  ELSIF p_quelle_art = 'wartungsvertrag'
        AND p_art = 'wartungsvertrag' THEN
    RETURN EXISTS (
      SELECT 1 FROM public.wartungsvertrag_zuordnungen z
      WHERE z.wartungsvertrag_id = p_quelle_id
        AND (z.hauptmitglied_id = v_mitglied_id
             OR z.vertragspartei_mitglied_id = v_mitglied_id)
    );
  ELSIF p_quelle_art = 'historisches_dokument'
        AND p_art = 'historischer_kombivertrag' THEN
    RETURN EXISTS (
      SELECT 1 FROM public.dokument d
      WHERE d.id = p_quelle_id AND d.mitglied_id = v_mitglied_id
    );
  END IF;

  RETURN false;
END;
$function$;

-- A newly created SECURITY DEFINER routine otherwise inherits PUBLIC EXECUTE.
REVOKE ALL PRIVILEGES ON FUNCTION
  public.ap012_can_read_fachnummer_reservierung(text,text,bigint)
FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION
  public.ap012_can_read_fachnummer_reservierung(text,text,bigint)
TO authenticated, service_role;

ALTER TABLE public.fachnummer_reservierung ENABLE ROW LEVEL SECURITY;

CREATE POLICY fachnummer_reservierung_select_scoped
ON public.fachnummer_reservierung
FOR SELECT TO authenticated
USING (
  public.ap012_can_read_fachnummer_reservierung(art,quelle_art,quelle_id)
);

-- PostgREST clients should only read the number and its source key; never
-- the staff UUID 'reserviert_von' or audit timestamp 'reserviert_am'.
REVOKE ALL PRIVILEGES ON TABLE public.fachnummer_reservierung FROM PUBLIC, anon;
REVOKE SELECT ON TABLE public.fachnummer_reservierung FROM authenticated;
GRANT SELECT (nummer, art, quelle_art, quelle_id)
  ON TABLE public.fachnummer_reservierung TO authenticated;

-- These existing SECURITY INVOKER views use exactly the four columns above.
-- Preserve their SELECT access for authorised web/MAUI users; eliminate
-- unnecessary anonymous grants and DML grants on the views themselves.
REVOKE ALL PRIVILEGES ON TABLE
  public.v_wartungsvertrag_fachnummer,
  public.v_pachtvertragsfassung_uebersicht
FROM anon;
REVOKE ALL PRIVILEGES ON TABLE
  public.v_wartungsvertrag_fachnummer,
  public.v_pachtvertragsfassung_uebersicht
FROM authenticated;
GRANT SELECT ON TABLE
  public.v_wartungsvertrag_fachnummer,
  public.v_pachtvertragsfassung_uebersicht
TO authenticated;

COMMIT;
