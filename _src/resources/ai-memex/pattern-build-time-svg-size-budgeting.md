---
title: "Build-Time SVG Size Budgeting for Fan-Out Static Assets"
description: "Four stacking optimizations that took a styled, brand-themed QR SVG from 98 KB to 18.7 KB across 1,735 pre-rendered files — without touching visual fidelity"
entry_type: pattern
published_date: "2026-05-16 17:30 -05:00"
last_updated_date: "2026-05-16 17:30 -05:00"
tags: "fsharp, svg, performance, static-site-generation, optimization, qr-code, build-time-rendering"
related_skill: ""
source_project: "lqdev-me"
related_entries: "pattern-build-time-svg-replaces-runtime-js, pattern-defensive-component-css-encodes-regression-fixes, pattern-pure-css-disclosure-replaces-js-modal"
---

# Build-Time SVG Size Budgeting for Fan-Out Static Assets

## Discovery

When you pre-render one styled SVG per content item on a 1,735-item static
site, the per-file size doesn't matter — until it does. The first naive
implementation of the per-page QR fan-out (Phase 3 of the QR migration,
PR #2390) emitted **98 KB average × 1,735 files = 166 MB** of deploy
bundle. Same SVG renderer that produces a beautiful ~3 KB QR for the
snippets page and a ~15 KB hero QR for the homepage suddenly ballooned
when called in a loop with per-page URLs.

The fix wasn't one big optimization — it was **four stacking
optimizations**, each addressing a different byte-source in the SVG, that
together took the average to 18.7 KB (5.2× reduction, total bundle from
166 MB → 31.7 MB) with **zero visual quality loss** at the rendered
display size.

## The Anatomy of a Bloated SVG

A styled QR SVG has three byte-sources:

1. **Path data for ~1,500 dark modules** (the "dots") — the dominant
   cost when each module is rendered as a rounded-corner arc with
   4-decimal float coords like `A0.5,0.5 0 0 1 3.784,2.216`.
2. **The center avatar** as a base64-encoded `<image href=...>`.
3. **SVG document scaffolding** — XML header, `<svg>` open tag with
   `viewBox`/`width`/`height`/`xmlns`/`xmlns:xlink`, background
   `<rect>`, finder pattern paths.

For long URLs (~70 chars at ECC High), the QR needs ~45-49 modules per
side, ~1,500 dark modules, and at 4-decimal float coords each module
emits ~60 characters of path data. That's ~90 KB before you even add
the avatar.

## The Optimization Stack

Each step targeted a specific byte source. Validated by `dotnet run`
and measured against the full 1,735-file output.

### Step 1 — Integer-grid coordinate space (98 KB → 70 KB)

In the homepage hero, the SVG renders at a fixed 280 px and the renderer
uses the caller-supplied `Size` as the viewBox dimension. That means
`cell = 280 / 49 = 5.714…` and every path coordinate is a float.

For per-page SVGs displayed via `<img src>` at arbitrary CSS sizes, we
don't need that pixel-aligned precision. The fix:

```fsharp
let canvasSize = if opts.IntegerGrid then float count else opts.Size
let cell = canvasSize / float count
// In IntegerGrid mode: canvasSize = count (e.g. 49), cell = 1.0,
// viewBox = "0 0 49 49", and all module coords become integers.
```

Coordinates went from `64.324` (6 chars) to `8` (1 char). Combined with
omitting the `width`/`height` attributes (the consumer scales via CSS),
this alone saved ~28 KB per file.

### Step 2 — Plain-square fast path for data modules (70 KB → 47 KB)

The existing `dotPath` emits neighbor-aware rounded-corner arcs so
adjacent modules visually merge — beautiful at the hero size, **invisible**
at module-cell sizes of 1 unit when the SVG is later scaled by the
browser. Sub-pixel rounded corners are not perceptible at 4-5 CSS pixels
per module.

A plain-square fast path drops the per-module byte cost from ~60 chars
to 12:

```fsharp
if opts.IntegerGrid then
    for y in 0 .. count - 1 do
        for x in 0 .. count - 1 do
            if isDataModule x y then
                dotPaths.Add (sprintf "M%d,%dh1v1h-1z" x y)
else
    // Original rounded-corner neighbor-aware path emission
    ...
```

For ~1,500 dark modules: 90 KB → 18 KB of path data.

### Step 3 — Drop the `xlink:href` duplicate (47 KB → 24 KB)

The center avatar is embedded as base64. The original renderer emitted
**both** `href` (SVG2) and `xlink:href` (SVG1.1 fallback) for maximum
compatibility:

```xml
<image x="..." y="..." width="..." height="..."
       href="data:image/png;base64,iVBORw0..."
       xlink:href="data:image/png;base64,iVBORw0..." />
```

That duplicates the entire base64 payload. For a ~17 KB encoded avatar,
the SVG contains 34 KB of avatar bytes. Every browser that can render
`<img src="*.svg">` understands SVG2 `href`. The `xlink:href` is dead
weight for this use case:

```fsharp
if opts.IntegerGrid then
    sprintf "<image ... href=\"%s\" />" dataUri
else
    sprintf "<image ... href=\"%s\" xlink:href=\"%s\" />" dataUri dataUri
```

Saved ~17 KB per file — the biggest single win of the four steps.

### Step 4 — Resize the source avatar at build start (24 KB → 18.7 KB)

The avatar PNG itself was 71 KB at source resolution. The center slot in
a per-page QR displays at roughly 60-70 CSS pixels. There's no reason to
embed a 71 KB image to display at 64 px.

A one-time SkiaSharp resize at build start:

```fsharp
let ensureSmallAvatar (srcPath: string) (outputPath: string) =
    use src = SKBitmap.Decode(srcPath)
    let targetSize = 64    // matches the on-page display size
    use target = new SKBitmap(SKImageInfo(targetSize, targetSize, ...))
    src.ScalePixels(target, SKSamplingOptions(SKCubicResampler.Mitchell))
        |> ignore
    use img = SKImage.FromBitmap(target)
    use data = img.Encode(SKEncodedImageFormat.Png, 100)
    use stream = File.OpenWrite(outputPath)
    data.SaveTo(stream)
```

71 KB → 4.6 KB PNG. Embedded as base64: ~6 KB instead of ~17 KB.
Cached in `_public/assets/images/qr/_avatar-sm.png` and reused for
every render.

## Why SkiaSharp, not ImageSharp

ImageSharp v3 is now Six Labors Split License — Apache 2.0 for
non-commercial use **or** a paid commercial license. SkiaSharp is
MIT-licensed, .NET Foundation maintained, and handles the single
PNG-resize use case trivially. Build-time only — no runtime deploy
impact on clients.

## Measurement Discipline

The only honest way to know whether each step actually helped:

```powershell
Remove-Item _public\assets\images\qr -Recurse -ErrorAction SilentlyContinue
dotnet run --no-build
$f = Get-ChildItem _public\assets\images\qr -Recurse -Filter *.svg
$s = $f | ForEach-Object { $_.Length/1KB }
"n={0}  avg={1:F1}  p90={2:F1}  max={3:F1}" -f $f.Count,
    ($s | Measure-Object -Average).Average,
    (($s | Sort-Object)[[int]($f.Count*0.9)]),
    ($s | Measure-Object -Maximum).Maximum
```

Stopping at "looks good" is not a budget. Each cumulative measurement
after each step confirmed which optimization was worth keeping and
which wasn't moving the needle.

## When to Apply This Pattern

**Yes**:
- Static site assets generated in a loop (1k+ files).
- SVGs (or any text-format) where bytes scale with what you put inside.
- Per-asset bytes affect total deploy bundle or CDN egress.

**No**:
- A single asset where 50 KB doesn't matter.
- Cases where visual fidelity at full display size requires the cost.
  (The homepage hero in this same codebase keeps the rounded-corner
  rendering — different display context, different budget.)

## Lessons

1. **A naive renderer reused at scale is the antipattern.** The same
   `Services.QRStyled.render` produces a beautiful hero and a bloated
   fan-out asset depending only on options. A dedicated `IntegerGrid`
   mode lets one renderer serve both.
2. **The biggest win is often a duplicate you didn't notice.** The
   `href`/`xlink:href` duplicate was hiding in plain sight in code
   that "matches the runtime modal's look" — it doesn't, the runtime
   modal didn't dual-emit.
3. **Browser scaling is forgiving.** Sub-pixel rounded corners are
   invisible at typical display sizes. Optimizations that preserve
   the **visible** geometry are free.
4. **Negotiate the budget if you can't hit it cleanly.** A user-set
   20 KB target was reachable here, but reaching it required pushing
   on four levers simultaneously. Either tell the user "we landed at
   X" before fanning out (cheap to undo) or be willing to relax the
   number (it's static-asset bytes, not request latency).

## Citations

- `Services/QRStyled.fs:46-66` — the `IntegerGrid` option
- `Services/QRStyled.fs:69-87` — `defaultOptions` vs `perPageOptions`
- `Services/QRStyled.fs:185-209` — canvas/cell computation
- `Services/QRStyled.fs:233-264` — plain-square fast path
- `Services/QRStyled.fs:293-313` — single-href emission
- `Services/QRStyled.fs:368-410` — `ensureSmallAvatar` SkiaSharp resize
- `Builder.fs` — `buildPerPageQRs`
- PR #2390 — full Phase 3 migration
