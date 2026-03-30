# ADR-0003: Static Site Generation Architecture

## Status
Accepted

## Context

Need a personal website that is fast, fully owned, portable, and inexpensive to host. The site must support 11+ content types, RSS feeds, IndieWeb compliance, ActivityPub integration, full-text search, and a text-only accessibility variant — all without requiring a server-side runtime.

## Decision

Build a custom static site generator in F# (.NET 10.0) that compiles markdown content with YAML frontmatter into a `_public/` directory of static HTML, RSS XML, and JSON files. The generator is deployed as a build step, and the output is hosted on Azure Static Web Apps.

**Technology stack** (from `PersonalSite.fsproj`):
- **Runtime**: .NET 10.0 (`net10.0`)
- **HTML generation**: Giraffe.ViewEngine 1.4.0 — type-safe HTML via F# functions
- **Markdown processing**: Markdig 0.38.0 — extensible CommonMark parser
- **YAML frontmatter**: YamlDotNet 16.3.0 — content metadata parsing
- **Data access**: FSharp.Data 5.0.2 — JSON/CSV type providers
- **Webmentions**: lqdev.WebmentionFs 0.0.7 — IndieWeb webmention support

**Build orchestration** (`Program.fs`):
1. Clean `_public/` output directory and copy static assets
2. Create output directory structure (feed, posts, presentations, snippets, wiki, tags)
3. Load content from `_src/` using AST-based parsers and `ContentProcessor<'T>` pattern
4. Build static pages (about, collections, contact, search, etc.)
5. Build all content types via GenericBuilder processors, collecting `FeedData` lists
6. Convert to `UnifiedFeedItem` lists for cross-cutting operations
7. Generate unified RSS feeds (fire-hose + type-specific + tag-based)
8. Generate ActivityPub content (activities, outbox, delivery queue)
9. Build timeline homepage and unified feed page
10. Generate text-only accessibility site via `TextOnlyBuilder`
11. Build tag pages, OPML, legacy feed aliases
12. Generate search indexes (content + tag indexes as JSON)

**Content source structure**: Markdown files in `_src/` subdirectories (`_src/feed/`, `_src/responses/`, etc.) with YAML frontmatter parsed by YamlDotNet into domain types defined in `Domain.fs`.

**Module compilation order** (from `.fsproj`): Domain → CustomBlocks → MediaTypes → ASTParsing → BlockRenderers → Services → GenericBuilder → ActivityPubBuilder → Views → Collections → SearchIndex → Loaders → TextOnlyBuilder → Builder → Program.

## Consequences

**Easier:**
- Fast page loads — all HTML is pre-generated, no server-side rendering.
- Low hosting cost — Azure Static Web Apps serves static files with global CDN.
- Full ownership — content lives as markdown files in a Git repository, portable to any hosting platform.
- Reproducible builds — running `dotnet run` produces the complete site deterministically.
- F# type system catches errors at compile time — mismatched content types, missing fields, and broken view functions fail the build rather than at runtime.

**More difficult:**
- No dynamic features without JavaScript progressive enhancement (search, theme switching, progressive loading all require client-side code).
- Build time grows with content volume — every page is regenerated on each build.
- Adding interactivity (comments, webmention receiving) requires external services or JavaScript.
- F# ecosystem is smaller than mainstream web frameworks, requiring custom solutions for common static site features.
