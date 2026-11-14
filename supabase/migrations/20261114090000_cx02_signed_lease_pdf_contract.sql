-- CX-02: freeze the complete old/new parcel state visibly in the signed PDF.
-- The already-published immutable v9 catalog entry remains unchanged.

create or replace function public.bereite_signierte_parzellen_vertragsaenderung_vor(
 p_vorgaenger_fassung_id bigint,p_flaeche_qm numeric,p_hat_strom boolean,p_hat_wasser boolean,p_begruendung text)
returns table(pacht_vertragsaenderung_id bigint,kommunikationsvorgang_id bigint)
language plpgsql security definer set search_path=public,pg_temp as $$
declare
 v_reason text:=nullif(btrim(p_begruendung),''); v_relation bigint; v_process bigint;
 v_change public.pacht_vertragsaenderung%rowtype; v_snapshot jsonb; v_hash text; v_clause text;
 v_old_area numeric; v_old_power boolean; v_old_water boolean;
begin
 if auth.uid() is null or not public.has_effective_permission(524288) then raise exception 'Keine Berechtigung für Vertragsänderungen.' using errcode='42501'; end if;
 if v_reason is null or p_flaeche_qm is null or p_flaeche_qm<0 or p_hat_strom is null or p_hat_wasser is null then raise exception 'Vollständiger Änderungsstand und Begründung erforderlich.' using errcode='22023'; end if;
 select f.pachtverhaeltnis_id,z.flaeche_qm,z.hat_strom,z.hat_wasser
   into v_relation,v_old_area,v_old_power,v_old_water
   from public.pachtvertragsfassung f
   join public.pachtverhaeltnis p on p.id=f.pachtverhaeltnis_id
   join public.parzelle z on z.id=p.parzelle_id
  where f.id=p_vorgaenger_fassung_id and p.beendet_am is null and f.nicht_mehr_massgeblich_am is null
    and not exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=f.id)
  for update of f,p,z;
 if not found then raise exception 'Die maßgebliche laufende Vertragsfassung fehlt.' using errcode='23514'; end if;
 v_snapshot:=jsonb_build_object('flaeche_qm',p_flaeche_qm,'hat_strom',p_hat_strom,'hat_wasser',p_hat_wasser);
 v_hash:=encode(pg_catalog.sha256(convert_to(v_snapshot::text,'utf8')),'hex');
 select prepared.kommunikationsvorgang_id into v_process from public.prepare_pachtvertrag_ersatz(p_vorgaenger_fassung_id,auth.uid()) as prepared;
 select c.* into v_change from public.pacht_vertragsaenderung as c where c.kommunikationsvorgang_id=v_process for update;
 if found then
   if v_change.aenderungsstand is distinct from v_snapshot or v_change.begruendung is distinct from v_reason or v_change.aenderungs_hash is distinct from v_hash then raise exception 'Abweichende Wiederholung einer Vertragsänderung ist nicht zulässig.' using errcode='23505'; end if;
   return query select v_change.id,v_process; return;
 end if;
 v_clause:=format(
   'Bisheriger Vertragsstand: Fläche %s m²; Strom %s; Wasser %s.%s%s',
   v_old_area,
   case when v_old_power then 'ja' else 'nein' end,
   case when v_old_water then 'ja' else 'nein' end,
   E'\n',
   public.p11_contract_change_clause(v_snapshot,v_reason)
 );
 insert into public.pacht_vertragsaenderung(pachtverhaeltnis_id,vorgaenger_fassung_id,kommunikationsvorgang_id,aenderungsstand,aenderungs_hash,begruendung,erstellt_von) values(v_relation,p_vorgaenger_fassung_id,v_process,v_snapshot,v_hash,v_reason,auth.uid()) returning * into v_change;
 update public.kommunikationsvorgang set entwurf_daten=jsonb_set(jsonb_set(jsonb_set(entwurf_daten,'{draft,p11_contract_change_id}',to_jsonb(v_change.id),true),'{draft,p11_contract_change_hash}',to_jsonb(v_hash),true),'{draft,additionalAgreements}',to_jsonb(concat_ws(E'\n',nullif(entwurf_daten #>> '{draft,additionalAgreements}',''),v_clause)),true) where id=v_process and status='vorbereitet' and signatur_gestartet_am is null;
 if not found then raise exception 'Der Änderungsentwurf konnte nicht eingefroren werden.' using errcode='40001'; end if;
 return query select v_change.id,v_process;
end $$;

revoke all on function public.bereite_signierte_parzellen_vertragsaenderung_vor(bigint,numeric,boolean,boolean,text) from public,anon;
grant execute on function public.bereite_signierte_parzellen_vertragsaenderung_vor(bigint,numeric,boolean,boolean,text) to authenticated;
