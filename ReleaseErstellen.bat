@echo off
setlocal EnableExtensions DisableDelayedExpansion
cls

REM ============================================================
REM KGV Release-Worktree vorbereiten und AWR starten
REM
REM Fester Worktree:
REM   C:\Programmieren\KGV\KGV.release
REM
REM Branch bleibt versionsbezogen:
REM   release/0.8.0
REM   release/0.8.1
REM   ...
REM
REM Ablauf:
REM 1. Release-Version abfragen
REM 2. vorhandenen generischen Release-Worktree pruefen/entfernen
REM 3. neuen Worktree vom aktuellen committed HEAD erzeugen
REM 4. appsettings.json uebernehmen
REM 5. Web-Abhaengigkeiten per npm run install:ci installieren
REM 6. temporaere AWR erzeugen/anpassen
REM 7. AWR starten
REM
REM Nicht committete Aenderungen aus KGV.neu werden NICHT uebernommen.
REM ============================================================

set "MAIN_REPO=C:\Programmieren\KGV\KGV.neu"
set "RELEASE_REPO=C:\Programmieren\KGV\KGV.release"
set "SOURCE_AWR=%MAIN_REPO%\AWR.bat"
set "SOURCE_APPSETTINGS=%MAIN_REPO%\appsettings.json"
set "SOURCE_KEYSTORE=%MAIN_REPO%\_secrets\Android\kgv-upload.keystore"

echo.
echo ============================================================
echo KGV Release erstellen
echo ============================================================
echo.

set "VERSION="
set /p VERSION=Release-Version eingeben ^(z.B. 0.8.0^): 

if not defined VERSION (
    echo FEHLER: Keine Version eingegeben.
    goto FAILURE
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$v='%VERSION%'.Trim(); if($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$'){exit 1}" >nul 2>&1
if errorlevel 1 (
    echo FEHLER: Ungueltige Version "%VERSION%".
    echo Erlaubt sind z.B. 0.8.0 oder 0.8.0.1
    goto FAILURE
)

set "RELEASE_BRANCH=release/%VERSION%"
set "TEMP_AWR=%TEMP%\KGV-AWR-%VERSION%.bat"

where git >nul 2>&1
if errorlevel 1 (
    echo FEHLER: git.exe wurde nicht gefunden.
    goto FAILURE
)

where npm >nul 2>&1
if errorlevel 1 (
    echo FEHLER: npm wurde nicht gefunden.
    goto FAILURE
)

if not exist "%MAIN_REPO%\.git" (
    echo FEHLER: Hauptrepo nicht gefunden:
    echo %MAIN_REPO%
    goto FAILURE
)

if not exist "%SOURCE_AWR%" (
    echo FEHLER: AWR.bat nicht gefunden:
    echo %SOURCE_AWR%
    goto FAILURE
)

if not exist "%SOURCE_APPSETTINGS%" (
    echo FEHLER: appsettings.json nicht gefunden:
    echo %SOURCE_APPSETTINGS%
    goto FAILURE
)

if not exist "%SOURCE_KEYSTORE%" (
    echo FEHLER: Android-Keystore nicht gefunden:
    echo %SOURCE_KEYSTORE%
    goto FAILURE
)

if exist "%RELEASE_REPO%" (
    echo Vorhandener Release-Worktree gefunden:
    echo %RELEASE_REPO%
    echo.

    git -C "%RELEASE_REPO%" rev-parse --is-inside-work-tree >nul 2>&1
    if errorlevel 1 (
        echo FEHLER: Der Ordner existiert, ist aber kein Git-Worktree.
        echo Bitte manuell pruefen:
        echo %RELEASE_REPO%
        goto FAILURE
    )

    for /f "delims=" %%I in ('git -C "%RELEASE_REPO%" status --porcelain') do set "OLD_RELEASE_DIRTY=1"
    if defined OLD_RELEASE_DIRTY (
        echo FEHLER: Der vorhandene Release-Worktree enthaelt Aenderungen.
        git -C "%RELEASE_REPO%" status -sb
        echo.
        echo Er wird NICHT automatisch entfernt.
        goto FAILURE
    )

    echo Alter Release-Worktree ist sauber und wird entfernt...
    git -C "%MAIN_REPO%" worktree remove "%RELEASE_REPO%"
    if errorlevel 1 (
        echo FEHLER: Alter Release-Worktree konnte nicht entfernt werden.
        goto FAILURE
    )
)

git -C "%MAIN_REPO%" worktree prune

for /f "delims=" %%I in ('git -C "%MAIN_REPO%" rev-parse HEAD 2^>nul') do set "BASE_COMMIT=%%I"
if not defined BASE_COMMIT (
    echo FEHLER: Aktueller Git-Commit konnte nicht ermittelt werden.
    goto FAILURE
)

for /f "delims=" %%I in ('git -C "%MAIN_REPO%" rev-parse --short^=12 HEAD 2^>nul') do set "BASE_SHORT=%%I"

echo.
echo Ausgangsrepo : %MAIN_REPO%
echo Ausgangscommit: %BASE_SHORT%
echo Releasebranch : %RELEASE_BRANCH%
echo Releasepfad   : %RELEASE_REPO%
echo.
echo HINWEIS:
echo Nicht committete Aenderungen im Hauptrepo werden nicht uebernommen.
echo.

git -C "%MAIN_REPO%" show-ref --verify --quiet "refs/heads/%RELEASE_BRANCH%"
if errorlevel 1 goto CREATE_BRANCH

echo Lokaler Release-Branch existiert bereits.
echo Erzeuge Worktree aus vorhandenem Branch...
git -C "%MAIN_REPO%" worktree add "%RELEASE_REPO%" "%RELEASE_BRANCH%"
if errorlevel 1 goto WORKTREE_FAILED
goto WORKTREE_READY

:CREATE_BRANCH
echo Erzeuge neuen Release-Branch und Worktree...
git -C "%MAIN_REPO%" worktree add -b "%RELEASE_BRANCH%" "%RELEASE_REPO%" "%BASE_COMMIT%"
if errorlevel 1 goto WORKTREE_FAILED

:WORKTREE_READY
echo.
echo Kopiere lokale appsettings.json...
copy /Y "%SOURCE_APPSETTINGS%" "%RELEASE_REPO%\appsettings.json" >nul
if errorlevel 1 (
    echo FEHLER: appsettings.json konnte nicht kopiert werden.
    goto FAILURE
)

echo.
echo Installiere Web-Abhaengigkeiten...
if not exist "%RELEASE_REPO%\KGV.Web\package.json" (
    echo FEHLER: KGV.Web\package.json wurde nicht gefunden.
    goto FAILURE
)

pushd "%RELEASE_REPO%\KGV.Web"
call npm run install:ci
set "NPM_RESULT=%ERRORLEVEL%"
popd

if not "%NPM_RESULT%"=="0" (
    echo FEHLER: Web-Abhaengigkeiten konnten nicht installiert werden.
    goto FAILURE
)

echo.
echo Erzeuge temporaere AWR...
copy /Y "%SOURCE_AWR%" "%TEMP_AWR%" >nul
if errorlevel 1 (
    echo FEHLER: Temporaere AWR konnte nicht erzeugt werden.
    goto FAILURE
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$p=Get-Content -Raw '%TEMP_AWR%';" ^
  "$oldRepo='set ""REPO=C:\Programmieren\KGV\KGV.neu""';" ^
  "$newRepo='set ""REPO=%RELEASE_REPO%""';" ^
  "$oldKey='set ""KEYSTORE=%%REPO%%\_secrets\Android\kgv-upload.keystore""';" ^
  "$newKey='set ""KEYSTORE=%SOURCE_KEYSTORE%""';" ^
  "if(-not $p.Contains($oldRepo)){throw 'REPO-Zeile in AWR nicht gefunden'};" ^
  "if(-not $p.Contains($oldKey)){throw 'KEYSTORE-Zeile in AWR nicht gefunden'};" ^
  "$p=$p.Replace($oldRepo,$newRepo).Replace($oldKey,$newKey);" ^
  "[IO.File]::WriteAllText('%TEMP_AWR%',$p,[Text.Encoding]::ASCII)"

if errorlevel 1 (
    echo FEHLER: Temporaere AWR konnte nicht angepasst werden.
    goto FAILURE
)

echo.
echo Pruefe Release-Worktree...
for /f "delims=" %%I in ('git -C "%RELEASE_REPO%" status --porcelain') do set "RELEASE_DIRTY=1"

if defined RELEASE_DIRTY (
    echo FEHLER: Release-Worktree ist nicht sauber.
    git -C "%RELEASE_REPO%" status -sb
    goto FAILURE
)

echo.
echo ============================================================
echo Release-Worktree vorbereitet
echo ============================================================
echo Repo     : %RELEASE_REPO%
echo Branch   : %RELEASE_BRANCH%
echo Commit   : %BASE_SHORT%
echo AWR Temp : %TEMP_AWR%
echo.
echo Starte jetzt AWR...
echo.

call "%TEMP_AWR%"
set "AWR_RESULT=%ERRORLEVEL%"

echo.
if "%AWR_RESULT%"=="0" (
    echo AWR wurde erfolgreich beendet.
) else (
    echo AWR wurde mit Fehlercode %AWR_RESULT% beendet.
)

echo.
echo Der generische Release-Worktree bleibt fuer Kontrolle erhalten:
echo %RELEASE_REPO%
echo.
echo Beim naechsten Release wird er automatisch entfernt,
echo sofern er sauber ist.
echo.

exit /b %AWR_RESULT%

:WORKTREE_FAILED
echo.
echo FEHLER: Release-Worktree konnte nicht erstellt werden.
goto FAILURE

:FAILURE
echo.
echo Vorgang abgebrochen.
exit /b 1
