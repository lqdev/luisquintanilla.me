# Org Capture Templates Hierarchy

## Visual Navigation Tree

This shows the complete 3-key navigation structure for Emacs org-capture.

```
C-c c (org-capture)
└── l (lqdev.me Content)
    ├── p (Post Types)
    │   ├── a → article.txt      (Article)
    │   └── p → post.txt         (Post)
    │
    ├── n (Note Types)
    │   ├── n → note.txt         (Note)
    │   └── c → note-crate-finds.txt (Crate Finds)
    │
    ├── m (Media Types)
    │   ├── p → photo.txt        (Photo)
    │   ├── v → video.txt        (Video)
    │   └── a → album.txt        (Album)
    │
    ├── r (Response Types)
    │   ├── r → reply.txt        (Reply)
    │   ├── s → star.txt         (Star/Favorite)
    │   ├── h → reshare.txt      (Reshare)
    │   └── b → bookmark.txt     (Bookmark)
    │
    └── o (Resources)
        ├── b → book.txt         (Book)
        ├── s → snippet.txt      (Snippet)
        ├── w → wiki.txt         (Wiki)
        ├── p → presentation.txt (Presentation)
        └── l → livestream.txt   (Livestream)
```

## Keybinding Reference

### Posts & Articles
- `C-c c l p a` → Create Article
- `C-c c l p p` → Create Post

### Notes
- `C-c c l n n` → Create Note
- `C-c c l n c` → Create Crate Finds Note

### Media
- `C-c c l m p` → Create Photo Post
- `C-c c l m v` → Create Video Post
- `C-c c l m a` → Create Album

### Responses (IndieWeb Social)
- `C-c c l r r` → Create Reply
- `C-c c l r s` → Create Star/Favorite
- `C-c c l r h` → Create Reshare
- `C-c c l r b` → Create Bookmark

### Resources
- `C-c c l o b` → Create Book Review
- `C-c c l o s` → Create Code Snippet
- `C-c c l o w` → Create Wiki Entry
- `C-c c l o p` → Create Presentation
- `C-c c l o l` → Create Livestream

## Template File Mapping

### Content Types (13 files)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| article.txt | Long-form articles | _src/posts/ |
| post.txt | General posts | _src/posts/ |
| note.txt | Microblog notes | _src/notes/ |
| note-crate-finds.txt | Music discoveries | _src/notes/ |
| photo.txt | Photo posts | _src/feed/ |
| video.txt | Video posts | _src/feed/ |
| album.txt | Photo albums | _src/albums/ |
| album-collection.txt | Album collections | _src/albums/ |
| playlist-collection.txt | Music playlists | _src/playlists/ |
| snippet.txt | Code snippets | _src/snippets/ |
| wiki.txt | Knowledge base | _src/wiki/ |
| presentation.txt | Presentations | _src/resources/presentations/ |
| livestream.txt | Livestreams | _src/streams/ |

### Response Types (5 files)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| response.txt | Generic response | _src/responses/ |
| reply.txt | Reply to posts | _src/responses/ |
| reshare.txt | Reshare content | _src/responses/ |
| star.txt | Favorite posts | _src/responses/ |
| bookmark.txt | Bookmark links | _src/responses/ |

### Book Reviews (1 file)
| Template File | Purpose | Target Directory |
|--------------|---------|------------------|
| book.txt | Book reviews | _src/resources/books/ |

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

**Total Templates:** 40
**Navigation Depth:** 3 keys maximum
**Coverage:** All content types in Domain.fs
