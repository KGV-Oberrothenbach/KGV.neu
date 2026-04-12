# KGV_Fortschrittslog_kompakt

Diese Fassung verdichtet das ausführliche Fortschrittslog auf die fachlich tragenden Informationen:
- **was umgesetzt wurde**
- **welche Wirkung das hatte**
- **welcher Rest offen blieb**
- **welche Validierung wiederholt genutzt wurde**

Nicht mehr einzeln aufgeführt sind die vielen wiederholten Zwischenstände wie:
- wiederholte Git-/Repo-Prüfungen
- identische Dateilisten pro Mini-Block
- mehrfach gleiche Buildaufrufe
- schrittweise Diagnose-Iterationen, wenn das Ergebnis später sauber zusammengeführt werden konnte

## Validierungsstandard

Sofern nicht anders erwähnt, wurden die betroffenen Blöcke mit den jeweils relevanten Builds abgeschlossen, typischerweise über:
- `KGV.Core`
- `KGV.Wpf`
- `KGV.Maui`

## Aktueller Gesamtstand (Stand 2026-04-08)

- **WPF und MAUI** sind in den großen Fachbereichen deutlich näher zusammengezogen.
- **Arbeitsstunden** inkl. Prüf-/Freigabeprozess, Review-Lock und mobilem Nachzug sind produktiv ausgebaut.
- **Termine, Bekanntmachungen und Arbeitseinsätze** laufen über echte Verwaltungs-/Editorpfade statt Platzhalter.
- **Dokumente** sind produktiv an den Google-Drive-Unterbau angebunden; normale Nutzer bleiben view-only.
- **Demo-/Play-Store-Konten** können im Adminpfad jetzt auf echte Demo-Daten begrenzt werden, ohne produktive Vereinsdaten freizugeben.
- **Mitglieder-/Nebenmitglied-Flows** wurden stark ausgebaut: Neuanlage, Nebenmitglied anlegen/bearbeiten, Mitgliedschaft beenden mit Folgeentscheid.
- **Wartungsverträge** sind auch für Nebenmitglieder geöffnet.
- **Rechte-/Rollenmodell** wurde auf `app_user.role` als führende Quelle gezogen; benutzerspezifische Fachrechte inkl. Ablese-Freigaben sind aufgebaut.
- **Android/MAUI** wurde in mehreren Blöcken für Release, Logging, Timeout/Resume, Foto-Uploads und Startstabilität gehärtet.
- **ReleaseManager / AWR** wurden für reale WPF-/Android-Releases deutlich ausgebaut.

## Chronologischer Kurzverlauf

## 2026-04-11
- Den bestehenden MAUI-Pachtvertrags-Flow auf dem echten Stand von `main` fachlich korrekt auf `Preview -> Unterschrift -> finales Speichern` umgestellt, ohne neuen Fachblock zu starten.
- Die Ist-Analyse zeigte, dass der mobile Pachtvertrag in `MemberParzellenDetailPage` und im Folgepfad nach Parzellenzuweisung bisher direkt über `CreatePachtvertragDokumentAsync(...)` im offiziellen Dokumentpfad gespeichert wurde; damit erfolgte die Persistierung zu früh, noch vor Dokumentprüfung und vor digitaler Unterschrift.
- Der mobile Flow erzeugt den Pachtvertrag jetzt zunächst nur temporär als vollständige PDF-Vorschau. Dafür wurde eine kleine `PachtvertragPreviewPage` ergänzt, die das komplette generierte Dokument über einen lokalen Temp-/Cache-Pfad zur Prüfung öffnet und zugleich `Zurück`, `Abbrechen` und `Weiter zur Unterschrift` anbietet.
- Der technische mobile Ablauf ist dabei in einem kleinen gemeinsamen `PachtvertragFlowHelper` gekapselt, damit sowohl der bestehende Einstieg im mitgliedsbezogenen Parzellen-Detail als auch der Folgepfad nach erfolgreicher Parzellenzuweisung denselben sauberen Preview-/Signatur-/Save-Pfad nutzen.
- Erst nach erfolgreicher digitaler Unterschrift wird der finale signierte Pachtvertrag im offiziellen Dokumentpfad gespeichert; bei Abbruch vor oder während der Unterschrift erfolgt keine endgültige Ablage.
- Der bestehende gemeinsame Pachtvertrags-Builder bleibt führend: die Vorschau wird aus demselben Produktivpfad erzeugt, und der finale Save nutzt weiter den vorhandenen gemeinsamen Dokumentpfad; temporäre Vorschau und finale Ablage bleiben sauber getrennt.
- Die bestehende MAUI-Signaturseite mit Querformat auf mobilen Geräten blieb unverändert erhalten.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bestehenden MAUI-Mitgliedsantrag-Flow auf dem echten Stand von `main` fachlich korrekt auf `Preview -> Unterschrift -> finales Speichern` umgestellt, ohne neuen Fachblock zu starten.
- Die Ist-Analyse zeigte, dass `MemberDetailPage` den Antrag bisher direkt nach Beitragsbestätigung über den offiziellen Dokumentpfad gespeichert hat; damit erfolgte die Persistierung zu früh, noch vor Dokumentprüfung und vor der digitalen Unterschrift.
- Der mobile Flow erzeugt den Mitgliedsantrag jetzt zunächst nur temporär als vollständige PDF-Vorschau. Dafür wurde eine kleine `MitgliedsantragPreviewPage` ergänzt, die das komplette generierte Dokument über einen lokalen Temp-/Cache-Pfad zur Prüfung öffnet und zugleich `Zurück`, `Abbrechen` und `Weiter zur Unterschrift` anbietet.
- `Zurück` führt wieder in den Bearbeitungsdialog, `Abbrechen` beendet den Flow ohne Persistierung, und erst nach erfolgreicher digitaler Unterschrift wird der finale Mitgliedsantrag im offiziellen Mitglieds-Dokumentpfad gespeichert.
- Der bestehende gemeinsame Dokumentpfad bleibt führend: die Vorschau kommt aus demselben gemeinsamen Mitgliedsantrag-Builder, und der finale Save nutzt weiterhin den gemeinsamen Uploadpfad; temporäre Vorschau und finale Ablage bleiben sauber getrennt.
- Die bestehende MAUI-Signaturseite mit Querformat auf mobilen Geräten blieb erhalten; der gemeinsame PDF-Signaturbuilder wurde nur textlich so geöffnet, dass er neben Vertragsdokumenten auch den signierten Mitgliedsantrag fachlich sauber ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Saisonverwaltungs-Block auf dem echten Stand von `main` produktiv in WPF und MAUI vervollständigt, ohne neuen Fachblock zu starten.
- Der gemeinsame Servicepfad `saison` wurde dabei sauber geschlossen: `GetSaisonRecordsAsync()` lädt die Saisondaten produktiv, `SaveSaisonAsync(...)` normalisiert `id` und `jahr` auf das Kalenderjahr und speichert die Felder der bestehenden Saisonstruktur produktiv zurück.
- In WPF wurden die bereits vorhandenen Bausteine `SaisonverwaltungViewModel` und `SaisonverwaltungView` jetzt wirklich an die Navigation angeschlossen; der Einstieg ist für Admin sichtbar als `Verwaltung` mit Unterpunkt `Saisonverwaltung`.
- In MAUI wurde die fehlende `SaisonverwaltungPage` ergänzt und in `MauiProgram`, `ShellRouteRegistrar` sowie `AdminShell` produktiv verdrahtet; auch dort erscheint der Einstieg nur für Admin unter `Verwaltung` / `Saisonverwaltung`.
- Fachlich bleibt der Block klein und konsistent: vorhandene Saisons werden angezeigt, neue Saisonvorschläge übernehmen die Vorjahreswerte, `id` und `jahr` entsprechen dem Kalenderjahr, vergangene Jahre bleiben schreibgeschützt und der Speichern-Button steht am Ende des Formulars.
- Der temporäre harte rote Sichtbarkeitstest aus `MemberDetailPage` wurde im selben Abschlusslauf wieder sauber zurückgebaut, ohne dort die Fachlogik zu ändern.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen Analyse-/Routingblock `memberdetails` auf dem echten Stand von `main` sauber aufgelöst und minimal korrigiert, ohne neuen Fachblock zu starten.
- Der Pfad aus der Mitgliedersuche schreibt das ausgewählte Mitglied korrekt in den `MemberContextState` und aktiviert danach bevorzugt die sichtbare Shell-Route `memberdetails`.
- Die eigentliche Abweichung lag im `AdminShell`: Dort war `memberdetails` im echten Stand nicht auf `MemberDetailPage`, sondern auf `MeineDatenPage` verdrahtet.
- Dadurch öffnete die sichtbare Stammdatenseite zwar fachlich korrekt den Stammdatenpfad, technisch aber nicht die erwartete `MemberDetailPage`; deshalb erschienen dort weder der harte rote Marker noch der Mitgliedsantragspfad dieser Seite.
- Minimaler Fix: `AdminShell` zeigt `memberdetails` jetzt auf `MemberDetailPage`; zusätzlich wurde der direkte Fallback aus `MemberSearchPage` ebenfalls auf `MemberDetailPage` gezogen, damit bevorzugter Routepfad und Ausweichpfad denselben Zieltyp verwenden.
- `UserShell` blieb bewusst unverändert, weil dort für den Eigenkontext weiterhin der getrennte Pfad `mydetails` gilt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Härte-/Sichtbarkeitstest auf dem echten Stand von `main` umgesetzt, ohne neuen Fachblock zu starten.
- In `MemberDetailPage` wurde der vorhandene Laufzeit-Hinweis ganz oben zu einem bewusst auffälligen roten Testblock ausgebaut, damit auf einem Screenshot sofort erkennbar ist, ob wirklich dieser Seiten-Code läuft.
- Der Block zeigt jetzt explizit `TEST MemberDetailPage aktiv`, `Build main`, einen klaren Marker sowie die wichtigsten Laufzeitwerte `PageType`, `member.Id`, `_isCreateMode` und `CanCreateMitglied` direkt auf der Seite an.
- Die eigentliche Fachlogik für Rechte, Dokumente und die Sichtbarkeit des Mitgliedsantrag-Buttons blieb unverändert; der Block dient nur dem eindeutigen harten Laufzeittest.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Diagnoseblock zur Laufzeitidentität auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im betroffenen Stammdatenpfad `MemberDetailPage` wird jetzt direkt sichtbar angezeigt, dass technisch wirklich `MemberDetailPage` geöffnet ist.
- Zusätzlich zeigt der temporäre Hinweis App-Version und Buildnummer aus dem bestehenden MAUI-Versionspfad, einen Build-Marker mit Zeitstempel/Git-Kennung sowie den aktuellen Page-Typ und den aktuellen Shell-/Seitenpfad an.
- Ziel des Blocks ist nur die eindeutige Laufzeitidentifikation des tatsächlich installierten Builds und des tatsächlich geöffneten Seitenpfads; Rechte, Dokumentpfade und Fachlogik blieben unverändert.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Diagnoseblock für den im Lauf weiterhin fehlenden sichtbaren Button `Mitgliedsantrag als PDF` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- In `KGV.Maui/Pages/MemberDetailPage.cs` wurde direkt an der bestehenden Sichtbarkeitsberechnung ein leicht wieder entfernbarer Diagnosehinweis ergänzt.
- Der Hinweis zeigt zur Laufzeit genau die drei relevanten Bedingungen des Buttons an: Page-Modus (`_isCreateMode`), geladene `member.Id` und Ergebnis von `PermissionChecks.CanCreateMitglied(...)`; zusätzlich wird die aktuelle Rolle und ein kurzer Begründungstext für einen unsichtbaren Button ausgegeben.
- Die eigentliche Fachlogik wurde bewusst nicht geändert; der Block dient nur dazu, sichtbar zu machen, welche Bedingung im echten MAUI-Lauf greift.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Abschlussblock `CreateMitglied`/Mitgliedsantrag gegen den echten Branch-Stand `feature/formularverwaltung` geprüft, ohne neuen Fachblock zu starten.
- Die fachliche Gegenprüfung zeigte, dass der sichtbare mobile Einstieg `Mitgliedsantrag als PDF` im bestehenden `MemberDetailPage`-Pfad bereits korrekt an `PermissionChecks.CanCreateMitglied(...)` hängt; der frühere Dokumentrechtepfad `CanManageDocuments(...)` ist dort nicht mehr aktiv.
- Auch die direkt angrenzenden mobilen Onboarding-/Verpachtungspfade sind bereits konsistent: Parzellenzuweisung in `MemberGardensPage` und `Pachtvertrag als PDF` in `MemberParzellenDetailPage` nutzen ebenfalls `CanCreateMitglied(...)`.
- Ergebnis: Für diesen Abschlusslauf war kein weiterer Codeeingriff nötig; es wurden bewusst keine WPF-Änderungen, keine neue Dokumentlogik und keine neue Rechte-/Schattenlogik ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Datumsfix im realen Save-/Serializer-Pfad auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Der reale Restfehler lag nicht mehr in den Modell-Convertern, sondern im bisherigen `.Set(...)`-Updatepfad des Services: Dort wurden rohe `DateTime`-Werte geschrieben und die erweiterten Modell-Converter damit umgangen.
- Der zentrale Fix liegt jetzt ausschließlich in `KGV.Infrastructure/Services/SupabaseService.cs`: Arbeitseinsatz und Termin nutzen für `Insert` und `Update` denselben sicheren gemeinsamen Write-Pfad.
- Dabei werden reine Datumswerte explizit als `yyyy-MM-dd` und `timestamp without time zone` explizit ohne `Z`/Offset an PostgREST geschrieben, damit keine ungewollte UTC-/Offset-Verschiebung mehr in den DB-Speicherpfad hineinläuft.
- Es wurden bewusst keine UI-Workarounds, keine neue Fachlogik und keine weiteren Modell-Experimente ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-09
- Den bereits umgesetzten Block `Mitgliedsantrag mit Mitgliedsbeitrag` auf dem echten Branch-Stand `feature/formularverwaltung` im Abschlusslauf fachlich und technisch sauber geschlossen, ohne neuen Fachblock zu starten.
- Der Mitgliedsantrag nutzt dafür weiter den bestehenden Saisonpfad `SaisonRecord.Mitgliedsbeitrag`; die Vorschlagsregel bleibt wie begonnen konsistent in WPF und MAUI: Beginn vor `01.07.` voller Jahresbeitrag, Beginn ab `01.07.` halber Jahresbeitrag.
- Der vorgeschlagene Beitrag bleibt in beiden Erzeugungsdialogen vor dem finalen Erzeugen manuell editierbar; der final verwendete Betrag wird sichtbar im PDF ausgegeben.
- Der Mitgliedsantrag bleibt fachlich rein mitgliedsbezogen; parzellenbezogene Inhalte werden dort nicht ausgegeben und bleiben weiterhin dem Pachtvertrag vorbehalten.
- Im Abschlusslauf wurde nur noch der kleine technische MAUI-Buildrest des bereits umgesetzten Dialogpfads geschlossen, indem die numerische Tastatur explizit auf `Microsoft.Maui.Keyboard.Numeric` gezogen wurde.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Block `Mitgliedsdokumente im Mitgliedskontext` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- In der mobilen mitgliedsbezogenen Navigation gibt es jetzt den sichtbaren Menüpunkt `Dokumente` direkt zwischen `Stammdaten` und `Wartungsverträge`; er führt nicht auf eine globale Dokumente-Startseite, sondern direkt in die bestehende Dokumentansicht des aktuell ausgewählten Mitglieds.
- Dafür wurden keine neuen Dokumentservices und kein neues MAUI-ViewModel eingeführt: die vorhandene `DokumentePage` bleibt führend und nutzt weiter die bestehenden Produktivpfade `GetMitgliedDokumenteAsync(...)`, `GetParzelleDokumenteAsync(...)` und `ResolveDokumentOpenUrlAsync(...)`.
- Die bisher falsche Kopplung von reiner Lesbarkeit an Dokument-Verwaltungsrechte wurde im mobilen Mitgliedskontext gelöst: Dokumente sehen und öffnen folgt dort jetzt dem ausgewählten Mitgliedskontext, während Upload, Löschen, signierten Scan ablegen und digitale Signatur weiter nur mit dem bestehenden Fachrecht `CanManageDocuments` sichtbar bleiben.
- Ergebnis: Die mitgliedsbezogene MAUI-Dokumentansicht zeigt jetzt die echten Dokumente des aktuell ausgewählten Mitglieds an; dazu gehören auch `Mitgliedsantrag` und `Pachtvertrag`, wobei der Pachtvertrag fachlich im Mitglied sichtbar bleibt, auch wenn er aus einem Parzellenkontext stammt.
- Validierung: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen WPF-Updateprüfungsblock auf dem echten Branch-Stand `feature/formularverwaltung` fachlich sauber nachgeschärft, ohne neuen Fachblock zu starten.
- Die Updateprüfung startet jetzt nicht mehr nur allgemein nach erfolgreichem Login, sondern erst nachdem das `MainWindow` bzw. die Startseite erstmals sauber gerendert wurde.
- Dafür wurde der bestehende WPF-Startup-Pfad minimal von einem frühen Dispatcher-Start auf den vorhandenen Window-Lifecycle `ContentRendered` plus Dispatcher-Idle verschoben; die eigentliche Updateprüfungslogik bleibt unverändert.
- Ergebnis: Die WPF-Updateabfrage läuft jetzt erst nach erfolgreichem Login und nach sauber geladenem Startfenster an, statt den frühen Hauptfenster-/Startseitenaufbau zu unterbrechen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Rechteblock `CreateMitglied` auf dem echten Branch-Stand `feature/formularverwaltung` fachlich sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Die Vererbungslogik wurde korrigiert: `CreateMitglied` wird nicht mehr automatisch an `Vorstand` vererbt.
- `Admin` behält das Recht weiterhin automatisch über die Rollenbasis; `Vorstand` kann `CreateMitglied` jetzt nur noch per expliziter Zuweisung erhalten.
- Der bestehende gemeinsame Check `CanCreateMitglied(...)` bleibt unverändert auf dem effektiven Rechte-Set und spiegelt damit den Sollzustand korrekt wider.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen Rechteblock `CreateMitglied` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Dafür wurde im bestehenden Permission-Pfad ein eigenes Fachrecht `CreateMitglied` ergänzt; es ist kein neuer globaler Rollentyp, sondern ein gezielter Fachblock für Aufnahme-/Onboarding- und Verpachtungspflichten.
- Vorstand behält den bisherigen Zugriff bewusst weiter über die Rollenbasis; Admin bleibt ebenfalls voll funktionsfähig. Zusätzlich kann das neue Fachrecht jetzt gezielt vergeben werden, ohne fremde Adminbereiche wie Rechteverwaltung oder Saisonverwaltung mit freizuschalten.
- `PermissionChecks` kennt jetzt den gemeinsamen Check `CanCreateMitglied(...)`; daran hängen die direkt betroffenen sichtbaren Einstiege und Freigaben für `Mitglied anlegen`, `Mitgliedsantrag erstellen`, `Parzelle zuweisen` und `Pachtvertrag erstellen`.
- In MAUI wurden dafür der sichtbare Einstieg `Mitglied neu anlegen`, der Stammdaten-Folgepfad `Mitgliedsantrag als PDF`, die mobile Parzellenzuweisung und der Pachtvertragspfad auf das neue Recht umgestellt.
- In WPF wurden die entsprechenden Einstiege analog an denselben gemeinsamen Check gehängt; für die Parzellenzuweisung wurde der bestehende Mitglieds-Bearbeitungspfad minimal so geöffnet, dass der Onboarding-/Verpachtungspfad mit `CreateMitglied` nutzbar bleibt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-UX-Fix für den Mitgliedsantrag im Stammdatenkontext auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Nach erfolgreicher Mitglieds-Neuanlage springt der mobile Pfad nicht mehr blind auf `MeineDatenPage`, sondern übernimmt den neu angelegten Datensatz direkt in den normalen Stammdatenkontext derselben Seite.
- Dadurch bleibt der Create-Modus nicht mehr hängen: Die Seite läuft danach als normale Stammdatenansicht weiter, und der bestehende Button `Mitgliedsantrag als PDF` ist für das gerade angelegte Mitglied mit Dokumentrechten direkt sichtbar und nutzbar.
- Der vorhandene Dialog `Mitgliedsantrag erstellen?` im Neuanlage-Flow bleibt bestehen; zusätzlich kann derselbe Mitgliedsantrag danach weiterhin klar aus den Stammdaten des gerade angelegten Mitglieds erzeugt werden.
- Ergebnis: Der mobile Mitgliedsantrag ist jetzt sowohl direkt im Neuanlage-Flow als auch anschließend alltagstauglich im normalen Stammdatenkontext erreichbar, ohne verwirrenden Seitensprung.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Bereinigungsblock für alte `Mitgliedsvertrag`-Reste auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im mobilen Mitgliedsdetail wird kein eigenständiger sichtbarer Pfad `Mitgliedsvertrag als PDF` mehr angeboten; fachlich sichtbar bleibt dort nur noch `Mitgliedsantrag als PDF`.
- Der bereits angepasste mobile Neuanlage-Flow bleibt damit konsistent: Nach erfolgreicher Anlage wird weiter nur `Mitgliedsantrag erstellen?` angeboten; ein separater sichtbarer MAUI-Flow für `Mitgliedsvertrag` entfällt.
- Im mobilen Dokumentpfad wurden die sichtbaren Vertrags-Folgeaktionen zusätzlich auf fachlich gültige `Pachtvertrag`-Dokumente begrenzt, sodass alte `Mitgliedsvertrag`-Dokumente in MAUI keine gesonderten Vertrags-Folgeaktionen mehr anbieten.
- Bestehende gemeinsame Service- und Dokumentpfade bleiben unverändert nutzbar; es wurde keine neue Vertragslogik und kein neuer Signaturblock ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Formularblock für `Mitgliedsantrag` und `Pachtvertrag` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im mobilen Neuanlage-Flow des Mitglieds wird nach erfolgreicher Anlage jetzt `Mitgliedsantrag erstellen?` gefragt; bei Zustimmung nutzt der Pfad direkt den bestehenden Produktivpfad `CreateMitgliedsantragDokumentAsync(...)` und legt das Dokument im vorhandenen Mitglieds-Dokumentpfad ab.
- Wird der Antrag im Neuanlage-Moment noch nicht erzeugt, bleibt der bestehende mobile Folgepfad erhalten: Im Mitgliedskontext kann `Mitgliedsantrag als PDF` weiterhin nachträglich manuell erzeugt werden.
- Für den mitgliedsbezogenen Gartenpfad wurde eine kleine mobile Parzellenzuweisung ergänzt. Sie nutzt den bestehenden gemeinsamen Servicepfad `AssignParzelleToMitgliedAsync(...)`, erlaubt die Auswahl einer freien Parzelle sowie des Zuweisungsdatums und baut keine neue Parallelarchitektur.
- Nach erfolgreicher Parzellenzuweisung wird mobil jetzt direkt `Pachtvertrag erstellen?` angeboten; bei Zustimmung nutzt der bestehende gemeinsame Dokumentpfad `CreatePachtvertragDokumentAsync(...)` direkt die neue Zuordnung und speichert den Vertrag im vorhandenen Mitglieds-Dokumentpfad.
- Auch der nachträgliche Pachtvertragspfad bleibt erhalten: Im mitgliedsbezogenen Gärten-/Parzellen-Detail ist `Pachtvertrag als PDF` weiterhin verfügbar, sofern eine passende Parzellenzuweisung mit Startdatum vorhanden ist.
- Der bestehende MAUI-Dokumentpfad bleibt führend; Folgeaktionen wie Öffnen, signierten Scan ablegen oder digital signieren wurden in diesem Block nicht neu erfunden und nicht umgebaut.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den noch offenen Abschluss zum Datumsfix auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Nach dem bereits ergänzten gemeinsamen `date`-Pfad wurde jetzt auch der fehlende gemeinsame `Newtonsoft.Json`-Pfad für PostgreSQL-`timestamp without time zone` ergänzt, weil der reale Save-/PostgREST-/Supabase-Pfad hier über `Newtonsoft.Json` serialisiert.
- Damit sind jetzt nicht nur `datum`, sondern auch die Datums-/Zeitfelder `sichtbar_ab`, `sichtbar_bis` und `anmeldung_bis` im gemeinsamen Produktivpfad gegen ungewollte UTC-/Offset-Verschiebungen abgesichert.
- Die betroffenen Modellfelder in `Arbeitseinsatz` und `Termin` tragen dafür jetzt zusätzlich passende `Newtonsoft.Json.JsonConverter(...)`-Attribute; denselben gemeinsamen Timestamp-Pfad wurde minimal auch für `Bekanntmachung` mitgezogen.
- Ergebnis: Der reale Save-Pfad ist jetzt für beide relevanten PostgreSQL-Typen gemeinsam abgesichert: `date` und `timestamp without time zone`, jeweils für `System.Text.Json` und `Newtonsoft.Json`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen WPF-Startup-Block zur Updateprüfung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Die bestehende Updateprüfung lief bisher noch vor dem Login im frühen App-Startup; dieser Pfad wurde jetzt hinter den erfolgreichen Login und hinter das Anzeigen des `MainWindow` verschoben.
- Dadurch bleibt der eigentliche Login-/Frühstartpfad schlanker, und die Updateabfrage kann den Start vor der Anmeldung nicht mehr vorzeitig beeinflussen.
- Ergänzend wurde der statische App-Pfad minimal mit Frühstart-Logging versehen, damit ein Absturz vor `OnStartup(...)` im WPF-Dateilog transparenter sichtbar wird.
- Die bestehende Updateprüfungslogik selbst bleibt erhalten; es wurde keine neue Update-Architektur und keine neue Login-Logik eingeführt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen Datumsfix-Block für `Arbeitseinsätze` und `Termine` auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Fehlerursache lag im gemeinsamen DateOnly-Save-Pfad: der reale PostgREST-/Supabase-Pfad serialisiert die betroffenen `date`-Felder hier über `Newtonsoft.Json`, während die bisherigen Modelle nur `System.Text.Json`-Konverter trugen.
- Dadurch konnten reine Datumswerte im Produktivpfad zeitzonenbedingt um `-1 Tag` kippen; der Fehler lag also nicht nur im Rücklesen, sondern bereits im zentralen Speichern.
- Zentraler Fix: gemeinsame `Newtonsoft.Json`-Konverter für PostgreSQL-`date` ergänzt und an die betroffenen gemeinsamen `date`-Modelle angebunden; der vorhandene `System.Text.Json`-Pfad bleibt parallel erhalten.
- Der Fix gilt damit zentral für `Arbeitseinsätze` und `Termine`; Insert und Update nutzen denselben korrigierten DateOnly-Pfad ohne UI-Sonderlogik oder verstreute Workarounds.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Nachziehblock für Vertrags-Einstiege auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neue Formular- oder Signaturlogik zu starten.
- Auf der mobilen Stammdatenseite des Mitglieds gibt es jetzt zusätzlich den Einstieg `Mitgliedsvertrag als PDF`; der Button nutzt den bereits bestehenden Produktivpfad `CreateMitgliedsvertragDokumentAsync(...)`.
- Im mitgliedsbezogenen Pfad `Gärten` bzw. in der zugehörigen Parzellen-Detailansicht gibt es jetzt zusätzlich den Einstieg `Pachtvertrag als PDF`; auch dieser nutzt direkt den bestehenden Produktivpfad `CreatePachtvertragDokumentAsync(...)`.
- Der neue mobile Pachtvertragspfad baut keine Schattenlogik und keine Ersatzwerte auf: maßgeblich bleibt das im Parzellenkontext vorhandene Zuordnungs-Startdatum; fehlt es, wird mit klarer Meldung abgebrochen.
- Validierung: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen Block `Pachtvertrag nutzt Saisonwerte` auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Der bestehende Saison-/Pachtvertragspfad nutzt jetzt für den Zahlungskasten gemeinsam `pacht_pro_qm` und `mitgliedsbeitrag` der zum Vertragsjahr passenden Saison; Ersatzwerte oder Schattenlogik wurden nicht ergänzt.
- Die fachliche Berechnung lautet jetzt `Pachtzins = Parzellenfläche * Saison.Pacht_pro_qm` mit kaufmännischer Rundung auf zwei Nachkommastellen und `Gesamt = Pachtzins + Mitgliedsbeitrag`.
- Fehlende Pflichtwerte laufen nicht mehr still weiter: fehlende Saison, fehlendes `pacht_pro_qm` und fehlender `mitgliedsbeitrag` brechen den bestehenden Produktivpfad jetzt mit klaren Fehlermeldungen ab.
- Die bestehende PDF-Vorlage und der vorhandene Feldbefüllungspfad bleiben führend; im Betragskasten werden jetzt mindestens Mitgliedsbeitrag, Pacht und Gesamt korrekt befüllt.
- Der bereits begonnene Saisonpfad wurde nur minimal mitgezogen, damit der aktuelle Stand buildfähig bleibt; es wurde keine weitere Saison-UI-Erweiterung gestartet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Einen kleinen, buildfähigen UI-/UX-Feinschliff-Block für die Formularverwaltung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neue Vertrags- oder Verwaltungslogik zu starten.
- Die Dokumentlisten in WPF und MAUI stellen Vertragsstatus jetzt klarer dar: Vertragsdokumente verwenden statt des bloßen Statuswerts die lesbaren Bezeichnungen `Unsignierte Vertragsfassung` und `Signierte Vertragsfassung`.
- `DocumentInfo` wurde dafür nur minimal um UI-Hilfseigenschaften ergänzt, sodass WPF und MAUI denselben bestehenden Dokumentpfad weiterverwenden und keine Schattenlogik für Anzeige oder Folgeaktionen aufbauen.
- In WPF bleibt der Pfad bewusst auf Verwaltung, Kontrolle, Dokumentanzeige und Upload signierter Scan-Fassungen beschränkt; ein kleiner Hinweistext stellt nun explizit klar, dass die direkte digitale Signatur ausschließlich in MAUI erfolgt und die unsignierte Fassung erhalten bleibt.
- Die WPF-Folgeaktion für unsignierte Vertragsdokumente wurde sprachlich auf `Signierten Scan hochladen` geschärft; für signierte Enddokumente und Nicht-Vertragsdokumente erscheinen weiterhin keine unpassenden Vertrags-Sonderaktionen.
- In MAUI wurden die Dokumentaktionen ebenfalls geglättet: `Öffnen`, `Signierten Scan ablegen` und `Digital signieren` sind für unsignierte Vertragsdokumente sauber getrennt sichtbar; signierte Enddokumente und Nicht-Vertragsdokumente zeigen keine unpassenden Folgeaktionen.
- Ergänzend wurden Meta-/Kontexthinweise in MAUI so geschärft, dass Folgeaktionen verständlich bleiben und ausdrücklich sichtbar ist, dass die unsignierte Vertragsfassung bei Scan-Upload oder digitaler Signatur erhalten bleibt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen MAUI-Digitalsignatur-Block auf dem echten Branch-Stand `feature/formularverwaltung` ohne neuen Fachblock technisch sauber abgeschlossen.
- Im mobilen Dokumentpfad gibt es für vorhandene unsignierte Vertragsdokumente (`Mitgliedsvertrag`, `Pachtvertrag`) jetzt zusätzlich die Aktion `Digital signieren`; der bestehende Upload-/Statuspfad für signierte Fassungen bleibt dabei führend.
- Dafür wurde ein gemeinsames, MAUI-unabhängiges Signaturmodell `DigitalSignatureCapture` ergänzt, damit die erfassten Striche/Punkte nicht als App-spezifische Sonderlogik im UI hängen bleiben.
- Die dedizierte Seite `VertragsSignaturPage` stellt eine große Signaturfläche sowie `Leeren`, `Übernehmen` und `Abbrechen` bereit; auf Android wird während dieses Flows über `MainActivity` gezielt Querformat aktiviert und beim Verlassen wieder freigegeben.
- Im Shared-/Servicepfad wurde `CreateSignedVertragsdokumentAsync(...)` ergänzt: Die bestehende unsignierte Vertragsfassung wird geladen, um eine zusätzliche Signaturseite ergänzt und anschließend als eigenes `signiert`-Enddokument im vorhandenen Mitglieds-Dokumentpfad gespeichert.
- Das bestehende Dateinamenschema bleibt auch für die digital signierte Fassung erhalten; die unsignierte Ausgangsfassung wird weder überschrieben noch gelöscht.
- Es wurde bewusst keine neue WPF-UI-Erweiterung und keine zusätzliche Fachlogik ergänzt; der Block schließt nur den bereits begonnenen MAUI-Digitalsignaturpfad technisch ab.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den nächsten kleinen, buildfähigen Formularblock auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den Signatur-/Status-Folgepfad für Vertragsdokumente geschlossen.
- Der bestehende Dokumenttyp-/Statuspfad bleibt führend: `Mitgliedsvertrag` und `Pachtvertrag` werden weiterhin separat über `unsigniert` und `signiert` unterschieden; die unsignierte Fassung bleibt beim Nachpflegen einer unterschriebenen Fassung unverändert erhalten und wird weder überschrieben noch gelöscht.
- Dafür wurde im gemeinsamen Servicepfad `UploadSignedVertragsdokumentAsync(...)` ergänzt. Der Pfad akzeptiert nur bestehende unsignierte Vertragskontexte (`Mitgliedsvertrag`, `Pachtvertrag`) und nur PDF-Dateien; die signierte Fassung wird als eigenes Enddokument im bestehenden Mitglieds-Dokumentpfad abgelegt.
- Das bestehende Dateinamenschema bleibt erhalten und wird für die signierte Fassung sauber fortgeführt: `<Name_Vorname>-<ID>-<yyyy-MM-dd>-<Dokumenttyp>-signiert.pdf`.
- `DocumentInfo` erkennt jetzt explizit, ob ein Dokument ein Vertragsdokument ist und ob dazu im UI eine signierte Fassung abgelegt werden darf; damit bleiben signierte und unsignierte Fassungen im bestehenden Dokumentkontext getrennt sichtbar und nachvollziehbar.
- In WPF wurde der Folgepfad minimal in die bestehende Mitglieds-Dokumentliste eingebunden: Für vorhandene unsignierte Vertragsdokumente erscheint dort jetzt `Signierte Fassung`, ohne den übrigen Dokumentpfad oder die Listenstruktur umzubauen.
- In MAUI wurde derselbe kleine Folgepfad in `DokumentePage` ergänzt; auch mobil kann damit eine unterschriebene Fassung zu einem vorhandenen unsignierten Vertragsdokument abgelegt werden, ohne neue Parallelansicht oder Signatur-Canvas.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Noch bewusst offen bleibt die spätere echte digitale Signatur-/Canvas-Logik; der aktuelle Block schafft nur den anschlussfähigen Status- und Upload-Unterbau.

- Den nächsten kleinen Formularblock auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den Mitgliedsvertrag auf den bestehenden Formular-/Dokumentpfad gezogen.
- Dafür wurde ein gemeinsamer Produktivpfad `Mitgliedsvertrag (unsigniert)` ergänzt: `ISupabaseService` und `SupabaseService` besitzen jetzt `CreateMitgliedsvertragDokumentAsync(...)`, das wie die vorhandenen Formularfälle direkt über den bestehenden Dokument-Upload und die Mitgliedsablage läuft.
- Da für den Mitgliedsvertrag noch keine eigene offizielle PDF-Vorlage im Repo geführt wird, wird der Vertrag zunächst als saubere wiederverwendbare Vereins-PDF über den vorhandenen Branding-/Vorlagenpfad erzeugt; die Kapselung liegt in `MitgliedsvertragDokumentFactory`, sodass später leicht auf eine echte Vorlage gewechselt werden kann.
- Der Mitgliedsvertrag gibt jetzt mindestens Name, Vorname, Geburtsdatum soweit vorhanden, Adresse, Kontaktangaben, Mitgliedskontext, Eintritts-/Vertragsdatum sowie einen Unterschriftsbereich aus und bleibt beim bestehenden Dateinamenschema `<Name_Vorname>-<ID>-<yyyy-MM-dd>-mitgliedsvertrag-unsigniert.pdf`.
- Der gemeinsame Vereinsdokument-Builder wurde nur minimal geöffnet: Statt eines fest verdrahteten Mitgliedsantrags-Intros akzeptiert er jetzt einen formularspezifischen Introtext, sodass Mitgliedsantrag und Mitgliedsvertrag denselben Brandingpfad weiterverwenden können, ohne neue Schattenlogik aufzubauen.
- Im WPF-Mitgliedsdetail wird nach erfolgreicher Mitglied-Neuanlage jetzt direkt gefragt `Mitgliedsvertrag erstellen?`; bei Zustimmung wird der unsignierte Vertrag erzeugt, im bestehenden Dokumentpfad des Mitglieds gespeichert und bei verfügbarer Open-URL direkt geöffnet.
- Der bestehende MAUI-Mitgliedspfad wurde in diesem kleinen Block ebenfalls mitgezogen: Nach erfolgreicher Neuanlage erscheint dieselbe Nachfrage, und der Vertrag wird über denselben gemeinsamen Servicepfad erzeugt; dabei wurde der Busy-Guard so korrigiert, dass die Vertragserzeugung im Neuanlagepfad nicht blockiert wird.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Für den fachlichen Minimaltest existiert im aktuellen Workspace kein automatisierter UI-Testpfad; `get_tests` lieferte weder für `KGV.Wpf` noch für `KGV.Maui` passende Tests, daher wurde der Block build-validiert und der sichtbare Flow über die echten UI-/Servicepfade implementiert.

- Den begonnenen Pachtvertragsblock auf dem echten Branch-Stand `feature/formularverwaltung` sauber technisch geschlossen, ohne einen neuen Fachblock zu starten.
- Die offizielle Vorlage `Formulare/Pachtvertrag_KGV_bereinigt_mit_Feldern.pdf` ist jetzt bewusst im nachverfolgten Projektstand eingebunden und wird zentral aus `KGV.Core` als echte PDF-Formularvorlage verwendet; eine Ersatzvorlage oder ein nachgebautes Layout wird nicht verwendet.
- Dafür wurde der gemeinsame Produktivpfad für `Pachtvertrag (unsigniert)` ergänzt: `SaisonRecord` kennt jetzt `pacht_pro_qm`, `SupabaseService` erzeugt den Vertrag über `CreatePachtvertragDokumentAsync(...)`, und `PachtvertragDokumentFactory` berechnet den Pachtzins zentral als `Parzellenfläche * Saison.Pacht_pro_qm` mit kaufmännischer Rundung auf zwei Nachkommastellen.
- Fehlerfälle laufen nicht mehr still weiter: fehlende Saison, fehlendes `pacht_pro_qm`, ungültige oder fehlende Parzellenfläche sowie fehlende Pflicht-/Signaturfelder in der offiziellen PDF-Vorlage brechen den Produktivpfad jetzt mit klaren Fehlermeldungen ab.
- Die echte PDF-Vorlage wird direkt befüllt; mindestens Pächter 1/2, Geburtsdaten, Anschrift, Telefon, Parzellennummer, Parzellenfläche, Vertragsbeginn, `pacht_pro_qm`, Pachtzins, Betragskasten sowie Ort/Datum laufen über die vorhandenen Formularfelder.
- Im WPF-Mitgliedsdetail ist `NewContractCommand` jetzt produktiv, und nach erfolgreicher Parzellenzuweisung folgt im bestehenden Flow die Nachfrage `Pachtvertrag erstellen?`; bei Zustimmung wird der unsignierte Vertrag im bestehenden Mitglieds-Dokumentpfad gespeichert und bei verfügbarer Open-URL direkt geöffnet.
- Der begonnene Restfehler im WPF-Zuweisungspfad wurde minimal behoben, indem die Parzellen-ID vor dem Reload gesichert wird und der nachfolgende Vertragsdialog damit keinen Nullzugriff mehr auf die zurückgesetzte UI-Auswahl hat.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Für den fachlichen Minimaltest existiert im aktuellen Repo kein automatisierter UI-Testpfad; deshalb wurde der Block build-validiert und die Fehlerpfade wurden direkt im produktiven Codepfad abgesichert.

- Den nächsten kleinen Block der Formularverwaltung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den vorhandenen Zwischenstand direkt weiterverwendet.
- Der Mitgliedsantrag nutzt jetzt keine einfache technische Text-PDF mehr, sondern eine echte Vereinsvorlage mit sauberem Briefkopf, Vereinsname, Register-/Kontaktzeile und identischem Vereinslogo.
- Dafür wurde ein gemeinsamer Core-Pfad für Vereinsbranding und Vereins-PDF-Layout aufgebaut; spätere Dokumente wie Mitgliedsvertrag oder Pachtvertrag können denselben Briefkopf-/Abschnitts-/Unterschriftsrahmen mitbenutzen.
- Die Logo-Quelle bleibt fachlich konsistent: Das bestehende Vereinslogo aus `KGV.Maui/Resources/Images/kgv_logo.png` wird zentral als eingebettete Core-Ressource verwendet und im PDF gerendert.
- Der Mitgliedsantrag gibt jetzt strukturiert mindestens Antragsteller/in, Geburtsdatum, Adresse, Kontaktangaben, Mitgliedskontext sowie einen Unterschriftsbereich aus und bleibt auf dem bestehenden Mitglied-/Dokumentpfad gespeichert.
- Die Einstiege in WPF und MAUI bleiben bestehen und wurden nur minimal auf `Mitgliedsantrag als PDF` geglättet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-08
- Den ersten kleinen Grundblock für die neue Formularverwaltung auf dem Branch `feature/formularverwaltung` umgesetzt.
- Dafür eine gemeinsame Core-Grundlage für Formular-Dokumenttyp, Formular-Dokumentstatus, Dateinamenschema und einfache PDF-Erzeugung eingeführt; vorbereitet für `Mitgliedsantrag`, `Mitgliedsvertrag` und `Pachtvertrag` sowie `unsigniert`/`signiert`.
- Den ersten konkreten Formularfall `Mitgliedsantrag` über den bestehenden Mitglieds-/Dokumentpfad angebunden: Mitglied laden, PDF erzeugen, über den vorhandenen Dokument-Upload speichern und dem Mitglied zuordnen.
- In WPF und MAUI jeweils einen ersten sichtbaren Einstieg im Mitgliedskontext ergänzt; nach Erzeugung wird das abgelegte Dokument direkt geöffnet, sofern der bestehende Dokumentpfad bereits eine Open-URL liefert.
- Dokumentlisten können die neue Grundlage jetzt fachlich mindestens nach Typ und Status unterscheiden, ohne neue Schattenablage neben dem bestehenden Dokumentpfad aufzubauen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Im WPF-Mitgliedsdetail die E-Mail-Sperrlogik minimal korrigiert: Bei Neuanlage bzw. ohne bestehenden App-User bleibt das Feld editierbar.
- Gesperrt bleibt das Feld jetzt nur noch, wenn das Mitglied bereits einen App-User über `AuthUserId` hat; der OTP-Button wird nur in diesem Fall eingeblendet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich; `dotnet build KGV.Maui/KGV.Maui.csproj` scheitert in diesem Block extern an `java.exe`/Systemressourcen.

- Den WPF-UpdatePrompt minimal repariert, indem `AppUpdateInfo` wieder die erwartete Methode `GetNotesText()` bereitstellt.
- Dadurch kompiliert `KGV.Wpf/Views/UpdatePromptWindow.xaml.cs` wieder ohne `CS1061` auf `AppUpdateInfo.GetNotesText()`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Die bestehende Demodaten-Ausblendung auf dem zentralen Operational-/Sichtbarkeitspfad auch für Admin erweitert.
- Dazu `OperationalDataFilter` um zentrale Prüfungen für `Arbeitseinsatz`, `Termin` und `Bekanntmachung` ergänzt; `TerminRecord` und `BekanntmachungRecord` tragen dafür jetzt ebenfalls `is_demo`.
- In `SupabaseService` hängen jetzt sowohl die Verwaltungslisten als auch die Home-/Startseitenpfade für Arbeitseinsätze, Termine und Bekanntmachungen an denselben bestehenden Operational-Filtern; dadurch bleiben diese Demodatensätze nun auch im Adminkontext ausgeblendet.
- `GetMitgliederAsync()` blendet Demomitglieder jetzt ebenfalls zentral aus, sodass der bestehende Mitgliedspfad auch für Admin keinen Demomitglied-Listenstand mehr liefert.
- Die früher eingebauten Demo-Schalterreste im Impressum wurden in WPF und MAUI wieder sauber zurückgebaut; beide UIs filtern die Impressum-Kontakte jetzt nur noch über `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter im Abschlusslauf noch einmal direkt gegen `origin/main` nur in den drei Ziel-Dateien geprüft und anschließend minimal nachgeschärft.
- WPF verwendet für den Demo-Schalter nicht mehr den alten fehleranfälligen Converter-Pfad `Visibility="{Binding IsDemoToggleVisible, Converter={StaticResource BoolToVisibilityConverter}}"`, sondern explizit den bestehenden ViewModel-Pfad `Visibility="{Binding DemoToggleVisibility, Mode=OneWay}"`.
- In `KGV.Wpf/ViewModels/ImpressumViewModel.cs` erzwingt `ShowDemoData` jetzt zusätzlich logisch, dass Demo-Daten nur mit Admin-Rolle aktiv sein können.
- In `KGV.Maui/Pages/ImpressumPage.cs` bleibt der Schalter in der Seitenkonstruktion direkt unter der Beschreibungs-Label-Zeile eingebunden; `UpdateDemoToggleVisibility()` refiltert jetzt explizit, wenn ein Admin-Demostatus beim Rollenwechsel entfällt.
- Beide UIs laufen weiter über denselben bestehenden Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter im finalen Abschlusslauf robust auf die bestehende WPF-ViewModel-Sichtbarkeit umgestellt und den vorhandenen MAUI-Adminpfad explizit konsolidiert.
- WPF-Ursache des Absturzes war die `StaticResource`-Abhängigkeit am Demo-Schalter in `KGV.Wpf/Views/ImpressumView.xaml`; beim Öffnen der View konnte dieser Ressourcenpfad eine `XamlParseException` auslösen.
- Reale WPF-Behebung: Converter-Abhängigkeit vollständig entfernt und `Visibility` direkt an die bestehende `DemoToggleVisibility`-Property aus `KGV.Wpf/ViewModels/ImpressumViewModel.cs` gebunden.
- Reale MAUI-Ergänzung/Absicherung: `KGV.Maui/Pages/ImpressumPage.cs` nutzt den vorhandenen Demo-Schalter jetzt mit explizitem Standardzustand `aus` und zentraler Admin-Freigabe über `IsDemoToggleVisible`/`ShowDemoData`; Vorstand sieht ihn nicht.
- Beide UIs bleiben auf demselben bestehenden Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter auf dem echten Repo-Stand final für WPF und MAUI vereinheitlicht.
- WPF-Ursache des Absturzes war die `StaticResource`-/Converter-Abhängigkeit am Demo-Schalter in `KGV.Wpf/Views/ImpressumView.xaml`; im View-Kontext war dieser Pfad nicht robust genug und konnte in eine `XamlParseException` laufen.
- Behebung in WPF: `BooleanToVisibilityConverter` direkt lokal in der View als Ressource `BoolToVisibilityConverter` bereitgestellt und die Sichtbarkeit des Schalters an die bestehende ViewModel-Property `IsDemoToggleVisible` gebunden; `ShowDemoData` bleibt unverändert im bestehenden Pfad.
- MAUI nutzt auf `KGV.Maui/Pages/ImpressumPage.cs` denselben vorhandenen Schalterpfad weiter, jetzt mit expliziter zentraler Admin-Sichtbarkeit; Vorstand sieht ihn nicht, bei Nicht-Admin bleibt der Switch auf `aus`.
- Beide UIs laufen weiter über denselben bestehenden Demo-/Operativfilterpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich; passende automatisierte `Impressum`-Tests waren nicht vorhanden.

- Den noch offenen Impressum-Demo-Schalter-Block sauber abgeschlossen.
- WPF-Ursache des Absturzes war die Converter-/`StaticResource`-Abhängigkeit im Demo-Schalter der `ImpressumView`; dieser Pfad wurde robust entfernt und die Sichtbarkeit stattdessen direkt aus dem bestehenden ViewModel über `DemoToggleVisibility` abgeleitet.
- In MAUI wurde der fehlende Schalter `Demo-Datensätze einblenden` auf der `ImpressumPage` ergänzt, nur für Admin sichtbar und standardmäßig aus.
- Beide UIs filtern die sichtbaren Impressum-Kontakte jetzt auf demselben bestehenden gemeinsamen Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Ergebnis: WPF crasht beim Öffnen des Impressums nicht mehr und MAUI besitzt nun denselben Admin-only-Demo-Schalter mit identischem Fachverhalten.

- Den defekten Impressum-Demo-Schalter in WPF gegen den echten Runtime-Stand repariert und den fehlenden MAUI-Schalter ergänzt.
- WPF-Ursache war kein Fehler im ViewModel, sondern eine `StaticResource`-Referenz auf `BoolToVisibilityConverter`, die in `ImpressumView.xaml` lokal nicht auflösbar war und die View beim Öffnen in eine `XamlParseException` laufen ließ.
- Der WPF-Ressourcenfehler wurde minimal behoben, indem der benötigte `BooleanToVisibilityConverter` direkt in der View als Ressource bereitgestellt wurde; die vorhandenen ViewModel-Properties `IsDemoToggleVisible` und `ShowDemoData` bleiben unverändert im Einsatz.
- In MAUI wurde auf `ImpressumPage` derselbe Schalter `Demo-Datensätze einblenden` ergänzt, nur für Admin sichtbar, standardmäßig aus und ohne Sonderarchitektur.
- Beide UIs filtern die sichtbaren Impressum-Kontakte über denselben bestehenden gemeinsamen Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.

- Das WPF-Impressum um den fehlenden Demo-Schalter ergänzt.
- Der Schalter erscheint nur auf der Impressum-Seite und nur für Admin; Vorstand sieht ihn nicht.
- Standardzustand bleibt aus, sodass Demo-Datensätze ohne explizites Einblenden weiter verborgen bleiben.
- Der Schalter nutzt keine WPF-Sonderlogik, sondern filtert die Impressum-Kontakte über den bestehenden gemeinsamen Demo-/Operativfilterpfad.
- Ergebnis: WPF kann Demo-Kontakte im Impressum nur gezielt durch Admin sichtbar machen, ohne die bestehende Demo-/Reviewer-Isolierung aufzuweichen.

- In MAUI das Feld `arbeitsstunden_altersregel_typ` im Hauptmitglied-Kontext ergänzt.
- Umsetzung auf der Stammdatenseite sowie im Hauptmitglied-Anlegen/Bearbeiten als Picker mit genau den Werten `mann80` und `frau75`.
- Für Hauptmitglieder ist das Feld beim Anlegen jetzt Pflicht; im Save-Pfad wird nur einer der beiden zulässigen Werte akzeptiert.
- Beim Nebenmitglied bleibt das Feld fachlich irrelevant, wird mobil nicht angezeigt und nicht als Pflichtfeld behandelt.
- Der DTO-/Supabase-Pfad wurde minimal mitgezogen, damit der Wert für Hauptmitglieder produktiv geladen und gespeichert wird.

- Den realen MAUI-Save-/Reload-Pfad der Stammdatenseite geprüft und minimal korrigiert.
- Ursache war kein fehlendes Binding, sondern dass der Reload nach erfolgreichem Speichern noch innerhalb des aktiven Busy-Zustands lief und dadurch am Busy-Guard von `LoadAsync()` sofort abbrach.
- `MeineDatenPage` lädt nach erfolgreichem Speichern jetzt erzwungen frisch nach, ohne manuellen Seitenwechsel und ohne erneutes Öffnen der Seite.
- Ergebnis: Die geänderten Stammdaten sind direkt nach dem Speichern auf der sichtbaren MAUI-Stammdatenseite sichtbar.

- Die sichtbare MAUI-Stammdatenseite im Mitgliedskontext gezielt verschlankt.
- Sichtbar bleiben dort jetzt nur noch `Grunddaten`, `Kontakt`, `Adresse` und `Bemerkung`.
- Aus der sichtbaren Stammdatenseite entfernt wurden die Blöcke `Mitgliedschaft`, `Wartungsverträge / Pflichtstunden`, `Mitgliedskontext`, `Verwaltung` sowie der direkte Dokumente-Button.
- Die entfernten Inhalte bleiben fachlich über die vorhandenen Shell-Untermenüs erreichbar; WPF blieb in diesem Block unverändert.
- Zusätzlich `email_rechnung_einwilligung` und `email_info_einwilligung` im MAUI-Kontaktbereich als Schalter eingebunden und durch den DTO-/Supabase-Speicherpfad gezogen.
- Ergebnis: Die mobile Stammdatenseite ist klarer fokussiert, ohne dass die entfernten Fachbereiche oder die beiden E-Mail-Einwilligungen im Datenpfad verloren gehen.

- WPF-Impressum gegen den gemeldeten Runtime-Bindingfehler geprüft.
- Ursache war kein Fehler in `PropertyPathWorker`, sondern eine schreibgeschützte ViewModel-Eigenschaft `ClubEmail`, die in `ImpressumView.xaml` in einem `Run` ohne explizites `OneWay` gebunden war.
- Minimaler Fix: `ClubEmail` im WPF-Impressum explizit auf `Mode=OneWay` gesetzt.
- Ergebnis: Der Runtime-Fehler `TwoWay- oder OneWayToSource-Bindungen funktionieren nicht mit der schreibgeschützten Eigenschaft "ClubEmail"` ist auf diesem Pfad geschlossen.

- Den Demo-/Play-Store-Adminblock auf dem echten Supabase-/RLS-Stand umgesetzt statt nur über UI-Sichtbarkeit.
- Vorrangregel ergänzt: Demo-/Reviewer-Konten bleiben auch mit Rolle `admin`/`vorstand` auf Demo-Daten begrenzt; nur echte produktive Admins/Vorstand behalten Vollzugriff auf Produktivdaten.
- Dafür relevante Admin-Lese-/Übersichts-/Detailpfade u. a. für `app_user`, `mitglied`, `parzelle`, `dokument`, `termin`, `arbeitseinsatz`, `bekanntmachung`, `arbeitsstunden`, `wartungsverträge`, `zaehler` und zugehörige Zuordnungen/Übersichten auf getrennte Produktiv-/Demo-RLS umgestellt.
- `termin` und `bekanntmachung` erhielten echte `is_demo`-Kennzeichnung; `v_startseite_arbeitseinsatz` und `v_pflichtstunden_uebersicht` wurden auf RLS-wirksamen `security_invoker` zurückgeführt.
- Ergebnis: Der vorhandene Demouser kann fachlich als Admin genutzt werden, ohne echte Vereinsdaten aus Adminlisten, Details oder Übersichten offenzulegen.

- Den gemeldeten WPF-Buildrest um `HomeView.xaml` ehrlich gegen den aktuellen Workspace geprüft.
- Befund: Die ungültige WPF-XAML-Eigenschaft `Spacing` war in `HomeView.xaml` im realen Stand bereits nicht mehr vorhanden.
- Der echte verbleibende Buildblocker lag im laufenden DateOnly-Block in `SupabaseService`:
  - fehlender Helper `NormalizeStartseiteArbeitseinsatzRecord(...)`
  - fehlende nullable Überladung von `NormalizeDateOnly(...)`
- Diese beiden Reste wurden minimal ergänzt; `HomeView.xaml` selbst blieb unverändert.
- Ergebnis: `KGV.Wpf/KGV.Wpf.csproj` baut wieder erfolgreich.

## 2026-04-07
- Die Android-/MAUI-System-Zurück-Taste wurde auf dem bestehenden Shell-Modell nachgezogen:
  - tiefe Unterseiten gehen jetzt schrittweise Ebene für Ebene zurück
  - offene Modals werden zuerst geschlossen
  - wenn kein sinnvoller Stack-Rückweg mehr vorhanden ist, erfolgt der Fallback zur Startseite
  - nur auf `home` bleibt das bestehende App-Beenden mit Rückfrage aktiv
- Root-Switch-/Login-Kontexte bleiben zentral geschützt; während aktivem Root-/Mitgliedswechsel wird kein Rücksprung in alte Shell-Kontexte zugelassen.

## 2026-03-22
- Home auf **Übersicht + Admin-Einstiege** ausgerichtet; echte getrennte Verwaltungsviews statt Home-Platzhaltern vorbereitet.
- **Termine**, **Bekanntmachungen** und **Arbeitseinsätze** produktiv an die realen Fachpfade angeschlossen.
- Für Bekanntmachungen wurde ein kleiner **HTML-Editor** eingebaut.
- Für Arbeitseinsätze wurden die Fachregeln um **optionale Teilnehmergrenze** und **optionalen Stundenwert** korrekt nachgezogen.
- **Arbeitsstunden-Freigabe** mit globalem Review-Lock, Prüftabelle und wiederverwendetem Editor aufgebaut.

## 2026-03-23
- Zentralen **Datums-/Zeitbug** der Verwaltungseditoren behoben.
- Begonnenen **Home-/Detail-Block für Arbeitseinsatz** sauber abgeschlossen.
- Produktive **Insert-Pfade** gegen fehlerhafte feste IDs geprüft.
- Zwei echte Fehler im **Arbeitsstunden-Produktivpfad** behoben:
  - fehlerhafte `id = 0` beim Insert
  - Speichern in der Freigabeansicht reagierte nicht sauber auf Checkboxen
- WPF-Bindingfehler in der Arbeitsstunden-Ansicht bereinigt.

## 2026-03-24
- Große **MAUI-Struktur- und Prüfserie** gestartet, um die mobilen Produktivpfade systematisch an den WPF-Fachstand anzugleichen.
- Themenblöcke dabei u. a.:
  - Shell-/Menüstruktur
  - Stammdaten / Gärten / Parzellen
  - Dokumente / Strom / Wasser
  - Mitglied neu / Wartungsverträge
  - Arbeitsstunden
  - Ablesen / RFID / Zählerwechsel
  - Auth-Sonderwege / Dialogersatz
  - Altlasten- und Testpfad-Bereinigung
- Wirkung: viele kleine MAUI-Rückwege, Sichtbarkeiten, Refresh- und Kontextfehler wurden in produktiv nutzbare Bahnen gezogen.

## 2026-03-25
- **Launcher-Icon** in MAUI auf das echte Vereinslogo korrigiert.
- MAUI-Shell-/Menüordnung im Bereich **Admin-Menü / Mein Profil** geglättet.
- **Termine-Nutzerpfad** mobil weiter geschlossen.
- Direkte **Home-Verwaltungszugänge** in MAUI fertiggezogen.
- Artefakt-/FotoUploadTest-Block sowie lokale Bereinigungs-/Archivthemen sauber eingeordnet.

## 2026-03-26
- Größere Serie von **MAUI-Compilefehlern** in vielen Seiten gezielt bereinigt, u. a.:
  - `HomeManagementPage`
  - `MeineDatenPage`
  - `MyProfilePage`
  - `HomeSectionDetailPage`
  - `ParzellenPage`
  - `NebenmitgliedPage`
  - `UserManagementPage`
  - `RfidEinrichtenPage`
  - `MemberGardensPage`
  - `FaelligeZaehlerPage`
  - `ArbeitsstundenReviewDetailPage`
- Zusätzlich aktive Home-/Detail-/Editorpfade fachlich gehärtet.
- Wirkung: MAUI-Gesamtbuild und mehrere produktive Hauptpfade wurden wieder belastbar.

## 2026-03-27
- Projektweite **Prüfung aller Create-Pfade** gegen das ID-freie Insert-Schema; Aufrufer in WPF und MAUI nachgezogen.
- **Startseite, Stammdaten und Flyout-Texte** in MAUI geglättet.
- **Wartungsverträge** fachlich für Haupt- und Nebenmitglieder korrigiert.
- Begonnener **Parzellen-/RFID-Block** real validiert und Restfehler geschlossen.

## 2026-03-28
- **ReleaseManager** und Android-/WPF-Releasepfad deutlich ausgebaut:
  - reales Inno-Setup-Skript
  - Android-Signing mit Laufzeitpasswörtern
  - robuste Versionslesung aus den SDK-Style-`csproj`
- MAUI-Releasepfad für Android stabilisiert:
  - korrekte Icon-/Signing-Basis
  - Release ohne problematischen AOT-Zwang
  - Dateilogging / Logcat-Diagnose für Release verbessert
- Wirkung: reale Release-Erstellung wurde deutlich robuster und nachvollziehbarer.

## 2026-03-29
- **ReleaseManager** um Preflight-/Systemcheck, Commit/Push, Release-Marker, Delta-Export und Versions-Refresh erweitert.
- **Invite-/AuthService-Fix** für FK-Fehler beim Flow `Nutzer hinzufügen`.
- Android-Release-Buildblocker im MAUI-Service minimal behoben.
- MAUI-Mitgliedsdetail gegen den Git-/Repo-Stand erneut verifiziert.

## 2026-03-30
- **OTP-Erstlogin** auf serverseitige Supabase-Edge-Function umgestellt und live verifiziert.
- Problem mit gesperrten Passwortfeldern nach OTP in MAUI behoben.
- MAUI zeigt **OTP-Fehlercode** jetzt supporttauglich sichtbar an.
- Mitgliedskontext in WPF und MAUI stärker auf das **Admin-Menü** ausgerichtet.
- **Impressum-Block** für WPF und MAUI fachlich fertiggezogen.

## 2026-03-31
- Großer **Foto-Upload-Block** produktiv abgeschlossen:
  - lokale Pending-Grundlage
  - WLAN-only-Schalter
  - persistente Pending-Queue
  - Retry-/Sync-Logik
  - produktive Anbindung an Ablesung und Zählereinbau/Erstablesung
- **Wartungsverträge** in Schreibpfaden vollständig auch für Nebenmitglieder geöffnet.
- **Resume-/Timeout-Block** aufgebaut und abgeschlossen:
  - Hintergrundzeit erfassen
  - Login-/Root-Reset nach 15 Minuten
  - Guardrails für mobile Lifecycle-Situationen

## 2026-04-01
- **Dokumente** in WPF und MAUI stark ausgebaut und auf den bestehenden Google-Drive-Unterbau gezogen:
  - UI geglättet
  - Drive-Vertrag nachgeschärft
  - Löschen inkl. Drive-/DB-Abgleich
  - finaler Drive-Rootvertrag
- **Parzellen-/Mitgliedspfad** in MAUI weiter vervollständigt.
- **RFID-Quittungston** minimal ergänzt.
- **Google-Play-Testfähigkeit / Release-Readiness** der MAUI-App geprüft und an Reststellen geschlossen.

## 2026-04-02
- In MAUI öffnet die **Mitgliedersuche** das ausgewählte Mitglied jetzt direkt in **Stammdaten**.
- **Normale Nutzer** wurden im Dokumentbereich klar auf **View-only** begrenzt.
- WPF-Startseite und Verwaltungslisten für **Bekanntmachungen, Termine und Arbeitseinsätze** fachlich geschlossen.
- Kleiner WPF-Restfehler um `SelectedFileName` beseitigt.
- Zusätzlich Save/Rücknavigation/Home-Refresh in den Managementpfaden geglättet.

## 2026-04-04
- **Rechtebasis V1** und benutzerspezifische Fachrechte weiter fertiggezogen.
- Verbleibende direkte Rollenprüfungen im Meterbereich auf **`PermissionChecks`** gezogen.
- **Nutzer-Ablesung als Einreichung** mit Freigabefundament aufgebaut.
- Globaler **Admin-Schalter** für Nutzer-Ablesungen technisch sauber ergänzt.
- Korrektur und Entfernen eingereichter Ablesungen in den Freigabepfad eingebaut.
- **Zählereinbau + Erstablesung** gegen den aktuellen Stand end-to-end validiert.
- Ergebnis: Admin-Menü und Rechteoberfläche tragen nun deutlich mehr echte Fachlogik statt grober Rollenchecks.

## 2026-04-05
- Großer Rollen-/Rechteumbau abgeschlossen:
  - **`app_user.role`** ist jetzt die führende Rollenquelle
  - **`mitglied.role`** bleibt nur noch Altbestand
- Dazu wurden Client-, DTO-, Login-, Rechte- und SQL-/RPC-Pfade bereinigt.
- Feature-Branch **`feature/app-user-role-source`** wurde merge-reif geprüft und anschließend sauber nach **`main`** übernommen.
- Für benutzerspezifische Fachrechte wurden Diagnose- und Stabilitätsfixes ergänzt:
  - WPF-Dateilogging für den Permission-/App-User-Fehlerpfad
  - Predicate-Fix beim `app_user`-Lookup nach `mitglied_id`
  - Typfix im Save-Pfad (`int8` / `long?`)
  - transparenterer Reload-/Verify-Pfad
- Wirkung: Rollen- und Rechteverhalten ist fachlich konsistenter und technisch belastbarer.

## 2026-04-06
- Sehr viele **ToDo-/Restblöcke** zusammengeführt und abgeschlossen.
- Relevante Fachabschlüsse:
  - **Termin/Bekanntmachung löschen** in den echten Managementpfaden sauber geschlossen
  - **Home** lädt mobil beim Anzeigen wieder frisch
  - **Android-Back** zentral auf `home` geglättet
  - WPF-Bindingwarnung `MemberDTO.Name` beseitigt
  - Rechte-Reload in WPF/MAUI transparenter gemacht
  - `mitglied.role` im Code klar als Legacy markiert; Eigenkontext-/Globalrechte geglättet
  - mobile Arbeitsstunden-Prüfung an den bestehenden globalen **Review-Lock** angeglichen
  - sichtbaren Diagnosepfad aus `Ablesen` entfernt
  - WPF-Sichtbarkeiten in `Ablesen` sauber nachgezogen
  - sinnvolle Unterschiede aus lokalem Vergleichsstand zurück in `main` übernommen
  - Dokumente im Parzellen-/Mitgliedskontext weiter auf **view-only/Eigenkontext** geglättet
  - stale Detailansicht in `MemberParzellenDetailPage` beseitigt
- Mitglieder-/Nebenmitglied-Block stark erweitert:
  - `Mitglied neu anlegen` in WPF und MAUI
  - direkte Nachfrage nach Hauptmitglied-Neuanlage, ob ein **Nebenmitglied** angelegt werden soll
  - `Nebenmitglied`-Seite mit **Neu**-Einstieg
  - Reload/Pull-to-Refresh der Mitgliedersuche
  - `Mitgliedschaft beenden` mit Folgeentscheid für vorhandenes Nebenmitglied
  - Admin-Shell-Mitgliedskontext gehärtet
  - Nebenmitglied-Create fachlich vervollständigt
- Release-/Diagnosepfad weiter ausgebaut:
  - Google-Play-Diagnoseartefakte
  - Android-Upload-Ordner für manuelle Releases
- Wirkung: Viele zuvor verstreute Restpunkte wurden auf produktive Endpfade gezogen.

## 2026-04-07
- **Nebenmitglied-Bearbeiten** gegen `origin/main` geprüft, vorhandenen Rettungsstand fachlich bestätigt und validiert.
- MAUI-Nebenmitglied-Maske um die vorbereiteten Zusatzfelder vervollständigt.
- Android-Startup-/Normalstart-Problem intensiv eingegrenzt und zusammengeführt:
  - frühe Shell-/VisualElement-/Root-Pfade mehrfach entschärft
  - Diagnosepfade verbessert
  - frühe Probezugriffe entfernt
  - Root-/Loaded-Verzögerungen gegen stabile Zustände zurückgeführt
  - Logging für `InnerException` und frühe Android-Pfade gehärtet
  - Log zusätzlich auf extern lesbaren Android-Pfad gespiegelt
- Entscheidendster Befund:
  - der reale Startup-Crash passte zum **R8/ProGuard-Packagingpfad**
  - `AndroidLinkTool=r8` und `AndroidEnableProguard=true` wurden aus dem Releasepfad wieder entfernt
  - die zuvor fehlende Klasse `com.microsoft.maui.PlatformDispatcher` war danach wieder im finalen APK enthalten
- Ehrlicher Reststand nach diesem Tag:
  - der Codepfad wurde auf einen stabileren Zustand zurückgeführt
  - der unmittelbare reale Gerätetest des finalen Startup-Fixes blieb als externer Nachtest offen

## Wichtig für die weitere Nutzung dieses Logs

Diese kompakte Fassung ersetzt **nicht** jedes einzelne Diagnose- oder Prüfdetail der Langfassung.
Sie ist dafür gedacht, den Projektstand schnell zu verstehen, ohne sich durch hunderte kleinteilige Zwischenblöcke zu arbeiten.

Für die tägliche Arbeit reicht diese Fassung in der Regel aus, weil sie die folgenden Informationen beibehält:
- welche großen Blöcke erledigt wurden
- welche Architekturentscheidungen getroffen wurden
- welche Fachpfade produktiv sind
- welche wichtigen Resttests oder Grenzen noch existieren
