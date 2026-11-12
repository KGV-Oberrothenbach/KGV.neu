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

### Testnachweis und offener technischer Blocker

Die neue rückrollbare lokale PostgreSQL/RLS-Fixture
`supabase/tests/20261112090000_p11_ap02b1_former_member_history_access_funktionstest.sql`
deckt aktiven Eigenzugriff, beendete Mitgliedschaft innerhalb der Frist,
31.12./01.01.-Grenze, abgelaufene Frist, fremde/manipulierte IDs, fehlenden
Mitgliedskontext, direkten RLS-Zugriff, effektive Dokumentverwaltung,
administrativen Entzug sowie unveränderten Zustand nach abgewiesenem Schreiben
ab. Sie wurde noch nicht gegen die lokale Supabase-PostgreSQL-Instanz
ausgeführt: Docker Desktop ist in der aktuellen Sitzung nicht erreichbar
(`//./pipe/dockerDesktopLinuxEngine` fehlt), sodass `supabase start` vor dem
Migrationslauf endet. Der vollständige Neuaufbau, die Ausführung dieser SQL-
Fixture und die daran anschließenden Dokument-/Mitgliedschaftsregressionen
bleiben daher offen.

Der isolierte Edge-Vertragstest
`supabase/tests/former-member-history-edge.test.mjs` wurde erfolgreich
ausgeführt. Er belegt die JWT-gebundene R05-RPC-Abfrage, die Ablehnung eines
abgelaufenen Eigenzugriffs, die Ablehnung einer manipulierten Dokument-ID,
individuelle Dokumentleserechte und den administrativen Entzug. Es erfolgen
keine externen Uploads oder Downloads.

Der Schutz zeitlich begrenzter bereits ausgegebener Storage-Links bleibt
planmäßig AP-02B.3. Die hier geprüften REST/RLS- und Drive-Proxy-Pfade geben
nach Ablauf der Frist keine Dokumentmetadaten oder Drive-Inhalte mehr frei.

**Abnahme AP-02B.1:** noch offen bis zum erfolgreichen lokalen Supabase-
Neuaufbau und den echten PostgreSQL-/RLS-Regressionen. B-01 bleibt unabhängig
davon offen.
