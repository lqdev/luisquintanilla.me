---
title: "Pattern: Keep Inline SVG Contiguous in Reveal Markdown Decks"
description: "Reveal.js markdown decks can leak inline SVG source as visible slide text when blank lines or malformed attributes cause marked to close the raw HTML block early."
entry_type: pattern
published_date: "2026-04-27 16:23 -05:00"
last_updated_date: "2026-04-27 16:23 -05:00"
tags: "javascript, web, presentations, patterns"
related_skill: ""
source_project: "website"
---

## Discovery

While preparing the Mycelium FediForum 2026 presentation, appendix slides that contained inline SVG diagrams rendered as broken slides. Instead of seeing a diagram, the slide showed literal SVG source text such as:

```html
<svg id="mycelium-mayor-roles" class="mycelium-diagram" ...>
```

Some SVG text nodes rendered as normal slide text, while shapes were missing or appeared outside the SVG. The issue became more visible after adding Reveal vertical slide navigation for appendix sections using `data-separator-vertical` in `Views/LayoutViews.fs`.

The browser DOM confirmed the failure mode:

- The `<svg>` element existed.
- Its `<style>` child stayed inside the SVG.
- Later `<rect>` and `<text>` children had escaped and become sibling elements or paragraphs in the slide.

That meant this was not only a CSS sizing problem. The markdown parser had broken the SVG block before the browser could render it as a complete diagram.

## Root Cause

Reveal's markdown plugin uses `marked` to parse markdown content from the `<textarea data-template>`. Raw HTML in markdown is sensitive to block boundaries. Blank lines inside a raw inline SVG can cause the parser to treat the raw HTML block as finished, so the SVG closes early from the parser's point of view.

This broken structure is easy to miss because the generated static HTML still contains the original markdown inside the `<textarea>`, and the browser may still create an `<svg>` node. The failure only becomes obvious after Reveal/marked transforms the markdown into slide DOM.

Malformed SVG attributes made the problem worse. Several appendix diagrams had missing spaces between attributes:

```html
<svg id="mycelium-mayor-roles"class="mycelium-diagram" ...>
<rect x="40" y="116"width="280" ... />
<line x1="416" y1="252"x2="392" ... />
```

Those are invalid or fragile SVG/HTML boundaries and can compound parsing problems.

## Solution

Keep inline SVG blocks contiguous inside Reveal markdown slides:

1. Remove blank lines between `<svg>` and `</svg>`.
2. Ensure every attribute is separated by a space.
3. Keep authoring comments outside SVG blocks, or remove them entirely.
4. Verify the rendered DOM, not just the generated static HTML.

Before:

```html
<svg id="example" class="mycelium-diagram" viewBox="0 0 980 540">
  <style>
    #example .title { fill: #f9fafb; }
  </style>

  <text class="title" x="490" y="44">Title</text>

  <rect x="40" y="80"width="240" height="120" />
</svg>
```

After:

```html
<svg id="example" class="mycelium-diagram" viewBox="0 0 980 540">
  <style>
    #example .title { fill: #f9fafb; }
  </style>
  <text class="title" x="490" y="44">Title</text>
  <rect x="40" y="80" width="240" height="120" />
</svg>
```

For the Mycelium deck, the fix included:

- Making every SVG block in `_src/resources/presentations/mycelium-fediforum-2026.md` contiguous.
- Repairing malformed attributes such as `id="..."class`, `y="..."width`, and `y1="..."x2`.
- Replacing an overflowing HTML table in the appendix with an SVG diagram to match the rest of the deck.
- Adding a deck-local compact text class for dense appendix text subslides.
- Adding the vertical slide separator to the actual presentation renderer:

```fsharp
section [ flag "data-markdown"; attr "data-separator-vertical" "\\r?\\n--\\r?\\n" ] [
    textarea [ flag "data-template" ] [
        rawText presentation.Content
    ]
]
```

Important detail: the Reveal markdown plugin reads vertical separators from the `<section data-markdown>` element's `data-separator-vertical` attribute. Setting `separatorVertical` in `Reveal.initialize()` is not enough for this plugin path.

## Verification

Use a browser-level check because the source markdown and generated static HTML can look correct while the post-Reveal DOM is broken.

Useful checks:

```javascript
const present = [...document.querySelectorAll('.reveal .slides section.present')].at(-1);
const svg = present.querySelector('svg#mycelium-mayor-roles');

console.log({
  hasSvg: !!svg,
  rectCountInSvg: svg?.querySelectorAll('rect').length,
  textCountInSvg: svg?.querySelectorAll('text').length,
  rectSiblings: [...present.children].filter((el) => el.tagName === 'RECT').length,
  sourceLeak: present.innerText.includes('<svg')
});
```

The healthy state is:

- `hasSvg: true`
- expected `<rect>` / `<text>` counts inside the SVG
- `rectSiblings: 0`
- `sourceLeak: false`

Also test real navigation behavior, especially with vertical slides:

- Load `#/appendix-mayor`.
- Press down arrow and confirm the next subslide appears.
- Confirm the slide does not show literal `<svg ...>` source text.
- Check that the slide's rendered bounds fit inside the embedded Reveal viewport.

## Prevention

When authoring Reveal markdown decks with inline SVG:

- Treat inline SVG as parser-sensitive raw HTML, not as normal XML.
- Do not put blank lines inside SVG blocks.
- Do not put markdown comments inside SVG blocks.
- Run a quick regex scan for missing attribute spaces:

```powershell
rg '"(class|width|height|x2)=|[0-9]"[a-zA-Z]' _src\resources\presentations\my-deck.md
```

- Prefer one visual vocabulary for appendix diagrams. Mixing dense HTML tables with SVG diagrams can create inconsistent sizing and overflow in embedded Reveal presentations.
- Verify the transformed browser DOM after any markdown separator or SVG-heavy deck change.

This pattern is especially relevant for decks rendered through the website's presentation pipeline, where markdown lives inside `<textarea data-template>` and Reveal transforms it client-side.
