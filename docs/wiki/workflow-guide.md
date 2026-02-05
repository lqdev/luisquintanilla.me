# Wiki Curation Workflow Guide

**Created**: 2026-02-01  
**Status**: Active  
**Purpose**: Step-by-step process for AI assistants and humans to create, structure, and update wiki content.

## Overview

This workflow guide provides practical processes for:
1. Creating new wiki entries
2. Migrating content from /docs to wiki
3. Publishing AI research as wiki entries
4. Maintaining and updating existing entries

---

## Quick Reference

### Wiki Entry Locations

| Purpose | Location |
|---------|----------|
| **Wiki entries** | `_src/resources/wiki/` |
| **Wiki output** | `_public/resources/wiki/` |
| **Documentation** | `docs/wiki/` |

### VS Code Snippets

| Snippet | Purpose |
|---------|---------|
| `wiki` | Create new wiki entry with frontmatter |
| `dt` | Insert current datetime with timezone |

### Essential Commands

```bash
# Build site to validate wiki entries
dotnet run

# Check wiki output
ls _public/resources/wiki/
```

---

## Workflow 1: Creating New Wiki Entries

### Step 1: Determine Entry Type

Choose the appropriate type based on content:

| If your content is... | Use type... |
|----------------------|-------------|
| Collection of related topics | Index Page |
| Technical documentation | Reference |
| Step-by-step instructions | How-To |
| Research findings | Research Summary |
| Option evaluation | Comparison |

### Step 2: Create the File

1. Navigate to `_src/resources/wiki/`
2. Create new file: `[topic-name].md`
3. Use VS Code snippet `wiki` or copy template below

### Step 3: Add Frontmatter

```yaml
---
title: "Your Title Here"
last_updated_date: "2026-02-01 10:30 -05:00"
tags: "category, technology, content-type"
---
```

### Step 4: Write Content

Follow the structure for your entry type (see templates below).

### Step 5: Add Cross-References

1. Link to related existing wiki entries
2. Update those entries to link back to your new entry

### Step 6: Validate

```bash
# Build the site
dotnet run

# Check your entry appears
ls _public/resources/wiki/[your-entry-name]/
```

### Step 7: Submit

Create PR with:
- New wiki entry file
- Any updated entries (backlinks)

---

## Workflow 2: Migrating from /docs to Wiki

### Decision Criteria

Migrate content to wiki if it meets **ALL** criteria:

- [ ] Evergreen knowledge (not project-specific)
- [ ] Useful beyond this repository
- [ ] Has lasting reference value
- [ ] Not already covered by existing wiki entry

### Migration Steps

#### Step 1: Identify Source Document

Use [Audit Report](audit-report.md) to identify migration candidates.

#### Step 2: Extract Evergreen Content

From the source document, extract:
- Core concepts and explanations
- Reusable patterns and examples
- External references

**Exclude**:
- Project-specific implementation details
- Dated context ("in this PR we...")
- Transient information

#### Step 3: Create Wiki Entry

Create new file in `_src/resources/wiki/` following templates.

#### Step 4: Transform Content

| Original Style | Wiki Style |
|---------------|------------|
| "In this implementation..." | "This pattern involves..." |
| "We discovered that..." | "A key consideration is..." |
| Project-specific code | Generic examples |

#### Step 5: Handle Source Document

Choose one:

| Scenario | Action |
|----------|--------|
| All content migrated | Archive to `/archive/` |
| Partial migration | Keep in `/docs`, add wiki reference |
| Still needed for context | Keep in `/docs`, link to wiki |

#### Step 6: Update References

1. Update any documents that referenced the source
2. Add cross-references in related wiki entries
3. Update index pages if applicable

---

## Workflow 3: AI Research to Wiki

### When to Convert Research to Wiki

Convert AI research outputs when:
- Research has lasting value
- Topic is general enough for reference
- Information is verified/accurate
- No existing wiki entry covers it

### AI Research Workflow

#### Phase 1: Research Execution

```
1. Conduct research using available tools
2. Compile findings with sources
3. Validate accuracy of claims
4. Identify wiki-worthy content
```

#### Phase 2: Proposal

Before creating entry, propose to human reviewer:

```markdown
## Wiki Entry Proposal

**Proposed Title**: [Title]  
**Category**: [Category from information architecture]  
**Content Type**: [reference/howto/research]

**Summary**: 
[2-3 sentence summary of what the entry will cover]

**Key Topics**:
- Topic 1
- Topic 2
- Topic 3

**Sources**:
- [Source 1]
- [Source 2]

**Rationale**: 
[Why this should be a wiki entry vs. staying in research notes]
```

#### Phase 3: Creation

After approval:

1. Create wiki entry following templates
2. Include AI attribution in References:
   ```markdown
   ## References
   
   - Research compiled with AI assistance (Claude, YYYY-MM-DD)
   - [Primary Source](url) - Verified documentation
   ```
3. Submit for review

#### Phase 4: Review

Human reviewer checks:
- Accuracy of claims
- Appropriate scope
- Proper attribution
- Guidelines compliance

---

## Workflow 4: Maintaining Existing Entries

### Regular Maintenance Triggers

| Trigger | Action |
|---------|--------|
| Technology version update | Update examples, links |
| Related new entry created | Add cross-reference |
| Error discovered | Correct and update date |
| Information outdated | Update or deprecate |

### Update Process

1. **Make changes** to content
2. **Update `last_updated_date`** in frontmatter
3. **Update References** if sources changed
4. **Add to changelog** if significant:
   ```markdown
   ## Changelog
   
   - 2026-02-01: Updated examples for v2.0
   - 2025-08-15: Initial entry
   ```
5. **Validate build** and submit

### Deprecation Process

When content is superseded:

1. Add deprecation notice:
   ```markdown
   > ⚠️ **Deprecated**: This entry is outdated. See [Replacement Entry](/resources/wiki/replacement) for current information.
   ```
2. Keep file for historical reference
3. Update linking entries to point to replacement
4. Consider archiving after 6+ months

---

## Templates

### Basic Wiki Entry

```markdown
---
title: "[Title]"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "category, technology, content-type"
---

## Overview

[What this entry covers and why it matters.]

## [Main Content Section]

[Primary information.]

## Related Topics

- [Related Entry 1](/resources/wiki/entry-1)
- [Related Entry 2](/resources/wiki/entry-2)

## References

- [Source 1](https://example.com) - Description
```

### How-To Entry

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

Instructions and explanation.

```code
# Example command or code
```

### Step 2: [Action]

Instructions and explanation.

## Verification

How to confirm success.

## Troubleshooting

### Issue: [Problem]

**Solution**: [Fix]

## Related Topics

- [Related Entry](/resources/wiki/entry)

## References

- [Source](https://example.com)
```

### Research Summary Entry

```markdown
---
title: "[Topic] Research Summary"
last_updated_date: "YYYY-MM-DD HH:MM -05:00"
tags: "category, topic, research"
---

## Overview

**Research Date**: YYYY-MM-DD  
**Research Tools**: [Tools used]  
**Status**: [Active/Complete]

Brief summary of the research area and purpose.

## Key Findings

### Finding 1: [Title]

Details and evidence.

### Finding 2: [Title]

Details and evidence.

## Recommendations

Based on the research:

1. [Recommendation 1]
2. [Recommendation 2]

## Related Topics

- [Related Entry](/resources/wiki/entry)

## References

- Research compiled with AI assistance (Claude, YYYY-MM-DD)
- [Source 1](https://example.com) - Description
- [Source 2](https://example.com) - Description
```

---

## Checklist for Wiki Contributions

Use this checklist before submitting:

### New Entry Checklist

- [ ] File created in `_src/resources/wiki/`
- [ ] Filename is lowercase-hyphenated.md
- [ ] Frontmatter includes title, date, and 2+ tags
- [ ] Overview section explains entry purpose
- [ ] All sections have content (no empty sections)
- [ ] Related Topics section links to other wiki entries
- [ ] References section cites sources
- [ ] Related entries updated with backlinks
- [ ] Build validates successfully (`dotnet run`)

### Migration Checklist

- [ ] Source document identified for migration
- [ ] Evergreen content extracted and transformed
- [ ] Project-specific content removed
- [ ] Source document handled (archived/updated/kept)
- [ ] References updated across codebase
- [ ] New entry follows guidelines

### Update Checklist

- [ ] Content changes made
- [ ] `last_updated_date` updated
- [ ] References section updated if needed
- [ ] Changelog added if significant change
- [ ] Related entries checked for accuracy
- [ ] Build validates successfully

---

## Troubleshooting

### Build Fails After Adding Entry

**Check**:
- YAML frontmatter syntax (proper quotes, colons)
- No duplicate fields in frontmatter
- File encoding is UTF-8

**Fix**:
```yaml
# Correct format
---
title: "Entry Title"
last_updated_date: "2026-02-01 10:30 -05:00"
tags: "tag1, tag2"
---
```

### Entry Not Appearing in Output

**Check**:
- File is in `_src/resources/wiki/` (not `/docs/wiki/`)
- File has `.md` extension
- Frontmatter is valid

### Tags Not Working

**Check**:
- Tags are comma-separated string, not array
- No trailing commas
- Tags are lowercase

---

## Support & Resources

### Documentation

- [Audit Report](audit-report.md) - Content migration priorities
- [Information Architecture](information-architecture.md) - Structure and categories
- [Contribution Guidelines](contribution-guidelines.md) - Standards and requirements

### Related System Documentation

- `docs/ai-memory-management-system.md` - AI memory patterns
- `projects/templates/` - Project documentation templates
- `.vscode/metadata.code-snippets` - Content creation shortcuts
