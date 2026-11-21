"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { useSelectedClub } from "../auth/AuthProvider";
import { FachrechtV2 } from "../../security/fachrechte-v2";
import type { FachrechtV2Row } from "../../services/users/fachrechte-v2-service";

// AP-02.3B: ausschliesslich V2-Schattenverwaltung. Alle Mutationen erfolgen
// ueber die auf der DemoDB geschuetzten und auditierten ap02_*-RPCs.
// Die alte UI bleibt bis zur einheitlichen serverseitigen Umschaltung erhalten.

type AssignedUserV2 = {
  user_id: string;
  mitglied_id: number | null;
  legacy_role: string;
  hauptrolle_code: string | null;
  zusatzrollen: string[];
  anzahl_ausnahmen: number;
  ist_geschuetzt: boolean;
};
type OverrideDecision = "inherit" | "grant" | "deny";

const mainRoles = [
  { code: "mitglied", title: "Mitglied" },
  { code: "bauausschuss", title: "Bauausschuss" },
  { code: "vorstand", title: "Vorstand" },
  { code: "vorstand_schriftfuehrer", title: "Vorstand-Schriftführer" },
  { code: "vorstand_kassenwart", title: "Vorstand-Kassenwart" },
] as const;

const groups = [
  { code: "A", title: "Mitglieder & Mitgliedschaft" },
  { code: "B", title: "Parzellen, Pacht & Wertermittlung" },
  { code: "C", title: "Zähler & Ablesungen" },
  { code: "D", title: "Finanzen & Jahresabschluss" },
  { code: "E", title: "Arbeitsstunden & Arbeitseinsätze" },
  { code: "F", title: "Dokumente & Vorlagen" },
  { code: "G", title: "Verträge, Vorgänge & Kommunikation" },
  { code: "H", title: "Vereinsorganisation" },
  { code: "I", title: "Auswertungen & Export" },
  { code: "J", title: "Benutzer & Systemsicherheit" },
] as const;

const adminOnlyCodes = new Set([
  "benutzer.lesen", "benutzer.verwalten", "berechtigungen.lesen",
  "berechtigungen.verwalten", "vereinsdaten.alle_lesen",
]);
const pendingClosingCodes = new Set([
  "jahresabschluss.finalisieren", "jahresabschluss.korrigieren",
]);
const allKnownCodes = new Set<string>(Object.values(FachrechtV2));

function validateRights(raw: unknown): FachrechtV2Row[] {
  if (!Array.isArray(raw) || raw.length !== allKnownCodes.size) {
    throw new Error("Die 55 Fachrechte wurden nicht vollständig geliefert.");
  }
  const seen = new Set<string>();
  return raw.map((item): FachrechtV2Row => {
    if (item === null || typeof item !== "object" || Array.isArray(item)) {
      throw new Error("Ungültige Antwort der Rechteverwaltung.");
    }
    const row = item as Record<string, unknown>;
    if (typeof row.code !== "string" || !allKnownCodes.has(row.code) || seen.has(row.code)
      || typeof row.gruppe !== "string" || !groups.some((g) => g.code === row.gruppe)
      || typeof row.titel !== "string" || typeof row.erlaubt !== "boolean"
      || typeof row.individuell_entzogen !== "boolean"
      || !Array.isArray(row.quellen) || !row.quellen.every((source) => typeof source === "string")) {
      throw new Error("Die Fachrechte-Antwort weicht vom Katalog ab.");
    }
    seen.add(row.code);
    return {
      code: row.code, gruppe: row.gruppe, titel: row.titel,
      erlaubt: row.erlaubt, individuell_entzogen: row.individuell_entzogen,
      quellen: row.quellen as string[],
    };
  });
}

function currentDecision(row: FachrechtV2Row): OverrideDecision {
  if (row.individuell_entzogen) return "deny";
  if (row.quellen.includes("individuelle_gewaehrung")) return "grant";
  return "inherit";
}

function failureMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Die Anfrage ist fehlgeschlagen.";
}

export function FachrechteV2Administration({ session, memberId }: {
  session: BrowserSession;
  memberId: number;
}) {
  const club = useSelectedClub();
  const [account, setAccount] = useState<AssignedUserV2 | null>(null);
  const [rights, setRights] = useState<FachrechtV2Row[]>([]);
  const [selectedRole, setSelectedRole] = useState("");
  const [draftOverrides, setDraftOverrides] = useState<Record<string, OverrideDecision>>({});
  const [reason, setReason] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [available, setAvailable] = useState(false);

  const loadData = useCallback(async () => {
    const users = await callSupabaseRpc<AssignedUserV2[]>(
      { club, session }, "ap02_list_user_access", {},
    );
    if (!Array.isArray(users)) throw new Error("Ungültige Benutzerliste.");
    const target = users.find((item) => item.mitglied_id === memberId) ?? null;
    if (!target) return { account: null, rights: [] as FachrechtV2Row[] };
    const preview = await callSupabaseRpc<unknown>(
      { club, session }, "ap02_admin_preview_user_rights", { p_user_id: target.user_id },
    );
    return { account: target, rights: validateRights(preview) };
  }, [club, session, memberId]);

  const loadAndApply = useCallback(async () => {
    const result = await loadData();
    setAccount(result.account);
    setRights(result.rights);
    setSelectedRole(result.account?.hauptrolle_code ?? "");
    setDraftOverrides(Object.fromEntries(result.rights.map((right) => [right.code, currentDecision(right)])));
    setAvailable(true);
  }, [loadData]);

  useEffect(() => {
    let active = true;
    void loadData().then((result) => {
      if (!active) return;
      setAccount(result.account);
      setRights(result.rights);
      setSelectedRole(result.account?.hauptrolle_code ?? "");
      setDraftOverrides(Object.fromEntries(result.rights.map((right) => [right.code, currentDecision(right)])));
      setAvailable(true);
      setError("");
    }).catch((cause: unknown) => {
      if (!active) return;
      setAvailable(false);
      setError(failureMessage(cause));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [loadData]);

  const isWorking = busy.length > 0;
  const enabledCount = useMemo(() => rights.filter((right) => right.erlaubt).length, [rights]);
  const normalizedReason = reason.trim();
  const hasReason = normalizedReason.length >= 3;

  async function runMutation(key: string, task: () => Promise<unknown>) {
    if (isWorking || !account || !hasReason) return;
    setBusy(key);
    setError("");
    setNotice("");
    try {
      const changed = await task();
      await loadAndApply();
      setNotice(changed === false ? "Bereits auf dem gewünschten Stand." : "Gespeichert und vom Server neu berechnet.");
    } catch (cause) {
      setError(failureMessage(cause));
    } finally {
      setBusy("");
    }
  }

  function saveMainRole() {
    if (!account || !selectedRole || !mainRoles.some((role) => role.code === selectedRole)) return;
    void runMutation("role", () => callSupabaseRpc<boolean>(
      { club, session }, "ap02_assign_main_role",
      { p_user_id: account.user_id, p_hauptrolle: selectedRole, p_grund: normalizedReason },
    ));
  }

  function switchExtraRole(code: "admin" | "wertermittlung", active: boolean) {
    if (!account) return;
    if (code === "admin" && account.ist_geschuetzt && !active) return;
    if (code === "wertermittlung" && active && account.hauptrolle_code !== "mitglied"
        && account.hauptrolle_code !== "bauausschuss") return;
    void runMutation(code, () => callSupabaseRpc<boolean>(
      { club, session }, "ap02_set_additional_role",
      { p_user_id: account.user_id, p_zusatzrolle: code, p_aktiv: active, p_grund: normalizedReason },
    ));
  }

  function saveRight(row: FachrechtV2Row) {
    if (!account) return;
    const decision = draftOverrides[row.code] ?? currentDecision(row);
    if ((adminOnlyCodes.has(row.code) || pendingClosingCodes.has(row.code)) && decision === "grant") return;
    if (account.ist_geschuetzt && row.code === "berechtigungen.verwalten" && decision === "deny") return;
    void runMutation(row.code, () => callSupabaseRpc<boolean>(
      { club, session }, "ap02_set_permission_override",
      { p_user_id: account.user_id, p_fachrecht: row.code,
        p_entscheidung: decision, p_grund: normalizedReason },
    ));
  }

  return <section className="data-workspace" aria-label="Fachrechte 2.0" style={{ marginTop: "1.5rem" }}>
    <h2>Fachrechte 2.0 – Neues Berechtigungssystem</h2>
    <p className="context-note">Derzeit parallel zum bisherigen Rechtesystem. Hier gespeicherte Änderungen gelten für die neuen AP-02-RPCs. Bestehende Fachaktionen verwenden bis zu ihrer serverseitigen Umstellung weiterhin die alten Rechte.</p>
    {loading && <p role="status">Lade Rechteverwaltung …</p>}
    {!loading && !available && <p className="notice" role="alert">Das neue Berechtigungssystem ist für diesen Zugang oder dieses Vereinsprojekt noch nicht freigeschaltet. {error}</p>}
    {available && !account && <p className="context-note">Für dieses Mitglied gibt es noch kein verknüpftes App-Konto.</p>}
    {available && account && <>
      <p className="context-note">Hauptrolle: <strong>{account.hauptrolle_code ?? "noch nicht zugewiesen"}</strong> · {enabledCount} von 55 Fachrechten effektiv erlaubt · {account.anzahl_ausnahmen} individuelle Ausnahmen{account.ist_geschuetzt ? " · geschütztes Admin-Konto" : ""}</p>
      {error && <p className="notice" role="alert">{error}</p>}
      {notice && <p className="notice" role="status">{notice}</p>}
      <div className="detail-panel">
        <label htmlFor="ap02-change-reason">Begründung jeder Änderung</label>
        <input id="ap02-change-reason" value={reason} maxLength={250}
          placeholder="z. B. Funktion gemäß Vorstandsbeschluss"
          onChange={(event) => setReason(event.target.value)} />
        {!hasReason && <p className="context-note">Mindestens drei Zeichen erforderlich; jede Änderung wird protokolliert.</p>}
        <label htmlFor="ap02-main-role">Hauptrolle</label>
        <select id="ap02-main-role" value={selectedRole} disabled={isWorking}
          onChange={(event) => setSelectedRole(event.target.value)}>
          <option value="">Noch nicht zugewiesen</option>
          {mainRoles.map((role) => <option key={role.code} value={role.code}>{role.title}</option>)}
        </select>
        <button disabled={isWorking || !hasReason || !selectedRole || selectedRole === account.hauptrolle_code} onClick={saveMainRole}>
          Hauptrolle speichern
        </button>
        <h3>Zusatzrollen</h3>
        {(["wertermittlung", "admin"] as const).map((code) => {
          const hasRole = account.zusatzrollen.includes(code);
          const protectedRole = code === "admin" && hasRole && account.ist_geschuetzt;
          const incompatible = code === "wertermittlung" && !hasRole &&
            !["mitglied", "bauausschuss"].includes(account.hauptrolle_code ?? "");
          return <div key={code} className="editor-actions">
            <span><strong>{code === "admin" ? "Admin (Systemverwaltung / Leserechte)" : "Wertermittlung"}</strong> – {hasRole ? "zugewiesen" : "nicht zugewiesen"}{protectedRole ? " (dauerhaft geschützt)" : ""}</span>
            <button className="secondary-action" disabled={isWorking || !hasReason || protectedRole || incompatible}
              onClick={() => switchExtraRole(code, !hasRole)}>{hasRole ? "Entziehen" : "Zuweisen"}</button>
          </div>;
        })}
      </div>
      <h3>Fachrechte nach Gruppen</h3>
      <p className="context-note">„Rollenstandard“ übernimmt die gültige Basis; „Gewähren“ ergänzt und „Entziehen“ sperrt. Individuelle Entzüge gehen vor. Abschlussrechte D5/D6 werden bis zur fachlichen Entscheidung nicht neu gewährt.</p>
      {groups.map((group) => {
        const entries = rights.filter((right) => right.gruppe === group.code);
        return <details key={group.code} className="detail-panel" style={{ marginTop: ".65rem" }}>
          <summary><strong>{group.code} · {group.title}</strong> ({entries.filter((row) => row.erlaubt).length}/{entries.length} erlaubt)</summary>
          <div className="data-table-wrap"><table><thead><tr><th>Fachrecht</th><th>Effektiv</th><th>Ausnahme</th><th>Aktion</th></tr></thead><tbody>
            {entries.map((right) => {
              const current = currentDecision(right);
              const draft = draftOverrides[right.code] ?? current;
              const forbidGrant = adminOnlyCodes.has(right.code) || pendingClosingCodes.has(right.code);
              const forbidDeny = account.ist_geschuetzt && right.code === "berechtigungen.verwalten";
              return <tr key={right.code}>
                <td><strong>{right.titel}</strong><small>{right.code} · {right.quellen.length ? right.quellen.join(", ") : "kein Grundrecht"}</small></td>
                <td>{right.erlaubt ? "Erlaubt" : right.individuell_entzogen ? "Entzogen" : "Nicht erlaubt"}</td>
                <td><select aria-label={`Ausnahme für ${right.titel}`} value={draft} disabled={isWorking}
                  onChange={(event) => setDraftOverrides((previous) => ({ ...previous, [right.code]: event.target.value as OverrideDecision }))}>
                  <option value="inherit">Rollenstandard</option>
                  <option value="grant" disabled={forbidGrant}>Gewähren</option>
                  <option value="deny" disabled={forbidDeny}>Entziehen</option>
                </select></td>
                <td><button className="secondary-action" disabled={isWorking || !hasReason || draft === current
                  || (forbidGrant && draft === "grant") || (forbidDeny && draft === "deny")}
                  onClick={() => saveRight(right)}>Speichern</button></td>
              </tr>;
            })}
          </tbody></table></div>
        </details>;
      })}
    </>}
  </section>;
}
