[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SupabaseUrl,
    [Parameter(Mandatory)][string]$AnonKey,
    [Parameter(Mandatory)][int]$SyntheticMemberId,
    [Parameter(Mandatory)][int]$SyntheticSaisonId,
    [Parameter(Mandatory)][string]$TestDate,
    [string]$AdminEmail = $env:KGV_DEMO_ADMIN_EMAIL,
    [string]$MemberEmail = $env:KGV_DEMO_MEMBER_EMAIL,
    [string]$TestKennung = 'FR207-E2E-20261124',
    [switch]$MigrationConfirmed
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if (-not $MigrationConfirmed) { throw 'FR2-07-Demo-Migration noch nicht bestaetigt.' }
if ($SupabaseUrl.TrimEnd('/') -ne 'https://ymxhszxfutjnljiuciyj.supabase.co') { throw 'Nur DemoDB zulaessig.' }
if ($SyntheticMemberId -le 0 -or $SyntheticSaisonId -le 0) { throw 'Synthetische Mitglieds- und Saison-ID erforderlich.' }
if ($TestKennung -notmatch '^FR207-E2E-[A-Za-z0-9_-]{4,40}$') { throw 'Testkennung ungueltig.' }
$day=[datetime]::MinValue
if (-not [datetime]::TryParseExact($TestDate,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture,
    [Globalization.DateTimeStyles]::None,[ref]$day)) { throw 'TestDate muss yyyy-MM-dd sein.' }
if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($MemberEmail)) {
    throw 'Test-E-Mails ausschliesslich aus KGV_DEMO_ADMIN_EMAIL und KGV_DEMO_MEMBER_EMAIL einlesen.'
}
function Read-Secret([string]$Name) {
    $path=Join-Path $env:LOCALAPPDATA "KGV/TestSecrets/$Name.secret"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "DPAPI-Testdatei fehlt: $Name" }
    $secure=(Get-Content -LiteralPath $path -Raw -Encoding UTF8).Trim() | ConvertTo-SecureString
    return [System.Net.NetworkCredential]::new('', $secure).Password
}
function Login([string]$Mail,[string]$Pass) {
    $body=@{email=$Mail; password=$Pass} | ConvertTo-Json -Compress
    $res=Invoke-RestMethod -Method Post -Uri "$SupabaseUrl/auth/v1/token?grant_type=password" `
      -Headers @{ apikey=$AnonKey } -Body $body -ContentType 'application/json'
    if (-not $res.access_token) { throw 'Demo-Login ohne Token.' }
    return [string]$res.access_token
}
function Http([string]$Method,[string]$Resource,[string]$Token,[object]$Body=$null,[bool]$Representation=$false) {
    $h=@{ apikey=$AnonKey; Authorization="Bearer $Token" }
    if ($Representation) { $h['Prefer']='return=representation' }
    $args=@{ Method=$Method; Uri="$SupabaseUrl/rest/v1/$Resource"; Headers=$h;
      SkipHttpErrorCheck=$true; ErrorAction='Stop' }
    if ($null -ne $Body) {
      $args['Body']=($Body | ConvertTo-Json -Depth 12 -Compress)
      $args['ContentType']='application/json'
    }
    $response=Invoke-WebRequest @args
    $result=$null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
      try { $result=ConvertFrom-Json -InputObject $response.Content -NoEnumerate }
      catch { $result=$null }
    }
    return @{status=[int]$response.StatusCode; body=$result}
}
$results=New-Object 'System.Collections.Generic.List[object]'
function Check([string]$Name,[bool]$Condition,[string]$Expected,[string]$Actual) {
    $results.Add(@{test=$Name; pass=$Condition; expected=$Expected; actual=$Actual})
    if (-not $Condition) { throw "FR207-Test nicht bestanden: $Name (Erwartung $Expected, Ergebnis $Actual)" }
}
$adminToken=Login $AdminEmail (Read-Secret 'admin')
$memberToken=Login $MemberEmail (Read-Secret 'mitglied')
$rights=@{}
foreach($code in @('arbeitsstunden.lesen','arbeitsstunden.bearbeiten','arbeitsstunden.freigeben','arbeitseinsatz.verwalten')) {
    $a=Http 'POST' 'rpc/ap02_has_effective_right' $adminToken @{p_fachrecht=$code}
    $m=Http 'POST' 'rpc/ap02_has_effective_right' $memberToken @{p_fachrecht=$code}
    Check "Auth-Snapshot $code" ($a.status -eq 200 -and $m.status -eq 200) '200/200' "$($a.status)/$($m.status)"
    $rights[$code]=@{admin=($a.body -eq $true);mitglied=($m.body -eq $true)}
}
# Eine positive Verwaltungs-Operation darf erst bei ECHTEN effektiven Rechten starten.
if (-not $rights['arbeitsstunden.bearbeiten'].admin -or
    -not $rights['arbeitsstunden.freigeben'].admin -or
    $rights['arbeitsstunden.bearbeiten'].mitglied -or
    $rights['arbeitsstunden.freigeben'].mitglied) {
    $results.Add(@{test='Voraussetzung Positiv-/Negativ-Identitaet';pass=$false;
       expected='Admin bearbeitet+freigibt, Mitglied ohne beide Rechte';actual='Effektive Rechte passen nicht'})
    $report=@{status='BLOCKIERT_TESTRECHTE';demo='ymxhszxfutjnljiuciyj';rights=$rights;
        tests=@($results);pending=@('Keine positiven Schreibtests gestartet')}
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PWD 'FR207_HTTP_E2E_Result.json') -Encoding utf8NoBOM
    throw 'Kontrollierte Demo-Testrechte fehlen. Weder Rechte noch Daten wurden geaendert.'
}
# Der ausgewählte Ziel-Datensatz muss explizit als synthetischer FR207-Demodatensatz erkennbar sein.
$memberInfo=Http 'GET' "mitglied?select=id,vorname,name&id=eq.$SyntheticMemberId" $adminToken
Check 'Synthetischer Zielmitglied-Lesecheck' ($memberInfo.status -eq 200 -and @($memberInfo.body).Count -eq 1) '200/1' "$($memberInfo.status)/$(@($memberInfo.body).Count)"
$target=@($memberInfo.body)[0]
if ("$($target.vorname) $($target.name)" -notmatch 'FR207') {
    throw 'Zielmitglied ist nicht als FR207 synthetisch markiert. Schreibtest verweigert.'
}
$marker="$TestKennung-stunden"
$query="arbeitsstunde?select=id,mitglied_id,saison_id,datum,stunden,status,freigegeben,art_der_arbeit&mitglied_id=eq.$SyntheticMemberId&art_der_arbeit=eq.$marker"
$pre=Http 'GET' $query $adminToken
Check 'Vorherbestand kontrolliert' ($pre.status -eq 200 -and @($pre.body).Count -le 1) '200/max1' "$($pre.status)/$(@($pre.body).Count)"
# NEGATIV VOR POSITIV: unberechtigte Person darf keine genehmigte Arbeitsstunde fuer Dritte schreiben.
$unallowed=Http 'POST' 'arbeitsstunde' $memberToken @{
 mitglied_id=$SyntheticMemberId;saison_id=$SyntheticSaisonId;datum=$TestDate;
 stunden=1;art_der_arbeit="$TestKennung-UNBEFUGT";status='genehmigt';freigegeben=$true
} $true
Check 'Unberechtigte administrative Genehmigung' ($unallowed.status -in @(401,403)) '401/403' "$($unallowed.status)"
$beforeNegative=Http 'GET' "arbeitsstunde?select=id&art_der_arbeit=eq.$TestKennung-UNBEFUGT" $adminToken
Check 'Keine negative Persistenz' ($beforeNegative.status -eq 200 -and @($beforeNegative.body).Count -eq 0) '200/0' "$($beforeNegative.status)/$(@($beforeNegative.body).Count)"
$rows=@($pre.body)
if ($rows.Count -eq 0) {
  $created=Http 'POST' 'arbeitsstunde' $adminToken @{
    mitglied_id=$SyntheticMemberId;saison_id=$SyntheticSaisonId;datum=$TestDate;
    stunden=1;art_der_arbeit=$marker;status='offen';freigegeben=$false;
    genehmigt_von=$null;genehmigt_am=$null
  } $true
  Check 'Positive offene Verwaltungserfassung' ($created.status -eq 201 -and @($created.body).Count -eq 1) '201/1' "$($created.status)/$(@($created.body).Count)"
  $id=[int]@($created.body)[0].id
} else {
  $id=[int]$rows[0].id
}
$fresh=Login $AdminEmail (Read-Secret 'admin')
$verified=Http 'GET' $query $fresh
Check 'Neuer HTTP-Session-Readback vor Entscheidung' ($verified.status -eq 200 -and @($verified.body).Count -eq 1) '200/1' "$($verified.status)/$(@($verified.body).Count)"
if (@($verified.body)[0].status -eq 'offen') {
  $decision=Http 'POST' 'rpc/review_arbeitsstunde' $adminToken @{
    p_arbeitsstunde_id=$id;p_aktion='freigeben';p_begruendung="$TestKennung Freigabe";
    p_datum=$null;p_stunden=$null;p_art_der_arbeit=$null
  }
  Check 'Positive fachliche Freigabe' ($decision.status -eq 200 -and $decision.body.aktion -eq 'freigegeben') '200/freigegeben' "$($decision.status)/$($decision.body.aktion)"
}
$repeat=Http 'POST' 'rpc/review_arbeitsstunde' $adminToken @{
  p_arbeitsstunde_id=$id;p_aktion='freigeben';p_begruendung="$TestKennung Wiederholung";
  p_datum=$null;p_stunden=$null;p_art_der_arbeit=$null
}
Check 'Endgueltige Wiederholung nicht-retrybar' ($repeat.status -in @(400,403,409,422)) '4xx/final' "$($repeat.status)"
$report=@{status='HTTP_TEILPRUEFUNG';atUtc=[DateTime]::UtcNow.ToString('o');demo='ymxhszxfutjnljiuciyj';
  objectId=$id;testMarker=$marker;rights=$rights;tests=@($results);
  retention='FR207-synthetischen Datensatz auf DemoDB behalten';
  pending=@('Unabhaengige SQL-Persistenzpruefung mit FR207_Verifikation.sql',
           'Konkurrierende Updates mit zwei berechtigten Identitaeten','MAUI/Web-GUI-E2E','Einsatzanmeldung und Stundenbestaetigung')}
$reportPath=Join-Path $PWD 'FR207_HTTP_E2E_Result.json'
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM
Write-Host "HTTP-E2E-TEILPRUEFUNG: $reportPath; unabhängige SQL-Verifikation ist noch offen."
