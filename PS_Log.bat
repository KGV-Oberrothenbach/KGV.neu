@echo off
cls
setlocal

set "REPO=C:\Programmieren\KGV\KGV.neu"
set "ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Set-Location '%REPO%';" ^
  "$adb = '%ADB%';" ^
  "if (!(Test-Path $adb)) { Write-Error ('ADB nicht gefunden: ' + $adb); exit 1 };" ^
  "$logDir = Join-Path (Get-Location) '_logs';" ^
  "New-Item -ItemType Directory -Path $logDir -Force | Out-Null;" ^
  "$ts = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss';" ^
  "$logFile = Join-Path $logDir ('Logcat_' + $ts + '.log');" ^
  "Write-Host ('ADB: ' + $adb);" ^
  "Write-Host ('Logdatei: ' + $logFile);" ^
  "& $adb logcat -c;" ^
  "& $adb logcat | Tee-Object -FilePath $logFile"

endlocal
