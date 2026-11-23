---
name: write-ai-memex
description: "Write knowledge entries to the AI Memex — capture patterns, research, decisions, and project outcomes during any coding session"
---

# Write AI Memex Entry

You are helping the user capture knowledge in their AI Memex — a distributed knowledge system published at lqdev.me/resources/ai-memex/.

## Context Detection

Before writing, detect where you are:

1. **Check for `PersonalSite.fsproj`** in the current directory or any parent directory
2. If found → you're in the lqdev.me repo → write directly to `_src/resources/ai-memex/{slug}.md`
3. If not found → you're in another project → write to `.ai-memex/{slug}.md` in the project root

## Entry Types

| Type | Purpose | Use When |
|------|---------|----------|
| `pattern` | Reusable solutions, gotchas, proven approaches | Bug fix, architecture decision, recurring solution |
| `research` | Technology evaluations, comparisons | Multi-approach analysis completed |
| `reference` | Living docs, architecture overviews | Something reusable was built |
| `project-report` | Feature summaries, retrospectives | Feature shipped, milestone reached |
| `blog-post` | AI-human collaboration insights | Meta-observations about the process |

## File Naming

Use kebab-case slugs that describe the content:
- `pattern-progressive-loading.md`
- `research-caching-strategies.md`
- `reference-api-auth-patterns.md`
- `project-report-v2-migration.md`

## YAML Frontmatter Schema

```yaml
---
title: "Clear, Descriptive Title"
description: "One-sentence summary of what this entry covers"
entry_type: pattern          # One of: pattern, research, reference, project-report, blog-post
published_date: "YYYY-MM-DD HH:mm zzz"
last_updated_date: "YYYY-MM-DD HH:mm zzz"
tags: "tag1, tag2, tag3"     # Comma-separated, lowercase
related_skill: ""            # Optional: skill name if this pattern relates to a skill
source_project: ""           # Optional: project name where this was discovered
---
```

## Quality Standards

### All entries
- **Evidence-based**: Include specific code, commands, or examples
- **Context-rich**: Explain WHY, not just WHAT
- **Discoverable**: Use clear titles and relevant tags
- **Self-contained**: Readable without other context

### By type
- **pattern**: Include Discovery (what happened), Root Cause (why), Solution (how), and Prevention (avoid in future)
- **research**: Include Options Considered, Evaluation Criteria, Recommendation, and Trade-offs
- **reference**: Include Overview, Key Components, Usage Examples, and Gotchas
- **project-report**: Include Objective, Approach, Outcome, and Lessons Learned
- **blog-post**: Include Observation, Context, Reflection, and Takeaway

## Consent Protocol

**ALWAYS ask before creating.** Never auto-generate entries.

Propose like this:
> "This looks like a good [type] entry for the AI Memex. Want me to write it up?"

If the user says yes, write the full entry. If they want changes, iterate.

## Tag Taxonomy

Use tags from these categories:
- **Technology**: `fsharp`, `dotnet`, `python`, `typescript`, `javascript`, `rust`, `docker`, `kubernetes`, `azure`
- **Domain**: `web`, `api`, `databases`, `devops`, `architecture`, `security`, `performance`, `accessibility`
- **Meta**: `patterns`, `research`, `ai-collaboration`

See `references/TEMPLATES.md` for per-type frontmatter templates.
