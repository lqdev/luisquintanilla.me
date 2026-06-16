---
title: "Pattern: Don't Regex Your Own Rendered HTML — Carry a Structured Render Product"
description: "The render-to-HTML-then-regex-strip-it-back round-trip is a recurring fragility (double-encoding, script break-out, heading duplication); carry a chrome-free structured body and recover facts from typed sources, and retire the old path via a flag-gated reviewed-diff cutover."
entry_type: pattern
published_date: "2026-06-15 14:35 -05:00"
last_updated_date: "2026-06-15 14:35 -05:00"
tags: fsharp, dotnet, architecture, html, rendering, refactoring, viewengine, patterns
related_entries: pattern-content-volume-html-parsing, pattern-viewengine-integration, pattern-long-lived-umbrella-branch-merge-strategy, pattern-diff-preview-as-trust-mechanism, pattern-right-sized-projection-registry
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

This is finding **F7** in the lqdev.me 2026 architecture assessment, and the only
*output-changing* bet in an otherwise byte-identical refactor. The homepage timeline built
each card by **re-parsing HTML it had just rendered**. The root cause was a single
`UnifiedFeedItem.Content: string` whose meaning was *heterogeneous*: raw markdown for some
types (posts/notes), but fully chrome'd `CardHtml` (an `<article>` wrapper + an `<h2><a>`
title link the card header already shows) for others (responses/bookmarks/reviews).

To render a card, the view then ran regexes to strip that chrome back off:

```fsharp
// regex-strip the <article>/<h1>/<h2><a> chrome the renderer just added,
// then neutralize <script> so the JSON payload can't break out of its tag
let cleanCardHtml (html: string) =
    Regex.Replace(html, @"<article[^>]*>", "")
    |> fun h -> h.Replace("</article>", "")
    |> fun h -> Regex.Replace(h, @"<h1[^>]*>.*?</h1>", "", RegexOptions.IgnoreCase)
    |> fun h -> Regex.Replace(h, @"<h2[^>]*><a[^>]*>.*?</a></h2>", "", RegexOptions.IgnoreCase)
    |> fun h -> h.Replace("<script", "&lt;script")
```

Three more regexes scraped *structured facts* out of rendered markup: a review cover image
`src`, a review's title/rating/summary, and a `YYYY-MM-DD HH:mm` response timestamp.

## Root Cause

A render → reparse → re-render round-trip throws away structure and then tries to
reconstruct it with string surgery. Every recovered fact is a guess against a moving
target (the renderer's output), which produces a recurring bug family:

- **Double HTML-encoding.** The review-cover regex scraped an `&` that was *already*
  encoded to `&amp;` in the rendered HTML; feeding it back through the ViewEngine encoded
  it again to `&amp;amp;`. The visitor saw a broken cover URL.
- **`</script>` break-out.** Embedding regex-cleaned HTML inside a `<script type="application/json">`
  block meant the payload could terminate the script tag early; the `<script>`-neutralizing
  `.Replace` existed *only* to paper over this.
- **Heading duplication / whitespace litter.** Stripping headings with `.*?` regexes left
  orphaned whitespace and occasionally corrupted adjacent content (an elfeed code block got
  mangled by the render-then-reparse double pass).

## Solution

Two moves, plus a safe way to ship them.

**1. Carry a structured render product.** Give each unified item a chrome-free `BodyHtml`,
produced once at conversion time by the canonical AST renderer, so the view **composes**
the body instead of stripping it. Responses render their card body from the *same* AST path
as their standalone page, so a card matches its own page by construction — no regex.

**2. Recover facts from typed sources, not rendered HTML.** The review cover image now comes
from `ReviewData.ImageUrl` read off the raw YAML/markdown source (encoded exactly once), not
a `Regex.Match` over rendered HTML. The progressive-load JSON is serialized from a typed
record (see [[pattern-stj-private-record-empty-object]]) instead of `sprintf`.

**3. Retire the old path via a flag-gated reviewed-diff cutover.** Because this is the one
change that is *not* byte-identical, it shipped behind a `RENDER_V2` flag in slices, with a
two-gate discipline:

- **Flag OFF stays byte-identical to baseline** at every slice (0 diffs / 13,525 files) —
  proving the old path is untouched and the new path fully isolated.
- **Flag ON is a reviewed diff**, characterized file-by-file before landing.
- At cutover, the new path becomes the default and the flag + every old regex helper is
  deleted in one commit, verified by a **second gate that is itself byte-identical**:
  *default output == the pre-cutover flag-ON snapshot, byte-for-byte.* That proves the
  deletion is a pure refactor of the now-live path, separating "did the render change?"
  (the reviewed diff) from "did deleting the dead path change anything?" (must be zero).

The final reviewed diff was two files: `index.html` (invisible whitespace + intended
heading suppression + one code-block fidelity *fix* + the STJ re-encode) and
`reviews/index.html` (exactly 1 of 36 covers — the double-encoding *fix*). No published URL
changed.

## Prevention

- **Render once, in one direction.** If a view needs structure (an image URL, a rating, a
  body without its title), get it from the *typed model*, never by parsing the HTML the
  same system just emitted.
- **A `string` field with heterogeneous meaning is the smell.** When `Content` is "raw
  markdown sometimes, rendered+chrome'd HTML other times," downstream consumers are forced
  into regex. Split the meanings (here: a clean `BodyHtml` seam) so each consumer composes.
- **Regex over generated HTML is a debt marker, not a tool.** The assessment tracked the
  count explicitly (5 clusters → 1 → 0) as a refactor exit metric.
- **For the one unavoidable output-changing change, gate it and use the snapshot-equality
  cutover gate** so the irreversible flip carries no surprise beyond the diff you already
  reviewed.
