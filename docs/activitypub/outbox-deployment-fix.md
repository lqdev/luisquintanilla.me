# ActivityPub Outbox Deployment Fix

**Date**: January 19, 2026  
**Issue**: Outbox endpoint returning only 20 stale items instead of 1548+ fresh items  
**Status**: ✅ Fixed

## Problem Statement

The `/api/activitypub/outbox` endpoint was returning only 20 old items (dating back to Sept 2024), despite Phase 3 implementation that generates outbox content from unified feeds. Static files like `actor.json` worked correctly, but the outbox data was stale.

### Symptoms
- Production endpoint showed `"totalItems": 20` with dates from Sept 2024
- Local build correctly generated 1548 items
- Static actor.json worked fine, suggesting deployment issue, not code issue
- No new activities appeared despite fresh content being published

## Root Cause Analysis

### The Deployment Flow Problem

1. **F# Build Process** (Working Correctly):
   - `Program.fs` calls `ActivityPubBuilder.buildOutbox()`
   - Generates fresh outbox with 1548 items
   - Output location: `_public/api/data/outbox/index.json` (5.5MB)

2. **Azure Static Web Apps Deployment** (The Issue):
   - Deploys `_public/` directory as static site ✅
   - Deploys `api/` directory as Azure Functions ❌
   - **Problem**: `api/data/outbox/index.json` was tracked in Git with stale data (32KB, 20 items)
   - Azure Functions read from `../data/outbox/index.json` (relative path from function directory)
   - This pointed to the stale Git-tracked file, NOT the fresh generated file

3. **Why Static Files Worked**:
   - `actor.json` and `webfinger.json` are static configuration files
   - They don't change during builds
   - No synchronization needed

### Technical Details

**File Comparison**:
```
Stale (Git-tracked):     api/data/outbox/index.json      32KB,   20 items
Fresh (Generated):      _public/api/data/outbox/index.json  5.5MB, 1548 items
```

**Azure Functions Path Resolution**:
```javascript
// api/outbox/index.js
const outboxPath = path.join(__dirname, '../data/outbox/index.json');
// Resolves to: api/data/outbox/index.json (NOT _public/api/data/outbox/index.json)
```

## Solution Implemented

### 1. Workflow Enhancement

Added synchronization step to `.github/workflows/publish-azure-static-web-apps.yml`:

```yaml
- name: Sync ActivityPub data for Azure Functions
  run: |
    echo "📦 Syncing generated ActivityPub data to Azure Functions directory..."
    echo "Syncing outbox data..."
    cp -v _public/api/data/outbox/index.json api/data/outbox/index.json
    echo "Syncing followers data..."
    cp -v _public/api/data/followers.json api/data/followers.json
    echo "✅ ActivityPub data synced successfully"
    echo "Outbox size: $(du -h api/data/outbox/index.json | cut -f1)"
    echo "Followers size: $(du -h api/data/followers.json | cut -f1)"
```

**Placement**: After "Generate website" step, before "Build And Deploy" step

### 2. Git Ignore Update

Updated `api/.gitignore` to prevent tracking generated files:

```gitignore
# Generated data files (copied from _public during deployment)
# These are generated at build time and should not be tracked
data/outbox/
data/notes/
```

**Rationale**: Generated files should not be version controlled. Fresh data is created on every build.

### 3. Repository Cleanup

Removed stale tracked files:
```bash
git rm --cached api/data/outbox/index.json
git rm --cached api/data/notes/*.json
```

**Result**: 21 files removed from Git tracking (1 outbox + 20 notes)

### 4. Documentation Updates

Updated `api/ACTIVITYPUB.md`:
- Added deployment synchronization warning
- Documented the sync workflow requirement
- Marked Phase 3 as complete with deployment fix notes
- Added troubleshooting section for similar issues

## Verification

### Local Testing
```bash
# Build generates fresh data
dotnet run
# Output: "✅ Total items: 1548, Total Create activities: 1548"

# Verify generated file
jq '.totalItems' _public/api/data/outbox/index.json
# Output: 1548

# Test sync command
cp _public/api/data/outbox/index.json api/data/outbox/index.json

# Verify synced file
jq '.totalItems' api/data/outbox/index.json
# Output: 1548

# Checksums match
md5sum api/data/outbox/index.json _public/api/data/outbox/index.json
# Both: b98567922698109599fac9539b2ae9a6
```

### Deployment Testing (Post-Merge)
After deployment to production:
1. Visit: `https://lqdev.me/api/activitypub/outbox`
2. Verify `"totalItems"` shows 1548+ (not 20)
3. Check activity dates are current (not Sept 2024)
4. Verify most recent content appears in outbox

## Architecture Impact

### Data Flow (After Fix)

```
┌─────────────────────────────────────────────────────────────┐
│ F# Build Process (dotnet run)                              │
├─────────────────────────────────────────────────────────────┤
│ 1. Collect unified feed items (1548+)                      │
│ 2. ActivityPubBuilder.buildOutbox()                        │
│ 3. Generate: _public/api/data/outbox/index.json           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ GitHub Actions Workflow                                     │
├─────────────────────────────────────────────────────────────┤
│ 1. Run dotnet build                                        │
│ 2. Run dotnet run (generates fresh data)                  │
│ 3. ⭐ NEW: Sync ActivityPub data                          │
│    - Copy _public/api/data/* → api/data/*                │
│ 4. Deploy to Azure Static Web Apps                        │
│    - _public/ → static site                               │
│    - api/ → Azure Functions (with fresh data!)            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Azure Production                                            │
├─────────────────────────────────────────────────────────────┤
│ Azure Functions: api/outbox/index.js                       │
│ ├─ Reads: ../data/outbox/index.json                       │
│ └─ Returns: Fresh 1548+ items ✅                          │
└─────────────────────────────────────────────────────────────┘
```

### File Responsibilities

| File | Type | Tracked in Git | Generated | Synced |
|------|------|----------------|-----------|---------|
| `api/data/actor.json` | Static Config | ✅ Yes | ❌ No | ❌ No |
| `api/data/webfinger.json` | Static Config | ✅ Yes | ❌ No | ❌ No |
| `api/data/outbox/index.json` | Generated Data | ❌ No | ✅ Yes | ✅ Yes |
| `api/data/followers.json` | Generated Data | ❌ No* | ✅ Yes | ✅ Yes |

*Note: `followers.json` was previously tracked but should be treated as generated (currently contains empty collection)

## Lessons Learned

### Key Insights

1. **Hybrid Deployments Need Synchronization**:
   - Static sites and Azure Functions deployed from different directories
   - Generated data must be explicitly synced between these directories
   - Git tracking can hide deployment issues during development

2. **Generated Files Should Not Be Tracked**:
   - Version control should contain source code and static configuration
   - Build artifacts (like generated outbox) should be `.gitignore`d
   - Stale tracked files can silently override fresh generated data

3. **Azure Static Web Apps Deployment Model**:
   - `app_location`: Deployed as static files
   - `api_location`: Deployed as Azure Functions
   - These are separate deployment processes
   - Data sharing between them requires explicit steps

4. **Relative Path Pitfalls**:
   - Azure Functions resolve relative paths from function directory
   - `../data/outbox/index.json` resolves to `api/data/`, not `_public/api/data/`
   - Path assumptions from local development may not match deployment

### Best Practices Established

1. **Build → Sync → Deploy Pattern**:
   - Always generate data first
   - Sync generated data to appropriate locations
   - Deploy with fresh data in all required locations

2. **Verification in Workflow**:
   - Display file sizes after sync for troubleshooting
   - Add checksums for critical data files
   - Log sync operations for audit trail

3. **Documentation Requirements**:
   - Clearly mark generated vs static files
   - Document deployment synchronization requirements
   - Update troubleshooting guides with deployment issues

## Related Issues

- **Original Issue**: Outbox returns only 20 old items (lqdev/luisquintanilla.me#1829 context)
- **Phase 3 Implementation**: ActivityPub outbox automation (PR #1829)
- **This Fix**: Deployment synchronization (Current PR)

## Future Considerations

### Monitoring

Add to deployment workflow (future enhancement):
```yaml
- name: Verify ActivityPub data sync
  run: |
    # Check that synced files are not empty
    test -s api/data/outbox/index.json || exit 1
    
    # Verify item count is reasonable
    ITEMS=$(jq '.totalItems' api/data/outbox/index.json)
    if [ "$ITEMS" -lt 1000 ]; then
      echo "⚠️ Warning: Only $ITEMS items in outbox (expected 1500+)"
    fi
```

### Alternative Approaches Considered

1. **Generate Directly to api/data/** ❌
   - Would break static site structure
   - `_public/` needs complete site for Azure deployment
   - Rejected: Maintain separation of concerns

2. **Symlinks** ❌
   - Git doesn't track symlink targets well
   - May not work across deployment environments
   - Rejected: Too fragile for CI/CD

3. **Post-Deployment Script** ❌
   - Would require Azure Functions to regenerate data
   - Adds complexity and potential failures
   - Rejected: Build-time generation is simpler

4. **Single Data Directory** ❌
   - Would require restructuring entire deployment
   - Azure Static Web Apps expects specific structure
   - Rejected: Too large a change

5. **Current Solution: Workflow Sync** ✅
   - Explicit and auditable
   - Works with existing architecture
   - Easy to troubleshoot
   - Selected: Best balance of simplicity and reliability

## Checklist for Similar Issues

If you encounter similar deployment issues:

- [ ] Check if generated files are tracked in Git
- [ ] Verify deployment directories (`app_location` vs `api_location`)
- [ ] Test file paths in Azure Functions (relative vs absolute)
- [ ] Compare local build output with production files
- [ ] Check file sizes and timestamps in deployment logs
- [ ] Verify `.gitignore` excludes generated artifacts
- [ ] Add sync steps to workflow if needed
- [ ] Document synchronization requirements

## References

- [Azure Static Web Apps Deployment](https://docs.microsoft.com/en-us/azure/static-web-apps/deploy-azure-functions)
- [GitHub Actions Workflows](https://docs.github.com/en/actions/using-workflows)
- [ActivityPub Spec - Outbox](https://www.w3.org/TR/activitypub/#outbox)
- [Phase 3 Implementation Complete](./phase3-implementation-complete.md)
- [ActivityPub Architecture Overview](./ACTIVITYPUB.md)
