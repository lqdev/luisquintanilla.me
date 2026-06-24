---
title: "Pattern: Composite Action for Shared CDN Upload (Issue-Driven Publishing)"
description: "Factor duplicated GitHub Actions upload steps into a local composite action so multiple issue-driven publishing jobs share one CDN-upload mechanism while authoring stays per content type."
entry_type: pattern
published_date: "2026-06-24 15:05 -05:00"
last_updated_date: "2026-06-24 15:05 -05:00"
tags: devops, automation, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
related_entries: pattern-rollback-safe-media-organizer-scripts, pattern-standalone-content-type-search-not-firehose
---

## Discovery

The site already had one issue-driven publishing flow: a **📷 Post Media** GitHub issue
triggers a `process-media` job that installs `uv`, creates a venv, installs `boto3`, and
runs `.github/scripts/upload_media.py` — which downloads drag-dropped attachments, uploads
them to Linode S3 (the CDN), and **rewrites the content file in place** into `:::media`
blocks with permanent CDN URLs.

Adding a second flow (**🏷️ Post For Sale Item** → `process-marketplace`) would have
copy-pasted those four upload steps verbatim. Instead, the **upload mechanism** was
extracted into a reusable **local composite action**, while **authoring stays per content
type** (each type keeps its own issue template + F# frontmatter generator).

## Root Cause / Problem

The upload script is already **content-agnostic** — `upload_media.py <content-file>`
operates on any file, not just `_src/media`. But its *invocation* (uv install, venv, deps,
run) was embedded inline in a single job. Duplicating it per content type guarantees drift:
the day you bump `boto3` or switch `uv` versions, you must remember every copy. The right
seam is "shared upload mechanism, scoped authoring."

## Solution

A local composite action at **`.github/actions/upload-to-cdn/action.yml`** wraps the four
steps. Inputs: a `content-file` path plus the five Linode secrets (composite actions
**cannot read repo secrets directly** — the caller must pass them via `with:`).

```yaml
runs:
  using: "composite"
  steps:
    - uses: astral-sh/setup-uv@v4
      with: { version: "latest" }
    - shell: bash
      run: uv venv
    - shell: bash
      run: uv pip install boto3==1.34.0 botocore==1.34.0 requests
    - shell: bash
      env:
        LINODE_STORAGE_ACCESS_KEY_ID: ${{ inputs.access-key-id }}
        # ...remaining 4 secrets mapped from inputs...
      run: uv run python .github/scripts/upload_media.py "${{ inputs.content-file }}"
```

Each caller job writes its content to a temp file, then calls the action:

```yaml
- name: Write listing content to file
  run: printf '%s' "${{ steps.extract-data.outputs.content }}" > /tmp/marketplace_content.txt
- name: Upload media to CDN
  uses: ./.github/actions/upload-to-cdn
  with:
    content-file: /tmp/marketplace_content.txt
    access-key-id: ${{ secrets.LINODE_STORAGE_ACCESS_KEY_ID }}
    # ...remaining 4 secrets...
```

`upload_media.py` is **unchanged**. The per-type authoring (frontmatter + body) stays in a
dedicated F# script (`Scripts/process-marketplace-issue.fsx`, mirroring
`process-media-issue.fsx`), so each content type controls its own schema.

## Key Components

- **`.github/actions/upload-to-cdn/action.yml`** — the shared composite action.
- **`.github/scripts/upload_media.py`** — content-agnostic uploader, reused as-is.
- **`.github/workflows/process-content-issue.yml`** — `process-media` and
  `process-marketplace` jobs each `uses: ./.github/actions/upload-to-cdn`.
- **`Scripts/process-*-issue.fsx`** — per-type frontmatter generators (the "authoring" half).

## Gotchas

- **Secrets cross the boundary as inputs.** Composite actions have no `secrets` context;
  map each secret to an input in every caller's `with:`.
- **Every `run:` step needs an explicit `shell:`.** Unlike job steps, composite `run`
  steps have no default shell.
- **Local composite actions require checkout first.** `uses: ./.github/...` only resolves
  after `actions/checkout` has run in the job.
- **`uv venv` persists across the composite's steps** because they share the job working
  directory — same behavior as when the steps were inline.
- **Behavior-preserving by construction.** Because the Python script and the per-type F#
  script are untouched, the refactor can't change output — a stale local test script
  (`test-media-workflow.sh`, not wired into CI) was *not* a valid gate. Validate with YAML
  parsing + a dry-run of the new generator instead.

## Prevention

Whenever a second job would copy CI steps that wrap a content-agnostic tool, stop and
extract a composite action keyed on the *mechanism*, not the *content*. Keep authoring
(schemas, frontmatter) in per-type scripts. This pairs naturally with
[[pattern-standalone-content-type-search-not-firehose]]: one adds the content type to the
site generator, the other gives it a zero-duplication publishing pipeline.
