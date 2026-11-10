# P11 – AP-02A: Restprüfung von Rollen- und Berechtigungsentscheidungen

Stand: 10.11.2026. Geltung: A1-Inventur und A2.1-Teilprüfung. Grundlage sind die aktive lokale PostgreSQL-Definition bis D6 (`20261109140000`), die P11-Fachregelmatrix und der Projektquellstand. WPF ist ausdrücklich nicht betrachtet. B-01 bleibt ein betrieblicher Freigabe-Blocker.

## Zusammenfassung

Die aktive Datenbank enthält 415 Funktionen und 114 RLS-Policies. Die reine Quellsuche lieferte 118 Dateien mit Rollenbegriffen; sie ist kein Fehlernachweis: technische Datenbankrollen, Rollenbasis-Masken, Eigentümer-/Statusprüfungen und historische Migrationsquellen sind hiervon getrennt zu bewerten. RPC 1–8 sind einzeln abgenommen und nicht erneut als offene Befunde geführt. Die offene Arbeit konzentriert sich auf fachliche SECURITY-DEFINER-Entscheidungen, Rollen-basierte RLS-Reste sowie UI/Edge-Aufrufketten.

## Befundliste und Arbeitsgruppen

| Gruppe | Objektbereich | Aktuelle Entscheidung / Befund | Erwartetes Fachrecht | Zusätzliche Schutzbedingungen | Risiko / Priorität | Status und Tests |
| --- | --- | --- | --- | --- | --- | --- |
| A2 | `update_app_user_access`, Benutzer-/Mitgliedschaftsverwaltung | `CanManageRoles`, Rollenbasis als Eingabe und Schutzkonto-Prüfung | `CanManageRoles` | `is_protected`, Zielkonto, bekannte Bitmaske, Audit | niedrig | korrekt; P11-T02-P/N/H, Entzug und Schutzkonto erneut regressieren |
| A2 | übrige Mitgliedschafts- und Auth-RPCs | A2.1 abgeschlossen: `end_membership` nutzt `CanEditAllMembers`; `find_auth_user_id_by_email` nutzt `CanManageRoles`; Auth-/Kontexthelfer, Vorstandsabstimmung und Linkverwaltung einzeln bewertet | `CanEditAllMembers` für Mitgliedschaftsende; `CanManageRoles` für Auth-ID-Lookup und Linkverwaltung; sonst technischer/eigener bzw. funktionsbezogener Vertrag | Authentifizierung, Eigenbezug, Status, Vertreter-/Antragsprovenance, Lock | niedrig | lokale Positiv-/Negativ-/Entzug-/Fremdlock-Tests rückrollbar bestanden; offene Aufnahme-/Vorstandsentscheidungen sind kein allgemeines Rollenfachrecht |
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

## A2.1 – Mitgliedschafts- und Auth-RPCs (abgeschlossen)

Die aktive lokale PostgreSQL-Definition nach D6 wurde inventarisiert. `update_app_user_access(uuid,text,bigint,bigint)` und `unlink_app_user(uuid)` sind korrekt mit effektivem `CanManageRoles` (`512`) geschützt; `unlink_app_user` sperrt zusätzlich `is_protected`. `provision_app_user_link(bigint,uuid,text)` ist `SECURITY DEFINER` mit festem `search_path` und ausschließlich für `service_role` ausführbar. Seine einzige privilegierte Edge-Aufrufkette (`kgv-invite-user`) validiert zunächst den Bearer-Token über `supabaseUser.auth.getUser`, prüft mit diesem Token `has_effective_permission(512)` und ruft erst danach die service-role-only RPC auf. Damit wurde keine clientseitig erreichbare Provisionierungsumgehung festgestellt.

Die Kontext-/Eigenhelfer `current_app_user_context`, `current_mitglied_id`, `current_app_role`, `current_user_email` und `get_user_role` geben ausschließlich den authentifizierten Eigenkontext zurück; sie sind keine Verwaltungsfreigaben. `before_user_created_allowlist` und `sync_app_user_from_mitglied` sind technische Hook-/Triggerfunktionen, nicht direkt fachliche Cliententscheidungen. Die Mitgliedsaufnahmefunktionen `mitgliedsaufnahme_berechtigungen`, `kann_mitgliedsaufnahme_entscheiden`, `darf_mitgliedsaufnahme_lesen`, `stimme_mitgliedsaufnahme_ab` und `entscheide_mitgliedsaufnahme` binden an gültige Vereinsfunktionsbesetzungen, Vertretungen oder eigene Snapshot-Stimmrechte; sie erhalten daher keinen pauschalen Rollenrechteersatz.

Zwei tatsächliche R01-Verstöße wurden korrigiert: `end_membership(bigint,text)` ignorierte individuelle Gewährungen und Entzüge durch `is_productive_admin_or_vorstand()` und verwendet nun effektives `CanEditAllMembers` (`4`), bei unveränderten Lock-, Haupt-/Nebenmitglied- und Statusprüfungen. `find_auth_user_id_by_email(text)` ignorierte für direkte Clientaufrufe ebenfalls Gewährungen und Entzüge; es verlangt nun effektives `CanManageRoles` (`512`), während `service_role` als technischer Aufrufer erhalten bleibt. Die Migration `20261110120000_p11_ap02a_a21_membership_auth.sql` wendet beide Änderungen gegen die jeweils aktive Funktionsdefinition an und prüft das Ergebnis.

Die echten, synthetischen und rückrollbaren PostgreSQL-Tests `20261110120000_p11_ap02a_a21_end_membership_funktionstest.sql` und `20261110120100_p11_ap02a_a21_find_auth_user_funktionstest.sql` bestanden nach vollständigem lokalem Neuaufbau. Sie belegen jeweils Positivgewährung, fehlendes Recht, administrativen Entzug und den erhaltenen Objekt-/Technikschutz (beim Mitgliedschaftsende zusätzlich Fremdlock; beim Auth-Lookup zusätzlich service-role). WPF blieb ausgeschlossen.

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
