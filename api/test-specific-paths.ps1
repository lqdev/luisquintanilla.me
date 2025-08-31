#!/usr/bin/env pwsh

# Test script for specific file paths mentioned by the user
Write-Host "🧪 Testing Specific File Path Patterns" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Test cases based on user's specific examples
$testCases = @(
    @{
        Name = "Image file in images subfolder"
        Url = "http://localhost:7071/api/files/images/my-image.jpg"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/images/my-image.jpg"
    },
    @{
        Name = "Video file in videos subfolder"
        Url = "http://localhost:7071/api/files/videos/my-video.mp4"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/videos/my-video.mp4"
    },
    @{
        Name = "Document in documents subfolder"
        Url = "http://localhost:7071/api/files/documents/report.pdf"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/documents/report.pdf"
    },
    @{
        Name = "Deep nested video path"
        Url = "http://localhost:7071/api/files/videos/2024/presentations/demo.mp4"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/videos/2024/presentations/demo.mp4"
    },
    @{
        Name = "Image with spaces and special chars"
        Url = "http://localhost:7071/api/files/images/my%20awesome%20image-2024.jpg"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/images/my%20awesome%20image-2024.jpg"
    }
)

Write-Host "Note: This test assumes Azure Functions is running locally." -ForegroundColor Yellow
Write-Host "Run 'func start' in the /api directory first if it's not already running." -ForegroundColor Yellow
Write-Host ""

foreach ($test in $testCases) {
    Write-Host "Testing: $($test.Name)" -ForegroundColor White
    Write-Host "  URL: $($test.Url)" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $test.Url -Method Head -MaximumRedirection 0 -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -eq 301) {
            $location = $response.Headers.Location
            if ($location -eq $test.ExpectedLocation) {
                Write-Host "  ✅ PASS - Correct redirect" -ForegroundColor Green
                Write-Host "  📍 Location: $location" -ForegroundColor Gray
            } else {
                Write-Host "  ❌ FAIL - Wrong redirect" -ForegroundColor Red
                Write-Host "     Expected: $($test.ExpectedLocation)" -ForegroundColor Red
                Write-Host "     Actual: $location" -ForegroundColor Red
            }
        } else {
            Write-Host "  ❌ FAIL - Expected status 301, got: $($response.StatusCode)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "  ⚠️  Could not connect - is the function running?" -ForegroundColor Yellow
        Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    
    Write-Host ""
}

Write-Host "💡 Quick Start Guide:" -ForegroundColor Cyan
Write-Host "1. cd api" -ForegroundColor Gray
Write-Host "2. npm install" -ForegroundColor Gray
Write-Host "3. func start" -ForegroundColor Gray
Write-Host "4. Run this test script again" -ForegroundColor Gray
