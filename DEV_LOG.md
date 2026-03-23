# KGV Entwicklungslog

---

## 2026-03-24 – Veralteten Remote-Snapshot um fachliche QR-Reste bereinigt

- Den aktuellen Repo-Stand zuerst geprüft: fachliche QR-Reste existierten nach der Live-Bereinigung nur noch im veralteten Snapshot `supabase/migrations/20260323093513_remote_schema.sql`.
- Gegen den bereits bereinigten Istzustand aus `_AI_DB_EXPORT/database.types.ts` abgeglichen: `public.parzelle` enthält dort keine QR-Spalten mehr, sondern nur noch die RFID-Felder.
- Den veralteten Remote-Snapshot deshalb auf den aktuellen fachlichen Stand gezogen:
  - `qr_code_wasser` aus der `parzelle`-Tabellendefinition entfernt
  - `qr_code_strom` aus der `parzelle`-Tabellendefinition entfernt
  - die veralteten Unique-Constraints `parzelle_qr_code_wasser_key` und `parzelle_qr_code_strom_key` entfernt
- Ergebnis: im Repo verbleiben in diesem Snapshot keine fachlichen QR-Reste mehr; der Snapshot passt damit wieder zum bereinigten Live-/Types-Stand.

## 2026-03-24 – WPF Button-Höhen projektweit auf kleine feste Fälle geprüft und angehoben

- Nach dem Fix in der Parzellenzuordnung wurden die WPF-Buttons mit kleinen festen Höhen projektweit geprüft.
- Alle expliziten WPF-Buttonhöhen unter `35` wurden auf `35` angehoben, damit Beschriftungen nicht mehr zu knapp sitzen.
- Konkret angepasst wurden die betroffenen Buttons in:
  - `GartenStromView`
  - `GartenWasserView`
  - `ArbeitsstundenView`
  - `ChangeEmailWindow`
  - `ResetPasswordWindow`
- Der Fix bleibt rein visuell; Fachlogik und Command-Pfade wurden nicht verändert.

## 2026-03-24 – WPF `MemberDetailView`: Parzellenzuordnungs-Zeile leicht erhöht

- Den aktuellen Arbeitsbaum vor dem UI-Fix geprüft; blockfremde lokale Änderungen wie `KGV.Wpf/Views/LoginWindow.xaml` und `KGV.Wpf/Views/MemberDetailView.xaml.cs` bleiben weiter unberührt.
- Die zu kleine Höhe lag nicht in der `MemberDetailView` selbst, sondern in der ausgelagerten `MemberParzellenSection` bei der Zeile für freie Parzelle, Startdatum und die beiden Aktionsbuttons.
- Für den kleinen WPF-UI-Fix wurde nur diese Zeile leicht erhöht:
  - Zeilen-`StackPanel` mit `MinHeight="36"`
  - Button-Höhen von `30` auf `35` erhöht
- Damit passt die Beschriftung wieder sauber in die Buttons, ohne die restliche Detailansicht oder Fachlogik zu verändern.

## 2026-03-24 – Appuser-Verwaltung ins `Admin-Menü` verschoben und an das ausgewählte Mitglied gebunden

- Den Istzustand zuerst im Repo geprüft: `Benutzerverwaltung` hing in WPF noch als globale Navigation, obwohl die fachlichen Appuser-Aktionen immer auf ein konkretes Mitglied zielen sollen. Gleichzeitig war die produktive Nutzeraktion `InviteUserAsync(...)` bereits vorhanden, ein sauberer Entfernen-Pfad aber nicht als eigene UI-gebundene Aktion organisiert.
- Die Menüstruktur wurde deshalb neu geordnet:
  - globaler Top-Level-Eintrag `Benutzerverwaltung` in WPF entfernt
  - stattdessen `Benutzerverwaltung` als eingerückter Unterpunkt im Mitgliedskontext unter `Admin-Menü`
  - damit bleibt die Navigation eindeutig und es gibt keine Doppelnavigation alt/neu
- Die WPF-`Benutzerverwaltung` arbeitet jetzt strikt auf dem zuvor ausgewählten Mitglied:
  - der Unterpunkt bekommt den aktuellen `SelectedMember` als Parameter
  - beim Laden wird nur noch die Appuser-Zuordnung dieses Mitglieds geladen
  - falls noch kein Appuser existiert, wird ein Platzhalter aus dem ausgewählten Mitglied aufgebaut, damit `Nutzer hinzufügen` trotzdem sauber auf genau diesen Datensatz arbeitet
- Die Aktionen wurden fachlich passend umgestellt:
  - `Nutzer hinzufügen` nutzt weiter den bestehenden produktiven Pfad `InviteUserAsync(...)`
  - `Nutzer entfernen` entfernt die Appuser-Zuordnung des ausgewählten Mitglieds, nicht das Mitglied selbst
  - ohne Mitgliedskontext laufen die Aktionen nicht und liefern eine klare fachliche Rückmeldung statt technischer Fehler
- Rechte wurden sauber getrennt:
  - `Benutzerverwaltung` ist in WPF nur noch im Admin-Mitgliedskontext sichtbar
  - in MAUI wurde die Menü-Sichtbarkeit ebenfalls auf echte Admins beschränkt; Vorstand sieht den Punkt dort ebenfalls nicht
- Technisch verifiziert: `KGV.Wpf` baut nach der Umordnung erfolgreich; der MAUI-Pfad wurde im Menü-/Rechteverhalten mitgezogen und nicht beschädigt.

## 2026-03-24 – Mitgliedersuche um E-Mail, Gartennummern und Hauptmitglied-Markierung erweitert

- Den Istzustand zuerst im Repo geprüft: die bestehende Mitgliedersuche zeigte bisher in WPF nur Name/Vorname und in MAUI nur Titel/Unterzeile. Ein neuer Suchdialog oder neue Fachlogik waren dafür nicht nötig.
- Die Erweiterung bleibt im bestehenden Suchpfad und nutzt nur bereits vorhandene Datenquellen:
  - Mitgliedsdaten aus `GetMitgliederAsync()`
  - Parzellen aus `GetAllParzellenAsync()`
  - aktuelle Belegungen aus `GetAllParzellenBelegungenAsync()`
- Daraus wird jetzt für die Ergebnisliste angereichert:
  - E-Mailadresse
  - Gartennummern aus aktuell aktiven Belegungen, falls vorhanden
  - Hauptmitglied-Markierung aus `hauptmitglied_id`
- WPF wurde direkt in der bestehenden GridView erweitert um die zusätzlichen Spalten `E-Mail`, `Gartennummern` und `Hauptmitglied` als deaktivierte Checkbox.
- MAUI wurde passend mitgedacht und die bestehende Suchliste ebenfalls um Gartennummern und Hauptmitglied-Markierung ergänzt; die E-Mail bleibt dort Teil der sichtbaren Ergebnisinfos. Es wurde kein zweiter Suchpfad aufgebaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Erweiterung erfolgreich.

## 2026-03-24 – Arbeitseinsatz-Teilnehmerliste: `Abmelden` pro Zeile produktiv über echten DB-Pfad ergänzt

- Den Istzustand zuerst gegen Repo und `_AI_DB_EXPORT` geprüft. `database.types.ts` bestätigt für diesen Block ausdrücklich neben `sign_up_for_arbeitseinsatz(...)` auch `sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` als echten DB-Funktionspfad; `roles.sql` enthält dafür keine abweichende App-Sonderregel, `AI_DATABASE_CONTEXT.sql` liefert keinen alternativen Pfad.
- `sign_off_from_arbeitseinsatz(...)` war App-seitig vor diesem Block noch nicht produktiv verwendet. Der neue Abmeldepfad wurde deshalb sauber in den Shared-Service ergänzt statt als direkter Tabellenhack daneben aufgebaut.
- In der Arbeitseinsatz-Detailview für Admin/Vorstand wurde pro Teilnehmerzeile der Button `Abmelden` ergänzt. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch Zeilenaktion.
- Der Button läuft mit kleiner Rückfrage und ruft danach produktiv `SignOffFromArbeitseinsatzAsync(...)` auf; dieser Shared-Service nutzt den echten RPC-/DB-Funktionspfad `sign_off_from_arbeitseinsatz(...)`.
- Nach erfolgreicher oder fachlich abgefangener Rückmeldung werden Teilnehmerliste und Detailzustand direkt neu geladen; damit aktualisieren sich auch Kapazitäts-/Anmeldeinformationen über den bestehenden Detailpfad mit.
- Technisch verifiziert: `KGV.Wpf` baut nach dem neuen Abmeldepfad erfolgreich; MAUI wurde im Shared-Servicepfad mitgedacht und nicht beschädigt.

## 2026-03-24 – Arbeitseinsatz-Block final verifiziert und sauber abgeschlossen

- Den aktuellen Arbeitsbaum erneut geprüft: blockeigene Codeänderungen aus diesem Arbeitseinsatz-Block waren bereits im Repo enthalten; lokal offen blieben nur blockfremde Änderungen und Artefakte wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Hilfs-/Exportdateien.
- Den bereits implementierten Zustand nochmals final verifiziert:
  - `Anmelden` bleibt in Home und Detail am selben echten Pfad `SignUpForArbeitseinsatzAsync(...)`
  - der Shared-Service nutzt weiter produktiv `sign_up_for_arbeitseinsatz(...)`
  - Teilnehmerliste erscheint nur für Admin/Vorstand in der Detailview
  - `Hinzufügen` erscheint nur für Admin/Vorstand
  - `Hinzufügen` verwendet die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewählte bestehende Mitglied wird über denselben RPC-Pfad eingetragen wie bei Selbstanmeldung
- Die Sichtbarkeit von `Anmelden` wird weiterhin belastbar aus realem Zustand bestimmt: Basisdaten aus `arbeitseinsatz` plus aktive `arbeitseinsatz_anmeldung` gegen Frist, Platzgrenze und bereits vorhandene Anmeldung des aktuellen Mitglieds.
- `KGV.Wpf` wurde final erfolgreich gebaut. Ein erster Buildlauf scheiterte nur an einer laufenden gesperrten `KGV.Wpf`-Instanz; nach Beenden der App lief der Build sauber durch. Der Block ist damit technisch und fachlich abgeschlossen.

## 2026-03-24 – Arbeitseinsatz: `Anmelden`-Regression und Admin-/Vorstand-Teilnehmerblock sauber abgeschlossen

- Den begonnenen Block erneut gegen den realen Arbeitsbaum und ausdrücklich gegen `_AI_DB_EXPORT` geprüft. Belastbar herangezogen wurden wieder `database.types.ts` mit `sign_up_for_arbeitseinsatz(...)` und `arbeitseinsatz_anmeldung`; `roles.sql` enthält dafür keine abweichende Zusatzregel, `AI_DATABASE_CONTEXT.sql` liefert hier keinen zusätzlichen App-Pfad.
- Die Regression beim verschwundenen `Anmelden` lag nicht am RPC selbst, sondern an der Sichtbarkeit: `CanRegister` wurde im sichtbaren UI zu eng nur aus dem Startseitenpfad übernommen. Der Shared-Service bestimmt die sichtbare Anmeldung jetzt wieder aus den realen Basisdaten von `arbeitseinsatz` plus aktiven `arbeitseinsatz_anmeldung`-Datensätzen für den aktuellen Benutzerkontext, inklusive Frist, Platzgrenze und bestehender Anmeldung.
- Home und Detail bleiben auf demselben echten Produktpfad: beide rufen weiterhin `SignUpForArbeitseinsatzAsync(...)` auf, das produktiv über `sign_up_for_arbeitseinsatz(...)` schreibt. Es wurde keine zweite Schreiblogik eröffnet.
- Für Admin/Vorstand wurde die Detailview klein und rollenabhängig ergänzt:
  - reale Teilnehmerliste über aktive `arbeitseinsatz_anmeldung`
  - `Hinzufügen`-Button nur für Admin/Vorstand
  - Auswahl ausschließlich über die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewählte bestehende Mitglied wird danach über denselben RPC-Pfad angemeldet wie bei Selbstanmeldung
- Dabei gibt es bewusst keine freie Texteingabe und keine Prüfung, ob das gewählte Mitglied App-User ist. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch `Hinzufügen`.
- Technisch finalisiert: kleine Warnungsbereinigung im Auswahlpfad abgeschlossen und `KGV.Wpf` baut erfolgreich; der Block ist damit jetzt sauber abschließbar.

## 2026-03-24 – `Anmelden` für Arbeitseinsatz im sichtbaren UI tatsächlich funktionsfähig gemacht

- Den realen Fehler ausdrücklich am laufenden WPF-Pfad geprüft: Home-Button und Detail-Button waren bereits an denselben Shared-Servicepfad gebunden, der sichtbare Hänger lag aber davor im übergebenen Datensatz.
- Die konkrete Ursache: `MapHomeWorkAssignment(...)` übernahm die `id` des Arbeitseinsatzes nicht in das sichtbare `HomeWorkAssignmentItem`. Dadurch lief die Detailansicht mit `WorkAssignmentId = 0` in einen stillen Vorab-Abbruch und auch der Home-Pfad arbeitete nicht auf einem belastbaren Datensatzschlüssel.
- Der Fix bleibt klein und ehrlich:
  - `SupabaseService` schreibt `record.Id` jetzt in das sichtbare Home-Item
  - `HomeViewModel` übergibt den Detailbutton nur noch dann als sichtbar, wenn `CanRegister` tatsächlich gilt
  - `HomeView.xaml` zeigt den Home-Button ebenfalls nur bei `CanRegister`
  - `HomeSectionDetailViewModel` meldet einen ungültigen Detailkontext jetzt sichtbar statt still zu returnen
- Ergebnis: Home und Detail nutzen weiterhin denselben RPC-Servicepfad, reagieren jetzt aber auch im sichtbaren UI belastbar statt mit stillem No-Op. Der Admin-/Vorstand-Teilnehmerblock blieb bewusst unberührt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten UI-/Datensatzfix erfolgreich; MAUI wurde nicht beschädigt.

## 2026-03-24 – RPC-Anmeldeblock für Arbeitseinsatz final verifiziert, committed und gepusht

- Den bereits umgesetzten RPC-Pfad für `Anmelden` nochmals final gegen den realen Arbeitsbaum geprüft und `KGV.Wpf` erfolgreich gebaut.
- Für den Abschluss wurden ausschließlich die zu diesem Block gehörenden Dateien gestaged; blockfremde lokale Änderungen wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Artefakte blieben unberührt.
- Kurz funktional verifiziert per Codepfad: Home und Detailview rufen denselben Shared-Service `SignUpForArbeitseinsatzAsync(...)` auf; Doppelanmeldung, Frist und Platzgrenze werden vor dem RPC geprüft und zusätzlich im RPC-Fehlerfall sauber in verständliche Rückmeldungen übersetzt.
- Der Block ist damit sauber als eigener Commit abgeschlossen und direkt gepusht.

## 2026-03-24 – `Anmelden` für Arbeitseinsatz produktiv über den echten DB-Funktionspfad angebunden

- Den Block ausdrücklich gegen den referenzierten DB-Export `_AI_DB_EXPORT` geführt. Belastbar genutzt wurden vor allem `database.types.ts` sowie die dort bestätigten Funktionen `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` und `sign_off_from_arbeitseinsatz(...)`, außerdem die Tabelle `arbeitseinsatz_anmeldung` und das Enum `arbeitseinsatz_anmeldung_status`. `roles.sql` wurde als Zusatzkontext geprüft; dort liegen keine abweichenden App-seitigen Sonderregeln für diesen Block.
- Daraus den fachlich richtigen Schreibpfad abgeleitet: die App schreibt die Anmeldung nicht als Schatteninsert direkt in `arbeitseinsatz_anmeldung`, sondern nutzt produktiv die vorhandene DB-Funktion `sign_up_for_arbeitseinsatz(...)`. Der direkte Tabellenzugriff bleibt nur lesend für Vorabprüfungen und Status-/Kapazitätsbewertung.
- Im Shared-Service wurde dafür ein echter Produktpfad ergänzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` prüft zuerst sauber Mitglied, bestehenden Datensatz, aktive Anmeldungen, Frist und Platzgrenze und ruft dann den bestätigten RPC-Pfad auf.
- Doppelanmeldungen werden dabei zweistufig verhindert: vor dem RPC über eine gezielte Lesesicht auf `arbeitseinsatz_anmeldung` mit `status = angemeldet`, zusätzlich über den DB-Funktionspfad selbst. Falls der RPC in einem Rennen dennoch in einen bereits angemeldeten Zustand läuft, wird das klein abgefangen und als verständliche Rückmeldung ausgegeben statt als technischer Rohfehler.
- Home und Detailview verwenden jetzt denselben echten Servicepfad statt doppelter ViewModel-Logik:
  - Home nutzt weiter `RegisterForWorkAssignmentCommand`, ruft aber jetzt den Shared-Service auf
  - die Detailansicht nutzt denselben Servicepfad über den im Kontext mitgegebenen `WorkAssignmentId`
  - beide erhalten dieselbe verständliche Erfolgs-/Fehlerrückmeldung
- Der Mitgliedsbezug bleibt beim real vorhandenen Benutzerpfad: bevorzugt wird `UserContext.MitgliedId`, mit kleinem Fallback über `EnsureCurrentMemberSelectedAsync()`; es wurde keine neue Sonderzuordnung eingeführt.
- Nach erfolgreicher Anmeldung wird der sichtbare Zustand klein aktualisiert: der betroffene Home-Eintrag bzw. der Detailkontext erhält den aktualisierten Startseiten-Datensatz zurück und der Button wird ehrlich deaktiviert, damit keine zweite sofortige Anmeldung ausgelöst wird. Eine Abmeldung oder Warteliste wurde bewusst nicht neu eröffnet.
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde im Shared-Core-/Servicepfad mitgedacht und nicht beschädigt.

## 2026-03-24 – Home-Zeitwerte für Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Sichtbarkeitsfehler nach dem expliziten WPF-Binding nochmals gegen den kompletten sichtbaren Pfad geprüft. Ergebnis: die WPF-Views waren bereits korrekt auf Start-/Endzeit gebunden; der Restfehler saß davor im Home-Lesepfad, weil `v_startseite_arbeitseinsatz` bzw. `v_startseite_termine` die Zeitfelder im aktuellen Stand nicht in jedem Fall belastbar lieferten.
- Der finale Fix bleibt deshalb klein und produktnah im bestehenden Home-Servicepfad: `LoadStartseiteArbeitseinsaetzeAsync()` und `LoadStartseiteTermineAsync()` laden die Startseiten-Records weiter aus den bestehenden Views, reichern fehlende `Beginn`-/`Ende`-Werte bei Bedarf aber gezielt aus den Basistabellen `arbeitseinsatz` bzw. `termin` anhand der bereits vorhandenen Datensatz-`Id` an.
- Damit ist jetzt belastbar abgesichert: `start_uhrzeit` und `end_uhrzeit` kommen nicht nur in der Basistabelle vor, sondern erreichen den tatsächlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lässt. Die sichtbare Bindung in Home und Detail kann damit auf denselben expliziten Start-/Endfeldern bleiben.
- Keine neue Fachlogik, keine neue Navigation und keine Home-Schattenarchitektur: es bleibt beim vorhandenen Home-Lesepfad plus kleiner Fallback-Anreicherung nur für fehlende Zeitwerte.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-Ergänzung nicht beschädigt.

## 2026-03-24 – Start- und Endzeit im tatsächlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler nochmals Ende-zu-Ende gegen den tatsächlichen WPF-Pfad geprüft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml`. Ergebnis: `start_uhrzeit`/`end_uhrzeit` kamen im Mapping als normalisierte Werte an, hingen im sichtbaren UI aber weiterhin zu stark an einem zusammengesetzten Zeittext bzw. indirekten Anzeigeweg.
- Die endgültige Korrektur setzt deshalb nicht mehr nur auf einen abstrakten Sammelstring, sondern auf explizite Start-/Endzeit-Properties im tatsächlich sichtbaren Pfad: `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `StartTimeText` und `EndTimeText`; der Detailkontext übernimmt dieselben Felder 1:1 für genau den ausgewählten Datensatz.
- In `SupabaseService` werden die vorhandenen `Beginn`-/`Ende`-Werte weiterhin zentral über `NormalizeTimeValue(...)` normalisiert und jetzt explizit in diese Start-/Endfelder geschrieben. Damit ist belastbar nachvollziehbar: die Zeitwerte kommen im Modell an und werden nicht mehr erst spät oder indirekt aus Nebenfeldern zusammengesetzt.
- Der sichtbare WPF-Pfad wurde anschließend passend darauf verdrahtet: auf Home bleibt die Zeitzeile als klarer Zeitraum aus genau dieser Quelle sichtbar; in der Detailansicht werden `Beginn` und `Ende` jetzt explizit als eigene Datensatzangaben gerendert statt nur als zusammengesetzte Sammelzeile.
- `Bekanntmachung` bleibt unverändert stabil: dort werden die neuen Zeitfelder leer übergeben, sodass keine Regression in der generischen Detailstruktur entsteht.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/Kontextergänzungen nicht beschädigt.

## 2026-03-24 – Home-/Detailanzeige für Arbeitseinsatz und Termin korrigiert: Uhrzeit explizit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den aktuellen Istzustand des bestehenden Anzeige-/Mappingpfads erneut gegen `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und `SupabaseService` geprüft. Ergebnis: die Uhrzeit lief bislang teils nur indirekt im `Subtitle` bzw. als separater Beginn-Text, während `Arbeitseinsatz`-Kapazitätsinfos zugleich in `DetailInfo` und `RegistrationInfo` landeten.
- Die Home-Anzeige wurde deshalb auf einen eindeutigen Sichtpfad umgestellt: `Arbeitseinsatz` und `Termin` erhalten jetzt jeweils einen expliziten `TimeText`, der über `BuildTimeRange(...)` aus Start- und Endzeit erzeugt und in Home sichtbar als `Uhrzeit` gerendert wird. Damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, wird konsistent `Start – Ende` angezeigt.
- Die Detailview nutzt denselben expliziten Zeitpfad jetzt ebenfalls für genau den ausgewählten Datensatz: `HomeSectionDetailContext` trägt `TimeText`, `HomeSectionDetailViewModel` reicht ihn 1:1 weiter, und `HomeSectionDetailView.xaml` zeigt ihn separat als `Uhrzeit` an. Dadurch werden Start- und Endzeit für `Arbeitseinsatz` und `Termin` sichtbar, ohne neue Listendarstellung oder Sammelkontexte einzuführen.
- Die doppelte Anzeige `Angemeldet: 0` kam aus zwei Quellen desselben Datensatzes: einmal aus `DetailInfo` (`Angemeldet`, `Freie Plätze`) und zusätzlich nochmals aus `RegistrationInfo` über `BuildCapacityText(...)`. Der Mappingpfad für `Arbeitseinsatz` wurde deshalb bereinigt: Kapazitätsinfos bleiben jetzt nur noch in `RegistrationInfo`; aus `DetailInfo` wurden die redundanten `Angemeldet`-/`Freie Plätze`-Zeilen entfernt.
- `DetailInfo` bleibt damit auf die übrigen Felder des ausgewählten Datensatzes fokussiert (`Thema`, `Datum`, `Treffpunkt`, ggf. `Max. Teilnehmer`), während Registrierungs-/Kapazitätsinfos nur noch einmal sichtbar sind. So bleibt die Detailview weiter auf genau einen Datensatz begrenzt, aber ohne redundante Anzeige-Fragmente.
- Die generische Detailstruktur für `Bekanntmachung` wurde mitgeprüft und nicht verschlechtert: dort bleibt `TimeText` leer, während `Subtitle`, `AdditionalInfo` und `Content` unverändert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fix erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschädigt.

## 2026-03-24 – Home-/Detailansicht für Arbeitseinsätze und Termine vervollständigt: Startzeit sichtbar, Detailinfos vollständig, Detailkontext bleibt beim ausgewählten Datensatz

- Den bestehenden Home-/Detailpfad vor dem Fix gegen die realen WPF-Dateien und das aktuelle Mapping geprüft: `HomeView`, `HomeViewModel`, `HomeSectionDetailView`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Items sowie das Mapping in `SupabaseService` waren bereits generisch vorhanden; `Arbeitseinsatz` zeigte die Startzeit auf Home schon separat, `Termin` dagegen noch nicht.
- Die Home-Übersicht wurde deshalb gezielt vervollständigt, ohne neue Sondernavigation zu bauen: `Termin` zeigt die Startzeit jetzt wie `Arbeitseinsatz` zusätzlich sichtbar in der Karte an. Für `Arbeitseinsatz` blieb die vorhandene konsistente Darstellung mit sichtbarem Beginn erhalten.
- Die Detailview wurde innerhalb der bestehenden Struktur ergänzt statt neu aufgebaut: zusätzlich zu `Title`, `Subtitle`, `AdditionalInfo` und `Content` zeigt sie jetzt auch die bereits aus dem gewählten Datensatz stammende Registrierungsinformation an. Dadurch bleiben für `Arbeitseinsatz` und `Termin` die restlichen zum Datensatz gehörenden Angaben sichtbar, ohne Felder aus anderen Einträgen zu vermischen.
- Den Punkt „nur ausgewählten Datensatz anzeigen“ explizit auf den bestehenden Kontextpfad abgesichert: `HomeViewModel` übergibt an die Detailansicht weiterhin nur skalare Kontextdaten des konkret angeklickten Eintrags (`Title`, `Subtitle`, `Content`, `AdditionalInfo`, Registrierungsinfo/Anmeldeaktion). Es wird keine Liste und kein unscharfer Sammelkontext an die Detailview gereicht.
- Der `Anmelden`-Button ist in der Detailview jetzt konsistent für `Arbeitseinsatz` sichtbar und nutzt denselben ehrlichen Pfad wie auf Home: derselbe Hinweisdialog, keine neue Fake-Fachlogik und weiterhin kein erfundener Schreibzugriff, solange der echte belastbare Anmeldepfad im Repo fehlt.
- `Bekanntmachung` wurde im selben generischen Detailpfad mitgeprüft: die bestehende Darstellung über `Title`, `Subtitle`, `AdditionalInfo` und `Content` bleibt erhalten; es geht dort kein Feld verloren und es wurde keine kaputte Spezialdarstellung eingeführt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die gezielten Shared-/Kontextanpassungen nicht beschädigt.

## 2026-03-24 – Arbeitsstunden endgültig gegen `id = 0` abgesichert, Save-Rücknavigation nachgezogen, Arbeitseinsatz-Home-Buttons korrigiert

- Den erneuten realen Fehlernachweis `arbeitsstunde.id = 0` nach dem vorigen Fix ausdrücklich als Beleg genommen, dass Attributverhalten allein im laufenden Pfad nicht reicht. Deshalb den kompletten Produktpfad nochmals bis zur tatsächlichen Create-Payload geprüft: WPF-Erfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogpfad (`ArbeitsstundenViewModel`), MAUI-Erfassung (`MyArbeitsstundenPage`) und `SupabaseService.AddArbeitsstundeAsync(...)`.
- Die robuste Korrektur liegt jetzt nicht mehr nur im Record-Attribut, sondern technisch im Insertmodell selbst: für `arbeitsstunde` gibt es nun einen expliziten Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`), und `AddArbeitsstundeAsync(...)` schreibt beim Insert nur noch über genau diesen Typ. Damit kann `Id` – auch wenn aufrufende `ArbeitsstundeRecord`-Instanzen lokal weiter mit Default `0` ankommen – technisch nicht mehr in die Create-Payload gelangen.
- Als zusätzliche Guard-Absicherung übernimmt der Create-Pfad die `Id` grundsätzlich überhaupt nicht mehr. Es wird weder auf positive noch auf nichtpositive `Id` vertraut; Updates bleiben weiterhin separat über `UpdateArbeitsstundeAsync(...)` mit echter bestehender `Id` bestehen. Eine App-seitige `lastrow+1`-Logik wurde bewusst nicht eingeführt.
- Die Arbeitsstunden-Erfassungsansicht wurde im Abschlusszug ebenfalls auf das gewünschte Produktverhalten gezogen: nach erfolgreichem Speichern bleibt die Eingabemaske nicht mehr offen stehen, sondern verlässt den Pfad sauber. Im Dialogkontext schließt sich das Fenster wie bisher; im normalen WPF-Erfassungspfad wird nach erfolgreichem Save zurück auf `Home` navigiert.
- Die Arbeitsstunden-Freigabeansicht zieht jetzt dasselbe Abschlussverhalten nach: wenn alle markierten/geänderten Zeilen erfolgreich gespeichert wurden, wird nicht in der Prüfansicht verharrt, sondern wieder sauber auf `Home` zurücknavigiert. Der bestehende Fachpfad bleibt unverändert; `status` bleibt optionales Anmerkungsfeld.
- Den Home-Eintrag für `Arbeitseinsatz` auf den gewünschten Interaktionsstand korrigiert: der separate `Details`-Button wurde entfernt, Detailöffnung läuft jetzt per Doppelklick auf die Karte. Stattdessen bleibt dort der Button `Anmelden` sichtbar. Weil weiterhin kein belastbarer produktiver Schreibpfad für die echte Anmeldung vorhanden ist, bleibt dieser Button bewusst eine ehrliche Info-Aktion statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschädigt.

## 2026-03-23 – Zwei echte Arbeitsstundenfehler behoben: `id = 0` bei Neuanlage unterbunden und Freigabe-Checkbox aktiviert Speichern wieder sofort

- Den realen Produktfehler im Arbeitsstunden-Insertpfad diesmal bewusst bis zur tatsächlich laufenden Payload verfolgt statt nur den Service isoliert zu betrachten: WPF-Usererfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogerfassung (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) erzeugen alle neue `ArbeitsstundeRecord`-Instanzen ohne explizite `Id`-Zuweisung, damit aber typbedingt mit lokalem Default `Id = 0`.
- Die eigentliche Ursache lag im Modellvertrag von `ArbeitsstundeRecord`: anders als bei den übrigen produktiven Create-Modellen stand dort noch `[PrimaryKey("id")]` ohne explizites `shouldInsert = false`. Im aktuell laufenden Paketstand `Supabase.Postgrest 4.1.0` konnte die Primärschlüsselspalte dadurch im realen Insertpfad weiter in die Payload gelangen; genau so ließ sich der reproduzierte neue Datensatz mit `id = 0` erklären.
- Den Fix deshalb am tatsächlichen Fehlerursprung gesetzt: `ArbeitsstundeRecord` verwendet jetzt wie die übrigen produktiven Create-Modelle explizit `[PrimaryKey("id", false)]`. Damit bleibt die `Id` für Update-/PrimaryKey-Zwecke vorhanden, wird bei Neuanlagen aber nicht mehr in die Insert-Payload aufgenommen.
- Die zweite reale Störung in `Arbeitsstunden freigeben` ebenfalls bis zur konkreten UI-Ursache zurückgeführt: `status` war fachlich bereits optional und wurde in `HasChanges` nicht als Pflichtfeld behandelt. Das Problem lag stattdessen an der WPF-Spalte `DataGridCheckBoxColumn` für `Freigeben`; deren Wert wurde im laufenden UI-Pfad nicht zuverlässig sofort in das Item zurückgeschrieben, sodass `PropertyChanged`, `HasPendingChanges` und damit `SpeichernCommand` beim bloßen Setzen der Checkbox nicht unmittelbar anzogen.
- Den Fix daher klein und UI-seitig gehalten: die Checkboxspalte läuft jetzt als `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`. Damit gilt schon das reine Setzen von `Freigeben` sofort als speicherrelevante Änderung.
- Fachregeln bleiben unverändert: offen bleibt `freigegeben = false`, `status` bleibt nur optionales Anmerkungsfeld, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`, und es wurde keine neue Freigabearchitektur oder App-seitige `lastrow+1`-Logik eingeführt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im selben Shared-Pfad mitgedacht und nicht beschädigt.

## 2026-03-23 – WPF-Bindingfix in `ArbeitsstundenErfassungView`: `CurrentMemberText` als reine OneWay-Anzeige verdrahtet

- Den aktuellen Bindingfehler in `ArbeitsstundenErfassungView` gezielt auf die einzige Bindungsstelle von `CurrentMemberText` zurückgeführt: der Wert dient fachlich nur der Anzeige des aktuellen Mitgliedskontexts.
- Deshalb bewusst **keinen** Setter in `ArbeitsstundenErfassungViewModel` ergänzt und keine Fachlogik verändert: `CurrentMemberText` bleibt eine reine Anzeigeeigenschaft.
- Die Anzeige in `ArbeitsstundenErfassungView.xaml` wurde minimal und robust auf explizite OneWay-Darstellung umgestellt: statt `Run` wird der Wert jetzt in einem eigenen `TextBlock` mit `Mode=OneWay` angezeigt.
- Die benachbarten Anzeige-`TextBlock`s bleiben strukturell intakt; es wurde nur der kleine Anzeigeabschnitt für das Mitglied ersetzt, ohne weitere Formularlogik oder ViewModel-Verträge anzufassen.
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Der kleine Bindingfix ist damit sauber abgeschlossen; MAUI blieb unberührt.

## 2026-03-23 â€“ Insert-Pfade auf explizite ID-Mitgabe geprÃ¼ft: produktive Neuanlagen senden aktuell keine feste ID aus der App

- Den aktuellen produktiven Insert-/Create-Stand zuerst gegen den realen Arbeitsbaum geprÃ¼ft statt auf Verdacht umzubauen: gesichtet wurden `SupabaseService`, die betroffenen Records/Modelle, die WPF-/MAUI-Neuanlagepfade fÃ¼r `arbeitsstunde`, `termin`, `bekanntmachung` und `arbeitseinsatz` sowie ein mÃ¶glicher Pfad fÃ¼r `arbeitseinsatz_anmeldung`.
- FÃ¼r `arbeitsstunde` den konkreten Verdacht am echten Produktpfad geprÃ¼ft: WPF (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) bauen neue `ArbeitsstundeRecord`-Instanzen ohne gesetzte `Id`; `AddArbeitsstundeAsync(...)` erstellt daraus wiederum ein neues Insert-Objekt ebenfalls ohne `Id`-Zuweisung und sendet nur die fachlichen Felder an PostgREST.
- FÃ¼r die drei Verwaltungs-Neuanlagen denselben Punkt abgesichert: die WPF-Editoren bauen im New-Mode zwar Arbeitsobjekte mit `Id = 0` als lokalem Defaultwert, aber die produktiven Service-Create-Pfade `CreateTerminAsync(...)`, `CreateBekanntmachungAsync(...)` und `CreateArbeitseinsatzAsync(...)` Ã¼bernehmen diese `Id` gerade **nicht** in ihre tatsÃ¤chlichen Insert-Objekte. Gesendet werden dort nur die fachlichen Spalten; Updates laufen weiterhin separat Ã¼ber die vorhandene Datensatz-`Id`.
- Einen echten produktiven Insert-Pfad fÃ¼r `arbeitseinsatz_anmeldung` gibt es im aktuellen Repo weiterhin nicht: der vorhandene `Anmelden`-Pfad auf Home ist nach wie vor nur eine ehrliche Info-Aktion ohne Schreibzugriff. Entsprechend war dort aktuell auch kein produktiver ID-Insertpfad zu korrigieren.
- ZusÃ¤tzlich den Library-Unterbau gegen den aktuellen Paketstand geprÃ¼ft: im verwendeten `Supabase.Postgrest`-Paket ist beim `PrimaryKey`-Attribut der Parameter `shouldInsert` standardmÃ¤ÃŸig `false`. Damit wird die PrimÃ¤rschlÃ¼sselspalte auch beim verbleibenden Muster `[PrimaryKey("id")]` nicht automatisch in Insert-Payloads aufgenommen, solange sie nicht explizit gesetzt und bewusst anderweitig serialisiert wird.
- Ergebnis der PrÃ¼fung: im aktuellen produktiven App-Code lieÃŸ sich fÃ¼r die geprÃ¼ften Neuanlagepfade keine Stelle nachweisen, an der bei Inserts eine feste `id` oder konkret `id = 0` aus der App mitgesendet wird.
- Deshalb bewusst **kein** Vermutungsfix eingebaut: keine `lastrow+1`-Logik, keine unnÃ¶tige Schatten-Insertarchitektur und keine DB-MigrationsÃ¤nderung auf Verdacht. Der Block bleibt bei der belastbaren Analyse und Dokumentation des aktuell bereits korrekten Insert-Musters `Insert ohne feste ID, Update mit bestehender ID`.
- Repo-seitig zusÃ¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine versionierten SQL-/Migrationsdateien fÃ¼r die betroffenen Tabellen vor; eine DB-seitige Sequence-/Default-PrÃ¼fung war im Repo daher nicht weiter nachvollziehbar, wurde aber fÃ¼r diesen Block auch nicht benÃ¶tigt, weil auf App-Seite kein fehlerhafter ID-Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wird durch diesen Dokumentations-/Analyseblock nicht beschÃ¤digt.

## 2026-03-23 â€“ Home-/Detail-Block fÃ¼r ArbeitseinsÃ¤tze sauber abgeschlossen: Beginn sichtbar, Detaildaten vervollstÃ¤ndigt, Anmelden ehrlich nur informativ

- Den begonnenen Home-/Detail-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufzuziehen: bereits halbfertig vorhanden waren erweiterte Home-/Detailmodelle, ein vorbereiteter optionaler `Anmelden`-Pfad im Detailkontext und erste Anpassungen im `HomeViewModel`, aber die sichtbaren WPF-XAML-Schritte und der hintere Helper-Tail des `SupabaseService` waren noch nicht sauber abgeschlossen.
- Den Home-/Detailpfad fÃ¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` gemeinsam nachgezogen, damit die generische Detailansicht durch die Erweiterung keine Felder verliert: zusÃ¤tzliche Datensatzinformationen laufen jetzt konsistent Ã¼ber `DetailInfo`/`AdditionalInfo`, statt nur bei ArbeitseinsÃ¤tzen Sonderwissen lokal in der View zu hinterlegen.
- `Arbeitseinsatz` zeigt auf Home jetzt den Beginn explizit sichtbar im Karteneintrag; zusÃ¤tzlich gibt es dort einen eigenen `Details`-Button und â€“ nur wenn der Datensatz Ã¼berhaupt anmeldbar markiert ist â€“ einen `Anmelden`-Button.
- Die Home-Detailansicht zeigt fÃ¼r `Arbeitseinsatz` jetzt neben Titel/Untertitel auch die Ã¼brigen im Datensatz belastbar vorhandenen Informationen wie Thema, Datum, Beginn, Ende, Treffpunkt sowie KapazitÃ¤ts-/Anmeldeinformationen; `Max. Teilnehmer` wird bewusst nur dann angezeigt, wenn sie aus den vorhandenen Datensatzwerten Ã¼berhaupt belastbar ableitbar ist.
- `Termin`- und `Bekanntmachung`-Detailansichten wurden im selben Pfad mitkonsolidiert: zusÃ¤tzliche Felder wie Ort, Thema, Betreff, VerÃ¶ffentlichungszeitpunkt oder Kurztext werden jetzt ebenfalls Ã¼ber denselben Detailkontext transportiert, statt in der generischen Detailview verloren zu gehen.
- Den halbfertig beschÃ¤digten Tail des `SupabaseService` sauber wieder geschlossen und dabei die kleinen Home-Helfer zentral konsolidiert (`AddDetailLine`, Datum-/Zeitformatierung, Textnormalisierung, Dokumentpfad-Helfer etc.), damit der angefangene Block wieder auf einem kompilierbaren produktiven Stand steht.
- Den `Anmelden`-Usecase bewusst **nicht** als neue Schattenlogik erÃ¶ffnet: im aktuellen Repo lieÃŸ sich weiterhin kein belastbarer produktiver Schreibpfad fÃ¼r eine echte Arbeitseinsatz-Anmeldung finden. Deshalb ist der neue `Anmelden`-Button in WPF nur als ehrliche Info-Aktion verdrahtet und weist explizit darauf hin, dass der echte Schreibpfad noch nicht angebunden ist.
- WPF wurde sichtbar fertiggestellt; MAUI wurde bei den gemeinsamen Home-Items/Mappingerweiterungen mitgedacht, aber bewusst nicht mit einer neuen Arbeitseinsatz-AnmeldeoberflÃ¤che erweitert, solange dafÃ¼r kein belastbarer gemeinsamer Fachpfad existiert.
- Technisch verifiziert: Workspace-Build erfolgreich; der begonnene Home-/Detail-Block ist damit ohne erfundene Anmeldefachlogik sauber abgeschlossen.

## 2026-03-23 â€“ Verwaltungseditoren und Home-Navigation nachgezogen: ZurÃ¼ck-Text repariert, Doppelklick auf Listen, Arbeitsstunden-Erfassung nur noch Ã¼ber Home

- Die drei WPF-Verwaltungseditoren `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen` nach dem Parserfix nochmals produktiv bereinigt: der beschÃ¤digte Toolbar-Text `ZurÃ¼ck` wird jetzt XAML-seitig stabil als Entity geschrieben, damit kein erneutes Zeichensatzproblem im Buttontext entsteht.
- FÃ¼r alle drei Verwaltungslisten einen echten Doppelklickpfad auf die ausgewÃ¤hlten EintrÃ¤ge ergÃ¤nzt: statt auf `InputBindings` zu vertrauen, Ã¶ffnen die `ListBox`-EintrÃ¤ge jetzt direkt per `MouseDoubleClick` den vorhandenen `OeffnenCommand` und damit den Bearbeitungseditor zuverlÃ¤ssig.
- Die WPF-Hauptnavigation wieder auf den gewÃ¼nschten Produktpfad zurÃ¼ckgezogen: `Arbeitsstunden erfassen` wird nicht mehr als eigener Navigationseintrag aufgebaut und bleibt nur noch Ã¼ber den vorhandenen Button auf der `Startseite` erreichbar.

## 2026-03-23 â€“ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃ¤chliche XAML-Ursache zurÃ¼ckgefÃ¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃ¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃ¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃ¼nschte Doppelklickpfad bleibt weiterhin korrekt Ã¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 â€“ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃ¼ck-/Dirty-Check ergÃ¤nzt, Navigation auf Home-only zurÃ¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃ¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃ¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃ¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃŸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃ¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃ¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃ¤tzliche zentrale Normalisierung im `SupabaseService` fÃ¼r geladene VerwaltungsdatensÃ¤tze sowie fÃ¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃ¼ckkehr erfolgt Ã¼ber den vorhandenen Navigationspfad zurÃ¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃ¼ck`-Button
  - `ZurÃ¼ck` und `Abbrechen` prÃ¼fen auf echte ungespeicherte Ã„nderungen
  - die RÃ¼ckfrage erscheint nur bei tatsÃ¤chlichen Ã„nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃ¼nschten Produktpfad zurÃ¼ckgezogen:
  - `ArbeitseinsÃ¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch Ã¼ber die vorhandenen Home-Buttons fÃ¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃ¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃ¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 â€“ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃ¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃ¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃ¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃ¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen Ã¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen Ã¼ber `status`; die globale PrÃ¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃ¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃ¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃ¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(Ã¤lteste zuerst)*
  - pro Zeile sichtbare PrÃ¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃ¤chlich geÃ¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃ¤tze bleiben unverÃ¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃ¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃ¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃ¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall Ã¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃ¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Ã–ffnen der Freigabeansicht wird eine globale PrÃ¼fsperre Ã¼ber die offenen `arbeitstunde`-DatensÃ¤tze gesetzt
  - andere PrÃ¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃ¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃ¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃ¤ngert die Sperre wÃ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃ¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach Ã¼bernehmbar
- Die bestehende Badge-/ZÃ¤hlerlogik bleibt weiterverwendet: Ã„nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃ¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 â€“ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃ¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃ¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃ¼r offene FÃ¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter Ã¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter Ã¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin Ã¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃ¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃ¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃ¼r `Datum`, `Stunden` und `Art der Arbeit` â€“ ohne zusÃ¤tzliche Fachfelder und ohne neue FreigabeoberflÃ¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃ¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃ¼r Benutzer mit eigenem Mitgliedskontext; zusÃ¤tzlich erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃ¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃ¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃ¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃ¶ÃŸer als `0` sein.
- FÃ¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃ¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃ¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃ¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃ¤hler zeigt weiter die tatsÃ¤chliche Anzahl offener PrÃ¼ffÃ¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃ¼fen` wurde entlang der betroffenen Navigations-/TitelflÃ¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃ¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃ¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃ¤hrend im Repo noch die Ã¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃ¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃ¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch Ã¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃ¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃ¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃ¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃŸlich fÃ¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃ¼r spÃ¤tere MAUI-ParitÃ¤t anschlussfÃ¤hig: die produktiven Lese-/Create-/Update-Pfade fÃ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃ¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃ¤hrend die eigentlichen EditoroberflÃ¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃ¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃ¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃ¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 â€“ ArbeitseinsÃ¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃ¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃ¤tze-Verwaltung erneut geprÃ¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste Ã¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃ¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃ¤nzt und nicht Ã¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃ¤nderte DatensÃ¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃ¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃ¼r `ArbeitseinsÃ¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃ¶ÃŸer als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃ¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃ¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃ¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃ¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃ¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃ¶ÃŸere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃ¤tze-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃ¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste Ã¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃ¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃ¤nzt und nicht Ã¼ber Home-Views geschrieben: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃ¤nderte DatensÃ¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃ¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃ¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃ¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃ¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `Ãœberschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃ¤nzt und parallel in einer kompakten Vorschau geprÃ¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃ¤tzlichen fachlichen Sortierregeln erfunden. FÃ¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃ¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃ¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃ¤chsten AufrÃ¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃ¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃ¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃ¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃ¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃŸerhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃ¤nderte DatensÃ¤tze direkt per Create/Update wieder nach `termin` zurÃ¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz Ã¶ffnet rechts den Editor mit echten Werten, `Neu` Ã¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃ¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃ¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃ¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃ¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃ¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃ¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃ¼r `Bekanntmachungen` und `ArbeitseinsÃ¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃ¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 â€“ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃ¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃ¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃ¼r `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃ¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die Ãœbersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃ¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃ¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃ¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃ¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: Ã¼ber neue gemeinsame Servicezugriffe werden die bestÃ¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃ¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃ¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃ¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃ¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃ¼r Admin/Vorstand verfÃ¼gbar und blÃ¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃ¼r spÃ¤tere MAUI-ParitÃ¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 â€“ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃ¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃ¤tigt: `ArbeitseinsÃ¤tze` und `Termine` laden Ã¼ber die Startseiten-Views stabil; die verbleibenden AuffÃ¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃ¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃŸgeblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃ¤tigte View.
- FÃ¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃ¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃ¼fung bleibt daher auf tatsÃ¤chliche RÃ¼ckgabefelder bzw. reale DatensÃ¤tze der View fokussiert.
- Der UX-Nachzug fÃ¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃ¤tzen, Terminen und Bekanntmachungen Ã¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 â€“ Home-UX-Nachzug: Startseite zeigt nur Listen, Details Ã¶ffnen in eigener View

- Den Home-Stand fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen auf den gewÃ¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃ¼bersichten, wÃ¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃ¼r eine eigenstÃ¤ndige WPF-Detailansicht fÃ¼r Home-Elemente ergÃ¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Ã–ffnen-Kommandos fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃ¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃ¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃ¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃ¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃ¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur Teil 3: Ursachenanalyse fÃ¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃ¼ft. Die entscheidende technische Ursache fÃ¼r den Pflichtstundenfehler lieÃŸ sich jetzt direkt aus dem WPF-Debug-Log bestÃ¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃ¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃ¤hrend `ArbeitseinsÃ¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃ¼r Home jetzt zunÃ¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃŸend robust clientseitig Ã¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃ¼r Pflichtstunden ergÃ¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃ¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃ¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃ¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃ¼ckgelieferte EintrÃ¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃ¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃ¤nzt: der Home-Pfad schreibt jetzt fÃ¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃ¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur Teil 2: Pflichtstunden Ã¼ber Hauptmitglied/Saison prÃ¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃ¼ft: da `ArbeitseinsÃ¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen â€“ Pflichtstunden Ã¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃ¶sung und Bekanntmachungen Ã¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃ¼r Home deshalb final prÃ¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃ¼r Home relevante Hauptmitgliedsbezug aufgelÃ¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃ¼bersicht Ã¼ber dieses Hauptmitglied geladen.
- ZusÃ¤tzlich wird die bestÃ¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃ¼cksichtigt, sondern zuerst direkt fÃ¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃ¤llt erst danach auf Jahres-/letzten Datensatz zurÃ¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃ¤tzlich mÃ¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃ¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃ¼r Titel, Inhalt und VerÃ¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃ¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃ¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃ¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃ¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃ¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃ¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃ¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃ¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃ¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃ¤llt nicht mehr komplett auf leere Platzhalter zurÃ¼ck.
- Kleine, gezielte technische Transparenz ergÃ¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃ¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃ¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃ¤tzlich vereinfacht und nÃ¤her auf den bestÃ¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃ¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃ¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃ¶rt â€“ im bestÃ¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 â€“ Home-Diagnose: bestÃ¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃ¤tigten DB-Schema-Stand geprÃ¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃ¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃ¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃ¤tigten Kern geschÃ¤rft, ohne neue App-Logik einzufÃ¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃ¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃ¶st zuerst die aktuelle Saison Ã¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose Ã¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃ¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃ¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃ¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃ¼cke Ã¼ber MAUI-PrÃ¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃ¶glichen Abschlussoptionen geprÃ¼ft: ein echter gemeinsamer Ablehnungs-/RÃ¼ckgabe-/Kommentarpfad wÃ¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃ¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃ¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃ¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃ¤hlt und den vorhandenen mobilen PrÃ¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃ¼fen` mobil jetzt nur noch dann an, wenn Ã¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃ¤chlich offene PrÃ¼ffÃ¤lle vorliegen; zusÃ¤tzlich erscheint die aktuelle Anzahl offener DatensÃ¤tze direkt im Titel des MenÃ¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder Ã¼ber denselben gemeinsamen PrÃ¼fpfad, sodass MenÃ¼eintrag und tatsÃ¤chliche offene FÃ¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃ¼hrt: die mobile Seite bleibt vollstÃ¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃ¼fstatusanzeige drauÃŸen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃ¼fflow in WPF bis zur Freigabe vervollstÃ¤ndigt

- Den aktuellen WPF-Istzustand fÃ¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃ¼ft: belastbar vorhanden waren bereits PrÃ¼fliste, Badge/ZÃ¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃ¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃ¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃ¼ckgefÃ¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃ¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃ¼fmodus geÃ¶ffnet, gefiltert auf die tatsÃ¤chlich offenen DatensÃ¤tze des ausgewÃ¤hlten Mitglieds.
- DafÃ¼r nur einen kleinen Navigationskontext ergÃ¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃ¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃ¼feintrÃ¤ge des konkret ausgewÃ¤hlten Mitglieds im PrÃ¼fmodus erscheinen.
- Admin/Vorstand kÃ¶nnen im PrÃ¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃ¤tzlich eine direkte `Freigeben`-Aktion fÃ¼r den ausgewÃ¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃ¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃ¼r einen sauberen RÃ¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃ¼fliste und Badge/ZÃ¤hler weiterhin Ã¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃ¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃ¼fmodus fachlich geschÃ¤rft: klarer Titel, Hinweistext, leerer PrÃ¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃ¤nde fÃ¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃ¼fliste und PrÃ¼fzÃ¤hler ausgeschlossen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃ¤ndigung des PrÃ¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃ¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃ¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation and den gemeinsamen PrÃ¼fpfad erneut geprÃ¼ft: ein belastbarer Listen-/Detailpfad fÃ¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃ¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und Ã¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃ¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃ¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃ¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃ¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃ¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃ¤ge, Vorstand/Admin kÃ¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃ¼hrt.
- Das Arbeitsstunden-Formular geschÃ¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃŸt konsistent `Speichern`.
- Kleinen WPF-PrÃ¼fpfad fÃ¼r offene Arbeitsstunden ergÃ¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃ¼hren von der PrÃ¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃ¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃ¼fen` erscheint fÃ¼r berechtigte Benutzer jetzt nur bei tatsÃ¤chlich offenen PrÃ¼ffÃ¤llen, zeigt einen kleinen ZÃ¤hler mit der Zahl offener DatensÃ¤tze und wird bei offenem PrÃ¼fstand sichtbar hervorgehoben; Aktualisierung lÃ¤uft nach Ã„nderungen Ã¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃ¼flisten und PrÃ¼fzÃ¤hlern ausgeschlossen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃ¤hig.

## 2026-03-22 â€“ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃ¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃ¤rten produktiven DB-Pfade geprÃ¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃ¤hrend `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃ¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃ¤hen: `HomeOverviewDTO` trÃ¤gt jetzt zusÃ¤tzlich gemeinsame Startseiten-Daten fÃ¼r Pflichtstunden, ArbeitseinsÃ¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃ¤dt diese Inhalte direkt aus den geklÃ¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃ¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃ¼r die Home-Ausgabe mitgefÃ¼hrt.
- Bereich `ArbeitseinsÃ¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃ¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃ¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃ¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃ¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃ¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃ¤sst sich wieder mit `SchlieÃŸen` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃ¼r Bekanntmachungen angebunden: echte EintrÃ¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃ¼gbaren Inhalte statt des bisherigen kÃ¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃ¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃ¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃ¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃ¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃ¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃ¼ft: konkret auffÃ¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃ¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃ¶ÃŸeren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃ¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃ¼ckgefÃ¼hrt: `MainWindowViewModel` lÃ¤dt fÃ¼r den Eigenkontext weiter den nÃ¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃ¤ÃŸig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃ¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃ¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃ¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃ¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃ¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃŸen`-Button ergÃ¤nzt.
- Weiterhin bewusst nicht erfunden: fÃ¼r `ArbeitseinsÃ¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃ¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃ¼ft und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃ¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃ¼fpfad fÃ¼r Block 6 bewusst genau diesen Core-Baustein gewÃ¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃ¤tzliche Infrastruktur direkt testbar.
- DafÃ¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃ¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃ¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃ¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 â€“ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃ¤chsten PrÃ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃ¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃ¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃ¼r WPF und MAUI noch ungetestet blieb.
- Als nÃ¤chsten kleinen PrÃ¼fpfad bewusst den `HomeOverviewFactory` gewÃ¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃ¼r Desktop und Mobil und lÃ¤sst sich ohne zusÃ¤tzliche Testinfrastruktur direkt prÃ¼fen.
- DafÃ¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃ¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃ¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃ¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃ¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃ¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃ¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃ¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃ¼ckgezogen, sondern sinnvoll zusammengefÃ¼hrt: die aktive Root-Projektdatei Ã¼bernimmt jetzt wieder die fÃ¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃ¤hrend der neue `OperationalDataFilter`-Block vollstÃ¤ndig erhalten bleibt.
- Fachlich passende Ã¤ltere Tests sauber zurÃ¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃ¼fpfad fÃ¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃŸes Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden Ã¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃ¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃ¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃ¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 1: Kleine aktive Testbasis fÃ¼r Demo-/Testdatenfilter zurÃ¼ckgeholt

- Die aktuelle Testlage geprÃ¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃ¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine Ã¤ltere Testidee fÃ¼r `ExportViewModel`, die fÃ¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃ¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃ¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃ¼fung) ohne dass dafÃ¼r eine groÃŸe Testinfrastruktur nÃ¶tig wÃ¤re.
- DafÃ¼r eine kleine aktive Testbasis im Root zurÃ¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃ¼ft zurÃ¼ckzuziehen.
- Den gewÃ¤hlten PrÃ¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃ¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃ¼ckgezogen: sie waren fÃ¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃ¤tten den Wiedereinstieg unnÃ¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃ¤tzlich lÃ¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃ¼nen Tests.

## 2026-03-22 â€“ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃ¤t mit Arbeitsstunden-PrÃ¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃ¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃ¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃ¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃ¤tslÃ¼cke fÃ¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃ¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃ¼hrt: die Umsetzung bleibt vollstÃ¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃ¼r offene Arbeitsstunden berÃ¼cksichtigt nur operative Mitglieder Ã¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃ¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃ¼r Arbeitsstunden, keine zusÃ¤tzlichen WPF-OberflÃ¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 â€“ Block 5 Prompt 2: NÃ¤chste mobile Admin-ParitÃ¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃ¼cken erneut gegen die produktiven WPF-Wege geprÃ¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃ¤chste kleine, aber fachlich wichtige ParitÃ¤tslÃ¼cke, weil WPF dafÃ¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃ¤tslÃ¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃ¼r belastbar zugeordnete Mitglieder auch die Rolle Ã¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃ¼r RollenÃ¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃ¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃ¤rft: die operative Benutzerliste bevorzugt nun die tatsÃ¤chliche Mitgliedsrolle vor einem evtl. Ã¤lteren `app_user`-Wert, damit RollenÃ¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃŸen: die bereits im gemeinsamen Benutzerlistenpfad eingefÃ¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 â€“ Block 5 Prompt 1: NÃ¤chste mobile Admin-ParitÃ¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃ¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃ¼ft: die deutlichste belastbare LÃ¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃ¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃ¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃ¼cke als nÃ¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃ¼r Admin/Vorstand, baut vollstÃ¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃ¼r MAUI ergÃ¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃŸen und Passwort-Reset versenden laufen mobil jetzt Ã¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Ã„nderungsgrenze auch mobil sauber berÃ¼cksichtigt: eine E-Mail-Ã„nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃ¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃ¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit Ã¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃ¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 â€“ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃ¼ft: offen war vor allem noch die Nutzer-ParitÃ¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃ¼fpfad `Arbeitsstunden prÃ¼fen` bleiben dagegen weiterhin bewusst auÃŸerhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃ¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃŸt sich die bisherige Home-/Pfad-LÃ¼cke gegenÃ¼ber MAUI.
- IrrefÃ¼hrende leere Bekanntmachungs-DetailflÃ¤chen entschÃ¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃ¼fpfad fÃ¼r `Arbeitsstunden prÃ¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃ¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 â€“ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃ¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃ¼f-/Bekanntmachungs-Backendpfad fÃ¼r Home ist dagegen weiterhin nicht belastbar genug fÃ¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃ¼r WPF und MAUI.
- NÃ¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃ¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃ¼fstÃ¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃ¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-Ãœberblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃ¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃ¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃ¶rige gemeinsame PrÃ¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 â€“ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃ¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃ¼ndelt jetzt den fachlichen Kern fÃ¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃ¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃ¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulauflistung wurde fÃ¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃ¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃ¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃ¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 â€“ Block 3/3 Prompt 3: Parzellen-KernlÃ¼cken und mobile Admin-ParitÃ¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃ¼ft: belastbar verfÃ¼gbar waren bereits aktive ZÃ¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃŸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃ¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃ¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃ¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃ¤hler einbauen und ausbauen sowie das Ã–ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃ¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃ¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃ¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃ¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 â€“ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃ¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃ¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃ¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃ¼ckgefÃ¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden Ãœberschneidungen gegen den gewÃ¤hlten Starttag geprÃ¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃ¤nzt: Mitglied zuordnen, Startdatum wÃ¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃ¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃ¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung Ã¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃ¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt Ã¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 â€“ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃ¤higkeit geprÃ¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃ¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃ¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃ¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃ¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃ¶rigen Parzellen-Dateien durchgefÃ¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃŸerhalb des Commits.

## 2026-03-21 â€“ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃ¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃ¼ft und archivwÃ¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃ¼cklichen Wunsch auch `KGV.Tests`; zusÃ¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich Ã¼berfÃ¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃ¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃ¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃ¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃ¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃ¤ndert gelassen: keine Ã„nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃŸerhalb der aktiven LÃ¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 â€“ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃ¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃ¼ft: nur die tatsÃ¤chlich geÃ¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃ¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃ¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃ¼r `Mailadresse Ã¤ndern` in `MemberDetailViewModel` ergÃ¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃ¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃ¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃ¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃŸend baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃ¼r diesen Teilblock vorbereitet: nur die tatsÃ¤chlich zugehÃ¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃŸen vor.

## 2026-03-21 â€“ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃ¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert Ã¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃ¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃ¤nde zurÃ¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃ¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃ¼ckkehrt.
- Login-/Recovery-ZustÃ¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃ¤umt alte OTP-ZustÃ¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃ¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃ¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃ¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 â€“ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃ¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃ¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu Ã¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt Ã¼ber eine echte `NavigationPage` mit `LoginPage` statt Ã¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃ¤rft: normales Login, OTP-PrÃ¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃ¤nde gefÃ¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃ¼hrung sauber: RÃ¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung Ã¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃ¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 â€“ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃ¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃ¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg Ã¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃ¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃ¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 â€“ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃ¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃ¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃ¤nzt: neues `README.md` als Startpunkt fÃ¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃ¼ckgefÃ¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃ¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃ¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃ¤digten/irrefÃ¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃ¼hrt: `KGV.slnx` um `KGV.Tests` ergÃ¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` Ã¼berfÃ¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃ¼fbaren Projekte ausgerichtet.

## 2026-03-21 â€“ Prompt 1/2: Home/Startseite â€“ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃ¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃ¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃ¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃ¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃ¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃ¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃ¼r `admin` und `vorstand` sichtbar, Ã¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃ¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃ¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃ¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 â€“ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃ¤nde fÃ¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃ¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

---

## Workaround & Vorgehensweise bei Ã„nderungen

- Ã„nderungen nur nach genauer RÃ¼ckfrage und vollstÃ¤ndiger Ansicht der Datei vornehmen.  
- Ich mÃ¶chte immer genau wissen, **wo und wie** die Ã„nderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor Ã„nderungen vorgeschlagen werden.  

---

## 2026-03-23 â€“ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃ¤chliche XAML-Ursache zurÃ¼ckgefÃ¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃ¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃ¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃ¼nschte Doppelklickpfad bleibt weiterhin korrekt Ã¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 â€“ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃ¼ck-/Dirty-Check ergÃ¤nzt, Navigation auf Home-only zurÃ¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃ¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃ¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃ¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃŸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃ¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃ¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃ¤tzliche zentrale Normalisierung im `SupabaseService` fÃ¼r geladene VerwaltungsdatensÃ¤tze sowie fÃ¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃ¼ckkehr erfolgt Ã¼ber den vorhandenen Navigationspfad zurÃ¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃ¼ck`-Button
  - `ZurÃ¼ck` und `Abbrechen` prÃ¼fen auf echte ungespeicherte Ã„nderungen
  - die RÃ¼ckfrage erscheint nur bei tatsÃ¤chlichen Ã„nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃ¼nschten Produktpfad zurÃ¼ckgezogen:
  - `ArbeitseinsÃ¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch Ã¼ber die vorhandenen Home-Buttons fÃ¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃ¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃ¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 â€“ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃ¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃ¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃ¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃ¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen Ã¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen Ã¼ber `status`; die globale PrÃ¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃ¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃ¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃ¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(Ã¤lteste zuerst)*
  - pro Zeile sichtbare PrÃ¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃ¤chlich geÃ¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃ¤tze bleiben unverÃ¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃ¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃ¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃ¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall Ã¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃ¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim Ã–ffnen der Freigabeansicht wird eine globale PrÃ¼fsperre Ã¼ber die offenen `arbeitstunde`-DatensÃ¤tze gesetzt
  - andere PrÃ¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃ¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃ¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃ¤ngert die Sperre wÃ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃ¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach Ã¼bernehmbar
- Die bestehende Badge-/ZÃ¤hlerlogik bleibt weiterverwendet: Ã„nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃ¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 â€“ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃ¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃ¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃ¼r offene FÃ¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter Ã¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter Ã¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin Ã¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃ¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃ¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃ¼r `Datum`, `Stunden` und `Art der Arbeit` â€“ ohne zusÃ¤tzliche Fachfelder und ohne neue FreigabeoberflÃ¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃ¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃ¼r Benutzer mit eigenem Mitgliedskontext; additionally erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃ¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃ¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃ¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃ¶ÃŸer als `0` sein.
- FÃ¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃ¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃ¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃ¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃ¤hler zeigt weiter die tatsÃ¤chliche Anzahl offener PrÃ¼ffÃ¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃ¼fen` wurde entlang der betroffenen Navigations-/TitelflÃ¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃ¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃ¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃ¤hrend im Repo noch die Ã¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃ¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃ¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch Ã¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃ¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃ¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃ¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃŸlich fÃ¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃ¼r spÃ¤tere MAUI-ParitÃ¤t anschlussfÃ¤hig: die produktiven Lese-/Create-/Update-Pfade fÃ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃ¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃ¤hrend die eigentlichen EditoroberflÃ¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃ¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃ¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃ¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 â€“ ArbeitseinsÃ¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃ¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃ¤tze-Verwaltung erneut geprÃ¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste Ã¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃ¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃ¤nzt und nicht Ã¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃ¤nderte DatensÃ¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃ¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃ¼r `ArbeitseinsÃ¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃ¶ÃŸer als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃ¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃ¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃ¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃ¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃ¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃ¶ÃŸere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃ¤tze-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃ¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste Ã¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃ¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃ¤nzt und nicht Ã¼ber Home-Views geschrieben: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃ¤nderte DatensÃ¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃ¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃ¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃ¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃ¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `Ãœberschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃ¤nzt und parallel in einer kompakten Vorschau geprÃ¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃ¤tzlichen fachlichen Sortierregeln erfunden. FÃ¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃ¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃ¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃ¤chsten AufrÃ¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 â€“ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃ¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃ¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃ¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃ¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃŸerhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃ¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃ¤nderte DatensÃ¤tze direkt per Create/Update wieder nach `termin` zurÃ¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz Ã¶ffnet rechts den Editor mit echten Werten, `Neu` Ã¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃ¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃ¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃ¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃ¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃ¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃ¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃ¼r `Bekanntmachungen` und `ArbeitseinsÃ¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃ¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 â€“ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃ¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃ¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃ¼r `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃ¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die Ãœbersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃ¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃ¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃ¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃ¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: Ã¼ber neue gemeinsame Servicezugriffe werden die bestÃ¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃ¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃ¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃ¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃ¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃ¼r Admin/Vorstand verfÃ¼gbar und blÃ¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃ¼r spÃ¤tere MAUI-ParitÃ¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 â€“ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃ¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃ¤tigt: `ArbeitseinsÃ¤tze` und `Termine` laden Ã¼ber die Startseiten-Views stabil; die verbleibenden AuffÃ¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃ¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃŸgeblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃ¤tigte View.
- FÃ¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃ¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃ¼fung bleibt daher auf tatsÃ¤chliche RÃ¼ckgabefelder bzw. reale DatensÃ¤tze der View fokussiert.
- Der UX-Nachzug fÃ¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃ¤tzen, Terminen und Bekanntmachungen Ã¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 â€“ Home-UX-Nachzug: Startseite zeigt nur Listen, Details Ã¶ffnen in eigener View

- Den Home-Stand fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen auf den gewÃ¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃ¼bersichten, wÃ¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃ¼r eine eigenstÃ¤ndige WPF-Detailansicht fÃ¼r Home-Elemente ergÃ¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite Ã–ffnen-Kommandos fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃ¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃ¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃ¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃ¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃ¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur Teil 3: Ursachenanalyse fÃ¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃ¼ft. Die entscheidende technische Ursache fÃ¼r den Pflichtstundenfehler lieÃŸ sich jetzt direkt aus dem WPF-Debug-Log bestÃ¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃ¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃ¤hrend `ArbeitseinsÃ¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃ¼r Home jetzt zunÃ¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃŸend robust clientseitig Ã¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃ¼r Pflichtstunden ergÃ¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃ¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃ¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃ¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃ¼ckgelieferte EintrÃ¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃ¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃ¤nzt: der Home-Pfad schreibt jetzt fÃ¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃ¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur Teil 2: Pflichtstunden Ã¼ber Hauptmitglied/Saison prÃ¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃ¼ft: da `ArbeitseinsÃ¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen â€“ Pflichtstunden Ã¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃ¶sung und Bekanntmachungen Ã¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃ¼r Home deshalb final prÃ¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃ¼r Home relevante Hauptmitgliedsbezug aufgelÃ¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃ¼bersicht Ã¼ber dieses Hauptmitglied geladen.
- ZusÃ¤tzlich wird die bestÃ¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃ¼cksichtigt, sondern zuerst direkt fÃ¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃ¤llt erst danach auf Jahres-/letzten Datensatz zurÃ¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃ¤tzlich mÃ¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃ¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃ¼r Titel, Inhalt und VerÃ¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃ¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃ¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 â€“ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃ¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃ¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃ¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃ¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃ¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃ¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃ¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃ¤llt nicht mehr komplett auf leere Platzhalter zurÃ¼ck.
- Kleine, gezielte technische Transparenz ergÃ¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃ¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃ¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃ¤tzlich vereinfacht und nÃ¤her auf den bestÃ¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃ¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃ¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃ¶rt â€“ im bestÃ¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 â€“ Home-Diagnose: bestÃ¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃ¤tigten DB-Schema-Stand geprÃ¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃ¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃ¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃ¤tigten Kern geschÃ¤rft, ohne neue App-Logik einzufÃ¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃ¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃ¶st zuerst die aktuelle Saison Ã¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose Ã¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃ¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃ¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃ¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃ¼cke Ã¼ber MAUI-PrÃ¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃ¶glichen Abschlussoptionen geprÃ¼ft: ein echter gemeinsamer Ablehnungs-/RÃ¼ckgabe-/Kommentarpfad wÃ¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃ¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃ¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃ¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃ¤hlt und den vorhandenen mobilen PrÃ¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃ¼fen` mobil jetzt nur noch dann an, wenn Ã¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃ¤chlich offene PrÃ¼ffÃ¤lle vorliegen; zusÃ¤tzlich erscheint die aktuelle Anzahl offener DatensÃ¤tze direkt im Titel des MenÃ¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder Ã¼ber denselben gemeinsamen PrÃ¼fpfad, sodass MenÃ¼eintrag und tatsÃ¤chliche offene FÃ¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃ¼hrt: die mobile Seite bleibt vollstÃ¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃ¼fstatusanzeige drauÃŸen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃ¼fflow in WPF bis zur Freigabe vervollstÃ¤ndigt

- Den aktuellen WPF-Istzustand fÃ¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃ¼ft: belastbar vorhanden waren bereits PrÃ¼fliste, Badge/ZÃ¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃ¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃ¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃ¼ckgefÃ¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃ¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃ¼fmodus geÃ¶ffnet, gefiltert auf die tatsÃ¤chlich offenen DatensÃ¤tze des ausgewÃ¤hlten Mitglieds.
- DafÃ¼r nur einen kleinen Navigationskontext ergÃ¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃ¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃ¼feintrÃ¤ge des konkret ausgewÃ¤hlten Mitglieds im PrÃ¼fmodus erscheinen.
- Admin/Vorstand kÃ¶nnen im PrÃ¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃ¤tzlich eine direkte `Freigeben`-Aktion fÃ¼r den ausgewÃ¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃ¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃ¼r einen sauberen RÃ¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃ¼fliste und Badge/ZÃ¤hler weiterhin Ã¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃ¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃ¼fmodus fachlich geschÃ¤rft: klarer Titel, Hinweistext, leerer PrÃ¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃ¤nde fÃ¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃ¼fliste und PrÃ¼fzÃ¤hler ausgeschlossen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃ¤ndigung des PrÃ¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃ¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃ¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation und den gemeinsamen PrÃ¼fpfad erneut geprÃ¼ft: ein belastbarer Listen-/Detailpfad fÃ¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃ¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und Ã¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃ¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃ¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃ¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃ¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃ¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃ¤ge, Vorstand/Admin kÃ¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃ¼hrt.
- Das Arbeitsstunden-Formular geschÃ¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃŸt konsistent `Speichern`.
- Kleinen WPF-PrÃ¼fpfad fÃ¼r offene Arbeitsstunden ergÃ¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃ¼hren von der PrÃ¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃ¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃ¼fen` erscheint fÃ¼r berechtigte Benutzer jetzt nur bei tatsÃ¤chlich offenen PrÃ¼ffÃ¤llen, zeigt einen kleinen ZÃ¤hler mit der Zahl offener DatensÃ¤tze und wird bei offenem PrÃ¼fstand sichtbar hervorgehoben; Aktualisierung lÃ¤uft nach Ã„nderungen Ã¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃ¼flisten und PrÃ¼fzÃ¤hlern ausgeschlossen, weil weiterhin ausschlieÃŸlich der gemeinsame operative PrÃ¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃ¤hig.

## 2026-03-22 â€“ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃ¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃ¤rten produktiven DB-Pfade geprÃ¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃ¤hrend `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃ¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃ¤hen: `HomeOverviewDTO` trÃ¤gt jetzt zusÃ¤tzlich gemeinsame Startseiten-Daten fÃ¼r Pflichtstunden, ArbeitseinsÃ¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃ¤dt diese Inhalte direkt aus den geklÃ¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃ¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃ¼r die Home-Ausgabe mitgefÃ¼hrt.
- Bereich `ArbeitseinsÃ¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃ¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃ¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃ¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃ¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃ¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃ¤sst sich wieder mit `SchlieÃŸen` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃ¼r Bekanntmachungen angebunden: echte EintrÃ¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃ¼gbaren Inhalte statt des bisherigen kÃ¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃ¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃ¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃ¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃ¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃ¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃ¼ft: konkret auffÃ¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃ¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃ¶ÃŸeren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃ¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃ¼ckgefÃ¼hrt: `MainWindowViewModel` lÃ¤dt fÃ¼r den Eigenkontext weiter den nÃ¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃ¤ÃŸig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃ¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃ¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃ¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃ¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃ¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃŸen`-Button ergÃ¤nzt.
- Weiterhin bewusst nicht erfunden: fÃ¼r `ArbeitseinsÃ¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃ¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃ¼ft und bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃ¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃ¼fpfad fÃ¼r Block 6 bewusst genau diesen Core-Baustein gewÃ¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃ¤tzliche Infrastruktur direkt testbar.
- DafÃ¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃ¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃ¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃ¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 â€“ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃ¤chsten PrÃ¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃ¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃ¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃ¼r WPF und MAUI noch ungetestet blieb.
- Als nÃ¤chsten kleinen PrÃ¼fpfad bewusst den `HomeOverviewFactory` gewÃ¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃ¼r Desktop und Mobil und lÃ¤sst sich ohne zusÃ¤tzliche Testinfrastruktur direkt prÃ¼fen.
- DafÃ¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃ¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃ¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃ¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃ¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃ¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃ¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃ¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃ¼ckgezogen, sondern sinnvoll zusammengefÃ¼hrt: die aktive Root-Projektdatei Ã¼bernimmt jetzt wieder die fÃ¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃ¤hrend der neue `OperationalDataFilter`-Block vollstÃ¤ndig erhalten bleibt.
- Fachlich passende Ã¤ltere Tests sauber zurÃ¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃ¼fpfad fÃ¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃŸes Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden Ã¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃ¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃ¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃ¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃ¤hig.

## 2026-03-22 â€“ Block 6 Prompt 1: Kleine aktive Testbasis fÃ¼r Demo-/Testdatenfilter zurÃ¼ckgeholt

- Die aktuelle Testlage geprÃ¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃ¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine Ã¤ltere Testidee fÃ¼r `ExportViewModel`, die fÃ¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃ¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃ¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃ¼fung) ohne dass dafÃ¼r eine groÃŸe Testinfrastruktur nÃ¶tig wÃ¤re.
- DafÃ¼r eine kleine aktive Testbasis im Root zurÃ¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃ¼ft zurÃ¼ckzuziehen.
- Den gewÃ¤hlten PrÃ¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃ¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃ¼ckgezogen: sie waren fÃ¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃ¤tten den Wiedereinstieg unnÃ¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃ¤tzlich lÃ¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃ¼nen Tests.

## 2026-03-22 â€“ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃ¤t mit Arbeitsstunden-PrÃ¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃ¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃ¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃ¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃ¤tslÃ¼cke fÃ¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃ¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃ¼hrt: die Umsetzung bleibt vollstÃ¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃ¼r offene Arbeitsstunden berÃ¼cksichtigt nur operative Mitglieder Ã¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃ¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃ¼r Arbeitsstunden, keine zusÃ¤tzlichen WPF-OberflÃ¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 â€“ Block 5 Prompt 2: NÃ¤chste mobile Admin-ParitÃ¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃ¼cken erneut gegen die produktiven WPF-Wege geprÃ¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃ¤chste kleine, aber fachlich wichtige ParitÃ¤tslÃ¼cke, weil WPF dafÃ¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃ¤tslÃ¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃ¼r belastbar zugeordnete Mitglieder auch die Rolle Ã¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃ¼r RollenÃ¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃ¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃ¤rft: die operative Benutzerliste bevorzugt nun die tatsÃ¤chliche Mitgliedsrolle vor einem evtl. Ã¤lteren `app_user`-Wert, damit RollenÃ¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃŸen: die bereits im gemeinsamen Benutzerlistenpfad eingefÃ¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 â€“ Block 5 Prompt 1: NÃ¤chste mobile Admin-ParitÃ¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃ¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃ¼ft: die deutlichste belastbare LÃ¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃ¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃ¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃ¼cke als nÃ¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃ¼r Admin/Vorstand, baut vollstÃ¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃ¼r MAUI ergÃ¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃŸen und Passwort-Reset versenden laufen mobil jetzt Ã¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-Ã„nderungsgrenze auch mobil sauber berÃ¼cksichtigt: eine E-Mail-Ã„nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃ¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃ¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit Ã¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃ¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃ¤tsschritt weiterhin erfolgreich.

## 2026-03-22 â€“ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃ¼ft: offen war vor allem noch die Nutzer-ParitÃ¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃ¼fpfad `Arbeitsstunden prÃ¼fen` bleiben dagegen weiterhin bewusst auÃŸerhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃ¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃŸt sich die bisherige Home-/Pfad-LÃ¼cke gegenÃ¼ber MAUI.
- IrrefÃ¼hrende leere Bekanntmachungs-DetailflÃ¤chen entschÃ¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃ¼fpfad fÃ¼r `Arbeitsstunden prÃ¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃ¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 â€“ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃ¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃ¼f-/Bekanntmachungs-Backendpfad fÃ¼r Home ist dagegen weiterhin nicht belastbar genug fÃ¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃ¼r WPF und MAUI.
- NÃ¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃ¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃ¼fstÃ¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃ¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-Ãœberblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃ¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃ¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃ¶rige gemeinsame PrÃ¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 â€“ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃ¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃ¼ndelt jetzt den fachlichen Kern fÃ¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃ¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃ¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulaufl listing wurde fÃ¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃ¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃ¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃ¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 â€“ Block 3/3 Prompt 3: Parzellen-KernlÃ¼cken und mobile Admin-ParitÃ¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃ¼ft: belastbar verfÃ¼gbar waren bereits aktive ZÃ¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃŸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃ¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃ¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃ¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃ¤hler einbauen und ausbauen sowie das Ã–ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃ¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃ¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃ¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃ¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 â€“ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃ¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃ¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃ¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃ¼ckgefÃ¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden Ãœberschneidungen gegen den gewÃ¤hlten Starttag geprÃ¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃ¤nzt: Mitglied zuordnen, Startdatum wÃ¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃ¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃ¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung Ã¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃ¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt Ã¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 â€“ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃ¤higkeit geprÃ¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃ¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃ¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃ¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃ¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃ¶rigen Parzellen-Dateien durchgefÃ¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃŸerhalb des Commits.

## 2026-03-21 â€“ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃ¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃ¼ft und archivwÃ¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃ¼cklichen Wunsch auch `KGV.Tests`; zusÃ¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich Ã¼berfÃ¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃ¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃ¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃ¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃ¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃ¤ndert gelassen: keine Ã„nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃŸerhalb der aktiven LÃ¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 â€“ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃ¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃ¼ft: nur die tatsÃ¤chlich geÃ¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃ¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃ¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃ¼r `Mailadresse Ã¤ndern` in `MemberDetailViewModel` ergÃ¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃ¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃ¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃ¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃŸend baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃ¼r diesen Teilblock vorbereitet: nur die tatsÃ¤chlich zugehÃ¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃŸen vor.

## 2026-03-21 â€“ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃ¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert Ã¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃ¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃ¤nde zurÃ¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃ¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃ¼ckkehrt.
- Login-/Recovery-ZustÃ¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃ¤umt alte OTP-ZustÃ¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃ¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃ¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃ¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 â€“ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃ¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃ¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu Ã¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt Ã¼ber eine echte `NavigationPage` mit `LoginPage` statt Ã¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃ¤rft: normales Login, OTP-PrÃ¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃ¤nde gefÃ¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃ¼hrung sauber: RÃ¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung Ã¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃ¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 â€“ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃ¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃ¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg Ã¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃ¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃ¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 â€“ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃ¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃ¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃ¤nzt: neues `README.md` als Startpunkt fÃ¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃ¼ckgefÃ¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃ¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃ¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃ¤digten/irrefÃ¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃ¼hrt: `KGV.slnx` um `KGV.Tests` ergÃ¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` Ã¼berfÃ¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃ¼fbaren Projekte ausgerichtet.

## 2026-03-21 â€“ Prompt 1/2: Home/Startseite â€“ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃ¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃ¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃ¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃ¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃ¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃ¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃ¼r `admin` und `vorstand` sichtbar, Ã¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃ¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃ¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃ¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 â€“ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃ¤nde fÃ¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃ¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
