# Personal IndieWeb Website

A modern F# static site generator implementing IndieWeb principles with unified content architecture, comprehensive RSS feeds, and progressive loading capabilities.

## ✨ Quick Publishing

**New**: You can now create note posts directly through GitHub Issues! 

👉 [**Create a Note Post**](https://github.com/lqdev/luisquintanilla.me/issues/new?template=post-note.yml) - Use our GitHub Issue Template for quick publishing

📖 [**Publishing Guide**](docs/github-issue-posting-guide.md) - Complete documentation on how to use GitHub Issue Template posting

This replaces the previous Discord-based publishing workflow with a native GitHub solution that provides better version control and review capabilities.

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

### Views Architecture (Modular)
- **Views/LayoutViews.fs** - Page-level layouts and structural views
- **Views/ContentViews.fs** - Individual content type view functions
- **Views/CollectionViews.fs** - Collection and listing views with progressive loading
- **Views/ComponentViews.fs** - Reusable UI components and utilities
- **Views/FeedViews.fs** - RSS feed and aggregation views
- **Views/TagViews.fs** - Tag-related view functions
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
  - **presentations/** - Reveal.js presentations with resources
  - **media/** - Photo albums and media collections
  - **resources/** - Books, tools, and reference materials
  - **streams/** - Live stream recordings and resources

### Build Output
- **_public/** - Generated static site (deployment target)
  - Semantic URL structure following W3C "Cool URIs" principles
  - RSS feeds for all content types and tags
  - Progressive loading assets and JSON data files
  - Optimized CSS and JavaScript assets

### Development Support
- **Data/** - JSON data files (blogroll, events, feeds, etc.)
- **Scripts/** - Development and maintenance scripts
- **docs/** - Architecture documentation and decision records
- **projects/** - Project management and development logs
- **test-scripts/** - F# test scripts for validation
- **_scratch/** - Development scratchpad for ideas and drafts

## 🔧 Key Features

### Content Processing
- **8 Content Types**: Posts, notes, responses, snippets, wiki, presentations, media, resources
- **AST-Based Processing**: Unified GenericBuilder pattern replacing legacy repetitive functions
- **Custom Blocks**: Media galleries, reviews, venue information, RSVP responses
- **Tag System**: Automatic tag extraction and RSS feed generation (1,187+ tag feeds)

### Feed Architecture
- **Comprehensive RSS**: Individual feeds for each content type
- **Tag-Based Filtering**: RSS feeds for every tag with proper category metadata
- **Unified Feed**: Combined feed of all content types
- **OPML Support**: Subscription management with feed discovery

### Performance & UX
- **Progressive Loading**: Client-side chunked loading for large content volumes
- **Desert Theme**: Modern responsive design with accessibility features
- **IndieWeb Compliance**: Full microformats2 markup and webmention support
- **External Libraries**: Proven integration patterns (Reveal.js, syntax highlighting)

### Development Workflow
- **VS Code Integration**: Complete snippet library aligned with Domain.fs
- **Build Validation**: Automated testing and output comparison
- **Hot Reload**: Development server with live reloading
- **Migration Patterns**: Proven feature flag approach for safe updates

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
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
Use VS Code snippets for efficient content creation:
- `post` - Blog post template
- `note` - Microblog note
- `response` - Social response (bookmark, like, reply)
- `snippet` - Code snippet
- `wiki` - Knowledge base entry
- Plus 12 more content types with full metadata templates

## 📊 Project Stats

- **Build Performance**: ~1.3s build times (79% improvement from architecture cleanup)
- **Content Scale**: 1,129+ content items across 8 content types  
- **Feed Generation**: 1,187+ RSS feeds (content types + tags + unified)
- **Code Quality**: 445+ lines of legacy code removed, unified patterns throughout
- **URL Health**: 97.8% reduction in broken links through comprehensive redirect strategy

## 🔗 Links

- **Live Site**: [https://www.lqdev.me](https://www.lqdev.me)
- **Architecture Docs**: `docs/core-infrastructure-architecture.md`
- **Development Logs**: `projects/archive/` and `changelog.md`
- **Migration History**: Complete 8-phase migration documented in project archives