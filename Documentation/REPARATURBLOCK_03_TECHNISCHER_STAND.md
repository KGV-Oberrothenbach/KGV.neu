# KGV-Software – Reparaturblock 03: Benutzerkontolöschung

**Status: in Bearbeitung, NICHT technisch abgenommen, KEINE Produktionsfreigabe.**

## Ausgangsbefund (19.11.2026)

- GitHub `fix/p11-reparaturwelle-02` HEAD `5989cd880eb049b1fda8449fa17a9cb14a35e65d`.
- Demo- und ProduktivDB mit je 249 Migrationen; beide zuvor fehlenden Migrationen wurden unabhängig bestätigt.
- Web rief `unlink_app_user(uuid)` direkt auf; Auth-Konto blieb bestehen.
- MAUI führte `mitglied.auth_user_id = NULL` und `app_user DELETE` direkt aus; Auth-Konto blieb ebenfalls bestehen.
- `unlink_app_user(uuid)` band die Löschung nicht an die erwartete Mitglieds-ID.
- Historische Dokumente und Verträge besitzen teils `auth.users`-Fremdschlüssel mit RESTRICT/NO ACTION. Die Auth-Löschung kann deswegen berechtigt scheitern und muss eine vollständige Wiederherstellung auslösen.

## Vorbereitete Korrekturen

1. Web-Service/UI verwendet `kgv-delete-user` mit `mitgliedId` und `expectedAuthUserId`. Vor dem destruktiven Aufruf verlangt der Client einen `dryRun` mit `contractVersion=2` und `outcome=eligible`; Erfolg nur nach `outcome=deleted`. Ein noch alter Edge-Deploymentstand kann damit nicht unbeabsichtigt löschen.
2. MAUI `AuthService.RemoveUserAsync` verwendet dieselbe zweistufige Edge-Vertragsprüfung; direkte Mitglied-/Appuser-Tabellenänderungen im Löschpfad entfallen.
3. Edge-Function verifiziert die JWT-Identität, prüft `CanManageRoles`, Eigenkonto, geschützte Konten und effektive Zielberechtigungen; veraltete Benutzerzuordnungen werden abgelehnt.
4. Neue versionsgeführte Migration erzeugt eine vorläufige, für Clients nicht lesbare Löschreservierung. Transaktionale Prüfung der Mitglieds-ID und Sperre gegen parallele Wiederverknüpfung; die bisherige Direkt-Unlink-RPC verliert `authenticated`-EXECUTE.
5. Auth-Fehler mit nachweislich existierendem Benutzer werden durch eine service-role-only SQL-Transaktion wiederhergestellt, einschließlich Rechte und Appuser-Zeitstempel. Unklare Ergebnisse bleiben ausdrücklich manuell prüfpflichtig und mit Sperrdatensatz gesichert.
6. 22 synthetische Node-Tests + neuer Web/MAUI-Quellvertrag; bestehender Appuser-Web-Vertrag angepasst.

## Lokaler Nachweis vor Git-Übernahme

- `node --test .../kgv-delete-user.p11-fix01.test.mjs`: **22/22 erfolgreich**, Mock-/Quelltests.
- `node --check .../delete-account.mjs`: bestanden.
- TypeScript-Einstiegspunkt: Node-Syntaxcheck mit `--experimental-strip-types`: bestanden.
- Kein echter PostgreSQL-Lauf, kein Live-HTTP-Test, kein .NET- oder MAUI-Build in dieser Umgebung.

## Kontrollierte Übertragung

`apply_reparaturblock03.ps1` prüft Branch, Commit, sauberes Arbeitsverzeichnis und 7 ursprüngliche Git-Blob-SHAs. Es erzeugt `fix/reparaturblock-03`, erstellt eine neue SQL-Migrationsdatei über Supabase CLI, überträgt die Änderungen, startet gezielte und breite lokale Tests/Builds und committet/pusht nur bei Erfolg. Kein Merge nach `main`, keine Datenbank- oder Edge-Deployments.

## Noch zwingend offene Abnahme

- Git-Übertragung und finaler neuer Git-SHA (HTTP 403 des GitHub-Connectors); CI des neuen SHA grün.
- Tatsächliches vollständiges SQL-Replay der neuen Migration in einer isolierten PostgreSQL-Testumgebung.
- **Gesondert freizugebende** DemoDB-Migration, danach Edge-Deployment der passenden Function. Der neue `contractVersion=2`-Endpunkt ist Voraussetzung für neue Clients; das alte RPC-Recht wird mit der Migration absichtlich entzogen.
- HTTP-Negativtests (ohne/ungültige JWT, keine Rolle, eigene/geschützte/privilegierte/fremde und stale Mitglieds-ID).
- Synthetischer Positivfall; Auth-Konto wirklich entfernt, Mitglied nicht gelöscht; Archiv und Verträge unverändert; parallele Wiederverknüpfung gesperrt.
- Auth-Fehler durch restriktiven Dokument-FK: Zustand und Rechte wiederhergestellt; Fehler eindeutig.
- Monitoring und manuelle Recovery des Sperrdatensatzes bei unbestimmten API-Ergebnissen.
- Unabhängige Prüfdokumentation des Demo-Deployments, der HTTP-Ergebnisse und der Versions-/ACL-Definitionen.
- Späterer Produktionsdeploy ausschließlich mit gesonderter Freigabe; keine produktive Kontolöschung in der Abnahme.

**Block 03 technisch abgeschlossen: NEIN.**
