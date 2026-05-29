# Website Development Backlog

*Last Updated: 2026-05-29*

This backlog tracks open opportunities and in-flight work for the site. It is intentionally **concise** — full implementation history lives in [`../changelog.md`](../changelog.md) and detailed project logs in [`archive/`](archive/). Reusable patterns and research are captured in the [AI Memex](../_src/resources/ai-memex/).

## 🎯 Current Status

The site is a mature, production IndieWeb static site generator (.NET 10, F#, Giraffe ViewEngine, Markdig). All core architecture is complete and stable:

- **Unified content pipeline** — all content types flow through the `GenericBuilder` AST pattern with custom Markdig blocks.
- **Feed architecture** — RSS/Atom/JSON + OPML across every content type, tag feeds, and an "Everything" unified feed.
- **Search** — site-wide client-side search (Fuse.js) over the full content index. *(shipped)*
- **Text-only site** — complete accessibility-first mirror at `/text/` (<50KB pages, WCAG 2.1 AA). *(shipped)*
- **ActivityPub federation** — the site federates: inbox handling, response semantics, link attachments, pagination, RSVP responses, account migration. *(live, ongoing phases)*
- **AI Memex** — AI-authored knowledge content type with build-time Knowledge Graph (`[[wikilinks]]`, backlinks, JSON-LD).
- **Desert theme** — bespoke design system, Bootstrap-free, light/dark variants.

`projects/active/` currently holds three **completed** ActivityPub phase docs (4A inbox handler, 5D dual-media, 6A RSVP) that are pending archival to [`archive/`](archive/); ongoing multi-PR work is otherwise tracked per session rather than as long-lived active-project files.

## 🚧 In Flight

### Site Improvements 2026-05 — umbrella `feature/site-improvements-2026-05`

A bite-size-PR program executed against one umbrella branch, merged to `main` at phase boundaries (α → β → δ → γ).

- **α — Docs & backlog hygiene** *(in progress)*: archive completed-migration/implementation docs (done), refresh `docs/README.md` index (done), refresh this backlog (this change), fill AI Memex stubs.
- **β — Foundations**: `/tools` directory page (personal "app store" of self-built browser extensions / PWAs), `/colophon` enhancements, and a shovel-ready issue to promote `OnlineRadioPlaylist` into a first-class **stations** content type (spec only, not implemented here).
- **δ — Automation & polish**: GitHub Actions **stale-content sweep**, GitHub Actions **yearly retrospective** draft generator (can reuse the website-stats script; gets richer once scrobbling is live), and remaining text-only polish.
- **γ — Now-playing & privacy** *(centerpiece, last)*: a `/now-playing` page driven by the self-hosted **[podcast-scrobbler](https://github.com/lqdev/podcast-scrobbler)** (ListenBrainz-compatible, DuckDB) via client-side fetch of CORS-exposed *aggregation* endpoints. Privacy is encoded structurally — aggregations are public, raw listens are token-gated. This work is the canonical example for two AI Memex pattern entries: **"private vault → public garden"** (PRIV-1) and **"aggregation as a privacy default"** (PRIV-2).

> The scrobbler integration spans two repos: the site (`/now-playing` UI + fetch) and `podcast-scrobbler` (its own `feature/now-playing-integration` umbrella for CORS-exposed aggregation endpoints).

## 📌 Open Opportunities (uncommitted)

Ideas worth doing eventually. Not scheduled; promote to "In Flight" when picked up.

### Content discovery
- **Response subtype landing pages** — dedicated `/reshares/`, `/replies/`, `/stars/` pages using the proven `buildBookmarksLandingPage` pattern. *(bookmarks landing page shipped; subtypes remain open)*
- **Related content / "you might also like"** — leverage the existing tag index and Knowledge Graph edges.
- **Tag organization** — hierarchy or curated topic clusters over the flat tag set.

### Quantified self (privacy-gated)
These are deliberately **deferred** until the listening/privacy primitives (γ above) prove the "private vault → public garden" model in production:
- **Reading life** — currently partly covered by reviews; a dedicated surface could aggregate read-later → bookmarks → reviews.
- **Watching life** — minimal today; low priority.
- **Places / travel** — interesting but privacy-bounded; existing travel guides cover the public-facing slice.
- **Scrobble granularity** — `/now-playing` ships at aggregate/crate-find granularity; per-listen detail is an optional later enhancement (display-only, no per-scrobble content entries).

### Tools & automation
- **`/tools` evolution** — v1 is a curated directory of links (read-it-later extension, github-post PWA, rss extension, playlist-creator). Could grow into richer per-tool pages, install instructions, or hosted demos.
- **Cross-posting / syndication** — WebSub support or POSSE automation building on the live ActivityPub layer.
- **Build performance** — profile build/feed-generation if times regress.

### Platform (discuss before acting)
- Major IndieWeb protocol additions, dynamic/back-end capabilities, or analytics with data-collection implications — require explicit discussion of privacy trade-offs first.

## ✅ Recently Completed (since 2025-08)

Compact pointer list — see [`../changelog.md`](../changelog.md) for full entries.

- **2026-02** — Review rendering: SVG star ratings, badge dedupe, gradient-ID collision fix
- **2026-01** — ActivityPub Phases 4A (inbox handler), 5A (response semantics), 5B/5F (link attachments & pagination), 6A (RSVP); account migration; Azure SWA queue-trigger fix
- **2025-11** — `Builder.fs` I/O helper refactoring
- **2025-09** — Repository directory cleanup; untagged-content discovery system
- **2025-08** — Azure SWA redirect migration; Azure Blob Storage migration; webmention modernization + timezone-aware date parsing; composable starter-pack system; content structure reorganization; response-type badge specificity; numerous text-only and timeline UX fixes

**Foundational milestones (complete, stable):** unified GenericBuilder migration of all content types · unified + tag RSS feed architecture · enhanced content discovery (search) · text-only accessibility site · desert theme design system · progressive loading for high content volumes · URL alignment ("Cool URIs don't change").

## 🧭 Decision Framework

Used when triaging new ideas (per the autonomous-partnership conventions):

- **🟢 GREEN** — obvious fixes, doc updates, logical next steps with clear benefit → act.
- **🟡 YELLOW** — feature/architecture improvements → propose with rationale first.
- **🔴 RED** — major architecture, platform, data-collection, or privacy-affecting changes → discuss before acting.
