import type { ClubDocumentType } from "./club-document";

export type MemberClubDocumentStatusItem = {
  dokumenttyp_code: ClubDocumentType["code"];
  bezeichnung: string;
  pflicht: boolean;
  vereinsdokument_fassung_id: number | null;
  fassung: string | null;
  gueltig_ab: string | null;
  gueltig_bis: string | null;
  storage_path: string | null;
  dateiname: string | null;
  bereitstellung_id: number | null;
  bereitstellungsart: string | null;
  bereitgestellt_am: string | null;
  bereitgestellt_von: string | null;
  status: "keine_aktuelle_fassung" | "fehlt" | "bereitgestellt";
};

export type MemberClubDocumentStatus = {
  documents: MemberClubDocumentStatusItem[];
  missingDocuments: MemberClubDocumentStatusItem[];
  complete: boolean;
  hasMissingClubVersion: boolean;
};

export type MemberClubDocumentDeliveryHistoryItem = {
  bereitstellung_id: number;
  mitglied_id: number;
  vereinsdokument_fassung_id: number;
  dokumenttyp_code: ClubDocumentType["code"];
  bezeichnung: string;
  fassung: string;
  bereitstellungsart: "digital" | "papier";
  bereitgestellt_am: string;
  bereitgestellt_von: string | null;
  storniert_am: string | null;
  storniert_von: string | null;
  storno_grund: string | null;
};
