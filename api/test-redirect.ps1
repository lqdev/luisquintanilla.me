#!/usr/bin/env pwsh

# Test script for Azure Functions file redirect functionality
# This script validates that the wildcard redirect API works correctly

Write-Host "🧪 Testing Azure Functions File Redirect API" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure Functions Core Tools is installed
try {
    $funcVersion = func --version 2>$null
    Write-Host "✅ Azure Functions Core Tools version: $funcVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure Functions Core Tools not found. Please install:" -ForegroundColor Red
    Write-Host "   npm install -g azure-functions-core-tools@4 --unsafe-perm true" -ForegroundColor Yellow
    exit 1
}

# Check if in correct directory
if (-not (Test-Path ".\files.js")) {
    Write-Host "❌ Please run this script from the /api directory" -ForegroundColor Red
    exit 1
}

# Install dependencies if needed
if (-not (Test-Path ".\node_modules")) {
    Write-Host "📦 Installing Node.js dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
        exit 1
    }
}

Write-Host "🚀 Starting Azure Functions runtime..." -ForegroundColor Yellow
Write-Host "   (Press Ctrl+C to stop the function host when testing is complete)" -ForegroundColor Gray

# Start Functions runtime in background
$functionHost = Start-Process -FilePath "func" -ArgumentList "start" -PassThru -WindowStyle Hidden

# Wait for function host to start
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "🧪 Running redirect tests..." -ForegroundColor Cyan

# Test cases
$testCases = @(
    @{
        Name = "Simple file redirect"
        Url = "http://localhost:7071/api/files/document.pdf"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/document.pdf"
    },
    @{
        Name = "Nested path redirect"
        Url = "http://localhost:7071/api/files/reports/2024/summary.pdf"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/reports/2024/summary.pdf"
    },
    @{
        Name = "File with query parameters"
        Url = "http://localhost:7071/api/files/document.pdf?download=true&version=latest"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/document.pdf?download=true&version=latest"
    },
    @{
        Name = "Deep nested structure"
        Url = "http://localhost:7071/api/files/projects/website/documentation/api.md"
        ExpectedLocation = "https://luisquintanillame.blob.core.windows.net/files/projects/website/documentation/api.md"
    }
)

$passedTests = 0
$totalTests = $testCases.Length

foreach ($test in $testCases) {
    Write-Host ""
    Write-Host "  Testing: $($test.Name)" -ForegroundColor White
    Write-Host "  URL: $($test.Url)" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $test.Url -Method Head -MaximumRedirection 0 -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -eq 301) {
            $location = $response.Headers.Location
            if ($location -eq $test.ExpectedLocation) {
                Write-Host "  ✅ PASS - Correct redirect to: $location" -ForegroundColor Green
                $passedTests++
            } else {
                Write-Host "  ❌ FAIL - Expected: $($test.ExpectedLocation)" -ForegroundColor Red
                Write-Host "           Actual: $location" -ForegroundColor Red
            }
            
            # Check for expected headers
            $cacheControl = $response.Headers.'Cache-Control'
            $robotsTag = $response.Headers.'X-Robots-Tag'
            
            if ($cacheControl) {
                Write-Host "  📄 Cache-Control: $cacheControl" -ForegroundColor Gray
            }
            if ($robotsTag) {
                Write-Host "  🤖 X-Robots-Tag: $robotsTag" -ForegroundColor Gray
            }
            
        } else {
            Write-Host "  ❌ FAIL - Expected status 301, got: $($response.StatusCode)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "  ❌ FAIL - Request failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🧪 Testing invalid requests..." -ForegroundColor Cyan

# Test invalid paths
$invalidTests = @(
    @{
        Name = "Directory traversal attempt"
        Url = "http://localhost:7071/api/files/../../../etc/passwd"
    },
    @{
        Name = "Empty file path"
        Url = "http://localhost:7071/api/files/"
    },
    @{
        Name = "Absolute path attempt"
        Url = "http://localhost:7071/api/files//etc/passwd"
    }
)

foreach ($test in $invalidTests) {
    Write-Host ""
    Write-Host "  Testing: $($test.Name)" -ForegroundColor White
    Write-Host "  URL: $($test.Url)" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $test.Url -Method Head -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -eq 400) {
            Write-Host "  ✅ PASS - Correctly rejected with status 400" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  ❌ FAIL - Expected status 400, got: $($response.StatusCode)" -ForegroundColor Red
        }
        
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 400) {
            Write-Host "  ✅ PASS - Correctly rejected with status 400" -ForegroundColor Green
            $passedTests++
        } else {
            Write-Host "  ❌ FAIL - Expected status 400, got: $statusCode" -ForegroundColor Red
        }
    }
}

$totalTests += $invalidTests.Length

# Cleanup - stop the function host
Write-Host ""
Write-Host "🛑 Stopping Azure Functions runtime..." -ForegroundColor Yellow
Stop-Process -Id $functionHost.Id -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "📊 Test Results" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
Write-Host "Passed: $passedTests / $totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Yellow" })

if ($passedTests -eq $totalTests) {
    Write-Host ""
    Write-Host "🎉 All tests passed! The file redirect API is working correctly." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "1. Deploy the Azure Static Web App with the API" -ForegroundColor Gray
    Write-Host "2. Test the production deployment" -ForegroundColor Gray
    Write-Host "3. Update any existing links to use the new redirect system" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "⚠️  Some tests failed. Please review the implementation." -ForegroundColor Yellow
    exit 1
}
