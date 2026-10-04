import { type BrowserSession } from "../../lib/supabase-auth";
import { listImprintMembers, listImprintSlots } from "../../repositories/imprint/imprint-repository";

export type ImprintContact = { id: number; functionName: string; name: string; mobile: string; address: string; isConstruction: boolean };

export async function loadImprintContacts(session: BrowserSession): Promise<ImprintContact[]> {
  const [slots, members] = await Promise.all([listImprintSlots(session), listImprintMembers(session)]);
  const byId = new Map(members.map((member) => [member.id, member]));
  const hasMarker = (value: string) => /demo|test|play\s*store|example\.(com|org|net)/i.test(value);
  return slots.map((slot) => {
    const member = slot.mitglied_id ? byId.get(slot.mitglied_id) : undefined;
    const combined = `${slot.slot_key} ${slot.funktion}`.toLocaleLowerCase("de");
    const isChair = combined.includes("vorsitz") && !/stellv|stellvertr|\bstv\b|2\.\s*vorsitz|zweite\s+vorsitz/.test(combined);
    const name = member ? [member.vorname, member.name].filter(Boolean).join(" ").trim() : "Aktuell nicht hinterlegt.";
    const city = member ? [member.plz, member.ort].filter(Boolean).join(" ").trim() : "";
    return { id: slot.id, functionName: slot.funktion?.trim() || "Funktion", name: name || "Aktuell nicht hinterlegt.", mobile: member?.handy?.trim() || "", address: isChair ? [member?.adresse?.trim(), city].filter(Boolean).join(", ") : "", isConstruction: combined.includes("bauausschuss") || combined.includes("bau-ausschuss") || combined.includes("bau ausschuss") || combined.includes("ausschuss") || (combined.includes("bau") && combined.includes("ausschuss")), isChair };
  }).filter((item) => !item.isChair && !hasMarker(`${item.name} ${item.mobile} ${item.address}`)).map(({ isChair: _isChair, ...item }) => item);
}
