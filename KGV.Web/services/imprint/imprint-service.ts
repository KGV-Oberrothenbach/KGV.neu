import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadClubConfiguration } from "../configuration/club-configuration-service";
import { listImprintMembers, listImprintSlots, type ImprintSlot } from "../../repositories/imprint/imprint-repository";

export type ImprintContact = { id: number; functionName: string; name: string; mobile: string; address: string; isConstruction: boolean };
export type ImprintData = { club: { name: string; register: string; email: string }; responsible: ImprintContact | null; board: ImprintContact[]; construction: ImprintContact[] };

export function isImprintChairSlot(slot: Pick<ImprintSlot, "slot_key" | "funktion">): boolean {
  const combined = `${slot.slot_key} ${slot.funktion}`.toLocaleLowerCase("de");
  return combined.includes("vorsitz") && !/stellv|stellvertr|\bstv\b|2\.\s*vorsitz|zweite\s+vorsitz/.test(combined);
}

function isConstructionSlot(slot: Pick<ImprintSlot, "slot_key" | "funktion">): boolean {
  const combined = `${slot.slot_key} ${slot.funktion}`.toLocaleLowerCase("de");
  return combined.includes("bauausschuss") || combined.includes("bau-ausschuss") || combined.includes("bau ausschuss") || (combined.includes("bau") && combined.includes("ausschuss"));
}

function createContact(slot: ImprintSlot, member: Awaited<ReturnType<typeof listImprintMembers>>[number] | undefined, isChair: boolean): ImprintContact {
  const name = member ? [member.vorname, member.name].filter(Boolean).join(" ").trim() : "";
  const city = member ? [member.plz, member.ort].filter(Boolean).join(" ").trim() : "";
  return { id: slot.id, functionName: slot.funktion?.trim() || "Funktion", name: name || "Aktuell nicht hinterlegt.", mobile: member?.handy?.trim() || "", address: isChair ? [member?.adresse?.trim(), city].filter(Boolean).join(", ") : "", isConstruction: isConstructionSlot(slot) };
}

export async function loadImprintData(club: ClubContext, session: BrowserSession): Promise<ImprintData> {
  const [configuration, slots, members] = await Promise.all([
    loadClubConfiguration(club, session),
    listImprintSlots(club, session),
    listImprintMembers(club, session),
  ]);
  const visibleMembers = members.filter((member) => session.isDemoAccount || !member.is_demo);
  const byId = new Map(visibleMembers.map((member) => [member.id, member]));
  let responsible: ImprintContact | null = null;
  const board: ImprintContact[] = [];
  const construction: ImprintContact[] = [];
  for (const slot of slots) {
    const isChair = isImprintChairSlot(slot);
    const contact = createContact(slot, slot.mitglied_id ? byId.get(slot.mitglied_id) : undefined, isChair);
    if (isChair) { responsible ??= contact; continue; }
    if (contact.isConstruction) construction.push(contact); else board.push(contact);
  }
  const active = configuration.exists ? configuration.draft : null;
  return { club: { name: active?.vereinsname?.trim() || "Aktuell nicht hinterlegt", register: active?.registerangabe?.trim() || "Aktuell nicht hinterlegt", email: active?.standard_email?.trim() || "" }, responsible, board, construction };
}
