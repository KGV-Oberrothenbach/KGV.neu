-- Fix ambiguous column reference in the authenticated Impressum SELECT policy.
-- The original policy resolved the unqualified "id" inside the subquery
-- to vereinsfunktion_verwendung.id instead of vereinsfunktion.id.

drop policy if exists vereinsfunktion_impressum_select_authenticated
on public.vereinsfunktion;
create policy vereinsfunktion_impressum_select_authenticated
on public.vereinsfunktion
for select
to authenticated
using (
    aktiv
    and id in (
        select v.vereinsfunktion_id
        from public.vereinsfunktion_verwendung v
        where v.verwendungszweck = 'impressum'
    )
);
