# ADR-0010: WebSub — Publisher Support and a Self-Hosted Forgetful Hub

## Status
Proposed — 2026-10-30. No code yet. Design detail lives in
[`docs/websub/ARCHITECTURE-OVERVIEW.md`](../websub/ARCHITECTURE-OVERVIEW.md).

Supersedes the one-line "WebSub support" item in `projects/backlog.md` (Tools & automation).
Does **not** touch the deferred PESOS/backfeed direction noted in [ADR-0009](0009-at-protocol-integration.md);
that remains a separate future ADR.

## Context

### What exists today

The site publishes ~14 canonical RSS feeds plus ~1,555 per-tag feeds (`UnifiedFeeds.fs`), four
legacy byte-copy aliases (`Builders/LegacyFeeds.fs`: `/blog.rss`, `/microblog.rss`, `/responses.rss`,
`/all.rss`), and JSON Feed 1.1 equivalents (`UnifiedFeeds.fs:222-275`). Measured on 2026-10-30 the
largest canonical feed is `/posts/feed.xml` at 352 KB / 20 items; most are 29–130 KB. Every feed is
re-generated on every deploy by `.github/workflows/publish-azure-static-web-apps.yml`.

Readers discover new content by **polling**. Nothing in the build or the workflow notifies anyone.

### The IndieWeb building-block audit

A 2026-10-30 audit of the site against the IndieWeb building blocks found the publishing side strong
(domain, permalinks, microformats2, IndieAuth, Webmention send + receive endpoint, ActivityPub, AT
Protocol POSSE, OPML rolls, text-only site) and identified **WebSub** as the one remaining block that
materially improves the experience of people *already following* the site — push delivery in seconds
instead of poll latency of minutes to hours — at near-zero publisher cost.

### Why consider running a hub, not just using one

The publisher-side change is identical regardless of hub. The question is whether to point at a public
hub (Switchboard, Google's `pubsubhubbub.appspot.com`) or run `hub.lqdev.me`.

Arguments for running one:

- **It fits the established pattern.** `indieauth.lqdev.tech` and `webmentions.lqdev.tech` are
  already thin, hand-built spoke services around the static hub. The ActivityPub `api/` already
  implements verify → fetch → signed fan-out with a delivery queue; a WebSub hub is the same shape.
- **Only two or three widely-used open hubs exist.** Switchboard is one person's VPS. Another open,
  well-run hub is a public good for the IndieWeb.
- **A hub is a privacy chokepoint, and that can be designed *away*.** A hub necessarily sees
  `(topic, subscriber-callback)` pairs — i.e. who reads what. The site's stated posture (no display of
  others' content; "aggregation as a privacy default" in the backlog) can be expressed structurally: a
  hub that stores hashes instead of bodies, encrypts callbacks at rest, keeps no per-request logs, and
  hard-deletes on lease expiry. That design is a second concrete instance of the privacy principle the
  `/now-playing` work is meant to demonstrate.
- **Cost is effectively zero.** Azure Functions Consumption + Table Storage at hobby scale is within the
  free grant; the only scaling cost is egress, bounded by caps.

Arguments against:

- **Operational commitment.** Subscribers and other publishers depend on uptime. This is a service, not
  a feature.
- **New custody.** Even forgetful, the hub holds data about other people for the first time.

### Prerequisite discovered during the audit

`UnifiedFeeds.fs:128` sets `<lastBuildDate>` to `DateTimeOffset.UtcNow`, so every feed's bytes change on
every deploy even when no items changed. This defeats conditional GET (`ETag` / `If-Modified-Since`),
makes "did this feed change?" undecidable for a hub, and would cause a hub to re-fan-out unchanged
content on every ping. It must be fixed before any hub is pointed at these feeds.

## Decision

### 1. Add WebSub publisher support to the site (Phase 0)

- Emit in **canonical feeds only** (the ~14 content-type and collection feeds; **not** the 1,555 tag
  feeds), inside `<channel>`:
  ```xml
  <atom:link rel="hub"  href="{hubUrl}"/>
  <atom:link rel="self" href="{canonicalFeedUrl}"/>
  ```
  with `xmlns:atom="http://www.w3.org/2005/Atom"` on `<rss>`. The hub URL is a single constant
  (`Constants.Urls.websubHub`).
- `rel="self"` always names the **canonical** path (`/posts/feed.xml`, never `/blog.rss`). Because
  aliases are byte copies, they inherit the canonical `self` automatically and collapse into one topic.
- JSON Feed: add the equivalent `"hubs": [{ "type": "WebSub", "url": "{hubUrl}" }]`.
- Fix `<lastBuildDate>` to `max(item pubDate)` so unchanged feeds are byte-identical across builds.
- Add one post-deploy workflow step that POSTs `hub.mode=publish&hub.url={feed}` for each canonical
  feed, after confirming the deployed feed is fresh (retry until the newest item's GUID is visible).
- **Start with Switchboard (`https://switchboard.p3k.io/`)** as `hubUrl`. Zero infrastructure; the
  publisher work is identical whichever hub is used, and swapping is a one-constant change.

### 2. Build a self-hosted, forgetful hub (Phases 1–3) — privacy by structure

Build `hub.lqdev.me` as a new repository, F# on Azure Functions (isolated worker, Consumption plan),
Table Storage, one Key Vault key, **Application Insights disabled**. Full design in the
architecture overview. The non-negotiable invariants:

| Invariant | Meaning |
|---|---|
| **Hash, don't store** | Topics keep a set of entry-ID hashes + `ETag`/`Last-Modified`; feed bodies live only in one invocation's memory. Retries re-fetch rather than persist payloads. No delivery queue with bodies. |
| **Encrypt the graph** | Callback URLs and HMAC secrets are encrypted at rest with a Key Vault key. Partition/row keys are SHA-256 of topic and callback. A dumped table yields counts, not relationships. |
| **No per-request logs** | No App Insights, no storage analytics, no platform diagnostics. One daily aggregate row (`topics`, `subscriptions`, `deliveries_ok`, `deliveries_failed`). |
| **No IPs** | Rate limiting via in-memory token buckets or `sha256(ip + rotating daily salt)` with 1-hour TTL. Raw IPs are never written. |
| **Forgetting is the default** | Lease max 7 days; hard-delete on expiry and immediately on unsubscribe; delete a topic when its last subscription is gone. |
| **No visibility into who-reads-what** | No endpoint, page, or admin view lists subscribers for a topic — not even for the operator's own feeds. |
| **Refuse private topics** | Reject `hub.url` with userinfo or query parameters matching `key|token|auth|secret|sig`. |
| **SSRF guard** | Resolve DNS; refuse private, loopback, link-local, and cloud-metadata ranges; HTTP(S) only; 2 MB body cap; 10 s timeout. Applied to topic fetches and callback POSTs. |

### 3. Bound the hub so "open" means "finite"

Topics are the only dimension where a stranger's choices become operator cost (arbitrary GETs of
arbitrary size). Publish and enforce:

| Limit | Value |
|---|---|
| Topic created only on a **verified subscribe** | — (publish pings for unknown topics are no-ops) |
| Topic must declare **this** hub via `rel=hub` | — (checked on subscribe) |
| Topics per registrable domain | 25 |
| Global topics | 1,000, raised deliberately |
| Subscribers per topic | 500 |
| Feed size | 2 MB |
| Publish coalescing | ≤ 1 fetch / 60 s / topic |
| Lease | ≤ 7 days |

No domain allowlist as the primary model; an allowlist exists only as an emergency switch.

### 4. Phase the rollout; each phase is independently shippable and reversible

| Phase | Scope | Where |
|---|---|---|
| 0 | Publisher support + `lastBuildDate` fix + workflow ping, pointing at Switchboard | this repo |
| 1 | Hub built and run **privately** for this site's feeds only | new repo `lqdev/websub-hub` (name TBD) |
| 2 | `Constants.Urls.websubHub` → `https://hub.lqdev.me/`; Switchboard subscriptions age out within their leases | this repo |
| 3 | Open to the public: caps, SSRF guard, privacy + limits statement at `/`, announce | hub repo |

Phase 3 is a conscious go/no-go, not an automatic consequence of Phase 2.

## Consequences

**Easier**
- Followers using WebSub-aware readers (Inoreader, Feedbin, NewsBlur, Feedly, Miniflux, FreshRSS,
  Aperture/Microsub) see posts within seconds of deploy. Readers without WebSub are unaffected.
- Unchanged feeds become byte-stable across builds, so conditional GET works for *all* readers — a
  bandwidth win independent of WebSub.
- The hub, if built, is a second worked example of "privacy by structure" alongside `/now-playing`,
  and a reusable pattern (verify → hash-diff → signed fan-out, zero retention).
- Swapping hubs is a one-constant change; Phase 0 carries no lock-in.

**Harder / costs**
- A new always-on service to keep healthy (Phases 1+). Expected money cost ≈ $0–1/month private,
  $3–8/month if public at modest scale; the real cost is operator attention.
- Opening the hub (Phase 3) creates custody of third-party data, however minimal, and a GDPR controller
  role in principle. The published statement and the invariants above are the mitigation; the
  go/no-go remains open.
- The workflow's post-deploy phase grows by one step and must wait for SWA propagation before pinging.
- Tag feeds deliberately get no `rel=hub`; anyone subscribed to a tag feed keeps polling.

**Deliberately out of scope**
- Displaying received webmentions or any third-party content on the site (unchanged policy).
- A hub for non-feed topics (arbitrary HTML pages) — spec-permitted, not offered.
- Polling fallback for publishers who never ping.
- Any analytics beyond the daily aggregate row.
