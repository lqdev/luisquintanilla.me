# Documentation

This directory contains guides and reference documentation for the website's features and content creation workflows.

> **About this index**: This is a *curated* index highlighting evergreen how-to guides, reference material, and current architecture docs. It is not an exhaustive file listing — historical implementation summaries and one-off fix notes live in `docs/` but are intentionally not indexed here. Completed-project artifacts are archived under [`../archive/summaries/`](../archive/summaries/) and the [projects archive](../projects/archive/). Project narratives are recorded in the [changelog](../changelog.md).

## 📚 Documentation Index

### 🚀 Publishing Workflows

- **[GitHub Issue Posting Guide](github-issue-posting-guide.md)** - Create content directly through GitHub Issues
- **[GitHub Issue Posting Config](github-issue-posting-config.md)** - Configuration reference for issue-based publishing
- **[Bookmark Publishing Workflow](bookmark-publishing-workflow.md)** - Publishing bookmark content
- **[Response Publishing Workflow](response-publishing-workflow.md)** - Publishing responses (likes, replies, reposts)
- **[Media Publishing Workflow](media-publishing-workflow.md)** - Publishing photos and media
- **[Process Read-Later Workflow](process-read-later-workflow.md)** - Read-it-later extension → published bookmark pipeline

### 📖 Content Creation Guides

- **[Org-capture Templates](org-capture-templates.md)** - Emacs hierarchical authoring (33 templates)
- **[VS Code Snippets](vs-code-snippets-modernization.md)** - Editor snippets aligned with Domain.fs
- **[How to Create Starter Packs](how-to-create-starter-packs.md)** - RSS/OPML feed collections
- **[How to Create Collections](how-to-create-collections.md)** - Blogroll, podroll, and custom collections
- **[Travel Guide How-To](travel-guide-howto.md)** - Travel guides with GPS and GPX generation
- **[Resume Feature](resume-feature.md)** - Create and maintain professional resume
- **[Custom Presentation Layouts](custom-presentation-layouts.md)** - 15 CSS layout classes for Reveal.js presentations
- **[Tag Standardization Guidelines](tag-standardization-guidelines.md)** - Tag naming and normalization conventions

### 🎯 Feature Reference

- **[PWA Implementation](PWA_IMPLEMENTATION.md)** - Progressive Web App with offline functionality
- **[Content Sharing Features](content-sharing-features.md)** - QR code generation, Web Share API, and permalink buttons
- **[Album Collections](ALBUM_COLLECTIONS.md)** - Photo/media collections with location metadata
- **[Playlist Collections](playlist-collections.md)** - Monthly music discoveries with YouTube/Spotify dual links
- **[Enhanced Content Discovery](enhanced-content-discovery-implementation.md)** - Site-wide search functionality
- **[Text-Only Site](text-only-site.md)** - Complete accessibility-first website (<50KB pages)
- **[Pinned Posts Feature](pinned-posts-feature.md)** - Pin important content to timeline top

### 🏗️ Architecture & Technical

- **[Architecture Decision Records](adr/)** - Key technical decisions and their rationale (ADRs)
- **[Core Infrastructure Architecture](core-infrastructure-architecture.md)** - System architecture overview
- **[Feed Architecture](feed-architecture.md)** - RSS feed generation and organization
- **[URL Alignment Architecture](url-alignment-architecture-decisions.md)** - URL structure decisions
- **[Review System Architecture](review-system-architecture-recommendation.md)** - Review system design
- **[Web API Implementation](WEB_API_IMPLEMENTATION_REPORT.md)** - API enhancements and endpoints
- **[Web API Enhancements Summary](WEB_API_ENHANCEMENTS_SUMMARY.md)** - API feature additions

### 🌐 ActivityPub / Fediverse

- **[ActivityPub Documentation](activitypub/)** - Federation architecture, deployment, and implementation status
- **[Architecture Overview](activitypub/ARCHITECTURE-OVERVIEW.md)** - High-level federation design
- **[Implementation Status](activitypub/implementation-status.md)** - Current phase tracking
- **[Deployment Guide](activitypub/deployment-guide.md)** - Azure deployment instructions

### 🔧 Workflows & Maintenance

- **[Broken Link Checker](broken-link-checker.md)** - Automated link validation
- **[Workflow Caching Optimization](workflow-caching-optimization.md)** - Build performance improvements

### 🤖 Development & AI

- **[AI Memex System](ai-memex-system.md)** - AI-authored content type and Knowledge Graph documentation
- **[Custom Agents Implementation](custom-agents-implementation.md)** - Multi-agentic Copilot workflow system

## 📋 Documentation Standards

All documentation follows these principles:

### Completeness
- Cover complete workflows from start to finish
- Include troubleshooting sections for common issues
- Provide real-world examples and code samples
- Document both successful patterns and lessons learned

### Clarity
- Write for content creators and contributors
- Explain the "why" behind architectural decisions
- Use clear headings and logical organization
- Include visual examples where helpful

### Currency
- Keep documentation up-to-date with latest implementation
- Document new features as they're added
- Archive outdated approaches with migration paths
- Link to related documentation for context

### Technical Depth
- Explain underlying architecture and implementation
- Document key files and functions
- Provide code examples and configuration snippets
- Include integration points and dependencies

## 🆕 Contributing Documentation

When adding new features or content types, please:

1. **Create Comprehensive Documentation**
   - Use existing docs as templates
   - Include overview, usage, examples, and troubleshooting
   - Document all configuration options and data formats

2. **Update This Index**
   - Add new documentation to appropriate section
   - Use descriptive titles that match the file content
   - Keep sections organized by category

3. **Cross-Reference Related Docs**
   - Link to related documentation within your doc
   - Update other docs that reference your feature
   - Maintain bidirectional links where appropriate

4. **Test All Instructions**
   - Verify all code examples work correctly
   - Test all commands and scripts
   - Validate all file paths and URLs
   - Ensure snippets and templates are current

## 🔗 Quick Links

- **[Main README](../README.md)** - Repository overview and getting started
- **[Contributing Guide](../CONTRIBUTING.md)** - Project guide for contributors and AI assistants
- **[Architecture Decision Records](adr/)** - Key technical decisions (ADRs)
- **[Changelog](../changelog.md)** - Project history and completed features
- **[Projects Archive](../projects/archive/)** - Detailed project implementation logs
- **[VS Code Snippets](../.vscode/)** - Content creation shortcuts