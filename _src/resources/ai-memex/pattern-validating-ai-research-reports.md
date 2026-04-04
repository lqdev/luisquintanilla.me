---
title: "Pattern: Systematic Validation of AI-Generated Research Reports"
description: "A methodology for fact-checking AI deep research reports by decomposing claims into parallel verification threads"
entry_type: pattern
published_date: "2026-04-04"
last_updated_date: "2026-04-04"
tags: "ai-collaboration, research, patterns"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: "pattern-github-models-endpoint-migration, pattern-pyoxigraph-serialize-not-to-json"
---

## Discovery

We received a ~660-line deep research report proposing a Markdown-to-Linked-Data knowledge bank on Azure. The report was comprehensive and well-structured, but we needed to validate its technical claims before building on it. Rather than reading and checking linearly, we decomposed the report into independent claim categories and verified them in parallel.

## Root Cause

AI deep research tools produce polished, confident documents that mix accurate information with subtle errors. The errors tend to cluster in specific categories:

1. **Stale API endpoints/versions** (the report referenced a deprecated GitHub Models endpoint)
2. **Hallucinated method names** (`.to_json()` doesn't exist in PyOxigraph)
3. **Missing constraints** (rate limits, storage limits not mentioned)
4. **Outdated defaults** (old auth patterns when better ones now exist)

These errors are especially dangerous because they're embedded in otherwise correct, well-cited content — making them hard to spot in a casual read.

## Solution

### Step 1: Decompose into verification categories

Read the full report and identify independent claim domains:
- **Infrastructure claims** (Azure SWA, Functions, pricing)
- **Library/API claims** (RDFLib, PyOxigraph, pySHACL)
- **Standards claims** (JSON-LD, SPARQL, SHACL, PROV-O, schema.org)
- **CI/CD patterns** (GitHub Actions, bot commits, secrets)
- **LLM provider claims** (endpoints, auth, rate limits, models)
- **Alternative approaches** (HuggingFace models, hybrid pipelines)

### Step 2: Launch parallel verification agents

For each category, spawn a focused research agent with:
- Specific claims to verify (quote the exact text)
- Instructions to check official docs, not just web summaries
- Request for evidence: URLs, version numbers, code examples

### Step 3: Cross-reference with targeted web searches

For critical claims (API endpoints, auth patterns), do additional targeted searches to resolve conflicts between agent findings.

### Step 4: Compile a corrections table

Categorize findings as:
- **❌ Critical** (code-breaking: wrong endpoints, missing auth, wrong API)
- **⚠️ Important** (won't crash but wrong: missing limits, outdated conventions)
- **✅ Validated** (confirmed correct with evidence)

### Step 5: Apply corrections in tracked commits

- Commit original report first (preserve history)
- Apply corrections in a second commit (clear diff of what changed)
- Add a "Validation Corrections" section documenting what changed and why

### Results from this session

The report was **~85% accurate** but had 5 critical bugs that would have caused runtime failures:
- Dead API endpoint
- Wrong model naming format
- Unnecessary API key (simpler auth available)
- Non-existent method call
- Missing permission scope

These were caught by parallel verification in ~5 minutes of wall-clock time.

## Prevention

- **Never build directly from an AI research report** without validation
- **Decompose and parallelize** — checking 6 categories simultaneously is faster than linear reading
- **Prioritize API/endpoint/auth claims** — these are the most likely to be stale
- **Check method signatures against official docs** — AI loves to hallucinate convenient APIs
- **Commit the original, then corrections** — future readers see exactly what was wrong and what was fixed
