@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =========================================================
REM KGV MAUI Release-Build mit Versionserhoehung
REM - setzt Arbeitsordner
REM - loescht bin/obj
REM - liest aktuelle Version aus KGV.Maui.csproj
REM - fragt neue sichtbare Version ab
REM - erhoeht Versionscode automatisch um 1
REM - baut signierte APK und AAB
REM - zeigt die neu erzeugten Signed-Dateien an
REM =========================================================

set "REPO=C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand"
set "CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"

cd /d "%REPO%" || (
  echo FEHLER: Repo-Pfad nicht gefunden: %REPO%
  exit /b 1
)

echo.
echo =========================================================
echo KGV MAUI Release
echo Repo: %REPO%
echo =========================================================
echo.

if not exist "%CSPROJ%" (
  echo FEHLER: csproj nicht gefunden: %CSPROJ%
  exit /b 1
)

echo Loesche alte Build-Artefakte...
if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"

echo.
echo Lese aktuelle Version aus der csproj...

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "[xml]$xml = Get-Content '%CSPROJ%';" ^
  "$groups = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion };" ^
  "if (-not $groups) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion gefunden' };" ^
  "$display = ($groups | ForEach-Object { if ($_.ApplicationDisplayVersion) { [string]$_.ApplicationDisplayVersion } } | Select-Object -First 1);" ^
  "$codes = @($groups | ForEach-Object { if ($_.ApplicationVersion) { [int]$_.ApplicationVersion } });" ^
  "if (-not $display) { throw 'ApplicationDisplayVersion konnte nicht gelesen werden' };" ^
  "if (-not $codes -or $codes.Count -eq 0) { throw 'ApplicationVersion konnte nicht gelesen werden' };" ^
  "$maxCode = ($codes | Measure-Object -Maximum).Maximum;" ^
  "Write-Output ($display + '|' + $maxCode)"`) do set "VERLINE=%%I"

for /f "tokens=1,2 delims=|" %%A in ("%VERLINE%") do (
  set "CUR_DISPLAY=%%A"
  set "CUR_CODE=%%B"
)

if not defined CUR_DISPLAY (
  echo FEHLER: ApplicationDisplayVersion konnte nicht gelesen werden.
  exit /b 1
)

if not defined CUR_CODE (
  echo FEHLER: ApplicationVersion konnte nicht gelesen werden.
  exit /b 1
)

echo Aktuelle sichtbare Version: %CUR_DISPLAY%
echo Aktueller Versionscode: %CUR_CODE%
echo.

set /p NEW_DISPLAY=Neue sichtbare Version eingeben (z. B. 0.2.9): 
if "%NEW_DISPLAY%"=="" (
  echo FEHLER: Keine neue sichtbare Version eingegeben.
  exit /b 1
)

set /a NEW_CODE=%CUR_CODE%+1

echo.
echo Setze neue Version...
echo   ApplicationDisplayVersion = %NEW_DISPLAY%
echo   ApplicationVersion        = %NEW_CODE%

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "[xml]$xml = Get-Content '%CSPROJ%';" ^
  "$groups = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion };" ^
  "if (-not $groups) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion gefunden' };" ^
  "foreach ($pg in $groups) {" ^
  "  if ($pg.ApplicationDisplayVersion) { $pg.ApplicationDisplayVersion = '%NEW_DISPLAY%' }" ^
  "  if ($pg.ApplicationVersion) { $pg.ApplicationVersion = '%NEW_CODE%' }" ^
  "}" ^
  "$xml.Save('%CSPROJ%')"

if errorlevel 1 (
  echo FEHLER: Version konnte nicht in die csproj geschrieben werden.
  exit /b 1
)

echo.
echo Bitte Keystore/Key-Passwort eingeben...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$p = Read-Host 'Keystore/Key Passwort' -AsSecureString;" ^
  "$BSTR = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);" ^
  "try { [Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR) }"`) do set "STOREPASS=%%P"

if not defined STOREPASS (
  echo FEHLER: Kein Passwort eingegeben.
  exit /b 1
)

echo.
echo =========================================================
echo Baue signierte APK...
echo =========================================================
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" ^
  -f net9.0-android ^
  -c Release ^
  -p:AndroidPackageFormat=apk ^
  -p:AndroidSigningStorePass="%STOREPASS%"

if errorlevel 1 (
  echo FEHLER: APK-Build fehlgeschlagen.
  exit /b 1
)

echo.
echo =========================================================
echo Baue signierte AAB...
echo =========================================================
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" ^
  -f net9.0-android ^
  -c Release ^
  -p:AndroidPackageFormat=aab ^
  -p:AndroidSigningStorePass="%STOREPASS%"

if errorlevel 1 (
  echo FEHLER: AAB-Build fehlgeschlagen.
  exit /b 1
)

echo.
echo =========================================================
echo Fertig. Neu erzeugte Signed-Dateien:
echo =========================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem '.\KGV.Maui\bin\Release\net9.0-android\publish' -Filter '*-Signed.*' | Sort-Object LastWriteTime -Descending | Select-Object FullName,Length,LastWriteTime | Format-List"

echo.
echo Neue sichtbare Version: %NEW_DISPLAY%
echo Neuer Versionscode: %NEW_CODE%
echo.

endlocal