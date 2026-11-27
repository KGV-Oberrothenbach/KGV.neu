// Values of v10 fields whose printed labels already include units.
// This helper does not authorize or resolve contract parties.
export function buildLeaseV10Fields({ area, rate, annual, hasSecondParty, isRepresentative, primaryAddress, secondaryAddress }) {
  if (![area, rate, annual].every(Number.isFinite) || area <= 0 || rate <= 0 || annual < 0) throw new Error('Ungültige Pachtvertragsbeträge');
  if (isRepresentative && !hasSecondParty) throw new Error('Gesetzliche Vertretung fehlt');
  const number = new Intl.NumberFormat('de-DE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const areaNumber = new Intl.NumberFormat('de-DE', { useGrouping: false, maximumFractionDigits: 8 });
  const role = hasSecondParty ? (isRepresentative ? 'Gesetzl. Vertretung' : 'Mitpächter/in 2') : '';
  return {
    flaeche_qm: areaNumber.format(area),
    flaeche_qm_wiederholung: areaNumber.format(area),
    pacht_pro_qm_zahl: number.format(rate),
    pachtzins_jahr_zahl: number.format(annual),
    zweite_partei_rolle: role || 'Keine zweite Partei',
    zweite_partei_signatur_rolle: role,
    paechter_anschrift: String(primaryAddress ?? '').replace(/, /g, '\n'),
    zweite_partei_anschrift: hasSecondParty ? String(secondaryAddress ?? '').replace(/, /g, '\n') : '',
    // There is no authoritative structured source for these optional fields.
    lage_beschreibung: '',
    flaechenbasis_nachweis: '',
  };
}
