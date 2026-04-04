---
title: "Pattern: PyOxigraph API — serialize() not to_json()"
description: "PyOxigraph SPARQL query results use .serialize(format=QueryResultsFormat.JSON), not the non-existent .to_json() method"
entry_type: pattern
published_date: "2026-04-04"
last_updated_date: "2026-04-04"
tags: "python, rdf, sparql, patterns"
related_skill: ""
source_project: "markdown-ld-kb"
related_entries: ""
---

## Discovery

A deep research report for a knowledge bank project included PyOxigraph code using `r.to_json()` to serialize SPARQL query results. This method does not exist. The code would fail at runtime with an `AttributeError`.

## Root Cause

AI-generated code and documentation often hallucinate convenient method names. `.to_json()` is a common pattern in pandas and other Python libraries, so LLMs confidently generate it. PyOxigraph uses a different serialization API that follows the RDF ecosystem convention of format-parameterized `.serialize()` calls.

## Solution

**Correct API**:
```python
from pyoxigraph import Store, QueryResultsFormat

store = Store()
store.load("dataset.trig")  # auto-detects format by extension

results = store.query("SELECT ?s ?p ?o WHERE { ?s ?p ?o }")

# Correct: serialize with explicit format
json_bytes = results.serialize(format=QueryResultsFormat.JSON)
json_str = json_bytes.decode("utf-8")
```

**Available formats** via `QueryResultsFormat`:
- `JSON` — W3C SPARQL Results JSON
- `XML` — W3C SPARQL Results XML
- `CSV` — CSV format
- `TSV` — TSV format

**For an Azure Function endpoint**:
```python
r = store.query(sparql_query)
payload = r.serialize(format=QueryResultsFormat.JSON)
return func.HttpResponse(payload, mimetype="application/sparql-results+json")
```

**Comparison with RDFLib** (which uses a different but also valid API):
```python
# RDFLib
result = graph.query(sparql_query)
json_str = result.serialize(format="json")  # string parameter, not enum
```

**Source**: [PyOxigraph SPARQL docs](https://pyoxigraph.readthedocs.io/en/stable/sparql.html)

## Prevention

- Always check official docs for serialization APIs — don't trust AI-generated convenience methods
- When comparing RDFLib vs PyOxigraph, note the API differences: string format names vs enum values
- Write a quick smoke test (`assert hasattr(result, 'serialize')`) early in development
- The `.serialize()` method returns `bytes`, not `str` — remember to decode if needed
