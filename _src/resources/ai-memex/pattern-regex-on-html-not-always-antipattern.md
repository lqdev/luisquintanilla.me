---
title: "Regex-on-Rendered-HTML Isn't Always the Anti-Pattern — Cross-Source Semantic Transforms Belong at the HTML Layer"
description: "Why a structural AST renderer could NOT replace the text-only image regex, and how to tell a legitimate HTML-layer transform from the render-then-un-render smell."
entry_type: pattern
published_date: "2026-06-15 18:16 -05:00"
last_updated_date: "2026-06-15 18:16 -05:00"
tags: "fsharp, dotnet, markdig, architecture, accessibility, patterns, refactoring"
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: pattern-regex-on-rendered-html-antipattern, pattern-stj-private-record-empty-object
---

## Discovery

While planning **B4** of the 2026 architecture refactor (text-only site "F10": *delete the
`TextOnlyViews` regex rewriting and re-render from the structured product*), I built a structural
Markdig renderer to replace `TextOnlyContentProcessor.replaceImagesWithText` — the regex that turns
every `<img>` in the text-only site into an accessible `<a ...>[Image: alt]</a>` text link.

The structural renderer (`MarkdownService.convertMdToTextOnlyHtml`) subclassed
`Markdig.Renderers.Html.Inlines.LinkInlineRenderer` and, for `IsImage` nodes, emitted the text link
instead of an `<img>`. It compiled clean and matched the markdown `![]()` case exactly.

**Then the investigation killed the premise.** The old regex runs on the *final rendered HTML*, so
it catches `<img>` from **three heterogeneous sources** in this corpus:

| Source | Example | Count (src files) | Covered by a `LinkInline` renderer? |
|--------|---------|-------------------|-------------------------------------|
| Markdown image syntax | `![alt](url)` → `<img>` | 231 | ✅ yes |
| Custom `:::media` block | `MediaBlockHtmlRenderer` emits `<img class="media-image">` (`CustomBlocks.fs:413`) | 19 | ❌ no — it's a *Block* renderer, not `LinkInline` |
| Raw HTML in markdown | literal `<img ...>` passed through | 11 | ❌ no — Markdig keeps raw HTML as an opaque `HtmlBlock`/`HtmlInline` string slice; **there is no `img` AST node** |

Wiring the structural renderer in would have **regressed sources #2 and #3** to broken `<img>` tags
in the text-only output — an accessibility *content* regression, not a benign render diff.

## Root Cause

The F10 item assumed text-only's image regex was the same anti-pattern that **B2** had just
eliminated for timeline cards (`cleanCardHtml`). It is not. The two are categorically different:

- **B2's `cleanCardHtml` was render-then-UN-render.** The renderer itself added chrome (heading
  wrappers, target-URL lines), then a regex stripped that *same* chrome back off. The information
  destroyed by the regex was information the *same pipeline* had just created. That is a true smell:
  the fix is to not add the chrome in the first place (carry a clean body through the AST). See the
  companion entry [[pattern-regex-on-rendered-html-antipattern]].

- **Text-only's `replaceImagesWithText` is a cross-source SEMANTIC transform.** It maps a *concept*
  ("an image") to an accessible representation ("a text link"), applied uniformly to final HTML
  regardless of which of three independent subsystems produced the `<img>`. One of those sources
  (raw HTML) has **no structural representation at all** — there is literally no AST node to
  intercept. For that case, operating on the rendered HTML string is not a smell; it is the *only*
  place where all three sources have converged into one uniform shape.

## The Discriminator (how to tell them apart)

> **Render-then-un-render (fix it):** the regex removes/rewrites markup that the *same* render
> pipeline produced, to recover information the pipeline already had. → Carry the information
> structurally; don't render-then-strip.
>
> **Cross-source semantic transform (keep it):** the regex maps a uniform *concept* across output
> produced by *multiple independent* renderers/sources, at least one of which has no AST node (raw
> HTML). → The post-render HTML layer is the correct convergence point. A structural rewrite would
> either regress the un-covered sources or merely *relocate* the regex into a custom
> `HtmlBlock`/`HtmlInline` renderer (still string surgery, now scattered).

A quick test: *"Could a single AST node type represent every input this regex must handle?"* If no
(raw HTML, multiple block renderers), the HTML layer is legitimate.

## Solution / Outcome

**Dropped B4.** Its two genuinely-valuable parts (shared nav data via `Navigation.renderTextOnlyNav`,
and response-subtype normalization via `TextOnlyViews.normalizeContentType`) had already shipped in
Phase 2. The only "remaining" work — deleting the image regex — was mis-scoped, so the correct
engineering call was to keep `replaceImagesWithText` and remove the speculative structural renderer
rather than ship a regression or a regex relocation. B4 was additive and `/text/`-scoped, never a
prerequisite for the umbrella→main promotion.

Incidental verification along the way: the canonical domain in the codebase is `www.lqdev.me`
(11 references, 0 for `luisquintanilla.me`), so the "domain-prefix bug" the plan suspected in
`replaceImagesWithText` did **not** exist.

## Prevention

- When an assessment says "delete the regex," first **enumerate every source that feeds the regex's
  input**. A regex on *final* HTML often spans sources a structural pass can't see.
- Treat "raw HTML embedded in markdown" as a hard boundary: Markdig (and most markdown engines)
  expose it as an opaque string, so any transform over it is inherently string-level.
- Don't pattern-match a refactor onto a prior win by surface similarity ("it's a regex on HTML, like
  the last one"). Classify by *what information the regex uses and where that information lives*.
- A speculative renderer that "builds clean" is not validated — it was the *coverage analysis*, not
  the compiler, that revealed the regression. Quantify the input distribution before wiring in.
