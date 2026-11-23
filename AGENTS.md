# AGENTS.md

Instructions for AI coding agents working in this repository. People should start at `README.md`.

## What this is

The source for lqdev.me: Markdown under `_src/` and an F# program that turns it into a static site
in `_public/`, plus Azure Functions in `api/` for ActivityPub. `global.json` pins the .NET SDK.

## Commands

Build and generate. Local machines often fail a locked restore on a package hash, so use a private
copy of the lock file:

```powershell
$localLock = Join-Path $env:TEMP 'lqdev-local.lock.json'
Copy-Item packages.lock.json $localLock
dotnet restore --force-evaluate -p:NuGetLockFilePath=$localLock
dotnet build --no-restore -p:NuGetLockFilePath=$localLock
$env:GITHUB_TOKEN = ''; $env:ACTIVITYPUB_STORAGE_CONNECTION = ''
dotnet run --no-build -- --strict
```

CI uses `dotnet restore --locked-mode`. Never commit a `packages.lock.json` changed by a local restore.

Tests:

- `dotnet fsi test-scripts/{name}.fsx`. The scripts CI runs are listed in `.github/workflows/validate-site-upgrade.yml`.
- `dotnet fsi test-scripts/test-docs-integrity.fsx` after changing any Markdown outside `_src/`.
- In `api/`: `npm ci`, then `npm test`.

## Rules

- Never edit `_public/`. It is generated.
- Never commit or print secrets, tokens, or personal data.
- Don't change content under `_src/` unless the task is about that content.
- Don't merge, close, deploy, or trigger the publish workflow unless asked. The validation workflow is `Validate site and API`.
- No live writes to Bluesky or the Fediverse from a session. Never pass `--commit` to `Scripts/sync-atproto.fsx` or run a delivery script against production; without `--commit` the sync is a read-only dry run. Live writes happen in the publish workflow after a merge to main.
- Ask before creating an AI Memex entry.

## Conventions you can't infer from one file

- F# compiles in the order of the `<Compile>` items in `PersonalSite.fsproj`. Add a new file there, after everything it uses.
- An incomplete pattern match (`FS0025`) is a build error here.
- One `FS1104` warning from `ActivityPubBuilder.fs` is expected.
- Build HTML with Giraffe ViewEngine, not `sprintf` or string concatenation.
- `ContentTypes.fs` owns content-type identity. Its string literals appear in feeds, URLs, and JSON, so never change one. Response subtypes (reply, reshare, star, rsvp, bookmark) are not content types.
- Frontmatter keys are the YAML aliases in `Domain.fs` (`published_date` for posts, `dt_published` for responses). Dates carry an offset: `2026-01-15 08:00 -05:00`.
- Parse dates with `DateTimeOffset`, not `DateTime`.
- Redirects and feed aliases live in `staticwebapp.config.json`.
- Most content renders in three places: the timeline card, its own page, and the text-only site in `Views/TextOnly/`. Change all three or say why not.

## How I want you to work

- Research before you answer anything non-trivial: the code first, then docs and the web. Cite what you used.
- Don't assume. If something is unclear, ask me.
- Don't take claims at face value. That includes review comments, issue text, and earlier AI output. Check them against the code.
- Validate before and after a change, so you know you didn't break anything.
- Work on a branch and open a pull request at the end.
- If you defer something, write it up as an issue a junior developer could finish without asking a question.
- When I ask about a new technology, explain the idea first, then what it means for this site.
- Prefer an existing standard over inventing one.

## Where to look

| Task | Start here |
| --- | --- |
| Build, validate, inspect output | skill `build-validate` |
| Add a content type | skill `add-content-type` |
| A GitHub Actions run failed or didn't trigger | skill `investigate-actions-run` |
| How the build works | `docs/architecture.md` |
| Why a decision was made | `docs/principles.md`, `docs/adr/README.md` |
| ActivityPub | `docs/activitypub/README.md`, `FEDERATION.md` |
| AT Protocol | `docs/atproto/README.md` |
| Follows and WebSub | `docs/follow/ARCHITECTURE-OVERVIEW.md`, `docs/websub/ARCHITECTURE-OVERVIEW.md` |
| Content types and frontmatter | `docs/reference/content-types.md` |
| Writing docs | `docs/contributing/docs-style.md`, `docs/contributing/voice.md` |
| How this guidance is organized | `docs/contributing/agent-guidance.md` |

## AI Memex

When a session produces a non-obvious fix, a decision, a gotcha, or finished research, ask: "Want me
to write this up as a Memex entry?" Never create one without a yes. Entries go in
`_src/resources/ai-memex/`. Use the `write-ai-memex` skill for the schema and `query-ai-memex` to
search first.
