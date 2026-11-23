[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SupabaseUrl,
    [Parameter(Mandatory)][string]$AnonKey,
    [Parameter(Mandatory)][int]$SyntheticSaisonId,
    [string]$AdminEmail = $env:KGV_DEMO_ADMIN_EMAIL,
    [string]$MemberEmail = $env:KGV_DEMO_MEMBER_EMAIL,
    [string]$TestKennung = 'FR206-E2E-20261122',
    [Parameter(Mandatory)][switch]$MauiE2EAbgeschlossen,
    [Parameter(Mandatory)][switch]$DemoMigrationFreigegebenUndAngewendet
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $MauiE2EAbgeschlossen -or -not $DemoMigrationFreigegebenUndAngewendet) {
    throw 'FR2-06-HTTP-E2E derzeit GESPERRT: MAUI-Abschluss und migrierte DemoDB fehlen.'
}
if ($SupabaseUrl.TrimEnd('/') -ne 'https://ymxhszxfutjnljiuciyj.supabase.co') {
    throw 'Nur die ausdruecklich freigegebene DemoDB ist erlaubt.'
}
if ($SyntheticSaisonId -le 0) { throw 'Ein positiver, ausdruecklich synthetischer Saisonbezug ist notwendig.' }
if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($MemberEmail)) {
    throw 'Testkonten-E-Mails fehlen; lokale Umgebungsvariablen KGV_DEMO_ADMIN_EMAIL / KGV_DEMO_MEMBER_EMAIL verwenden.'
}
if ($TestKennung -notmatch '^FR206-E2E-[A-Za-z0-9_-]{3,50}$') { throw 'Ungueltige synthetische Testkennung.' }

function Read-DpapiPassword([string]$Name) {
    $file = Join-Path $env:LOCALAPPDATA "KGV/TestSecrets/$Name.secret"
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Lokale DPAPI-Datei fehlt: $Name" }
    $secure = (Get-Content -LiteralPath $file -Raw -Encoding UTF8).Trim() | ConvertTo-SecureString
    return [System.Net.NetworkCredential]::new('', $secure).Password
}
function Login([string]$Email, [string]$Password) {
    $headers = @{ apikey = $AnonKey }
    $payload = @{ email=$Email; password=$Password } | ConvertTo-Json -Compress
    $res = Invoke-RestMethod -Method POST -Uri "$SupabaseUrl/auth/v1/token?grant_type=password" -Headers $headers -Body $payload -ContentType 'application/json'
    if (-not $res.access_token) { throw 'Login ohne Token' }
    return [string]$res.access_token
}
function Headers([string]$Token, [bool]$Write=$false) {
    $h = @{ apikey=$AnonKey; Authorization="Bearer $Token" }
    if ($Write) { $h['Prefer']='return=representation' }
    return $h
}
function Http([string]$Method,[string]$Url,[string]$Token,[object]$Payload=$null) {
    $h = Headers $Token ($Method -in @('POST','PATCH','PUT'))
    $args = @{ Method=$Method; Uri="$SupabaseUrl/rest/v1/$Url"; Headers=$h; SkipHttpErrorCheck=$true }
    if ($null -ne $Payload) {
        $args['Body'] = ($Payload | ConvertTo-Json -Compress -Depth 10)
        $args['ContentType'] = 'application/json'
    }
    $r = Invoke-WebRequest @args
    $parsed = $null
    if (-not [string]::IsNullOrWhiteSpace($r.Content)) {
        if ($r.Content.Trim() -eq '[]') { $parsed = @() }
        else {
            try { $parsed=ConvertFrom-Json -InputObject $r.Content -NoEnumerate }
            catch { $parsed=$null }
        }
    }
    return @{ status=[int]$r.StatusCode; body=$parsed }
}
function Require([bool]$Condition,[string]$Message) {
    if (-not $Condition) { throw $Message }
}

$adminPass = Read-DpapiPassword 'admin'
$memberPass = Read-DpapiPassword 'mitglied'
$adminToken = Login $AdminEmail $adminPass
$memberToken = Login $MemberEmail $memberPass
Remove-Variable adminPass, memberPass

$results = @()
function Check([string]$Label,[bool]$Value) {
    $script:results += @{ test=$Label; result=($(if($Value){'PASS'}else{'FAIL'})) }
    Require $Value "Test fehlgeschlagen: $Label"
}
# Rechtssnapshot direkt serverseitig, keine Legacy-Rollenannahme.
$adminRight=Http 'POST' 'rpc/ap02_has_effective_right' $adminToken @{p_fachrecht='finanzen.rechnungen'}
$memberRight=Http 'POST' 'rpc/ap02_has_effective_right' $memberToken @{p_fachrecht='finanzen.rechnungen'}
Check 'Admin mit wirksamem Recht' ($adminRight.status -eq 200 -and $adminRight.body -eq $true)
Check 'Mitglied ohne Recht' ($memberRight.status -eq 200 -and $memberRight.body -eq $false)

$invoiceQuery = "jahresabschluss_rechnung?select=*&rechnungsnummer=eq.$TestKennung"
$existing = Http 'GET' $invoiceQuery $adminToken
Check 'Synthetisches Objekt vorab lesen' ($existing.status -eq 200)
$matching = @($existing.body)
Check 'Keine Duplikate vor dem Test' ($matching.Count -le 1)
# Negative Berechtigungspruefungen vor der berechtigten Operation.
$bad = Http 'POST' 'jahresabschluss_rechnung' $memberToken @{
 saison_id=$SyntheticSaisonId; lieferant='FR206-E2E synthetisch';
 rechnungsnummer="$TestKennung-UNBERECHTIGT"; rechnungsdatum='2026-01-02'; gesamtbetrag=0
}
Check 'Mitglied darf keine Rechnung anlegen' ($bad.status -in @(401,403))
$badVerify = Http 'GET' "jahresabschluss_rechnung?select=id&rechnungsnummer=eq.$TestKennung-UNBERECHTIGT" $adminToken
Check 'Negativer Versuch nicht persistiert' ($badVerify.status -eq 200 -and @($badVerify.body).Count -eq 0)

if ($matching.Count -eq 0) {
    $create = Http 'POST' 'jahresabschluss_rechnung' $adminToken @{
      saison_id=$SyntheticSaisonId; lieferant='FR206-E2E synthetisch';
      rechnungsnummer=$TestKennung; rechnungsdatum='2026-01-02'; gesamtbetrag=0
    }
    Check 'Berechtigter HTTP-POST Rechnung' ($create.status -eq 201 -and @($create.body).Count -eq 1)
} else {
    Check 'Idempotente Wiederaufnahme synthetischer Rechnung' ($matching[0].lieferant -eq 'FR206-E2E synthetisch' -and [int]$matching[0].saison_id -eq $SyntheticSaisonId)
}
# Unabhaengige Persistenzpruefung mit neuer Auth-Sitzung statt nur POST-Ergebnis.
$freshToken=Login $AdminEmail (Read-DpapiPassword 'admin')
$after = Http 'GET' $invoiceQuery $freshToken
Check 'Unabhaengige Rechnungspersistenz' ($after.status -eq 200 -and @($after.body).Count -eq 1 -and [string]@($after.body)[0].lieferant -eq 'FR206-E2E synthetisch')
$memberRead = Http 'GET' $invoiceQuery $memberToken
Check 'Unberechtigtes Mitglied sieht fremde Rechnung nicht' ($memberRead.status -eq 200 -and @($memberRead.body).Count -eq 0)
$report = @{ runUtc=[DateTime]::UtcNow.ToString('o'); project='ymxhszxfutjnljiuciyj'; seasonId=$SyntheticSaisonId; invoiceIdentifier=$TestKennung;
 tests=$results; cleanup='Synthetische Rechnung wird als wiederverwendbare Demodaten behalten';
 pending=@('Positive Kostenartschreibung','Jahresabschluss vorbereiten und persistieren','Positionen-RPC Retry','Finalisierung negativ mit gesperrtem Fachrecht','Saison- und Objektgrenzen','UI-E2E') }
$path = Join-Path (Get-Location) "FR206_HTTP_E2E_Result.json"
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding UTF8
Write-Host "HTTP-E2E-Teilpruefung abgeschlossen: $path" -ForegroundColor Green
Write-Host 'Fehlende positive Schreib-/Retry-Tests sind explizit AUSSTEHEND, nicht bestanden.' -ForegroundColor Yellow
