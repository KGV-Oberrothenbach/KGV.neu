-- P11 AP-02B.3: protect meter-photo provenance and bind historical access to
-- the same R05/R10 boundary as the parcel and lease records.

create or replace function public.can_access_own_meter_reading(p_ablesung_id bigint)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.uid() is not null
    and exists (
      select 1
      from public.zaehler_ablesung a
      join public.zaehler z on z.id = a.zaehler_id
      where a.id = p_ablesung_id
        and z.parzelle_id is not null
        and public.can_access_own_lease_parcel(z.parzelle_id)
    );
$$;

revoke all on function public.can_access_own_meter_reading(bigint) from public, anon;
grant execute on function public.can_access_own_meter_reading(bigint) to authenticated, service_role;

drop policy if exists zaehler_ablesung_select_effective_permission on public.zaehler_ablesung;
create policy zaehler_ablesung_select_effective_permission on public.zaehler_ablesung
for select to authenticated
using (
  public.has_effective_permission(32)
  or public.can_access_own_meter_reading(id)
);

-- Drive file identifiers are authoritative evidence references.  They must be
-- written only by the trusted upload function's service-role chain, never by a
-- browser PATCH or by a direct RPC parameter.
create or replace function public.guard_meter_reading_photo_provenance()
returns trigger
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if tg_op = 'INSERT' then
    if coalesce(current_setting('request.jwt.claim.role', true), '') <> 'service_role'
       and (new.foto_pfad is not null or new.foto_drive_file_id is not null or new.foto_dateiname is not null) then
      raise exception 'Foto-Provenance darf nur durch den geschützten Uploadpfad gespeichert werden.' using errcode = '42501';
    end if;
    return new;
  end if;

  if new.foto_pfad is distinct from old.foto_pfad
     or new.foto_drive_file_id is distinct from old.foto_drive_file_id
     or new.foto_dateiname is distinct from old.foto_dateiname then
    if coalesce(current_setting('request.jwt.claim.role', true), '') <> 'service_role' then
      raise exception 'Foto-Provenance darf nur durch den geschützten Uploadpfad geändert werden.' using errcode = '42501';
    end if;
    if old.foto_drive_file_id is not null then
      raise exception 'Eine gespeicherte Foto-Provenance ist unveränderlich.' using errcode = '55000';
    end if;
  end if;
  return new;
end;
$$;

drop trigger if exists trg_guard_meter_reading_photo_provenance on public.zaehler_ablesung;
create trigger trg_guard_meter_reading_photo_provenance
before insert or update on public.zaehler_ablesung
for each row execute function public.guard_meter_reading_photo_provenance();

revoke all on function public.guard_meter_reading_photo_provenance() from public, anon, authenticated;

comment on function public.can_access_own_meter_reading(bigint) is
  'P11 AP-02B.3: caller-bound own meter-reading access, constrained by former-member R05 and former-tenant R10 via the parcel helper.';
