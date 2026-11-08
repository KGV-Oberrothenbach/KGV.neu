[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repositoryRoot
try {
    # All commands are explicitly local. The configured seed.sql loads the AP-01
    # fixture catalog after the complete versioned migration chain.
    npx supabase start
    npx supabase db reset --local
    npx supabase migration list --local
    Write-Host 'AP-01 local test database rebuilt from migrations and seeded with p11_ap01_test_fixture.'
}
finally {
    Pop-Location
}
