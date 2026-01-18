# ActivityPub Documentation Navigation

This document provides a quick reference to all ActivityPub-related documentation in this repository.

---

## 🎯 Start Here

**New to ActivityPub implementation?** Read these in order:

1. **[API Documentation](../api/ACTIVITYPUB.md)** - Understanding the endpoints and architecture
2. **[Implementation Status](activitypub-implementation-status.md)** - Current state, phases, and roadmap
3. **[Testing Guide](../Scripts/ACTIVITYPUB-SCRIPTS.md)** - How to test and validate functionality

---

## 📚 Documentation by Purpose

### Understanding Current State

| Document | Purpose | Audience |
|----------|---------|----------|
| **[Implementation Status](activitypub-implementation-status.md)** | **Complete current state overview** | Everyone |
| [API Documentation](../api/ACTIVITYPUB.md) | Endpoint reference and usage | Developers, Contributors |
| [Fix Summary](activitypub-fix-summary.md) | Phase 1-2 completion details | Reviewers, Historical reference |

### Implementation & Planning

| Document | Purpose | Audience |
|----------|---------|----------|
| [Implementation Plan](activitypub-implementation-plan.md) | Original 8-week phased plan | Architects, Historical reference |
| [Azure Functions Plan](activitypub-az-fn-implementation-plan.md) | Azure-specific strategy | Azure developers |
| [Script Documentation](../Scripts/ACTIVITYPUB-SCRIPTS.md) | RSS conversion and testing scripts | Phase 3 implementers |

### Deployment & Operations

| Document | Purpose | Audience |
|----------|---------|----------|
| [Deployment Guide](activitypub-deployment-guide.md) | Post-merge Azure setup | DevOps, Deployers |
| [Key Vault Setup](activitypub-keyvault-setup.md) | Secure key management | DevOps, Security |

---

## 🔍 Find Information By Question

### "What's currently working?"

➡️ **[Implementation Status](activitypub-implementation-status.md)** - See "Phase 1: Discovery & URL Standardization" and "Phase 2: Follow/Accept Workflow"

### "How do I test ActivityPub endpoints?"

➡️ **[API Documentation](../api/ACTIVITYPUB.md)** - See "Testing" section  
➡️ **[Script Documentation](../Scripts/ACTIVITYPUB-SCRIPTS.md)** - See "test-activitypub.sh" section

### "How do I deploy ActivityPub to production?"

➡️ **[Deployment Guide](activitypub-deployment-guide.md)** - Complete Azure setup walkthrough

### "What's the RSS script for?"

➡️ **[Implementation Status](activitypub-implementation-status.md)** - See "RSS Script Analysis" section  
➡️ **[Script Documentation](../Scripts/ACTIVITYPUB-SCRIPTS.md)** - See "rss-to-activitypub.fsx" section

### "What's planned for Phase 3?"

➡️ **[Implementation Status](activitypub-implementation-status.md)** - See "Phase 3: Outbox Automation" section  
➡️ **[Implementation Plan](activitypub-implementation-plan.md)** - See "Phase 3: Content Integration"

### "How do I add a new ActivityPub endpoint?"

➡️ **[API Documentation](../api/ACTIVITYPUB.md)** - See "Contributing" section  
➡️ **[Implementation Status](activitypub-implementation-status.md)** - See "Quick Reference for Contributors"

### "What are the URL patterns?"

➡️ **[Implementation Status](activitypub-implementation-status.md)** - See "URL Structure (Current)" and "URL Structure (Planned Migration)"  
➡️ **[API Documentation](../api/ACTIVITYPUB.md)** - See "URL Structure" section

### "How do I debug federation issues?"

➡️ **[API Documentation](../api/ACTIVITYPUB.md)** - See "Troubleshooting" section  
➡️ **[Deployment Guide](activitypub-deployment-guide.md)** - See "Common Issues" section

---

## 📋 Documentation Hierarchy

```
Primary Reference (Most Current & Complete)
├── /api/ACTIVITYPUB.md
│   └── Complete endpoint reference, current implementation, testing
│
Current State & Roadmap
├── /docs/activitypub-implementation-status.md
│   └── Phase breakdown, decisions log, next steps
│
Implementation Plans (Historical Context + Technical Details)
├── /docs/activitypub-implementation-plan.md
│   └── Original 8-week phased plan with F# integration details
├── /docs/activitypub-az-fn-implementation-plan.md
│   └── Azure Functions-specific implementation strategy
│
Status & Completion Summaries
├── /docs/activitypub-fix-summary.md
│   └── Phase 1-2 completion details, learnings, validation
│
Deployment & Operations
├── /docs/activitypub-deployment-guide.md
│   └── Post-merge Azure setup, testing procedures
├── /docs/activitypub-keyvault-setup.md
│   └── Azure Key Vault configuration details
│
Testing & Scripts
├── /Scripts/ACTIVITYPUB-SCRIPTS.md
│   └── Script documentation (test suite, RSS conversion)
├── /Scripts/test-activitypub.sh
│   └── Automated endpoint validation
├── /Scripts/rss-to-activitypub.fsx
    └── Phase 3 prototype (RSS → ActivityPub conversion)
```

---

## 🚀 Quick Actions

### Run ActivityPub Tests
```bash
./Scripts/test-activitypub.sh
```

### Test from Mastodon
1. Search for: `@lqdev@lqdev.me`
2. Click Follow
3. Verify in followers collection:
```bash
curl -H "Accept: application/activity+json" https://lqdev.me/api/followers
```

### Check Current Implementation Status
Read: [`activitypub-implementation-status.md`](activitypub-implementation-status.md)

### Deploy to Production
Follow: [`activitypub-deployment-guide.md`](activitypub-deployment-guide.md)

---

## 🔗 External Resources

### Specifications
- [W3C ActivityPub Recommendation](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)
- [WebFinger RFC 7033](https://tools.ietf.org/html/rfc7033)
- [HTTP Signatures](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures)

### Implementation Guides
- [Maho.dev: ActivityPub in Static Sites](https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/)
- [Mastodon ActivityPub Documentation](https://docs.joinmastodon.org/spec/activitypub/)
- [Mastodon WebFinger Guide](https://docs.joinmastodon.org/spec/webfinger/)

---

## 📝 Document Maintenance

**When to Update This Navigation**:
- New ActivityPub document is added
- Document purpose or audience changes significantly
- New major section is added to existing document
- Documentation hierarchy is reorganized

**Last Updated**: January 18, 2026  
**Maintainer**: See commit history
