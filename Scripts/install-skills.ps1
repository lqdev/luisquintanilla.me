# Install AI Memex Skills & Global Instructions
# Copies universal skills to ~/.agents/skills/ and global instructions to ~/.copilot/
#
# Usage: .\Scripts\install-skills.ps1
# Run from the lqdev.me repo root.

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Verify we're in the right repo
if (-not (Test-Path "PersonalSite.fsproj")) {
    Write-Error "Run this script from the lqdev.me repo root (PersonalSite.fsproj not found)"
    exit 1
}

$repoRoot = Get-Location
$skillSource = Join-Path $repoRoot ".github\skills\universal"
$skillTarget = Join-Path $HOME ".agents\skills"
$globalSource = Join-Path $repoRoot ".github\global\copilot-instructions.md"
$globalTarget = Join-Path $HOME ".copilot\copilot-instructions.md"

Write-Host "AI Memex Skills Installer" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# --- Install Universal Skills ---
if (-not (Test-Path $skillTarget)) {
    New-Item -ItemType Directory -Path $skillTarget -Force | Out-Null
}

Write-Host "Installing universal skills to: $skillTarget" -ForegroundColor Yellow

$skills = Get-ChildItem -Path $skillSource -Directory
foreach ($skill in $skills) {
    $targetSkillDir = Join-Path $skillTarget $skill.Name

    if ((Test-Path $targetSkillDir) -and -not $Force) {
        Write-Host "  [EXISTS] $($skill.Name) (use -Force to overwrite)" -ForegroundColor DarkGray
    } else {
        $tempDir = $null
        if (Test-Path $targetSkillDir) {
            # Preserve user-local config files (e.g. import-sources.json) that
            # exist only at the installed location and would be lost on delete
            $userConfigs = Get-ChildItem -Path $targetSkillDir -Filter "*.json" -File -ErrorAction SilentlyContinue |
                Where-Object { -not (Test-Path (Join-Path $skill.FullName $_.Name)) }
            if ($userConfigs) {
                $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "skill-backup-$($skill.Name)-$([guid]::NewGuid().ToString('N').Substring(0,8))"
                New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
                foreach ($cfg in $userConfigs) {
                    Copy-Item -Path $cfg.FullName -Destination $tempDir -Force
                    Write-Host "  [BACKUP] $($cfg.Name)" -ForegroundColor DarkYellow
                }
            }

            Remove-Item -Path $targetSkillDir -Recurse -Force
        }
        Copy-Item -Path $skill.FullName -Destination $targetSkillDir -Recurse

        # Restore preserved config files
        if ($tempDir -and (Test-Path $tempDir)) {
            foreach ($cfg in (Get-ChildItem -Path $tempDir -File)) {
                Copy-Item -Path $cfg.FullName -Destination $targetSkillDir
                Write-Host "  [RESTORED] $($cfg.Name)" -ForegroundColor DarkYellow
            }
            Remove-Item -Path $tempDir -Recurse -Force
        }

        Write-Host "  [INSTALLED] $($skill.Name)" -ForegroundColor Green
    }
}

# --- Install Global Instructions ---
Write-Host ""
Write-Host "Installing global instructions to: $globalTarget" -ForegroundColor Yellow

$copilotDir = Join-Path $HOME ".copilot"
if (-not (Test-Path $copilotDir)) {
    New-Item -ItemType Directory -Path $copilotDir -Force | Out-Null
}

$memexMarker = "## AI Memex Companion"

if (Test-Path $globalTarget) {
    $existing = Get-Content $globalTarget -Raw

    if ($existing -match [regex]::Escape($memexMarker)) {
        if ($Force) {
            # Remove old AI Memex section and replace
            $pattern = "(?s)## AI Memex Companion.*"
            $cleaned = $existing -replace $pattern, ""
            $newContent = Get-Content $globalSource -Raw
            $result = $cleaned.TrimEnd() + "`n`n" + $newContent
            Set-Content -Path $globalTarget -Value $result -NoNewline
            Write-Host "  [UPDATED] AI Memex section replaced" -ForegroundColor Green
        } else {
            Write-Host "  [EXISTS] AI Memex section already present (use -Force to update)" -ForegroundColor DarkGray
        }
    } else {
        # Append AI Memex section to existing file
        $newContent = Get-Content $globalSource -Raw
        $result = $existing.TrimEnd() + "`n`n" + $newContent
        Set-Content -Path $globalTarget -Value $result -NoNewline
        Write-Host "  [APPENDED] AI Memex section added to existing file" -ForegroundColor Green
    }
} else {
    Copy-Item -Path $globalSource -Destination $globalTarget
    Write-Host "  [CREATED] Global instructions file" -ForegroundColor Green
}

# --- Summary ---
Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Skills available in any project via ~/.agents/skills/"
Write-Host "Global trigger discipline active in every Copilot CLI session"
Write-Host ""
Write-Host "To verify: run 'copilot' and type '/skills' to see available skills"
