import type { ClubDocumentVersion } from "../../models/club-documents/club-document";

export function getValidClubDocumentVersion(versions: ClubDocumentVersion[], documentType: string, date: string): ClubDocumentVersion | null {
  const matches = versions.filter((version) => version.dokumenttyp_code === documentType && version.gueltig_ab <= date && (version.gueltig_bis === null || version.gueltig_bis >= date));
  if (matches.length > 1) throw new Error(`Mehrere gültige Fassungen für ${documentType} am ${date}.`);
  return matches[0] ?? null;
}

export function predecessorEnd(successorStart: string): string {
  const date = new Date(`${successorStart}T00:00:00Z`); date.setUTCDate(date.getUTCDate() - 1); return date.toISOString().slice(0, 10);
}

export function createClubDocumentStoragePath(documentType: string, version: string, fileName: string): string {
  if (!documentType || !version || !fileName || !/\.pdf$/i.test(fileName)) throw new Error("Dokumenttyp, Fassung und eine PDF-Datei sind erforderlich.");
  if ([documentType, version, fileName].some((value) => /[\\/]/.test(value))) throw new Error("Dokumenttyp, Fassung und Dateiname dürfen keinen Pfadtrenner enthalten.");
  return `vereinsunterlagen/${documentType}/${version}/${fileName}`;
}
