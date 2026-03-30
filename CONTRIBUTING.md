# Contributing to lqdev.me

An F# static site generator for a personal [IndieWeb](https://indieweb.org/) website. This guide serves as the operating manual for AI coding assistants (primarily GitHub Copilot) and helps human contributors understand the project.

## Architecture Overview

The site is built with:

- **F# / .NET 10** — core build logic (`PersonalSite.fsproj`)
- **Giraffe ViewEngine** — type-safe HTML generation
- **Markdig** — Markdown-to-HTML processing with custom extensions
- **YamlDotNet** — YAML frontmatter parsing
- **Azure Static Web Apps** — hosting with `staticwebapp.config.json` for routing

Source content lives in `_src/` and the build outputs to `_public/`.

## Content Types

| Type | Source Directory | URL Pattern | Key Frontmatter Fields | On Timeline |
|------|-----------------|-------------|----------------------|-------------|
| Post | `_src/posts/` | `/posts/{slug}/` | `post_type`, `title`, `description`, `published_date`, `tags` | Yes |
| Note | `_src/notes/` | `/notes/{slug}/` | `post_type`, `title`, `published_date`, `tags` | Yes |
| Response | `_src/responses/` | `/responses/{slug}/` | `title`, `targeturl`, `response_type`, `dt_published`, `dt_updated`, `tags` | Yes |
| Bookmark | `_src/bookmarks/` | `/bookmarks/{slug}/` | `title`, `bookmark_of`, `description`, `dt_published`, `dt_updated`, `tags` | Yes |
| Wiki | `_src/resources/wiki/` | `/resources/wiki/{slug}/` | `post_type`, `title`, `last_updated_date`, `tags` | No |
| Snippet | `_src/resources/snippets/` | `/resources/snippets/{slug}/` | `title`, `language`, `tags`, `created_date` | No |
| Presentation | `_src/resources/presentations/` | `/resources/presentations/{slug}/` | `title`, `resources`, `tags`, `date` | No |
| Book (Review) | `_src/reviews/library/` | `/reviews/{slug}/` | `title`, `post_type`, `published_date`, `tags` + `:::review` block | Yes |
| Media (Album) | `_src/media/` | `/media/{slug}/` | `title`, `post_type`, `published_date`, `tags` + `:::media` blocks | Yes |
| Album Collection | `_src/albums/` | `/collections/albums/{slug}/` | `title`, `description`, `date`, `tags` + `:::media` blocks | Yes |
| Playlist Collection | `_src/playlists/` | `/collections/playlists/{slug}/` | `title`, `description`, `date`, `tags` | No |
| AI Memex *(coming soon)* | `_src/resources/ai-memex/` | `/resources/ai-memex/{slug}/` | `title`, `description`, `entry_type`, `published_date`, `last_updated_date`, `tags` | No (own RSS feed) |

## Adding Content

### Blog Post (`_src/posts/`)

```yaml
---
post_type: "article"
title: "My Post Title"
description: "A brief description of the post"
published_date: "2025-01-15 08:00 -05:00"
tags: ["topic", "another-topic"]
---
```

### Note (`_src/notes/`)

```yaml
---
post_type: "note"
title: "Short note title"
published_date: "2025-01-15 19:00 -05:00"
tags: ["tag1", "tag2"]
---
```

### Wiki Entry (`_src/resources/wiki/`)

```yaml
---
post_type: "wiki"
title: "Topic Name"
last_updated_date: "01/15/2025 08:00 -05:00"
tags: topic, subtopic, category
---
```

Note: Wiki `tags` are a comma-separated string, not a YAML array.

### Response (`_src/responses/`)

```yaml
---
title: "Response Title"
targeturl: https://example.com/article
response_type: reply
dt_published: "2025-01-15 12:00 -05:00"
dt_updated: "2025-01-15 12:00 -05:00"
tags: ["tag1", "tag2"]
---
```

Valid `response_type` values: `reply`, `star`, `share`, `bookmark`, `rsvp`.

### Bookmark (`_src/bookmarks/`)

```yaml
---
title: "Bookmark Title"
bookmark_of: https://example.com/resource
description: "Why this is worth bookmarking"
dt_published: "2025-01-15 12:00 -05:00"
dt_updated: "2025-01-15 12:00 -05:00"
tags: ["tag1", "tag2"]
---
```

## RSS Feeds

The site generates `feed.xml` files inside each content type's output directory. User-friendly aliases are configured in `staticwebapp.config.json`:

| Alias | Redirects To | Content |
|-------|-------------|---------|
| `/all.rss` | `/feed/feed.xml` | All content (fire-hose feed) |
| `/blog.rss` | `/posts/feed.xml` | Blog posts |
| `/microblog.rss` | `/notes/feed.xml` | Notes |
| `/responses.rss` | `/responses/feed.xml` | Responses |
| `/bookmarks.rss` | `/bookmarks/feed.xml` | Bookmarks |
| `/reviews.rss` | `/reviews/feed.xml` | Book reviews |
| `/media.rss` | `/media/feed.xml` | Media albums |

Tag-specific feeds are also generated at `/tags/{tag}/feed.xml`.

## Build & Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (target framework: `net10.0`)
- Node.js 20+ (for Playwright tests and Azure SWA CLI, if needed)

### Build

```bash
dotnet build
```

### Run the generator

```bash
dotnet run
```

This reads `_src/`, processes all content, and writes the static site to `_public/`.

### Local preview

Serve `_public/` with any static file server:

```bash
npx serve _public
```

Or use [Azure Static Web Apps CLI](https://azure.github.io/static-web-apps-cli/):

```bash
npx @azure/static-web-apps-cli start _public
```

### Scripts

The `Scripts/` directory contains utility scripts (F# `.fsx` and shell scripts) for tasks like link checking, content migration, RSS generation, and ActivityPub testing. See `Scripts/README.md` for details.

## Project Structure

```
├── Domain.fs                  # Content types, metadata records, interfaces
├── CustomBlocks.fs            # Custom Markdig block parsers (:::media, :::review)
├── ASTParsing.fs              # Markdown AST parsing and frontmatter extraction
├── BlockRenderers.fs          # HTML rendering for custom blocks
├── MediaTypes.fs              # Media type definitions
├── GenericBuilder.fs          # ContentProcessor pattern, feed generation, unified feeds
├── ActivityPubBuilder.fs      # ActivityPub/Fediverse integration
├── Builder.fs                 # Page builders (buildPosts, buildNotes, etc.)
├── Loaders.fs                 # Legacy content loaders
├── TextOnlyBuilder.fs         # Accessibility-first text-only site generator
├── SearchIndex.fs             # Client-side search index generation
├── Collections.fs             # Collection system (blogroll, starter packs)
├── Program.fs                 # Build orchestration entry point
├── Services/                  # Support services
│   ├── Markdown.fs            #   Markdig pipeline configuration
│   ├── Tag.fs                 #   Tag processing and page generation
│   ├── ReadingTime.fs         #   Reading time estimation
│   ├── Webmention.fs          #   Webmention support
│   ├── RelatedContent.fs      #   Related content suggestions
│   └── FollowersSync.fs       #   ActivityPub followers
├── Views/                     # Giraffe ViewEngine templates
│   ├── LayoutViews.fs         #   Page layouts and timeline
│   ├── ContentViews.fs        #   Individual content page views
│   ├── CollectionViews.fs     #   Collection page views
│   ├── FeedViews.fs           #   Feed/index page views
│   ├── ComponentViews.fs      #   Reusable UI components
│   ├── TagViews.fs            #   Tag pages
│   ├── TravelViews.fs         #   Travel guide views
│   ├── TextOnlyViews.fs       #   Text-only site views
│   ├── Layouts.fs             #   Base HTML layouts
│   ├── Partials.fs            #   Shared partial views
│   └── Generator.fs           #   View generation utilities
├── _src/                      # Source content (markdown + frontmatter)
├── _public/                   # Generated output (git-ignored)
├── Scripts/                   # Utility scripts
├── Data/                      # JSON data files (collections, pinned posts)
├── docs/                      # Documentation and ADRs
├── css/ & styles/             # Stylesheets
└── staticwebapp.config.json   # Azure SWA routing and redirects
```

## AI Coding Assistant Guidelines

### Orientation

1. **Read this file first** — it covers architecture, content types, and conventions.
2. **Read `Domain.fs`** for the type definitions you'll be working with.
3. **Read `Program.fs`** to understand build orchestration and how content types are wired together.
4. **Read relevant source files** before making changes (e.g., `GenericBuilder.fs` for feed logic, `Views/LayoutViews.fs` for page templates).

### The GenericBuilder Pattern

New content types follow the `ContentProcessor<'T>` pattern defined in `GenericBuilder.fs`:

```fsharp
type ContentProcessor<'T> = {
    Parse: string -> 'T option        // Parse file to domain type
    Render: 'T -> string              // Render to HTML
    OutputPath: 'T -> string          // Output file path
    RenderCard: 'T -> string          // Card HTML for index pages
    RenderRss: 'T -> XElement option  // RSS XML element
}
```

To add a new content type:

1. Define the metadata record and wrapper type in `Domain.fs`
2. Create a `{Type}Processor` module in `GenericBuilder.fs` implementing `ContentProcessor<'T>`
3. Add a `build{Type}()` function in `Builder.fs` that uses `buildContentWithFeeds`
4. Wire it into `Program.fs` orchestration
5. Add view functions in the appropriate `Views/` module
6. Add the module to `PersonalSite.fsproj` compile order (order matters in F#!)

### Conventions

- **Commit messages**: Use conventional style with a `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` trailer
- **No planning files**: Don't create markdown planning/tracking files in the repo root
- **ViewEngine over sprintf**: Use Giraffe ViewEngine for HTML generation, not string interpolation
- **Type qualification**: Use fully qualified types (e.g., `MediaType.Unknown` not `Unknown`)
- **Frontmatter field names**: Always use the actual YAML alias names from `Domain.fs` (e.g., `published_date` not `date` for posts, `dt_published` not `date` for responses)
- **Standard markdown tables**: Use regular `| col |` table syntax, not `||` double-pipe formatting

### Architectural Decisions

The `docs/adr/` directory contains Architecture Decision Records documenting key design choices. Check there before proposing changes to established patterns.

## AI Memex Publishing

The AI Memex is a dedicated publishing space for AI coding assistants to share knowledge, patterns, and project insights as permanent content on the site.

### Entry Types

| Entry Type | Purpose |
|-----------|---------|
| `project-report` | Summary of a completed project or significant milestone |
| `research` | Deep research findings on a technology or approach |
| `reference` | Quick-reference guide or cheat sheet |
| `pattern` | Reusable pattern discovered during development |
| `blog-post` | General commentary or analysis from the AI perspective |

### File Location

Entries go in `_src/resources/ai-memex/` with descriptive filenames.

### Frontmatter Format

```yaml
---
title: "Entry Title"
description: "Brief description of the entry"
entry_type: "pattern"
published_date: "2025-01-15 08:00 -05:00"
last_updated_date: "2025-01-15 08:00 -05:00"
tags: ["fsharp", "static-site", "pattern"]
---
```

### When to Publish

- After completing a significant project or migration
- When discovering a reusable pattern that future sessions should know about
- After deep research that produced actionable insights
- When creating reference material that would help orient future AI sessions
- After solving a particularly tricky bug with lessons worth preserving
