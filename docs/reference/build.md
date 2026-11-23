# Build reference

The commands, flags, environment variables, and checks the generator uses. For a first run, start
with [getting started](../getting-started.md).

## Commands

| Command | What it does |
| --- | --- |
| `dotnet restore --locked-mode` | Restores packages and fails if anything differs from `packages.lock.json`. Pull request CI uses this. |
| `dotnet build --no-restore` | Compiles the generator. One FS1104 warning in `ActivityPubBuilder.fs` is expected. |
| `dotnet run --no-build` | Generates the site from `_src/` and `Data/` into `_public/`. |
| `dotnet run --no-build -- --strict` | The same, but exits 1 if any content file had an error. |

## Strict mode

By default a content file with an error, such as frontmatter that doesn't parse, is printed as an
error block and left out, and the run exits 0. That way one bad file doesn't stop the rest of the
site from publishing.

Strict mode turns any content error into exit code 1. Turn it on with the `--strict` argument or by
setting `STRICT_CONTENT=1`. Both are checked by `isStrict` in `Diagnostics.fs`. CI and the deploy
workflow both run with `--strict`.

## Environment variables

| Variable | Used by | Without it |
| --- | --- | --- |
| `STRICT_CONTENT` | `Diagnostics.fs` | Strict mode is off unless `--strict` is passed. Only the value 1 turns it on. |
| `GITHUB_TOKEN` | `EntityExtraction.fs` | AI entity extraction for the AI Memex knowledge graph is skipped. |
| `ACTIVITYPUB_STORAGE_CONNECTION` | `Services/FollowersSync.fs` | A warning is printed and the followers list is empty. |

Pull request CI sets the last two to empty strings, so it builds the same way a fresh clone does.

## The lock file

`packages.lock.json` is committed, and pull request CI restores with `--locked-mode`. If restore
fails on your machine with NU1403 (package content hash validation failed), restore against a copy
of the lock file outside the repo:

```powershell
$localLock = Join-Path $env:TEMP 'lqdev-net11-local.lock.json'
Copy-Item packages.lock.json $localLock
dotnet restore --force-evaluate -p:NuGetLockFilePath=$localLock
dotnet build --no-restore -p:NuGetLockFilePath=$localLock
```

Check that the package versions in your copy match the committed file, and don't commit a changed
`packages.lock.json`. [CONTRIBUTING.md](../../CONTRIBUTING.md) has the same procedure, and the
[lock hash drift pattern](../../_src/resources/ai-memex/pattern-nuget-lock-hash-drift.md) explains
why the hashes differ.

## What CI checks

`.github/workflows/validate-site-upgrade.yml` runs on pull requests, except ones that only change
`_src/` or `projects/archive/`. Its site job restores with `--locked-mode`, builds, generates with
`--strict`, and then runs these scripts from `test-scripts/`:

- `test-rss-generated-output.fsx`
- `test-rss-correctness.fsx`
- `test-rss-cdata.fsx`
- `test-rss-reply-threading.fsx`
- `test-rss-semantics.fsx`
- `test-social-discovery.fsx`
- `test-atproto-response-mapping.fsx`
- `test-atproto-media.fsx`
- `test-follow-yaml.fsx`
- `test-docs-integrity.fsx`
- `test-follow-choice.js` and `test-follow-processing.js`, run with Node

Its api job runs `npm ci`, `npm audit`, and `npm test` in `api/`.

## What the deploy checks

`.github/workflows/publish-azure-static-web-apps.yml` runs on every push to main. It restores with
`--force-evaluate` rather than `--locked-mode`, builds, generates with `--strict`, and runs the four
RSS scripts and `test-social-discovery.fsx` before deploying. It then runs
`Scripts/check-site-sizes.sh`, which warns when `_public/` reaches 14,000 files, because Azure
Static Web Apps allows 15,000. The build on 2026-11-23 wrote 14,645.
