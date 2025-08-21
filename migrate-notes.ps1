# Migration Script - Move Note Files from _src/feed/ to _src/notes/
# This script safely moves 240 note files to their correct location

Write-Host "CONTENT MIGRATION: NOTES" -ForegroundColor Yellow
Write-Host "=======================" -ForegroundColor Yellow
Write-Host "Migration Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

$srcDir = "c:\Dev\website\_src"
$feedDir = Join-Path $srcDir "feed"
$notesDir = Join-Path $srcDir "notes"

# Function to extract frontmatter field
function Get-FrontmatterField {
    param(
        [string]$FilePath,
        [string]$FieldName
    )
    
    try {
        $content = Get-Content $FilePath -Raw
        if ($content -match '(?s)^---\s*\n(.*?)\n---') {
            $frontmatter = $Matches[1]
            if ($frontmatter -match "(?m)^${FieldName}:\s*[`"']?([^`"'\n\r]+)[`"']?") {
                return $Matches[1].Trim()
            }
        }
    }
    catch {
        Write-Warning "Error reading $FilePath`: $($_.Exception.Message)"
    }
    return $null
}

# Ensure destination directory exists
if (!(Test-Path $notesDir)) {
    Write-Host "Creating directory: $notesDir" -ForegroundColor Green
    New-Item -ItemType Directory -Path $notesDir -Force | Out-Null
}

# Get all markdown files in feed directory
$feedFiles = Get-ChildItem $feedDir -Filter "*.md"
Write-Host "Scanning $($feedFiles.Count) files in feed directory..."

$notesToMove = @()
$skipped = @()

# Identify note files
foreach ($file in $feedFiles) {
    $postType = Get-FrontmatterField $file.FullName "post_type"
    
    if ($postType -and $postType.ToLower() -eq "note") {
        $notesToMove += $file
    }
    else {
        $skipped += @{
            File = $file.Name
            Type = if ($postType) { $postType } else { "no-type" }
        }
    }
}

Write-Host "Found $($notesToMove.Count) note files to move" -ForegroundColor Green
Write-Host "Skipping $($skipped.Count) non-note files" -ForegroundColor Yellow

if ($skipped.Count -gt 0) {
    Write-Host "`nFiles remaining in feed directory:" -ForegroundColor Yellow
    $skipped | ForEach-Object { Write-Host "  $($_.File) [$($_.Type)]" }
}

# Confirm migration
$confirm = Read-Host "`nProceed with moving $($notesToMove.Count) note files? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Migration cancelled." -ForegroundColor Red
    exit
}

# Perform migration
Write-Host "`nStarting migration..." -ForegroundColor Cyan
$moveCount = 0
$errorCount = 0

foreach ($file in $notesToMove) {
    try {
        $destination = Join-Path $notesDir $file.Name
        
        # Check if destination already exists
        if (Test-Path $destination) {
            Write-Warning "Destination file already exists: $($file.Name)"
            $errorCount++
            continue
        }
        
        # Move the file
        Move-Item $file.FullName $destination
        $moveCount++
        
        if ($moveCount % 50 -eq 0) {
            Write-Host "  Moved $moveCount files..." -ForegroundColor Green
        }
    }
    catch {
        Write-Error "Failed to move $($file.Name): $($_.Exception.Message)"
        $errorCount++
    }
}

# Summary
Write-Host "`n=== MIGRATION COMPLETE ===" -ForegroundColor Magenta
Write-Host "Successfully moved: $moveCount files" -ForegroundColor Green
Write-Host "Errors: $errorCount files" -ForegroundColor Red
Write-Host "Files remaining in feed: $($skipped.Count)" -ForegroundColor Yellow

if ($moveCount -gt 0) {
    Write-Host "`nNEXT STEPS:" -ForegroundColor Cyan
    Write-Host "1. Update Builder.fs to scan _src/notes/ instead of filtering _src/feed/"
    Write-Host "2. Test build process"
    Write-Host "3. Verify RSS feeds generate correctly"
}

Write-Host "`nMigration log saved to: notes-migration-log.txt" -ForegroundColor Cyan

# Save migration log
$logContent = @"
Notes Migration Log - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
================================================================

Files moved: $moveCount
Errors: $errorCount
Files remaining in feed: $($skipped.Count)

Moved files:
$($notesToMove | ForEach-Object { "  $($_.Name)" } | Out-String)

Files remaining in feed:
$($skipped | ForEach-Object { "  $($_.File) [$($_.Type)]" } | Out-String)
"@

$logContent | Out-File "notes-migration-log.txt" -Encoding UTF8
