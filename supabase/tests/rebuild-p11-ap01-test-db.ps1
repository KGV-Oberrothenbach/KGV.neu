[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Invoke-Supabase {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & npx supabase @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "supabase $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repositoryRoot
try {
    # All commands are explicitly local. The configured seed.sql loads the AP-01
    # fixture catalog after the complete versioned migration chain.
    Invoke-Supabase start
    Invoke-Supabase db reset --local
    Invoke-Supabase migration list --local
    Write-Host 'AP-01 local test database rebuilt from migrations and seeded with p11_ap01_test_fixture.'
}
finally {
    Pop-Location
}
