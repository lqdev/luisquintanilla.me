# Org Capture Templates

This directory contains **Emacs org-capture templates** for rapid content creation in this static site.

## Quick Reference

### 📄 Content Types (9 templates)
- `article.txt` - Long-form blog articles
- `note.txt` - Microblog notes
- `media.txt` - Media posts (images, videos, audio) in _src/media
- `album-collection.txt` - Album collections
- `playlist-collection.txt` - Music playlist collections
- `snippet.txt` - Code snippets
- `wiki.txt` - Knowledge base entries
- `presentation.txt` - Reveal.js presentations
- `livestream.txt` - Live stream recordings

### 💬 Response Types (2 templates)
- `response.txt` - Generic response with type selection (reply/star/reshare)
- `bookmark.txt` - Bookmark links (separate directory)

### 📚 Reviews (1 template)
- `review.txt` - Reviews using :::review::: blocks for all review types (book/movie/music/business/product) in _src/reviews

### 🎨 Custom Blocks (15 templates)

**Media Blocks (5):**
- `media-image.txt` - Image with caption
- `media-video.txt` - Video with caption
- `media-audio.txt` - Audio content
- `media-document.txt` - Document embedding
- `media-link.txt` - Link preview

**Review Blocks (5):**
- `review-book.txt` - Book reviews
- `review-movie.txt` - Movie reviews
- `review-music.txt` - Music reviews
- `review-product.txt` - Product reviews
- `review-business.txt` - Business/venue reviews

**Resume Blocks (5):**
- `resume-experience.txt` - Work experience
- `resume-skills.txt` - Skill categories
- `resume-project.txt` - Project showcases
- `resume-education.txt` - Education history
- `resume-testimonial.txt` - Testimonials

### 🛠️ Markdown Helpers (6 templates)
- `helper-datetime.txt` - Current datetime with timezone
- `helper-blockquote.txt` - Blockquote formatting
- `helper-codeblock.txt` - Fenced code blocks
- `helper-link.txt` - Markdown links
- `helper-image.txt` - Image embedding
- `helper-youtube.txt` - YouTube video embedding

## Total: 33 Templates

## Documentation

See **[docs/org-capture-templates.md](../docs/org-capture-templates.md)** for:
- Complete setup instructions
- Emacs configuration
- Hierarchical keybinding structure
- Usage examples and workflows
- Template customization guide
- Troubleshooting tips

## Quick Start

### Option 1: Use Standalone Config File
1. Copy `emacs-config.el` to your Emacs configuration directory
2. Update `lqdev-site-path` in the file to point to this repository
3. Add `(load-file "~/.emacs.d/org-capture-templates.el")` to your `init.el`

### Option 2: Add to Existing Config
1. Copy the configuration from `docs/org-capture-templates.md` to your `~/.emacs.d/init.el`
2. Set `lqdev-site-path` to point to this repository

### Usage
1. Press `C-c c l` to access the template hierarchy
2. Navigate: `l` (lqdev) → category → specific type
3. Fill prompts and create content!

## Template Syntax

Templates use Emacs org-capture expansion syntax:
- `%^{Prompt}` - Interactive prompt
- `%^{Prompt|default|opt1}` - Prompt with options
- `%<%Y-%m-%d %H:%M -05:00>` - Current datetime
- `%?` - Final cursor position

## Relationship to VS Code Snippets

This repository also has VS Code snippets in `.vscode/*.code-snippets` that provide similar functionality for VS Code users. Choose the system that matches your editor!

---

**Purpose:** Repository-specific templates for Emacs org-capture
**Maintained by:** Site content contributors
**Last Updated:** 2024-11-02
