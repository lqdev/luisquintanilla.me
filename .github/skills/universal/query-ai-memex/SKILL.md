---
name: query-ai-memex
description: "Search and retrieve knowledge from the AI Memex — find patterns, research, and references across projects"
---

# Query AI Memex

Help the user search their AI Memex knowledge base for relevant entries.

## Search Strategy

Search in order of priority:

### 1. Local `.ai-memex/` directory
Check if the current project has a `.ai-memex/` directory and search it first:
```
grep -ri "{search_term}" .ai-memex/
```

### 2. Central store (if accessible)
If you can determine the lqdev.me repo location, search the canonical source:
```
grep -ri "{search_term}" C:\Dev\website\_src\resources\ai-memex\
```

Note: The central store path may vary. Check for `PersonalSite.fsproj` in common
development directories, or ask the user for the path.

### 3. Search by tag
Tags are in YAML frontmatter. Search for specific tags:
```
grep -l "tags:.*{tag_name}" .ai-memex/*.md
grep -l "tags:.*{tag_name}" C:\Dev\website\_src\resources\ai-memex\*.md
```

### 4. Search by entry type
```
grep -l "entry_type: pattern" .ai-memex/*.md
grep -l "entry_type: research" C:\Dev\website\_src\resources\ai-memex\*.md
```

## Response Format

When returning results, show:
1. **Entry title** (from YAML `title` field)
2. **Entry type** and **tags**
3. **Brief excerpt** (first 2-3 lines of content after frontmatter)
4. **File path** for reference

If multiple results, rank by relevance to the query.

## Common Queries

- "What do I know about [topic]?" → search by keyword across all entries
- "Show me patterns for [technology]" → filter by entry_type: pattern + tag
- "What research have I done on [topic]?" → filter by entry_type: research
- "Recent entries" → sort by published_date descending
