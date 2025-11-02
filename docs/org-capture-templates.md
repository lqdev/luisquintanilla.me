# Org Capture Templates Documentation

## Overview

The `.templates` directory contains **Emacs org-capture templates** for rapid content authoring in this static site. These templates are specifically designed for this repository and provide consistent frontmatter structure for all content types supported by the F# static site generator.

## Purpose

Org-capture templates enable:
- **Quick content creation** with keyboard shortcuts in Emacs
- **Consistent metadata structure** aligned with Domain.fs type definitions
- **Interactive prompts** for required fields with sensible defaults
- **Direct integration** with the content workflow for this site

## Template Categories

The templates are organized into four main categories:

### 1. Content Types (9 templates)
Primary content types for posts, notes, and media:
- `article.txt` - Long-form blog articles
- `note.txt` - Microblog notes (IndieWeb)
- `media.txt` - Media posts (images, videos, audio) in _src/media
- `album-collection.txt` - Album collections
- `playlist-collection.txt` - Music playlist collections
- `snippet.txt` - Code snippets with syntax highlighting
- `wiki.txt` - Knowledge base entries
- `presentation.txt` - Reveal.js presentations
- `livestream.txt` - Live stream recordings

### 2. Response Types (2 templates)
IndieWeb social responses:
- `response.txt` - Generic response with type selection (reply/star/reshare)
- `bookmark.txt` - Bookmark links (separate directory)

### 3. Reviews (1 template)
Reviews using the :::review::: custom block:
- `review.txt` - Reviews for books, movies, music, products, businesses

### 4. Custom Blocks (15 templates)
Markdown custom block syntax for rich content:

**Media Blocks:**
- `media-image.txt` - Single image with caption
- `media-video.txt` - Single video with caption
- `media-audio.txt` - Audio content
- `media-document.txt` - Document embedding
- `media-link.txt` - Link with preview

**Review Blocks:**
- `review-book.txt` - Book reviews with ratings
- `review-movie.txt` - Movie reviews
- `review-music.txt` - Music reviews
- `review-product.txt` - Product reviews
- `review-business.txt` - Business/venue reviews

**Resume Blocks:**
- `resume-experience.txt` - Work experience entries
- `resume-skills.txt` - Skill categories
- `resume-project.txt` - Project showcases
- `resume-education.txt` - Education history
- `resume-testimonial.txt` - Testimonials and recommendations

### 5. Markdown Helpers (6 templates)
Common markdown patterns:
- `helper-datetime.txt` - Current datetime with timezone
- `helper-blockquote.txt` - Blockquote formatting
- `helper-codeblock.txt` - Fenced code blocks
- `helper-link.txt` - Markdown links
- `helper-image.txt` - Image embedding
- `helper-youtube.txt` - YouTube video embedding

## Template Syntax

### Org-capture Placeholders

Templates use Emacs org-capture expansion syntax:

| Syntax | Description | Example |
|--------|-------------|---------|
| `%^{Prompt}` | Interactive prompt for user input | `%^{Title}` |
| `%^{Prompt\|default\|opt1\|opt2}` | Prompt with default and options | `%^{Status\|Read\|Wishlist}` |
| `%<%Y-%m-%d %H:%M -05:00>` | Current datetime with format | `2024-11-02 19:30 -05:00` |
| `%?` | Final cursor position after expansion | Place at content start |

### Date Format

All templates use **EST/EDT timezone (`-05:00`)** to match the site's timezone configuration:
- Format: `%<%Y-%m-%d %H:%M -05:00>`
- Example: `2024-11-02 19:30 -05:00`

This ensures consistent date parsing in the F# Domain.fs types and proper chronological ordering.

## Emacs Configuration

### Basic Setup

Add this to your Emacs configuration file (`~/.emacs.d/init.el` or `~/.emacs`):

```elisp
;; Load org-capture
(require 'org-capture)

;; Define the path to your repository
(setq lqdev-site-path "~/path/to/luisquintanilla.me/")

;; Define org-capture templates for lqdev.me site
(setq org-capture-templates
      '(
        ;; Main content types hierarchy
        ("l" "lqdev.me Content")
        
        ;; Posts and Articles (lp*)
        ("lp" "Post Types")
        ("lpa" "Article" plain
         (file (lambda () (concat lqdev-site-path "_src/posts/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/article.txt")))
         :empty-lines-after 1)
        
        ;; Notes (ln*)
        ("ln" "Note Types")
        ("lnn" "Note" plain
         (file (lambda () (concat lqdev-site-path "_src/notes/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/note.txt")))
         :empty-lines-after 1)
        
        ;; Media (lm*)
        ("lm" "Media Types")
        ("lmm" "Media Post" plain
         (file (lambda () (concat lqdev-site-path "_src/media/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/media.txt")))
         :empty-lines-after 1)
        
        ("lma" "Album Collection" plain
         (file (lambda () (concat lqdev-site-path "_src/albums/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/album-collection.txt")))
         :empty-lines-after 1)
        
        ("lmp" "Playlist Collection" plain
         (file (lambda () (concat lqdev-site-path "_src/playlists/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/playlist-collection.txt")))
         :empty-lines-after 1)
        
        ;; Responses (lr*)
        ("lr" "Response Types")
        ("lrr" "Response" plain
         (file (lambda () (concat lqdev-site-path "_src/responses/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/response.txt")))
         :empty-lines-after 1)
        
        ("lrb" "Bookmark" plain
         (file (lambda () (concat lqdev-site-path "_src/bookmarks/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/bookmark.txt")))
         :empty-lines-after 1)
        
        ;; Resources (lo*)
        ("lo" "Resources")
        ("lov" "Review" plain
         (file (lambda () (concat lqdev-site-path "_src/reviews/library/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/review.txt")))
         :empty-lines-after 1)
        
        ("los" "Snippet" plain
         (file (lambda () (concat lqdev-site-path "_src/snippets/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/snippet.txt")))
         :empty-lines-after 1)
        
        ("low" "Wiki" plain
         (file (lambda () (concat lqdev-site-path "_src/wiki/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/wiki.txt")))
         :empty-lines-after 1)
        
        ("lop" "Presentation" plain
         (file (lambda () (concat lqdev-site-path "_src/resources/presentations/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/presentation.txt")))
         :empty-lines-after 1)
        
        ("lol" "Livestream" plain
         (file (lambda () (concat lqdev-site-path "_src/streams/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/livestream.txt")))
         :empty-lines-after 1)
        ))

;; Optional: Set a global keybinding for org-capture
(global-set-key (kbd "C-c c") 'org-capture)
```

### Keybinding Hierarchy

The configuration uses a **hierarchical 3-key system**:

1. **First key: `l`** - All lqdev.me content
2. **Second key**: Content category
   - `p` - Posts and articles
   - `n` - Notes
   - `m` - Media (media posts, albums, playlists)
   - `r` - Responses (IndieWeb social)
   - `o` - Resources (reviews, wiki, presentations)
3. **Third key**: Specific content type

**Examples:**
- `C-c c l p a` - Create article
- `C-c c l n n` - Create note
- `C-c c l r b` - Create bookmark
- `C-c c l m p` - Create photo post
- `C-c c l o w` - Create wiki entry

### Directory Structure Reference

Templates automatically create files in the correct directories:

```
_src/
├── posts/           # Articles and blog posts (lpa, lpp)
├── notes/           # Microblog notes (lnn)
├── responses/       # Social responses (lrr)
├── bookmarks/       # Bookmarks (lrb)
├── media/           # Media posts (lmm)
├── albums/          # Album collections (lma)
├── playlists/       # Music playlists (lmp)
├── snippets/        # Code snippets (los)
├── wiki/            # Knowledge base (low)
├── streams/         # Livestreams (lol)
└── reviews/
    └── library/     # Reviews (lov)
```

## Using Templates

### Quick Start

1. **Install and configure Emacs** with the configuration above
2. **Press `C-c c`** (or your configured keybinding)
3. **Navigate the hierarchy**: `l` → category → specific type
4. **Fill in prompts**: Interactive prompts guide you through required fields
5. **Edit content**: Cursor positioned at `%?` for immediate content entry

### Example Workflow: Creating a Note

1. Press `C-c c l n n` (Capture → lqdev → note → note)
2. Enter filename slug: `my-first-note`
3. Prompted for: Title, Tags
4. File created at: `_src/notes/2024-11-02-my-first-note.md`
5. Cursor positioned after frontmatter for content entry

### Example Workflow: Creating a Bookmark

1. Press `C-c c l r b` (Capture → lqdev → response → bookmark)
2. Enter filename slug: `interesting-article`
3. Prompted for: Title, Target URL, Tags
4. File created at: `_src/bookmarks/2024-11-02-interesting-article.md`
5. Add optional commentary after frontmatter

### Using Custom Block Templates

Custom block templates are meant to be **inserted into existing content**:

1. Open an existing content file
2. Position cursor where you want to insert the block
3. Use `C-c c` to select a block template
4. Fill in the prompts
5. Block is inserted at cursor position

**Note:** Block templates don't create new files - they insert content at the current cursor position.

## Template Customization

### Modifying Templates

Templates can be customized directly in the `.templates/` directory:

1. Edit the `.txt` file for the template
2. Maintain the org-capture syntax (`%^{}`, `%<>`, `%?`)
3. Ensure frontmatter matches Domain.fs type definitions
4. Test with `C-c c` in Emacs

### Adding New Templates

To add a new template:

1. **Create template file**: `.templates/my-template.txt`
2. **Add to Emacs config**: Insert new capture template entry
3. **Follow naming conventions**: Use descriptive prompts
4. **Test thoroughly**: Verify file creation and prompt flow

### Advanced Configuration

For more complex workflows, you can:

- **Use custom functions** for dynamic file paths
- **Add post-capture hooks** for automated actions
- **Integrate with other Emacs packages** (yasnippet, company-mode)
- **Create project-specific configurations** using `.dir-locals.el`

## Repository-Specific Considerations

### Timezone Configuration

All templates use **EST/EDT (`-05:00`)** to match:
- F# Domain.fs timezone parsing
- Azure Static Web Apps deployment timezone
- Consistent chronological ordering

**Don't change timezone** unless updating the entire site configuration.

### Filename Conventions

Templates use date-prefixed filenames for chronological organization:
- Format: `YYYY-MM-DD-slug.md`
- Example: `2024-11-02-my-article.md`

The `slug` should be:
- Lowercase with hyphens
- Descriptive and URL-friendly
- Unique within its directory

### Tag Format

Tags should be:
- Comma-separated strings in prompts
- Automatically formatted as arrays in frontmatter: `[tag1, tag2]`
- Lowercase and hyphenated
- Aligned with existing site tags for consistency

### Content Type Alignment

All templates are **aligned with Domain.fs** type definitions:
- Required fields match F# record types
- Field names use exact `YamlMember` aliases
- Date formats match parsing expectations
- Metadata structure follows established patterns

## Integration with VS Code Snippets

This site also has **VS Code snippets** (`.vscode/*.code-snippets`) that provide similar functionality for VS Code users. The org-capture templates offer:

**Advantages over VS Code snippets:**
- ✅ **Automatic file creation** with proper naming and location
- ✅ **Hierarchical navigation** for quick access
- ✅ **Consistent keyboard-driven workflow** 
- ✅ **Emacs ecosystem integration**

**VS Code snippets advantages:**
- ✅ **Visual editor interface**
- ✅ **Multi-language support** (F#, Markdown)
- ✅ **Inline snippet expansion**

Choose the system that matches your editor preference!

## Troubleshooting

### Templates Not Loading

**Problem:** Org-capture templates not appearing

**Solutions:**
1. Verify `lqdev-site-path` is set correctly in your config
2. Check template file paths exist: `ls ~/.../luisquintanilla.me/.templates/`
3. Reload Emacs config: `M-x eval-buffer`
4. Check for syntax errors in capture template definitions

### File Creation Fails

**Problem:** Template tries to create file but fails

**Solutions:**
1. Ensure target directory exists: `mkdir -p _src/posts`
2. Check file permissions: `ls -la _src/`
3. Verify filename doesn't contain invalid characters
4. Ensure you have write permissions to the repository

### Date Format Issues

**Problem:** Dates not parsing correctly in build

**Solutions:**
1. Verify timezone is `-05:00` in all templates
2. Check date format is `%<%Y-%m-%d %H:%M -05:00>`
3. Ensure no extra spaces or characters in date field
4. Test with sample content: `dotnet run`

### Prompt Not Working

**Problem:** Interactive prompt doesn't appear or accept input

**Solutions:**
1. Check for syntax errors in prompt definition: `%^{Prompt}`
2. Verify prompt text doesn't contain special characters
3. Test with simpler prompt first: `%^{Test}`
4. Ensure you're in an interactive Emacs session (not batch mode)

## Best Practices

### Content Creation Workflow

1. **Plan content type**: Determine the appropriate template before starting
2. **Prepare metadata**: Have title, tags, and other metadata ready
3. **Use consistent naming**: Follow established filename slug conventions
4. **Tag appropriately**: Use existing tags when possible for consistency
5. **Review frontmatter**: Verify all required fields are populated
6. **Build and test**: Run `dotnet run` to verify content builds correctly

### Template Maintenance

1. **Keep aligned with Domain.fs**: When F# types change, update templates
2. **Test after changes**: Verify template expansion after modifications
3. **Document customizations**: Note any repository-specific changes
4. **Version control**: Commit template changes with descriptive messages

### Hierarchical Organization

The 3-key hierarchy keeps templates organized and discoverable:
- **First level** (`l`): All site content
- **Second level**: Logical groupings (posts, media, responses)
- **Third level**: Specific types

When adding templates, maintain this hierarchy for consistency.

## Further Resources

### Emacs Org-capture Documentation
- [Official Org-capture Manual](https://orgmode.org/manual/Capture.html)
- [Org-capture Templates Tutorial](https://orgmode.org/manual/Capture-templates.html)

### Repository Documentation
- `docs/README.md` - General site documentation
- `docs/core-infrastructure-architecture.md` - F# architecture overview
- `.vscode/metadata.code-snippets` - VS Code alternative snippets
- `Domain.fs` - F# type definitions for all content types

### Related Workflows
- `docs/github-issue-posting-guide.md` - GitHub Issue publishing workflow
- `docs/media-publishing-workflow.md` - Media content workflow
- `docs/response-publishing-workflow.md` - Response content workflow

## Contributing

When adding or modifying templates:

1. **Test thoroughly** with Emacs org-capture
2. **Verify build** with `dotnet run`
3. **Update documentation** if adding new template types
4. **Maintain consistency** with existing templates
5. **Follow naming conventions** for discoverability

Templates should be **repository-specific** and focused on this static site's content types and structure.

---

**Template Count:** 33 templates across 5 categories
**Last Updated:** 2024-11-02
**Maintained by:** Site content contributors
