# Broken Links Analysis Script
# Analyzes the link-analysis-report.json to provide detailed broken links breakdown

Write-Host "=== BROKEN LINKS ANALYSIS ===" -ForegroundColor Yellow

try {
    # Load and parse the JSON report
    $report = Get-Content "link-analysis-report.json" | ConvertFrom-Json
    
    # Get total broken links count from summary
    Write-Host "Summary Total Broken: $($report.Summary.Broken)" -ForegroundColor Red
    
    # Count unique broken URLs
    $uniqueBrokenUrls = $report.BrokenLinks | Select-Object -Property OriginalUrl -Unique
    Write-Host "Unique Broken URLs: $($uniqueBrokenUrls.Count)" -ForegroundColor Cyan
    
    # Count total broken link instances (same URL on multiple pages)
    Write-Host "Total Broken Link Instances: $($report.BrokenLinks.Count)" -ForegroundColor Magenta
    
    # Show the breakdown
    Write-Host "`n=== BROKEN LINKS BREAKDOWN ===" -ForegroundColor Yellow
    $report.BrokenLinks | Group-Object OriginalUrl | Sort-Object Count -Descending | ForEach-Object {
        $url = $_.Name
        $count = $_.Count
        $pages = ($_.Group | Select-Object -ExpandProperty SourcePages | Sort-Object -Unique).Count
        Write-Host "$url : $count instances across $pages pages" -ForegroundColor White
    }
    
    # Show top problematic URLs (appearing on most pages)
    Write-Host "`n=== MOST WIDESPREAD BROKEN LINKS ===" -ForegroundColor Yellow
    $report.BrokenLinks | Group-Object OriginalUrl | ForEach-Object {
        $url = $_.Name
        $uniquePages = ($_.Group | Select-Object -ExpandProperty SourcePages | Sort-Object -Unique).Count
        [PSCustomObject]@{
            URL = $url
            PagesAffected = $uniquePages
            TotalInstances = $_.Count
        }
    } | Sort-Object PagesAffected -Descending | Select-Object -First 10 | Format-Table -AutoSize
    
    # Show all unique broken URLs for reference
    Write-Host "`n=== ALL UNIQUE BROKEN URLS ===" -ForegroundColor Yellow
    $uniqueBrokenUrls.OriginalUrl | Sort-Object | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error parsing JSON: $($_.Exception.Message)" -ForegroundColor Red
}