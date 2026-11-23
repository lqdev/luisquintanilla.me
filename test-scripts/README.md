# Test Scripts

This directory contains validation and testing scripts for the website's core infrastructure components.

## Usage

Run test scripts from the project root directory:

```bash
# Offline regression tests (CI runs these in validate-site-upgrade.yml)
dotnet fsi test-scripts\test-rss-generated-output.fsx # Generated feed output
dotnet fsi test-scripts\test-rss-correctness.fsx      # RSS correctness
dotnet fsi test-scripts\test-rss-cdata.fsx            # RSS CDATA regression guard
dotnet fsi test-scripts\test-rss-reply-threading.fsx  # Reply threading and feed propagation
dotnet fsi test-scripts\test-rss-semantics.fsx        # Five-kind annotations, independent consumer and compatibility
dotnet fsi test-scripts\test-social-discovery.fsx     # SocialSpec discovery artifacts and config
dotnet fsi test-scripts\test-atproto-response-mapping.fsx # AT Protocol response mapping
dotnet fsi test-scripts\test-atproto-media.fsx        # AT Protocol media
dotnet fsi test-scripts\test-follow-yaml.fsx          # Follow workflow YAML
dotnet fsi test-scripts\test-docs-integrity.fsx       # Links, file paths, and voice in README and docs
dotnet fsi test-scripts\test-docs-integrity.fsx --self-test # Docs integrity checks against planted problems in fixtures/docs-integrity

# Parser checks
dotnet fsi test-scripts\test-ast-parsing.fsx          # AST parsing functionality
dotnet fsi test-scripts\test-context-validation.fsx   # Custom blocks in different contexts
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
publication; see `docs\reference\feeds.md`.

## Parser Checks

- **`test-ast-parsing.fsx`** - Validates AST parsing, YAML front-matter parsing, and error handling
- **`test-context-validation.fsx`** - Validates custom block parsing and rendering in different contexts (cards, feeds, pages)

## Test Content

The `test-content/` directory at the repo root contains test files:
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
