# Architecture Assessment 2026 — Streamlining the F#/.NET Static Site Generator

**Date**: 2026-06-10
**Engagement**: External-consultant-style review ("Fable consultant") of the lqdev.me generator
**Companion plan**: [`projects/archive/2026-06-15-architecture-refactor-2026.md`](../projects/archive/2026-06-15-architecture-refactor-2026.md)
**Status**: Findings final; execution tracked in the companion plan

---

## 1. Executive Summary

This codebase **works**. It ships real content daily, owns its whole stack (custom SSG, Webmentions,
ActivityPub), and implements IndieWeb standards more completely than most personal sites on the
internet. Nothing in this report is a rewrite recommendation. The architecture's core idea — the
`ContentProcessor<'T>` / `FeedData<'T>` / `UnifiedFeedItem` pipeline — is sound and worth keeping.

The central problem is **change amplification**: the cost of the site's most common evolution
(adding or adjusting a content type) is spread across ~8 files and 4 hand-copied near-clones of the
same scaffolding. The pattern is *implicit* — it exists 11–14 times as copies rather than once as
code. A human maintainer without AI assistance must reverse-engineer the convention from the clones,
and every clone is an opportunity for drift (several drift bugs have already occurred and been
patched — see F5).

**Top-line numbers** (verified 2026-06-10):

| Metric | Value |
|---|---|
| Duplicated per-type build functions in `Builder.fs` | **11** (~40 lines each, ~85% identical) |
| Near-clone `convertXToUnified` functions in `GenericBuilder.fs` | **14** |
| Build functions executed twice per build | **5** (`Program.fs`) |
| Times `convertPostsToUnified` runs per build | **3** |
| Largest modules | `GenericBuilder.fs` 2,077 / `Builder.fs` 1,651 / `LayoutViews.fs` 1,422 lines |
| Files touched to add one content type | **~8** (per `add-content-type` skill checklist) |

**Recommended posture**: three tiers, executed in order, each independently shippable and
reversible — (1) zero-risk waste removal, (2) flag-gated consolidation of the duplicated
scaffolding, (3) costed go/no-go decisions on bigger bets (content-type registry, eliminating
HTML re-parsing, search-contract generation — the Fable compiler pilot was decided **NO-GO**
2026-06-10 — and text-only unification). Full sequencing in the companion plan. A cross-cutting
**design doctrine** (§8) — composition-first, railway-oriented error handling, AI-grade
diagnostics — governs *how* every fix is implemented.

---

## 2. Method & Sources

- **Code**: every `.fs` file enumerated and sized; the build pipeline (`GenericBuilder.fs`,
  `Builder.fs`, `Program.fs`, `ASTParsing.fs`), views (`Views/*.fs`), and domain (`Domain.fs`)
  read directly. All citations below verified against the working tree at assessment time.
- **Verification provenance**: an initial sweep used parallel exploration agents; every
  finding's core evidence was then re-verified first-hand against the working tree (this is
  how the converter count was corrected 12 → 14 and the 3× post-conversion was discovered).
  F6's layout-clone claim, F7/F10's text-only regex rewriting, and F8's silent
  `| Error _ -> None` were each confirmed by direct read (`Layouts.fs:414–530`,
  `TextOnlyViews.fs:39–87`, `GenericBuilder.fs:105–120`).
- **External research** (per the repo's research-first doctrine): F# style guide and component
  design guidelines (Microsoft Learn) plus community consensus on type-identity idioms
  (fsharpforfunandprofit "Designing with types"; FSharp.UMX) via Perplexity — informs F5 and
  §7; ionide/Fornax architecture via DeepWiki as prior art for the generators-as-data pattern
  — informs F1 and B1.
- **Voice & ethos**: `_src/about.md`, `_src/posts/fosdem-2026-social-web-thoughts.md`,
  `_src/posts/indieweb-create-day-2025-07.md`, `_src/posts/down-the-slow-web-rabbit-hole.md`.
- **Prior art reconciled**: `docs/content-processor-optimization-spec.md`,
  `docs/adr/0001-unified-content-processing.md`, `docs/adr/0003-static-site-architecture.md`,
  AI Memex pattern entries (notably `pattern-content-type-taxonomy-mismatch`).

---

## 3. The Soul Constraints

Every recommendation was filtered through principles the site itself articulates. These are
*constraints on the refactor*, not decoration:

1. **"Start as a website, add social capabilities as components."** (FOSDEM 2026 post.)
   Progressive enhancement is the site's architectural religion. Refactors must be incremental
   layers, never big-bang rewrites.
2. **One identity, many post types.** The author explicitly intends to keep adding IndieWeb post
   types (reviews, RSVPs, media, and more — see the IndieWeb Create Day 2025 post). Therefore
   **the cost of adding a post type is the metric that matters most**.
3. **Own the whole stack.** Custom SSG, custom Webmentions, custom ActivityPub. Solutions that
   add heavyweight external dependencies or lock-in are disqualified.
4. **Slow / quiet web.** Text-only site targets <50KB/page; the main site is deliberately light.
   Anything that grows client-side weight needs extraordinary justification.
5. **Human-maintainable without AI.** The author's stated requirement for this engagement:
   if AI can no longer make changes, a competent F# developer should be able to read the
   structure, not archaeology. Explicit beats implicit; one copy beats eleven.
6. **"Don't let perfect be the enemy of good."** Ship small reversible improvements; present
   ambitious options honestly with cost/benefit rather than mandating them.

---

## 4. What Is Already Good (keep these)

- **The processor abstraction is right.** `ContentProcessor<'T>` (Parse / Render / OutputPath /
  RenderCard / RenderRss) + `buildContentWithFeeds` + `FeedData<'T>` is a clean, type-safe
  pipeline core (`GenericBuilder.fs:60–84`). The problem is *around* it, not *in* it.
- **Type-first domain.** `Domain.fs` defines every content type as records with YAML attribute
  mapping and an `ITaggable` interface — exactly the right F# instinct.
- **AST-based parsing** (`ASTParsing.fs`) with typed `ParseError` / `ParsedDocument<'T>` —
  a principled foundation that several later layers unfortunately bypass (see F7).
- **The validation culture.** `OutputComparison.fs` (hash-based output diffing), feature-flag
  migrations (8× proven per project archives), incremental builds. This existing discipline is
  what makes the refactor plan below *safe* — we reuse it as the harness.
- **Documentation density.** ADRs, docs/, AI Memex patterns, project archives. Few personal
  codebases have this much institutional memory. The refactor should *consolidate into* this
  system, not duplicate it.

---

## 5. Findings Catalog

Each finding: **What / Why it matters / Root cause (5 Whys) / How to fix / When / Risk & rollback /
Evidence**. Severity reflects impact on human maintainability and change cost, not "is it broken
today" — almost nothing is broken today.

---

### F1 — Eleven hand-copied build functions (HIGH)

**What.** `Builder.fs` contains 11 per-type build functions that are ~85% identical:
`buildSnippets` (1242), `buildWikis` (1284), `buildPresentations` (1420), `buildBooks` (1453),
`buildPosts` (1485), `buildNotes` (1524), `buildResponses` (1563), `buildMedia` (1613),
`buildAlbumCollections` (1652), `buildPlaylistCollections` (1695), `buildBookmarks` (1762).
Each repeats the same scaffold: *enumerate `.md` files → create processor → `buildContentWithFeeds`
→ per-item: mkdir, split tags, find related content, render view, wrap in layout, write
`index.html` → render index page → return `FeedData list`*. Compare `buildSnippets`
(`Builder.fs:1242–1281`) with `buildWikis` (`Builder.fs:1284–1323`): the diff is the source
directory, the processor, the view function, and the title string. Everything else is the clone.

**Why it matters.**
- Any cross-cutting change (e.g., adding related-content to all types, changing output layout,
  adding a build statistic) must be made 11 times and can silently miss one.
- A human reading `Builder.fs` cannot tell *which differences between builders are intentional*
  (wikis sort by title, books have no tags) *and which are drift* — the signal is buried in
  ~440 lines of repetition.
- It directly taxes the soul constraint #2: every new post type adds another clone.

**Root cause (5 Whys).**
1. Why 11 clones? → Each content type was added by copying the previous one.
2. Why copy? → The migration program (8× feature-flag migrations) optimized for "ship this type
   safely now," and copying the last-known-good builder was the safest single-step move.
3. Why didn't an abstraction emerge? → The generic core (`buildContentWithFeeds`) *did* emerge,
   but only for the parse/feed half; the page-writing half (directories, views, layout wrapper,
   index page) was never lifted because each migration ended when output parity was reached.
4. Why did migrations stop at parity? → Success criteria were output-equality per type, with no
   metric guarding *cost of the next type*.
5. Why no such metric? → The architecture evolved organically under "preserve functionality"
   discipline; "minimize change amplification" was never an explicit goal. This assessment makes
   it one.

**How to fix.** Introduce one generic driver plus per-type *data*:

```fsharp
type ContentTypeBuild<'T> = {
    SourceDir: string list              // e.g. ["resources"; "snippets"]
    OutputDir: string list
    Processor: ContentProcessor<'T>
    ItemView: 'T -> 'T array -> XmlNode // item + siblings (for related content)
    IndexView: 'T array -> XmlNode
    IndexTitle: string
    ItemTitle: 'T -> string
    SortIndex: ('T array -> 'T array) option  // wikis sort by title; default = none
}

let buildContentType (config: ContentTypeBuild<'T>) : FeedData<'T> list = ...
```

The 11 functions become 11 small config records + one driver. Intentional differences (wiki
sorting, book tag absence) become *visible fields* instead of buried diffs. Migrate one type at
a time behind a feature flag with `OutputComparison` parity gates — the codebase's own proven
migration pattern, applied to itself. Two design notes (see §8 doctrine): the driver is a
*composition* of small named phases (enumerate → parse → sort → render → write → report), and
it is born **Result-aware** — it collects parse failures (the existing
`ASTParsing.ParseError`) instead of discarding them, so F8's fix doesn't have to re-touch
eleven call sites later.

**When.** Phase 2 (after the zero-risk wins). Effort: L (largest single item, but mechanical
after the first two types prove the driver).

**Risk & rollback.** Medium risk, fully mitigated: per-type flag, hash-verified output parity,
old builder deleted only after its replacement ships green. Rollback = flip flag / revert one
squash commit.

**Evidence.** `Builder.fs:1242–1281` vs `1284–1323` (side-by-side clone); list of all 11 above.

---

### F2 — Fourteen near-clone `convertXToUnified` functions (HIGH)

**What.** `GenericBuilder.fs` `UnifiedFeeds` module has 14 conversion functions
(`GenericBuilder.fs:1922–2131+`): Posts, Notes, Responses, ResponseBookmarks, Snippets, Wikis,
AiMemex, Presentations, Books, Albums, AlbumCollections, PlaylistCollections, Bookmarks,
BookmarkResponses. `convertPostsToUnified` (1922–1933) and `convertNotesToUnified` (1935–1946)
are **byte-identical except the string `"posts"` vs `"notes"`**. All 14 share the skeleton:
`List.choose → match RssXml → extract title/url/content/date/tags → construct UnifiedFeedItem
with mostly-None extras`. Only four carry genuinely distinct logic: responses (subtype as
ContentType, TargetUrl, RSVP — 1948–1973), books (ReviewData), albums (MediaData), bookmark
variants (filtering + `"bookmarks"` type).

**Why it matters.** Same as F1 — 14 copies of one idea — plus a subtle correctness hazard:
the *interesting* divergences (responses carrying their subtype as `ContentType`, the
bookmark-filtering split) are invisible among boilerplate. The taxonomy-mismatch bug documented
in AI Memex (`pattern-content-type-taxonomy-mismatch`) happened precisely because a consumer
didn't know responses carry subtypes — the knowledge lived in one clone among 14.

**Root cause (5 Whys).** Same chain as F1 (copy-the-last-one during migrations); additionally:
the `UnifiedFeedItem` record grew fields (`ResponseType`, `RsvpStatus`, `ReviewData`,
`MediaData`) per phase, and each new field forced edits to all 14 constructors, which entrenched
the copying because editing 14 places "was the pattern."

**How to fix.** One generic function + tiny per-type projection:

```fsharp
let toUnified
    (contentType: string)
    (getCore: FeedData<'T> -> string * string * string * string array) // title,content,date,tags
    (getExtras: FeedData<'T> -> UnifiedExtras)   // defaults for most types
    (feedDataList: FeedData<'T> list) : UnifiedFeedItem list
```

Ten of the fourteen collapse to one-liners; responses/books/albums/bookmarks keep small explicit
projections — making the *real* differences the only code you read.

**When.** Phase 2, naturally paired with F1. Effort: M.

**Risk & rollback.** Low — pure function refactor, output-comparable item-by-item; old functions
retained until parity proven.

**Evidence.** `GenericBuilder.fs:1922–1946` (identical pair), `1948–1991` (the genuinely
distinct response/bookmark logic), grep count = 14.

---

### F3 — Five builders run twice; converters run up to three times (MEDIUM — pure waste)

**What.** `Program.fs` executes the full parse-render-write cycle twice for five content types:
`buildSnippets` (94, 215), `buildWikis` (95, 219), `buildPresentations` (97, 206),
`buildBooks` (98, 230), `buildMedia` (99, 234) — the second call's result discarded
(`let _ = buildX()`). Additionally `loadPosts` (43) re-parses what `buildPosts` (86) parses, and
the same `convertXToUnified` calls are repeated verbatim in `timelineFeedItems` (104–112),
`allUnifiedItems` (115–128), and `blogArchiveFeedItems` (131–135) — so `convertPostsToUnified
postsFeedData` executes **three times** per build.

**Why it matters.** Every build pays double parse/IO/render for five types and triple conversion
for three types. Worse for maintainability: a reader cannot tell whether the second `buildX()`
call is load-bearing (it isn't — it rewrites identical files) — which is exactly the kind of
mystery that paralyzes a future human maintainer ("can I delete this?").

**Root cause (5 Whys).** Build orchestration in `Program.fs` grew append-only: the unified-feed
section (81–161) was added *before* the older per-type build section (205–235) was cleaned up;
nothing failed, so the duplicates stayed. Append-only growth happened because `main` is one
252-line script with no structure signaling where a concern already lives.

**How to fix.** Capture each `FeedData` once at the top; delete the five `let _ = buildX()`
calls; bind the three converter result lists to `let` values and reuse. ~20-line diff, output
byte-identical by construction.

**When.** Phase 1 — first change made, because it also speeds up every subsequent
build-verify cycle. Effort: S.

**Risk & rollback.** Near-zero; single revertible commit; `OutputComparison` confirms identity.

**Evidence.** `Program.fs:43–65, 86–99, 104–135, 205–235`.

---

### F4 — Dead feature flag and unreachable legacy path (LOW)

**What.** `useUnifiedTagSystem = true` hardcoded with a comment saying "Change to true to test"
(`Program.fs:241`); the `else` branch calls legacy `buildTagsPages` (260–262) — unreachable.
Per the codebase's own migration doctrine, the flag should have been removed at cutover.

**Why it matters.** Dead paths must still compile, still read, and imply a choice that no longer
exists. For a human auditor every dead flag is a "wait, which one runs in prod?" tax.

**Root cause.** Migration completed but the cleanup step (Phase 4 of the documented migration
pattern) wasn't executed for this one.

**How to fix.** Delete flag + else-branch; delete `buildTagsPages` and any now-unused legacy
helpers it exclusively used.

**When.** Phase 1. Effort: S. **Risk:** near-zero (dead code removal; output identical).

**Evidence.** `Program.fs:240–262`.

---

### F5 — Content-type identity is stringly-typed and inconsistent (HIGH)

**What.** Content-type names are bare string literals scattered across the codebase, with at
least three inconsistent conventions in simultaneous use:
- `ITaggable.ContentType` returns **singular** (`"post"`, `"snippet"`, `"wiki"` —
  `Domain.fs:44,149,174`), while the unified feed uses **plural** (`"posts"`, `"snippets"` —
  `GenericBuilder.fs:1932`, `Program.fs:104–128`).
- Responses' `UnifiedFeedItem.ContentType` carries the **subtype** (`"reply"`/`"reshare"`/
  `"star"`/`"rsvp"`, `GenericBuilder.fs:1961`), not `"responses"` — consumers must know to
  normalize (documented bug: AI Memex `pattern-content-type-taxonomy-mismatch`).
- URL routing from content type is a hand-maintained `match` **embedded inside a view function**
  (`LayoutViews.fs:385–398`), separately re-implemented wherever permalinks are needed.

**Why it matters.** This is the finding most likely to cause the *next* production bug. String
identity means the compiler cannot help; every consumer re-derives the mapping; the
singular/plural and subtype quirks are tribal knowledge. It already caused at least one shipped
bug class.

**Root cause (5 Whys).**
1. Why strings? → YAML frontmatter and URLs are strings, so strings flowed inward.
2. Why never centralized? → Each consumer needed only its slice, and adding a local literal was
   always cheaper *that day* than building the registry.
3. Why subtype-as-ContentType? → ActivityPub/badge work needed subtype specificity and the
   `ContentType` field was the available slot (memex entry documents the decision).
4. Why singular vs plural drift? → `ITaggable` predates the unified feed; nothing forced
   agreement.
5. Why nothing forced agreement? → No single source of truth existed to disagree *with*.

**How to fix.** Two steps of increasing strength:

*Step 1 (Phase 1.3, mechanical)*: a `ContentTypes` literals module as single authority —
canonical names, response-subtype set + normalization, URL-prefix table. Zero behavior change;
kills literal scatter. Stated honestly: this (like any `type CtKey = string` abbreviation) is
**documentation, not enforcement** — F# type abbreviations are erased and unify freely with
`string`, so the compiler cannot catch a tag passed where a content type belongs.

*Step 2 (Phase 2.7, the actual type-safety upgrade)*: model identity as a closed DU. Options
analyzed (researched: F# style guide / Microsoft Learn, fsharpforfunandprofit
"Designing with types", FSharp.UMX):

| Option | Enforcement | Exhaustiveness on new case | Serialization friction | Verdict |
|---|---|---|---|---|
| Alias `type CtKey = string` | none (erased) | none | none | docs only — fine in signatures, never a safety mechanism |
| Single-case DU `CtKey of string` (+`[<Struct>]`) | nominal | none (inner string stays open) | converters/unwrapping | wrong axis: protects *identity*, not the *vocabulary* |
| UMX `string<contentType>` | nominal, zero-cost | none | none (erased to string) | good identity tool; doesn't model the closed set; adds a dependency |
| **Closed DU** `[<RequireQualifiedAccess>] ContentType = Posts \| Notes \| …` | full | **compile error at every non-exhaustive match** | boundary `parse`/`serialize` pair (YAML/URLs) | **recommended** |

The closed DU's exhaustiveness is the payoff that matters here: adding `| Recipes` makes the
compiler enumerate every site that must handle it — turning the 8-file human checklist into
compiler-guided squiggles, which is precisely the human-maintainability property this
assessment optimizes for. Cost: one `parse : string -> ContentType option` /
`serialize : ContentType -> string` pair at the YAML/URL boundary, plus the permanent
discipline of **no wildcard `_` branches** in content-type matches (a wildcard silently
disables the exhaustiveness you paid for — see §7). Supporting evidence that this is the
codebase's own latent intent: `Domain.fs:407–412` already defines
`type ResponseType = Reply | Star | Share | Bookmark | Rsvp` as a DU — and then the YAML layer
bypasses it with a raw string field (`ResponseDetails.ResponseType: string`, `Domain.fs:418`).
Step 2 finishes what `Domain.fs` started, and forces the subtype-taxonomy question (F5's core
disease) to be answered explicitly instead of smuggled through a string field. Phase 3's B1
registry then keys off the DU.

**When.** Phase 1.3 (literals module + mechanical replacement), Phase 2.7 (closed DU). Effort: M.

**Risk & rollback.** Replacement is mechanical with compiler verification + output comparison.
The plural/singular *unification* is the only risky sub-step and is deferred/flagged.

**Evidence.** `Domain.fs:44,102,149,174,206,316`; `GenericBuilder.fs:1932,1945,1961,1990`;
`Program.fs:104–128`; `LayoutViews.fs:385–398`.

---

### F6 — Duplicated view functions across the rendering layer (HIGH)

**What.**
- `feedPostView` vs `notePostView` (`ContentViews.fs:145–167`): identical except the
  `cardFooter "posts"/"notes"` argument. Likewise `individualFeedPostView` vs
  `individualNotePostView` (170–196).
- Five response body views (`replyBodyView`, `reshareBodyView`, `starBodyView`,
  `bookmarkBodyView`, `rsvpBodyView`, `ContentViews.fs:24–142`) repeat the same
  content-cleanup + timestamp-strip + card-body skeleton, differing only in icon, color, and
  microformat class.
- ~10 collection/index views (`CollectionViews.fs:32–260`) repeat "heading + description +
  `ul` + loop" with different item types.
- `defaultLayout` vs `defaultIndexedLayout` (`Layouts.fs:414–560`): verified near-clones; the
  only observed difference is the Reveal.js asset block (`Layouts.fs:487–490`). Drift symptom
  found during verification: the comment claims the indexed variant "allows indexing by search
  engines" (`Layouts.fs:475`), yet **both** variants emit the same
  `<meta name="robots" content="nosnippet">` (`Layouts.fs:451` and `:518`) — either the comment
  or the markup is wrong. Possible latent SEO bug; resolve intentionally during consolidation
  (plan 2.3).

**Why it matters.** Visual/markup changes (microformats, card structure, accessibility fixes)
must be repeated N times; drift between the clones becomes user-visible inconsistency. The
response-body quintet is the clearest case: icon/color/class are *data*, not five functions.

**Root cause.** Same copy-forward dynamic as F1/F2, plus: ViewEngine makes copying functions
frictionless, and no shared "card" component owned the post-like shape (only `cardHeader`/
`cardFooter` atoms exist in `ComponentViews.fs`).

**How to fix.** (a) One parameterized post-card view (content type key + optional webmention
section flag) replaces the four post/note variants. (b) One `responseBodyView` taking a small
`{ Icon; Color; MicroformatClass }` record (or derived from `ResponseType` DU) replaces five.
(c) One generic listing view taking item-render function replaces the collection clones where
genuinely identical. (d) `defaultLayout`/`defaultIndexedLayout` collapse to one function with
an `includeReveal: bool` (or asset-list) parameter.

**When.** Phase 2. Effort: M. **Risk:** low-medium; HTML output must remain byte-identical —
gate with `OutputComparison` per view migrated.

**Evidence.** `ContentViews.fs:24–196`; `CollectionViews.fs:32–260`; `Layouts.fs:414–560`.

---

### F7 — Generated HTML is re-parsed and rewritten with regex (HIGH)

**What.** The pipeline renders markdown → HTML, then *downstream* code re-parses that HTML with
regex to mutate it:
- Timeline view strips `<article>` open/close tags, `<h1>…</h1>`, and `<h2><a>…</a></h2>` via
  four chained `Regex.Replace` calls (`LayoutViews.fs:331–343`) — and the **same block is
  duplicated** ~70 lines later for the progressive-loading JSON path (`LayoutViews.fs:402–411`).
- Progressive-loading JSON is assembled with `sprintf """{"title":"%s"...}"""` plus a
  hand-written `escapeJson` (`LayoutViews.fs:372–415`) instead of `System.Text.Json`.
- Review images are extracted from rendered HTML by regex (`ContentViews.fs:11–21`).
- Response views regex-strip timestamp lines from already-rendered content
  (`ContentViews.fs:30–32` and four siblings).
- Text-only site rewrites `<img>`/`href` in rendered HTML via regex (`TextOnlyViews.fs:39–87`).

**Why it matters.** This is the most *fragile* code in the repo. Each regex encodes an
assumption about markup another module generates; change the generator and the regex silently
stops matching (no error — wrong output). It also violates the repo's own stated convention
(ViewEngine composition over string manipulation). For a human maintainer, "HTML goes in, regex
edits it, different HTML comes out" is the hardest possible code to reason about. The in-file
duplication of the cleaning block means the two timeline paths can drift independently
(one already nearly did — the h2-link rule has a fix comment in only one phrasing).

**Root cause (5 Whys).**
1. Why regex-strip? → The timeline needed card content *without* the article wrapper/title that
   `Render`/`CardHtml` already baked in.
2. Why was it baked in? → Processors render one final HTML string per item
   (`ContentProcessor.Render: 'T -> string`), a single representation serving pages, cards,
   feeds, and timeline alike.
3. Why one representation? → The original design predates the timeline/unified-feed features
   that needed *parts* of the content.
4. Why not change the processor interface when timeline arrived? → Feature-flag migrations
   compared final output strings; inserting a structured intermediate would have invalidated the
   comparison harness mid-migration. Regex-on-output was the locally-safe move.
5. Why does it persist? → It works, and nothing re-examined the seam after migrations completed.

**How to fix.** Introduce a structured render product so consumers compose instead of strip:

```fsharp
type RenderedContent = {
    BodyHtml: string      // content WITHOUT article wrapper / title heading
    Title: string
    // wrapper applied by the page/card/feed view that needs it
}
```

Processors produce `RenderedContent`; the article wrapper and headings become view-level
composition. The regex blocks, the duplicated cleaning, and the timestamp stripping all become
deletable. JSON via `System.Text.Json` serialization of a small record. This touches the
rendering core, so it's a Phase 3 bet (B2) with full output-diff review — *but* the two
cheapest slices (deduplicate the cleaning block into one function; replace sprintf-JSON with
typed serialization) are safe enough for Phase 2.

**When.** Phase 2 (dedupe + JSON), Phase 3 (structural fix, go/no-go). Effort: M + L.

**Risk & rollback.** The Phase 2 slices are output-identical-verifiable. The Phase 3 core change
is the second-riskiest item in this report (after nothing — B1 is comparable); it gets its own
flag, parity harness, and ADR.

**Evidence.** `LayoutViews.fs:331–343, 372–415`; `ContentViews.fs:11–21, 30–32`;
`TextOnlyViews.fs:39–87`.

---

### F8 — Parse failures are silent (MEDIUM severity, HIGH leverage)

**What.** `ASTParsing.fs` already does this *right*: every parse function returns
`Result<ParsedDocument<'T>, ParseError>` with a typed error DU
(`ParseError = YamlParseError | MarkdownParseError`, `ASTParsing.fs:15–17`) carrying real
exception detail. Then the rail is **severed**: every processor's `Parse` wrapper collapses
the `Result` to `Option` with `| Error _ -> None` — at **12 sites** in `GenericBuilder.fs`
(lines 120, 190, 271, 339, 420, 504, 786, 899, 1080, 1196, 1317, 1392). The error value — file,
stage, exception message — is constructed and then thrown away. Broad `try/with` blocks in
loaders continue the pattern (`Builder.fs:1635–1647` etc.). No end-of-build summary exists.

**Why it matters.** A malformed frontmatter date doesn't fail the build — the post just
*vanishes from the site*. The author might not notice for days. For the slow-web ethos,
silently losing a post is worse than a loud build failure. And for AI-assisted maintenance,
a swallowed error is maximally expensive: the agent must reproduce, instrument, and rediscover
context the codebase already had in hand at the `Error` branch.

**Root cause.** The bottom layer was built railway-style; the consumers chose `List.choose`
convenience over error aggregation. What's missing is not error *handling* but error
*transport* — the failure track was never connected to a reporter.

**How to fix.** This is a **reconnection**, not new architecture — in two steps (full design
in §8.2–8.3 and plan 1.4/2.8):

1. *Interim (plan 1.4, additive)*: count and print skipped files per type in the build summary
   using the `ParseError` already in hand.
2. *Full railway (plan 2.8, after the F1 driver exists)*: parse **all** files, accumulating
   failures instead of short-circuiting (builds want validation-style accumulation, not
   monadic first-error abort); successes proceed to render; failures render a structured,
   self-contained report block (file → got/expected → fix pointer; format contract in §8.3).
   **Policy decided 2026-06-10: report loudly, keep building** — one bad file must not block
   publishing the rest; an opt-in `--strict` flag (exit 1) is available for CI later.

**When.** Phase 1 diagnostics; Phase 2.8 full railway. Effort: S then M.

**Risk & rollback.** Step 1 is stdout-only; zero output risk. Step 2 verifies byte-identical
`_public/` when no files fail; revert restores the interim reporter.

**Evidence.** `ASTParsing.fs:15–17, 33–69, 144–172` (the existing rail);
`GenericBuilder.fs:120–1392` (the 12 severance sites); `Builder.fs:1635–1737`;
`OutputComparison.fs:37–47`.

---

### F9 — God modules (MEDIUM)

**What.** Four modules exceed 1,000 lines: `GenericBuilder.fs` (2,077 — processors *and*
unified feeds *and* RSS/JSON feed generation), `Builder.fs` (1,651 — page builders *and* static
pages *and* archive exports), `LayoutViews.fs` (1,422 — timeline *and* every bespoke page view),
`TextOnlyViews.fs` (1,069). The repo's own convention says module-per-concern and functions
<20 lines; `buildAllFeeds` alone spans ~150 lines (`GenericBuilder.fs:1707–1854`),
`timelineHomeViewStratified` ~470 (`LayoutViews.fs:219–694`).

**Why it matters.** Navigation cost for humans; merge-conflict surface; the F# compile-order
constraint makes oversized modules accrete further (everything's already in scope there, so new
code lands there too — self-reinforcing).

**Root cause.** Single-file growth is the path of least resistance under F#'s explicit
compile-order; splitting requires touching the `.fsproj` and re-thinking boundaries, which never
made it into any migration's "done" criteria.

**How to fix.** Mechanical extraction along seams that already exist as inner modules:
`UnifiedFeeds` → `UnifiedFeeds.fs`; per-type processors → e.g. `Processors/` (optional);
timeline views out of `LayoutViews.fs`. Order in `.fsproj` preserved. No logic changes —
file moves verified by identical output + clean build.

**When.** Phase 2 (after F1/F2 shrink the modules naturally — split what remains). Effort: M.

**Risk & rollback.** Low; mechanical; compiler-checked.

**Evidence.** Line counts measured 2026-06-10; `GenericBuilder.fs:1707–1854`;
`LayoutViews.fs:219–694`.

---

### F10 — Text-only site is a parallel rendering universe (MEDIUM)

**What.** `TextOnlyViews.fs` (1,069 lines) + `TextOnlyBuilder.fs` rebuild page chrome, content
lists, and per-type rendering separately from the main site, including their own regex HTML
rewriting (F7) and their own normalization of response subtypes (the memex-documented bug
locus). Main-site components are not reused.

**Why it matters.** Every content-type addition or rendering fix must be made twice; parity gaps
are invisible until a user on a flip phone hits one. The accessibility mission (a soul
constraint) deserves *derivation* from one model, not a hand-maintained mirror.

**Root cause.** Text-only was built as an additive feature against `UnifiedFeedItem`'s *final
HTML*, so it had to deconstruct HTML (regex) rather than render from structure — the same
upstream cause as F7.

**How to fix.** Downstream of B2: once `RenderedContent`/structured projections exist, text-only
becomes a second *renderer* over the same model, sharing the content-item projection and nav
data. Until then, limit to targeted dedupe (shared normalization function for response
subtypes — partially exists; shared nav data per F11).

**When.** Phase 3 (B4), explicitly sequenced after/with B2. Effort: L.

**Risk & rollback.** Output diffs expected (that's the point — eliminating drift), so this is
review-the-diff territory, not hash-parity: a deliberate Phase 3 decision.

**Evidence.** `TextOnlyViews.fs:39–87, 124–340`; `TextOnlyBuilder.fs`; memex
`pattern-content-type-taxonomy-mismatch`.

---

### F11 — Navigation has no single source of truth (MEDIUM)

**What.** Desktop nav hardcoded in `Layouts.fs:5–303`; text-only nav separately hardcoded
(`Layouts.fs:583–601`); per-page back/home links hand-written throughout `TextOnlyViews.fs`
(216–323, 381–443). Adding a section = editing markup in 2–3 places.

**Why it matters.** Direct tax on the most common site evolution after posting: adding a page or
collection to the menu. Drift here is user-visible (a page reachable on main but absent from
text-only nav).

**How to fix.** A `NavigationData` value (list of sections/links — the `NavigationStructure`
type in `Domain.fs:280–291` already sketches this!) consumed by both layout renderers. Markup
stays per-renderer (desktop nav ≠ text-only nav visually); the *data* unifies.

**When.** Phase 2. Effort: S–M. **Risk:** low; nav HTML output comparable by hash.

**Evidence.** `Layouts.fs:5–303, 583–601`; `TextOnlyViews.fs:216–323`; `Domain.fs:280–291`
(unused-in-nav type proving the intent existed).

---

## 6. Fable-the-Compiler Evaluation (costed)

**Question.** Should client-side code (currently hand-written vanilla JS: `search.js` with
Fuse.js, back-to-top, progressive timeline loading, theme toggle, QR display) be written in F#
via [Fable](https://fable.io), sharing types with the generator?

**Pros (real).**
- One language across the stack; aligns with "own the whole stack."
- Shared types where client and server genuinely share a contract — the strongest case is the
  **search index**: `SearchIndex.fs` generates JSON that `search.js` consumes; type drift between
  them is a real, current risk class. Same for the progressive-loading JSON (F7's sprintf-JSON).
- The F# you'd write is testable with the same tooling as the generator.

**Cons (also real).**
- Adds Node + Fable + a bundler to a currently **pure-dotnet build**. That's a second toolchain
  to keep alive for years — directly against human-maintainability unless the human is more
  fluent in F# than JS (most aren't; *every* web developer can read the current vanilla JS).
- The client codebase is small and stable (~hundreds of lines, few changes per year). Compiler
  overhead amortizes badly over so little code.
- Bundle output of Fable + fable-library runtime risks the <50KB slow-web budget on the
  text-only/lighter pages (mitigable, but it's vigilance you don't currently need).
- The JS already works. Rewrites of working code rank last under "don't let perfect be the
  enemy of good."

**Costed recommendation.**
- **Do not** adopt Fable wholesale; **do not** introduce it into the main build path. Juice not
  worth squeeze for ~hundreds of lines of stable JS.
- **The contained pilot we evaluated (search surface only)** — define the search-index item
  shape once in F#, compile the consuming search module via Fable side-by-side behind a flag —
  was the *most* defensible scope, and even there the marginal benefit over the alternative
  below is small. **Decided NO-GO (see decision block).**
- **Cheaper alternative that captures most of the benefit:** keep JS, but *generate* the shared
  contracts — emit a JSON Schema or a small generated `.d.ts`/validation stub from
  `SearchIndex.fs` types at build time. Type-drift protection without a second toolchain.
  Recommend this first.

> **DECISION (2026-06-10, author): NO-GO on the Fable compiler pilot.** Cons accepted as
> written. The contract-generation alternative remains the adopted recommendation, tracked as
> an optional item in the companion plan. No ADR needed for the no-go; this section is the
> record.

---

## 7. Extensibility Model — How Fixed Is the Result?

Maintainability and extensibility only conflict when abstractions are wrong. This section
states explicitly what the refactored architecture keeps open, what it deliberately closes,
and why the closures *help* extension rather than hinder it.

**Design stance: closed world, compiler-guided extension.** This is a single-maintainer,
compiled SSG whose content-type vocabulary is finite at any moment but grows over time. For
that shape, idiomatic F# consensus (F# style guide; Wlaschin's "Designing with types") says:
model the vocabulary as a closed DU, push strings to the boundary. "Closed" does not mean
rigid — adding a case is a one-line edit whose *cost is paid by the compiler, not the human*:
every `match` that must care becomes a compile error, a guided tour of exactly the places
needing attention. For this context that is strictly better extensibility than stringly-typed
"openness," where adding a type means grepping and hoping.

**What stays open (extension points designed into Phases 2–3):**
- `ContentTypeBuild<'T>.ItemView` / `IndexView` are *arbitrary functions* — a new type with a
  fully bespoke page keeps complete ViewEngine power; the driver owns only the boring flow.
- Types may **opt out** of the driver entirely (the plan's ">2 new config fields → STOP"
  rule): hand-written builders coexist as first-class citizens; 9/11 migrated is defined as
  success. The abstraction is a default, not a cage.
- Optional hooks (`SortIndex` now; more *only when a real need appears*, never speculatively)
  — open for extension via composition, closed for modification of the common flow.
- New *behaviors* (a new feed format, a new export target) extend by adding a consumer of
  `UnifiedFeedItem`/registry data — the same axis ActivityPub and the text-only site already
  occupy. The registry makes this easier, not harder: one table to enumerate instead of 11
  functions to spelunk.

**What deliberately closes:**
- Content-type identity (closed DU — F5 step 2). Permanent discipline: **no wildcard `_`
  branches in content-type matches**; a wildcard silently disables the exhaustiveness that
  makes the closure valuable.
- The common build-flow shape (enumerate → parse → render → write → index) — closed inside
  the driver precisely so fixing it once fixes it everywhere.

**Prior art** (DeepWiki: ionide/Fornax): mature F# SSGs use exactly this generators-as-data
pattern — `GeneratorConfig` records with `Trigger`/`OutputFile` DUs declared in `config.fsx`.
Fornax's documented weakness — *implicit* content types defined ad-hoc in scripts, with poor
discoverability — is the inverse of this codebase's strength (compiled `Domain.fs` schemas).
The registry plan combines the two: data-driven wiring **plus** compiled types, which neither
system has alone.

**Honest risk:** a wrong abstraction ossifies. Mitigations baked into the plan: two-pilot
proof before mass migration (plan 2.1), the STOP rule, opt-out coexistence, and Phase 3 bets
gated individually. If the driver fights the 10th content type, the plan says leave that type
bespoke — the abstraction serves the types, never vice versa.

---

## 8. Design Doctrine — Composition, Railways, and the AI-Human Flywheel

Three cross-cutting principles (added at the 2026-06-10 review, author direction) that govern
*how* every fix in §5 is implemented. They are not new findings; they are the style
constitution for the refactor — chosen because F# makes each of them cheap, and because
together they serve both audiences this assessment optimizes for: the future human maintainer
and the AI agent on a token budget.

### 8.1 Composition over inheritance

The principle (author direction, clarified 2026-06-10): vary behavior by **composing functions
and data, never by subclassing**. The unit of design is the small, named, total function;
features are pipelines of them.

- **What the temptation looks like here**: generalizing the 11 builders the OO way — an
  abstract `ContentBuilderBase<'T>` with virtual `Parse`/`Render` hooks and 11 subclasses.
  Rejected. The F1 driver is instead *one ordinary function* taking a **record of functions**
  (`ContentTypeBuild<'T>` *has-a* `ItemView`; nothing *is-a* `Builder`). Variation lives in
  data, flow lives in one place, nothing dispatches virtually — and a reader can see every
  behavior a type gets by looking at one record value.
- **The codebase already votes this way** (verified): its homegrown abstractions are records
  of functions (`ContentProcessor<'T>`, `GenericBuilder.fs:60–84`); there are no self-made
  class hierarchies. The two apparent exceptions are principled and should stay contained:
  - `ITaggable` (`Domain.fs:13–18`, implemented by 11 records) — a single **shallow**
    interface used as a constraint, not a hierarchy. Keep it shallow; the projection
    alternative (map early to a common record, exactly what `UnifiedFeedItem` does) is the
    preferred direction, and B1's registry may subsume `ITaggable` entirely.
  - `CustomBlocks.fs` (`inherit ContainerBlock`/`BlockParser`/`HtmlObjectRenderer<_>`) —
    **host-library boundary**: Markdig's extension API requires subclassing. Acceptable
    because mandatory; never let that style leak inward into our own designs.
- **The driver is a composition**, not a framework: `enumerate → parseAll → sort → renderAll →
  writeAll → report` — each phase an ordinary function, independently testable and replaceable.
- **Corollary — composition over flags**: when a shared function must behave differently for
  one caller, extract a smaller function and compose — don't add a boolean parameter. A flag
  is a branch every future reader must simulate; a composed function is a name they can trust.
  (F1's `SortIndex` field; F6's parameterized views.)
- This codifies what the repo's conventions already gesture at (module-per-concern, ≤20-line
  functions) into the refactor's default move.

### 8.2 Railways where they pay (Result-oriented pipelines)

The codebase already laid the first rail: `ASTParsing.fs` returns
`Result<ParsedDocument<'T>, ParseError>` with a typed error DU (`ASTParsing.fs:15–17, 144+`).
The rail is severed at 12 `| Error _ -> None` sites in `GenericBuilder.fs` (F8). **ROP here is
a reconnection job, not new architecture.** Doctrine:

- **Result for expected domain failures** — frontmatter parsing, taxonomy/content-type
  validation, field validation. These are the failures a maintainer routinely causes and must
  routinely diagnose.
- **Exceptions stay for exceptional failures** — disk full, network down. Wrapping IO in
  `Result` adds ceremony, not safety (Wlaschin's own caveat against over-Result-ing). Do not
  Result-wrap the write phase.
- **Accumulate, don't short-circuit**: a build over ~1,200 files wants validation-style error
  *accumulation* (parse everything, collect every failure, report once), not monadic bind
  (first error aborts). The success track proceeds to render; the failure track feeds the
  report. `Result.partition`-style folds, not `result { }` blocks, at the batch boundary.
- **Policy (decided 2026-06-10): report loudly, keep building.** One malformed note must not
  block publishing the other 1,199 files — for a daily-publishing site, blocked deploys are a
  second disease, not a cure for the first (silent loss). Opt-in `--strict` (exit 1 on any
  content error) reserved for CI when wanted.
- **Discipline**: from plan 2.8 onward, `| Error _ -> ...` that *discards the error value* is a
  review-blocker in this codebase. The error was constructed for a reason; transport it.

### 8.3 Diagnostics for the AI-human flywheel

Goal: when something breaks, the error itself contains everything needed to formulate the fix —
for a human at a terminal or an agent on a token budget. No exploratory grepping. Three layers,
cheapest and strongest first:

1. **The compiler as checklist.** Closed DUs + exhaustive matching (F5/2.7) make every addition
   enumerate its own work. Enforce it: promote FS0025 (incomplete-match) from warning to
   **build error** (plan 1.5). Verified 2026-06-10: the build emits exactly one warning — the
   documented FS1104 — so this is free today and pays compounding dividends after 2.7. The
   no-wildcard discipline (§7) remains necessary alongside (a `_` branch *satisfies* FS0025
   while defeating its purpose).
2. **Errors as values, with fix pointers.** Each failure carries *where* (file), *what* (got),
   *expected* (the valid set), and *the authority to edit* (file:line). Sketch:

   ```fsharp
   [<RequireQualifiedAccess>]
   type ContentError =
       | ParseFailure       of file: string * stage: string * detail: string
       | UnknownContentType of file: string * got: string * valid: string list
       | UnknownResponseType of file: string * got: string * valid: string list
       | MissingField       of file: string * field: string * hint: string
   ```

   Rendered self-contained:

   ```
   ✗ UnknownResponseType: _src/responses/foo.md
       got 'boost' — valid: reply | reshare | star | rsvp
       fix: correct 'response_type' in the file, or extend ResponseType (Domain.fs:407)
   ```

   One block = one complete fix context. Token-efficient by construction: an agent reads the
   block and acts; it never spends a search loop reconstructing what the build already knew.
3. **Output contract: terse success, verbose structured failure.** Success output stays
   summary-level (this is the repo's existing Professional Build Output convention — the
   doctrine aligns, not fights). Failures print the full structured block. Optional
   later-if-wanted: a machine-readable `build-report.json` for direct agent consumption —
   deliberately *not* scheduled until stdout blocks prove insufficient.

**The flywheel**: compiler + structured errors shorten every fix loop → resolved gotchas get
captured as AI Memex patterns (existing system, ask-first convention) → captured patterns
improve the next session's priors → fewer and faster failures. The Memex is the knowledge half
of the loop; this doctrine builds the diagnostics half the code was missing.

---

## 9. Prioritized Roadmap (summary — full detail in companion plan)

| Phase | Theme | Findings | Risk posture | Output contract |
|---|---|---|---|---|
| 0 | Safety harness | — | none | baseline snapshot + `OutputComparison` gate + umbrella branch + warning inventory |
| 1 | Zero-risk waste removal | F3, F4, F5(start), F8(diag), FS0025→error (§8.3) | conservative | byte-identical |
| 2 | Consolidation | F1, F2, F6, F7(slices), F9, F11, F5(closed DU), F8(railway, §8.2) | balanced, flag-gated | byte-identical per migrated unit |
| 3 | Big bets (each go/no-go) | B1 registry (F1/F5 capstone), B2 structured rendering (F7), B3 contract-gen (Fable pilot: **NO-GO** 2026-06-10), B4 text-only unification (F10) | ambitious, reviewed diffs | reviewed/approved diffs |

**When** is expressed as strict phase ordering (each phase ships independently); effort sizes
(S/M/L) are per-finding above. Phase 1 is deliberately small enough to complete in one sitting;
Phase 2 items are independent of each other and can be cherry-picked; Phase 3 items each begin
with a written go/no-go using the cost/benefit framing in this report.

## 10. Decision Log Hooks

Load-bearing decisions to capture as ADRs *when made* (not before):
- **ADR-0006**: Generic content-type build driver (F1/F2 consolidation approach).
- **ADR-0007**: Structured render product / elimination of HTML re-parsing (B2).
- **ADR-0008** (only if pursued): search-contract generation approach. (Fable compiler pilot:
  **NO-GO** decided 2026-06-10 — recorded in §6 and the companion plan; no ADR needed.)

---

*Assessment by the Fable consultant engagement, 2026-06-10. All file:line citations verified
against the working tree on that date. Numbers will drift as the plan executes; the companion
plan tracks live status.*
