export type DocumentOwner =
  | { kind: "member"; id: number }
  | { kind: "parcel"; id: number };

export type Document = {
  id: number;
  mitglied_id: number | null;
  parzelle_id: number | null;
  bucket: string | null;
  storage_path: string | null;
  drive_file_id: string | null;
  titel: string | null;
  dateiname: string | null;
  mime_type: string | null;
  size_bytes: number | null;
  created_at: string | null;
  updated_at: string | null;
  created_by: string | null;
  archiviert_at: string | null;
  archiviert_by: string | null;
  archiviert_begruendung: string | null;
};

export const documentSelect = "id,mitglied_id,parzelle_id,bucket,storage_path,drive_file_id,titel,dateiname,mime_type,size_bytes,created_at,updated_at,created_by,archiviert_at,archiviert_by,archiviert_begruendung";

export function memberDocumentOwner(id: number): DocumentOwner {
  return createDocumentOwner("member", id);
}

export function parcelDocumentOwner(id: number): DocumentOwner {
  return createDocumentOwner("parcel", id);
}

export function documentOwnerFromRecord(document: Pick<Document, "mitglied_id" | "parzelle_id">): DocumentOwner | null {
  if (isValidDocumentOwnerId(document.mitglied_id) && document.parzelle_id === null) {
    return { kind: "member", id: document.mitglied_id };
  }
  if (isValidDocumentOwnerId(document.parzelle_id) && document.mitglied_id === null) {
    return { kind: "parcel", id: document.parzelle_id };
  }
  return null;
}

export function assertDocumentOwner(owner: unknown): asserts owner is DocumentOwner {
  if (!isDocumentOwner(owner)) throw new Error("Der Dokumenteigentümer ist ungültig.");
}

export function isDocumentOwner(owner: unknown): owner is DocumentOwner {
  if (!owner || typeof owner !== "object") return false;
  const candidate = owner as { kind?: unknown; id?: unknown };
  return (candidate.kind === "member" || candidate.kind === "parcel")
    && isValidDocumentOwnerId(candidate.id);
}

function createDocumentOwner(kind: DocumentOwner["kind"], id: number): DocumentOwner {
  if (!isValidDocumentOwnerId(id)) throw new Error("Der Dokumenteigentümer ist ungültig.");
  return kind === "member" ? { kind, id } : { kind, id };
}

function isValidDocumentOwnerId(id: unknown): id is number {
  return typeof id === "number" && Number.isInteger(id) && id > 0;
}
