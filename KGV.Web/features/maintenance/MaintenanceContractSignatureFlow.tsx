"use client";

import { useEffect, useState } from "react";
import { SignaturePad } from "../../components/signature/SignaturePad";
import { PreparedContractSignatorySelector } from "../../components/contracts/PreparedContractSignatorySelector";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { getCommunicationContractDraft } from "../../repositories/communication/communication-repository";
import { generateContract } from "../../services/contracts/contract-generation-service";
import { maintenanceContractPersistentSlots, maintenanceContractPositionLabel, preparedMaintenanceSignatureProgress } from "../../services/contracts/maintenance-contract-flow-service";
import { ensurePreparedContractSignaturePosition, listPreparedContractSignatures, savePreparedContractSignature, uploadPreparedContractSignature, type PreparedContractAuthorizedSignatory, type PreparedContractSignature } from "../../repositories/contracts/prepared-contract-signature-repository";
import { loadPreparedContractSignatories } from "../../services/contracts/prepared-contract-signatory-service";
import { getMaintenanceContractNumber } from "../../repositories/maintenance/maintenance-contract-repository";

type MaintenanceProcess = { id: number; status: string; revision: number; signatur_revision: number | null; mitglied_id: number; wartungsvertrag_id: number };

export function MaintenanceContractSignatureFlow({ club, session, assignmentId, preparedProcessId, principalMemberId, principalName, onClose }: { club: ClubContext; session: BrowserSession; assignmentId: number | null; preparedProcessId: number | null; principalMemberId: number; principalName: string; onClose: () => void }) {
  const [process, setProcess] = useState<MaintenanceProcess | null>(null);
  const [contractNumber, setContractNumber] = useState<number | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [positions, setPositions] = useState<PreparedContractSignature[]>([]);
  const [active, setActive] = useState<PreparedContractSignature | null>(null);
  const [drawing, setDrawing] = useState("");
  const [signatories, setSignatories] = useState<PreparedContractAuthorizedSignatory[]>([]);
  const [signatory, setSignatory] = useState<PreparedContractAuthorizedSignatory | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  const reload = async (id = process?.id, revision = process?.signatur_revision) => {
    if (!id || !revision) return;
    setPositions(await listPreparedContractSignatures(club, session, id, revision));
  };

  useEffect(() => () => { if (previewUrl) URL.revokeObjectURL(previewUrl); }, [previewUrl]);

  useEffect(() => {
    void (async () => {
      try {
        setBusy(true);
        const source = preparedProcessId
          ? await getCommunicationContractDraft(club, session, preparedProcessId)
          : assignmentId
            ? (await callSupabaseRpc<MaintenanceProcess[]>({ club, session }, "ensure_wartungsvertrag_prepared_contract", { p_zuordnung_id: assignmentId }))[0]
            : null;
        if (!source) throw new Error("Wartungsvertragsvorgang fehlt.");
        if (source.status === "erledigt") {
          setMessage("Der Wartungsvertrag ist bereits abgeschlossen.");
          return;
        }
        const sourceProcess = source as MaintenanceProcess;
        setProcess(sourceProcess);
        const existingNumber = await getMaintenanceContractNumber(club, session, sourceProcess.wartungsvertrag_id);
        if (sourceProcess.status !== "vorbereitet" && existingNumber === null) throw new Error("Für den signaturbereiten Wartungsvertrag fehlt die reservierte 90xxx.");
        setContractNumber(existingNumber);
        const preview = await generateContract(club, session, { action: "preview", type: "wartungsvertrag", member_id: sourceProcess.mitglied_id, start_date: new Date().toISOString().slice(0, 10), kommunikationsvorgang_id: sourceProcess.id });
        setPreviewUrl(preview.previewUrl ?? null);
        if (sourceProcess.status !== "vorbereitet") await prepareSigning(sourceProcess);
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "WV-Signaturvorgang konnte nicht geladen werden.");
      } finally {
        setBusy(false);
      }
    })();
  }, [assignmentId, preparedProcessId, club, session]);

  const prepareSigning = async (source: MaintenanceProcess) => {
    const started = source.status === "vorbereitet"
      ? (await callSupabaseRpc<MaintenanceProcess[]>({ club, session }, "start_prepared_contract_signature", { p_kommunikationsvorgang_id: source.id, p_expected_revision: source.revision, p_assigned_to: session.user.id }))[0]
      : source;
    if (!started?.signatur_revision) throw new Error("Der Wartungsvertrags-Signaturstart wurde nicht bestätigt.");
    setProcess(started);
    const number = await getMaintenanceContractNumber(club, session, started.wartungsvertrag_id);
    if (number === null) throw new Error("Der Signaturstart hat keine Wartungsvertragsnummer reserviert.");
    setContractNumber(number);
    for (const slot of maintenanceContractPersistentSlots(principalMemberId)) await ensurePreparedContractSignaturePosition(club, session, started.id, started.signatur_revision, slot);
    await reload(started.id, started.signatur_revision);
    const updatedPreview = await generateContract(club, session, { action: "preview", type: "wartungsvertrag", member_id: started.mitglied_id, start_date: new Date().toISOString().slice(0, 10), kommunikationsvorgang_id: started.id });
    setPreviewUrl(updatedPreview.previewUrl ?? null);
  };

  const startSigning = async (source: MaintenanceProcess) => {
    setBusy(true);
    setMessage("");
    try {
      await prepareSigning(source);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Der Signaturstart ist fehlgeschlagen.");
    } finally {
      setBusy(false);
    }
  };

  const open = async (position: PreparedContractSignature) => {
    if (position.status === "unterschrieben") return;
    setActive(position);
    setDrawing("");
    setSignatory(null);
    if (position.unterzeichner_typ === "vereinsunterzeichner" && process?.signatur_revision) {
      setSignatories(await loadPreparedContractSignatories(club, session, process.id, process.signatur_revision));
    }
  };

  const save = async () => {
    if (!active || !process?.signatur_revision || !drawing) return;
    try {
      setBusy(true);
      const excluded = positions.find((item) => item.signatur_schluessel !== active.signatur_schluessel && item.unterzeichner_typ === "vereinsunterzeichner" && item.status === "unterschrieben");
      if (active.unterzeichner_typ === "vereinsunterzeichner" && !signatory) throw new Error("Vereinsunterzeichner fehlt.");
      const upload = await uploadPreparedContractSignature(club, session, process.id, process.signatur_revision, active.signatur_schluessel, drawing);
      await savePreparedContractSignature(club, session, { id: process.id, revision: process.signatur_revision, key: active.signatur_schluessel, memberId: signatory?.mitglied_id ?? active.mitglied_id, signatory, storagePath: upload.storage_path, sha256: upload.sha256 });
      setActive(null);
      await reload();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Signatur konnte nicht gespeichert werden.");
    } finally {
      setBusy(false);
    }
  };

  const progress = preparedMaintenanceSignatureProgress(positions);
  const excluded = positions.find((position) => active && position.signatur_schluessel !== active.signatur_schluessel && position.unterzeichner_typ === "vereinsunterzeichner" && position.status === "unterschrieben")?.mitglied_id;

  return <section className="data-workspace">
    <h2>Wartungsvertrag unterschreiben</h2>
    <p>Vertragspartei: {principalName}</p>
    <p>{contractNumber === null ? "Noch keine Vertragsnummer; Reservierung bei Signaturstart." : `Vertrags-Nr. ${contractNumber}`}</p>
    {message && <p className="notice" role="status">{message}</p>}
    {previewUrl && <iframe title="Wartungsvertragsvorschau" src={previewUrl} className="contract-preview" />}
    {process?.status === "vorbereitet" && <button disabled={busy} onClick={() => void startSigning(process)}>Signatur starten – 90xxx reservieren</button>}
    {!!process?.signatur_revision && <>
      <p>{progress.signed} von {progress.required} Pflichtunterschriften</p>
      {positions.map((position) => <button key={position.id} disabled={busy || position.status === "unterschrieben"} onClick={() => void open(position)}>{position.status === "unterschrieben" ? "✓" : "○"} {maintenanceContractPositionLabel(position)} {position.unterzeichner_name_snapshot ?? ""} {position.vereinsfunktion_bezeichnung_snapshot ?? ""}</button>)}
      {active && <div>
        <h3>{maintenanceContractPositionLabel(active)}</h3>
        {active.signatur_schluessel === "mitglied" && <p>{principalName} unterschreibt als Vertragspartei.</p>}
        {active.unterzeichner_typ === "vereinsunterzeichner" && <PreparedContractSignatorySelector label="Vereinsunterzeichner" options={signatories} value={signatory} excludeMemberId={excluded} onChange={setSignatory} />}
        <SignaturePad label={maintenanceContractPositionLabel(active)} value={drawing} onChange={setDrawing} />
        <button disabled={busy || !drawing} onClick={() => void save()}>Speichern</button>
        <button onClick={() => { setActive(null); setDrawing(""); }}>Abbrechen</button>
      </div>}
      {progress.complete && <button disabled={busy} onClick={async () => {
        try {
          setBusy(true);
          await generateContract(club, session, { action: "finalize", type: "wartungsvertrag", member_id: principalMemberId, start_date: new Date().toISOString().slice(0, 10), kommunikationsvorgang_id: process.id });
          setMessage("Wartungsvertrag abgeschlossen.");
          await reload();
        } catch (error) {
          setMessage(error instanceof Error ? error.message : "Finalisierung fehlgeschlagen; Signaturen bleiben erhalten.");
          await reload();
        } finally {
          setBusy(false);
        }
      }}>Wartungsvertrag finalisieren</button>}
    </>}
    <button onClick={onClose}>Schließen</button>
  </section>;
}
