import type { MemberClubDocumentStatus } from "../../models/club-documents/member-club-document-status";

export type MemberClubDocumentSnapshotItem = { dokumenttyp: "satzung" | "kleingartenordnung" | "beitragsordnung" | "arbeitsordnung"; vereinsdokument_fassung_id: number; fassung: string; gueltig_ab: string; gueltig_bis?: string | null; bereitstellungsart: "digital" | "papier"; bereitgestellt_am: string; bereitstellung_id: number };
const order = ["satzung", "kleingartenordnung", "beitragsordnung", "arbeitsordnung"] as const;
const labels: Record<MemberClubDocumentSnapshotItem["dokumenttyp"], string> = { satzung: "Satzung", kleingartenordnung: "Kleingartenordnung", beitragsordnung: "Beitragsordnung", arbeitsordnung: "Arbeitsordnung" };

export function createMemberClubDocumentSnapshot(status: MemberClubDocumentStatus): MemberClubDocumentSnapshotItem[] {
  return order.map((dokumenttyp) => {
    const item = status.documents.find((candidate) => candidate.dokumenttyp_code === dokumenttyp);
    if (!item || item.status === "keine_aktuelle_fassung") throw new Error("Für mindestens eine Pflichtunterlage ist keine aktuelle Vereinsfassung hinterlegt.");
    if (item.status !== "bereitgestellt" || !item.vereinsdokument_fassung_id || !item.bereitstellung_id || !item.fassung || !item.gueltig_ab || !item.bereitstellungsart || !item.bereitgestellt_am) throw new Error("Mindestens eine aktuelle Vereinsunterlage fehlt beim Mitglied.");
    return { dokumenttyp, vereinsdokument_fassung_id: item.vereinsdokument_fassung_id, fassung: item.fassung, gueltig_ab: item.gueltig_ab, gueltig_bis: item.gueltig_bis, bereitstellungsart: item.bereitstellungsart as "digital" | "papier", bereitgestellt_am: item.bereitgestellt_am, bereitstellung_id: item.bereitstellung_id };
  });
}
export const isCompleteMemberClubDocumentSnapshot = (snapshot: MemberClubDocumentSnapshotItem[] | undefined): snapshot is MemberClubDocumentSnapshotItem[] => !!snapshot && snapshot.length === order.length && snapshot.every((item, index) => item.dokumenttyp === order[index] && item.vereinsdokument_fassung_id > 0 && item.bereitstellung_id > 0 && !!item.fassung && !!item.gueltig_ab && !!item.bereitgestellt_am);
/** Compares the persisted evidence, not its JSON representation. JSONB may reorder keys and omit undefined values. */
export const memberClubDocumentSnapshotsEqual = (left: MemberClubDocumentSnapshotItem[] | undefined, right: MemberClubDocumentSnapshotItem[] | undefined) =>
  isCompleteMemberClubDocumentSnapshot(left) && isCompleteMemberClubDocumentSnapshot(right) && left.every((item, index) => {
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
export const formatMemberClubDocumentSnapshotForPdf = (snapshot: MemberClubDocumentSnapshotItem[]) => { if (!isCompleteMemberClubDocumentSnapshot(snapshot)) throw new Error("Der Vereinsunterlagen-Nachweis ist unvollständig."); return snapshot.map((item) => `${labels[item.dokumenttyp]} – Fassung ${item.fassung} – ${item.bereitstellungsart === "papier" ? "Papier übergeben" : "digital bereitgestellt"} am ${new Intl.DateTimeFormat("de-DE", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.bereitgestellt_am))}`).join("\n"); };
