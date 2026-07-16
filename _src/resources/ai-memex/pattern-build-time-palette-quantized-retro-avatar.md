---
title: "Pattern: Build-Time Palette-Quantized Retro Avatar (VGA Mode 13h in F#)"
description: "Generate a Doom-style retro version of a static asset at build time with SkiaSharp — downscale + quantize to a fixed 256-color palette + nearest-neighbor upscale — instead of hand-editing an image or spending tokens on an AI redraw."
entry_type: pattern
published_date: "2026-07-16 09:46 -05:00"
last_updated_date: "2026-07-16 09:46 -05:00"
tags: fsharp, dotnet, static-site-generation, build-time-rendering, image-processing, skiasharp, performance
related_entries: pattern-build-time-svg-replaces-runtime-js, pattern-single-source-config-generate-vs-verify, pattern-build-time-svg-size-budgeting
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

Reading [Staniks' "Catlantean 3D" article](https://staniks.github.io/articles/catlantean-3d-blog-1/)
on VGA **Mode 13h** (320×200, 256 colors) crystallized what makes early-90s
graphics look the way they do: it isn't just *low resolution* — it's a **tightly
restricted, fixed palette** plus **quantization** to that palette. The chunky,
"crispy" look is the combination.

The goal: an alternate, Doom-style version of the site avatar — **without**
deleting the original, without hand-editing pixels in an image editor, and
without burning tokens asking an AI to redraw it on every tweak. The retro image
should be a *deterministic function* of the source image and a few knobs, so it
can be recalibrated with a one-line change and a rebuild.

## Root Cause

Why generate at build time instead of committing a pre-made PNG?

- **Tunability without tokens.** The "how chunky" decision (grid size) is a knob,
  not an art task. `GridSize 80 → 48` made ~8px blocks; that's a one-line diff,
  not a re-draw. No AI round-trip, no manual pixel work.
- **Provenance & reproducibility.** The retro avatar is derived from the
  committed source avatar by committed code. Change the source, rebuild, done.
- **The naive distance metric fails.** Plain Euclidean RGB distance to the
  palette drifts shading toward flat grey (the article calls this out). A
  perceptual **"redmean"** weighted distance keeps skin tones and shadows
  distinct. This is the non-obvious part — the quantizer's *distance function*
  matters more than the palette size.

## Solution

A single F# module (`Services/RetroAvatar.fs`) using **SkiaSharp** (already a
dependency, with Linux + Win32 native assets, so it runs in the GitHub Actions
Linux build — do **not** reach for `System.Drawing`, which isn't cross-platform).

Pipeline in `generateDoomAvatar`:

1. **Decode** the source PNG (`SKBitmap.Decode`).
2. **Optional center-crop** (`CropFraction`) to tighten framing on the face.
3. **Downscale** to a small square grid (`ScalePixels` with
   `SKFilterMode.Linear`) → pixelation.
4. **Quantize** every pixel to the nearest color in the authentic Doom PLAYPAL
   (256 colors) using a **redmean** weighted distance.
5. **Upscale nearest-neighbor** (`SKFilterMode.Nearest`) back to output size →
   hard pixel blocks.
6. **Encode** PNG.

Two details that saved pain:

- **Embed the palette as data, parse it, don't transcribe by hand.** The 768-byte
  PLAYPAL is pasted verbatim as `0xNN` tokens and parsed with a regex into 256
  `(byte*byte*byte)` triples — zero hand-transcription risk. (Palette color
  values aren't copyrightable; it's the standard id Tech 1 palette.)
- **Tunables live in one `Options` record** (`defaultOptions`:
  `GridSize=48, OutputSize=400, CropFraction=0.06, Dither=false`). The whole
  look is recalibrated from that one place.

```fsharp
// redmean perceptual distance — keeps shading from collapsing to grey
let private nearestPaletteColor (r,g,b) =
    let rmean = // average of source.r and candidate.r, per channel-pair
        ...
    // weight red/blue by rmean; green fixed. Pick argmin over the 256 palette.
```

Wiring: `Builders/Assets.fs` calls `generateDoomAvatar` right after copying the
source avatar and **before** the QR generators (so the QR center can embed the
retro face). The displayed filename is a single constant
(`Constants.Avatar.displayFileName`) consumed everywhere — see
[[pattern-single-source-config-generate-vs-verify]].

## Prevention

Apply this pattern whenever a static asset is a **deterministic transform** of a
committed source and the "style" is parameterizable:

- Prefer **build-time generation over a committed artifact** when the transform
  has knobs you'll want to tweak — it turns art tasks into config changes and
  keeps provenance in code. (Same spirit as
  [[pattern-build-time-svg-replaces-runtime-js]].)
- For palette quantization specifically, **use a perceptual distance** (redmean
  or better) — Euclidean RGB looks muddy.
- **Embed reference data (palettes, LUTs) as parseable text**, not
  hand-typed arrays.
- Keep all look-and-feel knobs in **one options record** so recalibration is a
  one-line, zero-token diff.
- Stay on the toolkit your CI already has native binaries for (**SkiaSharp**,
  not `System.Drawing`) so the Linux build doesn't surprise you.
