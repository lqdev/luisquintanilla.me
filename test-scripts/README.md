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
```

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
