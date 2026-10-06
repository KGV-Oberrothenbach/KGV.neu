"use client";

import { useEffect, useRef, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { emptyLeaseContractDraft, listEligibleLeaseParcels, loadLeaseContractData, prepareLeaseContractRequest, type LeaseContractData, type LeaseContractDraft } from "../../services/contracts/lease-service";
import { type LeaseParcel } from "../../repositories/parcels/parcel-repository";

const localToday = () => { const today = new Date(); return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`; };
const memberName = (member: { vorname: string | null; name: string | null; id: number }) => [member.vorname, member.name].filter(Boolean).join(" ") || `Mitglied #${member.id}`;

export function LeaseContractFlow({ session, memberId, onChangeDocumentType }: { session: BrowserSession; memberId: number; onChangeDocumentType: (type: "mitgliedsantrag" | "mitgliedsvertrag") => void }) {
  const [draft, setDraft] = useState<LeaseContractDraft>(() => emptyLeaseContractDraft(localToday()));
  const [parcels, setParcels] = useState<LeaseParcel[]>([]);
  const [data, setData] = useState<LeaseContractData | null>(null);
  const [message, setMessage] = useState("");
  const secondaryMemberChoiceContext = useRef<string | null>(null);

  useEffect(() => {
    let active = true;
    listEligibleLeaseParcels(session, memberId, draft.startDate).then((items) => {
      if (!active) return;
      setParcels(items);
      setDraft((current) => {
        const parcelId = current.parcelId && items.some((item) => item.id === current.parcelId) ? current.parcelId : items.length === 1 ? items[0].id : null;
        return { ...current, parcelId, includeSecondaryMember: false };
      });
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Geeignete Parzellen konnten nicht geladen werden."));
    return () => { active = false; };
  }, [session, memberId, draft.startDate]);

  useEffect(() => {
    if (!draft.parcelId) return;
    let active = true;
    loadLeaseContractData(session, memberId, draft.parcelId, draft.startDate).then((value) => {
      if (!active) return;
      setData(value);
      const context = `${memberId}:${value.parcel.id}:${value.startDate}`;
      if (secondaryMemberChoiceContext.current !== context) { secondaryMemberChoiceContext.current = context; setDraft((current) => ({ ...current, includeSecondaryMember: value.isMinor ? false : Boolean(value.secondaryMember) })); }
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Pachtvertragsbasis konnte nicht geladen werden."));
    return () => { active = false; };
  }, [session, memberId, draft.parcelId, draft.startDate]);

  const statusMessage = data?.status === "signed" ? "Ein signierter Pachtvertrag ist bereits vorhanden." : data?.status === "unsigned" ? "Ein unsignierter Pachtvertrag ist bereits vorhanden." : "";
  const blocked = Boolean(statusMessage);
  const prepared = data && !blocked ? (() => { try { return prepareLeaseContractRequest(data, draft); } catch { return null; } })() : null;

  return <section className="contract-composer"><div className="editor-actions"><button className="secondary-action" onClick={() => onChangeDocumentType("mitgliedsantrag")}>Mitgliedsantrag</button><button className="secondary-action" onClick={() => onChangeDocumentType("mitgliedsvertrag")}>Mitgliedsvertrag</button></div><h3>Pachtvertrag</h3>{(message || statusMessage) && <p className="notice" role="status">{message || statusMessage}</p>}<fieldset><label>Vertragsbeginn<input type="date" value={draft.startDate} onChange={(event) => { secondaryMemberChoiceContext.current = null; setData(null); setDraft((current) => ({ ...current, startDate: event.target.value, parcelId: null, includeSecondaryMember: false })); }} /></label><label>Parzelle<select value={draft.parcelId ?? ""} onChange={(event) => { secondaryMemberChoiceContext.current = null; setData(null); setDraft((current) => ({ ...current, parcelId: Number(event.target.value) || null, includeSecondaryMember: false })); }}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr ?? `#${parcel.id}`} – {parcel.Anlage ?? "Ohne Anlage"}</option>)}</select></label></fieldset>{data && <><section className="detail-panel"><h4>Pachtbasis</h4><p><strong>{memberName(data.member)}</strong> · Mitglied #{data.member.id}</p><p>Garten {data.parcel.garten_nr ?? data.parcel.id} · {data.parcel.Anlage ?? "Ohne Anlage"}</p><p>Fläche: {data.parcel.flaeche_qm} m² · Pachtpreis: {data.pachtPerSqm.toFixed(2)} €/m²</p><p>Jahrespacht: {data.annualRent.toFixed(2)} € · Laufendes Jahr: {data.runningYearRent.toFixed(2)} €</p></section>{!blocked && <fieldset><legend>Altvertrag</legend><label><input type="radio" checked={draft.hasPreviousContract === true} onChange={() => setDraft((current) => ({ ...current, hasPreviousContract: true, previousContractDate: current.previousContractDate ?? localToday() }))} /> Ja</label><label><input type="radio" checked={draft.hasPreviousContract === false} onChange={() => setDraft((current) => ({ ...current, hasPreviousContract: false, previousContractDate: null }))} /> Nein</label>{draft.hasPreviousContract && <label>Datum des Altvertrags<input type="date" value={draft.previousContractDate ?? ""} onChange={(event) => setDraft((current) => ({ ...current, previousContractDate: event.target.value || null }))} /></label>}{data.isMinor && data.legalRepresentative && <div><strong>Gesetzlicher Vertreter</strong><p>{memberName(data.legalRepresentative)} · Mitglied #{data.legalRepresentative.id}</p><p className="detail-hint">Aus dem signierten Mitgliedsantrag übernommen. Änderungen erfolgen in den Mitgliedsdaten, nicht im Pachtvertrag.</p></div>}{!data.isMinor && data.secondaryMember && <label className="check"><input type="checkbox" checked={draft.includeSecondaryMember} onChange={(event) => setDraft((current) => ({ ...current, includeSecondaryMember: event.target.checked }))} /> {memberName(data.secondaryMember)} als Pächter/in 2 aufnehmen und unterschreiben lassen.</label>}</fieldset>}<p className="detail-hint">{prepared ? "Pachtvertragsentwurf ist für G4.6 vorbereitet." : "Vorschau, Signaturen und Finalisierung folgen mit G4.6."}</p></>}</section>;
}
