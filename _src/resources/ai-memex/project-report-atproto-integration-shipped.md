---
title: "Project Report: Making lqdev.me an AT Protocol Node — Standard.site POSSE, Shipped Dormant"
description: "Landed a full AT Protocol / Standard.site integration on a static F# site behind off-by-default flags, proven byte-identical to prod before merge, then activated across two tracks — Posts as site.standard.document and Notes as native app.bsky.feed.post on the Bluesky timeline — mirroring the existing ActivityPub hub-and-spoke architecture."
entry_type: project-report
published_date: "2026-07-17 07:19 -05:00"
last_updated_date: "2026-07-17 11:58 -05:00"
tags: "atproto, bluesky, standard-site, activitypub, posse, fsharp, azure, static-site, indieweb, architecture"
related_skill: write-ai-memex
source_project: "lqdev-me"
related_entries: research-at-protocol-static-site-integration, pattern-atproto-static-node-wellknown-verification, pattern-atproto-tid-record-keys-sourcehash-workaround, pattern-long-lived-umbrella-branch-merge-strategy, pattern-ci-cd-fallout-byte-identical-refactor, pattern-staged-activation-limit-flag
---

## Objective

Make [lqdev.me](https://www.lqdev.me) — a static F# site (generator → Azure Static Web Apps) that already
federates over ActivityPub — a **first-class node in the ATmosphere (AT Protocol)** using the community
[Standard.site](https://standard.site/) Lexicons, which now get an enhanced render in the Bluesky timeline.
The constraint: keep the site's existing **"static hub, thin dynamic spokes"** shape (~99% static files,
~1% Azure Functions) and run **no PDS, relay, or AppView**. This report covers **Part B** — publishing
every Post as a `site.standard.document` record — landing on top of the already-live **Part A** (the
publication node + `.well-known` verification, see [[pattern-atproto-static-node-wellknown-verification]]).

## Architecture

The integration mirrors `ActivityPubBuilder.fs` almost beat-for-beat:

| Concern | ActivityPub (existing) | AT Protocol (this work) |
|---|---|---|
| Static identity | `/socialweb/actor.json` | `at://…/site.standard.publication/…` + `.well-known` |
| Per-post record | `create/{hash}.json` activities | `site.standard.document` records |
| Idempotency key | content-hash activity id | deterministic **TID** rkey + `sourceHash` field |
| Dynamic delivery | Azure Function queue worker | `Scripts/sync-atproto.fsx` (CI, POSSE) |
| Verification | — | in-`<head>` `<link rel="site.standard.document">` |

Two design constraints drove the record contract (detailed in
[[pattern-atproto-tid-record-keys-sourcehash-workaround]]):

1. **AT Protocol mandates TID record keys** — the ActivityPub content-hash-as-id trick can't become the
   rkey. Resolution: derive a deterministic TID from `published_date` (minute-floor) + a slug hash (so
   AT-URIs are precomputable and upserts are stateless), and carry the content hash as a **`sourceHash`
   extension field** for change detection.
2. **Omit-don't-null** — optional lexicon fields (`description`, `textContent`, `tags`) are omitted when
   empty rather than serialized as `null`, keeping records valid and lean.

Core module `AtProtoBuilder.fs` (324 lines): `Config` (DID/handle/publication AT-URI), `generateHash`,
`deriveTid` + a build-time `assertNoTidCollisions` guard, `buildDocumentRecordJson`, and
`buildAtProtoStaging` (writes `_public/api/data/atproto/documents/{rkey}.json`). The dynamic spoke is
`Scripts/sync-atproto.fsx` (250 lines, BCL-only so it runs under `dotnet fsi` with no build): it resolves
the PDS, lists existing records, and upserts — **dry-run by default**, with `--commit` gating the only
write path and a write-scope invariant that never clobbers records lacking our `sourceHash` marker.

### Shipped dormant: inert by construction

The whole feature sits behind one compile-time flag, `AtProtoBuilder.useAtProtoSync = false`
(`AtProtoBuilder.fs:202`). Every surface that could touch output is gated:

- **Staging generation** — `Program.fs:171`: `if AtProtoBuilder.useAtProtoSync then …` → not written.
- **Verification `<link>` tag** — `AtProtoBuilder.documentLinkHead` returns `[]` when `not useAtProtoSync`,
  so even though it's wired into the post `<head>` (`Builders/ContentTypePages.fs` → `Views/Layouts.fs`),
  it emits nothing.
- **CI sync job** — gated on `build_and_deploy_job.outputs.atproto_staged == 'true'`, so it's **skipped
  entirely** (not a no-op run) while the flag is off.

## Approach

Delivered as **phased stacked PRs into a long-lived umbrella branch** (`feature/atproto-integration`),
never onto `main` directly (which auto-deploys to prod) — see
[[pattern-long-lived-umbrella-branch-merge-strategy]]:

- Phase 1–3 → the record contract, staging, and the sync script.
- Phase 4 → the gated dry-run CI job (`sync_atproto_job`) + `docs/atproto/`.
- Each phase got a Copilot review (#2635, #2638, #2640, #2641, #2642), validated with an
  accept/reject/defer methodology (rejecting factually wrong suggestions with empirical probes, filing
  shovel-ready issues for defers such as **#2639**).
- One squashed **umbrella → main PR (#2643)** landed everything as a single clean commit.

## Outcome

| Metric | Value |
|---|---|
| Landed as | squash commit `35489498` on `main` (parent `e448070d`, linear) |
| Change surface | 13 files, +1247 / −10 — **zero `_src/` or `_public/` content** |
| Byte-identical proof | `verify-baseline.ps1`: **0 diff rows over 14,002 files** (excl. run-nondeterministic `graph.json`) |
| Live deploy (run `29581321403`) | Build & Deploy ✅, Queue ActivityPub ✅, Send Webmentions ✅, **Sync AT Protocol Documents ⏭️ skipped** |
| Live PDS writes | **none** — activation is a separate, deliberate step |

The merge triggered a prod deploy that served **byte-identical** content and correctly **skipped** the new
sync job — inertness proven pre-merge and confirmed live. Activation remains a three-step, user-gated
follow-up: (1) flip `useAtProtoSync = true`; (2) add the `ATPROTO_APP_PASSWORD` repo secret; (3) append
`--commit` to the workflow sync step.

## Lessons Learned

1. **Prove inertness — don't assert it.** "The flag is off, so nothing changes" is a claim, not evidence.
   A full SHA-256 diff of the entire generated `_public/` (main vs. the feature branch) turned the claim
   into a fact: 0 diff rows across 14,002 files. This is the same discipline as
   [[pattern-ci-cd-fallout-byte-identical-refactor]], applied to a *feature add* rather than a refactor.

2. **Gate at every output surface for a correct-by-construction no-op.** Because the CI job is gated on a
   *build-output* signal (staging files exist), it's **skipped**, not run-as-no-op. There's no wasted
   compute, no confusing green "did nothing" runs, and activation is purely additive (flip the flag → the
   job starts appearing). The gate and the behavior can't drift apart.

3. **`gh pr merge` updates the *remote*, not your local tracking branch.** After server-side-merging a
   phase PR into the umbrella, my local `feature/atproto-integration` was still at the pre-merge commit;
   `git fetch` only moved `origin/…`. Merging `main` onto that stale local base diverged the branch and
   the push was (correctly) rejected. Fix: `git reset --hard origin/<umbrella>` before continuing. On a
   long-lived integration branch, always re-sync local to origin after any server-side merge.

4. **Dry-run-by-default is the right posture for a write-capable script.** `sync-atproto.fsx` resolves the
   PDS, lists records, and prints a full create/update/skip plan with **no auth and no secret** required.
   The entire live-write capability is one `--commit` flag away, so the dangerous path is opt-in and
   everything short of it is safely observable — including in CI.

5. **Mirroring a proven architecture compresses risk.** Reusing the ActivityPub "static records + one thin
   dynamic sync" split meant the hard problems (idempotency, verification, POSSE cadence) were already
   solved conceptually; the work was translating them to AT Protocol's constraints (TID keys, Lexicon
   field rules) rather than inventing an integration from scratch.

## What's Next

- **Activation** (RED / deliberate): the three-step flip above — first live PDS write.
- **#2639** — finalize the `AtProtoBuilder` serialization strategy (typed DTOs vs. hand-built
  `JsonObject`); non-blocking.
- Foundation is in place to extend beyond Posts (notes, responses) to more `site.standard.document`
  records if desired.

## Update — Activated (2026-07-17)

The dormant flag is now flipped; the integration is **fully live**. Activation followed the cautious
staged approach captured in [[pattern-staged-activation-limit-flag]]:

- **Cautious first write** (PR #2644, squash `ffbe0f3a`): flipped `useAtProtoSync = true` and ran the CI
  sync with `--commit --limit 3`. Deploy run `29586883692` wrote the **3 oldest posts** and verified the
  pipeline end-to-end — PDS `listRecords` went empty→3, sync log `DONE: upserted 3/3`, and all three live
  post pages served the matching `<link rel="site.standard.document">` (bidirectional handshake).
- **Backfill** (PR #2645, squash `604182fc`): removed `--limit` → plain `--commit`. Deploy run
  `29587893500` logged `DONE: upserted 92/92` (the 3 already-written skipped on `sourceHash`).
  `listRecords` now returns **95** — every Post.

Steady state from here: every push to `main` upserts all pending Posts idempotently (create/update-only,
collection-scoped, never deletes). The first "irreversible-feeling" write to a third-party PDS was
de-risked into a reversible three-record experiment — the reusable takeaway is
[[pattern-staged-activation-limit-flag]].

## Update — Track B: Notes → native Bluesky posts (2026-07-17)

Part B above POSSEs Posts as `site.standard.document` records — rich cards **when a client supports the
Standard.site lexicon**. **Track B** goes one step further for **Notes**: it writes native
**`app.bsky.feed.post`** records, so short-form content shows up as ordinary posts directly in the
`bsky.app/profile/lqdev.me` timeline — no special client support required — each carrying an
`app.bsky.embed.external` card that links back to the canonical `/notes/{slug}/` URL on the site.

### Design deltas from Track A

- **Shared, third-party-populated collection.** Track A owns a custom collection (`site.standard.document`);
  Track B writes into `app.bsky.feed.post`, which **already held 14 hand-authored posts**. Same `sourceHash`
  write-scope invariant — now doing real work: every record the sync manages carries a `sourceHash` extension
  field, and the script only ever `putRecord`s records **it created**. Hand-authored posts carry no
  `sourceHash` → they are structurally never in the plan. Proven live: `PLAN: 1 create, 0 update, 0
  unchanged, 0 left-untouched` against a remote holding 14 untouched posts.
- **Forward-only cutoff, not backfill.** Bluesky feeds sort by **ingest time (`indexedAt`)**, not
  `createdAt`, so backfilling history would spam followers' timelines in reverse. Track B is gated on
  `notesActivationCutoff` (`2026-07-13`) — only Notes on/after the cutoff are eligible — so the timeline
  fills forward at natural cadence. `createdAt` still carries the true publish date, so each post is *dated*
  correctly even though it *ingests* now.
- **Same deterministic TID + idempotent upsert** as Track A, reused verbatim via `deriveTid`.

### Shipped dormant, then activated — same two-step discipline

Track B followed the identical cautious path (see [[pattern-staged-activation-limit-flag]]):

- **Dormant** (PR #2647, squash `27901b6c`): full Track B behind `useAtProtoNotesSync = false`; a
  parameterized `sync-atproto.fsx --collection` plus a **collection cross-check** that aborts *before any
  network I/O* if a staged record's collection doesn't match `--collection` — closing a footgun where the
  default `--dir` could otherwise load Track A documents into the posts collection. Verified byte-identical.
- **Activation** (PR #2648, squash `6f3a64b3`): flipped the flag `true` and switched the CI step to a live
  capped write, `--commit --limit 1`. Deploy run `29598413239` logged `+ CREATE 3mql6jgoavwtp … ✓ put …
  DONE: upserted 1/1`. Verified in the PDS (`getRecord`) and the public AppView (`getPostThread`,
  `@lqdev.me`) → **[the first Note is live on the timeline](https://bsky.app/profile/lqdev.me/post/3mql6jgoavwtp)**.

### Result

`lqdev.me` is now a **dual-track POSSE node**: long-form Posts as `site.standard.document` (rich cards for
Standard.site-aware clients) **and** Notes as native `app.bsky.feed.post` (ordinary timeline posts for
everyone). Both are static-first — records generated at build, one thin `dotnet fsi` sync spoke in CI —
create/update-only, never deleting. Remaining: a one-line follow-up removes `--limit 1` for steady-state
forward-only POSSE now that the first post is verified live.
