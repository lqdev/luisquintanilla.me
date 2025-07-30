# Architecture Reference Fix - Autonomous Implementation Plan

**Date**: 2025-07-29  
**Status**: 🟢 GREEN - Act Immediately  
**Scope**: ~150 URL pattern fixes (old architecture → new architecture)

## Critical Discovery Summary

**Pattern Identified**: Internal references still use old URL patterns while content exists at new architecture locations:

### URL Architecture Evolution
- **OLD**: `/wiki/[name]/` → **NEW**: `/resources/wiki/[name]/`
- **OLD**: `/snippets/[name]/` → **NEW**: `/resources/snippets/[name]/`  
- **OLD**: `/presentations/[name]/` → **NEW**: `/resources/presentations/[name]/`

### Confirmed Working Examples
- ✅ `/resources/wiki/markdig-advanced-extensions/` → 200 OK
- ❌ `/wiki/markdig-advanced-extensions/` → 404 Not Found
- ✅ `/resources/snippets/qr-code-generator/` → 200 OK  
- ❌ `/snippets/qr-code-generator/` → 404 Not Found
- ✅ `/resources/presentations/hello-world/` → 200 OK
- ❌ `/presentations/hello-world/` → 404 Not Found

## Implementation Strategy

### Phase A: Content Reference Search & Replace
**Objective**: Update all internal content references to use new architecture

1. **Search Pattern Analysis**:
   ```bash
   grep -r "/wiki/" _src/
   grep -r "/snippets/" _src/  
   grep -r "/presentations/" _src/
   ```

2. **Replace Operations**:
   ```bash
   # Wiki references
   find _src/ -name "*.md" -exec sed -i 's|/wiki/|/resources/wiki/|g' {} +
   
   # Snippets references  
   find _src/ -name "*.md" -exec sed -i 's|/snippets/|/resources/snippets/|g' {} +
   
   # Presentations references
   find _src/ -name "*.md" -exec sed -i 's|/presentations/|/resources/presentations/|g' {} +
   ```

### Phase B: F# Link Generation Updates
**Objective**: Update rendering functions to generate correct architecture URLs

1. **Identify Link Generation Functions**: Search F# code for hardcoded URL patterns
2. **Update Permalink Functions**: Ensure all content type linking uses new architecture  
3. **Navigation Link Updates**: Update sidebar and navigation link generation
4. **Cross-Reference Functions**: Update any content-to-content linking

### Phase C: Validation & Testing
**Objective**: Verify all reference updates work correctly

1. **Enhanced Link Analysis**: Re-run with architecture pattern fixes
2. **Build Validation**: Ensure site builds successfully with updates
3. **Navigation Testing**: Verify all internal links work correctly
4. **Regression Prevention**: Confirm no existing functionality broken

## Expected Impact

### Before Fix
- ~150 broken internal references pointing to old architecture patterns
- Content exists but inaccessible via internal links
- Navigation inconsistency between new and old URL patterns

### After Fix  
- All internal references use consistent new architecture
- Seamless navigation between related content
- Reduced broken link count from 252 → ~100 (actual missing content only)

## Risk Assessment

**🟢 LOW RISK**:
- Content preservation: No content moved, only references updated
- Functionality preservation: All content remains accessible, just via correct URLs
- Reversibility: Changes are systematic find-replace operations

**Validation Strategy**:
- Build testing after each phase
- Link analysis validation  
- Spot-check navigation flows

## Success Criteria

- [ ] Zero 404s from old architecture URL patterns that have new architecture equivalents
- [ ] Enhanced link analysis shows ~150 fewer broken links  
- [ ] All internal navigation uses consistent new architecture URLs
- [ ] Build process completes without errors
- [ ] No regression in existing functionality

## Autonomous Execution Readiness

This qualifies as **🟢 GREEN (Act Immediately)** because:
- ✅ Clear pattern identified with consistent solution
- ✅ Low risk operation (reference updates, not content changes)  
- ✅ Preserves all functionality while fixing inconsistencies
- ✅ Systematic approach with validation at each step
- ✅ No major architectural decisions required

**Ready for autonomous execution** following proven pattern-fixing methodology.
