# KGV_Fortschrittslog_ausfuehrlich

---

## 2026-03-22 – Prompt 4/5: Arbeitseinsätze-Verwaltung produktiv an `arbeitseinsatz` angeschlossen, inklusive Sonderregeln für Teilnehmergrenze und Stundenwert

- Den aktuellen Istzustand der vorbereiteten Arbeitseinsätze-Verwaltung vor dem Umbau erneut geprüft: `ArbeitseinsaetzeVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen Verwaltungsgerüst abgeleitet, die Liste kam nur aus dem Startseiten-Lesepfad, und rechts gab es noch keinen produktiven Editor mit bestätigten Basistabellenfeldern.
- Den bestätigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestätigten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `beschreibung`
  - `datum` *(Pflichtfeld)*
  - `start_uhrzeit`
  - `end_uhrzeit`
  - `treffpunkt`
  - `max_teilnehmer`
  - `stunden_wert`
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `anmeldung_bis`
  - `aktiv`
- Technische Felder wie `created_at` und `updated_at` sowie das technische Flag `is_demo` werden nicht als normale UI-Bearbeitungsfelder in den Vordergrund gestellt; `is_demo` wird im Produktpfad intern erhalten und nicht spekulativ umgedeutet.
- Gemeinsamen produktiven Basistabellenpfad ergänzt:
  - `GetArbeitseinsaetzeVerwaltungAsync()`
  - `CreateArbeitseinsatzAsync(...)`
  - `UpdateArbeitseinsatzAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `arbeitseinsatz`; es wird ausdrücklich nicht gegen `v_startseite_arbeitseinsatz` geschrieben.
- Die linke Verwaltungsseite nutzt damit jetzt reale Datensätze aus `arbeitseinsatz` statt einer bloßen Home-/View-Strukturableitung. Dadurch bleiben auch inaktive oder intern markierte Datensätze im Admin-/Vorstandskontext sauber bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick öffnet den Editor mit echten Werten
  - `Neu` öffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollständig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- Sonderregel `max_teilnehmer` fachlich sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - in der UI gibt es einen expliziten Zustand `ohne Begrenzung`
  - in diesem Zustand bleibt das eigentliche Zahlenfeld unsichtbar
  - gespeichert wird dann `NULL`, nie `0`
  - sobald eine Begrenzung aktiv ist, muss `max_teilnehmer > 0` gelten
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - leere Eingabe bleibt im Editor als optionaler Zustand zulässig
  - beim Speichern wird der DB-konforme operative Wert `0` verwendet, damit der NOT-NULL-/Default-Vertrag sauber eingehalten bleibt
  - eingegebene Werte müssen `>= 0` sein
- Bestätigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungültig
  - `Sichtbar bis < Sichtbar ab` ist ungültig
  - `max_teilnehmer <= 0` bei aktiver Begrenzung ist ungültig
  - `stunden_wert < 0` ist ungültig
  - `anmeldung_bis` wird als optionaler Timestamp mit vollständigem Datum+Uhrzeit-Paar geprüft, sobald das Feld genutzt wird
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe zentrale tolerante Zeitlogik für `Startuhrzeit`, `Enduhrzeit`, `Sichtbar ab`, `Sichtbar bis` und `Anmeldung bis`
- Für `Stundenwert` wurde ergänzend eine kleine tolerante numerische Parse-Logik eingebracht, damit Eingaben kulturrobust verarbeitet werden, ohne daraus ein neues größeres Shared-Parsing-Subsystem zu machen.
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Änderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer Aufräumcheck bewusst klein gehalten: die ältere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; größere Bereinigung wird nicht in diesen Block hineingezogen.
- Offene Punkte für den letzten Paritäts-/Aufräumblock: die drei produktiven Verwaltungseditoren stehen jetzt, offen bleibt vor allem die kleine Konsolidierung/Parität der verbliebenen Altstrukturen und der anschließende Abschluss-/Aufräumpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Arbeitseinsätze-Block erfolgreich.

## 2026-03-22 – Prompt 3/5: Bekanntmachungen-Verwaltung produktiv an `bekanntmachung` angeschlossen, inklusive kleinem HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung vor dem Umbau erneut geprüft: `BekanntmachungenVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen Verwaltungsgerüst abgeleitet, die Listenladung lief nur über den Startseiten-Lesepfad, und es gab noch keinen produktiven Editor für `inhalt_html`.
- Den bestätigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestätigten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `inhalt_html` *(Pflichtfeld)*
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `sort_order`
  - `aktiv`
- Technische Felder wie `created_at` und `updated_at` bleiben weiterhin bewusst außerhalb der normalen Bearbeitung; es wurden keine zusätzlichen Fantasiefelder ergänzt.
- Gemeinsamen produktiven Basistabellenpfad ergänzt:
  - `GetBekanntmachungenVerwaltungAsync()`
  - `CreateBekanntmachungAsync(...)`
  - `UpdateBekanntmachungAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `bekanntmachung`; es wird ausdrücklich nicht gegen `v_startseite_bekanntmachungen` geschrieben.
- Die linke Verwaltungsseite nutzt jetzt reale Datensätze aus `bekanntmachung` statt einer Home-/View-Strukturableitung. Dadurch bleibt die Admin-/Vorstandsverwaltung auch für noch nicht sichtbare oder inaktive Bekanntmachungen fachlich korrekt.
- Das rechte Verhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick öffnet den Editor mit echten Werten
  - `Neu` öffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollständig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- HTML-Editor-Entscheidung bewusst klein und kontrolliert gehalten: im Repo gab es vor dem Block keine belastbare kleine HTML-/Preview-Komponente und keine bereits genutzte leichte Editorabhängigkeit. Deshalb wurde keine schwere neue Editor-Architektur eingeführt, sondern eine produktive Bordmittel-Lösung umgesetzt:
  - HTML-Quellbearbeitung in einem dedizierten Editorbereich
  - kleine Snippet-Leiste für häufige HTML-Bausteine (`Absatz`, `Überschrift`, `Fett`, `Link`, `Liste`)
  - integrierte Live-Vorschau über den vorhandenen WPF-`WebBrowser`
- Damit bleibt `inhalt_html` nicht auf ein bloßes Plaintext-Endfeld reduziert, ohne einen übergroßen Richtext-Baukasten neu zu erfinden.
- Bestätigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `inhalt_html` darf nicht leer oder nur Leerzeichen sein
  - `sichtbar_bis < sichtbar_ab` ist ungültig
  - für `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit gemeinsam benötigt, sobald eines davon befüllt wird, damit keine stillen Timestamp-Annahmen entstehen
  - `sort_order` ist optional, muss aber bei Eingabe eine ganze Zahl sein
- Das Validierungs-/Fokusmuster aus `Termine` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe tolerante Zeitlogik für die Sichtbarkeits-Timestamps
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Änderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer Aufräumcheck bewusst klein gehalten: die ältere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht mit HTML-Vorschau; eine Bereinigung kann später separat folgen, ohne diesen Block aufzublähen.
- Offene Punkte für das nächste Modul: `Arbeitseinsätze` sind weiterhin das letzte der drei Home-nahen Verwaltungsfelder ohne bestätigten produktiven Editor; dort fehlen im aktiven Stand noch die sauber verifizierten Schreibfelder/-pfade analog zu `termin` und `bekanntmachung`.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block erfolgreich.

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
