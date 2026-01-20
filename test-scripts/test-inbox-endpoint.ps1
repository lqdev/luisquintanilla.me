# Test script to manually send a Follow activity to the inbox endpoint
# This helps diagnose if the endpoint is receiving and processing Follow requests

$inboxUrl = "https://lqdev.me/api/activitypub/inbox"
$actorUrl = "https://lqdev.me/api/activitypub/actor"

# Simple Follow activity (without signature for testing)
$followActivity = @{
    "@context" = "https://www.w3.org/ns/activitystreams"
    "id" = "https://test.example.com/follows/$(New-Guid)"
    "type" = "Follow"
    "actor" = "https://test.example.com/users/testuser"
    "object" = $actorUrl
} | ConvertTo-Json -Depth 10

Write-Host "Sending Follow activity to inbox..." -ForegroundColor Cyan
Write-Host "Follow Activity:" -ForegroundColor Yellow
Write-Host $followActivity

try {
    $response = Invoke-WebRequest -Uri $inboxUrl `
        -Method POST `
        -ContentType "application/activity+json" `
        -Body $followActivity `
        -UseBasicParsing `
        -Verbose
    
    Write-Host "`nResponse Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Body:" -ForegroundColor Green
    Write-Host $response.Content
} catch {
    Write-Host "`nError occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body:" -ForegroundColor Red
        Write-Host $responseBody
    }
}
