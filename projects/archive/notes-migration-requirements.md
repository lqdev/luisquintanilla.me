# Notes Migration Requirements

**Project**: Website Architecture Upgrade - Notes Processor  
**Target**: Migrate notes/feed content to AST-based processing system  
**Duration**: 1-2 days  

## Background Context

Notes represent the microblog content type of the personal website, containing short-form posts, status updates, and social media-style content. Currently processed using legacy string-based functions that need to be migrated to the unified AST-based infrastructure.

## Functional Requirements

### Core Functionality Preservation
- **FR1**: All existing note functionality must be preserved exactly
- **FR2**: RSS feed generation must continue working without changes
- **FR3**: Note index page and individual note pages must generate identically
- **FR4**: Chronological ordering and pagination must be maintained
- **FR5**: All note metadata (date, title, content) must be preserved

### New Capabilities
- **FR6**: Enable custom block support for rich note content (media, reviews, venues)
- **FR7**: Support IndieWeb microformats for microblog content
- **FR8**: Maintain compatibility with existing note format and structure
- **FR9**: Enable unified tag processing through ITaggable interface
- **FR10**: Support future note enhancements through AST-based processing

## Technical Requirements

### Architecture Compliance
- **TR1**: Follow GenericBuilder pattern used by all migrated content types
- **TR2**: Implement feature flag pattern (NEW_NOTES=true/false) for safe migration
- **TR3**: Use AST parsing instead of string manipulation
- **TR4**: Maintain zero regression during migration process
- **TR5**: Clean removal of legacy code after successful deployment

### Integration Requirements
- **TR6**: Note domain type must implement ITaggable interface
- **TR7**: Notes processor must integrate with existing GenericBuilder infrastructure
- **TR8**: Must coexist cleanly with other content types during migration
- **TR9**: RSS feed generation must use unified feed generation system
- **TR10**: Build process must remain unchanged during migration

## Content Analysis

### Current State
- **Location**: `_src/feed/*.md` (approximately 150+ files)
- **Format**: Markdown with YAML frontmatter
- **Metadata**: `post_type: "note"`, date, title, content
- **Processing**: `loadFeed()` → `buildFeedPage()` → output generation
- **Output**: `/feed/index.html` + RSS feeds + individual note pages

### Target State  
- **Processing**: `loadNotes()` → `NoteProcessor` → `buildNotes()` → output generation
- **Infrastructure**: AST-based parsing with custom block support
- **Features**: Enhanced with custom blocks, ITaggable interface, unified processing
- **Output**: Identical functionality with enhanced capabilities

## Success Criteria

### Migration Validation
- **SC1**: 100% output compatibility between old and new systems
- **SC2**: All 150+ notes process correctly with preserved metadata
- **SC3**: RSS feeds generate identically with proper XML structure
- **SC4**: Note index and individual pages maintain exact formatting
- **SC5**: Zero regression in existing functionality

### Code Quality
- **SC6**: Legacy functions removed after successful migration
- **SC7**: Clean integration with GenericBuilder infrastructure
- **SC8**: Proper error handling and validation
- **SC9**: Comprehensive test coverage for all functionality
- **SC10**: Documentation updated with migration details

### Architecture Goals
- **SC11**: Notes use same AST-based pattern as other content types
- **SC12**: Custom block support enabled for future enhancements
- **SC13**: ITaggable interface implemented for unified tag processing
- **SC14**: Feature flag safely removed after production deployment
- **SC15**: Code quality improvements documented and measured

## Risk Assessment

### Low Risk (Mitigated by Proven Pattern)
- **R1**: Migration methodology proven successful for 5 content types
- **R2**: Feature flag pattern provides safe rollback capability
- **R3**: GenericBuilder infrastructure stable and tested
- **R4**: Comprehensive validation scripts available

### Medium Risk (Manageable)
- **R5**: Large number of note files (150+) increases validation complexity
- **R6**: Microblog-specific features may need special handling
- **R7**: RSS feed compatibility critical for syndication

### Mitigation Strategies
- **M1**: Incremental testing with small batches of notes
- **M2**: Research microblog best practices before implementation
- **M3**: Comprehensive RSS feed validation scripts
- **M4**: Feature flag allows immediate rollback if issues arise

## Dependencies

### Prerequisites (All Complete)
- ✅ GenericBuilder infrastructure implemented and stable
- ✅ AST parsing system functional with custom block support
- ✅ Feature flag infrastructure operational
- ✅ 5 content types successfully migrated using same pattern
- ✅ Test framework and validation scripts available

### External Dependencies
- ✅ Markdig library for AST parsing
- ✅ FSharp.Data for YAML parsing
- ✅ Existing RSS feed infrastructure
- ✅ IndieWeb microformat standards

## Implementation Phases

### Phase 1: Analysis & Domain Enhancement (0.5 days)
- Analyze current note system and processing functions
- Research microblog patterns and IndieWeb standards
- Enhance Note domain type with ITaggable interface
- Create comprehensive test content and validation scripts

### Phase 2: Processor Implementation (0.5 days)
- Implement NoteProcessor in GenericBuilder following proven pattern
- Add buildNotes() function to Builder.fs with feature flag integration
- Integrate NEW_NOTES environment variable in Program.fs
- Validate AST-based processing with test notes

### Phase 3: Migration Validation (0.5 days)
- Create output comparison test scripts for old vs new systems
- Validate 100% compatibility across all 150+ notes
- Test RSS feed generation and note index functionality
- Confirm integration with other content types

### Phase 4: Production Deployment (0.5 days)
- Deploy new processor as default (remove feature flag dependency)
- Remove legacy loadFeed() and buildFeedPage() functions
- Clean up unused imports and dependencies
- Archive project and update documentation

## Testing Strategy

### Validation Approach
- Output comparison testing between old and new systems
- RSS feed XML validation and structure verification
- Integration testing with other content types
- Performance testing with large note dataset
- Feature flag switching validation

### Test Coverage
- Individual note page generation
- Note index page compilation
- RSS feed generation and validity
- Metadata preservation and formatting
- Custom block functionality (if applicable)
- Error handling and edge cases

This requirements document ensures the Notes migration follows the proven pattern while addressing the specific needs of microblog content processing.
