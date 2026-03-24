# KGV_Fortschrittslog_ausfuehrlich

---

## 2026-03-24 – Prompt 1/1: kleinen WPF-Bindungsfehler in `FotoUploadTestView` für schreibgeschützte Diagnosefelder behoben

- Den Block zuerst wieder gegen den realen Istzustand des lokalen Repositories und des Git-Arbeitsbaums geprüft.
- Einordnung vor dem Fix:
  - Dateien dieses Blocks: `KGV.Wpf/Views/FotoUploadTestView.xaml` plus die beiden Logdateien
  - blockfremde offene WPF-/Supabase-/Upload-Testdateien blieben bewusst unangetastet
  - `_AI_DB_EXPORT/*`, `_secrets/*`, `.github/copilot-instructions.md` und sonstige lokale Artefakte wurden nicht als Grundlage verwendet
- Den Fehler direkt am tatsächlichen WPF-Pfad geprüft:
  - `FotoUploadTestView.xaml`
  - `FotoUploadTestViewModel`
- Ursache bestätigt:
  - `RawResponseBody` ist im ViewModel ein schreibgeschütztes Anzeige-/Diagnosefeld
  - die Bindung in `FotoUploadTestView.xaml` lief auf `TextBox.Text` ohne expliziten Modus
  - `TextBox.Text` bindet in WPF standardmäßig `TwoWay`
  - daraus entstand beim Öffnen/Benutzen der Seite die Exception zur schreibgeschützten Eigenschaft `RawResponseBody`
- Den Fix bewusst klein und nur an der richtigen Stelle umgesetzt:
  - `Text="{Binding RawResponseBody, Mode=OneWay}"`
  - `IsReadOnly="True"` blieb bestehen
  - keine Setter im ViewModel ergänzt
  - keine Diagnosearchitektur umgebaut
  - keine MAUI-Datei angefasst
- Ergebnis:
  - die Rohantwort bleibt weiterhin sichtbar
  - die WPF-Seite versucht nicht mehr, in das schreibgeschützte Diagnosefeld zurückzuschreiben
  - der restliche Upload-Testblock bleibt unverändert
- Verifikation:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - der Bugfix bleibt damit klein, zielgerichtet und buildfähig


## 2026-03-24 â€“ Prompt 1/1: Gemeinsamen RFID-Scan-Kontext fÃ¼r `Ablesung erfassen` und `ZÃ¤hlerwechsel` produktiv umgesetzt

- Den Block erneut mit echter IstzustandsprÃ¼fung begonnen.
- Einordnung vor dem Umbau:
  - Dateien dieses Blocks waren die Placeholder fÃ¼r `Ablesung erfassen`, `ZÃ¤hlerwechsel` und `RfidScanContext` in WPF sowie die mobilen Placeholder fÃ¼r `AblesungErfassenPage` und `ZaehlerwechselPage`
  - blockfremde offene WPF-Dateien und `supabase/migrations/20260323093513_remote_schema.sql` blieben bewusst unangetastet
  - `_Archiv/_Recovery`, `_Archiv/_RecoveredArtifacts` und `_AI_DB_EXPORT/*` wurden nicht als fachliche Grundlage verwendet
- Fachlich fehlte bisher der gemeinsame produktive RFID-Lesekern fÃ¼r beide Workflows.
- Shared-Service nun klein und produktiv erweitert:
  - `RfidScanContextState` mit den drei fachlichen ZustÃ¤nden
  - `RfidScanContextResult` als gemeinsames Ergebnisobjekt
  - `ResolveRfidScanContextAsync(string uid)` in `ISupabaseService` / `SupabaseService`
  - UID-Normalisierung zentral im Service, damit WPF und MAUI keine abweichende Logik bauen
  - AuflÃ¶sung ausschlieÃŸlich Ã¼ber `v_rfid_scan_context`
- Zustandslogik jetzt fachlich klar und zentral:
  - kein Treffer in `v_rfid_scan_context` => `Unknown`
  - Treffer mit `aktiver_zaehler_id` => `KnownWithActiveMeter`
  - Treffer ohne `aktiver_zaehler_id` => `KnownWithoutActiveMeter`
- `RfidScanContextRecord` nur am echten View-Vertrag orientiert und mit Anzeigehilfen ergÃ¤nzt:
  - Medium-Anzeige
  - RFID-Anzeige
  - aktiver ZÃ¤hler ja/nein
  - ZÃ¤hlernummer
  - Status
  - Eichdatum / EichfÃ¤lligkeit
- WPF konkret umgesetzt:
  - den vorhandenen `RfidScanContextViewModel`-Placeholder in eine echte gemeinsame WPF-Kontextlogik umgebaut
  - den vorhandenen `RfidScanContextView`-Placeholder in eine echte UID-Eingabe- und Ergebnisanzeige umgebaut
  - `AblesungErfassenViewModel` bindet diesen gemeinsamen Kontext jetzt produktiv ein
  - `ZaehlerwechselScanViewModel` bindet denselben gemeinsamen Kontext ebenfalls ein
  - beide WPF-Seiten zeigen danach dieselben Kontextdaten, aber unterschiedliche workflow-spezifische Einordnung an
- MAUI konkret umgesetzt:
  - neues gemeinsames `RfidScanContextViewModel`
  - neue gemeinsame Basispage `RfidScanWorkflowPage`
  - `AblesungErfassenPage` von Placeholder auf produktive UID-Eingabe und Kontextanzeige umgestellt
  - `ZaehlerwechselPage` ebenso auf denselben gemeinsamen Kern umgestellt
  - beide mobilen Seiten unterscheiden nun ebenfalls sauber zwischen unbekanntem Tag, bekanntem Tag mit aktivem ZÃ¤hler und bekanntem Tag ohne aktiven ZÃ¤hler
- Workflow-Einordnung nach Aufrufziel:
  - `Ablesung erfassen`: bekannter Kontext wird angezeigt; mit aktivem ZÃ¤hler ist der Ablese-Kontext vorbereitet, ohne aktiven ZÃ¤hler wird sauber erklÃ¤rt, dass Ablesung noch nicht sinnvoll ist
  - `ZÃ¤hlerwechsel`: mit aktivem ZÃ¤hler wird Ausbau als nÃ¤chster Schritt angezeigt, ohne aktiven ZÃ¤hler Einbau
- Rechte/Konsistenz:
  - Bereich bleibt fachlich auf Admin/Vorstand begrenzt
  - WPF und MAUI nutzen denselben Shared-Servicepfad und dieselbe Zustandsableitung
  - keine QR- oder sonstige Schattenlogik ergÃ¤nzt
- Verifikation:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - Block ist damit als gemeinsamer produktiver RFID-Kontextkern fÃ¼r beide spÃ¤teren Fachflows buildfÃ¤hig abgeschlossen

## 2026-03-24 â€“ Prompt 1/1: `FÃ¤llige ZÃ¤hler` auf `v_zaehler_eichstatus` produktiv umgesetzt

- Den Block wieder mit echter VorprÃ¼fung gegen den aktuellen Repo- und Arbeitsbaumstand begonnen.
- Blockeinordnung vor Umsetzung:
  - Dateien dieses Blocks: `FaelligeZaehler`-Placeholder in WPF und MAUI plus fehlender Shared-Servicepfad fÃ¼r `v_zaehler_eichstatus`
  - blockfremde AltÃ¤nderungen sollten ausdrÃ¼cklich unangetastet bleiben
  - `_Archiv/_Recovery`, `_Archiv/_RecoveredArtifacts` und `_AI_DB_EXPORT/AI_DATABASE_CONTEXT.sql` wurden bewusst nicht als fachliche Grundlage verwendet
- Fachlich war im aktiven Codepfad vorhanden:
  - WPF-Placeholder `FaelligeZaehlerViewModel` / `FaelligeZaehlerView`
  - MAUI-Placeholder `FaelligeZaehlerPage`
  - noch kein produktiver Service-/Modelpfad fÃ¼r `v_zaehler_eichstatus`
- Gemeinsamen Datenpfad klein und produktiv hergestellt:
  - neues Model `ZaehlerEichstatusRecord` direkt fÃ¼r die View `v_zaehler_eichstatus`
  - neue Shared-Service-Methode `GetZaehlerEichstatusAsync()` in `ISupabaseService` / `SupabaseService`
  - Sortierung zentral im Service:
    - `ueberfaellig` zuerst
    - dann `bald_faellig`
    - danach `ok`
    - innerhalb dessen nach Tagen
    - anschlieÃŸend nach Garten und Anlage
- WPF konkret umgesetzt:
  - Placeholder-ViewModel ersetzt durch echte Ãœbersicht
  - Textfilter
  - Statusfilter
  - `Aktualisieren`-Button
  - Tabelle mit:
    - Anlage
    - Garten
    - Medium
    - ZÃ¤hler
    - Eichdatum
    - EichfÃ¤lligkeit
    - Status
    - Tage
  - Leer- und FehlerzustÃ¤nde sauber sichtbar
  - NavigationService auf den neuen ViewModel-Konstruktor angepasst
- MAUI konkret umgesetzt:
  - neue mobile ViewModel-Klasse `FaelligeZaehlerViewModel`
  - `FaelligeZaehlerPage` von Placeholder auf echte Liste/Kartenansicht umgestellt
  - gleicher Shared-Servicepfad wie in WPF
  - Textfilter
  - Statusfilter
  - `Aktualisieren`
  - leere Ergebnisse und Fehler werden klar angezeigt
- Rechte/Konsistenz:
  - Bereich bleibt in WPF und MAUI explizit auf Admin/Vorstand begrenzt
  - kein neuer Schattenpfad in der UI
  - keine fachfremden Ablesen-/ZÃ¤hlerwechsel-/RFID-Workflows mit hineingezogen
- Verifikation:
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - nach kleiner MAUI-Nachkorrektur blieb der Block ohne neue eigene Buildwarnung aus der neuen Seite buildfÃ¤hig abgeschlossen

## 2026-03-24 â€“ Prompt 1/1: `RFID einrichten` fachlich auf neuem DB-Vertrag nutzbar gemacht

- Den Block wieder ausdrÃ¼cklich gegen den realen Istzustand gefÃ¼hrt und nicht gegen Restore-/Recovery-Reste.
- Git-/ArbeitsbaumprÃ¼fung vor dem Umbau:
  - fÃ¼r diesen Block gab es anfangs noch keine offenen RFID-Implementierungsdateien
  - blockfremd offen waren weiterhin mehrere WPF-UI-Dateien, lokale Artefakte sowie `supabase/migrations/20260323093513_remote_schema.sql`
  - `_Archiv/_Recovery`, `_Archiv/_RecoveredArtifacts` und `_AI_DB_EXPORT/AI_DATABASE_CONTEXT.sql` wurden bewusst nicht als fachliche Grundlage verwendet
- Fachliche Grundlage des Blocks sauber auf den neuen DB-Vertrag gezogen:
  - produktiver Schreibpfad `assign_parzelle_rfid(...)`
  - Datenbasis `parzelle.rfid_strom` / `parzelle.rfid_wasser`
  - Konflikt-/Scanreferenz aus `v_rfid_scan_context`
  - keine neue Schattenarchitektur neben dem vorhandenen DB-Vertrag
- Den Shared-Service dafÃ¼r klein, aber belastbar ergÃ¤nzt:
  - `CheckParzelleRfidAssignmentAsync(...)`
  - `AssignParzelleRfidAsync(...)`
  - UID wird vor PrÃ¼fung/Speicherung konsistent getrimmt und in GroÃŸbuchstaben normalisiert
  - VorabprÃ¼fung auf:
    - Parzelle vorhanden
    - Medium gÃ¼ltig
    - UID vorhanden
    - Medium passt zur Parzelle (`hat_strom` / `hat_wasser`)
  - zusÃ¤tzliche KonfliktprÃ¼fung gegen den realen DB-Bestand aus `v_rfid_scan_context`
- Konfliktverhalten jetzt fachlich klar:
  - hÃ¤ngt die UID bereits an anderer Parzelle oder anderem Medium, wird Speichern blockiert und verstÃ¤ndlich erklÃ¤rt
  - ist an derselben Parzelle fÃ¼r dasselbe Medium bereits dieselbe UID hinterlegt, wird dies als bereits erfÃ¼llt erkannt statt unnÃ¶tig nochmals zu schreiben
  - existiert an derselben Parzelle fÃ¼r dasselbe Medium bereits eine andere UID, erfolgt kein stilles Ãœberschreiben; erst nach ausdrÃ¼cklicher BestÃ¤tigung wird Ã¼ber den RPC gespeichert
- `ParzelleRecord` nur minimal fÃ¼r diesen Block auf den neuen Stand gezogen:
  - `Anlage`
  - `hat_strom`
  - `hat_wasser`
  - `rfid_strom`
  - `rfid_wasser`
  - kleine Anzeigehilfen fÃ¼r benutzerfreundliche Parzellen-/RFID-Darstellung
- WPF konkret umgesetzt:
  - Placeholder-`RfidEinrichtenViewModel` ersetzt durch echte Admin-/Vorstand-Maske
  - Parzellenauswahl mit benutzerfreundlicher Anzeige `GartenNr - Anlage`
  - aktuelle Strom-/Wasser-RFID sichtbar
  - Mediumauswahl nur aus tatsÃ¤chlich passenden Medien der gewÃ¤hlten Parzelle
  - UID-Eingabe
  - `PrÃ¼fen`
  - `Speichern`
  - Ãœberschreib-BestÃ¤tigung per `MessageBox`
  - ViewModel-Fabrik in `NavigationService` passend erweitert
- MAUI konkret umgesetzt:
  - mobile `RfidEinrichtenPage` von Placeholder auf echte Maske umgestellt
  - dieselben Fachschritte wie in WPF
  - gleicher Shared-Servicepfad fÃ¼r PrÃ¼fen und Speichern
  - Ãœberschreib-BestÃ¤tigung per mobiler Dialogabfrage
  - kein separater mobiler Schattenpfad
- Rechte sauber gehalten:
  - WPF bleibt Ã¼ber Admin/Vorstand-Kontext begrenzt
  - MAUI bleibt Ã¼ber AdminShell plus expliziten ViewModel-Check auf Admin/Vorstand begrenzt
  - normales `CanManageReadings` wurde fÃ¼r diesen Bereich bewusst nicht als alleinige Freigabe verwendet
- Bewusst nicht in diesen Block gezogen:
  - kein groÃŸer Umbau von `Ablesung erfassen`
  - kein ZÃ¤hlerwechsel-Workflow
  - kein Komplettaustausch der alten Split-Modelle in allen Bereichen
  - keine Bereinigung aller Altpfade im selben Schritt
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich
  - der Block bleibt damit klein, buildfÃ¤hig und fachlich auf dem neuen RFID-DB-Vertrag abgeschlossen

## 2026-03-24 â€“ Prompt 1/1: Navigationsbereich `Ablesen` mit Ãœbersichtsseite in WPF und MAUI angelegt

- Den Block zuerst gegen den realen Repo-Iststand geprÃ¼ft und nichts geraten.
- Ergebnis der IstzustandsprÃ¼fung:
  - WPF hatte bereits vorbereitete Einzelbausteine fÃ¼r `RfidEinrichten`, `FÃ¤llige ZÃ¤hler` und `ZÃ¤hlerwechsel`, aber keinen zusammenhÃ¤ngenden Navigationspunkt `Ablesen`
  - fÃ¼r `Ablesung erfassen` existierte im WPF-Stand noch kein eigener Seitenpfad, nur Dialog-/Teilfragmente
  - MAUI hatte noch keinen eigenen MenÃ¼punkt und keine Ãœbersichtsseite fÃ¼r den gesamten Ablese-Bereich
  - die bestehende Rollen-/Rechtebasis war bereits vorhanden: WPF Ã¼ber `UserContext.Role`, MAUI Ã¼ber `IAuthService.IsAdmin` / `IsVorstand`
- Den globalen Einstieg deshalb klein und sauber ergÃ¤nzt statt direkt tiefe Fachlogik vorzuziehen:
  - neuer MenÃ¼punkt `Ablesen` in WPF
  - neuer MenÃ¼punkt `Ablesen` im MAUI-Admin-/Vorstand-Flyout
  - normale Nutzer sehen diesen Bereich nicht
- WPF konkret umgesetzt:
  - neue `AblesenOverviewViewModel` + `AblesenOverviewView`
  - vier groÃŸe Kacheln mit den geforderten Texten und Untertiteln
  - Navigationsverkabelung zu:
    - `AblesungErfassenViewModel` *(neu als schlanker Platzhalter)*
    - `ZaehlerwechselScanViewModel` *(bestehender vorbereiteter Pfad)*
    - `RfidEinrichtenViewModel` *(bestehender vorbereiteter Pfad)*
    - `FaelligeZaehlerViewModel` *(bestehender vorbereiteter Pfad)*
  - `App.xaml`, `NavigationService` und `MainWindowViewModel` dafÃ¼r sauber erweitert
- MAUI konkret umgesetzt:
  - neue mobile Ãœbersichtsseite `AblesenOverviewPage`
  - vier groÃŸe tappbare Kacheln fÃ¼r dieselben Funktionen
  - vier schlanke Zielseiten angelegt und als Shell-Routen registriert:
    - `AblesungErfassenPage`
    - `ZaehlerwechselPage`
    - `RfidEinrichtenPage`
    - `FaelligeZaehlerPage`
  - damit ist der Navigationsfluss mobil bereits vollstÃ¤ndig, ohne die Fachflows dieses Folgeblocks vorwegzunehmen
- Rechte und Konsistenz:
  - WPF zeigt `Ablesen` nur fÃ¼r `Admin`/`Vorstand`
  - MAUI zeigt `Ablesen` nur im Admin-/Vorstand-Shell und zusÃ¤tzlich explizit nur bei `IsAdmin || IsVorstand`
  - keine neue SchattenprÃ¼fung neben der vorhandenen Rollenbasis
- Der Block bleibt bewusst klein:
  - keine RFID-Scanlogik vorgezogen
  - kein ZÃ¤hlerwechsel-Workflow vorgezogen
  - keine fÃ¤lligen ZÃ¤hler fachlich umgesetzt
  - nur Struktur, Navigation, Rechte und vorbereitete Zielseiten

## 2026-03-24 â€“ Prompt 1/1: veralteten Remote-Snapshot von fachlichen QR-Resten bereinigt

- Den Zusatzblock zuerst gegen den Repo-Iststand geprÃ¼ft. Nach der Live-Bereinigung lagen fachliche QR-Reste nur noch im veralteten Snapshot `supabase/migrations/20260323093513_remote_schema.sql`.
- Abgleich gegen `_AI_DB_EXPORT/database.types.ts` bestÃ¤tigt den aktuellen Stand:
  - `public.parzelle` enthÃ¤lt dort keine `qr_code_*`-Felder mehr
  - fachlich relevant sind nur noch `rfid_wasser` und `rfid_strom`
- Der alte Snapshot wurde daher gezielt bereinigt statt eine neue Migrationslogik daneben aufzubauen:
  - `qr_code_wasser` aus `public.parzelle` entfernt
  - `qr_code_strom` aus `public.parzelle` entfernt
  - die alten Unique-Constraints auf beiden QR-Spalten entfernt
- Damit entspricht der Repo-Snapshot wieder dem bereinigten fachlichen Stand der Live-DB bzw. der aktuellen Typdefinitionen; es verbleiben keine fachlichen QR-Reste mehr im Snapshot.

## 2026-03-24 â€“ Prompt 1/1: kleine feste WPF-ButtonhÃ¶hen projektweit geprÃ¼ft und vereinheitlicht

- Ausgehend vom Layoutproblem in Strom/Wasser wurden die WPF-XAML-Dateien gezielt auf kleine feste ButtonhÃ¶hen geprÃ¼ft.
- Ergebnis: mehrere Buttons hatten noch feste HÃ¶hen von `30` bzw. `32`, was fÃ¼r die aktuelle Schrift-/Padding-Kombination zu knapp war.
- Daher wurden die betroffenen expliziten HÃ¶hen in WPF einheitlich auf `35` angehoben.
- Angepasst wurden die Buttons in:
  - `KGV.Wpf/Views/GartenStromView.xaml`
  - `KGV.Wpf/Views/GartenWasserView.xaml`
  - `KGV.Wpf/Views/ArbeitsstundenView.xaml`
  - `KGV.Wpf/Views/ChangeEmailWindow.xaml`
  - `KGV.Wpf/Views/ResetPasswordWindow.xaml`
- Die bereits angehobene Parzellenzuordnungszeile bleibt davon unberÃ¼hrt; zusammen ergibt sich jetzt ein konsistenteres WPF-Buttonbild.

## 2026-03-24 â€“ Prompt 1/1: `MemberDetailView`-Parzellenzuordnung in der HÃ¶he leicht erhÃ¶ht

- Den Istzustand zuerst geprÃ¼ft. Die betroffene Zeile sitzt nicht direkt in `MemberDetailView.xaml`, sondern in der ausgelagerten `KGV.Wpf/Views/MemberParzellenSection.xaml`.
- Der Fix bleibt bewusst klein und rein visuell:
  - Zeile der Parzellenzuordnung Ã¼ber `MinHeight="36"` leicht erhÃ¶ht
  - die beiden Buttons `Garten zuordnen` und `Belegung beenden` jeweils von HÃ¶he `30` auf `35` angehoben
- Ziel erreicht: die Beschriftungen passen wieder besser in die Buttons; Fachlogik, Bindings und Navigation bleiben unverÃ¤ndert.

## 2026-03-24 â€“ Prompt 1/1: Appuser-Verwaltung in `Admin-MenÃ¼` umgezogen und an das ausgewÃ¤hlte Mitglied gebunden

- Den Block zuerst gegen den aktuellen Repo-Stand geprÃ¼ft. Ergebnis:
  - `Benutzerverwaltung` hing in WPF noch als globaler Navigationseintrag
  - `Admin-MenÃ¼` existierte bereits im Mitgliedskontext
  - der produktive Add-/Invite-Pfad fÃ¼r Appuser lief bereits Ã¼ber `InviteUserAsync(...)`
  - ein separater frei eingebetteter Mitgliedsbezug fÃ¼r die Benutzerverwaltung fehlte bisher
- Die Navigation wurde deshalb bewusst ohne Doppelpunkte neu geordnet:
  - globales `Benutzerverwaltung` aus der WPF-Hauptnavigation entfernt
  - neuer Unterpunkt `Benutzerverwaltung` im Mitgliedsbereich direkt unter `Admin-MenÃ¼`
  - der Unterpunkt ist eingerÃ¼ckt und folgt damit sichtbar dem Admin-MenÃ¼ statt einer zweiten parallelen Route
- Der Mitgliedsbezug wird jetzt strikt erzwungen:
  - der neue Unterpunkt navigiert mit dem aktuell ausgewÃ¤hlten `MemberDTO`
  - die WPF-`UserManagementViewModel`-Instanz ist damit an genau dieses Mitglied gebunden
  - beim Laden werden nur noch Appuser-Daten dieses Mitglieds betrachtet
  - gibt es noch keinen Appuser, wird ein Mitglieds-Platzhalter aufgebaut, damit `Nutzer hinzufÃ¼gen` weiterhin sauber auf genau diesem ausgewÃ¤hlten Datensatz arbeitet
- Die produktiven Nutzerpfade wurden nicht ersetzt, sondern weiterverwendet:
  - `Nutzer hinzufÃ¼gen` nutzt weiter `InviteUserAsync(...)`
  - `Nutzer entfernen` arbeitet auf der bestehenden Auth-/Mitglied-/`app_user`-Zuordnung und entfernt die Appuser-VerknÃ¼pfung des ausgewÃ¤hlten Mitglieds
  - es wurde keine zweite Benutzerverwaltungsarchitektur daneben erÃ¶ffnet
- Die UI-/Bedienlogik wurde fachlich nachgezogen:
  - ohne ausgewÃ¤hltes Mitglied keine AusfÃ¼hrung
  - stattdessen klare fachliche RÃ¼ckmeldung
  - `Nutzer hinzufÃ¼gen` und `Nutzer entfernen` beziehen sich jetzt sichtbar auf das ausgewÃ¤hlte Mitglied und nicht auf eine freischwebende Benutzerzeile
  - keine Verwechslung mit LÃ¶schen des Mitglieds selbst
- Rechte sauber getrennt:
  - WPF-Unterpunkt `Benutzerverwaltung` nur fÃ¼r Admin sichtbar
  - Vorstand sieht den Punkt nicht mehr
  - in MAUI wurde zumindest die MenÃ¼-Sichtbarkeit ebenfalls auf Admin begrenzt, ohne unnÃ¶tigen UI-Umbau zu starten
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - der Shared-/Auth-Pfad bleibt konsistent
  - MAUI wurde im Rechtepfad mitgezogen und nicht beschÃ¤digt

## 2026-03-24 â€“ Prompt 1/1: Mitgliedersuche um E-Mail, Gartennummern und Hauptmitglied-Markierung erweitert

- Den Block zuerst gegen den aktuellen Repo-Stand geprÃ¼ft. Die bestehende Mitgliedersuche war bereits vorhanden; erweitert werden sollte nur die Ergebnisliste, ohne neuen Suchdialog und ohne neue Fachlogik.
- FÃ¼r die zusÃ¤tzliche Ãœbersicht wurden keine neuen Backendpfade eingefÃ¼hrt. Die Erweiterung nutzt ausschlieÃŸlich vorhandene Datenquellen:
  - Mitglieder Ã¼ber `GetMitgliederAsync()`
  - Parzellen Ã¼ber `GetAllParzellenAsync()`
  - Belegungen Ã¼ber `GetAllParzellenBelegungenAsync()`
- Aus diesen vorhandenen Daten wird die Ergebnisliste jetzt angereichert um:
  - E-Mailadresse
  - Gartennummern aus aktuell aktiven Belegungen, falls vorhanden
  - Hauptmitglied-Markierung auf Basis von `hauptmitglied_id`
- WPF konkret umgesetzt:
  - bestehende GridView der Mitgliedersuche erweitert
  - zusÃ¤tzliche Spalten `E-Mail`, `Gartennummern`, `Hauptmitglied`
  - `Hauptmitglied` als deaktivierte Checkbox zur reinen Ãœbersicht
  - kein Wechsel des Suchdialogs und keine neue Selektionslogik
- MAUI mitgedacht und gleichgezogen:
  - bestehende Suchliste um Gartennummern und Hauptmitglied-Markierung ergÃ¤nzt
  - E-Mail bleibt sichtbar in den Ergebnisinfos
  - keine zweite Suchimplementierung neben der vorhandenen Seite
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich
  - damit ist der Block sowohl im WPF- als auch im MAUI-Pfad belastbar abgeschlossen

## 2026-03-24 â€“ Prompt 1/1: Arbeitseinsatz-Teilnehmerliste fÃ¼r Admin/Vorstand um `Abmelden` pro Zeile ergÃ¤nzt

- Den Block zuerst wieder gegen den realen Istzustand und ausdrÃ¼cklich gegen `_AI_DB_EXPORT` geprÃ¼ft. Belastbar bestÃ¤tigt wurden dabei in `database.types.ts` beide echten DB-Funktionspfade des An-/Abmeldekonzepts:
  - `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)`
  - `sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)`
- `roles.sql` enthÃ¤lt fÃ¼r diesen Block keine zusÃ¤tzliche App-Regel; `AI_DATABASE_CONTEXT.sql` liefert keinen alternativen App-Schreibpfad. Daraus wurde der vorhandene echte Abmeldepfad direkt als PrimÃ¤rweg verwendet.
- Vor dem Fix geprÃ¼ft: Teilnehmerliste und `HinzufÃ¼gen` fÃ¼r Admin/Vorstand waren bereits vorhanden; `sign_off_from_arbeitseinsatz(...)` wurde App-seitig aber noch nicht produktiv genutzt.
- Den Shared-Service deshalb klein ergÃ¤nzt statt eine Sonderlogik daneben zu bauen:
  - `SignOffFromArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)`
  - fachliche VorabprÃ¼fung auf gÃ¼ltigen Arbeitseinsatz/Mitgliedsbezug
  - PrÃ¼fung, ob Ã¼berhaupt eine aktive Anmeldung fÃ¼r dieses Mitglied besteht
  - produktiver RPC-Aufruf Ã¼ber `sign_off_from_arbeitseinsatz(...)`
  - verstÃ¤ndliche RÃ¼ckmeldung statt technischer Rohfehler
- Die WPF-Detailview wurde fÃ¼r Admin/Vorstand gezielt erweitert:
  - pro Teilnehmerzeile zusÃ¤tzlicher Button `Abmelden`
  - kleine RÃ¼ckfrage vor der Aktion
  - keine neue Dialogstrecke und keine freie Eingabe
  - normale Nutzeransicht bleibt unverÃ¤ndert
- Nach erfolgreichem oder fachlich abgefangenem Abmelden werden Teilnehmerliste und Detailzustand direkt neu geladen. Dadurch aktualisieren sich Ã¼ber den bestehenden Detailpfad auch KapazitÃ¤ts- und Anmeldeinformationen mit.
- Der Block bleibt bewusst klein:
  - keine Warteliste
  - keine Historienlogik
  - kein direkter Tabellenhack
  - kein Schattenpfad neben den echten DB-Funktionen
- Technisch verifiziert: `KGV.Wpf` baut nach dem neuen Abmeldepfad erfolgreich; MAUI wurde durch die kleine Shared-Service-Erweiterung nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Arbeitseinsatz-Block final geprÃ¼ft, gebaut und sauber abgeschlossen

- Den aktuellen Arbeitsbaum nochmals gezielt gegen den Auftrag geprÃ¼ft. Ergebnis: die fachlichen und technischen Ã„nderungen dieses Blocks waren bereits im Repo umgesetzt; lokal offen waren nur blockfremde Dateien und Artefakte, die bewusst nicht aufgenommen wurden.
- Die Zielpunkte des Blocks nochmals real bestÃ¤tigt:
  - `Anmelden` ist sichtbar, wenn Anmeldung fachlich mÃ¶glich ist
  - Home und Detail nutzen denselben echten Pfad `SignUpForArbeitseinsatzAsync(...)`
  - dieser Shared-Service schreibt weiter produktiv Ã¼ber `sign_up_for_arbeitseinsatz(...)`
  - Teilnehmerliste nur fÃ¼r Admin/Vorstand in der Detailview
  - `HinzufÃ¼gen` nur fÃ¼r Admin/Vorstand
  - `HinzufÃ¼gen` verwendet die bestehende Maske `Mitglied suchen`
  - das ausgewÃ¤hlte bestehende Mitglied wird Ã¼ber denselben Anmeldungspfad eingetragen wie bei Selbstanmeldung
- Die Ursache fÃ¼r das frÃ¼here Verschwinden von `Anmelden` bleibt damit klar bestÃ¤tigt: nicht der RPC war defekt, sondern die Sichtbarkeit hing zu eng nur am Startseitenzustand. Der Shared-Service bestimmt die Anmeldbarkeit jetzt belastbar aus realem `arbeitseinsatz`- und `arbeitseinsatz_anmeldung`-Zustand gegen Frist, Platzgrenze und bestehende Anmeldung.
- Den technischen Abschluss ebenfalls nochmals sauber verifiziert: `KGV.Wpf` final gebaut. Ein erster Lauf scheiterte nur an einer gesperrten laufenden `KGV.Wpf`-Instanz; nach Beenden des Prozesses lief der Build erfolgreich durch. Kein neuer Codepfad und keine neue Fachlogik waren dafÃ¼r mehr nÃ¶tig.
- Der Block ist damit jetzt wirklich abgeschlossen; fÃ¼r diesen finalen Schritt werden nur die fortgefÃ¼hrten Logdateien committed und gepusht, blockfremde lokale Ã„nderungen bleiben unberÃ¼hrt.

## 2026-03-24 â€“ Prompt 1/1: Arbeitseinsatz-Regression bei `Anmelden` und Admin-/Vorstand-Teilnehmerblock sauber abgeschlossen

- Den begonnenen Block zuerst wieder gegen den realen Arbeitsbaum und ausdrÃ¼cklich gegen den lokalen DB-Analyseexport `_AI_DB_EXPORT` geprÃ¼ft. FÃ¼r diesen Abschluss waren erneut relevant:
  - `database.types.ts` mit Tabelle `arbeitseinsatz_anmeldung`
  - Enum `arbeitseinsatz_anmeldung_status`
  - Funktion `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` als echter Schreibpfad
  - `roles.sql` ohne abweichende zusÃ¤tzliche App-Regeln
  - `AI_DATABASE_CONTEXT.sql` ohne zusÃ¤tzlichen alternativen App-Schreibpfad
- TatsÃ¤chliche Ursache der neuen Regression: `Anmelden` war nicht wegen eines fehlenden RPC verschwunden, sondern wegen der Sichtbarkeit. `CanRegister` hing im sichtbaren UI zu eng nur am Startseitenzustand. Der Shared-Service ermittelt den Anmeldestatus jetzt wieder aus den realen Basisdaten von `arbeitseinsatz` plus aktiven `arbeitseinsatz_anmeldung`-DatensÃ¤tzen gegen Frist, Platzgrenze und bereits vorhandene Anmeldung im aktuellen Benutzerkontext.
- Damit ist die Sichtbarkeit jetzt wieder fachlich belastbar bestimmt:
  - sichtbar, wenn Anmeldung im echten Zustand mÃ¶glich ist
  - nicht sichtbar, wenn bereits angemeldet, Frist abgelaufen oder keine PlÃ¤tze frei
  - kein zweiter Schattenpfad neben dem RPC
- Home und Detail nutzen weiterhin denselben echten Produktpfad:
  - Home ruft `SignUpForArbeitseinsatzAsync(...)`
  - Detail ruft ebenfalls `SignUpForArbeitseinsatzAsync(...)`
  - beide schreiben produktiv Ã¼ber `sign_up_for_arbeitseinsatz(...)`
- Die Detailview wurde fÃ¼r Admin/Vorstand jetzt gezielt erweitert, ohne die normale Ansicht zu verÃ¤ndern:
  - Teilnehmerliste nur fÃ¼r Admin/Vorstand
  - `HinzufÃ¼gen` nur fÃ¼r Admin/Vorstand
  - reale Teilnehmerdaten aus aktiven `arbeitseinsatz_anmeldung`-DatensÃ¤tzen
  - keine Teilnehmerliste fÃ¼r normale Nutzer
- `HinzufÃ¼gen` lÃ¤uft ausschlieÃŸlich Ã¼ber Modell A:
  - bestehende Maske `Mitglied suchen` wird im Auswahlmodus wiederverwendet
  - keine freie Texteingabe
  - keine PrÃ¼fung, ob das gewÃ¤hlte Mitglied App-User ist
  - ausgewÃ¤hltes bestehendes Mitglied wird anschlieÃŸend Ã¼ber denselben RPC-Pfad angemeldet wie bei Selbstanmeldung
- Den begonnenen technischen Randpunkt ebenfalls sauber finalisiert:
  - kleine Warnungsbereinigung im Auswahlpfad abgeschlossen
  - `KGV.Wpf` baut danach erfolgreich
  - der Block ist damit jetzt wirklich abgeschlossen und commit-/push-fÃ¤hig

## 2026-03-24 â€“ Prompt 1/1: `Anmelden` fÃ¼r Arbeitseinsatz im sichtbaren UI wirklich funktionsfÃ¤hig gemacht, Teilnehmerblock bewusst noch offen gelassen

- Den realen Fehlernachweis nochmals gegen den tatsÃ¤chlich laufenden WPF-Pfad gefÃ¼hrt: `HomeView.xaml`, `HomeViewModel`, `HomeSectionDetailView.xaml`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext` und `SupabaseService` waren bereits auf denselben Shared-Servicepfad fÃ¼r `Anmelden` verdrahtet; der HÃ¤nger lag nicht mehr in einer fehlenden RPC-Anbindung.
- TatsÃ¤chliche Ursache des sichtbaren No-Op: das sichtbare Home-Item fÃ¼r `Arbeitseinsatz` erhielt im Mapping seine `id` nicht. Dadurch wurde in der Detailansicht `WorkAssignmentId = 0` weitergereicht und der Detail-Command lief in einen stillen Vorab-Abbruch. Auch der Home-Pfad arbeitete damit nicht auf einem belastbaren ArbeitseinsatzschlÃ¼ssel.
- Den Korrekturpfad deshalb klein direkt an der Ursache umgesetzt:
  - `MapHomeWorkAssignment(...)` Ã¼bernimmt jetzt `record.Id` in das sichtbare `HomeWorkAssignmentItem`
  - `HomeViewModel` setzt `ShowRegisterButton` im Detailkontext jetzt ehrlich aus `item.CanRegister` statt pauschal auf `true`
  - `HomeView.xaml` zeigt den Home-Button nur dann an, wenn `CanRegister` tatsÃ¤chlich gilt
  - `HomeSectionDetailViewModel` meldet einen ungÃ¼ltigen Detailkontext jetzt sichtbar, statt still zu `return`en
- Ergebnis des Fixpfads:
  - Home-Button reagiert jetzt real auf den vorhandenen RPC-Pfad
  - Detail-Button reagiert ebenfalls real auf denselben RPC-Pfad
  - es gibt keine stillen Klicks mehr ohne Nutzerreaktion
  - nach erfolgreicher Anmeldung bleibt die kleine UI-Aktualisierung des bestehenden Blocks erhalten
- Den Admin-/Vorstand-Teilnehmerblock bewusst noch nicht erÃ¶ffnet:
  - keine Teilnehmerliste
  - kein `HinzufÃ¼gen`
  - keine neue Detailerweiterung fÃ¼r Admin/Vorstand
  - dieser Block blieb ausdrÃ¼cklich nur auf dem normalen `Anmelden`-Pfad
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten UI-/Datensatzfix erfolgreich; MAUI wurde durch die kleine Shared-/WPF-Korrektur nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: RPC-Anmeldeblock fÃ¼r Arbeitseinsatz sauber abgeschlossen, committed und gepusht

- Den begonnenen Arbeitseinsatz-Anmeldeblock ohne neue Fachlogik sauber abgeschlossen.
- Abschlussverifikation: `KGV.Wpf` baut erfolgreich; der produktive Pfad bleibt `sign_up_for_arbeitseinsatz(...)` Ã¼ber den Shared-Service `SignUpForArbeitseinsatzAsync(...)`.
- Home und Detailview nutzen weiterhin denselben Servicepfad; es wurde kein zweiter ViewModel-Schreibpfad erÃ¶ffnet.
- Doppelanmeldung, abgelaufene Frist und volle Platzgrenze bleiben vor dem RPC geprÃ¼ft und werden zusÃ¤tzlich bei einem mÃ¶glichen RPC-Rennen verstÃ¤ndlich abgefangen.
- FÃ¼r Commit und Push wurden ausschlieÃŸlich die Blockdateien aufgenommen; blockfremde lokale Ã„nderungen und Artefakte blieben bewusst unberÃ¼hrt.

## 2026-03-24 â€“ Prompt 1/1: `Anmelden` fÃ¼r Arbeitseinsatz produktiv Ã¼ber den echten DB-Funktionspfad angeschlossen

- Den Block zuerst ausdrÃ¼cklich gegen den lokalen DB-Analyseexport `_AI_DB_EXPORT` gefÃ¼hrt und nicht gegen Vermutungen. Belastbar geprÃ¼ft wurden `database.types.ts`, `roles.sql` sowie der mitreferenzierte SQL-Kontext. Ausschlaggebend war dabei die in `database.types.ts` bestÃ¤tigte DB-Struktur:
  - Tabelle `arbeitseinsatz_anmeldung`
  - Enum `arbeitseinsatz_anmeldung_status` mit u. a. `angemeldet`
  - Funktion `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` mit RÃ¼ckgabe eines `arbeitseinsatz_anmeldung`-Datensatzes
  - Funktion `sign_off_from_arbeitseinsatz(...)`, die fÃ¼r diesen Block bewusst noch nicht produktiv geÃ¶ffnet wurde
- Daraus den fachlich richtigen Produktpfad abgeleitet: fÃ¼r die Anmeldung wird produktiv der vorhandene RPC-/DB-Funktionspfad `sign_up_for_arbeitseinsatz(...)` verwendet statt eines App-seitigen Direktinserts in `arbeitseinsatz_anmeldung`.
- Den aktuellen Repo-Istzustand davor nochmals geprÃ¼ft:
  - Home und Detailview hatten bereits sichtbare `Anmelden`-Buttons
  - beide hingen aber noch an einer reinen Hinweislogik in `HomeViewModel` bzw. `HomeSectionDetailViewModel`
  - ein echter Schreibpfad im `SupabaseService` existierte dafÃ¼r im aktuellen Repo noch nicht
- Den produktiven Shared-Servicepfad deshalb klein ergÃ¤nzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` lÃ¤uft jetzt zentral Ã¼ber `SupabaseService` und wird Ã¼ber `ISupabaseService` bereitgestellt.
- Vor dem eigentlichen RPC werden die geforderten Mindestregeln real und klein geprÃ¼ft:
  - fehlender Arbeitseinsatz-/Mitgliedsbezug -> verstÃ¤ndliche RÃ¼ckmeldung
  - bestehende aktive Anmeldung (`arbeitseinsatz_anmeldung.status = angemeldet`) -> keine Doppelanmeldung, verstÃ¤ndliche RÃ¼ckmeldung
  - `anmeldung_bis` abgelaufen -> verstÃ¤ndliche RÃ¼ckmeldung
  - `max_teilnehmer` gesetzt und bereits erreicht -> verstÃ¤ndliche RÃ¼ckmeldung
  - `max_teilnehmer = NULL` -> normale Anmeldung zulÃ¤ssig
  - keine Warteliste und keine Abmeldung in diesem Block
- FÃ¼r den eigentlichen Schreibschritt wird anschlieÃŸend der bestÃ¤tigte DB-Funktionspfad genutzt:
  - `client.Rpc<ArbeitseinsatzAnmeldungRecord>("sign_up_for_arbeitseinsatz", ...)`
  - damit bleibt die App auf dem vorhandenen DB-Vertrag statt neuer Schattenarchitektur
  - falls im Rennen zwischen VorabprÃ¼fung und RPC doch ein DB-seitiger Konflikt auftritt, wird dieser klein in eine verstÃ¤ndliche App-RÃ¼ckmeldung Ã¼bersetzt
- Home und Detailview nutzen jetzt denselben echten Servicepfad statt doppelter Logik:
  - `HomeViewModel.RegisterForWorkAssignmentCommand` ruft den Shared-Service auf
  - `HomeSectionDetailViewModel.AnmeldenCommand` ruft denselben Shared-Service Ã¼ber den im Detailkontext mitgegebenen `WorkAssignmentId` auf
  - die Detailansicht erhielt dafÃ¼r nur den kleinen zusÃ¤tzlichen Kontextwert `WorkAssignmentId`, keine neue Navigation
- Den Mitgliedsbezug bewusst am real vorhandenen Benutzerpfad gehalten:
  - bevorzugt `UserContext.MitgliedId`
  - kleiner Fallback Ã¼ber `EnsureCurrentMemberSelectedAsync()`
  - keine neue Sonderzuordnung
- UI-Verhalten nach erfolgreicher Anmeldung produktiv klein gehalten:
  - Home und Detail erhalten den aktualisierten Startseiten-Datensatz zurÃ¼ck
  - Registrierungs-/KapazitÃ¤tsanzeige wird damit direkt nachgezogen
  - der Button wird ehrlich deaktiviert, damit keine zweite direkte Anmeldung aus derselben Sitzung ausgelÃ¶st wird
  - keine neue groÃŸe UI-Architektur
- MAUI wurde in diesem Block nicht mit neuer OberflÃ¤che ausgebaut, aber der produktive Anmeldepfad liegt jetzt im Shared-Service und ist damit fÃ¼r spÃ¤tere mobile ParitÃ¤t vorbereitet statt verbaut.
- Offene Restpunkte bewusst klein gelassen:
  - `sign_off_from_arbeitseinsatz(...)` bleibt fÃ¼r einen spÃ¤teren separaten Block offen
  - keine Warteliste
  - keine zusÃ¤tzliche Anmeldehistorien-UI
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde durch die Shared-Service-Erweiterung nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Start-/Endzeit fÃ¼r Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Restfehler nach der expliziten WPF-Bindung nochmals bewusst Ende-zu-Ende geprÃ¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren weiterhin die sichtbare Strecke; dort war die Bindung inzwischen korrekt.
- Der tatsÃ¤chliche HÃ¤nger saÃŸ davor im Datenursprung des Home-Pfads: die Startseiten-Views `v_startseite_arbeitseinsatz` und `v_startseite_termine` lieferten `Beginn`/`Ende` im aktuellen Stand nicht in jedem Fall belastbar, obwohl die Zeiten in den Basistabellen `arbeitseinsatz.start_uhrzeit`, `arbeitseinsatz.end_uhrzeit`, `termin.start_uhrzeit` und `termin.end_uhrzeit` vorhanden sein konnten.
- Den finalen Fix deshalb klein am vorhandenen Home-Servicepfad umgesetzt statt nochmals an der View zu drehen:
  - `LoadStartseiteArbeitseinsaetzeAsync()` lÃ¤dt weiter aus `v_startseite_arbeitseinsatz`
  - `LoadStartseiteTermineAsync()` lÃ¤dt weiter aus `v_startseite_termine`
  - wenn dort `Beginn`/`Ende` leer ankommen, werden die Zeitwerte gezielt Ã¼ber die vorhandene Datensatz-`Id` aus `arbeitseinsatz` bzw. `termin` nachangereichert
  - die Nachanreicherung greift nur fÃ¼r fehlende Zeitwerte; bestehende View-Werte bleiben unverÃ¤ndert
- Damit ist jetzt final abgesichert:
  - `start_uhrzeit` und `end_uhrzeit` erreichen den tatsÃ¤chlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lÃ¤sst
  - Home zeigt den Zeitraum weiter aus derselben einen Zeitquelle
  - die Detailansicht rendert `Beginn` und `Ende` weiter explizit als eigene Datensatzangaben
  - `Bekanntmachung` bleibt unverÃ¤ndert unbeeintrÃ¤chtigt
- Fachlich bleibt der Block bewusst klein: keine neue Home-Architektur, keine neue Navigation, keine Schattenlogik, sondern nur eine gezielte Fallback-Anreicherung im bestehenden Lese-/Mappingpfad.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-ErgÃ¤nzung nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Start- und Endzeit im tatsÃ¤chlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler ausdrÃ¼cklich nochmals Ende-zu-Ende gegen den real gerenderten WPF-Pfad geprÃ¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren gemeinsam die tatsÃ¤chlich sichtbare Strecke dieses Blocks.
- Ergebnis der PrÃ¼fung: `start_uhrzeit` und `end_uhrzeit` kamen im Mapping als normalisierte Werte grundsÃ¤tzlich an, blieben im UI aber weiterhin nicht zuverlÃ¤ssig genug sichtbar, weil der letzte Pfad noch zu stark an einem zusammengesetzten Zeitfeld hing und die Detailansicht die Zeiten nicht als eigene Datensatzangaben renderte.
- Die Korrektur deshalb jetzt am tatsÃ¤chlich sichtbaren Anzeigeursprung umgesetzt:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt explizit `StartTimeText` und `EndTimeText`
  - der Home-/Detailkontext Ã¼bernimmt dieselben Felder 1:1 fÃ¼r genau den ausgewÃ¤hlten Datensatz
  - `SupabaseService` schreibt die vorhandenen `Beginn`-/`Ende`-Werte nach `NormalizeTimeValue(...)` gezielt in diese Felder
- Damit war belastbar abgesichert:
  - `start_uhrzeit` / `end_uhrzeit` kommen im Modell an
  - sie werden nicht mehr erst spÃ¤t oder indirekt aus `Subtitle`, `AdditionalInfo` oder anderen Nebenfeldern abgeleitet
  - die final sichtbare Bindung greift direkt auf diese expliziten Start-/Endfelder zu
- Home-Anzeige finaler Sichtpfad:
  - `HomeView.xaml` zeigt den Zeitraum weiter als klare Zeitzeile aus genau dieser einen Quelle
  - wenn nur eine Zeit vorhanden ist, bleibt genau diese sichtbar
  - wenn beide Zeiten vorhanden sind, erscheint konsistent `Start - Ende`
- Detailanzeige finaler Sichtpfad:
  - `HomeSectionDetailView.xaml` rendert `Beginn` und `Ende` jetzt als eigene sichtbare Datensatzangaben
  - die Zeit ist damit fÃ¼r `Arbeitseinsatz` und `Termin` nicht nur implizit oder im FlieÃŸtext vorhanden, sondern explizit im sichtbaren UI erkennbar
  - der Detailpfad bleibt weiterhin auf die Skalardaten des ausgewÃ¤hlten Datensatzes beschrÃ¤nkt
- `Bekanntmachung` wurde erneut mitgeprÃ¼ft und nicht verschlechtert: dort bleiben die neuen Zeitfelder leer, wÃ¤hrend die generische Detailstruktur unverÃ¤ndert funktioniert.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/KontextergÃ¤nzungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Start- und Endzeit im tatsÃ¤chlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler ausdrÃ¼cklich nochmals Ende-zu-Ende gegen den real gerenderten WPF-Pfad geprÃ¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren gemeinsam die tatsÃ¤chlich sichtbare Strecke dieses Blocks.
- Ergebnis der PrÃ¼fung: `start_uhrzeit` und `end_uhrzeit` kamen im Mapping als normalisierte Werte grundsÃ¤tzlich an, blieben im UI aber weiterhin nicht zuverlÃ¤ssig genug sichtbar, weil der letzte Pfad noch zu stark an einem zusammengesetzten Zeitfeld hing und die Detailansicht die Zeiten nicht als eigene Datensatzangaben renderte.
- Die Korrektur deshalb jetzt am tatsÃ¤chlich sichtbaren Anzeigeursprung umgesetzt:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt explizit `StartTimeText` und `EndTimeText`
  - der Home-/Detailkontext Ã¼bernimmt dieselben Felder 1:1 fÃ¼r genau den ausgewÃ¤hlten Datensatz
  - `SupabaseService` schreibt die vorhandenen `Beginn`-/`Ende`-Werte nach `NormalizeTimeValue(...)` gezielt in diese Felder
- Damit war belastbar abgesichert:
  - `start_uhrzeit` / `end_uhrzeit` kommen im Modell an
  - sie werden nicht mehr erst spÃ¤t oder indirekt aus `Subtitle`, `AdditionalInfo` oder anderen Nebenfeldern abgeleitet
  - die final sichtbare Bindung greift direkt auf diese expliziten Start-/Endfelder zu
- Home-Anzeige finaler Sichtpfad:
  - `HomeView.xaml` zeigt den Zeitraum weiter als klare Zeitzeile aus genau dieser einen Quelle
  - wenn nur eine Zeit vorhanden ist, bleibt genau diese sichtbar
  - wenn beide Zeiten vorhanden sind, erscheint konsistent `Start - Ende`
- Detailanzeige finaler Sichtpfad:
  - `HomeSectionDetailView.xaml` rendert `Beginn` und `Ende` jetzt als eigene sichtbare Datensatzangaben
  - die Zeit ist damit fÃ¼r `Arbeitseinsatz` und `Termin` nicht nur implizit oder im FlieÃŸtext vorhanden, sondern explizit im sichtbaren UI erkennbar
  - der Detailpfad bleibt weiterhin auf die Skalardaten des ausgewÃ¤hlten Datensatzes beschrÃ¤nkt
- `Bekanntmachung` wurde erneut mitgeprÃ¼ft und nicht verschlechtert: dort bleiben die neuen Zeitfelder leer, wÃ¤hrend die generische Detailstruktur unverÃ¤ndert funktioniert.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/KontextergÃ¤nzungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Home-/Detailanzeige fÃ¼r Arbeitseinsatz und Termin gezielt korrigiert: Uhrzeit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den bestehenden Anzeige-/Mappingpfad erneut gegen den realen Istzustand geprÃ¼ft: `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und das Home-Mapping in `SupabaseService` waren der tatsÃ¤chliche Fehlerort dieses Blocks.
- Sichtbarer Hauptbefund: die Uhrzeit hing bislang nicht an einem eindeutigen Anzeigeweg. Sie lief teils im `Subtitle`, teils als separater Beginn-Text und in der Detailansicht zusÃ¤tzlich in `AdditionalInfo`, wodurch sie im aktuellen UI nicht verlÃ¤sslich bzw. nicht klar genug sichtbar war.
- Den Zeitpfad deshalb gezielt auf eine einzige explizite Anzeigeform gezogen:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `TimeText` statt eines bloÃŸen Beginnfelds
  - `SupabaseService` erzeugt diesen Wert zentral Ã¼ber `BuildTimeRange(...)` aus Start- und Endzeit
  - `HomeView.xaml` rendert `Uhrzeit: {TimeText}` jetzt fÃ¼r `Arbeitseinsatz` und `Termin` sichtbar in der Ãœbersicht
  - damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, erscheint konsistent `Start â€“ Ende`
- Dieselbe Korrektur auf den Detailpfad Ã¼bertragen:
  - `HomeSectionDetailContext` wurde um `TimeText` ergÃ¤nzt
  - `HomeSectionDetailViewModel` reicht ihn unverÃ¤ndert nur fÃ¼r den ausgewÃ¤hlten Datensatz weiter
  - `HomeSectionDetailView.xaml` zeigt `Uhrzeit` jetzt separat sichtbar an
  - dadurch sind Start- und Endzeit fÃ¼r den ausgewÃ¤hlten `Arbeitseinsatz` und `Termin` in der Detailansicht klar sichtbar, ohne neue Navigation oder Mehrfachanzeige
- Die doppelte Anzeige `Angemeldet: 0` konkret am Mappingursprung beseitigt:
  - bislang kamen KapazitÃ¤tsdaten zweimal aus demselben Datensatzpfad
  - einmal als einzelne Zeilen `Angemeldet` / `Freie PlÃ¤tze` in `DetailInfo`
  - zusÃ¤tzlich nochmals zusammengefasst in `RegistrationInfo` Ã¼ber `BuildCapacityText(...)`
  - der `Arbeitseinsatz`-Mappingpfad wurde deshalb bereinigt: `RegistrationInfo` bleibt als einzige KapazitÃ¤tsanzeige erhalten; die redundanten Einzelzeilen wurden aus `DetailInfo` entfernt
- Ergebnis des Korrekturpfads:
  - Home zeigt die Uhrzeit jetzt sichtbar und eindeutig fÃ¼r `Arbeitseinsatz` und `Termin`
  - die Detailansicht zeigt Start-/Endzeit des ausgewÃ¤hlten Datensatzes sichtbar an
  - `Angemeldet: 0` erscheint fÃ¼r `Arbeitseinsatz` nicht mehr mehrfach
  - die Detailview bleibt auf die Skalardaten des konkret ausgewÃ¤hlten Datensatzes beschrÃ¤nkt und zeigt keine Sammel-/Mehrfachanzeige
- `Bekanntmachung` und die generische Detailstruktur wurden mitgeprÃ¼ft und nicht verschlechtert: dort bleibt `TimeText` leer, wÃ¤hrend `Subtitle`, `AdditionalInfo` und `Content` unverÃ¤ndert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fixblock erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Home-/Detailansicht fÃ¼r ArbeitseinsÃ¤tze und Termine sichtbar vervollstÃ¤ndigt, ohne neue Schattenlogik

- Den vorhandenen Home-/Detailpfad zuerst vollstÃ¤ndig gegen den realen Arbeitsbaum geprÃ¼ft: `HomeView.xaml`, `HomeViewModel`, `HomeSectionDetailView.xaml`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Item-Modelle sowie das Mapping in `SupabaseService` waren bereits produktiv vorhanden und dienten als Basis des Blocks.
- Ergebnis der IstzustandsprÃ¼fung:
  - `Arbeitseinsatz` trug die Startzeit im Home-Item bereits separat als `BeginText`
  - `Termin` hatte die Zeit zwar indirekt im Untertitel/Detailmapping, aber noch nicht separat sichtbar in der Home-Karte
  - die Detailansicht arbeitete bereits mit einem generischen Kontext aus Skalardaten, zeigte aber die Registrierungsinformation des ausgewÃ¤hlten Datensatzes noch nicht separat an
  - der `Anmelden`-Button war im Detailpfad nicht konsistent genug an den Arbeitseinsatz-Kontext gebunden
- Die Home-Ãœbersicht deshalb gezielt fachlich vervollstÃ¤ndigt und nicht umgebaut:
  - `HomeAppointmentItem` wurde um `BeginText`/`HasBeginText` ergÃ¤nzt
  - das Termin-Mapping in `SupabaseService` reicht den normalisierten Beginn jetzt explizit in das Home-Item durch
  - `HomeView.xaml` zeigt damit bei `Termin` den Beginn sichtbar in derselben kleinen Kartenlogik wie bei `Arbeitseinsatz`
  - `Arbeitseinsatz` blieb bei der bereits passenden sichtbaren Beginn-Anzeige
- Die Detailansicht innerhalb des bestehenden generischen Pfads vervollstÃ¤ndigt:
  - `HomeSectionDetailContext` trÃ¤gt jetzt zusÃ¤tzlich `RegistrationInfo` und einen expliziten Schalter fÃ¼r die Sichtbarkeit der Anmelden-Aktion
  - `HomeSectionDetailViewModel` reicht diese Daten 1:1 an die View weiter, ohne neue Sammel- oder Listenlogik
  - `HomeSectionDetailView.xaml` zeigt die Registrierungsinformation des ausgewÃ¤hlten Datensatzes jetzt in derselben Detailkarte zusÃ¤tzlich an
  - der `Anmelden`-Button bleibt in der Detailansicht fÃ¼r `Arbeitseinsatz` sichtbar und verwendet denselben ehrlichen Hinweisdialog wie auf Home
- Den Punkt â€žDetailview zeigt nur den ausgewÃ¤hlten Datensatzâ€œ bewusst am bestehenden Architekturpfad abgesichert:
  - `HomeViewModel` Ã¼bergibt beim Ã–ffnen der Detailansicht ausschlieÃŸlich die Skalardaten des konkret ausgewÃ¤hlten Home-Items
  - keine Listen, keine Mehrfachobjekte und kein nachtrÃ¤gliches Nachladen eines unscharfen Datensatzpools im Detailpfad
  - dadurch bleibt die Detailansicht auf genau den angeklickten Datensatz begrenzt
- `Termin`, `Arbeitseinsatz` und `Bekanntmachung` bleiben dabei im selben generischen DetailgerÃ¼st:
  - `Arbeitseinsatz` erhÃ¤lt zusÃ¤tzliche sichtbare Registrierungs-/Aktionskonsistenz
  - `Termin` erhÃ¤lt die separate Startzeit auf Home und behÃ¤lt die Datensatzdetails im Detailkontext
  - `Bekanntmachung` verliert keine bestehenden Felder; die generische Detaildarstellung bleibt intakt
- Offener Restpunkt bleibt bewusst unverÃ¤ndert klein: der echte produktive Schreibpfad fÃ¼r `Anmelden` ist im Repo weiterhin nicht belastbar vorhanden. Deshalb bleibt die Aktion auf Home und in der Detailansicht eine ehrliche Info-Verdrahtung statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die kleinen Kontext-/DarstellungsÃ¤nderungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Arbeitsstunden endgÃ¼ltig gegen `id = 0` abgesichert, Save-RÃ¼cknavigation abgeschlossen, Arbeitseinsatz-Home-Buttons korrigiert

- Den Block erneut bewusst gegen den realen laufenden Produktpfad gefÃ¼hrt, weil der erneute Datensatz `arbeitsstunde.id = 0` belegt hat, dass der vorige Schutz im tatsÃ¤chlichen Laufzeitverhalten noch nicht ausreichte.
- Den kompletten Create-Pfad fÃ¼r `arbeitsstunde` nochmals Ende-zu-Ende geprÃ¼ft:
  - WPF-Erfassung Ã¼ber `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad Ã¼ber `ArbeitsstundenViewModel`
  - MAUI-Erfassung Ã¼ber `MyArbeitsstundenPage`
  - Persistenzpfad Ã¼ber `SupabaseService.AddArbeitsstundeAsync(...)`
  - Ergebnis: die Aufrufer erzeugen weiterhin neue `ArbeitsstundeRecord`-Instanzen ohne fachliche `Id`, typbedingt aber mit lokalem Default `0`
- Daraus die jetzt robuste technische Konsequenz gezogen: auf Attributverhalten allein wird fÃ¼r `arbeitsstunde` nicht mehr vertraut. Stattdessen verwendet der Create-Pfad nun einen expliziten Insert-Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`).
- Der tatsÃ¤chliche Korrekturpfad fÃ¼r den Insertfehler ist damit jetzt zweistufig belastbar:
  - `AddArbeitsstundeAsync(...)` mappt neue DatensÃ¤tze in einen separaten Insert-Payload ohne PrimÃ¤rschlÃ¼sselspalte
  - die Create-Payload Ã¼bernimmt `Id` grundsÃ¤tzlich Ã¼berhaupt nicht mehr; damit kann auch ein ankommendes `ArbeitsstundeRecord` mit `Id <= 0` technisch nicht mehr in einen Insert mit `id = 0` mÃ¼nden
  - Updates bleiben unverÃ¤ndert Ã¼ber `UpdateArbeitsstundeAsync(...)` und die echte bestehende `Id`
  - keine `lastrow+1`-Logik
- Den Abschluss des Userflows fÃ¼r Arbeitsstunden zugleich produktiv nachgezogen:
  - nach erfolgreichem Speichern in `Arbeitsstunden erfassen` bleibt die Eingabemaske nicht mehr offen stehen
  - im Dialogkontext schlieÃŸt sich das Fenster wie bisher
  - im normalen WPF-Erfassungspfad wird nach erfolgreichem Save sauber zurÃ¼ck auf `Home` navigiert
- Dasselbe Abschlussverhalten jetzt auch fÃ¼r `Arbeitsstunden freigeben` umgesetzt:
  - Checkbox-/Status-Speichern bleibt wie im letzten Fix mÃ¶glich
  - wenn alle geÃ¤nderten/markierten Zeilen erfolgreich gespeichert wurden, navigiert die WPF-PrÃ¼fansicht anschlieÃŸend wieder auf `Home` zurÃ¼ck
  - der Fachpfad bleibt klein: `status` bleibt optionales Anmerkungsfeld, offen bleibt `freigegeben = false`, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
- ZusÃ¤tzlich den Home-Pfad fÃ¼r `Arbeitseinsatz` auf den gewÃ¼nschten Interaktionsstand zurÃ¼ckgezogen:
  - der separate `Details`-Button wurde entfernt
  - DetailÃ¶ffnung lÃ¤uft jetzt per Doppelklick auf die Karte
  - stattdessen bleibt dort `Anmelden` sichtbar
  - weil weiterhin kein belastbarer produktiver Schreibpfad fÃ¼r echte Anmeldungen im Repo nachweisbar ist, bleibt der Button bewusst eine ehrliche Info-Aktion statt neuer Schattenlogik
- Technisch verifiziert: `KGV.Wpf` baut nach dem Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Home-/Detailanzeige fÃ¼r Arbeitseinsatz und Termin gezielt korrigiert: Uhrzeit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den bestehenden Anzeige-/Mappingpfad erneut gegen den realen Istzustand geprÃ¼ft: `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und das Home-Mapping in `SupabaseService` waren der tatsÃ¤chliche Fehlerort dieses Blocks.
- Sichtbarer Hauptbefund: die Uhrzeit hing bislang nicht an einem eindeutigen Anzeigeweg. Sie lief teils im `Subtitle`, teils als separater Beginn-Text und in der Detailansicht zusÃ¤tzlich in `AdditionalInfo`, wodurch sie im aktuellen UI nicht verlÃ¤sslich bzw. nicht klar genug sichtbar war.
- Den Zeitpfad deshalb gezielt auf eine einzige explizite Anzeigeform gezogen:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `TimeText` statt eines bloÃŸen Beginnfelds
  - `SupabaseService` erzeugt diesen Wert zentral Ã¼ber `BuildTimeRange(...)` aus Start- und Endzeit
  - `HomeView.xaml` rendert `Uhrzeit: {TimeText}` jetzt fÃ¼r `Arbeitseinsatz` und `Termin` sichtbar in der Ãœbersicht
  - damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, erscheint konsistent `Start â€“ Ende`
- Dieselbe Korrektur auf den Detailpfad Ã¼bertragen:
  - `HomeSectionDetailContext` wurde um `TimeText` ergÃ¤nzt
  - `HomeSectionDetailViewModel` reicht ihn unverÃ¤ndert nur fÃ¼r den ausgewÃ¤hlten Datensatz weiter
  - `HomeSectionDetailView.xaml` zeigt `Uhrzeit` jetzt separat sichtbar an
  - dadurch sind Start- und Endzeit fÃ¼r den ausgewÃ¤hlten `Arbeitseinsatz` und `Termin` in der Detailansicht klar sichtbar, ohne neue Navigation oder Mehrfachanzeige
- Die doppelte Anzeige `Angemeldet: 0` konkret am Mappingursprung beseitigt:
  - bislang kamen KapazitÃ¤tsdaten zweimal aus demselben Datensatzpfad
  - einmal als einzelne Zeilen `Angemeldet` / `Freie PlÃ¤tze` in `DetailInfo`
  - zusÃ¤tzlich nochmals zusammengefasst in `RegistrationInfo` Ã¼ber `BuildCapacityText(...)`
  - der `Arbeitseinsatz`-Mappingpfad wurde deshalb bereinigt: `RegistrationInfo` bleibt als einzige KapazitÃ¤tsanzeige erhalten; die redundanten Einzelzeilen wurden aus `DetailInfo` entfernt
- Ergebnis des Korrekturpfads:
  - Home zeigt die Uhrzeit jetzt sichtbar und eindeutig fÃ¼r `Arbeitseinsatz` und `Termin`
  - die Detailansicht zeigt Start-/Endzeit des ausgewÃ¤hlten Datensatzes sichtbar an
  - `Angemeldet: 0` erscheint fÃ¼r `Arbeitseinsatz` nicht mehr mehrfach
  - die Detailview bleibt auf die Skalardaten des konkret ausgewÃ¤hlten Datensatzes beschrÃ¤nkt und zeigt keine Sammel-/Mehrfachanzeige
- `Bekanntmachung` und die generische Detailstruktur wurden mitgeprÃ¼ft und nicht verschlechtert: dort bleibt `TimeText` leer, wÃ¤hrend `Subtitle`, `AdditionalInfo` und `Content` unverÃ¤ndert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fixblock erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Home-/Detailansicht fÃ¼r ArbeitseinsÃ¤tze und Termine sichtbar vervollstÃ¤ndigt, ohne neue Schattenlogik

- Den vorhandenen Home-/Detailpfad zuerst vollstÃ¤ndig gegen den realen Arbeitsbaum geprÃ¼ft: `HomeView.xaml`, `HomeViewModel`, `HomeSectionDetailView.xaml`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Item-Modelle sowie das Mapping in `SupabaseService` waren bereits produktiv vorhanden und dienten als Basis des Blocks.
- Ergebnis der IstzustandsprÃ¼fung:
  - `Arbeitseinsatz` trug die Startzeit im Home-Item bereits separat als `BeginText`
  - `Termin` hatte die Zeit zwar indirekt im Untertitel/Detailmapping, aber noch nicht separat sichtbar in der Home-Karte
  - die Detailansicht arbeitete bereits mit einem generischen Kontext aus Skalardaten, zeigte aber die Registrierungsinformation des ausgewÃ¤hlten Datensatzes noch nicht separat an
  - der `Anmelden`-Button war im Detailpfad nicht konsistent genug an den Arbeitseinsatz-Kontext gebunden
- Die Home-Ãœbersicht deshalb gezielt fachlich vervollstÃ¤ndigt und nicht umgebaut:
  - `HomeAppointmentItem` wurde um `BeginText`/`HasBeginText` ergÃ¤nzt
  - das Termin-Mapping in `SupabaseService` reicht den normalisierten Beginn jetzt explizit in das Home-Item durch
  - `HomeView.xaml` zeigt damit bei `Termin` den Beginn sichtbar in derselben kleinen Kartenlogik wie bei `Arbeitseinsatz`
  - `Arbeitseinsatz` blieb bei der bereits passenden sichtbaren Beginn-Anzeige
- Die Detailansicht innerhalb des bestehenden generischen Pfads vervollstÃ¤ndigt:
  - `HomeSectionDetailContext` trÃ¤gt jetzt zusÃ¤tzlich `RegistrationInfo` und einen expliziten Schalter fÃ¼r die Sichtbarkeit der Anmelden-Aktion
  - `HomeSectionDetailViewModel` reicht diese Daten 1:1 an die View weiter, ohne neue Sammel- oder Listenlogik
  - `HomeSectionDetailView.xaml` zeigt die Registrierungsinformation des ausgewÃ¤hlten Datensatzes jetzt in derselben Detailkarte zusÃ¤tzlich an
  - der `Anmelden`-Button bleibt in der Detailansicht fÃ¼r `Arbeitseinsatz` sichtbar und verwendet denselben ehrlichen Hinweisdialog wie auf Home
- Den Punkt â€žDetailview zeigt nur den ausgewÃ¤hlten Datensatzâ€œ bewusst am bestehenden Architekturpfad abgesichert:
  - `HomeViewModel` Ã¼bergibt beim Ã–ffnen der Detailansicht ausschlieÃŸlich die Skalardaten des konkret ausgewÃ¤hlten Home-Items
  - keine Listen, keine Mehrfachobjekte und kein nachtrÃ¤gliches Nachladen eines unscharfen Datensatzpools im Detailpfad
  - dadurch bleibt die Detailansicht auf genau den angeklickten Datensatz begrenzt
- `Termin`, `Arbeitseinsatz` und `Bekanntmachung` bleiben dabei im selben generischen DetailgerÃ¼st:
  - `Arbeitseinsatz` erhÃ¤lt zusÃ¤tzliche sichtbare Registrierungs-/Aktionskonsistenz
  - `Termin` erhÃ¤lt die separate Startzeit auf Home und behÃ¤lt die Datensatzdetails im Detailkontext
  - `Bekanntmachung` verliert keine bestehenden Felder; die generische Detaildarstellung bleibt intakt
- Offener Restpunkt bleibt bewusst unverÃ¤ndert klein: der echte produktive Schreibpfad fÃ¼r `Anmelden` ist im Repo weiterhin nicht belastbar vorhanden. Deshalb bleibt die Aktion auf Home und in der Detailansicht eine ehrliche Info-Verdrahtung statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die kleinen Kontext-/DarstellungsÃ¤nderungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Prompt 1/1: Arbeitsstunden endgÃ¼ltig gegen `id = 0` abgesichert, Save-RÃ¼cknavigation abgeschlossen, Arbeitseinsatz-Home-Buttons korrigiert

- Den Block erneut bewusst gegen den realen laufenden Produktpfad gefÃ¼hrt, weil der erneute Datensatz `arbeitsstunde.id = 0` belegt hat, dass der vorige Schutz im tatsÃ¤chlichen Laufzeitverhalten noch nicht ausreichte.
- Den kompletten Create-Pfad fÃ¼r `arbeitsstunde` nochmals Ende-zu-Ende geprÃ¼ft:
  - WPF-Erfassung Ã¼ber `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad Ã¼ber `ArbeitsstundenViewModel`
  - MAUI-Erfassung Ã¼ber `MyArbeitsstundenPage`
  - Persistenzpfad Ã¼ber `SupabaseService.AddArbeitsstundeAsync(...)`
  - Ergebnis: die Aufrufer erzeugen weiterhin neue `ArbeitsstundeRecord`-Instanzen ohne fachliche `Id`, typbedingt aber mit lokalem Default `0`
- Daraus die jetzt robuste technische Konsequenz gezogen: auf Attributverhalten allein wird fÃ¼r `arbeitsstunde` nicht mehr vertraut. Stattdessen verwendet der Create-Pfad nun einen expliziten Insert-Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`).
- Der tatsÃ¤chliche Korrekturpfad fÃ¼r den Insertfehler ist damit jetzt zweistufig belastbar:
  - `AddArbeitsstundeAsync(...)` mappt neue DatensÃ¤tze in einen separaten Insert-Payload ohne PrimÃ¤rschlÃ¼sselspalte
  - die Create-Payload Ã¼bernimmt `Id` grundsÃ¤tzlich Ã¼berhaupt nicht mehr; damit kann auch ein ankommendes `ArbeitsstundeRecord` mit `Id <= 0` technisch nicht mehr in einen Insert mit `id = 0` mÃ¼nden
  - Updates bleiben unverÃ¤ndert Ã¼ber `UpdateArbeitsstundeAsync(...)` und die echte bestehende `Id`
  - keine `lastrow+1`-Logik
- Den Abschluss des Userflows fÃ¼r Arbeitsstunden zugleich produktiv nachgezogen:
  - nach erfolgreichem Speichern in `Arbeitsstunden erfassen` bleibt die Eingabemaske nicht mehr offen stehen
  - im Dialogkontext schlieÃŸt sich das Fenster wie bisher
  - im normalen WPF-Erfassungspfad wird nach erfolgreichem Save sauber zurÃ¼ck auf `Home` navigiert
- Dasselbe Abschlussverhalten jetzt auch fÃ¼r `Arbeitsstunden freigeben` umgesetzt:
  - Checkbox-/Status-Speichern bleibt wie im letzten Fix mÃ¶glich
  - wenn alle geÃ¤nderten/markierten Zeilen erfolgreich gespeichert wurden, navigiert die WPF-PrÃ¼fansicht anschlieÃŸend wieder auf `Home` zurÃ¼ck
  - der Fachpfad bleibt klein: `status` bleibt optionales Anmerkungsfeld, offen bleibt `freigegeben = false`, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
- ZusÃ¤tzlich den Home-Pfad fÃ¼r `Arbeitseinsatz` auf den gewÃ¼nschten Interaktionsstand zurÃ¼ckgezogen:
  - der separate `Details`-Button wurde entfernt
  - DetailÃ¶ffnung lÃ¤uft jetzt per Doppelklick auf die Karte
  - stattdessen bleibt dort `Anmelden` sichtbar
  - weil weiterhin kein belastbarer produktiver Schreibpfad fÃ¼r echte Anmeldungen im Repo nachweisbar ist, bleibt der Button bewusst eine ehrliche Info-Aktion statt neuer Schattenlogik
- Technisch verifiziert: `KGV.Wpf` baut nach dem Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-23 â€“ Prompt 1/1: zwei echte Arbeitsstundenfehler am realen Produktpfad behoben (`id = 0` bei Insert, Speichern in Freigabeansicht reagiert nicht auf Checkbox)

- Den Block ausdrÃ¼cklich gegen die zwei real reproduzierten Fehler gefÃ¼hrt und nicht als VermutungslÃ¶sung umgesetzt.
- Ersten Fehler `id = 0` im echten Neuanlagepfad vollstÃ¤ndig zurÃ¼ckverfolgt:
  - WPF-Usererfassung Ã¼ber `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad Ã¼ber `ArbeitsstundenViewModel`
  - MAUI-Usererfassung Ã¼ber `MyArbeitsstundenPage`
  - gemeinsamer Persistenzpfad Ã¼ber `SupabaseService.AddArbeitsstundeAsync(...)`
  - in allen drei Aufrufern wird fÃ¼r neue Arbeitsstunden keine fachliche `Id` gesetzt; die erzeugten `ArbeitsstundeRecord`-Instanzen tragen aber typbedingt lokal weiter den Defaultwert `0`
  - der entscheidende Unterschied zu den Ã¼brigen produktiven Create-Modellen lag im Modellattribut: `ArbeitsstundeRecord` verwendete noch `[PrimaryKey("id")]`, wÃ¤hrend die Ã¼brigen Insert-relevanten Tabellen bereits explizit `[PrimaryKey("id", false)]` verwenden
  - damit war die reale Ursache belastbar: im laufenden Paketstand konnte die PrimÃ¤rschlÃ¼sselspalte im Insertpfad weiterhin serialisiert werden und `Id = 0` in die Payload gelangen
- Korrekturpfad fÃ¼r den Insertfehler:
  - `ArbeitsstundeRecord` auf `[PrimaryKey("id", false)]` umgestellt
  - keine App-seitige `lastrow+1`-Logik
  - keine Ã„nderung am Updatepfad; bestehende DatensÃ¤tze verwenden ihre `Id` weiter normal
  - Ergebnis: bei Neuanlagen wird die `id` nicht mehr aus der App in die Create-Payload aufgenommen
- Zweiten Fehler in `Arbeitsstunden freigeben` ebenfalls bis zum realen UI-Pfad geprÃ¼ft:
  - `status` / Anmerkung war fachlich und technisch bereits optional
  - `HasChanges` in `PruefungseintragItem` wertet bereits korrekt `Freigeben || Status geÃ¤ndert`
  - die tatsÃ¤chliche StÃ¶rung lag nicht in einer Pflichtlogik fÃ¼r `status`, sondern an der WPF-Darstellung der Freigabe-Checkbox Ã¼ber `DataGridCheckBoxColumn`
  - im laufenden Gridpfad wurde die CheckboxÃ¤nderung nicht zuverlÃ¤ssig sofort in das Item zurÃ¼ckgeschrieben; dadurch zogen `PropertyChanged`, `HasPendingChanges` und `SpeichernCommand` beim bloÃŸen Setzen der Checkbox nicht unmittelbar an
- Korrekturpfad fÃ¼r die Freigabeansicht:
  - die Spalte `Freigeben` wurde auf `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding umgestellt
  - Binding jetzt mit `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`
  - dadurch gilt das Setzen der Checkbox sofort als relevante Ã„nderung und aktiviert den Speichernpfad ohne zusÃ¤tzliche Statuspflicht
- Fachregeln bewusst unverÃ¤ndert gelassen:
  - offen bleibt `freigegeben = false`
  - `status` bleibt reines optionales Anmerkungsfeld
  - Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
  - keine neue Freigabearchitektur
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im Shared-Pfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-23 â€“ Prompt 1/1: WPF-Bindingfehler in `ArbeitsstundenErfassungView` mit reinem Anzeige-Fix sauber abgeschlossen

- Den gemeldeten Fehler auf die reale Bindungsstelle in `KGV.Wpf/Views/ArbeitsstundenErfassungView.xaml` zurÃ¼ckgefÃ¼hrt: `CurrentMemberText` ist im `ArbeitsstundenErfassungViewModel` bewusst nur als schreibgeschÃ¼tzte Anzeigeproperty vorhanden.
- Damit blieb die fachliche Richtung klar: kein Setter im ViewModel, keine neue Fachlogik und keine Aufweichung des Anzeigevertrags nur zur Beruhigung des Bindings.
- Den Fix bewusst klein und XAML-seitig gehalten: die bisherige Anzeige Ã¼ber `Run Text="{Binding CurrentMemberText}"` wurde auf eine explizite Anzeige Ã¼ber einen eigenen `TextBlock` mit `Mode=OneWay` umgestellt.
- Die betroffene View wurde im Abschlusslauf nochmals gezielt gegen den Nachbarbereich geprÃ¼ft: der anschlieÃŸende `ValidationMessage`-`TextBlock` und die Ã¼brige XAML-Struktur bleiben intakt; es wurde nur der kleine Anzeigeabschnitt fÃ¼r das Mitglied angepasst.
- Der Block bleibt bewusst minimal:
  - keine Ã„nderung an `ArbeitsstundenErfassungViewModel`
  - keine Ã„nderung an Shared-/Servicepfaden
  - keine neue Fachlogik fÃ¼r Arbeitsstunden
  - kein Umbau auÃŸerhalb der betroffenen View
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Damit ist der kleine Bindingfix sauber abgeschlossen; MAUI wurde in diesem Block nicht angefasst.

## 2026-03-23 Ã¢â‚¬â€œ Prompt 1/1: produktive Insert-Pfade auf feste ID-Mitgabe geprÃƒÂ¼ft und als aktuell korrekt dokumentiert

- Den gewÃƒÂ¼nschten Block zuerst vollstÃƒÂ¤ndig als IstzustandsprÃƒÂ¼fung statt als Vorab-Fix angegangen: geprÃƒÂ¼ft wurden der aktuelle `SupabaseService`, alle relevanten produktiven Create-/Add-/Insert-Pfade, die beteiligten Record-/Request-Modelle sowie die WPF-/MAUI-Aufrufer fÃƒÂ¼r Neuanlagen.
- Den stÃƒÂ¤rksten Verdachtspfad `arbeitstunde` konkret bis zur tatsÃƒÂ¤chlichen Insert-Stelle zurÃƒÂ¼ckverfolgt:
  - WPF erzeugt neue Arbeitsstunden in `ArbeitsstundenViewModel`
  - MAUI erzeugt neue Arbeitsstunden in `MyArbeitsstundenPage`
  - beide erzeugen neue `ArbeitsstundeRecord`-Objekte **ohne** explizite `Id`
  - `SupabaseService.AddArbeitsstundeAsync(...)` baut daraus nochmals ein frisches Insert-Objekt und setzt ebenfalls **keine** `Id`
  - im aktuell produktiven Insert-Pfad konnte damit kein `id = 0` oder sonstige feste ID-Mitgabe aus der App nachgewiesen werden
- Danach die ÃƒÂ¼brigen produktiven Verwaltungs-Neuanlagen geprÃƒÂ¼ft:
  - `CreateTerminAsync(...)`
  - `CreateBekanntmachungAsync(...)`
  - `CreateArbeitseinsatzAsync(...)`
  - in den WPF-ViewModels laufen neue EditorzustÃƒÂ¤nde zwar als normale Record-Objekte, wobei `Id` im New-Mode lokal auf den Typdefault `0` fÃƒÂ¤llt
  - entscheidend ist aber der Servicepfad: die drei Create-Methoden erzeugen vor dem tatsÃƒÂ¤chlichen PostgREST-Insert jeweils ein separates Insert-Objekt und ÃƒÂ¼bernehmen die lokale `Id` gerade nicht in die Payload
  - Ergebnis: auch diese produktiven Neuanlagen senden aktuell keine feste ID aus der App
- Einen mÃƒÂ¶glichen Produktpfad `arbeitseinsatz_anmeldung` gesondert gesucht, weil er laut Zielarchitektur ebenfalls DB-gesteuert laufen mÃƒÂ¼sste. Im aktuellen Repo existiert dafÃƒÂ¼r aber weiterhin kein echter Schreibpfad; der vorhandene `Anmelden`-Button auf Home bleibt eine reine Hinweis-/Info-Aktion. Entsprechend war dort aktuell auch kein produktiver Insertpfad mit ID-Mitgabe vorhanden.
- Den Serializer-/Bibliotheksaspekt zusÃƒÂ¤tzlich gegen den real verwendeten Paketstand abgesichert: im eingebundenen `Supabase.Postgrest` besitzt `PrimaryKeyAttribute` den optionalen Parameter `shouldInsert`, dessen Defaultwert `false` ist. Der verbleibende Arbeitsstunden-Record mit `[PrimaryKey("id")]` sendet seine PrimÃƒÂ¤rschlÃƒÂ¼sselspalte damit im Insert-Kontext ebenfalls nicht automatisch mit. Das erklÃƒÂ¤rt, warum auch dieser Pfad trotz impliziter Schreibweise aktuell korrekt bleibt.
- Wichtiges Ergebnis des Blocks:
  - in den geprÃƒÂ¼ften produktiven Insert-Pfaden wurde aktuell **kein** `id = 0` aus der App nachgewiesen
  - es wurde auch keine feste `id` indirekt ÃƒÂ¼ber Mapper, Defaultkonstruktoren oder Insert-Helfer in die produktive Payload ÃƒÂ¼bernommen
  - Unterschiede zwischen WPF und MAUI bestehen fÃƒÂ¼r `arbeitsstunde` nicht; beide laufen ÃƒÂ¼ber denselben korrekten Shared-Servicepfad
- Deshalb bewusst **kein** Codeumbau auf Verdacht:
  - keine `lastrow+1`-Logik
  - keine unnÃƒÂ¶tigen zusÃƒÂ¤tzlichen Insert-Modelle nur zur kosmetischen Doppelabsicherung
  - keine DB-Migration ohne versionierte Repo-Basis
  - stattdessen saubere Dokumentation, dass das Zielmuster im aktuellen Produktstand bereits eingehalten wird: `Insert ohne feste ID`, `Update mit vorhandener ID`
- Repo-seitig zusÃƒÂ¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine SQL-/Migrationsdateien vor, ÃƒÂ¼ber die sich die Tabellen-Defaults oder Sequences versioniert nachprÃƒÂ¼fen lieÃƒÅ¸en. Eine DB-seitige Sequence-/Defaultkorrektur war fÃƒÂ¼r diesen Block aber auch nicht nÃƒÂ¶tig, weil auf App-Seite kein fehlerhafter Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wurde durch diesen Analyse-/Dokublock nicht beschÃƒÂ¤digt.

## 2026-03-23 Ã¢â‚¬â€œ Prompt 1/1: begonnenen Home-/Detail-Block fÃƒÂ¼r Arbeitseinsatz sauber abgeschlossen, ohne Fake-Anmeldepfad

- Den aktuellen Istzustand des begonnenen Blocks zuerst gegen den realen Arbeitsbaum geprÃƒÂ¼ft und nur konsolidiert: bereits vorhanden waren erweiterte Home-Items, ein optionaler `CanRegister`-/Detailkontext, vorbereitete Ãƒâ€žnderungen im `HomeViewModel` sowie ein halbfertig angefasster Mappingblock im `SupabaseService`; sichtbar offen waren vor allem die WPF-XAMLs `HomeView.xaml` und `HomeSectionDetailView.xaml`, auÃƒÅ¸erdem war der hintere Helper-Tail des `SupabaseService` beschÃƒÂ¤digt/abgeschnitten.
- Den gemeinsamen Home-/Detailpfad fÃƒÂ¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` zusammen geprÃƒÂ¼ft, damit die Erweiterung nicht nur einen einzelnen Kartentyp verbessert, sondern die generische Detailview insgesamt konsistent hÃƒÂ¤lt. Dazu wurden zusÃƒÂ¤tzliche Felder fÃƒÂ¼r Detailinfos an den gemeinsamen Home-Modellen ergÃƒÂ¤nzt und die Mappings so erweitert, dass `Ort`/`Thema` bei Terminen sowie `Betreff`/`Kurztext`/VerÃƒÂ¶ffentlichungsdaten bei Bekanntmachungen nicht mehr im Detailpfad verloren gehen.
- FÃƒÂ¼r `Arbeitseinsatz` auf Home den begonnenen Sichtpfad sauber fertiggestellt: die Karte zeigt jetzt explizit den Beginn, behÃƒÂ¤lt die vorhandenen Registrierungs-/KapazitÃƒÂ¤tshinweise und ÃƒÂ¶ffnet Details nicht mehr nur implizit ÃƒÂ¼ber die ganze Karte, sondern ÃƒÂ¼ber einen klaren `Details`-Button. ZusÃƒÂ¤tzlich wird Ã¢â‚¬â€œ nur bei tatsÃƒÂ¤chlich als anmeldbar markierten DatensÃƒÂ¤tzen Ã¢â‚¬â€œ ein eigener `Anmelden`-Button angezeigt.
- FÃƒÂ¼r die Detailansicht den vorhandenen generischen Pfad wiederverwendet statt Sondernavigation zu bauen: `HomeSectionDetailView` zeigt jetzt optional den `Anmelden`-Button sowie die in `AdditionalInfo` transportierten ÃƒÂ¼brigen Datensatzinformationen vor dem eigentlichen Beschreibungstext. Dadurch bleiben `Arbeitseinsatz`, `Termin` und `Bekanntmachung` im selben DetailgerÃƒÂ¼st, aber mit vollstÃƒÂ¤ndigerer Fachanzeige.
- Die Arbeitseinsatz-Detailinformationen bewusst klein und belastbar gehalten: Thema, Datum, Beginn, Ende, Treffpunkt, Anmelde-/KapazitÃƒÂ¤tsdaten und nur dann `Max. Teilnehmer`, wenn sich dieser Wert aus vorhandenen Viewdaten tatsÃƒÂ¤chlich ableiten lÃƒÂ¤sst. Wenn keine belastbare Maximalzahl vorliegt, wird dieses Feld nicht angezeigt.
- Den halbfertig beschÃƒÂ¤digten Tail des `SupabaseService` sauber rekonstruiert und konsolidiert, damit der angefangene Block wieder auf einem kompilierbaren Produktstand liegt. Dazu gehÃƒÂ¶ren die kleinen Home-Helfer (`AddDetailLine`, Zeit-/Datumsformatierung, Textnormalisierung, Dokumentpfad-Helfer, `FirstNonEmpty`, `CreateUnavailableException` usw.), die durch den abgebrochenen Zwischenstand teilweise nicht mehr vollstÃƒÂ¤ndig in der Datei standen.
- Den `Anmelden`-Usecase ausdrÃƒÂ¼cklich gegen das aktuelle Repo geprÃƒÂ¼ft: es gibt weiterhin keinen belastbar bestÃƒÂ¤tigten produktiven Schreibpfad fÃƒÂ¼r eine echte Arbeitseinsatz-Anmeldung. Deshalb wurde **keine** neue Schattenlogik eingefÃƒÂ¼hrt. Sowohl in der Home-Karte als auch in der Detailansicht ist `Anmelden` nur als ehrliche WPF-Info-Aktion verdrahtet, die klar darauf hinweist, dass der echte Schreibpfad noch nicht angebunden ist.
- MAUI wurde im Rahmen dieses Blocks mitgedacht, aber bewusst nicht kÃƒÂ¼nstlich erweitert: die gemeinsam genutzten Home-Modelle und Mappings bleiben kompatibel, ohne dass mobil bereits eine neue pseudo-produktive Arbeitseinsatz-Anmeldung aufgebaut wird.
- Technisch verifiziert: Workspace-Build erfolgreich. Der begonnene Home-/Detail-Block ist damit sauber abgeschlossen; offen bleibt nur die spÃƒÂ¤tere echte Anbindung eines bestÃƒÂ¤tigten Arbeitseinsatz-Anmelde-Schreibpfads.

## 2026-03-23 Ã¢â‚¬â€œ Prompt 1/1: Zeit-/Datumsbug der drei Verwaltungseditoren zentral behoben, Editorverhalten abgeschlossen und Navigation auf Home-only zurÃƒÂ¼ckgezogen

- Den begonnenen Bugfix-/UX-Block zuerst gegen den realen Arbeitsbaum konsolidiert: im halbfertigen Stand lagen bereits ein neuer zentraler Typ-/Mappingansatz, ZurÃƒÂ¼ck-/Dirty-Check-Grundlagen und die RÃƒÂ¼cknahme der Hauptnavigation vor, aber noch nicht sauber verifiziert, dokumentiert und bis zum Build-/Abschlusszustand durchgezogen.
- Die tatsÃƒÂ¤chliche Ursache des Verschiebungsfehlers wurde im gemeinsamen Typ-/Serialisierungs-/Mappingpfad verortet und nicht pro Formular erraten:
  - `termin.datum` ist fachlich eine PostgreSQL-`date`-Spalte
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis`, `termin.sichtbar_ab`, `sichtbar_bis` sowie `bekanntmachung.sichtbar_ab`, `sichtbar_bis` sind fachlich `timestamp without time zone`
  - im App-Pfad liefen diese Werte aber als normale `DateTime`-Werte ohne explizite JSON-Konverter fÃƒÂ¼r den tatsÃƒÂ¤chlichen DB-Typ
  - dadurch konnten beim PostgREST-/JSON-Transport implizite `DateTimeKind`-/UTC-/Local-Interpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim wiederholten Bearbeiten/Speichern erklÃƒÂ¤rt
- Die Korrektur deshalb zentral im Shared-Pfad umgesetzt und nicht als View-Hotfix:
  - neue JSON-Konverter `PostgresDateOnlyJsonConverter` und `NullablePostgresTimestampWithoutTimeZoneJsonConverter`
  - gezielte Annotation der betroffenen Record-Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÂ¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÂ¼r geladene VerwaltungsdatensÃƒÂ¤tze sowie fÃƒÂ¼r `Create*Async(...)` und `Update*Async(...)`
  - die letzte verbliebene abweichende Datumsnormalisierung im `CreateArbeitseinsatzAsync(...)`-Pfad wurde noch auf denselben date-only-Pfad vereinheitlicht
- Damit werden die fachlich kritischen Felder jetzt konsistent ohne Zeitzonenverschiebung behandelt:
  - `arbeitseinsatz.sichtbar_ab`
  - `arbeitseinsatz.sichtbar_bis`
  - `arbeitseinsatz.anmeldung_bis`
  - `termin.datum`
  - `termin.start_uhrzeit`
  - `termin.end_uhrzeit`
  - `termin.sichtbar_ab`
  - `termin.sichtbar_bis`
  - `bekanntmachung.sichtbar_ab`
  - `bekanntmachung.sichtbar_bis`
  Erneutes Speichern erzeugt damit keine weitere fachliche Verschiebung mehr.
- Das Editorverhalten der drei bestehenden WPF-Verwaltungseditoren produktiv abgeschlossen, ohne neue Views oder neue Dialogarchitektur zu bauen:
  - alle drei ViewModels erhalten den vorhandenen `MainWindowViewModel`-Kontext
  - nach erfolgreichem Speichern wird nicht mehr in der Bearbeiten-View verharrt, sondern ÃƒÂ¼ber den bestehenden Navigationspfad zurÃƒÂ¼ck auf `Home` gegangen
  - alle drei Editor-Views besitzen jetzt einen `ZurÃƒÂ¼ck`-Button
  - `ZurÃƒÂ¼ck` und `Abbrechen` verwenden denselben kleinen Dirty-Check ÃƒÂ¼ber einen initialen Snapshot des Editorzustands
  - RÃƒÂ¼ckfrage erscheint nur bei echten Ãƒâ€žnderungen; nach erfolgreichem Speichern ist der Zustand wieder sauber
- Die Navigation wurde auf den gewÃƒÂ¼nschten Produktpfad zurÃƒÂ¼ckgezogen:
  - `ArbeitseinsÃƒÂ¤tze bearbeiten`
  - `Termine bearbeiten`
  - `Bekanntmachungen bearbeiten`
  sind nicht mehr Teil der Hauptnavigation
  - erreichbar bleiben sie nur ÃƒÂ¼ber die vorhandenen Home-Buttons fÃƒÂ¼r Admin/Vorstand
  - damit bleibt kein Doppelpfad Home + Hauptnavigation stehen
- Kleine Abschlussbereinigung des halbfertigen Zustands zusÃƒÂ¤tzlich erledigt:
  - die drei Top-Leisten der Editor-Views wurden mit `ZurÃƒÂ¼ck` ergÃƒÂ¤nzt
  - der zwischenzeitliche Encodingrest beim Label `Ãƒâ€“ffnen` wurde bereinigt
- WPF wurde konkret angepasst; MAUI wurde nicht mit neuer EditoroberflÃƒÂ¤che erweitert. Da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, zieht MAUI fachlich denselben Fixpfad mit, ohne dass die mobile OberflÃƒÂ¤che beschÃƒÂ¤digt wird.
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich
  - verbleibende Warnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler
- Ergebnis: der begonnene Bugfix-/UX-Block fÃƒÂ¼r die drei bestehenden Verwaltungseditoren ist jetzt sauber abgeschlossen, dokumentiert, build-verifiziert und bereit fÃƒÂ¼r Commit/Push.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 2/2: Arbeitsstunden-Freigabe sauber abgeschlossen mit globalem Review-Lock, PrÃƒÂ¼ftabelle und wiederverwendetem Editor

- Den begonnenen Arbeitsstunden-Freigabe-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufgemacht: die vorbereiteten Ãƒâ€žnderungen fÃƒÂ¼r PrÃƒÂ¼ftabelle, globalen Lock auf `arbeitstunde`, Wiederverwendung des Erfassungseditors und die Umstellung auf `Arbeitsstunden freigeben` waren bereits im Repo angelegt, mussten aber noch sauber verifiziert, klein bereinigt und dokumentiert werden.
- Die realen Felder fÃƒÂ¼r den Produktpfad nochmals explizit gegen Modell und Service geprÃƒÂ¼ft und dann unverÃƒÂ¤ndert korrekt genutzt:
  - `freigegeben`
  - `status`
  - `genehmigt_am`
  - `genehmigt_von`
  - `lockedbyuserid`
  - `lockat`
- Den offenen PrÃƒÂ¼fzustand fachlich endgÃƒÂ¼ltig von `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÂ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das Anmerkungsfeld fÃƒÂ¼r Admin/Vorstand.
- Die WPF-Ansicht `Arbeitsstunden freigeben` dient jetzt wirklich primÃƒÂ¤r der PrÃƒÂ¼fung/Freigabe offener Arbeitsstunden und nicht mehr einer vorgelagerten Mitglieder-Sammelliste:
  - Darstellung als Tabelle
  - Sortierung nach `datum` aufsteigend *(ÃƒÂ¤lteste zuerst)*
  - pro Zeile sichtbar: Mitglied, Datum, Saison, Stunden, Art der Arbeit
  - zusÃƒÂ¤tzlich bearbeitbares `status`-Feld als Anmerkungsbereich
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Sitzungsspeichern produktiv umgesetzt: gespeichert werden ausdrÃƒÂ¼cklich nur markierte/geÃƒÂ¤nderte Zeilen; unbearbeitete offene DatensÃƒÂ¤tze bleiben offen. Damit ist die fachliche Vorgabe erfÃƒÂ¼llt, dass nicht jede Sitzung alle offenen FÃƒÂ¤lle abschlieÃƒÅ¸en muss.
- Die eigentliche Freigabelogik nutzt die realen Freigabefelder korrekt:
  - `freigegeben = true`
  - `genehmigt_am` wird beim Speichern gesetzt
  - `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÂ¼fenden Admins/Vorstands gesetzt
  - geÃƒÂ¤nderte, aber nicht freigegebene Zeilen bleiben mit `freigegeben = false` in der offenen Liste
- `Bearbeiten` verwendet jetzt bewusst denselben Arbeitsstunden-Erfassungseditor weiter, statt einen zweiten parallelen PrÃƒÂ¼feditor zu erfinden:
  - Wiederverwendung ÃƒÂ¼ber `ArbeitsstundenErfassungViewModel` / `ArbeitsstundenErfassungView`
  - im PrÃƒÂ¼f-/Adminkontext zusÃƒÂ¤tzlich sichtbares `status`-Feld
  - Ãƒâ€“ffnung in einem kleinen Host-Window, damit der bestehende Editorpfad auch modal im Reviewkontext nutzbar bleibt
  - nach dem Speichern wird die PrÃƒÂ¼ftabelle wieder frisch geladen
- Globaler Review-Lock klein und nachvollziehbar auf dem vorhandenen Tabellenmodell umgesetzt:
  - beim Ãƒâ€“ffnen der Freigabeansicht wird eine globale PrÃƒÂ¼fsperre fÃƒÂ¼r die offenen `arbeitstunde`-DatensÃƒÂ¤tze gesetzt
  - andere PrÃƒÂ¼fer kÃƒÂ¶nnen wÃƒÂ¤hrenddessen nicht parallel produktiv dieselbe Freigabesitzung bearbeiten
  - falls real auflÃƒÂ¶sbar, wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÂ¤ngert die Sperre wÃƒÂ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÂ¤ngende Locks laufen nach Timeout aus und kÃƒÂ¶nnen danach kontrolliert ÃƒÂ¼bernommen werden
- Die bestehende Badge-/ZÃƒÂ¤hlerlogik wurde bewusst weiterverwendet: Ãƒâ€žnderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, damit WPF-Navigation und vorhandene mobile Reviewindikatoren korrekt nachziehen.
- MAUI wurde in diesem Block nicht mit neuer Review-UI ausgebaut; konsistent mitgezogen wurde aber die zugrunde liegende Regel, dass offene Arbeitsstunden ÃƒÂ¼ber `freigegeben = false` laufen und `status` kein kÃƒÂ¼nstlicher Workflowwert mehr ist.
- Kleine technische Abschlussbereinigung des halbfertigen Zustands durchgefÃƒÂ¼hrt:
  - verbleibende Nullability-Warnung im WPF-Review-ViewModel bereinigt
  - Statusmeldung der PrÃƒÂ¼fansicht von der Locknachricht entkoppelt, damit RÃƒÂ¼ckmeldungen unabhÃƒÂ¤ngig sichtbar bleiben
  - die kurz sichtbare Design-Time-XAML-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der tatsÃƒÂ¤chliche WPF-Build lÃƒÂ¤uft erfolgreich durch
- Offene Restpunkte nach diesem Block bewusst klein gehalten:
  - die produktive WPF-Freigabe steht jetzt fachlich
  - ein spÃƒÂ¤terer separater Block kÃƒÂ¶nnte nur noch UX-Feinschliff oder weitergehende Reviewentscheidungen behandeln, ohne dass der Produktpfad aktuell unvollstÃƒÂ¤ndig wÃƒÂ¤re
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 1/2: Arbeitsstunden-Unterbau fÃƒÂ¼r einfachen Userflow wiederverwendet und Freigabe-Navigation vorbereitet

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÂ¼ft: produktiv vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, `AddArbeitsstundeAsync(...)`, `GetUnapprovedArbeitsstundenByMitgliedAsync()`, die bestehende Review-Ansicht fÃƒÂ¼r Admin/Vorstand sowie der WPF-/MAUI-Badgepfad ÃƒÂ¼ber `ArbeitsstundenChangedMessage`.
- Den vorhandenen Unterbau bewusst weiterverwendet statt neue Gesamtarchitektur zu bauen:
  - Speichern neuer User-EintrÃƒÂ¤ge lÃƒÂ¤uft weiter ÃƒÂ¼ber `AddArbeitsstundeAsync(...)`
  - offene FreigabefÃƒÂ¤lle werden weiter ÃƒÂ¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` ermittelt
  - Badge-/Sichtbarkeitsaktualisierung bleibt am vorhandenen `ArbeitsstundenChangedMessage`-Pfad angeschlossen
  - die bestehende Admin-/Vorstands-Reviewansicht wird nicht ersetzt, sondern nur sprachlich/navigativ konsolidiert
- FÃƒÂ¼r normale Nutzer jetzt einen eigenen klaren WPF-Erfassungsweg ergÃƒÂ¤nzt: `ArbeitsstundenErfassungViewModel` + `ArbeitsstundenErfassungView` bilden ein separates einfaches View nur fÃƒÂ¼r die Erfassung, ohne den bisherigen Review-/Bearbeitungspfad zu verdoppeln.
- Das neue Userformular zeigt bewusst nur die fachlich geforderten Felder:
  - `Datum` *(Pflichtfeld)*
  - `Stunden` *(Pflichtfeld)*
  - `Art der Arbeit` *(Pflichtfeld)*
- `freigegeben` ist in diesem Userflow kein sichtbares Feld; neue DatensÃƒÂ¤tze werden im Usermodus immer automatisch mit `freigegeben = false` und dem bestehenden Statuspfad `offen` gespeichert.
- Keine spekulativen Zusatzfelder eingefÃƒÂ¼hrt: insbesondere kein sichtbares Mitgliedsfeld, keine zusÃƒÂ¤tzliche Freigabesteuerung, keine neue Kommentierungs-/Ablehnungslogik und keine neue Saison-/Admin-Facharchitektur im Formular.
- Der neue Einstieg ist jetzt klar und eigenstÃƒÂ¤ndig statt als Inline-Teil auf Home:
  - neues WPF-Hauptnavigationselement `Arbeitsstunden erfassen` fÃƒÂ¼r Benutzer mit eigenem Mitgliedskontext
  - zusÃƒÂ¤tzlicher klarer Button `Arbeitsstunden erfassen` im Home-Bereich `Meine Arbeitsstunden`, der in dieselbe eigene View navigiert
- Die neue WPF-Erfassungsansicht bleibt einfach, aber produktiv:
  - aktueller eigener Mitgliedskontext wird ÃƒÂ¼ber den vorhandenen MainWindow-/UserContext-Pfad aufgelÃƒÂ¶st
  - aktuelle Saison wird ÃƒÂ¼ber den vorhandenen `GetSaisonRecordsAsync()`-Pfad ermittelt
  - `Abbrechen` setzt das Formular sauber zurÃƒÂ¼ck
  - `Speichern` steht am Ende des Formulars
- Validierung fÃƒÂ¼r den Userflow fachlich klein und produktnah umgesetzt:
  - Pflichtfelder werden markiert
  - fehlende Eingaben werden rot hervorgehoben
  - Fokus springt auf das erste fehlerhafte Feld von oben
  - `Stunden` mÃƒÂ¼ssen numerisch und grÃƒÂ¶ÃƒÅ¸er als `0` sein
  - keine kÃƒÂ¼nstlich schÃƒÂ¤rferen Zusatzregeln wurden eingefÃƒÂ¼hrt
- FÃƒÂ¼r Admin/Vorstand wurde in diesem Block bewusst noch keine neue Freigabe-OberflÃƒÂ¤che gebaut; stattdessen wurde der bestehende Pfad nur vorbereitet/konsolidiert:
  - WPF-Navigationseintrag wird jetzt als `Arbeitsstunden freigeben` gefÃƒÂ¼hrt
  - vorhandene WPF-Reviewtitel wurden auf `freigeben` umbenannt
  - bestehendes MAUI-AdminmenÃƒÂ¼ und die mobile Reviewtitel wurden ebenfalls auf `freigeben` konsolidiert
  - es bleibt bei derselben bestehenden Review-Mechanik, keine zweite parallele Adminansicht
- Der offene Freigabe-Indikator funktioniert weiter auf dem vorhandenen Produktpfad:
  - sichtbar nur fÃƒÂ¼r Admin/Vorstand bzw. Rollen mit `CanManageWorkHours`
  - sichtbar/hervorgehoben nur bei tatsÃƒÂ¤chlich offenen DatensÃƒÂ¤tzen mit `freigegeben = false`
  - Badge/ZÃƒÂ¤hler zeigt die Anzahl offener PrÃƒÂ¼ffÃƒÂ¤lle
  - nach neuer User-Erfassung aktualisiert sich dieser Pfad weiter ÃƒÂ¼ber `ArbeitsstundenChangedMessage`
- MAUI wurde nicht mit Platzhalterseiten aufgeblÃƒÂ¤ht: der Block beschrÃƒÂ¤nkt sich mobil auf die sprachliche Konsolidierung der vorhandenen Arbeitsstunden-/Freigabebegriffe; der gemeinsame Service- und Statusunterbau bleibt damit fÃƒÂ¼r spÃƒÂ¤tere ParitÃƒÂ¤t offen und unverletzt.
- FÃƒÂ¼r den nÃƒÂ¤chsten Freigabe-Block bleibt bewusst noch offen:
  - ob und wie die bestehende Freigabe-/ReviewoberflÃƒÂ¤che fachlich weiter vereinfacht oder umgestaltet wird
  - mÃƒÂ¶gliche weitergehende Admin-/Vorstandsentscheidungen jenseits der hier nur vorbereiteten Freigabenavigation
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 5/5: Verwaltungseditoren konsolidiert, alte Strukturreste entfernt und Abschlussstand fÃƒÂ¼r WPF/MAUI geschÃƒÂ¤rft

- Den aktuellen Abschlussstand der drei Verwaltungseditoren vor dem Konsolidierungsschritt nochmals gezielt geprÃƒÂ¼ft: produktiv verdrahtet waren bereits `ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView`; parallel lagen im Repo aber noch die ÃƒÂ¤lteren vorbereitenden Strukturviews ohne produktive Verdrahtung.
- Die produktive Navigation nochmals gegen `App.xaml`, `NavigationService`, `MainWindowViewModel` und `HomeViewModel` abgesichert:
  - `App.xaml` mappt die drei Verwaltungs-ViewModels ausschlieÃƒÅ¸lich auf die produktiven Editor-Views
  - `NavigationService` erzeugt weiterhin nur die drei produktiven Verwaltungs-ViewModels
  - `MainWindowViewModel` bietet die drei Verwaltungswege nur im Admin-/Vorstandskontext an
  - `HomeViewModel` ÃƒÂ¶ffnet aus Home heraus ebenfalls nur diese produktiven Verwaltungs-ViewModels
- Die alten strukturellen Verwaltungsviews wurden jetzt konsequent entfernt, weil sie nicht mehr produktiv genutzt wurden und nur noch doppeldeutige Koexistenz erzeugten:
  - `ArbeitseinsaetzeVerwaltungView.xaml` / `.xaml.cs`
  - `TermineVerwaltungView.xaml` / `.xaml.cs`
  - `BekanntmachungenVerwaltungView.xaml` / `.xaml.cs`
- Auch der frÃƒÂ¼here gemeinsame Strukturunterbau wurde bereinigt:
  - `HomeVerwaltungViewModelBase.cs`
  - `HomeVerwaltungListItem.cs`
  Diese Bausteine waren nach der Umstellung aller drei Bereiche auf echte Basistabellen-Editoren nicht mehr produktiv in Verwendung.
- Die Benennung/Architektur bleibt damit im Abschlussstand klar lesbar: die produktiven WPF-VerwaltungsoberflÃƒÂ¤chen tragen bewusst den Suffix `EditorView`, und es existiert daneben kein konkurrierender Alt-View-Pfad mehr.
- Einen grÃƒÂ¶ÃƒÅ¸eren Umbenennungsblock bewusst nicht erÃƒÂ¶ffnet: da die produktiven Editor-Views bereits eindeutig und direkt in `App.xaml` verdrahtet sind, hÃƒÂ¤tte ein zusÃƒÂ¤tzlicher Klassen-/Datei-Rename in diesem Abschlussblock mehr mechanische Bewegung als fachlichen Nutzen erzeugt.
- Gemeinsame Muster wurden nicht kÃƒÂ¼nstlich ÃƒÂ¼berabstrahiert: Validierungs-, Fokus- und Zeitlogik bleiben zwischen den drei Editor-ViewModels fachlich parallel und weiterhin gut nachvollziehbar. GrÃƒÂ¶ÃƒÅ¸ere neue Basisklassen/Abstraktionen wurden bewusst nicht mehr eingezogen, um den Abschlussblock nicht unnÃƒÂ¶tig zu verbreitern.
- Home-/Rechtepfad final abgesichert: Bearbeiten-Einstiege bleiben nur fÃƒÂ¼r Admin/Vorstand sichtbar und nutzbar; normale Nutzer bleiben sauber im lesenden Home-Modus. Es gibt im aktuellen Stand keine produktive Navigation mehr in veraltete Strukturviews und keine Bearbeitung direkt auf Home.
- MAUI-ParitÃƒÂ¤t fÃƒÂ¼r spÃƒÂ¤teren Anschluss fachlich abgesichert, ohne neue Platzhalterseiten zu bauen:
  - die produktiven Lese-/Create-/Update-Pfade fÃƒÂ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÂ¤ndig in den gemeinsamen Services/Modellen
  - damit ist die fachliche Grundlage fÃƒÂ¼r spÃƒÂ¤tere mobile VerwaltungsoberflÃƒÂ¤chen plattformneutral vorhanden
  - WPF-spezifisch bleiben aktuell nur die drei tatsÃƒÂ¤chlichen Editor-UIs inkl. Fokussteuerung/HTML-Vorschau
- Kleiner QualitÃƒÂ¤tsblock erfolgreich abgeschlossen: keine neue Fachlogik erÃƒÂ¶ffnet, keine Home-Bearbeitung ergÃƒÂ¤nzt, keine zusÃƒÂ¤tzliche Doppelverdrahtung stehen gelassen, und die aktive WPF-/MAUI-Basis bleibt buildfÃƒÂ¤hig.
- Reale Restoffenheiten nach dem Abschlussblock:
  - die drei produktiven WPF-Verwaltungseditoren stehen jetzt vollstÃƒÂ¤ndig
  - fÃƒÂ¼r MAUI existiert bewusst noch keine produktive VerwaltungsoberflÃƒÂ¤che fÃƒÂ¼r diese drei Bereiche, aber der gemeinsame Unterbau ist vorbereitet
  - verbleibend sind auÃƒÅ¸erhalb dieses Blocks nur noch getrennte, nicht blockrelevante Arbeitsbaum-Artefakte bzw. unabhÃƒÂ¤ngige UI-Themen wie die bereits offene `LoginWindow.xaml`
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung und Altlast-Bereinigung erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 4/5: ArbeitseinsÃƒÂ¤tze-Verwaltung produktiv an `arbeitseinsatz` angeschlossen, inklusive Sonderregeln fÃƒÂ¼r Teilnehmergrenze und Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÂ¤tze-Verwaltung vor dem Umbau erneut geprÃƒÂ¼ft: `ArbeitseinsaetzeVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃƒÂ¼st abgeleitet, die Liste kam nur aus dem Startseiten-Lesepfad, und rechts gab es noch keinen produktiven Editor mit bestÃƒÂ¤tigten Basistabellenfeldern.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestÃƒÂ¤tigten Fachfelder:
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
- Gemeinsamen produktiven Basistabellenpfad ergÃƒÂ¤nzt:
  - `GetArbeitseinsaetzeVerwaltungAsync()`
  - `CreateArbeitseinsatzAsync(...)`
  - `UpdateArbeitseinsatzAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `arbeitseinsatz`; es wird ausdrÃƒÂ¼cklich nicht gegen `v_startseite_arbeitseinsatz` geschrieben.
- Die linke Verwaltungsseite nutzt damit jetzt reale DatensÃƒÂ¤tze aus `arbeitseinsatz` statt einer bloÃƒÅ¸en Home-/View-Strukturableitung. Dadurch bleiben auch inaktive oder intern markierte DatensÃƒÂ¤tze im Admin-/Vorstandskontext sauber bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick ÃƒÂ¶ffnet den Editor mit echten Werten
  - `Neu` ÃƒÂ¶ffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollstÃƒÂ¤ndig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- Sonderregel `max_teilnehmer` fachlich sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - in der UI gibt es einen expliziten Zustand `ohne Begrenzung`
  - in diesem Zustand bleibt das eigentliche Zahlenfeld unsichtbar
  - gespeichert wird dann `NULL`, nie `0`
  - sobald eine Begrenzung aktiv ist, muss `max_teilnehmer > 0` gelten
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - leere Eingabe bleibt im Editor als optionaler Zustand zulÃƒÂ¤ssig
  - beim Speichern wird der DB-konforme operative Wert `0` verwendet, damit der NOT-NULL-/Default-Vertrag sauber eingehalten bleibt
  - eingegebene Werte mÃƒÂ¼ssen `>= 0` sein
- BestÃƒÂ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungÃƒÂ¼ltig
  - `Sichtbar bis < Sichtbar ab` ist ungÃƒÂ¼ltig
  - `max_teilnehmer <= 0` bei aktiver Begrenzung ist ungÃƒÂ¼ltig
  - `stunden_wert < 0` ist ungÃƒÂ¼ltig
  - `anmeldung_bis` wird als optionaler Timestamp mit vollstÃƒÂ¤ndigem Datum+Uhrzeit-Paar geprÃƒÂ¼ft, sobald das Feld genutzt wird
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe zentrale tolerante Zeitlogik fÃƒÂ¼r `Startuhrzeit`, `Enduhrzeit`, `Sichtbar ab`, `Sichtbar bis` und `Anmeldung bis`
- FÃƒÂ¼r `Stundenwert` wurde ergÃƒÂ¤nzend eine kleine tolerante numerische Parse-Logik eingebracht, damit Eingaben kulturrobust verarbeitet werden, ohne daraus ein neues grÃƒÂ¶ÃƒÅ¸eres Shared-Parsing-Subsystem zu machen.
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Ãƒâ€žnderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÂ¶ÃƒÅ¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Offene Punkte fÃƒÂ¼r den letzten ParitÃƒÂ¤ts-/AufrÃƒÂ¤umblock: die drei produktiven Verwaltungseditoren stehen jetzt, offen bleibt vor allem die kleine Konsolidierung/ParitÃƒÂ¤t der verbliebenen Altstrukturen und der anschlieÃƒÅ¸ende Abschluss-/AufrÃƒÂ¤umpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÂ¤tze-Block erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 3/5: Bekanntmachungen-Verwaltung produktiv an `bekanntmachung` angeschlossen, inklusive kleinem HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung vor dem Umbau erneut geprÃƒÂ¼ft: `BekanntmachungenVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃƒÂ¼st abgeleitet, die Listenladung lief nur ÃƒÂ¼ber den Startseiten-Lesepfad, und es gab noch keinen produktiven Editor fÃƒÂ¼r `inhalt_html`.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestÃƒÂ¤tigten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `inhalt_html` *(Pflichtfeld)*
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `sort_order`
  - `aktiv`
- Technische Felder wie `created_at` und `updated_at` bleiben weiterhin bewusst auÃƒÅ¸erhalb der normalen Bearbeitung; es wurden keine zusÃƒÂ¤tzlichen Fantasiefelder ergÃƒÂ¤nzt.
- Gemeinsamen produktiven Basistabellenpfad ergÃƒÂ¤nzt:
  - `GetBekanntmachungenVerwaltungAsync()`
  - `CreateBekanntmachungAsync(...)`
  - `UpdateBekanntmachungAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `bekanntmachung`; es wird ausdrÃƒÂ¼cklich nicht gegen `v_startseite_bekanntmachungen` geschrieben.
- Die linke Verwaltungsseite nutzt jetzt reale DatensÃƒÂ¤tze aus `bekanntmachung` statt einer Home-/View-Strukturableitung. Dadurch bleibt die Admin-/Vorstandsverwaltung auch fÃƒÂ¼r noch nicht sichtbare oder inaktive Bekanntmachungen fachlich korrekt.
- Das rechte Verhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick ÃƒÂ¶ffnet den Editor mit echten Werten
  - `Neu` ÃƒÂ¶ffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollstÃƒÂ¤ndig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- HTML-Editor-Entscheidung bewusst klein und kontrolliert gehalten: im Repo gab es vor dem Block keine belastbare kleine HTML-/Preview-Komponente und keine bereits genutzte leichte EditorabhÃƒÂ¤ngigkeit. Deshalb wurde keine schwere neue Editor-Architektur eingefÃƒÂ¼hrt, sondern eine produktive Bordmittel-LÃƒÂ¶sung umgesetzt:
  - HTML-Quellbearbeitung in einem dedizierten Editorbereich
  - kleine Snippet-Leiste fÃƒÂ¼r hÃƒÂ¤ufige HTML-Bausteine (`Absatz`, `ÃƒÅ“berschrift`, `Fett`, `Link`, `Liste`)
  - integrierte Live-Vorschau ÃƒÂ¼ber den vorhandenen WPF-`WebBrowser`
- Damit bleibt `inhalt_html` nicht auf ein bloÃƒÅ¸es Plaintext-Endfeld reduziert, ohne einen ÃƒÂ¼bergroÃƒÅ¸en Richtext-Baukasten neu zu erfinden.
- BestÃƒÂ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `inhalt_html` darf nicht leer oder nur Leerzeichen sein
  - `sichtbar_bis < sichtbar_ab` ist ungÃƒÂ¼ltig
  - fÃƒÂ¼r `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit gemeinsam benÃƒÂ¶tigt, sobald eines davon befÃƒÂ¼llt wird, damit keine stillen Timestamp-Annahmen entstehen
  - `sort_order` ist optional, muss aber bei Eingabe eine ganze Zahl sein
- Das Validierungs-/Fokusmuster aus `Termine` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe tolerante Zeitlogik fÃƒÂ¼r die Sichtbarkeits-Timestamps
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Ãƒâ€žnderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht mit HTML-Vorschau; eine Bereinigung kann spÃƒÂ¤ter separat folgen, ohne diesen Block aufzublÃƒÂ¤hen.
- Offene Punkte fÃƒÂ¼r das nÃƒÂ¤chste Modul: `ArbeitseinsÃƒÂ¤tze` sind weiterhin das letzte der drei Home-nahen Verwaltungsfelder ohne bestÃƒÂ¤tigten produktiven Editor; dort fehlen im aktiven Stand noch die sauber verifizierten Schreibfelder/-pfade analog zu `termin` und `bekanntmachung`.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 2/5: Termine-Verwaltung als erster echter Editor produktiv an `termin` angeschlossen

- Den aktuellen Istzustand der vorbereiteten Termine-Verwaltung vor dem Umbau erneut geprÃƒÂ¼ft: `TermineVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃƒÂ¼st abgeleitet, die Listenladung lief nicht ÃƒÂ¼ber die bestÃƒÂ¤tigte Basistabelle `termin`, und im rechten Bereich gab es noch keinen produktiven Editor mit bestÃƒÂ¤tigten Feldern.
- Den jetzt bestÃƒÂ¤tigten Tabellenvertrag von `termin` direkt in die produktive WPF-Bearbeitung ÃƒÂ¼berfÃƒÂ¼hrt. Bearbeitet werden genau die verifizierten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `beschreibung`
  - `datum` *(Pflichtfeld)*
  - `start_uhrzeit`
  - `end_uhrzeit`
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `aktiv`
- Technische Spalten wie `created_at` und `updated_at` bleiben bewusst auÃƒÅ¸erhalb der normalen EditoroberflÃƒÂ¤che; es wurden keine zusÃƒÂ¤tzlichen oder geratenen Felder ergÃƒÂ¤nzt.
- Gemeinsamen produktiven Servicepfad fÃƒÂ¼r die Basistabelle ergÃƒÂ¤nzt: `ISupabaseService`/`SupabaseService` laden die Verwaltungs-Liste jetzt direkt aus `termin` und schreiben neue/geÃƒÂ¤nderte DatensÃƒÂ¤tze per `CreateTerminAsync(...)` und `UpdateTerminAsync(...)` wieder gegen `termin` zurÃƒÂ¼ck. Es wird ausdrÃƒÂ¼cklich nicht gegen `v_startseite_termine` geschrieben.
- Die Terminliste links zeigt jetzt reale DatensÃƒÂ¤tze aus der Basistabelle statt der bisherigen reinen Strukturvorbereitung; dadurch sind auch nicht fÃƒÂ¼r Home gedachte VerwaltungszustÃƒÂ¤nde wie `aktiv = false` im Admin-/Vorstandskontext korrekt bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick auf einen vorhandenen Termin ÃƒÂ¶ffnet rechts den echten Editor mit den geladenen Werten
  - `Neu` ÃƒÂ¶ffnet einen leeren Editorzustand mit fachlich sinnvollem Default `aktiv = true`
  - `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÂ¤ndig
  - `Speichern` steht wie gefordert am Ende des Formulars
- BestÃƒÂ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungÃƒÂ¼ltig
  - `Sichtbar bis < Sichtbar ab` ist ungÃƒÂ¼ltig
  - fÃƒÂ¼r `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit jeweils gemeinsam benÃƒÂ¶tigt, sobald das Feld befÃƒÂ¼llt wird, damit kein stiller Timestamp-Anteil geraten wird
- Wiederverwendbares Eingabe-/Validierungsmuster fÃƒÂ¼r Folgeeditoren vorbereitet:
  - Pflicht- und Fehlerfelder werden rot markiert
  - beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben
  - tolerante Zeiteingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert
  - offensichtlich ungÃƒÂ¼ltige Zeiten bleiben sichtbar und werden nicht still geleert oder heimlich korrigiert
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut im Editor geÃƒÂ¶ffnet, damit Ãƒâ€žnderungen direkt nachvollziehbar bleiben.
- MAUI in diesem Block bewusst nicht mit Platzhalterseiten erweitert; durch den gemeinsamen produktiven Servicepfad fÃƒÂ¼r `termin` ist die spÃƒÂ¤tere mobile ParitÃƒÂ¤t aber nicht verbaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Termin-Block erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Prompt 1/5: Home nur mit Admin-Bearbeiten-Einstiegen, separate Verwaltungsviews ohne Platzhalter vorbereitet

- Aktuellen WPF-/MAUI-Istzustand vor dem Umbau erneut geprÃƒÂ¼ft: `HomeView`/`HomeViewModel` zeigten bereits nur noch Listen plus separate Detailviews, `NavigationService` und `MainWindowViewModel` waren ViewModel-first organisiert, und die bestÃƒÂ¤tigten Lesepfade fÃƒÂ¼r Home liefen ÃƒÂ¼ber `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen`.
- Im aktiven Repo zugleich keine belastbar verifizierten Schreibmodelle oder bestehenden Create/Update-Servicepfade fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen gefunden; genau deshalb wurde in diesem Block bewusst keine neue Formularlogik geraten.
- Home fachlich weiter bereinigt: auf der Startseite gibt es fÃƒÂ¼r normale Nutzer weiterhin nur die bisherigen ÃƒÅ“bersichtslisten; fÃƒÂ¼r Admin/Vorstand kommt jetzt zusÃƒÂ¤tzlich eine kleine Verwaltungssektion mit drei Bearbeiten-Einstiegen hinzu, aber keine Bearbeitung direkt auf Home.
- Drei echte separate WPF-Verwaltungsviews angelegt und in die bestehende Navigation eingebunden:
  - `ArbeitseinsaetzeVerwaltungView`
  - `TermineVerwaltungView`
  - `BekanntmachungenVerwaltungView`
- Jede Verwaltungsansicht nutzt bereits das Ziel-Layout mit linker Liste und rechter EditorflÃƒÂ¤che. Wenn weder ein vorhandener Datensatz geÃƒÂ¶ffnet noch `Neu` ausgelÃƒÂ¶st wurde, bleibt rechts bewusst ohne Formularfelder. Ein Doppelklick auf einen Listeneintrag ÃƒÂ¶ffnet rechts den Editierzustand mit den vorhandenen belastbaren Lesedaten; `Neu` ÃƒÂ¶ffnet denselben strukturellen Zustand leer.
- Keine Platzhalterformulare aufgebaut: weil die echten Schreibfelder/Basistabellen im aktiven Repo noch nicht sicher genug ableitbar waren, zeigt der rechte Bereich aktuell nur den geÃƒÂ¶ffneten Bearbeitungszustand plus die verifizierten Lese-/Schreibpfad-Hinweise statt geratener Eingabefelder.
- Reale Datenlisten statt Platzhalter verdrahtet: `ISupabaseService`/`SupabaseService` stellen die drei bestÃƒÂ¤tigten Startseiten-Lesepfade jetzt auch direkt fÃƒÂ¼r Verwaltungslisten bereit, sodass die neuen Views auf belastbaren Daten basieren und nicht auf Home-spezifischen Zwischenobjekten.
- Rechte entlang des vorhandenen Pfads sauber eingehÃƒÂ¤ngt: die Verwaltungsviews erscheinen in WPF nur fÃƒÂ¼r Admin/Vorstand sowohl ÃƒÂ¼ber Home als auch in der Hauptnavigation; MAUI bleibt in diesem Block unverÃƒÂ¤ndert und wird nicht mit neuen Platzhalterseiten aufgeblÃƒÂ¤ht, profitiert aber spÃƒÂ¤ter vom gemeinsamen Lesepfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block erfolgreich.
