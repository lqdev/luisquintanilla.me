---
title: "Pattern: NL-to-SPARQL via Schema-Injected Few-Shot Prompting"
description: "Translate natural language questions to SPARQL queries by injecting the actual ontology into an LLM prompt with few-shot examples, then validating output with the same parser that executes it"
entry_type: pattern
published_date: "2026-04-04 19:44 -05:00"
last_updated_date: "2026-04-04 19:44 -05:00"
tags: "python, ai, sparql, rdf, architecture, patterns"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: "pattern-github-models-zero-cost-ci-llm, blog-post-zero-cost-knowledge-graph-from-markdown"
---

## Discovery

We had a knowledge graph where an LLM pipeline extracts RDF triples from Markdown articles — but querying still required writing raw SPARQL. The same barrier we'd solved on the *write* side ("nobody will write RDF") existed on the *read* side ("nobody will write SPARQL").

The insight: the same LLM that *produces* the graph can also *query* it. And because the schema is small and fully known, we can inject it directly into the prompt to prevent hallucinated predicates.

## Root Cause

LLMs generate plausible-sounding but invalid SPARQL when they don't know the actual schema. They invent predicates like `schema:articleText` when the real property is `schema:name`, or reference classes that don't exist. Research shows schema-augmented prompts reduce invalid predicate errors by ~70%.

The key realization is that NL-to-SPARQL is a constrained generation problem — the ontology defines a finite vocabulary, so the LLM just needs to pick the right words from a known set.

## Solution

**Three-layer approach: Schema injection → Syntax validation → Safety enforcement**

### 1. Schema-injected system prompt

Embed the full ontology directly in the system prompt — classes, properties with domain/range, prefixes, and constraints:

```
AVAILABLE CLASSES:
  schema:Article, schema:Person, schema:Organization,
  schema:SoftwareApplication, schema:CreativeWork, schema:Thing

AVAILABLE PROPERTIES:
  schema:name          — label/title of any entity (xsd:string)
  schema:mentions      — article mentions entity (Article → Thing)
  schema:author        — author of article (Article → Person)
  ...

CONSTRAINTS:
1. Only use classes and properties listed above. Do NOT invent predicates.
2. Only generate SELECT or ASK queries.
3. Include LIMIT 100 unless user asks for all results.
4. If the question cannot be answered, output: CANNOT_ANSWER: <reason>
```

For small-to-medium ontologies (< 20 classes, < 30 properties), the full schema fits comfortably in the context window. For larger ontologies, retrieve only the relevant subset via RAG.

### 2. Few-shot examples matching real query patterns

Include 3-4 examples that demonstrate the actual query patterns users will need — not generic SPARQL:

```
Q: Which articles mention SPARQL?
A:
PREFIX schema: <https://schema.org/>
SELECT ?article ?title WHERE {
  ?article a schema:Article ;
           schema:name ?title ;
           schema:mentions ?entity .
  ?entity schema:name ?entityName .
  FILTER(LCASE(STR(?entityName)) = "sparql")
}
LIMIT 100
```

### 3. Validate → Retry with feedback → Safety enforce

```python
from rdflib.plugins.sparql import prepareQuery

def validate_sparql(sparql: str) -> tuple[bool, str]:
    try:
        prepareQuery(sparql)
        return True, ""
    except Exception as e:
        return False, str(e)
```

If validation fails, retry **once** with the error message appended:

```
Your previous SPARQL had a syntax error:
Expected SelectQuery, got 'SELEC' at position 0

Please fix the syntax error and output only the corrected SPARQL query.
```

This recovers ~70-80% of initially invalid queries. Cap at 2 LLM calls per request to respect rate limits.

Finally, enforce safety deterministically (never via LLM):
- Block mutating keywords (`INSERT`, `DELETE`, `DROP`, etc.)
- Inject `LIMIT 100` if absent
- Validate with code, not another LLM call

### 4. Return the generated SPARQL alongside results

```json
{
  "question": "What entities are in the knowledge graph?",
  "sparql": "PREFIX schema: ...\nSELECT ...",
  "results": { "head": {...}, "results": {"bindings": [...]} }
}
```

This is educational — users learn SPARQL by seeing what their questions translate to.

## Prevention

Apply this pattern proactively when:

- **You have a structured query language** (SPARQL, SQL, GraphQL, KQL) that end users shouldn't need to learn
- **The schema is known and bounded** — inject it to prevent hallucination rather than hoping the LLM guesses correctly
- **You already use an LLM** in the pipeline — the symmetry of "same LLM writes and reads" keeps the stack simple

**Avoid this pattern when:**
- The schema is too large for the context window (> ~100 properties) — use RAG to retrieve relevant fragments instead
- Queries require complex joins or aggregations — consider a template/hybrid approach
- Rate limits are extremely tight — each NL query costs 1-2 LLM calls
