# NB-2A Mitgliedsnummer – Web-Audit

Stand: Audit auf Commit `4a1afbf8e571334a055f6f505bca699da91ccfa2`. Gegenstand ist ausschließlich `KGV.Web`; es wurden keine fachlichen Anzeigen, Modelle oder Abfragen geändert.

## Bereits erledigt

Der vorhandene Teilstand aus Commit `4a1afbf8e571334a055f6f505bca699da91ccfa2` hat:

- das Web-Mitgliedsmodell um `mitgliedsnummer` ergänzt,
- Mitgliedsdatenabfragen um die Mitgliedsnummer erweitert,
- die Mitgliedersuche auf die Mitgliedsnummer ausgerichtet und
- die Ergebnistabelle der Mitgliedersuche um die Mitgliedsnummer ergänzt; die technische ID ist dort kein fachliches Such- oder Ausgabefeld mehr.

## User-facing Treffer

Alle folgenden Treffer sind Kategorie **B**: technische Mitglieds-IDs erscheinen direkt oder als Fallback in einer für Nutzende sichtbaren Anzeige und müssen durch die Mitgliedsnummer ersetzt werden. Für die Darstellung sollte eine einheitliche Form festgelegt werden, beispielsweise `Mitgliedsnummer 1001`; bei vorhandenem Namen bleibt der Name die Primäranzeige.

| Nr. | Datei | Stelle/Funktion | aktuelles Verhalten | benötigte Änderung | Abhängigkeit |
|---:|---|---|---|---|---|
| 1 | `KGV.Web/app/page.tsx` | Workspace-Kopf / `selectedMemberLabel` | Fallback `Mitglied #<id>` für den gewählten Mitgliedskontext | Fallback mit Mitgliedsnummer ausgeben | Workspace-Abfrage muss die Mitgliedsnummer liefern |
| 2 | `KGV.Web/app/page.tsx` | Wartungsverträge / `memberName` | Fallback für Zuordnungen zeigt `Mitglied #<id>` | Mitgliedsnummer statt ID anzeigen | Wartungsdaten benötigen Mitgliedsnummer oder Namensauflösung |
| 3 | `KGV.Web/app/page.tsx` | Benutzer und Rechte | Zeile der Mitgliedstabelle zeigt `#<member.id>` | Fachliche Mitgliedsnummer anzeigen | Mitgliedsliste für Rechteverwaltung |
| 4 | `KGV.Web/app/parcel-workspaces.tsx` | `memberName` und Belegungs-Auswahl | Namensfallback und Auswahloption zeigen technische IDs | Mitgliedsnummer verwenden | Parzellen-Mitgliedsliste |
| 5 | `KGV.Web/features/members/MemberStammdaten.tsx` | Stammdaten-Kopf | Zeigt `Mitglied #<member.id>` | Mitgliedsnummer als Stammdatenkennung ausgeben | Geladenes Mitglied |
| 6 | `KGV.Web/features/members/SecondaryMember.tsx` | Nebenmitglied-Ansicht | Zeigt `Mitglied #<secondary.id>` | Mitgliedsnummer des Nebenmitglieds ausgeben | Nebenmitglied-Abfrage |
| 7 | `KGV.Web/features/members/MembershipEnd.tsx` | Beendigungsdialog | Namensfallbacks enthalten Haupt-/Nebenmitglied-ID | Mitgliedsnummern verwenden | Haupt- und Nebenmitgliedsdaten |
| 8 | `KGV.Web/features/work-hours/WorkHoursReview.tsx` | Prüfung offener Arbeitsstunden und Verlauf | Tabelle, Detail und Verlauf zeigen `mitglied_id` bzw. `geprueft_von` | Mitgliedsnummern bzw. Mitgliedsnamen mit Mitgliedsnummer anzeigen | Prüf- und Verlaufsabfragen benötigen Auflösung der IDs |
| 9 | `KGV.Web/features/work-hours/MemberWorkHours.tsx` | eigener Prüfverlauf | `geprueft_von` wird als `Mitglied #<id>` angezeigt | Prüfer als Mitgliedsnummer/Name ausgeben | Verlaufsabfrage benötigt Prüferauflösung |
| 10 | `KGV.Web/features/work-assignments/WorkAssignmentParticipants.tsx` | Teilnehmerliste | Namensfallback `Mitglied #<id>` | Mitgliedsnummer verwenden | Teilnehmer-Mitgliedsliste |
| 11 | `KGV.Web/features/maintenance/MemberMaintenanceContracts.tsx` | Wartungsverträge eines Mitglieds | Fallbacks für Mitglied und Hauptmitglied enthalten IDs | Mitgliedsnummern verwenden | Wartungsvertragsdaten für Mitglied/Hauptmitglied |
| 12 | `KGV.Web/features/maintenance/ExpiringMaintenanceContracts.tsx` | auslaufende Wartungsverträge | Fallback `Mitglied #<hauptmitglied_id>` | Mitgliedsnummer ausgeben | Übersichtsdaten müssen sie bereitstellen |
| 13 | `KGV.Web/features/maintenance/assignments/AssignMembersToContract.tsx` | Fehlermeldung beim Zuweisen | Fehlerfallback enthält `targetId` | Mitgliedsnummer verwenden | Kandidaten-/Fehlerdaten zuordnen |
| 14 | `KGV.Web/features/contracts/LeaseContractFlow.tsx` | Unterzeichner-/Mitgliedslabel | Namensfallback enthält technische ID | Mitgliedsnummer verwenden | Vertrags-Mitgliedsdaten |
| 15 | `KGV.Web/services/home/home-service.ts` | `loadHomeAssignmentParticipants` | Teilnehmer-`displayName` fällt auf `Mitglied #<id>` zurück | Mitgliedsnummer verwenden | Namensliste muss Mitgliedsnummer enthalten |
| 16 | `KGV.Web/services/maintenance/maintenance-assignment-service.ts` | `displayName` für Wartungszuordnung | Namensfallback enthält technische ID | Mitgliedsnummer verwenden | Wartungs-Kandidatenliste |
| 17 | `KGV.Web/services/contracts/legal-representative-service.ts` | Auswahl gesetzlicher Vertreter | Optionslabel fällt auf technische ID zurück | Mitgliedsnummer verwenden | Mitglieder-Suchliste |
| 18 | `KGV.Web/services/parcels/parcel-service.ts` | Pächterlabel | Namens-/E-Mail-Fallback ist `Mitglied #<id>` | Mitgliedsnummer verwenden | Parzellen-Mitgliedsliste |
| 19 | `KGV.Web/services/communication/communication-service.ts` | Kommunikations-Mitgliedslabel | Namensfallback enthält technische ID | Mitgliedsnummer verwenden | Kommunikationsabfrage mit Mitglied |
| 20 | `KGV.Web/services/club-functions/club-function-service.ts` | Vereinsfunktions-Besetzung | Label zeigt ohne E-Mail `#<member.id>` | Mitgliedsnummer verwenden | Vereinsfunktions-Mitgliedsliste |

## Technische Treffer, die bleiben dürfen

Kategorie **A**. Diese Verwendungen sind keine fachlichen Ausgaben und sollen im Rahmen von NB-2A nicht geändert werden.

- **Foreign Keys und Persistenz:** Felder wie `mitglied_id`, `hauptmitglied_id`, `vorstand_mitglied_id` und `begleitperson_mitglied_id` in Datensätzen und Payloads bleiben technische Schlüssel.
- **API-Parameter und Datenzugriff:** `memberId`/`mitgliedId` in REST-Filtern, RPC-Parametern, Service-Signaturen und Repository-Aufrufen bleiben technische Identitäten.
- **Berechtigungslogik und Kontext:** Vergleiche wie `selectedMemberId === context.mitgliedId`, Prüfberechtigungen und mandantenbezogener Mitgliedskontext bleiben unverändert.
- **Routing, React-Schlüssel und Zustandsverwaltung:** Navigation zu einem Mitglied, `key`-Attribute, Selection-State und Lookup-Maps verwenden weiterhin die technische ID.
- **Vertrags- und Signaturworkflow:** Persistente Signatur-Slots, Zuordnungen und Entwurfsvalidierung referenzieren das Mitglied technisch über `memberId`/`mitglied_id`.
- **Diagnose, Speicher- und Testschlüssel:** lokale Storage-Keys, Fehlerkorrelation, Testdaten und technische App-Benutzer-IDs bleiben unverändert, sofern sie nicht in einer Fachanzeige erscheinen.

## Unklare Treffer

1. **Datenverfügbarkeit in projektspezifischen Abfragen:** `mitgliedsnummer` ist im Webmodell optional. Vor jedem Block muss geprüft werden, welche der verwendeten Spezialabfragen/Projektionen die Nummer bereits liefern und welche sie ergänzen müssen. Die Anzeige darf nicht still auf die technische ID zurückfallen.

Der Darstellungsstandard ist festgelegt: `Mgl.-Nr. <nummer>`; bei vorhandenem Namen `<Name> · Mgl.-Nr. <nummer>`.

## Implementierungsblöcke

### NB-2A-W1 – Mitgliedsstammdaten und Mitgliedskontext

Status: **erledigt** (Commit folgt diesem Arbeitsblock).

Dateien:

- `KGV.Web/app/page.tsx`
- `KGV.Web/features/members/MemberStammdaten.tsx`
- `KGV.Web/features/members/SecondaryMember.tsx`
- `KGV.Web/features/members/MembershipEnd.tsx`

Stellt die Nummer im Stammdatenbereich, im ausgewählten Workspace-Kontext und in Nebenmitglied-/Beendigungsdialogen fachlich korrekt dar.

### NB-2A-W2 – Parzellen und Rechteverwaltung

Status: **erledigt** (Commit folgt diesem Arbeitsblock).

Dateien:

- `KGV.Web/app/parcel-workspaces.tsx`
- `KGV.Web/services/parcels/parcel-service.ts`
- `KGV.Web/app/page.tsx` (Rechteverwaltung)
- `KGV.Web/models/members/member.ts` (nur falls eine fehlende Projektionsform ergänzt werden muss)

Ersetzt sichtbare IDs in Belegungs- und Pächterlabels sowie in der Rechteverwaltung. Technische Zuordnungswerte bleiben IDs.

### NB-2A-W3 – Arbeitsstunden und Arbeitseinsätze

Status: **erledigt** (Commit folgt diesem Arbeitsblock).

Dateien:

- `KGV.Web/features/work-hours/WorkHoursReview.tsx`
- `KGV.Web/features/work-hours/MemberWorkHours.tsx`
- `KGV.Web/features/work-assignments/WorkAssignmentParticipants.tsx`
- `KGV.Web/services/home/home-service.ts`

Führt eine fachliche Mitgliedsauflösung für Prüfungen, Prüfverläufe und Teilnehmerlisten ein. Besonders die Prüfer-ID im Verlauf ist separat testbar.

### NB-2A-W4 – Wartungsverträge und Vertragsauswahlen

Status: **erledigt** (Commit folgt diesem Arbeitsblock).

Dateien:

- `KGV.Web/features/maintenance/MemberMaintenanceContracts.tsx`
- `KGV.Web/features/maintenance/ExpiringMaintenanceContracts.tsx`
- `KGV.Web/features/maintenance/assignments/AssignMembersToContract.tsx`
- `KGV.Web/services/maintenance/maintenance-assignment-service.ts`
- `KGV.Web/features/contracts/LeaseContractFlow.tsx`
- `KGV.Web/services/contracts/legal-representative-service.ts`

Vereinheitlicht Mitgliedslabels für Wartungsübersichten, Zuweisungsfehler, Vertragsunterzeichner und Vertreterauswahl.

### NB-2A-W5 – Kommunikation und Vereinsfunktionen

Dateien:

- `KGV.Web/services/communication/communication-service.ts`
- `KGV.Web/services/club-functions/club-function-service.ts`
- `KGV.Web/features/club-functions/ClubFunctionManagement.tsx`

Ersetzt technische Fallbacks in Kommunikations- und Vereinsfunktionsanzeigen; die Auswahlwerte und Datenbankreferenzen bleiben technisch.

Jeder Block ist nach Prüfung der genannten Datenverfügbarkeit unabhängig implementierbar, in einem normalen CX-Turn testbar und mit eigenem Commit und Push abschließbar.
