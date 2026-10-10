# G9 – Termine & Bekanntmachungen

## Ziel und Scope

G9 gleicht Termine und Bekanntmachungen in Web und MAUI fachlich an. Datenbank, RPC/RLS und gemeinsamer Core sind bei zentral festgelegten Regeln führend. Die Web-Zielarchitektur ist:

```text
UI → Service → Repository → Supabase/RPC
```

`home-service` bleibt ein Aggregator. G9 führt keine globale Rollenarchitektur ein, sondern verwendet das zentrale Permission-System.

## Teilgruppen

- **G9.1 – Web-Struktur & Grundlagen:** Modelle, Features sowie Service-/Repository-Pfade für Termine und Bekanntmachungen.
- **G9.2 – Permissions, RLS & Demo-Scope:** effektive Rechte, Scope-Grenzen und Browser-Locks.
- **G9.3 – Terminverwaltung:** Verwaltung, Validierung und Lifecycle von Terminen.
- **G9.4 – Terminanzeige, Startseite & Kalenderexport:** Home-View und gemeinsamer ICS-Export.
- **G9.5 – Bekanntmachungsverwaltung:** Editor, Sortierung und Lifecycle von Bekanntmachungen.
- **G9.6 – Bekanntmachungsanzeige & HTML-Sicherheit:** sichere Anzeige, Vorschau und Links.
- **G9.7 – MAUI-Parität & Cleanup:** Home-States und Detailnavigation angleichen.
- **G9.8 – Regression & technische Prüfung:** Rechte-, HTML- und ICS-Regressionen sowie technische Prüfung.
- **G9.9 – Dokumentation & Controlled Merge:** Abschlussdokumentation und kontrollierte Merge-Vorbereitung.
- **G9.10 – Release:** erst nach Merge folgender Release-Ablauf.

## Verbindliche Fachregeln

### Termine und Bekanntmachungen

- Es gibt keinen aktiven Harddelete-Pfad in Web oder MAUI; Datensätze werden deaktiviert beziehungsweise reaktiviert.
- Für `termin` und `bekanntmachung` existiert in RLS keine DELETE-Policy.
- Neue Datensätze starten aktiv. Normales Speichern erhält den bestehenden Aktivstatus.
- Aktivieren und Deaktivieren sind eigene Lifecycle-Aktionen; ein Aktiv-Checkbox-Mischpfad im normalen Editor besteht nicht.

### Permissions

```text
CanManageAppointments  = 2097152 = 1 << 21
CanManageAnnouncements = 4194304 = 1 << 22
```

Die effektive Permission lautet `base role + grants - revocations`. Admin und Vorstand besitzen beide Rechte als Basis, User keines. Grants und Revocations bleiben unabhängig; es gibt keine Rollen-Fallbacks in Web oder MAUI. Homepage-Verwaltungsaktionen verwenden getrennt `CanManageAppointments` und `CanManageAnnouncements`.

## G9.1 – Web-Struktur

Termine liegen unter `features/appointments`, `models/appointments`, `services/appointments` und `repositories/appointments`; Bekanntmachungen entsprechend unter `features/announcements`, `models/announcements`, `services/announcements` und `repositories/announcements`. Der Kalenderpfad liegt unter `services/calendar`. Neue React-Komponenten enthalten keine direkte Supabase-Fachlogik. `page.tsx` bleibt außerhalb der ausgelösten G9-Fachbereiche Übergangsstruktur.

## G9.2 – Permissions, RLS, Demo-Scope und Locks

`has_effective_permission` ist die serverseitige Basis. Management-Policies erlauben SELECT, INSERT und UPDATE, bewusst aber kein DELETE; die normale sichtbare SELECT-Policy ist getrennt. Der Produktiv-/Demo-Scope wird serverseitig erzwungen: `is_demo` stammt beim INSERT aus der Session und kann per UPDATE nicht geändert werden. Browser-Edit-Locks unterstützen `termin` und `bekanntmachung`. Grants erweitern den Demo-/Produktivscope nicht.

## G9.3 – Terminverwaltung

Pflicht sind Titel und Datum. Beschreibung, Startzeit, Endzeit, Sichtbar ab und Sichtbar bis sind optional. Wenn Start und Ende gesetzt sind, gilt `Ende >= Start`; bei beiden Sichtbarkeitsgrenzen gilt `bis >= ab`. Start- und Endzeit bleiben unabhängig nullable.

Die Management-Sortierung ist:

```text
Datum ASC
Startzeit ASC NULLS LAST
Endzeit ASC NULLS LAST
Titel ASC, kultur-/case-insensitive
```

Termine werden nicht gelöscht, sondern deaktiviert oder reaktiviert. Normales Speichern erhält den Aktivstatus. Das Web verwendet für bestehende Datensätze eine Bearbeitungssperre.

## G9.4 – Terminanzeige und Kalender

Die Homepage liest `v_startseite_termine` mit `security_invoker = true`. Serverseitig gelten aktiv, Sichtbarkeitsfenster, keine vergangenen Termintage und Vereinszeit `Europe/Berlin`; ein Termin bleibt während des ganzen lokalen Termintags sichtbar. Der Client führt keinen zweiten Datums- oder Sichtbarkeitsfilter aus.

Die View sortiert nach `datum`, `start_uhrzeit NULLS LAST`, `end_uhrzeit NULLS LAST`, `titel`, `id`.

### ICS

`CalendarEventData` und `IcsCalendarBuilder` bilden den gemeinsamen Core-Builder. Er erstellt stabile UIDs; All-Day-Termine nutzen `DTSTART;VALUE=DATE` und exklusives `DTEND` am Folgetag. Termine mit Startzeit verwenden `Europe/Berlin` und `VTIMEZONE`; Start ohne Ende ist zulässig. Textfelder werden ICS-konform escaped. Das Web lädt/öffnet, MAUI teilt/öffnet. Ein zusätzliches Kalender-Permissionrecht gibt es nicht.

## G9.5 – Bekanntmachungsverwaltung

Felder sind Titel, HTML-Inhalt, Sichtbar ab, Sichtbar bis und optionale ganzzahlige Sortierreihenfolge. Die Management-Sortierung ist:

```text
sort_order ASC NULLS LAST
sichtbar_ab DESC NULLS LAST
Titel ASC case-insensitive
id ASC
```

Wie bei Terminen gibt es keinen Harddelete, sondern Deaktivieren und Reaktivieren; normales Speichern erhält den Aktivstatus. Das Web verwendet einen Edit-Lock.

## G9.6 – Bekanntmachungsanzeige & HTML

`v_startseite_bekanntmachungen` verwendet `security_invoker = true`. Die Datenbank ist für aktiv, `sichtbar_ab`, `sichtbar_bis` und Reihenfolge authoritative; ein zweiter Client-Sichtbarkeitsfilter existiert nicht. Die Sortierung lautet `sort_order ASC NULLS LAST`, `created_at DESC NULLS LAST`, `id DESC`.

### HTML-Sicherheitsmodell

Rohes Bekanntmachungs-HTML bleibt in der Datenbank unverändert. Sanitizing erfolgt ausschließlich für Anzeige und Vorschau. Im Core arbeiten `HtmlContentHelper` und `HtmlSanitizer`, im Web DOMPurify, `SafeAnnouncementHtml` und `sanitizeAnnouncementHtml`.

Erlaubte Tags sind `p br strong b em i u s`, `h1 h2 h3 h4`, `ul ol li`, `blockquote`, `a`, `table thead tbody tr th td`, `hr`, `code pre`. Erlaubte Attribute sind `href`, `title`, `colspan`, `rowspan`. Extern erlaubte URI-Schemes sind `http`, `https`, `mailto`, `tel`; das Web akzeptiert zusätzlich interne `#fragment`-Links.

Nicht zulässig sind unter anderem `javascript:`, `data:`, `file:`, `vbscript:`, `script`, `iframe`, `img`, `form` und Eventhandler. CSP verhindert Scripts, Frames, Objekte, Formulare und externe Ressourcen.

### MAUI sichere Linknavigation

`SafeWebViewNavigation` erlaubt intern leer, `about:blank`, `about:blank#...` und `#fragment`. Extern sind nur `http`, `https`, `mailto` und `tel` zulässig. Andere, relative oder unsichere Ziele werden nicht geöffnet.

## G9.7 – MAUI-Parität

`BekanntmachungenUserState` ist ein eigener State. Bekanntmachungsdetails unterstützen wie Termine vorheriger/nächster Eintrag und `x/y`; die Homepage-Reihenfolge bleibt authoritative und wird beim Öffnen nicht neu sortiert. `HomeSectionDetailPage.DeleteAsync()` ist nur noch für Arbeitseinsätze vorgesehen. Die Legacy-Felder `SelectedAnnouncement`, `HasSelectedAnnouncement`, `ShowAnnouncementDetail` und `ShowAnnouncementHint` wurden entfernt. Termin- und Bekanntmachungsdetails haben keinen Harddelete-Pfad.

## G9.8 – Regression und technischer Prüfstand

Permission-Tests decken Bitwerte, Rollenmasken, Grant, Revocation und die Unabhängigkeit beider Rechte ab. HTML-Tests decken sichere URI-Schemes, `javascript`, `data`, `file`, `vbscript`, CSP, leeren Bekanntmachungs-Fallback, HTML-Encoding, `colspan` und `rowspan` ab. ICS-Tests prüfen All-Day-exklusives DTEND, Europe/Berlin, Start-only und End-only, Text- und Location-Escaping, stabile UID sowie deterministischen DTSTAMP.

Web-Prüfstand:

```text
npx tsc --noEmit → erfolgreich
npm run build → erfolgreich
gezielter G9-Lint ohne monolithische app/page.tsx: 16 Dateien, 0 Fehler
```

`app/page.tsx` besitzt 12 vorbestehende `react-hooks/set-state-in-effect`-Fehler; diese bestanden vor G9 und wurden bewusst nicht fachfremd bereinigt. Der vollständige Web-Lint meldet 20 Fehler und 20 Warnungen: 12 bekannte Fehler in `page.tsx` sowie 8 weitere außerhalb der G9-Dateiliste. Es gibt keine neuen G9-spezifischen Fehler.

`dotnet test`, Core-Build und MAUI-Android-Build konnten zuletzt wegen des Umgebungs-/Restore-Blockers `NU1301` (`api.nuget.org` nicht auflösbar) nicht vollständig laufen; dies ist kein bestandener Build. Die Supabase CLI ist lokal nicht installiert, DB-Verträge wurden daher statisch geprüft.

## G9.9 – Dokumentation und Controlled Merge

G9.1 bis G9.8 sind freigegeben. Die Dokumentation wird vor dem Merge abgeschlossen; `codex/g9` wird ausschließlich als Ganzes nach `main` übernommen, ohne Teilmerge. Vor einem Merge müssen aktuelle `main`-Basis, `behind_by = 0`, sauberer Working Tree, gepushter Doku-Commit und geprüfter finaler Diff bestätigt sein. Der tatsächliche Merge erfolgt erst nach unabhängiger Freigabe von G9.9a.

## G9.10 – Release

Nach vollständigem Merge nach `main` folgt: `main` aktualisieren, sauberen Stand prüfen, finale Builds/Prüfungen auf `main`, Versionsnummer festlegen oder erhöhen, gegebenenfalls Versionscommit, Tag, Tag-Push, GitHub Release, Release Notes, Artefakte und Kontrolle des veröffentlichten Releases. Noch ist keine Versionsnummer festgelegt; der letzte Release vor G9 bleibt `0.7.23`. G9.10 entscheidet die neue Version.
