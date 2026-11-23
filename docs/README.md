# Documentation

Everything here describes the site as it is now. How things got built is in
[`../archive/`](../archive/README.md), [`../projects/archive/`](../projects/archive/), and the
[changelog](../changelog.md).

## Understand it

- [Principles](principles.md): why the site is built the way it is
- [Architecture](architecture.md): how a Markdown file becomes pages, feeds, and federated posts
- [Architecture decision records](adr/README.md): the decisions behind the design, one page each
- [FEDERATION.md](../FEDERATION.md): what the site federates and how
- [ActivityPub](activitypub/README.md), [AT Protocol](atproto/README.md),
  [follows](follow/ARCHITECTURE-OVERVIEW.md), [WebSub](websub/ARCHITECTURE-OVERVIEW.md)

## Run it

- [Getting started](getting-started.md): build and preview the site on your machine

## Publish something

- [From a GitHub issue](guides/publishing/github-issues.md): the main way I post notes, responses, and media
- [Bookmarks](guides/publishing/bookmarks.md), [responses](guides/publishing/responses.md),
  [media](guides/publishing/media.md), [playlists](guides/publishing/playlists.md),
  [read-later](guides/publishing/read-later.md)

## Write or set up content

- Editor templates: [Emacs org-capture](guides/authoring/emacs-org-capture.md),
  [VS Code snippets](guides/authoring/vs-code-snippets.md)
- [Collections](guides/authoring/collections.md), [starter packs](guides/authoring/starter-packs.md),
  [travel guides](guides/authoring/travel-guides.md)
- [Presentation layouts](guides/authoring/presentation-layouts.md),
  [resume](guides/authoring/resume.md), [pinned posts](guides/authoring/pinned-posts.md)

## Features

- [AI Memex and the knowledge graph](features/ai-memex.md)
- [Text-only site](features/text-only-site.md), [PWA](features/pwa.md)
- [Link previews](features/link-previews.md), [content sharing](features/content-sharing.md),
  [social discovery](features/social-discovery.md)
- [Album collections](features/album-collections.md), [playlist collections](features/playlist-collections.md)

## Look something up

- [Content types](reference/content-types.md), [build flags](reference/build.md)
- [Feeds](reference/feeds.md), [URL structure](reference/url-structure.md)
- [Tags](reference/tags.md), [slugs](reference/slugs.md)
- [Reviews schema](reference/reviews-schema.md), [issue templates](reference/issue-templates.md)

## Run and maintain

- [Broken link checker](guides/operations/broken-link-checker.md),
  [workflow caching](guides/operations/workflow-caching.md)
- [Read-later cleanup](guides/operations/read-later-cleanup.md),
  [weekly wrap-up](guides/operations/weekly-wrapup.md)

## Change the docs

- [Voice](contributing/voice.md): how prose here should sound
- [Docs style](contributing/docs-style.md): where a page goes and how to check it
- Check your changes with `dotnet fsi test-scripts/test-docs-integrity.fsx`
