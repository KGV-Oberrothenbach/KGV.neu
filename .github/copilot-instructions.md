# Copilot Instructions

## Project Guidelines
- Vor Änderungen den Istzustand im Repo prüfen; Änderungen blockweise minimal-invasiv umsetzen, kompakt zusammenfassen, committen und pushen. 
- Im KGV-Projekt immer den realen Repo-/Git-/Fortschrittslog-Stand prüfen; Änderungen als kleinsten buildfähigen Block umsetzen; danach `KGV_Fortschrittslog_ausfuehrlich.md` und `DEV_LOG.md` pflegen, `dotnet build KGV.Maui/KGV.Maui.csproj` ausführen, nur Blockdateien stagen sowie am Ende committen und pushen.
- Aktive Produktbereiche sind `KGV.Core`, `KGV.Infrastructure`, `KGV.Maui` und `KGV.Web`; MAUI und Web sollen fachlich konsistent auf denselben Backend- und Workflowverträgen aufbauen.
- Im KGV-Projekt bei Verifikations-/Abschlussblöcken zuerst Git gegen `origin/main` prüfen (fetch, status, branch divergence), danach nur direkt betroffene Dateien lesen, nur echte Restlücken schließen, MAUI und Web builden, Logs pflegen und anschließend committen/pushen.
- Für Strukturblöcke im KGV-Projekt immer zuerst den realen Istzustand prüfen; MAUI und Web fachlich parallel mitdenken; DEV_LOG.md und KGV_Fortschrittslog_ausfuehrlich.md fortführen; nur Blockdateien stagen; am Ende builden, committen und pushen.
- Im KGV-Projekt bei diesem Abschlusslauf nur MAUI-Dateien plus Logdateien anfassen, blockfremde Dateien draußen lassen, ehrlich dokumentieren und am Ende nur Blockdateien committen/pushen. Für diesen KGV-Abschlusslauf soll kein neuer Fachumfang gestartet werden; es geht nur um den sauberen technischen Abschluss des begonnenen Wartungsverträge-Prompt-2/3-Blocks, mit minimalen Korrekturen nur bei klaren Abschlussfehlern, ehrlicher Dokumentation blockfremder MAUI-Fehler und verpflichtendem Commit/Push über den vorgegebenen Visual-Studio-Git-Pfad.
- Im KGV-Projekt bei Abschlussläufen den realen Repo-Stand prüfen, nur den begonnenen Block minimal-invasiv abschließen, Logdateien pflegen und Git-Operationen über den vorgegebenen Visual-Studio-Git-Pfad `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Git\cmd\git.exe` ausführen; am Ende alle aktuellen Änderungen committen und nach origin/main pushen.
- Vor jedem Merge nach `main` alle vorhandenen und neu hinzugekommenen Supabase-Migrationen prüfen: SQL-Syntax, Klammerung, Reihenfolge und Abhängigkeiten, Wiederholbarkeit/Idempotenz, unbeabsichtigte Datenänderungen bei erneutem Ausführen sowie lokalen und Remote-Migrationsstand. `CREATE POLICY` braucht ein geeignetes vorheriges `DROP POLICY IF EXISTS`, Trigger ein `DROP TRIGGER IF EXISTS`, Constraints eine Existenzprüfung bzw. vorheriges Entfernen, Tabellen/Spalten verwenden wo sinnvoll `IF NOT EXISTS`, Funktionen und Views möglichst `CREATE OR REPLACE`. Bei verknüpftem Supabase-Projekt sind `npx supabase migration list --linked` und `npx supabase db push --linked --dry-run` verpflichtende Merge-Gates. Nach `main` wird erst gemergt, wenn keine unerwarteten Migrationen vorliegen, neue Migrationen syntaktisch plausibel sind, keine bekannte nicht-idempotente Migration unkontrolliert erneut laufen kann und der Dry-Run erfolgreich ist. `npx supabase db push --linked` nie automatisch im Merge-Check ausführen; produktive Migrationen bleiben eine bewusste, separate Aktion.
- DEV_LOG.md und KGV_Fortschrittslog_ausfuehrlich.md sollen gepflegt werden.
- Keine Schattenlogik neben echten DB-/RPC-Pfaden bauen.
- Speichern-Buttons sollen immer am Ende von Eingabeformularen platziert werden.
- Demo-/Play-Store-Testdaten dürfen fachliche Berechnungen und Auswertungen nicht beeinflussen.
- Admins/Vorstand sollen mobil grundsätzlich alles machen können, was sie auch am PC machen können.
- Im Auth-/Supabase-Kontext wurde vom anon key auf den sb_publishable bzw. Publishable Key umgestellt; neue Änderungen sollen das berücksichtigen.
- Auf der Startseite sollen bei Arbeitseinsätzen, Terminen und Bekanntmachungen nur Listen stehen; Details sollen in einer eigenen View statt in einem Teilbereich der HomeView geöffnet werden.
- Verwaltungseditoren für Arbeitseinsätze, Termine und Bekanntmachungen sollen nur über die HomeView statt über die Hauptnavigation erreichbar sein.
- Arbeitsstunden erfassen soll nicht in der Hauptnavigation erscheinen, sondern nur über den Button auf der Startseite erreichbar sein.
- Immer DEV_LOG.md ergänzen.
- Im KGV-Projekt sollen Home, Detail und Verwaltung fachlich strikt getrennt bleiben; bestehende Produktivpfade sind weiterzuverwenden, kleine buildfähige Blöcke mit Logpflege umzusetzen, nur Blockdateien zu stagen und am Ende MAUI zu builden, committen und pushen.
- Für den KGV-Wartungsverträge-Prompt 3/3 soll minimal-invasiv auf den bestehenden Prompt-1/3- und 2/3-Pfaden aufgebaut werden, MAUI und Web parallel fertiggestellt werden, Logs gepflegt werden und Git-Operationen über den vorgegebenen Visual-Studio-Git-Pfad mit verpflichtendem Commit und Push erfolgen.
- Für diesen KGV-Block vor Änderungen den realen Repo-Stand prüfen, MAUI und Web fachlich parallel mitdenken, Home-Sichtbarkeit für aktive Datensätze über konsistente sichtbar_von/sichtbar_bis-Regeln herstellen, Editor-Defaults ohne Sichtbarkeits-/Zeit-Checkboxen umsetzen, Logs pflegen und am Ende committen/pushen.

## MAUI-Spezifische Hinweise
- Das MAUI-Launcher-Icon ist trotz vorherigem Fix noch nicht korrekt; bitte sicherstellen, dass dies in zukünftigen Änderungen berücksichtigt wird.
- Beim Navigieren treten ANR-/"App reagiert nicht"-Probleme auf; diese sollten priorisiert untersucht und behoben werden.
