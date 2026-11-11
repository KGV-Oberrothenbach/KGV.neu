# P11 – AP-02A: Restprüfung von Rollen- und Berechtigungsentscheidungen

Stand: 10.11.2026. Geltung: A1-Inventur und A2.1-Teilprüfung. Grundlage sind die aktive lokale PostgreSQL-Definition bis D6 (`20261109140000`), die P11-Fachregelmatrix und der Projektquellstand. WPF ist ausdrücklich nicht betrachtet. B-01 bleibt ein betrieblicher Freigabe-Blocker.

## Zusammenfassung

Die aktive Datenbank enthält 415 Funktionen und 114 RLS-Policies. Die reine Quellsuche lieferte 118 Dateien mit Rollenbegriffen; sie ist kein Fehlernachweis: technische Datenbankrollen, Rollenbasis-Masken, Eigentümer-/Statusprüfungen und historische Migrationsquellen sind hiervon getrennt zu bewerten. RPC 1–8 sind einzeln abgenommen und nicht erneut als offene Befunde geführt. Die offene Arbeit konzentriert sich auf fachliche SECURITY-DEFINER-Entscheidungen, Rollen-basierte RLS-Reste sowie UI/Edge-Aufrufketten.

## Befundliste und Arbeitsgruppen

| Gruppe | Objektbereich | Aktuelle Entscheidung / Befund | Erwartetes Fachrecht | Zusätzliche Schutzbedingungen | Risiko / Priorität | Status und Tests |
| --- | --- | --- | --- | --- | --- | --- |
| A2 | `update_app_user_access`, Benutzer-/Mitgliedschaftsverwaltung | abgeschlossen: `CanManageRoles`, Rollenbasis als Eingabe und Schutzkonto-Prüfung | `CanManageRoles` | `is_protected`, Zielkonto, bekannte Bitmaske, Audit | niedrig | korrekt; P11-T02-P/N/H, Entzug und Schutzkonto regressiert |
| A2 | übrige Mitgliedschafts- und Auth-RPCs | A2.1 abgeschlossen: `end_membership` nutzt `CanEditAllMembers`; `find_auth_user_id_by_email` nutzt `CanManageRoles`; Auth-/Kontexthelfer, Vorstandsabstimmung und Linkverwaltung einzeln bewertet | `CanEditAllMembers` für Mitgliedschaftsende; `CanManageRoles` für Auth-ID-Lookup und Linkverwaltung; sonst technischer/eigener bzw. funktionsbezogener Vertrag | Authentifizierung, Eigenbezug, Status, Vertreter-/Antragsprovenance, Lock | niedrig | lokale Positiv-/Negativ-/Entzug-/Fremdlock-Tests rückrollbar bestanden; offene Aufnahme-/Vorstandsentscheidungen sind kein allgemeines Rollenfachrecht |
| A3 | Signatur-, Finaldokument-, Papierübergabe- und Dokumentarchivierungs-RPCs | A3.1: `start_prepared_contract_signature` von Rollenbasis auf effektives Vertragsrecht korrigiert | `CanCreateMitglied` | Akteursbindung, Zuweisung, Revision, Signatur-Snapshot, Status | hoch | A3.1 abgenommen; übrige A3-Ketten offen |
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

## A3.1 – Dokument- und Signaturberechtigungen (abgeschlossen)

A2 ist vollständig abgeschlossen; die aktive A2.1-Inventur ergab keine Restbefunde. Die aktive A3-Inventur umfasst insbesondere `start_prepared_contract_signature`, `speichere_prepared_contract_signatur`, `prepared_contract_signaturen_vollstaendig`, `complete_prepared_mitgliedsantrag`, `complete_prepared_pachtvertrag_vor_b3`, `complete_aufnahmebestaetigung`, `dokumentiere_prepared_contract_papieruebergabe`, `prepare_contract_dispatch` und `create_contract_redispatch` sowie die zugehörigen Snapshot-, Provenance- und Guard-Funktionen.

Als erste kritische Kette wurde `start_prepared_contract_signature(bigint,integer,uuid)` bearbeitet. Die `SECURITY DEFINER`-RPC war direkt für `authenticated` erreichbar, band Akteur und Zuweisung bereits korrekt und sperrte Status/Revision/Snapshot, verwendete jedoch noch `is_admin_or_vorstand()`. Migration `20261110130000_p11_ap02a_a31_signature_start.sql` verlangt jetzt effektives `CanCreateMitglied` (`524288`). Der Test `20261110130000_p11_ap02a_a31_signature_start_funktionstest.sql` belegt individuelle Gewährung, fehlendes Recht, administrativen Entzug, manipulierte Akteurs-ID, veraltete Revision, Snapshot-Erzeugung und unveränderten Zustand nach Fehlschlägen; alle Daten werden zurückgerollt. Finaldokument-Provenance und service-role-only Abschlüsse bleiben in den bereits abgenommenen Vertrags-RPCs geschützt.

Bereits korrekt bewertet: `complete_aufnahmebestaetigung` prüft Vorgangstyp, Status, Mitgliedsbezug und finale Dokumentprovenance; die Edge-Function `kgv-generate-contract` ruft sie nur über ihren serverseitigen Admin-Client auf. `provision_app_user_link` und die A2-Service-Kette bleiben unverändert. Offen für die folgenden A3-Teilpakete bleiben die unabhängigen Signaturspeicher-, Papierübergabe-, Versand-, Vereinsdokument- und Archivierungsketten; sie wurden nicht vorgezogen. WPF blieb ausgeschlossen.

## A3.2 – Signaturspeicherung (abgeschlossen)

`speichere_prepared_contract_signatur` akzeptiert nun keine bloße Verwaltungsrolle mehr: der angemeldete Auth-User muss über `app_user` dem übergebenen Mitglied entsprechen. Der bestehende Signaturpositions-, Status-, Revisions-, Storage-Pfad-, Hash-, Snapshot- und Unveränderlichkeitsschutz bleibt erhalten. Die Upload-Edge-Function validiert JWT und zugeordnetes Mitglied; Vereinsunterzeichner werden gegen den eingefrorenen Snapshot, private Unterzeichner gegen die Positionszuordnung geprüft. Der rückrollbare Test `20261111100000_p11_ap02a_a32_signature_storage_funktionstest.sql` bestätigt erfolgreiche eigene Signatur, Idempotenz, Fremdmitglied-/Akteurablehnung, Revisionsschutz und Vollständigkeit. Papierübergabe, Versand, Vereinsdokumente und Archivierung bleiben offen.

## A3.3 – Papierübergabe vorbereiteter Verträge (abgeschlossen)

`dokumentiere_prepared_contract_papieruebergabe` verlangt nun effektives `CanCreateMitglied`, bindet den Akteur an `zugewiesen_an` und akzeptiert nur eine zum tatsächlichen Übergabezeitpunkt gültige Vereinsdokumentfassung. Zusätzlich wurde die erstmalige Aufnahme eines Papiernachweises in ein leeres Nachweisarray korrigiert; Wiederholungen ersetzen nur den jeweiligen Dokumenttyp. Der rückrollbare Test `20261111110000_p11_ap02a_a33_paper_handover_funktionstest.sql` belegt individuelle Gewährung, fehlendes Recht, administrativen Entzug, Akteursbindung und atomare Speicherung. Nach vollständigem lokalen Neuaufbau am 11.11.2026 bestanden A3.1–A3.3, die Vertrags-RPCs 1–8 sowie die relevanten Supabase-, Web- und Edge-Regressionen. Papierübergabe dokumentiert ausschließlich die Unterlagenbereitstellung vor der Signatur und ersetzt keine Schriftform oder Kündigung.

## A3.4 – Vertragsversand und erneuter Versand (abgeschlossen)

`prepare_contract_dispatch(bigint,bigint)` und `create_contract_redispatch(bigint)` waren `SECURITY DEFINER`-RPCs mit festem `search_path` und korrekten `authenticated`-/`service_role`-Grants, nutzten für die Fachfreigabe aber nur die Funktionsbesetzung `vertragsversand`. Dadurch galten individuelle Gewährungen und Entzüge von `CanManageCommunicationWorkflows` (`1073741824`) nicht. Migration `20261111120000_p11_ap02a_a34_contract_dispatch.sql` verwendet nun dieses effektive Fachrecht, bindet interaktive Vorbereitung und Claim an `zugewiesen_an` und erhält den vertrauenswürdigen `service_role`-Pfad. Sie behebt außerdem den bestätigten Laufzeitfehler des Mailtext-Snapshots bei fehlender optionaler Tabelle `vereinskonfiguration` durch eine dynamische, sichere Fallback-Auflösung. Die Edge-Function `kgv-send-contract-documents` validiert den Caller-JWT explizit vor dem callergebundenen `claim_contract_dispatch`; erst danach liest und schreibt der Service-Client ausschließlich den beanspruchten Snapshot. Der rückrollbare Test `20261111120000_p11_ap02a_a34_contract_dispatch_funktionstest.sql` belegt Erstversand, Redispatch, individuelle Gewährung, Entzug, Fremdakteur, falsche Provenance, Status, Revision, manipulierte Redispatch-ID, Idempotenz, ACL, Historie und atomare Ablehnung. Keine E-Mails werden dabei versandt.

## A3.5 – Vereinsdokumente und Freigaben (abgeschlossen)

Die aktiven RPCs für Fassungen, Papier-/Digitalbereitstellungen, Status, Historie und Dokumenttypen sowie ihre RLS-Policies und Grants wurden geprüft. Die direkten Admin-/Vorstandsprüfungen in den schreibenden Vereinsdokument-RPCs und der digitalen Versand-Edge-Kette ignorierten individuelle Gewährungen und Entzüge. Migration `20261111130000_p11_ap02a_a35_club_documents.sql` verwendet für Fassungen und Bereitstellungen `CanManageClubDocuments` (`268435456`), für Dokumenttypen getrennt `CanManageDocumentTypes` (`134217728`) und ersetzt die `dokumenttyp`-Schreibpolicy entsprechend. Der service-role-only digitale Pfad bindet weiterhin den übergebenen Auslöser an `app_user`/Mitglied und prüft dessen effektives Recht; die Edge-Function validiert JWT und dieses Recht vor Service-Role-Zugriff. Fassungshistorie, unveränderliche Dateimetadaten, Storno-/Historienguard, Gültigkeit und bestehende RLS-Lesegrenzen bleiben erhalten. Der rückrollbare Test `20261111130000_p11_ap02a_a35_club_documents_funktionstest.sql` belegt Gewährung, fehlendes Recht, Admin-Entzug, Fassung, Papierbereitstellung und den getrennten Dokumenttyp-Pfad. Voll-Replay mit 202 Migrationen, relevante Dokument-/Vertragsregressionen und Web-Build bestanden.

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
