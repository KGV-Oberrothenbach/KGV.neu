/**
 * @typedef {import("../../models/maintenance/maintenance-assignment").MaintenanceAssignment} MaintenanceAssignment
 * @typedef {import("../../models/maintenance/maintenance-contract").MaintenanceContract} MaintenanceContract
 * @typedef {{ active: MaintenanceAssignment[]; future: MaintenanceAssignment[]; history: MaintenanceAssignment[] }} ClassifiedMaintenanceAssignments
 */

export function maintenanceReferenceDate() {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

/**
 * @param {MaintenanceAssignment[]} assignments
 * @param {string} referenceDate
 * @param {MaintenanceContract[]} contracts
 * @returns {ClassifiedMaintenanceAssignments}
 */
export function classifyMaintenanceAssignments(assignments, referenceDate, contracts = []) {
  const contractTitles = new Map(contracts.map((contract) => [contract.id, contract.title]));
  const title = (assignment) => contractTitles.get(assignment.maintenanceContractId) ?? "Wartungsvertrag";
  const byTitle = (left, right) => title(left).localeCompare(title(right), undefined, { sensitivity: "base" });
  const result = assignments.reduce((groups, assignment) => {
    if (assignment.validFrom > referenceDate) groups.future.push(assignment);
    else if (assignment.validUntil && assignment.validUntil < referenceDate) groups.history.push(assignment);
    else groups.active.push(assignment);
    return groups;
  }, { active: [], future: [], history: [] });

  result.active.sort((left, right) => byTitle(left, right) || left.validFrom.localeCompare(right.validFrom));
  result.future.sort((left, right) => left.validFrom.localeCompare(right.validFrom) || byTitle(left, right));
  result.history.sort((left, right) => (right.validUntil ?? "").localeCompare(left.validUntil ?? "") || byTitle(left, right));
  return result;
}
