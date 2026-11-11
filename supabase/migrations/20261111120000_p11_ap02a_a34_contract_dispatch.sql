-- P11 AP-02A / A3.4: contract dispatch is a communication-workflow action.
-- The dedicated function assignment was an organisational routing mechanism, but
-- it did not honour individual grants or revocations of the authoritative right.
create or replace function public.kann_vertragsversand_ausfuehren()
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
  select auth.role() = 'service_role'
      or (auth.uid() is not null and public.has_effective_permission(1073741824));
$$;

-- Older installations do not have the optional Vereinskonfiguration relation.
-- Keep dispatch operational with the established legal fallback while using that
-- relation when it is present; dynamic SQL avoids a hard dependency on it.
create or replace function public.vertragsversand_mailtext_snapshot()
returns text
language plpgsql
stable
security definer
set search_path = public, pg_temp
as $$
declare
  v_vereinsname text;
  v_strasse text;
  v_plz text;
  v_ort text;
  v_registerangabe text;
  v_standard_email text;
  v_vorstand text;
begin
  if to_regclass('public.vereinskonfiguration') is not null then
    execute $query$
      select vereinsname, strasse, plz, ort, registerangabe, standard_email
      from public.vereinskonfiguration
      where aktiv
      order by updated_at desc, id desc
      limit 1
    $query$
    into v_vereinsname, v_strasse, v_plz, v_ort, v_registerangabe, v_standard_email;
  end if;

  select string_agg(
    concat_ws(' ', bezeichnung, concat_ws(' ', vorname, nachname)),
    E'\n'
    order by ist_hauptkontakt desc, gruppen_sort_order, besetzung_id
  )
  into v_vorstand
  from public.impressum_kontakte();

  return E'Guten Tag,\n\nim Anhang erhalten Sie wichtige Unterlagen zu Ihrer Mitgliedschaft bzw. Ihrem Vertragsverhältnis im Kleingartenverein.\n\nBitte beachten Sie: Diese E-Mail und ihre Anhänge können vertrauliche und personenbezogene Informationen enthalten und sind ausschließlich für die vorgesehenen Empfänger bestimmt. Sollten Sie diese E-Mail irrtümlich erhalten haben, informieren Sie uns bitte und löschen Sie die Nachricht sowie die Anhänge.\n\nMit freundlichen Grüßen\n\n'
    || coalesce(nullif(v_vereinsname, ''), 'KGV Oberrothenbach e.V.')
    || E'\n' || coalesce(nullif(concat_ws(', ', v_strasse, concat_ws(' ', v_plz, v_ort)), ''), '')
    || case when nullif(v_vorstand, '') is null then '' else E'\nVertretungsberechtigt:\n' || v_vorstand end
    || case when nullif(v_registerangabe, '') is null then '' else E'\n' || v_registerangabe end
    || case when nullif(v_standard_email, '') is null then '' else E'\n' || v_standard_email end;
end;
$$;

create or replace function public.prepare_contract_dispatch(
  p_kommunikationsvorgang_id bigint,
  p_erneuter_versand_von_id bigint default null
)
returns bigint
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v public.kommunikationsvorgang%rowtype;
  v_id bigint;
begin
  if not public.kann_vertragsversand_ausfuehren() then
    raise exception 'Keine Berechtigung für Vertragsversand' using errcode = '42501';
  end if;

  select * into v
  from public.kommunikationsvorgang
  where id = p_kommunikationsvorgang_id
  for update;

  if not found then
    raise exception 'Unbekannter Vertragsvorgang' using errcode = 'P0002';
  end if;

  if auth.role() <> 'service_role'
     and (v.zugewiesen_an is null or v.zugewiesen_an is distinct from auth.uid()) then
    raise exception 'Der Vertragsvorgang ist nicht dem angemeldeten Bearbeiter zugewiesen' using errcode = '42501';
  end if;

  if p_erneuter_versand_von_id is null then
    select z.id into v_id
    from public.versandinstanz z
    where z.kommunikationsvorgang_id = v.id
      and z.revision = coalesce(v.signatur_revision, v.revision)
      and z.erneuter_versand_von_id is null
    order by z.id
    limit 1
    for update;
  else
    select z.id into v_id
    from public.versandinstanz z
    where z.kommunikationsvorgang_id = v.id
      and z.revision = coalesce(v.signatur_revision, v.revision)
      and z.erneuter_versand_von_id = p_erneuter_versand_von_id
      and z.status <> 'versendet'
    order by z.id desc
    limit 1
    for update;
  end if;

  if found then
    return v_id;
  end if;

  return public.prepare_contract_dispatch_vor_phase12(
    p_kommunikationsvorgang_id,
    p_erneuter_versand_von_id
  );
end;
$$;

create or replace function public.create_contract_redispatch(p_versandinstanz_id bigint)
returns bigint
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  z public.versandinstanz%rowtype;
begin
  if not public.kann_vertragsversand_ausfuehren() then
    raise exception 'Keine Berechtigung für Vertragsversand' using errcode = '42501';
  end if;

  select * into z
  from public.versandinstanz
  where id = p_versandinstanz_id
    and status = 'versendet'
  for key share;

  if not found then
    raise exception 'Erneut senden ist nur nach erfolgreichem Versand möglich' using errcode = '23514';
  end if;

  return public.prepare_contract_dispatch(z.kommunikationsvorgang_id, z.id);
end;
$$;

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
    raise exception 'Versandinstanz ist nicht beanspruchbar' using errcode = '40001';
  end if;

  return next;
end;
$$;

revoke all on function public.kann_vertragsversand_ausfuehren() from public, anon;
revoke all on function public.prepare_contract_dispatch(bigint, bigint), public.create_contract_redispatch(bigint), public.claim_contract_dispatch(bigint) from public, anon;
grant execute on function public.kann_vertragsversand_ausfuehren() to authenticated, service_role;
grant execute on function public.prepare_contract_dispatch(bigint, bigint), public.create_contract_redispatch(bigint), public.claim_contract_dispatch(bigint) to authenticated, service_role;
