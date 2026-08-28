# ADR-0009: AT Protocol Integration Architecture

## Status
Accepted — Parts A/B live; Part C image phase live; gallery/video dormant

Tracked in [issue #2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) (**Part A, Part B, and
the image phase are live; gallery/video remain unactivated**). Amended 2026-07-02 after a live-protocol validation
round: deterministic TID derivation adopted as the primary record-key design; Track B constraints
(truncation, immutability, forward-only backfill) and the app-password → OAuth trajectory added.
Amended 2026-07-16: **Part A (the `site.standard.publication` node) is now live** —
`at://did:plc:pme7qquljcdx6i4zyawoxypd/site.standard.publication/3mqs7sgylil2w`, verified at
`/.well-known/site.standard.publication` (PRs #2631/#2632); added the Track B **write-scope safety
invariant** to the Decision below.
Amended 2026-08-14: reviewed the
[Bluesky Protocol Services / Jetstream v2 (Network Replay)](https://atproto.com/blog/introducing-bluesky-protocol-services)
release — **no impact on this decision.** The integration is write-only POSSE via raw XRPC (no SDK, no
firehose/Jetstream consumption); `putRecord`/`createSession`/`listRecords` and App Passwords are
unchanged, and no `docs.bsky.app` links exist in source. Jetstream v2 **Network Replay** (stateless HTTP
archive snapshots) is noted as the likely enabling tech for the **deferred PESOS/backfeed** direction
(future ADR-0010). App-Password → OAuth remains *tracked, not scheduled* (no ecosystem cutoff date). Full
impact assessment: `_src/resources/ai-memex/research-bluesky-protocol-services-impact.md`.
Amended 2026-08-14: Part C rich-media POSSE is implemented behind independent image/gallery and video
flags and forward-only cutoffs. Images use native `app.bsky.embed.images` or `app.bsky.embed.gallery`;
video uses `app.bsky.embed.video` through the asynchronous video service. Activation and historical
backfill remain deliberately deferred.
Amended 2026-08-27: the image phase was activated with `useAtProtoMediaImageSync = true` and the
2026-08-01 forward-only cutoff; gallery and video remain independently gated.
Amended 2026-08-27 (Response POSSE): extended the content-type-per-lexicon mapping to **Responses**,
implemented behind four dormant flags (`useAtProtoBookmarkPostsSync`, `useAtProtoResharePostsSync`,
`useAtProtoRepostsSync`, `useAtProtoQuotePostsSync`, all `false`) with `DateTimeOffset.MaxValue` sentinel
cutoffs (activation must set BOTH a flag and a real forward-only date, so a flipped flag alone stages
nothing). Mapping: a public **bookmark** targeting an ordinary URL and an **ordinary-web reshare** each
become an `app.bsky.feed.post` link post (external card → the external target; canonical `lqdev.me`
URL carried in the post text via a UTF-8 byte facet). An **ATProto-targeted reshare** (recognised
strictly from a `bsky.app/profile/{actor}/post/{rkey}` permalink or a literal
`at://{did}/app.bsky.feed.post/{rkey}` URI) becomes an `app.bsky.feed.repost` when the body carries no
authored commentary, or an `app.bsky.feed.post` quote-post (`embed.record`, original not duplicated in
text) when it does — decided by scanning the Markdown source's top-level blocks (any non-blockquote
block = authored commentary). Classification lives in a new pure `AtProtoResponseMapping` module (before
`AtProtoBuilder` in compile order) over a tiny public `ASTParsing.parseMarkdownAst` wrapper. rkey seeds
are namespaced (`bookmark:`, `reshare-link:`, `quote:`, `repost:`) so response records never collide
with Post/Note/media records. **Reposts deliberately carry NO `sourceHash`** (the repost lexicon has no
extension slot): the sync guards them by their natural subject (URI + CID) — created once, never
overwritten. Quote/repost staging carries a `targetRef` sidecar the sync resolves (handle → DID, batched
`app.bsky.feed.getPosts`) into a real subject strongRef, **refusing to write any record whose target is
unresolved, before any write**. Out of scope (unchanged): replies, stars/likes, RSVPs, the private
`app.bsky.bookmark` procedure, PESOS, historical backfill, deletion propagation, and quote-posts with
media (Markdown images or custom media blocks).

## Context

The site already participates in the Fediverse via a hand-built, static+dynamic hybrid ActivityPub
implementation (`ActivityPubBuilder.fs`, see [ADR context in `docs/activitypub/`](../activitypub/ARCHITECTURE-OVERVIEW.md)).
The AT Protocol (the open protocol underlying Bluesky, "the ATmosphere") is a second, distinct
decentralized social protocol. A community-built Lexicon, [`standard.site`](https://standard.site/),
now lets any website publish long-form content as AT Protocol records, and links to those records get
enhanced preview treatment in the Bluesky app. Bluesky's core lexicons (`app.bsky.feed.post`, etc.) are
a second, separate vocabulary for short-form/native content.

Several architectural questions needed answers before any implementation could start:

1. **Identity** — does lqdev.me publish under its existing personal Bluesky account
   (`bsky.app/profile/lqdev.me`), or does the website need its own distinct AT Protocol identity (the
   way its ActivityPub actor, `acct:lqdev@lqdev.me`, is already a separate identity from the personal
   Mastodon account)?
2. **Hosting extent** — does this require running a Personal Data Server (PDS), relay, or AppView, or
   can it ride entirely on Bluesky's existing infrastructure, matching the "static hub, thin dynamic
   spokes" model the ActivityPub implementation already proved out?
3. **Build vs. buy** — an existing open-source CLI (Sequoia) already implements the Standard.site side
   of this problem for static site generators. Should it be adopted, or should this be hand-built like
   the rest of the site's protocol integrations?
4. **Protocol constraints** — AT Protocol's record-key (`rkey`) rules turned out to materially constrain
   the design (see Decision below) and needed to be understood before committing to an approach.

## Decision

**Reuse the existing identity.** Publish all AT Protocol records under the existing
`did:plc:pme7qquljcdx6i4zyawoxypd` / handle `lqdev.me` account (already domain-verified via DNS TXT,
hosted on Bluesky's own PDS at `amanita.us-east.host.bsky.network`). No second AT Protocol identity is
created. Standard.site and native `app.bsky.*` records live in separate record collections in the same
repo — they don't clutter the personal feed unless the record type is itself meant to appear there
(e.g. `app.bsky.feed.post`).

**No self-hosted PDS, relay, or AppView.** Because the existing account already lives on Bluesky's
hosted infrastructure, all four AT Protocol architectural layers (identity, PDS, relay, AppView) already
exist and run today. The only new work is a thin, occasional write path: authenticate once per CI run
and write records via XRPC. This is architecturally *simpler* than the existing ActivityPub
implementation, which had to build an inbox, a followers table, and a delivery queue precisely because
ActivityPub has no equivalent to a relay/AppView.

**Hand-build the integration in F#, mirroring the existing ActivityPub architecture, rather than
adopting a third-party CLI.** The Sequoia CLI (`sequoia.pub`) was evaluated in depth — including two
real production adopters running it in GitHub Actions CI — and rejected in favor of ownership,
consistent with how the static site generator, Webmentions service, and ActivityPub implementation were
all hand-built rather than adopting existing tools. The new integration follows the exact shape already
proven across 8 ActivityPub migrations: types → a single routing function that dispatches by content
type (mirroring `convertToActivity`) → static JSON generation during the normal `dotnet run` → a small
standalone `dotnet fsi` sync script (mirroring `Scripts/send-webmentions.fsx`) invoked from GitHub
Actions, feature-flagged for safe rollout.

**Deterministic TIDs derived from stable metadata, with a content-hash extension field for change
detection.** Unlike ActivityPub — where `generateActivityId` freely mints any URI as an Activity ID
from an MD5 content hash — AT Protocol's `site.standard.document`, `site.standard.publication`, and
`app.bsky.feed.post` Lexicons all mandate `"key": "tid"` (verified directly against the live Lexicon
schema records and the official `app.bsky.feed.post` Lexicon JSON). So the record key cannot be a
content hash. It can, however, still be deterministic: a TID is just a 64-bit integer (53 bits of
microseconds since the epoch + a 10-bit clock identifier) that is normally clock-derived but is
client-generatable, so deriving it from each item's original `published_date` plus a slug-hash
(sub-minute offset + clock ID) yields a spec-valid, rebuild-stable record key. That makes AT-URIs
precomputable at build time — verification `<link>` tags render in the same build, and
`putRecord` becomes a stateless idempotent upsert. A `sourceHash` extension field (Standard.site
Lexicons are explicitly documented as extensible; Bridgy Fed sets extension fields on
`app.bsky.feed.post` in production) carries the same MD5 content hash the ActivityPub implementation
already uses, enabling cheap skip-if-unchanged checks against `listRecords` output. Fallback, should
strict TID-monotonicity enforcement ever appear: omit the rkey, let the PDS mint it, and persist a
`sourceHash → AT-URI` map with a one-build-cycle lag for verification tags.

**Native-lexicon limits shape Track B's publishing semantics.** `app.bsky.feed.post` caps text at 300
graphemes (224 of 292 existing notes exceed it), Bluesky posts are effectively immutable (updates
change the CID and orphan engagement references), and feeds sort by `indexedAt` so bulk backfill would
flood followers' timelines. Notes therefore publish POSSE-style — truncated excerpt plus an
`app.bsky.embed.external` card linking the canonical note URL — as create-only records, forward-only
from the feature's activation date. Long-form documents (Track A) carry none of these constraints and
backfill fully.

**Rich-media POSSE remains outbound-only and forward-only.** Part C reuses the native
`app.bsky.feed.post` collection, placing the canonical `/media/{slug}/` URL in text with a UTF-8 link
facet because a post has one embed union. One to four supported images use `app.bsky.embed.images`;
five to ten use `app.bsky.embed.gallery` with `items` tagged `app.bsky.embed.gallery#image`; a media
post may contain only one MP4 video. The static build stages source URLs and metadata only. The sync
script downloads and validates pending assets after the dry-run, authentication, and write-scope gates,
derives exact image dimensions, uploads all blobs before the first `putRecord`, and polls the video
processing service with bounded retries. Independent flags and explicit cutoffs prevent accidental
backfill or video activation.

**The sync path only ever touches records it created.** Because the integration reuses the existing
identity, the automation writes into the *same* repo that holds the owner's hand-authored Bluesky
activity (~14 `app.bsky.feed.post` records today, plus replies/reposts). Every record the integration
writes carries a `sourceHash` extension field; the sync script filters `com.atproto.repo.listRecords`
output to records bearing that field *before* any create/`putRecord`/delete and never issues a blind,
collection-wide delete. This makes it structurally impossible for the automation to modify or remove a
manually-authored post — the single most important safety property of Track B, and a direct consequence
of sharing an AT Proto identity with the human account (unlike ActivityPub, whose actor
`acct:lqdev@lqdev.me` is a wholly separate identity that carries no such risk). Custom `site.standard.*`
writes additionally pass `validate: false` (the record stores with `validationStatus: "unknown"`, since
the Bluesky PDS cannot resolve the community lexicon — confirmed in Part A's publication create).

## Consequences

**Easier:**
- No new Azure infrastructure (Key Vault, Table Storage, Queue Storage, Azure Functions) — unlike
  ActivityPub, this integration needs none of it, since Bluesky's own relay/AppView handle discovery,
  timelines, and notifications automatically once a record is written.
- A single App Password (stored the same way the existing `ACTIVITYPUB_STORAGE_CONNECTION` secret is)
  is sufficient auth for both the custom Standard.site lexicon and native Bluesky lexicons — no OAuth
  client, no long-lived refresh-token storage, since CI runs are short-lived and authenticate fresh
  each time.
- Full ownership: no dependency on a third-party CLI's release cadence, bugs, or breaking changes; the
  implementation lives entirely in this repo's existing F# conventions.
- Content-type-per-lexicon mapping (Posts → `site.standard.document`, Notes → `app.bsky.feed.post`,
  Media → native embeds, future Responses → other native lexicons) extends cleanly without inventing a one-size-fits-all
  schema, matching how ActivityPub already handles this.

**More difficult:**
- The deterministic-TID design rests on PDSes continuing to accept client-supplied, spec-valid TID
  record keys (true today; vanity and imported rkeys exist in production). If strict monotonic-TID
  enforcement ever lands, the documented fallback (server-minted keys + a persisted `sourceHash →
  AT-URI` map, with a one-build-cycle lag for verification `<link>` tags) must be activated — more
  state, same architecture.
- No off-the-shelf tooling to lean on for edge cases (blob/image upload, rate limits — 5,000
  write-points/hour per DID, Lexicon schema drift) that a maintained third-party CLI would otherwise
  absorb — this repo now owns tracking AT Protocol/Lexicon changes directly.
- App passwords are deprecated (though still functional) in favor of atproto OAuth; the sync script's
  auth must eventually migrate to an OAuth confidential client, so authentication is isolated to a
  single swappable function.
- Track B's published form is lossy by protocol design: notes longer than 300 graphemes appear on
  Bluesky as excerpts with a link-out card, edits after publication are not propagated, and
  pre-activation notes never appear at all.
- Reusing the personal identity means the write path shares a repo with hand-authored posts, so the
  `sourceHash`-filter invariant (see Decision) is load-bearing safety rather than a nicety: any write
  code that skips it risks clobbering real posts. A dry-run diff must gate the first real sync.
- Rich-media uploads add external side effects and asynchronous processing, so the implementation must
  retain strict byte/signature validation, bounded polling, and the all-assets-before-first-write
  boundary. Failed or unsupported media must fail loudly rather than producing a text-only or partial
  native post.
