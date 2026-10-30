-- G7 shared read model. It is capability-gated just like the write RPC.
create or replace function public.get_historische_pachtvertragsvorschau(p_pachtverhaeltnis_ids bigint[], p_vertragsdatum date)
returns table(pachtverhaeltnis_id bigint, pachtvertragsfassung_id bigint, wirksam_ab date, gueltig_bis date, historische_vertragsnummer integer, vertragspartei_mitglied_ids bigint[], ist_vorgaenger boolean, ist_nachfolger boolean, vorgeschlagenes_vorgaengerende date)
language plpgsql stable security definer set search_path = public, pg_temp as $$
begin
  if not public.can_register_historical_contracts() then
    raise exception 'Für die Nacherfassung historischer Verträge fehlt die Berechtigung.' using errcode = '42501';
  end if;
  return query
  with versions as (
    select f.*, d.historische_vertragsnummer
    from public.pachtvertragsfassung f join public.dokument d on d.id = f.dokument_id
    where f.pachtverhaeltnis_id = any(p_pachtverhaeltnis_ids)
  )
  select v.pachtverhaeltnis_id, v.id, v.wirksam_ab, v.gueltig_bis, v.historische_vertragsnummer,
    coalesce(array(select b.mitglied_id from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id = v.pachtverhaeltnis_id and b.gueltig_ab <= v.wirksam_ab and (b.gueltig_bis is null or b.gueltig_bis >= v.wirksam_ab) order by b.mitglied_id), '{}'::bigint[]),
    v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum),
    v.wirksam_ab = (select min(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab > p_vertragsdatum),
    case when v.wirksam_ab = (select max(x.wirksam_ab) from public.pachtvertragsfassung x where x.pachtverhaeltnis_id = v.pachtverhaeltnis_id and x.wirksam_ab < p_vertragsdatum) then p_vertragsdatum - 1 end
  from versions v order by v.pachtverhaeltnis_id, v.wirksam_ab, v.id;
end;
$$;
revoke all on function public.get_historische_pachtvertragsvorschau(bigint[], date) from public, anon;
grant execute on function public.get_historische_pachtvertragsvorschau(bigint[], date) to authenticated, service_role;
