---
title: "Pattern: Activate Quote POSSE for Commented ATProto Reshares"
description: "A Bluesky target with authored commentary is a quote-post and requires the quote-specific POSSE track, not the ordinary reshare track."
entry_type: pattern
published_date: "2026-09-16 13:05 -05:00"
last_updated_date: "2026-09-16 13:33 -05:00"
tags: "fsharp, dotnet, atproto, bluesky, posse, devops, patterns"
related_skill: "write-ai-memex"
source_project: "lqdev.me"
---

## Discovery

A response at `_src/responses/using-atproto-from-microblog-screencast-2026-09-16.md` used:

```yaml
targeturl: https://bsky.app/profile/manton.org/post/3mvnhiztwqn2t
response_type: reshare
```

and included authored commentary. The site page deployed successfully, but the response did not
appear in the author's Bluesky feed. GitHub Actions run `35130774236` staged 11 ordinary reshare
records while reporting quote staging as absent because the quote mode was disabled.

The target post itself was valid: `app.bsky.feed.getPosts` resolved it to
`at://did:plc:pko7wbcggok753hnvndxh3ni/app.bsky.feed.post/3mvnhiztwqn2t` with a CID.

## Root Cause

Response POSSE classification is target-driven, not based only on `response_type`. A strict
`bsky.app/profile/{actor}/post/{rkey}` target is parsed as a native `AtProtoPost`. The body
classifier then treats any top-level non-blockquote content as authored commentary:

```fsharp
let target, kind = classifyResponse r.Metadata.TargetUrl (responseBody r)
```

That produces `QuotePost` for this response. The eligibility predicate routes it through the
quote-specific flag and cutoff:

```fsharp
| QuotePost -> useAtProtoQuotePostsSync && d >= quotePostsActivationCutoff
```

The active ordinary-web reshare flag only applies to `LinkPost`. At the time of investigation,
`useAtProtoQuotePostsSync = false` and `quotePostsActivationCutoff = DateTimeOffset.MaxValue`,
so no quote staging file was produced and the quote sync job was skipped.

## Solution

For this response's intended semantics—commentary attached to Manton's native Bluesky post—enable
the quote track and choose the exact forward-only activation instant:

```fsharp
let useAtProtoQuotePostsSync = true
let quotePostsActivationCutoff =
    DateTimeOffset(2026, 9, 16, 12, 48, 0, TimeSpan.FromHours -5.0)
```

No workflow plumbing is required. The existing workflow detects
`_public/api/data/atproto/quotes`, uploads the staging artifact, and runs
`Scripts/sync-atproto.fsx --dir atproto-quotes --collection app.bsky.feed.post --commit`.
The sync script resolves the target handle and fills the quote record's `embed.record` subject
with the target URI and CID before writing it.

If a bare native repost is intended instead, the response body must contain no authored
commentary, and the separate repost flag/cutoff must be activated. That is a different Bluesky
record type and should not be inferred from the generic `response_type: reshare` value.

## Validation and Restore Gotcha

The first local validation attempt failed before compilation with `NU1403`:
`FSharp.Core.10.1.401` in the local NuGet cache had a different package hash than the committed
`packages.lock.json`. The lockfile expected
`IGqjL9U8pQl7CaCpFP3f5slZMduE8c7ZBx7HycAQ9dc1tawyniNyw7Fh6L2cShIoQDcaDL+S8/kokhWs8WwnBQ==`,
while the cached `.nupkg.sha512` contained
`WdL2Eq+hgaQqsQ+bQEtkcaVDc0aVUktTHO3tW8O+AzXTPnEExKwiyOpIdx7I5Rxs5nWQBW6VTf2ryRi7IuQgBA==`.
This affected both `dotnet run` and `dotnet build --no-restore`; it was a local package-cache
integrity mismatch, not an error in the quote activation.

Because `PersonalSite.fsproj` enables `RestorePackagesWithLockFile`, simply setting
`RestorePackagesWithLockFile=false` failed with `NU1005` while the repository lockfile existed.
The non-invasive validation workaround was to use an isolated lock path:

```powershell
$tempLock = Join-Path $env:TEMP 'lqdev-no-lock.json'
dotnet restore --force-evaluate `
  -p:RestorePackagesWithLockFile=false `
  -p:NuGetLockFilePath=$tempLock
dotnet build --no-restore
dotnet run --no-build
```

This restored successfully, produced one quote staging record, and left `packages.lock.json`
unchanged. Do not regenerate or commit the lockfile just to work around a machine-local cache
mismatch; verify `git diff -- packages.lock.json` and prefer the isolated restore path. The normal
GitHub Actions deployment restore remained healthy.

## Prevention

Before activating a response track, classify the pair of `targeturl` and Markdown body with
`AtProtoResponseMapping.classifyResponse`. Use this routing table:

| Target | Body | Native record |
|---|---|---|
| Ordinary web URL | Any | External link post |
| Native ATProto post | Authored commentary | Quote-post |
| Native ATProto post | Only blockquotes/empty | Repost |

Keep each mode's flag and forward-only cutoff in the same change. Because the cutoff comparison
is inclusive, inspect same-minute responses before setting it. After deployment, verify both the CI
staging count and the public Bluesky feed/API; a successful static-site deployment alone does not
prove that a native record was written.
