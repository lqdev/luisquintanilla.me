# Repository Audit Report

**Date**: 2026-02-01  
**Scope**: Comprehensive analysis of all files, scripts, code, and documentation

---

## Executive Summary

This audit inventories and categorizes all files in the repository, identifies potential cleanup candidates, and provides recommendations for ongoing maintenance.

### Repository Statistics

| Category | Count |
|----------|-------|
| F# Scripts (`.fsx`) | 55 |
| Shell Scripts (`.sh`) | 22 |
| PowerShell Scripts (`.ps1`) | 9 |
| Python Scripts (`.py`) | 14 |
| JavaScript Files (`.js`) | 50+ |
| Core F# Modules (`.fs`) | 20+ |
| Markdown Documentation | 100+ |

---

## 1. Scripts Analysis (`/Scripts/`)

### Active Production Scripts (Keep)

| Script | Purpose | Status |
|--------|---------|--------|
| `process-bookmark-issue.fsx` | GitHub Issue → Bookmark content | ✅ Active |
| `process-github-issue.fsx` | Generic issue processing | ✅ Active |
| `process-media-issue.fsx` | GitHub Issue → Media content | ✅ Active |
| `process-playlist-issue.fsx` | GitHub Issue → Playlist content | ✅ Active |
| `process-response-issue.fsx` | GitHub Issue → Response content | ✅ Active |
| `process-review-issue.fsx` | GitHub Issue → Review content | ✅ Active |
| `send-webmentions.fsx` | Webmention sending | ✅ Active |
| `identify-webmentions.fsx` | Webmention discovery | ✅ Active |
| `rss.fsx` | RSS feed utilities | ✅ Active |
| `tags.fsx` | Tag management | ✅ Active |
| `stats.fsx` | Site statistics | ✅ Active |
| `weekly-wrapup.fsx` | Content summarization | ✅ Active |

### Azure/ActivityPub Scripts (Keep)

| Script | Purpose | Status |
|--------|---------|--------|
| `setup-activitypub-azure-resources.ps1` | Azure resource provisioning | ✅ Active |
| `configure-activitypub-secrets.ps1` | GitHub/Azure secret config | ✅ Active |
| `rss-to-activitypub.fsx` | RSS → ActivityPub conversion | ✅ Active |
| `jwk-to-pem.ps1` / `.sh` | Key format conversion | ✅ Active |

### Testing/Validation Scripts (Keep in Scripts/)

| Script | Purpose | Status |
|--------|---------|--------|
| `check-broken-links.fsx` | Link validation | ✅ Active |
| `check-broken-links-simple.fsx` | Simplified link check | ✅ Active |
| `check-site-sizes.ps1` / `.sh` | Size monitoring | ✅ Active |
| `test-activitypub.sh` | ActivityPub testing | ✅ Active |
| `test-activitypub-production.sh` | Production testing | ✅ Active |
| `validate-activitypub-urls.sh` | URL validation | ✅ Active |

### Candidates for Review/Archive

| Script | Purpose | Recommendation |
|--------|---------|----------------|
| `scratch.fsx` | Development scratch | ⚠️ Consider removing or archiving |
| `samplePresentation.html` | Sample file | ⚠️ Move to demos if needed |
| `migrate-book-reviews.fsx` | One-time migration | ⚠️ Archive after confirming completion |
| `testTags.fsx` | Duplicate of test-scripts version? | ⚠️ Review for duplication |
| `ai.fsx` | AI-related utilities | 🔍 Review current usage |

---

## 2. Test Scripts Analysis (`/test-scripts/`)

### Migration Test Scripts (Completed - Archive Candidates)

These scripts were used for completed migrations:

| Script | Migration | Status |
|--------|-----------|--------|
| `book-migration-analysis.fsx` | Books migration | ✅ Complete - Archive |
| `migrate-book-reviews.fsx` | Books migration | ✅ Complete - Archive |
| `test-migrate-*.fsx` (6 files) | Various migrations | ✅ Complete - Archive |
| `test-migrated-files.fsx` | Migration validation | ✅ Complete - Archive |

### Core Infrastructure Tests (Keep)

| Script | Purpose | Status |
|--------|---------|--------|
| `test-ast-parsing.fsx` | AST parsing validation | ✅ Active |
| `test-context-validation.fsx` | Context validation | ✅ Active |
| `test-integration.fsx` | Integration testing | ✅ Active |
| `test-output-comparison.fsx` | Output validation | ✅ Active |

### Feature-Specific Tests (Keep)

| Script | Purpose | Status |
|--------|---------|--------|
| `test-phase1-collections.fsx` | Collections testing | ✅ Active |
| `test-phase2-*.fsx` | Phase 2 testing | ✅ Active |
| `test-travel-collection.fsx` | Travel feature testing | ✅ Active |
| `test-reading-time.fsx` | Reading time feature | ✅ Active |

### Issue-Specific Tests (Archive Candidates)

These were created for specific GitHub issues and may no longer be needed:

| Script | Issue | Recommendation |
|--------|-------|----------------|
| `test-issue-688.py` | Issue #688 | ⚠️ Archive if issue closed |
| `test-issue-698-fix.py` | Issue #698 | ⚠️ Archive if issue closed |
| `test-issue-722.py` | Issue #722 | ⚠️ Archive if issue closed |

### Python Test Scripts

| Script | Purpose | Recommendation |
|--------|---------|----------------|
| `test-media-*.py` (5 files) | Media testing | 🔍 Review - consolidate or archive |
| `demo-position-fix.py` | Position fix demo | ⚠️ Archive after review |
| `test-end-to-end-workflow.py` | E2E testing | ✅ Keep if still used |

---

## 3. Test Validation Directory (`/_test_validation/`)

### Current Contents

```
_test_validation/
├── design/          # UI design tests (1.6MB index.html)
├── media/           # Media content samples
├── new_wikis/wiki/  # New wiki format samples
└── old_wikis/wiki/  # Old wiki format samples
```

### Recommendation

**Consider archiving entire directory** - This appears to be migration validation content:
- `new_wikis` and `old_wikis` suggest wiki migration validation (completed)
- Large `index.html` in design suggests UI prototype
- Media samples may have been for testing media processing

---

## 4. Core F# Modules Analysis

### Active Production Modules (Root Directory)

| Module | Purpose | Status |
|--------|---------|--------|
| `Program.fs` | Entry point | ✅ Active |
| `Domain.fs` | Core types | ✅ Active |
| `Builder.fs` | Build orchestration | ✅ Active |
| `GenericBuilder.fs` | Content processing | ✅ Active |
| `ASTParsing.fs` | Markdown parsing | ✅ Active |
| `CustomBlocks.fs` | Markdig extensions | ✅ Active |
| `BlockRenderers.fs` | HTML rendering | ✅ Active |
| `Loaders.fs` | File loading | ✅ Active |
| `MediaTypes.fs` | Media handling | ✅ Active |
| `SearchIndex.fs` | Search functionality | ✅ Active |
| `TextOnlyBuilder.fs` | Accessibility site | ✅ Active |
| `Collections.fs` | Collections system | ✅ Active |
| `StarterPackSystem.fs` | Starter packs | ✅ Active |
| `ActivityPubBuilder.fs` | Fediverse integration | ✅ Active |
| `OutputComparison.fs` | Build validation | ✅ Active |

### Views Modules (`/Views/`)

All 10 view modules are active and properly organized.

### Services Modules (`/Services/`)

All service modules (Markdown, Tag, Opml, Webmention) are active.

---

## 5. API Directory (`/api/`)

### Structure

```
api/
├── activitypub-activities/
├── actor/
├── data/
├── followers/
├── following/
├── inbox/
├── outbox/
├── scripts/
├── utils/
├── webfinger/
├── QueueDeliveryTasks/
├── test-health/
└── [test files]
```

### Test Files in API (Review)

| File | Recommendation |
|------|----------------|
| `test-post-delivery.js` | ⚠️ Move to test-scripts or archive |
| `test-redirect.ps1` | ⚠️ Move to test-scripts or archive |
| `test-specific-paths.ps1` | ⚠️ Move to test-scripts or archive |
| `test-table-storage.js` | ⚠️ Move to test-scripts or archive |

---

## 6. GitHub Actions Workflows (`.github/`)

### Active Workflows

| Workflow | Purpose | Status |
|----------|---------|--------|
| `azure-static-web-apps.yml` | Site deployment | ✅ Active |
| `process-content-issue.yml` | Content publishing | ✅ Active |
| `broken-link-checker.yml` | Link validation | ✅ Active |

### Scripts Directory

| Script | Purpose | Status |
|--------|---------|--------|
| `upload_media.py` | S3 media upload | ✅ Active |
| `test_s3_connection.py` | S3 connection test | ⚠️ Could move to test-scripts |

---

## 7. Miscellaneous Files

### Test Content (`/test-content/`)

| File | Purpose | Recommendation |
|------|---------|----------------|
| `comprehensive-blocks-test.md` | Block testing | ✅ Keep |
| `simple-review-test.md` | Review testing | ✅ Keep |
| `simplified-schema-example.md` | Schema example | ✅ Keep |
| `start-server.ps1` | Dev server | ⚠️ Consider moving to Scripts |

### Scratch Directory (`/_scratch/`)

Development draft content - currently appropriate location.

### Issues Directory (`/issues/`)

Contains `1.md` - purpose unclear, review needed.

---

## 8. Recommendations Summary

### Immediate Actions (Low Risk)

1. ✅ **DONE**: Move root-level implementation summaries to archive
2. ✅ **DONE**: Move demo files to archive
3. ✅ **DONE**: Create ADR system
4. ✅ **DONE**: Create CONTRIBUTING.md

### Recommended Next Steps

1. **Archive completed migration scripts**:
   - Move `migrate-book-reviews.fsx` to archive
   - Move `test-migrate-*.fsx` files to archive

2. **Review issue-specific test scripts**:
   - Check if issues #688, #698, #722 are closed
   - Archive test scripts for closed issues

3. **Consolidate test files in API**:
   - Move `test-*.js` and `test-*.ps1` from `/api/` to `/test-scripts/`

4. **Review `_test_validation/` directory**:
   - If wiki migration is complete, archive the directory
   - If design testing is complete, archive or delete

5. **Clean up duplicates**:
   - Compare `Scripts/testTags.fsx` with `test-scripts/` versions
   - Remove duplicates

### Long-term Maintenance

1. **Script documentation**: Add purpose comments to all scripts
2. **Test script organization**: Consider categorizing by feature vs migration vs issue
3. **Regular audits**: Schedule quarterly script/test review

---

## 9. File Counts by Directory

| Directory | Files | Notes |
|-----------|-------|-------|
| `/Scripts/` | 47 | Mix of production and test scripts |
| `/test-scripts/` | 50 | Testing and validation scripts |
| `/Views/` | 10 | Modular view architecture |
| `/Services/` | 4 | Shared services |
| `/api/` | 20+ | Azure Functions |
| `/docs/` | 65+ | Feature documentation |
| `/archive/` | 25+ | Historical artifacts |
| `/_src/` | 1000+ | Content source files |

---

## 10. Archival Criteria (Updated)

Files should be archived when they:

1. **Migration scripts**: One-time migrations that are complete
2. **Issue-specific tests**: Tests for resolved GitHub issues
3. **Demo/prototype files**: Proof-of-concept files no longer needed
4. **Implementation summaries**: Documentation of completed features
5. **Superseded code**: Code replaced by newer implementations

Files should NOT be archived:

1. **Active production scripts**: Used in workflows or regular operations
2. **Infrastructure tests**: Validate core functionality
3. **Feature tests**: Validate current features
4. **Configuration files**: Active configuration

---

*This audit was conducted as part of the Repository Cleanup and Information Architecture initiative (Issue #2049).*
