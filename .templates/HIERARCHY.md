# Org Capture Templates Hierarchy

## Visual Navigation Tree

This shows the complete 3-key navigation structure for Emacs org-capture.

```
C-c c (org-capture)
└── l (lqdev.me Content)
    ├── p (Post Types)
    │   └── a → article.txt      (Article)
    │
    ├── n (Note Types)
    │   └── n → note.txt         (Note)
    │
    ├── m (Media Types)
    │   ├── m → media.txt        (Media - image/video/audio)
    │   ├── a → album-collection.txt (Album Collection)
    │   └── p → playlist-collection.txt (Playlist Collection)
    │
    ├── r (Response Types)
    │   ├── r → response.txt     (Response - reply/star/reshare)
    │   └── b → bookmark.txt     (Bookmark)
    │
    └── o (Resources)
        ├── v → review.txt       (Review)
        ├── s → snippet.txt      (Snippet)
        ├── w → wiki.txt         (Wiki)
        ├── p → presentation.txt (Presentation)
        └── l → livestream.txt   (Livestream)
```

## Keybinding Reference

### Posts & Articles
- `C-c c l p a` → Create Article

### Notes
- `C-c c l n n` → Create Note

### Media
- `C-c c l m m` → Create Media Post (image/video/audio)
- `C-c c l m a` → Create Album Collection
- `C-c c l m p` → Create Playlist Collection

### Responses (IndieWeb Social)
- `C-c c l r r` → Create Response (with type selection)
- `C-c c l r b` → Create Bookmark

### Resources
- `C-c c l o v` → Create Review
- `C-c c l o s` → Create Code Snippet
- `C-c c l o w` → Create Wiki Entry
- `C-c c l o p` → Create Presentation
- `C-c c l o l` → Create Livestream

## Template File Mapping

### Content Types (9 files)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| article.txt | Long-form articles | _src/posts/ |
| note.txt | Microblog notes | _src/notes/ |
| media.txt | Media posts (image/video/audio) | _src/media/ |
| album-collection.txt | Album collections | _src/albums/ |
| playlist-collection.txt | Music playlists | _src/playlists/ |
| snippet.txt | Code snippets | _src/snippets/ |
| wiki.txt | Knowledge base | _src/wiki/ |
| presentation.txt | Presentations | _src/resources/presentations/ |
| livestream.txt | Livestreams | _src/streams/ |

### Response Types (2 files)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| response.txt | Generic response (reply/star/reshare) | _src/responses/ |
| bookmark.txt | Bookmark links | _src/bookmarks/ |

### Reviews (1 file)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| review.txt | Reviews using :::review::: blocks | _src/reviews/library/ |

### Custom Blocks (15 files)
These templates insert content blocks at cursor position (no new file):

**Media Blocks (5):**
- media-image.txt
- media-video.txt
- media-audio.txt
- media-document.txt
- media-link.txt

**Review Blocks (5):**
- review-book.txt
- review-movie.txt
- review-music.txt
- review-product.txt
- review-business.txt

**Resume Blocks (5):**
- resume-experience.txt
- resume-skills.txt
- resume-project.txt
- resume-education.txt
- resume-testimonial.txt

### Markdown Helpers (6 files)
Quick insertion helpers (no new file):
- helper-datetime.txt
- helper-blockquote.txt
- helper-codeblock.txt
- helper-link.txt
- helper-image.txt
- helper-youtube.txt

## Mnemonic Key System

The second-level keys use mnemonics for easy recall:
- **p** = Posts
- **n** = Notes
- **m** = Media
- **r** = Responses
- **o** = Other/Resources (can't use 'r')

Third-level keys match the first letter of content type:
- **a** = Article
- **p** = Post/Photo/Presentation
- **n** = Note
- **v** = Video
- **r** = Reply
- **s** = Star/Snippet
- **h** = resHare (reshare)
- **b** = Bookmark/Book
- **w** = Wiki
- **l** = Livestream

## Usage Tips

1. **Learn one category at a time**: Start with notes (`l n n`) or posts (`l p a`)
2. **Use muscle memory**: The 3-key sequences become automatic with practice
3. **Tab completion**: Emacs shows available options at each level
4. **Consistent patterns**: Similar content types use similar keys
5. **Filename slugs**: Always use lowercase-with-hyphens for slugs

---

**Total Templates:** 33
**Navigation Depth:** 3 keys maximum
**Coverage:** All active content types aligned with current architecture
