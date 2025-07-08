# Test Scripts

This directory contains validation and testing scripts for the website's core infrastructure components.

## Usage

Run test scripts from the project root directory:

```bash
# Test AST parsing functionality
dotnet fsi test-scripts\test-ast-parsing.fsx

# Test Phase 1B generic builder and block renderers
dotnet fsi test-scripts\test-phase1b.fsx
```

## Scripts

- **`test-ast-parsing.fsx`** - Validates AST parsing, YAML front-matter parsing, and error handling
- **`test-phase1b.fsx`** - Tests generic builder pattern and custom block renderers

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
