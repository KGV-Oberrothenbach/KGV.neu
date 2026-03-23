# KGV Entwicklungslog

---

## 2026-03-23 – Insert-Pfade auf explizite ID-Mitgabe geprüft: produktive Neuanlagen senden aktuell keine feste ID aus der App

- Den aktuellen produktiven Insert-/Create-Stand zuerst gegen den realen Arbeitsbaum geprüft statt auf Verdacht umzubauen: gesichtet wurden `SupabaseService`, die betroffenen Records/Modelle, die WPF-/MAUI-Neuanlagepfade für `arbeitsstunde`, `termin`, `bekanntmachung` und `arbeitseinsatz` sowie ein möglicher Pfad für `arbeitseinsatz_anmeldung`.
- Für `arbeitsstunde` den konkreten Verdacht am echten Produktpfad geprüft: WPF (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) bauen neue `ArbeitsstundeRecord`-Instanzen ohne gesetzte `Id`; `AddArbeitsstundeAsync(...)` erstellt daraus wiederum ein neues Insert-Objekt ebenfalls ohne `Id`-Zuweisung und sendet nur die fachlichen Felder an PostgREST.
- Für die drei Verwaltungs-Neuanlagen denselben Punkt abgesichert: die WPF-Editoren bauen im New-Mode zwar Arbeitsobjekte mit `Id = 0` als lokalem Defaultwert, aber die produktiven Service-Create-Pfade `CreateTerminAsync(...)`, `CreateBekanntmachungAsync(...)` und `CreateArbeitseinsatzAsync(...)` übernehmen diese `Id` gerade **nicht** in ihre tatsächlichen Insert-Objekte. Gesendet werden dort nur die fachlichen Spalten; Updates laufen weiterhin separat über die vorhandene Datensatz-`Id`.
- Einen echten produktiven Insert-Pfad für `arbeitseinsatz_anmeldung` gibt es im aktuellen Repo weiterhin nicht: der vorhandene `Anmelden`-Pfad auf Home ist nach wie vor nur eine ehrliche Info-Aktion ohne Schreibzugriff. Entsprechend war dort aktuell auch kein produktiver ID-Insertpfad zu korrigieren.
- Zusätzlich den Library-Unterbau gegen den aktuellen Paketstand geprüft: im verwendeten `Supabase.Postgrest`-Paket ist beim `PrimaryKey`-Attribut der Parameter `shouldInsert` standardmäßig `false`. Damit wird die Primärschlüsselspalte auch beim verbleibenden Muster `[PrimaryKey("id")]` nicht automatisch in Insert-Payloads aufgenommen, solange sie nicht explizit gesetzt und bewusst anderweitig serialisiert wird.
- Ergebnis der Prüfung: im aktuellen produktiven App-Code ließ sich für die geprüften Neuanlagepfade keine Stelle nachweisen, an der bei Inserts eine feste `id` oder konkret `id = 0` aus der App mitgesendet wird.
- Deshalb bewusst **kein** Vermutungsfix eingebaut: keine `lastrow+1`-Logik, keine unnötige Schatten-Insertarchitektur und keine DB-Migrationsänderung auf Verdacht. Der Block bleibt bei der belastbaren Analyse und Dokumentation des aktuell bereits korrekten Insert-Musters `Insert ohne feste ID, Update mit bestehender ID`.
- Repo-seitig zusätzlich verifiziert: im aktuellen Arbeitsstand liegen keine versionierten SQL-/Migrationsdateien für die betroffenen Tabellen vor; eine DB-seitige Sequence-/Default-Prüfung war im Repo daher nicht weiter nachvollziehbar, wurde aber für diesen Block auch nicht benötigt, weil auf App-Seite kein fehlerhafter ID-Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wird durch diesen Dokumentations-/Analyseblock nicht beschädigt.

## 2026-03-23 – Home-/Detail-Block für Arbeitseinsätze sauber abgeschlossen: Beginn sichtbar, Detaildaten vervollständigt, Anmelden ehrlich nur informativ

- Den begonnenen Home-/Detail-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufzuziehen: bereits halbfertig vorhanden waren erweiterte Home-/Detailmodelle, ein vorbereiteter optionaler `Anmelden`-Pfad im Detailkontext und erste Anpassungen im `HomeViewModel`, aber die sichtbaren WPF-XAML-Schritte und der hintere Helper-Tail des `SupabaseService` waren noch nicht sauber abgeschlossen.
- Den Home-/Detailpfad für `Arbeitseinsatz`, `Termin` und `Bekanntmachung` gemeinsam nachgezogen, damit die generische Detailansicht durch die Erweiterung keine Felder verliert: zusätzliche Datensatzinformationen laufen jetzt konsistent über `DetailInfo`/`AdditionalInfo`, statt nur bei Arbeitseinsätzen Sonderwissen lokal in der View zu hinterlegen.
- `Arbeitseinsatz` zeigt auf Home jetzt den Beginn explizit sichtbar im Karteneintrag; zusätzlich gibt es dort einen eigenen `Details`-Button und – nur wenn der Datensatz überhaupt anmeldbar markiert ist – einen `Anmelden`-Button.
- Die Home-Detailansicht zeigt für `Arbeitseinsatz` jetzt neben Titel/Untertitel auch die übrigen im Datensatz belastbar vorhandenen Informationen wie Thema, Datum, Beginn, Ende, Treffpunkt sowie Kapazitäts-/Anmeldeinformationen; `Max. Teilnehmer` wird bewusst nur dann angezeigt, wenn sie aus den vorhandenen Datensatzwerten überhaupt belastbar ableitbar ist.
- `Termin`- und `Bekanntmachung`-Detailansichten wurden im selben Pfad mitkonsolidiert: zusätzliche Felder wie Ort, Thema, Betreff, Veröffentlichungszeitpunkt oder Kurztext werden jetzt ebenfalls über denselben Detailkontext transportiert, statt in der generischen Detailview verloren zu gehen.
- Den halbfertig beschädigten Tail des `SupabaseService` sauber wieder geschlossen und dabei die kleinen Home-Helfer zentral konsolidiert (`AddDetailLine`, Datum-/Zeitformatierung, Textnormalisierung, Dokumentpfad-Helfer etc.), damit der angefangene Block wieder auf einem kompilierbaren produktiven Stand steht.
- Den `Anmelden`-Usecase bewusst **nicht** als neue Schattenlogik eröffnet: im aktuellen Repo ließ sich weiterhin kein belastbarer produktiver Schreibpfad für eine echte Arbeitseinsatz-Anmeldung finden. Deshalb ist der neue `Anmelden`-Button in WPF nur als ehrliche Info-Aktion verdrahtet und weist explizit darauf hin, dass der echte Schreibpfad noch nicht angebunden ist.
- WPF wurde sichtbar fertiggestellt; MAUI wurde bei den gemeinsamen Home-Items/Mappingerweiterungen mitgedacht, aber bewusst nicht mit einer neuen Arbeitseinsatz-Anmeldeoberfläche erweitert, solange dafür kein belastbarer gemeinsamer Fachpfad existiert.
- Technisch verifiziert: Workspace-Build erfolgreich; der begonnene Home-/Detail-Block ist damit ohne erfundene Anmeldefachlogik sauber abgeschlossen.

## 2026-03-23 – Verwaltungseditoren und Home-Navigation nachgezogen: Zurück-Text repariert, Doppelklick auf Listen, Arbeitsstunden-Erfassung nur noch über Home

- Die drei WPF-Verwaltungseditoren `Arbeitseinsätze`, `Termine` und `Bekanntmachungen` nach dem Parserfix nochmals produktiv bereinigt: der beschädigte Toolbar-Text `Zurück` wird jetzt XAML-seitig stabil als Entity geschrieben, damit kein erneutes Zeichensatzproblem im Buttontext entsteht.
- Für alle drei Verwaltungslisten einen echten Doppelklickpfad auf die ausgewählten Einträge ergänzt: statt auf `InputBindings` zu vertrauen, öffnen die `ListBox`-Einträge jetzt direkt per `MouseDoubleClick` den vorhandenen `OeffnenCommand` und damit den Bearbeitungseditor zuverlässig.
- Die WPF-Hauptnavigation wieder auf den gewünschten Produktpfad zurückgezogen: `Arbeitsstunden erfassen` wird nicht mehr als eigener Navigationseintrag aufgebaut und bleibt nur noch über den vorhandenen Button auf der `Startseite` erreichbar.

## 2026-03-23 – Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsächliche XAML-Ursache zurückgeführt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geöffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungültigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewünschte Doppelklickpfad bleibt weiterhin korrekt über die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 – Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, Zurück-/Dirty-Check ergänzt, Navigation auf Home-only zurückgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`Arbeitseinsätze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprüft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu eröffnen.
- Der reproduzierbare Verschiebungsfehler ließ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklärte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter für PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusätzliche zentrale Normalisierung im `SupabaseService` für geladene Verwaltungsdatensätze sowie für die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - Rückkehr erfolgt über den vorhandenen Navigationspfad zurück zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `Zurück`-Button
  - `Zurück` und `Abbrechen` prüfen auf echte ungespeicherte Änderungen
  - die Rückfrage erscheint nur bei tatsächlichen Änderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewünschten Produktpfad zurückgezogen:
  - `Arbeitseinsätze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch über die vorhandenen Home-Buttons für Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer Verwaltungsoberfläche erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusätzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 – Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, Prüftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprüft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue Prüftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im Prüfkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen über `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen über `status`; die globale Prüfsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen Prüfpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr künstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld für Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frühere Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(älteste zuerst)*
  - pro Zeile sichtbare Prüfinformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsächlich geänderte oder zur Freigabe markierte Zeilen; unbearbeitete offene Datensätze bleiben unverändert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prüfenden Benutzers gesetzt. Nicht freigegebene, aber geänderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im Prüf-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall öffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusätzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Öffnen der Freigabeansicht wird eine globale Prüfsperre über die offenen `arbeitstunde`-Datensätze gesetzt
  - andere Prüfer erhalten bei aktiver Fremdsperre nur eine saubere Rückmeldung statt paralleler produktiver Bearbeitung
  - soweit auflösbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlängert die Sperre während der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hängende Locks laufen nach Timeout kontrolliert aus und sind danach übernehmbar
- Die bestehende Badge-/Zählerlogik bleibt weiterverwendet: Änderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer Freigabeoberfläche erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 – Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprüft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene Prüfgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad für offene Fälle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter über `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter über `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin über `ArbeitsstundenChangedMessage` verteilt.
- Für normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergänzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow für `Datum`, `Stunden` und `Art der Arbeit` – ohne zusätzliche Fachfelder und ohne neue Freigabeoberfläche.
- Der neue Einstieg ist bewusst sichtbar und eigenständig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` für Benutzer mit eigenem Mitgliedskontext; zusätzlich erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View führt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung für den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. Für `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und größer als `0` sein.
- Für Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende Prüf-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-Adminmenü zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene Datensätze mit `freigegeben = false` vorhanden sind; der Badge/Zähler zeigt weiter die tatsächliche Anzahl offener Prüffälle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prüfen` wurde entlang der betroffenen Navigations-/Titelflächen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 – Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprüft: `Termine`, `Bekanntmachungen` und `Arbeitseinsätze` waren bereits produktiv an ihre Basistabellen angebunden, während im Repo noch die älteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschärft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsächlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch überholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehöriger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frühere gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprüft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschließlich für Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt für spätere MAUI-Parität anschlussfähig: die produktiven Lese-/Create-/Update-Pfade für `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollständig im plattformneutralen Shared-Core-/Infrastructure-Bereich, während die eigentlichen Editoroberflächen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik eröffnet: der Block blieb bewusst bei Konsolidierung, Aufräumen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergänzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 – Arbeitseinsätze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln für Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten Arbeitseinsätze-Verwaltung erneut geprüft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste über `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestätigter Schreibpfad gegen die Basistabelle.
- Den bestätigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergänzt und nicht über Startseiten-Views geschrieben: gemeinsamer Service lädt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geänderte Datensätze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurück.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch für `Arbeitseinsätze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert größer als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe führt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` geführt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestätigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusätzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergänzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen Aufräumcheck bewusst klein gehalten: die ältere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; größere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Arbeitseinsätze-Block weiterhin erfolgreich.

## 2026-03-22 – Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprüft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste über die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestätigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestätigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergänzt und nicht über Home-Views geschrieben: gemeinsamer Service lädt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geänderte Datensätze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurück.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungültige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- Für `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingeführt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `Überschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergänzt und parallel in einer kompakten Vorschau geprüft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusätzlichen fachlichen Sortierregeln erfunden. Für neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestätigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergänzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner Aufräumcheck bewusst klein gehalten: die ältere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nächsten Aufräumschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 – Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprüft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestätigten Schreibpfad auf die Basistabelle.
- Den bestätigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestätigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst außerhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lädt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geänderte Datensätze direkt per Create/Update wieder nach `termin` zurück.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz öffnet rechts den Editor mit echten Werten, `Neu` öffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollständig, und `Speichern` bleibt am Ende des Formulars.
- Bestätigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- Für Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergänzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungültige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster für Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit für `Bekanntmachungen` und `Arbeitseinsätze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergänzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 – Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprüft: Home zeigt bereits nur Listen und separate Detailviews, aber für Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur für `Arbeitseinsätze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services für diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die Übersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusätzlich nur drei kleine Bearbeiten-Einstiege (`Arbeitseinsätze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- Für WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter Editorfläche.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: über neue gemeinsame Servicezugriffe werden die bestätigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch für die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geöffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geöffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehängt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur für Admin/Vorstand verfügbar und blähen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber für spätere MAUI-Parität nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 – Nachtrag Home-Diagnose: aktuell laden stabil nur Arbeitseinsätze und Termine

- Im aktuellen WPF-Nachtest bestätigt: `Arbeitseinsätze` und `Termine` laden über die Startseiten-Views stabil; die verbleibenden Auffälligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlägt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. Maßgeblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestätigte View.
- Für `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irreführender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die Restprüfung bleibt daher auf tatsächliche Rückgabefelder bzw. reale Datensätze der View fokussiert.
- Der UX-Nachzug für Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von Arbeitseinsätzen, Terminen und Bekanntmachungen öffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 – Home-UX-Nachzug: Startseite zeigt nur Listen, Details öffnen in eigener View

- Den Home-Stand für Arbeitseinsätze, Termine und Bekanntmachungen auf den gewünschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/Kartenübersichten, während Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- Dafür eine eigenständige WPF-Detailansicht für Home-Elemente ergänzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Öffnen-Kommandos für Arbeitseinsätze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-Detailzustände in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige für Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergänzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurückgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzuführen.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur Teil 3: Ursachenanalyse für verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprüft. Die entscheidende technische Ursache für den Pflichtstundenfehler ließ sich jetzt direkt aus dem WPF-Debug-Log bestätigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestätigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, während `Arbeitseinsätze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird für Home jetzt zunächst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschließend robust clientseitig über den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung für Pflichtstunden ergänzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestätigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- Für Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irreführenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurückgelieferte Einträge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusätzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergänzt: der Home-Pfad schreibt jetzt für Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusätzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur Teil 2: Pflichtstunden über Hauptmitglied/Saison präzisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprüft: da `Arbeitseinsätze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen – Pflichtstunden über den falschen Mitgliedsbezug bzw. zu grobe Saisonauflösung und Bekanntmachungen über ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad für Home deshalb final präzisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der für Home relevante Hauptmitgliedsbezug aufgelöst; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die Pflichtstundenübersicht über dieses Hauptmitglied geladen.
- Zusätzlich wird die bestätigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berücksichtigt, sondern zuerst direkt für die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fällt erst danach auf Jahres-/letzten Datensatz zurück.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusätzlich mögliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping für Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette für Titel, Inhalt und Veröffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverändert klein, zeigt aber reale View-Daten robuster an, statt früh in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprüft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fällt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurück.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestätigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhängig vom konkreten DB-/Session-Kontext ist damit bestätigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, Arbeitseinsätze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fällt nicht mehr komplett auf leere Platzhalter zurück.
- Kleine, gezielte technische Transparenz ergänzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusätzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte für die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusätzlich vereinfacht und näher auf den bestätigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt für das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusätzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehört – im bestätigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 – Home-Diagnose: bestätigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestätigten DB-Schema-Stand geprüft; dabei zeigte sich, dass die Home-Anbindung zwar grundsätzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestätigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestätigten Kern geschärft, ohne neue App-Logik einzuführen: `PflichtstundenUebersichtRecord` bildet jetzt zusätzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` löst zuerst die aktuelle Saison über `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose über ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusätzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht künstlich verloren gehen; der wesentliche Datenverlust lag im geprüften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 3: Letzte kleine Restlücke über MAUI-Prüfstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei möglichen Abschlussoptionen geprüft: ein echter gemeinsamer Ablehnungs-/Rückgabe-/Kommentarpfad wäre trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafür im aktiven Modell und Servicepfad keine saubere fachliche Rückgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-Prüfpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewählt und den vorhandenen mobilen Prüfpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prüfen` mobil jetzt nur noch dann an, wenn über den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsächlich offene Prüffälle vorliegen; zusätzlich erscheint die aktuelle Anzahl offener Datensätze direkt im Titel des Menüpunkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder über denselben gemeinsamen Prüfpfad, sodass Menüeintrag und tatsächliche offene Fälle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingeführt: die mobile Seite bleibt vollständig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen Prüfstatusanzeige draußen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-Prüfflow in WPF bis zur Freigabe vervollständigt

- Den aktuellen WPF-Istzustand für `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprüft: belastbar vorhanden waren bereits Prüfliste, Badge/Zähler und die bestehende Bearbeitungsansicht, aber aus der Prüfliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den Prüfpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurückgeführt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prüfen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen Prüfmodus geöffnet, gefiltert auf die tatsächlich offenen Datensätze des ausgewählten Mitglieds.
- Dafür nur einen kleinen Navigationskontext ergänzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert für die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene Prüfeinträge des konkret ausgewählten Mitglieds im Prüfmodus erscheinen.
- Admin/Vorstand können im Prüfmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusätzlich eine direkte `Freigeben`-Aktion für den ausgewählten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/Rückgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell für einen sauberen Rückgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, Prüfliste und Badge/Zähler weiterhin über den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen Prüfservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im Prüfmodus fachlich geschärft: klarer Titel, Hinweistext, leerer Prüfzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte Zustände für Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus Prüfliste und Prüfzähler ausgeschlossen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der Vervollständigung des Prüfflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfähig.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und Prüfstatus in WPF angeschlossen

- Den aktuellen Istzustand für `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation and den gemeinsamen Prüfpfad erneut geprüft: ein belastbarer Listen-/Detailpfad für Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-Prüfstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und öffnet belastbar die bestehende WPF-`ArbeitsstundenView` für das aktuelle Mitglied statt neuer Schattenlogik.
- Dafür den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch für Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlässig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hängt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene Einträge, Vorstand/Admin können wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingeführt.
- Das Arbeitsstunden-Formular geschärft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heißt konsistent `Speichern`.
- Kleinen WPF-Prüfpfad für offene Arbeitsstunden ergänzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und führen von der Prüfliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten Prüfstatus gestellt: der Eintrag `Arbeitsstunden prüfen` erscheint für berechtigte Benutzer jetzt nur bei tatsächlich offenen Prüffällen, zeigt einen kleinen Zähler mit der Zahl offener Datensätze und wird bei offenem Prüfstand sichtbar hervorgehoben; Aktualisierung läuft nach Änderungen über den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus Prüflisten und Prüfzählern ausgeschlossen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfähig.

## 2026-03-22 – WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand für `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklärten produktiven DB-Pfade geprüft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, während `Arbeitseinsätze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/Leerzustände nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublähen: `HomeOverviewDTO` trägt jetzt zusätzlich gemeinsame Startseiten-Daten für Pflichtstunden, Arbeitseinsätze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lädt diese Inhalte direkt aus den geklärten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusätzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) für die Home-Ausgabe mitgeführt.
- Bereich `Arbeitseinsätze` an `v_startseite_arbeitseinsatz` angebunden: echte Einträge mit Titel, Datum/Zeit, Treffpunkt sowie Kapazitäts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafür im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte Einträge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterführende Informationen nur aus den belastbar vorhandenen View-Feldern und lässt sich wieder mit `Schließen` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View für Bekanntmachungen angebunden: echte Einträge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfügbaren Inhalte statt des bisherigen künstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergänzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden für Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzuführen.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird für nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfähig.

## 2026-03-22 – WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand für Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprüft: konkret auffällig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewünschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen größeren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusätzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurückgeführt: `MainWindowViewModel` lädt für den Eigenkontext weiter den nötigen Mitgliedskontext vor, navigiert danach aber standardmäßig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` für das aktuelle Kalenderjahr mit belastbar ableitbaren Werten für freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfügbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewünschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `Arbeitseinsätze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; für `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `Schließen`-Button ergänzt.
- Weiterhin bewusst nicht erfunden: für `Arbeitseinsätze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfähigen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprüft und bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten Prüfpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprüft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen Prüfpfad für Block 6 bewusst genau diesen Core-Baustein gewählt: fachlich wichtig, technisch klar abgegrenzt und ohne zusätzliche Infrastruktur direkt testbar.
- Dafür `UserRolesTests` in die bestehende aktive Teststruktur ergänzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine künstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhängen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 – Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nächsten Prüfpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprüft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, während der gemeinsame Home-Kern trotz hoher fachlicher Relevanz für WPF und MAUI noch ungetestet blieb.
- Als nächsten kleinen Prüfpfad bewusst den `HomeOverviewFactory` gewählt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe für Desktop und Mobil und lässt sich ohne zusätzliche Testinfrastruktur direkt prüfen.
- Dafür `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergänzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets für `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine künstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad für die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengeführt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprüft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frühere WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurückgezogen, sondern sinnvoll zusammengeführt: die aktive Root-Projektdatei übernimmt jetzt wieder die für den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, während der neue `OperationalDataFilter`-Block vollständig erhalten bleibt.
- Fachlich passende ältere Tests sauber zurückgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner Prüfpfad für den aktuellen produktiven Export-Code reaktiviert, statt als bloßes Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden übernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengeführtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende Testansätze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` läuft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 1: Kleine aktive Testbasis für Demo-/Testdatenfilter zurückgeholt

- Die aktuelle Testlage geprüft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die Lösung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ältere Testidee für `ExportViewModel`, die für den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen Prüfpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewählt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-Prüfung) ohne dass dafür eine große Testinfrastruktur nötig wäre.
- Dafür eine kleine aktive Testbasis im Root zurückgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprüft zurückzuziehen.
- Den gewählten Prüfpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prüfen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurückgezogen: sie waren für diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hätten den Wiedereinstieg unnötig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusätzlich läuft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grünen Tests.

## 2026-03-22 – Block 5 Prompt 3: Letzte kleine mobile Admin-Parität mit Arbeitsstunden-Prüfung geschlossen

- Die verbleibenden mobilen Admin-Lücken erneut gegen belastbare WPF-/MAUI-Pfade geprüft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prüfen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine Paritätslücke für Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile Prüfseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingeführt: die Umsetzung bleibt vollständig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung für offene Arbeitsstunden berücksichtigt nur operative Mitglieder über den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-Prüfung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform für Arbeitsstunden, keine zusätzlichen WPF-Oberflächen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 – Block 5 Prompt 2: Nächste mobile Admin-Parität mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-Lücken erneut gegen die produktiven WPF-Wege geprüft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nächste kleine, aber fachlich wichtige Paritätslücke, weil WPF dafür bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine Paritätslücke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt für belastbar zugeordnete Mitglieder auch die Rolle ändern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt für Rollenänderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulässigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschärft: die operative Benutzerliste bevorzugt nun die tatsächliche Mitgliedsrolle vor einem evtl. älteren `app_user`-Wert, damit Rollenänderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block draußen: die bereits im gemeinsamen Benutzerlistenpfad eingeführte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-Paritätsschritt weiterhin erfolgreich.

## 2026-03-22 – Block 5 Prompt 1: Nächste mobile Admin-Parität mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-Parität erneut gegen die aktiven WPF-Verwaltungswege geprüft: die deutlichste belastbare Lücke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad für Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafür aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese Lücke als nächsten kleinen Kernblock priorisiert: sie ist fachlich relevant für Admin/Vorstand, baut vollständig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` für MAUI ergänzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoßen und Passwort-Reset versenden laufen mobil jetzt über denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Änderungsgrenze auch mobil sauber berücksichtigt: eine E-Mail-Änderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur für das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten für diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit über den gemeinsamen Auth-Pfad erhalten; keine neue WPF-Oberfläche aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-Paritätsschritt weiterhin erfolgreich.

## 2026-03-22 – Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprüft: offen war vor allem noch die Nutzer-Parität zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der Prüfpfad `Arbeitsstunden prüfen` bleiben dagegen weiterhin bewusst außerhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns für den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schließt sich die bisherige Home-/Pfad-Lücke gegenüber MAUI.
- Irreführende leere Bekanntmachungs-Detailflächen entschärft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen Prüfpfad für `Arbeitsstunden prüfen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingeführt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 – Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprüft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer Prüf-/Bekanntmachungs-Backendpfad für Home ist dagegen weiterhin nicht belastbar genug für neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad für WPF und MAUI.
- Nächste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis für den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener Prüfstände und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzählen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-Überblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsächlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-Prüfseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehörige gemeinsame Prüfpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 – Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprüft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bündelt jetzt den fachlichen Kern für Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrücken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad für Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulauflistung wurde für Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen für Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: für diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafür noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 – Block 3/3 Prompt 3: Parzellen-Kernlücken und mobile Admin-Parität abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprüft: belastbar verfügbar waren bereits aktive Zähler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und ließ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollständig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-Parität im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, Stromzähler tauschen, Wasser-Ablesungen speichern/bearbeiten, Wasserzähler einbauen und ausbauen sowie das Öffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/Wasserzählerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusätzliche Parzellen-Stammdaten oder neue Dokument-/Zähler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit für das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 – Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprüft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade für Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafür noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurückgeführt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden Überschneidungen gegen den gewählten Starttag geprüft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergänzt: Mitglied zuordnen, Startdatum wählen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende Fachanschlüsse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile Parzellenübersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung über denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprüft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt überlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 – Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und Abschlussfähigkeit geprüft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus Zähler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestätigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad für Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation für den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehörigen Parzellen-Dateien durchgeführt; blockfremde untracked Artefakte bleiben weiterhin außerhalb des Commits.

## 2026-03-21 – Archivblock 1/1: Root-Struktur nach Block 2 aufgeräumt

- Root-Istzustand nach Abschluss von Block 2 geprüft und archivwürdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrücklichen Wunsch auch `KGV.Tests`; zusätzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich überführt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestätigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nötigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthält nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusätzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverändert gelassen: keine Änderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst außerhalb der aktiven Lösung und des aktiven CI-Laufs archiviert.

## 2026-03-21 – Block 2/3 Abschluss: Einladung, Erstlogin und Mailänderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprüft: nur die tatsächlich geänderten Auth-Dateien, WPF-/MAUI-Anschlüsse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschädigte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode für `Mailadresse ändern` in `MemberDetailViewModel` ergänzt.
- Auth-Abschlusslogik inhaltlich bestätigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-Mailänderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgeführt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschließend baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss für diesen Teilblock vorbereitet: nur die tatsächlich zugehörigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst außen vor.

## 2026-03-21 – Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprüft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert über denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-Zustände bereinigt: Recovery-/OTP-Start setzt veraltete Auth-Zustände zurück; erfolgreicher Passwortwechsel beendet die Recovery-Session zusätzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurückkehrt.
- Login-/Recovery-Zustände aufeinander abgestimmt: erfolgreicher Passwort-Login räumt alte OTP-Zustände auf, damit kein halbfertiger Recovery-Kontext in den regulären Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschärft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primär `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 – Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprüft: aktiver Loginpfad, OTP-Anforderung, OTP-Prüfung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu öffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt über eine echte `NavigationPage` mit `LoginPage` statt über den bisherigen App-Platzhalter.
- MAUI-Loginflow geschärft: normales Login, OTP-Prüfung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte Zustände geführt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-Führung sauber: Rückkehr in den normalen Loginzustand mit erneuter Anmeldung über das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- Mailänderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 – Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprüft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollständig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg über `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschädigte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgeführt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 – Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprüft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsächlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergänzt: neues `README.md` als Startpunkt für Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurückgeführt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergänzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- Irreführende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschädigten/irreführenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgeführt: `KGV.slnx` um `KGV.Tests` ergänzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` überführt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prüfbaren Projekte ausgerichtet.

## 2026-03-21 – Prompt 1/2: Home/Startseite – Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprüft: `HomeViewModel`/`HomeView` in WPF war bisher nur Modulübersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die Detailfläche erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne Einträge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-Menü verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau für `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur für `admin` und `vorstand` sichtbar, über die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthält nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprüft: weiterhin keine belastbare bestehende fachliche Quelle für `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 – Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere Zustände für normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prüfen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

---

## Workaround & Vorgehensweise bei Änderungen

- Änderungen nur nach genauer Rückfrage und vollständiger Ansicht der Datei vornehmen.  
- Ich möchte immer genau wissen, **wo und wie** die Änderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor Änderungen vorgeschlagen werden.  

---

## 2026-03-23 – Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsächliche XAML-Ursache zurückgeführt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geöffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungültigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewünschte Doppelklickpfad bleibt weiterhin korrekt über die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 – Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, Zurück-/Dirty-Check ergänzt, Navigation auf Home-only zurückgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`Arbeitseinsätze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprüft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu eröffnen.
- Der reproduzierbare Verschiebungsfehler ließ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklärte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter für PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusätzliche zentrale Normalisierung im `SupabaseService` für geladene Verwaltungsdatensätze sowie für die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - Rückkehr erfolgt über den vorhandenen Navigationspfad zurück zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `Zurück`-Button
  - `Zurück` und `Abbrechen` prüfen auf echte ungespeicherte Änderungen
  - die Rückfrage erscheint nur bei tatsächlichen Änderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewünschten Produktpfad zurückgezogen:
  - `Arbeitseinsätze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch über die vorhandenen Home-Buttons für Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer Verwaltungsoberfläche erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusätzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 – Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, Prüftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprüft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue Prüftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im Prüfkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen über `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen über `status`; die globale Prüfsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen Prüfpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr künstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld für Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frühere Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(älteste zuerst)*
  - pro Zeile sichtbare Prüfinformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsächlich geänderte oder zur Freigabe markierte Zeilen; unbearbeitete offene Datensätze bleiben unverändert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prüfenden Benutzers gesetzt. Nicht freigegebene, aber geänderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im Prüf-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall öffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusätzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Öffnen der Freigabeansicht wird eine globale Prüfsperre über die offenen `arbeitstunde`-Datensätze gesetzt
  - andere Prüfer erhalten bei aktiver Fremdsperre nur eine saubere Rückmeldung statt paralleler produktiver Bearbeitung
  - soweit auflösbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlängert die Sperre während der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hängende Locks laufen nach Timeout kontrolliert aus und sind danach übernehmbar
- Die bestehende Badge-/Zählerlogik bleibt weiterverwendet: Änderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer Freigabeoberfläche erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 – Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprüft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene Prüfgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad für offene Fälle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter über `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter über `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin über `ArbeitsstundenChangedMessage` verteilt.
- Für normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergänzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow für `Datum`, `Stunden` und `Art der Arbeit` – ohne zusätzliche Fachfelder und ohne neue Freigabeoberfläche.
- Der neue Einstieg ist bewusst sichtbar und eigenständig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` für Benutzer mit eigenem Mitgliedskontext; additionally erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View führt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung für den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. Für `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und größer als `0` sein.
- Für Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende Prüf-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-Adminmenü zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene Datensätze mit `freigegeben = false` vorhanden sind; der Badge/Zähler zeigt weiter die tatsächliche Anzahl offener Prüffälle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prüfen` wurde entlang der betroffenen Navigations-/Titelflächen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 – Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprüft: `Termine`, `Bekanntmachungen` und `Arbeitseinsätze` waren bereits produktiv an ihre Basistabellen angebunden, während im Repo noch die älteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschärft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsächlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch überholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehöriger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frühere gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprüft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschließlich für Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt für spätere MAUI-Parität anschlussfähig: die produktiven Lese-/Create-/Update-Pfade für `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollständig im plattformneutralen Shared-Core-/Infrastructure-Bereich, während die eigentlichen Editoroberflächen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik eröffnet: der Block blieb bewusst bei Konsolidierung, Aufräumen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergänzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 – Arbeitseinsätze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln für Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten Arbeitseinsätze-Verwaltung erneut geprüft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste über `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestätigter Schreibpfad gegen die Basistabelle.
- Den bestätigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergänzt und nicht über Startseiten-Views geschrieben: gemeinsamer Service lädt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geänderte Datensätze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurück.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch für `Arbeitseinsätze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert größer als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe führt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` geführt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestätigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusätzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergänzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen Aufräumcheck bewusst klein gehalten: die ältere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; größere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Arbeitseinsätze-Block weiterhin erfolgreich.

## 2026-03-22 – Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprüft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste über die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestätigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestätigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergänzt und nicht über Home-Views geschrieben: gemeinsamer Service lädt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geänderte Datensätze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurück.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungültige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- Für `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingeführt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `Überschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergänzt und parallel in einer kompakten Vorschau geprüft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusätzlichen fachlichen Sortierregeln erfunden. Für neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestätigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergänzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner Aufräumcheck bewusst klein gehalten: die ältere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nächsten Aufräumschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 – Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprüft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestätigten Schreibpfad auf die Basistabelle.
- Den bestätigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestätigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst außerhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lädt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geänderte Datensätze direkt per Create/Update wieder nach `termin` zurück.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz öffnet rechts den Editor mit echten Werten, `Neu` öffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollständig, und `Speichern` bleibt am Ende des Formulars.
- Bestätigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- Für Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergänzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungültige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster für Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit für `Bekanntmachungen` und `Arbeitseinsätze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergänzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 – Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprüft: Home zeigt bereits nur Listen und separate Detailviews, aber für Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur für `Arbeitseinsätze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services für diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die Übersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusätzlich nur drei kleine Bearbeiten-Einstiege (`Arbeitseinsätze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- Für WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter Editorfläche.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: über neue gemeinsame Servicezugriffe werden die bestätigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch für die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geöffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geöffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehängt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur für Admin/Vorstand verfügbar und blähen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber für spätere MAUI-Parität nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 – Nachtrag Home-Diagnose: aktuell laden stabil nur Arbeitseinsätze und Termine

- Im aktuellen WPF-Nachtest bestätigt: `Arbeitseinsätze` und `Termine` laden über die Startseiten-Views stabil; die verbleibenden Auffälligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlägt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. Maßgeblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestätigte View.
- Für `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irreführender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die Restprüfung bleibt daher auf tatsächliche Rückgabefelder bzw. reale Datensätze der View fokussiert.
- Der UX-Nachzug für Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von Arbeitseinsätzen, Terminen und Bekanntmachungen öffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 – Home-UX-Nachzug: Startseite zeigt nur Listen, Details öffnen in eigener View

- Den Home-Stand für Arbeitseinsätze, Termine und Bekanntmachungen auf den gewünschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/Kartenübersichten, während Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- Dafür eine eigenständige WPF-Detailansicht für Home-Elemente ergänzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Öffnen-Kommandos für Arbeitseinsätze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-Detailzustände in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige für Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergänzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurückgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzuführen.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur Teil 3: Ursachenanalyse für verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprüft. Die entscheidende technische Ursache für den Pflichtstundenfehler ließ sich jetzt direkt aus dem WPF-Debug-Log bestätigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestätigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, während `Arbeitseinsätze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird für Home jetzt zunächst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschließend robust clientseitig über den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung für Pflichtstunden ergänzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestätigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- Für Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irreführenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurückgelieferte Einträge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusätzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergänzt: der Home-Pfad schreibt jetzt für Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusätzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur Teil 2: Pflichtstunden über Hauptmitglied/Saison präzisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprüft: da `Arbeitseinsätze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen – Pflichtstunden über den falschen Mitgliedsbezug bzw. zu grobe Saisonauflösung und Bekanntmachungen über ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad für Home deshalb final präzisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der für Home relevante Hauptmitgliedsbezug aufgelöst; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die Pflichtstundenübersicht über dieses Hauptmitglied geladen.
- Zusätzlich wird die bestätigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berücksichtigt, sondern zuerst direkt für die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fällt erst danach auf Jahres-/letzten Datensatz zurück.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusätzlich mögliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping für Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette für Titel, Inhalt und Veröffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverändert klein, zeigt aber reale View-Daten robuster an, statt früh in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 – HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprüft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fällt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurück.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestätigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhängig vom konkreten DB-/Session-Kontext ist damit bestätigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, Arbeitseinsätze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fällt nicht mehr komplett auf leere Platzhalter zurück.
- Kleine, gezielte technische Transparenz ergänzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusätzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte für die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusätzlich vereinfacht und näher auf den bestätigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt für das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusätzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehört – im bestätigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 – Home-Diagnose: bestätigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestätigten DB-Schema-Stand geprüft; dabei zeigte sich, dass die Home-Anbindung zwar grundsätzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestätigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestätigten Kern geschärft, ohne neue App-Logik einzuführen: `PflichtstundenUebersichtRecord` bildet jetzt zusätzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` löst zuerst die aktuelle Saison über `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose über ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusätzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht künstlich verloren gehen; der wesentliche Datenverlust lag im geprüften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 3: Letzte kleine Restlücke über MAUI-Prüfstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei möglichen Abschlussoptionen geprüft: ein echter gemeinsamer Ablehnungs-/Rückgabe-/Kommentarpfad wäre trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafür im aktiven Modell und Servicepfad keine saubere fachliche Rückgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-Prüfpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewählt und den vorhandenen mobilen Prüfpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prüfen` mobil jetzt nur noch dann an, wenn über den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsächlich offene Prüffälle vorliegen; zusätzlich erscheint die aktuelle Anzahl offener Datensätze direkt im Titel des Menüpunkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder über denselben gemeinsamen Prüfpfad, sodass Menüeintrag und tatsächliche offene Fälle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingeführt: die mobile Seite bleibt vollständig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen Prüfstatusanzeige draußen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-Prüfflow in WPF bis zur Freigabe vervollständigt

- Den aktuellen WPF-Istzustand für `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprüft: belastbar vorhanden waren bereits Prüfliste, Badge/Zähler und die bestehende Bearbeitungsansicht, aber aus der Prüfliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den Prüfpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurückgeführt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prüfen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen Prüfmodus geöffnet, gefiltert auf die tatsächlich offenen Datensätze des ausgewählten Mitglieds.
- Dafür nur einen kleinen Navigationskontext ergänzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert für die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene Prüfeinträge des konkret ausgewählten Mitglieds im Prüfmodus erscheinen.
- Admin/Vorstand können im Prüfmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusätzlich eine direkte `Freigeben`-Aktion für den ausgewählten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/Rückgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell für einen sauberen Rückgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, Prüfliste und Badge/Zähler weiterhin über den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen Prüfservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im Prüfmodus fachlich geschärft: klarer Titel, Hinweistext, leerer Prüfzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte Zustände für Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus Prüfliste und Prüfzähler ausgeschlossen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der Vervollständigung des Prüfflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfähig.

## 2026-03-22 – Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und Prüfstatus in WPF angeschlossen

- Den aktuellen Istzustand für `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation und den gemeinsamen Prüfpfad erneut geprüft: ein belastbarer Listen-/Detailpfad für Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-Prüfstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und öffnet belastbar die bestehende WPF-`ArbeitsstundenView` für das aktuelle Mitglied statt neuer Schattenlogik.
- Dafür den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch für Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlässig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hängt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene Einträge, Vorstand/Admin können wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingeführt.
- Das Arbeitsstunden-Formular geschärft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heißt konsistent `Speichern`.
- Kleinen WPF-Prüfpfad für offene Arbeitsstunden ergänzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und führen von der Prüfliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten Prüfstatus gestellt: der Eintrag `Arbeitsstunden prüfen` erscheint für berechtigte Benutzer jetzt nur bei tatsächlich offenen Prüffällen, zeigt einen kleinen Zähler mit der Zahl offener Datensätze und wird bei offenem Prüfstand sichtbar hervorgehoben; Aktualisierung läuft nach Änderungen über den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus Prüflisten und Prüfzählern ausgeschlossen, weil weiterhin ausschließlich der gemeinsame operative Prüfpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfähig.

## 2026-03-22 – WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand für `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklärten produktiven DB-Pfade geprüft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, während `Arbeitseinsätze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/Leerzustände nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublähen: `HomeOverviewDTO` trägt jetzt zusätzlich gemeinsame Startseiten-Daten für Pflichtstunden, Arbeitseinsätze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lädt diese Inhalte direkt aus den geklärten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusätzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) für die Home-Ausgabe mitgeführt.
- Bereich `Arbeitseinsätze` an `v_startseite_arbeitseinsatz` angebunden: echte Einträge mit Titel, Datum/Zeit, Treffpunkt sowie Kapazitäts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafür im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte Einträge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterführende Informationen nur aus den belastbar vorhandenen View-Feldern und lässt sich wieder mit `Schließen` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View für Bekanntmachungen angebunden: echte Einträge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfügbaren Inhalte statt des bisherigen künstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergänzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden für Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzuführen.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird für nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfähig.

## 2026-03-22 – WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand für Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprüft: konkret auffällig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewünschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen größeren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusätzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurückgeführt: `MainWindowViewModel` lädt für den Eigenkontext weiter den nötigen Mitgliedskontext vor, navigiert danach aber standardmäßig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` für das aktuelle Kalenderjahr mit belastbar ableitbaren Werten für freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfügbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewünschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `Arbeitseinsätze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; für `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `Schließen`-Button ergänzt.
- Weiterhin bewusst nicht erfunden: für `Arbeitseinsätze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfähigen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprüft und bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten Prüfpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprüft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen Prüfpfad für Block 6 bewusst genau diesen Core-Baustein gewählt: fachlich wichtig, technisch klar abgegrenzt und ohne zusätzliche Infrastruktur direkt testbar.
- Dafür `UserRolesTests` in die bestehende aktive Teststruktur ergänzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine künstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhängen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 – Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nächsten Prüfpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprüft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, während der gemeinsame Home-Kern trotz hoher fachlicher Relevanz für WPF und MAUI noch ungetestet blieb.
- Als nächsten kleinen Prüfpfad bewusst den `HomeOverviewFactory` gewählt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe für Desktop und Mobil und lässt sich ohne zusätzliche Testinfrastruktur direkt prüfen.
- Dafür `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergänzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets für `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine künstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad für die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengeführt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprüft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frühere WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurückgezogen, sondern sinnvoll zusammengeführt: die aktive Root-Projektdatei übernimmt jetzt wieder die für den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, während der neue `OperationalDataFilter`-Block vollständig erhalten bleibt.
- Fachlich passende ältere Tests sauber zurückgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner Prüfpfad für den aktuellen produktiven Export-Code reaktiviert, statt als bloßes Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden übernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengeführtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende Testansätze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` läuft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfähig.

## 2026-03-22 – Block 6 Prompt 1: Kleine aktive Testbasis für Demo-/Testdatenfilter zurückgeholt

- Die aktuelle Testlage geprüft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die Lösung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ältere Testidee für `ExportViewModel`, die für den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen Prüfpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewählt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-Prüfung) ohne dass dafür eine große Testinfrastruktur nötig wäre.
- Dafür eine kleine aktive Testbasis im Root zurückgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprüft zurückzuziehen.
- Den gewählten Prüfpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prüfen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurückgezogen: sie waren für diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hätten den Wiedereinstieg unnötig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusätzlich läuft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grünen Tests.

## 2026-03-22 – Block 5 Prompt 3: Letzte kleine mobile Admin-Parität mit Arbeitsstunden-Prüfung geschlossen

- Die verbleibenden mobilen Admin-Lücken erneut gegen belastbare WPF-/MAUI-Pfade geprüft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prüfen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine Paritätslücke für Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile Prüfseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingeführt: die Umsetzung bleibt vollständig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung für offene Arbeitsstunden berücksichtigt nur operative Mitglieder über den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-Prüfung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform für Arbeitsstunden, keine zusätzlichen WPF-Oberflächen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 – Block 5 Prompt 2: Nächste mobile Admin-Parität mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-Lücken erneut gegen die produktiven WPF-Wege geprüft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nächste kleine, aber fachlich wichtige Paritätslücke, weil WPF dafür bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine Paritätslücke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt für belastbar zugeordnete Mitglieder auch die Rolle ändern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt für Rollenänderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulässigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschärft: die operative Benutzerliste bevorzugt nun die tatsächliche Mitgliedsrolle vor einem evtl. älteren `app_user`-Wert, damit Rollenänderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block draußen: die bereits im gemeinsamen Benutzerlistenpfad eingeführte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-Paritätsschritt weiterhin erfolgreich.

## 2026-03-22 – Block 5 Prompt 1: Nächste mobile Admin-Parität mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-Parität erneut gegen die aktiven WPF-Verwaltungswege geprüft: die deutlichste belastbare Lücke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad für Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafür aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese Lücke als nächsten kleinen Kernblock priorisiert: sie ist fachlich relevant für Admin/Vorstand, baut vollständig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` für MAUI ergänzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoßen und Passwort-Reset versenden laufen mobil jetzt über denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Änderungsgrenze auch mobil sauber berücksichtigt: eine E-Mail-Änderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur für das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten für diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit über den gemeinsamen Auth-Pfad erhalten; keine neue WPF-Oberfläche aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-Paritätsschritt weiterhin erfolgreich.

## 2026-03-22 – Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprüft: offen war vor allem noch die Nutzer-Parität zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der Prüfpfad `Arbeitsstunden prüfen` bleiben dagegen weiterhin bewusst außerhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns für den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schließt sich die bisherige Home-/Pfad-Lücke gegenüber MAUI.
- Irreführende leere Bekanntmachungs-Detailflächen entschärft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen Prüfpfad für `Arbeitsstunden prüfen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingeführt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 – Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprüft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer Prüf-/Bekanntmachungs-Backendpfad für Home ist dagegen weiterhin nicht belastbar genug für neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad für WPF und MAUI.
- Nächste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis für den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener Prüfstände und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzählen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-Überblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsächlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-Prüfseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehörige gemeinsame Prüfpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 – Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprüft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bündelt jetzt den fachlichen Kern für Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrücken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad für Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulaufl listing wurde für Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen für Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: für diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafür noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 – Block 3/3 Prompt 3: Parzellen-Kernlücken und mobile Admin-Parität abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprüft: belastbar verfügbar waren bereits aktive Zähler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und ließ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollständig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-Parität im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, Stromzähler tauschen, Wasser-Ablesungen speichern/bearbeiten, Wasserzähler einbauen und ausbauen sowie das Öffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/Wasserzählerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusätzliche Parzellen-Stammdaten oder neue Dokument-/Zähler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit für das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 – Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprüft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade für Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafür noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurückgeführt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden Überschneidungen gegen den gewählten Starttag geprüft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergänzt: Mitglied zuordnen, Startdatum wählen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende Fachanschlüsse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile Parzellenübersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung über denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprüft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt überlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 – Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und Abschlussfähigkeit geprüft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus Zähler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestätigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad für Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation für den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehörigen Parzellen-Dateien durchgeführt; blockfremde untracked Artefakte bleiben weiterhin außerhalb des Commits.

## 2026-03-21 – Archivblock 1/1: Root-Struktur nach Block 2 aufgeräumt

- Root-Istzustand nach Abschluss von Block 2 geprüft und archivwürdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrücklichen Wunsch auch `KGV.Tests`; zusätzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich überführt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestätigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nötigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthält nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusätzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverändert gelassen: keine Änderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst außerhalb der aktiven Lösung und des aktiven CI-Laufs archiviert.

## 2026-03-21 – Block 2/3 Abschluss: Einladung, Erstlogin und Mailänderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprüft: nur die tatsächlich geänderten Auth-Dateien, WPF-/MAUI-Anschlüsse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschädigte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode für `Mailadresse ändern` in `MemberDetailViewModel` ergänzt.
- Auth-Abschlusslogik inhaltlich bestätigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-Mailänderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgeführt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschließend baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss für diesen Teilblock vorbereitet: nur die tatsächlich zugehörigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst außen vor.

## 2026-03-21 – Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprüft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert über denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-Zustände bereinigt: Recovery-/OTP-Start setzt veraltete Auth-Zustände zurück; erfolgreicher Passwortwechsel beendet die Recovery-Session zusätzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurückkehrt.
- Login-/Recovery-Zustände aufeinander abgestimmt: erfolgreicher Passwort-Login räumt alte OTP-Zustände auf, damit kein halbfertiger Recovery-Kontext in den regulären Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschärft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primär `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 – Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprüft: aktiver Loginpfad, OTP-Anforderung, OTP-Prüfung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu öffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt über eine echte `NavigationPage` mit `LoginPage` statt über den bisherigen App-Platzhalter.
- MAUI-Loginflow geschärft: normales Login, OTP-Prüfung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte Zustände geführt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-Führung sauber: Rückkehr in den normalen Loginzustand mit erneuter Anmeldung über das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- Mailänderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 – Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprüft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollständig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg über `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschädigte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgeführt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 – Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprüft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsächlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergänzt: neues `README.md` als Startpunkt für Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurückgeführt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergänzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- Irreführende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschädigten/irreführenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgeführt: `KGV.slnx` um `KGV.Tests` ergänzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` überführt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prüfbaren Projekte ausgerichtet.

## 2026-03-21 – Prompt 1/2: Home/Startseite – Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprüft: `HomeViewModel`/`HomeView` in WPF war bisher nur Modulübersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die Detailfläche erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne Einträge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-Menü verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau für `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur für `admin` und `vorstand` sichtbar, über die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthält nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprüft: weiterhin keine belastbare bestehende fachliche Quelle für `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 – Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere Zustände für normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prüfen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
