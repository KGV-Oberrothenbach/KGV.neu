-- P11 AP-02B.3: local authenticated RLS and photo-provenance acceptance.
-- Synthetic rows only; this test always rolls back.
begin;

insert into auth.users(id,aud,role,email,encrypted_password,email_confirmed_at,raw_app_meta_data,raw_user_meta_data,created_at,updated_at) values
  ('00000000-0000-0000-0000-00000000b231','authenticated','authenticated','p11-b23-former@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b232','authenticated','authenticated','p11-b23-successor@example.test','x',now(),'{}','{}',now(),now()),
  ('00000000-0000-0000-0000-00000000b233','authenticated','authenticated','p11-b23-revoked@example.test','x',now(),'{}','{}',now(),now());

insert into public.mitglied(name,vorname,aktiv,aufnahme_status,auth_user_id) values
  ('P11 B23 Former tenant','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b231'),
  ('P11 B23 Successor','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b232'),
  ('P11 B23 Revoked administrator','Test',true,'aufgenommen','00000000-0000-0000-0000-00000000b233');

create temporary table p11_b23_ids(former_member_id bigint, successor_member_id bigint, parcel_id bigint, meter_id bigint, reading_id bigint) on commit drop;
insert into p11_b23_ids
select (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b231'),
       (select id from public.mitglied where auth_user_id='00000000-0000-0000-0000-00000000b232'),0,0,0;
insert into public.app_user(user_id,mitglied_id,role,permission_grants,permission_revocations)
select '00000000-0000-0000-0000-00000000b231'::uuid,former_member_id,'user',64,0 from p11_b23_ids
union all select '00000000-0000-0000-0000-00000000b232'::uuid,successor_member_id,'user',0,0 from p11_b23_ids
union all select '00000000-0000-0000-0000-00000000b233'::uuid,null,'admin',0,32
on conflict(user_id) do update set mitglied_id=excluded.mitglied_id,role=excluded.role,permission_grants=excluded.permission_grants,permission_revocations=excluded.permission_revocations;

set local session_replication_role=replica;
insert into public.parzelle(garten_nr,"Anlage") values ('B23-1','P11');
update p11_b23_ids set parcel_id=(select id from public.parzelle where garten_nr='B23-1');
insert into public.zaehler(parzelle_id,medium,zaehlernummer,eichdatum,eichfaellig_am,eingebaut_am,status)
select parcel_id,'strom','P11-B23',date '2020-01-01',date '2030-01-01',date '2020-01-01','aktiv' from p11_b23_ids;
update p11_b23_ids set meter_id=(select id from public.zaehler where zaehlernummer='P11-B23');
insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum)
select parcel_id,former_member_id,date '2020-01-01',(public.kgv_local_now()::date - 1) from p11_b23_ids;
insert into public.zaehler_ablesung(zaehler_id,ablesedatum,stand,art,freigegeben,pruefstatus)
select meter_id,(public.kgv_local_now()::date - 2),42,'normal',true,'freigegeben' from p11_b23_ids;
update p11_b23_ids set reading_id=(select id from public.zaehler_ablesung where zaehler_id=meter_id);
set local session_replication_role=origin;
grant select on p11_b23_ids to authenticated;

set local role authenticated;
do $$ declare v p11_b23_ids%rowtype; begin
  select * into v from p11_b23_ids;
  perform set_config('request.jwt.claim.role','authenticated',true);
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b231',true);
  if not public.can_access_own_meter_reading(v.reading_id) or not exists(select 1 from public.zaehler_ablesung where id=v.reading_id) then
    raise exception 'P11 B23 former tenant cannot read own photo evidence before takeover';
  end if;
  begin
    update public.zaehler_ablesung set foto_drive_file_id='forged-drive-id',foto_pfad='https://attacker.invalid/photo',foto_dateiname='forged.jpg' where id=v.reading_id;
    raise exception 'P11 B23 direct photo provenance rewrite was accepted';
  exception when sqlstate '42501' then null; end;
end $$;
reset role;

-- A planned successor does not revoke history; a factual takeover does.
insert into public.parzellen_belegung(parzelle_id,mitglied_id,von_datum,bis_datum)
select parcel_id,successor_member_id,public.kgv_local_now()::date + 1,null from p11_b23_ids;
set local role authenticated;
do $$ declare v p11_b23_ids%rowtype; begin
  select * into v from p11_b23_ids; perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b231',true);
  if not public.can_access_own_meter_reading(v.reading_id) then raise exception 'P11 B23 planned successor prematurely blocked photo'; end if;
end $$;
reset role;
update public.parzellen_belegung set von_datum=public.kgv_local_now()::date where parzelle_id=(select parcel_id from p11_b23_ids) and mitglied_id=(select successor_member_id from p11_b23_ids);
set local role authenticated;
do $$ declare v p11_b23_ids%rowtype; begin
  select * into v from p11_b23_ids; perform set_config('request.jwt.claim.role','authenticated',true); perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b231',true);
  if public.can_access_own_meter_reading(v.reading_id) or exists(select 1 from public.zaehler_ablesung where id=v.reading_id) then raise exception 'P11 B23 successor takeover did not close photo access'; end if;
  perform set_config('request.jwt.claim.sub','00000000-0000-0000-0000-00000000b233',true);
  if exists(select 1 from public.zaehler_ablesung where id=v.reading_id) then raise exception 'P11 B23 revoked admin retained meter-photo access'; end if;
end $$;
reset role;
rollback;
