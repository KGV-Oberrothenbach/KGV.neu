# KGV_Fortschrittslog_ausfuehrlich

---

## 2026-03-22 – Prompt 2/5: Termine-Verwaltung als erster echter Editor produktiv an `termin` angeschlossen

- Den aktuellen Istzustand der vorbereiteten Termine-Verwaltung vor dem Umbau erneut geprüft: `TermineVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen Verwaltungsgerüst abgeleitet, die Listenladung lief nicht über die bestätigte Basistabelle `termin`, und im rechten Bereich gab es noch keinen produktiven Editor mit bestätigten Feldern.
- Den jetzt bestätigten Tabellenvertrag von `termin` direkt in die produktive WPF-Bearbeitung überführt. Bearbeitet werden genau die verifizierten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `beschreibung`
  - `datum` *(Pflichtfeld)*
  - `start_uhrzeit`
  - `end_uhrzeit`
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `aktiv`
- Technische Spalten wie `created_at` und `updated_at` bleiben bewusst außerhalb der normalen Editoroberfläche; es wurden keine zusätzlichen oder geratenen Felder ergänzt.
- Gemeinsamen produktiven Servicepfad für die Basistabelle ergänzt: `ISupabaseService`/`SupabaseService` laden die Verwaltungs-Liste jetzt direkt aus `termin` und schreiben neue/geänderte Datensätze per `CreateTerminAsync(...)` und `UpdateTerminAsync(...)` wieder gegen `termin` zurück. Es wird ausdrücklich nicht gegen `v_startseite_termine` geschrieben.
- Die Terminliste links zeigt jetzt reale Datensätze aus der Basistabelle statt der bisherigen reinen Strukturvorbereitung; dadurch sind auch nicht für Home gedachte Verwaltungszustände wie `aktiv = false` im Admin-/Vorstandskontext korrekt bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick auf einen vorhandenen Termin öffnet rechts den echten Editor mit den geladenen Werten
  - `Neu` öffnet einen leeren Editorzustand mit fachlich sinnvollem Default `aktiv = true`
  - `Abbrechen` verwirft den Bearbeitungszustand wieder vollständig
  - `Speichern` steht wie gefordert am Ende des Formulars
- Bestätigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungültig
  - `Sichtbar bis < Sichtbar ab` ist ungültig
  - für `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit jeweils gemeinsam benötigt, sobald das Feld befüllt wird, damit kein stiller Timestamp-Anteil geraten wird
- Wiederverwendbares Eingabe-/Validierungsmuster für Folgeeditoren vorbereitet:
  - Pflicht- und Fehlerfelder werden rot markiert
  - beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben
  - tolerante Zeiteingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert
  - offensichtlich ungültige Zeiten bleiben sichtbar und werden nicht still geleert oder heimlich korrigiert
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut im Editor geöffnet, damit Änderungen direkt nachvollziehbar bleiben.
- MAUI in diesem Block bewusst nicht mit Platzhalterseiten erweitert; durch den gemeinsamen produktiven Servicepfad für `termin` ist die spätere mobile Parität aber nicht verbaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Termin-Block erfolgreich.

## 2026-03-22 – Prompt 1/5: Home nur mit Admin-Bearbeiten-Einstiegen, separate Verwaltungsviews ohne Platzhalter vorbereitet

- Aktuellen WPF-/MAUI-Istzustand vor dem Umbau erneut geprüft: `HomeView`/`HomeViewModel` zeigten bereits nur noch Listen plus separate Detailviews, `NavigationService` und `MainWindowViewModel` waren ViewModel-first organisiert, und die bestätigten Lesepfade für Home liefen über `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen`.
- Im aktiven Repo zugleich keine belastbar verifizierten Schreibmodelle oder bestehenden Create/Update-Servicepfade für Arbeitseinsätze, Termine und Bekanntmachungen gefunden; genau deshalb wurde in diesem Block bewusst keine neue Formularlogik geraten.
- Home fachlich weiter bereinigt: auf der Startseite gibt es für normale Nutzer weiterhin nur die bisherigen Übersichtslisten; für Admin/Vorstand kommt jetzt zusätzlich eine kleine Verwaltungssektion mit drei Bearbeiten-Einstiegen hinzu, aber keine Bearbeitung direkt auf Home.
- Drei echte separate WPF-Verwaltungsviews angelegt und in die bestehende Navigation eingebunden:
  - `ArbeitseinsaetzeVerwaltungView`
  - `TermineVerwaltungView`
  - `BekanntmachungenVerwaltungView`
- Jede Verwaltungsansicht nutzt bereits das Ziel-Layout mit linker Liste und rechter Editorfläche. Wenn weder ein vorhandener Datensatz geöffnet noch `Neu` ausgelöst wurde, bleibt rechts bewusst ohne Formularfelder. Ein Doppelklick auf einen Listeneintrag öffnet rechts den Editierzustand mit den vorhandenen belastbaren Lesedaten; `Neu` öffnet denselben strukturellen Zustand leer.
- Keine Platzhalterformulare aufgebaut: weil die echten Schreibfelder/Basistabellen im aktiven Repo noch nicht sicher genug ableitbar waren, zeigt der rechte Bereich aktuell nur den geöffneten Bearbeitungszustand plus die verifizierten Lese-/Schreibpfad-Hinweise statt geratener Eingabefelder.
- Reale Datenlisten statt Platzhalter verdrahtet: `ISupabaseService`/`SupabaseService` stellen die drei bestätigten Startseiten-Lesepfade jetzt auch direkt für Verwaltungslisten bereit, sodass die neuen Views auf belastbaren Daten basieren und nicht auf Home-spezifischen Zwischenobjekten.
- Rechte entlang des vorhandenen Pfads sauber eingehängt: die Verwaltungsviews erscheinen in WPF nur für Admin/Vorstand sowohl über Home als auch in der Hauptnavigation; MAUI bleibt in diesem Block unverändert und wird nicht mit neuen Platzhalterseiten aufgebläht, profitiert aber später vom gemeinsamen Lesepfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block erfolgreich.
