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

## 2026-04-08
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
