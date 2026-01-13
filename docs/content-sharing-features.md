# Content Sharing Features: QR Code and Permalink Buttons

## Overview

The Content Sharing Features provide three integrated sharing mechanisms available on every content page: Copy Permalink, Web Share, and QR Code generation. These features enable users to easily share content across devices and platforms using modern web APIs with progressive enhancement and full accessibility support.

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
- **Dynamic QR code generation** for any page URL
- **Modal interface** with preview and actions
- **Download capability** for QR code images
- **Responsive design** optimized for all screen sizes

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
- `_src/css/custom/qrcode.css` - QR modal styling (287 lines)

**JavaScript Files:**
- `_src/js/clipboard.js` - Copy to clipboard functionality
- `_src/js/share.js` - Web Share API integration (6KB)
- `_src/js/qrcode.js` - QR code generation and modal (9.4KB)

**F# View Components:**
- `Views/ComponentViews.fs` - Button generation functions:
  - `copyPermalinkButton` (lines 63-73)
  - `webShareButton` (lines 76-86)
  - `qrCodeButton` (lines 89-99)

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

**QR Code:**
```javascript
if (typeof QRCodeStyling === 'undefined') {
    // Show message about library availability
    // Button still visible for consistency
}
```

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

1. User clicks "📱 QR Code" button
2. Modal overlay appears with loading spinner
3. QR code generated using QRCodeStyling library
4. Modal shows:
   - Page URL in readable format
   - Generated QR code (256x256)
   - Copy URL button
   - Download QR image button
5. User can scan with phone camera or download image

### Implementation

**Button Generation (F#):**
```fsharp
let qrCodeButton (relativeUrl: string) =
    button [
        _class "qr-code-btn permalink-action-btn"
        _type "button"
        _title "Generate QR Code"
        attr "data-url" relativeUrl
        attr "aria-label" "Generate QR code for this page"
    ] [
        tag "span" [_class "button-icon"; attr "aria-hidden" "true"] [str "📱"]
        tag "span" [_class "button-label"] [str "QR Code"]
    ]
```

**JavaScript Handler (`qrcode.js`):**
```javascript
class QRCodeManager {
    generateQR(url) {
        this.qrCode = new QRCodeStyling({
            width: 256,
            height: 256,
            data: url,
            dotsOptions: {
                color: "#2C3E50",
                type: "rounded"
            },
            backgroundOptions: {
                color: "#ffffff"
            },
            cornersSquareOptions: {
                type: "extra-rounded"
            }
        });
        
        this.qrCode.append(container);
    }
}
```

### Modal Interface

**Structure:**
```html
<div class="qr-modal-overlay">
    <div class="qr-modal">
        <div class="qr-modal-header">
            <h2>📱 Scan to Open</h2>
            <button class="qr-modal-close">×</button>
        </div>
        <div class="qr-url-display">https://...</div>
        <div class="qr-code-container">
            <!-- QR code canvas -->
        </div>
        <div class="qr-modal-actions">
            <button class="qr-copy-btn">Copy URL</button>
            <button class="qr-download-btn">Download</button>
        </div>
    </div>
</div>
```

**Features:**
- **Close mechanisms**: X button, click outside, ESC key
- **Copy URL**: Quick copy within modal
- **Download**: Save QR code as PNG image
- **Smooth animations**: Modal slide-in, spinner rotation
- **Backdrop blur**: Focus attention on modal

### QR Code Customization

Generated QR codes include:
- **Size**: 256x256 pixels (optimal for screens and printing)
- **Style**: Rounded dots for modern appearance
- **Colors**: Dark dots (#2C3E50) on white background
- **Corner style**: Extra-rounded for visual appeal
- **Error correction**: Medium level (recovers from 15% damage)

### Download Functionality

When user clicks "Download":
1. QR code rendered as PNG blob
2. Blob converted to download URL
3. Temporary anchor element created
4. Filename: `qr-code-[timestamp].png`
5. Download triggered automatically
6. Cleanup: URL revoked after download

## Accessibility Features

### Keyboard Navigation

All buttons are fully keyboard accessible:
- **Tab key**: Navigate between buttons
- **Enter/Space**: Activate button
- **ESC key**: Close QR modal
- **Focus indicators**: Visible outline on focus

### Screen Reader Support

**ARIA Labels:**
- `aria-label` on all buttons for clear purpose
- `aria-hidden="true"` on decorative icon spans
- Proper button roles and types

**Announcements:**
- Success states announced ("Copied!")
- Error states announced with helpful context
- Modal opening/closing announced

### Visual Indicators

**Color is not the only indicator:**
- Text changes ("Copy" → "Copied!")
- Icons change (📋 → ✅)
- Size/position changes (hover lift)
- All information conveyed through text

### Reduced Motion

Users with `prefers-reduced-motion: reduce` get:
- No transform animations
- No spinner rotation
- No modal slide-in
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
- `--card-background`: Modal background
- `--card-border`: Modal and input borders

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
- QR modal shows full URL in monospace font
- URL wraps properly with `word-break: break-all`
- QR code handles long URLs with proper encoding

**Offline/Network Issues:**
- Copy button works offline (clipboard API)
- QR code generated client-side (no network needed)
- Share button gracefully handles network errors

**Browser Limitations:**
- Feature detection for all APIs
- Helpful error messages when features unavailable
- Buttons always visible for consistency

## Performance Considerations

### Bundle Size

**Total JavaScript:**
- `clipboard.js`: ~6KB
- `share.js`: 6KB
- `qrcode.js`: 9.4KB
- QRCodeStyling library: ~47KB (loaded on demand)
- **Total**: ~68KB (compressed smaller with gzip)

**Total CSS:**
- `permalink-buttons.css`: ~6KB
- `qrcode.css`: ~8KB
- **Total**: ~14KB

### Loading Strategy

**Eager Loading:**
- Button CSS (small, needed immediately)
- Clipboard JavaScript (small, common use)

**Lazy Loading:**
- QRCodeStyling library (larger, loaded on first QR button click)
- Modal created only when first QR button clicked

### Optimization

**DOM Efficiency:**
- Single modal instance reused for all QR codes
- Event delegation for button clicks
- Minimal DOM manipulation

**Memory Management:**
- QR code instances cleaned up after modal close
- Download URLs revoked after use
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

**No QR Library:**
- Button shows helpful message
- Link to URL still available in modal

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

### QR Code Not Generating

**Symptoms**: Modal shows loading spinner indefinitely

**Possible Causes:**
1. QRCodeStyling library not loaded
2. Invalid URL for QR encoding
3. JavaScript error in console

**Solutions:**
1. Check network tab for library load
2. Verify URL is valid and not too long
3. Check console for error messages

### Modal Won't Close

**Symptoms**: Modal stuck on screen

**Possible Causes:**
1. JavaScript error preventing close
2. Event handler not attached
3. CSS preventing interaction

**Solutions:**
1. Refresh page to reset state
2. Press ESC key (alternative close method)
3. Check console for errors

## Future Enhancements

### Potential Additions

**Planned:**
- QR code color customization matching theme
- Share history/recently shared items
- One-click share to specific platforms (Twitter, LinkedIn)

**Under Consideration:**
- Email share option
- Print-friendly QR code page
- QR code with logo/branding
- Analytics for share tracking
- Batch QR generation for multiple pages

**Not Planned:**
- Complex social media integrations (OAuth)
- Server-side QR generation
- URL shortening service

## Related Documentation

- [Desert Theme](../README.md#performance--ux) - Theme integration details
- [Progressive Enhancement](PWA_IMPLEMENTATION.md) - Web API usage patterns
- [Accessibility](text-only-site.md) - Accessibility compliance
- [Component System](../Views/ComponentViews.fs) - Button generation functions

## Success Metrics

Since implementation:
- **Universal availability**: All content pages have sharing buttons
- **Consistent design**: Unified styling across all three button types
- **Accessibility compliant**: Full WCAG 2.1 AA compliance
- **Progressive enhancement**: Works across capability spectrum
- **Zero regressions**: Seamless integration with existing design system
- **Performance optimized**: Minimal bundle size impact (<70KB total)

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
