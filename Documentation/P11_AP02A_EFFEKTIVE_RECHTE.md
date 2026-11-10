# P11 – AP-02A: Effektive Rollen- und Rechteverwaltung

Stand: 08.11.2026
Referenzen: P11-R01, P11-R02; P11-T01-P/N/H und P11-T02-P/N/H.

## Umsetzung

Die bestehende Formel `(Rollenbasis | individuelle Gewährungen) & ~individuelle Entzüge` ist in Core, Web und der neuen konsolidierenden Sicherheitsmigration der einzige maßgebliche Rechtevertrag. Die Rollen `admin`, `vorstand` und `user` liefern nur die jeweilige Ausgangsmaske.

Ergänzt wurden benannte Fachrechte für Export, Vereins-/Saisonkonfiguration, Dokumenttypen, Vereinsunterlagen, Vereinsfunktionen, Kommunikationsvorgänge und Dokumentarchivierung. Web und MAUI leiten die betroffenen Navigationen nun über diese effektiven Rechte ab. Die vorhandene Rollen-/Rechteverwaltung bleibt durch `CanManageRoles` geschützt.

Die Migration `20261108120000_p11_ap02a_effective_permissions.sql` setzt die zentrale SQL-Maske auf alle bekannten Rechte fort, schützt `update_app_user_access` weiterhin serverseitig mit `CanManageRoles` und erhält unbekannte zukünftige Bits bei Rollenwechseln. Individuelle Gewährungen und Entzüge bleiben dadurch unverändert; ein Entzug hat auch gegenüber der Rollenbasis Vorrang.

Der Schutz eines geschützten Kontos verlangt ausschließlich das verbleibende effektive Recht `CanManageRoles`. Er verlangt weder die Rolle `admin` noch zusätzliche Fachrechte und erzeugt daher keine solche Berechtigung.

## Ersetzte Entscheidungen

- Web-Capabilities für Export, Archivierung, Vereins-/Saisonkonfiguration, Zähler-Einreichungskonfiguration und Serienbrief nutzen effektive Rechte statt Rollennamen.
- MAUI-Verwaltungsnavigation für Kommunikation, Export, Vereinsunterlagen, Vereinsfunktionen und historische Verträge nutzt `PermissionChecks`.
- Der serverseitige Zählerlese-/Schreibpfad ersetzt die alten Admin-/Vorstands-RLS-Policies durch effektive Zählerrechte. Damit darf ein `user` mit Gewährung schreiben; ein `admin` mit Entzug wird auch über direkten Datenbankzugriff abgewiesen.

## Tests und aktueller Stand

- Vollständiger lokaler Supabase-Neuaufbau einschließlich Seed erfolgreich.
- P11-T01/P11-T02 sind als Vertragstest `KGV.Web/security/p11-ap02a-effective-permissions.contract.test.mjs` automatisiert: Formel, serverseitiger Zählerpfad, Rollenwechselerhalt, `CanManageRoles` und Sperrschutz.
- Web-Tests für Permissions, Capabilities, SQL-Permissions, MAUI-Zugriff und Navigation erfolgreich.
- Web-Build und MAUI-Build erfolgreich; bestehende Paket- und Nullbarkeitswarnungen bleiben unverändert.

## Offene Punkte vor AP-02A-Abnahme

AP-02B übernimmt Datenschutz-, Dokument-, Foto- und Demo-Scope-Härtung. Vor der Abnahme von AP-02A müssen jedoch die noch vorhandenen direkten Rollenprüfungen in historischen Vertrags-/Dokument-RPCs und RLS-Policies fachlich einzeln auf die hier ergänzten effektiven Rechte umgestellt werden. Diese Umstellung darf nicht pauschal erfolgen, weil die jeweiligen Vorgänge unterschiedliche Fachrechte und Demo-Scopes benötigen.

Der ausschließlich lesende Remote-Migrationsabgleich zeigte erwartungsgemäß die drei bewusst lokalen AP-01-Replay-Foundation-Migrationen als nicht produktiv angewendet. Es wurde kein `db push --linked` ausgeführt; diese Abweichung ist vor einer produktiven Migration ausdrücklich erneut zu entscheiden.

## Vertrags- und Dokumentrechte: geprüfter Zwischenstand

Die lokalen Endzustands-Policies wurden erneut inventarisiert. Folgende Korrekturen sind in `20261108120000_p11_ap02a_effective_permissions.sql` enthalten und der lokale Voll-Replay akzeptiert sie:

| Objektgruppe | Bisher | Effektives Fachrecht | Korrektur |
| --- | --- | --- | --- |
| `dokument` | Admin-/Vorstands-Policy | `CanReadDocuments` / `CanManageDocuments` | Lese-, Schreib- und bestehender Eigentümer-/Demo-Scope getrennt abgebildet. |
| Pachtvertrag, Beteiligung, Vertragsfassung | Admin-/Vorstands-Select | `CanCreateMitglied` | Vertragsbearbeitung ist individuell gewährbar; Eigentümergrenzen bleiben außerhalb dieser Verwaltungs-Policy bestehen. |
| Vereinsdokumentbereitstellung/-fassung/-typ | Admin-/Vorstands-Policies | `CanManageClubDocuments` | Freigabe, Storno und Verwaltung nutzen das benannte Verwaltungsrecht. |
| Vorbereitete Signaturen und Snapshots | Admin-/Vorstands-Select | `CanManageDocuments` | Dokumentverwaltung steuert den lesenden Verwaltungszugriff. |

Noch offen sind SECURITY-DEFINER-RPCs für vorbereitete Mitglieds- und Pachtverträge, Wartungsverträge, Vertragsnummern, Signatur- und Papierübergabeprozesse sowie historische Dokumentregistrierung. Sie müssen jeweils mit ihrem konkreten Aufruf- und Demo-Scope einzeln ersetzt werden; sie sind deshalb nicht als abgeschlossen markiert. Es wurde kein Zwischencommit erstellt.

## Vertrags-RPC: Mitgliedsaufnahme-Vorstandsbearbeitung

**Teilbereich abgeschlossen:** `start_mitgliedsaufnahme_vorstandsbearbeitung(bigint)` ist als einzelne RPC auf effektive Rechte umgestellt und lokal gegen die Datenbank abgenommen. AP-02A insgesamt bleibt offen.

| RPC | Bisherige Prüfung | Effektives Fachrecht / neue Prüfung | Erhaltene bzw. ergänzte Schutzbedingungen | Testnachweis |
| --- | --- | --- | --- | --- |
| `start_mitgliedsaufnahme_vorstandsbearbeitung(bigint)` | `auth.uid()` und `is_admin_or_vorstand()` | `auth.uid()` und `has_effective_permission(524288)` (`CanCreateMitglied`) | `SECURITY DEFINER` mit `search_path = public, pg_temp`; direkter Clientaufruf weiterhin nur für `authenticated` (neben der technischen `service_role`); Vorgangstyp, Dokumenttyp, Status, Signaturrevision, Finaldokument-Provenance und Abstimmungs-Snapshot bleiben unverändert. Demo-/Reviewer-Konten werden zusätzlich auf einen Demo-Antragsteller begrenzt. | `supabase/tests/20261108120000_p11_ap02a_start_mitgliedsaufnahme_funktionstest.sql` erfolgreich gegen die lokale Datenbank: individuelle Gewährung positiv; fehlendes Fachrecht und administrativer Rollenbasis-Entzug negativ (`42501`); Demo-Konto gegen Echtdaten negativ (`42501`); ungültiger Vorgang weiterhin fachlich negativ (`23514`). |

Die Fachzuordnung zu `CanCreateMitglied` folgt dem ausgelösten Mitgliedsaufnahmeprozess. `CanManageDocuments` wird nicht benötigt: Die RPC verwaltet weder freie Dokumente noch ersetzt sie die finale Dokumentprüfung; sie prüft nur deren bereits gesicherte Provenance.

Der damalige Zwischenstand mit sieben offenen Vertrags-/Dokument-RPCs wird durch den folgenden, später abgenommenen zweiten RPC-Abschnitt fortgeschrieben.

## Vertrags-RPC: Abschluss vorbereiteter Mitgliedsanträge

**Teilbereich abgeschlossen:** `complete_prepared_mitgliedsantrag(bigint, integer, uuid)` ist als zweite einzelne RPC auf effektive Rechte umgestellt und lokal gegen die Datenbank abgenommen. AP-02A insgesamt bleibt offen.

| RPC | Bisherige Prüfung | Effektives Fachrecht / neue Prüfung | Erhaltene bzw. ergänzte Schutzbedingungen | Testnachweis |
| --- | --- | --- | --- | --- |
| `complete_prepared_mitgliedsantrag(bigint, integer, uuid)` | `service_role` sowie `p_created_by` mit Rolle `admin` oder `vorstand` | Weiterhin ausschließlich `service_role`; für den übergebenen, zugewiesenen Akteur wird `CanCreateMitglied` (`524288`) aus Rollenbasis, individuellen Gewährungen und Entzügen bestimmt. | `SECURITY DEFINER` mit `search_path = public, pg_temp`; keine Ausführung durch `authenticated`; Vorgangsart, Status, Revision, Signaturrevision, Zuweisung, Finaldokument-Provenance, Vertreter- und Abschlusslogik bleiben unverändert. Demo-Konten dürfen ausschließlich Demo-Anträge abschließen. | `supabase/tests/20261108120000_p11_ap02a_complete_mitgliedsantrag_funktionstest.sql` erfolgreich gegen die lokale Datenbank: individuelle Gewährung positiv; fehlendes Fachrecht und administrativer Rollenbasis-Entzug negativ (`42501`); fremder zugewiesener Akteur negativ (`40001`); Demo-Konto gegen Echtdaten negativ (`42501`); ungültiger Vorgang negativ (`23514`); direkter `authenticated`-RPC-Aufruf negativ (`42501`). |

Die Fachzuordnung zu `CanCreateMitglied` folgt dem eigentlichen Abschluss der Mitgliedsaufnahme. Sie prüft bewusst den an die vertrauenswürdige Serverkette gebundenen Parameter `p_created_by`, nicht die technische `service_role`. Die individuelle Entzugsmaske bleibt damit auch für Admins wirksam.

Nicht bearbeitet und weiterhin offen: sechs weitere identifizierte Vertrags-/Dokument-RPCs, insbesondere vorbereitete Pachtabschlüsse, Signaturen, Wartungsverträge, Vertragsnummern, Papierübergaben und historische Dokumentregistrierung.

## Vertrags-RPC: Abschluss vorbereiteter Pachtverträge

**Teilbereich abgeschlossen:** `complete_prepared_pachtvertrag_vor_b3(bigint, integer, uuid)` ist als dritte einzelne RPC auf effektive Rechte umgestellt und lokal gegen die Datenbank abgenommen. AP-02A insgesamt bleibt offen.

| RPC | Bisherige Prüfung | Effektives Fachrecht / neue Prüfung | Erhaltene bzw. ergänzte Schutzbedingungen | Testnachweis |
| --- | --- | --- | --- | --- |
| `complete_prepared_pachtvertrag_vor_b3(bigint, integer, uuid)` | `service_role` sowie `p_created_by` mit Rolle `admin` oder `vorstand` | Weiterhin ausschließlich `service_role`; der übergebene und dem Vorgang zugewiesene Akteur braucht `CanCreateMitglied` (`524288`) aus Rollenbasis, Gewährungen und Entzügen. | Sicherer `search_path`; Parameterpflicht; Vorgangsart, Revision, Zuweisung, Finaldokument und Provenance, Signaturstand, Pachtbeginn, Mitpächter, Sperren, Vertragsfassung und Abschlussupdate bleiben erhalten. Demo-Konten sind auf Demo-Mitglieder begrenzt. | `supabase/tests/20261108120000_p11_ap02a_complete_pachtvertrag_vor_b3_funktionstest.sql` erfolgreich: individuelle Gewährung schließt einen Erstvertrag ab; fehlendes Recht, Admin-Entzug, fremde Zuweisung und Demo-Echtdaten werden mit `42501` abgewiesen; ungültiger Vorgang mit `23514`; direkter `authenticated`-Aufruf mit `42501`. |

Die Zuordnung zu `CanCreateMitglied` ist fachlich korrekt, weil die RPC ein Pachtverhältnis einschließlich Parteien und Vertragsfassung registriert. `CanManageDocuments` genügt nicht; das Finaldokument wird lediglich als bereits geprüfte, revisionsgebundene Vertragsgrundlage verwendet.

**Zwingende technische Abhängigkeiten:** `register_pachtverhaeltnis_erstvertrag` und `register_pachtvertrags_neufassung` erhalten dieselbe effektive Akteurprüfung, weil der Abschluss sie intern unter `service_role` aufruft und eine individuelle Gewährung sonst dort erneut blockiert würde. Ihre vollständige bestehende Fachlogik bleibt erhalten. Im Erstvertragszweig wurde zusätzlich eine bestehende Mehrdeutigkeit der Rückgabespalten durch Tabellenalias behoben; ohne diese Qualifizierung scheiterte der echte Abschluss mit einer SQL-Mehrdeutigkeitsmeldung.

Nicht bearbeitet und weiterhin offen: fünf weitere identifizierte Vertrags-/Dokument-RPCs, insbesondere Signaturen, Wartungsverträge, Vertragsnummern, Papierübergaben und historische Dokumentregistrierung.

## Vertrags-RPC: Verwerfen vorbereiteter Verträge

**Teilbereich abgeschlossen:** `discard_prepared_contract(bigint, integer, uuid)` ist als vierte RPC auf effektive Rechte umgestellt und lokal abgenommen.

| RPC | Bisherige Prüfung | Effektive Fachrechte | Schutz und Test |
| --- | --- | --- | --- |
| `discard_prepared_contract` | `admin`/`vorstand` | `CanCreateMitglied` plus `CanManageDocuments` | Service-only, Zuweisung, Revision, Status, Demo-Scope und Vertragsart bleiben geschützt. Der erfolgreiche Test archiviert die zugehörigen Dokumente und setzt den Vorgang auf `verworfen`; fehlendes Recht, Entzug, Fremdzuweisung, Demo-Echtdaten und direkter Clientaufruf liefern `42501`, abgeschlossene Vorgänge `23514`. |

Der Finaldokumentverweis bleibt beim verworfenen Vorgang als revisionsfähiger Bezug auf das archivierte Dokument erhalten; das ist erforderlich, weil der bestehende Workflow-Trigger das nachträgliche Löschen eines Finaldokuments ausdrücklich sperrt.

## Vertrags-RPC: Ersatzpachtvertrag vorbereiten

**Teilbereich abgeschlossen:** `prepare_pachtvertrag_ersatz(bigint, uuid)` wurde lokal abgenommen. Sie verwendet `CanCreateMitglied` statt der direkten Admin-/Vorstandsprüfung; Akteursbindung, Sperre, Vorgängerprüfung und Idempotenz bleiben erhalten.

Der echte lokale SQL-Test `20261108120000_p11_ap02a_prepare_pachtvertrag_ersatz_funktionstest.sql` bestand nach Korrektur des reservierten Testbezeichners `foreign`: individuelle Gewährung und idempotenter Rückgabepfad erfolgreich; fehlendes Recht, Admin-Entzug und fremde Akteurszuweisung jeweils `42501`; unbekannter Vorgänger `P0002`. Der vollständige lokale Neuaufbau war erfolgreich. Anschließend bestanden RPC 1–4 sowie alle 87 Supabase-Regressionstests.

Die vollständige RPC-5/6-Kette ist zusätzlich mit `20261108120000_p11_ap02a_prepare_pachtvertrag_ersatz_neuanlage.sql` abgenommen: Ein individuell berechtigter `user` legte tatsächlich einen neuen vorbereiteten Ersatzvorgang an; Mitglied, Vorgängerfassung, Akteur und Status wurden nach Rollenrücksetzung direkt geprüft. Der erste `<NULL>`-Befund war eine RLS-beschränkte Testabfrage, nicht ein verlorener RPC-Rückgabewert. Der zweite Aufruf lieferte denselben Vorgang; exakt ein aktiver Ersatzvorgang blieb bestehen. Alle sechs RPC-Tests und 87/87 Supabase-Regressionen bestanden anschließend. RPC 5 und RPC 6 sind damit als Teilbereiche abgenommen; AP-02A insgesamt bleibt offen.

## Vertrags-RPC: Erstvertragsregistrierung

**Teilbereich abgeschlossen:** `register_pachtverhaeltnis_erstvertrag(bigint, bigint, date, bigint, date, date, uuid, bigint)` ist als RPC 7 gegen die aktive lokale PostgreSQL-Definition abgenommen. AP-02A insgesamt bleibt offen; B-01 bleibt offen.

| Aspekt | Nachweis |
| --- | --- |
| Effektives Fachrecht | Der Akteur benötigt `CanCreateMitglied` (`524288`) aus Rollenbasis, individuellen Gewährungen und Entzügen. Ein `user` mit ausschließlich individueller Gewährung registrierte erfolgreich einen Erstvertrag. Ein Benutzer ohne Recht und ein Admin mit individuellem Entzug wurden jeweils mit `42501` abgewiesen. |
| Akteur und Service-Role-Kette | Die RPC ist ausschließlich für `service_role` ausführbar; der übergebene Akteur muss mit `app_user` und Mitglied verknüpft sein. Ein fremder/ungültiger Akteur sowie ein direkter `authenticated`-Aufruf wurden mit `42501` abgewiesen. |
| Fach- und Datenintegrität | Der erfolgreiche Aufruf legte Pachtverhältnis, Erstvertragsfassung und Haupt-/Mitpächter-Beteiligungen mit richtiger Parzelle und Dokumentverknüpfung an. Abweichendes Wirksamkeitsdatum, falsche Parzelle bzw. Vertragspartei und ein zweiter Erstvertrag wurden abgewiesen (`23514` bzw. `23505`); fehlgeschlagene Aufrufe hinterließen keine Teilregistrierung. |
| Technischer Schutz | Die aktive Definition ist `SECURITY DEFINER` mit festem `search_path = public, pg_temp`; `anon` und `authenticated` besitzen kein `EXECUTE`, `service_role` besitzt es. Die Advisory-Sperre und die Prüfung auf ein aktives Pachtverhältnis verhindern Doppelregistrierung/Überlappung. |

Der rückrollbare echte PostgreSQL-Test `supabase/tests/20261110100000_p11_ap02a_register_pachtverhaeltnis_erstvertrag_funktionstest.sql` wurde am lokalen Schema bis Migration `20261109140000` erfolgreich ausgeführt. Alle Daten sind synthetisch; der Lauf endete mit `ROLLBACK`. Anschließend bestanden die sechs bestehenden AP-02A-RPC-1-bis-RPC-6-SQL-Tests. Die aktuelle Web-Sicherheits-/Supabase-Vertragssuite bestand mit 86/86, der P11-AP01-Vertragstest mit 5/5, der Pachtvertrags-E2E-Test mit 6/6 und der Web-Produktionsbuild. Keine Demo-Scope-Prüfung wurde wieder eingeführt.

## Vertrags-RPC: Neufassung registrieren

**Teilbereich abgeschlossen:** `register_pachtvertrags_neufassung(bigint, bigint, bigint, date, date, uuid)` ist als RPC 8 gegen die aktive lokale PostgreSQL-Definition abgenommen. Die acht einzeln bearbeiteten Vertrags-RPCs sind damit jeweils getestet; AP-02A insgesamt und B-01 bleiben offen.

Der rückrollbare Echt-Test `supabase/tests/20261110110000_p11_ap02a_register_pachtvertrags_neufassung_funktionstest.sql` wies eine neue `neufassung` mit korrektem Dokument, Datum, Vorgänger- und Nachfolgerbeziehung nach. Das bestehende Pachtverhältnis und dessen Hauptpächter-Beteiligung blieben unverändert. Eine individuelle `CanCreateMitglied`-Gewährung war erfolgreich; fehlendes Recht, administrativer Entzug und ungültiger Akteur wurden mit `42501` abgewiesen. Direkter `authenticated`-Aufruf wurde abgewiesen; unbekanntes Pachtverhältnis liefert `P0002`, ungültige Daten `23514` und die zweite Neufassung desselben Vorgängers `23505`. Fehlgeschlagene Aufrufe hinterließen keine Teilfassung. Der Test endete mit `ROLLBACK`; `SECURITY DEFINER`, fester `search_path` und service-role-exklusives `EXECUTE` wurden zuvor an der aktiven Definition bestätigt.

Danach bestanden alle RPC-1-bis-RPC-8-SQL-Funktionstests und der D4-Nummernreservierungstest erneut. Die Web-Sicherheits-/Supabase-Vertragssuite bestand mit 86/86, der P11-AP01-Vertragstest mit 5/5, der Pachtvertrags-E2E-Test mit 6/6 sowie Web-, Core-, Infrastructure- und MAUI-Builds. Bestehend bleiben ImageSharp-`NU1902`/`NU1903`-, Nullable- und veraltete-MAUI-API-Warnungen; keine neue Migration war erforderlich.
