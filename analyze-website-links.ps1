# Website Link Analysis Script
# Analyzes all URLs on the website, categorizes them, and checks for broken links

param(
    [string]$SiteRoot = "c:\Dev\website\_public",
    [string]$BaseUrl = "http://localhost:8080",
    [string]$OutputFile = "link-analysis-report.json",
    [switch]$CheckExternal = $false,
    [int]$MaxConcurrentChecks = 10
)

# Initialize collections
$allLinks = @()
$internalLinks = @()
$externalLinks = @()
$brokenLinks = @()
$pageLinks = @{}
$linkCounts = @{
    Total = 0
    Internal = 0
    External = 0
    Broken = 0
}

# Function to extract links from HTML content
function Get-LinksFromHtml {
    param(
        [string]$HtmlContent,
        [string]$PagePath
    )
    
    $links = @()
    
    # Regex patterns for different types of links
    $patterns = @{
        'href' = 'href\s*=\s*["' + "'" + ']([^"' + "'" + ']+)["' + "'" + ']'
        'src' = 'src\s*=\s*["' + "'" + ']([^"' + "'" + ']+)["' + "'" + ']'
        'action' = 'action\s*=\s*["' + "'" + ']([^"' + "'" + ']+)["' + "'" + ']'
        'content' = 'content\s*=\s*["' + "'" + ']([^"' + "'" + ']*(?:https?://[^"' + "'" + ']+)[^"' + "'" + ']*)["' + "'" + ']'
    }
    
    foreach ($patternName in $patterns.Keys) {
        $pattern = $patterns[$patternName]
        $matches = [regex]::Matches($HtmlContent, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        
        foreach ($match in $matches) {
            $url = $match.Groups[1].Value
            
            # Skip empty, javascript, mailto, tel, and anchor-only links
            if ($url -and $url -ne "" -and 
                $url -notmatch "^javascript:" -and 
                $url -notmatch "^mailto:" -and 
                $url -notmatch "^tel:" -and
                $url -ne "#") {
                
                $links += [PSCustomObject]@{
                    Url = $url
                    Type = $patternName
                    Page = $PagePath
                }
            }
        }
    }
    
    return $links
}

# Function to normalize URLs
function Normalize-Url {
    param([string]$Url, [string]$BaseUrl)
    
    if ($Url.StartsWith("//")) {
        return "https:$Url"
    }
    elseif ($Url.StartsWith("/")) {
        return "$BaseUrl$Url"
    }
    elseif ($Url.StartsWith("http://") -or $Url.StartsWith("https://")) {
        return $Url
    }
    elseif ($Url.StartsWith("./") -or -not $Url.Contains("://")) {
        # Relative URL
        return "$BaseUrl/$($Url.TrimStart('./'))"
    }
    else {
        return $Url
    }
}

# Function to check if URL is internal
function Test-InternalUrl {
    param([string]$Url, [string]$BaseUrl)
    
    $normalizedUrl = Normalize-Url -Url $Url -BaseUrl $BaseUrl
    
    # Check for localhost (development)
    if ($normalizedUrl.StartsWith($BaseUrl)) {
        return $true
    }
    
    # Check for production domains (both are internal to this website)
    if ($normalizedUrl.StartsWith("https://www.luisquintanilla.me") -or 
        $normalizedUrl.StartsWith("https://luisquintanilla.me") -or
        $normalizedUrl.StartsWith("https://www.lqdev.me") -or 
        $normalizedUrl.StartsWith("https://lqdev.me")) {
        return $true
    }
    
    return $false
}

# Function to check if URL is accessible (lightweight HEAD request)
function Test-UrlAccessible {
    param([string]$Url)
    
    try {
        # Use HEAD request for lightweight checking
        $response = Invoke-WebRequest -Uri $Url -Method Head -TimeoutSec 5 -UseBasicParsing -DisableKeepAlive -ErrorAction Stop
        return @{
            Accessible = $true
            StatusCode = $response.StatusCode
            Error = $null
            UrlTested = $Url
        }
    }
    catch {
        # For internal URLs that might be directories, try with trailing slash
        if ($Url -match "^http://localhost:8080" -and $Url -notmatch "\/$" -and $Url -notmatch "\.[a-zA-Z0-9]{2,4}$") {
            try {
                $urlWithSlash = $Url + "/"
                $response = Invoke-WebRequest -Uri $urlWithSlash -Method Head -TimeoutSec 5 -UseBasicParsing -DisableKeepAlive -ErrorAction Stop
                return @{
                    Accessible = $true
                    StatusCode = $response.StatusCode
                    Error = "Fixed with trailing slash"
                    UrlTested = $urlWithSlash
                }
            }
            catch {
                # Both attempts failed
            }
        }
        
        # Extract status code from error if available
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        return @{
            Accessible = $false
            StatusCode = $statusCode
            Error = $_.Exception.Message
            UrlTested = $Url
        }
    }
}

# Function to get all HTML files recursively
function Get-HtmlFiles {
    param([string]$Path)
    
    return Get-ChildItem -Path $Path -Recurse -Include "*.html" | ForEach-Object {
        $relativePath = $_.FullName.Replace($SiteRoot, "").Replace("\", "/")
        if (-not $relativePath.StartsWith("/")) {
            $relativePath = "/$relativePath"
        }
        [PSCustomObject]@{
            FullPath = $_.FullName
            RelativePath = $relativePath
            FileName = $_.Name
        }
    }
}

Write-Host "Starting website link analysis..." -ForegroundColor Green
Write-Host "Site Root: $SiteRoot" -ForegroundColor Yellow
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow

# Get all HTML files
Write-Host "`nScanning for HTML files..." -ForegroundColor Blue
$htmlFiles = Get-HtmlFiles -Path $SiteRoot
Write-Host "Found $($htmlFiles.Count) HTML files" -ForegroundColor Green

# Extract links from each HTML file
Write-Host "`nExtracting links from HTML files..." -ForegroundColor Blue
$totalFiles = $htmlFiles.Count
$currentFile = 0

foreach ($file in $htmlFiles) {
    $currentFile++
    Write-Progress -Activity "Analyzing HTML files" -Status "Processing $($file.RelativePath)" -PercentComplete (($currentFile / $totalFiles) * 100)
    
    try {
        $content = Get-Content -Path $file.FullPath -Raw -Encoding UTF8
        $pageLinks[$file.RelativePath] = @()
        
        $links = Get-LinksFromHtml -HtmlContent $content -PagePath $file.RelativePath
        
        foreach ($link in $links) {
            $normalizedUrl = Normalize-Url -Url $link.Url -BaseUrl $BaseUrl
            $isInternal = Test-InternalUrl -Url $link.Url -BaseUrl $BaseUrl
            
            $linkObject = [PSCustomObject]@{
                OriginalUrl = $link.Url
                NormalizedUrl = $normalizedUrl
                IsInternal = $isInternal
                Type = $link.Type
                SourcePage = $link.Page
                IsAccessible = $null
                StatusCode = $null
                Error = $null
            }
            
            $allLinks += $linkObject
            $pageLinks[$file.RelativePath] += $linkObject
            
            if ($isInternal) {
                $internalLinks += $linkObject
            } else {
                $externalLinks += $linkObject
            }
        }
    }
    catch {
        Write-Warning "Error processing file $($file.RelativePath): $($_.Exception.Message)"
    }
}

Write-Progress -Activity "Analyzing HTML files" -Completed

# Update counts
$linkCounts.Total = $allLinks.Count
$linkCounts.Internal = $internalLinks.Count
$linkCounts.External = $externalLinks.Count

Write-Host "`nLink extraction complete!" -ForegroundColor Green
Write-Host "Total links found: $($linkCounts.Total)" -ForegroundColor Yellow
Write-Host "Internal links: $($linkCounts.Internal)" -ForegroundColor Yellow
Write-Host "External links: $($linkCounts.External)" -ForegroundColor Yellow

# Check internal links for accessibility
Write-Host "`nChecking internal links..." -ForegroundColor Blue
$uniqueInternalLinks = $internalLinks | Sort-Object NormalizedUrl -Unique
$totalInternal = $uniqueInternalLinks.Count
$currentInternal = 0

# Create hashtable for fast URL lookups - optimization for O(1) access
$urlStatusCache = @{}

foreach ($link in $uniqueInternalLinks) {
    $currentInternal++
    Write-Progress -Activity "Checking internal links" -Status "Checking $($link.NormalizedUrl)" -PercentComplete (($currentInternal / $totalInternal) * 100)
    
    $result = Test-UrlAccessible -Url $link.NormalizedUrl
    
    # Cache the result for this URL
    $urlStatusCache[$link.NormalizedUrl] = $result
    
    if (-not $result.Accessible) {
        $brokenLinks += $link
    }
    
    # Show progress
    $status = if ($result.Accessible) { "OK ($($result.StatusCode))" } else { "BROKEN" }
    Write-Host "  [$currentInternal/$totalInternal] $($link.NormalizedUrl) - $status" -ForegroundColor $(if ($result.Accessible) { 'Green' } else { 'Red' })
}

# Update all links with cached results in a single pass - O(n) instead of O(n²)
Write-Host "Updating link status..." -ForegroundColor Blue
foreach ($link in $allLinks) {
    if ($urlStatusCache.ContainsKey($link.NormalizedUrl)) {
        $cachedResult = $urlStatusCache[$link.NormalizedUrl]
        $link.IsAccessible = $cachedResult.Accessible
        $link.StatusCode = $cachedResult.StatusCode
        $link.Error = $cachedResult.Error
    }
}

Write-Progress -Activity "Checking internal links" -Completed

# Check external links if requested
if ($CheckExternal) {
    Write-Host "`nChecking external links (limited sample)..." -ForegroundColor Blue
    $uniqueExternalLinks = $externalLinks | Sort-Object NormalizedUrl -Unique | Select-Object -First 20 # Reduced limit for performance
    $totalExternal = $uniqueExternalLinks.Count
    
    Write-Host "Checking $totalExternal external links with rate limiting..." -ForegroundColor Yellow
    
    foreach ($link in $uniqueExternalLinks) {
        Write-Host "  Checking: $($link.NormalizedUrl)" -ForegroundColor Gray
        
        $result = Test-UrlAccessible -Url $link.NormalizedUrl
        
        # Cache the result for this URL
        $urlStatusCache[$link.NormalizedUrl] = $result
        
        if (-not $result.Accessible) {
            $brokenLinks += $link
        }
        
        Write-Host "    Status: $(if ($result.Accessible) { "OK ($($result.StatusCode))" } else { "BROKEN - $($result.Error)" })" -ForegroundColor $(if ($result.Accessible) { 'Green' } else { 'Red' })
        
        # Rate limiting to be respectful
        Start-Sleep -Milliseconds 500
    }
    
    # Update external links with cached results
    Write-Host "Updating external link status..." -ForegroundColor Blue
    foreach ($link in $allLinks) {
        if (-not $link.IsInternal -and $urlStatusCache.ContainsKey($link.NormalizedUrl)) {
            $cachedResult = $urlStatusCache[$link.NormalizedUrl]
            $link.IsAccessible = $cachedResult.Accessible
            $link.StatusCode = $cachedResult.StatusCode
            $link.Error = $cachedResult.Error
        }
    }
}

# Update broken links count
$linkCounts.Broken = ($allLinks | Where-Object { $_.IsAccessible -eq $false }).Count

# Generate report
Write-Host "`nGenerating report..." -ForegroundColor Blue

# Create source pages lookup for broken links - optimization
$linksByUrl = @{}
foreach ($link in $allLinks) {
    if (-not $linksByUrl.ContainsKey($link.NormalizedUrl)) {
        $linksByUrl[$link.NormalizedUrl] = @()
    }
    $linksByUrl[$link.NormalizedUrl] += $link.SourcePage
}

$report = @{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    SiteRoot = $SiteRoot
    BaseUrl = $BaseUrl
    Summary = $linkCounts
    BrokenLinks = @($brokenLinks | Sort-Object NormalizedUrl -Unique | ForEach-Object {
        @{
            Url = $_.NormalizedUrl
            OriginalUrl = $_.OriginalUrl
            Error = $_.Error
            StatusCode = $_.StatusCode
            SourcePages = @($linksByUrl[$_.NormalizedUrl] | Select-Object -Unique)
        }
    })
    LinksByPage = @{}
    TopExternalDomains = @($externalLinks | ForEach-Object { 
        try { ([System.Uri]$_.NormalizedUrl).Host } catch { "unknown" }
    } | Group-Object | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object {
        @{ Domain = $_.Name; Count = $_.Count }
    })
    InternalLinkDistribution = @($internalLinks | Group-Object SourcePage | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object {
        @{ Page = $_.Name; LinkCount = $_.Count }
    })
}

# Add links by page
Write-Host "Processing page-level statistics..." -ForegroundColor Blue
$totalPages = $pageLinks.Keys.Count
$currentPage = 0

foreach ($page in $pageLinks.Keys) {
    $currentPage++
    Write-Progress -Activity "Generating page reports" -Status "Processing $page" -PercentComplete (($currentPage / $totalPages) * 100)
    
    $report.LinksByPage[$page] = @{
        TotalLinks = $pageLinks[$page].Count
        InternalLinks = ($pageLinks[$page] | Where-Object IsInternal).Count
        ExternalLinks = ($pageLinks[$page] | Where-Object { -not $_.IsInternal }).Count
        BrokenLinks = ($pageLinks[$page] | Where-Object { $_.IsAccessible -eq $false }).Count
        Links = @($pageLinks[$page] | ForEach-Object {
            @{
                Url = $_.NormalizedUrl
                OriginalUrl = $_.OriginalUrl
                IsInternal = $_.IsInternal
                Type = $_.Type
                IsAccessible = $_.IsAccessible
                StatusCode = $_.StatusCode
                Error = $_.Error
            }
        })
    }
}

Write-Progress -Activity "Generating page reports" -Completed

# Save report to JSON
Write-Host "Saving detailed report to JSON..." -ForegroundColor Blue
$report | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8

# Display summary
Write-Host "`n" + "="*60 -ForegroundColor Green
Write-Host "WEBSITE LINK ANALYSIS REPORT" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Green

Write-Host "`nSUMMARY:" -ForegroundColor Yellow
Write-Host "  Total Links: $($linkCounts.Total)" -ForegroundColor White
Write-Host "  Internal Links: $($linkCounts.Internal)" -ForegroundColor Green
Write-Host "  External Links: $($linkCounts.External)" -ForegroundColor Blue
Write-Host "  Broken Links: $($linkCounts.Broken)" -ForegroundColor $(if ($linkCounts.Broken -eq 0) { "Green" } else { "Red" })

if ($brokenLinks.Count -gt 0) {
    Write-Host "`nBROKEN LINKS:" -ForegroundColor Red
    $uniqueBrokenLinks = $brokenLinks | Sort-Object NormalizedUrl -Unique
    foreach ($brokenLink in $uniqueBrokenLinks) {
        # Use pre-built lookup instead of scanning all links
        $sourcePagesForLink = $linksByUrl[$brokenLink.NormalizedUrl] | Select-Object -Unique
        Write-Host "  URL: $($brokenLink.NormalizedUrl)" -ForegroundColor Red
        Write-Host "    Error: $($brokenLink.Error)" -ForegroundColor Yellow
        Write-Host "    Found on pages:" -ForegroundColor Yellow
        foreach ($page in $sourcePagesForLink) {
            Write-Host "      - $page" -ForegroundColor Gray
        }
        Write-Host ""
    }
}

Write-Host "`nTOP EXTERNAL DOMAINS:" -ForegroundColor Yellow
foreach ($domain in $report.TopExternalDomains) {
    Write-Host "  $($domain.Domain): $($domain.Count) links" -ForegroundColor White
}

Write-Host "`nPAGES WITH MOST LINKS:" -ForegroundColor Yellow
foreach ($page in $report.InternalLinkDistribution) {
    Write-Host "  $($page.Page): $($page.LinkCount) links" -ForegroundColor White
}

Write-Host "`nDetailed report saved to: $OutputFile" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Green
