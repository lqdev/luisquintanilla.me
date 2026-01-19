#!/bin/bash
# Convert JWK from Azure Key Vault to PEM format for ActivityPub
# Requirements: az CLI, jq, node (node is required for Azure Functions anyway)

VAULT_NAME="${1:-lqdev-activitypub-kv}"
KEY_NAME="${2:-activitypub-signing-key}"

echo "Fetching key from Key Vault..."
JWK=$(az keyvault key show --vault-name "$VAULT_NAME" --name "$KEY_NAME" --query "key" -o json)

# Extract modulus (n) and exponent (e)  
N=$(echo "$JWK" | jq -r '.n')
E=$(echo "$JWK" | jq -r '.e')

echo "Converting JWK to PEM using Node.js..."

# Use Node.js built-in crypto (no additional dependencies needed)
node << EOF
const crypto = require('crypto');

// Base64url to base64 conversion
function base64urlToBase64(base64url) {
  let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const padding = (4 - (base64.length % 4)) % 4;
  base64 += '='.repeat(padding);
  return base64;
}

const n = base64urlToBase64('$N');
const e = base64urlToBase64('$E');

// Create RSA public key from JWK components
const publicKey = crypto.createPublicKey({
  key: {
    kty: 'RSA',
    n: n,
    e: e
  },
  format: 'jwk'
});

// Export as PEM
const pem = publicKey.export({
  type: 'spki',
  format: 'pem'
});

console.log('\nPublic Key in PEM format:');
console.log(pem);

// Save to file
require('fs').writeFileSync('public-key.pem', pem);
console.log('Saved to: public-key.pem');
EOF

echo ""
echo "✅ Conversion complete!"

