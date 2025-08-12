# Check Site Sizes Script (PowerShell)
# Shows total site size, main site size (excluding text-only), and text-only site size

Write-Host "📊 Website Size Analysis" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Function to get directory size in MB
function Get-DirectorySize {
    param([string]$Path)
    if (Test-Path $Path) {
        $size = (Get-ChildItem $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
        return [math]::Round($size / 1MB, 2)
    }
    return 0
}

# Function to format size
function Format-Size {
    param([double]$SizeInMB)
    if ($SizeInMB -gt 1024) {
        return "$([math]::Round($SizeInMB / 1024, 2)) GB"
    } else {
        return "$SizeInMB MB"
    }
}

$publicPath = ".\_public"

if (Test-Path $publicPath) {
    # Total site size
    $totalSize = Get-DirectorySize $publicPath
    $formattedTotal = Format-Size $totalSize
    
    Write-Host ""
    Write-Host "🚀 TOTAL WEBSITE SIZE: $formattedTotal" -ForegroundColor Green -BackgroundColor DarkGreen
    Write-Host "=================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "🌐 Full Site: $formattedTotal" -ForegroundColor Green
    
    # Text-only site size
    $textPath = "$publicPath\text"
    if (Test-Path $textPath) {
        $textSize = Get-DirectorySize $textPath
        Write-Host "📝 Text-Only Site Size: $(Format-Size $textSize)" -ForegroundColor Yellow
        
        $mainSize = $totalSize - $textSize
        Write-Host "🎨 Main Site Size: $(Format-Size $mainSize)" -ForegroundColor Blue
    } else {
        Write-Host "📝 Text-Only Site: Not found" -ForegroundColor Red
        Write-Host "🎨 Main Site Size: $(Format-Size $totalSize)" -ForegroundColor Blue
    }
    
    Write-Host ""
    Write-Host "📁 Directory Breakdown:" -ForegroundColor Cyan
    Write-Host "----------------------" -ForegroundColor Cyan
    
    # Show breakdown of major directories
    Get-ChildItem $publicPath -Directory | ForEach-Object {
        $dirSize = Get-DirectorySize $_.FullName
        $name = $_.Name.PadRight(15)
        Write-Host "$name $(Format-Size $dirSize)"
    }
    
    Write-Host ""
    Write-Host "📈 Content Statistics:" -ForegroundColor Cyan
    Write-Host "---------------------" -ForegroundColor Cyan
    
    # Count files in main areas
    $postsPath = "$publicPath\posts"
    if (Test-Path $postsPath) {
        $postCount = (Get-ChildItem $postsPath -Recurse -Filter "index.html").Count
        Write-Host "📄 Posts: $postCount"
    }
    
    $notesPath = "$publicPath\notes"
    if (Test-Path $notesPath) {
        $noteCount = (Get-ChildItem $notesPath -Recurse -Filter "index.html").Count
        Write-Host "📝 Notes: $noteCount"
    }
    
    $responsesPath = "$publicPath\responses"
    if (Test-Path $responsesPath) {
        $responseCount = (Get-ChildItem $responsesPath -Recurse -Filter "index.html").Count
        Write-Host "💬 Responses: $responseCount"
    }
    
    if (Test-Path $textPath) {
        $textPageCount = (Get-ChildItem $textPath -Recurse -Filter "*.html").Count
        Write-Host "🔤 Text-Only Pages: $textPageCount"
    }
    
    Write-Host ""
} else {
    Write-Host "❌ _public directory not found" -ForegroundColor Red
}
