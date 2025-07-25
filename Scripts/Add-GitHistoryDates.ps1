# Git History Date Extraction and Frontmatter Enhancement Script
# Extracts creation dates from Git history and adds them to snippet/wiki frontmatter

param(
    [switch]$DryRun = $false,
    [switch]$Test = $false
)

# Function to get file creation date from Git history
function Get-FileCreationDate {
    param([string]$FilePath)
    
    $fileName = Split-Path $FilePath -Leaf
    
    try {
        $output = & git log --all --full-history --format="%aI" --reverse -- "**/$fileName" 2>$null
        if ($output -and $output.Count -gt 0) {
            $firstDate = $output[0].Trim()
            return [DateTime]::Parse($firstDate)
        }
    }
    catch {
        Write-Warning "Could not get creation date for $fileName"
    }
    
    return $null
}

# Function to get file last modification date from Git history  
function Get-FileLastModificationDate {
    param([string]$FilePath)
    
    $fileName = Split-Path $FilePath -Leaf
    
    try {
        $output = & git log --all --full-history --format="%aI" -- "**/$fileName" 2>$null
        if ($output -and $output.Count -gt 0) {
            $firstDate = $output[0].Trim()
            return [DateTime]::Parse($firstDate)
        }
    }
    catch {
        Write-Warning "Could not get modification date for $fileName"
    }
    
    return $null
}

# Function to format date for frontmatter
function Format-DateForFrontmatter {
    param([DateTime]$Date)
    return $Date.ToString("MM/dd/yyyy HH:mm") + " -05:00"
}

# Function to add date to frontmatter
function Add-DateToFrontmatter {
    param(
        [string]$FilePath,
        [DateTime]$Date,
        [string]$DateFieldName
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found: $FilePath"
        return $false
    }
    
    $content = Get-Content $FilePath -Raw
    $formattedDate = Format-DateForFrontmatter $Date
    
    # Check if file has frontmatter
    if (-not $content.StartsWith("---")) {
        Write-Warning "No frontmatter found in: $(Split-Path $FilePath -Leaf)"
        return $false
    }
    
    # Check if date field already exists
    if ($content -match "$DateFieldName\s*:") {
        Write-Host "  Date field '$DateFieldName' already exists, skipping..." -ForegroundColor Yellow
        return $false
    }
    
    # Find the end of frontmatter
    $lines = $content -split "`n"
    $frontmatterEnd = -1
    
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq "---") {
            $frontmatterEnd = $i
            break
        }
    }
    
    if ($frontmatterEnd -eq -1) {
        Write-Warning "Could not find end of frontmatter in: $(Split-Path $FilePath -Leaf)"
        return $false
    }
    
    # Insert the date field before the closing ---
    $newLine = "$DateFieldName`: `"$formattedDate`""
    $lines[$frontmatterEnd] = $newLine + "`n---"
    
    $newContent = $lines -join "`n"
    
    if ($DryRun) {
        Write-Host "  [DRY RUN] Would add: $newLine" -ForegroundColor Cyan
    } else {
        Set-Content $FilePath $newContent -NoNewline
        Write-Host "  Added: $newLine" -ForegroundColor Green
    }
    
    return $true
}

# Test function to show what we can extract
function Test-GitHistoryExtraction {
    Write-Host "Testing Git History Date Extraction" -ForegroundColor Blue
    Write-Host "====================================" -ForegroundColor Blue
    
    $testFiles = @(
        "_src/resources/snippets/fsharp-data-rss-parser.md",
        "_src/resources/snippets/lqdev-me-website-post-metrics.md", 
        "_src/resources/wiki/devcontainers-configurations.md",
        "_src/resources/snippets/create-matrix-user-cli.md"
    )
    
    foreach ($filePath in $testFiles) {
        if (Test-Path $filePath) {
            $fileName = Split-Path $filePath -Leaf
            Write-Host "`nFile: $fileName" -ForegroundColor White
            
            $creationDate = Get-FileCreationDate $filePath
            if ($creationDate) {
                $formatted = Format-DateForFrontmatter $creationDate
                Write-Host "  Creation Date: $($creationDate.ToString('yyyy-MM-dd HH:mm:ss')) ($formatted)" -ForegroundColor Green
            } else {
                Write-Host "  Creation Date: Not found in Git history" -ForegroundColor Red
            }
            
            $modDate = Get-FileLastModificationDate $filePath
            if ($modDate) {
                $formatted = Format-DateForFrontmatter $modDate  
                Write-Host "  Last Modified: $($modDate.ToString('yyyy-MM-dd HH:mm:ss')) ($formatted)" -ForegroundColor Green
            } else {
                Write-Host "  Last Modified: Not found in Git history" -ForegroundColor Red
            }
        } else {
            Write-Host "`nFile not found: $filePath" -ForegroundColor Red
        }
    }
}

# Process all snippets
function Process-Snippets {
    $snippetsDir = "_src/resources/snippets"
    
    if (-not (Test-Path $snippetsDir)) {
        Write-Warning "Snippets directory not found: $snippetsDir"
        return
    }
    
    $snippetFiles = Get-ChildItem $snippetsDir -Filter "*.md"
    Write-Host "`nProcessing Snippets ($($snippetFiles.Count) files):" -ForegroundColor Blue
    
    $processed = 0
    $skipped = 0
    
    foreach ($file in $snippetFiles) {
        Write-Host "`nProcessing: $($file.Name)" -ForegroundColor White
        
        $creationDate = Get-FileCreationDate $file.FullName
        if ($creationDate) {
            if (Add-DateToFrontmatter $file.FullName $creationDate "created_date") {
                $processed++
            } else {
                $skipped++
            }
        } else {
            Write-Host "  No Git history found, skipping..." -ForegroundColor Yellow
            $skipped++
        }
    }
    
    Write-Host "`nSnippets Summary: $processed processed, $skipped skipped" -ForegroundColor Blue
}

# Process all wikis
function Process-Wikis {
    $wikiDir = "_src/resources/wiki"
    
    if (-not (Test-Path $wikiDir)) {
        Write-Warning "Wiki directory not found: $wikiDir"
        return
    }
    
    $wikiFiles = Get-ChildItem $wikiDir -Filter "*.md"
    Write-Host "`nProcessing Wikis ($($wikiFiles.Count) files):" -ForegroundColor Blue
    
    $processed = 0
    $skipped = 0
    
    foreach ($file in $wikiFiles) {
        Write-Host "`nProcessing: $($file.Name)" -ForegroundColor White
        
        $modDate = Get-FileLastModificationDate $file.FullName
        if ($modDate) {
            if (Add-DateToFrontmatter $file.FullName $modDate "last_updated_date") {
                $processed++
            } else {
                $skipped++
            }
        } else {
            Write-Host "  No Git history found, skipping..." -ForegroundColor Yellow
            $skipped++
        }
    }
    
    Write-Host "`nWikis Summary: $processed processed, $skipped skipped" -ForegroundColor Blue
}

# Main execution
Write-Host "Git History Date Enhancement Script" -ForegroundColor Magenta
Write-Host "===================================" -ForegroundColor Magenta

if ($Test) {
    Test-GitHistoryExtraction
    Write-Host "`nTest complete! Use without -Test flag to process files." -ForegroundColor Magenta
} else {
    if ($DryRun) {
        Write-Host "DRY RUN MODE - No files will be modified" -ForegroundColor Yellow
    }
    
    Process-Snippets
    Process-Wikis
    
    Write-Host "`nProcessing complete!" -ForegroundColor Magenta
    if ($DryRun) {
        Write-Host "Run without -DryRun to actually modify files." -ForegroundColor Yellow
    } else {
        Write-Host "Files have been updated with Git history dates." -ForegroundColor Green
    }
}
