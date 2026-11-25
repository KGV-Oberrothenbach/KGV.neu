# FR2-12 v2 – Status

- main dd394206ee6d14cd648159a2e91d4693ffa66210, FR2-11 integriert.
- DemoDB 291 Migrationen, Versionen-MD5 c2348a0171c5a8228549ea7e1e0fc523; VDB 255, MD5 1e26e9b8a81757fee907547bf7d8f94f.
- 5 App-Konten; 1 ohne Hauptrolle, 1 mit inkonsistentem Mitgliedsbezug; keine effektiven Rechte bei diesen inkonsistenten Konten. 1 geschützter wirksamer Rechteverwalter, 14 Overrides, 0 offene Löschreservierungen, 55 Rechte (52 aktiv). Keine Änderung dieser Bestände.
- Alte Löschkompensation restaurierte lediglich Legacy-Rolle/-Bits; neue Snapshot-Kandidaten sichern alle drei V2-Zuordnungstabellen. Vor Migration muss eine transaktionale reale Funktionsprüfung mit unabhängiger Wiederherstellung erfolgen; keine Freigabe aufgrund statischer Tests.
- Invite verweigert privilegierte Rollen statt sie nur mit zusätzlichem Recht zuzulassen. Bestehende Konten nicht umrollen. Altclients mit Admin-/Vorstands-Invite werden deshalb abgewiesen; neue Rolle getrennt vergeben. Passwort-Reset bleibt öffentliche Erstlogin-Schnittstelle; Enumeration/Rate-Limit und Rechtepfade noch prüfen.
- Alte Rollenbits und Schnittstellen nicht pauschal entfernen. MAUI-Kontoliste und Navigation noch nicht vollständig getrennt. Globale Leserechte, Session und alte RLS/RPCs noch offen.
- Migrations- und Deploymentphase bewusst gesperrt, bis lokale und SQL-Prüfungen bestätigt sind. Keine Produktivänderung.

- Lesen erfordert das ausdrücklich gewährte Leserecht (auch wenn ein Verwaltungsrecht vorhanden ist); damit kann ein individueller Entzug nicht über ein Verwaltungsrecht umgangen werden. MAUI-Kontoliste verwendet die bestehende RLS. Fachrechte-V2-Ansichten in MAUI und die vollständige Legacy-Prüfung sind weiterhin offen.

## V5 – Sicherheitskorrekturen nach erfolgreichem V4

- V4: Supabase 165/165, Web Security 126/126, MAUI Contract 4/4, .NET 458/458, Web/Core/Infrastructure/MAUI Android Build PASS. Drei Deno- und 29 FR212-Tests aus V2/V3 belegt.
- SQL Teilprobe der Löschkompensation: positive Reservierung und exakte Wiederherstellung, 1+1+1 V2-Zuordnungen. PZ212 war absichtlicher Rollback. Vorher/nachher MD5 ae3edd867138287e7c65198853b99d95; keine Fixture-Konten verblieben; 291 Migrationen.
- Signatur-Replay prüft jetzt denselben Unterzeichner wie Upload; zusätzlicher Laufzeittest mit Mock-RPC und Storage-Zähler.
- MAUI: getrennte Menüeinstiege, serverseitig gelesene Schreibbefugnis, keine aktive Legacy-Übersicht. Alte nicht mehr aktive Methoden bleiben bis zur Abhängigkeitsprüfung erhalten.
- SQL: aktuelles Konto muss tatsächlich konsistent und nicht gesperrt/soft-deleted sein; E-Mail-Suche für Kontoverwaltung; fünf Systemrechte nicht direkt grantbar; geschützte Admin-Identität behält Lesen und Verwalten der Berechtigungen. Keine Änderung an Rollen oder Katalog.
- Offen: exakte vollständige Migration gegen Demo transaktional prüfen; Auth-Session-ID-/Refresh-Verhalten, OTP-Antworten und Rate-Limits, vollständige globale Leserechte/indirekte SQL-Abhängigkeiten, parallele Rechteänderungen und UI-Abnahme. Export-Diagnosewarnungen aus FR2-11 (nicht in V5 geändert).
- Keine Migration, kein Deployment, kein Commit oder Push in V5. Kein Produktionszugriff schreibend.

### Unabhängige V5-Teilprüfungen

- Signatur-Originalquelle Git-Blob a7c24aed002c756b009cb3c45f4b9df26dd8ac65 rekonstruiert und per git hash-object bestätigt. Neuer Mock-Verhaltenstest am Original 7/12, nach Korrektur 12/12. Kein echtes HTTP-E2E.
- Demo-SQL-Teilprobe: Benutzerverwaltung ohne Rechteverwaltung, Leserechte ohne Mutation, positive Kontosuche und Kontostatus, 55 Rechte in Vorschau; globales Leserecht als einziges wirksames Fachrecht, fremdes Mitglied lesbar, Dokument-Write abgewiesen. Sperrung verhindert Kontext und effektives globales Recht. Danach Testfixture wegen Unique-Constraint auf app_user.mitglied_id kontrolliert gescheitert (23505), alle Änderungen zurückgerollt.
- Zweite Probe PZ212/FR212_V5_PROVISION_PROBE_PASS: direkter Aufruf durch authenticated abgewiesen; service_role darf nur user einladen; Admin/Vorstand abgewiesen; Hauptrolle mitglied ohne Zusatzrechte/Overrides; erneuter Link abgewiesen; alle fünf System-Grants abgewiesen; geschütztes Admin-Konto kann berechtigungen.lesen/verwalten nicht entzogen bekommen; verwaister Kontext abgewiesen. Absichtlich zurückgerollt.
- Nach beiden Prüfungen unabhängig: MD5 ae3edd867138287e7c65198853b99d95, keine synthetischen Konten oder Mitglieder, 291 Migrationen, fr212_user_account nicht installiert.
- Keine direkte Verwendung von vereinsdaten.alle_lesen bzw. can_access_live_internal_data in nicht-SELECT-Policies (Public/Storage). Indirekte Abhängigkeiten und Session-ID-Widerruf noch nicht vollständig geprüft.
- Der neue SQL-Entwurf als Ganzes bleibt ungeprüft; die Teilproben sind keine Migrationsfreigabe.
