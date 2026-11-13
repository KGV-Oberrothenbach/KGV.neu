# P11 – AP-03 Mitglieder-Arbeitsplan

## AP-03.1 – Mitgliederstammdaten und persönliche Datenänderungen

### Befund und Umsetzung

Die bisherige Mitgliederverwaltung verwendete für administrative RLS direkt
`admin`/`vorstand`. Eine individuelle Gewährung von `CanEditAllMembers` konnte
deshalb trotz UPDATE-Recht kein Zielmitglied sehen. Die Eigenansicht setzte
zudem auf einen generischen Tabellen-PATCH.

Migrationen `20261112120000_p11_ap03a_member_profile_self_service.sql` und
`20261112121000_p11_ap03a_member_effective_read_policy.sql` trennen diese
Wege: Verwaltungslesen und -schreiben verwenden `CanViewMembers` oder
`CanEditAllMembers`; Eigenlesen bleibt an die Auth-/Mitgliedsverknüpfung
gebunden. `update_own_member_contact` bindet an `auth.uid()` und aktualisiert
atomar ausschließlich Anschrift, PLZ, Ort sowie Telefon/Mobil. Status,
Mitgliedsnummer, Rollen, Rechte, Mitgliedschaft und Pachtbezüge bleiben aus
dem Eigenpfad ausgeschlossen. Der Infrastructure-Dienst fordert die RPC auf
und meldet ohne zurückgelieferten Datensatz keinen Erfolg.

Die E-Mail bleibt für verknüpfte Mitglieder bei Supabase Auth autoritativ:
der vorhandene OTP-/Bestätigungsweg ruft erst nach erfolgreicher Bestätigung
`sync_current_member_email()` auf. Fehlende oder fehlgeschlagene Auth-
Bestätigung ändert damit nicht unbemerkt die Mitgliedstabelle. Die Web-
Eigenansicht sperrt unzulässige Stammdaten und verweist für die E-Mail auf
diesen bestätigten Auth-Weg; MAUI verwendet ihn bereits über MyProfilePage.

### Nachweis

Der vollständige lokale Neuaufbau einschließlich Seed bestand mit 214
Migrationen. Die rückrollbare authentifizierte Fixture
`20261112120000_p11_ap03a_member_profile_self_service_funktionstest.sql`
bestand mit `ROLLBACK`: eigener Kontakt, Fremd-/Status-PATCH, individueller
Verwalter, administrativer Entzug und unveränderter Zustand nach Ablehnung.
Die aktive SELECT-/UPDATE-Policy wurde geprüft.

Web-SQL- und E-Mail-Auth-Verträge bestanden, ebenso der Web-Produktionsbuild.
Die .NET-Suite besteht mit 419/419; Infrastructure und MAUI-Android bauen.
Bestehende Nullable- und Web-Chunk-Warnungen sind unverändert. B-01 bleibt
als betrieblicher Edge-Bereitstellungsnachweis offen.

**Abnahme AP-03.1:** vollständig abgenommen.

## AP-03.2 – Mitgliedschaftsstatus und formelle Beendigung

### Befund und Umsetzung

`end_membership` verwendete bereits effektiv `CanEditAllMembers`, dokumentierte
aber weder einen verpflichtenden formellen Grund noch Akteur und Zeitpunkt in
einer revisionsfähigen Historie. Außerdem konnte ein Verwaltungsbenutzer das
Mitgliedschaftsende über den generischen Mitglieder-PATCH setzen. Migration
`20261113100000_p11_ap03b_formal_membership_termination.sql` führt deshalb
die append-only Tabelle `mitgliedschaftsbeendigung_audit` ein, verlangt einen
nichtleeren Grund, hält die bestehende Bearbeitungssperre ein und verweigert
eine Beendigung bei wirksamer Pachtbeteiligung. Der Trigger
`trg_guard_formal_membership_end` lässt Änderungen von `mitglied_ende` nur im
formellen Vorgang zu. Web und MAUI erfassen und übertragen den Grund; der
Aufruf bleibt an die effektive Capability gebunden.

Die Beendigung einer Pacht wird nicht als Mitgliedschaftsende behandelt; diese
Trennung wird durch die formelle RPC und die fehlende generische Enddatum-
Freigabe bewahrt. Die konkrete Umstellung auf den im Datenmodell vorgesehenen
passiven Status beim letzten Pachtende liegt weiterhin im Pachtwechselprozess
(AP-04); AP-03.2 führt dafür keine neue, parallele Statusautomatik ein.

### Nachweis

Der lokale Neuaufbau einschließlich Seed bestand mit 215 Migrationen. Die
rückrollbare `authenticated`-Fixture
`20261113100000_p11_ap03b_formal_membership_termination_funktionstest.sql`
bestätigt individuelle Gewährung, fehlendes Recht, administrativen Entzug,
formellen Grund/Akteur/Audit, Nebenmitglied-Beförderung und die Ablehnung eines
direkten Enddatum-PATCHes. Die A2.1-Mitgliedschaftsregression wurde für den
verpflichtenden Grund angepasst und bestand ebenfalls.

Die relevante Web-Berechtigungs- und Auth-Synchronisationssuite, R05/R10,
die A2.1-Regression, der Web-Produktionsbuild sowie die .NET-Suite (419/419)
bestanden. Core, Infrastructure und MAUI-Android bauen fehlerfrei.

**Abnahme AP-03.2:** vollständig abgenommen; die Umsetzung des operativen
Pachtwechsels einschließlich einer etwaigen Statusautomatik bleibt AP-04.

## AP-03.3 – Mitgliedsanträge und Aufnahmeentscheidung

### Befund und Umsetzung

Der bestehende Antragsprozess prüft serverseitig Finaldokument, Revision,
Pflichtsignaturen und den eingefrorenen Funktions-Snapshot; Stimmen sind
versioniert und identische Wiederholungen idempotent. Der Start der
Vorstandsbearbeitung verwendete bereits effektiv `CanCreateMitglied` (524288).
Die abschließende Aufnahme-/Ablehnungsentscheidung prüfte dagegen nur die
Vereinsfunktionsbesetzung. Damit konnte ein individuell entzogener
Entscheider trotz fehlender effektiver Fachberechtigung entscheiden.

Migration `20261113110000_p11_ap03c_membership_admission_effective_decision.sql`
verlangt deshalb für `kann_mitgliedsaufnahme_entscheiden()` und den direkten
Entscheidungs-RPC beide Bedingungen: aktuelle Funktionszuständigkeit und
effektives `CanCreateMitglied`. Sie erhält den Snapshot-, Nein-/Sitzungs-,
Idempotenz-, Dokumentprovenance- und Aufnahmebestätigungsprozess unverändert.

### Nachweis

Der vollständige lokale Neuaufbau einschließlich Seed bestand mit 216
Migrationen. Die rückrollbare `authenticated`-Fixture
`20261113110000_p11_ap03c_membership_admission_effective_decision_funktionstest.sql`
bestätigt die individuelle Gewährung und den Vorrang eines administrativen
Entzugs. Die Mitgliedsantrags-E2E-Suite bestand mit 6/6, die
Vorstandsworkflow-/Web-Verträge mit 10/10 und der neue Berechtigungsvertrag.
Die .NET-Suite besteht mit 419/419; Web-Produktionsbuild sowie Core,
Infrastructure und MAUI-Android bauen fehlerfrei. Die bekannten Nullable-
und Web-Chunk-Warnungen bestehen fort.

**Abnahme AP-03.3:** vollständig abgenommen. B-01 bleibt als betrieblicher
Deployment- und Projektzuordnungsnachweis offen.
