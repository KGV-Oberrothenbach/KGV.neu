"use client";
import type { PreparedContractAuthorizedSignatory } from "../../repositories/contracts/prepared-contract-signature-repository";
import { preparedContractSignatoryLabel } from "../../services/contracts/prepared-contract-signatory-service";

export function PreparedContractSignatorySelector({ label, options, value, excludeMemberId, onChange }: {
  label: string;
  options: PreparedContractAuthorizedSignatory[];
  value: PreparedContractAuthorizedSignatory | null;
  excludeMemberId?: number | null;
  onChange: (value: PreparedContractAuthorizedSignatory | null) => void;
}) {
  const selectable = options.filter(option => option.mitglied_id !== excludeMemberId);
  const valueKey = value ? `${value.mitglied_id}:${value.vereinsfunktion_besetzung_id}:${value.vertretung_id ?? ""}` : "";
  return <label>{label}<select value={valueKey} onChange={event => {
    const selected = selectable.find(option => `${option.mitglied_id}:${option.vereinsfunktion_besetzung_id}:${option.vertretung_id ?? ""}` === event.target.value) ?? null;
    onChange(selected);
  }}><option value="">Bitte auswählen</option>{selectable.map(option => <option key={`${option.mitglied_id}:${option.vereinsfunktion_besetzung_id}:${option.vertretung_id ?? ""}`} value={`${option.mitglied_id}:${option.vereinsfunktion_besetzung_id}:${option.vertretung_id ?? ""}`}>{preparedContractSignatoryLabel(option)}</option>)}</select></label>;
}
