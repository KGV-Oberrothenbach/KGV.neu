-- G8.6: management may document a completed historical assignment after it was deactivated.
create or replace function public.confirm_arbeitseinsatz_arbeitsstunde(p_arbeitseinsatz_anmeldung_id bigint, p_stunden numeric, p_art_der_arbeit text default null)
returns public.arbeitsstunde language plpgsql security definer set search_path = public, pg_temp as $$
declare v_registration public.arbeitseinsatz_anmeldung; v_assignment public.arbeitseinsatz; v_saison_id integer; v_reviewer integer; v_row public.arbeitsstunde; v_end timestamp without time zone;
begin
  if not public.has_effective_permission(1048576) or not public.has_effective_permission(256) then raise exception 'Für die Arbeitsstundenbestätigung fehlen ManageWorkAssignments und ManageWorkHours.' using errcode = '42501'; end if;
  if p_stunden is null or p_stunden <= 0 then raise exception 'Die Stunden müssen größer als 0 sein.' using errcode = '22023'; end if;
  v_reviewer := public.current_mitglied_id(); if v_reviewer is null then raise exception 'Der Bearbeiter ist keinem Mitglied zugeordnet.' using errcode = '42501'; end if;
  select * into v_registration from public.arbeitseinsatz_anmeldung where id = p_arbeitseinsatz_anmeldung_id for update;
  if not found then raise exception 'Die Anmeldung existiert nicht.' using errcode = 'P0002'; end if;
  select * into v_assignment from public.arbeitseinsatz where id = v_registration.arbeitseinsatz_id;
  if not found then raise exception 'Der Arbeitseinsatz wurde nicht gefunden.' using errcode = 'P0002'; end if;
  if not ((not public.is_demo_or_reviewer() and coalesce(v_assignment.is_demo, false) = false and not public.is_demo_mitglied_id(v_registration.mitglied_id)) or (public.is_demo_or_reviewer() and public.is_demo_member_arbeitseinsatz_scope(v_registration.mitglied_id, v_assignment.id))) then raise exception 'Arbeitseinsatz liegt außerhalb des zulässigen Scopes.' using errcode = '42501'; end if;
  if v_registration.status = 'teilgenommen' or exists (select 1 from public.arbeitsstunde where arbeitseinsatz_anmeldung_id = v_registration.id) then raise exception 'Für diese Anmeldung existiert bereits eine Arbeitsstunde.' using errcode = '23505'; end if;
  v_end := v_assignment.datum + coalesce(v_assignment.end_uhrzeit, v_assignment.start_uhrzeit, time '23:59'); if public.kgv_local_now() < v_end then raise exception 'Arbeitsstunden können erst nach Einsatzende bestätigt werden.' using errcode = '42501'; end if;
  select id into v_saison_id from public.saison where jahr = extract(year from v_assignment.datum)::integer limit 1; if v_saison_id is null then raise exception 'Für das Einsatzjahr ist keine Saison hinterlegt.' using errcode = 'P0002'; end if;
  insert into public.arbeitsstunde(mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id) values (v_registration.mitglied_id,v_saison_id,v_assignment.datum,p_stunden,coalesce(nullif(btrim(p_art_der_arbeit),''),v_assignment.titel),'genehmigt',true,v_reviewer,now(),v_registration.id) returning * into v_row;
  update public.arbeitseinsatz_anmeldung set status = 'teilgenommen', updated_at = now() where id = v_registration.id;
  return v_row;
end; $$;
revoke all on function public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) from public, anon;
grant execute on function public.confirm_arbeitseinsatz_arbeitsstunde(bigint,numeric,text) to authenticated;

create or replace view public.v_startseite_arbeitseinsatz
with (security_invoker = 'true') as
select a.id, a.titel, a.beschreibung, a.datum, a.start_uhrzeit, a.end_uhrzeit,
  a.treffpunkt, a.max_teilnehmer, a.stunden_wert, a.sichtbar_ab, a.sichtbar_bis,
  a.anmeldung_bis,
  coalesce(sum(case when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1 else 0 end), 0)::integer as angemeldet_count,
  case when a.max_teilnehmer is null then null::integer else greatest(a.max_teilnehmer - coalesce(sum(case when aa.status = 'angemeldet'::public.arbeitseinsatz_anmeldung_status then 1 else 0 end), 0), 0)::integer end as freie_plaetze,
  a.aktiv
from public.arbeitseinsatz a
left join public.arbeitseinsatz_anmeldung aa on aa.arbeitseinsatz_id = a.id
where a.aktiv = true
  and (a.sichtbar_ab is null or a.sichtbar_ab <= public.kgv_local_now())
  and (a.sichtbar_bis is null or a.sichtbar_bis >= public.kgv_local_now())
group by a.id, a.titel, a.beschreibung, a.datum, a.start_uhrzeit, a.end_uhrzeit,
  a.treffpunkt, a.max_teilnehmer, a.stunden_wert, a.sichtbar_ab, a.sichtbar_bis,
  a.anmeldung_bis, a.aktiv
order by a.datum, a.start_uhrzeit nulls first, a.id;
