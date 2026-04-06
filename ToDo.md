# ToDo / Zielbild – KGV-Software

Stand: 2026-04-06 nach Repo-Abgleich gegen `main`.

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

### Status-Legende
- `erledigt` = im Repo fachlich vorhanden und für diesen Punkt aktuell ausreichend abgeschlossen
- `teilweise` = Teilpfad umgesetzt, Restprüfung oder Folgearbeit offen
- `offen` = noch nicht ausreichend umgesetzt

---

## 3. Zielbild – Repo-Abgleich

### 1. MAUI: Termin komplett löschen
**Status:** erledigt
- Sicherheitsabfrage vorhanden
- echtes Löschen vorhanden
- Rückkehr/Aktualisierung der Übersicht vorhanden

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
- erst auf der Startseite greift das normale App-Back-Verhalten

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
**Status:** offen
- mehrere Zeitfenster / Schichten minimalinvasiv unterstützen
- Beispiel:
  - 12–14 Uhr
  - 14–16 Uhr
  - unterschiedliche Teilnehmerzahlen

### 24. Technische Restpunkte / Warnungen
**Status:** offen
Nur anfassen, wenn funktional relevant oder im jeweiligen Block direkt betroffen:
- `KGV.Infrastructure/Services/SupabaseService.cs`
- `KGV.Maui/Pages/HomeManagementPage.cs`
- `KGV.Maui/Pages/ImpressumPage.cs`
- WPF-Verwaltungs-ViewModels
- sonstige bekannte Warnungen

---

## 4. Nächster sinnvoller Umsetzungsblock

Aktueller Block:
- **22. Wartungsverträge dürfen auch Nebenmitglieder haben**

Ziel dieses begonnenen Folgeblocks:
- Nebenmitglieder sollen nicht nur im UI erscheinen, sondern fachlich belastbar eigene Wartungsvertragszuordnungen tragen können
- WPF und MAUI sollen dabei auf denselben Servicepfaden laufen
- Pflichtstunden-/Mitgliedskontext darf dadurch nicht unbeabsichtigt beschädigt werden

---

## 5. Definition „Zielbild“

Wenn in diesem Projekt künftig gesagt wird:
- „Prüfe das Zielbild gegen das Git-Repo“
- „Vergleiche den aktuellen Stand mit unserem Zielbild“
- „Was fehlt noch im Zielbild?“

dann ist damit genau dieses Dokument gemeint.