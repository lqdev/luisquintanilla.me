# ActivityPub Scripts Documentation

This document describes the ActivityPub-related scripts in this directory and their role in the overall implementation.

---

## test-activitypub.sh

**Purpose**: Local development test suite for ActivityPub endpoints  
**Status**: Production-ready, actively used  
**Usage**: Run during local development to validate Azure Functions  
**Test Target**: `http://localhost:7071` (local Azure Functions runtime)

### What It Tests

- WebFinger discovery (both `@lqdev.me` and `@www.lqdev.me` formats)
- Actor endpoint validation
- Collections endpoints (outbox, followers, following)
- HTTP status codes and content-type headers
- JSON structure validation

### Running the Tests

```bash
# Start Azure Functions locally first
cd api
func start

# In another terminal, from repository root
./Scripts/test-activitypub.sh

# Expected output:
# ✓ WebFinger endpoint works
# ✓ Actor endpoint works
# ✓ Outbox endpoint works
# ✓ Followers endpoint works
# ✓ Following endpoint works
# All ActivityPub endpoints are working correctly!
```

### When to Run

- Before committing ActivityPub endpoint changes
- During local development and debugging
- When testing Azure Functions locally
- Before pushing to GitHub (pre-deployment validation)

### Test Coverage

| Endpoint | Tests |
|----------|-------|
| WebFinger | Response format, both domain variants, status code |
| Actor | JSON structure, required fields, public key format |
| Outbox | Collection type, ordered items, activity structure |
| Followers | Collection type, dynamic updates |
| Following | Collection type, empty state handling |

---

## test-activitypub-production.sh

**Purpose**: Production deployment validation and health monitoring  
**Status**: ✅ Production-ready (Created January 2026)  
**Usage**: Validate ActivityPub deployment after production release  
**Test Target**: `https://lqdev.me` (live production domain)

### What It Tests

- **Phase 1**: WebFinger discovery (primary and WWW subdomain)
- **Phase 2**: Actor profile endpoint
- **Phase 3**: Collection endpoints (outbox, followers, following)
- **Phase 4**: Inbox endpoint (GET request)
- **Phase 5**: Static data files (actor.json, webfinger.json, etc.)
- **Phase 6**: URL pattern consistency validation

### Running the Tests

```bash
# From repository root (requires internet connectivity)
./Scripts/test-activitypub-production.sh

# Expected output (full deployment):
# ==========================================
# ActivityPub Production Deployment Tests
# Testing Domain: https://lqdev.me
# ==========================================
# 
# Phase 1: WebFinger Discovery Tests
# ✓ PASS: WebFinger: Primary Handle
# ✓ PASS: WebFinger: WWW Subdomain
# 
# Phase 2: Actor Profile Tests
# ✓ PASS: Actor Profile
# 
# ... (more tests)
# 
# ✅ All tests passed! ActivityPub deployment is fully functional.
```

### When to Run

- **After deployment to production** (REQUIRED)
- After Azure Functions updates or configuration changes
- As part of CI/CD validation (GitHub Actions workflow available)
- For periodic health monitoring (weekly recommended)
- When debugging federation issues reported by users

### Test Coverage

Comprehensive production validation across 6 phases:

| Phase | Tests | Criticality |
|-------|-------|-------------|
| **WebFinger Discovery** | Primary handle, WWW subdomain | 🔴 Critical |
| **Actor Profile** | Profile structure, publicKey, endpoints | 🔴 Critical |
| **Collection Endpoints** | Outbox, followers, following | 🔴 Critical |
| **Inbox Endpoint** | GET request handling | 🟡 Important |
| **Static Data Files** | All JSON files deployed correctly | 🟡 Important |
| **URL Pattern** | Consistency validation | 🟢 Nice-to-have |

### GitHub Actions Integration

Automated testing available via workflow:

```bash
# Manual trigger from GitHub Actions UI
# Go to: Actions → "Test ActivityPub Deployment" → Run workflow

# Or via GitHub CLI
gh workflow run test-activitypub-deployment.yml
```

The workflow includes:
- Automated endpoint testing
- Static file validation
- Documentation verification
- Automatic issue creation on failure (scheduled runs)
- Test result artifacts

### Output and Reporting

The script provides detailed output with color-coded results:
- ✅ **Green (PASS)**: Test passed successfully
- ⚠️  **Yellow (WARN)**: Test passed with warnings or non-critical issues
- ❌ **Red (FAIL)**: Test failed, requires attention

Failed tests include troubleshooting guidance pointing to:
- `/docs/activitypub/POST-DEPLOYMENT-TEST-RESULTS.md` (troubleshooting guide)
- `/docs/activitypub/deployment-guide.md` (deployment instructions)
- Application Insights logs (for Azure Function errors)

### Related Documentation

- **Test Results Template**: `/docs/activitypub/POST-DEPLOYMENT-TEST-RESULTS.md`
- **Troubleshooting Guide**: Same document, includes common issues and resolutions
- **Deployment Guide**: `/docs/activitypub/deployment-guide.md`
- **Implementation Status**: `/docs/activitypub/implementation-status.md`

---

## rss-to-activitypub.fsx

**Purpose**: Prototype for Phase 3 (Outbox Automation) implementation  
**Status**: ⚠️ **DEPRECATED** - No longer used in deployment workflow (removed January 2026)  
**Role**: Historical reference implementation, superseded by ActivityPubBuilder.fs

> **Note**: As of January 2026, ActivityPub outbox generation is handled by `ActivityPubBuilder.buildOutbox` in the main F# build process (Program.fs). This script served as a prototype during Phase 3 development but is no longer part of the deployment workflow.

### What It Does

1. **Parse RSS Feed**: Reads `_public/feed/feed.xml` (or custom path)
2. **Convert to ActivityPub**: Transforms RSS items to ActivityPub Note objects
3. **Generate Activities**: Wraps Notes in Create activities
4. **Build Outbox**: Creates complete OrderedCollection
5. **Output Files**: Writes to `api/data/` structure

### Output Structure

```
api/data/
├── notes/
│   ├── {hash1}.json  # Individual ActivityPub Note
│   ├── {hash2}.json
│   └── ...
└── outbox/
    └── index.json    # OrderedCollection of Create activities
```

### Running the Script

```bash
# Manual execution with defaults (auto-detects paths)
dotnet fsi Scripts/rss-to-activitypub.fsx

# With custom paths
dotnet fsi Scripts/rss-to-activitypub.fsx \
  --rss-path ./_public/feed/feed.xml \
  --static-path ./api/data \
  --domain "https://lqdev.me"

# Show help
dotnet fsi Scripts/rss-to-activitypub.fsx --help
```

### Command Line Options

```
--rss-path <path>         Path to RSS feed XML file
--static-path <path>      Path to static site output directory
--author-username <user>  Author username (e.g., @user@domain.com)
--site-actor-uri <uri>    Site actor URI
--domain <domain>         Site domain (e.g., https://lqdev.me)
--author-uri <uri>        Author profile URI
--content-template <tmpl> Content template
--notes-path <path>       Relative path for notes
--outbox-path <path>      Relative path for outbox
--help, -h                Show help message
```

### URL Pattern Note

✅ **Current Implementation**: This script now generates URLs using the **current production pattern** (`/api/activitypub/*`), which matches the deployed implementation.

**Current URLs**:
- `https://lqdev.me/api/activitypub/actor`
- `https://lqdev.me/api/activitypub/inbox`
- `https://lqdev.me/api/activitypub/outbox`
- `https://lqdev.me/api/activitypub/followers`
- `https://lqdev.me/api/activitypub/following`

The script output matches the production endpoint structure documented in `/api/ACTIVITYPUB.md`.

### Phase 3 Integration Status

**✅ COMPLETE**: Phase 3 goals achieved through `ActivityPubBuilder.fs` module integration

**Replaced By**: `ActivityPubBuilder.buildOutbox` in main F# build process  
**Location**: Called from `Program.fs` (line 144)  
**Output**: `_public/api/data/outbox/index.json` with complete unified feed content

**What Was Implemented** (January 2026):
- ✅ ActivityPub conversion integrated into F# build process (`ActivityPubBuilder.fs`)
- ✅ Outbox generated automatically during site build from unified feed
- ✅ 1,548 Create activities generated from actual content (posts, notes, responses, etc.)
- ✅ Production-ready OrderedCollection structure with proper ActivityPub spec compliance

**This Script's Legacy**:
1. **Proven Conversion Logic**: Patterns informed `ActivityPubBuilder.fs` implementation
2. **Output Format Reference**: Validated JSON structure and file organization
3. **Content Template System**: Demonstrated conversion approaches
4. **Hash-Based IDs**: Pattern adopted for stable activity identifiers

### Key Conversion Patterns

#### RSS Item → ActivityPub Note

```fsharp
// RSS item structure
{ Title, Link, Description, Content, PubDate, Tags }

// Converted to ActivityPub Note
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/notes/{hash}",
  "type": "Note",
  "content": "{title}\n\n{tags}\n\n🔗 {link}",
  "url": "{original-post-url}",
  "published": "{iso-8601-date}",
  "attributedTo": "https://lqdev.me/api/activitypub/actor",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "tag": [/* hashtags and mentions */]
}
```

#### Note → Create Activity

```fsharp
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/outbox/activities/{hash}",
  "type": "Create",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "{iso-8601-date}",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "object": {/* Note object */}
}
```

### Development Notes

**File-Based Storage**: Script uses file-based storage (no database), aligning with static site architecture

**Build-Time Generation**: Designed for build-time execution, not runtime generation

**RSS as Source**: Treats RSS feed as source of truth for content, ensuring parity

**Cleanup Strategy**: Includes cleanup function to start fresh on each run

---

## Related Documentation

### Implementation Status
- **Current Phase Breakdown**: `/docs/activitypub/implementation-status.md`
- **Phase 3 Planning**: `/docs/activitypub/implementation-plan.md` (Phase 3 section)
- **Completion Summary**: `/docs/activitypub/fix-summary.md`

### API Documentation
- **Endpoint Reference**: `/api/ACTIVITYPUB.md`
- **Azure Functions Code**: `/api/` directory

### Deployment & Operations
- **Deployment Guide**: `/docs/activitypub/deployment-guide.md`
- **Key Vault Setup**: `/docs/activitypub/keyvault-setup.md`

---

## Future Script Enhancements

### Potential Additions

**Phase 3 Integration Script**: Wrapper that runs as part of F# build
```bash
# Hypothetical Phase 3 usage
dotnet run --project PersonalSite.fsproj -- --generate-activitypub
```

**Validation Script**: Verify generated ActivityPub objects against spec
```bash
# Validate outbox structure
./Scripts/validate-activitypub.sh api/data/outbox/index.json
```

**Migration Script**: Update URLs from current to new pattern
```bash
# Update all URLs to /api/activitypub/* pattern
./Scripts/migrate-activitypub-urls.sh
```

### Testing Enhancements

- Add JSON schema validation to test suite
- Test signature generation and verification
- Validate ActivityPub objects against W3C spec
- Add performance benchmarks for large feeds

---

## Contributing

When adding new ActivityPub scripts:

1. **Document Purpose**: Add clear comments explaining script role and status
2. **Update This README**: Add section describing new script
3. **Add Tests**: Create or update test suite for new functionality
4. **Cross-Reference**: Link to relevant docs in comments
5. **Follow Patterns**: Use consistent naming and structure

---

**Last Updated**: January 18, 2026  
**Maintainer**: See commit history for script authors
