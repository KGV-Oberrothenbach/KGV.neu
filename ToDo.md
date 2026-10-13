# ToDo / Zielbild – KGV-Software

Stand: 2026-04-07 nach Recovery-Abgleich gegen `origin/main`.

## 1. Bedeutung dieses Dokuments

Dieses Dokument ist das aktuelle **Zielbild** der KGV-Software.

Wenn künftig gesagt wird:
- „Prüfe das Zielbild gegen das Git-Repo“
- „Vergleiche den aktuellen Stand mit unserem Zielbild“
- „Was fehlt noch im Zielbild?“

dann ist damit genau diese Datei gemeint.

---

## 2. Verbindliche Arbeitsgrundlage

- Arbeitsgrundlage ist `copilot-instructions.md`
- gearbeitet wird auf `main`
- vor jeder Umsetzung zuerst den echten Repo-Stand gegen dieses Zielbild prüfen
- minimalinvasiv arbeiten
- WPF und MAUI fachlich möglichst gleichziehen
- Admins sollen mobil grundsätzlich alles können, was sie am PC können
- Save-Buttons immer am Ende von Eingabeformularen
- keine unnötigen Großumbauten
- keine Schattenlogik neben bestehenden Servicepfaden
- Save-/Reload-/Navigation-/Berechtigungslogik nicht unbeabsichtigt beschädigen
- untracked Dateien wie `.github/copilot-instructions.md`, `AWR.bat`, `_secrets/` und `ToDo.md` nur bewusst anfassen
- Logs immer pflegen:
  - `DEV_LOG.md`
  - `KGV_Fortschrittslog_ausfuehrlich.md`
- nach jedem Block:
  - bauen
  - Logs pflegen
  - committen
  - pushen
- Nach jeder Änderung an dieser `ToDo.md`:
  - Punkte nach Themen gruppieren
  - committen
  - pushen

### Dauerhafte Merge-Checkliste: Supabase-Migrationen

Vor Merge nach `main`: Supabase-Migrationen prüfen (Syntax, Klammerung, Reihenfolge/Abhängigkeiten, Idempotenz, keine unbeabsichtigten Datenänderungen bei Wiederholung und realen lokalen/Remote-Migrationsstand). Dabei `CREATE POLICY` mit geeignetem vorherigem `DROP POLICY IF EXISTS`, Trigger mit `DROP TRIGGER IF EXISTS`, Constraints mit Existenzprüfung bzw. vorherigem Entfernen, Tabellen/Spalten wo sinnvoll mit `IF NOT EXISTS` sowie Funktionen und Views möglichst mit `CREATE OR REPLACE` absichern.

Bei verknüpftem Supabase-Projekt vor dem Merge ausführen und bewerten:

```powershell
npx supabase migration list --linked
npx supabase db push --linked --dry-run
```

Erst nach `main` mergen, wenn keine unerwarteten Migrationen auftauchen, alle neuen Migrationen syntaktisch plausibel sind, keine bekannte nicht-idempotente Migration unkontrolliert erneut laufen würde und der Dry-Run erfolgreich ist. Ein produktiver `npx supabase db push --linked` ist nicht Teil dieser Checkliste und wird nur bewusst separat ausgeführt.

### Status-Legende
- `erledigt` = im Repo fachlich vorhanden und für diesen Punkt aktuell ausreichend abgeschlossen
- `teilweise` = Teilpfad umgesetzt, Restprüfung oder Folgearbeit offen
- `offen` = noch nicht ausreichend umgesetzt

---

## 2a. Zusätzliche Folgeprüfung 2026-04-06

Diese Folgeprüfung wurde nach zusätzlicher Nutzerpriorisierung gegen den echten Repo-Stand eingeordnet:

### Mitgliedersuche: Button `Mitglied neu anlegen`
**Status:** erledigt
- WPF-Mitgliedersuche hat den Button jetzt im Suchbereich
- MAUI-Mitgliedersuche hat den Button jetzt im Suchbereich
- beide Pfade öffnen einen minimalen produktiven Neuanlagepfad für neue Hauptmitglieder
- nach erfolgreicher Hauptmitglied-Neuanlage wird jetzt direkt gefragt, ob ein Nebenmitglied angelegt werden soll
- MAUI-Mitgliedersuche lädt nach erfolgreicher Neuanlage beim Rückweg wieder belastbar neu und unterstützt funktionierendes Pull-to-Refresh
- auf der MAUI-Seite `Nebenmitglied` erscheint bei fehlendem Nebenmitglied jetzt `Neu` und öffnet denselben Create-Pfad mit optionaler Adressübernahme

### Termin: Buttons `Bearbeiten` und `Löschen`
**Status:** erledigt
- Termin-Detailpfad liefert belastbar die nötige Termin-ID
- Bearbeiten-/Löschen-Pfade sind dadurch im MAUI-Detail verfügbar

### MAUI-Zurück-Taste mit Exit-Rückfrage
**Status:** erledigt
- Unterseiten und Detailseiten navigieren jetzt schrittweise Ebene für Ebene zurück
- modale MAUI-Seiten werden über System-Zurück zuerst geschlossen
- wenn kein sinnvoller Rückpfad mehr existiert, erfolgt der Fallback zur Startseite
- auf der Startseite erscheint die Rückfrage zum App-Beenden

### Mitgliedschaft beenden mit Folgeentscheid
**Status:** erledigt
- Hauptmitglied kann jetzt über den bestehenden Stammdatenpfad beendet werden
- bei vorhandenem Nebenmitglied wird der Folgeentscheid abgefragt:
  - Nebenmitglied ebenfalls beenden
  - oder Nebenmitglied zum Hauptmitglied machen
- WPF und MAUI laufen dafür auf demselben Servicepfad

### MAUI: Nebenmitglied-Bearbeiten für Bestandsdatensatz vervollständigt
**Status:** erledigt
- der bestehende Bearbeiten-Pfad lädt und speichert jetzt zusätzlich:
  - `E-Mail-Adresse`
  - `Geburtsdatum`
  - `Beginn / MitgliedSeit`
  - `WhatsApp-Einwilligung`
- der Shared-Servicepfad respektiert dabei weiterhin die Auth-Mail-Regel:
  - mit vorhandenem `auth_user` bleibt die Mailadresse führend unverändert
  - ohne `auth_user` darf die Mailadresse über diesen Pfad geändert werden

---

## 3. Zielbild – Repo-Abgleich

### 1. MAUI: Termin komplett löschen
**Status:** erledigt
- Sicherheitsabfrage vorhanden
- echtes Löschen vorhanden
- Rückkehr/Aktualisierung der Übersicht vorhanden
- Termin-Detailpfad liefert jetzt auch belastbar die Buttons für `Bearbeiten` und `Löschen`

### 2. MAUI: Bekanntmachung komplett löschen
**Status:** erledigt
- Sicherheitsabfrage vorhanden
- echtes Löschen vorhanden
- Rückkehr/Aktualisierung der Übersicht vorhanden

### 3. Startseite automatisch neu laden / gegen Iststand feinjustieren
**Status:** erledigt
- MAUI lädt Startseite bei `OnAppearing` und Kontextwechsel neu
- WPF lädt Home beim Navigieren in die Startseite neu
- manuelles Aktualisieren bleibt erhalten

### 4. MAUI-Zurück-Taste glätten
**Status:** erledigt
- Unterseiten navigieren zuerst zur Startseite zurück
- auf der Startseite erscheint jetzt die Rückfrage zum App-Beenden statt eines direkten Exits

### 5. WPF-Bindingfehler `MemberDTO.Name`
**Status:** erledigt
- betroffener realer Bindingpfad wurde auf echte Properties korrigiert

### 6. Weitere sichtbare UI-Restfehler beobachten
**Status:** offen
- nur funktional relevante Warnungen nachziehen
- keine unnötige Großbaustelle aus allgemeinen Warnungen machen

### 7. Live-Verifikation benutzerspezifischer Fachrechte weiter absichern
**Status:** erledigt
- Save-/Reload-Pfade, `permission_grants`, `permission_revocations`, `updated_at` und Anzeige des verknüpften App-Users wurden im Repo bereits nachgezogen

### 8. `mitglied.role` als Altbestand bewusst beobachten
**Status:** teilweise
- fachlich nicht mehr führend
- im Code als Altbestand kenntlich gemacht
- physischer Drop bleibt bewusst offen

### 9. Rechte-/Freigabemodell weiter fachlich glätten
**Status:** erledigt
- Eigenkontext und globale Rechtepfade wurden im aktuellen Repo bereits klarer getrennt

### 10. Eigene Zählerablesung für normale Nutzer mit Freigabeprozess
**Status:** erledigt
- Freigabepfad und Admin-Schalter sind im Repo berücksichtigt

### 11. Ablesen / Zählerwechsel / Freigaben weiter fachlich absichern
**Status:** erledigt
- Einstiegspfade und Rechte sind fachlich klarer getrennt

### 12. Arbeitsstunden-Prüfprozess praktisch durchtesten
**Status:** teilweise
- Prüfpfade, Verlauf und mobile Sperre sind umgesetzt
- praktische Realbetriebsprüfung bleibt offen

### 13. Arbeitsstunden-Freigaben weiter beobachten
**Status:** offen
- nur bei echtem Realfehler nachziehen

### 14. Dokumente im Parzellen-/Mitgliedskontext weiter gegenprüfen
**Status:** teilweise
- View-only-/Schreibrechte wurden in WPF und MAUI weiter geglättet
- weitere Praxisgegenprüfung bleibt offen

### 15. Restinkonsistenzen in Stammdaten-/Parzellen-Navigation beobachten
**Status:** teilweise
- einzelne MAUI-Kontext-/Sichtbarkeitslücken wurden geschlossen
- weitere Auffälligkeiten bleiben zu beobachten

### 16. Parzellen-/Mitgliedskontext im Alltag weiter testen
**Status:** offen
- Navigation
- Dokumente
- Strom/Wasser-Historie
- Haupt-/Nebenmitgliedswechsel
- Web-Regressionsfix Mitglied → Gärten → Ablesen abgeschlossen: Mitglieds- und Parzellenkontext bleiben aktiv; G6-Historie ist auf die konkrete `parzellen_belegung` von `von_datum` bis einschließlich `bis_datum` begrenzt. Es werden nur vorhandene Ablesungen angezeigt, keine Pachtbeginn-Ablesung synthetisch erzeugt.

### 17. MAUI-Realtests weiterführen
**Status:** offen
- Kamera
- Foto
- Upload
- RFID
- Ableseabläufe
- Navigation
- Rechte-/Rollenpfade
- Home-Reload
- Zurück-Taste

### 18. Allgemeine mobile UX glätten
**Status:** offen
- nur echte Auffälligkeiten nachziehen
- keine unnötigen Großumbauten

### 19. Google Play Test-/Release-Reife weiter absichern
**Status:** offen
- prüfen, was noch fehlt, damit Google Play die App testen kann

### 20. Release-Manager / Release-Workflow weiter praktisch prüfen
**Status:** offen
- reale Releases und Versionsfluss weiter verifizieren

### 21. Produktiv- und Testreife weiter erhöhen
**Status:** offen
- Store-/Testeranforderungen
- reale Installations-/Updatepfade
- reale Geräteprüfungen

### 22. Wartungsverträge dürfen auch Nebenmitglieder haben
**Status:** teilweise
- Folgeblock begonnen
- mitgliedsbezogene Wartungsvertrags-Servicepfade werden nicht mehr pauschal auf das Hauptmitglied normalisiert
- MAUI hat im Mitgliedskontext jetzt auch einen direkten Einstieg in die mitgliedsbezogenen Wartungsverträge
- Restprüfung für Gesamtverhalten in WPF/MAUI und Pflichtstunden-Kontext bleibt offen

### 23. Terminserie / Mehrschicht-Funktion für Arbeitseinsätze
**Status:** erledigt
- mehrere Zeitfenster / Schichten minimalinvasiv unterstützen
- Beispiel:
  - 12–14 Uhr
  - 14–16 Uhr
  - unterschiedliche Teilnehmerzahlen
- umgesetzt über das bestehende Einzelslot-Modell: mehrere Schichten werden als eigene Arbeitseinsätze erfasst
- `Speichern + nächste Schicht` ist in WPF und MAUI vorhanden und befüllt die Folgeschicht direkt vor

### 24. Technische Restpunkte / Warnungen
**Status:** offen
Nur anfassen, wenn funktional relevant oder im jeweiligen Block direkt betroffen:
- `KGV.Infrastructure/Services/SupabaseService.cs`
- `KGV.Maui/Pages/HomeManagementPage.cs`
- `KGV.Maui/Pages/ImpressumPage.cs`
- WPF-Verwaltungs-ViewModels
- sonstige bekannte Warnungen

---

## 3a. WEB App – offene Punkte

### Navigation
- Saison-Auswahl in der Navigationsleiste: Dropdown ohne Funktion (durch den WEB-F1-Saison-Fix behoben; Details siehe erledigten Punkt unten).

### Saison-Auswahl in der Navigationsleiste
**Status:** erledigt
- Der `SeasonPicker` und der persistierte Workspace-Kontext waren bereits korrekt verdrahtet und bleiben unverändert.
- Tatsächliche Ursache: Die globale Arbeitsstunden-Freigabe hat die zentrale `saisonId` nicht an ihre Repository-Abfrage übergeben und zeigte daher Einträge aus allen Saisons an.
- Zusätzlich haben saisonbezogene Exportfilter bisher das aktuelle Kalenderjahr statt der ausgewählten Workspace-Saison vorbelegt.
- Beide Bereiche verwenden nun die zentrale Saisonwahl und laden bei einem Wechsel erneut; Mitglieds- und Parzellenkontexte bleiben unverändert erhalten.
- Beim neuen App-Start hat die aktuelle Kalender-Saison nun Vorrang vor einer zuvor gespeicherten manuellen Auswahl. Innerhalb der laufenden Sitzung bleibt die manuelle Saisonwahl weiterhin wirksam.

### Impressum
**Status:** erledigt
- Der frühere offene Punkt „Vorstand und Bauausschuss sind nicht verknüpft“ ist durch den Impressum-Fix behoben.
- Tatsächliche Ursache: Der zentrale Web-Demo-Filter behandelte `impressum_funktion_slot` wie eine Tabelle mit eigenem `is_demo`-Feld. Die Tabelle wird jedoch über ihre `mitglied_id` per RLS gescoped und besitzt diese Spalte nicht; dadurch konnte die Slot-Abfrage fehlschlagen, bevor die vorhandene Zuordnung zu Mitgliedern aufgelöst wurde.
- Lösung: Der Slot-Request bleibt dem Datenbank-RLS überlassen. Die verknüpften Mitglieder werden weiterhin über ihren eigenen `is_demo`-Scope geladen; der bestehende Pfad Slot → `mitglied_id` → Mitglied → Vorstand/Bauausschuss bleibt unverändert.

### Mitglieder
- **(erledigt)** Mitglied → Dokumente: Vorhandene Dokumente werden nicht angezeigt.
  - Ursache: `dokument` besitzt kein eigenes `is_demo`-Feld, war aber in `demoScopedTables`; `readSupabase()` ergänzte einen ungültigen clientseitigen Filter und die Mitglieder-Dokumentabfrage schlug fehl.
  - Lösung: `dokument` aus `demoScopedTables` entfernt. Demo-/Produktivscope bleibt der RLS über Mitglieds-/Parzellen-Owner überlassen. Mitglieds- und Parzellen-Dokumente bleiben getrennte Owner-Pfade; aktive Mitgliedsdokumente werden weiterhin nach `mitglied_id` und `archiviert_at is null` geladen.
- Mitglied → Gärten → Ablesen **(erledigt)**: Ursache war der Wechsel in den globalen Ablesemodus, wodurch Mitglieds- und Belegungskontext verloren gingen. Die Historie bleibt jetzt beim aktiven Mitglied und der ausgewählten Parzelle und wird für die konkrete `parzellen_belegung` anhand `von_datum`/`bis_datum` gefiltert. Es wird keine synthetische Pachtbeginn-Ablesung erzeugt.

### Arbeitsstunden
- **(erledigt)** Seite Arbeitsstunden: Keine Daten sichtbar, obwohl Daten erfasst sind.
  - Ursache: `arbeitsstunde` hatte keinen eigenen `is_demo`-Parameter im Schema, stand aber in `demoScopedTables`; dadurch ergänzte `readSupabase()` einen ungültigen clientseitigen Filter.
  - Lösung: `arbeitsstunde` aus `demoScopedTables` entfernt. Der Demo-/Produktiv-Scope bleibt über die RLS anhand `mitglied_id` erhalten; die Mitglieds- und Saisonfilter der Ansicht bleiben unverändert.
- **(erledigt) Technischer G7/G8-Folgepunkt:** Die letzte wirksame Arbeitsstunden-UPDATE-Triggerfunktion aus `20261009220000_g8_work_assignment_work_hours.sql` verglich `NEW.is_demo`/`OLD.is_demo`, obwohl `arbeitsstunde` diese Spalte nicht besitzt. Die neue Forward-Migration `20261011114000_g7_fix_work_hour_update_trigger.sql` ersetzt nur die Funktion und entfernt ausschließlich diesen ungültigen Vergleich; Mitglieds-/Saison-/Einsatzzuordnungs- und Lock-Schutz bleiben bestehen. Keine Tabellenspalte oder RLS wurde geändert.

### Mailversand
- Mailversand direkt aus der WEB-App ermöglichen.

---

## 3b. Plattformübergreifend – PDF-Vorlagen zentral in Supabase Storage

**Status:** offen

### Ziel
- Eingebettete PDF-Vorlagen aus dem Anwendungscode entfernen und zentral über Supabase Storage verwalten.
- MAUI und WEB verwenden dieselben zentralen Vorlagen.
- Änderungen an Vorlagen sollen ohne neue App-Version und ohne neuen WEB-Deploy möglich sein.
- Supabase Storage dient ausschließlich der Vorlagenbereitstellung; fertig erzeugte Dokumente bleiben wie bisher auf Google Drive.

### Vorlagentypen und fachliche Trennung
- Formulare:
  - `mitgliedsantrag`
  - `pachtvertrag`
  - `protokoll`
- Layoutvorlagen:
  - `export_kopf` für Listen, Auswertungen und sonstige Exporte (kompakter Vereinskopf mit Logo und erforderlichen Vereinsangaben)
  - `briefbogen` für Korrespondenz mit Mitgliedern (Vereinslogo/-daten, Absenderzeile, Empfängeranschrift, Datum, Betreff, Brieftext und bei Bedarf Fußbereich)
- Der bisherige allgemeine Briefkopf wird fachlich in `export_kopf` und `briefbogen` getrennt.

### Privater Supabase-Storage-Bucket und Versionierung
- Privaten Bucket `pdf-vorlagen` einrichten.
- Zielstruktur:
  ```text
  pdf-vorlagen/
  ├── formulare/
  │   ├── mitgliedsantrag/v1.pdf
  │   ├── pachtvertrag/v1.pdf
  │   └── protokoll/v1.pdf
  └── layouts/
      ├── export_kopf/v1.pdf
      └── briefbogen/v1.pdf
  ```
- Jede Vorlagenänderung wird als neue Version abgelegt (z. B. `v2.pdf`, `v3.pdf`); bereits verwendete Versionen dürfen nie überschrieben werden.

### Zentrale Vorlagenverwaltung
- Tabelle `pdf_vorlage` für die verfügbaren Vorlagen anlegen.
- Mindestens abbilden: `id`, `schluessel`, `bezeichnung`, `typ`, `version`, `storage_path`, `aktiv`, `gueltig_ab`, `erstellt_am`, `erstellt_von`, `bemerkung`.
- `typ` unterscheidet mindestens `formular` und `layout`.
- Pro `schluessel` muss genau bestimmbar sein, welche Version aktiv ist.
- Bei Bedarf technische `schema_version` ergänzen, damit inkompatible Formular-/Layoutversionen erkannt werden (z. B. `pachtvertrag-v2`). Rein optische Vorlagenänderungen bleiben ohne Codeänderung möglich.

### Zugriff aus MAUI und WEB
- Fachlicher Zugriff ausschließlich über stabile Vorlagenschlüssel; keine fachlich verwendeten Dateinamen oder lokalen Ressourcenpfade im Anwendungscode.
- Zentralen `PdfTemplateService` vorsehen, der:
  1. den Vorlagenschlüssel entgegennimmt,
  2. die aktive Version aus Supabase ermittelt,
  3. die Datei aus dem privaten Storage lädt,
  4. sie der PDF-Erzeugung bereitstellt und
  5. bei fehlender oder ungültiger Vorlage einen verständlichen Fehler liefert.
- Ein dauerhafter Offline-Cache ist nicht erforderlich; ein technischer, temporärer Sitzungscache ist zulässig.

### Briefbogen
- Briefbogen als variable Layoutgrundlage auslegen: Briefbogen + Empfänger + Datum + Betreff + variabler Brieftext = fertiger Brief.
- Perspektivisch für Drucken und PDF-Erzeugung geeignet halten; erzeugte PDFs können weiterhin in Google Drive gespeichert und später per E-Mail versandt werden.
- Die Architektur darf eine spätere Unterscheidung zwischen `briefbogen_seite1` und `briefbogen_folgeseite` für mehrseitige Schreiben nicht verhindern.

### Migration
1. Alle derzeit im Code vorhandenen PDF-Vorlagen erfassen und den neuen Schlüsseln zuordnen.
2. Aktuelle Dateien als Version 1 in den privaten Supabase-Storage übernehmen.
3. Datensätze in `pdf_vorlage` anlegen und aktive Versionen festlegen.
4. Bestehende PDF-Erzeugungen auf den `PdfTemplateService` umstellen.
5. Lokale Vorlagenreferenzen entfernen; eingebettete Vorlagendateien erst nach erfolgreicher Umstellung aus dem Projekt löschen.
6. Die bestehende PDF-Erzeugung darf durch die reine Verlagerung optisch und fachlich zunächst unverändert bleiben.

### Nicht Bestandteil dieses Umbaus
- Kein grafischer Vorlageneditor in der Anwendung.
- Keine Upload-/Versionsverwaltung durch normale Vereinsmitglieder.
- Keine Offline-Verfügbarkeit der Vorlagen.
- Keine Migration fertiger Dokumente von Google Drive nach Supabase.
- Kein vollständiger E-Mail-Versand von Briefen und keine frei editierbaren PDF-Formulare durch Benutzer.

### Abnahmekriterien
- Mitgliedsantrag, Pachtvertrag und Protokoll werden aus Supabase-Vorlagen erzeugt.
- Exporte verwenden `export_kopf`; ein eigenständiger `briefbogen` ist vorhanden.
- MAUI und WEB greifen auf dieselben zentralen Vorlagen zu.
- Keine fachlich verwendete PDF-Vorlage ist mehr fest im Anwendungscode eingebettet.
- Vorlagen sind versionierbar, alte Versionen werden nicht überschrieben und ein Vorlagenwechsel ist ohne neue App-Version möglich, sofern die technische Befülllogik kompatibel bleibt.
- Fertige Dokumente werden weiterhin auf Google Drive gespeichert.

---

## 4. Nächster sinnvoller Umsetzungsblock

Aktueller Block:
- **Jahresabschluss in MAUI**

Die verbindliche, detaillierte Planung liegt in
[`Documentation/MAUI_JAHRESABSCHLUSS_UMSETZUNGSPLAN.md`](Documentation/MAUI_JAHRESABSCHLUSS_UMSETZUNGSPLAN.md).

Begonnen wird mit der verbindlichen Festlegung der dort genannten Geschäftsregeln, danach folgen Datenmodell, gemeinsamer Servicepfad und MAUI-Oberflächen. Die alte Browser-Umsetzungsplanung wurde entfernt; eine Browser-Umsetzung ist erst nach dem stabilen MAUI-Fachablauf wieder abzuleiten.

---

## 5. Definition „Zielbild“

Wenn in diesem Projekt künftig gesagt wird:
- „Prüfe das Zielbild gegen das Git-Repo“
- „Vergleiche den aktuellen Stand mit unserem Zielbild“
- „Was fehlt noch im Zielbild?“

dann ist damit genau dieses Dokument gemeint.
