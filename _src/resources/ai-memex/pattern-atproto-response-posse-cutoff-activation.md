---
title: "Pattern: Activating AT Protocol Response POSSE Safely"
description: "Use a response-kind-specific flag and exact forward-only cutoff when enabling AT Protocol POSSE."
entry_type: pattern
published_date: "2026-08-27 21:04 -05:00"
last_updated_date: "2026-08-27 21:04 -05:00"
tags: "fsharp, dotnet, architecture, devops, patterns"
related_skill: "write-ai-memex"
source_project: "lqdev.me"
---

## Discovery

GitHub Actions run `33134512660` generated successful site output but reported bookmark, reshare,
quote, and repost staging as absent because the response modes were intentionally disabled. The
most recent response was a `reshare` published at `2026-08-27 20:58 -05:00` with an ordinary
Anthropic URL target.

## Root Cause

Response POSSE has four independent modes. Each mode requires both a flag and a forward-only
cutoff; `DateTimeOffset.MaxValue` is a sentinel that produces no eligible records. Classification
is target-driven: an ordinary URL is always a `LinkPost`, while quote-post and repost decisions
only apply to native AT Protocol post targets. Therefore a blockquote-only body does not turn an
ordinary-web reshare into a repost.

## Solution

Enable only the matching mode and set its cutoff to the latest response's exact publication
instant:

```fsharp
let useAtProtoResharePostsSync = true
let resharePostsActivationCutoff =
    DateTimeOffset(2026, 8, 27, 20, 58, 0, TimeSpan.FromHours -5.0)
```

The eligibility predicate remains forward-only and inclusive:

```fsharp
| LinkPost -> useAtProtoResharePostsSync && d >= resharePostsActivationCutoff
```

This stages an `app.bsky.feed.post` link record under `reshares/`, with the external target in an
`app.bsky.embed.external` card and the site's canonical response URL in post text. Existing CI
already checks staging, uploads `atproto-staging-reshares`, and invokes
`Scripts/sync-atproto.fsx --collection app.bsky.feed.post --commit`; no workflow plumbing is needed.

## Prevention

Before activation, inspect the latest source frontmatter and classify the target with
`AtProtoResponseMapping.classifyResponse` rather than inferring from screenshots or body quoting.
Check for same-minute peers because minute-precision cutoffs include all records with the same
timestamp. Keep unrelated response flags false, replace a sentinel cutoff in the same change as
its flag, generate local staging, and use a dry-run or capped sync before any live write. Never
run the live sync during local validation.
