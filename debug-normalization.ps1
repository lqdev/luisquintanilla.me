# Focused debug - replicate the link analysis normalization logic
$testUrl = "/assets/images/contact/qr-bluesky.png"
$baseUrl = "http://localhost:8080"

Write-Host "Original URL: $testUrl" -ForegroundColor Yellow
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow

# Replicate the exact logic from the analyze-website-links.ps1 script
function Normalize-Url {
    param([string]$Url, [string]$BaseUrl)
    
    Write-Host "Normalize-Url called with: '$Url'" -ForegroundColor Cyan
    
    if ($Url.StartsWith("//")) {
        $result = "https:$Url"
        Write-Host "  Branch: // -> $result" -ForegroundColor Green
        return $result
    }
    elseif ($Url.StartsWith("/")) {
        $result = "$BaseUrl$Url"
        Write-Host "  Branch: / -> $result" -ForegroundColor Green
        return $result
    }
    elseif ($Url.StartsWith("http://") -or $Url.StartsWith("https://")) {
        Write-Host "  Branch: http(s):// -> $Url" -ForegroundColor Green
        return $Url
    }
    elseif ($Url.StartsWith("./") -or -not $Url.Contains("://")) {
        # Relative URL
        $result = "$BaseUrl/$($Url.TrimStart('./'))"
        Write-Host "  Branch: relative -> $result" -ForegroundColor Green
        return $result
    }
    else {
        Write-Host "  Branch: else -> $Url" -ForegroundColor Green
        return $Url
    }
}

$normalizedUrl = Normalize-Url -Url $testUrl -BaseUrl $baseUrl
Write-Host "Final normalized URL: $normalizedUrl" -ForegroundColor Magenta

# Test the URL
try {
    $response = Invoke-WebRequest -Uri $normalizedUrl -Method Head
    Write-Host "URL test result: SUCCESS ($($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "URL test result: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
