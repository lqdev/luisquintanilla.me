# AT Protocol Integration Documentation

**🎯 Start Here** — entrypoint and source of truth for the site's AT Protocol / ATmosphere integration.

**Status:**
- **Part A (publication node): 🟢 LIVE** — lqdev.me is a verified `site.standard.publication`.
- **Part B (per-post documents): 🟡 IMPLEMENTED, FLAG OFF** — merged behind
  `AtProtoBuilder.useAtProtoSync = false`; `_public/` output is byte-identical to the pre-integration
  baseline until [activated](#-activation-in-3-steps).

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

- **Static:** the normal `dotnet run` build stages one `site.standard.document` record per Post and
  renders a verification `<link>` tag into each post — pure functions, no network.
- **Dynamic:** one post-build `dotnet fsi` script (`Scripts/sync-atproto.fsx`) upserts those records to
  the existing Bluesky-hosted PDS. No Azure Function, no new infrastructure.

Reused identity: handle `lqdev.me` / `did:plc:pme7qquljcdx6i4zyawoxypd`, hosted on Bluesky's PDS.

---

## 🔒 Safety model (why this is safe to merge with the flag off)

- **Dry-run by default.** `sync-atproto.fsx` writes nothing without `--commit` **and** the
  `ATPROTO_APP_PASSWORD` secret.
- **Flag-off = no-op.** With `useAtProtoSync = false`, no staging is produced; the CI sync job is
  **skipped entirely** and `_public/` is byte-identical to baseline.
- **Collection-scoped + write-scope guard.** Only touches `site.standard.document`, and only manages
  records bearing our `sourceHash` — hand-authored `app.bsky.feed.post` content is untouchable.
- **Create/update only, never delete. Idempotent. Fail-fast on corrupt staging.**

Full details in [ARCHITECTURE-OVERVIEW.md §6](ARCHITECTURE-OVERVIEW.md#6-sync-script--the-only-dynamic-step).

---

## 🚀 Activation in 3 steps

Each step is a deliberate, reviewable change (details:
[ARCHITECTURE-OVERVIEW.md §8](ARCHITECTURE-OVERVIEW.md#8-activation-runbook)):

1. Set `AtProtoBuilder.useAtProtoSync = true` → the CI `sync_atproto_job` starts running in **dry-run**
   and prints the plan. Review it.
2. Add the `ATPROTO_APP_PASSWORD` repository secret (a dedicated Bluesky App Password).
3. Append `--commit` to the sync step in `.github/workflows/publish-azure-static-web-apps.yml`.

Roll back by reverting step 3 (back to dry-run) or step 1 (fully off).

---

## 🧪 Tests

- `test-scripts/test-atproto-tid.fsx` — 19 assertions: TID determinism, format, collision behaviour.
- `test-scripts/test-atproto-document-json.fsx` — 24 assertions: the `site.standard.document` wire
  contract (`$type`, required fields, omit-don't-null, the verification-critical `/posts/{slug}/` path,
  tag normalization, and the `sourceHash` formula).

Run: `dotnet fsi test-scripts/test-atproto-document-json.fsx`
