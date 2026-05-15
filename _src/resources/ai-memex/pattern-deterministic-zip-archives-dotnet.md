---
title: "Pattern: Deterministic ZIP Archives in .NET"
description: "Producing byte-identical .zip output across builds with System.IO.Compression — three layered gotchas around ZipArchiveEntry.LastWriteTime, set-before-Open ordering, Dictionary iteration, and the 1980 DOS-time floor."
entry_type: pattern
published_date: "2026-05-14 23:05 -05:00"
last_updated_date: "2026-05-14 23:05 -05:00"
tags: dotnet, fsharp, zip, determinism, reproducible-builds, gotcha, archives
related_entries: pattern-deterministic-first-intelligent-second, pattern-nuget-lock-hash-drift, pattern-garmin-gpx-waypoint-strictness
source_project: lqdev-me
---

## Context

I added [Blog Archive Format](https://blogarchive.org/) (`.bar`) export to my F# static site generator. A `.bar` file is just a ZIP containing `index.html` + `feed.json` + bundled `uploads/*`. The whole point of the format is **portability and diffability** — you should be able to compare two backups and see only the content that actually changed.

First implementation generated each archive with `System.IO.Compression.ZipArchive` in `Create` mode, the obvious-looking way:

```fsharp
use archiveStream = File.Open(zipPath, FileMode.CreateNew, FileAccess.Write)
use archive = new ZipArchive(archiveStream, ZipArchiveMode.Create)

let indexEntry = archive.CreateEntry("index.html")
use indexWriter = new StreamWriter(indexEntry.Open())
indexWriter.Write(html)
```

Two consecutive `dotnet run` invocations with **zero content changes** produced different SHA-256 hashes for every `.bar`:

```
posts.bar     BD7FFF1961030095...   (build A)
posts.bar     372342977F9EF23F...   (build B)  ← different
```

That defeats the format's whole reason for existing.

## Problem

`ZipArchive.CreateEntry(name)` defaults `ZipArchiveEntry.LastWriteTime` to `DateTimeOffset.Now` at the moment of creation. The ZIP central directory stores this per-entry, so wall-clock drift between builds → different bytes → different hash → useless `git diff` / `rsync` / `sha256sum`-based change detection on the published artifact.

There are three more traps stacked on top of the obvious one — I tripped over each in turn.

## Solution

```fsharp
// 1. ZIP DOS-time minimum is 1980-01-01; clamp anything earlier.
let zipEpoch = DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
let clampForZip (d: DateTimeOffset) = if d < zipEpoch then zipEpoch else d

// 2. Derive a stable timestamp from CONTENT, not the build clock.
let mostRecentDate =
    archiveEntries
    |> List.tryHead                                            // already sorted desc
    |> Option.map (fun e -> DateTimeOffset.Parse e.Item.Date)
    |> Option.defaultValue zipEpoch
    |> clampForZip

use archiveStream = File.Open(zipPath, FileMode.CreateNew, FileAccess.Write)
use archive = new ZipArchive(archiveStream, ZipArchiveMode.Create)

// 3. Set LastWriteTime BEFORE Open() — see "Gotcha #2" below.
let indexEntry = archive.CreateEntry("index.html")
indexEntry.LastWriteTime <- mostRecentDate
do
    use indexWriter = new StreamWriter(indexEntry.Open())
    indexWriter.Write(buildIndexHtml ...)

let feedEntry = archive.CreateEntry("feed.json")
feedEntry.LastWriteTime <- mostRecentDate
do
    use feedWriter = new StreamWriter(feedEntry.Open())
    feedWriter.Write(generateFeedJson ...)

// 4. Sort by in-archive path so write order is independent of Dictionary semantics.
uploadPathBySource
|> Seq.sortBy (fun kvp -> kvp.Value)
|> Seq.iter (fun kvp ->
    let uploadEntry = archive.CreateEntry(kvp.Value)
    uploadEntry.LastWriteTime <- mostRecentDate
    use entryStream = uploadEntry.Open()
    use sourceStream = File.OpenRead(kvp.Key)
    sourceStream.CopyTo(entryStream))
```

Verified with `Get-FileHash`: two consecutive builds now produce byte-identical archives across `posts.bar`, `notes.bar`, `responses.bar`, and `all.bar`.

## The four gotchas

### Gotcha #1 (primary) — Default `LastWriteTime` is wall-clock

The .NET BCL's `ZipArchiveEntry.LastWriteTime` getter docs hint at this but the failure mode is silent — your archive is "valid", it just isn't reproducible. Set it explicitly to a value derived from the **content**, not the **build environment**. Common choices:

- Most-recent content `mtime` (what I used)
- Source commit timestamp (`git log -1 --format=%cI`)
- A fixed epoch like `2000-01-01` if you don't care about timestamps at all
- `SOURCE_DATE_EPOCH` if you're following the [reproducible-builds.org](https://reproducible-builds.org/) spec

### Gotcha #2 (the trap that bit me) — Set BEFORE `Open()` in Create mode

My first attempt set the timestamp after writing content, which seemed natural:

```fsharp
let entry = archive.CreateEntry("index.html")
do
    use w = new StreamWriter(entry.Open())
    w.Write(html)
entry.LastWriteTime <- mostRecentDate   // ← throws
```

Result:

```
System.IO.IOException: Cannot modify entry in Create mode after entry has been opened for writing.
   at System.IO.Compression.ZipArchiveEntry.set_LastWriteTime(DateTimeOffset value)
```

In `ZipArchiveMode.Create` the central-directory record is finalized when the entry stream is opened (or closed); after that, metadata is frozen. You **must** set `LastWriteTime` between `CreateEntry()` and `Open()`. (This restriction does not apply in `Update` mode, which buffers the whole archive in memory and rewrites on dispose — but `Update` is much slower and uses much more memory.)

### Gotcha #3 — Dictionary iteration order is not contractually stable

The image-bundling code dedupes uploads via `Dictionary<sourcePath, archivePath>` and then iterates to write entries:

```fsharp
uploadPathBySource |> Seq.iter (fun kvp -> ...)
```

In current CoreCLR, `Dictionary<TKey,TValue>` enumeration order matches insertion order in practice — but the BCL contract explicitly states it's undefined and may change. For determinism, sort by the in-archive path before writing:

```fsharp
uploadPathBySource |> Seq.sortBy (fun kvp -> kvp.Value) |> Seq.iter ...
```

### Gotcha #4 — DOS-time floor is 1980-01-01

The ZIP file format uses MS-DOS date/time encoding (16-bit date + 16-bit time, year stored as `year - 1980`). `DateTimeOffset.MinValue` will throw `ArgumentOutOfRangeException` when assigned. Always clamp:

```fsharp
let zipEpoch = DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
let safe = if d < zipEpoch then zipEpoch else d
```

The 2-second time resolution is also worth knowing — ZIP can't represent sub-2-second precision. If your "stable timestamp" comes from a higher-resolution source, two values within the same 2-second window will already collapse to the same DOS time, which is fine for determinism but surprising if you're debugging.

## When this matters

- **Backup formats** (BAR, EPUB, OOXML, ODF — all ZIPs) where users diff or hash to detect change
- **CI artifacts** where caching keys depend on archive hash
- **Reproducible builds** following [reproducible-builds.org](https://reproducible-builds.org/) — same source must produce same bytes regardless of when/where built
- **Content-addressed storage** (IPFS, Git LFS) where the hash IS the address
- **Forensics / archival** where bit-identical replication is part of the trust model

## When it doesn't matter

- One-off archives created and consumed in the same process
- Backups stored to dated paths (`backup-2026-05-14.zip`) where filename, not content hash, is the identifier
- Cases where you actively WANT the timestamp to reflect build time (less common than you'd think)

## Verification

The cheap regression test that catches all four gotchas in one shot:

```powershell
dotnet run
$h1 = (Get-FileHash _public/archive/posts.bar -Algorithm SHA256).Hash
dotnet run
$h2 = (Get-FileHash _public/archive/posts.bar -Algorithm SHA256).Hash
if ($h1 -eq $h2) { "deterministic" } else { "NON-DETERMINISTIC" }
```

Worth wiring into CI on any project that ships ZIP-based artifacts as part of its public surface.

## Citations

- `Builder.fs:511-548` — `buildBarArchive` post-fix
- PR [#2386](https://github.com/lqdev/luisquintanilla.me/pull/2386), commit `3a7cf9c8`
- Issue [#2385](https://github.com/lqdev/luisquintanilla.me/issues/2385) — BAR format spec requiring portability
- [Blog Archive Format spec](https://blogarchive.org/)
- [.NET docs — ZipArchiveEntry.LastWriteTime](https://learn.microsoft.com/dotnet/api/system.io.compression.ziparchiveentry.lastwritetime)
- [reproducible-builds.org](https://reproducible-builds.org/)

## Related

- [[pattern-deterministic-first-intelligent-second]] — the broader principle this is one mechanical instance of
- [[pattern-nuget-lock-hash-drift]] — sibling .NET reproducibility gotcha (NuGet lock file content hashes)
- [[pattern-garmin-gpx-waypoint-strictness]] — sibling "portable archive format has stricter rules than your generator assumes" gotcha, also surfaced via PR validation
