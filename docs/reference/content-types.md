# Content types

The 14 content types in `ContentTypes.fs`, where their files live, and the frontmatter each one
reads. Keys come from the `YamlMember` aliases in `Domain.fs`. The three feed columns are the flags
in the content roster in `Program.fs`.

| Type (wire name) | Source directory | Page URL | Frontmatter keys | Timeline | All-content feed | Blog archive |
| --- | --- | --- | --- | --- | --- | --- |
| `posts` | `_src/posts/` | /posts/{slug}/ | post_type, title, description, published_date, tags (array), reading_time_minutes | Yes | Yes | Yes |
| `notes` | `_src/notes/` | /notes/{slug}/ | Same keys as posts | Yes | Yes | Yes |
| `responses` | `_src/responses/` | /responses/{slug}/ | title, targeturl, response_type, rsvp_status, dt_published, dt_updated, tags (array), reading_time_minutes | Yes | Yes | Yes |
| `bookmarks` | `_src/bookmarks/` | /bookmarks/{slug}/ | Same keys as responses, with response_type: bookmark | Yes | Yes | No |
| `snippets` | `_src/resources/snippets/` | /resources/snippets/{slug}/ | title, language, tags (comma-separated string), created_date | No | Yes | No |
| `wiki` | `_src/resources/wiki/` | /resources/wiki/{slug}/ | title, last_updated_date, tags (comma-separated string) | No | Yes | No |
| `presentations` | `_src/resources/presentations/` | /resources/presentations/{slug}/ | title, resources (list of text and url), tags (comma-separated string), date | No | Yes | No |
| `reviews` | `_src/reviews/` and its subfolders | /reviews/{slug}/ | title, published_date, tags (array), plus a `:::review` block | Yes | Yes | No |
| `media` | `_src/media/` | /media/{slug}/ | post_type, title, published_date, tags, images (imagepath, description, alttext), plus `:::media` blocks | Yes | Yes | No |
| `album-collection` | `_src/albums/` | /collections/albums/{slug}/ | title, description, date, location (lat, lon), tags | Yes | Yes | No |
| `playlist-collection` | `_src/playlists/` | /collections/playlists/{slug}/ | title, description, date, tags | No | Yes | No |
| `ai-memex` | `_src/resources/ai-memex/` | /resources/ai-memex/{slug}/ | title, description, entry_type, published_date, last_updated_date, tags (comma-separated string), related_skill, source_project, related_entries | No | Yes | No |
| `marketplace` | `_src/marketplace/` | /marketplace/{slug}/ | title, description, published_date, price, currency, price_note, status, condition, category, location, tags (array) | No | Yes | No |
| `streams` | `_src/streams/` | /streams/{slug}/ | title, video_url, published_date, resources | Not in the roster | Not in the roster | Not in the roster |

Notes on the table:

- Bookmarks go through the response processor, so they read the response keys. `Domain.fs` also
  has a BookmarkDetails type with a bookmark_of key, and `Processors/BookmarkProcessor.fs` uses it,
  but the build never calls that processor.
- Valid `response_type` values are reply, star, reshare, bookmark, and rsvp. share is accepted as an
  old name for reshare. The /bookmarks/ and /rsvp/ landing pages are built from responses too.
- Review files sit in subfolders of `_src/reviews/` (business, library, movies, music, products),
  and every Markdown file under it is read. ReviewDetails also accepts the older keys author, isbn,
  cover, source, rating, and date_published.
- Wiki files often carry post_type: "wiki", but WikiDetails doesn't read it.
- Streams are loaded by `Loaders.fs`, not by a processor, and also build the /streams/ index and
  the /live/ page.
- Tags are a YAML array for some types and a comma-separated string for others. The table says which.

## Frontmatter examples

Post, in `_src/posts/`:

```yaml
---
post_type: "article"
title: "My Post Title"
description: "A brief description of the post"
published_date: "2025-01-15 08:00 -05:00"
tags: ["topic", "another-topic"]
---
```

Note, in `_src/notes/`:

```yaml
---
post_type: "note"
title: "Short note title"
published_date: "2025-01-15 19:00 -05:00"
tags: ["tag1", "tag2"]
---
```

Wiki entry, in `_src/resources/wiki/` (tags are a comma-separated string):

```yaml
---
post_type: "wiki"
title: "Topic Name"
last_updated_date: "01/15/2025 08:00 -05:00"
tags: topic, subtopic, category
---
```

Response, in `_src/responses/`:

```yaml
---
title: "Response Title"
targeturl: https://example.com/article
response_type: reply
dt_published: "2025-01-15 12:00 -05:00"
dt_updated: "2025-01-15 12:00 -05:00"
tags: ["tag1", "tag2"]
---
```

Bookmark, in `_src/bookmarks/`:

```yaml
---
title: "Bookmark Title"
targeturl: https://example.com/resource
response_type: bookmark
dt_published: "2025-01-15 12:00 -05:00"
dt_updated: "2025-01-15 12:00 -05:00"
tags: ["tag1", "tag2"]
---

Optional commentary about why this is worth bookmarking.
```

Feed URLs for each type are in [feeds](feeds.md), and URL rules are in
[URL structure](url-structure.md). To add a new content type, use the add-content-type skill in
`.github/skills/add-content-type/`.
