---
title: "Pattern: Zero-Cost LLM in CI via GitHub Models + GITHUB_TOKEN"
description: "Use the built-in GITHUB_TOKEN with permissions: models: read to call GitHub Models from GitHub Actions — no API keys, no secrets, no cost"
entry_type: pattern
published_date: "2026-04-04"
last_updated_date: "2026-04-04"
tags: "github, ai, ci-cd, github-actions, patterns"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: "pattern-github-models-endpoint-migration"
---

## Discovery

While building a knowledge graph extraction pipeline that runs in GitHub Actions CI, we needed an LLM to extract entities and relations from Markdown documents. The typical approach requires an `LLM_API_KEY` stored as a repository secret. We discovered that GitHub Models supports native `GITHUB_TOKEN` authentication since April 2025 — eliminating the need for any separate API key.

## Root Cause

GitHub integrated its Models API with the existing `GITHUB_TOKEN` infrastructure used by GitHub Actions. This means any workflow can call LLMs with zero additional credential setup. The key requirement is a new permission scope: `models: read`.

## Solution

**Workflow YAML**:
```yaml
permissions:
  contents: write   # if you need to commit artifacts
  models: read      # required for GitHub Models API access

jobs:
  extract:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - name: Run LLM extraction
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          python tools/extract.py
```

**Python code**:
```python
from openai import OpenAI
import os

client = OpenAI(
    base_url="https://models.github.ai/inference",
    api_key=os.environ["GITHUB_TOKEN"]
)

response = client.chat.completions.create(
    model="openai/gpt-4o-mini",
    messages=[...],
    temperature=0,
    response_format={"type": "json_object"}
)
```

**Free tier limits (GPT-4o-mini)**:
- 150 requests/day
- 8,000 input tokens / 4,000 output tokens per request
- ~15-30 RPM

**Batching strategy for CI** (process 100 text chunks):
- Batch 3-5 chunks per request → ~20-30 API calls total
- Cache results by `(chunk_id, prompt_version, model)` to skip unchanged content
- Use incremental builds (git diff) to only process modified files

**Source**: [GitHub Blog — Actions token integration now GA in GitHub Models](https://github.blog/changelog/2025-04-14-github-actions-token-integration-now-generally-available-in-github-models/)

## Prevention

- Always add `permissions: models: read` — without it, GITHUB_TOKEN will not authenticate with the Models API
- Be aware of the 150 req/day limit for GPT-4o-mini — batch aggressively and cache
- Each model has independent daily limits (UserByModelByDay), so you can spread across models if needed
- For production workloads exceeding free limits, GitHub offers pay-as-you-go billing
