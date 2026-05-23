---
title: "Pattern: Aspire Persistent-Volume Migrations"
description: "A 3-layer mental model for managing on-disk state across Aspire integrations — volume name lifecycle, app-internal schema migrations, and image-tag bumps — plus practical backup/swap/restore recipes."
entry_type: pattern
published_date: "2026-05-22 20:30 -05:00"
last_updated_date: "2026-05-22 20:30 -05:00"
tags: "dotnet, aspire, docker, devops, patterns"
related_skill: "write-ai-memex"
source_project: "CommunityToolkit.Aspire.Hosting.Jellyfin"
---

## Discovery

While shipping a Jellyfin hosting integration for the [Aspire Community Toolkit](https://github.com/CommunityToolkit/Aspire) ([PR #1353](https://github.com/CommunityToolkit/Aspire/pull/1353)), a natural follow-up came up:

> How do migrations work for persistent volumes?

It turns out Aspire's documentation answers this in three different places — `WithDataVolume` is in the AppHost docs, the app's own schema migrations are in the app's docs, and image pinning is in the integration-author docs. None of them talk to each other, and there's no one-pager that ties the lifecycle together. New integration authors (and users of their integrations) keep tripping on the same edges:

- "I renamed my AppHost project and lost all my data."
- "I bumped the image tag and the container crash-loops."
- "I started with a bind mount, now I want a real volume — how do I migrate?"

This entry captures the mental model that resolves all three.

## Root Cause

State persistence in an Aspire-orchestrated container app is **not one system**. It's three independent layers, each with its own lifecycle and its own failure mode:

| Layer | What it controls | Who owns it | Failure mode |
|-------|------------------|-------------|--------------|
| 1. Volume **identity** | The Docker volume name that Aspire generates and mounts into the container | Aspire (`VolumeNameGenerator`) | Identity changes → previous volume is orphaned, container starts with an empty disk |
| 2. **In-app** schema/data | Tables, files-on-disk layout, format versions inside the volume | The container app (e.g. Jellyfin's EF Core migrations) | Downgrade across a schema change → crash loop |
| 3. **Image** tag | Which version of the app is running and therefore which Layer-2 migrations exist | The integration author (`*ContainerImageTags.cs`) | Tag bump → forced Layer-2 migration, often forward-only |

A "migration" question almost always involves **more than one** of these layers. Treating them as one system is what causes people to lose data.

## Solution

Think about persistence as three layers and address them independently.

### Layer 1: Aspire volume name lifecycle

When you call `WithDataVolume()` *without arguments*, Aspire generates a name using [`VolumeNameGenerator`](https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting/Utils/VolumeNameGenerator.cs). The generated name is a hash of:

- The AppHost project's path on disk
- The resource name passed to `AddJellyfin("...")` (or equivalent)

This has a subtle but consequential implication: **renaming or moving the AppHost project changes the volume name**, leaving the old volume orphaned and starting the container with an empty disk. Same thing if you rename the resource.

**Mitigation:** always pass an explicit name if the data matters.

```csharp
// ❌ Fragile - tied to AppHost path + resource name hash
builder.AddJellyfin("jellyfin").WithDataVolume();

// ✅ Stable across moves, renames, and refactors
builder.AddJellyfin("jellyfin").WithDataVolume("jellyfin-data");
```

This applies to **every** Aspire integration with a `WithDataVolume`/`WithCacheVolume`/etc. method — Postgres, Redis, Elasticsearch, Jellyfin, anything.

### Layer 2: In-app schema and data migrations

This layer is whatever the containerized app does on startup. Aspire doesn't see it and doesn't manage it. Examples:

- **Jellyfin** runs EF Core migrations against its SQLite databases inside the data volume on startup. Forward-only. No rollback.
- **Postgres** loads existing `PGDATA`; major-version bumps require `pg_upgrade` (Aspire's `AddPostgres` won't do this for you).
- **Elasticsearch** rewrites index metadata when started against an older format and refuses downgrades.

The integration author can't control this layer — it's the app's behavior. What the integration *can* do is document the upgrade story and pin a sensible default image tag.

### Layer 3: Image tag bumps

The integration author exposes the version via a constants file:

```csharp
// src/CommunityToolkit.Aspire.Hosting.Jellyfin/JellyfinContainerImageTags.cs
internal static class JellyfinContainerImageTags
{
    public const string Registry = "docker.io";
    public const string Image = "jellyfin/jellyfin";
    public const string Tag = "10.11";
}
```

Bumping `Tag` is what *triggers* a Layer-2 migration the next time the container starts. The Layer-1 volume identity is unchanged, so the new image sees the old data — and that's exactly when the on-startup migration runs.

Convention across Aspire integrations:

- Pin to a major or major-minor (not `latest`) so users don't get surprise migrations
- Bump deliberately, in a versioned release of the integration package, with release notes
- Document the upgrade path in the integration README when the app has a breaking schema change

## Recipes

### Pre-upgrade backup

Before bumping `Tag` (Layer 3) on a production-like environment, snapshot the volume. Works for any integration that uses a named volume:

```bash
docker run --rm \
  -v jellyfin-data:/data \
  -v ${PWD}:/backup \
  alpine tar czf /backup/jellyfin-data-$(date +%F).tgz -C /data .
```

If Layer-2 migration fails after the upgrade, restore by reversing the tar:

```bash
docker volume create jellyfin-data-restored
docker run --rm \
  -v jellyfin-data-restored:/data \
  -v ${PWD}:/backup \
  alpine tar xzf /backup/jellyfin-data-2026-05-22.tgz -C /data
```

Then point the AppHost at the restored volume:

```csharp
builder.AddJellyfin("jellyfin").WithDataVolume("jellyfin-data-restored");
```

### Bind mount → named volume

Common when graduating from "dev folder on my laptop" to "real volume":

```csharp
// Was:
builder.AddJellyfin("jellyfin").WithDataBindMount("./data/jellyfin");

// Want:
builder.AddJellyfin("jellyfin").WithDataVolume("jellyfin-data");
```

Migrate the data **once**, before flipping the AppHost code:

```bash
docker volume create jellyfin-data
docker run --rm \
  -v $(pwd)/data/jellyfin:/source:ro \
  -v jellyfin-data:/dest \
  alpine sh -c "cp -a /source/. /dest/"
```

Then deploy the code change. The container will start against the populated volume and skip first-run setup.

### Named volume → bind mount (debugging)

Reverse direction, useful when you need to inspect the data with host tools:

```bash
docker run --rm \
  -v jellyfin-data:/source:ro \
  -v $(pwd)/data/jellyfin:/dest \
  alpine sh -c "cp -a /source/. /dest/"
```

## Prevention

- **Always name your volumes** if the data has any value: `WithDataVolume("explicit-name")`. The convenience of `WithDataVolume()` is a footgun for anything beyond throwaway demos.
- **Snapshot before bumping `Tag`** in any integration whose app has on-disk state. Forward-only migrations are the norm, not the exception.
- **Pin to major or major-minor** in `*ContainerImageTags.cs` — never `latest`. The integration's release version is the user's signal that a migration window is opening.
- **Document the upgrade story** in the integration's README when the app has a known breaking-change pattern (e.g. Postgres major bumps, Elasticsearch index format changes).
- **Use `ContainerLifetime.Persistent`** for stateful services so the container survives AppHost restarts and you're not constantly re-running Layer-2 migrations during local dev:

  ```csharp
  builder.AddJellyfin("jellyfin")
         .WithLifetime(ContainerLifetime.Persistent)
         .WithDataVolume("jellyfin-data");
  ```

## Worked example: the Jellyfin integration

The code we shipped in [PR #1353](https://github.com/CommunityToolkit/Aspire/pull/1353) demonstrates the integration-author side of all three layers:

- **Layer 1:** `JellyfinHostingExtension.WithDataVolume(name?, isReadOnly?)` — accepts an explicit name and falls back to `VolumeNameGenerator.Generate(...)` when omitted. Same shape for `WithCacheVolume`.
- **Layer 2:** Not the integration's job. Jellyfin's own EF Core migrations run on startup against the mounted `/config` directory.
- **Layer 3:** `JellyfinContainerImageTags.Tag = "10.11"` — pinned to a minor line, bumped only when we cut a new package version.

The same three-layer split applies to any Aspire integration that wraps a stateful container.
