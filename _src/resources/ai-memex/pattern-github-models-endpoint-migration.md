---
title: "Pattern: GitHub Models Endpoint Migration (Azure → github.ai)"
description: "The GitHub Models API migrated from models.inference.ai.azure.com to models.github.ai/inference in 2025 — old endpoint is dead and model naming changed to publisher/model format"
entry_type: pattern
published_date: "2026-04-04"
last_updated_date: "2026-04-04"
tags: "github, ai, api, patterns"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: "pattern-github-models-zero-cost-ci-llm"
---

## Discovery

While validating a deep research report for a Markdown-to-Linked-Data knowledge bank project, we found the report referenced `https://models.inference.ai.azure.com` as the GitHub Models API endpoint. This endpoint was deprecated in July 2025 and stopped responding entirely in October 2025.

The report also used bare model names like `"gpt-4o-mini"` — these now 404 because the API requires `publisher/model` format.

## Root Cause

GitHub Models was initially hosted on Azure infrastructure and used Azure-style model naming. In mid-2025, GitHub migrated to its own domain (`models.github.ai`) with a new naming convention. This is a common pattern with platform services — initial launches use shared infrastructure before graduating to dedicated endpoints.

The change is documented in the GitHub Blog changelog but is easy to miss if you're working from older tutorials, cached documentation, or AI-generated research reports that trained on pre-migration data.

## Solution

**Endpoint**: `https://models.github.ai/inference`

**Model naming**: Use `publisher/model` format:
- `openai/gpt-4o-mini` (not `gpt-4o-mini`)
- `openai/gpt-4o` (not `gpt-4o`)

**Python SDK (OpenAI-compatible)**:
```python
from openai import OpenAI
import os

client = OpenAI(
    base_url="https://models.github.ai/inference",
    api_key=os.environ["GITHUB_TOKEN"]
)

response = client.chat.completions.create(
    model="openai/gpt-4o-mini",
    messages=[{"role": "user", "content": "Hello"}],
    temperature=0
)
```

**Sources**:
- [GitHub Blog — Deprecation of Azure endpoint](https://github.blog/changelog/2025-07-17-deprecation-of-azure-endpoint-for-github-models/)
- [GitHub Docs — REST API for Models Inference](https://docs.github.com/en/rest/models/inference)

## Prevention

- Always check the current GitHub Models documentation before starting a project — endpoints may have changed
- AI-generated research reports (including from deep research tools) may reference stale endpoints — validate API URLs independently
- Pin the endpoint URL in a single config constant so migration is a one-line change
