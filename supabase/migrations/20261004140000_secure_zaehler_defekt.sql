CREATE OR REPLACE FUNCTION public.mark_meter_defective(p_zaehler_id bigint)
RETURNS boolean
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path TO 'public', 'pg_temp'
AS $$
DECLARE
	v_zaehler public.zaehler;
BEGIN
	SELECT z.*
	  INTO v_zaehler
	  FROM public.zaehler z
	 WHERE z.id = p_zaehler_id;

	IF NOT FOUND THEN
		RAISE EXCEPTION 'Zähler % wurde nicht gefunden.', p_zaehler_id;
	END IF;

	IF v_zaehler.status <> 'aktiv'::public.zaehler_status THEN
		RAISE EXCEPTION 'Zähler % ist nicht aktiv.', p_zaehler_id;
	END IF;

	IF v_zaehler.ausgebaut_am IS NOT NULL THEN
		RAISE EXCEPTION 'Zähler % ist bereits ausgebaut.', p_zaehler_id;
	END IF;

	IF NOT public.is_admin_or_vorstand()
	   AND NOT EXISTS (
			SELECT 1
			  FROM public.parzellen_belegung pb
			 WHERE pb.parzelle_id = v_zaehler.parzelle_id
			   AND pb.mitglied_id = public.current_mitglied_id()
			   AND pb.von_datum <= CURRENT_DATE
			   AND (pb.bis_datum IS NULL OR pb.bis_datum >= CURRENT_DATE)
	   ) THEN
		RAISE EXCEPTION 'Keine Berechtigung für Zähler %.', p_zaehler_id;
	END IF;

	UPDATE public.zaehler
	   SET defekt = true
	 WHERE id = p_zaehler_id;

	RETURN true;
END;
$$;

REVOKE ALL ON FUNCTION public.mark_meter_defective(bigint) FROM PUBLIC;
REVOKE ALL ON FUNCTION public.mark_meter_defective(bigint) FROM anon;
GRANT EXECUTE ON FUNCTION public.mark_meter_defective(bigint) TO authenticated;
