---
title: "Pattern: De-risk a First Live Write with a --limit N Staged Activation"
description: "Before the first irreversible-feeling write to an external stateful system, add a --limit N flag to the sync tool: write a tiny batch, verify the full handshake end-to-end, then remove the flag to backfill — idempotency makes the two phases converge with zero double-writes."
entry_type: pattern
published_date: "2026-07-17 09:32 -05:00"
last_updated_date: "2026-07-17 09:32 -05:00"
tags: "atproto, bluesky, posse, ci-cd, deployment, idempotency, fsharp, architecture, patterns"
related_skill: write-ai-memex
source_project: "lqdev-me"
related_entries: project-report-atproto-integration-shipped, pattern-atproto-tid-record-keys-sourcehash-workaround, pattern-atproto-static-node-wellknown-verification
---

## Discovery

Activating the AT Protocol POSSE integration for lqdev.me meant the **first-ever live write** to the
site's Bluesky-hosted PDS — pushing 95 `site.standard.document` records (one per Post) via a CI job. All
the code had landed on `main` weeks earlier, dormant behind `AtProtoBuilder.useAtProtoSync = false`, and
had been dry-run-validated repeatedly. Yet the actual cutover still felt irreversible: writing to a remote,
stateful, third-party system you don't host, with a lexicon whose reader/indexer rendering you can't fully
observe locally.

The temptation was to flip the flag and let the job write all 95 at once. The safer instinct — proven
correct — was to write **3 records first**, verify the entire pipeline end-to-end, and only then backfill
the remaining 92.

## Root Cause — why "dry-run passed" isn't enough

A dry run exercises serialization and the *plan* (create/update/skip diffing), but it does **not** exercise
the parts that only exist at real write time:

- **Auth + session** — `createSession` with the app password, JWT handling, PDS resolution.
- **The live `putRecord` path** — `validate:false` behaviour, `validationStatus:"unknown"`, CID return.
- **The bidirectional verification handshake** — each live page must serve
  `<link rel="site.standard.document" href="at://…/{rkey}">` that resolves to a record whose `path`
  points *back* at that page. You can only confirm the round-trip once real `rkey`s exist.
- **Reader/indexer shape** — whether external indexers (Standard.site discovery, Bluesky cards) accept the
  record's field layout.

If any of these is subtly wrong, doing all 95 at once multiplies the mistake 95× and buries the signal in
noise. A 3-record batch makes a wrong field trivially visible and cheap to fix.

## Solution — a `--limit N` flag + an end-to-end verification gate

Add a small, permanent safety valve to the sync engine that caps how many records a single run may write.
In `Scripts/sync-atproto.fsx` the plan is computed as a tagged array, then truncated:

```fsharp
// planned: ("CREATE" | "UPDATE", Staged)[]  — creates sorted by rkey, then updates
let plannedAll = Array.append creates updates
let planned =
    match limitOpt with
    | Some n when n > 0 -> Array.truncate n plannedAll   // cap this run to the first N
    | _                 -> plannedAll
// ... later ...
for (_op, s) in planned do
    putRecord session s     // Thread.Sleep 150 between writes; validate:false
```

Activate cautiously by wiring the CI step to write only a few:

```yaml
run: dotnet fsi Scripts/sync-atproto.fsx --dir atproto-staging --commit --limit 3
```

**Verify the full handshake before going further** — all three surfaces, not just the write log:

```powershell
# 1. The records actually landed (empty -> 3):
curl.exe -s "https://amanita.us-east.host.bsky.network/xrpc/com.atproto.repo.listRecords?repo=$did&collection=site.standard.document"

# 2. The sync log agrees:  "PLAN: 95 create" -> "--limit 3" -> "DONE: upserted 3/3"

# 3. The bidirectional <link> resolves on the LIVE page (the real test):
curl.exe -s "https://lqdev.me/posts/hello-world/" | Select-String 'site\.standard\.document'
#   -> href="at://…/site.standard.document/3ezxgyodnba6r"  (matches the record whose path = /posts/hello-world/)
```

Only after all three pass, **backfill with a one-line follow-up PR** that removes the cap:

```yaml
run: dotnet fsi Scripts/sync-atproto.fsx --dir atproto-staging --commit
```

The next deploy logged `DONE: upserted 92/92` — and the 3 already-written records were **skipped** because
their `sourceHash` still matched. `listRecords` went to a clean **95**. No record was ever written twice.

## Why `--limit N` beats a date cutoff or a hand-picked list

Because the collection uses time-ordered TIDs as record keys (see
[[pattern-atproto-tid-record-keys-sourcehash-workaround]]), the create set is naturally sorted oldest-first.
`--limit 3` therefore deterministically selects the **3 oldest posts** with zero configuration — no cutoff
date to choose, no slugs to hand-enumerate, and it's reproducible. The cautious batch and the full backfill
draw from the same computed plan, so they can't disagree about *what* to write, only *how many*.

## Prevention — when to reach for this

Use a `--limit N` staged activation for **any first live write to an external, stateful system you don't
host**: POSSE syndication, bulk webhook/API upserts, first-run data migrations, bulk email. The two
preconditions that make it safe:

1. **Idempotency** — a content-hash skip (here `sourceHash`) so re-running without the cap writes only the
   remainder and never double-writes. Without this, staging is dangerous.
2. **An observable end-to-end signal** — not just "the API returned 200", but the downstream artifact a
   consumer actually sees (here, the on-page `<link>` round-trip). Verify that on the small batch.

Keep the flag in the tool permanently. It doubles as a throttle for rate-limited targets and a blast-radius
limiter for any future risky re-sync. The cost is ~10 lines; the payoff is turning an irreversible-feeling
cutover into a reversible, observable, three-record experiment.

This activation followed [[project-report-atproto-integration-shipped]] (the dormant ship) and completed
the bidirectional verification described in [[pattern-atproto-static-node-wellknown-verification]].
