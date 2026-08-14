---
title: "Research: Bluesky Protocol Services & Jetstream v2 — Impact on lqdev.me's AT Proto Integration"
description: "Assessing whether the 2026-08-14 Bluesky Protocol Services / Jetstream v2 (Network Replay) release affects lqdev.me's write-only POSSE AT Protocol integration. Conclusion: no functional impact; one forward-looking enabler for the parked backfeed idea."
entry_type: research
published_date: "2026-08-14 10:57 -05:00"
last_updated_date: "2026-08-14 10:57 -05:00"
tags: atproto, bluesky, jetstream, posse, architecture, research
related_skill: query-ai-memex
source_project: lqdev-me
related_entries: research-at-protocol-static-site-integration, pattern-standalone-content-type-search-not-firehose, project-report-atproto-integration-shipped
---

## Context

On 2026-08-14 Bluesky launched
[Bluesky Protocol Services](https://atproto.com/blog/introducing-bluesky-protocol-services): a new
`bsky.network` brand and website for the public AT Protocol infrastructure it operates, **retiring the
old `docs.bsky.app` site**. The release bundles several things:

- **Jetstream v2 — Network Replay** (the headline). A compressed archive of the whole network, consumable
  two ways: *Network Replay* (`POST` filters to `planSnapshot`, download sealed segments over plain HTTP,
  then cut over to the live WebSocket at the tip with no gap) or a pure point-in-time *snapshot*
  (`listSegments` + `getSegment`, HTTP only, no WebSocket). New `network.bsky.jetstream.*` methods
  (`planBackfill`, `listSegments`, `getSegment`, `getBlock`). The **live tail stays open and
  unauthenticated**; only **archive** requests now require a `bsky.network` API token. New instances at
  `wss://jetstream.us-{west,east}.bsky.network`; v1 keeps running unchanged.
- **Jetstream SDKs** — new TypeScript and Go clients.
- **The Bluesky TypeScript SDK rebased on `@atproto/lex`** (`@bsky/sdk`); the legacy `@atproto/api`
  continues to work.
- **`endpoints.bsky.app` HTTP reference updated**, spun out of `docs.bsky.app`.

The question: **how does this affect lqdev.me's AT Protocol integration, if at all?**

## Our integration in one paragraph (what actually matters here)

lqdev.me is a live AT Protocol node — see [[project-report-atproto-integration-shipped]]. The
integration is **write-only POSSE**, across two tracks that are both **activated and committing on every
push to `main`**:

- **Track A** — Posts → `site.standard.document` (`AtProtoBuilder.useAtProtoSync = true`).
- **Track B** — Notes → native `app.bsky.feed.post` (`AtProtoBuilder.useAtProtoNotesSync = true`).

The write path is a standalone `dotnet fsi` script (`Scripts/sync-atproto.fsx`) that calls **raw XRPC**
over `HttpClient` — `com.atproto.server.createSession` (App Password auth), `com.atproto.repo.listRecords`
(unauth read, for the skip-if-unchanged plan), and `com.atproto.repo.putRecord` (idempotent upsert). The
PDS endpoint is resolved dynamically from the DID document via `plc.directory`. There is **no SDK**, and
we **never subscribe to the firehose or Jetstream** — discovery, timelines, and notifications are handled
downstream by Bluesky's own relay/AppView once a record is written. That "publish, never subscribe" shape
is the whole reason this release barely touches us.

## Impact by surface

| Announcement item | Relevance to our integration |
|---|---|
| **Jetstream v2 / Network Replay** (`network.bsky.jetstream.*`, archive API token) | **Consumption-only.** We publish, never subscribe. Zero impact on the write path. |
| **Jetstream SDKs (TS/Go)** | Not used — we call XRPC over `HttpClient`. No impact. |
| **Bluesky TS SDK rebased on `@atproto/lex`** (`@atproto/api` still works) | Not used. No impact. |
| **`endpoints.bsky.app` HTTP reference update** | Our methods (`com.atproto.repo.putRecord` / `listRecords`, `com.atproto.server.createSession`) are core, stable, and hit the **PDS** (resolved from the DID doc), not `bsky.app`. No impact. |
| **`docs.bsky.app` retired → `bsky.network`** | **No stale links in our source.** Our spec links point to stable `atproto.com/specs/*` + `standard.site`; only `bsky.app/profile/...` app/RSS links exist (unaffected). |
| **App Passwords / auth** | Unchanged — see below. The new token requirement is scoped to Jetstream *archive* requests, which we never make. |

**Net: no functional impact.** Nothing in the release changes `putRecord`, `createSession`,
`listRecords`, DID/PDS resolution, or the `site.standard.*` / `app.bsky.feed.post` lexicons we write. No
code change is required to keep the integration working exactly as it does today.

## Auth (App Password → OAuth): no change, no work item

Worth stating explicitly because it's an easy thing to conflate with a release like this:

- **App-Password auth is live and exercised.** CI authenticates every push via
  `com.atproto.server.createSession` using the `ATPROTO_APP_PASSWORD` secret (both tracks run with
  `--commit`). So this is not a dormant code path.
- **This release does not touch App Passwords.** The only new token requirement is for Jetstream
  **archive** requests — a consumption feature we don't use. The live tail remains unauthenticated.
- **No ecosystem deadline.** As of 2026-08 there is **no published hard cutoff / sunset date** for App
  Passwords; the OAuth migration is gated on Bluesky *announcing* a deprecation date. It is
  **tracked, not scheduled**.
- **Already isolated for a clean future swap.** ADR-0009 records the App-Password → atproto-OAuth
  trajectory and deliberately keeps auth in a single swappable function (`createSession` call), so the
  eventual migration is a localized change.
- **Conclusion: no action, no backlog item.** There is nothing to do here now, and this release does not
  change that.

## The one real relevance: Jetstream v2 Network Replay ⇒ the parked backfeed

The single place this release intersects our roadmap is the **deferred PESOS / backfeed idea** parked in
`projects/backlog.md` (2026-07-16): *"whether to also pull existing Bluesky posts back into the site as
an archive (distinct from Part B's site→AT-Proto POSSE)... would warrant its own issue + ADR-0010."*

We deliberately don't consume the firehose today. Prior research
([[research-at-protocol-static-site-integration]]) found that building a from-scratch indexer is
genuinely hard — bandwidth costs forced ecosystem tools to rewrite from firehose/Tap to Jetstream +
Cloudflare — and our content model intentionally keeps some types out of any firehose-style stream
([[pattern-standalone-content-type-search-not-firehose]]). Jetstream v2 **Network Replay** materially
lowers the barrier for the parked idea:

- **Stateless HTTP archive snapshots** (`planSnapshot` / `listSegments` / `getSegment`) — a
  point-in-time copy of just the slice you filter for, over plain HTTP. No WebSocket, no per-consumer
  cursor to persist, no subscription to register, nothing to self-host.
- **Fits our "static hub, thin dynamic spoke" model** — a backfeed could be a post-build `dotnet fsi`
  step in the same shape as `Scripts/sync-atproto.fsx` and `Scripts/send-webmentions.fsx`, rather than a
  standing service.
- **Caveat:** archive requests now require a `bsky.network` API token (one-time account setup); the live
  tail is still open/unauthenticated.

This makes Jetstream v2 Network Replay the **likely enabling technology** whenever the backfeed direction
is revisited — but it is forward-looking, not a change to today's integration.

## Recommendation

- **No action on the current integration.** It is unaffected; keep publishing as-is.
- **Documentation-only capture:** this entry, a light ADR-0009 "ecosystem watch" note, and a backlog
  pointer noting Jetstream v2 Network Replay as the enabling tech for the parked backfeed (and the
  archive API-token requirement).
- **Re-evaluate** Jetstream v2 Network Replay if/when the PESOS/backfeed direction is picked up — that
  work would land under its own issue + **ADR-0010**.

## References

- [Introducing Bluesky Protocol Services](https://atproto.com/blog/introducing-bluesky-protocol-services)
  · [bsky.network](https://bsky.network)
- [AT Protocol](https://atproto.com/) · [TID spec](https://atproto.com/specs/tid) ·
  [Record Key spec](https://atproto.com/specs/record-key)
- [ADR-0009 — AT Protocol Integration](https://github.com/lqdev/luisquintanilla.me/blob/main/docs/adr/0009-at-protocol-integration.md)
  · [issue #2574](https://github.com/lqdev/luisquintanilla.me/issues/2574)
- Related: [[research-at-protocol-static-site-integration]] ·
  [[pattern-standalone-content-type-search-not-firehose]] ·
  [[project-report-atproto-integration-shipped]]
