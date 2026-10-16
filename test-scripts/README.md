# Test Scripts

This directory contains validation and testing scripts for the website's core infrastructure components.

## Usage

Run test scripts from the project root directory:

```bash
# Core Infrastructure Tests
dotnet fsi test-scripts\test-ast-parsing.fsx        # AST parsing functionality
dotnet fsi test-scripts\test-phase1b.fsx            # Generic builder and block renderers  
dotnet fsi test-scripts\test-phase1c.fsx            # ITaggable and domain integration

# Comprehensive Validation Tests
dotnet fsi test-scripts\test-comparison-phase1d.fsx # AST vs string parsing comparison
dotnet fsi test-scripts\test-context-validation.fsx # Custom blocks in different contexts
dotnet fsi test-scripts\test-integration-phase1d.fsx # Integration with existing build

# Content Migration Tests
dotnet fsi test-scripts\test-snippets-migration.fsx # Snippets migration validation
dotnet fsi test-scripts\test-wiki-migration.fsx     # Wiki migration validation

# Feature Flag Tests
dotnet fsi test-scripts\test-feature-flags.fsx      # Feature flag system validation
dotnet fsi test-scripts\test-rss-cdata.fsx          # RSS CDATA regression guard
dotnet fsi test-scripts\test-rss-reply-threading.fsx # Reply threading and feed propagation
dotnet fsi test-scripts\test-rss-semantics.fsx       # Five-kind annotations, independent consumer and compatibility
dotnet fsi test-scripts\test-social-discovery.fsx   # SocialSpec discovery artifacts and config
```

## RSS Semantics Compatibility

After `dotnet build`, `test-rss-semantics.fsx` validates the exact synthetic five-item
issue example, real post/note/reply/star/bookmark source fixtures, short-post/long-note
classification, case handling, invalid-target diagnostics and strict policy,
escaping/CDATA, historical and opaque GUIDs with optional permalink attributes,
topic isolation, and controlled type/unified/tag propagation. Its consumer selects
elements by namespace URI without calling the producer mapping. It does not fetch
public feeds or publish synthetic content.

For a fresh prechange processor comparison under identical source inputs, capture
fixtures before modifying/building serializers, then compare after rebuilding:

```powershell
dotnet fsi test-scripts\test-rss-semantics.fsx --capture-fixtures <baseline-directory>
dotnet build
dotnet fsi test-scripts\test-rss-semantics.fsx --baseline-fixtures <baseline-directory>
# After strict site generation, also consume the actual local feed windows:
dotnet fsi test-scripts\test-rss-semantics.fsx --generated-output
```

Capture the generated feeds before changes as well. After `dotnet run -- --strict`,
compare existing items keyed by GUID, preserving membership/order, legacy XML
fields and attributes, CDATA and Markdown; permit only the specified Atom
annotations and valid UTC channel build-time changes. Compare JSON Feed bytes and
BAR ZIP member payloads (ZIP timestamps themselves vary per build). Keep the
existing RSS correctness, CDATA, threading, generated-output, and social-discovery
checks intact. Live XML/reader verification remains gated on separately approved
publication; see `docs\feed-architecture.md`.

## Core Infrastructure Tests

- **`test-ast-parsing.fsx`** - Validates AST parsing, YAML front-matter parsing, and error handling
- **`test-phase1b.fsx`** - Tests generic builder pattern and custom block renderers
- **`test-phase1c.fsx`** - Tests ITaggable interface and domain integration

## Comprehensive Validation Tests

- **`test-comparison-phase1d.fsx`** - Compares new AST-based parsing with existing string-based processing
- **`test-context-validation.fsx`** - Validates custom block parsing and rendering in different contexts (cards, feeds, pages)
- **`test-integration-phase1d.fsx`** - Tests integration with existing build process to ensure no regressions

## Test Content

The `test-content/` directory contains comprehensive test files:
- **`comprehensive-blocks-test.md`** - All custom block types for testing
- **`simple-review-test.md`** - Simple test case for basic validation

## Purpose

These scripts provide:
- Validation that new infrastructure works correctly
- Regression testing during development
- Examples of how to use the new APIs
- Quick feedback on compilation and runtime issues

## Organization

- Scripts are kept separate from main codebase for clarity
- Named with descriptive patterns: `test-[feature/phase].fsx`
- Include both positive and negative test cases
- Provide verbose output for debugging
