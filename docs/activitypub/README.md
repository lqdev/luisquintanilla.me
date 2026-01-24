# ActivityPub Implementation Documentation

**🎯 Start Here** - This is the **entrypoint and source of truth** for all ActivityPub implementation documentation.

**🔒 Production Status**: HTTP Signature Verification **LIVE** as of January 23, 2026

---

## 📖 Primary Documentation

### For Everyone: Comprehensive Overview

**[ARCHITECTURE-OVERVIEW.md](ARCHITECTURE-OVERVIEW.md)** ⭐ **START HERE**
- Complete architectural overview and implementation guide
- High-level architecture (static+dynamic hybrid, Azure Functions, URL scheme, security, storage)
- Implementation phases and current status (**Phases 1-4 COMPLETE** 🎉)
- Azure infrastructure details (Table Storage, Queue Storage, Key Vault, Application Insights)
- Cost considerations and analysis
- Data flow and processing diagrams
- Testing and validation procedures
- Complete reference documentation index

### For AI Coding Assistants

**Quick Reference**: [`implementation-status.md`](implementation-status.md)
- Current phase status and completion details (**Phase 4 COMPLETE** 🔒)
- URL patterns and architectural decisions
- Build strategy and integration points
- Quick reference for contributors

**API Documentation**: [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md)
- Endpoint reference and usage
- Testing procedures
- Troubleshooting guide

**HTTP Signature Verification**: [`phase4-http-signature-verification-complete.md`](phase4-http-signature-verification-complete.md) 🔒
- Complete Phase 4 implementation summary
- Production rollout timeline with hotfixes
- Security enhancements and testing results
- Key learnings from deployment

---

## 📚 Detailed Documentation by Purpose

### For Developers

| What You Need | Document |
|---------------|----------|
| **Comprehensive architecture overview** | [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) ⭐ |
| **Current implementation status** | [`implementation-status.md`](implementation-status.md) |
| **Endpoint documentation** | [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) |
| **Test and validate** | [`../../Scripts/ACTIVITYPUB-SCRIPTS.md`](../../Scripts/ACTIVITYPUB-SCRIPTS.md) |
| **Deploy to Azure** | [`deployment-guide.md`](deployment-guide.md) |
| **Configure Key Vault** | [`keyvault-setup.md`](keyvault-setup.md) |

### For Architects

| Document | Purpose |
|----------|---------|
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) | Complete architecture, design decisions, data flows |
| [`follower-management-architecture.md`](follower-management-architecture.md) | Why static sites need dynamic backends |
| [`phase4-implementation-plan.md`](phase4-implementation-plan.md) | Detailed Phase 4 planning and decisions |
| [`phase4-kickoff-summary.md`](phase4-kickoff-summary.md) | Phase 4 preparation and architecture decisions |

### For Operations/DevOps

| Document | Purpose |
|----------|---------|
| [`deployment-guide.md`](deployment-guide.md) | Azure setup and deployment procedures |
| [`keyvault-setup.md`](keyvault-setup.md) | Azure Key Vault configuration |
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md#testing--validation) | Testing and monitoring procedures |

### Phase Completion Summaries (Historical Record)

| Document | Phase | Date |
|----------|-------|------|
| [`phase3-implementation-complete.md`](phase3-implementation-complete.md) | Phase 3: Outbox Automation | Jan 18, 2026 |
| [`phase4a-complete-summary.md`](phase4a-complete-summary.md) | Phase 4A: Inbox Handler | Jan 18, 2026 |
| [`phase4b-4c-complete-summary.md`](phase4b-4c-complete-summary.md) | Phase 4B/C: Delivery | Jan 20, 2026 |

---

## 🗂️ Documentation Structure

```
docs/activitypub/
├── README.md (this file) ⭐ START HERE
│
├── ARCHITECTURE-OVERVIEW.md ⭐ COMPREHENSIVE GUIDE
│   └── Complete architecture, phases, infrastructure, costs, testing
│
├── implementation-status.md ⭐ CURRENT STATUS
│   └── Phase breakdown, decisions log, roadmap
│
├── follower-management-architecture.md
│   └── Why hybrid static+dynamic is necessary
│
├── Phase Planning & Summaries
│   ├── phase3-implementation-complete.md
│   ├── phase3-research-summary.md
│   ├── phase4-implementation-plan.md
│   ├── phase4-kickoff-summary.md
│   ├── phase4-quick-reference.md
│   ├── phase4-research-summary.md
│   ├── phase4a-complete-summary.md
│   ├── phase4a-testing-guide.md
│   └── phase4b-4c-complete-summary.md
│
├── Operational Guides
│   ├── deployment-guide.md
│   ├── keyvault-setup.md
│   ├── notes-function-proxy.md
│   └── outbox-deployment-fix.md
│
└── historical/ (archived documentation)
    ├── README.md (archive index)
    ├── implementation-plan.md (original 8-week plan)
    ├── az-fn-implementation-plan.md (Azure Functions strategy)
    ├── fix-summary.md (Phase 1-2 completion)
    ├── reconciliation-summary.md (doc reconciliation)
    ├── ACTIVITYPUB-DOCS.md (early navigation)
    └── testing docs (early test implementation)
```

---

## 🚀 Implementation Status at a Glance

| Phase | Status | What It Does |
|-------|--------|--------------|
| **Phase 1** | ✅ **COMPLETE** | Discovery & URL Standardization |
| **Phase 2** | ✅ **COMPLETE** | Follow/Accept Workflow + Key Vault Security |
| **Phase 3** | ✅ **COMPLETE** | Outbox Automation from F# Build (1,547+ items) |
| **Phase 4A** | ✅ **COMPLETE** | Inbox Handler + Table Storage Integration |
| **Phase 4B/C** | ✅ **COMPLETE** | Delivery Infrastructure + GitHub Actions |

**Current Capabilities**:
- ✅ Discoverable from Mastodon (`@lqdev@lqdev.me`)
- ✅ Accept Follow requests with HTTP signature verification
- ✅ Maintain persistent followers collection in Azure Table Storage
- ✅ Secure key management via Azure Key Vault
- ✅ Automatic outbox generation from website content
- ✅ Automatic post delivery to all follower inboxes
- ✅ Queue-based async processing with retry logic
- ✅ Production monitoring via Application Insights

---

## 🧪 Quick Actions

### Test ActivityPub Endpoints
```bash
./Scripts/test-activitypub.sh
```

### Check Implementation Status
Read: [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) or [`implementation-status.md`](implementation-status.md)

### Test from Mastodon
1. Search for: `@lqdev@lqdev.me`
2. Click Follow
3. Publish new content to test delivery
4. Verify post appears in your timeline

### Deploy to Production
Follow: [`deployment-guide.md`](deployment-guide.md)

---

## 📍 Key Implementation Details

### Current URL Pattern (Production)
```
https://lqdev.me/.well-known/webfinger           → Discovery
https://lqdev.me/api/activitypub/actor           → Actor profile
https://lqdev.me/api/activitypub/inbox           → Receive activities
https://lqdev.me/api/activitypub/outbox          → Public activities
https://lqdev.me/api/activitypub/followers       → Followers collection
https://lqdev.me/api/activitypub/following       → Following collection
https://lqdev.me/api/activitypub/notes/{hash}    → Individual notes
```

**Rationale**: `/api/activitypub/*` enables other `/api/*` functionality while keeping ActivityPub logically grouped.

### Azure Infrastructure

**Services Used**:
- Azure Functions (serverless compute)
- Azure Table Storage (follower state, delivery tracking)
- Azure Queue Storage (async processing)
- Azure Key Vault (signing keys)
- Application Insights (monitoring)

**Monthly Cost**: ~$0.02-5 (mostly within free tiers)

---

## 📖 External Resources

### Specifications
- [W3C ActivityPub](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)
- [WebFinger RFC 7033](https://tools.ietf.org/html/rfc7033)
- [HTTP Signatures RFC 9421](https://datatracker.ietf.org/doc/html/rfc9421)

### Implementation Guides
- [Maho.dev: ActivityPub in Static Sites](https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/)
- [Mastodon ActivityPub Docs](https://docs.joinmastodon.org/spec/activitypub/)

---

## 🤝 Contributing

When working with ActivityPub implementation:

1. **Start with Overview**: Read [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) to understand architecture
2. **Check Current Status**: Review [`implementation-status.md`](implementation-status.md) for latest state
3. **API Reference**: Check [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) for endpoints
4. **Test**: Run `./Scripts/test-activitypub.sh` after changes
5. **Update Docs**: Keep documentation current with implementation changes

---

**Last Updated**: January 22, 2026  
**Maintainer**: See commit history
