---
title: "Pattern: Keep Response Media Out of AT Protocol Plaintext Excerpts"
description: "Use the Markdown AST to omit linked preview media from response-card text while preserving the site rendering and external-card metadata."
entry_type: pattern
published_date: "2026-09-16 14:20 -05:00"
last_updated_date: "2026-09-16 14:20 -05:00"
tags: "fsharp, dotnet, web, architecture, patterns"
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

Two live reshare responses containing YouTube thumbnails showed the same video title twice in
Bluesky: once in the `Shared: {title}` header and external-card title, and again in the post
excerpt. The affected posts were the Michigan game-winning HAIL MARY response and
`They Ain't You (feat. Thundercat)`.

## Root Cause

The response POSSE builder derived the link-post excerpt with `stripToPlainText`. Its regex
conversion intentionally changed Markdown images into their alt text:

```fsharp
t <- Regex.Replace(t, @"!\[([^\]]*)\]\([^)]*\)", "$1")
```

The response source uses a linked YouTube thumbnail, so the thumbnail alt text was copied into
the plaintext excerpt. The same title was already present in the `app.bsky.embed.external`
card, and the link conversion also left a trailing `")` when the title contained parentheses.

## Solution

Keep the original Markdown unchanged for the website, but remove Markdown links that contain
images before deriving the response POSSE excerpt. Parse the source with the same Markdig AST
pipeline used by the site, recursively detect `LinkInline` image nodes, merge their source
spans, and remove those spans before the existing plaintext conversion:

```fsharp
let private stripResponseToPlainText (markdown: string) =
    markdown |> removeMarkdownImageLinks |> stripToPlainText
```

Use this response-specific conversion for bookmark and ordinary-web reshare excerpts. The
resulting record keeps the intentional `Shared: {title}` header and canonical site URL, while
the external card owns the target title and preview:

```text
Shared: They Ain't You (feat. Thundercat)

Great collab

https://lqdev.me/responses/they-aint-you-feat-thundercat-2026-08-31/
```

## Prevention

Test the record builder with the real nested image-link shape and a title containing parentheses.
The response regression suite now verifies both the post text and external-card description.
When a syndication format has a native media/card field, do not flatten that same media into
plaintext merely because the source Markdown has useful alt text. Prefer AST spans over regexes
for nested Markdown constructs.
