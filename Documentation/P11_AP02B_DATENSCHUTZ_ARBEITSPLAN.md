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
