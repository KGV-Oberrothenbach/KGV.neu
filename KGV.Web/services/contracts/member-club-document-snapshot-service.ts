import type { MemberClubDocumentStatus } from "../../models/club-documents/member-club-document-status";

export type PreparedContractDocumentType = "mitgliedsantrag" | "pachtvertrag";
export type MemberClubDocumentType = "satzung" | "kleingartenordnung" | "beitragsordnung" | "arbeitsordnung";
export type MemberClubDocumentSnapshotItem = { dokumenttyp: MemberClubDocumentType; vereinsdokument_fassung_id: number | null; fassung: string | null; gueltig_ab: string | null; gueltig_bis?: string | null; bereitstellungsart: "digital" | "papier"; bereitgestellt_am: string; bereitstellung_id: number | null };
/** The sole contract-type rule. Do not derive this from document names or UI selections. */
const required: Record<PreparedContractDocumentType, readonly MemberClubDocumentType[]> = { mitgliedsantrag: ["satzung"], pachtvertrag: ["kleingartenordnung", "beitragsordnung", "arbeitsordnung"] };
const labels: Record<MemberClubDocumentSnapshotItem["dokumenttyp"], string> = { satzung: "Satzung", kleingartenordnung: "Kleingartenordnung", beitragsordnung: "Beitragsordnung", arbeitsordnung: "Arbeitsordnung" };
export const requiredClubDocumentsFor = (documentType: PreparedContractDocumentType) => required[documentType];

export function createMemberClubDocumentSnapshot(status: MemberClubDocumentStatus, documentType: PreparedContractDocumentType): MemberClubDocumentSnapshotItem[] {
  return requiredClubDocumentsFor(documentType).map((dokumenttyp) => {
    const item = status.documents.find((candidate) => candidate.dokumenttyp_code === dokumenttyp);
    if (!item || item.status === "keine_aktuelle_fassung") throw new Error("Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt.");
    if (item.status !== "bereitgestellt" || !item.vereinsdokument_fassung_id || !item.bereitstellung_id || !item.fassung || !item.gueltig_ab || !item.bereitstellungsart || !item.bereitgestellt_am) throw new Error("Mindestens eine aktuelle Vereinsunterlage fehlt beim Mitglied.");
    return { dokumenttyp, vereinsdokument_fassung_id: item.vereinsdokument_fassung_id, fassung: item.fassung, gueltig_ab: item.gueltig_ab, gueltig_bis: item.gueltig_bis, bereitstellungsart: item.bereitstellungsart as "digital" | "papier", bereitgestellt_am: item.bereitgestellt_am, bereitstellung_id: item.bereitstellung_id };
  });
}
export const isCompleteMemberClubDocumentSnapshot = (snapshot: MemberClubDocumentSnapshotItem[] | undefined, documentType: PreparedContractDocumentType): snapshot is MemberClubDocumentSnapshotItem[] => !!snapshot && snapshot.length === requiredClubDocumentsFor(documentType).length && snapshot.every((item, index) => item.dokumenttyp === requiredClubDocumentsFor(documentType)[index] && !!item.bereitgestellt_am && (item.bereitstellungsart === "papier" || (!!item.vereinsdokument_fassung_id && !!item.bereitstellung_id && !!item.fassung && !!item.gueltig_ab)));
/** Compares the persisted evidence, not its JSON representation. JSONB may reorder keys and omit undefined values. */
export const memberClubDocumentSnapshotsEqual = (left: MemberClubDocumentSnapshotItem[] | undefined, right: MemberClubDocumentSnapshotItem[] | undefined, documentType: PreparedContractDocumentType) =>
  isCompleteMemberClubDocumentSnapshot(left, documentType) && isCompleteMemberClubDocumentSnapshot(right, documentType) && left.every((item, index) => {
    const other = right[index];
    return item.dokumenttyp === other.dokumenttyp &&
      item.vereinsdokument_fassung_id === other.vereinsdokument_fassung_id &&
      item.fassung === other.fassung &&
      item.gueltig_ab === other.gueltig_ab &&
      (item.gueltig_bis ?? null) === (other.gueltig_bis ?? null) &&
      item.bereitstellungsart === other.bereitstellungsart &&
      item.bereitgestellt_am === other.bereitgestellt_am &&
      item.bereitstellung_id === other.bereitstellung_id;
  });
export const formatMemberClubDocumentSnapshotForPdf = (snapshot: MemberClubDocumentSnapshotItem[], documentType: PreparedContractDocumentType) => { if (!isCompleteMemberClubDocumentSnapshot(snapshot, documentType)) throw new Error("Der Vereinsunterlagen-Nachweis ist unvollständig."); return snapshot.map((item) => `${labels[item.dokumenttyp]} – ${item.fassung ? `Fassung ${item.fassung}` : "Papierfassung ausgehändigt"} – ${item.bereitstellungsart === "papier" ? "Papier übergeben" : "digital bereitgestellt"} am ${new Intl.DateTimeFormat("de-DE", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.bereitgestellt_am))}`).join("\n"); };
