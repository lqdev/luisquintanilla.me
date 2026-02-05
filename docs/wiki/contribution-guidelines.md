# Wiki Contribution Guidelines

**Created**: 2026-02-01  
**Status**: Active  
**Purpose**: Standards for creating, maintaining, and updating wiki entries by both humans and AI assistants.

## Overview

This document establishes the curation and documentation standards for wiki contributions. Following these guidelines ensures:

- **Consistency**: Uniform structure and quality across entries
- **Discoverability**: Proper tagging and linking for findability
- **Maintainability**: Clear ownership and update procedures
- **AI-Compatibility**: Structured content that AI can reference and build upon

---

## Who Should Contribute

### Human Contributors
- Site owner for original research and personal knowledge
- Collaborators for specialized topics

### AI Assistants
- Research summary compilation
- Knowledge synthesis from multiple sources
- Documentation generation from implementation work
- Cross-referencing and link suggestions

---

## Content Standards

### Quality Criteria

All wiki entries must meet these standards:

| Criterion | Description |
|-----------|-------------|
| **Accuracy** | Information is factually correct and verified |
| **Completeness** | Topic is covered sufficiently for the intended audience |
| **Clarity** | Written for readability; avoids unnecessary jargon |
| **Currency** | Information is up-to-date or clearly dated |
| **Attribution** | Sources are cited for external information |

### Writing Style

- **Tone**: Informative, professional, accessible
- **Person**: Second person ("you") for how-tos; third person for references
- **Tense**: Present tense for facts; past tense for historical context
- **Length**: Concise but complete; prefer shorter entries with links to detailed topics
- **Format**: Use headings, lists, and code blocks for scannability

### Content Types and When to Use Them

| Type | Use When | Example |
|------|----------|---------|
| **Index Page** | Collecting multiple related topics | Machine Learning index |
| **Reference** | Documenting a technology or concept | HTTP Signatures |
| **How-To** | Teaching a specific task | Configure DevContainers |
| **Research Summary** | Presenting research findings | ActivityPub Patterns |
| **Comparison** | Evaluating options | Static Site Generators |

---

## Metadata Requirements

### Required Frontmatter

Every wiki entry must include this YAML frontmatter:

```yaml
---
title: "[Descriptive Title]"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "primary-category, technology, content-type, topic"
---
```

### Field Specifications

| Field | Required | Format | Example |
|-------|----------|--------|---------|
| `title` | Yes | Quoted string | `"HTTP Signatures"` |
| `last_updated_date` | Yes | `YYYY-MM-DD HH:MM -05:00` | `"2026-02-01 10:30 -05:00"` |
| `tags` | Yes | Comma-separated string | `"security, activitypub, reference"` |

### Tagging Requirements

1. **Minimum 2 tags**: At least one category and one content type
2. **Maximum 10 tags**: Avoid over-tagging; be selective
3. **Use existing tags first**: Check existing entries before creating new tags
4. **Tag format**: lowercase, hyphenated multi-word tags

#### Standard Tags by Category

```
# Categories
linux, nixos, development, self-hosting, privacy, security
web-standards, indieweb, activitypub, ai, machine-learning
productivity, tools

# Content Types
index, reference, howto, research, comparison

# Technologies
fsharp, dotnet, python, javascript, rust, go
docker, azure, github-actions
```

---

## Naming Conventions

### File Names

- **Format**: `lowercase-hyphenated-name.md`
- **Descriptive**: Indicate content clearly
- **No prefixes**: Use tags for categorization, not filename prefixes
- **Consistent**: Follow existing patterns

### Examples

| Good | Bad | Why |
|------|-----|-----|
| `http-signatures.md` | `HTTPSignatures.md` | Use lowercase + hyphens |
| `nixos-garbage-collection.md` | `gc.md` | Be descriptive |
| `activitypub-patterns.md` | `dev-activitypub.md` | No prefixes |

---

## Structure Requirements

### Minimum Sections

Every wiki entry must have:

1. **Overview**: What this entry covers and why it matters
2. **Content Section(s)**: The main information (varies by type)
3. **References**: Sources, further reading, or related external links

### Recommended Sections

| Section | Purpose | When to Include |
|---------|---------|-----------------|
| **Prerequisites** | What's needed before following | How-tos |
| **Examples** | Concrete usage demonstrations | References |
| **Troubleshooting** | Common issues and solutions | How-tos |
| **Related Topics** | Links to other wiki entries | Always recommended |
| **Changelog** | Entry update history | Complex or evolving entries |

### Section Ordering

Follow this general order:

```markdown
## Overview
## [Content-Specific Sections]
## Examples (if applicable)
## Related Topics
## Troubleshooting (if applicable)  
## References
```

---

## Linking Standards

### Internal Wiki Links

Link to other wiki entries using relative paths:

```markdown
[Related Entry](/resources/wiki/entry-name)
```

### Cross-Reference Best Practices

1. **Bidirectional**: When linking to entry A from entry B, also link B from A
2. **Contextual**: Place links where they provide value, not just at the end
3. **Descriptive**: Use meaningful link text, not "click here"

### External Links

- Include protocol: `https://example.com`
- Use descriptive text: `[Official Documentation](https://example.com/docs)`
- Prefer stable URLs (avoid URLs likely to change)

---

## Attribution & Sources

### When to Cite

Always cite sources for:
- Factual claims from external sources
- Quotations
- Configuration examples from documentation
- Research findings from specific sources

### Citation Format

Use a References section with link and brief description:

```markdown
## References

- [Official Documentation](https://example.com/docs) - Authoritative source for configuration options
- [Community Guide](https://example.com/guide) - Practical examples and troubleshooting
```

### AI-Generated Content Attribution

When AI assistance is used:

```markdown
## References

- Research compiled with AI assistance (Claude, 2026-02-01)
- [Primary Source](https://example.com) - Verified documentation
```

---

## For AI Assistants

### When to Create Wiki Entries

Create a new wiki entry when:

1. **Research is Complete**: You've compiled information worth preserving
2. **Knowledge is Evergreen**: The information has lasting value
3. **Topic Fits Wiki**: It's general knowledge, not project-specific
4. **No Existing Entry**: The topic isn't already covered

### AI Contribution Workflow

1. **Research Phase**: Gather information from reliable sources
2. **Draft Phase**: Create entry following templates
3. **Validation**: Verify accuracy of claims
4. **Linking**: Add cross-references to related entries
5. **Submission**: Create/update file in `_src/resources/wiki/`

### What AI Should NOT Add to Wiki

- Project-specific implementation details (→ use /docs)
- Transient information (→ use logs or archives)
- Unverified claims without sources
- Duplicate content from existing entries

### Linking from AI Research

When AI research could become a wiki entry:

```
I've completed research on [topic] that would make a good wiki entry.
Key findings:
- [Finding 1]
- [Finding 2]

Would you like me to create a wiki entry following the contribution guidelines?
```

---

## Review Process

### Before Submitting

- [ ] Frontmatter complete with title, date, and tags
- [ ] File name follows conventions
- [ ] All sections have content (no empty sections)
- [ ] Links are valid and descriptive
- [ ] Sources are cited where appropriate
- [ ] Related entries updated with backlinks
- [ ] Spelling and grammar checked

### Review Criteria

Reviewers check for:

1. **Accuracy**: Information is correct
2. **Completeness**: Topic is adequately covered
3. **Standards**: Guidelines are followed
4. **Quality**: Writing is clear and useful
5. **Integration**: Links and tags are appropriate

---

## Maintenance

### Update Triggers

Update wiki entries when:

- Information becomes outdated
- Errors are discovered
- Related new content is added
- Technology versions change

### Update Process

1. Update content with new information
2. Update `last_updated_date` in frontmatter
3. Add or update References section
4. Update cross-references if needed

### Deprecation

If an entry becomes obsolete:

1. Add deprecation notice at top:
   ```markdown
   > ⚠️ **Deprecated**: This entry is outdated. See [New Entry](/resources/wiki/new-entry) for current information.
   ```
2. Keep entry for historical reference
3. Update linking entries to point to replacement

---

## Templates

### Quick Start

Use the VS Code snippet `wiki` for basic structure:

```markdown
---
title: "[Title]"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "[tags]"
---

## Overview

[Content]
```

### Full Templates

See [Information Architecture](information-architecture.md) for complete templates:
- Index Page Template
- Reference Entry Template
- How-To Entry Template
- Research Summary Template

---

## Summary

Following these guidelines ensures:

✅ **Consistent quality** across all wiki entries  
✅ **Easy discovery** through proper tagging and linking  
✅ **Sustainable maintenance** with clear update procedures  
✅ **AI integration** through structured, referenceable content  
✅ **Long-term value** as a living knowledge base
