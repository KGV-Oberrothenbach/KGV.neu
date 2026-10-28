import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");

const generator = await read("supabase/functions/kgv-generate-contract/index.ts");
const webPage = await read("KGV.Web/app/page.tsx");
const parcelWorkspaces = await read("KGV.Web/app/parcel-workspaces.tsx");
const memberParcelDetail = await read("KGV.Web/features/parcels/MemberParcelDetail.tsx");
const communicationCenter = await read("KGV.Web/features/communication/CommunicationCenter.tsx");
const contractDispatch = await read("KGV.Web/features/communication/ContractDispatchPanel.tsx");
const historicalUpload = await read("KGV.Web/features/documents/HistoricalDocumentUploadDialog.tsx");
const pendingPhotos = await read("KGV.Web/features/readings/PendingPhotoUploads.tsx");
const mauiParcels = await read("KGV.Maui/Pages/ParzellenPage.cs");
const mauiMemberParcels = await read("KGV.Maui/Pages/MemberParzellenDetailPage.cs");
const mauiGardenAssign = await read("KGV.Maui/Pages/MemberGardenAssignPage.cs");
const mauiLease = await read("KGV.Maui/Pages/PachtvertragDialogPage.cs");
const mauiDocuments = await read("KGV.Maui/Pages/DokumentePage.xaml.cs");
const mauiFinalReview = await read("KGV.Maui/Pages/PreparedFinalDocumentReviewFlow.cs");
const coreLeaseFactory = await read("KGV.Core/Utilities/PachtvertragDokumentFactory.cs");

test("Alt-Signierpfad verwendet einen neutralen sichtbaren Dateinamen", () => {
  assert.match(generator, /"Pachtvertrag-signiert\.pdf"/);
  assert.doesNotMatch(generator, /Pachtvertrag-signiert-\$\{id\}/);
});

test("Pachtvertrag-PDF fällt nicht auf die Parzellen-ID zurück", () => {
  assert.match(generator, /parzelle_nr: safe\(parcel\.garten_nr\) \|\| "nicht verfügbar"/);
  assert.doesNotMatch(generator, /parzelle_nr:[^\n]*parcel\.id/);
  assert.match(coreLeaseFactory, /ParzelleNummer = string\.IsNullOrWhiteSpace\(parzelle\.GartenNr\) \? "nicht verfügbar"/);
  assert.doesNotMatch(coreLeaseFactory, /ParzelleNummer[^\n]*parzelle\.Id/);
});

test("Parzellenstammdaten zeigen keine Parzellen-ID", () => {
  assert.doesNotMatch(parcelWorkspaces, /Parzellen-ID|#\{draft\.id\}/);
});

test("Mitgliedsparzellen nutzen bei fehlender Gartennummer einen neutralen Text", () => {
  assert.match(parcelWorkspaces, /item\.parcel\?\.garten_nr \?\? "nicht verfügbar"/);
  assert.doesNotMatch(parcelWorkspaces, /#\$\{item\.assignment\.parzelle_id\}/);
});

test("Protokollhistorie nutzt keine Parzellen-ID als Ersatznummer", () => {
  assert.match(parcelWorkspaces, /\?\.garten_nr \?\? "nicht verfügbar"/);
  assert.doesNotMatch(parcelWorkspaces, /#\$\{item\.parzelle_id\}/);
});

test("Parzellendetail und Ablesehistorie nutzen neutrale Fallbacks", () => {
  assert.equal((memberParcelDetail.match(/parcel\?\.garten_nr \?\? "nicht verfügbar"/g) ?? []).length, 2);
  assert.doesNotMatch(memberParcelDetail, /#\$\{assignment\.parzelle_id\}/);
});

test("aktiver Parzellenfilter zeigt die Gartendarstellung", () => {
  assert.match(webPage, /Parzellenfilter aktiv: \{parcelName\(selectedParcelId\)\}/);
  assert.doesNotMatch(webPage, /Parzellenfilter aktiv: #\{selectedParcelId\}/);
});

test("Mitgliedsfallback verwendet Mitgliedsnummer oder neutralen Text", () => {
  assert.match(webPage, /Mgl\.-Nr\. \$\{member\.mitgliedsnummer\}/);
  assert.doesNotMatch(webPage, /Mitglied #\$\{id\}/);
  assert.doesNotMatch(parcelWorkspaces, /Mitglied #\$\{member\.id\}/);
});

test("Kommunikationsdetails zeigen keine Vorgangs- oder Revisions-IDs", () => {
  assert.doesNotMatch(communicationCenter, /Vorgangs-ID|Signatur-Revision|label="Revision"/);
  assert.match(communicationCenter, /item\.final_dokument_id \? "Vorhanden" : "–"/);
  assert.doesNotMatch(communicationCenter, /final_dokument_id\?\.toString/);
});

test("Neuversand zeigt keine technische Vorgänger-ID", () => {
  assert.match(contractDispatch, /erneuter Versand<\//);
  assert.doesNotMatch(contractDispatch, /erneuter Versand von #/);
});

test("historischer Upload zeigt keine Dokument-ID", () => {
  assert.match(historicalUpload, /Dokument bereits hochgeladen/);
  assert.doesNotMatch(historicalUpload, /Bereits hochgeladenes Dokument:[^\n]*documentId/);
});

test("offene Foto-Uploads zeigen keine Ablesungs-ID", () => {
  assert.match(pendingPhotos, /· Ablesung<\/span>/);
  assert.doesNotMatch(pendingPhotos, /Ablesung #\{item\.readingId\}/);
});

test("MAUI-Parzellendetails enthalten keine technische ID-Zeile", () => {
  assert.doesNotMatch(mauiParcels, /CreateValueLabel\("ID"/);
  assert.doesNotMatch(mauiMemberParcels, /CreateValueLabel\("ID"/);
});

test("MAUI-Parzellenauswahlen fallen neutral statt auf IDs zurück", () => {
  assert.match(mauiGardenAssign, /\? "nicht verfügbar" : parzelle\.GartenNr\.Trim\(\)/);
  assert.match(mauiLease, /\? "nicht verfügbar" : parzelle\.GartenNr\.Trim\(\)/);
  assert.match(mauiDocuments, /detail\?\.DisplayName \?\? "Parzelle"/);
  assert.doesNotMatch(`${mauiGardenAssign}\n${mauiLease}\n${mauiDocuments}`, /Parzelle #|\$"#\{parzelle\.Id\}"/);
});

test("MAUI-Finalprüfung zeigt keine Dokument-ID", () => {
  assert.match(mauiFinalReview, /"Finaldokument wurde geladen\."/);
  assert.doesNotMatch(mauiFinalReview, /Finaldokument #/);
});

test("bekannte Wartungsvertragsansichten bleiben fachnummernbasiert", async () => {
  const [continuation, assignment] = await Promise.all([
    read("KGV.Web/features/maintenance/MaintenanceContinuationPanel.tsx"),
    read("KGV.Web/services/maintenance/maintenance-assignment-service.ts"),
  ]);
  assert.match(continuation, /Wartungsvertrag Nr\. \{detail\.vertragsnummer\}/);
  assert.doesNotMatch(`${continuation}\n${assignment}`, /Wartungsvertrag #|Bestehender Wartungsvertrag #/);
});
