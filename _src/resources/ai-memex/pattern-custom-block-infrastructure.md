---
title: "Pattern: Custom Block Infrastructure for Markdown"
description: "Custom markdown block parsers require careful pipeline ordering and individual extension registration to avoid conflicts with built-in Markdig containers."
entry_type: pattern
published_date: "2026-07-22 00:00 -05:00"
last_updated_date: "2026-07-22 00:00 -05:00"
tags: fsharp, web, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
---

## Discovery

Building custom markdown block syntax (like `:::media:::`, `:::review:::`, `:::venue:::`) on top of the Markdig parsing pipeline revealed a critical ordering dependency. The convenience method `UseAdvancedExtensions()` includes a built-in `CustomContainers` extension that silently consumes custom block syntax before our parsers ever see it. This produces no errors — the blocks simply render as raw text instead of structured HTML, making the bug difficult to diagnose.

## Root Cause / Problem

Markdig's `UseAdvancedExtensions()` is a convenience method that registers many extensions at once, including `CustomContainers`. When `CustomContainers` is registered in the pipeline, it matches the `:::` delimiter syntax and processes custom blocks before any project-specific parsers can handle them. The result is that custom blocks like `:::review:::` are consumed by the generic container handler and rendered as plain text with a `<div>` wrapper, rather than being processed by the project's custom YAML-to-HTML renderer.

The failure is silent — no exceptions are thrown, no warnings are logged. The markdown pipeline processes the content and produces valid (but wrong) HTML. This makes it particularly difficult to debug, because the pipeline appears to be working correctly.

## Solution

Replace `UseAdvancedExtensions()` with individual extension registrations, explicitly selecting only the Markdig extensions the project actually needs:

- Use `UsePipeTables()` for table support
- Use `UseGenericAttributes()` for attribute syntax
- Use `UseEmphasisExtras()` for extended emphasis
- Register other specific extensions as needed
- **Never** include `UseCustomContainers()` alongside custom block parsers

Additional implementation details for robust custom blocks:

- **YAML Content Processing**: Post-process extracted YAML content from within blocks to fix indentation issues and filter empty lines. The raw content between `:::` delimiters often has inconsistent whitespace that breaks YAML parsing.
- **Semantic HTML Output**: Custom blocks should render semantic HTML elements (`<figure>`, `<figcaption>`, `<address>`, `<dl>`) with CSS classes for styling, rather than generic `<div>` wrappers.
- **Testing Approach**: Validate both the YAML parsing stage (ensuring no exceptions are thrown for edge cases) and the HTML output stage (confirming proper semantic rendering rather than raw text passthrough).

## Key Components

- **Markdig Pipeline Builder**: The `MarkdownPipelineBuilder` configuration where extension ordering determines processing priority
- **Custom Block Parsers**: Project-specific parsers in `CustomBlocks.fs` that handle `:::media:::`, `:::review:::`, `:::venue:::`, and `:::rsvp:::` syntax
- **YAML Post-Processor**: Logic that cleans up extracted block content before passing it to the YAML deserializer
- **Semantic Renderers**: Functions that convert parsed block data into semantic HTML with proper elements and CSS classes
- **Individual Extensions**: `UsePipeTables()`, `UseGenericAttributes()`, and other specific Markdig extensions registered without the umbrella method

## Results

- Custom blocks render correctly as structured, semantic HTML instead of raw text
- YAML content within blocks parses reliably without indentation-related exceptions
- The Markdig pipeline processes standard markdown features (tables, emphasis, attributes) alongside custom blocks without conflicts
- New custom block types can be added by following the established parser registration pattern

## Benefits

Understanding this pipeline ordering dependency prevents a class of bugs that are exceptionally difficult to diagnose. The symptom — blocks rendering as plain text — looks like a parser bug or a syntax error in the content, not a pipeline configuration issue. Documenting the requirement to use individual extensions rather than `UseAdvancedExtensions()` saves significant debugging time for anyone extending the custom block system. It also establishes a clear pattern for adding new block types: write the parser, register it in the pipeline, and test both parsing and rendering independently.
