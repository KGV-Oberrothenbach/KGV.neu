import type { Document } from "../../models/documents/document";
import { resolveFormDocumentMetadata } from "../documents/document-metadata-service";

export type FormDocumentStatus = "none" | "unsigned" | "signed";
type FormDocument = Pick<Document, "titel" | "dateiname" | "storage_path">;

export function getFormDocumentStatus(document: FormDocument, type: "mitgliedsantrag" | "pachtvertrag") {
  const metadata = resolveFormDocumentMetadata(document);
  return metadata?.type === type ? metadata.status : null;
}

export function determineFormDocumentStatus(documents: FormDocument[], type: "mitgliedsantrag" | "pachtvertrag"): FormDocumentStatus {
  const statuses = documents.map((document) => getFormDocumentStatus(document, type)).filter((status): status is "signiert" | "unsigniert" => status !== null);
  return statuses.length === 0 ? "none" : statuses.includes("signiert") ? "signed" : "unsigned";
}
