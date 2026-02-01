# Documentation

This directory contains comprehensive guides and documentation for the website's features and content creation workflows.

## 📚 Documentation Index

### 📊 Repository Audit & Governance

- **[Repository Audit Report](REPOSITORY_AUDIT_REPORT.md)** - Comprehensive analysis of all files, scripts, and code

### 📋 Architectural Decision Records

The **[ADR Index](adr/README.md)** contains documented architectural decisions:

- **[ADR-0001](adr/0001-unified-content-processing.md)** - Unified Content Processing with GenericBuilder
- **[ADR-0002](adr/0002-indieweb-microformats.md)** - IndieWeb Microformats Integration
- **[ADR-0003](adr/0003-static-site-architecture.md)** - F# Static Site Generator Architecture
- **[ADR-0004](adr/0004-progressive-loading.md)** - Progressive Loading for Large Content Volumes
- **[ADR-0005](adr/0005-file-organization.md)** - Repository File Organization

### 🚀 Getting Started & Publishing

- **[GitHub Issue Posting Guide](github-issue-posting-guide.md)** - Create content directly through GitHub Issues
- **[Bookmark Publishing Workflow](bookmark-publishing-workflow.md)** - Publishing bookmark content
- **[Response Publishing Workflow](response-publishing-workflow.md)** - Publishing responses (likes, replies, reposts)
- **[Media Publishing Workflow](media-publishing-workflow.md)** - Publishing photos and media

### 🎯 Core Features (July 2025+)

- **[Resume Feature](resume-feature.md)** - Professional resume with custom Markdig blocks
- **[Custom Presentation Layouts](custom-presentation-layouts.md)** - 15 CSS layout classes for Reveal.js presentations
- **[PWA Implementation](PWA_IMPLEMENTATION.md)** - Progressive Web App with offline functionality
- **[Content Sharing Features](content-sharing-features.md)** - QR code generation, Web Share API, and permalink buttons
- **[Album Collections](ALBUM_COLLECTIONS.md)** - Photo/media collections with location metadata
- **[Playlist Collections](playlist-collections.md)** - Monthly music discoveries with YouTube/Spotify dual links
- **[Starter Packs System](how-to-create-starter-packs.md)** - Curated RSS feed collections
- **[Enhanced Content Discovery](enhanced-content-discovery-implementation.md)** - Site-wide search functionality
- **[Text-Only Site](text-only-site.md)** - Complete accessibility-first website (<50KB pages)
- **[Pinned Posts Feature](pinned-posts-feature.md)** - Pin important content to timeline top
- **[Back to Top Button](back-to-top-button-implementation.md)** - Scroll-based navigation

### 📖 Content Creation Guides

- **[Resume Feature](resume-feature.md)** - Create and maintain professional resume
- **[How to Create Starter Packs](how-to-create-starter-packs.md)** - RSS/OPML feed collections
- **[How to Create Collections](how-to-create-collections.md)** - Blogroll, podroll, and custom collections
- **[Travel Guide How-To](travel-guide-howto.md)** - Travel guides with GPS and GPX generation
- **[VS Code Snippets Modernization](vs-code-snippets-modernization.md)** - Content creation shortcuts

### 🏗️ Architecture & Technical

- **[Core Infrastructure Architecture](core-infrastructure-architecture.md)** - System architecture overview
- **[Feed Architecture](feed-architecture.md)** - RSS feed generation and organization
- **[Feature Flag Pattern](feature-flag-pattern.md)** - Safe feature deployment strategy
- **[Web API Implementation](WEB_API_IMPLEMENTATION_REPORT.md)** - API enhancements and endpoints
- **[Web API Enhancements Summary](WEB_API_ENHANCEMENTS_SUMMARY.md)** - API feature additions

### 🔧 Workflows & Optimization

- **[Workflow Caching Optimization](workflow-caching-optimization.md)** - Build performance improvements
- **[Workflow Caching Before/After](workflow-caching-before-after.md)** - Performance comparison
- **[Broken Link Checker](broken-link-checker.md)** - Automated link validation
- **[URL Alignment Architecture](url-alignment-architecture-decisions.md)** - URL structure decisions

### 📱 Accessibility & UX

- **[Explicit Home Navigation](explicit-home-navigation-implementation.md)** - Research-backed navigation
- **[Target URL Display](target-url-display-implementation.md)** - Response and bookmark URL handling
- **[Bookmarks Landing Page](bookmarks-landing-page-implementation.md)** - Content type landing pages

### 🔄 Migration & Maintenance

- **[Migration Guide - Posts](migration-guide-posts.md)** - Post migration documentation
- **[Migration Guide - Snippets](migration-guide-snippets.md)** - Snippet migration
- **[Migration Guide - Wiki](migration-guide-wiki.md)** - Wiki migration
- **[Review Migration Summary](review-migration-summary.md)** - Review system migration
- **[Review System Architecture](review-system-architecture-recommendation.md)** - Review system design

### 🤖 Development & AI

- **[AI Memory Management System](ai-memory-management-system.md)** - AI-assisted development patterns
- **[Copilot Partnership Evolution](copilot-partnership-evolution-autonomy.md)** - GitHub Copilot workflows
- **[ActivityPub Implementation Plan](activitypub-implementation-plan.md)** - Fediverse integration planning

### 🔍 Content Analysis & Optimization

- **[Content Processor Optimization](content-processor-optimization-summary.md)** - Performance improvements
- **[Content Volume HTML Parsing](content-volume-html-parsing-discovery.md)** - Large content handling

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
- **[Changelog](../changelog.md)** - Project history and completed features
- **[Projects Archive](../projects/archive/)** - Detailed project implementation logs
- **[VS Code Snippets](../.vscode/)** - Content creation shortcuts