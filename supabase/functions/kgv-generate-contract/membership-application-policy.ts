export function usesPersistentMembershipSignatures(action: string, fieldSpecVersion: string) {
  return action === "finalize" && (
    fieldSpecVersion === "mitgliedsantrag-field-spec/v4" ||
    fieldSpecVersion === "mitgliedsantrag-field-spec/v5"
  );
}
