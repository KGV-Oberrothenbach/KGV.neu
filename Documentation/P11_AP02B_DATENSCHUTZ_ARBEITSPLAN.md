# P11 – AP-02B Datenschutz-Arbeitsplan

## AP-02B.1 – Historienzugriff ehemaliger Mitglieder (in Umsetzung)

### Bestätigter Befund

Die aktive `dokument_select_effective`-RLS-Policy gewährte den Eigenzugriff
unbegrenzt über `mitglied_id = current_mitglied_id()`. Der Downloadpfad der
Edge Function `kgv-upload-document` reproduzierte dieselbe unbegrenzte
Freigabe nach einem Service-Role-Lookup. Damit blieb der Eigenzugriff nach dem
Ende der in P11-R05 bestimmten Frist bestehen.

### Umsetzung

Migration `20261112090000_p11_ap02b1_former_member_history_access.sql` ersetzt
den Eigenzweig durch den fest gebundenen `SECURITY DEFINER`-Helfer
`can_access_own_member_history_document(bigint)`. Er leitet das Mitglied
ausschließlich aus `auth.uid()` über den vorhandenen App-User-Kontext ab und
akzeptiert ein formelles `mitglied_ende` nur bis einschließlich 31.12. des
Folgejahres. Die Berechnung liegt in der reinen, nicht clientseitig als
Autorisierung verwendbaren Datumsfunktion
`former_member_history_access_until(date)`.

Die Verwaltungszweige der RLS-Policy bleiben bei den vorhandenen effektiven
Rechten `CanReadDocuments` und `CanManageDocuments`. Der Edge-Download fragt
dieselben effektiven Rechte ab und delegiert Eigenhistorie ausschließlich an
die neue caller-gebundene RPC; frei übergebene Mitglieds- oder Dokumentdaten
werden nicht als Berechtigungsgrundlage verwendet. Es wird keine automatische
Beendigung von Mitgliedschaft oder Zugang eingeführt.

### Test- und Abnahmenachweis

Die neue rückrollbare lokale PostgreSQL/RLS-Fixture
`supabase/tests/20261112090000_p11_ap02b1_former_member_history_access_funktionstest.sql`
deckt aktiven Eigenzugriff, beendete Mitgliedschaft innerhalb der Frist,
31.12./01.01.-Grenze, abgelaufene Frist, fremde/manipulierte IDs, fehlenden
Mitgliedskontext, direkten RLS-Zugriff, effektive Dokumentverwaltung,
administrativen Entzug sowie unveränderten Zustand nach abgewiesenem Schreiben
ab. Sie wurde gegen die lokale Supabase-PostgreSQL-Instanz als Rolle
`authenticated` ausgeführt und endete mit `ROLLBACK`. Der Test bestätigt die
aktive und die beendete Eigenmitgliedschaft innerhalb der Frist, die Sperre
nach Fristablauf, fremde/manipulierte IDs, fehlenden Mitgliedskontext,
effektive Dokumentverwaltung, den vollständigen Entzug der beiden möglichen
Dokumentleserechte (`CanReadDocuments | CanManageDocuments`) sowie den
unveränderten Zustand nach einem abgewiesenen Schreibversuch. Die
Datumsgrenze wird ohne Systemuhränderung über die reine Fristberechnung für
31.12. und 01.01. reproduzierbar verifiziert; die RLS-Ausführung prüft diese
Berechnung mit relativ zur lokalen Serverzeit angelegten Mitgliedschaften.

Nach dem kontrollierten Start von Docker Desktop und WSL2 lief der vollständige
lokale `supabase db reset --local` einschließlich Seed erfolgreich durch. Die
lokale Datenbank enthält 210 angewendete Migrationen einschließlich
`20261112090000_p11_ap02b1_former_member_history_access.sql`. Die aktive
Definition, die `dokument_select_effective`-Policy sowie die EXECUTE-Grants
für `authenticated` und `service_role` wurden anschließend direkt im lokalen
PostgreSQL geprüft. Die betroffenen A2.1-, A3.5- und A3.6-Fixtures bestanden
ebenfalls jeweils mit `ROLLBACK`.

Der isolierte Edge-Vertragstest
`supabase/tests/former-member-history-edge.test.mjs` wurde zusammen mit der
vollständigen Edge-Vertragssuite erfolgreich ausgeführt (90/90). Sie belegt
die JWT-gebundene R05-RPC-Abfrage, die Ablehnung eines abgelaufenen
Eigenzugriffs, die Ablehnung einer manipulierten Dokument-ID, individuelle
Dokumentleserechte und den administrativen Entzug. Es erfolgen keine externen
Uploads oder Downloads. Die Web-Dokumentproxy- und SQL-
Berechtigungsverträge sowie der Web-Produktionsbuild bestanden. Core,
Infrastructure und MAUI-Android bauen; die vollständige .NET-Suite besteht
mit 419/419. Bekannte Core-/Infrastructure-Nullable-Warnungen und der
Web-Chunk-Size-Hinweis bleiben unverändert.

Der Schutz zeitlich begrenzter bereits ausgegebener Storage-Links bleibt
planmäßig AP-02B.3. Die hier geprüften REST/RLS- und Drive-Proxy-Pfade geben
nach Ablauf der Frist keine Dokumentmetadaten oder Drive-Inhalte mehr frei.

**Abnahme AP-02B.1:** vollständig abgenommen. B-01 bleibt unabhängig davon
offen.

## AP-02B.2 – Historienzugriff ehemaliger Pächter (abgeschlossen)

### Bestätigter Befund und Umsetzung

Die aktiven RLS-Policies `parzelle_select_assigned_or_admin` und
`parzellen_belegung_select_own_or_admin` erlaubten einem Mitglied seine
eigenen alten Belegungen und die zugehörige Parzelle zeitlich unbegrenzt. Für
Pachtvertragsdokumente konnte außerdem der R05-Eigenzweig greifen, obwohl ein
Nachpächter die Parzelle bereits tatsächlich übernommen hatte. Damit waren
neue Parzellendaten beziehungsweise spätere Vertragsunterlagen nicht
ausreichend von der früheren Pächterberechtigung getrennt.

Migration `20261112100000_p11_ap02b2_former_tenant_privacy.sql` führt eng
gebundene Eigenhelfer für Belegung, Parzelle, Pachtverhältnis und
vertragsgebundene Dokumente ein. Sie leiten das Mitglied ausschließlich aus
`auth.uid()`/`current_mitglied_id()` ab. Eine Belegung eines anderen Mitglieds
auf derselben Parzelle beendet den Sonderzugriff erst, wenn ihr
`von_datum` tatsächlich erreicht ist; geplante Zukunftsbelegungen bleiben
unwirksam. Nach einer tatsächlichen Übernahme bleiben weder Parzelle,
frühere Belegung, Pachtverhältnis, Vertragsfassung noch das alte
Pachtvertragsdokument sichtbar. Ein unabhängiges weiteres historisches
Pachtverhältnis bleibt dagegen zugänglich.

Der R05-Helfer schließt nun Pachtvertragsfassungen aus; diese laufen
ausschließlich über die strengere R10-Prüfung. Das bewahrt den Eigenzugriff
auf sonstige Mitgliedshistorie bei fortbestehender aktiver oder passiver
Mitgliedschaft und verhindert zugleich, dass R05 die Nachpächtergrenze
umgeht. Parzellenlesen/-schreiben verwendet jetzt die bestehenden effektiven
Rechte `CanReadParzellen`/`CanWriteParzellen`; die vorhandene
Vertragsverwaltungsberechtigung `CanCreateMitglied` bleibt für die
administrative Vertragsansicht erhalten. Individuelle Entzüge wirken in allen
Verwaltungszweigen.

Der Drive-Downloadpfad fragt zusätzlich die caller-gebundene
`can_access_own_historical_lease_document`-RPC ab. Freie Mitglieds-,
Parzellen- oder Dokument-IDs bilden keine Autorisierungsquelle. Bestehende
historische Daten, Fassungen und Provenance werden nicht verändert.

### Test- und Abnahmenachweis

Der vollständige lokale Neuaufbau einschließlich Seed lief mit 211 Migrationen
erfolgreich durch. Die aktive RLS-Policy- und EXECUTE-Grant-Prüfung bestätigt
die effektiven Verwaltungs- und die getrennten Eigenzweige.

Die rückrollbare, echte PostgreSQL-/RLS-Fixture
`supabase/tests/20261112100000_p11_ap02b2_former_tenant_privacy_funktionstest.sql`
lief als `authenticated` und endete mit `ROLLBACK`. Sie belegt eigenen
Pachtzugriff vor der Übernahme, eine geplante Zukunftsbelegung ohne vorzeitige
Sperre, die Sperre mit tatsächlicher Übernahme, Fremd-/manipulierte IDs,
mehrere historische Pachtverhältnisse, fortbestehende Mitgliedschaft und
nichtpachtbezogene R05-Historie, vollständigen administrativen Entzug sowie
Schreib-/Provenanceschutz.

Die R05-Fixture sowie Erstvertrags-, Neufassungs- und Dokumentarchivierungs-
Regressionen bestanden jeweils mit `ROLLBACK`. Die vollständige Edge-Suite
bestand mit 92/92, einschließlich des neuen R10-Downloadvertrags. Die
Web-Dokumentproxy- und SQL-Berechtigungsverträge sowie der Web-
Produktionsbuild bestanden. Core, Infrastructure und MAUI-Android bauen; die
vollständige .NET-Suite besteht mit 419/419. Bestehende Nullable-Warnungen und
der Web-Chunk-Size-Hinweis bleiben unverändert.

Bereits ausgegebene Storage-Links und ihre Laufzeiten bleiben planmäßig
AP-02B.3. B-01 bleibt unabhängig als betrieblicher Bereitstellungsnachweis
offen.

**Abnahme AP-02B.2:** vollständig abgenommen.

## AP-02B.3 – Geschützte Dateien, Zählerfotos und Downloadlinks (in Umsetzung)

### Bestätigter Befund und Umsetzung

Die Ablesungs-RLS ließ über eine zeitlich unbegrenzte Eigenbelegung auch nach
R10-relevanter Nachpächterübernahme die zugehörigen Ablesungen und damit ihre
Fotoverweise sichtbar. Der Drive-Fotoproxy verwendete außerdem für `admin` und
`vorstand` eine direkte Rollenabkürzung; individuelle Entzüge von
`CanReadMeters` wirkten dort nicht. Schließlich konnte ein Client über den
direkten Tabellen-PATCH beliebige Drive-Datei-IDs und Pfade einer Ablesung
zuordnen; der privilegierte Proxy hätte diese IDs anschließend abgerufen.

Migration `20261112110000_p11_ap02b3_protected_file_access.sql` ersetzt den
Eigenzweig durch `can_access_own_meter_reading(bigint)`. Der SECURITY-DEFINER-
Helfer bindet an `auth.uid()` und delegiert die Mitgliedschaftsfrist R05 und
die Nachpächtergrenze R10 an den vorhandenen caller-gebundenen
Parzellenhelfer. Verwaltungszugriff bleibt beim effektiven `CanReadMeters`.
Der Trigger `guard_meter_reading_photo_provenance` sperrt browserseitige
Foto-/Drive-Provenance sowohl beim Insert als auch beim Update; bereits
verknüpfte Nachweise sind unveränderlich.

Der Foto-Upload erhält jetzt zwingend die Ablesungs-ID, autorisiert sie per
JWT-gebundenem User-Client gegen `CanReadMeters` oder den Eigenhelfer und
leitet Pfad, Datum, Parzelle, Medium und Ableseart ausschließlich aus dem
gespeicherten Ablesungsdatensatz ab. Erst danach schreibt die vertrauenswürdige
Service-Role-Kette die einmalige Drive-Provenance. Der Client erhält weder
Drive-ID noch Web-Link und kann keine direkte Datenbankverknüpfung mehr
ausführen. Der Web-Viewer entfernt den Legacy-Fallback auf beliebige
`http(s)`-Pfade und ruft ausschließlich den autorisierten Proxy auf; seine
Antwort trägt `Cache-Control: private, no-store`.

Bereits ausgestellte externe Drive- oder Storage-Bearer-Links können technisch
bis zu ihrem jeweiligen Ablauf gültig bleiben. Der korrigierte Pfad stellt
keine solchen Links mehr aus und prüft jede neue Proxy-Anfrage erneut. Eine
sofortige serverseitige Widerrufbarkeit historisch bereits ausgegebener
Bearer-URLs ist damit ausdrücklich nicht behauptet.

### Test- und Abnahmenachweis

Der vollständige lokale `supabase db reset --local` einschließlich Seed
verarbeitete 212 Migrationen einschließlich der neuen AP-02B.3-Migration.
Die rückrollbare Fixture
`20261112110000_p11_ap02b3_protected_file_access_funktionstest.sql` lief als
`authenticated` mit `ROLLBACK`: berechtigter Eigenzugriff, Fremd-/manipulierte
Provenance, geplanter und tatsächlicher Nachpächterwechsel sowie ein
individuell von `CanReadMeters` entzogener Administrator werden geprüft.
Sie bestätigt, dass keine unberechtigte Tabellenänderung oder Fotoverknüpfung
stattfindet. Die aktiven RLS-Policy und EXECUTE-Grants wurden danach direkt
im lokalen PostgreSQL kontrolliert.

Die R05- und R10-Fixtures bestanden erneut mit `ROLLBACK`. Die gesamte
lokale Edge-Vertragssuite bestand mit 92/92; der neue Web-Fotovertrag prüft
die verpflichtende Ablesungs-ID, das Fehlen browserseitiger Drive-IDs, die
caller-gebundene Autorisierung und `private, no-store`. Die geänderte Edge
Function startete lokal ohne Deployment. Web-Dokumentproxy und SQL-
Berechtigungsverträge sowie der Produktionsbuild bestanden. Die vollständige
.NET-Suite besteht mit 419/419; Core, Infrastructure und MAUI-Android bauen
erfolgreich. Bestehende Nullable- und Web-Chunk-Hinweise bleiben unverändert.

**Abnahme AP-02B.3:** vollständig abgenommen.

**AP-02B-Gesamtabnahme:** technisch abgenommen. R05 (ehemalige Mitglieder),
R10 (ehemalige Pächter) und der serverseitig autorisierte neue Datei-/Fotoabruf
sind durch lokale Nachweise abgedeckt. B-01 bleibt als betrieblicher Nachweis
der tatsächlichen Edge-Bereitstellung offen. Die unvermeidbare Restlaufzeit
historisch bereits ausgegebener Bearer-Links ist kein neuer Freigabepfad und
muss bis zum Ablauf solcher Links als Restrisiko berücksichtigt werden.
