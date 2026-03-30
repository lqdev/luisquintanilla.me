# ADR-0005: Desert Theme Design System

## Status
Accepted

## Context

Need a cohesive visual identity for a personal website that supports light and dark modes, maintains accessibility, and provides distinct visual treatment for 11+ content types on timeline views. The design should be systematic enough that new content types can be added without ad-hoc color choices.

## Decision

Implement a desert-inspired design system using CSS custom properties defined in `_src/css/custom/variables.css`. The system provides two complete themes — Desert Day (light) and Desert Night (dark) — with named color tokens, semantic mappings, and a spacing/typography scale.

### Light Theme — Desert Day (`:root`)

| Token | Value | Purpose |
|-------|-------|---------|
| `--desert-sand` | `#F4E7D3` | Background — warm, welcoming |
| `--saguaro-green` | `#4B6F44` | Text & borders — natural, readable |
| `--saguaro-green-dark` | `#3A5535` | Darker variant for borders |
| `--saguaro-green-light` | `#5F8A58` | Lighter variant for accents |
| `--sunset-orange` | `#FF4500` | Accent & buttons — energetic |
| `--cactus-flower` | `#DDA0DD` | Hover states — delightful |
| `--cactus-bloom` | `#E6B3E6` | Lighter purple variant |
| `--sky-blue` | `#87CEEB` | Links — friendly |
| `--desert-sage` | `#9CAF88` | Muted green for wiki |
| `--sunrise-pink` | `#FFB6C1` | Soft pink for presentations |

### Dark Theme — Desert Night (`[data-theme="dark"]`)

| Token | Value | Purpose |
|-------|-------|---------|
| `--midnight-indigo` | `#2C3E50` | Background — sophisticated dark |
| `--desert-moonlight` | `#BDC3C7` | Text — easy on eyes |
| `--ocotillo-bloom` | `#FF6347` | Accent — warm in darkness |
| `--cactus-green` | `#4B6F44` | Hover — subtle interactions |
| `--saguaro-shadow` | `#34495E` | Card background — depth |

### Semantic Color Mapping

Tokens are mapped to semantic variables that components reference:
- `--background-color`, `--text-color`, `--accent-color`, `--hover-color`, `--link-color`
- `--card-background`, `--card-border`, `--button-background`, `--button-text`, `--button-hover`
- `--sidebar-background`, `--sidebar-text`

### Content Type Badge Colors

Each content type has a distinct badge color from the desert palette, defined in `_src/css/custom/timeline.css`:

| Content Type | Color | Name |
|-------------|-------|------|
| Posts | `#FF4500` | Sunset Orange |
| Notes | `#DDA0DD` | Cactus Flower |
| Responses | `#FF6347` | Ocotillo Bloom |
| Snippets | `#9CAF88` | Desert Sage |
| Wiki | `#8FBC8F` | Palo Verde |
| Presentations | `#DAA520` | Joshua Tree |
| Reviews | `#CD853F` | Barrel Cactus |
| Bookmarks | `#9CAF88` | Desert Sage |
| Streams | `#8FBC8F` | Palo Verde |

### Design Scale

The system includes a consistent spacing scale (`--spacing-xs` through `--spacing-xxl`), border radius tokens (`--border-radius-sm/md/lg`), shadow levels (`--shadow-sm/md/lg`), and transition speeds (`--transition-fast/normal/slow`) with `prefers-reduced-motion` support.

Dark mode is activated via `[data-theme="dark"]` attribute on the body element, with automatic detection via `@media (prefers-color-scheme: dark)` for users without an explicit preference.

## Consequences

**Easier:**
- Consistent look across all pages — components reference semantic tokens, not raw hex values.
- Dark mode support is automatic — switching `data-theme` attribute swaps the entire palette via CSS custom property cascade.
- Accessibility compliance — `prefers-reduced-motion` disables all transitions, and `prefers-color-scheme` auto-detects system preference.
- Content types are visually distinct on the timeline — each badge color is immediately recognizable.

**More difficult:**
- New content types need a badge color chosen from the desert palette — the color must be visually distinct from existing types while fitting the theme.
- Both light and dark themes must be maintained in parallel — any new semantic token needs values in both `:root` and `[data-theme="dark"]`.
- Color accessibility (contrast ratios) must be verified for both themes when adding new tokens.
