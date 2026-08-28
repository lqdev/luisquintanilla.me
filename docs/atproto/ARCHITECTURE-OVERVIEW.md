# AT Protocol Integration — Architecture Overview

**Tracking:** [#2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) ·
[ADR-0009](../adr/0009-at-protocol-integration.md)

**Status:**
- **Part A — publication node: LIVE.** lqdev.me is a verified `site.standard.publication` in the
  ATmosphere.
- **Part B — per-post documents: LIVE.** `AtProtoBuilder.useAtProtoSync = true`; every push to `main`
  upserts one `site.standard.document` record per Post via `Scripts/sync-atproto.fsx --commit`.
- **Track B — Notes as native posts: LIVE.** `AtProtoBuilder.useAtProtoNotesSync = true`; Notes
  published on/after `notesActivationCutoff` (2026-07-13) POSSE to the Bluesky timeline as
  `app.bsky.feed.post` records (forward-only). To roll back, drop `--commit` (dry-run) or set the
  flag(s) to `false` (fully off, `_public/` byte-identical to the pre-integration baseline).
- **Part C — rich-media POSSE: IMAGE PHASE ACTIVE; GALLERY/VIDEO DORMANT.** Image, gallery, and video
  staging plus collection-safe materialization are implemented behind independent flags.
  `useAtProtoMediaImageSync = true` activates post-cutoff image manifests; gallery and video remain
  `false`, and no historical media is backfilled.
- **Response POSSE — RESHARE LINK-POST PHASE ACTIVE.** Ordinary-web reshares published on/after
  `2026-08-27 20:58 -05:00` become `app.bsky.feed.post` link posts. Public bookmarks, native reposts,
  and quote-posts remain implemented but dormant with independent flags and sentinel cutoffs.

This mirrors the site's existing [ActivityPub](../activitypub/ARCHITECTURE-OVERVIEW.md) approach: a
static hub (the F# build) with a thin dynamic spoke (one post-build `dotnet fsi` sync script run from
CI), no self-hosted infrastructure.

---

## 1. Identity & hosting — reused, nothing new

| Layer | Value | Notes |
|-------|-------|-------|
| Handle | `lqdev.me` | Domain-verified via DNS TXT `_atproto.lqdev.me` |
| DID | `did:plc:pme7qquljcdx6i4zyawoxypd` | Reused — **no second identity is minted** |
| PDS | `*.host.bsky.network` (currently `amanita.us-east.host.bsky.network`) | **Resolved dynamically** from the DID document at sync time — Bluesky migrates accounts between hosts, so the host is never hardcoded as the source of truth |
| Relay / AppView | Bluesky's own + the Standard.site indexer ecosystem | **Not self-hosted** — writing a record to the existing PDS is sufficient; discovery/timelines are handled downstream |

All four AT Protocol layers already exist and run on Bluesky's infrastructure. The only new dynamic
surface area is **one CI step that authenticates and writes records** — no Azure Function, no Key
Vault, no storage.

---

## 2. Two tracks

| Track | Content type | Lexicon | Status |
|-------|--------------|---------|--------|
| **A** | Posts | `site.standard.document` (community Standard.site lexicon) | **🟢 Live** (`useAtProtoSync = true`) |
| **B** | Notes | native `app.bsky.feed.post` | **🟢 Live** (`useAtProtoNotesSync = true`, forward-only from 2026-07-13) |
| **C** | Media | native `app.bsky.feed.post` + image/gallery/video embeds | **🟡 Implemented, dormant** (independent flags and cutoffs) |
| **D** | Bookmarks | `app.bsky.feed.post` + `app.bsky.embed.external` | **🟡 Implemented, dormant** (forward-only) |
| **E** | Ordinary-web reshares | `app.bsky.feed.post` + `app.bsky.embed.external` | **🟢 Active** (forward-only from 2026-08-27 20:58 -05:00) |
| **F** | ATProto reshares without commentary | `app.bsky.feed.repost` | **🟡 Implemented, dormant** (forward-only) |
| **G** | ATProto reshares with commentary | `app.bsky.feed.post` + `app.bsky.embed.record` | **🟡 Implemented, dormant** (forward-only) |
| — | Replies, stars/likes, RSVP | native `app.bsky.*` | Deferred to future phases |

Standard.site fills the "long-form article" gap that AT Protocol itself lacks, and is
[surfaced in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline).

---

## 3. Build pipeline (pure, network-free)

Everything static-generatable happens in the normal `dotnet run`, as pure functions with no network
calls — exactly like `ActivityPubBuilder.buildActivities`.

```
Program.fs (roster)
  ├─ if AtProtoBuilder.useAtProtoSync then
  │     AtProtoBuilder.buildAtProtoStaging posts "_public"
  │        └─ _public/api/data/atproto/documents/{rkey}.json   ← one wrapper file per Post
  ├─ if AtProtoBuilder.useAtProtoNotesSync then
  │     AtProtoBuilder.buildAtProtoNotesStaging notes "_public"
  │        └─ _public/api/data/atproto/posts/{rkey}.json       ← native Note
  └─ if a Part C media flag is enabled then
        AtProtoBuilder.buildAtProtoMediaStaging albums "_public"
           └─ _public/api/data/atproto/media/{images|galleries|videos}/{rkey}.json
  ├─ if bookmark mode is enabled then
  │     AtProtoBuilder.buildAtProtoBookmarksStaging bookmarks "_public"
  │        └─ _public/api/data/atproto/bookmarks/{rkey}.json
  └─ if a reshare mode is enabled then
        AtProtoBuilder.buildAtProtoResharesStaging responses "_public"
           ├─ _public/api/data/atproto/reshares/{rkey}.json       ← ordinary-web link posts
           ├─ _public/api/data/atproto/quotes/{rkey}.json         ← pending quote embeds
           └─ _public/api/data/atproto/reposts/{rkey}.json        ← pending repost subjects
```

**`AtProtoBuilder.fs`** is the core module (added to `PersonalSite.fsproj` after
`ActivityPubBuilder.fs`). Key functions:

- **`Config`** — public, non-secret identity + the Part A `publicationAtUri`. Publication
  name/description reuse `Constants.Site.title` / `Constants.Pwa.description` (single source of truth).
- **`deriveTid published slug`** — deterministic [TID](https://atproto.com/specs/tid) record key.
  AT Protocol mandates `"key": "tid"` for these lexicons, so the rkey **cannot** be a content hash.
  A TID is a 64-bit value (top bit 0, 53 bits of microseconds since epoch, 10-bit clock id) encoded as
  13 base32-sortable chars. We derive it from the post's **publish minute** + a slug-hash sub-minute
  offset, yielding spec-valid, **rebuild-stable** rkeys. This makes AT-URIs **precomputable at build
  time** (so the verification `<link>` tag can be rendered in the same build) and `putRecord` a
  stateless idempotent upsert.
- **`assertNoTidCollisions`** — a build-time invariant: if two posts derive the same rkey, the build
  **fails loudly** rather than silently overwriting.
- **`buildDocumentRecordJson post published slug`** — builds the `site.standard.document` record
  (see §4).
- **`documentLinkHead dateStr slug`** — the per-post verification `<link>` tag, flag-gated (returns
  `[]` when the flag is off or the date is unparseable, so pages stay byte-identical).
- **Response mapping** — `AtProtoResponseMapping` parses only strict `https://bsky.app/profile/.../post/...`
  and `at://did:.../app.bsky.feed.post/...` targets. It uses the Markdown AST's top-level blocks to
  distinguish authored commentary from quoted source material; no YAML field or historical content changes
  are required.

---

## 4. The `site.standard.document` record

Every field is **derived** from existing frontmatter + `Config` — no new markdown is authored.

```jsonc
// file: _public/api/data/atproto/documents/{rkey}.json
{
  "collection": "site.standard.document",
  "rkey": "<13-char deterministic TID>",
  "record": {
    "$type":       "site.standard.document",
    "site":        "at://did:plc:pme7qquljcdx6i4zyawoxypd/site.standard.publication/3mqs7sgylil2w",
    "title":       "…",                       // frontmatter title, ≤500 graphemes
    "path":        "/posts/{slug}/",           // via ContentTypes.urlPrefix — MUST match the real URL
    "description": "…",                        // frontmatter description; OMITTED when blank
    "textContent": "…",                        // body → plaintext (stripToPlainText); OMITTED when blank
    "tags":        ["dotnet","fsharp"],        // normalized via TagService; OMITTED when empty
    "publishedAt": "2026-01-31T22:14:00.000-05:00",  // ISO 8601 with offset
    "sourceHash":  "<md5>"                      // EXTENSION field — change detection + write scope
  }
}
```

Contract details (locked by `test-scripts/test-atproto-document-json.fsx`, 24 assertions):

- **Omit, don't null.** Optional fields that are empty are absent keys, never `"key": null`.
- **`path` is verification-critical.** It is derived from `ContentTypes.urlPrefix` — the single
  authority for permalink prefixes — so the staged `path` can never drift from the real published URL.
  Standard.site fetches `{publication.url}{path}` and looks for the matching `<link>`; any drift
  silently breaks verification.
- **Tag normalization** runs through `TagService.processTagName` (the site's single tag authority), so
  record tags match the taxonomy used by tag pages and RSS tag feeds (`.net`/`.net core` → `dotnet`,
  `c#` → `csharp`, spaces → hyphens). The `untagged` sentinel and blanks are dropped; duplicates are
  removed.
- **`sourceHash`** = `md5(canonicalUrl + path + "\u0000" + content)`. It is our **extension field**
  (lexicons are documented as extensible). It drives skip-if-unchanged and — critically — scopes writes
  to records we created (see §6).

---

## 5. Verification handshake (bidirectional)

1. **Publication → site** (Part A, live): `/.well-known/site.standard.publication` serves the
   publication AT-URI. Shipped via `_src/.well-known/` → `Builders/Assets.fs` copies it to `_public/`.
2. **Document → page** (Part B): each post's `<head>` carries
   `<link rel="site.standard.document" href="at://…/site.standard.document/{TID}" />`, emitted by
   `documentLinkHead`. Because `deriveTid` is deterministic, this tag is rendered in the **same build**
   that stages the record — no one-build-cycle lag.

A reader verifies a document by fetching `{publication.url}{path}` and confirming the `<link>` points
back at the record's AT-URI.

---

## 6. Sync script — the only dynamic step

**`Scripts/sync-atproto.fsx`** (a standalone `dotnet fsi` script, mirroring
`Scripts/send-webmentions.fsx` — not an Azure Function). It reads the staged records and upserts them
via `com.atproto.repo.putRecord`.

**Safety model (all validated read-only):**

- **Dry-run by default.** Without `--commit` the script only READS: resolves the PDS, lists existing
  records, prints the plan, and writes nothing. A live write requires **both** `--commit` **and** the
  `ATPROTO_APP_PASSWORD` secret.
- **Flag-off no-op.** With the flag off, no staging dir exists, so the script prints "nothing to sync"
  and exits 0 without touching the network.
- **Collection-scoped.** Each invocation targets one collection and its matching staging family:
  `site.standard.document` for Posts, or the shared `app.bsky.feed.post` collection for Notes and
  rich media. The latter uses the `sourceHash` write-scope guard so hand-authored posts remain
  untouched.
- **Create / update only, never delete.** Records are upserted by their deterministic TID rkey.
- **Idempotent.** A record whose remote `sourceHash` matches the staged one is skipped.
- **Write-scope guard.** A remote record sharing a staged rkey but lacking our `sourceHash` marker is
  reported as **left-untouched** and never modified — we only ever manage records we created.
- **Fail-fast on corrupt staging.** A staging file with a missing/blank `sourceHash` aborts with a
  clear error + exit 1 (never a silent rewrite storm).
- **Custom-lexicon validation handling.** `validate: false` is used only for
  `site.standard.*` writes, whose schema the PDS cannot resolve; native `app.bsky.*` writes omit it
  so the PDS validates the real schema.
- **Secret hygiene.** The app password and session JWT are never printed.
- **Media side-effect boundary.** Media URLs are not downloaded during planning or dry-run. On a
  write, each pending image is signature-checked, size-limited to 2,000,000 bytes, and dimension-read
  before `uploadBlob`; MP4 videos are signature-checked and limited to 300,000,000 bytes before the
  asynchronous `video.bsky.app` processing flow. All required blobs are prepared before the first
  `putRecord`, so a failed asset cannot leave a partial native post.
- **Native media shapes.** One to four images use `app.bsky.embed.images`; five to ten use
  `app.bsky.embed.gallery` with `items` tagged `app.bsky.embed.gallery#image`. Video is restricted
  to one MP4 per post and uses `app.bsky.embed.video`. The canonical media URL remains in post text
  with a UTF-8 byte-offset facet because a native post has one embed union.

Plan vocabulary: `create` (rkey absent) · `update` (ours, changed) · `unchanged` (skip) ·
`left-untouched` (present but not ours).

---

## 7. Response POSSE records

Response routing is derived from the existing `response_type`, `targeturl`, and Markdown source:

| Website intent | Target | Native record |
|---|---|---|
| Bookmark | Any ordinary URL | `app.bsky.feed.post` with an external card |
| Reshare | Ordinary web URL | `app.bsky.feed.post` with an external card |
| Reshare without authored commentary | Native ATProto post | `app.bsky.feed.repost` |
| Reshare with authored commentary | Native ATProto post | `app.bsky.feed.post` quote-post |

Link-post text preserves the hub as `Bookmarked: {title}` or `Shared: {title}`, followed by the
selected excerpt and the canonical `/bookmarks/{slug}/` or `/responses/{slug}/` URL. The canonical URL
has a UTF-8 byte-indexed link facet; the external card points at the target resource. Quote-post text
contains only authored commentary plus the canonical response URL because the original post is carried
by `embed.record`. Native reposts carry only the resolved `{uri, cid}` subject.

The static build writes pending quote/repost wrappers with a `targetRef` sidecar. The sync script
resolves handles to DIDs and batches `app.bsky.feed.getPosts` requests (25 URIs maximum), then fills
the strong reference before planning or authentication. Any unresolved target aborts that track before
the first write. Reposts do not use `sourceHash`; their immutable subject is the ownership guard.
All response rkeys are intent-qualified (`bookmark:`, `reshare-link:`, `quote:`, `repost:`), and one
collision assertion covers all native tracks before staging.

---

## 8. CI job

`sync_atproto_job` in `.github/workflows/publish-azure-static-web-apps.yml`, shaped like
`send_webmentions_job`:

- `needs: build_and_deploy_job`; downloads only the document, Note, media, and/or response artifacts that were
  produced.
- **Gated** on document, Note, media, and four response staging outputs — a `Check for AT Protocol
  staging` step sets them from the presence of staged files. All flags off → all outputs `false` →
  **the job is skipped entirely** (zero cost, no confusing no-op runs).
- Runs only on `push` to `main`.
- Runs **live** (`--commit`) for both Track A (`site.standard.document`) and Track B
  (`app.bsky.feed.post`) staging.
- Track C uses the same `app.bsky.feed.post` collection and write-scope guard, but separate
  `--media-kind images` and `--media-kind videos` invocations. Media artifacts are downloaded only
  when their corresponding staging gate is true; the image flag is active while gallery and video
  remain dormant.
- Response artifacts use independent bookmark, reshare-link, quote, and repost downloads and sync
  invocations. Link and quote posts use the shared `app.bsky.feed.post` collection; reposts use
  `app.bsky.feed.repost`. The reshare-link mode is active from its real cutoff; the other response
  modes remain dormant until their source flag and cutoff are activated together.

---

## 9. Activation runbook — Tracks A/B + image + reshare-link phases ✅ complete; other response tracks 🟡 dormant

The document, Note, and image tracks run **live on every push to `main`**. Gallery and video remain
deliberately dormant until their independent rollouts are approved:

1. ✅ **Staging enabled:** `AtProtoBuilder.useAtProtoSync = true` (Track A) and
   `useAtProtoNotesSync = true` (Track B) → each main-branch build produces staging and the
   `sync_atproto_job` runs.
2. ✅ **Secret added:** repository secret `ATPROTO_APP_PASSWORD` (a dedicated Bluesky App Password —
   see [ADR-0009](../adr/0009-at-protocol-integration.md) and the one-time-setup notes).
3. ✅ **Live:** the sync steps run with `--commit`, so each main-branch push writes records for real.

4. ✅ **Image activation:** `useAtProtoMediaImageSync = true` with the 2026-08-01 forward-only cutoff;
   the image sync runs collection-scoped with `--commit` and source-hash idempotency.
5. 🟡 **Gallery/video activation:** enable each independent flag only after its own rollout review and
   verify the PDS/AppView record before expanding the phase.

Response activation is intentionally forward-only and ordered to limit risk:

1. 🟡 Set `bookmarkPostsActivationCutoff` to the desired instant and flip
   `useAtProtoBookmarkPostsSync = true`. Run a dry-run, then one live record with `--commit --limit 1`.
2. ✅ `useAtProtoResharePostsSync = true` with `resharePostsActivationCutoff` set to
   `2026-08-27 20:58 -05:00`; ordinary-web reshare link posts run live and forward-only.
3. 🟡 Repeat for `repostsActivationCutoff` / `useAtProtoRepostsSync`.
4. 🟡 Repeat for `quotePostsActivationCutoff` / `useAtProtoQuotePostsSync`.

Verify the PDS record and Bluesky rendering after each capped run before removing `--limit`. The
builder fails closed if a response flag is enabled while its cutoff is still `DateTimeOffset.MaxValue`.
To roll back any track, drop `--commit` (dry-run) or set its staging flag to `false`; this phase never
deletes or unreposts records. Reclassifying an already-published reshare can leave the old native
record orphaned until deletion reconciliation is implemented.

---

## 10. File map

| File | Role |
|------|------|
| `AtProtoBuilder.fs` | Core module: `Config`, deterministic TIDs, document/Note/media/response record builders, flags and cutoffs |
| `AtProtoResponseMapping.fs` | Pure response target parsing and Markdown commentary classification |
| `Services\AtProtoMediaValidation.fs` | Network-free image signature/dimension and MP4 size/container validation |
| `Program.fs` | Flag-gated calls for document, Note, rich-media, and response staging plus shared native rkey checks |
| `Scripts/sync-atproto.fsx` | Post-build POSSE sync, target strong-ref resolution, media validation/materialization, and video polling |
| `Scripts/create-atproto-publication.fsx` | One-time Part A publication bootstrap |
| `_src/.well-known/site.standard.publication` | Part A verification file (live) |
| `.github/workflows/publish-azure-static-web-apps.yml` | `sync_atproto_job` + document/media/response staging gates |
| `test-scripts/test-atproto-tid.fsx` | 19 TID determinism/format assertions |
| `test-scripts/test-atproto-document-json.fsx` | 24 wire-contract assertions |
| `test-scripts/test-atproto-media.fsx` | 30 rich-media contract assertions |
| `test-scripts/test-atproto-response-mapping.fsx` | 46 response target, routing, text, facets, tags, and record-shape assertions |
| `docs/adr/0009-at-protocol-integration.md` | Architecture Decision Record |

---

## 11. References

- Issue [#2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) — implementation tracking
- [ADR-0009](../adr/0009-at-protocol-integration.md) — durable decisions
- [AT Protocol](https://atproto.com/) · [TID spec](https://atproto.com/specs/tid) ·
  [Record Key spec](https://atproto.com/specs/record-key)
- [Standard.site](https://standard.site/) ·
  [Standard.site in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline)
- [ActivityPub architecture](../activitypub/ARCHITECTURE-OVERVIEW.md) — the sibling static+dynamic
  hybrid this mirrors
