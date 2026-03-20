# KGV Entwicklungslog

---

## Workaround & Vorgehensweise bei Änderungen

- Änderungen nur nach genauer Rückfrage und vollständiger Ansicht der Datei vornehmen.  
- Ich möchte immer genau wissen, **wo und wie** die Änderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor Änderungen vorgeschlagen werden.  

---

## 2026-02-18 – Projektstart

### Erledigt
- Neues WPF Projekt (.NET 8) erstellt
- MVVM + DI Struktur geplant und vorbereitet
- Dokumentationsstruktur angelegt

### Erkenntnisse
- Sauberer Neustart ist effizienter als Reparieren
- Klare Architektur spart Debugzeit

### Offene Aufgaben
- LoginView implementieren
- MainViewModel aufsetzen
- SupabaseService minimal implementieren

---

## 2026-02-18 – Architektur & MVVM

### Erledigt
- MVVM Ordnerstruktur erstellt
- BaseViewModel als ObservableObject vorbereitet
- NavigationService Interface erstellt
- CommunityToolkit.Mvvm eingebunden
- Microsoft.Extensions.DependencyInjection eingebunden
- MainViewModel Grundgerüst erstellt
- StartViewModel Grundgerüst erstellt

### Erkenntnisse
- Saubere Trennung von Verantwortlichkeiten reduziert Fehler
- UI-Logik muss komplett vom Datenzugriff getrennt sein

---

## 2026-02-18 – LoginView & StartViewModel

### Erledigt
- LoginView mit Email- und Passwortfeld erstellt
- Toggle-Passwort-Visibility implementiert
- StartViewModel: LoginCommand angelegt
- Passwort aus PasswordBox ausgelesen
- MessageBox-Feedback bei leerer Eingabe

### Erkenntnisse
- Binding allein reicht für Passwort nicht aus (PasswordBox ist nicht bindbar)
- Temporär über View auf PasswordBox zuzugreifen ist nötig

---

## 2026-02-18 – MainWindow & MainViewModel

### Erledigt
- MainWindow Layout erstellt mit Navigation links / Arbeitsbereich rechts
- Mitgliedersuche implementiert (WatermarkHelper statt PlaceholderText)
- Suchergebnisse als ListBox
- Untermenüs für ausgewähltes Mitglied dynamisch
- Export-Button implementiert
- MainViewModel mit ObservableCollections und Commands

### Erkenntnisse
- PlaceholderText ist in WPF nicht vorhanden → WatermarkHelper nutzen
- Commands sollten aus ViewModel angesteuert werden, Events nur temporär für UI

---

## 2026-02-18 – MemberViewModel & DTO

### Erledigt
- MemberDTO erstellt mit Stammdaten:
  - Vorname, Nachname, Strasse, Plz, Ort, Telefon, Email, Bemerkungen, WhatsappEinwilligung
- Interne/Admin-Felder:
  - AuthUserId, IstKGV, Aktiv, Role
- MemberViewModel bindet an DTO und ObservableProperties
- LoadFromDTO und SaveChanges implementiert
- PLZ-Property-Fix: Variablenname korrekt auf `Plz` geändert

### Erkenntnisse
- Non-Nullable Warnungen (CS8618) durch Initialisierung lösen oder `required` nutzen
- Alle Änderungen müssen sauber zwischen DTO und ViewModel synchronisiert werden

---

## 2026-02-18 – App.xaml.cs

### Erledigt
- DI Setup für Services implementiert
- Startup-Event sauber ersetzt, alte `Application_Startup` entfernt
- CS1061 Fehlerquelle behoben

### Erkenntnisse
- Wichtiger Workaround: Änderungen nur mit kompletter Dateiansicht vornehmen

---

## 2026-03-20 – Kontrollierter Wiederaufbau: Basis ladbar gemacht

### Erledigt
- `KGV.slnx` und die Projektdateien von `KGV.Core`, `KGV.Infrastructure` und `KGV.Wpf` gegen den aktuellen Ordnerstand geprüft
- Veralteten expliziten Compile-Eintrag für die fehlende Datei `KGV.Core\Security\PasswordPolicy.cs` aus `KGV.Core.csproj` entfernt
- Beschädigte binäre Datei `KGV.Infrastructure\Services\SupabaseService.cs` durch eine minimale textuelle Platzhalter-Implementierung ersetzt
- Fehlende `KGV.Wpf\AppSettings.cs` in kleiner WPF-tauglicher Form wiederhergestellt
- Build-Reihenfolge geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich
- Gesamter Workspace-Build geprüft: verbleibender Blocker aktuell in `KGV.Maui`

### Erkenntnisse
- Der erste echte Wiederaufbau-Blocker saß wie vermutet in `KGV.Core`: die Datei `PasswordPolicy.cs` fehlt physisch, der csproj-Eintrag war veraltet
- `KGV.Infrastructure` enthielt mindestens eine beschädigte Recovery-Datei, die nicht als C#-Quelltext vorlag
- `KGV.Wpf` ist projektstrukturell wieder brauchbar; der nächste WPF-Fehler war kein csproj-Problem, sondern eine fehlende `AppSettings`-Klasse
- Die Recovery-Spuren zeigen deutliche Verluste gegenüber dem neueren Stand, insbesondere bei WPF-Views, Core-Modellen und `KGV.ReleaseManager`
- `KGV.Maui` ist noch nicht baubar, weil mindestens `Resources\Splash\splash.svg` fehlt und lokal die Android-API 35 nicht installiert ist

### Erste Lückenliste
- Sicher fehlt: `KGV.Core\Security\PasswordPolicy.cs`
- Sicher fehlt: `KGV.ReleaseManager\KGV.ReleaseManager.csproj` bzw. das gesamte Projektverzeichnis im aktuellen Arbeitsstand
- Sicher fehlt in WPF u. a.: `ChangeEmailWindow`, `UserManagementView`, `ResetPasswordWindow`, `RfidEinrichtenView`, `RfidScanContextView`, `FaelligeZaehlerView`, `HomeView`, `ParzellenVerwaltungView`, `MemberWartungsvertraegeView`, `SaisonView`, `ZaehlerwechselAusbauView`, `ZaehlerwechselEinbauView`, `ZaehlerwechselScanView`, `ArbeitseinsaetzeVerwaltungView`, `BekanntmachungenVerwaltungView`, `ImpressumView`, `TermineVerwaltungView`, `WartungsvertraegeVerwaltungView`, `UpdateAvailableWindow`
- Sicher fehlt in Core laut Recovery/PDB-Spuren u. a.: `RfidScanContextRecord`, `WartungsvertragRecord`, `WartungsvertragZuordnungRecord`, `WasseruhrVorschauItem`, `UpdateCheckModels`, `AppUserDTO`, `DeleteUserAccountResult`, `InviteUserAccountResult`, `OAuthSignInStartResult`, `PrepareAddUserResult`, `ParzelleVerwaltungItem`
- Vorhanden bzw. wiederhergestellt: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, gültige `KGV.Wpf.csproj`, zentrale bestehende WPF-Views wie `DokumenteParzellenView`, `ZaehlerTauschDialog`, `GartenWasserView`, `GartenStromView`, `DokumenteView`

### Offene Aufgaben
- `KGV.Maui` separat stabilisieren (`splash.svg`, Android-SDK/API-Stand, danach Projektdatei/Assets erneut prüfen)
- `SupabaseService` fachlich aus Recovery-/Referenzspuren gezielt rekonstruieren, da aktuell nur ein Build-Platzhalter vorliegt
- Verlustliste mit `_Recovery\HotFileLists` und weiteren Recovery-Indizes schrittweise vertiefen

---

## 2026-03-20 – Kontrollierter Wiederaufbau: Core-/WPF-Bausteine gezielt rekonstruiert

### Erledigt
- Kleine fehlende Core-Typen rekonstruiert: `AppUserDTO`, `DeleteUserAccountResult`, `InviteUserAccountResult`, `OAuthSignInStartResult`, `PrepareAddUserResult`, `UpdateCheckModels`, `WasseruhrVorschauItem`, `RfidScanContextRecord`, `ParzelleVerwaltungItem`
- `PasswordPolicy` bewusst noch nicht rekonstruiert, da weiterhin keine belastbare aktuelle Verwendung oder Validierungslogik vorliegt
- Priorisierte WPF-Placeholder-ViewModels rekonstruiert: `UserManagementViewModel`, `ChangeEmailViewModel`, `ResetPasswordViewModel`, `RfidEinrichtenViewModel`, `RfidScanContextViewModel`, `FaelligeZaehlerViewModel`, `ParzellenVerwaltungViewModel`, `MemberWartungsvertraegeViewModel`, `WartungsvertraegeVerwaltungViewModel`, `HomeViewModel`, `ZaehlerwechselAusbauViewModel`, `ZaehlerwechselEinbauViewModel`, `ZaehlerwechselScanViewModel`
- Priorisierte WPF-Views/Windows rekonstruiert: `UserManagementView`, `ChangeEmailWindow`, `ResetPasswordWindow`, `RfidEinrichtenView`, `RfidScanContextView`, `FaelligeZaehlerView`, `ParzellenVerwaltungView`, `MemberWartungsvertraegeView`, `WartungsvertraegeVerwaltungView`, `HomeView`, `ZaehlerwechselAusbauView`, `ZaehlerwechselEinbauView`, `ZaehlerwechselScanView`
- `App.xaml` um passende `DataTemplate`-Zuordnungen für die neuen UserControl-ViewModels erweitert
- Build-Reihenfolge erneut geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich

### Erkenntnisse
- Die aktuell fehlenden kleinen Core-Typen werden im laufenden Code noch nicht direkt verwendet; sie stammen derzeit belastbar aus Recovery-/PDB-Spuren und wurden deshalb bewusst nur als neutrale Datencontainer rekonstruiert
- Die fehlenden priorisierten WPF-Bausteine sind aktuell ebenfalls noch nicht in die laufende Navigation eingehängt; die strukturelle Wiederherstellung erfolgt daher zunächst über ViewModels, Views und `DataTemplate`-Registrierung
- Der `SupabaseService` muss im nächsten fachlichen Block nicht pauschal, sondern methodenpriorisiert rekonstruiert werden

### Vorbereitete `SupabaseService`-Priorisierung
- Sofort nötig für bestehendes WPF/Auth/Stammdaten:
  - `GetMitgliedByIdAsync`
  - `GetMitgliederAsync`
  - `UpdateMitgliedAsync`
  - `TryLockMitgliedAsync`
  - `ReleaseLockMitgliedAsync`
  - `GetNebenmitgliedByHauptmitgliedIdAsync`
  - `GetArbeitsstundenAsync`
  - `UpdateArbeitsstundeAsync`
  - `GetSaisonRecordsAsync`
  - `GetMitgliedDokumenteAsync`
  - `GetParzelleDokumenteAsync`
- Nötig für RFID/Zähler und die jetzt vorbereiteten WPF-Bausteine:
  - `GetStromAblesungenAsync`
  - `GetWasserAblesungenAsync`
  - `SetStromzaehlerAusgebautAmAsync`
  - `SetWasserzaehlerAusgebautAmAsync`
  - `UpdateAblesungAsync`
  - später zusätzlich neue Methoden für RFID-/Wartungsvertrags-/Parzellenverwaltungslogik, sobald deren ViewModels fachlich rekonstruiert werden
- Später nötig für MAUI:
  - `AddArbeitsstundeAsync`
  - `GetUnapprovedArbeitsstundenByMitgliedAsync`
  - `UpdateOwnContactAsync`
  - `GetMitgliedByAuthUserIdAsync`

### Verbleibende Lücken
- Weiter offen: `PasswordPolicy`, `WartungsvertragRecord`, `WartungsvertragZuordnungRecord`, weitere Startseiten-/ReleaseManager-/Update-Komponenten
- Weiter offen in WPF: `SaisonView`, `UpdateAvailableWindow`, `ImpressumView`, `ArbeitseinsaetzeVerwaltungView`, `BekanntmachungenVerwaltungView`, `TermineVerwaltungView` und zugehörige fachliche ViewModels

---

## 2026-03-20 – Kontrollierter Wiederaufbau: `SupabaseService` für WPF/Auth/Stammdaten gezielt rekonstruiert

### Erledigt
- `KGV.Infrastructure/Services/SupabaseService.cs` für priorisierte WPF/Auth/Stammdaten-Methoden gezielt rekonstruiert:
  - `GetMitgliederAsync`
  - `GetMitgliedByIdAsync`
  - `UpdateMitgliedAsync`
  - `TryLockMitgliedAsync`
  - `ReleaseLockMitgliedAsync`
  - `GetNebenmitgliedByHauptmitgliedIdAsync`
  - `GetSaisonRecordsAsync`
  - `GetArbeitsstundenAsync`
  - `UpdateArbeitsstundeAsync`
  - `GetMitgliedDokumenteAsync`
  - `GetParzelleDokumenteAsync`
- Als direkt benötigte Hilfen zusätzlich rekonstruiert:
  - `GetSeasonsAsync`
  - `GetMitgliedByAuthUserIdAsync(Guid)`
  - `GetMitgliedByAuthUserIdAsync(string)`
  - `CreateDokumentSignedUrlAsync`
- Kleine WPF-Anschlusskorrekturen vorgenommen:
  - `Mobilnummer`-Mapping in `MemberDetailViewModel`, `NebenmitgliedDetailViewModel` und `AdminRoleViewModel` ergänzt
  - `MemberDetailViewModel.OnNavigatedToAsync()` toleriert die weiterhin offenen Parzellenmethoden vorläufig, damit Stammdaten-/Nebenmitglied-Pfade nicht mehr direkt am Platzhalter abbrechen
- Build-Reihenfolge erneut geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich

### Verwendete Quellen / Spuren
- Aktuelle Interfaces und reale WPF-Aufrufstellen (`MemberSearchViewModel`, `MemberDetailViewModel`, `NebenmitgliedDetailViewModel`, `AdminRoleViewModel`, `ArbeitsstundenViewModel`, `DokumenteViewModel`, `GartenDokumenteViewModel`)
- Vorhandene Records/DTOs: `MitgliedRecord`, `ArbeitsstundeRecord`, `ArbeitsstundeDTO`, `DokumentRecord`, `DocumentInfo`, `MemberDTO`
- Recovery-/PDB-Spur: `_Recovery\PdbDocumentLists\KGV.Infrastructure.txt` bestätigt weiterhin die ursprüngliche Existenz von `SupabaseService.cs`
- Paket-/API-Spuren aus lokal installierten Supabase-.NET-Paketen für `Set(...)`, `Update(...)` und `CreateSignedUrl(...)`
- Keine verwertbare Recovery-Quellkopie von `SupabaseService.cs` gefunden; `_Recovery\HotFileLists\KGV.Infrastructure.csproj.FileListAbsolute.txt` ist fachlich unbrauchbar / beschädigt

### Verbleibende Platzhalter in `SupabaseService`
- Weiterhin Platzhalter u. a. für:
  - Parzellen-/Belegungslogik
  - RFID-/Zählerlogik
  - `AddArbeitsstundeAsync`, `DeleteArbeitsstundeAsync`, Arbeitsstunden-Locking
  - Storage-/Dokument-Uploadthemen jenseits `CreateDokumentSignedUrlAsync`

### Risiken / Hinweise
- `KGV.Infrastructure` baut aktuell mit Nullable-Warnungen in `SupabaseService.cs`; funktional blockiert das den Build nicht, sollte aber im nächsten Service-Block bereinigt werden
- Der Workspace-Gesamtbuild scheitert weiterhin außerhalb dieses Blocks an `KGV.Maui` (`Resources\Splash\splash.svg`, lokales Android-SDK/API 35)
- Zusätzliche `XLS0414`-Meldungen aus dem Workspace-Gesamtcheck verhalten sich weiterhin wie Design-Time/XAML-Designer-Spuren; der gezielte `dotnet build` von `KGV.Wpf` ist erfolgreich

### Nächster Block
- Parzellen-/Belegungsmethoden im `SupabaseService` rekonstruieren, damit `MemberDetailViewModel.LoadParzellenAsync()` nicht mehr im Fallback läuft
- Danach gezielt RFID-/Zähler-Methoden und die dazugehörigen WPF-Bausteine anbinden

---

## 2026-03-20 – Kontrollierter Wiederaufbau: Auth-/UserManagement-Flows in WPF fachlich angeschlossen

### Erledigt
- `IAuthService` um die aktuell belastbar ableitbaren Auth-/UserManagement-Pfade erweitert:
  - `GetAppUsersAsync`
  - `ChangeEmailAsync`
  - `SendPasswordResetEmailAsync`
- `AuthService` fachlich angebunden für:
  - Laden einer belastbaren Benutzerliste aus `app_user` + `mitglied`
  - session-basierte E-Mail-Änderungsanforderung über die vorhandene Supabase-Auth-API
  - Passwort-Reset-Mail über den vorhandenen Supabase-Reset-Pfad
- `UserManagementViewModel` von Placeholder auf belastbaren Admin-Minimalfluss umgestellt:
  - Laden der Benutzer-/Mitgliedseinträge
  - Refresh
  - Dialogstart für E-Mail-Änderung und Passwort-Reset
- `ChangeEmailViewModel` / `ChangeEmailWindow` fachlich angeschlossen:
  - aktuelles E-Mail-Ziel
  - neue E-Mail erfassen
  - vorhandenen Supabase-Änderungspfad anstoßen
  - bewusst nur für den aktuell angemeldeten Benutzer freigegeben
- `ResetPasswordViewModel` / `ResetPasswordWindow` fachlich angeschlossen:
  - E-Mail-Ziel erfassen/vorbelegen
  - Reset-Mail anstoßen
- WPF-Navigation ergänzt:
  - `NavigationService` kann `UserManagementViewModel` instanziieren
  - `MainWindowViewModel` zeigt `Benutzerverwaltung` für Admin-/Rollenrechte an
- Build-Reihenfolge erneut geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich

### Behobener Buildfehler
- Der begonnene Block war zunächst an `KGV.Infrastructure/Authentication/AuthService.cs` blockiert:
  - Mehrdeutigkeit zwischen `Supabase.Gotrue.Client` und `Supabase.Client`
  - zusätzlich Namespace-Schatten durch `KGV.Infrastructure.Supabase`
- Behoben durch:
  - eindeutige Verwendung von `global::Supabase.Client`
  - Alias für `Supabase.Gotrue.UserAttributes`

### Verwendete Quellen / Spuren
- Aktuelle WPF-Aufrufstellen und View-Hüllen: `UserManagementViewModel`, `ChangeEmailViewModel`, `ResetPasswordViewModel`, `UserManagementView`, `ChangeEmailWindow`, `ResetPasswordWindow`
- Bestehende Modelle/Typen: `AppUserDTO`, `InviteUserAccountResult`, `DeleteUserAccountResult`, `PrepareAddUserResult`, `OAuthSignInStartResult`, `AppUserRecord`, `MitgliedRecord`
- Bestehende Auth-Architektur: `IAuthService`, `AuthService`, `LoginViewModel`, `App.xaml.cs`
- Recovery-/PDB-Spuren: `_Recovery\PdbDocumentLists\KGV.Wpf.txt`, `_Recovery\PdbDocumentLists\KGV.Core.txt`
- Lokale Supabase-Gotrue-Paketspuren für `IGotrueClient.Update(...)` und `ResetPasswordForEmail(...)`

### Weiter offen
- Noch nicht belastbar rekonstruiert im Auth-/UserManagement-Bereich:
  - echte Admin-Invite-/Delete-Flows trotz vorhandener Resultmodelle
  - OTP-/Bestätigungsabschluss im WPF-Client nach E-Mail-Wechsel
  - weitergehende Benutzerverwaltungsaktionen jenseits Listen-/Dialog-Minimalfluss
- `EmailBestaetigt` bleibt aktuell mangels belastbarer Quelle unverdrahtet

### Risiken / Hinweise
- Der angeschlossene E-Mail-Änderungspfad bleibt bewusst session-basiert; keine spekulative Admin-Änderung fremder Benutzer eingeführt
- `KGV.Infrastructure` baut weiterhin mit den bereits bekannten Nullable-Warnungen in `SupabaseService.cs`; kein Blocker für diesen Auth-Block
- Git hat in diesem Block weder beim Commit noch beim Push nach einem Konto gefragt

---

## 2026-03-20 – Kontrollierter Wiederaufbau: Parzellenverwaltung, Dokumente und Home fachlich angeschlossen

### Erledigt
- In `SupabaseService` die für WPF jetzt direkt benötigten Parzellen-Lesepfade rekonstruiert:
  - `GetParzelleByNumberAsync`
  - `GetAllParzellenAsync`
  - `GetCurrentBelegungForParzelleAsync`
  - `GetBelegungenForMitgliedAsync`
  - `GetAllParzellenBelegungenAsync`
- Den provisorischen `NotSupportedException`-Fallback in `MemberDetailViewModel.OnNavigatedToAsync()` entfernt; `LoadParzellenAsync()` läuft jetzt über echte Servicepfade
- `ParzellenVerwaltungViewModel` fachlich angeschlossen:
  - Laden der Parzellen
  - Laden der aktuellen Mitgliedsbezüge
  - Öffnen des zugeordneten Mitglieds
  - Öffnen der Parzellendokumente über den bestehenden `GartenDokumenteViewModel`-Pfad
- `ParzellenVerwaltungView` von Placeholder auf belastbare Listen-/Navigationsansicht umgestellt
- `HomeViewModel` / `HomeView` an die bestehende Navigation und Rechteauswertung angeschlossen:
  - Startseite zeigt die aktuell aus Rechten/Navigation belastbar ableitbaren Module
  - keine spekulativen Dashboard-Zahlen eingeführt
- `MainWindowViewModel` um `Startseite` und `Parzellenverwaltung` in der Hauptnavigation ergänzt
- `NavigationService` kann jetzt `HomeViewModel` und `ParzellenVerwaltungViewModel` gezielt erzeugen
- Dokumentpfade für Mitglieder/Parzellen blieben beim bestehenden Service-/Signed-URL-Pfad; keine neue Dateilogik eingeführt
- Build-Reihenfolge erneut geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich

### Verwendete Quellen / Spuren
- Aktuelle WPF-Aufrufstellen und ViewModels: `MemberDetailViewModel`, `MemberSearchViewModel`, `GartenDokumenteViewModel`, `MainWindowViewModel`, `ParzellenVerwaltungViewModel`, `HomeViewModel`
- Vorhandene Modelle/DTOs: `ParzelleRecord`, `ParzellenBelegungRecord`, `ParzellenBelegungDTO`, `ParzelleVerwaltungItem`, `DocumentInfo`
- Bestehende Dokumentpfade: `DokumenteViewModel`, `GartenDokumenteViewModel`, `CreateDokumentSignedUrlAsync`
- Recovery-/PDB-Spuren: `_Recovery\PdbDocumentLists\KGV.Wpf.txt`, `_Recovery\PdbDocumentLists\KGV.Infrastructure.txt`

### Ersetzte tolerante Pfade
- `MemberDetailViewModel` nutzt keinen stillen Parzellen-Fallback mehr
- `ParzellenVerwaltungViewModel` und `HomeViewModel` sind nicht mehr bloße Placeholder-Hüllen

### Weiter offen
- Parzellen-Bearbeitung/Zuweisung selbst bleibt weiterhin offen, solange `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` noch Platzhalter sind
- `DokumenteParzellenView` als separates Host-Fenster bleibt weiterhin unverdrahtet; fachlich genutzt wird aktuell der bestehende `GartenDokumenteViewModel`-/`GartenDokumenteListeView`-Pfad
- Keine Dashboard-Statistiken ergänzt, da dafür keine belastbaren Startseiten-Records oder Berechnungsregeln rekonstruiert wurden
- Demo-/Play-Store-Testdaten waren in diesem Block fachlich nicht ausschlaggebend, weil keine Summen-/Auswertungslogik ergänzt wurde

### Risiken / Hinweise
- `KGV.Infrastructure` baut weiter mit den bereits bekannten Nullable-Warnungen in `SupabaseService.cs`; kein Blocker für diesen Block
- Git hat in diesem Block weder beim Commit noch beim Push nach einem Konto gefragt

---

## 2026-03-20 – Pflichtabschluss: Zähler-/Ablesungsblock gezielt eingegrenzt und abgeschlossen

### Erledigt
- Den zu breit begonnenen Block bewusst auf den belastbar ableitbaren Zähler-/Ablesungsbereich eingegrenzt
- In `SupabaseService` die direkt von `GartenStromViewModel` und `GartenWasserViewModel` benötigten Methoden rekonstruiert:
  - `GetStromAblesungenAsync`
  - `GetWasserAblesungenAsync`
  - `GetActiveStromzaehlerAsync`
  - `GetActiveWasserzaehlerAsync`
  - `AddStromzaehlerAsync`
  - `AddWasserzaehlerAsync`
  - `SetStromzaehlerAusgebautAmAsync`
  - `SetWasserzaehlerAusgebautAmAsync`
  - `AddAblesungAsync`
  - `UpdateAblesungAsync`
- Nur direkt nötige Hilfsfunktionen ergänzt:
  - Datumsnormalisierung für Zähler-/Ablesungsoperationen
  - aktive-Zähler-Prüfung
  - Laden der Parzellen-Zähler
  - Mapping von `AblesungRecord` nach `ZaehlerAblesungDTO`
- Keine zusätzlichen WPF-Module oder neuen Placeholder-Views angefasst; die bestehenden Garten-Strom-/Wasser-Pfade nutzen jetzt die rekonstruierten Servicepfade
- Build-Reihenfolge erneut geprüft: `KGV.Core` erfolgreich, `KGV.Infrastructure` erfolgreich, `KGV.Wpf` erfolgreich

### Verwendete Quellen / Spuren
- Reale vorhandene Core-Modelle: `AblesungRecord`, `ZaehlerAblesungDTO`, `StromzaehlerRecord`, `WasserzaehlerRecord`
- Reale WPF-Aufrufstellen: `GartenStromViewModel`, `GartenWasserViewModel`
- Bestehende Service-Schnittstelle: `ISupabaseService`
- Keine RFID-/Wartungsvertragslogik allein aus PDB-Spuren abgeleitet

### Bewusst NICHT umgesetzt
- RFID blieb offen:
  - aktuell nur `RfidScanContextRecord` als kleiner Modelltyp belastbar vorhanden
  - keine belastbar vorhandenen Servicepfade oder ausreichend gestützte Hardware-/Zuordnungslogik im Workspace
- Wartungsverträge blieben offen:
  - `WartungsvertragRecord` und `WartungsvertragZuordnungRecord` sind im aktuellen Workspace nicht als belastbare Quellbasis verfügbar, trotz PDB-Spuren
  - daher keine spekulative Rekonstruktion der Vertragslogik
- Demo-/Play-Store-Testdaten-Ausschluss in Code bewusst nicht umgesetzt:
  - im eingegrenzten Zähler-/Ablesungsblock gibt es an den tatsächlich betroffenen Aufrufstellen keine belastbaren vorhandenen Erkennungsmerkmale oder Filter
- Wasseruhr-Vorschauwarnung bewusst offen gelassen:
  - die zugrunde liegenden belastbaren Aufrufstellen/Regelmarker sind im aktuellen Workspace nicht ausreichend vorhanden

### Risiken / Hinweise
- `KGV.Infrastructure` baut weiterhin mit bekannten Nullable-Warnungen in `SupabaseService.cs`; kein Blocker für diesen Zähler-Teilblock
- Git fragte beim Push nicht nach einem Konto
- Die Meldung `credential-manager-core` wurde in diesem Zähler-Teilblock nicht beobachtet

---

## Nächste Schritte

1. SupabaseService minimal implementieren, um Login und Stammdaten zu testen  
2. Dashboard-Layout fertigstellen, Inhalte aus MainViewModel laden  
3. Member-CRUD Funktionen implementieren (Anlegen, Bearbeiten, Löschen)  
4. Rollen- und Policy-System final im ViewModel/Service einbauen  
5. Exportfunktionen fertigstellen  
6. Warnung CS8618 in App.xaml.cs überprüfen / ggf. `required` verwenden  
7. PLZ-Fix in allen ViewModels konsistent anwenden  
8. Unit Tests für ViewModels vorbereiten
