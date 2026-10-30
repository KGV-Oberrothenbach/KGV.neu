@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "SUCCESS_SOUND=C:\Users\Andreas\Music\phantom_oper_benachrichtigung.wav"

call :MAIN
set "FINAL_EXIT_CODE=%ERRORLEVEL%"

call :PLAY_SOUND

exit /b %FINAL_EXIT_CODE%


:MAIN
cls

REM KGV MAUI + Web Release. Every successful Web build is deployed to Cloudflare.
set "REPO=C:\Programmieren\KGV\KGV.neu"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "WEB_ROOT=%REPO%\KGV.Web"
set "WEB_PACKAGE=%WEB_ROOT%\package.json"
set "PUBLISH_ROOT=%REPO%\publish"
set "WEB_PROD_WORKER_NAME=kgv-web"
set "WEB_TEST_WORKER_NAME=kgv-web-test"
set "WEB_PROD_HOST=https://app.a-b-i.net"
set "WEB_TEST_HOST=https://test-app.a-b-i.net"

set "GIT="
if exist "C:\Program Files\Git\cmd\git.exe" set "GIT=C:\Program Files\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\bin\git.exe" set "GIT=C:\Program Files\Git\bin\git.exe"
if not defined GIT for %%G in (git.exe) do set "GIT=%%~$PATH:G"
if not exist "%MAUI_CSPROJ%" (echo FEHLER: MAUI-Projekt fehlt.& exit /b 1)
if not exist "%WEB_PACKAGE%" (echo FEHLER: Web package.json fehlt.& exit /b 1)
if not defined GIT (echo FEHLER: git.exe wurde nicht gefunden.& exit /b 1)
where npm >nul 2>&1 || (echo FEHLER: npm wurde nicht gefunden.& exit /b 1)
cd /d "%REPO%" || exit /b 1

set "GIT_RESULT=%TEMP%\kgv-awr-git-%RANDOM%-%RANDOM%.txt"
set "PS_RESULT=%TEMP%\kgv-awr-ps-%RANDOM%-%RANDOM%.txt"
call :DELETE_TEMP

REM Use files for external command results: no PowerShell inside for /f.
"%GIT%" -C "%REPO%" branch --show-current > "%GIT_RESULT%" 2>nul
set "CURRENT_BRANCH="
for %%Z in ("%GIT_RESULT%") do if %%~zZ GTR 0 set /p CURRENT_BRANCH=<"%GIT_RESULT%"
if not defined CURRENT_BRANCH (
  "%GIT%" -C "%REPO%" rev-parse --abbrev-ref HEAD > "%GIT_RESULT%" 2>nul
  for %%Z in ("%GIT_RESULT%") do if %%~zZ GTR 0 set /p CURRENT_BRANCH=<"%GIT_RESULT%"
)
if not defined CURRENT_BRANCH (echo FEHLER: Branch konnte nicht ermittelt werden.& goto FAILURE_END)
"%GIT%" -C "%REPO%" rev-parse --short=12 HEAD > "%GIT_RESULT%" 2>nul
set "CURRENT_COMMIT="
for %%Z in ("%GIT_RESULT%") do if %%~zZ GTR 0 set /p CURRENT_COMMIT=<"%GIT_RESULT%"
if not defined CURRENT_COMMIT (echo FEHLER: Commit konnte nicht ermittelt werden.& goto FAILURE_END)
"%GIT%" -C "%REPO%" status --porcelain > "%GIT_RESULT%" 2>nul
for %%Z in ("%GIT_RESULT%") do if %%~zZ GTR 0 set "REPO_DIRTY=1"
if defined REPO_DIRTY (
  echo FEHLER: Das Repo ist nicht sauber.
  "%GIT%" -C "%REPO%" status -sb
  goto FAILURE_END
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';$x=[xml](Get-Content '%MAUI_CSPROJ%');$g=@($x.Project.PropertyGroup | Where-Object {$_.ApplicationDisplayVersion -or $_.ApplicationVersion -or $_.AndroidTargetSdkVersion});$d=($g | ForEach-Object {if($_.ApplicationDisplayVersion){[string]$_.ApplicationDisplayVersion}} | Select-Object -First 1);$c=@($g | ForEach-Object {if($_.ApplicationVersion){[int]$_.ApplicationVersion}});$s=($g | ForEach-Object {if($_.AndroidTargetSdkVersion){[string]$_.AndroidTargetSdkVersion}} | Select-Object -First 1);if(-not $d -or $c.Count -eq 0 -or -not $s -or $d -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$'){throw 'Ungueltige MAUI-Versionseinstellungen'};Write-Output ($d+'|'+(($c | Measure-Object -Maximum).Maximum)+'|'+$s)" > "%PS_RESULT%"
if errorlevel 1 (echo FEHLER: MAUI-Versionen konnten nicht gelesen werden.& goto FAILURE_END)
set "VERLINE=" & set /p VERLINE=<"%PS_RESULT%"
for /f "tokens=1,2,3 delims=|" %%A in ("%VERLINE%") do (set "CUR_MAUI_DISPLAY=%%A" & set "CUR_MAUI_CODE=%%B" & set "ANDROID_TARGET_SDK=%%C")
if not defined CUR_MAUI_DISPLAY (echo FEHLER: Ungueltige MAUI-Versionsausgabe.& goto FAILURE_END)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';$p='%CUR_MAUI_DISPLAY%'.Split('.');$a=[int]$p[0];$b=[int]$p[1];$c=[int]$p[2];$test=if($p.Count -eq 3){'{0}.{1}.{2}.1' -f $a,$b,$c}else{'{0}.{1}.{2}.{3}' -f $a,$b,$c,(([int]$p[3])+1)};$release='{0}.{1}.{2}' -f $a,$b,($c+1);Write-Output ($test+'|'+$release)" > "%PS_RESULT%"
if errorlevel 1 (echo FEHLER: Zielversionen konnten nicht berechnet werden.& goto FAILURE_END)
set "PROPOSALS=" & set /p PROPOSALS=<"%PS_RESULT%"
for /f "tokens=1,2 delims=|" %%A in ("%PROPOSALS%") do (set "TEST_VERSION=%%A" & set "RELEASE_VERSION=%%B")
if not defined TEST_VERSION (echo FEHLER: Ungueltige Versionsvorschlaege.& goto FAILURE_END)

echo.
echo KGV MAUI + Web Release - Branch %CURRENT_BRANCH%, Commit %CURRENT_COMMIT%
echo Aktuelle MAUI-Version: %CUR_MAUI_DISPLAY% (Android-Code %CUR_MAUI_CODE%, SDK %ANDROID_TARGET_SDK%)
echo [1] Test / Zwischenstand (%TEST_VERSION%)
echo [2] Release (%RELEASE_VERSION%)
echo [3] Abbrechen
choice /C 123 /N /M "Auswahl"
if errorlevel 3 goto CANCEL_RELEASE
if errorlevel 2 (set "RELEASE_KIND=Release" & set "SUGGESTED_VERSION=%RELEASE_VERSION%") else (set "RELEASE_KIND=Test / Zwischenstand" & set "SUGGESTED_VERSION=%TEST_VERSION%")
set "WEB_ONLY="
set "BUILD_SCOPE=MAUI + Web"
if /I "%RELEASE_KIND%"=="Test / Zwischenstand" (
  echo.
  echo Testumfang:
  echo [1] MAUI + Web
  echo [2] Nur Web
  choice /C 12 /N /M "Auswahl"
  if errorlevel 2 (set "WEB_ONLY=1" & set "BUILD_SCOPE=Nur Web")
)
if defined WEB_ONLY goto CONFIGURE_WEB_ONLY_TEST

set "TARGET_VERSION=" & set /p TARGET_VERSION=Zielversion [%SUGGESTED_VERSION%]:
if not defined TARGET_VERSION set "TARGET_VERSION=%SUGGESTED_VERSION%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';$v='%TARGET_VERSION%'.Trim();if($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$'){throw 'ungueltig'};Write-Output $v" > "%PS_RESULT%"
if errorlevel 1 (echo FEHLER: Zielversion muss drei oder vier numerische Teile haben.& goto FAILURE_END)
set "TARGET_VERSION=" & set /p TARGET_VERSION=<"%PS_RESULT%"
if not defined TARGET_VERSION goto FAILURE_END
set "WEB_REVISION=0"
set "WEB_DISPLAY_VERSION=%TARGET_VERSION%"
goto CHECK_RELEASE_BRANCH

:CONFIGURE_WEB_ONLY_TEST
REM A Web-only test never changes the MAUI application version. Its separate
REM revision is persisted in the copied Web release metadata.
set "TARGET_VERSION=%CUR_MAUI_DISPLAY%"
set "WEB_REVISION=1"
set "EXISTING_WEB_RELEASE_INFO=%PUBLISH_ROOT%\%TARGET_VERSION%\Web\dist\client\release-info.json"
if not exist "%EXISTING_WEB_RELEASE_INFO%" goto WEB_ONLY_VERSION_READY
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';$r=Get-Content -Raw '%EXISTING_WEB_RELEASE_INFO%' | ConvertFrom-Json;if($null -eq $r.webRevision){Write-Output 1;exit 0};$n=[int]$r.webRevision;if($n -lt 0){throw 'Web-Revision ist ungueltig'};Write-Output ($n + 1)" > "%PS_RESULT%"
if errorlevel 1 (echo FEHLER: Vorhandene Web-Revision konnte nicht gelesen werden.& goto FAILURE_END)
set "WEB_REVISION=" & set /p WEB_REVISION=<"%PS_RESULT%"
if not defined WEB_REVISION (echo FEHLER: Vorhandene Web-Revision ist leer.& goto FAILURE_END)
:WEB_ONLY_VERSION_READY
set "WEB_DISPLAY_VERSION=%TARGET_VERSION% / Web %WEB_REVISION%"
echo.
echo Web-only-Testversion: %WEB_DISPLAY_VERSION%

:CHECK_RELEASE_BRANCH
if /I "%RELEASE_KIND%"=="Release" if /I not "%CURRENT_BRANCH%"=="main" (
  echo WARNUNG: Release von "%CURRENT_BRANCH%" statt main.
  choice /C JN /N /M "Trotzdem fortsetzen? [J/N]"
  if errorlevel 2 goto CANCEL_RELEASE
)

if not defined WEB_ONLY set /a NEW_ANDROID_CODE=%CUR_MAUI_CODE%+1
set "VERSION_ROOT=%PUBLISH_ROOT%\%TARGET_VERSION%"
set "ANDROID_OUT=%VERSION_ROOT%\Android"
set "ANDROID_UPLOAD_OUT=%ANDROID_OUT%\Upload"
set "ANDROID_DIAG_OUT=%ANDROID_OUT%\GooglePlay-Diagnose"
set "ANDROID_PUBLISH=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish"
set "WEB_OUT=%VERSION_ROOT%\Web"
set "WEB_DIST=%WEB_ROOT%\dist"
set "WEB_CLIENT_DIST=%WEB_DIST%\client"
set "WEB_RELEASE_INFO=%WEB_CLIENT_DIST%\release-info.json"
echo.
echo Art: %RELEASE_KIND% / Umfang: %BUILD_SCOPE% / Version: %WEB_DISPLAY_VERSION%
if not defined WEB_ONLY echo Android-Code: %NEW_ANDROID_CODE%
echo Cloudflare-Deployment: wird nach erfolgreichem Web-Build ausgefuehrt
choice /C JN /N /M "%BUILD_SCOPE% jetzt bauen? [J/N]"
if errorlevel 2 goto CANCEL_RELEASE

if defined WEB_ONLY goto BUILD_WEB
set "VERSION_CHANGED=1"
call :AWR_STEP "MAUI-Version setzen" "%MAUI_CSPROJ%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';$x=[xml](Get-Content '%MAUI_CSPROJ%');$g=@($x.Project.PropertyGroup | Where-Object {$_.ApplicationDisplayVersion -or $_.ApplicationVersion});if($g.Count -eq 0){throw 'Versionseinstellungen fehlen'};foreach($p in $g){$p.ApplicationDisplayVersion='%TARGET_VERSION%';$p.ApplicationVersion='%NEW_ANDROID_CODE%'};$x.Save('%MAUI_CSPROJ%')"
if errorlevel 1 goto BUILD_FAILED

set "KEYSTORE=%REPO%\_secrets\Android\kgv-upload.keystore"
set "KEYALIAS=kgvupload"
call :AWR_STEP "Keystore pruefen" "%KEYSTORE%"
if not exist "%KEYSTORE%" (echo FEHLER: Keystore-Datei nicht gefunden.& goto BUILD_FAILED)
call :AWR_STEP "Keystore-Passwort lesen" "%PS_RESULT%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$p=Read-Host 'Android-Keystore-Passwort' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b)}" > "%PS_RESULT%"
if errorlevel 1 goto BUILD_FAILED
set "STOREPASS=" & set /p STOREPASS=<"%PS_RESULT%"
if not defined STOREPASS goto BUILD_FAILED

if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"
call :AWR_STEP "Android-Publish APK" "%ANDROID_PUBLISH%"
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED
call :AWR_STEP "Android-Publish AAB" "%ANDROID_PUBLISH%"
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED

call :AWR_STEP "Signed-Artefakte suchen" "%ANDROID_PUBLISH%"
set "APK_FILE=" & set "AAB_FILE="
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*-Signed.apk" 2^>nul') do if not defined APK_FILE set "APK_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*-Signed.aab" 2^>nul') do if not defined AAB_FILE set "AAB_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
if not defined APK_FILE (set "AWR_FAILURE_CODE=1" & set "AWR_FAILURE_DETAIL=Signed APK nicht gefunden: %ANDROID_PUBLISH%\*-Signed.apk" & goto BUILD_FAILED)
if not defined AAB_FILE (set "AWR_FAILURE_CODE=1" & set "AWR_FAILURE_DETAIL=Signed AAB nicht gefunden: %ANDROID_PUBLISH%\*-Signed.aab" & goto BUILD_FAILED)

REM Only the signed MAUI outputs are handed to the versioned release folder.
REM Do not create additional version-named APK/AAB copies in the MAUI bin folder.
set "CENTRAL_APK=%APK_FILE%"
set "CENTRAL_AAB=%AAB_FILE%"
set "CENTRAL_SYMBOLS=%ANDROID_PUBLISH%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip"
set "CENTRAL_MAPPING=%ANDROID_PUBLISH%\KGV-Android-%TARGET_VERSION%-mapping.txt"
call :AWR_STEP "Vorherige Native-Debugsymbole entfernen" "%CENTRAL_SYMBOLS%"
if exist "%CENTRAL_SYMBOLS%" (
  del /q "%CENTRAL_SYMBOLS%" || goto BUILD_FAILED
)
call :AWR_STEP "Vorherige Mapping-Datei entfernen" "%CENTRAL_MAPPING%"
if exist "%CENTRAL_MAPPING%" (
  del /q "%CENTRAL_MAPPING%" || goto BUILD_FAILED
)
call :AWR_STEP "Native Debugsymbole erstellen" "%REPO%\KGV.Maui\obj\Release\net10.0-android\app_shared_libraries"
set "SYMBOL_RESULT="
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';Add-Type -AssemblyName System.IO.Compression.FileSystem;$root='%REPO%\KGV.Maui\obj\Release\net10.0-android\app_shared_libraries';$dest='%ANDROID_PUBLISH%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip';$files=@();if(Test-Path -LiteralPath $root){foreach($abi in Get-ChildItem -LiteralPath $root -Directory){foreach($file in Get-ChildItem -LiteralPath $abi.FullName -Recurse -File -Filter '*.so'){$files+=,[pscustomobject]@{Path=$file.FullName;Entry=($abi.Name+'/'+$file.Name)}}}};if($files.Count -eq 0){Write-Output 'NO_SYMBOLS';exit 0};$duplicates=@($files|Group-Object Entry|Where-Object {$_.Count -gt 1});if($duplicates.Count -gt 0){throw 'Native-Symbol-Dateinamen sind innerhalb einer ABI nicht eindeutig'};$archive=[IO.Compression.ZipFile]::Open($dest,[IO.Compression.ZipArchiveMode]::Create);try{foreach($file in $files){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.Path,$file.Entry,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$archive.Dispose()};$check=[IO.Compression.ZipFile]::OpenRead($dest);try{$entries=@($check.Entries);$invalid=@($entries|Where-Object {$parts=$_.FullName.Split('/');$parts.Count -ne 2 -or -not $parts[1].EndsWith('.so')});if($entries.Count -lt 1 -or $invalid.Count -gt 0){throw 'Native-Symbol-ZIP hat eine ungueltige Struktur'};$abis=@($entries|ForEach-Object {$_.FullName.Split('/')[0]}|Sort-Object -Unique);Write-Output ('SYMBOLS|'+$entries.Count+'|'+($abis -join ','))}finally{$check.Dispose()}" > "%PS_RESULT%"
if errorlevel 1 goto BUILD_FAILED
set /p SYMBOL_RESULT=<"%PS_RESULT%"
if /I "%SYMBOL_RESULT%"=="NO_SYMBOLS" (
  set "CENTRAL_SYMBOLS="
  echo WARNUNG: Keine nativen .so-Dateien fuer das Google-Play-Symbol-ZIP gefunden.
) else (
  for /f "tokens=1,2,3 delims=|" %%A in ("%SYMBOL_RESULT%") do (
    echo Native Debugsymbole: %%B .so-Dateien ^(ABIs: %%C^)
  )
)
call :AWR_STEP "Mapping-Datei suchen" "%REPO%\KGV.Maui\bin\Release\net10.0-android"
set "MAPPING_FILE="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$roots=@('%REPO%\KGV.Maui\bin\Release\net10.0-android','%REPO%\KGV.Maui\obj\Release\net10.0-android');Get-ChildItem $roots -Recurse -File -Filter 'mapping.txt' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName"`) do if not defined MAPPING_FILE set "MAPPING_FILE=%%I"
if defined MAPPING_FILE (
  call :AWR_STEP "Mapping-Datei kopieren" "%MAPPING_FILE%"
  copy /Y "%MAPPING_FILE%" "%CENTRAL_MAPPING%" >nul || goto BUILD_FAILED
  echo Offenlegungsdatei: KGV-Android-%TARGET_VERSION%-mapping.txt
) else (
  set "CENTRAL_MAPPING="
  echo Keine Offenlegungsdatei erzeugt - R8/Code Shrinker ist nicht aktiviert.
)
call :AWR_STEP "Google-Play-Begleitdatei erstellen" "%ANDROID_PUBLISH%\GooglePlay-Upload.txt"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';$symbols=if(Test-Path -LiteralPath '%ANDROID_PUBLISH%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip'){'KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip'}else{'nicht erzeugt - keine nativen .so-Dateien gefunden'};$mapping=if(Test-Path -LiteralPath '%ANDROID_PUBLISH%\KGV-Android-%TARGET_VERSION%-mapping.txt'){'KGV-Android-%TARGET_VERSION%-mapping.txt'}else{'nicht erzeugt - R8 nicht aktiviert'};$lines=@('KGV Android Release','','Version: %TARGET_VERSION%','VersionCode: %NEW_ANDROID_CODE%','Branch: %CURRENT_BRANCH%','Commit: %CURRENT_COMMIT%','','AAB:','de.kgv.oberrothenbach-Signed.aab','','APK:','de.kgv.oberrothenbach-Signed.apk','','Native Debugsymbole:',$symbols,'','Offenlegungsdatei:',$mapping,'','Erzeugt: '+[DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'));Set-Content -LiteralPath '%ANDROID_PUBLISH%\GooglePlay-Upload.txt' -Value $lines -Encoding UTF8"
if errorlevel 1 goto BUILD_FAILED

:BUILD_WEB
call :AWR_STEP "Web-Build" "%WEB_ROOT%"
pushd "%WEB_ROOT%" || goto BUILD_FAILED
set "KGV_RELEASE_VERSION=%TARGET_VERSION%"
set "KGV_RELEASE_DISPLAY_VERSION=%WEB_DISPLAY_VERSION%"
set "KGV_WEB_REVISION=%WEB_REVISION%"
set "KGV_RELEASE_BRANCH=%CURRENT_BRANCH%"
set "KGV_RELEASE_COMMIT=%CURRENT_COMMIT%"
set "KGV_RELEASE_KIND=%RELEASE_KIND%"
set "KGV_RELEASE_SCOPE=%BUILD_SCOPE%"
call npm run build
set "WEB_BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%WEB_BUILD_RESULT%"=="0" (set "AWR_FAILURE_CODE=%WEB_BUILD_RESULT%" & set "AWR_FAILURE_DETAIL=npm run build in %WEB_ROOT%" & goto BUILD_FAILED)
if not exist "%WEB_CLIENT_DIST%" (set "AWR_FAILURE_CODE=1" & set "AWR_FAILURE_DETAIL=Web-Client-Dist fehlt: %WEB_CLIENT_DIST%" & goto BUILD_FAILED)
call :AWR_STEP "Web-Release-Info erstellen" "%WEB_RELEASE_INFO%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';[ordered]@{version='%TARGET_VERSION%';displayVersion='%WEB_DISPLAY_VERSION%';webRevision='%WEB_REVISION%';branch='%CURRENT_BRANCH%';commit='%CURRENT_COMMIT%';kind='%RELEASE_KIND%';scope='%BUILD_SCOPE%';builtAtUtc=[DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')} | ConvertTo-Json | Set-Content -Path '%WEB_RELEASE_INFO%' -Encoding UTF8"
if errorlevel 1 goto BUILD_FAILED

if defined WEB_ONLY goto COPY_WEB
call :AWR_STEP "Release-Ordner erstellen" "%VERSION_ROOT%"
if not exist "%ANDROID_UPLOAD_OUT%" mkdir "%ANDROID_UPLOAD_OUT%" || goto BUILD_FAILED
if not exist "%ANDROID_DIAG_OUT%" mkdir "%ANDROID_DIAG_OUT%" || goto BUILD_FAILED
if not exist "%WEB_OUT%" mkdir "%WEB_OUT%" || goto BUILD_FAILED
call :AWR_STEP "Android-Release-Ordner bereinigen" "%ANDROID_OUT%"
call :CLEAN_ANDROID_RELEASE_OUTPUT "%ANDROID_OUT%" || goto BUILD_FAILED
call :CLEAN_ANDROID_RELEASE_OUTPUT "%ANDROID_UPLOAD_OUT%" || goto BUILD_FAILED
call :AWR_STEP "Signed-Artefakte kopieren" "%ANDROID_OUT%"
copy /Y "%CENTRAL_APK%" "%ANDROID_OUT%\de.kgv.oberrothenbach-Signed.apk" >nul || goto BUILD_FAILED
copy /Y "%CENTRAL_AAB%" "%ANDROID_OUT%\de.kgv.oberrothenbach-Signed.aab" >nul || goto BUILD_FAILED
copy /Y "%CENTRAL_APK%" "%ANDROID_UPLOAD_OUT%\de.kgv.oberrothenbach-Signed.apk" >nul || goto BUILD_FAILED
copy /Y "%CENTRAL_AAB%" "%ANDROID_UPLOAD_OUT%\de.kgv.oberrothenbach-Signed.aab" >nul || goto BUILD_FAILED
if not defined CENTRAL_SYMBOLS (
  call :AWR_STEP "Nicht vorhandene Debugsymbole bereinigen" "%ANDROID_OUT%"
  if exist "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" (
    del /q "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" || goto BUILD_FAILED
  )
  if exist "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" (
    del /q "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" || goto BUILD_FAILED
  )
)
if defined CENTRAL_SYMBOLS (
  call :AWR_STEP "Native Debugsymbole kopieren" "%CENTRAL_SYMBOLS%"
  copy /Y "%CENTRAL_SYMBOLS%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" >nul || goto BUILD_FAILED
  copy /Y "%CENTRAL_SYMBOLS%" "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-native-debug-symbols.zip" >nul || goto BUILD_FAILED
)
if not defined CENTRAL_MAPPING (
  call :AWR_STEP "Nicht vorhandene Mapping-Datei bereinigen" "%ANDROID_OUT%"
  if exist "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" (
    del /q "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" || goto BUILD_FAILED
  )
  if exist "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" (
    del /q "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" || goto BUILD_FAILED
  )
)
if defined CENTRAL_MAPPING (
  call :AWR_STEP "Mapping-Datei verteilen" "%CENTRAL_MAPPING%"
  copy /Y "%CENTRAL_MAPPING%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" >nul || goto BUILD_FAILED
  copy /Y "%CENTRAL_MAPPING%" "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%-mapping.txt" >nul || goto BUILD_FAILED
)
call :AWR_STEP "Google-Play-Begleitdatei kopieren" "%ANDROID_PUBLISH%\GooglePlay-Upload.txt"
copy /Y "%ANDROID_PUBLISH%\GooglePlay-Upload.txt" "%ANDROID_OUT%\GooglePlay-Upload.txt" >nul || goto BUILD_FAILED
copy /Y "%ANDROID_PUBLISH%\GooglePlay-Upload.txt" "%ANDROID_UPLOAD_OUT%\GooglePlay-Upload.txt" >nul || goto BUILD_FAILED
:COPY_WEB
call :AWR_STEP "Web-Artefakte kopieren" "%WEB_OUT%\dist"
if not exist "%WEB_OUT%" mkdir "%WEB_OUT%" || goto BUILD_FAILED
robocopy "%WEB_DIST%" "%WEB_OUT%\dist" /E /NFL /NDL /NJH /NJS /NC /NS >nul
set "WEB_COPY_RESULT=%ERRORLEVEL%"
if %WEB_COPY_RESULT% GEQ 8 (set "AWR_FAILURE_CODE=%WEB_COPY_RESULT%" & set "AWR_FAILURE_DETAIL=robocopy: %WEB_DIST% -> %WEB_OUT%\dist" & goto BUILD_FAILED)
call :AWR_STEP "Cloudflare-Deployment" "%WEB_ROOT%"
call :DEPLOY_WEB
if errorlevel 1 goto BUILD_FAILED

if defined WEB_ONLY goto WEB_ONLY_SUCCESS
call :AWR_STEP "Release-Commit erstellen" "%REPO%"
"%GIT%" -C "%REPO%" add "KGV.Maui/KGV.Maui.csproj"
"%GIT%" -C "%REPO%" commit -m "%RELEASE_KIND% %TARGET_VERSION% MAUI/Web" || goto BUILD_FAILED
call :AWR_STEP "Release-Commit pushen" "origin/%CURRENT_BRANCH%"
"%GIT%" -C "%REPO%" push origin "%CURRENT_BRANCH%" || goto BUILD_FAILED
echo RELEASE FERTIG: %TARGET_VERSION%
goto RELEASE_END

:WEB_ONLY_SUCCESS
echo WEB-ONLY-TEST FERTIG: %WEB_DISPLAY_VERSION%
echo MAUI-Version unveraendert: %CUR_MAUI_DISPLAY%
goto RELEASE_END

:DEPLOY_WEB
if /I "%RELEASE_KIND%"=="Release" (
  set "WEB_WORKER_NAME=%WEB_PROD_WORKER_NAME%"
  set "WEB_DEPLOY_HOST=%WEB_PROD_HOST%"
) else (
  set "WEB_WORKER_NAME=%WEB_TEST_WORKER_NAME%"
  set "WEB_DEPLOY_HOST=%WEB_TEST_HOST%"
)
pushd "%WEB_ROOT%" || exit /b 1
call npx wrangler deploy --config dist\server\wrangler.json --name "%WEB_WORKER_NAME%"
set "DEPLOY_RESULT=%ERRORLEVEL%"
popd
if not "%DEPLOY_RESULT%"=="0" exit /b %DEPLOY_RESULT%
echo Web deployed: %WEB_DEPLOY_HOST%
exit /b 0

:CANCEL_RELEASE
echo Release abgebrochen. Die Versionsdatei wurde nicht geaendert.
goto RELEASE_END

:BUILD_FAILED
set "BUILD_FAILED_CAPTURED_ERRORLEVEL=%ERRORLEVEL%"
if defined AWR_FAILURE_CODE set "BUILD_FAILED_CAPTURED_ERRORLEVEL=%AWR_FAILURE_CODE%"
echo FEHLER: Schritt fehlgeschlagen: %AWR_STEP%
echo FEHLER: ERRORLEVEL: %BUILD_FAILED_CAPTURED_ERRORLEVEL%
if defined AWR_STEP_PATH echo FEHLER: Betroffener Pfad: %AWR_STEP_PATH%
if defined AWR_FAILURE_DETAIL echo FEHLER: Detail: %AWR_FAILURE_DETAIL%
echo FEHLER: Release fehlgeschlagen. Setze die MAUI-Version zurueck...
if defined VERSION_CHANGED powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';$x=[xml](Get-Content '%MAUI_CSPROJ%');foreach($p in @($x.Project.PropertyGroup | Where-Object {$_.ApplicationDisplayVersion -or $_.ApplicationVersion})){$p.ApplicationDisplayVersion='%CUR_MAUI_DISPLAY%';$p.ApplicationVersion='%CUR_MAUI_CODE%'};$x.Save('%MAUI_CSPROJ%')"
goto FAILURE_END

:FAILURE_END
call :DELETE_TEMP
set "STOREPASS=" & exit /b 1

:RELEASE_END
call :DELETE_TEMP
set "STOREPASS=" & exit /b 0

:DELETE_TEMP
if defined GIT_RESULT del /q "%GIT_RESULT%" >nul 2>&1
if defined PS_RESULT del /q "%PS_RESULT%" >nul 2>&1
exit /b 0

:AWR_STEP
set "AWR_STEP=%~1"
set "AWR_STEP_PATH=%~2"
set "AWR_FAILURE_CODE="
set "AWR_FAILURE_DETAIL="
echo [AWR] Schritt: %AWR_STEP%
if defined AWR_STEP_PATH echo [AWR] Pfad: %AWR_STEP_PATH%
exit /b 0


:CLEAN_ANDROID_RELEASE_OUTPUT
REM Only clean the finished versioned release directory. Never touch bin or obj.
set "RELEASE_OUTPUT_DIR=%~1"
for %%F in (
  "%RELEASE_OUTPUT_DIR%\de.kgv.oberrothenbach.aab"
  "%RELEASE_OUTPUT_DIR%\de.kgv.oberrothenbach.apk"
  "%RELEASE_OUTPUT_DIR%\Microsoft.Android.Resource.Designer.dll"
  "%RELEASE_OUTPUT_DIR%\_Microsoft.Android.Resource.Designer.dll"
  "%RELEASE_OUTPUT_DIR%\KGV-Android-*.aab"
  "%RELEASE_OUTPUT_DIR%\KGV-Android-*.apk"
) do (
  if exist "%%~F" (
    del /q "%%~F" || exit /b 1
  )
)
set "RELEASE_OUTPUT_DIR="
exit /b 0


:PLAY_SOUND
echo.
echo Spiele Abschlusssound...

if exist "%SUCCESS_SOUND%" (
  powershell -NoProfile -Command "$player = New-Object System.Media.SoundPlayer '%SUCCESS_SOUND%'; $player.PlaySync()"
) else (
  echo HINWEIS: Sounddatei nicht gefunden:
  echo %SUCCESS_SOUND%
  powershell -NoProfile -Command "[Console]::Beep(1000,500)"
)

exit /b 0
