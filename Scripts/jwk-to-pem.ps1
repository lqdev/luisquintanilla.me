# Convert JWK from Azure Key Vault to PEM format for ActivityPub

param(
    [string]$VaultName = "lqdev-activitypub-kv",
    [string]$KeyName = "activitypub-signing-key"
)

# Get the JWK from Key Vault
Write-Host "Fetching key from Key Vault..."
$jwkJson = az keyvault key show --vault-name $VaultName --name $KeyName --query "key" -o json
$jwk = $jwkJson | ConvertFrom-Json

# Convert base64url to base64
function ConvertFrom-Base64Url {
    param([string]$base64url)
    $base64 = $base64url.Replace('-', '+').Replace('_', '/')
    # Add padding
    $padding = (4 - ($base64.Length % 4)) % 4
    $base64 += '=' * $padding
    return $base64
}

# Convert the modulus (n) and exponent (e) from base64url to bytes
$nBase64 = ConvertFrom-Base64Url $jwk.n
$eBase64 = ConvertFrom-Base64Url $jwk.e

$nBytes = [Convert]::FromBase64String($nBase64)
$eBytes = [Convert]::FromBase64String($eBase64)

Write-Host "Creating RSA public key..."

# Create RSA parameters
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsaParams = New-Object System.Security.Cryptography.RSAParameters
$rsaParams.Modulus = $nBytes
$rsaParams.Exponent = $eBytes
$rsa.ImportParameters($rsaParams)

# Export as PEM
$publicKeyBytes = $rsa.ExportSubjectPublicKeyInfo()
$publicKeyBase64 = [Convert]::ToBase64String($publicKeyBytes)

# Format as PEM (64 chars per line)
$pem = "-----BEGIN PUBLIC KEY-----`n"
for ($i = 0; $i -lt $publicKeyBase64.Length; $i += 64) {
    $length = [Math]::Min(64, $publicKeyBase64.Length - $i)
    $pem += $publicKeyBase64.Substring($i, $length) + "`n"
}
$pem += "-----END PUBLIC KEY-----"

Write-Host "`nPublic Key in PEM format:"
Write-Host ""
Write-Output $pem
Write-Host ""

# Also save to file
$outputFile = "public-key.pem"
$pem | Out-File -FilePath $outputFile -Encoding ASCII -NoNewline
Write-Host "Saved to: $outputFile"
