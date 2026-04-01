---
title: "Pattern: Feed Architecture Consistency"
description: "Unified feed pattern consistency with prominent subscription hub placement dramatically improves user experience and maintains architectural coherence."
entry_type: pattern
published_date: "2026-04-01 00:00 -05:00"
last_updated_date: "2026-04-01 00:00 -05:00"
tags: fsharp, web, indieweb, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
---

## Discovery

As the site accumulated multiple content types — each with its own RSS feed — the feed architecture grew organically. Some feeds followed one URL pattern, others followed another. The unified "all content" feed existed but wasn't prominently featured. Users who wanted to subscribe had to discover feed URLs through view-source or guesswork. Making the feed architecture consistent and featuring a subscription hub with prominent placement dramatically improved the user experience and maintained architectural coherence across the entire system.

## Root Cause / Problem

Organic growth of feed infrastructure leads to inconsistent URL patterns. When each content type's feed is added at a different time by different sessions, the naming conventions drift. One feed might live at `/posts/feed.xml`, another at `/notes/rss.xml`, and a third at `/feed/responses.xml`. This inconsistency makes feeds harder to discover, harder to document, and harder to maintain. It also creates a poor experience for users who want to subscribe — there's no single place that explains what's available and how to access it.

## Solution

The pattern establishes a consistent URL structure and a prominent subscription hub:

- **Consistent URL Structure**: All feeds follow the `/[type]/feed.xml` canonical path with user-friendly aliases like `/[type].rss`. The unified feed follows the same pattern, available at both `/feed/feed.xml` and `/all.rss`.

- **Subscription Hub Integration**: Important feeds are featured prominently on the site with clear descriptions explaining what each feed contains. The hub uses user-friendly URLs rather than raw XML paths, making it easy for users to share subscription links.

- **User-Friendly Aliases**: Memorable URLs like `/all.rss` for the unified feed make sharing and verbal communication simple. These aliases are generated alongside the canonical feed files during the build process.

- **OPML Integration**: Featured feeds appear as the first entries in the site's OPML file, enabling one-click import into feed readers.

- **Backward Compatibility**: When transitioning from an inconsistent pattern, both old and new URLs are generated during a transition period. This prevents existing subscribers from losing their feeds.

- **Pattern Documentation**: The URL structure and alias relationships are documented so that future content types automatically get feeds that follow the established pattern.

## Key Components

- **GenericBuilder.fs**: Feed generation functions that produce both canonical and alias feed files for each content type
- **Program.fs**: Build orchestration that generates all feeds with consistent naming
- **OPML generation**: Subscription list that references user-friendly feed URLs
- **Subscription hub page**: User-facing page listing all available feeds with descriptions

## Results

- **Consistent URLs**: Every content type feed follows the same predictable pattern
- **Discoverability**: Users can find and subscribe to feeds through the subscription hub without needing to inspect page source
- **Shareability**: User-friendly aliases make it easy to tell someone "subscribe to lqdev.me/all.rss"
- **OPML compatibility**: Feed readers that support OPML can import the complete subscription list in one action

## Benefits

Improved feed discoverability means more subscribers and better engagement with the site's content. Consistent architecture patterns reduce cognitive load during development — when adding a new content type, the feed URL pattern is already decided. The subscription hub creates a single source of truth for what feeds are available. Backward compatibility during transitions prevents subscriber loss. The pattern is self-documenting: seeing one feed URL tells you the structure of all feed URLs.
