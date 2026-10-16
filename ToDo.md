# ToDo – KGV-Software

Letzte Aktualisierung: 2026-10-16 (D3: gemeinsame Renderer-Migration abgeschlossen; D4 bleibt offen)

Diese Datei enthält ausschließlich offene, aktuelle Aufgaben. Erledigte Punkte, historische Abgleiche und frühere Zielbildtexte werden nicht fortgeführt.

## Pflege der ToDo

- Neue Punkte immer dem passenden Themenbereich zuordnen.
- Nach jedem erfolgreich abgeschlossenen Auftrag gruppieren, committen und auf den aktuellen Arbeitsbranch pushen.
- Keine erledigten Punkte in dieser Liste führen; bei Abschluss entfernen oder in die zugehörige Dokumentation bzw. Release-Notiz übernehmen.
- Punkte unter „manuell eingetragene Punkte“ fachlich eindeutig einem passenden Themenbereich zuordnen und anschließend den Sammelbereich nur für nicht eindeutig zuordenbare Punkte verwenden.

## WEB App

### Kopfbereich und Markenauftritt

- [ ] Das bisherige „K“-Logo durch das echte KGV-Logo ersetzen.

### Kommunikation

- [ ] Mailversand direkt aus der WEB-App ermöglichen.

### Kleine UI- und Kosmetikfehler

- [ ] Kleine sichtbare UI-Fehler (Beschriftungen, Abstände, Darstellung und ähnliche Auffälligkeiten) gesammelt aufnehmen und priorisiert bereinigen.
- [ ] Nur funktional relevante WEB-Warnungen und Restfehler nachziehen; keine unnötigen Großumbauten.
- [ ] Nach Auswahl eines Mitglieds den Bereich „Gärten“ im Web vollständig erreichbar halten.
- [ ] Nach Auswahl eines Mitglieds in der Web-Mitgliedersuche direkt zu dessen Stammdatenansicht wechseln.
- [ ] Im Web-MA-Signaturablauf die Zeichenfläche beim Wechsel zum nächsten Unterzeichner automatisch leeren, ohne bereits übernommene Signaturen zu verlieren.
- [ ] Übersicht der Wartungsverträge im Web als anklickbare Tabelle mit Titel, möglicher und tatsächlich vergebener Anzahl darstellen.

## PDF-Vorlagen – zentral in Supabase Storage

### Ziel

- [ ] Eingebettete PDF-Vorlagen aus dem Anwendungscode entfernen und zentral über einen privaten Supabase-Storage-Bucket bereitstellen. (Pachtvertrag: zentraler Pfad umgesetzt; lokaler Kompatibilitätsfallback bleibt bis zum produktiven Abnahmetest.)
- [ ] MAUI und WEB auf dieselben zentralen Vorlagen umstellen. (Pachtvertrag v2: Template, Field-Spec, SHA-256 und Aktivierungs-SQL liegen vor; Storage-Upload und Edge-Function-Deployment stehen noch aus.)
- [ ] Vorlagenwechsel ohne neue App-Version oder WEB-Deploy ermöglichen.
- [ ] Fertig erzeugte Dokumente weiterhin ausschließlich auf Google Drive speichern.

### Vorlagenarten

- [ ] Formulare: `mitgliedsantrag`, `pachtvertrag`, `protokoll`.
- [ ] Layoutvorlagen: `export_kopf` für Listen/Auswertungen/Exporte und `briefbogen` für Korrespondenz.
- [ ] Den bisherigen allgemeinen Briefkopf fachlich in `export_kopf` und `briefbogen` trennen.
- [ ] Briefbogen für Empfänger, Datum, Betreff und variablen Brieftext auslegen; spätere erste-/Folgeseiten-Vorlagen dürfen architektonisch nicht ausgeschlossen werden.

### Storage, Datenmodell und Versionierung

- [ ] Privaten Bucket `pdf-vorlagen` mit `formulare/<schluessel>/vN.pdf` und `layouts/<schluessel>/vN.pdf` einrichten.
- [ ] Tabelle `pdf_vorlage` anlegen: `id`, `schluessel`, `bezeichnung`, `typ`, `version`, `storage_path`, `aktiv`, `gueltig_ab`, `erstellt_am`, `erstellt_von`, `bemerkung`.
- [ ] Pro Schlüssel eindeutig eine aktive Version bestimmen.
- [ ] Vorlagen nie überschreiben; jede Änderung als neue Version ablegen.
- [ ] Bei Bedarf eine technische `schema_version` führen, um inkompatible Formular-/Layoutversionen zu erkennen; rein optische Änderungen bleiben ohne Codeänderung möglich.

### Gemeinsamer Zugriff und Migration

- [ ] Keine fachlich verwendeten lokalen PDF-Dateinamen oder Ressourcenpfade im Anwendungscode belassen.
- [ ] Bestehende Vorlagen vollständig erfassen, den Schlüsseln zuordnen und als Version 1 übernehmen.
- [ ] Aktive `pdf_vorlage`-Datensätze anlegen und bestehende PDF-Erzeugungen auf den zentralen Service umstellen.
- [ ] Eingebettete Vorlagendateien erst nach erfolgreicher, optisch und fachlich unveränderter Umstellung löschen.
- [ ] Kein dauerhafter Offline-Cache erforderlich; ein temporärer Sitzungscache ist zulässig.

### Abnahme

- [ ] Mitgliedsantrag, Pachtvertrag und Protokoll aus Supabase-Vorlagen erzeugen.
- [ ] Den Web-Mitgliedsantrag nach dem Edge-Function-Deployment mit zentralem Template-Katalog und allen drei Signaturfällen (3/5/5) praktisch testen.
- [ ] Exporte mit `export_kopf` erzeugen und einen eigenständigen `briefbogen` bereitstellen.
- [ ] Versionierung, Schutz alter Versionen und gemeinsamer Zugriff von MAUI und WEB nachweisen.

## Fachliche Resttests und Konsistenz

### Mitglieder, Parzellen und Dokumente

- [ ] Dokumente im Mitglieds- und Parzellenkontext praktisch gegenprüfen.
- [ ] Web-Dokumentupload gegen DDB praktisch abnehmen, einschließlich Demo-Scope, Edge-Diagnose und multipart Uploadvertrag.
- [ ] Google Drive ist ausschließlich serverseitige Dateiablage. Clients laden und öffnen Dokumente ausschließlich über authentifizierte Supabase-Endpunkte; direkte Google-Drive-Links sind unzulässig.
- [ ] Restinkonsistenzen in Stammdaten- und Parzellen-Navigation erfassen und beheben.
- [ ] Mitglieds-/Parzellenalltag testen: Navigation, Dokumente, Strom-/Wasser-Historie sowie Haupt-/Nebenmitgliedswechsel.
- [ ] Offenen Altbestand `mitglied.role` fachlich weiter beobachten und über einen späteren physischen Rückbau entscheiden.
- [ ] Wartungsverträge für Nebenmitglieder in WPF und MAUI einschließlich Pflichtstunden-Kontext vollständig prüfen.
- [ ] Pachtvertrag in MAUI und Web mit Datumsauswahl fachlich beenden können.
- [ ] WEB-Pachtvertrag im Parzellendetail entsprechend dem MAUI-Ablauf praktisch abnehmen: feste aktive Zuordnung, Vorschau, Einzelsignaturen und sichere Dokumentöffnung.
- [ ] Bei Kündigung der Mitgliedschaft Datum und bestehenden Pachtvertrag prüfen; erforderliche zweijährige Nachsorge einschließlich Strom, Wasser und Verwaltungspauschale (50 % Jahresbeitrag) fachlich abbilden.

### Arbeitsstunden

- [ ] Arbeitsstunden-Prüfprozess im Realbetrieb praktisch durchtesten.
- [ ] Arbeitsstunden-Freigaben bei realen Auffälligkeiten weiter absichern.

### MAUI und allgemeine UX

- [ ] MAUI-Realtests für Kamera, Foto, Upload, RFID, Ableseabläufe, Navigation, Rechte-/Rollenpfade, Home-Reload und Zurück-Taste durchführen.
- [ ] Allgemeine mobile UX anhand realer Auffälligkeiten glätten.

## Technische Qualität und Release

- [ ] Mitgliedsantrag auf Android praktisch abnehmen; die unterbrochene Signaturlinie beim Nebenmitglied als spätere Layout-/Kosmetikkorrektur behandeln.

- [ ] Funktional relevante technische Restwarnungen nachziehen, insbesondere in `SupabaseService`, `HomeManagementPage`, `ImpressumPage` und WPF-Verwaltungs-ViewModels.
- [ ] Google-Play-Test- und Release-Reife prüfen und fehlende Anforderungen ergänzen.
- [ ] Release-Manager und Release-Workflow mit realen Releases und Versionsflüssen verifizieren.
- [ ] Produktiv- und Testreife durch reale Installations-, Update- und Geräteprüfungen erhöhen.

## Aktueller Umsetzungsfokus

- [ ] Jahresabschluss in MAUI umsetzen; die detaillierte Planung liegt in `Documentation/MAUI_JAHRESABSCHLUSS_UMSETZUNGSPLAN.md`.
