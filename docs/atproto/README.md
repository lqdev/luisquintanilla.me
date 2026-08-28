# AT Protocol Integration Documentation

**🎯 Start Here** — entrypoint and source of truth for the site's AT Protocol / ATmosphere integration.

**Status:**
- **Part A (publication node): 🟢 LIVE** — lqdev.me is a verified `site.standard.publication`.
- **Part B (per-post documents): 🟢 LIVE** — `AtProtoBuilder.useAtProtoSync = true`; every push to
  `main` upserts one `site.standard.document` record per Post via `Scripts/sync-atproto.fsx --commit`.
- **Track B (Notes → native posts): 🟢 LIVE** — `AtProtoBuilder.useAtProtoNotesSync = true`; Notes
  published on/after `notesActivationCutoff` (2026-07-13) are POSSE'd to the Bluesky timeline as
  `app.bsky.feed.post` records (forward-only). Roll back by dropping `--commit` (dry-run) or setting the
  flag(s) to `false` (fully off, byte-identical baseline).
- **Part C (rich-media POSSE): 🟢 IMAGE PHASE ACTIVE; GALLERY/VIDEO DORMANT** — deterministic
  image/gallery/video manifests, native embeds, source-hash protection, binary validation, and CI phase
  gates are implemented. `AtProtoBuilder.useAtProtoMediaImageSync = true` stages eligible images
  published on/after the 2026-08-01 cutoff; gallery and video flags remain `false`.
- **Response POSSE (bookmarks + reshares → native records): 🟡 IMPLEMENTED, DORMANT** — bookmarks and
  ordinary-web reshares → `app.bsky.feed.post` link posts; ATProto-targeted reshares → `app.bsky.feed.repost`
  (no commentary) or `app.bsky.feed.post` quote-post (with commentary). Four flags
  (`useAtProtoBookmarkPostsSync`, `useAtProtoResharePostsSync`, `useAtProtoRepostsSync`,
  `useAtProtoQuotePostsSync`) are all `false` with `DateTimeOffset.MaxValue` sentinel cutoffs. Activate a
  mode by setting its flag `true` AND replacing its sentinel with a real forward-only date. See the
  ADR-0009 2026-08-27 Response-POSSE amendment.

---

## 📖 Primary documentation

**[ARCHITECTURE-OVERVIEW.md](ARCHITECTURE-OVERVIEW.md)** ⭐ **START HERE**
- Identity & hosting (reuse, no self-hosted infra)
- Build pipeline (`AtProtoBuilder.fs`), the `site.standard.document` record, deterministic TID keys
- Verification handshake, sync-script safety model, CI job, file map, references

**[ADR-0009](../adr/0009-at-protocol-integration.md)** — the durable architectural decisions
(reuse identity, no self-hosted PDS/relay/AppView, DIY F# over a third-party CLI, TID-constrained keys
handled via a `sourceHash` extension field).

**Issue [#2574](https://github.com/lqdev/luisquintanilla.me/issues/2574)** — implementation tracking.

---

## 🧭 What this is

The same "static hub, thin dynamic spoke" model the site already uses for
[ActivityPub](../activitypub/ARCHITECTURE-OVERVIEW.md), applied to AT Protocol:

- **Static:** the normal `dotnet run` build stages `site.standard.document` records for Posts,
  native Note records, and (when activated) media manifests — pure functions, no network.
- **Dynamic:** one post-build `dotnet fsi` script (`Scripts/sync-atproto.fsx`) upserts those records and
  materializes media blobs in the existing Bluesky-hosted PDS. No Azure Function, no new infrastructure.

Reused identity: handle `lqdev.me` / `did:plc:pme7qquljcdx6i4zyawoxypd`, hosted on Bluesky's PDS.

---

## 🔒 Safety model (why the live sync is safe)

- **Dry-run unless `--commit`.** `sync-atproto.fsx` writes nothing without `--commit` **and** the
  `ATPROTO_APP_PASSWORD` secret, so you can preview the plan read-only. **CI passes `--commit`** on every
  push to `main` (live).
- **Independent flag gates.** Document, Note, image/gallery, and video staging are controlled
  independently. When all are false, no staging is produced, the CI sync job is **skipped entirely**,
  and `_public/` is byte-identical to baseline.
- **Collection-scoped + write-scope guard.** Each sync invocation targets one collection; shared
  `app.bsky.feed.post` records are managed only when they carry our `sourceHash`, so hand-authored
  content is untouchable.
- **Create/update only, never delete. Idempotent. Fail-fast on corrupt staging.**

Full details in [ARCHITECTURE-OVERVIEW.md §6](ARCHITECTURE-OVERVIEW.md#6-sync-script--the-only-dynamic-step).

---

## 🚀 Activation — Tracks A/B + image phase ✅ complete; gallery/video 🟡 dormant

The document and Note tracks run **live on every push to `main`**. The same sequence is retained as
the historical runbook (details:
[ARCHITECTURE-OVERVIEW.md §9](ARCHITECTURE-OVERVIEW.md#9-activation-runbook)):

1. ✅ `AtProtoBuilder.useAtProtoSync = true` (Track A) and `useAtProtoNotesSync = true` (Track B).
2. ✅ `ATPROTO_APP_PASSWORD` repository secret added (a dedicated Bluesky App Password).
3. ✅ `--commit` on the sync steps in `.github/workflows/publish-azure-static-web-apps.yml`.

4. ✅ Image phase activated with `useAtProtoMediaImageSync = true` and the 2026-08-01 forward-only
   cutoff. The workflow's image sync is collection-scoped and idempotent.
5. 🟡 Gallery and video remain independently gated; activate each only after its own rollout review.

Roll back by removing `--commit` (back to dry-run) or setting the affected staging flag to `false`.

---

## 🧪 Tests

- `test-scripts/test-atproto-tid.fsx` — 19 assertions: TID determinism, format, collision behaviour.
- `test-scripts/test-atproto-document-json.fsx` — 24 assertions: the `site.standard.document` wire
  contract (`$type`, required fields, omit-don't-null, the verification-critical `/posts/{slug}/` path,
  tag normalization, and the `sourceHash` formula).
- `test-scripts/test-atproto-media.fsx` — 29 assertions: media extraction, native embed selection,
  gallery `items`, alt fallbacks, facets, limits, hashes, cutoffs, validation, and native rkey collisions.
- `test-scripts/test-atproto-response-mapping.fsx` — 46 assertions: strict target-URL parsing
  (`bsky.app` permalink / `at://` post URI vs ordinary + non-post Bluesky links), the authored-commentary
  quote-vs-repost decision (including a `>` inside a code fence), and the bookmark/reshare/quote/repost
  record builders (text contract, external-vs-record embeds, no-`sourceHash` reposts, namespaced rkeys).

Run: `dotnet fsi test-scripts/test-atproto-document-json.fsx`
