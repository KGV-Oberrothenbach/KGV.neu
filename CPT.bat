@echo off
setlocal

cd /d "C:\Programmieren\KGV\KGV.neu"

set "GIT=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"

if not exist "%GIT%" (
    echo Git wurde hier nicht gefunden:
    echo %GIT%
    pause
    exit /b 1
)

echo.
echo === AKTUELLER STATUS ===
"%GIT%" status --short --branch
echo.

set /p COMMITMSG=Commit-Nachricht eingeben: 

if "%COMMITMSG%"=="" (
    echo Keine Commit-Nachricht eingegeben. Abbruch.
    pause
    exit /b 1
)

echo.
echo === STAGE NUR GETRACKTE AENDERUNGEN ===
"%GIT%" add -u

echo.
echo === STATUS NACH ADD -u ===
"%GIT%" status --short
echo.

"%GIT%" diff --cached --quiet
if %errorlevel%==0 (
    echo Keine getrackten Aenderungen zum Committen gefunden.
    pause
    exit /b 0
)

echo.
echo === COMMIT ===
"%GIT%" commit -m "%COMMITMSG%"
if errorlevel 1 (
    echo Commit fehlgeschlagen.
    pause
    exit /b 1
)

echo.
echo === PUSH ===
"%GIT%" push origin main
if errorlevel 1 (
    echo Push fehlgeschlagen.
    pause
    exit /b 1
)

echo.
echo Fertig.
pause
endlocal