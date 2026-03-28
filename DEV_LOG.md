# KGV Entwicklungslog

---

## 2026-03-28 – Prompt 1/1: MAUI-Parzellenverwaltung um Listenansicht und Suche ergänzt

- Zuerst den echten Istzustand im aktuellen Repo geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
  - `KGV.Core/Models/ParzelleVerwaltungItem.cs`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
- Reale Ursache, warum die Liste bisher fehlte:
  - `ParzellenPage` sprang direkt in den bestehenden Detailpfad
  - im `ParzellenViewModel` gab es nur die interne Sammlung `Items`, aber keine such-/listenfähige, an die UI gebundene Übersicht
  - dadurch existierte keine echte auswählbare Parzellenliste und keine Suche oberhalb der Detailansicht
- Minimal ergänzt, ohne neuen Fachumfang:
  - `KGV.Core/Models/ParzelleVerwaltungItem.cs`
    - listenbezogene Anzeige für Pächter als `Name, Vorname`
    - SortKey- und Search-Text für stabile Sortierung und Filterung ergänzt
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
    - `FilteredItems` und `SearchText` ergänzt
    - Filterung nach `Garten Nr` und Pächtername ergänzt
    - stabile Sortierung nach vorhandenem `GartenNrSortKey` gezogen
    - bestehende Detailladung bleibt weiter an `SelectedItem` und `GetParzelleDetailAsync(...)` gebunden
  - `KGV.Maui/Pages/ParzellenPage.cs`
    - oberhalb der bestehenden Detailsektionen eine suchbare `CollectionView` ergänzt
    - Liste zeigt je Eintrag nur `Garten Nr` und den aktuellen Pächter
    - freie Parzellen werden neutral als `Nicht verpachtet` angezeigt
    - Detailsektionen darunter bleiben auf dem bestehenden Pfad erhalten
- Ergebnis:
  - Suche filtert nach `Garten Nr` und Pächtername
  - Antippen eines Listeneintrags setzt `SelectedItem`
  - die vorhandenen Stammdaten-/Belegungsdetails werden darunter weiter über den bestehenden Pfad geladen
  - der Mitglieds-/Kontextpfad bleibt erhalten, weil weiterhin dieselbe `SelectedItem`- und Context-Logik verwendet wird
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - im MAUI-Build bleiben nur die bereits bekannten Warnungen (`XA1037`, Nullability in `KGV.Maui/Pages/HomeManagementPage.cs`)

## 2026-03-27 – Prompt 3/3 Abschluss: MAUI-Parzellenverwaltung und RFID-Einrichten gegen den realen Blockstand validiert, Restfehler bereinigt und Abschluss vorbereitet

- Zuerst den echten Istzustand des begonnenen Parzellen-/RFID-Blocks geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
  - die real geänderten Blockdateien im Arbeitsbaum:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Core/Models/ParzelleDetailDTO.cs`
    - `KGV.Core/Models/ParzelleRecord.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Maui/Pages/ParzellenPage.cs`
    - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
    - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
- Real bestätigt auf dem aktuellen Zwischenstand:
  - die MAUI-Parzellenseite ist fachlich auf reduzierte Stammdaten + Belegung gezogen
  - sichtbar bleiben in `Stammdaten` genau `ID`, `Garten Nr`, `Fläche`, `hat Wasser`, `hat Strom`, `rfid Wasser`, `rfid Strom`, `Anlage`
  - `Belegung` zeigt bei freier Parzelle den Button `Zuweisen`
  - bei belegter Parzelle wird `Name, Vorname` als Button/Link zur Mitgliedsstammdatenansicht verwendet
  - die Flächen-Schutzabfrage ist real in `ParzellenPage.SaveStammdatenAsync()` vorhanden und fragt Änderungen an `Fläche` vor dem Speichern ausdrücklich ab
- Reale Ursache der leeren Medium-Liste in `RFID einrichten`:
  - die Medium-Auswahl wurde im begonnenen Block im MAUI-ViewModel zuvor nur UI-seitig aus `HatStrom` / `HatWasser` aufgebaut
  - zusätzlich fehlte im Shared-Service die dazu passende zentrale fachliche Auswahlprüfung
  - Parzellen mit inkonsistentem oder nicht belastbarem Ausstattungszustand konnten dadurch in der Mediumsauswahl leer laufen
- Minimal bereinigt, ohne neuen Fachumfang:
  - `ISupabaseService` um den gemeinsamen Abfragepfad für verfügbare RFID-Medien ergänzt
  - `SupabaseService` liefert die RFID-Medien jetzt zentral aus derselben fachlichen Wahrheit:
    - vorhandene Ausstattung (`hat_strom` / `hat_wasser`)
    - bereits hinterlegte RFID am Medium
    - aktiver Zähler am Medium
    - Fallback auf beide Medien, wenn der Ausstattungszustand nicht belastbar ist
  - dieselbe gemeinsame Service-Logik wird in `CheckParzelleRfidAssignmentAsync(...)` verwendet; die Prüfung verlässt sich damit nicht mehr nur auf UI-Filterung
  - `RfidEinrichtenViewModel` lädt die Medium-Auswahl asynchron aus dem gemeinsamen Service statt lokaler `Hat...`-Sonderlogik
  - `ParzellenPage` compile-bedingt auf den real reduzierten MAUI-C#-Pfad bereinigt, damit die Abschlussseite sauber baut
- Validierung:
  - dateibezogene Fehler auf den direkt betroffenen Blockdateien nach den Korrekturen unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - im grünen MAUI-Build bleiben nur bekannte Warnungen, insbesondere `XA1037` und vorhandene Nullability-Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` war für diesen MAUI-Abschlussblock nicht zwingend und wurde nicht zusätzlich gestartet
- Blockstatus:
  - der begonnene MAUI-Parzellen-/RFID-Block ist fachlich und technisch abgeschlossen; offen blieb vor dem Git-Abschluss nur noch die organisatorische Commit-/Push-Abwicklung

## 2026-03-27 – Prompt 2/3 Abschluss: Wartungsverträge-Nebenmitglieder-Block final validiert, Migration abgeglichen und sauber abgeschlossen

- Echten Abschluss-Istzustand des begonnenen Blocks erneut geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - reale geänderte Wartungsvertragsdateien im Arbeitsbaum
  - neue Migration `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
- Im Arbeitsbaum real vorhanden und abschließend bestätigt:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/WartungsvertragDetailItem.cs`
  - `KGV.Wpf/ViewModels/WartungsvertragMitgliederZuordnungViewModel.cs`
  - `KGV.Wpf/Views/WartungsvertragDetailView.xaml`
  - `KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs`
  - `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Fachkonsistenz des Blocks bestätigt:
  - WPF und MAUI nutzen weiterhin denselben Shared-/Infrastructure-Pfad über `ISupabaseService`
  - Nebenmitglieder bleiben in Anzeige, Detail und Zuweisung erhalten und werden nicht mehr still auf das Hauptmitglied umgebogen
  - Pflichtstunden-/Wartungsvertragsbezug ist mitgliedsbezogen angezogen; die Migration bildet dieselbe fachliche Wahrheit wie der App-Code ab
- Direkter Restfehler dieses Blocks minimal bereinigt:
  - `KGV.Wpf/Views/WartungsvertragDetailView.xaml` zeigt den Mitgliedskontext jetzt auch real im Grid an; damit entspricht die WPF-Detailansicht dem begonnenen Blockstand und der MAUI-Detaildarstellung
- Abschlussvalidierung:
  - dateibezogene Prüfung auf den Blockdateien unauffällig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - bekannte Warnungen bleiben unverändert bestehen, u. a. `XA1037` und Nullability-Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
- Blockstatus:
  - der Wartungsverträge-Nebenmitglieder-Block ist damit technisch und organisatorisch abgeschlossen

## 2026-03-27 – Prompt 2/3: Wartungsverträge für Haupt- und Nebenmitglieder in WPF und MAUI fachlich geradegezogen

- Zuerst den echten Istzustand auf Basis des gepushten Stands `4857f64` geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
  - betroffene Wartungsvertrags-Pfade in `KGV.Core`, `KGV.Infrastructure`, WPF und MAUI
- Zu eng auf Hauptmitglieder laufende Fachlogik gefunden in:
  - `SupabaseService`: `GetWartungsvertraegeForMitgliedAsync`, `GetAssignableWartungsvertraegeForMitgliedAsync`, `AssignMitgliederToWartungsvertragAsync` und `AssignWartungsvertraegeToMitgliedAsync` normalisierten Nebenmitglieder auf `HauptmitgliedId`
  - WPF- und MAUI-Zuweisungslisten filterten Nebenmitglieder zusätzlich komplett aus den auswählbaren Mitgliedern heraus
  - die Datenbankmigration enthielt in `validate_wartungsvertrag_zuordnung()` noch die harte Regel "nur Hauptmitglieder zulassen"
  - der Pflichtstunden-/Wartungsvertragsbezug lief in der Migration und im Shared-Service weiter zu stark über Hauptmitglied-Pfade
- Bereinigung dieses Blocks:
  - Shared-/Infrastructure-Pfade auf direkte Mitgliedszuordnung für Wartungsverträge umgestellt; Nebenmitglieder werden nicht mehr auf Hauptmitglieder umgebogen
  - WPF- und MAUI-Zuweisungslisten schließen aktive Nebenmitglieder nicht mehr künstlich aus
  - WPF-Detailansicht zeigt den Mitgliedskontext jetzt zusätzlich explizit an; MAUI-Detail nutzt denselben gemeinsamen Kontexttext im Untertitel
  - neue Migration `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql` ergänzt:
    - Validierung erlaubt Wartungsvertragszuordnungen auch für Nebenmitglieder
    - Pflichtstunden-/Wartungsvertragsbezug liefert wieder mitgliedsbezogene Zeilen statt still nur Hauptmitgliedern
- Ergebnis:
  - Anzeige, Detail und Zuweisung verwenden jetzt dieselbe fachliche Wahrheit für Haupt- und Nebenmitglieder
  - bestehende WPF-/MAUI-Pfade wurden weiterverwendet; keine neue Schattenlogik, keine neue Seite
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - offen bleiben nur die bekannten Warnungen, u. a. `XA1037` sowie Nullability-Warnungen in `HomeManagementPage.cs`

## 2026-03-27 – Prompt 1/3: MAUI-Startseite, Stammdaten und Navigationstext minimal bereinigt

- Zuerst den echten Istzustand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
  - die aktiven MAUI-Pfade `HomePage`, `HomeViewModel`, `MemberContextState`, `MeineDatenPage`, `AdminShell`, `UserShell`, `MemberSearchPage`
- Home/Startseite korrigiert:
  - `MemberContextState` um eine Änderungsbenachrichtigung ergänzt
  - `HomePage` reagiert jetzt auf Kontextwechsel, invalidiert den ViewModel-Zustand und lädt beim Anzeigen sofort neu
  - dadurch wird Home beim ersten Öffnen bzw. nach Mitgliedswechsel mit dem aktuellen Kontext geladen statt mit veraltetem Cache
  - der Button `Arbeitsstunde erfassen` hängt weiter am bestehenden Produktivpfad, wird aber jetzt mit dem aktiven Kontext sauber neu bewertet
  - `HomeViewModel.UserContextText` zeigt den echten Home-Kontext jetzt explizit an: ausgewähltes Mitglied für Admin/Vorstand, sonst eigenes Mitglied
- Stammdaten entschlackt:
  - auf `MeineDatenPage` die Bereiche `Mitgliedschaft`, `Wartungsverträge / Pflichtstunden` und `Verwaltung` aus der sichtbaren Seite entfernt
  - `Mitgliedskontext` wird nur noch angezeigt, wenn wirklich eine Beziehung existiert
  - sichtbar ist dort jetzt fachlich klar `Hauptmitglied` mit verknüpftem Nebenmitglied oder `Nebenmitglied` mit zugeordnetem Hauptmitglied
  - ohne echte Verknüpfung bleibt der Bereich vollständig ausgeblendet
- Navigationstext korrigiert:
  - im Admin-Flyout `Arbeitsstunden · Arbeitsstunden freigeben` auf `Arbeitsstunden freigeben` reduziert
  - dieselbe Kürzung gilt auch für den Zählertext mit offenen Freigaben
- Validierung:
  - dateibezogene Fehler auf den geänderten MAUI-Dateien blieben unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief erfolgreich durch
  - offen bleiben nur bekannte Warnungen, unter anderem `XA1037` und Nullability-Warnungen in `HomeManagementPage.cs`

## 2026-03-27 – Prompt 1/1: bekannte MAUI-Altfehler gezielt bereinigt und Gesamtbuild wieder grün gezogen

- Zuerst den echten Istzustand auf Basis des gepushten Stands `85a7859` geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
  - echten Build `dotnet build KGV.Maui/KGV.Maui.csproj`
- Reales Fehlerbild laut aktuellem MAUI-Build:
  - `KGV.Maui/App.xaml.cs`: `InitializeComponent` nicht vorhanden
  - `KGV.Maui/Pages/AblesungErfassenPage.cs`: `IPlatformApplication` nicht vorhanden
  - `KGV.Maui/Pages/ZaehlerwechselPage.cs`: `IPlatformApplication` nicht vorhanden
  - `KGV.Maui/Settings/AppSettings.cs`: `FileSystem` nicht vorhanden
  - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`: `InitializeComponent` nicht vorhanden
- Minimal bereinigt, ohne neuen Fachumfang:
  - `App.xaml.cs`: leeren XAML-Initialisierungspfad entfernt; die App erzeugt ihr Root-Window weiter rein über den bestehenden C#-Pfad
  - `AblesungErfassenPage.cs` und `ZaehlerwechselPage.cs`: Servicezugriff von nicht vorhandenem `IPlatformApplication` auf den vorhandenen MAUI-Kontext `Application.Current?.Handler?.MauiContext?.Services` umgestellt
  - `AppSettings.cs`: fehlenden MAUI-Storage-Namensraum für `FileSystem.AppDataDirectory` ergänzt
  - `MemberSearchPage.xaml.cs`: fehlende XAML-Initialisierung durch denselben Seitenaufbau in C# ersetzt, damit der produktive Page-Pfad wieder sauber kompiliert
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` läuft jetzt erfolgreich durch
  - offen bleiben nur Warnungen, u.a. die bekannte Android-Warnung zu `AndroidCreatePackagePerAbi` und vorhandene Nullability-Warnungen in `HomeManagementPage.cs`
  - zusätzlicher Sicherheitslauf `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief ebenfalls erfolgreich durch

## 2026-03-27 – Prompt 4/4: projektweite Abschlussprüfung der Create-/Insert-Architektur durchgeführt und letzte Reststelle bereinigt

- Zuerst den echten Istzustand gegen den gepushten Stand `f3b37dc` geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/InsertRecordMappingExtensions.cs`
  - realen Git-Status über den vorgegebenen Visual-Studio-Git-Pfad
- Projektweite Suchläufe auf dem aktuellen Repo-Stand durchgeführt:
  - `.Insert(` über alle C#-Dateien
  - `From<...Record>().Insert(...)` bzw. `Insert(new ...Record` über alle C#-Dateien
  - `Id = 0`, `id = 0`, `entryId=0`, `entryId = 0` über C#-/XAML-Dateien
  - Aufrufsuche für die produktiven Create-Pfade `Nebenmitglied`, `Stromzähler`, `Wasserzähler`, `Ablesung`, `Arbeitsstunde`, `Wartungsvertrag`, `Arbeitseinsatz`, `Termin`, `Bekanntmachung`
- Ergebnis der Gesamtsuche:
  - die bereits bekannten produktiven Create-Pfade in `SupabaseService` und ihren WPF-/MAUI-Aufrufern laufen auf Insert-Modelle bzw. Create-Requests ohne `Id`
  - für die betroffenen produktiven UI-Pfade blieb die Suche nach `Id = 0` und `entryId=0` leer
  - letzte verbleibende produktive Reststelle war ein Insert im Auth-Bereich: `AuthService` legte `app_user` noch über `AppUserRecord` statt über ein separates Insert-Modell an
- Bereinigung dieses Abschlussblocks:
  - neues Insert-Modell `KGV.Infrastructure/Models/AppUserInsertRecord.cs` ergänzt
  - `KGV.Infrastructure/Authentication/AuthService.cs` auf `client.From<AppUserInsertRecord>().Insert(...)` umgestellt
  - damit laufen nun auch die produktiven `app_user`-Neuanlagen nicht mehr über einen DB-Record-Typ
- Abschlussbewertung:
  - Create bleibt ohne `Id`
  - Update bleibt getrennt über bestehende Record-/ID-Pfade
  - `CreateNebenmitgliedAsync(...)` ist weiterhin produktiv über `NebenmitgliedCreateDTO` aktiv; ein alter Nebenmitglied-Altvertrag oder Altaufrufer wurde im aktuellen Repo-Stand nicht mehr gefunden
  - im aktuellen produktiven Suchraum ist die Create-Architektur jetzt auf Insert-Modelle bzw. Create-Requests ohne `Id` vereinheitlicht

## 2026-03-27 – Prompt 2/4 Abschlusslauf: begonnenen Create-Aufrufer-Block gegen den echten Repo-Stand final geprüft

- Erneut den realen Istzustand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/InsertRecordMappingExtensions.cs`
  - die bereits betroffenen WPF-/MAUI-Aufrufer
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
- Im aktuellen Stand bestätigt:
  - WPF- und MAUI-Create-Aufrufer für `Arbeitsstunden`, `Arbeitseinsätze`, `Termine`, `Bekanntmachungen`, `Wartungsverträge`, `Stromzähler`, `Wasserzähler` und `Ablesung` zeigen auf Insert-Modelle ohne `Id`
  - der WPF-Aufrufer für `CreateNebenmitgliedAsync(...)` verwendet produktiv `NebenmitgliedCreateDTO`
  - `entryId = 0` bzw. `Id = 0` bleibt nur UI-intern; die echten Create-Pfade senden keine `Id` mehr an Supabase
  - Update-Pfade bleiben weiter an echte bestehende IDs gebunden
- Validierung im Abschlusslauf:
  - gezielte Dateifehler auf allen bereits geänderten Blockdateien blieben unauffällig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erneut erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb ausschließlich an bereits vorhandenen blockfremden Altfehlern in `App.xaml.cs`, `AblesungErfassenPage.cs`, `ZaehlerwechselPage.cs`, `AppSettings.cs` und `MemberSearchPage.xaml.cs` rot
- Ergebnis:
  - keine direkten Restfehler dieses Prompt-2/4-Blocks mehr offen
  - der gemeinsame Create-Aufrufer-Block ist fachlich abgeschlossen und wird jetzt organisatorisch per Commit/Push abgeschlossen

## 2026-03-27 – Prompt 2/4 Abschluss: compile-bedingte WPF-/MAUI-Aufrufer auf neue Insert-Signaturen gezogen und Block sauber abgeschlossen

- Zuerst den echten Istzustand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - die neuen Insert-Modelle in `KGV.Core/Models`
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- Ehrlicher Befund vor der Anpassung:
  - die gemeinsamen Verträge und Insert-Modelle ohne `Id` waren bereits vorhanden
  - compile-bedingt offen waren nur direkte WPF-/MAUI-Aufrufer, die noch alte Create-Signaturen verwendeten
  - betroffen laut echtem Buildfehlerbild waren Create-Aufrufer für:
    - `Ablesung`
    - `Stromzaehler`
    - `Wasserzaehler`
    - `Arbeitsstunde`
    - `Arbeitseinsatz`
    - `Termin`
    - `Bekanntmachung`
    - `Wartungsvertrag`
    - `Nebenmitglied`
- Umsetzung bewusst minimal und nur compile-bedingt:
  - WPF-Aufrufer in den betroffenen ViewModels auf die neuen gemeinsamen Create-Signaturen gezogen
  - MAUI-Aufrufer in den betroffenen Pages/ViewModels auf dieselben Insert-Signaturen gezogen
  - kleiner gemeinsamer Helper `KGV.Core/Models/InsertRecordMappingExtensions.cs` ergänzt, aber nur für die wirklich mehrfach betroffenen Record-zu-Insert-Abbildungen:
    - `ArbeitsstundeRecord`
    - `ArbeitseinsatzRecord`
    - `TerminRecord`
    - `BekanntmachungRecord`
    - `WartungsvertragRecord`
  - Zähler-/Ablesungs-Aufrufer bewusst ohne Schattenlogik direkt auf `...InsertRecord` umgestellt
  - WPF-Nebenmitglied-Aufrufer auf `CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request)` gezogen; der gemeinsame Servicevertrag bleibt damit die einzige Wahrheit und der produktive Pfad ist wieder nutzbar
  - beim echten Create läuft jetzt kein DB-Record mit `Id` und kein `id = 0` mehr in die umgestellten Insert-Pfade
- Technische Verifikation:
  - gezielte Dateifehler auf allen direkt geänderten Blockdateien blieben nach der Umstellung unauffällig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` läuft jetzt wieder erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist hinsichtlich der betroffenen Insert-Aufrufer bereinigt; offen bleiben im aktuellen Workspace nur bereits vorhandene blockfremde MAUI-Fehler in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
- Ergebnis:
  - die direkten WPF-/MAUI-UI-Aufrufer zeigen jetzt auf die Insert-Modelle ohne `Id`
  - `CreateNebenmitgliedAsync(...)` ist im produktiven gemeinsamen Vertrag wieder nutzbar
  - der Prompt-2/4-Block ist fachlich für das Signatur-Nachziehen abgeschlossen; dokumentierte blockfremde MAUI-Altfehler wurden bewusst nicht in diesen Block hineingezogen

## 2026-03-27 – Prompt 1/4: Insert-Modelle ohne `Id` im gemeinsamen Core-/Infrastructure-Unterbau eingeführt und Servicevertrag auf Create-vs-Update getrennt

- Zuerst den echten Repo-/Log-/Vertragsstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - die betroffenen Create-Blöcke in `KGV.Infrastructure/Services/SupabaseService.cs`
  - die vorhandenen Record-/DTO-Dateien in `KGV.Core/Models`
- Ehrlicher Befund vor der Umstellung:
  - bereits sauber im Insert-Sinne waren nur die vorhandenen Pfade für `ParzellenBelegung` und die bestehende Infrastructure-Abbildung für `Arbeitsstunde`
  - noch unsauber bzw. nicht produktiv im gemeinsamen Vertrag waren die Create-Pfade für:
    - `Nebenmitglied`
    - `Stromzaehler`
    - `Wasserzaehler`
    - `Ablesung`
    - `Wartungsvertrag`
    - `WartungsvertragZuordnung`
    - `Arbeitseinsatz`
    - `Termin`
    - `Bekanntmachung`
  - diese Pfade basierten im aktuellen Stand noch auf DB-Records mit `Id` oder waren wie `CreateNebenmitgliedAsync` noch auf `Unavailable(...)`
- In diesem Block bewusst nur Core/Infrastructure umgesetzt:
  - neue Insert-Modelle ohne Primärschlüssel `Id` angelegt:
    - `KGV.Core/Models/MitgliedInsertRecord.cs`
    - `KGV.Core/Models/StromzaehlerInsertRecord.cs`
    - `KGV.Core/Models/WasserzaehlerInsertRecord.cs`
    - `KGV.Core/Models/AblesungInsertRecord.cs`
    - `KGV.Core/Models/ArbeitseinsatzInsertRecord.cs`
    - `KGV.Core/Models/TerminInsertRecord.cs`
    - `KGV.Core/Models/BekanntmachungInsertRecord.cs`
    - `KGV.Core/Models/WartungsvertragInsertRecord.cs`
    - `KGV.Core/Models/WartungsvertragZuordnungInsertRecord.cs`
  - `ISupabaseService` für betroffene Create-Pfade sauber auf Insert-Modelle ohne `Id` getrennt:
    - `CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request)`
    - `AddStromzaehlerAsync(StromzaehlerInsertRecord request)`
    - `AddWasserzaehlerAsync(WasserzaehlerInsertRecord request)`
    - `AddAblesungAsync(AblesungInsertRecord request)`
    - `AddArbeitsstundeAsync(ArbeitsstundeInsertRecord request)`
    - `CreateWartungsvertragAsync(WartungsvertragInsertRecord request)`
    - `CreateArbeitseinsatzAsync(ArbeitseinsatzInsertRecord request)`
    - `CreateTerminAsync(TerminInsertRecord request)`
    - `CreateBekanntmachungAsync(BekanntmachungInsertRecord request)`
  - `SupabaseService` auf dieselben Signaturen nachgezogen und die betroffenen Create-Insert-Pfade auf die neuen Insert-Modelle umgestellt
  - die beiden bestehenden Wartungsvertrag-Zuordnungspfade verwenden intern jetzt ebenfalls `WartungsvertragZuordnungInsertRecord`
- Bewusst noch nicht in diesem Block umgesetzt:
  - keine breite WPF-/MAUI-Aufruferumstellung
  - `CreateNebenmitgliedAsync` ist im Vertrag jetzt sauber für den späteren Produktivpfad vorbereitet, in der Implementierung aber in diesem Block noch nicht reaktiviert
- Technische Verifikation:
  - gezielte Dateifehler auf Interface, Service und neue Insert-Modelle blieben unauffällig
  - `dotnet build KGV.Core/KGV.Core.csproj` lief erfolgreich durch
  - `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj` lief erfolgreich durch
  - im Infrastructure-Build blieben nur bereits vorhandene Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber kein blockierender Fehler dieses Strukturblocks

## 2026-03-27 – Prompt 1/1: Mitglieds-Unterpunkte optisch wie `↳ Wartungsverträge` vereinheitlicht, ohne Rechte-/Routingänderung

- Zuerst den echten Repo-/Navigationsstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/UserShell.cs`
  - `KGV.Wpf/ViewModels/MainWindowViewModel.cs`
- Ehrlicher Befund vor der Korrektur:
  - im aktuellen MAUI-Shell-Stand waren die relevanten Mitglieds-Unterpunkte bereits mit `↳` konsistent aufgebaut
  - die optische Uneinheitlichkeit saß im WPF-Mitgliedsbereich in `MainWindowViewModel`
  - betroffen waren dort genau die Labels/Einrückungen von:
    - `Stammdaten`
    - `Arbeitsstunden`
    - `Dokumente`
    - `Admin-Menü`
    - `Benutzerverwaltung`
- Umsetzung bewusst klein und nur im Darstellungsblock:
  - in `KGV.Wpf/ViewModels/MainWindowViewModel.cs` alle fünf genannten Mitglieds-Unterpunkte auf dasselbe sichtbare Präfix `↳` wie bei `Wartungsverträge` gezogen
  - dieselben fünf Unterpunkte zusätzlich auf dieselbe Einrückung/Margin wie `↳ Wartungsverträge` vereinheitlicht
  - Reihenfolge, Zielseiten, Rechte und Sichtbarkeitslogik blieben unverändert
  - MAUI wurde nach Prüfung bewusst nicht geändert, weil der Shell-Stand dort bereits konsistent war
- Technische Verifikation:
  - `get_errors` auf `KGV.Wpf/ViewModels/MainWindowViewModel.cs` blieb unauffällig
  - passender WPF-Build `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - der WPF-Build zeigte dabei nur bereits vorhandene Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber keinen blockierenden Fehler für diesen kleinen UI-Darstellungsblock
- Ergebnis:
  - die genannten Mitglieds-Unterpunkte wirken jetzt optisch einheitlich zum bestehenden Stil von `↳ Wartungsverträge`
  - es wurde keine neue Fachlogik und keine Routing-/Rechteänderung eröffnet

## 2026-03-27 – Prompt 1/1 Abschlusslauf: begonnenen WPF-Wartungsverträge-Bugfixblock geprüft, Logs nachgezogen und für sauberen Git-Abschluss eingegrenzt

- Vor dem Abschlusslauf erneut nur den echten Repo-/Log-/Blockstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - genau die ausdrücklich genannten Blockdateien:
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml`
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml.cs`
    - `KGV.Wpf/ViewModels/WartungsvertragMitgliederZuordnungViewModel.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
  - gezielte Dateifehler auf genau diesen vier WPF-/Service-Dateien
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Stand:
  - der begonnene kleine WPF-Wartungsverträge-Bugfix war in den genannten Blockdateien bereits produktiv vorhanden
  - Doppelklick in der globalen Wartungsverträge-Liste öffnet jetzt denselben Detailpfad wie der vorhandene `Öffnen`-Button
  - die Mitgliedersortierung im Zuordnungsdialog läuft jetzt belastbar über Nachname/Vorname statt nur über den zusammengesetzten Anzeigenamen
  - die echte Save-Ursache lag im Insertpfad `wartungsvertrag_zuordnungen`; dort werden `created_at` und `updated_at` jetzt gesetzt
  - Postgrest-/DB-Details werden im direkt betroffenen Save-Fehlerpfad jetzt sauberer in Logger und Debug-Ausgabe sichtbar
  - in diesen vier Blockdateien zeigte sich im aktuellen Abschlusslauf kein weiterer direkter Korrekturbedarf; zusätzlicher Produktivcode wurde deshalb nicht mehr geändert
- Technische Verifikation dieses Abschlusslaufs:
  - `get_errors` auf den vier Blockdateien blieb unauffällig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - der WPF-Build blieb dabei nur mit bereits vorhandenen Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber ohne blockierenden Fehler für diesen kleinen Wartungsverträge-Abschlussblock
- Ergebnis dieses kleinen WPF-Bugfixblocks:
  - der WPF-Wartungsverträge-Bugfixblock ist fachlich abgeschlossen
  - für den Git-Abschluss werden nur die vier echten Blockdateien plus die beiden Logdateien aufgenommen; blockfremde offene Dateien bleiben ausdrücklich draußen

## 2026-03-27 – Prompt 3/3 Abschlusslauf: mitgliedsbezogenen Wartungsverträge-Block ehrlich fortgeschrieben und für sauberen Git-Abschluss eingegrenzt

- Zuerst erneut nur den echten Repo-/Log-/Blockstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - genau die ausdrücklich genannten Prompt-3/3-Blockdateien:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Core/Models/MemberWartungsvertragItem.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Wpf/ViewModels/MemberWartungsvertraegeViewModel.cs`
    - `KGV.Wpf/Views/MemberWartungsvertraegeView.xaml`
    - `KGV.Maui/Pages/MemberWartungsvertraegePage.cs`
  - gezielte Dateifehler auf genau diesen Dateien
  - `get_tests` mit Bezug auf `Wartungsvertrag`
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Abschlussstand:
  - der mitgliedsbezogene Wartungsverträge-Flow war in den genannten Blockdateien bereits produktiv ergänzt
  - die Shared-Servicepfade für freie Wartungsverträge je Mitglied, Zuweisen an genau ein Mitglied und Beenden über `gueltig_bis` waren im aktuellen Stand vorhanden
  - WPF-Mitgliedsansicht und `KGV.Maui/Pages/MemberWartungsvertraegePage.cs` waren dateibezogen ohne zusätzlichen Abschlussfehler
  - deshalb wurden keine weiteren Fachänderungen am Produktivcode dieses Prompt-3/3-Blocks vorgenommen; nachgezogen wurden nur die Abschlusslogs
- Technische Verifikation:
  - `get_errors` auf den sechs Blockdateien blieb unauffällig
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte weiterhin keinen passenden Testfall
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb im aktuellen Workspace nur blockfremd rot in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - diese MAUI-Fehler wurden bewusst nicht repariert, weil sie nicht zu diesem Wartungsverträge-Abschlussblock gehören
- Ergebnis:
  - Prompt `3/3` ist fachlich abgeschlossen
  - der Git-Abschluss wird auf die echten Blockdateien plus Logdateien begrenzt; blockfremde offene Änderungen bleiben draußen

## 2026-03-27 – Prompt 2/3 Abschlusslauf: vorhandenen Wartungsverträge-Blockcommit geprüft und sauber nach `origin/main` gepusht

- Den echten Git-Istzustand über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad geprüft:
  - Branch `main`
  - `HEAD` auf `74d0f72`
  - `74d0f72` lokal vorhanden
  - `origin/main` vor dem Lauf genau einen Commit hinter dem lokalen Stand
- Den vorhandenen Commit `74d0f72` gegen die ausdrücklich genannten Wartungsverträge-Blockdateien geprüft:
  - kein neuer Fach- oder Abschlussfehler in den Blockdateien
  - deshalb keine neuen Codeänderungen am Wartungsverträge-Block vorgenommen
- Buildstatus bewusst nicht neu zu einem Reparaturblock ausgerollt:
  - WPF bleibt im zuletzt verifizierten Stand grün
  - MAUI bleibt für diesen Abschlusslauf nur blockfremd dokumentiert rot in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
- Den vorhandenen Block-Commit anschließend ohne weitere Fachänderung erfolgreich nach `origin/main` gepusht.
- Blockfremde offene lokale Änderungen im Arbeitsbaum blieben bewusst ungestagt und außerhalb dieses Wartungsverträge-Abschlusses.

## 2026-03-27 – Prompt 2/3: begonnenen Wartungsverträge-Verwaltungsblock auf realem Zwischenstand fertiggestellt, MAUI-Anschlüsse geschlossen und Rückkehrpfade saubergezogen

- Zuerst den echten aktuellen Repo-/Log-/Dateistand geprüft statt neu anzusetzen:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - die ausdrücklich genannten Shared-/WPF-/MAUI-Wartungsverträge-Dateien des begonnenen Prompt-2/3-Blocks
  - gezielte Dateifehler auf genau diesen Blockdateien
- Ehrlicher Befund im vorhandenen Zwischenstand:
  - der Shared-Servicepfad für `CreateWartungsvertragAsync(...)`, `UpdateWartungsvertragAsync(...)` und `AssignMitgliederToWartungsvertragAsync(...)` war bereits real vorhanden
  - die WPF-Verwaltungsseiten `WartungsvertragEditor` und `WartungsvertragMitgliederZuordnung` waren im aktuellen Stand fachlich weitgehend fertig angelegt
  - unvollständig war vor allem der MAUI-Verwaltungsanschluss:
    - `WartungsvertragEditorPage` und `WartungsvertragAssignMembersPage` existierten bereits, waren aber noch nicht vollständig an DI-/Routing-/Detailpfade angeschlossen
    - `WartungsvertraegePage` bot noch keinen produktiven Einstieg `Neu`
    - `WartungsvertragDetailPage` hatte den begonnenen Verwaltungsblock noch nicht mit `Bearbeiten` / `Mitglieder zuweisen` und sauberem Reload nach Rückkehr abgeschlossen
- Umsetzung deshalb bewusst klein und nur auf den begonnenen Wartungsverträge-Block begrenzt:
  - WPF:
    - `WartungsvertraegeVerwaltungViewModel` für den fertigen Verwaltungsstand sprachlich geschärft
    - `EditCommand`/`NewCommand`-Requery bei Auswahl-/Busy-Wechsel sauber nachgezogen
    - `WartungsvertragDetailViewModel`-Beschreibung auf den nun produktiven Verwaltungsstand angepasst
  - MAUI:
    - `WartungsvertragEditorPage` und `WartungsvertragAssignMembersPage` über `MauiProgram` und `ShellRouteRegistrar` produktiv angeschlossen
    - `WartungsvertraegePage` um den produktiven Admin-/Vorstand-Einstieg `Neu` ergänzt
    - globale Detailnavigation aus der Übersichtsseite jetzt mit explizitem `adminMode=1`, damit Verwaltungsaktionen nur im globalen Verwaltungsweg sichtbar werden und nicht unbeabsichtigt im mitgliedsbezogenen Detailpfad von Prompt `3/3`
    - `WartungsvertragDetailPage` um `Bearbeiten` und `Mitglieder zuweisen` ergänzt
    - `WartungsvertragDetailPage` lädt beim Wiedererscheinen jetzt bewusst erneut, damit `Belegt` / `Frei` / Mitgliederliste nach Speichern oder Zuweisung nicht in einem Altzustand hängen bleiben
    - Busy-/Aktionszustände für Übersicht, Detail und Zuweisungsseite sichtbar deaktiviert bzw. konsistent gehalten
- Fachlich bestätigter Stand des fertiggestellten Prompt-2/3-Blocks:
  - Wartungsvertrag `Neu` produktiv vorhanden
  - Wartungsvertrag `Bearbeiten` produktiv vorhanden
  - globale Mitgliederzuweisung produktiv vorhanden
  - Kontingent wird weiterhin fachlich im Shared-Service abgesichert; freie Plätze bleiben in WPF und MAUI sichtbar, zusätzliche Auswahl wird bei Erreichen des Kontingents deaktiviert bzw. serverseitig sauber blockiert
  - Create bleibt ohne fehlerhafte DB-ID; Update bleibt an echte bestehende ID gebunden
  - Rückkehr nach Speichern führt wieder in die passende View:
    - WPF zurück in die gemeinsame Detailansicht
    - MAUI zurück in die passende Detailansicht bzw. bei `Neu` in die neu angelegte Detailansicht
- Bewusst nicht vorgezogen:
  - kein mitgliedsbezogener Zuweisungsflow aus der Mitgliedsansicht
  - keine Historienverwaltung / kein Beenden bestehender Zuordnungen als eigener Fachblock
  - keine Massenoperationen
  - Prompt `3/3` bleibt damit ausdrücklich offen
- Technische Verifikation:
  - `get_errors` auf den direkt geänderten Blockdateien blieb unauffällig
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte weiterhin keinen passenden Testfall
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb im aktuellen Workspace nicht grün, aber blockfremd an bereits offenen Fehlern in:
    - `KGV.Maui/App.xaml.cs` (`InitializeComponent`)
    - `KGV.Maui/Settings/AppSettings.cs` (`FileSystem`)
    - `KGV.Maui/Pages/AblesungErfassenPage.cs` (`IPlatformApplication`)
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs` (`IPlatformApplication`)
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs` (`InitializeComponent`)
  - die direkt geänderten Wartungsverträge-Dateien selbst blieben dateibezogen fehlerfrei

## 2026-03-27 – Prompt 1/3 Abschlusslauf: Wartungsverträge-Basisblock in WPF und MAUI technisch sauber final validiert

- Vor dem Abschlusslauf erneut nur den realen Repo-/Log-/Blockstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - ausschließlich die bereits zum Wartungsverträge-Basisblock gehörenden Shared-/WPF-/MAUI-Dateien
  - gezielte Dateifehler auf genau diesen Blockdateien
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
  - realen Git-Status über den ausdrücklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Abschlussstand:
  - die bereits angelegten Wartungsverträge-Dateien in `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` waren dateibezogen im aktuellen Stand konsistent
  - in diesem Abschlusslauf mussten an den Blockdateien keine zusätzlichen Fach- oder Compilekorrekturen mehr vorgenommen werden
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte aktuell keinen passenden Testfall; ein zusätzlicher Testlauf war daher für diesen Block nicht möglich
  - blockfremde bereits vorhandene Änderungen im Arbeitsbaum wurden bewusst nicht angefasst und nicht in den Abschluss dieses Blocks gezogen
- Technische Verifikation:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief erfolgreich durch
  - der MAUI-Build zeigt im aktuellen Workspace weiterhin nur bereits vorhandene Nullable-Warnungen, aber keinen blockierenden Fehler für diesen Block
- Finaler Blockumfang in diesem Abschlusslauf:
  - Shared/Core/Infrastructure:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Core/Models/WartungsvertragRecord.cs`
    - `KGV.Core/Models/WartungsvertragZuordnungRecord.cs`
    - `KGV.Core/Models/WartungsvertragOverviewItem.cs`
    - `KGV.Core/Models/WartungsvertragDetailItem.cs`
    - `KGV.Core/Models/MemberWartungsvertragItem.cs`
  - WPF:
    - `KGV.Wpf/ViewModels/MainWindowViewModel.cs`
    - `KGV.Wpf/Infrastructure/Services/NavigationService.cs`
    - `KGV.Wpf/App.xaml`
    - `KGV.Wpf/ViewModels/WartungsvertraegeVerwaltungViewModel.cs`
    - `KGV.Wpf/ViewModels/MemberWartungsvertraegeViewModel.cs`
    - `KGV.Wpf/ViewModels/WartungsvertragDetailViewModel.cs`
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml`
    - `KGV.Wpf/Views/MemberWartungsvertraegeView.xaml`
    - `KGV.Wpf/Views/WartungsvertragDetailView.xaml`
    - `KGV.Wpf/Views/WartungsvertragDetailView.xaml.cs`
  - MAUI:
    - `KGV.Maui/AdminShell.cs`
    - `KGV.Maui/UserShell.cs`
    - `KGV.Maui/ShellRouteRegistrar.cs`
    - `KGV.Maui/MauiProgram.cs`
    - `KGV.Maui/Pages/WartungsvertraegePage.cs`
    - `KGV.Maui/Pages/MemberWartungsvertraegePage.cs`
    - `KGV.Maui/Pages/WartungsvertragDetailPage.cs`
- Abgrenzung:
  - Prompt `2/3` und Prompt `3/3` wurden in diesem Abschlusslauf bewusst nicht fachlich vorgezogen
  - offen bleiben damit weiterhin ausdrücklich weitergehende Funktionen wie Neu/Bearbeiten, Zuweisen per Checkbox, Historienbearbeitung und Kontingent-Auswahllogik

## 2026-03-27 – MAUI-Menüpunkt `Wartungsverträge` wieder in die Navigation aufgenommen, über vorhandenen Mitgliedskontext statt neuer Seite

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - WPF nur lesend als fachliche Referenz:
    - `KGV.Wpf/ViewModels/MainWindowViewModel.cs`
    - `KGV.Wpf/ViewModels/MemberWartungsvertraegeViewModel.cs`
    - `KGV.Wpf/ViewModels/WartungsvertraegeVerwaltungViewModel.cs`
    - `KGV.Wpf/Views/MemberWartungsvertraegeView.xaml`
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml`
  - aktive MAUI-Navigationspfade:
    - `KGV.Maui/AdminShell.cs`
    - `KGV.Maui/UserShell.cs`
    - `KGV.Maui/ShellRouteRegistrar.cs`
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - in WPF existieren aktuell für `Wartungsverträge` nur vorbereitete Placeholder (`MemberWartungsvertraegeViewModel` / `WartungsvertraegeVerwaltungViewModel`); ein eigener produktiv verdrahteter Hauptnavigationspunkt war im aktuellen Stand nicht mehr aktiv
  - fachlich liegt das Thema in WPF damit näher am Mitgliedskontext als an einem losen allgemeinen Root-Menü
  - in MAUI existierte ebenfalls noch keine eigene Seite oder Route für `Wartungsverträge`
  - der belastbare bestehende mobile Produktivpfad war bereits vorhanden: `MeineDatenPage` enthält die produktive Sektion `Wartungsverträge / Pflichtstunden`
- Umsetzung bewusst klein und nur im MAUI-Navigationspfad:
  - in `KGV.Maui/AdminShell.cs` unter dem ausgewählten Mitgliedskontext den Menüpunkt `↳ Wartungsverträge` ergänzt
  - in `KGV.Maui/UserShell.cs` im eigenen Mitgliedskontext den Menüpunkt `↳ Wartungsverträge` ergänzt
  - beide Menüeinträge verwenden bewusst den vorhandenen Zielpfad `MeineDatenPage`
  - keine neue Seite, keine neue Dummy-Route und kein größerer Flyout-Umbau
  - `Wartungsverträge` ist damit in MAUI kontextgebunden sichtbar:
    - Admin/Vorstand im ausgewählten Mitgliedskontext
    - User im eigenen Mitgliedskontext
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen Navigationsdateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Workspace erfolgreich durch
  - der MAUI-Gesamtbuild blieb nach diesem kleinen Navigationsblock grün

## 2026-03-27 – Neuen blockfremden MAUI-Compile-Rest in `ShellRouteRegistrar`, `MemberSearchViewModel`, `AblesenOverviewPage`, `ArbeitseinsaetzeManagementPage`, `BekanntmachungenManagementPage`, `MemberSearchPage.xaml.cs` und `TermineManagementPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - `KGV.Maui/ViewModels/MemberSearchViewModel.cs`
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeManagementPage.cs`
  - `KGV.Maui/Pages/BekanntmachungenManagementPage.cs`
  - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - `KGV.Maui/Pages/MemberSearchPage.xaml`
  - `KGV.Maui/Pages/TermineManagementPage.cs`
  - gezielte Dateifehler für genau diese MAUI-Dateien
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die sichtbaren Fehler lagen erneut nicht in Fachlogik, sondern in fehlenden aktiven MAUI-Usings bzw. Typauflösungen
  - betroffen waren vor allem:
    - `Routing` in `ShellRouteRegistrar.cs`
    - `Command` in `MemberSearchViewModel.cs`
    - `LineBreakMode` und `CornerRadius` in `AblesenOverviewPage.cs`
    - `Shell` in `ArbeitseinsaetzeManagementPage.cs`, `BekanntmachungenManagementPage.cs` und `TermineManagementPage.cs`
  - `MemberSearchPage.xaml.cs` wurde zusammen mit `MemberSearchPage.xaml` geprüft; `x:Class`, Namespace, Basistyp und Code-Behind passen im aktuellen Stand zusammen und waren nicht der Fehlerursprung
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/ShellRouteRegistrar.cs` `using Microsoft.Maui.Controls` ergänzt
  - in `KGV.Maui/ViewModels/MemberSearchViewModel.cs` `using Microsoft.Maui.Controls` ergänzt
  - in `KGV.Maui/Pages/AblesenOverviewPage.cs` `using Microsoft.Maui` ergänzt
  - in `KGV.Maui/Pages/ArbeitseinsaetzeManagementPage.cs` `using Microsoft.Maui.Controls` ergänzt
  - in `KGV.Maui/Pages/BekanntmachungenManagementPage.cs` `using Microsoft.Maui.Controls` ergänzt
  - in `KGV.Maui/Pages/TermineManagementPage.cs` `using Microsoft.Maui.Controls` ergänzt
  - an `MemberSearchPage.xaml.cs` und `MemberSearchPage.xaml` keine Codeänderung, weil der XAML-/Code-Behind-Vertrag im aktuellen Stand bereits konsistent ist
  - keine Fachlogik, keine Navigation, keine Shell-/Routing-/CRUD-/Supabase-Pfade umgebaut
- Technische Verifikation:
  - `get_errors` auf den sieben Zielpfaden blieb nach der Korrektur unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Workspace wieder erfolgreich durch
  - der MAUI-Gesamtbuild ist mit diesem kleinen Compileblock wieder grün

## 2026-03-27 – Blockfremden MAUI-Startup-Compilefehlerstand in `MauiProgram`, `App.xaml.cs` und `MainApplication` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/App.xaml.cs`
  - `KGV.Maui/MainApplication.cs`
  - gezielte Dateifehler für genau diese drei MAUI-Startup-Dateien
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die sichtbaren Fehler in genau diesen drei Dateien lagen nicht in Fachlogik, sondern in fehlenden aktiven MAUI-Startup-Namespaces
  - betroffen waren vor allem Typauflösungen für `MauiApp`, `IActivationState` sowie direkt benachbarte Startup-/Hosting-/Storage-Typen
  - `MauiProgram.cs` fehlten im aktuellen Stand aktive Referenzen für Hosting-/Storage- und DI-Erweiterungen
  - `App.xaml.cs` fehlte der aktive MAUI-Typbezug für `IActivationState`
  - `MainApplication.cs` fehlte der aktive Hosting-Typbezug für `MauiApp`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/MauiProgram.cs` die fehlenden aktiven `using`-Direktiven für:
    - `Microsoft.Extensions.DependencyInjection`
    - `Microsoft.Maui.Controls.Hosting`
    - `Microsoft.Maui.Hosting`
    - `Microsoft.Maui.Storage`
    ergänzt
  - in `KGV.Maui/App.xaml.cs` die fehlenden aktiven `using`-Direktiven für:
    - `Microsoft.Maui`
    - `System`
    - `System.Threading.Tasks`
    ergänzt
  - in `KGV.Maui/MainApplication.cs` die fehlende aktive `using`-Direktive `Microsoft.Maui.Hosting` ergänzt
  - keine Fachlogik, keine Navigation, keine Shell-/Routing-/CRUD-/Supabase-Pfade verändert
- Technische Verifikation:
  - `get_errors` auf den drei direkt betroffenen Startup-Dateien blieb nach der Korrektur unauffällig
  - die drei Zielpfade sind damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Block weiterhin nicht grün
  - der Gesamtbuild scheitert jetzt blockfremd an weiteren bereits offenen MAUI-Compilefehlern, u. a. in:
    - `KGV.Maui/ShellRouteRegistrar.cs`
    - `KGV.Maui/ViewModels/MemberSearchViewModel.cs`
    - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - `KGV.Maui/Pages/ArbeitseinsaetzeManagementPage.cs`
    - `KGV.Maui/Pages/BekanntmachungenManagementPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
    - `KGV.Maui/Pages/TermineManagementPage.cs`
  - diese blockfremden Fehler wurden in diesem kleinen Startup-Block bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-27 – MAUI-RFID-Flows auf `Scan-first` zurückgezogen, NFC-Einstellungsweg erhalten und blockfremden Buildrest ehrlich dokumentiert

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - WPF-Referenz nur lesend für den Soll-Flow:
    - `KGV.Wpf/Views/AblesenOverviewView.xaml`
    - `KGV.Wpf/Views/ZaehlerwechselScanView.xaml`
    - `KGV.Wpf/Views/RfidScanContextView.xaml`
    - `KGV.Wpf/ViewModels/RfidEinrichtenViewModel.cs`
    - `KGV.Wpf/ViewModels/ZaehlerwechselScanViewModel.cs`
    - `KGV.Wpf/ViewModels/RfidScanContextViewModel.cs`
  - aktive MAUI-RFID-Pfade:
    - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
    - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
    - `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`
    - `KGV.Maui/Services/INfcScanService.cs`
  - gezielte Dateivalidierung der direkt betroffenen RFID-Dateien
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - der aktive MAUI-Einstieg `RfidEinrichtenPage` zeigte fachlich zu früh bereits `Parzelle`, `Medium`, Prüfen und Speichern; der Einstieg war damit nicht mehr scan-first
  - der gemeinsame MAUI-Scanpfad `RfidScanWorkflowPage` zeigte für `Ablesen` und `Zählerwechsel` ebenfalls schon auf dem ersten Screen zusätzliche Folge-/Fallback-Bereiche statt zuerst nur den Scan
  - der gute NFC-aus-Hinweis mit Button in die Systemeinstellungen war im aktuellen MAUI-Pfad bereits vorhanden und durfte nicht verloren gehen
  - direkt benachbart lag in `AblesenOverviewPage.cs` zusätzlich ein gleichartiger MAUI-Compilefehler durch fehlende Usings vor; dieser wurde wegen unmittelbarer Beteiligung am RFID-Einstieg mitgenommen
- Umsetzung bewusst klein und nur im RFID-Pfad:
  - `RfidEinrichtenViewModel` um einen kleinen vorgeschalteten Scan-/Existenzprüfschritt ergänzt:
    - gescannte UID wird jetzt zuerst global über den vorhandenen Produktivpfad `ResolveRfidScanContextAsync(...)` geprüft
    - bekannter Tag => klare Meldung, kein normaler Speichern-Flow
    - unbekannter Tag => erst dann zweiter Schritt mit `Parzelle`, `Medium`, Prüfen und Speichern
    - sichtbare Busy-Texte für `RFID wird geprüft.` und `RFID wird gespeichert.` ergänzt
  - `RfidEinrichtenPage` auf scan-first zurückgezogen:
    - erster Screen zeigt jetzt nur noch den Scanbereich
    - Folgeansicht für Zuordnung wird nur für unbekannte Tags sichtbar
    - vorhandener NFC-Hinweis und der Button `NFC-Einstellungen öffnen` bleiben erhalten
  - `RfidScanWorkflowPage` für `Ablesen` und `Zählerwechsel` so geschärft, dass zunächst nur der Scanbereich sichtbar ist; Kontext-/Entscheidungsbereich erscheint erst nach aufgelöstem Scan
  - in `AblesenOverviewPage.cs` nur die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt, weil die Seite direkt zum betroffenen RFID-Einstieg gehört
  - keine neue RFID-Architektur, keine neue Shell-/Routing-/Supabase-Struktur und keine WPF-Änderung
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen RFID-Dateien blieb nach der Korrektur unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Block nicht grün geblieben
  - der Build scheitert aktuell blockfremd an bereits offenen bzw. neu außerhalb dieses RFID-Blocks sichtbaren MAUI-Compilefehlern in:
    - `KGV.Maui/MauiProgram.cs`
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/MainApplication.cs`
  - dort fehlen im aktuellen Auszug u. a. aktive Typauflösungen für `MauiApp` bzw. `IActivationState`
  - diese blockfremden Fehler wurden in diesem RFID-Block bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-27 – Nächsten blockfremden MAUI-Compileblock für `ArbeitsstundenReviewPage`, `DokumentePage` und `ExportPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs`
  - `KGV.Maui/Pages/DokumentePage.xaml`
  - `KGV.Maui/Pages/DokumentePage.xaml.cs`
  - `KGV.Maui/Pages/ExportPage.cs`
  - gezielte Dateifehler für genau diese drei MAUI-Pfade
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - `ArbeitsstundenReviewPage.cs` und `ExportPage.cs` liegen im aktiven Stand als code-only MAUI-Seiten vor
  - `DokumentePage.xaml.cs` liegt zwar als `.xaml.cs` vor, die direkt zugehörige `DokumentePage.xaml` ist im aktuellen Stand leer; ein aktiver `x:Class`-/Partial-/Code-Behind-Vertrag war dort somit nicht der Fehlerursprung
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/ApplicationModel-/System-Namespaces
  - betroffen waren u. a. Typauflösungen für `ContentPage`, `CollectionView`, `Label`, `Button`, `Border`, `ScrollView`, `VerticalStackLayout`, `SelectionChangedEventArgs`, `IValueConverter`, `LineBreakMode`, `Colors`, `Thickness`, `FileSystem`, `ShareFileRequest` und `ShareFile`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-/Collections-/Linq-/Tasks-Usings ergänzt
  - in `KGV.Maui/Pages/DokumentePage.xaml.cs` die fehlenden aktiven MAUI-/ApplicationModel-/Graphics-/System-/Linq-/Tasks-Usings ergänzt
  - in `KGV.Maui/Pages/ExportPage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/DataTransfer-/Storage-/Graphics-/System-Usings ergänzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geändert
  - keine XAML-Struktur erfunden oder umgebaut, weil `DokumentePage.xaml` im aktuellen Stand keinen aktiven XAML-Klassenvertrag trägt
- Technische Verifikation:
  - `get_errors` auf den drei Zielpfaden blieb nach dem Fix unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` läuft im aktuellen Workspace wieder erfolgreich durch
  - der gezielte MAUI-Compileblock ist damit dateibezogen und im Gesamtprojekt compile-seitig bereinigt

## 2026-03-26 – MAUI-Laufzeit-/Fachblock für Home-/Detail-/Editorpfade gezielt nach grünem Build geprüft und minimal gehärtet

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - `KGV.Maui/Pages/HomeManagementPage.cs`
  - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
  - `KGV.Maui/Pages/BekanntmachungEditorPage.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
  - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - gezielte Suchläufe auf `entryId=0`, `id = 0`, Delete-Pfade, Busy-Texte, `.Result` und `.Wait(`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die aktiven Admin-/Vorstand-Aktionen `Neu`, `Bearbeiten`, `Löschen` lagen im aktuellen MAUI-Stand bereits auf derselben Detailseite `HomeSectionDetailPage`; es war dafür keine neue Admin-Sonderseite nötig
  - der Startseiten-Schnellzugriff `Arbeitsstunde erfassen` ist im aktiven `HomePage`-/`HomeViewModel`-Pfad bereits vorhanden und weiter funktionsfähig angebunden
  - die mobilen Editorpfade für `Bekanntmachungen`, `Termine`, `Arbeitseinsätze` und auch `Arbeitsstunden` setzten beim Erzeugen der Persistenzobjekte lokal noch explizit `Id = 0`, obwohl die produktiven Create-Pfade im Service Insert ohne DB-ID fahren sollen
  - `DeleteBekanntmachungAsync(...)`, `DeleteTerminAsync(...)` und `DeleteArbeitseinsatzAsync(...)` existieren im Shared-Service bereits; im aktiven Detailpfad fehlte dort aber noch robuste Fehlerbehandlung bei Laufzeitfehlern
  - sichtbare Busy-Texte waren im betroffenen MAUI-Pfad nur teilweise durchgezogen; insbesondere `BekanntmachungEditorPage` und `TermineEditorPage` setzten vor Save noch keinen klaren Speicherkontext für den Nutzer
  - der direkte Suchlauf auf `.Result` und `.Wait(` über die direkt betroffenen MAUI-Dateien blieb leer
  - ein geforderter CPU-Profiler-Lauf für den Trägheitspfad ließ sich im aktuellen Visual-Studio-Setup trotz zweitem Versuch nicht starten, weil die referenzierte `.diagsession` nicht gefunden wurde; der Performance-Teil war damit messtechnisch blockiert
- Umsetzung bewusst klein und nur auf die direkt betroffenen Laufzeit-/Fachursachen begrenzt:
  - in `KGV.Maui/Pages/BekanntmachungEditorPage.cs` den Save-Pfad sichtbar auf `Daten werden gespeichert.` gezogen, per `Task.Yield()` kurz an den Renderzyklus zurückgegeben und die blinde Initialisierung `Id = 0` entfernt; eine `Id` wird jetzt nur noch im echten Bearbeiten-Modus gesetzt
  - in `KGV.Maui/Pages/TermineEditorPage.cs` dieselbe Korrektur für Busy-Text, kurzes Render-Yield und saubere Unterscheidung `Neu` vs. `Bearbeiten` ohne explizites `Id = 0`
  - in `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs` die Record-Erzeugung ebenfalls von der expliziten `Id = 0`-Initialisierung gelöst; eine `Id` wird nur noch gesetzt, wenn tatsächlich ein bestehender Datensatz bearbeitet wird
  - in `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs` denselben Save-Pfad klein nachgezogen: kurzes `Task.Yield()` vor dem produktiven Save und keine explizite `Id = 0`-Zuweisung mehr bei Neuanlage
  - in `KGV.Maui/Pages/HomeSectionDetailPage.cs` die produktiven Aktionen `Anmelden`, `Abmelden` und `Löschen` robuster gemacht:
    - Busy-Farbe für `Daten werden gespeichert.` / `Datensatz wird gelöscht.` sichtbar auf den aktiven Ladezustand gezogen
    - zusätzliche Fehlerbehandlung ergänzt, damit Laufzeitfehler im Detailpfad nicht still oder unkontrolliert durchlaufen
    - Delete bleibt auf demselben bestehenden Produktivpfad mit Bestätigungsdialog und anschließendem Home-Reload über `ReloadAsync()`
  - keine neue Seite, keine neue CRUD-Architektur, keine Shell-/Routing-Rückdrehung und keine Änderung an WPF
- Technische Verifikation:
  - `get_errors` auf den direkt geänderten MAUI-Dateien blieb nach der Korrektur unauffällig
  - der gezielte Suchlauf auf `.Result|.Wait(` blieb in den direkt betroffenen MAUI-Pfaden weiter leer
  - der Startseiten-Button `Arbeitsstunde erfassen` ist im aktiven Pfad weiter vorhanden; kein Wiederherstellungsumbau war nötig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Lauf nicht grün geblieben
  - der Gesamtbuild scheitert aktuell blockfremd an bereits vorhandenen bzw. neu außerhalb dieses Blocks offenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs`
    - `KGV.Maui/Pages/DokumentePage.xaml.cs`
    - `KGV.Maui/Pages/ExportPage.cs`
  - diese Fehler wurden in diesem Lauf bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 – Nächsten blockfremden MAUI-Compileblock für `FaelligeZaehlerPage`, `ArbeitsstundenReviewDetailPage` und `ManagementOverviewPageBase` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/FaelligeZaehlerPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs`
  - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
  - gezielte Dateifehler für genau diese drei MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle drei betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-Gegenstücke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen nicht in Fachlogik, sondern erneut in fehlenden MAUI-/Graphics-/System-Namespaces
  - betroffen waren dort u. a. Typauflösungen für `ContentPage`, `CollectionView`, `Label`, `Button`, `ScrollView`, `VerticalStackLayout`, `HorizontalStackLayout`, `Border`, `Editor`, `View`, `Colors`, `LineBreakMode`, `TextAlignment`, `GridLength`, `CornerRadius`, `Dispatcher` und `FontAttributes`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/FaelligeZaehlerPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt
  - in `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-/Tasks-Usings ergänzt
  - in `KGV.Maui/Pages/ManagementOverviewPageBase.cs` die fehlenden aktiven MAUI-/Graphics-/Collections-/Linq-/System-/Tasks-Usings ergänzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geändert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die drei Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf den drei Zielseiten blieb nach dem Fix unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` läuft im aktuellen Workspace wieder erfolgreich durch
  - der gezielte MAUI-Compileblock ist damit dateibezogen und im Gesamtprojekt compile-seitig bereinigt

## 2026-03-26 – Nächsten blockfremden MAUI-Compileblock für `NebenmitgliedPage`, `UserManagementPage`, `RfidEinrichtenPage`, `MemberGardensPage` und `RfidScanWorkflowPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/NebenmitgliedPage.cs`
  - `KGV.Maui/Pages/UserManagementPage.cs`
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
  - `KGV.Maui/Pages/MemberGardensPage.cs`
  - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - gezielte Dateifehler für genau diese fünf MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle fünf betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-Gegenstücke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/System-Namespaces
  - betroffen waren dort u. a. Typauflösungen für `ContentPage`, `View`, `Label`, `Button`, `Entry`, `Border`, `CollectionView`, `Picker`, `Grid`, `IValueConverter`, `Keyboard`, `Thickness`, `CornerRadius`, `LineBreakMode`, `TextAlignment`, `GridLength` und `Shell`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/NebenmitgliedPage.cs` die fehlenden aktiven MAUI-/System-Usings ergänzt
  - in `KGV.Maui/Pages/UserManagementPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt
  - in `KGV.Maui/Pages/RfidEinrichtenPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt
  - in `KGV.Maui/Pages/MemberGardensPage.cs` die fehlenden aktiven MAUI-/Graphics-/Collections-/System-Usings ergänzt
  - in `KGV.Maui/Pages/RfidScanWorkflowPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geändert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die fünf Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf den fünf Zielseiten blieb nach dem Fix unauffällig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grün
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/FaelligeZaehlerPage.cs`
    - `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs`
    - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 – Nächsten blockfremden MAUI-Compileblock für `MyProfilePage`, `HomeSectionDetailPage` und `ParzellenPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/MyProfilePage.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - gezielte Dateifehler für genau diese drei MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle drei betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-Gegenstücke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/System-/ApplicationModel-Namespaces
  - betroffen waren dort u. a. Typauflösungen für `ContentPage`, `View`, `Border`, `Label`, `Button`, `Entry`, `Grid`, `CollectionView`, `VerticalStackLayout`, `HorizontalStackLayout`, `IValueConverter`, `Launcher`, `Colors`, `Thickness`, `Keyboard`, `LineBreakMode`, `LayoutOptions` und `TextAlignment`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/MyProfilePage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/System-Usings ergänzt
  - in `KGV.Maui/Pages/HomeSectionDetailPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergänzt
  - in `KGV.Maui/Pages/ParzellenPage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/System-Usings ergänzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geändert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die drei Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf `KGV.Maui/Pages/MyProfilePage.cs`, `KGV.Maui/Pages/HomeSectionDetailPage.cs` und `KGV.Maui/Pages/ParzellenPage.cs` blieb nach dem Fix unauffällig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grün
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/NebenmitgliedPage.cs`
    - `KGV.Maui/Pages/UserManagementPage.cs`
    - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
    - `KGV.Maui/Pages/MemberGardensPage.cs`
    - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 – MAUI-Compileblock für `HomeManagementPage` und `MeineDatenPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/HomeManagementPage.cs`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - gezielte Dateifehler für beide MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - beide betroffenen Dateien liegen im aktuellen Stand als code-only MAUI-Seiten vor; aktive `.xaml`-Gegenstücke zu `HomeManagementPage` bzw. `MeineDatenPage` wurden im direkten Pfad nicht gefunden
  - die Compilefehler in beiden Dateien entstanden nicht aus Fachlogik, sondern aus fehlenden/falschen MAUI-Namespacebezügen
  - konkret fehlten die aktiven Typauflösungen u. a. für `ContentPage`, `IQueryAttributable`, `View`, `Border`, `VerticalStackLayout`, `Picker`, `Button`, `Label`, `CheckBox`, `Switch`, `Thickness`, `Keyboard`, `LineBreakMode` und `TextAlignment`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/HomeManagementPage.cs` die fehlenden MAUI-/System-Usings ergänzt
  - in `KGV.Maui/Pages/MeineDatenPage.xaml.cs` die fehlenden MAUI-/System-Usings ergänzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-Pfade verändert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil beide betroffenen Seiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs` blieb nach dem Fix unauffällig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grün
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/MyProfilePage.cs`
    - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
    - `KGV.Maui/Pages/ParzellenPage.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 – MAUI-Arbeitseinsatz-/Busy-Block dokumentarisch sauber abgeschlossen, nur Blockdateien für Commit eingegrenzt

- Vor dem Abschlusslauf erneut nur den realen Git-/Log-/Blockstand geprüft:
  - Git-Status über den im aktuellen Visual-Studio-Setup vorhandenen Git-Pfad `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund im Abschlusslauf:
  - im aktuellen Arbeitsbaum liegen zusätzlich viele blockfremde offene Dateien, u. a. WPF-, Supabase- und Artefaktpfade
  - im direkt betroffenen Blockpfad selbst war kein neuer Fachumbau mehr nötig; der bereits umgesetzte Fixstand ist konsistent
  - `CreateArbeitseinsatzAsync(...)` setzt für neue Arbeitseinsätze `CreatedAt` und `UpdatedAt` explizit, `UpdateArbeitseinsatzAsync(...)` zieht `UpdatedAt` beim Speichern nach
  - die Busy-/Ladehinweise im direkt betroffenen MAUI-Pfad werden früh sichtbar gesetzt und die direkt betroffenen Buttons/Eingaben während Laden/Speichern deaktiviert
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` ist im aktuellen aktiven `HomePage`-Pfad weiterhin vorhanden; dafür war kein weiterer Umbau nötig
- In diesem Abschlusslauf bewusst nur dokumentiert und nicht neu umgebaut:
  - keine neue Architektur
  - keine weitere UI-/Fachlogik außerhalb der bereits angepassten Blockdateien
  - insbesondere `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und `KGV.Maui/Pages/HomeManagementPage.cs` nur als blockfremde Buildursachen benannt
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen Blockdateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der aktuelle Lauf scheitert weiterhin blockfremd breit, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese Fehler wurden im Abschlusslauf ausdrücklich nur dokumentiert und nicht repariert
- Git-/Commit-Einordnung dieses Abschlusslaufs:
  - gestaged werden bewusst nur die echten Blockdateien dieses Arbeitseinsatz-/Busy-Abschlusses
  - blockfremde offene WPF-/Supabase-/Artefaktdateien bleiben ausdrücklich außerhalb des Commits

## 2026-03-26 – MAUI-Arbeitseinsatz-Create repariert, Busy-State im direkten Produktivpfad geschärft und Home-Schnellzugriff geprüft

- Vor dem kleinen Bugfix-Block erneut nur den realen MAUI-/Core-/Infrastructure-Stand geprüft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Core/Models/ArbeitseinsatzRecord.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - ergänzend den genauen Suchlauf auf `.Result` und `.Wait(` in den direkt betroffenen MAUI-Dateien
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor Umsetzung:
  - der Fehler beim Neuanlegen eines Arbeitseinsatzes war im Produktivpfad klar im Service lokalisierbar
  - `CreateArbeitseinsatzAsync(...)` baute beim Insert zwar einen neuen `ArbeitseinsatzRecord`, setzte aber `created_at` und `updated_at` nicht
  - dadurch lief der Insert weiter in den DB-Constraint `23502` auf `arbeitseinsatz.created_at`
  - im MAUI-Pfad lagen keine direkten `.Result`-/`.Wait()`-Blockierer in den betroffenen Dateien vor, aber Busy-Texte konnten im direkten Home-/Detail-/Editorpfad zu spät sichtbar werden
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` ist im aktuellen aktiven `HomePage`-Pfad vorhanden; ein fachlicher Wiederherstellungsumbau war dafür nicht nötig
- Umsetzung bewusst klein und nur in den direkt betroffenen Pfaden:
  - `CreateArbeitseinsatzAsync(...)` setzt beim Insert jetzt `CreatedAt` und `UpdatedAt` explizit auf `DateTime.UtcNow`
  - `UpdateArbeitseinsatzAsync(...)` setzt beim Speichern zusätzlich `UpdatedAt` auf `DateTime.UtcNow`
  - der Rückladepfad nach dem Insert wurde analog zum vorherigen Termin-Fix auf `Titel` + `Datum` eingegrenzt statt die komplette Tabelle unfiltriert neu zu laden
  - `ArbeitseinsaetzeEditorPage` zeigt Busy-Texte für Laden/Speichern jetzt zuverlässig im selben Produktivpfad an, deaktiviert die Eingaben währenddessen und gibt nach `Speichern` sauber zurück in die bestehende Arbeitseinsatz-Übersicht `management_workassignments`
  - `HomePage` setzt den Ladehinweis vor dem Reload sichtbar, deaktiviert die direkt betroffenen Aktionsbuttons währenddessen und fängt Fehler im Home-Reloadpfad sauber sichtbar ab
  - `HomeSectionDetailPage` gibt dem Renderzyklus bei Laden/Speichern/Löschen im direkt betroffenen Detailpfad kurz Raum, damit Busy-/Ladetexte vor den Serviceaufrufen sichtbar werden
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` wurde geprüft und ist im aktuellen aktiven Home-Pfad weiterhin vorhanden
- Technische Verifikation:
  - der exakte Suchlauf auf `.Result|.Wait(` in den direkt betroffenen MAUI-Dateien blieb leer
  - `get_errors` auf den direkt betroffenen Dateien blieb vor dem Abschlusslauf unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheitert weiter blockfremd breit an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese bestehenden Buildfehler wurden in diesem kleinen Arbeitseinsatz-/Busy-Block bewusst nicht repariert

## 2026-03-26 – MAUI-Home-/Detail-/Editor-Block sauber abgeschlossen, Logs nachgezogen und Buildstand ehrlich dokumentiert

- Vor dem Abschlusslauf erneut nur den realen MAUI-/Log-/Git-Stand geprüft:
  - Git-Status über den installierten Git-Pfad `C:\Program Files\Git\cmd\git.exe`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - die bereits in diesem Block angefassten MAUI-Dateien
- WPF wurde in diesem Abschlusslauf bewusst nicht angefasst.
- Ehrlicher Befund vor dem Logabschluss:
  - der Arbeitsbaum war vor der Logpflege git-seitig sauber; offen war in diesem Lauf vor allem der dokumentarische Abschluss des bereits umgesetzten MAUI-Blocks
  - es wurde kein neuer Fachumbau mehr gestartet
  - der Block selbst betrifft die bereits angepassten MAUI-/Shared-Dateien:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Core/Models/HomeAnnouncementItem.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Maui/ViewModels/HomeViewModel.cs`
    - `KGV.Maui/Pages/HomePage.xaml.cs`
    - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
    - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
    - `KGV.Maui/Pages/TermineEditorPage.cs`
    - `KGV.Maui/Pages/BekanntmachungEditorPage.cs`
    - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
    - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
    - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
- Inhalt des bereits umgesetzten Blocks, jetzt nur noch sauber festgehalten:
  - der frühere Neu-Sentinel `entryId=0` wurde in den betroffenen Editorpfaden bereinigt; neue Datensätze laufen jetzt über `null`/kein `entryId` statt über eine künstliche `0`
  - Admin/Vorstand erhalten auf denselben mobilen Produktivseiten zusätzliche Aktionen `Neu`, `Bearbeiten`, `Löschen` statt eines neuen Sonderpfads
  - Delete-Pfade für `Termine`, `Arbeitseinsätze` und `Bekanntmachungen` wurden produktiv an `ISupabaseService` / `SupabaseService` angebunden
  - Busy-/Lade-/Speichertexte wurden im direkt betroffenen Pfad ergänzt, insbesondere auf:
    - `HomePage`
    - `MyArbeitsstundenPage`
    - `ArbeitsstundenEditorPage`
    - `TermineEditorPage`
    - `BekanntmachungEditorPage`
    - `ArbeitseinsaetzeEditorPage`
    - `HomeSectionDetailPage`
    - `ManagementOverviewPageBase`
  - betroffene Seiten planen Ladevorgänge jetzt nicht mehr sofort blockierend im `OnAppearing`, sondern mit kleinen `_loadScheduled`-/Busy-Guards nach dem ersten Renderzyklus
  - im direkt betroffenen Pfad wurden keine `.Result`-/`.Wait()`-Nutzungen mehr gefunden
- Technische Verifikation im Abschlusslauf:
  - erneuter `git status --short` über den echten Git-Pfad war vor der Logpflege sauber
  - erneuter Suchlauf auf `entryId=0|.Result|.Wait(` in den betroffenen MAUI-Pfaden blieb leer
  - `get_errors` auf den direkt geänderten Blockdateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grün
  - der aktuelle Build scheitert weiterhin blockfremd breit an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese blockfremden Fehler wurden im Abschlusslauf bewusst nur dokumentiert und nicht mehr umgebaut

## 2026-03-26 – MAUI-Terminanlage-Rückladepfad nach Insert stabilisiert

- Vor dem kleinen Block erneut nur den realen MAUI-/Service-Iststand für den gemeldeten Terminfehler geprüft:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - aktuelles Debug-Output
- Ehrlicher Befund:
  - der Insert selbst lief durch
  - im Debug-Log stand aber weiterhin `CreateTerminAsync created termin (null)`
  - Ursache war damit nicht mehr der DB-Constraint, sondern der Rückladepfad nach dem Insert
  - `CreateTerminAsync` lud nach dem Speichern die komplette Terminliste unfiltriert neu und suchte den gerade erzeugten Datensatz danach per In-Memory-Match wieder heraus
  - das war unnötig fragil
- Umsetzung bewusst klein:
  - den Reload nach `Insert(...)` auf `Titel` + `Datum` eingegrenzt
  - danach weiter den bestpassenden Datensatz per bestehendem Match + höchste `Id` gewählt
- Technische Verifikation:
  - `get_errors` auf `KGV.Infrastructure/Services/SupabaseService.cs` blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Block ohne neue dateibezogene Fehler an

## 2026-03-26 – MAUI-Terminanlage repariert und Launcher-Icon wieder auf PNG festgezogen

- Vor dem kleinen Block erneut nur den realen MAUI-Iststand für den gemeldeten Fehler geprüft:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/TerminRecord.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- Ehrlicher Befund:
  - der Fehler beim Anlegen eines neuen Termins war im Log bereits klar sichtbar:
    - `CreateTerminAsync failed`
    - DB-Fehler `null value in column "created_at" of relation "termin" violates not-null constraint`
  - `TerminRecord` enthält `CreatedAt` und `UpdatedAt`
  - `CreateTerminAsync` setzte diese Felder beim Insert aber nicht
  - der aktive Launcher-Icon-Pfad stand nach dem letzten Block wieder auf `Resources/AppIcon/appicon.svg`
  - genau das passte zum gemeldeten Befund, dass das Starticon trotz Löschen von `bin`/`obj` und Neuinstallation weiterhin nicht stimmt
- Umsetzung bewusst klein und nur in den betroffenen Produktivpfaden:
  - `CreateTerminAsync` setzt beim Insert jetzt `CreatedAt` und `UpdatedAt` auf `DateTime.UtcNow`
  - `UpdateTerminAsync` setzt `UpdatedAt` beim Speichern ebenfalls auf `DateTime.UtcNow`
  - `KGV.Maui.csproj` zieht den aktiven `MauiIcon`-Pfad wieder auf das vorhandene PNG zurück:
    - `Resources/AppIcon/appicon.png`
  - die Splash-Konfiguration bleibt weiter auf der SVG
- Technische Verifikation:
  - `get_errors` auf den geänderten Dateien blieb unauffällig
  - `get_tests` für `Project=KGV.Maui` lieferte weiterhin keine passenden Tests
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Typ-/Namespacefehlern, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Navigations-ANR entschärft, Home-Ladepfad entkoppelt und Launcher-Icon auf SVG vereinheitlicht

- Vor dem kleinen Block erneut nur den realen MAUI-Iststand, die aktiven Navigationsseiten und die Android-/Icon-Dateien geprüft:
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- Ehrlicher Befund:
  - CPU-Profiling ließ sich im aktuellen Setup nicht direkt starten
  - die Debug-/Laufzeitlogs zeigten den Hänger trotzdem klar:
    - sehr viele `Skipped frames`
    - `Davey! duration=22560ms`
    - Home-Ladevorgänge parallel zu Navigations-/UI-Phasen
    - zusätzliche Supabase-/DNS-Fehler im Home-Pfad (`Unable to resolve host ...supabase.co`)
  - in den aktiven Seiten wurde Laden weiterhin direkt im `OnAppearing` bzw. beim Navigieren gestartet
  - `HomeViewModel.InitializeAsync()` lud den Home-Überblick bei gleichem Kontext immer wieder neu
  - `GetHomeOverviewAsync(...)` arbeitete die Home-Sektionen seriell ab, wodurch Netzwerkprobleme die Wartezeit unnötig verlängerten
  - Android meldete zusätzlich wiederholt `OnBackInvokedCallback is not enabled`
  - das Launcher-Icon lief zuletzt über einen PNG-Pfad, während der Splash parallel weiter die SVG verwendete
- Umsetzung bewusst klein und nur im aktiven MAUI-/Home-/Android-Pfad:
  - `HomePage`, `MyArbeitsstundenPage` und `ArbeitsstundenEditorPage` stoßen ihr Laden jetzt erst per `Dispatcher` nach dem ersten Render-Zyklus an statt direkt im Navigationsfenster zu awaiten
  - doppelte Ladeplanung auf denselben Seiten über kleine `_loadScheduled`-/`_isLoading`-Guards abgefangen
  - Listen-Navigation in `HomePage` und `MyArbeitsstundenPage` gegen Reentrancy/Doppeltaps abgesichert
  - `HomeViewModel.InitializeAsync()` cached den zuletzt geladenen Kontext und lädt bei unverändertem Rolle-/Mitgliedskontext nicht erneut
  - `KGV.Infrastructure/Services/SupabaseService.cs` lädt die Home-Teilbereiche jetzt parallel statt seriell
  - `AndroidManifest.xml` ergänzt um `android:enableOnBackInvokedCallback="true"`
  - `KGV.Maui.csproj` auf eine einheitliche SVG-Quelle für `MauiIcon` und `MauiSplashScreen` umgestellt
- Technische Verifikation:
  - `get_errors` auf den geänderten Dateien blieb unauffällig
  - `get_tests` für `Project=KGV.Maui` lieferte keine passenden Tests
  - `dotnet build KGV.Maui/KGV.Maui.csproj` weiterhin nicht erfolgreich
  - der erste Lauf scheiterte blockfremd u. a. an vorhandenen `InitializeComponent`-Fehlern in `KGV.Maui/App.xaml.cs` und `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - der zweite Lauf scheiterte weiterhin breit blockfremd an bereits vorhandenen MAUI-Typ-/Namespacefehlern, u. a. in `KGV.Maui/Pages/BekanntmachungEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Loginlogo auf korrektes KGV-Bild umgestellt, Launcher-Icon unverändert gelassen

- Vor dem kleinen Block erneut nur den MAUI-Logo-/Bildpfad und gezielt diese Dateien geprüft:
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Resources/Images/*`
  - `KGV.Maui/Resources/AppIcon/*`
- Ehrlicher Befund:
  - die Loginseite lud aktuell weiterhin `kgv_logo.svg`
  - damit wurde in der UI das falsche grüne Personen-/Splash-Logo angezeigt statt des eigentlichen KGV-Bilds
  - das Launcher-Icon war nicht das Problem; der aktive `MauiIcon`-Pfad blieb korrekt auf `Resources/AppIcon/appicon.png`
  - ein normales UI-PNG unter `Resources/Images` fehlte bislang
- Umsetzung bewusst klein und nur fürs Login-/UI-Logo:
  - aus `Resources/AppIcon/appicon.png` ein normales UI-Bild unter `KGV.Maui/Resources/Images/kgv_logo.png` bereitgestellt
  - `KGV.Maui.csproj` erweitert, damit `Resources/Images/*.png` als `MauiImage` eingebunden werden
  - das alte falsche `Resources/Images/kgv_logo.svg` entfernt, damit kein doppelter Ausgabename `kgv_logo` mehr entsteht
  - `LoginPage` vom falschen `kgv_logo.svg` auf `kgv_logo.png` umgestellt
  - Launcher-Icon-/Manifest-/Android-Iconlogik bewusst nicht verändert
- Technische Verifikation:
  - `get_errors` auf den geänderten Dateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - ein direkt benachbarter Resizetizer-Duplikatfehler um `kgv_logo` wurde in diesem Block noch mit bereinigt
  - der Lauf scheiterte danach weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/BekanntmachungEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs`, `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und `KGV.Maui/Pages/TermineEditorPage.cs`

## 2026-03-26 – MAUI-Arbeitsstundenpfad nach Flyout-Umbau stabilisiert

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den Fortschrittslog und gezielt diese aktiven Dateien geprüft:
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - `KGV.Maui/ShellNavigationHelper.cs`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/UserShell.cs`
  - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
- Ehrlicher Befund:
  - `workhours` war nach dem Flyout-Umbau nur noch im `UserShell` ein echter sichtbarer Shell-Root
  - im `AdminShell` lag der Arbeitsstunden-Unterpunkt dagegen auf `member_workhours`
  - genau deshalb war `//workhours` auf der Editorseite nach `Abbrechen` bzw. `Speichern` für Admin/Vorstand kein gültiger universeller Rückpfad mehr
  - zusätzlich war `MyArbeitsstundenPage` selbst noch nicht als Detailroute registriert, also gab es keinen sauberen Fallback außerhalb sichtbarer Shell-Roots
  - der Home-Schnellzugriff für `Arbeitsstunde erfassen` war für Admin/Vorstand noch an den eigenen Nutzerkontext statt an den ausgewählten Mitgliedskontext gebunden
- Umsetzung bewusst klein und nur in MAUI:
  - `MyArbeitsstundenPage` als vorhandene Detailroute registriert
  - `ShellNavigationHelper` um eine kleine Prüfung erweitert, ob eine sichtbare Shell-Content-Route aktuell vorhanden ist
  - `ArbeitsstundenEditorPage` verwendet für `Abbrechen` und Save-Erfolg jetzt einen kontextabhängigen gültigen Rückpfad:
    - `//member_workhours`, wenn der Admin-/Mitgliedskontextpfad sichtbar ist
    - `//workhours`, wenn der User-Root sichtbar ist
    - sonst Fallback auf die registrierte Detailroute `MyArbeitsstundenPage`
  - `HomeViewModel.CanCreateWorkHoursEntry` für Admin/Vorstand an den vorhandenen ausgewählten `MemberContextState` gekoppelt; User bleiben auf dem eigenen Mitgliedskontext
- Ergebnis für den Pfad:
  - kein universeller ungültiger `//workhours`-Rücksprung mehr
  - Liste und Editor bleiben auf vorhandenen Produktivseiten
  - `Abbrechen` und Save-Erfolg führen auf einen gültigen mobilen Arbeitsstundenpfad zurück
  - Admin/Vorstand laufen über den ausgewählten Mitgliedskontext, User über den eigenen
- Technische Verifikation:
  - `get_errors` auf den geänderten Arbeitsstunden-Dateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Android-AppIcon final auf genau einen aktiven PNG-Pfad festgezogen

- Vor dem kleinen Block erneut nur den MAUI-Icon-Pfad und gezielt diese Dateien geprüft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Resources/AppIcon/*`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV.Maui/Platforms/Android/Resources/*`
- Ehrlicher Befund:
  - im `csproj` existierte bereits genau ein expliziter `MauiIcon`-Eintrag auf `Resources/AppIcon/appicon.png`
  - parallel lag aber weiterhin `Resources/AppIcon/appicon.svg` im selben Ordner und wurde nicht nur für Splash genutzt, sondern zusätzlich noch als normales `MauiImage` unter `Resources/Images/kgv_logo.svg` verlinkt; der Launcher-Override war damit zwar nicht direkt aktiv, der Altpfad blieb aber unnötig doppelt verdrahtet
  - `AndroidManifest.xml` enthält weiterhin weder `android:icon` noch `android:roundIcon`
  - unter `Platforms/Android/Resources/*` existieren im Workspace keine manuellen `mipmap`-/`drawable`-Icon-Reste, die das MAUI-AppIcon überschreiben
- Umsetzung bewusst klein und nur fürs AppIcon:
  - in `KGV.Maui.csproj` zusätzlich explizit `MauiIcon Remove="Resources\AppIcon\*.svg"` gesetzt
  - die zusätzliche `MauiImage`-Verlinkung der `AppIcon`-SVG wurde entfernt
  - stattdessen liegt das allgemeine Bild-Asset jetzt separat unter `KGV.Maui/Resources/Images/kgv_logo.svg`
  - damit bleibt für den Launcher-Pfad genau ein aktives `MauiIcon` übrig:
    - `Resources/AppIcon/appicon.png`
  - Splash-/Fach-/Login-/Shell-Logik bewusst nicht verändert
- Android-Test-/Bereinigungshinweis:
  - `bin` und `obj` löschen
  - App auf dem Gerät deinstallieren
  - neu bauen und neu installieren, weil Android Launcher-Icons cached
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - ein direkt benachbarter Android-AAPT-Fehler aus der doppelten `appicon.svg`-Nutzung als `MauiImage` wurde in diesem Block mit bereinigt
  - der Lauf scheiterte danach weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Login-Root-Switch auf Android stabilisiert und AppIcon-Pfad bereinigt

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den Fortschrittslog und gezielt diese aktiven Dateien geprüft:
  - `KGV.Maui/App.xaml.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/UserShell.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV.Maui/Resources/AppIcon/*`
- Ehrlicher Befund:
  - `LoginPage`, `AdminShell` und `UserShell` waren in `MauiProgram` weiterhin bereits sinnvoll `transient` registriert; ein Lifetime-Problem war dort nicht die Ursache
  - der verzögerte Android-Seitenwechsel entstand im vorhandenen Root-Switch-Pfad dadurch, dass `App.SwitchToCurrentRootAsync()` auf Android ein neues `Window` öffnete und das alte danach schloss
  - dadurch blieb der sichtbare Wechsel instabil, obwohl Login und Shell-Aufbau bereits erfolgreich waren
  - zusätzlich existierte in `LoginPage` noch ein zweiter Fallback-Pfad, der bei Bedarf selbst `AdminShell`/`UserShell` erzeugte und direkt auf `window.Page` setzte
  - beim AppIcon war der aktive `MauiIcon`-Pfad bereits auf `Resources/AppIcon/appicon.png` gesetzt, aber lokal lag die tatsächliche PNG-Datei noch nur im Arbeitsbaum; außerdem nutzte der Splash zusätzlich dieselbe `AppIcon`-SVG, was den aktiven Pfad unnötig unklar machte
  - `AndroidManifest.xml` überschreibt das App-Symbol nicht
- Umsetzung bewusst klein und nur in MAUI:
  - `App.SwitchToCurrentRootAsync()` baut den Ziel-Root jetzt auf dem `MainThread` und setzt ihn direkt auf das aktive vorhandene Fenster
  - der Android-spezifische Ersatzfenster-/`OpenWindow`-/`CloseWindow`-Pfad wurde entfernt
  - `LoginPage` verwendet für den erfolgreichen Loginabschluss jetzt nur noch den einen zentralen App-Root-Wechselpfad statt zusätzlich Shells lokal als Fallback zu setzen
  - `KGV.Maui.csproj` bleibt beim aktiven AppIcon-Pfad `Resources/AppIcon/appicon.png`
  - der Splash bleibt bewusst auf dem vorhandenen buildbaren `Resources/AppIcon/appicon.svg`, weil die vorhandene `Resources/Splash/splash.svg` in diesem Workspace aktuell keinen gültigen Android-`drawable/splash` erzeugte
  - das tatsächlich referenzierte Logo-Asset `KGV.Maui/Resources/AppIcon/appicon.png` wird mit in den Commit genommen
- Android-Test-/Bereinigungshinweis:
  - für einen sauberen Icon-Test `bin`/`obj` neu erzeugen
  - App auf dem Gerät deinstallieren
  - APK/App neu installieren, weil Android Launcher-Icons aggressiv cached
- Technische Verifikation:
  - `get_errors` auf den geänderten Root-Switch-Dateien blieb unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Shell-Route `management_workassignments` nach Flyout-Umbau repariert

- Vor dem kleinen Bugfix-Block erneut den realen MAUI-Iststand, die Shell-/Route-Dateien und die betroffenen `GoToAsync(...)`-Aufrufer geprüft.
- Ehrlicher Befund:
  - `management_workassignments` war nach dem Flyout-Umbau kein Shell-Root-Ziel mehr, wurde aber weiter als absolutes `//management_workassignments` angesprungen
  - in `ShellRouteRegistrar` war kein expliziter Alias `management_workassignments` registriert; vorhanden war nur `nameof(ArbeitseinsaetzeManagementPage)`
  - direkt benachbart lag dasselbe Muster auch bei `management_announcements` und `management_appointments` vor
- Umsetzung bewusst klein und nur in MAUI:
  - in `ShellRouteRegistrar` Alias-Routen für `management_announcements`, `management_appointments` und `management_workassignments` registriert
  - absolute Aufrufe auf nicht mehr rootfähige Ziele auf relative Navigation umgestellt:
    - `HomePage`
    - `HomeSectionDetailPage`
    - `HomeManagementPage`
    - `ArbeitseinsaetzeEditorPage`
  - echte Root-Ziele mit `//` bewusst nicht verändert
- Technische Verifikation:
  - `get_errors` auf den geänderten Dateien blieb für diesen Block unauffällig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiterhin blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Flyout auf kleine Hauptnavigation reduziert und Mitgliedskontext sauber eingehängt

- Vor dem kleinen Block erneut Repo-/Arbeitsbaumstand, aktiven Fortschrittslog und die real verwendeten MAUI-Shell-/Mitgliedskontextdateien geprüft.
- Ehrlicher Befund:
  - `AdminShell` und `UserShell` enthielten noch deutlich mehr Flyout-Einträge als für den mobilen Hauptweg gewünscht
  - die gewünschten Mitglieds-Unterpunkte waren im Admin-Flyout bisher immer sichtbar statt erst nach Mitgliedsauswahl
  - `MemberSearchPage` setzte den Mitgliedskontext bereits, aktualisierte das Flyout danach aber noch nicht gezielt
  - der vorhandene mobile Arbeitsstundenpfad war bereits vorhanden, nutzte im Adminkontext aber noch nur den globalen eigenen Mitgliedsbezug statt den ausgewählten Mitgliedskontext
- Umsetzung bewusst klein und nur in MAUI:
  - `AdminShell` auf die gewünschten Hauptpunkte reduziert:
    - `Startseite`
    - `Ablesen`
    - `Parzellenverwaltung`
    - `Arbeitsstunden freigeben`
    - `Export`
    - `Mitgliedersuche`
  - direkte Verwaltungs-/Profil-/Mehrfach-Flyoutpunkte entfernt
  - Mitglieds-Unterpunkte im Flyout jetzt optisch eingerückt mit `↳`
  - Admin/Vorstand sehen diese Unterpunkte nur noch bei vorhandenem `SelectedMember`
  - `UserShell` zeigt für den sofort aktiven Eigenkontext direkt die eingerückten Unterpunkte:
    - `↳ Stammdaten`
    - `↳ Nebenmitglied`
    - `↳ Gärten des Mitglieds`
    - `↳ Arbeitsstunden`
  - `MemberSearchPage` baut das aktuelle Shell-Menü nach Mitgliedsauswahl sofort neu auf, bevor in `Stammdaten` navigiert wird
  - `MyArbeitsstundenPage` und `ArbeitsstundenEditorPage` berücksichtigen im Admin-/Vorstandskontext jetzt den ausgewählten `MemberContextState`, damit der Unterpunkt `↳ Arbeitsstunden` auf dem vorhandenen mobilen Produktivpfad für das ausgewählte Mitglied arbeitet
  - `ShellNavigationHelper` minimal um den expliziten MAUI-Shell-Namespace geschärft
- Technische Verifikation:
  - `get_tests` für `KGV.Tests` ergab weiterhin keine passenden aktiven Tests für diesen kleinen MAUI-Navigationsblock
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte blockfremd weiterhin an bereits bestehenden breiten MAUI-Control-/Namespacefehlern u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 – MAUI-Login-UI korrigiert und Arbeitsstunden-Schnellzugriff auf Home ergänzt

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den aktuellen Fortschrittslog und die aktiven MAUI-Dateien für `LoginPage`, `HomePage`, `MyArbeitsstundenPage`, `ArbeitsstundenEditorPage`, `ShellRouteRegistrar`, `AdminShell`, `UserShell` und `HomeViewModel` geprüft.
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `KGV.Maui/Pages/LoginPage.xaml.cs` ist der aktive Loginpfad und baute die komplette UI derzeit rein in C# auf
  - die Passwortsichtbarkeit lief dort noch über einen separaten Button `Passwort anzeigen` unterhalb des Passwortfelds
  - der Button `Anmelden` stand im aktuellen Mobilfluss noch hinter den sekundären Aktionen statt direkt unter dem Passwortfeld
  - der vorhandene Erfassungspfad für Arbeitsstunden war bereits belastbar vorhanden über `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs` mit Navigation `ArbeitsstundenEditorPage?entryId=0`
  - auf `KGV.Maui/Pages/HomePage.xaml.cs` zeigte der Bereich `Arbeitsstunden` bisher nur Überblick/Hinweise, aber noch keinen direkten Erfassungsbutton
- Den Korrekturblock deshalb bewusst klein und nur im aktiven MAUI-Pfad umgesetzt:
  - auf `LoginPage` das Passwortfeld auf einen eingebetteten Auge-/Auge-zu-Toggle umgestellt statt eigenem Sichtbarkeitsbutton unter dem Feld
  - `Anmelden` direkt unter das Passwortfeld gezogen
  - die sekundären Aktionen `Einladung / Erstlogin-Code anfordern` und `Passwort vergessen` unterhalb von `Anmelden` angeordnet
  - die bestehende Login-/OTP-/Passwort-Logik fachlich unverändert gelassen
  - auf `HomePage` im Bereich `Arbeitsstunden` einen klaren Button `Arbeitsstunde erfassen` ergänzt
  - der neue Home-Schnellzugriff nutzt keinen neuen Dummy-Pfad, sondern den bereits vorhandenen Editorpfad `ArbeitsstundenEditorPage?entryId=0`
  - die Sichtbarkeit des Schnellzugriffs an den vorhandenen eigenen Mitgliedskontext gekoppelt über `HomeViewModel`, damit kein unnötiger toter Pfad für Kontexte ohne `MitgliedId` angeboten wird
- Technische Verifikation:
  - `get_tests` ergab im aktuellen Workspace keine passenden aktiven Tests für `KGV.Tests`
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace nicht erfolgreich
  - der Lauf scheiterte weiterhin an breit vorhandenen MAUI-Typ-/Namespacefehlern im aktuellen Stand, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und im selben Lauf auch an generischen Control-Typen in `KGV.Maui/Pages/HomePage.xaml.cs`; der Block ist damit aktuell nicht sauber buildisoliert validierbar
  - `git` war in der aktuellen Terminalumgebung nicht verfügbar (`GIT_NOT_FOUND`), deshalb konnte der geforderte Commit-/Push-Abschluss in diesem Lauf nicht technisch ausgeführt werden

## 2026-03-25 – MAUI-Launcher-Icon auf vorhandenes Vereinslogo-PNG umgestellt

- Vor dem kleinen Block erneut Repo-/Arbeitsbaumstand, Fortschrittslog und aktiven MAUI-AppIcon-Pfad geprüft.
- Ehrlicher Befund:
  - `AndroidManifest.xml` überschreibt das Launcher-Icon nicht
  - `KGV.Maui.csproj` nutzte noch `appicon.svg` als `MauiIcon`
  - das vorhandene `KGV.Maui/Resources/AppIcon/appicon.png` lag bereits im aktiven Repo
- Umsetzung bewusst klein gehalten:
  - nur den aktiven `MauiIcon`-Pfad auf `appicon.png` umgestellt
  - Splash, Shell und übrige MAUI-Pfade unverändert gelassen

## 2026-03-25 – Finaler Git-Abschluss: Artefakt-/FotoUploadTest-Block sauber auf echte Blockdateien eingegrenzt

- Vor dem Git-Abschluss erneut den realen Repo-/Arbeitsbaumstand, den Fortschrittslog und die aktiven Referenzen des WPF-`FotoUploadTest`-Pfads geprüft.
- Ehrlicher Befund:
  - weiterhin viele blockfremde offene Dateien im Arbeitsbaum
  - aktiver WPF-`FotoUploadTest`-Pfad klar belegt
  - MAUI-Variante weiterhin Archivmaterial
  - `.github/copilot-instructions.md` nicht blockzugehörig genug für diesen Commit
- Für den Abschluss nur diese Dateiklassen in den Commit genommen:
  - aktive WPF-/Core-/Infrastructure-`FotoUploadTest`-Dateien
  - notwendige kleine MAUI-Restbereinigung in `MauiProgram`
  - aktive Supabase-Begleitdateien
  - echte Archivdateien des MAUI-/Altpfads
  - beide Logdateien
- Ausdrücklich nicht aufgenommen:
  - blockfremde WPF-XAML-/SQL-Änderungen
  - `.vscode/`
  - `_AI_DB_EXPORT/`
  - `_secrets/`
  - lokale Installer/APKs/Asset-Dubletten und sonstige Exporte

## 2026-03-25 – Block Artefaktbereinigung: aktive `FotoUploadTest`-/Supabase-Dateien übernommen, lokale Restartefakte getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und die unversionierten/blockfremden Artefakte geprüft.
- Ehrlicher Befund im aktuellen Repo-Stand:
  - das WPF-`FotoUploadTest` war bereits aktiv verdrahtet, die zugehörigen Dateien lagen aber noch unversioniert lokal vor
  - die MAUI-Variante war aktuell nicht produktiv angebunden und damit kein aktiver Hauptpfad
  - zusätzliche Supabase-Begleitdateien für `kgv-upload-photo` waren legitim, aber noch unversioniert
  - weitere lokale Artefakte wie Binaries, Secrets, Exporte, Dubletten und Tooldateien waren nicht belastbar Teil der aktiven Repo-Struktur
- Den Korrekturblock deshalb bewusst klein und sauber getrennt umgesetzt:
  - aktive WPF-`FotoUploadTest`-Dateien und ihre Core-/Infrastructure-Verdrahtung ordentlich ins Repo übernommen
  - lose MAUI-Registrierung des ungenutzten `FotoUploadTestViewModel` entfernt
  - MAUI-`FotoUploadTest`-Dateien in `_Archiv/KGV.Maui/...` verschoben
  - alten Nebenpfad `supabase/kgv-upload-photo/index.ts` nach `_Archiv/supabase/...` verschoben
  - `supabase/.gitignore`, `.npmrc` und `deno.json` des aktiven Functionpfads sauber ins Repo übernommen
  - verbleibende lokale Archivkandidaten in `_Archiv/LOCAL_ARTIFACT_ARCHIVE_CANDIDATES.md` zusammengefasst
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-25 – Block 5B-final: Abschlussprüfung, Restentkopplung und Repo-Bereinigung für den aktuellen MAUI/WPF-Zyklus abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, `README.md`, `KGV.slnx`, `KGV.Tests` und `_Archiv/KGV.Tests` geprüft.
- Für Block `5B-final` gezielt geprüft:
  - WPF-Hauptpfad `HomeView`
  - aktive MAUI-Hauptpfade für Home, Detail, Verwaltung, Arbeitsstunden und Shell
  - Restreferenzen auf `HomeManagementPage`
  - Testprojekt-Einordnung im Root und im Archiv
- Ehrlicher Befund im aktuellen Repo-Stand:
  - die großen MAUI-Fachblöcke waren bereits fachlich getrennt und buildfähig abgeschlossen
  - letzter echter aktiver Altübergang über `HomeManagementPage` lag noch in `HomeSectionDetailPage` für `Termine` und `Bekanntmachungen`
  - `ManagementOverviewPageBase` trug zusätzlich noch einen ungenutzten Standard-Fallback auf `HomeManagementPage`
  - `KGV.Tests` im Root und `_Archiv/KGV.Tests` waren dokumentarisch noch nicht sauber genug gegeneinander eingeordnet
- Den Abschlussblock deshalb bewusst klein nur auf belegbare Restpunkte begrenzt:
  - `HomeSectionDetailPage` navigiert bei `Termine` jetzt direkt auf `//management_appointments`
  - `HomeSectionDetailPage` navigiert bei `Bekanntmachungen` jetzt direkt auf `//management_announcements`
  - `ManagementOverviewPageBase` wurde vom alten impliziten `HomeManagementPage`-Fallback entkoppelt und verlangt jetzt explizite Produktivpfade der abgeleiteten Seiten
  - `README.md` ordnet `KGV.Tests` im Root jetzt klar als vorhandenes, aber nicht in `KGV.slnx`/CI eingebundenes Testprojekt ein; `_Archiv/KGV.Tests` bleibt die archivierte ältere Kopie
  - `HomeManagementPage` bleibt bewusst als technischer Kompatibilitätspfad im Repo und wird nicht auf Verdacht gelöscht
- Abschlussbild nach dem Block:
  - `Home` bleibt Übersicht
  - `Verwaltung` bleibt getrennt
  - `Editor` bleibt `Editor`
  - `Detail` bleibt `Detail`
  - keine unnötigen aktiven Mischpfade mehr im MAUI-Hauptweg
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-25 – Block 5A-final: Home-Verwaltungszugänge in MAUI direkt auf Produktivseiten gezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block `5A-final` gezielt geprüft:
  - `HomePage`
  - `HomeManagementPage`
  - vorhandene Managementseiten und Shell-Routen für `Arbeitseinsätze`, `Termine` und `Bekanntmachungen`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die echten Produktivseiten waren bereits vorhanden
  - `Arbeitseinsätze` gingen von `Home` schon direkt auf die Managementseite
  - `Termine` und `Bekanntmachungen` liefen von `Home` aber noch unnötig über `HomeManagementPage`
- Den Korrekturblock deshalb bewusst klein und nur in der Navigation umgesetzt:
  - `Termine bearbeiten` auf `Home` zieht jetzt direkt auf `//management_appointments`
  - `Bekanntmachungen bearbeiten` auf `Home` zieht jetzt direkt auf `//management_announcements`
  - `HomeManagementPage` bleibt nur technischer Rest-/Kompatibilitätspfad und wird nicht weiter als normaler Home-Hauptweg genutzt
  - Sichtbarkeit für Admin/Vorstand blieb unverändert
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt Desktop-Views nutzt MAUI direkte Shell-Routen auf die vorhandenen Mobilseiten bei gleicher fachlicher Trennung
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 – Block 4B2-finish: Termine-Nutzerpfad in MAUI sichtbar mobil abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block `4B2-finish` gezielt geprüft:
  - `HomeSectionDetailPage`
  - `HomePage`
  - `TermineUserState`
  - `HomeContextState`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `4B1` hatte den datensatzweisen Termine-Zustand schon vorbereitet
  - sichtbar war die Footer-Navigation bislang aber noch nur für Arbeitseinsätze aktiv
  - der Terminpfad brauchte noch den sichtbaren mobilen Abschluss `← x/y →`
- Den Korrekturblock deshalb bewusst klein nur im sichtbaren Termine-Detailpfad umgesetzt:
  - `TermineUserState` um reine Navigationshilfen ergänzt
  - `HomeSectionDetailPage` zeigt jetzt für Termine unten `← x/y →`
  - Blättern läuft direkt über `TermineUserState`
  - Termin-Detail bleibt rein lesend; kein `Bearbeiten` im Nutzerpfad
  - Fallback auf Einzeltermin aus `HomeContextState` bleibt robust erhalten
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt Desktop-Klickpfad mit Listenfokus nutzt MAUI mobil bewusst einen datensatzweisen Footer
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 – Block 4B1: Termine-Nutzerpfad in MAUI auf datensatzweisen Zustand vorbereitet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block `4B1` gezielt geprüft:
  - `HomePage`
  - `HomeSectionDetailPage`
  - `HomeContextState`
  - `HomeViewModel`
  - `SupabaseService`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - Termine liefen bereits korrekt über `HomePage` in `HomeSectionDetailPage`
  - der Klickpfad übergab aber noch nur den Einzeltermin statt einer datensatzweisen Terminbasis
  - ein eigener kleiner Termine-Nutzerzustand fehlte noch
- Den Korrekturblock deshalb bewusst klein und nur als Grundlage umgesetzt:
  - neuer `TermineUserState` für Terminliste plus aktuelle Position
  - `HomePage` seedet beim Termin-Klick jetzt die gesamte chronologische Terminliste plus aktuelle Auswahl
  - `HomeSectionDetailPage` nutzt für Termine jetzt bevorzugt den aktuellen Eintrag aus diesem Zustand
  - Shared-Sortierung für Home-Termine auf `Datum`, `Startzeit`, `Endzeit`, `Titel`, `Id` abgesichert
  - keine sichtbare Pfeilnavigation in diesem Block vorgezogen
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - `4B1` bereitet nur den datensatzweisen Mobilfluss vor; die sichtbare UI-Schärfung bleibt bewusst `4B2`
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 – Block 4A-small: Admin-Menü und Mein Profil in MAUI-Shell ruhiger geordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block `4A-small` gezielt geprüft:
  - `AdminShell`
  - `UserShell`
  - aktuelle Sichtbarkeit und Einordnung von `Benutzerverwaltung` und `Mein Profil`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `Benutzerverwaltung` hing im Admin-Flyout noch fachlich unter `Mitglieder`
  - `Mein Profil` war im Admin-Flyout noch nicht so ruhig als eigener persönlicher Bereich eingeordnet wie im User-Flyout
- Den Korrekturblock deshalb bewusst klein nur in der Shell-Struktur umgesetzt:
  - `Benutzerverwaltung` in `AdminShell` auf `Admin-Menü · Benutzerverwaltung` gezogen
  - Sichtbarkeit unverändert nur für `Admin`
  - `Mein Profil` in `AdminShell` auf `Mein Bereich · Mein Profil` gezogen
  - keine neuen Seiten, keine neuen Routen und keine neue Rollenlogik eingeführt
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt Desktop-Untermenüs bleibt MAUI bei einer flachen Flyout-Struktur mit fachlichen Präfixen
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 – Block 3B3-finish: Arbeitseinsätze in MAUI auf Überblick, Editor und Datensatzfluss fachlich abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `3B3-finish` gezielt geprüft:
  - MAUI: `ArbeitseinsaetzeManagementPage`, `ArbeitseinsaetzeEditorPage`, `HomeManagementPage`, `HomePage`, `HomeSectionDetailPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
  - Shared/Produktivpfade: `SupabaseService`, `HomeDashboardItems`, `StartseiteArbeitseinsatzRecord`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die Übersicht war noch nicht konsequent chronologisch und ruhig genug
  - der Arbeitseinsatz-Editorpfad war noch nicht vollständig produktiv verdrahtet
  - Verwaltungs- und User-Datensatzfluss mit Pfeilnavigation fehlten noch
  - `HomeManagementPage` war für Arbeitseinsätze noch Rest-Hauptpfad
- Den Korrekturblock deshalb bewusst klein und nur auf `Arbeitseinsätze` umgesetzt:
  - chronologische Verwaltungssortierung nach `Datum`, `Startzeit`, `Endzeit`, `Titel`
  - eigener produktiver Editorpfad `ArbeitseinsaetzeEditorPage` für `Neu` und `Bearbeiten`
  - mobiler Verwaltungs-Datensatzfluss im Editor mit `← x/y →` und Save-vor-Wechsel
  - mobiler User-Datensatzfluss in `HomeSectionDetailPage` mit `← x/y →` sowie `Anmelden` / `Abmelden` je Datensatz
  - `HomeManagementPage` für Arbeitseinsätze auf Übersicht/Editor umgeleitet
  - vorhandene RPC-/Shared-Pfade für `Anmelden` und `Abmelden` weiterverwendet
  - `ShellRouteRegistrar` und `MauiProgram` minimal ergänzt
  - neue kleine Zustandsdienste für Verwaltungs- und Usernavigation ergänzt
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt Desktop-Nebeneinander von Liste und Editor nutzt MAUI mobil getrennte Seiten plus kompakte Datensatznavigation
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - blockeigene Warnungen bereinigt; verbleibend nur 4 ältere Warnungen in `HomeManagementPage.cs`

## 2026-03-25 – Block 3B2: Termine in MAUI auf Überblick plus eigenen Editorpfad getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `3B2` gezielt geprüft:
  - WPF: `TermineVerwaltungEditorView`
  - MAUI: `TermineManagementPage`, `HomeManagementPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `TermineManagementPage` war zwar bereits ruhige Übersicht, öffnete `Neu`/`Bearbeiten` aber noch in den technischen Restpfad `HomeManagementPage`
  - damit blieb für Termine der eigentliche mobile Produktiv-Editor weiter an einer Mischseite hängen
  - zusätzlich lief die Übersicht noch nicht ausdrücklich auf der geforderten chronologischen Hauptsortierung nach Datum/Uhrzeit
- Den Korrekturblock deshalb bewusst klein und nur auf `Termine` umgesetzt:
  - neuer eigener Editorpfad `TermineEditorPage` ergänzt
  - `TermineManagementPage` öffnet `Neu` und `Bearbeiten` jetzt direkt in diesen neuen Editorpfad
  - die Übersicht wurde ausdrücklich auf chronologische Reihenfolge nach `Datum`, `Startzeit`, `Endzeit`, `Titel` gezogen
  - der neue Editor trennt Übersicht und Bearbeitung sichtbar auf zwei Seiten
  - fachlich relevante Felder aus dem bestehenden Shared-/WPF-Pfad bleiben im Editor erhalten:
    - `Titel`
    - `Beschreibung`
    - `Datum`
    - `Startzeit`
    - `Endzeit`
    - `Sichtbar ab`
    - `Sichtbar bis`
    - `Aktiv`
  - Speichern bleibt am Ende des Formulars
  - `HomeManagementPage` wurde für den Bereich `Termine` als Haupteditor entkoppelt und leitet jetzt bei diesem Bereich auf Übersicht bzw. neuen Editorpfad weiter
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt WPF-Liste plus Editor in derselben Breite nutzt MAUI mobil bewusst getrennte Seiten `Übersicht` und `Editor`
  - für die touch-taugliche Zeit-/Datumsbearbeitung nutzt der mobile Editor stabile `DatePicker`-/`TimePicker`-Felder mit klaren Aktivschaltern für optionale Zeit- und Sichtbarkeitsangaben statt einer Desktop-artigen freien Zeiteingabe in derselben Formularfläche
- Bewusst nicht gemacht:
  - keine Änderungen an `Bekanntmachungen`
  - keine Änderungen an `Arbeitseinsätze`
  - keine Änderungen an Home, Stammdaten, Gärten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` für `KGV.Tests` ergab keine passenden Testfälle für diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus dem neuen Termine-Editorpfad

## 2026-03-25 – Block 3B1: Bekanntmachungen in MAUI auf Überblick plus eigenen Editorpfad getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `3B1` gezielt geprüft:
  - WPF: `BekanntmachungenVerwaltungEditorView`
  - MAUI: `BekanntmachungenManagementPage`, `ManagementOverviewPageBase`, `HomeManagementPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `BekanntmachungenManagementPage` war zwar bereits ruhige Übersicht, öffnete `Neu`/`Bearbeiten` aber noch in den technischen Restpfad `HomeManagementPage`
  - damit blieb für Bekanntmachungen der eigentliche mobile Produktiv-Editor weiter an einer Mischseite hängen
  - die bestehende HTML-Bearbeitung war mobil noch nicht in einen eigenen getrennten Editorpfad gezogen
- Den Korrekturblock deshalb bewusst klein und nur auf `Bekanntmachungen` umgesetzt:
  - neuer eigener Editorpfad `BekanntmachungEditorPage` ergänzt
  - `BekanntmachungenManagementPage` öffnet `Neu` und `Bearbeiten` jetzt direkt in diesen neuen Editorpfad
  - `ManagementOverviewPageBase` dafür minimal von technischem Fortsetzungspfad auf überschreibbare Öffnen-/Neu-Aktionen erweitert
  - der neue Editor trennt Übersicht und Bearbeitung sichtbar auf zwei Seiten
  - HTML-Bearbeitung bleibt fachlich erhalten über echten HTML-Quelltexteditor, Snippet-Buttons und mobile Vorschau per `WebView`
  - fachlich relevante Felder aus dem bestehenden Shared-/WPF-Pfad bleiben im Editor erhalten:
    - `Titel`
    - `InhaltHtml`
    - `Sichtbar ab`
    - `Sichtbar bis`
    - `SortOrder`
    - `Aktiv`
  - Speichern bleibt am Ende des Formulars
  - `HomeManagementPage` wurde für den Bereich `Bekanntmachungen` als Haupteditor entkoppelt und leitet jetzt bei diesem Bereich auf Übersicht bzw. neuen Editorpfad weiter
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt WPF-Liste plus Editor in derselben Breite nutzt MAUI mobil bewusst getrennte Seiten `Übersicht` und `Editor`
  - die HTML-Bearbeitung bleibt dabei als Quelltext + Snippets + Vorschau erhalten, weil dies mobil belastbarer und risikoärmer ist als ein neuer Richtext-Sonderbau
- Bewusst nicht gemacht:
  - keine Änderungen an `Termine`
  - keine Änderungen an `Arbeitseinsätze`
  - keine Änderungen an Home, Stammdaten, Gärten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` für `KGV.Tests` ergab keine passenden Testfälle für diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` sowie bestehenden blockfremden Pfaden und stammen nicht aus dem neuen Bekanntmachungen-Editorpfad

## 2026-03-25 – Block 3A: Verwaltungsbereiche in MAUI fachlich auf drei eigene Übersichtsseiten entflochten

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `3A` gezielt geprüft:
  - WPF: `BekanntmachungenVerwaltungEditorView`, `TermineVerwaltungEditorView`, `ArbeitseinsaetzeVerwaltungEditorView`
  - MAUI: `HomeManagementPage`, `AdminShell`, `ShellRouteRegistrar`, `MauiProgram`, bestehende Home-/Verwaltungszugänge
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die drei Bereiche `Bekanntmachungen`, `Termine` und `Arbeitseinsätze` liefen mobil weiterhin primär über die Sammelseite `HomeManagementPage`
  - fachliche Bereichstrennung und direkte Verwaltungs-Einstiege waren gegenüber WPF damit noch zu schwach
  - `HomeManagementPage` blieb damit faktisch noch die Produktiv-Sammelseite statt nur technischer Restpfad
- Den Korrekturblock deshalb bewusst klein und nur auf Bereichsstruktur/Übersicht umgesetzt:
  - neue ruhige MAUI-Übersichtsseiten ergänzt:
    - `BekanntmachungenManagementPage`
    - `TermineManagementPage`
    - `ArbeitseinsaetzeManagementPage`
  - gemeinsame kleine Basis `ManagementOverviewPageBase` ergänzt, damit alle drei Seiten nur ihren jeweils eigenen Bereich zeigen
  - jede Seite zeigt nur:
    - eigene Liste vorhandener Datensätze
    - klaren Einstieg `Neu`
    - keinen Bereichswechsel innerhalb derselben Seite
  - `AdminShell` zeigt die drei Verwaltungsbereiche jetzt direkt auf diese drei neuen Hauptseiten statt auf die Sammelseite
  - `ShellRouteRegistrar` und `MauiProgram` minimal für die drei neuen Seiten ergänzt
  - `HomeManagementPage` nicht riskant gelöscht, sondern entkoppelt:
    - neuer technischer Restpfad für Fortsetzung innerhalb genau eines Bereichs
    - kann jetzt optional mit fixiertem Bereich, ausgewähltem Datensatz oder `Neu` geöffnet werden
    - Bereichsauswahl wird in diesem technischen Fortsetzungspfad bei fixierter Herkunft ausgeblendet
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt WPF-Editor+Liste pro Bereich ist mobil in `3A` zunächst nur die ruhige Bereichsübersicht als eigener Hauptweg nachgezogen
  - der technische Fortsetzungspfad über `HomeManagementPage` bleibt bis `3B` bewusst bestehen, um keinen riskanten Großumbau der Editorlogik vorzuziehen
- Bewusst nicht gemacht:
  - keine eigentlichen Editorseiten pro Datensatz in diesem Block
  - keine HTML-/Zeit-/Validierungs-Feinschliffe
  - keine Änderungen an Home, Stammdaten, Gärten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` für `KGV.Tests` ergab keine passenden Testfälle für diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden und stammen nicht aus der neuen Bereichstrennung selbst

## 2026-03-25 – Block 2B: Arbeitsstunden-Freigabe in MAUI auf ruhige Übersicht und Einzeldatensatz-Prüfung umgestellt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `2B` gezielt geprüft:
  - WPF: `ArbeitsstundenPruefungView`, ergänzend `ArbeitsstundenView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `ArbeitsstundenReviewPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `ArbeitsstundenReviewPage` zeigte offene Prüffälle noch mit direkter `Genehmigen`-/`Ablehnen`-Aktion direkt in der Listenansicht
  - damit blieb mobil weiter eine Mischseite aus Übersicht und Entscheidung statt eines klaren Prüfpfads pro Datensatz
  - eine WPF-nähere mobile Datensatznavigation für den Prüfkontext fehlte noch vollständig
- Den Korrekturblock deshalb bewusst klein und nur im Reviewpfad umgesetzt:
  - `ArbeitsstundenReviewPage` auf ruhige Übersicht offener Prüffälle zurückgezogen
  - Antippen eines offenen Falls öffnet jetzt eine eigene mobile Prüfdetailseite
  - neue Seite `ArbeitsstundenReviewDetailPage` ergänzt
  - neuer Zustand `ArbeitsstundenReviewState` hält die aktuelle offene Prüffallliste und die mobile Datensatzposition
  - Prüfdetail zeigt genau einen Datensatz mit Mitglied, Datum, Stunden, Art der Arbeit und Status-/Anmerkungsfeld
  - unten mobile Datensatznavigation nur mit Pfeil links, Positionsanzeige `x/y` mittig und Pfeil rechts
  - bei offenen Änderungen wird vor dem Blättern zuerst gespeichert; bei Fehler bleibt die Seite stehen
  - `Freigeben`, `Ablehnen` und `Speichern` laufen weiter über dieselben bestehenden Shared-Servicepfade
  - entschiedene/freigegebene bzw. abgelehnte Fälle werden nicht mehr als normaler offener Prüffall behandelt
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt einer WPF-Prüftabelle nutzt MAUI mobil bewusst Übersicht plus Einzeldatensatz-Prüfung auf eigener Seite
  - diese Abweichung ist fachlich gewollt, weil sie auf kleineren Bildschirmen den Prüfkontext klarer und touch-tauglicher hält
- Bewusst nicht gemacht:
  - keine Änderung an `MyArbeitsstundenPage`
  - keine Änderung am normalen Nutzer-Erfassungs-/Bearbeitungspfad
  - keine Änderung an Home, Stammdaten, Gärten, Parzellen, Ablesen, Verwaltung, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` für `KGV.Tests` ergab keine passenden Testfälle für diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 – Block 2A: Arbeitsstunden-Nutzerpfad in MAUI auf Übersicht / Erfassen / Bearbeiten getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprüft.
- Für Block `2A` gezielt geprüft:
  - WPF: `ArbeitsstundenView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `MyArbeitsstundenPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `MyArbeitsstundenPage` mischte Übersicht und Editor noch auf einer Sammelseite
  - Antippen eines Eintrags füllte dasselbe Formular unten wieder auf
  - bestätigte/freigegebene Einträge waren im Nutzerpfad noch nicht klar genug als gesperrte Nur-Ansicht getrennt
- Den Korrekturblock deshalb bewusst klein und nur im normalen Nutzerpfad umgesetzt:
  - `MyArbeitsstundenPage` auf ruhige Übersicht zurückgezogen
  - `Soll`, `Geleistet`, `Offen` bleiben oben sichtbar
  - klarer Einstieg `Neu erfassen`
  - vorhandene Einträge öffnen jetzt einen eigenen mobilen Arbeitsstunden-Pfad statt das Übersichtsformular wiederzuverwenden
  - neue Seite `ArbeitsstundenEditorPage` ergänzt
  - dieselbe Seite trägt mobil die drei fachlichen Rollen:
    - `Arbeitsstunde erfassen`
    - `Arbeitsstunde bearbeiten`
    - `Arbeitsstunde ansehen`
  - unbestätigte Einträge öffnen bearbeitbar
  - freigegebene Einträge öffnen nur noch lesbar mit klarem Sperrhinweis
  - `Speichern` und `Abbrechen` bleiben am Ende des Eingabeformulars
  - nach `Speichern` oder `Abbrechen` geht der Nutzer klar zurück zur Übersicht `Meine Arbeitsstunden`
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt WPF-Dialog/Fenster verwendet MAUI eine eigene mobile Seite für Erfassen/Bearbeiten/Ansicht
  - diese Abweichung ist fachlich gewollt, weil sie auf kleineren Bildschirmen Übersicht und Editor sauber trennt
- Bewusst nicht gemacht:
  - keine Änderung an `ArbeitsstundenReviewPage`
  - keine Änderung an Freigabe-/Prüflogik für Admin/Vorstand
  - keine Änderung an Home, Stammdaten, Gärten, Parzellen, Ablesen, Verwaltung, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` für `KGV.Tests` ergab keine passenden Testfälle für diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 – Block 1B2: NFC-Hauptweg in MAUI technisch abgeschlossen und validiert

- Vor dem Abschlussblock erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block `1B2` gezielt den bereits begonnenen NFC-/Workflow-Stand geprüft:
  - `AblesenOverviewPage`
  - `RfidScanWorkflowPage`
  - `AblesungErfassenPage`
  - `ZaehlerwechselPage`
  - `RfidEinrichtenPage`
  - `RfidScanContextViewModel`
  - Android-NFC-Pfad in `Platforms/Android`
- Ehrlicher Befund im begonnenen Stand:
  - der fachliche NFC-Block war bereits angebaut
  - der technische Abschluss fehlte aber noch
  - insbesondere waren Build, Logpflege, Commit und Push noch offen
- Den Abschluss deshalb strikt auf den bestehenden `1B2`-Block begrenzt:
  - MAUI-Build für `KGV.Maui/KGV.Maui.csproj` ausgeführt
  - nur blockbezogene Buildfehler im neuen NFC-/Workflow-Pfad korrigiert
  - Android-Namespace-/Context-/Settings-Konflikte im NFC-Service bereinigt
  - nicht verfügbare `Expander`-Verwendung in den MAUI-Seiten durch einfache ruhige Sektionen ersetzt
  - neue blockeigene Nullability-Warnung im RFID-Workflow entfernt
- Fachlicher Endstand von `1B2` nach dem Abschluss:
  - NFC ist im mobilen operativen RFID-/Ablesen-Bereich jetzt der Hauptweg
  - gelesene UID wird weiter in die bestehenden echten Produktivpfade eingespeist
  - `Parzelle + Medium` steht als fachlicher Ersatzweg bereit, wenn NFC nicht verfügbar oder deaktiviert ist
  - manuelle UID-Eingabe bleibt nur noch als technischer Notfallweg sichtbar
- Erlaubte mobile Abweichung ausdrücklich festgehalten:
  - Geräte ohne NFC bzw. mit deaktiviertem NFC erhalten mobil den fachlichen Ersatzweg über `Parzelle + Medium`
  - bei `RFID einrichten` bleibt UID-Tippen nur als technischer Notfallweg bestehen, weil ohne bekannte Tag-UID keine echte RFID-Zuordnung gespeichert werden kann
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 – Block 1B1: Parzellen-Details in MAUI auf ruhige Leseseite zurückgezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprüft.
- Für Block 1B1 gezielt geprüft:
  - MAUI: `ParzellenPage`, `ParzellenViewModel`
  - WPF-Referenz: `GartenStromView`, `GartenWasserView`, fachlich ergänzend `AblesenOverviewView`
- Ehrlicher Befund im aktuellen Stand:
  - `ParzellenPage` enthielt zwar bereits einen ruhigeren Bereich `Aktuelle Ablesedaten`, stellte diesen aber noch kartenartig dar
  - zusätzlich lag dort mit `Zum Ablesen-Bereich` noch eine direkte operative Aktion mitten im Detailblock
  - im mitgliedsgebundenen Detailkontext wirkte die Seite damit noch zu sehr wie eine Sammelarbeitsseite statt wie eine ruhige Leseseite
- Den Korrekturblock bewusst klein und nur im Detailpfad umgesetzt:
  - `Aktuelle Ablesedaten` sichtbar von kartenartiger Darstellung auf eine nüchterne tabellenähnliche Zeilenstruktur umgebaut
  - Kopfzeile für `Medium`, `Letzter Stand`, `Datum`, `Foto`
  - Zeilen zeigen jetzt nur die ruhigen Kerndaten statt einer dekorativen Kartenoptik
  - der direkte Button `Zum Ablesen-Bereich` aus dem Detailblock entfernt
  - der Hinweis auf den operativen Bereich `Ablesen` bleibt nur noch als ruhiger Einordnungstext bestehen
  - der Verwaltungs-/Zuordnungsblock wird im mitgliedsgebundenen Detailkontext nicht mehr als operative Arbeitsfläche gezeigt; der globale Verwaltungsweg bleibt unverändert erhalten
- Warum dieser kleine Block fachlich sinnvoll ist:
  - er trennt das Lesen/Einordnen im Parzellen-Detail klarer vom operativen Arbeiten im Bereich `Ablesen`
  - die Detailseite wirkt im Mitglieds-/Gartenkontext sichtbar ruhiger
  - bestehende operative Fachpfade wurden nicht neu gebaut oder verlagert, sondern nur sauber aus dem Detailblock herausgezogen
- Bewusst nicht gemacht:
  - kein Umbau von `AblesenOverviewPage`
  - keine Änderung an `RfidEinrichtenPage`, `RfidScanWorkflowPage` oder `ZaehlerwechselPage`
  - keine neue Parzellen-/Garten-Architektur
  - keine Änderung an Stammdaten, Gärten des Mitglieds, Arbeitsstunden, Verwaltung, Export oder Auth
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Parzellen-Details wirken ruhiger
  - aktuelle Ablesedaten sind tabellenähnlich sichtbar
  - direkte operative Ablesen-Aktion ist aus dem Detailblock entfernt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte bestehende Warnungen außerhalb dieses Blocks bleiben bestehen

## 2026-03-25 – Block 1A: Stammdaten und Gärten des Mitglieds in MAUI WPF-nah getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprüft.
- Für Block 1A gezielt geprüft:
  - MAUI: `MeineDatenPage`, `AdminShell`, `UserShell`, `MauiProgram`, `ShellRouteRegistrar`, `MemberContextState`, `ParzellenContextState`
  - WPF-Referenz: `MemberDetailView`, `StammdatenView`, `MemberParzellenSection`
- Ehrlicher Befund im aktuellen Stand:
  - `MeineDatenPage` blieb trotz vorheriger Feinschliffe fachlich noch überladen, weil der Garten-/Parzellenbereich weiterhin direkt auf der Stammdatenseite eingebettet war
  - `Mitgliedsdokumente` waren bereits unten separat platziert, die Seite wirkte durch den eingebetteten Gartenblock aber weiter zu gemischt
  - `Bearbeiten` war vorhanden, die fachliche Trennung `Stammdaten` vs. `Gärten des Mitglieds` blieb mobil aber noch schwächer als in WPF
- Den Korrekturblock bewusst nur auf Stammdaten vs. Gärten begrenzt:
  - keine Parzellen-Detailanpassung
  - keine Ablesen-/NFC-/Zählerwechsel-Änderung
  - keine neue Fachlogik außerhalb des bereits vorhandenen Mitglieds-/Parzellenkontexts
- Auf der Stammdatenseite umgesetzt:
  - den eingebetteten Bereich `Gärten` aus `MeineDatenPage` entfernt
  - `MeineDatenPage` bleibt damit fachlich ruhiger auf Stammdaten-/Mitgliedskontext- und Verwaltungsinformationen fokussiert
  - `Bearbeiten` bleibt als sichtbarer Einstieg erhalten
  - `Mitgliedsdokumente` bleiben als eigener Button ganz unten auf der Seite
- Neuen klaren Mitgliedsbereich `Gärten` ergänzt:
  - neue Seite `MemberGardensPage`
  - zeigt die Garten-/Parzellenzuordnungen des ausgewählten Mitglieds in einer eigenen Seite
  - vorhandener ehrlicher Einstieg `Parzelle zuordnen` bleibt dort erhalten
  - Antippen einer Zuordnung nutzt weiter den bestehenden Mitgliedskontext-/Parzellenpfad statt einer neuen Schattenarchitektur
- Navigation/Shell minimal mitgezogen:
  - `AdminShell`: `Mitglieder · Gärten des Mitglieds` zeigt jetzt die neue `MemberGardensPage`
  - `UserShell` erhielt zusätzlich `Mein Bereich · Gärten`, damit der bisherige eigene Gartenzugang nicht still entfällt
  - `MauiProgram` und `ShellRouteRegistrar` wurden minimal für die neue Seite bzw. den bestehenden Parzellenpfad ergänzt
- Erlaubte mobile Abweichung ausdrücklich benannt:
  - statt eines innerhalb derselben Fläche dauerhaft eingebetteten Desktop-Unterbereichs wurde mobil bewusst eine eigene Seite `Gärten` ergänzt
  - diese Abweichung ist fachlich gewollt und sinnvoll, weil sie Stammdaten ruhiger hält und den Mitgliedsgartenbereich klarer trennt
- Bewusst nicht gemacht:
  - kein Umbau von `ParzellenPage`
  - keine Änderung an Ablesen, NFC, Strom/Wasser oder Zählerwechsel
  - kein neuer mobiler Parzellen-Zuordnungseditor
  - keine Änderung an Export, Profil, Auth oder Arbeitsstunden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - `Stammdaten` ist ruhiger
  - `Gärten des Mitglieds` sind als eigener Bereich getrennt
  - `Mitgliedsdokumente` bleiben korrekt unten als eigener Button
  - `Bearbeiten` bleibt sichtbar
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte bestehende Warnungen außerhalb dieses Blocks bleiben bestehen

## 2026-03-24 – Block 0A: MAUI-Shell-/Menüstruktur WPF-nah neu geordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 0A gezielt geprüft:
  - MAUI: `AdminShell`, `UserShell`, `MauiProgram`, `ShellNavigationHelper`
  - WPF-Referenz: fachliche Haupt-/Mitgliedsnavigation in `MainWindowViewModel`
- Ehrlicher Befund im aktuellen Stand:
  - `AdminShell` und `UserShell` enthielten zwar bereits produktive Kernziele, die fachliche Gruppierung war gegenüber WPF aber noch zu flach und zu unscharf
  - besonders Mitgliedskontext, Parzellenverwaltung, Ablesen, Arbeitsstunden, Verwaltung und Profil lagen mobil noch nicht klar genug in einer WPF-nahen Reihenfolge und Gruppierung
  - `Gärten des Mitglieds` durfte dabei ausdrücklich nicht mit der globalen `Parzellenverwaltung` vermischt werden
- Den Korrekturblock bewusst nur auf die Shell-Struktur begrenzt:
  - keine Inhaltsänderung an Fachseiten
  - keine neue Fachlogik
  - keine Android-/Login-/Root-Baustelle geöffnet
- In `AdminShell` WPF-nahe fachliche Ordnung hergestellt:
  - `Startseite`
  - `Mitglieder · Mitgliedersuche`
  - `Mitglieder · Stammdaten`
  - `Mitglieder · Nebenmitglied`
  - `Mitglieder · Benutzerverwaltung` *(Admin-only)*
  - `Mitglieder · Gärten des Mitglieds`
  - `Parzellenverwaltung`
  - `Ablesen · Ablesen`
  - `Ablesen · RFID einrichten`
  - `Ablesen · Fällige Zähler`
  - `Ablesen · Zählerwechsel`
  - `Arbeitsstunden · Meine Arbeitsstunden`
  - `Arbeitsstunden · Arbeitsstunden freigeben`
  - `Verwaltung · Bekanntmachungen`
  - `Verwaltung · Termine`
  - `Verwaltung · Arbeitseinsätze`
  - `Export`
  - `Mein Profil`
- In `UserShell` die fachliche Struktur auf `Mein Bereich` umgeordnet:
  - `Startseite`
  - `Mein Bereich · Meine Stammdaten`
  - `Mein Bereich · Nebenmitglied`
  - `Mein Bereich · Meine Arbeitsstunden`
  - `Mein Bereich · Mein Profil`
- Kleine Hilfsanpassungen gezielt mitgezogen:
  - für `Meine Stammdaten` im User-Shell wird vor dem Öffnen der vorhandene eigene Mitgliedskontext gesetzt, damit die bestehende `MeineDatenPage` WPF-nah als eigener Stammdatenpfad nutzbar bleibt
  - die drei Verwaltungsunterpunkte verwenden weiter die vorhandene `HomeManagementPage`, aber gezielt mit bestehendem Query-Parameter für Bereichsauswahl statt neuer Seitenarchitektur
  - der Badge-/Titelpfad für `Arbeitsstunden freigeben` wurde an die neue Gruppierungsbenennung angepasst
- Erzwungene kleine mobile Abweichung gegenüber WPF ausdrücklich benannt:
  - echtes verschachteltes Flyout mit visuellen Unterebenen bietet die Standard-MAUI-Shell ohne größeren Custom-Flyout-Umbau nicht belastbar an
  - deshalb wurde die WPF-nahe Unterstruktur im Flyout minimal-invasiv über klare fachliche Gruppierungspräfixe `Bereich · Unterpunkt` umgesetzt
  - das hält die Bereiche getrennt und adressierbar, ohne eine neue Shell-Architektur zu eröffnen
- Bewusst nicht gemacht:
  - kein Umbau von `LoginPage`
  - kein Eingriff in `App.xaml.cs` oder Android-Rootpfade
  - keine Seitenkopf-/Hamburger-/Zurück-Themen dieses Blocks vorweggenommen
  - keine Inhaltsänderung an Home-, Stammdaten-, Parzellen-, Ablesen-, Export- oder Verwaltungsseiten
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Admin-/Vorstand-Menü ist fachlich deutlich WPF-näher gruppiert
  - Nutzer-Menü ist auf `Mein Bereich` gebündelt
  - Mitgliedsbezogene Gärten und globale Parzellenverwaltung bleiben getrennt
  - keine produktiven Fachbereiche entfernt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte bestehende Warnungen außerhalb dieses Blocks bleiben bestehen

## 2026-03-24 – MAUI Feinschliff für Stammdaten / Gärten / Parzelle / Ablesen mit kleinem UI-/Flow-Block fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprüft.
- Für diesen Feinschliff gezielt geprüft:
  - MAUI: `MeineDatenPage`, `MemberSearchPage`, `AdminShell`, `ParzellenPage`, `RfidScanWorkflowPage`, `AblesenOverviewPage`, `ShellRouteRegistrar`
  - WPF-Referenz: `MemberDetailView`, `MemberParzellenSection`, `GartenStromView`, `GartenWasserView`
- Ehrlicher Befund im aktuellen Stand:
  - auf `MeineDatenPage` hing `Mitgliedsdokumente` noch fachlich unruhig im Bereich `Gärten / Parzellen`
  - `Stammdaten aktualisieren` brachte gegenüber dem ohnehin vorhandenen Reload beim Wiedererscheinen nur geringen Mehrwert
  - der fehlende Hamburger auf der mobilen Stammdatenseite lag nicht an einer kaputten Shell, sondern daran, dass der Admin-Mitgliedskontext bislang als gepushte Detailroute aus der Suche geöffnet wurde
  - im Parzellen-Detailbereich waren die mobilen Strom-/Wasser-/Zählerwechsel-Bedienblöcke zu operativ und zogen fachlich wieder in eine Seite hinein, die ruhiger read-only bleiben sollte
  - für NFC/RFID existiert im aktiven MAUI-Stand weiterhin keine direkte Geräteanbindung; die UI wirkte aber noch zu sehr so, als sei manuelle UID-Eingabe der Hauptweg
- Kleinen Korrekturblock deshalb nur in diesen vier Themen umgesetzt:
  - `MeineDatenPage` erhielt einen kleinen `Bearbeiten`-Einstieg
  - für eigene Stammdaten führt er in den vorhandenen mobilen Profilpfad; für fremde Mitglieder wird ehrlich erklärt, dass ein mobiler Volleditor aktuell noch fehlt
  - `Mitgliedsdokumente` wurden aus dem Gartenbereich herausgezogen und als eigener Button an das Seitenende gesetzt
  - `Stammdaten aktualisieren` wurde entfernt
  - der Bereich heißt jetzt nur noch `Gärten`
  - zusätzlicher Button `Parzelle zuordnen` öffnet als ehrlichen bestehenden Einstieg die vorhandene Parzellenverwaltung statt einen Fake-Flow zu bauen
  - `AdminShell` erhielt einen echten Shell-Einstieg `Stammdaten`; `MemberSearchPage` navigiert dorthin jetzt über den Shell-Rootpfad
  - dadurch erscheint im Admin-Stammdatenpfad wieder das Flyout-/Hamburger-Menü statt eines reinen Zurück-Pfeils
  - `ParzellenPage` zeigt im Detailbereich jetzt nur noch einen ruhigen ReadOnly-Block `Aktuelle Ablesedaten`
  - sichtbar bleiben dort Medium, letzter Stand, Datum, Zählernummer und ein Fotobutton, wenn ein Fotopfad vorhanden ist
  - der operative Ablesen-/Zählerwechselpfad wurde bewusst aus dem Detailbereich herausgezogen und über einen klaren Übergang `Zum Ablesen-Bereich` getrennt
  - `AblesenOverviewPage` und `RfidScanWorkflowPage` kommunizieren jetzt ehrlich, dass direkter NFC-/RFID-Gerätescan im aktuellen MAUI-Stand noch nicht aktiv angebunden ist
  - manuelle UID-Eingabe wird sichtbar als `Fallback` gekennzeichnet und nicht mehr wie der primäre Produktpfad dargestellt
- Warum dieser kleine Block fachlich sinnvoll ist:
  - er beruhigt genau die von außen sichtbaren UI-/Flow-Schwächen, ohne neue Architektur zu eröffnen
  - bestehende belastbare Shared-Servicepfade für Parzellen, Dokumente und RFID-Kontext bleiben unverändert die fachliche Grundlage
  - an fehlenden mobilen Volleditoren oder fehlender NFC-Geräteintegration wird nichts beschönigt; stattdessen wird die UI ehrlicher und klarer
- Bewusst nicht gemacht:
  - kein neuer mobiler Volleditor für fremde Stammdaten
  - kein neuer Parzellen-Zuordnungs-Spezialdialog nur für den Mitgliedskontext
  - keine neue NFC-Architektur oder Geräteintegration
  - keine Änderung an WPF-Dateien, Export, Arbeitsstunden, Home-Verwaltung oder Auth-Grundpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Stammdatenseite wirkt ruhiger
  - Mitgliedsdokumente sind separat unten erreichbar
  - Gärten sind sprachlich klarer getrennt
  - Parzellen-Detail zeigt nur noch ReadOnly-Kernkontext für Ablesungen
  - Ablesen-/RFID-Pfad kommuniziert jetzt ehrlich den manuellen Fallback
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte bestehende Warnungen außerhalb dieses Blocks bleiben bestehen

## 2026-03-24 – Systematische View-Prüfung Block 9: Altlasten / MAUI-only-Seiten / Testpfade mit kleinem MAUI-Altpfad-Aufräumblock fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 9 gezielt geprüft:
  - WPF: `FotoUploadTestView` sowie die derzeit sichtbaren Test-/Hilfspfade im Arbeitsbaum
  - MAUI: `RoleChoicePage`, `ExitPage`, `MyProfilePage`, `AblesenFeaturePage`, Shell-Routen, produktive Menüsichtbarkeit und erkennbare Hilfs-/Altseiten
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - fachlich nötig oder nicht
  - produktiv erreichbar oder nicht
  - Altlast oder legitimer MAUI-only-Pfad
  - Risiko beim Entfernen
  - Auswirkungen auf Navigation, Build und Rechte
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `RoleChoicePage` ist im aktiven Repo-Pfad bereits nicht mehr vorhanden und damit keine verbleibende produktive Altlast mehr
  - `MyProfilePage` ist ein legitimer produktiver MAUI-only-Pfad und bleibt sinnvoll im User-Shell-Menü verankert
  - `AblesenFeaturePage` war im aktiven MAUI-Pfad nur noch eine ungenutzte Hilfsseite ohne produktive Route oder Menüeinbindung
  - `ExitPage` war zwar noch produktiv im User-/Admin-Shell-Menü eingehängt, ist mobil aber fachlich kein sinnvoller Produktpfad
  - zusätzliche Foto-/Upload-Testpfade sind im aktuellen Arbeitsbaum sichtbar, aber teilweise lokal/unversioniert; diese wurden für diesen kleinen Block bewusst nicht aggressiv mitbereinigt
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - produktiver Flyout-Eintrag `Beenden` in MAUI war als Altpfad überflüssig
  - `ExitPage` selbst war nach Entfernen dieses Menüs ein toter MAUI-only-Pfad
  - `AblesenFeaturePage` war eine ungenutzte Hilfsseite ohne produktive Einbindung
- Kleinen Korrekturblock deshalb nur im MAUI-Altlasten-/Routing-Rahmen umgesetzt:
  - `Beenden` aus `AdminShell` entfernt
  - `Beenden` aus `UserShell` entfernt
  - `ExitPage` aus dem DI-Container entfernt
  - ungenutzte Datei `ExitPage.cs` gelöscht
  - ungenutzte Datei `AblesenFeaturePage.cs` gelöscht
- Warum dieser kleine Block fachlich sinnvoll ist:
  - der Block entfernt klare tote bzw. produktiv fragwürdige Altpfade mit sehr geringem Risiko
  - produktive Kernpfade wie `MyProfilePage`, Login, Home, Stammdaten, Export, Ablesen und Arbeitsstunden bleiben unberührt
  - statt einer riskanten Großreinigung wurden nur eindeutig entkoppelte Altseiten bereinigt
- Bewusst nicht gemacht:
  - keine aggressive Bereinigung lokaler/unversionierter Testartefakte
  - keine Änderung an `MyProfilePage`, weil die Seite produktiv sinnvoll bleibt
  - keine WPF-Löschung von `FotoUploadTestView`, weil dessen aktiver Produkt-/Teststatus im aktuellen Arbeitsbaum nicht belastbar genug für eine sichere Kleinbereinigung war
  - keine neue Navigationsarchitektur
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - produktive Navigation bleibt intakt
  - `MyProfilePage` bleibt erreichbar
  - kein produktiver MAUI-`Beenden`-Altpfad mehr im Flyout
  - keine Verschlechterung der bisher geprüften Hauptpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - der kleine Aufräumblock bleibt buildfähig

## 2026-03-24 – Systematische View-Prüfung Block 8: Auth-Sonderwege / Dialogersatz mit kleinem MAUI-Auth-Dialogersatz-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 8 gezielt geprüft:
  - WPF: `ChangeEmailWindow`, `ResetPasswordWindow`, `DatumDialog`
  - MAUI: `LoginPage`, `MyProfilePage`, aktive Auth-/OTP-/Passwort-/Mailänderungs-Pfade sowie die relevanten Auth-Servicepfade
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der OTP-/Recovery-Unterbau ist in MAUI bereits vorhanden und läuft produktiv über `IAuthService`
  - der Passwort-Reset ist mobil bereits fachlich tragfähig im Login integriert
  - `DatumDialog` ist mobil durch vorhandene `DatePicker`-/Inline-Eingaben ausreichend implizit gelöst; dafür war kein eigener neuer Dialogpfad nötig
  - die größte kleine Restlücke lag aktuell bei der Mailänderung auf `MyProfilePage`
  - dort war der mobile Pfad bisher nur als Folge von `DisplayPromptAsync(...)` gelöst und damit schwächer als der vorhandene WPF-Dialogfluss
- Wichtigste kleine Restabweichung mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - die MAUI-Mailänderung bot noch keinen stabilen sichtbaren Dialogersatz mit klarer OTP-Stufe, Statusanzeige und Wiederholbarkeit
  - zusätzlich fehlte auf der Profilseite ein kleiner direkter Passwort-Reset-Sonderweg analog zum WPF-/Login-Kontext
- Kleinen Korrekturblock deshalb nur innerhalb von Auth-Sonderwegen / Dialogersatz umgesetzt:
  - `MyProfilePage` erhielt einen echten inline Dialogersatz für die Mailänderung statt der bisherigen Prompt-Kette
  - sichtbar sind jetzt:
    - aktuelle E-Mail
    - neue E-Mail
    - OTP-Stufe nach Codeanforderung
    - laufende Statusrückmeldung direkt auf der Seite
  - der mobile Flow nutzt weiterhin dieselben bestehenden Auth-Servicepfade:
    - `RequestEmailChangeAsync(...)`
    - `VerifyEmailChangeOtpAsync(...)`
  - zusätzlich wurde auf derselben Profilseite ein kleiner stabiler Button `Passwort-Reset senden` ergänzt
  - dieser nutzt weiter den bestehenden produktiven Recovery-Pfad `SendPasswordResetEmailAsync(...)`
  - die eigentliche Codeeingabe und Passwort-Neusetzung bleiben bewusst im vorhandenen Login-Flow
- Warum dieser kleine Block fachlich sinnvoll ist:
  - Mailänderung war der sichtbar schwächste mobile Sonderpfad gegenüber WPF
  - ein inline Dialogersatz ist mobil fachlich gleichwertiger als verschachtelte Prompts, ohne ein WPF-Fenster 1:1 nachzubauen
  - der Block bleibt vollständig auf bestehenden Auth-/OTP-Pfaden und baut keine Schattenlogik
- Bewusst nicht gemacht:
  - keine neue Auth-Architektur
  - kein separater neuer MAUI-Sonderseitenbaum nur für Mailänderung oder Reset
  - kein künstlicher Nachbau von `DatumDialog`, weil `DatePicker`-Inlinepfade mobil bereits ausreichend sind
  - keine Änderung an Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs-, Export- oder Ablesenpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Login bleibt stabil
  - bestehende OTP-/Auth-Pfade bleiben intakt
  - Mailänderung ist mobil jetzt sichtbarer und stabiler erreichbar
  - Passwort-Reset ist aus dem Login weiterhin nutzbar und zusätzlich im Profilkontext direkt anstoßbar
  - keine Verschlechterung bereits geprüfter Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs-, Export- oder Ablesenpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte bestehende Warnungen bleiben außerhalb dieses kleinen Blocks bestehen

## 2026-03-24 – Systematische View-Prüfung Block 7: Ablesen / RFID / Zählerwechsel mit kleinem MAUI-Rückwege-/Refresh-/Reset-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 7 gezielt geprüft:
  - WPF: `AblesenOverviewView`, `AblesungErfassenView`, `AblesungDialog`, `RfidEinrichtenView`, `RfidScanContextView`, `FaelligeZaehlerView`, `ZaehlerwechselScanView`, `ZaehlerwechselAusbauView`, `ZaehlerwechselEinbauView`, `ZaehlerTauschDialog`
  - MAUI: `AblesenOverviewPage`, `AblesungErfassenPage`, `AblesenFeaturePage`, `RfidEinrichtenPage`, `RfidScanWorkflowPage`, `FaelligeZaehlerPage`, `ZaehlerwechselPage` sowie die zugehörigen RFID-/Scan-ViewModels
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der MAUI-Ablesen-Bereich ist bereits erreichbar und nutzt die vorhandenen echten Shared-Servicepfade für RFID-Kontext, RFID-Zuordnung und fällige Zähler
  - `RfidEinrichtenPage` und `FaelligeZaehlerPage` sind fachlich bereits produktiv nutzbar
  - `AblesungErfassenPage` und `ZaehlerwechselPage` sind mobil derzeit – analog zum aktuell sichtbaren WPF-Hauptpfad – vor allem Kontext-/Einordnungsseiten und noch kein voller mobiler Endausbau von Ablesungsdialog bzw. Ausbau-/Einbau-Workflow
  - der größte kleine Restabstand lag damit aktuell vor allem in Flow, Rückwegen und Wiederverwendbarkeit im mobilen Scanpfad
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - in den mobilen Unterseiten fehlte ein expliziter Rückweg zurück zur `Ablesen`-Übersicht
  - wiederholte RFID-Prüfungen auf derselben Seite hatten noch keinen klaren Reset-/Neustartpfad
  - `FaelligeZaehlerPage` lud bisher nur beim ersten Anzeigen und konnte beim Rückweg veraltet bleiben
  - `RfidEinrichtenPage` initialisierte bisher ebenfalls nur einmal pro Seiteninstanz
- Kleinen Korrekturblock deshalb nur im MAUI-Ablesen-/RFID-/Zählerwechsel-Rahmen umgesetzt:
  - `RfidScanContextViewModel` erhielt einen kleinen `Reset()`-Pfad für neuen Scanbeginn ohne neue Schattenlogik
  - `RfidScanWorkflowPage` bietet jetzt zusätzlich `Neuen Scan beginnen`
  - dieselbe Workflowbasis bietet jetzt einen expliziten Button `Zur Ablesen-Übersicht`
  - `RfidEinrichtenPage` erhielt ebenfalls einen expliziten Rückweg zur `Ablesen`-Übersicht
  - `RfidEinrichtenPage` initialisiert beim Wiedererscheinen erneut und bleibt damit kontextstabiler
  - `FaelligeZaehlerPage` lädt beim Wiedererscheinen jetzt erneut und bleibt dadurch aktueller
- Warum dieser kleine Block fachlich sinnvoll ist:
  - die zugrunde liegenden Fachpfade waren bereits vorhanden
  - der größte direkte Nutzwert lag deshalb in saubereren Rückwegen und in einem wiederholbaren Scanfluss statt in vorschneller neuer Unterseitenarchitektur
  - der Block bleibt vollständig auf bestehenden Shared-Services und bestehenden MAUI-Seiten
- Bewusst nicht gemacht:
  - kein neuer mobiler Vollworkflow für `AblesungDialog`
  - kein neuer Ausbau-/Einbau-Unterseitenbau für Zählerwechsel
  - kein Umbau von Foto-/Upload-/Cloudpfaden
  - keine Änderung an Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs- oder Exportpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Ablesen-Bereich bleibt erreichbar
  - RFID-/Scanpfade bleiben erreichbar
  - Zählerwechsel bleibt erreichbar
  - Rückwege und Wiederholungsfluss im mobilen Scanpfad sind klarer
  - keine Verschlechterung bereits geprüfter Home-, Mitglieds-, Garten-, Arbeitsstunden- oder Verwaltungs-/Exportpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - der geänderte MAUI-Pfad baut ohne neue eigene Warnungen

## 2026-03-24 – Systematische View-Prüfung Block 5: Arbeitsstunden komplett mit kleinem mobilen Überblick-/Erfassungs-/Bearbeitungs-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 5 gezielt geprüft:
  - WPF: `ArbeitsstundenView`, `ArbeitsstundenPruefungView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `MyArbeitsstundenPage`, `ArbeitsstundenReviewPage` sowie die relevanten Shared-Servicepfade rund um Arbeitsstunden
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der mobile Arbeitsstunden-Unterbau ist bereits belastbar vorhanden
  - Erfassung und Review laufen in MAUI bereits auf denselben Shared-Servicepfaden wie in WPF
  - der größte kleine Restabstand lag aktuell im normalen mobilen Nutzerpfad, nicht in fehlender Basisspeicherung
  - `MyArbeitsstundenPage` blieb im aktuellen Stand gegenüber WPF schwächer bei Übersicht, Rückkehrstabilität und Bearbeitung bereits erfasster Einträge
  - der mobile Freigabepfad bleibt funktional vorhanden, ist gegenüber WPF aber weiterhin deutlich einfacher
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - `MyArbeitsstundenPage` initialisierte bisher nur einmal pro Seiteninstanz und konnte beim Rückweg veraltete Daten zeigen
  - mobil fehlte auf der eigenen Arbeitsstunden-Seite ein klarer `Soll / Geleistet / Offen`-Überblick
  - bestehende Arbeitsstunden konnten mobil im normalen Erfassungspfad noch nicht im selben Formular wieder bearbeitet werden
  - die mobile Stundenvalidierung wich mit einer zusätzlichen `<= 24`-Sonderregel unnötig vom WPF-/Shared-Pfad ab
- Kleinen Korrekturblock deshalb nur im Arbeitsstunden-Bereich umgesetzt:
  - `MyArbeitsstundenPage` lädt beim Wiedererscheinen jetzt erneut und bleibt dadurch kontextstabiler
  - die Seite zeigt jetzt oben einen kleinen Pflichtstunden-Überblick mit `Soll`, `Geleistet` und `Offen`
  - der Überblick nutzt den vorhandenen Shared-Servicepfad `GetPflichtstundenUebersichtForMitgliedAsync(...)`
  - das bestehende mobile Erfassungsformular kann jetzt auch zum Bearbeiten bereits erfasster Arbeitsstunden wiederverwendet werden
  - Antippen eines bestehenden Eintrags füllt das Formular; derselbe Speichern-Pfad schreibt dann per `UpdateArbeitsstundeAsync(...)`
  - zusätzlicher Button `Bearbeiten abbrechen` setzt den Editorzustand sauber zurück
  - die Stundenvalidierung wurde auf den belastbaren WPF-/Shared-Ansatz zurückgezogen: gültige Zahl größer als `0`, keine zusätzliche Schattenregel `<= 24`
  - die Eingabe `Art der Arbeit` wurde mobil von einem einfachen Einzeilenfeld auf einen besser lesbaren Editor angehoben
- Warum dieser kleine Block fachlich sinnvoll ist:
  - der größte echte Praxisabstand lag im normalen mobilen Nutzerpfad
  - Übersicht, Rückkehrstabilität und Bearbeitung liefern direkt Nutzwert ohne neue Architektur
  - der Block bleibt vollständig auf bestehenden Shared-Servicepfaden und baut keine Schattenlogik
- Bewusst nicht gemacht:
  - kein neuer mobiler Review-/Freigabeeditor analog zur WPF-Prüftabelle
  - kein Umbau der bestehenden WPF-Dialogarchitektur
  - keine Änderung an Home, Mitglieds-, Garten-, Verwaltungs-, Export- oder Ablesen-Pfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Arbeitsstundenbereich bleibt erreichbar
  - Nutzerpfad bleibt intakt und ist jetzt auf Mobil übersichtlicher und bearbeitbarer
  - Review-/Freigabepfad bleibt unverändert nutzbar
  - keine Verschlechterung bereits geprüfter Home-, Mitglieds- oder Gartenpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – Systematische View-Prüfung Block 4: Parzellen / Garten / Dokumente / Strom / Wasser mit kleinem Rückwege-/Kontext-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 4 gezielt geprüft:
  - WPF: `ParzellenVerwaltungView`, `DokumenteView`, `DokumenteParzellenView`, `GartenDokumenteView`, `GartenDokumenteListeView`, `GartenStromView`, `GartenWasserView`, `StromView`, `WasserView`
  - MAUI: `ParzellenPage`, `DokumentePage`, relevante Bereiche in `MeineDatenPage`, `ParzellenContextState`, `ParzellenViewModel`
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der fachliche MAUI-Parzellenkern ist bereits belastbar vorhanden
  - Gartenkontext aus dem Mitgliedskontext funktioniert bereits
  - Strom, Wasser und Parzellen-Dokumente sind auf demselben mobilen Fachpfad bereits nutzbar
  - der größte kleine Restabstand lag aktuell weniger in fehlender Fachlogik als in Rückwegen und Kontextklarheit
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - aus dem mobilen Gartenkontext gab es noch keinen expliziten Rückweg direkt zurück in die Stammdatenansicht
  - der Rückweg zur globalen Parzellenübersicht war sprachlich noch zu nah am Kontextpfad formuliert
  - die Trennung zwischen Mitgliedsdokumenten und Garten-/Parzellen-Dokumenten war mobil funktional vorhanden, aber im UI noch nicht klar genug benannt
  - beim Wiedererscheinen der `ParzellenPage` wurde der gewählte Detailkontext nicht ausdrücklich erneut nachgeladen
- Kleinen Korrekturblock deshalb nur im Parzellen-/Garten-/Dokumente-Rahmen umgesetzt:
  - `ParzellenPage` erhielt im mitgliedsgebundenen Gartenkontext jetzt einen expliziten Button `Zur Stammdatenansicht`
  - der bisherige Kontext-Reset wurde sprachlich auf `Zur globalen Parzellenübersicht` geschärft
  - der Dokumentebereich im Parzellen-/Gartenkontext wird jetzt explizit als `Garten-Dokumente` bezeichnet
  - zusätzlicher Hinweis macht klar, dass Mitgliedsdokumente weiterhin in den Stammdaten bleiben, während hier die Dokumente der ausgewählten Parzelle erscheinen
  - `ParzellenViewModel` kann den aktuell gewählten Detailkontext jetzt gezielt erneut laden; `ParzellenPage` nutzt das beim Wiedererscheinen der Seite
- Warum dieser kleine Block fachlich sinnvoll ist:
  - die bestehende MAUI-Parzellenlogik war bereits funktional belastbar
  - der größte Unterschied lag damit im Flow, in der Trennung der Kontexte und in der Rücknavigation
  - genau dort wurde jetzt nachgezogen, ohne neue Unterseiten oder Schattenlogik zu bauen
- Bewusst nicht gemacht:
  - keine neue Parzellen-/Garten-Seitenarchitektur
  - kein neuer Dokumente-Unterbau
  - kein Umbau der bestehenden Strom-/Wasser-Speicherpfade
  - keine Änderung an Arbeitsstunden, Verwaltung, Export oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Parzellenpfad bleibt erreichbar
  - Gartenkontext aus Stammdaten funktioniert weiter
  - Rückwege zwischen Gartenkontext, globaler Parzellenübersicht und Stammdaten sind klarer
  - Dokumente / Strom / Wasser bleiben nutzbar
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – Systematische View-Prüfung Block 3: Mitglied neu / Wartungsverträge / erweiterte Mitgliedsverwaltung mit kleinem Wartungsvertrags-Einstieg fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 3 gezielt geprüft:
  - WPF: `MitgliedNeuView`, `NewMemberView`, `MemberWartungsvertraegeView`, `WartungsvertraegeVerwaltungView`
  - MAUI: vorhandene Pendants bzw. fehlende Pfade rund um Mitgliedsanlage, Wartungsverträge und erweiterte Mitgliedsverwaltung
  - ergänzend die Shared-Service-/Schema-Pfade für Wartungsverträge und Pflichtstundenbezug
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `Mitglied neu` ist in WPF derzeit selbst nur als Placeholder vorhanden; in MAUI existiert dafür aktuell kein belastbarer produktiver Pfad
  - `MemberWartungsvertraegeView` und `WartungsvertraegeVerwaltungView` sind in WPF derzeit ebenfalls nur vorbereitete Placeholder
  - in MAUI existiert ebenfalls noch kein echter eigener Verwaltungseditor für Wartungsverträge
  - belastbar vorhanden ist aber bereits der gemeinsame Datenpfad über `v_pflichtstunden_uebersicht`, inklusive `hat_wartungsvertrag`, `ist_befreit` und `regelgrund`
- Deshalb bewusst den kleineren und fachlich klareren Einstieg gewählt statt `Mitglied neu` oder Wartungsvertragsverwaltung halb zu eröffnen:
  - keinen Fake-CRUD für `Mitglied neu`
  - keinen halben Wartungsvertragseditor ohne belastbaren Referenzpfad
  - stattdessen einen echten kleinen ReadOnly-Einstieg im mobilen Mitgliedskontext
- Umgesetzt:
  - neuer Shared-Service-Lesepfad `GetPflichtstundenUebersichtForMitgliedAsync(int mitgliedId)` in `ISupabaseService` / `SupabaseService`
  - der Pfad nutzt den bestehenden Pflichtstunden-/Wartungsvertragsstatus statt Schattenlogik
  - `MeineDatenPage` zeigt jetzt zusätzlich einen Abschnitt `Wartungsverträge / Pflichtstunden`
  - sichtbar werden dort für das ausgewählte Mitglied:
    - Bewertungsjahr
    - Wartungsvertrag ja/nein
    - von Pflichtstunden befreit ja/nein
    - Regelgrund
  - für Admin/Vorstand wird zusätzlich ehrlich eingeblendet, dass ein eigener mobiler Wartungsvertrags-Verwaltungseditor im aktuellen Stand noch nicht vorhanden ist
  - für normale Nutzer wird derselbe Status als ReadOnly-Kontextinformation aus der zentralen Pflichtstunden-Übersicht angezeigt
- Warum dieser kleine Block fachlich sinnvoll ist:
  - Wartungsverträge hängen direkt mit Pflichtstundenbefreiung und Mitgliedskontext zusammen
  - die zugrunde liegenden Informationen sind bereits belastbar vorhanden
  - der Block liefert deshalb echten Nutzwert, ohne neue Pflege-/Schreibarchitektur zu erfinden
- Bewusst nicht gemacht:
  - kein neuer Pfad `Mitglied neu`
  - kein mobiler CRUD-Editor für `wartungsvertraege` oder `wartungsvertrag_zuordnungen`
  - keine Änderung an Home, Shell, Parzellen, Export, Verwaltung oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Stammdaten-/Mitgliedskontext bleibt intakt
  - der neue ReadOnly-Pfad für Wartungsvertrags-/Befreiungsstatus ist im Mitgliedskontext erreichbar
  - Rechtepfade bleiben unverändert korrekt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – Systematische View-Prüfung Block 2: Mitgliedersuche / Stammdaten / Mitgliedskontext mit kleinem Kontext-/Nebenmitglied-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für Block 2 gezielt geprüft:
  - WPF: `MemberSearchView`, `MemberDetailView`, `StammdatenView`, `MemberParzellenSection`, `NebenmitgliedDialog`, `UserManagementView`
  - MAUI: `MemberSearchPage`, `MeineDatenPage.xaml.cs`, `UserManagementPage`, `NebenmitgliedPage`, `MemberContextState` und zugehörige Kontextpfade
- Systematischer Vergleich entlang derselben Logik durchgeführt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - Mitgliedersuche, Stammdaten und der grundsätzliche Mitgliedskontext sind mobil bereits belastbar vorhanden
  - Stammdaten wurden zuletzt bereits auf `Stammdaten` umbenannt und inhaltlich verbreitert
  - `UserManagement` bleibt an den Mitgliedskontext gebunden
  - der größte kleine Restabstand lag jetzt beim Kontextverhalten rund um Mitgliedersuche-Wiederkehr und den mobilen Nebenmitgliedspfad
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - `MemberSearchPage` initialisierte bisher nur beim ersten Anzeigen und konnte beim Rückweg in dieselbe Seiteninstanz veraltet bleiben
  - der Nebenmitgliedspfad war mobil zwar vorhanden, aber aus dem ausgewählten Mitgliedskontext heraus nicht direkt erreichbar
  - `NebenmitgliedPage` orientierte sich bisher nur am angemeldeten Hauptmitglied statt am aktuell ausgewählten Mitgliedskontext
- Kleinen Korrekturblock deshalb nur innerhalb von Mitgliedersuche / Stammdaten / Mitgliedskontext umgesetzt:
  - `MemberSearchPage` initialisiert beim erneuten Anzeigen jetzt wieder und bleibt dadurch konsistenter zum WPF-Rückkehrverhalten
  - `MeineDatenPage` zeigt jetzt einen kleinen `Mitgliedskontext`-Block für den vorhandenen Nebenmitgliedspfad
  - dort wird sichtbar gemacht:
    - wenn ein Nebenmitglied vorhanden ist
    - wenn keines vorhanden ist
    - wenn das ausgewählte Mitglied selbst kein Hauptmitglied ist
  - `NebenmitgliedPage` nutzt den ausgewählten `MemberContextState` jetzt bevorzugt als Hauptmitgliedsbezug statt nur den global angemeldeten Benutzerkontext
  - zusätzlich lädt die mobile Nebenmitgliedseite beim Wiedererscheinen erneut und bleibt dadurch kontextstabiler
  - die mobile Mitgliedsabbildung übernimmt jetzt auch `IstHauptmitglied`, damit die Kontextentscheidung belastbar bleibt
- Bewusst nicht gemacht:
  - keine Neuarchitektur
  - kein neuer kompletter Nebenmitglied-Anlageflow in MAUI
  - keine tieferen Parzellen-/Gartenpfade in diesem Block mitgezogen
  - keine Änderung an Home, Shell, Export, Verwaltung oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Mitgliedersuche bleibt stabil nutzbar
  - Stammdaten bleiben erreichbar
  - Mitgliedskontext bleibt konsistent
  - Nebenmitglied ist aus dem Mitgliedskontext jetzt direkt erreichbar, sofern fachlich vorhanden/relevant
  - `UserManagement` bleibt unverändert an den Mitgliedskontext gebunden
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – Systematische View-Prüfung Block 1: Einstieg / Shell / Home mit kleinem MAUI-Refresh-/Detailpfad-Fix fortgeführt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Für den Einstieg-/Shell-/Home-Vergleich gezielt geprüft:
  - WPF: `LoginWindow`, `MainWindow`, `HomeView`, `HomeSectionDetailView`, relevante ViewModels
  - MAUI: `LoginPage`, `App.xaml.cs`, `AdminShell`, `UserShell`, `HomePage`, `HomeSectionDetailPage`, `ShellNavigationHelper`
- Abgleich Ergebnis im aktuellen Repo-Stand:
  - Login-/Root-/Shell-Startpfad ist im Kern bereits sauber nachgezogen
  - Home landet bereits stabil auf `home`
  - Detailpfad aus Home ist grundsätzlich vorhanden
  - relevante Restabweichungen zum WPF-Rahmen blieben aber noch im kleinen Nutzungsfluss nach dem ersten Einstieg
- Wichtigste kleine Restabweichung mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - WPF lädt Home beim erneuten Öffnen/Navigieren wieder frisch
  - MAUI-`HomePage` initialisierte bisher nur einmal pro Seiteninstanz
  - dadurch konnten Rückkehrpfade aus Detail-/Verwaltungsseiten fachlich mit veraltetem Home-Zustand stehenbleiben
- Kleinen Korrekturblock deshalb nur im Home-/Detailpfad umgesetzt:
  - `HomePage.OnAppearing()` lädt Home jetzt bei erneutem Sichtbarwerden wieder nach
  - gleichzeitig gegen Reentrancy abgesichert über kleinen `_isLoading`-Guard
  - `HomeSectionDetailPage` erhielt zusätzlich einen expliziten Button `Zur Startseite`, analog zum klaren WPF-Rückweg
  - Rückweg nutzt den bestehenden Shell-Home-Pfad und öffnet keine neue Navigationsarchitektur
- Bewusst nicht gemacht:
  - keine neue Shell-Architektur
  - kein Umbau von Login-/Root-/Android-Pfaden
  - noch keine Folgeblöcke für Mitgliedersuche, Stammdaten, Parzellen, Arbeitsstunden oder Verwaltung mitgezogen
  - weitere erkannte WPF-/MAUI-Deltas aus Einstieg/Home bewusst für spätere systematische Blöcke dokumentativ offen gelassen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Login bleibt funktionsfähig
  - Home bleibt direkt und stabil erreichbar
  - Rückkehr aus Home-Detail hat jetzt einen expliziten Startseitenpfad
  - Home-Daten werden beim Wiedererscheinen der Seite erneut geladen und damit konsistenter zum WPF-Navigationsverhalten
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – MAUI-Stammdaten-Block: Mitgliedskontext in `Stammdaten` umbenannt und mobile Stammdatenansicht an WPF angenähert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Relevante Referenzpfade geprüft:
  - `KGV.Wpf/ViewModels/MemberDetailViewModel.cs`
  - `KGV.Wpf/Views/MemberDetailView.xaml`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `KGV.Maui/State/MemberContextState.cs`
  - ergänzend die mobilen Such-/Kontextpfade, damit die bestehende Navigation intakt bleibt
- Relevanter Istzustand vor dem Block:
  - der mobile Mitgliedskontext war bereits vorhanden
  - die Seite lief aber sprachlich noch als `Mitgliedskontext`
  - fachlich zeigte sie gegenüber WPF nur einen verkürzten Auszug statt klar gruppierter Stammdaten
- Den Block bewusst klein und nur auf den mobilen Stammdatenbereich gehalten:
  - keine Neuarchitektur
  - kein Umbau von Home, Login oder Shell außerhalb der nötigen Benennung
  - keine neue Schattenlogik neben den bestehenden Servicepfaden
- Umgesetzt:
  - Titel des mobilen Mitgliedskontexts auf `Stammdaten` gezogen
  - bestehende `MeineDatenPage` weiterverwendet und inhaltlich deutlich ausgebaut
  - Stammdaten jetzt mobil sauber gruppiert analog zur WPF-Referenz:
    - `Grunddaten`
    - `Kontakt`
    - `Adresse`
    - `Mitgliedschaft`
    - `Gärten / Parzellen`
    - `Verwaltung`
  - zusätzliche relevante WPF-Felder jetzt auch mobil sichtbar:
    - Nachname
    - Vorname
    - Geburtsdatum
    - E-Mail
    - Telefon
    - Mobilnummer
    - WhatsApp-Einwilligung
    - Straße / Hausnummer
    - PLZ
    - Ort
    - Rolle
    - Mitglied seit
    - Mitglied Ende
    - Aktiv
    - Bemerkungen
  - die Werte werden als ruhige, saubere ReadOnly-Felder dargestellt statt als wenige Sammeltexte
  - der Verwaltungsbereich bleibt funktional erhalten, wird aber für nicht berechtigte Rollen vollständig ausgeblendet
  - der Aktualisieren-Button wurde sprachlich auf den Stammdatenkontext angepasst
- Bewusst nicht gemacht:
  - keine neue mobile Vollbearbeitungslogik für alle Stammdatenfelder in diesem kleinen Block
  - keine WPF-Änderung
  - keine Änderung an Dokumente-, Garten- oder Admin-Menü-Pfaden außerhalb der Seite selbst
- Ehrliche Restlage nach dem Block:
  - der mobile Stammdatenbereich ist jetzt fachlich deutlich näher an WPF und keine knappe Platzhalteransicht mehr
  - einzelne WPF-Bearbeitungskomfortpfade wie vollwertiger Edit-/Lock-Flow für alle Stammdatenfelder sind mobil in diesem Block bewusst noch nicht nachgezogen
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 – MAUI-UI-/Flow-Feinschliff: Home nach Login direkt sichtbar, Arbeitsstunden-Kacheln klarer, Home-Export entfernt, Mitgliedersuche beruhigt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft.
- Relevante MAUI-Dateien gezielt geprüft:
  - `App.xaml.cs`
  - `LoginPage.xaml.cs`
  - `AdminShell.cs`
  - `UserShell.cs`
  - `ShellNavigationHelper.cs`
  - `HomePage.xaml.cs`
  - `HomeViewModel.cs`
  - `MemberSearchPage.xaml`
  - `MemberSearchPage.xaml.cs`
  - `MemberSearchViewModel.cs`
- Relevanter Befund zum Login-Handover:
  - die Shell wurde nach Login bereits korrekt aufgebaut
  - der aktive Shell-Einstieg wurde aber nur generisch auf einen gültigen Eintrag gezogen, nicht gezielt auf `home`
  - dadurch blieb ein plausibler Pfad, dass die richtige Shell steht, Home aber nicht sofort als sichtbare Zielseite landet
- Den Home-Start deshalb minimal-invasiv gehärtet:
  - `ShellNavigationHelper` kann jetzt gezielt eine bevorzugte Content-Route aktivieren
  - `App`, `AdminShell`, `UserShell` und der Login-Fallback bevorzugen jetzt explizit die Route `home`
  - damit landet der frische Shell-Aufbau nach Login direkt sichtbar auf `Startseite`, ohne die bestehende Root-/Fragment-Härtung wieder aufzureißen
- Home-Seite punktuell weiter verbessert:
  - `Arbeitsstunden` zeigt `Soll`, `Geleistet` und `Offen` jetzt als drei ruhige Kennzahl-Karten statt nur als verdichtete Zeile
  - die bisherige Fallback-Liste bleibt nur noch sichtbar, wenn keine echte Pflichtstunden-Zusammenfassung geladen wurde
  - der Home-Shortcut `Mitglieder exportieren` wurde aus dem Verwaltungsbereich entfernt; der separate Export-Navigationspunkt bleibt unverändert erhalten
- Mitgliedersuche optisch beruhigt:
  - die große Checkbox für `Hauptmitglied` wurde aus der Listenansicht entfernt
  - statt dessen klare Kartenstruktur mit:
    - Name links
    - Zusatzzeile darunter
    - Gartennummer(n) rechts als ruhiges Badge
    - kleines textliches `Hauptmitglied`-Badge statt sperriger Checkbox
  - Auswahl-/Navigationsfunktion bleibt unverändert erhalten
- Bewusst nicht gemacht:
  - keine neue Architektur
  - keine Änderung an WPF-Dateien
  - kein Eingriff in Export-Logik oder Mitgliedskontextpfade außerhalb des kleinen UI-/Flow-Feinschliffs
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs`

## 2026-03-24 – MAUI-Home-Feinschliff: Schnellzugriff entfernt und Startseitenbereiche ruhiger gegliedert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den aktuellen MAUI-Home-Pfad gezielt geprüft:
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - vorhandene MAUI-Stile unter `Resources/Styles`
- Relevanter Istzustand vor dem Block:
  - `Schnellzugriff` war auf Home noch sichtbar
  - die übrigen Home-Bereiche waren funktional vorhanden, wirkten aber visuell noch zu nah beieinander
  - besonders `Arbeitsstunden` und `Arbeitseinsätze` waren optisch zu wenig voneinander getrennt
- Den Block bewusst klein und rein visuell gehalten:
  - kein neuer Fachpfad
  - keine neue Navigation
  - keine Änderung an den bestehenden Detailseitenpfaden
- Umgesetzt:
  - Bereich `Schnellzugriff` aus der mobilen Home-Seite entfernt
  - die Home-Seite stattdessen in ruhige, klar getrennte Abschnittskarten gegliedert
  - vier Bereiche bekamen jeweils eine eigene, zurückhaltende visuelle Identität über subtile Hintergrund-/Akzentfarben:
    - `Arbeitsstunden`
    - `Arbeitseinsätze`
    - `Termine`
    - `Bekanntmachungen`
  - jede Sektion besitzt jetzt klareren Header, Untertitel, Trennlinie und konsistentere Innenabstände
  - die Einträge innerhalb der Bereiche laufen jetzt ebenfalls als ruhig akzentuierte Karten statt als kaum getrennte Textblöcke
  - der vorhandene Verwaltungsbereich für Admin/Vorstand wurde optisch an dieselbe Struktur angepasst
- Kleine inhaltliche Begradigung im ViewModel:
  - der frühere operative Bereich wird auf Home jetzt klar als `Arbeitsstunden` bezeichnet
  - damit passt die sichtbare Struktur wieder direkt zur gewünschten Fachgliederung
- Bewusst nicht gemacht:
  - keine Änderung an WPF-Dateien, weil der Block ausdrücklich MAUI-Home-Feinschliff war
  - keine neue Logik für `QuickLinks`
  - keine Änderung der bestehenden Klickpfade zu Detail-/Verwaltungsseiten
- Fachliche Kurzvalidierung nach dem Umbau:
  - `Schnellzugriff` ist auf Home entfernt
  - die Home-Seite bleibt vollständig nutzbar
  - die vier Hauptbereiche sind visuell klarer unterscheidbar, ohne bunt-chaotisch zu wirken
  - bestehende Klickpfade über Home bleiben erhalten
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen liegen weiterhin in `HomeManagementPage.cs`

## 2026-03-24 – Repo-Wahrheit zum MAUI-Root-Handover verifiziert und Android-Fragment-Restore gegen NavigationRootManager-Crash abgesichert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog, den echten Git-Arbeitsbaum und zusätzlich die pausierte Android-Debugsitzung geprüft.
- Repo-Wahrheit zuerst ausdrücklich verifiziert:
  - der zuletzt dokumentierte Root-Handover-Fix ist im aktiven Repo tatsächlich vorhanden
  - `App.xaml.cs` enthält bereits die zentrale Root-Erzeugung und `SwitchToCurrentRootAsync()`
  - `LoginPage.xaml.cs` delegiert den Handover bereits an `App`
  - damit war die Ausgangslage nicht „Fix fehlt im Repo“, sondern „Fix vorhanden, Crashpfad bleibt trotzdem aktiv“
- Den verbleibenden Crash deshalb nicht erneut geraten, sondern über die Android-Debuglogs eingegrenzt:
  - Crash weiterhin beim Activity-Start, noch vor nutzbarer Login-Interaktion
  - Stacktrace: `NavigationRootManager_ElementBasedFragment` / `No view found for id ...`
  - damit lag der Restfehler nun plausibler im Android-Fragment-Restore eines alten/stalen MAUI-Navigationszustands als im bereits eingebauten Root-Handover-Code selbst
- Kleinen Android-Fix direkt am wahrscheinlichsten Restpfad umgesetzt:
  - `MainActivity.OnCreate(Bundle? savedInstanceState)` ergänzt
  - `base.OnCreate(null)` statt Restore des vorhandenen `savedInstanceState`
  - dadurch versucht Android beim Start nicht mehr, alte MAUI-/Fragment-Navigationscontainer wieder an eine inzwischen nicht mehr passende Rootstruktur zu hängen
- Dieser Fix bleibt minimal-invasiv:
  - keine neue Navigationsarchitektur
  - kein Rückbau des echten Root-Handover-Stands
  - keine unnötigen WPF-Änderungen
- Loginbutton-/Altlogik nochmals gegengeprüft:
  - `Anmelden` bleibt im aktiven Repo klarer Textbutton
  - keine alte Icon-Button-Logik mehr aktiv gefunden
- Build-/Deployrest kurz mitgedacht:
  - Debuglogs zeigen weiter FastDev-/Override-Pfade auf dem Gerät
  - das kann ältere Gerätezustände begünstigen, war aber hier nicht die eigentliche Codeursache des beobachteten Fragment-Restore-Crashs
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unveränderte Warnungen bleiben in `HomeManagementPage.cs`

## 2026-03-24 – MAUI-Login-Rootwechsel für Android gegen `NavigationRootManager_ElementBasedFragment` abgesichert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den aktuellen MAUI-Root-/Shellpfad nochmals gezielt geprüft:
  - `App.xaml.cs`
  - `LoginPage.xaml.cs`
  - `AdminShell.cs`
  - `UserShell.cs`
  - `ShellNavigationHelper.cs`
  - `MauiProgram.cs`
- Zusätzlich die Debug-/Android-Logs geprüft, weil die Debugsitzung pausiert vorlag.
- Relevanter Befund aus den Logs:
  - Crash auf Android: `No view found for id ... for fragment NavigationRootManager_ElementBasedFragment`
  - der Stack lief in den Android-Fragment-/Activity-Startpfad hinein
  - das passte fachlich zu einem Root-Mismatch zwischen alter Login-Hierarchie und neuer Shell-Hierarchie
- Plausibelste Ursache im aktuellen Stand präzisiert:
  - `App.CreateWindow(...)` erzeugte immer `LoginPage` als Window-Root
  - nach Login wurde später per `window.Page = shell` auf Shell umgestellt
  - wenn Android Activity/Window erneut anlegt oder Fragmente restauriert, kann dadurch ein alter Fragmentzustand gegen einen nicht mehr passenden Rootcontainer laufen
- Den Fix klein und direkt am Root-Handover umgesetzt:
  - `App` baut den aktuellen Root jetzt zentral selbst auf statt nur eine feste `LoginPage` zu halten
  - `CreateWindow(...)` liefert abhängig vom aktuellen Benutzerkontext entweder `LoginPage` oder eine frisch gebaute Ziel-Shell
  - dadurch bekommt auch ein später neu erzeugtes Android-Window wieder die zur Session passende Root-Hierarchie
  - für den aktiven Loginwechsel wurde der Rootwechsel in `App.SwitchToCurrentRootAsync()` zentralisiert
  - auf Android wird dabei ein neues Window mit passender Ziel-Root geöffnet und das alte Login-Window erst danach geschlossen
  - damit läuft kein Android-Fragmenthost mehr gegen eine gerade ersetzte Rootstruktur im selben Window weiter
- `LoginPage` verwendet diesen zentralen Handover jetzt direkt statt selbst die Window-Root-Seite hart zu ersetzen.
- Loginbutton-/Altlogik mitgeprüft:
  - `Anmelden` bleibt ein klarer Textbutton
  - keine alte Icon-Button-Logik mehr im aktiven Loginpfad gefunden
  - Debuglogs zeigen weiter FastDev-/Override-Deploypfade; das ist ein möglicher Grund, warum auf Geräten ältere Stände sichtbar wirken können, aber kein aktiver UI-Codepfad im Repo
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen stammen unverändert aus `HomeManagementPage.cs`

## 2026-03-24 – Android-Shell-Fragment-Crash in MAUI entschärft und Login-Branding klargezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft; blockfremde offene WPF-/Supabase-Dateien blieben wieder unangetastet.
- Gezielt den aktuellen MAUI-Start-/Shell-Pfad geprüft:
  - `App.xaml.cs`
  - `MauiProgram.cs`
  - `AdminShell.cs`
  - `UserShell.cs`
  - `ShellNavigationHelper.cs`
  - `ShellRouteRegistrar.cs`
  - `LoginPage.xaml.cs`
- Plausibelster Crash-Pfad für Android identifiziert:
  - Shell-Routen wurden erst beim Shell-Bau registriert statt einmal zentral beim Start
  - nach dem Login lief zusätzlich noch eine nachgelagerte Shell-Aktivierung über Dispatcher gegen die frisch gesetzte Shell
  - in `AdminShell` wurde der Flyout-Eintrag `Arbeitsstunden freigeben` nach Renderbeginn asynchron über `IsVisible` ein-/ausgeblendet
  - diese Kombination ist ein realistischer Auslöser für verwaiste Android-`ShellItemRenderer`-/Fragmentzustände
- Den Fix klein und minimal-invasiv umgesetzt:
  - gemeinsame Shell-Routen werden jetzt einmal zentral in `MauiProgram` registriert
  - die Shell-Konstruktoren registrieren keine Routen mehr selbst
  - `LoginPage.SwitchToUserContext(...)` setzt nach `BuildMenu()` direkt die Ziel-Shell, ohne zusätzliche nachgelagerte Dispatcher-Navigation gegen dieselbe Shell-Instanz
  - `App.xaml.cs` startet direkt mit `LoginPage` statt mit einer zusätzlichen `NavigationPage`-Hülle, damit beim Wechsel zur Shell kein unnötiger Container im Android-Pfad mitläuft
  - `AdminShell.RefreshWorkhoursReviewMenuAsync()` ändert nur noch den Titel/Badge-Text, blendet den Shell-Eintrag aber nicht mehr nachträglich sichtbar/unsichtbar
  - damit werden nach Renderbeginn keine Shell-Items mehr dynamisch aus dem Android-Container entfernt
- Branding/Login zusätzlich klargezogen:
  - geprüft: aktiver Launcher-Pfad bleibt `Resources/AppIcon/appicon.svg`
  - der Launcher-Icon-Pfad wurde im Projektfile zusätzlich explizit mit `BaseSize` abgesichert
  - der Splash bleibt weiterhin auf demselben KGV-Logo
  - auf der Loginseite bleibt das Logo oben sichtbar
  - der `Anmelden`-Button wurde bewusst wieder als klarer Textbutton ohne Logo im Button selbst ausgeführt
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen stammen unverändert aus `HomeManagementPage.cs` und nicht aus diesem Block

## 2026-03-24 – MAUI-Restabgleich mit Altpfad-Bereinigung und Editor-/Rechte-Feinschliff abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und den echten Git-Arbeitsbaum geprüft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den Restabgleich gezielt auf kleine, klare Praxisprobleme fokussiert statt einen neuen Großblock zu öffnen.
- Bereinigte Alt-/Testpfade:
  - alte `RoleChoicePage` aus MAUI entfernt; der Pfad ist seit der Umstellung auf echten `UserContext` fachlich überholt
  - der Testpfad `Foto-Upload testen` wurde aus dem aktiven `Ablesen`-Einstieg und aus den zentral registrierten Shell-Routen entfernt, damit im produktiven MAUI-Menü kein veralteter Testpfad mehr auftaucht
- Editor-/Verwaltungsfeinschliff auf den mobilen Verwaltungsseiten umgesetzt:
  - `HomeManagementPage` schaltet Eingabefelder für Zeit-/Teilnehmeroptionen jetzt passend aktiv/inaktiv statt alles immer offen zu lassen
  - neuer Datensatz setzt die alte Auswahl im Listenbereich sauber zurück
  - Busy-Zustände sperren Bereichsauswahl, Listen und Speichern belastbar während Ladevorgängen/Speichern
  - Basiskontrollen an WPF angeglichen:
    - Endzeit darf nicht vor Startzeit liegen
    - Stundenwert bei Arbeitseinsätzen darf nicht negativ sein
    - HTML-Inhalt von Bekanntmachungen wird getrimmt gespeichert
  - `Speichern` bleibt am Formularende
- Rechte-/Sichtbarkeitsfeinschliff umgesetzt:
  - `HomeManagementPage` blendet den Editorbereich für nicht berechtigte Rollen komplett aus und lässt nicht nur eine Statusmeldung stehen
  - `ExportPage` blendet die Exportaktion für nicht berechtigte Rollen aus statt nur deaktiviert stehen zu lassen
  - bestehende Regeln bleiben erhalten:
    - `Benutzerverwaltung` nur für Admin
    - `Export` nur für Admin/Vorstand
    - Home-Verwaltung nur für Admin/Vorstand
- Ehrlicher Stand nach diesem Feinschliff:
  - kein neuer fachlicher Blocker im MAUI-Kernpfad sichtbar
  - offen bleiben vor allem Komfort-/Feintuning-Themen und einzelne tiefere Desktop-Editorparitäten
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Export produktiv nachgezogen und Restlücken ehrlich eingeordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausführlichen Fortschrittslog und zusätzlich den echten Git-Arbeitsbaum geprüft; blockfremde offene WPF-/Supabase-Dateien blieben wieder unangetastet.
- WPF-Exportpfad als Referenz geprüft:
  - produktiv vorhanden war dort ein einfacher, aber echter CSV-Export für Mitglieder über `ExportViewModel`
  - der fachlich größte Nutzen mit kleinem Risiko liegt damit klar beim Mitglieder-CSV statt bei einer größeren Exportplattform
- Bestehenden Fach-/Shared-Unterbau geprüft:
  - Exportbasis ist `GetMitgliederAsync()`
  - WPF hatte bisher seine CSV-Erzeugung lokal im ViewModel
  - für MAUI gibt es belastbare Android-Wege für Dateiablage/Teilen über `FileSystem` + `Share`
- Den Block deshalb klein und produktiv umgesetzt:
  - gemeinsame CSV-Erzeugung nach `KGV.Core` in `MitgliederCsvExportBuilder` gezogen, damit WPF und MAUI denselben Exportkern verwenden
  - Demo-/Test-/Play-Store-Mitglieder werden im operativen Export explizit ausgeschlossen
  - WPF-`ExportViewModel` nutzt jetzt denselben Shared-Builder statt eigene CSV-Logik daneben zu halten
  - neue mobile `ExportPage` ergänzt
  - Export erzeugt lokal eine echte CSV-Datei und gibt sie anschließend über den Android-Teilen-/Share-Pfad weiter
  - Admin/Vorstand erreichen den Export jetzt mobil:
    - über eigenen Shell-Menüpunkt `Export`
    - zusätzlich über den Home-Verwaltungsbereich
- Bewusst nicht gemacht:
  - keine neue mehrstufige Exportplattform
  - keine breite Portierung aller denkbaren WPF-Exportarten in diesen Block
  - keine Änderung an bereits stabilisierten Login-/Mitglieds-/Garten-/Home-Pfaden außerhalb der nötigen Einbindung
- Ehrliche Restlückeneinordnung nach diesem Block:
  - kritisch:
    - aktuell keine neue kritische MAUI-Sackgasse mit demselben Gewicht wie zuvor bei Mitglieds-/Garten-/Home-Kontext identifiziert
  - wichtig:
    - tiefere WPF-Editorparität in mobilen Verwaltungsseiten (Validierung, Fokusführung, mehr Feldtiefe)
    - weitere Exportarten, falls im WPF-Alltag mehr als Mitglieder-CSV fachlich relevant genutzt werden
    - Restparität einzelner Admin-/Kontextpfade außerhalb des jetzt geschlossenen Kerns
  - später:
    - UI-/Komfortausbau des mobilen Exportpfads (z. B. zusätzliche Formate oder differenziertere Auswahl)
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Home und mobile Verwaltungszugänge für Bekanntmachungen, Termine und Arbeitseinsätze auf echten Shared-Pfad gezogen

- Vor dem Block wieder den realen Repo-/Arbeitsbaumstand sowie den aktuellen ausführlichen Fortschrittslog geprüft; blockfremde offene WPF-/Supabase-Dateien blieben bewusst unangetastet.
- Ausgangszustand vor diesem Block:
  - MAUI-`HomePage` zeigte bisher nur Schnellzugriffe, operative Hinweise und eine reduzierte Bekanntmachungsdetailanzeige direkt auf der Startseite
  - `Arbeitseinsätze` und `Termine` aus `HomeOverviewDTO` wurden mobil noch gar nicht als eigener Home-Pfad genutzt
  - für `Bekanntmachungen`, `Termine` und `Arbeitseinsätze` fehlten mobil weiterhin echte Verwaltungsoberflächen trotz vorhandener Shared-Service-CRUD-Pfade
- Den WPF-/MAUI-Abgleich für diesen Block auf größten Nutzen mit kleinem Risiko fokussiert:
  - Home-Details nicht mehr inline, sondern in eigener Seite
  - echte mobile Verwaltungszugänge für die drei Bereiche
  - Wiederverwendung der bestehenden Shared-Servicepfade statt neuer Schattenlogik
- Dazu den MAUI-Home-Pfad gezielt nachgezogen:
  - `HomeViewModel` nutzt jetzt neben Schnellzugriffen/Hint-Blöcken auch die vorhandenen Home-Daten für `Arbeitseinsätze`, `Termine` und `Bekanntmachungen`
  - `HomePage` zeigt diese drei Listen jetzt mobil sichtbar an
  - Auswahl läuft nicht mehr in eine Inline-Detailspalte, sondern über neuen echten Detailpfad `HomeSectionDetailPage`
- Neuer mobiler Home-Detailpfad ergänzt:
  - kleiner `HomeContextState` hält den aktuell ausgewählten Home-Eintrag
  - `HomeSectionDetailPage` zeigt Details für `Arbeitseinsatz`, `Termin` und `Bekanntmachung`
  - bei `Arbeitseinsatz` bleibt der produktive Anmeldepfad mobil nutzbar
  - für Admin/Vorstand werden beim Arbeitseinsatz zusätzlich Teilnehmer über den bestehenden Shared-Servicepfad geladen
- Mobile Verwaltungsoberfläche für die drei Bereiche als ein kompakter gemeinsamer Block ergänzt:
  - neue `HomeManagementPage`
  - Auswahl der Module `Arbeitseinsätze`, `Termine`, `Bekanntmachungen`
  - Listen laden jeweils direkt aus den vorhandenen Shared-Servicepfaden
  - Speichern läuft direkt über:
    - `CreateArbeitseinsatzAsync(...)` / `UpdateArbeitseinsatzAsync(...)`
    - `CreateTerminAsync(...)` / `UpdateTerminAsync(...)`
    - `CreateBekanntmachungAsync(...)` / `UpdateBekanntmachungAsync(...)`
  - `HTML-Inhalt` für Bekanntmachungen ist mobil damit auf einem echten Bearbeitungspfad nutzbar
  - `Speichern` bleibt jeweils am Ende des Formularpfads
- Home stellt diese Verwaltungswege für Admin/Vorstand jetzt direkt erreichbar bereit:
  - `Arbeitseinsätze bearbeiten`
  - `Termine bearbeiten`
  - `Bekanntmachungen bearbeiten`
- Die Shell-Routen dafür wurden zentralisiert registriert, damit Admin- und User-Shell keine doppelten MAUI-Routen gegeneinander registrieren.
- Bewusst nicht gemacht:
  - keine neue Gesamtarchitektur für Home oder Verwaltung
  - kein vollständiger Nachbau aller WPF-Editorvalidierungen/Fokussteuerungen in diesem einen Block
  - keine Änderung an bereits stabilisierten Login-/Mitglieds-/Gartenkontextblöcken außerhalb der benötigten Home-Routen
- Fachliche Kurzvalidierung für diesen Block:
  - Home zeigt die neuen Listen-/Detaileinstiege
  - Admin/Vorstand erreicht mobil die Verwaltungsfunktionen für `Bekanntmachungen`, `Termine` und `Arbeitseinsätze`
  - bestehende Nutzerpfade wie Schnellzugriffe und Arbeitseinsatz-Anmeldung bleiben intakt
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Garten-/Parzellenkontext im Mitgliedspfad auf bestehenden Detailkern gezogen

- Vor dem Block wieder den echten Repo-/Arbeitsbaumstand und den aktuellen Fortschrittslog geprüft; blockfremde offene WPF-/Supabase-Dateien blieben erneut unangetastet.
- Ausgangszustand vor dem Umbau:
  - der mobile Mitgliedskontext zeigte Gärten/Parzellen bisher nur als Information
  - `Strom`, `Wasser` und Garten-Dokumente waren aus dem Mitgliedspfad noch nicht als echter Gartenkontext erreichbar
  - gleichzeitig existierte in MAUI bereits ein belastbarer Parzellen-Detailpfad (`ParzellenPage` + `ParzellenViewModel`) mit genau diesen Fachbereichen
- WPF-Referenzpfad geprüft:
  - im Desktop-Mitgliedskontext führen Garten-Unterpunkte auf `Strom`, `Wasser` und `Dokumente`
  - der mobile Block sollte deshalb keinen neuen Schattenpfad eröffnen, sondern den vorhandenen belastbaren MAUI-Parzellenkern kontextgebunden nutzbar machen
- Den Block bewusst klein und wiederverwendend umgesetzt:
  - neuer kleiner `ParzellenContextState` hält den aus dem Mitgliedskontext angeforderten Garten/Parzelle-Kontext
  - `MeineDatenPage` öffnet beim Tippen auf eine vorhandene Zuordnung jetzt den echten Gartenkontext statt nur Text anzuzeigen
  - der Wechsel läuft auf die bestehende `ParzellenPage`
  - `ParzellenViewModel` übernimmt den angeforderten Kontext, selektiert die gewünschte Parzelle automatisch und schaltet Titel/Beschreibung/Hinweis auf den Mitgliedskontext um
  - `ParzellenPage` zeigt im Kontextmodus direkt den bereits vorhandenen Fachkern mit:
    - `Strom`
    - `Wasser`
    - Garten-Dokumenten
  - zusätzlich kleiner Rückweg `Zur Parzellenübersicht`, damit die globale Parzellenverwaltung weiter nutzbar bleibt
- Wichtiger fachlicher Punkt:
  - Es wurde keine neue Platzhalterseite für Garten-Unterbereiche gebaut.
  - Der mobile Mitgliedspfad verwendet jetzt direkt die vorhandenen belastbaren Daten-/Servicepfade aus der Parzellenverwaltung für Zählerstatus, Ablesungen und Garten-Dokumente.
- Fachliche Kurzvalidierung für diesen Block:
  - Mitglied auswählen
  - in Mitgliedskontext wechseln
  - Garten/Parzelle antippen
  - `Strom`, `Wasser` und Garten-Dokumente im echten Gartenkontext erreichbar
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Mitgliedskontext, Admin-Menü und Mitgliedsdokumente auf belastbaren Kernpfad gezogen

- Den echten Stand vor dem Block wieder direkt im Repo/Arbeitsbaum geprüft: MAUI hatte nach der Branding-Korrektur weiter keinen echten mitgliedsbezogenen Detailpfad, `MemberSearchPage` endete nur mit `DisplayAlert`, `MeineDatenPage` und `DokumentePage` waren noch Platzhalter, und die mobile `Benutzerverwaltung` hing weiterhin lose global statt am Mitgliedskontext.
- WPF als Referenzpfad geprüft:
  - `MemberSearchViewModel` navigiert dort in `MemberDetailViewModel`
  - `DokumenteViewModel` arbeitet mit `GetMitgliedDokumenteAsync(...)` + `CreateDokumentSignedUrlAsync(...)`
  - `Benutzerverwaltung` ist im Mitgliedskontext gebunden und legt bei Bedarf einen Platzhalter-Appuser für Einladung/Erstlogin an
  - `Benutzerverwaltung` bleibt Admin-only, nicht Vorstand-only
- Den MAUI-Block deshalb klein, aber produktiv am bestehenden Fachkern umgesetzt:
  - neuer kleiner `MemberContextState` hält den ausgewählten Mitgliedskontext mobil über die Navigation hinweg
  - `MemberSearchPage` wechselt nach Auswahl jetzt nicht mehr nur in einen Alert, sondern setzt den Mitgliedskontext und navigiert in die vorhandene `MeineDatenPage`
  - die frühere Platzhalter-`MeineDatenPage` wurde in einen echten mobilen Mitgliedskontext umgebaut:
    - Mitgliedsgrunddaten
    - Kontakt/Adresse
    - Garten-/Parzellenzuordnungen
    - Dokumente-Einstieg
    - `Admin-Menü` im Mitgliedskontext
  - das `Admin-Menü` orientiert sich am WPF-Pfad:
    - sichtbar für Admin/Vorstand im ausgewählten Mitgliedskontext
    - Rolleninformation und Rollenwechsel über denselben Lock-/`UpdateMitgliedAsync(...)`-Pfad wie in WPF
    - `Benutzerverwaltung` nur für Admin sichtbar
  - die lose globale `Benutzerverwaltung` wurde aus der `AdminShell` entfernt und stattdessen nur noch als Kontextpfad-Routenziel verwendet
  - die frühere Platzhalter-`DokumentePage` wurde auf den realen Mitgliedsdokumente-Pfad mit `GetMitgliedDokumenteAsync(...)` und Signed-URL-Öffnung umgestellt
  - `UserManagementViewModel` / `UserManagementPage` wurden auf den ausgewählten Mitgliedskontext gebunden:
    - lädt nur noch den Appuser des ausgewählten Mitglieds
    - legt bei fehlender Zuordnung einen Platzhalter für Einladung/Erstlogin an
    - erlaubt kontextgebunden Einladung, Passwort-Reset, Rollenpflege und Entfernen des Appusers
- Bewusst nicht gemacht:
  - keine neue Gesamtarchitektur für Navigation
  - kein Garten-Dokumente-Endausbau im mobilen Mitgliedspfad
  - keine Änderung an Login-/Shell-Fixes außerhalb des für diesen Block nötigen Routing-/Menübezugs
- Fachlicher Stand nach diesem Block:
  - Admin/Vorstand kann mobil ein Mitglied auswählen
  - landet in einem echten Mitgliedskontext statt in einem Alert
  - sieht dort das `Admin-Menü`
  - Mitgliedsdokumente sind über einen echten Storage-Pfad erreichbar
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Branding auf echtes Vereinslogo gezogen und Startpfad ehrlich gegen WPF abgeglichen

- Vor dem Block den echten Arbeitsbaum erneut geprüft; der Stand ist weiterhin nicht synchron: viele blockfremde lokale Änderungen liegen offen, MAUI ist fachlich und UI-seitig noch nicht auf WPF-Niveau.
- Branding-Istzustand belastbar geprüft:
  - aktives MAUI-App-Icon kam bereits aus `KGV.Maui/Resources/AppIcon/appicon.svg`
  - aktiver Splash kam dagegen noch aus `KGV.Maui/Resources/Splash/splash.svg` und zeigte nur eine generische KGV-Platzhaltergrafik
  - `LoginPage` zeigte bislang gar kein Vereinslogo
  - zusätzlich liegen lokale/unversionierte Dubletten wie `KGV.Maui/Resources/AppIcon/appicon.png` und `Logo.png` im Arbeitsbaum; sie wurden für diesen Block nur als Befund geprüft, nicht zur neuen Grundlage gemacht
- Den Block klein und buildfähig direkt am aktiven MAUI-Pfad korrigiert:
  - `KGV.Maui.csproj` nutzt für den Splash jetzt dasselbe echte Vereinslogo wie für das App-Icon (`Resources/AppIcon/appicon.svg`)
  - dasselbe Logo wird zusätzlich als MAUI-Bildressource eingebunden, damit es auf der Loginseite sauber wiederverwendbar ist
  - `LoginPage` zeigt oben jetzt das Vereinslogo sichtbar an
  - der Start-/Login-Button `Anmelden` trägt das Logo ebenfalls links im Button und bleibt optisch kompakt statt überladen
- Bewusst nicht gemacht:
  - keine Neuarchitektur des Loginflows
  - keine Änderung an WPF
  - keine Übernahme unversionierter Dubletten in diesen Block
  - keine pauschale Behauptung, MAUI sei jetzt gleichauf mit WPF
- Ehrlicher Zwischenstand nach dem Vergleich:
  - WPF bleibt klar funktionaler und tiefer ausgebaut
  - MAUI hat belastbare Kernpfade für Login, Shell, Home-Basis, Parzellen-Kern, Arbeitsstunden, Ablesen und Benutzerverwaltung
  - MAUI fehlt aber weiterhin u. a. bei Export, memberbezogener Detailnavigation, Garten-Unterbereichen, Dokumentenpfaden, Home-Detail-/Verwaltungsansichten und mehreren Admin-/Kontextpfaden aus WPF
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Shell gegen leeren aktiven Einstieg gehärtet, Shell-Wechsel nach Login belastbar gemacht

- Den realen Repo-/Arbeitsbaumstand vor dem Block geprüft; blockfremde offene WPF-/Supabase-Dateien bewusst unangetastet gelassen.
- Den bereits begonnenen Block 1 im echten Stand validiert:
  - `LoginPage` lädt nach Login bereits den echten `UserContext`
  - die alte aktive Rollenwahl ist aus dem Loginflow entfernt
  - `AdminShell` richtet Rechte bereits am geladenen `UserContext` aus
- Der offene Restfehler lag damit im Shell-Startpfad von MAUI und nicht mehr in der Rollenwahl:
  - `AdminShell` / `UserShell` bauten Menüs zwar auf, härteten den aktiven `ShellItem`/`ShellSection`/`ShellContent` aber nicht belastbar nach
  - beim Wechsel auf wiederverwendete Shell-Instanzen bzw. bei späteren Sichtbarkeitsänderungen konnte der aktive Shell-Einstieg ungültig werden
  - genau dieser Zustand passt zum beobachteten Fehler `Active Shell Item not set`
- Den Shell-Block minimal-invasiv gehärtet:
  - gemeinsame Shell-Hilfe ergänzt, die immer einen gültigen sichtbaren Einstieg über `ShellItem` → `ShellSection` → `ShellContent` sicherstellt
  - `AdminShell` und `UserShell` rufen diese Absicherung jetzt nach dem Menüaufbau und zusätzlich beim Laden der Shell auf
  - `AdminShell.RefreshWorkhoursReviewMenuAsync()` stabilisiert nach Sichtbarkeitswechseln den aktiven Einstieg erneut, statt nur implizit auf den alten Zustand zu vertrauen
  - `LoginPage` baut die Ziel-Shell jetzt vollständig auf, sichert den aktiven Einstieg ab und wechselt erst dann die Window-Page; direkt nach dem Wechsel wird die Shell nochmals auf dem UI-Dispatcher nachgehärtet
  - `AdminShell` und `UserShell` werden in MAUI nicht mehr als Singleton wiederverwendet, sondern pro Wechsel frisch erzeugt, damit kein alter Shell-Zustand in einen neuen Login-/Restore-Pfad hineinragt
- Keine Neuarchitektur eingeführt:
  - keine neue Navigationsstruktur
  - kein neuer Rechtepfad
  - keine Änderung an WPF
  - keine Rückkehr zur alten `RoleChoicePage`
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – MAUI-Loginflow auf echten Benutzerkontext umgestellt, alte Rollenwahl aus aktivem Flow entfernt

- Den realen Repo-/Arbeitsbaumstand vor Block 1 geprüft; blockfremde offene Dateien bewusst unangetastet gelassen.
- WPF als Referenz geprüft: dort gibt es nach Login keine manuelle Rollenwahl, sondern direkt den echten `UserContext` mit anschließendem Rechtepfad.
- MAUI-Istzustand geprüft:
  - `LoginPage` leitete nach erfolgreichem Login noch in `RoleChoicePage` um oder nutzte einen gespeicherten `AppMode`
  - der Shell-/Startpfad hing damit nicht belastbar am echten Benutzerkontext
  - `AdminShell` stützte Teilrechte noch auf `IAuthService.IsAdmin` / `IsVorstand` statt auf den geladenen `UserContext`
- Block 1 gezielt umgesetzt:
  - alte Rollenwahl aus dem aktiven Loginflow entfernt
  - Shell-Auswahl nach Login jetzt direkt aus `IUserContextService.GetUserContextAsync(...)`
  - `UserContextState` wird nach Login mit echtem Rechtekontext befüllt
  - alter gespeicherter `AppMode` wird für diesen Flow geleert, damit keine frühere Rollenwahl mehr nachwirkt
  - `AdminShell` richtet Admin-/Vorstand-Menüs jetzt am geladenen `UserContext` aus
  - `RoleChoicePage` wurde zusätzlich aus dem aktiven DI-/Navigationspfad entfernt
- Keine WPF-Datei geändert; MAUI wurde minimal-invasiv an die WPF-Fachlogik angeglichen.
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 – Root-Folder-Pfad in `kgv-upload-photo` gezielt abgesichert und diagnostisch geschärft

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprüft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprüft:
  - `supabase/functions/kgv-upload-photo/index.ts` brach nach erfolgreicher eigener Auth aktuell im Schritt `root folder validate` ab
  - `supabase/config.toml` blieb unverändert bei `verify_jwt = false` für `kgv-upload-photo`
- Den Root-Folder-Pfad bewusst klein, aber belastbar geschärft:
  - `GOOGLE_DRIVE_ROOT_FOLDER_ID` wird jetzt sauber gelesen und getrimmt
  - harmlose Formatdiagnose loggt nur `hasValue`, Roh-/Trim-Länge und `trimmedEmpty`
  - der echte Secretwert selbst wird nicht geloggt
- Fehlverhalten jetzt klarer:
  - fehlend/leer => `Google Drive root folder id missing or empty`
  - nicht erreichbar/nicht nutzbar in Drive => schrittbezogene Root-Folder-Lookup-Fehler
- Zusätzliche Root-Folder-Logmarker ergänzt:
  - `root folder validate start`
  - `root folder validate result`
  - `root folder validate failed`
  - `drive root folder lookup start`
  - `drive root folder lookup success`
- Die Root-Folder-ID wird jetzt nicht nur lokal validiert, sondern vor dem Unterordnerbau einmal klein gegen Drive geprüft.
- Keine Änderung an Auth, Secrets, WPF, MAUI oder anderen Functions.
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo --project-ref itjcabiibuodkxayhvjq` erfolgreich

## 2026-03-24 – Diagnoseblock für `kgv-upload-photo` ergänzt: Hänger lokalisierbar, Google-Aufrufe mit Timeouts abgesichert

- Den realen Repo-/Arbeitsbaumstand vor dem Diagnoseblock geprüft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprüft:
  - `supabase/functions/kgv-upload-photo/index.ts` enthielt bereits die produktive Self-Auth und den Google-Drive-Uploadpfad
  - `supabase/config.toml` blieb korrekt bei `verify_jwt = false` für `kgv-upload-photo`
- Nur den Supabase-Diagnosepfad ergänzt, ohne WPF-/MAUI-Dateien oder Secrets anzufassen.
- In `kgv-upload-photo` gezielte Log-Marker ergänzt für:
  - Function-Start
  - Auth-Header erkannt / Auth-Start / Auth erfolgreich
  - Content-Type validiert
  - `formData()` Start / geparsed
  - Input normalisiert / validiert
  - Google-Token-Refresh Start / Erfolg
  - Ordneranlage je Segment Start / Erfolg
  - Drive-Upload Start / Erfolg
  - Success-/Error-Return
- Externe Google-Aufrufe jetzt fail-fast mit `AbortSignal.timeout(...)` abgesichert:
  - OAuth-Token-Refresh
  - Drive `files.list`
  - Drive-Ordner anlegen
  - Drive-Datei-Upload
- Timeout-/Fetch-Fehler liefern jetzt schrittbezogene Fehlermeldungen statt stillen Hängern, z. B. `Google token refresh timeout`, `Drive files.list timeout`, `Drive upload timeout`.
- Datumsinput klein robuster gemacht:
  - bisher nur `YYYY-MM-DD`
  - jetzt zusätzlich `dd.MM.yyyy`
  - ungültige Werte liefern eine klare Validierungsfehlermeldung
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo --project-ref itjcabiibuodkxayhvjq` erfolgreich

## 2026-03-24 â€“ doppelte JWT-PrÃ¼fung fÃ¼r `kgv-upload-photo` abgeschaltet

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprÃ¼ft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprÃ¼ft: `supabase/config.toml` enthielt fÃ¼r `kgv-upload-photo` bereits einen Funktionsblock, dort stand aber noch `verify_jwt = true`; der echte Self-Auth-Code lag bereits in `supabase/kgv-upload-photo/index.ts`.
- Wichtiger Repo-Befund: der eigentliche Deploy-Pfad `supabase/functions/kgv-upload-photo/index.ts` war noch ein Placeholder.
- Damit der Deploy die bestehende Funktionsauth nicht Ã¼berschreibt, wurde der vorhandene echte Function-Code gezielt in den echten Deploy-Pfad Ã¼bernommen.
- Danach in `supabase/config.toml` nur fÃ¼r `kgv-upload-photo` ergÃ¤nzt:
  - `[functions.kgv-upload-photo]`
  - `verify_jwt = false`
- Die eigene Function-Auth bleibt aktiv: Bearer-Token lesen, `auth.getUser(jwt)`, RollenprÃ¼fung auf Admin/Vorstand Ã¼ber `app_user.role`.
- Keine Secrets geÃ¤ndert, keine andere Function auf `verify_jwt = false` gesetzt, keine App-/WPF-/MAUI-Datei angefasst.
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo` erfolgreich


## 2026-03-24 â€“ WPF-Bindungsfehler in `FotoUploadTestView` bei schreibgeschÃ¼tzter Rohantwort behoben

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprÃ¼ft; blockfremde offene Dateien bewusst nicht angefasst.
- Ursache geprÃ¼ft: `RawResponseBody` ist im WPF-ViewModel ein reines Anzeige-/Diagnosefeld ohne Setter, wurde in `FotoUploadTestView.xaml` aber an `TextBox.Text` ohne expliziten Modus gebunden.
- Da `TextBox.Text` in WPF standardmÃ¤ÃŸig `TwoWay` bindet, entstand beim Ã–ffnen/Benutzen der DiagnoseflÃ¤che die bekannte Exception zur schreibgeschÃ¼tzten Eigenschaft.
- Fix bewusst klein und nur XAML-seitig umgesetzt:
  - `RawResponseBody` in `KGV.Wpf/Views/FotoUploadTestView.xaml` explizit auf `Mode=OneWay` gesetzt
  - `IsReadOnly="True"` blieb erhalten
- Kein kÃ¼nstlicher Setter im ViewModel, kein Umbau der Ã¼brigen Upload-Teststruktur, keine MAUI-Ã„nderung.
- Technisch verifiziert:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich


## 2026-03-24 Ã¢â‚¬â€œ Gemeinsamen RFID-Scan-Kontext fÃƒÂ¼r `Ablesung erfassen` und `ZÃƒÂ¤hlerwechsel` produktiv umgesetzt

- Vor dem Umbau erneut den realen Repo-/Arbeitsbaumstand geprÃƒÂ¼ft und blockfremde offene Dateien bewusst unberÃƒÂ¼hrt gelassen.
- Den gemeinsamen produktiven RFID-Lesepfad im Shared-Service ergÃƒÂ¤nzt:
  - neues Enum `RfidScanContextState`
  - neues Ergebnis-DTO `RfidScanContextResult`
  - neue Service-Methode `ResolveRfidScanContextAsync(...)`
  - UID-Normalisierung bleibt zentral im `SupabaseService`
  - produktiver Lesepfad ist `v_rfid_scan_context`
- `RfidScanContextRecord` am echten View-Vertrag belassen und nur um Anzeige-/Statushilfen ergÃƒÂ¤nzt.
- Fachliche Zustandsableitung jetzt zentral und fÃƒÂ¼r beide Clients gleich:
  - `Unknown`
  - `KnownWithActiveMeter`
  - `KnownWithoutActiveMeter`
- WPF:
  - vorhandenen Placeholder `RfidScanContextViewModel` / `RfidScanContextView` in echte gemeinsame RFID-Kontextanzeige umgebaut
  - `AblesungErfassenViewModel` und `ZaehlerwechselScanViewModel` auf produktive UID-Eingabe + KontextauflÃƒÂ¶sung umgestellt
  - Ergebnisanzeige mit Anlage, Garten, Medium, RFID, aktivem ZÃƒÂ¤hler, ZÃƒÂ¤hlernummer, Status, Eichdaten
  - workflow-spezifische Einordnung fÃƒÂ¼r Ablesung bzw. ZÃƒÂ¤hlerwechsel ergÃƒÂ¤nzt
- MAUI:
  - gemeinsames `RfidScanContextViewModel` und gemeinsame Basispage `RfidScanWorkflowPage` ergÃƒÂ¤nzt
  - `AblesungErfassenPage` und `ZaehlerwechselPage` auf denselben produktiven Kontextpfad umgestellt
  - manuelle UID-Eingabe, KontextauflÃƒÂ¶sung und workflow-spezifische Einordnung mobil nutzbar
- Rechte:
  - Bereich bleibt auf Admin/Vorstand begrenzt
  - keine QR- oder Schattenlogik eingefÃƒÂ¼hrt
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 Ã¢â‚¬â€œ `FÃƒÂ¤llige ZÃƒÂ¤hler` produktiv auf Basis von `v_zaehler_eichstatus` umgesetzt

- Vor Start den realen Repo-/Arbeitsbaumzustand erneut geprÃƒÂ¼ft; keine Recovery-Reste als fachliche Grundlage verwendet.
- `FÃƒÂ¤llige ZÃƒÂ¤hler` in WPF und MAUI war vorher nur als Placeholder vorhanden.
- Gemeinsamen Produktivpfad im Shared-Service ergÃƒÂ¤nzt:
  - neues Model `ZaehlerEichstatusRecord` fÃƒÂ¼r `v_zaehler_eichstatus`
  - neue Service-Methode `GetZaehlerEichstatusAsync()`
  - Sortierung zentral im Service: zuerst kritisch/fÃƒÂ¤llig, dann nach Tagen, dann Garten/Anlage
- WPF:
  - `FaelligeZaehlerViewModel` von Placeholder auf echte ÃƒÅ“bersicht umgestellt
  - Textfilter, Statusfilter und `Aktualisieren`
  - Tabelle mit Anlage, Garten, Medium, ZÃƒÂ¤hler, Eichdatum, EichfÃƒÂ¤lligkeit, Status und Tage
  - saubere Leer-/FehlerzustÃƒÂ¤nde
- MAUI:
  - `FaelligeZaehlerPage` auf produktive mobile ÃƒÅ“bersicht umgestellt
  - gleiches Datenmodell und gleicher Shared-Servicepfad
  - Textfilter, Statusfilter, `Aktualisieren`
  - mobile Kartenansicht statt breiter Tabelle
- Rechte explizit auf Admin/Vorstand gehalten.
- Technisch verifiziert:
  - keine passenden TestfÃƒÂ¤lle in `KGV.Tests` fÃƒÂ¼r diesen Block gefunden
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 Ã¢â‚¬â€œ `RFID einrichten` produktiv auf neuem DB-Vertrag umgesetzt

- Den aktuellen Repo- und Git-Stand vor dem Umbau erneut geprÃƒÂ¼ft.
- Blockfremde offene Dateien im Arbeitsbaum bewusst nicht angerÃƒÂ¼hrt; insbesondere offene WPF-UI-Dateien, lokale Artefakte, `_Archiv/_Recovery` und `_AI_DB_EXPORT/AI_DATABASE_CONTEXT.sql` blieben auÃƒÅ¸en vor.
- FÃƒÂ¼r die RFID-Zuordnung wurde der gemeinsame Produktivpfad im Shared-Service ergÃƒÂ¤nzt:
  - PrÃƒÂ¼fung und Speichern laufen jetzt zentral ÃƒÂ¼ber `ISupabaseService` / `SupabaseService`
  - produktiver Schreibpfad ist `assign_parzelle_rfid(...)`
  - KonfliktprÃƒÂ¼fung nutzt den realen DB-Bestand ÃƒÂ¼ber `parzelle` plus `v_rfid_scan_context`
  - UID wird konsistent getrimmt und in GroÃƒÅ¸buchstaben normalisiert
- `ParzelleRecord` wurde nur minimal auf den neuen DB-Vertrag erweitert:
  - `Anlage`
  - `hat_strom`
  - `hat_wasser`
  - `rfid_strom`
  - `rfid_wasser`
  - bestehende Altpfade auf getrennten Split-Tabellen wurden in diesem Block nicht zur neuen Grundlage gemacht
- WPF:
  - bestehende Seite `RFID einrichten` von Placeholder auf echte Maske umgestellt
  - Parzellenauswahl, aktuelle Strom-/Wasser-RFID, Medium, UID, `PrÃƒÂ¼fen`, `Speichern`
  - ÃƒÅ“berschreiben vorhandener RFID an derselben Parzelle nur nach expliziter BestÃƒÂ¤tigung
  - Konflikt bei bereits anderweitig vergebener UID wird klar blockiert
- MAUI:
  - mobile `RfidEinrichtenPage` auf denselben Shared-Servicepfad umgestellt
  - gleiche Fachschritte wie in WPF
  - ÃƒÅ“berschreib-BestÃƒÂ¤tigung mobil per Dialog
- Rechte:
  - Bereich bleibt explizit auf Admin/Vorstand begrenzt
  - keine Ãƒâ€“ffnung ÃƒÂ¼ber das zu breite Flag `CanManageReadings`
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 Ã¢â‚¬â€œ `Ablesen` als eigener Navigationsbereich fÃƒÂ¼r Admin/Vorstand in WPF und MAUI angelegt

- Den realen Istzustand vor dem Block geprÃƒÂ¼ft:
  - WPF hatte bereits vorbereitete ViewModels fÃƒÂ¼r `RfidEinrichten`, `FÃƒÂ¤llige ZÃƒÂ¤hler` und `ZÃƒÂ¤hlerwechsel`, aber noch keinen zentralen Einstiegsbereich `Ablesen`
  - MAUI hatte noch keinen eigenen Ablese-MenÃƒÂ¼punkt und keine ÃƒÅ“bersichtsseite fÃƒÂ¼r diese vier Funktionen
  - die bestehende Rollen-/Rechtebasis war bereits vorhanden (`UserContext.Role` in WPF, `IsAdmin` / `IsVorstand` im MAUI-Authpfad)
- Den neuen globalen MenÃƒÂ¼punkt `Ablesen` deshalb nur fÃƒÂ¼r Admin/Vorstand in beiden UIs ergÃƒÂ¤nzt.
- WPF:
  - neue ÃƒÅ“bersichtsseite `Ablesen` mit vier groÃƒÅ¸en Kacheln angelegt
  - Kacheln navigieren zu `Ablesung erfassen`, `ZÃƒÂ¤hlerwechsel`, `RFID einrichten`, `FÃƒÂ¤llige ZÃƒÂ¤hler`
  - vorhandene vorbereitete WPF-ViewModels fÃƒÂ¼r `ZÃƒÂ¤hlerwechsel`, `RFID einrichten` und `FÃƒÂ¤llige ZÃƒÂ¤hler` weiterverwendet
  - fÃƒÂ¼r `Ablesung erfassen` eine schlanke Platzhalterseite ergÃƒÂ¤nzt
- MAUI:
  - neuer Flyout-Eintrag `Ablesen` im Admin-/Vorstand-Shell-MenÃƒÂ¼
  - neue mobile ÃƒÅ“bersichtsseite mit vier gut tappbaren Kacheln angelegt
  - vier schlanke Zielseiten/Platzhalter fÃƒÂ¼r den weiteren Ablese-Ausbau registriert
- Der Block bleibt bewusst auf Navigation, Rechte und ÃƒÅ“bersichtsseite beschrÃƒÂ¤nkt; tiefe Fachflows folgen erst in den nÃƒÂ¤chsten BlÃƒÂ¶cken.

## 2026-03-24 Ã¢â‚¬â€œ Veralteten Remote-Snapshot um fachliche QR-Reste bereinigt

- Den aktuellen Repo-Stand zuerst geprÃƒÂ¼ft: fachliche QR-Reste existierten nach der Live-Bereinigung nur noch im veralteten Snapshot `supabase/migrations/20260323093513_remote_schema.sql`.
- Gegen den bereits bereinigten Istzustand aus `_AI_DB_EXPORT/database.types.ts` abgeglichen: `public.parzelle` enthÃƒÂ¤lt dort keine QR-Spalten mehr, sondern nur noch die RFID-Felder.
- Den veralteten Remote-Snapshot deshalb auf den aktuellen fachlichen Stand gezogen:
  - `qr_code_wasser` aus der `parzelle`-Tabellendefinition entfernt
  - `qr_code_strom` aus der `parzelle`-Tabellendefinition entfernt
  - die veralteten Unique-Constraints `parzelle_qr_code_wasser_key` und `parzelle_qr_code_strom_key` entfernt
- Ergebnis: im Repo verbleiben in diesem Snapshot keine fachlichen QR-Reste mehr; der Snapshot passt damit wieder zum bereinigten Live-/Types-Stand.

## 2026-03-24 Ã¢â‚¬â€œ WPF Button-HÃƒÂ¶hen projektweit auf kleine feste FÃƒÂ¤lle geprÃƒÂ¼ft und angehoben

- Nach dem Fix in der Parzellenzuordnung wurden die WPF-Buttons mit kleinen festen HÃƒÂ¶hen projektweit geprÃƒÂ¼ft.
- Alle expliziten WPF-ButtonhÃƒÂ¶hen unter `35` wurden auf `35` angehoben, damit Beschriftungen nicht mehr zu knapp sitzen.
- Konkret angepasst wurden die betroffenen Buttons in:
  - `GartenStromView`
  - `GartenWasserView`
  - `ArbeitsstundenView`
  - `ChangeEmailWindow`
  - `ResetPasswordWindow`
- Der Fix bleibt rein visuell; Fachlogik und Command-Pfade wurden nicht verÃƒÂ¤ndert.

## 2026-03-24 Ã¢â‚¬â€œ WPF `MemberDetailView`: Parzellenzuordnungs-Zeile leicht erhÃƒÂ¶ht

- Den aktuellen Arbeitsbaum vor dem UI-Fix geprÃƒÂ¼ft; blockfremde lokale Ãƒâ€žnderungen wie `KGV.Wpf/Views/LoginWindow.xaml` und `KGV.Wpf/Views/MemberDetailView.xaml.cs` bleiben weiter unberÃƒÂ¼hrt.
- Die zu kleine HÃƒÂ¶he lag nicht in der `MemberDetailView` selbst, sondern in der ausgelagerten `MemberParzellenSection` bei der Zeile fÃƒÂ¼r freie Parzelle, Startdatum und die beiden Aktionsbuttons.
- FÃƒÂ¼r den kleinen WPF-UI-Fix wurde nur diese Zeile leicht erhÃƒÂ¶ht:
  - Zeilen-`StackPanel` mit `MinHeight="36"`
  - Button-HÃƒÂ¶hen von `30` auf `35` erhÃƒÂ¶ht
- Damit passt die Beschriftung wieder sauber in die Buttons, ohne die restliche Detailansicht oder Fachlogik zu verÃƒÂ¤ndern.

## 2026-03-24 Ã¢â‚¬â€œ Appuser-Verwaltung ins `Admin-MenÃƒÂ¼` verschoben und an das ausgewÃƒÂ¤hlte Mitglied gebunden

- Den Istzustand zuerst im Repo geprÃƒÂ¼ft: `Benutzerverwaltung` hing in WPF noch als globale Navigation, obwohl die fachlichen Appuser-Aktionen immer auf ein konkretes Mitglied zielen sollen. Gleichzeitig war die produktive Nutzeraktion `InviteUserAsync(...)` bereits vorhanden, ein sauberer Entfernen-Pfad aber nicht als eigene UI-gebundene Aktion organisiert.
- Die MenÃƒÂ¼struktur wurde deshalb neu geordnet:
  - globaler Top-Level-Eintrag `Benutzerverwaltung` in WPF entfernt
  - stattdessen `Benutzerverwaltung` als eingerÃƒÂ¼ckter Unterpunkt im Mitgliedskontext unter `Admin-MenÃƒÂ¼`
  - damit bleibt die Navigation eindeutig und es gibt keine Doppelnavigation alt/neu
- Die WPF-`Benutzerverwaltung` arbeitet jetzt strikt auf dem zuvor ausgewÃƒÂ¤hlten Mitglied:
  - der Unterpunkt bekommt den aktuellen `SelectedMember` als Parameter
  - beim Laden wird nur noch die Appuser-Zuordnung dieses Mitglieds geladen
  - falls noch kein Appuser existiert, wird ein Platzhalter aus dem ausgewÃƒÂ¤hlten Mitglied aufgebaut, damit `Nutzer hinzufÃƒÂ¼gen` trotzdem sauber auf genau diesen Datensatz arbeitet
- Die Aktionen wurden fachlich passend umgestellt:
  - `Nutzer hinzufÃƒÂ¼gen` nutzt weiter den bestehenden produktiven Pfad `InviteUserAsync(...)`
  - `Nutzer entfernen` entfernt die Appuser-Zuordnung des ausgewÃƒÂ¤hlten Mitglieds, nicht das Mitglied selbst
  - ohne Mitgliedskontext laufen die Aktionen nicht und liefern eine klare fachliche RÃƒÂ¼ckmeldung statt technischer Fehler
- Rechte wurden sauber getrennt:
  - `Benutzerverwaltung` ist in WPF nur noch im Admin-Mitgliedskontext sichtbar
  - in MAUI wurde die MenÃƒÂ¼-Sichtbarkeit ebenfalls auf echte Admins beschrÃƒÂ¤nkt; Vorstand sieht den Punkt dort ebenfalls nicht
- Technisch verifiziert: `KGV.Wpf` baut nach der Umordnung erfolgreich; der MAUI-Pfad wurde im MenÃƒÂ¼-/Rechteverhalten mitgezogen und nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Mitgliedersuche um E-Mail, Gartennummern und Hauptmitglied-Markierung erweitert

- Den Istzustand zuerst im Repo geprÃƒÂ¼ft: die bestehende Mitgliedersuche zeigte bisher in WPF nur Name/Vorname und in MAUI nur Titel/Unterzeile. Ein neuer Suchdialog oder neue Fachlogik waren dafÃƒÂ¼r nicht nÃƒÂ¶tig.
- Die Erweiterung bleibt im bestehenden Suchpfad und nutzt nur bereits vorhandene Datenquellen:
  - Mitgliedsdaten aus `GetMitgliederAsync()`
  - Parzellen aus `GetAllParzellenAsync()`
  - aktuelle Belegungen aus `GetAllParzellenBelegungenAsync()`
- Daraus wird jetzt fÃƒÂ¼r die Ergebnisliste angereichert:
  - E-Mailadresse
  - Gartennummern aus aktuell aktiven Belegungen, falls vorhanden
  - Hauptmitglied-Markierung aus `hauptmitglied_id`
- WPF wurde direkt in der bestehenden GridView erweitert um die zusÃƒÂ¤tzlichen Spalten `E-Mail`, `Gartennummern` und `Hauptmitglied` als deaktivierte Checkbox.
- MAUI wurde passend mitgedacht und die bestehende Suchliste ebenfalls um Gartennummern und Hauptmitglied-Markierung ergÃƒÂ¤nzt; die E-Mail bleibt dort Teil der sichtbaren Ergebnisinfos. Es wurde kein zweiter Suchpfad aufgebaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Erweiterung erfolgreich.

## 2026-03-24 Ã¢â‚¬â€œ Arbeitseinsatz-Teilnehmerliste: `Abmelden` pro Zeile produktiv ÃƒÂ¼ber echten DB-Pfad ergÃƒÂ¤nzt

- Den Istzustand zuerst gegen Repo und `_AI_DB_EXPORT` geprÃƒÂ¼ft. `database.types.ts` bestÃƒÂ¤tigt fÃƒÂ¼r diesen Block ausdrÃƒÂ¼cklich neben `sign_up_for_arbeitseinsatz(...)` auch `sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` als echten DB-Funktionspfad; `roles.sql` enthÃƒÂ¤lt dafÃƒÂ¼r keine abweichende App-Sonderregel, `AI_DATABASE_CONTEXT.sql` liefert keinen alternativen Pfad.
- `sign_off_from_arbeitseinsatz(...)` war App-seitig vor diesem Block noch nicht produktiv verwendet. Der neue Abmeldepfad wurde deshalb sauber in den Shared-Service ergÃƒÂ¤nzt statt als direkter Tabellenhack daneben aufgebaut.
- In der Arbeitseinsatz-Detailview fÃƒÂ¼r Admin/Vorstand wurde pro Teilnehmerzeile der Button `Abmelden` ergÃƒÂ¤nzt. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch Zeilenaktion.
- Der Button lÃƒÂ¤uft mit kleiner RÃƒÂ¼ckfrage und ruft danach produktiv `SignOffFromArbeitseinsatzAsync(...)` auf; dieser Shared-Service nutzt den echten RPC-/DB-Funktionspfad `sign_off_from_arbeitseinsatz(...)`.
- Nach erfolgreicher oder fachlich abgefangener RÃƒÂ¼ckmeldung werden Teilnehmerliste und Detailzustand direkt neu geladen; damit aktualisieren sich auch KapazitÃƒÂ¤ts-/Anmeldeinformationen ÃƒÂ¼ber den bestehenden Detailpfad mit.
- Technisch verifiziert: `KGV.Wpf` baut nach dem neuen Abmeldepfad erfolgreich; MAUI wurde im Shared-Servicepfad mitgedacht und nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Arbeitseinsatz-Block final verifiziert und sauber abgeschlossen

- Den aktuellen Arbeitsbaum erneut geprÃƒÂ¼ft: blockeigene CodeÃƒÂ¤nderungen aus diesem Arbeitseinsatz-Block waren bereits im Repo enthalten; lokal offen blieben nur blockfremde Ãƒâ€žnderungen und Artefakte wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Hilfs-/Exportdateien.
- Den bereits implementierten Zustand nochmals final verifiziert:
  - `Anmelden` bleibt in Home und Detail am selben echten Pfad `SignUpForArbeitseinsatzAsync(...)`
  - der Shared-Service nutzt weiter produktiv `sign_up_for_arbeitseinsatz(...)`
  - Teilnehmerliste erscheint nur fÃƒÂ¼r Admin/Vorstand in der Detailview
  - `HinzufÃƒÂ¼gen` erscheint nur fÃƒÂ¼r Admin/Vorstand
  - `HinzufÃƒÂ¼gen` verwendet die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃƒÂ¤hlte bestehende Mitglied wird ÃƒÂ¼ber denselben RPC-Pfad eingetragen wie bei Selbstanmeldung
- Die Sichtbarkeit von `Anmelden` wird weiterhin belastbar aus realem Zustand bestimmt: Basisdaten aus `arbeitseinsatz` plus aktive `arbeitseinsatz_anmeldung` gegen Frist, Platzgrenze und bereits vorhandene Anmeldung des aktuellen Mitglieds.
- `KGV.Wpf` wurde final erfolgreich gebaut. Ein erster Buildlauf scheiterte nur an einer laufenden gesperrten `KGV.Wpf`-Instanz; nach Beenden der App lief der Build sauber durch. Der Block ist damit technisch und fachlich abgeschlossen.

## 2026-03-24 Ã¢â‚¬â€œ Arbeitseinsatz: `Anmelden`-Regression und Admin-/Vorstand-Teilnehmerblock sauber abgeschlossen

- Den begonnenen Block erneut gegen den realen Arbeitsbaum und ausdrÃƒÂ¼cklich gegen `_AI_DB_EXPORT` geprÃƒÂ¼ft. Belastbar herangezogen wurden wieder `database.types.ts` mit `sign_up_for_arbeitseinsatz(...)` und `arbeitseinsatz_anmeldung`; `roles.sql` enthÃƒÂ¤lt dafÃƒÂ¼r keine abweichende Zusatzregel, `AI_DATABASE_CONTEXT.sql` liefert hier keinen zusÃƒÂ¤tzlichen App-Pfad.
- Die Regression beim verschwundenen `Anmelden` lag nicht am RPC selbst, sondern an der Sichtbarkeit: `CanRegister` wurde im sichtbaren UI zu eng nur aus dem Startseitenpfad ÃƒÂ¼bernommen. Der Shared-Service bestimmt die sichtbare Anmeldung jetzt wieder aus den realen Basisdaten von `arbeitseinsatz` plus aktiven `arbeitseinsatz_anmeldung`-DatensÃƒÂ¤tzen fÃƒÂ¼r den aktuellen Benutzerkontext, inklusive Frist, Platzgrenze und bestehender Anmeldung.
- Home und Detail bleiben auf demselben echten Produktpfad: beide rufen weiterhin `SignUpForArbeitseinsatzAsync(...)` auf, das produktiv ÃƒÂ¼ber `sign_up_for_arbeitseinsatz(...)` schreibt. Es wurde keine zweite Schreiblogik erÃƒÂ¶ffnet.
- FÃƒÂ¼r Admin/Vorstand wurde die Detailview klein und rollenabhÃƒÂ¤ngig ergÃƒÂ¤nzt:
  - reale Teilnehmerliste ÃƒÂ¼ber aktive `arbeitseinsatz_anmeldung`
  - `HinzufÃƒÂ¼gen`-Button nur fÃƒÂ¼r Admin/Vorstand
  - Auswahl ausschlieÃƒÅ¸lich ÃƒÂ¼ber die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃƒÂ¤hlte bestehende Mitglied wird danach ÃƒÂ¼ber denselben RPC-Pfad angemeldet wie bei Selbstanmeldung
- Dabei gibt es bewusst keine freie Texteingabe und keine PrÃƒÂ¼fung, ob das gewÃƒÂ¤hlte Mitglied App-User ist. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch `HinzufÃƒÂ¼gen`.
- Technisch finalisiert: kleine Warnungsbereinigung im Auswahlpfad abgeschlossen und `KGV.Wpf` baut erfolgreich; der Block ist damit jetzt sauber abschlieÃƒÅ¸bar.

## 2026-03-24 Ã¢â‚¬â€œ `Anmelden` fÃƒÂ¼r Arbeitseinsatz im sichtbaren UI tatsÃƒÂ¤chlich funktionsfÃƒÂ¤hig gemacht

- Den realen Fehler ausdrÃƒÂ¼cklich am laufenden WPF-Pfad geprÃƒÂ¼ft: Home-Button und Detail-Button waren bereits an denselben Shared-Servicepfad gebunden, der sichtbare HÃƒÂ¤nger lag aber davor im ÃƒÂ¼bergebenen Datensatz.
- Die konkrete Ursache: `MapHomeWorkAssignment(...)` ÃƒÂ¼bernahm die `id` des Arbeitseinsatzes nicht in das sichtbare `HomeWorkAssignmentItem`. Dadurch lief die Detailansicht mit `WorkAssignmentId = 0` in einen stillen Vorab-Abbruch und auch der Home-Pfad arbeitete nicht auf einem belastbaren DatensatzschlÃƒÂ¼ssel.
- Der Fix bleibt klein und ehrlich:
  - `SupabaseService` schreibt `record.Id` jetzt in das sichtbare Home-Item
  - `HomeViewModel` ÃƒÂ¼bergibt den Detailbutton nur noch dann als sichtbar, wenn `CanRegister` tatsÃƒÂ¤chlich gilt
  - `HomeView.xaml` zeigt den Home-Button ebenfalls nur bei `CanRegister`
  - `HomeSectionDetailViewModel` meldet einen ungÃƒÂ¼ltigen Detailkontext jetzt sichtbar statt still zu returnen
- Ergebnis: Home und Detail nutzen weiterhin denselben RPC-Servicepfad, reagieren jetzt aber auch im sichtbaren UI belastbar statt mit stillem No-Op. Der Admin-/Vorstand-Teilnehmerblock blieb bewusst unberÃƒÂ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten UI-/Datensatzfix erfolgreich; MAUI wurde nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ RPC-Anmeldeblock fÃƒÂ¼r Arbeitseinsatz final verifiziert, committed und gepusht

- Den bereits umgesetzten RPC-Pfad fÃƒÂ¼r `Anmelden` nochmals final gegen den realen Arbeitsbaum geprÃƒÂ¼ft und `KGV.Wpf` erfolgreich gebaut.
- FÃƒÂ¼r den Abschluss wurden ausschlieÃƒÅ¸lich die zu diesem Block gehÃƒÂ¶renden Dateien gestaged; blockfremde lokale Ãƒâ€žnderungen wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Artefakte blieben unberÃƒÂ¼hrt.
- Kurz funktional verifiziert per Codepfad: Home und Detailview rufen denselben Shared-Service `SignUpForArbeitseinsatzAsync(...)` auf; Doppelanmeldung, Frist und Platzgrenze werden vor dem RPC geprÃƒÂ¼ft und zusÃƒÂ¤tzlich im RPC-Fehlerfall sauber in verstÃƒÂ¤ndliche RÃƒÂ¼ckmeldungen ÃƒÂ¼bersetzt.
- Der Block ist damit sauber als eigener Commit abgeschlossen und direkt gepusht.

## 2026-03-24 Ã¢â‚¬â€œ `Anmelden` fÃƒÂ¼r Arbeitseinsatz produktiv ÃƒÂ¼ber den echten DB-Funktionspfad angebunden

- Den Block ausdrÃƒÂ¼cklich gegen den referenzierten DB-Export `_AI_DB_EXPORT` gefÃƒÂ¼hrt. Belastbar genutzt wurden vor allem `database.types.ts` sowie die dort bestÃƒÂ¤tigten Funktionen `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` und `sign_off_from_arbeitseinsatz(...)`, auÃƒÅ¸erdem die Tabelle `arbeitseinsatz_anmeldung` und das Enum `arbeitseinsatz_anmeldung_status`. `roles.sql` wurde als Zusatzkontext geprÃƒÂ¼ft; dort liegen keine abweichenden App-seitigen Sonderregeln fÃƒÂ¼r diesen Block.
- Daraus den fachlich richtigen Schreibpfad abgeleitet: die App schreibt die Anmeldung nicht als Schatteninsert direkt in `arbeitseinsatz_anmeldung`, sondern nutzt produktiv die vorhandene DB-Funktion `sign_up_for_arbeitseinsatz(...)`. Der direkte Tabellenzugriff bleibt nur lesend fÃƒÂ¼r VorabprÃƒÂ¼fungen und Status-/KapazitÃƒÂ¤tsbewertung.
- Im Shared-Service wurde dafÃƒÂ¼r ein echter Produktpfad ergÃƒÂ¤nzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` prÃƒÂ¼ft zuerst sauber Mitglied, bestehenden Datensatz, aktive Anmeldungen, Frist und Platzgrenze und ruft dann den bestÃƒÂ¤tigten RPC-Pfad auf.
- Doppelanmeldungen werden dabei zweistufig verhindert: vor dem RPC ÃƒÂ¼ber eine gezielte Lesesicht auf `arbeitseinsatz_anmeldung` mit `status = angemeldet`, zusÃƒÂ¤tzlich ÃƒÂ¼ber den DB-Funktionspfad selbst. Falls der RPC in einem Rennen dennoch in einen bereits angemeldeten Zustand lÃƒÂ¤uft, wird das klein abgefangen und als verstÃƒÂ¤ndliche RÃƒÂ¼ckmeldung ausgegeben statt als technischer Rohfehler.
- Home und Detailview verwenden jetzt denselben echten Servicepfad statt doppelter ViewModel-Logik:
  - Home nutzt weiter `RegisterForWorkAssignmentCommand`, ruft aber jetzt den Shared-Service auf
  - die Detailansicht nutzt denselben Servicepfad ÃƒÂ¼ber den im Kontext mitgegebenen `WorkAssignmentId`
  - beide erhalten dieselbe verstÃƒÂ¤ndliche Erfolgs-/FehlerrÃƒÂ¼ckmeldung
- Der Mitgliedsbezug bleibt beim real vorhandenen Benutzerpfad: bevorzugt wird `UserContext.MitgliedId`, mit kleinem Fallback ÃƒÂ¼ber `EnsureCurrentMemberSelectedAsync()`; es wurde keine neue Sonderzuordnung eingefÃƒÂ¼hrt.
- Nach erfolgreicher Anmeldung wird der sichtbare Zustand klein aktualisiert: der betroffene Home-Eintrag bzw. der Detailkontext erhÃƒÂ¤lt den aktualisierten Startseiten-Datensatz zurÃƒÂ¼ck und der Button wird ehrlich deaktiviert, damit keine zweite sofortige Anmeldung ausgelÃƒÂ¶st wird. Eine Abmeldung oder Warteliste wurde bewusst nicht neu erÃƒÂ¶ffnet.
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde im Shared-Core-/Servicepfad mitgedacht und nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Home-Zeitwerte fÃƒÂ¼r Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Sichtbarkeitsfehler nach dem expliziten WPF-Binding nochmals gegen den kompletten sichtbaren Pfad geprÃƒÂ¼ft. Ergebnis: die WPF-Views waren bereits korrekt auf Start-/Endzeit gebunden; der Restfehler saÃƒÅ¸ davor im Home-Lesepfad, weil `v_startseite_arbeitseinsatz` bzw. `v_startseite_termine` die Zeitfelder im aktuellen Stand nicht in jedem Fall belastbar lieferten.
- Der finale Fix bleibt deshalb klein und produktnah im bestehenden Home-Servicepfad: `LoadStartseiteArbeitseinsaetzeAsync()` und `LoadStartseiteTermineAsync()` laden die Startseiten-Records weiter aus den bestehenden Views, reichern fehlende `Beginn`-/`Ende`-Werte bei Bedarf aber gezielt aus den Basistabellen `arbeitseinsatz` bzw. `termin` anhand der bereits vorhandenen Datensatz-`Id` an.
- Damit ist jetzt belastbar abgesichert: `start_uhrzeit` und `end_uhrzeit` kommen nicht nur in der Basistabelle vor, sondern erreichen den tatsÃƒÂ¤chlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lÃƒÂ¤sst. Die sichtbare Bindung in Home und Detail kann damit auf denselben expliziten Start-/Endfeldern bleiben.
- Keine neue Fachlogik, keine neue Navigation und keine Home-Schattenarchitektur: es bleibt beim vorhandenen Home-Lesepfad plus kleiner Fallback-Anreicherung nur fÃƒÂ¼r fehlende Zeitwerte.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-ErgÃƒÂ¤nzung nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Start- und Endzeit im tatsÃƒÂ¤chlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler nochmals Ende-zu-Ende gegen den tatsÃƒÂ¤chlichen WPF-Pfad geprÃƒÂ¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml`. Ergebnis: `start_uhrzeit`/`end_uhrzeit` kamen im Mapping als normalisierte Werte an, hingen im sichtbaren UI aber weiterhin zu stark an einem zusammengesetzten Zeittext bzw. indirekten Anzeigeweg.
- Die endgÃƒÂ¼ltige Korrektur setzt deshalb nicht mehr nur auf einen abstrakten Sammelstring, sondern auf explizite Start-/Endzeit-Properties im tatsÃƒÂ¤chlich sichtbaren Pfad: `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `StartTimeText` und `EndTimeText`; der Detailkontext ÃƒÂ¼bernimmt dieselben Felder 1:1 fÃƒÂ¼r genau den ausgewÃƒÂ¤hlten Datensatz.
- In `SupabaseService` werden die vorhandenen `Beginn`-/`Ende`-Werte weiterhin zentral ÃƒÂ¼ber `NormalizeTimeValue(...)` normalisiert und jetzt explizit in diese Start-/Endfelder geschrieben. Damit ist belastbar nachvollziehbar: die Zeitwerte kommen im Modell an und werden nicht mehr erst spÃƒÂ¤t oder indirekt aus Nebenfeldern zusammengesetzt.
- Der sichtbare WPF-Pfad wurde anschlieÃƒÅ¸end passend darauf verdrahtet: auf Home bleibt die Zeitzeile als klarer Zeitraum aus genau dieser Quelle sichtbar; in der Detailansicht werden `Beginn` und `Ende` jetzt explizit als eigene Datensatzangaben gerendert statt nur als zusammengesetzte Sammelzeile.
- `Bekanntmachung` bleibt unverÃƒÂ¤ndert stabil: dort werden die neuen Zeitfelder leer ÃƒÂ¼bergeben, sodass keine Regression in der generischen Detailstruktur entsteht.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/KontextergÃƒÂ¤nzungen nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Home-/Detailanzeige fÃƒÂ¼r Arbeitseinsatz und Termin korrigiert: Uhrzeit explizit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den aktuellen Istzustand des bestehenden Anzeige-/Mappingpfads erneut gegen `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und `SupabaseService` geprÃƒÂ¼ft. Ergebnis: die Uhrzeit lief bislang teils nur indirekt im `Subtitle` bzw. als separater Beginn-Text, wÃƒÂ¤hrend `Arbeitseinsatz`-KapazitÃƒÂ¤tsinfos zugleich in `DetailInfo` und `RegistrationInfo` landeten.
- Die Home-Anzeige wurde deshalb auf einen eindeutigen Sichtpfad umgestellt: `Arbeitseinsatz` und `Termin` erhalten jetzt jeweils einen expliziten `TimeText`, der ÃƒÂ¼ber `BuildTimeRange(...)` aus Start- und Endzeit erzeugt und in Home sichtbar als `Uhrzeit` gerendert wird. Damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, wird konsistent `Start Ã¢â‚¬â€œ Ende` angezeigt.
- Die Detailview nutzt denselben expliziten Zeitpfad jetzt ebenfalls fÃƒÂ¼r genau den ausgewÃƒÂ¤hlten Datensatz: `HomeSectionDetailContext` trÃƒÂ¤gt `TimeText`, `HomeSectionDetailViewModel` reicht ihn 1:1 weiter, und `HomeSectionDetailView.xaml` zeigt ihn separat als `Uhrzeit` an. Dadurch werden Start- und Endzeit fÃƒÂ¼r `Arbeitseinsatz` und `Termin` sichtbar, ohne neue Listendarstellung oder Sammelkontexte einzufÃƒÂ¼hren.
- Die doppelte Anzeige `Angemeldet: 0` kam aus zwei Quellen desselben Datensatzes: einmal aus `DetailInfo` (`Angemeldet`, `Freie PlÃƒÂ¤tze`) und zusÃƒÂ¤tzlich nochmals aus `RegistrationInfo` ÃƒÂ¼ber `BuildCapacityText(...)`. Der Mappingpfad fÃƒÂ¼r `Arbeitseinsatz` wurde deshalb bereinigt: KapazitÃƒÂ¤tsinfos bleiben jetzt nur noch in `RegistrationInfo`; aus `DetailInfo` wurden die redundanten `Angemeldet`-/`Freie PlÃƒÂ¤tze`-Zeilen entfernt.
- `DetailInfo` bleibt damit auf die ÃƒÂ¼brigen Felder des ausgewÃƒÂ¤hlten Datensatzes fokussiert (`Thema`, `Datum`, `Treffpunkt`, ggf. `Max. Teilnehmer`), wÃƒÂ¤hrend Registrierungs-/KapazitÃƒÂ¤tsinfos nur noch einmal sichtbar sind. So bleibt die Detailview weiter auf genau einen Datensatz begrenzt, aber ohne redundante Anzeige-Fragmente.
- Die generische Detailstruktur fÃƒÂ¼r `Bekanntmachung` wurde mitgeprÃƒÂ¼ft und nicht verschlechtert: dort bleibt `TimeText` leer, wÃƒÂ¤hrend `Subtitle`, `AdditionalInfo` und `Content` unverÃƒÂ¤ndert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fix erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Home-/Detailansicht fÃƒÂ¼r ArbeitseinsÃƒÂ¤tze und Termine vervollstÃƒÂ¤ndigt: Startzeit sichtbar, Detailinfos vollstÃƒÂ¤ndig, Detailkontext bleibt beim ausgewÃƒÂ¤hlten Datensatz

- Den bestehenden Home-/Detailpfad vor dem Fix gegen die realen WPF-Dateien und das aktuelle Mapping geprÃƒÂ¼ft: `HomeView`, `HomeViewModel`, `HomeSectionDetailView`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Items sowie das Mapping in `SupabaseService` waren bereits generisch vorhanden; `Arbeitseinsatz` zeigte die Startzeit auf Home schon separat, `Termin` dagegen noch nicht.
- Die Home-ÃƒÅ“bersicht wurde deshalb gezielt vervollstÃƒÂ¤ndigt, ohne neue Sondernavigation zu bauen: `Termin` zeigt die Startzeit jetzt wie `Arbeitseinsatz` zusÃƒÂ¤tzlich sichtbar in der Karte an. FÃƒÂ¼r `Arbeitseinsatz` blieb die vorhandene konsistente Darstellung mit sichtbarem Beginn erhalten.
- Die Detailview wurde innerhalb der bestehenden Struktur ergÃƒÂ¤nzt statt neu aufgebaut: zusÃƒÂ¤tzlich zu `Title`, `Subtitle`, `AdditionalInfo` und `Content` zeigt sie jetzt auch die bereits aus dem gewÃƒÂ¤hlten Datensatz stammende Registrierungsinformation an. Dadurch bleiben fÃƒÂ¼r `Arbeitseinsatz` und `Termin` die restlichen zum Datensatz gehÃƒÂ¶renden Angaben sichtbar, ohne Felder aus anderen EintrÃƒÂ¤gen zu vermischen.
- Den Punkt Ã¢â‚¬Å¾nur ausgewÃƒÂ¤hlten Datensatz anzeigenÃ¢â‚¬Å“ explizit auf den bestehenden Kontextpfad abgesichert: `HomeViewModel` ÃƒÂ¼bergibt an die Detailansicht weiterhin nur skalare Kontextdaten des konkret angeklickten Eintrags (`Title`, `Subtitle`, `Content`, `AdditionalInfo`, Registrierungsinfo/Anmeldeaktion). Es wird keine Liste und kein unscharfer Sammelkontext an die Detailview gereicht.
- Der `Anmelden`-Button ist in der Detailview jetzt konsistent fÃƒÂ¼r `Arbeitseinsatz` sichtbar und nutzt denselben ehrlichen Pfad wie auf Home: derselbe Hinweisdialog, keine neue Fake-Fachlogik und weiterhin kein erfundener Schreibzugriff, solange der echte belastbare Anmeldepfad im Repo fehlt.
- `Bekanntmachung` wurde im selben generischen Detailpfad mitgeprÃƒÂ¼ft: die bestehende Darstellung ÃƒÂ¼ber `Title`, `Subtitle`, `AdditionalInfo` und `Content` bleibt erhalten; es geht dort kein Feld verloren und es wurde keine kaputte Spezialdarstellung eingefÃƒÂ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die gezielten Shared-/Kontextanpassungen nicht beschÃƒÂ¤digt.

## 2026-03-24 Ã¢â‚¬â€œ Arbeitsstunden endgÃƒÂ¼ltig gegen `id = 0` abgesichert, Save-RÃƒÂ¼cknavigation nachgezogen, Arbeitseinsatz-Home-Buttons korrigiert

- Den erneuten realen Fehlernachweis `arbeitsstunde.id = 0` nach dem vorigen Fix ausdrÃƒÂ¼cklich als Beleg genommen, dass Attributverhalten allein im laufenden Pfad nicht reicht. Deshalb den kompletten Produktpfad nochmals bis zur tatsÃƒÂ¤chlichen Create-Payload geprÃƒÂ¼ft: WPF-Erfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogpfad (`ArbeitsstundenViewModel`), MAUI-Erfassung (`MyArbeitsstundenPage`) und `SupabaseService.AddArbeitsstundeAsync(...)`.
- Die robuste Korrektur liegt jetzt nicht mehr nur im Record-Attribut, sondern technisch im Insertmodell selbst: fÃƒÂ¼r `arbeitsstunde` gibt es nun einen expliziten Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`), und `AddArbeitsstundeAsync(...)` schreibt beim Insert nur noch ÃƒÂ¼ber genau diesen Typ. Damit kann `Id` Ã¢â‚¬â€œ auch wenn aufrufende `ArbeitsstundeRecord`-Instanzen lokal weiter mit Default `0` ankommen Ã¢â‚¬â€œ technisch nicht mehr in die Create-Payload gelangen.
- Als zusÃƒÂ¤tzliche Guard-Absicherung ÃƒÂ¼bernimmt der Create-Pfad die `Id` grundsÃƒÂ¤tzlich ÃƒÂ¼berhaupt nicht mehr. Es wird weder auf positive noch auf nichtpositive `Id` vertraut; Updates bleiben weiterhin separat ÃƒÂ¼ber `UpdateArbeitsstundeAsync(...)` mit echter bestehender `Id` bestehen. Eine App-seitige `lastrow+1`-Logik wurde bewusst nicht eingefÃƒÂ¼hrt.
- Die Arbeitsstunden-Erfassungsansicht wurde im Abschlusszug ebenfalls auf das gewÃƒÂ¼nschte Produktverhalten gezogen: nach erfolgreichem Speichern bleibt die Eingabemaske nicht mehr offen stehen, sondern verlÃƒÂ¤sst den Pfad sauber. Im Dialogkontext schlieÃƒÅ¸t sich das Fenster wie bisher; im normalen WPF-Erfassungspfad wird nach erfolgreichem Save zurÃƒÂ¼ck auf `Home` navigiert.
- Die Arbeitsstunden-Freigabeansicht zieht jetzt dasselbe Abschlussverhalten nach: wenn alle markierten/geÃƒÂ¤nderten Zeilen erfolgreich gespeichert wurden, wird nicht in der PrÃƒÂ¼fansicht verharrt, sondern wieder sauber auf `Home` zurÃƒÂ¼cknavigiert. Der bestehende Fachpfad bleibt unverÃƒÂ¤ndert; `status` bleibt optionales Anmerkungsfeld.
- Den Home-Eintrag fÃƒÂ¼r `Arbeitseinsatz` auf den gewÃƒÂ¼nschten Interaktionsstand korrigiert: der separate `Details`-Button wurde entfernt, DetailÃƒÂ¶ffnung lÃƒÂ¤uft jetzt per Doppelklick auf die Karte. Stattdessen bleibt dort der Button `Anmelden` sichtbar. Weil weiterhin kein belastbarer produktiver Schreibpfad fÃƒÂ¼r die echte Anmeldung vorhanden ist, bleibt dieser Button bewusst eine ehrliche Info-Aktion statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschÃƒÂ¤digt.

## 2026-03-23 Ã¢â‚¬â€œ Zwei echte Arbeitsstundenfehler behoben: `id = 0` bei Neuanlage unterbunden und Freigabe-Checkbox aktiviert Speichern wieder sofort

- Den realen Produktfehler im Arbeitsstunden-Insertpfad diesmal bewusst bis zur tatsÃƒÂ¤chlich laufenden Payload verfolgt statt nur den Service isoliert zu betrachten: WPF-Usererfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogerfassung (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) erzeugen alle neue `ArbeitsstundeRecord`-Instanzen ohne explizite `Id`-Zuweisung, damit aber typbedingt mit lokalem Default `Id = 0`.
- Die eigentliche Ursache lag im Modellvertrag von `ArbeitsstundeRecord`: anders als bei den ÃƒÂ¼brigen produktiven Create-Modellen stand dort noch `[PrimaryKey("id")]` ohne explizites `shouldInsert = false`. Im aktuell laufenden Paketstand `Supabase.Postgrest 4.1.0` konnte die PrimÃƒÂ¤rschlÃƒÂ¼sselspalte dadurch im realen Insertpfad weiter in die Payload gelangen; genau so lieÃƒÅ¸ sich der reproduzierte neue Datensatz mit `id = 0` erklÃƒÂ¤ren.
- Den Fix deshalb am tatsÃƒÂ¤chlichen Fehlerursprung gesetzt: `ArbeitsstundeRecord` verwendet jetzt wie die ÃƒÂ¼brigen produktiven Create-Modelle explizit `[PrimaryKey("id", false)]`. Damit bleibt die `Id` fÃƒÂ¼r Update-/PrimaryKey-Zwecke vorhanden, wird bei Neuanlagen aber nicht mehr in die Insert-Payload aufgenommen.
- Die zweite reale StÃƒÂ¶rung in `Arbeitsstunden freigeben` ebenfalls bis zur konkreten UI-Ursache zurÃƒÂ¼ckgefÃƒÂ¼hrt: `status` war fachlich bereits optional und wurde in `HasChanges` nicht als Pflichtfeld behandelt. Das Problem lag stattdessen an der WPF-Spalte `DataGridCheckBoxColumn` fÃƒÂ¼r `Freigeben`; deren Wert wurde im laufenden UI-Pfad nicht zuverlÃƒÂ¤ssig sofort in das Item zurÃƒÂ¼ckgeschrieben, sodass `PropertyChanged`, `HasPendingChanges` und damit `SpeichernCommand` beim bloÃƒÅ¸en Setzen der Checkbox nicht unmittelbar anzogen.
- Den Fix daher klein und UI-seitig gehalten: die Checkboxspalte lÃƒÂ¤uft jetzt als `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`. Damit gilt schon das reine Setzen von `Freigeben` sofort als speicherrelevante Ãƒâ€žnderung.
- Fachregeln bleiben unverÃƒÂ¤ndert: offen bleibt `freigegeben = false`, `status` bleibt nur optionales Anmerkungsfeld, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`, und es wurde keine neue Freigabearchitektur oder App-seitige `lastrow+1`-Logik eingefÃƒÂ¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im selben Shared-Pfad mitgedacht und nicht beschÃƒÂ¤digt.

## 2026-03-23 Ã¢â‚¬â€œ WPF-Bindingfix in `ArbeitsstundenErfassungView`: `CurrentMemberText` als reine OneWay-Anzeige verdrahtet

- Den aktuellen Bindingfehler in `ArbeitsstundenErfassungView` gezielt auf die einzige Bindungsstelle von `CurrentMemberText` zurÃƒÂ¼ckgefÃƒÂ¼hrt: der Wert dient fachlich nur der Anzeige des aktuellen Mitgliedskontexts.
- Deshalb bewusst **keinen** Setter in `ArbeitsstundenErfassungViewModel` ergÃƒÂ¤nzt und keine Fachlogik verÃƒÂ¤ndert: `CurrentMemberText` bleibt eine reine Anzeigeeigenschaft.
- Die Anzeige in `ArbeitsstundenErfassungView.xaml` wurde minimal und robust auf explizite OneWay-Darstellung umgestellt: statt `Run` wird der Wert jetzt in einem eigenen `TextBlock` mit `Mode=OneWay` angezeigt.
- Die benachbarten Anzeige-`TextBlock`s bleiben strukturell intakt; es wurde nur der kleine Anzeigeabschnitt fÃƒÂ¼r das Mitglied ersetzt, ohne weitere Formularlogik oder ViewModel-VertrÃƒÂ¤ge anzufassen.
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Der kleine Bindingfix ist damit sauber abgeschlossen; MAUI blieb unberÃƒÂ¼hrt.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Insert-Pfade auf explizite ID-Mitgabe geprÃƒÆ’Ã‚Â¼ft: produktive Neuanlagen senden aktuell keine feste ID aus der App

- Den aktuellen produktiven Insert-/Create-Stand zuerst gegen den realen Arbeitsbaum geprÃƒÆ’Ã‚Â¼ft statt auf Verdacht umzubauen: gesichtet wurden `SupabaseService`, die betroffenen Records/Modelle, die WPF-/MAUI-Neuanlagepfade fÃƒÆ’Ã‚Â¼r `arbeitsstunde`, `termin`, `bekanntmachung` und `arbeitseinsatz` sowie ein mÃƒÆ’Ã‚Â¶glicher Pfad fÃƒÆ’Ã‚Â¼r `arbeitseinsatz_anmeldung`.
- FÃƒÆ’Ã‚Â¼r `arbeitsstunde` den konkreten Verdacht am echten Produktpfad geprÃƒÆ’Ã‚Â¼ft: WPF (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) bauen neue `ArbeitsstundeRecord`-Instanzen ohne gesetzte `Id`; `AddArbeitsstundeAsync(...)` erstellt daraus wiederum ein neues Insert-Objekt ebenfalls ohne `Id`-Zuweisung und sendet nur die fachlichen Felder an PostgREST.
- FÃƒÆ’Ã‚Â¼r die drei Verwaltungs-Neuanlagen denselben Punkt abgesichert: die WPF-Editoren bauen im New-Mode zwar Arbeitsobjekte mit `Id = 0` als lokalem Defaultwert, aber die produktiven Service-Create-Pfade `CreateTerminAsync(...)`, `CreateBekanntmachungAsync(...)` und `CreateArbeitseinsatzAsync(...)` ÃƒÆ’Ã‚Â¼bernehmen diese `Id` gerade **nicht** in ihre tatsÃƒÆ’Ã‚Â¤chlichen Insert-Objekte. Gesendet werden dort nur die fachlichen Spalten; Updates laufen weiterhin separat ÃƒÆ’Ã‚Â¼ber die vorhandene Datensatz-`Id`.
- Einen echten produktiven Insert-Pfad fÃƒÆ’Ã‚Â¼r `arbeitseinsatz_anmeldung` gibt es im aktuellen Repo weiterhin nicht: der vorhandene `Anmelden`-Pfad auf Home ist nach wie vor nur eine ehrliche Info-Aktion ohne Schreibzugriff. Entsprechend war dort aktuell auch kein produktiver ID-Insertpfad zu korrigieren.
- ZusÃƒÆ’Ã‚Â¤tzlich den Library-Unterbau gegen den aktuellen Paketstand geprÃƒÆ’Ã‚Â¼ft: im verwendeten `Supabase.Postgrest`-Paket ist beim `PrimaryKey`-Attribut der Parameter `shouldInsert` standardmÃƒÆ’Ã‚Â¤ÃƒÆ’Ã…Â¸ig `false`. Damit wird die PrimÃƒÆ’Ã‚Â¤rschlÃƒÆ’Ã‚Â¼sselspalte auch beim verbleibenden Muster `[PrimaryKey("id")]` nicht automatisch in Insert-Payloads aufgenommen, solange sie nicht explizit gesetzt und bewusst anderweitig serialisiert wird.
- Ergebnis der PrÃƒÆ’Ã‚Â¼fung: im aktuellen produktiven App-Code lieÃƒÆ’Ã…Â¸ sich fÃƒÆ’Ã‚Â¼r die geprÃƒÆ’Ã‚Â¼ften Neuanlagepfade keine Stelle nachweisen, an der bei Inserts eine feste `id` oder konkret `id = 0` aus der App mitgesendet wird.
- Deshalb bewusst **kein** Vermutungsfix eingebaut: keine `lastrow+1`-Logik, keine unnÃƒÆ’Ã‚Â¶tige Schatten-Insertarchitektur und keine DB-MigrationsÃƒÆ’Ã‚Â¤nderung auf Verdacht. Der Block bleibt bei der belastbaren Analyse und Dokumentation des aktuell bereits korrekten Insert-Musters `Insert ohne feste ID, Update mit bestehender ID`.
- Repo-seitig zusÃƒÆ’Ã‚Â¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine versionierten SQL-/Migrationsdateien fÃƒÆ’Ã‚Â¼r die betroffenen Tabellen vor; eine DB-seitige Sequence-/Default-PrÃƒÆ’Ã‚Â¼fung war im Repo daher nicht weiter nachvollziehbar, wurde aber fÃƒÆ’Ã‚Â¼r diesen Block auch nicht benÃƒÆ’Ã‚Â¶tigt, weil auf App-Seite kein fehlerhafter ID-Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wird durch diesen Dokumentations-/Analyseblock nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-/Detail-Block fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze sauber abgeschlossen: Beginn sichtbar, Detaildaten vervollstÃƒÆ’Ã‚Â¤ndigt, Anmelden ehrlich nur informativ

- Den begonnenen Home-/Detail-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufzuziehen: bereits halbfertig vorhanden waren erweiterte Home-/Detailmodelle, ein vorbereiteter optionaler `Anmelden`-Pfad im Detailkontext und erste Anpassungen im `HomeViewModel`, aber die sichtbaren WPF-XAML-Schritte und der hintere Helper-Tail des `SupabaseService` waren noch nicht sauber abgeschlossen.
- Den Home-/Detailpfad fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` gemeinsam nachgezogen, damit die generische Detailansicht durch die Erweiterung keine Felder verliert: zusÃƒÆ’Ã‚Â¤tzliche Datensatzinformationen laufen jetzt konsistent ÃƒÆ’Ã‚Â¼ber `DetailInfo`/`AdditionalInfo`, statt nur bei ArbeitseinsÃƒÆ’Ã‚Â¤tzen Sonderwissen lokal in der View zu hinterlegen.
- `Arbeitseinsatz` zeigt auf Home jetzt den Beginn explizit sichtbar im Karteneintrag; zusÃƒÆ’Ã‚Â¤tzlich gibt es dort einen eigenen `Details`-Button und ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ nur wenn der Datensatz ÃƒÆ’Ã‚Â¼berhaupt anmeldbar markiert ist ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ einen `Anmelden`-Button.
- Die Home-Detailansicht zeigt fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` jetzt neben Titel/Untertitel auch die ÃƒÆ’Ã‚Â¼brigen im Datensatz belastbar vorhandenen Informationen wie Thema, Datum, Beginn, Ende, Treffpunkt sowie KapazitÃƒÆ’Ã‚Â¤ts-/Anmeldeinformationen; `Max. Teilnehmer` wird bewusst nur dann angezeigt, wenn sie aus den vorhandenen Datensatzwerten ÃƒÆ’Ã‚Â¼berhaupt belastbar ableitbar ist.
- `Termin`- und `Bekanntmachung`-Detailansichten wurden im selben Pfad mitkonsolidiert: zusÃƒÆ’Ã‚Â¤tzliche Felder wie Ort, Thema, Betreff, VerÃƒÆ’Ã‚Â¶ffentlichungszeitpunkt oder Kurztext werden jetzt ebenfalls ÃƒÆ’Ã‚Â¼ber denselben Detailkontext transportiert, statt in der generischen Detailview verloren zu gehen.
- Den halbfertig beschÃƒÆ’Ã‚Â¤digten Tail des `SupabaseService` sauber wieder geschlossen und dabei die kleinen Home-Helfer zentral konsolidiert (`AddDetailLine`, Datum-/Zeitformatierung, Textnormalisierung, Dokumentpfad-Helfer etc.), damit der angefangene Block wieder auf einem kompilierbaren produktiven Stand steht.
- Den `Anmelden`-Usecase bewusst **nicht** als neue Schattenlogik erÃƒÆ’Ã‚Â¶ffnet: im aktuellen Repo lieÃƒÆ’Ã…Â¸ sich weiterhin kein belastbarer produktiver Schreibpfad fÃƒÆ’Ã‚Â¼r eine echte Arbeitseinsatz-Anmeldung finden. Deshalb ist der neue `Anmelden`-Button in WPF nur als ehrliche Info-Aktion verdrahtet und weist explizit darauf hin, dass der echte Schreibpfad noch nicht angebunden ist.
- WPF wurde sichtbar fertiggestellt; MAUI wurde bei den gemeinsamen Home-Items/Mappingerweiterungen mitgedacht, aber bewusst nicht mit einer neuen Arbeitseinsatz-AnmeldeoberflÃƒÆ’Ã‚Â¤che erweitert, solange dafÃƒÆ’Ã‚Â¼r kein belastbarer gemeinsamer Fachpfad existiert.
- Technisch verifiziert: Workspace-Build erfolgreich; der begonnene Home-/Detail-Block ist damit ohne erfundene Anmeldefachlogik sauber abgeschlossen.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren und Home-Navigation nachgezogen: ZurÃƒÆ’Ã‚Â¼ck-Text repariert, Doppelklick auf Listen, Arbeitsstunden-Erfassung nur noch ÃƒÆ’Ã‚Â¼ber Home

- Die drei WPF-Verwaltungseditoren `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen` nach dem Parserfix nochmals produktiv bereinigt: der beschÃƒÆ’Ã‚Â¤digte Toolbar-Text `ZurÃƒÆ’Ã‚Â¼ck` wird jetzt XAML-seitig stabil als Entity geschrieben, damit kein erneutes Zeichensatzproblem im Buttontext entsteht.
- FÃƒÆ’Ã‚Â¼r alle drei Verwaltungslisten einen echten Doppelklickpfad auf die ausgewÃƒÆ’Ã‚Â¤hlten EintrÃƒÆ’Ã‚Â¤ge ergÃƒÆ’Ã‚Â¤nzt: statt auf `InputBindings` zu vertrauen, ÃƒÆ’Ã‚Â¶ffnen die `ListBox`-EintrÃƒÆ’Ã‚Â¤ge jetzt direkt per `MouseDoubleClick` den vorhandenen `OeffnenCommand` und damit den Bearbeitungseditor zuverlÃƒÆ’Ã‚Â¤ssig.
- Die WPF-Hauptnavigation wieder auf den gewÃƒÆ’Ã‚Â¼nschten Produktpfad zurÃƒÆ’Ã‚Â¼ckgezogen: `Arbeitsstunden erfassen` wird nicht mehr als eigener Navigationseintrag aufgebaut und bleibt nur noch ÃƒÆ’Ã‚Â¼ber den vorhandenen Button auf der `Startseite` erreichbar.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÆ’Ã‚Â¤chliche XAML-Ursache zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÆ’Ã‚Â¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÆ’Ã‚Â¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÆ’Ã‚Â¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÆ’Ã‚Â¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÆ’Ã‚Â¼ck-/Dirty-Check ergÃƒÆ’Ã‚Â¤nzt, Navigation auf Home-only zurÃƒÆ’Ã‚Â¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÆ’Ã‚Â¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÆ’Ã‚Â¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÆ’Ã…Â¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÆ’Ã‚Â¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÆ’Ã‚Â¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÆ’Ã‚Â¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÆ’Ã‚Â¼r geladene VerwaltungsdatensÃƒÆ’Ã‚Â¤tze sowie fÃƒÆ’Ã‚Â¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÆ’Ã‚Â¼ckkehr erfolgt ÃƒÆ’Ã‚Â¼ber den vorhandenen Navigationspfad zurÃƒÆ’Ã‚Â¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÆ’Ã‚Â¼ck`-Button
  - `ZurÃƒÆ’Ã‚Â¼ck` und `Abbrechen` prÃƒÆ’Ã‚Â¼fen auf echte ungespeicherte ÃƒÆ’Ã¢â‚¬Å¾nderungen
  - die RÃƒÆ’Ã‚Â¼ckfrage erscheint nur bei tatsÃƒÆ’Ã‚Â¤chlichen ÃƒÆ’Ã¢â‚¬Å¾nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÆ’Ã‚Â¼nschten Produktpfad zurÃƒÆ’Ã‚Â¼ckgezogen:
  - `ArbeitseinsÃƒÆ’Ã‚Â¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÆ’Ã‚Â¼ber die vorhandenen Home-Buttons fÃƒÆ’Ã‚Â¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÆ’Ã‚Â¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÆ’Ã‚Â¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÆ’Ã‚Â¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÆ’Ã‚Â¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÆ’Ã‚Â¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÆ’Ã‚Â¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÆ’Ã‚Â¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÆ’Ã‚Â¼ber `status`; die globale PrÃƒÆ’Ã‚Â¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÆ’Ã‚Â¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÆ’Ã‚Â¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÆ’Ã‚Â¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÆ’Ã‚Â¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÆ’Ã‚Â¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÆ’Ã‚Â¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÆ’Ã‚Â¤chlich geÃƒÆ’Ã‚Â¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÆ’Ã‚Â¤tze bleiben unverÃƒÆ’Ã‚Â¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÆ’Ã‚Â¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÆ’Ã‚Â¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÆ’Ã‚Â¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÆ’Ã‚Â¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÆ’Ã‚Â¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim ÃƒÆ’Ã¢â‚¬â€œffnen der Freigabeansicht wird eine globale PrÃƒÆ’Ã‚Â¼fsperre ÃƒÆ’Ã‚Â¼ber die offenen `arbeitstunde`-DatensÃƒÆ’Ã‚Â¤tze gesetzt
  - andere PrÃƒÆ’Ã‚Â¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÆ’Ã‚Â¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÆ’Ã‚Â¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÆ’Ã‚Â¤ngert die Sperre wÃƒÆ’Ã‚Â¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÆ’Ã‚Â¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÆ’Ã‚Â¼bernehmbar
- Die bestehende Badge-/ZÃƒÆ’Ã‚Â¤hlerlogik bleibt weiterverwendet: ÃƒÆ’Ã¢â‚¬Å¾nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÆ’Ã‚Â¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÆ’Ã‚Â¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÆ’Ã‚Â¼r offene FÃƒÆ’Ã‚Â¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÆ’Ã‚Â¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÆ’Ã‚Â¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÆ’Ã‚Â¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÆ’Ã‚Â¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÆ’Ã‚Â¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÆ’Ã‚Â¼r `Datum`, `Stunden` und `Art der Arbeit` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ ohne zusÃƒÆ’Ã‚Â¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÆ’Ã‚Â¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÆ’Ã‚Â¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÆ’Ã‚Â¼r Benutzer mit eigenem Mitgliedskontext; zusÃƒÆ’Ã‚Â¤tzlich erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÆ’Ã‚Â¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÆ’Ã‚Â¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÆ’Ã‚Â¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸er als `0` sein.
- FÃƒÆ’Ã‚Â¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÆ’Ã‚Â¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÆ’Ã‚Â¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÆ’Ã‚Â¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÆ’Ã‚Â¤hler zeigt weiter die tatsÃƒÆ’Ã‚Â¤chliche Anzahl offener PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÆ’Ã‚Â¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÆ’Ã‚Â¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã‚Â¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÆ’Ã‚Â¤hrend im Repo noch die ÃƒÆ’Ã‚Â¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÆ’Ã‚Â¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÆ’Ã‚Â¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÆ’Ã‚Â¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÆ’Ã‚Â¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÆ’Ã‚Â¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÆ’Ã‚Â¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÆ’Ã…Â¸lich fÃƒÆ’Ã‚Â¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÆ’Ã‚Â¼r spÃƒÆ’Ã‚Â¤tere MAUI-ParitÃƒÆ’Ã‚Â¤t anschlussfÃƒÆ’Ã‚Â¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÆ’Ã‚Â¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÆ’Ã‚Â¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÆ’Ã‚Â¤hrend die eigentlichen EditoroberflÃƒÆ’Ã‚Â¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÆ’Ã‚Â¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÆ’Ã‚Â¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÆ’Ã‚Â¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ ArbeitseinsÃƒÆ’Ã‚Â¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÆ’Ã‚Â¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÆ’Ã‚Â¤tze-Verwaltung erneut geprÃƒÆ’Ã‚Â¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÆ’Ã‚Â¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÆ’Ã‚Â¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÆ’Ã‚Â¤nzt und nicht ÃƒÆ’Ã‚Â¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÆ’Ã‚Â¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÆ’Ã‚Â¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÆ’Ã‚Â¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÆ’Ã‚Â¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÆ’Ã‚Â¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÆ’Ã‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã‚Â¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÆ’Ã‚Â¤tze-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÆ’Ã‚Â¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã‚Â¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÆ’Ã‚Â¤nzt und nicht ÃƒÆ’Ã‚Â¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÆ’Ã‚Â¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÆ’Ã‚Â¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÆ’Ã‚Â¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÆ’Ã‚Â¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÆ’Ã…â€œberschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÆ’Ã‚Â¤nzt und parallel in einer kompakten Vorschau geprÃƒÆ’Ã‚Â¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÆ’Ã‚Â¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÆ’Ã‚Â¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÆ’Ã‚Â¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÆ’Ã‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã‚Â¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÆ’Ã‚Â¤chsten AufrÃƒÆ’Ã‚Â¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÆ’Ã‚Â¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã‚Â¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÆ’Ã…Â¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze direkt per Create/Update wieder nach `termin` zurÃƒÆ’Ã‚Â¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÆ’Ã‚Â¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÆ’Ã‚Â¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÆ’Ã‚Â¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÆ’Ã‚Â¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÆ’Ã‚Â¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÆ’Ã‚Â¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÆ’Ã‚Â¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÆ’Ã‚Â¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã‚Â¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÆ’Ã‚Â¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÆ’Ã‚Â¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÆ’Ã‚Â¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÆ’Ã…â€œbersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÆ’Ã‚Â¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÆ’Ã‚Â¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÆ’Ã‚Â¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÆ’Ã‚Â¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÆ’Ã‚Â¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÆ’Ã‚Â¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÆ’Ã‚Â¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÆ’Ã‚Â¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÆ’Ã‚Â¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÆ’Ã‚Â¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand verfÃƒÆ’Ã‚Â¼gbar und blÃƒÆ’Ã‚Â¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÆ’Ã‚Â¼r spÃƒÆ’Ã‚Â¤tere MAUI-ParitÃƒÆ’Ã‚Â¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÆ’Ã‚Â¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÆ’Ã‚Â¤tigt: `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` laden ÃƒÆ’Ã‚Â¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÆ’Ã‚Â¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÆ’Ã‚Â¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÆ’Ã…Â¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÆ’Ã‚Â¤tigte View.
- FÃƒÆ’Ã‚Â¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÆ’Ã‚Â¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÆ’Ã‚Â¼fung bleibt daher auf tatsÃƒÆ’Ã‚Â¤chliche RÃƒÆ’Ã‚Â¼ckgabefelder bzw. reale DatensÃƒÆ’Ã‚Â¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÆ’Ã‚Â¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÆ’Ã‚Â¤tzen, Terminen und Bekanntmachungen ÃƒÆ’Ã‚Â¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÆ’Ã‚Â¶ffnen in eigener View

- Den Home-Stand fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen auf den gewÃƒÆ’Ã‚Â¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÆ’Ã‚Â¼bersichten, wÃƒÆ’Ã‚Â¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÆ’Ã‚Â¼r eine eigenstÃƒÆ’Ã‚Â¤ndige WPF-Detailansicht fÃƒÆ’Ã‚Â¼r Home-Elemente ergÃƒÆ’Ã‚Â¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite ÃƒÆ’Ã¢â‚¬â€œffnen-Kommandos fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÆ’Ã‚Â¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÆ’Ã‚Â¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÆ’Ã‚Â¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÆ’Ã‚Â¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÆ’Ã‚Â¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÆ’Ã‚Â¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÆ’Ã‚Â¼ft. Die entscheidende technische Ursache fÃƒÆ’Ã‚Â¼r den Pflichtstundenfehler lieÃƒÆ’Ã…Â¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÆ’Ã‚Â¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÆ’Ã‚Â¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÆ’Ã‚Â¤hrend `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÆ’Ã‚Â¼r Home jetzt zunÃƒÆ’Ã‚Â¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÆ’Ã…Â¸end robust clientseitig ÃƒÆ’Ã‚Â¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÆ’Ã‚Â¼r Pflichtstunden ergÃƒÆ’Ã‚Â¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÆ’Ã‚Â¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÆ’Ã‚Â¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÆ’Ã‚Â¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÆ’Ã‚Â¼ckgelieferte EintrÃƒÆ’Ã‚Â¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÆ’Ã‚Â¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÆ’Ã‚Â¤nzt: der Home-Pfad schreibt jetzt fÃƒÆ’Ã‚Â¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÆ’Ã‚Â¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÆ’Ã‚Â¼ber Hauptmitglied/Saison prÃƒÆ’Ã‚Â¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÆ’Ã‚Â¼ft: da `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Pflichtstunden ÃƒÆ’Ã‚Â¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÆ’Ã‚Â¶sung und Bekanntmachungen ÃƒÆ’Ã‚Â¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÆ’Ã‚Â¼r Home deshalb final prÃƒÆ’Ã‚Â¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÆ’Ã‚Â¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÆ’Ã‚Â¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÆ’Ã‚Â¼bersicht ÃƒÆ’Ã‚Â¼ber dieses Hauptmitglied geladen.
- ZusÃƒÆ’Ã‚Â¤tzlich wird die bestÃƒÆ’Ã‚Â¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÆ’Ã‚Â¼cksichtigt, sondern zuerst direkt fÃƒÆ’Ã‚Â¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÆ’Ã‚Â¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÆ’Ã‚Â¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÆ’Ã‚Â¤tzlich mÃƒÆ’Ã‚Â¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÆ’Ã‚Â¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÆ’Ã‚Â¼r Titel, Inhalt und VerÃƒÆ’Ã‚Â¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÆ’Ã‚Â¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÆ’Ã‚Â¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÆ’Ã‚Â¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÆ’Ã‚Â¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÆ’Ã‚Â¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÆ’Ã‚Â¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÆ’Ã‚Â¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÆ’Ã‚Â¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÆ’Ã‚Â¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÆ’Ã‚Â¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÆ’Ã‚Â¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÆ’Ã‚Â¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÆ’Ã‚Â¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÆ’Ã‚Â¤tzlich vereinfacht und nÃƒÆ’Ã‚Â¤her auf den bestÃƒÆ’Ã‚Â¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÆ’Ã‚Â¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÆ’Ã‚Â¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÆ’Ã‚Â¶rt ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ im bestÃƒÆ’Ã‚Â¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-Diagnose: bestÃƒÆ’Ã‚Â¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÆ’Ã‚Â¤tigten DB-Schema-Stand geprÃƒÆ’Ã‚Â¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÆ’Ã‚Â¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÆ’Ã‚Â¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÆ’Ã‚Â¤tigten Kern geschÃƒÆ’Ã‚Â¤rft, ohne neue App-Logik einzufÃƒÆ’Ã‚Â¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÆ’Ã‚Â¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÆ’Ã‚Â¶st zuerst die aktuelle Saison ÃƒÆ’Ã‚Â¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÆ’Ã‚Â¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÆ’Ã‚Â¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÆ’Ã‚Â¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÆ’Ã‚Â¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÆ’Ã‚Â¼cke ÃƒÆ’Ã‚Â¼ber MAUI-PrÃƒÆ’Ã‚Â¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÆ’Ã‚Â¶glichen Abschlussoptionen geprÃƒÆ’Ã‚Â¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÆ’Ã‚Â¼ckgabe-/Kommentarpfad wÃƒÆ’Ã‚Â¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÆ’Ã‚Â¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÆ’Ã‚Â¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÆ’Ã‚Â¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÆ’Ã‚Â¤hlt und den vorhandenen mobilen PrÃƒÆ’Ã‚Â¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` mobil jetzt nur noch dann an, wenn ÃƒÆ’Ã‚Â¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÆ’Ã‚Â¤chlich offene PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤lle vorliegen; zusÃƒÆ’Ã‚Â¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÆ’Ã‚Â¤tze direkt im Titel des MenÃƒÆ’Ã‚Â¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÆ’Ã‚Â¼ber denselben gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad, sodass MenÃƒÆ’Ã‚Â¼eintrag und tatsÃƒÆ’Ã‚Â¤chliche offene FÃƒÆ’Ã‚Â¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÆ’Ã‚Â¼hrt: die mobile Seite bleibt vollstÃƒÆ’Ã‚Â¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÆ’Ã‚Â¼fstatusanzeige drauÃƒÆ’Ã…Â¸en, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÆ’Ã‚Â¼fflow in WPF bis zur Freigabe vervollstÃƒÆ’Ã‚Â¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã‚Â¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bereits PrÃƒÆ’Ã‚Â¼fliste, Badge/ZÃƒÆ’Ã‚Â¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÆ’Ã‚Â¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÆ’Ã‚Â¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÆ’Ã‚Â¼fmodus geÃƒÆ’Ã‚Â¶ffnet, gefiltert auf die tatsÃƒÆ’Ã‚Â¤chlich offenen DatensÃƒÆ’Ã‚Â¤tze des ausgewÃƒÆ’Ã‚Â¤hlten Mitglieds.
- DafÃƒÆ’Ã‚Â¼r nur einen kleinen Navigationskontext ergÃƒÆ’Ã‚Â¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÆ’Ã‚Â¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÆ’Ã‚Â¼feintrÃƒÆ’Ã‚Â¤ge des konkret ausgewÃƒÆ’Ã‚Â¤hlten Mitglieds im PrÃƒÆ’Ã‚Â¼fmodus erscheinen.
- Admin/Vorstand kÃƒÆ’Ã‚Â¶nnen im PrÃƒÆ’Ã‚Â¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÆ’Ã‚Â¤tzlich eine direkte `Freigeben`-Aktion fÃƒÆ’Ã‚Â¼r den ausgewÃƒÆ’Ã‚Â¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÆ’Ã‚Â¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÆ’Ã‚Â¼r einen sauberen RÃƒÆ’Ã‚Â¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÆ’Ã‚Â¼fliste und Badge/ZÃƒÆ’Ã‚Â¤hler weiterhin ÃƒÆ’Ã‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÆ’Ã‚Â¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÆ’Ã‚Â¼fmodus fachlich geschÃƒÆ’Ã‚Â¤rft: klarer Titel, Hinweistext, leerer PrÃƒÆ’Ã‚Â¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÆ’Ã‚Â¤nde fÃƒÆ’Ã‚Â¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã‚Â¼fliste und PrÃƒÆ’Ã‚Â¼fzÃƒÆ’Ã‚Â¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÆ’Ã‚Â¤ndigung des PrÃƒÆ’Ã‚Â¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÆ’Ã‚Â¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÆ’Ã‚Â¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation and den gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad erneut geprÃƒÆ’Ã‚Â¼ft: ein belastbarer Listen-/Detailpfad fÃƒÆ’Ã‚Â¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÆ’Ã‚Â¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÆ’Ã‚Â¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÆ’Ã‚Â¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÆ’Ã‚Â¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÆ’Ã‚Â¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÆ’Ã‚Â¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÆ’Ã‚Â¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÆ’Ã‚Â¤ge, Vorstand/Admin kÃƒÆ’Ã‚Â¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÆ’Ã‚Â¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÆ’Ã‚Â¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÆ’Ã…Â¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r offene Arbeitsstunden ergÃƒÆ’Ã‚Â¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÆ’Ã‚Â¼hren von der PrÃƒÆ’Ã‚Â¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÆ’Ã‚Â¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` erscheint fÃƒÆ’Ã‚Â¼r berechtigte Benutzer jetzt nur bei tatsÃƒÆ’Ã‚Â¤chlich offenen PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤llen, zeigt einen kleinen ZÃƒÆ’Ã‚Â¤hler mit der Zahl offener DatensÃƒÆ’Ã‚Â¤tze und wird bei offenem PrÃƒÆ’Ã‚Â¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÆ’Ã‚Â¤uft nach ÃƒÆ’Ã¢â‚¬Å¾nderungen ÃƒÆ’Ã‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã‚Â¼flisten und PrÃƒÆ’Ã‚Â¼fzÃƒÆ’Ã‚Â¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÆ’Ã‚Â¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÆ’Ã‚Â¤rten produktiven DB-Pfade geprÃƒÆ’Ã‚Â¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÆ’Ã‚Â¤hrend `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÆ’Ã‚Â¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÆ’Ã‚Â¤hen: `HomeOverviewDTO` trÃƒÆ’Ã‚Â¤gt jetzt zusÃƒÆ’Ã‚Â¤tzlich gemeinsame Startseiten-Daten fÃƒÆ’Ã‚Â¼r Pflichtstunden, ArbeitseinsÃƒÆ’Ã‚Â¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÆ’Ã‚Â¤dt diese Inhalte direkt aus den geklÃƒÆ’Ã‚Â¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÆ’Ã‚Â¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÆ’Ã‚Â¼r die Home-Ausgabe mitgefÃƒÆ’Ã‚Â¼hrt.
- Bereich `ArbeitseinsÃƒÆ’Ã‚Â¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÆ’Ã‚Â¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÆ’Ã‚Â¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÆ’Ã‚Â¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÆ’Ã‚Â¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÆ’Ã‚Â¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÆ’Ã‚Â¤sst sich wieder mit `SchlieÃƒÆ’Ã…Â¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÆ’Ã‚Â¼r Bekanntmachungen angebunden: echte EintrÃƒÆ’Ã‚Â¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÆ’Ã‚Â¼gbaren Inhalte statt des bisherigen kÃƒÆ’Ã‚Â¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÆ’Ã‚Â¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÆ’Ã‚Â¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÆ’Ã‚Â¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÆ’Ã‚Â¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã‚Â¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÆ’Ã‚Â¼ft: konkret auffÃƒÆ’Ã‚Â¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÆ’Ã‚Â¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÆ’Ã‚Â¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: `MainWindowViewModel` lÃƒÆ’Ã‚Â¤dt fÃƒÆ’Ã‚Â¼r den Eigenkontext weiter den nÃƒÆ’Ã‚Â¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÆ’Ã‚Â¤ÃƒÆ’Ã…Â¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÆ’Ã‚Â¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÆ’Ã‚Â¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÆ’Ã‚Â¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÆ’Ã‚Â¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÆ’Ã…Â¸en`-Button ergÃƒÆ’Ã‚Â¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÆ’Ã‚Â¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÆ’Ã‚Â¼ft und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÆ’Ã‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÆ’Ã‚Â¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÆ’Ã‚Â¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÆ’Ã‚Â¤tzliche Infrastruktur direkt testbar.
- DafÃƒÆ’Ã‚Â¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã‚Â¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÆ’Ã‚Â¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÆ’Ã‚Â¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÆ’Ã‚Â¤chsten PrÃƒÆ’Ã‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÆ’Ã‚Â¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÆ’Ã‚Â¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÆ’Ã‚Â¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÆ’Ã‚Â¤chsten kleinen PrÃƒÆ’Ã‚Â¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÆ’Ã‚Â¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÆ’Ã‚Â¼r Desktop und Mobil und lÃƒÆ’Ã‚Â¤sst sich ohne zusÃƒÆ’Ã‚Â¤tzliche Testinfrastruktur direkt prÃƒÆ’Ã‚Â¼fen.
- DafÃƒÆ’Ã‚Â¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã‚Â¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÆ’Ã‚Â¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÆ’Ã‚Â¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÆ’Ã‚Â¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÆ’Ã‚Â¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÆ’Ã‚Â¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÆ’Ã‚Â¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÆ’Ã‚Â¼ckgezogen, sondern sinnvoll zusammengefÃƒÆ’Ã‚Â¼hrt: die aktive Root-Projektdatei ÃƒÆ’Ã‚Â¼bernimmt jetzt wieder die fÃƒÆ’Ã‚Â¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÆ’Ã‚Â¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÆ’Ã‚Â¤ndig erhalten bleibt.
- Fachlich passende ÃƒÆ’Ã‚Â¤ltere Tests sauber zurÃƒÆ’Ã‚Â¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÆ’Ã…Â¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÆ’Ã‚Â¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÆ’Ã‚Â¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÆ’Ã‚Â¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÆ’Ã‚Â¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÆ’Ã‚Â¼r Demo-/Testdatenfilter zurÃƒÆ’Ã‚Â¼ckgeholt

- Die aktuelle Testlage geprÃƒÆ’Ã‚Â¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÆ’Ã‚Â¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÆ’Ã‚Â¤ltere Testidee fÃƒÆ’Ã‚Â¼r `ExportViewModel`, die fÃƒÆ’Ã‚Â¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÆ’Ã‚Â¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÆ’Ã‚Â¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung) ohne dass dafÃƒÆ’Ã‚Â¼r eine groÃƒÆ’Ã…Â¸e Testinfrastruktur nÃƒÆ’Ã‚Â¶tig wÃƒÆ’Ã‚Â¤re.
- DafÃƒÆ’Ã‚Â¼r eine kleine aktive Testbasis im Root zurÃƒÆ’Ã‚Â¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÆ’Ã‚Â¼ft zurÃƒÆ’Ã‚Â¼ckzuziehen.
- Den gewÃƒÆ’Ã‚Â¤hlten PrÃƒÆ’Ã‚Â¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÆ’Ã‚Â¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÆ’Ã‚Â¼ckgezogen: sie waren fÃƒÆ’Ã‚Â¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÆ’Ã‚Â¤tten den Wiedereinstieg unnÃƒÆ’Ã‚Â¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÆ’Ã‚Â¤tzlich lÃƒÆ’Ã‚Â¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÆ’Ã‚Â¼nen Tests.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã‚Â¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÆ’Ã‚Â¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke fÃƒÆ’Ã‚Â¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÆ’Ã‚Â¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÆ’Ã‚Â¼hrt: die Umsetzung bleibt vollstÃƒÆ’Ã‚Â¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÆ’Ã‚Â¼r offene Arbeitsstunden berÃƒÆ’Ã‚Â¼cksichtigt nur operative Mitglieder ÃƒÆ’Ã‚Â¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÆ’Ã‚Â¼r Arbeitsstunden, keine zusÃƒÆ’Ã‚Â¤tzlichen WPF-OberflÃƒÆ’Ã‚Â¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 2: NÃƒÆ’Ã‚Â¤chste mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã‚Â¼cken erneut gegen die produktiven WPF-Wege geprÃƒÆ’Ã‚Â¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÆ’Ã‚Â¤chste kleine, aber fachlich wichtige ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke, weil WPF dafÃƒÆ’Ã‚Â¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÆ’Ã‚Â¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÆ’Ã‚Â¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÆ’Ã‚Â¼r RollenÃƒÆ’Ã‚Â¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÆ’Ã‚Â¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÆ’Ã‚Â¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÆ’Ã‚Â¤chliche Mitgliedsrolle vor einem evtl. ÃƒÆ’Ã‚Â¤lteren `app_user`-Wert, damit RollenÃƒÆ’Ã‚Â¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÆ’Ã…Â¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÆ’Ã‚Â¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÆ’Ã‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 1: NÃƒÆ’Ã‚Â¤chste mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÆ’Ã‚Â¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÆ’Ã‚Â¼ft: die deutlichste belastbare LÃƒÆ’Ã‚Â¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÆ’Ã‚Â¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÆ’Ã‚Â¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÆ’Ã‚Â¼cke als nÃƒÆ’Ã‚Â¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÆ’Ã‚Â¼r Admin/Vorstand, baut vollstÃƒÆ’Ã‚Â¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÆ’Ã‚Â¼r MAUI ergÃƒÆ’Ã‚Â¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÆ’Ã…Â¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÆ’Ã‚Â¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-ÃƒÆ’Ã¢â‚¬Å¾nderungsgrenze auch mobil sauber berÃƒÆ’Ã‚Â¼cksichtigt: eine E-Mail-ÃƒÆ’Ã¢â‚¬Å¾nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÆ’Ã‚Â¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÆ’Ã‚Â¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÆ’Ã‚Â¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÆ’Ã‚Â¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÆ’Ã‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÆ’Ã‚Â¼ft: offen war vor allem noch die Nutzer-ParitÃƒÆ’Ã‚Â¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÆ’Ã‚Â¼fpfad `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` bleiben dagegen weiterhin bewusst auÃƒÆ’Ã…Â¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÆ’Ã‚Â¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÆ’Ã…Â¸t sich die bisherige Home-/Pfad-LÃƒÆ’Ã‚Â¼cke gegenÃƒÆ’Ã‚Â¼ber MAUI.
- IrrefÃƒÆ’Ã‚Â¼hrende leere Bekanntmachungs-DetailflÃƒÆ’Ã‚Â¤chen entschÃƒÆ’Ã‚Â¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÆ’Ã‚Â¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÆ’Ã‚Â¼f-/Bekanntmachungs-Backendpfad fÃƒÆ’Ã‚Â¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÆ’Ã‚Â¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÆ’Ã‚Â¼r WPF und MAUI.
- NÃƒÆ’Ã‚Â¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÆ’Ã‚Â¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÆ’Ã‚Â¼fstÃƒÆ’Ã‚Â¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÆ’Ã‚Â¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÆ’Ã…â€œberblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÆ’Ã‚Â¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÆ’Ã‚Â¶rige gemeinsame PrÃƒÆ’Ã‚Â¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÆ’Ã‚Â¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÆ’Ã‚Â¼ndelt jetzt den fachlichen Kern fÃƒÆ’Ã‚Â¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÆ’Ã‚Â¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÆ’Ã‚Â¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulauflistung wurde fÃƒÆ’Ã‚Â¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÆ’Ã‚Â¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÆ’Ã‚Â¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÆ’Ã‚Â¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 3: Parzellen-KernlÃƒÆ’Ã‚Â¼cken und mobile Admin-ParitÃƒÆ’Ã‚Â¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÆ’Ã‚Â¼ft: belastbar verfÃƒÆ’Ã‚Â¼gbar waren bereits aktive ZÃƒÆ’Ã‚Â¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÆ’Ã…Â¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÆ’Ã‚Â¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÆ’Ã‚Â¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÆ’Ã‚Â¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÆ’Ã‚Â¤hler einbauen und ausbauen sowie das ÃƒÆ’Ã¢â‚¬â€œffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÆ’Ã‚Â¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÆ’Ã‚Â¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÆ’Ã‚Â¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÆ’Ã‚Â¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÆ’Ã‚Â¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÆ’Ã‚Â¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÆ’Ã‚Â¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÆ’Ã…â€œberschneidungen gegen den gewÃƒÆ’Ã‚Â¤hlten Starttag geprÃƒÆ’Ã‚Â¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÆ’Ã‚Â¤nzt: Mitglied zuordnen, Startdatum wÃƒÆ’Ã‚Â¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÆ’Ã‚Â¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÆ’Ã‚Â¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÆ’Ã‚Â¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÆ’Ã‚Â¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÆ’Ã‚Â¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÆ’Ã‚Â¤higkeit geprÃƒÆ’Ã‚Â¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÆ’Ã‚Â¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÆ’Ã‚Â¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÆ’Ã‚Â¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÆ’Ã‚Â¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÆ’Ã‚Â¶rigen Parzellen-Dateien durchgefÃƒÆ’Ã‚Â¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÆ’Ã…Â¸erhalb des Commits.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÆ’Ã‚Â¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÆ’Ã‚Â¼ft und archivwÃƒÆ’Ã‚Â¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÆ’Ã‚Â¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÆ’Ã‚Â¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÆ’Ã‚Â¼berfÃƒÆ’Ã‚Â¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÆ’Ã‚Â¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÆ’Ã‚Â¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÆ’Ã‚Â¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÆ’Ã‚Â¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÆ’Ã‚Â¤ndert gelassen: keine ÃƒÆ’Ã¢â‚¬Å¾nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÆ’Ã…Â¸erhalb der aktiven LÃƒÆ’Ã‚Â¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÆ’Ã‚Â¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÆ’Ã‚Â¼ft: nur die tatsÃƒÆ’Ã‚Â¤chlich geÃƒÆ’Ã‚Â¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÆ’Ã‚Â¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÆ’Ã‚Â¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÆ’Ã‚Â¼r `Mailadresse ÃƒÆ’Ã‚Â¤ndern` in `MemberDetailViewModel` ergÃƒÆ’Ã‚Â¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÆ’Ã‚Â¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÆ’Ã‚Â¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÆ’Ã‚Â¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÆ’Ã…Â¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÆ’Ã‚Â¼r diesen Teilblock vorbereitet: nur die tatsÃƒÆ’Ã‚Â¤chlich zugehÃƒÆ’Ã‚Â¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÆ’Ã…Â¸en vor.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÆ’Ã‚Â¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÆ’Ã‚Â¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÆ’Ã‚Â¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÆ’Ã‚Â¤nde zurÃƒÆ’Ã‚Â¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÆ’Ã‚Â¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÆ’Ã‚Â¼ckkehrt.
- Login-/Recovery-ZustÃƒÆ’Ã‚Â¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÆ’Ã‚Â¤umt alte OTP-ZustÃƒÆ’Ã‚Â¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÆ’Ã‚Â¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÆ’Ã‚Â¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÆ’Ã‚Â¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÆ’Ã‚Â¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÆ’Ã‚Â¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÆ’Ã‚Â¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÆ’Ã‚Â¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÆ’Ã‚Â¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÆ’Ã‚Â¤rft: normales Login, OTP-PrÃƒÆ’Ã‚Â¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÆ’Ã‚Â¤nde gefÃƒÆ’Ã‚Â¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÆ’Ã‚Â¼hrung sauber: RÃƒÆ’Ã‚Â¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÆ’Ã‚Â¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÆ’Ã‚Â¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÆ’Ã‚Â¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÆ’Ã‚Â¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÆ’Ã‚Â¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÆ’Ã‚Â¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÆ’Ã‚Â¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÆ’Ã‚Â¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÆ’Ã‚Â¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÆ’Ã‚Â¤nzt: neues `README.md` als Startpunkt fÃƒÆ’Ã‚Â¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÆ’Ã‚Â¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÆ’Ã‚Â¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÆ’Ã‚Â¤digten/irrefÃƒÆ’Ã‚Â¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÆ’Ã‚Â¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÆ’Ã‚Â¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÆ’Ã‚Â¼berfÃƒÆ’Ã‚Â¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÆ’Ã‚Â¼fbaren Projekte ausgerichtet.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Prompt 1/2: Home/Startseite ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÆ’Ã‚Â¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÆ’Ã‚Â¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÆ’Ã‚Â¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÆ’Ã‚Â¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÆ’Ã‚Â¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÆ’Ã‚Â¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÆ’Ã‚Â¼r `admin` und `vorstand` sichtbar, ÃƒÆ’Ã‚Â¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÆ’Ã‚Â¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÆ’Ã‚Â¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÆ’Ã‚Â¤nde fÃƒÆ’Ã‚Â¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÆ’Ã‚Â¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

---

## Workaround & Vorgehensweise bei ÃƒÆ’Ã¢â‚¬Å¾nderungen

- ÃƒÆ’Ã¢â‚¬Å¾nderungen nur nach genauer RÃƒÆ’Ã‚Â¼ckfrage und vollstÃƒÆ’Ã‚Â¤ndiger Ansicht der Datei vornehmen.  
- Ich mÃƒÆ’Ã‚Â¶chte immer genau wissen, **wo und wie** die ÃƒÆ’Ã¢â‚¬Å¾nderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor ÃƒÆ’Ã¢â‚¬Å¾nderungen vorgeschlagen werden.  

---

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÆ’Ã‚Â¤chliche XAML-Ursache zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÆ’Ã‚Â¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÆ’Ã‚Â¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÆ’Ã‚Â¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÆ’Ã‚Â¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÆ’Ã‚Â¼ck-/Dirty-Check ergÃƒÆ’Ã‚Â¤nzt, Navigation auf Home-only zurÃƒÆ’Ã‚Â¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÆ’Ã‚Â¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÆ’Ã‚Â¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÆ’Ã…Â¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÆ’Ã‚Â¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÆ’Ã‚Â¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÆ’Ã‚Â¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÆ’Ã‚Â¼r geladene VerwaltungsdatensÃƒÆ’Ã‚Â¤tze sowie fÃƒÆ’Ã‚Â¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÆ’Ã‚Â¼ckkehr erfolgt ÃƒÆ’Ã‚Â¼ber den vorhandenen Navigationspfad zurÃƒÆ’Ã‚Â¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÆ’Ã‚Â¼ck`-Button
  - `ZurÃƒÆ’Ã‚Â¼ck` und `Abbrechen` prÃƒÆ’Ã‚Â¼fen auf echte ungespeicherte ÃƒÆ’Ã¢â‚¬Å¾nderungen
  - die RÃƒÆ’Ã‚Â¼ckfrage erscheint nur bei tatsÃƒÆ’Ã‚Â¤chlichen ÃƒÆ’Ã¢â‚¬Å¾nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÆ’Ã‚Â¼nschten Produktpfad zurÃƒÆ’Ã‚Â¼ckgezogen:
  - `ArbeitseinsÃƒÆ’Ã‚Â¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÆ’Ã‚Â¼ber die vorhandenen Home-Buttons fÃƒÆ’Ã‚Â¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÆ’Ã‚Â¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÆ’Ã‚Â¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÆ’Ã‚Â¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÆ’Ã‚Â¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÆ’Ã‚Â¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÆ’Ã‚Â¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÆ’Ã‚Â¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÆ’Ã‚Â¼ber `status`; die globale PrÃƒÆ’Ã‚Â¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÆ’Ã‚Â¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÆ’Ã‚Â¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÆ’Ã‚Â¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÆ’Ã‚Â¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÆ’Ã‚Â¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÆ’Ã‚Â¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÆ’Ã‚Â¤chlich geÃƒÆ’Ã‚Â¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÆ’Ã‚Â¤tze bleiben unverÃƒÆ’Ã‚Â¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÆ’Ã‚Â¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÆ’Ã‚Â¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÆ’Ã‚Â¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÆ’Ã‚Â¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÆ’Ã‚Â¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim ÃƒÆ’Ã¢â‚¬â€œffnen der Freigabeansicht wird eine globale PrÃƒÆ’Ã‚Â¼fsperre ÃƒÆ’Ã‚Â¼ber die offenen `arbeitstunde`-DatensÃƒÆ’Ã‚Â¤tze gesetzt
  - andere PrÃƒÆ’Ã‚Â¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÆ’Ã‚Â¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÆ’Ã‚Â¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÆ’Ã‚Â¤ngert die Sperre wÃƒÆ’Ã‚Â¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÆ’Ã‚Â¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÆ’Ã‚Â¼bernehmbar
- Die bestehende Badge-/ZÃƒÆ’Ã‚Â¤hlerlogik bleibt weiterverwendet: ÃƒÆ’Ã¢â‚¬Å¾nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÆ’Ã‚Â¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÆ’Ã‚Â¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÆ’Ã‚Â¼r offene FÃƒÆ’Ã‚Â¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÆ’Ã‚Â¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÆ’Ã‚Â¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÆ’Ã‚Â¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÆ’Ã‚Â¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÆ’Ã‚Â¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÆ’Ã‚Â¼r `Datum`, `Stunden` und `Art der Arbeit` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ ohne zusÃƒÆ’Ã‚Â¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÆ’Ã‚Â¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÆ’Ã‚Â¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÆ’Ã‚Â¼r Benutzer mit eigenem Mitgliedskontext; additionally erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÆ’Ã‚Â¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÆ’Ã‚Â¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÆ’Ã‚Â¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸er als `0` sein.
- FÃƒÆ’Ã‚Â¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÆ’Ã‚Â¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÆ’Ã‚Â¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÆ’Ã‚Â¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÆ’Ã‚Â¤hler zeigt weiter die tatsÃƒÆ’Ã‚Â¤chliche Anzahl offener PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÆ’Ã‚Â¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÆ’Ã‚Â¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã‚Â¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÆ’Ã‚Â¤hrend im Repo noch die ÃƒÆ’Ã‚Â¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÆ’Ã‚Â¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÆ’Ã‚Â¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÆ’Ã‚Â¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÆ’Ã‚Â¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÆ’Ã‚Â¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÆ’Ã‚Â¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÆ’Ã…Â¸lich fÃƒÆ’Ã‚Â¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÆ’Ã‚Â¼r spÃƒÆ’Ã‚Â¤tere MAUI-ParitÃƒÆ’Ã‚Â¤t anschlussfÃƒÆ’Ã‚Â¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÆ’Ã‚Â¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÆ’Ã‚Â¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÆ’Ã‚Â¤hrend die eigentlichen EditoroberflÃƒÆ’Ã‚Â¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÆ’Ã‚Â¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÆ’Ã‚Â¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÆ’Ã‚Â¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ ArbeitseinsÃƒÆ’Ã‚Â¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÆ’Ã‚Â¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÆ’Ã‚Â¤tze-Verwaltung erneut geprÃƒÆ’Ã‚Â¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÆ’Ã‚Â¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÆ’Ã‚Â¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÆ’Ã‚Â¤nzt und nicht ÃƒÆ’Ã‚Â¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÆ’Ã‚Â¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÆ’Ã‚Â¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÆ’Ã‚Â¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÆ’Ã‚Â¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÆ’Ã‚Â¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÆ’Ã‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã‚Â¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÆ’Ã‚Â¤tze-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÆ’Ã‚Â¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã‚Â¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÆ’Ã‚Â¤nzt und nicht ÃƒÆ’Ã‚Â¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÆ’Ã‚Â¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÆ’Ã‚Â¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÆ’Ã‚Â¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÆ’Ã‚Â¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÆ’Ã…â€œberschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÆ’Ã‚Â¤nzt und parallel in einer kompakten Vorschau geprÃƒÆ’Ã‚Â¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÆ’Ã‚Â¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÆ’Ã‚Â¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÆ’Ã‚Â¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÆ’Ã‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã‚Â¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÆ’Ã‚Â¤chsten AufrÃƒÆ’Ã‚Â¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÆ’Ã‚Â¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÆ’Ã‚Â¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã‚Â¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÆ’Ã…Â¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÆ’Ã‚Â¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÆ’Ã‚Â¤nderte DatensÃƒÆ’Ã‚Â¤tze direkt per Create/Update wieder nach `termin` zurÃƒÆ’Ã‚Â¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÆ’Ã‚Â¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÆ’Ã‚Â¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÆ’Ã‚Â¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÆ’Ã‚Â¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÆ’Ã‚Â¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÆ’Ã‚Â¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÆ’Ã‚Â¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÆ’Ã‚Â¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã‚Â¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÆ’Ã‚Â¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÆ’Ã‚Â¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÆ’Ã‚Â¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÆ’Ã‚Â¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÆ’Ã…â€œbersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÆ’Ã‚Â¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÆ’Ã‚Â¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÆ’Ã‚Â¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÆ’Ã‚Â¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÆ’Ã‚Â¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÆ’Ã‚Â¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÆ’Ã‚Â¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÆ’Ã‚Â¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÆ’Ã‚Â¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÆ’Ã‚Â¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand verfÃƒÆ’Ã‚Â¼gbar und blÃƒÆ’Ã‚Â¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÆ’Ã‚Â¼r spÃƒÆ’Ã‚Â¤tere MAUI-ParitÃƒÆ’Ã‚Â¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÆ’Ã‚Â¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÆ’Ã‚Â¤tigt: `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` laden ÃƒÆ’Ã‚Â¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÆ’Ã‚Â¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÆ’Ã‚Â¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÆ’Ã…Â¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÆ’Ã‚Â¤tigte View.
- FÃƒÆ’Ã‚Â¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÆ’Ã‚Â¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÆ’Ã‚Â¼fung bleibt daher auf tatsÃƒÆ’Ã‚Â¤chliche RÃƒÆ’Ã‚Â¼ckgabefelder bzw. reale DatensÃƒÆ’Ã‚Â¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÆ’Ã‚Â¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÆ’Ã‚Â¤tzen, Terminen und Bekanntmachungen ÃƒÆ’Ã‚Â¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÆ’Ã‚Â¶ffnen in eigener View

- Den Home-Stand fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen auf den gewÃƒÆ’Ã‚Â¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÆ’Ã‚Â¼bersichten, wÃƒÆ’Ã‚Â¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÆ’Ã‚Â¼r eine eigenstÃƒÆ’Ã‚Â¤ndige WPF-Detailansicht fÃƒÆ’Ã‚Â¼r Home-Elemente ergÃƒÆ’Ã‚Â¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite ÃƒÆ’Ã¢â‚¬â€œffnen-Kommandos fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÆ’Ã‚Â¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÆ’Ã‚Â¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÆ’Ã‚Â¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÆ’Ã‚Â¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÆ’Ã‚Â¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÆ’Ã‚Â¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÆ’Ã‚Â¼ft. Die entscheidende technische Ursache fÃƒÆ’Ã‚Â¼r den Pflichtstundenfehler lieÃƒÆ’Ã…Â¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÆ’Ã‚Â¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÆ’Ã‚Â¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÆ’Ã‚Â¤hrend `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÆ’Ã‚Â¼r Home jetzt zunÃƒÆ’Ã‚Â¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÆ’Ã…Â¸end robust clientseitig ÃƒÆ’Ã‚Â¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÆ’Ã‚Â¼r Pflichtstunden ergÃƒÆ’Ã‚Â¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÆ’Ã‚Â¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÆ’Ã‚Â¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÆ’Ã‚Â¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÆ’Ã‚Â¼ckgelieferte EintrÃƒÆ’Ã‚Â¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÆ’Ã‚Â¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÆ’Ã‚Â¤nzt: der Home-Pfad schreibt jetzt fÃƒÆ’Ã‚Â¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÆ’Ã‚Â¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÆ’Ã‚Â¼ber Hauptmitglied/Saison prÃƒÆ’Ã‚Â¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÆ’Ã‚Â¼ft: da `ArbeitseinsÃƒÆ’Ã‚Â¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Pflichtstunden ÃƒÆ’Ã‚Â¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÆ’Ã‚Â¶sung und Bekanntmachungen ÃƒÆ’Ã‚Â¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÆ’Ã‚Â¼r Home deshalb final prÃƒÆ’Ã‚Â¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÆ’Ã‚Â¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÆ’Ã‚Â¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÆ’Ã‚Â¼bersicht ÃƒÆ’Ã‚Â¼ber dieses Hauptmitglied geladen.
- ZusÃƒÆ’Ã‚Â¤tzlich wird die bestÃƒÆ’Ã‚Â¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÆ’Ã‚Â¼cksichtigt, sondern zuerst direkt fÃƒÆ’Ã‚Â¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÆ’Ã‚Â¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÆ’Ã‚Â¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÆ’Ã‚Â¤tzlich mÃƒÆ’Ã‚Â¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÆ’Ã‚Â¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÆ’Ã‚Â¼r Titel, Inhalt und VerÃƒÆ’Ã‚Â¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÆ’Ã‚Â¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÆ’Ã‚Â¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÆ’Ã‚Â¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÆ’Ã‚Â¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÆ’Ã‚Â¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÆ’Ã‚Â¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÆ’Ã‚Â¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÆ’Ã‚Â¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÆ’Ã‚Â¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÆ’Ã‚Â¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÆ’Ã‚Â¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÆ’Ã‚Â¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÆ’Ã‚Â¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÆ’Ã‚Â¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÆ’Ã‚Â¤tzlich vereinfacht und nÃƒÆ’Ã‚Â¤her auf den bestÃƒÆ’Ã‚Â¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÆ’Ã‚Â¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÆ’Ã‚Â¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÆ’Ã‚Â¶rt ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ im bestÃƒÆ’Ã‚Â¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-Diagnose: bestÃƒÆ’Ã‚Â¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÆ’Ã‚Â¤tigten DB-Schema-Stand geprÃƒÆ’Ã‚Â¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÆ’Ã‚Â¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÆ’Ã‚Â¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÆ’Ã‚Â¤tigten Kern geschÃƒÆ’Ã‚Â¤rft, ohne neue App-Logik einzufÃƒÆ’Ã‚Â¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÆ’Ã‚Â¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÆ’Ã‚Â¶st zuerst die aktuelle Saison ÃƒÆ’Ã‚Â¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÆ’Ã‚Â¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÆ’Ã‚Â¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÆ’Ã‚Â¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÆ’Ã‚Â¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÆ’Ã‚Â¼cke ÃƒÆ’Ã‚Â¼ber MAUI-PrÃƒÆ’Ã‚Â¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÆ’Ã‚Â¶glichen Abschlussoptionen geprÃƒÆ’Ã‚Â¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÆ’Ã‚Â¼ckgabe-/Kommentarpfad wÃƒÆ’Ã‚Â¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÆ’Ã‚Â¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÆ’Ã‚Â¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÆ’Ã‚Â¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÆ’Ã‚Â¤hlt und den vorhandenen mobilen PrÃƒÆ’Ã‚Â¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` mobil jetzt nur noch dann an, wenn ÃƒÆ’Ã‚Â¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÆ’Ã‚Â¤chlich offene PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤lle vorliegen; zusÃƒÆ’Ã‚Â¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÆ’Ã‚Â¤tze direkt im Titel des MenÃƒÆ’Ã‚Â¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÆ’Ã‚Â¼ber denselben gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad, sodass MenÃƒÆ’Ã‚Â¼eintrag und tatsÃƒÆ’Ã‚Â¤chliche offene FÃƒÆ’Ã‚Â¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÆ’Ã‚Â¼hrt: die mobile Seite bleibt vollstÃƒÆ’Ã‚Â¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÆ’Ã‚Â¼fstatusanzeige drauÃƒÆ’Ã…Â¸en, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÆ’Ã‚Â¼fflow in WPF bis zur Freigabe vervollstÃƒÆ’Ã‚Â¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã‚Â¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren bereits PrÃƒÆ’Ã‚Â¼fliste, Badge/ZÃƒÆ’Ã‚Â¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÆ’Ã‚Â¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÆ’Ã‚Â¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÆ’Ã‚Â¼fmodus geÃƒÆ’Ã‚Â¶ffnet, gefiltert auf die tatsÃƒÆ’Ã‚Â¤chlich offenen DatensÃƒÆ’Ã‚Â¤tze des ausgewÃƒÆ’Ã‚Â¤hlten Mitglieds.
- DafÃƒÆ’Ã‚Â¼r nur einen kleinen Navigationskontext ergÃƒÆ’Ã‚Â¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÆ’Ã‚Â¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÆ’Ã‚Â¼feintrÃƒÆ’Ã‚Â¤ge des konkret ausgewÃƒÆ’Ã‚Â¤hlten Mitglieds im PrÃƒÆ’Ã‚Â¼fmodus erscheinen.
- Admin/Vorstand kÃƒÆ’Ã‚Â¶nnen im PrÃƒÆ’Ã‚Â¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÆ’Ã‚Â¤tzlich eine direkte `Freigeben`-Aktion fÃƒÆ’Ã‚Â¼r den ausgewÃƒÆ’Ã‚Â¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÆ’Ã‚Â¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÆ’Ã‚Â¼r einen sauberen RÃƒÆ’Ã‚Â¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÆ’Ã‚Â¼fliste und Badge/ZÃƒÆ’Ã‚Â¤hler weiterhin ÃƒÆ’Ã‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÆ’Ã‚Â¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÆ’Ã‚Â¼fmodus fachlich geschÃƒÆ’Ã‚Â¤rft: klarer Titel, Hinweistext, leerer PrÃƒÆ’Ã‚Â¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÆ’Ã‚Â¤nde fÃƒÆ’Ã‚Â¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã‚Â¼fliste und PrÃƒÆ’Ã‚Â¼fzÃƒÆ’Ã‚Â¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÆ’Ã‚Â¤ndigung des PrÃƒÆ’Ã‚Â¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÆ’Ã‚Â¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÆ’Ã‚Â¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation und den gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad erneut geprÃƒÆ’Ã‚Â¼ft: ein belastbarer Listen-/Detailpfad fÃƒÆ’Ã‚Â¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÆ’Ã‚Â¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÆ’Ã‚Â¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÆ’Ã‚Â¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÆ’Ã‚Â¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÆ’Ã‚Â¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÆ’Ã‚Â¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÆ’Ã‚Â¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÆ’Ã‚Â¤ge, Vorstand/Admin kÃƒÆ’Ã‚Â¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÆ’Ã‚Â¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÆ’Ã‚Â¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÆ’Ã…Â¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r offene Arbeitsstunden ergÃƒÆ’Ã‚Â¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÆ’Ã‚Â¼hren von der PrÃƒÆ’Ã‚Â¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÆ’Ã‚Â¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` erscheint fÃƒÆ’Ã‚Â¼r berechtigte Benutzer jetzt nur bei tatsÃƒÆ’Ã‚Â¤chlich offenen PrÃƒÆ’Ã‚Â¼ffÃƒÆ’Ã‚Â¤llen, zeigt einen kleinen ZÃƒÆ’Ã‚Â¤hler mit der Zahl offener DatensÃƒÆ’Ã‚Â¤tze und wird bei offenem PrÃƒÆ’Ã‚Â¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÆ’Ã‚Â¤uft nach ÃƒÆ’Ã¢â‚¬Å¾nderungen ÃƒÆ’Ã‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã‚Â¼flisten und PrÃƒÆ’Ã‚Â¼fzÃƒÆ’Ã‚Â¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã…Â¸lich der gemeinsame operative PrÃƒÆ’Ã‚Â¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÆ’Ã‚Â¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÆ’Ã‚Â¤rten produktiven DB-Pfade geprÃƒÆ’Ã‚Â¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÆ’Ã‚Â¤hrend `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÆ’Ã‚Â¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÆ’Ã‚Â¤hen: `HomeOverviewDTO` trÃƒÆ’Ã‚Â¤gt jetzt zusÃƒÆ’Ã‚Â¤tzlich gemeinsame Startseiten-Daten fÃƒÆ’Ã‚Â¼r Pflichtstunden, ArbeitseinsÃƒÆ’Ã‚Â¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÆ’Ã‚Â¤dt diese Inhalte direkt aus den geklÃƒÆ’Ã‚Â¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÆ’Ã‚Â¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÆ’Ã‚Â¼r die Home-Ausgabe mitgefÃƒÆ’Ã‚Â¼hrt.
- Bereich `ArbeitseinsÃƒÆ’Ã‚Â¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÆ’Ã‚Â¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÆ’Ã‚Â¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÆ’Ã‚Â¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÆ’Ã‚Â¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÆ’Ã‚Â¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÆ’Ã‚Â¤sst sich wieder mit `SchlieÃƒÆ’Ã…Â¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÆ’Ã‚Â¼r Bekanntmachungen angebunden: echte EintrÃƒÆ’Ã‚Â¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÆ’Ã‚Â¼gbaren Inhalte statt des bisherigen kÃƒÆ’Ã‚Â¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÆ’Ã‚Â¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÆ’Ã‚Â¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÆ’Ã‚Â¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÆ’Ã‚Â¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã‚Â¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÆ’Ã‚Â¼ft: konkret auffÃƒÆ’Ã‚Â¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÆ’Ã‚Â¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÆ’Ã‚Â¶ÃƒÆ’Ã…Â¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÆ’Ã‚Â¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: `MainWindowViewModel` lÃƒÆ’Ã‚Â¤dt fÃƒÆ’Ã‚Â¼r den Eigenkontext weiter den nÃƒÆ’Ã‚Â¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÆ’Ã‚Â¤ÃƒÆ’Ã…Â¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÆ’Ã‚Â¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÆ’Ã‚Â¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÆ’Ã‚Â¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÆ’Ã‚Â¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÆ’Ã…Â¸en`-Button ergÃƒÆ’Ã‚Â¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÆ’Ã‚Â¼r `ArbeitseinsÃƒÆ’Ã‚Â¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÆ’Ã‚Â¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÆ’Ã‚Â¼ft und bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÆ’Ã‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÆ’Ã‚Â¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÆ’Ã‚Â¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÆ’Ã‚Â¤tzliche Infrastruktur direkt testbar.
- DafÃƒÆ’Ã‚Â¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã‚Â¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÆ’Ã‚Â¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÆ’Ã‚Â¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÆ’Ã‚Â¤chsten PrÃƒÆ’Ã‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÆ’Ã‚Â¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÆ’Ã‚Â¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÆ’Ã‚Â¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÆ’Ã‚Â¤chsten kleinen PrÃƒÆ’Ã‚Â¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÆ’Ã‚Â¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÆ’Ã‚Â¼r Desktop und Mobil und lÃƒÆ’Ã‚Â¤sst sich ohne zusÃƒÆ’Ã‚Â¤tzliche Testinfrastruktur direkt prÃƒÆ’Ã‚Â¼fen.
- DafÃƒÆ’Ã‚Â¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã‚Â¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÆ’Ã‚Â¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÆ’Ã‚Â¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÆ’Ã‚Â¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÆ’Ã‚Â¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÆ’Ã‚Â¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÆ’Ã‚Â¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÆ’Ã‚Â¼ckgezogen, sondern sinnvoll zusammengefÃƒÆ’Ã‚Â¼hrt: die aktive Root-Projektdatei ÃƒÆ’Ã‚Â¼bernimmt jetzt wieder die fÃƒÆ’Ã‚Â¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÆ’Ã‚Â¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÆ’Ã‚Â¤ndig erhalten bleibt.
- Fachlich passende ÃƒÆ’Ã‚Â¤ltere Tests sauber zurÃƒÆ’Ã‚Â¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÆ’Ã…Â¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÆ’Ã‚Â¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÆ’Ã‚Â¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÆ’Ã‚Â¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÆ’Ã‚Â¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã‚Â¤hig.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÆ’Ã‚Â¼r Demo-/Testdatenfilter zurÃƒÆ’Ã‚Â¼ckgeholt

- Die aktuelle Testlage geprÃƒÆ’Ã‚Â¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÆ’Ã‚Â¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÆ’Ã‚Â¤ltere Testidee fÃƒÆ’Ã‚Â¼r `ExportViewModel`, die fÃƒÆ’Ã‚Â¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÆ’Ã‚Â¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÆ’Ã‚Â¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung) ohne dass dafÃƒÆ’Ã‚Â¼r eine groÃƒÆ’Ã…Â¸e Testinfrastruktur nÃƒÆ’Ã‚Â¶tig wÃƒÆ’Ã‚Â¤re.
- DafÃƒÆ’Ã‚Â¼r eine kleine aktive Testbasis im Root zurÃƒÆ’Ã‚Â¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÆ’Ã‚Â¼ft zurÃƒÆ’Ã‚Â¼ckzuziehen.
- Den gewÃƒÆ’Ã‚Â¤hlten PrÃƒÆ’Ã‚Â¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÆ’Ã‚Â¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÆ’Ã‚Â¼ckgezogen: sie waren fÃƒÆ’Ã‚Â¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÆ’Ã‚Â¤tten den Wiedereinstieg unnÃƒÆ’Ã‚Â¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÆ’Ã‚Â¤tzlich lÃƒÆ’Ã‚Â¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÆ’Ã‚Â¼nen Tests.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã‚Â¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÆ’Ã‚Â¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke fÃƒÆ’Ã‚Â¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÆ’Ã‚Â¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÆ’Ã‚Â¼hrt: die Umsetzung bleibt vollstÃƒÆ’Ã‚Â¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÆ’Ã‚Â¼r offene Arbeitsstunden berÃƒÆ’Ã‚Â¼cksichtigt nur operative Mitglieder ÃƒÆ’Ã‚Â¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÆ’Ã‚Â¼r Arbeitsstunden, keine zusÃƒÆ’Ã‚Â¤tzlichen WPF-OberflÃƒÆ’Ã‚Â¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 2: NÃƒÆ’Ã‚Â¤chste mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã‚Â¼cken erneut gegen die produktiven WPF-Wege geprÃƒÆ’Ã‚Â¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÆ’Ã‚Â¤chste kleine, aber fachlich wichtige ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke, weil WPF dafÃƒÆ’Ã‚Â¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÆ’Ã‚Â¤tslÃƒÆ’Ã‚Â¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÆ’Ã‚Â¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÆ’Ã‚Â¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÆ’Ã‚Â¼r RollenÃƒÆ’Ã‚Â¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÆ’Ã‚Â¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÆ’Ã‚Â¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÆ’Ã‚Â¤chliche Mitgliedsrolle vor einem evtl. ÃƒÆ’Ã‚Â¤lteren `app_user`-Wert, damit RollenÃƒÆ’Ã‚Â¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÆ’Ã…Â¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÆ’Ã‚Â¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÆ’Ã‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 5 Prompt 1: NÃƒÆ’Ã‚Â¤chste mobile Admin-ParitÃƒÆ’Ã‚Â¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÆ’Ã‚Â¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÆ’Ã‚Â¼ft: die deutlichste belastbare LÃƒÆ’Ã‚Â¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÆ’Ã‚Â¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÆ’Ã‚Â¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÆ’Ã‚Â¼cke als nÃƒÆ’Ã‚Â¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÆ’Ã‚Â¼r Admin/Vorstand, baut vollstÃƒÆ’Ã‚Â¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÆ’Ã‚Â¼r MAUI ergÃƒÆ’Ã‚Â¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÆ’Ã…Â¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÆ’Ã‚Â¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-ÃƒÆ’Ã¢â‚¬Å¾nderungsgrenze auch mobil sauber berÃƒÆ’Ã‚Â¼cksichtigt: eine E-Mail-ÃƒÆ’Ã¢â‚¬Å¾nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÆ’Ã‚Â¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÆ’Ã‚Â¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÆ’Ã‚Â¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÆ’Ã‚Â¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÆ’Ã‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÆ’Ã‚Â¼ft: offen war vor allem noch die Nutzer-ParitÃƒÆ’Ã‚Â¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÆ’Ã‚Â¼fpfad `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen` bleiben dagegen weiterhin bewusst auÃƒÆ’Ã…Â¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÆ’Ã‚Â¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÆ’Ã…Â¸t sich die bisherige Home-/Pfad-LÃƒÆ’Ã‚Â¼cke gegenÃƒÆ’Ã‚Â¼ber MAUI.
- IrrefÃƒÆ’Ã‚Â¼hrende leere Bekanntmachungs-DetailflÃƒÆ’Ã‚Â¤chen entschÃƒÆ’Ã‚Â¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÆ’Ã‚Â¼fpfad fÃƒÆ’Ã‚Â¼r `Arbeitsstunden prÃƒÆ’Ã‚Â¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÆ’Ã‚Â¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÆ’Ã‚Â¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÆ’Ã‚Â¼f-/Bekanntmachungs-Backendpfad fÃƒÆ’Ã‚Â¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÆ’Ã‚Â¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÆ’Ã‚Â¼r WPF und MAUI.
- NÃƒÆ’Ã‚Â¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÆ’Ã‚Â¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÆ’Ã‚Â¼fstÃƒÆ’Ã‚Â¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÆ’Ã‚Â¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÆ’Ã…â€œberblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÆ’Ã‚Â¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÆ’Ã‚Â¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÆ’Ã‚Â¶rige gemeinsame PrÃƒÆ’Ã‚Â¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÆ’Ã‚Â¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÆ’Ã‚Â¼ndelt jetzt den fachlichen Kern fÃƒÆ’Ã‚Â¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÆ’Ã‚Â¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÆ’Ã‚Â¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulaufl listing wurde fÃƒÆ’Ã‚Â¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÆ’Ã‚Â¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÆ’Ã‚Â¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÆ’Ã‚Â¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 3: Parzellen-KernlÃƒÆ’Ã‚Â¼cken und mobile Admin-ParitÃƒÆ’Ã‚Â¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÆ’Ã‚Â¼ft: belastbar verfÃƒÆ’Ã‚Â¼gbar waren bereits aktive ZÃƒÆ’Ã‚Â¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÆ’Ã…Â¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÆ’Ã‚Â¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÆ’Ã‚Â¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÆ’Ã‚Â¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÆ’Ã‚Â¤hler einbauen und ausbauen sowie das ÃƒÆ’Ã¢â‚¬â€œffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÆ’Ã‚Â¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÆ’Ã‚Â¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÆ’Ã‚Â¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÆ’Ã‚Â¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÆ’Ã‚Â¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÆ’Ã‚Â¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÆ’Ã‚Â¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÆ’Ã…â€œberschneidungen gegen den gewÃƒÆ’Ã‚Â¤hlten Starttag geprÃƒÆ’Ã‚Â¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÆ’Ã‚Â¤nzt: Mitglied zuordnen, Startdatum wÃƒÆ’Ã‚Â¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÆ’Ã‚Â¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÆ’Ã‚Â¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÆ’Ã‚Â¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÆ’Ã‚Â¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÆ’Ã‚Â¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÆ’Ã‚Â¤higkeit geprÃƒÆ’Ã‚Â¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÆ’Ã‚Â¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÆ’Ã‚Â¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÆ’Ã‚Â¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÆ’Ã‚Â¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÆ’Ã‚Â¶rigen Parzellen-Dateien durchgefÃƒÆ’Ã‚Â¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÆ’Ã…Â¸erhalb des Commits.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÆ’Ã‚Â¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÆ’Ã‚Â¼ft und archivwÃƒÆ’Ã‚Â¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÆ’Ã‚Â¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÆ’Ã‚Â¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÆ’Ã‚Â¼berfÃƒÆ’Ã‚Â¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÆ’Ã‚Â¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÆ’Ã‚Â¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÆ’Ã‚Â¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÆ’Ã‚Â¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÆ’Ã‚Â¤ndert gelassen: keine ÃƒÆ’Ã¢â‚¬Å¾nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÆ’Ã…Â¸erhalb der aktiven LÃƒÆ’Ã‚Â¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÆ’Ã‚Â¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÆ’Ã‚Â¼ft: nur die tatsÃƒÆ’Ã‚Â¤chlich geÃƒÆ’Ã‚Â¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÆ’Ã‚Â¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÆ’Ã‚Â¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÆ’Ã‚Â¼r `Mailadresse ÃƒÆ’Ã‚Â¤ndern` in `MemberDetailViewModel` ergÃƒÆ’Ã‚Â¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÆ’Ã‚Â¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÆ’Ã‚Â¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÆ’Ã‚Â¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÆ’Ã…Â¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÆ’Ã‚Â¼r diesen Teilblock vorbereitet: nur die tatsÃƒÆ’Ã‚Â¤chlich zugehÃƒÆ’Ã‚Â¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÆ’Ã…Â¸en vor.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÆ’Ã‚Â¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÆ’Ã‚Â¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÆ’Ã‚Â¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÆ’Ã‚Â¤nde zurÃƒÆ’Ã‚Â¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÆ’Ã‚Â¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÆ’Ã‚Â¼ckkehrt.
- Login-/Recovery-ZustÃƒÆ’Ã‚Â¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÆ’Ã‚Â¤umt alte OTP-ZustÃƒÆ’Ã‚Â¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÆ’Ã‚Â¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÆ’Ã‚Â¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÆ’Ã‚Â¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÆ’Ã‚Â¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÆ’Ã‚Â¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÆ’Ã‚Â¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÆ’Ã‚Â¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÆ’Ã‚Â¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÆ’Ã‚Â¤rft: normales Login, OTP-PrÃƒÆ’Ã‚Â¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÆ’Ã‚Â¤nde gefÃƒÆ’Ã‚Â¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÆ’Ã‚Â¼hrung sauber: RÃƒÆ’Ã‚Â¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÆ’Ã‚Â¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÆ’Ã‚Â¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÆ’Ã‚Â¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÆ’Ã‚Â¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÆ’Ã‚Â¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÆ’Ã‚Â¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÆ’Ã‚Â¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÆ’Ã‚Â¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÆ’Ã‚Â¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÆ’Ã‚Â¤nzt: neues `README.md` als Startpunkt fÃƒÆ’Ã‚Â¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÆ’Ã‚Â¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÆ’Ã‚Â¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÆ’Ã‚Â¤digten/irrefÃƒÆ’Ã‚Â¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÆ’Ã‚Â¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÆ’Ã‚Â¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÆ’Ã‚Â¼berfÃƒÆ’Ã‚Â¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÆ’Ã‚Â¼fbaren Projekte ausgerichtet.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Prompt 1/2: Home/Startseite ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÆ’Ã‚Â¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÆ’Ã‚Â¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÆ’Ã‚Â¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÆ’Ã‚Â¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÆ’Ã‚Â¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÆ’Ã‚Â¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÆ’Ã‚Â¼r `admin` und `vorstand` sichtbar, ÃƒÆ’Ã‚Â¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÆ’Ã‚Â¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÆ’Ã‚Â¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÆ’Ã‚Â¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÆ’Ã‚Â¤nde fÃƒÆ’Ã‚Â¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÆ’Ã‚Â¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

