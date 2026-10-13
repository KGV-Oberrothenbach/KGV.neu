@echo off
cls
setlocal EnableExtensions DisableDelayedExpansion

REM KGV MAUI + Web Release. Cloudflare deployment remains disabled by default.
set "REPO=C:\Programmieren\KGV\KGV.neu"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "WEB_ROOT=%REPO%\KGV.Web"
set "WEB_PACKAGE=%WEB_ROOT%\package.json"
set "PUBLISH_ROOT=%REPO%\publish"
set "ENABLE_CLOUDFLARE_DEPLOY=0"
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
 "$ErrorActionPreference='Stop';$p='%CUR_MAUI_DISPLAY%'.Split('.');$a=[int]$p[0];$b=[int]$p[1];$c=[int]$p[2];$test=if($p.Count -eq 3){$a+'.'+$b+'.'+$c+'.1'}else{$a+'.'+$b+'.'+$c+'.'+(([int]$p[3])+1)};$release=$a+'.'+$b+'.'+($c+1);Write-Output ($test+'|'+$release)" > "%PS_RESULT%"
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
set "TARGET_VERSION=" & set /p TARGET_VERSION=Zielversion [%SUGGESTED_VERSION%]:
if not defined TARGET_VERSION set "TARGET_VERSION=%SUGGESTED_VERSION%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';$v='%TARGET_VERSION%'.Trim();if($v -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$'){throw 'ungueltig'};Write-Output $v" > "%PS_RESULT%"
if errorlevel 1 (echo FEHLER: Zielversion muss drei oder vier numerische Teile haben.& goto FAILURE_END)
set "TARGET_VERSION=" & set /p TARGET_VERSION=<"%PS_RESULT%"
if not defined TARGET_VERSION goto FAILURE_END
if /I "%RELEASE_KIND%"=="Release" if /I not "%CURRENT_BRANCH%"=="main" (
  echo WARNUNG: Release von "%CURRENT_BRANCH%" statt main.
  choice /C JN /N /M "Trotzdem fortsetzen? [J/N]"
  if errorlevel 2 goto CANCEL_RELEASE
)

set /a NEW_ANDROID_CODE=%CUR_MAUI_CODE%+1
set "VERSION_ROOT=%PUBLISH_ROOT%\%TARGET_VERSION%"
set "ANDROID_OUT=%VERSION_ROOT%\Android"
set "ANDROID_UPLOAD_OUT=%ANDROID_OUT%\Upload"
set "ANDROID_DIAG_OUT=%ANDROID_OUT%\GooglePlay-Diagnose"
set "WEB_OUT=%VERSION_ROOT%\Web"
set "WEB_DIST=%WEB_ROOT%\dist"
set "WEB_CLIENT_DIST=%WEB_DIST%\client"
set "WEB_RELEASE_INFO=%WEB_CLIENT_DIST%\release-info.json"
echo.
echo Art: %RELEASE_KIND% / Zielversion: %TARGET_VERSION% / Android-Code: %NEW_ANDROID_CODE%
echo Cloudflare-Deployment: deaktiviert
choice /C JN /N /M "MAUI und Web jetzt bauen? [J/N]"
if errorlevel 2 goto CANCEL_RELEASE

set "VERSION_CHANGED=1"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ErrorActionPreference='Stop';$x=[xml](Get-Content '%MAUI_CSPROJ%');$g=@($x.Project.PropertyGroup | Where-Object {$_.ApplicationDisplayVersion -or $_.ApplicationVersion});if($g.Count -eq 0){throw 'Versionseinstellungen fehlen'};foreach($p in $g){$p.ApplicationDisplayVersion='%TARGET_VERSION%';$p.ApplicationVersion='%NEW_ANDROID_CODE%'};$x.Save('%MAUI_CSPROJ%')"
if errorlevel 1 goto BUILD_FAILED

set "KEYSTORE=%REPO%\_secrets\Android\kgv-upload.keystore"
set "KEYALIAS=kgvupload"
if not exist "%KEYSTORE%" (echo FEHLER: Keystore-Datei nicht gefunden.& goto BUILD_FAILED)
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$p=Read-Host 'Android-Keystore-Passwort' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b)}" > "%PS_RESULT%"
if errorlevel 1 goto BUILD_FAILED
set "STOREPASS=" & set /p STOREPASS=<"%PS_RESULT%"
if not defined STOREPASS goto BUILD_FAILED

if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED

set "APK_FILE=" & set "AAB_FILE="
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*-Signed.apk" 2^>nul') do if not defined APK_FILE set "APK_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*.apk" 2^>nul') do if not defined APK_FILE set "APK_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*-Signed.aab" 2^>nul') do if not defined AAB_FILE set "AAB_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
for /f "delims=" %%I in ('dir /b /a-d /o-d "%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*.aab" 2^>nul') do if not defined AAB_FILE set "AAB_FILE=%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\%%I"
if not defined APK_FILE goto BUILD_FAILED
if not defined AAB_FILE goto BUILD_FAILED

pushd "%WEB_ROOT%" || goto BUILD_FAILED
set "KGV_RELEASE_VERSION=%TARGET_VERSION%"
set "KGV_RELEASE_BRANCH=%CURRENT_BRANCH%"
set "KGV_RELEASE_COMMIT=%CURRENT_COMMIT%"
set "KGV_RELEASE_KIND=%RELEASE_KIND%"
call npm run build
set "WEB_BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%WEB_BUILD_RESULT%"=="0" goto BUILD_FAILED
if not exist "%WEB_CLIENT_DIST%" goto BUILD_FAILED
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';[ordered]@{version='%TARGET_VERSION%';branch='%CURRENT_BRANCH%';commit='%CURRENT_COMMIT%';kind='%RELEASE_KIND%';builtAtUtc=[DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')} | ConvertTo-Json | Set-Content -Path '%WEB_RELEASE_INFO%' -Encoding UTF8"
if errorlevel 1 goto BUILD_FAILED

if not exist "%ANDROID_UPLOAD_OUT%" mkdir "%ANDROID_UPLOAD_OUT%" || goto BUILD_FAILED
if not exist "%ANDROID_DIAG_OUT%" mkdir "%ANDROID_DIAG_OUT%" || goto BUILD_FAILED
if not exist "%WEB_OUT%" mkdir "%WEB_OUT%" || goto BUILD_FAILED
copy /Y "%APK_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk" >nul || goto BUILD_FAILED
copy /Y "%AAB_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab" >nul || goto BUILD_FAILED
copy /Y "%APK_FILE%" "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%.apk" >nul || goto BUILD_FAILED
copy /Y "%AAB_FILE%" "%ANDROID_UPLOAD_OUT%\KGV-Android-%TARGET_VERSION%.aab" >nul || goto BUILD_FAILED
robocopy "%WEB_DIST%" "%WEB_OUT%\dist" /E /NFL /NDL /NJH /NJS /NC /NS >nul
set "WEB_COPY_RESULT=%ERRORLEVEL%"
if %WEB_COPY_RESULT% GEQ 8 goto BUILD_FAILED
if "%ENABLE_CLOUDFLARE_DEPLOY%"=="1" (
  call :DEPLOY_WEB
  if errorlevel 1 goto BUILD_FAILED
)

"%GIT%" -C "%REPO%" add "KGV.Maui/KGV.Maui.csproj"
"%GIT%" -C "%REPO%" commit -m "%RELEASE_KIND% %TARGET_VERSION% MAUI/Web" || goto BUILD_FAILED
"%GIT%" -C "%REPO%" push origin "%CURRENT_BRANCH%" || goto BUILD_FAILED
echo RELEASE FERTIG: %TARGET_VERSION%
goto RELEASE_END

:DEPLOY_WEB
if /I "%RELEASE_KIND%"=="Release" (set "WEB_WORKER_NAME=%WEB_PROD_WORKER_NAME%") else (set "WEB_WORKER_NAME=%WEB_TEST_WORKER_NAME%")
pushd "%WEB_ROOT%" || exit /b 1
call npx wrangler deploy --config dist\server\wrangler.json --name "%WEB_WORKER_NAME%"
set "DEPLOY_RESULT=%ERRORLEVEL%"
popd
exit /b %DEPLOY_RESULT%

:CANCEL_RELEASE
echo Release abgebrochen. Die Versionsdatei wurde nicht geaendert.
goto RELEASE_END

:BUILD_FAILED
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
