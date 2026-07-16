---
title: "Pattern: Single-Source Config — Generate the Safe Ones, Verify the Coupled One"
description: "Centralize site identity in one F# module, then generate derived config artifacts from it — but for a document that's byte-coupled to a separate deploy, verify-and-fail-loud instead of rewriting, keeping the single-source guarantee without the coupling risk."
entry_type: pattern
published_date: "2026-07-16 09:46 -05:00"
last_updated_date: "2026-07-16 09:46 -05:00"
tags: fsharp, dotnet, static-site-generation, architecture, configuration, build-time-rendering, activitypub, pwa
related_entries: pattern-build-time-palette-quantized-retro-avatar, pattern-false-unification, pattern-ci-cd-fallout-byte-identical-refactor, pattern-build-time-svg-replaces-runtime-js
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

Site identity — author name, avatar filename, theme color, canonical URL,
fediverse handle, PWA name — was hardcoded as string literals scattered across
views, layouts, a PWA `manifest.json`, a `service-worker.js`, and an ActivityPub
`actor.json`. Changing the avatar meant editing ~25 places; changing the theme
color, several more. Classic "define it in one place" opportunity.

The obvious move — a `Constants.fs` single source of truth — is right. But the
interesting part was the **three static config documents**: could they be
*generated* from the constants so they can't drift? The naive answer is "yes,
generate all three." The correct answer turned out to be **"generate two,
verify the third."**

## Root Cause

Not every "derived" artifact is safe to regenerate from the build:

- **`manifest.json`** — pure PWA config, nothing else consumes the source copy,
  formatting is irrelevant to consumers. **Fully generate** it. Zero risk.
- **`service-worker.js`** — large hand-written caching logic you do *not* want to
  port to F#, but with two values that must track constants (`CACHE_VERSION`,
  precache URL list). **Token-inject**: turn the source into a template with
  `__CACHE_VERSION__` / `__PRECACHE_URLS__` and replace at build. Logic stays
  hand-owned; the values become single-source.
- **`actor.json`** — federation-critical and the trap. It is:
  1. **byte-coupled** to a *separate* deploy (it ships with the sibling `api/`
     Azure Functions app, which reads the committed file directly), and
  2. **CRLF-encoded with exact escaping** — `System.Text.Json` emits LF and
     different escaping, so a re-serialize is **never** byte-identical.

  Rewriting `actor.json` from the site build would couple two independently
  deployed things and risk changing a federation identity document that Mastodon
  depends on. This is the same class of hazard as
  [[pattern-ci-cd-fallout-byte-identical-refactor]]: an out-of-tree consumer of a
  file the refactor "owns."

So the rule that emerged: **generation is only safe when the build fully owns the
artifact's consumers and formatting.** When it doesn't, downgrade from *generate*
to *verify*.

## Solution

**One `Constants.fs`** (compiled right after `Domain.fs`), submodules
`Urls / Author / Site / Theme / Avatar / ActivityPub / Pwa`. It absorbed the
earlier single-purpose `Avatars.fs`. Views, `Layouts.fs`, and
`ActivityPubBuilder.Config.baseUrl` all reference it.

**`Builders/GeneratedConfig.fs`** with three functions, wired into
`Assets.copyStaticFiles` (replacing the old `File.Copy` of manifest/SW):

```fsharp
// 1. FULLY GENERATE — safe: build owns it end to end
let generateManifest () =
    let manifest = {| name = Constants.Pwa.name; theme_color = Constants.Theme.color; ... |}
    File.WriteAllText(Path.Join(outputDir, "manifest.json"),
                      JsonSerializer.Serialize(manifest, jsonOpts))

// 2. TOKEN-INJECT — keep hand-written JS, single-source the values
let generateServiceWorker () =
    let template = File.ReadAllText(Path.Join(srcDir, "service-worker.js"))
    // fail loudly if tokens are missing (someone edited the template wrong)
    let precacheJs = Constants.Pwa.precache |> List.map (sprintf "    '%s'") |> String.concat ",\n"
    template.Replace("__CACHE_VERSION__", Constants.Pwa.cacheVersion)
            .Replace("__PRECACHE_URLS__", "[\n" + precacheJs + "\n]")
    |> fun s -> File.WriteAllText(Path.Join(outputDir, "service-worker.js"), s)

// 3. VERIFY, DON'T REWRITE — coupled + CRLF byte-identity
let verifyActor () =
    use doc = JsonDocument.Parse(File.ReadAllText "api/data/actor.json")
    // PARSE and compare field values (not raw substrings — PEM has escaped \n)
    check "name" Constants.Author.name (str "name")
    check "icon.url" Constants.Avatar.displayUrl (icon.url)
    check "publicKeyPem" Constants.ActivityPub.publicKeyPem (pem)
    // ... if any mismatch: failwithf "actor.json out of sync with Constants: ..."
```

`verifyActor` runs at `dotnet run` and **fails the build loudly** if the
federation document drifts from constants — so the two are updated together,
deliberately, but the build never mutates the file the Functions app deploys.

One subtlety: **verify by parsing, not substring-matching.** The public key in
the file is stored with escaped `\n`; the parsed JSON value has real newlines,
which is what the F# constant holds. Compare parsed values.

## Prevention

- **Single source of truth is right for identity values** — but see
  [[pattern-false-unification]]: unify the *data*, not necessarily the rendering.
  Here the constants unify; each renderer/artifact still owns its own shape.
- **Classify each derived artifact before deciding generate vs. verify:**
  - Build fully owns consumers + formatting → **generate**.
  - Build owns the values but not the logic/formatting → **token-inject**.
  - A separate deploy or byte-identity owns it → **verify-and-fail-loud**.
- **A loud build failure is a feature**, not a limitation, for coupled files: it
  forces a deliberate, reviewed update instead of a silent, possibly
  federation-breaking rewrite.
- Watch for **CRLF / exact-escaping** requirements — they quietly make
  "regenerate identical" impossible and are a strong signal to switch to verify.
- Related build-time generation reasoning: [[pattern-build-time-svg-replaces-runtime-js]]
  and the avatar it drives, [[pattern-build-time-palette-quantized-retro-avatar]].
