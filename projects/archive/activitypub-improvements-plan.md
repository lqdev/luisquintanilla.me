# ActivityPub Improvements Plan

**Branch**: `feature/activitypub-improvements`  
**Date**: January 19, 2026  
**Related PR**: #1831 (Outbox synchronization)

## Executive Summary

This plan addresses three interconnected issues in the ActivityPub implementation:
1. **Architectural inconsistency**: Notes served from Azure Functions directory vs static site structure
2. **Outbox ordering**: Items not in reverse chronological order (ActivityPub spec requirement)
3. **Domain inconsistency**: Mixed usage of `www.lqdev.me` vs `lqdev.me` apex domain

## Problem Analysis

### Issue 1: Static Files in Azure Functions Directory

**Current State**:
- Notes served from `api/data/notes/` directory via Azure Functions endpoint
- Route: `/api/activitypub/notes/{noteId}`
- Function reads JSON files from `api/data/notes/` directory
- **Problem**: Notes are NOT currently being generated to `api/data/notes/`

**Root Cause**:
PR #1831 synchronized outbox data but **individual note files are not being generated at all**. The `ActivityPubBuilder.buildOutbox` only creates the outbox collection file, not individual note JSON files.

**Discovery from Research** (Perplexity analysis):
- Best practice: Serve ActivityPub content from static directory when possible for performance/caching
- Note IDs must be dereferenceable URIs that return the actual object
- Hybrid approach: Static content serving + dynamic endpoints only for genuinely dynamic operations
- Individual notes are perfect candidates for static serving (immutable content)

**Architectural Decision Required**:

**Option A - Full Static (Recommended)**:
- Generate individual note JSON files during F# build to `_public/activitypub/notes/{noteId}.json`
- Serve directly from static directory: `https://lqdev.me/activitypub/notes/{noteId}`
- Configure Azure Static Web Apps to serve with `application/activity+json` Content-Type
- **Pros**: Better performance, CDN caching, simpler architecture, follows best practices
- **Cons**: Requires static web app configuration for Content-Type headers

**Option B - Keep Azure Functions (Not Recommended)**:
- Generate note files to both `_public/api/data/notes/` and sync to `api/data/notes/`
- Keep current Azure Functions endpoint: `/api/activitypub/notes/{noteId}`
- **Pros**: Existing endpoint structure remains unchanged
- **Cons**: Unnecessary complexity, worse performance, additional sync step

### Issue 2: Outbox Not Sorted in Reverse Chronological Order

**Current State**:
```powershell
First 3 dates: 
09/27/2025 18:36:00  # September 2025
09/14/2025 21:50:00
09/14/2025 22:31:00

Last 3 dates:
01/14/2025 19:51:00  # January 2025
01/14/2022 09:10:00  # January 2022
01/02/2023 14:22:00  # January 2023
```

**Problem**: Items are NOT in reverse chronological order. ActivityPub spec REQUIRES reverse chronological (newest first).

**Root Cause Analysis**:

Looking at [ActivityPubBuilder.fs:271-274](c:\\Dev\\website\\ActivityPubBuilder.fs#L271-L274):
```fsharp
let activities = 
    unifiedItems
    |> List.sortByDescending (fun item -> item.Date)  // This SHOULD work
    |> List.map (convertToNote >> convertToCreateActivity)
```

The code attempts to sort by date descending, but dates are stored as **strings**, not DateTimeOffset values. String sorting produces lexicographic ordering, not chronological ordering.

**Example of String Sorting Failure**:
```fsharp
// String dates sort lexicographically, not chronologically
"2025-09-27T18:36:00-05:00"  // Comes before
"2025-01-14T19:51:00-05:00"  // Because "09" > "01" in string comparison
```

**Solution**: Parse dates to DateTimeOffset before sorting:
```fsharp
let activities = 
    unifiedItems
    |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
    |> List.map (convertToNote >> convertToCreateActivity)
```

### Issue 3: Note ID 404 Errors

**Current Problem**:
- Note ID: `https://lqdev.me/api/notes/d97d66cdd6d8caad5219756036458d70`
- Returns: 404 Not Found

**Root Cause**: Two-part problem:
1. **Individual note files not being generated** (see Issue 1)
2. **URL mismatch**: Note IDs use `/api/notes/` but Azure Function route is `/api/activitypub/notes/`

Looking at [api/notes/function.json](c:\\Dev\\website\\api\\notes\\function.json):
```json
"route": "activitypub/notes/{noteId:regex(^[a-f0-9]{32}$)}"
```

The route is `/api/activitypub/notes/` but note IDs in outbox reference `/api/notes/`.

**Generated Note ID** (from ActivityPubBuilder.fs:162):
```fsharp
let generateNoteId (url: string) (content: string) : string =
    let hash = generateHash (url + content)
    sprintf "%s/api/notes/%s" Config.baseUrl hash  // Uses /api/notes/
```

**Mismatch**: `/api/notes/{hash}` ≠ `/api/activitypub/notes/{hash}`

### Issue 4: Domain Inconsistency (www vs apex)

**Current State**:
Mixed usage throughout codebase:
- `www.lqdev.me` - Found in 20+ locations (GenericBuilder.fs, Builder.fs, etc.)
- `lqdev.me` - Used in ActivityPubBuilder.fs Config module

**ActivityPub Standards**:
- Note IDs use: `https://lqdev.me/api/notes/{hash}`
- Actor URI: `https://lqdev.me/api/actor`
- Outbox URI: `https://lqdev.me/api/outbox`

**RSS Feeds use**:
- Posts: `https://www.lqdev.me/posts/{filename}`
- Notes: `https://www.lqdev.me/notes/{filename}`

**Impact**: 
- Inconsistent federation behavior
- URL resolution issues
- SEO implications (duplicate content signals)
- User confusion

**Decision Required**: Choose ONE canonical domain for all content URLs.

**Recommendation**: Use apex domain `lqdev.me` everywhere:
- Simpler, cleaner URLs
- Modern web standard (www subdomain increasingly unnecessary)
- Already used in ActivityPub implementation
- Easier to remember and type

**Migration Strategy**:
1. Choose `lqdev.me` as canonical domain
2. Update all URL generation to use apex domain
3. Configure `www.lqdev.me` → `lqdev.me` redirects in Azure Static Web Apps
4. Update existing content references in future builds

## Proposed Solutions

### Solution 1: Move to Full Static Architecture (Recommended)

**Phase 1: Generate Individual Note Files**

Enhance `ActivityPubBuilder.fs` to generate individual note JSON files:

```fsharp
/// Build individual note files for static serving
let buildNotes (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Generating individual ActivityPub note files..."
    
    let notesDir = Path.Combine(outputDir, "activitypub", "notes")
    Directory.CreateDirectory(notesDir) |> ignore
    
    let notes = 
        unifiedItems
        |> List.map convertToNote
    
    for note in notes do
        // Extract hash from note ID
        let noteId = note.Id.Split('/') |> Array.last
        let notePath = Path.Combine(notesDir, $"{noteId}.json")
        let json = JsonSerializer.Serialize(note, jsonOptions)
        File.WriteAllText(notePath, json)
    
    printfn "  ✅ Generated %d ActivityPub note files" notes.Length
```

**Phase 2: Update Note ID Generation**

Update note ID format to match static URL structure:

```fsharp
let generateNoteId (url: string) (content: string) : string =
    let hash = generateHash (url + content)
    sprintf "%s/activitypub/notes/%s" Config.baseUrl hash  // Changed from /api/notes/
```

**Phase 3: Configure Azure Static Web Apps**

Update `staticwebapp.config.json`:

```json
{
  "routes": [
    {
      "route": "/activitypub/notes/*",
      "headers": {
        "Content-Type": "application/activity+json"
      }
    }
  ],
  "mimeTypes": {
    ".json": "application/activity+json"
  }
}
```

**Phase 4: Remove Azure Functions Notes Endpoint**

- Delete `api/notes/` directory and function
- Remove from deployment
- Update documentation

### Solution 2: Fix Chronological Ordering

**Simple Fix** in `ActivityPubBuilder.fs`:

```fsharp
let buildOutbox (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Converting %d items to ActivityPub format..." unifiedItems.Length
    
    // Convert all items to Create activities (reverse chronological)
    let activities = 
        unifiedItems
        |> List.sortByDescending (fun item -> 
            try DateTimeOffset.Parse(item.Date)
            with | _ -> DateTimeOffset.MinValue)  // Fallback for parse errors
        |> List.map (convertToNote >> convertToCreateActivity)
    
    // ... rest of function
```

**Validation**: After fix, verify:
```powershell
$outbox = Get-Content _public/api/data/outbox/index.json | ConvertFrom-Json
$dates = $outbox.orderedItems | ForEach-Object { [DateTimeOffset]::Parse($_.published) }
$sorted = $dates | Sort-Object -Descending
($dates -eq $sorted) -eq $true  # Should return True
```

### Solution 3: Domain Consistency - Use Apex Domain Everywhere

**Centralize Domain Configuration**:

Create `Config.fs` module for shared configuration:

```fsharp
module Config

let canonicalDomain = "https://lqdev.me"

let makeUrl (path: string) =
    let cleanPath = path.TrimStart('/')
    sprintf "%s/%s" canonicalDomain cleanPath
```

**Update All Builders**:

1. **GenericBuilder.fs**: Replace hardcoded `www.lqdev.me` with `Config.makeUrl`
2. **Builder.fs**: Update RSS feed URL generation
3. **ActivityPubBuilder.fs**: Already uses `lqdev.me` (verify consistency)
4. **TextOnlyBuilder.fs**: Update any hardcoded URLs

**Azure Static Web Apps Redirect Configuration**:

Add to `staticwebapp.config.json`:

```json
{
  "routes": [
    {
      "route": "/*",
      "headers": {
        "Strict-Transport-Security": "max-age=31536000; includeSubDomains"
      }
    }
  ],
  "globalHeaders": {
    "Link": "<https://lqdev.me>; rel=\"canonical\""
  }
}
```

**Note**: Azure Static Web Apps automatically handles www → apex redirects when custom domain is configured. Verify in Azure portal.

## Implementation Plan

### Phase 1: Fix Critical Issues (This PR)

**Estimated Effort**: 4-6 hours

1. ✅ Create feature branch
2. ✅ Research best practices
3. ✅ Analyze root causes
4. ✅ Create comprehensive plan
5. **Fix chronological ordering** (ActivityPubBuilder.fs)
6. **Implement individual note generation** (ActivityPubBuilder.fs)
7. **Update note ID format** to match new static structure
8. **Create Config module** for domain consistency
9. **Update URL generation** across all builders
10. **Configure staticwebapp.config.json** for proper Content-Type headers
11. **Update build integration** in Program.fs
12. **Test locally** and validate fixes
13. **Update documentation**

### Phase 2: Architecture Migration (Follow-up PR)

**Estimated Effort**: 2-3 hours

1. **Remove Azure Functions notes endpoint**
2. **Update workflow** to remove note sync step (no longer needed)
3. **Verify production deployment**
4. **Update API documentation**

### Phase 3: Domain Migration Verification (No Code Changes)

**Estimated Effort**: 1 hour

1. **Verify www → apex redirects** in Azure portal
2. **Test federation** with new URLs
3. **Update external documentation** if needed

## Success Criteria

### Functional Requirements
- ✅ Individual note files generated and accessible
- ✅ Note IDs dereferenceable (return 200 with correct JSON)
- ✅ Outbox items in strict reverse chronological order (newest first)
- ✅ All URLs use consistent domain (apex domain)
- ✅ Content-Type headers correct for all ActivityPub endpoints

### Performance Requirements
- ✅ Note files served from CDN/static cache
- ✅ Response times < 100ms for cached content
- ✅ No unnecessary Azure Functions invocations

### Federation Requirements
- ✅ Mastodon/Pleroma can dereference all note IDs
- ✅ Outbox pagination works correctly
- ✅ Follow/unfollow workflows unaffected

## Testing Strategy

### Local Testing
```powershell
# Build site
dotnet run

# Verify note files generated
Test-Path _public/activitypub/notes/*.json

# Check note count
(Get-ChildItem _public/activitypub/notes/*.json).Count  # Should match outbox totalItems

# Verify chronological order
$outbox = Get-Content _public/api/data/outbox/index.json | ConvertFrom-Json
$dates = $outbox.orderedItems | ForEach-Object { [DateTimeOffset]::Parse($_.published) }
$sorted = $dates | Sort-Object -Descending
$dates[0] -eq $sorted[0]  # Should be true (newest first)

# Sample note structure
Get-Content _public/activitypub/notes/d97d66cdd6d8caad5219756036458d70.json | ConvertFrom-Json
```

### Production Testing
```bash
# After deployment
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/activitypub/notes/d97d66cdd6d8caad5219756036458d70"

# Should return 200 with note JSON

# Verify outbox ordering
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/outbox" | jq '.orderedItems[0:3] | .[].published'

# Dates should be in descending order
```

### Federation Testing
- Follow account from Mastodon instance
- Verify note links in outbox work
- Check that note IDs resolve correctly
- Confirm timeline displays properly

## Risk Assessment

### Low Risk
- Chronological ordering fix (simple string → date parsing)
- Domain configuration centralization (no URL changes yet)

### Medium Risk
- Individual note file generation (new functionality, needs testing)
- Note ID format change (affects federation, requires coordination)

### High Risk
- Removing Azure Functions endpoint (breaking change for cached references)
- Domain migration (SEO implications, requires careful redirect configuration)

## Rollback Plan

### If Note Generation Fails
- Comment out note generation in Program.fs
- Keep outbox-only generation
- Notes return 404 temporarily (non-breaking)

### If Chronological Fix Has Issues
- Revert to string sorting
- Document as known issue
- Plan separate fix

### If Domain Changes Break Federation
- Revert URL changes
- Keep configuration module for future use
- Plan gradual migration

## Documentation Updates

### Files to Update
- [x] `projects/active/activitypub-improvements-plan.md` (this file)
- [ ] `api/ACTIVITYPUB.md` - Document note serving architecture
- [ ] `docs/activitypub/implementation-status.md` - Update with fixes
- [ ] `changelog.md` - Add entry for improvements
- [ ] README.md - Update ActivityPub section if needed

### New Documentation to Create
- [ ] `docs/activitypub/static-notes-architecture.md` - Explain note serving approach
- [ ] `docs/activitypub/domain-consistency-guide.md` - Document URL patterns

## Related Issues & PRs

- PR #1831: Outbox synchronization fix
- Related to Phase 3 ActivityPub implementation
- Builds on deployment architecture from PR #1831

## Research Citations

Key insights from Perplexity research on ActivityPub best practices:
- Static file serving for immutable content (notes, outbox) maximizes performance
- Note IDs must be dereferenceable URIs per ActivityPub spec
- Hybrid architecture: static content + dynamic inbox/federation
- Reverse chronological ordering mandatory for OrderedCollections
- Content-Type headers critical for federation compatibility

## Next Steps

1. **Review this plan** and get approval on architectural decisions
2. **Implement Phase 1** fixes in current branch
3. **Test thoroughly** before merging
4. **Schedule Phase 2** for follow-up PR
5. **Monitor federation** after deployment

---

**Questions for Review**:
1. Approve static file serving approach for notes?
2. Approve apex domain (`lqdev.me`) as canonical?
3. Approve note ID format change to `/activitypub/notes/`?
4. Any concerns about breaking changes?
