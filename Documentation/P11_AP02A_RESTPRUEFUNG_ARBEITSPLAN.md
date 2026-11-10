# P11 – AP-02A: Restprüfung von Rollen- und Berechtigungsentscheidungen

Stand: 10.11.2026. Geltung: A1-Inventur, keine Implementierung. Grundlage sind die aktive lokale PostgreSQL-Definition bis `20261109140000`, die P11-Fachregelmatrix und der Projektquellstand. WPF ist ausdrücklich nicht betrachtet. B-01 bleibt ein betrieblicher Freigabe-Blocker.

## Zusammenfassung

Die aktive Datenbank enthält 415 Funktionen und 114 RLS-Policies. Die reine Quellsuche lieferte 118 Dateien mit Rollenbegriffen; sie ist kein Fehlernachweis: technische Datenbankrollen, Rollenbasis-Masken, Eigentümer-/Statusprüfungen und historische Migrationsquellen sind hiervon getrennt zu bewerten. RPC 1–8 sind einzeln abgenommen und nicht erneut als offene Befunde geführt. Die offene Arbeit konzentriert sich auf fachliche SECURITY-DEFINER-Entscheidungen, Rollen-basierte RLS-Reste sowie UI/Edge-Aufrufketten.

## Befundliste und Arbeitsgruppen

| Gruppe | Objektbereich | Aktuelle Entscheidung / Befund | Erwartetes Fachrecht | Zusätzliche Schutzbedingungen | Risiko / Priorität | Status und Tests |
| --- | --- | --- | --- | --- | --- | --- |
| A2 | `update_app_user_access`, Benutzer-/Mitgliedschaftsverwaltung | `CanManageRoles`, Rollenbasis als Eingabe und Schutzkonto-Prüfung | `CanManageRoles` | `is_protected`, Zielkonto, bekannte Bitmaske, Audit | niedrig | korrekt; P11-T02-P/N/H, Entzug und Schutzkonto erneut regressieren |
| A2 | übrige Mitgliedschafts- und Auth-RPCs | Rollenbezüge in Aufnahme-, Status- und Eigentümerpfaden einzeln gegen aktive Definition prüfen | noch je Vorgang bestimmen | Authentifizierung, Eigenbezug, Status, Vertreter-/Antragsprovenance | hoch | offen; Positivgewährung, Entzug, Fremdmitglied, direkter RPC |
| A3 | Signatur-, Finaldokument-, Papierübergabe- und Dokumentarchivierungs-RPCs | verbleibende direkte Admin/Vorstand-Prüfungen sind fachlich einzeln zuzuordnen | noch bestimmen (`CanManageDocuments` möglich, nicht vorausgesetzt) | Dokumentprovenance, Revision, Signaturstand, Eigentümer, Archivierung | kritisch | offen; positive Gewährung, Entzug, fremdes Dokument, Replay/Revision, service-role |
| A3 | Dokument-RLS | effektive Lese-/Schreibrechte und Eigentümergrenzen bestehen bereits | `CanReadDocuments` / `CanManageDocuments` | Mitglied/Parzelle, Historie, Archivierung | mittel | teilweise korrekt; Policies gegen aktive Definition und RLS-Positiv/Negativ prüfen |
| A4 | Vertrags-RPC 1–8 einschließlich Erst-/Neufassung | effektives `CanCreateMitglied`, service-role-only, sichere Suchpfade | `CanCreateMitglied` | Akteurbindung, Vorgänger, Partei, Dokument, Revision, Sperren | niedrig | bereits abgenommen; bestehende SQL-Tests als Regression |
| A4 | Wartungsverträge, Vertragsnummern, historische Vertragsregistrierung | direkte Rollenentscheidungen und Service-Role-Parameter verbleiben | fachlich noch bestimmen | Nummern-Reservierung, Vertragspartner, Status, Provenance, Idempotenz | kritisch | offen; Parallelität, Entzug, falsche Partei/Dokument, Nummernkonflikt |
| A5 | Zähler, Arbeitsdienst, Vereinsfunktionen | Zählerpfad ist auf effektive Rechte umgestellt; übrige Rollenreste gruppenweise prüfen | jeweilige benannte Rechte | Foto, Zeit, Saison, Besitz-/Objektbezug | mittel | Zähler korrekt regressiert; Arbeitsdienst/Vereinsfunktionen offen |
| A6 | Edge Functions und Web/MAUI/Core/Infrastructure | UI darf Fachrecht nur darstellen; Edge darf Akteur/URL/Service-Key nicht aus Request übernehmen | jeweiliges Serverrecht | ClubContext, Auth, serverseitige Revalidierung | hoch | teilweise korrekt; Transport-/Kontextverträge vorhanden, alle privilegierten Edge-Ketten offen prüfen |

## Kritische Risiken

1. Ein einzelnes Fachrecht darf in A3/A4 nicht Dokumentprovenance, Revision, Signatur-, Status-, Parteien- oder Service-Role-Schutz ersetzen.
2. Service-Role-RPCs mit Akteurs-ID benötigen weiterhin eine vertrauenswürdige serverseitige Kette und eine effektive Rechteprüfung des übergebenen Akteurs.
3. Rollenbezeichnungen in Clients sind kein Autorisierungsnachweis; alle schreibenden Pfade müssen serverseitig effektiv geprüft werden.
4. Keine Demo-Marker oder Scope-Sonderlogik wieder einführen; Projekttrennung ändert Eigentümer- und Objektprüfungen nicht.

## Verbindliche Reihenfolge und Abnahme

1. **A2:** Mitgliedschaft/Benutzerverwaltung; jede offene RPC erhält Positivgewährung, Entzug, Fremdakteur und Direktaufruf-Test.
2. **A3:** Dokument, Signatur und Papierübergabe; zuerst service-only und finale Provenance-Ketten.
3. **A4:** Wartungsverträge, Nummern und historische Registrierung; inklusive Parallelitäts- und Idempotenztests.
4. **A5:** Zähler-, Arbeitsdienst- und Vereinsfunktionen; fachrecht- und objektspezifische RLS/RPC-Tests.
5. **A6:** Edge/Web/MAUI/Core/Infrastructure; Quellenverträge, keine rollenbasierte UI-Freigabe ohne Serververtrag.
6. **A7:** gesamte aktive Migration-/Kataloginventur, alle Teiltests, Web-/MAUI-/Core-/Infrastructure-Builds und bekannte fünf .NET-Altfehler. AP-02A erst dann schließen; die zwei Rollenmasken-Altfehler sind explizit zu bewerten.

Abnahmekriterium je Gruppe: aktive Definitionen statt historische Migrationen prüfen, fachlich begründetes effektives Recht dokumentieren, Gewährung und Entzug nachweisen, `anon`/`authenticated`/service-role-Grants prüfen, Objekt-/Status-/Provenance-Schutz negativ testen und Rollback/Parallelität belegen. Unklare Rechtezuordnungen bleiben offen und werden nicht geraten.

## Dokumentationspflege für A7

`P11_AP02A_EFFEKTIVE_RECHTE.md` enthält historische Testnachweise mit früheren Demo-Scope-Formulierungen. Diese werden für A7 auf Gültigkeit gegen D0–D6 geprüft und bei Bedarf als historischer Kontext, nicht durch nachträgliche Verfälschung des Nachweises, gekennzeichnet.
