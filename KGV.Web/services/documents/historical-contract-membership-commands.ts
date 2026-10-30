/** Only explicit decisions for removed predecessor parties enter the RPC payload.
 * The RPC independently derives and validates that context from database history. */
export function buildHistoricalMembershipEnds(
  documentTypeCode: string,
  removedPartyIds: readonly number[],
  confirmedEnds: Readonly<Record<number, string>>,
) {
  if (documentTypeCode !== "historischer_pv_folgevertrag") return [];
  return [...new Set(removedPartyIds)]
    .filter(id => confirmedEnds[id] !== undefined)
    .map(id => ({ mitglied_id: id, mitglied_ende: confirmedEnds[id] }));
}
