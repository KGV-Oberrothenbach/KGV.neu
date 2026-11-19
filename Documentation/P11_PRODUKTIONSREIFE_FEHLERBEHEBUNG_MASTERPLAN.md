# KGV-Software – verbindlicher Fehlerbehebungsplan bis zur Produktionsfreigabe

**Start:** 18.11.2026, Ausgangscommit `0dfe03486f1373f6328a33aca3979c5d469a89cf`  
**Geltung:** Web, MAUI, KGV.Core, KGV.Infrastructure, Supabase, gemeinsame Dienste. **WPF ausgeschlossen.**  
**Arbeitsmodus:** Keine erneute Generalprüfung. Bestehenden Audit als Mängelliste verwenden. Einen Blocker **korrigieren → eng testen → in Demo nachweisen → schließen**. Keine unbelegten Grünmeldungen. Erkenntnisse nur als zusätzliche, konkret reproduzierte Mängel in diesen Plan aufnehmen. Keine Neuinstallation für neue Vereine vor Go-live.

## Feste Reihenfolge

| ID | Arbeitspaket / Bezug | Status | Technische Behebung | Abnahmekriterium |
|---|---|---|---|---|
| FIX-01 | Benutzerlöschung; F2 | **Code vorbereitet; E2E offen** | `kgv-delete-user` versionieren; effektive `CanManageRoles` prüfen; geschützte/eigene/privilegierte Konten sperren; `unlink_app_user` unter Row-Lock härten; Auth-Fehler kompensieren bzw. fail-closed melden | Neue Migration in lokalem Replay; negative/positive Tests mit synthetischen Konten in Demo; Reviewer-Freigabe; kontrollierter Prod-Deploy mit separater Freigabe |
| FIX-02 | Ablesungsfotos; F3 | **Code vorbereitet; technische Abnahme offen** | Aktuellen Web-/MAUI-Vertrag `file + ablesung_id` mit berechtigungsgebundener Edge-Function implementieren; serverseitig Metadaten ableiten und Provenance speichern | Live-Demo-HTTP-Tests Upload/Download für eigene, fremde, historische Ablesungen, mit entzogenem Recht sowie fehlerhafter JWT; Prod-Deploy erst freigegeben |
| FIX-03 | DB-Drift; F4 | **Code vorbereitet; technische Abnahme offen** | `get_user_role`, `who_am_i`, Rollen-Trigger und `zaehler_ablesung.pruefstatus` migrationsgeführt an verbindliche Produktiv-Semantik angleichen | Gleiche fachliche Definitionen und Defaultwerte in Demo/Prod; Regressionen Rolle/Ablesefreigabe |
| FIX-04 | Edge-Drift; F5 | **Offen: 13 Quellcode-/Deployment-Abgleiche und Demo-HTTP-E2E ausstehend** | Alle 13 Namen/Slugs, `verify_jwt`, Code-SHAs, Abhängigkeiten/Versionen und Deployments mit `main` abgleichen; Demo `clever-task` korrigieren | Jede URL aufrufbar, dokumentiertes Soll-Ist-Manifest, negative HTTP-Auth-Tests, Versionen gegen freigegebenen Git-SHA nachgewiesen |
| FIX-05 | CI; F6 | **Code vorbereitet; technische Abnahme offen** | P11-Regressionen in allgemeine `main`-CI integrieren; .NET 8/10, Node/Web, SQL, MAUI, PGlite, Contracts, E2E | Vollständiger Workflow auf neuem Release-Commit grün; keine nur historischen Testergebnisse |
| FIX-06 | Vertragsketten, E-Mail | Offen | Mitgliedsantrag, Pachtvertrag, Wartungsvertrag, historische Verträge, Signatur-, Snapshot-, PDF- und Versandpfade je fachlichem Prüfprotokoll reparieren | Je Vertragsart synthetischer realer E2E; Signaturen, Dokumente, Berechtigungen, Idempotenz, Versand in kontrolliertem Kanal nachgewiesen |
| FIX-07 | Restliche Sicherheits-/Fachblocker | Offen | Fehlende RLS der internen Tabelle, 28 anon-RPCs (davon 21 Trigger separate) klassifizieren; weitere im Protokoll offene Berechtigungs-, Mitglieds-, Parzellen-, Zähler-, Arbeitszeit-, Finanz- und Exportfehler beheben | Alle als echte Sicherheits- oder Funktionsmängel bestätigten Fälle haben Fix+Regression, Warnungen ohne Exploit sauber abgegrenzt |
| FIX-08 | Wiederherstellung | Offen | Das bestehende VDB-Backup in völlig isolierter lokaler Umgebung vollständig wiederherstellen | Nachweis SQL-Restore, Datenintegrität, RLS/Schema, Lesefunktion; keine Verbindung zu Prod |
| FIX-09 | Go-live | Offen | Einmaliger finaler Regression-/Freigabedurchlauf und Release-Dokumentation | Alle kritischen Blocker geschlossen, aktueller SHA/Deploy identifiziert, positive Protokolle, explicit Go/No-Go |

## FIX-01 – Konkreter Arbeitsnachweis

- Ausgangsbefund: Produktion `kgv-delete-user` Version 24 nicht aktiv im Repository versioniert; `get_user_role()` Rollenprüfung anstelle von `CanManageRoles`; ungeschützte Lösch-/Unlink-Reihenfolge.
- Korrekturpaket: neue versionierte Function `index.ts`, separat testbarer Workflow `delete-account.mjs`, SQL-Vorlage zum Härten von `unlink_app_user(uuid)`, neues test file und Script zur lokalen Git-Übernahme.
- Offline-Test: **17/17 bestanden** am 18.11.2026; keine SQL-Replay-/HTTP-/Demo-Tests behauptet.
- Begründete Sicherheitsentscheidung: Zielkonten mit effektiver `CanManageRoles` werden von diesem Endpoint bewusst **nicht** gelöscht, selbst wenn `is_protected=false`. Dafür ist ggf. später ein separates Admin-Verfahren mit ausdrücklicher Schutzkontoentscheidung nötig.
- Offen: lokaler Merge/Push wegen fehlendem GitHub-Schreibzugriff (`403`); lokaler Supabase-Replay; End-to-End-Demo-Abnahme; kontrollierter Prod-Deploy nach Freigabe.

## Reparaturwelle 01 – Stand 19.11.2026 (noch nicht abgenommen)

- FIX-01: Commit `62d1e92` bleibt unveraendert. Die historischen 17 Offline-Tests sind kein neuer Abnahmenachweis. Lokales SQL-Replay, HTTP-E2E, Demo-Freigabe und Main-Integration bleiben offen.
- FIX-02: Neue schreibseitige Ablesungsfoto-RPC, unveraenderlicher Ersteller, fehlertolerantes MAUI-Save/Queue/Upload, serverseitige Metadaten, unabhaengige Lese-/Schreibberechtigung, Fix von `normal`/`jea` und Diagnoselog. Live-E2E mit synthetischen Personen fuer eigene/fremde/historische/entzogene Rechte offen.
- FIX-03: Demo-Drift in `get_user_role`, `who_am_i`, `sync_app_user_from_mitglied`, Trigger und Default `pruefstatus` durch versionierte Migration behoben (Code). `app_user` bleibt die Rollen-Vorlagen-Autoritaet. Rechte duerfen sich nur aus effektiven Permissions ergeben. Demo-/Prod-Migrationsabnahme offen.
- FIX-04 (noch offen): Alle 13 Edge-EntryPoints explizit in `supabase/config.toml`, SHA-Snapshot in Paket. Demo-Slug `clever-task` weiterhin **nicht automatisch angefasst**. Code-/Runtimevergleich und negative HTTP-Tests vor freigegebenem Demo-Deployment offen. Produktivdeploy nur separat freigeben.
- FIX-05: CI verwendet die vorhandene P11-Supabase-E2E als wiederverwendbaren Workflow, PGlite, .NET, MAUI und Web. Neue Exclusion schliesst bei `parzellen_belegung` die unverbundene Altbelegung-Luecke (vor Migration Konfliktpruefung). Die urspruengliche Fixture `overlapping occupancy accepted` muss im isolierten Replay grün werden. CI-Abnahme offen.
- Die groesseren Bestandsdateien liegen im Paket als eindeutig gepruefte Patches, damit keine fremden lokalen Aenderungen ueberschrieben werden. Das Windows-Skript materialisiert die vollstaendigen Dateien aus dem vorhandenen FIX-01-Checkout.
- ProduktivDB und DemoDB wurden bei der Paketvorbereitung **nur gelesen**. Kein DB-Push, kein Live-Deploy, keine Benutzerloeschung und kein Merge nach `main`.

## Verbindliche Sicherheitsregeln

1. Keine produktiven Datenänderungen, Migrationen, Benutzerlöschungen, Edge-Deployments oder echten Mailversände ohne explizite Freigabe nach Sicherheitsprüfung.
2. Git-Aktionen auf einem **eigenen Branch** ohne Y/N-Fragen; kein ungetesteter Merge nach `main`.
3. Vor jeder Demo-Migration/Deployment Vergleich des aktuellen Repository- und Migrationstandes, keine stillen Überschreibungen.
4. Kein erneutes Aufmachen erfolgreich abgenommener Punkte ohne konkret nachgewiesene Regression.
5. Fortlaufende Chat-Titel mit **echten Nummern**: `02 – KGV-Produktionsreifeprüfung – FIX-01 Abschluss und FIX-02`, dann `03 – ...` usw.
