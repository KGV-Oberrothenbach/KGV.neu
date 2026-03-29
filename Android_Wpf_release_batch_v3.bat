@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM =========================================================
REM KGV MAUI + WPF Release mit gemeinsamer Zielversion
REM - setzt MAUI + WPF auf dieselbe Zielversion
REM - erhoeht Android ApplicationVersion automatisch um 1
REM - speichert Artefakte unter publish\<Version>
REM - verwendet KGV.Wpf.exe als WPF-Release-Quelle
REM - kopiert diese EXE zusaetzlich ins Repo KGV-WPF
REM - fuehrt dort Commit + Push aus
REM =========================================================

set "REPO=C:\Programmieren\Restore KGV\KGV.neu\03_Arbeitsstand"
set "WPF_RELEASE_REPO=C:\Programmieren\Restore KGV\KGV-WPF"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "WPF_CSPROJ=%REPO%\KGV.Wpf\KGV.Wpf.csproj"
set "PUBLISH_ROOT=%REPO%\publish"

set "GIT="
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\cmd\git.exe" set "GIT=C:\Program Files\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\bin\git.exe" set "GIT=C:\Program Files\Git\bin\git.exe"

set "ISCC="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

cd /d "%REPO%" || (
  echo FEHLER: Repo-Pfad nicht gefunden: %REPO%
  exit /b 1
)

echo.
echo =========================================================
echo KGV MAUI + WPF Release
echo Repo: %REPO%
echo WPF-Zielrepo: %WPF_RELEASE_REPO%
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

if not exist "%WPF_RELEASE_REPO%" (
  echo FEHLER: WPF-Zielrepo nicht gefunden: %WPF_RELEASE_REPO%
  exit /b 1
)

if not defined GIT (
  echo FEHLER: git.exe wurde nicht gefunden.
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
  "$mauiGroups = @($mauiXml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion });" ^
  "if ($mauiGroups.Count -eq 0) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion in KGV.Maui.csproj gefunden' }" ^
  "$mauiDisplay = ($mauiGroups | ForEach-Object { if ($_.ApplicationDisplayVersion) { [string]$_.ApplicationDisplayVersion } } | Select-Object -First 1);" ^
  "$mauiCodes = @($mauiGroups | ForEach-Object { if ($_.ApplicationVersion) { [int]$_.ApplicationVersion } });" ^
  "if (-not $mauiDisplay) { throw 'ApplicationDisplayVersion konnte nicht gelesen werden' }" ^
  "if ($mauiCodes.Count -eq 0) { throw 'ApplicationVersion konnte nicht gelesen werden' }" ^
  "$mauiCode = ($mauiCodes | Measure-Object -Maximum).Maximum;" ^
  "$wpfXml = [xml](Get-Content '%WPF_CSPROJ%');" ^
  "$wpfGroups = @($wpfXml.Project.PropertyGroup);" ^
  "$wpfVersion = ($wpfGroups | ForEach-Object { if ($_.Version) { [string]$_.Version } } | Select-Object -First 1);" ^
  "$wpfFileVersion = ($wpfGroups | ForEach-Object { if ($_.FileVersion) { [string]$_.FileVersion } } | Select-Object -First 1);" ^
  "if (-not $wpfVersion) { $wpfVersion = $wpfFileVersion }" ^
  "if (-not $wpfVersion) { $wpfVersion = '<nicht gesetzt>' }" ^
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

set /p TARGET_VERSION=Gemeinsame Zielversion fuer MAUI und WPF eingeben (z. B. 0.2.10): 
if "%TARGET_VERSION%"=="" (
  echo FEHLER: Keine Zielversion eingegeben.
  exit /b 1
)

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$v = '%TARGET_VERSION%'.Trim();" ^
  "if ($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$') { throw 'Zielversion muss 3 oder 4 numerische Teile haben, z. B. 0.2.10 oder 0.2.10.0' }" ^
  "$parts = $v.Split('.');" ^
  "if ($parts.Count -eq 3) { $v4 = $v + '.0' } else { $v4 = $v }" ^
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
set "VERSION_ROOT=%PUBLISH_ROOT%\%TARGET_VERSION%"
set "ANDROID_OUT=%VERSION_ROOT%\Android"
set "WPF_OUT=%VERSION_ROOT%\WPF"
set "WPF_PUBLISH_OUT=%WPF_OUT%\Publish"
set "WPF_SETUP_OUT=%WPF_OUT%\Setup"

if not exist "%PUBLISH_ROOT%" mkdir "%PUBLISH_ROOT%"
if not exist "%VERSION_ROOT%" mkdir "%VERSION_ROOT%"
if not exist "%ANDROID_OUT%" mkdir "%ANDROID_OUT%"
if not exist "%WPF_OUT%" mkdir "%WPF_OUT%"
if not exist "%WPF_PUBLISH_OUT%" mkdir "%WPF_PUBLISH_OUT%"
if not exist "%WPF_SETUP_OUT%" mkdir "%WPF_SETUP_OUT%"

echo.
echo Setze gemeinsame Zielversion...
echo   MAUI ApplicationDisplayVersion = %TARGET_VERSION%
echo   MAUI ApplicationVersion        = %NEW_ANDROID_CODE%
echo   WPF Version/FileVersion        = %TARGET_VERSION%
echo   WPF AssemblyVersion            = %TARGET_VERSION_4%
echo   Artefaktordner                 = %VERSION_ROOT%
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "function SetNodeText([xml]$doc, [System.Xml.XmlElement]$parent, [string]$name, [string]$value) {" ^
  "  $node = $parent.SelectSingleNode($name);" ^
  "  if ($null -eq $node) { $node = $doc.CreateElement($name); [void]$parent.AppendChild($node) }" ^
  "  $node.InnerText = $value;" ^
  "}" ^
  "$mauiXml = [xml](Get-Content '%MAUI_CSPROJ%');" ^
  "$mauiGroups = @($mauiXml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion -or $_.ApplicationVersion });" ^
  "if ($mauiGroups.Count -eq 0) { throw 'Keine ApplicationDisplayVersion/ApplicationVersion in KGV.Maui.csproj gefunden' }" ^
  "foreach ($pg in $mauiGroups) {" ^
  "  if ($pg.ApplicationDisplayVersion -or $pg.ApplicationVersion) {" ^
  "    SetNodeText $mauiXml $pg 'ApplicationDisplayVersion' '%TARGET_VERSION%';" ^
  "    SetNodeText $mauiXml $pg 'ApplicationVersion' '%NEW_ANDROID_CODE%';" ^
  "  }" ^
  "}" ^
  "$mauiXml.Save('%MAUI_CSPROJ%');" ^
  "$wpfXml = [xml](Get-Content '%WPF_CSPROJ%');" ^
  "$groups = @($wpfXml.Project.PropertyGroup);" ^
  "$versionGroup = $groups | Where-Object { $_.Version -or $_.FileVersion -or $_.AssemblyVersion -or $_.InformationalVersion } | Select-Object -First 1;" ^
  "if ($null -eq $versionGroup) { $versionGroup = $wpfXml.CreateElement('PropertyGroup'); [void]$wpfXml.Project.AppendChild($versionGroup) }" ^
  "SetNodeText $wpfXml $versionGroup 'Version' '%TARGET_VERSION%';" ^
  "SetNodeText $wpfXml $versionGroup 'FileVersion' '%TARGET_VERSION%';" ^
  "SetNodeText $wpfXml $versionGroup 'InformationalVersion' '%TARGET_VERSION%';" ^
  "SetNodeText $wpfXml $versionGroup 'AssemblyVersion' '%TARGET_VERSION_4%';" ^
  "$wpfXml.Save('%WPF_CSPROJ%');"

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

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$publish = Join-Path '%REPO%' 'KGV.Maui\bin\Release\net9.0-android\publish';" ^
  "$apk = Get-ChildItem $publish -Filter '*-Signed.apk' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if (-not $apk) { $apk = Get-ChildItem $publish -Filter '*.apk' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }" ^
  "if ($apk) { Write-Output $apk.FullName }"`) do set "APK_FILE=%%I"

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$publish = Join-Path '%REPO%' 'KGV.Maui\bin\Release\net9.0-android\publish';" ^
  "$aab = Get-ChildItem $publish -Filter '*-Signed.aab' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if (-not $aab) { $aab = Get-ChildItem $publish -Filter '*.aab' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }" ^
  "if ($aab) { Write-Output $aab.FullName }"`) do set "AAB_FILE=%%I"

if not defined APK_FILE (
  echo FEHLER: Keine APK-Datei gefunden.
  exit /b 1
)

if not defined AAB_FILE (
  echo FEHLER: Keine AAB-Datei gefunden.
  exit /b 1
)

copy /Y "%APK_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk" >nul || (
  echo FEHLER: APK konnte nicht nach %ANDROID_OUT% kopiert werden.
  exit /b 1
)
copy /Y "%APK_FILE%" "%VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.apk" >nul || (
  echo FEHLER: APK konnte nicht nach %VERSION_ROOT% kopiert werden.
  exit /b 1
)
copy /Y "%AAB_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab" >nul || (
  echo FEHLER: AAB konnte nicht nach %ANDROID_OUT% kopiert werden.
  exit /b 1
)
copy /Y "%AAB_FILE%" "%VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.aab" >nul || (
  echo FEHLER: AAB konnte nicht nach %VERSION_ROOT% kopiert werden.
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

set "WPF_SOURCE_DIR=%REPO%\KGV.Wpf\bin\Release\net8.0-windows\win-x64"
set "WPF_SOURCE_EXE=%WPF_SOURCE_DIR%\KGV.Wpf.exe"

if not exist "%WPF_SOURCE_EXE%" (
  echo FEHLER: Erwartete WPF-Datei wurde nicht gefunden:
  echo   %WPF_SOURCE_EXE%
  exit /b 1
)

echo.
echo Kopiere WPF-Release-Dateien nach %WPF_PUBLISH_OUT% ...
robocopy "%WPF_SOURCE_DIR%" "%WPF_PUBLISH_OUT%" /E /NFL /NDL /NJH /NJS /NC /NS >nul
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
  echo FEHLER: WPF-Release-Dateien konnten nicht nach %WPF_PUBLISH_OUT% kopiert werden.
  exit /b 1
)

copy /Y "%WPF_SOURCE_EXE%" "%WPF_SETUP_OUT%\KGV-Setup-%TARGET_VERSION%.exe" >nul || (
  echo FEHLER: WPF EXE konnte nicht nach %WPF_SETUP_OUT% kopiert werden.
  exit /b 1
)
copy /Y "%WPF_SOURCE_EXE%" "%VERSION_ROOT%\KGV-Setup-%TARGET_VERSION%.exe" >nul || (
  echo FEHLER: WPF EXE konnte nicht nach %VERSION_ROOT% kopiert werden.
  exit /b 1
)
copy /Y "%WPF_SOURCE_EXE%" "%WPF_RELEASE_REPO%\KGV-Setup-%TARGET_VERSION%.exe" >nul || (
  echo FEHLER: WPF EXE konnte nicht als versionsierte Datei ins KGV-WPF Repo kopiert werden.
  exit /b 1
)
copy /Y "%WPF_SOURCE_EXE%" "%WPF_RELEASE_REPO%\KGV-Setup.exe" >nul || (
  echo FEHLER: WPF EXE konnte nicht als KGV-Setup.exe ins KGV-WPF Repo kopiert werden.
  exit /b 1
)
echo.
echo =========================================================
echo Commit + Push im WPF-Release-Repo...
echo =========================================================
cd /d "%WPF_RELEASE_REPO%" || (
  echo FEHLER: Konnte nicht in das WPF-Release-Repo wechseln.
  exit /b 1
)

for /f "usebackq delims=" %%I in (`"%GIT%" rev-parse --abbrev-ref HEAD`) do set "WPF_GIT_BRANCH=%%I"
if not defined WPF_GIT_BRANCH set "WPF_GIT_BRANCH=main"

"%GIT%" status -sb
"%GIT%" add -A
"%GIT%" diff --cached --quiet
if errorlevel 1 (
  "%GIT%" commit -m "Release %TARGET_VERSION%"
  if errorlevel 1 (
    echo FEHLER: Git-Commit im KGV-WPF Repo fehlgeschlagen.
    exit /b 1
  )
  "%GIT%" push origin %WPF_GIT_BRANCH%
  if errorlevel 1 (
    echo FEHLER: Git-Push im KGV-WPF Repo fehlgeschlagen.
    exit /b 1
  )
) else (
  echo HINWEIS: Im KGV-WPF Repo gab es nichts zu committen.
)

echo.
echo =========================================================
echo Fertig. Erzeugte Release-Dateien
echo =========================================================
echo Version: %TARGET_VERSION%
echo Artefaktordner: %VERSION_ROOT%
echo.
echo Hauptdateien in publish\%TARGET_VERSION%:
echo   %VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.apk
echo   %VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.aab
echo   %VERSION_ROOT%\KGV.Setup-%TARGET_VERSION%.exe
echo.
echo Zusaetzliche Unterordner:
echo   %ANDROID_OUT%
echo   %WPF_OUT%
echo.
echo WPF Publish:
echo   %WPF_PUBLISH_OUT%
echo.
echo WPF Release-EXE als Setup-Namen:
echo   %WPF_SETUP_OUT%\KGV-Setup-%TARGET_VERSION%.exe
echo   %WPF_RELEASE_REPO%\KGV-Setup-%TARGET_VERSION%.exe
echo   %WPF_RELEASE_REPO%\KGV-Setup.exe
echo.
"%GIT%" -C "%WPF_RELEASE_REPO%" status -sb

exit /b 0
