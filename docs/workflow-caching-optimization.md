# Workflow Caching Optimization

## Overview

This document describes the dependency caching optimizations implemented across all GitHub Actions workflows to improve build performance and reduce workflow execution time.

## Changes Made

### 1. NuGet Package Lock File

- **File**: `packages.lock.json`
- **Purpose**: Provides deterministic package resolution for reproducible builds
- **Configuration**: Added `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to `PersonalSite.fsproj`

### 2. Workflow Updates

All workflows using .NET have been updated to leverage the built-in caching feature of `actions/setup-dotnet@v4`:

#### Updated Workflows:
1. **publish-azure-static-web-apps.yml** - Main deployment workflow
2. **stats.yml** - Post statistics workflow
3. **process-content-issue.yml** - Content processing workflow (4 jobs: note, response, bookmark, media)
4. **check-broken-links.yml** - Link validation workflow
5. **weekly-wrapup.yml** - Weekly summary workflow

#### Changes Applied to Each Workflow:

**Before:**
```yaml
- name: Setup .NET SDK 9.x
  uses: actions/setup-dotnet@v4
  with: 
    dotnet-version: '9.0.x'

- name: Install Dependencies
  run: dotnet restore

- name: Build project
  run: dotnet build
```

**After:**
```yaml
- name: Setup .NET SDK 9.x
  uses: actions/setup-dotnet@v4
  with: 
    dotnet-version: '9.0.x'
    cache: true
    cache-dependency-path: '**/packages.lock.json'

- name: Install Dependencies
  run: dotnet restore --locked-mode

- name: Build project
  run: dotnet build --no-restore
```

### 3. Additional Improvements

- **Updated action versions**: `stats.yml` updated from `actions/setup-dotnet@v1.9.0` to `v4` and `actions/checkout@v2` to `v4`
- **Locked mode restore**: Using `--locked-mode` ensures builds fail if dependencies don't match the lock file
- **Optimized builds**: Using `--no-restore` flag prevents redundant package restoration

## Expected Benefits

### Performance Improvements

1. **Faster Builds**: NuGet packages are cached between workflow runs, eliminating the need to download dependencies every time
2. **Reduced Network I/O**: Packages are restored from GitHub's cache instead of NuGet.org
3. **Lower Build Times**: On cache hits, dependency restoration can be reduced from 20-40 seconds to 2-8 seconds

### Reliability Improvements

1. **Deterministic Builds**: `packages.lock.json` ensures identical package versions across all builds
2. **Early Failure Detection**: `--locked-mode` will fail the build if dependencies change unexpectedly
3. **Reproducible Environments**: Lock file ensures dev and CI environments use the same package versions

### Resource Optimization

1. **Reduced NuGet.org Load**: Fewer package downloads reduce load on NuGet infrastructure
2. **Lower Bandwidth Usage**: Cached packages consume less bandwidth
3. **Faster Feedback**: Developers get build results faster due to reduced dependency resolution time

## How Caching Works

The `actions/setup-dotnet` action with `cache: true` will:

1. **Before restore**: Check for a cache hit based on the hash of `packages.lock.json`
2. **On cache hit**: Restore packages from GitHub Actions cache (very fast)
3. **On cache miss**: Download packages from NuGet.org and cache them for future runs
4. **Cache key**: Generated from the content hash of all `packages.lock.json` files matching the pattern

## Cache Behavior

- **Cache Location**: GitHub Actions cache storage (up to 10GB per repository)
- **Cache Lifetime**: Caches are automatically evicted after 7 days of no access
- **Cache Invalidation**: Automatically invalidated when `packages.lock.json` changes
- **Global Packages Folder**: Caches the `~/.nuget/packages` directory

## Maintenance

### Updating Dependencies

When updating NuGet packages:

1. Update package versions in `PersonalSite.fsproj`
2. Run `dotnet restore` to regenerate `packages.lock.json`
3. Commit the updated `packages.lock.json` file
4. The next workflow run will create a new cache with the updated dependencies

### Troubleshooting

If builds fail with NU1403 errors about missing packages:

1. Verify `packages.lock.json` is committed to the repository
2. Ensure `RestorePackagesWithLockFile` is set to `true` in the project file
3. Check that `cache-dependency-path` pattern matches the lock file location

## Monitoring

To monitor caching effectiveness:

1. Check workflow logs for cache hit/miss messages
2. Compare build times before and after caching implementation
3. Monitor the "Restore dependencies" step duration across workflow runs

## References

- [GitHub Actions: Caching dependencies](https://docs.github.com/en/actions/using-workflows/caching-dependencies-to-speed-up-workflows)
- [actions/setup-dotnet: Caching NuGet Packages](https://github.com/actions/setup-dotnet#caching-nuget-packages)
- [NuGet Lock Files](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies)
