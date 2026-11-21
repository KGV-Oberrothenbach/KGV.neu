import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { FachrechtV2 } from "../../security/fachrechte-v2";

// AP-02.3A: ausschliesslich ein serverseitig berechneter Schattenstatus.
// Die bisherige Navigation und die tatsaechlichen API-Grenzen bleiben unveraendert.
export type FachrechtV2Row = {
  code: string;
  gruppe: string;
  titel: string;
  erlaubt: boolean;
  individuell_entzogen: boolean;
  quellen: string[];
};

export async function loadMyFachrechteV2(
  club: ClubContext, session: BrowserSession,
): Promise<readonly FachrechtV2Row[]> {
  const result = await callSupabaseRpc<unknown>(
    { club, session }, "ap02_preview_effective_rights", {},
  );
  const allowedCodes = new Set<string>(Object.values(FachrechtV2));
  if (!Array.isArray(result) || result.length !== allowedCodes.size) {
    throw new Error("Fachrechte 2.0 konnten nicht vollstaendig geladen werden.");
  }
  const seen = new Set<string>();
  const rights = result.map((item: unknown): FachrechtV2Row => {
    if (typeof item !== "object" || item === null || Array.isArray(item)) {
      throw new Error("Ungueltige Fachrechte-Antwort.");
    }
    const row = item as Record<string, unknown>;
    if (typeof row.code !== "string" || !allowedCodes.has(row.code) || seen.has(row.code)
        || typeof row.gruppe !== "string" || typeof row.titel !== "string"
        || typeof row.erlaubt !== "boolean" || typeof row.individuell_entzogen !== "boolean"
        || !Array.isArray(row.quellen) || !row.quellen.every((part: unknown) => typeof part === "string")) {
      throw new Error("Fachrechte-Antwort entspricht nicht dem Katalog.");
    }
    seen.add(row.code);
    return {
      code: row.code, gruppe: row.gruppe, titel: row.titel,
      erlaubt: row.erlaubt, individuell_entzogen: row.individuell_entzogen,
      quellen: row.quellen as string[],
    };
  });
  return rights;
}
