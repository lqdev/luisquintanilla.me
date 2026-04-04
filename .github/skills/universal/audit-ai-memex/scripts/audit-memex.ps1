# AI Memex Audit Script
# Analyzes health and freshness of the AI Memex knowledge base
#
# Checks: entry freshness, spoke-hub drift, broken references,
#          orphan entries, and critical hub entries
#
# Usage: & "$HOME/.agents/skills/audit-ai-memex/scripts/audit-memex.ps1"
# Must be run from the lqdev.me repo root.

param(
    [string]$ConfigPath = "$HOME\.agents\skills\import-ai-memex\import-sources.json",
    [int]$StaleDays = 90
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "PersonalSite.fsproj")) {
    Write-Error "Must be run from the lqdev.me repo root (PersonalSite.fsproj not found)"
    exit 1
}

$hubDir = "_src\resources\ai-memex"
$graphPath = "_public\resources\ai-memex\graph.json"

function Get-FrontMatterField {
    param([string]$Content, [string]$Field)
    if ($Content -match "(?m)^${Field}:\s*`"?([^`"\r\n]+)`"?\s*$") {
        return $Matches[1].Trim()
    }
    return $null
}

# --- Collect hub entries ---
$hubEntries = @{}
$hubFiles = Get-ChildItem -Path $hubDir -Filter "*.md" -File
foreach ($f in $hubFiles) {
    $content = Get-Content $f.FullName -Raw
    $slug = $f.BaseName
    $hubEntries[$slug] = @{
        File = $f.Name
        Title = Get-FrontMatterField $content "title"
        EntryType = Get-FrontMatterField $content "entry_type"
        Published = Get-FrontMatterField $content "published_date"
        LastUpdated = Get-FrontMatterField $content "last_updated_date"
        RelatedEntries = Get-FrontMatterField $content "related_entries"
        SourceProject = Get-FrontMatterField $content "source_project"
    }
}

$totalEntries = $hubEntries.Count
$today = Get-Date

Write-Host ""
Write-Host "AI Memex Health Audit" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Hub entries: $totalEntries"
Write-Host "Stale threshold: $StaleDays days"
Write-Host ""

# --- 1. Freshness Analysis ---
Write-Host "1. FRESHNESS" -ForegroundColor Yellow
Write-Host "   ---------"

$staleEntries = @()
$neverUpdated = 0

foreach ($slug in ($hubEntries.Keys | Sort-Object)) {
    $entry = $hubEntries[$slug]
    $lastUpdated = $entry.LastUpdated
    if (-not $lastUpdated) { continue }

    try {
        $updateDate = [DateTime]::Parse($lastUpdated)
        $ageDays = ($today - $updateDate).Days

        if ($entry.Published -eq $entry.LastUpdated) {
            $neverUpdated++
        }

        if ($ageDays -gt $StaleDays) {
            $staleEntries += @{
                Slug = $slug
                Type = $entry.EntryType
                LastUpdated = $lastUpdated
                AgeDays = $ageDays
            }
        }
    } catch {
        Write-Host "   [WARN] Cannot parse date for: $slug ($lastUpdated)" -ForegroundColor DarkYellow
    }
}

if ($staleEntries.Count -gt 0) {
    Write-Host "   Stale entries (>$StaleDays days):" -ForegroundColor Red
    foreach ($s in ($staleEntries | Sort-Object { $_.AgeDays } -Descending)) {
        Write-Host "   - $($s.Slug) ($($s.Type)) — $($s.AgeDays) days" -ForegroundColor DarkGray
    }
} else {
    Write-Host "   All entries within $StaleDays-day threshold" -ForegroundColor Green
}
Write-Host "   Never updated (last_updated == published): $neverUpdated / $totalEntries"
Write-Host ""

# --- 2. Spoke-Hub Drift ---
Write-Host "2. SPOKE-HUB DRIFT" -ForegroundColor Yellow
Write-Host "   ----------------"

$driftCount = 0
if (Test-Path $ConfigPath) {
    $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    foreach ($source in $config.sources) {
        if (-not (Test-Path $source.path)) {
            Write-Host "   [SKIP] Source not found: $($source.project) ($($source.path))" -ForegroundColor DarkGray
            continue
        }

        $spokeFiles = Get-ChildItem -Path $source.path -Filter "*.md" -File -ErrorAction SilentlyContinue
        foreach ($sf in $spokeFiles) {
            $slug = $sf.BaseName
            $hubEntry = $hubEntries[$slug]
            if (-not $hubEntry) {
                Write-Host "   [MISSING] $($sf.Name) in spoke '$($source.project)' not in hub" -ForegroundColor Red
                $driftCount++
                continue
            }

            $spokeContent = Get-Content $sf.FullName -Raw
            $spokeDate = Get-FrontMatterField $spokeContent "last_updated_date"
            $hubDate = $hubEntry.LastUpdated

            if ($spokeDate -and $hubDate) {
                try {
                    if ([DateTime]::Parse($spokeDate) -gt [DateTime]::Parse($hubDate)) {
                        Write-Host "   [DRIFT] $slug — spoke: $spokeDate > hub: $hubDate ($($source.project))" -ForegroundColor Yellow
                        $driftCount++
                    }
                } catch {
                    if ($spokeDate -gt $hubDate) {
                        Write-Host "   [DRIFT] $slug — spoke: $spokeDate > hub: $hubDate ($($source.project))" -ForegroundColor Yellow
                        $driftCount++
                    }
                }
            }
        }
    }
} else {
    Write-Host "   [SKIP] No import-sources.json found at: $ConfigPath" -ForegroundColor DarkGray
}

if ($driftCount -eq 0) {
    Write-Host "   All spokes in sync with hub" -ForegroundColor Green
}
Write-Host ""

# --- 3. Broken References ---
Write-Host "3. BROKEN REFERENCES" -ForegroundColor Yellow
Write-Host "   ------------------"

$brokenCount = 0
foreach ($slug in ($hubEntries.Keys | Sort-Object)) {
    $entry = $hubEntries[$slug]
    $related = $entry.RelatedEntries
    if (-not $related) { continue }

    $refs = $related -split '\s*,\s*' | Where-Object { $_ }
    foreach ($ref in $refs) {
        if (-not $hubEntries.ContainsKey($ref)) {
            Write-Host "   [BROKEN] $slug -> $ref (not found)" -ForegroundColor Red
            $brokenCount++
        }
    }
}

if ($brokenCount -eq 0) {
    Write-Host "   All related_entries references are valid" -ForegroundColor Green
}
Write-Host ""

# --- 4. Graph Analysis (orphans + hubs) ---
Write-Host "4. GRAPH ANALYSIS" -ForegroundColor Yellow
Write-Host "   ---------------"

if (Test-Path $graphPath) {
    $graph = Get-Content $graphPath -Raw | ConvertFrom-Json

    # Count inbound references per node
    $inbound = @{}
    foreach ($node in $graph.nodes) {
        $inbound[$node.id] = 0
    }
    foreach ($edge in $graph.edges) {
        $target = if ($edge.target -is [string]) { $edge.target } else { $edge.target.id }
        if ($inbound.ContainsKey($target)) {
            $inbound[$target]++
        }
    }

    # Orphans (0 inbound)
    $orphans = $inbound.GetEnumerator() | Where-Object { $_.Value -eq 0 } | Sort-Object Name
    if ($orphans.Count -gt 0) {
        Write-Host "   Orphan entries (0 inbound references):" -ForegroundColor DarkYellow
        foreach ($o in $orphans) {
            $type = if ($hubEntries[$o.Name]) { $hubEntries[$o.Name].EntryType } else { "?" }
            Write-Host "   - $($o.Name) ($type)" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "   No orphan entries" -ForegroundColor Green
    }

    # Critical hubs (3+ inbound)
    $hubs = $inbound.GetEnumerator() | Where-Object { $_.Value -ge 3 } | Sort-Object Value -Descending
    if ($hubs.Count -gt 0) {
        Write-Host "   Critical hub entries (3+ inbound refs):" -ForegroundColor DarkYellow
        foreach ($h in $hubs) {
            $type = if ($hubEntries[$h.Name]) { $hubEntries[$h.Name].EntryType } else { "?" }
            Write-Host "   - $($h.Name) ($type) — $($h.Value) inbound" -ForegroundColor DarkGray
        }
    }

    Write-Host "   Total nodes: $($graph.nodes.Count) | Total edges: $($graph.edges.Count)"
} else {
    Write-Host "   [SKIP] graph.json not found at: $graphPath" -ForegroundColor DarkGray
    Write-Host "   Run 'dotnet run' to generate it."
}
Write-Host ""

# --- Summary ---
$staleCount = $staleEntries.Count
$orphanCount = if (Test-Path $graphPath) { ($orphans | Measure-Object).Count } else { 0 }
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "-------"
Write-Host "Memex Health: $totalEntries entries | $staleCount stale | $driftCount drifted | $brokenCount broken refs | $orphanCount orphans"
Write-Host ""
