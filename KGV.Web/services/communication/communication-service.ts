import type { AssignmentFilter, CommunicationArea, CommunicationItem } from "../../models/communication/communication-item";

const areaFilters: Record<CommunicationArea, Record<string, string>> = {
  signature: { vorgangstyp: "eq.vertrag_signatur", status: "in.(entwurf,vorbereitet,zur_signatur,letzte_pruefung)" },
  documents: { vorgangstyp: "eq.dokument_erzeugung", status: "in.(vorbereitet,fehler)" },
  mail: { vorgangstyp: "eq.mail", status: "in.(mail_bereit,wird_versendet,fehler)" },
  sent: { vorgangstyp: "eq.mail", status: "eq.erledigt", versendet_am: "not.is.null" },
  print: { vorgangstyp: "eq.druck", status: "in.(druck_bereit,fehler)" },
  history: { status: "in.(erledigt,abgebrochen,verworfen)" },
};

export function communicationQuery(area: CommunicationArea, assignment: AssignmentFilter, userId: string): Record<string, string> {
  const assignmentQuery: Record<string, string> = assignment === "mine" ? { zugewiesen_an: `eq.${userId}` } : assignment === "unassigned" ? { zugewiesen_an: "is.null" } : {};
  return { ...areaFilters[area], ...assignmentQuery };
}

export function isOpenCommunicationWorkItem(item: Pick<CommunicationItem, "vorgangstyp" | "status">): boolean {
  if (item.vorgangstyp === "vertrag_signatur") return ["entwurf", "vorbereitet", "zur_signatur", "letzte_pruefung"].includes(item.status);
  if (item.vorgangstyp === "mail") return ["mail_bereit", "wird_versendet", "fehler"].includes(item.status);
  if (item.vorgangstyp === "dokument_erzeugung") return ["vorbereitet", "fehler"].includes(item.status);
  return item.vorgangstyp === "druck" && ["druck_bereit", "fehler"].includes(item.status);
}

export function isHistoryCommunicationItem(item: Pick<CommunicationItem, "vorgangstyp" | "status" | "versendet_am">): boolean {
  return ["erledigt", "abgebrochen", "verworfen"].includes(item.status)
    && !(item.vorgangstyp === "mail" && item.status === "erledigt" && item.versendet_am !== null);
}

const documentLabels: Record<string, string> = { mitgliedsantrag: "Mitgliedsantrag", pachtvertrag: "Pachtvertrag", wartungsvertrag: "Wartungsvertrag", anschreiben: "Anschreiben", wartungsvertrag_verlaengerung: "Wartungsvertragsverlängerung", wartungsvertrag_beendigung: "Wartungsvertragsbeendigung" };
const statusLabels: Record<string, string> = { entwurf: "Entwurf", vorbereitet: "Vorbereitet", zur_signatur: "Zur Signatur", letzte_pruefung: "Letzte Prüfung", mail_bereit: "Versandbereit", wird_versendet: "Wird versendet", druck_bereit: "Druckbereit", erledigt: "Erledigt", fehler: "Fehler", abgebrochen: "Abgebrochen", verworfen: "Verworfen" };
const typeLabels: Record<string, string> = { vertrag_signatur: "Vertrag / Signatur", dokument_erzeugung: "Dokumenterzeugung", mail: "Mail", druck: "Druck" };

export const communicationDocumentLabel = (value: string) => documentLabels[value] ?? value.replaceAll("_", " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
export const communicationStatusLabel = (value: string) => statusLabels[value] ?? value;
export const communicationTypeLabel = (value: string) => typeLabels[value] ?? value;
export const communicationMemberLabel = (item: CommunicationItem) => item.mitglied ? [item.mitglied.vorname, item.mitglied.name].filter(Boolean).join(" ") || `Mitglied #${item.mitglied.id}` : "–";
export const communicationParcelLabel = (item: CommunicationItem) => item.parzelle?.garten_nr ? `Garten ${item.parzelle.garten_nr}` : "–";
export const communicationActorLabel = (value: string | null) => value ? `${value.slice(0, 8)}…` : "–";
