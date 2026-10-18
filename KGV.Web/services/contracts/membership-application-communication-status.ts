export type ActiveMembershipApplicationStatus = "vorbereitet" | "zur_signatur" | "letzte_pruefung";

export function membershipApplicationCommunicationLabel(status: string | null | undefined): string | null {
  switch (status) {
    case "vorbereitet": return "Mitgliedsantrag: Zur Signatur vorbereitet";
    case "zur_signatur": return "Mitgliedsantrag: Signatur läuft";
    case "letzte_pruefung": return "Mitgliedsantrag: Letzte Prüfung";
    default: return null;
  }
}
