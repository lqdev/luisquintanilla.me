---
title: "Presentation Layouts Reference"
resources: 
  - text: "GitHub Repository"
    url: "https://github.com/lqdev/luisquintanilla.me"
  - text: "VS Code Snippets"
    url: "https://github.com/lqdev/luisquintanilla.me/blob/main/.vscode/presentation-layouts.code-snippets"
date: "10/24/2025 03:00 -05:00"
---

<div class="layout-centered">

# Presentation Layouts Reference

A comprehensive guide to custom layout classes for Reveal.js presentations

**15 Ready-to-Use Layouts**

</div>

Note:
This presentation demonstrates all available layout classes. Each slide uses the layout it's describing, so you can see exactly how it works in practice.

---

<div class="layout-two-column">
<div>

## What This Provides

- 15 pre-built layout classes
- CSS Grid & Flexbox powered
- Responsive design
- Easy to use in markdown
- No F# changes needed

</div>
<div>

## Benefits

- Faster slide creation
- Consistent styling
- Professional appearance
- Flexible composition
- VS Code snippets included

</div>
</div>

Note:
This layout system provides reusable CSS classes that work seamlessly with Reveal.js. Simply wrap your content in divs with the appropriate class names.

---

<div class="layout-big-text">

# Column Layouts

</div>

---

## Two Column Layout

**Class:** `.layout-two-column`

<div class="layout-two-column">
<div>

### Left Side

Use this layout for:
- Side-by-side comparisons
- Feature vs. benefit lists
- Two related concepts
- Before and after

**Equal width columns with 2rem gap**

</div>
<div>

### Right Side

Example use cases:
- Problem | Solution
- Old approach | New approach
- Theory | Practice
- Frontend | Backend

**Responsive on mobile**

</div>
</div>

Note:
Perfect for balanced content. Both columns get equal space. Great for comparisons and dual concepts. Remember to include blank lines before and after content blocks for proper Markdown parsing.

---

## Three Column Layout

**Class:** `.layout-three-column`

<div class="layout-three-column">
<div>

### Column 1

- Point A
- Point B
- Point C

First third of content

</div>
<div>

### Column 2

- Point D
- Point E
- Point F

Second third of content

</div>
<div>

### Column 3

- Point G
- Point H
- Point I

Third third of content

</div>
</div>

Note:
Three equal columns with 1.5rem gap. Ideal for feature lists, step-by-step processes, or categorized content. Stacks vertically on mobile for better readability.

---

<div class="layout-big-text">

# Split Layouts

</div>

---

## Split 70/30 Layout

**Class:** `.layout-split-70-30`

<div class="layout-split-70-30">
<div>

### Main Content Area (70%)

This layout gives prominence to the left side, making it perfect for:

- Detailed explanations with brief notes
- Code examples with key takeaways
- Main content with supplementary information
- Diagrams with annotations

The larger space allows for more detailed content, while the sidebar provides context or supporting points without overwhelming the main message.

</div>
<div>

### Sidebar (30%)

**Key Points:**
- Emphasizes main content
- 70/30 split
- 2rem gap
- Left aligned

</div>
</div>

Note:
Use this when you have a primary piece of content that needs more space, with secondary information in a sidebar. Common for code examples with explanations.

---

## Split 30/70 Layout

**Class:** `.layout-split-30-70`

<div class="layout-split-30-70">
<div>

### Sidebar (30%)

**Key Points:**
- Reversed flow
- Sidebar first
- 30/70 split
- Visual balance

</div>
<div>

### Main Content Area (70%)

The reverse split puts supplementary information first, guiding the reader's attention:

- Navigation or table of contents on left
- Reference information before main content
- Setup instructions before the main demo
- Context before detailed explanation

This creates a natural left-to-right reading flow with the sidebar acting as an introduction or guide.

</div>
</div>

Note:
Same proportions as 70/30 but reversed. Use when the sidebar content should be read first, or when you want to establish context before diving into the main content.

---

<div class="layout-big-text">

# Image Layouts

</div>

---

## Image Left Layout

**Class:** `.layout-image-left`

<div class="layout-image-left">
<div>

![Sample diagram](https://via.placeholder.com/400x300/2d4a5c/ffffff?text=Diagram+or+Image)

</div>
<div>

### Content on Right (60%)

The image-left layout is ideal for:

- Showing diagrams with explanations
- Displaying charts with analysis
- Architecture diagrams with descriptions
- Screenshots with commentary

The image gets 40% of the space, providing good visibility while leaving room for detailed text explanation.

</div>
</div>

Note:
Images are vertically centered and scale to fit their container. Use for visual content that needs accompanying explanation. Works great with diagrams, charts, or screenshots.

---

## Image Right Layout

**Class:** `.layout-image-right`

<div class="layout-image-right">
<div>

### Content on Left (60%)

This layout reverses the image position:

- Text-first, visual-second flow
- Explain before showing
- Build anticipation
- Natural reading pattern

The reversed layout can be more effective when you want to explain what the reader is about to see, rather than describing what they're looking at.

</div>
<div>

![Sample visualization](https://via.placeholder.com/400x300/2d4a5c/ffffff?text=Chart+or+Photo)

</div>
</div>

Note:
Same proportions as image-left but flipped. Choose based on whether you want to show or tell first. Both layouts center content vertically for visual balance.

---

<div class="layout-big-text">

# Special Purpose Layouts

</div>

---

<div class="layout-centered">

## Centered Layout

**Class:** `.layout-centered`

This layout centers content both vertically and horizontally.

Perfect for:
- Title slides
- Section dividers
- Key quotes
- Single important points

**Minimum height: 60vh**

</div>

Note:
The centered layout is what we're using right now! It's perfect for slides that need focus and impact. Great for section breaks and important standalone messages.

---

<div class="layout-big-text">

# Big Text Layout

</div>

Note:
The big text layout makes h1 and h2 elements 4rem in size. Use it for maximum impact slides, key takeaways, or section titles. This slide demonstrates the layout - notice the large heading.

---

## Code Demo Layout

**Class:** `.layout-code-demo`

<div class="layout-code-demo">
<div>

### Code

```fsharp
// F# function example
let greet name =
    $"Hello, {name}!"

let message = greet "World"
printfn "%s" message
```

</div>
<div>

### Result

```
Hello, World!
```

**Features:**
- Side-by-side code and output
- 70vh height
- Scrollable overflow
- Equal columns

</div>
</div>

Note:
Perfect for live coding demos or showing code with its output. Each side scrolls independently if content is too large. The 70vh height gives plenty of vertical space.

---

<div class="layout-full-width">

## Full Width Layout

**Class:** `.layout-full-width`

This layout removes all default margins and uses the entire slide width. Useful for:
- Full-width images or diagrams
- Edge-to-edge content
- Breaking out of standard margins
- Maximum content space

**Uses !important to override Reveal.js defaults**

</div>

Note:
Use sparingly for content that truly needs the full width. The !important declarations ensure it overrides Reveal.js's default margins.

---

## Title Content Layout

**Class:** `.layout-title-content`

<div class="layout-title-content">
<div>

### Auto-Sized Title Section

This section takes only the space it needs.

</div>
<div>

### Flexible Content Area

The content area expands to fill remaining space. This is perfect for:

- Title with detailed content below
- Header with flexible body
- Label with expandable information
- Category with multiple items

The 2rem gap creates clear visual separation between title and content sections.

</div>
</div>

Note:
The title row auto-sizes while the content row flexes to fill remaining space. Great for slides with clear header/body structure.

---

## Grid 4 Layout

**Class:** `.layout-grid-4`

<div class="layout-grid-4">
<div>

![Tech 1](https://via.placeholder.com/150/4a90e2/ffffff?text=F%23)

**F# Language**

</div>
<div>

![Tech 2](https://via.placeholder.com/150/50c878/ffffff?text=.NET)

**.NET Platform**

</div>
<div>

![Tech 3](https://via.placeholder.com/150/ff6b6b/ffffff?text=HTML)

**HTML5**

</div>
<div>

![Tech 4](https://via.placeholder.com/150/f39c12/ffffff?text=CSS)

**CSS3**

</div>
</div>

Note:
2x2 grid with centered items. Perfect for showing 4 related items like technologies, features, or team members. Each cell centers its content both horizontally and vertically.

---

## Grid 6 Layout

**Class:** `.layout-grid-6`

<div class="layout-grid-6">
<div>

![Item 1](https://via.placeholder.com/120/e74c3c/ffffff?text=1)

**Step 1**

</div>
<div>

![Item 2](https://via.placeholder.com/120/3498db/ffffff?text=2)

**Step 2**

</div>
<div>

![Item 3](https://via.placeholder.com/120/2ecc71/ffffff?text=3)

**Step 3**

</div>
<div>

![Item 4](https://via.placeholder.com/120/f39c12/ffffff?text=4)

**Step 4**

</div>
<div>

![Item 5](https://via.placeholder.com/120/9b59b6/ffffff?text=5)

**Step 5**

</div>
<div>

![Item 6](https://via.placeholder.com/120/1abc9c/ffffff?text=6)

**Step 6**

</div>
</div>

Note:
3x2 grid layout for showing 6 items. Great for process steps, technology stacks, or any collection of 6 related items. Tighter 1.5rem gap works well with more items.

---

## Comparison Layout

**Class:** `.layout-comparison`

<div class="layout-comparison">
<div>

### Before

- Complex setup
- Many dependencies
- Manual configuration
- Slow build times
- Hard to maintain

</div>
<div class="divider">

→

</div>
<div>

### After

- Simple setup
- Minimal dependencies
- Auto-configuration
- Fast builds
- Easy maintenance

</div>
</div>

Note:
Perfect for before/after comparisons. The center column shows a visual divider (arrow by default). The divider has specific styling with large font size and gray color.

---

<div class="layout-quote">

## Quote Layout

**Class:** `.layout-quote`

<blockquote>
The best way to predict the future is to invent it.
</blockquote>

<div class="attribution">
— Alan Kay
</div>

</div>

Note:
Specially styled for quotes. The blockquote has a left border, large italic text, and the attribution is right-aligned with lighter color. Perfect for inspirational quotes or testimonials.

---

<div class="layout-two-column">
<div>

## Usage Example

```html
<div class="layout-two-column">
<div>

Left content

</div>
<div>

Right content

</div>
</div>
```

</div>
<div>

## Important Tips

- Include blank lines
- Around content blocks
- For Markdown parsing
- Inside HTML divs

**HTML Structure:**
- Outer div with layout class
- Inner divs for each section
- Blank lines essential

</div>
</div>

Note:
Remember: blank lines before and after content blocks are crucial for proper Markdown parsing when content is inside HTML divs. This is a Reveal.js requirement, not a limitation of our layouts.

---

<div class="layout-centered">

## VS Code Snippets Available

Type `layout-` in VS Code to see all available snippets:

- `layout-2col` - Two columns
- `layout-3col` - Three columns
- `layout-split-70-30` - Split layouts
- `layout-image-left` - Image layouts
- `layout-centered` - Special layouts
- ...and 10 more!

**Check `.vscode/presentation-layouts.code-snippets`**

</div>

Note:
All 15 layouts have corresponding VS Code snippets for quick insertion. The snippets include tab stops for efficient content entry and proper structure with blank lines.

---

<div class="layout-two-column">
<div>

## Responsive Design

All layouts adapt to mobile:

- Columns stack vertically
- Grids adjust to 2 columns
- Text sizes scale down
- Spacing remains consistent

**Breakpoint:** 768px

</div>
<div>

## Best Practices

- Use semantic headings
- Keep content concise
- Consider contrast
- Test on mobile
- Use blank lines
- Combine layouts

</div>
</div>

Note:
The layouts are designed mobile-first and include responsive breakpoints. On smaller screens, multi-column layouts stack vertically for readability.

---

<div class="layout-centered">

# Start Using Layouts!

Copy the HTML from any slide in this presentation

Use the VS Code snippets for quick insertion

Mix and match layouts for your needs

**Resources:** [GitHub](https://github.com/lqdev/luisquintanilla.me) | [VS Code Snippets](https://github.com/lqdev/luisquintanilla.me/blob/main/.vscode/presentation-layouts.code-snippets)

</div>

Note:
This presentation itself serves as a template library. Copy any slide's HTML structure and modify the content for your own presentations. All layouts work together harmoniously.
