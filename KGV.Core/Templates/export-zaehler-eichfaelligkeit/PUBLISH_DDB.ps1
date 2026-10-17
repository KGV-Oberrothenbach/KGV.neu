# Requires a Supabase CLI login with DDB admin/Vorstand access.
# Run from the repository root. Never overwrite a published template path.

$ErrorActionPreference = 'Stop'
$projectRef = 'ymxhszxfutjnljiuciyj'
$templateRoot = Split-Path -Parent $PSScriptRoot
$templates = @(
    @{ Key = 'export-zaehler-eichfaelligkeit-hoch'; Directory = Join-Path $templateRoot 'export-zaehler-eichfaelligkeit-hoch\v1' },
    @{ Key = 'export-zaehler-eichfaelligkeit-quer'; Directory = Join-Path $templateRoot 'export-zaehler-eichfaelligkeit-quer\v1' }
)

supabase link --project-ref $projectRef

foreach ($template in $templates) {
    $targetDirectory = "ss:///pdf-templates/templates/$($template.Key)/v1"
    $existing = @(supabase storage ls $targetDirectory --experimental --linked)
    if ($LASTEXITCODE -ne 0) { throw "Der Storage-Pfad für $($template.Key) konnte nicht geprüft werden." }
    if ($existing.Count -gt 0) { throw "$($template.Key)/v1 existiert bereits im Storage und wird nicht überschrieben." }
}

foreach ($template in $templates) {
    $pdf = Join-Path $template.Directory 'template.pdf'
    $fieldSpec = Join-Path $template.Directory 'field-spec.json'
    if (-not (Test-Path -LiteralPath $pdf -PathType Leaf)) { throw "PDF fehlt: $pdf" }
    if (-not (Test-Path -LiteralPath $fieldSpec -PathType Leaf)) { throw "Field-Spec fehlt: $fieldSpec" }

    $storageBase = "ss:///pdf-templates/templates/$($template.Key)/v1"
    supabase storage cp $pdf "$storageBase/template.pdf" --content-type application/pdf --experimental --linked
    supabase storage cp $fieldSpec "$storageBase/field-spec.json" --content-type application/json --experimental --linked
}

# Applies the guarded catalog-publication migration after all four uploads succeed.
supabase db push --linked
