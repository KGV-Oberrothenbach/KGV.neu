# Copilot Instructions

## Project Guidelines
- Vor Änderungen den Istzustand im Repo prüfen; Änderungen blockweise minimal-invasiv umsetzen, kompakt zusammenfassen, committen und pushen. 
- Im KGV-Projekt immer den realen Repo-/Git-/Fortschrittslog-Stand prüfen; Änderungen als kleinsten buildfähigen Block umsetzen; danach `KGV_Fortschrittslog_ausfuehrlich.md` und `DEV_LOG.md` pflegen, `dotnet build KGV.Maui/KGV.Maui.csproj` ausführen, nur Blockdateien stagen sowie am Ende committen und pushen.
- Im KGV-Projekt vor Änderungen den realen Repo-Stand prüfen, DEV_LOG.md und KGV_Fortschrittslog_ausfuehrlich.md pflegen, MAUI und WPF builden und am Ende committen/pushen.
- Für Strukturblöcke im KGV-Projekt immer zuerst den realen Istzustand prüfen; WPF und MAUI parallel mitdenken; DEV_LOG.md und KGV_Fortschrittslog_ausfuehrlich.md fortführen; nur Blockdateien stagen; am Ende builden, committen und pushen.
- Änderungen sollen immer für WPF und MAUI mitgedacht werden. Wenn im KGV-Projekt ausdrücklich 'nur MAUI' gefordert ist, sollen nur MAUI-Dateien geändert und WPF-Dateien nicht angerührt werden. Bei ausdrücklich genanntem MAUI-Bugfix nur MAUI-Dateien ändern, WPF nicht anfassen, minimal-invasiv vorgehen, Logs pflegen, MAUI builden und am Ende committen/pushen.
- Im KGV-Projekt bei diesem Abschlusslauf nur MAUI-Dateien plus Logdateien anfassen, blockfremde Dateien draußen lassen, ehrlich dokumentieren und am Ende nur Blockdateien committen/pushen. Für diesen KGV-Abschlusslauf soll kein neuer Fachumfang gestartet werden; es geht nur um den sauberen technischen Abschluss des begonnenen Wartungsverträge-Prompt-2/3-Blocks, mit minimalen Korrekturen nur bei klaren Abschlussfehlern, ehrlicher Dokumentation blockfremder MAUI-Fehler und verpflichtendem Commit/Push über den vorgegebenen Visual-Studio-Git-Pfad.
- Im KGV-Projekt bei Abschlussläufen den realen Repo-Stand prüfen, nur den begonnenen Block minimal-invasiv abschließen, Logdateien pflegen und Git-Operationen über den vorgegebenen Visual-Studio-Git-Pfad `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Git\cmd\git.exe` ausführen; am Ende alle aktuellen Änderungen committen und nach origin/main pushen.
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
- Für den KGV-Wartungsverträge-Prompt 2/3 soll auf dem real begonnenen Zwischenstand aufgebaut werden: nur direkt blockbezogene Wartungsverträge-Dateien anfassen, WPF und MAUI parallel fertigstellen, Logs pflegen, WPF- und MAUI-Build validieren und am Ende nur Blockdateien über den vorgegebenen Visual-Studio-Git-Pfad committen und nach origin/main pushen.
- Für den KGV-Wartungsverträge-Prompt 3/3 soll minimal-invasiv auf den bestehenden Prompt-1/3- und 2/3-Pfaden aufgebaut werden, WPF und MAUI parallel fertiggestellt werden, Logs gepflegt werden und Git-Operationen über den vorgegebenen Visual-Studio-Git-Pfad mit verpflichtendem Commit und Push erfolgen.
- Im KGV.ReleaseManager für diese Blöcke zuerst den realen Repo-Stand prüfen, interne Logs und ReleaseManager-Logs pflegen, ReleaseManager und gesamte Solution builden und am Ende committen/pushen.

## MAUI-Spezifische Hinweise
- Das MAUI-Launcher-Icon ist trotz vorherigem Fix noch nicht korrekt; bitte sicherstellen, dass dies in zukünftigen Änderungen berücksichtigt wird.
- Beim Navigieren treten ANR-/"App reagiert nicht"-Probleme auf; diese sollten priorisiert untersucht und behoben werden.