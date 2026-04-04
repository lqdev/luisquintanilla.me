---
title: "Pattern: Content Type Landing Page"
description: "Proper landing pages for all content types significantly improve content discoverability and user experience, following consistent structural patterns."
entry_type: pattern
published_date: "2026-04-01 00:00 -05:00"
last_updated_date: "2026-04-01 00:00 -05:00"
tags: fsharp, web, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
related_entries: pattern-generic-builder-content-processor, pattern-feed-architecture-consistency, codebase-context
---

## Discovery

As the site grew to support many content types — posts, notes, responses, bookmarks, snippets, wiki entries, albums, playlists — a gap emerged: not every content type had a proper landing page. Some types were only accessible through the unified timeline or tag pages. Users had no dedicated entry point to browse, say, all bookmarks or all snippets. Proper landing pages for all content types significantly improve content discoverability and user experience when they follow consistent structural patterns.

## Root Cause / Problem

Without dedicated landing pages, content types become second-class citizens. A user who wants to browse all bookmarks has to know they exist on the timeline and use a filter — there's no URL they can navigate to directly, no page they can bookmark in their browser. This hurts discoverability, makes the site harder to navigate, and breaks the expectation that `/bookmarks/` should show all bookmarks.

The underlying problem is that adding a new content type to the F# pipeline (parser, processor, builder) doesn't automatically create a landing page. Each landing page requires explicit view function creation, builder integration, and directory management. Without a documented pattern, each new content type risks being added without its landing page.

## Solution

The pattern uses existing content with type metadata rather than creating separate file structures. For example, bookmarks are responses with `response_type: bookmark` — the landing page filters on that metadata instead of requiring a separate `_src/bookmarks/` directory.

The implementation follows a consistent sequence:

1. **CollectionViews.fs Updates**: Create or modify view functions for proper landing page structure. Each landing page gets an `h2` header, a descriptive paragraph explaining the content type, and a chronological list of entries.

2. **Builder Function Creation**: Create a dedicated filtering and page generation function (e.g., `buildBookmarksLandingPage`) that filters the content collection and passes results to the view function.

3. **Build Integration**: Add the function call to `Program.fs` orchestration, placing it after data collection so all content is available for filtering.

4. **View Function Reuse**: Leverage existing view functions (e.g., `responseView`) for consistent UI across content types rather than creating entirely new rendering logic.

5. **Directory Management**: Automatic directory creation and `index.html` generation following the `/content-type/index.html` URL pattern.

## Key Components

- **CollectionViews.fs**: View functions that render landing page HTML with consistent structure across all content types
- **GenericBuilder.fs**: Builder functions that filter content collections and invoke view rendering
- **Program.fs**: Build orchestration that calls landing page generators after content data is collected
- **Existing view functions**: Reused for individual item rendering within landing pages

## Results

- **Landing page parity**: Every content type gets a dedicated landing page at a predictable URL
- **Proper content filtering**: Landing pages show only the relevant content type, correctly filtered from the broader collection
- **Chronological ordering**: Content appears in reverse-chronological order, matching user expectations
- **Seamless build integration**: Landing page generation fits naturally into the existing build pipeline without disrupting other content processing

## Benefits

This pattern ensures that adding a new content type to the site follows a predictable path that includes discoverability from day one. It reduces the chance of "orphaned" content types that exist in the pipeline but have no user-facing entry point. The consistent structure (header, description, chronological list) means users learn the navigation pattern once and can apply it across the entire site. Maintenance is simplified because all landing pages share the same structural template, and changes to the pattern propagate naturally.
