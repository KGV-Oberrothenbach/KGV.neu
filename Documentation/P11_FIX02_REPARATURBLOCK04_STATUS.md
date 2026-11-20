# FIX-02 / Reparaturblock 04 – technische Bestands- und Korrekturdokumentation

**20.11.2026.** Ausgangsbranch: `fix/reparaturblock-03`, SHA `40145c1c8a8c07d6c87c7c0ba8989f73de9e7e62`. WPF ausgeschlossen.

## Unabhängig lesend verifiziert (vor Codeänderung)

- `main` `0dfe03486f1373f6328a33aca3979c5d469a89cf`; DemoDB und ProduktivDB je 250 Migrationen; FIX-02-Migration vorhanden.
- `kgv-upload-photo`: Demo V11, Produktiv V20, jeweils ACTIVE / `verify_jwt=true`; beide Deployments und Repo-Fassung waren byteidentisch.
- `zaehler_ablesung`: Demo 195 Ablesungen, 0 mit Drive-Foto; Produktiv 221 Ablesungen, 221 mit Drive-ID, 220 mit vollständigen Referenzen. Historische Ersteller-IDs null.
- `trg_guard_meter_reading_photo_provenance` ist aktiv in beiden Datenbanken und unterbindet normale Client-INSERTs/UPDATEs auf Fotoreferenzen; auch nachträgliche Veränderung schon verknüpfter Drive-IDs wird unterbunden.
- `can_upload_meter_reading_photo(bigint)` nutzt effektives Recht 32 oder die autorisierte, aktive Eigenablesung; Alt-Leserechte begründen allein kein Schreibrecht.
- Bestehende `capture_meter_reading`-RPC besitzt noch Foto-Parameter, ihre unautorisierte Übernahme ist jedoch durch den unabhängigen Foto-Provenance-Trigger geschützt. Live-Negativtest ausstehend.

## Korrektur dieses Branches

Die Edge-Function hat für Upload-Dateinamen `meterNumberRaw = ""` verwendet, obwohl `zaehler.zaehlernummer` verfügbar ist. Die Ablesung wird weiterhin ausschließlich über die gespeicherte `ablesung_id` bestimmt; die serverseitige Select-Projektion und Typdefinition werden um `zaehlernummer` ergänzt, und der generierte Dateiname enthält die DB-Zählernummer. Web/MAUI bleiben unverändert; kein neues Uploaddatum im Client.

Geänderte Dateien: `supabase/functions/kgv-upload-photo/index.ts`, `supabase/tests/p11-fix-wave01.contract.test.mjs` sowie diese Dokumentation. Gezielt ausgeführter Node-Vertragstest: im lokalen Reparaturskript erfolgreich (sonst kein Commit). Vollständige Regressionen, .NET-/Web-/MAUI-Builds, GitHub CI und echte HTTP-E2E sind **noch offen**, bis sie gesondert ausgeführt/nachgewiesen werden.

## Sicherheits- und Freigabegrenzen

Kein ProduktivDB-Eingriff, kein Produktiv-Edge-Deploy, kein Merge nach `main`. Kein Demo-Deploy im Rahmen des lokalen Quellcodepatches. Vor Demo-Deployment den aktuellen Backend-Stand erneut vergleichen. Testaccounts, -zähler und -fotos müssen synthetisch sein; das beigefügte Demo-HTTP-Skript erstellt ein markiertes Testbild und protokolliert genau die zu bereinigenden Artefakte. Nicht durchgeführte Fälle (fremd/historisch/entzogen/Offline/Retry) bleiben gesperrt, bis echte Positiv-/Negativ-HTTP- und UI-Nachweise vorliegen.

**Status: Reparaturcode vorbereitet; technische Demo-Abnahme ausstehend.**
## 20.11.2026 – Demo-Provenance-Fix nach echtem HTTP-Test

- Demo-HTTP-Negativfälle: ohne JWT 401; ungültiges JWT 401; fremdes Konto 910040 403; unbekannte Ablesung 403.
- Beim eigenen Upload auf 990000101 erreichte die Edge-Function nachweislich die Google-Drive-Ablage, scheiterte jedoch bei der Datenbank-Verknüpfung: PostgREST PATCH 403 / Trigger-Fehler 42501 / Edge HTTP 500.
- Ursache: Alter Trigger las nur `request.jwt.claim.role`. PostgREST-Server verwendet auch `request.jwt.claims`. Neue Schutzbedingung: **effektive DB-Rolle service_role UND JWT-Rolle service_role**; Doppelverknüpfung weiterhin gesperrt.
- Migration `20261120181935_p11_fix02_meter_photo_provenance_role_claim.sql` in DemoDB installiert (251 Migrationen), NICHT in ProduktivDB (250).
- Direkt in DemoDB per rückgerollter Transaktion getestet: effektiver service_role-Erstschreibzugriff zulässig; normaler authentifizierter Benutzer gesperrt; vorgetäuschter Service-JWT mit DB-Rolle authenticated gesperrt; bestehende Drive-Verknüpfung unveränderlich. Keine künstlichen Fotoreferenzen zurückgelassen.
- Demo-Edge `kgv-upload-photo` V13 blieb unverändert; beide Web-/MAUI-Clients unverändert. Produktiv-Edge V20 und `main` unverändert.
- Ausstehend: authentifizierten HTTP-Positivtest nach SQL-Korrektur wiederholen, Download/Hash, private Drive-Sichtbarkeit, vollständige mobile Offline-/Retry-UI-Prüfung. Eine finale Demo-Abnahme erst nach den tatsächlichen Nachweisen aussprechen.
- Google-Drive-Ziel: Serverlogs zeigten OAuth-, Ordner- und Testbild-Upload erfolgreich; der Cleanup-Versuch des fehlgeschlagenen Fotos ist nicht unabhängig als erfolgreich bestätigt. Ausschließlich synthetische Daten bereinigen.
