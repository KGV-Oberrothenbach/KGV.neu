export type LeaseTenantCandidate = { id: number; aktiv: boolean; aufnahme_status?: string };

export function isEligibleLeaseTenant(candidate: LeaseTenantCandidate | null | undefined) {
  return Boolean(candidate && candidate.aktiv && candidate.aufnahme_status === "aufgenommen");
}

export function canUseSecondLeaseTenant(primaryId: number, candidate: LeaseTenantCandidate | null | undefined) {
  return isEligibleLeaseTenant(candidate) && candidate!.id !== primaryId;
}

const roundMoney = (value: number) => Math.round(value * 100) / 100;
export function calculateLeaseRent(area: number, rate: number, startDate: string) {
  const month = Number(startDate.slice(5, 7));
  const annualRent = roundMoney(area * rate);
  const runningYearRent = roundMoney(annualRent * Math.max(1, Math.min(12, 13 - month)) / 12);
  return { annualRent, runningYearRent };
}
