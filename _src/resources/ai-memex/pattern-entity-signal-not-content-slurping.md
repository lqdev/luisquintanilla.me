---
title: "Pattern: Entity Signal, Not Content Slurping — robots.txt Is the Lever, Not JSON-LD Placement"
description: "You cannot serve embedded JSON-LD to search bots while withholding it from AI crawlers — it lives in the HTML any fetcher reads. Keep JSON-LD as a machine/entity signal and control ingestion at the crawler layer: a generated robots.txt that allows search + AI-answer bots but disallows AI-training crawlers, plus a retained nosnippet meta."
entry_type: pattern
published_date: "2026-07-16 11:58 -05:00"
last_updated_date: "2026-07-16 11:58 -05:00"
tags: seo, jsonld, robots-txt, ai-crawlers, privacy, static-site-generation, fsharp, indieweb, structured-data
related_entries: pattern-jsonld-shared-identity-graph, pattern-single-source-config-generate-vs-verify, pattern-jsonld-central-injection-build-driver
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

Adding schema.org JSON-LD raised a real tension: the owner wants the *semantic /
entity-graph signal* for search + AI systems, but does **not** want big-tech
crawlers slurping content for model training. The instinct is "serve JSON-LD to the
good bots only." That instinct is wrong.

## The Key Realization

**JSON-LD is embedded in the page HTML, so any bot that fetches the page reads it.**
On a static site (Azure Static Web Apps here, no per-request server logic) you can't
vary the body by user-agent. So JSON-LD placement is *not* a privacy lever. The
levers are two **orthogonal** controls:

1. **SERP / AI-Overview display** → the `<meta name="robots" content="nosnippet">`
   that was already on every page. Google honors `nosnippet`/`max-snippet` for AI
   Overviews too, so it *already* limits big-tech from displaying/summarizing
   content — while still letting them read JSON-LD for non-snippet features (site
   name, knowledge panel, entity graph). **Keep it.**
2. **AI training / dataset ingestion** → `robots.txt` user-agent rules. This is the
   actual "don't slurp my content" control — and the site **had no robots.txt at
   all**, so nothing was being blocked.

## The Policy (block training, allow answers)

Generated at build time from a single-source `Constants.Crawlers` list (mirrors the
"generate derived config from constants" pattern — see
[[pattern-single-source-config-generate-vs-verify]]):

- **Disallow** AI *training* / dataset crawlers: `GPTBot`, `Google-Extended`,
  `ClaudeBot`/`anthropic-ai`, `CCBot`, `Bytespider`, `Applebot-Extended`,
  `Meta-ExternalAgent`, `Amazonbot`, `Google-CloudVertexBot`, `Diffbot`, …
- **Allow** (default) classic search (`Googlebot`, `Bingbot`) **and** AI
  *answer/retrieval* bots (`OAI-SearchBot`, `ChatGPT-User`, `PerplexityBot`,
  `Perplexity-User`) — so the site can still be **cited** in AI answers.

## Facts That Make This Safe (verified 2026)

- Blocking `Google-Extended` does **not** affect Googlebot search indexing; blocking
  `Applebot-Extended` does not affect Applebot search. The "-Extended" tokens are
  model-use opt-outs, separate from the search crawlers.
- The `noai` / `noimageai` meta tags are **not** broadly honored; robots.txt is the
  only widely-respected baseline (and only reputable, documented bots comply — it's
  a policy signal, not enforcement).
- Google's Sitelinks Search Box / `SearchAction` `potentialAction` was **deprecated
  Nov 2024** — skip it; no rich-result benefit.

## Takeaway

Decouple "who can *read* my structured data" (everyone, always — it's in the HTML)
from "who can *ingest* it for training" (robots.txt) and "who can *display* it in
results" (`nosnippet`). Trying to solve the second with JSON-LD placement is a
category error. See [[pattern-jsonld-shared-identity-graph]] for the JSON-LD side.
