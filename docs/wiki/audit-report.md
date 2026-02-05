# Wiki Content Audit Report

**Date**: 2026-02-01  
**Purpose**: Identify /docs content suitable for wiki migration and establish curation priorities.

## Executive Summary

The `/docs` directory contains **99 markdown files** covering implementation guides, architecture documentation, and workflow references. The existing wiki (`_src/resources/wiki/`) contains **27 pages** focused on practical how-tos and reference material. This audit identifies **high-value candidates** for wiki migration based on content type, evergreen potential, and discoverability benefits.

---

## Current State Analysis

### /docs Directory Structure (99 files)

| Category | Count | Description |
|----------|-------|-------------|
| **Implementation Summaries** | ~25 | Feature implementation documentation |
| **How-To Guides** | ~12 | Step-by-step workflow guides |
| **Architecture Documentation** | ~10 | System design and patterns |
| **ActivityPub Docs** | ~25 | Fediverse integration (subdirectory) |
| **Migration Guides** | ~5 | Content migration procedures |
| **Workflow Optimization** | ~8 | CI/CD and process improvements |
| **Feature Documentation** | ~14 | User-facing feature guides |

### Current Wiki (`_src/resources/wiki/`) - 27 entries

| Topic Area | Count | Examples |
|------------|-------|----------|
| **Linux/NixOS** | 8 | NixOS configs, package management, hardware |
| **Self-Hosting** | 5 | Mastodon, Matrix, Owncast |
| **Development Tools** | 5 | DevContainers, Markdig, Pandoc |
| **Privacy/Productivity** | 4 | Privacy guides, plaintext productivity |
| **Index/Meta** | 5 | ML, wiki overview, alternative frontends |

---

## Recommended Wiki Migrations

### 🟢 HIGH PRIORITY - Immediate Value

These documents are **evergreen knowledge** with broad applicability and should move to wiki:

#### 1. DevContainers & Development Environment
**Source**: `docs/devcontainers-configurations.md` concepts (partially in wiki already)  
**Wiki Action**: Expand existing wiki entry with latest configurations

#### 2. Plain Text Productivity
**Current**: Already in wiki as `plaintext.md`  
**Action**: Reference from /docs guides where applicable

#### 3. ActivityPub Architecture Overview
**Source**: `docs/activitypub/ARCHITECTURE-OVERVIEW.md`  
**Rationale**: Comprehensive reference for Fediverse implementation patterns  
**Wiki Topic**: `activitypub-implementation.md`  
**Priority**: High - Evergreen technical reference

#### 4. RSS Feed Architecture
**Source**: `docs/feed-architecture.md`  
**Rationale**: Reference documentation for IndieWeb feed patterns  
**Wiki Topic**: `rss-feed-architecture.md`  
**Priority**: High - Core IndieWeb knowledge

#### 5. Progressive Web App Implementation
**Source**: `docs/PWA_IMPLEMENTATION.md`  
**Rationale**: Reusable pattern for PWA implementation  
**Wiki Topic**: `progressive-web-apps.md`  
**Priority**: High - Web development reference

#### 6. Feature Flag Pattern
**Source**: `docs/feature-flag-pattern.md`  
**Rationale**: Software engineering pattern applicable across projects  
**Wiki Topic**: `feature-flags.md`  
**Priority**: High - Development best practice

---

### 🟡 MEDIUM PRIORITY - Research Summaries

These contain **AI research outputs** and should be curated into wiki-style entries:

#### 7. HTTP Signature Verification
**Source**: `docs/activitypub/http-signature-verification-plan.md`  
**Rationale**: Technical deep-dive on cryptographic signatures  
**Wiki Topic**: `http-signatures.md`  
**Priority**: Medium - Specialized but valuable reference

#### 8. Follower Management Architecture
**Source**: `docs/activitypub/follower-management-architecture.md`  
**Rationale**: Static site ActivityPub patterns  
**Wiki Topic**: Could merge with activitypub-implementation wiki entry

#### 9. Content Processor Optimization
**Source**: `docs/content-processor-optimization-summary.md`  
**Rationale**: F# performance patterns  
**Wiki Topic**: `fsharp-performance-patterns.md`  
**Priority**: Medium - Development reference

#### 10. AI Memory Management System
**Source**: `docs/ai-memory-management-system.md`  
**Rationale**: Meta-documentation on AI workflows  
**Wiki Topic**: `ai-assisted-development.md`  
**Priority**: Medium - Process documentation

---

### 🔵 LOW PRIORITY - Contextual Reference

These documents are **project-specific** and may stay in /docs but could inform wiki entries:

#### Publishing Workflows (Keep in /docs)
- `bookmark-publishing-workflow.md`
- `response-publishing-workflow.md`
- `media-publishing-workflow.md`

**Rationale**: These are internal workflow guides specific to this site. May reference wiki entries but shouldn't become wiki pages themselves.

#### Implementation Summaries (Archive Candidates)
- `*-implementation.md` files
- `*-summary.md` files

**Rationale**: These document completed work. Archive for reference but don't convert to wiki. Per issue #2049/#2050, these should move to `/archive/`.

---

## Documents to NOT Migrate

The following should remain in `/docs` or be archived:

| Document Type | Example | Reason |
|--------------|---------|--------|
| **Migration Guides** | `migration-guide-*.md` | Site-specific, transient |
| **Fix Summaries** | `*_FIX.md`, `*-fix-*.md` | Historical issue resolution |
| **Implementation Reports** | `*-implementation.md` | Completed project documentation |
| **Test Documentation** | `test-*.md` | Development artifacts |
| **Historical Docs** | `activitypub/historical/*` | Already archived |

---

## Proposed New Wiki Entries

Based on audit, recommend creating these **new wiki entries**:

### From /docs Content

| Wiki Entry | Source Documents | Content Type |
|------------|-----------------|--------------|
| `activitypub-implementation.md` | ActivityPub architecture docs | Technical Deep-Dive |
| `rss-feed-architecture.md` | feed-architecture.md | Technical Reference |
| `progressive-web-apps.md` | PWA_IMPLEMENTATION.md | How-To |
| `feature-flags.md` | feature-flag-pattern.md | Best Practice |
| `http-signatures.md` | http-signature-verification-plan.md | Technical Deep-Dive |
| `fsharp-static-sites.md` | core-infrastructure-architecture.md | Technical Reference |
| `ai-assisted-development.md` | ai-memory-management-system.md | Process Guide |

### From Research Outputs (Future)

| Wiki Entry | Content Type | Source |
|------------|--------------|--------|
| AI research summaries | Research Synopsis | AI research outputs |
| Technology comparisons | Evaluation | Research projects |
| Tool configurations | Reference | Development notes |

---

## Integration with Issue #2049/#2050

This audit aligns with the repository cleanup strategy:

1. **Implementation summaries** → Move to `/archive/` (not wiki)
2. **Evergreen knowledge** → Migrate to wiki
3. **Process documentation** → Keep in `/docs` with wiki references
4. **Historical/contextual** → Archive per #2050 plan

---

## Next Steps

1. **Create Information Architecture** document defining wiki categories and tagging
2. **Develop Contribution Guidelines** for consistent wiki entry creation
3. **Define Workflow** for AI-generated content to become wiki entries
4. **Prioritize Migration** starting with HIGH priority items
5. **Coordinate with #2050** for archive vs. wiki decisions

---

## Appendix: Full /docs File Inventory

<details>
<summary>Click to expand full file list</summary>

### Root /docs Files (74)
- ALBUM_COLLECTIONS.md
- MEDIA_BLOCK_FIX.md  
- PWA_IMPLEMENTATION.md
- README.md
- VIDEO_UPLOAD_FIX.md
- WEB_API_ENHANCEMENTS_SUMMARY.md
- WEB_API_IMPLEMENTATION_REPORT.md
- ai-memory-management-system.md
- album-collections-implementation-summary.md
- automerge-fix-summary.md
- autonomous-session-summary-2025-08-04.md
- back-to-top-button-implementation.md
- bookmark-publishing-workflow.md
- bookmarks-landing-page-implementation.md
- broken-link-checker.md
- builder-io-helpers-refactoring.md
- content-processor-optimization-spec.md
- content-processor-optimization-summary.md
- content-sharing-features.md
- content-volume-html-parsing-discovery.md
- copilot-partnership-evolution-autonomy.md
- core-infrastructure-architecture.md
- custom-agents-implementation.md
- custom-presentation-layouts.md
- enhanced-content-discovery-implementation.md
- explicit-home-navigation-implementation.md
- feature-flag-pattern.fsx
- feature-flag-pattern.md
- feed-architecture.md
- github-actions-fix.md
- github-issue-posting-config.md
- github-issue-posting-guide.md
- github-issue-posting-spec.md
- how-to-create-collections.md
- how-to-create-starter-packs.md
- media-publishing-workflow.md
- migration-guide-posts.md
- migration-guide-snippets.md
- migration-guide-wiki.md
- org-capture-templates.md
- pinned-posts-feature.md
- playlist-automation-implementation.md
- playlist-collections.md
- process-read-later-workflow.md
- read-later-cleanup-implementation.md
- read-later-cleanup.md
- read-later-url-removal-feature.md
- response-publishing-workflow.md
- resume-feature.md
- review-github-issue-posting-specification.md
- review-migration-specification.md
- review-migration-summary.md
- review-publishing-implementation-guide.md
- review-system-architecture-recommendation.md
- target-url-display-implementation.md
- test-read-later-cleanup.md
- text-only-site.md
- travel-guide-howto.md
- url-alignment-architecture-decisions.md
- vs-code-snippets-modernization.md
- workflow-caching-before-after.md
- workflow-caching-optimization.md

### /docs/activitypub Files (25)
- ARCHITECTURE-OVERVIEW.md
- README.md
- activitypub-expansion-proposal.md
- deployment-guide.md
- follower-management-architecture.md
- free-tier-monitoring-guide.md
- http-signature-verification-plan.md
- implementation-status.md
- keyvault-setup.md
- notes-function-proxy.md
- outbox-deployment-fix.md
- phase3-implementation-complete.md
- phase3-research-summary.md
- phase4-http-signature-verification-complete.md
- phase4-implementation-plan.md
- phase4-kickoff-summary.md
- phase4-quick-reference.md
- phase4-research-summary.md
- phase4a-complete-summary.md
- phase4a-testing-guide.md
- phase4b-4c-complete-summary.md
- phase4d-shared-inbox-research.md
- phase5-fediverse-native-expansion-plan.md
- phase5a-implementation-complete.md
- phase5d-media-research.md
- phase6a-rsvp-research.md
- historical/ (3 files)

</details>
