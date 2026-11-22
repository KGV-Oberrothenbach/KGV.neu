-- FR2-04: Lost-response-Retry beim Signaturstart bereits vor der
-- clientseitigen Anlage der Signaturpositionen sicher erlauben.
-- Ziel: nur vorhandene Funktion; keine Tabellen-/Policy-/Datenänderung.
-- Die vorhandenen Rechte- und Snapshot-Prüfungen bleiben unverändert.
DO $fr204_retry$
DECLARE
  v_function regprocedure := 'public.start_prepared_contract_signature(bigint,integer,uuid)'::regprocedure;
  v_definition text;
  v_old text := $old$
     and exists (
       select 1 from public.prepared_contract_signatur p
       where p.kommunikationsvorgang_id = v.id
         and p.signatur_revision = p_expected_revision
     )$old$;
  v_new text := $new$
     and (
       v.status = 'zur_signatur'
       or exists (
         select 1 from public.prepared_contract_signatur p
         where p.kommunikationsvorgang_id = v.id
           and p.signatur_revision = p_expected_revision
       )
     )$new$;
BEGIN
  v_definition := pg_get_functiondef(v_function);
  IF v_definition IS NULL
     OR length(v_definition) - length(replace(v_definition, v_old, '')) <> length(v_old)
     OR position(v_new IN v_definition) > 0 THEN
    RAISE EXCEPTION 'FR2-04: unbekannter RPC-Ausgangsstand; Funktion unverändert.'
      USING ERRCODE = '23514';
  END IF;
  EXECUTE replace(v_definition, v_old, v_new);
  IF position(v_new IN pg_get_functiondef(v_function)) = 0 THEN
    RAISE EXCEPTION 'FR2-04: Signaturstart-Retry-Nachkontrolle fehlgeschlagen.'
      USING ERRCODE = '23514';
  END IF;
END
$fr204_retry$;
