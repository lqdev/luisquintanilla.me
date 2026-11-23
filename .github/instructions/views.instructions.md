---
applyTo: "Views/**,_src/css/**,_src/js/**"
---

# Views, CSS, and JavaScript

The HTML views are Giraffe ViewEngine in `Views/`. Styles and scripts are in `_src/css/` and `_src/js/` and are copied to the site at build time.

- Treat a visual change to an item as three changes: the timeline card, the item's own page, and the text-only page in `Views/TextOnly/`. Change all three or say why not.
- Change the timeline in both places that draw it. The server renders the first items and `_src/js/timeline.js` renders the rest from JSON, so a value such as a badge or a URL computed only on the server is wrong for older items.
- Show the specific response type (reply, reshare, star, rsvp, bookmark) on badges and labels, not a generic "responses".
- Keep the home timeline loading from embedded JSON in chunks. Don't render every item into the page; a thousand-plus cards breaks the browser's parser.
- Keep text-only pages free of images and JavaScript and under 50 KB. Images become links that describe them.
- Keep microformats2 classes when you restyle. Hide markup with the `microformat-hidden` class instead of deleting it, because IndieWeb parsers read it.
- Style both themes. Dark rules use `[data-theme="dark"]`; check the light theme too.
- Import a new stylesheet in `_src/css/custom/` from `_src/css/custom/main.css`, or the site never loads it.
- Keep the link-preview bucket hash identical in `Services/LinkClassification.fs` and `_src/js/link-preview.js`: both use FNV-1a over the same path key, modulo `bucketCount` in `Constants.fs`.

Read first: `docs/features/text-only-site.md`
