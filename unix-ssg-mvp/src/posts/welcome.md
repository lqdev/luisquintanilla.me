---
post_type: "article"
title: "Welcome to Unix SSG"
published_date: "2025-01-13 10:00 -05:00"
tags: ["unix", "ssg", "bash", "static-site"]
description: "A demonstration of the Unix-based static site generator"
---

# Welcome to the Unix Static Site Generator

This is a demonstration of a **minimal static site generator** built with standard Unix tools.

## Features

- ✅ Markdown processing with Pandoc
- ✅ YAML frontmatter parsing with yq  
- ✅ HTML templating with envsubst
- ✅ RSS feed generation
- ✅ Tag-based organization
- ✅ Parallel processing with Make
- ✅ Minimal dependencies

## Philosophy

The Unix philosophy of **"do one thing well"** applied to static site generation:

> Write programs that do one thing and do it well. Write programs to work together.

Each component has a single responsibility:
- `pandoc` converts markdown to HTML
- `yq` processes YAML metadata  
- `envsubst` handles template substitution
- `make` orchestrates the build process

## Code Example

```bash
# Process all markdown files in parallel
find src -name "*.md" -type f | \
    xargs -P $(nproc) -I {} bin/process-markdown.sh {}
```

This approach minimizes dependencies while maintaining full functionality.