# NB-2A Mitgliedsnummer – MAUI-Audit

Stand: Audit auf Commit `08a76d6958e0466ad4687d75bb8f3c29d2a1ff17`. Gegenstand ist ausschließlich die Bestandsaufnahme für `KGV.Maui`; es wurden keine Anzeigen, Modelle, Services oder Abfragen geändert.

## Ausgangslage

- NB-1 hat die Datenbank-generierte, kanonische `mitglied.mitgliedsnummer` eingeführt (`mitglied.id + 1000`).
- Der Web-Teil von NB-2A ist abgeschlossen.
- Darstellungsstandard für eine reine Referenz: `Mgl.-Nr. <nummer>`.
- Mit Namen: `<Name> · Mgl.-Nr. <nummer>`.
- Die technische `mitglied.id` bleibt für FK, API-/RPC-Parameter, Navigation, Berechtigungen, State/Context, Auswahlwerte und Diagnose intern bestehen.

## User-facing Treffer

Alle folgenden Treffer sind Kategorie **B**. Die technische ID ist direkt sichtbar oder kann als sichtbarer Fallback erscheinen. Bei Umsetzungen darf kein Fallback auf die technische ID bestehen bleiben.

| Nr. | Datei | Stelle/Funktion | aktuelles Verhalten | benötigte Änderung | Abhängigkeit |
|---:|---|---|---|---|---|
| 1 | `KGV.Maui/Pages/AdminMenuPage.cs` | `UpdateSelectedMemberInfo` | Zeigt `Ausgewähltes Mitglied: <Name> (ID: <id>)`. | Name mit `· Mgl.-Nr. <nummer>` ausgeben. | `MemberContextState.SelectedMember` muss die Nummer tragen oder aus dem geladenen Mitglied stammen. |
| 2 | `KGV.Maui/ViewModels/UserManagementViewModel.cs` | `BoundMemberDisplayName`, `BoundMemberInfo` | Fallback `Mitglied #<id>` und zusätzlich `(ID: <id>)`. | Standardlabel verwenden; technische ID nicht ausgeben. | `MemberDTO`/MemberContext-Projektion. |
| 3 | `KGV.Maui/Pages/UserManagementPage.cs` | Detailbereich des ausgewählten App-Users | Bindet `SelectedUser.MitgliedId` direkt als Wert „Mitglied“. | Name und Mitgliedsnummer des verknüpften Mitglieds anzeigen; die ID nur als Auswahl-/Servicewert behalten. | `AppUserDTO` enthält nur die ID; Lookup oder erweiterte Projektionsform nötig. |
| 4 | `KGV.Maui/Pages/MemberDetailPage.cs` | Kopf, Mitgliedschaftsstatus und Umwandlungsdialog | Kopf und Haupt-/Nebenmitglied-Fallbacks zeigen `Mitglied #<id>`. | Kopf und Dialogtexte im Standardformat ausgeben. | Geladene `MitgliedRecord`-Objekte enthalten bereits `Mitgliedsnummer`; `MemberDTO`-Mapping ergänzen. |
| 5 | `KGV.Maui/Pages/MeineDatenPage.xaml.cs` | Kopf des Mitgliedskontexts | Fallback `Mitglied #<id>`. | Nummer statt ID ausgeben; bei Namen kombinieren. | `MapMember`/`MemberContextState`. |
| 6 | `KGV.Maui/Pages/MemberGardenAssignPage.cs` | Seitentitel | Fallback `Parzelle zuordnen für Mitglied #<id>`. | Standardlabel einsetzen. | Gewählter `MemberDTO` braucht Mitgliedsnummer oder die geladene `MitgliedRecord`-Instanz wird verwendet. |
| 7 | `KGV.Maui/Pages/MemberGardensPage.cs` | Seitentitel | Fallback `Gärten von Mitglied #<id>`. | Standardlabel einsetzen. | `MemberDTO`-Mapping/geladener Datensatz. |
| 8 | `KGV.Maui/Pages/MemberWartungsvertraegePage.cs` | Seitentitel und `BuildDisplayName` | Fallback `Mitglied #<id>`. | Nummer statt ID; bei Namen Name und Nummer ausgeben. | Das hier geladene `MitgliedRecord` hat die Nummer bereits. |
| 9 | `KGV.Maui/Pages/MitgliedsantragDialogPage.cs` | bestehendes Mitglied / gesetzlicher Vertreter | Picker-Fallback `Mitglied #<id>`; Optionslabel `<Name> · #<id>`. | Picker und Bestätigungstexte auf den Standard umstellen. | `MitgliedRecord` liefert die Nummer bereits. |
| 10 | `KGV.Maui/Pages/NebenmitgliedPage.cs` | Titel „Nebenmitglied für …“ | Fallback `Mitglied #<id>`. | Standardlabel einsetzen. | Hauptmitglied ist als `MitgliedRecord` geladen. |
| 11 | `KGV.Maui/Pages/PachtvertragDialogPage.cs` | Pächter/in-, Vertreter- und Nebenmitglied-Labels | `BuildMemberDisplayName` zeigt bei Name `Name · #<id>`, sonst `Mitglied #<id>`. | Einheitlich `Name · Mgl.-Nr. <nummer>` bzw. reine Referenz. | Alle beteiligten `MitgliedRecord`-Objekte haben die Nummer bereits. |
| 12 | `KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs` | Kandidatenliste für Vertragszuordnung | Fallback `Mitglied #<id>`. | Kandidatenlabel um Mitgliedsnummer ergänzen und ID-Fallback ersetzen. | Mitgliedsliste liefert `MitgliedRecord` mit Nummer. |
| 13 | `KGV.Maui/ViewModels/MemberSearchViewModel.cs` | Suchresultat-Fallback | Ohne Name/E-Mail: `Mitglied <id>`. | `Mgl.-Nr. <nummer>` ausgeben und Namen ggf. um die Nummer ergänzen. | Mapper nach `MemberDTO`/`MemberSearchResultItem`; `MitgliedRecord` hat Nummer. |
| 14 | `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs` | `BuildMemberDisplay` | Ohne Vor-/Nachname: `Mitglied <mitgliedId>`. | Fachliches Mitgliedslabel mit Nummer einsetzen. | `ArbeitsstundeDTO` enthält nur `MitgliedId` und Namen; fachliche Auflösung/Projektion erforderlich. |
| 15 | `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs` | Fehlertext in `TryResolveApproverId` | Sichtbarer Text „Genehmiger-MitgliedId fehlt.“ | Fachlich formulieren, z. B. „Genehmiger-Kontext fehlt.“ | Keine Nummer erforderlich. |
| 16 | `KGV.Maui/Pages/MyArbeitsstundenPage.cs` | fehlender Mitgliedskontext | Sichtbarer Text „MitgliedId fehlt.“ | Fachlich formulieren, z. B. „Mitgliedskontext fehlt.“ | Keine Nummer erforderlich. |
| 17 | `KGV.Maui/Pages/MemberParzellenDetailPage.cs` | Pachtvertrags-Validierung | Alert nennt „Mitglieds-ID“. | Fachlich formulieren, z. B. „Mitgliedskontext fehlt.“ | Keine Nummer erforderlich. |

## Technische Treffer, die bleiben dürfen

Kategorie **A**. Die umfangreichen Verwendungen von `Id`, `MemberId` und `MitgliedId` in `KGV.Maui` sind überwiegend technische Schlüssel und nicht Teil der fachlichen Anzeige.

- **Navigation und Auswahlzustand:** Shell-Routen, `MemberContextState`, `ParzellenContextState`, Picker-Auswahlwerte, Dictionaries und CollectionView-Selection verwenden die technische ID.
- **FKs und Persistenz:** `MitgliedId`, `HauptmitgliedId`, `NebenmitgliedId`, `VorstandMitgliedId` und ähnliche Felder in Requests, Entwürfen, Wartungsvertrags- und Dokumentzuordnungen bleiben unverändert.
- **Service-, RPC- und Repository-Aufrufe:** Parameter für `ISupabaseService`, Vertrags-, Dokument-, Arbeitsstunden-, Arbeitsdienst- und RFID-Workflows bleiben technische IDs.
- **Berechtigung und Kontext:** `PermissionChecks`, der eigene Mitgliedskontext, Rollenauflösung sowie Gleichheitsprüfungen bleiben ID-basiert.
- **Diagnose und Infrastruktur:** `AppFileLog`, `Debug.WriteLine`, `Console.WriteLine`, lokale Warteschlangen, Dateinamen, Cache-/Storage-Schlüssel sowie Support-IDs bleiben technische Diagnosewerte. Sie sind keine normalen Benutzeroberflächen.

## Unklare Treffer

1. **Name-only-Labels:** Mehrere Listen (etwa Arbeitseinsatz-Teilnehmer, Vereinsfunktionen, Parzellen und Kommunikation) zeigen derzeit nur Namen bzw. E-Mail, aber keine technische ID. Vor der Umsetzung ist fachlich zu entscheiden, ob der verbindliche Standard auch dort die Mitgliedsnummer zusätzlich verlangt. Dieser Audit-Block erfasst nur sichtbare technische IDs bzw. deren Fallbacks.
2. **Arbeitsstunden-Prüfung:** `ArbeitsstundeDTO` führt Name und `MitgliedId`, jedoch keine Mitgliedsnummer. Vor M3 muss entschieden werden, ob die bestehende Übersichtsdatenprojektion die Nummer ergänzt oder die Page eine gebündelte Mitgliederauflösung erhält. Pro Eintrag ist kein N+1-Lookup vorzusehen.
3. **Benutzerverwaltung:** `AppUserDTO.MitgliedId` ist ein sichtbares Direktbinding. Vor M1 ist festzulegen, ob die Mitgliedsnummer als Feld einer Benutzer-Projektion geliefert oder über eine einmalige Mitgliederliste aufgelöst wird.

## MAUI-Abhängigkeiten außerhalb KGV.Maui

Für die spätere Anzeige sind folgende bereits vorhandene Core-/Infrastructure-Teile relevant. Sie werden in diesem Audit nicht geändert.

| Datei | warum relevant | späterer Bearbeitungsblock |
|---|---|---|
| `KGV.Core/Models/MitgliedRecord.cs` | Hat die kanonische, DB-gemappte Property `Mitgliedsnummer`; die regulären `GetMitgliederAsync`-/`GetMitgliedByIdAsync`-Abfragen laden sie bereits. | M1–M4 verwenden, keine Neuberechnung. |
| `KGV.Core/Models/MemberDTO.cs` | Enthält `Id` und `DisplayName`, aber keine Mitgliedsnummer; `SelectedMember` und mehrere MAUI-Mapper verlieren die Information. | M1; Core-Änderung separat prüfen. |
| `KGV.Core/Models/AppUserDTO.cs` | Die sichtbare Benutzerverwaltung enthält nur `MitgliedId`, keine fachliche Mitgliedsprojektion. | M1; Projektion/Lookup festlegen. |
| `KGV.Core/Models/ArbeitsstundeDTO.cs` | Für den sichtbaren Arbeitsstunden-Fallback sind `MitgliedId` und Namen vorhanden, nicht die Mitgliedsnummer. | M3; Übersichtsdatenprojektion ergänzen oder Lookup bündeln. |
| `KGV.Infrastructure/Services/SupabaseService.cs` | `GetMitgliederAsync`, `GetMitgliedByIdAsync` und `GetMitgliedByAuthUserIdAsync` laden `MitgliedRecord` vollständig; keine explizite Select-Projektion verhindert die Nummer. | M1–M4 nur prüfen, falls ein Spezialpfad abweicht. |

## Implementierungsblöcke

### NB-2A-M1 – Mitgliedskontext und Benutzerverwaltung

Status: **erledigt**.

Dateien (voraussichtlich 4–6): `MemberDTO`/zugehörige Mapping-Stellen, `AdminMenuPage.cs`, `UserManagementViewModel.cs`, `UserManagementPage.cs`, gegebenenfalls die App-User-Projektion.

Ersetzt die direkt sichtbaren IDs im globalen Mitgliedskontext und in der Benutzerverwaltung. `MemberDTO` trägt die kanonische Nummer, einschließlich Clone/MemberContext. Die Benutzerverwaltung löst `AppUserDTO.MitgliedId` durch eine einmal je Ladevorgang geladene Mitgliederliste mit Lookup auf; es gibt keine Einzelabfragen je App-User und `AppUserDTO` bleibt die technische Zuordnung.

### NB-2A-M2 – Mitgliedsstammdaten, Nebenmitglied und Mitgliedsantrag

Dateien (voraussichtlich 5–6): `MemberDetailPage.cs`, `MeineDatenPage.xaml.cs`, `NebenmitgliedPage.cs`, `MitgliedsantragDialogPage.cs` und die dafür genutzten MAUI-Mapper.

Vereinheitlicht Köpfe, Haupt-/Nebenmitglied-Bezüge, Umwandlungsdialog und Vertreterpicker. Die geladenen `MitgliedRecord`-Objekte liefern die Nummer bereits.

### NB-2A-M3 – Arbeitsstunden und Kontextfehler

Dateien (voraussichtlich 3–5): `ArbeitsstundenReviewDetailPage.cs`, `MyArbeitsstundenPage.cs`, ggf. `ArbeitsstundeDTO` und die zugehörige Übersichtsdatenprojektion.

Ersetzt den Arbeitsstunden-Fallback und die sichtbaren technischen Kontextfehler. Die Prüfungsübersicht erhält eine einmalige, testbare fachliche Mitgliedsauflösung.

### NB-2A-M4 – Parzellen, Pachtvertrag und Wartungsverträge

Dateien (voraussichtlich 5–6): `MemberGardenAssignPage.cs`, `MemberGardensPage.cs`, `MemberParzellenDetailPage.cs`, `PachtvertragDialogPage.cs`, `MemberWartungsvertraegePage.cs`, `WartungsvertragAssignMembersPage.cs`.

Vereinheitlicht Titel, Pachtvertragsparteien, Vertragszuordnung und technische Kontextfehlermeldungen. Technische IDs in Vertragsrequests und Navigation bleiben unverändert.

### NB-2A-M5 – Mitgliedssuche und abschließende Regression

Dateien (voraussichtlich 3–4): `MemberSearchViewModel.cs`, die betroffenen Mapper/Result-Items sowie vorhandene MAUI-Contract-Tests oder ergänzende fokussierte Tests.

Ersetzt den Such-Fallback durch die Mitgliedsnummer und prüft, dass alle in M1–M4 angefassten Labels den gemeinsamen Darstellungsstandard verwenden.

Jeder Block ist unabhängig implementierbar, auf seinen Fachbereich begrenzt, separat testbar und soll mit einem eigenen Commit und Push abgeschlossen werden. PDF-/Dokumentgeneratoren, Exporte, Datenbankmigrationen und KGV.Web sind ausdrücklich nicht Teil dieser Blöcke.
