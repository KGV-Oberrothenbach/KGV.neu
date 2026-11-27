-- FR2-RB04D4: check authorization before object-dependent errors and locks.
DO $migration$
DECLARE
  f regprocedure;
  def text;
  patched text;
  anchor text;
  replacement text;
  expected_before text;
  expected_after text;
BEGIN
  FOR f,expected_before,expected_after IN SELECT * FROM (VALUES
    ('public.mark_meter_defective(bigint)'::regprocedure,'6d61b66dd9a5721751205b34554f05c5','0f2c9f349b395158a33603890ec4b45c'),
    ('public.register_historischen_pachtvertrag(bigint,text,date,boolean,bigint[],jsonb,bigint,date,bigint[],jsonb,jsonb)'::regprocedure,'684b91864f6c63993c744f6ec2126c15','c6f5cab5e89d82b713fc5d99dadc9274')
  ) AS v(f,before_hash,after_hash) LOOP
    SELECT pg_get_functiondef(f) INTO def;
    IF md5(def) IS DISTINCT FROM expected_before
       OR has_function_privilege('anon',f,'execute')
       OR NOT has_function_privilege('authenticated',f,'execute')
       OR NOT has_function_privilege('service_role',f,'execute') THEN
      RAISE EXCEPTION 'Unexpected authorization precheck baseline: %',f;
    END IF;
    patched:=replace(def,E'\r\n',E'\n');
    IF f='public.mark_meter_defective(bigint)'::regprocedure THEN
      anchor:=$a$  select z.* into v_zaehler from public.zaehler z where z.id=p_zaehler_id;
  if not found then raise exception 'Zähler % wurde nicht gefunden.',p_zaehler_id; end if;$a$;
      replacement:=$b$  select z.* into v_zaehler from public.zaehler z
  where z.id=p_zaehler_id and auth.uid() is not null
    and (public.ap02_has_effective_right('zaehlerwechsel.verwalten') or exists (
      select 1 from public.parzellen_belegung pb where pb.parzelle_id=z.parzelle_id
        and pb.mitglied_id=public.current_mitglied_id()
        and (pb.von_datum is null or pb.von_datum<=current_date)
        and (pb.bis_datum is null or pb.bis_datum>=current_date)))
  for update;
  if not found then raise exception 'Zähler nicht freigegeben.' using errcode='42501'; end if;$b$;
      IF (length(patched)-length(replace(patched,anchor,'')))/length(anchor)<>1 THEN
        RAISE EXCEPTION 'Meter selection anchor mismatch';
      END IF;
      patched:=replace(patched,anchor,replacement);
      anchor:=$a$  if not (public.ap02_has_effective_right('zaehlerwechsel.verwalten') or (not public.ap02_has_effective_right('zaehlerwechsel.verwalten') and exists(
    select 1 from public.parzellen_belegung pb where pb.parzelle_id=v_zaehler.parzelle_id
      and pb.mitglied_id=public.current_mitglied_id()
      and (pb.von_datum is null or pb.von_datum<=current_date)
      and (pb.bis_datum is null or pb.bis_datum>=current_date)))) then
    raise exception 'Keine Berechtigung für Zähler %.',p_zaehler_id;
  end if;
$a$;
      replacement:='';
    ELSE
      anchor:=$a$begin
 if exists(select 1 from public.historical_contract_operation$a$;
      replacement:=$b$begin
 if not coalesce(public.can_register_historical_contracts(),false) then
   raise exception 'Altvertragsberechtigung fehlt.' using errcode='42501';
 end if;
 if exists(select 1 from public.historical_contract_operation$b$;
    END IF;
    IF (length(patched)-length(replace(patched,anchor,'')))/length(anchor)<>1 THEN
      RAISE EXCEPTION 'Authorization anchor mismatch: %',f;
    END IF;
    patched:=replace(patched,anchor,replacement);
    IF md5(patched) IS DISTINCT FROM expected_after THEN
      RAISE EXCEPTION 'Unexpected patch fingerprint: %',f;
    END IF;
    EXECUTE patched;
    IF md5(pg_get_functiondef(f)) IS DISTINCT FROM expected_after
       OR has_function_privilege('anon',f,'execute')
       OR NOT has_function_privilege('authenticated',f,'execute')
       OR NOT has_function_privilege('service_role',f,'execute') THEN
      RAISE EXCEPTION 'Unexpected authorization postcondition: %',f;
    END IF;
  END LOOP;
END
$migration$;
