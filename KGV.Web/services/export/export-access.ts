// UI projection of FR2-11 source authorization; RPC/RLS remain authoritative.
const sourceRights: Readonly<Record<string, readonly string[]>> = Object.freeze({
  mitgliederliste: ["mitglied.lesen", "parzelle.lesen"],
  datenschutz_aktive_mitglieder: ["mitglied.lesen", "parzelle.lesen"],
  parzellenliste: ["parzelle.lesen", "mitglied.lesen"],
  arbeitsstunden_uebersicht: ["arbeitsstunden.lesen", "mitglied.lesen", "parzelle.lesen"],
  jahresablesung_status: ["zaehler.lesen", "parzelle.lesen", "mitglied.lesen"],
  zaehler_eichfaelligkeit: ["zaehler.lesen", "parzelle.lesen", "mitglied.lesen"],
  jae_xlsx: ["zaehler.lesen", "parzelle.lesen", "mitglied.lesen"],
});
type Right = { code: string; erlaubt: boolean };
type Definition = { export_key: string; aktiv: boolean };

export function canUseExport(key: string, rights: readonly Right[] | null): boolean {
  if (!rights || !Object.hasOwn(sourceRights, key)) return false;
  const granted = new Set(rights.filter((right) => right.erlaubt === true).map((right) => right.code));
  return ["auswertung.lesen", "export.ausfuehren", ...sourceRights[key]].every((code) => granted.has(code));
}

export function authorizedExportDefinitions<T extends Definition>(definitions: readonly T[], rights: readonly Right[] | null): T[] {
  // JAE is a separate dedicated action, not a generic catalog export.
  return definitions.filter((definition) => definition.aktiv && definition.export_key !== "jae_xlsx" && canUseExport(definition.export_key, rights));
}

export function chooseAuthorizedExportKey(definitions: readonly Definition[], preferred: string): string {
  return definitions.some((definition) => definition.export_key === preferred) ? preferred : definitions[0]?.export_key ?? "";
}
