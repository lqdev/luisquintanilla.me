# Migration Script - Move Bookmark Files from _src/responses/ to _src/bookmarks/
# This script safely moves 290 bookmark files to their correct location

Write-Host "CONTENT MIGRATION: BOOKMARKS" -ForegroundColor Yellow
Write-Host "============================" -ForegroundColor Yellow
Write-Host "Migration Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

$srcDir = "c:\Dev\website\_src"
$responsesDir = Join-Path $srcDir "responses"
$bookmarksDir = Join-Path $srcDir "bookmarks"

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
if (!(Test-Path $bookmarksDir)) {
    Write-Host "Creating directory: $bookmarksDir" -ForegroundColor Green
    New-Item -ItemType Directory -Path $bookmarksDir -Force | Out-Null
}

# Get all markdown files in responses directory
$responseFiles = Get-ChildItem $responsesDir -Filter "*.md"
Write-Host "Scanning $($responseFiles.Count) files in responses directory..."

$bookmarksToMove = @()
$remaining = @()

# Identify bookmark files
foreach ($file in $responseFiles) {
    $responseType = Get-FrontmatterField $file.FullName "response_type"
    
    if ($responseType -and $responseType.ToLower() -eq "bookmark") {
        $bookmarksToMove += $file
    }
    else {
        $remaining += @{
            File = $file.Name
            Type = if ($responseType) { $responseType } else { "no-type" }
        }
    }
}

Write-Host "Found $($bookmarksToMove.Count) bookmark files to move" -ForegroundColor Green
Write-Host "Leaving $($remaining.Count) non-bookmark files in responses" -ForegroundColor Yellow

# Show what will remain in responses
$responseTypeGroups = $remaining | Group-Object Type | Sort-Object Count -Descending
Write-Host "`nFiles remaining in responses directory:" -ForegroundColor Yellow
$responseTypeGroups | ForEach-Object { 
    Write-Host "  $($_.Name): $($_.Count) files" 
}

# Confirm migration
$confirm = Read-Host "`nProceed with moving $($bookmarksToMove.Count) bookmark files? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Migration cancelled." -ForegroundColor Red
    exit
}

# Perform migration
Write-Host "`nStarting migration..." -ForegroundColor Cyan
$moveCount = 0
$errorCount = 0

foreach ($file in $bookmarksToMove) {
    try {
        $destination = Join-Path $bookmarksDir $file.Name
        
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
Write-Host "Files remaining in responses: $($remaining.Count)" -ForegroundColor Yellow

if ($moveCount -gt 0) {
    Write-Host "`nNEXT STEPS:" -ForegroundColor Cyan
    Write-Host "1. Update Builder.fs to scan _src/bookmarks/ instead of filtering _src/responses/"
    Write-Host "2. Test build process"
    Write-Host "3. Verify RSS feeds generate correctly"
}

Write-Host "`nMigration log saved to: bookmarks-migration-log.txt" -ForegroundColor Cyan

# Save migration log
$logContent = @"
Bookmarks Migration Log - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
===================================================================

Files moved: $moveCount
Errors: $errorCount
Files remaining in responses: $($remaining.Count)

Response types remaining in responses directory:
$($responseTypeGroups | ForEach-Object { "  $($_.Name): $($_.Count) files" } | Out-String)

Moved bookmark files:
$($bookmarksToMove | ForEach-Object { "  $($_.Name)" } | Out-String)
"@

$logContent | Out-File "bookmarks-migration-log.txt" -Encoding UTF8
