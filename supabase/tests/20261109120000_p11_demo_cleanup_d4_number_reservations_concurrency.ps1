[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$container = docker ps --filter 'name=supabase_db_' --format '{{.Names}}' | Select-Object -First 1
if (-not $container) {
    throw 'No local Supabase PostgreSQL container is running.'
}

$sourceId = 94990
$sequenceState = docker exec $container psql -U postgres -d postgres -At -v ON_ERROR_STOP=1 -c `
    "select last_value::text || ':' || is_called::text from public.fachnummer_mitgliedsantrag_seq"
if ($LASTEXITCODE -ne 0 -or -not $sequenceState) {
    throw 'Could not read the local Fachnummer sequence state.'
}
$stateParts = $sequenceState.Trim().Split(':')
$lastValue = [int]$stateParts[0]
$wasCalled = $stateParts[1] -eq 't'

$firstSql = "begin; select set_config('request.jwt.claim.role','service_role',true); select public.reserve_fachnummer('mitgliedsantrag','kommunikationsvorgang',$sourceId); select pg_sleep(2); commit;"
$secondSql = "begin; select set_config('request.jwt.claim.role','service_role',true); select public.reserve_fachnummer('mitgliedsantrag','kommunikationsvorgang',$sourceId); commit;"

$first = $null
$second = $null
try {
    $first = Start-Job -ScriptBlock {
        param($containerName, $sql)
        docker exec $containerName psql -U postgres -d postgres -At -v ON_ERROR_STOP=1 -c $sql
        if ($LASTEXITCODE -ne 0) { throw 'First concurrent reservation failed.' }
    } -ArgumentList $container, $firstSql

    Start-Sleep -Milliseconds 300

    $second = Start-Job -ScriptBlock {
        param($containerName, $sql)
        docker exec $containerName psql -U postgres -d postgres -At -v ON_ERROR_STOP=1 -c $sql
        if ($LASTEXITCODE -ne 0) { throw 'Second concurrent reservation failed.' }
    } -ArgumentList $container, $secondSql

    Wait-Job -Job $first, $second | Out-Null
    $firstOutput = @(Receive-Job -Job $first)
    $secondOutput = @(Receive-Job -Job $second)
    if ($first.State -ne 'Completed' -or $second.State -ne 'Completed') {
        throw "Concurrent reservation jobs did not complete: $($first.State), $($second.State)."
    }

    $firstNumber = $firstOutput | Where-Object { $_ -match '^\d{5}$' } | Select-Object -First 1
    $secondNumber = $secondOutput | Where-Object { $_ -match '^\d{5}$' } | Select-Object -First 1
    if (-not $firstNumber -or $firstNumber -ne $secondNumber) {
        throw "Concurrent calls returned different numbers: '$firstNumber' and '$secondNumber'."
    }

    $count = docker exec $container psql -U postgres -d postgres -At -v ON_ERROR_STOP=1 -c `
        "select count(*) from public.fachnummer_reservierung where quelle_art='kommunikationsvorgang' and quelle_id=$sourceId"
    if ($LASTEXITCODE -ne 0 -or $count.Trim() -ne '1') {
        throw "Concurrent calls created $($count.Trim()) reservations instead of one."
    }

    Write-Host "D4 concurrent reservation passed with one idempotent number: $firstNumber"
}
finally {
    if ($first) { Remove-Job -Job $first -Force -ErrorAction SilentlyContinue }
    if ($second) { Remove-Job -Job $second -Force -ErrorAction SilentlyContinue }
    docker exec $container psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c `
        "delete from public.fachnummer_reservierung where quelle_art='kommunikationsvorgang' and quelle_id=$sourceId; select setval('public.fachnummer_mitgliedsantrag_seq',$lastValue,$($wasCalled.ToString().ToLowerInvariant()));" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to clean up the synthetic D4 concurrency reservation.'
    }
}
