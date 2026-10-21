import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadClubConfiguration } from "../configuration/club-configuration-service";
import { listImprintContacts, type ImprintContactRow } from "../../repositories/imprint/imprint-repository";

export type ImprintContact = { id: number; functionName: string; name: string; mobile: string; address: string };
export type ImprintGroup = { key: string; label: string; order: number; contacts: ImprintContact[] };
export type ImprintData = { club: { name: string; register: string; email: string }; responsible: ImprintContact[]; groups: ImprintGroup[] };

function createContact(row: ImprintContactRow): ImprintContact {
  const name = [row.vorname, row.nachname].filter(Boolean).join(" ").trim() || "Aktuell nicht hinterlegt.";
  const city = [row.plz, row.ort].filter(Boolean).join(" ").trim();
  return { id: row.besetzung_id, functionName: row.bezeichnung?.trim() || "Funktion", name, mobile: row.handy?.trim() || "", address: row.zeige_ladungsfaehige_anschrift ? [row.adresse?.trim(), city].filter(Boolean).join(", ") : "" };
}

export function groupImprintContacts(rows: ImprintContactRow[]): Pick<ImprintData, "responsible" | "groups"> {
  const responsible: ImprintContact[] = []; const groups = new Map<string, ImprintGroup>();
  for (const row of rows) { const contact = createContact(row); if (row.ist_hauptkontakt) { responsible.push(contact); continue; } const group = groups.get(row.gruppen_schluessel) ?? { key: row.gruppen_schluessel, label: row.gruppen_bezeichnung, order: row.gruppen_sort_order, contacts: [] }; group.contacts.push(contact); groups.set(group.key, group); }
  return { responsible, groups: [...groups.values()].sort((a, b) => a.order - b.order || a.label.localeCompare(b.label, "de")) };
}

export async function loadImprintData(club: ClubContext, session: BrowserSession): Promise<ImprintData> {
  const [configuration, contacts] = await Promise.all([
    loadClubConfiguration(club, session),
    listImprintContacts(club, session),
  ]);
  const active = configuration.exists ? configuration.draft : null;
  return { club: { name: active?.vereinsname?.trim() || "Aktuell nicht hinterlegt", register: active?.registerangabe?.trim() || "Aktuell nicht hinterlegt", email: active?.standard_email?.trim() || "" }, ...groupImprintContacts(contacts) };
}
