# Wiki Information Architecture

**Created**: 2026-02-01  
**Status**: Proposed  
**Purpose**: Define the category structure, navigation, and tagging taxonomy for the wiki.

## Overview

The wiki serves as a **living knowledge base** for:
- Technical references and how-tos
- Research summaries and AI outputs
- Evergreen knowledge on recurring topics
- Topic-focused resources with cross-references

---

## Category Structure

### Primary Categories

Organize wiki entries into these main categories using **tags** (the wiki system uses a flat file structure with tag-based organization):

| Category | Tag Prefix | Description | Examples |
|----------|------------|-------------|----------|
| **Development** | `dev-*` | Programming, tools, workflows | `dev-fsharp`, `dev-dotnet`, `dev-python` |
| **Infrastructure** | `infra-*` | DevOps, hosting, servers | `infra-nixos`, `infra-docker`, `infra-azure` |
| **Web Standards** | `web-*` | IndieWeb, ActivityPub, RSS | `web-indieweb`, `web-activitypub`, `web-rss` |
| **Privacy & Security** | `privacy-*`, `security-*` | Privacy tools, security practices | `privacy-tools`, `security-http-signatures` |
| **AI & ML** | `ai-*`, `ml-*` | AI research, ML frameworks | `ai-research`, `ml-frameworks` |
| **Productivity** | `productivity-*` | Workflows, tools, practices | `productivity-plaintext`, `productivity-pkm` |
| **Self-Hosting** | `selfhost-*` | Running services | `selfhost-mastodon`, `selfhost-matrix` |
| **Linux** | `linux-*` | Linux administration | `linux-nixos`, `linux-manjaro` |

### Tag Taxonomy

#### Existing Tags Analysis

Current wiki entries use these tag patterns:
- **Technology identifiers**: `python`, `dotnet`, `fsharp`, `nixos`
- **Broad categories**: `technology`, `software`, `programming`
- **Topic-specific**: `devcontainer`, `mastodon`, `privacy`
- **Meta tags**: `index`, `wiki`, `howto`

#### Proposed Tag Standards

1. **Primary Category Tag** (required): One of the category prefixes above
2. **Technology Tags** (when applicable): `python`, `fsharp`, `javascript`, etc.
3. **Content Type Tags** (required): `reference`, `howto`, `research`, `index`
4. **Topic Tags** (as needed): Specific subject matter tags

#### Tag Format Guidelines

- **Lowercase**: All tags should be lowercase
- **Hyphenated**: Multi-word tags use hyphens (`http-signatures`, not `httpsignatures`)
- **Singular**: Use singular form (`tool` not `tools`)
- **Consistent**: Use existing tags before creating new ones

---

## Content Types

### Wiki Entry Types

| Type | Tag | Description | Structure |
|------|-----|-------------|-----------|
| **Index Page** | `index` | Collection of related topics | Overview + Links |
| **Reference** | `reference` | Technical documentation | Overview + Details + Examples |
| **How-To** | `howto` | Step-by-step guides | Prerequisites + Steps + Troubleshooting |
| **Research Summary** | `research` | AI/human research outputs | Summary + Findings + Sources |
| **Comparison** | `comparison` | Technology evaluations | Criteria + Options + Recommendations |

### Entry Structure Templates

#### Index Page Structure
```markdown
---
title: "[Topic] Index"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "topic, index"
---

## Overview

Brief description of the topic area.

## Resources

- [Subtopic 1](/resources/wiki/subtopic-1)
- [Subtopic 2](/resources/wiki/subtopic-2)

## References

- [External Resource](https://example.com)
```

#### Reference Entry Structure
```markdown
---
title: "[Technology/Concept]"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "category, technology, reference"
---

## Overview

What this is and why it matters.

## Key Concepts

### Concept 1
Explanation...

### Concept 2
Explanation...

## Examples

```code
Example usage...
```

## Related Topics

- [Related Entry](/resources/wiki/related)

## References

- [Source 1](https://example.com)
```

#### How-To Entry Structure
```markdown
---
title: "How to [Task]"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "category, technology, howto"
---

## Overview

What you'll accomplish by following this guide.

## Prerequisites

- Requirement 1
- Requirement 2

## Steps

### Step 1: [Action]
Instructions...

### Step 2: [Action]
Instructions...

## Verification

How to confirm success.

## Troubleshooting

### Common Issue 1
Solution...

## References

- [Source](https://example.com)
```

#### Research Summary Structure
```markdown
---
title: "[Topic] Research Summary"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "category, topic, research"
---

## Overview

**Research Date**: YYYY-MM-DD  
**Research Sources**: [Tools/Methods used]  
**Status**: [Active/Complete]

## Summary

Key findings in 2-3 paragraphs.

## Detailed Findings

### Finding 1
Details...

### Finding 2
Details...

## Recommendations

- Recommendation 1
- Recommendation 2

## Sources

- [Source 1](https://example.com) - Brief description
- [Source 2](https://example.com) - Brief description
```

---

## Navigation & Discovery

### Entry Points

1. **Wiki Index** (`wiki.md`): Master index linking to all category index pages
2. **Category Index Pages**: Each major category has an index page
3. **Tag-Based Navigation**: Site search and tag pages for discovery
4. **Cross-References**: In-page links to related wiki entries

### Linking Strategy

#### Internal Wiki Links
```markdown
[Related Topic](/resources/wiki/related-topic)
```

#### Cross-References Section
Every entry should include a "Related Topics" or "See Also" section:
```markdown
## Related Topics

- [Related Entry 1](/resources/wiki/entry-1) - Brief description
- [Related Entry 2](/resources/wiki/entry-2) - Brief description
```

#### Backlinks
When creating new entries, update related entries to link back:
- Check existing entries that cover related topics
- Add links in their "Related Topics" sections

---

## File Naming Conventions

### Format
- **Lowercase**: All filenames in lowercase
- **Hyphenated**: Words separated by hyphens
- **Descriptive**: Clear indication of content
- **No Prefixes**: Category indicated by tags, not filename

### Examples
| Good | Bad |
|------|-----|
| `http-signatures.md` | `HTTPSignatures.md` |
| `nixos-garbage-collection.md` | `nixos_garbage_collection.md` |
| `activitypub-implementation.md` | `activitypub.md` (too vague) |
| `feature-flags.md` | `dev-feature-flags.md` (no prefixes) |

---

## Maintenance & Governance

### Update Triggers

Wiki entries should be updated when:
1. **Information Changes**: Technology updates, new versions
2. **Errors Found**: Corrections needed
3. **Enhancements**: Additional examples or sections added
4. **Cross-Reference Updates**: New related content created

### Freshness Indicators

The `last_updated_date` field serves as a freshness indicator:
- **Current**: Updated within 6 months
- **Review Needed**: 6-12 months since update
- **Stale**: Over 12 months without review

### Ownership

- **Primary Maintainer**: Site owner
- **AI Contributors**: Can propose new entries and updates following guidelines
- **Review Process**: All changes reviewed before merge

---

## Integration with Site Architecture

### Build System

Wiki entries are processed by the F# static site generator:
- **Location**: `_src/resources/wiki/`
- **Output**: `/resources/wiki/[entry-name]/`
- **Feed**: Included in site RSS feeds with `wiki` content type

### Search Integration

Wiki entries are included in site-wide search:
- Title and tags indexed
- Full-text content searchable
- Tag-based filtering available

### Text-Only Site

Wiki entries have text-only versions:
- **Location**: `/text/resources/wiki/[entry-name]/`
- Accessible version maintained automatically

---

## Proposed Category Index Pages

Create these index pages to organize the wiki:

| Index Page | Topics Covered |
|------------|----------------|
| `machine-learning.md` | ✅ Exists - AI/ML topics |
| `linux.md` | NEW - NixOS, Manjaro, system admin |
| `self-hosting.md` | NEW - Mastodon, Matrix, Owncast |
| `indieweb.md` | NEW - RSS, ActivityPub, microformats |
| `development.md` | NEW - DevContainers, F#, tooling |
| `privacy.md` | ✅ Exists - Privacy tools and practices |
| `productivity.md` | NEW - Plaintext, PKM, workflows |

---

## Summary

This information architecture provides:

1. **Consistent Structure**: Standard templates for different content types
2. **Discoverable Organization**: Tag-based categorization with index pages
3. **Cross-Referenced Knowledge**: Linking strategy for connected topics
4. **Maintainable Standards**: Clear naming and update conventions
5. **Scalable Design**: Categories can expand as content grows
