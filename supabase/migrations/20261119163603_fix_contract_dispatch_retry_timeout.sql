-- Repair block 02: a business-level claim rejection must not use SQLSTATE
-- 40001. PostgREST 14 retries serialization failures, so a missing or otherwise
-- non-claimable dispatch kept running until the HTTP request timed out.
create or replace function public.claim_contract_dispatch(p_versandinstanz_id bigint)
returns table(id bigint, betreff text, mailtext text)
language plpgsql
security definer
set search_path = public, pg_temp
as $$
begin
  if not public.kann_vertragsversand_ausfuehren() then
    raise exception 'Keine Berechtigung für Vertragsversand' using errcode = '42501';
  end if;

  update public.versandinstanz z
  set status = 'wird_versendet',
      beansprucht_am = now(),
      beansprucht_von = auth.uid(),
      letzter_fehler = null
  from public.kommunikationsvorgang v
  where z.id = p_versandinstanz_id
    and v.id = z.kommunikationsvorgang_id
    and (auth.role() = 'service_role'
         or (v.zugewiesen_an is not null and v.zugewiesen_an = auth.uid()))
    and (z.status in ('bereit', 'fehler', 'teilweise_fehlerhaft')
         or (z.status = 'wird_versendet' and z.beansprucht_am < now() - interval '15 minutes'))
    and exists (
      select 1
      from public.versandinstanz_empfaenger_snapshot e
      where e.versandinstanz_id = p_versandinstanz_id
        and e.status in ('bereit', 'fehler')
    )
  returning z.id, z.betreff_snapshot, z.text_snapshot into id, betreff, mailtext;

  if not found then
    raise sqlstate 'PT403' using
      message = 'Versandinstanz ist nicht beanspruchbar';
  end if;

  return next;
end;
$$;

revoke all on function public.claim_contract_dispatch(bigint) from public, anon;
grant execute on function public.claim_contract_dispatch(bigint) to authenticated, service_role;

comment on function public.claim_contract_dispatch(bigint) is
  'Claims an assigned contract dispatch atomically; non-claimable rows fail once with non-retryable HTTP 403 (PT403).';
