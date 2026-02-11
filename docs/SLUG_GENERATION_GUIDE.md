# Slug Generation Guide

## Overview

This document explains how slugs are generated for content posts created through GitHub issue templates. Understanding slug generation helps you predict filenames and avoid conflicts.

## What is a Slug?

A **slug** is the URL-friendly identifier used in the filename and URL for a post. For example:
- Title: "My Awesome Post"
- Slug: "my-awesome-post"
- Filename: "my-awesome-post-2024-01-15.md"
- URL: "https://luisquintanilla.me/{content-type}/my-awesome-post-2024-01-15" (where `{content-type}` is `notes`, `bookmarks`, `responses`, etc.)

## Slug Generation Rules

### Standard Behavior

All post types follow these standardized rules:

1. **Custom Slug Provided**: If you provide a custom slug in the issue template, it will be used as-is (after sanitization)
   - Input: Custom slug = "my-custom-slug"
   - Output: `my-custom-slug.md`

2. **No Custom Slug**: If you leave the slug field blank, the system auto-generates a slug from the title and appends the current date
   - Input: Title = "My Awesome Post", Slug = (blank)
   - Output: `my-awesome-post-2024-01-15.md`

### Slug Sanitization

All slugs (custom or auto-generated) are sanitized using these rules:

1. Convert to lowercase
2. Replace spaces with hyphens (`-`)
3. Replace underscores with hyphens (`-`)
4. Remove all characters except: `a-z`, `0-9`, and `-`
5. Collapse multiple consecutive hyphens into one
6. Trim leading/trailing hyphens

**Examples**:
| Input | Output |
|-------|--------|
| `"Hello World"` | `"hello-world"` |
| `"API_Design_2024"` | `"api-design-2024"` |
| `"React.js & Vue.js"` | `"reactjs-vuejs"` |
| `"___test___"` | `"test"` |

### Length Limits

- Auto-generated slugs from titles are limited to 50 characters (60 for playlists)
- Custom slugs are used as provided (after sanitization) without length limits
- If truncation occurs, trailing hyphens are removed

## Post Type Examples

### Note Posts

**With Custom Slug**:
```yaml
Title: "Understanding F# Pattern Matching"
Slug: "fsharp-patterns"
Result: fsharp-patterns.md
```

**Without Custom Slug**:
```yaml
Title: "Understanding F# Pattern Matching"
Slug: (blank)
Result: understanding-f-pattern-matching-2024-01-15.md
```

### Bookmark Posts

**With Custom Slug**:
```yaml
Title: "Great Article on Web Performance"
Target URL: https://example.com/article
Slug: "web-perf-article"
Result: web-perf-article.md
```

**Without Custom Slug**:
```yaml
Title: "Great Article on Web Performance"
Target URL: https://example.com/article
Slug: (blank)
Result: great-article-on-web-performance-2024-01-15.md
```

### Media Posts

**With Custom Slug**:
```yaml
Title: "Vacation Photos"
Slug: "hawaii-2024"
Result: hawaii-2024.md
```

**Without Custom Slug**:
```yaml
Title: "Vacation Photos"
Slug: (blank)
Result: vacation-photos-2024-01-15.md
```

### Playlist Posts

**With Custom Slug**:
```yaml
Playlist Title: "Coding Focus Music"
Slug: "coding-focus"
Result: coding-focus.md
```

**Without Custom Slug**:
```yaml
Playlist Title: "Coding Focus Music"
Slug: (blank)
Result: coding-focus-music-2024-01-15.md
```

### Response Posts (Reply, Reshare, Star, RSVP)

**With Custom Slug**:
```yaml
Title: "Reply to Great Blog Post"
Response Type: reply
Slug: "my-reply"
Result: my-reply.md
```

**Without Custom Slug**:
```yaml
Title: "Reply to Great Blog Post"
Response Type: reply
Slug: (blank)
Result: reply-to-great-blog-post-2024-01-15.md
```

### Review Posts (Book, Movie, Music, Business, Product)

**With Custom Slug**:
```yaml
Item Name: "The Pragmatic Programmer"
Review Type: book
Slug: "pragmatic-programmer-review"
Result: pragmatic-programmer-review.md
```

**Without Custom Slug**:
```yaml
Item Name: "The Pragmatic Programmer"
Review Type: book
Slug: (blank)
Result: the-pragmatic-programmer-2024-01-15.md
```

## Why Date Appending?

When auto-generating slugs, the system appends the date (YYYY-MM-DD format) for several important reasons:

1. **Uniqueness**: Prevents filename conflicts when multiple posts have similar titles
2. **Chronological Organization**: Makes it easy to see when posts were created
3. **Version Control**: Allows tracking multiple posts about the same topic over time
4. **Predictability**: Standard pattern makes it easy to find files

## Best Practices

### When to Use Custom Slugs

Use custom slugs when:
- You want a specific, memorable URL
- You're creating a "canonical" post on a topic (e.g., "about", "contact")
- You need to match an existing URL structure
- You want to avoid date suffixes

### When to Let Slugs Auto-Generate

Let the system auto-generate slugs when:
- It's a time-sensitive post (the date context is useful)
- You're creating multiple posts on similar topics
- You don't have a specific URL requirement
- You want to minimize the risk of filename conflicts

### Tips for Good Slugs

1. **Keep them short**: Aim for 3-5 words maximum
2. **Be descriptive**: The slug should hint at the content
3. **Use keywords**: Include important terms for SEO
4. **Avoid dates in custom slugs**: The system adds dates automatically when needed
5. **No special characters**: Stick to letters, numbers, and hyphens

**Good Examples**:
- `fsharp-tips`
- `web-performance-guide`
- `docker-basics`

**Avoid**:
- `my-post-2024-01-15` (redundant date)
- `post-1234` (not descriptive)
- `this-is-a-really-long-slug-that-contains-too-many-words` (too long)

## Troubleshooting

### "File already exists" Error

If you get a file conflict error:
1. Check if you've already created a post with that slug
2. Use a different custom slug
3. Let the system auto-generate (includes date for uniqueness)

### Slug Doesn't Match Expectations

If the generated slug looks wrong:
1. Check slug sanitization rules (special characters are removed)
2. Verify the title doesn't have too many special characters
3. Consider providing a custom slug for better control

### Date Not Appearing in Filename

This only happens when:
1. You provided a custom slug (intentional behavior)
2. The script didn't run correctly (check workflow logs)

## Technical Implementation

For developers and contributors working on the processing scripts:

### Standard Pattern

All processing scripts follow this pattern:

```fsharp
// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
```

### Scripts Using This Pattern

- `Scripts/process-github-issue.fsx` (notes)
- `Scripts/process-bookmark-issue.fsx` (bookmarks)
- `Scripts/process-media-issue.fsx` (media)
- `Scripts/process-playlist-issue.fsx` (playlists)
- `Scripts/process-response-issue.fsx` (responses)
- `Scripts/process-review-issue.fsx` (reviews - uses helper function)

### Date Format

The date format is always `yyyy-MM-dd` (e.g., "2024-01-15"):
- **Year**: 4 digits
- **Month**: 2 digits (zero-padded)
- **Day**: 2 digits (zero-padded)

This format ensures:
- Lexicographic sorting matches chronological sorting
- International standard (ISO 8601)
- No ambiguity (unlike MM-DD-YYYY or DD-MM-YYYY)

## Summary

✅ **Custom slug provided** → Use custom slug (no date)  
✅ **No custom slug** → Generate from title + append date  
✅ **All post types** → Follow same pattern consistently  
✅ **Sanitization** → Applied to all slugs for URL safety  

---

**Last Updated**: 2024-01-15  
**Maintainer**: Issue Publisher Agent  
**Related Docs**: 
- [SLUG_GENERATION_AUDIT.md](./SLUG_GENERATION_AUDIT.md) - Technical audit report
- [GitHub Workflows Guide](./../.github/workflows/README.md) - Workflow documentation
