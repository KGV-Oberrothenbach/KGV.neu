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

## AP-04.2 – Nachsorge und Übergabeprotokolle

### Befund und Umsetzung

Die Nachsorgegrundstruktur aus `20261113130000` wurde gezielt ergänzt. Die
Abschluss-RPC `beende_pacht_nachsorge` bindet die Entscheidung an das
bestehende effektive Pachtvertragsrecht (`CanCreateMitglied`), verlangt Art
und Begründung und schreibt Akteur/Zeitpunkt sowohl an der Nachsorge als auch
unveränderlich in `pacht_nachsorge_ereignis`. Bereits abgeschlossene
Nachsorgen sind nicht reaktivierbar; Wiederholungen sind idempotent.

Die neue Triggerkette beendet offene Nachsorge ausschließlich innerhalb der
tatsächlichen Anlage bzw. Wirksamwerdung einer Hauptpächterbeteiligung auf
derselben Parzelle. Künftige Beteiligungen bleiben wirkungslos. Die
serverseitige Zuordnung eines vorhandenen abgeschlossenen Rückgabe- oder
Übernahmeprotokolls prüft Parzelle und rechtlich beendetes Pachtverhältnis und
auditert die Zuordnung, ohne Zählerstände, Fotos oder Dokumentreferenzen zu
kopieren oder zu verändern. Mitgliedschaft, App-Zugang und der bereits
beendete Altvertrag werden durch diese Wege nicht verändert.

### Nachweis

Vollständiger lokaler Supabase-Neuaufbau einschließlich Seed erfolgreich;
die aktiven Migrationen reichen bis
`20261113133000_p11_ap04b_aftercare_audit_field_correction.sql`. Die
rückrollbare `authenticated`-Fixture
`20261113131000_p11_ap04b_aftercare_funktionstest.sql` bestätigt fehlendes
Recht einschließlich Admin-Entzug, Bindung an einen rechtlich beendeten
Altvertrag, Zweijahresgrenze, abgewiesene geplante Übernahme, automatischen
Abschluss bei wirksamer Hauptpächterübernahme, Audit, Protokollzuordnung und
idempotenten Wiederholungsabschluss.

**Abnahme AP-04.2:** Datenbankvertrag und RLS-Nachweis abgenommen; die
bestehende Parzellenprotokoll-Oberfläche wird weiterverwendet. B-01 bleibt
offen.
