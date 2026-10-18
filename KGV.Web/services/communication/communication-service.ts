import type { AssignmentFilter, CommunicationArea, CommunicationItem } from "../../models/communication/communication-item";

const areaFilters: Record<CommunicationArea, Record<string, string>> = {
  signature: { vorgangstyp: "eq.vertrag_signatur", status: "in.(entwurf,vorbereitet,zur_signatur,letzte_pruefung)" },
  mail: { vorgangstyp: "eq.mail", status: "in.(mail_bereit,wird_versendet,fehler)" },
  print: { vorgangstyp: "eq.druck", status: "in.(druck_bereit,fehler)" },
  done: { status: "in.(erledigt,abgebrochen)" },
};

export function communicationQuery(area: CommunicationArea, assignment: AssignmentFilter, userId: string): Record<string, string> {
  const assignmentQuery: Record<string, string> = assignment === "mine" ? { zugewiesen_an: `eq.${userId}` } : assignment === "unassigned" ? { zugewiesen_an: "is.null" } : {};
  return { ...areaFilters[area], ...assignmentQuery };
}

const documentLabels: Record<string, string> = { mitgliedsantrag: "Mitgliedsantrag", pachtvertrag: "Pachtvertrag", wartungsvertrag: "Wartungsvertrag", anschreiben: "Anschreiben", wartungsvertrag_verlaengerung: "Wartungsvertragsverlängerung", wartungsvertrag_beendigung: "Wartungsvertragsbeendigung" };
const statusLabels: Record<string, string> = { entwurf: "Entwurf", vorbereitet: "Vorbereitet", zur_signatur: "Zur Signatur", letzte_pruefung: "Letzte Prüfung", mail_bereit: "Versandbereit", wird_versendet: "Wird versendet", druck_bereit: "Druckbereit", erledigt: "Erledigt", fehler: "Fehler", abgebrochen: "Abgebrochen" };
const typeLabels: Record<string, string> = { vertrag_signatur: "Vertrag / Signatur", mail: "Mail", druck: "Druck" };

export const communicationDocumentLabel = (value: string) => documentLabels[value] ?? value.replaceAll("_", " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
export const communicationStatusLabel = (value: string) => statusLabels[value] ?? value;
export const communicationTypeLabel = (value: string) => typeLabels[value] ?? value;
export const communicationMemberLabel = (item: CommunicationItem) => item.mitglied ? [item.mitglied.vorname, item.mitglied.name].filter(Boolean).join(" ") || `Mitglied #${item.mitglied.id}` : "–";
export const communicationParcelLabel = (item: CommunicationItem) => item.parzelle?.garten_nr ? `Garten ${item.parzelle.garten_nr}` : "–";
export const communicationActorLabel = (value: string | null) => value ? `${value.slice(0, 8)}…` : "–";
