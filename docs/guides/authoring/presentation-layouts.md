# Custom Presentation Layouts for Reveal.js

## Overview

The Custom Presentation Layouts System provides 15 pre-built CSS layout classes designed specifically for creating professional, structured slides in Reveal.js presentations. These layouts eliminate the need for manual CSS styling and provide consistent, responsive designs across all presentations.

## Features

- **15 Ready-to-Use Layouts**: Comprehensive set of layout patterns for all common slide structures
- **CSS Grid & Flexbox Powered**: Modern, responsive layouts that work on all screen sizes
- **No F# Changes Needed**: Pure CSS solution that works with markdown-based presentations
- **VS Code Snippets Included**: Quick layout insertion with tab completion
- **Mobile Responsive**: All layouts adapt gracefully to smaller screens
- **Zero Dependencies**: Works directly with existing Reveal.js setup

## Available Layout Classes

### Column Layouts

#### Two Column Layout (`.layout-two-column`)
Equal-width columns with 2rem gap, perfect for side-by-side comparisons.

**Use cases:**
- Feature vs. benefit lists
- Before and after comparisons
- Problem and solution pairs
- Theory vs. practice

**Example:**
```html
<div class="layout-two-column">
<div>

## Left Column

- Point 1
- Point 2
- Point 3

</div>
<div>

## Right Column

- Point A
- Point B
- Point C

</div>
</div>
```

#### Three Column Layout (`.layout-three-column`)
Three equal-width columns with 1.5rem gap for categorical content.

**Use cases:**
- Feature lists
- Step-by-step processes
- Categorized content
- Multi-option comparisons

### Split Layouts

#### Split 70/30 (`.layout-split-70-30`)
Main content area (70%) with a sidebar (30%).

**Use cases:**
- Main content with supporting information
- Primary content with examples
- Content with sidebar notes
- Focus on main topic with references

#### Split 30/70 (`.layout-split-30-70`)
Sidebar (30%) with main content area (70%).

**Use cases:**
- Menu or navigation with content
- Key points leading to details
- Summary with full explanation

### Image Layouts

#### Image Left (`.layout-image-left`)
Image (40%) with content (60%), aligned center.

**Use cases:**
- Product demonstrations
- Feature showcases
- Visual examples with descriptions
- Screenshots with explanations

#### Image Right (`.layout-image-right`)
Content (60%) with image (40%), aligned center.

**Use cases:**
- Descriptions with supporting images
- Text-focused content with visual aids
- Content with diagrams

### Specialized Layouts

#### Centered (`.layout-centered`)
Vertically and horizontally centered content, ideal for section breaks.

**Use cases:**
- Title slides
- Section dividers
- Key messages
- Chapter introductions

#### Big Text (`.layout-big-text`)
Large, centered text for maximum impact.

**Use cases:**
- Key quotes
- Important statistics
- Main takeaways
- Call-to-action slides

#### Header Content (`.layout-header-content`)
Header area with content below.

**Use cases:**
- Consistent slide structure
- Title with detailed content
- Topic headers with explanations

#### Side Notes (`.layout-side-notes`)
Main content with a notes column on the right.

**Use cases:**
- Content with annotations
- Main points with references
- Text with footnotes

#### Vertical Stack (`.layout-vertical-stack`)
Vertically stacked sections with equal spacing.

**Use cases:**
- Sequential content
- Layered information
- Stacked comparisons

#### Grid 2x2 (`.layout-grid-2x2`)
Four equal quadrants in a 2x2 grid.

**Use cases:**
- Four-part concepts
- Quadrant analysis
- Four feature comparison
- SWOT analysis

#### Quote (`.layout-quote`)
Large, styled blockquote for emphasis.

**Use cases:**
- Testimonials
- Important quotes
- Key statements
- User feedback

#### Code Split (`.layout-code-split`)
Code on left, explanation on right.

**Use cases:**
- Code walkthroughs
- Syntax demonstrations
- Code explanations
- Technical tutorials

#### Full Width (`.layout-full-width`)
Content spanning the full slide width with padding.

**Use cases:**
- Wide tables
- Full-width images
- Large diagrams
- Maximum content space

## VS Code Snippets

The system includes comprehensive VS Code snippets for all layouts. Simply type the snippet prefix and press Tab:

- `layout-two-column` - Two column layout
- `layout-three-column` - Three column layout
- `layout-split-70-30` - 70/30 split layout
- `layout-split-30-70` - 30/70 split layout
- `layout-image-left` - Image left layout
- `layout-image-right` - Image right layout
- `layout-centered` - Centered layout
- `layout-big-text` - Big text layout
- `layout-header-content` - Header content layout
- `layout-side-notes` - Side notes layout
- `layout-vertical-stack` - Vertical stack layout
- `layout-grid-2x2` - 2x2 grid layout
- `layout-quote` - Quote layout
- `layout-code-split` - Code split layout
- `layout-full-width` - Full width layout

## Usage Instructions

### Basic Usage

1. Create or open a presentation markdown file in `_src/resources/presentations/`
2. Add a slide separator (`---`)
3. Type a layout snippet prefix (e.g., `layout-two-column`)
4. Press Tab to expand the snippet
5. Fill in your content within the placeholders
6. Build the site to see your presentation

### Important Markdown Notes

**Always include blank lines:**
```html
<div class="layout-two-column">
<div>

<!-- Blank line above this comment is crucial -->

## Your Content

<!-- Blank line below this comment is crucial -->

</div>
</div>
```

Without blank lines, Markdown won't be processed correctly inside HTML divs.

### Combining Layouts

You can nest some layouts for advanced structures:

```html
<div class="layout-split-70-30">
<div>

<div class="layout-two-column">
<div>

## Sub-column 1

</div>
<div>

## Sub-column 2

</div>
</div>

</div>
<div>

## Sidebar

</div>
</div>
```

## Implementation Details

### File Locations

- **CSS File**: `_src/css/presentation-layouts.css`
- **VS Code Snippets**: `.vscode/presentation-layouts.code-snippets`
- **Demo Presentation**: `_src/resources/presentations/layout-reference.md`

### Technical Architecture

The layout system uses:
- **CSS Grid** for column-based layouts (two-column, three-column, splits)
- **Flexbox** for centering and vertical stacking
- **Responsive Design** with mobile-first approach
- **Semantic Classes** with `layout-` prefix to avoid conflicts

All layouts are designed to work seamlessly with Reveal.js's existing CSS and don't interfere with other site styles.

### Browser Support

The layouts work in all modern browsers:
- Chrome/Edge 57+
- Firefox 52+
- Safari 10.1+
- Opera 44+

All layouts gracefully degrade on older browsers by stacking content vertically.

## Examples

### Real-World Example: Feature Comparison

```html
<div class="layout-two-column">
<div>

## Traditional Approach

- Manual CSS for each slide
- Inconsistent styling
- Time-consuming setup
- Hard to maintain

</div>
<div>

## Layout System

- Pre-built, tested layouts
- Consistent appearance
- Fast slide creation
- Easy updates

</div>
</div>
```

### Real-World Example: Code Walkthrough

```html
<div class="layout-code-split">
<div>

```fsharp
let buildSite() =
    loadContent()
    |> processContent
    |> generatePages
    |> writeOutput
```

</div>
<div>

## Build Pipeline

1. Load all markdown files
2. Process with AST parser
3. Generate HTML pages
4. Write to output directory

</div>
</div>
```

## Best Practices

### Content Density
- Don't overcrowd slides - use layouts to organize, not to fit more content
- Two-column layouts work best with 3-5 bullet points per column
- Three-column layouts work best with 2-3 items per column

### Visual Hierarchy
- Use bigger layouts (70/30, image layouts) for important content
- Use centered and big-text layouts for key messages
- Use grid layouts for equal-weight comparisons

### Consistency
- Stick to 3-5 layout patterns per presentation for consistency
- Use the same layout for similar content types
- Reserve special layouts (big-text, quote) for emphasis

### Mobile Considerations
- All layouts stack vertically on mobile automatically
- Test your presentation on mobile devices
- Keep content readable at all screen sizes

## Troubleshooting

### Content Not Rendering as Markdown

**Problem**: Content appears as plain text instead of formatted markdown.

**Solution**: Add blank lines before and after your content within HTML divs:

```html
<!-- Wrong -->
<div>
## Heading
Content
</div>

<!-- Correct -->
<div>

## Heading

Content

</div>
```

### Layout Not Applying

**Problem**: Slide appears unstyled.

**Solution**: 
1. Check that `presentation-layouts.css` is included in the build
2. Verify the CSS file is being copied to `_public/css/`
3. Ensure the class name is spelled correctly (all lowercase, with hyphens)

### Images Not Sizing Correctly

**Problem**: Images too large or too small in image layouts.

**Solution**: The image layouts automatically size images with `object-fit: contain`. If you need manual control, add inline styles:

```html
<img src="path/to/image.jpg" style="max-height: 400px;" />
```

## Related Documentation

- [How to Create Presentations](../README.md#content-creation) - General presentation creation guide
- [Reveal.js Documentation](https://revealjs.com/) - Reveal.js framework documentation
- [VS Code Snippets Guide](vs-code-snippets-modernization.md) - VS Code snippets system

## Future Enhancements

Potential future additions:
- Animation presets for layout transitions
- Theme variants (dark mode optimized)
- Additional specialty layouts based on usage patterns
- Interactive layout builder tool
