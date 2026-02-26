# Personal IndieWeb Website

A modern F# static site generator implementing IndieWeb principles with unified content architecture, comprehensive RSS feeds, and progressive loading capabilities.

## ✨ Quick Publishing

**GitHub Issue Publishing**: Create content directly through GitHub Issues with automated workflows! 

### Available Issue Templates
- 📝 [**Post a Note**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-note.yml) - Quick note publishing
- 🔖 [**Post a Bookmark**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-bookmark.yml) - Share interesting links
- 💬 [**Post a Response**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-response.yml) - Replies, likes, and reposts
- 📸 [**Post Media**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-media.yml) - Photos and media content
- 🎵 [**Post a Playlist**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-playlist.yml) - Music playlists with Spotify and YouTube links
- 📚 [**Post a Book Review**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-review-book.yml) - Share book reviews
- 🎬 [**Post a Movie Review**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-review-movie.yml) - Share movie reviews
- 🎵 [**Post a Music Review**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-review-music.yml) - Share music reviews
- 🏢 [**Post a Business Review**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-review-business.yml) - Share business reviews
- 🛍️ [**Post a Product Review**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-review-product.yml) - Share product reviews

> **Note for Reviews**: Use the appropriate review template link above - each template automatically applies the correct labels. The workflow validates that both labels are present before processing.
> 
> **Note for Playlists**: Requires repository secrets `SPOTIFY_CLIENT_ID` and `SPOTIFY_CLIENT_SECRET` to be configured for Spotify API access.

📖 [**Publishing Guide**](docs/github-issue-posting-guide.md) - Complete documentation on GitHub Issue Template posting

GitHub Issue publishing provides native version control, better review workflows, and automated PR creation, replacing the previous Discord-based publishing system.

## 🏗️ Architecture Overview

This is a production-ready IndieWeb site built with F# featuring:
- **Unified Content Processing**: AST-based GenericBuilder pattern for all 8+ content types
- **Comprehensive Feed Architecture**: RSS 2.0 feeds with tag-based filtering (1,187+ feeds)
- **Progressive Loading**: Handles 1000+ content items without performance issues
- **Desert Theme Integration**: Modern responsive design with IndieWeb microformats2
- **External Library Support**: Proven patterns for integrating JavaScript libraries

## 📂 Repository Structure

### Core Application Files
- **Program.fs** - Main application entrypoint and build orchestration
- **Builder.fs** - High-level build functions coordinating content generation
- **Domain.fs** - Core types and ITaggable interface for unified content processing
- **GenericBuilder.fs** - Unified AST-based content processors for all content types
- **Loaders.fs** - File system operations and content loading utilities

### Infrastructure Modules
- **ASTParsing.fs** - Abstract Syntax Tree parsing for Markdown content
- **CustomBlocks.fs** - Custom Markdig extensions (media, reviews, venues, RSVP blocks)
- **BlockRenderers.fs** - HTML rendering for custom block types
- **MediaTypes.fs** - Media type detection and handling
- **OutputComparison.fs** - Build validation and output comparison utilities
- **SearchIndex.fs** - Build-time search index generation for client-side search
- **TextOnlyBuilder.fs** - Accessibility-first text-only site generation
- **Collections.fs** - Collections system (travel guides, starter packs, blogroll)
- **StarterPackSystem.fs** - Starter pack configuration and generation

### Views Architecture (Modular)
- **Views/LayoutViews.fs** - Page-level layouts and structural views
- **Views/ContentViews.fs** - Individual content type view functions
- **Views/CollectionViews.fs** - Collection and listing views with progressive loading
- **Views/ComponentViews.fs** - Reusable UI components and utilities
- **Views/FeedViews.fs** - RSS feed and aggregation views
- **Views/TagViews.fs** - Tag-related view functions
- **Views/TextOnlyViews.fs** - Accessibility-first text-only site views
- **Views/TravelViews.fs** - Travel collection-specific views with GPS integration
- **Views/Partials.fs** - Re-export layer maintaining backward compatibility
- **Views/Layouts.fs** - Base layout components
- **Views/Generator.fs** - View generation utilities

### Services
- **Services/Markdown.fs** - Markdown processing with custom block support
- **Services/Tag.fs** - Tag management and RSS feed generation
- **Services/Opml.fs** - OPML feed aggregation for subscription management
- **Services/Webmention.fs** - IndieWeb webmention integration

### Content Structure
- **_src/** - Source content in Markdown with YAML frontmatter
  - **posts/** - Long-form blog posts
  - **notes/** - Microblog note posts (IndieWeb notes)
  - **responses/** - Social responses (replies, likes, bookmarks, reposts)
  - **snippets/** - Code snippets with syntax highlighting
  - **wiki/** - Knowledge base and reference notes
  - **resources/presentations/** - Reveal.js presentations with custom layouts and resources
  - **media/** - Photo albums and media collections
  - **albums/** - Curated media collections with location metadata
  - **playlists/** - Music playlist collections with YouTube/Spotify links
  - **resources/** - Books, tools, and reference materials
  - **streams/** - Live stream recordings and resources

### Build Output
- **_public/** - Generated static site (deployment target)
  - Semantic URL structure following W3C "Cool URIs" principles
  - RSS feeds for all content types and tags
  - Progressive loading assets and JSON data files
  - Optimized CSS and JavaScript assets
  - `/text/` - Text-only accessibility site (<50KB pages)
  - `/collections/` - Curated collections (starter packs, travel guides, albums)
  - Service worker and PWA manifest for offline functionality

### Development Support
- **Data/** - JSON data files (blogroll, events, feeds, etc.)
- **Scripts/** - Development and maintenance scripts
- **docs/** - Architecture documentation and decision records
- **projects/** - Project management and development logs
- **test-scripts/** - F# test scripts for validation
- **_scratch/** - Development scratchpad for ideas and drafts

## 🔧 Key Features

### Content Management & Publishing
- **9 Content Types**: Posts, notes, responses, snippets, wiki, presentations, media, albums, playlists, resources
- **GitHub Issue Publishing**: Create note posts directly through GitHub Issues with automated PR workflow
- **Pinned Posts**: Pin important content to the top of your timeline via JSON configuration
- **Album Collections**: Curated photo/media groupings with location metadata and timeline integration
- **Playlist Collections**: Monthly music discoveries with YouTube/Spotify dual links for platform-agnostic listening
- **AST-Based Processing**: Unified GenericBuilder pattern replacing legacy repetitive functions
- **Custom Blocks**: Media galleries, reviews, venue information, RSVP responses
- **Tag System**: Automatic tag extraction and RSS feed generation (1,187+ tag feeds)
- **Untagged Content Discovery**: Automatic detection and organization of content needing tags

### Feed Architecture & Discovery
- **Comprehensive RSS**: Individual feeds for each content type
- **Tag-Based Filtering**: RSS feeds for every tag with proper category metadata
- **Unified Feed**: Combined feed of all content types
- **OPML Support**: Subscription management with feed discovery
- **Starter Packs System**: Curated RSS feed collections for easy topic-based subscriptions
- **Site-wide Search**: Client-side fuzzy search with Fuse.js for 1,130+ content items

### Collections & Organization
- **Travel Collections**: GPS-enabled travel guides with GPX file generation and map integration
- **Blogroll/Podroll**: Curated link collections with RSS/OPML export
- **Starter Packs**: Topic-based RSS feed collections inspired by BlueSky
- **Album Collections**: Photo/media galleries with location metadata
- **Playlist Collections**: Monthly music discoveries ("Crate Finds") with YouTube/Spotify links
- **Content Collections**: Flexible grouping system for any content organization

### Performance & UX
- **Progressive Loading**: Client-side chunked loading for large content volumes
- **Progressive Web App (PWA)**: Offline-first functionality with service worker caching
- **Content Sharing**: QR code generation, Web Share API, and copy permalink buttons on every page
- **Back to Top Button**: Scroll-based navigation with mobile optimization
- **Explicit Home Navigation**: Research-backed navigation improvements for mixed audiences
- **Desert Theme**: Modern responsive design with accessibility features
- **IndieWeb Compliance**: Full microformats2 markup and webmention support
- **Timezone-Aware Parsing**: Consistent date handling across all environments

### Accessibility & Universal Design
- **Text-Only Site**: Complete accessibility-first website at `/text/` subdirectory
- **WCAG 2.1 AA Compliance**: Full keyboard navigation, screen reader support, reduced motion
- **2G Network Optimization**: <50KB page loads for universal device compatibility
- **Flip Phone Support**: Core functionality accessible through basic mobile browsers

### Presentations
- **Custom Layout System**: 15 pre-built CSS layout classes for Reveal.js presentations
- **VS Code Snippets**: Quick slide creation with layout templates
- **Responsive Design**: Mobile-optimized presentation viewing

### Development Workflow
- **Custom Copilot Agents**: Multi-agentic system with orchestrator + 5 specialist agents for enhanced development assistance (see `.github/agents/`)
- **Emacs Org-capture Templates**: 33 hierarchical templates for rapid content creation with 3-key navigation (see `docs/org-capture-templates.md`)
- **VS Code Integration**: Complete snippet library aligned with Domain.fs (17 content types + layout snippets)
- **Build Validation**: Automated testing and output comparison
- **Azure Integration**: Native redirects via Static Web Apps configuration
- **GitHub Actions**: Automated workflows for content publishing, broken link checking, and deployments
- **Migration Patterns**: Proven feature flag approach for safe updates

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK
- F# compiler

### Build Commands
```bash
# Restore packages
dotnet restore

# Build and generate site
dotnet run

# Start development server
./start-server.ps1    # Windows
```

### Content Creation

**Two powerful authoring systems** for your editor preference:

#### Emacs Org-capture (33 templates)
Hierarchical 3-key navigation with automatic file creation:
- **Content Types** (9): articles, notes, media, albums, playlists, presentations, wiki, etc.
- **Response Types** (2): responses (with type selection), bookmarks
- **Reviews** (1): reviews using :::review::: blocks
- **Custom Blocks** (15): media, reviews, resume components
- **Markdown Helpers** (6): code blocks, blockquotes, links, images

**Setup:** See `docs/org-capture-templates.md` for complete Emacs configuration
**Usage:** `C-c c l` → category → specific type

#### VS Code Snippets (17+ templates)
Visual editor snippets for inline expansion:
- `post` - Blog post template
- `note` - Microblog note
- `response` - Social response (bookmark, like, reply)
- `snippet` - Code snippet
- `wiki` - Knowledge base entry
- `album-collection` - Photo/media album with location metadata
- `travel-collection` - Travel guide with GPS coordinates
- `presentation` - Reveal.js presentation with custom layouts
- Plus content blocks, metadata helpers, and presentation layouts

### Presentation Layouts
Create professional presentations with 15 custom layout classes:
- `layout-two-column` / `layout-three-column` - Multi-column layouts
- `layout-split-70-30` / `layout-split-30-70` - Asymmetric splits
- `layout-image-left` / `layout-image-right` - Image + content layouts
- `layout-centered` / `layout-big-text` - Impact layouts
- And 7 more specialized layouts with VS Code snippets

## 🤖 Custom Copilot Agents

This repository features a **multi-agentic Copilot workflow system** with specialized agents for enhanced development assistance. The system includes an orchestrator agent that routes tasks to domain specialists, enabling contextually-aware code suggestions and automated workflows.

### Agent Architecture

**Orchestrator Agent** (`@orchestrator`)
- Meta-coordinator that analyzes requests and routes to appropriate specialists
- Coordinates multi-step workflows across multiple agents
- Maintains architectural consistency across changes
- Provides decision trees for single vs multi-agent tasks

**Specialist Agents**:
1. **Content Creator** (`@content-creator`) - Content types, markdown blocks, YAML frontmatter, IndieWeb standards
2. **F# Generator** (`@fsharp-generator`) - F# codebase, GenericBuilder pattern, AST processing, RSS feeds, ViewEngine rendering
3. **Issue Publisher** (`@issue-publisher`) - GitHub Actions workflows, issue templates, S3 integration, automation pipelines
4. **Build Automation** (`@build-automation`) - Build scripts, validation, testing, performance optimization, orchestration

### Usage Examples

**Content-Only Changes**:
```
"Create a new presentation with custom layout blocks" → @content-creator
```

**F# Code Changes**:
```
"Add a new content type to GenericBuilder" → @fsharp-generator
```

**GitHub Workflow Changes**:
```
"Fix the S3 upload workflow for media files" → @issue-publisher
```

**Build & Testing**:
```
"Add validation for RSS feed generation" → @build-automation
```

**Multi-Agent Workflows**:
```
"Implement a new content type"
→ @orchestrator coordinates:
  1. @content-creator: Define YAML schema and custom blocks
  2. @fsharp-generator: Implement Domain type and processor
  3. @build-automation: Create validation tests
  4. @issue-publisher: Create GitHub issue template (optional)
```

### Agent Configuration

All agents are defined in `.github/agents/` with YAML frontmatter following GitHub's Custom Agents specification. Each agent includes:
- Domain expertise and responsibilities
- Integration patterns with other agents
- Code examples and validation checklists
- Reference resources and documentation links

**Documentation**: See individual agent files in `.github/agents/` for complete specifications and usage patterns.

## 📊 Project Stats

- **Build Performance**: ~1.3s build times (89% improvement from architecture cleanup)
- **Content Scale**: 1,130+ content items across 9 content types  
- **Feed Generation**: 1,187+ RSS feeds (content types + tags + unified)
- **Code Quality**: 445+ lines of legacy code removed, unified patterns throughout
- **URL Health**: 97.8% reduction in broken links through comprehensive redirect strategy
- **Search Capability**: Site-wide search across all 1,130+ content items with sub-100ms performance
- **Accessibility**: Complete text-only site with <50KB page loads for universal device compatibility
- **PWA Support**: Offline-first functionality with intelligent caching strategies

## 🔗 Links

- **Live Site**: [https://www.lqdev.me](https://www.lqdev.me)
- **Contributing**: [CONTRIBUTING.md](CONTRIBUTING.md) - Guidelines for contributors and AI assistants
- **Architecture Docs**: [docs/core-infrastructure-architecture.md](docs/core-infrastructure-architecture.md)
- **Architectural Decision Records**: [docs/adr/](docs/adr/) - Key design decisions documented
- **Development Logs**: [projects/archive/](projects/archive/) and [changelog.md](changelog.md)
- **Migration History**: Complete 8-phase migration documented in project archives