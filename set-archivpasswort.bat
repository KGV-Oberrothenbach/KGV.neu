@echo off
setlocal EnableExtensions

REM Setzt das Archivpasswort niemals im Klartext, sondern nur als SHA-256-Hash
REM im angegebenen Supabase-Projekt.
set "REPO=C:\Programmieren\KGV\KGV.neu"
cd /d "%REPO%" || (echo FEHLER: Repo-Pfad nicht gefunden.& exit /b 1)

echo =========================================================
echo KGV Dokumentarchivierung - Archivpasswort setzen
echo =========================================================
echo.
echo Demo-Projekt:  ymxhszxfutjnljiuciyj
echo Vereinsprojekt: itjcabiibuodkxayhvjq
echo.
set /p PROJECT_REF=Supabase Project-Ref eingeben: 
if "%PROJECT_REF%"=="" (
  echo FEHLER: Keine Project-Ref eingegeben.
  exit /b 1
)

echo.
echo Zielprojekt: %PROJECT_REF%
choice /C JN /N /M "Archivpasswort als Secret in diesem Projekt setzen? [J/N]: "
if errorlevel 2 (
  echo Abgebrochen. Es wurde nichts geaendert.
  exit /b 0
)

set "PROJECT_REF=%PROJECT_REF%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$projectRef = $env:PROJECT_REF;" ^
  "$securePassword = Read-Host 'Neues Archivpasswort eingeben' -AsSecureString;" ^
  "$confirmPassword = Read-Host 'Archivpasswort wiederholen' -AsSecureString;" ^
  "$toPlainText = { param($value) $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value); try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) } };" ^
  "$password = & $toPlainText $securePassword; $confirmation = & $toPlainText $confirmPassword;" ^
  "if ([string]::IsNullOrWhiteSpace($password) -or $password -ne $confirmation) { Write-Error 'Passwoerter sind leer oder stimmen nicht ueberein.'; exit 1 };" ^
  "$hashBytes = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($password));" ^
  "$hash = [Convert]::ToHexString($hashBytes).ToLowerInvariant();" ^
  "$password = $null; $confirmation = $null;" ^
  "& npx supabase secrets set ('KGV_DOCUMENT_ARCHIVE_PASSWORD_SHA256=' + $hash) --project-ref $projectRef;" ^
  "exit $LASTEXITCODE"

if errorlevel 1 (
  echo.
  echo FEHLER: Das Archivpasswort-Secret wurde nicht gesetzt.
  exit /b 1
)

echo.
echo Erfolgreich: KGV_DOCUMENT_ARCHIVE_PASSWORD_SHA256 wurde gesetzt.
echo Nun kann die Dokumentarchivierung in diesem Projekt deployt werden.
exit /b 0
