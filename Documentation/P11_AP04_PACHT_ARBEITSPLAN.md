# P11 – AP-04 Pacht-Arbeitsplan

## AP-04.1 – Pachtende, Mitgliedschaftsstatus und Pächterbeteiligungen

### Befund und Umsetzung

Das rechtliche Pachtende wird im bestehenden Modell durch
`pachtverhaeltnis.beendet_am` gespeichert; `parzellen_belegung` ist nur die
getrennte operative Zuordnung. Vor AP-04.1 fehlte ein autorisierter,
transaktionaler Abschlussweg, der das Vertragsende, die
Beteiligungshistorie und P11-R06 gemeinsam verarbeitet.

Migration `20261113120000_p11_ap04a_lease_end_membership_status.sql` führt
`beende_pachtverhaeltnis` ein. Sie verlangt effektives
`CanCreateMitglied` (das bestehende Pachtvertrags-Fachrecht), ein aktuelles
Enddatum und einen Grund, beendet die Vertragsbeziehung mit Audit und schreibt
die Beteiligungs-Enddaten. Nur ein Hauptpächter ohne weitere wirksame
Pachtbeteiligung wird passiv (`aktiv=false`); `mitglied_ende`, App-Zugang und
Nebenmitgliedschaft bleiben unverändert. Gleiches Enddatum ist idempotent,
ein abweichendes zweites Enddatum wird abgewiesen. Ein Mitpächter übernimmt
keinen Altvertrag; die bestehende Erstvertragslogik verlangt weiterhin einen
neuen Vertrag mit Hauptpächter.

### Nachweis

Der lokale Neuaufbau einschließlich Seed bestand mit 217 Migrationen. Die
rückrollbare `authenticated`-Fixture
`20261113120000_p11_ap04a_lease_end_membership_status_funktionstest.sql`
bestätigt fehlendes Recht, die Passivstellung nach letzter Hauptpacht,
erhaltenes Mitgliedschaftsende/App-Zugang/Nebenmitglied, Audit und Retry.

**Abnahme AP-04.1:** vollständig abgenommen. Nachsorge, Übergabeprotokolle,
historische Korrekturen und weitere Pachtwechsel bleiben spätere AP-04-Pakete.
B-01 bleibt offen.
