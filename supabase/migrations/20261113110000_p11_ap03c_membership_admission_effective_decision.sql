-- P11 AP-03.3 / R01: an office assignment identifies the competent decision
-- maker, while CanCreateMitglied remains the effective technical authority.
-- Individual grants and revocations must apply to the final decision just as
-- they already apply when board handling is started.

create or replace function public.kann_mitgliedsaufnahme_entscheiden()
returns boolean
language sql
stable
security definer
set search_path to 'public', 'pg_temp'
as $$
  select public.has_effective_permission(524288)
     and exists (
       select 1
       from public.mitgliedsaufnahme_berechtigungen('mitgliedsaufnahme_entscheidung', current_date) x
       join public.app_user au on au.mitglied_id = x.mitglied_id
       where au.user_id = auth.uid()
     );
$$;

do $$
declare
  v_definition text;
  v_old text := 'if not exists(select 1 from public.mitgliedsaufnahme_berechtigungen(''mitgliedsaufnahme_entscheidung'',current_date) x join public.app_user au on au.mitglied_id=x.mitglied_id where au.user_id=caller) then raise exception ''Keine aktuelle Entscheidungsberechtigung'' using errcode=''42501''; end if;';
  v_new text := 'if not public.kann_mitgliedsaufnahme_entscheiden() then raise exception ''Keine aktuelle Entscheidungsberechtigung'' using errcode=''42501''; end if;';
begin
  select pg_get_functiondef('public.entscheide_mitgliedsaufnahme(bigint,text,text,date,text,date)'::regprocedure)
    into v_definition;
  if position(v_old in v_definition) = 0 then
    raise exception 'P11 AP-03.3: expected admission-decision authorization guard was not found'
      using errcode = '2BP01';
  end if;
  execute replace(v_definition, v_old, v_new);
end;
$$;

revoke all on function public.kann_mitgliedsaufnahme_entscheiden() from public, anon;
grant execute on function public.kann_mitgliedsaufnahme_entscheiden() to authenticated;

comment on function public.kann_mitgliedsaufnahme_entscheiden() is
  'P11 AP-03.3: membership admission decisions require both the current configured office and effective CanCreateMitglied (524288).';
