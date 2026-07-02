# ADR-0009: AT Protocol Integration Architecture

## Status
Proposed

Tracked in [issue #2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) (shovel-ready spec,
not yet implemented).

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

**Content-hash extension field instead of a computed record key.** Unlike ActivityPub — where
`generateActivityId` freely mints any URI as an Activity ID from an MD5 content hash — AT Protocol's
`site.standard.document`, `site.standard.publication`, and `app.bsky.feed.post` Lexicons all mandate
`"key": "tid"` (verified directly against the live Lexicon schema records and the official
`app.bsky.feed.post` Lexicon JSON). A Timestamp Identifier can't be precomputed client-side the way a
content hash can, so the AT-URI for a given piece of content is only known *after* the PDS creates the
record. Since Standard.site Lexicons are explicitly documented as extensible, the same stable,
content-addressed idempotency check the ActivityPub implementation already relies on is preserved by
embedding the hash as an additional `sourceHash` field on the record, rather than as the record key
itself.

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
  future Media/Responses → other native lexicons) extends cleanly without inventing a one-size-fits-all
  schema, matching how ActivityPub already handles this.

**More difficult:**
- The verification `<link rel="site.standard.document">` tag can't be computed in the same build pass
  that creates the record (TIDs aren't known until after creation) — requires a persisted
  hash-to-AT-URI map read back into the next build's ViewEngine rendering, a one-build-cycle lag for
  brand-new posts. This is an inherent protocol constraint, not something ownership avoids.
- No off-the-shelf tooling to lean on for edge cases (blob/image upload, rate limits, Lexicon schema
  drift) that a maintained third-party CLI would otherwise absorb — this repo now owns tracking AT
  Protocol/Lexicon changes directly.
- Two independent sync/idempotency strategies are needed: `path`-based matching for Track A (Standard.site
  documents have a natural `path` field) versus a small committed state file for Track B (native
  `app.bsky.feed.post` has no equivalent field).
