param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9]{20}$')]
    [string]$ProjectRef
)

$ErrorActionPreference = 'Stop'
$templateDirectory = Join-Path $PSScriptRoot 'v3'
$target = 'ss:///pdf-templates/templates/export-allgemein/v3'
$pdf = Join-Path $templateDirectory 'template.pdf'
$fieldSpec = Join-Path $templateDirectory 'field-spec.json'
if (-not (Test-Path -LiteralPath $pdf -PathType Leaf)) { throw "PDF fehlt: $pdf" }
if (-not (Test-Path -LiteralPath $fieldSpec -PathType Leaf)) { throw "Field-Spec fehlt: $fieldSpec" }

npx supabase link --project-ref $ProjectRef
if ($LASTEXITCODE -ne 0) { throw 'Supabase-Projekt konnte nicht verknüpft werden.' }
Push-Location -LiteralPath $templateDirectory
try {
    $existing = @(npx supabase storage ls $target --experimental --linked)
    if ($LASTEXITCODE -ne 0) { throw 'Der Storage-Zielpfad konnte nicht geprüft werden.' }
    if ($existing.Count -gt 0) { throw 'export-allgemein/v3 existiert bereits im Storage und wird nicht überschrieben.' }

    # The CLI accepts a relative local source reliably on Windows. Keep the
    # destination as an explicit Storage URI, never a local path.
    npx supabase storage cp 'template.pdf' "$target/template.pdf" --content-type application/pdf --experimental --linked
    if ($LASTEXITCODE -ne 0) { throw 'PDF-Vorlage konnte nicht hochgeladen werden.' }
    npx supabase storage cp 'field-spec.json' "$target/field-spec.json" --content-type application/json --experimental --linked
    if ($LASTEXITCODE -ne 0) { throw 'Field-Spec konnte nicht hochgeladen werden.' }
}
finally {
    Pop-Location
}
