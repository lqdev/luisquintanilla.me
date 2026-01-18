const { DefaultAzureCredential } = require('@azure/identity');
const { CryptographyClient } = require('@azure/keyvault-keys');
const crypto = require('crypto');

/**
 * KeyVault utility for ActivityPub signing and verification
 * Follows "simpler is better" principle with fallback to env variables
 */
class KeyVaultSigner {
  constructor() {
    this.keyId = process.env.KEY_VAULT_KEY_ID;
    this.useKeyVault = !!this.keyId;
    
    if (this.useKeyVault) {
      // Production: Use Azure Key Vault
      this.credential = new DefaultAzureCredential();
      this.client = new CryptographyClient(this.keyId, this.credential);
    } else {
      // Development: Use environment variable key
      this.privateKeyPem = process.env.ACTIVITYPUB_PRIVATE_KEY;
      if (!this.privateKeyPem) {
        console.warn('WARNING: No signing key configured (neither KEY_VAULT_KEY_ID nor ACTIVITYPUB_PRIVATE_KEY)');
      }
    }
  }

  /**
   * Sign data with RSA-SHA256
   * @param {Buffer} data - Data to sign (typically a hash)
   * @returns {Promise<Buffer>} Signature
   */
  async sign(data) {
    if (this.useKeyVault) {
      // Use Azure Key Vault for signing
      const result = await this.client.sign('RS256', data);
      return Buffer.from(result.result);
    } else if (this.privateKeyPem) {
      // Use Node.js crypto for development
      const sign = crypto.createSign('RSA-SHA256');
      sign.update(data);
      sign.end();
      return sign.sign(this.privateKeyPem);
    } else {
      throw new Error('No signing key configured');
    }
  }

  /**
   * Verify signature with RSA-SHA256
   * @param {Buffer} data - Original data (typically a hash)
   * @param {Buffer} signature - Signature to verify
   * @param {string} publicKeyPem - PEM-encoded public key
   * @returns {Promise<boolean>} True if valid
   */
  async verify(data, signature, publicKeyPem) {
    if (this.useKeyVault) {
      // Note: For incoming signatures, we verify against the sender's public key
      // not our Key Vault key. Use Node.js crypto for this.
      const verify = crypto.createVerify('RSA-SHA256');
      verify.update(data);
      verify.end();
      return verify.verify(publicKeyPem, signature);
    } else {
      // Use Node.js crypto for verification
      const verify = crypto.createVerify('RSA-SHA256');
      verify.update(data);
      verify.end();
      return verify.verify(publicKeyPem, signature);
    }
  }

  /**
   * Check if signing is configured
   * @returns {boolean}
   */
  isConfigured() {
    return this.useKeyVault || !!this.privateKeyPem;
  }
}

module.exports = { KeyVaultSigner };
