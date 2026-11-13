-- AP-04.4 correction: the signed document must visibly contain the frozen
-- change, and completion requires the complete lease signature plan.

create or replace function public.bereite_signierte_parzellen_vertragsaenderung_vor(
  p_vorgaenger_fassung_id bigint, p_flaeche_qm numeric, p_hat_strom boolean,
  p_hat_wasser boolean, p_begruendung text
) returns table(pacht_vertragsaenderung_id bigint, kommunikationsvorgang_id bigint)
language plpgsql security definer set search_path=public,pg_temp as $$
declare v_reason text:=nullif(btrim(p_begruendung),''); v_relation bigint; v_change bigint; v_process bigint; v_snapshot jsonb; v_hash text; v_clause text;
begin
 if auth.uid() is null or not public.has_effective_permission(524288) then raise exception 'Keine Berechtigung für Vertragsänderungen.' using errcode='42501'; end if;
 if v_reason is null or p_flaeche_qm is null or p_flaeche_qm<0 or p_hat_strom is null or p_hat_wasser is null then raise exception 'Vollständiger Änderungsstand und Begründung erforderlich.' using errcode='22023'; end if;
 select f.pachtverhaeltnis_id into v_relation from public.pachtvertragsfassung f join public.pachtverhaeltnis p on p.id=f.pachtverhaeltnis_id
 where f.id=p_vorgaenger_fassung_id and p.beendet_am is null and f.nicht_mehr_massgeblich_am is null and not exists(select 1 from public.pachtvertragsfassung n where n.vorgaenger_fassung_id=f.id) for update;
 if not found then raise exception 'Die maßgebliche laufende Vertragsfassung fehlt.' using errcode='23514'; end if;
 select kommunikationsvorgang_id into v_process from public.prepare_pachtvertrag_ersatz(p_vorgaenger_fassung_id,auth.uid());
 if exists(select 1 from public.pacht_vertragsaenderung where kommunikationsvorgang_id=v_process) then select id into v_change from public.pacht_vertragsaenderung where kommunikationsvorgang_id=v_process; return query select v_change,v_process; return; end if;
 v_snapshot:=jsonb_build_object('flaeche_qm',p_flaeche_qm,'hat_strom',p_hat_strom,'hat_wasser',p_hat_wasser);
 v_hash:=encode(digest(convert_to(v_snapshot::text,'utf8'),'sha256'),'hex');
 v_clause:=format('Vertragsänderung (verbindlich): Fläche %s m²; Strom %s; Wasser %s. Begründung: %s',p_flaeche_qm,case when p_hat_strom then 'ja' else 'nein' end,case when p_hat_wasser then 'ja' else 'nein' end,v_reason);
 insert into public.pacht_vertragsaenderung(pachtverhaeltnis_id,vorgaenger_fassung_id,kommunikationsvorgang_id,aenderungsstand,aenderungs_hash,begruendung,erstellt_von)
 values(v_relation,p_vorgaenger_fassung_id,v_process,v_snapshot,v_hash,v_reason,auth.uid()) returning id into v_change;
 update public.kommunikationsvorgang set entwurf_daten=jsonb_set(jsonb_set(jsonb_set(entwurf_daten,'{draft,p11_contract_change_id}',to_jsonb(v_change),true),'{draft,p11_contract_change_hash}',to_jsonb(v_hash),true),'{draft,additionalAgreements}',to_jsonb(concat_ws(E'\n',nullif(entwurf_daten #>> '{draft,additionalAgreements}',''),v_clause)),true)
 where id=v_process and status='vorbereitet' and signatur_gestartet_am is null;
 if not found then raise exception 'Der Änderungsentwurf konnte nicht eingefroren werden.' using errcode='40001'; end if;
 return query select v_change,v_process;
end $$;

create or replace function public.bind_pacht_vertragsaenderung_an_fassung()
returns trigger language plpgsql security definer set search_path=public,pg_temp as $$
declare v_change public.pacht_vertragsaenderung%rowtype; v_process public.kommunikationsvorgang%rowtype; v_expected text[];
begin
 select c.* into v_change from public.pacht_vertragsaenderung c join public.kommunikationsvorgang k on k.id=c.kommunikationsvorgang_id join public.dokument d on d.source_kommunikationsvorgang_id=k.id where d.id=new.dokument_id for update of c;
 if not found then return new; end if;
 select k.* into v_process from public.kommunikationsvorgang k where k.id=v_change.kommunikationsvorgang_id for update;
 v_expected:=array['lease.member','vorstand_1','vorstand_2'];
 if coalesce((v_process.entwurf_daten #>> '{draft,includeSecondaryMember}')::boolean,false) then v_expected:=array_append(v_expected,'lease.second_party'); end if;
 if new.pachtverhaeltnis_id is distinct from v_change.pachtverhaeltnis_id or new.vorgaenger_fassung_id is distinct from v_change.vorgaenger_fassung_id
    or v_process.status<>'letzte_pruefung' or v_process.signatur_revision is null or v_process.signatur_revision<>v_process.revision
    or v_process.entwurf_daten #>> '{draft,p11_contract_change_id}' is distinct from v_change.id::text
    or v_process.entwurf_daten #>> '{draft,p11_contract_change_hash}' is distinct from v_change.aenderungs_hash
    or not (v_process.entwurf_daten #>> '{draft,additionalAgreements}' like ('%' || v_change.begruendung || '%'))
    or (select count(distinct s.signatur_schluessel) from public.prepared_contract_signatur s where s.kommunikationsvorgang_id=v_process.id and s.signatur_revision=v_process.signatur_revision and s.pflicht and s.status='unterschrieben' and s.signatur_schluessel=any(v_expected))<>cardinality(v_expected)
    or exists(select 1 from unnest(v_expected) key where not exists(select 1 from public.prepared_contract_signatur s where s.kommunikationsvorgang_id=v_process.id and s.signatur_revision=v_process.signatur_revision and s.signatur_schluessel=key and s.pflicht and s.status='unterschrieben'))
 then raise exception 'Die Vertragsfassung autorisiert keinen vollständigen eingefrorenen Änderungsstand.' using errcode='23514'; end if;
 update public.pacht_vertragsaenderung set pachtvertragsfassung_id=new.id,signiert_am=now() where id=v_change.id;
 return new;
end $$;
revoke all on function public.bind_pacht_vertragsaenderung_an_fassung(), public.bereite_signierte_parzellen_vertragsaenderung_vor(bigint,numeric,boolean,boolean,text) from public,anon;
grant execute on function public.bereite_signierte_parzellen_vertragsaenderung_vor(bigint,numeric,boolean,boolean,text) to authenticated;
