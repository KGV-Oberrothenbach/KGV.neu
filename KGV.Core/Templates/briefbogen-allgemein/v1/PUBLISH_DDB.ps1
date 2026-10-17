# Requires a Supabase CLI login with productive admin/Vorstand access.
# Run from the repository root. Never overwrite a published template path.
# This is intentionally disabled until the Vorstand has approved briefbogen-allgemein/v1.

$ErrorActionPreference = 'Stop'
$projectRef = 'ymxhszxfutjnljiuciyj'
$boardApproved = $false
$templateDir = Join-Path $PSScriptRoot '.'
$pdf = Join-Path $templateDir 'template.pdf'
$fieldSpec = Join-Path $templateDir 'field-spec.json'

if (-not $boardApproved) {
    throw 'briefbogen-allgemein/v1 ist noch nicht vom Vorstand freigegeben; kein Upload und keine Migration werden ausgeführt.'
}

supabase link --project-ref $projectRef

# Confirm that both versioned target paths are still unused in this environment.
$existing = @(supabase storage ls ss:///pdf-templates/templates/briefbogen-allgemein/v1 --experimental --linked)
if ($LASTEXITCODE -ne 0) { throw 'Der vorhandene Storage-Pfad konnte nicht geprüft werden.' }
if ($existing.Count -gt 0) { throw 'briefbogen-allgemein/v1 existiert bereits im Storage und wird nicht überschrieben.' }

supabase storage cp $pdf ss:///pdf-templates/templates/briefbogen-allgemein/v1/template.pdf --content-type application/pdf --experimental --linked
supabase storage cp $fieldSpec ss:///pdf-templates/templates/briefbogen-allgemein/v1/field-spec.json --content-type application/json --experimental --linked

# Applies 20261017125000_publish_briefbogen_allgemein_v1.sql after both uploads succeed.
supabase db push --linked
