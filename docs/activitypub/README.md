# ActivityPub Implementation Documentation

**🎯 Start Here** - This is the **entrypoint and source of truth** for all ActivityPub implementation documentation.

---

## Quick Navigation

### For AI Coding Assistants

**Primary Reference**: [`implementation-status.md`](implementation-status.md)
- Complete current state (Phase 1-2 production, Phase 3-4 planned)
- URL patterns (current `/api/*` vs planned `/api/activitypub/*`)
- RSS script role and Phase 3 integration
- Build strategy and architectural decisions
- Quick reference for contributors

**API Documentation**: [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md)
- Endpoint reference and usage
- Testing procedures
- Troubleshooting guide

### For Developers

| What You Need | Document |
|---------------|----------|
| **Current implementation status** | [`implementation-status.md`](implementation-status.md) |
| **Endpoint documentation** | [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) |
| **Test and validate** | [`../../Scripts/ACTIVITYPUB-SCRIPTS.md`](../../Scripts/ACTIVITYPUB-SCRIPTS.md) |
| **Deploy to Azure** | [`deployment-guide.md`](deployment-guide.md) |
| **Configure Key Vault** | [`keyvault-setup.md`](keyvault-setup.md) |

### For Reviewers

| Document | Purpose |
|----------|---------|
| [`reconciliation-summary.md`](reconciliation-summary.md) | Complete reconciliation outcomes and decisions |
| [`fix-summary.md`](fix-summary.md) | Phase 1-2 completion details |
| [`ACTIVITYPUB-DOCS.md`](ACTIVITYPUB-DOCS.md) | Full navigation guide |

---

## Document Structure

```
docs/activitypub/
├── README.md (this file) ⭐ START HERE
│
├── implementation-status.md ⭐ MASTER STATUS
│   └── Current state, phases, roadmap, decisions
│
├── ACTIVITYPUB-DOCS.md
│   └── Comprehensive navigation guide
│
├── reconciliation-summary.md
│   └── Documentation reconciliation outcomes
│
├── fix-summary.md
│   └── Phase 1-2 completion summary
│
├── deployment-guide.md
│   └── Azure setup and testing
│
├── keyvault-setup.md
│   └── Key Vault configuration
│
├── implementation-plan.md
│   └── Original technical plan (historical)
│
└── az-fn-implementation-plan.md
    └── Azure Functions strategy (historical)
```

---

## Implementation Status at a Glance

| Phase | Status | What It Does |
|-------|--------|--------------|
| **Phase 1** | ✅ **PRODUCTION** | Discovery & URL Standardization |
| **Phase 2** | ✅ **PRODUCTION** | Follow/Accept Workflow + Key Vault Security |
| **Phase 3** | 📋 **PLANNED** | Outbox Automation from F# Build |
| **Phase 4** | 📋 **FUTURE** | Activity Delivery to Followers |

**Current Capabilities**:
- ✅ Discoverable from Mastodon (`@lqdev@lqdev.me`)
- ✅ Accept Follow requests with HTTP signature verification
- ✅ Maintain persistent followers collection
- ✅ Secure key management via Azure Key Vault
- ⏳ Automatic content delivery (requires Phase 3+4)

---

## Quick Actions

### Test ActivityPub Endpoints
```bash
./Scripts/test-activitypub.sh
```

### Check Implementation Status
Read: [`implementation-status.md`](implementation-status.md)

### Test from Mastodon
1. Search for: `@lqdev@lqdev.me`
2. Click Follow
3. Verify: `curl -H "Accept: application/activity+json" https://lqdev.me/api/followers`

### Deploy to Production
Follow: [`deployment-guide.md`](deployment-guide.md)

---

## Key Implementation Details

### Current URL Pattern (Production)
```
https://lqdev.me/.well-known/webfinger → /api/webfinger
https://lqdev.me/api/actor
https://lqdev.me/api/inbox
https://lqdev.me/api/outbox
https://lqdev.me/api/followers
https://lqdev.me/api/following
```

### Planned URL Migration
```
https://lqdev.me/api/activitypub/actor
https://lqdev.me/api/activitypub/inbox
https://lqdev.me/api/activitypub/outbox
https://lqdev.me/api/activitypub/followers
https://lqdev.me/api/activitypub/following
```

**Rationale**: Enable other `/api/*` functionality while keeping ActivityPub logically grouped.

### RSS Script (`Scripts/rss-to-activitypub.fsx`)
- **Role**: Phase 3 prototype for F# build integration
- **Status**: Standalone (not integrated with build)
- **Purpose**: Demonstrates RSS → ActivityPub conversion patterns
- **URL Pattern**: Uses planned `/api/activitypub/*` pattern (future-aligned)

---

## External Resources

### Specifications
- [W3C ActivityPub](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)
- [WebFinger RFC 7033](https://tools.ietf.org/html/rfc7033)
- [HTTP Signatures](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures)

### Implementation Guides
- [Maho.dev: ActivityPub in Static Sites](https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/)
- [Mastodon ActivityPub Docs](https://docs.joinmastodon.org/spec/activitypub/)

---

## Contributing

When working with ActivityPub implementation:

1. **Start Here**: Read [`implementation-status.md`](implementation-status.md) for current state
2. **API Reference**: Check [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) for endpoints
3. **Test**: Run `./Scripts/test-activitypub.sh` after changes
4. **Update Docs**: Keep [`implementation-status.md`](implementation-status.md) current

---

**Last Updated**: January 18, 2026  
**Maintainer**: See commit history for recent contributors
