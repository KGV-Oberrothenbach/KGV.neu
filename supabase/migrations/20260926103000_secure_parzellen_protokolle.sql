-- Protokolle enthalten personenbezogene Angaben, Zählerstände und Unterschriften.
-- Sie sind ausschließlich für Vorstand/Admin zugänglich. Mitglieder erhalten nur
-- Zugriff auf das zugehörige Dokument über die bestehende dokument-RLS-Policy.

alter table public.parzellen_protokoll enable row level security;
alter table public.parzellen_protokoll_ablesung enable row level security;
alter table public.parzellen_begehung_feststellung enable row level security;

create policy parzellen_protokoll_admin_full
on public.parzellen_protokoll
for all to authenticated
using (public.is_productive_admin_or_vorstand())
with check (public.is_productive_admin_or_vorstand());

create policy parzellen_protokoll_demo_admin_full
on public.parzellen_protokoll
for all to authenticated
using (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1 from public.mitglied m
    where m.id = parzellen_protokoll.mitglied_id
      and coalesce(m.is_demo, false) = true
  )
)
with check (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1 from public.mitglied m
    where m.id = parzellen_protokoll.mitglied_id
      and coalesce(m.is_demo, false) = true
  )
);

create policy parzellen_protokoll_ablesung_admin_full
on public.parzellen_protokoll_ablesung
for all to authenticated
using (public.is_productive_admin_or_vorstand())
with check (public.is_productive_admin_or_vorstand());

create policy parzellen_protokoll_ablesung_demo_admin_full
on public.parzellen_protokoll_ablesung
for all to authenticated
using (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1
    from public.parzellen_protokoll p
    join public.mitglied m on m.id = p.mitglied_id
    where p.id = parzellen_protokoll_ablesung.protokoll_id
      and coalesce(m.is_demo, false) = true
  )
)
with check (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1
    from public.parzellen_protokoll p
    join public.mitglied m on m.id = p.mitglied_id
    where p.id = parzellen_protokoll_ablesung.protokoll_id
      and coalesce(m.is_demo, false) = true
  )
);

create policy parzellen_begehung_feststellung_admin_full
on public.parzellen_begehung_feststellung
for all to authenticated
using (public.is_productive_admin_or_vorstand())
with check (public.is_productive_admin_or_vorstand());

create policy parzellen_begehung_feststellung_demo_admin_full
on public.parzellen_begehung_feststellung
for all to authenticated
using (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1
    from public.parzellen_protokoll p
    join public.mitglied m on m.id = p.mitglied_id
    where p.id = parzellen_begehung_feststellung.protokoll_id
      and coalesce(m.is_demo, false) = true
  )
)
with check (
  public.is_restricted_demo_admin_or_vorstand()
  and exists (
    select 1
    from public.parzellen_protokoll p
    join public.mitglied m on m.id = p.mitglied_id
    where p.id = parzellen_begehung_feststellung.protokoll_id
      and coalesce(m.is_demo, false) = true
  )
);
