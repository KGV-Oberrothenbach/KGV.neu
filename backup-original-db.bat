@echo off
setlocal EnableExtensions DisableDelayedExpansion

REM Erstellt einen lokalen PostgreSQL-Backup der Originaldatenbank.
REM Das Passwort wird verdeckt eingegeben, nur für diesen Lauf verwendet und nicht gespeichert.

set "REPO=C:\Programmieren\KGV\KGV.neu"
set "PROJECT_REF=itjcabiibuodkxayhvjq"
set "BACKUP_ROOT=%REPO%\_backups\original-db"
set "STAMP="
for /f "delims=" %%I in ('powershell -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'"') do set "STAMP=%%I"
set "BACKUP_DIR=%BACKUP_ROOT%\%STAMP%"
set "WORKDIR=%TEMP%\kgv-original-db-backup-%RANDOM%%RANDOM%"

if not exist "%REPO%" (echo FEHLER: Repo-Pfad nicht gefunden.& exit /b 1)
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%" || (echo FEHLER: Backup-Ordner konnte nicht erstellt werden.& exit /b 1)
mkdir "%WORKDIR%\supabase\.temp" || (echo FEHLER: Temporaerer Supabase-Ordner konnte nicht erstellt werden.& exit /b 1)

> "%WORKDIR%\supabase\config.toml" echo project_id = "original-db-backup"
> "%WORKDIR%\supabase\.temp\project-ref" echo %PROJECT_REF%

echo =========================================================
echo KGV Originaldatenbank - lokaler Backup
echo Projekt: %PROJECT_REF%
echo Ziel:    %BACKUP_DIR%
echo =========================================================
echo.
echo Bitte das Postgres-Datenbankpasswort eingeben.
echo Es wird nicht gespeichert oder ausgegeben.
for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$p=Read-Host 'Postgres-Datenbankpasswort' -AsSecureString;$b=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($p);try{[Runtime.InteropServices.Marshal]::PtrToStringBSTR($b)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b)}"`) do set "DBPASS=%%P"
if not defined DBPASS (echo FEHLER: Kein Passwort eingegeben.& goto FAILED)

echo.
echo [1/7] Schema public sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema public --keep-comments --file "%BACKUP_DIR%\01-public-schema.sql" || goto FAILED

echo [2/7] Daten public sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema public --data-only --use-copy --file "%BACKUP_DIR%\02-public-data.sql" || goto FAILED

echo [3/7] Auth-Schema und Auth-Daten sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema auth --keep-comments --file "%BACKUP_DIR%\03-auth-schema.sql" || goto FAILED
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema auth --data-only --use-copy --file "%BACKUP_DIR%\04-auth-data.sql" || goto FAILED

echo [4/7] Storage-Metadaten sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema storage --keep-comments --file "%BACKUP_DIR%\05-storage-schema.sql" || goto FAILED
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema storage --data-only --use-copy --file "%BACKUP_DIR%\06-storage-metadata.sql" || goto FAILED

echo [5/7] Rollen sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --role-only --file "%BACKUP_DIR%\07-roles.sql" || goto FAILED

echo [6/7] Migrationshistorie sichern...
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema supabase_migrations --keep-comments --file "%BACKUP_DIR%\08-migrations-schema.sql" || goto FAILED
call npx supabase db dump --linked --workdir "%WORKDIR%" --password "%DBPASS%" --schema supabase_migrations --data-only --use-copy --file "%BACKUP_DIR%\09-migrations-data.sql" || goto FAILED

echo [7/7] Backup-Dateien pruefen...
powershell -NoProfile -Command "$files=Get-ChildItem -LiteralPath '%BACKUP_DIR%' -File -Filter '*.sql';if($files.Count -lt 9 -or ($files | Where-Object Length -eq 0)){throw 'Mindestens eine erwartete Backup-Datei fehlt oder ist leer.'};$files | Select-Object Name,Length | Format-Table -AutoSize"
if errorlevel 1 goto FAILED

> "%BACKUP_DIR%\README.txt" echo Erstellt: %DATE% %TIME%
>> "%BACKUP_DIR%\README.txt" echo Projekt: %PROJECT_REF%
>> "%BACKUP_DIR%\README.txt" echo Enthalten: public, auth, Storage-Metadaten, Rollen und Migrationshistorie.
>> "%BACKUP_DIR%\README.txt" echo Nicht enthalten: eigentliche Storage-Dateien, Edge-Function-Secrets und Dashboard-Konfiguration.

set "DBPASS="
rmdir /s /q "%WORKDIR%"
echo.
echo BACKUP ERFOLGREICH: %BACKUP_DIR%
exit /b 0

:FAILED
set "DBPASS="
if exist "%WORKDIR%" rmdir /s /q "%WORKDIR%"
echo.
echo BACKUP FEHLGESCHLAGEN. Unvollstaendige Dateien bleiben zur Diagnose in: %BACKUP_DIR%
exit /b 1
