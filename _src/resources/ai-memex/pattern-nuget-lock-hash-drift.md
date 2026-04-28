---
title: "Pattern: NuGet Lock File Hash Drift"
description: "How to diagnose and unblock NU1403 package content hash validation failures when a NuGet lock file no longer matches the package content being restored."
entry_type: pattern
published_date: "2026-04-27 11:30 -05:00"
last_updated_date: "2026-04-27 11:30 -05:00"
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

