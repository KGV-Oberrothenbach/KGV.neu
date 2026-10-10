# G8 – Arbeitseinsätze

## Ziel und Scope

G8 entwickelt Arbeitseinsätze schrittweise weiter und harmonisiert Web und MAUI. Die verbindliche Web-Zielarchitektur lautet:

```text
UI → Service → Repository → Supabase/RPC
```

Fachliche Änderungen erfolgen ausschließlich in der dafür vorgesehenen Teilgruppe.

## Teilgruppen

- **G8.1 – Web-Grundstruktur, Typen und Lesezugriffe:** Verwaltungs-UI aus `app/page.tsx` herauslösen, gemeinsame Modelle zentralisieren und Verwaltungslesezugriffe hinter Service und Repository bringen.
- **G8.2 – Berechtigungen und Rollen:** `ManageWorkAssignments`, effektive Rollenrechte sowie Grants und Revocations.
- **G8.3 – Editor-Vorgaben:** fachliche Standardwerte für Sichtbarkeit und Anmeldeschluss.
- **G8.4 – An- und Abmelderegeln:** Beginn-, Frist- und Kapazitätsregeln.
- **G8.5 – Teilnehmerverwaltung:** Statusverwaltung und Ablösung der Verwaltungs-Altlogik.
- **G8.6 – Arbeitsstundenübergabe:** eigenständige und administrative Arbeitsstundenerfassung im Zusammenhang mit Einsätzen.
- **G8.7 – MAUI/Web-Gleichstand und Bereinigung:** fachliche und technische Angleichung beider Clients.
- **G8.8 – Regression / technische Abschlussprüfung:** vollständige fachliche, technische und Sicherheitsprüfung.
- **G8.9 – Dokumentation und G8-Abschluss:** Dokumentation finalisieren und erst danach den Gesamtmerge vorbereiten.
- **G8.10 – Release:** Release erst nach vollständigem Merge nach `main` erstellen und kontrollieren.

## Bereits beschlossene Fachregeln

### Berechtigung und Gleichstand

- `ManageWorkAssignments` ist für Admin und Vorstand das Basisrecht.
- Benutzerspezifische Grants und Revocations gelten auch für diese Permission.
- Web und MAUI erhalten dieselben Rechte, Vorgabewerte, Validierungen, An-/Abmelderegeln, Teilnehmerstatus, Arbeitsstundenerfassung, Demo-Regeln und Lock-Regeln.

### Editor und Anmeldung

- Für einen neuen Einsatz gilt `sichtbar_ab = sofort`.
- Der Standard-Anmeldeschluss ist Einsatzdatum minus zwei Tage, 00:00 Uhr.
- Der Standard für `sichtbar_bis` ist Einsatzdatum plus 14 Tage.
- Diese Werte bleiben im Editor änderbar.
- Eine Benutzerabmeldung ist bis zum Einsatzbeginn möglich, auch nach dem Anmeldeschluss.
- Eine Anmeldung wird bei Abmeldung nicht gelöscht, sondern auf `abgesagt` gesetzt; die Historie bleibt erhalten.

### Teilnehmer und Arbeitsstunden

- Es gibt keinen manuellen „Teilgenommen“-Button.
- `nicht_erschienen` ist eine Verwaltungsaktion.
- Benutzer können nach dem Einsatz eigene Stunden mit änderbarer Stundenanzahl einreichen; diese durchlaufen die normale G7-Prüfung.
- Admin und Vorstand können Stunden je Teilnehmer mit änderbarer Stundenanzahl administrativ erfassen.
- Es gibt keine automatische Arbeitsstundenübernahme.
- Doppelte Übernahme muss technisch verhindert werden.
- Bereits entstandene Arbeitsstunden werden durch G8 nicht automatisch gelöscht.

## G8.1 – Ausgangszustand und Web-Struktur

Ausgangszustand war eine monolithische Verwaltungsansicht in `KGV.Web/app/page.tsx`, einschließlich lokaler Work-Assignment-Typen, Editor, Teilnehmeransicht und direkter lesender Supabase-Zugriffe.

G8.1 legt die Web-UI unter `KGV.Web/features/work-assignments/` ab und trennt `WorkAssignmentsManagement`, `WorkAssignmentList`, `WorkAssignmentEditor` und `WorkAssignmentParticipants`. Die gemeinsamen Verträge `WorkAssignment`, `WorkAssignmentRegistration` und `WorkAssignmentMember` liegen unter `KGV.Web/models/work-assignments/`. Verwaltungslesevorgänge folgen damit UI → Work-Assignment-Service → Work-Assignment-Repository → Supabase.

Bewusst verbliebene Altlogik sind die vorhandenen Schreibvorgänge für Einsätze und Anmeldungen sowie die bisherige Übernahme einer Teilnahme als Arbeitsstunde. Diese werden erst in G8.3 bis G8.6 fachlich überarbeitet.

## G8.2 – Berechtigungen, RLS, Demo-Scope und Locks

`CanManageWorkAssignments` ist als Bit `1 << 20` (`1048576`) zentral definiert. Admin und Vorstand besitzen es als Basisrecht, Benutzer nicht. Wie bei allen Fachrechten wird die effektive Berechtigung aus Basisrolle, `permission_grants` und `permission_revocations` gebildet; ein Grant ermöglicht die Verwaltung für Benutzer, eine Revocation entzieht sie auch dem Vorstand.

Die zuvor abweichenden serverseitigen Rollenmasken wurden bereinigt: Core, Web und Supabase verwenden nun dieselbe Basis-Permission-Matrix für Admin, Vorstand und Benutzer.

Web und MAUI prüfen die effektive Permission für Verwaltungszugänge, Teilnehmeransicht und Editieren; die normale Startseitenansicht sowie die eigene Anmeldung bleiben davon getrennt. Bestehende Einsätze werden in beiden Clients über den gemeinsamen serverseitigen Bearbeitungs-Lock geöffnet; ohne Permission oder ohne erfolgreich angeforderten Lock ist keine Bearbeitung möglich.

Die Supabase-Policies ersetzen die bisherigen Admin-/Vorstands-Hintertüren für `arbeitseinsatz` und `arbeitseinsatz_anmeldung` durch `has_effective_permission(1048576)` und einen zusätzlichen Produktiv-/Demo-Scope. Demo- und Reviewer-Konten bleiben auf zulässige Demo-Daten beschränkt; Grants erweitern diesen Scope nicht. Die normale Sichtbarkeit aktiver Einsätze und die eigenen An-/Abmeldungen bleiben als getrennte Benutzerpfade erhalten.

G8.2 zieht bewusst keine Punkte aus G8.3 bis G8.6 vor: Editor-Defaults, Frist-/Kapazitätsregeln, Teilnehmerstatus und Arbeitsstundenübergabe bleiben unverändert.

## G8.3 – Verwaltung, Editor-Defaults und Validierung

Neue Einsätze verwenden in Web und MAUI Berliner Vereinszeit: `sichtbar_ab` ist der aktuelle Zeitpunkt auf Minute, `anmeldung_bis` zwei Kalendertage vor dem Einsatztag um 00:00 Uhr und `sichtbar_bis` 14 Kalendertage nach dem Einsatztag um 23:59 Uhr. Aktiv ist standardmäßig gesetzt; alle Werte bleiben editierbar. Folgeschichten übernehmen Stammdaten und Schichtdauer, berechnen jedoch Sichtbarkeit und Anmeldeschluss neu.

Die gemeinsame Validierung verlangt Titel und Datum, erlaubt keine negative Stundenzahl oder Teilnehmerzahl unter eins, kein Ende vor Beginn, keinen ungültigen Sichtbarkeitszeitraum und keinen Anmeldeschluss nach Einsatzbeginn. Die Datenbank sichert diese Verwaltungswerte zusätzlich ab.

Web-CRUD folgt nun UI → Service → Repository → Supabase. Absagen bedeutet weiterhin ausschließlich `aktiv = false` und erhält Anmeldungen; endgültiges Löschen bleibt getrennt und weist auf das FK-CASCADE hin. G8.4 bis G8.6 (Benutzer-An-/Abmeldelogik, Teilnehmerstatus und Arbeitsstundenintegration) wurden bewusst nicht vorgezogen.

## G8.4 – Anmeldung, Abmeldung, Fristen und Kapazität

Die normalen Web- und MAUI-Pfade verwenden die serverseitigen Signup- und Signoff-RPCs als fachliche Wahrheit. Signup verlangt eigenes Mitglied, passenden Produktiv-/Demo-Scope, einen aktiven Einsatz, offene Frist, freien Platz und einen Zeitpunkt vor dem Beginn. Der Beginn ist `datum + start_uhrzeit`, ohne Startzeit konsistent `datum 23:59`; alle Zeitvergleiche erfolgen mit `kgv_local_now()` in Berliner Vereinszeit.

Signoff setzt den bestehenden Datensatz ausschließlich auf `abgesagt`; es erfolgt kein Own-DELETE. Eine Abmeldung ist unabhängig vom Anmeldeschluss bis vor Beginn möglich, auch für inzwischen deaktivierte Einsätze. Wiederanmeldung reaktiviert denselben Datensatz. Signup sperrt die Einsatzzeile mit `FOR UPDATE`, sodass parallele Anmeldungen den letzten Platz nicht doppelt erhalten.

Direkte Benutzerstatuswechsel sind auf `angemeldet` und `abgesagt` beschränkt; `teilgenommen` und `nicht_erschienen` bleiben Verwaltungslogik. Web und MAUI verwenden dieselben RPCs; die Web-Anzeige verwendet Berliner Zeit und zeigt den Abmeldeweg nach Frist weiterhin bis zum Beginn. G8.5 und G8.6 wurden bewusst nicht vorgezogen.

## G8.5 – Zentrale Teilnehmerverwaltung und Statusführung

Die Verwaltungsansichten in Web und MAUI lesen alle Anmeldungen eines Einsatzes und zeigen sie gruppiert als **Angemeldet**, **Teilgenommen**, **Nicht erschienen** und **Abgesagt**. Für die Kapazität zählen ausschließlich Datensätze mit Status `angemeldet`. Die Teilnehmerliste wird über die gemeinsame Service-/Repository-Schicht bzw. den Shared-Service geladen; die Verwaltung schreibt nicht mehr direkt auf `arbeitseinsatz_anmeldung`.

Die Verwaltungsaktion läuft ausschließlich über `manage_arbeitseinsatz_anmeldung(arbeitseinsatz_id, mitglied_id, action)`. Sie verlangt die effektive Permission `ManageWorkAssignments`, sperrt Einsatz und vorhandene Anmeldung mit `FOR UPDATE` und erzwingt den Produktiv-/Demo-Scope. Sie akzeptiert nur die Aktionen `anmelden`, `absagen` und `nicht_erschienen`; ein manueller Status `teilgenommen` wird nicht angeboten. Direkte RLS-Schreibrechte für administrative Teilnehmeränderungen bestehen nicht.

Eine administrative Anmeldung ist für aktive Mitglieder vor Einsatzbeginn möglich und ignoriert den Anmeldeschluss, nicht jedoch `aktiv`, Beginn oder Kapazität. Ein bestehender Status `angemeldet` bleibt idempotent; `abgesagt` wird im selben historischen Datensatz reaktiviert. `teilgenommen` und `nicht_erschienen` werden nicht wieder geöffnet. Die Verwaltung kann `angemeldet` bis vor Beginn absagen, `abgesagt` idempotent belassen und `nicht_erschienen` als Korrektur auf `abgesagt` setzen; `teilgenommen` bleibt unveränderlich.

`nicht_erschienen` ist erst ab Beginn zulässig (ohne Startzeit ab 23:59 Uhr) und kann nur aus `angemeldet` gesetzt werden; die Wiederholung bleibt idempotent. Damit bleibt die G8.4-Selbstbedienung strikt getrennt: normale Mitglieder dürfen weiterhin nur zwischen `angemeldet` und `abgesagt` wechseln und niemals Verwaltungsstatus überschreiben. Arbeitsstunden werden in G8.5 weder erzeugt noch übernommen; die dafür vorgesehene G8.6-Logik wurde nicht begonnen.

## G8.6 – Arbeitseinsatz → Arbeitsstunden

`arbeitsstunde.arbeitseinsatz_anmeldung_id` verknüpft eine Einsatz-Arbeitsstunde technisch mit genau einer Anmeldung. Der Fremdschlüssel verwendet `ON DELETE SET NULL`; ein partieller Unique-Index verhindert mehr als eine Stunde je Anmeldung. Altbestände bleiben unverknüpft, werden nicht heuristisch migriert und bestehende Stunden werden bei späteren Status- oder Einsatzänderungen nicht automatisch gelöscht.

Der Benutzer reicht nach Einsatzende über einen dedizierten RPC eine offene G7-Arbeitsstunde ein. Einsatzdatum, Mitglied, Saison des Einsatzjahres und Link stammen ausschließlich vom Server; Stunden und Arbeitsart bleiben editierbar. Eine administrative Bestätigung verlangt gleichzeitig `ManageWorkAssignments` und `ManageWorkHours`, erzeugt sofort eine genehmigte G7-Stunde und setzt die Anmeldung atomar auf `teilgenommen`.

Die vorhandene G7-Review bleibt der einzige Prüfpfad: Freigeben und Korrigieren einer verknüpften Stunde bestätigen die Teilnahme innerhalb derselben Transaktion, Ablehnen und Löschen ändern den Teilnehmerstatus nicht. Es gibt keinen manuellen Teilgenommen-Button und keine automatische Stundenanlage durch bloße Teilnahme. Produktiv- und Demo-Scope folgen den G8.5-Grenzen.

## G8.7 – MAUI/Web-Gleichstand und Bereinigung

Das C#-Startseitenmodell entspricht jetzt exakt `v_startseite_arbeitseinsatz`: Titel, Beschreibung, Datum, Start-/Endzeit, Treffpunkt, Kapazität, Stundenwert, Sichtbarkeit, Anmeldeschluss, Aktivstatus und Teilnehmerzahlen. Die alten Übergangsfelder `thema`, `beginn`, `ende` und `anmeldung_moeglich` wurden aus diesem Pfad entfernt.

Die Startseiten-Infrastruktur verwendet die kanonische View direkt. Die früheren Nachlade-/Enrichment-Fallbacks für Einsatzzeiten, Aktivstatus, Sichtbarkeit, Kapazität und Titel/Beschreibung sowie die doppelte Sichtbarkeitsfilterung entfallen. Nur der benutzerspezifische eigene Anmeldestatus wird weiterhin getrennt aus `arbeitseinsatz_anmeldung` gelesen. Die View bleibt `SECURITY INVOKER`; Produktiv-/Demo-Scope wird dadurch nicht umgangen.

`WorkAssignmentRules` bündelt Defaults, Validierung sowie fachlichen Beginn und fachliches Ende mit Vereinszeit. MAUI verwendet diese Regeln in Teilnehmerverwaltung und Startseiten-Detail für die gleichen Grenzen wie Web. Effektive Permissions bleiben die Grundlage für Arbeitseinsatzverwaltung und Stundenbestätigung; die Web-Architektur ist UI → Service → Repository → Supabase/RPC, der MAUI-Pfad Page → ISupabaseService → SupabaseService → Supabase/RPC.

Die Home-Webtypen liegen im gemeinsamen Work-Assignment-Modellbereich statt repository-lokal. G8-Komponenten enthalten keine direkten Supabase-Aufrufe. Web und MAUI wurden für Verwaltung, Locks, Anmeldung/Abmeldung, Teilnehmerstatus, eigene und administrative Arbeitsstunden, Linkstatus, Datumsschutz und Demo-/Produktivscope gegen die bestehenden G8.1–G8.6-Regeln abgeglichen. G8.7 führt keine neuen Fachregeln ein.

## G8.8 – Regression / technische Abschlussprüfung

G8.1 bis G8.7 wurden als Regression gegen die finale Migrationskette, die Web- und MAUI-Pfade sowie die zentralen Regelhelfer geprüft. Die Permission-Matrix bestätigt: Admin und Vorstand besitzen `ManageWorkAssignments` und `ManageWorkHours`, ein Benutzer mit ausschließlich `ManageWorkAssignments` darf Teilnehmer verwalten, aber nicht administrativ bestätigen, und ein Benutzer mit beiden Grants darf beides. Eine Revocation bleibt wirksam. Die Unit-Tests prüfen die getrennte effektive Berechnung der beiden Rechte zusätzlich.

Die statische RPC-/RLS-Prüfung umfasst CRUD-Validierung und Defaults, Browser-Locks, Signup/Signoff, Kapazität ausschließlich für `angemeldet`, Verwaltungsstatus, `nicht_erschienen`, G8.6-Stunden, G7-Review-Synchronisierung, Link-Unique-Index und Einsatzdatumsschutz. Alle G8-RPCs verwenden `SECURITY DEFINER` mit eingeschränktem `search_path`, Auth-/Permission-Prüfungen und `PUBLIC`-/`anon`-Revoke sowie `authenticated`-Grant. Die Startseitenview bleibt `SECURITY INVOKER`; View, C#-Modell und Home-Webmodell verwenden dieselben kanonischen Spalten. React-G8-Komponenten enthalten keine direkten Supabase-/RPC-Aufrufe; MAUI-Pages verwenden den Servicepfad.

Dabei wurde eine Scope-Regression behoben: Die finalen Self-Service-RPCs für Signup und Signoff prüfen im Produktivkontext jetzt – wie Verwaltung und Stunden-RPCs – sowohl einen produktiven Einsatz als auch ein produktives Mitglied. Demo-/Reviewer-Konten bleiben auf `is_demo_member_arbeitseinsatz_scope(...)` beschränkt; Grants erweitern diese Grenze nicht. Die Migrationskette, Migrationsrechte und Funktionsendstände wurden statisch verglichen. DB-Integrationstests konnten lokal nicht ausgeführt werden, weil die Supabase-CLI nicht installiert ist; die entsprechenden Fälle wurden daher vollständig statisch geprüft.

Technisch gehören `git diff --check`, Core-/Infrastructure-Build, relevante Tests, Web-TypeScript, Web-Build, gezielter G8-Lint, vollständiger Lint mit getrennten Altbefunden sowie MAUI-Kompilierung zur Prüfung. G8.8 führt keine neue Fachregel ein und ersetzt weder G8.9 noch G8.10.

## G8.9 – Dokumentation und G8-Abschluss

G8-Dokumentation, Architekturentscheidungen, Permission, An-/Abmelderegeln sowie Teilnehmer- und Arbeitsstundenlogik werden final festgehalten. Restpunkte und die vollständige G8-Prüfung werden dokumentiert. Erst danach darf `codex/g8` nach `main` gemergt werden; vorher erfolgt kein Teil-Merge.

## G8.10 – Release

G8 ist erst nach erfolgreicher Release-Erstellung abgeschlossen. Nach dem vollständigen Merge nach `main` werden in dieser Reihenfolge `main` aktualisiert und auf einen sauberen Stand geprüft, Web und MAUI auf `main` final gebaut, die Versionsnummer festgelegt oder erhöht, ein erforderlicher Versionscommit erstellt, ein Git-Tag erstellt und gepusht, ein GitHub Release mit Release Notes erstellt, erforderliche Release-Artefakte erzeugt und angehängt sowie das veröffentlichte Release kontrolliert.

Die Release Notes enthalten mindestens Arbeitseinsatzverwaltung, `ManageWorkAssignments`, Web-/MAUI-Rechte, An-/Abmelderegeln, Teilnehmerverwaltung, Arbeitsstunden aus Arbeitseinsätzen, die G7-Integration, Demo-/RLS-Anpassungen und relevante Architekturänderungen.
