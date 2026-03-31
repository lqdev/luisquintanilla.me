# Tag Standardization Guidelines

## Overview

All tags are automatically normalized at build time by `TagService.processTagName` in `Services/Tag.fs`. This means you can write tags naturally in content frontmatter — the system handles casing, spacing, and deduplication.

When authoring content, prefer the **canonical forms** listed below. While the system will consolidate variants automatically, using canonical forms directly keeps the source consistent.

---

## Automatic Normalizations

### 1. Case & Whitespace
Tags are lowercased and trimmed. Spaces and underscores become hyphens.

| You write | Becomes |
|---|---|
| `AI` | `ai` |
| `Open Source` | `open-source` → `opensource` |
| `machine_learning` | `machine-learning` → `machinelearning` |

### 2. Technology Names
Special-character technology names are normalized before anything else.

| You write | Canonical |
|---|---|
| `.net`, `.net core` | `dotnet` |
| `.net framework` | `dotnet-framework` |
| `asp.net` | `aspnet` |
| `c#` | `csharp` |
| `f#` | `fsharp` |
| `node.js` | `nodejs` |
| `next.js` | `nextjs` |
| `vue.js` | `vuejs` |

### 3. Plural → Singular Consolidations

Use singular forms. All plurals are mapped to their singular canonical tag.

| Plural (avoid) | Canonical singular |
|---|---|
| `agents` | `agent` |
| `algorithms` | `algorithm` |
| `apps` | `app` |
| `audiobooks` | `audiobook` |
| `blogs` | `blog` |
| `books` | `book` |
| `calendars` | `calendar` |
| `embeddings` | `embedding` |
| `evaluations` | `evaluation` |
| `feeds` | `feed` |
| `images` | `image` |
| `llms` | `llm` |
| `models` | `model` |
| `movies` | `movie` |
| `nationalparks` | `nationalpark` |
| `neuralnetworks` | `neuralnetwork` |
| `newsletters` | `newsletter` |
| `notebooks` | `notebook` |
| `openstandards` | `openstandard` |
| `phones` | `phone` |
| `photos` | `photo` |
| `podcasts` | `podcast` |
| `programminglanguages` | `programminglanguage` |
| `protocols` | `protocol` |
| `smartphones` | `smartphone` |
| `standards` | `standard` |
| `tokenizers` | `tokenizer` |
| `transformers` | `transformer` |
| `videos` | `video` |
| `webmentions` | `webmention` |
| `websites` | `website` |

### 4. Gerund / Verb-Form Consolidations

| Variant (avoid) | Canonical |
|---|---|
| `selfhosting` | `selfhost` |
| `self-hosting` | `selfhost` |

### 5. Compound-Word Normalization

The dominant (most-used) form in existing content is the canonical. Hyphenated and spaced variants are all mapped to it.

| Variant (avoid) | Canonical | Notes |
|---|---|---|
| `artificial intelligence`, `artificial-intelligence` | `artificialintelligence` | |
| `computer vision`, `computer-vision` | `computervision` | |
| `data science`, `data-science` | `datascience` | |
| `.net core`, `dotnet-core` | `dotnetcore` | |
| `e-mail` | `email` | |
| `hugging face`, `hugging-face` | `huggingface` | |
| `local first`, `local-first` | `localfirst` | |
| `machine learning`, `machine-learning` | `machinelearning` | |
| `open source`, `open-source` | `opensource` | |
| `org mode`, `org-mode` | `orgmode` | |
| `raspberry pi`, `raspberry-pi` | `raspberrypi` | |
| `social media`, `social-media` | `socialmedia` | |
| `text to speech`, `text-to-speech` | `texttospeech` | |

### 6. Typo Fixes

| Typo | Correct |
|---|---|
| `messsaging` | `messaging` |

---

## Best Practices for New Tags

1. **Use singular nouns** — `podcast` not `podcasts`, `tool` not `tools`
2. **Concatenate compound concepts** — `machinelearning` not `machine-learning`; `socialmedia` not `social media`
3. **Use established technology abbreviations** — `ai`, `llm`, `rss`, `foss`
4. **Avoid redundant specificity** — tag `indieweb` not `indiewebsite`
5. **Check existing tags first** — run `Scripts/tags.fsx` to see what's in use before creating a new tag

---

## Adding New Consolidations

If you discover new duplicate patterns, add them to `canonicalTagMap` in `Services/Tag.fs`:

```fsharp
let private canonicalTagMap =
    dict [
        // ... existing entries ...
        "newplural", "newsingular"   // add here
    ]
```

The map uses **exact-match lookup** on the fully-processed tag string (post lowercase, post hyphenation, post special-char removal). This is safe against substring collisions.
