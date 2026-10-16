---
title: "Pattern: Strict Validation with Explicit Rejection Reporting"
description: "Preserve strict input and security validation while allowing batch projections to complete through deterministic, reviewable rejection reports."
entry_type: pattern
published_date: "2026-09-19 15:50 -05:00"
last_updated_date: "2026-09-19 15:50 -05:00"
tags: "fsharp, architecture, security, patterns"
related_skill: ""
source_project: "lqdev.me / AT Resource Graph"
---

## Discovery

The opt-in AT Resource Graph staging path projected the existing Blogroll, Podroll, and YouTube collections into bundle JSON. The wire contract intentionally accepts only absolute HTTPS syndication URLs without user information. That validation exposed three existing HTTP `XmlUrl` values in `Data/podroll.json`.

The original batch projection threw on the first invalid member. As a result, enabling the documented staging gate could not produce the three expected bundles. The review finding was valid, but changing the source URLs to HTTPS would have silently changed the site's existing feed contract.

## Root Cause

Two different concerns had been coupled:

1. Pure Resource Graph projection must reject data that violates the wire contract.
2. A batch staging/export operation must make progress across a collection and make every omission visible.

Treating every invalid member as a fatal batch error made strict validation correct but operationally incomplete. Silently dropping invalid members would have made the batch complete at the cost of data loss and poor auditability.

## Solution

Keep the strict projection path for callers that require a valid bundle:

```fsharp
let projectCollection collection =
    // Invalid URI or unsupported source type raises explicitly.
    ...
```

Use a separate tolerant path only for the opt-in batch staging operation:

```fsharp
let projectCollectionForStaging collection =
    // Returns accepted members plus deterministic rejection records.
    ...
```

The staging path follows these rules:

- Enforce the same absolute-HTTPS and no-user-info validation; never rewrite `http://` to `https://`.
- Emit every accepted member and preserve its source position, allowing gaps when earlier members are rejected.
- Record each rejected item in deterministic `rejections.json` with collection identity, item title, original `XmlUrl`, source type, position, stable code, and reason.
- Report emitted and rejected counts in `manifest.json`.
- Hash the complete original source snapshot, including rejected entries, so changes remain detectable.
- Generate all three collection bundles even when one contains invalid source entries.
- Leave the existing site feeds and OPML untouched; staging remains default-off and local-only.

The real configured-data regression test demonstrated 59 emitted members and 3 explicitly rejected HTTP Podroll entries. Focused assertions passed 38/38, and repeated staging runs produced byte-identical bundles, manifest, and rejection report.

## Prevention

When a strict validator is used inside a batch projection or export:

- Separate the strict pure function from the tolerant orchestration boundary.
- Treat rejection as a first-class output, not an exception that aborts unrelated work or an omission hidden from operators.
- Preserve original input values in diagnostics while excluding invalid values from the validated output.
- Test against real configured data in addition to synthetic invalid fixtures.
- Make rejection reports deterministic and include stable machine-readable codes.
- Do not “repair” identifiers or URLs unless the source contract explicitly authorizes normalization.
