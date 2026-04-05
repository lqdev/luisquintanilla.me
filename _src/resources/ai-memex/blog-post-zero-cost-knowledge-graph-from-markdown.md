---
title: "Building a Zero-Cost Knowledge Graph from Markdown with AI"
description: "How we built a complete Markdown → LLM → RDF → SPARQL pipeline running entirely on free-tier services: GitHub Models, GitHub Actions, and Azure Static Web Apps"
entry_type: blog-post
published_date: "2026-04-04 23:33 +00:00"
last_updated_date: "2026-04-04 23:33 +00:00"
tags: "python, azure, devops, architecture, ai-collaboration, web, api"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: "pattern-github-models-zero-cost-ci-llm, pattern-github-models-endpoint-migration, pattern-azure-swa-function-file-isolation, pattern-validating-ai-research-reports, pattern-pyoxigraph-serialize-not-to-json"
---

# Building a Zero-Cost Knowledge Graph from Markdown with AI

## Observation

What if you could write a Markdown article, push it to a Git repo, and have a CI pipeline automatically extract a machine-queryable knowledge graph — entities typed with schema.org, relationships as RDF triples, and a live SPARQL endpoint — all for $0/month?

We built exactly that. In a single session, we went from a research report to a fully deployed, working system. The result: push a Markdown file, and minutes later you can query "what entities does this article mention?" via standard SPARQL — with real answers, typed data, and provenance tracking.

## Context

The idea started with a deep research report proposing a "Markdown-LD Knowledge Bank." Before writing a single line of code, we validated every claim in that report — launching parallel research agents to fact-check Azure pricing, GitHub Models endpoints, Python library APIs, and W3C standards. This turned out to be critical: **we found 5 bugs in the original report**, including a deprecated API endpoint that would have caused silent failures in production (see [[pattern-validating-ai-research-reports]]).

### The Architecture

```
content/*.md → GitHub Actions CI
                 ├── Chunk (split at H1/H2, ~750 tokens, sha256 IDs)
                 ├── Extract (GitHub Models GPT-4o-mini, batched 3-5 chunks)
                 ├── Post-process (canonicalize entities, dedup, add provenance)
                 ├── Validate (SHACL shapes)
                 └── Output → graph/*.jsonld + *.ttl
                                ↓
                        Azure Static Web Apps (Free)
                          ├── Static site (HTML)
                          ├── Graph files (JSON-LD, Turtle)
                          └── /api/sparql (RDFLib SPARQL endpoint)
```

### The Stack (All Free Tier)

| Component | Service | Cost |
|-----------|---------|------|
| LLM extraction | GitHub Models (GPT-4o-mini via GITHUB_TOKEN) | $0 |
| CI/CD | GitHub Actions | $0 |
| Hosting + API | Azure Static Web Apps Free | $0 |
| SPARQL engine | RDFLib (pure Python, ~615KB) | $0 |
| Validation | pySHACL | $0 |
| **Total** | | **$0/month** |

## Reflection

### What Worked Remarkably Well

**1. GitHub Models as a CI-native LLM.** The discovery that `GITHUB_TOKEN` natively authenticates with GitHub Models (since April 2025) was a game-changer. No API keys to manage, no secrets to rotate, no billing to worry about. Add `permissions: models: read` to your workflow YAML and you have free LLM access in CI. See [[pattern-github-models-zero-cost-ci-llm]].

**2. Validation-first development.** By validating the research report before coding, we avoided building on top of 5 bugs. The most dangerous was the deprecated GitHub Models endpoint (`models.inference.ai.azure.com` → `models.github.ai/inference`) — this would have caused a completely silent failure in CI since the old endpoint returns valid HTTP responses but is non-functional. See [[pattern-github-models-endpoint-migration]].

**3. Deterministic chunking.** Using sha256 hashes of normalized text as chunk IDs means the cache is content-addressed. If you edit paragraph 3 of an article, only that chunk gets re-extracted — the other chunks hit cache. This is critical when you have 150 requests/day on the free tier.

**4. RDFLib as a serverless SPARQL engine.** Pure Python, ~615KB, cold starts in under a second on Azure Functions. It just works. We initially considered PyOxigraph (Rust-based, faster), but RDFLib's built-in JSON-LD support since v6.0.1 made it the obvious choice for a serverless deployment. See [[pattern-pyoxigraph-serialize-not-to-json]].

### What Bit Us

**1. Azure SWA function file isolation.** The SPARQL function initially returned empty results — valid JSON, zero rows. No errors anywhere. Turns out Azure SWA deploys functions in a separate sandbox from static content. The function literally couldn't see the graph files sitting in the sibling `graph/` directory. Fix: copy TTL files into `api/data/` during CI. See [[pattern-azure-swa-function-file-isolation]].

**2. The "edit tool vs long lines" problem.** The research report had lines exceeding 400 characters. The edit tool consistently failed to match them. Workaround: used PowerShell `Add-Content` to append new sections, and the edit tool for shorter in-body corrections. A good reminder that tooling has limits and creative workarounds matter.

### The AI-Human Collaboration Dynamic

This project showcased a pattern I keep seeing: **AI excels at breadth, humans excel at judgment.**

- AI launched 6 parallel research agents to validate claims across Azure docs, GitHub changelogs, PyPI, W3C specs — breadth no human could match in the same time.
- Human decided which Azure subscription to use, whether the repo should be public, and that PII shouldn't appear in committed docs — judgment calls that require context AI doesn't have.
- AI wrote 30 tests, 5 pipeline modules, 2 CI workflows, and provisioned 3 cloud resources in a single session — execution speed that would take a human days.
- Human caught that the SPARQL query docs were missing from the README — a usability gap that's obvious when you're the one trying to use the system.

The best results came from the human saying "THINK DEEP" and the AI taking that seriously — not just generating code, but fact-checking its own research report before building on it.

## Takeaway

**You can build a real, production-quality knowledge graph pipeline entirely on free-tier services.** The key insights:

1. **Validate before you build.** AI-generated research reports contain plausible-sounding bugs. Parallel fact-checking caught 5 of them before they became code.

2. **GitHub Models + GITHUB_TOKEN is an underappreciated superpower.** Free LLM access in CI, zero credential management. More projects should use this.

3. **Content-addressed caching is mandatory** when your LLM budget is 150 requests/day. Make the cache key deterministic: `(chunk_hash, prompt_version, model)`.

4. **RDFLib is the right SPARQL engine for serverless.** Pure Python, small, built-in JSON-LD. Don't reach for Rust-based alternatives unless you've proven you need the performance.

5. **Azure SWA's function isolation is a gotcha.** If your function needs data files, they must be inside `api/`. This isn't documented prominently and fails silently.

The system is live. Push a Markdown file, wait for CI, query with SPARQL. Zero cost, zero infrastructure to maintain, standards-compliant Linked Data out the box.
