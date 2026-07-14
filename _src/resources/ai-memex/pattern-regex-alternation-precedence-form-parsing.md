---
title: "Pattern: Group regex label alternations or they silently swallow the capture group"
description: "An unparenthesized | in a dynamically-built regex splits the whole pattern, so a prefix and capture group attached to one end stop applying to the other alternatives — dropping data with no error."
entry_type: pattern
published_date: "2026-07-13 20:49 -05:00"
last_updated_date: "2026-07-13 20:49 -05:00"
tags: "javascript, regex, github-actions, content-pipeline, patterns, gotcha"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: pattern-structured-json-over-html-parsing
---

## Discovery

Issue #2618 (a Predator: Badlands movie review) included a "Detailed Review Content"
section, but the generated file in PR #2619 contained only the `:::review` metadata block —
the entire prose body was missing. The live page rendered stars, director, and synopsis but
no actual review. An earlier movie review (`hell-house-llc-lineage`, created 11 days prior)
had its body intact, which flagged this as a **regression**, not a long-standing gap.

## Root Cause

`.github/workflows/process-content-issue.yml` extracts issue-form fields with a helper that
builds a regex from a label string:

```js
function extractFormValue(body, label) {
  const regex = new RegExp(`### ${label}\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|$)`, 'i');
  const match = body.match(regex);
  return (match && match[1]) ? match[1].trim() : '';
}
```

To tolerate two possible header spellings, the label was passed as an alternation:

```js
extractFormValue(issueBody, 'Detailed Review Content \\(Optional\\)|Detailed Review \\(Optional\\)')
```

`|` has the **lowest precedence** in a regex, so interpolating that label produces two
top-level alternatives across the *entire* pattern:

1. `### Detailed Review Content \(Optional\)` — matches the real header, but has **no capture group**
2. `Detailed Review \(Optional\)\s*\n\s*\n([\s\S]*?)...` — has the capture group, but no `### ` prefix

The engine matches alternative 1 first → `match[1]` is `undefined` → the body is returned as
`""`. No exception, no warning; the field is silently dropped. The same construction made the
`pros` / `cons` fields fragile (they only worked by luck because the *last* alternative
happened to carry the capture group).

## Fix

Wrap the interpolated label in a **non-capturing group** so the shared prefix, capture group,
and lookahead bind to every alternative:

```js
const regex = new RegExp(`### (?:${label})\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|$)`, 'i');
```

Applied to all six identical `extractFormValue` definitions in the workflow (note, media,
response, bookmark, marketplace, review steps). It is a no-op for single-alias labels and a
correctness fix for multi-alias labels, so it is safe to apply uniformly.

## Lessons

- **Any time you interpolate a variable into a regex, assume it may contain `|` and wrap it in
  `(?:…)`.** Precedence bugs from string-built regexes are invisible until the specific
  alternative that lacks the surrounding tokens is the one that matches.
- **Silent-empty is the dangerous failure mode.** Because the helper returns `''` on no-match
  (indistinguishable from a legitimately empty optional field), the drop produced valid-looking
  output and passed straight through to a merged PR and a live page.
- **A working sibling is the fastest triage signal.** The intact `hell-house` review dated the
  regression and pointed directly at what changed (the added `|Detailed Review (Optional)` alias).

## Verification

Node reproduction: the original regex returned `""` for the issue body; the `(?:…)`-wrapped
version returned the full multi-paragraph body, with `pros` still correctly empty for a
`_No response_` field. Confirmed end-to-end with `dotnet build` + `dotnet run`, then grepping
the regenerated `_public/reviews/predator-badlands-2026-07-13/index.html` for the body text.
