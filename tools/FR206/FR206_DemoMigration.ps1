[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][string]$Worktree,
    [Parameter(Mandatory)][switch]$MauiE2EAbgeschlossen,
    [Parameter(Mandatory)][switch]$ExpliziteDemoFreigabe,
    [switch]$Apply,
    [string]$ExpectedBase = '20261122190917'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Dieses Skript ist ausschliesslich fuer den SPAETEREN, separat genehmigten Schritt.
# Nicht durch FR206_Install.ps1 aufrufen.
if (-not $MauiE2EAbgeschlossen -or -not $ExpliziteDemoFreigabe) {
    throw 'Abbruch: MAUI-E2E-Abschluss und ausdrueckliche DemoDB-Migrationsfreigabe fehlen.'
}
if (-not (Test-Path -LiteralPath $Worktree -PathType Container)) { throw 'Worktree fehlt.' }
$expectedBranch = 'work/fr2-06-finanzen-jahresabschluss-20261122'
$actualBranch = & git -C $Worktree branch --show-current
if ($LASTEXITCODE -ne 0 -or $actualBranch.Trim() -ne $expectedBranch) { throw 'Falscher Git-Branch.' }
$dirty = & git -C $Worktree status --porcelain
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Git-Worktree muss vor Remote-Migration sauber und committet sein.' }
$link = Join-Path $Worktree 'supabase/.temp/project-ref'
if (-not (Test-Path -LiteralPath $link -PathType Leaf)) {
    throw 'Keine lokale Projekt-Verknuepfung. Erst nach Freigabe kontrolliert mit der DemoDB verbinden.'
}
$project = (Get-Content -LiteralPath $link -Raw -Encoding UTF8).Trim()
if ($project -ne 'ymxhszxfutjnljiuciyj') {
    throw "Abbruch: verknuepftes Projekt ist nicht die DemoDB (erkannt: $project)."
}
$version = '20261122235600'
$file = Join-Path $Worktree "supabase/migrations/${version}_fr2_06_finance_rights_cutover.sql"
if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw 'FR2-06-Migration fehlt.' }
if (-not (Get-Command npx -ErrorAction SilentlyContinue)) { throw 'npx fehlt.' }
Push-Location $Worktree
try {
    & npx --yes supabase@2.121.0 migration list --linked
    if ($LASTEXITCODE -ne 0) { throw 'Migrationen konnten nicht gelesen werden.' }
    # Die tatsaechlich anstehenden Migrationen muessen vor dem Push gesondert
    # gegen den erwarteten Bestand geprueft werden. Keine automatische Annahme,
    # dass ein db push nur unsere eine Migration anwenden wird.
    if (-not $Apply) {
        Write-Host 'NUR VORPRUEFUNG. Kein db push ausgefuehrt.' -ForegroundColor Yellow
        Write-Host "Erwartete letzte Demo-Migration vor FR2-06: $ExpectedBase"
        return
    }
    if ($ExpectedBase -ne '20261122190917') { throw 'Unerwartete Basismigration - erneute fachliche Pruefung notwendig.' }
    # Sicherheit: selbst mit -Apply kein unbeaufsichtigter Push aller lokalen Migrationen.
    throw 'GESPERRT: Migrationsliste und SQL muessen nach MAUI-Abschluss einzeln kontrolliert werden. Remote-Ausfuehrung erst in gesondert geprueftem Schritt freigeben.'
}
finally { Pop-Location }
