---
title: "Comprehensive Custom Block Test"
description: "Test document with all custom block types for Phase 1D validation"
published_date: "2025-07-08"
tags: ["test", "custom-blocks", "phase1d"]
---

# Comprehensive Custom Block Test

This document contains all custom block types to validate the complete infrastructure implementation.

## Media Block Test

:::media
- url: "https://example.com/image1.jpg"
  alt: "Test landscape image"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "A beautiful landscape photo"
- url: "https://example.com/video1.mp4"
  alt: "Test video"
  mediaType: "video"
  aspectRatio: "square"
  caption: "Sample video content"
:::media

## Review Block Test

:::review
title: "Amazing Product Review"
item: "Test Product Name"
itemType: "product"
rating: 4.5
scale: 5.0
summary: "This is an excellent product that exceeded expectations."
pros:
  - "Great build quality"
  - "Excellent performance"
  - "Good value for money"
cons:
  - "Could be slightly lighter"
  - "Documentation could be better"
additionalFields:
  price: "$99.99"
  availability: "In Stock"
:::review

## Venue Block Test

:::venue
name: "Conference Center Downtown"
address: "123 Main Street"
city: "San Francisco"
state: "CA"
postalCode: "94105"
country: "USA"
url: "https://example.com/venue"
phone: "+1-555-123-4567"
email: "info@venue.com"
description: "Modern conference facility in the heart of downtown"
:::venue

## RSVP Block Test

:::rsvp
event: "Tech Conference 2025"
eventUrl: "https://example.com/event"
response: "yes"
note: "Looking forward to the keynote!"
attendees: 1
datetime: "2025-08-15T09:00:00Z"
location: "Conference Center Downtown"
:::rsvp

## Mixed Content Test

This is regular markdown content mixed with custom blocks.

### Another Media Block

:::media
- url: "https://example.com/photo.png"
  alt: "Another test image"
  mediaType: "image"
  aspectRatio: "portrait"
:::media

**Bold text** and *italic text* work normally.

### Code Block (Regular Markdown)

```fsharp
let testFunction x = x + 1
```

### Final Review Block

:::review
title: "Book Review: F# Programming"
item: "Expert F# 4.0"
itemType: "book"
rating: 5.0
scale: 5.0
summary: "Comprehensive guide to F# programming with excellent examples."
:::review

## Conclusion

This document tests all custom block types in various contexts:
- Multiple media items in a single block
- Complex review data with pros/cons
- Venue information with full address details  
- RSVP with event metadata
- Mixed content with regular markdown
- Nested block structures

All blocks should render correctly with proper HTML output and microformat support.
