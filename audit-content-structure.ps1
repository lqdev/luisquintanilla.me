# Content Structure Audit Script
# Analyzes content types in feed and responses directories

Write-Host "CONTENT STRUCTURE AUDIT" -ForegroundColor Yellow
Write-Host "======================" -ForegroundColor Yellow
Write-Host "Audit Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

$srcDir = "c:\Dev\website\_src"
$feedDir = Join-Path $srcDir "feed"
$responsesDir = Join-Path $srcDir "responses"

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

# Audit feed directory
Write-Host "=== FEED DIRECTORY AUDIT ===" -ForegroundColor Cyan
$feedFiles = Get-ChildItem $feedDir -Filter "*.md"
Write-Host "Total files in feed directory: $($feedFiles.Count)"

$noteFiles = @()
$feedPosts = @()
$otherTypes = @()
$noType = @()

foreach ($file in $feedFiles) {
    $postType = Get-FrontmatterField $file.FullName "post_type"
    
    if ($postType) {
        switch ($postType.ToLower()) {
            "note" { $noteFiles += $file.Name }
            "feed" { $feedPosts += $file.Name }
            default { $otherTypes += "$($file.Name) ($postType)" }
        }
    }
    else {
        $noType += $file.Name
    }
}

Write-Host "Notes (should move to _src/notes/): $($noteFiles.Count)" -ForegroundColor Green
Write-Host "Feed posts (stay in _src/feed/): $($feedPosts.Count)" -ForegroundColor White
Write-Host "Other post types: $($otherTypes.Count)" -ForegroundColor Yellow
Write-Host "No post_type specified: $($noType.Count)" -ForegroundColor Red

if ($noteFiles.Count -gt 0) {
    Write-Host "`n--- Note files to move ---" -ForegroundColor Green
    $noteFiles | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    if ($noteFiles.Count -gt 10) {
        Write-Host "  ... and $($noteFiles.Count - 10) more"
    }
}

if ($otherTypes.Count -gt 0) {
    Write-Host "`n--- Other post types found ---" -ForegroundColor Yellow
    $otherTypes | ForEach-Object { Write-Host "  $_" }
}

# Audit responses directory
Write-Host "`n=== RESPONSES DIRECTORY AUDIT ===" -ForegroundColor Cyan
$responseFiles = Get-ChildItem $responsesDir -Filter "*.md"
Write-Host "Total files in responses directory: $($responseFiles.Count)"

$bookmarkFiles = @()
$replyFiles = @()
$reshareFiles = @()
$starFiles = @()
$otherResponseTypes = @()
$noResponseType = @()

foreach ($file in $responseFiles) {
    $responseType = Get-FrontmatterField $file.FullName "response_type"
    
    if ($responseType) {
        switch ($responseType.ToLower()) {
            "bookmark" { $bookmarkFiles += $file.Name }
            "reply" { $replyFiles += $file.Name }
            "reshare" { $reshareFiles += $file.Name }
            "star" { $starFiles += $file.Name }
            default { $otherResponseTypes += "$($file.Name) ($responseType)" }
        }
    }
    else {
        $noResponseType += $file.Name
    }
}

Write-Host "Bookmarks (should move to _src/bookmarks/): $($bookmarkFiles.Count)" -ForegroundColor Green
Write-Host "Replies (stay in _src/responses/): $($replyFiles.Count)" -ForegroundColor White
Write-Host "Reshares (stay in _src/responses/): $($reshareFiles.Count)" -ForegroundColor White
Write-Host "Stars (stay in _src/responses/): $($starFiles.Count)" -ForegroundColor White
Write-Host "Other response types: $($otherResponseTypes.Count)" -ForegroundColor Yellow
Write-Host "No response_type specified: $($noResponseType.Count)" -ForegroundColor Red

if ($bookmarkFiles.Count -gt 0) {
    Write-Host "`n--- Bookmark files to move ---" -ForegroundColor Green
    $bookmarkFiles | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    if ($bookmarkFiles.Count -gt 10) {
        Write-Host "  ... and $($bookmarkFiles.Count - 10) more"
    }
}

if ($otherResponseTypes.Count -gt 0) {
    Write-Host "`n--- Other response types found ---" -ForegroundColor Yellow
    $otherResponseTypes | ForEach-Object { Write-Host "  $_" }
}

# Summary
Write-Host "`n=== AUDIT SUMMARY ===" -ForegroundColor Magenta
Write-Host "Files to move from _src/feed/ to _src/notes/: $($noteFiles.Count)" -ForegroundColor Green
Write-Host "Files to move from _src/responses/ to _src/bookmarks/: $($bookmarkFiles.Count)" -ForegroundColor Green
Write-Host "Files remaining in _src/feed/: $($feedPosts.Count + $otherTypes.Count + $noType.Count)"
Write-Host "Files remaining in _src/responses/: $($replyFiles.Count + $reshareFiles.Count + $starFiles.Count + $otherResponseTypes.Count + $noResponseType.Count)"

Write-Host "`n=== NEXT STEPS ===" -ForegroundColor Magenta
if ($noteFiles.Count -gt 0) {
    Write-Host "1. Create migration script to move $($noteFiles.Count) note files to _src/notes/" -ForegroundColor Green
}
if ($bookmarkFiles.Count -gt 0) {
    Write-Host "2. Create migration script to move $($bookmarkFiles.Count) bookmark files to _src/bookmarks/" -ForegroundColor Green
}
Write-Host "3. Update Builder.fs file path references"
Write-Host "4. Test build process"

# Export file lists for migration scripts
$noteFiles | Out-File "note-files-to-move.txt" -Encoding UTF8
$bookmarkFiles | Out-File "bookmark-files-to-move.txt" -Encoding UTF8

Write-Host "`nFile lists exported:" -ForegroundColor Cyan
Write-Host "- note-files-to-move.txt ($($noteFiles.Count) files)"
Write-Host "- bookmark-files-to-move.txt ($($bookmarkFiles.Count) files)"
