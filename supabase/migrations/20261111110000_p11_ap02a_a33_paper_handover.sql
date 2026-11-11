-- P11 AP-02A / A3.3: paper handover is a prepared-contract action.
begin;
do $$
declare v_definition text;
begin
 select pg_get_functiondef('public.dokumentiere_prepared_contract_papieruebergabe(bigint,text,text,timestamp with time zone,text)'::regprocedure) into v_definition;
 if position('if not public.is_admin_or_vorstand() or auth.uid() is null then' in v_definition)=0 then raise exception 'P11 A3.3 expected role guard not found' using errcode='2BP01'; end if;
 v_definition:=replace(v_definition,'if not public.is_admin_or_vorstand() or auth.uid() is null then raise exception ''Keine Berechtigung für Papierübergabe'' using errcode=''42501''; end if;', 'if auth.uid() is null or not public.has_effective_permission(524288) then raise exception ''CanCreateMitglied required for paper handover'' using errcode=''42501''; end if;');
 v_definition:=replace(v_definition,'required:=case v.dokumenttyp', 'if not found then raise exception ''Unbekannter Vertragsvorgang'' using errcode=''P0002''; end if; if v.zugewiesen_an is distinct from auth.uid() then raise exception ''Der Vertragsvorgang ist nicht dem angemeldeten Bearbeiter zugewiesen'' using errcode=''42501''; end if; required:=case v.dokumenttyp');
 v_definition:=replace(v_definition,'where dokumenttyp_code=p_dokumenttyp and fassung=btrim(p_fassung); if f.id is null then', 'where dokumenttyp_code=p_dokumenttyp and fassung=btrim(p_fassung) and gueltig_ab<=p_uebergabe_am::date and (gueltig_bis is null or gueltig_bis>=p_uebergabe_am::date); if f.id is null then');
 execute v_definition;
 if position('has_effective_permission(524288)' in pg_get_functiondef('public.dokumentiere_prepared_contract_papieruebergabe(bigint,text,text,timestamp with time zone,text)'::regprocedure))=0 then raise exception 'P11 A3.3 effective-right guard not applied' using errcode='2BP01'; end if;
end $$;
do $$
declare v_definition text; v_old text := 'coalesce((select jsonb_agg(case when x->>''dokumenttyp''=p_dokumenttyp then a else x end) from jsonb_array_elements(coalesce(v.entwurf_daten->''vereinsunterlagenNachweis'',''[]'')) x),''[]''::jsonb)';
begin
 select pg_get_functiondef('public.dokumentiere_prepared_contract_papieruebergabe(bigint,text,text,timestamp with time zone,text)'::regprocedure) into v_definition;
 if position(v_old in v_definition)=0 then raise exception 'P11 A3.3 expected handover-array expression not found' using errcode='2BP01'; end if;
 v_definition:=replace(v_definition,v_old,'case when exists(select 1 from jsonb_array_elements(coalesce(v.entwurf_daten->''vereinsunterlagenNachweis'',''[]'')) x where x->>''dokumenttyp''=p_dokumenttyp) then '||v_old||' else jsonb_build_array(a) end');
 execute v_definition;
end $$;
commit;
