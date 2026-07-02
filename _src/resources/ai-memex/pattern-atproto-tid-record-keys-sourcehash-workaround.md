---
title: "Pattern: AT Protocol Record Keys Must Be TIDs — Use a sourceHash Extension Field Instead"
description: "AT Protocol Lexicons mandate specific record-key types per collection; deterministic content-hash IDs (the ActivityPub trick) can't become the rkey itself, but can be embedded as an extension field."
entry_type: pattern
published_date: "2026-07-02 15:00 -05:00"
last_updated_date: "2026-07-02 15:00 -05:00"
tags: "atproto, bluesky, lexicon, activitypub, architecture, patterns"
related_skill: ""
source_project: "lqdev-me"
---

## Discovery

While designing a hand-built AT Protocol integration for lqdev.me (mirroring the existing
`ActivityPubBuilder.fs` architecture), the plan was to reuse the exact same idempotency trick that
powers the ActivityPub implementation: `generateActivityId` (`ActivityPubBuilder.fs:415-417`) computes
a stable MD5 hash of `url + content` and uses it *directly* as the freely-chosen Activity URI. Since the
same input always produces the same ID, rebuilds are naturally idempotent with zero external state.

The plan was to do the same for AT Protocol: derive a deterministic `rkey` (the record-key segment of
an AT-URI, e.g. `at://did:plc:.../site.standard.document/{rkey}`) from each post's slug or content hash,
so the eventual AT-URI could be computed *before* ever calling the PDS — collapsing what would otherwise
be a two-phase "publish, then learn the URI, then render a verification link tag" flow into a single
pass.

## Root Cause

AT Protocol Lexicon schemas declare a required record-key **type** per collection
(`atproto.com/specs/record-key` documents four types: `tid`, `nsid`, `literal:<value>`, and `any` — only
`any` permits an arbitrary deterministic string).

Verified directly against the *live* Lexicon schema records (not just prose docs) via
`com.atproto.repo.getRecord`, and cross-checked against the official `app.bsky.feed.post` Lexicon JSON
on `github.com/bluesky-social/atproto`:

```powershell
# Resolve the DID that published the standard.site lexicons, then its PDS:
$doc = curl.exe -s "https://plc.directory/did:plc:re3ebnp5v7ffagz6rb6xfei4" | ConvertFrom-Json
$pds = ($doc.service | Where-Object { $_.type -eq "AtprotoPersonalDataServer" }).serviceEndpoint

# Query the lexicon schema record itself and check its declared key type:
$json = curl.exe -s "$pds/xrpc/com.atproto.repo.getRecord?repo=did:plc:re3ebnp5v7ffagz6rb6xfei4&collection=com.atproto.lexicon.schema&rkey=site.standard.document" | ConvertFrom-Json
$json.value.defs.main.key   # -> "tid"
```

Result: **`site.standard.document`, `site.standard.publication`, and `app.bsky.feed.post` all declare
`"key": "tid"`.** A TID (Timestamp Identifier) is a specific base32-sortable, clock-derived format —
not an arbitrary string. Unlike ActivityPub (where any dereferenceable URI can be an object's `id`),
these AT Protocol record types' identifiers are minted by the client/PDS at creation time and **cannot
be precomputed** from content. The AT-URI for a given piece of content is only known *after* the record
is created.

An earlier AI-generated web search result had claimed a different (also incorrect) restriction — that
App Passwords can't write custom Lexicon collections at all. Real-world evidence (two independent
production blogs running exactly this kind of sync in GitHub Actions CI with an App Password) refuted
that claim. The lesson generalizes: **verify protocol-level constraints against live schema data or
working production examples, not AI-generated summaries of documentation.**

## Solution

Standard.site's own docs state Lexicons are explicitly extensible: *"Lexicons are extendable.
Additional properties may be added to better suit the needs of a project."* Instead of forcing a content
hash into the `rkey` (not allowed), embed it as an **extra field on the record itself** — e.g.
`sourceHash` — preserving the exact same stable, content-addressed idempotency check, just one level
removed from the identifier:

```fsharp
type AtProtoDocument = {
    Type: string
    Site: string
    Title: string
    Path: string
    // ... other Standard.site document fields ...
    SourceHash: string   // extension field: MD5(url + content), same role as
                         // ActivityPubBuilder.fs's generateHash, but NOT the rkey
}
```

Sync algorithm (no precomputed AT-URI needed):
1. `com.atproto.repo.listRecords` to fetch all existing records in the collection for this repo.
2. Match each locally-staged item against the existing records — by a natural field if the Lexicon has
   one (Standard.site's `site.standard.document` has `path`, which is already unique per post), and by
   `sourceHash` to detect content changes.
3. `createRecord` for genuinely new items (server mints the TID); `putRecord` for changed items (reuse
   the TID already assigned); skip unchanged items.
4. Persist the resulting `sourceHash → AT-URI` map for the *next* build to use (e.g. to render a
   `<link rel="site.standard.document" href="at://...">` verification tag) — a one-build-cycle lag for
   brand-new content is acceptable and matches how real-world Standard.site publishing tools (Sequoia's
   `publish` → `inject` sequence) behave too.

For Lexicons with no natural matching field (e.g. `app.bsky.feed.post` has nothing like `path`), a
small committed state file (slug → AT-URI) is the fallback — same spirit as this repo's existing
`followers.json` being "regenerated from [an external] source of truth."

## Prevention

Before assuming an ID-generation pattern proven in one protocol (e.g. ActivityPub's "any URI can be an
object ID") transfers to a different protocol (AT Protocol), **check the target protocol's actual
identifier constraints directly**:
- Look up the record-key type in the Lexicon's own schema definition — either by resolving the
  publishing DID and querying `com.atproto.repo.getRecord` against its `com.atproto.lexicon.schema`
  collection (rkey = the lexicon's own NSID), or by reading the raw Lexicon JSON in the defining
  project's source repo (`"key": "tid" | "any" | "literal:<value>"` in the `record` definition's
  `defs.main`).
- If the required key type doesn't support deterministic/precomputed values (`tid` does not; `any`
  does), design around a **content-hash extension field for idempotency** instead of a
  content-hash-as-identifier scheme, and accept that the real identifier/URI is only knowable after a
  create/write call completes.
- Don't trust an AI-generated web search summary for protocol-level permission/constraint claims when a
  direct query against the live system (or a working production example) is available — it's fast and
  authoritative.
