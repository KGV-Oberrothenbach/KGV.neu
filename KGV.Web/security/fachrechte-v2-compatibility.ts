// AP-02.4K: only server-evaluated Fachrechte 2.0 can grant UI capabilities.
// Numeric values are a temporary adapter for existing UI components, not
// an independent authorization system. Server-side RPC/RLS remain authoritative.
import { Permission } from "./permissions";
import type { FachrechtV2Row } from "../services/users/fachrechte-v2-service";

export function hasFachrechtV2(rows: readonly FachrechtV2Row[] | null, code: string): boolean {
  return rows?.some((right) => right.code === code && right.erlaubt === true) === true;
}

const flags: readonly (readonly [number, readonly string[]])[] = [
  [Permission.searchMembers, ["mitglied.lesen"]],
  [Permission.viewMembers, ["mitglied.lesen"]],
  [Permission.editAllMembers, ["mitglied.bearbeiten"]],
  [Permission.createMember, ["mitglied.anlegen"]],
  [Permission.manageDocuments, ["dokument.verwalten"]],
  [Permission.readMeters, ["zaehler.lesen"]],
  [Permission.manageMeterChanges, ["zaehlerwechsel.verwalten"]],
  [Permission.approveMeterReadings, ["ablesung.freigeben"]],
  [Permission.manageWorkHours, ["arbeitsstunden.bearbeiten", "arbeitsstunden.freigeben"]],
  [Permission.manageRoles, ["berechtigungen.verwalten"]],
  [Permission.showStammdaten, ["mitglied.lesen"]],
  [Permission.readStammdaten, ["mitglied.lesen"]],
  [Permission.writeStammdaten, ["mitglied.bearbeiten"]],
  [Permission.showParzellen, ["parzelle.lesen"]],
  [Permission.readParzellen, ["parzelle.lesen"]],
  [Permission.writeParzellen, ["parzelle.bearbeiten"]],
  [Permission.readDocuments, ["dokument.lesen"]],
  [Permission.readWorkHours, ["arbeitsstunden.lesen"]],
  [Permission.readRoles, ["berechtigungen.lesen"]],
  [Permission.manageWorkAssignments, ["arbeitseinsatz.verwalten"]],
  [Permission.manageAppointments, ["termin.verwalten"]],
  [Permission.manageAnnouncements, ["bekanntmachung.verwalten"]],
  [Permission.performYearEndClosing, ["jahresabschluss.finalisieren"]],
  [Permission.registerHistoricalContracts, ["historische_vertraege.erfassen"]],
  [Permission.manageExports, ["export.ausfuehren"]],
  [Permission.manageClubSeasonConfiguration, ["saison.verwalten"]],
  [Permission.manageDocumentTypes, ["dokumenttyp.verwalten"]],
  [Permission.manageClubDocuments, ["vereinsunterlagen.verwalten"]],
  [Permission.manageClubFunctions, ["vereinsfunktion.verwalten"]],
  [Permission.manageCommunicationWorkflows, ["vorgang.bearbeiten"]],
  [Permission.archiveDocuments, ["dokument.archivieren"]],
];

export function permissionMaskFromFachrechteV2(rows: readonly FachrechtV2Row[] | null, memberId: number | null): number {
  if (!rows || rows.length !== 55) return 0; // no legacy-role fallback
  const granted = new Set(rows.filter((row) => row.erlaubt).map((row) => row.code));
  // An explicit deny for member-read must also block the historic own-data UI flag.
  const ownReadDenied = rows.some((row) => row.code === "mitglied.lesen" && row.individuell_entzogen);
  let mask = memberId !== null && memberId > 0 && !ownReadDenied ? Permission.seeOwnData : 0;
  for (const [bit, rights] of flags) {
    if (rights.every((name) => granted.has(name))) mask += bit;
  }
  return mask;
}