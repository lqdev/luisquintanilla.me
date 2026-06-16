---
title: "Pattern: NuGet Lock File Hash Drift"
description: "How to diagnose and unblock NU1403 package content hash validation failures when a NuGet lock file no longer matches the package content being restored."
entry_type: pattern
published_date: "2026-04-27 11:30 -05:00"
last_updated_date: "2026-06-15 19:37 -05:00"
tags: "dotnet, devops, patterns"
related_skill: "write-ai-memex"
source_project: "website"
---

## Discovery

While drafting a FediForum presentation in the website repo, `dotnet build` failed before compilation:

```text
C:\Dev\website\PersonalSite.fsproj : error NU1403:
  Package content hash validation failed for FSharp.Core.10.1.203.
  The package is different than the last restore.
```

The project uses NuGet lock files:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

The stale entry was in `packages.lock.json`:

```json
"FSharp.Core": {
  "type": "Direct",
  "requested": "[10.1.203, )",
  "resolved": "10.1.203",
  "contentHash": "OF7jtxizT+7LKeuHQBTQF3SxcnxBXN8DEF2QuAeOV2r8UN3toAjUL9hqiTVqkerorrMzb5SJ+ppqWSIXRYsozg=="
}
```

Clearing the targeted package cache and then all NuGet caches did not resolve the issue. A locked restore continued to fail, which meant the restored package content no longer matched the hash recorded in the lock file.

## Root Cause

NuGet lock files record the expected content hash for each resolved package. When restore runs in locked mode, NuGet verifies that the package content it downloads or reads from cache matches the hash in `packages.lock.json`.

This failure can come from:

- a corrupt local package cache
- a stale HTTP cache
- a lock file generated against package content that no longer matches the current package source
- a package source/proxy serving different content for the same package version

The important distinction: **cache corruption is fixable without changing the repo; true lock drift requires regenerating the lock file and reviewing the diff.**

## Solution

Start with the least invasive fix: remove only the failing package from the global package cache and clear the HTTP cache.

```powershell
Set-Location C:\Dev\website

Remove-Item -Recurse -Force "$env:USERPROFILE\.nuget\packages\fsharp.core\10.1.203" -ErrorAction SilentlyContinue
dotnet nuget locals http-cache --clear

dotnet restore --locked-mode
dotnet build --no-restore
```

If the failure persists, clear all NuGet caches and retry locked restore:

```powershell
Set-Location C:\Dev\website

dotnet nuget locals all --clear
dotnet restore --locked-mode
dotnet build --no-restore
```

If a fully clean cache still fails, back up the lock file outside the repo and regenerate it:

```powershell
Set-Location C:\Dev\website

Copy-Item packages.lock.json "$env:TEMP\website-packages.lock.json.bak" -Force
dotnet restore --force-evaluate
dotnet build --no-restore --nologo
```

In this case, `--force-evaluate` updated only the `FSharp.Core 10.1.203` hash:

```diff
- "contentHash": "OF7jtxizT+7LKeuHQBTQF3SxcnxBXN8DEF2QuAeOV2r8UN3toAjUL9hqiTVqkerorrMzb5SJ+ppqWSIXRYsozg=="
+ "contentHash": "vkMhbcd1CDq2ipBFR1UWdfQ6iz8HwRmmraLpZbKHfMcFUGq2hsYQXFjlBEdTWuOYRRBTN6c9N11d3PYEkJBk4g=="
```

After that, the build succeeded:

```text
Build succeeded with 1 warning(s) in 7.7s
```

## Prevention

Do not immediately delete or regenerate `packages.lock.json` when NU1403 appears. Treat the lock file as supply-chain evidence:

1. First clear only the failing package and HTTP cache.
2. Then clear all NuGet caches if needed.
3. Only regenerate the lock file after a clean locked restore still fails.
4. Back up the old lock file outside the repo before regenerating.
5. Review the lock-file diff carefully and commit it separately from unrelated content changes when possible.

Avoid bypassing package hash validation. The check is useful: it distinguishes "my local cache is stale" from "the package content available to me no longer matches the dependency lock."

## Variant: Merge Conflict Across Environments (2026-06-15)

The original case above is single-environment (build fails, regenerate the hash). A second, distinct
form shows up when **merging two branches that both bumped the same package version on different
machines**. During the 2026 architecture refactor I merged `origin/main` into a long-lived umbrella
branch and hit a *git merge conflict* on `packages.lock.json`:

```diff
  "FSharp.Core": {
    "type": "Direct",
    "requested": "[10.1.301, )",
    "resolved": "10.1.301",
-   "contentHash": "MYegQxogsOgt0SQrH7wUMRJmQb6Yn8Vke12VMGIeU17KLcsQOFVISPsEPY4fDFSb2NEl/CVYztVy92q0RPKp2A=="   # umbrella (local machine)
+   "contentHash": "FwQFuqOA1+qQnFl6Vqg6piS83ZRuKJqXJQxOM52M5tNarT2XQ/pMvw8fOOlnhcOYvdZyje2PJ8rK/J64ZI729Q=="   # origin/main (CI)
  }
```

**Same version, two different hashes.** Both are legitimate regenerations — one produced by CI, one by
the local machine — because the recorded content hash is environment-specific (cache/feed
recompression), not a real package difference. Neither is "wrong."

### Why the cache-clearing playbook doesn't apply here

- It is not local corruption, so clearing caches changes nothing.
- `--force-evaluate` on the local machine produces the **local** hash, which (a) may match neither
  committed value and (b) creates churn: it would flip `main`'s hash on every promotion, and the
  *next* CI build would just regenerate it back. You'd be fighting the drift forever.
- Note: `RestorePackagesWithLockFile=true` **without** `RestoreLockedMode` is still enough to make a
  plain `dotnet build` fail with NU1403 here — the global cache content differed from the recorded
  hash, so the check fired even outside explicit `--locked-mode`.

### Resolution: keep the deployment lineage's hash, regenerate locally without committing

The lock file can only hold one hash, and under drift it validates for one environment at a time.
Pick the environment that is the **source of truth for deployment** (CI / `main`) and let local dev
work around it transiently:

```powershell
# During the merge, resolve the conflict to main's (CI-validated) hash:
git checkout --theirs -- packages.lock.json    # 'theirs' = the branch being merged in (origin/main)
git add packages.lock.json
git commit                                      # merge commit keeps CI's hash → zero promotion churn

# Build/verify locally without committing the local hash:
dotnet restore --force-evaluate                 # rewrites the lock to THIS machine's hash
dotnet build -c Release                         # succeeds (the package version is identical)
dotnet run  -c Release                          # generate + verify
git checkout HEAD -- packages.lock.json         # discard the local-hash drift; committed lock stays CI's
```

The package **version** is identical on both sides, so a force-evaluated local build is a fully valid
proof that the merged *code* builds — the hash is orthogonal to compilation. Committing CI's hash
keeps CI green and makes the eventual umbrella→main promotion a churn-free fast-forward; the only cost
is that local builds on the umbrella need a one-time `--force-evaluate` (same as `main` already does).

**Decision rule:** when a lock hash conflicts across branches, commit the hash of whichever
environment gates deployment; force-evaluate elsewhere and don't commit it. Only commit a *new* hash
(the single-environment path above) when the package genuinely changed, not when environments merely
disagree.
