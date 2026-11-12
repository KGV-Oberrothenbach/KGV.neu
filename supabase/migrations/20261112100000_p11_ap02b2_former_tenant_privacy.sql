-- P11 AP-02B.2 / R10: former tenants retain only their own lease history
-- until an actually started later occupancy of the same parcel.  A future
-- allocation is intentionally not a handover.
begin;

create or replace function public.can_access_own_lease_relationship(p_pachtverhaeltnis_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and public.current_mitglied_id() is not null
    and exists (
      select 1
      from public.pachtverhaeltnis r
      join public.pachtverhaeltnis_beteiligung b on b.pachtverhaeltnis_id = r.id
      join public.mitglied m on m.id = b.mitglied_id
      where r.id = p_pachtverhaeltnis_id
        and b.mitglied_id = public.current_mitglied_id()
        and (m.mitglied_ende is null or public.kgv_local_now()::date <= public.former_member_history_access_until(m.mitglied_ende))
        and not exists (
          select 1
          from public.parzellen_belegung successor
          where successor.parzelle_id = r.parzelle_id
            and successor.mitglied_id <> b.mitglied_id
            and successor.von_datum is not null
            and successor.von_datum <= public.kgv_local_now()::date
            and successor.von_datum > b.gueltig_ab
        )
    );
$$;

create or replace function public.can_access_own_lease_occupancy(p_belegung_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and public.current_mitglied_id() is not null
    and exists (
      select 1
      from public.parzellen_belegung own_occupancy
      join public.mitglied m on m.id = own_occupancy.mitglied_id
      where own_occupancy.id = p_belegung_id
        and own_occupancy.mitglied_id = public.current_mitglied_id()
        and (m.mitglied_ende is null or public.kgv_local_now()::date <= public.former_member_history_access_until(m.mitglied_ende))
        and not exists (
          select 1
          from public.parzellen_belegung successor
          where successor.parzelle_id = own_occupancy.parzelle_id
            and successor.mitglied_id <> own_occupancy.mitglied_id
            and successor.von_datum is not null
            and successor.von_datum <= public.kgv_local_now()::date
            and successor.von_datum > own_occupancy.von_datum
        )
    );
$$;

create or replace function public.can_access_own_lease_parcel(p_parzelle_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and public.current_mitglied_id() is not null
    and exists (
      select 1
      from public.parzellen_belegung own_occupancy
      join public.mitglied m on m.id = own_occupancy.mitglied_id
      where own_occupancy.parzelle_id = p_parzelle_id
        and own_occupancy.mitglied_id = public.current_mitglied_id()
        and (m.mitglied_ende is null or public.kgv_local_now()::date <= public.former_member_history_access_until(m.mitglied_ende))
        and not exists (
          select 1
          from public.parzellen_belegung successor
          where successor.parzelle_id = own_occupancy.parzelle_id
            and successor.mitglied_id <> own_occupancy.mitglied_id
            and successor.von_datum is not null
            and successor.von_datum <= public.kgv_local_now()::date
            and successor.von_datum > own_occupancy.von_datum
        )
    );
$$;

create or replace function public.can_access_own_historical_lease_document(p_dokument_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and exists (
      select 1
      from public.pachtvertragsfassung f
      where f.dokument_id = p_dokument_id
        and public.can_access_own_lease_relationship(f.pachtverhaeltnis_id)
    );
$$;

-- R05 continues to apply to ordinary personal history.  A lease-contract
-- document is deliberately delegated to the stricter R10 rule above.
create or replace function public.can_access_own_member_history_document(p_dokument_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and public.current_mitglied_id() is not null
    and exists (
      select 1
      from public.dokument d
      join public.mitglied m on m.id = d.mitglied_id
      where d.id = p_dokument_id
        and m.id = public.current_mitglied_id()
        and not exists (select 1 from public.pachtvertragsfassung f where f.dokument_id = d.id)
        and (m.mitglied_ende is null or public.kgv_local_now()::date <= public.former_member_history_access_until(m.mitglied_ende))
    );
$$;

revoke all on function public.can_access_own_lease_relationship(bigint), public.can_access_own_lease_occupancy(bigint), public.can_access_own_lease_parcel(bigint), public.can_access_own_historical_lease_document(bigint) from public, anon;
grant execute on function public.can_access_own_lease_relationship(bigint), public.can_access_own_lease_occupancy(bigint), public.can_access_own_lease_parcel(bigint), public.can_access_own_historical_lease_document(bigint) to authenticated, service_role;

drop policy if exists parzelle_admin_full on public.parzelle;
drop policy if exists parzelle_select_assigned_or_admin on public.parzelle;
create policy parzelle_select_effective_or_own_lease on public.parzelle for select to authenticated
  using (public.has_effective_permission(16384) or public.can_access_own_lease_parcel(id));
create policy parzelle_write_effective on public.parzelle for all to authenticated
  using (public.has_effective_permission(32768)) with check (public.has_effective_permission(32768));

drop policy if exists parzellen_belegung_admin_full on public.parzellen_belegung;
drop policy if exists parzellen_belegung_select_own_or_admin on public.parzellen_belegung;
create policy parzellen_belegung_select_effective_or_own_lease on public.parzellen_belegung for select to authenticated
  using (public.has_effective_permission(16384) or public.can_access_own_lease_occupancy(id));
create policy parzellen_belegung_write_effective on public.parzellen_belegung for all to authenticated
  using (public.has_effective_permission(32768)) with check (public.has_effective_permission(32768));

drop policy if exists pachtverhaeltnis_contract_effective on public.pachtverhaeltnis;
drop policy if exists pachtverhaeltnis_beteiligung_contract_effective on public.pachtverhaeltnis_beteiligung;
drop policy if exists pachtvertragsfassung_contract_effective on public.pachtvertragsfassung;
create policy pachtverhaeltnis_contract_effective_or_own_history on public.pachtverhaeltnis for select to authenticated
  using (public.has_effective_permission(524288) or public.can_access_own_lease_relationship(id));
create policy pachtverhaeltnis_beteiligung_contract_effective_or_own_history on public.pachtverhaeltnis_beteiligung for select to authenticated
  using (public.has_effective_permission(524288) or public.can_access_own_lease_relationship(pachtverhaeltnis_id));
create policy pachtvertragsfassung_contract_effective_or_own_history on public.pachtvertragsfassung for select to authenticated
  using (public.has_effective_permission(524288) or public.can_access_own_lease_relationship(pachtverhaeltnis_id));

drop policy if exists dokument_select_effective on public.dokument;
create policy dokument_select_effective on public.dokument for select to authenticated
using (
  public.has_effective_permission(65536)
  or public.has_effective_permission(16)
  or public.can_access_own_member_history_document(id)
  or public.can_access_own_historical_lease_document(id)
  or (parzelle_id is not null and exists (
    select 1 from public.parzellen_belegung b
    where b.parzelle_id = dokument.parzelle_id
      and b.mitglied_id = public.current_mitglied_id()
      and (b.von_datum is null or b.von_datum <= public.kgv_local_now()::date)
      and (b.bis_datum is null or b.bis_datum >= public.kgv_local_now()::date)
  ))
);

commit;
