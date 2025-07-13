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

### Architecture Consistency
- [ ] Follow proven 4-phase migration pattern (7 consecutive successes)
- [ ] Maintain external URL structure and behavior
- [ ] Integrate with existing tag system
- [ ] Use unified RSS feed architecture
- [ ] Apply established custom block rendering patterns

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
mainimage: /images/fall-mountains-main.jpg
published_date: 2023-10-15
images:
  - imagepath: /images/fall-mountains-1.jpg
    description: "Mountain vista in autumn"
    alttext: "Fall colors on mountain landscape"
```

**Required Transformation**:
Convert photo metadata to :::media blocks:
```markdown
:::media
type: image
src: /images/fall-mountains-1.jpg
alt: Fall colors on mountain landscape
caption: Mountain vista in autumn
:::
```

### Implementation Strategy

**Phase 1: Domain Enhancement**
- Add `Tags` field to `AlbumDetails` with YamlMember alias
- Implement Album ITaggable helper functions in Domain.fs
- Create validation test scripts for enhanced domain

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
- Support for image content type
- Automatic responsive image rendering
- Metadata preservation (alt text, captions)
- Integration with existing CSS/styling

**Content Conversion Logic**:
```fsharp
// Convert AlbumImage array to :::media blocks
let convertAlbumImagesToMediaBlocks (images: AlbumImage[]) =
    images
    |> Array.map (fun img -> 
        sprintf ":::media\ntype: image\nsrc: %s\nalt: %s\ncaption: %s\n:::" 
            img.ImagePath img.AltText img.Description)
    |> String.concat "\n\n"
```

### RSS Feed Architecture

Albums will follow established feed patterns:
- Individual album pages: `/albums/{filename}/`
- Album RSS feed: `/feed/albums/rss.xml`
- Album HTML index: `/feed/albums/index.html`
- Integration with main feed aggregation
- XML validation and proper channel structure

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
