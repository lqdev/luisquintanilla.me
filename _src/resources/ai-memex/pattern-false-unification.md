---
title: "Pattern: False Unification — When 'Single Source of Truth' Is the Wrong Fix"
description: "Before unifying two similar-looking things into one abstraction, check whether they share an evolution axis — sometimes the real duplication is dead code, and forcing unification couples things that should stay independent."
entry_type: pattern
published_date: "2026-06-11 11:45 -05:00"
last_updated_date: "2026-06-11 11:45 -05:00"
tags: architecture, refactoring, fsharp, design, abstraction, yagni, maintainability, navigation
related_entries: pattern-content-type-taxonomy-mismatch, pattern-fsharp-module-extraction-inference-flip, pattern-feed-architecture-consistency
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

The lqdev.me architecture assessment (finding F11) flagged: "Navigation has no single source of
truth — desktop nav hardcoded in `Layouts.fs`, text-only nav separately hardcoded, adding a
section means editing markup in 2–3 places." The proposed fix: "a `NavigationData` value consumed
by **both** layout renderers; markup stays per-renderer, the *data* unifies."

The obvious move is to build one shared `NavigationData` and have both navs render from it. I
nearly did. Stopping to think first — prompted by a "what actually makes sense here? think deep"
nudge — changed the design and produced a better, lower-risk result.

## The trap: unifying things that only *look* duplicated

I read both navs carefully before unifying:

- **Desktop ("desert") nav**: root URL space (`/about`, `/collections/blogroll`, …), per-link SVG
  icons, 5 top links + two dropdowns (Collections: 11 items, Resources: 6 items).
- **Text-only nav**: the `/text/` URL space, a flat **curated** list of 6 links — Home, About,
  Contact, **All Content**, RSS Feeds, Help. No icons, no dropdowns.

These are **not the same links with different markup**. They are *different link sets over
different URL spaces*. Crucially, the text-only nav is **structurally drift-immune** to the site's
main growth axis (adding collections): it routes through a single **"All Content"** aggregation
page instead of listing collections individually. So the very drift F11 worries about ("a page on
the main site absent from the text-only nav") doesn't happen for collections on the text-only
side — by design.

Forcing both to render from one shared value would mean carrying per-item flags like
`showInDesktop` / `showInTextOnly` forever, to reconstruct a divergence the two surfaces *want*.
That's **false unification**: coupling two things that look similar but evolve independently,
paying complexity tax permanently to serve an abstraction neither side needs.

## The real duplication was dead code

The actual "multiple sources of truth" hazard wasn't the live navs at all — it was **three dead
orphans** masquerading as navigation infrastructure:

1. `defaultNavBar` in `Layouts.fs` — 167 lines of a stale Bootstrap navbar, **defined but never
   referenced** (superseded by the desert nav long ago). A maintainer could "helpfully" edit it
   and see no effect, or worse, wire it back in.
2. `getNavigationStructure` in `Collections.fs` — a function that builds a nav structure from
   collections, **never called**.
3. `NavigationStructure` / `NavigationSection` types in `Domain.fs` — **only** consumed by the
   dead `getNavigationStructure`. (The assessment even noted this: "unused-in-nav type proving the
   intent existed.")

These were the genuine source-of-truth confusion. Deleting them is pure clarity with **zero
output change** — exactly the kind of high-value, zero-risk cleanup that hides behind a
mis-framed "go unify the navs" task.

## Solution

What actually improved maintainability:

1. **Delete the three dead orphans.** Verified byte-identical (they rendered nothing).
2. **Data-drive only the surface that genuinely drifts** — the desktop nav. New
   `Views/Navigation.fs` with a typed model:
   ```fsharp
   type NavLink = { Href: string; Label: string; Icon: XmlNode option }
   type NavMenuEntry = MenuLink of NavLink | MenuDivider
   type NavSection =
       | LinkGroup of NavLink list
       | DropdownGroup of Id: string * Label: string * Icon: XmlNode * Entries: NavMenuEntry list
   ```
   The whole desktop menu is now one `desktopSections : NavSection list` value; adding a
   collection is a one-line data edit. A `renderSection` function reproduces the exact prior
   markup (verified byte-identical).
3. **Keep the text-only nav as its own independent value** (`textOnlyNav` + `renderTextOnlyNav`)
   **co-located in the same module** for discoverability — but *not* derived from the desktop
   data. The module honestly names two surfaces instead of pretending they're one.

Net: `Layouts.fs` 660 → 361 lines; output byte-identical; the desktop nav data is now the clean
seam a future content-type registry can feed.

## Prevention / heuristics

- **Before unifying, ask: do these share an evolution axis?** If adding a feature touches both in
  lockstep, unify. If they diverge intentionally (different audiences, URL spaces, curation
  policies), keep them separate and *co-locate* instead of *couple*.
- **"Single source of truth" applies per-concern, not per-resemblance.** Two things rendering
  links doesn't make them one navigation concern.
- **Hunt for dead code first.** Apparent duplication is often a live thing plus one or more dead
  look-alikes. Deleting the dead ones is the highest-value, lowest-risk part of the fix and is
  easy to miss when you're focused on "unifying."
- **Co-location ≠ coupling.** Putting two independent values in one module gives discoverability
  ("all nav lives here") without forcing a shared abstraction.
- **Watch for `show-in-X` flags as a smell.** If unifying two things requires per-item flags to
  re-split them, you're probably unifying things that wanted to stay apart.

## Outcome

The mis-framed "unify the navs" task became "delete 3 dead orphans + data-drive the one nav that
drifts + keep the curated surface independent." Lower risk, byte-identical, and it removed the
*actual* maintenance hazard the original framing missed.
