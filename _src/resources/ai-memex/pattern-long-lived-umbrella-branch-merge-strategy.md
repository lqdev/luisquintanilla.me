---
title: "Pattern: Long-Lived Umbrella Branch with Merge-Commit Boundaries"
description: "A git workflow for shipping a multi-phase improvement program as small, reviewable PRs while keeping a single long-lived integration branch that survives across phases."
entry_type: pattern
published_date: "2026-06-01 15:30 -05:00"
last_updated_date: "2026-06-01 15:30 -05:00"
tags: "git, devops, workflow, patterns, lqdev-me"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: pattern-nuget-lock-hash-drift, codebase-context
---

## Discovery

A large, open-ended site-improvement program (docs hygiene, a `/tools` page, `/colophon` enhancements, text-only accessibility, automations, a now-playing widget, privacy patterns) needed to ship incrementally over many sessions without:

- One giant unreviewable PR,
- A swarm of tiny PRs straight to `main` that each leave `main` in a half-finished phase, or
- A feature branch that drifts so far from `main` it becomes a painful merge.

The work was organized into phases (α → β → δ → γ). The question was how to structure branches and merges so each unit of work is small and reviewable, yet phases land on `main` as coherent, validated units — and the integration branch keeps living so the *next* phase can start immediately.

## Root Cause

Two git merge facts make this work cleanly:

1. **Squash-merging into an integration branch** keeps that branch's history tidy (one commit per logical unit of work) while still allowing per-unit review.
2. **A merge commit at the integration→main boundary** keeps the integration branch's HEAD as an *ancestor* of the new `main`. That ancestry is exactly what lets the integration branch **fast-forward** back onto `main` afterward — so it ends each phase at `0 0` divergence and is immediately ready for the next phase. (A squash merge at the boundary would *rewrite* the commits, breaking ancestry and forcing the long-lived branch to be rebased or recreated.)

If you squash at the boundary instead, the umbrella branch and `main` diverge by construction (umbrella still has the original commits; main has a new squashed commit with no shared ancestry), and you can no longer fast-forward — defeating the "keep it alive" goal.

## Solution

**Topology:** one long-lived umbrella branch (e.g. `feature/site-improvements-2026-05`), short-lived per-unit work branches off it, and `main`.

**Per unit of work:**
1. Branch off the umbrella (NOT `main`): `git checkout -b improve/<slug>`.
2. Make atomic commits; if F# touched, `dotnet build` then `dotnet run` to verify the site generates.
3. Open a PR with **base = umbrella**, review, then **squash-merge into the umbrella** and delete the work branch.

**At each phase boundary:**
4. Open a PR with **base = `main`, head = umbrella**.
5. Merge it with a **merge commit** — NOT squash:
   ```bash
   gh pr merge <n> --merge
   ```
6. Fast-forward the umbrella back onto `main` so it ends even (`0 0`):
   ```bash
   git checkout main && git pull --ff-only
   git checkout feature/site-improvements-2026-05
   git merge origin/main --ff-only
   git push
   git rev-list --left-right --count origin/main...feature/site-improvements-2026-05   # expect: 0  0
   ```
7. **Keep the umbrella branch alive** for the next phase. Do not delete it.

**Handling `main` drift during a phase:** `main` routinely advances with unrelated content commits while a code phase is in flight. As long as those commits don't touch the same files (content vs. code), the boundary merge stays `CLEAN`/`MERGEABLE` and reconciles automatically — no mid-phase rebase needed. Verify before opening the boundary PR:
```bash
git log --oneline origin/main..feature/site-improvements-2026-05   # what the boundary will carry
git log --oneline feature/site-improvements-2026-05..origin/main   # what main added (expect pure content)
```

**Body discipline:** PR bodies, issue bodies, and commit messages are authored in files and passed via `--body-file` / `git commit -F`, then the temp file is removed (avoids inline-quoting and encoding pitfalls; see the UTF-8 note below).

## Prevention

- **Always `--merge` (never `--squash`) at the umbrella→main boundary.** Squashing there is the single mistake that breaks the "keep-alive + fast-forward" property.
- **Branch every work unit off the umbrella, never off `main`** — otherwise the unit misses in-flight phase context.
- After each boundary, assert `0 0` divergence as a guardrail; a non-zero count means the fast-forward didn't happen (usually because someone squashed the boundary or `main` got a conflicting commit).
- This pattern is worth the ceremony when work spans **multiple phases over multiple sessions**. For a single small change, branch off `main` directly — the umbrella adds no value.

### PowerShell + `gh` UTF-8 gotcha (encountered repeatedly in this workflow)

When driving `gh` from PowerShell, em-dashes and other non-ASCII corrupt into mojibake unless you set the console encoding **before** calling `gh`:
```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$env:GH_PAGER = ""
```
When editing an existing issue/PR body, fetch it as JSON, splice with `.Replace()`, and write back with an explicit UTF-8-without-BOM encoder:
```powershell
$body = (gh issue view <n> --json body | ConvertFrom-Json).body
[System.IO.File]::WriteAllText($path, $body, (New-Object System.Text.UTF8Encoding $false))
```
