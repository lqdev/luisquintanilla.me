---
title: "Pattern: Text-Only Accessibility Site"
description: "Complete accessibility-first website implementation using F# ViewEngine + semantic HTML + minimal CSS provides universal access while maintaining performance excellence and content parity."
entry_type: pattern
published_date: "2026-07-22 00:00 -05:00"
last_updated_date: "2026-07-22 00:00 -05:00"
tags: fsharp, web, accessibility, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
---

## Discovery

The main site — with its desert theme, JavaScript-powered timeline, progressive loading, and rich media — serves most users well. But it's inaccessible on 2G networks, flip phones, screen readers with limited JavaScript support, and other constrained environments. Building a complete text-only alternative site using F# ViewEngine with semantic HTML and minimal CSS provides universal access while maintaining full content parity and performance excellence.

## Root Cause / Problem

Modern web development optimizes for modern browsers. Even a well-built static site with good performance can be inaccessible when it assumes CSS custom properties, JavaScript execution, or image loading. Users on assistive technology, extreme low-bandwidth connections, or legacy devices get a degraded or broken experience. The problem isn't just performance — it's fundamental accessibility. A text-only alternative must provide complete content parity, not a stripped-down subset.

## Solution

The pattern implements a complete parallel site under the `/text/` URL path, built with the same F# ViewEngine that powers the main site but with radically different rendering priorities:

- **Foundation Architecture**: Dedicated `textOnlyLayout` templates in F# ViewEngine generate semantic HTML with no decorative markup. Every element serves a structural purpose.

- **Minimal CSS Strategy**: A stylesheet under 5KB provides readable typography, proper spacing, and WCAG 2.1 AA contrast compliance. No images, no custom fonts, no animations.

- **URL Structure**: The `/text/` subdirectory preserves all content type organization. `/text/posts/` mirrors `/posts/`, `/text/notes/` mirrors `/notes/`, and so on.

- **True Text-Only Content**: Images are converted to descriptive text rather than being stripped entirely. An `<img>` tag with alt text becomes `[Image: alt-text] (View image: URL)`, preserving the information while remaining accessible on any device.

- **HTML-to-Text Conversion**: The `TextOnlyContentProcessor` module handles semantic structure preservation — headings become markdown-style markers, lists maintain their structure, code blocks are preserved, and emphasis is converted to text markers.

- **Comprehensive Browsing**: Tag-based navigation with occurrence counts, chronological archives organized by year and month, and cross-site navigation links between text-only and full versions.

- **Performance Targets**: Every page loads under 50KB, optimized for 2G networks. The homepage weighs 7.6KB.

### Content Processing Pattern

The image-to-text conversion follows specific rules:

```
<img src="/photo.jpg" alt="Sunset over the desert"> 
→ [Image: Sunset over the desert] (View image: /photo.jpg)
```

When no alt text exists, the fallback is `[Image]`. All external links preserve their URLs in parentheses. Headings convert to markdown format. HTML tags are cleaned from the final output for pure text rendering.

## Key Components

- **TextOnlyViews.fs**: Complete view module with 14+ functions covering all browsing patterns — home, posts, notes, responses, tags, archives, individual content pages, and navigation
- **TextOnlyBuilder.fs**: Site generation orchestration that processes all content types through the text-only pipeline
- **TextOnlyContentProcessor**: Module within TextOnlyViews.fs handling HTML-to-text conversion with image replacement, link preservation, and semantic structure maintenance
- **sanitizeTagForPath**: Function handling special characters in user-generated tag names for clean URL generation
- **Accessibility features**: Skip links, ARIA labels, semantic landmarks, keyboard navigation, and form-based functionality with optional JavaScript enhancement

## Results

- **Content Parity**: 1,130+ content pages rendered with zero information loss compared to the main site
- **Performance Excellence**: 7.6KB homepage, all pages under the 50KB target
- **Universal Compatibility**: Verified working on 2G network simulations, basic feature phones, screen readers, and assistive technology
- **Build Efficiency**: Zero impact on existing build process — the text-only site generates alongside the main site without slowing the build

## Benefits

The text-only site provides a complete universal access solution. Every piece of content on the main site has a text-only equivalent. Performance is exceptional on any connection speed. Screen readers and assistive technology get clean semantic HTML without fighting through decorative markup. The pattern integrates seamlessly with the existing F# build pipeline — adding a new content type to the main site means adding its text-only rendering is a natural next step. The approach demonstrates that accessibility and content richness are not in conflict; they're parallel expressions of the same content.
