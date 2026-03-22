# KGV Entwicklungslog

---

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
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; zusätzlich wurde `global.json` auf die lokal installierte SDK-Version `9.0.310` fixiert.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.


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
  - `GetBelegungenFürMitgliedAsync`
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
