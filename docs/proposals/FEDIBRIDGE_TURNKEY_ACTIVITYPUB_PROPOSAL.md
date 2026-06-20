# FediBridge: Turnkey ActivityPub for Static Sites

## Proposal: Packaging Your ActivityPub Implementation as an Open-Source Solution

**Created**: January 23, 2026  
**Status**: Proposal  
**Branch**: `proposal/fedibridge-turnkey-activitypub`

---

## Executive Summary

Your current ActivityPub implementation represents a **production-proven, battle-tested solution** for adding Fediverse capabilities to static websites. After deep analysis of your implementation and extensive research into existing solutions, this proposal outlines how to package it as **FediBridge** - a turnkey, beginner-friendly, AI-assistant-compatible ActivityPub solution for Azure Static Web Apps.

### Why This Matters

1. **Gap in the Market**: Most ActivityPub solutions are either complex server software (Mastodon, Pleroma) or bridges with external dependencies (Bridgy Fed). There's no turnkey "just deploy it" solution for static site owners.

2. **Your Unique Position**: You've solved the hard problems - HTTP signatures, Key Vault integration, queue-based delivery, follower management - with a proven architecture.

3. **IndieWeb + Fediverse Convergence**: Growing demand from both communities for self-hosted solutions that don't require server administration expertise.

---

## Vision: What FediBridge Should Be

### Core Principles

1. **"Deploy in 15 Minutes"** - Complete setup from zero to federated in under 15 minutes
2. **Zero Code Required** - Configuration-only customization for common use cases
3. **AI-Assistant Friendly** - Structured for easy troubleshooting and extension via AI tools
4. **Cost-Effective** - ~$0.02/month operational cost using Azure free tiers
5. **Self-Hosted Ownership** - Your keys, your data, your identity

### Target Users

| User Type | Technical Level | What They Need |
|-----------|-----------------|----------------|
| **IndieWeb Blogger** | Low-Medium | "I have a static site and want to appear in Mastodon" |
| **Developer** | Medium-High | "I want to understand and customize the implementation" |
| **AI-Assisted User** | Any | "I want to deploy this and have AI help me fix issues" |

---

## Proposed Architecture

### Component Separation

Transform your current monolithic implementation into a **modular, extractable package**:

```
fedibridge/
├── core/                          # Core ActivityPub logic (extractable)
│   ├── config/                    # Configuration management
│   │   ├── schema.json           # JSON Schema for validation
│   │   ├── defaults.json         # Sensible defaults
│   │   └── README.md             # Configuration documentation
│   │
│   ├── functions/                 # Azure Functions (Node.js)
│   │   ├── webfinger/            # Discovery endpoint
│   │   ├── actor/                # Actor profile endpoint
│   │   ├── inbox/                # Receive activities
│   │   ├── outbox/               # Serve activities
│   │   ├── followers/            # Followers collection
│   │   ├── following/            # Following collection
│   │   └── utils/                # Shared utilities
│   │       ├── signatures.js     # HTTP Signatures
│   │       ├── keyvault.js       # Azure Key Vault
│   │       └── tableStorage.js   # Azure Table Storage
│   │
│   ├── templates/                 # Configuration templates
│   │   ├── actor.template.json   # Actor profile template
│   │   ├── webfinger.template.json
│   │   └── staticwebapp.template.json
│   │
│   └── scripts/                   # Setup and utility scripts
│       ├── setup.ps1             # Windows setup
│       ├── setup.sh              # Linux/Mac setup
│       ├── generate-keys.js      # Key pair generation
│       └── validate-config.js    # Configuration validator
│
├── integrations/                  # Optional integrations
│   ├── hugo/                     # Hugo static site generator
│   ├── eleventy/                 # Eleventy integration
│   ├── astro/                    # Astro integration
│   └── generic/                  # RSS-to-ActivityPub converter
│
├── docs/                          # Comprehensive documentation
│   ├── getting-started/          # Step-by-step guides
│   ├── troubleshooting/          # Common issues + solutions
│   ├── ai-prompts/               # Pre-written AI assistant prompts
│   └── architecture/             # Technical deep-dives
│
└── examples/                      # Complete working examples
    ├── minimal/                   # Bare minimum setup
    ├── blog/                      # Blog with posts
    └── portfolio/                 # Portfolio site
```

---

## Configuration-First Design

### The `fedibridge.config.json` File

Everything a user needs to customize goes in one file:

```json
{
  "$schema": "https://fedibridge.dev/schema/v1.json",
  
  "site": {
    "domain": "example.com",
    "title": "My Blog",
    "description": "A personal blog about technology"
  },
  
  "actor": {
    "username": "jane",
    "displayName": "Jane Developer",
    "summary": "Software engineer and open web enthusiast",
    "avatar": "/avatar.png",
    "discoverable": true
  },
  
  "content": {
    "source": "rss",
    "feedUrl": "/feed.xml",
    "contentTypes": ["posts", "notes"]
  },
  
  "azure": {
    "resourceGroup": "fedibridge-rg",
    "location": "eastus",
    "keyVaultName": "fedibridge-kv"
  },
  
  "features": {
    "autoAcceptFollows": true,
    "signatureVerification": true,
    "deliveryRetries": 5
  }
}
```

### Configuration Validation

Built-in validation with helpful error messages:

```bash
$ npx fedibridge validate

✅ Configuration valid!

📋 Summary:
   Actor: @jane@example.com
   Domain: example.com
   Content Source: RSS feed at /feed.xml
   
⚠️  Recommendations:
   - Consider adding a custom avatar (currently using default)
   - Enable signature verification for production (currently disabled)
```

---

## Beginner-Friendly Setup Flow

### Option 1: Interactive CLI Wizard

```bash
$ npx create-fedibridge@latest

🌐 Welcome to FediBridge Setup!

? What is your domain name? example.com
? What username do you want? (@___@example.com) jane
? What is your display name? Jane Developer
? Brief bio for your profile: Software engineer and open web enthusiast

? Do you have an existing static site? (Y/n) y
? How do you generate your site? (Hugo/Eleventy/Astro/Other) Hugo
? Where is your RSS feed? /index.xml

? Which Azure region? (Use arrow keys)
  ❯ East US (closest to you)
    West Europe
    Southeast Asia
    More options...

🔧 Setting up Azure resources...
   ✅ Resource group created
   ✅ Key Vault created
   ✅ Storage account created
   ✅ Key pair generated and stored
   
🚀 Ready to deploy!

   Next steps:
   1. Copy these files to your repository
   2. Add AZURE_STATIC_WEB_APPS_API_TOKEN to your secrets
   3. Push to main branch
   
   Your Fediverse handle will be: @jane@example.com
```

### Option 2: GitHub Template Repository

1. **Click "Use this template"** on GitHub
2. **Fill out a form** with basic configuration
3. **Automated setup** via GitHub Actions
4. **Test with provided validation script**

### Option 3: One-Click Azure Deploy

```markdown
[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/...)
```

Single button that:
- Creates all Azure resources
- Generates cryptographic keys
- Configures Static Web App
- Provides next steps

---

## AI-Assistant Friendly Design

### Structured Error Messages

Every error includes context that AI assistants can parse:

```json
{
  "error": "HTTP_SIGNATURE_VERIFICATION_FAILED",
  "code": "FEDI-401",
  "message": "Failed to verify HTTP signature from remote server",
  "context": {
    "remoteActor": "https://mastodon.social/users/example",
    "signatureAlgorithm": "rsa-sha256",
    "missingHeaders": ["digest"],
    "timestamp": "2026-01-23T21:30:00Z"
  },
  "troubleshooting": {
    "commonCauses": [
      "Remote server clock skew > 5 minutes",
      "Missing Digest header in request",
      "Public key mismatch"
    ],
    "suggestedActions": [
      "Check if remote actor's public key is accessible",
      "Verify your server time is synchronized",
      "Review signature verification logs"
    ],
    "documentationUrl": "https://fedibridge.dev/docs/troubleshooting/signature-errors"
  }
}
```

### Pre-Written AI Prompts

Include a library of prompts users can copy-paste:

```markdown
## docs/ai-prompts/troubleshooting.md

### Diagnose Federation Issue

Copy this prompt to your AI assistant:

---
I'm using FediBridge for ActivityPub on my static site. I'm experiencing 
[describe issue]. Here's my configuration:

```json
[paste your fedibridge.config.json]
```

And here's the error from my Azure Functions logs:

```
[paste error logs]
```

Please help me diagnose and fix this issue. FediBridge documentation 
is at https://fedibridge.dev/docs
---
```

### Copilot Instructions File

Include a `.github/copilot-instructions-fedibridge.md`:

```markdown
# FediBridge Copilot Instructions

## Project Context
This is a FediBridge deployment - a turnkey ActivityPub solution for static sites.

## Key Files
- `fedibridge.config.json` - All user configuration
- `api/` - Azure Functions for ActivityPub endpoints
- `api/utils/` - Shared utilities (signatures, storage, keyvault)

## Common Tasks

### Adding a new content type
1. Update `fedibridge.config.json` content.contentTypes array
2. Ensure your RSS feed includes the new type
3. Rebuild and deploy

### Debugging signature issues
1. Check `api/utils/signatures.js` for verification logic
2. Enable verbose logging in Azure Functions
3. Compare with Mastodon's signature implementation

## Architecture
- Static files served from Azure Blob Storage via CDN
- Azure Functions handle dynamic ActivityPub endpoints
- Azure Table Storage stores follower state
- Azure Key Vault manages cryptographic keys
- Queue-based async delivery with retry logic
```

---

## RSS-to-ActivityPub Conversion

### Universal Content Ingestion

Since most static sites have RSS feeds, make this the primary integration point:

```javascript
// integrations/generic/rss-to-activitypub.js

/**
 * Converts RSS feed items to ActivityPub Create activities
 * Supports: RSS 2.0, Atom, JSON Feed
 */
async function convertFeed(feedUrl, config) {
  const feed = await parseFeed(feedUrl);
  
  return feed.items.map(item => ({
    "@context": "https://www.w3.org/ns/activitystreams",
    "type": "Create",
    "actor": config.actorUri,
    "published": item.pubDate,
    "object": {
      "type": detectType(item), // Article, Note, etc.
      "id": generateNoteId(item),
      "content": item.content || item.description,
      "name": item.title,
      "url": item.link,
      "attributedTo": config.actorUri,
      "to": ["https://www.w3.org/ns/activitystreams#Public"],
      "cc": [config.followersUri]
    }
  }));
}
```

### Static Site Generator Plugins

Provide native plugins for popular generators:

```javascript
// integrations/hugo/index.js
// Hugo post-build hook that generates ActivityPub data

module.exports = {
  onPostBuild: async ({ config, outputDir }) => {
    const feed = await parseFeed(`${outputDir}/index.xml`);
    await generateOutbox(feed, config);
    await generateNotes(feed, config);
  }
};
```

---

## Azure Resource Setup

### Infrastructure as Code (Bicep)

Provide a one-click deployable Bicep template:

```bicep
// infra/main.bicep

@description('The domain name for your site')
param domainName string

@description('The username for your Fediverse account')
param username string

@description('Azure region for resources')
param location string = resourceGroup().location

// Key Vault for cryptographic keys
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'fedibridge-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

// Storage account for Table Storage
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'fedibridge${uniqueString(resourceGroup().id)}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
}

// Generate RSA key pair
resource rsaKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'activitypub-signing-key'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: ['sign', 'verify']
  }
}
```

### GitHub Actions Workflow

Automated deployment with all necessary steps:

```yaml
# .github/workflows/deploy-fedibridge.yml

name: Deploy FediBridge

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Validate Configuration
        run: npx fedibridge validate
      
      - name: Generate ActivityPub Data
        run: npx fedibridge build
      
      - name: Deploy to Azure
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_SWA_TOKEN }}
          app_location: "./_public"
          api_location: "./api"
      
      - name: Trigger Delivery of New Posts
        env:
          STORAGE_CONNECTION: ${{ secrets.STORAGE_CONNECTION }}
        run: npx fedibridge deliver --since=24h
```

---

## Repository Structure Options

### Option A: Standalone Package (Recommended)

Create a new repository: `github.com/lqdev/fedibridge`

**Pros:**
- Clean separation from your personal site
- Easier for others to fork and contribute
- Clearer branding and documentation
- Can publish to npm as `create-fedibridge`

**Cons:**
- Need to maintain separately
- May diverge from your production implementation

### Option B: Extract as Subdirectory

Keep in your current repo under `/fedibridge/`

**Pros:**
- Single source of truth
- Your site serves as living documentation
- Changes automatically reflected

**Cons:**
- Harder for others to extract just the package
- Your personal site config mixed with generic solution

### Option C: Git Submodule

Core in separate repo, consumed as submodule

**Pros:**
- Best of both worlds
- Clear versioning
- Your site uses specific versions

**Cons:**
- Submodule complexity
- More complex contribution workflow

### Recommendation: Option A with Sync Workflow

1. Create `github.com/lqdev/fedibridge` as the main package
2. Your site `luisquintanilla.me` uses it as a dependency
3. Automated workflow syncs proven patterns from your site to the package

---

## Documentation Strategy

### Beginner Guide Structure

```
docs/
├── getting-started/
│   ├── what-is-activitypub.md      # 5-minute explainer
│   ├── prerequisites.md             # What you need before starting
│   ├── quick-start.md               # 15-minute setup guide
│   └── first-post.md                # Your first federated post
│
├── configuration/
│   ├── basic-settings.md            # Essential config
│   ├── advanced-settings.md         # Power user options
│   └── environment-variables.md     # All env vars explained
│
├── integrations/
│   ├── hugo.md
│   ├── eleventy.md
│   ├── astro.md
│   ├── nextjs.md
│   └── custom-rss.md
│
├── troubleshooting/
│   ├── common-issues.md             # FAQ-style
│   ├── signature-errors.md          # HTTP Signature problems
│   ├── delivery-failures.md         # Outbox issues
│   ├── discovery-problems.md        # WebFinger issues
│   └── ask-ai.md                    # How to get AI help
│
└── architecture/
    ├── overview.md                   # High-level design
    ├── security.md                   # Key management, signatures
    ├── data-flow.md                  # Request lifecycle
    └── cost-analysis.md              # Azure pricing breakdown
```

### Interactive Tutorials

Consider adding:
- **Embedded Azure Cloud Shell** links for one-click resource creation
- **Mermaid diagrams** for architecture visualization
- **Copy buttons** on all code blocks
- **"Try it" sandboxes** using Azure's free tier

---

## Migration Path for Existing Users

### From Mastodon

```bash
$ npx fedibridge migrate mastodon

📦 Mastodon Migration Assistant

? Enter your Mastodon archive path: ~/Downloads/mastodon-archive.tar.gz

Importing:
  ✅ Profile information
  ✅ Avatar and header images
  ✅ Following list (142 accounts)
  ✅ Followers (redirects will be set up)
  ⚠️  Posts cannot be migrated (they stay on Mastodon)
  ⚠️  DMs and bookmarks are not transferable

🔄 Setting up account migration...
   Your Mastodon profile will point to your new FediBridge account.
   Followers will be notified to re-follow you.

Ready to migrate? (Y/n)
```

### From Bridgy Fed

```bash
$ npx fedibridge migrate bridgy-fed

📦 Bridgy Fed Migration

This will:
1. Import your existing followers from Bridgy Fed
2. Set up redirects from your Bridgy Fed profile
3. Notify your followers of the migration

Your new handle: @jane@example.com
Your old handle: @example.com@fed.brid.gy
```

---

## Testing & Validation

### Automated Test Suite

```javascript
// tests/federation.test.js

describe('Federation Tests', () => {
  it('should return valid WebFinger response', async () => {
    const response = await fetch('/.well-known/webfinger?resource=acct:test@example.com');
    expect(response.headers.get('content-type')).toBe('application/jrd+json');
    const data = await response.json();
    expect(data.subject).toBe('acct:test@example.com');
  });

  it('should verify HTTP signatures', async () => {
    const signedRequest = await signRequest(testActivity, testPrivateKey);
    const response = await fetch('/api/activitypub/inbox', signedRequest);
    expect(response.status).toBe(202);
  });

  it('should accept Follow activities', async () => {
    const follow = createFollowActivity('https://test.server/user');
    const response = await sendToInbox(follow);
    expect(response.status).toBe(202);
    
    // Check for Accept activity
    const accept = await waitForAccept();
    expect(accept.type).toBe('Accept');
  });
});
```

### FediDB Integration

Provide easy testing against [FediDB](https://fedidb.org):

```bash
$ npx fedibridge test --live

🧪 Running live federation tests...

Testing against: fedidb.org
  ✅ WebFinger discovery
  ✅ Actor profile fetch
  ✅ Follow/Accept workflow
  ✅ HTTP signature verification
  ✅ Outbox collection

All tests passed! Your FediBridge is ready for federation.
```

---

## Cost Transparency

### Azure Pricing Breakdown

| Resource | Free Tier | Estimated Monthly Cost |
|----------|-----------|----------------------|
| Static Web Apps | 100GB bandwidth | $0.00 |
| Functions | 1M executions | $0.00 |
| Table Storage | 5GB | $0.00 |
| Key Vault | 10,000 operations | $0.00 |
| **Total** | | **~$0.02/month** |

> Note: Costs only occur after exceeding free tiers. A typical personal blog with <1000 followers will stay well within free limits.

---

## Roadmap

### Phase 1: Core Extraction (2-3 weeks)
- [ ] Create new repository with clean structure
- [ ] Extract and generalize Azure Functions
- [ ] Create configuration schema and validation
- [ ] Write getting-started documentation
- [ ] Build CLI setup wizard

### Phase 2: Static Site Integrations (2-3 weeks)
- [ ] Hugo plugin
- [ ] Eleventy plugin
- [ ] Astro integration
- [ ] Generic RSS-to-ActivityPub converter

### Phase 3: Developer Experience (2 weeks)
- [ ] One-click Azure deploy button
- [ ] GitHub template repository
- [ ] Comprehensive test suite
- [ ] AI-assistant prompt library

### Phase 4: Community & Polish (Ongoing)
- [ ] FediDB testing integration
- [ ] Migration tools from other platforms
- [ ] Community showcase
- [ ] Video tutorials

---

## Open Questions

1. **Naming**: Is "FediBridge" the right name? Alternatives:
   - StaticPub
   - FediStatic
   - IndieAP (IndieWeb ActivityPub)
   - SimplePub

2. **Scope**: Should this include:
   - Reply handling (receive and display replies)?
   - Likes/Boosts display?
   - A simple admin dashboard?

3. **Platform Expansion**: Should we support:
   - Cloudflare Workers + D1 + KV?
   - Netlify Functions + Fauna?
   - Vercel Functions + Upstash?

4. **Monetization**: If this becomes popular:
   - Hosted version for non-technical users?
   - Paid support tier?
   - Purely community-driven?

---

## Next Steps

1. **Review this proposal** - Does this vision align with your goals?
2. **Prioritize features** - What's essential for v1.0?
3. **Create the repository** - Start with `github.com/lqdev/fedibridge`
4. **Extract core logic** - Begin modularizing your implementation
5. **Write the quick-start guide** - Documentation-driven development

---

## Appendix: Research Sources

### Existing Solutions Analyzed

| Solution | Approach | Beginner-Friendly | Self-Hosted | AI-Friendly |
|----------|----------|-------------------|-------------|-------------|
| **Bridgy Fed** | Bridge service | ✅ | ❌ | ⚠️ |
| **Fedify** | TypeScript framework | ⚠️ | ✅ | ✅ |
| **ActivityPub Express** | Node.js library | ⚠️ | ✅ | ⚠️ |
| **Takahē** | Full server | ❌ | ✅ | ⚠️ |
| **WordPress Plugin** | Plugin | ✅ | ✅ | ⚠️ |
| **Your Implementation** | Azure Functions | ⚠️ | ✅ | ⚠️ |
| **FediBridge (proposed)** | Turnkey package | ✅ | ✅ | ✅ |

### Key Insights from Research

1. **Configuration complexity is the #1 barrier** - Most solutions require understanding HTTP signatures, key management, and protocol details upfront.

2. **AI-assistant compatibility is unexplored** - No existing solution explicitly designs for AI troubleshooting.

3. **Static site + serverless is proven** - Multiple implementations (maho.dev, Paul Kinlan) validate this approach.

4. **Cost-effectiveness is achievable** - Azure free tiers make this practically free for personal use.

5. **HTTP Signatures remain the hardest part** - Every implementation struggles with signature verification edge cases.

---

*This proposal represents the culmination of analyzing your production implementation, researching existing solutions, and envisioning what a truly beginner-friendly, AI-assisted ActivityPub solution could look like.*
