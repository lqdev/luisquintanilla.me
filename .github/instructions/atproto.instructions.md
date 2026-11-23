---
applyTo: "AtProto*.fs,Services/AtProtoMediaValidation.fs,Scripts/sync-atproto.fsx"
---

# AT Protocol

The build writes staging records for Bluesky, and `Scripts/sync-atproto.fsx` uploads them from CI.

- Treat the integration as write-only POSSE over raw XRPC calls. There is no AT Protocol SDK and nothing reads the firehose or Jetstream, so don't add either for a small change.
- Never run `Scripts/sync-atproto.fsx --commit` from a session. Without `--commit` it only reads and prints its plan, which is the safe way to check a change.
- Don't change how record keys are derived. They are deterministic TIDs built from stable metadata, so a different derivation writes every post again under a new key.
- Ship a new mode switched off: a flag set to `false` and a `DateTimeOffset.MaxValue` cutoff. Turning it on means setting both the flag and a real forward-only date, in its own reviewed change.
- For a first live run of a new mode, use `--limit` to write a small batch and check it before the full backfill.
- Run `test-scripts/test-atproto-response-mapping.fsx` and `test-scripts/test-atproto-media.fsx` after a change here.

Read first: `docs/atproto/README.md`, `docs/adr/0009-at-protocol-integration.md`
