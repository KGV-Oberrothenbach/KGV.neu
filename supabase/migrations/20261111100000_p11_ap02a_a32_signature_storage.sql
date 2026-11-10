-- P11 AP-02A / A3.2: a signature is authorized by the signer identity and
-- frozen contract snapshot, not by a broad contract-administration role.
begin;
do $$
declare v_definition text;
begin
 select pg_get_functiondef('public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamp with time zone)'::regprocedure) into v_definition;
 if position('if not public.is_admin_or_vorstand() or auth.uid() is null then' in v_definition)=0 then raise exception 'P11 A3.2 expected role guard not found' using errcode='2BP01'; end if;
 v_definition:=replace(v_definition,'if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception ''Nur angemeldete administrative Vertragsbearbeitung darf Signaturen speichern.'' using errcode=''42501''; end if;', 'if auth.uid() is null then raise exception ''Anmeldung erforderlich.'' using errcode=''42501''; end if;');
 v_definition:=replace(v_definition,'if not found then raise exception ''Die Signaturposition wurde nicht vorbereitet.'' using errcode=''P0002''; end if;', 'if not found then raise exception ''Die Signaturposition wurde nicht vorbereitet.'' using errcode=''P0002''; end if; if not exists (select 1 from public.app_user au where au.user_id=auth.uid() and au.mitglied_id=p_mitglied_id) then raise exception ''Die Signatur darf nur durch die zugeordnete Person abgegeben werden.'' using errcode=''42501''; end if;');
 execute v_definition;
 if position('au.user_id=auth.uid() and au.mitglied_id=p_mitglied_id' in pg_get_functiondef('public.speichere_prepared_contract_signatur(bigint,integer,text,bigint,text,bigint,bigint,bigint,text,text,timestamp with time zone)'::regprocedure))=0 then raise exception 'P11 A3.2 signer binding was not applied' using errcode='2BP01'; end if;
end $$;
commit;
