export type LeaseTenantRecord = { id?: number; aktiv?: boolean; aufnahme_status?: string };

export function isEligibleLeaseTenantRecord(candidate: LeaseTenantRecord | null | undefined) {
  return Boolean(candidate && candidate.aktiv === true && candidate.aufnahme_status === "aufgenommen");
}
