---
title: "Stale Bootstrap Utility Shim After CSS Migration"
description: "When a CSS framework is removed but its class names remain in markup, a targeted utility shim preserves rendering without a site-wide rewrite or URL churn."
entry_type: pattern
published_date: "2026-07-03 13:50 -05:00"
last_updated_date: "2026-07-03 13:50 -05:00"
tags: "fsharp, web, css, patterns, migration"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: ""
---

## Discovery

After migrating lqdev.me from Bootstrap to a custom desert theme, the `/reviews/` section looked broken: type labels and ratings were glued together (`MovieRating: 4.0/5.0`), tags had no spacing (`horrormoviehellhousellc`), titles appeared multiple times, and a movie review submitted via GitHub issue #2576 had no visible review text.

A scripted audit revealed the real cause: Bootstrap CSS had been removed from `Views/Layouts.fs`, but ~25 Bootstrap utility/component classes were still emitted by F# markup across `CollectionViews.fs`, `LayoutViews.fs`, `TravelViews.fs`, `ContentViews.fs`, and `ComponentViews.fs`. The classes were now unstyled, so margins, gaps, badges, and grid containers collapsed.

## Root Cause

Removing a CSS framework is not the same as removing its class names. When markup continues to rely on classes like `.d-grid`, `.gap-3`, `.me-2`, `.badge`, `.bg-secondary`, and `.img-fluid`, those elements fall back to browser defaults. The result is silent visual regression: no build errors, but broken spacing, missing pills, and bunched content.

Two specific surprises deepened the bug:

1. `.d-grid` was never defined in the new theme — only `.d-flex` existed. A typo (`d-grip`) masked this further, so fixing the typo alone would not have restored grid spacing.
2. `.content-type-badge` was `position: absolute` (designed to float on timeline cards), so reusing it inline for review type labels would have caused layout overlap.

## Solution

Use a **utility shim** — a single CSS file that redefines the leftover Bootstrap classes in terms of the new design-system tokens. This avoids a risky site-wide markup rewrite and preserves URL permanence.

### 1. Audit real stale classes

Cross-reference emitted class names against the loaded stylesheets. The audit produced this set for lqdev.me:

```text
d-grid, gap-1..5, me-1/2/3/4, ms-1/2/3, align-self-center, align-top,
float-right/left, text-muted, text-primary, text-warning, text-decoration-none,
small, lead, display-4, img-fluid, img-thumbnail, rounded-circle, border,
border-top, bg-primary/secondary/light, badge, badge-light,
btn-outline-primary/secondary, btn-group, card-text, table
```

### 2. Create the shim

`_src/css/custom/utilities.css` maps each stale class to desert-theme variables:

```css
.d-grid { display: grid !important; }
.gap-3 { gap: var(--spacing-md) !important; }
.me-2 { margin-right: var(--spacing-sm) !important; }

.badge {
  display: inline-block;
  padding: 0.25em 0.6em;
  font-size: 0.78em;
  font-weight: 600;
  border-radius: var(--border-radius-sm);
  background-color: var(--accent-color);
  color: var(--button-text);
}

.img-fluid { max-width: 100%; height: auto; }
```

Import it last in the theme hub so it fills gaps without fighting intentional styles:

```css
/* _src/css/custom/main.css */
@import url('./marketplace.css');
@import url('./utilities.css');
```

### 3. Fix the reported surface natively

For the broken reviews section, rewrite the specific views with first-class desert classes rather than relying on the shim:

- `Views/ContentViews.fs` — `reviewPostView` now emits `.review-card` with cover, title, subtitle, `.review-type-badge`, and `SvgStarRating.render`.
- `Views/LayoutViews.fs` — `reviewPageView` deduplicated the item title, replaced dead Bootstrap tag links with `ComponentViews.postTagsSection`, and uses `.review-type-badge` inline.
- `_src/css/custom/components.css` — appended `.review-*` rules for the card, badge, and review block.

### 4. Recover lost content

The missing movie text was not a rendering bug — the file lacked the body. The review pipeline already supports a `content` field, so the fix was to paste the verbatim issue body into the markdown file after the `:::review` block.

## Prevention

- **Audit after framework removal.** A build-time or CI check that diffs emitted class names against loaded CSS catches stale classes before they ship.
- **Do not reuse absolute-positioned components for inline markup.** `.content-type-badge` and `.review-type-badge` are separate concerns.
- **Verify utility classes exist, not just that typos are fixed.** `.d-grid` had to be defined; the `d-grip` typo was only half the problem.
- **Keep the shim explicit.** Name it `utilities.css` (or `bootstrap-shim.css`) and document that it is a migration artifact, not a permanent design layer.

## Trade-offs

| Approach | Pros | Cons |
|---|---|---|
| Utility shim | Low risk, no URL changes, fixes whole site at once | Carries legacy class names forward |
| Full markup rewrite | Cleanest long-term architecture | High risk, many files, potential URL/microformat breakage |
| Hybrid (shim + native rewrite for reported surface) | Best of both: site no longer broken, key UX polished | Slightly more files touched |

For lqdev.me the hybrid approach was correct: the shim fixed ~25 stale classes site-wide instantly, while the reviews rewrite addressed the user-reported surface with a polished, desert-native design.

## Files

- `_src/css/custom/utilities.css` — new shim
- `_src/css/custom/main.css` — import added
- `Views/CollectionViews.fs` — `d-grip` → `d-grid`
- `Views/ContentViews.fs` — `reviewPostView` rewrite
- `Views/LayoutViews.fs` — `reviewPageView` dedupe + tags
- `_src/css/custom/components.css` — `.review-*` styles
- `_src/reviews/movies/hell-house-llc-lineage-2026-07-03.md` — recovered review body
