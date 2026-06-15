# ADR-0008: Structured Render Product for the Timeline (kill regex-on-rendered-HTML)

## Status
Accepted

## Context

The homepage timeline composed each card by **re-parsing already-rendered HTML**. The unified
feed item carried a single `Content: string` whose *meaning was heterogeneous*: raw markdown for
posts/notes/media, but fully chrome'd `CardHtml` (an `<article>` wrapper plus an `<h2><a>` title
link the card header already shows) for responses/bookmarks/reviews. To render a card, the
timeline therefore ran a regex pass (`cleanCardHtml`) to **strip the chrome back off** the HTML
it had just built, and additional regexes scraped structured facts out of rendered markup:

- `TimelineViews.cleanCardHtml` — regex-strips `<article>`, `<h1>`, and `<h2><a>` chrome, then
  neutralizes `<script>` to keep the progressive-load JSON from breaking out of its `<script>` tag.
- `TimelineViews.createSimplifiedReviewContent` — `IndexOf`/`Substring` scraping of a rendered
  `custom-review-block` to recover title/image/rating/summary.
- `ContentViews.extractImageFromReviewHtml` — a `Regex.Match` over rendered review HTML to recover
  the cover image `src`.
- `ContentViews.cleanResponseContent` — a `YYYY-MM-DD HH:mm` timestamp/placeholder regex over
  rendered response bodies (already dead code by the time of B2 — see below).

The progressive-load JSON was *also* hand-built with `sprintf` + a bespoke `escapeJson`.

This is finding **F7** in `docs/architecture-assessment-2026.md`: a render→reparse→re-render
round-trip that is fragile (double-encoding bugs, `</script>` break-out risk, heading
duplication) and impossible to reason about locally. It is the **only output-changing bet** in the
2026 refactor — every other bet (Phases 0–2, B1) is byte-identical. B2 is therefore the one
**reviewed-diff / RED** change, executed behind a `RENDER_V2` flag so the old and new paths could
be compared byte-for-byte before cutover.

## Decision

Carry a **chrome-free structured body** (`UnifiedFeedItem.BodyHtml`) on each unified item, produced
at conversion time by composing the canonical AST renderer — so the timeline **composes** the body
structurally instead of regex-stripping it. Recover structured facts (review cover image) from the
**typed source** (`GenericBuilder.ReviewDataExtractor` over `MarkdownSource`), not from rendered
HTML. Serialize the progressive-load JSON with `System.Text.Json` over a typed record, not `sprintf`.

Delivered as three flag-gated slices, each byte-identical with the flag OFF and a reviewed diff
with it ON, then a cutover that flips the default ON and **deletes the legacy paths**:

- **B2.1 — clean-body seam.** `ASTParsing.removeRedundantCardHeadings` (shared AST pass dropping
  the level-1 + bare-link level-2 headings the old regex approximated) and
  `ASTParsing.renderCardHtmlFromMarkdown` (the canonical bare renderer; responses use it so a card
  matches its own page). `convertMdToCardHtml` delegates to the shared helper. Kills `cleanCardHtml`.
- **B2.2 — `System.Text.Json`.** A public record `TimelineViews.ProgressiveContentItem` serialized
  with STJ replaces `escapeJson` + `sprintf`. STJ's default HTML-safe encoder escapes `<>&`
  (`<` → `\u003C`), which *structurally* closes the latent `</script>` break-out the old path relied
  on `cleanCardHtml` to neutralize.
- **B2.3 — structured review image + dead-code removal.** `bookPostView` reads the cover from
  `ReviewData.ImageUrl` (raw YAML) instead of scraping rendered HTML; the dead response-body-view
  cluster (incl. `cleanResponseContent`) was removed byte-identically.

### Why a flag, and why "byte-identical OFF" was the gate

View code cannot see `argv`, so `RENDER_V2` latched the `--render-v2` answer at startup. The
discipline: with the flag OFF, `_public/` stayed **byte-identical to baseline** (0 diffs / 13,525
files) at every slice — proving the old path was untouched and the new path fully isolated. With
the flag ON, the diff was **reviewed**, not zero. At cutover the new path became the default and the
flag plus both OFF branches and all four regex helpers were deleted in one commit, verified by a
second gate: **default output == the pre-cutover flag-ON snapshot, byte-for-byte** (0 diffs /
13,525) — proving the deletion is a pure refactor of the now-live path.

### The reviewed diff (what actually changed for visitors)

Two files differ from the pre-B2 baseline, both fully characterized:

- `index.html` — ~1199 invisible whitespace cleanups (orphaned `\n` where redundant headings were
  structurally removed), the intended in-content heading suppression (57 `<h1>` + 1 bare-link `<h2>`,
  matching the old *visual* behavior), one code-block fidelity fix (an elfeed bookmark the old
  render-then-reparse double pass corrupted), and the STJ-re-encoded progressive-load JSON (a
  faithful 1:1 re-encoding: literal `<` count 15116 == `\u003C` count 15116; `&` 4041 == 4041;
  every decoded `title`/`contentType`/`date`/`url` identical across all 1570 items).
- `reviews/index.html` — exactly **1 of 36** book covers changed, and it is a **strict bug fix**: the
  old regex scraped an already-HTML-encoded `&` (`&amp;`) which ViewEngine then re-encoded to
  `&amp;amp;` (double-encoded); the structured value reads raw YAML and encodes once (`&amp;`).

Everything else is byte-identical.

## Consequences

**Easier / fixed:**
- The render→reparse→re-render round-trip is gone. The four regex helpers (`cleanCardHtml`,
  `createSimplifiedReviewContent`, `extractImageFromReviewHtml`, `cleanResponseContent`) and the dead
  `timelineHomeView` are deleted.
- Structured facts come from typed sources; the double-encoding and `</script>` break-out bug classes
  are structurally impossible.
- Progressive-load JSON is produced by a real serializer over a typed record.

**Gotcha captured:** a `private` F# record serializes as empty `{}` under `System.Text.Json` — its
reflection serializer only reads properties of *accessible* types. The record must be public.

**Constraints / unchanged:**
- No published URL changed (URL permanence invariant held).
- Markdown content types (posts/notes/media) keep their existing figure pipeline.

**Exit:** there is no flag to revert to — B2 is the new single path. Reverting means restoring the
deleted helpers from git history. The pre-cutover flag-ON snapshot equals the shipped output, so the
cutover commit carries no behavioral surprise beyond the two reviewed files above.
