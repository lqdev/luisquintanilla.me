# Contributing

How changes get made to this repo, and what to check before one goes in.

## Build and check your change

Restore, build, and generate the site the way CI does. Clearing the two variables turns off the
optional services, as CI does for pull requests:

```powershell
dotnet restore --locked-mode
dotnet build --no-restore
$env:GITHUB_TOKEN = ''
$env:ACTIVITYPUB_STORAGE_CONNECTION = ''
dotnet run --no-build -- --strict
```

If the locked restore fails with NU1403 on your machine, use the isolated lock file route in
[docs/getting-started.md](docs/getting-started.md), and don't commit a changed
`packages.lock.json`. The flags, environment variables, and the tests CI runs are in the
[build reference](docs/reference/build.md).

## Where things are

Content is in `_src/`, the generator is the F# project at the root, and
[docs/architecture.md](docs/architecture.md) maps each folder and file to what it does.

## Conventions

- Changes go in on a branch through a pull request. Pull requests that change anything outside
  `_src/` and `projects/archive/` run the `Validate site and API` workflow, and it has to pass.
- A docs change has to pass `dotnet fsi test-scripts/test-docs-integrity.fsx`, which CI also runs.
- A new F# file goes into `PersonalSite.fsproj` in dependency order. F# compiles files in the order
  the project lists them, so a file can only use what comes before it.
- Don't edit `_public/`. The build writes it.

## Docs

Where a page goes and how it's laid out is in
[docs/contributing/docs-style.md](docs/contributing/docs-style.md). How it should sound is in
[docs/contributing/voice.md](docs/contributing/voice.md).

## AI assistants

Instructions for AI coding agents are in [AGENTS.md](AGENTS.md).