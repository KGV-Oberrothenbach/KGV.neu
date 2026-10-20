param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9]{20}$')]
    [string]$ProjectRef
)

$ErrorActionPreference = 'Stop'
$templateDirectory = Join-Path $PSScriptRoot 'v1'
$target = 'ss:///pdf-templates/templates/export-allgemein/v1'
$pdf = Join-Path $templateDirectory 'template.pdf'
$fieldSpec = Join-Path $templateDirectory 'field-spec.json'
if (-not (Test-Path -LiteralPath $pdf -PathType Leaf)) { throw "PDF fehlt: $pdf" }
if (-not (Test-Path -LiteralPath $fieldSpec -PathType Leaf)) { throw "Field-Spec fehlt: $fieldSpec" }

npx supabase link --project-ref $ProjectRef
$existing = @(npx supabase storage ls $target --experimental --linked)
if ($LASTEXITCODE -ne 0) { throw 'Der Storage-Zielpfad konnte nicht geprüft werden.' }
if ($existing.Count -gt 0) { throw 'export-allgemein/v1 existiert bereits im Storage und wird nicht überschrieben.' }
npx supabase storage cp $pdf "$target/template.pdf" --content-type application/pdf --experimental --linked
npx supabase storage cp $fieldSpec "$target/field-spec.json" --content-type application/json --experimental --linked
