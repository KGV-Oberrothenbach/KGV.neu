-- CX-01: bind a persisted signature hash to metadata written by the only
-- authorised upload path. Existing already-bound rows remain retryable.
begin;

do $$
declare
  v_definition text;
  v_old text := $needle$if not exists(select 1 from storage.objects where bucket_id='prepared-contract-signatures' and name=p_storage_path) then raise exception 'Das referenzierte Signaturbild liegt nicht im privaten Storage.' using errcode='23514'; end if;$needle$;
  v_new text := $replacement$if not exists(select 1 from storage.objects where bucket_id='prepared-contract-signatures' and name=p_storage_path) then raise exception 'Das referenzierte Signaturbild liegt nicht im privaten Storage.' using errcode='23514'; end if;
 if not exists(select 1 from storage.objects where bucket_id='prepared-contract-signatures' and name=p_storage_path and lower(coalesce(user_metadata->>'sha256',''))=p_signatur_sha256)
    and not exists(select 1 from public.prepared_contract_signatur where kommunikationsvorgang_id=p_kommunikationsvorgang_id and signatur_revision=p_signatur_revision and signatur_schluessel=p_signatur_schluessel and status='unterschrieben' and storage_path=p_storage_path and signatur_sha256=p_signatur_sha256)
 then raise exception 'Der Signatur-Hash stimmt nicht mit dem privaten Storage-Objekt überein.' using errcode='23514'; end if;$replacement$;
begin
  select pg_get_functiondef(
    'public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamp with time zone)'::regprocedure
  ) into v_definition;

  if position(v_old in v_definition) = 0 then
    raise exception 'CX-01: expected storage existence guard was not found'
      using errcode = '2BP01';
  end if;

  v_definition := replace(v_definition, v_old, v_new);
  execute v_definition;

  if position('user_metadata->>''sha256''' in pg_get_functiondef(
    'public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamp with time zone)'::regprocedure
  )) = 0 then
    raise exception 'CX-01: SHA-256 metadata binding was not applied'
      using errcode = '2BP01';
  end if;
end $$;

commit;
