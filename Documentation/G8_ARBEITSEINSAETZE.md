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

Web und MAUI prüfen die effektive Permission für Verwaltungszugänge, Teilnehmeransicht und Editieren; die normale Startseitenansicht sowie die eigene Anmeldung bleiben davon getrennt. Bestehende Einsätze werden in beiden Clients über den gemeinsamen serverseitigen Bearbeitungs-Lock geöffnet; ohne Permission oder ohne erfolgreich angeforderten Lock ist keine Bearbeitung möglich.

Die Supabase-Policies ersetzen die bisherigen Admin-/Vorstands-Hintertüren für `arbeitseinsatz` und `arbeitseinsatz_anmeldung` durch `has_effective_permission(1048576)` und einen zusätzlichen Produktiv-/Demo-Scope. Demo- und Reviewer-Konten bleiben auf zulässige Demo-Daten beschränkt; Grants erweitern diesen Scope nicht. Die normale Sichtbarkeit aktiver Einsätze und die eigenen An-/Abmeldungen bleiben als getrennte Benutzerpfade erhalten.

G8.2 zieht bewusst keine Punkte aus G8.3 bis G8.6 vor: Editor-Defaults, Frist-/Kapazitätsregeln, Teilnehmerstatus und Arbeitsstundenübergabe bleiben unverändert.

## G8.7 – MAUI/Web-Gleichstand und Bereinigung

G8.7 stellt für Web und MAUI dieselben Rechte, Vorgabewerte, Validierungen, Anmeldungen und Abmeldungen, Teilnehmerstatus, Arbeitsstundenerfassung, Demo-Regeln und Lock-Regeln sicher. Alte Rollenprüfungen werden entfernt; überflüssige Methoden und tote Typen werden bereinigt. Direkte G8-Supabase-Zugriffe aus React werden beseitigt und der Startseitenmodell-/View-Mismatch wird korrigiert. Die Web-Zielarchitektur bleibt UI → Service → Repository → Supabase/RPC.

## G8.8 – Regression / technische Abschlussprüfung

Mindestens geprüft werden Admin, Vorstand, User, User mit Grant, Vorstand mit Revocation sowie Demo- und Reviewer-Fälle. Die Regression umfasst Arbeitseinsätze anlegen, bearbeiten, deaktivieren und löschen, Bearbeitungssperren, Folgeschichten, Kapazität, Anmeldeschluss, Abmeldung nach Anmeldeschluss, Verhalten nach Einsatzbeginn, Teilnehmerverwaltung und `nicht_erschienen`.

Ebenfalls geprüft werden eigene Arbeitsstunden des Benutzers, administrative Stunden von Vorstand/Admin, geänderte Stundenanzahl, Schutz vor doppelter Übernahme, abgelehnte Arbeitsstunden und der Erhalt der G7-Historie. Technisch sind TypeScript, Web-Build, Lint, MAUI-Build, Migrationen, RLS/RPC und `git diff --check` Teil der Abschlussprüfung.

## G8.9 – Dokumentation und G8-Abschluss

G8-Dokumentation, Architekturentscheidungen, Permission, An-/Abmelderegeln sowie Teilnehmer- und Arbeitsstundenlogik werden final festgehalten. Restpunkte und die vollständige G8-Prüfung werden dokumentiert. Erst danach darf `codex/g8` nach `main` gemergt werden; vorher erfolgt kein Teil-Merge.

## G8.10 – Release

G8 ist erst nach erfolgreicher Release-Erstellung abgeschlossen. Nach dem vollständigen Merge nach `main` werden in dieser Reihenfolge `main` aktualisiert und auf einen sauberen Stand geprüft, Web und MAUI auf `main` final gebaut, die Versionsnummer festgelegt oder erhöht, ein erforderlicher Versionscommit erstellt, ein Git-Tag erstellt und gepusht, ein GitHub Release mit Release Notes erstellt, erforderliche Release-Artefakte erzeugt und angehängt sowie das veröffentlichte Release kontrolliert.

Die Release Notes enthalten mindestens Arbeitseinsatzverwaltung, `ManageWorkAssignments`, Web-/MAUI-Rechte, An-/Abmelderegeln, Teilnehmerverwaltung, Arbeitsstunden aus Arbeitseinsätzen, die G7-Integration, Demo-/RLS-Anpassungen und relevante Architekturänderungen.
