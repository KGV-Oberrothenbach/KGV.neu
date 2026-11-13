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
