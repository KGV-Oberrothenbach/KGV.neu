# KGV Software – Verbindliche technische Arbeitsregeln

**Stand:** 22.11.2026
**Geltung:** Alle neuen Chats, Startprompts, Reparaturblöcke, Fachrechte-Arbeitspakete, Prüfungen und Weiterentwicklungen der KGV-Software.

> **Verbindliche Startregel:** Vor Beginn jeder neuen Arbeit diese Datei auf `main` lesen und als technische Arbeitsgrundlage berücksichtigen. Jeder Startprompt muss auf diese Datei verweisen. Bei erkennbarem Änderungs- oder Ergänzungsbedarf den Nutzer ausdrücklich fragen, ob diese Datei erweitert oder angepasst werden soll. Regeländerungen erst nach Zustimmung durchführen.

Diese Datei beschreibt die **Arbeitsweise**, nicht den fachlichen Inhalt einzelner Arbeitspakete. Sie ersetzt weder die Kontrolle des aktuellen Iststands noch konkrete Auftrags- und Freigabeanforderungen.

## 1. Iststand, Zuständigkeiten und Sicherheit

- GitHub-Repository: `KGV-Oberrothenbach/KGV.neu`. Zu Beginn tatsächlichen Branch, HEAD-Commit, Arbeitsbaumzustand und einschlägige Dateien prüfen; frühere Chatangaben nicht ungeprüft übernehmen.
- Änderungen grundsätzlich nur im benannten Arbeitsbranch. **Direkte Änderungen an `main` nur nach ausdrücklicher Nutzeranweisung**; niemals eigenmächtig mergen oder veröffentlichen.
- DemoDB: `ymxhszxfutjnljiuciyj`. ProduktivDB/VDB: `itjcabiibuodkxayhvjq`.
- Vor Datenbankarbeiten Zielumgebung und Migrationsstand kontrollieren. **Keine produktiven Datenbankänderungen ohne ausdrückliche Freigabe.**
- Bestehende Benutzeränderungen, lokale Dateien und Git-Stände schützen. Kein ungefragtes `git reset`, `clean`, Überschreiben oder Löschen.
- WPF ist aus den laufenden Modernisierungsarbeiten ausgeschlossen, sofern nicht ausdrücklich anders beauftragt.

## 2. Supabase zwingend über NPX

**Nie stillschweigend die globale Supabase-CLI benutzen.** Standardaufruf:

```powershell
npx --yes supabase@2.121.0 <befehl>
```

- Für sämtliche Supabase-CLI-Aufrufe **NPX mit der expliziten Version `supabase@2.121.0`** verwenden, bis eine Änderung ausdrücklich beschlossen und geprüft ist.
- Vorhandene Supabase-Anmeldung wiederverwenden. Nicht bei jedem neuen Chat nach einem zusätzlichen Supabase Personal Access Token fragen.
- Kein automatisches CLI-Upgrade auf v3 oder andere Versionen.
- Für Datenbankarbeiten vorzugsweise einen getrennten temporären Supabase-Arbeitsordner verwenden, ohne die bestehende Repository-Verknüpfung zu verändern.
- Vor Remote-Operationen `supabase/.temp/project-ref` beziehungsweise die tatsächliche Projektverknüpfung kontrollieren und mit der vorgesehenen DemoDB/VDB abgleichen.
- SQL-Migrationen und Commits in sinnvollen, getesteten Blöcken bündeln, nicht jede kleine Änderung einzeln pushen.
- Externe Exitcodes kontrollieren; nach fehlgeschlagenem NPX-/Supabase-Aufruf abbrechen.

## 3. PowerShell-Skripte: reproduzierbar und fehlersicher

- Standardmäßig PowerShell 7 verwenden. Für ein Arbeitspaket möglichst **ein vollständiges, geprüftes `.ps1`-Skript und einen Startbefehl** liefern, nicht lange fehleranfällige Copy-and-paste-Folgen.
- **ZIP-Lieferungen immer mit automatischem Entpacken und Ausführen:** Der an den Nutzer gegebene **einzige Startbefehl** muss das heruntergeladene ZIP selbst finden, in einen temporären Ordner entpacken, die enthaltenen Dateien prüfen und das eigentliche Skript starten. **Niemals voraussetzen, dass der Nutzer zuvor manuell entpackt oder einen festen Entpackordner angelegt hat.**
- **Keinen ungeprüften Download- oder Entpackpfad behaupten.** Vor dem Ausführen per `Test-Path` kontrollieren; mögliche Browser-Dateisuffixe wie `(1)` berücksichtigen. Wenn die Datei fehlt, eine konkrete Fehlermeldung ausgeben statt mit einem nicht existierenden Skriptpfad fortzufahren.
- Paket und Startbefehl als Einheit prüfen: ZIP-Inhalt und Dateienamen müssen zum Startcode passen. Für zukünftige Pakete möglichst eine selbstentpackende PowerShell-Einstiegsroutine anbieten, die einen einzigen Copy-and-paste-Aufruf ermöglicht.
- `$ErrorActionPreference = 'Stop'` und `Set-StrictMode -Version Latest` setzen.
- **PowerShell-Ausführungsrichtlinie vor dem Start berücksichtigen:** Heruntergeladene, nicht digital signierte `.ps1`-Dateien können durch `RemoteSigned`/`AllSigned` oder die Herkunftsmarkierung (Mark-of-the-Web) blockiert werden. Deshalb muss der **gelieferte Startbefehl auch unter üblichen Windows-Standardrichtlinien ausführbar sein**. Für zuvor geprüfte, vertrauenswürdige Einmalskripte einen **separaten** PowerShell-7-Prozess mit `pwsh -NoProfile -ExecutionPolicy Bypass -File <geprüfter Pfad>` verwenden, falls nötig. Der Bypass gilt **nur für diesen Prozess**; die dauerhafte System-/Benutzerrichtlinie nicht verändern.
- Falls die Richtlinie durch `MachinePolicy` oder `UserPolicy` zentral erzwungen wird, **nicht versuchen, sie zu umgehen**; stattdessen mit konkreter Fehlermeldung abbrechen und einen zulässigen Ausführungsweg abstimmen. `Unblock-File` nur für zuvor verifizierte einzelne Downloads verwenden und nur, wenn dies die lokale Richtlinie erlaubt. Keine pauschale globale Freigabe, kein dauerhaftes `Set-ExecutionPolicy Unrestricted` oder `Bypass`.
- **Dateien vor Auslieferung auf Git-Formatierungsfehler prüfen:** Generierte Dateien müssen vor der Übergabe einen erfolgreichen `git diff --check` beziehungsweise nach dem Staging `git diff --cached --check` bestehen. Insbesondere keine nachgestellten Leerzeichen oder Tabs. Fehler bereits vor Erstellung des Downloadpakets beheben.
- **Vor Übergabe den Parser des gesamten Skripts prüfen.**
- Variablen vor Doppelpunkt korrekt begrenzen: `${File}:` statt `$File:`.
- Pfade, benötigte Programme/Dateien, Branch, erwarteten Commit, Repository- und Ziel-DB-Zustand vor Mutationen prüfen.
- Bei `git`, `npx`, `node`, `dotnet` und anderen externen Prozessen `$LASTEXITCODE` kontrollieren. Fehler verhindern Folgeaktionen.
- Quelltext nur gezielt ersetzen: erwartete Fundstellenzahl prüfen; bei Abweichungen abbrechen statt blind ersetzen.
- Neue Skript- und Quelltextdateien möglichst UTF-8 ohne BOM speichern; existierende Dateiformate respektieren.
- Skripte in Vorprüfung, Vorbereitung, Tests, Änderung, Nachkontrolle und Bericht/Bereinigung gliedern.
- Wiederholte Ausführung darf keine unkontrollierten Duplikate, Überschreibungen oder Löschungen verursachen.
- Maschinenlesbare Ergebnisse (vorzugsweise JSON) mit überprüfbaren Zuständen, Ergebnissen, Fehlern und offenen Punkten erzeugen; keine Geheimnisse protokollieren.

## 4. Lokale Testkonten und Geheimnisse

Die zwei Demo-Testpasswörter liegen lokal mit Windows-DPAPI verschlüsselt vor. Standardpfade:

```text
%LOCALAPPDATA%\KGV\TestSecrets\admin.secret
%LOCALAPPDATA%\KGV\TestSecrets\mitglied.secret
```

- Für lokale HTTP-E2E-Tests diese Dateien beim Ausführen der PowerShell-Skripte unter **demselben Windows-Benutzerkonto auf demselben PC** einlesen und mittels `ConvertTo-SecureString` entschlüsseln. **Nicht erneut im Chat nach den Testpasswörtern fragen.**
- Für den HTTP-Login benötigter Klartext nur kurzzeitig im lokalen Arbeitsspeicher; erhaltene JWTs ebenfalls kurzlebig behandeln.
- ChatGPT selbst kann die lokalen verschlüsselten Dateien nicht direkt auslesen. Entschlüsselung erfolgt beim lokalen Skriptlauf.
- Passwörter, JWTs, Service-Role-Keys, Access-Tokens und vollständige Auth-Antworten niemals im Chat, Git, Testbericht, Debug-Log oder Commit ausgeben.
- Fehlermeldungen vor Ausgabe auf mögliche Geheimnisse prüfen; keine Auth-Header protokollieren.
- Reguläre Anwendungstests mit Publishable-/Anon-Key und echten Benutzer-JWTs, **nicht** mit Service-Role-Key als Ersatz für Fachberechtigungen.
- DPAPI-Dateien nicht in Git oder Cloud-Synchronisierung aufnehmen. Diese Schutzmethode ist an Windows-Konto und PC gebunden und schützt nicht vor Schadsoftware im selben Benutzerkontext.

## 5. Teststufen konsequent unterscheiden

1. **Statische/Contract-Prüfung:** Quellcode, erwartete Aufrufe, Schnittstellen – kein Laufzeitnachweis.
2. **SQL-/RLS-/RPC-Prüfung:** Datenbankrechte und Rollenwirkung; nach Möglichkeit transaktional mit `BEGIN`/`ROLLBACK`.
3. **Unauthentifizierter HTTP-Smoke:** fehlendes oder ungültiges JWT wird korrekt abgelehnt, beispielsweise HTTP 401. **Allein kein E2E-Nachweis.**
4. **Authentifizierter negativer HTTP-Test:** echte Demo-Benutzer ohne Fachrecht, mit fremdem Objekt oder unzulässigem Zustand werden korrekt abgelehnt.
5. **Authentifizierter positiver HTTP-Test:** berechtigter Demo-Benutzer führt die **echte erfolgreiche Fachoperation** am bereitgestellten Endpunkt durch.
6. **Unabhängige Persistenzprüfung:** DB-/Storage-Zustand, Objektbezug, Revision und Metadaten prüfen; bei Dateien nach Möglichkeit Download samt SHA-256-Abgleich. Fehlerfälle, Retry und Idempotenz berücksichtigen.
7. **Echter UI-E2E-Test:** soweit für das Arbeitspaket erforderlich den Ablauf in Web/MAUI mit realer Benutzerinteraktion testen.

**Wichtig:** Ein 401-/403-Smoke, bestandener Contract-Test, Build oder Deployment ist **kein Ersatz** für einen erfolgreichen authentifizierten HTTP-E2E-Test. Unerwartete 5xx, Timeouts und ungewollte 403 im positiven Test sind Fehler.

## 6. HTTP-E2E-Testskripte – Mindeststandard

- Gegen die **tatsächlich bereitgestellte Demo-Umgebung** testen, nicht nur gegen lokale Quellen.
- Mindestens zwei reale Demo-Identitäten in getrennten Sessions verwenden. Aktuelle effektive Fachrechte und Objektscope prüfen, nicht veraltete Rollenbits oder historische Berechtigungsannahmen.
- Negative Varianten vor der berechtigten positiven Operation prüfen.
- Für jeden Aufruf Methode, Ziel, Soll-/Ist-Status und fachlich relevanten Response-Inhalt bewerten.
- Schreiboperationen zusätzlich **unabhängig** in der Datenbank nachprüfen; Erfolg nie nur am HTTP-Status festmachen.
- Bei Datei-, Signatur- und Dokumentoperationen Storage-Metadaten, authentifizierten Download, Dateityp und gegebenenfalls SHA-256-Bytevergleich prüfen.
- Ausschließlich synthetische, eindeutig gekennzeichnete Testobjekte verwenden. Vorher Zustand/IDs erfassen; in `finally` gezielt bereinigen und Ergebnis unabhängig kontrollieren. Keine echten Vereinsdaten löschen oder überschreiben.
- Tests mit notwendiger Bestandsänderung vorab absichern und die Wiederherstellung nachweisen.
- Keine Passwörter/JWTs in JSON-Berichten. Testname, Erwartung, Istwert, Status, Testobjekt-IDs und Bereinigungsstatus erfassen.
- Teilprüfungen ausdrücklich als **teilweise geprüft** kennzeichnen, wenn positiver Schreibaufruf, Speicherung oder nachgelagerter UI-Schritt fehlen.

## 7. Arbeitsabschluss, Git und Deployment

- Zusammenhängende Änderungen zunächst lokal/auf Demo prüfen und anschließend als nachvollziehbares Paket committen und pushen.
- Build-, Test- und CI-Ergebnisse dem **tatsächlich geprüften Commit** zuordnen.
- Nach Architekturwechseln Tests an den neuen Sollzustand anpassen, nicht die Anwendung an veraltete Testannahmen.
- Abhängig vom Umfang Web, MAUI, Core, Infrastructure, Supabase und gemeinsame Dienste prüfen.
- Anwendungscode nach erfolgreich überprüftem Test- und Commitstand bei Bedarf mittels AWR als **`Test / Zwischenstand`** bereitstellen. **Kein Produktionsrelease ohne Freigabe.**
- Abschlussprotokoll mit Branch/SHA, geänderten Dateien, Migrationen, DB-/Deploymentstand, tatsächlich ausgeführten Teststufen, HTTP-E2E-Nachweisen, Bereinigung und offenen Punkten.
- **Nur nachgewiesene Ergebnisse als erfolgreich melden.** Nicht ausgeführte Prüfungen und verbleibende Risiken benennen.

## 8. Pflicht für neue Chats und Startprompts

1. **Vor jeder Arbeit die aktuelle Datei auf `main` lesen:** `Documentation/KGV_TECHNISCHE_ARBEITSREGELN.md`.
2. Diese Regeln sind die **verbindliche Grundlage jedes neuen Startprompts**. Parallel den tatsächlichen Arbeitsbranch und Iststand überprüfen.
3. Technische Entscheidungen eigenständig und reproduzierbar umsetzen; bereits geklärte Fragen nicht wiederholen.
4. **Bei erkanntem Ergänzungs- oder Änderungsbedarf ausdrücklich fragen, ob die Regeldatei erweitert oder angepasst werden soll.** Keine Regeländerung ohne Zustimmung.
5. Freigaben für Produktivänderungen, Releases oder fachliche Entscheidungen bleiben notwendig.
