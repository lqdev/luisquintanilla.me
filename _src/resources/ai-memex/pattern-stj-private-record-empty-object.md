---
title: "Pattern: A private F# Record Serializes as {} under System.Text.Json"
description: "System.Text.Json's reflection serializer only reads properties of accessible (public) types, so an F# record marked private silently serializes as an empty object instead of throwing."
entry_type: pattern
published_date: "2026-06-15 14:20 -05:00"
last_updated_date: "2026-06-15 14:20 -05:00"
tags: fsharp, dotnet, json, serialization, gotcha, reflection
related_entries: pattern-regex-on-rendered-html-antipattern, pattern-fsharp-module-extraction-inference-flip, pattern-progressive-loading, pattern-deterministic-zip-archives-dotnet
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

Phase 3 / B2 of the lqdev.me refactor replaced a hand-rolled `sprintf` + `escapeJson`
JSON builder (for the homepage progressive-load payload) with `System.Text.Json` (STJ)
over a typed record. The natural F# instinct is to make a throwaway DTO `private` — it
exists only to be serialized in one place:

```fsharp
type private ProgressiveContentItem =   // <-- the bug
    { title: string
      contentType: string
      date: string
      url: string
      content: string
      tags: string[] }
```

The build was clean. The output was catastrophically wrong: **every item serialized as
`{}`**. No exception, no warning — just a JSON array of empty objects.

## Root Cause

`System.Text.Json`'s default (reflection-based) serializer enumerates the **public**
properties of the runtime type. An F# record compiles its fields to properties, but when
the record is declared `private`, those properties are not accessible to STJ's reflection
walk. STJ does not throw on "no serializable members" — it emits `{}`. So a `private`
record is not a compile error and not a runtime error; it is a **silent empty serialize**.

This is specifically a visibility interaction, not an F#-vs-C# interop quirk. The same
record made public round-trips perfectly.

## Solution

Drop `private`. The record must be at least as accessible as the serializer's call site:

```fsharp
type ProgressiveContentItem =            // public — STJ can read the properties
    { title: string
      contentType: string
      date: string
      url: string
      content: string
      tags: string[] }
```

Verified by re-encoding 1570 items: every decoded `title`/`contentType`/`date`/`url`
matched the previous hand-rolled output exactly, and STJ's HTML-safe encoder produced a
faithful 1:1 escape (literal `<` count == `\u003C` count, `&` == `\u0026`).

## Prevention

- **Default DTOs to public** when they will be serialized by any reflection-based
  serializer (STJ, Newtonsoft, etc.). "Private because it's local" is the wrong instinct
  for a serialization target.
- **Treat `{}` output as a visibility smell**, not a data problem. If a serializer emits
  empty objects for a type you know has fields, check the type's accessibility *first* —
  before suspecting the data, the options, or the encoder.
- If you must keep the type private for API hygiene, switch to an explicit
  `JsonSerializerContext` (source-generated) or hand a public projection to the serializer
  — the reflection path cannot see private members no matter the options.
- This bites hardest in F# precisely because F# makes `private` cheap and idiomatic on
  small records; in C# the equivalent (an internal/private DTO) hits the same wall.
