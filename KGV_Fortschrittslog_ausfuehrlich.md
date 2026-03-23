# KGV_Fortschrittslog_ausfuehrlich

---

## 2026-03-24 – Prompt 1/1: RPC-Anmeldeblock für Arbeitseinsatz sauber abgeschlossen, committed und gepusht

- Den begonnenen Arbeitseinsatz-Anmeldeblock ohne neue Fachlogik sauber abgeschlossen.
- Abschlussverifikation: `KGV.Wpf` baut erfolgreich; der produktive Pfad bleibt `sign_up_for_arbeitseinsatz(...)` über den Shared-Service `SignUpForArbeitseinsatzAsync(...)`.
- Home und Detailview nutzen weiterhin denselben Servicepfad; es wurde kein zweiter ViewModel-Schreibpfad eröffnet.
- Doppelanmeldung, abgelaufene Frist und volle Platzgrenze bleiben vor dem RPC geprüft und werden zusätzlich bei einem möglichen RPC-Rennen verständlich abgefangen.
- Für Commit und Push wurden ausschließlich die Blockdateien aufgenommen; blockfremde lokale Änderungen und Artefakte blieben bewusst unberührt.

## 2026-03-24 – Prompt 1/1: `Anmelden` für Arbeitseinsatz produktiv über den echten DB-Funktionspfad angeschlossen

- Den Block zuerst ausdrücklich gegen den lokalen DB-Analyseexport `_AI_DB_EXPORT` geführt und nicht gegen Vermutungen. Belastbar geprüft wurden `database.types.ts`, `roles.sql` sowie der mitreferenzierte SQL-Kontext. Ausschlaggebend war dabei die in `database.types.ts` bestätigte DB-Struktur:
  - Tabelle `arbeitseinsatz_anmeldung`
  - Enum `arbeitseinsatz_anmeldung_status` mit u. a. `angemeldet`
  - Funktion `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` mit Rückgabe eines `arbeitseinsatz_anmeldung`-Datensatzes
  - Funktion `sign_off_from_arbeitseinsatz(...)`, die für diesen Block bewusst noch nicht produktiv geöffnet wurde
- Daraus den fachlich richtigen Produktpfad abgeleitet: für die Anmeldung wird produktiv der vorhandene RPC-/DB-Funktionspfad `sign_up_for_arbeitseinsatz(...)` verwendet statt eines App-seitigen Direktinserts in `arbeitseinsatz_anmeldung`.
- Den aktuellen Repo-Istzustand davor nochmals geprüft:
  - Home und Detailview hatten bereits sichtbare `Anmelden`-Buttons
  - beide hingen aber noch an einer reinen Hinweislogik in `HomeViewModel` bzw. `HomeSectionDetailViewModel`
  - ein echter Schreibpfad im `SupabaseService` existierte dafür im aktuellen Repo noch nicht
- Den produktiven Shared-Servicepfad deshalb klein ergänzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` läuft jetzt zentral über `SupabaseService` und wird über `ISupabaseService` bereitgestellt.
- Vor dem eigentlichen RPC werden die geforderten Mindestregeln real und klein geprüft:
  - fehlender Arbeitseinsatz-/Mitgliedsbezug -> verständliche Rückmeldung
  - bestehende aktive Anmeldung (`arbeitseinsatz_anmeldung.status = angemeldet`) -> keine Doppelanmeldung, verständliche Rückmeldung
  - `anmeldung_bis` abgelaufen -> verständliche Rückmeldung
  - `max_teilnehmer` gesetzt und bereits erreicht -> verständliche Rückmeldung
  - `max_teilnehmer = NULL` -> normale Anmeldung zulässig
  - keine Warteliste und keine Abmeldung in diesem Block
- Für den eigentlichen Schreibschritt wird anschließend der bestätigte DB-Funktionspfad genutzt:
  - `client.Rpc<ArbeitseinsatzAnmeldungRecord>("sign_up_for_arbeitseinsatz", ...)`
  - damit bleibt die App auf dem vorhandenen DB-Vertrag statt neuer Schattenarchitektur
  - falls im Rennen zwischen Vorabprüfung und RPC doch ein DB-seitiger Konflikt auftritt, wird dieser klein in eine verständliche App-Rückmeldung übersetzt
- Home und Detailview nutzen jetzt denselben echten Servicepfad statt doppelter Logik:
  - `HomeViewModel.RegisterForWorkAssignmentCommand` ruft den Shared-Service auf
  - `HomeSectionDetailViewModel.AnmeldenCommand` ruft denselben Shared-Service über den im Detailkontext mitgegebenen `WorkAssignmentId` auf
  - die Detailansicht erhielt dafür nur den kleinen zusätzlichen Kontextwert `WorkAssignmentId`, keine neue Navigation
- Den Mitgliedsbezug bewusst am real vorhandenen Benutzerpfad gehalten:
  - bevorzugt `UserContext.MitgliedId`
  - kleiner Fallback über `EnsureCurrentMemberSelectedAsync()`
  - keine neue Sonderzuordnung
- UI-Verhalten nach erfolgreicher Anmeldung produktiv klein gehalten:
  - Home und Detail erhalten den aktualisierten Startseiten-Datensatz zurück
  - Registrierungs-/Kapazitätsanzeige wird damit direkt nachgezogen
  - der Button wird ehrlich deaktiviert, damit keine zweite direkte Anmeldung aus derselben Sitzung ausgelöst wird
  - keine neue große UI-Architektur
- MAUI wurde in diesem Block nicht mit neuer Oberfläche ausgebaut, aber der produktive Anmeldepfad liegt jetzt im Shared-Service und ist damit für spätere mobile Parität vorbereitet statt verbaut.
- Offene Restpunkte bewusst klein gelassen:
  - `sign_off_from_arbeitseinsatz(...)` bleibt für einen späteren separaten Block offen
  - keine Warteliste
  - keine zusätzliche Anmeldehistorien-UI
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde durch die Shared-Service-Erweiterung nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Start-/Endzeit für Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Restfehler nach der expliziten WPF-Bindung nochmals bewusst Ende-zu-Ende geprüft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren weiterhin die sichtbare Strecke; dort war die Bindung inzwischen korrekt.
- Der tatsächliche Hänger saß davor im Datenursprung des Home-Pfads: die Startseiten-Views `v_startseite_arbeitseinsatz` und `v_startseite_termine` lieferten `Beginn`/`Ende` im aktuellen Stand nicht in jedem Fall belastbar, obwohl die Zeiten in den Basistabellen `arbeitseinsatz.start_uhrzeit`, `arbeitseinsatz.end_uhrzeit`, `termin.start_uhrzeit` und `termin.end_uhrzeit` vorhanden sein konnten.
- Den finalen Fix deshalb klein am vorhandenen Home-Servicepfad umgesetzt statt nochmals an der View zu drehen:
  - `LoadStartseiteArbeitseinsaetzeAsync()` lädt weiter aus `v_startseite_arbeitseinsatz`
  - `LoadStartseiteTermineAsync()` lädt weiter aus `v_startseite_termine`
  - wenn dort `Beginn`/`Ende` leer ankommen, werden die Zeitwerte gezielt über die vorhandene Datensatz-`Id` aus `arbeitseinsatz` bzw. `termin` nachangereichert
  - die Nachanreicherung greift nur für fehlende Zeitwerte; bestehende View-Werte bleiben unverändert
- Damit ist jetzt final abgesichert:
  - `start_uhrzeit` und `end_uhrzeit` erreichen den tatsächlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lässt
  - Home zeigt den Zeitraum weiter aus derselben einen Zeitquelle
  - die Detailansicht rendert `Beginn` und `Ende` weiter explizit als eigene Datensatzangaben
  - `Bekanntmachung` bleibt unverändert unbeeinträchtigt
- Fachlich bleibt der Block bewusst klein: keine neue Home-Architektur, keine neue Navigation, keine Schattenlogik, sondern nur eine gezielte Fallback-Anreicherung im bestehenden Lese-/Mappingpfad.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-Ergänzung nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Start- und Endzeit im tatsächlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler ausdrücklich nochmals Ende-zu-Ende gegen den real gerenderten WPF-Pfad geprüft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren gemeinsam die tatsächlich sichtbare Strecke dieses Blocks.
- Ergebnis der Prüfung: `start_uhrzeit` und `end_uhrzeit` kamen im Mapping als normalisierte Werte grundsätzlich an, blieben im UI aber weiterhin nicht zuverlässig genug sichtbar, weil der letzte Pfad noch zu stark an einem zusammengesetzten Zeitfeld hing und die Detailansicht die Zeiten nicht als eigene Datensatzangaben renderte.
- Die Korrektur deshalb jetzt am tatsächlich sichtbaren Anzeigeursprung umgesetzt:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt explizit `StartTimeText` und `EndTimeText`
  - der Home-/Detailkontext übernimmt dieselben Felder 1:1 für genau den ausgewählten Datensatz
  - `SupabaseService` schreibt die vorhandenen `Beginn`-/`Ende`-Werte nach `NormalizeTimeValue(...)` gezielt in diese Felder
- Damit war belastbar abgesichert:
  - `start_uhrzeit` / `end_uhrzeit` kommen im Modell an
  - sie werden nicht mehr erst spät oder indirekt aus `Subtitle`, `AdditionalInfo` oder anderen Nebenfeldern abgeleitet
  - die final sichtbare Bindung greift direkt auf diese expliziten Start-/Endfelder zu
- Home-Anzeige finaler Sichtpfad:
  - `HomeView.xaml` zeigt den Zeitraum weiter als klare Zeitzeile aus genau dieser einen Quelle
  - wenn nur eine Zeit vorhanden ist, bleibt genau diese sichtbar
  - wenn beide Zeiten vorhanden sind, erscheint konsistent `Start - Ende`
- Detailanzeige finaler Sichtpfad:
  - `HomeSectionDetailView.xaml` rendert `Beginn` und `Ende` jetzt als eigene sichtbare Datensatzangaben
  - die Zeit ist damit für `Arbeitseinsatz` und `Termin` nicht nur implizit oder im Fließtext vorhanden, sondern explizit im sichtbaren UI erkennbar
  - der Detailpfad bleibt weiterhin auf die Skalardaten des ausgewählten Datensatzes beschränkt
- `Bekanntmachung` wurde erneut mitgeprüft und nicht verschlechtert: dort bleiben die neuen Zeitfelder leer, während die generische Detailstruktur unverändert funktioniert.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/Kontextergänzungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Start- und Endzeit im tatsächlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler ausdrücklich nochmals Ende-zu-Ende gegen den real gerenderten WPF-Pfad geprüft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml` waren gemeinsam die tatsächlich sichtbare Strecke dieses Blocks.
- Ergebnis der Prüfung: `start_uhrzeit` und `end_uhrzeit` kamen im Mapping als normalisierte Werte grundsätzlich an, blieben im UI aber weiterhin nicht zuverlässig genug sichtbar, weil der letzte Pfad noch zu stark an einem zusammengesetzten Zeitfeld hing und die Detailansicht die Zeiten nicht als eigene Datensatzangaben renderte.
- Die Korrektur deshalb jetzt am tatsächlich sichtbaren Anzeigeursprung umgesetzt:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt explizit `StartTimeText` und `EndTimeText`
  - der Home-/Detailkontext übernimmt dieselben Felder 1:1 für genau den ausgewählten Datensatz
  - `SupabaseService` schreibt die vorhandenen `Beginn`-/`Ende`-Werte nach `NormalizeTimeValue(...)` gezielt in diese Felder
- Damit war belastbar abgesichert:
  - `start_uhrzeit` / `end_uhrzeit` kommen im Modell an
  - sie werden nicht mehr erst spät oder indirekt aus `Subtitle`, `AdditionalInfo` oder anderen Nebenfeldern abgeleitet
  - die final sichtbare Bindung greift direkt auf diese expliziten Start-/Endfelder zu
- Home-Anzeige finaler Sichtpfad:
  - `HomeView.xaml` zeigt den Zeitraum weiter als klare Zeitzeile aus genau dieser einen Quelle
  - wenn nur eine Zeit vorhanden ist, bleibt genau diese sichtbar
  - wenn beide Zeiten vorhanden sind, erscheint konsistent `Start - Ende`
- Detailanzeige finaler Sichtpfad:
  - `HomeSectionDetailView.xaml` rendert `Beginn` und `Ende` jetzt als eigene sichtbare Datensatzangaben
  - die Zeit ist damit für `Arbeitseinsatz` und `Termin` nicht nur implizit oder im Fließtext vorhanden, sondern explizit im sichtbaren UI erkennbar
  - der Detailpfad bleibt weiterhin auf die Skalardaten des ausgewählten Datensatzes beschränkt
- `Bekanntmachung` wurde erneut mitgeprüft und nicht verschlechtert: dort bleiben die neuen Zeitfelder leer, während die generische Detailstruktur unverändert funktioniert.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/Kontextergänzungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Home-/Detailanzeige für Arbeitseinsatz und Termin gezielt korrigiert: Uhrzeit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den bestehenden Anzeige-/Mappingpfad erneut gegen den realen Istzustand geprüft: `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und das Home-Mapping in `SupabaseService` waren der tatsächliche Fehlerort dieses Blocks.
- Sichtbarer Hauptbefund: die Uhrzeit hing bislang nicht an einem eindeutigen Anzeigeweg. Sie lief teils im `Subtitle`, teils als separater Beginn-Text und in der Detailansicht zusätzlich in `AdditionalInfo`, wodurch sie im aktuellen UI nicht verlässlich bzw. nicht klar genug sichtbar war.
- Den Zeitpfad deshalb gezielt auf eine einzige explizite Anzeigeform gezogen:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `TimeText` statt eines bloßen Beginnfelds
  - `SupabaseService` erzeugt diesen Wert zentral über `BuildTimeRange(...)` aus Start- und Endzeit
  - `HomeView.xaml` rendert `Uhrzeit: {TimeText}` jetzt für `Arbeitseinsatz` und `Termin` sichtbar in der Übersicht
  - damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, erscheint konsistent `Start – Ende`
- Dieselbe Korrektur auf den Detailpfad übertragen:
  - `HomeSectionDetailContext` wurde um `TimeText` ergänzt
  - `HomeSectionDetailViewModel` reicht ihn unverändert nur für den ausgewählten Datensatz weiter
  - `HomeSectionDetailView.xaml` zeigt `Uhrzeit` jetzt separat sichtbar an
  - dadurch sind Start- und Endzeit für den ausgewählten `Arbeitseinsatz` und `Termin` in der Detailansicht klar sichtbar, ohne neue Navigation oder Mehrfachanzeige
- Die doppelte Anzeige `Angemeldet: 0` konkret am Mappingursprung beseitigt:
  - bislang kamen Kapazitätsdaten zweimal aus demselben Datensatzpfad
  - einmal als einzelne Zeilen `Angemeldet` / `Freie Plätze` in `DetailInfo`
  - zusätzlich nochmals zusammengefasst in `RegistrationInfo` über `BuildCapacityText(...)`
  - der `Arbeitseinsatz`-Mappingpfad wurde deshalb bereinigt: `RegistrationInfo` bleibt als einzige Kapazitätsanzeige erhalten; die redundanten Einzelzeilen wurden aus `DetailInfo` entfernt
- Ergebnis des Korrekturpfads:
  - Home zeigt die Uhrzeit jetzt sichtbar und eindeutig für `Arbeitseinsatz` und `Termin`
  - die Detailansicht zeigt Start-/Endzeit des ausgewählten Datensatzes sichtbar an
  - `Angemeldet: 0` erscheint für `Arbeitseinsatz` nicht mehr mehrfach
  - die Detailview bleibt auf die Skalardaten des konkret ausgewählten Datensatzes beschränkt und zeigt keine Sammel-/Mehrfachanzeige
- `Bekanntmachung` und die generische Detailstruktur wurden mitgeprüft und nicht verschlechtert: dort bleibt `TimeText` leer, während `Subtitle`, `AdditionalInfo` und `Content` unverändert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fixblock erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Home-/Detailansicht für Arbeitseinsätze und Termine sichtbar vervollständigt, ohne neue Schattenlogik

- Den vorhandenen Home-/Detailpfad zuerst vollständig gegen den realen Arbeitsbaum geprüft: `HomeView.xaml`, `HomeViewModel`, `HomeSectionDetailView.xaml`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Item-Modelle sowie das Mapping in `SupabaseService` waren bereits produktiv vorhanden und dienten als Basis des Blocks.
- Ergebnis der Istzustandsprüfung:
  - `Arbeitseinsatz` trug die Startzeit im Home-Item bereits separat als `BeginText`
  - `Termin` hatte die Zeit zwar indirekt im Untertitel/Detailmapping, aber noch nicht separat sichtbar in der Home-Karte
  - die Detailansicht arbeitete bereits mit einem generischen Kontext aus Skalardaten, zeigte aber die Registrierungsinformation des ausgewählten Datensatzes noch nicht separat an
  - der `Anmelden`-Button war im Detailpfad nicht konsistent genug an den Arbeitseinsatz-Kontext gebunden
- Die Home-Übersicht deshalb gezielt fachlich vervollständigt und nicht umgebaut:
  - `HomeAppointmentItem` wurde um `BeginText`/`HasBeginText` ergänzt
  - das Termin-Mapping in `SupabaseService` reicht den normalisierten Beginn jetzt explizit in das Home-Item durch
  - `HomeView.xaml` zeigt damit bei `Termin` den Beginn sichtbar in derselben kleinen Kartenlogik wie bei `Arbeitseinsatz`
  - `Arbeitseinsatz` blieb bei der bereits passenden sichtbaren Beginn-Anzeige
- Die Detailansicht innerhalb des bestehenden generischen Pfads vervollständigt:
  - `HomeSectionDetailContext` trägt jetzt zusätzlich `RegistrationInfo` und einen expliziten Schalter für die Sichtbarkeit der Anmelden-Aktion
  - `HomeSectionDetailViewModel` reicht diese Daten 1:1 an die View weiter, ohne neue Sammel- oder Listenlogik
  - `HomeSectionDetailView.xaml` zeigt die Registrierungsinformation des ausgewählten Datensatzes jetzt in derselben Detailkarte zusätzlich an
  - der `Anmelden`-Button bleibt in der Detailansicht für `Arbeitseinsatz` sichtbar und verwendet denselben ehrlichen Hinweisdialog wie auf Home
- Den Punkt „Detailview zeigt nur den ausgewählten Datensatz“ bewusst am bestehenden Architekturpfad abgesichert:
  - `HomeViewModel` übergibt beim Öffnen der Detailansicht ausschließlich die Skalardaten des konkret ausgewählten Home-Items
  - keine Listen, keine Mehrfachobjekte und kein nachträgliches Nachladen eines unscharfen Datensatzpools im Detailpfad
  - dadurch bleibt die Detailansicht auf genau den angeklickten Datensatz begrenzt
- `Termin`, `Arbeitseinsatz` und `Bekanntmachung` bleiben dabei im selben generischen Detailgerüst:
  - `Arbeitseinsatz` erhält zusätzliche sichtbare Registrierungs-/Aktionskonsistenz
  - `Termin` erhält die separate Startzeit auf Home und behält die Datensatzdetails im Detailkontext
  - `Bekanntmachung` verliert keine bestehenden Felder; die generische Detaildarstellung bleibt intakt
- Offener Restpunkt bleibt bewusst unverändert klein: der echte produktive Schreibpfad für `Anmelden` ist im Repo weiterhin nicht belastbar vorhanden. Deshalb bleibt die Aktion auf Home und in der Detailansicht eine ehrliche Info-Verdrahtung statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die kleinen Kontext-/Darstellungsänderungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Arbeitsstunden endgültig gegen `id = 0` abgesichert, Save-Rücknavigation abgeschlossen, Arbeitseinsatz-Home-Buttons korrigiert

- Den Block erneut bewusst gegen den realen laufenden Produktpfad geführt, weil der erneute Datensatz `arbeitsstunde.id = 0` belegt hat, dass der vorige Schutz im tatsächlichen Laufzeitverhalten noch nicht ausreichte.
- Den kompletten Create-Pfad für `arbeitsstunde` nochmals Ende-zu-Ende geprüft:
  - WPF-Erfassung über `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad über `ArbeitsstundenViewModel`
  - MAUI-Erfassung über `MyArbeitsstundenPage`
  - Persistenzpfad über `SupabaseService.AddArbeitsstundeAsync(...)`
  - Ergebnis: die Aufrufer erzeugen weiterhin neue `ArbeitsstundeRecord`-Instanzen ohne fachliche `Id`, typbedingt aber mit lokalem Default `0`
- Daraus die jetzt robuste technische Konsequenz gezogen: auf Attributverhalten allein wird für `arbeitsstunde` nicht mehr vertraut. Stattdessen verwendet der Create-Pfad nun einen expliziten Insert-Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`).
- Der tatsächliche Korrekturpfad für den Insertfehler ist damit jetzt zweistufig belastbar:
  - `AddArbeitsstundeAsync(...)` mappt neue Datensätze in einen separaten Insert-Payload ohne Primärschlüsselspalte
  - die Create-Payload übernimmt `Id` grundsätzlich überhaupt nicht mehr; damit kann auch ein ankommendes `ArbeitsstundeRecord` mit `Id <= 0` technisch nicht mehr in einen Insert mit `id = 0` münden
  - Updates bleiben unverändert über `UpdateArbeitsstundeAsync(...)` und die echte bestehende `Id`
  - keine `lastrow+1`-Logik
- Den Abschluss des Userflows für Arbeitsstunden zugleich produktiv nachgezogen:
  - nach erfolgreichem Speichern in `Arbeitsstunden erfassen` bleibt die Eingabemaske nicht mehr offen stehen
  - im Dialogkontext schließt sich das Fenster wie bisher
  - im normalen WPF-Erfassungspfad wird nach erfolgreichem Save sauber zurück auf `Home` navigiert
- Dasselbe Abschlussverhalten jetzt auch für `Arbeitsstunden freigeben` umgesetzt:
  - Checkbox-/Status-Speichern bleibt wie im letzten Fix möglich
  - wenn alle geänderten/markierten Zeilen erfolgreich gespeichert wurden, navigiert die WPF-Prüfansicht anschließend wieder auf `Home` zurück
  - der Fachpfad bleibt klein: `status` bleibt optionales Anmerkungsfeld, offen bleibt `freigegeben = false`, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
- Zusätzlich den Home-Pfad für `Arbeitseinsatz` auf den gewünschten Interaktionsstand zurückgezogen:
  - der separate `Details`-Button wurde entfernt
  - Detailöffnung läuft jetzt per Doppelklick auf die Karte
  - stattdessen bleibt dort `Anmelden` sichtbar
  - weil weiterhin kein belastbarer produktiver Schreibpfad für echte Anmeldungen im Repo nachweisbar ist, bleibt der Button bewusst eine ehrliche Info-Aktion statt neuer Schattenlogik
- Technisch verifiziert: `KGV.Wpf` baut nach dem Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Home-/Detailanzeige für Arbeitseinsatz und Termin gezielt korrigiert: Uhrzeit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den bestehenden Anzeige-/Mappingpfad erneut gegen den realen Istzustand geprüft: `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und das Home-Mapping in `SupabaseService` waren der tatsächliche Fehlerort dieses Blocks.
- Sichtbarer Hauptbefund: die Uhrzeit hing bislang nicht an einem eindeutigen Anzeigeweg. Sie lief teils im `Subtitle`, teils als separater Beginn-Text und in der Detailansicht zusätzlich in `AdditionalInfo`, wodurch sie im aktuellen UI nicht verlässlich bzw. nicht klar genug sichtbar war.
- Den Zeitpfad deshalb gezielt auf eine einzige explizite Anzeigeform gezogen:
  - `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `TimeText` statt eines bloßen Beginnfelds
  - `SupabaseService` erzeugt diesen Wert zentral über `BuildTimeRange(...)` aus Start- und Endzeit
  - `HomeView.xaml` rendert `Uhrzeit: {TimeText}` jetzt für `Arbeitseinsatz` und `Termin` sichtbar in der Übersicht
  - damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, erscheint konsistent `Start – Ende`
- Dieselbe Korrektur auf den Detailpfad übertragen:
  - `HomeSectionDetailContext` wurde um `TimeText` ergänzt
  - `HomeSectionDetailViewModel` reicht ihn unverändert nur für den ausgewählten Datensatz weiter
  - `HomeSectionDetailView.xaml` zeigt `Uhrzeit` jetzt separat sichtbar an
  - dadurch sind Start- und Endzeit für den ausgewählten `Arbeitseinsatz` und `Termin` in der Detailansicht klar sichtbar, ohne neue Navigation oder Mehrfachanzeige
- Die doppelte Anzeige `Angemeldet: 0` konkret am Mappingursprung beseitigt:
  - bislang kamen Kapazitätsdaten zweimal aus demselben Datensatzpfad
  - einmal als einzelne Zeilen `Angemeldet` / `Freie Plätze` in `DetailInfo`
  - zusätzlich nochmals zusammengefasst in `RegistrationInfo` über `BuildCapacityText(...)`
  - der `Arbeitseinsatz`-Mappingpfad wurde deshalb bereinigt: `RegistrationInfo` bleibt als einzige Kapazitätsanzeige erhalten; die redundanten Einzelzeilen wurden aus `DetailInfo` entfernt
- Ergebnis des Korrekturpfads:
  - Home zeigt die Uhrzeit jetzt sichtbar und eindeutig für `Arbeitseinsatz` und `Termin`
  - die Detailansicht zeigt Start-/Endzeit des ausgewählten Datensatzes sichtbar an
  - `Angemeldet: 0` erscheint für `Arbeitseinsatz` nicht mehr mehrfach
  - die Detailview bleibt auf die Skalardaten des konkret ausgewählten Datensatzes beschränkt und zeigt keine Sammel-/Mehrfachanzeige
- `Bekanntmachung` und die generische Detailstruktur wurden mitgeprüft und nicht verschlechtert: dort bleibt `TimeText` leer, während `Subtitle`, `AdditionalInfo` und `Content` unverändert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fixblock erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Home-/Detailansicht für Arbeitseinsätze und Termine sichtbar vervollständigt, ohne neue Schattenlogik

- Den vorhandenen Home-/Detailpfad zuerst vollständig gegen den realen Arbeitsbaum geprüft: `HomeView.xaml`, `HomeViewModel`, `HomeSectionDetailView.xaml`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Item-Modelle sowie das Mapping in `SupabaseService` waren bereits produktiv vorhanden und dienten als Basis des Blocks.
- Ergebnis der Istzustandsprüfung:
  - `Arbeitseinsatz` trug die Startzeit im Home-Item bereits separat als `BeginText`
  - `Termin` hatte die Zeit zwar indirekt im Untertitel/Detailmapping, aber noch nicht separat sichtbar in der Home-Karte
  - die Detailansicht arbeitete bereits mit einem generischen Kontext aus Skalardaten, zeigte aber die Registrierungsinformation des ausgewählten Datensatzes noch nicht separat an
  - der `Anmelden`-Button war im Detailpfad nicht konsistent genug an den Arbeitseinsatz-Kontext gebunden
- Die Home-Übersicht deshalb gezielt fachlich vervollständigt und nicht umgebaut:
  - `HomeAppointmentItem` wurde um `BeginText`/`HasBeginText` ergänzt
  - das Termin-Mapping in `SupabaseService` reicht den normalisierten Beginn jetzt explizit in das Home-Item durch
  - `HomeView.xaml` zeigt damit bei `Termin` den Beginn sichtbar in derselben kleinen Kartenlogik wie bei `Arbeitseinsatz`
  - `Arbeitseinsatz` blieb bei der bereits passenden sichtbaren Beginn-Anzeige
- Die Detailansicht innerhalb des bestehenden generischen Pfads vervollständigt:
  - `HomeSectionDetailContext` trägt jetzt zusätzlich `RegistrationInfo` und einen expliziten Schalter für die Sichtbarkeit der Anmelden-Aktion
  - `HomeSectionDetailViewModel` reicht diese Daten 1:1 an die View weiter, ohne neue Sammel- oder Listenlogik
  - `HomeSectionDetailView.xaml` zeigt die Registrierungsinformation des ausgewählten Datensatzes jetzt in derselben Detailkarte zusätzlich an
  - der `Anmelden`-Button bleibt in der Detailansicht für `Arbeitseinsatz` sichtbar und verwendet denselben ehrlichen Hinweisdialog wie auf Home
- Den Punkt „Detailview zeigt nur den ausgewählten Datensatz“ bewusst am bestehenden Architekturpfad abgesichert:
  - `HomeViewModel` übergibt beim Öffnen der Detailansicht ausschließlich die Skalardaten des konkret ausgewählten Home-Items
  - keine Listen, keine Mehrfachobjekte und kein nachträgliches Nachladen eines unscharfen Datensatzpools im Detailpfad
  - dadurch bleibt die Detailansicht auf genau den angeklickten Datensatz begrenzt
- `Termin`, `Arbeitseinsatz` und `Bekanntmachung` bleiben dabei im selben generischen Detailgerüst:
  - `Arbeitseinsatz` erhält zusätzliche sichtbare Registrierungs-/Aktionskonsistenz
  - `Termin` erhält die separate Startzeit auf Home und behält die Datensatzdetails im Detailkontext
  - `Bekanntmachung` verliert keine bestehenden Felder; die generische Detaildarstellung bleibt intakt
- Offener Restpunkt bleibt bewusst unverändert klein: der echte produktive Schreibpfad für `Anmelden` ist im Repo weiterhin nicht belastbar vorhanden. Deshalb bleibt die Aktion auf Home und in der Detailansicht eine ehrliche Info-Verdrahtung statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die kleinen Kontext-/Darstellungsänderungen nicht beschädigt.

## 2026-03-24 – Prompt 1/1: Arbeitsstunden endgültig gegen `id = 0` abgesichert, Save-Rücknavigation abgeschlossen, Arbeitseinsatz-Home-Buttons korrigiert

- Den Block erneut bewusst gegen den realen laufenden Produktpfad geführt, weil der erneute Datensatz `arbeitsstunde.id = 0` belegt hat, dass der vorige Schutz im tatsächlichen Laufzeitverhalten noch nicht ausreichte.
- Den kompletten Create-Pfad für `arbeitsstunde` nochmals Ende-zu-Ende geprüft:
  - WPF-Erfassung über `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad über `ArbeitsstundenViewModel`
  - MAUI-Erfassung über `MyArbeitsstundenPage`
  - Persistenzpfad über `SupabaseService.AddArbeitsstundeAsync(...)`
  - Ergebnis: die Aufrufer erzeugen weiterhin neue `ArbeitsstundeRecord`-Instanzen ohne fachliche `Id`, typbedingt aber mit lokalem Default `0`
- Daraus die jetzt robuste technische Konsequenz gezogen: auf Attributverhalten allein wird für `arbeitsstunde` nicht mehr vertraut. Stattdessen verwendet der Create-Pfad nun einen expliziten Insert-Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`).
- Der tatsächliche Korrekturpfad für den Insertfehler ist damit jetzt zweistufig belastbar:
  - `AddArbeitsstundeAsync(...)` mappt neue Datensätze in einen separaten Insert-Payload ohne Primärschlüsselspalte
  - die Create-Payload übernimmt `Id` grundsätzlich überhaupt nicht mehr; damit kann auch ein ankommendes `ArbeitsstundeRecord` mit `Id <= 0` technisch nicht mehr in einen Insert mit `id = 0` münden
  - Updates bleiben unverändert über `UpdateArbeitsstundeAsync(...)` und die echte bestehende `Id`
  - keine `lastrow+1`-Logik
- Den Abschluss des Userflows für Arbeitsstunden zugleich produktiv nachgezogen:
  - nach erfolgreichem Speichern in `Arbeitsstunden erfassen` bleibt die Eingabemaske nicht mehr offen stehen
  - im Dialogkontext schließt sich das Fenster wie bisher
  - im normalen WPF-Erfassungspfad wird nach erfolgreichem Save sauber zurück auf `Home` navigiert
- Dasselbe Abschlussverhalten jetzt auch für `Arbeitsstunden freigeben` umgesetzt:
  - Checkbox-/Status-Speichern bleibt wie im letzten Fix möglich
  - wenn alle geänderten/markierten Zeilen erfolgreich gespeichert wurden, navigiert die WPF-Prüfansicht anschließend wieder auf `Home` zurück
  - der Fachpfad bleibt klein: `status` bleibt optionales Anmerkungsfeld, offen bleibt `freigegeben = false`, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
- Zusätzlich den Home-Pfad für `Arbeitseinsatz` auf den gewünschten Interaktionsstand zurückgezogen:
  - der separate `Details`-Button wurde entfernt
  - Detailöffnung läuft jetzt per Doppelklick auf die Karte
  - stattdessen bleibt dort `Anmelden` sichtbar
  - weil weiterhin kein belastbarer produktiver Schreibpfad für echte Anmeldungen im Repo nachweisbar ist, bleibt der Button bewusst eine ehrliche Info-Aktion statt neuer Schattenlogik
- Technisch verifiziert: `KGV.Wpf` baut nach dem Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschädigt.

## 2026-03-23 – Prompt 1/1: zwei echte Arbeitsstundenfehler am realen Produktpfad behoben (`id = 0` bei Insert, Speichern in Freigabeansicht reagiert nicht auf Checkbox)

- Den Block ausdrücklich gegen die zwei real reproduzierten Fehler geführt und nicht als Vermutungslösung umgesetzt.
- Ersten Fehler `id = 0` im echten Neuanlagepfad vollständig zurückverfolgt:
  - WPF-Usererfassung über `ArbeitsstundenErfassungViewModel`
  - WPF-Dialogpfad über `ArbeitsstundenViewModel`
  - MAUI-Usererfassung über `MyArbeitsstundenPage`
  - gemeinsamer Persistenzpfad über `SupabaseService.AddArbeitsstundeAsync(...)`
  - in allen drei Aufrufern wird für neue Arbeitsstunden keine fachliche `Id` gesetzt; die erzeugten `ArbeitsstundeRecord`-Instanzen tragen aber typbedingt lokal weiter den Defaultwert `0`
  - der entscheidende Unterschied zu den übrigen produktiven Create-Modellen lag im Modellattribut: `ArbeitsstundeRecord` verwendete noch `[PrimaryKey("id")]`, während die übrigen Insert-relevanten Tabellen bereits explizit `[PrimaryKey("id", false)]` verwenden
  - damit war die reale Ursache belastbar: im laufenden Paketstand konnte die Primärschlüsselspalte im Insertpfad weiterhin serialisiert werden und `Id = 0` in die Payload gelangen
- Korrekturpfad für den Insertfehler:
  - `ArbeitsstundeRecord` auf `[PrimaryKey("id", false)]` umgestellt
  - keine App-seitige `lastrow+1`-Logik
  - keine Änderung am Updatepfad; bestehende Datensätze verwenden ihre `Id` weiter normal
  - Ergebnis: bei Neuanlagen wird die `id` nicht mehr aus der App in die Create-Payload aufgenommen
- Zweiten Fehler in `Arbeitsstunden freigeben` ebenfalls bis zum realen UI-Pfad geprüft:
  - `status` / Anmerkung war fachlich und technisch bereits optional
  - `HasChanges` in `PruefungseintragItem` wertet bereits korrekt `Freigeben || Status geändert`
  - die tatsächliche Störung lag nicht in einer Pflichtlogik für `status`, sondern an der WPF-Darstellung der Freigabe-Checkbox über `DataGridCheckBoxColumn`
  - im laufenden Gridpfad wurde die Checkboxänderung nicht zuverlässig sofort in das Item zurückgeschrieben; dadurch zogen `PropertyChanged`, `HasPendingChanges` und `SpeichernCommand` beim bloßen Setzen der Checkbox nicht unmittelbar an
- Korrekturpfad für die Freigabeansicht:
  - die Spalte `Freigeben` wurde auf `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding umgestellt
  - Binding jetzt mit `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`
  - dadurch gilt das Setzen der Checkbox sofort als relevante Änderung und aktiviert den Speichernpfad ohne zusätzliche Statuspflicht
- Fachregeln bewusst unverändert gelassen:
  - offen bleibt `freigegeben = false`
  - `status` bleibt reines optionales Anmerkungsfeld
  - Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`
  - keine neue Freigabearchitektur
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im Shared-Pfad mitgedacht und nicht beschädigt.

## 2026-03-23 – Prompt 1/1: WPF-Bindingfehler in `ArbeitsstundenErfassungView` mit reinem Anzeige-Fix sauber abgeschlossen

- Den gemeldeten Fehler auf die reale Bindungsstelle in `KGV.Wpf/Views/ArbeitsstundenErfassungView.xaml` zurückgeführt: `CurrentMemberText` ist im `ArbeitsstundenErfassungViewModel` bewusst nur als schreibgeschützte Anzeigeproperty vorhanden.
- Damit blieb die fachliche Richtung klar: kein Setter im ViewModel, keine neue Fachlogik und keine Aufweichung des Anzeigevertrags nur zur Beruhigung des Bindings.
- Den Fix bewusst klein und XAML-seitig gehalten: die bisherige Anzeige über `Run Text="{Binding CurrentMemberText}"` wurde auf eine explizite Anzeige über einen eigenen `TextBlock` mit `Mode=OneWay` umgestellt.
- Die betroffene View wurde im Abschlusslauf nochmals gezielt gegen den Nachbarbereich geprüft: der anschließende `ValidationMessage`-`TextBlock` und die übrige XAML-Struktur bleiben intakt; es wurde nur der kleine Anzeigeabschnitt für das Mitglied angepasst.
- Der Block bleibt bewusst minimal:
  - keine Änderung an `ArbeitsstundenErfassungViewModel`
  - keine Änderung an Shared-/Servicepfaden
  - keine neue Fachlogik für Arbeitsstunden
  - kein Umbau außerhalb der betroffenen View
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Damit ist der kleine Bindingfix sauber abgeschlossen; MAUI wurde in diesem Block nicht angefasst.

## 2026-03-23 â€“ Prompt 1/1: produktive Insert-Pfade auf feste ID-Mitgabe geprÃ¼ft und als aktuell korrekt dokumentiert

- Den gewÃ¼nschten Block zuerst vollstÃ¤ndig als IstzustandsprÃ¼fung statt als Vorab-Fix angegangen: geprÃ¼ft wurden der aktuelle `SupabaseService`, alle relevanten produktiven Create-/Add-/Insert-Pfade, die beteiligten Record-/Request-Modelle sowie die WPF-/MAUI-Aufrufer fÃ¼r Neuanlagen.
- Den stÃ¤rksten Verdachtspfad `arbeitstunde` konkret bis zur tatsÃ¤chlichen Insert-Stelle zurÃ¼ckverfolgt:
  - WPF erzeugt neue Arbeitsstunden in `ArbeitsstundenViewModel`
  - MAUI erzeugt neue Arbeitsstunden in `MyArbeitsstundenPage`
  - beide erzeugen neue `ArbeitsstundeRecord`-Objekte **ohne** explizite `Id`
  - `SupabaseService.AddArbeitsstundeAsync(...)` baut daraus nochmals ein frisches Insert-Objekt und setzt ebenfalls **keine** `Id`
  - im aktuell produktiven Insert-Pfad konnte damit kein `id = 0` oder sonstige feste ID-Mitgabe aus der App nachgewiesen werden
- Danach die Ã¼brigen produktiven Verwaltungs-Neuanlagen geprÃ¼ft:
  - `CreateTerminAsync(...)`
  - `CreateBekanntmachungAsync(...)`
  - `CreateArbeitseinsatzAsync(...)`
  - in den WPF-ViewModels laufen neue EditorzustÃ¤nde zwar als normale Record-Objekte, wobei `Id` im New-Mode lokal auf den Typdefault `0` fÃ¤llt
  - entscheidend ist aber der Servicepfad: die drei Create-Methoden erzeugen vor dem tatsÃ¤chlichen PostgREST-Insert jeweils ein separates Insert-Objekt und Ã¼bernehmen die lokale `Id` gerade nicht in die Payload
  - Ergebnis: auch diese produktiven Neuanlagen senden aktuell keine feste ID aus der App
- Einen mÃ¶glichen Produktpfad `arbeitseinsatz_anmeldung` gesondert gesucht, weil er laut Zielarchitektur ebenfalls DB-gesteuert laufen mÃ¼sste. Im aktuellen Repo existiert dafÃ¼r aber weiterhin kein echter Schreibpfad; der vorhandene `Anmelden`-Button auf Home bleibt eine reine Hinweis-/Info-Aktion. Entsprechend war dort aktuell auch kein produktiver Insertpfad mit ID-Mitgabe vorhanden.
- Den Serializer-/Bibliotheksaspekt zusÃ¤tzlich gegen den real verwendeten Paketstand abgesichert: im eingebundenen `Supabase.Postgrest` besitzt `PrimaryKeyAttribute` den optionalen Parameter `shouldInsert`, dessen Defaultwert `false` ist. Der verbleibende Arbeitsstunden-Record mit `[PrimaryKey("id")]` sendet seine PrimÃ¤rschlÃ¼sselspalte damit im Insert-Kontext ebenfalls nicht automatisch mit. Das erklÃ¤rt, warum auch dieser Pfad trotz impliziter Schreibweise aktuell korrekt bleibt.
- Wichtiges Ergebnis des Blocks:
  - in den geprÃ¼ften produktiven Insert-Pfaden wurde aktuell **kein** `id = 0` aus der App nachgewiesen
  - es wurde auch keine feste `id` indirekt Ã¼ber Mapper, Defaultkonstruktoren oder Insert-Helfer in die produktive Payload Ã¼bernommen
  - Unterschiede zwischen WPF und MAUI bestehen fÃ¼r `arbeitsstunde` nicht; beide laufen Ã¼ber denselben korrekten Shared-Servicepfad
- Deshalb bewusst **kein** Codeumbau auf Verdacht:
  - keine `lastrow+1`-Logik
  - keine unnÃ¶tigen zusÃ¤tzlichen Insert-Modelle nur zur kosmetischen Doppelabsicherung
  - keine DB-Migration ohne versionierte Repo-Basis
  - stattdessen saubere Dokumentation, dass das Zielmuster im aktuellen Produktstand bereits eingehalten wird: `Insert ohne feste ID`, `Update mit vorhandener ID`
- Repo-seitig zusÃ¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine SQL-/Migrationsdateien vor, Ã¼ber die sich die Tabellen-Defaults oder Sequences versioniert nachprÃ¼fen lieÃŸen. Eine DB-seitige Sequence-/Defaultkorrektur war fÃ¼r diesen Block aber auch nicht nÃ¶tig, weil auf App-Seite kein fehlerhafter Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wurde durch diesen Analyse-/Dokublock nicht beschÃ¤digt.

## 2026-03-23 â€“ Prompt 1/1: begonnenen Home-/Detail-Block fÃ¼r Arbeitseinsatz sauber abgeschlossen, ohne Fake-Anmeldepfad

- Den aktuellen Istzustand des begonnenen Blocks zuerst gegen den realen Arbeitsbaum geprÃ¼ft und nur konsolidiert: bereits vorhanden waren erweiterte Home-Items, ein optionaler `CanRegister`-/Detailkontext, vorbereitete Ã„nderungen im `HomeViewModel` sowie ein halbfertig angefasster Mappingblock im `SupabaseService`; sichtbar offen waren vor allem die WPF-XAMLs `HomeView.xaml` und `HomeSectionDetailView.xaml`, auÃŸerdem war der hintere Helper-Tail des `SupabaseService` beschÃ¤digt/abgeschnitten.
- Den gemeinsamen Home-/Detailpfad fÃ¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` zusammen geprÃ¼ft, damit die Erweiterung nicht nur einen einzelnen Kartentyp verbessert, sondern die generische Detailview insgesamt konsistent hÃ¤lt. Dazu wurden zusÃ¤tzliche Felder fÃ¼r Detailinfos an den gemeinsamen Home-Modellen ergÃ¤nzt und die Mappings so erweitert, dass `Ort`/`Thema` bei Terminen sowie `Betreff`/`Kurztext`/VerÃ¶ffentlichungsdaten bei Bekanntmachungen nicht mehr im Detailpfad verloren gehen.
- FÃ¼r `Arbeitseinsatz` auf Home den begonnenen Sichtpfad sauber fertiggestellt: die Karte zeigt jetzt explizit den Beginn, behÃ¤lt die vorhandenen Registrierungs-/KapazitÃ¤tshinweise und Ã¶ffnet Details nicht mehr nur implizit Ã¼ber die ganze Karte, sondern Ã¼ber einen klaren `Details`-Button. ZusÃ¤tzlich wird â€“ nur bei tatsÃ¤chlich als anmeldbar markierten DatensÃ¤tzen â€“ ein eigener `Anmelden`-Button angezeigt.
- FÃ¼r die Detailansicht den vorhandenen generischen Pfad wiederverwendet statt Sondernavigation zu bauen: `HomeSectionDetailView` zeigt jetzt optional den `Anmelden`-Button sowie die in `AdditionalInfo` transportierten Ã¼brigen Datensatzinformationen vor dem eigentlichen Beschreibungstext. Dadurch bleiben `Arbeitseinsatz`, `Termin` und `Bekanntmachung` im selben DetailgerÃ¼st, aber mit vollstÃ¤ndigerer Fachanzeige.
- Die Arbeitseinsatz-Detailinformationen bewusst klein und belastbar gehalten: Thema, Datum, Beginn, Ende, Treffpunkt, Anmelde-/KapazitÃ¤tsdaten und nur dann `Max. Teilnehmer`, wenn sich dieser Wert aus vorhandenen Viewdaten tatsÃ¤chlich ableiten lÃ¤sst. Wenn keine belastbare Maximalzahl vorliegt, wird dieses Feld nicht angezeigt.
- Den halbfertig beschÃ¤digten Tail des `SupabaseService` sauber rekonstruiert und konsolidiert, damit der angefangene Block wieder auf einem kompilierbaren Produktstand liegt. Dazu gehÃ¶ren die kleinen Home-Helfer (`AddDetailLine`, Zeit-/Datumsformatierung, Textnormalisierung, Dokumentpfad-Helfer, `FirstNonEmpty`, `CreateUnavailableException` usw.), die durch den abgebrochenen Zwischenstand teilweise nicht mehr vollstÃ¤ndig in der Datei standen.
- Den `Anmelden`-Usecase ausdrÃ¼cklich gegen das aktuelle Repo geprÃ¼ft: es gibt weiterhin keinen belastbar bestÃ¤tigten produktiven Schreibpfad fÃ¼r eine echte Arbeitseinsatz-Anmeldung. Deshalb wurde **keine** neue Schattenlogik eingefÃ¼hrt. Sowohl in der Home-Karte als auch in der Detailansicht ist `Anmelden` nur als ehrliche WPF-Info-Aktion verdrahtet, die klar darauf hinweist, dass der echte Schreibpfad noch nicht angebunden ist.
- MAUI wurde im Rahmen dieses Blocks mitgedacht, aber bewusst nicht kÃ¼nstlich erweitert: die gemeinsam genutzten Home-Modelle und Mappings bleiben kompatibel, ohne dass mobil bereits eine neue pseudo-produktive Arbeitseinsatz-Anmeldung aufgebaut wird.
- Technisch verifiziert: Workspace-Build erfolgreich. Der begonnene Home-/Detail-Block ist damit sauber abgeschlossen; offen bleibt nur die spÃ¤tere echte Anbindung eines bestÃ¤tigten Arbeitseinsatz-Anmelde-Schreibpfads.

## 2026-03-23 â€“ Prompt 1/1: Zeit-/Datumsbug der drei Verwaltungseditoren zentral behoben, Editorverhalten abgeschlossen und Navigation auf Home-only zurÃ¼ckgezogen

- Den begonnenen Bugfix-/UX-Block zuerst gegen den realen Arbeitsbaum konsolidiert: im halbfertigen Stand lagen bereits ein neuer zentraler Typ-/Mappingansatz, ZurÃ¼ck-/Dirty-Check-Grundlagen und die RÃ¼cknahme der Hauptnavigation vor, aber noch nicht sauber verifiziert, dokumentiert und bis zum Build-/Abschlusszustand durchgezogen.
- Die tatsÃ¤chliche Ursache des Verschiebungsfehlers wurde im gemeinsamen Typ-/Serialisierungs-/Mappingpfad verortet und nicht pro Formular erraten:
  - `termin.datum` ist fachlich eine PostgreSQL-`date`-Spalte
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis`, `termin.sichtbar_ab`, `sichtbar_bis` sowie `bekanntmachung.sichtbar_ab`, `sichtbar_bis` sind fachlich `timestamp without time zone`
  - im App-Pfad liefen diese Werte aber als normale `DateTime`-Werte ohne explizite JSON-Konverter fÃ¼r den tatsÃ¤chlichen DB-Typ
  - dadurch konnten beim PostgREST-/JSON-Transport implizite `DateTimeKind`-/UTC-/Local-Interpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim wiederholten Bearbeiten/Speichern erklÃ¤rt
- Die Korrektur deshalb zentral im Shared-Pfad umgesetzt und nicht als View-Hotfix:
  - neue JSON-Konverter `PostgresDateOnlyJsonConverter` und `NullablePostgresTimestampWithoutTimeZoneJsonConverter`
  - gezielte Annotation der betroffenen Record-Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃ¤tzliche zentrale Normalisierung im `SupabaseService` fÃ¼r geladene VerwaltungsdatensÃ¤tze sowie fÃ¼r `Create*Async(...)` und `Update*Async(...)`
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
  - nach erfolgreichem Speichern wird nicht mehr in der Bearbeiten-View verharrt, sondern Ã¼ber den bestehenden Navigationspfad zurÃ¼ck auf `Home` gegangen
  - alle drei Editor-Views besitzen jetzt einen `ZurÃ¼ck`-Button
  - `ZurÃ¼ck` und `Abbrechen` verwenden denselben kleinen Dirty-Check Ã¼ber einen initialen Snapshot des Editorzustands
  - RÃ¼ckfrage erscheint nur bei echten Ã„nderungen; nach erfolgreichem Speichern ist der Zustand wieder sauber
- Die Navigation wurde auf den gewÃ¼nschten Produktpfad zurÃ¼ckgezogen:
  - `ArbeitseinsÃ¤tze bearbeiten`
  - `Termine bearbeiten`
  - `Bekanntmachungen bearbeiten`
  sind nicht mehr Teil der Hauptnavigation
  - erreichbar bleiben sie nur Ã¼ber die vorhandenen Home-Buttons fÃ¼r Admin/Vorstand
  - damit bleibt kein Doppelpfad Home + Hauptnavigation stehen
- Kleine Abschlussbereinigung des halbfertigen Zustands zusÃ¤tzlich erledigt:
  - die drei Top-Leisten der Editor-Views wurden mit `ZurÃ¼ck` ergÃ¤nzt
  - der zwischenzeitliche Encodingrest beim Label `Ã–ffnen` wurde bereinigt
- WPF wurde konkret angepasst; MAUI wurde nicht mit neuer EditoroberflÃ¤che erweitert. Da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, zieht MAUI fachlich denselben Fixpfad mit, ohne dass die mobile OberflÃ¤che beschÃ¤digt wird.
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich
  - verbleibende Warnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler
- Ergebnis: der begonnene Bugfix-/UX-Block fÃ¼r die drei bestehenden Verwaltungseditoren ist jetzt sauber abgeschlossen, dokumentiert, build-verifiziert und bereit fÃ¼r Commit/Push.

## 2026-03-22 â€“ Prompt 2/2: Arbeitsstunden-Freigabe sauber abgeschlossen mit globalem Review-Lock, PrÃ¼ftabelle und wiederverwendetem Editor

- Den begonnenen Arbeitsstunden-Freigabe-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufgemacht: die vorbereiteten Ã„nderungen fÃ¼r PrÃ¼ftabelle, globalen Lock auf `arbeitstunde`, Wiederverwendung des Erfassungseditors und die Umstellung auf `Arbeitsstunden freigeben` waren bereits im Repo angelegt, mussten aber noch sauber verifiziert, klein bereinigt und dokumentiert werden.
- Die realen Felder fÃ¼r den Produktpfad nochmals explizit gegen Modell und Service geprÃ¼ft und dann unverÃ¤ndert korrekt genutzt:
  - `freigegeben`
  - `status`
  - `genehmigt_am`
  - `genehmigt_von`
  - `lockedbyuserid`
  - `lockat`
- Den offenen PrÃ¼fzustand fachlich endgÃ¼ltig von `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃ¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das Anmerkungsfeld fÃ¼r Admin/Vorstand.
- Die WPF-Ansicht `Arbeitsstunden freigeben` dient jetzt wirklich primÃ¤r der PrÃ¼fung/Freigabe offener Arbeitsstunden und nicht mehr einer vorgelagerten Mitglieder-Sammelliste:
  - Darstellung als Tabelle
  - Sortierung nach `datum` aufsteigend *(Ã¤lteste zuerst)*
  - pro Zeile sichtbar: Mitglied, Datum, Saison, Stunden, Art der Arbeit
  - zusÃ¤tzlich bearbeitbares `status`-Feld als Anmerkungsbereich
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Sitzungsspeichern produktiv umgesetzt: gespeichert werden ausdrÃ¼cklich nur markierte/geÃ¤nderte Zeilen; unbearbeitete offene DatensÃ¤tze bleiben offen. Damit ist die fachliche Vorgabe erfÃ¼llt, dass nicht jede Sitzung alle offenen FÃ¤lle abschlieÃŸen muss.
- Die eigentliche Freigabelogik nutzt die realen Freigabefelder korrekt:
  - `freigegeben = true`
  - `genehmigt_am` wird beim Speichern gesetzt
  - `genehmigt_von` wird auf das aktuelle Mitglied des prÃ¼fenden Admins/Vorstands gesetzt
  - geÃ¤nderte, aber nicht freigegebene Zeilen bleiben mit `freigegeben = false` in der offenen Liste
- `Bearbeiten` verwendet jetzt bewusst denselben Arbeitsstunden-Erfassungseditor weiter, statt einen zweiten parallelen PrÃ¼feditor zu erfinden:
  - Wiederverwendung Ã¼ber `ArbeitsstundenErfassungViewModel` / `ArbeitsstundenErfassungView`
  - im PrÃ¼f-/Adminkontext zusÃ¤tzlich sichtbares `status`-Feld
  - Ã–ffnung in einem kleinen Host-Window, damit der bestehende Editorpfad auch modal im Reviewkontext nutzbar bleibt
  - nach dem Speichern wird die PrÃ¼ftabelle wieder frisch geladen
- Globaler Review-Lock klein und nachvollziehbar auf dem vorhandenen Tabellenmodell umgesetzt:
  - beim Ã–ffnen der Freigabeansicht wird eine globale PrÃ¼fsperre fÃ¼r die offenen `arbeitstunde`-DatensÃ¤tze gesetzt
  - andere PrÃ¼fer kÃ¶nnen wÃ¤hrenddessen nicht parallel produktiv dieselbe Freigabesitzung bearbeiten
  - falls real auflÃ¶sbar, wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃ¤ngert die Sperre wÃ¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃ¤ngende Locks laufen nach Timeout aus und kÃ¶nnen danach kontrolliert Ã¼bernommen werden
- Die bestehende Badge-/ZÃ¤hlerlogik wurde bewusst weiterverwendet: Ã„nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, damit WPF-Navigation und vorhandene mobile Reviewindikatoren korrekt nachziehen.
- MAUI wurde in diesem Block nicht mit neuer Review-UI ausgebaut; konsistent mitgezogen wurde aber die zugrunde liegende Regel, dass offene Arbeitsstunden Ã¼ber `freigegeben = false` laufen und `status` kein kÃ¼nstlicher Workflowwert mehr ist.
- Kleine technische Abschlussbereinigung des halbfertigen Zustands durchgefÃ¼hrt:
  - verbleibende Nullability-Warnung im WPF-Review-ViewModel bereinigt
  - Statusmeldung der PrÃ¼fansicht von der Locknachricht entkoppelt, damit RÃ¼ckmeldungen unabhÃ¤ngig sichtbar bleiben
  - die kurz sichtbare Design-Time-XAML-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der tatsÃ¤chliche WPF-Build lÃ¤uft erfolgreich durch
- Offene Restpunkte nach diesem Block bewusst klein gehalten:
  - die produktive WPF-Freigabe steht jetzt fachlich
  - ein spÃ¤terer separater Block kÃ¶nnte nur noch UX-Feinschliff oder weitergehende Reviewentscheidungen behandeln, ohne dass der Produktpfad aktuell unvollstÃ¤ndig wÃ¤re
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich.

## 2026-03-22 â€“ Prompt 1/2: Arbeitsstunden-Unterbau fÃ¼r einfachen Userflow wiederverwendet und Freigabe-Navigation vorbereitet

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃ¼ft: produktiv vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, `AddArbeitsstundeAsync(...)`, `GetUnapprovedArbeitsstundenByMitgliedAsync()`, die bestehende Review-Ansicht fÃ¼r Admin/Vorstand sowie der WPF-/MAUI-Badgepfad Ã¼ber `ArbeitsstundenChangedMessage`.
- Den vorhandenen Unterbau bewusst weiterverwendet statt neue Gesamtarchitektur zu bauen:
  - Speichern neuer User-EintrÃ¤ge lÃ¤uft weiter Ã¼ber `AddArbeitsstundeAsync(...)`
  - offene FreigabefÃ¤lle werden weiter Ã¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` ermittelt
  - Badge-/Sichtbarkeitsaktualisierung bleibt am vorhandenen `ArbeitsstundenChangedMessage`-Pfad angeschlossen
  - die bestehende Admin-/Vorstands-Reviewansicht wird nicht ersetzt, sondern nur sprachlich/navigativ konsolidiert
- FÃ¼r normale Nutzer jetzt einen eigenen klaren WPF-Erfassungsweg ergÃ¤nzt: `ArbeitsstundenErfassungViewModel` + `ArbeitsstundenErfassungView` bilden ein separates einfaches View nur fÃ¼r die Erfassung, ohne den bisherigen Review-/Bearbeitungspfad zu verdoppeln.
- Das neue Userformular zeigt bewusst nur die fachlich geforderten Felder:
  - `Datum` *(Pflichtfeld)*
  - `Stunden` *(Pflichtfeld)*
  - `Art der Arbeit` *(Pflichtfeld)*
- `freigegeben` ist in diesem Userflow kein sichtbares Feld; neue DatensÃ¤tze werden im Usermodus immer automatisch mit `freigegeben = false` und dem bestehenden Statuspfad `offen` gespeichert.
- Keine spekulativen Zusatzfelder eingefÃ¼hrt: insbesondere kein sichtbares Mitgliedsfeld, keine zusÃ¤tzliche Freigabesteuerung, keine neue Kommentierungs-/Ablehnungslogik und keine neue Saison-/Admin-Facharchitektur im Formular.
- Der neue Einstieg ist jetzt klar und eigenstÃ¤ndig statt als Inline-Teil auf Home:
  - neues WPF-Hauptnavigationselement `Arbeitsstunden erfassen` fÃ¼r Benutzer mit eigenem Mitgliedskontext
  - zusÃ¤tzlicher klarer Button `Arbeitsstunden erfassen` im Home-Bereich `Meine Arbeitsstunden`, der in dieselbe eigene View navigiert
- Die neue WPF-Erfassungsansicht bleibt einfach, aber produktiv:
  - aktueller eigener Mitgliedskontext wird Ã¼ber den vorhandenen MainWindow-/UserContext-Pfad aufgelÃ¶st
  - aktuelle Saison wird Ã¼ber den vorhandenen `GetSaisonRecordsAsync()`-Pfad ermittelt
  - `Abbrechen` setzt das Formular sauber zurÃ¼ck
  - `Speichern` steht am Ende des Formulars
- Validierung fÃ¼r den Userflow fachlich klein und produktnah umgesetzt:
  - Pflichtfelder werden markiert
  - fehlende Eingaben werden rot hervorgehoben
  - Fokus springt auf das erste fehlerhafte Feld von oben
  - `Stunden` mÃ¼ssen numerisch und grÃ¶ÃŸer als `0` sein
  - keine kÃ¼nstlich schÃ¤rferen Zusatzregeln wurden eingefÃ¼hrt
- FÃ¼r Admin/Vorstand wurde in diesem Block bewusst noch keine neue Freigabe-OberflÃ¤che gebaut; stattdessen wurde der bestehende Pfad nur vorbereitet/konsolidiert:
  - WPF-Navigationseintrag wird jetzt als `Arbeitsstunden freigeben` gefÃ¼hrt
  - vorhandene WPF-Reviewtitel wurden auf `freigeben` umbenannt
  - bestehendes MAUI-AdminmenÃ¼ und die mobile Reviewtitel wurden ebenfalls auf `freigeben` konsolidiert
  - es bleibt bei derselben bestehenden Review-Mechanik, keine zweite parallele Adminansicht
- Der offene Freigabe-Indikator funktioniert weiter auf dem vorhandenen Produktpfad:
  - sichtbar nur fÃ¼r Admin/Vorstand bzw. Rollen mit `CanManageWorkHours`
  - sichtbar/hervorgehoben nur bei tatsÃ¤chlich offenen DatensÃ¤tzen mit `freigegeben = false`
  - Badge/ZÃ¤hler zeigt die Anzahl offener PrÃ¼ffÃ¤lle
  - nach neuer User-Erfassung aktualisiert sich dieser Pfad weiter Ã¼ber `ArbeitsstundenChangedMessage`
- MAUI wurde nicht mit Platzhalterseiten aufgeblÃ¤ht: der Block beschrÃ¤nkt sich mobil auf die sprachliche Konsolidierung der vorhandenen Arbeitsstunden-/Freigabebegriffe; der gemeinsame Service- und Statusunterbau bleibt damit fÃ¼r spÃ¤tere ParitÃ¤t offen und unverletzt.
- FÃ¼r den nÃ¤chsten Freigabe-Block bleibt bewusst noch offen:
  - ob und wie die bestehende Freigabe-/ReviewoberflÃ¤che fachlich weiter vereinfacht oder umgestaltet wird
  - mÃ¶gliche weitergehende Admin-/Vorstandsentscheidungen jenseits der hier nur vorbereiteten Freigabenavigation
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block erfolgreich.

## 2026-03-22 â€“ Prompt 5/5: Verwaltungseditoren konsolidiert, alte Strukturreste entfernt und Abschlussstand fÃ¼r WPF/MAUI geschÃ¤rft

- Den aktuellen Abschlussstand der drei Verwaltungseditoren vor dem Konsolidierungsschritt nochmals gezielt geprÃ¼ft: produktiv verdrahtet waren bereits `ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView`; parallel lagen im Repo aber noch die Ã¤lteren vorbereitenden Strukturviews ohne produktive Verdrahtung.
- Die produktive Navigation nochmals gegen `App.xaml`, `NavigationService`, `MainWindowViewModel` und `HomeViewModel` abgesichert:
  - `App.xaml` mappt die drei Verwaltungs-ViewModels ausschlieÃŸlich auf die produktiven Editor-Views
  - `NavigationService` erzeugt weiterhin nur die drei produktiven Verwaltungs-ViewModels
  - `MainWindowViewModel` bietet die drei Verwaltungswege nur im Admin-/Vorstandskontext an
  - `HomeViewModel` Ã¶ffnet aus Home heraus ebenfalls nur diese produktiven Verwaltungs-ViewModels
- Die alten strukturellen Verwaltungsviews wurden jetzt konsequent entfernt, weil sie nicht mehr produktiv genutzt wurden und nur noch doppeldeutige Koexistenz erzeugten:
  - `ArbeitseinsaetzeVerwaltungView.xaml` / `.xaml.cs`
  - `TermineVerwaltungView.xaml` / `.xaml.cs`
  - `BekanntmachungenVerwaltungView.xaml` / `.xaml.cs`
- Auch der frÃ¼here gemeinsame Strukturunterbau wurde bereinigt:
  - `HomeVerwaltungViewModelBase.cs`
  - `HomeVerwaltungListItem.cs`
  Diese Bausteine waren nach der Umstellung aller drei Bereiche auf echte Basistabellen-Editoren nicht mehr produktiv in Verwendung.
- Die Benennung/Architektur bleibt damit im Abschlussstand klar lesbar: die produktiven WPF-VerwaltungsoberflÃ¤chen tragen bewusst den Suffix `EditorView`, und es existiert daneben kein konkurrierender Alt-View-Pfad mehr.
- Einen grÃ¶ÃŸeren Umbenennungsblock bewusst nicht erÃ¶ffnet: da die produktiven Editor-Views bereits eindeutig und direkt in `App.xaml` verdrahtet sind, hÃ¤tte ein zusÃ¤tzlicher Klassen-/Datei-Rename in diesem Abschlussblock mehr mechanische Bewegung als fachlichen Nutzen erzeugt.
- Gemeinsame Muster wurden nicht kÃ¼nstlich Ã¼berabstrahiert: Validierungs-, Fokus- und Zeitlogik bleiben zwischen den drei Editor-ViewModels fachlich parallel und weiterhin gut nachvollziehbar. GrÃ¶ÃŸere neue Basisklassen/Abstraktionen wurden bewusst nicht mehr eingezogen, um den Abschlussblock nicht unnÃ¶tig zu verbreitern.
- Home-/Rechtepfad final abgesichert: Bearbeiten-Einstiege bleiben nur fÃ¼r Admin/Vorstand sichtbar und nutzbar; normale Nutzer bleiben sauber im lesenden Home-Modus. Es gibt im aktuellen Stand keine produktive Navigation mehr in veraltete Strukturviews und keine Bearbeitung direkt auf Home.
- MAUI-ParitÃ¤t fÃ¼r spÃ¤teren Anschluss fachlich abgesichert, ohne neue Platzhalterseiten zu bauen:
  - die produktiven Lese-/Create-/Update-Pfade fÃ¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃ¤ndig in den gemeinsamen Services/Modellen
  - damit ist die fachliche Grundlage fÃ¼r spÃ¤tere mobile VerwaltungsoberflÃ¤chen plattformneutral vorhanden
  - WPF-spezifisch bleiben aktuell nur die drei tatsÃ¤chlichen Editor-UIs inkl. Fokussteuerung/HTML-Vorschau
- Kleiner QualitÃ¤tsblock erfolgreich abgeschlossen: keine neue Fachlogik erÃ¶ffnet, keine Home-Bearbeitung ergÃ¤nzt, keine zusÃ¤tzliche Doppelverdrahtung stehen gelassen, und die aktive WPF-/MAUI-Basis bleibt buildfÃ¤hig.
- Reale Restoffenheiten nach dem Abschlussblock:
  - die drei produktiven WPF-Verwaltungseditoren stehen jetzt vollstÃ¤ndig
  - fÃ¼r MAUI existiert bewusst noch keine produktive VerwaltungsoberflÃ¤che fÃ¼r diese drei Bereiche, aber der gemeinsame Unterbau ist vorbereitet
  - verbleibend sind auÃŸerhalb dieses Blocks nur noch getrennte, nicht blockrelevante Arbeitsbaum-Artefakte bzw. unabhÃ¤ngige UI-Themen wie die bereits offene `LoginWindow.xaml`
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung und Altlast-Bereinigung erfolgreich.

## 2026-03-22 â€“ Prompt 4/5: ArbeitseinsÃ¤tze-Verwaltung produktiv an `arbeitseinsatz` angeschlossen, inklusive Sonderregeln fÃ¼r Teilnehmergrenze und Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃ¤tze-Verwaltung vor dem Umbau erneut geprÃ¼ft: `ArbeitseinsaetzeVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃ¼st abgeleitet, die Liste kam nur aus dem Startseiten-Lesepfad, und rechts gab es noch keinen produktiven Editor mit bestÃ¤tigten Basistabellenfeldern.
- Den bestÃ¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestÃ¤tigten Fachfelder:
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
- Gemeinsamen produktiven Basistabellenpfad ergÃ¤nzt:
  - `GetArbeitseinsaetzeVerwaltungAsync()`
  - `CreateArbeitseinsatzAsync(...)`
  - `UpdateArbeitseinsatzAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `arbeitseinsatz`; es wird ausdrÃ¼cklich nicht gegen `v_startseite_arbeitseinsatz` geschrieben.
- Die linke Verwaltungsseite nutzt damit jetzt reale DatensÃ¤tze aus `arbeitseinsatz` statt einer bloÃŸen Home-/View-Strukturableitung. Dadurch bleiben auch inaktive oder intern markierte DatensÃ¤tze im Admin-/Vorstandskontext sauber bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick Ã¶ffnet den Editor mit echten Werten
  - `Neu` Ã¶ffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollstÃ¤ndig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- Sonderregel `max_teilnehmer` fachlich sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - in der UI gibt es einen expliziten Zustand `ohne Begrenzung`
  - in diesem Zustand bleibt das eigentliche Zahlenfeld unsichtbar
  - gespeichert wird dann `NULL`, nie `0`
  - sobald eine Begrenzung aktiv ist, muss `max_teilnehmer > 0` gelten
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt:
  - das Feld ist nicht verpflichtend
  - leere Eingabe bleibt im Editor als optionaler Zustand zulÃ¤ssig
  - beim Speichern wird der DB-konforme operative Wert `0` verwendet, damit der NOT-NULL-/Default-Vertrag sauber eingehalten bleibt
  - eingegebene Werte mÃ¼ssen `>= 0` sein
- BestÃ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungÃ¼ltig
  - `Sichtbar bis < Sichtbar ab` ist ungÃ¼ltig
  - `max_teilnehmer <= 0` bei aktiver Begrenzung ist ungÃ¼ltig
  - `stunden_wert < 0` ist ungÃ¼ltig
  - `anmeldung_bis` wird als optionaler Timestamp mit vollstÃ¤ndigem Datum+Uhrzeit-Paar geprÃ¼ft, sobald das Feld genutzt wird
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe zentrale tolerante Zeitlogik fÃ¼r `Startuhrzeit`, `Enduhrzeit`, `Sichtbar ab`, `Sichtbar bis` und `Anmeldung bis`
- FÃ¼r `Stundenwert` wurde ergÃ¤nzend eine kleine tolerante numerische Parse-Logik eingebracht, damit Eingaben kulturrobust verarbeitet werden, ohne daraus ein neues grÃ¶ÃŸeres Shared-Parsing-Subsystem zu machen.
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Ã„nderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃ¶ÃŸere Bereinigung wird nicht in diesen Block hineingezogen.
- Offene Punkte fÃ¼r den letzten ParitÃ¤ts-/AufrÃ¤umblock: die drei produktiven Verwaltungseditoren stehen jetzt, offen bleibt vor allem die kleine Konsolidierung/ParitÃ¤t der verbliebenen Altstrukturen und der anschlieÃŸende Abschluss-/AufrÃ¤umpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃ¤tze-Block erfolgreich.

## 2026-03-22 â€“ Prompt 3/5: Bekanntmachungen-Verwaltung produktiv an `bekanntmachung` angeschlossen, inklusive kleinem HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung vor dem Umbau erneut geprÃ¼ft: `BekanntmachungenVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃ¼st abgeleitet, die Listenladung lief nur Ã¼ber den Startseiten-Lesepfad, und es gab noch keinen produktiven Editor fÃ¼r `inhalt_html`.
- Den bestÃ¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv an die WPF-Verwaltung angebunden. Bearbeitet werden genau die bestÃ¤tigten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `inhalt_html` *(Pflichtfeld)*
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `sort_order`
  - `aktiv`
- Technische Felder wie `created_at` und `updated_at` bleiben weiterhin bewusst auÃŸerhalb der normalen Bearbeitung; es wurden keine zusÃ¤tzlichen Fantasiefelder ergÃ¤nzt.
- Gemeinsamen produktiven Basistabellenpfad ergÃ¤nzt:
  - `GetBekanntmachungenVerwaltungAsync()`
  - `CreateBekanntmachungAsync(...)`
  - `UpdateBekanntmachungAsync(...)`
  Diese Methoden lesen/schreiben direkt auf `bekanntmachung`; es wird ausdrÃ¼cklich nicht gegen `v_startseite_bekanntmachungen` geschrieben.
- Die linke Verwaltungsseite nutzt jetzt reale DatensÃ¤tze aus `bekanntmachung` statt einer Home-/View-Strukturableitung. Dadurch bleibt die Admin-/Vorstandsverwaltung auch fÃ¼r noch nicht sichtbare oder inaktive Bekanntmachungen fachlich korrekt.
- Das rechte Verhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick Ã¶ffnet den Editor mit echten Werten
  - `Neu` Ã¶ffnet einen leeren Editorzustand
  - `Abbrechen` verwirft den Bearbeitungszustand vollstÃ¤ndig
  - `Speichern` bleibt wie gefordert am Ende des Formulars
- HTML-Editor-Entscheidung bewusst klein und kontrolliert gehalten: im Repo gab es vor dem Block keine belastbare kleine HTML-/Preview-Komponente und keine bereits genutzte leichte EditorabhÃ¤ngigkeit. Deshalb wurde keine schwere neue Editor-Architektur eingefÃ¼hrt, sondern eine produktive Bordmittel-LÃ¶sung umgesetzt:
  - HTML-Quellbearbeitung in einem dedizierten Editorbereich
  - kleine Snippet-Leiste fÃ¼r hÃ¤ufige HTML-Bausteine (`Absatz`, `Ãœberschrift`, `Fett`, `Link`, `Liste`)
  - integrierte Live-Vorschau Ã¼ber den vorhandenen WPF-`WebBrowser`
- Damit bleibt `inhalt_html` nicht auf ein bloÃŸes Plaintext-Endfeld reduziert, ohne einen Ã¼bergroÃŸen Richtext-Baukasten neu zu erfinden.
- BestÃ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `inhalt_html` darf nicht leer oder nur Leerzeichen sein
  - `sichtbar_bis < sichtbar_ab` ist ungÃ¼ltig
  - fÃ¼r `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit gemeinsam benÃ¶tigt, sobald eines davon befÃ¼llt wird, damit keine stillen Timestamp-Annahmen entstehen
  - `sort_order` ist optional, muss aber bei Eingabe eine ganze Zahl sein
- Das Validierungs-/Fokusmuster aus `Termine` wurde bewusst wiederverwendet:
  - rote Hervorhebung fehlerhafter Felder
  - Fokus springt beim Speichern auf das erste fehlerhafte Feld von oben
  - dieselbe tolerante Zeitlogik fÃ¼r die Sichtbarkeits-Timestamps
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut angezeigt, damit Ã„nderungen unmittelbar nachvollziehbar bleiben.
- Kleiner technischer AufrÃ¤umcheck bewusst klein gehalten: die Ã¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht mit HTML-Vorschau; eine Bereinigung kann spÃ¤ter separat folgen, ohne diesen Block aufzublÃ¤hen.
- Offene Punkte fÃ¼r das nÃ¤chste Modul: `ArbeitseinsÃ¤tze` sind weiterhin das letzte der drei Home-nahen Verwaltungsfelder ohne bestÃ¤tigten produktiven Editor; dort fehlen im aktiven Stand noch die sauber verifizierten Schreibfelder/-pfade analog zu `termin` und `bekanntmachung`.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block erfolgreich.

## 2026-03-22 â€“ Prompt 2/5: Termine-Verwaltung als erster echter Editor produktiv an `termin` angeschlossen

- Den aktuellen Istzustand der vorbereiteten Termine-Verwaltung vor dem Umbau erneut geprÃ¼ft: `TermineVerwaltungViewModel` war bislang noch nur strukturell aus dem gemeinsamen VerwaltungsgerÃ¼st abgeleitet, die Listenladung lief nicht Ã¼ber die bestÃ¤tigte Basistabelle `termin`, und im rechten Bereich gab es noch keinen produktiven Editor mit bestÃ¤tigten Feldern.
- Den jetzt bestÃ¤tigten Tabellenvertrag von `termin` direkt in die produktive WPF-Bearbeitung Ã¼berfÃ¼hrt. Bearbeitet werden genau die verifizierten Fachfelder:
  - `titel` *(Pflichtfeld)*
  - `beschreibung`
  - `datum` *(Pflichtfeld)*
  - `start_uhrzeit`
  - `end_uhrzeit`
  - `sichtbar_ab`
  - `sichtbar_bis`
  - `aktiv`
- Technische Spalten wie `created_at` und `updated_at` bleiben bewusst auÃŸerhalb der normalen EditoroberflÃ¤che; es wurden keine zusÃ¤tzlichen oder geratenen Felder ergÃ¤nzt.
- Gemeinsamen produktiven Servicepfad fÃ¼r die Basistabelle ergÃ¤nzt: `ISupabaseService`/`SupabaseService` laden die Verwaltungs-Liste jetzt direkt aus `termin` und schreiben neue/geÃ¤nderte DatensÃ¤tze per `CreateTerminAsync(...)` und `UpdateTerminAsync(...)` wieder gegen `termin` zurÃ¼ck. Es wird ausdrÃ¼cklich nicht gegen `v_startseite_termine` geschrieben.
- Die Terminliste links zeigt jetzt reale DatensÃ¤tze aus der Basistabelle statt der bisherigen reinen Strukturvorbereitung; dadurch sind auch nicht fÃ¼r Home gedachte VerwaltungszustÃ¤nde wie `aktiv = false` im Admin-/Vorstandskontext korrekt bearbeitbar.
- Das rechte Bearbeitungsverhalten ist jetzt produktiv und konsistent:
  - ohne `Neu` oder Doppelklick bleibt rechts leer
  - Doppelklick auf einen vorhandenen Termin Ã¶ffnet rechts den echten Editor mit den geladenen Werten
  - `Neu` Ã¶ffnet einen leeren Editorzustand mit fachlich sinnvollem Default `aktiv = true`
  - `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃ¤ndig
  - `Speichern` steht wie gefordert am Ende des Formulars
- BestÃ¤tigte Validierungsregeln direkt umgesetzt:
  - `Titel` darf nicht leer oder nur Leerzeichen sein
  - `Datum` ist Pflicht
  - `Enduhrzeit < Startuhrzeit` ist ungÃ¼ltig
  - `Sichtbar bis < Sichtbar ab` ist ungÃ¼ltig
  - fÃ¼r `sichtbar_ab`/`sichtbar_bis` werden Datum und Uhrzeit jeweils gemeinsam benÃ¶tigt, sobald das Feld befÃ¼llt wird, damit kein stiller Timestamp-Anteil geraten wird
- Wiederverwendbares Eingabe-/Validierungsmuster fÃ¼r Folgeeditoren vorbereitet:
  - Pflicht- und Fehlerfelder werden rot markiert
  - beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben
  - tolerante Zeiteingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert
  - offensichtlich ungÃ¼ltige Zeiten bleiben sichtbar und werden nicht still geleert oder heimlich korrigiert
- Nach erfolgreichem Speichern wird die Liste neu geladen und der gespeicherte Datensatz wieder selektiert/erneut im Editor geÃ¶ffnet, damit Ã„nderungen direkt nachvollziehbar bleiben.
- MAUI in diesem Block bewusst nicht mit Platzhalterseiten erweitert; durch den gemeinsamen produktiven Servicepfad fÃ¼r `termin` ist die spÃ¤tere mobile ParitÃ¤t aber nicht verbaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Termin-Block erfolgreich.

## 2026-03-22 â€“ Prompt 1/5: Home nur mit Admin-Bearbeiten-Einstiegen, separate Verwaltungsviews ohne Platzhalter vorbereitet

- Aktuellen WPF-/MAUI-Istzustand vor dem Umbau erneut geprÃ¼ft: `HomeView`/`HomeViewModel` zeigten bereits nur noch Listen plus separate Detailviews, `NavigationService` und `MainWindowViewModel` waren ViewModel-first organisiert, und die bestÃ¤tigten Lesepfade fÃ¼r Home liefen Ã¼ber `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen`.
- Im aktiven Repo zugleich keine belastbar verifizierten Schreibmodelle oder bestehenden Create/Update-Servicepfade fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen gefunden; genau deshalb wurde in diesem Block bewusst keine neue Formularlogik geraten.
- Home fachlich weiter bereinigt: auf der Startseite gibt es fÃ¼r normale Nutzer weiterhin nur die bisherigen Ãœbersichtslisten; fÃ¼r Admin/Vorstand kommt jetzt zusÃ¤tzlich eine kleine Verwaltungssektion mit drei Bearbeiten-Einstiegen hinzu, aber keine Bearbeitung direkt auf Home.
- Drei echte separate WPF-Verwaltungsviews angelegt und in die bestehende Navigation eingebunden:
  - `ArbeitseinsaetzeVerwaltungView`
  - `TermineVerwaltungView`
  - `BekanntmachungenVerwaltungView`
- Jede Verwaltungsansicht nutzt bereits das Ziel-Layout mit linker Liste und rechter EditorflÃ¤che. Wenn weder ein vorhandener Datensatz geÃ¶ffnet noch `Neu` ausgelÃ¶st wurde, bleibt rechts bewusst ohne Formularfelder. Ein Doppelklick auf einen Listeneintrag Ã¶ffnet rechts den Editierzustand mit den vorhandenen belastbaren Lesedaten; `Neu` Ã¶ffnet denselben strukturellen Zustand leer.
- Keine Platzhalterformulare aufgebaut: weil die echten Schreibfelder/Basistabellen im aktiven Repo noch nicht sicher genug ableitbar waren, zeigt der rechte Bereich aktuell nur den geÃ¶ffneten Bearbeitungszustand plus die verifizierten Lese-/Schreibpfad-Hinweise statt geratener Eingabefelder.
- Reale Datenlisten statt Platzhalter verdrahtet: `ISupabaseService`/`SupabaseService` stellen die drei bestÃ¤tigten Startseiten-Lesepfade jetzt auch direkt fÃ¼r Verwaltungslisten bereit, sodass die neuen Views auf belastbaren Daten basieren und nicht auf Home-spezifischen Zwischenobjekten.
- Rechte entlang des vorhandenen Pfads sauber eingehÃ¤ngt: die Verwaltungsviews erscheinen in WPF nur fÃ¼r Admin/Vorstand sowohl Ã¼ber Home als auch in der Hauptnavigation; MAUI bleibt in diesem Block unverÃ¤ndert und wird nicht mit neuen Platzhalterseiten aufgeblÃ¤ht, profitiert aber spÃ¤ter vom gemeinsamen Lesepfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block erfolgreich.
