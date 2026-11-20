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