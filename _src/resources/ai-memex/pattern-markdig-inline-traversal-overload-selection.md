---
title: "Pattern: Explicit Markdig Inline Traversal During F# SDK Upgrades"
description: "Use a MarkdownObject upcast to select general AST traversal when a document's block-container overload conflicts with inline node types."
entry_type: pattern
published_date: "2026-11-14 21:34 -06:00"
last_updated_date: "2026-11-14 21:34 -06:00"
tags: "fsharp, dotnet, web, architecture, patterns"
related_skill: "build-validate"
source_project: "lqdev-me"
related_entries: "pattern-atproto-response-media-excerpts, pattern-nuget-lock-hash-drift"
---

## Discovery

During the site's migration to stable .NET SDK 11.0.100, the initial generator build
rejected two Markdig traversals with a type error saying that `LinkInline` was
incompatible with `Block`. Markdig remained at version 0.38.0.

The affected calls both searched a whole Markdown document for inline links:

- `AtProtoResponseMapping.fs`, in `containsUnsupportedQuoteMedia`, detects images
  before staging a quote post that cannot represent their media.
- `AtProtoBuilder.fs`, in `removeMarkdownImageLinks`, finds linked image previews
  whose source spans should be removed from a response's syndicated excerpt.

Explicitly upcasting the document to `Markdig.Syntax.MarkdownObject` resolved those
build diagnostics without changing the traversal's predicates or the Markdown
pipeline.

## Root Cause

Markdig 0.38.0 exposes three generic `Descendants<T>` overloads. Reflection against
the same assembly used by the generator confirmed their different constraints:

| Input type | Generic node constraint | Appropriate traversal |
|---|---|---|
| `MarkdownObject` | `MarkdownObject` | General AST traversal, including inline nodes inside blocks |
| `ContainerInline` | `Inline` | An existing inline container |
| `ContainerBlock` | `Block` | Block nodes inside a block container |

A `MarkdownDocument` is also a `ContainerBlock`. If a call requesting `LinkInline`
binds to the block-container overload, its generic constraint cannot be satisfied:
`LinkInline` is an inline node, not a `Block`. Fully qualifying the method name does
not itself distinguish these overloads.

**Scope matters:** a minimal standalone F# Interactive example using the original,
uncast call successfully found an image under both SDK 10.0.403 and SDK 11.0.100.
The explicit version also passed under both SDKs. This is therefore a
project-context compatibility repair observed during the migration, not proof
that every F# 11 call to Markdig inline traversal fails, or that Markdig needs a
major-version upgrade.

## Solution

Make the intended general traversal input explicit:

```fsharp
let hasMarkdownImage (markdown: string) : bool =
    let document = Markdig.Markdown.Parse(markdown)
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.Inlines.LinkInline>(
        document :> Markdig.Syntax.MarkdownObject)
    |> Seq.exists (fun link -> link.IsImage)
```

The upcast selects the overload whose generic constraint permits inline nodes.
It retains the same document object; it does not reparse content, rebuild nodes,
change source spans, or introduce a runtime downcast.

In the site itself, retain `ASTParsing.parseMarkdownAst` rather than substituting
the default parser used in this standalone example. The shared parser preserves
custom blocks and the pipeline used to render pages.

The production change was limited to the argument:

```fsharp
// Before: the input also satisfies the block-container overload.
Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.Inlines.LinkInline>(doc)

// After: express general AST traversal explicitly.
Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.Inlines.LinkInline>(
    doc :> Markdig.Syntax.MarkdownObject)
```

Block traversal, such as `Descendants<MediaBlock>(doc)`, remains unchanged. Its
block-container constraint is appropriate.

## Validation

The .NET 11 generator build and both relevant regression scripts passed after the
change:

```powershell
dotnet build --no-restore -p:NuGetLockFilePath=$localLock
dotnet fsi test-scripts\test-atproto-response-mapping.fsx
dotnet fsi test-scripts\test-atproto-media.fsx
```

`$localLock` is the isolated local lockfile described in
[[pattern-nuget-lock-hash-drift]]. It preserves the committed CI lockfile when
SDK-bundled package hashes differ locally; it is not a package-version override.
Ubuntu CI also passed ordinary locked restore, build, strict generation, and the
regression scripts using the committed lockfile.

For migration commit `0ec6072c` in
[PR #2960](https://github.com/lqdev/luisquintanilla.me/pull/2960), comparison against
two .NET 10 baseline generations preserved the 14,500-file inventory and all 59
binary files. Remaining content matched after precisely checking the pre-existing
RSS and knowledge-graph build clocks and the intentional Node runtime setting.
That comparison predates this new knowledge entry; adding an entry intentionally
changes content, feeds, and graph output.

The excerpt behavior protected by these traversals is described in
[[pattern-atproto-response-media-excerpts]].

## Prevention

- When an inline traversal reports a block constraint, inspect the actual
  overload input types and generic constraints before changing dependencies.
- Use the `MarkdownObject` upcast for whole-document inline traversal when
  overload selection needs to be explicit; keep block-only traversal separate.
- Validate the full consuming project and its shared parsing pipeline. A
  standalone F# Interactive success does not reproduce every project-context
  binding failure.
- Keep SDK and package versions pinned during migration so a narrow compiler
  compatibility change can be evaluated independently of dependency upgrades.
