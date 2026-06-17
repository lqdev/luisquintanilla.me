---
title: "Pattern: CI/CD Fallout a Byte-Identical Refactor Can't See — Out-of-Tree Consumers & Deploy Races"
description: "A god-module decomposition verified byte-identical locally still broke CI twice: standalone .fsx scripts that #r the compiled DLL failed at runtime on a module rename, and rapid PR merges raced the single Azure SWA deploy environment, canceling the latest commit."
entry_type: pattern
published_date: "2026-06-16 22:19 -05:00"
last_updated_date: "2026-06-16 22:19 -05:00"
tags: fsharp, dotnet, devops, ci-cd, azure, refactoring, gotcha, patterns
related_entries: pattern-god-module-decomposition-at-scale, pattern-fsharp-module-extraction-inference-flip, pattern-azure-swa-function-file-isolation, pattern-long-lived-umbrella-branch-merge-strategy
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

The [[pattern-god-module-decomposition-at-scale|god-module decomposition]] split `Builder.fs`,
`GenericBuilder.fs`, and `TextOnlyViews.fs` into cohesive modules under a strict **byte-identical
`_public/` contract** (`verify-baseline.ps1` SHA-256 hash-manifest diff; URL permanence is a hard
invariant). Local verification was green and the PR (#2480) merged clean.

It still broke CI/CD **twice** — in two places the byte-identical proof structurally cannot see.
The unifying lesson: *byte-identical `_public/` proves the generator's output is unchanged. It says
nothing about (a) other programs that consume the compiled artifact, or (b) how the deploy pipeline
behaves under concurrency.* Extend the safety net to both.

## Gotcha 1 — out-of-tree `.fsx` consumers break at CI *runtime*, not build time

The repo has standalone F# scripts that the CI pipeline runs *after* `dotnet run`, e.g.
`Scripts/identify-webmentions.fsx` (deploy workflow) and `Scripts/stats.fsx` (stats workflow).
They don't compile into the project — they reference the **already-built DLL**:

```fsharp
#r "../bin/Debug/net10.0/PersonalSite.dll"
// ...
let processor = GenericBuilder.ResponseProcessor.create()      // OLD qualified name
let feedData  = GenericBuilder.buildContentWithFeeds processor responseFiles
```

The decomposition lifted the per-type processors out of `GenericBuilder`'s nested submodules into
top-level `Processors/*.fs` modules, so `GenericBuilder.ResponseProcessor` became bare
`ResponseProcessor`. Every **in-project** call site was updated and the build was clean. But the
scripts live outside the project graph, so nothing rebuilt or rechecked them. They failed only when
CI executed them:

```
Scripts/identify-webmentions.fsx(27): error FS0039:
  The value, namespace, type or module 'ResponseProcessor' is not defined.
```

Build ✓, Generate website ✓, then the *next* step died (CI run `27659690669`). The fix is the same
de-qualification done in-project — but you have to know the scripts exist:

```fsharp
let processor = ResponseProcessor.create()                     // top-level now
let feedData  = GenericBuilder.buildContentWithFeeds processor responseFiles  // STAYS qualified
```

Subtlety worth internalizing: only the **lifted** names change. `GenericBuilder.buildContentWithFeeds`
and the `GenericBuilder.UnifiedFeeds` submodule remained in the core module and stay qualified. A
blind find-replace of `GenericBuilder.` would have broken those. (Fix shipped in PR #2484.)

### Root cause — the harness blind spot

`verify-baseline.ps1` runs `dotnet build` + `dotnet run` and diffs `_public/`. It **never loads the
`.fsx` scripts**, which is precisely the class of consumer a module rename breaks. Byte-identical
output is necessary but not sufficient: it validates the generator, not the generator's *clients*.

## Gotcha 2 — rapid merges race the single Azure SWA deploy environment

After the refactor, four follow-up PRs merged to `main` in quick succession. Two of them
(#2485 → `69e14e44`, #2486 → `dc9df27c`) landed **~4 seconds apart**. Each push triggers the deploy
workflow, and both deployed to the *same* Azure Static Web Apps production environment concurrently:

| Run | Commit | Result |
|---|---|---|
| 27662684459 | `69e14e44` (#2485) | ✅ success |
| 27662686264 | `dc9df27c` (#2486 — **latest `main`**) | ❌ `Deployment Canceled` |

Every pre-deploy step passed (build, generate, scripts, size check); the failure was Azure-side, at
the upload/poll step: `Status: Failed ... Deployment Failure Reason: Deployment Canceled`. The
counter-intuitive part: the run that got canceled was the one for the **latest commit**, so
`main`'s HEAD was left *undeployed* — the previous commit's output was live instead. Order is not
guaranteed when two deploys race; "newest wins" is not automatic.

Immediate recovery (nothing else in flight, so no re-race):

```powershell
gh run rerun 27662686264 --failed   # rebuilds + redeploys dc9df27c → success
```

### Root cause + durable fix

The deploy workflow had **no `concurrency:` group**, so concurrent runs raced for one environment.
A workflow-level guard makes the newest push cleanly supersede an older in-flight deploy:

```yaml
# .github/workflows/publish-azure-static-web-apps.yml
concurrency:
  group: azure-swa-deploy
  cancel-in-progress: true   # newest deploy wins; older in-flight one is canceled, not raced
```

With `cancel-in-progress: true` the loser is canceled *as a GitHub run* (clean, expected) rather than
surfacing as an Azure "Deployment Canceled" failure on the latest commit. (Shipped in PR #2487.) The
alternative `cancel-in-progress: false` serializes deploys instead — safer against mid-upload aborts,
but runs every intermediate deploy.

## Prevention

1. **Inventory out-of-tree consumers before any module rename/move.** Grep the whole repo (not just
   the project's compile graph) for the old qualified names — `.fsx`, docs, snippets, CI YAML.
   `rg "GenericBuilder\.\w+Processor"` would have found all three broken scripts in one shot.
2. **Make the verification harness typecheck the scripts CI runs.** After `dotnet build`, parse/
   typecheck each CI `.fsx` (e.g. `dotnet fsi --use:<script> ... ` or a no-op load) so a rename fails
   *locally*, not three steps into the deploy. Byte-identical `_public/` ⇒ generator unchanged;
   it does **not** ⇒ clients still compile.
3. **Give every single-environment deploy a `concurrency:` group.** Any pipeline that deploys to one
   shared target from `push: main` should serialize, or rapid merges will race and can strand the
   latest commit undeployed.
4. **When merging a burst of PRs, expect the deploy race** — either space the merges, or confirm the
   *final* deploy run (for the latest SHA) is the one that succeeded, not an earlier sibling.

## Takeaway

A "structural-only, byte-identical" change is the safest kind of refactor — and that safety label is
exactly why these two failures were surprising. The byte-identical contract is scoped to the
generated site. The compiled DLL's *other* consumers and the *deploy pipeline's* concurrency live
outside that scope, so they need their own guardrails: a consumer inventory + script typecheck for
the former, a `concurrency:` group for the latter.
