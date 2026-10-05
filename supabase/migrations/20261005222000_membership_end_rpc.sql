CREATE FUNCTION public.end_membership(
    p_main_member_id bigint,
    p_secondary_decision text DEFAULT NULL
)
RETURNS TABLE (
    success boolean,
    message text,
    updated_main_member jsonb,
    updated_secondary_member jsonb,
    secondary_decision text
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path TO 'public', 'pg_temp'
AS $$
DECLARE
    v_user_id uuid := auth.uid();
    v_main_member public.mitglied%ROWTYPE;
    v_secondary_member public.mitglied%ROWTYPE;
    v_has_secondary boolean := false;
    v_is_productive_admin boolean := false;
    v_is_restricted_demo_admin boolean := false;
    v_end_date date := (public.kgv_local_now())::date;
    v_now_utc timestamp without time zone := now() AT TIME ZONE 'UTC';
    v_message text;
BEGIN
    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Anmeldung erforderlich.' USING ERRCODE = '42501';
    END IF;

    v_is_productive_admin := public.is_productive_admin_or_vorstand();
    v_is_restricted_demo_admin := public.is_restricted_demo_admin_or_vorstand();
    IF NOT v_is_productive_admin AND NOT v_is_restricted_demo_admin THEN
        RAISE EXCEPTION 'Keine Berechtigung zum Beenden von Mitgliedschaften.' USING ERRCODE = '42501';
    END IF;

    SELECT m.*
      INTO v_main_member
      FROM public.mitglied AS m
     WHERE m.id = p_main_member_id
     FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Hauptmitglied wurde nicht gefunden.';
    END IF;

    IF v_is_restricted_demo_admin AND NOT public.is_demo_mitglied_id(v_main_member.id) THEN
        RAISE EXCEPTION 'Keine Berechtigung für dieses Mitglied.' USING ERRCODE = '42501';
    END IF;

    IF v_main_member.hauptmitglied_id IS NOT NULL THEN
        RAISE EXCEPTION 'Der Folgeentscheid ist nur für Hauptmitglieder verfügbar.';
    END IF;

    IF v_main_member.mitglied_ende IS NOT NULL THEN
        RAISE EXCEPTION 'Die Mitgliedschaft ist bereits beendet.';
    END IF;

    IF v_main_member.lockedbyuserid IS DISTINCT FROM v_user_id
       OR v_main_member.lockat IS NULL
       OR v_main_member.lockat <= v_now_utc - INTERVAL '10 minutes' THEN
        RAISE EXCEPTION 'Kein gültiger Lock auf dem Hauptmitglied.';
    END IF;

    SELECT m.*
      INTO v_secondary_member
      FROM public.mitglied AS m
     WHERE m.hauptmitglied_id = p_main_member_id
     ORDER BY m.id
     LIMIT 1
     FOR UPDATE;
    v_has_secondary := FOUND;

    IF NOT v_has_secondary THEN
        IF p_secondary_decision IS NOT NULL THEN
            RAISE EXCEPTION 'Ohne Nebenmitglied ist keine Folgeentscheidung zulässig.';
        END IF;
    ELSIF p_secondary_decision IS NULL
       OR p_secondary_decision NOT IN ('end_secondary', 'promote_secondary') THEN
        RAISE EXCEPTION 'Für das vorhandene Nebenmitglied ist eine gültige Folgeentscheidung erforderlich.';
    END IF;

    IF v_has_secondary AND v_is_restricted_demo_admin
       AND NOT public.is_demo_mitglied_id(v_secondary_member.id) THEN
        RAISE EXCEPTION 'Keine Berechtigung für das Nebenmitglied.' USING ERRCODE = '42501';
    END IF;

    IF v_has_secondary
       AND v_secondary_member.lockedbyuserid IS NOT NULL
       AND v_secondary_member.lockedbyuserid <> v_user_id
       AND (
           v_secondary_member.lockat IS NULL
           OR v_secondary_member.lockat + INTERVAL '10 minutes' > v_now_utc
       ) THEN
        RAISE EXCEPTION 'Das Nebenmitglied ist aktuell gesperrt.';
    END IF;

    IF v_has_secondary THEN
        IF p_secondary_decision = 'end_secondary' THEN
            UPDATE public.mitglied AS m
               SET mitglied_ende = v_end_date,
                   aktiv = false
             WHERE m.id = v_secondary_member.id
            RETURNING m.* INTO v_secondary_member;

            v_message := 'Haupt- und Nebenmitglied wurden beendet.';
        ELSE
            UPDATE public.mitglied AS m
               SET hauptmitglied_id = NULL,
                   mitglied_ende = NULL,
                   aktiv = true
             WHERE m.id = v_secondary_member.id
            RETURNING m.* INTO v_secondary_member;

            v_message := 'Hauptmitglied wurde beendet und das Nebenmitglied zum Hauptmitglied gemacht.';
        END IF;
    ELSE
        v_message := 'Mitgliedschaft wurde beendet.';
    END IF;

    UPDATE public.mitglied AS m
       SET mitglied_ende = v_end_date,
           aktiv = false
     WHERE m.id = v_main_member.id
    RETURNING m.* INTO v_main_member;

    RETURN QUERY
    SELECT
        true,
        v_message,
        to_jsonb(v_main_member),
        CASE WHEN v_has_secondary THEN to_jsonb(v_secondary_member) ELSE NULL::jsonb END,
        p_secondary_decision;
END;
$$;

REVOKE ALL ON FUNCTION public.end_membership(bigint, text) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.end_membership(bigint, text) TO authenticated;
