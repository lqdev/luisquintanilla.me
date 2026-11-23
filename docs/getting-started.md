# Getting started

Build the site and preview it on your machine. Everything below was run in a fresh clone on
Windows on 2026-11-23, and the output shown is what came back.

## What you need

- The .NET SDK 11.0.100. `global.json` pins that exact version and doesn't roll forward, so a
  newer SDK won't pick up the project. Check with `dotnet --version`.
- Git, and Node.js if you want to preview with `npx serve` (I used Node 22).

If you'd rather not install anything, open the repo in its dev container. `.devcontainer.json` sets
up a Debian image with the 11.0.100 SDK and the Ionide extension for F#.

## Get the code

```powershell
git clone https://github.com/lqdev/luisquintanilla.me.git
cd luisquintanilla.me
dotnet --version
```

You should see `11.0.100`.

## Restore with your own copy of the lock file

The repo commits `packages.lock.json`, and CI restores with `--locked-mode`. On my machine that
failed:

```text
error NU1403: Package content hash validation failed for FSharp.Core.11.0.101. The package is
different than the last restore.
```

The committed lock file's hashes match what CI downloads, and a local NuGet cache sometimes holds a
package with a different hash. So restore against a copy of the lock file kept outside the repo,
and leave the committed one alone:

```powershell
$localLock = Join-Path $env:TEMP 'lqdev-net11-local.lock.json'
Copy-Item packages.lock.json $localLock
dotnet restore --force-evaluate -p:NuGetLockFilePath=$localLock
```

That restore took about a second. If `dotnet restore --locked-mode` works for you, you can skip
the copy and drop the `-p:NuGetLockFilePath` argument from the build below.

## Build and generate

```powershell
$env:GITHUB_TOKEN = ''
$env:ACTIVITYPUB_STORAGE_CONNECTION = ''
dotnet build --no-restore -p:NuGetLockFilePath=$localLock
dotnet run --no-build -- --strict
```

The build took 10 seconds and ended with `1 Warning(s)` and `0 Error(s)`. The warning is FS1104 in
`ActivityPubBuilder.fs`, and it's expected.

Generating the site took 95 seconds and wrote 14,645 files to `_public/`. The last lines looked like
this:

```text
✅ Search indexes generated: 1904 content items, 1716 tags
⚠ 68 broken internal link(s)
✅ Link previews generated: 1956 records in 64 buckets
✅ Link registry generated: 2915 links from 1904 content pages (68 broken)
```

The broken links are a report, not a failure. With `--strict` the run only fails on content errors,
such as a file whose frontmatter doesn't parse. The exit code was 0.

## Preview it

```powershell
npx serve _public
```

Open the address it prints (http://localhost:3000 by default). The home page and /posts/ both
loaded for me.

## What gets skipped on your machine

The two empty environment variables above are how CI runs pull requests, and both features degrade
quietly:

- Without `GITHUB_TOKEN`, `EntityExtraction.fs` doesn't create a chat client, so the AI entity
  extraction for the AI Memex knowledge graph is skipped.
- Without `ACTIVITYPUB_STORAGE_CONNECTION`, `Services/FollowersSync.fs` prints a warning and
  returns an empty followers list. The build still writes the followers file.

## When it fails

If restore fails with NU1403, use the isolated lock file route above. Don't commit a
changed `packages.lock.json`: CI checks the committed hashes, and a hash from your machine can break
it. After restoring, compare the package versions in your copy with the committed file; only the
hashes should differ.

If `dotnet --version` prints something other than 11.0.100, install that SDK or use the dev
container. `global.json` stops the build from using a different one.

If `dotnet run -- --strict` exits 1, scroll up for the error blocks. Each names the file and the
problem. Without `--strict` the same file is left out and the rest of the site still builds.

For the build flags, the environment variables, and the checks CI runs, see the
[build reference](reference/build.md). For how the pieces fit together, see the
[architecture page](architecture.md).
