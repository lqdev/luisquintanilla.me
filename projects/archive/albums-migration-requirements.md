# Albums Migration Requirements

**Project**: Albums Migration to AST-Based GenericBuilder  
**Date**: 2025-01-13  
**Status**: Active  
**Content Type**: 8/8 (Final Migration)

## Problem Statement

Albums are the final content type requiring migration to the AST-based GenericBuilder pattern. Currently:

- Albums infrastructure exists but is **deliberately disabled** (commented out in Program.fs)
- Has working `loadAlbums` function and `parseAlbum` implementation
- Contains single sample album with photo metadata structure
- Lacks :::media block integration as specified in backlog
- Missing tags field and ITaggable interface implementation
- No RSS feed generation or HTML index pages

## Success Criteria

### Primary Objectives
- [ ] **Phase 1**: Enhance Album domain with tags and ITaggable interface
- [ ] **Phase 2**: Implement AlbumProcessor in GenericBuilder with :::media block support
- [ ] **Phase 3**: Replace disabled build functions with feature flag approach
- [ ] **Phase 4**: Enable production deployment and clean up legacy code

### Technical Requirements
- [ ] Albums use `:::media` blocks exclusively for image rendering
- [ ] Convert photo metadata arrays to :::media block structure
- [ ] Implement AlbumDetails with tags field
- [ ] Add Album ITaggable helper functions
- [ ] Create album processor in GenericBuilder.fs
- [ ] Add `NEW_ALBUMS=true` feature flag support
- [ ] Generate album RSS feeds automatically
- [ ] Create album HTML index pages
- [ ] Support mixed media content types
- [ ] Preserve advanced media metadata
- [ ] **Implement h-entry microformats for albums**
- [ ] **Support webmention integration for album pages**
- [ ] **Add proper semantic markup for accessibility**
- [ ] **Include p-category tags for IndieWeb discovery**
- [ ] **Maintain responsive card layout consistency**

### Architecture Consistency
- [ ] Follow proven 4-phase migration pattern (7 consecutive successes)
- [ ] Maintain external URL structure and behavior
- [ ] Integrate with existing tag system
- [ ] Use unified RSS feed architecture
- [ ] Apply established custom block rendering patterns
- [ ] **Preserve IndieWeb h-entry microformat compatibility**
- [ ] **Maintain webmention endpoint integration**
- [ ] **Follow responsive card layout pattern from responses/notes**

## Technical Approach

### Current State Analysis

**Infrastructure Status**:
- ✅ Album domain types defined (`Album`, `AlbumDetails`, `AlbumImage`)
- ✅ Loading functions implemented (`loadAlbums`, `parseAlbum`)
- ✅ AST parsing functions available (`parseAlbum`, `parseAlbumFromFile`)
- ❌ Build functions commented out (`buildAlbumPage`, `buildAlbumPages`)
- ❌ No tags field in `AlbumDetails`
- ❌ Missing ITaggable implementation
- ❌ No :::media block integration

**Content Structure**:
```yaml
post_type: album
title: Fall Mountains
published_date: 2023-10-15
tags: photography, mountains, autumn
images:
  - imagepath: /images/fall-mountains-1.jpg
    description: "Mountain vista in autumn"
    alttext: "Fall colors on mountain landscape"
```

**Required Transformation**:
Convert to :::media blocks with markdown content support:
```markdown
# Fall Mountains

Beautiful autumn colors captured during a mountain hike.

:::media
type: image
src: /images/fall-mountains-1.jpg
alt: Fall colors on mountain landscape
caption: Mountain vista in autumn
:::

More commentary about the experience and photos...
```

**Key Changes**:
- **URL Structure**: `/media/{filename}/` instead of `/albums/{filename}/`
- **Remove mainimage field**: Display media content directly via :::media blocks
- **Direct :::media conversion**: No preservation of original YAML structure
- **Markdown content support**: Albums can include commentary like other posts
- **Mixed media support**: `/media/` pattern handles all media types
- **No automatic tags**: Manual tagging only
- **Unified tag system**: Album tags integrate with existing tag filtering

### Implementation Strategy

**Phase 1: Domain Enhancement**
- Add `Tags` field to `AlbumDetails` with YamlMember alias
- Remove `MainImage` field from `AlbumDetails` (no longer needed)
- Implement Album ITaggable helper functions in Domain.fs
- Create validation test scripts for enhanced domain
- **Update single `/albums/` link in Views/Partials.fs line 754 to `/media/`**

**URL Migration Status**: ✅ **Only one link to update** - no markdown content or navigation links found

**Phase 2: Processor Implementation**  
- Add `AlbumProcessor` function to GenericBuilder.fs
- Convert `AlbumImage` arrays to :::media block content
- Implement media metadata preservation
- Add album build function to Builder.fs
- Integrate `NEW_ALBUMS` feature flag in Program.fs

**Phase 3: Migration Validation**
- Create output comparison test scripts
- Validate RSS feed generation for albums
- Test album HTML index page creation
- Confirm :::media block rendering
- Verify tag integration and filtering

**Phase 4: Production Deployment**
- Deploy new processor as default
- Remove commented build functions from Program.fs
- Clean up unused album infrastructure
- Archive project and update documentation

### Media Block Integration

**Custom Block Structure**:
Albums will use existing :::media blocks from CustomBlocks.fs:
- **Support for all media types** (images, videos, etc.)
- **Automatic responsive image rendering with lazy loading**
- **u-photo microformat for album images** (IndieWeb compliance)
- **Metadata preservation** (alt text, captions)
- **Integration with existing CSS/styling**
- **Multiple photos in h-entry** (following IndieWeb best practices)
- **No thumbnail generation required**

**Content Conversion Logic**:
```fsharp
// Convert AlbumImage array to :::media blocks with IndieWeb collection pattern
let convertAlbumToMarkdown (album: Album) (existingContent: string) =
    let mediaBlocks = 
        album.Metadata.Images
        |> Array.map (fun img -> 
            // Each :::media block becomes a nested h-entry with u-photo
            sprintf ":::media\ntype: image\nsrc: %s\nalt: %s\ncaption: %s\n:::" 
                img.ImagePath img.AltText img.Description)
        |> String.concat "\n\n"
    
    // Combine any existing markdown content with media blocks
    match existingContent.Trim() with
    | "" -> mediaBlocks
    | content -> sprintf "%s\n\n%s" content mediaBlocks

// IndieWeb microformat structure will render as:
// Parent h-entry (album) containing nested h-entries (each :::media block)
// Preserves photo ordering and per-image metadata per IndieWeb best practices
```

**URL Migration Strategy**:
- **Check existing posts** for `/albums/` links and update to `/media/`
- **Preserve SEO** with proper redirects if needed
- **Update internal navigation** to use `/media/` pattern

### RSS Feed Architecture

Albums will follow established feed patterns with media-focused URLs:
- **Individual album pages**: `/media/{filename}/` (changed from `/albums/`)
- **Album RSS feed**: `/feed/media/rss.xml` 
- **Album HTML index**: `/feed/media/index.html` (lists all albums, no pagination)
- **Media index page**: `/media/` (lists all albums/media content)
- **Integration with main feed aggregation** (album feeds included in main RSS)
- **XML validation and proper channel structure**
- **RSS entries include all images and full album content**

## Implementation Decisions

### Explicit Design Choices
Based on requirements clarification, the following decisions are locked in:

**URL Structure**:
- ✅ Albums use `/media/{filename}/` pattern (not `/albums/`)
- ✅ Media index at `/media/` lists all albums
- ✅ No pagination required for album listings
- ✅ Album feeds included in main RSS aggregation

**Content Strategy**:
- ✅ Remove `mainimage` field from AlbumDetails (display via :::media blocks)
- ✅ No preservation of original YAML structure during migration
- ✅ Direct conversion to :::media blocks with markdown content support
- ✅ Albums support commentary and markdown like other posts

**Tagging & Classification**:
- ✅ No automatic tags generated
- ✅ Album tags integrate with existing unified tag filtering system
- ✅ No separate album tag namespace

**Media & Performance**:
- ✅ Implement lazy loading for album images
- ✅ No thumbnail generation required
- ✅ Support mixed media types through `/media/` pattern

**IndieWeb Compliance**:
- ✅ Album images use u-photo microformat
- ✅ **Multiple photos in single h-entry: Use collection pattern** (parent h-entry with nested child h-entries for each image)
- ✅ **Preserve photo ordering and per-image metadata** through nested hierarchy
- ✅ **Each photo as separate h-entry** with individual captions and attribution
- ✅ Standard POSSE behavior (treat like any other post)
- ✅ **Support POSSE segmentation** for platforms with media limits (e.g., Twitter's 4-photo limit)

**Migration Strategy**:
- ✅ Check for existing `/albums/` links in posts and update to `/media/`
- ✅ No legacy URL compatibility required
- ✅ No validation required for album fields

## Risk Assessment

### Low Risk Factors
- ✅ Proven migration pattern (7 consecutive successes)
- ✅ Existing infrastructure and loading functions
- ✅ Simple content structure (photo metadata)
- ✅ Established :::media block implementation
- ✅ Feature flag safety mechanism

### Potential Challenges
- 📋 Album content conversion from metadata arrays to markdown blocks
- 📋 Ensuring media metadata preservation during transformation
- 📋 Tag field addition requires careful domain enhancement
- 📋 Single sample album limits validation scope

### Mitigation Strategies
- Use proven domain enhancement pattern from books migration
- Create comprehensive test scripts for media conversion
- Validate against sample album before broader implementation
- Follow incremental testing approach with build validation

## Timeline Estimate

**Total Estimated Effort**: 3-4 hours
- **Phase 1** (Domain Enhancement): 45 minutes
- **Phase 2** (Processor Implementation): 90 minutes  
- **Phase 3** (Migration Validation): 60 minutes
- **Phase 4** (Production Deployment): 30 minutes

## Dependencies

- ✅ Responses migration completed successfully
- ✅ AST-based GenericBuilder infrastructure mature
- ✅ Custom :::media blocks implemented
- ✅ ITaggable interface and helper patterns established
- ✅ RSS feed architecture and validation tools available

## Success Metrics

### Code Quality
- Album domain implements ITaggable interface
- AlbumProcessor follows GenericBuilder pattern
- Media metadata fully preserved
- Feature flag integration clean

### Architecture Impact  
- 8/8 content types unified under GenericBuilder
- Complete migration pattern validation
- Consistent RSS feed architecture
- Final content type architecture consolidation

### Validation Criteria
- ✅ Existing album renders identically with new system
- ✅ Album RSS feeds validate and integrate properly
- ✅ Tag filtering includes album content
- ✅ :::media blocks render correctly with responsive images
- ✅ Build process remains stable and fast

---

**Next Steps**: Begin Phase 1 domain enhancement following proven migration methodology.
