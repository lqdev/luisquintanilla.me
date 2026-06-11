# Architecture Refactor 2026 — Phased Execution Plan

**Project**: Streamlining the F#/.NET static site generator
**Priority**: High | **Complexity**: Large (phased into S/M/L independently-shippable units)
**Source assessment**: [`docs/architecture-assessment-2026.md`](../../docs/architecture-assessment-2026.md) — findings F1–F11, bets B1–B4
**Status**: `[>]` Active — Phase 0 complete; **Phase 1 complete** (1.1–1.5 done, all byte-identical); **Phase 2 in progress** (2.1 build driver complete — all 11/11 builders migrated, byte-identical; 2.2 generic `toUnified` complete — 8 converters collapsed, byte-identical; 2.3 view dedupe complete — post-cards/response-bodies/layouts consolidated, byte-identical; 2.4 F7 slice (a) complete — `cleanCardHtml` unifies 4 copies, byte-identical; slice (b) STJ swap deferred to B2; 2.5 module splits complete — `UnifiedFeeds.fs` + `Views/TimelineViews.fs` extracted, byte-identical; 2.6 nav data complete — `Views/Navigation.fs` single source for desktop nav + 3 dead orphans removed, byte-identical; 2.7 ContentType DU complete — closed `[<RequireQualifiedAccess>] type ContentType` + `parse`/`serialize`, 2 dispatch sites exhaustive/no-wildcard, response subtypes kept on `ResponseType`, byte-identical)
**Last updated**: 2026-06-10

> Read the assessment first. This plan is the *how/when*; the assessment is the *what/why*
> (including 5-whys root causes, evidence citations, and the Fable-the-compiler evaluation).
> Every phase honors the soul constraints (assessment §3) and the **design doctrine**
> (assessment §8: composition-first, railway error handling, AI-grade diagnostics):
> progressive enhancement, cheap new post types, own the stack, slow web, human-maintainable
> without AI.

---

## Operating Rules (apply to every phase)

1. **Git**: long-lived umbrella branch `refactor/architecture-2026`. Work branches squash-merge
   into the umbrella (one logical change per squash = one-revert rollback). Umbrella merges to
   `main` with a **merge commit** (never squash), then fast-forward umbrella back to stay even.
2. **Output contract**: Phases 1–2 must produce **byte-identical `_public/` output** (verified
   via baseline snapshot diff) unless a step explicitly says "reviewed diff". Phase 3 bets are
   reviewed-diff territory by design.
3. **Build gate**: `dotnet build` clean (the single pre-existing `FS1104` warning in
   `ActivityPubBuilder.fs` is expected) + `dotnet run` completes after *every* commit-able unit.
4. **Stop-the-line**: any unexplained output diff halts the step; investigate or revert. Never
   stack a second change on an unverified first one.
5. **Conventions**: type-first (`Domain.fs`), ViewEngine over sprintf, module-per-concern,
   fully-qualified types, functions ≤20 lines where practical, `.fsproj` compile order updated
   with any new file.
6. **Composition over inheritance** (assessment §8.1): vary behavior with records of
   functions, higher-order functions, and generics — never new abstract classes or interface
   hierarchies (subclassing only where a host library's extension API demands it, e.g. Markdig
   in `CustomBlocks.fs`). Corollary: extract-and-compose beats boolean flags.
7. **Diagnostics contract** (assessment §8.3): success output stays terse; failures print
   structured, self-contained blocks (file → got/expected → `fix:` pointer). From step 2.8
   onward, any `| Error _ -> ...` that discards the error value is a review-blocker.

### Verification harness (used everywhere)

```powershell
# 1. Baseline (once per phase start, from clean main/umbrella):
dotnet run                          # generates _public/
Copy-Item _public _public_baseline -Recurse

# 2. After each change:
dotnet build && dotnet run

# 3. Compare (byte-identical expectation):
# Hash every file in both trees and diff the manifests
Get-ChildItem _public -Recurse -File | ForEach-Object {
  [pscustomobject]@{ Path = $_.FullName.Substring((Resolve-Path _public).Path.Length); Hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
} | Sort-Object Path | Export-Csv after.csv
# (same for _public_baseline -> before.csv) ; then:
Compare-Object (Import-Csv before.csv) (Import-Csv after.csv) -Property Path, Hash
# Expect: no output. Any row = investigate.
```

Note: if any build steps embed timestamps/nondeterminism, identify them in Phase 0 and exclude
those files from the byte-identical contract explicitly (documented list, reviewed once).

**Documented nondeterminism exclusions (from Phase 0.4 — the complete list):**
- `resources/ai-memex/graph.json` — only `stats.generatedAt` (UTC build timestamp;
  `KnowledgeGraph.fs:527`) varies between runs; graph structure is deterministic. Exclude this
  one file from the byte-identical diff (the harness `Compare-Object` should filter
  `Path -ne '\resources\ai-memex\graph.json'`), or strip/normalize `generatedAt` before hashing.
  No other file in the 13,486-file tree is nondeterministic (RSS feeds included).

---

## Executor Recipe — how to run any step (read once; applies to every step)

Audience: **any executor with zero prior context** — a junior developer or an AI agent. Each
step below is a self-contained task; this recipe is the shared choreography, kept here once so
the steps stay terse. Phases 0–1 are executable as written; Phase 2 steps become mechanical
after their pilots (see the Phase 2 preamble); Phase 3 bets are decision-gated and are *not*
junior tasks until their go/no-go note is written.

1. **Start**: `git checkout refactor/architecture-2026 && git pull`, then create a work branch
   `git checkout -b refactor/<step-id>` (e.g. `refactor/1.3-content-types`).
2. **Read first**: the step's linked assessment finding (its F-number) — it carries the *why*
   and exact file:line evidence. Don't start a step you can't restate in one sentence.
3. **Implement exactly the step's scope.** Out-of-scope discoveries go in the ledger Notes
   column — never fixed opportunistically (stop-the-line, rule 4).
4. **Universal Definition of Done** (every step, in addition to its own **Verify** line):
   - [ ] `dotnet build` clean — only `FS1104` expected (and from 1.5 onward, FS0025 is an error)
   - [ ] `dotnet run` completes
   - [ ] Hash-manifest diff vs baseline is empty — or the step explicitly says "reviewed diff",
         in which case attach the diff summary to the PR/commit message
   - [ ] The step's own **Verify** line satisfied
   - [ ] Status Ledger row updated (status, dates, notes)
5. **Merge**: squash into the umbrella with message `refactor(<step-id>): <summary>`.
6. **Rollback** (any time): `git revert <squash-sha>` — one squash per step keeps this always
   true.
7. **Stuck or surprised**: stop. Record what you saw in the ledger Notes, ask the author.
   Never stack a second change on an unverified first one.

---

## Phase 0 — Safety Harness (S; prerequisite for all)

**Goal**: a trustworthy baseline + rollback rails. No production code changes.

**Status: COMPLETE (2026-06-10).** Umbrella branch `refactor/architecture-2026` created from
clean `main` (after committing an FSharp.Core lock bump 10.1.300→10.1.301 — the local SDK
forces it; `dotnet restore --locked-mode` failed with NU1004 until regenerated via
`--force-evaluate`, then committed so `main` builds reproducibly).

- [x] 0.1 Umbrella branch `refactor/architecture-2026` created from up-to-date `main`.
- [x] 0.2 `dotnet build -c Release`: **succeeded in 14.9s**. `dotnet run -c Release` (full site
      generation): **~110s**, **13,486 files** in `_public/`. (Baseline metrics for Phase 1.1 delta.)
- [x] 0.3 Snapshot `_public/` → `_public_baseline/` (gitignored — added `_public_baseline/`,
      `_public_second/`, `before.csv`, `after.csv` to `.gitignore`).
- [x] 0.4 Second fresh `dotnet run` + SHA-256 hash-manifest diff over all 13,486 files:
      **exactly ONE nondeterministic file** — `resources/ai-memex/graph.json`. Sole varying field
      is `stats.generatedAt` (a UTC build timestamp; source `KnowledgeGraph.fs:527`,
      `let now = DateTimeOffset.UtcNow.ToString("o")`, emitted at `:543`). The graph structure
      (nodes/edges/clusters/counts) is byte-identical; only the timestamp differs. RSS feeds and
      everything else are already deterministic. **Documented contract exclusion (the only one):**
      `resources/ai-memex/graph.json`. The verification harness must exclude this single file
      (or normalize/strip `generatedAt`) from the byte-identical comparison in Phases 1–2.
- [x] 0.5 `OutputComparison.fs` confirmed present and usable (`compareOutputs`/`validateOutputs`,
      MD5 + line-level diff) as an alternative to the PowerShell harness. The PowerShell hash
      harness is the primary tool (it found 0.4's exclusion); `OutputComparison.fs` is the
      in-repo fallback.
- [x] 0.6 Warning inventory: **exactly 1** warning — `FS1104` at `ActivityPubBuilder.fs:805`
      (the documented expected one), **zero FS0025**. Precondition proof for step 1.5 holds.

**Done when**: two consecutive builds diff clean (modulo documented exclusions); baseline time
recorded; umbrella branch pushed. ✅ **Met** — clean modulo the single `graph.json` exclusion;
baseline build 14.9s / generate ~110s / 13,486 files recorded; umbrella branch pushed.

---

## Phase 1 — Conservative Quick Wins (S–M; output byte-identical; GREEN-zone)

Independent steps; each is its own squash commit. Order chosen so each speeds up the next.

### 1.1 Kill the double-builds (F3) — ✅ DONE (2026-06-10)

**Result: byte-identical output verified** (modulo the documented `graph.json` exclusion).
Implemented in three independently hash-verified sub-steps:
- **B (converters bound once)**: all 12 `convertXToUnified` results bound to `let` values
  immediately after the `FeedData` captures; `timelineFeedItems` / `allUnifiedItems` /
  `blogArchiveFeedItems` now reference the bindings (posts/notes/responses no longer convert 3×).
- **A (duplicate builder calls)**: deleted **4 of 5** `let _ = buildX()` duplicates
  (`buildPresentations`, `buildSnippets`, `buildWikis`, `buildMedia`).
  **`buildBooks()` duplicate RETAINED** — discovered entanglement: `StarRating` SVG gradient IDs
  come from a **global mutable counter** (`BlockRenderers.fs:85`, also `CustomBlocks.fs:454`), so
  the shipped review pages' IDs depend on this being the last render pass. Removing it is
  cosmetic-only (IDs shift `halfGrad16`→`halfGrad5`) but breaks byte-identity. Eliminating this
  last duplicate requires making the gradient IDs page-local/deterministic first — **logged for
  the StarRating cleanup (relates to F7/B2)**. An explanatory comment marks the retained call.
- **C (re-parse dedup)**: removed the top-of-`main` `loadPosts`/`responses` re-parses and the
  **dead** `feedNotes` binding (referenced only in a commented-out line); `posts`/`responses` for
  tag pages now derive from `postsFeedData`/`responsesFeedData`. Confirms `loadPosts` and the
  `PostProcessor` path produce equivalent tag-page output.
- **Timing**: build ~15s (unchanged — compilation). Generation wall-clock is I/O- and
  entity-extraction-dominated and noisy across single samples (~110–135s); the F3 win is the
  removal of redundant work, not a guaranteed wall-clock drop.
- **Verify**: ✅ byte-identical. **Rollback**: one revert of the 1.1 squash commit.

### 1.2 Remove the dead tag-system flag (F4) — ✅ DONE (2026-06-10)
- `Program.fs`: deleted `useUnifiedTagSystem` flag + the unreachable `else` branch; the unified
  tag path now runs unconditionally.
- `Builder.fs`: deleted the now-orphaned legacy `buildTagsPages` (grep-verified single caller was
  the deleted `else`). Cascade-removed its exclusively-used helpers: `individualTagView`
  (`Views/TagViews.fs`) + its `Views/Partials.fs` re-export, and `processTaggedPost`,
  `processTaggedResponse`, `getTagsFromPost`, `getTagsFromResponse` (`Services/Tag.fs`). Kept
  `processTagName` (shared) and the `*Unified` views/processing (live path).
- **Net −150 lines** of dead code. **Verify**: ✅ byte-identical; build clean.
  **Rollback**: one revert of the 1.2 squash.

### 1.3 Introduce the ContentType source of truth (F5 — step 1 of 2) — ✅ DONE (2026-06-10)
- New `ContentTypes.fs` (compiles right after `Domain.fs`): `[<Literal>]` canonical names
  (Posts/Notes/Responses/Bookmarks/Snippets/Wiki/Presentations/Reviews/Media/Streams/AiMemex/
  AlbumCollection/PlaylistCollection), response-subtype literals (Reply/Reshare/Star/Rsvp/Bookmark)
  + `ResponseSubtypes` set + `normalizeResponseSubtype`, and `urlPrefix` (the permalink table
  lifted verbatim from `LayoutViews.fs`).
- Mechanically swapped literals → `ContentTypes.*` in: all `GenericBuilder` `convert*ToUnified`
  ContentType stamps; the `typeConfigurations` RSS feed cluster (tuple keys + `Some ct`); the
  `buildJsonFeeds` typeConfigs + `responseStreamContentTypes` set + `toJsonFeedContent` match;
  the responses-feed subtype filter (`[star;reply;reshare;responses]`, order preserved); the
  `<> ai-memex` syndication guards; bookmark response-subtype comparisons; the two
  `LayoutViews` permalink matches (now call `ContentTypes.urlPrefix`); and the three Program.fs
  feed-item lists (`timeline`/`allUnified`/`blogArchive`) + the ActivityPub `<> ai-memex` guard.
- **Deliberately NOT touched** (deferred to 2.7/B1 — *behavior* decisions, not mechanical):
  `allTaggableContent`'s `"wikis"` (plural) key — differs from the feed vocab's `"wiki"`; changing
  it would alter output. Builder.fs path/dir literals (filesystem-layout concern, not
  `UnifiedFeedItem.ContentType` identity). `ITaggable.ContentType` singular values.
- Honest scope: `[<Literal>]`s erase to `string` → **documentation, not enforcement**. The
  closed-DU enforcement upgrade is 2.7; this just collects the scatter so that swap is mechanical.
- **Verify**: ✅ byte-identical (0 diffs). Build clean (1 expected FS1104). **Rollback**: one revert.

### 1.4 Build summary for skipped files (F8 — interim diagnostics only) — ✅ DONE (2026-06-10)
- Added skip reporting in `GenericBuilder.buildContentWithFeeds` — the universal choke point all
  12 content builders (+ Loaders.fs) route through, so one edit covers every type with zero
  call-site churn (no label param → scripts/tests untouched). Partition parsed vs skipped; if any
  parse returned `None`, print `⚠ N file(s) skipped (… content omitted …):` + each skipped path
  (the path conveys the content type). Restructured `List.choose` → `List.map` + `List.choose snd`;
  the returned `FeedData` list is element- and order-identical.
- **Verify**: ✅ byte-identical (0 diffs). Full regenerate emitted **zero** skip lines (every `.md`
  parses today) → no output noise in the common case and no hidden intentional-`None` cases.
- **Interim only** — typed accumulation + fix-hint blocks + `--strict` is step 2.8. **Rollback**: one revert.

### 1.5 Compiler strictness: incomplete matches become build errors — ✅ DONE (2026-06-10)
- `PersonalSite.fsproj`: added `<WarningsAsErrors>FS0025</WarningsAsErrors>`.
- **Verify**: build clean — exactly 1 warning (FS1104) and **0 FS0025**, so the flag fires on
  nothing today. Byte-identity holds **by construction**: a diagnostic-*severity* flag with zero
  matching instances cannot change the emitted assembly, hence cannot change `_public/` (full
  hash-verify deferred to 1.4, which is the step that actually adds code). **Rollback**: one revert.
- Why it matters: the payoff compounds after 2.7 — adding a case to the closed `ContentType` DU
  turns every non-updated `match` into a **compile error**, making the compiler the
  add-a-content-type checklist. Note: a wildcard `_` branch satisfies FS0025 while defeating it —
  the no-wildcard review discipline (operating rule 7 / assessment §7) stays in force.

**Phase 1 done when**: all five steps merged to umbrella, each hash-verified; build-time delta
recorded; umbrella merged to `main` (merge commit) if desired at this checkpoint.

---

## Phase 2 — Balanced Consolidation (M–L; flag-gated; output byte-identical per migrated unit)

Items independent of each other — cherry-pick order, though 2.1→2.2 is the natural pairing and
**2.8 requires 2.1**. Each unit: implement alongside old path → flag/parallel-compare → cut
over → delete old path.

**Junior-readiness strategy — pilot-then-mechanical**: each consolidation's first instance
(the pilot) is done carefully once and reviewed; every subsequent instance copies the pilot
commit as a literal template. The pilots are the skilled work; the repetitions are
deliberately mechanical.

### 2.1 Generic build driver (F1)
- New `ContentTypeBuild<'T>` record + `buildContentType` driver in a new `BuildDriver.fs`
  (insert in `.fsproj` after `GenericBuilder.fs`). Skeleton (assessment F1 + §8.1–8.2; final
  shape at pilot):
  ```fsharp
  type ContentTypeBuild<'T> = {
      Name: string                                  // ContentTypes literal (1.3)
      SourceDir: string list                        // e.g. ["resources"; "snippets"]
      OutputDir: string list
      Processor: GenericBuilder.ContentProcessor<'T>
      ItemView: 'T -> 'T array -> XmlNode           // item + siblings
      IndexView: 'T array -> XmlNode
      IndexTitle: string
      ItemTitle: 'T -> string
      SortIndex: ('T array -> 'T array) option      // wikis sort by title; None = source order
  }

  /// Composition of small named phases (assessment §8.1), born Result-aware (§8.2):
  /// enumerate → parseAll → sort → renderAll → writeAll → report
  /// parseAll keeps the failure track (ASTParsing.ParseError) instead of dropping it.
  let buildContentType (cfg: ContentTypeBuild<'T>)
      : GenericBuilder.FeedData<'T> list * (string * ASTParsing.ParseError) list = ...
  ```
  Until 2.8 lands, the returned failure list feeds the 1.4 reporter (same output); 2.8 then
  upgrades it to `ContentError` blocks without re-touching the eleven call sites.
- Migration order (easiest→hardest, two pilots prove the driver):
  1. `buildSnippets` (pilot 1 — simplest clone)
  2. `buildWikis` (pilot 2 — proves `SortIndex` variation)
  3. `buildPosts`, `buildNotes`, `buildResponses`, `buildBookmarks`
  4. `buildPresentations`, `buildBooks` (proves no-tags variation)
  5. `buildMedia`, `buildAlbumCollections`, `buildPlaylistCollections` (most bespoke — if the
     driver needs >2 new config fields to absorb one of these, STOP and leave that builder
     hand-written with a comment; do not force the abstraction. 9/11 migrated is success.)
- Per type: add config; run driver in place of old function; hash-verify; squash; next.
- **Prior art** (researched via DeepWiki): ionide/Fornax drives generation from
  `GeneratorConfig` records (`Trigger`/`OutputFile` DUs) — the generators-as-data pattern is
  proven in mature F# SSGs; we add what Fornax lacks (compiled domain types per content type).
- **Capstone**: when all migrated, old per-type functions deleted; `Builder.fs` shrinks ~400 lines.
- **ADR-0006** written when pilot 2 proves the design (use `docs/adr/template.md`).
- **Rollback**: per-type revert; driver itself reverts cleanly if pilots fail.

**STATUS: COMPLETE (byte-identical, 0 diffs each step).** Shipped `BuildDriver.fs`
(`ContentTypeBuild<'T>` + `buildContentType`), inserted after `Views/Generator.fs`
(not after `GenericBuilder.fs` — the driver calls `ViewGenerator.generate`, so it must
compile after Generator.fs and before Builder.fs). **All 11/11 builders migrated**
(exceeded the 9/11 target; STOP rule never triggered). Deviations from the sketch above,
all deliberate:
- Index fields grouped into an **optional** `Index: IndexConfig<'T> option` (View/Title/Sort)
  so `buildBookmarks` can declare `Index = None` (its landing page comes from responses).
  This absorbed the only "no-index" case without a new top-level field.
- Driver guards the source dir with `Directory.Exists` → `[]` (preserves albums/playlists safety).
- `media`/`album`/`playlist` use an AST-rendered `ItemView` by closing over a pre-bound
  `processor` (`processor.Render content |> convertMdToHtml`).
- **Return type kept as `GenericBuilder.FeedData<'T> list`** (NOT the `* ParseError list`
  tuple sketched above). The railway/Result-aware upgrade is deferred to **2.8**, which will
  change the driver's return shape in one place without re-touching the 11 call sites.
- ADR-0006 written and indexed.

### 2.2 Generic `toUnified` (F2)
- Add `UnifiedExtras` defaults + generic projection (assessment F2 sketch). Collapse the ~10
  trivial converters; keep explicit small projections for responses / bookmarks / books / albums
  (their divergence is the documentation).
- Hash-verify per converter swapped. **Rollback**: per-converter revert.

**STATUS: COMPLETE (byte-identical, 0 diffs vs umbrella tip).** Added private
`UnifiedExtras` record + `defaultExtras`, `arrayTags`/`splitTags` helpers, and a generic
`toUnified (contentType) (getCore: FeedData<'T> -> string*string*string*string[]) (getExtras: FeedData<'T> -> UnifiedExtras) feedDataList`
in `GenericBuilder.fs`. Collapsed **8** trivial converters (posts, notes, snippets, wikis,
ai-memex, presentations, album-collections, playlist-collections). Kept **6** explicit
(responses, responseBookmarks, bookmarkResponses, books, albums, bookmarks) — genuine
divergence (dynamic ContentType from response subtype + filters; CardHtml + ReviewData;
MediaData; TargetUrl from BookmarkOf). F# gotcha: annotate the `getCore` lambda param
`(fun (fd: FeedData<X>) -> …)` so `'T` pins before the lambda body is checked.

### 2.3 View dedupe (F6)
- `postCardView` parameterized by `(feedKey: string, withWebmention: bool)` replaces
  `feedPostView`/`notePostView`/`individualFeedPostView`/`individualNotePostView`
  (`ContentViews.fs:145–196`).
- `responseBodyView (style: ResponseStyle) (post: Response)` where
  `ResponseStyle = { Icon: string; Color: string; MfClass: string }` derived from response type
  replaces the five bodies (`ContentViews.fs:24–142`). The shared cleanup/timestamp-strip moves
  to ONE helper (interim — B2 deletes it entirely later).
- Collapse `defaultLayout`/`defaultIndexedLayout` (`Layouts.fs:414–560`) into one function with
  an asset-bundle parameter (e.g. `includeReveal: bool`).
- Collection-view consolidation only where genuinely identical (resist over-abstracting).
- Hash-verify per view swapped. **Rollback**: per-view revert.

**STATUS: COMPLETE (byte-identical, 0 diffs).** `postCardView (feedKey) (withWebmention)`
replaces the four post/note bodies (4 named wrappers retained for call sites). Four
response bodies (reply/reshare/star/bookmark) fold into `responseBodyView (style: ResponseStyle)`
+ a shared `cleanResponseContent` helper; **`rsvpBodyView` kept explicit** (p-rsvp span +
" to " + `target=_blank` is genuine divergence — same "divergence is the documentation"
rule as 2.2). `defaultLayout`/`defaultIndexedLayout` collapse to a private `layoutCore
(includeReveal: bool)` with two named wrappers. **SEO note resolved**: both layouts always
emitted `<meta robots=nosnippet>`; the misleading "allows indexing" comment was corrected
in place (nosnippet permits indexing, suppresses snippets) — markup unchanged to preserve
byte-identity; any actual robots change is a separate content decision, not this refactor.

### 2.4 F7 cheap slices (full fix is B2)
- Extract the duplicated regex cleaning block into a single named function `cleanCardHtml` with
  a comment pointing at B2. One copy of the fragility instead of N.
- ~~Replace the `sprintf`-built progressive-loading JSON + hand `escapeJson` with
  `System.Text.Json`.~~ **Moved to B2** (decision 2026-06-11) — see below.
- **Rollback**: per-slice revert.

**STATUS: slice (a) COMPLETE (byte-identical, 0 diffs); slice (b) DEFERRED to B2.**
Slice (a): `cleanCardHtml (html: string)` (private, `LayoutViews.fs`) now backs **all four**
timeline cleaning copies — two initial-render paths (one is a reviews-only `then`-branch with
a latent missing `else`, preserved as-is) and two progressive-loading JSON paths (one with a
`createSimplifiedReviewContent` branch). Helper takes already-rendered HTML so callers keep
their own `convertMdToHtml`/`createSimplifiedReviewContent` choice — that's what let it unify
all four. Byte-identical by construction (same ops, extracted).

Slice (b) — **decision: defer the STJ swap into B2, not a standalone step.** Rationale:
(1) STJ cannot be byte-identical here (it escapes `<>&`/control chars differently from the
hand `escapeJson`), so it would be the *only* byte-breaking step in an otherwise spotless
byte-identical Phase 2 — high contract cost, low marginal value once (a) removed the
duplication. (2) B2 introduces `RenderedContent` and rewrites this JSON-assembly path anyway;
the JSON will be serialized from a typed record there, making STJ the natural serializer and
folding its non-byte-identical diff into B2's single ADR + parity-harness review. (3) The
current escaper is correct and localized — safe to carry until the structural rewrite subsumes
it. (Agreement: hand-rolled JSON escaping is a smell; STJ is the right end state, just timed
with B2.)

### 2.5 Module splits (F9 — after 2.1/2.2 shrink things)
- Lift `UnifiedFeeds` out of `GenericBuilder.fs` → `UnifiedFeeds.fs` (insert in `.fsproj` after
  `GenericBuilder.fs`). Pure move; qualified names keep call sites valid.
- Lift timeline views out of `LayoutViews.fs` → `Views/TimelineViews.fs` if it still exceeds
  ~800 lines post-2.3/2.4.
- Hash-verify after each move (should be trivially identical). **Rollback**: per-move revert.

**STATUS: COMPLETE (byte-identical, 0 diffs each move) — 2026-06-11.** Two pure moves, no
logic change. (1) `UnifiedFeeds` lifted out of `GenericBuilder.fs` (2123→1476 lines) into a
top-level `module UnifiedFeeds` in `UnifiedFeeds.fs` (inserted in `.fsproj` right after
`GenericBuilder.fs`, before `ActivityPubBuilder.fs`). Promotion technique: a top-level `module`
(no `=`) keeps its 4-space-indented body verbatim (precedent: `Layouts.fs`), so the ~645-line
body moved without reindenting. 6 consumer files re-pointed `GenericBuilder.UnifiedFeeds.*` →
`UnifiedFeeds.*`, plus 3 `open GenericBuilder.UnifiedFeeds` → `open UnifiedFeeds` (the latter was
the one the dotted find/replace missed — caught by the compiler as a record-field inference flip
in `TextOnlyViews.fs`). (2) Timeline cluster (`sanitizeTagForUrl`, `cleanCardHtml`,
`extractReviewItemType`, `createSimplifiedReviewContent`, `avatarFlipCard`,
`timelineHomeViewStratified`, `timelineHomeView`) lifted out of `LayoutViews.fs` (1402→767 lines,
now under the ~800 budget) into `Views/TimelineViews.fs` (compiled before `LayoutViews.fs`).
The cluster was fully self-contained (all helpers private-to-cluster; `escapeJson`/
`getProperPermalink` defined inline). Callers `Builder.fs` + `Views/Partials.fs` re-pointed to
`TimelineViews.*`. **Lesson (memex candidate):** moving a nested module changes which `open`
path resolves a type — bare type names that resolved via `open Outer.Inner` silently flip to a
same-field record (here `UnifiedFeedItem` → `TravelCollectionItem`) until the `open` is fixed.

### 2.6 Navigation data source of truth (F11)
- Define nav as data (the existing `NavigationStructure`/`NavigationSection` types in
  `Domain.fs:280–291` are the starting point — finish what was sketched). Both `Layouts.fs`
  navs render from it; markup stays per-renderer.
- Hash-verify. **Rollback**: one revert.

**STATUS: COMPLETE (byte-identical, 0 diffs) — 2026-06-11.** New `Views/Navigation.fs` is the
single source of truth for the desktop ("desert") nav: a typed model (`NavSection` = `LinkGroup`
| `DropdownGroup`; `NavMenuEntry` = `MenuLink` | `MenuDivider`; `NavLink` carries an optional
icon node) with the link/section data in `desktopSections`, rendered by `desertNavigation` to
byte-identical markup. Adding a collection to the menu is now a one-line data edit; this value is
the seam bet B1 (content-type registry) will feed.

**Deliberately did NOT force-unify the two navs** (the assessment's literal "one data value both
render from"). Deep finding: the text-only nav is a *curated, structurally drift-immune* surface
over the `/text/` URL space — it routes through an "All Content" aggregation rather than listing
collections individually, so it does not drift when collections are added. The two are different
link sets over different URL spaces, not the same links with different markup; coupling them would
require permanent "show-in-X" flags serving a unification neither surface wants. So the text-only
nav is modelled as its own `textOnlyNav` list + `renderTextOnlyNav` (co-located in the same module
for discoverability, but independent). This honours URL permanence and the repo's text-only
curation doctrine.

The real "multiple sources of truth" hazard was **three dead orphans**, all removed (zero output
change): `defaultNavBar` (167 lines of stale Bootstrap navbar in `Layouts.fs`, never referenced),
`getNavigationStructure` (`Collections.fs`, never called), and the `NavigationStructure`/
`NavigationSection` types in `Domain.fs` (only the dead function used them — the assessment's
"unused-in-nav type proving the intent existed"). `Layouts.fs` 660 → 361 lines.

### 2.7 Closed `ContentType` DU (F5 — step 2 of 2; the enforcement upgrade)
- Replace the 1.3 literals module's *consumers* with a real closed DU
  (`[<RequireQualifiedAccess>] type ContentType = Posts | Notes | …`) + one boundary pair
  `parse : string -> ContentType option` / `serialize : ContentType -> string`. Strings survive
  only at the YAML/frontmatter and URL boundaries. A failed `parse` at the boundary produces a
  `ContentError`-style block (2.8) listing the valid set and a nearest-match hint
  (`got 'recipies' — did you mean 'recipes'?`).
- Rationale (assessment F5 + §7; researched: F# style guide, Wlaschin "Designing with types"):
  of alias / single-case DU / UMX / closed DU, only the closed DU buys **exhaustiveness** —
  adding `| Recipes` later makes the compiler enumerate every match that must handle it. That
  converts the 8-file human checklist into compiler-guided errors, which is the whole point.
- **Discipline (permanent, enforce in review): no wildcard `_` branches in content-type
  matches** — a wildcard silently disables the exhaustiveness paid for here.
- Precedent in-repo: `Domain.fs:407–412` already defines `ResponseType` as a DU that the YAML
  layer bypasses with a raw string (`Domain.fs:418`). 2.7 includes wiring that existing DU in
  properly — which forces the response-subtype taxonomy decision (memex
  pattern-content-type-taxonomy-mismatch) to be made explicitly.
- Sequenced **after 2.1/2.2** so the DU lands in consolidated code (1 driver + few projections),
  not 25 clones. **Verify**: byte-identical output. **Rollback**: revert to 1.3 literals.

**STATUS: COMPLETE (byte-identical, 0 diffs over 13,510 files) — 2026-06-11.** Added
`[<RequireQualifiedAccess>] type ContentType` (13 canonical cases) + the boundary pair
`parse : string -> ContentType option` / `serialize : ContentType -> string` to `ContentTypes.fs`.
The two *genuine dispatch* sites — the only places that match on content-type identity to make a
decision — were made exhaustive over the DU with **no wildcard**: `urlPrefix : ContentType -> string`
(AlbumCollection/PlaylistCollection now explicit, previously fell through the `/{other}/` fallback to
the same value) and `toJsonFeedContent` (routed through `parse`, exhaustive over `ContentType option`
incl. `None`). Callers passing a wire string use the new `urlPrefixForKey` shim, which preserves the
old `| other -> sprintf "/%s/" other` fallback byte-for-byte (this is what keeps response subtypes
and any unmapped value identical). Adding a `ContentType` case now fails to compile until both
dispatch sites choose a behaviour — the F5 payoff.

**Taxonomy decision (the choice F5 forces, made explicitly):** response subtypes
(reply/reshare/star/rsvp/bookmark) are **`Domain.ResponseType` values, not `ContentType` cases** — the
DU deliberately excludes them. They survive only as strings in the `UnifiedFeedItem.ContentType` wire
field, where they are the timeline grouping key and are serialized into the timeline JSON (see memex
`pattern-content-type-taxonomy-mismatch`). **Deliberately did NOT** force the closed DU onto the
string-typed `UnifiedFeedItem.ContentType` wire field nor onto `ResponseDetails.ResponseType`: those
are serialization boundaries that carry subtypes and flow to byte-visible output; converting them is a
behavioural change (Phase 3 / B1 registry), not a byte-identical refactor. The ~90 equality/key sites
that read or set the wire field stay string-based for the same reason — they are serialization, not
dispatch, so they gain nothing from the DU. `serialize` is the forward path B1 will key off of.

### 2.8 Railway parse track — reconnect the severed rail (F8 full fix; §8.2–8.3; **requires 2.1**)
- Context: `ASTParsing.fs` already returns `Result<ParsedDocument<'T>, ParseError>`
  (`ASTParsing.fs:144+`); the rail is severed at 12 `| Error _ -> None` sites
  (`GenericBuilder.fs:120…1392`). Post-2.1 the driver is the consolidated consumer — reconnect
  once, not twelve times.
- New `ContentError` DU (in `ContentTypes.fs` or a small `Diagnostics.fs` compiled early) — the
  assessment §8.3 sketch: every case carries *file*, *got*, *valid set/hint*, and a `fix:`
  pointer to the authority file:line. Includes a `render : ContentError -> string` producing
  the standard block:
  ```
  ✗ UnknownResponseType: _src/responses/foo.md
      got 'boost' — valid: reply | reshare | star | rsvp
      fix: correct 'response_type' in the file, or extend ResponseType (Domain.fs:407)
  ```
- **Accumulate, don't short-circuit**: parse all files; partition successes/failures; successes
  render as normal; failures print one block each + a one-line per-type summary. No
  `result { }` short-circuiting at the batch boundary.
- **Default behavior (decided 2026-06-10): report + exit 0** — a bad file must not block
  publishing the rest. Add opt-in strict mode (`--strict` arg or `STRICT_CONTENT=1` env) →
  exit 1 on any content error, for CI use when wanted.
- Retire the broad `try/with → continue` blocks in loaders (`Builder.fs:1635–1647` etc.) where
  the railway now covers them; 1.4's interim reporter is superseded and deleted.
- **Verify**: `_public/` byte-identical when no files fail; a deliberately-broken fixture file
  (temp, not committed) produces the structured block with exit 0, and exit 1 under strict
  mode. **Rollback**: revert; the 1.4 reporter returns.

**Phase 2 done when**: chosen units merged + verified; `Builder.fs`/`GenericBuilder.fs` line
counts re-measured and recorded vs assessment baseline; ADR-0006 committed; changelog entry per
house convention.

---

## Phase 3 — Ambitious Bets (each an explicit go/no-go; reviewed diffs allowed)

Rule: each bet starts with a **one-page go/no-go note** (cost, benefit, exit) appended to this
file, decided *before* code. None is pre-approved. Sequencing below is the recommended order.

### B1 — Content-type registry (capstone of F1+F5) — *recommended GO, after Phase 2*
- **What**: one record per content type unifying what Phase 1.3 + 2.1 + 2.6 built piecemeal:
  identity, feed key, URL prefix, source dir, processor, views, nav presence. Adding a post type
  = adding one registry entry + a Domain record + views (target: 8 files → ≤3 meaningful edits;
  update `.github/skills/add-content-type` checklist accordingly).
- **Why GO**: directly serves "one identity, many post types" — the author's stated growth axis.
- **Cost**: M–L (mostly rearranging Phase-2 outputs into one table). **Exit**: registry deleted,
  Phase-2 configs remain free-standing (no worse than Phase 2 end-state).

### B2 — Structured render product; delete HTML re-parsing (F7 full fix) — *go/no-go after B1*
- **What**: `ContentProcessor.Render` returns `RenderedContent` (BodyHtml without
  wrapper/title + metadata); pages/cards/timeline/text-only compose instead of regex-stripping.
  Deletes `cleanCardHtml`, timestamp regexes, review-image regex extraction.
- **Risk**: highest in plan — touches every processor + the timeline. Flag-gated
  (`RENDER_V2`), full-site reviewed diff (expect *intentional* diffs where regex was lossy).
  **ADR-0007** regardless of outcome. **Exit**: flag off; old path untouched until cutover.
- **Folds in 2.4 slice (b)** (deferred 2026-06-11): replace the progressive-loading
  `sprintf`-JSON + hand `escapeJson` (`LayoutViews.fs`) with `System.Text.Json` serializing the
  typed `RenderedContent`-derived record. Its non-byte-identical JSON diff (STJ escapes
  `<>&`/control chars differently) is reviewed inside this step's diff rather than as a lone
  byte-breaking step in byte-identical Phase 2.

### B3 — Search-contract generation (Fable compiler pilot: **NO-GO**, decided 2026-06-10)
- **Decision (author, 2026-06-10): the Fable-the-compiler pilot is NO-GO.** Cons in assessment
  §6 accepted: second toolchain (Node/Fable/bundler) in a pure-dotnet pipeline, bundle-weight
  risk vs the slow-web/<50KB ethos, and a future human maintainer is likelier to know vanilla
  JS than Fable. No ADR needed; assessment §6 + this entry are the record.
- **What remains adopted (optional, cheap)**: **contract generation** — emit a JSON
  Schema/types artifact from `SearchIndex.fs` at build time and validate `search.js`'s
  consumption in CI. Type-drift protection across the F#/JS boundary with zero new toolchain.
  Schedule whenever convenient; it is independent of all other bets.

### B4 — Text-only derives from the shared model (F10) — *only after/with B2*
- Re-render text-only from `RenderedContent` + shared nav data + shared subtype normalization,
  deleting `TextOnlyViews.fs:39–87` regex rewriting. Reviewed diff with explicit text-only
  parity checklist (<50KB budget re-verified per page type).

---

## Success Metrics (measured against Phase 0 baseline)

- [ ] 11 duplicated builders → 1 driver + ≤11 config records (≥9 migrated)
- [ ] 14 converter clones → 1 generic + ≤4 explicit projections
- [ ] 0 duplicate build executions in `Program.fs` (5 today); converters run 1× each (3× today)
- [ ] 0 dead feature flags (1 today)
- [ ] Content-type identity: single authority module (1.3) → closed DU, exhaustive matches, no wildcards (2.7)
- [ ] Railway reconnected: 12 `| Error _ -> None` sites → 0; every parse failure surfaces as a
      self-contained block (file → got/expected → fix pointer); FS0025 = compile error (1.5)
- [ ] Regex-on-generated-HTML sites: 5 clusters today → 1 (Phase 2) → 0 (if B2 lands)
- [ ] `Builder.fs` + `GenericBuilder.fs` combined: 3,728 lines today → target <2,500 (Ph 2)
- [ ] Build wall-clock: improved vs baseline (record actual numbers Phase 0 vs Phase 1.1)
- [ ] Skipped-file visibility: every silently-dropped file now reported (0 reporting today)
- [ ] Adding a content type: ~8 files today → ≤3 meaningful edits (if B1 lands); skill checklist updated
- [ ] Zero unexplained output diffs at every checkpoint; site soul untouched (no visual/UX change in Phases 0–2)

## Completion Protocol (house rules)

- Phase logs in `logs/YYYY-MM-DD-arch-refactor-phaseN-log.md` during work; summarize to
  `changelog.md` and delete logs at each phase close.
- On full completion: archive this plan to `projects/archive/`, update `docs/` references,
  write changelog entry, and propose (ask-first, per convention) an AI Memex `project-report`
  + a `pattern` entry for the generic-driver migration if it proves reusable.

## Status Ledger

| Phase | Status | Started | Completed | Notes |
|---|---|---|---|---|
| 0 — Safety harness | `[x]` done | 2026-06-10 | 2026-06-10 | Umbrella branch created off main (after FSharp.Core lock bump 10.1.300→10.1.301 committed). Baseline: build 14.9s / generate ~110s / 13,486 files. Warnings: 1×FS1104, 0×FS0025. **Contract exclusion: `resources/ai-memex/graph.json`** (only `stats.generatedAt` build timestamp varies; `KnowledgeGraph.fs:527`). |
| 1.1 double-builds | `[x]` done | 2026-06-10 | 2026-06-10 | Byte-identical. Converters bound 1×; 4/5 duplicate builder calls removed; posts/responses tag arrays derived from FeedData; dead `feedNotes` removed. **buildBooks() duplicate retained** — StarRating gradient IDs use a global counter (BlockRenderers.fs:85), removal is cosmetic-only but breaks byte-identity; needs deterministic page-local IDs first (→F7/B2). |
| 1.2 dead flag | `[x]` done | 2026-06-10 | 2026-06-10 | Byte-identical. Removed `useUnifiedTagSystem` flag + unreachable `else`; deleted legacy `buildTagsPages` + 5 exclusively-used helpers. Net −150 lines. |
| 1.3 ContentTypes module | `[x]` done | 2026-06-10 | 2026-06-10 | Byte-identical. New `ContentTypes.fs` literals authority; mechanical swap across GenericBuilder converters/feeds, LayoutViews permalink, Program.fs feed lists. Left `"wikis"` plural key + Builder.fs path literals for 2.7/B1. |
| 1.4 skip diagnostics | `[x]` done | 2026-06-10 | 2026-06-10 | Byte-identical. Skip reporter in `buildContentWithFeeds` choke point; stdout-only, 0 skips today. interim; superseded by 2.8 |
| 1.5 FS0025 → error | `[x]` done | 2026-06-10 | 2026-06-10 | Added `<WarningsAsErrors>FS0025</WarningsAsErrors>`. Build clean, 0 FS0025. Byte-identical by construction (severity-only flag, zero hits). |
| 2.1 build driver | `[x]` | byte-identical (0 diffs) | 11/11 builders migrated | ADR-0006 |
| 2.2 generic toUnified | `[x]` | 2026-06-10 | 2026-06-10 | Byte-identical (0 diffs vs umbrella tip). Added `UnifiedExtras`/`defaultExtras`/`arrayTags`/`splitTags` + generic `toUnified`; collapsed 8 trivial converters (posts, notes, snippets, wikis, ai-memex, presentations, album/playlist-collections). Kept responses family / books / albums / bookmarks explicit (divergence = documentation). |
| 2.3 view dedupe | `[x]` | 2026-06-11 | 2026-06-11 | Byte-identical (0 diffs). `postCardView (feedKey, withWebmention)` + 4 wrappers; `responseBodyView (style)` + `cleanResponseContent` folds 4 bodies (rsvp kept explicit); `layoutCore (includeReveal)` + `defaultLayout`/`defaultIndexedLayout` wrappers. SEO nosnippet comment corrected, markup unchanged. |
| 2.4 F7 slices | `[x]` slice (a); (b)→B2 | 2026-06-11 | 2026-06-11 | Byte-identical (0 diffs). `cleanCardHtml (html)` unifies all 4 timeline cleaning copies. Slice (b) STJ swap deferred into B2 (can't be byte-identical; B2 rewrites the path anyway). |
| 2.5 module splits | `[x]` | 2026-06-11 | 2026-06-11 | Byte-identical (0 diffs each move). (1) `UnifiedFeeds` lifted from `GenericBuilder.fs` (2123→1476) into top-level `UnifiedFeeds.fs` (.fsproj after GenericBuilder); 6 consumers + 3 `open` statements re-pointed. (2) Timeline cluster lifted from `LayoutViews.fs` (1402→767) into `Views/TimelineViews.fs` (compiled before LayoutViews); callers `Builder.fs`/`Partials.fs` re-pointed. Pure moves, no logic change. |
| 2.6 nav data | `[x]` | 2026-06-11 | 2026-06-11 | Byte-identical (0 diffs). New `Views/Navigation.fs`: typed `NavSection`/`NavLink`/`NavMenuEntry` model + `desertNavigation` rendered from `desktopSections` data (adding a collection = one data row); `renderTextOnlyNav` from its own `textOnlyNav` list (kept independent — curated `/text/` surface, drift-immune via "All Content" aggregation). Deleted 3 dead orphans: `defaultNavBar` (167 lines, Layouts), `getNavigationStructure` (Collections), `NavigationStructure`/`NavigationSection` (Domain). Layouts.fs 660→361. Did NOT force-unify the two navs (false unification). Seam ready for B1. |
| 2.7 ContentType DU | `[x]` | 2026-06-11 | 2026-06-11 | Byte-identical (0 diffs over 13,510 files). Added `[<RequireQualifiedAccess>] type ContentType` (13 canonical cases) + boundary pair `parse`/`serialize` in `ContentTypes.fs`. The 2 genuine dispatch sites made exhaustive over the DU, no wildcard: `urlPrefix : ContentType -> string` (AlbumCollection/PlaylistCollection now explicit) + `urlPrefixForKey` string shim preserving the `/{other}/` fallback byte-for-byte; `toJsonFeedContent` routed through `parse`, exhaustive over `ContentType option`. **Taxonomy decision:** response subtypes are `Domain.ResponseType`, not `ContentType` — excluded from the DU, survive only as wire strings in `UnifiedFeedItem.ContentType` (timeline grouping key). ~90 equality/key sites stay string-based (serialization boundary, not dispatch). Adding a case now fails to compile until both dispatch sites handle it. |
| 2.8 railway parse track | `[ ]` | — | — | after 2.1; F8 full fix |
| B1 registry | `[ ]` go/no-go pending | — | — | |
| B2 structured render | `[ ]` go/no-go pending | — | — | ADR-0007 |
| B3 Fable pilot | **NO-GO** (2026-06-10) | — | — | contract-gen adopted as optional item |
| B4 text-only unification | `[ ]` go/no-go pending | — | — | requires B2 |
