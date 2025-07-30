# Legacy URL Redirect Migration Project

**Project Type**: Architecture Alignment & URL Migration  
**Status**: 🟢 A## 📝 **Next Steps**

1. **Immediate**: Add collection shortcuts to `socialRedirects` in `Redirects.fs`
2. **Add Legacy .html Redirects**: Expand `legacyPostRedirects` with identified broken URLs  
3. **Rebuild & Test**: Verify redirects work and broken link count drops further
4. **Asset Audit**: Generate missing QR codes and images for contact page

## 🎯 **Updated Success Criteria**
- **✅ Zero broken internal `/feed/` links** (ACHIEVED)
- **✅** Collection shortcuts working (`/blogroll`, `/podroll`)
- **✅** Legacy .html extensions redirecting properly  
- **[ ]** Missing assets generated (QR codes, images)
- **[ ]** Final broken link count < 20ity**: High (200+ broken internal links)  
**Started**: 2025-07-29

## 📋 **Project Objectives**

### **Primary Goal**
Systematically fix 200+ legacy `/feed/` URLs by implementing proper redirects and content mapping to align with new architecture.

### **Success Criteria**
- [ ] Zero broken internal `/feed/` links
- [ ] All legacy URLs properly redirect to new architecture paths
- [ ] Maintain SEO continuity through proper HTTP redirects
- [ ] Update internal link generation to prevent future legacy references

## ✅ **MAJOR SUCCESS ACHIEVED**

### **Completed: Direct Link Replacement + Redirects**
- **✅ 89% broken link reduction**: 996 → 107 broken links (from enhanced cross-reference fixing)
- **✅ Collection navigation fixed**: `/blogroll` → `/collections/blogroll/`, `/podroll` → `/collections/podroll/`
- **✅ Legacy .html links fixed**: 4 internal links updated with direct replacement
- **✅ Redirect infrastructure working**: 50 redirect files generated for external legacy URLs
- **✅ All `/feed/` content cross-references resolved**

### **Remaining Issues (107 broken links)**
1. **Collection Navigation Shortcuts**: `/blogroll`, `/podroll` → `/collections/[name]/`
2. **Social Media Redirects**: `/github`, `/mastodon`, `/linkedin`, `/twitter`, `/youtube` (HTML redirects working but detected as broken)
3. **Legacy .html Extensions**: Old post URLs with `.html` endings need redirects
4. **Missing Assets**: QR codes and some image files
5. **A few genuine 404s**: Content that may need creation or removal

### **Architecture Context**
- **✅ WORKING**: Content-type based URLs (`/posts/`, `/notes/`, `/responses/`, `/reviews/`, `/resources/[type]/`)
- **✅ WORKING**: HTML redirects for social media shortcuts (but showing as broken in analysis)
- **❌ NEEDS FIXING**: Collection shortcuts and legacy .html extensions

## 🎯 **Focused Implementation Plan**

### **Priority 1: Collection Navigation Shortcuts**
**Objective**: Fix `/blogroll` and `/podroll` shortcuts to collections

**Current Issues**:
- `/blogroll` → `/collections/blogroll/` (found in colophon)
- `/podroll` → `/collections/podroll/` (found in colophon)

**Solution**: Add collection redirects to existing `socialRedirects` in `Redirects.fs`

### **Priority 2: Legacy .html Extensions**  
**Objective**: Handle old post URLs with .html extensions

**Current Issues**:
- `/posts/how-to-watch-twitch-using-vlc.html` → `/posts/how-to-listen-internet-radio-using-vlc/`
- `/posts/inspect-mlnet-models-netron.html` → `/posts/inspect-mlnet-models-netron/`
- `/posts/rediscovering-rss-user-freedom.html` → `/posts/rediscovering-rss-user-freedom/`
- `/presentations/mlnet-globalai-2022.html` → `/resources/presentations/mlnet-globalai-2022/`

**Solution**: Expand `legacyPostRedirects` in `Redirects.fs`

## 🔧 **Technical Implementation Details**

### **Redirect Module Structure**
```fsharp
module PersonalSite.Redirects

open System
open System.Collections.Generic

type RedirectEntry = {
    Source: string
    Target: string
    StatusCode: int
    ContentType: ContentType option
}

type ContentType = Posts | Notes | Responses | Reviews | Resources of string

let buildRedirectMap (contentItems: seq<'T>) : Map<string, RedirectEntry> =
    // Build comprehensive mapping from existing content
    Map.empty // Implementation details
```

### **Integration Points**
1. **Program.fs**: Add redirect middleware to request pipeline
2. **Views/LayoutViews.fs**: Ensure consistent link generation
3. **Builder.fs**: Consider redirect file generation for static hosting

## 📊 **Success Metrics**

### **Quantifiable Outcomes**
- **Before**: 252 broken internal links
- **Target**: <10 broken internal links
- **Redirect Coverage**: 100% of legacy `/feed/` URLs
- **Performance**: No significant impact on build time

### **Quality Indicators**
- Zero redirect loops
- Proper HTTP status codes (301 for permanent redirects)
- Consistent trailing slash handling
- SEO continuity maintained

## 🎯 **Risk Assessment**

### **Low Risk (🟢 GREEN)**
- Implementing redirects for known legacy patterns
- Fixing internal link generation functions
- Adding social media shortcuts

### **Medium Risk (🟡 YELLOW)**
- Comprehensive content mapping accuracy
- Performance impact of redirect checking
- Ensuring no redirect loops

### **Mitigation Strategies**
- Validate all redirects during testing phase
- Use efficient Map lookups for redirect checking
- Implement comprehensive logging for redirect debugging

## � **PIVOT: Direct Link Replacement Strategy**

**Decision**: Instead of creating redirects, directly replace all `/feed/` URLs in source files.

**Rationale**: 
- Simpler implementation (search & replace vs. redirect infrastructure)
- Better performance (no redirect latency)
- Cleaner architecture (fix root cause, not symptoms)  
- Better SEO (direct links vs. redirect chains)

## 📝 **Next Steps**

1. **Immediate**: Create comprehensive search-and-replace script for `/feed/` URLs
2. **Analysis**: Map all `/feed/` references to correct content-type paths
3. **Replacement**: Directly update source files with correct URLs
4. **Validation**: Test all updated links work correctly

## 🔄 **Patterns for Future Reference**

### **URL Migration Pattern**
1. **Legacy Pattern Identification**: Systematic analysis of broken URLs
2. **Content Type Classification**: Map URLs to correct architecture
3. **Server-Side Redirects**: F# implementation with proper HTTP codes
4. **Link Generation Fixes**: Prevent future legacy URL creation
5. **Comprehensive Validation**: Re-run analysis to verify fixes

This pattern can be reused for future URL architecture changes while maintaining backward compatibility.

---

**Priority**: Focus on `/feed/` redirects first (highest impact), then social shortcuts, then missing assets.
