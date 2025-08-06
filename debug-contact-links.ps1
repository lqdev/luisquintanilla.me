# Debug script to check contact page link parsing
$contactHtml = Get-Content "c:\Dev\website\_public\contact\index.html" -Raw

# Extract href and src links using the same regex as the main script
$hrefPattern = 'href\s*=\s*["' + "'" + ']([^"' + "'" + ']+)["' + "'" + ']'
$srcPattern = 'src\s*=\s*["' + "'" + ']([^"' + "'" + ']+)["' + "'" + ']'

Write-Host "=== HREF Links in Contact Page ===" -ForegroundColor Green
$hrefMatches = [regex]::Matches($contactHtml, $hrefPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
foreach ($match in $hrefMatches) {
    $url = $match.Groups[1].Value
    if ($url -match "qr-") {
        Write-Host "Found QR link: $url" -ForegroundColor Yellow
    }
}

Write-Host "`n=== SRC Links in Contact Page ===" -ForegroundColor Green  
$srcMatches = [regex]::Matches($contactHtml, $srcPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
foreach ($match in $srcMatches) {
    $url = $match.Groups[1].Value
    if ($url -match "qr-") {
        Write-Host "Found QR src: $url" -ForegroundColor Yellow
    }
}

Write-Host "`n=== All QR References ===" -ForegroundColor Green
$allQrMatches = [regex]::Matches($contactHtml, "qr-[^""'>\s]+", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
foreach ($match in $allQrMatches) {
    Write-Host "QR reference: $($match.Value)" -ForegroundColor Cyan
}
