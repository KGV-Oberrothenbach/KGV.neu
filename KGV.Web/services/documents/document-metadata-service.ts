import type { Document } from "../../models/documents/document";
import type { FormDocumentMetadata, FormDocumentMetadataStatus, FormDocumentType } from "../../models/documents/form-document";

type FormDocumentSource = Pick<Document, "dateiname" | "storage_path" | "titel">;

const types = new Set<FormDocumentType>(["mitgliedsantrag", "mitgliedsvertrag", "pachtvertrag"]);
const statuses = new Set<FormDocumentMetadataStatus>(["unsigniert", "signiert"]);
const currentFileName = /^.+-\d+-\d{4}-\d{2}-\d{2}-([a-z0-9]+)-([a-z0-9]+)\.pdf$/i;
const legacyFileName = /^(mitgliedsantrag|mitgliedsvertrag|pachtvertrag)-\((signiert|unsigniert)\)_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.pdf$/i;
const titlePattern = /^(mitgliedsantrag|mitgliedsvertrag|pachtvertrag)\s*\((signiert|unsigniert)\)$/i;

export function resolveFormDocumentMetadata(document: FormDocumentSource): FormDocumentMetadata | null {
  for (const candidate of [document.dateiname, document.storage_path, document.titel]) {
    const metadata = parseFormDocumentMetadata(candidate);
    if (metadata) return metadata;
  }
  return null;
}

export function isFormDocument(document: FormDocumentSource): boolean {
  return resolveFormDocumentMetadata(document) !== null;
}

export function isContractDocument(document: FormDocumentSource): boolean {
  const metadata = resolveFormDocumentMetadata(document);
  return metadata?.type === "mitgliedsvertrag" || metadata?.type === "pachtvertrag";
}

export function isUnsignedContractDocument(document: FormDocumentSource): boolean {
  return isContractDocument(document) && resolveFormDocumentMetadata(document)?.status === "unsigniert";
}

export function isSignedContractDocument(document: FormDocumentSource): boolean {
  return isContractDocument(document) && resolveFormDocumentMetadata(document)?.status === "signiert";
}

export function formDocumentTypeLabel(type: FormDocumentType): string {
  return type === "mitgliedsantrag" ? "Mitgliedsantrag" : type === "mitgliedsvertrag" ? "Mitgliedsvertrag" : "Pachtvertrag";
}

export function formDocumentStatusLabel(status: FormDocumentMetadataStatus): string {
  return status === "signiert" ? "Signiert" : "Unsigniert";
}

function parseFormDocumentMetadata(value: string | null): FormDocumentMetadata | null {
  const candidate = fileName(value);
  if (!candidate) return null;
  const match = currentFileName.exec(candidate) ?? legacyFileName.exec(candidate) ?? titlePattern.exec(candidate);
  if (!match) return null;
  const type = match[1].toLocaleLowerCase("de") as FormDocumentType;
  const status = match[2].toLocaleLowerCase("de") as FormDocumentMetadataStatus;
  return types.has(type) && statuses.has(status) ? { type, status } : null;
}

function fileName(value: string | null): string {
  return value?.trim().replaceAll("\\", "/").split("/").pop() ?? "";
}
