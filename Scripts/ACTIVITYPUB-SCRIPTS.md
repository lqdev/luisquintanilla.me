# ActivityPub Scripts Documentation

This document describes the ActivityPub-related scripts in this directory and their role in the overall implementation.

---

## test-activitypub.sh

**Purpose**: Automated test suite for ActivityPub endpoints  
**Status**: Production-ready, actively used  
**Usage**: Run before and after changes to validate functionality

### What It Tests

- WebFinger discovery (both `@lqdev.me` and `@www.lqdev.me` formats)
- Actor endpoint validation
- Collections endpoints (outbox, followers, following)
- HTTP status codes and content-type headers
- JSON structure validation

### Running the Tests

```bash
# From repository root
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
- After deployment to production
- When debugging federation issues
- As part of CI/CD validation (future integration)

### Test Coverage

| Endpoint | Tests |
|----------|-------|
| WebFinger | Response format, both domain variants, status code |
| Actor | JSON structure, required fields, public key format |
| Outbox | Collection type, ordered items, activity structure |
| Followers | Collection type, dynamic updates |
| Following | Collection type, empty state handling |

---

## rss-to-activitypub.fsx

**Purpose**: Prototype for Phase 3 (Outbox Automation) implementation  
**Status**: Standalone script, not integrated with build  
**Role**: Reference implementation for future F# module

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

⚠️ **Important**: This script generates URLs using the **planned future pattern** (`/api/activitypub/*`), which differs from the current implementation (`/api/*`).

**Current Implementation**:
- `https://lqdev.me/api/actor`
- `https://lqdev.me/api/inbox`
- `https://lqdev.me/api/outbox`

**Script Output** (Planned Pattern):
- `https://lqdev.me/api/activitypub/actor`
- `https://lqdev.me/api/activitypub/inbox`
- `https://lqdev.me/api/activitypub/outbox`

When the URL migration happens, this script will already use the correct pattern. Until then, be aware of this discrepancy when reviewing generated files.

### Integration with Phase 3

**Current State**: Standalone prototype, not part of build pipeline  
**Phase 3 Goals**: 
- Integrate RSS → ActivityPub conversion into F# build process
- Generate outbox automatically during site build
- Replace manual outbox entries with actual content
- Create individual note files for content discovery

**How This Script Helps Phase 3**:
1. **Proven Conversion Logic**: RSS parsing and ActivityPub object generation patterns
2. **Output Format Reference**: Shows expected JSON structure and file organization
3. **Content Template System**: Demonstrates customizable content formatting
4. **Hash-Based IDs**: Provides stable, unique identifiers for activities

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
