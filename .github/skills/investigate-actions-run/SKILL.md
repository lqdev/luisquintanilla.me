---
name: investigate-actions-run
description: Use when the user shares a GitHub Actions run or job URL, says a workflow failed or that nothing triggered, or asks whether a step such as the AT Protocol sync, ActivityPub delivery, or deploy actually ran in lqdev/luisquintanilla.me. Finds the failed or skipped step, maps it to the workflow file, and reports cause, evidence, and fix.
---

# Investigate a GitHub Actions run

This is read-only. Don't re-run, cancel, approve, or dispatch a workflow unless asked.

## Steps

1. Take the IDs from the URL: `/actions/runs/{run_id}/job/{job_id}`. If there is no URL, find the
   run first:

   ```powershell
   gh run list --repo lqdev/luisquintanilla.me --limit 10 --json databaseId,workflowName,event,headBranch,conclusion,createdAt,displayTitle
   ```

   Add `--workflow {file}.yml` or `--branch main` to narrow it.
2. Get the shape of the run, with every job and every step that didn't succeed:

   ```powershell
   $r = gh run view {run_id} --repo lqdev/luisquintanilla.me --json workflowName,event,headBranch,headSha,conclusion,createdAt,jobs | ConvertFrom-Json
   "$($r.workflowName) | $($r.event) | $($r.headBranch) | $($r.headSha.Substring(0,8)) | $($r.conclusion)"
   foreach ($j in $r.jobs) { "JOB $($j.name): $($j.conclusion) ($($j.databaseId))"; $j.steps | Where-Object conclusion -ne 'success' | ForEach-Object { "  step $($_.number) $($_.name): $($_.conclusion)" } }
   ```

   A failed step is usually followed by `skipped` steps; those are fallout, not causes. If `jobs`
   is empty, go to the first gotcha below.
3. Read the failure:

   ```powershell
   gh run view {run_id} --repo lqdev/luisquintanilla.me --log-failed | Select-String 'error|Error|exit code|No such file|not found' | Select-Object -Last 30
   ```

   The log names each line by job, but the step column often says `UNKNOWN STEP`. Use the step
   names from step 2. For one job: `gh run view --job {job_id} --repo lqdev/luisquintanilla.me --log`.
4. Open the workflow file under `.github/workflows/` and find the step. Read the workflow as it ran,
   not as it is now: `git show {headSha}:.github/workflows/{file}` (run `git fetch origin {headSha}`
   first if the commit is missing). Read its `on:` block and any `if:` on the job or step.
5. If the question is "did X run?", a `skipped` job or step with an `if:` is usually the answer.
   Quote the condition and the value that made it false.
6. If a file path is in the error, check whether the file exists at that commit and when it moved:
   `git log origin/main --oneline -3 -- {path}`.
7. Report in the format below. If you defer the fix, write it up as an issue.

## Gotchas

- A run with no jobs that ended in failure never started. `gh run view` blames "a workflow file
  issue", which may be wrong. Read the annotation on the run page with `web_fetch` on the run URL.
  Pull requests opened by `github-actions[bot]` from `content/issue-*` branches show "This workflow
  run required approval but was not approved before it expired." That is expected, not a bug.
- "No validation ran" is often the path filter. `validate-site-upgrade.yml` runs on pull requests
  and ignores `_src/**` and `projects/archive/**`, so a content-only pull request has no checks.
- The publish workflow (`publish-azure-static-web-apps.yml`) runs on every push to `main`, with no
  path filter. Its concurrency group has `cancel-in-progress: true`, so a `cancelled` publish
  followed by a newer `success` means a later merge replaced it.
- The AT Protocol sync is the job "Sync AT Protocol Records" (`sync_atproto_job`). It runs only on
  a push to `main` when the build staged at least one record. The "Check for AT Protocol staging"
  step in the build job sets those outputs and logs what it found. A skipped sync usually means
  nothing was staged; read that step's log, then `docs/atproto/README.md`, before calling it a bug.
- Scheduled checks such as `test-activitypub-deployment.yml` (Mondays 09:00 UTC) read files from
  the repository. When a file moves, they fail on the next schedule, often weeks after the change.
- Logs expire. If `--log` returns nothing, say so and work from the workflow at `headSha`.
- The publish workflow deploys the site and delivers to the Fediverse and Bluesky. Never trigger it
  to test a theory.

## Report format

```text
Run: {url}
Workflow and trigger: {workflowName}, {event}, {headBranch} at {short sha}
What happened: {one sentence}
Evidence: {the log lines or the if: condition, quoted}
Cause: {one or two sentences}
Fix: {what to change, and in which file and line; or "nothing to fix, by design"}
```
