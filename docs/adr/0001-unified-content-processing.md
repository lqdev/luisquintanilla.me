# Unified Content Processing with GenericBuilder Pattern

- **Status**: Accepted
- **Date**: 2025-01-01 (documented 2026-02-01)
- **Decision-makers**: Luis Quintanilla
- **Consulted**: GitHub Copilot

## Context and Problem Statement

The site has 9+ content types (posts, notes, responses, snippets, wiki, presentations, media, albums, playlists), each requiring:
- YAML frontmatter parsing
- Markdown processing
- HTML generation
- RSS feed generation
- Tag extraction

The original implementation had separate, repetitive functions for each content type, leading to code duplication and maintenance burden.

## Decision Drivers

- Code duplication across content type processors
- Difficulty adding new content types
- Inconsistent behavior between content types
- Maintenance overhead when updating processing logic
- Need for 1,187+ RSS feeds (content types + tags + unified)

## Considered Options

1. **Continue with separate functions per content type**
2. **Generic builder pattern with ITaggable interface**
3. **External static site generator (Hugo, Astro, etc.)**

## Decision Outcome

**Chosen option**: "Generic builder pattern with ITaggable interface", because it provides type-safe unified processing while leveraging F#'s strong type system and maintaining full control over the generation process.

### Consequences

**Good:**
- 445+ lines of legacy code removed
- Single processing pipeline for all content types
- Consistent RSS feed generation across all types
- Easy to add new content types
- Build times improved from ~12s to ~1.3s (89% improvement)
- Type-safe tag extraction via ITaggable interface

**Bad:**
- Higher initial learning curve for GenericBuilder pattern
- More complex type system

### Confirmation

- All content types successfully migrated
- RSS feeds validate correctly
- No regression in output quality
- Build performance improved significantly

## Implementation Details

The pattern consists of:

1. **Domain.fs**: Core types and ITaggable interface
2. **GenericBuilder.fs**: Unified AST-based content processors
3. **Builder.fs**: High-level orchestration functions
4. **Services/Tag.fs**: Centralized tag processing

### ITaggable Interface

```fsharp
type ITaggable =
    abstract Tags: string array
    abstract Title: string
    abstract Date: string
    abstract FileName: string
```

### Migration Process (8 phases)

1. Books/Reviews migration
2. Posts migration
3. Notes migration
4. Snippets migration
5. Wiki migration
6. Responses migration
7. Presentations migration
8. Albums migration

## More Information

- Migration history documented in `projects/archive/`
- Feature flag pattern used for safe migrations (see ADR-0004)
- Changelog entries for each migration phase
