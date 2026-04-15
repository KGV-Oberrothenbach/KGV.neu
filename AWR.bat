@echo off
setlocal EnableExtensions

REM =========================================================
REM KGV MAUI + WPF Release mit gemeinsamer Zielversion
REM - setzt MAUI + WPF auf dieselbe Zielversion
REM - erhoeht Android ApplicationVersion automatisch um 1
REM - speichert Artefakte unter publish\<Version>
REM - erstellt fuer WPF einen echten Inno-Setup-Installer
REM - verwendet als Setup-Dateiname: KGV-Setup-<Version>.exe
REM - synchronisiert das Repo KGV-WPF vor dem Kopieren per pull --rebase
REM - kopiert danach den Installer nach KGV-WPF und fuehrt Commit + Push aus
REM =========================================================

set "REPO=C:\Programmieren\KGV\KGV.neu"
set "WPF_RELEASE_REPO=C:\Programmieren\KGV\KGV-WPF"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "WPF_CSPROJ=%REPO%\KGV.Wpf\KGV.Wpf.csproj"
set "WPF_ISS=%REPO%\KGV.Wpf\Installer\KGV.Wpf.iss"
set "PUBLISH_ROOT=%REPO%\publish"

set "GIT="
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\cmd\git.exe" set "GIT=C:\Program Files\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\bin\git.exe" set "GIT=C:\Program Files\Git\bin\git.exe"

set "ISCC="
if exist "C:\Progra~2\Inno Setup 6\ISCC.exe" set "ISCC=C:\Progra~2\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "C:\Users\%USERNAME%\AppData\Local\Programs\Inno Setup 6\ISCC.exe" set "ISCC=C:\Users\%USERNAME%\AppData\Local\Programs\Inno Setup 6\ISCC.exe"

set "CERT_DIR=%REPO%\_secrets\Windows"
set "PFX_FILE=%CERT_DIR%\kgv-codesign.pfx"
set "CER_FILE=%CERT_DIR%\kgv-codesign.cer"
set "SIGNTOOL=C:\Progra~2\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"


cd /d "%REPO%" || (
  echo FEHLER: Repo-Pfad nicht gefunden: %REPO%
  exit /b 1
)

if not exist "%MAUI_CSPROJ%" (
  echo FEHLER: MAUI csproj nicht gefunden: %MAUI_CSPROJ%
  exit /b 1
)

if not exist "%WPF_CSPROJ%" (
  echo FEHLER: WPF csproj nicht gefunden: %WPF_CSPROJ%
  exit /b 1
)

if not exist "%WPF_ISS%" (
  echo FEHLER: Inno-Script nicht gefunden: %WPF_ISS%
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

if not defined ISCC (
  echo FEHLER: ISCC.exe wurde nicht gefunden.
  echo Erwartete Pfade:
  echo   C:\Program Files ^(x86^)\Inno Setup 6\ISCC.exe
  echo   C:\Program Files\Inno Setup 6\ISCC.exe
  echo   %LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe
  exit /b 1
)

if not exist "%SIGNTOOL%" (
  echo FEHLER: signtool.exe wurde nicht gefunden: %SIGNTOOL%
  exit /b 1
)

set "CURRENT_BRANCH="
for /f "delims=" %%I in ('""%GIT%" -C "%REPO%" branch --show-current 2^>nul"') do set "CURRENT_BRANCH=%%I"
if not defined CURRENT_BRANCH (
  for /f "delims=" %%I in ('""%GIT%" -C "%REPO%" rev-parse --abbrev-ref HEAD 2^>nul"') do set "CURRENT_BRANCH=%%I"
)
if not defined CURRENT_BRANCH (
  echo FEHLER: Aktueller Branch im Hauptrepo konnte nicht ermittelt werden.
  exit /b 1
)

echo.
echo =========================================================
echo KGV MAUI + WPF Release
echo Repo: %REPO%
echo WPF-Zielrepo: %WPF_RELEASE_REPO%
echo Quellrepo-Branch: %CURRENT_BRANCH%
echo =========================================================
echo.

echo Loesche alte Build-Artefakte...
if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"
if exist "%REPO%\KGV.Wpf\bin"  rmdir /s /q "%REPO%\KGV.Wpf\bin"
if exist "%REPO%\KGV.Wpf\obj"  rmdir /s /q "%REPO%\KGV.Wpf\obj"
if exist "%REPO%\KGV.Wpf\Installer\Output" rmdir /s /q "%REPO%\KGV.Wpf\Installer\Output"

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
set "ANDROID_UPLOAD_OUT=%ANDROID_OUT%\Upload"
set "ANDROID_DIAG_OUT=%ANDROID_OUT%\GooglePlay-Diagnose"
set "WPF_OUT=%VERSION_ROOT%\WPF"
set "WPF_PUBLISH_OUT=%WPF_OUT%\Publish"
set "WPF_SETUP_OUT=%WPF_OUT%\Setup"
set "WPF_SETUP_FILE=%WPF_SETUP_OUT%\KGV-Setup-%TARGET_VERSION%.exe"
set "VERSIONED_SETUP_NAME=KGV-Setup-%TARGET_VERSION%.exe"
set "LEGACY_VERSIONED_SETUP_NAME=KGV.Setup-%TARGET_VERSION%.exe"
set "LATEST_SETUP_NAME=KGV-Setup.exe"
set "VERSION_MANIFEST_NAME=version.json"
set "VERSION_MANIFEST_FILE=%WPF_SETUP_OUT%\%VERSION_MANIFEST_NAME%"
set "VERSION_MANIFEST_RELEASE_FILE=%VERSION_ROOT%\%VERSION_MANIFEST_NAME%"
set "WPF_RELEASE_BASE_URL=https://kgv-oberrothenbach.github.io/KGV-WPF"

if not exist "%PUBLISH_ROOT%" mkdir "%PUBLISH_ROOT%"
if not exist "%VERSION_ROOT%" mkdir "%VERSION_ROOT%"
if not exist "%ANDROID_OUT%" mkdir "%ANDROID_OUT%"
if not exist "%ANDROID_UPLOAD_OUT%" mkdir "%ANDROID_UPLOAD_OUT%"
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
set "KEYSTORE=C:\Programmieren\KGV\KGV.neu\_secrets\Android\kgv-upload.keystore"
set "KEYALIAS=kgvupload"

echo Verwende festen Keystore-Pfad: %KEYSTORE%
echo Verwende festen Key-Alias: %KEYALIAS%

if not exist "%KEYSTORE%" (
  echo FEHLER: Keystore-Datei nicht gefunden: %KEYSTORE%
  exit /b 1
)

if not defined KEYALIAS (
  echo FEHLER: Kein Key-Alias konfiguriert.
  exit /b 1
)

echo.
echo =========================================================
echo Passwortabfragen
echo =========================================================
echo Bitte Android-Keystore-Passwort eingeben...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$p = Read-Host 'Android-Keystore-Passwort' -AsSecureString;" ^
  "$BSTR = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);" ^
  "try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($BSTR) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR) }"`) do set "STOREPASS=%%P"

if not defined STOREPASS (
  echo FEHLER: Kein Android-Keystore-Passwort eingegeben.
  exit /b 1
)

set "KEYPASS=%STOREPASS%"

if not exist "%PFX_FILE%" (
  echo FEHLER: Code-Signing-PFX nicht gefunden: %PFX_FILE%
  exit /b 1
)

echo Bitte Windows-Code-Signing-Passwort eingeben...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$p = Read-Host 'Windows-Code-Signing-Passwort' -AsSecureString;" ^
  "$BSTR = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);" ^
  "try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($BSTR) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR) }"`) do set "SIGN_PWD=%%P"

if not defined SIGN_PWD (
  echo FEHLER: Kein Windows-Code-Signing-Passwort eingegeben.
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
  -p:AndroidKeyStore=true ^
  -p:AndroidSigningKeyStore="%KEYSTORE%" ^
  -p:AndroidSigningStorePass="%STOREPASS%" ^
  -p:AndroidSigningKeyAlias="%KEYALIAS%" ^
  -p:AndroidSigningKeyPass="%KEYPASS%"

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
  -p:AndroidKeyStore=true ^
  -p:AndroidSigningKeyStore="%KEYSTORE%" ^
  -p:AndroidSigningStorePass="%STOREPASS%" ^
  -p:AndroidSigningKeyAlias="%KEYALIAS%" ^
  -p:AndroidSigningKeyPass="%KEYPASS%"

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
echo Sammle Google-Play-Diagnose-Artefakte...
echo =========================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$repo = '%REPO%';" ^
  "$diagOut = '%ANDROID_DIAG_OUT%';" ^
  "$mappingOut = Join-Path $diagOut 'Mapping';" ^
  "$nativeOut = Join-Path $diagOut 'NativeDebugSymbols';" ^
  "$statusFile = Join-Path $diagOut 'STATUS.txt';" ^
  "New-Item -ItemType Directory -Force -Path $diagOut, $mappingOut, $nativeOut | Out-Null;" ^
  "$mappingPattern = Join-Path $mappingOut '*'; if (Test-Path $mappingPattern) { Remove-Item $mappingPattern -Recurse -Force -ErrorAction SilentlyContinue }" ^
  "$nativePattern = Join-Path $nativeOut '*'; if (Test-Path $nativePattern) { Remove-Item $nativePattern -Recurse -Force -ErrorAction SilentlyContinue }" ^
  "$searchRoots = @((Join-Path $repo 'KGV.Maui\bin\Release\net9.0-android'), (Join-Path $repo 'KGV.Maui\obj\Release\net9.0-android')) | Where-Object { Test-Path $_ };" ^
  "$mappingCandidates = foreach ($root in $searchRoots) { Get-ChildItem $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\lp\\' -and @('mapping.txt','proguard.map','proguard_mapping.txt') -contains $_.Name.ToLowerInvariant() } };" ^
  "$mappingFile = $mappingCandidates | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if ($mappingFile) { Copy-Item $mappingFile.FullName (Join-Path $mappingOut $mappingFile.Name) -Force }" ^
  "$nativeFiles = foreach ($root in $searchRoots) { Get-ChildItem $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\lp\\' -and (($_.Name.ToLowerInvariant() -in @('native-debug-symbols.zip','symbols.zip')) -or ($_.Extension.ToLowerInvariant() -in @('.dbg','.sym'))) } };" ^
  "$nativeDirs = foreach ($root in $searchRoots) { Get-ChildItem $root -Recurse -Directory -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\lp\\' -and ($_.Name -eq 'symbols' -or $_.Name -eq 'native-debug-symbols' -or $_.Name -eq 'app_shared_libraries' -or $_.Name -like '*.mSYM') } };" ^
  "$copiedNative = @();" ^
  "foreach ($file in ($nativeFiles | Sort-Object FullName -Unique)) { Copy-Item $file.FullName (Join-Path $nativeOut $file.Name) -Force; $copiedNative += $file.FullName }" ^
  "foreach ($dir in ($nativeDirs | Sort-Object FullName -Unique)) { $dest = Join-Path $nativeOut $dir.Name; if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }; Copy-Item $dir.FullName $dest -Recurse -Force; $copiedNative += $dir.FullName }" ^
  "$lines = @();" ^
  "$lines += 'Google-Play-Diagnose-Artefakte';" ^
  "$lines += 'Version: %TARGET_VERSION%';" ^
  "$lines += 'Release-Ordner: %VERSION_ROOT%';" ^
  "$lines += 'Android-Ordner: %ANDROID_OUT%';" ^
  "$lines += '';" ^
  "if ($mappingFile) { $lines += 'Mapping-Datei: vorhanden'; $lines += ('Quelle: ' + $mappingFile.FullName); $lines += ('Kopie:  ' + (Join-Path $mappingOut $mappingFile.Name)) } else { $lines += 'Mapping-Datei: nicht gefunden'; $lines += 'Hinweis: Im aktuellen MAUI-/Android-Release wurde keine app-spezifische R8/ProGuard-Mapping-Datei im Buildoutput gefunden.' }" ^
  "$lines += '';" ^
  "if ($copiedNative.Count -gt 0) { $lines += 'Native Debug-Symbole: vorhanden'; $lines += 'Quellen:'; $lines += ($copiedNative | ForEach-Object { '  - ' + $_ }) } else { $lines += 'Native Debug-Symbole: nicht gefunden'; $lines += 'Hinweis: Im aktuellen MAUI-/Android-Release wurden keine nativen Debug-Symbol-Artefakte im Buildoutput gefunden.' }" ^
  "$lines += '';" ^
  "$lines += 'Suchwurzeln:';" ^
  "$lines += ($searchRoots | ForEach-Object { '  - ' + $_ });" ^
  "Set-Content -Path $statusFile -Value $lines -Encoding UTF8;" ^
  "Write-Host ('Diagnose-Status geschrieben: ' + $statusFile);" ^
  "if ($mappingFile) { Write-Host ('Mapping-Datei übernommen: ' + $mappingFile.FullName) } else { Write-Host 'Keine Mapping-Datei gefunden.' }" ^
  "if ($copiedNative.Count -gt 0) { Write-Host ('Native Debug-Symbole übernommen: ' + $nativeOut) } else { Write-Host 'Keine nativen Debug-Symbole gefunden.' }"

if errorlevel 1 (
  echo FEHLER: Google-Play-Diagnose-Artefakte konnten nicht ausgewertet werden.
  exit /b 1
)

echo.
echo =========================================================
echo Erstelle Android-Upload-Ordner...
echo =========================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$uploadOut = '%ANDROID_UPLOAD_OUT%';" ^
  "$diagOut = '%ANDROID_DIAG_OUT%';" ^
  "$mappingOut = Join-Path $diagOut 'Mapping';" ^
  "$nativeOut = Join-Path $diagOut 'NativeDebugSymbols';" ^
  "$apkSource = '%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk';" ^
  "$aabSource = '%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab';" ^
  "$mappingTarget = Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%-mapping.txt';" ^
  "$nativeZipTarget = Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip';" ^
  "$readmeFile = Join-Path $uploadOut 'README.txt';" ^
  "New-Item -ItemType Directory -Force -Path $uploadOut | Out-Null;" ^
  "$uploadPattern = Join-Path $uploadOut '*'; if (Test-Path $uploadPattern) { Remove-Item $uploadPattern -Recurse -Force -ErrorAction SilentlyContinue }" ^
  "Copy-Item $apkSource (Join-Path $uploadOut ([System.IO.Path]::GetFileName($apkSource))) -Force;" ^
  "Copy-Item $aabSource (Join-Path $uploadOut ([System.IO.Path]::GetFileName($aabSource))) -Force;" ^
  "$mappingFile = Get-ChildItem $mappingOut -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if ($mappingFile) { Copy-Item $mappingFile.FullName $mappingTarget -Force }" ^
  "$nativeZipSource = Get-ChildItem $nativeOut -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Name.ToLowerInvariant() -in @('native-debug-symbols.zip','symbols.zip') } | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if ($nativeZipSource) { Copy-Item $nativeZipSource.FullName $nativeZipTarget -Force } else { $nativeItems = Get-ChildItem $nativeOut -Force -ErrorAction SilentlyContinue; if ($nativeItems.Count -gt 0) { Compress-Archive -Path ($nativeItems | ForEach-Object { $_.FullName }) -DestinationPath $nativeZipTarget -CompressionLevel Optimal -Force } }" ^
  "$lines = @();" ^
  "$lines += 'Android-Upload-Artefakte';" ^
  "$lines += 'Version: %TARGET_VERSION%';" ^
  "$lines += '';" ^
  "$lines += 'Play Console:';" ^
  "$lines += '  - AAB: fuer den Store-Upload';" ^
  "$lines += '  - Mapping: fuer Deobfuskation/Diagnose, falls vorhanden';" ^
  "$lines += '  - Native ZIP: fuer native Debug-Symbole, falls vorhanden';" ^
  "$lines += '';" ^
  "$lines += 'Direkte Verteilung/Website:';" ^
  "$lines += '  - APK: fuer manuelle Weitergabe/Installation';" ^
  "$lines += '';" ^
  "$lines += 'Status:';" ^
  "$lines += ('  - APK: ' + (Test-Path (Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%.apk')));" ^
  "$lines += ('  - AAB: ' + (Test-Path (Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%.aab')));" ^
  "$lines += ('  - Mapping: ' + (Test-Path $mappingTarget));" ^
  "$lines += ('  - Native ZIP: ' + (Test-Path $nativeZipTarget));" ^
  "Set-Content -Path $readmeFile -Value $lines -Encoding UTF8;" ^
  "Write-Host ('Upload-Ordner befuellt: ' + $uploadOut);" ^
  "Write-Host ('APK: ' + (Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%.apk'));" ^
  "Write-Host ('AAB: ' + (Join-Path $uploadOut 'KGV-Android-%TARGET_VERSION%.aab'));" ^
  "if (Test-Path $mappingTarget) { Write-Host ('Mapping: ' + $mappingTarget) } else { Write-Host 'Mapping: nicht vorhanden.' }" ^
  "if (Test-Path $nativeZipTarget) { Write-Host ('Native ZIP: ' + $nativeZipTarget) } else { Write-Host 'Native ZIP: nicht vorhanden.' }"

if errorlevel 1 (
  echo FEHLER: Android-Upload-Ordner konnte nicht erstellt werden.
  exit /b 1
)

echo.
echo =========================================================
echo Baue WPF Release-Binaries fuer Publish...
echo =========================================================
dotnet publish ".\KGV.Wpf\KGV.Wpf.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained false

if errorlevel 1 (
  echo FEHLER: WPF Publish-Build fehlgeschlagen.
  exit /b 1
)

echo.
echo =========================================================
echo Baue WPF Release fuer Inno Setup...
echo =========================================================
dotnet build ".\KGV.Wpf\KGV.Wpf.csproj" -c Release

if errorlevel 1 (
  echo FEHLER: WPF Release-Build fuer Inno Setup fehlgeschlagen.
  exit /b 1
)

set "WPF_PUBLISH_SOURCE_DIR=%REPO%\KGV.Wpf\bin\Release\net8.0-windows\win-x64\publish"
set "WPF_BUILD_SOURCE_DIR=%REPO%\KGV.Wpf\bin\Release\net8.0-windows"
set "WPF_BUILD_SOURCE_EXE=%WPF_BUILD_SOURCE_DIR%\KGV.Wpf.exe"
set "INNO_OUTPUT_FILE=%WPF_SETUP_FILE%"

if not exist "%WPF_BUILD_SOURCE_EXE%" (
  echo FEHLER: Erwartete WPF-Datei fuer Inno Setup wurde nicht gefunden:
  echo   %WPF_BUILD_SOURCE_EXE%
  exit /b 1
)

if not exist "%WPF_PUBLISH_SOURCE_DIR%" (
  echo FEHLER: Erwarteter WPF-Publish-Ordner wurde nicht gefunden:
  echo   %WPF_PUBLISH_SOURCE_DIR%
  exit /b 1
)

echo.
echo Kopiere WPF-Publish-Dateien nach %WPF_PUBLISH_OUT% ...
robocopy "%WPF_PUBLISH_SOURCE_DIR%" "%WPF_PUBLISH_OUT%" /E /NFL /NDL /NJH /NJS /NC /NS >nul
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
  echo FEHLER: WPF-Publish-Dateien konnten nicht nach %WPF_PUBLISH_OUT% kopiert werden.
  exit /b 1
)

echo.
echo =========================================================
echo Signiere WPF-Dateien...
echo =========================================================
"%SIGNTOOL%" sign /f "%PFX_FILE%" /p "%SIGN_PWD%" /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 "%WPF_BUILD_SOURCE_EXE%"
if errorlevel 1 (
  echo FEHLER: WPF Build-EXE konnte nicht signiert werden.
  exit /b 1
)

if exist "%WPF_PUBLISH_OUT%\KGV.Wpf.exe" (
  "%SIGNTOOL%" sign /f "%PFX_FILE%" /p "%SIGN_PWD%" /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 "%WPF_PUBLISH_OUT%\KGV.Wpf.exe"
  if errorlevel 1 (
    echo FEHLER: WPF Publish-EXE konnte nicht signiert werden.
    exit /b 1
  )
) else (
  echo FEHLER: KGV.Wpf.exe wurde im Publish-Ordner nicht gefunden.
  exit /b 1
)

echo.
echo =========================================================
echo Erstelle Inno-Setup-Installer...
echo =========================================================
"%ISCC%" /DAppVersion="%TARGET_VERSION%" /O"%WPF_SETUP_OUT%" /F"KGV-Setup-%TARGET_VERSION%" "%WPF_ISS%"

if errorlevel 1 (
  echo FEHLER: Inno-Setup-Compiler fehlgeschlagen.
  exit /b 1
)

if not exist "%INNO_OUTPUT_FILE%" (
  echo FEHLER: Erwarteter Setup-Installer wurde nicht gefunden:
  echo   %INNO_OUTPUT_FILE%
  exit /b 1
)

echo.
echo =========================================================
echo Signiere Setup-Installer...
echo =========================================================
"%SIGNTOOL%" sign /f "%PFX_FILE%" /p "%SIGN_PWD%" /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 "%INNO_OUTPUT_FILE%"
if errorlevel 1 (
  echo FEHLER: Setup-Installer konnte nicht signiert werden.
  exit /b 1
)

copy /Y "%INNO_OUTPUT_FILE%" "%VERSION_ROOT%\%VERSIONED_SETUP_NAME%" >nul || (
  echo FEHLER: Setup konnte nicht nach %VERSION_ROOT% kopiert werden.
  exit /b 1
)

echo.
echo =========================================================
echo Erzeuge version.json fuer WPF-Updatepruefung...
echo =========================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$manifestPath = '%VERSION_MANIFEST_FILE%';" ^
  "$manifestReleasePath = '%VERSION_MANIFEST_RELEASE_FILE%';" ^
  "$baseUrl = '%WPF_RELEASE_BASE_URL%'.TrimEnd('/');" ^
  "$version = '%TARGET_VERSION%';" ^
  "$latestSetupName = '%LATEST_SETUP_NAME%';" ^
  "$versionedSetupName = '%VERSIONED_SETUP_NAME%';" ^
  "$manifest = [ordered]@{" ^
  "  version = $version;" ^
  "  setupUrl = ($baseUrl + '/' + $latestSetupName);" ^
  "  versionedSetupUrl = ($baseUrl + '/' + $versionedSetupName);" ^
  "  publishedAt = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ');" ^
  "  mandatory = $false;" ^
  "  notes = '';" ^
  "};" ^
  "$json = $manifest | ConvertTo-Json -Depth 5;" ^
  "Set-Content -Path $manifestPath -Value $json -Encoding UTF8;" ^
  "Set-Content -Path $manifestReleasePath -Value $json -Encoding UTF8;" ^
  "Write-Host ('version.json erstellt: ' + $manifestPath);" ^
  "Write-Host ('version.json kopiert nach: ' + $manifestReleasePath);"

if errorlevel 1 (
  echo FEHLER: version.json konnte nicht erstellt werden.
  exit /b 1
)

echo.
echo =========================================================
echo Synchronisiere WPF-Release-Repo vor Dateikopie...
echo =========================================================
pushd "%WPF_RELEASE_REPO%" || (
  echo FEHLER: Konnte nicht in das WPF-Release-Repo wechseln.
  exit /b 1
)

if exist ".git\rebase-merge" (
  echo FEHLER: Im KGV-WPF-Repo laeuft bereits ein Rebase.
  popd
  exit /b 1
)
if exist ".git\rebase-apply" (
  echo FEHLER: Im KGV-WPF-Repo laeuft bereits ein Rebase.
  popd
  exit /b 1
)
if exist ".git\MERGE_HEAD" (
  echo FEHLER: Im KGV-WPF-Repo liegt bereits ein Merge-Konflikt vor.
  popd
  exit /b 1
)

set "WPF_REPO_DIRTY="
for /f %%I in ('"%GIT%" status --porcelain') do set "WPF_REPO_DIRTY=1"
if defined WPF_REPO_DIRTY (
  echo FEHLER: KGV-WPF-Repo ist nicht sauber. Bitte zuerst bereinigen.
  "%GIT%" status -sb
  popd
  exit /b 1
)

"%GIT%" pull --rebase origin main || (
  echo FEHLER: Git-Pull/Rebase im KGV-WPF-Repo fehlgeschlagen.
  popd
  exit /b 1
)

echo.
echo =========================================================
echo Kopiere Inno-Setup-Installer ins WPF-Release-Repo...
echo =========================================================
copy /Y "%INNO_OUTPUT_FILE%" "%WPF_RELEASE_REPO%\%LATEST_SETUP_NAME%" >nul || (
  echo FEHLER: KGV-Setup.exe konnte nicht kopiert werden.
  popd
  exit /b 1
)

copy /Y "%INNO_OUTPUT_FILE%" "%WPF_RELEASE_REPO%\%VERSIONED_SETUP_NAME%" >nul || (
  echo FEHLER: %VERSIONED_SETUP_NAME% konnte nicht kopiert werden.
  popd
  exit /b 1
)

copy /Y "%VERSION_MANIFEST_FILE%" "%WPF_RELEASE_REPO%\%VERSION_MANIFEST_NAME%" >nul || (
  echo FEHLER: version.json konnte nicht ins WPF-Release-Repo kopiert werden.
  popd
  exit /b 1
)

if exist "%WPF_RELEASE_REPO%\%LEGACY_VERSIONED_SETUP_NAME%" (
  del /F /Q "%WPF_RELEASE_REPO%\%LEGACY_VERSIONED_SETUP_NAME%"
)

echo.
echo =========================================================
echo Commit + Push im WPF-Release-Repo...
echo =========================================================
"%GIT%" status -sb
"%GIT%" add "%LATEST_SETUP_NAME%" "%VERSIONED_SETUP_NAME%" "%VERSION_MANIFEST_NAME%"
"%GIT%" rm --ignore-unmatch "%LEGACY_VERSIONED_SETUP_NAME%" >nul 2>&1
"%GIT%" diff --cached --quiet
if errorlevel 1 (
  "%GIT%" commit -m "Release %TARGET_VERSION%" || (
    echo FEHLER: Git-Commit im KGV-WPF Repo fehlgeschlagen.
    popd
    exit /b 1
  )
  "%GIT%" push origin main || (
    echo FEHLER: Git-Push im KGV-WPF Repo fehlgeschlagen.
    echo HINWEIS: Wenn in der Zwischenzeit erneut remote gepusht wurde, Batch einfach erneut starten.
    popd
    exit /b 1
  )
) else (
  echo HINWEIS: Im KGV-WPF Repo gab es nichts zu committen.
)

popd

echo.
echo =========================================================
echo Commit + Push der Versionsdateien im Hauptrepo...
echo =========================================================
"%GIT%" -C "%REPO%" status -sb
"%GIT%" -C "%REPO%" add "KGV.Maui/KGV.Maui.csproj" "KGV.Wpf/KGV.Wpf.csproj"
"%GIT%" -C "%REPO%" diff --cached --quiet
if errorlevel 1 (
  "%GIT%" -C "%REPO%" commit -m "Release %TARGET_VERSION% Versionsdateien aktualisiert" || (
    echo FEHLER: Git-Commit der csproj-Dateien im Hauptrepo fehlgeschlagen.
    exit /b 1
  )
  "%GIT%" -C "%REPO%" push origin "%CURRENT_BRANCH%" || (
    echo FEHLER: Git-Push der csproj-Dateien im Hauptrepo fehlgeschlagen.
    echo Zielbranch im Hauptrepo: %CURRENT_BRANCH%
    echo HINWEIS: Wenn in der Zwischenzeit erneut remote gepusht wurde, Batch einfach erneut starten.
    exit /b 1
  )
) else (
  echo HINWEIS: Im Hauptrepo gab es bei den csproj-Dateien nichts zu committen.
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
echo   %VERSION_ROOT%\%VERSIONED_SETUP_NAME%
echo.
echo Google-Play-Diagnose:
echo   %ANDROID_DIAG_OUT%
echo   %ANDROID_DIAG_OUT%\STATUS.txt
echo.
echo Android Upload:
echo   %ANDROID_UPLOAD_OUT%
echo   %ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%.apk
echo   %ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%.aab
echo   %ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt
echo   %ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip
echo   %ANDROID_UPLOAD_OUT%\README.txt
echo.
echo Zusaetzliche Unterordner:
echo   %ANDROID_OUT%
echo   %WPF_OUT%
echo.
echo WPF Publish:
echo   %WPF_PUBLISH_OUT%
echo.
echo WPF Setup:
echo   %WPF_SETUP_FILE%
echo   %VERSION_MANIFEST_RELEASE_FILE%
echo   %WPF_RELEASE_REPO%\%VERSIONED_SETUP_NAME%
echo   %WPF_RELEASE_REPO%\%LATEST_SETUP_NAME%
echo   %WPF_RELEASE_REPO%\%VERSION_MANIFEST_NAME%
echo.
"%GIT%" -C "%WPF_RELEASE_REPO%" status -sb

set "STOREPASS="
set "KEYPASS="
set "SIGN_PWD="

exit /b 0
