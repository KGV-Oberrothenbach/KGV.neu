# KGV Entwicklungslog

---

## 2026-03-24 – WPF-Bindungsfehler in `FotoUploadTestView` bei schreibgeschützter Rohantwort behoben

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprüft; blockfremde offene Dateien bewusst nicht angefasst.
- Ursache geprüft: `RawResponseBody` ist im WPF-ViewModel ein reines Anzeige-/Diagnosefeld ohne Setter, wurde in `FotoUploadTestView.xaml` aber an `TextBox.Text` ohne expliziten Modus gebunden.
- Da `TextBox.Text` in WPF standardmäßig `TwoWay` bindet, entstand beim Öffnen/Benutzen der Diagnosefläche die bekannte Exception zur schreibgeschützten Eigenschaft.
- Fix bewusst klein und nur XAML-seitig umgesetzt:
  - `RawResponseBody` in `KGV.Wpf/Views/FotoUploadTestView.xaml` explizit auf `Mode=OneWay` gesetzt
  - `IsReadOnly="True"` blieb erhalten
- Kein künstlicher Setter im ViewModel, kein Umbau der übrigen Upload-Teststruktur, keine MAUI-Änderung.
- Technisch verifiziert:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich


## 2026-03-24 â€“ Gemeinsamen RFID-Scan-Kontext fÃ¼r `Ablesung erfassen` und `ZÃ¤hlerwechsel` produktiv umgesetzt

- Vor dem Umbau erneut den realen Repo-/Arbeitsbaumstand geprÃ¼ft und blockfremde offene Dateien bewusst unberÃ¼hrt gelassen.
- Den gemeinsamen produktiven RFID-Lesepfad im Shared-Service ergÃ¤nzt:
  - neues Enum `RfidScanContextState`
  - neues Ergebnis-DTO `RfidScanContextResult`
  - neue Service-Methode `ResolveRfidScanContextAsync(...)`
  - UID-Normalisierung bleibt zentral im `SupabaseService`
  - produktiver Lesepfad ist `v_rfid_scan_context`
- `RfidScanContextRecord` am echten View-Vertrag belassen und nur um Anzeige-/Statushilfen ergÃ¤nzt.
- Fachliche Zustandsableitung jetzt zentral und fÃ¼r beide Clients gleich:
  - `Unknown`
  - `KnownWithActiveMeter`
  - `KnownWithoutActiveMeter`
- WPF:
  - vorhandenen Placeholder `RfidScanContextViewModel` / `RfidScanContextView` in echte gemeinsame RFID-Kontextanzeige umgebaut
  - `AblesungErfassenViewModel` und `ZaehlerwechselScanViewModel` auf produktive UID-Eingabe + KontextauflÃ¶sung umgestellt
  - Ergebnisanzeige mit Anlage, Garten, Medium, RFID, aktivem ZÃ¤hler, ZÃ¤hlernummer, Status, Eichdaten
  - workflow-spezifische Einordnung fÃ¼r Ablesung bzw. ZÃ¤hlerwechsel ergÃ¤nzt
- MAUI:
  - gemeinsames `RfidScanContextViewModel` und gemeinsame Basispage `RfidScanWorkflowPage` ergÃ¤nzt
  - `AblesungErfassenPage` und `ZaehlerwechselPage` auf denselben produktiven Kontextpfad umgestellt
  - manuelle UID-Eingabe, KontextauflÃ¶sung und workflow-spezifische Einordnung mobil nutzbar
- Rechte:
  - Bereich bleibt auf Admin/Vorstand begrenzt
  - keine QR- oder Schattenlogik eingefÃ¼hrt
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 â€“ `FÃ¤llige ZÃ¤hler` produktiv auf Basis von `v_zaehler_eichstatus` umgesetzt

- Vor Start den realen Repo-/Arbeitsbaumzustand erneut geprÃ¼ft; keine Recovery-Reste als fachliche Grundlage verwendet.
- `FÃ¤llige ZÃ¤hler` in WPF und MAUI war vorher nur als Placeholder vorhanden.
- Gemeinsamen Produktivpfad im Shared-Service ergÃ¤nzt:
  - neues Model `ZaehlerEichstatusRecord` fÃ¼r `v_zaehler_eichstatus`
  - neue Service-Methode `GetZaehlerEichstatusAsync()`
  - Sortierung zentral im Service: zuerst kritisch/fÃ¤llig, dann nach Tagen, dann Garten/Anlage
- WPF:
  - `FaelligeZaehlerViewModel` von Placeholder auf echte Ãœbersicht umgestellt
  - Textfilter, Statusfilter und `Aktualisieren`
  - Tabelle mit Anlage, Garten, Medium, ZÃ¤hler, Eichdatum, EichfÃ¤lligkeit, Status und Tage
  - saubere Leer-/FehlerzustÃ¤nde
- MAUI:
  - `FaelligeZaehlerPage` auf produktive mobile Ãœbersicht umgestellt
  - gleiches Datenmodell und gleicher Shared-Servicepfad
  - Textfilter, Statusfilter, `Aktualisieren`
  - mobile Kartenansicht statt breiter Tabelle
- Rechte explizit auf Admin/Vorstand gehalten.
- Technisch verifiziert:
  - keine passenden TestfÃ¤lle in `KGV.Tests` fÃ¼r diesen Block gefunden
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 â€“ `RFID einrichten` produktiv auf neuem DB-Vertrag umgesetzt

- Den aktuellen Repo- und Git-Stand vor dem Umbau erneut geprÃ¼ft.
- Blockfremde offene Dateien im Arbeitsbaum bewusst nicht angerÃ¼hrt; insbesondere offene WPF-UI-Dateien, lokale Artefakte, `_Archiv/_Recovery` und `_AI_DB_EXPORT/AI_DATABASE_CONTEXT.sql` blieben auÃŸen vor.
- FÃ¼r die RFID-Zuordnung wurde der gemeinsame Produktivpfad im Shared-Service ergÃ¤nzt:
  - PrÃ¼fung und Speichern laufen jetzt zentral Ã¼ber `ISupabaseService` / `SupabaseService`
  - produktiver Schreibpfad ist `assign_parzelle_rfid(...)`
  - KonfliktprÃ¼fung nutzt den realen DB-Bestand Ã¼ber `parzelle` plus `v_rfid_scan_context`
  - UID wird konsistent getrimmt und in GroÃŸbuchstaben normalisiert
- `ParzelleRecord` wurde nur minimal auf den neuen DB-Vertrag erweitert:
  - `Anlage`
  - `hat_strom`
  - `hat_wasser`
  - `rfid_strom`
  - `rfid_wasser`
  - bestehende Altpfade auf getrennten Split-Tabellen wurden in diesem Block nicht zur neuen Grundlage gemacht
- WPF:
  - bestehende Seite `RFID einrichten` von Placeholder auf echte Maske umgestellt
  - Parzellenauswahl, aktuelle Strom-/Wasser-RFID, Medium, UID, `PrÃ¼fen`, `Speichern`
  - Ãœberschreiben vorhandener RFID an derselben Parzelle nur nach expliziter BestÃ¤tigung
  - Konflikt bei bereits anderweitig vergebener UID wird klar blockiert
- MAUI:
  - mobile `RfidEinrichtenPage` auf denselben Shared-Servicepfad umgestellt
  - gleiche Fachschritte wie in WPF
  - Ãœberschreib-BestÃ¤tigung mobil per Dialog
- Rechte:
  - Bereich bleibt explizit auf Admin/Vorstand begrenzt
  - keine Ã–ffnung Ã¼ber das zu breite Flag `CanManageReadings`
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 â€“ `Ablesen` als eigener Navigationsbereich fÃ¼r Admin/Vorstand in WPF und MAUI angelegt

- Den realen Istzustand vor dem Block geprÃ¼ft:
  - WPF hatte bereits vorbereitete ViewModels fÃ¼r `RfidEinrichten`, `FÃ¤llige ZÃ¤hler` und `ZÃ¤hlerwechsel`, aber noch keinen zentralen Einstiegsbereich `Ablesen`
  - MAUI hatte noch keinen eigenen Ablese-MenÃ¼punkt und keine Ãœbersichtsseite fÃ¼r diese vier Funktionen
  - die bestehende Rollen-/Rechtebasis war bereits vorhanden (`UserContext.Role` in WPF, `IsAdmin` / `IsVorstand` im MAUI-Authpfad)
- Den neuen globalen MenÃ¼punkt `Ablesen` deshalb nur fÃ¼r Admin/Vorstand in beiden UIs ergÃ¤nzt.
- WPF:
  - neue Ãœbersichtsseite `Ablesen` mit vier groÃŸen Kacheln angelegt
  - Kacheln navigieren zu `Ablesung erfassen`, `ZÃ¤hlerwechsel`, `RFID einrichten`, `FÃ¤llige ZÃ¤hler`
  - vorhandene vorbereitete WPF-ViewModels fÃ¼r `ZÃ¤hlerwechsel`, `RFID einrichten` und `FÃ¤llige ZÃ¤hler` weiterverwendet
  - fÃ¼r `Ablesung erfassen` eine schlanke Platzhalterseite ergÃ¤nzt
- MAUI:
  - neuer Flyout-Eintrag `Ablesen` im Admin-/Vorstand-Shell-MenÃ¼
  - neue mobile Ãœbersichtsseite mit vier gut tappbaren Kacheln angelegt
  - vier schlanke Zielseiten/Platzhalter fÃ¼r den weiteren Ablese-Ausbau registriert
- Der Block bleibt bewusst auf Navigation, Rechte und Ãœbersichtsseite beschrÃ¤nkt; tiefe Fachflows folgen erst in den nÃ¤chsten BlÃ¶cken.

## 2026-03-24 â€“ Veralteten Remote-Snapshot um fachliche QR-Reste bereinigt

- Den aktuellen Repo-Stand zuerst geprÃ¼ft: fachliche QR-Reste existierten nach der Live-Bereinigung nur noch im veralteten Snapshot `supabase/migrations/20260323093513_remote_schema.sql`.
- Gegen den bereits bereinigten Istzustand aus `_AI_DB_EXPORT/database.types.ts` abgeglichen: `public.parzelle` enthÃ¤lt dort keine QR-Spalten mehr, sondern nur noch die RFID-Felder.
- Den veralteten Remote-Snapshot deshalb auf den aktuellen fachlichen Stand gezogen:
  - `qr_code_wasser` aus der `parzelle`-Tabellendefinition entfernt
  - `qr_code_strom` aus der `parzelle`-Tabellendefinition entfernt
  - die veralteten Unique-Constraints `parzelle_qr_code_wasser_key` und `parzelle_qr_code_strom_key` entfernt
- Ergebnis: im Repo verbleiben in diesem Snapshot keine fachlichen QR-Reste mehr; der Snapshot passt damit wieder zum bereinigten Live-/Types-Stand.

## 2026-03-24 â€“ WPF Button-HÃ¶hen projektweit auf kleine feste FÃ¤lle geprÃ¼ft und angehoben

- Nach dem Fix in der Parzellenzuordnung wurden die WPF-Buttons mit kleinen festen HÃ¶hen projektweit geprÃ¼ft.
- Alle expliziten WPF-ButtonhÃ¶hen unter `35` wurden auf `35` angehoben, damit Beschriftungen nicht mehr zu knapp sitzen.
- Konkret angepasst wurden die betroffenen Buttons in:
  - `GartenStromView`
  - `GartenWasserView`
  - `ArbeitsstundenView`
  - `ChangeEmailWindow`
  - `ResetPasswordWindow`
- Der Fix bleibt rein visuell; Fachlogik und Command-Pfade wurden nicht verÃ¤ndert.

## 2026-03-24 â€“ WPF `MemberDetailView`: Parzellenzuordnungs-Zeile leicht erhÃ¶ht

- Den aktuellen Arbeitsbaum vor dem UI-Fix geprÃ¼ft; blockfremde lokale Ã„nderungen wie `KGV.Wpf/Views/LoginWindow.xaml` und `KGV.Wpf/Views/MemberDetailView.xaml.cs` bleiben weiter unberÃ¼hrt.
- Die zu kleine HÃ¶he lag nicht in der `MemberDetailView` selbst, sondern in der ausgelagerten `MemberParzellenSection` bei der Zeile fÃ¼r freie Parzelle, Startdatum und die beiden Aktionsbuttons.
- FÃ¼r den kleinen WPF-UI-Fix wurde nur diese Zeile leicht erhÃ¶ht:
  - Zeilen-`StackPanel` mit `MinHeight="36"`
  - Button-HÃ¶hen von `30` auf `35` erhÃ¶ht
- Damit passt die Beschriftung wieder sauber in die Buttons, ohne die restliche Detailansicht oder Fachlogik zu verÃ¤ndern.

## 2026-03-24 â€“ Appuser-Verwaltung ins `Admin-MenÃ¼` verschoben und an das ausgewÃ¤hlte Mitglied gebunden

- Den Istzustand zuerst im Repo geprÃ¼ft: `Benutzerverwaltung` hing in WPF noch als globale Navigation, obwohl die fachlichen Appuser-Aktionen immer auf ein konkretes Mitglied zielen sollen. Gleichzeitig war die produktive Nutzeraktion `InviteUserAsync(...)` bereits vorhanden, ein sauberer Entfernen-Pfad aber nicht als eigene UI-gebundene Aktion organisiert.
- Die MenÃ¼struktur wurde deshalb neu geordnet:
  - globaler Top-Level-Eintrag `Benutzerverwaltung` in WPF entfernt
  - stattdessen `Benutzerverwaltung` als eingerÃ¼ckter Unterpunkt im Mitgliedskontext unter `Admin-MenÃ¼`
  - damit bleibt die Navigation eindeutig und es gibt keine Doppelnavigation alt/neu
- Die WPF-`Benutzerverwaltung` arbeitet jetzt strikt auf dem zuvor ausgewÃ¤hlten Mitglied:
  - der Unterpunkt bekommt den aktuellen `SelectedMember` als Parameter
  - beim Laden wird nur noch die Appuser-Zuordnung dieses Mitglieds geladen
  - falls noch kein Appuser existiert, wird ein Platzhalter aus dem ausgewÃ¤hlten Mitglied aufgebaut, damit `Nutzer hinzufÃ¼gen` trotzdem sauber auf genau diesen Datensatz arbeitet
- Die Aktionen wurden fachlich passend umgestellt:
  - `Nutzer hinzufÃ¼gen` nutzt weiter den bestehenden produktiven Pfad `InviteUserAsync(...)`
  - `Nutzer entfernen` entfernt die Appuser-Zuordnung des ausgewÃ¤hlten Mitglieds, nicht das Mitglied selbst
  - ohne Mitgliedskontext laufen die Aktionen nicht und liefern eine klare fachliche RÃ¼ckmeldung statt technischer Fehler
- Rechte wurden sauber getrennt:
  - `Benutzerverwaltung` ist in WPF nur noch im Admin-Mitgliedskontext sichtbar
  - in MAUI wurde die MenÃ¼-Sichtbarkeit ebenfalls auf echte Admins beschrÃ¤nkt; Vorstand sieht den Punkt dort ebenfalls nicht
- Technisch verifiziert: `KGV.Wpf` baut nach der Umordnung erfolgreich; der MAUI-Pfad wurde im MenÃ¼-/Rechteverhalten mitgezogen und nicht beschÃ¤digt.

## 2026-03-24 â€“ Mitgliedersuche um E-Mail, Gartennummern und Hauptmitglied-Markierung erweitert

- Den Istzustand zuerst im Repo geprÃ¼ft: die bestehende Mitgliedersuche zeigte bisher in WPF nur Name/Vorname und in MAUI nur Titel/Unterzeile. Ein neuer Suchdialog oder neue Fachlogik waren dafÃ¼r nicht nÃ¶tig.
- Die Erweiterung bleibt im bestehenden Suchpfad und nutzt nur bereits vorhandene Datenquellen:
  - Mitgliedsdaten aus `GetMitgliederAsync()`
  - Parzellen aus `GetAllParzellenAsync()`
  - aktuelle Belegungen aus `GetAllParzellenBelegungenAsync()`
- Daraus wird jetzt fÃ¼r die Ergebnisliste angereichert:
  - E-Mailadresse
  - Gartennummern aus aktuell aktiven Belegungen, falls vorhanden
  - Hauptmitglied-Markierung aus `hauptmitglied_id`
- WPF wurde direkt in der bestehenden GridView erweitert um die zusÃ¤tzlichen Spalten `E-Mail`, `Gartennummern` und `Hauptmitglied` als deaktivierte Checkbox.
- MAUI wurde passend mitgedacht und die bestehende Suchliste ebenfalls um Gartennummern und Hauptmitglied-Markierung ergÃ¤nzt; die E-Mail bleibt dort Teil der sichtbaren Ergebnisinfos. Es wurde kein zweiter Suchpfad aufgebaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Erweiterung erfolgreich.

## 2026-03-24 â€“ Arbeitseinsatz-Teilnehmerliste: `Abmelden` pro Zeile produktiv Ã¼ber echten DB-Pfad ergÃ¤nzt

- Den Istzustand zuerst gegen Repo und `_AI_DB_EXPORT` geprÃ¼ft. `database.types.ts` bestÃ¤tigt fÃ¼r diesen Block ausdrÃ¼cklich neben `sign_up_for_arbeitseinsatz(...)` auch `sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` als echten DB-Funktionspfad; `roles.sql` enthÃ¤lt dafÃ¼r keine abweichende App-Sonderregel, `AI_DATABASE_CONTEXT.sql` liefert keinen alternativen Pfad.
- `sign_off_from_arbeitseinsatz(...)` war App-seitig vor diesem Block noch nicht produktiv verwendet. Der neue Abmeldepfad wurde deshalb sauber in den Shared-Service ergÃ¤nzt statt als direkter Tabellenhack daneben aufgebaut.
- In der Arbeitseinsatz-Detailview fÃ¼r Admin/Vorstand wurde pro Teilnehmerzeile der Button `Abmelden` ergÃ¤nzt. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch Zeilenaktion.
- Der Button lÃ¤uft mit kleiner RÃ¼ckfrage und ruft danach produktiv `SignOffFromArbeitseinsatzAsync(...)` auf; dieser Shared-Service nutzt den echten RPC-/DB-Funktionspfad `sign_off_from_arbeitseinsatz(...)`.
- Nach erfolgreicher oder fachlich abgefangener RÃ¼ckmeldung werden Teilnehmerliste und Detailzustand direkt neu geladen; damit aktualisieren sich auch KapazitÃ¤ts-/Anmeldeinformationen Ã¼ber den bestehenden Detailpfad mit.
- Technisch verifiziert: `KGV.Wpf` baut nach dem neuen Abmeldepfad erfolgreich; MAUI wurde im Shared-Servicepfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-24 â€“ Arbeitseinsatz-Block final verifiziert und sauber abgeschlossen

- Den aktuellen Arbeitsbaum erneut geprÃ¼ft: blockeigene CodeÃ¤nderungen aus diesem Arbeitseinsatz-Block waren bereits im Repo enthalten; lokal offen blieben nur blockfremde Ã„nderungen und Artefakte wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Hilfs-/Exportdateien.
- Den bereits implementierten Zustand nochmals final verifiziert:
  - `Anmelden` bleibt in Home und Detail am selben echten Pfad `SignUpForArbeitseinsatzAsync(...)`
  - der Shared-Service nutzt weiter produktiv `sign_up_for_arbeitseinsatz(...)`
  - Teilnehmerliste erscheint nur fÃ¼r Admin/Vorstand in der Detailview
  - `HinzufÃ¼gen` erscheint nur fÃ¼r Admin/Vorstand
  - `HinzufÃ¼gen` verwendet die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃ¤hlte bestehende Mitglied wird Ã¼ber denselben RPC-Pfad eingetragen wie bei Selbstanmeldung
- Die Sichtbarkeit von `Anmelden` wird weiterhin belastbar aus realem Zustand bestimmt: Basisdaten aus `arbeitseinsatz` plus aktive `arbeitseinsatz_anmeldung` gegen Frist, Platzgrenze und bereits vorhandene Anmeldung des aktuellen Mitglieds.
- `KGV.Wpf` wurde final erfolgreich gebaut. Ein erster Buildlauf scheiterte nur an einer laufenden gesperrten `KGV.Wpf`-Instanz; nach Beenden der App lief der Build sauber durch. Der Block ist damit technisch und fachlich abgeschlossen.

## 2026-03-24 â€“ Arbeitseinsatz: `Anmelden`-Regression und Admin-/Vorstand-Teilnehmerblock sauber abgeschlossen

- Den begonnenen Block erneut gegen den realen Arbeitsbaum und ausdrÃ¼cklich gegen `_AI_DB_EXPORT` geprÃ¼ft. Belastbar herangezogen wurden wieder `database.types.ts` mit `sign_up_for_arbeitseinsatz(...)` und `arbeitseinsatz_anmeldung`; `roles.sql` enthÃ¤lt dafÃ¼r keine abweichende Zusatzregel, `AI_DATABASE_CONTEXT.sql` liefert hier keinen zusÃ¤tzlichen App-Pfad.
- Die Regression beim verschwundenen `Anmelden` lag nicht am RPC selbst, sondern an der Sichtbarkeit: `CanRegister` wurde im sichtbaren UI zu eng nur aus dem Startseitenpfad Ã¼bernommen. Der Shared-Service bestimmt die sichtbare Anmeldung jetzt wieder aus den realen Basisdaten von `arbeitseinsatz` plus aktiven `arbeitseinsatz_anmeldung`-DatensÃ¤tzen fÃ¼r den aktuellen Benutzerkontext, inklusive Frist, Platzgrenze und bestehender Anmeldung.
- Home und Detail bleiben auf demselben echten Produktpfad: beide rufen weiterhin `SignUpForArbeitseinsatzAsync(...)` auf, das produktiv Ã¼ber `sign_up_for_arbeitseinsatz(...)` schreibt. Es wurde keine zweite Schreiblogik erÃ¶ffnet.
- FÃ¼r Admin/Vorstand wurde die Detailview klein und rollenabhÃ¤ngig ergÃ¤nzt:
  - reale Teilnehmerliste Ã¼ber aktive `arbeitseinsatz_anmeldung`
  - `HinzufÃ¼gen`-Button nur fÃ¼r Admin/Vorstand
  - Auswahl ausschlieÃŸlich Ã¼ber die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃ¤hlte bestehende Mitglied wird danach Ã¼ber denselben RPC-Pfad angemeldet wie bei Selbstanmeldung
- Dabei gibt es bewusst keine freie Texteingabe und keine PrÃ¼fung, ob das gewÃ¤hlte Mitglied App-User ist. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch `HinzufÃ¼gen`.
- Technisch finalisiert: kleine Warnungsbereinigung im Auswahlpfad abgeschlossen und `KGV.Wpf` baut erfolgreich; der Block ist damit jetzt sauber abschlieÃŸbar.

## 2026-03-24 â€“ `Anmelden` fÃ¼r Arbeitseinsatz im sichtbaren UI tatsÃ¤chlich funktionsfÃ¤hig gemacht

- Den realen Fehler ausdrÃ¼cklich am laufenden WPF-Pfad geprÃ¼ft: Home-Button und Detail-Button waren bereits an denselben Shared-Servicepfad gebunden, der sichtbare HÃ¤nger lag aber davor im Ã¼bergebenen Datensatz.
- Die konkrete Ursache: `MapHomeWorkAssignment(...)` Ã¼bernahm die `id` des Arbeitseinsatzes nicht in das sichtbare `HomeWorkAssignmentItem`. Dadurch lief die Detailansicht mit `WorkAssignmentId = 0` in einen stillen Vorab-Abbruch und auch der Home-Pfad arbeitete nicht auf einem belastbaren DatensatzschlÃ¼ssel.
- Der Fix bleibt klein und ehrlich:
  - `SupabaseService` schreibt `record.Id` jetzt in das sichtbare Home-Item
  - `HomeViewModel` Ã¼bergibt den Detailbutton nur noch dann als sichtbar, wenn `CanRegister` tatsÃ¤chlich gilt
  - `HomeView.xaml` zeigt den Home-Button ebenfalls nur bei `CanRegister`
  - `HomeSectionDetailViewModel` meldet einen ungÃ¼ltigen Detailkontext jetzt sichtbar statt still zu returnen
- Ergebnis: Home und Detail nutzen weiterhin denselben RPC-Servicepfad, reagieren jetzt aber auch im sichtbaren UI belastbar statt mit stillem No-Op. Der Admin-/Vorstand-Teilnehmerblock blieb bewusst unberÃ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten UI-/Datensatzfix erfolgreich; MAUI wurde nicht beschÃ¤digt.

## 2026-03-24 â€“ RPC-Anmeldeblock fÃ¼r Arbeitseinsatz final verifiziert, committed und gepusht

- Den bereits umgesetzten RPC-Pfad fÃ¼r `Anmelden` nochmals final gegen den realen Arbeitsbaum geprÃ¼ft und `KGV.Wpf` erfolgreich gebaut.
- FÃ¼r den Abschluss wurden ausschlieÃŸlich die zu diesem Block gehÃ¶renden Dateien gestaged; blockfremde lokale Ã„nderungen wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Artefakte blieben unberÃ¼hrt.
- Kurz funktional verifiziert per Codepfad: Home und Detailview rufen denselben Shared-Service `SignUpForArbeitseinsatzAsync(...)` auf; Doppelanmeldung, Frist und Platzgrenze werden vor dem RPC geprÃ¼ft und zusÃ¤tzlich im RPC-Fehlerfall sauber in verstÃ¤ndliche RÃ¼ckmeldungen Ã¼bersetzt.
- Der Block ist damit sauber als eigener Commit abgeschlossen und direkt gepusht.

## 2026-03-24 â€“ `Anmelden` fÃ¼r Arbeitseinsatz produktiv Ã¼ber den echten DB-Funktionspfad angebunden

- Den Block ausdrÃ¼cklich gegen den referenzierten DB-Export `_AI_DB_EXPORT` gefÃ¼hrt. Belastbar genutzt wurden vor allem `database.types.ts` sowie die dort bestÃ¤tigten Funktionen `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` und `sign_off_from_arbeitseinsatz(...)`, auÃŸerdem die Tabelle `arbeitseinsatz_anmeldung` und das Enum `arbeitseinsatz_anmeldung_status`. `roles.sql` wurde als Zusatzkontext geprÃ¼ft; dort liegen keine abweichenden App-seitigen Sonderregeln fÃ¼r diesen Block.
- Daraus den fachlich richtigen Schreibpfad abgeleitet: die App schreibt die Anmeldung nicht als Schatteninsert direkt in `arbeitseinsatz_anmeldung`, sondern nutzt produktiv die vorhandene DB-Funktion `sign_up_for_arbeitseinsatz(...)`. Der direkte Tabellenzugriff bleibt nur lesend fÃ¼r VorabprÃ¼fungen und Status-/KapazitÃ¤tsbewertung.
- Im Shared-Service wurde dafÃ¼r ein echter Produktpfad ergÃ¤nzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` prÃ¼ft zuerst sauber Mitglied, bestehenden Datensatz, aktive Anmeldungen, Frist und Platzgrenze und ruft dann den bestÃ¤tigten RPC-Pfad auf.
- Doppelanmeldungen werden dabei zweistufig verhindert: vor dem RPC Ã¼ber eine gezielte Lesesicht auf `arbeitseinsatz_anmeldung` mit `status = angemeldet`, zusÃ¤tzlich Ã¼ber den DB-Funktionspfad selbst. Falls der RPC in einem Rennen dennoch in einen bereits angemeldeten Zustand lÃ¤uft, wird das klein abgefangen und als verstÃ¤ndliche RÃ¼ckmeldung ausgegeben statt als technischer Rohfehler.
- Home und Detailview verwenden jetzt denselben echten Servicepfad statt doppelter ViewModel-Logik:
  - Home nutzt weiter `RegisterForWorkAssignmentCommand`, ruft aber jetzt den Shared-Service auf
  - die Detailansicht nutzt denselben Servicepfad Ã¼ber den im Kontext mitgegebenen `WorkAssignmentId`
  - beide erhalten dieselbe verstÃ¤ndliche Erfolgs-/FehlerrÃ¼ckmeldung
- Der Mitgliedsbezug bleibt beim real vorhandenen Benutzerpfad: bevorzugt wird `UserContext.MitgliedId`, mit kleinem Fallback Ã¼ber `EnsureCurrentMemberSelectedAsync()`; es wurde keine neue Sonderzuordnung eingefÃ¼hrt.
- Nach erfolgreicher Anmeldung wird der sichtbare Zustand klein aktualisiert: der betroffene Home-Eintrag bzw. der Detailkontext erhÃ¤lt den aktualisierten Startseiten-Datensatz zurÃ¼ck und der Button wird ehrlich deaktiviert, damit keine zweite sofortige Anmeldung ausgelÃ¶st wird. Eine Abmeldung oder Warteliste wurde bewusst nicht neu erÃ¶ffnet.
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde im Shared-Core-/Servicepfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-24 â€“ Home-Zeitwerte fÃ¼r Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Sichtbarkeitsfehler nach dem expliziten WPF-Binding nochmals gegen den kompletten sichtbaren Pfad geprÃ¼ft. Ergebnis: die WPF-Views waren bereits korrekt auf Start-/Endzeit gebunden; der Restfehler saÃŸ davor im Home-Lesepfad, weil `v_startseite_arbeitseinsatz` bzw. `v_startseite_termine` die Zeitfelder im aktuellen Stand nicht in jedem Fall belastbar lieferten.
- Der finale Fix bleibt deshalb klein und produktnah im bestehenden Home-Servicepfad: `LoadStartseiteArbeitseinsaetzeAsync()` und `LoadStartseiteTermineAsync()` laden die Startseiten-Records weiter aus den bestehenden Views, reichern fehlende `Beginn`-/`Ende`-Werte bei Bedarf aber gezielt aus den Basistabellen `arbeitseinsatz` bzw. `termin` anhand der bereits vorhandenen Datensatz-`Id` an.
- Damit ist jetzt belastbar abgesichert: `start_uhrzeit` und `end_uhrzeit` kommen nicht nur in der Basistabelle vor, sondern erreichen den tatsÃ¤chlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lÃ¤sst. Die sichtbare Bindung in Home und Detail kann damit auf denselben expliziten Start-/Endfeldern bleiben.
- Keine neue Fachlogik, keine neue Navigation und keine Home-Schattenarchitektur: es bleibt beim vorhandenen Home-Lesepfad plus kleiner Fallback-Anreicherung nur fÃ¼r fehlende Zeitwerte.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-ErgÃ¤nzung nicht beschÃ¤digt.

## 2026-03-24 â€“ Start- und Endzeit im tatsÃ¤chlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler nochmals Ende-zu-Ende gegen den tatsÃ¤chlichen WPF-Pfad geprÃ¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml`. Ergebnis: `start_uhrzeit`/`end_uhrzeit` kamen im Mapping als normalisierte Werte an, hingen im sichtbaren UI aber weiterhin zu stark an einem zusammengesetzten Zeittext bzw. indirekten Anzeigeweg.
- Die endgÃ¼ltige Korrektur setzt deshalb nicht mehr nur auf einen abstrakten Sammelstring, sondern auf explizite Start-/Endzeit-Properties im tatsÃ¤chlich sichtbaren Pfad: `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `StartTimeText` und `EndTimeText`; der Detailkontext Ã¼bernimmt dieselben Felder 1:1 fÃ¼r genau den ausgewÃ¤hlten Datensatz.
- In `SupabaseService` werden die vorhandenen `Beginn`-/`Ende`-Werte weiterhin zentral Ã¼ber `NormalizeTimeValue(...)` normalisiert und jetzt explizit in diese Start-/Endfelder geschrieben. Damit ist belastbar nachvollziehbar: die Zeitwerte kommen im Modell an und werden nicht mehr erst spÃ¤t oder indirekt aus Nebenfeldern zusammengesetzt.
- Der sichtbare WPF-Pfad wurde anschlieÃŸend passend darauf verdrahtet: auf Home bleibt die Zeitzeile als klarer Zeitraum aus genau dieser Quelle sichtbar; in der Detailansicht werden `Beginn` und `Ende` jetzt explizit als eigene Datensatzangaben gerendert statt nur als zusammengesetzte Sammelzeile.
- `Bekanntmachung` bleibt unverÃ¤ndert stabil: dort werden die neuen Zeitfelder leer Ã¼bergeben, sodass keine Regression in der generischen Detailstruktur entsteht.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/KontextergÃ¤nzungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Home-/Detailanzeige fÃ¼r Arbeitseinsatz und Termin korrigiert: Uhrzeit explizit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den aktuellen Istzustand des bestehenden Anzeige-/Mappingpfads erneut gegen `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und `SupabaseService` geprÃ¼ft. Ergebnis: die Uhrzeit lief bislang teils nur indirekt im `Subtitle` bzw. als separater Beginn-Text, wÃ¤hrend `Arbeitseinsatz`-KapazitÃ¤tsinfos zugleich in `DetailInfo` und `RegistrationInfo` landeten.
- Die Home-Anzeige wurde deshalb auf einen eindeutigen Sichtpfad umgestellt: `Arbeitseinsatz` und `Termin` erhalten jetzt jeweils einen expliziten `TimeText`, der Ã¼ber `BuildTimeRange(...)` aus Start- und Endzeit erzeugt und in Home sichtbar als `Uhrzeit` gerendert wird. Damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, wird konsistent `Start â€“ Ende` angezeigt.
- Die Detailview nutzt denselben expliziten Zeitpfad jetzt ebenfalls fÃ¼r genau den ausgewÃ¤hlten Datensatz: `HomeSectionDetailContext` trÃ¤gt `TimeText`, `HomeSectionDetailViewModel` reicht ihn 1:1 weiter, und `HomeSectionDetailView.xaml` zeigt ihn separat als `Uhrzeit` an. Dadurch werden Start- und Endzeit fÃ¼r `Arbeitseinsatz` und `Termin` sichtbar, ohne neue Listendarstellung oder Sammelkontexte einzufÃ¼hren.
- Die doppelte Anzeige `Angemeldet: 0` kam aus zwei Quellen desselben Datensatzes: einmal aus `DetailInfo` (`Angemeldet`, `Freie PlÃ¤tze`) und zusÃ¤tzlich nochmals aus `RegistrationInfo` Ã¼ber `BuildCapacityText(...)`. Der Mappingpfad fÃ¼r `Arbeitseinsatz` wurde deshalb bereinigt: KapazitÃ¤tsinfos bleiben jetzt nur noch in `RegistrationInfo`; aus `DetailInfo` wurden die redundanten `Angemeldet`-/`Freie PlÃ¤tze`-Zeilen entfernt.
- `DetailInfo` bleibt damit auf die Ã¼brigen Felder des ausgewÃ¤hlten Datensatzes fokussiert (`Thema`, `Datum`, `Treffpunkt`, ggf. `Max. Teilnehmer`), wÃ¤hrend Registrierungs-/KapazitÃ¤tsinfos nur noch einmal sichtbar sind. So bleibt die Detailview weiter auf genau einen Datensatz begrenzt, aber ohne redundante Anzeige-Fragmente.
- Die generische Detailstruktur fÃ¼r `Bekanntmachung` wurde mitgeprÃ¼ft und nicht verschlechtert: dort bleibt `TimeText` leer, wÃ¤hrend `Subtitle`, `AdditionalInfo` und `Content` unverÃ¤ndert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fix erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Home-/Detailansicht fÃ¼r ArbeitseinsÃ¤tze und Termine vervollstÃ¤ndigt: Startzeit sichtbar, Detailinfos vollstÃ¤ndig, Detailkontext bleibt beim ausgewÃ¤hlten Datensatz

- Den bestehenden Home-/Detailpfad vor dem Fix gegen die realen WPF-Dateien und das aktuelle Mapping geprÃ¼ft: `HomeView`, `HomeViewModel`, `HomeSectionDetailView`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Items sowie das Mapping in `SupabaseService` waren bereits generisch vorhanden; `Arbeitseinsatz` zeigte die Startzeit auf Home schon separat, `Termin` dagegen noch nicht.
- Die Home-Ãœbersicht wurde deshalb gezielt vervollstÃ¤ndigt, ohne neue Sondernavigation zu bauen: `Termin` zeigt die Startzeit jetzt wie `Arbeitseinsatz` zusÃ¤tzlich sichtbar in der Karte an. FÃ¼r `Arbeitseinsatz` blieb die vorhandene konsistente Darstellung mit sichtbarem Beginn erhalten.
- Die Detailview wurde innerhalb der bestehenden Struktur ergÃ¤nzt statt neu aufgebaut: zusÃ¤tzlich zu `Title`, `Subtitle`, `AdditionalInfo` und `Content` zeigt sie jetzt auch die bereits aus dem gewÃ¤hlten Datensatz stammende Registrierungsinformation an. Dadurch bleiben fÃ¼r `Arbeitseinsatz` und `Termin` die restlichen zum Datensatz gehÃ¶renden Angaben sichtbar, ohne Felder aus anderen EintrÃ¤gen zu vermischen.
- Den Punkt â€žnur ausgewÃ¤hlten Datensatz anzeigenâ€œ explizit auf den bestehenden Kontextpfad abgesichert: `HomeViewModel` Ã¼bergibt an die Detailansicht weiterhin nur skalare Kontextdaten des konkret angeklickten Eintrags (`Title`, `Subtitle`, `Content`, `AdditionalInfo`, Registrierungsinfo/Anmeldeaktion). Es wird keine Liste und kein unscharfer Sammelkontext an die Detailview gereicht.
- Der `Anmelden`-Button ist in der Detailview jetzt konsistent fÃ¼r `Arbeitseinsatz` sichtbar und nutzt denselben ehrlichen Pfad wie auf Home: derselbe Hinweisdialog, keine neue Fake-Fachlogik und weiterhin kein erfundener Schreibzugriff, solange der echte belastbare Anmeldepfad im Repo fehlt.
- `Bekanntmachung` wurde im selben generischen Detailpfad mitgeprÃ¼ft: die bestehende Darstellung Ã¼ber `Title`, `Subtitle`, `AdditionalInfo` und `Content` bleibt erhalten; es geht dort kein Feld verloren und es wurde keine kaputte Spezialdarstellung eingefÃ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die gezielten Shared-/Kontextanpassungen nicht beschÃ¤digt.

## 2026-03-24 â€“ Arbeitsstunden endgÃ¼ltig gegen `id = 0` abgesichert, Save-RÃ¼cknavigation nachgezogen, Arbeitseinsatz-Home-Buttons korrigiert

- Den erneuten realen Fehlernachweis `arbeitsstunde.id = 0` nach dem vorigen Fix ausdrÃ¼cklich als Beleg genommen, dass Attributverhalten allein im laufenden Pfad nicht reicht. Deshalb den kompletten Produktpfad nochmals bis zur tatsÃ¤chlichen Create-Payload geprÃ¼ft: WPF-Erfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogpfad (`ArbeitsstundenViewModel`), MAUI-Erfassung (`MyArbeitsstundenPage`) und `SupabaseService.AddArbeitsstundeAsync(...)`.
- Die robuste Korrektur liegt jetzt nicht mehr nur im Record-Attribut, sondern technisch im Insertmodell selbst: fÃ¼r `arbeitsstunde` gibt es nun einen expliziten Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`), und `AddArbeitsstundeAsync(...)` schreibt beim Insert nur noch Ã¼ber genau diesen Typ. Damit kann `Id` â€“ auch wenn aufrufende `ArbeitsstundeRecord`-Instanzen lokal weiter mit Default `0` ankommen â€“ technisch nicht mehr in die Create-Payload gelangen.
- Als zusÃ¤tzliche Guard-Absicherung Ã¼bernimmt der Create-Pfad die `Id` grundsÃ¤tzlich Ã¼berhaupt nicht mehr. Es wird weder auf positive noch auf nichtpositive `Id` vertraut; Updates bleiben weiterhin separat Ã¼ber `UpdateArbeitsstundeAsync(...)` mit echter bestehender `Id` bestehen. Eine App-seitige `lastrow+1`-Logik wurde bewusst nicht eingefÃ¼hrt.
- Die Arbeitsstunden-Erfassungsansicht wurde im Abschlusszug ebenfalls auf das gewÃ¼nschte Produktverhalten gezogen: nach erfolgreichem Speichern bleibt die Eingabemaske nicht mehr offen stehen, sondern verlÃ¤sst den Pfad sauber. Im Dialogkontext schlieÃŸt sich das Fenster wie bisher; im normalen WPF-Erfassungspfad wird nach erfolgreichem Save zurÃ¼ck auf `Home` navigiert.
- Die Arbeitsstunden-Freigabeansicht zieht jetzt dasselbe Abschlussverhalten nach: wenn alle markierten/geÃ¤nderten Zeilen erfolgreich gespeichert wurden, wird nicht in der PrÃ¼fansicht verharrt, sondern wieder sauber auf `Home` zurÃ¼cknavigiert. Der bestehende Fachpfad bleibt unverÃ¤ndert; `status` bleibt optionales Anmerkungsfeld.
- Den Home-Eintrag fÃ¼r `Arbeitseinsatz` auf den gewÃ¼nschten Interaktionsstand korrigiert: der separate `Details`-Button wurde entfernt, DetailÃ¶ffnung lÃ¤uft jetzt per Doppelklick auf die Karte. Stattdessen bleibt dort der Button `Anmelden` sichtbar. Weil weiterhin kein belastbarer produktiver Schreibpfad fÃ¼r die echte Anmeldung vorhanden ist, bleibt dieser Button bewusst eine ehrliche Info-Aktion statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-23 â€“ Zwei echte Arbeitsstundenfehler behoben: `id = 0` bei Neuanlage unterbunden und Freigabe-Checkbox aktiviert Speichern wieder sofort

- Den realen Produktfehler im Arbeitsstunden-Insertpfad diesmal bewusst bis zur tatsÃ¤chlich laufenden Payload verfolgt statt nur den Service isoliert zu betrachten: WPF-Usererfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogerfassung (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) erzeugen alle neue `ArbeitsstundeRecord`-Instanzen ohne explizite `Id`-Zuweisung, damit aber typbedingt mit lokalem Default `Id = 0`.
- Die eigentliche Ursache lag im Modellvertrag von `ArbeitsstundeRecord`: anders als bei den Ã¼brigen produktiven Create-Modellen stand dort noch `[PrimaryKey("id")]` ohne explizites `shouldInsert = false`. Im aktuell laufenden Paketstand `Supabase.Postgrest 4.1.0` konnte die PrimÃ¤rschlÃ¼sselspalte dadurch im realen Insertpfad weiter in die Payload gelangen; genau so lieÃŸ sich der reproduzierte neue Datensatz mit `id = 0` erklÃ¤ren.
- Den Fix deshalb am tatsÃ¤chlichen Fehlerursprung gesetzt: `ArbeitsstundeRecord` verwendet jetzt wie die Ã¼brigen produktiven Create-Modelle explizit `[PrimaryKey("id", false)]`. Damit bleibt die `Id` fÃ¼r Update-/PrimaryKey-Zwecke vorhanden, wird bei Neuanlagen aber nicht mehr in die Insert-Payload aufgenommen.
- Die zweite reale StÃ¶rung in `Arbeitsstunden freigeben` ebenfalls bis zur konkreten UI-Ursache zurÃ¼ckgefÃ¼hrt: `status` war fachlich bereits optional und wurde in `HasChanges` nicht als Pflichtfeld behandelt. Das Problem lag stattdessen an der WPF-Spalte `DataGridCheckBoxColumn` fÃ¼r `Freigeben`; deren Wert wurde im laufenden UI-Pfad nicht zuverlÃ¤ssig sofort in das Item zurÃ¼ckgeschrieben, sodass `PropertyChanged`, `HasPendingChanges` und damit `SpeichernCommand` beim bloÃŸen Setzen der Checkbox nicht unmittelbar anzogen.
- Den Fix daher klein und UI-seitig gehalten: die Checkboxspalte lÃ¤uft jetzt als `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`. Damit gilt schon das reine Setzen von `Freigeben` sofort als speicherrelevante Ã„nderung.
- Fachregeln bleiben unverÃ¤ndert: offen bleibt `freigegeben = false`, `status` bleibt nur optionales Anmerkungsfeld, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`, und es wurde keine neue Freigabearchitektur oder App-seitige `lastrow+1`-Logik eingefÃ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im selben Shared-Pfad mitgedacht und nicht beschÃ¤digt.

## 2026-03-23 â€“ WPF-Bindingfix in `ArbeitsstundenErfassungView`: `CurrentMemberText` als reine OneWay-Anzeige verdrahtet

- Den aktuellen Bindingfehler in `ArbeitsstundenErfassungView` gezielt auf die einzige Bindungsstelle von `CurrentMemberText` zurÃ¼ckgefÃ¼hrt: der Wert dient fachlich nur der Anzeige des aktuellen Mitgliedskontexts.
- Deshalb bewusst **keinen** Setter in `ArbeitsstundenErfassungViewModel` ergÃ¤nzt und keine Fachlogik verÃ¤ndert: `CurrentMemberText` bleibt eine reine Anzeigeeigenschaft.
- Die Anzeige in `ArbeitsstundenErfassungView.xaml` wurde minimal und robust auf explizite OneWay-Darstellung umgestellt: statt `Run` wird der Wert jetzt in einem eigenen `TextBlock` mit `Mode=OneWay` angezeigt.
- Die benachbarten Anzeige-`TextBlock`s bleiben strukturell intakt; es wurde nur der kleine Anzeigeabschnitt fÃ¼r das Mitglied ersetzt, ohne weitere Formularlogik oder ViewModel-VertrÃ¤ge anzufassen.
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Der kleine Bindingfix ist damit sauber abgeschlossen; MAUI blieb unberÃ¼hrt.

## 2026-03-23 Ã¢â‚¬â€œ Insert-Pfade auf explizite ID-Mitgabe geprÃƒÂ¼ft: produktive Neuanlagen senden aktuell keine feste ID aus der App

- Den aktuellen produktiven Insert-/Create-Stand zuerst gegen den realen Arbeitsbaum geprÃƒÂ¼ft statt auf Verdacht umzubauen: gesichtet wurden `SupabaseService`, die betroffenen Records/Modelle, die WPF-/MAUI-Neuanlagepfade fÃƒÂ¼r `arbeitsstunde`, `termin`, `bekanntmachung` und `arbeitseinsatz` sowie ein mÃƒÂ¶glicher Pfad fÃƒÂ¼r `arbeitseinsatz_anmeldung`.
- FÃƒÂ¼r `arbeitsstunde` den konkreten Verdacht am echten Produktpfad geprÃƒÂ¼ft: WPF (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) bauen neue `ArbeitsstundeRecord`-Instanzen ohne gesetzte `Id`; `AddArbeitsstundeAsync(...)` erstellt daraus wiederum ein neues Insert-Objekt ebenfalls ohne `Id`-Zuweisung und sendet nur die fachlichen Felder an PostgREST.
- FÃƒÂ¼r die drei Verwaltungs-Neuanlagen denselben Punkt abgesichert: die WPF-Editoren bauen im New-Mode zwar Arbeitsobjekte mit `Id = 0` als lokalem Defaultwert, aber die produktiven Service-Create-Pfade `CreateTerminAsync(...)`, `CreateBekanntmachungAsync(...)` und `CreateArbeitseinsatzAsync(...)` ÃƒÂ¼bernehmen diese `Id` gerade **nicht** in ihre tatsÃƒÂ¤chlichen Insert-Objekte. Gesendet werden dort nur die fachlichen Spalten; Updates laufen weiterhin separat ÃƒÂ¼ber die vorhandene Datensatz-`Id`.
- Einen echten produktiven Insert-Pfad fÃƒÂ¼r `arbeitseinsatz_anmeldung` gibt es im aktuellen Repo weiterhin nicht: der vorhandene `Anmelden`-Pfad auf Home ist nach wie vor nur eine ehrliche Info-Aktion ohne Schreibzugriff. Entsprechend war dort aktuell auch kein produktiver ID-Insertpfad zu korrigieren.
- ZusÃƒÂ¤tzlich den Library-Unterbau gegen den aktuellen Paketstand geprÃƒÂ¼ft: im verwendeten `Supabase.Postgrest`-Paket ist beim `PrimaryKey`-Attribut der Parameter `shouldInsert` standardmÃƒÂ¤ÃƒÅ¸ig `false`. Damit wird die PrimÃƒÂ¤rschlÃƒÂ¼sselspalte auch beim verbleibenden Muster `[PrimaryKey("id")]` nicht automatisch in Insert-Payloads aufgenommen, solange sie nicht explizit gesetzt und bewusst anderweitig serialisiert wird.
- Ergebnis der PrÃƒÂ¼fung: im aktuellen produktiven App-Code lieÃƒÅ¸ sich fÃƒÂ¼r die geprÃƒÂ¼ften Neuanlagepfade keine Stelle nachweisen, an der bei Inserts eine feste `id` oder konkret `id = 0` aus der App mitgesendet wird.
- Deshalb bewusst **kein** Vermutungsfix eingebaut: keine `lastrow+1`-Logik, keine unnÃƒÂ¶tige Schatten-Insertarchitektur und keine DB-MigrationsÃƒÂ¤nderung auf Verdacht. Der Block bleibt bei der belastbaren Analyse und Dokumentation des aktuell bereits korrekten Insert-Musters `Insert ohne feste ID, Update mit bestehender ID`.
- Repo-seitig zusÃƒÂ¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine versionierten SQL-/Migrationsdateien fÃƒÂ¼r die betroffenen Tabellen vor; eine DB-seitige Sequence-/Default-PrÃƒÂ¼fung war im Repo daher nicht weiter nachvollziehbar, wurde aber fÃƒÂ¼r diesen Block auch nicht benÃƒÂ¶tigt, weil auf App-Seite kein fehlerhafter ID-Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wird durch diesen Dokumentations-/Analyseblock nicht beschÃƒÂ¤digt.

## 2026-03-23 Ã¢â‚¬â€œ Home-/Detail-Block fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze sauber abgeschlossen: Beginn sichtbar, Detaildaten vervollstÃƒÂ¤ndigt, Anmelden ehrlich nur informativ

- Den begonnenen Home-/Detail-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufzuziehen: bereits halbfertig vorhanden waren erweiterte Home-/Detailmodelle, ein vorbereiteter optionaler `Anmelden`-Pfad im Detailkontext und erste Anpassungen im `HomeViewModel`, aber die sichtbaren WPF-XAML-Schritte und der hintere Helper-Tail des `SupabaseService` waren noch nicht sauber abgeschlossen.
- Den Home-/Detailpfad fÃƒÂ¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` gemeinsam nachgezogen, damit die generische Detailansicht durch die Erweiterung keine Felder verliert: zusÃƒÂ¤tzliche Datensatzinformationen laufen jetzt konsistent ÃƒÂ¼ber `DetailInfo`/`AdditionalInfo`, statt nur bei ArbeitseinsÃƒÂ¤tzen Sonderwissen lokal in der View zu hinterlegen.
- `Arbeitseinsatz` zeigt auf Home jetzt den Beginn explizit sichtbar im Karteneintrag; zusÃƒÂ¤tzlich gibt es dort einen eigenen `Details`-Button und Ã¢â‚¬â€œ nur wenn der Datensatz ÃƒÂ¼berhaupt anmeldbar markiert ist Ã¢â‚¬â€œ einen `Anmelden`-Button.
- Die Home-Detailansicht zeigt fÃƒÂ¼r `Arbeitseinsatz` jetzt neben Titel/Untertitel auch die ÃƒÂ¼brigen im Datensatz belastbar vorhandenen Informationen wie Thema, Datum, Beginn, Ende, Treffpunkt sowie KapazitÃƒÂ¤ts-/Anmeldeinformationen; `Max. Teilnehmer` wird bewusst nur dann angezeigt, wenn sie aus den vorhandenen Datensatzwerten ÃƒÂ¼berhaupt belastbar ableitbar ist.
- `Termin`- und `Bekanntmachung`-Detailansichten wurden im selben Pfad mitkonsolidiert: zusÃƒÂ¤tzliche Felder wie Ort, Thema, Betreff, VerÃƒÂ¶ffentlichungszeitpunkt oder Kurztext werden jetzt ebenfalls ÃƒÂ¼ber denselben Detailkontext transportiert, statt in der generischen Detailview verloren zu gehen.
- Den halbfertig beschÃƒÂ¤digten Tail des `SupabaseService` sauber wieder geschlossen und dabei die kleinen Home-Helfer zentral konsolidiert (`AddDetailLine`, Datum-/Zeitformatierung, Textnormalisierung, Dokumentpfad-Helfer etc.), damit der angefangene Block wieder auf einem kompilierbaren produktiven Stand steht.
- Den `Anmelden`-Usecase bewusst **nicht** als neue Schattenlogik erÃƒÂ¶ffnet: im aktuellen Repo lieÃƒÅ¸ sich weiterhin kein belastbarer produktiver Schreibpfad fÃƒÂ¼r eine echte Arbeitseinsatz-Anmeldung finden. Deshalb ist der neue `Anmelden`-Button in WPF nur als ehrliche Info-Aktion verdrahtet und weist explizit darauf hin, dass der echte Schreibpfad noch nicht angebunden ist.
- WPF wurde sichtbar fertiggestellt; MAUI wurde bei den gemeinsamen Home-Items/Mappingerweiterungen mitgedacht, aber bewusst nicht mit einer neuen Arbeitseinsatz-AnmeldeoberflÃƒÂ¤che erweitert, solange dafÃƒÂ¼r kein belastbarer gemeinsamer Fachpfad existiert.
- Technisch verifiziert: Workspace-Build erfolgreich; der begonnene Home-/Detail-Block ist damit ohne erfundene Anmeldefachlogik sauber abgeschlossen.

## 2026-03-23 Ã¢â‚¬â€œ Verwaltungseditoren und Home-Navigation nachgezogen: ZurÃƒÂ¼ck-Text repariert, Doppelklick auf Listen, Arbeitsstunden-Erfassung nur noch ÃƒÂ¼ber Home

- Die drei WPF-Verwaltungseditoren `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen` nach dem Parserfix nochmals produktiv bereinigt: der beschÃƒÂ¤digte Toolbar-Text `ZurÃƒÂ¼ck` wird jetzt XAML-seitig stabil als Entity geschrieben, damit kein erneutes Zeichensatzproblem im Buttontext entsteht.
- FÃƒÂ¼r alle drei Verwaltungslisten einen echten Doppelklickpfad auf die ausgewÃƒÂ¤hlten EintrÃƒÂ¤ge ergÃƒÂ¤nzt: statt auf `InputBindings` zu vertrauen, ÃƒÂ¶ffnen die `ListBox`-EintrÃƒÂ¤ge jetzt direkt per `MouseDoubleClick` den vorhandenen `OeffnenCommand` und damit den Bearbeitungseditor zuverlÃƒÂ¤ssig.
- Die WPF-Hauptnavigation wieder auf den gewÃƒÂ¼nschten Produktpfad zurÃƒÂ¼ckgezogen: `Arbeitsstunden erfassen` wird nicht mehr als eigener Navigationseintrag aufgebaut und bleibt nur noch ÃƒÂ¼ber den vorhandenen Button auf der `Startseite` erreichbar.

## 2026-03-23 Ã¢â‚¬â€œ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÂ¤chliche XAML-Ursache zurÃƒÂ¼ckgefÃƒÂ¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÂ¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÂ¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÂ¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÂ¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 Ã¢â‚¬â€œ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÂ¼ck-/Dirty-Check ergÃƒÂ¤nzt, Navigation auf Home-only zurÃƒÂ¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÂ¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÂ¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÂ¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÅ¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÂ¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÂ¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÂ¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÂ¼r geladene VerwaltungsdatensÃƒÂ¤tze sowie fÃƒÂ¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÂ¼ckkehr erfolgt ÃƒÂ¼ber den vorhandenen Navigationspfad zurÃƒÂ¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÂ¼ck`-Button
  - `ZurÃƒÂ¼ck` und `Abbrechen` prÃƒÂ¼fen auf echte ungespeicherte Ãƒâ€žnderungen
  - die RÃƒÂ¼ckfrage erscheint nur bei tatsÃƒÂ¤chlichen Ãƒâ€žnderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÂ¼nschten Produktpfad zurÃƒÂ¼ckgezogen:
  - `ArbeitseinsÃƒÂ¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÂ¼ber die vorhandenen Home-Buttons fÃƒÂ¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÂ¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÂ¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÂ¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÂ¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÂ¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÂ¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÂ¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÂ¼ber `status`; die globale PrÃƒÂ¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÂ¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÂ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÂ¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÂ¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÂ¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÂ¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÂ¤chlich geÃƒÂ¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÂ¤tze bleiben unverÃƒÂ¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÂ¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÂ¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÂ¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÂ¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÂ¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Ãƒâ€“ffnen der Freigabeansicht wird eine globale PrÃƒÂ¼fsperre ÃƒÂ¼ber die offenen `arbeitstunde`-DatensÃƒÂ¤tze gesetzt
  - andere PrÃƒÂ¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÂ¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÂ¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÂ¤ngert die Sperre wÃƒÂ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÂ¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÂ¼bernehmbar
- Die bestehende Badge-/ZÃƒÂ¤hlerlogik bleibt weiterverwendet: Ãƒâ€žnderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÂ¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÂ¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÂ¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÂ¼r offene FÃƒÂ¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÂ¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÂ¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÂ¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÂ¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÂ¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÂ¼r `Datum`, `Stunden` und `Art der Arbeit` Ã¢â‚¬â€œ ohne zusÃƒÂ¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÂ¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÂ¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÂ¼r Benutzer mit eigenem Mitgliedskontext; zusÃƒÂ¤tzlich erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÂ¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÂ¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÂ¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÂ¶ÃƒÅ¸er als `0` sein.
- FÃƒÂ¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÂ¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÂ¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÂ¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÂ¤hler zeigt weiter die tatsÃƒÂ¤chliche Anzahl offener PrÃƒÂ¼ffÃƒÂ¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÂ¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÂ¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÂ¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÂ¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÂ¤hrend im Repo noch die ÃƒÂ¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÂ¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÂ¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÂ¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÂ¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÂ¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÂ¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÅ¸lich fÃƒÂ¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÂ¼r spÃƒÂ¤tere MAUI-ParitÃƒÂ¤t anschlussfÃƒÂ¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÂ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÂ¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÂ¤hrend die eigentlichen EditoroberflÃƒÂ¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÂ¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÂ¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÂ¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ ArbeitseinsÃƒÂ¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÂ¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÂ¤tze-Verwaltung erneut geprÃƒÂ¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÂ¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÂ¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÂ¤nzt und nicht ÃƒÂ¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÂ¤nderte DatensÃƒÂ¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÂ¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÂ¶ÃƒÅ¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÂ¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÂ¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÂ¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÂ¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÂ¶ÃƒÅ¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÂ¤tze-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÂ¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÂ¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÂ¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÂ¤nzt und nicht ÃƒÂ¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÂ¤nderte DatensÃƒÂ¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÂ¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÂ¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÂ¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÂ¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÅ“berschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÂ¤nzt und parallel in einer kompakten Vorschau geprÃƒÂ¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÂ¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÂ¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÂ¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÂ¤chsten AufrÃƒÂ¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÂ¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÂ¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÂ¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÅ¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÂ¤nderte DatensÃƒÂ¤tze direkt per Create/Update wieder nach `termin` zurÃƒÂ¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÂ¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÂ¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÂ¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÂ¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÂ¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÂ¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÂ¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÂ¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÂ¼r `Bekanntmachungen` und `ArbeitseinsÃƒÂ¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÂ¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÂ¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÂ¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÅ“bersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÂ¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÂ¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÂ¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÂ¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÂ¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÂ¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÂ¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÂ¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÂ¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÂ¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÂ¼r Admin/Vorstand verfÃƒÂ¼gbar und blÃƒÂ¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÂ¼r spÃƒÂ¤tere MAUI-ParitÃƒÂ¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÂ¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÂ¤tigt: `ArbeitseinsÃƒÂ¤tze` und `Termine` laden ÃƒÂ¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÂ¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÂ¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÅ¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÂ¤tigte View.
- FÃƒÂ¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÂ¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÂ¼fung bleibt daher auf tatsÃƒÂ¤chliche RÃƒÂ¼ckgabefelder bzw. reale DatensÃƒÂ¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÂ¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÂ¤tzen, Terminen und Bekanntmachungen ÃƒÂ¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 Ã¢â‚¬â€œ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÂ¶ffnen in eigener View

- Den Home-Stand fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen auf den gewÃƒÂ¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÂ¼bersichten, wÃƒÂ¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÂ¼r eine eigenstÃƒÂ¤ndige WPF-Detailansicht fÃƒÂ¼r Home-Elemente ergÃƒÂ¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Ãƒâ€“ffnen-Kommandos fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÂ¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÂ¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÂ¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÂ¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÂ¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÂ¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÂ¼ft. Die entscheidende technische Ursache fÃƒÂ¼r den Pflichtstundenfehler lieÃƒÅ¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÂ¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÂ¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÂ¤hrend `ArbeitseinsÃƒÂ¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÂ¼r Home jetzt zunÃƒÂ¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÅ¸end robust clientseitig ÃƒÂ¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÂ¼r Pflichtstunden ergÃƒÂ¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÂ¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÂ¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÂ¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÂ¼ckgelieferte EintrÃƒÂ¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÂ¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÂ¤nzt: der Home-Pfad schreibt jetzt fÃƒÂ¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÂ¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÂ¼ber Hauptmitglied/Saison prÃƒÂ¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÂ¼ft: da `ArbeitseinsÃƒÂ¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen Ã¢â‚¬â€œ Pflichtstunden ÃƒÂ¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÂ¶sung und Bekanntmachungen ÃƒÂ¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÂ¼r Home deshalb final prÃƒÂ¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÂ¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÂ¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÂ¼bersicht ÃƒÂ¼ber dieses Hauptmitglied geladen.
- ZusÃƒÂ¤tzlich wird die bestÃƒÂ¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÂ¼cksichtigt, sondern zuerst direkt fÃƒÂ¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÂ¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÂ¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÂ¤tzlich mÃƒÂ¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÂ¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÂ¼r Titel, Inhalt und VerÃƒÂ¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÂ¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÂ¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÂ¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÂ¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÂ¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÂ¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÂ¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÂ¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÂ¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÂ¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÂ¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÂ¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÂ¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÂ¤tzlich vereinfacht und nÃƒÂ¤her auf den bestÃƒÂ¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÂ¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÂ¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÂ¶rt Ã¢â‚¬â€œ im bestÃƒÂ¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Home-Diagnose: bestÃƒÂ¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÂ¤tigten DB-Schema-Stand geprÃƒÂ¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÂ¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÂ¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÂ¤tigten Kern geschÃƒÂ¤rft, ohne neue App-Logik einzufÃƒÂ¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÂ¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÂ¶st zuerst die aktuelle Saison ÃƒÂ¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÂ¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÂ¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÂ¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÂ¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÂ¼cke ÃƒÂ¼ber MAUI-PrÃƒÂ¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÂ¶glichen Abschlussoptionen geprÃƒÂ¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÂ¼ckgabe-/Kommentarpfad wÃƒÂ¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÂ¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÂ¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÂ¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÂ¤hlt und den vorhandenen mobilen PrÃƒÂ¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÂ¼fen` mobil jetzt nur noch dann an, wenn ÃƒÂ¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÂ¤chlich offene PrÃƒÂ¼ffÃƒÂ¤lle vorliegen; zusÃƒÂ¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÂ¤tze direkt im Titel des MenÃƒÂ¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÂ¼ber denselben gemeinsamen PrÃƒÂ¼fpfad, sodass MenÃƒÂ¼eintrag und tatsÃƒÂ¤chliche offene FÃƒÂ¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÂ¼hrt: die mobile Seite bleibt vollstÃƒÂ¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÂ¼fstatusanzeige drauÃƒÅ¸en, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÂ¼fflow in WPF bis zur Freigabe vervollstÃƒÂ¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÂ¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÂ¼ft: belastbar vorhanden waren bereits PrÃƒÂ¼fliste, Badge/ZÃƒÂ¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÂ¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÂ¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÂ¼ckgefÃƒÂ¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÂ¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÂ¼fmodus geÃƒÂ¶ffnet, gefiltert auf die tatsÃƒÂ¤chlich offenen DatensÃƒÂ¤tze des ausgewÃƒÂ¤hlten Mitglieds.
- DafÃƒÂ¼r nur einen kleinen Navigationskontext ergÃƒÂ¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÂ¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÂ¼feintrÃƒÂ¤ge des konkret ausgewÃƒÂ¤hlten Mitglieds im PrÃƒÂ¼fmodus erscheinen.
- Admin/Vorstand kÃƒÂ¶nnen im PrÃƒÂ¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÂ¤tzlich eine direkte `Freigeben`-Aktion fÃƒÂ¼r den ausgewÃƒÂ¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÂ¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÂ¼r einen sauberen RÃƒÂ¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÂ¼fliste und Badge/ZÃƒÂ¤hler weiterhin ÃƒÂ¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÂ¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÂ¼fmodus fachlich geschÃƒÂ¤rft: klarer Titel, Hinweistext, leerer PrÃƒÂ¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÂ¤nde fÃƒÂ¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÂ¼fliste und PrÃƒÂ¼fzÃƒÂ¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÂ¤ndigung des PrÃƒÂ¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÂ¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÂ¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation and den gemeinsamen PrÃƒÂ¼fpfad erneut geprÃƒÂ¼ft: ein belastbarer Listen-/Detailpfad fÃƒÂ¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÂ¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÂ¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÂ¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÂ¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÂ¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÂ¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÂ¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÂ¤ge, Vorstand/Admin kÃƒÂ¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÂ¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÂ¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÅ¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÂ¼fpfad fÃƒÂ¼r offene Arbeitsstunden ergÃƒÂ¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÂ¼hren von der PrÃƒÂ¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÂ¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÂ¼fen` erscheint fÃƒÂ¼r berechtigte Benutzer jetzt nur bei tatsÃƒÂ¤chlich offenen PrÃƒÂ¼ffÃƒÂ¤llen, zeigt einen kleinen ZÃƒÂ¤hler mit der Zahl offener DatensÃƒÂ¤tze und wird bei offenem PrÃƒÂ¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÂ¤uft nach Ãƒâ€žnderungen ÃƒÂ¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÂ¼flisten und PrÃƒÂ¼fzÃƒÂ¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÂ¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÂ¤rten produktiven DB-Pfade geprÃƒÂ¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÂ¤hrend `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÂ¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÂ¤hen: `HomeOverviewDTO` trÃƒÂ¤gt jetzt zusÃƒÂ¤tzlich gemeinsame Startseiten-Daten fÃƒÂ¼r Pflichtstunden, ArbeitseinsÃƒÂ¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÂ¤dt diese Inhalte direkt aus den geklÃƒÂ¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÂ¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÂ¼r die Home-Ausgabe mitgefÃƒÂ¼hrt.
- Bereich `ArbeitseinsÃƒÂ¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÂ¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÂ¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÂ¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÂ¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÂ¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÂ¤sst sich wieder mit `SchlieÃƒÅ¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÂ¼r Bekanntmachungen angebunden: echte EintrÃƒÂ¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÂ¼gbaren Inhalte statt des bisherigen kÃƒÂ¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÂ¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÂ¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÂ¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÂ¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÂ¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÂ¼ft: konkret auffÃƒÂ¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÂ¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÂ¶ÃƒÅ¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÂ¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÂ¼ckgefÃƒÂ¼hrt: `MainWindowViewModel` lÃƒÂ¤dt fÃƒÂ¼r den Eigenkontext weiter den nÃƒÂ¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÂ¤ÃƒÅ¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÂ¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÂ¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÂ¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÂ¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÂ¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÅ¸en`-Button ergÃƒÂ¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÂ¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÂ¼ft und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÂ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÂ¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÂ¼fpfad fÃƒÂ¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÂ¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÂ¤tzliche Infrastruktur direkt testbar.
- DafÃƒÂ¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÂ¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÂ¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÂ¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÂ¤chsten PrÃƒÂ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÂ¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÂ¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÂ¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÂ¤chsten kleinen PrÃƒÂ¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÂ¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÂ¼r Desktop und Mobil und lÃƒÂ¤sst sich ohne zusÃƒÂ¤tzliche Testinfrastruktur direkt prÃƒÂ¼fen.
- DafÃƒÂ¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÂ¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÂ¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÂ¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÂ¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÂ¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÂ¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÂ¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÂ¼ckgezogen, sondern sinnvoll zusammengefÃƒÂ¼hrt: die aktive Root-Projektdatei ÃƒÂ¼bernimmt jetzt wieder die fÃƒÂ¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÂ¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÂ¤ndig erhalten bleibt.
- Fachlich passende ÃƒÂ¤ltere Tests sauber zurÃƒÂ¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÂ¼fpfad fÃƒÂ¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÅ¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÂ¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÂ¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÂ¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÂ¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÂ¼r Demo-/Testdatenfilter zurÃƒÂ¼ckgeholt

- Die aktuelle Testlage geprÃƒÂ¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÂ¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÂ¤ltere Testidee fÃƒÂ¼r `ExportViewModel`, die fÃƒÂ¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÂ¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÂ¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÂ¼fung) ohne dass dafÃƒÂ¼r eine groÃƒÅ¸e Testinfrastruktur nÃƒÂ¶tig wÃƒÂ¤re.
- DafÃƒÂ¼r eine kleine aktive Testbasis im Root zurÃƒÂ¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÂ¼ft zurÃƒÂ¼ckzuziehen.
- Den gewÃƒÂ¤hlten PrÃƒÂ¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÂ¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÂ¼ckgezogen: sie waren fÃƒÂ¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÂ¤tten den Wiedereinstieg unnÃƒÂ¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÂ¤tzlich lÃƒÂ¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÂ¼nen Tests.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÂ¤t mit Arbeitsstunden-PrÃƒÂ¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÂ¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÂ¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÂ¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÂ¤tslÃƒÂ¼cke fÃƒÂ¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÂ¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÂ¼hrt: die Umsetzung bleibt vollstÃƒÂ¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÂ¼r offene Arbeitsstunden berÃƒÂ¼cksichtigt nur operative Mitglieder ÃƒÂ¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÂ¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÂ¼r Arbeitsstunden, keine zusÃƒÂ¤tzlichen WPF-OberflÃƒÂ¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 2: NÃƒÂ¤chste mobile Admin-ParitÃƒÂ¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÂ¼cken erneut gegen die produktiven WPF-Wege geprÃƒÂ¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÂ¤chste kleine, aber fachlich wichtige ParitÃƒÂ¤tslÃƒÂ¼cke, weil WPF dafÃƒÂ¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÂ¤tslÃƒÂ¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÂ¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÂ¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÂ¼r RollenÃƒÂ¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÂ¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÂ¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÂ¤chliche Mitgliedsrolle vor einem evtl. ÃƒÂ¤lteren `app_user`-Wert, damit RollenÃƒÂ¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÅ¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÂ¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÂ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 1: NÃƒÂ¤chste mobile Admin-ParitÃƒÂ¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÂ¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÂ¼ft: die deutlichste belastbare LÃƒÂ¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÂ¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÂ¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÂ¼cke als nÃƒÂ¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÂ¼r Admin/Vorstand, baut vollstÃƒÂ¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÂ¼r MAUI ergÃƒÂ¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÅ¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÂ¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Ãƒâ€žnderungsgrenze auch mobil sauber berÃƒÂ¼cksichtigt: eine E-Mail-Ãƒâ€žnderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÂ¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÂ¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÂ¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÂ¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÂ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÂ¼ft: offen war vor allem noch die Nutzer-ParitÃƒÂ¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÂ¼fpfad `Arbeitsstunden prÃƒÂ¼fen` bleiben dagegen weiterhin bewusst auÃƒÅ¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÂ¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÅ¸t sich die bisherige Home-/Pfad-LÃƒÂ¼cke gegenÃƒÂ¼ber MAUI.
- IrrefÃƒÂ¼hrende leere Bekanntmachungs-DetailflÃƒÂ¤chen entschÃƒÂ¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÂ¼fpfad fÃƒÂ¼r `Arbeitsstunden prÃƒÂ¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÂ¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÂ¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÂ¼f-/Bekanntmachungs-Backendpfad fÃƒÂ¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÂ¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÂ¼r WPF und MAUI.
- NÃƒÂ¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÂ¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÂ¼fstÃƒÂ¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÂ¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÅ“berblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÂ¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÂ¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÂ¶rige gemeinsame PrÃƒÂ¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÂ¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÂ¼ndelt jetzt den fachlichen Kern fÃƒÂ¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÂ¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÂ¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulauflistung wurde fÃƒÂ¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÂ¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÂ¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÂ¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 3: Parzellen-KernlÃƒÂ¼cken und mobile Admin-ParitÃƒÂ¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÂ¼ft: belastbar verfÃƒÂ¼gbar waren bereits aktive ZÃƒÂ¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÅ¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÂ¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÂ¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÂ¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÂ¤hler einbauen und ausbauen sowie das Ãƒâ€“ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÂ¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÂ¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÂ¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÂ¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÂ¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÂ¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÂ¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÂ¼ckgefÃƒÂ¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÅ“berschneidungen gegen den gewÃƒÂ¤hlten Starttag geprÃƒÂ¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÂ¤nzt: Mitglied zuordnen, Startdatum wÃƒÂ¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÂ¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÂ¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÂ¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÂ¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÂ¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÂ¤higkeit geprÃƒÂ¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÂ¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÂ¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÂ¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÂ¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÂ¶rigen Parzellen-Dateien durchgefÃƒÂ¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÅ¸erhalb des Commits.

## 2026-03-21 Ã¢â‚¬â€œ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÂ¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÂ¼ft und archivwÃƒÂ¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÂ¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÂ¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÂ¼berfÃƒÂ¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÂ¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÂ¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÂ¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÂ¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÂ¤ndert gelassen: keine Ãƒâ€žnderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÅ¸erhalb der aktiven LÃƒÂ¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÂ¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÂ¼ft: nur die tatsÃƒÂ¤chlich geÃƒÂ¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÂ¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÂ¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÂ¼r `Mailadresse ÃƒÂ¤ndern` in `MemberDetailViewModel` ergÃƒÂ¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÂ¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÂ¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÂ¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÅ¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÂ¼r diesen Teilblock vorbereitet: nur die tatsÃƒÂ¤chlich zugehÃƒÂ¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÅ¸en vor.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÂ¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÂ¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÂ¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÂ¤nde zurÃƒÂ¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÂ¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÂ¼ckkehrt.
- Login-/Recovery-ZustÃƒÂ¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÂ¤umt alte OTP-ZustÃƒÂ¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÂ¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÂ¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÂ¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÂ¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÂ¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÂ¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÂ¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÂ¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÂ¤rft: normales Login, OTP-PrÃƒÂ¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÂ¤nde gefÃƒÂ¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÂ¼hrung sauber: RÃƒÂ¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÂ¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÂ¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 Ã¢â‚¬â€œ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÂ¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÂ¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÂ¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÂ¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÂ¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 Ã¢â‚¬â€œ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÂ¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÂ¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÂ¤nzt: neues `README.md` als Startpunkt fÃƒÂ¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÂ¼ckgefÃƒÂ¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÂ¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÂ¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÂ¤digten/irrefÃƒÂ¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÂ¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÂ¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÂ¼berfÃƒÂ¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÂ¼fbaren Projekte ausgerichtet.

## 2026-03-21 Ã¢â‚¬â€œ Prompt 1/2: Home/Startseite Ã¢â‚¬â€œ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÂ¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÂ¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÂ¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÂ¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÂ¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÂ¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÂ¼r `admin` und `vorstand` sichtbar, ÃƒÂ¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÂ¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÂ¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÂ¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 Ã¢â‚¬â€œ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÂ¤nde fÃƒÂ¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÂ¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

---

## Workaround & Vorgehensweise bei Ãƒâ€žnderungen

- Ãƒâ€žnderungen nur nach genauer RÃƒÂ¼ckfrage und vollstÃƒÂ¤ndiger Ansicht der Datei vornehmen.  
- Ich mÃƒÂ¶chte immer genau wissen, **wo und wie** die Ãƒâ€žnderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor Ãƒâ€žnderungen vorgeschlagen werden.  

---

## 2026-03-23 Ã¢â‚¬â€œ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÂ¤chliche XAML-Ursache zurÃƒÂ¼ckgefÃƒÂ¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÂ¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÂ¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÂ¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÂ¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 Ã¢â‚¬â€œ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÂ¼ck-/Dirty-Check ergÃƒÂ¤nzt, Navigation auf Home-only zurÃƒÂ¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÂ¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÂ¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÂ¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÅ¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÂ¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÂ¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÂ¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÂ¼r geladene VerwaltungsdatensÃƒÂ¤tze sowie fÃƒÂ¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÂ¼ckkehr erfolgt ÃƒÂ¼ber den vorhandenen Navigationspfad zurÃƒÂ¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÂ¼ck`-Button
  - `ZurÃƒÂ¼ck` und `Abbrechen` prÃƒÂ¼fen auf echte ungespeicherte Ãƒâ€žnderungen
  - die RÃƒÂ¼ckfrage erscheint nur bei tatsÃƒÂ¤chlichen Ãƒâ€žnderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÂ¼nschten Produktpfad zurÃƒÂ¼ckgezogen:
  - `ArbeitseinsÃƒÂ¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÂ¼ber die vorhandenen Home-Buttons fÃƒÂ¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÂ¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÂ¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÂ¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÂ¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÂ¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÂ¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÂ¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÂ¼ber `status`; die globale PrÃƒÂ¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÂ¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÂ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÂ¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÂ¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÂ¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÂ¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÂ¤chlich geÃƒÂ¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÂ¤tze bleiben unverÃƒÂ¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÂ¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÂ¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÂ¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÂ¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÂ¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Ãƒâ€“ffnen der Freigabeansicht wird eine globale PrÃƒÂ¼fsperre ÃƒÂ¼ber die offenen `arbeitstunde`-DatensÃƒÂ¤tze gesetzt
  - andere PrÃƒÂ¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÂ¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÂ¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÂ¤ngert die Sperre wÃƒÂ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÂ¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÂ¼bernehmbar
- Die bestehende Badge-/ZÃƒÂ¤hlerlogik bleibt weiterverwendet: Ãƒâ€žnderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÂ¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÂ¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÂ¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÂ¼r offene FÃƒÂ¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÂ¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÂ¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÂ¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÂ¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÂ¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÂ¼r `Datum`, `Stunden` und `Art der Arbeit` Ã¢â‚¬â€œ ohne zusÃƒÂ¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÂ¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÂ¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÂ¼r Benutzer mit eigenem Mitgliedskontext; additionally erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÂ¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÂ¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÂ¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÂ¶ÃƒÅ¸er als `0` sein.
- FÃƒÂ¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÂ¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÂ¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÂ¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÂ¤hler zeigt weiter die tatsÃƒÂ¤chliche Anzahl offener PrÃƒÂ¼ffÃƒÂ¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÂ¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÂ¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÂ¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÂ¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÂ¤hrend im Repo noch die ÃƒÂ¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÂ¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÂ¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÂ¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÂ¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÂ¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÂ¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÅ¸lich fÃƒÂ¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÂ¼r spÃƒÂ¤tere MAUI-ParitÃƒÂ¤t anschlussfÃƒÂ¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÂ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÂ¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÂ¤hrend die eigentlichen EditoroberflÃƒÂ¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÂ¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÂ¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÂ¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ ArbeitseinsÃƒÂ¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÂ¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÂ¤tze-Verwaltung erneut geprÃƒÂ¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÂ¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÂ¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÂ¤nzt und nicht ÃƒÂ¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÂ¤nderte DatensÃƒÂ¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÂ¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÂ¶ÃƒÅ¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÂ¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÂ¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÂ¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÂ¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÂ¶ÃƒÅ¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÂ¤tze-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÂ¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÂ¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÂ¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÂ¤nzt und nicht ÃƒÂ¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÂ¤nderte DatensÃƒÂ¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÂ¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÂ¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÂ¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÂ¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÅ“berschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÂ¤nzt und parallel in einer kompakten Vorschau geprÃƒÂ¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÂ¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÂ¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÂ¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÂ¤umcheck bewusst klein gehalten: die ÃƒÂ¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÂ¤chsten AufrÃƒÂ¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÂ¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÂ¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÂ¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÂ¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÅ¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÂ¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÂ¤nderte DatensÃƒÂ¤tze direkt per Create/Update wieder nach `termin` zurÃƒÂ¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÂ¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÂ¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÂ¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÂ¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÂ¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÂ¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÂ¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÂ¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÂ¼r `Bekanntmachungen` und `ArbeitseinsÃƒÂ¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÂ¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÂ¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÂ¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÂ¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÅ“bersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÂ¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÂ¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÂ¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÂ¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÂ¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÂ¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÂ¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÂ¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÂ¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÂ¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÂ¼r Admin/Vorstand verfÃƒÂ¼gbar und blÃƒÂ¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÂ¼r spÃƒÂ¤tere MAUI-ParitÃƒÂ¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÂ¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÂ¤tigt: `ArbeitseinsÃƒÂ¤tze` und `Termine` laden ÃƒÂ¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÂ¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÂ¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÅ¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÂ¤tigte View.
- FÃƒÂ¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÂ¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÂ¼fung bleibt daher auf tatsÃƒÂ¤chliche RÃƒÂ¼ckgabefelder bzw. reale DatensÃƒÂ¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÂ¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÂ¤tzen, Terminen und Bekanntmachungen ÃƒÂ¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 Ã¢â‚¬â€œ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÂ¶ffnen in eigener View

- Den Home-Stand fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen auf den gewÃƒÂ¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÂ¼bersichten, wÃƒÂ¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÂ¼r eine eigenstÃƒÂ¤ndige WPF-Detailansicht fÃƒÂ¼r Home-Elemente ergÃƒÂ¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Ãƒâ€“ffnen-Kommandos fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÂ¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÂ¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÂ¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÂ¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÂ¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÂ¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÂ¼ft. Die entscheidende technische Ursache fÃƒÂ¼r den Pflichtstundenfehler lieÃƒÅ¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÂ¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÂ¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÂ¤hrend `ArbeitseinsÃƒÂ¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÂ¼r Home jetzt zunÃƒÂ¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÅ¸end robust clientseitig ÃƒÂ¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÂ¼r Pflichtstunden ergÃƒÂ¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÂ¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÂ¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÂ¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÂ¼ckgelieferte EintrÃƒÂ¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÂ¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÂ¤nzt: der Home-Pfad schreibt jetzt fÃƒÂ¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÂ¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÂ¼ber Hauptmitglied/Saison prÃƒÂ¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÂ¼ft: da `ArbeitseinsÃƒÂ¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen Ã¢â‚¬â€œ Pflichtstunden ÃƒÂ¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÂ¶sung und Bekanntmachungen ÃƒÂ¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÂ¼r Home deshalb final prÃƒÂ¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÂ¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÂ¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÂ¼bersicht ÃƒÂ¼ber dieses Hauptmitglied geladen.
- ZusÃƒÂ¤tzlich wird die bestÃƒÂ¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÂ¼cksichtigt, sondern zuerst direkt fÃƒÂ¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÂ¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÂ¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÂ¤tzlich mÃƒÂ¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÂ¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÂ¼r Titel, Inhalt und VerÃƒÂ¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÂ¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÂ¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÂ¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÂ¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÂ¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÂ¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÂ¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÂ¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÂ¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÂ¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÂ¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÂ¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÂ¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÂ¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÂ¤tzlich vereinfacht und nÃƒÂ¤her auf den bestÃƒÂ¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÂ¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÂ¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÂ¶rt Ã¢â‚¬â€œ im bestÃƒÂ¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Home-Diagnose: bestÃƒÂ¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÂ¤tigten DB-Schema-Stand geprÃƒÂ¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÂ¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÂ¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÂ¤tigten Kern geschÃƒÂ¤rft, ohne neue App-Logik einzufÃƒÂ¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÂ¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÂ¶st zuerst die aktuelle Saison ÃƒÂ¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÂ¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÂ¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÂ¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÂ¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÂ¼cke ÃƒÂ¼ber MAUI-PrÃƒÂ¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÂ¶glichen Abschlussoptionen geprÃƒÂ¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÂ¼ckgabe-/Kommentarpfad wÃƒÂ¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÂ¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÂ¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÂ¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÂ¤hlt und den vorhandenen mobilen PrÃƒÂ¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÂ¼fen` mobil jetzt nur noch dann an, wenn ÃƒÂ¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÂ¤chlich offene PrÃƒÂ¼ffÃƒÂ¤lle vorliegen; zusÃƒÂ¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÂ¤tze direkt im Titel des MenÃƒÂ¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÂ¼ber denselben gemeinsamen PrÃƒÂ¼fpfad, sodass MenÃƒÂ¼eintrag und tatsÃƒÂ¤chliche offene FÃƒÂ¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÂ¼hrt: die mobile Seite bleibt vollstÃƒÂ¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÂ¼fstatusanzeige drauÃƒÅ¸en, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÂ¼fflow in WPF bis zur Freigabe vervollstÃƒÂ¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÂ¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÂ¼ft: belastbar vorhanden waren bereits PrÃƒÂ¼fliste, Badge/ZÃƒÂ¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÂ¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÂ¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÂ¼ckgefÃƒÂ¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÂ¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÂ¼fmodus geÃƒÂ¶ffnet, gefiltert auf die tatsÃƒÂ¤chlich offenen DatensÃƒÂ¤tze des ausgewÃƒÂ¤hlten Mitglieds.
- DafÃƒÂ¼r nur einen kleinen Navigationskontext ergÃƒÂ¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÂ¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÂ¼feintrÃƒÂ¤ge des konkret ausgewÃƒÂ¤hlten Mitglieds im PrÃƒÂ¼fmodus erscheinen.
- Admin/Vorstand kÃƒÂ¶nnen im PrÃƒÂ¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÂ¤tzlich eine direkte `Freigeben`-Aktion fÃƒÂ¼r den ausgewÃƒÂ¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÂ¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÂ¼r einen sauberen RÃƒÂ¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÂ¼fliste und Badge/ZÃƒÂ¤hler weiterhin ÃƒÂ¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÂ¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÂ¼fmodus fachlich geschÃƒÂ¤rft: klarer Titel, Hinweistext, leerer PrÃƒÂ¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÂ¤nde fÃƒÂ¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÂ¼fliste und PrÃƒÂ¼fzÃƒÂ¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÂ¤ndigung des PrÃƒÂ¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÂ¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÂ¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation und den gemeinsamen PrÃƒÂ¼fpfad erneut geprÃƒÂ¼ft: ein belastbarer Listen-/Detailpfad fÃƒÂ¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÂ¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÂ¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÂ¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÂ¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÂ¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÂ¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÂ¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÂ¤ge, Vorstand/Admin kÃƒÂ¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÂ¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÂ¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÅ¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÂ¼fpfad fÃƒÂ¼r offene Arbeitsstunden ergÃƒÂ¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÂ¼hren von der PrÃƒÂ¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÂ¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÂ¼fen` erscheint fÃƒÂ¼r berechtigte Benutzer jetzt nur bei tatsÃƒÂ¤chlich offenen PrÃƒÂ¼ffÃƒÂ¤llen, zeigt einen kleinen ZÃƒÂ¤hler mit der Zahl offener DatensÃƒÂ¤tze und wird bei offenem PrÃƒÂ¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÂ¤uft nach Ãƒâ€žnderungen ÃƒÂ¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÂ¼flisten und PrÃƒÂ¼fzÃƒÂ¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÅ¸lich der gemeinsame operative PrÃƒÂ¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÂ¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÂ¤rten produktiven DB-Pfade geprÃƒÂ¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÂ¤hrend `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÂ¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÂ¤hen: `HomeOverviewDTO` trÃƒÂ¤gt jetzt zusÃƒÂ¤tzlich gemeinsame Startseiten-Daten fÃƒÂ¼r Pflichtstunden, ArbeitseinsÃƒÂ¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÂ¤dt diese Inhalte direkt aus den geklÃƒÂ¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÂ¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÂ¼r die Home-Ausgabe mitgefÃƒÂ¼hrt.
- Bereich `ArbeitseinsÃƒÂ¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÂ¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÂ¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÂ¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÂ¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÂ¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÂ¤sst sich wieder mit `SchlieÃƒÅ¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÂ¼r Bekanntmachungen angebunden: echte EintrÃƒÂ¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÂ¼gbaren Inhalte statt des bisherigen kÃƒÂ¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÂ¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÂ¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÂ¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÂ¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÂ¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÂ¼ft: konkret auffÃƒÂ¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÂ¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÂ¶ÃƒÅ¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÂ¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÂ¼ckgefÃƒÂ¼hrt: `MainWindowViewModel` lÃƒÂ¤dt fÃƒÂ¼r den Eigenkontext weiter den nÃƒÂ¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÂ¤ÃƒÅ¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÂ¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÂ¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÂ¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÂ¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÂ¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÂ¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÅ¸en`-Button ergÃƒÂ¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÂ¼r `ArbeitseinsÃƒÂ¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÂ¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÂ¼ft und bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÂ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÂ¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÂ¼fpfad fÃƒÂ¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÂ¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÂ¤tzliche Infrastruktur direkt testbar.
- DafÃƒÂ¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÂ¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÂ¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÂ¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÂ¤chsten PrÃƒÂ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÂ¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÂ¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÂ¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÂ¤chsten kleinen PrÃƒÂ¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÂ¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÂ¼r Desktop und Mobil und lÃƒÂ¤sst sich ohne zusÃƒÂ¤tzliche Testinfrastruktur direkt prÃƒÂ¼fen.
- DafÃƒÂ¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÂ¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÂ¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÂ¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÂ¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÂ¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÂ¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÂ¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÂ¼ckgezogen, sondern sinnvoll zusammengefÃƒÂ¼hrt: die aktive Root-Projektdatei ÃƒÂ¼bernimmt jetzt wieder die fÃƒÂ¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÂ¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÂ¤ndig erhalten bleibt.
- Fachlich passende ÃƒÂ¤ltere Tests sauber zurÃƒÂ¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÂ¼fpfad fÃƒÂ¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÅ¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÂ¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÂ¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÂ¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÂ¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÂ¤hig.

## 2026-03-22 Ã¢â‚¬â€œ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÂ¼r Demo-/Testdatenfilter zurÃƒÂ¼ckgeholt

- Die aktuelle Testlage geprÃƒÂ¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÂ¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÂ¤ltere Testidee fÃƒÂ¼r `ExportViewModel`, die fÃƒÂ¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÂ¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÂ¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÂ¼fung) ohne dass dafÃƒÂ¼r eine groÃƒÅ¸e Testinfrastruktur nÃƒÂ¶tig wÃƒÂ¤re.
- DafÃƒÂ¼r eine kleine aktive Testbasis im Root zurÃƒÂ¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÂ¼ft zurÃƒÂ¼ckzuziehen.
- Den gewÃƒÂ¤hlten PrÃƒÂ¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÂ¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÂ¼ckgezogen: sie waren fÃƒÂ¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÂ¤tten den Wiedereinstieg unnÃƒÂ¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÂ¤tzlich lÃƒÂ¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÂ¼nen Tests.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÂ¤t mit Arbeitsstunden-PrÃƒÂ¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÂ¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÂ¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÂ¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÂ¤tslÃƒÂ¼cke fÃƒÂ¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÂ¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÂ¼hrt: die Umsetzung bleibt vollstÃƒÂ¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÂ¼r offene Arbeitsstunden berÃƒÂ¼cksichtigt nur operative Mitglieder ÃƒÂ¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÂ¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÂ¼r Arbeitsstunden, keine zusÃƒÂ¤tzlichen WPF-OberflÃƒÂ¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 2: NÃƒÂ¤chste mobile Admin-ParitÃƒÂ¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÂ¼cken erneut gegen die produktiven WPF-Wege geprÃƒÂ¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÂ¤chste kleine, aber fachlich wichtige ParitÃƒÂ¤tslÃƒÂ¼cke, weil WPF dafÃƒÂ¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÂ¤tslÃƒÂ¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÂ¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÂ¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÂ¼r RollenÃƒÂ¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÂ¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÂ¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÂ¤chliche Mitgliedsrolle vor einem evtl. ÃƒÂ¤lteren `app_user`-Wert, damit RollenÃƒÂ¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÅ¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÂ¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÂ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 5 Prompt 1: NÃƒÂ¤chste mobile Admin-ParitÃƒÂ¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÂ¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÂ¼ft: die deutlichste belastbare LÃƒÂ¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÂ¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÂ¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÂ¼cke als nÃƒÂ¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÂ¼r Admin/Vorstand, baut vollstÃƒÂ¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÂ¼r MAUI ergÃƒÂ¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÅ¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÂ¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Ãƒâ€žnderungsgrenze auch mobil sauber berÃƒÂ¼cksichtigt: eine E-Mail-Ãƒâ€žnderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÂ¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÂ¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÂ¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÂ¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÂ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÂ¼ft: offen war vor allem noch die Nutzer-ParitÃƒÂ¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÂ¼fpfad `Arbeitsstunden prÃƒÂ¼fen` bleiben dagegen weiterhin bewusst auÃƒÅ¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÂ¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÅ¸t sich die bisherige Home-/Pfad-LÃƒÂ¼cke gegenÃƒÂ¼ber MAUI.
- IrrefÃƒÂ¼hrende leere Bekanntmachungs-DetailflÃƒÂ¤chen entschÃƒÂ¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÂ¼fpfad fÃƒÂ¼r `Arbeitsstunden prÃƒÂ¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÂ¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÂ¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÂ¼f-/Bekanntmachungs-Backendpfad fÃƒÂ¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÂ¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÂ¼r WPF und MAUI.
- NÃƒÂ¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÂ¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÂ¼fstÃƒÂ¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÂ¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÅ“berblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÂ¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÂ¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÂ¶rige gemeinsame PrÃƒÂ¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÂ¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÂ¼ndelt jetzt den fachlichen Kern fÃƒÂ¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÂ¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÂ¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulaufl listing wurde fÃƒÂ¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÂ¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÂ¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÂ¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 3: Parzellen-KernlÃƒÂ¼cken und mobile Admin-ParitÃƒÂ¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÂ¼ft: belastbar verfÃƒÂ¼gbar waren bereits aktive ZÃƒÂ¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÅ¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÂ¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÂ¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÂ¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÂ¤hler einbauen und ausbauen sowie das Ãƒâ€“ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÂ¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÂ¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÂ¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÂ¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÂ¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÂ¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÂ¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÂ¼ckgefÃƒÂ¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÅ“berschneidungen gegen den gewÃƒÂ¤hlten Starttag geprÃƒÂ¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÂ¤nzt: Mitglied zuordnen, Startdatum wÃƒÂ¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÂ¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÂ¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÂ¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÂ¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÂ¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 Ã¢â‚¬â€œ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÂ¤higkeit geprÃƒÂ¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÂ¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÂ¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÂ¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÂ¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÂ¶rigen Parzellen-Dateien durchgefÃƒÂ¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÅ¸erhalb des Commits.

## 2026-03-21 Ã¢â‚¬â€œ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÂ¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÂ¼ft und archivwÃƒÂ¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÂ¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÂ¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÂ¼berfÃƒÂ¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÂ¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÂ¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÂ¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÂ¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÂ¤ndert gelassen: keine Ãƒâ€žnderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÅ¸erhalb der aktiven LÃƒÂ¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÂ¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÂ¼ft: nur die tatsÃƒÂ¤chlich geÃƒÂ¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÂ¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÂ¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÂ¼r `Mailadresse ÃƒÂ¤ndern` in `MemberDetailViewModel` ergÃƒÂ¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÂ¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÂ¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÂ¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÅ¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÂ¼r diesen Teilblock vorbereitet: nur die tatsÃƒÂ¤chlich zugehÃƒÂ¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÅ¸en vor.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÂ¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÂ¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÂ¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÂ¤nde zurÃƒÂ¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÂ¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÂ¼ckkehrt.
- Login-/Recovery-ZustÃƒÂ¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÂ¤umt alte OTP-ZustÃƒÂ¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÂ¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÂ¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÂ¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 Ã¢â‚¬â€œ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÂ¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÂ¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÂ¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÂ¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÂ¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÂ¤rft: normales Login, OTP-PrÃƒÂ¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÂ¤nde gefÃƒÂ¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÂ¼hrung sauber: RÃƒÂ¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÂ¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÂ¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 Ã¢â‚¬â€œ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÂ¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÂ¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÂ¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÂ¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÂ¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 Ã¢â‚¬â€œ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÂ¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÂ¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÂ¤nzt: neues `README.md` als Startpunkt fÃƒÂ¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÂ¼ckgefÃƒÂ¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÂ¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÂ¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÂ¤digten/irrefÃƒÂ¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÂ¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÂ¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÂ¼berfÃƒÂ¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÂ¼fbaren Projekte ausgerichtet.

## 2026-03-21 Ã¢â‚¬â€œ Prompt 1/2: Home/Startseite Ã¢â‚¬â€œ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÂ¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÂ¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÂ¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÂ¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÂ¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÂ¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÂ¼r `admin` und `vorstand` sichtbar, ÃƒÂ¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÂ¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÂ¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÂ¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 Ã¢â‚¬â€œ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÂ¤nde fÃƒÂ¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÂ¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
