---
name: build-validate
description: "Build and validate the lqdev.me static site generator — dotnet build, dotnet run, and output verification"
---

# Build & Validate lqdev.me

Standard build and validation workflow for the F# static site generator.

## Quick Reference

```powershell
# Compile (fast, catches type errors)
dotnet build

# Generate site (full pipeline, outputs to _public/)
dotnet run
```

## Build Expectations

### `dotnet build`
- **Target**: .NET 11 (net11.0)
- **SDK**: 11.0.100, pinned by `global.json`
- **Expected**: 0 errors
- **Known warning**: 1 pre-existing FS1104 warning in ActivityPubBuilder.fs (ignore)
- **Duration**: ~10 seconds

### `dotnet run`
- **What it does**: Runs the full static site generator pipeline
- **Output**: All generated files in `_public/` directory
- **Duration**: ~60-90 seconds
- **Expected**: Exit code 0 with content generation summary

## When to Build

- After modifying any `.fs` file
- After adding new content to `_src/`
- Before committing code changes (at minimum `dotnet build`)

## Common Issues

### Local NuGet lock hash drift
Keep the CI-validated repository lock hash. Use an isolated local lockfile when
local package content differs; see
`_src/resources/ai-memex/pattern-nuget-lock-hash-drift.md` and `CONTRIBUTING.md`.
Verify that package versions are unchanged before treating the difference as hash drift.

### Missing module in project file
If you create a new `.fs` file, add it to `PersonalSite.fsproj` in the `<ItemGroup>`.
F# project files require explicit file ordering.

### Type inference errors
Use explicit type annotations in F# code. The compiler sometimes can't infer types
across module boundaries.

### Content not appearing
Check that:
1. Source file is in the correct `_src/` subdirectory
2. YAML frontmatter is valid (no tabs, correct indentation)
3. The build function for that content type is called in `Program.fs`

## Output Verification

After `dotnet run`, spot-check:
- `_public/index.html` — homepage renders
- `_public/feed/feed.xml` — unified feed has entries
- Content-specific directories exist with `index.html` files
