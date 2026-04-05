# AI Memex Import Script
# Consolidates entries from other projects' .ai-memex/ directories
# into the central lqdev.me store at _src/resources/ai-memex/
#
# Features:
#   - Imports new entries from spoke repos
#   - Updates hub entries when spoke version has a newer last_updated_date
#   - Preserves hub-only fields (related_entries seeded by knowledge graph)
#   - Normalizes schemas on ingest (dates, source_project)
#   - Validates required frontmatter fields

param(
    [string]$ConfigPath = "$HOME\.agents\skills\import-ai-memex\import-sources.json",
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "PersonalSite.fsproj")) {
    Write-Error "Must be run from the lqdev.me repo root (PersonalSite.fsproj not found)"
    exit 1
}

if (-not (Test-Path $ConfigPath)) {
    Write-Warning "Import sources config not found at: $ConfigPath"
    Write-Host "Create it from the example: .github/skills/universal/import-ai-memex/assets/import-sources.example.json"
    exit 1
}

function Get-FrontMatterField {
    param([string]$Content, [string]$Field)
    if ($Content -match "(?m)^${Field}:\s*`"?([^`"\r\n]+)`"?\s*$") {
        return $Matches[1].Trim()
    }
    return $null
}

function Merge-RelatedEntries {
    param([string]$SpokeContent, [string]$HubContent)
    $spokeRelated = Get-FrontMatterField $SpokeContent "related_entries"
    $hubRelated = Get-FrontMatterField $HubContent "related_entries"

    if (-not $hubRelated) { return $SpokeContent }
    if (-not $spokeRelated) {
        # Hub has related_entries but spoke doesn't — inject hub's value
        if ($SpokeContent -match '(?m)^related_entries:') {
            return $SpokeContent -replace '(?m)^related_entries:.*$', "related_entries: `"$hubRelated`""
        } else {
            return $SpokeContent -replace '(---\s*\r?\n)', "`$1related_entries: `"$hubRelated`"`n"
        }
    }

    # Both have entries — merge unique values
    $spokeSet = $spokeRelated -split '\s*,\s*' | Where-Object { $_ }
    $hubSet = $hubRelated -split '\s*,\s*' | Where-Object { $_ }
    $merged = ($spokeSet + $hubSet) | Select-Object -Unique
    $mergedStr = $merged -join ", "

    return $SpokeContent -replace '(?m)^related_entries:.*$', "related_entries: `"$mergedStr`""
}

function Normalize-DateString {
    param([string]$DateValue)
    if ($DateValue -match '^\d{4}-\d{2}-\d{2}$') { return "$DateValue 00:00 +00:00" }
    if ($DateValue -match ' UTC$') { return $DateValue -replace ' UTC$', ' +00:00' }
    return $DateValue
}

function Normalize-DateField {
    param([string]$Content, [string]$Field)
    $val = Get-FrontMatterField $Content $Field
    if (-not $val) { return $Content }
    $normalized = Normalize-DateString $val
    if ($normalized -ne $val) {
        $Content = $Content -replace "(?m)^${Field}:\s*`"[^`"]*`"", "${Field}: `"$normalized`""
    }
    return $Content
}

function Set-AuthoritativeSourceProject {
    param([string]$Content, [string]$ProjectName)
    # Always use the config's project name as source of truth
    if ($Content -match '(?m)^source_project:') {
        $Content = $Content -replace '(?m)^source_project:.*$', "source_project: `"$ProjectName`""
    } else {
        $Content = $Content -replace '(---\s*\r?\n)', "`$1source_project: `"$ProjectName`"`n"
    }
    return $Content
}

function Test-RequiredFields {
    param([string]$Content, [string]$FileName)
    $warnings = @()
    @("title", "description", "entry_type", "published_date", "last_updated_date") | ForEach-Object {
        $val = Get-FrontMatterField $Content $_
        if (-not $val) { $warnings += "    [WARN] Missing required field: $_" }
    }
    if ($warnings) {
        Write-Host "  [SCHEMA] $FileName" -ForegroundColor DarkYellow
        $warnings | ForEach-Object { Write-Host $_ -ForegroundColor DarkYellow }
    }
    return $warnings.Count -eq 0
}

function Normalize-Entry {
    param([string]$Content, [string]$ProjectName)
    $Content = Normalize-DateField $Content "published_date"
    $Content = Normalize-DateField $Content "last_updated_date"
    $Content = Set-AuthoritativeSourceProject $Content $ProjectName
    return $Content
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$targetDir = $config.target

if (-not (Test-Path $targetDir)) {
    Write-Error "Target directory not found: $targetDir"
    exit 1
}

$imported = 0
$updated = 0
$skipped = 0
$errors = 0

foreach ($source in $config.sources) {
    $sourcePath = $source.path
    $projectName = $source.project

    Write-Host "Source: $projectName ($sourcePath)" -ForegroundColor Cyan

    if (-not (Test-Path $sourcePath)) {
        Write-Warning "  Source not found (skipping): $sourcePath"
        continue
    }

    $entries = Get-ChildItem -Path $sourcePath -Filter "*.md" -File -ErrorAction SilentlyContinue
    if (-not $entries) {
        Write-Host "  No entries found"
        continue
    }

    foreach ($entry in $entries) {
        $targetFile = Join-Path $targetDir $entry.Name

        if (Test-Path $targetFile) {
            # Compare last_updated_date to decide whether to update
            $spokeContent = Get-Content $entry.FullName -Raw
            $hubContent = Get-Content $targetFile -Raw
            $spokeDate = Get-FrontMatterField $spokeContent "last_updated_date"
            $hubDate = Get-FrontMatterField $hubContent "last_updated_date"

            $shouldUpdate = $Force
            if (-not $Force -and $spokeDate -and $hubDate) {
                $normSpoke = Normalize-DateString $spokeDate
                $normHub = Normalize-DateString $hubDate
                try {
                    $shouldUpdate = [DateTimeOffset]::Parse($normSpoke) -gt [DateTimeOffset]::Parse($normHub)
                } catch {
                    $shouldUpdate = $normSpoke -gt $normHub
                }
            }

            if ($shouldUpdate) {
                if ($DryRun) {
                    Write-Host "  [DRY RUN] Would update: $($entry.Name) (spoke: $spokeDate > hub: $hubDate)"
                } else {
                    try {
                        $mergedContent = Merge-RelatedEntries $spokeContent $hubContent
                        $mergedContent = Normalize-Entry $mergedContent $projectName
                        Test-RequiredFields $mergedContent $entry.Name | Out-Null
                        Set-Content -Path $targetFile -Value $mergedContent -Encoding utf8 -NoNewline
                        Write-Host "  [UPDATED] $($entry.Name) (spoke: $spokeDate > hub: $hubDate)" -ForegroundColor Yellow
                    } catch {
                        Write-Warning "  Failed to update $($entry.Name): $_"
                        $errors++
                        continue
                    }
                }
                $updated++
            } else {
                Write-Host "  [CURRENT] $($entry.Name)" -ForegroundColor DarkGray
                $skipped++
            }
            continue
        }

        # New entry
        if ($DryRun) {
            Write-Host "  [DRY RUN] Would import: $($entry.Name) from $projectName"
            $imported++
            continue
        }

        try {
            $content = Get-Content $entry.FullName -Raw
            $content = Normalize-Entry $content $projectName
            Test-RequiredFields $content $entry.Name | Out-Null
            Set-Content -Path $targetFile -Value $content -Encoding utf8 -NoNewline
            Write-Host "  [IMPORTED] $($entry.Name)" -ForegroundColor Green
            $imported++
        }
        catch {
            Write-Warning "  Failed to import $($entry.Name): $_"
            $errors++
        }
    }
}

Write-Host ""
$action = if ($DryRun) { "DRY RUN" } else { "Import" }
Write-Host "$action complete: $imported imported, $updated updated, $skipped current, $errors errors"
if (($imported + $updated) -gt 0 -and -not $DryRun) {
    Write-Host "Next steps: review changes, then run 'dotnet run' to publish"
}
