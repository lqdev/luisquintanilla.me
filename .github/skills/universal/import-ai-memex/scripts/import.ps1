# AI Memex Import Script
# Consolidates entries from other projects' .ai-memex/ directories
# into the central lqdev.me store at _src/resources/ai-memex/

param(
    [string]$ConfigPath = "$HOME\.agents\skills\import-ai-memex\import-sources.json",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Verify we're in the lqdev.me repo
if (-not (Test-Path "PersonalSite.fsproj")) {
    Write-Error "Must be run from the lqdev.me repo root (PersonalSite.fsproj not found)"
    exit 1
}

# Load import sources config
if (-not (Test-Path $ConfigPath)) {
    Write-Warning "Import sources config not found at: $ConfigPath"
    Write-Host "Create it from the example: .github/skills/universal/import-ai-memex/assets/import-sources.example.json"
    exit 1
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$targetDir = $config.target

if (-not (Test-Path $targetDir)) {
    Write-Error "Target directory not found: $targetDir"
    exit 1
}

$imported = 0
$skipped = 0
$errors = 0

foreach ($source in $config.sources) {
    $sourcePath = $source.path
    $projectName = $source.project

    if (-not (Test-Path $sourcePath)) {
        Write-Warning "Source not found (skipping): $sourcePath"
        continue
    }

    $entries = Get-ChildItem -Path $sourcePath -Filter "*.md" -File -ErrorAction SilentlyContinue
    if (-not $entries) {
        Write-Host "No entries found in: $sourcePath"
        continue
    }

    foreach ($entry in $entries) {
        $targetFile = Join-Path $targetDir $entry.Name

        if (Test-Path $targetFile) {
            Write-Host "  Already exists (skipping): $($entry.Name)"
            $skipped++
            continue
        }

        if ($DryRun) {
            Write-Host "  [DRY RUN] Would import: $($entry.Name) from $projectName"
            $imported++
            continue
        }

        try {
            $content = Get-Content $entry.FullName -Raw

            # Add source_project if not already present
            if ($content -notmatch 'source_project:') {
                $content = $content -replace '(---\s*\n)', "`$1source_project: `"$projectName`"`n"
            }

            Set-Content -Path $targetFile -Value $content -NoNewline
            Write-Host "  Imported: $($entry.Name) from $projectName"
            $imported++
        }
        catch {
            Write-Warning "  Failed to import $($entry.Name): $_"
            $errors++
        }
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "DRY RUN complete: $imported would be imported, $skipped already exist, $errors errors"
} else {
    Write-Host "Import complete: $imported imported, $skipped already exist, $errors errors"
    if ($imported -gt 0) {
        Write-Host "Next steps: review imported entries, then run 'dotnet run' to publish"
    }
}
