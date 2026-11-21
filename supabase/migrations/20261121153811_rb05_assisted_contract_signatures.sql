-- RB-05 Demo: Signaturen am gemeinsamen Endgerät durch beauftragte Vertragsbearbeitung.
-- Reuses an already verified, assigned operator with effective permission 524288.
-- Does NOT grant permissions and does NOT modify production records.
-- The real signer is preserved as mitglied_id/name; the logged-in operator is
-- preserved as unterzeichnet_von_auth_user_id.
DO $rb05_patch$
DECLARE
  v_definition text;
  v_updated text;
  v_old text := $old_guard$if not found then raise exception 'Die Signaturposition wurde nicht vorbereitet.' using errcode='P0002'; end if; if not exists (select 1 from public.app_user au where au.user_id=auth.uid() and au.mitglied_id=p_mitglied_id) and not (v_position.unterzeichner_typ='gesetzlicher_vertreter' and v_position.mitglied_id is null and p_mitglied_id is null and nullif(btrim(v_position.unterzeichner_name_snapshot),'') is not null and nullif(btrim(p_unterzeichner_name_snapshot),'')=nullif(btrim(v_position.unterzeichner_name_snapshot),'') and public.has_effective_permission(524288)) then raise exception 'Die Signatur darf nur durch die zugeordnete Person abgegeben werden.' using errcode='42501'; end if;$old_guard$;
  v_new text := $new_guard$if not found then
   raise exception 'Die Signaturposition wurde nicht vorbereitet.' using errcode='P0002';
 end if;
 -- The signer physically signs on the operator's screen. The operator and
 -- the signer's member identity are distinct and remain separately recorded.
 -- Never allow changing a fixed private signer when collecting on site.
 if v_position.unterzeichner_typ <> 'vereinsunterzeichner'
    and v_position.mitglied_id is distinct from p_mitglied_id then
   raise exception 'Die unterschreibende Person entspricht nicht der vorgesehenen Vertragspartei.' using errcode='42501';
 end if;
 -- The session may belong either to the actual signer, or to the currently
 -- assigned, effectively authorized contract operator. A role label alone
 -- is insufficient: individual permission revocations must take effect.
 if not exists (
     select 1 from public.app_user au
     where au.user_id = auth.uid() and au.mitglied_id = p_mitglied_id
   ) and not (
     public.has_effective_permission(524288)
     and exists (
       select 1 from public.kommunikationsvorgang v
       where v.id = p_kommunikationsvorgang_id
         and v.zugewiesen_an = auth.uid()
         and v.status = 'zur_signatur'
         and v.signatur_revision = p_signatur_revision
     )
   ) then
   raise exception 'Nur die unterschreibende Person oder der zugewiesene berechtigte Vertragsbearbeiter darf eine Signatur erfassen.' using errcode='42501';
 end if;
 -- A non-member representative has no app account. Preserve the frozen
 -- representative name and prevent swapping it during assisted signing.
 if v_position.unterzeichner_typ = 'gesetzlicher_vertreter'
    and p_mitglied_id is null
    and (
      nullif(btrim(v_position.unterzeichner_name_snapshot),'') is null
      or nullif(btrim(p_unterzeichner_name_snapshot),'') is distinct from
         nullif(btrim(v_position.unterzeichner_name_snapshot),'')
    ) then
    raise exception 'Der gesetzliche Vertreter stimmt nicht mit dem Signaturplan überein.' using errcode='42501';
 end if;$new_guard$;
BEGIN
  SELECT pg_get_functiondef(p.oid) INTO v_definition
  FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
  WHERE n.nspname='public'
    AND p.proname='speichere_prepared_contract_signatur'
    AND pg_get_function_identity_arguments(p.oid) LIKE 'p_kommunikationsvorgang_id bigint,%';
  IF v_definition IS NULL THEN
    RAISE EXCEPTION 'RB05 assisted signature function not found';
  END IF;
  IF length(v_definition) - length(replace(v_definition, v_old, '')) <> length(v_old) THEN
    RAISE EXCEPTION 'RB05 assisted signature expected source not unique / changed';
  END IF;
  v_updated := replace(v_definition,v_old,v_new);
  EXECUTE v_updated;
  IF NOT EXISTS (
    SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
    WHERE n.nspname='public'
      AND p.proname='speichere_prepared_contract_signatur'
      AND position('zugewiesene berechtigte Vertragsbearbeiter' in pg_get_functiondef(p.oid))>0
  ) THEN
    RAISE EXCEPTION 'RB05 assisted signature patch verification failed';
  END IF;
END;
$rb05_patch$;
