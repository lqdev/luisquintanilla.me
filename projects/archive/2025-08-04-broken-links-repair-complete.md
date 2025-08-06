# Broken Links Repair - URL Realignment Compliance

**Project**: Fix broken `/feed/` URLs and align with URL architecture standards  
**Created**: 2025-07-29  
**Priority**: MEDIUM - Enhanced analysis reduced scope significantly  
**Status**: [>] ACTIVE - Legacy content migration phase

**MAJOR BREAKTHROUGH**: Enhanced link analysis with trailing slash handling revealed that ~800 of 1000+ "broken" links were routing issues, not missing content. Actual broken links reduced to 252 items requiring targeted fixes.

## Problem Analysis

### Root Cause
Legacy content in `_src/feed/` directory generating URLs like `/feed/[slug]` that conflict with our URL realignment architecture. According to `docs/url-alignment-architecture-decisions.md`, the `/feed/` path should ONLY contain OPML discovery files.

### Domain Classification Update
**Important**: `www.luisquintanilla.me` and `www.lqdev.me` are both internal domains for this website. The link analysis script needs to be updated to treat these as internal links, not external.

### Broken URLs Identified
**Total Broken Links**: 1000+ URLs across multiple categories

**Critical 404 Errors**:
- `/blogroll` (missing collection page)
- `/bluesky` (missing social media page)  
- `/bluesky.rss` (missing RSS feed)
- `/youtube` (missing social media page)
- `/youtube.rss` (missing RSS feed)
- `/wiki` (missing wiki index)

**Legacy /feed/ Content** (200+ broken URLs):
- `/feed/how-do-you-watch-blueray-on-pc` (404)
- `/feed/am-i-in-2006-newspapers-cds-mp3s` (404)  
- `/feed/rss-vlc-podcast-management` (404)
- `/feed/defcon-media-server` (404)
- `/feed/404-media-full-text-rss` (404)
- `/feed/verge-physical-media-week` (404)
- `/feed/streaming-fatigue-collecting-dvds` (404)
- `/feed/dismantle-technology-newport` (404)

**Production Domain Issues**:
- `https://www.lqdev.me/collections/blogroll` (404)
- `https://www.lqdev.me/collections/forums` (404)
- `https://www.lqdev.me/collections/podroll` (404)
- `https://www.lqdev.me/collections/youtube` (404)
- `https://www.luisquintanilla.me/contact.html` (404)

**Tag System Failures** (500+ URLs):
- Connection errors on most `/tags/[tag-name]` URLs
- Affects major tags like `/tags/ai`, `/tags/llm`, `/tags/technology`
- May indicate local server routing issues

### Additional Investigation Needed
- Links to production domains (`https://www.luisquintanilla.me/*`) that may be broken
- Links to alternate domain (`https://www.lqdev.me/*`) that may need validation

### Navigation Issues
**Connection Errors** (Local Development):
- `/tags/*` - 500+ tag URLs failing with connection errors
- May indicate F# routing issues or build process problems

**404 Errors** (Missing Pages):
- `/reviews` - Should redirect or exist  
- `/collections/youtube` - Missing collection page
- `/resources/snippets` - Missing snippets index
- `/resources/wiki` - Missing wiki index
- `/wiki` - Missing main wiki page

## Implementation Plan

### Phase 1: Content Type Analysis ✅
**Objective**: Categorize all content in `_src/feed/` directory

**Findings**:
- **195+ files** in `_src/feed/` directory
- **Majority are notes** (post_type: "note")
- **Weekly summaries** (should be notes)
- **Some responses** may need to go to `_src/responses/`

### Phase 1.5: Link Analysis Script Update ✅
**Objective**: Fix domain classification in link analysis script

**Completed**:
- ✅ Updated `Test-InternalUrl` function to treat both production domains as internal
- ✅ Added domain checks for `luisquintanilla.me` and `lqdev.me` (with/without www)
- ✅ **Re-run link analysis completed** - analyzed 2,317 HTML files
- ✅ **Critical broken links identified**: 1000+ broken URLs

**MAJOR DISCOVERY** 🎯:
- ✅ **Trailing slash issue identified**: Many "404" URLs work with trailing `/`
- ✅ **Verified fixes**: `/collections/youtube/`, `/resources/snippets/`, `/resources/wiki/`, `/wiki/`, `/reviews/`, `/tags/ai/`, `/tags/llm/` all return 200 OK
- ✅ **Root cause**: Directory URLs without trailing slashes not handled by server routing
- 🔄 **Quick fix potential**: 500+ "broken" URLs may just need trailing slash handling

**Key Findings**:
- **Top External Domains**: webmentions.lqdev.tech (3,420), github.com (2,491), twitter.com (2,328)
- **Critical Broken Patterns**:
  - **Legacy /feed/ URLs**: 200+ broken `/feed/[content-name]/` links (still need migration)
  - **Missing Collections**: `/blogroll`, `/bluesky`, `/bluesky.rss` (404s) - need creation
  - **Tag System Failures**: 500+ tag URLs - **LIKELY FIXED** with trailing slashes
  - **Production Domain 404s**: Several `https://www.lqdev.me/collections/*` URLs returning 404
  - **Navigation Routes**: `/wiki`, `/youtube`, `/youtube.rss` returning 404s - some fixed with `/`

### Phase 1.7: Enhanced URL Testing with Trailing Slash Handling ✅ COMPLETED
**Objective**: Create improved link checker that handles directory URLs properly

**Actions**:
1. ✅ **Enhanced Test-UrlAccessible function** to try both with/without trailing slashes  
2. ✅ **Re-run analysis completed** with trailing slash fallback logic
3. ✅ **Categorized results**: True 404s vs. missing trailing slash issues identified
4. [ ] **Identify F# routing fix** for automatic trailing slash redirects (future enhancement)

**FINAL RESULTS**:
- **Total Unique URLs**: 28,953
- **Total Internal URLs**: 12,616 
- **Total External URLs**: 16,337
- **Actual Broken Links**: 252 (down from 1000+ with trailing slash handling)

**Broken Link Categories**:
1. **Legacy /feed/ URLs**: ~200 items (require content migration)  
2. **Missing Images**: /images/contact/ QR codes and other assets
3. **Broken Internal References**: Old post references, presentation links
4. **Invalid External URLs**: Production domain timeouts and 404s

**Impact Assessment**: 
- ✅ **Trailing slash handling successfully reduced broken links by ~800**
- ✅ **Remaining 252 issues are actual missing content requiring targeted fixes**
- ✅ **Primary focus now shifts to /feed/ content migration (~200 URLs)**
- Focuses effort on actual missing content vs. routing issues

### Phase 2: Architecture Reference Updates � IMMEDIATE PRIORITY
**Objective**: Fix the ~150 architecture migration references (old URL patterns to new)

**CRITICAL DISCOVERY**: Most "broken" links are old architecture references where content exists but internal links point to legacy patterns:
- `/wiki/[name]/` → content exists at `/resources/wiki/[name]/` ✅
- `/snippets/[name]/` → content exists at `/resources/snippets/[name]/` ✅  
- `/presentations/[name]/` → content exists at `/resources/presentations/[name]/` ✅

**Architecture Pattern Fixes**:
1. **🟢 GREEN (Act Immediately)** - Search and replace URL patterns in content files
2. **Internal Link Generation** - Update F# rendering functions to use new architecture
3. **Navigation Consistency** - Ensure all internal references follow new URL patterns
4. **Reference Validation** - Verify all updated links resolve correctly

**Actions**:
1. ✅ **Architecture pattern identified** - old vs new URL structure confirmed
2. [ ] **Search and replace** old URL patterns (`/wiki/` → `/resources/wiki/`, etc.)
3. [ ] **Update F# link generation** in rendering functions  
4. [ ] **Validate reference updates** with enhanced link analysis
5. [ ] **Test navigation flow** across updated architecture

### Phase 3: Link Reference Updates
**Objective**: Update internal references to use correct URLs

**Actions**:
1. [ ] Update weekly summary files with correct content-type URLs
2. [ ] Search and replace all `/feed/[slug]` internal references
3. [ ] Update any hardcoded links in content files
4. [ ] Validate all internal links point to existing content

### Phase 4: URL Structure Validation
**Objective**: Ensure `/feed/` directory serves only OPML files

**Actions**:
1. [ ] Verify `/feed/` contains only OPML discovery files
2. [ ] Test content-proximate feeds (`/notes/feed.xml`, etc.)
3. [ ] Ensure build process generates correct URL structure
4. [ ] Document any redirect rules needed for external links

### Phase 5: Navigation Issues Resolution
**Objective**: Fix connection errors in navigation

**Actions**:
1. [ ] Investigate local server routing issues
2. [ ] Verify file existence for failing navigation routes
3. [ ] Test navigation after content migration
4. [ ] Fix any build process issues

## Success Criteria

- [ ] Zero 404 errors from `/feed/[slug]` pattern URLs
- [ ] All content accessible via correct content-type URLs
- [ ] All internal links function correctly  
- [ ] Navigation routes work without connection errors
- [ ] `/feed/` directory contains only OPML discovery files
- [ ] Content-proximate feeds functional
- [ ] Build process completes without errors

## Architecture Compliance

**URL Realignment Standards**:
- ✅ Content at `/content-type/[slug]/` format
- ✅ OPML discovery files in `/feed/` only
- ✅ Content-proximate feeds at `/content-type/feed.xml`
- ✅ IndieWeb microformats2 preservation
- ✅ Semantic URL structure maintenance

## Risk Assessment

**Low Risk**: 
- Content migration preserves all existing content
- URL structure improves semantic clarity
- Build process remains stable

**Mitigation**:
- Validate each migration step with build testing
- Maintain backup of original structure
- Test all content accessibility after migration

## Next Actions

**Immediate Priority (High Impact)**:
1. **Content Migration**: Start moving 195+ files from `_src/feed/` to `_src/notes/`
2. **Missing Collections**: Create missing `/blogroll`, `/bluesky`, `/youtube` pages  
3. **Tag System Debug**: Investigate connection errors affecting 500+ tag URLs
4. **Production Domain Sync**: Fix 404s on production domain collection pages

**Medium Priority**:
5. **Link Updates**: Update internal references in weekly summaries
6. **Build Testing**: Validate each phase with successful builds  
7. **Wiki Pages**: Create missing wiki index and redirect structure

**Low Priority**:
8. **RSS Feeds**: Create missing `/bluesky.rss` and `/youtube.rss` feeds
9. **Navigation Polish**: Fix remaining navigation connection errors

**Critical Path**: Content migration → Tag system fix → Missing page creation → Link updates

This project addresses 1000+ broken links, with the legacy `/feed/` content migration being the highest impact fix affecting 200+ URLs immediately.
