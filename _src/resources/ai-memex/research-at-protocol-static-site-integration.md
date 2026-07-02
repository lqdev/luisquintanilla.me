---
title: "Research: AT Protocol Integration Options for a Static Site"
description: "Evaluating how to extend lqdev.me's static+dynamic ActivityPub model to AT Protocol/Bluesky — identity, hosting extent, and build-vs-adopt."
entry_type: research
published_date: "2026-07-02 15:00 -05:00"
last_updated_date: "2026-07-02 15:00 -05:00"
tags: "atproto, bluesky, activitypub, fediverse, architecture, research"
related_skill: ""
source_project: "lqdev-me"
---

## Context

lqdev.me already participates in the Fediverse via a hand-built, static+dynamic hybrid ActivityPub
implementation (`ActivityPubBuilder.fs`, Azure Functions for the inbox/delivery, ~99% static / ~1%
dynamic). The question: can the same "static hub, thin dynamic spokes" model extend to the AT Protocol
(the protocol underlying Bluesky, "the ATmosphere")? Three sub-questions needed answers: **identity**
(reuse the existing personal Bluesky account, or mint a new one — mirroring how the ActivityPub actor
`acct:lqdev@lqdev.me` is a separate identity from the personal Mastodon account?), **hosting extent**
(does this require running a PDS/relay/AppView?), and **build vs. adopt** (hand-build vs. use an
existing tool).

## Options Considered

### Identity: reuse existing account vs. mint a new one

- **Reuse `bsky.app/profile/lqdev.me`** — Pros: matches the "one identity, many post types" philosophy
  already stated in `_src/posts/fosdem-2026-social-web-thoughts.md`; the handle is already
  domain-verified (`lqdev.me`, via DNS TXT `_atproto.lqdev.me`); Standard.site/native records live in
  separate record collections, so they don't clutter the personal feed unless the record type itself is
  meant to (like `app.bsky.feed.post`). Cons: less separation between "Luis posting personally" and "the
  website's publication data," if that distinction matters later.
- **Mint a new identity** — Pros: cleaner separation, mirrors the AP actor being distinct from the
  personal Mastodon account. Cons: requires a second domain-verified handle (can't reuse `lqdev.me` for
  two DIDs), fragments discovery (people would need to follow two accounts), contradicts the "one
  account" philosophy explicitly stated in the FOSDEM post.
- **Decision: reuse the existing account.** Verified via live DID resolution
  (`did:plc:pme7qquljcdx6i4zyawoxypd`, PDS at `amanita.us-east.host.bsky.network`) before deciding.

### Hosting extent: self-host infrastructure vs. ride on Bluesky's existing infrastructure

AT Protocol has four layers: DID+handle, PDS, relay, AppView. Because the existing account is already
hosted on Bluesky's own PDS (confirmed via DID doc resolution, not assumed), all four layers already
exist and run. Confirmed via `atproto.com/blog/indexing-standard-site` (written by the author of the
most popular Standard.site publishing tool) that building a *from-scratch indexer* is genuinely hard
(bandwidth costs forced a rewrite from firehose/Tap to Jetstream + Cloudflare) — but not something
lqdev.me needs to do; existing indexers (`docs.surf`, `pub-search.waow.tech`, Bluesky's own AppView)
already do this. **Decision: zero self-hosted infrastructure** — simpler than the existing ActivityPub
implementation, which had to build an inbox/followers-table/delivery-queue precisely because
ActivityPub has no relay/AppView equivalent.

### Build vs. adopt: Sequoia CLI vs. hand-built F#

- **Sequoia CLI** (`sequoia.pub`, MIT, TypeScript) — Pros: purpose-built for exactly "Markdown SSG →
  Standard.site," actively maintained, two real production adopters
  ([rednafi.com](https://rednafi.com/misc/standard-site/),
  [adamdjbrett.com](https://www.adamdjbrett.com/blog/standard-site-eleventy-sequoia/)) running it
  successfully in GitHub Actions CI with an App Password, handles blob upload/CLI ergonomics/state
  tracking out of the box. Cons: a new dependency outside this repo's control; conflicts with the
  project's established preference for owning the whole stack.
- **Hand-built F#** mirroring `ActivityPubBuilder.fs` — Pros: full ownership and control, consistent
  with how the static site generator, Webmentions service, and ActivityPub implementation were all
  hand-built; reuses the exact proven shape (types → routing-by-content-type function → static JSON
  generation at build time → small standalone `dotnet fsi` sync script) already validated across 8
  ActivityPub migrations. Cons: no off-the-shelf handling for edge cases (blob upload, rate limits,
  Lexicon schema drift) that a maintained CLI would otherwise absorb; more code to write and maintain.
- **Decision: hand-built F#.** Explicit user direction: *"Why would I use Sequoia CLI? Why not DIY and
  build my own? I want to own as much as possible."*

## Evaluation Criteria

- **Ownership/control** — weighted highest per explicit user preference (see the "build philosophy"
  user memory: prefers DIY over third-party tools even when a proven off-the-shelf option exists).
- **Consistency with existing codebase conventions** — reusing `ActivityPubBuilder.fs`'s shape
  (Domain Enhancement → Processor Implementation → Migration Validation → Production Deployment)
  minimizes new patterns to learn/maintain.
- **Hosting/operational cost** — zero new Azure infrastructure was strongly preferred, matching the
  existing "~99% static, ~1% dynamic" cost profile.
- **Protocol correctness** — verified live against actual Lexicon schemas and DID documents rather than
  assuming behavior from documentation alone (see the companion pattern entry on the TID/rkey
  constraint discovered during this research).

## Recommendation

Reuse the existing `lqdev.me` Bluesky identity; require zero self-hosted PDS/relay/AppView; hand-build
the integration in F#, mirroring `ActivityPubBuilder.fs`'s proven architecture, with two tracks:
Standard.site `site.standard.document` records for long-form Posts (filling the "article" gap AT
Protocol itself lacks), and native `app.bsky.feed.post` records for short-form Notes (the idiomatic
native lexicon, no third-party schema needed). Full spec captured in
[GitHub issue #2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) and
[ADR-0009](https://github.com/lqdev/luisquintanilla.me/blob/main/docs/adr/0009-at-protocol-integration.md).

## Trade-offs

- Sacrificing Sequoia's maintained edge-case handling (blob upload, rate limits, schema drift tracking)
  in exchange for full ownership — acceptable given the user's stated priority and the relatively small
  surface area needed (two record types, no images/blobs in the MVP scope).
- Sacrificing "ship faster" in exchange for architectural consistency — acceptable since this is a
  spec-only deliverable for now; a future session implements it with full context already researched.
- Media, Responses, and RSVP content types are explicitly deferred rather than attempting full parity
  with the ActivityPub implementation immediately — AT Protocol's native reply/like/repost lexicons only
  support strong-ref targets to other AT Protocol records, so most of lqdev.me's "Responses" (which
  target arbitrary external URLs) don't map cleanly yet.
