# AT Protocol Integration — Architecture Overview

**Tracking:** [#2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) ·
[ADR-0009](../adr/0009-at-protocol-integration.md)

**Status:**
- **Part A — publication node: LIVE.** lqdev.me is a verified `site.standard.publication` in the
  ATmosphere.
- **Part B — per-post documents: IMPLEMENTED, flag OFF.** All code is merged behind
  `AtProtoBuilder.useAtProtoSync = false`, so generated `_public/` output is byte-identical to the
  pre-integration baseline. No records are written until [activation](#activation-runbook).

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

## 2. Two tracks (only Track A is built)

| Track | Content type | Lexicon | Status |
|-------|--------------|---------|--------|
| **A** | Posts | `site.standard.document` (community Standard.site lexicon) | **Built (flag off)** |
| B | Notes, Media, Responses, RSVP | native `app.bsky.feed.post` + embeds | Deferred to future phases |

Standard.site fills the "long-form article" gap that AT Protocol itself lacks, and is
[surfaced in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline).

---

## 3. Build pipeline (pure, network-free)

Everything static-generatable happens in the normal `dotnet run`, as pure functions with no network
calls — exactly like `ActivityPubBuilder.buildActivities`.

```
Program.fs (roster)
  └─ if AtProtoBuilder.useAtProtoSync then
        AtProtoBuilder.buildAtProtoStaging posts "_public"
           └─ _public/api/data/atproto/documents/{rkey}.json   ← one wrapper file per Post
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
- **Collection-scoped.** Only ever touches `site.standard.document`. The ~14 hand-authored posts live
  in `app.bsky.feed.post`, a different collection this script never names.
- **Create / update only, never delete.** Records are upserted by their deterministic TID rkey.
- **Idempotent.** A record whose remote `sourceHash` matches the staged one is skipped.
- **Write-scope guard.** A remote record sharing a staged rkey but lacking our `sourceHash` marker is
  reported as **left-untouched** and never modified — we only ever manage records we created.
- **Fail-fast on corrupt staging.** A staging file with a missing/blank `sourceHash` aborts with a
  clear error + exit 1 (never a silent rewrite storm).
- **`validate: false`** on writes — guarantees the write regardless of whether the PDS can resolve the
  custom `site.standard.*` schema (the record carries `validationStatus: "unknown"`).
- **Secret hygiene.** The app password and session JWT are never printed.

Plan vocabulary: `create` (rkey absent) · `update` (ours, changed) · `unchanged` (skip) ·
`left-untouched` (present but not ours).

---

## 7. CI job

`sync_atproto_job` in `.github/workflows/publish-azure-static-web-apps.yml`, shaped like
`send_webmentions_job`:

- `needs: build_and_deploy_job`; downloads the `atproto-staging` artifact.
- **Gated** on `build_and_deploy_job.outputs.atproto_staged` — a `Check for AT Protocol staging` step
  sets it from the presence of staged files. Flag off → `false` → **the job is skipped entirely** (zero
  cost, no confusing no-op runs).
- Runs only on `push` to `main`.
- Runs **dry-run** (no `--commit`) until activated.

---

## 8. Activation runbook

The three gates, in order. Each is a deliberate, reviewable change:

1. **Enable staging:** set `AtProtoBuilder.useAtProtoSync = true`. The next main-branch build produces
   staging → the `sync_atproto_job` starts running in **dry-run** and prints the create/update plan.
   Review it in the Actions logs.
2. **Add the secret:** create repository secret `ATPROTO_APP_PASSWORD` (a dedicated Bluesky App
   Password — see [ADR-0009](../adr/0009-at-protocol-integration.md) and the one-time-setup notes).
3. **Go live:** append `--commit` to the sync step in the workflow. The next main-branch push writes
   records for real.

To roll back at any point, revert step 3 (dry-run) or step 1 (fully off, byte-identical baseline).

---

## 9. File map

| File | Role |
|------|------|
| `AtProtoBuilder.fs` | Core module: `Config`, `deriveTid`, `buildDocumentRecordJson`, `buildAtProtoStaging`, `documentLinkHead`, `useAtProtoSync` flag |
| `Program.fs` | Flag-gated call to `buildAtProtoStaging` |
| `Scripts/sync-atproto.fsx` | Post-build POSSE sync (dry-run by default) |
| `Scripts/create-atproto-publication.fsx` | One-time Part A publication bootstrap |
| `_src/.well-known/site.standard.publication` | Part A verification file (live) |
| `.github/workflows/publish-azure-static-web-apps.yml` | `sync_atproto_job` + `atproto_staged` gate |
| `test-scripts/test-atproto-tid.fsx` | 19 TID determinism/format assertions |
| `test-scripts/test-atproto-document-json.fsx` | 24 wire-contract assertions |
| `docs/adr/0009-at-protocol-integration.md` | Architecture Decision Record |

---

## 10. References

- Issue [#2574](https://github.com/lqdev/luisquintanilla.me/issues/2574) — implementation tracking
- [ADR-0009](../adr/0009-at-protocol-integration.md) — durable decisions
- [AT Protocol](https://atproto.com/) · [TID spec](https://atproto.com/specs/tid) ·
  [Record Key spec](https://atproto.com/specs/record-key)
- [Standard.site](https://standard.site/) ·
  [Standard.site in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline)
- [ActivityPub architecture](../activitypub/ARCHITECTURE-OVERVIEW.md) — the sibling static+dynamic
  hybrid this mirrors
