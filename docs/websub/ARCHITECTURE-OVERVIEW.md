# WebSub — Architecture Overview

Design companion to [ADR-0010](../adr/0010-websub-publisher-and-hub.md). Covers both halves:
the publisher-side changes in this repository (Phase 0/2) and the self-hosted hub (Phases 1–3,
separate repository). **No code exists yet**; this document is the spec to build from.

Spec reference: [WebSub, W3C Recommendation 23 Jan 2018](https://www.w3.org/TR/websub/).

---

## 1. Roles and vocabulary

| Term | Meaning here |
|---|---|
| **Topic** | A URL whose content changes over time — for this site, a canonical feed URL. |
| **Publisher** | This site. Declares a hub in each topic and pings the hub after deploy. |
| **Hub** | Holds `(topic → callbacks)`, fetches the topic on ping, diffs, pushes new entries. |
| **Subscriber** | Reader *software* with a public callback URL (Inoreader, Feedbin, Miniflux, FreshRSS, Aperture…). Never a person or browser. |
| **Lease** | Subscription lifetime, in seconds. Subscribers renew; the hub deletes on expiry. |

Publishers do not register with a hub. Subscribers discover the hub from the topic. The hub learns
about a topic the first time a subscriber asks for it.

---

## 2. Publisher side (this repository)

### 2.1 Inventory of topics (measured 2026-10-30, `_public/`)

| Canonical feed | Size | Items | Aliases (byte copies) |
|---|---|---|---|
| `/posts/feed.xml` | 352 KB | 20 | `/blog.rss` |
| `/resources/presentations/feed.xml` | 252 KB | 7 | |
| `/resources/ai-memex/feed.xml` | 233 KB | 20 | |
| `/resources/wiki/feed.xml` | 225 KB | 20 | |
| `/collections/playlists/feed.xml` | 136 KB | 20 | |
| `/resources/snippets/feed.xml` | 125 KB | 15 | |
| `/reviews/feed.xml` | 123 KB | 20 | |
| `/bookmarks/feed.xml` | 40 KB | 20 | |
| `/media/feed.xml` | 33 KB | 19 | |
| `/notes/feed.xml` | 32 KB | 20 | `/microblog.rss` |
| `/feed/feed.xml` | 29 KB | 20 | `/all.rss`, `/feed/index.xml` |
| `/responses/feed.xml` | 29 KB | 20 | `/responses.rss`, `/feed/responses/index.xml` |
| `/rsvp/feed.xml` | 7 KB | 5 | |
| `/collections/albums/feed.xml` | 2 KB | 2 | |
| `/marketplace/feed.xml` | 2 KB | 1 | `/marketplace.rss` |

**Excluded**: the 1,555 `/tags/*/feed.xml` feeds (26 MB total). They change rarely, nobody needs them
pushed, and including them would make this one domain exceed any reasonable per-domain topic cap.

### 2.2 Feed changes (`UnifiedFeeds.fs`)

In `generateRssFeed` (`UnifiedFeeds.fs:127`):

```xml
<rss version="2.0"
     xmlns:source="http://source.scripting.com/"
     xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    …
    <atom:link rel="hub"  href="https://switchboard.p3k.io/"/>
    <atom:link rel="self" href="https://www.lqdev.me/posts/feed.xml"/>
```

- `rel="self"` is **required** by the spec for the hub to identify the topic. It must always be the
  canonical path. Aliases produced by `Builders/LegacyFeeds.fs` are `File.Copy`s, so they carry the
  canonical `self` and resolve to the same topic — no fragmentation, nothing to ping twice.
- Gate emission on a `FeedConfiguration` flag (e.g. `WebSubEnabled: bool`), true for the canonical
  set above, false for tag feeds.
- The hub URL is `Constants.Urls.websubHub`. Changing hubs is changing one string.

In the JSON Feed writer (`UnifiedFeeds.fs:222`), per JSON Feed 1.1:

```json
"hubs": [ { "type": "WebSub", "url": "https://switchboard.p3k.io/" } ]
```

### 2.3 Prerequisite: byte-stable feeds

`UnifiedFeeds.fs:128` — `let buildDate = formatRssDate DateTimeOffset.UtcNow` — makes every feed
differ on every build. Replace with the newest item's publication date (fall back to a fixed epoch for
empty feeds). Effects:

- Unchanged feeds are byte-identical across deploys → Azure SWA serves a stable `ETag` → readers'
  conditional GETs return `304`.
- A hub's `ETag`/`If-Modified-Since` probe short-circuits before download.
- A hub's entry-ID diff is meaningful: no items changed ⇒ no fan-out.

This should ship **before** any ping is sent, including to Switchboard.

### 2.4 Workflow step (`.github/workflows/publish-azure-static-web-apps.yml`)

Add after `Build And Deploy` (line ~158), in the same job:

```yaml
- name: Notify WebSub hub
  env:
    HUB: https://switchboard.p3k.io/
  run: |
    set -euo pipefail
    for path in /feed/feed.xml /posts/feed.xml /notes/feed.xml /responses/feed.xml \
                /bookmarks/feed.xml /media/feed.xml /reviews/feed.xml /rsvp/feed.xml \
                /resources/snippets/feed.xml /resources/wiki/feed.xml \
                /resources/presentations/feed.xml /resources/ai-memex/feed.xml \
                /collections/playlists/feed.xml /collections/albums/feed.xml \
                /marketplace/feed.xml; do
      url="https://www.lqdev.me${path}"
      # Wait for SWA propagation: the deployed feed must match what we just built.
      local_hash=$(sha256sum "_public${path}" | cut -d' ' -f1)
      for i in $(seq 1 12); do
        remote_hash=$(curl -fsSL "$url" | sha256sum | cut -d' ' -f1)
        [ "$remote_hash" = "$local_hash" ] && break
        sleep 10
      done
      curl -fsS -X POST "$HUB" \
        --data-urlencode "hub.mode=publish" \
        --data-urlencode "hub.url=${url}" >/dev/null || echo "::warning::ping failed for ${url}"
    done
```

Notes:
- The propagation wait matters: if the hub fetches before SWA has propagated, it sees the old bytes
  and finds no diff. Hash comparison is exact; a GUID check would also work.
- Pinging all 15 every deploy is fine: with §2.3 in place, unchanged topics cost the hub one
  conditional GET and nothing else.
- Better: derive the list from the build (write `_public/websub-topics.txt` from the same
  `FeedConfiguration` flag) so the workflow never drifts from the code.
- Pings are unauthenticated by design. A stranger pinging about these feeds just causes a re-fetch.

### 2.5 Phase 2: switch hubs

Change `Constants.Urls.websubHub` and the workflow `HUB` env (or read it from the generated topics
file). Existing Switchboard subscriptions expire within their lease (typically ≤ 10 days); readers
re-discover the new hub from `rel="hub"` on their next poll and re-subscribe there. No coordination
with anyone required.

---

## 3. The hub (`hub.lqdev.me`, separate repository)

### 3.1 Design principle

**Cheap because forgetful.** Every byte not stored is a byte not paid for and not leakable. The hub is
built to be structurally unable to answer "who reads what" — the same "aggregation as a privacy
default" idea as `/now-playing`, applied to infrastructure.

### 3.2 Platform

| Component | Choice | Why |
|---|---|---|
| Compute | Azure Functions, **Consumption** plan, F# isolated worker (.NET 10) | Idle = free; 1M executions / 400k GB·s free grant; matches the site's stack |
| State | Azure **Table Storage** (one storage account) | Cheapest durable store; a few thousand rows |
| Key | Azure **Key Vault**, one RSA/AES key | Encrypt callbacks + secrets at rest |
| Scheduling | **Timer trigger** for lease sweep | Free on Consumption |
| Queue | **None** | No persisted payloads; see §3.5 |
| Telemetry | **Application Insights OFF**, no diagnostic settings, no storage analytics | Biggest single privacy *and* cost item: AI would log every callback URL and bill past 5 GB |
| Domain | `hub.lqdev.me` with Functions managed certificate | Free |

Not used: SWA-managed functions (HTTP-only — no timer), Cosmos, Service Bus, Durable Functions,
Premium plan. Estimated cost: ≈ $0.10–0.50/month private; ≈ $3–8/month public at modest scale;
egress is the only component that scales.

### 3.3 Endpoints

All at `https://hub.lqdev.me/`.

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/` | Human-readable page: what this is, how to use it, **limits**, **privacy statement** (§3.8) |
| `POST` | `/` | WebSub endpoint. Dispatch on `hub.mode`: `subscribe`, `unsubscribe`, `publish` |
| `GET` | `/health` | `200` + daily aggregate counters (no per-topic data) |
| `POST` | `/sweep` | Optional manual trigger for the lease sweep (function-key protected); the timer is the normal path |

The spec requires a single hub URL for both subscribe and publish; keep `/` as that URL.

### 3.4 Storage schema

```
Table: Subscriptions
  PartitionKey = sha256(topicUrl)          -- groups a topic's subscribers
  RowKey       = sha256(callbackUrl)
  encCallback  : bytes   -- callback URL, encrypted (Key Vault key)
  encSecret    : bytes?  -- hub.secret, encrypted; null if subscriber gave none
  expiresUtc   : DateTimeOffset
  failures     : int     -- consecutive delivery failures (reset on success)

Table: Topics
  PartitionKey = sha256(topicUrl)
  RowKey       = "topic"
  topicUrl     : string  -- plaintext: required to fetch; not personal data
  domain       : string  -- registrable domain (eTLD+1), for the per-domain cap
  entryHashes  : string  -- JSON array of sha256(entry id | guid | link) for the last fetch
  etag         : string?
  lastModified : string?
  lastFetchUtc : DateTimeOffset
  subCount     : int     -- maintained on subscribe/unsubscribe/sweep

Table: Stats
  PartitionKey = "daily"
  RowKey       = yyyy-MM-dd
  topics, subscriptions, deliveriesOk, deliveriesFailed : int

Table: RateLimit                                   -- optional; prefer in-memory buckets
  PartitionKey = sha256(ip + dailySalt)
  RowKey       = minuteBucket
  count        : int
  (TTL: purge rows older than 1 h in the sweep)
```

Deliberately absent: feed bodies, delivery payloads, raw IPs, user agents, per-delivery timestamps,
request logs, subscriber ↔ reader-service mapping.

### 3.5 Flows

**Subscribe / unsubscribe** (`hub.mode=subscribe|unsubscribe`, form-encoded)

1. Validate `hub.topic`, `hub.callback` (absolute HTTP(S); SSRF guard §3.7; no credentials §3.7).
2. For `subscribe`:
   - If topic unknown: fetch it (conditional GET not applicable yet), require it to parse as
     RSS/Atom/JSON Feed **and** to declare `rel="hub"` pointing at this hub. Enforce feed-size cap.
   - Enforce caps: per-domain topics, global topics, subscribers per topic (§3.6).
3. **Intent verification**: `GET {callback}?hub.mode=…&hub.topic=…&hub.challenge={random}&hub.lease_seconds={lease}`.
   Require `2xx` with body == challenge. This is what prevents the hub being used to spam arbitrary
   URLs.
4. Upsert `Subscriptions` row (encrypt callback/secret), clamp lease to ≤ 7 days, update `subCount`.
   For `unsubscribe`: hard-delete the row; delete the topic if `subCount` hits 0.
5. Respond `202 Accepted` to the original POST **before** verification completes if verification is
   made asynchronous; synchronous verification within the request is acceptable at this scale and is
   simpler. Return `4xx` with a plain-text reason on validation failure.

**Publish** (`hub.mode=publish`, `hub.url` one or more)

1. For each URL: if no `Topics` row → **no-op** (`202` regardless; do not create topics on ping).
2. Coalesce: if `lastFetchUtc` < 60 s ago → `202`, no fetch.
3. Fetch with `If-None-Match`/`If-Modified-Since`. `304` → done.
4. Parse; compute entry hashes; `newEntries = fetched − stored`. If empty → update
   `etag`/`lastModified`/`lastFetchUtc`, done.
5. Build the delivery body: the feed document containing **only new entries** (spec §7: the hub MAY
   send a partial document). Hold it in memory only.
6. For each subscriber (decrypt callback/secret): `POST {callback}` with
   `Content-Type` of the feed, `Link: <{hub}>; rel="hub", <{topic}>; rel="self"`, and
   `X-Hub-Signature: sha256={HMAC(secret, body)}` when a secret exists.
   - Inline retry: 3 attempts, backoff 2 s / 8 s / 30 s, 10 s timeout each.
   - On final failure increment `failures`; delete the subscription at 10 consecutive failures.
7. Store new `entryHashes`, `etag`, `lastModified`, `lastFetchUtc`. **Discard the body.**
8. Increment `Stats`.

No queue: the Consumption plan allows 5–10 minutes per invocation, ample for ≤ 500 callbacks with
bounded retries. If a run is cut short, the next ping re-fetches and re-diffs; the entry hashes are
updated only after fan-out, so missed subscribers are retried then.

**Sweep** (timer, hourly)

- Delete `Subscriptions` where `expiresUtc < now`; decrement/delete topics accordingly.
- Purge `RateLimit` rows older than 1 h.
- Roll the daily `Stats` row.

### 3.6 Limits (published on `/`)

| Limit | Value | Enforced at |
|---|---|---|
| Topic exists only after a verified subscribe | — | publish (no-op), subscribe |
| Topic must declare this hub (`rel=hub`) | — | subscribe |
| Topics per registrable domain | 25 | subscribe |
| Global topics | 1,000 (raise deliberately; `503` when full) | subscribe |
| Subscribers per topic | 500 | subscribe |
| Feed size | 2 MB | subscribe, publish fetch |
| Fetch timeout | 10 s | all outbound |
| Publish coalescing | ≤ 1 fetch / 60 s / topic | publish |
| Lease | ≤ 7 days (604 800 s); default 7 days if unspecified | subscribe |
| Consecutive delivery failures before drop | 10 | publish |
| Subscribe/publish requests per IP | 60 / min | all inbound |

Cost bound these imply: 1,000 topics × 2 MB × (1 fetch/min worst case) is the theoretical ceiling;
realistic load (~50 KB feeds, ~10 pings/day) is ≈ 15 GB/month, inside Azure's free egress.

### 3.7 Security

- **SSRF**: before any outbound request, resolve the hostname and refuse if any address is in
  `10/8`, `172.16/12`, `192.168/16`, `127/8`, `169.254/16` (incl. `169.254.169.254` metadata),
  `::1`, `fc00::/7`, `fe80::/10`, or `0.0.0.0`. Refuse non-HTTP(S) schemes and non-default ports
  other than 80/443/8080/8443. Re-check on redirects (max 3). Applies to topics *and* callbacks.
- **Credentialed topics**: refuse `hub.url`/`hub.topic` with userinfo or query keys matching
  `/^(key|token|auth|secret|sig|signature|api_key|apikey)$/i`.
- **Callback integrity**: subscribers that supply `hub.secret` get HMAC-SHA256 signatures; the `/`
  page recommends it.
- **Secrets at rest**: Key Vault key; Function identity has `unwrap`/`decrypt` only. Rotate yearly.
- **Rate limiting without IPs**: in-memory token bucket per instance (acceptable on Consumption's
  single-instance-at-idle behaviour); fall back to the salted `RateLimit` table under scale-out.
- **No admin API** that enumerates subscribers. Operator debugging uses aggregate counters only.

### 3.8 Privacy statement (to publish verbatim on `/`)

> **What this hub stores.** For each feed ("topic") someone has subscribed to: the feed URL, a set of
> hashes of its entry IDs, and HTTP cache headers. For each subscription: the subscriber's callback URL
> and optional secret, both encrypted at rest, plus an expiry time.
>
> **What it never stores.** Feed contents. Delivery payloads. IP addresses. User agents. Request logs.
> Any record of which entries were delivered to which subscriber. Any association between a
> subscriber and a person.
>
> **How long.** Subscriptions are deleted when their lease (max 7 days) expires or on unsubscribe,
> whichever is first. A topic is deleted when its last subscription is gone. There are no backups of
> subscription data.
>
> **What the operator can see.** Daily totals: number of topics, number of subscriptions, number of
> successful and failed deliveries. Nothing per topic or per subscriber.
>
> **Hosting.** Microsoft Azure (Functions + Table Storage). Microsoft is the only third party in the
> path.
>
> **Limits.** (table from §3.6)

### 3.9 Repository layout (proposed)

```
websub-hub/
├── src/
│   ├── Hub.fsproj                 # F# isolated worker, net10.0
│   ├── Domain.fs                  # Topic, Subscription, Limits, HubError
│   ├── Security.fs                # SSRF guard, credential-URL filter, HMAC, Key Vault crypto
│   ├── FeedParsing.fs             # RSS/Atom/JSON Feed → entries, hub/self discovery, partial doc
│   ├── Storage.fs                 # Table Storage access, hashing of keys
│   ├── Subscribe.fs               # verify-intent flow
│   ├── Publish.fs                 # fetch → diff → fan-out
│   ├── Sweep.fs                   # timer: leases, rate-limit purge, stats roll
│   └── Functions.fs               # HTTP + timer trigger bindings
├── infra/                         # Bicep: Function App (Consumption), Storage, Key Vault, DNS
├── tests/                         # SSRF table, feed parsing fixtures, diff logic, HMAC vectors
├── public/index.html              # the `/` page: usage, limits, privacy statement
└── README.md
```

### 3.10 Verification plan

- **Phase 0**: Subscribe to `/notes/feed.xml` from a WebSub-aware reader (Miniflux/FreshRSS on the Pi
  works), publish a note, confirm arrival within ~30 s of the workflow ping. Confirm `304` on a repeat
  `curl -H 'If-None-Match: …'` of an unchanged feed.
- **Phase 1**: Same test against `hub.lqdev.me`. Dump the Table Storage account and confirm no
  plaintext callbacks or feed bodies. Confirm App Insights resource does not exist.
- **Phase 3**: Attempt to subscribe a topic on a domain already at 25; expect `429`/`4xx` with reason.
  Attempt `hub.topic=http://169.254.169.254/`; expect refusal with no outbound request (verify via
  Function logs **disabled** → verify via a canary: a topic on a host you control that records hits).

---

## 4. Open questions

1. **Repository name** for the hub (`websub-hub`, `lqdev-hub`, …).
2. **Go/no-go for Phase 3** is deferred until Phase 1 has run privately for a few weeks.
3. **Callback-domain caps**: should one subscriber domain (e.g. a large reader service) be able to hold
   unlimited subscriptions? Probably yes — they are the point — but worth a cap at 10,000 as a
   circuit breaker.
4. **Atom output**: the site emits RSS 2.0 + JSON Feed only. The hub should accept Atom from other
   publishers regardless.
5. **Should tag feeds ever get `rel=hub`?** Current answer: no. Revisit if a per-domain exemption for
   the operator's own domain becomes desirable.
