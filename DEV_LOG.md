# KGV Entwicklungslog

---

## 2026-04-01 – Abschluss Block 2.2: MAUI `Gärten des Mitgliedes` / Parzellen-Detailansicht ohne weiteren Fachumbau sauber abgeschlossen

- Den realen Git-/Arbeitsstand erneut geprüft.
- Als echte Blockdateien von `2.2` bestätigt:
  - `KGV.Maui/Pages/AblesungErfassenPage.cs`
  - `KGV.Maui/Pages/DokumentePage.xaml.cs`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/State/ParzellenContextState.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
  - `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`
- Ehrlicher Befund im Abschlusslauf:
  - der fachliche Umbau des Blocks lag im aktuellen Arbeitsbaum bereits vor
  - der Mitgliedspfad wirkt bereits als stärkere Parzellen-Detailansicht
  - `Strom` und `Wasser` sind an den bestehenden Ablese-/Fallbackpfad angeschlossen
  - `Dokumente` laufen im Mitglieds-/Parzellenkontext über den vorhandenen Dokumentepfad
  - der Parzellenkontext wird beim Wechsel innerhalb des Mitgliedspfads bereits sauber nachgeführt
  - im Abschlusslauf zeigte sich kein direkter weiterer Fachfehler, der noch neue Codeänderungen an den Blockdateien erfordert hätte
- In diesem Abschlusslauf umgesetzt:
  - `DEV_LOG.md` und `KGV_Fortschrittslog_ausfuehrlich.md` ehrlich für den Abschluss nachgezogen
  - kein weiterer Fachumbau an den MAUI-Blockdateien
- Blockfremd lokal offen bzw. bewusst nicht Teil dieses Abschlussblocks:
  - `.github/copilot-instructions.md`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.ReleaseManager/KGV_Fortschritt_ausfuehrlich.md`
  - `AWR.bat`
  - `_secrets/`
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Offene Abgrenzung zu `2.3`:
  - kein neuer MAUI-Fachblock begonnen
  - weitergehender Ausbau über die in `2.2` geschlossene Parzellen-Detailwirkung hinaus bleibt bewusst Thema eines Folgeblocks

## 2026-04-01 – Abschluss Block 2.1: MAUI `Gärten des Mitgliedes` / Mitgliedsfokussierung / Haupt-Nebenmitglied-Verknüpfung ohne weitere Codeänderung abgeschlossen

- Den realen Git-/Arbeitsstand erneut geprüft.
- Als echte Blockdateien von `2.1` bestätigt:
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `KGV.Maui/Pages/MemberGardensPage.cs`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/State/ParzellenContextState.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
- Blockfremd lokal geändert blieben bewusst draußen:
  - `.github/copilot-instructions.md`
  - `KGV.Core/Models/ImpressumInfo.cs`
  - `KGV.Core/Models/ImpressumKontaktItem.cs`
  - `KGV.Maui/Pages/ImpressumPage.cs`
  - `KGV.Wpf/ViewModels/ImpressumViewModel.cs`
  - `KGV.Wpf/Views/ImpressumView.xaml`
  - untracked weiterhin `AWR.bat` und `Android_Wpf_release_batch_v4.bat`
- Ehrlicher Befund:
  - der begonnene Block `2.1` war in den fünf MAUI-Dateien bereits buildfähig vorhanden
  - kein direkter Abschlussfehler in den Blockdateien sichtbar
  - deshalb keine weiteren Codeänderungen an den fünf Blockdateien vorgenommen
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Ergebnis:
  - Block `2.1` nur technisch/ehrlich abgeschlossen
  - keine neuen Detailfunktionen oder Nebenbaustellen begonnen

## 2026-04-01 – MAUI Foto-Upload/Connectivity: Hinweis „Netzwerkstatus konnte nicht ermittelt werden“ minimal behoben

- Vor dem Fix den realen Blockzustand geprüft:
  - `KGV.Maui/Services/PendingPhotos/PendingPhotoUploadDecision.cs`
  - `KGV.Maui/Services/PendingPhotos/PendingPhotoSyncService.cs`
  - `KGV.Maui/Services/PendingPhotos/PendingPhotoService.cs`
  - `KGV.Maui/Settings/PhotoUploadPreferences.cs`
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
  - `KGV.Maui/Pages/AblesungErfassenPage.cs`
  - `KGV.Maui/Pages/PendingPhotoUploadsPage.cs`
  - `KGV.Maui/Services/PendingPhotos/PendingPhotoMenuState.cs`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Echter Befund:
  - der Hinweis `Netzwerkstatus konnte nicht ermittelt werden.` entstand zentral in `PendingPhotoUploadDecision.CanUploadNow(...)` im generischen `catch`
  - laut MAUI-Connectivity-Doku können `NetworkAccess` und `ConnectionProfiles` auf Android ohne `ACCESS_NETWORK_STATE` fehlschlagen
  - im aktuellen Repo fehlte diese Permission real in `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - dadurch lief der Connectivity-Check auf Android in den Catch-Pfad und lieferte die technische Meldung, obwohl die Pending-/Retry-Architektur an sich intakt war
  - zusätzlich war der `Unknown`-/unklare Status im bestehenden Entscheidungsblock zu grob behandelt; fachlich soll dann lieber pending geblieben werden
- Minimal umgesetzt:
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
    - `android.permission.ACCESS_NETWORK_STATE` ergänzt, damit MAUI `Connectivity` den Status auf Android sauber lesen kann
  - `KGV.Maui/Services/PendingPhotos/PendingPhotoUploadDecision.cs`
    - bestehende Logik beibehalten, aber robuster gemacht
    - `NetworkAccess.Unknown` führt jetzt nicht mehr zu einer technischen Fehlermeldung, sondern zu einer nutzerfreundlichen Pending-Aussage
    - bei WLAN-only und leerem/unklarem `ConnectionProfiles` wird kein unsicherer Sofort-Upload erzwungen; das Foto bleibt pending
    - Catch-Pfad liefert jetzt ebenfalls eine fachlich sinnvolle Pending-Meldung statt `Netzwerkstatus konnte nicht ermittelt werden.`
- Wichtig für die Blockgrenze:
  - keine neue Upload-Architektur
  - keine Änderung an Pending-Queue, Retry-/Sync-Flow oder Menülogik `Foto-Upload (X)`
  - keine Änderung am Timeout-/Resume-Block
  - kein WPF-Code angepasst; WPF nur gegengeprüft
- Validierung:
  - Test-Explorer: keine passenden automatisierten Tests für `PendingPhoto`/`PhotoUpload` gefunden
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen, u. a. in `KGV.Maui/Pages/HomeManagementPage.cs` sowie eine vorhandene Warnung in `KGV.Maui/Pages/ImpressumPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Fachliches Ergebnis:
  - bei normal ermittelbarem Netz greift die bestehende Upload-Entscheidung wieder sauber
  - bei unklarem Netzstatus bleibt das Foto lokal/pending statt unsicher hochgeladen zu werden
  - die Nutzerführung ist weniger technisch und widerspruchsfrei

## 2026-03-31 – Timeout-/Resume-Block fachlich abgerundet und sauber abgeschlossen

- Den realen Istzustand von Prompt `2/3` zuerst geprüft:
  - `KGV.Maui/App.xaml.cs`
  - `KGV.Maui/Settings/AppSettings.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - relevante flüchtige State-Klassen nur lesend
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Ehrlicher Befund vor dem Abschlussblock:
  - der fachliche Kern war bereits umgesetzt:
    - Background-Zeitstempel vorhanden
    - Resume-Dauer vorhanden
    - Reset auf Login bei mehr als 15 Minuten vorhanden
    - flüchtige States wurden bereits geleert
    - persistente Pending-Fotos blieben bereits erhalten
  - offen blieb nur noch kleiner Lifecycle-/Guardrail-Feinschliff im zentralen Resume-Pfad
- Minimal umgesetzt:
  - `KGV.Maui/App.xaml.cs`
    - Resume-Verarbeitung jetzt zusätzlich in kleinem `try/catch`, damit ein Fehler im Resume-Handler nicht unkontrolliert durchläuft
    - zusätzliche kleine Lifecycle-Marker für gerätetest-nahe Nachvollziehbarkeit ergänzt:
      - `APP_RESUME_TIMEOUT_NO_TIMESTAMP`
      - `APP_RESUME_TIMEOUT_WITHIN_THRESHOLD`
      - `APP_RESUME_TIMEOUT_SKIPPED_NO_SESSION`
      - `APP_RESUME_TIMEOUT_ALREADY_RUNNING`
      - `APP_RESUME_TIMEOUT_COMPLETED`
    - parallele bzw. doppelte Timeout-Resets werden jetzt weiterhin verhindert und zusätzlich explizit geloggt
- Wichtig für die Blockgrenze:
  - keine Änderung am Foto-Upload-Block
  - keine Änderung an Pending-Foto-Queue/-Sync-Logik
  - keine Änderung am Wartungsvertragsblock
  - keine Änderung an blockfremden lokalen Dateien
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Ehrlicher Abschlussstand:
  - Resume unter 15 Minuten => normales Weiterarbeiten
  - Resume über 15 Minuten => sauberer Root-Reset auf Login
  - doppelte Resets werden abgefangen
  - Gerätetest ist über die zusätzlichen Lifecycle-Marker klarer nachvollziehbar
  - Timeout-/Resume-Thema für diesen 3/3-Block fachlich abgeschlossen => ja

## 2026-03-31 – Resume-Timeout ab 15 Minuten sauber auf Login/Root zurückgesetzt

- Den realen Zwischenstand nach Prompt `1/3` zuerst geprüft:
  - `AppSettings.LastBackgroundedAtUtc` war bereits vorhanden
  - `App.xaml.cs` berechnete beim Window-`Resumed` die Hintergrunddauer bereits und loggte sie
  - der eigentliche Timeout-Reset auf Login war noch nicht umgesetzt
  - im Arbeitsbaum lagen für diesen Block nur die direkt betroffenen Session-/Lifecycle-Dateien offen; untracked `AWR.bat` und `Android_Wpf_release_batch_v4.bat` blieben bewusst unangetastet
- Für den Block gezielt geprüft:
  - `KGV.Maui/App.xaml.cs`
  - `KGV.Maui/Settings/AppSettings.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/State/UserContextState.cs`
  - weitere flüchtige MAUI-State-Klassen (`MemberContextState`, `ParzellenContextState`, `HomeContextState`, `ArbeitsstundenReviewState`, `ArbeitseinsaetzeManagementState`, `ArbeitseinsaetzeUserState`, `TermineUserState`, `ZaehlerwechselWorkflowState`)
  - vorhandene Pending-Foto-Pfade (`PendingPhotoQueue`, `PendingPhotoStorage`, `PendingPhotoSyncService`)
  - minimal nötiger Auth-Pfad `KGV.Core/Interfaces/IAuthService.cs` / `KGV.Infrastructure/Authentication/AuthService.cs`
- Minimal umgesetzt:
  - `KGV.Maui/App.xaml.cs`
    - zentrale Resume-Auswertung jetzt mit fester Schwelle `15` Minuten
    - Verhalten:
      - `<= 15` Minuten Hintergrundzeit => normales Resume
      - `> 15` Minuten Hintergrundzeit => Timeout-Reset
    - bei Timeout wird jetzt zentral:
      - aktive Sitzung verworfen
      - nur flüchtiger MAUI-Session-/Navigationszustand geleert
      - Root über den bestehenden `CreateRootPage()`-/`SwitchToCurrentRootAsync()`-Pfad auf `LoginPage` zurückgesetzt
    - zusätzlicher Lifecycle-Logmarker für den ausgelösten Timeout-Reset
  - `KGV.Maui/Settings/AppSettings.cs`
    - kleiner Reset-Pfad für den persistierten Background-Timestamp ergänzt, damit Resume-Entscheidungen nicht mit alten Werten weiterlaufen
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
    - kompakter, nicht-technischer Hinweis nach Timeout-Reset möglich:
      - `Die App war zu lange im Hintergrund. Bitte erneut anmelden.`
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
    - kleiner produktiver `LogoutAsync()`-Pfad ergänzt, damit die aktive Supabase-Sitzung beim Resume-Timeout sauber verworfen werden kann
- Wichtig für die Blockgrenze:
  - Pending-Fotos, Pending-Queue, WLAN-only-Schalter und sonstige persistente Einstellungen bleiben erhalten
  - es werden nur flüchtige Session-/Navigationszustände zurückgesetzt
  - kein Eingriff in den Foto-Upload-Block außer der bewussten Nicht-Beschädigung seiner persistenten Queue
  - kein WPF-Umbau außer dem minimal nötigen Shared-/Auth-Vertrag
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Ehrliche Abschlussprüfung:
  - App startet normal => code-seitig ja
  - Resume unter 15 Minuten bleibt normal => code-seitig ja
  - Resume über 15 Minuten führt sauber auf Login => code-seitig ja
  - Pending-Fotos bleiben erhalten => code-seitig ja
  - Shell-/Login-Initialisierung nicht regressiv geändert => build- und code-seitig ja

## 2026-03-31 – Hängengebliebenen MAUI-Foto-Upload-Diagnoseblock sauber aufgenommen und abgeschlossen

- Den echten Zwischenstand des hängen gebliebenen Blocks zuerst geprüft:
  - `git status --short --branch` => `## main...origin/main`
  - blockbezogen offen waren real:
    - `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
    - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - `KGV.Core/Models/PhotoUploadTestResult.cs`
    - `DEV_LOG.md`
    - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `KGV.Core/Interfaces/IPhotoUploadTestService.cs` war im aktuellen Git-Stand unverändert; dort hing nichts offen
  - blockfremde lokale Änderungen u. a. in `KGV.Core/Interfaces/IAuthService.cs`, `KGV.Core/Models/AblesungInsertRecord.cs`, `KGV.Core/Models/InviteUserAccountResult.cs`, `KGV.Maui/KGV.Maui.csproj`, `KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs`, `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`, `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`, `KGV.Wpf/KGV.Wpf.csproj` sowie ungetrackten Batch-/Modelldateien blieben bewusst draußen
- Ehrliche Einordnung des Zwischenstands:
  - der vorige Lauf hatte den Kern des Diagnoseblocks bereits lokal begonnen
  - vorhanden waren schon:
    - wiederverwendeter `HttpClient`
    - Timeout von 3 Minuten
    - Diagnosecodes/Failure-Stages im Ergebnis
    - stufenbezogene Fehlerbehandlung in `PhotoUploadTestService`
    - kompakte Anzeige des Diagnosecodes im MAUI-Testbereich
  - offen bzw. noch nicht sauber abgeschlossen waren:
    - präzisere Fehlerdiagnose im gestarteten Restpfad (`multipartContentLength`, saubere `FailureStage` beim Response-Lesen / HTTP-Fehler)
    - kompakteres UI-Fallback statt roher Exception-Meldung im MAUI-Testbereich
    - Build-/Log-/Git-Abschluss des Blocks
- Nur den offenen Rest dieses Blocks fertiggestellt:
  - `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
    - zusätzliche Vorab-Logs für `UPLOAD_AUTH_TOKEN_MISSING` und `UPLOAD_CONFIG_MISSING`
    - Fehlerlogs enthalten jetzt auch `multipartContentLength`, wenn im Requestpfad vorhanden
    - `FailureStage` wird bei `UPLOAD_RESPONSE_READ_FAIL` jetzt korrekt auf `response_read` gesetzt
    - HTTP-Fehler nach gelesener Response tragen jetzt ebenfalls die korrekte Fehlerphase statt eines unscharfen Restzustands
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - UI-Fallback bei unerwartetem Aufrufer-/UI-Fehler jetzt kompakt mit `UPLOAD_UNHANDLED` statt rohem Exception-Text
    - keine Stacktraces und keine lange technische Fehlermeldung im UI
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - verbleiben nur die bekannten Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
- Ehrliche Abschlussprüfung:
  - hängengebliebener Block jetzt vollständig abgeschlossen => ja
  - Uploadfehler genauer diagnostizierbar => ja
  - UI zeigt kompakten Diagnosehinweis => ja

## 2026-03-31 – MAUI Foto-Upload Diagnose für `Socket closed` verbessert

- Den realen Git-Stand zuerst geprüft:
  - `main` liegt auf `origin/main`
  - gleichzeitig bestehen weiterhin blockfremde lokale Änderungen u. a. in `KGV.Core/Interfaces/IAuthService.cs`, `KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs`, `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`, `KGV.Maui/ViewModels/RfidScanContextViewModel.cs` sowie mehrere ungetrackte Batch-/Modelldateien
  - diese Fremdstände wurden bewusst nicht als Grundlage verwendet und nicht mitgezogen
- Für den Block gezielt geprüft:
  - `KGV.Core/Interfaces/IPhotoUploadTestService.cs`
  - `KGV.Core/Models/PhotoUploadTestResult.cs`
  - `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
- Ehrlicher Befund vor dem Fix:
  - der produktive Uploadpfad lief direkt über `PhotoUploadTestService.UploadAsync(...)`
  - dort wurde pro Upload ein neuer `HttpClient` erzeugt
  - es war kein expliziter Timeout gesetzt
  - Multipart wurde direkt erzeugt und die Antwort anschließend sofort per `ReadAsStringAsync()` gelesen
  - Fehler wurden bisher praktisch auf `_logger.LogWarning(ex, ...)` plus `ex.Message` verkürzt
  - dadurch kam in MAUI beim Fehler `Socket closed` kein belastbarer technischer Bruchpunkt in UI oder Log an
- Minimal umgesetzt:
  - `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
    - bestehender Uploadpfad bleibt erhalten; keine neue Upload-Architektur
    - Service nutzt jetzt einen wiederverwendeten `HttpClient` mit explizitem Timeout von 3 Minuten statt pro Request einen neuen Client zu bauen
    - Upload wird stufenbezogen diagnostiziert:
      - `before_request`
      - `send`
      - `response_read`
    - belastbare Entwicklerlogs ergänzt mit:
      - Diagnosecode
      - Stage
      - Function-Name / URL
      - Dateiname
      - Dateigröße
      - Multipart-Content-Length wenn berechenbar
      - Exception-Typ
      - Exception-Message

## 2026-03-31 – MAUI Google-Play-Readiness im Repo geprüft (Signing-Secrets entfernt, AAB-Publish verifiziert)

- Vor dem Block den realen Git-Stand geprüft:
  - `main` liegt auf `origin/main`
  - ungetrackt bleiben wie gehabt lokal: `AWR.bat`, `_secrets/` (nicht committen)
- Direkte Repo-Befunde (nur belastbare Punkte):
  - `KGV.Maui/KGV.Maui.csproj` targetet `net9.0-android`, `AndroidTargetSdkVersion=35`, `AndroidMinSdkVersion=21`.
  - `ApplicationId=de.kgv.oberrothenbach` ist gesetzt.
  - Versionsfelder sind vorhanden: `ApplicationDisplayVersion` / `ApplicationVersion`.
  - Release ist als AAB-Pfad vorgesehen.
- Repo-seitiger Blocker vor Fix:
  - In `KGV.Maui.csproj` waren Signing-Details als Klartext bzw. Secret-Pfad hinterlegt:
    - `AndroidSigningStorePass` / `AndroidSigningKeyPass` (Klartext)
    - `AndroidSigningKeyStore` zeigte direkt in `..\_secrets\...`
  - Zusätzlich war Debug teilweise auf AAB + Signing verdrahtet (unnötig riskant/unklar).
- Minimaler Fix:
  - Hardcoded Signing-Passwörter und Keystore-Pfad aus `KGV.Maui.csproj` entfernt.
  - Debug wieder auf APK ohne Signing-Zwang zurückgeführt.
  - Release-AAB-Einstellung beibehalten; Release-Defaults (`AndroidCreatePackagePerAbi=false`, `AndroidUseAapt2=true`) sauber im Release-Block.
- Lokale Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` erfolgreich (nur bekannte Warnungen in `HomeManagementPage.cs`).
  - `dotnet publish ... -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=false` erfolgreich.
    - AABs liegen unter `KGV.Maui\bin\Release\net9.0-android\publish\` (`de.kgv.oberrothenbach*.aab`).

## 2026-03-31 – Release Manager: Android Signing nach csproj-Bereinigung weiterhin sauber externalisiert

- Ausgangslage:
  - `KGV.Maui.csproj` enthält keine hardcodierten Signing-Passwörter und keinen festen `_secrets`-Keystorepfad mehr.
  - Android-Signing muss im `KGV.ReleaseManager` weiter über lokale Settings (Keystore/Alias) + Laufzeitpasswörter erfolgen.
- Befund im Repo:
  - `BuildCommandService.CreateAndroidPublishCommand(...)` transportiert Passwörter bereits korrekt als Prozess-Umgebungsvariablen.
  - Gleichzeitig waren Keystore/Alias/StorePass im Release-Lauf als Pflichtbedingungen verdrahtet.
  - `AndroidKeyStore=true` + Signing-Properties wurden immer übergeben.
- Minimal umgesetzt:
  - Signing wird im Publish-Command nur dann aktiviert (inkl. `AndroidKeyStore=true` + Signing-Properties), wenn Keystore + Alias + Passwortdaten tatsächlich vorhanden sind.
  - Fehlen Signing-Daten, wird `AndroidKeyStore=false` gesetzt und der Release-Lauf liefert Hinweise statt harter Fehler.
  - Keine Secrets werden geloggt oder im Repo hinterlegt.
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` erfolgreich.
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` erfolgreich.
      - InnerException-Typ
      - InnerException-Message
      - HTTP-Status wenn schon vorhanden
      - Retry ja/nein
    - kompakte Diagnosecodes ergänzt, u. a.:
      - `UPLOAD_SOCKET_CLOSED`
      - `UPLOAD_TIMEOUT`
      - `UPLOAD_RESPONSE_READ_FAIL`
      - `UPLOAD_SEND_FAIL`
      - `UPLOAD_HTTP_ERROR`
    - Response-Lesen separat abgesichert, damit ein Bruch beim Lesen nicht mehr als unscharfer Gesamtfehler endet
    - kleiner enger Retry nur für diesen Testpfad bei transienten Transportfehlern im Send-Pfad ergänzt (maximal 1 Wiederholung)
  - `KGV.Core/Models/PhotoUploadTestResult.cs`
    - kompakte Rückgabefelder `DiagnosticCode` und `FailureStage` ergänzt
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - Statusmeldung bleibt kurz, zeigt bei Fehlern jetzt aber zusätzlich den kompakten Diagnosecode
    - Ergebnisbereich zeigt bei Fehlern ebenfalls die Diagnosecode-Zeile, ohne Stacktraces ins UI zu kippen
- Fachliches Ergebnis:
  - der echte Uploadfehler `Socket closed` ist jetzt im bestehenden Testpfad deutlich genauer eingrenzbar
  - UI bleibt knapp und nutzbar, liefert aber einen kompakten technischen Hinweis
  - der Uploadpfad ist technisch robuster, ohne den großen Queue-/Hintergrundumbau vorzuziehen
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` folgt nach den Code- und Logänderungen in diesem Block

## 2026-03-31 – MAUI Foto-Testbutton und Versionshinweis ergänzt

- Den realen Git-Stand zuerst geprüft:
  - `main` liegt auf `origin/main`
  - gleichzeitig bestehen blockfremde lokale Änderungen u. a. in `KGV.Core/Interfaces/IAuthService.cs`, `KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs`, `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`, `KGV.Maui/ViewModels/RfidScanContextViewModel.cs` sowie mehrere ungetrackte Batch-/Modelldateien
  - diese Fremdstände wurden bewusst nicht als Grundlage verwendet
- Für den Block gezielt geprüft:
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Infrastructure/Services/PhotoUploadTestService.cs`
  - `KGV.Core/Interfaces/IPhotoUploadTestService.cs`
  - `KGV.Core/Models/PhotoUploadTestRequest.cs`
  - `KGV.Core/Models/PhotoUploadTestResult.cs`
  - ergänzend der bestehende WPF-Referenzpfad `KGV.Wpf/ViewModels/FotoUploadTestViewModel.cs`
- Ehrlicher Befund vor dem Fix:
  - der produktive Uploadpfad für `kgv-upload-photo` ist bereits im Shared-Service `PhotoUploadTestService` vorhanden
  - der Service liefert für den Testpfad bereits sichtbare Rückgabedaten wie `file_id`, `file_name`, `relative_path`, HTTP-Status und Rohantwort
  - in MAUI gab es auf dem echten committed Produktpfad aber noch keinen kleinen Diagnoseeinstieg dafür
  - eine bestehende frühere MAUI-Testseite liegt nur noch im Archiv und sollte laut Blockgrenze gerade nicht als eigener großer neuer Seitenpfad reaktiviert werden
  - eine sichtbare Versionsanzeige aus dem realen AppInfo-Pfad war in MAUI bisher nicht vorhanden
- Minimal umgesetzt:
  - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - kleinen Diagnosebereich `Foto-Upload testen` direkt auf dem stabilen Admin-/Vorstand-Pfad `Ablesen` ergänzt
    - zwei kleine Einstiege für echten Gerätetest:
      - `Foto aufnehmen`
      - `Foto hinzufügen`
    - vorhandenen Shared-Uploadservice `IPhotoUploadTestService` wiederverwendet; keine neue Backendlogik und keine neue Edge Function
    - sichtbare Rückgabe knapp ergänzt:
      - HTTP-Status
      - `FileId`
      - `Dateiname`
      - `Pfad`
      - bei Fehlern zusätzlich Rohantwort
    - der normale Ablesen-Produktfluss bleibt unverändert; der Testbereich ist nur ein kleiner separater Diagnoseblock unter den normalen Kacheln
  - `KGV.Maui/MauiProgram.cs`
    - `AblesenOverviewPage` für DI registriert
  - `KGV.Maui/AdminShell.cs`
    - `Ablesen` zieht die Seite jetzt sauber über DI, damit der Testbereich den bestehenden Uploadservice direkt nutzen kann
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
    - kleiner sichtbarer Versionshinweis ergänzt
    - Version wird real aus `AppInfo.Current.VersionString` und `AppInfo.Current.BuildString` abgeleitet, nicht hart codiert
- Fachliches Ergebnis:
  - auf einem echten committed MAUI-Produktpfad gibt es jetzt einen kleinen Foto-Test-/Diagnosebereich
  - derselbe bestehende Uploadpfad wie im WPF-Testkontext ist damit auf Gerät testbar
  - die App zeigt ihre Version jetzt sichtbar und nachvollziehbar direkt aus dem realen Produktpfad an
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - ehrliche Abschlussprüfung:
    - Foto-Testbutton vorhanden => ja
    - Uploadpfad damit testbar => ja
    - Versionshinweis sichtbar => ja

## 2026-03-31 – MAUI Ablesen UI nach erfolgreichem Scan beruhigt

- Den realen MAUI-Ablesen-Pfad zuerst direkt geprüft:
  - `KGV.Maui/Pages/AblesungErfassenPage.cs`
  - ergänzend die bestehende gemeinsame Scanlogik in `KGV.Maui/Pages/RfidScanWorkflowPage.cs` und `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`
- Ehrlicher Befund vor dem Fix:
  - die produktive Ablesen-Seite zeigte nach erfolgreichem aktivem Scan weiter parallel den festen Bereich `RFID-Scan`, den Fallback-Bereich und zusätzlich noch die Einordnungssektion
  - dadurch war die UI im aktiven Ablese-Zustand unnötig unruhig, obwohl fachlich nur noch `Ablesekontext` und `Ablesen` relevant sind
  - der Scan- und Ablesepfad selbst funktionierte bereits; betroffen war nur die Sichtbarkeitslogik der Seite
- Minimal umgesetzt:
  - in `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - scanbezogene Einleitung in eine eigene sichtbarkeitssteuerbare Labelinstanz gezogen
    - feste Sections `RFID-Scan` und `Fallback ohne NFC` als eigene Felder referenzierbar gemacht
    - nach einem erfolgreichen aktiven Ablese-Kontext werden jetzt ausgeblendet:
      - scanbezogene Einleitung
      - `RFID-Scan`
      - `Fallback ohne NFC`
      - `Einordnung`
    - sichtbar bleiben dann nur noch:
      - `Ablesekontext`
      - `Ablesen`
  - kein Foto-Flow-Umbau
  - kein Backend-/Supabase-Umbau
  - kein neuer RFID- oder Zählerwechsel-Fachflow
- Fachliches Ergebnis:
  - nach erfolgreichem aktivem Scan ist die Ablesen-UI jetzt deutlich ruhiger
  - der Nutzer sieht im Arbeitszustand nur noch den relevanten Kontext und das eigentliche Ableseformular
  - der bestehende funktionierende Scan- und Speicherpfad bleibt unverändert erhalten
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`

## 2026-03-31 – Zähleranlage auf gemeinsame Tabelle `zaehler` umgestellt

- Den realen Repo-/Git-Stand zuerst geprüft und den Block nur auf den gemeinsamen Zählerpfad plus Logdateien begrenzt.
- Direkt geprüft wurden:
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Core/Models/StromzaehlerInsertRecord.cs`
  - `KGV.Core/Models/WasserzaehlerInsertRecord.cs`
  - `KGV.Core/Models/StromzaehlerRecord.cs`
  - `KGV.Core/Models/WasserzaehlerRecord.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`
  - die realen Shared-Verwendungen in WPF/MAUI für `AddStromzaehlerAsync(...)`, `AddWasserzaehlerAsync(...)`, `GetActiveStromzaehlerAsync(...)`, `GetActiveWasserzaehlerAsync(...)`, `SetStromzaehlerAusgebautAmAsync(...)`, `SetWasserzaehlerAusgebautAmAsync(...)`
- Ehrlicher Befund vor dem Fix:
  - der produktive Zählereinbau schrieb im Shared-Service noch gegen die alten Tabellen `stromzaehler` / `wasserzaehler`
  - der neue produktive DB-Vertrag liegt aber auf der gemeinsamen Tabelle `public.zaehler`
  - dadurch war der reale Produktivfehler nicht Validierung, sondern der veraltete Tabellenzugriffspfad
  - zusätzlich blockiert der produktive Trigger den Insert, wenn für die Parzelle keine passende RFID für das Medium hinterlegt ist
- Minimal umgesetzt:
  - neue gemeinsame Core-Modelle:
    - `KGV.Core/Models/ZaehlerRecord.cs`
    - `KGV.Core/Models/ZaehlerInsertRecord.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
    - neue gemeinsame private Kernlogik `AddZaehlerCoreAsync(...)`
    - bestehende öffentliche Methoden `AddStromzaehlerAsync(...)` / `AddWasserzaehlerAsync(...)` bleiben erhalten und mappen intern nur noch auf den gemeinsamen `zaehler`-Insert
    - es werden nur die für `zaehler` nötigen Felder gesendet:
      - `parzelle_id`
      - `medium`
      - `zaehlernummer`
      - `eichdatum`
      - `eingebaut_am`
    - `id`, `status` und `eichfaellig_am` werden nicht mehr unnötig aus der App gesetzt
    - `eichdatum` wird fachlich aus dem eingegebenen Jahr auf `01.01.<Jahr>` normalisiert
    - aktive Zählerauflösung und Ausbaupfade wurden intern ebenfalls auf `zaehler` umgebogen, damit bestehende Strom-/Wasser-Methoden für WPF/MAUI kompatibel bleiben
  - RFID-Vorbedingung jetzt fachlich sauber behandelt:
    - Vorabprüfung gegen `parzelle.rfid_strom` / `parzelle.rfid_wasser`
    - verständliche Meldung statt technischem Rohfehler, z. B.:
      - `Für diese Parzelle ist noch keine RFID für Wasser hinterlegt. Bitte zuerst RFID einrichten.`
    - zusätzlich kleine Trigger-/PostgREST-Übersetzung als Sicherheitsnetz, falls DB-seitig doch noch derselbe Fehler zurückkommt
- Wichtig für die Blockgrenze:
  - kein Foto-Flow-Umbau
  - keine DB-Migration
  - keine Triggeränderung
  - keine neue WPF-Fachbaustelle
  - `ZaehlerwechselEinbauPage` musste nicht separat umgebaut werden, weil sie bereits die bestehenden Shared-Methoden nutzt und damit automatisch auf dem neuen `zaehler`-Pfad landet
- Fachliches Ergebnis:
  - der produktive Zählereinbau schreibt nicht mehr gegen `stromzaehler` / `wasserzaehler`, sondern gegen `zaehler`
  - der Folgeflow `Zähler anlegen -> Anfangsablesung` bleibt erhalten
  - der alte Tabellennamenfehler ist damit code-seitig geschlossen
  - die RFID-Fehlersituation wird fachlich verständlich abgefangen statt als roher technischer DB-Fehler an Nutzer weiterzureichen
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - bewusst nicht behauptet wurde ein produktiver DB-Schreibtest; der Code ist jetzt aber für den realen Testkandidaten `Parzelle 6` auf dem aktuellen Stand fachlich vorbereitet
- Abschlussstand:
  - der Block ist bereit für Commit und Push mit strikt auf die `zaehler`-Umstellung begrenzten Dateien
  - der erste sinnvolle echte Produktivtest ist `Parzelle 6`, weil dort die passende RFID bereits hinterlegt ist
  - dieser Realtest wird hier nur vorbereitet und nicht als bereits durchgeführt behauptet

## 2026-03-30 – OTP Passwort-Neusetzen: Passwortfelder nach OTP in MAUI entsperrt

- Den realen Repo-/Git-Stand zuerst geprüft; blockfremde offene Änderungen bewusst nicht als Grundlage verwendet.
- Den tatsächlichen OTP-/SetPassword-Clientpfad geprüft:
  - MAUI: `KGV.Maui/Pages/LoginPage.xaml.cs`
  - WPF: `KGV.Wpf/ViewModels/LoginViewModel.cs`, `KGV.Wpf/Views/LoginWindow.xaml`, `KGV.Wpf/Views/LoginWindow.xaml.cs`
  - WPF-Resetdialog `ResetPasswordWindow` nur zur sauberen Abgrenzung gegengelesen
- Ehrlicher Befund:
  - der produktiv betroffene Restfehler sitzt im MAUI-Loginpfad nach erfolgreicher OTP-Prüfung
  - `ShowSetPassword()` schaltet zwar den sichtbaren Reset-Schritt ein, aber die beiden `Entry`-Controls für neues Passwort und Passwortwiederholung wurden bereits initial mit `IsVisible = false` erzeugt
  - dadurch blieb nach erfolgreichem OTP der umgebende Feldcontainer sichtbar, die eigentlichen Eingabefelder selbst aber unsichtbar/nicht bedienbar
  - der WPF-Pfad zeigte denselben echten Statefehler im aktuellen Repo-Stand nicht
- Minimal korrigiert:
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
    - `newPasswordEntry` und `confirmPasswordEntry` nicht mehr dauerhaft mit `IsVisible = false` erzeugt
    - nach `ShowSetPassword()` wird der Fokus direkt auf `newPasswordEntry` gesetzt
- Fachliches Ergebnis:
  - nach erfolgreicher OTP-Prüfung sind die Felder für neues Passwort und Passwortwiederholung im produktiven MAUI-Pfad wieder editierbar
  - der bestehende Ablauf bleibt unverändert:
    - OTP prüfen
    - neues Passwort setzen
    - zurück zum Login
    - erneute Anmeldung mit neuem Passwort

## 2026-03-30 – First-Login Edge Function produktiv deployt und live verifiziert

- Den bereits umgesetzten Serverpfad nicht erneut umgebaut, sondern nur real deployt und gegen das echte Supabase-Projekt geprüft.
- Geprüft wurden:
  - `supabase/functions/kgv-request-first-login-otp/index.ts`
  - `supabase/functions/kgv-request-first-login-otp/deno.json`
  - `supabase/config.toml`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
- Reale Deploy-Voraussetzungen bestätigt:
  - Function-Dateien vollständig vorhanden
  - `config.toml` hängt die Function korrekt ein
  - `verify_jwt = false` ist für den Vor-Auth-Endpunkt korrekt gesetzt
  - die Function nutzt serverseitig `SUPABASE_URL` und `SUPABASE_SERVICE_ROLE_KEY`
  - Remote-Secrets im Zielprojekt vorhanden (`supabase secrets list` geprüft)
- Produktiv deployt:
  - `supabase functions deploy kgv-request-first-login-otp --project-ref itjcabiibuodkxayhvjq`
  - `supabase functions list --project-ref itjcabiibuodkxayhvjq` bestätigt danach:
    - `kgv-request-first-login-otp`
    - `ACTIVE`
    - `VERSION 1`
- Live-Verifikation des veröffentlichten Endpunkts direkt per HTTP durchgeführt:
  - POST auf `https://itjcabiibuodkxayhvjq.supabase.co/functions/v1/kgv-request-first-login-otp`
  - mit Repo-`PublishableKey`
  - Rückgabe war generisch und kontrolliert:
    - `success = false`
    - `diagnosticCode = OTP_FIRST_LOGIN_EDGE_REJECTED`
  - wichtig: dabei trat **kein** früherer Client-Fehler `permission denied for table mitglied` mehr auf
- Ehrliche Abgrenzung:
  - eine echte Wiederholung mit der historisch betroffenen Mail war in diesem Block nicht automatisiert möglich, weil diese Mail im Repo-/Workspace-Stand nicht belastbar vorlag
  - damit ist der serverseitige First-Login-Pfad produktiv deployt und technisch live verifiziert, ein echter OTP-Versandfall für genau diese Mail aber nicht zusätzlich belegt
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 – OTP Erstlogin auf serverseitige Edge Function umgestellt

- Den realen Blockstand zuerst auf dem aktuellen Repo geprüft:
  - bestehender Function-Stil unter `supabase/functions/kgv-upload-photo`
  - aktueller OTP-Erstloginpfad in `KGV.Infrastructure/Authentication/AuthService.cs`
  - MAUI-Login in `KGV.Maui/Pages/LoginPage.xaml.cs`
  - WPF-Login in `KGV.Wpf/ViewModels/LoginViewModel.cs`
- Neue Edge Function ergänzt:
  - `supabase/functions/kgv-request-first-login-otp/index.ts`
  - öffentlich nur als Function-Endpunkt, aber serverseitig mit `SUPABASE_SERVICE_ROLE_KEY`
  - prüft serverseitig:
    - genau ein operatives Mitglied zur E-Mail
    - genau ein `app_user` zur Parzelle/Mitgliedsverknüpfung
    - Konsistenz zwischen `mitglied.auth_user_id`, `app_user.user_id` und `auth.users`
  - löst danach serverseitig den Recovery-/OTP-Pfad aus
  - Antwort an den Client bleibt generisch; Diagnosecode wird separat mitgeliefert
- `supabase/config.toml` um die neue Function ergänzt:
  - `[functions.kgv-request-first-login-otp]`
  - `verify_jwt = false`
  - eigener `deno.json`-/`entrypoint`-Pfad
- `KGV.Infrastructure/Authentication/AuthService.cs` minimal umgestellt:
  - `RequestOtpAsync(...)` nutzt für `first-login` jetzt nicht mehr `EnsureOtpPreparationAsync(...)`
  - der First-Login-Pfad ruft stattdessen nur noch `kgv-request-first-login-otp` auf
  - die bestehende Diagnose-/Supportcode-Logik bleibt erhalten und wird aus der Function-Antwort gespeist
  - der normale Password-Recovery-/Verify-/SetPassword-Pfad blieb bewusst unverändert
- `KGV.Wpf/ViewModels/LoginViewModel.cs` zeigt bei OTP-Fehlschlag jetzt denselben Diagnosecode aus dem Shared-`AuthService`
- Validierung des Abschlussstands:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
    - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - zusätzlicher Codecheck bestätigt: `AuthService.RequestOtpAsync(...)` nutzt im `first-login`-Pfad jetzt nur noch `InvokeFirstLoginOtpFunctionAsync(...)`
  - lokaler `deno check` war in diesem Visual-Studio-/Terminal-Setup nicht verfügbar, daher keine Tool-basierte TS-Validierung

## 2026-03-30 – MAUI OTP Fehlercode für Support sichtbar gemacht

- Den realen OTP-Iststand zuerst auf dem aktuellen `main` geprüft:
  - `AuthService` loggt bereits die echten Stufen `OTP_REQUEST_BLOCK`, `OTP_RECOVERY_REQUEST_GOTRUE_FAIL`, `OTP_RECOVERY_REQUEST_FAIL` und `OTP_REQUEST_EXCEPTION`
  - ein letzter auslesbarer OTP-Fehlercontainer existierte davor noch nicht
  - `LoginPage` zeigte bei Fehlschlag weiterhin nur eine Sammelmeldung ohne direkt nutzbaren Support-Code
- Minimal umgesetzt:
  - neues Shared-Modell `KGV.Core/Models/OtpFailureDiagnosticInfo.cs` ergänzt
  - `KGV.Infrastructure/Authentication/AuthService.cs` hält jetzt die letzte OTP-Fehldiagnose mit:
    - `Code`
    - `UserMessage`
    - `Timestamp`
  - der Container wird direkt im realen Shared-OTP-Pfad gesetzt, also nicht separat in der UI geraten:
    - `OTP_REQUEST_BLOCK`
    - `OTP_REQUEST_RESULT`
    - `OTP_REQUEST_EXCEPTION`
    - `OTP_RECOVERY_REQUEST_GOTRUE_FAIL`
    - `OTP_RECOVERY_REQUEST_FAIL`
  - `KGV.Maui/Pages/LoginPage.xaml.cs` zeigt bei OTP-Fehlschlag jetzt zusätzlich einen kleinen Support-Hinweis mit Diagnosecode und bietet `Code kopieren` an
  - keine rohen Exception-Texte an Endnutzer ausgegeben
- Bewusst nicht gemacht:
  - keine Konfigurationsänderung
  - kein neuer OTP-Fachumbau
  - keine WPF-UI-Änderung
  - `IAuthService` bewusst nicht angefasst, weil die Datei im Arbeitsbaum bereits blockfremd verändert war; MAUI liest den letzten OTP-Code daher direkt aus dem realen `AuthService`
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 – MAUI OTP Diagnose und Konfigurationsabweichung geklärt

- Realen Repo-/Git-/OTP-Iststand zuerst gegen den aktuellen Workspace geprüft.
- Direkt verglichen wurden:
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Wpf/App.xaml.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Wpf/ViewModels/LoginViewModel.cs`
  - `appsettings.json`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
- Belastbarer Konfigurationsbefund:
  - WPF lädt effektiv aus `CurrentDirectory` und `AppContext.BaseDirectory`; optional wird zusätzlich per Reflection `AddUserSecrets<App>(...)` versucht
  - MAUI lädt effektiv nur das eingebettete Paket-Asset `appsettings.json`
  - im aktuellen Repo-/Buildstand ist aber **keine aktive Konfigurationsabweichung nachweisbar**:
    - Root-`appsettings.json`, WPF-Output-`appsettings.json` und MAUI-Paket-Asset `obj\...\assets\appsettings.json` hatten denselben SHA256-Hash
    - im Repo selbst gibt es keinen nachweisbaren `UserSecretsId`-Pfad in den aktiven Projektdateien
  - damit ist im aktuellen Stand nicht die Repo-Konfiguration der naheliegende Unterschied zwischen WPF und MAUI
- Minimal nachgezogen wurde deshalb der Diagnoseblock statt eines blinden Auth-Umbaus:
  - `KGV.Maui/MauiProgram.cs`
    - Appsettings-Hash des tatsächlich geladenen MAUI-Paket-Assets wird jetzt geloggt
    - effektiver Supabase-Fingerprint (`host`, `keyLength`, `keySuffix`, `keySha256`) wird beim MAUI-Startup geloggt
  - `KGV.Wpf/App.xaml.cs`
    - effektive WPF-Konfigurationsquellen und derselbe Supabase-Fingerprint werden jetzt in `Debug`/`Trace` ausgegeben
  - `KGV.Infrastructure/Authentication/AuthService.cs`
    - OTP-Anforderung jetzt mit klaren Diagnosemarkern für:
      - `OTP_REQUEST_START`
      - `OTP_REQUEST_BLOCK`
      - `OTP_RECOVERY_REQUEST_START`
      - `OTP_RECOVERY_REQUEST_OK`
      - `OTP_RECOVERY_REQUEST_GOTRUE_FAIL`
      - `OTP_RECOVERY_REQUEST_FAIL`
      - `OTP_REQUEST_RESULT`
    - dadurch ist künftig sichtbar, ob der Fehlschlag aus Vorbereitung, Recovery-Request oder Exception kommt
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
    - Endnutzer behält kurze verständliche Meldung
    - zusätzlich wird beim Fehlschlag explizit auf den Diagnose-Log verwiesen und ein eigener Logeintrag erzeugt
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Ergebnis dieses Blocks:
  - der aktuelle Repo-/Buildstand zeigt keine belastbare Konfigurationsabweichung zwischen WPF und MAUI
  - der Unterschied ist aktuell eher ein Laufzeit-/Umgebungs- oder OTP-Request-Thema
  - der MAUI-Pfad loggt den echten Fehlergrund jetzt deutlich präziser, ohne rohe Techniktexte direkt an Endnutzer auszugeben

## 2026-03-30 – Mitgliedskontext in MAUI und WPF auf Admin-Menü umgestellt

- Realen Git-Stand vor dem Block geprüft:
  - `git fetch origin`
  - `git status --short --branch` => `## main...origin/main`
  - `git log --oneline -n 1` => `fb15e69 Impressum fachlich auf Kontaktregeln angepasst`
- Gleichzeitig vorhandene blockfremde lokale Änderungen im Arbeitsbaum bewusst nicht in diesen Block gezogen.
- MAUI fachlich umgestellt:
  - direkter Eintrag `↳ Benutzerverwaltung` aus `KGV.Maui/AdminShell.cs` entfernt
  - stattdessen `↳ Admin-Menü` im ausgewählten Mitgliedskontext ergänzt
  - neue kleine `KGV.Maui/Pages/AdminMenuPage.cs` angelegt
  - die Seite zeigt den Bezug auf das aktuell ausgewählte Mitglied und verlinkt nur die bereits vorhandene produktive Funktion `Benutzerverwaltung`
  - der neue Pfad bleibt nur für `Admin` sichtbar
- WPF fachlich umgestellt:
  - direkten Eintrag `↳ Benutzerverwaltung` aus `MemberNavigationItems` in `KGV.Wpf/ViewModels/MainWindowViewModel.cs` entfernt
  - `↳ Admin-Menü` bleibt der einzige Mitgliedskontext-Einstieg für appuserbezogene Aktionen
  - `KGV.Wpf/ViewModels/AdminRoleViewModel.cs` und `KGV.Wpf/Views/AdminRoleView.xaml` minimal ergänzt, damit die bestehende gebundene Benutzerverwaltung von dort für das ausgewählte Mitglied geöffnet werden kann
  - der Einstieg zur Benutzerverwaltung bleibt nur für `Admin` sichtbar
- Keine neuen Backend-Endpunkte, keine neue Auth-Schattenlogik, keine blockfremden UI-Umbauten.
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 – Impressum-Kontaktregel-Block mit Buildrest sauber abgeschlossen

- Nur den echten Restfehler des begonnenen Impressum-Blocks in `KGV.Infrastructure/Services/SupabaseService.cs` minimal geschlossen.
- Ursache am Iststand geprüft: die kleine Hilfsmethode `NormalizeMeterEichjahr(...)` fehlte lokal, die beiden Aufrufe in `AddStromzaehlerAsync(...)` und `AddWasserzaehlerAsync(...)` waren ansonsten fachlich passend.
- Minimalfix umgesetzt:
  - `NormalizeMeterEichjahr(DateTime)` in `SupabaseService` wieder ergänzt
  - keine Refactorings, keine fachfremden Änderungen, keine neuen Features
- Validierung direkt danach:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Abschlussstand des begonnenen Impressum-Blocks:
  - Impressum-Kontaktregeln in Shared, MAUI und WPF bleiben auf dem begonnenen Fachstand
  - der frühere Buildrest `NormalizeMeterEichjahr` blockiert den Abschluss nicht mehr
  - verbleibend nur bekannte Warnungen außerhalb dieses Impressum-Abschlusses

## 2026-03-30 – Impressum-Kontaktregeln in WPF auf den begonnenen Stand fertiggezogen

- Auf dem lokal begonnenen Impressum-Block aufgesetzt; kein Neuaufbau, keine Nebenbaustellen.
- Direkt geschlossen wurde nur der echte offene Rest in `KGV.Wpf/Views/ImpressumView.xaml`.
- WPF-Renderlogik jetzt fachlich passend zum bereits angepassten Shared-/ViewModel-/MAUI-Stand:
  - Vorstandsvorsitzende: Funktion, Name, vollständige Adresse, Handynummer falls vorhanden
  - übrige Personen: nur Name und Handynummer
  - keine persönlichen E-Mail-Adressen mehr im WPF-Impressum
  - Vereinsmail ausschließlich `kgvoberrothenbach@gmx.de` im statischen Vereinsblock
  - keine leeren Kontaktzeilen für fehlende Adresse oder fehlende Handynummer
- Kurzer Abgleich `ImpressumViewModel` ↔ `ImpressumView.xaml`: verwendete Bindings passen zum begonnenen Kontaktmodell (`ClubEmail`, `IsVorstandsvorsitzende`, `ShowAdresse`, `HasHandy`, `Handy`, `Adresse`).
- Validierung ehrlich durchgeführt:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => fehlgeschlagen
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => fehlgeschlagen
  - echter vorhandener Restfehler außerhalb dieses Impressum-Abschlusses: `KGV.Infrastructure/Services/SupabaseService.cs` referenziert `NormalizeMeterEichjahr` an den Zeilen `610` und `630`, der Name ist im aktuellen Workspace nicht auflösbar
  - dieser Fehler wurde in diesem Block bewusst nicht angefasst, weil er nicht zur Impressum-Kontaktregel gehört

## 2026-03-30 â€“ MAUI Stammdaten Laufzeitpfad auf Git-Stand korrigiert

- Reale Ist-Analyse im aktuellen Repo durchgefÃ¼hrt:
  - `UserShell` Ã¶ffnet im Nutzerkontext fÃ¼r `â†³ Stammdaten` bereits `MeineDatenPage`
  - `MeineDatenPage` enthÃ¤lt im aktuellen Git-Stand den produktiven Anzeige-/Bearbeiten-Modus mit `Bearbeiten`, `Speichern`, `Abbrechen`, `DatePicker` plus `Leeren` und separatem WhatsApp-Switch
  - `MyProfilePage` existiert weiterhin als separater Self-Service-/Profilpfad, hÃ¤ngt aber nicht im produktiven Mitglieds-MenÃ¼pfad
  - im Admin-/Mitgliedskontext zeigte `AdminShell` fÃ¼r `â†³ Stammdaten` weiterhin noch auf `MemberDetailPage`
  - genau dort lag auch das beobachtete Alt-UI mit Datums-Schaltern vor den `DatePicker`-Feldern und ohne den neuen `Bearbeiten`-Button
- Echte Ursache des Laufzeitfehlers:
  - nicht `MeineDatenPage`, sondern der alte Admin-Mitgliedspfad `MemberDetailPage` wurde geÃ¶ffnet
  - dadurch war zur Laufzeit weiterhin das alte Stammdaten-UI sichtbar, obwohl `MeineDatenPage` im Repo schon auf dem neuen Stand lag
- Minimal korrigiert:
  - `KGV.Maui/AdminShell.cs`
    - MenÃ¼punkt `â†³ Stammdaten` im ausgewÃ¤hlten Mitgliedskontext auf `MeineDatenPage` umgestellt
  - `KGV.Maui/ShellRouteRegistrar.cs`
    - Legacy-Route `nameof(MemberDetailPage)` ebenfalls auf `MeineDatenPage` gelegt, damit auch verbliebene Alt-Navigationen im Mitgliedskontext auf das aktuelle Git-UI laufen
- Fachliches Ergebnis dieses Blocks:
  - produktiver MAUI-Mitgliedskontext nutzt jetzt konsistent `MeineDatenPage`
  - sichtbarer `Bearbeiten`-Button erscheint dort, wenn der Fachkontext Bearbeitung erlaubt
  - im Anzeige-Modus keine Datums-Schalter
  - im Bearbeiten-Modus Datumsbearbeitung nur Ã¼ber `DatePicker` plus `Leeren`
  - `MyProfilePage` bleibt als separater Self-Service-Pfad bestehen, mischt sich aber nicht mehr in den Mitgliedskontext
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 â€“ Impressum in MAUI und WPF produktiv abgeschlossen

- Git-Stand des begonnenen lokalen Impressum-Blocks erneut geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - `git log --oneline -n 1` => `300a835 WPF ZÃ¤hlerwechsel auf Korrekturflow Ã¼ber Parzelle umgestellt`
  - damit wurde der lokal begonnene Impressum-Block direkt auf dem aktuellen Stand nach `300a835` sauber weitergefÃ¼hrt und nicht neu aufgesetzt
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses Impressum-Blocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffene Impressum-Dateien geprÃ¼ft/abgeschlossen:
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Core/Models/ImpressumFunktionSlotRecord.cs`
  - `KGV.Core/Models/ImpressumInfo.cs`
  - `KGV.Core/Models/ImpressumKontaktItem.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/ImpressumPage.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/UserShell.cs`
  - `KGV.Wpf/ViewModels/ImpressumViewModel.cs`
  - `KGV.Wpf/Views/ImpressumView.xaml`
  - `KGV.Wpf/Views/ImpressumView.xaml.cs`
  - `KGV.Wpf/Infrastructure/Services/NavigationService.cs`
  - `KGV.Wpf/ViewModels/MainWindowViewModel.cs`
  - `KGV.Wpf/App.xaml`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Bestehenden lokalen Stand minimal zu Ende gefÃ¼hrt:
  - gemeinsamer Read-Pfad fÃ¼r `impressum_funktion_slot` bleibt als Shared-/Supabase-Lesepfad erhalten
  - statische Vereinsangaben fest eingebaut:
    - `Kleingartenverein Oberrothenbach e.V.`
    - `Amtsgericht Chemnitz VR 70502`
  - dynamische Bereiche `Vorstand` und `Bauausschuss` lesen die realen Slot-/Mitgliedsdaten Ã¼ber den bestehenden Supabase-Servicepfad
  - bei fehlender `mitglied_id` oder unvollstÃ¤ndigen Daten wird neutraler Fallback angezeigt statt einer Exception
  - MAUI-Impressum bleibt reine Leseseite und hÃ¤ngt als eigener MenÃ¼punkt in `AdminShell` und `UserShell`
  - WPF-Impressum bleibt reine Leseseite und hÃ¤ngt als normaler Navigationseintrag in `MainWindowViewModel`
- Echter offener Rest des begonnenen Blocks sauber geschlossen:
  - in `KGV.Maui/Pages/ImpressumPage.cs` den fehlenden Namespace `using Microsoft.Maui.Controls.Shapes;` ergÃ¤nzt
  - dadurch wurde `RoundRectangle` korrekt aufgelÃ¶st; keine weitere FachÃ¤nderung nÃ¶tig
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly -v:q` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly -v:q` => erfolgreich
  - damit ist der lokal begonnene Impressum-Block technisch sauber abgeschlossen

## 2026-03-30 â€“ WPF ZÃ¤hlerwechsel auf Korrekturflow Ã¼ber Parzelle umgestellt

- Git-Stand vor dem Block gegen `origin/main` geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - `git log --oneline -n 1` => `0e2908e MAUI ZÃ¤hlerwechsel auf 3-Fall-Flow umgestellt`
  - `git branch --contains 0e2908e` / `git branch -r --contains 0e2908e` => `main` und `origin/main`
  - damit lag der geforderte Ausgangsstand nach `0e2908e` real vor
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses WPF-ZÃ¤hlerwechsel-Blocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffene Dateien geprÃ¼ft/angepasst:
  - `KGV.Wpf/Views/AblesenOverviewView.xaml`
  - `KGV.Wpf/ViewModels/ZaehlerwechselScanViewModel.cs`
  - `KGV.Wpf/ViewModels/ZaehlerwechselEinbauViewModel.cs`
  - `KGV.Wpf/ViewModels/ZaehlerwechselAusbauViewModel.cs`
  - `KGV.Wpf/Views/ZaehlerwechselAusbauView.xaml`
  - `KGV.Wpf/Views/ZaehlerwechselEinbauView.xaml`
  - `KGV.Wpf/Views/ZaehlerwechselScanView.xaml`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Ehrlicher Istzustand vor dem Fix:
  - die WPF-`Ablesen`-Kachel `ZÃ¤hlerwechsel` sprach noch von `Tag scannen`
  - `ZaehlerwechselScanViewModel` arbeitete noch RFID-getrieben Ã¼ber `RfidScanContextViewModel`
  - `ZaehlerwechselAusbauViewModel` und `ZaehlerwechselEinbauViewModel` waren noch reine Placeholder
  - WPF war damit fÃ¼r den ZÃ¤hlerwechsel noch zu stark als Vor-Ort-/RFID-Flow angelegt statt als Korrektur-/Verwaltungsweg Ã¼ber Parzelle
- Minimal umgesetzt:
  - `AblesenOverviewView.xaml` beschreibt `ZÃ¤hlerwechsel` jetzt fachlich als Korrektur-/Verwaltungsweg Ã¼ber Gartennummer oder Parzelle
  - `ZaehlerwechselScanViewModel` von RFID-Einstieg auf ruhige Such-/Auswahllogik umgebaut:
    - Suche nach Gartennummer oder Parzelle
    - Auswahl einer Parzelle
    - Laden des aktuellen Strom-/Wasserzustands Ã¼ber den bestehenden Servicepfad `GetParzelleDetailAsync(...)`
    - pro Medium klare Ableitung:
      - aktiver ZÃ¤hler vorhanden => Ausbau-/Korrekturpfad
      - kein aktiver ZÃ¤hler => Einbaupfad
  - `ZaehlerwechselScanView.xaml` zeigt jetzt die Suchmaske und die pro Medium geladenen ZustÃ¤nde statt RFID-Scan-Kontext
  - `ZaehlerwechselAusbauViewModel` und `ZaehlerwechselAusbauView.xaml` produktiv nachgezogen:
    - Schlussstand
    - Ausbau-Datum
    - Speicherung der Schlussablesung
    - Beenden des aktiven ZÃ¤hlers Ã¼ber die bestehenden Servicepfade
  - `ZaehlerwechselEinbauViewModel` und `ZaehlerwechselEinbauView.xaml` produktiv nachgezogen:
    - ZÃ¤hlernummer
    - Eichdatum
    - Einbau-Datum
    - Anfangsstand
    - Anlage des neuen ZÃ¤hlers Ã¼ber die bestehenden Servicepfade
    - Speicherung der Anfangsablesung
  - kein neuer Backend-Endpunkt, kein neuer RFID-WPF-Flow, keine MAUI-Datei geÃ¤ndert
- Fachliches Ergebnis dieses Blocks:
  - WPF-ZÃ¤hlerwechsel startet jetzt als Korrektur-/Verwaltungsweg Ã¼ber Parzelle statt Ã¼ber RFID-Scan
  - nach Auswahl der Parzelle werden Strom und Wasser fachlich getrennt geladen
  - daraus Ã¶ffnet WPF den passenden Einbau- oder Ausbaupfad je Medium
  - MAUI bleibt damit der primÃ¤re Vor-Ort-/RFID-Flow, WPF der Korrekturblock
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly -v:q` => erfolgreich
  - nur bestehende Warnungen auÃŸerhalb dieses Blocks blieben erhalten, vor allem in `KGV.Infrastructure/Services/SupabaseService.cs`
- Git-Abschluss dieses Blocks bewusst eng:
  - nur die direkt betroffenen WPF-ZÃ¤hlerwechsel-Dateien plus Logdateien gehÃ¶ren in diesen Commit
  - blockfremde lokale Ã„nderungen bleiben auÃŸerhalb dieses Commits

## 2026-03-30 â€“ MAUI ZÃ¤hlerwechsel auf 3-Fall-Flow umgestellt

- Vor dem Block Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - `git log --oneline -n 1` => `892b997 Parzellendetails in MAUI und WPF fachlich geschlossen`
  - `git branch --contains 892b997` / `git branch -r --contains 892b997` => `main` und `origin/main`
  - damit lag der geforderte Ausgangsstand nach `892b997` real vor
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses MAUI-ZÃ¤hlerwechsel-Blocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffene Dateien geprÃ¼ft/angepasst:
  - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Ehrlicher Istzustand vor dem Fix:
  - `ZaehlerwechselPage` nutzte noch den generischen RFID-Kontext-Workflow mit den Buttons `Weiter zum Ausbau` / `Weiter zum Einbau`
  - unbekannte Tags wurden dort nur mit Hinweistext stehen gelassen statt direkt in den bestehenden RFID-HinzufÃ¼gen-Flow zu gehen
  - `ZaehlerwechselAusbauPage` und `ZaehlerwechselEinbauPage` waren fachlich bereits produktiv nah dran und kehrten nach erfolgreichem Speichern schon wieder in einen frischen Scanpfad zurÃ¼ck
- Minimal umgesetzt:
  - generische Hauptlogik in `ZaehlerwechselPage` entfernt; die Seite reagiert jetzt direkt auf den aufgelÃ¶sten RFID-Kontext
  - 3-Fall-Flow jetzt produktiv:
    - RFID unbekannt => bestehender Flow `RFID hinzufÃ¼gen` Ã¼ber `RfidEinrichtenPage`
    - RFID bekannt ohne aktiven ZÃ¤hler => direkter Wechsel in `ZaehlerwechselEinbauPage`
    - RFID bekannt mit aktivem ZÃ¤hler => zuerst BestÃ¤tigung `Wollen Sie den ZÃ¤hler ausbauen?`, nur bei `Ja` Wechsel in `ZaehlerwechselAusbauPage`
  - dafÃ¼r keinen neuen Service und keinen neuen Backendpfad gebaut; `ResolveRfidScanContextAsync(...)` bleibt der bestehende AuflÃ¶sungspfad
  - `RfidEinrichtenPage` kann jetzt optional eine bereits gescannte UID per Query Ã¼bernehmen und direkt in den bestehenden RFID-HinzufÃ¼gen-Flow einsteigen, statt einen erneuten Scan zu erzwingen
  - bestehende Ausbau-/Einbau-Seiten blieben unangetastet, weil sie den erfolgreichen RÃ¼ckweg in einen frischen Scan-Zustand bereits korrekt abdecken
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs` blieben unverÃ¤ndert bestehen
- Git-Abschluss dieses Blocks bewusst eng:
  - nur `KGV.Maui/Pages/ZaehlerwechselPage.cs`, `KGV.Maui/Pages/RfidEinrichtenPage.cs`, `DEV_LOG.md` und `KGV_Fortschrittslog_ausfuehrlich.md`
  - blockfremde lokale Ã„nderungen bleiben auÃŸerhalb dieses Commits

## 2026-03-30 â€“ Parzellendetails in MAUI und WPF fachlich geschlossen

- Vor dem Block Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - `git log --oneline -n 1` => `ef02846 MAUI Stammdaten Mail-Regel im Editmodus`
  - `git branch --contains ef02846` / `git branch -r --contains ef02846` => `main` und `origin/main`
  - damit lag der geforderte Ausgangsstand nach `ef02846` real vor
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses Parzellen-Blocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffene Dateien geprÃ¼ft/angepasst:
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
  - `KGV.Wpf/Views/ParzellenVerwaltungView.xaml`
  - `KGV.Wpf/ViewModels/ParzellenVerwaltungViewModel.cs`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Fachlicher Repo-Istzustand vor dem Fix:
  - MAUI hatte FlÃ¤che / `hat_wasser` / `hat_strom` bereits im Parzellen-Stammdatenpfad, zeigte aber zusÃ¤tzlich noch komplette Belegungs-/Zuweisungs-UI direkt auf der Parzellenseite
  - die MAUI-FlÃ¤chenbestÃ¤tigung verwendete noch nicht den jetzt geforderten exakten Wortlaut
  - WPF zeigte FlÃ¤che / `hat_wasser` / `hat_strom` in der Parzellen-Detailansicht noch nicht
  - WPF enthielt zusÃ¤tzlich weiterhin die Gruppe `Belegung / Zuordnung` samt `Zuordnen` / `Aktive Belegung beenden`
- Minimal umgesetzt:
  - **MAUI**
    - komplette Belegungs-/Zuweisungssektion aus `ParzellenPage` entfernt
    - Editbereich auf genau diese Felder reduziert:
      - `EditFlaeche`
      - `EditHatWasser`
      - `EditHatStrom`
    - Save-Pfad beibehalten, aber nicht editierbare Stammdaten beim Speichern aus `SelectedDetail` konserviert
    - FlÃ¤chenbestÃ¤tigung auf exakt den geforderten Text umgestellt:
      - `Bist du dir sicher, dass du die FlÃ¤che der Parzelle Ã¤ndern mÃ¶chtest?`
  - **WPF**
    - Stammdatenanzeige um FlÃ¤che / `hat Wasser` / `hat Strom` ergÃ¤nzt
    - echten Anzeige-/Bearbeiten-Modus ergÃ¤nzt
    - im Bearbeiten-Modus nur FlÃ¤che / `hat Wasser` / `hat Strom` editierbar gemacht
    - Speicherung Ã¼ber den bestehenden Pfad `UpdateParzelleStammdatenAsync(ParzelleRecord)` umgesetzt
    - FlÃ¤chenbestÃ¤tigung mit exakt demselben Wortlaut ergÃ¤nzt
    - komplette Gruppe `Belegung / Zuordnung` aus der Parzellenansicht entfernt
    - tote Zuweisungs-/Beendigungslogik im WPF-ViewModel mit abgebaut
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - in MAUI nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
- Git-Abschluss dieses Blocks bewusst eng:
  - nur die direkt betroffenen Parzellen-Dateien plus Logdateien gehÃ¶ren in diesen Commit
  - blockfremde lokale Ã„nderungen bleiben auÃŸerhalb dieses Commits

## 2026-03-30 â€“ MAUI-Stammdaten: Mail-Regel im Editmodus fachlich geschlossen

- Vor dem Block Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main 120bd93 [origin/main] MAUI Stammdaten Editmodus im Mitgliedskontext`
  - `git log --oneline -n 1` => `120bd93 MAUI Stammdaten Editmodus im Mitgliedskontext`
  - damit war der geforderte Ausgangsstand nach `120bd93` real vorhanden und bereits auf `origin/main`
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses engen MAUI-Abschlussblocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffen nur gelesen/angepasst:
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Reale RestlÃ¼cke des Standes nach `120bd93`:
  - der neue MAUI-Editmodus war vorhanden
  - die Mail-Regel war im UI aber noch nicht fachlich geschlossen
  - `UpdateMitgliedAsync(...)` schÃ¼tzt die Mail serverseitig bereits korrekt, aber `MeineDatenPage` nutzte diese Regel im mobilen Editmodus noch nicht sichtbar/genau genug
- Minimal umgesetzt, nur im vorhandenen MAUI-Seitenpfad:
  - `MeineDatenPage` merkt sich jetzt aus dem bereits geladenen `MitgliedRecord`, ob `AuthUserId` vorhanden ist
  - E-Mail wird im Anzeige-Modus weiter normal angezeigt
  - im Bearbeiten-Modus gilt jetzt exakt:
    - Admin/Vorstand + **kein** `AuthUserId` => E-Mail editierbar
    - sobald `AuthUserId` vorhanden => E-Mail read-only
    - bei vorhandenem `AuthUserId` erscheint klarer Hinweistext, dass die Mailadresse vom Nutzer selbst Ã¼ber den bestehenden Supabase-/OTP-MailÃ¤nderungsweg geÃ¤ndert werden muss
    - normale Nutzer bekommen in `MeineDatenPage` keine editierbare Mail freigeschaltet
  - beim Speichern wird `dto.Email` nur dann aus dem Editor Ã¼bernommen, wenn die Fachregel das erlaubt
  - ansonsten bleibt die bestehende Mail unverÃ¤ndert
  - zusÃ¤tzliche Validierung nur fÃ¼r den erlaubten Editfall:
    - trimmen
    - leere Mail blockieren
    - einfache PlausibilitÃ¤tsprÃ¼fung des Mailformats
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs` blieben unverÃ¤ndert
- Git-Abschluss dieses Blocks bewusst eng:
  - nur `KGV.Maui/Pages/MeineDatenPage.xaml.cs`, `DEV_LOG.md` und `KGV_Fortschrittslog_ausfuehrlich.md`
  - blockfremde lokale Ã„nderungen bleiben auÃŸerhalb dieses Commits

## 2026-03-30 â€“ MAUI-Stammdaten: echter Edit-Modus im Mitgliedskontext in `MeineDatenPage`

- Vor dem Block Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main 055ab73 [origin/main] Fix WPF invite button visibility refresh`
  - keine Divergenz zu `origin/main`
- Gleichzeitig real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses MAUI-Blocks:
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Core/Models/MemberUserLinkStatus.cs`
  - `KGV.Core/Models/MemberUserLinkStatusDto.cs`
  - `AWR.bat`
  - `Android_Wpf_release_batch_v4.bat`
- Direkt betroffenen MAUI-Pfad geprÃ¼ft:
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - ergÃ¤nzend nur lesend als Referenz: `KGV.Maui/Pages/MyProfilePage.cs`
- Reale RestlÃ¼cke vor dem Fix:
  - `MeineDatenPage` war fÃ¼r fremde Mitglieder praktisch read-only
  - `Bearbeiten` leitete bei Eigenfall nur nach `MyProfilePage` weiter
  - fÃ¼r fremde Mitglieder kam stattdessen nur der Dialog, dass ein mobiler Volleditor noch nicht vorhanden sei
  - Datumseingaben im gewÃ¼nschten mobilen Editmodus existierten dort real noch gar nicht
- Minimal umgesetzt, primÃ¤r nur in `KGV.Maui/Pages/MeineDatenPage.xaml.cs`:
  - echter Seitenzustand `Anzeige-Modus` / `Bearbeiten-Modus` ergÃ¤nzt
  - Anzeige-Modus zeigt weiterhin reine Wertdarstellung
  - Bearbeiten-Modus zeigt echte mobile Eingabefelder fÃ¼r:
    - `Vorname`
    - `Nachname`
    - `Telefon`
    - `Mobilnummer`
    - `StraÃŸe`
    - `PLZ`
    - `Ort`
    - `Bemerkung`
  - `Geburtsdatum`, `Mitglied seit`, `Mitglied Ende` jetzt als DatePicker + `Leeren`-Button umgesetzt, ausdrÃ¼cklich **ohne** Vorschalt-Schalter
  - `WhatsApp` bleibt als Switch nur im Bearbeiten-Modus sichtbar/bearbeitbar
  - `Speichern` und `Abbrechen` stehen jetzt am Ende des Eingabeformulars
  - der frÃ¼here Dialog "mobiler Volleditor ... noch nicht vorhanden" entfÃ¤llt
  - Admin/Vorstand kÃ¶nnen fremde Mitglieder jetzt direkt in `MeineDatenPage` bearbeiten und Ã¼ber den vorhandenen Lock-/`UpdateMitgliedAsync(...)`-Pfad speichern
  - `MyProfilePage` blieb bewusst unverÃ¤ndert als Self-Service-/OTP-Kontextseite
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs` verblieben
- Git-Abschluss dieses Blocks bewusst eng:
  - nur `KGV.Maui/Pages/MeineDatenPage.xaml.cs`, `DEV_LOG.md` und `KGV_Fortschrittslog_ausfuehrlich.md`
  - blockfremde lokale Ã„nderungen bleiben auÃŸerhalb dieses MAUI-Commits

## 2026-03-30 â€“ WPF-Bugfix: `Nutzer hinzufÃ¼gen` in der gebundenen Benutzerverwaltung wieder sichtbar

- Vor dem Block Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main 43f5513 [origin/main] Document invite fix repo verification`
  - keine Divergenz zu `origin/main`
- ZusÃ¤tzlich real im Arbeitsbaum sichtbar, aber bewusst **nicht** Teil dieses kleinen WPF-Blocks:
  - laufende lokale Ã„nderungen in `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Core/Models/InviteUserAccountResult.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - ungetrackte Dateien `KGV.Core/Models/MemberUserLinkStatus.cs`, `KGV.Core/Models/MemberUserLinkStatusDto.cs`, `Android_Wpf_release_batch_v4.bat`, `Android_Wpf_release_batch_v5.bat`
- Direkt betroffenen WPF-Pfad geprÃ¼ft:
  - `KGV.Wpf/ViewModels/UserManagementViewModel.cs`
  - `KGV.Wpf/Views/UserManagementView.xaml`
  - MAUI nur lesend gegengeprÃ¼ft: `KGV.Maui/ViewModels/UserManagementViewModel.cs`
- Reale Ursache des Restfehlers:
  - `ShowInviteAction` ist in WPF eine berechnete Property
  - der Button in `UserManagementView.xaml` war bereits korrekt an `ShowInviteAction` gebunden
  - im WPF-ViewModel fehlten aber die notwendigen `PropertyChanged`-Benachrichtigungen fÃ¼r `ShowInviteAction`
  - dadurch blieb die Visibility im gebundenen Mitgliedskontext nach `SelectedUser`-/`IsBusy`-/`LoadAsync()`-Wechseln hÃ¤ngen, obwohl der Invite-Fall fachlich vorlag
- Minimal umgesetzt:
  - in `KGV.Wpf/ViewModels/UserManagementViewModel.cs` `OnPropertyChanged(nameof(ShowInviteAction))` ergÃ¤nzt:
    - im Setter von `SelectedUser`
    - im Setter von `IsBusy`
    - nach dem relevanten Statuswechsel in `LoadAsync()`
  - passend dazu in `LoadAsync()` und bei Busy-Wechseln die Command-Requery fÃ¼r Invite/Remove sauber nachgezogen
  - `UserManagementView.xaml` blieb fachlich unverÃ¤ndert, weil das Visibility-Binding bereits korrekt auf `ShowInviteAction` zeigt
- Fachliches Ergebnis:
  - gebundenes Mitglied mit E-Mail und ohne verknÃ¼pften App-User/Auth-User zeigt `Nutzer hinzufÃ¼gen` in WPF wieder sichtbar an
  - konsistent verknÃ¼pfte FÃ¤lle blenden den Button weiter korrekt aus
  - `Nutzer entfernen` bleibt auf dem vorhandenen WPF-Pfad unverÃ¤ndert fachlich gekoppelt
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly -v:q` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich, nur bekannte Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - dateibezogene FehlerprÃ¼fung auf `KGV.Wpf/ViewModels/UserManagementViewModel.cs` und `KGV.Wpf/Views/UserManagementView.xaml` => unauffÃ¤llig
- Git-Abschluss dieses kleinen Blocks:
  - nur `KGV.Wpf/ViewModels/UserManagementViewModel.cs`, `DEV_LOG.md` und `KGV_Fortschrittslog_ausfuehrlich.md` gehÃ¶ren zu diesem Commit
  - blockfremde lokale Ã„nderungen bleiben ausdrÃ¼cklich auÃŸerhalb dieses WPF-Bugfix-Commits

## 2026-03-30 â€“ Repo-Abgleich: Invite-/OTP-Fix ist real auf `origin/main` vorhanden

- Vor dem Block Git-RealitÃ¤t gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch --contains 7047fdd` => `main`
  - `git branch -r --contains 7047fdd` => `origin/main`
  - `git branch --contains 0d9d025` => `main`
  - `git branch -r --contains 0d9d025` => `origin/main`
  - `git rev-parse HEAD` == `git rev-parse origin/main` => beide auf `0d9d025...`
- Ergebnis der Git-PrÃ¼fung:
  - `7047fdd` war real auf dem aktuellen Arbeitsbranch enthalten
  - `7047fdd` war real auf `origin/main` enthalten
  - `0d9d025` war real auf dem aktuellen Arbeitsbranch enthalten
  - `0d9d025` war real auf `origin/main` enthalten
  - es lag also **kein Branch-/Push-/Cherry-Pick-Mismatch** vor
- Repo-Code gegen Behauptung abgeglichen:
  - `AuthService` enthÃ¤lt real die Referenz auf `find_auth_user_id_by_email`
  - Diagnose-/Persistenz-Logs (`AUTH_DIAG`) sind real im Code vorhanden
  - Readback-Verifikation fÃ¼r `mitglied.auth_user_id` ist real vorhanden
  - Readback-Verifikation fÃ¼r `app_user` ist real vorhanden
  - reparierender Pfad nach `VerifyOtpAsync(...)` ist real vorhanden
- WPF/MAUI-GegenprÃ¼fung:
  - WPF ruft weiter `InviteUserAsync(...)` aus `KGV.Wpf/ViewModels/UserManagementViewModel.cs` auf
  - MAUI ruft denselben Pfad aus `KGV.Maui/Pages/MemberDetailPage.cs` auf
  - kein zusÃ¤tzlicher UI-Mismatch als Ursache erkennbar
- Config-Abgleich:
  - WPF und MAUI lesen im Repo dieselbe Root-`appsettings.json`
  - beide zeigen dort auf dieselbe Supabase-URL `https://itjcabiibuodkxayhvjq.supabase.co`
- Bewertung dieses Abgleichblocks:
  - der echte Invite-/OTP-Fix ist bereits auf `origin/main` angekommen
  - wenn der produktive Test weiterhin scheitert, liegt die nÃ¤chste reale Verdachtsrichtung **nicht** in fehlenden Git-Commits auf `main`, sondern in Runtime-/DB-/Supabase-Produktivbedingungen auÃŸerhalb des reinen Repo-Stands
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- Bewusst nicht Teil dieses Abgleichblocks geblieben:
  - lokale Ã„nderung in `KGV.Maui/KGV.Maui.csproj`
  - ungetrackte Batch-Dateien `Android_Wpf_release_batch_v4.bat` und `Android_Wpf_release_batch_v5.bat`

## 2026-03-30 â€“ Diagnoseblock: `Nutzer hinzufÃ¼gen` schreibt produktiv weiter keine `auth_user_id`

- Vor dem Block den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main 7047fdd [origin/main] Fix auth user resolution for invites`
  - keine Divergenz zu `origin/main`
- Den aktuellen Produktivpfad erneut gegen den Code geprÃ¼ft:
  - WPF ruft `InviteUserAsync(...)` aus `KGV.Wpf/ViewModels/UserManagementViewModel.cs` auf
  - MAUI ruft denselben Servicepfad im Mitgliedskontext aus `KGV.Maui/Pages/MemberDetailPage.cs` bzw. `KGV.Maui/ViewModels/UserManagementViewModel.cs` auf
  - damit liegt der Hauptverdacht weiterhin zentral im `AuthService`, nicht in einer der UIs
- Config-/ProjektprÃ¼fung:
  - WPF lÃ¤dt `appsettings.json` aus dem Workspace-/Output-Pfad in `KGV.Wpf/App.xaml.cs`
  - MAUI paketiert und lÃ¤dt dieselbe `appsettings.json` in `KGV.Maui/MauiProgram.cs`
  - beide zeigen im Repo auf dieselbe Supabase-URL `https://itjcabiibuodkxayhvjq.supabase.co`
  - realer Repo-Befund: im Repo selbst kein `UserSecretsId`, also kein nachweisbarer separater Secrets-Pfad im Projektstand
- SQL-/RPC-PrÃ¼fung:
  - Migration `supabase/migrations/20260330104500_find_auth_user_id_by_email.sql` geprÃ¼ft
  - RPC-Name passt exakt zum C#-Aufruf `find_auth_user_id_by_email`
  - RechteprÃ¼fung in SQL ist vorhanden (`service_role` oder `is_admin_or_vorstand()`)
  - aber: ob die Migration bereits wirklich im produktiven Supabase-Projekt eingespielt ist, lÃ¤sst sich rein aus dem Repo nicht beweisen
- Exakter technischer Blindspot des bisherigen Stands identifiziert:
  - `EnsureMemberInviteMappingAsync(...)` behandelte ein `Update()` bislang ohne Readback-Verifikation als Erfolg
  - wenn das DB-Update auf `mitglied.auth_user_id` produktiv wegen RLS, 0 geÃ¤nderten Zeilen oder abweichendem Runtime-Kontext nicht wirklich persistiert wurde, lief der Flow bisher trotzdem weiter
  - gleiches Problem bei `EnsureAppUserRecordAsync(...)`: Anlage/Aktualisierung wurde nicht belastbar nachgelesen
  - dadurch konnte OTP ausgelÃ¶st werden, obwohl `mitglied.auth_user_id` oder `app_user` tatsÃ¤chlich nicht sauber geschrieben waren
- FÃ¼r den Beweisblock umgesetzt:
  - gezielte `AUTH_DIAG`-Logs in `AuthService` ergÃ¤nzt
  - Diagnosepunkte loggen jetzt u.a.:
    - Start des Invite-Laufs inkl. Mitglied, E-Mail-Maske, Endpoint-Kontext
    - MitgliedsauflÃ¶sung
    - RPC-Lookup-Start / Erfolg / leeres Ergebnis / Fehlerstatus / Exception
    - Wiederverwendeter oder neu aufgelÃ¶ster Auth-User
    - Erfolg / Retry / Verifikationsfehler beim Schreiben von `mitglied.auth_user_id`
    - Erfolg / Verifikationsfehler bei `app_user`
    - tatsÃ¤chliches AuslÃ¶sen des OTP-Versands
  - Logs gehen nicht nur Ã¼ber `ILogger`, sondern zusÃ¤tzlich Ã¼ber `System.Diagnostics.Trace`, damit der Beweisblock auch im WPF-Pfad ohne injizierten Logger sichtbar bleibt
- Minimal gehÃ¤rtet:
  - `EnsureMemberInviteMappingAsync(...)` liest nach jedem Update das Mitglied zurÃ¼ck und betrachtet den Schritt erst dann als erfolgreich, wenn `AuthUserId` wirklich persistiert ist
  - `EnsureAppUserRecordAsync(...)` verifiziert `app_user` nach Insert/Update per Readback
  - `EnsureInvitePreparationAsync(...)` und `EnsureOtpPreparationAsync(...)` brechen jetzt sauber ab, wenn die Mitglied-Zuordnung zwar versucht, aber nicht wirklich persistiert wurde
- Ergebnis dieses Diagnoseblocks:
  - der bisher eindeutig nachweisbare technische Fehlerpunkt lag beim Schritt **DB-Update / Persistenznachweis von `mitglied.auth_user_id`**
  - der bisherige Code konnte diesen Schritt fÃ¤lschlich als erfolgreich behandeln, obwohl keine belastbare Persistenz nachgewiesen war
  - mit dem neuen Diagnoseblock lÃ¤sst sich der nÃ¤chste produktive Test jetzt exakt auf einen der Schritte `RPC`, `mitglied.auth_user_id`, `app_user` oder `OTP` eingrenzen
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- Bewusst nicht Teil dieses Diagnoseblocks geblieben:
  - lokale Ã„nderung in `KGV.Maui/KGV.Maui.csproj`
  - ungetrackte Batch-Dateien `Android_Wpf_release_batch_v4.bat` und `Android_Wpf_release_batch_v5.bat`

## 2026-03-30 â€“ Prompt 1/1: Harter Fix fÃ¼r Invite-Flow ohne geschriebenes `auth_user_id`

- Vor dem Block den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main b76f918 [origin/main] Fix invite and OTP member mapping flow`
  - keine Divergenz zu `origin/main`
- Aktuellen Produktivpfad bestÃ¤tigt:
  - WPF und MAUI rufen fÃ¼r `Nutzer hinzufÃ¼gen` bereits `_authService.InviteUserAsync(...)` auf
  - der Restfehler lag weiterhin zentral in `KGV.Infrastructure/Authentication/AuthService.cs`
- Reale RestlÃ¼cken vor Umsetzung:
  - `InviteUserAsync(...)` erlaubte noch einen Deferred-Repair-Pfad und garantierte damit nicht in jedem Fall `mitglied.auth_user_id` und `app_user` vor dem OTP-Versand
  - `EnsureAuthUserForInviteAsync(...)` konnte vorhandene Auth-User noch nicht belastbar Ã¼ber einen echten Repo-/DB-Pfad per E-Mail auflÃ¶sen
  - `RequestOtpAsync(...)` und `SendPasswordResetEmailAsync(...)` verwendeten denselben Vorbereitungsweg wie Invite und konnten dadurch fachlich zu viel automatisch vorbereiten
  - `KGV.Maui/ViewModels/UserManagementViewModel.cs` blendete `Nutzer hinzufÃ¼gen` im gebundenen Mitgliedskontext weiterhin aus
  - `KGV.Maui/Pages/MemberDetailPage.cs` lieÃŸ den Pfad mobil weiterhin nur fÃ¼r Admin zu, nicht fÃ¼r Vorstand
- Minimal-invasiv umgesetzt:
  - neue echte DB-/RPC-Grundlage ergÃ¤nzt: `supabase/migrations/20260330104500_find_auth_user_id_by_email.sql`
    - Security-Definer-Funktion `public.find_auth_user_id_by_email(text)` liest fÃ¼r `admin`/`vorstand` bzw. `service_role` belastbar die vorhandene `auth.users.id` per E-Mail
  - `AuthService` nutzt diese RPC-AuflÃ¶sung jetzt vor bzw. nach `SignUp`, um bestehende Auth-User sauber weiterzuverarbeiten statt nur auf Deferred-Repair zu hoffen
  - `InviteUserAsync(...)` verlangt jetzt vor OTP-Versand einen wirklich vorbereiteten Zustand:
    - Auth-User vorhanden/aufgelÃ¶st
    - `mitglied.auth_user_id` gesetzt
    - `app_user` angelegt/aktualisiert
  - `RequestOtpAsync(...)` und `SendPasswordResetEmailAsync(...)` erzeugen keine Auth-User mehr blind im freien Login-Kontext
    - sie lassen nur noch bereits fachlich vorbereitete FÃ¤lle durch
    - fehlende Zuordnungen werden nur aus bereits vorhandenen produktiven Mappings repariert
  - `VerifyOtpAsync(...)` bleibt als zusÃ¤tzlicher Sicherheits-/Reparaturpfad erhalten und vervollstÃ¤ndigt nach erfolgreicher OTP-BestÃ¤tigung weiterhin `mitglied.auth_user_id` und `app_user`
  - WPF- und MAUI-Benutzerverwaltung wurden in der Sichtbarkeit konsistent gezogen:
    - WPF zeigt `Nutzer hinzufÃ¼gen` nur noch bei ungebundenem App-User-Kontext fÃ¼r das gebundene Mitglied an
    - MAUI zeigt `Nutzer hinzufÃ¼gen` jetzt auch im gebundenen Mitgliedskontext an
    - MAUI-Mitgliedsdetail erlaubt `Nutzer hinzufÃ¼gen` und Benutzerverwaltung jetzt fÃ¼r `Admin` und `Vorstand`
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- Bewusst nicht Teil dieses Blocks geblieben:
  - lokale Ã„nderung in `KGV.Maui/KGV.Maui.csproj`
  - ungetrackte Batch-Dateien `Android_Wpf_release_batch_v4.bat` und `Android_Wpf_release_batch_v5.bat`

## 2026-03-30 â€“ Prompt 1/1: Nutzer hinzufÃ¼gen in WPF und MAUI gegen fehlende Auth-Zuordnung und freien OTP-Request gehÃ¤rtet

- Vor dem Block den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main 2a514c3 [origin/main] Clarify rolled back release steps`
  - keine Divergenz zu `origin/main`
- ZusÃ¤tzlich sichtbare blockfremde Arbeitsbaum-EintrÃ¤ge vor Start dokumentiert:
  - `KGV.Maui/KGV.Maui.csproj` mit lokaler `ApplicationVersion`-ErhÃ¶hung
  - `Android_Wpf_release_batch_v4.bat`
  - `Android_Wpf_release_batch_v5.bat`
- Den echten Istzustand fÃ¼r `Nutzer hinzufÃ¼gen` entlang des Produktivpfads geprÃ¼ft:
  - WPF: `KGV.Wpf/ViewModels/UserManagementViewModel.cs`
  - MAUI: `KGV.Maui/ViewModels/UserManagementViewModel.cs`, `KGV.Maui/Pages/MemberDetailPage.cs`, `KGV.Maui/Pages/UserManagementPage.cs`, `KGV.Maui/UserShell.cs`
  - Shared/Auth: `KGV.Core/Interfaces/IAuthService.cs`, `KGV.Infrastructure/Authentication/AuthService.cs`, `KGV.Infrastructure/Models/AppUserRecord.cs`, `KGV.Infrastructure/Models/AppUserInsertRecord.cs`, `KGV.Core/Models/MitgliedRecord.cs`, `KGV.Infrastructure/Services/SupabaseService.cs`
  - Login-/OTP-Pfade: `KGV.Wpf/ViewModels/LoginViewModel.cs`, `KGV.Wpf/ViewModels/ResetPasswordViewModel.cs`, `KGV.Maui/Pages/LoginPage.xaml.cs`
- Reale Befunde vor der Korrektur:
  - WPF und MAUI waren UI-seitig bereits an `InviteUserAsync(...)` angebunden
  - MAUI-Pfad war vorhanden und im Mitgliedsdetail fÃ¼r Admin sichtbar, also kein reiner Navigationsfehler
  - der Hauptfehler lag im Shared/Auth-Pfad:
    - `InviteUserAsync(...)` versuchte zwar Auth-User + Mitglied + `app_user` vorzubereiten
    - bei bereits existierendem Auth-User ohne sauber gesetztes `mitglied.auth_user_id` konnte `EnsureAuthUserForInviteAsync(...)` aber keine belastbare User-ID liefern
    - der freie `RequestOtpAsync(...)` versendete OTP bisher ohne vorherige Mitglieds-/ZuordnungsprÃ¼fung direkt Ã¼ber `ResetPasswordForEmail(...)`
    - `VerifyOtpAsync(...)` reparierte die Mitglied-/`app_user`-Zuordnung bisher nicht nach erfolgreicher OTP-BestÃ¤tigung
- Minimal-invasiv umgesetzt:
  - `AuthService` um einen robusteren Vorbereitungs-/Reparaturpfad erweitert:
    - gÃ¼ltiges Mitglied wird vor Invite/OTP jetzt belastbar per Mail bzw. Mitglied-ID aufgelÃ¶st
    - vorhandene Zuordnung wird zuerst Ã¼ber `mitglied.auth_user_id` und danach Ã¼ber `app_user.mitglied_id` wiederverwendet
    - nur wenn keine bestehende Zuordnung auffindbar ist, wird ein neues Auth-Konto erzeugt
    - bei bereits vorhandenem Auth-Konto ohne direkt lesbare ID wird ein kontrollierter Deferred-Repair-Pfad verwendet, der die Zuordnung unmittelbar nach erfolgreicher OTP-BestÃ¤tigung repariert
  - `RequestOtpAsync(...)` und `SendPasswordResetEmailAsync(...)` versenden jetzt keinen OTP mehr fÃ¼r Mailadressen ohne eindeutig vorbereitbares operatives Mitglied
  - `VerifyOtpAsync(...)` repariert nach erfolgreicher OTP-PrÃ¼fung jetzt sofort `mitglied.auth_user_id` und `app_user`, bevor der Passwort-Setzen-/Login-Fortgang weitergeht
  - `InviteUserAsync(...)` nutzt denselben idempotenten Vorbereitungsfluss wie OTP/Recovery
  - WPF- und MAUI-Fehlermeldungen fÃ¼r Invite/OTP/Reset auf fachlich klare, aber nicht technisch leaky Texte geschÃ¤rft
  - vorhandene MAUI-Mitgliedsdetail-/Benutzerverwaltungs-Pfade bewusst weiterverwendet; kein unnÃ¶tiger UI-Umbau
- ZusÃ¤tzliche PrÃ¼fung zum Mailtext:
  - im Repo wurde kein eigener Mail-/Template-Pfad fÃ¼r Supabase-Invite-/Recovery-Mails gefunden
  - deshalb nur der sichtbare Einladungs-/Erstlogin-Text im UI/Service minimal um App-/PC-Einstiegshinweise ergÃ¤nzt
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- UnverÃ¤ndert bewusst nicht Teil dieses Blocks:
  - lokale Ã„nderung in `KGV.Maui/KGV.Maui.csproj`
  - ungetrackte Batch-Dateien `Android_Wpf_release_batch_v4.bat` und `Android_Wpf_release_batch_v5.bat`
- Logische Ergebnisbewertung:
  - WPF und MAUI verwenden weiter denselben Shared-Auth-Flow
  - freier OTP-Request lÃ¤uft nicht mehr fÃ¼r unbekannte/nicht vorbereitbare Mailadressen
  - bestehende Mitglied-/AppUser-Reparatur passiert spÃ¤testens direkt nach OTP-BestÃ¤tigung und nicht erst nach Passwortvergabe oder erstem regulÃ¤rem Login

## 2026-03-30 â€“ Kurzfix: Rollback zeigt zurÃ¼ckgesetzte Release-Schritte jetzt nicht mehr grÃ¼n an

- Den gemeldeten Istzustand direkt am echten Release-Manager-Pfad geprÃ¼ft.
- Echte RestlÃ¼cke:
  - nach fehlgeschlagenem Push mit erfolgreichem Rollback blieben `Versionen erhÃ¶hen/schreiben`, `Marker schreiben` und `Commit ausfÃ¼hren` in der Schrittliste weiter auf `Erfolgreich`
  - fachlich war das irrefÃ¼hrend, weil diese Schritte zwar ausgefÃ¼hrt, durch den Rollback aber wieder zurÃ¼ckgenommen wurden
- Minimal korrigiert:
  - neuer Schrittzustand `ZurÃ¼ckgesetzt`
  - `ReleaseExecutionService` markiert nach erfolgreichem Rollback jetzt zurÃ¼ckgenommene Schritte explizit als `ZurÃ¼ckgesetzt`
  - betroffen sind dabei gezielt die im Rollback wirklich rÃ¼ckgÃ¤ngig gemachten Schritte wie Versionsschreiben, Marker und lokale Commits
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich, nur bekannte Warnungen in `HomeManagementPage.cs`
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 â€“ Kurzfix: Systemcheck blockierte fÃ¤lschlich an `Inno Setup`

- Reproduziert und geprÃ¼ft, warum der neue Release-Manager-Systemcheck trotz gÃ¼ltiger Umgebung als nicht startbar erschien.
- Echte Ursache:
  - `ReleasePreflightService.CheckInnoSetupAsync(...)` prÃ¼fte `ISCC.exe` Ã¼ber `ISCC.exe /?`
  - die lokale Inno-Setup-Installation ist dabei korrekt aufrufbar, liefert fÃ¼r den Hilfebildschirm aber ExitCode `1`
  - der Systemcheck behandelte diesen HilferÃ¼ckgabecode fÃ¤lschlich als Fehler und markierte dadurch die Umgebung als nicht startbar
- Minimal korrigiert:
  - `ReleasePreflightService` wertet den Inno-Setup-Aufruf jetzt als erfolgreich, wenn der Prozess sauber startet und der typische `Inno Setup`-Hilfetext zurÃ¼ckkommt
  - echte Startfehler bleiben weiterhin Fehler
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-30 â€“ Prompt 2/2: Release Manager auf transaktionalen Echt-Release mit Schrittstatus und Rollback gehÃ¤rtet

- Vor dem Block erneut den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main a97d047 [origin/main] Add release manager preflight system check`
  - `git log HEAD..origin/main` => leer
  - `git log origin/main..HEAD` => leer
- Danach nur den direkt betroffenen Release-Manager-Pfad gelesen:
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseMarkerService.cs`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseFolderService.cs`
  - `KGV.ReleaseManager/Services/ReleaseVersionFileService.cs`
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionRequest.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionResult.cs`
- Ehrlicher Istzustand vor dem Umbau:
  - der Echt-Release lief bereits Ã¼ber Versionsschreiben -> Build(s) -> ArtefaktverÃ¶ffentlichung -> Marker -> Git
  - Versionen wurden Ã¼ber `ReleaseVersionFileService.WriteTargetVersion(...)` erhÃ¶ht
  - RÃ¼cksetzen lief bisher nur grob Ã¼ber Dateibackups in `RollbackFailure(...)`
  - Marker wurde vor Commit/Push geschrieben
  - Commit und Push waren noch in einem kombinierten Pfad zusammengezogen
  - inkonsistente Restgefahr bestand vor allem bei Fehlern nach lokalem Commit bzw. wÃ¤hrend Push, weil der Endzustand dann nicht mehr sauber als transaktional bewertet wurde
  - im UI fehlte zusÃ¤tzlich noch eine echte Schrittliste und ein klarer Abschlussstatus fÃ¼r Erfolg / Fehler / Rollback erfolgreich / Rollback unvollstÃ¤ndig
- Minimal umgesetzt:
  - neue Ergebnis-/Statusmodelle fÃ¼r transaktionalen Releaseablauf ergÃ¤nzt:
    - Gesamtstatus
    - Schrittergebnisse
    - Restore-Ergebnis
  - `ReleaseExecutionService` intern auf nachvollziehbare fachliche Schrittkette gezogen:
    - Ausgangsversionen lesen
    - Versionen schreiben
    - WPF bauen
    - APK bauen
    - AAB bauen
    - VerÃ¶ffentlichungsordner befÃ¼llen
    - Marker schreiben
    - Commit ausfÃ¼hren
    - Push ausfÃ¼hren
    - Rollback
    - Abschluss
  - `VersionService` wird jetzt auch im echten Releasepfad verwendet, damit der Lauf die Ausgangsversionen ausdrÃ¼cklich liest und protokolliert
  - `ReleaseVersionFileService.RestoreBackups(...)` liefert jetzt ein echtes Restore-Ergebnis statt eines stillen `void`
  - lokales Git-Rollback ergÃ¤nzt:
    - ursprÃ¼ngliche HEADs werden vor Commit erfasst
    - lokale Commits kÃ¶nnen bei Fehlern vor erfolgreichem Push per `reset --hard` zurÃ¼ckgesetzt werden
  - Push-Reihenfolge so gezogen, dass zuerst das WPF-Zielrepo und zuletzt das Quellrepo gepusht wird; damit bleibt der markerfÃ¼hrende Quellrepo-Push so spÃ¤t wie mÃ¶glich
  - bei bereits erfolgtem Push wird der Zustand jetzt ehrlich als `rollback unvollstÃ¤ndig` gemeldet statt still als sauberer Rollback zu gelten
  - `ReleaseExecutionResult` enthÃ¤lt jetzt:
    - Gesamtstatus
    - Schrittliste
    - Marker-/Commit-/Push-Endzustand
  - bestehender Statusbereich im UI minimal erweitert:
    - zusÃ¤tzlicher Abschlussstatus
    - sichtbare Release-Schrittliste
    - bestehender Ausgabelog darunter beibehalten
  - `MainWindow.xaml.cs` Ã¼bernimmt echte Release-Ergebnisse jetzt in den ViewModel-Status; auch bei preflight-/settings-blockierten LÃ¤ufen wird der alte Schrittzustand nicht mehr still weiterverwendet
- Logging geschÃ¤rft:
  - Start echter Release
  - Start/Erfolg/Fehler je Release-Schritt
  - Rollback gestartet / erfolgreich / unvollstÃ¤ndig
  - Marker final ja/nein
  - Commit final ja/nein
  - Push final ja/nein
  - finale Gesamtbewertung
- Belastbar validiert:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- Logische PrÃ¼fung des neuen Pfads:
  - Dry Run bleibt marker-/commit-/push-frei
  - Fehler nach Versionsschreiben laufen jetzt klar in einen Rollbackpfad mit unterscheidbarer Erfolgs-/Teilfehlerbewertung
  - Marker/Commit/Push werden nur im Vollerfolg final als `ja` bewertet
  - bei bereits erfolgtem Push bleibt der Zustand bewusst ehrlich als `rollback unvollstÃ¤ndig` markiert; ein echter verteilter Remote-Rollback wurde nicht kÃ¼nstlich vorgetÃ¤uscht

## 2026-03-29 â€“ Prompt 1/2: Release Manager um sichtbaren Preflight-Systemcheck vor Dry Run und Echt-Release erweitert

- Vor dem Block den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main b4c33d9 [origin/main] Document invite auth user mapping race fix`
  - `git log HEAD..origin/main` => leer
  - `git log origin/main..HEAD` => leer
- Git-PrÃ¼fung lief erneut Ã¼ber den lokal vorhandenen Visual-Studio-Git-Pfad `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`, weil `git` im reinen PowerShell-Pfad weiter nicht direkt verfÃ¼gbar war.
- Danach nur den direkt betroffenen Release-Manager-Pfad gelesen:
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionRequest.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionResult.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseMarkerService.cs`
  - `KGV.ReleaseManager/Services/SettingsService.cs`
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/Services/BuildCommandService.cs`
  - `KGV.ReleaseManager/Services/ProcessExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseArtifactService.cs`
  - `KGV.ReleaseManager/Services/ReleaseFolderService.cs`
  - `KGV.ReleaseManager/Services/VersionService.cs`
- Ehrlicher Istzustand vor dem Umbau:
  - vorhanden waren bereits Settings-Validierung (`ValidateSettings`), Release-Request-Validierung (`ValidateRequest`), Versionserkennung, Releaseordner-Vorbereitung sowie Marker-/Git-/Build-Ablauf im Erfolgsfall
  - es fehlte aber ein sichtbarer, separater Preflight-Systemcheck vor dem echten Release
  - externe PflichtprÃ¼fungen wie `Git`/`ISCC` aufrufbar, Git-Repos initialisiert, Ausgabepfade beschreibbar/erstellbar und Keystore-/Projektdateien lesbar wurden vor dem Start noch nicht gesammelt als einzelne fachliche Checks ausgewertet
  - der bestehende UI-Statusbereich rechts war der sinnvollste Ort fÃ¼r die ErgÃ¤nzung; ein zweiter konkurrierender Statusbereich war nicht nÃ¶tig
- Minimal umgesetzt:
  - neue Release-Manager-Modelle fÃ¼r einzelne Preflight-Checks und Gesamtstatus ergÃ¤nzt
  - neuer `ReleasePreflightService` sammelt jetzt lesbare PflichtprÃ¼fungen mit Ergebnis `OK` / `Warnung` / `Fehler`
  - geprÃ¼ft werden jetzt â€“ abhÃ¤ngig vom ausgewÃ¤hlten Releaseziel â€“ u. a.:
    - Quellrepo vorhanden
    - Git-Executable vorhanden und aufrufbar
    - Quellrepo/WPF-Zielrepo als Git-Repo initialisiert und mit `origin` lesbar
    - Projekt-/Versionsdateien lesbar
    - Release-Ausgabeordner beschreibbar
    - WPF-Zielrepo vorhanden
    - WPF-Setup-Skript lesbar
    - Inno Setup aufrufbar
    - Android-Projekt vorhanden
    - APK-/AAB-Ausgabepfade vorhanden oder erstellbar
    - Android-Keystore vorhanden
    - Android-Keystore-Alias gesetzt
  - `GitCommandService` lÃ¶st `git.exe` jetzt zusÃ¤tzlich aus bekannten lokalen Installationspfaden auf, damit Systemcheck und spÃ¤terer Release nicht nur von einem zufÃ¤lligen PATH-Eintrag abhÃ¤ngen
  - der bestehende Statusbereich im UI wurde erweitert:
    - Zusammenfassung `bereit` / `eingeschrÃ¤nkt` / `nicht startbar`
    - Button `Systemcheck`
    - sichtbare Ergebnisliste je PflichtprÃ¼fung
    - bestehendes Status-Log darunter unverÃ¤ndert weitergenutzt
  - `RunDryRelease` und `RunRelease` fÃ¼hren jetzt zuerst automatisch den Preflight aus
  - bei Preflight-Fehlern wird sauber abgebrochen:
    - kein Marker
    - kein Commit
    - kein Push
    - kein halb gestarteter Release-Ablauf
  - Dry Run bleibt marker-/commit-/push-frei; die Dry-Run-Erfolgsmeldung wurde an den vorgeschalteten Systemcheck angepasst
  - Logging im Statuspfad ergÃ¤nzt:
    - Start des Systemchecks
    - Ergebnis je PflichtprÃ¼fung
    - Gesamtstatus
    - blockierter Release bei fehlerhaftem Preflight
    - klare Trennung zwischen Dry Run und Echt-Release
- Belastbar validiert:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich
- Logische PrÃ¼fung des neuen Produktpfads:
  - manueller Systemcheck nutzt denselben Service wie der automatische Preflight vor Dry Run/Echt-Release
  - Pflichtfehler blockieren den Start vor Passwortabfrage, Marker, Commit und Push
  - Dry Run bleibt weiterhin schreibfrei
  - der bestehende Release-Ablauf selbst wurde ansonsten nicht unnÃ¶tig umgebaut

## 2026-03-29 â€“ Prompt 1/1: Shared Invite-/AuthService-Fix gegen FK-Race bei `Nutzer hinzufÃ¼gen`

- Zuerst den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main d4e0830 [origin/main] Fix ambiguous Android Application reference`
  - `git log --oneline --decorate HEAD..origin/main` => leer
  - `git log --oneline --decorate origin/main..HEAD` => leer
- Ehrlicher Git-Befund vor dem Fix:
  - lokaler Stand und `origin/main` waren identisch
  - keine ungepushten lokalen Commits
  - es existierten aber uncommittete lokale Ã„nderungen im Arbeitsbaum:
    - `KGV.Maui/KGV.Maui.csproj`
    - `KGV.Wpf/KGV.Wpf.csproj`
    - `Android_Wpf_release_batch.bat`
    - `Android_Wpf_release_batch_v2.bat`
    - `Android_Wpf_release_batch_v3.bat`
  - ein Update vor der Umsetzung war nicht nÃ¶tig
- Danach nur den direkt betroffenen Shared-/UI-Pfad gelesen:
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Wpf/ViewModels/UserManagementViewModel.cs`
  - `KGV.Maui/Pages/MemberDetailPage.cs`
  - `KGV.Maui/Pages/UserManagementPage.cs`
  - `KGV.Maui/ViewModels/UserManagementViewModel.cs`
  - ergÃ¤nzend die direkt betroffenen Models/VertrÃ¤ge:
    - `KGV.Core/Interfaces/IAuthService.cs`
    - `KGV.Core/Models/AppUserDTO.cs`
    - `KGV.Core/Models/InviteUserAccountResult.cs`
    - `KGV.Core/Models/MitgliedRecord.cs`
    - `KGV.Infrastructure/Models/AppUserRecord.cs`
- Echte Shared-Ursache:
  - WPF und MAUI verwenden beide denselben Invite-Pfad `InviteUserAsync(...)` im gemeinsamen `AuthService`
  - die `authUserId` kommt dort aus `AppUserDTO.AuthUserId` oder â€“ falls noch leer â€“ frisch aus `EnsureAuthUserForInviteAsync(...)`
  - direkt danach setzte `EnsureMemberInviteMappingAsync(...)` sofort `mitglied.auth_user_id`
  - genau dort saÃŸ das reale Race: der referenzierte Auth-User war nach dem Invite/SignUp noch nicht in jedem Fall sofort belastbar Ã¼ber den FK auf `auth.users` sichtbar
  - dadurch entstand der beobachtete FK-Fehler `23503` auf `mitglied_auth_user_id_fkey`
- Minimal im Shared-Pfad umgesetzt:
  - nur `KGV.Infrastructure/Authentication/AuthService.cs` geÃ¤ndert
  - `EnsureMemberInviteMappingAsync(...)` besitzt jetzt einen kleinen Retry-/Wartepfad statt eines direkten Einmal-Updates
  - der Mapping-Schritt behandelt gezielt den FK-Fehler `23503` auf `mitglied_auth_user_id_fkey` als Sichtbarkeits-/Timing-Race und wartet kurz mit erneutem Versuch
  - keine UI-ScheinlÃ¶sung in WPF oder MAUI gebaut
  - `mitglied.auth_user_id` wird damit praktisch erst dann erfolgreich gesetzt, wenn der referenzierte Auth-User fÃ¼r den FK wirklich sichtbar ist
- Logging im Invite-Pfad geschÃ¤rft:
  - `mitgliedId`
  - maskierte E-Mail
  - verwendete `authUserId`
  - Invite-Schritt (`ensure-auth-user`, `ensure-member-mapping`, `ensure-app-user`, `request-recovery-otp`)
  - ob der Mapping-/Verifikationsschritt erfolgreich war
  - wie viele Retry-Versuche gebraucht wurden
  - PostgREST-Detail bei weiterem FK-Fehler
- Belastbar validiert:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` => erfolgreich
- Ehrliche E2E-Abgrenzung:
  - ein echter Invite-End-to-End-Lauf gegen Backend/Auth konnte in diesem Block nicht automatisiert ausgefÃ¼hrt werden
  - logisch geprÃ¼ft wurde aber der echte Shared-Codepfad: Auth-User-ID-Ermittlung -> FK-gefÃ¤hrdetes Mitglied-Mapping -> App-User-Record -> OTP-Request
  - der bisherige harte Einmal-Update wurde dort sauber durch den kleinen produktiven Retry-/Verifikationspfad ersetzt
- Git-Abschluss:
  - `git add -A`
  - `git commit -m "Fix invite auth user mapping race"`
  - Commit-Hash: `4ee6c03`
  - `git push origin main` => erfolgreich
  - final `git status -sb` => `## main...origin/main`

## 2026-03-29 â€“ Prompt 1/1: Android-Release-Buildblocker durch mehrdeutigen `Application`-Verweis minimal behoben

- Zuerst den echten Repo-Stand geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - im Arbeitsbaum lagen zusÃ¤tzlich nur residuale Testlauf-Artefakte aus dem fehlgeschlagenen Releaseversuch:
    - `KGV.Maui/KGV.Maui.csproj`
    - `KGV.Wpf/KGV.Wpf.csproj`
    - `Zielversion 0.2.10.txt`
- Danach nur die direkt betroffene Android-Datei gelesen:
  - `KGV.Maui/Platforms/Android/Services/AndroidRfidFeedbackService.cs`
- Ehrlicher Befund:
  - der Release-Manager selbst war nicht die Fehlerursache
  - der reale Blocker saÃŸ im MAUI-Android-Build bei `CS0104`: `Application` war mehrdeutig zwischen `Android.App.Application` und `Microsoft.Maui.Controls.Application`
  - die im Arbeitsbaum sichtbaren `csproj`-Ã„nderungen waren keine fachlichen ProduktÃ¤nderungen, sondern nur residuale Testlaufspuren; sie wurden vor dem Abschluss wieder bereinigt
- Minimal umgesetzt:
  - in `AndroidRfidFeedbackService.cs` den Zugriff auf den Android-Kontext per Alias `AndroidApplication = Android.App.Application` eindeutig gemacht
  - `RingtoneManager.GetRingtone(...)` verwendet jetzt eindeutig `AndroidApplication.Context`
  - keine VerhaltensÃ¤nderung, nur Compileblocker beseitigt
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -f net9.0-android -c Release -clp:ErrorsOnly -v:q` => erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -clp:ErrorsOnly -v:q` => erfolgreich
- Bestehende Warnungen in `HomeManagementPage.cs` wurden in diesem Block bewusst nicht umgebaut, da sie den Build nicht blockierten.

## 2026-03-29 â€“ Prompt 1/1: Release-Manager-End-to-End-Fluss gegengeprÃ¼ft, Marker-Statusanzeige vereinheitlicht

- Zuerst den echten Git-Stand gegen `origin/main` geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main` zeigt auf `origin/main`
  - `git log origin/main..HEAD` => leer
  - `git log HEAD..origin/main` => leer
- Der laut Instructions vorgegebene Git-Pfad unter `C:\Program Files\Microsoft Visual Studio\2022\...\git.exe` existiert lokal weiterhin nicht; fÃ¼r die Git-PrÃ¼fung und den Abschluss wird deshalb erneut der vorhandene Visual-Studio-Git-Pfad unter `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe` verwendet.
- Danach nur die direkt betroffenen Release-Manager-Dateien gelesen:
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesImportExportService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesHistoryService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseMarkerService.cs`
- End-to-End-PrÃ¼fung im aktuellen Produktpfad:
  - Versions-Refresh liest weiter direkt aus `KGV.Wpf.csproj` und `KGV.Maui.csproj` und aktualisiert Ã¼ber `ApplyVersionDetection(...)` die UI-Felder sowie die Zielversion konsistent neu.
  - Delta-Export verwendet weiter das Delta vor dem neuesten Marker in der newest-first-Logstruktur; ohne Marker bleibt der Initialfall auf dem gesamten relevanten Logbereich erhalten.
  - RÃ¼ckimport bleibt intakt: `ParseImportedSummary(...)` erkennt weiter `## WPF / Download` und `## Android / Play Store`; `SaveImportedSummary_Click(...)` speichert produktbezogen in die Historien.
  - Dry Run erzeugt weiter keine Marker, Commits oder Pushes; Echt-Release schreibt Marker und fÃ¼hrt danach Git-/Artefaktpfade aus.
  - echte RestlÃ¼cke gefunden: die zentrale Anzeige `LastKnownReleaseText` lief noch Ã¼ber die lokale Historienauswahl und war damit nicht zwingend derselbe Anker wie Delta-Export und Releaseabschluss.
- Minimal korrigiert:
  - `ReleaseMarkerService` kann jetzt direkt einen menschenlesbaren Statustext aus dem neuesten Fortschrittslog-Marker erzeugen.
  - `MainWindow.xaml.cs` verwendet fÃ¼r die zentrale Release-Anzeige jetzt bevorzugt genau diesen Marker-Status; nur ohne Marker fÃ¤llt die Anzeige weiter sauber auf den bisherigen Historienanker zurÃ¼ck.
  - `ReleaseExecutionService.ValidateAsync(...)` meldet den Dry Run jetzt ausdrÃ¼cklich als marker-/commit-/push-frei, damit die UI-RÃ¼ckmeldung im End-to-End-Fluss fachlich klarer ist.
- Belastbar validiert:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
- Ehrliche Abgrenzung:
  - kein echter produktiver Release mit externen Tools/echten Signaturdaten ausgefÃ¼hrt
  - End-to-End fachlich und technisch im Codepfad geprÃ¼ft; reale externe ToolausfÃ¼hrung blieb unverÃ¤ndert auÃŸerhalb dieses Minimalblocks

## 2026-03-29 â€“ Prompt 1/1 Abschlusslauf: Release-Manager-Block final verifiziert, Vollbuild bestÃ¤tigt, Git-Abschluss vorbereitet

- Vor Ã„nderungen den echten Git-Stand erneut geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main` zeigt auf `origin/main`
  - `git log origin/main..HEAD` => leer
  - `git log HEAD..origin/main` => leer
- Der in den Instructions genannte Git-Pfad unter `...Visual Studio\2022\...\git.exe` war lokal nicht vorhanden; die Git-PrÃ¼fung lief deshalb belastbar Ã¼ber den vorhandenen Visual-Studio-Git-Pfad `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`.
- Reale Ausgangslage im Arbeitsbaum vor diesem Abschlusslauf:
  - die Release-Manager-Dateien aus dem begonnenen Block waren weiter geÃ¤ndert
  - `.github/copilot-instructions.md` lag weiterhin zusÃ¤tzlich geÃ¤ndert vor
- Nur die direkt betroffenen Release-Manager-Dateien erneut gegengelesen:
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseMarkerService.cs`
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/Models/ReleaseExecutionRequest.cs`
- Ehrlicher Befund:
  - der NachschÃ¤rfungs-Patch in `MainWindow.xaml.cs` fÃ¼r die markerbasierte zentrale Statusanzeige war im aktuellen Stand vorhanden
  - in den direkt geprÃ¼ften Release-Manager-Dateien ergaben sich keine unmittelbaren Compile-/Using-/Namensprobleme
  - deshalb kein weiterer Produktcode auf Verdacht geÃ¤ndert
- ZusatzprÃ¼fung zu `.github/copilot-instructions.md`:
  - Datei enthÃ¤lt real nur die bereits vorhandene zusÃ¤tzliche Verifikations-/Abschlussregel
  - inhaltlich in diesem Lauf nicht neu umgebaut
- Belastbar nachgezogen und bestÃ¤tigt:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug -clp:ErrorsOnly` => erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug -clp:ErrorsOnly` => erfolgreich, vorhandene Warnungen bleiben auÃŸerhalb dieses Blocks bestehen
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug -clp:ErrorsOnly` => erfolgreich, vorhandene Warnungen in `HomeManagementPage.cs` bleiben auÃŸerhalb dieses Blocks bestehen
  - `dotnet build KGV.slnx -c Debug -clp:ErrorsOnly` => erfolgreich

## 2026-03-29 â€“ Prompt 1/1: Release Manager um Commit/Push, Release-Marker, Delta-Export und Versions-Refresh erweitert

- Den realen Repo-Stand zuerst geprÃ¼ft:
  - `git status -sb` => `## main...origin/main`
  - im Arbeitsbaum lag vor dem Block bereits zusÃ¤tzlich eine uncommittete Ã„nderung an `.github/copilot-instructions.md`
- Danach nur den direkt betroffenen Release-Manager-Pfad gelesen:
  - `KGV.ReleaseManager/Services/GitCommandService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - ergÃ¤nzend Release-/Artefakt-/Versionsservices und `KGV.ReleaseManager/README.md`
- Minimal umgesetzt:
  - `GitCommandService` um reale `add`-/`commit`-/`push`-Kommandos erweitert
  - echter Release-Marker-Service ergÃ¤nzt; Markerformat:
    - `- [RELEASE_MARKER] Version {version} erfolgreich erstellt am {yyyy-MM-dd HH:mm}`
  - Marker wird nur im erfolgreichen Echt-Release geschrieben; Dry Run schreibt keinen Marker
  - der Export fÃ¼r Versionszusammenfassungen nutzt jetzt das Log-Delta seit dem letzten Release-Marker; ohne Marker wird der gesamte relevante Logbereich verwendet
  - `MainWindow` erhielt den Button `Aktuelle Versionen neu einlesen`; er lÃ¤dt die Versionen erneut direkt aus den Projektdateien und stÃ¶ÃŸt keinen Release an
  - der echte Release-Flow hÃ¤ngt Commit/Push jetzt an den Erfolgsablauf fÃ¼r Quellrepo und WPF-Zielrepo
- Fehlerverhalten bewusst klein gehalten:
  - Marker-Schreibfehler fÃ¼hren vor Git-Schreiboperationen sauber in den Fehlerpfad zurÃ¼ck
  - Dry Run fÃ¼hrt keine Git-Schreiboperationen aus
  - Git-Fehler werden sichtbar protokolliert; bei Fehlern vor lokalem Commit wird weiter sauber zurÃ¼ckgerollt
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.slnx -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1 Variante A: Git-Stand strikt geprÃ¼ft, sichtbaren MAUI-Mitgliedsdetail-Abschluss erneut verifiziert, kein Produktcode offen

- Zuerst den echten Git-Stand geprÃ¼ft:
  - `git fetch origin`
  - `git status -sb` => `## main...origin/main`
  - `git branch -vv` => `main` zeigt direkt auf `origin/main`
  - `git log HEAD..origin/main` => leer
  - `git log origin/main..HEAD` => leer
- Ehrlicher Git-Befund zu Beginn:
  - lokaler Branch war nicht hinter `origin/main`
  - lokaler Branch war nicht vor `origin/main`
  - keine ungepushten lokalen Commits
  - keine uncommitteten lokalen Ã„nderungen
  - Arbeitsstand nach `fetch` war bereits exakt auf dem richtigen Stand (`HEAD == origin/main`)
- Danach nur die direkt betroffenen Dateien geprÃ¼ft:
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `KGV.Maui/Pages/MemberDetailPage.cs`
  - `KGV.Maui/Pages/UserManagementPage.cs`
  - `KGV.Maui/ViewModels/UserManagementViewModel.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
- Ehrlicher Repo-Befund:
  - `â†³ Stammdaten` zeigt bereits auf `MemberDetailPage`
  - `MeineDatenPage` bleibt der Eigenpfad des eingeloggten Nutzers
  - `MemberDetailPage` enthÃ¤lt bereits `Nutzer hinzufÃ¼gen`
  - die sichtbare Mailregel ist bereits vollstÃ¤ndig umgesetzt
  - `UserManagementPage`/`UserManagementViewModel` verwenden bereits den sichtbaren Invite-Wortlaut `Nutzer hinzufÃ¼gen`
  - `SupabaseService.UpdateMitgliedAsync(...)` enthÃ¤lt bereits die Backend-Regel fÃ¼r Mail
- Konsequenz:
  - kein Produktcode geÃ¤ndert
  - nur Verifikations-/Loglauf durchgefÃ¼hrt
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: sichtbaren MAUI-Mitgliedsdetail-Abschluss gegen GitHub-main erneut verifiziert, kein weiterer Produktcode offen

- Den realen Stand erneut direkt gegen `origin/main` geprÃ¼ft.
- Ehrlicher GitHub-main-Befund in den direkt betroffenen Dateien:
  - `AdminShell` zeigt `â†³ Stammdaten` auf `MemberDetailPage`; der frÃ¼here Pfad auf `MeineDatenPage` ist auf `origin/main` in diesem Block real nicht mehr offen
  - `MeineDatenPage` bleibt der Eigenpfad des eingeloggten Nutzers
  - `MemberDetailPage` enthÃ¤lt bereits den produktiven Admin-Flow `Nutzer hinzufÃ¼gen`
  - die sichtbare Mailregel in `MemberDetailPage` ist bereits vollstÃ¤ndig umgesetzt:
    - E-Mail als echtes Eingabefeld
    - editierbar nur ohne gebundenen App-/Auth-User
    - read-only mit Hinweis auf Self-Service-MailÃ¤nderung bei vorhandenem App-/Auth-User
  - `UserManagementViewModel` begrenzt den Self-Service-Mailpfad bereits korrekt auf das aktuell angemeldete Konto
  - `SupabaseService.UpdateMitgliedAsync(...)` enthÃ¤lt die Shared-/Backend-Regel bereits unverÃ¤ndert korrekt
  - `UserManagementPage` / `UserManagementViewModel` verwenden im sichtbaren Invite-Wortlaut bereits `Nutzer hinzufÃ¼gen`; der alte Text `Einladung / Erstlogin-Code senden` ist auf `origin/main` in diesem Pfad nicht mehr offen
- Konsequenz dieses Laufs:
  - kein weiterer MAUI-Produktcode geÃ¤ndert, weil die in der Prompt-Annahme genannten GitHub-main-Restpunkte real bereits geschlossen waren
  - nur die Logs auf den erneut verifizierten GitHub-main-Iststand fortgeschrieben
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: MAUI-Stammdatenpfad, sichtbare Mailregel und Invite-Benennung gegen GitHub-main erneut verifiziert, kein weiterer Produktcode mehr offen

- Den realen Stand erneut direkt gegen `origin/main` geprÃ¼ft.
- Ehrlicher GitHub-main-Befund in den direkt betroffenen Dateien:
  - `AdminShell` zeigt `â†³ Stammdaten` bereits auf `MemberDetailPage`
  - `MeineDatenPage` bleibt der Eigenpfad des eingeloggten Nutzers
  - `MemberDetailPage` enthÃ¤lt bereits den produktiven `Nutzer hinzufÃ¼gen`-Flow
  - `MemberDetailPage` zeigt die sichtbare Mailregel bereits korrekt:
    - E-Mail als echtes Eingabefeld
    - editierbar nur ohne gebundenen App-/Auth-User
    - read-only mit Hinweis auf Self-Service-MailÃ¤nderung bei vorhandenem App-/Auth-User
  - `UserManagementViewModel` begrenzt die MailÃ¤nderung bereits korrekt auf das aktuell angemeldete Konto
  - `SupabaseService.UpdateMitgliedAsync(...)` erzwingt die Backend-Regel fÃ¼r Mail bereits im Shared-Pfad
  - `UserManagementPage` / `UserManagementViewModel` verwenden im sichtbaren Invite-Wortlaut bereits `Nutzer hinzufÃ¼gen`; der frÃ¼here Text `Einladung / Erstlogin-Code senden` ist auf `origin/main` in diesem Pfad nicht mehr der reale Stand
- Konsequenz dieses Laufs:
  - die in der Prompt-Annahme genannten GitHub-main-RestwidersprÃ¼che waren real nicht mehr offen
  - deshalb kein weiterer MAUI-Produktcode geÃ¤ndert
  - nur die Logs auf den erneut verifizierten GitHub-main-Iststand fortgeschrieben
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: MAUI-Stammdatenpfad, `Nutzer hinzufÃ¼gen` und Mailregel im Mitgliedskontext sauber abgeschlossen

- Den realen Stand zuerst erneut direkt gegen `origin/main` geprÃ¼ft.
- Ehrlicher GitHub-main-Befund in den direkt betroffenen MAUI-Dateien:
  - `AdminShell` zeigt `â†³ Stammdaten` bereits auf `MemberDetailPage`; der frÃ¼here Pfad auf `MeineDatenPage` war auf `origin/main` bereits nicht mehr der reale Stand
  - `MemberDetailPage` enthÃ¤lt bereits produktives Speichern sowie den produktiven Button `Nutzer hinzufÃ¼gen`
  - `MeineDatenPage` bleibt der Eigenpfad des eingeloggten Nutzers
  - real offen waren noch die WidersprÃ¼che im mobilen Mitgliedskontext:
    - Mailadresse in `MemberDetailPage` blieb trotz erlaubtem Sonderfall ohne Auth-User faktisch nicht speicherbar
    - `UserManagementPage` fÃ¼hrte den Invite-Text bzw. den gebundenen Kontext noch nicht sauber genug ohne Doppelangebot
- Minimal umgesetzt:
  - `AdminShell` bewusst unverÃ¤ndert gelassen, weil `â†³ Stammdaten` auf GitHub `main` bereits korrekt auf `MemberDetailPage` verdrahtet war
  - `MemberDetailPage` zeigt die Mailadresse jetzt als echtes Eingabefeld, aber nur editierbar solange noch kein gebundener App-/Auth-User existiert
  - bei vorhandenem App-/Auth-User bleibt das Mailfeld read-only und zeigt klar den Hinweis auf den Self-Service-MailÃ¤nderungsweg des Nutzers selbst
  - der bisherige pauschale Mail-Reset beim Speichern wurde im MAUI-Pfad auf den erlaubten Sonderfall ohne Auth-User geÃ¶ffnet
  - der gemeinsame Speicherdienst `UpdateMitgliedAsync(...)` erzwingt dieselbe Fachregel zusÃ¤tzlich im Shared-Pfad: MailÃ¤nderung nur ohne gebundenen Auth-User
  - `Nutzer hinzufÃ¼gen` bleibt sichtbar im `MemberDetailPage`-Mitgliedskontext und bleibt Admin-only
  - `UserManagementPage`/`UserManagementViewModel` wurden im gebundenen Mitgliedskontext sprachlich geschÃ¤rft; der Invite wird dort nicht mehr doppelt neben dem Stammdatenpfad angeboten
  - der bestehende Self-Service-MailÃ¤nderungsweg im User-Management bleibt erhalten
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: MAUI-ZÃ¤hlerwechsel-Folgepfad gegen GitHub-main erneut verifiziert, kein weiterer Produktcode mehr offen

- Den realen Stand erneut direkt gegen `origin/main` geprÃ¼ft.
- Ehrlicher Befund auf GitHub `main` in den direkt betroffenen MAUI-Dateien:
  - `Routing.RegisterRoute(nameof(ZaehlerwechselAusbauPage), ...)` ist bereits vorhanden
  - `Routing.RegisterRoute(nameof(ZaehlerwechselEinbauPage), ...)` ist bereits vorhanden
  - `AddTransient<ZaehlerwechselAusbauPage>()` ist bereits vorhanden
  - `AddTransient<ZaehlerwechselEinbauPage>()` ist bereits vorhanden
  - `IRfidFeedbackService` und `AndroidRfidFeedbackService` sind bereits vorhanden und in `MauiProgram` registriert
  - der Quittungston wird bereits in `RfidScanContextViewModel.ResolveAsync()` bei erfolgreicher bekannter KontextauflÃ¶sung ausgelÃ¶st
  - der RÃ¼ckweg nach erfolgreichem Ausbau/Einbau bereinigt den Workflow-State bereits und fÃ¼hrt wieder auf einen frischen ZÃ¤hlerwechsel-Scanpfad
- Konsequenz dieses Abschlusslaufs:
  - kein weiterer Produktcode geÃ¤ndert, weil die in der Prompt-Annahme genannten GitHub-main-RestlÃ¼cken real bereits geschlossen waren
  - nur die Logs auf den verifizierten GitHub-main-Iststand fortgeschrieben
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: MAUI-ZÃ¤hlerwechsel-Block gegen GitHub-main verifiziert, DI-RestlÃ¼cke real geschlossen und dann gepusht

- Den realen Stand erneut gegen `origin/main` geprÃ¼ft.
- Ehrlicher Befund auf GitHub `main`:
  - `ZaehlerwechselPage` mit `Weiter zum Ausbau` / `Weiter zum Einbau` war bereits vorhanden
  - `ZaehlerwechselAusbauPage` und `ZaehlerwechselEinbauPage` existierten bereits
  - die Shell-Routen fÃ¼r beide Folgeseiten waren auf `origin/main` bereits registriert
  - die echte noch offene RestlÃ¼cke lag in `KGV.Maui/MauiProgram.cs`: die beiden Folgeseiten waren dort lokal vor dem Abschluss noch nicht wirklich als `Transient` registriert
  - der kleine Android-RFID-Feedback-Service und der frische RÃ¼ckweg lagen lokal bereits vor, waren aber noch nicht nach `origin/main` gepusht
- Minimal umgesetzt:
  - `MauiProgram` jetzt real um `AddTransient<ZaehlerwechselAusbauPage>()` und `AddTransient<ZaehlerwechselEinbauPage>()` ergÃ¤nzt
  - bestehender Folgepfad Ã¼ber `Shell.Current.GoToAsync(...)` unverÃ¤ndert beibehalten
  - bestehender RFID-Quittungston bleibt an `RfidScanContextViewModel.ResolveAsync()` bei erfolgreicher bekannter KontextauflÃ¶sung hÃ¤ngen
  - Workflow-State wird nach erfolgreichem Ausbau/Einbau weiter per `_workflowState.Clear()` bereinigt und danach auf einen frischen ZÃ¤hlerwechsel-Scanpfad zurÃ¼ckgefÃ¼hrt
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich

## 2026-03-29 â€“ Prompt 1/1: MAUI-ZÃ¤hlerwechsel-Routing fertig angeschlossen und mobiler RFID-Quittungston ergÃ¤nzt

- Vor Beginn den realen Repo-Stand gegen `origin/main` geprÃ¼ft (`git fetch origin`, `HEAD == origin/main`) und dann gezielt gelesen:
  - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
  - `KGV.Maui/Pages/ZaehlerwechselAusbauPage.cs`
  - `KGV.Maui/Pages/ZaehlerwechselEinbauPage.cs`
  - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`
  - `KGV.Maui/State/ZaehlerwechselWorkflowState.cs`
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/Platforms/Android/Services/AndroidNfcScanService.cs`
- Ehrlicher Befund:
  - die Shell-Routen fÃ¼r `ZaehlerwechselAusbauPage` und `ZaehlerwechselEinbauPage` waren im Registrar bereits vorhanden
  - die kleine reale LÃ¼cke saÃŸ aber noch in `MauiProgram`: die beiden Folgeseiten waren dort noch nicht im bestehenden DI-/Routenmuster als Transients registriert
  - dadurch war der Folgepfad nach `Shell.Current.GoToAsync(route)` noch nicht sauber vollstÃ¤ndig an dieselbe MAUI-Routinglogik angeschlossen wie die Ã¼brigen Seiten
- Minimal umgesetzt:
  - `MauiProgram` um `AddTransient<ZaehlerwechselAusbauPage>()` und `AddTransient<ZaehlerwechselEinbauPage>()` ergÃ¤nzt
  - der bestehende Aufruf `Shell.Current.GoToAsync(route)` in `ZaehlerwechselPage` blieb unverÃ¤ndert
  - nach erfolgreichem Ausbau/Einbau wird der Workflow-State jetzt bereinigt und bewusst auf einen frischen ZÃ¤hlerwechsel-Scanpfad zurÃ¼ckgefÃ¼hrt:
    - zuerst `//ablesen`
    - danach erneut `ZaehlerwechselPage`
  - damit bleibt nach erfolgreichem Speichern kein alter Formular-/Workflow-Kontext hÃ¤ngen
  - kleiner mobiler RFID-Quittungston ergÃ¤nzt:
    - neue Service-Schnittstelle `IRfidFeedbackService`
    - Android-Implementierung `AndroidRfidFeedbackService`
    - AuslÃ¶sung in `RfidScanContextViewModel.ResolveAsync()` nach erfolgreicher bekannter RFID-KontextauflÃ¶sung
  - wenn das GerÃ¤t den Ton nicht abspielen kann, lÃ¤uft der Scanpfad fachlich trotzdem normal weiter
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
- Logischer PrÃ¼fpfad nach dem Block:
  - ZÃ¤hlerwechsel Ã¶ffnen
  - RFID scannen
  - `Weiter zum Ausbau` / `Weiter zum Einbau` Ã¶ffnet die jeweilige Seite wirklich
  - Speichern erfolgreich
  - Erfolgsmeldung erscheint
  - RÃ¼ckweg landet auf einem frischen ZÃ¤hlerwechsel-Scanpfad
  - alter Workflow-Kontext bleibt nicht hÃ¤ngen
  - RFID-Quittungston wird bei erfolgreicher bekannter KontextauflÃ¶sung ausgelÃ¶st oder fÃ¤llt lautlos aus, ohne den Flow zu blockieren

## 2026-03-29 â€“ Prompt 1/1: MAUI-ZÃ¤hlerwechsel nach RFID-Scan in echte Ausbau-/Einbauformulare verzweigt

- Vor Beginn den realen Repo-Stand gegen `origin/main` geprÃ¼ft (`git fetch origin`, Vergleich `HEAD` gegen `origin/main`) und danach gezielt gelesen:
  - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
  - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - `KGV.Maui/ViewModels/RfidScanContextViewModel.cs`
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Core/Models/RfidScanContextResult.cs`
  - `KGV.Core/Models/RfidScanContextRecord.cs`
  - `KGV.Core/Models/StromzaehlerInsertRecord.cs`
  - `KGV.Core/Models/WasserzaehlerInsertRecord.cs`
  - `KGV.Core/Models/AblesungInsertRecord.cs`
  - WPF grob als Referenz: `ZaehlerwechselScanViewModel`, `ZaehlerwechselEinbauViewModel`, `ZaehlerwechselAusbauViewModel`
- Ehrlicher Befund:
  - der MAUI-Scanpfad lÃ¶ste den RFID-Kontext bereits produktiv auf
  - `ZaehlerwechselPage` blieb danach aber nur beim Entscheidungstext stehen
  - es gab noch keinen mobilen Folgepfad, obwohl fachlich bereits unterscheidbar war zwischen:
    - bekanntem Tag mit aktivem ZÃ¤hler => Ausbau nÃ¶tig
    - bekanntem Tag ohne aktiven ZÃ¤hler => Einbau nÃ¶tig
- Minimal umgesetzt:
  - kleiner Zustand `ZaehlerwechselWorkflowState` ergÃ¤nzt, damit der aufgelÃ¶ste Scan-Kontext ohne Query-String-Bastelei stabil an Folgeformulare Ã¼bergeben wird
  - `RfidScanContextViewModel` liefert jetzt sichtbare Weiter-ZustÃ¤nde fÃ¼r Ausbau/Einbau
  - `RfidScanWorkflowPage` wurde minimal um einen optionalen Aktionsbereich erweitert
  - `ZaehlerwechselPage` zeigt jetzt nach erfolgreichem Scan klar sichtbare Folge-CTAs:
    - `Weiter zum Ausbau`
    - `Weiter zum Einbau`
  - neue MAUI-Seite `ZaehlerwechselAusbauPage` ergÃ¤nzt:
    - zeigt Parzelle, Medium, aktiven ZÃ¤hler und ZÃ¤hlernummer
    - speichert produktiv zuerst die Schlussablesung
    - beendet danach den aktiven ZÃ¤hler Ã¼ber `ausgebaut_am`
  - neue MAUI-Seite `ZaehlerwechselEinbauPage` ergÃ¤nzt:
    - zeigt Parzelle, Medium und RFID-Kontext
    - legt produktiv neuen ZÃ¤hler in der passenden Tabelle an
    - speichert danach die Anfangsablesung fÃ¼r den neu aktiven ZÃ¤hler
  - Medium-Unterscheidung bleibt sauber auf den vorhandenen Produktivpfaden:
    - Strom => `AddStromzaehlerAsync(...)` / `SetStromzaehlerAusgebautAmAsync(...)`
    - Wasser => `AddWasserzaehlerAsync(...)` / `SetWasserzaehlerAusgebautAmAsync(...)`
    - Ablesung immer Ã¼ber `AddAblesungAsync(...)`
  - keine Foto-/Upload-Schattenlogik ergÃ¤nzt, weil dafÃ¼r in diesem Block kein stabiler vorhandener MAUI-Pfad gebraucht wurde
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
- Logischer PrÃ¼fpfad nach dem Block:
  - ZÃ¤hlerwechsel Ã¶ffnen
  - RFID scannen / UID prÃ¼fen
  - bekannter Kontext mit aktivem ZÃ¤hler => `Weiter zum Ausbau`
  - bekannter Kontext ohne aktiven ZÃ¤hler => `Weiter zum Einbau`
  - unbekannter Tag => kein Folgeformular
  - Ausbau speichert Schlussablesung + setzt `ausgebaut_am`
  - Einbau legt ZÃ¤hler an + speichert Anfangsablesung

## 2026-03-29 â€“ Prompt 1/1: MAUI mitgliedsgebundene Stammdaten und `Nutzer hinzufÃ¼gen` auf den echten Mitgliedskontext gezogen

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - `KGV.Maui/Pages/MyProfilePage.cs`
  - `KGV.Maui/Pages/UserManagementPage.cs`
  - `KGV.Maui/ViewModels/UserManagementViewModel.cs`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `KGV.Maui/State/MemberContextState.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/ShellRouteRegistrar.cs`
  - fachliche Referenz: `KGV.Wpf/ViewModels/MemberDetailViewModel.cs`, `KGV.Wpf/ViewModels/UserManagementViewModel.cs`
- Ehrlicher Befund:
  - `â†³ Stammdaten` im mobilen Admin-/Vorstandskontext zeigte real nur auf `MeineDatenPage`
  - dort fÃ¼hrte `Bearbeiten` fÃ¼r fremde Mitglieder nicht auf einen echten Mitgliedseditor, sondern auf den Eigenprofilpfad bzw. in eine dokumentierte Sackgasse
  - der produktive Invite-/Erstlogin-Flow war mobil bereits vorhanden, wurde im gebundenen Mitgliedskontext aber noch nicht passend sichtbar und nicht mit dem WPF-Wortlaut `Nutzer hinzufÃ¼gen` angeboten
- Minimal umgesetzt:
  - neue echte MAUI-Mitgliedsdetailseite `KGV.Maui/Pages/MemberDetailPage.cs` angelegt
  - `â†³ Stammdaten` im `AdminShell` zeigt jetzt auf diese Seite statt auf den bisherigen Eigen-/ReadOnly-Pfad
  - die neue Seite lÃ¤dt das ausgewÃ¤hlte Mitglied Ã¼ber `MemberContextState.SelectedMember.Id` und speichert produktiv Ã¼ber den vorhandenen Mitglied-Lock-/`UpdateMitgliedAsync(...)`-Pfad
  - der bisherige Eigenpfad `MeineDatenPage` / `MyProfilePage` bleibt fÃ¼r den selbst eingeloggten Nutzer unverÃ¤ndert bestehen
  - im mitgliedsgebundenen Detailpfad ist `Nutzer hinzufÃ¼gen` jetzt als klarer Admin-CTA sichtbar, wenn das ausgewÃ¤hlte Mitglied noch keinen App-User hat
  - derselbe produktive Invite-/Erstlogin-Flow wird dabei weiterverwendet; es wurde kein Dummy-Flow ergÃ¤nzt
  - `UserManagementPage` / `UserManagementViewModel` verwenden im gebundenen Mitgliedskontext jetzt ebenfalls den WPF-Wortlaut `Nutzer hinzufÃ¼gen` und bieten die Invite-Aktion bei bereits vorhandenem App-User nicht mehr fÃ¤lschlich aktiv an
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
- Logischer PrÃ¼fpfad nach dem Block:
  - Mitglied in der Suche auswÃ¤hlen
  - `â†³ Stammdaten` Ã¶ffnen
  - ausgewÃ¤hltes Mitglied bearbeiten und produktiv speichern
  - Mitglied ohne App-User wÃ¤hlen
  - `Nutzer hinzufÃ¼gen` im Detailpfad sichtbar
  - derselbe Invite-/Erstlogin-Flow wie bisher wird aufgerufen

## 2026-03-29 â€“ Prompt 1/1: MAUI-Release-Loginpfad gegen echten Supabase-Connection-Failure im Android-Netzwerkpfad gehÃ¤rtet

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Infrastructure/Supabase/SupabaseClientFactory.cs`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/MainApplication.cs`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `.github/copilot-instructions.md`
  - reale Git-Lage, Release-APK und angeschlossenes GerÃ¤t per `adb devices`
- Ehrlicher Befund:
  - `appsettings.json`, Supabase-Konfiguration und Login-Startmarker waren im Release bereits korrekt sichtbar
  - der eigentliche Fehler lag nicht mehr in Icon, Appsettings oder Benutzerlogik, sondern im echten Netzwerkpfad vor dem Gotrue-Login
  - im Android-Manifest fehlte im aktuellen Stand die Berechtigung `android.permission.INTERNET`; das passt fachlich zu `GotrueException: Connection failure` im Releasepfad
  - `AuthService` lieferte fÃ¼r den GerÃ¤te-Log schon grobe Fehlmarker, aber noch keinen ausreichend prÃ¤zisen Netzwerk-/InnerException-Kontext
- Minimal umgesetzt:
  - `AndroidManifest.xml` um `android.permission.INTERNET` ergÃ¤nzt, damit Release denselben HTTPS-/Supabase-Pfad wie Debug nutzen kann
  - `AuthService.LoginAsync(...)` protokolliert jetzt zusÃ¤tzlich maskiert:
    - Zielschema
    - Zielhost der Supabase-URL
    - Exception-Typ
    - InnerException-Typ
    - maskierte InnerException-Nachricht
  - bestehende `KGV`-Logcat-Marker bleiben unverÃ¤ndert erhalten; die neue Netzwerkdiagnose kommt ergÃ¤nzend dazu
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=apk` erfolgreich; signierte Release-APK erzeugt
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - Release-APK auf echtes GerÃ¤t installiert (`adb install -r ...Signed.apk` erfolgreich)
  - `adb logcat -s KGV` nach App-Start zeigt weiter sauber:
    - `APP_START`
    - `APPSETTINGS_LOAD_OK`
    - `SUPABASE_CONFIG_PRESENT_YES`
  - `adb shell dumpsys package de.kgv.oberrothenbach | findstr INTERNET` bestÃ¤tigt die Berechtigung jetzt auf dem installierten Release-Paket
- Offener Rest fÃ¼r den nÃ¤chsten echten GerÃ¤telogin:
  - der Login selbst konnte in diesem Lauf nicht automatisiert ausgelÃ¶st werden, weil keine Zugangsdaten/keine UI-Automation im Block vorlagen
  - falls der Fehler wider Erwarten weiter besteht, liefert `adb logcat -s KGV` jetzt den prÃ¤ziseren Netzwerk-/InnerException-Kontext statt nur `Connection failure`

## 2026-03-28 â€“ Prompt 1/1: MAUI-Release-Loginpfad mit echtem Android-Logcat-Tag diagnostizierbar gemacht

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/Services/Diagnostics/AppFileLog.cs`
  - reale Git-/Buildlage und aktuelles `adb devices`
- Ehrlicher Befund:
  - das Dateilogging fÃ¼r Release war bereits vorhanden, aber die Diagnose landete nicht zuverlÃ¤ssig mit festem App-Tag in Android Logcat
  - `AuthService` loggte den Loginpfad schon maskiert, aber ohne klar suchbare Login-Fehlermarker fÃ¼r Release
  - `KGV.Maui.csproj` war fÃ¼r den Loginpfad bereits debug-nah genug gehÃ¤rtet: kein AOT, `AndroidLinkMode=None`, `PublishTrimmed=false`
  - ein echter GerÃ¤tetest/Logcat-Livecheck war in diesem Lauf nicht mÃ¶glich, weil `adb devices` kein verbundenes GerÃ¤t zeigte
- Minimal umgesetzt:
  - bestehendes Diagnose-Logging auf festen Android-Logcat-Tag `KGV` erweitert; dieselben EintrÃ¤ge laufen weiter zusÃ¤tzlich in `kgv-release.log`
  - neue explizite Marker im Startup-/Konfigurationspfad ergÃ¤nzt:
    - `APP_START`
    - `APPSETTINGS_LOAD_OK` / `APPSETTINGS_LOAD_FAIL`
    - `SUPABASE_CONFIG_PRESENT_YES` / `SUPABASE_CONFIG_PRESENT_NO`
  - Loginpfad in `LoginPage` um klare Marker ergÃ¤nzt:
    - `LOGIN_STARTED`
    - `LOGIN_RESULT_SUCCESS`
    - `LOGIN_RESULT_FAIL`
    - `LOGIN_EXCEPTION:...`
  - `AuthService.LoginAsync(...)` um konkrete, maskierte Release-Diagnose fÃ¼r die kritischen Stufen ergÃ¤nzt (`GET_CLIENT`, `SIGN_IN`, Null-Session, fehlende UserId, Guid-/Role-Lookup, Gotrue-/Unexpected-Exception)
  - Rootwechsel nach erfolgreichem Login jetzt als echter Erfolgspfad ausgewertet; scheitert der App-Root-Wechsel, wird dies als `LOGIN_EXCEPTION:APP_ROOT_SWITCH_FAILED` protokolliert statt still als Erfolg durchzulaufen
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=apk` erfolgreich; signierte Release-APK erzeugt
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - `adb devices` zeigte in diesem Lauf kein verbundenes GerÃ¤t; daher kein echter `adb logcat`-/GerÃ¤te-Login-Test mÃ¶glich
- Diagnosehinweis fÃ¼r den nÃ¤chsten GerÃ¤tetest:
  - `adb logcat -s KGV`
  - alternativ ungefiltert nach `APP_START`, `LOGIN_STARTED`, `LOGIN_EXCEPTION`, `LOGIN_RESULT_FAIL` suchen

## 2026-03-28 â€“ Prompt 1/1: MAUI-Release mit Dateilogging und expliziten Android-Launcher-Ressourcen gehÃ¤rtet

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - `KGV.Maui/MainApplication.cs`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV.Maui/Resources/AppIcon/*`
  - realen Git-Status sowie reale Release-Artefakte unter `KGV.Maui/bin/Release/net9.0-android`
- Ehrlicher Befund:
  - `AuthService` loggte den Loginpfad bereits maskiert; im Release fehlte aber weiterhin ein echtes Dateilog im AppData-Verzeichnis
  - das reine `MauiIcon`-Setup war trotz vorhandener `@mipmap/appicon`-Referenzen praktisch nicht belastbar genug; fÃ¼r Android fehlten robuste explizite Launcher-Ressourcen im Projektpfad
  - `appsettings.json` und Supabase-Konfiguration wurden im Startup schon validiert, aber noch nicht zusÃ¤tzlich in ein auslesbares GerÃ¤te-Dateilog geschrieben
  - reale GerÃ¤tediagnose per `adb` war in diesem Lauf nicht mÃ¶glich; deshalb Fokus auf build- und paketierbare Release-HÃ¤rtung mit auslesbarer Logdatei
- Minimal umgesetzt:
  - kleinen Datei-Logger unter `KGV.Maui/Services/Diagnostics/*` ergÃ¤nzt; Logpfad ist zur Laufzeit `%LocalApplicationData%/kgv-release.log` bzw. MAUI-`AppDataDirectory`
  - `MauiProgram` schreibt jetzt Appstart, `appsettings.json`-Ladestatus, Supabase-Konfigurationsstatus und den Diagnose-Logpfad in die Logdatei; bestehende `ILogger`-EintrÃ¤ge aus `AuthService` laufen dort ebenfalls auf
  - `LoginPage` schreibt Start/Fehlschlag maskiert mit und verweist bei Release-Fehlern auf das Diagnose-Log statt nur stumm/generisch zu scheitern
  - Release explizit weiter ohne AOT gehalten und zusÃ¤tzlich mit `PublishTrimmed=false` debug-nÃ¤her fixiert
  - Android-Launcher-Icon nicht mehr nur Ã¼ber `MauiIcon`, sondern Ã¼ber explizite Android-Ressourcen unter `Platforms/Android/Resources/*` abgesichert; Manifest setzt `android:icon` und `android:roundIcon` jetzt ebenfalls direkt
- Validierung:
  - `dotnet clean KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=apk` erfolgreich; signierte APK erzeugt
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=aab` erfolgreich; signierte AAB erzeugt
  - `aapt dump badging` auf `de.kgv.oberrothenbach-Signed.apk` bestÃ¤tigt `application icon='res/mipmap-anydpi-v26/appicon.xml'`
  - APK-InhaltsprÃ¼fung bestÃ¤tigt explizite `appicon`-/`appicon_round`-Ressourcen sowie weiterhin `NO_AOT_LIBS`
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - echter GerÃ¤te-Login konnte in diesem Lauf nicht ausgefÃ¼hrt werden; dafÃ¼r ist jetzt die Dateilog-Ursache auf dem GerÃ¤t auslesbar

## 2026-03-28 â€“ Prompt 1/1: MAUI-Releasepfad fÃ¼r Login, Icon und signierte Artefakte auf den realen Stand gehÃ¤rtet

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/MainApplication.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - reale APK/AAB-Ausgaben, `aapt dump badging`, APK-Inhalt sowie vorhandener Keystore-Pfad unter `..\_secrets\Android\kgv-upload.keystore`
- Ehrlicher Befund:
  - Diagnose- und Icon-Fixes aus dem letzten Block waren bereits vorhanden
  - im realen `csproj` war der Releasepfad aber noch nicht vollstÃ¤ndig auf den bestÃ¤tigten Nicht-AOT-Stand festgezogen
  - die reale `MauiIcon`-Quelle zeigte weiterhin auf `appicon.png`, obwohl `.NET MAUI` App-Icons bevorzugt sauber aus der SVG-Quelle generiert werden
  - der aktuelle Signierpfad im `csproj` war auÃŸerdem inkonsistent: `AndroidSigningKeyStore` zeigte auf eine nicht vorhandene lokale Datei im Projektordner, und fÃ¼r den Signiervorgang fehlte real `AndroidSigningKeyPass`
  - `AuthService.LoginAsync(...)` war fÃ¼r sichere Fehlerdiagnose bereits ausreichend maskiert; dort war kein zusÃ¤tzlicher Eingriff nÃ¶tig
- Minimal umgesetzt:
  - Release explizit auf `RunAOTCompilation=false` und `AndroidEnableProfiledAot=false` festgezogen
  - `MauiIcon` auf die echte SVG-Quelle `Resources\AppIcon\appicon.svg` umgestellt
  - `AndroidSigningKeyStore` auf den real vorhandenen Workspace-Pfad `..\_secrets\Android\kgv-upload.keystore` korrigiert
  - `AndroidSigningKeyPass` aus dem bereits vorhandenen `AndroidSigningStorePass` abgeleitet, damit signierte Release-Builds wieder belastbar durchlaufen
- Validierung:
  - `dotnet clean KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=apk` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android -p:AndroidPackageFormat=aab` erfolgreich
  - `aapt dump badging` auf der signierten Release-APK zeigt weiterhin ein gesetztes App-Icon (`res/mipmap-anydpi-v26/appicon.xml`)
  - APK-InhaltsprÃ¼fung bestÃ¤tigt `NO_AOT_LIBS`
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - realer GerÃ¤tetest war in diesem Lauf nicht mÃ¶glich, weil kein aktives `adb`-GerÃ¤t verbunden war

## 2026-03-28 â€“ Prompt 1/1: MAUI-Release-Bug fÃ¼r Login/Launcher-Icon minimal gehÃ¤rtet

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/MainApplication.cs`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV.Maui/MainActivity.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - reale Release-Artefakte und APK-Inhalt per `aapt`/ZIP-PrÃ¼fung
- Ehrlicher Befund:
  - `appsettings.json` war im APK/AAB tatsÃ¤chlich vorhanden, Fehler beim Laden wurden in `MauiProgram` aber komplett still geschluckt
  - `AuthService.LoginAsync(...)` loggt bereits maskiert und ohne Passwort-/Token-Leak; im Release fehlte aber eine belastbare Start-/Fehlerdiagnose
  - der zentrale Unterschied zwischen Debug und Release war weiterhin der Linker: Debug lief mit `AndroidLinkMode=None`, Release nicht
  - AppIcon-Ressourcen wurden in Release zwar erzeugt und ins APK gepackt, das fertige APK referenzierte im Manifest aber kein Application-Icon (`aapt dump badging`: `application icon=''`)
- Minimal umgesetzt:
  - Release in `KGV.Maui.csproj` auf `AndroidLinkMode=None` gezogen, damit der funktionierende Debug-Pfad nicht im Release durch Linking/Trimming auseinanderlÃ¤uft
  - `MauiProgram` lÃ¤dt `appsettings.json` jetzt nicht mehr stillschweigend; Fehler werden in Logcat/Debug ausgegeben und fÃ¼hren zu einer klaren Startup-Exception
  - Supabase-Konfiguration wird direkt nach dem Laden validiert; fehlende `Url`/`PublishableKey` verschwinden nicht mehr erst als diffuser Login-Fehler
  - Debug-Logging fÃ¼r MAUI jetzt auch auÃŸerhalb von `#if DEBUG` aktiviert; zusÃ¤tzlich unhandled/unobserved Exceptions in Startup/Tasks nach Logcat gespiegelt
  - `MainApplication` setzt das Launcher-Icon explizit auf `@mipmap/appicon` und `@mipmap/appicon_round`
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Release` erfolgreich
  - `dotnet publish KGV.Maui/KGV.Maui.csproj -c Release -f net9.0-android` erfolgreich; signierte Release-APK erzeugt
  - `aapt dump badging` auf der signierten Release-APK zeigt jetzt `application icon='res/mipmap-anydpi-v26/appicon.xml'`
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - GerÃ¤teinstallation der neuen Release-APK war lokal nicht zerstÃ¶rungsfrei mÃ¶glich, weil auf dem angeschlossenen GerÃ¤t bereits eine inkompatibel signierte Variante installiert ist (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`)

## 2026-03-28 â€“ Prompt 1/1: Android-Version im ReleaseManager namespace- und SDK-style-robust aus `KGV.Maui.csproj` gelesen

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - Git-Status des aktuellen Repos
- Ehrlicher Befund:
  - `ApplicationDisplayVersion` war in `KGV.Maui.csproj` korrekt gesetzt
  - `VersionService` las Properties Ã¼ber `Elements("PropertyGroup")` + `ToDictionary(...)`
  - die eigentliche Fehlerursache lag nicht an fehlender Android-Version, sondern an mehrfach vorhandenen Property-Namen in den konditionalen SDK-Style-PropertyGroups (`AndroidPackageFormat`, `AndroidKeyStore`, `AndroidCreatePackagePerAbi`), wodurch die Dictionary-Erzeugung fehlschlug und die Versionslesung leer zurÃ¼ckfiel
- Minimal umgesetzt:
  - `GetPropertyValue(...)` in `KGV.ReleaseManager/Services/VersionService.cs` auf eine robuste LocalName-basierte Iteration Ã¼ber `PropertyGroup`-Kinder umgestellt
  - keine RÃ¼ckkehr zu `AssemblyInfo`, Manifest oder anderen Nebenpfaden
  - WPF- und Android-Version bleiben weiterhin ausschlieÃŸlich csproj-basiert
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich, nur bereits vorhandene Warnungen
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen
  - `dotnet build KGV.slnx -c Debug` erfolgreich

## 2026-03-28 â€“ Prompt 1/1: ReleaseManager auf csproj-Versionen und getrennte WPF-/Android-VerlÃ¤ufe umgestellt

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.Wpf/KGV.Wpf.csproj`
  - `KGV.Wpf/AssemblyInfo.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.ReleaseManager/Services/VersionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseVersionFileService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesAnalysisService.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesImportExportService.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
- Ehrlicher Befund:
  - Android-Version lag bereits sauber in `KGV.Maui.csproj` Ã¼ber `ApplicationDisplayVersion` und `ApplicationVersion`
  - WPF hatte in `KGV.Wpf.csproj` noch keine eigene Produktversion
  - der ReleaseManager las bzw. schrieb Versionen noch Ã¼ber alte Nebenpfade mit Assembly-/Manifest-Fallbacks und behandelte WPF faktisch als Master
  - die lokale Release-Historie war bisher nur gemeinsam gespeichert, nicht produktgetrennt auswÃ¤hlbar
- Minimal umgesetzt:
  - `KGV.Wpf.csproj` um eine direkte Produktversion Ã¼ber `<Version>0.2.6</Version>` ergÃ¤nzt
  - `VersionService` liest WPF und Android jetzt ausschlieÃŸlich direkt aus den beiden `csproj`-Dateien
  - Fallbacks auf `AssemblyInfo.cs`, Android-Manifest oder sonstige Nebenpfade entfernt
  - Zielversion wird jetzt erst aus der konkreten Release-Auswahl abgeleitet; bei gemeinsamem Release mit Drift aus dem hÃ¶heren Stand, nicht mehr blind aus WPF
  - `ReleaseVersionFileService` schreibt nur noch in die ausgewÃ¤hlten `csproj`-Dateien und aktualisiert bei Einzelrelease nicht mehr automatisch beide Produkte
  - Release-Notiz-/Versionsverlauf lokal auf getrennte WPF-/Android-Historien unter `%LocalAppData%\KGV.ReleaseManager\release-notes-history-wpf.json` und `...-android.json` aufgeteilt
  - UI zeigt aktuelle WPF- und Android-Version getrennt sowie getrennte Historienpfade/-stÃ¤nde an
  - Import von Release-Zusammenfassungen erlaubt jetzt WPF-only, Android-only oder beide zusammen passend zur Zielauswahl
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Debug` erfolgreich, nur bereits vorhandene Warnungen in `SupabaseService.cs`
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich, nur bereits bekannte Warnungen (`XA1037`, Nullability in `HomeManagementPage.cs`)
  - `dotnet build KGV.slnx -c Debug` erfolgreich

## 2026-03-28 â€“ Inbetriebnahme Block 2/3: Android-Signing-Pfad im Release Manager mit LaufzeitpasswÃ¶rtern gehÃ¤rtet

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.ReleaseManager/Services/BuildCommandService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/RuntimeSecretPromptService.cs`
  - `KGV.ReleaseManager/Models/ReleaseManagerSettings.cs`
  - `KGV.Maui/KGV.Maui.csproj`
- Ehrlicher Befund:
  - Keystore-Pfad, Alias und Package Name waren bereits vorhanden
  - LaufzeitpasswÃ¶rter wurden nicht gespeichert, liefen aber noch als Klartext in der Android-Build-Commandline
  - der Komfortfall `Key-Passwort = Keystore-Passwort` war noch nicht explizit im Dialog sichtbar
- Minimal umgesetzt:
  - Signierungsdialog mit expliziter Option `Key-Passwort = Keystore-Passwort`
  - Android-PasswÃ¶rter laufen jetzt Ã¼ber temporÃ¤re Prozess-Umgebungsvariablen statt Klartext-Commandline
  - Android-Buildpfad Ã¼bernimmt jetzt den konfigurierten Package Name als `ApplicationId`
  - Android-Fehlertexte schwÃ¤rzen LaufzeitpasswÃ¶rter
  - LaufzeitpasswÃ¶rter werden nach dem Lauf aus dem Requestobjekt geleert
- Validierung:
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - servicebasierter Test bestÃ¤tigt: keine KlartextpasswÃ¶rter in der Commandline, keine Passwortspeicherung in Settings, verstÃ¤ndliche Meldungen fÃ¼r fehlenden Keystore/Alias

## 2026-03-28 â€“ Inbetriebnahme Block 1/3: reales Inno-Setup-Skript fÃ¼r den WPF-Releasepfad ergÃ¤nzt

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - keine vorhandene `*.iss`-Datei im Quellrepo `C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand`
  - reale WPF-Projektdatei `KGV.Wpf/KGV.Wpf.csproj`
  - reale Zielrepo-Struktur in `C:\Programmieren\Restore KGV\KGV-WPF` mit `KGV-Setup.exe`, `KGV-Setup-0.2.4.exe`, `KGV-Setup-0.2.5.exe`, `KGV-Setup-0.2.6.exe`, `releases.json`, `version.json`
- Ehrlicher Befund:
  - der Release Manager konnte `ISCC.exe` bereits aufrufen, aber im Quellrepo fehlte noch ein belastbares Inno-Setup-Skript
  - die reale Namenslogik fÃ¼r WPF-Setups war im lokalen Zielrepo bereits klar erkennbar
- Minimal umgesetzt:
  - neues reales Skript `KGV.Wpf/Installer/KGV.Wpf.iss`
  - basiert auf der WPF-Releaseausgabe `KGV.Wpf\bin\Release\net8.0-windows`
  - Haupt-EXE `KGV.Wpf.exe`
  - minimale ReleaseManager-ErgÃ¤nzung: Zielversion wird beim Inno-Aufruf als `AppVersion`-Define Ã¼bergeben
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj -c Release` erfolgreich
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.slnx -c Debug` erfolgreich
  - `ISCC.exe`-Compile mit dem neuen Skript erfolgreich

## 2026-03-28 â€“ Prompt 5/5: Release Manager um Log-Auswertung, Exporttext und versionierten Import ergÃ¤nzt

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/Services/ReleaseNotesImportExportService.cs`
  - `KGV.ReleaseManager/Services/LogExtractionService.cs`
  - reale Loglage im Quellrepo: `KGV_Fortschrittslog_ausfuehrlich.md`, `DEV_LOG.md`
- Ehrlicher Befund:
  - Versionslogik, Logquelle und Release-Ablauf-GrundgerÃ¼st waren bereits vorhanden
  - es fehlten aber noch ein belastbarer Release-Anker, ein kopierfertiger Exporttext und eine lokale versionierte Speicherung importierter WPF-/Android-Release-Texte
  - zusÃ¤tzliche produktbezogene Release-History-Dateien wurden im Quellrepo nicht gefunden
- Minimal ergÃ¤nzt:
  - lokale Release-Notiz-Historie als JSON unter `%LocalAppData%\KGV.ReleaseManager\release-notes-history.json`
  - Auswertung relevanter Logabschnitte seit dem letzten gespeicherten Release-Anker
  - Filter auf ReleaseManager-interne Ã„nderungen
  - kopierfertiger Exporttext mit ChatGPT-Prompt fÃ¼r `WPF / Download` und `Android / Play Store`
  - ImportprÃ¼fung und lokale versionierte Speicherung der erzeugten Zusammenfassung
- Validierung:
  - `dotnet restore KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.slnx -c Debug` erfolgreich

## 2026-03-28 â€“ Prompt 4/5: Release Manager echte Release-AusfÃ¼hrung weiter auf reale Pfade und Zielablagen gezogen

- Vor Beginn den realen Istzustand geprÃ¼ft:
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/Services/SettingsService.cs`
  - `KGV.ReleaseManager/Services/ReleaseExecutionService.cs`
  - `KGV.ReleaseManager/Services/ReleaseArtifactService.cs`
  - reale Pfade `C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand`, `C:\Programmieren\Restore KGV\KGV-WPF`, `C:\Programmieren\Restore KGV\Releases\KGV`, `...\Android\APK`, `...\Android\AAB`
  - reale Build-/Versionsstellen in `KGV.Wpf` und `KGV.Maui` nur lesend
- Ehrlicher Befund:
  - die Release-AusfÃ¼hrung war im `KGV.ReleaseManager` bereits verdrahtet, vorbelegte bestÃ¤tigte Pfade fehlten aber noch
  - `ISCC.exe` ist lokal weiterhin nicht gefunden
  - im Quellrepo ist weiterhin kein `.iss`-Skript vorhanden
  - das lokale Repo `KGV-WPF` enthÃ¤lt die Release-Dateien klar im Repo-Root (`KGV-Setup.exe`, `KGV-Setup-0.2.x.exe`, `releases.json`)
- Minimal ergÃ¤nzt:
  - bestÃ¤tigte Pfade werden jetzt als Default-Settings vorbelegt, ohne einen Fake-Pfad fÃ¼r `ISCC.exe` zu setzen
  - nach BenutzerbestÃ¤tigung des real vorhandenen Inno-Setup-Compilers wird jetzt auch `C:\Users\Braen\AppData\Local\Programs\Inno Setup 6\ISCC.exe` automatisch als Default gesetzt, solange das Setting leer ist
  - Release-Validierung erlaubt das spÃ¤tere Anlegen des Release-Roots statt ihn schon beim Speichern zu verlangen
  - WPF-Setups werden bei Erfolg zusÃ¤tzlich in das lokale `KGV-WPF`-Repo kopiert; vorhandenes `KGV-Setup.exe` kann dabei als aktuelle Setup-Datei aktualisiert werden
  - APK und AAB werden bei Erfolg zusÃ¤tzlich in die konfigurierten Ausgabeordner kopiert
- Validierung:
  - `dotnet restore KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj -c Debug` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj -c Debug` erfolgreich
  - `dotnet build KGV.slnx -c Debug` erfolgreich

## 2026-03-28 â€“ Block 1: Loginfenster/OTP-Flow in WPF und MAUI auf den sichtbaren Stand zurÃ¼ckgezogen

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.Wpf/Views/LoginWindow.xaml`
  - `KGV.Wpf/Views/LoginWindow.xaml.cs`
  - `KGV.Wpf/ViewModels/LoginViewModel.cs`
  - `KGV.Wpf/Views/ResetPasswordWindow.xaml`
  - `KGV.Wpf/ViewModels/ResetPasswordViewModel.cs`
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Core/Interfaces/IAuthService.cs`
  - `KGV.Infrastructure/Authentication/AuthService.cs`
  - Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad geprÃ¼ft
- Ehrlicher Befund auf dem aktuellen Stand:
  - der OTP-/Erstlogin-Unterbau Ã¼ber `IAuthService` und `AuthService` war bereits vorhanden und musste nicht neu erfunden werden
  - WPF zeigte den sichtbaren Login-Stand aber nicht mehr sauber: `Anmelden` stand unter den SekundÃ¤raktionen, die Passwortregeln waren nur statisch und der RÃ¼ckweg aus dem OTP-Flow fehlte sichtbar
  - MAUI nutzte denselben Flow bereits direkt auf der Login-Seite, zeigte die PasswortprÃ¼fung aber noch zu grob und fÃ¼hrte `Passwort vergessen` nicht direkt in denselben sichtbaren Code-/Neupasswortpfad zurÃ¼ck
- Minimal umgesetzt:
  - `LoginViewModel` um sichtbare Passwortregeln, vollstÃ¤ndige PasswortprÃ¼fung und einen kleinen `ZurÃ¼ck zum Login`-Pfad ergÃ¤nzt
  - `LoginWindow` auf den sichtbaren Zielstand gezogen: `E-Mail + Passwort`, Passwort-Sichtbarkeit, `Anmelden`, OTP-Code anfordern, Code prÃ¼fen, direktes neues Passwort setzen, Passwortbedingungen sichtbar, Passwort-vergessen weiter vorhanden
  - `LoginWindow.xaml.cs` synchronisiert jetzt die Passwortfelder sauber mit dem ViewModel, damit der OTP-/Neupasswort-Flow sichtbar zurÃ¼ckgesetzt wird
  - `LoginPage` in MAUI auf denselben sichtbaren Flow gezogen; `Passwort vergessen` fÃ¼hrt dort jetzt direkt in denselben OTP-/Neupasswort-Bereich zurÃ¼ck
- Validierung:
  - `dotnet build KGV.Core/KGV.Core.csproj` erfolgreich
  - `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj` erfolgreich, nur bereits bekannte Nullability-Warnungen in `SupabaseService.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich, nur bereits bekannte Warnungen (`XA1037`, Nullability in `HomeManagementPage.cs`)

## 2026-03-28 â€“ Prompt 1/1: Startseiten-Sichtbarkeit und Editor-Defaults fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen vereinheitlicht

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - `KGV.Maui/Pages/BekanntmachungEditorPage.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Wpf/ViewModels/ArbeitseinsaetzeVerwaltungViewModel.cs`
  - `KGV.Wpf/ViewModels/TermineVerwaltungViewModel.cs`
  - `KGV.Wpf/ViewModels/BekanntmachungenVerwaltungViewModel.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad geprÃ¼ft
- Ehrlicher Befund auf dem aktuellen Stand:
  - die Startseite lud die drei Bereiche bereits Ã¼ber gemeinsame Home-Pfade, filterte die Sichtbarkeit aber nicht explizit auf eine gemeinsame `aktiv`-/`sichtbar_von`-/`sichtbar_bis`-Regel gegen die Basistabellen
  - in MAUI enthielten Arbeitseinsatz- und Termin-Editor noch Checkboxen fÃ¼r Zeit-/Sichtbarkeitsfelder
  - WPF hatte keine solchen Checkboxen mehr, startete neue DatensÃ¤tze aber noch teils mit leeren Zeit-/Sichtbarkeitsfeldern
- Minimal umgesetzt:
  - Startseiten-Sichtbarkeit fÃ¼r ArbeitseinsÃ¤tze, Termine und Bekanntmachungen zentral auf dieselbe Regel gezogen: `aktiv = true`, `sichtbar_ab` leer oder <= jetzt, `sichtbar_bis` leer oder >= jetzt
  - dabei werden die Home-View-Daten mit den Basistabellen abgeglichen, statt neue Schattenlogik zu erÃ¶ffnen
  - MAUI-Editoren fÃ¼r ArbeitseinsÃ¤tze und Termine von den Checkboxen `Startzeit verwenden`, `Endzeit verwenden`, `Sichtbar ab verwenden`, `Sichtbar bis verwenden` bereinigt
  - neue DatensÃ¤tze erhalten jetzt in WPF und MAUI sinnvolle Standardwerte fÃ¼r Datum/Zeit/Sichtbarkeit
  - Create-Pfade im gemeinsamen `SupabaseService` sichern zusÃ¤tzlich ab, dass bei Neuanlage keine fachlich leeren Zeit-/Sichtbarkeitswerte entstehen
  - der MAUI-Arbeitseinsatz-Editor kehrt nach Speichern jetzt wie WPF wieder auf die Startseite zurÃ¼ck

## 2026-03-28 â€“ Prompt 2/5: Release Manager Settings-GrundgerÃ¼st verdrahtet

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.ReleaseManager/Models/ReleaseManagerSettings.cs`
  - `KGV.ReleaseManager/ViewModels/MainViewModel.cs`
  - `KGV.ReleaseManager/Services/SettingsService.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/CHANGELOG.md`
  - `KGV.ReleaseManager/KGV_Fortschritt_ausfuehrlich.md`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad geprÃ¼ft
- Ehrlicher Befund auf dem aktuellen Stand:
  - ein Settings-Modell, ein `SettingsService` und die Grund-UI im `MainWindow` waren bereits als Scaffold vorhanden
  - Laden/Speichern war aber noch nicht robust verdrahtet
  - die Einstellungen wurden beim Start noch nicht automatisch geladen
  - Pflichtfeld-/Pfadvalidierung und vorbereitete Store-Felder fehlten noch
- Minimal umgesetzt:
  - `ReleaseManagerSettings` um `StoreUrl` ergÃ¤nzt und mit zentraler Normalisierung versehen
  - `SettingsService` auf robuste JSON-Persistenz mit verstÃ¤ndlichen RÃ¼ckmeldungen bei fehlender oder beschÃ¤digter Datei umgestellt
  - `MainViewModel` validiert jetzt Pflichtfelder, lokale Hauptpfade sowie optionale Package-/Track-/URL-Felder grundlegend
  - `MainWindow` in die Bereiche `Projektpfade`, `Android / Play Store` und `VerÃ¶ffentlichung` gegliedert
  - gespeicherte Einstellungen werden jetzt beim Start automatisch geladen
  - klarer Speichern-Button am Ende des Settings-Formulars platziert
- Abgrenzung:
  - keine echte Release-Automation verdrahtet
  - keine Git-, Build-, Inno- oder Android-Signing-Logik fachlich umgesetzt
  - ReleaseManager-Ã„nderungen nicht in Endnutzer-Release-Notes des KGV-Produkts Ã¼bernommen

## 2026-03-28 â€“ Prompt 1/1: Release Manager BuildfÃ¤higkeit geprÃ¼ft, keine Buildfixes nÃ¶tig

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - Solution-Projekte Ã¼ber den geÃ¶ffneten Solution-Stand geprÃ¼ft
  - `KGV.ReleaseManager/KGV.ReleaseManager.csproj`
  - `KGV.ReleaseManager/App.xaml`
  - `KGV.ReleaseManager/App.xaml.cs`
  - `KGV.ReleaseManager/MainWindow.xaml`
  - `KGV.ReleaseManager/MainWindow.xaml.cs`
  - `KGV.ReleaseManager/KGV_Fortschritt_ausfuehrlich.md`
  - `KGV.ReleaseManager/CHANGELOG.md`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad geprÃ¼ft
- Ehrlicher Befund auf dem aktuellen Stand:
  - `KGV.ReleaseManager` ist im aktuellen Solution-Stand eingebunden
  - das Projekt ist als WPF-Projekt auf `net8.0-windows` mit `UseWPF=true` konsistent aufgebaut
  - `App.xaml`, `MainWindow` und Startup-Verkabelung sind buildseitig konsistent
  - beim PrÃ¼flauf zeigten sich keine fehlenden Usings, keine Namespace-/Dateikonflikte, keine XAML-Buildfehler und keine fehlerhaften Projektverweise
  - es waren keine minimalen Codefixes am Release-Manager nÃ¶tig
- Validierung:
  - `dotnet restore KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
  - `dotnet build KGV.ReleaseManager/KGV.ReleaseManager.csproj` erfolgreich
  - `dotnet build KGV.slnx` erfolgreich
- Dokumentation:
  - nur interne Entwicklungslogs ergÃ¤nzt
  - keine ReleaseManager-EintrÃ¤ge in Endnutzer-Release-Notes des KGV-Produkts Ã¼bernommen

## 2026-03-28 â€“ Prompt 1/1: MAUI-Home-Arbeitsstunden vom ausgewÃ¤hlten Mitglied entkoppelt

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
  - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
  - `KGV.Maui/State/MemberContextState.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund auf dem aktuellen Stand:
  - der Home-Arbeitsstundenbereich hing fÃ¤lschlich am `SelectedMember`
  - dadurch konnten Button-Sichtbarkeit und Pflichtstunden-Zusammenfassung auf Home auf ein fremdes Mitglied umbiegen
  - auch der Home-Button `Arbeitsstunde erfassen` Ã¶ffnete fÃ¼r Admin/Vorstand implizit wieder den ausgewÃ¤hlten Mitgliedskontext
- Minimal umgesetzt, ohne die Ã¼brigen Mitgliedskontextpfade zu zerstÃ¶ren:
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
    - Home-Arbeitsstunden lÃ¶sen den Mitgliedskontext jetzt immer aus `CurrentMitgliedId` des eingeloggten Nutzers
    - `CanCreateOwnWorkHoursEntry` ergÃ¤nzt und vom `SelectedMember` entkoppelt
    - die Arbeitsstunden-Zusammenfassung auf Home nutzt damit nicht mehr den ausgewÃ¤hlten Mitgliedskontext
  - `KGV.Maui/Pages/HomePage.xaml.cs`
    - der Home-Button `Arbeitsstunde erfassen` bindet jetzt an den eigenen Nutzerkontext
    - Ã–ffnen des Editors erfolgt explizit mit `context=self`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
    - neuer expliziter Eigenkontext fÃ¼r den Home-Einstieg ergÃ¤nzt
    - der Editor respektiert `context=self` und Ã¼bernimmt dann nicht mehr still den aktuellen `SelectedMember`
- Fachliches Ergebnis:
  - Home-Arbeitsstunden bleiben jetzt immer im Kontext des eingeloggten Nutzers
  - der Button `Arbeitsstunde erfassen` ist nicht mehr davon abhÃ¤ngig, ob vorher ein anderes Mitglied ausgewÃ¤hlt wurde
  - die mitgliedsbezogenen Arbeitsstundenpfade fÃ¼r andere Mitglieder bleiben separat erhalten
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-28 â€“ Prompt 1/1: MAUI-RFID-Ausstattung jetzt direkt bearbeitbar und Medium-Picker wird nach Save synchronisiert

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
  - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
  - `KGV.Core/Models/ParzelleRecord.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund auf dem aktuellen Stand:
  - `hat Strom` und `hat Wasser` wurden in `RFID einrichten` bereits angezeigt, aber nur read-only
  - fÃ¼r Ã„nderungen musste bisher in einen anderen Stammdatenpfad gewechselt werden
  - der vorhandene Persistenzpfad `UpdateParzelleStammdatenAsync(ParzelleRecord)` war bereits vorhanden und wiederverwendbar
- Minimal umgesetzt, ohne neuen Spezial-Service:
  - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
    - editierbare Properties `EditHatStrom` und `EditHatWasser` ergÃ¤nzt
    - `HasAusstattungChanges`, `CanEditAusstattung` und `CanSaveAusstattung` ergÃ¤nzt
    - Speichern der Ausstattung Ã¼ber den bestehenden Pfad `UpdateParzelleStammdatenAsync(...)` ergÃ¤nzt
    - nach erfolgreichem Speichern lokale Parzellenwerte aktualisiert und `MediumOptions` direkt neu geladen
    - inkonsistente Medium-Auswahl wird Ã¼ber den bestehenden Reload-Pfad sauber bereinigt
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
    - Switches auf TwoWay-Binding umgestellt
    - neuen Button `Ausstattung speichern` ergÃ¤nzt
    - bestehende RFID-Anzeigen, Medium-Picker sowie PrÃ¼f-/Speicherpfad fÃ¼r RFID beibehalten
- Fachliches Ergebnis:
  - Admin/Vorstand kÃ¶nnen `hat Strom` und `hat Wasser` jetzt direkt im RFID-Pfad Ã¤ndern und speichern
  - vorhandene Parzellen-Stammdaten werden dafÃ¼r wiederverwendet; keine neue Schattenlogik
  - der Medium-Picker synchronisiert sich nach dem Speichern sofort mit dem neuen Ausstattungszustand
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-28 â€“ Prompt 1/1: MAUI-Parzellen-Bearbeiten sichtbar getrennt und RFID-Ausstattung ergÃ¤nzt

- Zuerst den realen Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
  - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
  - `KGV.Core/Models/ParzelleRecord.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Parzellenverwaltung minimal und sauber korrigiert:
  - normale Stammdatenanzeige wird im Bearbeiten-Modus jetzt ausgeblendet
  - neue ViewModel-Property `ShowReadOnlyStammdaten` steuert die Sichtbarkeit robust statt UI-Hack
  - Bearbeiten-/Save-/Abbrechen-/Detail-Ladelogik bleibt unverÃ¤ndert
  - andere Sektionen wie Belegung und Navigation wurden bewusst nicht mit ausgeblendet
- `RFID einrichten` ergÃ¤nzt:
  - nach Parzellenauswahl werden jetzt zusÃ¤tzlich `hat Strom` und `hat Wasser` angezeigt
  - Darstellung ist read-only Ã¼ber deaktivierte Switches
  - Werte kommen direkt aus dem bestehenden `SelectedParzelle`-/`ParzelleRecord`-Pfad
  - bestehende Anzeigen fÃ¼r aktuelle Strom-/Wasser-RFID und der Medium-Flow bleiben erhalten
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-28 â€“ Prompt 1/1: MAUI-Parzellenverwaltung um Listenansicht und Suche ergÃ¤nzt

- Zuerst den echten Istzustand im aktuellen Repo geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
  - `KGV.Core/Models/ParzelleVerwaltungItem.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Reale Ursache, warum die Liste bisher fehlte:
  - `ParzellenPage` sprang direkt in den bestehenden Detailpfad
  - im `ParzellenViewModel` gab es nur die interne Sammlung `Items`, aber keine such-/listenfÃ¤hige, an die UI gebundene Ãœbersicht
  - dadurch existierte keine echte auswÃ¤hlbare Parzellenliste und keine Suche oberhalb der Detailansicht
- Minimal ergÃ¤nzt, ohne neuen Fachumfang:
  - `KGV.Core/Models/ParzelleVerwaltungItem.cs`
    - listenbezogene Anzeige fÃ¼r PÃ¤chter als `Name, Vorname`
    - SortKey- und Search-Text fÃ¼r stabile Sortierung und Filterung ergÃ¤nzt
  - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
    - `FilteredItems` und `SearchText` ergÃ¤nzt
    - Filterung nach `Garten Nr` und PÃ¤chtername ergÃ¤nzt
    - stabile Sortierung nach vorhandenem `GartenNrSortKey` gezogen
    - bestehende Detailladung bleibt weiter an `SelectedItem` und `GetParzelleDetailAsync(...)` gebunden
  - `KGV.Maui/Pages/ParzellenPage.cs`
    - oberhalb der bestehenden Detailsektionen eine suchbare `CollectionView` ergÃ¤nzt
    - Liste zeigt je Eintrag nur `Garten Nr` und den aktuellen PÃ¤chter
    - freie Parzellen werden neutral als `Nicht verpachtet` angezeigt
    - Detailsektionen darunter bleiben auf dem bestehenden Pfad erhalten
- Ergebnis:
  - Suche filtert nach `Garten Nr` und PÃ¤chtername
  - Antippen eines Listeneintrags setzt `SelectedItem`
  - die vorhandenen Stammdaten-/Belegungsdetails werden darunter weiter Ã¼ber den bestehenden Pfad geladen
  - der Mitglieds-/Kontextpfad bleibt erhalten, weil weiterhin dieselbe `SelectedItem`- und Context-Logik verwendet wird
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - im MAUI-Build bleiben nur die bereits bekannten Warnungen (`XA1037`, Nullability in `KGV.Maui/Pages/HomeManagementPage.cs`)

## 2026-03-27 â€“ Prompt 3/3 Abschluss: MAUI-Parzellenverwaltung und RFID-Einrichten gegen den realen Blockstand validiert, Restfehler bereinigt und Abschluss vorbereitet

- Zuerst den echten Istzustand des begonnenen Parzellen-/RFID-Blocks geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
  - die real geÃ¤nderten Blockdateien im Arbeitsbaum:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Core/Models/ParzelleDetailDTO.cs`
    - `KGV.Core/Models/ParzelleRecord.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Maui/Pages/ParzellenPage.cs`
    - `KGV.Maui/ViewModels/ParzellenViewModel.cs`
    - `KGV.Maui/ViewModels/RfidEinrichtenViewModel.cs`
- Real bestÃ¤tigt auf dem aktuellen Zwischenstand:
  - die MAUI-Parzellenseite ist fachlich auf reduzierte Stammdaten + Belegung gezogen
  - sichtbar bleiben in `Stammdaten` genau `ID`, `Garten Nr`, `FlÃ¤che`, `hat Wasser`, `hat Strom`, `rfid Wasser`, `rfid Strom`, `Anlage`
  - `Belegung` zeigt bei freier Parzelle den Button `Zuweisen`
  - bei belegter Parzelle wird `Name, Vorname` als Button/Link zur Mitgliedsstammdatenansicht verwendet
  - die FlÃ¤chen-Schutzabfrage ist real in `ParzellenPage.SaveStammdatenAsync()` vorhanden und fragt Ã„nderungen an `FlÃ¤che` vor dem Speichern ausdrÃ¼cklich ab
- Reale Ursache der leeren Medium-Liste in `RFID einrichten`:
  - die Medium-Auswahl wurde im begonnenen Block im MAUI-ViewModel zuvor nur UI-seitig aus `HatStrom` / `HatWasser` aufgebaut
  - zusÃ¤tzlich fehlte im Shared-Service die dazu passende zentrale fachliche AuswahlprÃ¼fung
  - Parzellen mit inkonsistentem oder nicht belastbarem Ausstattungszustand konnten dadurch in der Mediumsauswahl leer laufen
- Minimal bereinigt, ohne neuen Fachumfang:
  - `ISupabaseService` um den gemeinsamen Abfragepfad fÃ¼r verfÃ¼gbare RFID-Medien ergÃ¤nzt
  - `SupabaseService` liefert die RFID-Medien jetzt zentral aus derselben fachlichen Wahrheit:
    - vorhandene Ausstattung (`hat_strom` / `hat_wasser`)
    - bereits hinterlegte RFID am Medium
    - aktiver ZÃ¤hler am Medium
    - Fallback auf beide Medien, wenn der Ausstattungszustand nicht belastbar ist
  - dieselbe gemeinsame Service-Logik wird in `CheckParzelleRfidAssignmentAsync(...)` verwendet; die PrÃ¼fung verlÃ¤sst sich damit nicht mehr nur auf UI-Filterung
  - `RfidEinrichtenViewModel` lÃ¤dt die Medium-Auswahl asynchron aus dem gemeinsamen Service statt lokaler `Hat...`-Sonderlogik
  - `ParzellenPage` compile-bedingt auf den real reduzierten MAUI-C#-Pfad bereinigt, damit die Abschlussseite sauber baut
- Validierung:
  - dateibezogene Fehler auf den direkt betroffenen Blockdateien nach den Korrekturen unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - im grÃ¼nen MAUI-Build bleiben nur bekannte Warnungen, insbesondere `XA1037` und vorhandene Nullability-Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` war fÃ¼r diesen MAUI-Abschlussblock nicht zwingend und wurde nicht zusÃ¤tzlich gestartet
- Blockstatus:
  - der begonnene MAUI-Parzellen-/RFID-Block ist fachlich und technisch abgeschlossen; offen blieb vor dem Git-Abschluss nur noch die organisatorische Commit-/Push-Abwicklung

## 2026-03-27 â€“ Prompt 2/3 Abschluss: WartungsvertrÃ¤ge-Nebenmitglieder-Block final validiert, Migration abgeglichen und sauber abgeschlossen

- Echten Abschluss-Istzustand des begonnenen Blocks erneut geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - reale geÃ¤nderte Wartungsvertragsdateien im Arbeitsbaum
  - neue Migration `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Im Arbeitsbaum real vorhanden und abschlieÃŸend bestÃ¤tigt:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/WartungsvertragDetailItem.cs`
  - `KGV.Wpf/ViewModels/WartungsvertragMitgliederZuordnungViewModel.cs`
  - `KGV.Wpf/Views/WartungsvertragDetailView.xaml`
  - `KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs`
  - `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- Fachkonsistenz des Blocks bestÃ¤tigt:
  - WPF und MAUI nutzen weiterhin denselben Shared-/Infrastructure-Pfad Ã¼ber `ISupabaseService`
  - Nebenmitglieder bleiben in Anzeige, Detail und Zuweisung erhalten und werden nicht mehr still auf das Hauptmitglied umgebogen
  - Pflichtstunden-/Wartungsvertragsbezug ist mitgliedsbezogen angezogen; die Migration bildet dieselbe fachliche Wahrheit wie der App-Code ab
- Direkter Restfehler dieses Blocks minimal bereinigt:
  - `KGV.Wpf/Views/WartungsvertragDetailView.xaml` zeigt den Mitgliedskontext jetzt auch real im Grid an; damit entspricht die WPF-Detailansicht dem begonnenen Blockstand und der MAUI-Detaildarstellung
- Abschlussvalidierung:
  - dateibezogene PrÃ¼fung auf den Blockdateien unauffÃ¤llig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - bekannte Warnungen bleiben unverÃ¤ndert bestehen, u. a. `XA1037` und Nullability-Warnungen in `KGV.Maui/Pages/HomeManagementPage.cs`
- Blockstatus:
  - der WartungsvertrÃ¤ge-Nebenmitglieder-Block ist damit technisch und organisatorisch abgeschlossen

## 2026-03-27 â€“ Prompt 2/3: WartungsvertrÃ¤ge fÃ¼r Haupt- und Nebenmitglieder in WPF und MAUI fachlich geradegezogen

- Zuerst den echten Istzustand auf Basis des gepushten Stands `4857f64` geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
  - betroffene Wartungsvertrags-Pfade in `KGV.Core`, `KGV.Infrastructure`, WPF und MAUI
- Zu eng auf Hauptmitglieder laufende Fachlogik gefunden in:
  - `SupabaseService`: `GetWartungsvertraegeForMitgliedAsync`, `GetAssignableWartungsvertraegeForMitgliedAsync`, `AssignMitgliederToWartungsvertragAsync` und `AssignWartungsvertraegeToMitgliedAsync` normalisierten Nebenmitglieder auf `HauptmitgliedId`
  - WPF- und MAUI-Zuweisungslisten filterten Nebenmitglieder zusÃ¤tzlich komplett aus den auswÃ¤hlbaren Mitgliedern heraus
  - die Datenbankmigration enthielt in `validate_wartungsvertrag_zuordnung()` noch die harte Regel "nur Hauptmitglieder zulassen"
  - der Pflichtstunden-/Wartungsvertragsbezug lief in der Migration und im Shared-Service weiter zu stark Ã¼ber Hauptmitglied-Pfade
- Bereinigung dieses Blocks:
  - Shared-/Infrastructure-Pfade auf direkte Mitgliedszuordnung fÃ¼r WartungsvertrÃ¤ge umgestellt; Nebenmitglieder werden nicht mehr auf Hauptmitglieder umgebogen
  - WPF- und MAUI-Zuweisungslisten schlieÃŸen aktive Nebenmitglieder nicht mehr kÃ¼nstlich aus
  - WPF-Detailansicht zeigt den Mitgliedskontext jetzt zusÃ¤tzlich explizit an; MAUI-Detail nutzt denselben gemeinsamen Kontexttext im Untertitel
  - neue Migration `supabase/migrations/20260327160000_wartungsvertraege_nebenmitglieder.sql` ergÃ¤nzt:
    - Validierung erlaubt Wartungsvertragszuordnungen auch fÃ¼r Nebenmitglieder
    - Pflichtstunden-/Wartungsvertragsbezug liefert wieder mitgliedsbezogene Zeilen statt still nur Hauptmitgliedern
- Ergebnis:
  - Anzeige, Detail und Zuweisung verwenden jetzt dieselbe fachliche Wahrheit fÃ¼r Haupt- und Nebenmitglieder
  - bestehende WPF-/MAUI-Pfade wurden weiterverwendet; keine neue Schattenlogik, keine neue Seite
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - offen bleiben nur die bekannten Warnungen, u. a. `XA1037` sowie Nullability-Warnungen in `HomeManagementPage.cs`

## 2026-03-27 â€“ Prompt 1/3: MAUI-Startseite, Stammdaten und Navigationstext minimal bereinigt

- Zuerst den echten Istzustand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
  - die aktiven MAUI-Pfade `HomePage`, `HomeViewModel`, `MemberContextState`, `MeineDatenPage`, `AdminShell`, `UserShell`, `MemberSearchPage`
- Home/Startseite korrigiert:
  - `MemberContextState` um eine Ã„nderungsbenachrichtigung ergÃ¤nzt
  - `HomePage` reagiert jetzt auf Kontextwechsel, invalidiert den ViewModel-Zustand und lÃ¤dt beim Anzeigen sofort neu
  - dadurch wird Home beim ersten Ã–ffnen bzw. nach Mitgliedswechsel mit dem aktuellen Kontext geladen statt mit veraltetem Cache
  - der Button `Arbeitsstunde erfassen` hÃ¤ngt weiter am bestehenden Produktivpfad, wird aber jetzt mit dem aktiven Kontext sauber neu bewertet
  - `HomeViewModel.UserContextText` zeigt den echten Home-Kontext jetzt explizit an: ausgewÃ¤hltes Mitglied fÃ¼r Admin/Vorstand, sonst eigenes Mitglied
- Stammdaten entschlackt:
  - auf `MeineDatenPage` die Bereiche `Mitgliedschaft`, `WartungsvertrÃ¤ge / Pflichtstunden` und `Verwaltung` aus der sichtbaren Seite entfernt
  - `Mitgliedskontext` wird nur noch angezeigt, wenn wirklich eine Beziehung existiert
  - sichtbar ist dort jetzt fachlich klar `Hauptmitglied` mit verknÃ¼pftem Nebenmitglied oder `Nebenmitglied` mit zugeordnetem Hauptmitglied
  - ohne echte VerknÃ¼pfung bleibt der Bereich vollstÃ¤ndig ausgeblendet
- Navigationstext korrigiert:
  - im Admin-Flyout `Arbeitsstunden Â· Arbeitsstunden freigeben` auf `Arbeitsstunden freigeben` reduziert
  - dieselbe KÃ¼rzung gilt auch fÃ¼r den ZÃ¤hlertext mit offenen Freigaben
- Validierung:
  - dateibezogene Fehler auf den geÃ¤nderten MAUI-Dateien blieben unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief erfolgreich durch
  - offen bleiben nur bekannte Warnungen, unter anderem `XA1037` und Nullability-Warnungen in `HomeManagementPage.cs`

## 2026-03-27 â€“ Prompt 1/1: bekannte MAUI-Altfehler gezielt bereinigt und Gesamtbuild wieder grÃ¼n gezogen

- Zuerst den echten Istzustand auf Basis des gepushten Stands `85a7859` geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
  - echten Build `dotnet build KGV.Maui/KGV.Maui.csproj`
- Reales Fehlerbild laut aktuellem MAUI-Build:
  - `KGV.Maui/App.xaml.cs`: `InitializeComponent` nicht vorhanden
  - `KGV.Maui/Pages/AblesungErfassenPage.cs`: `IPlatformApplication` nicht vorhanden
  - `KGV.Maui/Pages/ZaehlerwechselPage.cs`: `IPlatformApplication` nicht vorhanden
  - `KGV.Maui/Settings/AppSettings.cs`: `FileSystem` nicht vorhanden
  - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`: `InitializeComponent` nicht vorhanden
- Minimal bereinigt, ohne neuen Fachumfang:
  - `App.xaml.cs`: leeren XAML-Initialisierungspfad entfernt; die App erzeugt ihr Root-Window weiter rein Ã¼ber den bestehenden C#-Pfad
  - `AblesungErfassenPage.cs` und `ZaehlerwechselPage.cs`: Servicezugriff von nicht vorhandenem `IPlatformApplication` auf den vorhandenen MAUI-Kontext `Application.Current?.Handler?.MauiContext?.Services` umgestellt
  - `AppSettings.cs`: fehlenden MAUI-Storage-Namensraum fÃ¼r `FileSystem.AppDataDirectory` ergÃ¤nzt
  - `MemberSearchPage.xaml.cs`: fehlende XAML-Initialisierung durch denselben Seitenaufbau in C# ersetzt, damit der produktive Page-Pfad wieder sauber kompiliert
- Validierung:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lÃ¤uft jetzt erfolgreich durch
  - offen bleiben nur Warnungen, u.a. die bekannte Android-Warnung zu `AndroidCreatePackagePerAbi` und vorhandene Nullability-Warnungen in `HomeManagementPage.cs`
  - zusÃ¤tzlicher Sicherheitslauf `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief ebenfalls erfolgreich durch

## 2026-03-27 â€“ Prompt 4/4: projektweite AbschlussprÃ¼fung der Create-/Insert-Architektur durchgefÃ¼hrt und letzte Reststelle bereinigt

- Zuerst den echten Istzustand gegen den gepushten Stand `f3b37dc` geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/InsertRecordMappingExtensions.cs`
  - realen Git-Status Ã¼ber den vorgegebenen Visual-Studio-Git-Pfad
- Projektweite SuchlÃ¤ufe auf dem aktuellen Repo-Stand durchgefÃ¼hrt:
  - `.Insert(` Ã¼ber alle C#-Dateien
  - `From<...Record>().Insert(...)` bzw. `Insert(new ...Record` Ã¼ber alle C#-Dateien
  - `Id = 0`, `id = 0`, `entryId=0`, `entryId = 0` Ã¼ber C#-/XAML-Dateien
  - Aufrufsuche fÃ¼r die produktiven Create-Pfade `Nebenmitglied`, `StromzÃ¤hler`, `WasserzÃ¤hler`, `Ablesung`, `Arbeitsstunde`, `Wartungsvertrag`, `Arbeitseinsatz`, `Termin`, `Bekanntmachung`
- Ergebnis der Gesamtsuche:
  - die bereits bekannten produktiven Create-Pfade in `SupabaseService` und ihren WPF-/MAUI-Aufrufern laufen auf Insert-Modelle bzw. Create-Requests ohne `Id`
  - fÃ¼r die betroffenen produktiven UI-Pfade blieb die Suche nach `Id = 0` und `entryId=0` leer
  - letzte verbleibende produktive Reststelle war ein Insert im Auth-Bereich: `AuthService` legte `app_user` noch Ã¼ber `AppUserRecord` statt Ã¼ber ein separates Insert-Modell an
- Bereinigung dieses Abschlussblocks:
  - neues Insert-Modell `KGV.Infrastructure/Models/AppUserInsertRecord.cs` ergÃ¤nzt
  - `KGV.Infrastructure/Authentication/AuthService.cs` auf `client.From<AppUserInsertRecord>().Insert(...)` umgestellt
  - damit laufen nun auch die produktiven `app_user`-Neuanlagen nicht mehr Ã¼ber einen DB-Record-Typ
- Abschlussbewertung:
  - Create bleibt ohne `Id`
  - Update bleibt getrennt Ã¼ber bestehende Record-/ID-Pfade
  - `CreateNebenmitgliedAsync(...)` ist weiterhin produktiv Ã¼ber `NebenmitgliedCreateDTO` aktiv; ein alter Nebenmitglied-Altvertrag oder Altaufrufer wurde im aktuellen Repo-Stand nicht mehr gefunden
  - im aktuellen produktiven Suchraum ist die Create-Architektur jetzt auf Insert-Modelle bzw. Create-Requests ohne `Id` vereinheitlicht

## 2026-03-27 â€“ Prompt 2/4 Abschlusslauf: begonnenen Create-Aufrufer-Block gegen den echten Repo-Stand final geprÃ¼ft

- Erneut den realen Istzustand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/InsertRecordMappingExtensions.cs`
  - die bereits betroffenen WPF-/MAUI-Aufrufer
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
- Im aktuellen Stand bestÃ¤tigt:
  - WPF- und MAUI-Create-Aufrufer fÃ¼r `Arbeitsstunden`, `ArbeitseinsÃ¤tze`, `Termine`, `Bekanntmachungen`, `WartungsvertrÃ¤ge`, `StromzÃ¤hler`, `WasserzÃ¤hler` und `Ablesung` zeigen auf Insert-Modelle ohne `Id`
  - der WPF-Aufrufer fÃ¼r `CreateNebenmitgliedAsync(...)` verwendet produktiv `NebenmitgliedCreateDTO`
  - `entryId = 0` bzw. `Id = 0` bleibt nur UI-intern; die echten Create-Pfade senden keine `Id` mehr an Supabase
  - Update-Pfade bleiben weiter an echte bestehende IDs gebunden
- Validierung im Abschlusslauf:
  - gezielte Dateifehler auf allen bereits geÃ¤nderten Blockdateien blieben unauffÃ¤llig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erneut erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb ausschlieÃŸlich an bereits vorhandenen blockfremden Altfehlern in `App.xaml.cs`, `AblesungErfassenPage.cs`, `ZaehlerwechselPage.cs`, `AppSettings.cs` und `MemberSearchPage.xaml.cs` rot
- Ergebnis:
  - keine direkten Restfehler dieses Prompt-2/4-Blocks mehr offen
  - der gemeinsame Create-Aufrufer-Block ist fachlich abgeschlossen und wird jetzt organisatorisch per Commit/Push abgeschlossen

## 2026-03-27 â€“ Prompt 2/4 Abschluss: compile-bedingte WPF-/MAUI-Aufrufer auf neue Insert-Signaturen gezogen und Block sauber abgeschlossen

- Zuerst den echten Istzustand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - die neuen Insert-Modelle in `KGV.Core/Models`
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- Ehrlicher Befund vor der Anpassung:
  - die gemeinsamen VertrÃ¤ge und Insert-Modelle ohne `Id` waren bereits vorhanden
  - compile-bedingt offen waren nur direkte WPF-/MAUI-Aufrufer, die noch alte Create-Signaturen verwendeten
  - betroffen laut echtem Buildfehlerbild waren Create-Aufrufer fÃ¼r:
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
  - kleiner gemeinsamer Helper `KGV.Core/Models/InsertRecordMappingExtensions.cs` ergÃ¤nzt, aber nur fÃ¼r die wirklich mehrfach betroffenen Record-zu-Insert-Abbildungen:
    - `ArbeitsstundeRecord`
    - `ArbeitseinsatzRecord`
    - `TerminRecord`
    - `BekanntmachungRecord`
    - `WartungsvertragRecord`
  - ZÃ¤hler-/Ablesungs-Aufrufer bewusst ohne Schattenlogik direkt auf `...InsertRecord` umgestellt
  - WPF-Nebenmitglied-Aufrufer auf `CreateNebenmitgliedAsync(NebenmitgliedCreateDTO request)` gezogen; der gemeinsame Servicevertrag bleibt damit die einzige Wahrheit und der produktive Pfad ist wieder nutzbar
  - beim echten Create lÃ¤uft jetzt kein DB-Record mit `Id` und kein `id = 0` mehr in die umgestellten Insert-Pfade
- Technische Verifikation:
  - gezielte Dateifehler auf allen direkt geÃ¤nderten Blockdateien blieben nach der Umstellung unauffÃ¤llig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lÃ¤uft jetzt wieder erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist hinsichtlich der betroffenen Insert-Aufrufer bereinigt; offen bleiben im aktuellen Workspace nur bereits vorhandene blockfremde MAUI-Fehler in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
- Ergebnis:
  - die direkten WPF-/MAUI-UI-Aufrufer zeigen jetzt auf die Insert-Modelle ohne `Id`
  - `CreateNebenmitgliedAsync(...)` ist im produktiven gemeinsamen Vertrag wieder nutzbar
  - der Prompt-2/4-Block ist fachlich fÃ¼r das Signatur-Nachziehen abgeschlossen; dokumentierte blockfremde MAUI-Altfehler wurden bewusst nicht in diesen Block hineingezogen

## 2026-03-27 â€“ Prompt 1/4: Insert-Modelle ohne `Id` im gemeinsamen Core-/Infrastructure-Unterbau eingefÃ¼hrt und Servicevertrag auf Create-vs-Update getrennt

- Zuerst den echten Repo-/Log-/Vertragsstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - die betroffenen Create-BlÃ¶cke in `KGV.Infrastructure/Services/SupabaseService.cs`
  - die vorhandenen Record-/DTO-Dateien in `KGV.Core/Models`
- Ehrlicher Befund vor der Umstellung:
  - bereits sauber im Insert-Sinne waren nur die vorhandenen Pfade fÃ¼r `ParzellenBelegung` und die bestehende Infrastructure-Abbildung fÃ¼r `Arbeitsstunde`
  - noch unsauber bzw. nicht produktiv im gemeinsamen Vertrag waren die Create-Pfade fÃ¼r:
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
  - neue Insert-Modelle ohne PrimÃ¤rschlÃ¼ssel `Id` angelegt:
    - `KGV.Core/Models/MitgliedInsertRecord.cs`
    - `KGV.Core/Models/StromzaehlerInsertRecord.cs`
    - `KGV.Core/Models/WasserzaehlerInsertRecord.cs`
    - `KGV.Core/Models/AblesungInsertRecord.cs`
    - `KGV.Core/Models/ArbeitseinsatzInsertRecord.cs`
    - `KGV.Core/Models/TerminInsertRecord.cs`
    - `KGV.Core/Models/BekanntmachungInsertRecord.cs`
    - `KGV.Core/Models/WartungsvertragInsertRecord.cs`
    - `KGV.Core/Models/WartungsvertragZuordnungInsertRecord.cs`
  - `ISupabaseService` fÃ¼r betroffene Create-Pfade sauber auf Insert-Modelle ohne `Id` getrennt:
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
  - `CreateNebenmitgliedAsync` ist im Vertrag jetzt sauber fÃ¼r den spÃ¤teren Produktivpfad vorbereitet, in der Implementierung aber in diesem Block noch nicht reaktiviert
- Technische Verifikation:
  - gezielte Dateifehler auf Interface, Service und neue Insert-Modelle blieben unauffÃ¤llig
  - `dotnet build KGV.Core/KGV.Core.csproj` lief erfolgreich durch
  - `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj` lief erfolgreich durch
  - im Infrastructure-Build blieben nur bereits vorhandene Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber kein blockierender Fehler dieses Strukturblocks

## 2026-03-27 â€“ Prompt 1/1: Mitglieds-Unterpunkte optisch wie `â†³ WartungsvertrÃ¤ge` vereinheitlicht, ohne Rechte-/RoutingÃ¤nderung

- Zuerst den echten Repo-/Navigationsstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/AdminShell.cs`
  - `KGV.Maui/UserShell.cs`
  - `KGV.Wpf/ViewModels/MainWindowViewModel.cs`
- Ehrlicher Befund vor der Korrektur:
  - im aktuellen MAUI-Shell-Stand waren die relevanten Mitglieds-Unterpunkte bereits mit `â†³` konsistent aufgebaut
  - die optische Uneinheitlichkeit saÃŸ im WPF-Mitgliedsbereich in `MainWindowViewModel`
  - betroffen waren dort genau die Labels/EinrÃ¼ckungen von:
    - `Stammdaten`
    - `Arbeitsstunden`
    - `Dokumente`
    - `Admin-MenÃ¼`
    - `Benutzerverwaltung`
- Umsetzung bewusst klein und nur im Darstellungsblock:
  - in `KGV.Wpf/ViewModels/MainWindowViewModel.cs` alle fÃ¼nf genannten Mitglieds-Unterpunkte auf dasselbe sichtbare PrÃ¤fix `â†³` wie bei `WartungsvertrÃ¤ge` gezogen
  - dieselben fÃ¼nf Unterpunkte zusÃ¤tzlich auf dieselbe EinrÃ¼ckung/Margin wie `â†³ WartungsvertrÃ¤ge` vereinheitlicht
  - Reihenfolge, Zielseiten, Rechte und Sichtbarkeitslogik blieben unverÃ¤ndert
  - MAUI wurde nach PrÃ¼fung bewusst nicht geÃ¤ndert, weil der Shell-Stand dort bereits konsistent war
- Technische Verifikation:
  - `get_errors` auf `KGV.Wpf/ViewModels/MainWindowViewModel.cs` blieb unauffÃ¤llig
  - passender WPF-Build `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - der WPF-Build zeigte dabei nur bereits vorhandene Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber keinen blockierenden Fehler fÃ¼r diesen kleinen UI-Darstellungsblock
- Ergebnis:
  - die genannten Mitglieds-Unterpunkte wirken jetzt optisch einheitlich zum bestehenden Stil von `â†³ WartungsvertrÃ¤ge`
  - es wurde keine neue Fachlogik und keine Routing-/RechteÃ¤nderung erÃ¶ffnet

## 2026-03-27 â€“ Prompt 1/1 Abschlusslauf: begonnenen WPF-WartungsvertrÃ¤ge-Bugfixblock geprÃ¼ft, Logs nachgezogen und fÃ¼r sauberen Git-Abschluss eingegrenzt

- Vor dem Abschlusslauf erneut nur den echten Repo-/Log-/Blockstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - genau die ausdrÃ¼cklich genannten Blockdateien:
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml`
    - `KGV.Wpf/Views/WartungsvertraegeVerwaltungView.xaml.cs`
    - `KGV.Wpf/ViewModels/WartungsvertragMitgliederZuordnungViewModel.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
  - gezielte Dateifehler auf genau diesen vier WPF-/Service-Dateien
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Stand:
  - der begonnene kleine WPF-WartungsvertrÃ¤ge-Bugfix war in den genannten Blockdateien bereits produktiv vorhanden
  - Doppelklick in der globalen WartungsvertrÃ¤ge-Liste Ã¶ffnet jetzt denselben Detailpfad wie der vorhandene `Ã–ffnen`-Button
  - die Mitgliedersortierung im Zuordnungsdialog lÃ¤uft jetzt belastbar Ã¼ber Nachname/Vorname statt nur Ã¼ber den zusammengesetzten Anzeigenamen
  - die echte Save-Ursache lag im Insertpfad `wartungsvertrag_zuordnungen`; dort werden `created_at` und `updated_at` jetzt gesetzt
  - Postgrest-/DB-Details werden im direkt betroffenen Save-Fehlerpfad jetzt sauberer in Logger und Debug-Ausgabe sichtbar
  - in diesen vier Blockdateien zeigte sich im aktuellen Abschlusslauf kein weiterer direkter Korrekturbedarf; zusÃ¤tzlicher Produktivcode wurde deshalb nicht mehr geÃ¤ndert
- Technische Verifikation dieses Abschlusslaufs:
  - `get_errors` auf den vier Blockdateien blieb unauffÃ¤llig
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - der WPF-Build blieb dabei nur mit bereits vorhandenen Nullability-Warnungen in `KGV.Infrastructure/Services/SupabaseService.cs`, aber ohne blockierenden Fehler fÃ¼r diesen kleinen WartungsvertrÃ¤ge-Abschlussblock
- Ergebnis dieses kleinen WPF-Bugfixblocks:
  - der WPF-WartungsvertrÃ¤ge-Bugfixblock ist fachlich abgeschlossen
  - fÃ¼r den Git-Abschluss werden nur die vier echten Blockdateien plus die beiden Logdateien aufgenommen; blockfremde offene Dateien bleiben ausdrÃ¼cklich drauÃŸen

## 2026-03-27 â€“ Prompt 3/3 Abschlusslauf: mitgliedsbezogenen WartungsvertrÃ¤ge-Block ehrlich fortgeschrieben und fÃ¼r sauberen Git-Abschluss eingegrenzt

- Zuerst erneut nur den echten Repo-/Log-/Blockstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - genau die ausdrÃ¼cklich genannten Prompt-3/3-Blockdateien:
    - `KGV.Core/Interfaces/ISupabaseService.cs`
    - `KGV.Core/Models/MemberWartungsvertragItem.cs`
    - `KGV.Infrastructure/Services/SupabaseService.cs`
    - `KGV.Wpf/ViewModels/MemberWartungsvertraegeViewModel.cs`
    - `KGV.Wpf/Views/MemberWartungsvertraegeView.xaml`
    - `KGV.Maui/Pages/MemberWartungsvertraegePage.cs`
  - gezielte Dateifehler auf genau diesen Dateien
  - `get_tests` mit Bezug auf `Wartungsvertrag`
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Abschlussstand:
  - der mitgliedsbezogene WartungsvertrÃ¤ge-Flow war in den genannten Blockdateien bereits produktiv ergÃ¤nzt
  - die Shared-Servicepfade fÃ¼r freie WartungsvertrÃ¤ge je Mitglied, Zuweisen an genau ein Mitglied und Beenden Ã¼ber `gueltig_bis` waren im aktuellen Stand vorhanden
  - WPF-Mitgliedsansicht und `KGV.Maui/Pages/MemberWartungsvertraegePage.cs` waren dateibezogen ohne zusÃ¤tzlichen Abschlussfehler
  - deshalb wurden keine weiteren FachÃ¤nderungen am Produktivcode dieses Prompt-3/3-Blocks vorgenommen; nachgezogen wurden nur die Abschlusslogs
- Technische Verifikation:
  - `get_errors` auf den sechs Blockdateien blieb unauffÃ¤llig
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte weiterhin keinen passenden Testfall
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb im aktuellen Workspace nur blockfremd rot in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - diese MAUI-Fehler wurden bewusst nicht repariert, weil sie nicht zu diesem WartungsvertrÃ¤ge-Abschlussblock gehÃ¶ren
- Ergebnis:
  - Prompt `3/3` ist fachlich abgeschlossen
  - der Git-Abschluss wird auf die echten Blockdateien plus Logdateien begrenzt; blockfremde offene Ã„nderungen bleiben drauÃŸen

## 2026-03-27 â€“ Prompt 2/3 Abschlusslauf: vorhandenen WartungsvertrÃ¤ge-Blockcommit geprÃ¼ft und sauber nach `origin/main` gepusht

- Den echten Git-Istzustand Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad geprÃ¼ft:
  - Branch `main`
  - `HEAD` auf `74d0f72`
  - `74d0f72` lokal vorhanden
  - `origin/main` vor dem Lauf genau einen Commit hinter dem lokalen Stand
- Den vorhandenen Commit `74d0f72` gegen die ausdrÃ¼cklich genannten WartungsvertrÃ¤ge-Blockdateien geprÃ¼ft:
  - kein neuer Fach- oder Abschlussfehler in den Blockdateien
  - deshalb keine neuen CodeÃ¤nderungen am WartungsvertrÃ¤ge-Block vorgenommen
- Buildstatus bewusst nicht neu zu einem Reparaturblock ausgerollt:
  - WPF bleibt im zuletzt verifizierten Stand grÃ¼n
  - MAUI bleibt fÃ¼r diesen Abschlusslauf nur blockfremd dokumentiert rot in:
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/Settings/AppSettings.cs`
    - `KGV.Maui/Pages/AblesungErfassenPage.cs`
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
- Den vorhandenen Block-Commit anschlieÃŸend ohne weitere FachÃ¤nderung erfolgreich nach `origin/main` gepusht.
- Blockfremde offene lokale Ã„nderungen im Arbeitsbaum blieben bewusst ungestagt und auÃŸerhalb dieses WartungsvertrÃ¤ge-Abschlusses.

## 2026-03-27 â€“ Prompt 2/3: begonnenen WartungsvertrÃ¤ge-Verwaltungsblock auf realem Zwischenstand fertiggestellt, MAUI-AnschlÃ¼sse geschlossen und RÃ¼ckkehrpfade saubergezogen

- Zuerst den echten aktuellen Repo-/Log-/Dateistand geprÃ¼ft statt neu anzusetzen:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - die ausdrÃ¼cklich genannten Shared-/WPF-/MAUI-WartungsvertrÃ¤ge-Dateien des begonnenen Prompt-2/3-Blocks
  - gezielte Dateifehler auf genau diesen Blockdateien
- Ehrlicher Befund im vorhandenen Zwischenstand:
  - der Shared-Servicepfad fÃ¼r `CreateWartungsvertragAsync(...)`, `UpdateWartungsvertragAsync(...)` und `AssignMitgliederToWartungsvertragAsync(...)` war bereits real vorhanden
  - die WPF-Verwaltungsseiten `WartungsvertragEditor` und `WartungsvertragMitgliederZuordnung` waren im aktuellen Stand fachlich weitgehend fertig angelegt
  - unvollstÃ¤ndig war vor allem der MAUI-Verwaltungsanschluss:
    - `WartungsvertragEditorPage` und `WartungsvertragAssignMembersPage` existierten bereits, waren aber noch nicht vollstÃ¤ndig an DI-/Routing-/Detailpfade angeschlossen
    - `WartungsvertraegePage` bot noch keinen produktiven Einstieg `Neu`
    - `WartungsvertragDetailPage` hatte den begonnenen Verwaltungsblock noch nicht mit `Bearbeiten` / `Mitglieder zuweisen` und sauberem Reload nach RÃ¼ckkehr abgeschlossen
- Umsetzung deshalb bewusst klein und nur auf den begonnenen WartungsvertrÃ¤ge-Block begrenzt:
  - WPF:
    - `WartungsvertraegeVerwaltungViewModel` fÃ¼r den fertigen Verwaltungsstand sprachlich geschÃ¤rft
    - `EditCommand`/`NewCommand`-Requery bei Auswahl-/Busy-Wechsel sauber nachgezogen
    - `WartungsvertragDetailViewModel`-Beschreibung auf den nun produktiven Verwaltungsstand angepasst
  - MAUI:
    - `WartungsvertragEditorPage` und `WartungsvertragAssignMembersPage` Ã¼ber `MauiProgram` und `ShellRouteRegistrar` produktiv angeschlossen
    - `WartungsvertraegePage` um den produktiven Admin-/Vorstand-Einstieg `Neu` ergÃ¤nzt
    - globale Detailnavigation aus der Ãœbersichtsseite jetzt mit explizitem `adminMode=1`, damit Verwaltungsaktionen nur im globalen Verwaltungsweg sichtbar werden und nicht unbeabsichtigt im mitgliedsbezogenen Detailpfad von Prompt `3/3`
    - `WartungsvertragDetailPage` um `Bearbeiten` und `Mitglieder zuweisen` ergÃ¤nzt
    - `WartungsvertragDetailPage` lÃ¤dt beim Wiedererscheinen jetzt bewusst erneut, damit `Belegt` / `Frei` / Mitgliederliste nach Speichern oder Zuweisung nicht in einem Altzustand hÃ¤ngen bleiben
    - Busy-/AktionszustÃ¤nde fÃ¼r Ãœbersicht, Detail und Zuweisungsseite sichtbar deaktiviert bzw. konsistent gehalten
- Fachlich bestÃ¤tigter Stand des fertiggestellten Prompt-2/3-Blocks:
  - Wartungsvertrag `Neu` produktiv vorhanden
  - Wartungsvertrag `Bearbeiten` produktiv vorhanden
  - globale Mitgliederzuweisung produktiv vorhanden
  - Kontingent wird weiterhin fachlich im Shared-Service abgesichert; freie PlÃ¤tze bleiben in WPF und MAUI sichtbar, zusÃ¤tzliche Auswahl wird bei Erreichen des Kontingents deaktiviert bzw. serverseitig sauber blockiert
  - Create bleibt ohne fehlerhafte DB-ID; Update bleibt an echte bestehende ID gebunden
  - RÃ¼ckkehr nach Speichern fÃ¼hrt wieder in die passende View:
    - WPF zurÃ¼ck in die gemeinsame Detailansicht
    - MAUI zurÃ¼ck in die passende Detailansicht bzw. bei `Neu` in die neu angelegte Detailansicht
- Bewusst nicht vorgezogen:
  - kein mitgliedsbezogener Zuweisungsflow aus der Mitgliedsansicht
  - keine Historienverwaltung / kein Beenden bestehender Zuordnungen als eigener Fachblock
  - keine Massenoperationen
  - Prompt `3/3` bleibt damit ausdrÃ¼cklich offen
- Technische Verifikation:
  - `get_errors` auf den direkt geÃ¤nderten Blockdateien blieb unauffÃ¤llig
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte weiterhin keinen passenden Testfall
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` blieb im aktuellen Workspace nicht grÃ¼n, aber blockfremd an bereits offenen Fehlern in:
    - `KGV.Maui/App.xaml.cs` (`InitializeComponent`)
    - `KGV.Maui/Settings/AppSettings.cs` (`FileSystem`)
    - `KGV.Maui/Pages/AblesungErfassenPage.cs` (`IPlatformApplication`)
    - `KGV.Maui/Pages/ZaehlerwechselPage.cs` (`IPlatformApplication`)
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs` (`InitializeComponent`)
  - die direkt geÃ¤nderten WartungsvertrÃ¤ge-Dateien selbst blieben dateibezogen fehlerfrei

## 2026-03-27 â€“ Prompt 1/3 Abschlusslauf: WartungsvertrÃ¤ge-Basisblock in WPF und MAUI technisch sauber final validiert

- Vor dem Abschlusslauf erneut nur den realen Repo-/Log-/Blockstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - ausschlieÃŸlich die bereits zum WartungsvertrÃ¤ge-Basisblock gehÃ¶renden Shared-/WPF-/MAUI-Dateien
  - gezielte Dateifehler auf genau diesen Blockdateien
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
  - realen Git-Status Ã¼ber den ausdrÃ¼cklich vorgegebenen Visual-Studio-Git-Pfad
- Ehrlicher Befund im aktuellen Abschlussstand:
  - die bereits angelegten WartungsvertrÃ¤ge-Dateien in `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` waren dateibezogen im aktuellen Stand konsistent
  - in diesem Abschlusslauf mussten an den Blockdateien keine zusÃ¤tzlichen Fach- oder Compilekorrekturen mehr vorgenommen werden
  - `get_tests` mit Bezug auf `Wartungsvertrag` lieferte aktuell keinen passenden Testfall; ein zusÃ¤tzlicher Testlauf war daher fÃ¼r diesen Block nicht mÃ¶glich
  - blockfremde bereits vorhandene Ã„nderungen im Arbeitsbaum wurden bewusst nicht angefasst und nicht in den Abschluss dieses Blocks gezogen
- Technische Verifikation:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief erfolgreich durch
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief erfolgreich durch
  - der MAUI-Build zeigt im aktuellen Workspace weiterhin nur bereits vorhandene Nullable-Warnungen, aber keinen blockierenden Fehler fÃ¼r diesen Block
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
  - offen bleiben damit weiterhin ausdrÃ¼cklich weitergehende Funktionen wie Neu/Bearbeiten, Zuweisen per Checkbox, Historienbearbeitung und Kontingent-Auswahllogik

## 2026-03-27 â€“ MAUI-MenÃ¼punkt `WartungsvertrÃ¤ge` wieder in die Navigation aufgenommen, Ã¼ber vorhandenen Mitgliedskontext statt neuer Seite

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprÃ¼ft:
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
  - in WPF existieren aktuell fÃ¼r `WartungsvertrÃ¤ge` nur vorbereitete Placeholder (`MemberWartungsvertraegeViewModel` / `WartungsvertraegeVerwaltungViewModel`); ein eigener produktiv verdrahteter Hauptnavigationspunkt war im aktuellen Stand nicht mehr aktiv
  - fachlich liegt das Thema in WPF damit nÃ¤her am Mitgliedskontext als an einem losen allgemeinen Root-MenÃ¼
  - in MAUI existierte ebenfalls noch keine eigene Seite oder Route fÃ¼r `WartungsvertrÃ¤ge`
  - der belastbare bestehende mobile Produktivpfad war bereits vorhanden: `MeineDatenPage` enthÃ¤lt die produktive Sektion `WartungsvertrÃ¤ge / Pflichtstunden`
- Umsetzung bewusst klein und nur im MAUI-Navigationspfad:
  - in `KGV.Maui/AdminShell.cs` unter dem ausgewÃ¤hlten Mitgliedskontext den MenÃ¼punkt `â†³ WartungsvertrÃ¤ge` ergÃ¤nzt
  - in `KGV.Maui/UserShell.cs` im eigenen Mitgliedskontext den MenÃ¼punkt `â†³ WartungsvertrÃ¤ge` ergÃ¤nzt
  - beide MenÃ¼eintrÃ¤ge verwenden bewusst den vorhandenen Zielpfad `MeineDatenPage`
  - keine neue Seite, keine neue Dummy-Route und kein grÃ¶ÃŸerer Flyout-Umbau
  - `WartungsvertrÃ¤ge` ist damit in MAUI kontextgebunden sichtbar:
    - Admin/Vorstand im ausgewÃ¤hlten Mitgliedskontext
    - User im eigenen Mitgliedskontext
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen Navigationsdateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Workspace erfolgreich durch
  - der MAUI-Gesamtbuild blieb nach diesem kleinen Navigationsblock grÃ¼n

## 2026-03-27 â€“ Neuen blockfremden MAUI-Compile-Rest in `ShellRouteRegistrar`, `MemberSearchViewModel`, `AblesenOverviewPage`, `ArbeitseinsaetzeManagementPage`, `BekanntmachungenManagementPage`, `MemberSearchPage.xaml.cs` und `TermineManagementPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
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
  - gezielte Dateifehler fÃ¼r genau diese MAUI-Dateien
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die sichtbaren Fehler lagen erneut nicht in Fachlogik, sondern in fehlenden aktiven MAUI-Usings bzw. TypauflÃ¶sungen
  - betroffen waren vor allem:
    - `Routing` in `ShellRouteRegistrar.cs`
    - `Command` in `MemberSearchViewModel.cs`
    - `LineBreakMode` und `CornerRadius` in `AblesenOverviewPage.cs`
    - `Shell` in `ArbeitseinsaetzeManagementPage.cs`, `BekanntmachungenManagementPage.cs` und `TermineManagementPage.cs`
  - `MemberSearchPage.xaml.cs` wurde zusammen mit `MemberSearchPage.xaml` geprÃ¼ft; `x:Class`, Namespace, Basistyp und Code-Behind passen im aktuellen Stand zusammen und waren nicht der Fehlerursprung
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/ShellRouteRegistrar.cs` `using Microsoft.Maui.Controls` ergÃ¤nzt
  - in `KGV.Maui/ViewModels/MemberSearchViewModel.cs` `using Microsoft.Maui.Controls` ergÃ¤nzt
  - in `KGV.Maui/Pages/AblesenOverviewPage.cs` `using Microsoft.Maui` ergÃ¤nzt
  - in `KGV.Maui/Pages/ArbeitseinsaetzeManagementPage.cs` `using Microsoft.Maui.Controls` ergÃ¤nzt
  - in `KGV.Maui/Pages/BekanntmachungenManagementPage.cs` `using Microsoft.Maui.Controls` ergÃ¤nzt
  - in `KGV.Maui/Pages/TermineManagementPage.cs` `using Microsoft.Maui.Controls` ergÃ¤nzt
  - an `MemberSearchPage.xaml.cs` und `MemberSearchPage.xaml` keine CodeÃ¤nderung, weil der XAML-/Code-Behind-Vertrag im aktuellen Stand bereits konsistent ist
  - keine Fachlogik, keine Navigation, keine Shell-/Routing-/CRUD-/Supabase-Pfade umgebaut
- Technische Verifikation:
  - `get_errors` auf den sieben Zielpfaden blieb nach der Korrektur unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Workspace wieder erfolgreich durch
  - der MAUI-Gesamtbuild ist mit diesem kleinen Compileblock wieder grÃ¼n

## 2026-03-27 â€“ Blockfremden MAUI-Startup-Compilefehlerstand in `MauiProgram`, `App.xaml.cs` und `MainApplication` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - `KGV.Maui/MauiProgram.cs`
  - `KGV.Maui/App.xaml.cs`
  - `KGV.Maui/MainApplication.cs`
  - gezielte Dateifehler fÃ¼r genau diese drei MAUI-Startup-Dateien
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die sichtbaren Fehler in genau diesen drei Dateien lagen nicht in Fachlogik, sondern in fehlenden aktiven MAUI-Startup-Namespaces
  - betroffen waren vor allem TypauflÃ¶sungen fÃ¼r `MauiApp`, `IActivationState` sowie direkt benachbarte Startup-/Hosting-/Storage-Typen
  - `MauiProgram.cs` fehlten im aktuellen Stand aktive Referenzen fÃ¼r Hosting-/Storage- und DI-Erweiterungen
  - `App.xaml.cs` fehlte der aktive MAUI-Typbezug fÃ¼r `IActivationState`
  - `MainApplication.cs` fehlte der aktive Hosting-Typbezug fÃ¼r `MauiApp`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/MauiProgram.cs` die fehlenden aktiven `using`-Direktiven fÃ¼r:
    - `Microsoft.Extensions.DependencyInjection`
    - `Microsoft.Maui.Controls.Hosting`
    - `Microsoft.Maui.Hosting`
    - `Microsoft.Maui.Storage`
    ergÃ¤nzt
  - in `KGV.Maui/App.xaml.cs` die fehlenden aktiven `using`-Direktiven fÃ¼r:
    - `Microsoft.Maui`
    - `System`
    - `System.Threading.Tasks`
    ergÃ¤nzt
  - in `KGV.Maui/MainApplication.cs` die fehlende aktive `using`-Direktive `Microsoft.Maui.Hosting` ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Shell-/Routing-/CRUD-/Supabase-Pfade verÃ¤ndert
- Technische Verifikation:
  - `get_errors` auf den drei direkt betroffenen Startup-Dateien blieb nach der Korrektur unauffÃ¤llig
  - die drei Zielpfade sind damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Block weiterhin nicht grÃ¼n
  - der Gesamtbuild scheitert jetzt blockfremd an weiteren bereits offenen MAUI-Compilefehlern, u. a. in:
    - `KGV.Maui/ShellRouteRegistrar.cs`
    - `KGV.Maui/ViewModels/MemberSearchViewModel.cs`
    - `KGV.Maui/Pages/AblesenOverviewPage.cs`
    - `KGV.Maui/Pages/ArbeitseinsaetzeManagementPage.cs`
    - `KGV.Maui/Pages/BekanntmachungenManagementPage.cs`
    - `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
    - `KGV.Maui/Pages/TermineManagementPage.cs`
  - diese blockfremden Fehler wurden in diesem kleinen Startup-Block bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-27 â€“ MAUI-RFID-Flows auf `Scan-first` zurÃ¼ckgezogen, NFC-Einstellungsweg erhalten und blockfremden Buildrest ehrlich dokumentiert

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - WPF-Referenz nur lesend fÃ¼r den Soll-Flow:
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
  - der aktive MAUI-Einstieg `RfidEinrichtenPage` zeigte fachlich zu frÃ¼h bereits `Parzelle`, `Medium`, PrÃ¼fen und Speichern; der Einstieg war damit nicht mehr scan-first
  - der gemeinsame MAUI-Scanpfad `RfidScanWorkflowPage` zeigte fÃ¼r `Ablesen` und `ZÃ¤hlerwechsel` ebenfalls schon auf dem ersten Screen zusÃ¤tzliche Folge-/Fallback-Bereiche statt zuerst nur den Scan
  - der gute NFC-aus-Hinweis mit Button in die Systemeinstellungen war im aktuellen MAUI-Pfad bereits vorhanden und durfte nicht verloren gehen
  - direkt benachbart lag in `AblesenOverviewPage.cs` zusÃ¤tzlich ein gleichartiger MAUI-Compilefehler durch fehlende Usings vor; dieser wurde wegen unmittelbarer Beteiligung am RFID-Einstieg mitgenommen
- Umsetzung bewusst klein und nur im RFID-Pfad:
  - `RfidEinrichtenViewModel` um einen kleinen vorgeschalteten Scan-/ExistenzprÃ¼fschritt ergÃ¤nzt:
    - gescannte UID wird jetzt zuerst global Ã¼ber den vorhandenen Produktivpfad `ResolveRfidScanContextAsync(...)` geprÃ¼ft
    - bekannter Tag => klare Meldung, kein normaler Speichern-Flow
    - unbekannter Tag => erst dann zweiter Schritt mit `Parzelle`, `Medium`, PrÃ¼fen und Speichern
    - sichtbare Busy-Texte fÃ¼r `RFID wird geprÃ¼ft.` und `RFID wird gespeichert.` ergÃ¤nzt
  - `RfidEinrichtenPage` auf scan-first zurÃ¼ckgezogen:
    - erster Screen zeigt jetzt nur noch den Scanbereich
    - Folgeansicht fÃ¼r Zuordnung wird nur fÃ¼r unbekannte Tags sichtbar
    - vorhandener NFC-Hinweis und der Button `NFC-Einstellungen Ã¶ffnen` bleiben erhalten
  - `RfidScanWorkflowPage` fÃ¼r `Ablesen` und `ZÃ¤hlerwechsel` so geschÃ¤rft, dass zunÃ¤chst nur der Scanbereich sichtbar ist; Kontext-/Entscheidungsbereich erscheint erst nach aufgelÃ¶stem Scan
  - in `AblesenOverviewPage.cs` nur die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt, weil die Seite direkt zum betroffenen RFID-Einstieg gehÃ¶rt
  - keine neue RFID-Architektur, keine neue Shell-/Routing-/Supabase-Struktur und keine WPF-Ã„nderung
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen RFID-Dateien blieb nach der Korrektur unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Block nicht grÃ¼n geblieben
  - der Build scheitert aktuell blockfremd an bereits offenen bzw. neu auÃŸerhalb dieses RFID-Blocks sichtbaren MAUI-Compilefehlern in:
    - `KGV.Maui/MauiProgram.cs`
    - `KGV.Maui/App.xaml.cs`
    - `KGV.Maui/MainApplication.cs`
  - dort fehlen im aktuellen Auszug u. a. aktive TypauflÃ¶sungen fÃ¼r `MauiApp` bzw. `IActivationState`
  - diese blockfremden Fehler wurden in diesem RFID-Block bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-27 â€“ NÃ¤chsten blockfremden MAUI-Compileblock fÃ¼r `ArbeitsstundenReviewPage`, `DokumentePage` und `ExportPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `.github/copilot-instructions.md`
  - `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs`
  - `KGV.Maui/Pages/DokumentePage.xaml`
  - `KGV.Maui/Pages/DokumentePage.xaml.cs`
  - `KGV.Maui/Pages/ExportPage.cs`
  - gezielte Dateifehler fÃ¼r genau diese drei MAUI-Pfade
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - `ArbeitsstundenReviewPage.cs` und `ExportPage.cs` liegen im aktiven Stand als code-only MAUI-Seiten vor
  - `DokumentePage.xaml.cs` liegt zwar als `.xaml.cs` vor, die direkt zugehÃ¶rige `DokumentePage.xaml` ist im aktuellen Stand leer; ein aktiver `x:Class`-/Partial-/Code-Behind-Vertrag war dort somit nicht der Fehlerursprung
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/ApplicationModel-/System-Namespaces
  - betroffen waren u. a. TypauflÃ¶sungen fÃ¼r `ContentPage`, `CollectionView`, `Label`, `Button`, `Border`, `ScrollView`, `VerticalStackLayout`, `SelectionChangedEventArgs`, `IValueConverter`, `LineBreakMode`, `Colors`, `Thickness`, `FileSystem`, `ShareFileRequest` und `ShareFile`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-/Collections-/Linq-/Tasks-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/DokumentePage.xaml.cs` die fehlenden aktiven MAUI-/ApplicationModel-/Graphics-/System-/Linq-/Tasks-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/ExportPage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/DataTransfer-/Storage-/Graphics-/System-Usings ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geÃ¤ndert
  - keine XAML-Struktur erfunden oder umgebaut, weil `DokumentePage.xaml` im aktuellen Stand keinen aktiven XAML-Klassenvertrag trÃ¤gt
- Technische Verifikation:
  - `get_errors` auf den drei Zielpfaden blieb nach dem Fix unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lÃ¤uft im aktuellen Workspace wieder erfolgreich durch
  - der gezielte MAUI-Compileblock ist damit dateibezogen und im Gesamtprojekt compile-seitig bereinigt

## 2026-03-26 â€“ MAUI-Laufzeit-/Fachblock fÃ¼r Home-/Detail-/Editorpfade gezielt nach grÃ¼nem Build geprÃ¼ft und minimal gehÃ¤rtet

- Vor dem Block erneut nur den realen Repo-/Log-/Pfadstand geprÃ¼ft:
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
  - gezielte SuchlÃ¤ufe auf `entryId=0`, `id = 0`, Delete-Pfade, Busy-Texte, `.Result` und `.Wait(`
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - die aktiven Admin-/Vorstand-Aktionen `Neu`, `Bearbeiten`, `LÃ¶schen` lagen im aktuellen MAUI-Stand bereits auf derselben Detailseite `HomeSectionDetailPage`; es war dafÃ¼r keine neue Admin-Sonderseite nÃ¶tig
  - der Startseiten-Schnellzugriff `Arbeitsstunde erfassen` ist im aktiven `HomePage`-/`HomeViewModel`-Pfad bereits vorhanden und weiter funktionsfÃ¤hig angebunden
  - die mobilen Editorpfade fÃ¼r `Bekanntmachungen`, `Termine`, `ArbeitseinsÃ¤tze` und auch `Arbeitsstunden` setzten beim Erzeugen der Persistenzobjekte lokal noch explizit `Id = 0`, obwohl die produktiven Create-Pfade im Service Insert ohne DB-ID fahren sollen
  - `DeleteBekanntmachungAsync(...)`, `DeleteTerminAsync(...)` und `DeleteArbeitseinsatzAsync(...)` existieren im Shared-Service bereits; im aktiven Detailpfad fehlte dort aber noch robuste Fehlerbehandlung bei Laufzeitfehlern
  - sichtbare Busy-Texte waren im betroffenen MAUI-Pfad nur teilweise durchgezogen; insbesondere `BekanntmachungEditorPage` und `TermineEditorPage` setzten vor Save noch keinen klaren Speicherkontext fÃ¼r den Nutzer
  - der direkte Suchlauf auf `.Result` und `.Wait(` Ã¼ber die direkt betroffenen MAUI-Dateien blieb leer
  - ein geforderter CPU-Profiler-Lauf fÃ¼r den TrÃ¤gheitspfad lieÃŸ sich im aktuellen Visual-Studio-Setup trotz zweitem Versuch nicht starten, weil die referenzierte `.diagsession` nicht gefunden wurde; der Performance-Teil war damit messtechnisch blockiert
- Umsetzung bewusst klein und nur auf die direkt betroffenen Laufzeit-/Fachursachen begrenzt:
  - in `KGV.Maui/Pages/BekanntmachungEditorPage.cs` den Save-Pfad sichtbar auf `Daten werden gespeichert.` gezogen, per `Task.Yield()` kurz an den Renderzyklus zurÃ¼ckgegeben und die blinde Initialisierung `Id = 0` entfernt; eine `Id` wird jetzt nur noch im echten Bearbeiten-Modus gesetzt
  - in `KGV.Maui/Pages/TermineEditorPage.cs` dieselbe Korrektur fÃ¼r Busy-Text, kurzes Render-Yield und saubere Unterscheidung `Neu` vs. `Bearbeiten` ohne explizites `Id = 0`
  - in `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs` die Record-Erzeugung ebenfalls von der expliziten `Id = 0`-Initialisierung gelÃ¶st; eine `Id` wird nur noch gesetzt, wenn tatsÃ¤chlich ein bestehender Datensatz bearbeitet wird
  - in `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs` denselben Save-Pfad klein nachgezogen: kurzes `Task.Yield()` vor dem produktiven Save und keine explizite `Id = 0`-Zuweisung mehr bei Neuanlage
  - in `KGV.Maui/Pages/HomeSectionDetailPage.cs` die produktiven Aktionen `Anmelden`, `Abmelden` und `LÃ¶schen` robuster gemacht:
    - Busy-Farbe fÃ¼r `Daten werden gespeichert.` / `Datensatz wird gelÃ¶scht.` sichtbar auf den aktiven Ladezustand gezogen
    - zusÃ¤tzliche Fehlerbehandlung ergÃ¤nzt, damit Laufzeitfehler im Detailpfad nicht still oder unkontrolliert durchlaufen
    - Delete bleibt auf demselben bestehenden Produktivpfad mit BestÃ¤tigungsdialog und anschlieÃŸendem Home-Reload Ã¼ber `ReloadAsync()`
  - keine neue Seite, keine neue CRUD-Architektur, keine Shell-/Routing-RÃ¼ckdrehung und keine Ã„nderung an WPF
- Technische Verifikation:
  - `get_errors` auf den direkt geÃ¤nderten MAUI-Dateien blieb nach der Korrektur unauffÃ¤llig
  - der gezielte Suchlauf auf `.Result|.Wait(` blieb in den direkt betroffenen MAUI-Pfaden weiter leer
  - der Startseiten-Button `Arbeitsstunde erfassen` ist im aktiven Pfad weiter vorhanden; kein Wiederherstellungsumbau war nÃ¶tig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` ist im aktuellen Workspace nach diesem Lauf nicht grÃ¼n geblieben
  - der Gesamtbuild scheitert aktuell blockfremd an bereits vorhandenen bzw. neu auÃŸerhalb dieses Blocks offenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/ArbeitsstundenReviewPage.cs`
    - `KGV.Maui/Pages/DokumentePage.xaml.cs`
    - `KGV.Maui/Pages/ExportPage.cs`
  - diese Fehler wurden in diesem Lauf bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 â€“ NÃ¤chsten blockfremden MAUI-Compileblock fÃ¼r `FaelligeZaehlerPage`, `ArbeitsstundenReviewDetailPage` und `ManagementOverviewPageBase` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/FaelligeZaehlerPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs`
  - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
  - gezielte Dateifehler fÃ¼r genau diese drei MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle drei betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-GegenstÃ¼cke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen nicht in Fachlogik, sondern erneut in fehlenden MAUI-/Graphics-/System-Namespaces
  - betroffen waren dort u. a. TypauflÃ¶sungen fÃ¼r `ContentPage`, `CollectionView`, `Label`, `Button`, `ScrollView`, `VerticalStackLayout`, `HorizontalStackLayout`, `Border`, `Editor`, `View`, `Colors`, `LineBreakMode`, `TextAlignment`, `GridLength`, `CornerRadius`, `Dispatcher` und `FontAttributes`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/FaelligeZaehlerPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-/Tasks-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/ManagementOverviewPageBase.cs` die fehlenden aktiven MAUI-/Graphics-/Collections-/Linq-/System-/Tasks-Usings ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geÃ¤ndert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die drei Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf den drei Zielseiten blieb nach dem Fix unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lÃ¤uft im aktuellen Workspace wieder erfolgreich durch
  - der gezielte MAUI-Compileblock ist damit dateibezogen und im Gesamtprojekt compile-seitig bereinigt

## 2026-03-26 â€“ NÃ¤chsten blockfremden MAUI-Compileblock fÃ¼r `NebenmitgliedPage`, `UserManagementPage`, `RfidEinrichtenPage`, `MemberGardensPage` und `RfidScanWorkflowPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/NebenmitgliedPage.cs`
  - `KGV.Maui/Pages/UserManagementPage.cs`
  - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
  - `KGV.Maui/Pages/MemberGardensPage.cs`
  - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - gezielte Dateifehler fÃ¼r genau diese fÃ¼nf MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle fÃ¼nf betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-GegenstÃ¼cke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/System-Namespaces
  - betroffen waren dort u. a. TypauflÃ¶sungen fÃ¼r `ContentPage`, `View`, `Label`, `Button`, `Entry`, `Border`, `CollectionView`, `Picker`, `Grid`, `IValueConverter`, `Keyboard`, `Thickness`, `CornerRadius`, `LineBreakMode`, `TextAlignment`, `GridLength` und `Shell`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/NebenmitgliedPage.cs` die fehlenden aktiven MAUI-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/UserManagementPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/RfidEinrichtenPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/MemberGardensPage.cs` die fehlenden aktiven MAUI-/Graphics-/Collections-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/RfidScanWorkflowPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geÃ¤ndert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die fÃ¼nf Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf den fÃ¼nf Zielseiten blieb nach dem Fix unauffÃ¤llig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grÃ¼n
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/FaelligeZaehlerPage.cs`
    - `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs`
    - `KGV.Maui/Pages/ManagementOverviewPageBase.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 â€“ NÃ¤chsten blockfremden MAUI-Compileblock fÃ¼r `MyProfilePage`, `HomeSectionDetailPage` und `ParzellenPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/MyProfilePage.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - `KGV.Maui/Pages/ParzellenPage.cs`
  - gezielte Dateifehler fÃ¼r genau diese drei MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - alle drei betroffenen Dateien liegen im aktiven Stand als code-only MAUI-Seiten vor; direkte aktive `.xaml`-GegenstÃ¼cke wurden im Seitenpfad nicht gefunden
  - die Compilefehler lagen erneut nicht in Fachlogik, sondern in fehlenden MAUI-/Graphics-/System-/ApplicationModel-Namespaces
  - betroffen waren dort u. a. TypauflÃ¶sungen fÃ¼r `ContentPage`, `View`, `Border`, `Label`, `Button`, `Entry`, `Grid`, `CollectionView`, `VerticalStackLayout`, `HorizontalStackLayout`, `IValueConverter`, `Launcher`, `Colors`, `Thickness`, `Keyboard`, `LineBreakMode`, `LayoutOptions` und `TextAlignment`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/MyProfilePage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/HomeSectionDetailPage.cs` die fehlenden aktiven MAUI-/Graphics-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/ParzellenPage.cs` die fehlenden aktiven MAUI-/ApplicationModel-/System-Usings ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-/Supabase-Pfade geÃ¤ndert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil die drei Zielseiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf `KGV.Maui/Pages/MyProfilePage.cs`, `KGV.Maui/Pages/HomeSectionDetailPage.cs` und `KGV.Maui/Pages/ParzellenPage.cs` blieb nach dem Fix unauffÃ¤llig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grÃ¼n
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/NebenmitgliedPage.cs`
    - `KGV.Maui/Pages/UserManagementPage.cs`
    - `KGV.Maui/Pages/RfidEinrichtenPage.cs`
    - `KGV.Maui/Pages/MemberGardensPage.cs`
    - `KGV.Maui/Pages/RfidScanWorkflowPage.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 â€“ MAUI-Compileblock fÃ¼r `HomeManagementPage` und `MeineDatenPage` gezielt bereinigt

- Vor dem Block erneut nur den realen Repo-/Log-/Fehlerstand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Maui/Pages/HomeManagementPage.cs`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - gezielte Dateifehler fÃ¼r beide MAUI-Seiten
  - `dotnet build KGV.Maui/KGV.Maui.csproj`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor der Korrektur:
  - beide betroffenen Dateien liegen im aktuellen Stand als code-only MAUI-Seiten vor; aktive `.xaml`-GegenstÃ¼cke zu `HomeManagementPage` bzw. `MeineDatenPage` wurden im direkten Pfad nicht gefunden
  - die Compilefehler in beiden Dateien entstanden nicht aus Fachlogik, sondern aus fehlenden/falschen MAUI-NamespacebezÃ¼gen
  - konkret fehlten die aktiven TypauflÃ¶sungen u. a. fÃ¼r `ContentPage`, `IQueryAttributable`, `View`, `Border`, `VerticalStackLayout`, `Picker`, `Button`, `Label`, `CheckBox`, `Switch`, `Thickness`, `Keyboard`, `LineBreakMode` und `TextAlignment`
- Umsetzung bewusst klein und nur auf die Compile-Ursachen begrenzt:
  - in `KGV.Maui/Pages/HomeManagementPage.cs` die fehlenden MAUI-/System-Usings ergÃ¤nzt
  - in `KGV.Maui/Pages/MeineDatenPage.xaml.cs` die fehlenden MAUI-/System-Usings ergÃ¤nzt
  - keine Fachlogik, keine Navigation, keine Rechte- oder CRUD-Pfade verÃ¤ndert
  - keine XAML-/Code-Behind-Umbauten gestartet, weil beide betroffenen Seiten im aktiven Stand code-only sind
- Technische Verifikation:
  - `get_errors` auf `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs` blieb nach dem Fix unauffÃ¤llig
  - der gezielte Seitenblock ist damit dateibezogen compile-seitig bereinigt
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grÃ¼n
  - der Gesamtbuild scheitert weiter blockfremd an gleichartigen bereits vorhandenen MAUI-Namespace-/Controlfehlern, u. a. in:
    - `KGV.Maui/Pages/MyProfilePage.cs`
    - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
    - `KGV.Maui/Pages/ParzellenPage.cs`
  - diese Fehler wurden in diesem kleinen Compileblock bewusst nur dokumentiert und nicht mit umgebaut

## 2026-03-26 â€“ MAUI-Arbeitseinsatz-/Busy-Block dokumentarisch sauber abgeschlossen, nur Blockdateien fÃ¼r Commit eingegrenzt

- Vor dem Abschlusslauf erneut nur den realen Git-/Log-/Blockstand geprÃ¼ft:
  - Git-Status Ã¼ber den im aktuellen Visual-Studio-Setup vorhandenen Git-Pfad `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund im Abschlusslauf:
  - im aktuellen Arbeitsbaum liegen zusÃ¤tzlich viele blockfremde offene Dateien, u. a. WPF-, Supabase- und Artefaktpfade
  - im direkt betroffenen Blockpfad selbst war kein neuer Fachumbau mehr nÃ¶tig; der bereits umgesetzte Fixstand ist konsistent
  - `CreateArbeitseinsatzAsync(...)` setzt fÃ¼r neue ArbeitseinsÃ¤tze `CreatedAt` und `UpdatedAt` explizit, `UpdateArbeitseinsatzAsync(...)` zieht `UpdatedAt` beim Speichern nach
  - die Busy-/Ladehinweise im direkt betroffenen MAUI-Pfad werden frÃ¼h sichtbar gesetzt und die direkt betroffenen Buttons/Eingaben wÃ¤hrend Laden/Speichern deaktiviert
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` ist im aktuellen aktiven `HomePage`-Pfad weiterhin vorhanden; dafÃ¼r war kein weiterer Umbau nÃ¶tig
- In diesem Abschlusslauf bewusst nur dokumentiert und nicht neu umgebaut:
  - keine neue Architektur
  - keine weitere UI-/Fachlogik auÃŸerhalb der bereits angepassten Blockdateien
  - insbesondere `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und `KGV.Maui/Pages/HomeManagementPage.cs` nur als blockfremde Buildursachen benannt
- Technische Verifikation:
  - `get_errors` auf den direkt betroffenen Blockdateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der aktuelle Lauf scheitert weiterhin blockfremd breit, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese Fehler wurden im Abschlusslauf ausdrÃ¼cklich nur dokumentiert und nicht repariert
- Git-/Commit-Einordnung dieses Abschlusslaufs:
  - gestaged werden bewusst nur die echten Blockdateien dieses Arbeitseinsatz-/Busy-Abschlusses
  - blockfremde offene WPF-/Supabase-/Artefaktdateien bleiben ausdrÃ¼cklich auÃŸerhalb des Commits

## 2026-03-26 â€“ MAUI-Arbeitseinsatz-Create repariert, Busy-State im direkten Produktivpfad geschÃ¤rft und Home-Schnellzugriff geprÃ¼ft

- Vor dem kleinen Bugfix-Block erneut nur den realen MAUI-/Core-/Infrastructure-Stand geprÃ¼ft:
  - `KGV_Fortschrittslog_ausfuehrlich.md`
  - `DEV_LOG.md`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Interfaces/ISupabaseService.cs`
  - `KGV.Core/Models/ArbeitseinsatzRecord.cs`
  - `KGV.Maui/Pages/ArbeitseinsaetzeEditorPage.cs`
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - ergÃ¤nzend den genauen Suchlauf auf `.Result` und `.Wait(` in den direkt betroffenen MAUI-Dateien
- WPF wurde bewusst nicht angefasst.
- Ehrlicher Befund vor Umsetzung:
  - der Fehler beim Neuanlegen eines Arbeitseinsatzes war im Produktivpfad klar im Service lokalisierbar
  - `CreateArbeitseinsatzAsync(...)` baute beim Insert zwar einen neuen `ArbeitseinsatzRecord`, setzte aber `created_at` und `updated_at` nicht
  - dadurch lief der Insert weiter in den DB-Constraint `23502` auf `arbeitseinsatz.created_at`
  - im MAUI-Pfad lagen keine direkten `.Result`-/`.Wait()`-Blockierer in den betroffenen Dateien vor, aber Busy-Texte konnten im direkten Home-/Detail-/Editorpfad zu spÃ¤t sichtbar werden
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` ist im aktuellen aktiven `HomePage`-Pfad vorhanden; ein fachlicher Wiederherstellungsumbau war dafÃ¼r nicht nÃ¶tig
- Umsetzung bewusst klein und nur in den direkt betroffenen Pfaden:
  - `CreateArbeitseinsatzAsync(...)` setzt beim Insert jetzt `CreatedAt` und `UpdatedAt` explizit auf `DateTime.UtcNow`
  - `UpdateArbeitseinsatzAsync(...)` setzt beim Speichern zusÃ¤tzlich `UpdatedAt` auf `DateTime.UtcNow`
  - der RÃ¼ckladepfad nach dem Insert wurde analog zum vorherigen Termin-Fix auf `Titel` + `Datum` eingegrenzt statt die komplette Tabelle unfiltriert neu zu laden
  - `ArbeitseinsaetzeEditorPage` zeigt Busy-Texte fÃ¼r Laden/Speichern jetzt zuverlÃ¤ssig im selben Produktivpfad an, deaktiviert die Eingaben wÃ¤hrenddessen und gibt nach `Speichern` sauber zurÃ¼ck in die bestehende Arbeitseinsatz-Ãœbersicht `management_workassignments`
  - `HomePage` setzt den Ladehinweis vor dem Reload sichtbar, deaktiviert die direkt betroffenen Aktionsbuttons wÃ¤hrenddessen und fÃ¤ngt Fehler im Home-Reloadpfad sauber sichtbar ab
  - `HomeSectionDetailPage` gibt dem Renderzyklus bei Laden/Speichern/LÃ¶schen im direkt betroffenen Detailpfad kurz Raum, damit Busy-/Ladetexte vor den Serviceaufrufen sichtbar werden
  - der Home-Schnellzugriff `Arbeitsstunde erfassen` wurde geprÃ¼ft und ist im aktuellen aktiven Home-Pfad weiterhin vorhanden
- Technische Verifikation:
  - der exakte Suchlauf auf `.Result|.Wait(` in den direkt betroffenen MAUI-Dateien blieb leer
  - `get_errors` auf den direkt betroffenen Dateien blieb vor dem Abschlusslauf unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheitert weiter blockfremd breit an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese bestehenden Buildfehler wurden in diesem kleinen Arbeitseinsatz-/Busy-Block bewusst nicht repariert

## 2026-03-26 â€“ MAUI-Home-/Detail-/Editor-Block sauber abgeschlossen, Logs nachgezogen und Buildstand ehrlich dokumentiert

- Vor dem Abschlusslauf erneut nur den realen MAUI-/Log-/Git-Stand geprÃ¼ft:
  - Git-Status Ã¼ber den installierten Git-Pfad `C:\Program Files\Git\cmd\git.exe`
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
  - der frÃ¼here Neu-Sentinel `entryId=0` wurde in den betroffenen Editorpfaden bereinigt; neue DatensÃ¤tze laufen jetzt Ã¼ber `null`/kein `entryId` statt Ã¼ber eine kÃ¼nstliche `0`
  - Admin/Vorstand erhalten auf denselben mobilen Produktivseiten zusÃ¤tzliche Aktionen `Neu`, `Bearbeiten`, `LÃ¶schen` statt eines neuen Sonderpfads
  - Delete-Pfade fÃ¼r `Termine`, `ArbeitseinsÃ¤tze` und `Bekanntmachungen` wurden produktiv an `ISupabaseService` / `SupabaseService` angebunden
  - Busy-/Lade-/Speichertexte wurden im direkt betroffenen Pfad ergÃ¤nzt, insbesondere auf:
    - `HomePage`
    - `MyArbeitsstundenPage`
    - `ArbeitsstundenEditorPage`
    - `TermineEditorPage`
    - `BekanntmachungEditorPage`
    - `ArbeitseinsaetzeEditorPage`
    - `HomeSectionDetailPage`
    - `ManagementOverviewPageBase`
  - betroffene Seiten planen LadevorgÃ¤nge jetzt nicht mehr sofort blockierend im `OnAppearing`, sondern mit kleinen `_loadScheduled`-/Busy-Guards nach dem ersten Renderzyklus
  - im direkt betroffenen Pfad wurden keine `.Result`-/`.Wait()`-Nutzungen mehr gefunden
- Technische Verifikation im Abschlusslauf:
  - erneuter `git status --short` Ã¼ber den echten Git-Pfad war vor der Logpflege sauber
  - erneuter Suchlauf auf `entryId=0|.Result|.Wait(` in den betroffenen MAUI-Pfaden blieb leer
  - `get_errors` auf den direkt geÃ¤nderten Blockdateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` bleibt im Workspace weiterhin nicht grÃ¼n
  - der aktuelle Build scheitert weiterhin blockfremd breit an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in:
    - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
    - `KGV.Maui/Pages/HomeManagementPage.cs`
  - diese blockfremden Fehler wurden im Abschlusslauf bewusst nur dokumentiert und nicht mehr umgebaut

## 2026-03-26 â€“ MAUI-Terminanlage-RÃ¼ckladepfad nach Insert stabilisiert

- Vor dem kleinen Block erneut nur den realen MAUI-/Service-Iststand fÃ¼r den gemeldeten Terminfehler geprÃ¼ft:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - aktuelles Debug-Output
- Ehrlicher Befund:
  - der Insert selbst lief durch
  - im Debug-Log stand aber weiterhin `CreateTerminAsync created termin (null)`
  - Ursache war damit nicht mehr der DB-Constraint, sondern der RÃ¼ckladepfad nach dem Insert
  - `CreateTerminAsync` lud nach dem Speichern die komplette Terminliste unfiltriert neu und suchte den gerade erzeugten Datensatz danach per In-Memory-Match wieder heraus
  - das war unnÃ¶tig fragil
- Umsetzung bewusst klein:
  - den Reload nach `Insert(...)` auf `Titel` + `Datum` eingegrenzt
  - danach weiter den bestpassenden Datensatz per bestehendem Match + hÃ¶chste `Id` gewÃ¤hlt
- Technische Verifikation:
  - `get_errors` auf `KGV.Infrastructure/Services/SupabaseService.cs` blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` lief im aktuellen Block ohne neue dateibezogene Fehler an

## 2026-03-26 â€“ MAUI-Terminanlage repariert und Launcher-Icon wieder auf PNG festgezogen

- Vor dem kleinen Block erneut nur den realen MAUI-Iststand fÃ¼r den gemeldeten Fehler geprÃ¼ft:
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Core/Models/TerminRecord.cs`
  - `KGV.Maui/Pages/TermineEditorPage.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- Ehrlicher Befund:
  - der Fehler beim Anlegen eines neuen Termins war im Log bereits klar sichtbar:
    - `CreateTerminAsync failed`
    - DB-Fehler `null value in column "created_at" of relation "termin" violates not-null constraint`
  - `TerminRecord` enthÃ¤lt `CreatedAt` und `UpdatedAt`
  - `CreateTerminAsync` setzte diese Felder beim Insert aber nicht
  - der aktive Launcher-Icon-Pfad stand nach dem letzten Block wieder auf `Resources/AppIcon/appicon.svg`
  - genau das passte zum gemeldeten Befund, dass das Starticon trotz LÃ¶schen von `bin`/`obj` und Neuinstallation weiterhin nicht stimmt
- Umsetzung bewusst klein und nur in den betroffenen Produktivpfaden:
  - `CreateTerminAsync` setzt beim Insert jetzt `CreatedAt` und `UpdatedAt` auf `DateTime.UtcNow`
  - `UpdateTerminAsync` setzt `UpdatedAt` beim Speichern ebenfalls auf `DateTime.UtcNow`
  - `KGV.Maui.csproj` zieht den aktiven `MauiIcon`-Pfad wieder auf das vorhandene PNG zurÃ¼ck:
    - `Resources/AppIcon/appicon.png`
  - die Splash-Konfiguration bleibt weiter auf der SVG
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Dateien blieb unauffÃ¤llig
  - `get_tests` fÃ¼r `Project=KGV.Maui` lieferte weiterhin keine passenden Tests
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Typ-/Namespacefehlern, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Navigations-ANR entschÃ¤rft, Home-Ladepfad entkoppelt und Launcher-Icon auf SVG vereinheitlicht

- Vor dem kleinen Block erneut nur den realen MAUI-Iststand, die aktiven Navigationsseiten und die Android-/Icon-Dateien geprÃ¼ft:
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/Pages/MyArbeitsstundenPage.cs`
  - `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Infrastructure/Services/SupabaseService.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
- Ehrlicher Befund:
  - CPU-Profiling lieÃŸ sich im aktuellen Setup nicht direkt starten
  - die Debug-/Laufzeitlogs zeigten den HÃ¤nger trotzdem klar:
    - sehr viele `Skipped frames`
    - `Davey! duration=22560ms`
    - Home-LadevorgÃ¤nge parallel zu Navigations-/UI-Phasen
    - zusÃ¤tzliche Supabase-/DNS-Fehler im Home-Pfad (`Unable to resolve host ...supabase.co`)
  - in den aktiven Seiten wurde Laden weiterhin direkt im `OnAppearing` bzw. beim Navigieren gestartet
  - `HomeViewModel.InitializeAsync()` lud den Home-Ãœberblick bei gleichem Kontext immer wieder neu
  - `GetHomeOverviewAsync(...)` arbeitete die Home-Sektionen seriell ab, wodurch Netzwerkprobleme die Wartezeit unnÃ¶tig verlÃ¤ngerten
  - Android meldete zusÃ¤tzlich wiederholt `OnBackInvokedCallback is not enabled`
  - das Launcher-Icon lief zuletzt Ã¼ber einen PNG-Pfad, wÃ¤hrend der Splash parallel weiter die SVG verwendete
- Umsetzung bewusst klein und nur im aktiven MAUI-/Home-/Android-Pfad:
  - `HomePage`, `MyArbeitsstundenPage` und `ArbeitsstundenEditorPage` stoÃŸen ihr Laden jetzt erst per `Dispatcher` nach dem ersten Render-Zyklus an statt direkt im Navigationsfenster zu awaiten
  - doppelte Ladeplanung auf denselben Seiten Ã¼ber kleine `_loadScheduled`-/`_isLoading`-Guards abgefangen
  - Listen-Navigation in `HomePage` und `MyArbeitsstundenPage` gegen Reentrancy/Doppeltaps abgesichert
  - `HomeViewModel.InitializeAsync()` cached den zuletzt geladenen Kontext und lÃ¤dt bei unverÃ¤ndertem Rolle-/Mitgliedskontext nicht erneut
  - `KGV.Infrastructure/Services/SupabaseService.cs` lÃ¤dt die Home-Teilbereiche jetzt parallel statt seriell
  - `AndroidManifest.xml` ergÃ¤nzt um `android:enableOnBackInvokedCallback="true"`
  - `KGV.Maui.csproj` auf eine einheitliche SVG-Quelle fÃ¼r `MauiIcon` und `MauiSplashScreen` umgestellt
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Dateien blieb unauffÃ¤llig
  - `get_tests` fÃ¼r `Project=KGV.Maui` lieferte keine passenden Tests
  - `dotnet build KGV.Maui/KGV.Maui.csproj` weiterhin nicht erfolgreich
  - der erste Lauf scheiterte blockfremd u. a. an vorhandenen `InitializeComponent`-Fehlern in `KGV.Maui/App.xaml.cs` und `KGV.Maui/Pages/MemberSearchPage.xaml.cs`
  - der zweite Lauf scheiterte weiterhin breit blockfremd an bereits vorhandenen MAUI-Typ-/Namespacefehlern, u. a. in `KGV.Maui/Pages/BekanntmachungEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Loginlogo auf korrektes KGV-Bild umgestellt, Launcher-Icon unverÃ¤ndert gelassen

- Vor dem kleinen Block erneut nur den MAUI-Logo-/Bildpfad und gezielt diese Dateien geprÃ¼ft:
  - `KGV.Maui/Pages/LoginPage.xaml.cs`
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Resources/Images/*`
  - `KGV.Maui/Resources/AppIcon/*`
- Ehrlicher Befund:
  - die Loginseite lud aktuell weiterhin `kgv_logo.svg`
  - damit wurde in der UI das falsche grÃ¼ne Personen-/Splash-Logo angezeigt statt des eigentlichen KGV-Bilds
  - das Launcher-Icon war nicht das Problem; der aktive `MauiIcon`-Pfad blieb korrekt auf `Resources/AppIcon/appicon.png`
  - ein normales UI-PNG unter `Resources/Images` fehlte bislang
- Umsetzung bewusst klein und nur fÃ¼rs Login-/UI-Logo:
  - aus `Resources/AppIcon/appicon.png` ein normales UI-Bild unter `KGV.Maui/Resources/Images/kgv_logo.png` bereitgestellt
  - `KGV.Maui.csproj` erweitert, damit `Resources/Images/*.png` als `MauiImage` eingebunden werden
  - das alte falsche `Resources/Images/kgv_logo.svg` entfernt, damit kein doppelter Ausgabename `kgv_logo` mehr entsteht
  - `LoginPage` vom falschen `kgv_logo.svg` auf `kgv_logo.png` umgestellt
  - Launcher-Icon-/Manifest-/Android-Iconlogik bewusst nicht verÃ¤ndert
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Dateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - ein direkt benachbarter Resizetizer-Duplikatfehler um `kgv_logo` wurde in diesem Block noch mit bereinigt
  - der Lauf scheiterte danach weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/BekanntmachungEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs`, `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und `KGV.Maui/Pages/TermineEditorPage.cs`

## 2026-03-26 â€“ MAUI-Arbeitsstundenpfad nach Flyout-Umbau stabilisiert

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den Fortschrittslog und gezielt diese aktiven Dateien geprÃ¼ft:
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
  - genau deshalb war `//workhours` auf der Editorseite nach `Abbrechen` bzw. `Speichern` fÃ¼r Admin/Vorstand kein gÃ¼ltiger universeller RÃ¼ckpfad mehr
  - zusÃ¤tzlich war `MyArbeitsstundenPage` selbst noch nicht als Detailroute registriert, also gab es keinen sauberen Fallback auÃŸerhalb sichtbarer Shell-Roots
  - der Home-Schnellzugriff fÃ¼r `Arbeitsstunde erfassen` war fÃ¼r Admin/Vorstand noch an den eigenen Nutzerkontext statt an den ausgewÃ¤hlten Mitgliedskontext gebunden
- Umsetzung bewusst klein und nur in MAUI:
  - `MyArbeitsstundenPage` als vorhandene Detailroute registriert
  - `ShellNavigationHelper` um eine kleine PrÃ¼fung erweitert, ob eine sichtbare Shell-Content-Route aktuell vorhanden ist
  - `ArbeitsstundenEditorPage` verwendet fÃ¼r `Abbrechen` und Save-Erfolg jetzt einen kontextabhÃ¤ngigen gÃ¼ltigen RÃ¼ckpfad:
    - `//member_workhours`, wenn der Admin-/Mitgliedskontextpfad sichtbar ist
    - `//workhours`, wenn der User-Root sichtbar ist
    - sonst Fallback auf die registrierte Detailroute `MyArbeitsstundenPage`
  - `HomeViewModel.CanCreateWorkHoursEntry` fÃ¼r Admin/Vorstand an den vorhandenen ausgewÃ¤hlten `MemberContextState` gekoppelt; User bleiben auf dem eigenen Mitgliedskontext
- Ergebnis fÃ¼r den Pfad:
  - kein universeller ungÃ¼ltiger `//workhours`-RÃ¼cksprung mehr
  - Liste und Editor bleiben auf vorhandenen Produktivseiten
  - `Abbrechen` und Save-Erfolg fÃ¼hren auf einen gÃ¼ltigen mobilen Arbeitsstundenpfad zurÃ¼ck
  - Admin/Vorstand laufen Ã¼ber den ausgewÃ¤hlten Mitgliedskontext, User Ã¼ber den eigenen
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Arbeitsstunden-Dateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Android-AppIcon final auf genau einen aktiven PNG-Pfad festgezogen

- Vor dem kleinen Block erneut nur den MAUI-Icon-Pfad und gezielt diese Dateien geprÃ¼ft:
  - `KGV.Maui/KGV.Maui.csproj`
  - `KGV.Maui/Resources/AppIcon/*`
  - `KGV.Maui/Platforms/Android/AndroidManifest.xml`
  - `KGV.Maui/Platforms/Android/Resources/*`
- Ehrlicher Befund:
  - im `csproj` existierte bereits genau ein expliziter `MauiIcon`-Eintrag auf `Resources/AppIcon/appicon.png`
  - parallel lag aber weiterhin `Resources/AppIcon/appicon.svg` im selben Ordner und wurde nicht nur fÃ¼r Splash genutzt, sondern zusÃ¤tzlich noch als normales `MauiImage` unter `Resources/Images/kgv_logo.svg` verlinkt; der Launcher-Override war damit zwar nicht direkt aktiv, der Altpfad blieb aber unnÃ¶tig doppelt verdrahtet
  - `AndroidManifest.xml` enthÃ¤lt weiterhin weder `android:icon` noch `android:roundIcon`
  - unter `Platforms/Android/Resources/*` existieren im Workspace keine manuellen `mipmap`-/`drawable`-Icon-Reste, die das MAUI-AppIcon Ã¼berschreiben
- Umsetzung bewusst klein und nur fÃ¼rs AppIcon:
  - in `KGV.Maui.csproj` zusÃ¤tzlich explizit `MauiIcon Remove="Resources\AppIcon\*.svg"` gesetzt
  - die zusÃ¤tzliche `MauiImage`-Verlinkung der `AppIcon`-SVG wurde entfernt
  - stattdessen liegt das allgemeine Bild-Asset jetzt separat unter `KGV.Maui/Resources/Images/kgv_logo.svg`
  - damit bleibt fÃ¼r den Launcher-Pfad genau ein aktives `MauiIcon` Ã¼brig:
    - `Resources/AppIcon/appicon.png`
  - Splash-/Fach-/Login-/Shell-Logik bewusst nicht verÃ¤ndert
- Android-Test-/Bereinigungshinweis:
  - `bin` und `obj` lÃ¶schen
  - App auf dem GerÃ¤t deinstallieren
  - neu bauen und neu installieren, weil Android Launcher-Icons cached
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - ein direkt benachbarter Android-AAPT-Fehler aus der doppelten `appicon.svg`-Nutzung als `MauiImage` wurde in diesem Block mit bereinigt
  - der Lauf scheiterte danach weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Login-Root-Switch auf Android stabilisiert und AppIcon-Pfad bereinigt

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den Fortschrittslog und gezielt diese aktiven Dateien geprÃ¼ft:
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
  - der verzÃ¶gerte Android-Seitenwechsel entstand im vorhandenen Root-Switch-Pfad dadurch, dass `App.SwitchToCurrentRootAsync()` auf Android ein neues `Window` Ã¶ffnete und das alte danach schloss
  - dadurch blieb der sichtbare Wechsel instabil, obwohl Login und Shell-Aufbau bereits erfolgreich waren
  - zusÃ¤tzlich existierte in `LoginPage` noch ein zweiter Fallback-Pfad, der bei Bedarf selbst `AdminShell`/`UserShell` erzeugte und direkt auf `window.Page` setzte
  - beim AppIcon war der aktive `MauiIcon`-Pfad bereits auf `Resources/AppIcon/appicon.png` gesetzt, aber lokal lag die tatsÃ¤chliche PNG-Datei noch nur im Arbeitsbaum; auÃŸerdem nutzte der Splash zusÃ¤tzlich dieselbe `AppIcon`-SVG, was den aktiven Pfad unnÃ¶tig unklar machte
  - `AndroidManifest.xml` Ã¼berschreibt das App-Symbol nicht
- Umsetzung bewusst klein und nur in MAUI:
  - `App.SwitchToCurrentRootAsync()` baut den Ziel-Root jetzt auf dem `MainThread` und setzt ihn direkt auf das aktive vorhandene Fenster
  - der Android-spezifische Ersatzfenster-/`OpenWindow`-/`CloseWindow`-Pfad wurde entfernt
  - `LoginPage` verwendet fÃ¼r den erfolgreichen Loginabschluss jetzt nur noch den einen zentralen App-Root-Wechselpfad statt zusÃ¤tzlich Shells lokal als Fallback zu setzen
  - `KGV.Maui.csproj` bleibt beim aktiven AppIcon-Pfad `Resources/AppIcon/appicon.png`
  - der Splash bleibt bewusst auf dem vorhandenen buildbaren `Resources/AppIcon/appicon.svg`, weil die vorhandene `Resources/Splash/splash.svg` in diesem Workspace aktuell keinen gÃ¼ltigen Android-`drawable/splash` erzeugte
  - das tatsÃ¤chlich referenzierte Logo-Asset `KGV.Maui/Resources/AppIcon/appicon.png` wird mit in den Commit genommen
- Android-Test-/Bereinigungshinweis:
  - fÃ¼r einen sauberen Icon-Test `bin`/`obj` neu erzeugen
  - App auf dem GerÃ¤t deinstallieren
  - APK/App neu installieren, weil Android Launcher-Icons aggressiv cached
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Root-Switch-Dateien blieb unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiter blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Shell-Route `management_workassignments` nach Flyout-Umbau repariert

- Vor dem kleinen Bugfix-Block erneut den realen MAUI-Iststand, die Shell-/Route-Dateien und die betroffenen `GoToAsync(...)`-Aufrufer geprÃ¼ft.
- Ehrlicher Befund:
  - `management_workassignments` war nach dem Flyout-Umbau kein Shell-Root-Ziel mehr, wurde aber weiter als absolutes `//management_workassignments` angesprungen
  - in `ShellRouteRegistrar` war kein expliziter Alias `management_workassignments` registriert; vorhanden war nur `nameof(ArbeitseinsaetzeManagementPage)`
  - direkt benachbart lag dasselbe Muster auch bei `management_announcements` und `management_appointments` vor
- Umsetzung bewusst klein und nur in MAUI:
  - in `ShellRouteRegistrar` Alias-Routen fÃ¼r `management_announcements`, `management_appointments` und `management_workassignments` registriert
  - absolute Aufrufe auf nicht mehr rootfÃ¤hige Ziele auf relative Navigation umgestellt:
    - `HomePage`
    - `HomeSectionDetailPage`
    - `HomeManagementPage`
    - `ArbeitseinsaetzeEditorPage`
  - echte Root-Ziele mit `//` bewusst nicht verÃ¤ndert
- Technische Verifikation:
  - `get_errors` auf den geÃ¤nderten Dateien blieb fÃ¼r diesen Block unauffÃ¤llig
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte weiterhin blockfremd an bereits vorhandenen MAUI-Control-/Namespacefehlern u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Flyout auf kleine Hauptnavigation reduziert und Mitgliedskontext sauber eingehÃ¤ngt

- Vor dem kleinen Block erneut Repo-/Arbeitsbaumstand, aktiven Fortschrittslog und die real verwendeten MAUI-Shell-/Mitgliedskontextdateien geprÃ¼ft.
- Ehrlicher Befund:
  - `AdminShell` und `UserShell` enthielten noch deutlich mehr Flyout-EintrÃ¤ge als fÃ¼r den mobilen Hauptweg gewÃ¼nscht
  - die gewÃ¼nschten Mitglieds-Unterpunkte waren im Admin-Flyout bisher immer sichtbar statt erst nach Mitgliedsauswahl
  - `MemberSearchPage` setzte den Mitgliedskontext bereits, aktualisierte das Flyout danach aber noch nicht gezielt
  - der vorhandene mobile Arbeitsstundenpfad war bereits vorhanden, nutzte im Adminkontext aber noch nur den globalen eigenen Mitgliedsbezug statt den ausgewÃ¤hlten Mitgliedskontext
- Umsetzung bewusst klein und nur in MAUI:
  - `AdminShell` auf die gewÃ¼nschten Hauptpunkte reduziert:
    - `Startseite`
    - `Ablesen`
    - `Parzellenverwaltung`
    - `Arbeitsstunden freigeben`
    - `Export`
    - `Mitgliedersuche`
  - direkte Verwaltungs-/Profil-/Mehrfach-Flyoutpunkte entfernt
  - Mitglieds-Unterpunkte im Flyout jetzt optisch eingerÃ¼ckt mit `â†³`
  - Admin/Vorstand sehen diese Unterpunkte nur noch bei vorhandenem `SelectedMember`
  - `UserShell` zeigt fÃ¼r den sofort aktiven Eigenkontext direkt die eingerÃ¼ckten Unterpunkte:
    - `â†³ Stammdaten`
    - `â†³ Nebenmitglied`
    - `â†³ GÃ¤rten des Mitglieds`
    - `â†³ Arbeitsstunden`
  - `MemberSearchPage` baut das aktuelle Shell-MenÃ¼ nach Mitgliedsauswahl sofort neu auf, bevor in `Stammdaten` navigiert wird
  - `MyArbeitsstundenPage` und `ArbeitsstundenEditorPage` berÃ¼cksichtigen im Admin-/Vorstandskontext jetzt den ausgewÃ¤hlten `MemberContextState`, damit der Unterpunkt `â†³ Arbeitsstunden` auf dem vorhandenen mobilen Produktivpfad fÃ¼r das ausgewÃ¤hlte Mitglied arbeitet
  - `ShellNavigationHelper` minimal um den expliziten MAUI-Shell-Namespace geschÃ¤rft
- Technische Verifikation:
  - `get_tests` fÃ¼r `KGV.Tests` ergab weiterhin keine passenden aktiven Tests fÃ¼r diesen kleinen MAUI-Navigationsblock
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace weiterhin nicht erfolgreich
  - der Lauf scheiterte blockfremd weiterhin an bereits bestehenden breiten MAUI-Control-/Namespacefehlern u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/HomeManagementPage.cs` und `KGV.Maui/Pages/MeineDatenPage.xaml.cs`

## 2026-03-26 â€“ MAUI-Login-UI korrigiert und Arbeitsstunden-Schnellzugriff auf Home ergÃ¤nzt

- Vor dem kleinen Block erneut den realen MAUI-Iststand, den aktuellen Fortschrittslog und die aktiven MAUI-Dateien fÃ¼r `LoginPage`, `HomePage`, `MyArbeitsstundenPage`, `ArbeitsstundenEditorPage`, `ShellRouteRegistrar`, `AdminShell`, `UserShell` und `HomeViewModel` geprÃ¼ft.
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `KGV.Maui/Pages/LoginPage.xaml.cs` ist der aktive Loginpfad und baute die komplette UI derzeit rein in C# auf
  - die Passwortsichtbarkeit lief dort noch Ã¼ber einen separaten Button `Passwort anzeigen` unterhalb des Passwortfelds
  - der Button `Anmelden` stand im aktuellen Mobilfluss noch hinter den sekundÃ¤ren Aktionen statt direkt unter dem Passwortfeld
  - der vorhandene Erfassungspfad fÃ¼r Arbeitsstunden war bereits belastbar vorhanden Ã¼ber `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs` mit Navigation `ArbeitsstundenEditorPage?entryId=0`
  - auf `KGV.Maui/Pages/HomePage.xaml.cs` zeigte der Bereich `Arbeitsstunden` bisher nur Ãœberblick/Hinweise, aber noch keinen direkten Erfassungsbutton
- Den Korrekturblock deshalb bewusst klein und nur im aktiven MAUI-Pfad umgesetzt:
  - auf `LoginPage` das Passwortfeld auf einen eingebetteten Auge-/Auge-zu-Toggle umgestellt statt eigenem Sichtbarkeitsbutton unter dem Feld
  - `Anmelden` direkt unter das Passwortfeld gezogen
  - die sekundÃ¤ren Aktionen `Einladung / Erstlogin-Code anfordern` und `Passwort vergessen` unterhalb von `Anmelden` angeordnet
  - die bestehende Login-/OTP-/Passwort-Logik fachlich unverÃ¤ndert gelassen
  - auf `HomePage` im Bereich `Arbeitsstunden` einen klaren Button `Arbeitsstunde erfassen` ergÃ¤nzt
  - der neue Home-Schnellzugriff nutzt keinen neuen Dummy-Pfad, sondern den bereits vorhandenen Editorpfad `ArbeitsstundenEditorPage?entryId=0`
  - die Sichtbarkeit des Schnellzugriffs an den vorhandenen eigenen Mitgliedskontext gekoppelt Ã¼ber `HomeViewModel`, damit kein unnÃ¶tiger toter Pfad fÃ¼r Kontexte ohne `MitgliedId` angeboten wird
- Technische Verifikation:
  - `get_tests` ergab im aktuellen Workspace keine passenden aktiven Tests fÃ¼r `KGV.Tests`
  - `dotnet build KGV.Maui/KGV.Maui.csproj` im aktuellen Workspace nicht erfolgreich
  - der Lauf scheiterte weiterhin an breit vorhandenen MAUI-Typ-/Namespacefehlern im aktuellen Stand, u. a. in `KGV.Maui/Pages/TermineEditorPage.cs`, `KGV.Maui/Pages/MeineDatenPage.xaml.cs` und im selben Lauf auch an generischen Control-Typen in `KGV.Maui/Pages/HomePage.xaml.cs`; der Block ist damit aktuell nicht sauber buildisoliert validierbar
  - `git` war in der aktuellen Terminalumgebung nicht verfÃ¼gbar (`GIT_NOT_FOUND`), deshalb konnte der geforderte Commit-/Push-Abschluss in diesem Lauf nicht technisch ausgefÃ¼hrt werden

## 2026-03-25 â€“ MAUI-Launcher-Icon auf vorhandenes Vereinslogo-PNG umgestellt

- Vor dem kleinen Block erneut Repo-/Arbeitsbaumstand, Fortschrittslog und aktiven MAUI-AppIcon-Pfad geprÃ¼ft.
- Ehrlicher Befund:
  - `AndroidManifest.xml` Ã¼berschreibt das Launcher-Icon nicht
  - `KGV.Maui.csproj` nutzte noch `appicon.svg` als `MauiIcon`
  - das vorhandene `KGV.Maui/Resources/AppIcon/appicon.png` lag bereits im aktiven Repo
- Umsetzung bewusst klein gehalten:
  - nur den aktiven `MauiIcon`-Pfad auf `appicon.png` umgestellt
  - Splash, Shell und Ã¼brige MAUI-Pfade unverÃ¤ndert gelassen

## 2026-03-25 â€“ Finaler Git-Abschluss: Artefakt-/FotoUploadTest-Block sauber auf echte Blockdateien eingegrenzt

- Vor dem Git-Abschluss erneut den realen Repo-/Arbeitsbaumstand, den Fortschrittslog und die aktiven Referenzen des WPF-`FotoUploadTest`-Pfads geprÃ¼ft.
- Ehrlicher Befund:
  - weiterhin viele blockfremde offene Dateien im Arbeitsbaum
  - aktiver WPF-`FotoUploadTest`-Pfad klar belegt
  - MAUI-Variante weiterhin Archivmaterial
  - `.github/copilot-instructions.md` nicht blockzugehÃ¶rig genug fÃ¼r diesen Commit
- FÃ¼r den Abschluss nur diese Dateiklassen in den Commit genommen:
  - aktive WPF-/Core-/Infrastructure-`FotoUploadTest`-Dateien
  - notwendige kleine MAUI-Restbereinigung in `MauiProgram`
  - aktive Supabase-Begleitdateien
  - echte Archivdateien des MAUI-/Altpfads
  - beide Logdateien
- AusdrÃ¼cklich nicht aufgenommen:
  - blockfremde WPF-XAML-/SQL-Ã„nderungen
  - `.vscode/`
  - `_AI_DB_EXPORT/`
  - `_secrets/`
  - lokale Installer/APKs/Asset-Dubletten und sonstige Exporte

## 2026-03-25 â€“ Block Artefaktbereinigung: aktive `FotoUploadTest`-/Supabase-Dateien Ã¼bernommen, lokale Restartefakte getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und die unversionierten/blockfremden Artefakte geprÃ¼ft.
- Ehrlicher Befund im aktuellen Repo-Stand:
  - das WPF-`FotoUploadTest` war bereits aktiv verdrahtet, die zugehÃ¶rigen Dateien lagen aber noch unversioniert lokal vor
  - die MAUI-Variante war aktuell nicht produktiv angebunden und damit kein aktiver Hauptpfad
  - zusÃ¤tzliche Supabase-Begleitdateien fÃ¼r `kgv-upload-photo` waren legitim, aber noch unversioniert
  - weitere lokale Artefakte wie Binaries, Secrets, Exporte, Dubletten und Tooldateien waren nicht belastbar Teil der aktiven Repo-Struktur
- Den Korrekturblock deshalb bewusst klein und sauber getrennt umgesetzt:
  - aktive WPF-`FotoUploadTest`-Dateien und ihre Core-/Infrastructure-Verdrahtung ordentlich ins Repo Ã¼bernommen
  - lose MAUI-Registrierung des ungenutzten `FotoUploadTestViewModel` entfernt
  - MAUI-`FotoUploadTest`-Dateien in `_Archiv/KGV.Maui/...` verschoben
  - alten Nebenpfad `supabase/kgv-upload-photo/index.ts` nach `_Archiv/supabase/...` verschoben
  - `supabase/.gitignore`, `.npmrc` und `deno.json` des aktiven Functionpfads sauber ins Repo Ã¼bernommen
  - verbleibende lokale Archivkandidaten in `_Archiv/LOCAL_ARTIFACT_ARCHIVE_CANDIDATES.md` zusammengefasst
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-25 â€“ Block 5B-final: AbschlussprÃ¼fung, Restentkopplung und Repo-Bereinigung fÃ¼r den aktuellen MAUI/WPF-Zyklus abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, `README.md`, `KGV.slnx`, `KGV.Tests` und `_Archiv/KGV.Tests` geprÃ¼ft.
- FÃ¼r Block `5B-final` gezielt geprÃ¼ft:
  - WPF-Hauptpfad `HomeView`
  - aktive MAUI-Hauptpfade fÃ¼r Home, Detail, Verwaltung, Arbeitsstunden und Shell
  - Restreferenzen auf `HomeManagementPage`
  - Testprojekt-Einordnung im Root und im Archiv
- Ehrlicher Befund im aktuellen Repo-Stand:
  - die groÃŸen MAUI-FachblÃ¶cke waren bereits fachlich getrennt und buildfÃ¤hig abgeschlossen
  - letzter echter aktiver AltÃ¼bergang Ã¼ber `HomeManagementPage` lag noch in `HomeSectionDetailPage` fÃ¼r `Termine` und `Bekanntmachungen`
  - `ManagementOverviewPageBase` trug zusÃ¤tzlich noch einen ungenutzten Standard-Fallback auf `HomeManagementPage`
  - `KGV.Tests` im Root und `_Archiv/KGV.Tests` waren dokumentarisch noch nicht sauber genug gegeneinander eingeordnet
- Den Abschlussblock deshalb bewusst klein nur auf belegbare Restpunkte begrenzt:
  - `HomeSectionDetailPage` navigiert bei `Termine` jetzt direkt auf `//management_appointments`
  - `HomeSectionDetailPage` navigiert bei `Bekanntmachungen` jetzt direkt auf `//management_announcements`
  - `ManagementOverviewPageBase` wurde vom alten impliziten `HomeManagementPage`-Fallback entkoppelt und verlangt jetzt explizite Produktivpfade der abgeleiteten Seiten
  - `README.md` ordnet `KGV.Tests` im Root jetzt klar als vorhandenes, aber nicht in `KGV.slnx`/CI eingebundenes Testprojekt ein; `_Archiv/KGV.Tests` bleibt die archivierte Ã¤ltere Kopie
  - `HomeManagementPage` bleibt bewusst als technischer KompatibilitÃ¤tspfad im Repo und wird nicht auf Verdacht gelÃ¶scht
- Abschlussbild nach dem Block:
  - `Home` bleibt Ãœbersicht
  - `Verwaltung` bleibt getrennt
  - `Editor` bleibt `Editor`
  - `Detail` bleibt `Detail`
  - keine unnÃ¶tigen aktiven Mischpfade mehr im MAUI-Hauptweg
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich

## 2026-03-25 â€“ Block 5A-final: Home-VerwaltungszugÃ¤nge in MAUI direkt auf Produktivseiten gezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `5A-final` gezielt geprÃ¼ft:
  - `HomePage`
  - `HomeManagementPage`
  - vorhandene Managementseiten und Shell-Routen fÃ¼r `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die echten Produktivseiten waren bereits vorhanden
  - `ArbeitseinsÃ¤tze` gingen von `Home` schon direkt auf die Managementseite
  - `Termine` und `Bekanntmachungen` liefen von `Home` aber noch unnÃ¶tig Ã¼ber `HomeManagementPage`
- Den Korrekturblock deshalb bewusst klein und nur in der Navigation umgesetzt:
  - `Termine bearbeiten` auf `Home` zieht jetzt direkt auf `//management_appointments`
  - `Bekanntmachungen bearbeiten` auf `Home` zieht jetzt direkt auf `//management_announcements`
  - `HomeManagementPage` bleibt nur technischer Rest-/KompatibilitÃ¤tspfad und wird nicht weiter als normaler Home-Hauptweg genutzt
  - Sichtbarkeit fÃ¼r Admin/Vorstand blieb unverÃ¤ndert
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt Desktop-Views nutzt MAUI direkte Shell-Routen auf die vorhandenen Mobilseiten bei gleicher fachlicher Trennung
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 â€“ Block 4B2-finish: Termine-Nutzerpfad in MAUI sichtbar mobil abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `4B2-finish` gezielt geprÃ¼ft:
  - `HomeSectionDetailPage`
  - `HomePage`
  - `TermineUserState`
  - `HomeContextState`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `4B1` hatte den datensatzweisen Termine-Zustand schon vorbereitet
  - sichtbar war die Footer-Navigation bislang aber noch nur fÃ¼r ArbeitseinsÃ¤tze aktiv
  - der Terminpfad brauchte noch den sichtbaren mobilen Abschluss `â† x/y â†’`
- Den Korrekturblock deshalb bewusst klein nur im sichtbaren Termine-Detailpfad umgesetzt:
  - `TermineUserState` um reine Navigationshilfen ergÃ¤nzt
  - `HomeSectionDetailPage` zeigt jetzt fÃ¼r Termine unten `â† x/y â†’`
  - BlÃ¤ttern lÃ¤uft direkt Ã¼ber `TermineUserState`
  - Termin-Detail bleibt rein lesend; kein `Bearbeiten` im Nutzerpfad
  - Fallback auf Einzeltermin aus `HomeContextState` bleibt robust erhalten
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt Desktop-Klickpfad mit Listenfokus nutzt MAUI mobil bewusst einen datensatzweisen Footer
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 â€“ Block 4B1: Termine-Nutzerpfad in MAUI auf datensatzweisen Zustand vorbereitet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `4B1` gezielt geprÃ¼ft:
  - `HomePage`
  - `HomeSectionDetailPage`
  - `HomeContextState`
  - `HomeViewModel`
  - `SupabaseService`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - Termine liefen bereits korrekt Ã¼ber `HomePage` in `HomeSectionDetailPage`
  - der Klickpfad Ã¼bergab aber noch nur den Einzeltermin statt einer datensatzweisen Terminbasis
  - ein eigener kleiner Termine-Nutzerzustand fehlte noch
- Den Korrekturblock deshalb bewusst klein und nur als Grundlage umgesetzt:
  - neuer `TermineUserState` fÃ¼r Terminliste plus aktuelle Position
  - `HomePage` seedet beim Termin-Klick jetzt die gesamte chronologische Terminliste plus aktuelle Auswahl
  - `HomeSectionDetailPage` nutzt fÃ¼r Termine jetzt bevorzugt den aktuellen Eintrag aus diesem Zustand
  - Shared-Sortierung fÃ¼r Home-Termine auf `Datum`, `Startzeit`, `Endzeit`, `Titel`, `Id` abgesichert
  - keine sichtbare Pfeilnavigation in diesem Block vorgezogen
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - `4B1` bereitet nur den datensatzweisen Mobilfluss vor; die sichtbare UI-SchÃ¤rfung bleibt bewusst `4B2`
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 â€“ Block 4A-small: Admin-MenÃ¼ und Mein Profil in MAUI-Shell ruhiger geordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `4A-small` gezielt geprÃ¼ft:
  - `AdminShell`
  - `UserShell`
  - aktuelle Sichtbarkeit und Einordnung von `Benutzerverwaltung` und `Mein Profil`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `Benutzerverwaltung` hing im Admin-Flyout noch fachlich unter `Mitglieder`
  - `Mein Profil` war im Admin-Flyout noch nicht so ruhig als eigener persÃ¶nlicher Bereich eingeordnet wie im User-Flyout
- Den Korrekturblock deshalb bewusst klein nur in der Shell-Struktur umgesetzt:
  - `Benutzerverwaltung` in `AdminShell` auf `Admin-MenÃ¼ Â· Benutzerverwaltung` gezogen
  - Sichtbarkeit unverÃ¤ndert nur fÃ¼r `Admin`
  - `Mein Profil` in `AdminShell` auf `Mein Bereich Â· Mein Profil` gezogen
  - keine neuen Seiten, keine neuen Routen und keine neue Rollenlogik eingefÃ¼hrt
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt Desktop-UntermenÃ¼s bleibt MAUI bei einer flachen Flyout-Struktur mit fachlichen PrÃ¤fixen
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-25 â€“ Block 3B3-finish: ArbeitseinsÃ¤tze in MAUI auf Ãœberblick, Editor und Datensatzfluss fachlich abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `3B3-finish` gezielt geprÃ¼ft:
  - MAUI: `ArbeitseinsaetzeManagementPage`, `ArbeitseinsaetzeEditorPage`, `HomeManagementPage`, `HomePage`, `HomeSectionDetailPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
  - Shared/Produktivpfade: `SupabaseService`, `HomeDashboardItems`, `StartseiteArbeitseinsatzRecord`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die Ãœbersicht war noch nicht konsequent chronologisch und ruhig genug
  - der Arbeitseinsatz-Editorpfad war noch nicht vollstÃ¤ndig produktiv verdrahtet
  - Verwaltungs- und User-Datensatzfluss mit Pfeilnavigation fehlten noch
  - `HomeManagementPage` war fÃ¼r ArbeitseinsÃ¤tze noch Rest-Hauptpfad
- Den Korrekturblock deshalb bewusst klein und nur auf `ArbeitseinsÃ¤tze` umgesetzt:
  - chronologische Verwaltungssortierung nach `Datum`, `Startzeit`, `Endzeit`, `Titel`
  - eigener produktiver Editorpfad `ArbeitseinsaetzeEditorPage` fÃ¼r `Neu` und `Bearbeiten`
  - mobiler Verwaltungs-Datensatzfluss im Editor mit `â† x/y â†’` und Save-vor-Wechsel
  - mobiler User-Datensatzfluss in `HomeSectionDetailPage` mit `â† x/y â†’` sowie `Anmelden` / `Abmelden` je Datensatz
  - `HomeManagementPage` fÃ¼r ArbeitseinsÃ¤tze auf Ãœbersicht/Editor umgeleitet
  - vorhandene RPC-/Shared-Pfade fÃ¼r `Anmelden` und `Abmelden` weiterverwendet
  - `ShellRouteRegistrar` und `MauiProgram` minimal ergÃ¤nzt
  - neue kleine Zustandsdienste fÃ¼r Verwaltungs- und Usernavigation ergÃ¤nzt
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt Desktop-Nebeneinander von Liste und Editor nutzt MAUI mobil getrennte Seiten plus kompakte Datensatznavigation
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - blockeigene Warnungen bereinigt; verbleibend nur 4 Ã¤ltere Warnungen in `HomeManagementPage.cs`

## 2026-03-25 â€“ Block 3B2: Termine in MAUI auf Ãœberblick plus eigenen Editorpfad getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `3B2` gezielt geprÃ¼ft:
  - WPF: `TermineVerwaltungEditorView`
  - MAUI: `TermineManagementPage`, `HomeManagementPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `TermineManagementPage` war zwar bereits ruhige Ãœbersicht, Ã¶ffnete `Neu`/`Bearbeiten` aber noch in den technischen Restpfad `HomeManagementPage`
  - damit blieb fÃ¼r Termine der eigentliche mobile Produktiv-Editor weiter an einer Mischseite hÃ¤ngen
  - zusÃ¤tzlich lief die Ãœbersicht noch nicht ausdrÃ¼cklich auf der geforderten chronologischen Hauptsortierung nach Datum/Uhrzeit
- Den Korrekturblock deshalb bewusst klein und nur auf `Termine` umgesetzt:
  - neuer eigener Editorpfad `TermineEditorPage` ergÃ¤nzt
  - `TermineManagementPage` Ã¶ffnet `Neu` und `Bearbeiten` jetzt direkt in diesen neuen Editorpfad
  - die Ãœbersicht wurde ausdrÃ¼cklich auf chronologische Reihenfolge nach `Datum`, `Startzeit`, `Endzeit`, `Titel` gezogen
  - der neue Editor trennt Ãœbersicht und Bearbeitung sichtbar auf zwei Seiten
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
  - `HomeManagementPage` wurde fÃ¼r den Bereich `Termine` als Haupteditor entkoppelt und leitet jetzt bei diesem Bereich auf Ãœbersicht bzw. neuen Editorpfad weiter
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt WPF-Liste plus Editor in derselben Breite nutzt MAUI mobil bewusst getrennte Seiten `Ãœbersicht` und `Editor`
  - fÃ¼r die touch-taugliche Zeit-/Datumsbearbeitung nutzt der mobile Editor stabile `DatePicker`-/`TimePicker`-Felder mit klaren Aktivschaltern fÃ¼r optionale Zeit- und Sichtbarkeitsangaben statt einer Desktop-artigen freien Zeiteingabe in derselben FormularflÃ¤che
- Bewusst nicht gemacht:
  - keine Ã„nderungen an `Bekanntmachungen`
  - keine Ã„nderungen an `ArbeitseinsÃ¤tze`
  - keine Ã„nderungen an Home, Stammdaten, GÃ¤rten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus dem neuen Termine-Editorpfad

## 2026-03-25 â€“ Block 3B1: Bekanntmachungen in MAUI auf Ãœberblick plus eigenen Editorpfad getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `3B1` gezielt geprÃ¼ft:
  - WPF: `BekanntmachungenVerwaltungEditorView`
  - MAUI: `BekanntmachungenManagementPage`, `ManagementOverviewPageBase`, `HomeManagementPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `BekanntmachungenManagementPage` war zwar bereits ruhige Ãœbersicht, Ã¶ffnete `Neu`/`Bearbeiten` aber noch in den technischen Restpfad `HomeManagementPage`
  - damit blieb fÃ¼r Bekanntmachungen der eigentliche mobile Produktiv-Editor weiter an einer Mischseite hÃ¤ngen
  - die bestehende HTML-Bearbeitung war mobil noch nicht in einen eigenen getrennten Editorpfad gezogen
- Den Korrekturblock deshalb bewusst klein und nur auf `Bekanntmachungen` umgesetzt:
  - neuer eigener Editorpfad `BekanntmachungEditorPage` ergÃ¤nzt
  - `BekanntmachungenManagementPage` Ã¶ffnet `Neu` und `Bearbeiten` jetzt direkt in diesen neuen Editorpfad
  - `ManagementOverviewPageBase` dafÃ¼r minimal von technischem Fortsetzungspfad auf Ã¼berschreibbare Ã–ffnen-/Neu-Aktionen erweitert
  - der neue Editor trennt Ãœbersicht und Bearbeitung sichtbar auf zwei Seiten
  - HTML-Bearbeitung bleibt fachlich erhalten Ã¼ber echten HTML-Quelltexteditor, Snippet-Buttons und mobile Vorschau per `WebView`
  - fachlich relevante Felder aus dem bestehenden Shared-/WPF-Pfad bleiben im Editor erhalten:
    - `Titel`
    - `InhaltHtml`
    - `Sichtbar ab`
    - `Sichtbar bis`
    - `SortOrder`
    - `Aktiv`
  - Speichern bleibt am Ende des Formulars
  - `HomeManagementPage` wurde fÃ¼r den Bereich `Bekanntmachungen` als Haupteditor entkoppelt und leitet jetzt bei diesem Bereich auf Ãœbersicht bzw. neuen Editorpfad weiter
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt WPF-Liste plus Editor in derselben Breite nutzt MAUI mobil bewusst getrennte Seiten `Ãœbersicht` und `Editor`
  - die HTML-Bearbeitung bleibt dabei als Quelltext + Snippets + Vorschau erhalten, weil dies mobil belastbarer und risikoÃ¤rmer ist als ein neuer Richtext-Sonderbau
- Bewusst nicht gemacht:
  - keine Ã„nderungen an `Termine`
  - keine Ã„nderungen an `ArbeitseinsÃ¤tze`
  - keine Ã„nderungen an Home, Stammdaten, GÃ¤rten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` sowie bestehenden blockfremden Pfaden und stammen nicht aus dem neuen Bekanntmachungen-Editorpfad

## 2026-03-25 â€“ Block 3A: Verwaltungsbereiche in MAUI fachlich auf drei eigene Ãœbersichtsseiten entflochten

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `3A` gezielt geprÃ¼ft:
  - WPF: `BekanntmachungenVerwaltungEditorView`, `TermineVerwaltungEditorView`, `ArbeitseinsaetzeVerwaltungEditorView`
  - MAUI: `HomeManagementPage`, `AdminShell`, `ShellRouteRegistrar`, `MauiProgram`, bestehende Home-/VerwaltungszugÃ¤nge
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - die drei Bereiche `Bekanntmachungen`, `Termine` und `ArbeitseinsÃ¤tze` liefen mobil weiterhin primÃ¤r Ã¼ber die Sammelseite `HomeManagementPage`
  - fachliche Bereichstrennung und direkte Verwaltungs-Einstiege waren gegenÃ¼ber WPF damit noch zu schwach
  - `HomeManagementPage` blieb damit faktisch noch die Produktiv-Sammelseite statt nur technischer Restpfad
- Den Korrekturblock deshalb bewusst klein und nur auf Bereichsstruktur/Ãœbersicht umgesetzt:
  - neue ruhige MAUI-Ãœbersichtsseiten ergÃ¤nzt:
    - `BekanntmachungenManagementPage`
    - `TermineManagementPage`
    - `ArbeitseinsaetzeManagementPage`
  - gemeinsame kleine Basis `ManagementOverviewPageBase` ergÃ¤nzt, damit alle drei Seiten nur ihren jeweils eigenen Bereich zeigen
  - jede Seite zeigt nur:
    - eigene Liste vorhandener DatensÃ¤tze
    - klaren Einstieg `Neu`
    - keinen Bereichswechsel innerhalb derselben Seite
  - `AdminShell` zeigt die drei Verwaltungsbereiche jetzt direkt auf diese drei neuen Hauptseiten statt auf die Sammelseite
  - `ShellRouteRegistrar` und `MauiProgram` minimal fÃ¼r die drei neuen Seiten ergÃ¤nzt
  - `HomeManagementPage` nicht riskant gelÃ¶scht, sondern entkoppelt:
    - neuer technischer Restpfad fÃ¼r Fortsetzung innerhalb genau eines Bereichs
    - kann jetzt optional mit fixiertem Bereich, ausgewÃ¤hltem Datensatz oder `Neu` geÃ¶ffnet werden
    - Bereichsauswahl wird in diesem technischen Fortsetzungspfad bei fixierter Herkunft ausgeblendet
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt WPF-Editor+Liste pro Bereich ist mobil in `3A` zunÃ¤chst nur die ruhige BereichsÃ¼bersicht als eigener Hauptweg nachgezogen
  - der technische Fortsetzungspfad Ã¼ber `HomeManagementPage` bleibt bis `3B` bewusst bestehen, um keinen riskanten GroÃŸumbau der Editorlogik vorzuziehen
- Bewusst nicht gemacht:
  - keine eigentlichen Editorseiten pro Datensatz in diesem Block
  - keine HTML-/Zeit-/Validierungs-Feinschliffe
  - keine Ã„nderungen an Home, Stammdaten, GÃ¤rten, Parzellen, Ablesen, Arbeitsstunden, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden und stammen nicht aus der neuen Bereichstrennung selbst

## 2026-03-25 â€“ Block 2B: Arbeitsstunden-Freigabe in MAUI auf ruhige Ãœbersicht und Einzeldatensatz-PrÃ¼fung umgestellt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `2B` gezielt geprÃ¼ft:
  - WPF: `ArbeitsstundenPruefungView`, ergÃ¤nzend `ArbeitsstundenView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `ArbeitsstundenReviewPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `ArbeitsstundenReviewPage` zeigte offene PrÃ¼ffÃ¤lle noch mit direkter `Genehmigen`-/`Ablehnen`-Aktion direkt in der Listenansicht
  - damit blieb mobil weiter eine Mischseite aus Ãœbersicht und Entscheidung statt eines klaren PrÃ¼fpfads pro Datensatz
  - eine WPF-nÃ¤here mobile Datensatznavigation fÃ¼r den PrÃ¼fkontext fehlte noch vollstÃ¤ndig
- Den Korrekturblock deshalb bewusst klein und nur im Reviewpfad umgesetzt:
  - `ArbeitsstundenReviewPage` auf ruhige Ãœbersicht offener PrÃ¼ffÃ¤lle zurÃ¼ckgezogen
  - Antippen eines offenen Falls Ã¶ffnet jetzt eine eigene mobile PrÃ¼fdetailseite
  - neue Seite `ArbeitsstundenReviewDetailPage` ergÃ¤nzt
  - neuer Zustand `ArbeitsstundenReviewState` hÃ¤lt die aktuelle offene PrÃ¼ffallliste und die mobile Datensatzposition
  - PrÃ¼fdetail zeigt genau einen Datensatz mit Mitglied, Datum, Stunden, Art der Arbeit und Status-/Anmerkungsfeld
  - unten mobile Datensatznavigation nur mit Pfeil links, Positionsanzeige `x/y` mittig und Pfeil rechts
  - bei offenen Ã„nderungen wird vor dem BlÃ¤ttern zuerst gespeichert; bei Fehler bleibt die Seite stehen
  - `Freigeben`, `Ablehnen` und `Speichern` laufen weiter Ã¼ber dieselben bestehenden Shared-Servicepfade
  - entschiedene/freigegebene bzw. abgelehnte FÃ¤lle werden nicht mehr als normaler offener PrÃ¼ffall behandelt
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt einer WPF-PrÃ¼ftabelle nutzt MAUI mobil bewusst Ãœbersicht plus Einzeldatensatz-PrÃ¼fung auf eigener Seite
  - diese Abweichung ist fachlich gewollt, weil sie auf kleineren Bildschirmen den PrÃ¼fkontext klarer und touch-tauglicher hÃ¤lt
- Bewusst nicht gemacht:
  - keine Ã„nderung an `MyArbeitsstundenPage`
  - keine Ã„nderung am normalen Nutzer-Erfassungs-/Bearbeitungspfad
  - keine Ã„nderung an Home, Stammdaten, GÃ¤rten, Parzellen, Ablesen, Verwaltung, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 â€“ Block 2A: Arbeitsstunden-Nutzerpfad in MAUI auf Ãœbersicht / Erfassen / Bearbeiten getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die WPF-Referenzpfade und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `2A` gezielt geprÃ¼ft:
  - WPF: `ArbeitsstundenView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `MyArbeitsstundenPage`, Routing/DI in `ShellRouteRegistrar` und `MauiProgram`
- Ehrlicher Befund im aktuellen MAUI-Stand:
  - `MyArbeitsstundenPage` mischte Ãœbersicht und Editor noch auf einer Sammelseite
  - Antippen eines Eintrags fÃ¼llte dasselbe Formular unten wieder auf
  - bestÃ¤tigte/freigegebene EintrÃ¤ge waren im Nutzerpfad noch nicht klar genug als gesperrte Nur-Ansicht getrennt
- Den Korrekturblock deshalb bewusst klein und nur im normalen Nutzerpfad umgesetzt:
  - `MyArbeitsstundenPage` auf ruhige Ãœbersicht zurÃ¼ckgezogen
  - `Soll`, `Geleistet`, `Offen` bleiben oben sichtbar
  - klarer Einstieg `Neu erfassen`
  - vorhandene EintrÃ¤ge Ã¶ffnen jetzt einen eigenen mobilen Arbeitsstunden-Pfad statt das Ãœbersichtsformular wiederzuverwenden
  - neue Seite `ArbeitsstundenEditorPage` ergÃ¤nzt
  - dieselbe Seite trÃ¤gt mobil die drei fachlichen Rollen:
    - `Arbeitsstunde erfassen`
    - `Arbeitsstunde bearbeiten`
    - `Arbeitsstunde ansehen`
  - unbestÃ¤tigte EintrÃ¤ge Ã¶ffnen bearbeitbar
  - freigegebene EintrÃ¤ge Ã¶ffnen nur noch lesbar mit klarem Sperrhinweis
  - `Speichern` und `Abbrechen` bleiben am Ende des Eingabeformulars
  - nach `Speichern` oder `Abbrechen` geht der Nutzer klar zurÃ¼ck zur Ãœbersicht `Meine Arbeitsstunden`
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt WPF-Dialog/Fenster verwendet MAUI eine eigene mobile Seite fÃ¼r Erfassen/Bearbeiten/Ansicht
  - diese Abweichung ist fachlich gewollt, weil sie auf kleineren Bildschirmen Ãœbersicht und Editor sauber trennt
- Bewusst nicht gemacht:
  - keine Ã„nderung an `ArbeitsstundenReviewPage`
  - keine Ã„nderung an Freigabe-/PrÃ¼flogik fÃ¼r Admin/Vorstand
  - keine Ã„nderung an Home, Stammdaten, GÃ¤rten, Parzellen, Ablesen, Verwaltung, Export oder Auth
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - `get_tests` fÃ¼r `KGV.Tests` ergab keine passenden TestfÃ¤lle fÃ¼r diesen Block
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 â€“ Block 1B2: NFC-Hauptweg in MAUI technisch abgeschlossen und validiert

- Vor dem Abschlussblock erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block `1B2` gezielt den bereits begonnenen NFC-/Workflow-Stand geprÃ¼ft:
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
  - MAUI-Build fÃ¼r `KGV.Maui/KGV.Maui.csproj` ausgefÃ¼hrt
  - nur blockbezogene Buildfehler im neuen NFC-/Workflow-Pfad korrigiert
  - Android-Namespace-/Context-/Settings-Konflikte im NFC-Service bereinigt
  - nicht verfÃ¼gbare `Expander`-Verwendung in den MAUI-Seiten durch einfache ruhige Sektionen ersetzt
  - neue blockeigene Nullability-Warnung im RFID-Workflow entfernt
- Fachlicher Endstand von `1B2` nach dem Abschluss:
  - NFC ist im mobilen operativen RFID-/Ablesen-Bereich jetzt der Hauptweg
  - gelesene UID wird weiter in die bestehenden echten Produktivpfade eingespeist
  - `Parzelle + Medium` steht als fachlicher Ersatzweg bereit, wenn NFC nicht verfÃ¼gbar oder deaktiviert ist
  - manuelle UID-Eingabe bleibt nur noch als technischer Notfallweg sichtbar
- Erlaubte mobile Abweichung ausdrÃ¼cklich festgehalten:
  - GerÃ¤te ohne NFC bzw. mit deaktiviertem NFC erhalten mobil den fachlichen Ersatzweg Ã¼ber `Parzelle + Medium`
  - bei `RFID einrichten` bleibt UID-Tippen nur als technischer Notfallweg bestehen, weil ohne bekannte Tag-UID keine echte RFID-Zuordnung gespeichert werden kann
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen liegen weiter in `HomeManagementPage.cs` und stammen nicht aus diesem Block

## 2026-03-25 â€“ Block 1B1: Parzellen-Details in MAUI auf ruhige Leseseite zurÃ¼ckgezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 1B1 gezielt geprÃ¼ft:
  - MAUI: `ParzellenPage`, `ParzellenViewModel`
  - WPF-Referenz: `GartenStromView`, `GartenWasserView`, fachlich ergÃ¤nzend `AblesenOverviewView`
- Ehrlicher Befund im aktuellen Stand:
  - `ParzellenPage` enthielt zwar bereits einen ruhigeren Bereich `Aktuelle Ablesedaten`, stellte diesen aber noch kartenartig dar
  - zusÃ¤tzlich lag dort mit `Zum Ablesen-Bereich` noch eine direkte operative Aktion mitten im Detailblock
  - im mitgliedsgebundenen Detailkontext wirkte die Seite damit noch zu sehr wie eine Sammelarbeitsseite statt wie eine ruhige Leseseite
- Den Korrekturblock bewusst klein und nur im Detailpfad umgesetzt:
  - `Aktuelle Ablesedaten` sichtbar von kartenartiger Darstellung auf eine nÃ¼chterne tabellenÃ¤hnliche Zeilenstruktur umgebaut
  - Kopfzeile fÃ¼r `Medium`, `Letzter Stand`, `Datum`, `Foto`
  - Zeilen zeigen jetzt nur die ruhigen Kerndaten statt einer dekorativen Kartenoptik
  - der direkte Button `Zum Ablesen-Bereich` aus dem Detailblock entfernt
  - der Hinweis auf den operativen Bereich `Ablesen` bleibt nur noch als ruhiger Einordnungstext bestehen
  - der Verwaltungs-/Zuordnungsblock wird im mitgliedsgebundenen Detailkontext nicht mehr als operative ArbeitsflÃ¤che gezeigt; der globale Verwaltungsweg bleibt unverÃ¤ndert erhalten
- Warum dieser kleine Block fachlich sinnvoll ist:
  - er trennt das Lesen/Einordnen im Parzellen-Detail klarer vom operativen Arbeiten im Bereich `Ablesen`
  - die Detailseite wirkt im Mitglieds-/Gartenkontext sichtbar ruhiger
  - bestehende operative Fachpfade wurden nicht neu gebaut oder verlagert, sondern nur sauber aus dem Detailblock herausgezogen
- Bewusst nicht gemacht:
  - kein Umbau von `AblesenOverviewPage`
  - keine Ã„nderung an `RfidEinrichtenPage`, `RfidScanWorkflowPage` oder `ZaehlerwechselPage`
  - keine neue Parzellen-/Garten-Architektur
  - keine Ã„nderung an Stammdaten, GÃ¤rten des Mitglieds, Arbeitsstunden, Verwaltung, Export oder Auth
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Parzellen-Details wirken ruhiger
  - aktuelle Ablesedaten sind tabellenÃ¤hnlich sichtbar
  - direkte operative Ablesen-Aktion ist aus dem Detailblock entfernt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte bestehende Warnungen auÃŸerhalb dieses Blocks bleiben bestehen

## 2026-03-25 â€“ Block 1A: Stammdaten und GÃ¤rten des Mitglieds in MAUI WPF-nah getrennt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 1A gezielt geprÃ¼ft:
  - MAUI: `MeineDatenPage`, `AdminShell`, `UserShell`, `MauiProgram`, `ShellRouteRegistrar`, `MemberContextState`, `ParzellenContextState`
  - WPF-Referenz: `MemberDetailView`, `StammdatenView`, `MemberParzellenSection`
- Ehrlicher Befund im aktuellen Stand:
  - `MeineDatenPage` blieb trotz vorheriger Feinschliffe fachlich noch Ã¼berladen, weil der Garten-/Parzellenbereich weiterhin direkt auf der Stammdatenseite eingebettet war
  - `Mitgliedsdokumente` waren bereits unten separat platziert, die Seite wirkte durch den eingebetteten Gartenblock aber weiter zu gemischt
  - `Bearbeiten` war vorhanden, die fachliche Trennung `Stammdaten` vs. `GÃ¤rten des Mitglieds` blieb mobil aber noch schwÃ¤cher als in WPF
- Den Korrekturblock bewusst nur auf Stammdaten vs. GÃ¤rten begrenzt:
  - keine Parzellen-Detailanpassung
  - keine Ablesen-/NFC-/ZÃ¤hlerwechsel-Ã„nderung
  - keine neue Fachlogik auÃŸerhalb des bereits vorhandenen Mitglieds-/Parzellenkontexts
- Auf der Stammdatenseite umgesetzt:
  - den eingebetteten Bereich `GÃ¤rten` aus `MeineDatenPage` entfernt
  - `MeineDatenPage` bleibt damit fachlich ruhiger auf Stammdaten-/Mitgliedskontext- und Verwaltungsinformationen fokussiert
  - `Bearbeiten` bleibt als sichtbarer Einstieg erhalten
  - `Mitgliedsdokumente` bleiben als eigener Button ganz unten auf der Seite
- Neuen klaren Mitgliedsbereich `GÃ¤rten` ergÃ¤nzt:
  - neue Seite `MemberGardensPage`
  - zeigt die Garten-/Parzellenzuordnungen des ausgewÃ¤hlten Mitglieds in einer eigenen Seite
  - vorhandener ehrlicher Einstieg `Parzelle zuordnen` bleibt dort erhalten
  - Antippen einer Zuordnung nutzt weiter den bestehenden Mitgliedskontext-/Parzellenpfad statt einer neuen Schattenarchitektur
- Navigation/Shell minimal mitgezogen:
  - `AdminShell`: `Mitglieder Â· GÃ¤rten des Mitglieds` zeigt jetzt die neue `MemberGardensPage`
  - `UserShell` erhielt zusÃ¤tzlich `Mein Bereich Â· GÃ¤rten`, damit der bisherige eigene Gartenzugang nicht still entfÃ¤llt
  - `MauiProgram` und `ShellRouteRegistrar` wurden minimal fÃ¼r die neue Seite bzw. den bestehenden Parzellenpfad ergÃ¤nzt
- Erlaubte mobile Abweichung ausdrÃ¼cklich benannt:
  - statt eines innerhalb derselben FlÃ¤che dauerhaft eingebetteten Desktop-Unterbereichs wurde mobil bewusst eine eigene Seite `GÃ¤rten` ergÃ¤nzt
  - diese Abweichung ist fachlich gewollt und sinnvoll, weil sie Stammdaten ruhiger hÃ¤lt und den Mitgliedsgartenbereich klarer trennt
- Bewusst nicht gemacht:
  - kein Umbau von `ParzellenPage`
  - keine Ã„nderung an Ablesen, NFC, Strom/Wasser oder ZÃ¤hlerwechsel
  - kein neuer mobiler Parzellen-Zuordnungseditor
  - keine Ã„nderung an Export, Profil, Auth oder Arbeitsstunden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - `Stammdaten` ist ruhiger
  - `GÃ¤rten des Mitglieds` sind als eigener Bereich getrennt
  - `Mitgliedsdokumente` bleiben korrekt unten als eigener Button
  - `Bearbeiten` bleibt sichtbar
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte bestehende Warnungen auÃŸerhalb dieses Blocks bleiben bestehen

## 2026-03-24 â€“ Block 0A: MAUI-Shell-/MenÃ¼struktur WPF-nah neu geordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 0A gezielt geprÃ¼ft:
  - MAUI: `AdminShell`, `UserShell`, `MauiProgram`, `ShellNavigationHelper`
  - WPF-Referenz: fachliche Haupt-/Mitgliedsnavigation in `MainWindowViewModel`
- Ehrlicher Befund im aktuellen Stand:
  - `AdminShell` und `UserShell` enthielten zwar bereits produktive Kernziele, die fachliche Gruppierung war gegenÃ¼ber WPF aber noch zu flach und zu unscharf
  - besonders Mitgliedskontext, Parzellenverwaltung, Ablesen, Arbeitsstunden, Verwaltung und Profil lagen mobil noch nicht klar genug in einer WPF-nahen Reihenfolge und Gruppierung
  - `GÃ¤rten des Mitglieds` durfte dabei ausdrÃ¼cklich nicht mit der globalen `Parzellenverwaltung` vermischt werden
- Den Korrekturblock bewusst nur auf die Shell-Struktur begrenzt:
  - keine InhaltsÃ¤nderung an Fachseiten
  - keine neue Fachlogik
  - keine Android-/Login-/Root-Baustelle geÃ¶ffnet
- In `AdminShell` WPF-nahe fachliche Ordnung hergestellt:
  - `Startseite`
  - `Mitglieder Â· Mitgliedersuche`
  - `Mitglieder Â· Stammdaten`
  - `Mitglieder Â· Nebenmitglied`
  - `Mitglieder Â· Benutzerverwaltung` *(Admin-only)*
  - `Mitglieder Â· GÃ¤rten des Mitglieds`
  - `Parzellenverwaltung`
  - `Ablesen Â· Ablesen`
  - `Ablesen Â· RFID einrichten`
  - `Ablesen Â· FÃ¤llige ZÃ¤hler`
  - `Ablesen Â· ZÃ¤hlerwechsel`
  - `Arbeitsstunden Â· Meine Arbeitsstunden`
  - `Arbeitsstunden Â· Arbeitsstunden freigeben`
  - `Verwaltung Â· Bekanntmachungen`
  - `Verwaltung Â· Termine`
  - `Verwaltung Â· ArbeitseinsÃ¤tze`
  - `Export`
  - `Mein Profil`
- In `UserShell` die fachliche Struktur auf `Mein Bereich` umgeordnet:
  - `Startseite`
  - `Mein Bereich Â· Meine Stammdaten`
  - `Mein Bereich Â· Nebenmitglied`
  - `Mein Bereich Â· Meine Arbeitsstunden`
  - `Mein Bereich Â· Mein Profil`
- Kleine Hilfsanpassungen gezielt mitgezogen:
  - fÃ¼r `Meine Stammdaten` im User-Shell wird vor dem Ã–ffnen der vorhandene eigene Mitgliedskontext gesetzt, damit die bestehende `MeineDatenPage` WPF-nah als eigener Stammdatenpfad nutzbar bleibt
  - die drei Verwaltungsunterpunkte verwenden weiter die vorhandene `HomeManagementPage`, aber gezielt mit bestehendem Query-Parameter fÃ¼r Bereichsauswahl statt neuer Seitenarchitektur
  - der Badge-/Titelpfad fÃ¼r `Arbeitsstunden freigeben` wurde an die neue Gruppierungsbenennung angepasst
- Erzwungene kleine mobile Abweichung gegenÃ¼ber WPF ausdrÃ¼cklich benannt:
  - echtes verschachteltes Flyout mit visuellen Unterebenen bietet die Standard-MAUI-Shell ohne grÃ¶ÃŸeren Custom-Flyout-Umbau nicht belastbar an
  - deshalb wurde die WPF-nahe Unterstruktur im Flyout minimal-invasiv Ã¼ber klare fachliche GruppierungsprÃ¤fixe `Bereich Â· Unterpunkt` umgesetzt
  - das hÃ¤lt die Bereiche getrennt und adressierbar, ohne eine neue Shell-Architektur zu erÃ¶ffnen
- Bewusst nicht gemacht:
  - kein Umbau von `LoginPage`
  - kein Eingriff in `App.xaml.cs` oder Android-Rootpfade
  - keine Seitenkopf-/Hamburger-/ZurÃ¼ck-Themen dieses Blocks vorweggenommen
  - keine InhaltsÃ¤nderung an Home-, Stammdaten-, Parzellen-, Ablesen-, Export- oder Verwaltungsseiten
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Admin-/Vorstand-MenÃ¼ ist fachlich deutlich WPF-nÃ¤her gruppiert
  - Nutzer-MenÃ¼ ist auf `Mein Bereich` gebÃ¼ndelt
  - Mitgliedsbezogene GÃ¤rten und globale Parzellenverwaltung bleiben getrennt
  - keine produktiven Fachbereiche entfernt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte bestehende Warnungen auÃŸerhalb dieses Blocks bleiben bestehen

## 2026-03-24 â€“ MAUI Feinschliff fÃ¼r Stammdaten / GÃ¤rten / Parzelle / Ablesen mit kleinem UI-/Flow-Block fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, die Repo-Wahrheit und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r diesen Feinschliff gezielt geprÃ¼ft:
  - MAUI: `MeineDatenPage`, `MemberSearchPage`, `AdminShell`, `ParzellenPage`, `RfidScanWorkflowPage`, `AblesenOverviewPage`, `ShellRouteRegistrar`
  - WPF-Referenz: `MemberDetailView`, `MemberParzellenSection`, `GartenStromView`, `GartenWasserView`
- Ehrlicher Befund im aktuellen Stand:
  - auf `MeineDatenPage` hing `Mitgliedsdokumente` noch fachlich unruhig im Bereich `GÃ¤rten / Parzellen`
  - `Stammdaten aktualisieren` brachte gegenÃ¼ber dem ohnehin vorhandenen Reload beim Wiedererscheinen nur geringen Mehrwert
  - der fehlende Hamburger auf der mobilen Stammdatenseite lag nicht an einer kaputten Shell, sondern daran, dass der Admin-Mitgliedskontext bislang als gepushte Detailroute aus der Suche geÃ¶ffnet wurde
  - im Parzellen-Detailbereich waren die mobilen Strom-/Wasser-/ZÃ¤hlerwechsel-BedienblÃ¶cke zu operativ und zogen fachlich wieder in eine Seite hinein, die ruhiger read-only bleiben sollte
  - fÃ¼r NFC/RFID existiert im aktiven MAUI-Stand weiterhin keine direkte GerÃ¤teanbindung; die UI wirkte aber noch zu sehr so, als sei manuelle UID-Eingabe der Hauptweg
- Kleinen Korrekturblock deshalb nur in diesen vier Themen umgesetzt:
  - `MeineDatenPage` erhielt einen kleinen `Bearbeiten`-Einstieg
  - fÃ¼r eigene Stammdaten fÃ¼hrt er in den vorhandenen mobilen Profilpfad; fÃ¼r fremde Mitglieder wird ehrlich erklÃ¤rt, dass ein mobiler Volleditor aktuell noch fehlt
  - `Mitgliedsdokumente` wurden aus dem Gartenbereich herausgezogen und als eigener Button an das Seitenende gesetzt
  - `Stammdaten aktualisieren` wurde entfernt
  - der Bereich heiÃŸt jetzt nur noch `GÃ¤rten`
  - zusÃ¤tzlicher Button `Parzelle zuordnen` Ã¶ffnet als ehrlichen bestehenden Einstieg die vorhandene Parzellenverwaltung statt einen Fake-Flow zu bauen
  - `AdminShell` erhielt einen echten Shell-Einstieg `Stammdaten`; `MemberSearchPage` navigiert dorthin jetzt Ã¼ber den Shell-Rootpfad
  - dadurch erscheint im Admin-Stammdatenpfad wieder das Flyout-/Hamburger-MenÃ¼ statt eines reinen ZurÃ¼ck-Pfeils
  - `ParzellenPage` zeigt im Detailbereich jetzt nur noch einen ruhigen ReadOnly-Block `Aktuelle Ablesedaten`
  - sichtbar bleiben dort Medium, letzter Stand, Datum, ZÃ¤hlernummer und ein Fotobutton, wenn ein Fotopfad vorhanden ist
  - der operative Ablesen-/ZÃ¤hlerwechselpfad wurde bewusst aus dem Detailbereich herausgezogen und Ã¼ber einen klaren Ãœbergang `Zum Ablesen-Bereich` getrennt
  - `AblesenOverviewPage` und `RfidScanWorkflowPage` kommunizieren jetzt ehrlich, dass direkter NFC-/RFID-GerÃ¤tescan im aktuellen MAUI-Stand noch nicht aktiv angebunden ist
  - manuelle UID-Eingabe wird sichtbar als `Fallback` gekennzeichnet und nicht mehr wie der primÃ¤re Produktpfad dargestellt
- Warum dieser kleine Block fachlich sinnvoll ist:
  - er beruhigt genau die von auÃŸen sichtbaren UI-/Flow-SchwÃ¤chen, ohne neue Architektur zu erÃ¶ffnen
  - bestehende belastbare Shared-Servicepfade fÃ¼r Parzellen, Dokumente und RFID-Kontext bleiben unverÃ¤ndert die fachliche Grundlage
  - an fehlenden mobilen Volleditoren oder fehlender NFC-GerÃ¤teintegration wird nichts beschÃ¶nigt; stattdessen wird die UI ehrlicher und klarer
- Bewusst nicht gemacht:
  - kein neuer mobiler Volleditor fÃ¼r fremde Stammdaten
  - kein neuer Parzellen-Zuordnungs-Spezialdialog nur fÃ¼r den Mitgliedskontext
  - keine neue NFC-Architektur oder GerÃ¤teintegration
  - keine Ã„nderung an WPF-Dateien, Export, Arbeitsstunden, Home-Verwaltung oder Auth-Grundpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Stammdatenseite wirkt ruhiger
  - Mitgliedsdokumente sind separat unten erreichbar
  - GÃ¤rten sind sprachlich klarer getrennt
  - Parzellen-Detail zeigt nur noch ReadOnly-Kernkontext fÃ¼r Ablesungen
  - Ablesen-/RFID-Pfad kommuniziert jetzt ehrlich den manuellen Fallback
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte bestehende Warnungen auÃŸerhalb dieses Blocks bleiben bestehen

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 9: Altlasten / MAUI-only-Seiten / Testpfade mit kleinem MAUI-Altpfad-AufrÃ¤umblock fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 9 gezielt geprÃ¼ft:
  - WPF: `FotoUploadTestView` sowie die derzeit sichtbaren Test-/Hilfspfade im Arbeitsbaum
  - MAUI: `RoleChoicePage`, `ExitPage`, `MyProfilePage`, `AblesenFeaturePage`, Shell-Routen, produktive MenÃ¼sichtbarkeit und erkennbare Hilfs-/Altseiten
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - fachlich nÃ¶tig oder nicht
  - produktiv erreichbar oder nicht
  - Altlast oder legitimer MAUI-only-Pfad
  - Risiko beim Entfernen
  - Auswirkungen auf Navigation, Build und Rechte
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `RoleChoicePage` ist im aktiven Repo-Pfad bereits nicht mehr vorhanden und damit keine verbleibende produktive Altlast mehr
  - `MyProfilePage` ist ein legitimer produktiver MAUI-only-Pfad und bleibt sinnvoll im User-Shell-MenÃ¼ verankert
  - `AblesenFeaturePage` war im aktiven MAUI-Pfad nur noch eine ungenutzte Hilfsseite ohne produktive Route oder MenÃ¼einbindung
  - `ExitPage` war zwar noch produktiv im User-/Admin-Shell-MenÃ¼ eingehÃ¤ngt, ist mobil aber fachlich kein sinnvoller Produktpfad
  - zusÃ¤tzliche Foto-/Upload-Testpfade sind im aktuellen Arbeitsbaum sichtbar, aber teilweise lokal/unversioniert; diese wurden fÃ¼r diesen kleinen Block bewusst nicht aggressiv mitbereinigt
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - produktiver Flyout-Eintrag `Beenden` in MAUI war als Altpfad Ã¼berflÃ¼ssig
  - `ExitPage` selbst war nach Entfernen dieses MenÃ¼s ein toter MAUI-only-Pfad
  - `AblesenFeaturePage` war eine ungenutzte Hilfsseite ohne produktive Einbindung
- Kleinen Korrekturblock deshalb nur im MAUI-Altlasten-/Routing-Rahmen umgesetzt:
  - `Beenden` aus `AdminShell` entfernt
  - `Beenden` aus `UserShell` entfernt
  - `ExitPage` aus dem DI-Container entfernt
  - ungenutzte Datei `ExitPage.cs` gelÃ¶scht
  - ungenutzte Datei `AblesenFeaturePage.cs` gelÃ¶scht
- Warum dieser kleine Block fachlich sinnvoll ist:
  - der Block entfernt klare tote bzw. produktiv fragwÃ¼rdige Altpfade mit sehr geringem Risiko
  - produktive Kernpfade wie `MyProfilePage`, Login, Home, Stammdaten, Export, Ablesen und Arbeitsstunden bleiben unberÃ¼hrt
  - statt einer riskanten GroÃŸreinigung wurden nur eindeutig entkoppelte Altseiten bereinigt
- Bewusst nicht gemacht:
  - keine aggressive Bereinigung lokaler/unversionierter Testartefakte
  - keine Ã„nderung an `MyProfilePage`, weil die Seite produktiv sinnvoll bleibt
  - keine WPF-LÃ¶schung von `FotoUploadTestView`, weil dessen aktiver Produkt-/Teststatus im aktuellen Arbeitsbaum nicht belastbar genug fÃ¼r eine sichere Kleinbereinigung war
  - keine neue Navigationsarchitektur
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - produktive Navigation bleibt intakt
  - `MyProfilePage` bleibt erreichbar
  - kein produktiver MAUI-`Beenden`-Altpfad mehr im Flyout
  - keine Verschlechterung der bisher geprÃ¼ften Hauptpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - der kleine AufrÃ¤umblock bleibt buildfÃ¤hig

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 8: Auth-Sonderwege / Dialogersatz mit kleinem MAUI-Auth-Dialogersatz-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 8 gezielt geprÃ¼ft:
  - WPF: `ChangeEmailWindow`, `ResetPasswordWindow`, `DatumDialog`
  - MAUI: `LoginPage`, `MyProfilePage`, aktive Auth-/OTP-/Passwort-/MailÃ¤nderungs-Pfade sowie die relevanten Auth-Servicepfade
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der OTP-/Recovery-Unterbau ist in MAUI bereits vorhanden und lÃ¤uft produktiv Ã¼ber `IAuthService`
  - der Passwort-Reset ist mobil bereits fachlich tragfÃ¤hig im Login integriert
  - `DatumDialog` ist mobil durch vorhandene `DatePicker`-/Inline-Eingaben ausreichend implizit gelÃ¶st; dafÃ¼r war kein eigener neuer Dialogpfad nÃ¶tig
  - die grÃ¶ÃŸte kleine RestlÃ¼cke lag aktuell bei der MailÃ¤nderung auf `MyProfilePage`
  - dort war der mobile Pfad bisher nur als Folge von `DisplayPromptAsync(...)` gelÃ¶st und damit schwÃ¤cher als der vorhandene WPF-Dialogfluss
- Wichtigste kleine Restabweichung mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - die MAUI-MailÃ¤nderung bot noch keinen stabilen sichtbaren Dialogersatz mit klarer OTP-Stufe, Statusanzeige und Wiederholbarkeit
  - zusÃ¤tzlich fehlte auf der Profilseite ein kleiner direkter Passwort-Reset-Sonderweg analog zum WPF-/Login-Kontext
- Kleinen Korrekturblock deshalb nur innerhalb von Auth-Sonderwegen / Dialogersatz umgesetzt:
  - `MyProfilePage` erhielt einen echten inline Dialogersatz fÃ¼r die MailÃ¤nderung statt der bisherigen Prompt-Kette
  - sichtbar sind jetzt:
    - aktuelle E-Mail
    - neue E-Mail
    - OTP-Stufe nach Codeanforderung
    - laufende StatusrÃ¼ckmeldung direkt auf der Seite
  - der mobile Flow nutzt weiterhin dieselben bestehenden Auth-Servicepfade:
    - `RequestEmailChangeAsync(...)`
    - `VerifyEmailChangeOtpAsync(...)`
  - zusÃ¤tzlich wurde auf derselben Profilseite ein kleiner stabiler Button `Passwort-Reset senden` ergÃ¤nzt
  - dieser nutzt weiter den bestehenden produktiven Recovery-Pfad `SendPasswordResetEmailAsync(...)`
  - die eigentliche Codeeingabe und Passwort-Neusetzung bleiben bewusst im vorhandenen Login-Flow
- Warum dieser kleine Block fachlich sinnvoll ist:
  - MailÃ¤nderung war der sichtbar schwÃ¤chste mobile Sonderpfad gegenÃ¼ber WPF
  - ein inline Dialogersatz ist mobil fachlich gleichwertiger als verschachtelte Prompts, ohne ein WPF-Fenster 1:1 nachzubauen
  - der Block bleibt vollstÃ¤ndig auf bestehenden Auth-/OTP-Pfaden und baut keine Schattenlogik
- Bewusst nicht gemacht:
  - keine neue Auth-Architektur
  - kein separater neuer MAUI-Sonderseitenbaum nur fÃ¼r MailÃ¤nderung oder Reset
  - kein kÃ¼nstlicher Nachbau von `DatumDialog`, weil `DatePicker`-Inlinepfade mobil bereits ausreichend sind
  - keine Ã„nderung an Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs-, Export- oder Ablesenpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Login bleibt stabil
  - bestehende OTP-/Auth-Pfade bleiben intakt
  - MailÃ¤nderung ist mobil jetzt sichtbarer und stabiler erreichbar
  - Passwort-Reset ist aus dem Login weiterhin nutzbar und zusÃ¤tzlich im Profilkontext direkt anstoÃŸbar
  - keine Verschlechterung bereits geprÃ¼fter Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs-, Export- oder Ablesenpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte bestehende Warnungen bleiben auÃŸerhalb dieses kleinen Blocks bestehen

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 7: Ablesen / RFID / ZÃ¤hlerwechsel mit kleinem MAUI-RÃ¼ckwege-/Refresh-/Reset-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 7 gezielt geprÃ¼ft:
  - WPF: `AblesenOverviewView`, `AblesungErfassenView`, `AblesungDialog`, `RfidEinrichtenView`, `RfidScanContextView`, `FaelligeZaehlerView`, `ZaehlerwechselScanView`, `ZaehlerwechselAusbauView`, `ZaehlerwechselEinbauView`, `ZaehlerTauschDialog`
  - MAUI: `AblesenOverviewPage`, `AblesungErfassenPage`, `AblesenFeaturePage`, `RfidEinrichtenPage`, `RfidScanWorkflowPage`, `FaelligeZaehlerPage`, `ZaehlerwechselPage` sowie die zugehÃ¶rigen RFID-/Scan-ViewModels
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der MAUI-Ablesen-Bereich ist bereits erreichbar und nutzt die vorhandenen echten Shared-Servicepfade fÃ¼r RFID-Kontext, RFID-Zuordnung und fÃ¤llige ZÃ¤hler
  - `RfidEinrichtenPage` und `FaelligeZaehlerPage` sind fachlich bereits produktiv nutzbar
  - `AblesungErfassenPage` und `ZaehlerwechselPage` sind mobil derzeit â€“ analog zum aktuell sichtbaren WPF-Hauptpfad â€“ vor allem Kontext-/Einordnungsseiten und noch kein voller mobiler Endausbau von Ablesungsdialog bzw. Ausbau-/Einbau-Workflow
  - der grÃ¶ÃŸte kleine Restabstand lag damit aktuell vor allem in Flow, RÃ¼ckwegen und Wiederverwendbarkeit im mobilen Scanpfad
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - in den mobilen Unterseiten fehlte ein expliziter RÃ¼ckweg zurÃ¼ck zur `Ablesen`-Ãœbersicht
  - wiederholte RFID-PrÃ¼fungen auf derselben Seite hatten noch keinen klaren Reset-/Neustartpfad
  - `FaelligeZaehlerPage` lud bisher nur beim ersten Anzeigen und konnte beim RÃ¼ckweg veraltet bleiben
  - `RfidEinrichtenPage` initialisierte bisher ebenfalls nur einmal pro Seiteninstanz
- Kleinen Korrekturblock deshalb nur im MAUI-Ablesen-/RFID-/ZÃ¤hlerwechsel-Rahmen umgesetzt:
  - `RfidScanContextViewModel` erhielt einen kleinen `Reset()`-Pfad fÃ¼r neuen Scanbeginn ohne neue Schattenlogik
  - `RfidScanWorkflowPage` bietet jetzt zusÃ¤tzlich `Neuen Scan beginnen`
  - dieselbe Workflowbasis bietet jetzt einen expliziten Button `Zur Ablesen-Ãœbersicht`
  - `RfidEinrichtenPage` erhielt ebenfalls einen expliziten RÃ¼ckweg zur `Ablesen`-Ãœbersicht
  - `RfidEinrichtenPage` initialisiert beim Wiedererscheinen erneut und bleibt damit kontextstabiler
  - `FaelligeZaehlerPage` lÃ¤dt beim Wiedererscheinen jetzt erneut und bleibt dadurch aktueller
- Warum dieser kleine Block fachlich sinnvoll ist:
  - die zugrunde liegenden Fachpfade waren bereits vorhanden
  - der grÃ¶ÃŸte direkte Nutzwert lag deshalb in saubereren RÃ¼ckwegen und in einem wiederholbaren Scanfluss statt in vorschneller neuer Unterseitenarchitektur
  - der Block bleibt vollstÃ¤ndig auf bestehenden Shared-Services und bestehenden MAUI-Seiten
- Bewusst nicht gemacht:
  - kein neuer mobiler Vollworkflow fÃ¼r `AblesungDialog`
  - kein neuer Ausbau-/Einbau-Unterseitenbau fÃ¼r ZÃ¤hlerwechsel
  - kein Umbau von Foto-/Upload-/Cloudpfaden
  - keine Ã„nderung an Home-, Mitglieds-, Garten-, Arbeitsstunden-, Verwaltungs- oder Exportpfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Ablesen-Bereich bleibt erreichbar
  - RFID-/Scanpfade bleiben erreichbar
  - ZÃ¤hlerwechsel bleibt erreichbar
  - RÃ¼ckwege und Wiederholungsfluss im mobilen Scanpfad sind klarer
  - keine Verschlechterung bereits geprÃ¼fter Home-, Mitglieds-, Garten-, Arbeitsstunden- oder Verwaltungs-/Exportpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - der geÃ¤nderte MAUI-Pfad baut ohne neue eigene Warnungen

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 5: Arbeitsstunden komplett mit kleinem mobilen Ãœberblick-/Erfassungs-/Bearbeitungs-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 5 gezielt geprÃ¼ft:
  - WPF: `ArbeitsstundenView`, `ArbeitsstundenPruefungView`, `ArbeitsstundenErfassungView`, `ArbeitsstundenErfassungWindow`, `ArbeitsstundeDialog`
  - MAUI: `MyArbeitsstundenPage`, `ArbeitsstundenReviewPage` sowie die relevanten Shared-Servicepfade rund um Arbeitsstunden
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der mobile Arbeitsstunden-Unterbau ist bereits belastbar vorhanden
  - Erfassung und Review laufen in MAUI bereits auf denselben Shared-Servicepfaden wie in WPF
  - der grÃ¶ÃŸte kleine Restabstand lag aktuell im normalen mobilen Nutzerpfad, nicht in fehlender Basisspeicherung
  - `MyArbeitsstundenPage` blieb im aktuellen Stand gegenÃ¼ber WPF schwÃ¤cher bei Ãœbersicht, RÃ¼ckkehrstabilitÃ¤t und Bearbeitung bereits erfasster EintrÃ¤ge
  - der mobile Freigabepfad bleibt funktional vorhanden, ist gegenÃ¼ber WPF aber weiterhin deutlich einfacher
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - `MyArbeitsstundenPage` initialisierte bisher nur einmal pro Seiteninstanz und konnte beim RÃ¼ckweg veraltete Daten zeigen
  - mobil fehlte auf der eigenen Arbeitsstunden-Seite ein klarer `Soll / Geleistet / Offen`-Ãœberblick
  - bestehende Arbeitsstunden konnten mobil im normalen Erfassungspfad noch nicht im selben Formular wieder bearbeitet werden
  - die mobile Stundenvalidierung wich mit einer zusÃ¤tzlichen `<= 24`-Sonderregel unnÃ¶tig vom WPF-/Shared-Pfad ab
- Kleinen Korrekturblock deshalb nur im Arbeitsstunden-Bereich umgesetzt:
  - `MyArbeitsstundenPage` lÃ¤dt beim Wiedererscheinen jetzt erneut und bleibt dadurch kontextstabiler
  - die Seite zeigt jetzt oben einen kleinen Pflichtstunden-Ãœberblick mit `Soll`, `Geleistet` und `Offen`
  - der Ãœberblick nutzt den vorhandenen Shared-Servicepfad `GetPflichtstundenUebersichtForMitgliedAsync(...)`
  - das bestehende mobile Erfassungsformular kann jetzt auch zum Bearbeiten bereits erfasster Arbeitsstunden wiederverwendet werden
  - Antippen eines bestehenden Eintrags fÃ¼llt das Formular; derselbe Speichern-Pfad schreibt dann per `UpdateArbeitsstundeAsync(...)`
  - zusÃ¤tzlicher Button `Bearbeiten abbrechen` setzt den Editorzustand sauber zurÃ¼ck
  - die Stundenvalidierung wurde auf den belastbaren WPF-/Shared-Ansatz zurÃ¼ckgezogen: gÃ¼ltige Zahl grÃ¶ÃŸer als `0`, keine zusÃ¤tzliche Schattenregel `<= 24`
  - die Eingabe `Art der Arbeit` wurde mobil von einem einfachen Einzeilenfeld auf einen besser lesbaren Editor angehoben
- Warum dieser kleine Block fachlich sinnvoll ist:
  - der grÃ¶ÃŸte echte Praxisabstand lag im normalen mobilen Nutzerpfad
  - Ãœbersicht, RÃ¼ckkehrstabilitÃ¤t und Bearbeitung liefern direkt Nutzwert ohne neue Architektur
  - der Block bleibt vollstÃ¤ndig auf bestehenden Shared-Servicepfaden und baut keine Schattenlogik
- Bewusst nicht gemacht:
  - kein neuer mobiler Review-/Freigabeeditor analog zur WPF-PrÃ¼ftabelle
  - kein Umbau der bestehenden WPF-Dialogarchitektur
  - keine Ã„nderung an Home, Mitglieds-, Garten-, Verwaltungs-, Export- oder Ablesen-Pfaden
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Arbeitsstundenbereich bleibt erreichbar
  - Nutzerpfad bleibt intakt und ist jetzt auf Mobil Ã¼bersichtlicher und bearbeitbarer
  - Review-/Freigabepfad bleibt unverÃ¤ndert nutzbar
  - keine Verschlechterung bereits geprÃ¼fter Home-, Mitglieds- oder Gartenpfade
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 4: Parzellen / Garten / Dokumente / Strom / Wasser mit kleinem RÃ¼ckwege-/Kontext-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 4 gezielt geprÃ¼ft:
  - WPF: `ParzellenVerwaltungView`, `DokumenteView`, `DokumenteParzellenView`, `GartenDokumenteView`, `GartenDokumenteListeView`, `GartenStromView`, `GartenWasserView`, `StromView`, `WasserView`
  - MAUI: `ParzellenPage`, `DokumentePage`, relevante Bereiche in `MeineDatenPage`, `ParzellenContextState`, `ParzellenViewModel`
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - der fachliche MAUI-Parzellenkern ist bereits belastbar vorhanden
  - Gartenkontext aus dem Mitgliedskontext funktioniert bereits
  - Strom, Wasser und Parzellen-Dokumente sind auf demselben mobilen Fachpfad bereits nutzbar
  - der grÃ¶ÃŸte kleine Restabstand lag aktuell weniger in fehlender Fachlogik als in RÃ¼ckwegen und Kontextklarheit
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - aus dem mobilen Gartenkontext gab es noch keinen expliziten RÃ¼ckweg direkt zurÃ¼ck in die Stammdatenansicht
  - der RÃ¼ckweg zur globalen ParzellenÃ¼bersicht war sprachlich noch zu nah am Kontextpfad formuliert
  - die Trennung zwischen Mitgliedsdokumenten und Garten-/Parzellen-Dokumenten war mobil funktional vorhanden, aber im UI noch nicht klar genug benannt
  - beim Wiedererscheinen der `ParzellenPage` wurde der gewÃ¤hlte Detailkontext nicht ausdrÃ¼cklich erneut nachgeladen
- Kleinen Korrekturblock deshalb nur im Parzellen-/Garten-/Dokumente-Rahmen umgesetzt:
  - `ParzellenPage` erhielt im mitgliedsgebundenen Gartenkontext jetzt einen expliziten Button `Zur Stammdatenansicht`
  - der bisherige Kontext-Reset wurde sprachlich auf `Zur globalen ParzellenÃ¼bersicht` geschÃ¤rft
  - der Dokumentebereich im Parzellen-/Gartenkontext wird jetzt explizit als `Garten-Dokumente` bezeichnet
  - zusÃ¤tzlicher Hinweis macht klar, dass Mitgliedsdokumente weiterhin in den Stammdaten bleiben, wÃ¤hrend hier die Dokumente der ausgewÃ¤hlten Parzelle erscheinen
  - `ParzellenViewModel` kann den aktuell gewÃ¤hlten Detailkontext jetzt gezielt erneut laden; `ParzellenPage` nutzt das beim Wiedererscheinen der Seite
- Warum dieser kleine Block fachlich sinnvoll ist:
  - die bestehende MAUI-Parzellenlogik war bereits funktional belastbar
  - der grÃ¶ÃŸte Unterschied lag damit im Flow, in der Trennung der Kontexte und in der RÃ¼cknavigation
  - genau dort wurde jetzt nachgezogen, ohne neue Unterseiten oder Schattenlogik zu bauen
- Bewusst nicht gemacht:
  - keine neue Parzellen-/Garten-Seitenarchitektur
  - kein neuer Dokumente-Unterbau
  - kein Umbau der bestehenden Strom-/Wasser-Speicherpfade
  - keine Ã„nderung an Arbeitsstunden, Verwaltung, Export oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Parzellenpfad bleibt erreichbar
  - Gartenkontext aus Stammdaten funktioniert weiter
  - RÃ¼ckwege zwischen Gartenkontext, globaler ParzellenÃ¼bersicht und Stammdaten sind klarer
  - Dokumente / Strom / Wasser bleiben nutzbar
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 3: Mitglied neu / WartungsvertrÃ¤ge / erweiterte Mitgliedsverwaltung mit kleinem Wartungsvertrags-Einstieg fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 3 gezielt geprÃ¼ft:
  - WPF: `MitgliedNeuView`, `NewMemberView`, `MemberWartungsvertraegeView`, `WartungsvertraegeVerwaltungView`
  - MAUI: vorhandene Pendants bzw. fehlende Pfade rund um Mitgliedsanlage, WartungsvertrÃ¤ge und erweiterte Mitgliedsverwaltung
  - ergÃ¤nzend die Shared-Service-/Schema-Pfade fÃ¼r WartungsvertrÃ¤ge und Pflichtstundenbezug
- Ehrlicher Befund im aktuellen Repo-Stand:
  - `Mitglied neu` ist in WPF derzeit selbst nur als Placeholder vorhanden; in MAUI existiert dafÃ¼r aktuell kein belastbarer produktiver Pfad
  - `MemberWartungsvertraegeView` und `WartungsvertraegeVerwaltungView` sind in WPF derzeit ebenfalls nur vorbereitete Placeholder
  - in MAUI existiert ebenfalls noch kein echter eigener Verwaltungseditor fÃ¼r WartungsvertrÃ¤ge
  - belastbar vorhanden ist aber bereits der gemeinsame Datenpfad Ã¼ber `v_pflichtstunden_uebersicht`, inklusive `hat_wartungsvertrag`, `ist_befreit` und `regelgrund`
- Deshalb bewusst den kleineren und fachlich klareren Einstieg gewÃ¤hlt statt `Mitglied neu` oder Wartungsvertragsverwaltung halb zu erÃ¶ffnen:
  - keinen Fake-CRUD fÃ¼r `Mitglied neu`
  - keinen halben Wartungsvertragseditor ohne belastbaren Referenzpfad
  - stattdessen einen echten kleinen ReadOnly-Einstieg im mobilen Mitgliedskontext
- Umgesetzt:
  - neuer Shared-Service-Lesepfad `GetPflichtstundenUebersichtForMitgliedAsync(int mitgliedId)` in `ISupabaseService` / `SupabaseService`
  - der Pfad nutzt den bestehenden Pflichtstunden-/Wartungsvertragsstatus statt Schattenlogik
  - `MeineDatenPage` zeigt jetzt zusÃ¤tzlich einen Abschnitt `WartungsvertrÃ¤ge / Pflichtstunden`
  - sichtbar werden dort fÃ¼r das ausgewÃ¤hlte Mitglied:
    - Bewertungsjahr
    - Wartungsvertrag ja/nein
    - von Pflichtstunden befreit ja/nein
    - Regelgrund
  - fÃ¼r Admin/Vorstand wird zusÃ¤tzlich ehrlich eingeblendet, dass ein eigener mobiler Wartungsvertrags-Verwaltungseditor im aktuellen Stand noch nicht vorhanden ist
  - fÃ¼r normale Nutzer wird derselbe Status als ReadOnly-Kontextinformation aus der zentralen Pflichtstunden-Ãœbersicht angezeigt
- Warum dieser kleine Block fachlich sinnvoll ist:
  - WartungsvertrÃ¤ge hÃ¤ngen direkt mit Pflichtstundenbefreiung und Mitgliedskontext zusammen
  - die zugrunde liegenden Informationen sind bereits belastbar vorhanden
  - der Block liefert deshalb echten Nutzwert, ohne neue Pflege-/Schreibarchitektur zu erfinden
- Bewusst nicht gemacht:
  - kein neuer Pfad `Mitglied neu`
  - kein mobiler CRUD-Editor fÃ¼r `wartungsvertraege` oder `wartungsvertrag_zuordnungen`
  - keine Ã„nderung an Home, Shell, Parzellen, Export, Verwaltung oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Stammdaten-/Mitgliedskontext bleibt intakt
  - der neue ReadOnly-Pfad fÃ¼r Wartungsvertrags-/Befreiungsstatus ist im Mitgliedskontext erreichbar
  - Rechtepfade bleiben unverÃ¤ndert korrekt
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 2: Mitgliedersuche / Stammdaten / Mitgliedskontext mit kleinem Kontext-/Nebenmitglied-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r Block 2 gezielt geprÃ¼ft:
  - WPF: `MemberSearchView`, `MemberDetailView`, `StammdatenView`, `MemberParzellenSection`, `NebenmitgliedDialog`, `UserManagementView`
  - MAUI: `MemberSearchPage`, `MeineDatenPage.xaml.cs`, `UserManagementPage`, `NebenmitgliedPage`, `MemberContextState` und zugehÃ¶rige Kontextpfade
- Systematischer Vergleich entlang derselben Logik durchgefÃ¼hrt:
  - UI/Struktur
  - Daten/Fachinhalt
  - Aktionen/Commands
  - Navigation/Flow
  - Rechte/Sichtbarkeit
- Ehrlicher Befund im aktuellen Repo-Stand:
  - Mitgliedersuche, Stammdaten und der grundsÃ¤tzliche Mitgliedskontext sind mobil bereits belastbar vorhanden
  - Stammdaten wurden zuletzt bereits auf `Stammdaten` umbenannt und inhaltlich verbreitert
  - `UserManagement` bleibt an den Mitgliedskontext gebunden
  - der grÃ¶ÃŸte kleine Restabstand lag jetzt beim Kontextverhalten rund um Mitgliedersuche-Wiederkehr und den mobilen Nebenmitgliedspfad
- Wichtigste kleine Restabweichungen mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - `MemberSearchPage` initialisierte bisher nur beim ersten Anzeigen und konnte beim RÃ¼ckweg in dieselbe Seiteninstanz veraltet bleiben
  - der Nebenmitgliedspfad war mobil zwar vorhanden, aber aus dem ausgewÃ¤hlten Mitgliedskontext heraus nicht direkt erreichbar
  - `NebenmitgliedPage` orientierte sich bisher nur am angemeldeten Hauptmitglied statt am aktuell ausgewÃ¤hlten Mitgliedskontext
- Kleinen Korrekturblock deshalb nur innerhalb von Mitgliedersuche / Stammdaten / Mitgliedskontext umgesetzt:
  - `MemberSearchPage` initialisiert beim erneuten Anzeigen jetzt wieder und bleibt dadurch konsistenter zum WPF-RÃ¼ckkehrverhalten
  - `MeineDatenPage` zeigt jetzt einen kleinen `Mitgliedskontext`-Block fÃ¼r den vorhandenen Nebenmitgliedspfad
  - dort wird sichtbar gemacht:
    - wenn ein Nebenmitglied vorhanden ist
    - wenn keines vorhanden ist
    - wenn das ausgewÃ¤hlte Mitglied selbst kein Hauptmitglied ist
  - `NebenmitgliedPage` nutzt den ausgewÃ¤hlten `MemberContextState` jetzt bevorzugt als Hauptmitgliedsbezug statt nur den global angemeldeten Benutzerkontext
  - zusÃ¤tzlich lÃ¤dt die mobile Nebenmitgliedseite beim Wiedererscheinen erneut und bleibt dadurch kontextstabiler
  - die mobile Mitgliedsabbildung Ã¼bernimmt jetzt auch `IstHauptmitglied`, damit die Kontextentscheidung belastbar bleibt
- Bewusst nicht gemacht:
  - keine Neuarchitektur
  - kein neuer kompletter Nebenmitglied-Anlageflow in MAUI
  - keine tieferen Parzellen-/Gartenpfade in diesem Block mitgezogen
  - keine Ã„nderung an Home, Shell, Export, Verwaltung oder Ablesen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Mitgliedersuche bleibt stabil nutzbar
  - Stammdaten bleiben erreichbar
  - Mitgliedskontext bleibt konsistent
  - Nebenmitglied ist aus dem Mitgliedskontext jetzt direkt erreichbar, sofern fachlich vorhanden/relevant
  - `UserManagement` bleibt unverÃ¤ndert an den Mitgliedskontext gebunden
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ Systematische View-PrÃ¼fung Block 1: Einstieg / Shell / Home mit kleinem MAUI-Refresh-/Detailpfad-Fix fortgefÃ¼hrt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- FÃ¼r den Einstieg-/Shell-/Home-Vergleich gezielt geprÃ¼ft:
  - WPF: `LoginWindow`, `MainWindow`, `HomeView`, `HomeSectionDetailView`, relevante ViewModels
  - MAUI: `LoginPage`, `App.xaml.cs`, `AdminShell`, `UserShell`, `HomePage`, `HomeSectionDetailPage`, `ShellNavigationHelper`
- Abgleich Ergebnis im aktuellen Repo-Stand:
  - Login-/Root-/Shell-Startpfad ist im Kern bereits sauber nachgezogen
  - Home landet bereits stabil auf `home`
  - Detailpfad aus Home ist grundsÃ¤tzlich vorhanden
  - relevante Restabweichungen zum WPF-Rahmen blieben aber noch im kleinen Nutzungsfluss nach dem ersten Einstieg
- Wichtigste kleine Restabweichung mit hohem Praxisnutzen und geringem Risiko identifiziert:
  - WPF lÃ¤dt Home beim erneuten Ã–ffnen/Navigieren wieder frisch
  - MAUI-`HomePage` initialisierte bisher nur einmal pro Seiteninstanz
  - dadurch konnten RÃ¼ckkehrpfade aus Detail-/Verwaltungsseiten fachlich mit veraltetem Home-Zustand stehenbleiben
- Kleinen Korrekturblock deshalb nur im Home-/Detailpfad umgesetzt:
  - `HomePage.OnAppearing()` lÃ¤dt Home jetzt bei erneutem Sichtbarwerden wieder nach
  - gleichzeitig gegen Reentrancy abgesichert Ã¼ber kleinen `_isLoading`-Guard
  - `HomeSectionDetailPage` erhielt zusÃ¤tzlich einen expliziten Button `Zur Startseite`, analog zum klaren WPF-RÃ¼ckweg
  - RÃ¼ckweg nutzt den bestehenden Shell-Home-Pfad und Ã¶ffnet keine neue Navigationsarchitektur
- Bewusst nicht gemacht:
  - keine neue Shell-Architektur
  - kein Umbau von Login-/Root-/Android-Pfaden
  - noch keine FolgeblÃ¶cke fÃ¼r Mitgliedersuche, Stammdaten, Parzellen, Arbeitsstunden oder Verwaltung mitgezogen
  - weitere erkannte WPF-/MAUI-Deltas aus Einstieg/Home bewusst fÃ¼r spÃ¤tere systematische BlÃ¶cke dokumentativ offen gelassen
- Fachliche Kurzvalidierung nach dem kleinen Block:
  - Login bleibt funktionsfÃ¤hig
  - Home bleibt direkt und stabil erreichbar
  - RÃ¼ckkehr aus Home-Detail hat jetzt einen expliziten Startseitenpfad
  - Home-Daten werden beim Wiedererscheinen der Seite erneut geladen und damit konsistenter zum WPF-Navigationsverhalten
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ MAUI-Stammdaten-Block: Mitgliedskontext in `Stammdaten` umbenannt und mobile Stammdatenansicht an WPF angenÃ¤hert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- Relevante Referenzpfade geprÃ¼ft:
  - `KGV.Wpf/ViewModels/MemberDetailViewModel.cs`
  - `KGV.Wpf/Views/MemberDetailView.xaml`
  - `KGV.Maui/Pages/MeineDatenPage.xaml.cs`
  - `KGV.Maui/State/MemberContextState.cs`
  - ergÃ¤nzend die mobilen Such-/Kontextpfade, damit die bestehende Navigation intakt bleibt
- Relevanter Istzustand vor dem Block:
  - der mobile Mitgliedskontext war bereits vorhanden
  - die Seite lief aber sprachlich noch als `Mitgliedskontext`
  - fachlich zeigte sie gegenÃ¼ber WPF nur einen verkÃ¼rzten Auszug statt klar gruppierter Stammdaten
- Den Block bewusst klein und nur auf den mobilen Stammdatenbereich gehalten:
  - keine Neuarchitektur
  - kein Umbau von Home, Login oder Shell auÃŸerhalb der nÃ¶tigen Benennung
  - keine neue Schattenlogik neben den bestehenden Servicepfaden
- Umgesetzt:
  - Titel des mobilen Mitgliedskontexts auf `Stammdaten` gezogen
  - bestehende `MeineDatenPage` weiterverwendet und inhaltlich deutlich ausgebaut
  - Stammdaten jetzt mobil sauber gruppiert analog zur WPF-Referenz:
    - `Grunddaten`
    - `Kontakt`
    - `Adresse`
    - `Mitgliedschaft`
    - `GÃ¤rten / Parzellen`
    - `Verwaltung`
  - zusÃ¤tzliche relevante WPF-Felder jetzt auch mobil sichtbar:
    - Nachname
    - Vorname
    - Geburtsdatum
    - E-Mail
    - Telefon
    - Mobilnummer
    - WhatsApp-Einwilligung
    - StraÃŸe / Hausnummer
    - PLZ
    - Ort
    - Rolle
    - Mitglied seit
    - Mitglied Ende
    - Aktiv
    - Bemerkungen
  - die Werte werden als ruhige, saubere ReadOnly-Felder dargestellt statt als wenige Sammeltexte
  - der Verwaltungsbereich bleibt funktional erhalten, wird aber fÃ¼r nicht berechtigte Rollen vollstÃ¤ndig ausgeblendet
  - der Aktualisieren-Button wurde sprachlich auf den Stammdatenkontext angepasst
- Bewusst nicht gemacht:
  - keine neue mobile Vollbearbeitungslogik fÃ¼r alle Stammdatenfelder in diesem kleinen Block
  - keine WPF-Ã„nderung
  - keine Ã„nderung an Dokumente-, Garten- oder Admin-MenÃ¼-Pfaden auÃŸerhalb der Seite selbst
- Ehrliche Restlage nach dem Block:
  - der mobile Stammdatenbereich ist jetzt fachlich deutlich nÃ¤her an WPF und keine knappe Platzhalteransicht mehr
  - einzelne WPF-Bearbeitungskomfortpfade wie vollwertiger Edit-/Lock-Flow fÃ¼r alle Stammdatenfelder sind mobil in diesem Block bewusst noch nicht nachgezogen
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs` sowie bestehenden Infrastructure-Nullability-Pfaden

## 2026-03-24 â€“ MAUI-UI-/Flow-Feinschliff: Home nach Login direkt sichtbar, Arbeitsstunden-Kacheln klarer, Home-Export entfernt, Mitgliedersuche beruhigt

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft.
- Relevante MAUI-Dateien gezielt geprÃ¼ft:
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
  - der aktive Shell-Einstieg wurde aber nur generisch auf einen gÃ¼ltigen Eintrag gezogen, nicht gezielt auf `home`
  - dadurch blieb ein plausibler Pfad, dass die richtige Shell steht, Home aber nicht sofort als sichtbare Zielseite landet
- Den Home-Start deshalb minimal-invasiv gehÃ¤rtet:
  - `ShellNavigationHelper` kann jetzt gezielt eine bevorzugte Content-Route aktivieren
  - `App`, `AdminShell`, `UserShell` und der Login-Fallback bevorzugen jetzt explizit die Route `home`
  - damit landet der frische Shell-Aufbau nach Login direkt sichtbar auf `Startseite`, ohne die bestehende Root-/Fragment-HÃ¤rtung wieder aufzureiÃŸen
- Home-Seite punktuell weiter verbessert:
  - `Arbeitsstunden` zeigt `Soll`, `Geleistet` und `Offen` jetzt als drei ruhige Kennzahl-Karten statt nur als verdichtete Zeile
  - die bisherige Fallback-Liste bleibt nur noch sichtbar, wenn keine echte Pflichtstunden-Zusammenfassung geladen wurde
  - der Home-Shortcut `Mitglieder exportieren` wurde aus dem Verwaltungsbereich entfernt; der separate Export-Navigationspunkt bleibt unverÃ¤ndert erhalten
- Mitgliedersuche optisch beruhigt:
  - die groÃŸe Checkbox fÃ¼r `Hauptmitglied` wurde aus der Listenansicht entfernt
  - statt dessen klare Kartenstruktur mit:
    - Name links
    - Zusatzzeile darunter
    - Gartennummer(n) rechts als ruhiges Badge
    - kleines textliches `Hauptmitglied`-Badge statt sperriger Checkbox
  - Auswahl-/Navigationsfunktion bleibt unverÃ¤ndert erhalten
- Bewusst nicht gemacht:
  - keine neue Architektur
  - keine Ã„nderung an WPF-Dateien
  - kein Eingriff in Export-Logik oder Mitgliedskontextpfade auÃŸerhalb des kleinen UI-/Flow-Feinschliffs
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs`

## 2026-03-24 â€“ MAUI-Home-Feinschliff: Schnellzugriff entfernt und Startseitenbereiche ruhiger gegliedert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den aktuellen MAUI-Home-Pfad gezielt geprÃ¼ft:
  - `KGV.Maui/Pages/HomePage.xaml.cs`
  - `KGV.Maui/ViewModels/HomeViewModel.cs`
  - `KGV.Maui/Pages/HomeSectionDetailPage.cs`
  - vorhandene MAUI-Stile unter `Resources/Styles`
- Relevanter Istzustand vor dem Block:
  - `Schnellzugriff` war auf Home noch sichtbar
  - die Ã¼brigen Home-Bereiche waren funktional vorhanden, wirkten aber visuell noch zu nah beieinander
  - besonders `Arbeitsstunden` und `ArbeitseinsÃ¤tze` waren optisch zu wenig voneinander getrennt
- Den Block bewusst klein und rein visuell gehalten:
  - kein neuer Fachpfad
  - keine neue Navigation
  - keine Ã„nderung an den bestehenden Detailseitenpfaden
- Umgesetzt:
  - Bereich `Schnellzugriff` aus der mobilen Home-Seite entfernt
  - die Home-Seite stattdessen in ruhige, klar getrennte Abschnittskarten gegliedert
  - vier Bereiche bekamen jeweils eine eigene, zurÃ¼ckhaltende visuelle IdentitÃ¤t Ã¼ber subtile Hintergrund-/Akzentfarben:
    - `Arbeitsstunden`
    - `ArbeitseinsÃ¤tze`
    - `Termine`
    - `Bekanntmachungen`
  - jede Sektion besitzt jetzt klareren Header, Untertitel, Trennlinie und konsistentere InnenabstÃ¤nde
  - die EintrÃ¤ge innerhalb der Bereiche laufen jetzt ebenfalls als ruhig akzentuierte Karten statt als kaum getrennte TextblÃ¶cke
  - der vorhandene Verwaltungsbereich fÃ¼r Admin/Vorstand wurde optisch an dieselbe Struktur angepasst
- Kleine inhaltliche Begradigung im ViewModel:
  - der frÃ¼here operative Bereich wird auf Home jetzt klar als `Arbeitsstunden` bezeichnet
  - damit passt die sichtbare Struktur wieder direkt zur gewÃ¼nschten Fachgliederung
- Bewusst nicht gemacht:
  - keine Ã„nderung an WPF-Dateien, weil der Block ausdrÃ¼cklich MAUI-Home-Feinschliff war
  - keine neue Logik fÃ¼r `QuickLinks`
  - keine Ã„nderung der bestehenden Klickpfade zu Detail-/Verwaltungsseiten
- Fachliche Kurzvalidierung nach dem Umbau:
  - `Schnellzugriff` ist auf Home entfernt
  - die Home-Seite bleibt vollstÃ¤ndig nutzbar
  - die vier Hauptbereiche sind visuell klarer unterscheidbar, ohne bunt-chaotisch zu wirken
  - bestehende Klickpfade Ã¼ber Home bleiben erhalten
- Technische Verifikation:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen liegen weiterhin in `HomeManagementPage.cs`

## 2026-03-24 â€“ Repo-Wahrheit zum MAUI-Root-Handover verifiziert und Android-Fragment-Restore gegen NavigationRootManager-Crash abgesichert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog, den echten Git-Arbeitsbaum und zusÃ¤tzlich die pausierte Android-Debugsitzung geprÃ¼ft.
- Repo-Wahrheit zuerst ausdrÃ¼cklich verifiziert:
  - der zuletzt dokumentierte Root-Handover-Fix ist im aktiven Repo tatsÃ¤chlich vorhanden
  - `App.xaml.cs` enthÃ¤lt bereits die zentrale Root-Erzeugung und `SwitchToCurrentRootAsync()`
  - `LoginPage.xaml.cs` delegiert den Handover bereits an `App`
  - damit war die Ausgangslage nicht â€žFix fehlt im Repoâ€œ, sondern â€žFix vorhanden, Crashpfad bleibt trotzdem aktivâ€œ
- Den verbleibenden Crash deshalb nicht erneut geraten, sondern Ã¼ber die Android-Debuglogs eingegrenzt:
  - Crash weiterhin beim Activity-Start, noch vor nutzbarer Login-Interaktion
  - Stacktrace: `NavigationRootManager_ElementBasedFragment` / `No view found for id ...`
  - damit lag der Restfehler nun plausibler im Android-Fragment-Restore eines alten/stalen MAUI-Navigationszustands als im bereits eingebauten Root-Handover-Code selbst
- Kleinen Android-Fix direkt am wahrscheinlichsten Restpfad umgesetzt:
  - `MainActivity.OnCreate(Bundle? savedInstanceState)` ergÃ¤nzt
  - `base.OnCreate(null)` statt Restore des vorhandenen `savedInstanceState`
  - dadurch versucht Android beim Start nicht mehr, alte MAUI-/Fragment-Navigationscontainer wieder an eine inzwischen nicht mehr passende Rootstruktur zu hÃ¤ngen
- Dieser Fix bleibt minimal-invasiv:
  - keine neue Navigationsarchitektur
  - kein RÃ¼ckbau des echten Root-Handover-Stands
  - keine unnÃ¶tigen WPF-Ã„nderungen
- Loginbutton-/Altlogik nochmals gegengeprÃ¼ft:
  - `Anmelden` bleibt im aktiven Repo klarer Textbutton
  - keine alte Icon-Button-Logik mehr aktiv gefunden
- Build-/Deployrest kurz mitgedacht:
  - Debuglogs zeigen weiter FastDev-/Override-Pfade auf dem GerÃ¤t
  - das kann Ã¤ltere GerÃ¤tezustÃ¤nde begÃ¼nstigen, war aber hier nicht die eigentliche Codeursache des beobachteten Fragment-Restore-Crashs
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - unverÃ¤nderte Warnungen bleiben in `HomeManagementPage.cs`

## 2026-03-24 â€“ MAUI-Login-Rootwechsel fÃ¼r Android gegen `NavigationRootManager_ElementBasedFragment` abgesichert

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den aktuellen MAUI-Root-/Shellpfad nochmals gezielt geprÃ¼ft:
  - `App.xaml.cs`
  - `LoginPage.xaml.cs`
  - `AdminShell.cs`
  - `UserShell.cs`
  - `ShellNavigationHelper.cs`
  - `MauiProgram.cs`
- ZusÃ¤tzlich die Debug-/Android-Logs geprÃ¼ft, weil die Debugsitzung pausiert vorlag.
- Relevanter Befund aus den Logs:
  - Crash auf Android: `No view found for id ... for fragment NavigationRootManager_ElementBasedFragment`
  - der Stack lief in den Android-Fragment-/Activity-Startpfad hinein
  - das passte fachlich zu einem Root-Mismatch zwischen alter Login-Hierarchie und neuer Shell-Hierarchie
- Plausibelste Ursache im aktuellen Stand prÃ¤zisiert:
  - `App.CreateWindow(...)` erzeugte immer `LoginPage` als Window-Root
  - nach Login wurde spÃ¤ter per `window.Page = shell` auf Shell umgestellt
  - wenn Android Activity/Window erneut anlegt oder Fragmente restauriert, kann dadurch ein alter Fragmentzustand gegen einen nicht mehr passenden Rootcontainer laufen
- Den Fix klein und direkt am Root-Handover umgesetzt:
  - `App` baut den aktuellen Root jetzt zentral selbst auf statt nur eine feste `LoginPage` zu halten
  - `CreateWindow(...)` liefert abhÃ¤ngig vom aktuellen Benutzerkontext entweder `LoginPage` oder eine frisch gebaute Ziel-Shell
  - dadurch bekommt auch ein spÃ¤ter neu erzeugtes Android-Window wieder die zur Session passende Root-Hierarchie
  - fÃ¼r den aktiven Loginwechsel wurde der Rootwechsel in `App.SwitchToCurrentRootAsync()` zentralisiert
  - auf Android wird dabei ein neues Window mit passender Ziel-Root geÃ¶ffnet und das alte Login-Window erst danach geschlossen
  - damit lÃ¤uft kein Android-Fragmenthost mehr gegen eine gerade ersetzte Rootstruktur im selben Window weiter
- `LoginPage` verwendet diesen zentralen Handover jetzt direkt statt selbst die Window-Root-Seite hart zu ersetzen.
- Loginbutton-/Altlogik mitgeprÃ¼ft:
  - `Anmelden` bleibt ein klarer Textbutton
  - keine alte Icon-Button-Logik mehr im aktiven Loginpfad gefunden
  - Debuglogs zeigen weiter FastDev-/Override-Deploypfade; das ist ein mÃ¶glicher Grund, warum auf GerÃ¤ten Ã¤ltere StÃ¤nde sichtbar wirken kÃ¶nnen, aber kein aktiver UI-Codepfad im Repo
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen stammen unverÃ¤ndert aus `HomeManagementPage.cs`

## 2026-03-24 â€“ Android-Shell-Fragment-Crash in MAUI entschÃ¤rft und Login-Branding klargezogen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben wieder unangetastet.
- Gezielt den aktuellen MAUI-Start-/Shell-Pfad geprÃ¼ft:
  - `App.xaml.cs`
  - `MauiProgram.cs`
  - `AdminShell.cs`
  - `UserShell.cs`
  - `ShellNavigationHelper.cs`
  - `ShellRouteRegistrar.cs`
  - `LoginPage.xaml.cs`
- Plausibelster Crash-Pfad fÃ¼r Android identifiziert:
  - Shell-Routen wurden erst beim Shell-Bau registriert statt einmal zentral beim Start
  - nach dem Login lief zusÃ¤tzlich noch eine nachgelagerte Shell-Aktivierung Ã¼ber Dispatcher gegen die frisch gesetzte Shell
  - in `AdminShell` wurde der Flyout-Eintrag `Arbeitsstunden freigeben` nach Renderbeginn asynchron Ã¼ber `IsVisible` ein-/ausgeblendet
  - diese Kombination ist ein realistischer AuslÃ¶ser fÃ¼r verwaiste Android-`ShellItemRenderer`-/FragmentzustÃ¤nde
- Den Fix klein und minimal-invasiv umgesetzt:
  - gemeinsame Shell-Routen werden jetzt einmal zentral in `MauiProgram` registriert
  - die Shell-Konstruktoren registrieren keine Routen mehr selbst
  - `LoginPage.SwitchToUserContext(...)` setzt nach `BuildMenu()` direkt die Ziel-Shell, ohne zusÃ¤tzliche nachgelagerte Dispatcher-Navigation gegen dieselbe Shell-Instanz
  - `App.xaml.cs` startet direkt mit `LoginPage` statt mit einer zusÃ¤tzlichen `NavigationPage`-HÃ¼lle, damit beim Wechsel zur Shell kein unnÃ¶tiger Container im Android-Pfad mitlÃ¤uft
  - `AdminShell.RefreshWorkhoursReviewMenuAsync()` Ã¤ndert nur noch den Titel/Badge-Text, blendet den Shell-Eintrag aber nicht mehr nachtrÃ¤glich sichtbar/unsichtbar
  - damit werden nach Renderbeginn keine Shell-Items mehr dynamisch aus dem Android-Container entfernt
- Branding/Login zusÃ¤tzlich klargezogen:
  - geprÃ¼ft: aktiver Launcher-Pfad bleibt `Resources/AppIcon/appicon.svg`
  - der Launcher-Icon-Pfad wurde im Projektfile zusÃ¤tzlich explizit mit `BaseSize` abgesichert
  - der Splash bleibt weiterhin auf demselben KGV-Logo
  - auf der Loginseite bleibt das Logo oben sichtbar
  - der `Anmelden`-Button wurde bewusst wieder als klarer Textbutton ohne Logo im Button selbst ausgefÃ¼hrt
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
  - verbleibende Warnungen stammen unverÃ¤ndert aus `HomeManagementPage.cs` und nicht aus diesem Block

## 2026-03-24 â€“ MAUI-Restabgleich mit Altpfad-Bereinigung und Editor-/Rechte-Feinschliff abgeschlossen

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und den echten Git-Arbeitsbaum geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben unangetastet.
- Den Restabgleich gezielt auf kleine, klare Praxisprobleme fokussiert statt einen neuen GroÃŸblock zu Ã¶ffnen.
- Bereinigte Alt-/Testpfade:
  - alte `RoleChoicePage` aus MAUI entfernt; der Pfad ist seit der Umstellung auf echten `UserContext` fachlich Ã¼berholt
  - der Testpfad `Foto-Upload testen` wurde aus dem aktiven `Ablesen`-Einstieg und aus den zentral registrierten Shell-Routen entfernt, damit im produktiven MAUI-MenÃ¼ kein veralteter Testpfad mehr auftaucht
- Editor-/Verwaltungsfeinschliff auf den mobilen Verwaltungsseiten umgesetzt:
  - `HomeManagementPage` schaltet Eingabefelder fÃ¼r Zeit-/Teilnehmeroptionen jetzt passend aktiv/inaktiv statt alles immer offen zu lassen
  - neuer Datensatz setzt die alte Auswahl im Listenbereich sauber zurÃ¼ck
  - Busy-ZustÃ¤nde sperren Bereichsauswahl, Listen und Speichern belastbar wÃ¤hrend LadevorgÃ¤ngen/Speichern
  - Basiskontrollen an WPF angeglichen:
    - Endzeit darf nicht vor Startzeit liegen
    - Stundenwert bei ArbeitseinsÃ¤tzen darf nicht negativ sein
    - HTML-Inhalt von Bekanntmachungen wird getrimmt gespeichert
  - `Speichern` bleibt am Formularende
- Rechte-/Sichtbarkeitsfeinschliff umgesetzt:
  - `HomeManagementPage` blendet den Editorbereich fÃ¼r nicht berechtigte Rollen komplett aus und lÃ¤sst nicht nur eine Statusmeldung stehen
  - `ExportPage` blendet die Exportaktion fÃ¼r nicht berechtigte Rollen aus statt nur deaktiviert stehen zu lassen
  - bestehende Regeln bleiben erhalten:
    - `Benutzerverwaltung` nur fÃ¼r Admin
    - `Export` nur fÃ¼r Admin/Vorstand
    - Home-Verwaltung nur fÃ¼r Admin/Vorstand
- Ehrlicher Stand nach diesem Feinschliff:
  - kein neuer fachlicher Blocker im MAUI-Kernpfad sichtbar
  - offen bleiben vor allem Komfort-/Feintuning-Themen und einzelne tiefere Desktop-EditorparitÃ¤ten
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Export produktiv nachgezogen und RestlÃ¼cken ehrlich eingeordnet

- Vor dem Block erneut den realen Repo-/Arbeitsbaumstand, den aktuellen ausfÃ¼hrlichen Fortschrittslog und zusÃ¤tzlich den echten Git-Arbeitsbaum geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben wieder unangetastet.
- WPF-Exportpfad als Referenz geprÃ¼ft:
  - produktiv vorhanden war dort ein einfacher, aber echter CSV-Export fÃ¼r Mitglieder Ã¼ber `ExportViewModel`
  - der fachlich grÃ¶ÃŸte Nutzen mit kleinem Risiko liegt damit klar beim Mitglieder-CSV statt bei einer grÃ¶ÃŸeren Exportplattform
- Bestehenden Fach-/Shared-Unterbau geprÃ¼ft:
  - Exportbasis ist `GetMitgliederAsync()`
  - WPF hatte bisher seine CSV-Erzeugung lokal im ViewModel
  - fÃ¼r MAUI gibt es belastbare Android-Wege fÃ¼r Dateiablage/Teilen Ã¼ber `FileSystem` + `Share`
- Den Block deshalb klein und produktiv umgesetzt:
  - gemeinsame CSV-Erzeugung nach `KGV.Core` in `MitgliederCsvExportBuilder` gezogen, damit WPF und MAUI denselben Exportkern verwenden
  - Demo-/Test-/Play-Store-Mitglieder werden im operativen Export explizit ausgeschlossen
  - WPF-`ExportViewModel` nutzt jetzt denselben Shared-Builder statt eigene CSV-Logik daneben zu halten
  - neue mobile `ExportPage` ergÃ¤nzt
  - Export erzeugt lokal eine echte CSV-Datei und gibt sie anschlieÃŸend Ã¼ber den Android-Teilen-/Share-Pfad weiter
  - Admin/Vorstand erreichen den Export jetzt mobil:
    - Ã¼ber eigenen Shell-MenÃ¼punkt `Export`
    - zusÃ¤tzlich Ã¼ber den Home-Verwaltungsbereich
- Bewusst nicht gemacht:
  - keine neue mehrstufige Exportplattform
  - keine breite Portierung aller denkbaren WPF-Exportarten in diesen Block
  - keine Ã„nderung an bereits stabilisierten Login-/Mitglieds-/Garten-/Home-Pfaden auÃŸerhalb der nÃ¶tigen Einbindung
- Ehrliche RestlÃ¼ckeneinordnung nach diesem Block:
  - kritisch:
    - aktuell keine neue kritische MAUI-Sackgasse mit demselben Gewicht wie zuvor bei Mitglieds-/Garten-/Home-Kontext identifiziert
  - wichtig:
    - tiefere WPF-EditorparitÃ¤t in mobilen Verwaltungsseiten (Validierung, FokusfÃ¼hrung, mehr Feldtiefe)
    - weitere Exportarten, falls im WPF-Alltag mehr als Mitglieder-CSV fachlich relevant genutzt werden
    - RestparitÃ¤t einzelner Admin-/Kontextpfade auÃŸerhalb des jetzt geschlossenen Kerns
  - spÃ¤ter:
    - UI-/Komfortausbau des mobilen Exportpfads (z. B. zusÃ¤tzliche Formate oder differenziertere Auswahl)
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Home und mobile VerwaltungszugÃ¤nge fÃ¼r Bekanntmachungen, Termine und ArbeitseinsÃ¤tze auf echten Shared-Pfad gezogen

- Vor dem Block wieder den realen Repo-/Arbeitsbaumstand sowie den aktuellen ausfÃ¼hrlichen Fortschrittslog geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben bewusst unangetastet.
- Ausgangszustand vor diesem Block:
  - MAUI-`HomePage` zeigte bisher nur Schnellzugriffe, operative Hinweise und eine reduzierte Bekanntmachungsdetailanzeige direkt auf der Startseite
  - `ArbeitseinsÃ¤tze` und `Termine` aus `HomeOverviewDTO` wurden mobil noch gar nicht als eigener Home-Pfad genutzt
  - fÃ¼r `Bekanntmachungen`, `Termine` und `ArbeitseinsÃ¤tze` fehlten mobil weiterhin echte VerwaltungsoberflÃ¤chen trotz vorhandener Shared-Service-CRUD-Pfade
- Den WPF-/MAUI-Abgleich fÃ¼r diesen Block auf grÃ¶ÃŸten Nutzen mit kleinem Risiko fokussiert:
  - Home-Details nicht mehr inline, sondern in eigener Seite
  - echte mobile VerwaltungszugÃ¤nge fÃ¼r die drei Bereiche
  - Wiederverwendung der bestehenden Shared-Servicepfade statt neuer Schattenlogik
- Dazu den MAUI-Home-Pfad gezielt nachgezogen:
  - `HomeViewModel` nutzt jetzt neben Schnellzugriffen/Hint-BlÃ¶cken auch die vorhandenen Home-Daten fÃ¼r `ArbeitseinsÃ¤tze`, `Termine` und `Bekanntmachungen`
  - `HomePage` zeigt diese drei Listen jetzt mobil sichtbar an
  - Auswahl lÃ¤uft nicht mehr in eine Inline-Detailspalte, sondern Ã¼ber neuen echten Detailpfad `HomeSectionDetailPage`
- Neuer mobiler Home-Detailpfad ergÃ¤nzt:
  - kleiner `HomeContextState` hÃ¤lt den aktuell ausgewÃ¤hlten Home-Eintrag
  - `HomeSectionDetailPage` zeigt Details fÃ¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung`
  - bei `Arbeitseinsatz` bleibt der produktive Anmeldepfad mobil nutzbar
  - fÃ¼r Admin/Vorstand werden beim Arbeitseinsatz zusÃ¤tzlich Teilnehmer Ã¼ber den bestehenden Shared-Servicepfad geladen
- Mobile VerwaltungsoberflÃ¤che fÃ¼r die drei Bereiche als ein kompakter gemeinsamer Block ergÃ¤nzt:
  - neue `HomeManagementPage`
  - Auswahl der Module `ArbeitseinsÃ¤tze`, `Termine`, `Bekanntmachungen`
  - Listen laden jeweils direkt aus den vorhandenen Shared-Servicepfaden
  - Speichern lÃ¤uft direkt Ã¼ber:
    - `CreateArbeitseinsatzAsync(...)` / `UpdateArbeitseinsatzAsync(...)`
    - `CreateTerminAsync(...)` / `UpdateTerminAsync(...)`
    - `CreateBekanntmachungAsync(...)` / `UpdateBekanntmachungAsync(...)`
  - `HTML-Inhalt` fÃ¼r Bekanntmachungen ist mobil damit auf einem echten Bearbeitungspfad nutzbar
  - `Speichern` bleibt jeweils am Ende des Formularpfads
- Home stellt diese Verwaltungswege fÃ¼r Admin/Vorstand jetzt direkt erreichbar bereit:
  - `ArbeitseinsÃ¤tze bearbeiten`
  - `Termine bearbeiten`
  - `Bekanntmachungen bearbeiten`
- Die Shell-Routen dafÃ¼r wurden zentralisiert registriert, damit Admin- und User-Shell keine doppelten MAUI-Routen gegeneinander registrieren.
- Bewusst nicht gemacht:
  - keine neue Gesamtarchitektur fÃ¼r Home oder Verwaltung
  - kein vollstÃ¤ndiger Nachbau aller WPF-Editorvalidierungen/Fokussteuerungen in diesem einen Block
  - keine Ã„nderung an bereits stabilisierten Login-/Mitglieds-/GartenkontextblÃ¶cken auÃŸerhalb der benÃ¶tigten Home-Routen
- Fachliche Kurzvalidierung fÃ¼r diesen Block:
  - Home zeigt die neuen Listen-/Detaileinstiege
  - Admin/Vorstand erreicht mobil die Verwaltungsfunktionen fÃ¼r `Bekanntmachungen`, `Termine` und `ArbeitseinsÃ¤tze`
  - bestehende Nutzerpfade wie Schnellzugriffe und Arbeitseinsatz-Anmeldung bleiben intakt
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Garten-/Parzellenkontext im Mitgliedspfad auf bestehenden Detailkern gezogen

- Vor dem Block wieder den echten Repo-/Arbeitsbaumstand und den aktuellen Fortschrittslog geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien blieben erneut unangetastet.
- Ausgangszustand vor dem Umbau:
  - der mobile Mitgliedskontext zeigte GÃ¤rten/Parzellen bisher nur als Information
  - `Strom`, `Wasser` und Garten-Dokumente waren aus dem Mitgliedspfad noch nicht als echter Gartenkontext erreichbar
  - gleichzeitig existierte in MAUI bereits ein belastbarer Parzellen-Detailpfad (`ParzellenPage` + `ParzellenViewModel`) mit genau diesen Fachbereichen
- WPF-Referenzpfad geprÃ¼ft:
  - im Desktop-Mitgliedskontext fÃ¼hren Garten-Unterpunkte auf `Strom`, `Wasser` und `Dokumente`
  - der mobile Block sollte deshalb keinen neuen Schattenpfad erÃ¶ffnen, sondern den vorhandenen belastbaren MAUI-Parzellenkern kontextgebunden nutzbar machen
- Den Block bewusst klein und wiederverwendend umgesetzt:
  - neuer kleiner `ParzellenContextState` hÃ¤lt den aus dem Mitgliedskontext angeforderten Garten/Parzelle-Kontext
  - `MeineDatenPage` Ã¶ffnet beim Tippen auf eine vorhandene Zuordnung jetzt den echten Gartenkontext statt nur Text anzuzeigen
  - der Wechsel lÃ¤uft auf die bestehende `ParzellenPage`
  - `ParzellenViewModel` Ã¼bernimmt den angeforderten Kontext, selektiert die gewÃ¼nschte Parzelle automatisch und schaltet Titel/Beschreibung/Hinweis auf den Mitgliedskontext um
  - `ParzellenPage` zeigt im Kontextmodus direkt den bereits vorhandenen Fachkern mit:
    - `Strom`
    - `Wasser`
    - Garten-Dokumenten
  - zusÃ¤tzlich kleiner RÃ¼ckweg `Zur ParzellenÃ¼bersicht`, damit die globale Parzellenverwaltung weiter nutzbar bleibt
- Wichtiger fachlicher Punkt:
  - Es wurde keine neue Platzhalterseite fÃ¼r Garten-Unterbereiche gebaut.
  - Der mobile Mitgliedspfad verwendet jetzt direkt die vorhandenen belastbaren Daten-/Servicepfade aus der Parzellenverwaltung fÃ¼r ZÃ¤hlerstatus, Ablesungen und Garten-Dokumente.
- Fachliche Kurzvalidierung fÃ¼r diesen Block:
  - Mitglied auswÃ¤hlen
  - in Mitgliedskontext wechseln
  - Garten/Parzelle antippen
  - `Strom`, `Wasser` und Garten-Dokumente im echten Gartenkontext erreichbar
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Mitgliedskontext, Admin-MenÃ¼ und Mitgliedsdokumente auf belastbaren Kernpfad gezogen

- Den echten Stand vor dem Block wieder direkt im Repo/Arbeitsbaum geprÃ¼ft: MAUI hatte nach der Branding-Korrektur weiter keinen echten mitgliedsbezogenen Detailpfad, `MemberSearchPage` endete nur mit `DisplayAlert`, `MeineDatenPage` und `DokumentePage` waren noch Platzhalter, und die mobile `Benutzerverwaltung` hing weiterhin lose global statt am Mitgliedskontext.
- WPF als Referenzpfad geprÃ¼ft:
  - `MemberSearchViewModel` navigiert dort in `MemberDetailViewModel`
  - `DokumenteViewModel` arbeitet mit `GetMitgliedDokumenteAsync(...)` + `CreateDokumentSignedUrlAsync(...)`
  - `Benutzerverwaltung` ist im Mitgliedskontext gebunden und legt bei Bedarf einen Platzhalter-Appuser fÃ¼r Einladung/Erstlogin an
  - `Benutzerverwaltung` bleibt Admin-only, nicht Vorstand-only
- Den MAUI-Block deshalb klein, aber produktiv am bestehenden Fachkern umgesetzt:
  - neuer kleiner `MemberContextState` hÃ¤lt den ausgewÃ¤hlten Mitgliedskontext mobil Ã¼ber die Navigation hinweg
  - `MemberSearchPage` wechselt nach Auswahl jetzt nicht mehr nur in einen Alert, sondern setzt den Mitgliedskontext und navigiert in die vorhandene `MeineDatenPage`
  - die frÃ¼here Platzhalter-`MeineDatenPage` wurde in einen echten mobilen Mitgliedskontext umgebaut:
    - Mitgliedsgrunddaten
    - Kontakt/Adresse
    - Garten-/Parzellenzuordnungen
    - Dokumente-Einstieg
    - `Admin-MenÃ¼` im Mitgliedskontext
  - das `Admin-MenÃ¼` orientiert sich am WPF-Pfad:
    - sichtbar fÃ¼r Admin/Vorstand im ausgewÃ¤hlten Mitgliedskontext
    - Rolleninformation und Rollenwechsel Ã¼ber denselben Lock-/`UpdateMitgliedAsync(...)`-Pfad wie in WPF
    - `Benutzerverwaltung` nur fÃ¼r Admin sichtbar
  - die lose globale `Benutzerverwaltung` wurde aus der `AdminShell` entfernt und stattdessen nur noch als Kontextpfad-Routenziel verwendet
  - die frÃ¼here Platzhalter-`DokumentePage` wurde auf den realen Mitgliedsdokumente-Pfad mit `GetMitgliedDokumenteAsync(...)` und Signed-URL-Ã–ffnung umgestellt
  - `UserManagementViewModel` / `UserManagementPage` wurden auf den ausgewÃ¤hlten Mitgliedskontext gebunden:
    - lÃ¤dt nur noch den Appuser des ausgewÃ¤hlten Mitglieds
    - legt bei fehlender Zuordnung einen Platzhalter fÃ¼r Einladung/Erstlogin an
    - erlaubt kontextgebunden Einladung, Passwort-Reset, Rollenpflege und Entfernen des Appusers
- Bewusst nicht gemacht:
  - keine neue Gesamtarchitektur fÃ¼r Navigation
  - kein Garten-Dokumente-Endausbau im mobilen Mitgliedspfad
  - keine Ã„nderung an Login-/Shell-Fixes auÃŸerhalb des fÃ¼r diesen Block nÃ¶tigen Routing-/MenÃ¼bezugs
- Fachlicher Stand nach diesem Block:
  - Admin/Vorstand kann mobil ein Mitglied auswÃ¤hlen
  - landet in einem echten Mitgliedskontext statt in einem Alert
  - sieht dort das `Admin-MenÃ¼`
  - Mitgliedsdokumente sind Ã¼ber einen echten Storage-Pfad erreichbar
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Branding auf echtes Vereinslogo gezogen und Startpfad ehrlich gegen WPF abgeglichen

- Vor dem Block den echten Arbeitsbaum erneut geprÃ¼ft; der Stand ist weiterhin nicht synchron: viele blockfremde lokale Ã„nderungen liegen offen, MAUI ist fachlich und UI-seitig noch nicht auf WPF-Niveau.
- Branding-Istzustand belastbar geprÃ¼ft:
  - aktives MAUI-App-Icon kam bereits aus `KGV.Maui/Resources/AppIcon/appicon.svg`
  - aktiver Splash kam dagegen noch aus `KGV.Maui/Resources/Splash/splash.svg` und zeigte nur eine generische KGV-Platzhaltergrafik
  - `LoginPage` zeigte bislang gar kein Vereinslogo
  - zusÃ¤tzlich liegen lokale/unversionierte Dubletten wie `KGV.Maui/Resources/AppIcon/appicon.png` und `Logo.png` im Arbeitsbaum; sie wurden fÃ¼r diesen Block nur als Befund geprÃ¼ft, nicht zur neuen Grundlage gemacht
- Den Block klein und buildfÃ¤hig direkt am aktiven MAUI-Pfad korrigiert:
  - `KGV.Maui.csproj` nutzt fÃ¼r den Splash jetzt dasselbe echte Vereinslogo wie fÃ¼r das App-Icon (`Resources/AppIcon/appicon.svg`)
  - dasselbe Logo wird zusÃ¤tzlich als MAUI-Bildressource eingebunden, damit es auf der Loginseite sauber wiederverwendbar ist
  - `LoginPage` zeigt oben jetzt das Vereinslogo sichtbar an
  - der Start-/Login-Button `Anmelden` trÃ¤gt das Logo ebenfalls links im Button und bleibt optisch kompakt statt Ã¼berladen
- Bewusst nicht gemacht:
  - keine Neuarchitektur des Loginflows
  - keine Ã„nderung an WPF
  - keine Ãœbernahme unversionierter Dubletten in diesen Block
  - keine pauschale Behauptung, MAUI sei jetzt gleichauf mit WPF
- Ehrlicher Zwischenstand nach dem Vergleich:
  - WPF bleibt klar funktionaler und tiefer ausgebaut
  - MAUI hat belastbare Kernpfade fÃ¼r Login, Shell, Home-Basis, Parzellen-Kern, Arbeitsstunden, Ablesen und Benutzerverwaltung
  - MAUI fehlt aber weiterhin u. a. bei Export, memberbezogener Detailnavigation, Garten-Unterbereichen, Dokumentenpfaden, Home-Detail-/Verwaltungsansichten und mehreren Admin-/Kontextpfaden aus WPF
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Shell gegen leeren aktiven Einstieg gehÃ¤rtet, Shell-Wechsel nach Login belastbar gemacht

- Den realen Repo-/Arbeitsbaumstand vor dem Block geprÃ¼ft; blockfremde offene WPF-/Supabase-Dateien bewusst unangetastet gelassen.
- Den bereits begonnenen Block 1 im echten Stand validiert:
  - `LoginPage` lÃ¤dt nach Login bereits den echten `UserContext`
  - die alte aktive Rollenwahl ist aus dem Loginflow entfernt
  - `AdminShell` richtet Rechte bereits am geladenen `UserContext` aus
- Der offene Restfehler lag damit im Shell-Startpfad von MAUI und nicht mehr in der Rollenwahl:
  - `AdminShell` / `UserShell` bauten MenÃ¼s zwar auf, hÃ¤rteten den aktiven `ShellItem`/`ShellSection`/`ShellContent` aber nicht belastbar nach
  - beim Wechsel auf wiederverwendete Shell-Instanzen bzw. bei spÃ¤teren SichtbarkeitsÃ¤nderungen konnte der aktive Shell-Einstieg ungÃ¼ltig werden
  - genau dieser Zustand passt zum beobachteten Fehler `Active Shell Item not set`
- Den Shell-Block minimal-invasiv gehÃ¤rtet:
  - gemeinsame Shell-Hilfe ergÃ¤nzt, die immer einen gÃ¼ltigen sichtbaren Einstieg Ã¼ber `ShellItem` â†’ `ShellSection` â†’ `ShellContent` sicherstellt
  - `AdminShell` und `UserShell` rufen diese Absicherung jetzt nach dem MenÃ¼aufbau und zusÃ¤tzlich beim Laden der Shell auf
  - `AdminShell.RefreshWorkhoursReviewMenuAsync()` stabilisiert nach Sichtbarkeitswechseln den aktiven Einstieg erneut, statt nur implizit auf den alten Zustand zu vertrauen
  - `LoginPage` baut die Ziel-Shell jetzt vollstÃ¤ndig auf, sichert den aktiven Einstieg ab und wechselt erst dann die Window-Page; direkt nach dem Wechsel wird die Shell nochmals auf dem UI-Dispatcher nachgehÃ¤rtet
  - `AdminShell` und `UserShell` werden in MAUI nicht mehr als Singleton wiederverwendet, sondern pro Wechsel frisch erzeugt, damit kein alter Shell-Zustand in einen neuen Login-/Restore-Pfad hineinragt
- Keine Neuarchitektur eingefÃ¼hrt:
  - keine neue Navigationsstruktur
  - kein neuer Rechtepfad
  - keine Ã„nderung an WPF
  - keine RÃ¼ckkehr zur alten `RoleChoicePage`
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ MAUI-Loginflow auf echten Benutzerkontext umgestellt, alte Rollenwahl aus aktivem Flow entfernt

- Den realen Repo-/Arbeitsbaumstand vor Block 1 geprÃ¼ft; blockfremde offene Dateien bewusst unangetastet gelassen.
- WPF als Referenz geprÃ¼ft: dort gibt es nach Login keine manuelle Rollenwahl, sondern direkt den echten `UserContext` mit anschlieÃŸendem Rechtepfad.
- MAUI-Istzustand geprÃ¼ft:
  - `LoginPage` leitete nach erfolgreichem Login noch in `RoleChoicePage` um oder nutzte einen gespeicherten `AppMode`
  - der Shell-/Startpfad hing damit nicht belastbar am echten Benutzerkontext
  - `AdminShell` stÃ¼tzte Teilrechte noch auf `IAuthService.IsAdmin` / `IsVorstand` statt auf den geladenen `UserContext`
- Block 1 gezielt umgesetzt:
  - alte Rollenwahl aus dem aktiven Loginflow entfernt
  - Shell-Auswahl nach Login jetzt direkt aus `IUserContextService.GetUserContextAsync(...)`
  - `UserContextState` wird nach Login mit echtem Rechtekontext befÃ¼llt
  - alter gespeicherter `AppMode` wird fÃ¼r diesen Flow geleert, damit keine frÃ¼here Rollenwahl mehr nachwirkt
  - `AdminShell` richtet Admin-/Vorstand-MenÃ¼s jetzt am geladenen `UserContext` aus
  - `RoleChoicePage` wurde zusÃ¤tzlich aus dem aktiven DI-/Navigationspfad entfernt
- Keine WPF-Datei geÃ¤ndert; MAUI wurde minimal-invasiv an die WPF-Fachlogik angeglichen.
- Technisch verifiziert:
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich

## 2026-03-24 â€“ Root-Folder-Pfad in `kgv-upload-photo` gezielt abgesichert und diagnostisch geschÃ¤rft

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprÃ¼ft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprÃ¼ft:
  - `supabase/functions/kgv-upload-photo/index.ts` brach nach erfolgreicher eigener Auth aktuell im Schritt `root folder validate` ab
  - `supabase/config.toml` blieb unverÃ¤ndert bei `verify_jwt = false` fÃ¼r `kgv-upload-photo`
- Den Root-Folder-Pfad bewusst klein, aber belastbar geschÃ¤rft:
  - `GOOGLE_DRIVE_ROOT_FOLDER_ID` wird jetzt sauber gelesen und getrimmt
  - harmlose Formatdiagnose loggt nur `hasValue`, Roh-/Trim-LÃ¤nge und `trimmedEmpty`
  - der echte Secretwert selbst wird nicht geloggt
- Fehlverhalten jetzt klarer:
  - fehlend/leer => `Google Drive root folder id missing or empty`
  - nicht erreichbar/nicht nutzbar in Drive => schrittbezogene Root-Folder-Lookup-Fehler
- ZusÃ¤tzliche Root-Folder-Logmarker ergÃ¤nzt:
  - `root folder validate start`
  - `root folder validate result`
  - `root folder validate failed`
  - `drive root folder lookup start`
  - `drive root folder lookup success`
- Die Root-Folder-ID wird jetzt nicht nur lokal validiert, sondern vor dem Unterordnerbau einmal klein gegen Drive geprÃ¼ft.
- Keine Ã„nderung an Auth, Secrets, WPF, MAUI oder anderen Functions.
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo --project-ref itjcabiibuodkxayhvjq` erfolgreich

## 2026-03-24 â€“ Diagnoseblock fÃ¼r `kgv-upload-photo` ergÃ¤nzt: HÃ¤nger lokalisierbar, Google-Aufrufe mit Timeouts abgesichert

- Den realen Repo-/Arbeitsbaumstand vor dem Diagnoseblock geprÃ¼ft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprÃ¼ft:
  - `supabase/functions/kgv-upload-photo/index.ts` enthielt bereits die produktive Self-Auth und den Google-Drive-Uploadpfad
  - `supabase/config.toml` blieb korrekt bei `verify_jwt = false` fÃ¼r `kgv-upload-photo`
- Nur den Supabase-Diagnosepfad ergÃ¤nzt, ohne WPF-/MAUI-Dateien oder Secrets anzufassen.
- In `kgv-upload-photo` gezielte Log-Marker ergÃ¤nzt fÃ¼r:
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
- Timeout-/Fetch-Fehler liefern jetzt schrittbezogene Fehlermeldungen statt stillen HÃ¤ngern, z. B. `Google token refresh timeout`, `Drive files.list timeout`, `Drive upload timeout`.
- Datumsinput klein robuster gemacht:
  - bisher nur `YYYY-MM-DD`
  - jetzt zusÃ¤tzlich `dd.MM.yyyy`
  - ungÃ¼ltige Werte liefern eine klare Validierungsfehlermeldung
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo --project-ref itjcabiibuodkxayhvjq` erfolgreich

## 2026-03-24 Ã¢â‚¬â€œ doppelte JWT-PrÃƒÂ¼fung fÃƒÂ¼r `kgv-upload-photo` abgeschaltet

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprÃƒÂ¼ft; blockfremde offene Dateien bewusst unangetastet gelassen.
- Istzustand geprÃƒÂ¼ft: `supabase/config.toml` enthielt fÃƒÂ¼r `kgv-upload-photo` bereits einen Funktionsblock, dort stand aber noch `verify_jwt = true`; der echte Self-Auth-Code lag bereits in `supabase/kgv-upload-photo/index.ts`.
- Wichtiger Repo-Befund: der eigentliche Deploy-Pfad `supabase/functions/kgv-upload-photo/index.ts` war noch ein Placeholder.
- Damit der Deploy die bestehende Funktionsauth nicht ÃƒÂ¼berschreibt, wurde der vorhandene echte Function-Code gezielt in den echten Deploy-Pfad ÃƒÂ¼bernommen.
- Danach in `supabase/config.toml` nur fÃƒÂ¼r `kgv-upload-photo` ergÃƒÂ¤nzt:
  - `[functions.kgv-upload-photo]`
  - `verify_jwt = false`
- Die eigene Function-Auth bleibt aktiv: Bearer-Token lesen, `auth.getUser(jwt)`, RollenprÃƒÂ¼fung auf Admin/Vorstand ÃƒÂ¼ber `app_user.role`.
- Keine Secrets geÃƒÂ¤ndert, keine andere Function auf `verify_jwt = false` gesetzt, keine App-/WPF-/MAUI-Datei angefasst.
- Technisch verifiziert:
  - `supabase functions deploy kgv-upload-photo` erfolgreich


## 2026-03-24 Ã¢â‚¬â€œ WPF-Bindungsfehler in `FotoUploadTestView` bei schreibgeschÃƒÂ¼tzter Rohantwort behoben

- Den realen Repo-/Arbeitsbaumstand vor dem Fix geprÃƒÂ¼ft; blockfremde offene Dateien bewusst nicht angefasst.
- Ursache geprÃƒÂ¼ft: `RawResponseBody` ist im WPF-ViewModel ein reines Anzeige-/Diagnosefeld ohne Setter, wurde in `FotoUploadTestView.xaml` aber an `TextBox.Text` ohne expliziten Modus gebunden.
- Da `TextBox.Text` in WPF standardmÃƒÂ¤ÃƒÅ¸ig `TwoWay` bindet, entstand beim Ãƒâ€“ffnen/Benutzen der DiagnoseflÃƒÂ¤che die bekannte Exception zur schreibgeschÃƒÂ¼tzten Eigenschaft.
- Fix bewusst klein und nur XAML-seitig umgesetzt:
  - `RawResponseBody` in `KGV.Wpf/Views/FotoUploadTestView.xaml` explizit auf `Mode=OneWay` gesetzt
  - `IsReadOnly="True"` blieb erhalten
- Kein kÃƒÂ¼nstlicher Setter im ViewModel, kein Umbau der ÃƒÂ¼brigen Upload-Teststruktur, keine MAUI-Ãƒâ€žnderung.
- Technisch verifiziert:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich


## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Gemeinsamen RFID-Scan-Kontext fÃƒÆ’Ã‚Â¼r `Ablesung erfassen` und `ZÃƒÆ’Ã‚Â¤hlerwechsel` produktiv umgesetzt

- Vor dem Umbau erneut den realen Repo-/Arbeitsbaumstand geprÃƒÆ’Ã‚Â¼ft und blockfremde offene Dateien bewusst unberÃƒÆ’Ã‚Â¼hrt gelassen.
- Den gemeinsamen produktiven RFID-Lesepfad im Shared-Service ergÃƒÆ’Ã‚Â¤nzt:
  - neues Enum `RfidScanContextState`
  - neues Ergebnis-DTO `RfidScanContextResult`
  - neue Service-Methode `ResolveRfidScanContextAsync(...)`
  - UID-Normalisierung bleibt zentral im `SupabaseService`
  - produktiver Lesepfad ist `v_rfid_scan_context`
- `RfidScanContextRecord` am echten View-Vertrag belassen und nur um Anzeige-/Statushilfen ergÃƒÆ’Ã‚Â¤nzt.
- Fachliche Zustandsableitung jetzt zentral und fÃƒÆ’Ã‚Â¼r beide Clients gleich:
  - `Unknown`
  - `KnownWithActiveMeter`
  - `KnownWithoutActiveMeter`
- WPF:
  - vorhandenen Placeholder `RfidScanContextViewModel` / `RfidScanContextView` in echte gemeinsame RFID-Kontextanzeige umgebaut
  - `AblesungErfassenViewModel` und `ZaehlerwechselScanViewModel` auf produktive UID-Eingabe + KontextauflÃƒÆ’Ã‚Â¶sung umgestellt
  - Ergebnisanzeige mit Anlage, Garten, Medium, RFID, aktivem ZÃƒÆ’Ã‚Â¤hler, ZÃƒÆ’Ã‚Â¤hlernummer, Status, Eichdaten
  - workflow-spezifische Einordnung fÃƒÆ’Ã‚Â¼r Ablesung bzw. ZÃƒÆ’Ã‚Â¤hlerwechsel ergÃƒÆ’Ã‚Â¤nzt
- MAUI:
  - gemeinsames `RfidScanContextViewModel` und gemeinsame Basispage `RfidScanWorkflowPage` ergÃƒÆ’Ã‚Â¤nzt
  - `AblesungErfassenPage` und `ZaehlerwechselPage` auf denselben produktiven Kontextpfad umgestellt
  - manuelle UID-Eingabe, KontextauflÃƒÆ’Ã‚Â¶sung und workflow-spezifische Einordnung mobil nutzbar
- Rechte:
  - Bereich bleibt auf Admin/Vorstand begrenzt
  - keine QR- oder Schattenlogik eingefÃƒÆ’Ã‚Â¼hrt
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ `FÃƒÆ’Ã‚Â¤llige ZÃƒÆ’Ã‚Â¤hler` produktiv auf Basis von `v_zaehler_eichstatus` umgesetzt

- Vor Start den realen Repo-/Arbeitsbaumzustand erneut geprÃƒÆ’Ã‚Â¼ft; keine Recovery-Reste als fachliche Grundlage verwendet.
- `FÃƒÆ’Ã‚Â¤llige ZÃƒÆ’Ã‚Â¤hler` in WPF und MAUI war vorher nur als Placeholder vorhanden.
- Gemeinsamen Produktivpfad im Shared-Service ergÃƒÆ’Ã‚Â¤nzt:
  - neues Model `ZaehlerEichstatusRecord` fÃƒÆ’Ã‚Â¼r `v_zaehler_eichstatus`
  - neue Service-Methode `GetZaehlerEichstatusAsync()`
  - Sortierung zentral im Service: zuerst kritisch/fÃƒÆ’Ã‚Â¤llig, dann nach Tagen, dann Garten/Anlage
- WPF:
  - `FaelligeZaehlerViewModel` von Placeholder auf echte ÃƒÆ’Ã…â€œbersicht umgestellt
  - Textfilter, Statusfilter und `Aktualisieren`
  - Tabelle mit Anlage, Garten, Medium, ZÃƒÆ’Ã‚Â¤hler, Eichdatum, EichfÃƒÆ’Ã‚Â¤lligkeit, Status und Tage
  - saubere Leer-/FehlerzustÃƒÆ’Ã‚Â¤nde
- MAUI:
  - `FaelligeZaehlerPage` auf produktive mobile ÃƒÆ’Ã…â€œbersicht umgestellt
  - gleiches Datenmodell und gleicher Shared-Servicepfad
  - Textfilter, Statusfilter, `Aktualisieren`
  - mobile Kartenansicht statt breiter Tabelle
- Rechte explizit auf Admin/Vorstand gehalten.
- Technisch verifiziert:
  - keine passenden TestfÃƒÆ’Ã‚Â¤lle in `KGV.Tests` fÃƒÆ’Ã‚Â¼r diesen Block gefunden
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ `RFID einrichten` produktiv auf neuem DB-Vertrag umgesetzt

- Den aktuellen Repo- und Git-Stand vor dem Umbau erneut geprÃƒÆ’Ã‚Â¼ft.
- Blockfremde offene Dateien im Arbeitsbaum bewusst nicht angerÃƒÆ’Ã‚Â¼hrt; insbesondere offene WPF-UI-Dateien, lokale Artefakte, `_Archiv/_Recovery` und `_AI_DB_EXPORT/AI_DATABASE_CONTEXT.sql` blieben auÃƒÆ’Ã…Â¸en vor.
- FÃƒÆ’Ã‚Â¼r die RFID-Zuordnung wurde der gemeinsame Produktivpfad im Shared-Service ergÃƒÆ’Ã‚Â¤nzt:
  - PrÃƒÆ’Ã‚Â¼fung und Speichern laufen jetzt zentral ÃƒÆ’Ã‚Â¼ber `ISupabaseService` / `SupabaseService`
  - produktiver Schreibpfad ist `assign_parzelle_rfid(...)`
  - KonfliktprÃƒÆ’Ã‚Â¼fung nutzt den realen DB-Bestand ÃƒÆ’Ã‚Â¼ber `parzelle` plus `v_rfid_scan_context`
  - UID wird konsistent getrimmt und in GroÃƒÆ’Ã…Â¸buchstaben normalisiert
- `ParzelleRecord` wurde nur minimal auf den neuen DB-Vertrag erweitert:
  - `Anlage`
  - `hat_strom`
  - `hat_wasser`
  - `rfid_strom`
  - `rfid_wasser`
  - bestehende Altpfade auf getrennten Split-Tabellen wurden in diesem Block nicht zur neuen Grundlage gemacht
- WPF:
  - bestehende Seite `RFID einrichten` von Placeholder auf echte Maske umgestellt
  - Parzellenauswahl, aktuelle Strom-/Wasser-RFID, Medium, UID, `PrÃƒÆ’Ã‚Â¼fen`, `Speichern`
  - ÃƒÆ’Ã…â€œberschreiben vorhandener RFID an derselben Parzelle nur nach expliziter BestÃƒÆ’Ã‚Â¤tigung
  - Konflikt bei bereits anderweitig vergebener UID wird klar blockiert
- MAUI:
  - mobile `RfidEinrichtenPage` auf denselben Shared-Servicepfad umgestellt
  - gleiche Fachschritte wie in WPF
  - ÃƒÆ’Ã…â€œberschreib-BestÃƒÆ’Ã‚Â¤tigung mobil per Dialog
- Rechte:
  - Bereich bleibt explizit auf Admin/Vorstand begrenzt
  - keine ÃƒÆ’Ã¢â‚¬â€œffnung ÃƒÆ’Ã‚Â¼ber das zu breite Flag `CanManageReadings`
- Technisch verifiziert:
  - `KGV.Wpf` baut erfolgreich
  - `KGV.Maui` baut erfolgreich

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ `Ablesen` als eigener Navigationsbereich fÃƒÆ’Ã‚Â¼r Admin/Vorstand in WPF und MAUI angelegt

- Den realen Istzustand vor dem Block geprÃƒÆ’Ã‚Â¼ft:
  - WPF hatte bereits vorbereitete ViewModels fÃƒÆ’Ã‚Â¼r `RfidEinrichten`, `FÃƒÆ’Ã‚Â¤llige ZÃƒÆ’Ã‚Â¤hler` und `ZÃƒÆ’Ã‚Â¤hlerwechsel`, aber noch keinen zentralen Einstiegsbereich `Ablesen`
  - MAUI hatte noch keinen eigenen Ablese-MenÃƒÆ’Ã‚Â¼punkt und keine ÃƒÆ’Ã…â€œbersichtsseite fÃƒÆ’Ã‚Â¼r diese vier Funktionen
  - die bestehende Rollen-/Rechtebasis war bereits vorhanden (`UserContext.Role` in WPF, `IsAdmin` / `IsVorstand` im MAUI-Authpfad)
- Den neuen globalen MenÃƒÆ’Ã‚Â¼punkt `Ablesen` deshalb nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand in beiden UIs ergÃƒÆ’Ã‚Â¤nzt.
- WPF:
  - neue ÃƒÆ’Ã…â€œbersichtsseite `Ablesen` mit vier groÃƒÆ’Ã…Â¸en Kacheln angelegt
  - Kacheln navigieren zu `Ablesung erfassen`, `ZÃƒÆ’Ã‚Â¤hlerwechsel`, `RFID einrichten`, `FÃƒÆ’Ã‚Â¤llige ZÃƒÆ’Ã‚Â¤hler`
  - vorhandene vorbereitete WPF-ViewModels fÃƒÆ’Ã‚Â¼r `ZÃƒÆ’Ã‚Â¤hlerwechsel`, `RFID einrichten` und `FÃƒÆ’Ã‚Â¤llige ZÃƒÆ’Ã‚Â¤hler` weiterverwendet
  - fÃƒÆ’Ã‚Â¼r `Ablesung erfassen` eine schlanke Platzhalterseite ergÃƒÆ’Ã‚Â¤nzt
- MAUI:
  - neuer Flyout-Eintrag `Ablesen` im Admin-/Vorstand-Shell-MenÃƒÆ’Ã‚Â¼
  - neue mobile ÃƒÆ’Ã…â€œbersichtsseite mit vier gut tappbaren Kacheln angelegt
  - vier schlanke Zielseiten/Platzhalter fÃƒÆ’Ã‚Â¼r den weiteren Ablese-Ausbau registriert
- Der Block bleibt bewusst auf Navigation, Rechte und ÃƒÆ’Ã…â€œbersichtsseite beschrÃƒÆ’Ã‚Â¤nkt; tiefe Fachflows folgen erst in den nÃƒÆ’Ã‚Â¤chsten BlÃƒÆ’Ã‚Â¶cken.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Veralteten Remote-Snapshot um fachliche QR-Reste bereinigt

- Den aktuellen Repo-Stand zuerst geprÃƒÆ’Ã‚Â¼ft: fachliche QR-Reste existierten nach der Live-Bereinigung nur noch im veralteten Snapshot `supabase/migrations/20260323093513_remote_schema.sql`.
- Gegen den bereits bereinigten Istzustand aus `_AI_DB_EXPORT/database.types.ts` abgeglichen: `public.parzelle` enthÃƒÆ’Ã‚Â¤lt dort keine QR-Spalten mehr, sondern nur noch die RFID-Felder.
- Den veralteten Remote-Snapshot deshalb auf den aktuellen fachlichen Stand gezogen:
  - `qr_code_wasser` aus der `parzelle`-Tabellendefinition entfernt
  - `qr_code_strom` aus der `parzelle`-Tabellendefinition entfernt
  - die veralteten Unique-Constraints `parzelle_qr_code_wasser_key` und `parzelle_qr_code_strom_key` entfernt
- Ergebnis: im Repo verbleiben in diesem Snapshot keine fachlichen QR-Reste mehr; der Snapshot passt damit wieder zum bereinigten Live-/Types-Stand.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF Button-HÃƒÆ’Ã‚Â¶hen projektweit auf kleine feste FÃƒÆ’Ã‚Â¤lle geprÃƒÆ’Ã‚Â¼ft und angehoben

- Nach dem Fix in der Parzellenzuordnung wurden die WPF-Buttons mit kleinen festen HÃƒÆ’Ã‚Â¶hen projektweit geprÃƒÆ’Ã‚Â¼ft.
- Alle expliziten WPF-ButtonhÃƒÆ’Ã‚Â¶hen unter `35` wurden auf `35` angehoben, damit Beschriftungen nicht mehr zu knapp sitzen.
- Konkret angepasst wurden die betroffenen Buttons in:
  - `GartenStromView`
  - `GartenWasserView`
  - `ArbeitsstundenView`
  - `ChangeEmailWindow`
  - `ResetPasswordWindow`
- Der Fix bleibt rein visuell; Fachlogik und Command-Pfade wurden nicht verÃƒÆ’Ã‚Â¤ndert.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF `MemberDetailView`: Parzellenzuordnungs-Zeile leicht erhÃƒÆ’Ã‚Â¶ht

- Den aktuellen Arbeitsbaum vor dem UI-Fix geprÃƒÆ’Ã‚Â¼ft; blockfremde lokale ÃƒÆ’Ã¢â‚¬Å¾nderungen wie `KGV.Wpf/Views/LoginWindow.xaml` und `KGV.Wpf/Views/MemberDetailView.xaml.cs` bleiben weiter unberÃƒÆ’Ã‚Â¼hrt.
- Die zu kleine HÃƒÆ’Ã‚Â¶he lag nicht in der `MemberDetailView` selbst, sondern in der ausgelagerten `MemberParzellenSection` bei der Zeile fÃƒÆ’Ã‚Â¼r freie Parzelle, Startdatum und die beiden Aktionsbuttons.
- FÃƒÆ’Ã‚Â¼r den kleinen WPF-UI-Fix wurde nur diese Zeile leicht erhÃƒÆ’Ã‚Â¶ht:
  - Zeilen-`StackPanel` mit `MinHeight="36"`
  - Button-HÃƒÆ’Ã‚Â¶hen von `30` auf `35` erhÃƒÆ’Ã‚Â¶ht
- Damit passt die Beschriftung wieder sauber in die Buttons, ohne die restliche Detailansicht oder Fachlogik zu verÃƒÆ’Ã‚Â¤ndern.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Appuser-Verwaltung ins `Admin-MenÃƒÆ’Ã‚Â¼` verschoben und an das ausgewÃƒÆ’Ã‚Â¤hlte Mitglied gebunden

- Den Istzustand zuerst im Repo geprÃƒÆ’Ã‚Â¼ft: `Benutzerverwaltung` hing in WPF noch als globale Navigation, obwohl die fachlichen Appuser-Aktionen immer auf ein konkretes Mitglied zielen sollen. Gleichzeitig war die produktive Nutzeraktion `InviteUserAsync(...)` bereits vorhanden, ein sauberer Entfernen-Pfad aber nicht als eigene UI-gebundene Aktion organisiert.
- Die MenÃƒÆ’Ã‚Â¼struktur wurde deshalb neu geordnet:
  - globaler Top-Level-Eintrag `Benutzerverwaltung` in WPF entfernt
  - stattdessen `Benutzerverwaltung` als eingerÃƒÆ’Ã‚Â¼ckter Unterpunkt im Mitgliedskontext unter `Admin-MenÃƒÆ’Ã‚Â¼`
  - damit bleibt die Navigation eindeutig und es gibt keine Doppelnavigation alt/neu
- Die WPF-`Benutzerverwaltung` arbeitet jetzt strikt auf dem zuvor ausgewÃƒÆ’Ã‚Â¤hlten Mitglied:
  - der Unterpunkt bekommt den aktuellen `SelectedMember` als Parameter
  - beim Laden wird nur noch die Appuser-Zuordnung dieses Mitglieds geladen
  - falls noch kein Appuser existiert, wird ein Platzhalter aus dem ausgewÃƒÆ’Ã‚Â¤hlten Mitglied aufgebaut, damit `Nutzer hinzufÃƒÆ’Ã‚Â¼gen` trotzdem sauber auf genau diesen Datensatz arbeitet
- Die Aktionen wurden fachlich passend umgestellt:
  - `Nutzer hinzufÃƒÆ’Ã‚Â¼gen` nutzt weiter den bestehenden produktiven Pfad `InviteUserAsync(...)`
  - `Nutzer entfernen` entfernt die Appuser-Zuordnung des ausgewÃƒÆ’Ã‚Â¤hlten Mitglieds, nicht das Mitglied selbst
  - ohne Mitgliedskontext laufen die Aktionen nicht und liefern eine klare fachliche RÃƒÆ’Ã‚Â¼ckmeldung statt technischer Fehler
- Rechte wurden sauber getrennt:
  - `Benutzerverwaltung` ist in WPF nur noch im Admin-Mitgliedskontext sichtbar
  - in MAUI wurde die MenÃƒÆ’Ã‚Â¼-Sichtbarkeit ebenfalls auf echte Admins beschrÃƒÆ’Ã‚Â¤nkt; Vorstand sieht den Punkt dort ebenfalls nicht
- Technisch verifiziert: `KGV.Wpf` baut nach der Umordnung erfolgreich; der MAUI-Pfad wurde im MenÃƒÆ’Ã‚Â¼-/Rechteverhalten mitgezogen und nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Mitgliedersuche um E-Mail, Gartennummern und Hauptmitglied-Markierung erweitert

- Den Istzustand zuerst im Repo geprÃƒÆ’Ã‚Â¼ft: die bestehende Mitgliedersuche zeigte bisher in WPF nur Name/Vorname und in MAUI nur Titel/Unterzeile. Ein neuer Suchdialog oder neue Fachlogik waren dafÃƒÆ’Ã‚Â¼r nicht nÃƒÆ’Ã‚Â¶tig.
- Die Erweiterung bleibt im bestehenden Suchpfad und nutzt nur bereits vorhandene Datenquellen:
  - Mitgliedsdaten aus `GetMitgliederAsync()`
  - Parzellen aus `GetAllParzellenAsync()`
  - aktuelle Belegungen aus `GetAllParzellenBelegungenAsync()`
- Daraus wird jetzt fÃƒÆ’Ã‚Â¼r die Ergebnisliste angereichert:
  - E-Mailadresse
  - Gartennummern aus aktuell aktiven Belegungen, falls vorhanden
  - Hauptmitglied-Markierung aus `hauptmitglied_id`
- WPF wurde direkt in der bestehenden GridView erweitert um die zusÃƒÆ’Ã‚Â¤tzlichen Spalten `E-Mail`, `Gartennummern` und `Hauptmitglied` als deaktivierte Checkbox.
- MAUI wurde passend mitgedacht und die bestehende Suchliste ebenfalls um Gartennummern und Hauptmitglied-Markierung ergÃƒÆ’Ã‚Â¤nzt; die E-Mail bleibt dort Teil der sichtbaren Ergebnisinfos. Es wurde kein zweiter Suchpfad aufgebaut.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Erweiterung erfolgreich.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitseinsatz-Teilnehmerliste: `Abmelden` pro Zeile produktiv ÃƒÆ’Ã‚Â¼ber echten DB-Pfad ergÃƒÆ’Ã‚Â¤nzt

- Den Istzustand zuerst gegen Repo und `_AI_DB_EXPORT` geprÃƒÆ’Ã‚Â¼ft. `database.types.ts` bestÃƒÆ’Ã‚Â¤tigt fÃƒÆ’Ã‚Â¼r diesen Block ausdrÃƒÆ’Ã‚Â¼cklich neben `sign_up_for_arbeitseinsatz(...)` auch `sign_off_from_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` als echten DB-Funktionspfad; `roles.sql` enthÃƒÆ’Ã‚Â¤lt dafÃƒÆ’Ã‚Â¼r keine abweichende App-Sonderregel, `AI_DATABASE_CONTEXT.sql` liefert keinen alternativen Pfad.
- `sign_off_from_arbeitseinsatz(...)` war App-seitig vor diesem Block noch nicht produktiv verwendet. Der neue Abmeldepfad wurde deshalb sauber in den Shared-Service ergÃƒÆ’Ã‚Â¤nzt statt als direkter Tabellenhack daneben aufgebaut.
- In der Arbeitseinsatz-Detailview fÃƒÆ’Ã‚Â¼r Admin/Vorstand wurde pro Teilnehmerzeile der Button `Abmelden` ergÃƒÆ’Ã‚Â¤nzt. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch Zeilenaktion.
- Der Button lÃƒÆ’Ã‚Â¤uft mit kleiner RÃƒÆ’Ã‚Â¼ckfrage und ruft danach produktiv `SignOffFromArbeitseinsatzAsync(...)` auf; dieser Shared-Service nutzt den echten RPC-/DB-Funktionspfad `sign_off_from_arbeitseinsatz(...)`.
- Nach erfolgreicher oder fachlich abgefangener RÃƒÆ’Ã‚Â¼ckmeldung werden Teilnehmerliste und Detailzustand direkt neu geladen; damit aktualisieren sich auch KapazitÃƒÆ’Ã‚Â¤ts-/Anmeldeinformationen ÃƒÆ’Ã‚Â¼ber den bestehenden Detailpfad mit.
- Technisch verifiziert: `KGV.Wpf` baut nach dem neuen Abmeldepfad erfolgreich; MAUI wurde im Shared-Servicepfad mitgedacht und nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitseinsatz-Block final verifiziert und sauber abgeschlossen

- Den aktuellen Arbeitsbaum erneut geprÃƒÆ’Ã‚Â¼ft: blockeigene CodeÃƒÆ’Ã‚Â¤nderungen aus diesem Arbeitseinsatz-Block waren bereits im Repo enthalten; lokal offen blieben nur blockfremde ÃƒÆ’Ã¢â‚¬Å¾nderungen und Artefakte wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Hilfs-/Exportdateien.
- Den bereits implementierten Zustand nochmals final verifiziert:
  - `Anmelden` bleibt in Home und Detail am selben echten Pfad `SignUpForArbeitseinsatzAsync(...)`
  - der Shared-Service nutzt weiter produktiv `sign_up_for_arbeitseinsatz(...)`
  - Teilnehmerliste erscheint nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand in der Detailview
  - `HinzufÃƒÆ’Ã‚Â¼gen` erscheint nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand
  - `HinzufÃƒÆ’Ã‚Â¼gen` verwendet die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃƒÆ’Ã‚Â¤hlte bestehende Mitglied wird ÃƒÆ’Ã‚Â¼ber denselben RPC-Pfad eingetragen wie bei Selbstanmeldung
- Die Sichtbarkeit von `Anmelden` wird weiterhin belastbar aus realem Zustand bestimmt: Basisdaten aus `arbeitseinsatz` plus aktive `arbeitseinsatz_anmeldung` gegen Frist, Platzgrenze und bereits vorhandene Anmeldung des aktuellen Mitglieds.
- `KGV.Wpf` wurde final erfolgreich gebaut. Ein erster Buildlauf scheiterte nur an einer laufenden gesperrten `KGV.Wpf`-Instanz; nach Beenden der App lief der Build sauber durch. Der Block ist damit technisch und fachlich abgeschlossen.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitseinsatz: `Anmelden`-Regression und Admin-/Vorstand-Teilnehmerblock sauber abgeschlossen

- Den begonnenen Block erneut gegen den realen Arbeitsbaum und ausdrÃƒÆ’Ã‚Â¼cklich gegen `_AI_DB_EXPORT` geprÃƒÆ’Ã‚Â¼ft. Belastbar herangezogen wurden wieder `database.types.ts` mit `sign_up_for_arbeitseinsatz(...)` und `arbeitseinsatz_anmeldung`; `roles.sql` enthÃƒÆ’Ã‚Â¤lt dafÃƒÆ’Ã‚Â¼r keine abweichende Zusatzregel, `AI_DATABASE_CONTEXT.sql` liefert hier keinen zusÃƒÆ’Ã‚Â¤tzlichen App-Pfad.
- Die Regression beim verschwundenen `Anmelden` lag nicht am RPC selbst, sondern an der Sichtbarkeit: `CanRegister` wurde im sichtbaren UI zu eng nur aus dem Startseitenpfad ÃƒÆ’Ã‚Â¼bernommen. Der Shared-Service bestimmt die sichtbare Anmeldung jetzt wieder aus den realen Basisdaten von `arbeitseinsatz` plus aktiven `arbeitseinsatz_anmeldung`-DatensÃƒÆ’Ã‚Â¤tzen fÃƒÆ’Ã‚Â¼r den aktuellen Benutzerkontext, inklusive Frist, Platzgrenze und bestehender Anmeldung.
- Home und Detail bleiben auf demselben echten Produktpfad: beide rufen weiterhin `SignUpForArbeitseinsatzAsync(...)` auf, das produktiv ÃƒÆ’Ã‚Â¼ber `sign_up_for_arbeitseinsatz(...)` schreibt. Es wurde keine zweite Schreiblogik erÃƒÆ’Ã‚Â¶ffnet.
- FÃƒÆ’Ã‚Â¼r Admin/Vorstand wurde die Detailview klein und rollenabhÃƒÆ’Ã‚Â¤ngig ergÃƒÆ’Ã‚Â¤nzt:
  - reale Teilnehmerliste ÃƒÆ’Ã‚Â¼ber aktive `arbeitseinsatz_anmeldung`
  - `HinzufÃƒÆ’Ã‚Â¼gen`-Button nur fÃƒÆ’Ã‚Â¼r Admin/Vorstand
  - Auswahl ausschlieÃƒÆ’Ã…Â¸lich ÃƒÆ’Ã‚Â¼ber die bestehende Maske `Mitglied suchen` im Auswahlmodus
  - das ausgewÃƒÆ’Ã‚Â¤hlte bestehende Mitglied wird danach ÃƒÆ’Ã‚Â¼ber denselben RPC-Pfad angemeldet wie bei Selbstanmeldung
- Dabei gibt es bewusst keine freie Texteingabe und keine PrÃƒÆ’Ã‚Â¼fung, ob das gewÃƒÆ’Ã‚Â¤hlte Mitglied App-User ist. Normale Nutzer sehen weiterhin weder Teilnehmerliste noch `HinzufÃƒÆ’Ã‚Â¼gen`.
- Technisch finalisiert: kleine Warnungsbereinigung im Auswahlpfad abgeschlossen und `KGV.Wpf` baut erfolgreich; der Block ist damit jetzt sauber abschlieÃƒÆ’Ã…Â¸bar.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ `Anmelden` fÃƒÆ’Ã‚Â¼r Arbeitseinsatz im sichtbaren UI tatsÃƒÆ’Ã‚Â¤chlich funktionsfÃƒÆ’Ã‚Â¤hig gemacht

- Den realen Fehler ausdrÃƒÆ’Ã‚Â¼cklich am laufenden WPF-Pfad geprÃƒÆ’Ã‚Â¼ft: Home-Button und Detail-Button waren bereits an denselben Shared-Servicepfad gebunden, der sichtbare HÃƒÆ’Ã‚Â¤nger lag aber davor im ÃƒÆ’Ã‚Â¼bergebenen Datensatz.
- Die konkrete Ursache: `MapHomeWorkAssignment(...)` ÃƒÆ’Ã‚Â¼bernahm die `id` des Arbeitseinsatzes nicht in das sichtbare `HomeWorkAssignmentItem`. Dadurch lief die Detailansicht mit `WorkAssignmentId = 0` in einen stillen Vorab-Abbruch und auch der Home-Pfad arbeitete nicht auf einem belastbaren DatensatzschlÃƒÆ’Ã‚Â¼ssel.
- Der Fix bleibt klein und ehrlich:
  - `SupabaseService` schreibt `record.Id` jetzt in das sichtbare Home-Item
  - `HomeViewModel` ÃƒÆ’Ã‚Â¼bergibt den Detailbutton nur noch dann als sichtbar, wenn `CanRegister` tatsÃƒÆ’Ã‚Â¤chlich gilt
  - `HomeView.xaml` zeigt den Home-Button ebenfalls nur bei `CanRegister`
  - `HomeSectionDetailViewModel` meldet einen ungÃƒÆ’Ã‚Â¼ltigen Detailkontext jetzt sichtbar statt still zu returnen
- Ergebnis: Home und Detail nutzen weiterhin denselben RPC-Servicepfad, reagieren jetzt aber auch im sichtbaren UI belastbar statt mit stillem No-Op. Der Admin-/Vorstand-Teilnehmerblock blieb bewusst unberÃƒÆ’Ã‚Â¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten UI-/Datensatzfix erfolgreich; MAUI wurde nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ RPC-Anmeldeblock fÃƒÆ’Ã‚Â¼r Arbeitseinsatz final verifiziert, committed und gepusht

- Den bereits umgesetzten RPC-Pfad fÃƒÆ’Ã‚Â¼r `Anmelden` nochmals final gegen den realen Arbeitsbaum geprÃƒÆ’Ã‚Â¼ft und `KGV.Wpf` erfolgreich gebaut.
- FÃƒÆ’Ã‚Â¼r den Abschluss wurden ausschlieÃƒÆ’Ã…Â¸lich die zu diesem Block gehÃƒÆ’Ã‚Â¶renden Dateien gestaged; blockfremde lokale ÃƒÆ’Ã¢â‚¬Å¾nderungen wie `KGV.Wpf/Views/LoginWindow.xaml` sowie untracked Artefakte blieben unberÃƒÆ’Ã‚Â¼hrt.
- Kurz funktional verifiziert per Codepfad: Home und Detailview rufen denselben Shared-Service `SignUpForArbeitseinsatzAsync(...)` auf; Doppelanmeldung, Frist und Platzgrenze werden vor dem RPC geprÃƒÆ’Ã‚Â¼ft und zusÃƒÆ’Ã‚Â¤tzlich im RPC-Fehlerfall sauber in verstÃƒÆ’Ã‚Â¤ndliche RÃƒÆ’Ã‚Â¼ckmeldungen ÃƒÆ’Ã‚Â¼bersetzt.
- Der Block ist damit sauber als eigener Commit abgeschlossen und direkt gepusht.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ `Anmelden` fÃƒÆ’Ã‚Â¼r Arbeitseinsatz produktiv ÃƒÆ’Ã‚Â¼ber den echten DB-Funktionspfad angebunden

- Den Block ausdrÃƒÆ’Ã‚Â¼cklich gegen den referenzierten DB-Export `_AI_DB_EXPORT` gefÃƒÆ’Ã‚Â¼hrt. Belastbar genutzt wurden vor allem `database.types.ts` sowie die dort bestÃƒÆ’Ã‚Â¤tigten Funktionen `sign_up_for_arbeitseinsatz(p_arbeitseinsatz_id, p_mitglied_id)` und `sign_off_from_arbeitseinsatz(...)`, auÃƒÆ’Ã…Â¸erdem die Tabelle `arbeitseinsatz_anmeldung` und das Enum `arbeitseinsatz_anmeldung_status`. `roles.sql` wurde als Zusatzkontext geprÃƒÆ’Ã‚Â¼ft; dort liegen keine abweichenden App-seitigen Sonderregeln fÃƒÆ’Ã‚Â¼r diesen Block.
- Daraus den fachlich richtigen Schreibpfad abgeleitet: die App schreibt die Anmeldung nicht als Schatteninsert direkt in `arbeitseinsatz_anmeldung`, sondern nutzt produktiv die vorhandene DB-Funktion `sign_up_for_arbeitseinsatz(...)`. Der direkte Tabellenzugriff bleibt nur lesend fÃƒÆ’Ã‚Â¼r VorabprÃƒÆ’Ã‚Â¼fungen und Status-/KapazitÃƒÆ’Ã‚Â¤tsbewertung.
- Im Shared-Service wurde dafÃƒÆ’Ã‚Â¼r ein echter Produktpfad ergÃƒÆ’Ã‚Â¤nzt: `SignUpForArbeitseinsatzAsync(int arbeitseinsatzId, int mitgliedId)` prÃƒÆ’Ã‚Â¼ft zuerst sauber Mitglied, bestehenden Datensatz, aktive Anmeldungen, Frist und Platzgrenze und ruft dann den bestÃƒÆ’Ã‚Â¤tigten RPC-Pfad auf.
- Doppelanmeldungen werden dabei zweistufig verhindert: vor dem RPC ÃƒÆ’Ã‚Â¼ber eine gezielte Lesesicht auf `arbeitseinsatz_anmeldung` mit `status = angemeldet`, zusÃƒÆ’Ã‚Â¤tzlich ÃƒÆ’Ã‚Â¼ber den DB-Funktionspfad selbst. Falls der RPC in einem Rennen dennoch in einen bereits angemeldeten Zustand lÃƒÆ’Ã‚Â¤uft, wird das klein abgefangen und als verstÃƒÆ’Ã‚Â¤ndliche RÃƒÆ’Ã‚Â¼ckmeldung ausgegeben statt als technischer Rohfehler.
- Home und Detailview verwenden jetzt denselben echten Servicepfad statt doppelter ViewModel-Logik:
  - Home nutzt weiter `RegisterForWorkAssignmentCommand`, ruft aber jetzt den Shared-Service auf
  - die Detailansicht nutzt denselben Servicepfad ÃƒÆ’Ã‚Â¼ber den im Kontext mitgegebenen `WorkAssignmentId`
  - beide erhalten dieselbe verstÃƒÆ’Ã‚Â¤ndliche Erfolgs-/FehlerrÃƒÆ’Ã‚Â¼ckmeldung
- Der Mitgliedsbezug bleibt beim real vorhandenen Benutzerpfad: bevorzugt wird `UserContext.MitgliedId`, mit kleinem Fallback ÃƒÆ’Ã‚Â¼ber `EnsureCurrentMemberSelectedAsync()`; es wurde keine neue Sonderzuordnung eingefÃƒÆ’Ã‚Â¼hrt.
- Nach erfolgreicher Anmeldung wird der sichtbare Zustand klein aktualisiert: der betroffene Home-Eintrag bzw. der Detailkontext erhÃƒÆ’Ã‚Â¤lt den aktualisierten Startseiten-Datensatz zurÃƒÆ’Ã‚Â¼ck und der Button wird ehrlich deaktiviert, damit keine zweite sofortige Anmeldung ausgelÃƒÆ’Ã‚Â¶st wird. Eine Abmeldung oder Warteliste wurde bewusst nicht neu erÃƒÆ’Ã‚Â¶ffnet.
- Technisch verifiziert: `KGV.Wpf` baut nach dem produktiven Arbeitseinsatz-Anmeldeblock erfolgreich; MAUI wurde im Shared-Core-/Servicepfad mitgedacht und nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-Zeitwerte fÃƒÆ’Ã‚Â¼r Arbeitseinsatz und Termin final aus dem echten Home-Lesepfad nachgereicht

- Den verbleibenden Sichtbarkeitsfehler nach dem expliziten WPF-Binding nochmals gegen den kompletten sichtbaren Pfad geprÃƒÆ’Ã‚Â¼ft. Ergebnis: die WPF-Views waren bereits korrekt auf Start-/Endzeit gebunden; der Restfehler saÃƒÆ’Ã…Â¸ davor im Home-Lesepfad, weil `v_startseite_arbeitseinsatz` bzw. `v_startseite_termine` die Zeitfelder im aktuellen Stand nicht in jedem Fall belastbar lieferten.
- Der finale Fix bleibt deshalb klein und produktnah im bestehenden Home-Servicepfad: `LoadStartseiteArbeitseinsaetzeAsync()` und `LoadStartseiteTermineAsync()` laden die Startseiten-Records weiter aus den bestehenden Views, reichern fehlende `Beginn`-/`Ende`-Werte bei Bedarf aber gezielt aus den Basistabellen `arbeitseinsatz` bzw. `termin` anhand der bereits vorhandenen Datensatz-`Id` an.
- Damit ist jetzt belastbar abgesichert: `start_uhrzeit` und `end_uhrzeit` kommen nicht nur in der Basistabelle vor, sondern erreichen den tatsÃƒÆ’Ã‚Â¤chlich sichtbaren WPF-Pfad auch dann, wenn die Startseiten-View sie im aktuellen Stand leer lÃƒÆ’Ã‚Â¤sst. Die sichtbare Bindung in Home und Detail kann damit auf denselben expliziten Start-/Endfeldern bleiben.
- Keine neue Fachlogik, keine neue Navigation und keine Home-Schattenarchitektur: es bleibt beim vorhandenen Home-Lesepfad plus kleiner Fallback-Anreicherung nur fÃƒÆ’Ã‚Â¼r fehlende Zeitwerte.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Home-Zeitfix erfolgreich; MAUI wurde durch die kleine Shared-Service-ErgÃƒÆ’Ã‚Â¤nzung nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Start- und Endzeit im tatsÃƒÆ’Ã‚Â¤chlich sichtbaren WPF-Pfad final sichtbar gemacht

- Den sichtbaren Restfehler nochmals Ende-zu-Ende gegen den tatsÃƒÆ’Ã‚Â¤chlichen WPF-Pfad geprÃƒÆ’Ã‚Â¼ft: `SupabaseService`, Home-Items, `HomeViewModel`, `HomeSectionDetailContext`, `HomeSectionDetailViewModel`, `HomeView.xaml` und `HomeSectionDetailView.xaml`. Ergebnis: `start_uhrzeit`/`end_uhrzeit` kamen im Mapping als normalisierte Werte an, hingen im sichtbaren UI aber weiterhin zu stark an einem zusammengesetzten Zeittext bzw. indirekten Anzeigeweg.
- Die endgÃƒÆ’Ã‚Â¼ltige Korrektur setzt deshalb nicht mehr nur auf einen abstrakten Sammelstring, sondern auf explizite Start-/Endzeit-Properties im tatsÃƒÆ’Ã‚Â¤chlich sichtbaren Pfad: `HomeWorkAssignmentItem` und `HomeAppointmentItem` tragen jetzt `StartTimeText` und `EndTimeText`; der Detailkontext ÃƒÆ’Ã‚Â¼bernimmt dieselben Felder 1:1 fÃƒÆ’Ã‚Â¼r genau den ausgewÃƒÆ’Ã‚Â¤hlten Datensatz.
- In `SupabaseService` werden die vorhandenen `Beginn`-/`Ende`-Werte weiterhin zentral ÃƒÆ’Ã‚Â¼ber `NormalizeTimeValue(...)` normalisiert und jetzt explizit in diese Start-/Endfelder geschrieben. Damit ist belastbar nachvollziehbar: die Zeitwerte kommen im Modell an und werden nicht mehr erst spÃƒÆ’Ã‚Â¤t oder indirekt aus Nebenfeldern zusammengesetzt.
- Der sichtbare WPF-Pfad wurde anschlieÃƒÆ’Ã…Â¸end passend darauf verdrahtet: auf Home bleibt die Zeitzeile als klarer Zeitraum aus genau dieser Quelle sichtbar; in der Detailansicht werden `Beginn` und `Ende` jetzt explizit als eigene Datensatzangaben gerendert statt nur als zusammengesetzte Sammelzeile.
- `Bekanntmachung` bleibt unverÃƒÆ’Ã‚Â¤ndert stabil: dort werden die neuen Zeitfelder leer ÃƒÆ’Ã‚Â¼bergeben, sodass keine Regression in der generischen Detailstruktur entsteht.
- Technisch verifiziert: `KGV.Wpf` baut nach dem finalen Sichtbarkeitsfix erfolgreich; MAUI wurde durch die kleinen Modell-/KontextergÃƒÆ’Ã‚Â¤nzungen nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-/Detailanzeige fÃƒÆ’Ã‚Â¼r Arbeitseinsatz und Termin korrigiert: Uhrzeit explizit sichtbar, doppelte `Angemeldet`-Anzeige entfernt

- Den aktuellen Istzustand des bestehenden Anzeige-/Mappingpfads erneut gegen `HomeView.xaml`, `HomeSectionDetailView.xaml`, `HomeViewModel`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, `HomeDashboardItems` und `SupabaseService` geprÃƒÆ’Ã‚Â¼ft. Ergebnis: die Uhrzeit lief bislang teils nur indirekt im `Subtitle` bzw. als separater Beginn-Text, wÃƒÆ’Ã‚Â¤hrend `Arbeitseinsatz`-KapazitÃƒÆ’Ã‚Â¤tsinfos zugleich in `DetailInfo` und `RegistrationInfo` landeten.
- Die Home-Anzeige wurde deshalb auf einen eindeutigen Sichtpfad umgestellt: `Arbeitseinsatz` und `Termin` erhalten jetzt jeweils einen expliziten `TimeText`, der ÃƒÆ’Ã‚Â¼ber `BuildTimeRange(...)` aus Start- und Endzeit erzeugt und in Home sichtbar als `Uhrzeit` gerendert wird. Damit ist mindestens die Startzeit sichtbar; wenn Ende vorhanden ist, wird konsistent `Start ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Ende` angezeigt.
- Die Detailview nutzt denselben expliziten Zeitpfad jetzt ebenfalls fÃƒÆ’Ã‚Â¼r genau den ausgewÃƒÆ’Ã‚Â¤hlten Datensatz: `HomeSectionDetailContext` trÃƒÆ’Ã‚Â¤gt `TimeText`, `HomeSectionDetailViewModel` reicht ihn 1:1 weiter, und `HomeSectionDetailView.xaml` zeigt ihn separat als `Uhrzeit` an. Dadurch werden Start- und Endzeit fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` und `Termin` sichtbar, ohne neue Listendarstellung oder Sammelkontexte einzufÃƒÆ’Ã‚Â¼hren.
- Die doppelte Anzeige `Angemeldet: 0` kam aus zwei Quellen desselben Datensatzes: einmal aus `DetailInfo` (`Angemeldet`, `Freie PlÃƒÆ’Ã‚Â¤tze`) und zusÃƒÆ’Ã‚Â¤tzlich nochmals aus `RegistrationInfo` ÃƒÆ’Ã‚Â¼ber `BuildCapacityText(...)`. Der Mappingpfad fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` wurde deshalb bereinigt: KapazitÃƒÆ’Ã‚Â¤tsinfos bleiben jetzt nur noch in `RegistrationInfo`; aus `DetailInfo` wurden die redundanten `Angemeldet`-/`Freie PlÃƒÆ’Ã‚Â¤tze`-Zeilen entfernt.
- `DetailInfo` bleibt damit auf die ÃƒÆ’Ã‚Â¼brigen Felder des ausgewÃƒÆ’Ã‚Â¤hlten Datensatzes fokussiert (`Thema`, `Datum`, `Treffpunkt`, ggf. `Max. Teilnehmer`), wÃƒÆ’Ã‚Â¤hrend Registrierungs-/KapazitÃƒÆ’Ã‚Â¤tsinfos nur noch einmal sichtbar sind. So bleibt die Detailview weiter auf genau einen Datensatz begrenzt, aber ohne redundante Anzeige-Fragmente.
- Die generische Detailstruktur fÃƒÆ’Ã‚Â¼r `Bekanntmachung` wurde mitgeprÃƒÆ’Ã‚Â¼ft und nicht verschlechtert: dort bleibt `TimeText` leer, wÃƒÆ’Ã‚Â¤hrend `Subtitle`, `AdditionalInfo` und `Content` unverÃƒÆ’Ã‚Â¤ndert funktionieren.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Anzeige-Fix erfolgreich; MAUI wurde durch die kleinen Home-/Detailmodell- und Mappinganpassungen nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Home-/Detailansicht fÃƒÆ’Ã‚Â¼r ArbeitseinsÃƒÆ’Ã‚Â¤tze und Termine vervollstÃƒÆ’Ã‚Â¤ndigt: Startzeit sichtbar, Detailinfos vollstÃƒÆ’Ã‚Â¤ndig, Detailkontext bleibt beim ausgewÃƒÆ’Ã‚Â¤hlten Datensatz

- Den bestehenden Home-/Detailpfad vor dem Fix gegen die realen WPF-Dateien und das aktuelle Mapping geprÃƒÆ’Ã‚Â¼ft: `HomeView`, `HomeViewModel`, `HomeSectionDetailView`, `HomeSectionDetailViewModel`, `HomeSectionDetailContext`, die Home-Items sowie das Mapping in `SupabaseService` waren bereits generisch vorhanden; `Arbeitseinsatz` zeigte die Startzeit auf Home schon separat, `Termin` dagegen noch nicht.
- Die Home-ÃƒÆ’Ã…â€œbersicht wurde deshalb gezielt vervollstÃƒÆ’Ã‚Â¤ndigt, ohne neue Sondernavigation zu bauen: `Termin` zeigt die Startzeit jetzt wie `Arbeitseinsatz` zusÃƒÆ’Ã‚Â¤tzlich sichtbar in der Karte an. FÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` blieb die vorhandene konsistente Darstellung mit sichtbarem Beginn erhalten.
- Die Detailview wurde innerhalb der bestehenden Struktur ergÃƒÆ’Ã‚Â¤nzt statt neu aufgebaut: zusÃƒÆ’Ã‚Â¤tzlich zu `Title`, `Subtitle`, `AdditionalInfo` und `Content` zeigt sie jetzt auch die bereits aus dem gewÃƒÆ’Ã‚Â¤hlten Datensatz stammende Registrierungsinformation an. Dadurch bleiben fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` und `Termin` die restlichen zum Datensatz gehÃƒÆ’Ã‚Â¶renden Angaben sichtbar, ohne Felder aus anderen EintrÃƒÆ’Ã‚Â¤gen zu vermischen.
- Den Punkt ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nur ausgewÃƒÆ’Ã‚Â¤hlten Datensatz anzeigenÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ explizit auf den bestehenden Kontextpfad abgesichert: `HomeViewModel` ÃƒÆ’Ã‚Â¼bergibt an die Detailansicht weiterhin nur skalare Kontextdaten des konkret angeklickten Eintrags (`Title`, `Subtitle`, `Content`, `AdditionalInfo`, Registrierungsinfo/Anmeldeaktion). Es wird keine Liste und kein unscharfer Sammelkontext an die Detailview gereicht.
- Der `Anmelden`-Button ist in der Detailview jetzt konsistent fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` sichtbar und nutzt denselben ehrlichen Pfad wie auf Home: derselbe Hinweisdialog, keine neue Fake-Fachlogik und weiterhin kein erfundener Schreibzugriff, solange der echte belastbare Anmeldepfad im Repo fehlt.
- `Bekanntmachung` wurde im selben generischen Detailpfad mitgeprÃƒÆ’Ã‚Â¼ft: die bestehende Darstellung ÃƒÆ’Ã‚Â¼ber `Title`, `Subtitle`, `AdditionalInfo` und `Content` bleibt erhalten; es geht dort kein Feld verloren und es wurde keine kaputte Spezialdarstellung eingefÃƒÆ’Ã‚Â¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Home-/Detailblock erfolgreich; MAUI wurde durch die gezielten Shared-/Kontextanpassungen nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-24 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Arbeitsstunden endgÃƒÆ’Ã‚Â¼ltig gegen `id = 0` abgesichert, Save-RÃƒÆ’Ã‚Â¼cknavigation nachgezogen, Arbeitseinsatz-Home-Buttons korrigiert

- Den erneuten realen Fehlernachweis `arbeitsstunde.id = 0` nach dem vorigen Fix ausdrÃƒÆ’Ã‚Â¼cklich als Beleg genommen, dass Attributverhalten allein im laufenden Pfad nicht reicht. Deshalb den kompletten Produktpfad nochmals bis zur tatsÃƒÆ’Ã‚Â¤chlichen Create-Payload geprÃƒÆ’Ã‚Â¼ft: WPF-Erfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogpfad (`ArbeitsstundenViewModel`), MAUI-Erfassung (`MyArbeitsstundenPage`) und `SupabaseService.AddArbeitsstundeAsync(...)`.
- Die robuste Korrektur liegt jetzt nicht mehr nur im Record-Attribut, sondern technisch im Insertmodell selbst: fÃƒÆ’Ã‚Â¼r `arbeitsstunde` gibt es nun einen expliziten Payloadtyp ohne `Id` (`ArbeitsstundeInsertRecord`), und `AddArbeitsstundeAsync(...)` schreibt beim Insert nur noch ÃƒÆ’Ã‚Â¼ber genau diesen Typ. Damit kann `Id` ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ auch wenn aufrufende `ArbeitsstundeRecord`-Instanzen lokal weiter mit Default `0` ankommen ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ technisch nicht mehr in die Create-Payload gelangen.
- Als zusÃƒÆ’Ã‚Â¤tzliche Guard-Absicherung ÃƒÆ’Ã‚Â¼bernimmt der Create-Pfad die `Id` grundsÃƒÆ’Ã‚Â¤tzlich ÃƒÆ’Ã‚Â¼berhaupt nicht mehr. Es wird weder auf positive noch auf nichtpositive `Id` vertraut; Updates bleiben weiterhin separat ÃƒÆ’Ã‚Â¼ber `UpdateArbeitsstundeAsync(...)` mit echter bestehender `Id` bestehen. Eine App-seitige `lastrow+1`-Logik wurde bewusst nicht eingefÃƒÆ’Ã‚Â¼hrt.
- Die Arbeitsstunden-Erfassungsansicht wurde im Abschlusszug ebenfalls auf das gewÃƒÆ’Ã‚Â¼nschte Produktverhalten gezogen: nach erfolgreichem Speichern bleibt die Eingabemaske nicht mehr offen stehen, sondern verlÃƒÆ’Ã‚Â¤sst den Pfad sauber. Im Dialogkontext schlieÃƒÆ’Ã…Â¸t sich das Fenster wie bisher; im normalen WPF-Erfassungspfad wird nach erfolgreichem Save zurÃƒÆ’Ã‚Â¼ck auf `Home` navigiert.
- Die Arbeitsstunden-Freigabeansicht zieht jetzt dasselbe Abschlussverhalten nach: wenn alle markierten/geÃƒÆ’Ã‚Â¤nderten Zeilen erfolgreich gespeichert wurden, wird nicht in der PrÃƒÆ’Ã‚Â¼fansicht verharrt, sondern wieder sauber auf `Home` zurÃƒÆ’Ã‚Â¼cknavigiert. Der bestehende Fachpfad bleibt unverÃƒÆ’Ã‚Â¤ndert; `status` bleibt optionales Anmerkungsfeld.
- Den Home-Eintrag fÃƒÆ’Ã‚Â¼r `Arbeitseinsatz` auf den gewÃƒÆ’Ã‚Â¼nschten Interaktionsstand korrigiert: der separate `Details`-Button wurde entfernt, DetailÃƒÆ’Ã‚Â¶ffnung lÃƒÆ’Ã‚Â¤uft jetzt per Doppelklick auf die Karte. Stattdessen bleibt dort der Button `Anmelden` sichtbar. Weil weiterhin kein belastbarer produktiver Schreibpfad fÃƒÆ’Ã‚Â¼r die echte Anmeldung vorhanden ist, bleibt dieser Button bewusst eine ehrliche Info-Aktion statt neuer Fake-Fachlogik.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Abschlussblock erfolgreich; MAUI wurde im Shared-Arbeitsstundenpfad mitgedacht und nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ Zwei echte Arbeitsstundenfehler behoben: `id = 0` bei Neuanlage unterbunden und Freigabe-Checkbox aktiviert Speichern wieder sofort

- Den realen Produktfehler im Arbeitsstunden-Insertpfad diesmal bewusst bis zur tatsÃƒÆ’Ã‚Â¤chlich laufenden Payload verfolgt statt nur den Service isoliert zu betrachten: WPF-Usererfassung (`ArbeitsstundenErfassungViewModel`), WPF-Dialogerfassung (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) erzeugen alle neue `ArbeitsstundeRecord`-Instanzen ohne explizite `Id`-Zuweisung, damit aber typbedingt mit lokalem Default `Id = 0`.
- Die eigentliche Ursache lag im Modellvertrag von `ArbeitsstundeRecord`: anders als bei den ÃƒÆ’Ã‚Â¼brigen produktiven Create-Modellen stand dort noch `[PrimaryKey("id")]` ohne explizites `shouldInsert = false`. Im aktuell laufenden Paketstand `Supabase.Postgrest 4.1.0` konnte die PrimÃƒÆ’Ã‚Â¤rschlÃƒÆ’Ã‚Â¼sselspalte dadurch im realen Insertpfad weiter in die Payload gelangen; genau so lieÃƒÆ’Ã…Â¸ sich der reproduzierte neue Datensatz mit `id = 0` erklÃƒÆ’Ã‚Â¤ren.
- Den Fix deshalb am tatsÃƒÆ’Ã‚Â¤chlichen Fehlerursprung gesetzt: `ArbeitsstundeRecord` verwendet jetzt wie die ÃƒÆ’Ã‚Â¼brigen produktiven Create-Modelle explizit `[PrimaryKey("id", false)]`. Damit bleibt die `Id` fÃƒÆ’Ã‚Â¼r Update-/PrimaryKey-Zwecke vorhanden, wird bei Neuanlagen aber nicht mehr in die Insert-Payload aufgenommen.
- Die zweite reale StÃƒÆ’Ã‚Â¶rung in `Arbeitsstunden freigeben` ebenfalls bis zur konkreten UI-Ursache zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: `status` war fachlich bereits optional und wurde in `HasChanges` nicht als Pflichtfeld behandelt. Das Problem lag stattdessen an der WPF-Spalte `DataGridCheckBoxColumn` fÃƒÆ’Ã‚Â¼r `Freigeben`; deren Wert wurde im laufenden UI-Pfad nicht zuverlÃƒÆ’Ã‚Â¤ssig sofort in das Item zurÃƒÆ’Ã‚Â¼ckgeschrieben, sodass `PropertyChanged`, `HasPendingChanges` und damit `SpeichernCommand` beim bloÃƒÆ’Ã…Â¸en Setzen der Checkbox nicht unmittelbar anzogen.
- Den Fix daher klein und UI-seitig gehalten: die Checkboxspalte lÃƒÆ’Ã‚Â¤uft jetzt als `DataGridTemplateColumn` mit explizitem `CheckBox`-Binding `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`. Damit gilt schon das reine Setzen von `Freigeben` sofort als speicherrelevante ÃƒÆ’Ã¢â‚¬Å¾nderung.
- Fachregeln bleiben unverÃƒÆ’Ã‚Â¤ndert: offen bleibt `freigegeben = false`, `status` bleibt nur optionales Anmerkungsfeld, Freigabe setzt weiter `freigegeben = true`, `genehmigt_am`, `genehmigt_von`, und es wurde keine neue Freigabearchitektur oder App-seitige `lastrow+1`-Logik eingefÃƒÆ’Ã‚Â¼hrt.
- Technisch verifiziert: `KGV.Wpf` baut nach dem gezielten Fixblock erfolgreich; MAUI wurde im selben Shared-Pfad mitgedacht und nicht beschÃƒÆ’Ã‚Â¤digt.

## 2026-03-23 ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ WPF-Bindingfix in `ArbeitsstundenErfassungView`: `CurrentMemberText` als reine OneWay-Anzeige verdrahtet

- Den aktuellen Bindingfehler in `ArbeitsstundenErfassungView` gezielt auf die einzige Bindungsstelle von `CurrentMemberText` zurÃƒÆ’Ã‚Â¼ckgefÃƒÆ’Ã‚Â¼hrt: der Wert dient fachlich nur der Anzeige des aktuellen Mitgliedskontexts.
- Deshalb bewusst **keinen** Setter in `ArbeitsstundenErfassungViewModel` ergÃƒÆ’Ã‚Â¤nzt und keine Fachlogik verÃƒÆ’Ã‚Â¤ndert: `CurrentMemberText` bleibt eine reine Anzeigeeigenschaft.
- Die Anzeige in `ArbeitsstundenErfassungView.xaml` wurde minimal und robust auf explizite OneWay-Darstellung umgestellt: statt `Run` wird der Wert jetzt in einem eigenen `TextBlock` mit `Mode=OneWay` angezeigt.
- Die benachbarten Anzeige-`TextBlock`s bleiben strukturell intakt; es wurde nur der kleine Anzeigeabschnitt fÃƒÆ’Ã‚Â¼r das Mitglied ersetzt, ohne weitere Formularlogik oder ViewModel-VertrÃƒÆ’Ã‚Â¤ge anzufassen.
- Technisch verifiziert: finaler `KGV.Wpf`-Build erfolgreich. Der kleine Bindingfix ist damit sauber abgeschlossen; MAUI blieb unberÃƒÆ’Ã‚Â¼hrt.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Insert-Pfade auf explizite ID-Mitgabe geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: produktive Neuanlagen senden aktuell keine feste ID aus der App

- Den aktuellen produktiven Insert-/Create-Stand zuerst gegen den realen Arbeitsbaum geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft statt auf Verdacht umzubauen: gesichtet wurden `SupabaseService`, die betroffenen Records/Modelle, die WPF-/MAUI-Neuanlagepfade fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `arbeitsstunde`, `termin`, `bekanntmachung` und `arbeitseinsatz` sowie ein mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶glicher Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `arbeitseinsatz_anmeldung`.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `arbeitsstunde` den konkreten Verdacht am echten Produktpfad geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: WPF (`ArbeitsstundenViewModel`) und MAUI (`MyArbeitsstundenPage`) bauen neue `ArbeitsstundeRecord`-Instanzen ohne gesetzte `Id`; `AddArbeitsstundeAsync(...)` erstellt daraus wiederum ein neues Insert-Objekt ebenfalls ohne `Id`-Zuweisung und sendet nur die fachlichen Felder an PostgREST.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die drei Verwaltungs-Neuanlagen denselben Punkt abgesichert: die WPF-Editoren bauen im New-Mode zwar Arbeitsobjekte mit `Id = 0` als lokalem Defaultwert, aber die produktiven Service-Create-Pfade `CreateTerminAsync(...)`, `CreateBekanntmachungAsync(...)` und `CreateArbeitseinsatzAsync(...)` ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernehmen diese `Id` gerade **nicht** in ihre tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlichen Insert-Objekte. Gesendet werden dort nur die fachlichen Spalten; Updates laufen weiterhin separat ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandene Datensatz-`Id`.
- Einen echten produktiven Insert-Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `arbeitseinsatz_anmeldung` gibt es im aktuellen Repo weiterhin nicht: der vorhandene `Anmelden`-Pfad auf Home ist nach wie vor nur eine ehrliche Info-Aktion ohne Schreibzugriff. Entsprechend war dort aktuell auch kein produktiver ID-Insertpfad zu korrigieren.
- ZusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich den Library-Unterbau gegen den aktuellen Paketstand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: im verwendeten `Supabase.Postgrest`-Paket ist beim `PrimaryKey`-Attribut der Parameter `shouldInsert` standardmÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ig `false`. Damit wird die PrimÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rschlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼sselspalte auch beim verbleibenden Muster `[PrimaryKey("id")]` nicht automatisch in Insert-Payloads aufgenommen, solange sie nicht explizit gesetzt und bewusst anderweitig serialisiert wird.
- Ergebnis der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung: im aktuellen produktiven App-Code lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ften Neuanlagepfade keine Stelle nachweisen, an der bei Inserts eine feste `id` oder konkret `id = 0` aus der App mitgesendet wird.
- Deshalb bewusst **kein** Vermutungsfix eingebaut: keine `lastrow+1`-Logik, keine unnÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tige Schatten-Insertarchitektur und keine DB-MigrationsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderung auf Verdacht. Der Block bleibt bei der belastbaren Analyse und Dokumentation des aktuell bereits korrekten Insert-Musters `Insert ohne feste ID, Update mit bestehender ID`.
- Repo-seitig zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich verifiziert: im aktuellen Arbeitsstand liegen keine versionierten SQL-/Migrationsdateien fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die betroffenen Tabellen vor; eine DB-seitige Sequence-/Default-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung war im Repo daher nicht weiter nachvollziehbar, wurde aber fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen Block auch nicht benÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tigt, weil auf App-Seite kein fehlerhafter ID-Insertpfad mehr vorliegt.
- Technisch verifiziert: Workspace-Build erfolgreich; WPF bleibt buildbar und MAUI wird durch diesen Dokumentations-/Analyseblock nicht beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digt.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-/Detail-Block fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze sauber abgeschlossen: Beginn sichtbar, Detaildaten vervollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndigt, Anmelden ehrlich nur informativ

- Den begonnenen Home-/Detail-Block zuerst gegen den realen Arbeitsbaum konsolidiert statt neu aufzuziehen: bereits halbfertig vorhanden waren erweiterte Home-/Detailmodelle, ein vorbereiteter optionaler `Anmelden`-Pfad im Detailkontext und erste Anpassungen im `HomeViewModel`, aber die sichtbaren WPF-XAML-Schritte und der hintere Helper-Tail des `SupabaseService` waren noch nicht sauber abgeschlossen.
- Den Home-/Detailpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Arbeitseinsatz`, `Termin` und `Bekanntmachung` gemeinsam nachgezogen, damit die generische Detailansicht durch die Erweiterung keine Felder verliert: zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Datensatzinformationen laufen jetzt konsistent ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `DetailInfo`/`AdditionalInfo`, statt nur bei ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzen Sonderwissen lokal in der View zu hinterlegen.
- `Arbeitseinsatz` zeigt auf Home jetzt den Beginn explizit sichtbar im Karteneintrag; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich gibt es dort einen eigenen `Details`-Button und ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ nur wenn der Datensatz ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berhaupt anmeldbar markiert ist ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ einen `Anmelden`-Button.
- Die Home-Detailansicht zeigt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Arbeitseinsatz` jetzt neben Titel/Untertitel auch die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼brigen im Datensatz belastbar vorhandenen Informationen wie Thema, Datum, Beginn, Ende, Treffpunkt sowie KapazitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ts-/Anmeldeinformationen; `Max. Teilnehmer` wird bewusst nur dann angezeigt, wenn sie aus den vorhandenen Datensatzwerten ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berhaupt belastbar ableitbar ist.
- `Termin`- und `Bekanntmachung`-Detailansichten wurden im selben Pfad mitkonsolidiert: zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Felder wie Ort, Thema, Betreff, VerÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffentlichungszeitpunkt oder Kurztext werden jetzt ebenfalls ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben Detailkontext transportiert, statt in der generischen Detailview verloren zu gehen.
- Den halbfertig beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digten Tail des `SupabaseService` sauber wieder geschlossen und dabei die kleinen Home-Helfer zentral konsolidiert (`AddDetailLine`, Datum-/Zeitformatierung, Textnormalisierung, Dokumentpfad-Helfer etc.), damit der angefangene Block wieder auf einem kompilierbaren produktiven Stand steht.
- Den `Anmelden`-Usecase bewusst **nicht** als neue Schattenlogik erÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet: im aktuellen Repo lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich weiterhin kein belastbarer produktiver Schreibpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine echte Arbeitseinsatz-Anmeldung finden. Deshalb ist der neue `Anmelden`-Button in WPF nur als ehrliche Info-Aktion verdrahtet und weist explizit darauf hin, dass der echte Schreibpfad noch nicht angebunden ist.
- WPF wurde sichtbar fertiggestellt; MAUI wurde bei den gemeinsamen Home-Items/Mappingerweiterungen mitgedacht, aber bewusst nicht mit einer neuen Arbeitseinsatz-AnmeldeoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erweitert, solange dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r kein belastbarer gemeinsamer Fachpfad existiert.
- Technisch verifiziert: Workspace-Build erfolgreich; der begonnene Home-/Detail-Block ist damit ohne erfundene Anmeldefachlogik sauber abgeschlossen.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren und Home-Navigation nachgezogen: ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck-Text repariert, Doppelklick auf Listen, Arbeitsstunden-Erfassung nur noch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Home

- Die drei WPF-Verwaltungseditoren `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen` nach dem Parserfix nochmals produktiv bereinigt: der beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digte Toolbar-Text `ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck` wird jetzt XAML-seitig stabil als Entity geschrieben, damit kein erneutes Zeichensatzproblem im Buttontext entsteht.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r alle drei Verwaltungslisten einen echten Doppelklickpfad auf die ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: statt auf `InputBindings` zu vertrauen, ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen die `ListBox`-EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge jetzt direkt per `MouseDoubleClick` den vorhandenen `OeffnenCommand` und damit den Bearbeitungseditor zuverlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ssig.
- Die WPF-Hauptnavigation wieder auf den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten Produktpfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen: `Arbeitsstunden erfassen` wird nicht mehr als eigener Navigationseintrag aufgebaut und bleibt nur noch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den vorhandenen Button auf der `Startseite` erreichbar.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche XAML-Ursache zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck-/Dirty-Check ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt, Navigation auf Home-only zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r geladene VerwaltungsdatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze sowie fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehr erfolgt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den vorhandenen Navigationspfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck`-Button
  - `ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck` und `Abbrechen` prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen auf echte ungespeicherte ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen
  - die RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckfrage erscheint nur bei tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlichen ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten Produktpfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen:
  - `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandenen Home-Buttons fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `status`; die globale PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bleiben unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen der Freigabeansicht wird eine globale PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fsperre ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die offenen `arbeitstunde`-DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze gesetzt
  - andere PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngert die Sperre wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernehmbar
- Die bestehende Badge-/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlerlogik bleibt weiterverwendet: ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Datum`, `Stunden` und `Art der Arbeit` ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Benutzer mit eigenem Mitgliedskontext; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸er als `0` sein.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler zeigt weiter die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche Anzahl offener PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend im Repo noch die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r spÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tere MAUI-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t anschlussfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend die eigentlichen EditoroberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Verwaltung erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und nicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und nicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und parallel in einer kompakten Vorschau geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze direkt per Create/Update wieder nach `termin` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“bersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar und blÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r spÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tere MAUI-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` laden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte View.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung bleibt daher auf tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabefelder bzw. reale DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzen, Terminen und Bekanntmachungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen in eigener View

- Den Home-Stand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen auf den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersichten, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine eigenstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndige WPF-Detailansicht fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home-Elemente ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen-Kommandos fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft. Die entscheidende technische Ursache fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Pflichtstundenfehler lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home jetzt zunÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸end robust clientseitig ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgelieferte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: der Home-Pfad schreibt jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Hauptmitglied/Saison prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: da `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Pflichtstunden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung und Bekanntmachungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home deshalb final prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber dieses Hauptmitglied geladen.
- ZusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich wird die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt, sondern zuerst direkt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Titel, Inhalt und VerÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich vereinfacht und nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤her auf den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rt ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ im bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-Diagnose: bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Schema-Stand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Kern geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft, ohne neue App-Logik einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶st zuerst die aktuelle Saison ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber MAUI-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶glichen Abschlussoptionen geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe-/Kommentarpfad wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt und den vorhandenen mobilen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` mobil jetzt nur noch dann an, wenn ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle vorliegen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze direkt im Titel des MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad, sodass MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼eintrag und tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche offene FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die mobile Seite bleibt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatusanzeige drauÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fflow in WPF bis zur Freigabe vervollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bereits PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste, Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet, gefiltert auf die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offenen DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze des ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Mitglieds.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r nur einen kleinen Navigationskontext ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼feintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge des konkret ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Mitglieds im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus erscheinen.
- Admin/Vorstand kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶nnen im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich eine direkte `Freigeben`-Aktion fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r einen sauberen RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste und Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler weiterhin ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus fachlich geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: klarer Titel, Hinweistext, leerer PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndigung des PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation and den gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: ein belastbarer Listen-/Detailpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge, Vorstand/Admin kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene Arbeitsstunden ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren von der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` erscheint fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r berechtigte Benutzer jetzt nur bei tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offenen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llen, zeigt einen kleinen ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler mit der Zahl offener DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und wird bei offenem PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft nach ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼flisten und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rten produktiven DB-Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hen: `HomeOverviewDTO` trÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤gt jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich gemeinsame Startseiten-Daten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden, ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt diese Inhalte direkt aus den geklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Home-Ausgabe mitgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Bereich `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤sst sich wieder mit `SchlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbaren Inhalte statt des bisherigen kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: konkret auffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `MainWindowViewModel` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Eigenkontext weiter den nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en`-Button ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Infrastruktur direkt testbar.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Desktop und Mobil und lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤sst sich ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Testinfrastruktur direkt prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen, sondern sinnvoll zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die aktive Root-Projektdatei ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernimmt jetzt wieder die fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig erhalten bleibt.
- Fachlich passende ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere Tests sauber zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Demo-/Testdatenfilter zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt

- Die aktuelle Testlage geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere Testidee fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ExportViewModel`, die fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung) ohne dass dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine groÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸e Testinfrastruktur nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tig wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤re.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine kleine aktive Testbasis im Root zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckzuziehen.
- Den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen: sie waren fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tten den Wiedereinstieg unnÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nen Tests.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die Umsetzung bleibt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene Arbeitsstunden berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt nur operative Mitglieder ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Arbeitsstunden, keine zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen WPF-OberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 2: NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken erneut gegen die produktiven WPF-Wege geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste kleine, aber fachlich wichtige ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke, weil WPF dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r RollenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche Mitgliedsrolle vor einem evtl. ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteren `app_user`-Wert, damit RollenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 1: NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die deutlichste belastbare LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand, baut vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r MAUI ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungsgrenze auch mobil sauber berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt: eine E-Mail-ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: offen war vor allem noch die Nutzer-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` bleiben dagegen weiterhin bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸t sich die bisherige Home-/Pfad-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke gegenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber MAUI.
- IrrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende leere Bekanntmachungs-DetailflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen entschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Bekanntmachungs-Backendpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF und MAUI.
- NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rige gemeinsame PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ndelt jetzt den fachlichen Kern fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulauflistung wurde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 3: Parzellen-KernlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken und mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar waren bereits aktive ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler einbauen und ausbauen sowie das ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berschneidungen gegen den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Starttag geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Mitglied zuordnen, Startdatum wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤higkeit geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rigen Parzellen-Dateien durchgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb des Commits.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und archivwÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert gelassen: keine ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb der aktiven LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: nur die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Mailadresse ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndern` in `MemberDetailViewModel` ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen Teilblock vorbereitet: nur die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en vor.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehrt.
- Login-/Recovery-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umt alte OTP-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: normales Login, OTP-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde gefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrung sauber: RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neues `README.md` als Startpunkt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digten/irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fbaren Projekte ausgerichtet.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Prompt 1/2: Home/Startseite ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `admin` und `vorstand` sichtbar, ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.

---

## Workaround & Vorgehensweise bei ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen

- ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen nur nach genauer RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckfrage und vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndiger Ansicht der Datei vornehmen.  
- Ich mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶chte immer genau wissen, **wo und wie** die ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen erfolgen.  
- Am liebsten **komplett die Datei** erhalten, damit der neue Code sauber angepasst werden kann.  
- Wenn Unsicherheit besteht, fordere ich immer die aktuelle Datei an, bevor ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen vorgeschlagen werden.  

---

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren repariert: fehlerhafte InputBindings in allen drei Listen auf echten Doppelklick-Pfad korrigiert

- Die aktuelle WPF-Parserexception in `ArbeitseinsaetzeVerwaltungEditorView` bis auf die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche XAML-Ursache zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: im oberen Toolbar-`StackPanel` stand versehentlich ein `MouseBinding` direkt zwischen den Buttons.
- Der Fehler war kein isolierter Einzelfall der geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffneten View, sondern derselbe Copy/Paste-Fehler lag parallel auch in `TermineVerwaltungEditorView` und `BekanntmachungenVerwaltungEditorView` vor.
- Den gemeinsamen Root-Cause deshalb direkt an allen drei produktiven Verwaltungseditoren behoben: die ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltigen direkten `MouseBinding`-Kinder wurden aus den Toolbar-`StackPanel`s entfernt; der gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschte Doppelklickpfad bleibt weiterhin korrekt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandenen `ListBox.InputBindings` auf `OeffnenCommand` aktiv.
- Ergebnis: `InitializeComponent()` kann die drei Views wieder korrekt laden, ohne den vorhandenen Doppelklick auf die Eintragslisten zu verlieren.

## 2026-03-23 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren sauber abgeschlossen: Zeit-/Datumsbug zentral behoben, ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck-/Dirty-Check ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt, Navigation auf Home-only zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen

- Den halbfertigen Stand der drei produktiven Verwaltungseditoren (`ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine`, `Bekanntmachungen`) zuerst gegen Arbeitsbaum, Records, `SupabaseService`, WPF-ViewModels und Navigation geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und nur den begonnenen Block konsolidiert statt neue Fachlogik zu erÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen.
- Der reproduzierbare Verschiebungsfehler lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich auf den gemeinsamen Typ-/Mappingpfad eingrenzen, nicht auf einzelne Formulare:
  - `termin.datum` lief als `DateTime`, fachlich ist die Spalte aber `date`
  - `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` liefen als `DateTime`, fachlich sind sie `timestamp without time zone`
  - dadurch konnten beim JSON-/PostgREST-Transport implizite `DateTimeKind`-/Zeitzoneninterpretationen greifen, was die beobachteten `-1 Tag`- bzw. `-1/-2 Stunden`-Verschiebungen beim erneuten Bearbeiten/Speichern erklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rte
- Die Ursache deshalb zentral und nachvollziehbar im Shared-Pfad korrigiert:
  - neue explizite JSON-Konverter fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r PostgreSQL-`date` und `timestamp without time zone`
  - gezielte Annotation der betroffenen Felder in `ArbeitseinsatzRecord`, `TerminRecord` und `BekanntmachungRecord`
  - zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche zentrale Normalisierung im `SupabaseService` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r geladene VerwaltungsdatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze sowie fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Create-/Update-Pfade der drei Module
  - die letzte verbliebene abweichende Datumsnormalisierung im `arbeitseinsatz`-Create-Pfad wurde ebenfalls auf denselben date-only-Pfad gezogen
- Ergebnis des Fixpfads:
  - `arbeitseinsatz.sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` bleiben stabil
  - `termin.datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` bleiben stabil
  - `bekanntmachung.sichtbar_ab` und `sichtbar_bis` bleiben stabil
  - erneutes Speichern erzeugt keine weitere fachliche Verschiebung mehr
- Das Editorverhalten der drei bestehenden WPF-Verwaltungsviews jetzt sauber abgeschlossen:
  - nach erfolgreichem Speichern wird die Bearbeiten-View automatisch geschlossen
  - RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehr erfolgt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den vorhandenen Navigationspfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck zur `HomeView`
  - alle drei Editor-Views haben jetzt einen `ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck`-Button
  - `ZurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck` und `Abbrechen` prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen auf echte ungespeicherte ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen
  - die RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckfrage erscheint nur bei tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlichen ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen; nach erfolgreichem Speichern gilt der Zustand wieder als sauber
- Die Navigation wurde auf den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten Produktpfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen:
  - `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bearbeiten`, `Termine bearbeiten` und `Bekanntmachungen bearbeiten` sind nicht mehr in der Hauptnavigation verlinkt
  - erreichbar bleiben sie nur noch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandenen Home-Buttons fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand
  - der Doppelpfad Home + Hauptnavigation wurde damit beseitigt
- WPF konkret angepasst, MAUI bewusst nicht mit neuer VerwaltungsoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erweitert; da die eigentliche Ursache im Shared-Core-/Service-/Typmapping lag, profitiert MAUI vom zentralen Fix ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen UI-Umbau.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich. Verbleibende Buildwarnungen liegen im bestehenden `SupabaseService.Set(...)`-Pfad und sind reine Nullability-Hinweise, kein verbleibender Zeit-/Datumsfehler.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Freigabe produktiv abgeschlossen: globaler Review-Lock, PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ftabelle und Editor-Wiederverwendung

- Den begonnenen Arbeitsstunden-Freigabe-Block gegen den realen Arbeitsbaum erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und nur konsolidiert statt neu umgebaut: vorhanden waren bereits die neue PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ftabelle, der globale Lock-Unterbau auf `arbeitstunde`, die Wiederverwendung des Erfassungseditors im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fkontext sowie die sprachliche Konsolidierung auf `Arbeitsstunden freigeben`.
- Die real verwendeten `arbeitstunde`-Felder nochmals gegen Modell und Servicepfad abgesichert: Freigaben laufen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `freigegeben`, `genehmigt_am` und `genehmigt_von`; Anmerkungen laufen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `status`; die globale PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fsperre nutzt die real vorhandenen Lockfelder `lockedbyuserid` und `lockat`.
- Den offenen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fachlich sauber vom Textfeld `status` entkoppelt: offene Arbeitsstunden basieren jetzt konsistent auf `freigegeben = false`; `status` wird nicht mehr kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlich als Workflowwert `offen` erzwungen, sondern bleibt das fachliche Anmerkungsfeld fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand.
- Die WPF-Freigabeansicht ist jetzt produktiv als Tabelle aufgebaut statt als frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here Mitglieder-Sammelliste:
  - Sortierung nach `datum` aufsteigend *(ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteste zuerst)*
  - pro Zeile sichtbare PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼finformationen zu Mitglied, Datum, Saison, Stunden und Art der Arbeit
  - bearbeitbares Feld `status / Anmerkung`
  - Checkbox `Freigeben`
  - Button `Bearbeiten`
- Selektives Speichern umgesetzt: gespeichert werden nur tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte oder zur Freigabe markierte Zeilen; unbearbeitete offene DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bleiben unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert offen und fallen nicht still mit in dieselbe Sitzung.
- Die eigentliche Freigabe setzt beim Speichern die realen Freigabefelder korrekt: `freigegeben = true`, `genehmigt_am` wird gesetzt, `genehmigt_von` wird auf das aktuelle Mitglied des prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fenden Benutzers gesetzt. Nicht freigegebene, aber geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte Zeilen behalten `freigegeben = false` und bleiben in der offenen Liste.
- Der vorhandene Arbeitsstunden-Erfassungseditor wird jetzt auch im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Adminkontext wiederverwendet statt eine zweite Schatten-Editorarchitektur zu bauen: im Bearbeiten-Fall ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet derselbe Editorpfad mit den normalen Arbeitsstundenfeldern plus zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich sichtbarem `status`-Feld.
- Globaler Review-Lock klein und robust umgesetzt:
  - beim ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen der Freigabeansicht wird eine globale PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fsperre ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die offenen `arbeitstunde`-DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze gesetzt
  - andere PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fer erhalten bei aktiver Fremdsperre nur eine saubere RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckmeldung statt paralleler produktiver Bearbeitung
  - soweit auflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sbar wird angezeigt, wer gesperrt hat und seit wann
  - ein Heartbeat verlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngert die Sperre wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der Sitzung
  - beim Verlassen der Ansicht wird die Sperre wieder freigegeben
  - hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngende Locks laufen nach Timeout kontrolliert aus und sind danach ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernehmbar
- Die bestehende Badge-/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlerlogik bleibt weiterverwendet: ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen und Freigaben senden weiterhin `ArbeitsstundenChangedMessage`, sodass WPF-Navigation und bestehende mobile Reviewindikatoren konsistent aktualisiert werden.
- MAUI wurde in diesem Abschlussblock bewusst nicht mit neuer FreigabeoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erweitert; der gemeinsame Unterbau wurde aber konsistent mitgezogen, insbesondere die Entkopplung von offenem Zustand und `status`-Textfeld.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem sauber abgeschlossenen Arbeitsstunden-Freigabe-Block erfolgreich. Die kurz sichtbare XAML-Design-Time-Meldung zu `ArbeitsstundenErfassungView` war nicht buildrelevant; der Produktbuild ist erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Userflow klargezogen: eigenes Erfassungs-View + vorbereiteter Freigabe-Indikator

- Den aktuellen Arbeitsstunden-Istzustand vor dem Umbau erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bereits `ArbeitsstundenView`/`ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, der produktive Servicepfad `AddArbeitsstundeAsync(...)`, die bestehende offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fgruppenlogik `GetUnapprovedArbeitsstundenByMitgliedAsync()` sowie der WPF-/MAUI-Badgepfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle.
- Den bestehenden Unterbau bewusst weiterverwendet statt neue Arbeitsstunden-Architektur aufzubauen: neue Erfassung schreibt weiter ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `AddArbeitsstundeAsync(...)`, offene Freigaben laufen weiter ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `GetUnapprovedArbeitsstundenByMitgliedAsync()` und Aktualisierungen werden weiterhin ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `ArbeitsstundenChangedMessage` verteilt.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r normale Nutzer jetzt einen klaren eigenen Erfassungsweg ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neue WPF-Ansicht `ArbeitsstundenErfassungView` plus `ArbeitsstundenErfassungViewModel` bietet genau den einfachen Userflow fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Datum`, `Stunden` und `Art der Arbeit` ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Fachfelder und ohne neue FreigabeoberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che.
- Der neue Einstieg ist bewusst sichtbar und eigenstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig statt Inline-Home-Bereich: es gibt jetzt einen klaren Navigationspunkt `Arbeitsstunden erfassen` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Benutzer mit eigenem Mitgliedskontext; additionally erscheint auf Home im Bereich `Meine Arbeitsstunden` ein eigener Button `Arbeitsstunden erfassen`, der in dieselbe neue View fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Das Userformular bleibt fachlich bewusst klein: sichtbar sind nur `Datum`, `Stunden` und `Art der Arbeit`; `freigegeben` wird im Usermodus nicht angezeigt und beim Speichern immer automatisch `false` gesetzt.
- Die Validierung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Userflow folgt dem inzwischen etablierten Muster der Verwaltungseditoren: Pflichtfelder sind gekennzeichnet, fehlende Eingaben werden rot markiert und der Fokus springt beim Speichern auf das erste fehlerhafte Feld. FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Stunden` bleibt die fachliche Minimalregel produktnah: Wert muss numerisch und grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸er als `0` sein.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand wurde in diesem Block bewusst keine neue Freigabeansicht erfunden: stattdessen wurde die bestehende PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Review-Navigation sauber auf die Zielbezeichnung `Arbeitsstunden freigeben` konsolidiert.
- Der offene Freigabe-Indikator bleibt auf dem vorhandenen Pfad: WPF-Hauptnavigation und bestehendes MAUI-AdminmenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ zeigen den Punkt `Arbeitsstunden freigeben` nur dann an bzw. hervorgehoben, wenn offene DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze mit `freigegeben = false` vorhanden sind; der Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler zeigt weiter die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche Anzahl offener PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle.
- Keine Doppelnavigation stehen gelassen: der bisherige Begriff `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` wurde entlang der betroffenen Navigations-/TitelflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen auf `Arbeitsstunden freigeben` konsolidiert, ohne parallel einen zweiten Zweckpfad aufzubauen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Userflow-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Verwaltungseditoren konsolidiert: alte Strukturviews entfernt, produktive Pfade klargezogen

- Den aktuellen Abschlussstand der drei Verwaltungsbereiche erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `Termine`, `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` waren bereits produktiv an ihre Basistabellen angebunden, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend im Repo noch die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteren rein strukturellen Verwaltungsviews parallel neben den inzwischen produktiven Editor-Views lagen.
- Die produktive WPF-Verdrahtung final geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: `App.xaml` bleibt klar auf die drei produktiven Editor-Views gemappt (`ArbeitseinsaetzeVerwaltungEditorView`, `TermineVerwaltungEditorView`, `BekanntmachungenVerwaltungEditorView`), sodass die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich genutzte Verwaltungsarchitektur jetzt ohne Doppelpfad lesbar bleibt.
- Technisch ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berholte Altlasten bereinigt statt weiter mitzuschleppen: die alten strukturellen Views `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` inklusive zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶riger Code-behind-Dateien wurden entfernt, weil sie nicht mehr produktiv verdrahtet waren und nur noch Verwirrung erzeugten.
- Auch der frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here gemeinsame Strukturunterbau wurde entfernt: `HomeVerwaltungViewModelBase` und `HomeVerwaltungListItem` werden im aktuellen produktiven Stand nicht mehr verwendet, seit alle drei Verwaltungseditoren auf eigene echte Basistabellen-Editoren umgestellt wurden.
- Home-/Rechtepfad nochmals gegen den Abschlusszustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die Bearbeiten-Einstiege auf Home sowie in der Hauptnavigation bleiben ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand sichtbar; normale Nutzer bleiben weiter im lesenden Home-Modus ohne Bearbeitung direkt auf der Startseite.
- Die gemeinsame Service-/Modellebene bleibt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r spÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tere MAUI-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t anschlussfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig: die produktiven Lese-/Create-/Update-Pfade fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `termin`, `bekanntmachung` und `arbeitseinsatz` liegen jetzt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im plattformneutralen Shared-Core-/Infrastructure-Bereich, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend die eigentlichen EditoroberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen weiterhin bewusst WPF-spezifisch bleiben.
- Keine neue Fachlogik erÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet: der Block blieb bewusst bei Konsolidierung, AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umen und Architekturabschluss; es wurden keine neuen Verwaltungsfeatures, keine neuen MAUI-Platzhalterseiten und keine Home-Bearbeitung ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Konsolidierung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Verwaltung produktiv gemacht: Basistabelle `arbeitseinsatz` + Sonderregeln fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Teilnehmer/Stundenwert

- Den aktuellen Istzustand der vorbereiteten ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Verwaltung erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: vorhanden waren bisher Verwaltungsnavigation, die strukturelle WPF-Ansicht und eine Leseliste ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `v_startseite_arbeitseinsatz`, aber noch kein produktiver Editor und kein bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigter Schreibpfad gegen die Basistabelle.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `arbeitseinsatz` jetzt direkt produktiv an die WPF-Verwaltung angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `treffpunkt`, `max_teilnehmer`, `stunden_wert`, `sichtbar_ab`, `sichtbar_bis`, `anmeldung_bis` und `aktiv` sind jetzt die echten Bearbeitungsfelder; `created_at`, `updated_at` und `is_demo` bleiben bewusst keine normalen UI-Standardfelder.
- Echten Basistabellenpfad ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und nicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Startseiten-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `arbeitseinsatz` und schreibt neue bzw. geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze per `CreateArbeitseinsatzAsync(...)` und `UpdateArbeitseinsatzAsync(...)` wieder gegen `arbeitseinsatz` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Das Validierungs-/Fokusmuster aus `Termine` und `Bekanntmachungen` bewusst wiederverwendet: Pflichtfelder werden rot markiert, beim Speichern springt der Fokus auf das erste fehlerhafte Feld von oben, und die zentrale tolerante Zeitlogik wird auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` erneut genutzt.
- Sonderregel `max_teilnehmer` explizit fachlich sauber umgesetzt: Teilnehmerbegrenzung ist kein Pflichtfeld; im Zustand `ohne Begrenzung` bleibt das eigentliche Eingabefeld unsichtbar und gespeichert wird `NULL` statt `0`. Sobald eine Begrenzung aktiv ist, muss der Wert grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸er als `0` sein.
- Sonderregel `stunden_wert` ebenfalls sauber umgesetzt: das Feld bleibt optional, eine leere Eingabe fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt nicht zu einem Fehler und wird beim Speichern DB-konform auf den operativen Wert `0` gefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; nur negative Eingaben werden als Fehler markiert.
- Weitere bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Validierungsregeln produktiv angeschlossen: `Titel` darf nicht leer sein, `Datum` ist Pflicht, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen. `Anmeldung bis` wird zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich als optionaler, aber formal konsistenter Timestamp behandelt.
- Nach erfolgreichem Speichern wird die Arbeitseinsatzliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Kleinen AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere strukturelle `ArbeitseinsaetzeVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ere Bereinigung wird nicht in diesen Block hineingezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Bekanntmachungen-Verwaltung produktiv gemacht: Basistabelle `bekanntmachung` + kleiner HTML-Editor

- Den aktuellen Istzustand der vorbereiteten Bekanntmachungen-Verwaltung erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bisher nur Verwaltungsnavigation, Strukturansicht und Home-nahe Leseliste ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die Startseiten-View; ein echter Editor, ein produktiver Schreibpfad auf die Basistabelle und eine HTML-taugliche Bearbeitung fehlten noch.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `bekanntmachung` jetzt direkt produktiv angeschlossen: `titel`, `inhalt_html`, `sichtbar_ab`, `sichtbar_bis`, `sort_order` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Bearbeitungsfelder verwendet; `created_at` und `updated_at` bleiben weiterhin bewusst technische Nebenspalten.
- Echten Basistabellenpfad ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und nicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Home-Views geschrieben: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt direkt aus `bekanntmachung` und schreibt neue/geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze per `CreateBekanntmachungAsync(...)` bzw. `UpdateBekanntmachungAsync(...)` wieder gegen `bekanntmachung` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den vorhandenen Validierungs-/Fokusansatz aus der produktiven `Termine`-Verwaltung bewusst wiederverwendet: `Titel` und `HTML-Inhalt` sind Pflichtfelder, `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen, ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltige Werte werden rot hervorgehoben, und beim Speichern springt der Fokus wieder auf das erste fehlerhafte Feld von oben.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `inhalt_html` bewusst keine schwere neue Richtext-Architektur eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: stattdessen gibt es jetzt einen kleinen kontrollierten HTML-Editor mit Snippet-Leiste (`Absatz`, `ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berschrift`, `Fett`, `Link`, `Liste`) plus integrierter Live-Vorschau auf Basis des vorhandenen WPF-Bordmittels `WebBrowser`.
- Damit bleibt der Editor produktiv nutzbar, ohne auf ein reines Plaintext-Endfeld reduziert zu sein: HTML kann direkt bearbeitet, durch kleine Bausteine ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und parallel in einer kompakten Vorschau geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft werden.
- `Sortierreihenfolge` wird bewusst nur technisch korrekt als optionale Ganzzahl behandelt; es wurden keine zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen fachlichen Sortierregeln erfunden. FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r neue Bekanntmachungen bleibt `aktiv = true` als sinnvoller Editor-Default im Rahmen des bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Defaults bestehen.
- Nach erfolgreichem Speichern wird die Bekanntmachungsliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die Fehlerbehandlung, statt Fehler still zu verschlucken.
- Kleiner AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umcheck bewusst klein gehalten: die ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere strukturelle `BekanntmachungenVerwaltungView` bleibt vorerst noch im Repo, produktiv verdrahtet ist jetzt aber die neue Editoransicht; weitere Bereinigung wird nur nach Bedarf im nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten AufrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umschritt gezogen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem produktiven Bekanntmachungen-Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Termine-Verwaltung produktiv gemacht: echter Editor gegen `termin`

- Den aktuellen Istzustand der bereits vorbereiteten `TermineVerwaltungView` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bisher nur Listenstruktur, Navigation und der Lesepfad; der rechte Bereich war noch kein echter Editor und es gab noch keinen bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Schreibpfad auf die Basistabelle.
- Den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Tabellenvertrag von `termin` jetzt direkt produktiv angeschlossen: `titel`, `beschreibung`, `datum`, `start_uhrzeit`, `end_uhrzeit`, `sichtbar_ab`, `sichtbar_bis` und `aktiv` werden im WPF-Editor exakt als bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Bearbeitungsfelder verwendet; technische Felder wie `created_at` und `updated_at` bleiben bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb der normalen Bearbeitung.
- Den echten Schreibpfad sauber auf die Basistabelle gelegt und nicht auf Home-Views: gemeinsamer Service lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt die Verwaltungs-Liste jetzt aus `termin` und schreibt neue bzw. geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderte DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze direkt per Create/Update wieder nach `termin` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Die Termine-Verwaltung ist damit erstmals fachlich produktiv: Doppelklick auf einen Datensatz ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet rechts den Editor mit echten Werten, `Neu` ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet einen leeren Editorzustand, `Abbrechen` verwirft den Bearbeitungszustand wieder vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig, und `Speichern` bleibt am Ende des Formulars.
- BestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Validierungsregeln direkt umgesetzt statt zu raten: `Titel` und `Datum` sind Pflichtfelder, `Titel` darf nicht nur aus Leerzeichen bestehen, `Enduhrzeit` darf nicht vor `Startuhrzeit` liegen, und `Sichtbar bis` darf nicht vor `Sichtbar ab` liegen.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Zeitfelder eine wiederverwendbare tolerante Eingabelogik ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Eingaben wie `8`, `08`, `830`, `8:30` und `8.30` werden auf `HH:mm` normalisiert; offensichtlich ungÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ltige Zeiten bleiben sichtbar und werden nicht still korrigiert, sondern sauber als Fehler markiert.
- Das WPF-Bedienmuster fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Folgemodule vorbereitet: fehlerhafte Pflicht-/Zeitfelder werden rot hervorgehoben, und beim Speichern springt der Fokus automatisch auf das erste fehlerhafte Feld von oben; dieses Muster ist damit fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` und `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` wiederverwendbar.
- Nach erfolgreichem Speichern wird die Terminliste neu geladen und der gespeicherte Datensatz erneut selektiert/angezeigt; gezielte, knappe Diagnose-Logs im Service ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzen die bisherige Fehlerbehandlung, statt Fehler stumm zu verschlucken.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der produktiven Termin-Verwaltung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-Admin-Bearbeiten entzerrt: separate Verwaltungsviews statt Bearbeitung auf Home

- Den aktuellen Home-/Navigationsstand erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: Home zeigt bereits nur Listen und separate Detailviews, aber fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand fehlte noch eine saubere Verwaltungsarchitektur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen`; zugleich waren im aktiven Repo keine belastbar verifizierten Schreibmodelle/-services fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diese drei Bereiche vorhanden.
- Home deshalb bewusst schlank gehalten: normale Nutzer sehen weiter nur die ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“bersichtslisten; Admin/Vorstand erhalten auf Home jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich nur drei kleine Bearbeiten-Einstiege (`ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze bearbeiten`, `Termine bearbeiten`, `Bekanntmachungen bearbeiten`) statt Bearbeitung direkt in der Startseite.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF echte separate Verwaltungsansichten aufgebaut: `ArbeitseinsaetzeVerwaltungView`, `TermineVerwaltungView` und `BekanntmachungenVerwaltungView` sind jetzt produktiv navigierbar und folgen alle demselben Zielaufbau mit linker Datenliste und rechter EditorflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che.
- Die Listen nutzen reale Datenpfade statt Home-Umweg oder Platzhalterdaten: ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber neue gemeinsame Servicezugriffe werden die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Startseiten-Lesepfade `v_startseite_arbeitseinsatz`, `v_startseite_termine` und `v_startseite_bekanntmachungen` direkt auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Verwaltungslisten bereitgestellt.
- Der rechte Bereich bleibt bewusst feldarm, solange der Schreibvertrag nicht belastbar genug im Repo vorhanden ist: bei keinem geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffneten Datensatz bleibt der Editorbereich leer; bei `Neu` oder Doppelklick wird der echte Editierzustand geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet, aber ohne geratene Formularfelder oder erfundene Pflichtdefinitionen.
- Rechte sauber am bestehenden Pfad eingehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngt: die drei Verwaltungsviews sind in WPF-Navigation und Home-Einstiegen nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar und blÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hen MAUI nicht mit neuen Platzhalterseiten auf; die gemeinsame Serviceerweiterung bleibt aber fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r spÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tere MAUI-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t nutzbar.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Block weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Nachtrag Home-Diagnose: aktuell laden stabil nur ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und Termine

- Im aktuellen WPF-Nachtest bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` laden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die Startseiten-Views stabil; die verbleibenden AuffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lligkeiten betreffen weiterhin den Pflichtstundenbereich und den Bereich `Bekanntmachungen`.
- Der Pflichtstundenfehler ist technisch konkret belegt: im Debug-Log schlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤gt `LoadPflichtstundenSummaryAsync(...)` mit `column v_pflichtstunden_uebersicht.mitglied_id does not exist` fehl. MaÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸geblich ist damit nicht ein allgemeiner Home-Fehler, sondern eine falsche serverseitige Spaltenannahme im bisherigen Filterpfad auf die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte View.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` zeigt sich im Gegenzug aktuell kein gleichartiger harter Section-Crash, sondern eher ein leerer bzw. fachlich irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrender Placeholderzustand trotz vorhandener Startseiten-Anbindung; die RestprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung bleibt daher auf tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabefelder bzw. reale DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze der View fokussiert.
- Der UX-Nachzug fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home bleibt davon getrennt: auf der Startseite werden jetzt nur Listen angezeigt; Details von ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzen, Terminen und Bekanntmachungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen in einer eigenen View statt in Inline-Teilbereichen der `HomeView`.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-UX-Nachzug: Startseite zeigt nur Listen, Details ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen in eigener View

- Den Home-Stand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen auf den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten UX-Pfad umgestellt: die Startseite zeigt jetzt nur noch Listen-/KartenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersichten, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend Details nicht mehr in kleinen Teilbereichen innerhalb der `HomeView` erscheinen.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine eigenstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndige WPF-Detailansicht fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home-Elemente ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt (`HomeSectionDetailViewModel` + `HomeSectionDetailView`) und in die bestehende ViewModel-first-Navigation eingebunden.
- `HomeViewModel` verwendet jetzt explizite ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen-Kommandos fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen; aus den Home-Listen wird jeweils in die neue Detailansicht navigiert statt lokale Inline-DetailzustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde in `HomeView` zu verwalten.
- Die bisherige Inline-Detailanzeige fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Termine/Bekanntmachungen wurde aus `HomeView.xaml` entfernt. Gleichzeitig wurde auch der Arbeitseinsatz-Bereich auf eine reine Liste reduziert, damit alle drei Startseitenbereiche konsistent denselben Detailfluss nutzen.
- Kleine Navigationshilfe ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: aus der neuen Detailansicht kann direkt wieder auf die Startseite zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgesprungen werden, ohne neue Gesamtarchitektur oder separaten Historienmechanismus einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren.
- Technisch verifiziert: `KGV.Wpf` baut nach der Home-UX-Umstellung weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur Teil 3: Ursachenanalyse fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r verbleibende Pflichtstunden-/Bekanntmachungsprobleme und gezielter Fix

- Den verbleibenden Restzustand gezielt gegen die zwei noch fehlerhaften Home-Bereiche geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft. Die entscheidende technische Ursache fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Pflichtstundenfehler lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ sich jetzt direkt aus dem WPF-Debug-Log bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigen: `LoadPflichtstundenSummaryAsync(...)` erzeugte eine PostgREST-Fehlermeldung `column v_pflichtstunden_uebersicht.mitglied_id does not exist`.
- Damit war der Kernfehler klar: der Home-Pfad filterte serverseitig auf eine im bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten View-Kontext nicht vorhandene Spalte `mitglied_id`; die Pflichtstunden-Section fiel deshalb trotz vorhandener Viewdaten in den Fehlerpfad, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` weiter sauber laden konnten.
- Den Pflichtstundenpfad deshalb gezielt repariert, ohne neue Fachlogik zu bauen: `v_pflichtstunden_uebersicht` wird fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home jetzt zunÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chst ohne den fehlerhaften serverseitigen Mitgliedsfilter gelesen; die Auswahl des passenden Datensatzes erfolgt anschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸end robust clientseitig ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den relevanten Home-Bezug `hauptmitglied_id` bzw. vorhandene Mitgliedszuordnungen und die aktuelle Saison.
- Die Record-Abbildung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt den wahrscheinlichen Hauptmitgliedsbezug (`hauptmitglied_id`) jetzt explizit, damit der bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte DB-Kern auch im App-Mapping nachvollziehbar ausgewertet werden kann.
- FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen ergab die Ursachenanalyse kein erneutes Section-Ladeversagen, sondern einen irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrenden leeren Placeholder-Zustand bei erfolgreichem Laden ohne zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgelieferte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge. Deshalb wurde der alte Text `kein belastbarer Bekanntmachungs-Pfad angebunden` durch einen fachlich ehrlicheren Empty-State ersetzt und die leere Section zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich gezielt im Logger/Debug-Ausgabekanal protokolliert.
- Kleine technische Transparenz ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: der Home-Pfad schreibt jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden und leere Bekanntmachungs-Sections nachvollziehbare Diagnosehinweise in Logger/Debug-Ausgabe, ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Logging-Flut im Normalfall zu erzeugen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Ursachenreparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur Teil 2: Pflichtstunden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber Hauptmitglied/Saison prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤zisiert und Bekanntmachungen robuster gemappt

- Den verbleibenden Home-Restzustand erneut gegen die beiden Problemfelder geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: da `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` und `Termine` bereits erschienen, war ein global falscher Home-Grundpfad weniger wahrscheinlich als zwei gezieltere Ursachen ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Pflichtstunden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den falschen Mitgliedsbezug bzw. zu grobe SaisonauflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung und Bekanntmachungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber ein zu starres Record-Mapping auf die Startseiten-View.
- Den Pflichtstundenpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home deshalb final prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤zisiert, ohne neue Fachlogik zu bauen: vor dem Laden aus `v_pflichtstunden_uebersicht` wird jetzt der fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home relevante Hauptmitgliedsbezug aufgelÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶st; liegt beim aktuellen Mitglied ein `hauptmitglied_id` vor, wird die PflichtstundenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber dieses Hauptmitglied geladen.
- ZusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich wird die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte aktuelle Saison jetzt nicht mehr nur lose bei der Nachsortierung berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt, sondern zuerst direkt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die View-Abfrage verwendet; Home fragt damit bevorzugt exakt den Datensatz `(haupt- bzw. zielmitglied, aktuelle saison_id)` aus `v_pflichtstunden_uebersicht` ab und fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt erst danach auf Jahres-/letzten Datensatz zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den Bekanntmachungs-Record auf realistischere View-Abweichungen tolerant gemacht: `StartseiteBekanntmachungRecord` bildet jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶gliche Aliasfelder wie `bekanntmachung_id`, `betreff`, `text`, `inhalt_html`, `kurztext`, `datum` und `erstellt_am` ab, damit produktive Startseiten-Views nicht schon an kleineren Spaltenabweichungen leer laufen.
- Das Home-Mapping fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen nutzt diese Felder jetzt gezielt als Fallback-Kette fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Titel, Inhalt und VerÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffentlichungsdatum; dadurch bleibt der vorhandene Detailbereich unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert klein, zeigt aber reale View-Daten robuster an, statt frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼h in den Placeholderzustand zu fallen.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Restfix weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ HomeView-Reparatur: globalen Leerlauf durch geschluckte Home-Section-Fehler behoben

- Den aktuellen Home-Ladepfad in `SupabaseService.GetHomeOverviewAsync(...)`, den Startseiten-Records und `HomeViewModel` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft; dabei zeigte sich als wahrscheinlichster Kernfehler nicht mehr das grundlegende View-Naming, sondern das Fehlerverhalten des Gesamtladers: sobald eine einzelne Home-Section beim Laden/Mappen scheitert, fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt wegen des globalen `ExecuteAsync(..., fallback)` bislang der komplette Home-Block still auf einen leeren Factory-Stand zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Den Verdacht technisch abgesichert: ein direkter Test auf den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Termin-View lieferte im isolierten REST-Zugriff einen Berechtigungsfehler; unabhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngig vom konkreten DB-/Session-Kontext ist damit bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt, dass einzelne Section-Fehler im Home-Pfad realistisch sind und bisher den kompletten Home-Inhalt leerfallen lassen konnten.
- `GetHomeOverviewAsync(...)` deshalb gezielt robust gemacht statt neu designt: Pflichtstunden, ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze, Termine und Bekanntmachungen werden jetzt jeweils separat geladen; wenn eine Section scheitert, bleiben die anderen Section-Daten erhalten und die Home-Seite fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llt nicht mehr komplett auf leere Platzhalter zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck.
- Kleine, gezielte technische Transparenz ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: fehlgeschlagene Home-Sections werden jetzt im vorhandenen Logger und zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich per `Debug.WriteLine` mit Section-Namen protokolliert, ohne dauerhafte Log-Flut im Normalfall zu erzeugen.
- Die Empty-State-Texte fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die drei Startseitenbereiche unterscheiden jetzt zwischen `keine Daten vorhanden` und `Laden der Section fehlgeschlagen`; damit bleibt der echte Grund im WPF-/MAUI-Test nachvollziehbar, statt pauschal eine leere Startseiten-View zu behaupten.
- Den Pflichtstundenpfad zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich vereinfacht und nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤her auf den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Kern gezogen: die Home-Zusammenfassung wird jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Mitglied direkt aus `v_pflichtstunden_uebersicht` geladen, ohne dass ein zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlicher App-seitiger Demo-/Test-Filter den View-Datensatz schon vorab ausblendet; die Filterregel bleibt damit dort, wo sie fachlich hingehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rt ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ im bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-/Produktpfad.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Reparatur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Home-Diagnose: bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigte Pflichtstunden- und Startseiten-Views sauber auf den realen DB-Stand korrigiert

- Den aktuellen Home-Istzustand in `ISupabaseService`, `SupabaseService`, den Home-Records/DTOs sowie `HomeViewModel` gezielt gegen den inzwischen bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten DB-Schema-Stand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft; dabei zeigte sich, dass die Home-Anbindung zwar grundsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich auf Startseiten-Views setzte, aber bei `Termine` und `Bekanntmachungen` noch falsche Singular-Viewnamen im Code hinterlegt waren.
- Die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Startseiten-Views sauber nachgezogen: `StartseiteTerminRecord` verwendet jetzt `public.v_startseite_termine` statt des bisherigen falschen `v_startseite_termin`, und `StartseiteBekanntmachungRecord` zeigt auf `public.v_startseite_bekanntmachungen` statt auf einen veralteten Singular-Namen.
- Den Pflichtstundenpfad gegen den bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Kern geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft, ohne neue App-Logik einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren: `PflichtstundenUebersichtRecord` bildet jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich `saison_id` ab, und `SupabaseService.LoadPflichtstundenSummaryAsync(...)` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶st zuerst die aktuelle Saison ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `saison` auf und greift dann bevorzugt genau auf den passenden Datensatz aus `v_pflichtstunden_uebersicht` zu, statt nur lose ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber ein Jahres-Fallback zu gehen.
- Keine lokale Sollstunden-Berechnung aufgebaut oder beibehalten: Home liest `pflichtstunden_soll`, `geleistete_stunden`, `offene_stunden` und die Regelhinweise weiter direkt aus `v_pflichtstunden_uebersicht`; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Alters-, Wartungsvertrags- oder Nebenmitglied-Logik wird in der App nicht nachkonstruiert.
- Die restliche Home-Mappinglogik bewusst klein gehalten: vorhandene Text-/Markup-Normalisierung bleibt nur soweit erhalten, dass echte Startseiteninhalte nicht kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlich verloren gehen; der wesentliche Datenverlust lag im geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ften Stand an den falschen Viewnamen und der zu schwachen Saisonzuordnung.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach der Korrektur weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 3: Letzte kleine RestlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber MAUI-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus geschlossen und Block abgeschlossen

- Den verbliebenen Restzustand erneut gegen die zwei mÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶glichen Abschlussoptionen geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: ein echter gemeinsamer Ablehnungs-/RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe-/Kommentarpfad wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤re trotz vorhandenem Statusstring weiterhin nicht belastbar genug, weil dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r im aktiven Modell und Servicepfad keine saubere fachliche RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe-/Kommentarbasis vorhanden ist; der bereits existierende MAUI-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad war dagegen der klar belastbarere Abschlusskandidat.
- Deshalb bewusst Option B gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt und den vorhandenen mobilen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad auf denselben gemeinsamen Statuskern wie in WPF gebracht, statt einen neuen Ablehnungs-Workflow zu erfinden.
- `AdminShell` zeigt `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` mobil jetzt nur noch dann an, wenn ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle vorliegen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich erscheint die aktuelle Anzahl offener DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze direkt im Titel des MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼punkts.
- Die bestehende mobile `ArbeitsstundenReviewPage` aktualisiert diesen Shell-Status jetzt nach dem Laden sowie nach Genehmigen/Ablehnen direkt wieder ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad, sodass MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼eintrag und tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche offene FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lle konsistent bleiben.
- Keine neue MAUI-Schattenlogik eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die mobile Seite bleibt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig auf dem bereits produktiven Review-Unterbau mit `GetUnapprovedArbeitsstundenByMitgliedAsync()`, `GetArbeitsstundenAsync()` und `UpdateArbeitsstundeAsync()`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus der mobilen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatusanzeige drauÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem letzten kleinen Schritt weiterhin erfolgreich; der Arbeitsstunden-Block ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 2: Admin-/Vorstands-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fflow in WPF bis zur Freigabe vervollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndigt

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitsstundenPruefungView`, `ArbeitsstundenView`, Dialog, Navigation und die Arbeitsstunden-Servicepfade erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren bereits PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste, Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler und die bestehende Bearbeitungsansicht, aber aus der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste heraus lief der Weg noch zu unspezifisch nur in die allgemeine Mitgliedsliste der Arbeitsstunden.
- Den PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad gezielt auf den bestehenden WPF-Arbeitsstundenpfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt statt neue Parallelansicht zu bauen: aus `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` wird jetzt die vorhandene `ArbeitsstundenView` im kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet, gefiltert auf die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offenen DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze des ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Mitglieds.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r nur einen kleinen Navigationskontext ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt, keine neue Gesamtarchitektur: `ArbeitsstundenNavigationContext` steuert fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die bestehende `ArbeitsstundenViewModel`-Logik, ob Nebenmitglied-Daten einbezogen werden oder ob nur offene PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼feintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge des konkret ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Mitglieds im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus erscheinen.
- Admin/Vorstand kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶nnen im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus jetzt nicht nur sichten, sondern auch entscheiden: die bestehende Arbeitsstundenansicht bietet dort zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich eine direkte `Freigeben`-Aktion fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den ausgewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten offenen Datensatz; Doppelklick/Bearbeiten bleibt als vorhandener Produktpfad weiter nutzbar.
- Ablehnen/RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe weiterhin bewusst nicht erfunden: im aktiven Modell gibt es zwar einen Statusstring, aber kein belastbar vorhandenes UI-/Service-Fachmodell fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r einen sauberen RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgabe- oder Kommentarpfad; daher wurde dieser Zweig in diesem Block nicht spekulativ aufgebaut.
- Zustandskonsistenz nach Entscheidungen gesichert: nach Freigabe oder Bearbeitung werden Arbeitsstundenliste, PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste und Badge/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler weiterhin ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad bzw. den vorhandenen gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fservice `GetUnapprovedArbeitsstundenByMitgliedAsync()` aktualisiert.
- Die bestehende Arbeitsstundenansicht im PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fmodus fachlich geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: klarer Titel, Hinweistext, leerer PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzustand ohne Restdaten sowie Statusspalte in der Liste, damit offene/erledigte ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand nachvollziehbar bleiben.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad verwendet wird.
- Technisch verifiziert: `KGV.Wpf` baut nach der VervollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndigung des PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fflows erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Arbeitsstunden-Flow Prompt 1: Home-Einstieg, Neu-Erfassung und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus in WPF angeschlossen

- Den aktuellen Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeView`/`HomeViewModel`, `ArbeitsstundenViewModel`, `ArbeitsstundeDialog`, Navigation und den gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: ein belastbarer Listen-/Detailpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Arbeitsstunden war bereits vorhanden, aber aus Home noch nicht nutzbar, die Neu-Erfassung hing weiterhin an einem noch nicht produktiven `AddArbeitsstundeAsync`, und ein echter WPF-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus in der Navigation fehlte komplett.
- Den ersten Interaktionsschritt aus Home sauber angeschlossen: der Wert `geleistete Stunden` im oberen Bereich `Meine Arbeitsstunden` ist jetzt klickbar und ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnet belastbar die bestehende WPF-`ArbeitsstundenView` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Mitglied statt neuer Schattenlogik.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den MainWindow-Kontext minimal erweitert statt neue Navigationsarchitektur zu bauen: das aktuelle Mitglied kann nun bei Bedarf auch fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand aus dem Benutzerkontext geladen werden, damit der Home-Einstieg in die eigenen Arbeitsstunden zuverlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ssig funktioniert.
- Bestehende Arbeitsstundenansicht produktiv nutzbar gemacht: `AddArbeitsstundeAsync` und `DeleteArbeitsstundeAsync` im `SupabaseService` sind jetzt aktiv, sodass die vorhandene WPF-`Neu`-/Bearbeiten-Logik nicht mehr am Platzhalter hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngt.
- Die neue Erfassung fachlich auf den echten Statuspfad gezogen: normale Benutzer erfassen weiterhin nicht freigegebene EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge, Vorstand/Admin kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶nnen wie bisher direkt freigeben; es wurde keine lokale Freigabesimulation oder neue Freigabearchitektur eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Das Arbeitsstunden-Formular geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft statt neu erfunden: Pflichtfelder sind jetzt mit `*` markiert, fehlende Eingaben werden direkt im bestehenden `ArbeitsstundeDialog` rot umrandet, und der Aktionsbutton am Formularende heiÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸t konsistent `Speichern`.
- Kleinen WPF-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene Arbeitsstunden ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neue `ArbeitsstundenPruefungViewModel`/`ArbeitsstundenPruefungView` nutzen den vorhandenen gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` und fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren von der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fliste direkt in die bestehende Arbeitsstundenansicht des betroffenen Mitglieds.
- Navigation auf echten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstatus gestellt: der Eintrag `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` erscheint fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r berechtigte Benutzer jetzt nur bei tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich offenen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llen, zeigt einen kleinen ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler mit der Zahl offener DatensÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und wird bei offenem PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstand sichtbar hervorgehoben; Aktualisierung lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft nach ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `ArbeitsstundenChangedMessage`-Pfad.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block aus PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼flisten und PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlern ausgeschlossen, weil weiterhin ausschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸lich der gemeinsame operative PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad genutzt wird.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Arbeitsstunden-Hotfix erfolgreich; zur Konsistenz wurde auch `KGV.Maui` gebaut und bleibt weiterhin buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ WPF-Home-Datenbankanbindung: Startseiten-Views und Pflichtstundenpfad eingebunden

- Den aktuellen Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeViewModel`, `HomeView`, `HomeOverviewDTO`, `ISupabaseService` und `SupabaseService` erneut gegen die geklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rten produktiven DB-Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: der obere Arbeitsstundenbereich rechnete noch lokal aus `arbeitsstunde`, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen` auf WPF-Home weiterhin nur Platzhalter-/LeerzustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde nutzten.
- Gemeinsamen Home-Datenpfad auf den belastbaren Stand erweitert statt UI-Logik weiter aufzublÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hen: `HomeOverviewDTO` trÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤gt jetzt zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich gemeinsame Startseiten-Daten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Pflichtstunden, ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze und Termine; `SupabaseService.GetHomeOverviewAsync(...)` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt diese Inhalte direkt aus den geklÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rten View-Pfaden.
- Oberen Bereich `Meine Arbeitsstunden` sauber auf `v_pflichtstunden_uebersicht` umgestellt: Soll-, geleistete und offene Stunden werden nicht mehr lokal in WPF berechnet, sondern aus dem zentralen Pflichtstundenpfad gelesen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich werden die Befreiungs-/Regelhinweise (`hat_wartungsvertrag`, `altersbefreit`, `ist_befreit`, `regelgrund`) fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Home-Ausgabe mitgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Bereich `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze` an `v_startseite_arbeitseinsatz` angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge mit Titel, Datum/Zeit, Treffpunkt sowie KapazitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ts-/Anmeldehinweisen werden jetzt auf Home angezeigt; ein eigener Anmelde-Flow wurde bewusst nicht erfunden, weil dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r im aktiven WPF-Stand weiterhin kein belastbarer Produktpfad vorhanden ist.
- Bereich `Termine` an `v_startseite_termin` angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge erscheinen jetzt klickbar im WPF-Home; der Detailbereich zeigt Thema, Datum/Zeit, Ort und weiterfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende Informationen nur aus den belastbar vorhandenen View-Feldern und lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤sst sich wieder mit `SchlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en` beenden.
- Bereich `Bekanntmachungen` an die vorhandene Startseiten-View fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Bekanntmachungen angebunden: echte EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge werden geladen, Titel bleiben anklickbar und der Detailbereich zeigt die verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbaren Inhalte statt des bisherigen kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen Leerstands; eine neue HTML-Redaktionsplattform wurde bewusst nicht begonnen.
- Kleine Anzeige-Normalisierung ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Startseiten-Texte aus Termin-/Bekanntmachungsviews werden fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home kompakt aus vorhandenem Markup bereinigt, ohne eine neue Inhaltsarchitektur einzufÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hren.
- Demo-/Play-Store-Testdaten bleiben beim Pflichtstunden-/Home-Kontext ausgeschlossen: die obere Pflichtstundenanzeige wird fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r nicht operative Mitgliedskontexte weiterhin nicht gebildet.
- Technisch verifiziert: `KGV.Wpf` baut nach der Umstellung erfolgreich; zur Konsistenz wurde auch `KGV.Maui` mitgebaut und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ WPF-Hotfixblock: Login-Lesbarkeit, Home-Start und Home-Zielzustand auf belastbaren Stand gebracht

- Den aktuellen WPF-Istzustand fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Login, Startnavigation und Home erneut gegen den aktiven Produktstand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: konkret auffÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤llig waren schlecht lesbare Login-Aktionsbuttons, ein zu kleiner Passwort-Zusatzbutton, der Start auf `Mitgliedersuche` statt `Startseite` sowie eine Home-Ansicht, deren bisheriger Aufbau nicht dem gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschten Zielzustand entsprach.
- Lesbarkeit und Bedienbarkeit im WPF-Login gezielt korrigiert: die Aktionsbuttons verwenden jetzt einen grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸eren, lesbaren Login-spezifischen Zuschnitt mit sauberer Umbruchdarstellung; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich wurde der Passwort-Zusatzbutton rechts im Passwortbereich von einem zu kleinen Overlay auf einen klar bedienbaren `Anzeigen`-Button mit eigenem Platz umgestellt.
- WPF-Startpfad nach Login auf die `Startseite` zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `MainWindowViewModel` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤dt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Eigenkontext weiter den nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tigen Mitgliedskontext vor, navigiert danach aber standardmÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ig nach `Home` statt direkt in die `Mitgliedersuche` bzw. `Meine Daten`.
- WPF-Home fachlich auf einen belastbaren Zielaufbau umgebaut: oben steht jetzt ein klarer Bereich `Meine Arbeitsstunden` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuelle Kalenderjahr mit belastbar ableitbaren Werten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r freigegebene und noch offene Stunden; `Sollstunden` bleibt bewusst ehrlich als derzeit nicht belastbar verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar gekennzeichnet, weil im aktiven Stand kein belastbarer Sollstundenpfad vorhanden ist.
- Darunter die gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nschte Dreiteilung fachlich sauber auf den aktiven Stand gezogen: `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, `Termine` und `Bekanntmachungen` erscheinen jetzt als klare Spaltenbereiche; fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` bleibt die vorhandene Listen-/Detaillogik aktiv und wurde um einen `SchlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en`-Button ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Weiterhin bewusst nicht erfunden: fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ArbeitseinsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze`, sonstige `Termine` und belastbare `Bekanntmachungs`-Verwaltung existieren im aktiven Workspace weiterhin keine tragfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤higen WPF-Service-/View-Pfade; deshalb zeigt Home dort ehrliche Empty-/Admin-Hinweise statt neuer Schattenarchitektur oder Fake-Formulare.
- Technisch verifiziert: `KGV.Wpf` baut nach dem Hotfixblock weiterhin erfolgreich; die aktive MAUI-Basis wurde zur Konsistenz ebenfalls mitgeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 3: Gemeinsamen UserRoles-Kern als letzten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` und den aktiven Produktpfaden erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: vorhanden waren bereits Filter-, Export- und Home-Kern-Tests; weiterhin ungetestet blieb aber der kleine gemeinsame Rollen-Kern `UserRoles`, der inzwischen in WPF, MAUI, Home-Logik und Benutzerverwaltung zentral mitverwendet wird.
- Als letzten sinnvollen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Block 6 bewusst genau diesen Core-Baustein gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: fachlich wichtig, technisch klar abgegrenzt und ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Infrastruktur direkt testbar.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `UserRolesTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: abgesichert werden jetzt die robuste Rolleninterpretation per `Parse(...)`, die normalisierten Storage-Werte aus `ToStorageValue(...)` sowie die stabile gemeinsame Reihenfolge von `AssignableRoles`.
- Der Block bleibt bewusst klein und produktnah: keine UI-Tests, keine kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen Mocks und keine Archivannahmen, sondern nur der aktuelle Shared-Core-Pfad, von dem mehrere aktive Produktstellen direkt abhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ngen.
- Technisch verifiziert: `KGV.Tests` baut, alle aktiven Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig; Block 6 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 2: Gemeinsamen HomeOverviewFactory als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad abgesichert

- Den Istzustand von `KGV.Tests` erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: aktiv vorhanden waren jetzt der Demo-/Testdatenfilter-Block und der wiederhergestellte Export-Test, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der gemeinsame Home-Kern trotz hoher fachlicher Relevanz fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF und MAUI noch ungetestet blieb.
- Als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad bewusst den `HomeOverviewFactory` gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: er ist ein klar abgegrenzter produktiver Core-Pfad, steuert die gemeinsamen Home-Schnellzugriffe fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Desktop und Mobil und lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤sst sich ohne zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Testinfrastruktur direkt prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `HomeOverviewFactoryTests` in die bestehende aktive Teststruktur ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt statt weitere Archivideen zu reaktivieren: abgesichert werden jetzt die fachlich erwarteten Schnellzugriffs-Sets fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Admin`, `Vorstand` und `User`.
- Der Testblock bleibt bewusst klein: keine UI-Tests, keine Shell-/Navigations-Infrastruktur und keine kÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nstlichen String-Volltests, sondern nur der belastbare Shared-Core-Entscheidungspfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r die Home-Quicklinks.
- Technisch verifiziert: `KGV.Tests` baut weiter, die Tests laufen erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 1b: Archiviertes KGV.Tests sauber mit aktivem Root-Stand zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt

- Den Istzustand von aktivem Root-`KGV.Tests` und `_Archiv\KGV.Tests` erneut gegeneinander geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: im Root lag bislang nur der neue kleine `OperationalDataFilter`-Block, im Archiv dagegen die frÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼here WPF-nahe Teststruktur mit `ExportViewModelTests` und Windows-zielender Projektdatei.
- Das archivierte Testprojekt nicht blind zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen, sondern sinnvoll zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die aktive Root-Projektdatei ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernimmt jetzt wieder die fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen Stand passende Windows-/WPF-Teststruktur aus dem Archiv, wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hrend der neue `OperationalDataFilter`-Block vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig erhalten bleibt.
- Fachlich passende ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere Tests sauber zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt: `ExportViewModelTests` wurden als weiterhin sinnvoller, kleiner PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen produktiven Export-Code reaktiviert, statt als bloÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸es Archivmaterial liegen zu bleiben.
- Veraltete Recovery-/Archivannahmen bewusst nicht breit aktiviert: nur die fachlich weiterhin passende Struktur und der konkrete Export-Test wurden ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bernommen; keine pauschale Reaktivierung weiterer Altlasten.
- Aktives `KGV.Tests` ist damit wieder ein sauber zusammengefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrtes Testprojekt aus aktuellem Root-Stand plus sinnvoller Archivstruktur, nicht zwei konkurrierende TestansÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tze nebeneinander.
- Technisch verifiziert: `KGV.Tests` baut im wiederhergestellten Stand, `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft erfolgreich, und die aktive Produktbasis (`KGV.Wpf`, `KGV.Maui`) bleibt buildfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hig.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 6 Prompt 1: Kleine aktive Testbasis fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Demo-/Testdatenfilter zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt

- Die aktuelle Testlage geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: im aktiven Root fehlte weiterhin ein reales `KGV.Tests`-Projekt, obwohl die LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung bereits auf diesen Pfad zeigte; im Archiv lag noch eine ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ltere Testidee fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `ExportViewModel`, die fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den jetzigen Fokus aber nicht der passendste Wiedereinstieg war.
- Als ersten kleinen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad bewusst die Demo-/Play-Store-Testdatenfilterung gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlt: sie ist fachlich wichtig, technisch klar abgegrenzt und wirkt inzwischen in mehreren produktiven Pfaden (`Home`, Benutzerverwaltung, mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung) ohne dass dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine groÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸e Testinfrastruktur nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tig wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤re.
- DafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r eine kleine aktive Testbasis im Root zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgeholt: neues aktives `KGV.Tests`-Projekt mit minimalem xUnit-Unterbau und direkter Referenz auf `KGV.Core`, statt Archiv-/Recovery-Annahmen ungeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckzuziehen.
- Den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad mit fokussierten Tests abgesichert: `OperationalDataFilterTests` prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen jetzt belastbar, dass offensichtliche Demo-/Test-/Play-Store-Marker bei Mitgliedern und App-User-Kontexten aus operativen Pfaden ausgeschlossen werden, reale Daten aber durchlaufen.
- Archivierte Export-Tests bewusst nicht mit zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgezogen: sie waren fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen ersten kleinen Testblock nicht der beste fachliche Kandidat und hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tten den Wiedereinstieg unnÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tig verbreitert.
- Technisch verifiziert: `KGV.Tests`, `KGV.Wpf` und `KGV.Maui` bauen; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤uft `dotnet test KGV.Tests\KGV.Tests.csproj -c Debug --no-build` erfolgreich mit 4/4 grÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼nen Tests.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 3: Letzte kleine mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken erneut gegen belastbare WPF-/MAUI-Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die wichtigste kleine Rest-Sackgasse lag bei `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen`, weil der mobile Admin-Pfad bereits in der `AdminShell` existierte, fachlich aber weiterhin am fehlenden gemeinsamen Servicepfad `GetUnapprovedArbeitsstundenByMitgliedAsync()` hing.
- Genau diese letzte kleine ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Block 5 priorisiert und geschlossen: der gemeinsame Supabase-Service liefert jetzt wieder belastbar die Mitgliedergruppen mit offenen Arbeitsstunden, sodass die bestehende mobile PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fseite ohne neue Architektur auf ihrem vorhandenen Pfad nutzbar wird.
- Keine neue Schattenlogik eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: die Umsetzung bleibt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im gemeinsamen `SupabaseService` und verwendet die bereits vorhandenen `ArbeitsstundeRecord`-/`MitgliedRecord`-Modelle sowie die bestehende mobile `ArbeitsstundenReviewPage`.
- Demo-/Play-Store-Testdaten direkt im betroffenen Verwaltungsweg ausgeschlossen: die neue Gruppenbildung fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r offene Arbeitsstunden berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt nur operative Mitglieder ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bestehenden `OperationalDataFilter`, damit keine Testkonten in die mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung hineinlaufen.
- Nur den blockrelevanten Kern geschlossen: keine neue Gesamtplattform fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Arbeitsstunden, keine zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlichen WPF-OberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen und keine Ausweitung auf weitere Admin-Baustellen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem dritten Block-5-Schritt weiterhin erfolgreich; Block 5 ist damit sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 2: NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Rollenbearbeitung geschlossen

- Die verbleibenden mobilen Admin-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken erneut gegen die produktiven WPF-Wege geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: nach der mobilen Benutzerverwaltung blieb die Rollenbearbeitung die nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste kleine, aber fachlich wichtige ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke, weil WPF dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r bereits einen belastbaren Sperr-/Update-Pfad im `AdminRoleViewModel` nutzt, MAUI in der neuen Benutzerverwaltung aber bislang nur lese- und Einladungsaktionen bot.
- Genau diese eine ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tslÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke priorisiert und geschlossen: die mobile `UserManagementPage` kann jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r belastbar zugeordnete Mitglieder auch die Rolle ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndern und speichern, statt an dieser Stelle weiter als Admin-Sackgasse zu enden.
- Bestehenden Unterbau wiederverwendet statt neue Schattenlogik aufzubauen: MAUI nutzt jetzt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r RollenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungen denselben Kern aus `TryLockMitgliedAsync`, `GetMitgliedByIdAsync`, `UpdateMitgliedAsync` und `ReleaseLockMitgliedAsync`, den der WPF-Adminpfad bereits verwendet.
- Rollenliste zwischen WPF und MAUI auf einen gemeinsamen Core-Stand gebracht: `UserRoles.AssignableRoles` liefert jetzt die zulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ssigen Rollen zentral, sodass beide Clients denselben belastbaren Auswahlumfang nutzen.
- Gemeinsame Anzeige nach dem Speichern geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: die operative Benutzerliste bevorzugt nun die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chliche Mitgliedsrolle vor einem evtl. ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lteren `app_user`-Wert, damit RollenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungen nach Refresh in WPF und MAUI konsistent sichtbar sind.
- Demo-/Play-Store-Testdaten bleiben auch in diesem Block drauÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en: die bereits im gemeinsamen Benutzerlistenpfad eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrte Filterung greift weiterhin, sodass keine Testkonten in die mobile Rollenverwaltung hineinlaufen.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem zweiten mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 5 Prompt 1: NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t mit Benutzerverwaltung geschlossen

- Den Istzustand der mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t erneut gegen die aktiven WPF-Verwaltungswege geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: die deutlichste belastbare LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke lag bei der `Benutzerverwaltung`, weil WPF bereits einen produktiven Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Benutzer-/Mitgliedszuordnungen, Einladung/Erstlogin und Passwort-Reset hatte, MAUI dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r aber noch gar keinen echten Admin-Arbeitsweg bot.
- Diese LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke als nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chsten kleinen Kernblock priorisiert: sie ist fachlich relevant fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Admin/Vorstand, baut vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig auf bestehenden `IAuthService`-/`AuthService`-Pfaden auf und braucht keine neue Gesamtarchitektur.
- Neue mobile `UserManagementPage` plus `UserManagementViewModel` fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r MAUI ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und in die `AdminShell` aufgenommen: Benutzerliste laden, Einladung/Erstlogin-Code anstoÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en und Passwort-Reset versenden laufen mobil jetzt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben belastbaren Auth-Pfad wie in WPF.
- Die belastbar vorhandene E-Mail-ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungsgrenze auch mobil sauber berÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cksichtigt: eine E-Mail-ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderung wird in der neuen mobilen Benutzerverwaltung weiterhin nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das aktuell angemeldete Konto angeboten und nutzt denselben bestehenden OTP-/Verifikationsfluss statt neuer Admin-Schattenlogik.
- Demo-/Play-Store-Testdaten fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diese Verwaltungsliste direkt auf dem gemeinsamen Pfad ausgeschlossen: `AuthService.GetAppUsersAsync()` filtert offensichtliche Demo-/Test-/Play-Store-Mitglieder jetzt aus der operativen Benutzerverwaltung heraus, sodass WPF und MAUI dieselbe saubere Admin-Liste sehen.
- Nur notwendige WPF-Konsistenz bleibt implizit ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den gemeinsamen Auth-Pfad erhalten; keine neue WPF-OberflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che aufgebaut, weil der Desktop-Pfad bereits fachlich vorhanden war.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach diesem mobilen Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tsschritt weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 3: Home fachlich sauber abgeschlossen

- Den verbleibenden Home-Istzustand erneut gegen belastbare Pfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: offen war vor allem noch die Nutzer-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t zwischen WPF und MAUI beim direkten Zugang zu eigenen Arbeitsstunden; der Bekanntmachungs-Homepfad und der PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen` bleiben dagegen weiterhin bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb des belastbaren Home-Kerns.
- Letzten gemeinsamen Home-Schnellzugriff nachgezogen: `Meine Arbeitsstunden` ist jetzt Teil des gemeinsamen `HomeOverview`-Kerns fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den eigenen Benutzerkontext und zeigt in WPF und MAUI auf vorhandene lebende Pfade statt auf neue Schattenlogik.
- WPF-Nutzerpfad an den bereits vorhandenen Fachstand angepasst: der bestehende `ArbeitsstundenViewModel`-Pfad wird im Eigenkontext jetzt nicht nur indirekt, sondern auch sichtbar und sauber aus Home heraus nutzbar; damit schlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸t sich die bisherige Home-/Pfad-LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cke gegenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber MAUI.
- IrrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende leere Bekanntmachungs-DetailflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chen entschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: WPF und MAUI blenden den Detailbereich jetzt aus, solange auf Home kein belastbarer Bekanntmachungsinhalt vorhanden ist, statt eine tote Detailspalte bzw. einen leeren Detailkopf stehen zu lassen.
- Weiterhin bewusst nicht erweitert: keine neue Bekanntmachungs-Gesamtarchitektur, keine neue Kennzahlenplattform und keine Home-Verlinkung auf den weiterhin nicht rekonstruierten gemeinsamen PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Arbeitsstunden prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen`.
- Demo-/Play-Store-Testdaten bleiben auch im Abschlussstand aus Home-Aussagen ausgeschlossen; es wurde kein neuer Auswertungspfad ohne belastbare Filterbasis eingefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Home-Teilblock weiterhin erfolgreich; Block 4 ist damit fachlich sauber abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 2: Operative Home-Inhalte auf gemeinsamen Kern gesetzt

- Den aktuellen Stand der operativen Home-Inhalte geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar vorhanden waren vor allem bestehende Schnellzugriffe sowie der Arbeitsstunden-Datenpfad des aktuellen Mitgliedskontexts; ein gemeinsamer PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼f-/Bekanntmachungs-Backendpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home ist dagegen weiterhin nicht belastbar genug fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r neue Schnellzugriffe.
- Gemeinsamen Home-Datenpfad gezielt erweitert statt neue Client-Schattenlogik zu bauen: `ISupabaseService.GetHomeOverviewAsync(...)` liefert jetzt den Home-Kern zusammen mit operativen Hinweisen aus demselben Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r WPF und MAUI.
- NÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chste belastbare operative Home-Erweiterung auf Basis vorhandener Fachmodule umgesetzt: Home zeigt jetzt einen kompakten Arbeitsstunden-Hinweis fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den aktuellen Mitgliedskontext im laufenden Saisonbezug, inklusive offener PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde und automatischer Einbeziehung des vorhandenen Nebenmitglied-Pfads, wenn dieser fachlich belastbar vorhanden ist.
- Demo-/Play-Store-Testdaten dabei gezielt aus Home-Hinweisen herausgehalten: ein kleiner gemeinsamer Filter blendet offensichtliche Demo-/Test-/Play-Store-Mitglieder aus operativen Home-Zusammenfassungen aus, statt diese in Home-Aussagen mitzuzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlen.
- WPF- und MAUI-Startseite fachlich parallel nachgezogen: beide Clients lesen denselben Home-ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berblick, zeigen dieselben operativen Hinweise an und behalten nur die gemeinsamen lebenden Schnellzugriffe auf tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich nutzbare Pfade.
- Tote oder unsichere Home-Pfade bewusst nicht aufgenommen: die vorhandene mobile Arbeitsstunden-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fseite bleibt aus dem gemeinsamen Home-Kern heraus, solange der zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rige gemeinsame PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fpfad im aktiven `SupabaseService` noch nicht rekonstruiert ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem zweiten Home-Teilblock weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 4/3 Prompt 1: Home-/Startseiten auf gemeinsamen Kernstand gebracht

- Den Istzustand der aktiven Startseiten geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: WPF-`HomeViewModel` zeigte bisher eine rein aus Desktop-Navigation abgeleitete Modulliste plus leere Bekanntmachungen; MAUI-`HomeViewModel` zeigte nur Kontext und dieselbe leere Bekanntmachungssektion ohne gleichwertige Schnellzugriffe.
- Kleinsten belastbaren gemeinsamen Home-Umfang auf einen gemeinsamen Core-Pfad gezogen: neues `HomeOverviewDTO` mit `HomeQuickLinkItem`/`HomeQuickLinkKey` bÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ndelt jetzt den fachlichen Kern fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home zentral in `KGV.Core`, statt Schnellzugriffslogik getrennt in WPF und MAUI auszudrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken.
- Gemeinsame Home-Aussagen vereinheitlicht: beide Clients nutzen jetzt dieselbe Beschreibung des Home-Kerns, dieselben gemeinsamen Schnellzugriffe pro Rolle und dieselbe ehrliche Bekanntmachungs-Aussage, dass aktuell noch kein belastbarer Bekanntmachungs-Pfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home angebunden ist.
- WPF-Home auf den gemeinsamen Kernstand umgestellt: die bisher rein desktop-spezifische Modulaufl listing wurde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Home auf die gemeinsamen Kern-Schnellzugriffe reduziert; der tote Bekanntmachungs-`Bearbeiten`-Platzhalter wurde entfernt.
- MAUI-Home fachlich nachgezogen: gemeinsame Schnellzugriffe sind jetzt direkt von der Startseite aus erreichbar und springen auf die vorhandenen Shell-Routen fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Mitgliedersuche, Parzellen bzw. eigene Stammdaten.
- Demo-/Play-Store-Testdaten bewusst nicht in Home-Kennzahlen oder Hinweise eingerechnet: fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen Block wurde keine Summen-/Kennzahlenlogik aufgebaut, solange dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r noch kein belastbarer gemeinsamer Auswertungspfad vorliegt.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Home-Abgleich weiterhin erfolgreich.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 3: Parzellen-KernlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cken und mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t abgeschlossen

- Den Istzustand der Parzellenzentrale erneut gegen die vorhandenen Fachpfade geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: belastbar verfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼gbar waren bereits aktive ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler, Ablesungen, Belegungsstatus und Parzellen-Dokumente, aber MAUI nutzte davon in der Zentrale bislang nur einen Teil und lieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸ Strom-/Wasser-Verwaltung als Sackgasse stehen.
- Mobile Kernpfade ohne neue Parallelarchitektur direkt in der bestehenden `ParzellenPage` geschlossen: vorhandene Strom-/Wasser-Ablesungen und Parzellen-Dokumente werden jetzt vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig aus dem bestehenden Servicepfad geladen und in der zentralen Parzellenansicht angezeigt.
- Mobile Admin-ParitÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤t im Kern weiter angehoben: aus der MAUI-Parzellenzentrale sind jetzt auch Strom-Ablesungen speichern/bearbeiten, StromzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler tauschen, Wasser-Ablesungen speichern/bearbeiten, WasserzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler einbauen und ausbauen sowie das ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“ffnen aller Parzellen-Dokumente direkt erreichbar.
- WPF-Zentralansicht fachlich nachgezogen, ohne neue Logik zu erfinden: bereits im DTO vorhandene aktive Strom-/WasserzÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlerdaten und Dokument-Zeitpunkte werden in der Parzellen-Detailansicht jetzt klarer sichtbar gemacht.
- Weiterhin bewusst nicht erfunden: separate Anschlussflags, zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzliche Parzellen-Stammdaten oder neue Dokument-/ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler-Gesamtarchitektur wurden nicht aufgebaut; sichtbar bleibt nur, was im aktiven Modell, DTO und Servicepfad belastbar vorhanden ist.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem dritten Teilblock weiterhin erfolgreich; Block 3 ist damit fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r das Parzellenmodul fachlich und technisch im direkten Anschlussbereich abgeschlossen.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 2: Parzellen-Aktionen und MemberDetail-Sichtbarkeit nachgezogen

- Offene Parzellen-Verwaltungswege gezielt geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: zentrale Detailansicht aus Prompt 1 war vorhanden, aber die Servicepfade fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Zuordnung und Beendigungen waren im aktiven `SupabaseService` noch nicht umgesetzt und MAUI bot dafÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r noch keinen echten Arbeitsweg.
- Parzellen-Belegungsaktionen auf den bestehenden Pfad zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt statt neue Architektur aufzubauen: `AssignParzelleToMitgliedAsync` und `EndParzellenBelegungAsync` im `SupabaseService` implementiert; dabei werden ÃƒÆ’Ã†â€™Ãƒâ€¦Ã¢â‚¬Å“berschneidungen gegen den gewÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlten Starttag geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und Beendigungen direkt auf dem aktiven Belegungsdatensatz aktualisiert.
- Zentrale WPF-Parzellenansicht um direkte Verwaltungsaktionen ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: Mitglied zuordnen, Startdatum wÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hlen und aktive Belegung beenden jetzt direkt in der Detailansicht; bestehende FachanschlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼sse zu Mitglied, Strom, Wasser und Dokumenten bleiben erhalten.
- MAUI-Adminansicht `ParzellenPage` parallel nachgezogen: mobile ParzellenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht bietet jetzt ebenfalls direkte Zuordnung und Beendigung ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben gemeinsamen Servicepfad, damit die zentrale mobile Parzellenansicht keine reine Sackgasse bleibt.
- Das alte Sichtbarkeitsproblem der `MemberDetailView` an der Ursache geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und global behoben: das `GroupBox`-Template in `Themes/Controls.xaml` rendert Header und Inhalt jetzt in getrennten Zeilen statt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berlagernd, sodass Eingabefelder nicht mehr unter farbigen Header-/Bereichselementen verschwinden.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Teilblock weiterhin erfolgreich; bekannte bestehende Warnungen der rekonstruierten Basis wurden nicht als neuer Nebenblock aufgemacht.

## 2026-03-22 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 3/3 Prompt 1 Abschluss: Parzellen-Stammdaten technisch verifiziert und abgeschlossen

- Den bereits umgesetzten Stand von `ParzelleDetailDTO`, `GetParzelleDetailAsync`, der erweiterten WPF-Parzellenansicht und der neuen MAUI-`ParzellenPage` gezielt nur auf technische Konsistenz und AbschlussfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤higkeit geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft.
- Keine neue Fachlogik begonnen: der Teilblock blieb bei der vorhandenen zentralen Parzellen-Detailansicht mit belastbar sichtbaren Stammdaten, Belegung, Wasser-/Stromstatus aus ZÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤hler-/Ablesedaten und Dokumentbezug.
- Gemeinsamen Datenpfad bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: WPF und MAUI greifen auf denselben kleinen Service-Detailpfad fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Parzellen zu, statt parallele UI-Schattenlogik aufzubauen.
- Technische Verifikation fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r den Abschlusslauf vorgesehen auf `KGV.Wpf` und `KGV.Maui`; bekannte bestehende Warnungen der rekonstruierten Basis werden dabei nicht neu aufgemacht.
- Git-Abschluss dieses Teilblocks wird bewusst nur mit den zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rigen Parzellen-Dateien durchgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; blockfremde untracked Artefakte bleiben weiterhin auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb des Commits.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Archivblock 1/1: Root-Struktur nach Block 2 aufgerÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umt

- Root-Istzustand nach Abschluss von Block 2 geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft und archivwÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼rdige Bereiche eingegrenzt: sicher `_Recovery`, `_RecoveredArtifacts` und auf ausdrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼cklichen Wunsch auch `KGV.Tests`; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich nur klar nicht aktive Root-Hilfsdateien wie `Dateistruktur.txt`, `copilot-instructions.md` und `vergleich_android_wpf_memberdetail.png` in den Archivbereich ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Neuen Sammelordner `_Archiv` im Root angelegt und die bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigten Archivbereiche dorthin verschoben, damit die aktive Entwicklungsbasis im Root sichtbar auf `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui`, `supabase` und die nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶tigen Meta-Dateien reduziert ist.
- Aktive Verweise bereinigt: `KGV.slnx` enthÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lt nur noch die nicht archivierten Projekte; `.github/workflows/ci.yml` baut nur noch die aktive Basis ohne das archivierte Testprojekt.
- Root- und Metadokumente auf die neue Struktur nachgezogen: `README.md`, `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md` und `Documentation/DEVELOPMENT.md` ordnen `_Archiv` jetzt sauber ein; zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich beschreibt `_Archiv/README.md` den Zweck des Sammelordners.
- Fachlogik unverÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndert gelassen: keine ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾nderungen an Auth-, Home-, Parzellen-, Rollen- oder Release-Pfaden; der Schritt blieb rein strukturell.
- Abschluss technisch verifiziert: `KGV.Wpf` und `KGV.Maui` bauen nach dem Archivschritt weiterhin erfolgreich; `KGV.Tests` bleibt bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸erhalb der aktiven LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶sung und des aktiven CI-Laufs archiviert.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3 Abschluss: Einladung, Erstlogin und MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderung technisch verifiziert

- Den nach dem fachlichen Abschluss abgebrochenen Stand gezielt technisch nachgeprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: nur die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich geÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderten Auth-Dateien, WPF-/MAUI-AnschlÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼sse und `supabase/config.toml` erneut gesichtet.
- Echte Restfehler bereinigt statt neu umzubauen: die syntaktisch beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digte `KGV.Maui\Pages\MyProfilePage.cs` sauber geschlossen und die fehlende WPF-Command-Methode fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Mailadresse ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndern` in `MemberDetailViewModel` ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt.
- Auth-Abschlusslogik inhaltlich bestÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tigt: Admin-Einladung/Erstlogin, OTP-basierter Passwortwechsel, Passwort-vergessen entlang des OTP-Hauptwegs, separater OTP-MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderungsflow sowie gesperrtes E-Mail-Feld in den WPF-Stammdaten bleiben im korrigierten Stand konsistent.
- Technische Verifikation erfolgreich ausgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.Wpf` baut im Abschlusslauf erfolgreich; anschlieÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸end baut auch `KGV.Maui` erfolgreich. Sichtbar bleiben nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.
- Git-Abschluss fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r diesen Teilblock vorbereitet: nur die tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlich zugehÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶rigen Auth-/UI-/Konfigurationsdateien werden committed; blockfremde untracked Artefakte bleiben weiterhin bewusst auÃƒÆ’Ã†â€™Ãƒâ€¦Ã‚Â¸en vor.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3: OTP-/Auth-Unterbau auf Client-Flow konsolidiert

- Istzustand des Auth-/OTP-Unterbaus geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `IAuthService`, `AuthService`, `SupabaseClientFactory`, WPF-/MAUI-Konfigurationszugriffe und aktive Recovery-/OTP-Pfade gegen den bereits bereinigten Client-Flow abgeglichen.
- Recovery-/OTP-Hauptweg im `AuthService` konsolidiert: `RequestOtpAsync` und der separate Passwort-vergessen-Pfad laufen jetzt kontrolliert ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber denselben Recovery-Unterbau, ohne konkurrierende alternative Hauptpfade im Service stehen zu lassen.
- Service-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde bereinigt: Recovery-/OTP-Start setzt veraltete Auth-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ck; erfolgreicher Passwortwechsel beendet die Recovery-Session zusÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤tzlich per `SignOut`, damit der Client wie vorgesehen sauber in den normalen Re-Login zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehrt.
- Login-/Recovery-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde aufeinander abgestimmt: erfolgreicher Passwort-Login rÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤umt alte OTP-ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde auf, damit kein halbfertiger Recovery-Kontext in den regulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ren Loginpfad hineinragt.
- Publishable-Key-Umstellung nachgeschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: `SupabaseClientFactory` und WPF-Startup akzeptieren jetzt primÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤r `Supabase:PublishableKey` und bleiben nur kompatibel zu `Supabase:Key`; `appsettings.json` ist auf `PublishableKey` umgestellt.
- Toten Auth-Helfer bereinigt: die leere Datei `KGV.Infrastructure\MockAuthService.cs` aus der aktiven Codebasis entfernt.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 2/3: Auth- und Login-Einstiegspunkte clientseitig konsolidiert

- Istzustand der Auth-Einstiegspunkte in WPF und MAUI geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: aktiver Loginpfad, OTP-Anforderung, OTP-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung, Passwort-neu-setzen, Passwort-vergessen und Navigation nach erfolgreichem Login gegen Alt-/Platzhalterpfade abgeglichen.
- WPF-Loginpfad bereinigt: der Passwort-vergessen-Dialog wird jetzt aus dem aktiven `LoginViewModel` mit demselben `IAuthService` erzeugt, statt einen unverdrahteten Fallback-Dialog ohne Auth-Kontext zu ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¶ffnen.
- Toten WPF-Altpfad entfernt: `StartViewModel` als alter Platzhalter-Loginpfad aus der aktiven Codebasis entfernt.
- MAUI-Loginstart bereinigt: `App.xaml.cs` startet die App jetzt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber eine echte `NavigationPage` mit `LoginPage` statt ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber den bisherigen App-Platzhalter.
- MAUI-Loginflow geschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤rft: normales Login, OTP-PrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fung und Passwort-neu-setzen werden in `LoginPage` jetzt als getrennte ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde gefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; dadurch bleiben keine parallel sichtbaren konkurrierenden Auth-Teileinstiege mehr aktiv.
- Nach Passwortsetzen bleibt die Client-FÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrung sauber: RÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckkehr in den normalen Loginzustand mit erneuter Anmeldung ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber das neue Passwort.
- Toten MAUI-Altpfad entfernt: der veraltete, nicht mehr verwendete `AppShell`-Pfad wurde aus der aktiven Codebasis entfernt; aktiv bleiben nur `LoginPage`, `RoleChoicePage`, `AdminShell` und `UserShell`.
- MailÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nderung bewusst getrennt gelassen: keine Vermischung des Login-/Reset-Einstiegs mit dem vorhandenen `ChangeEmail`-Pfad.
- Endstand verifiziert: `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend sind nur die bereits bekannten, nicht blockierenden Warnungen aus der rekonstruierten Basis.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1/3 Abschluss: Konsolidierung verifiziert und abgeschlossen

- Git-Istzustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: Arbeitsbaum vor dem Abschlusslauf sauber; der Konsolidierungsstand lag bereits vollstÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ndig im aktuellen Stand vor.
- Root-/Doku-/Meta-Dateien gezielt verifiziert: zentraler Einstieg ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber `README.md`, Recovery-Bereiche als Referenz markiert, Mehrprojektstruktur dokumentiert, lokale Entwicklungsdoku vorhanden und keine fachlichen Codepfade erweitert.
- Kleine Restfehler bereinigt: beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digte Zeichen in `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` auf saubere Hinweistexte korrigiert.
- Kurze technische Verifikation ausgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.Wpf` und `KGV.Tests` bauen im Abschlusslauf erfolgreich; sichtbar bleiben nur die bereits bestehenden, nicht blockierenden Warnungen aus der rekonstruierten Basis, insbesondere Nullable-Warnungen in `KGV.Infrastructure\Services\SupabaseService.cs`.
- Block 1 damit inhaltlich abgeschlossen: aktive Arbeitsbasis, Recovery-Kontext, Dokumentation und Einstiegspunkte sind klar voneinander getrennt.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1/3: Quellstand als aktive Entwicklungsbasis konsolidiert

- Root-Aufbau und Meta-Dokumente geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `README_RETTUNG.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `DEV_LOG.md`, `ci.yml`, `KGV.slnx`, `Dateistruktur.txt`, `Documentation` sowie `_Recovery` und `_RecoveredArtifacts` gegen den tatsÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤chlichen Mehrprojektstand abgeglichen.
- Zentralen aktiven Einstieg ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: neues `README.md` als Startpunkt fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r Entwicklung, Build und Repo-Einordnung.
- Recovery-Bereiche klar gekennzeichnet: `_Recovery` und `_RecoveredArtifacts` jeweils mit eigener `README.md` als reine Archiv-/Referenzbereiche dokumentiert; `README_RETTUNG.md` auf Herkunfts-/Recovery-Kontext zurÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ckgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt.
- Architektur- und Entscheidungsdoku auf den realen Stand umgestellt: Mehrprojektstruktur mit `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf`, `KGV.Maui` und `KGV.Tests` dokumentiert; offene Unsicherheiten bewusst ehrlich benannt.
- Pragmatische Entwicklungsdoku ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt: `Documentation/DEVELOPMENT.md` beschreibt den empfohlenen Einstieg, lokale Voraussetzungen, typische Builds und die aktuelle Rolle von WPF, MAUI und Tests.
- IrrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrende Root-/Doku-Einstiegspunkte bereinigt: `Dateistruktur.txt` als historischer Snapshot umgewidmet, `Documentation/DEV_LOG.md` und `Documentation/CHANGELOG.md` von beschÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤digten/irrefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrenden Restinhalten auf klare Hinweisdateien umgestellt.
- Kleine technische Konsolidierung durchgefÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt: `KGV.slnx` um `KGV.Tests` ergÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nzt und das bisher wirkungslose Root-`ci.yml` in eine echte GitHub-Workflow-Datei unter `.github/workflows/ci.yml` ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼berfÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼hrt; Workflow auf den aktuellen SDK-Stand und die lokal belastbar prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fbaren Projekte ausgerichtet.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Prompt 1/2: Home/Startseite ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Bekanntmachungen + Bearbeiten-Buttons angeglichen

- Istzustand geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: `HomeViewModel`/`HomeView` in WPF war bisher nur ModulÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼bersicht; `HomePage` in MAUI war noch Platzhalter. Eine belastbare bestehende Bekanntmachungs-Datenquelle ist im aktuellen Workspace derzeit nicht vorhanden.
- WPF-Startseite gezielt erweitert: `Bekanntmachungen` zeigt jetzt nur Titel als Liste und die DetailflÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤che erst nach Auswahl; ohne Auswahl erscheint ein kompakter Hinweis, ohne EintrÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤ge ein kompakter Empty-State.
- MAUI-Startseite parallel angeglichen: `HomePage` ist jetzt als echte Startseite im Shell-MenÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ verdrahtet und nutzt denselben Listen-/Detail-Grundaufbau fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen`.
- Rollenlogik vereinheitlicht: `Bearbeiten` auf Home/Start ist nur fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `admin` und `vorstand` sichtbar, ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ber die vorhandene `UserContext.Role`-Logik in WPF und MAUI.
- Block bewusst klein gehalten: keine neue Backend-Architektur, keine neue Volltextdarstellung aller Bekanntmachungen auf der Startseite, keine fachfremden Umbauten.
- Abschluss technisch verifiziert: `KGV.Wpf` baut erfolgreich; `KGV.Maui` baut erfolgreich und enthÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤lt nach Korrektur der neuen Home-Layoutstellen nur noch die bereits bekannte, nicht blockierende Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.
- Datenquelle erneut geprÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼ft: weiterhin keine belastbare bestehende fachliche Quelle fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r `Bekanntmachungen` im aktuellen Workspace gefunden; deshalb bleibt bewusst der saubere strukturelle Listen-/Detail-Mechanismus mit kompaktem Leerzustand ohne Fake-Fachlogik aktiv.

## 2026-03-21 ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ Block 1: Auth/Login final abgeschlossen

- `AuthService`-OTP-/Recovery-Flow von lokalem Dev-Bypass auf echte Supabase-Recovery-Verifikation umgestellt; Passwortsetzen nutzt danach die authentifizierte Recovery-Session.
- WPF-Login finalisiert: `LoginViewModel`, `LoginWindow.xaml`, `LoginWindow.xaml.cs` und `App.xaml` auf saubere ZustÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¤nde fÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼r normales Login, OTP-Eingabe, Passwort-Neusetzen, Statusanzeige und kontextbezogene Enter-Aktion gebracht.
- MAUI-Login finalisiert: `LoginPage.xaml.cs` auf denselben Ablauf gebracht (`E-Mail + Passwort`, Passwort sichtbar/unsichtbar, OTP anfordern, OTP prÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¼fen, neues Passwort mit Wiederholung, Passwort vergessen).
- MAUI-Buildreparaturen abgeschlossen: `AppShell.xaml.cs` an XAML angeglichen (`partial` + `InitializeComponent()`), fehlende `MemberSearch`-Verdrahtung mit realer MAUI-VM an die vorhandene `MemberSearchPage.xaml` angebunden und die fremde WPF-Datei `KGV.Maui\Pages\DaschboardPage.xaml` entfernt.
- Asset-/Konfigurationskorrekturen abgeschlossen: `KGV.Maui.csproj` verwendet nun `Resources\AppIcon\appicon.svg`, `Resources\Splash\splash.svg` und die reale `..\appsettings.json`-Einbindung statt der veralteten `appsettings.enc`-Referenz; additionally `global.json` is fixed to the locally installed SDK version `9.0.310`.
- Kleinen Restfehler bereinigt: ungenutztes Ereignis `ShowResetPasswordRequested` aus `LoginViewModel` entfernt.
- Endstand verifiziert: `KGV.Core`, `KGV.Infrastructure`, `KGV.Wpf` und `KGV.Maui` bauen erfolgreich; verbleibend ist nur die dokumentierte, nicht blockierende MAUI-Warnung zu `Application.MainPage.set` in `KGV.Maui\App.xaml.cs`.



