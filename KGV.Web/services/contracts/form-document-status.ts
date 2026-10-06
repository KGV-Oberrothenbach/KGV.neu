export type FormDocumentStatus = "none" | "unsigned" | "signed";
type FormDocument = { titel: string | null; dateiname: string | null };

function statusOf(document: FormDocument, type: "mitgliedsantrag" | "pachtvertrag") {
  const fileName = (document.dateiname ?? "").trim().split(/[\\/]/).pop() ?? "";
  const title = (document.titel ?? "").trim();
  const escapedType = type.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const current = new RegExp(`^.+-\\d+-\\d{4}-\\d{2}-\\d{2}-${escapedType}-(signiert|unsigniert)\\.pdf$`, "i").exec(fileName);
  const legacy = new RegExp(`^${escapedType}-\\((signiert|unsigniert)\\)_\\d{4}-\\d{2}-\\d{2}_\\d{2}-\\d{2}-\\d{2}\\.pdf$`, "i").exec(fileName);
  const definedTitle = new RegExp(`^${escapedType}\\s*\\((signiert|unsigniert)\\)$`, "i").exec(title);
  return (current ?? legacy ?? definedTitle)?.[1].toLocaleLowerCase("de") ?? null;
}

export function determineFormDocumentStatus(documents: FormDocument[], type: "mitgliedsantrag" | "pachtvertrag"): FormDocumentStatus {
  const statuses = documents.map((document) => statusOf(document, type)).filter((status): status is "signiert" | "unsigniert" => status !== null);
  return statuses.length === 0 ? "none" : statuses.includes("signiert") ? "signed" : "unsigned";
}
