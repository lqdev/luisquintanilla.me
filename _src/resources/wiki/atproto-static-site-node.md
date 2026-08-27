---
post_type: "wiki"
title: "Make Your Static Site an AT Protocol Node (Standard.site + POSSE to Bluesky)"
last_updated_date: "07/17/2026 13:34 -05:00"
tags: atproto, bluesky, standardsite, indieweb, posse, staticsite, decentralization, fediverse, activitypub, socialweb
---

## Overview

This guide shows how to turn **any static site** into a first-class node in the
[ATmosphere](https://atproto.com/) — the network built on the AT Protocol (the open protocol underneath
[Bluesky](https://bsky.app/)) — using the community [Standard.site](https://standard.site/) Lexicons,
and how to **POSSE** (Publish on your Own Site, Syndicate Elsewhere) your content so it shows up in the
Bluesky timeline.

The best part: you do **not** need to run any servers. No PDS, no relay, no AppView. Roughly 99% of this
is static files your generator already produces; the only dynamic piece is **one small script that
authenticates once and writes records**, run from your existing CI (e.g. GitHub Actions). It mirrors the
same "static hub, thin dynamic spoke" shape that an [ActivityPub](https://www.w3.org/TR/activitypub/)
integration uses.

The commands below are plain `curl` + [`jq`](https://jqlang.github.io/jq/) (XRPC over HTTPS), so they work
regardless of your stack — Hugo, Jekyll, Eleventy, Astro, Zola, a hand-rolled generator, anything. Where
it helps, a callout shows how a concrete F# static-site generator implements the same idea:

> 🔧 **Reference (F# static site):** callouts like this point at a real, hand-built F# implementation of
> everything here. They're illustrative — you don't need F# to follow the guide.

**What's in this guide:**

1. Why do this? The hub-and-spoke idea
2. AT Protocol in five minutes
3. The big decisions (identity, hosting, lexicons, record keys)
4. What you'll need
5. Step 1 — Verify your identity (handle → DID → PDS)
6. Step 2 — Become a publication node
7. Step 3 — POSSE long-form posts as documents (Track A)
8. Step 4 — POSSE short-form as native Bluesky posts (Track B)
9. Step 4c — POSSE rich media with native embeds (Part C)
10. Step 5 — Automate it in CI
11. Verify & troubleshoot
12. Safety & idempotency checklist
13. Reference implementation & further reading
14. Appendix A — Placeholders
15. Appendix B — Example records (JSON)

> ℹ️ Every command uses **placeholders** (`yourdomain.com`, `did:plc:xxxx…`, `APP_PASSWORD`, …). Substitute
> your own values. See **Appendix A** at the end for the full list.

## Why do this? The hub-and-spoke idea

The [IndieWeb](https://indieweb.org/) principle is simple: **own your content**. Your website is the
canonical home for everything you publish; social networks are just distribution channels. The practice
that implements this is [**POSSE**](https://indieweb.org/POSSE) — *Publish on your Own Site, Syndicate
Elsewhere*. You write once on your site, then push copies out to the networks where your audience is.

Picture your site as a **hub** and each protocol as a **spoke**:

```
                    ┌──────────────┐
      Fediverse ◀───┤              ├───▶ AT Protocol / Bluesky
     (ActivityPub)  │  YOUR SITE   │      (Standard.site + app.bsky.*)
                    │   (the hub)  │
          RSS ◀─────┤              ├───▶ Webmentions
                    └──────────────┘
```

You may already federate over ActivityPub (the Mastodon/Fediverse protocol). The **AT Protocol** is a
second, distinct decentralized social protocol. Adding it as another spoke means your posts can be
discovered, followed, and surfaced inside Bluesky — while your site stays the source of truth.

Two things make this newly worthwhile for **static** sites:

- **Standard.site** is a community Lexicon that models a website as a *publication* with *documents*
  (articles). AT Protocol's own core vocabulary is tuned for short posts; Standard.site fills the
  long-form gap.
- Bluesky now **surfaces Standard.site publications in the timeline** — see the announcement,
  [Standard.site in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline). Links
  to your articles get an enhanced render, and your publication is discoverable.

The constraint we hold throughout: **static-first, no new infrastructure.** If it can be a build
artifact, it is one; only genuinely dynamic work (one authenticated write) runs outside the build.

## AT Protocol in five minutes

Five concepts are enough to follow this guide:

- **DID** — your permanent, portable identifier, e.g. `did:plc:xxxx…`. It survives even if you change domains or hosts.
- **Handle** — a human-friendly name that resolves to your DID. **It can be your own domain** (`yourdomain.com`), verified via a DNS record.
- **PDS** (Personal Data Server) — the server that stores your *repository*, a signed collection of records. Bluesky hosts one for you.
- **Lexicon** — a schema defining a record type (its `$type`, fields, and rules). `app.bsky.feed.post` and `site.standard.document` are Lexicons.
- **Relay / AppView** — aggregation infrastructure that crawls PDSes and builds timelines, search, and notifications. Bluesky runs these; so do third-party indexers.

**"Being a node"** just means: your PDS holds records, and the relay/AppView layer picks them up and shows
them to people. When you already have a Bluesky account, **all of this already exists and runs** — you're
simply going to write some additional record types into the repository you already own.

The two Standard.site Lexicons you'll use:

- **`site.standard.publication`** — one record describing your whole site (its `url` and `name`). You create
  this **once**.
- **`site.standard.document`** — one record per article, carrying its title, path, description, tags, and
  publish date.

(There are also `app.bsky.feed.post` for native short posts, and Standard.site's `graph.subscription` /
`graph.recommend` for follows/endorsements. We use `document`, `publication`, and `feed.post` here.)

## The big decisions

Before any commands, four questions shape the whole design. Here are the answers, with the reasoning.

### 1. Reuse your existing identity, mint a new one, or self-host a PDS?

If you already post on Bluesky under your domain, **reuse that identity.** Your domain handle *is* your
site's presence — there's no need for a second account.

- **Reuse your existing account** — *recommended*, cost: none. You already have a Bluesky handle at your domain. Site records live in the same repo, in their own collections; they don't clutter your personal feed unless the record type is itself a timeline post.
- **Mint a separate AT Protocol identity for the site** — cost: an extra account + handle setup. Choose this if you want the site's records fully separate from your personal posting identity.
- **Self-host a PDS** — cost: ongoing server ops. Choose this for maximum sovereignty if you're willing to run infrastructure. Not required for any of this.

> 🔧 **Reference (F# static site):** the reference site reuses its existing `did:plc:…` / `yourdomain.com`
> account. Custom `site.standard.*` records and native `app.bsky.*` posts are separate collections in the
> **same** repository.

### 2. How much infrastructure do you have to host? (Almost none.)

Because your account already lives on a hosted PDS, **all four AT Protocol layers already run.** The only
new dynamic surface area is a single CI step that authenticates and writes records. Here's who hosts what:

- Identity (DID + handle) — **no**, it already exists.
- PDS (stores your records) — **no**, Bluesky hosts it.
- Relay + AppView (timelines, search, discovery) — **no**, Bluesky plus third-party indexers.
- The write step (POSSE records to the PDS) — **yes**, one small script in CI. This is the only piece you run.

This is actually *simpler* than an ActivityPub integration, which has to build an inbox, a followers store,
and a delivery queue (because ActivityPub has no central relay). AT Protocol has no equivalent to build:
writing a record is enough; discovery happens downstream automatically.

### 3. Can you write custom (Standard.site) lexicons to a hosted PDS?

**Yes.** A Bluesky-hosted PDS accepts records of Lexicons it doesn't recognize, as long as you pass
`validate: false` on the write. The record is stored with `validationStatus: "unknown"` — which is exactly
what you want. (This is verifiable against real production sites that publish Standard.site records from a
Bluesky-hosted PDS.)

> ⚠️ **Gotcha:** Don't trust AI-summarized claims that "Bluesky rejects custom lexicons." That's false —
> confirm protocol behavior against live records or a working reference site, not prose.

### 4. Record keys and idempotency (the one fiddly protocol rule)

Every record's address is an **AT-URI**: `at://{did}/{collection}/{rkey}`. The `rkey` (record key) is the
last segment. Standard.site's `document`/`publication` and Bluesky's `feed.post` Lexicons all mandate that
the rkey be a **TID** (Timestamp Identifier) — a specific 64-bit, clock-derived, base32-sortable format —
**not** an arbitrary string.

That matters because a common idempotency trick ("hash the content, use the hash as the ID") **can't** be
used for the rkey. Two clean ways to handle it:

- **Let the PDS mint the TID** (simplest). Omit the `rkey` when you `createRecord`; the server assigns one.
  You learn the AT-URI *after* creation, so on later syncs you match your local posts to existing records by
  a stable field — Standard.site documents have a unique `path` — to decide create/update/skip.
- **Derive a deterministic TID** yourself from the post's publish time + a slug hash (advanced). A TID is
  just a 64-bit integer (top bit 0, 53 bits of microseconds since the epoch, 10-bit clock id) encoded as 13
  base32-sortable chars — nothing forces the timestamp to be "now," so you can compute a spec-valid,
  rebuild-stable rkey. This makes AT-URIs **precomputable at build time** (so a verification `<link>` tag can
  be rendered in the *same* build) and turns each write into a stateless idempotent `putRecord`.

Either way, to detect *content changes* you add your **own extension field** — call it `sourceHash` — set to
a hash of the content. Lexicons are explicitly extensible, so an extra field is allowed. `sourceHash` gives
you the content-addressed idempotency you wanted, one level removed from the identifier, and (as you'll see)
doubles as a safety marker.

> 🔧 **Reference (F# static site):** the reference implementation derives a deterministic TID
> (`deriveTid published slug`), asserts no two posts collide at build time, and carries
> `sourceHash = md5(url + path + "\0" + content)` as the extension field. See the
> [TID spec](https://atproto.com/specs/tid) and [Record Key spec](https://atproto.com/specs/record-key).

## What you'll need

- **A domain** you control.
- **A Bluesky account whose handle is your domain.** In the Bluesky app: *Settings → Account → Handle →
  I have my own domain*, then add the DNS `_atproto` TXT record it gives you. (Alternatively, self-host a
  PDS — out of scope here.)
- **A Bluesky App Password** (see Step 2). Never use your main password in scripts.
- **Any static site generator**, and a **CI system** (this guide uses GitHub Actions as the worked example).
- **`curl` and `jq`** locally for the one-time setup and for testing.

## Step 1 — Verify your identity (handle → DID → PDS)

Everything keys off your DID and your PDS host. Resolve both up front. **Never hardcode the PDS host** — 
accounts migrate between hosts, so always resolve it from the DID document at run time.

```bash
# --- Fill in your handle ---
HANDLE="yourdomain.com"

# 1) Resolve your handle to a DID (public, no auth)
DID=$(curl -s "https://public.api.bsky.app/xrpc/com.atproto.identity.resolveHandle?handle=$HANDLE" | jq -r .did)
echo "$DID"
# -> did:plc:xxxxxxxxxxxxxxxxxxxxxxxx

# 2) Find your PDS host from the DID document (public, no auth)
PDS=$(curl -s "https://plc.directory/$DID" \
      | jq -r '.service[] | select(.type=="AtprotoPersonalDataServer") | .serviceEndpoint')
echo "$PDS"
# -> https://xxxxxxxx.us-east.host.bsky.network
```

You now have `DID` and `PDS`. Both are public — no secret needed for reads.

## Step 2 — Become a publication node

This is the one-time setup that makes your site a verified Standard.site *publication*. Two parts: create
the record (an authenticated write) and publish a static verification file.

### 2a. Mint a dedicated App Password

In the Bluesky app: *Settings → Privacy and Security → App Passwords → Add App Password*. Name it something
like `site-atproto-sync`, leave DM access unchecked, and copy the `xxxx-xxxx-xxxx-xxxx` value (shown once).
It's independently revocable and can't delete your account — safe for automation.

```bash
APP_PASSWORD="xxxx-xxxx-xxxx-xxxx"
```

### 2b. Create the `site.standard.publication` record — EXACTLY ONCE

> ⚠️ **Gotcha:** there is no automatic de-duplication. Running `createRecord` twice makes **two**
> publications. Do this once; to change metadata later, use `putRecord` with the **same** rkey.

First authenticate to get a session token, then (optionally) confirm no publication exists yet, then create:

```bash
# Authenticate (fresh session; the token is short-lived)
ACCESS_JWT=$(jq -n --arg id "$HANDLE" --arg pw "$APP_PASSWORD" '{identifier:$id, password:$pw}' \
  | curl -s -X POST "$PDS/xrpc/com.atproto.server.createSession" \
      -H 'Content-Type: application/json' --data-binary @- | jq -r .accessJwt)

# Safety: confirm zero existing publications (should print an empty list)
curl -s "$PDS/xrpc/com.atproto.repo.listRecords?repo=$DID&collection=site.standard.publication" | jq '.records'

# Create the publication (RUN ONCE). jq builds the JSON so escaping/quoting is correct.
jq -n --arg did "$DID" --arg url "https://$HANDLE" \
      --arg name "Your Site Name" \
      --arg desc "A short description of your site." '
  {
    repo: $did,
    collection: "site.standard.publication",
    validate: false,
    record: {
      "$type": "site.standard.publication",
      url: $url,
      name: $name,
      description: $desc,
      preferences: { showInDiscover: true }
    }
  }' \
| curl -s -X POST "$PDS/xrpc/com.atproto.repo.createRecord" \
    -H "Authorization: Bearer $ACCESS_JWT" -H 'Content-Type: application/json' \
    --data-binary @- | jq -r .uri
# -> at://did:plc:xxxxxxxxxxxxxxxxxxxxxxxx/site.standard.publication/<PUB_RKEY>
```

**Copy that AT-URI.** It's your publication's address; every `site.standard.document` will reference it, and
your verification file will contain it.

> 🔧 **Reference (F# static site):** this one-time create runs as an idempotent, manually-dispatched CI job
> (`workflow_dispatch`) so the App Password stays in a repository secret and only the *public* AT-URI is
> printed to the log. The script lists-then-creates, so re-running is always safe. (Running one-time
> provisioning in CI also sidesteps the fact that CI secrets are write-only — you can't read them back to run
> locally.)

### 2c. Publish the verification file (byte-exact)

Standard.site verifies domain ownership via a file at `/.well-known/site.standard.publication` whose body is
**exactly** the publication AT-URI. Have your generator emit it as a static asset.

```bash
# Write the AT-URI with NO trailing newline (printf, not echo)
mkdir -p .well-known
printf '%s' 'at://did:plc:xxxxxxxxxxxxxxxxxxxxxxxx/site.standard.publication/<PUB_RKEY>' \
  > .well-known/site.standard.publication
```

> ⚠️ **Gotcha (byte format):** match a working reference **byte-for-byte** — the file should be the bare
> AT-URI, **no trailing newline, no UTF-8 BOM**. `echo` appends a newline; use `printf '%s'`. The
> `Content-Type` doesn't matter (verifiers read the body as text), but an extra byte can trip a naïve
> verifier. Confirm the emitted bytes, e.g.:
>
> ```bash
> wc -c .well-known/site.standard.publication   # byte count == length of the AT-URI, nothing extra
> tail -c 1 .well-known/site.standard.publication | xxd   # last byte is the URI's last char, NOT 0a (newline)
> ```

> 🔧 **Reference (F# static site):** the file lives in the generator's `_src/.well-known/` and the asset step
> copies `.well-known/` verbatim into the published output — no special handling.

Deploy, then verify it (see **Verify & troubleshoot** below) — check the **body**, not just the status
code, because SPA/static hosts often rewrite missing paths to `index.html` with a `200`.

✅ After Step 2 your site is a verified publication node. No articles are published yet — that's Track A.

## Step 3 — POSSE long-form posts as documents (Track A)

Now publish one `site.standard.document` per article. These carry no length limits and are safe to
**backfill in full** (they're not timeline posts, so bulk-creating them won't flood anyone).

### 3a. The document record contract

Every field is **derived** from data your site already has (frontmatter + the article body). Shape:

```jsonc
{
  "$type":       "site.standard.document",
  "site":        "at://did:plc:xxxx…/site.standard.publication/<PUB_RKEY>", // from Step 2
  "title":       "My Article Title",              // ≤ 500 graphemes
  "path":        "/posts/my-article/",            // MUST equal the real URL path (see gotcha)
  "description": "One-line summary.",             // omit the key entirely if blank
  "textContent": "Article body as plain text…",   // strip HTML/markdown; omit if blank
  "tags":        ["indieweb", "atproto"],         // normalized; omit if empty
  "publishedAt": "2026-01-31T22:14:00.000-05:00", // ISO 8601 with offset
  "sourceHash":  "<hash-of-content>"              // YOUR extension field: change detection + write scope
}
```

Rules that matter:

- **Omit, don't null.** Empty optional fields are *absent keys*, never `"key": null`.
- **`path` is verification-critical.** Standard.site fetches `{publication.url}{path}` and looks for a
  matching `<link>` tag. If `path` drifts from your real URL (trailing slash, prefix, casing), verification
  silently fails. Derive it from the same source your permalinks use.
- **Normalize tags** the same way your site's tag pages do, so records match your taxonomy.
- **`sourceHash`** is your own field (e.g. `md5(url + path + "\0" + content)`). It powers skip-if-unchanged
  and marks records as *yours* (crucial in Track B).

### 3b. Write a document

Using the server-minted-TID approach (simplest), omit `rkey` and let the PDS assign it:

```bash
jq -n --arg did "$DID" --arg site "at://$DID/site.standard.publication/<PUB_RKEY>" \
      --arg title "My Article Title" --arg path "/posts/my-article/" \
      --arg desc "One-line summary." --arg text "Article body as plain text…" \
      --arg published "2026-01-31T22:14:00.000-05:00" --arg hash "<hash-of-content>" '
  {
    repo: $did, collection: "site.standard.document", validate: false,
    record: {
      "$type": "site.standard.document",
      site: $site, title: $title, path: $path,
      description: $desc, textContent: $text,
      tags: ["indieweb","atproto"],
      publishedAt: $published, sourceHash: $hash
    }
  }' \
| curl -s -X POST "$PDS/xrpc/com.atproto.repo.createRecord" \
    -H "Authorization: Bearer $ACCESS_JWT" -H 'Content-Type: application/json' \
    --data-binary @- | jq -r .uri
# -> at://did:plc:xxxx…/site.standard.document/<TID>
```

To make this **idempotent** across rebuilds, don't blindly create. First list existing records, then decide:

```bash
# Fetch all existing documents once
curl -s "$PDS/xrpc/com.atproto.repo.listRecords?repo=$DID&collection=site.standard.document&limit=100" \
  | jq '.records[] | {uri, path: .value.path, sourceHash: .value.sourceHash}'
```

For each local post: if no record has its `path` → **create**; if one exists but its `sourceHash` differs →
**`putRecord`** at that record's rkey; if `sourceHash` matches → **skip**. (With deterministic TIDs you skip
the matching entirely and always `putRecord` at the computed rkey — same result, less bookkeeping.)

### 3c. Emit the verification `<link>` tag

Each article's HTML `<head>` must carry a back-reference to its record's AT-URI:

```html
<link rel="site.standard.document" href="at://did:plc:xxxx…/site.standard.document/<TID>" />
```

A reader/indexer verifies a document by fetching `{publication.url}{path}` and confirming this `<link>`
points back at the record. If you derive TIDs deterministically, you can render this tag in the same build
that stages the record. If you let the PDS mint the TID, render it on the next build (a one-build-cycle lag
for brand-new posts is fine).

> 🔧 **Reference (F# static site):** `documentLinkHead` emits this tag per post, gated behind the same flag
> as the sync so pages stay byte-identical until you activate. Deterministic TIDs mean no lag.

## Step 4 — POSSE short-form as native Bluesky posts (Track B)

Track A documents render richly **in clients that understand Standard.site**. For short notes you may also
want them to appear as **ordinary posts in the Bluesky timeline for everyone** — that's a native
`app.bsky.feed.post` record, optionally with a link card back to your canonical URL.

### 4a. The native post record

```jsonc
{
  "$type": "app.bsky.feed.post",
  "text": "A short note (≤ 300 graphemes). Longer ones get an excerpt + a link card.",
  "createdAt": "2026-07-13T09:00:00.000-05:00",     // your true publish time
  "langs": ["en"],
  "embed": {
    "$type": "app.bsky.embed.external",
    "external": {
      "uri": "https://yourdomain.com/notes/my-note/", // canonical URL on YOUR site
      "title": "My Note",
      "description": "One-line summary."
    }
  },
  "sourceHash": "<hash-of-content>"                    // extension field — marks this as OURS
}
```

Write it exactly like a document, but `--collection app.bsky.feed.post` and **omit `validate`** (or set it
`true`) — this is a *known* Lexicon, so let the PDS validate it and reject malformed posts.

### 4b. Track B's three gotchas

> ⚠️ **300-grapheme cap.** `app.bsky.feed.post` hard-limits text to 300 graphemes / 3,000 UTF-8 bytes. For
> anything longer, post a truncated excerpt and let the `embed.external` card carry the link to the full
> note. (Putting the URL in the card also avoids computing UTF-8 *byte-offset* "facets" for inline links.)

> ⚠️ **Bluesky sorts the timeline by ingest time (`indexedAt`), not `createdAt`.** If you backfill your
> whole history at once, followers see a flood of "new" posts in reverse order. **Go forward-only:** pick an
> activation cutoff date and only POSSE notes published on/after it. `createdAt` still carries the true date,
> so each post is *dated* correctly even though it *ingests* now.

> ⚠️ **You share this collection with your hand-authored posts.** Because you reused your identity,
> `app.bsky.feed.post` already contains posts you wrote by hand in the app. Your automation must **only ever
> touch records it created** — identified by the presence of your `sourceHash` field. Never blind-update or
> delete across the collection. This "write-scope guard" is the single most important safety property here.

> 🔧 **Reference (F# static site):** Track B stages only notes on/after a `notesActivationCutoff`, reuses the
> same `deriveTid`, and the sync's plan reports any record lacking our `sourceHash` as *left-untouched* — so
> hand-authored posts are structurally impossible to modify.

### 4c. Rich-media POSSE with native embeds (Part C)

Rich media uses the same `app.bsky.feed.post` collection, but the embed union carries the uploaded
asset. Keep the canonical media URL in the post text and add a link facet; a native post cannot combine
an external card with an image, gallery, or video embed.

- One to four supported images use `app.bsky.embed.images`.
- Five to ten images use `app.bsky.embed.gallery`; the field is `items`, and each item is tagged
  `app.bsky.embed.gallery#image`.
- A media post may contain one MP4 video, uploaded through `video.bsky.app` and materialized as
  `app.bsky.embed.video`. Mixed image/video and multiple-video posts must be rejected.

The static build should stage only source URLs, MIME types, accessibility text, and dimensions. The
post-build sync downloads media only after the dry-run, authentication, and write-scope gates. It must
validate file signatures, enforce the 2,000,000-byte image and 300,000,000-byte video limits, derive
exact image dimensions, and finish every upload before the first `putRecord`. For a cautious rollout,
use independent image/gallery and video flags with separate forward-only activation cutoffs; do not
backfill historical media.

> 🔧 **Reference (F# static site):** Part C stages manifests under
> `_public/api/data/atproto/media/images`, `galleries`, and `videos`. The CI job passes
> `--media-kind images` or `--media-kind videos` to `Scripts/sync-atproto.fsx`; the flags remain off
> by default, so the current build produces no media manifests.

## Step 5 — Automate it in CI

Wrap the write logic in a small script and run it after your normal build, from CI. The essential pattern:

1. Read the records your build staged (as JSON files).
2. Resolve the PDS from the DID document (don't hardcode it).
3. `createSession` with the App Password → JWT.
4. `listRecords` for the target collection.
5. Compute a plan: **create** (absent) · **update** (yours, changed) · **unchanged** (skip) ·
   **left-untouched** (present but not yours — never modify).
6. Apply via `createRecord` / `putRecord`. Never delete.

Make it **safe by default**:

- **Dry-run unless a `--commit` flag is passed.** Reads (resolve, list, print plan) need no secret; only the
  write path needs `--commit` *and* the App Password. This lets CI print the plan harmlessly until you're
  ready.
- **Gate the job** so it's skipped entirely until you opt in (e.g. only when staging files exist, only on
  pushes to your main branch).
- **Cap the first live run** with a `--limit N` so your very first write is a small, verifiable batch; then
  re-run without the cap to backfill (already-written records skip via `sourceHash`).

A representative GitHub Actions job:

```yaml
sync-atproto:
  needs: build_and_deploy          # run after the site is built/deployed
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4      # or whatever runtime your sync script needs
      with: { dotnet-version: '10.0.x' }
    - name: Sync records to the PDS
      env:
        ATPROTO_APP_PASSWORD: ${{ secrets.ATPROTO_APP_PASSWORD }}
      run: |
        # Dry-run until you're ready; add --commit to go live.
        your-sync-command --commit
```

Store the App Password as the repository secret `ATPROTO_APP_PASSWORD` (*Settings → Secrets and variables →
Actions*).

> 🔧 **Reference (F# static site):** the sync is a standalone `dotnet fsi` script (BCL-only, no build needed),
> shaped like the repo's existing Webmentions sender. Its CLI: dry-run by default; `--commit` to write;
> `--limit N` for a capped first run; `--collection` + `--dir` to target Track A vs Track B; and
> `--media-kind images|videos` to select a Part C phase. A per-file cross-check aborts *before any
> network I/O* if a staged record's collection doesn't match `--collection`.

**Activation runbook** — three deliberate, reversible gates, in order:

1. **Turn on staging** (your build starts emitting records) → the CI job runs **dry-run** and prints the
   plan. Review it.
2. **Add the `ATPROTO_APP_PASSWORD` secret.**
3. **Add `--commit`** (start with `--limit N`). The next push writes for real. Verify, then remove the cap.

To roll back, drop `--commit` (dry-run) or turn staging back off (fully inert).

## Verify & troubleshoot

**Confirm the publication verification file (check the BODY, not the status):**

```bash
curl -s "https://yourdomain.com/.well-known/site.standard.publication"; echo
# Must print exactly your publication AT-URI — NOT your homepage HTML.
```

> ⚠️ **SPA-fallback trap.** Static hosts with a navigation fallback rewrite unknown paths to `index.html`
> with a `200`. So a not-yet-deployed endpoint can return `200` **plus your whole homepage** — a naïve "is it
> 200?" check passes on garbage. Assert `Content-Type` is *not* `text/html` **and** the body equals the
> AT-URI. A real file at the path takes precedence over the fallback once deployed.

**Confirm the bidirectional handshake** (record → your domain, and your domain → record):

```bash
PUB_RKEY="<PUB_RKEY>"
SITE=$(curl -s "https://yourdomain.com/.well-known/site.standard.publication")
REC=$(curl -s "$PDS/xrpc/com.atproto.repo.getRecord?repo=$DID&collection=site.standard.publication&rkey=$PUB_RKEY")
echo "$REC" | jq -r '.value.url'   # -> https://yourdomain.com   (record -> site)
echo "$SITE"                        # -> at://…/<PUB_RKEY>        (site -> record)
```

**List what you've published:**

```bash
curl -s "$PDS/xrpc/com.atproto.repo.listRecords?repo=$DID&collection=site.standard.document&limit=100" | jq '.records | length'
```

**See a native post live on the timeline** (Track B): open `https://bsky.app/profile/yourdomain.com`, or query
the public AppView with `app.bsky.feed.getPostThread`.

**Common issues:**

- **`.well-known` returns your homepage HTML** — SPA fallback; the file isn't deployed yet, or your generator isn't emitting it. Check the body and `Content-Type`.
- **Verification fails though the file looks right** — trailing newline or BOM in the file. Re-emit with `printf '%s'`, no BOM.
- **Documents don't verify** — `path` doesn't match the real URL (slash / prefix / case). Align `path` with your permalink.
- **Build fails: "TID collision"** — two posts derived the same rkey. Spread the sub-minute offset with a stronger slug hash; assert uniqueness at build time.
- **Note posted but truncated** — expected: over 300 graphemes becomes an excerpt plus a link card.
- **Old notes suddenly flooded the timeline** — you backfilled Track B. Only POSSE forward from a cutoff; `createdAt` keeps the true date.
- **Media upload fails validation** — check the declared MIME type, file signature, byte limit, and
  dimensions. The sync intentionally rejects incompatible media instead of silently resizing or
  truncating it.
- **Write rejected: unknown lexicon** — pass `validate: false` for `site.standard.*` writes.
- **Rate limited** — the PDS write budget is roughly 5,000 points/hour per DID (a create costs 3 points). Batch/backfill within that; `applyWrites` can group writes into one commit.
- **Secret leaked in logs** — never print the App Password or JWT. Keep the secret write-only in CI.

## Safety & idempotency checklist

Before your first live write, confirm every box:

- **Dry-run by default** — writing requires an explicit `--commit` *and* the secret.
- **Create/update only, never delete.**
- **Collection-scoped** — the script names exactly the collection it manages.
- **Write-scope guard** — only touch records carrying your `sourceHash`; never modify others (protects
      hand-authored posts in the shared `app.bsky.feed.post` collection).
- **Idempotent** — skip when the remote `sourceHash` matches the staged one.
- **Fail-fast on corrupt staging** — a missing/blank `sourceHash` aborts, never a silent rewrite storm.
- **PDS resolved dynamically** from the DID document, never hardcoded.
- **`path` == real URL** for every document.
- **`.well-known` byte-exact** — no trailing newline, no BOM.
- **Track B forward-only** from an activation cutoff.
- **Part C forward-only** from independent image/gallery and video activation cutoffs.
- **Media uploads happen only after planning and authentication**, and all required blobs are ready
      before the first native post write.
- **Ship dormant behind a flag** and prove your generated output is byte-identical before activating, so
      the addition can't change what your site already serves.

> 🔧 **Reference (F# static site):** the whole feature shipped behind an off-by-default compile flag and was
> proven byte-identical to production (a full hash diff of the generated output, 0 differing files) *before*
> the first activation. Activation was then a separate, cautious, capped first write.

## Reference implementation & further reading

A complete, hand-built F# implementation of everything above (static generator + one `dotnet fsi` sync
script, no self-hosted infrastructure) lives in a public repository. Useful files to read as a worked
example:

- `AtProtoBuilder.fs` — record contract, deterministic `deriveTid`, `sourceHash`, staging, the `<link>` tag.
- `Scripts/sync-atproto.fsx` — the dry-run-by-default POSSE sync (create/update-only, write-scope guard,
  `--commit` / `--limit` / `--collection` / `--dir`, media validation, image/gallery materialization,
  and bounded video processing).
- `Scripts/create-atproto-publication.fsx` — the idempotent one-time publication bootstrap.
- The publish workflow — the gated CI sync job.
- `docs/atproto/` and `docs/adr/0009-at-protocol-integration.md` — architecture + the durable decisions.

The same repository's **ActivityPub** integration is the sibling "static hub + thin dynamic spoke" this
mirrors — a good second reference for the overall shape.

External docs:

- [AT Protocol](https://atproto.com/) · [TID spec](https://atproto.com/specs/tid) ·
  [Record Key spec](https://atproto.com/specs/record-key)
- [Standard.site](https://standard.site/) ·
  [Standard.site verification](https://standard.site/docs/verification/) ·
  [Standard.site in the Bluesky timeline](https://atproto.com/blog/standard-site-bluesky-timeline)
- [Bluesky: create an App Password](https://bsky.app/settings/app-passwords) ·
  [set your domain as your handle](https://bsky.social/about/blog/4-28-2023-domain-handle-tutorial)
- [IndieWeb: POSSE](https://indieweb.org/POSSE) · [why own your content](https://indieweb.org/why)

## Appendix A — Placeholders

Replace these throughout with your own values:

- `yourdomain.com` — your domain, which is also your Bluesky **handle**.
- `did:plc:xxxxxxxxxxxxxxxxxxxxxxxx` — your **DID** (from `resolveHandle`).
- `https://xxxxxxxx.us-east.host.bsky.network` — your **PDS** host (resolved from the DID document).
- `xxxx-xxxx-xxxx-xxxx` / `APP_PASSWORD` — a dedicated Bluesky **App Password** (repo secret `ATPROTO_APP_PASSWORD`).
- `<PUB_RKEY>` — the record key of your one `site.standard.publication`.
- `at://…/site.standard.publication/<PUB_RKEY>` — your **publication AT-URI**.
- `<TID>` — a `site.standard.document` / `app.bsky.feed.post` record key.
- `<hash-of-content>` — your `sourceHash` extension-field value.

## Appendix B — Example records (JSON)

**`site.standard.publication`** (created once):

```json
{
  "$type": "site.standard.publication",
  "url": "https://yourdomain.com",
  "name": "Your Site Name",
  "description": "A short description of your site.",
  "preferences": { "showInDiscover": true }
}
```

**`site.standard.document`** (one per article, Track A):

```json
{
  "$type": "site.standard.document",
  "site": "at://did:plc:xxxxxxxxxxxxxxxxxxxxxxxx/site.standard.publication/<PUB_RKEY>",
  "title": "My Article Title",
  "path": "/posts/my-article/",
  "description": "One-line summary.",
  "textContent": "The article body, converted to plain text…",
  "tags": ["indieweb", "atproto"],
  "publishedAt": "2026-01-31T22:14:00.000-05:00",
  "sourceHash": "d41d8cd98f00b204e9800998ecf8427e"
}
```

**`app.bsky.feed.post`** (short note as a native timeline post, Track B):

```json
{
  "$type": "app.bsky.feed.post",
  "text": "A short note. Longer ones post as an excerpt plus a link card back to the site.",
  "createdAt": "2026-07-13T09:00:00.000-05:00",
  "langs": ["en"],
  "embed": {
    "$type": "app.bsky.embed.external",
    "external": {
      "uri": "https://yourdomain.com/notes/my-note/",
      "title": "My Note",
      "description": "One-line summary."
    }
  },
  "sourceHash": "d41d8cd98f00b204e9800998ecf8427e"
}
```
