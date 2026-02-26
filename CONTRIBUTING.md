# Contributing to luisquintanilla.me

Thank you for your interest in contributing! This guide provides everything you need to know about making contributions to this repository.

## Table of Contents

- [Quick Start](#quick-start)
- [Repository Structure](#repository-structure)
- [Development Workflow](#development-workflow)
- [Content Creation](#content-creation)
- [Code Contributions](#code-contributions)
- [File Organization Guidelines](#file-organization-guidelines)
- [Pull Request Guidelines](#pull-request-guidelines)
- [Checklist Templates](#checklist-templates)

---

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- F# compiler (included with .NET SDK)
- Git

### Build Commands

```bash
# Clone and enter the repository
git clone https://github.com/lqdev/luisquintanilla.me.git
cd luisquintanilla.me

# Restore packages
dotnet restore

# Build and generate site
dotnet run

# Site is generated in _public/ directory
```

### Directory Overview

```
├── _src/           # Source content (Markdown)
├── _public/        # Generated output (do not edit)
├── docs/           # Documentation
├── Scripts/        # Development scripts
├── Services/       # Shared F# services
├── Views/          # F# view modules
├── projects/       # Project management
└── archive/        # Historical files
```

---

## Repository Structure

### Core F# Modules

| File | Purpose |
|------|---------|
| `Program.fs` | Entry point and build orchestration |
| `Domain.fs` | Core types and ITaggable interface |
| `Builder.fs` | High-level build functions |
| `GenericBuilder.fs` | Unified content processing |
| `ASTParsing.fs` | Markdown AST parsing |
| `CustomBlocks.fs` | Markdig extensions |
| `BlockRenderers.fs` | HTML rendering for custom blocks |

### Content Source (`_src/`)

| Directory | Content Type | RSS Feed |
|-----------|--------------|----------|
| `posts/` | Long-form blog posts | /posts.rss |
| `notes/` | Microblog notes | /notes.rss |
| `responses/` | Social responses | /responses.rss |
| `snippets/` | Code snippets | /snippets.rss |
| `wiki/` | Knowledge base | /wiki.rss |
| `media/` | Photos/media | /media.rss |
| `albums/` | Photo collections | /albums.rss |
| `playlists/` | Music playlists | /playlists.rss |
| `resources/presentations/` | Reveal.js presentations | /presentations.rss |

### Documentation (`docs/`)

| Directory | Purpose |
|-----------|---------|
| `docs/` | Feature documentation |
| `docs/adr/` | Architectural Decision Records |
| `docs/activitypub/` | ActivityPub integration docs |

### Project Management (`projects/`)

| Directory | Purpose |
|-----------|---------|
| `projects/active/` | Current work only (max 1-2 files) |
| `projects/archive/` | Completed project documentation |
| `projects/completed/` | Brief completion notes |
| `projects/templates/` | Templates for project docs |
| `projects/backlog.md` | Prioritized backlog |

---

## Development Workflow

### 1. Understand the Task

- Read existing documentation in `docs/`
- Check `docs/adr/` for architectural decisions
- Review `changelog.md` for recent changes
- Look at similar implementations in the codebase

### 2. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

### 3. Make Changes

- Make small, incremental changes
- Build after each significant change: `dotnet build`
- Test changes by running: `dotnet run`
- Verify output in `_public/`

### 4. Document Changes

- Update relevant documentation in `docs/`
- Add ADR if making architectural decisions
- Update `changelog.md` for significant changes

### 5. Submit PR

- Follow [Pull Request Guidelines](#pull-request-guidelines)
- Use the PR checklist template

---

## Content Creation

### Creating New Content

Content is created as Markdown files with YAML frontmatter in the `_src/` directory.

#### Note Example

```markdown
---
title: "My Note Title"
date: 2026-02-01 12:00 -05:00
tags: ["tag1", "tag2"]
---

Note content here.
```

#### Post Example

```markdown
---
title: "Blog Post Title"
date: 2026-02-01 12:00 -05:00
tags: ["technology", "tutorial"]
description: "Brief description for SEO and previews"
---

Full blog post content here.
```

### GitHub Issue Publishing

You can also create content via GitHub Issues using our templates:

- [Post a Note](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-note.yml)
- [Post a Bookmark](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-bookmark.yml)
- [Post Media](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-media.yml)

See `docs/github-issue-posting-guide.md` for complete documentation.

### VS Code Snippets

Use the provided snippets for quick content creation:
- `note` - New note template
- `post` - New post template
- `response` - New response template

See `.vscode/personalsite.code-snippets` for all available snippets.

---

## Code Contributions

### Adding a New Content Type

1. **Define types in `Domain.fs`**
   - Add metadata type with required fields
   - Implement ITaggable interface

2. **Add processing in `GenericBuilder.fs`**
   - Create AST processing function
   - Add to unified feed conversion

3. **Create views in `Views/`**
   - Add page view function
   - Add collection view if needed

4. **Update `Builder.fs`**
   - Add build function
   - Integrate into build pipeline

5. **Update `Program.fs`**
   - Call new build functions in correct order

6. **Document the change**
   - Add documentation in `docs/`
   - Create ADR if architectural decision
   - Update this CONTRIBUTING guide

### Code Style Guidelines

- **F# conventions**: Follow standard F# formatting
- **Type annotations**: Use explicit types in public APIs
- **Module organization**: One concern per module
- **Function size**: Consider refactoring if >20 lines
- **Comments**: Match existing comment style (minimal inline, docstrings for public APIs)

### Testing

```bash
# Run build with test content
dotnet run

# Run F# test scripts
dotnet fsi test-scripts/test-ast-parsing.fsx

# Check for compiler warnings
dotnet build 2>&1 | grep -i warning
```

---

## File Organization Guidelines

### Where to Put New Files

| File Type | Location | Example |
|-----------|----------|---------|
| New content | `_src/{content-type}/` | `_src/posts/my-post.md` |
| Feature documentation | `docs/` | `docs/new-feature.md` |
| Architecture decision | `docs/adr/` | `docs/adr/0006-new-decision.md` |
| F# source | Root or `Views/` or `Services/` | `NewModule.fs` |
| Development script | `Scripts/` | `Scripts/new-script.fsx` |
| Test script | `test-scripts/` | `test-scripts/test-new.fsx` |
| Project plan | `projects/active/` | `projects/active/new-project.md` |
| Static assets | `_src/assets/` | `_src/assets/image.png` |
| JSON data | `Data/` | `Data/new-data.json` |

### Files to NEVER Commit

These should be in `.gitignore`:
- `_public/` - Generated output
- `obj/` - Build artifacts
- `bin/` - Build output
- `node_modules/` - NPM packages
- `.env` - Environment secrets
- `*.user` - IDE user settings

### Cleaning Up Temporary Files

If you create temporary files during development:
1. Work in `/tmp/` for truly temporary files
2. Add to `.gitignore` if they might persist
3. Clean up before committing

---

## Pull Request Guidelines

### Before Submitting

- [ ] Code builds without errors: `dotnet build`
- [ ] Site generates correctly: `dotnet run`
- [ ] No new warnings introduced
- [ ] Documentation updated if needed
- [ ] Changelog updated for user-facing changes

### PR Title Format

```
type: Brief description

Examples:
feat: Add RSVP response type for events
fix: Correct date parsing for timezone handling
docs: Update contributing guidelines
refactor: Simplify tag processing logic
```

### PR Description Template

```markdown
## Summary
Brief description of changes.

## Changes
- List of specific changes
- Another change

## Testing
Describe how you tested the changes.

## Checklist
- [ ] Build passes
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Changelog updated (if user-facing)
```

---

## Checklist Templates

### New Feature Checklist

- [ ] Types defined in `Domain.fs`
- [ ] Processing added to `GenericBuilder.fs`
- [ ] Views created in `Views/`
- [ ] Build integration in `Builder.fs` and `Program.fs`
- [ ] RSS feed generation working
- [ ] Documentation added to `docs/`
- [ ] VS Code snippet added (if content type)
- [ ] Changelog entry added
- [ ] ADR created (if architectural decision)

### Bug Fix Checklist

- [ ] Root cause identified
- [ ] Fix implemented with minimal changes
- [ ] Regression tested
- [ ] Build passes
- [ ] Changelog updated

### Documentation Checklist

- [ ] Clear and accurate content
- [ ] Links verified
- [ ] Code examples tested
- [ ] Index updated (if new doc)
- [ ] Cross-references added

### Cleanup/Refactoring Checklist

- [ ] No functionality changes (unless intentional)
- [ ] Build passes before and after
- [ ] Output identical (where applicable)
- [ ] Internal links updated
- [ ] Documentation updated

---

## Getting Help

- **Documentation**: Check `docs/README.md` for the index
- **Architecture**: Review `docs/adr/` for design decisions
- **History**: See `changelog.md` and `projects/archive/`
- **Issues**: Open a GitHub issue for questions

## AI Assistant Guidelines

If you're an AI assistant working on this repository:

1. **Understand first**: Read `docs/core-infrastructure-architecture.md`
2. **Check ADRs**: Review `docs/adr/` for architectural decisions
3. **Follow patterns**: Match existing code style and patterns
4. **Test early**: Build after each change with `dotnet build`
5. **Document**: Update docs for any user-facing changes
6. **Small commits**: Make incremental, atomic changes
7. **No regressions**: Verify existing functionality still works

See the custom agents in `.github/agents/` for specialized assistance:
- `@content-creator` - Content and markdown
- `@fsharp-generator` - F# code changes
- `@build-automation` - Build and testing
- `@issue-publisher` - GitHub workflows
