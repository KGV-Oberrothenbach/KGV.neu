-- AP-01.4: real PostgreSQL RLS simulation for direct table/view/RPC access.
-- Synthetic rows only; the whole test is rolled back.
begin;

insert into auth.users(
  id, aud, role, email, encrypted_password, email_confirmed_at,
  raw_app_meta_data, raw_user_meta_data, created_at, updated_at
) values
  ('00000000-0000-0000-0000-000000014001','authenticated','authenticated','ap014-own@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014002','authenticated','authenticated','ap014-foreign@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014003','authenticated','authenticated','ap014-granted-view@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014004','authenticated','authenticated','ap014-granted-edit@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014005','authenticated','authenticated','ap014-vorstand@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014006','authenticated','authenticated','ap014-admin@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014007','authenticated','authenticated','ap014-revoked@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014008','authenticated','authenticated','ap014-unmapped@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014009','authenticated','authenticated','ap014-representative@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014010','authenticated','authenticated','ap014-minor@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014011','authenticated','authenticated','ap014-own-revoked@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-000000014012','authenticated','authenticated','ap014-grant-revoked@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(
  name, vorname, aktiv, aufnahme_status, auth_user_id, email,
  adresse, plz, ort, telefon, handy, geburtsdatum
) values
  ('AP014 Own','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014001','ap014-own@example.test','Eigenweg 1','01001','Eigenort','01001','01002','1980-01-01'),
  ('AP014 Foreign','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014002','ap014-foreign@example.test','Fremdweg 2','02002','Fremdort','02001','02002','1981-02-02'),
  ('AP014 Granted View','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014003','ap014-granted-view@example.test','Leserweg 3','03003','Leserort','03001','03002','1982-03-03'),
  ('AP014 Granted Edit','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014004','ap014-granted-edit@example.test','Editorweg 4','04004','Editorort','04001','04002','1983-04-04'),
  ('AP014 Vorstand','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014005','ap014-vorstand@example.test','Vorstandweg 5','05005','Vorstandort','05001','05002','1984-05-05'),
  ('AP014 Admin','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014006','ap014-admin@example.test','Adminweg 6','06006','Adminort','06001','06002','1985-06-06'),
  ('AP014 Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014007','ap014-revoked@example.test','Entzugweg 7','07007','Entzugort','07001','07002','1986-07-07'),
  ('AP014 Representative','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014009','ap014-representative@example.test','Vertreterweg 9','09009','Vertreterort','09001','09002','1987-09-09'),
  ('AP014 Minor','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014010','ap014-minor@example.test','Kindweg 10','10010','Kindort','10001','10002','2015-10-10'),
  ('AP014 Own Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014011','ap014-own-revoked@example.test','Sperrweg 11','11011','Sperrort','11001','11002','1988-11-11'),
  ('AP014 Grant Revoked','Test',true,'aufgenommen','00000000-0000-0000-0000-000000014012','ap014-grant-revoked@example.test','Widerrufweg 12','12012','Widerrufort','12001','12002','1989-12-12');

create temporary table ap014_member_ids(
  key text primary key,
  id bigint not null
) on commit drop;
insert into ap014_member_ids(key, id)
select split_part(email, '@', 1), id
from public.mitglied
where email like 'ap014-%@example.test';
grant select on ap014_member_ids to authenticated;

insert into public.app_user(user_id, mitglied_id, role, permission_grants, permission_revocations)
select '00000000-0000-0000-0000-000000014001'::uuid, id, 'user', 0, 0 from ap014_member_ids where key='ap014-own'
union all select '00000000-0000-0000-0000-000000014002', id, 'user', 0, 0 from ap014_member_ids where key='ap014-foreign'
union all select '00000000-0000-0000-0000-000000014003', id, 'user', 2, 0 from ap014_member_ids where key='ap014-granted-view'
union all select '00000000-0000-0000-0000-000000014004', id, 'user', 4, 0 from ap014_member_ids where key='ap014-granted-edit'
union all select '00000000-0000-0000-0000-000000014005', id, 'vorstand', 0, 0 from ap014_member_ids where key='ap014-vorstand'
union all select '00000000-0000-0000-0000-000000014006', id, 'admin', 0, 0 from ap014_member_ids where key='ap014-admin'
union all select '00000000-0000-0000-0000-000000014007', id, 'admin', 0, 6 from ap014_member_ids where key='ap014-revoked'
union all select '00000000-0000-0000-0000-000000014008', null, 'user', 0, 0
union all select '00000000-0000-0000-0000-000000014009', id, 'user', 0, 0 from ap014_member_ids where key='ap014-representative'
union all select '00000000-0000-0000-0000-000000014010', id, 'user', 0, 0 from ap014_member_ids where key='ap014-minor'
union all select '00000000-0000-0000-0000-000000014011', id, 'user', 0, 8 from ap014_member_ids where key='ap014-own-revoked'
union all select '00000000-0000-0000-0000-000000014012', id, 'user', 2, 2 from ap014_member_ids where key='ap014-grant-revoked'
on conflict(user_id) do update set
  mitglied_id=excluded.mitglied_id,
  role=excluded.role,
  permission_grants=excluded.permission_grants,
  permission_revocations=excluded.permission_revocations;

insert into public.mitglied_gesetzlicher_vertreter(
  minderjaehriges_mitglied_id, vertreter_mitglied_id, gueltig_ab, gueltig_bis, bemerkung
)
select minor.id, representative.id, current_date - 1, null, 'AP-01.4 synthetic test'
from ap014_member_ids minor
cross join ap014_member_ids representative
where minor.key='ap014-minor' and representative.key='ap014-representative';

-- Anonymous callers do not even receive table privileges.
set local role anon;
do $test$
begin
  begin
    perform 1 from public.mitglied limit 1;
    raise exception 'AP-01.4 anon unexpectedly read public.mitglied';
  exception when insufficient_privilege then
    null;
  end;
end
$test$;
reset role;

set local role authenticated;

-- Standard role: exactly the consistently bound own row, including its own PII.
do $test$
declare v_count integer; v_email text;
begin
  perform set_config('request.jwt.claim.role','authenticated',true);
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014001',true);
  select count(*) into v_count
  from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 1 then raise exception 'AP-01.4 standard user saw % synthetic member rows instead of own row', v_count; end if;
  select m.email into v_email from public.mitglied m where m.auth_user_id='00000000-0000-0000-0000-000000014001';
  if v_email <> 'ap014-own@example.test' then raise exception 'AP-01.4 own PII was not readable'; end if;
  if exists (
    select 1 from public.mitglied m
    where m.id=(select id from ap014_member_ids where key='ap014-foreign')
  ) then raise exception 'AP-01.4 standard user read a foreign member id'; end if;
end
$test$;

-- A security-invoker view must inherit the same member-row scope.
do $test$
declare v_count integer;
begin
  select count(*) into v_count
  from public.v_mitglied_mitgliedsantrag_nachweisstatus v
  join ap014_member_ids ids on ids.id=v.mitglied_id;
  if v_count <> 1 then raise exception 'AP-01.4 member view bypassed RLS with % rows', v_count; end if;
end
$test$;

-- Explicit individual global grants are honored.  An effective revocation wins.
do $test$
declare v_count integer;
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014003',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 11 then raise exception 'AP-01.4 explicit CanViewMembers grant saw % of 11 rows', v_count; end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014004',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 11 then raise exception 'AP-01.4 explicit CanEditAllMembers grant saw % of 11 rows', v_count; end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014012',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 1 then raise exception 'AP-01.4 revoked individual view grant retained global access (% rows)', v_count; end if;
end
$test$;

-- Vorstand/admin retain global effective access; removing both global bits denies it.
do $test$
declare v_count integer;
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014005',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 11 then raise exception 'AP-01.4 Vorstand saw % of 11 rows', v_count; end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014006',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 11 then raise exception 'AP-01.4 admin saw % of 11 rows', v_count; end if;

  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014007',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 0 then raise exception 'AP-01.4 revoked admin retained member access (% rows)', v_count; end if;
end
$test$;

-- Revoking the own-data bit must deny own access and must never broaden scope.
do $test$
declare v_count integer;
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014011',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 0 then raise exception 'AP-01.4 own-data revocation broadened or retained access (% rows)', v_count; end if;
end
$test$;

-- Missing member mapping and an invalid/missing session fail closed.
do $test$
declare v_count integer;
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014008',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 0 then raise exception 'AP-01.4 unmapped user saw % member rows', v_count; end if;

  perform set_config('request.jwt.claim.sub','',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 0 then raise exception 'AP-01.4 invalid session saw % member rows', v_count; end if;
end
$test$;

-- Active legal representation exposes only the representative and represented minor.
do $test$
declare v_count integer;
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014009',true);
  select count(*) into v_count from public.mitglied m join ap014_member_ids ids on ids.id=m.id;
  if v_count <> 2 then raise exception 'AP-01.4 legal representative saw % rows instead of own + minor', v_count; end if;
  if not exists (
    select 1 from public.mitglied m
    where m.id=(select id from ap014_member_ids where key='ap014-minor')
  ) then raise exception 'AP-01.4 legal representative could not read represented minor'; end if;
  if exists (
    select 1 from public.mitglied m
    where m.id=(select id from ap014_member_ids where key='ap014-foreign')
  ) then raise exception 'AP-01.4 legal representative read unrelated member'; end if;
end
$test$;

-- The protected export gateway is a SECURITY DEFINER path and must reject a user.
do $test$
begin
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-000000014001',true);
  begin
    perform * from public.rpc_execute_export('datenschutz_aktive_mitglieder','{}'::jsonb);
    raise exception 'AP-01.4 standard user executed the protected personal-data export';
  exception when sqlstate '42501' then
    null;
  end;
end
$test$;

reset role;

-- Catalog guards for REST-exposed table/view/function paths.
do $test$
declare v_policy text; v_options text[];
begin
  select qual into v_policy
  from pg_policies
  where schemaname='public' and tablename='mitglied' and policyname='mitglied_select_authorized_scope';
  if v_policy is null or v_policy not like '%can_read_mitglied%' then
    raise exception 'AP-01.4 authoritative member SELECT policy is missing';
  end if;

  select c.reloptions into v_options
  from pg_class c join pg_namespace n on n.oid=c.relnamespace
  where n.nspname='public' and c.relname='v_mitglied_mitgliedsantrag_nachweisstatus';
  if not ('security_invoker=true'=any(coalesce(v_options,array[]::text[]))) then
    raise exception 'AP-01.4 member proof-status view is not security_invoker';
  end if;

  if has_function_privilege('anon','public.can_read_mitglied(bigint,uuid)','EXECUTE') then
    raise exception 'AP-01.4 anon can execute member authorization helper';
  end if;
  if not has_function_privilege('authenticated','public.can_read_mitglied(bigint,uuid)','EXECUTE') then
    raise exception 'AP-01.4 policy caller cannot execute member authorization helper';
  end if;
end
$test$;

rollback;
