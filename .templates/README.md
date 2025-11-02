# Org Capture Templates

This directory contains **Emacs org-capture templates** for rapid content creation in this static site.

## Quick Reference

### 📄 Content Types (13 templates)
- `article.txt` - Long-form blog articles
- `post.txt` - General blog posts  
- `note.txt` - Microblog notes
- `note-crate-finds.txt` - Monthly music discoveries
- `photo.txt` - Photo posts
- `video.txt` - Video posts
- `album.txt` - Photo albums with metadata
- `album-collection.txt` - Album collections
- `playlist-collection.txt` - Music playlist collections
- `snippet.txt` - Code snippets
- `wiki.txt` - Knowledge base entries
- `presentation.txt` - Reveal.js presentations
- `livestream.txt` - Live stream recordings

### 💬 Response Types (5 templates)
- `response.txt` - Generic response (with type selection)
- `reply.txt` - Reply to posts
- `reshare.txt` - Reshare/repost content
- `star.txt` - Favorite/like posts
- `bookmark.txt` - Bookmark links

### 📚 Book Reviews (1 template)
- `book.txt` - Book reviews with ratings and notes

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

## Total: 40 Templates

## Documentation

See **[docs/org-capture-templates.md](../docs/org-capture-templates.md)** for:
- Complete setup instructions
- Emacs configuration
- Hierarchical keybinding structure
- Usage examples and workflows
- Template customization guide
- Troubleshooting tips

## Quick Start

1. Add the Emacs configuration from `docs/org-capture-templates.md` to your `~/.emacs.d/init.el`
2. Set `lqdev-site-path` to point to this repository
3. Press `C-c c l` to access the template hierarchy
4. Navigate: `l` (lqdev) → category → specific type
5. Fill prompts and create content!

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
