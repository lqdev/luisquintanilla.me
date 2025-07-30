# Legacy URL Redirect Migration Project

**Project Type**: Architecture Alignment & URL Migration  
**Status**: 🟢 Active  
**Priority**: High (200+ broken internal links)  
**Started**: 2025-07-29

## 📋 **Project Objectives**

### **Primary Goal**
Systematically fix 200+ legacy `/feed/` URLs by implementing proper redirects and content mapping to align with new architecture.

### **Success Criteria**
- [ ] Zero broken internal `/feed/` links
- [ ] All legacy URLs properly redirect to new architecture paths
- [ ] Maintain SEO continuity through proper HTTP redirects
- [ ] Update internal link generation to prevent future legacy references

## 🔍 **Current State Analysis**

### **Broken Link Patterns Identified**
1. **Legacy Feed URLs** (~200 links): `/feed/[slug]` → `/[content-type]/[slug]/`
2. **Missing Social Redirects**: `/github`, `/mastodon`, `/linkedin` → Contact page shortcuts
3. **Missing Images**: QR codes and feed-specific images
4. **Legacy Post References**: Old `.html` extensions and outdated paths

### **Architecture Evolution Context**
- **OLD**: Everything under `/feed/[slug]`
- **NEW**: Content-type based: `/posts/`, `/notes/`, `/responses/`, `/reviews/`, `/resources/[type]/`

## 📋 **Implementation Plan**

### **Phase 1: Content Mapping & Redirect Strategy**
**Duration**: 1-2 hours  
**Objective**: Create systematic mapping of legacy URLs to new architecture

#### **Tasks**:
1. **Extract Legacy URL Patterns**
   - Parse all `/feed/` URLs from link analysis report
   - Group by content type patterns (posts, notes, responses, etc.)
   
2. **Create Content Mapping Database**
   - Map each `/feed/[slug]` to correct `/[content-type]/[slug]/`
   - Identify content type from slug patterns or existing content analysis
   
3. **Design Redirect Strategy**
   - Implement F# redirect logic in routing
   - Consider static redirect files for common patterns
   - Ensure proper HTTP status codes (301 for permanent redirects)

### **Phase 2: F# Redirect Implementation**
**Duration**: 2-3 hours  
**Objective**: Implement server-side redirect handling

#### **Tasks**:
1. **Create Redirect Module**
   ```fsharp
   module Redirects
   
   type RedirectMap = Map<string, string>
   
   let legacyFeedRedirects: RedirectMap = 
       Map [
           "/feed/slug1", "/posts/slug1/"
           "/feed/slug2", "/notes/slug2/"
           // ... comprehensive mapping
       ]
   
   let tryGetRedirect (path: string) : string option =
       legacyFeedRedirects |> Map.tryFind path
   ```

2. **Integrate with Routing**
   - Add redirect checking to main request handler
   - Return 301 redirects for matched legacy URLs
   - Ensure proper URL construction with trailing slashes

3. **Social Media Shortcuts**
   ```fsharp
   let socialRedirects = 
       Map [
           "/github", "https://github.com/lqdev"
           "/mastodon", "https://toot.lqdev.tech/@lqdev"
           "/linkedin", "https://www.linkedin.com/in/lquintanilla01/"
       ]
   ```

### **Phase 3: Content Link Audit & Fix**
**Duration**: 2-3 hours  
**Objective**: Fix internal link generation to prevent future legacy references

#### **Tasks**:
1. **Audit Link Generation Functions**
   - Review all `getPermalink` functions in Views modules
   - Ensure consistent use of new architecture patterns
   - Fix any remaining references to `/feed/` URLs

2. **Update Content References**
   - Search for hardcoded `/feed/` links in content files
   - Update to use proper content-type paths
   - Ensure all internal links use relative paths consistently

3. **Missing Asset Generation**
   - Regenerate missing QR codes for contact page
   - Ensure all referenced images exist in correct locations
   - Update any feed-specific image references

### **Phase 4: Validation & Testing**
**Duration**: 1 hour  
**Objective**: Verify all fixes work correctly

#### **Tasks**:
1. **Re-run Link Analysis**
   - Execute updated link analysis script
   - Verify broken link count reduced to near-zero
   - Confirm redirects working correctly

2. **Manual Testing**
   - Test sample legacy URLs manually
   - Verify social media shortcuts work
   - Confirm proper HTTP status codes

3. **SEO Validation**
   - Ensure redirects use proper 301 status codes
   - Verify no redirect loops created
   - Test that search engines can follow redirects

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

## 📝 **Next Steps**

1. **Immediate**: Begin Phase 1 - Extract and categorize all legacy URLs
2. **Research**: Use grep_search to find all `/feed/` references in codebase
3. **Validation**: Test redirect implementation against sample URLs
4. **Documentation**: Update architecture docs with redirect patterns

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
