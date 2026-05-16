---
title: "Pre-Rendered Build-Time SVGs Replace Runtime JS for Static-Site Assets"
description: "Migration pattern: when a static site uses a CDN JS library at runtime to generate an asset that doesn't actually change per visit, render it at build time instead"
entry_type: pattern
published_date: "2026-05-16 17:30 -05:00"
last_updated_date: "2026-05-16 17:30 -05:00"
tags: "fsharp, static-site-generation, performance, progressive-enhancement, build-time-rendering, dependency-elimination, qr-code"
related_skill: ""
source_project: "lqdev-me"
related_entries: "pattern-build-time-svg-size-budgeting, pattern-pure-css-disclosure-replaces-js-modal, pattern-defensive-component-css-encodes-regression-fixes"
---

# Pre-Rendered Build-Time SVGs Replace Runtime JS for Static-Site Assets

## Discovery

The site shipped a per-page "QR Code" button on every content page. The
implementation was straightforward and reasonable when it was written:

1. Load `qr-code-styling@1.5.0` from a CDN on every page.
2. Load `_src/js/qrcode.js` (~280 lines) that wires up a click handler.
3. On click, open a modal, instantiate `QRCodeStyling`, generate the SVG
   in-browser from the page's URL, and inject it into the DOM.

Every visit paid:
- A blocking CDN network round-trip to fetch the library.
- ~10 KB of JS over the wire.
- Runtime computation on click.

But the **output never changes**. The QR code encodes the page URL.
The page URL is fixed at build time. Every visitor to every page
regenerates the **same** SVG that every other visitor regenerates. The
asset is deterministic-per-URL — exactly the shape that build-time
rendering exists for.

Phase 3 of the QR migration (PR #2390) moved the computation
client → build: one pre-rendered SVG per content page, written to
`/assets/images/qr/<type>/<slug>.svg` at build time, served as a static
asset by the same CDN that serves every other static file.

## The Migration Pattern

Three architectural moves, each addressing one of the three runtime costs:

### 1. Replace the runtime renderer with a build-time emitter

Find the function that generates the asset (here: `qr-code-styling`'s
`QRCodeStyling` constructor) and either:
- Port it to your build language (the choice for this project, since
  the styling needed F# control). See `Services/QRStyled.fs`.
- Or call the existing JS at build time via Node.

The build-time emitter writes to a deterministic per-asset path:

```fsharp
let renderPageQR (outputDir: string) (item: UnifiedFeedItem) =
    let payload = item.Url.Replace("https://www.lqdev.me/", "https://lqdev.me/")
    let outputPath =
        Path.Combine(outputDir, "assets", "images", "qr",
                     item.ContentType, slug + ".svg")
    // ... render styled SVG, write to outputPath
```

Iterate over all content items once per build:

```fsharp
let buildPerPageQRs (outputDir: string) (items: UnifiedFeedItem list) =
    for item in items do
        renderPageQR outputDir item
```

### 2. Replace the runtime UI with a pure-CSS disclosure

The old UI was a JS modal: button → click handler → DOM injection →
overlay → close button. All of it existed to wrap the runtime SVG
generation.

With the SVG pre-rendered, the UI collapses to native HTML:

```fsharp
let qrCodeButton (relativeUrl: string) =
    let qrPath = "/assets/images/qr/" + relativeUrl.Trim('/') + ".svg"
    tag "details" [ _class "qr-code-disclosure" ] [
        tag "summary" [ _class "qr-code-btn permalink-action-btn" ] [
            tag "span" [ _class "button-icon" ] [ str "📱" ]
            tag "span" [ _class "button-label" ] [ str "QR Code" ]
        ]
        div [ _class "qr-code-panel" ] [
            img [ _src qrPath; _class "qr-code-img"; attr "loading" "lazy" ]
        ]
    ]
```

`<details>` provides toggle behavior natively. `<img loading="lazy">`
defers the SVG fetch until the disclosure opens. **No JavaScript**.

### 3. Delete the now-dead JS and CDN dependency

This is the part teams forget. The build-time replacement only delivers
its full value if you actually evict the old code path:

- Delete the script tag from layouts (the CDN `<script>` AND your
  wrapper JS).
- Delete the JS source file (`_src/js/qrcode.js`).
- Trim the CSS to what the new disclosure needs (modal CSS is dead).
- **Bump the service worker cache version** — otherwise clients with a
  cached old `index.html` will keep trying to load the deleted script
  and fail silently.

```js
// _src/service-worker.js
// v1.0.3: Phase 3 per-page QR migration — `qr-code-styling` CDN script
// and `/assets/js/qrcode.js` were removed. Bumping the cache version
// forces clients to evict stale entries that still reference them.
const CACHE_VERSION = 'v1.0.3';
```

## Why This Specific Migration Is Worth Doing

The runtime cost was non-trivial because it applied to **every page
load**. Replacing it with a static asset gives the visitor:

- No CDN round-trip → faster first paint, no third-party dependency.
- No runtime JS → smaller cumulative layout shift, smaller JS bundle,
  smaller parse cost on low-power devices.
- Works without JS at all → progressive enhancement default.
- The asset is now eligible for the same caching/CDN/SW strategies as
  every other image on the site.

The build pays once per asset, served from your CDN forever. The
visitor pays nothing.

## Counter-Indications

This isn't free. Before doing it, ask:

- **Does the asset actually not change per visitor?** If it does
  (auth-aware, personalized, time-sensitive), build-time rendering is
  wrong.
- **Is the build cost acceptable?** 1,735 SVGs took ~5 seconds of
  build time. For 100k items you'd need a different approach
  (incremental build cache, content-hashed manifest).
- **Is the bundle size acceptable?** 31.7 MB across 1,735 files is fine
  for a personal site on a modern CDN. For a much larger fan-out you
  need a size-budget pass first (see related entry).

## The Tells That Suggest This Pattern Applies

You're a candidate for this migration if you have:

1. A runtime JS library generating a deterministic-per-URL asset.
2. A modal/disclosure UI wrapping that generation.
3. A static site generator that already iterates over all content.

The triangulation is: deterministic asset + iteration already exists
+ no per-visit variation. Move it.

## Lessons

1. **Audit "runtime computation" with the question: does the output
   actually depend on the visitor?** If the answer is "no, it depends
   on the page", it should be build-time.
2. **The UI almost always collapses too.** A JS modal exists because
   the JS generation needed somewhere to put its output. Pre-rendered
   assets can usually live in a plain `<img>` inside a `<details>`.
3. **Migration completion = eviction.** Pre-rendering the asset
   without removing the old JS leaves you with both costs plus the
   build cost. Always delete the dead path AND bump the SW cache.
4. **The build language doesn't have to match the original.** This
   project ported `qr-code-styling`'s look to F# rather than calling
   the JS library from Node at build time, because the styling logic
   was simple enough to reimplement and the dependency reduction was
   worth more than feature parity.

## Citations

- PR #2390 — Phase 3 per-page QR migration (this pattern, end-to-end)
- `Services/QRStyled.fs` — the build-time emitter
- `Builder.fs` — `buildPerPageQRs`
- `Views/ComponentViews.fs:88-120` — disclosure UI
- `Views/Layouts.fs` (around the script block) — CDN script removed
- `_src/service-worker.js` — `CACHE_VERSION` bump for eviction
