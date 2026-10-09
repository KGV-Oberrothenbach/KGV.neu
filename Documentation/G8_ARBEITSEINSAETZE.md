# G8 – Arbeitseinsätze

## Ziel und Scope

G8 entwickelt die Arbeitseinsätze schrittweise weiter, ohne die bestehende Mitglieder-Startseite unnötig umzubauen. Die Zielarchitektur der Web-Verwaltung lautet: UI → Service → Repository → Supabase. Fachliche Änderungen werden jeweils nur in der dafür vorgesehenen Teilgruppe umgesetzt; G8 umfasst noch keine pauschale Neugestaltung von MAUI, Rollen oder Datenbankzugriffen.

## Teilgruppen

- **G8.1 – Web-Grundstruktur, Typen und Lesezugriffe:** Web-Verwaltung strukturell aus `app/page.tsx` herauslösen, gemeinsame Modelle zentralisieren und Verwaltungslesezugriffe hinter Service und Repository bringen.
- **G8.2 – Berechtigungen und Rollen:** Arbeitseinsatz-spezifische Permission und die dafür nötige Rollenbereinigung; noch nicht begonnen.
- **G8.3 – Editor-Vorgaben:** fachliche Standardwerte für Sichtbarkeit und Anmeldeschluss; noch nicht begonnen.
- **G8.4 – An- und Abmelderegeln:** Beginn-, Frist- und Kapazitätsregeln; noch nicht begonnen.
- **G8.5 – Teilnehmerverwaltung:** die derzeitige Verwaltungs-Altlogik fachlich ablösen; noch nicht begonnen.
- **G8.6 – Arbeitsstundenübergabe:** Teilnahme und Arbeitsstunden fachlich verknüpfen und gegen doppelte Übernahme schützen; noch nicht begonnen.
- **G8.7 – G8-Folgegruppe:** noch nicht begonnen; Detailumfang wird erst mit der zugehörigen Gruppenbeschreibung festgelegt.
- **G8.8 – G8-Folgegruppe:** noch nicht begonnen; Detailumfang wird erst mit der zugehörigen Gruppenbeschreibung festgelegt.
- **G8.9 – G8-Folgegruppe:** noch nicht begonnen; Detailumfang wird erst mit der zugehörigen Gruppenbeschreibung festgelegt.
- **G8.10 – G8-Folgegruppe:** noch nicht begonnen; Detailumfang wird erst mit der zugehörigen Gruppenbeschreibung festgelegt.

## Bereits beschlossene Fachregeln

- G8.1 führt keine Permission, Rollenmatrix, RLS-Änderung, Migration oder neue RPC ein.
- Die vorhandene Home-Funktion bleibt auf ihrem bestehenden Pfad Home UI → Home Service → Work-Assignment-Repository → Supabase/RPC.
- Neue Editor-Standards, An-/Abmeldefristen, Teilnehmerstatusregeln und Arbeitsstundenregeln werden nicht vor ihren jeweiligen Teilgruppen vorgezogen.
- MAUI wird in G8.1 nicht verändert.

## G8.1 – Ausgangszustand und Web-Struktur

Ausgangszustand war eine monolithische Verwaltungsansicht in `KGV.Web/app/page.tsx`, einschließlich lokaler Work-Assignment-Typen, Editor, Teilnehmeransicht und direkter lesender Supabase-Zugriffe.

G8.1 legt die Web-UI unter `KGV.Web/features/work-assignments/` ab und trennt `WorkAssignmentsManagement`, `WorkAssignmentList`, `WorkAssignmentEditor` und `WorkAssignmentParticipants`. Die gemeinsamen Verträge `WorkAssignment`, `WorkAssignmentRegistration` und `WorkAssignmentMember` liegen unter `KGV.Web/models/work-assignments/`. Verwaltungslesevorgänge folgen damit UI → Work-Assignment-Service → Work-Assignment-Repository → Supabase.

Bewusst verbliebene Altlogik sind die vorhandenen Schreibvorgänge für Einsätze und Anmeldungen sowie die bisherige Übernahme einer Teilnahme als Arbeitsstunde. Diese werden erst in G8.3 bis G8.6 fachlich überarbeitet. G8.2 ist noch nicht begonnen.
