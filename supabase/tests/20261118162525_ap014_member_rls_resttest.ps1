param(
    [Parameter(Mandatory = $true)] [string] $LocalUrl,
    [Parameter(Mandatory = $true)] [string] $AnonKey,
    [Parameter(Mandatory = $true)] [string] $ServiceRoleKey
)

$ErrorActionPreference = 'Stop'
$baseUri = [Uri]$LocalUrl
if (-not $baseUri.IsLoopback) {
    throw 'AP-01.4 REST test refuses every non-loopback Supabase URL.'
}

$suffix = [Guid]::NewGuid().ToString('N')
$password = "AP014-$suffix-aA1!"
$ownEmail = "ap014-rest-own-$suffix@example.test"
$foreignEmail = "ap014-rest-foreign-$suffix@example.test"
$adminHeaders = @{
    apikey = $ServiceRoleKey
    Authorization = "Bearer $ServiceRoleKey"
}
$ownUserId = $null
$foreignUserId = $null
$ownMemberId = $null
$foreignMemberId = $null

function Invoke-JsonRequest {
    param(
        [Parameter(Mandatory = $true)] [string] $Method,
        [Parameter(Mandatory = $true)] [string] $Uri,
        [Parameter(Mandatory = $true)] [hashtable] $Headers,
        [object] $Body,
        [string] $Prefer
    )

    $request = @{
        Method = $Method
        Uri = $Uri
        Headers = $Headers
        ContentType = 'application/json'
    }
    if ($null -ne $Body) { $request.Body = ($Body | ConvertTo-Json -Depth 10 -Compress) }
    if ($Prefer) { $request.Headers = $Headers.Clone(); $request.Headers.Prefer = $Prefer }
    Invoke-RestMethod @request
}

try {
    $ownUser = Invoke-JsonRequest Post "$LocalUrl/auth/v1/admin/users" $adminHeaders @{
        email = $ownEmail; password = $password; email_confirm = $true
    }
    $foreignUser = Invoke-JsonRequest Post "$LocalUrl/auth/v1/admin/users" $adminHeaders @{
        email = $foreignEmail; password = $password; email_confirm = $true
    }
    $ownUserId = if ($ownUser.user) { $ownUser.user.id } else { $ownUser.id }
    $foreignUserId = if ($foreignUser.user) { $foreignUser.user.id } else { $foreignUser.id }

    $ownMember = Invoke-JsonRequest Post "$LocalUrl/rest/v1/mitglied" $adminHeaders @{
        name = 'AP014 REST Own'; vorname = 'Test'; aktiv = $true; aufnahme_status = 'aufgenommen'
        auth_user_id = $ownUserId; email = $ownEmail; adresse = 'REST Eigenweg 1'; plz = '01001'; ort = 'REST Eigenort'
    } 'return=representation'
    $foreignMember = Invoke-JsonRequest Post "$LocalUrl/rest/v1/mitglied" $adminHeaders @{
        name = 'AP014 REST Foreign'; vorname = 'Test'; aktiv = $true; aufnahme_status = 'aufgenommen'
        auth_user_id = $foreignUserId; email = $foreignEmail; adresse = 'REST Fremdweg 2'; plz = '02002'; ort = 'REST Fremdort'
    } 'return=representation'
    $ownMemberId = @($ownMember)[0].id
    $foreignMemberId = @($foreignMember)[0].id

    Invoke-JsonRequest Post "$LocalUrl/rest/v1/app_user" $adminHeaders @{
        user_id = $ownUserId; mitglied_id = $ownMemberId; role = 'user'; permission_grants = 0; permission_revocations = 0
    } 'resolution=merge-duplicates' | Out-Null
    Invoke-JsonRequest Post "$LocalUrl/rest/v1/app_user" $adminHeaders @{
        user_id = $foreignUserId; mitglied_id = $foreignMemberId; role = 'user'; permission_grants = 0; permission_revocations = 0
    } 'resolution=merge-duplicates' | Out-Null

    $session = Invoke-JsonRequest Post "$LocalUrl/auth/v1/token?grant_type=password" @{ apikey = $AnonKey } @{
        email = $ownEmail; password = $password
    }
    $userHeaders = @{ apikey = $AnonKey; Authorization = "Bearer $($session.access_token)" }

    $visibleRows = Invoke-JsonRequest Get "$LocalUrl/rest/v1/mitglied?select=id,email,adresse,telefon,geburtsdatum&email=like.ap014-rest-*-$suffix@example.test" $userHeaders
    if (@($visibleRows).Count -ne 1 -or @($visibleRows)[0].id -ne $ownMemberId) {
        throw "Authenticated REST scope returned $(@($visibleRows).Count) rows instead of the own row."
    }

    $foreignRows = Invoke-JsonRequest Get "$LocalUrl/rest/v1/mitglied?select=id,email&id=eq.$foreignMemberId" $userHeaders
    if (@($foreignRows).Count -ne 0) {
        throw 'Authenticated REST request returned a foreign member row.'
    }

    $viewRows = Invoke-JsonRequest Get "$LocalUrl/rest/v1/v_mitglied_mitgliedsantrag_nachweisstatus?select=mitglied_id&mitglied_id=in.($ownMemberId,$foreignMemberId)" $userHeaders
    if (@($viewRows).Count -ne 1 -or @($viewRows)[0].mitglied_id -ne $ownMemberId) {
        throw 'Security-invoker member view did not preserve the REST row scope.'
    }

    $anonDenied = $false
    try {
        Invoke-JsonRequest Get "$LocalUrl/rest/v1/mitglied?select=id&limit=1" @{ apikey = $AnonKey } | Out-Null
    }
    catch {
        $status = [int]$_.Exception.Response.StatusCode
        if ($status -in 401, 403) { $anonDenied = $true } else { throw }
    }
    if (-not $anonDenied) { throw 'Anonymous REST request was not rejected.' }

    [pscustomobject]@{
        AnonymousDenied = $true
        AuthenticatedOwnRows = 1
        ForeignRows = 0
        SecurityInvokerViewRows = 1
    }
}
finally {
    if ($ownUserId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/rest/v1/app_user?user_id=eq.$ownUserId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
    if ($foreignUserId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/rest/v1/app_user?user_id=eq.$foreignUserId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
    if ($ownMemberId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/rest/v1/mitglied?id=eq.$ownMemberId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
    if ($foreignMemberId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/rest/v1/mitglied?id=eq.$foreignMemberId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
    if ($ownUserId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/auth/v1/admin/users/$ownUserId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
    if ($foreignUserId) {
        Invoke-RestMethod -Method Delete -Uri "$LocalUrl/auth/v1/admin/users/$foreignUserId" -Headers $adminHeaders -ErrorAction SilentlyContinue | Out-Null
    }
}
