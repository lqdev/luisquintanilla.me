# Content sharing

Every content page has three ways to share it: Copy Permalink, Web Share, and a QR code. Copy and Share use browser APIs. The QR code is an SVG made at build time and shown with a plain HTML `<details>` element, so it needs no JavaScript.

## Features

### 1. Copy Permalink Button (📋)
- **One-click URL copying** to clipboard
- **Visual feedback** with success/error states
- **Keyboard accessible** with proper ARIA labels
- **Works universally** across all browsers with clipboard API support

### 2. Web Share Button (🔗)
- **Native share dialog** using Web Share API
- **Platform integration** (iOS share sheet, Android share menu)
- **Automatic metadata** extraction from content
- **Graceful fallback** when API unavailable

### 3. QR Code Button (📱)
- **Pre-rendered SVG** for every content page, made during the build
- **`<details>` disclosure**: clicking the button opens a panel with the QR image
- **No JavaScript or CDN library** at runtime; the image loads lazily when the panel opens

## Visual Design

### Button Appearance

All three buttons share a unified design system:
- **Transparent background** with accent color border
- **Hover effect**: Filled background with lift animation
- **Active state**: Pressed animation for tactile feedback
- **Success states**: Green background for copy/share success
- **Icon + Label**: Emoji icons with text labels for clarity

### Responsive Behavior

**Desktop (>768px):**
- Full button with icon and label
- Side-by-side layout
- Standard padding and spacing

**Mobile (480-768px):**
- Reduced padding, slightly smaller text
- Horizontal layout maintained
- Optimized touch targets

**Small Screens (<480px):**
- **Icon-only mode** to save space
- Larger icons (1.25rem) for better visibility
- Maintained 44px+ touch targets for accessibility

## Technical Implementation

### File Structure

**CSS Files:**
- `_src/css/custom/permalink-buttons.css` - Unified button styling (211 lines)
- `_src/css/custom/qrcode.css` - QR disclosure panel styling

**JavaScript Files:**
- `_src/js/clipboard.js` - Copy to clipboard functionality
- `_src/js/share.js` - Web Share API integration (6KB)

**F# View Components:**
- `Views/ComponentViews.fs` - Button generation functions: `copyPermalinkButton`, `webShareButton`, `qrCodeButton`

**F# Build Step (QR images):**
- `Builders/Assets.fs` - `buildPerPageQRs` writes one SVG per content page
- `Services/QRStyled.fs` - the SVG renderer

### Integration Points

Buttons are included in:
- Individual content pages (posts, notes, responses)
- Timeline cards
- Collection pages
- Any page with shareable content

### Progressive Enhancement

Each feature uses progressive enhancement:

**Copy Button:**
```javascript
if (navigator.clipboard && navigator.clipboard.writeText) {
    // Full clipboard API support
} else {
    // Fallback to execCommand or error message
}
```

**Share Button:**
```javascript
if (navigator.share) {
    // Use native Web Share API
} else {
    // Show helpful error or alternative
}
```

**QR Code:** no script involved. `<details>` works in every current browser without JavaScript.

## Copy Permalink Button

### How It Works

1. User clicks "📋 Copy" button
2. JavaScript reads `data-url` attribute
3. URL copied to clipboard using Clipboard API
4. Button shows success state with "✅ Copied!"
5. After 2 seconds, returns to normal state

### Implementation

**Button Generation (F#):**
```fsharp
let copyPermalinkButton (relativeUrl: string) =
    button [
        _class "copy-permalink-btn permalink-action-btn"
        _type "button"
        _title "Copy to clipboard"
        attr "data-url" relativeUrl
        attr "aria-label" $"Copy permalink to clipboard"
    ] [
        tag "span" [_class "button-icon"; attr "aria-hidden" "true"] [str "📋"]
        tag "span" [_class "button-label"] [str "Copy"]
    ]
```

**JavaScript Handler (`clipboard.js`):**
- Listens for clicks on `.copy-permalink-btn`
- Extracts URL from `data-url` attribute
- Uses `navigator.clipboard.writeText()` for modern browsers
- Provides visual feedback with button state changes
- Handles errors gracefully with user-friendly messages

### User Experience

**Success Flow:**
1. Click → Button changes to "✅ Copied!" with green background
2. URL now in clipboard
3. Button returns to normal after 2 seconds

**Error Flow:**
1. Click → If clipboard API unavailable, shows error state
2. User sees helpful message about browser limitations

## Web Share Button

### How It Works

1. User clicks "🔗 Share" button
2. JavaScript extracts content metadata (title, URL, description)
3. Native share dialog opens (iOS share sheet, Android menu, etc.)
4. User selects share destination (Messages, Email, Social Media, etc.)
5. Content shared with extracted metadata

### Implementation

**Button Generation (F#):**
```fsharp
let webShareButton (relativeUrl: string) =
    button [
        _class "web-share-btn permalink-action-btn"
        _type "button"
        _title "Share via Web Share API"
        attr "data-url" relativeUrl
        attr "aria-label" "Share this page"
    ] [
        tag "span" [_class "button-icon"; attr "aria-hidden" "true"] [str "🔗"]
        tag "span" [_class "button-label"] [str "Share"]
    ]
```

**JavaScript Handler (`share.js`):**
```javascript
class ShareManager {
    extractContentMetadata(article, url) {
        // Extracts title from .p-name, .post-title, h1, h2
        // Extracts summary from .p-summary, .post-content, .e-content
        // Truncates to 200 characters for sharing
        // Ensures absolute URL
    }

    async shareContent(metadata, button) {
        await navigator.share({
            title: metadata.title,
            text: metadata.text,
            url: metadata.url
        });
    }
}
```

### Metadata Extraction

The system intelligently extracts metadata from page structure:

**Title Priority:**
1. `.p-name` (microformats2)
2. `.post-title`
3. `h1` or `h2` tags
4. Fallback to `document.title`

**Content Priority:**
1. `.p-summary` (microformats2)
2. `.post-content`
3. `.e-content` (microformats2)
4. Truncated to 200 characters with ellipsis

### Platform Support

**Full Support:**
- ✅ iOS Safari 12+ (native share sheet)
- ✅ Android Chrome 61+ (native share menu)
- ✅ Android Firefox 71+
- ✅ Windows 10+ (built-in share dialog)

**Limited/No Support:**
- ⚠️ Desktop Safari (older versions)
- ⚠️ Desktop Chrome (requires HTTPS and user gesture)
- ❌ Internet Explorer (no support)

## QR Code Button

### How It Works

1. During the build, `buildPerPageQRs` in `Builders/Assets.fs` renders one SVG for each content
   page into `_public/assets/images/qr/<type>/<slug>.svg`. The QR encodes the canonical
   `https://lqdev.me/<type>/<slug>/` URL.
2. On the page, the "📱 QR Code" button is the `<summary>` of a `<details>` element.
3. Clicking it opens the panel, and the browser loads the SVG (`loading="lazy"`).
4. The reader scans it with a phone camera. To save it, they can save the image from the
   browser.

### Implementation

**Disclosure (F#, `Views/ComponentViews.fs`):**
```fsharp
let qrCodeButton (relativeUrl: string) =
    let qrPath =
        let trimmed = relativeUrl.Trim('/')
        "/assets/images/qr/" + trimmed + ".svg"
    tag "details" [ _class "qr-code-disclosure" ] [
        tag "summary" [
            _class "qr-code-btn permalink-action-btn"
            _title "Show QR code for this page"
            attr "aria-label" "Show QR code for this page"
        ] [
            tag "span" [ _class "button-icon"; attr "aria-hidden" "true" ] [ str "📱" ]
            tag "span" [ _class "button-label" ] [ str "QR Code" ]
        ]
        div [ _class "qr-code-panel" ] [
            img [ _src qrPath; _alt "QR code linking to this page"; _class "qr-code-img"
                  attr "loading" "lazy"; attr "decoding" "async" ]
        ]
    ]
```

### QR Code Style

`Services/QRStyled.fs` draws the SVG in F#. The QR bit matrix comes from
`Net.Codecrete.QrCodeGenerator`.
- **Colors**: dark slate modules (`#1a2332`) and orange finder corners (`#ff6b35`) on white
- **Shape**: rounded modules that merge with their neighbours
- **Center image**: a small copy of the avatar, embedded as a data URI so the SVG is one file
- **Error correction**: High, which recovers the modules hidden under the avatar

This replaced an earlier client-side version that loaded the `qr-code-styling` library from a CDN
and drew the code in a modal (PR #2390).

## Accessibility Features

### Keyboard Navigation

All buttons are fully keyboard accessible:
- **Tab key**: Navigate between buttons
- **Enter/Space**: Activate button
- **Focus indicators**: Visible outline on focus

### Screen Reader Support

**ARIA Labels:**
- `aria-label` on all buttons for clear purpose
- `aria-hidden="true"` on decorative icon spans
- Proper button roles and types

**Announcements:**
- Success states announced ("Copied!")
- Error states announced with helpful context
- The QR `<details>` element reports its open or closed state natively

### Visual Indicators

**Color is not the only indicator:**
- Text changes ("Copy" → "Copied!")
- Icons change (📋 → ✅)
- Size/position changes (hover lift)
- All information conveyed through text

### Reduced Motion

Users with `prefers-reduced-motion: reduce` get:
- No transform animations
- Static transitions only

## Theme Integration

### Desert Theme Support

All buttons integrate with the desert theme:
- **Variables**: Use CSS custom properties for colors
- **Dark mode**: Proper contrast in dark theme
- **Light mode**: Proper contrast in light theme
- **Accent color**: Consistent tomato red (#FF6347)

**CSS Variables Used:**
- `--accent-color`: Button border and hover background
- `--background-color`: Button hover text color
- `--text-color`: Default text color

### Theme Switching

Buttons respond to theme changes:
```css
[data-theme="dark"] .permalink-action-btn {
    /* Dark mode styles */
}

[data-theme="light"] .permalink-action-btn {
    /* Light mode styles */
}
```

## Use Cases

### Primary Use Cases

**Content Sharing:**
- Share blog posts on social media
- Send article links to friends/colleagues
- Add to reading lists and bookmarks

**Cross-Device Access:**
- Open article on phone by scanning QR from desktop
- Quick transfer between devices without typing URLs
- Conference presentations showing QR codes

**Content Organization:**
- Copy URLs for note-taking apps
- Build custom collections with permalinks
- Reference links in documentation

### Edge Cases Handled

**Long URLs:**
- QR code handles long URLs with proper encoding

**Offline/Network Issues:**
- Copy button works offline (clipboard API)
- QR code is a static image made at build time
- Share button gracefully handles network errors

**Browser Limitations:**
- Feature detection for all APIs
- Helpful error messages when features unavailable
- Buttons always visible for consistency

## Performance Considerations

### Bundle Size

**Total JavaScript:**
- `clipboard.js` and `share.js`
- The QR code adds no JavaScript. Each page's SVG is fetched only when its panel opens.

**Total CSS:**
- `permalink-buttons.css`
- `qrcode.css`

### Loading Strategy

**Eager Loading:**
- Button CSS (small, needed immediately)
- Clipboard and share JavaScript

**Lazy Loading:**
- QR SVG (`loading="lazy"`, fetched when the disclosure opens)

### Optimization

**DOM Efficiency:**
- Event delegation for button clicks
- Minimal DOM manipulation

**Memory Management:**
- No memory leaks from event listeners

## Browser Compatibility

### Feature Support Matrix

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| Copy Button | ✅ 63+ | ✅ 53+ | ✅ 13.1+ | ✅ 79+ |
| Web Share | ✅ 89+ | ✅ 71+ | ✅ 12.1+ | ✅ 93+ |
| QR Code | ✅ All | ✅ All | ✅ All | ✅ All |

### Fallback Strategies

**No Clipboard API:**
- Show error message with instructions
- Suggest manual copy (Ctrl+C)

**No Web Share:**
- Show error message
- Suggest using copy button or QR code

## Troubleshooting

### Copy Button Not Working

**Symptoms**: Click doesn't copy URL

**Possible Causes:**
1. Not HTTPS (Clipboard API requires secure context)
2. Browser permissions denied
3. Old browser version

**Solutions:**
1. Ensure site served over HTTPS
2. Check browser console for permission errors
3. Update browser or use alternative sharing method

### Share Button Does Nothing

**Symptoms**: Click has no effect

**Possible Causes:**
1. Browser doesn't support Web Share API
2. Not triggered by user gesture
3. Invalid share data

**Solutions:**
1. Check `navigator.share` availability
2. Ensure share triggered directly from user click
3. Verify URL is absolute and valid

### QR Code Image Missing

**Symptoms**: The QR panel opens but the image is broken

**Possible Causes:**
1. The page wasn't part of the unified feed passed to `buildPerPageQRs`, so no SVG was written
2. The page URL doesn't match the `<type>/<slug>` path the SVG was written to

**Solutions:**
1. Check that `_public/assets/images/qr/<type>/<slug>.svg` exists after a build
2. Check the build log line `📱 Generated N per-page QR SVGs`

## Future Enhancements

### Potential Additions

**Planned:**
- QR code color customization matching theme
- Share history/recently shared items
- One-click share to specific platforms (Twitter, LinkedIn)

**Under Consideration:**
- Email share option
- Print-friendly QR code page
- Analytics for share tracking

**Not Planned:**
- Complex social media integrations (OAuth)
- URL shortening service

## Related Documentation

- [Desert Theme](../../README.md#performance--ux) - Theme integration details
- [Progressive Enhancement](pwa.md) - Web API usage patterns
- [Accessibility](text-only-site.md) - Accessibility compliance
- [Component System](../../Views/ComponentViews.fs) - Button generation functions

## Success Metrics

Since implementation:
- **Universal availability**: All content pages have sharing buttons
- **Consistent design**: Unified styling across all three button types
- **Accessibility compliant**: Full WCAG 2.1 AA compliance
- **Progressive enhancement**: Works across capability spectrum
- **Zero regressions**: Seamless integration with existing design system

## Summary

The Content Sharing Features (QR Code and Permalink Buttons) provide a modern, accessible, and user-friendly way to share content across devices and platforms. Through progressive enhancement, careful attention to accessibility, and seamless theme integration, these features enhance user experience without compromising performance or compatibility.

**Key Benefits:**
- ✅ One-click URL copying
- ✅ Native platform sharing
- ✅ Cross-device QR codes
- ✅ Full accessibility support
- ✅ Responsive design
- ✅ Theme integration
- ✅ Progressive enhancement
