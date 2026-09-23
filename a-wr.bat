@echo off
cls
setlocal EnableExtensions

REM MAUI/Android-Fassung von awr.bat: identischer Android-Ablauf, ohne WPF.
set "REPO=C:\Programmieren\KGV\KGV.neu"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "PUBLISH_ROOT=%REPO%\publish"
set "GIT="
if exist "C:\Program Files\Git\cmd\git.exe" set "GIT=C:\Program Files\Git\cmd\git.exe"
if not defined GIT if exist "C:\Program Files\Git\bin\git.exe" set "GIT=C:\Program Files\Git\bin\git.exe"

cd /d "%REPO%" || (echo FEHLER: Repo-Pfad nicht gefunden.& exit /b 1)
if not exist "%MAUI_CSPROJ%" (echo FEHLER: MAUI csproj nicht gefunden.& exit /b 1)
if not defined GIT (echo FEHLER: git.exe wurde nicht gefunden.& exit /b 1)
for /f "delims=" %%I in ('""%GIT%" -C "%REPO%" branch --show-current"') do set "CURRENT_BRANCH=%%I"
if not defined CURRENT_BRANCH (echo FEHLER: Branch konnte nicht ermittelt werden.& exit /b 1)

echo =========================================================
echo KGV MAUI / Android Release
echo Repo: %REPO%
echo Quellrepo-Branch: %CURRENT_BRANCH%
echo =========================================================
echo.
echo Loesche alte MAUI-Build-Artefakte...
if exist "%REPO%\KGV.Maui\bin" rmdir /s /q "%REPO%\KGV.Maui\bin"
if exist "%REPO%\KGV.Maui\obj" rmdir /s /q "%REPO%\KGV.Maui\obj"

echo.
echo Lese aktuelle Version und Android Target SDK...
for /f "tokens=1,2,3 delims=|" %%A in ('powershell -NoProfile -Command "$x=[xml](Get-Content '%MAUI_CSPROJ%');$d='';$c=0;$s='';foreach($p in $x.Project.PropertyGroup){if($p.ApplicationDisplayVersion -and -not $d){$d=[string]$p.ApplicationDisplayVersion};if($p.ApplicationVersion -and [int]$p.ApplicationVersion -gt $c){$c=[int]$p.ApplicationVersion};if($p.AndroidTargetSdkVersion -and -not $s){$s=[string]$p.AndroidTargetSdkVersion}};Write-Output ($d+'|'+$c+'|'+$s)"') do (
  set "CUR_MAUI_DISPLAY=%%A"
  set "CUR_MAUI_CODE=%%B"
  set "ANDROID_TARGET_SDK=%%C"
)
if not defined CUR_MAUI_DISPLAY (echo FEHLER: MAUI-Version konnte nicht gelesen werden.& exit /b 1)
if not defined CUR_MAUI_CODE (echo FEHLER: Android-Code konnte nicht gelesen werden.& exit /b 1)
if not defined ANDROID_TARGET_SDK (echo FEHLER: Target SDK konnte nicht gelesen werden.& exit /b 1)

echo Aktuelle MAUI sichtbare Version: %CUR_MAUI_DISPLAY%
echo Aktueller Android Versionscode: %CUR_MAUI_CODE%
echo Aktuelles Android Target SDK: %ANDROID_TARGET_SDK%
echo.
set /p TARGET_VERSION=MAUI Zielversion eingeben (z. B. 0.2.10):
if "%TARGET_VERSION%"=="" (echo FEHLER: Keine Zielversion eingegeben.& exit /b 1)
for /f "tokens=1-4 delims=." %%A in ("%TARGET_VERSION%") do (
  set "VERSION_PART_1=%%A"
  set "VERSION_PART_2=%%B"
  set "VERSION_PART_3=%%C"
)
if not defined VERSION_PART_1 (echo FEHLER: Zielversion muss mindestens 3 Teile haben, z. B. 0.7.2.& exit /b 1)
if not defined VERSION_PART_2 (echo FEHLER: Zielversion muss mindestens 3 Teile haben, z. B. 0.7.2.& exit /b 1)
if not defined VERSION_PART_3 (echo FEHLER: Zielversion muss mindestens 3 Teile haben, z. B. 0.7.2.& exit /b 1)

set /a NEW_ANDROID_CODE=%CUR_MAUI_CODE%+1
set "VERSION_ROOT=%PUBLISH_ROOT%\%TARGET_VERSION%"
set "ANDROID_OUT=%VERSION_ROOT%\Android"
set "ANDROID_UPLOAD_OUT=%ANDROID_OUT%\Upload"
set "ORIGINAL_MAUI_DISPLAY=%CUR_MAUI_DISPLAY%"
set "ORIGINAL_MAUI_CODE=%CUR_MAUI_CODE%"
if not exist "%ANDROID_UPLOAD_OUT%" mkdir "%ANDROID_UPLOAD_OUT%"

echo.
echo =========================================================
echo VERSION UND ANDROID TARGET SDK PRUEFEN
echo =========================================================
echo Neue MAUI Version:       %TARGET_VERSION%
echo Neuer Android Code:       %NEW_ANDROID_CODE%
echo Android Target SDK:       %ANDROID_TARGET_SDK%
echo.
echo Ursprungswerte:
echo MAUI Version:             %ORIGINAL_MAUI_DISPLAY%
echo Android Versionscode:     %ORIGINAL_MAUI_CODE%
echo.
choice /C JN /N /M "Release mit diesen Einstellungen starten? [J/N]: "
if errorlevel 2 goto CANCEL_RELEASE

powershell -NoProfile -ExecutionPolicy Bypass -Command "$x=[xml](Get-Content '%MAUI_CSPROJ%');foreach($p in $x.Project.PropertyGroup){if($p.ApplicationDisplayVersion -or $p.ApplicationVersion){$p.ApplicationDisplayVersion='%TARGET_VERSION%';$p.ApplicationVersion='%NEW_ANDROID_CODE%'}};$x.Save('%MAUI_CSPROJ%')"
if errorlevel 1 goto BUILD_FAILED
set "KEYSTORE=%REPO%\_secrets\Android\kgv-upload.keystore"
set "KEYALIAS=kgvupload"
if not exist "%KEYSTORE%" (echo FEHLER: Keystore-Datei nicht gefunden: %KEYSTORE%& goto BUILD_FAILED)

echo.
echo Bitte Android-Keystore-Passwort eingeben...
for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$p=Read-Host 'Android-Keystore-Passwort' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b)}"`) do set "STOREPASS=%%P"
if not defined STOREPASS goto BUILD_FAILED

echo Baue signierte APK...
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED
echo Baue signierte AAB...
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || goto BUILD_FAILED

set "APK_FILE="
set "AAB_FILE="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$publish = Join-Path '%REPO%' 'KGV.Maui\bin\Release\net10.0-android\publish';" ^
  "$apk = Get-ChildItem $publish -Filter '*-Signed.apk' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if (-not $apk) { $apk = Get-ChildItem $publish -Filter '*.apk' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }" ^
  "if ($apk) { Write-Output $apk.FullName }"`) do set "APK_FILE=%%I"
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$publish = Join-Path '%REPO%' 'KGV.Maui\bin\Release\net10.0-android\publish';" ^
  "$aab = Get-ChildItem $publish -Filter '*-Signed.aab' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1;" ^
  "if (-not $aab) { $aab = Get-ChildItem $publish -Filter '*.aab' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }" ^
  "if ($aab) { Write-Output $aab.FullName }"`) do set "AAB_FILE=%%I"
if not defined APK_FILE (echo FEHLER: Keine APK-Datei im Publish-Ordner gefunden.& goto BUILD_FAILED)
if not defined AAB_FILE (echo FEHLER: Keine AAB-Datei im Publish-Ordner gefunden.& goto BUILD_FAILED)
echo APK-Quelle: %APK_FILE%
echo AAB-Quelle: %AAB_FILE%
copy /Y "%APK_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk" >nul || goto BUILD_FAILED
copy /Y "%APK_FILE%" "%VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.apk" >nul || goto BUILD_FAILED
copy /Y "%AAB_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab" >nul || goto BUILD_FAILED
copy /Y "%AAB_FILE%" "%VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.aab" >nul || goto BUILD_FAILED
copy /Y "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk" "%ANDROID_UPLOAD_OUT%" >nul
copy /Y "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab" "%ANDROID_UPLOAD_OUT%" >nul

"%GIT%" -C "%REPO%" status -sb
"%GIT%" -C "%REPO%" add "KGV.Maui/KGV.Maui.csproj"
"%GIT%" -C "%REPO%" diff --cached --quiet
if errorlevel 1 (
  "%GIT%" -C "%REPO%" commit -m "Release %TARGET_VERSION% MAUI Versionsdatei aktualisiert" || exit /b 1
  "%GIT%" -C "%REPO%" push origin "%CURRENT_BRANCH%" || exit /b 1
)
echo.
echo Fertig.
echo Version: %TARGET_VERSION%
echo APK: %VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.apk
echo AAB: %VERSION_ROOT%\KGV-Android-%TARGET_VERSION%.aab
echo Android Upload: %ANDROID_UPLOAD_OUT%
set "STOREPASS="
exit /b 0

:CANCEL_RELEASE
echo Release abgebrochen. Die Versionsdatei wurde nicht geaendert.
exit /b 0

:BUILD_FAILED
echo FEHLER: Release fehlgeschlagen. Setze MAUI-Version zurueck...
powershell -NoProfile -Command "$x=[xml](Get-Content '%MAUI_CSPROJ%');foreach($p in $x.Project.PropertyGroup){if($p.ApplicationDisplayVersion -or $p.ApplicationVersion){$p.ApplicationDisplayVersion='%ORIGINAL_MAUI_DISPLAY%';$p.ApplicationVersion='%ORIGINAL_MAUI_CODE%'}};$x.Save('%MAUI_CSPROJ%')"
set "STOREPASS="
exit /b 1
