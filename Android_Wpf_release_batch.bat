@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =========================================================
REM KGV MAUI + WPF Release-Build mit gemeinsamer Zielversion
REM - setzt Arbeitsordner
REM - loescht bin/obj
REM - liest aktuelle Versionen
REM - fragt EINE gemeinsame Zielversion ab
REM - erhoeht Android-Versionscode automatisch um 1
REM - setzt MAUI + WPF auf dieselbe Zielversion
REM - baut signierte APK und AAB
REM - baut WPF Release
REM - erstellt optional WPF-Setup per Inno Setup
REM =========================================================

set "REPO=C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "WPF_CSPROJ=%REPO%\KGV.Wpf\KGV.Wpf.csproj"

cd /d "%REPO%" || (
  echo FEHLER: Repo-Pfad nicht gefunden: %REPO%
  exit /b 1
)

echo.
echo =========================================================
echo KGV MAUI + WPF Release
echo Repo: %REPO%
echo =========================================================
echo.

if not exist "%MAUI_CSPROJ%" (
  echo FEHLER: MAUI csproj nicht gefunden: %MAUI_CSPROJ%
  exit /b 1
)

if not exist "%WPF_CSPROJ%" (
  echo FEHLER: WPF csproj nicht gefunden: %WPF_CSPROJ%
  exit /b 1
)

echo Loesche alte Build-Artefakte...
if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"
if exist "%REPO%\KGV.Wpf\bin"  rmdir /s /q "%REPO%\KGV.Wpf\bin"
if exist "%REPO%\KGV.Wpf\obj"  rmdir /s /q "%REPO%\KGV.Wpf\obj"

echo.
echo Lese aktuelle Versionen...

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$mauiXml = [xml](Get-Content '%MAUI_CSPROJ%');" ^
  "$mauiGroups = $mauiXml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion };" ^
  "if (-not $mauiGroups) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion in KGV.Maui.csproj gefunden' };" ^
  "$mauiDisplay = ($mauiGroups | ForEach-Object { if ($_.ApplicationDisplayVersion) { [string]$_.ApplicationDisplayVersion } } | Select-Object -First 1);" ^
  "$mauiCodes = @($mauiGroups | ForEach-Object { if ($_.ApplicationVersion) { [int]$_.ApplicationVersion } });" ^
  "if (-not $mauiDisplay) { throw 'ApplicationDisplayVersion konnte nicht gelesen werden' };" ^
  "if (-not $mauiCodes -or $mauiCodes.Count -eq 0) { throw 'ApplicationVersion konnte nicht gelesen werden' };" ^
  "$mauiCode = ($mauiCodes | Measure-Object -Maximum).Maximum;" ^
  "$wpfXml = [xml](Get-Content '%WPF_CSPROJ%');" ^
  "$wpfGroups = $wpfXml.Project.PropertyGroup;" ^
  "$wpfVersion = ($wpfGroups | ForEach-Object { if ($_.Version) { [string]$_.Version } } | Select-Object -First 1);" ^
  "$wpfFileVersion = ($wpfGroups | ForEach-Object { if ($_.FileVersion) { [string]$_.FileVersion } } | Select-Object -First 1);" ^
  "if (-not $wpfVersion) { $wpfVersion = $wpfFileVersion };" ^
  "if (-not $wpfVersion) { $wpfVersion = '<nicht gesetzt>' };" ^
  "Write-Output ($mauiDisplay + '|' + $mauiCode + '|' + $wpfVersion)"`) do set "VERLINE=%%I"

for /f "tokens=1,2,3 delims=|" %%A in ("%VERLINE%") do (
  set "CUR_MAUI_DISPLAY=%%A"
  set "CUR_MAUI_CODE=%%B"
  set "CUR_WPF_VERSION=%%C"
)

if not defined CUR_MAUI_DISPLAY (
  echo FEHLER: MAUI-Version konnte nicht gelesen werden.
  exit /b 1
)

if not defined CUR_MAUI_CODE (
  echo FEHLER: MAUI-Versionscode konnte nicht gelesen werden.
  exit /b 1
)

echo Aktuelle MAUI sichtbare Version: %CUR_MAUI_DISPLAY%
echo Aktueller Android Versionscode: %CUR_MAUI_CODE%
echo Aktuelle WPF Version: %CUR_WPF_VERSION%
echo.

set /p TARGET_VERSION=Gemeinsame Zielversion fuer MAUI und WPF eingeben (z. B. 0.2.9): 
if "%TARGET_VERSION%"=="" (
  echo FEHLER: Keine Zielversion eingegeben.
  exit /b 1
)

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$v = '%TARGET_VERSION%'.Trim();" ^
  "if ($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$') { throw 'Zielversion muss 3 oder 4 numerische Teile haben, z. B. 0.2.9 oder 0.2.9.0' };" ^
  "$parts = $v.Split('.');" ^
  "if ($parts.Count -eq 3) { $v4 = $v + '.0' } else { $v4 = $v };" ^
  "Write-Output ($v + '|' + $v4)"`) do set "VERCHECK=%%I"

if errorlevel 1 (
  echo FEHLER: Zielversion ist ungueltig.
  exit /b 1
)

for /f "tokens=1,2 delims=|" %%A in ("%VERCHECK%") do (
  set "TARGET_VERSION=%%A"
  set "TARGET_VERSION_4=%%B"
)

set /a NEW_ANDROID_CODE=%CUR_MAUI_CODE%+1

echo.
echo Setze gemeinsame Zielversion...
echo   MAUI ApplicationDisplayVersion = %TARGET_VERSION%
echo   MAUI ApplicationVersion        = %NEW_ANDROID_CODE%
echo   WPF Version/FileVersion        = %TARGET_VERSION%
echo   WPF AssemblyVersion            = %TARGET_VERSION_4%
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$mauiXml = [xml](Get-Content '%MAUI_CSPROJ%');" ^
  "$mauiGroups = $mauiXml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion };" ^
  "if (-not $mauiGroups) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion in KGV.Maui.csproj gefunden' };" ^
  "foreach ($pg in $mauiGroups) {" ^
  "  if ($pg.ApplicationDisplayVersion) { $pg.ApplicationDisplayVersion = '%TARGET_VERSION%' }" ^
  "  if ($pg.ApplicationVersion) { $pg.ApplicationVersion = '%NEW_ANDROID_CODE%' }" ^
  "}" ^
  "$mauiXml.Save('%MAUI_CSPROJ%');" ^
  "$wpfXml = [xml](Get-Content '%WPF_CSPROJ%');" ^
  "$groups = @($wpfXml.Project.PropertyGroup);" ^
  "$versionGroup = ($groups | Where-Object { $_.Version -or $_.FileVersion -or $_.AssemblyVersion -or $_.InformationalVersion } | Select-Object -First 1);" ^
  "if (-not $versionGroup) { $versionGroup = $wpfXml.CreateElement('PropertyGroup'); $null = $wpfXml.Project.AppendChild($versionGroup) }" ^
  "function SetOrCreate($parent, $name, $value) {" ^
  "  $node = $parent.$name;" ^
  "  if (-not $node) { $node = $wpfXml.CreateElement($name); $null = $parent.AppendChild($node) }" ^
  "  $node.InnerText = $value;" ^
  "}" ^
  "SetOrCreate $versionGroup 'Version' '%TARGET_VERSION%';" ^
  "SetOrCreate $versionGroup 'FileVersion' '%TARGET_VERSION%';" ^
  "SetOrCreate $versionGroup 'InformationalVersion' '%TARGET_VERSION%';" ^
  "SetOrCreate $versionGroup 'AssemblyVersion' '%TARGET_VERSION_4%';" ^
  "$wpfXml.Save('%WPF_CSPROJ%')"

if errorlevel 1 (
  echo FEHLER: Versionen konnten nicht geschrieben werden.
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
echo Baue WPF Release...
echo =========================================================
dotnet publish ".\KGV.Wpf\KGV.Wpf.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained false

if errorlevel 1 (
  echo FEHLER: WPF Release-Build fehlgeschlagen.
  exit /b 1
)

set "ISCC="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

set "WPF_ISS="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$repo = '%REPO%';" ^
  "$iss = Get-ChildItem $repo -Recurse -Filter *.iss | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' } | Sort-Object FullName | Select-Object -First 1 -ExpandProperty FullName;" ^
  "if ($iss) { Write-Output $iss }"`) do set "WPF_ISS=%%I"

echo.
echo =========================================================
echo WPF Release-Ergebnis
echo =========================================================
echo WPF Publish-Ordner:
echo   %REPO%\KGV.Wpf\bin\Release

if defined ISCC if defined WPF_ISS (
  echo.
  echo Inno Setup gefunden:
  echo   %ISCC%
  echo Verwende ISS-Datei:
  echo   %WPF_ISS%
  echo.
  echo =========================================================
  echo Erstelle WPF Setup...
  echo =========================================================
  "%ISCC%" /DAppVersion="%TARGET_VERSION%" /DMyAppVersion="%TARGET_VERSION%" "%WPF_ISS%"

  if errorlevel 1 (
    echo FEHLER: WPF-Setup-Erstellung mit Inno Setup fehlgeschlagen.
    exit /b 1
  )
) else (
  echo HINWEIS: WPF-Setup wurde nicht erstellt.
  if not defined ISCC echo   Ursache: ISCC.exe nicht gefunden.
  if not defined WPF_ISS echo   Ursache: Keine .iss-Datei im Repo gefunden.
)

echo.
echo =========================================================
echo Fertig. Neu erzeugte Release-Dateien:
echo =========================================================
echo.
echo MAUI Signed-Dateien:
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem '.\KGV.Maui\bin\Release\net9.0-android\publish' -Filter '*-Signed.*' | Sort-Object LastWriteTime -Descending | Select-Object FullName,Length,LastWriteTime | Format-List"

echo.
echo WPF Release-Dateien:
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$items = Get-ChildItem '.\KGV.Wpf\bin\Release' -Recurse -Include *.exe,*.msi | Sort-Object LastWriteTime -Descending;" ^
  "if ($items) { $items | Select-Object FullName,Length,LastWriteTime | Format-List } else { Write-Host 'Keine WPF EXE/MSI-Dateien gefunden.' }"

echo.
echo Gemeinsame Zielversion: %TARGET_VERSION%
echo Neuer Android Versionscode: %NEW_ANDROID_CODE%
echo.

endlocal
