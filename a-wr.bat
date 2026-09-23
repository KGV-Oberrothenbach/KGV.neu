@echo off
cls
setlocal EnableExtensions

REM =========================================================
REM KGV MAUI / Android Release
REM - setzt nur die MAUI-Zielversion
REM - erhoeht Android ApplicationVersion automatisch um 1
REM - erstellt signierte APK und AAB unter publish\<Version>\Android
REM - kein WPF-Build, kein Inno Setup, kein WPF-Release-Repo
REM =========================================================

set "REPO=C:\Programmieren\KGV\KGV.neu"
set "MAUI_CSPROJ=%REPO%\KGV.Maui\KGV.Maui.csproj"
set "PUBLISH_ROOT=%REPO%\publish"
set "GIT=C:\Program Files\Git\cmd\git.exe"

cd /d "%REPO%" || exit /b 1
if not exist "%MAUI_CSPROJ%" (
  echo FEHLER: MAUI csproj nicht gefunden.
  exit /b 1
)

for /f "tokens=1,2,3 delims=|" %%A in ('powershell -NoProfile -Command "$x=[xml](Get-Content '%MAUI_CSPROJ%');$g=@($x.Project.PropertyGroup ^| ? { $_.ApplicationDisplayVersion -or $_.ApplicationVersion -or $_.AndroidTargetSdkVersion });$d=($g ^| %% { if($_.ApplicationDisplayVersion){$_.ApplicationDisplayVersion} } ^| select -First 1);$c=($g ^| %% { if($_.ApplicationVersion){[int]$_.ApplicationVersion} } ^| measure -Maximum).Maximum;$s=($g ^| %% { if($_.AndroidTargetSdkVersion){$_.AndroidTargetSdkVersion} } ^| select -First 1);Write-Output ($d+'|'+$c+'|'+$s)"') do (
  set "OLD_DISPLAY=%%A"
  set "OLD_CODE=%%B"
  set "ANDROID_TARGET_SDK=%%C"
)

if not defined OLD_DISPLAY exit /b 1
set /p TARGET_VERSION=MAUI Zielversion eingeben (z. B. 0.6.20): 
if "%TARGET_VERSION%"=="" exit /b 1
set /a NEW_CODE=%OLD_CODE%+1

set "VERSION_ROOT=%PUBLISH_ROOT%\%TARGET_VERSION%"
set "ANDROID_OUT=%VERSION_ROOT%\Android"
set "KEYSTORE=%REPO%\_secrets\Android\kgv-upload.keystore"
set "KEYALIAS=kgvupload"

echo.
echo MAUI Version: %OLD_DISPLAY% -^> %TARGET_VERSION%
echo Android Code: %OLD_CODE% -^> %NEW_CODE%
choice /C JN /N /M "Android-Release starten? [J/N]: "
if errorlevel 2 exit /b 0

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop';$x=[xml](Get-Content '%MAUI_CSPROJ%');@($x.Project.PropertyGroup ^| ? { $_.ApplicationDisplayVersion -or $_.ApplicationVersion }) ^| %% { $_.ApplicationDisplayVersion='%TARGET_VERSION%'; $_.ApplicationVersion='%NEW_CODE%' };$x.Save('%MAUI_CSPROJ%')" || exit /b 1

if not exist "%KEYSTORE%" (
  echo FEHLER: Android-Keystore nicht gefunden.
  exit /b 1
)

for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$p=Read-Host 'Android-Keystore-Passwort' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b)}"`) do set "STOREPASS=%%P"
if not defined STOREPASS exit /b 1

dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || exit /b 1
dotnet publish ".\KGV.Maui\KGV.Maui.csproj" -f net10.0-android -c Release -p:AndroidTargetSdkVersion=%ANDROID_TARGET_SDK% -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore="%KEYSTORE%" -p:AndroidSigningStorePass="%STOREPASS%" -p:AndroidSigningKeyAlias="%KEYALIAS%" -p:AndroidSigningKeyPass="%STOREPASS%" || exit /b 1

if not exist "%ANDROID_OUT%" mkdir "%ANDROID_OUT%"
for %%F in ("%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*.apk") do set "APK_FILE=%%~fF"
for %%F in ("%REPO%\KGV.Maui\bin\Release\net10.0-android\publish\*.aab") do set "AAB_FILE=%%~fF"
if not defined APK_FILE exit /b 1
if not defined AAB_FILE exit /b 1
copy /Y "%APK_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.apk" >nul
copy /Y "%AAB_FILE%" "%ANDROID_OUT%\KGV-Android-%TARGET_VERSION%.aab" >nul

"%GIT%" -C "%REPO%" add "KGV.Maui/KGV.Maui.csproj"
"%GIT%" -C "%REPO%" commit -m "Release %TARGET_VERSION% MAUI Versionsdatei aktualisiert"
"%GIT%" -C "%REPO%" push origin

set "STOREPASS="
echo Fertig: %ANDROID_OUT%
exit /b 0
