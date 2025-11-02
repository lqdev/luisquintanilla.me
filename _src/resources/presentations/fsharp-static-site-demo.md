---
title: "Building with F# - A Technical Talk"
resources: 
  - text: "F# Language Guide"
    url: "https://docs.microsoft.com/en-us/dotnet/fsharp/"
  - text: "Sample Code Repository"
    url: "https://github.com/lqdev/luisquintanilla.me"
date: "10/24/2025 03:30 -05:00"
---

<div class="layout-centered">

# Building with F#

A practical guide to static site generation

**Luis Quintanilla**

October 2025

</div>

---

<div class="layout-big-text">

# Why F#?

</div>

---

## Key Benefits

<div class="layout-two-column">
<div>

### Developer Experience

- Concise syntax
- Type inference
- Immutability by default
- Pattern matching
- Excellent tooling

</div>
<div>

### Production Ready

- .NET ecosystem
- Great performance
- Strong typing
- Cross-platform
- Battle-tested

</div>
</div>

---

## Architecture Overview

<div class="layout-split-70-30">
<div>

### Core Components

The static site generator uses a modular architecture:

1. **Content Processing** - Markdown parsing with custom blocks
2. **View Generation** - F# ViewEngine for type-safe HTML
3. **Build Pipeline** - Orchestrated content generation
4. **Asset Management** - CSS, JavaScript, and image optimization

Each component is independently testable and follows functional programming principles.

</div>
<div>

**Tech Stack:**
- F# 9.0
- .NET 9
- Giraffe ViewEngine
- Markdig
- Reveal.js

</div>
</div>

---

## Code Example

<div class="layout-code-demo">
<div>

### Implementation

```fsharp
type Post = {
    Title: string
    Content: string
    Date: string
    Tags: string array
}

let renderPost post =
    article [] [
        h1 [] [Text post.Title]
        div [] [rawText post.Content]
    ]
```

</div>
<div>

### Output

Clean, type-safe HTML generation using F#'s powerful type system and functional composition.

**Benefits:**
- Compile-time safety
- No string concatenation
- Composable views
- Testable code

</div>
</div>

---

## Technology Stack

<div class="layout-grid-6">
<div>

![F#](https://via.placeholder.com/120/4a90e2/ffffff?text=F%23)

**F# 9.0**

</div>
<div>

![.NET](https://via.placeholder.com/120/512bd4/ffffff?text=.NET)

**.NET 9**

</div>
<div>

![HTML](https://via.placeholder.com/120/e44d26/ffffff?text=HTML)

**HTML5**

</div>
<div>

![CSS](https://via.placeholder.com/120/264de4/ffffff?text=CSS)

**CSS3**

</div>
<div>

![Markdown](https://via.placeholder.com/120/000000/ffffff?text=MD)

**Markdown**

</div>
<div>

![Git](https://via.placeholder.com/120/f05032/ffffff?text=Git)

**Git**

</div>
</div>

---

## Before vs After

<div class="layout-comparison">
<div>

### Manual HTML

- Error-prone strings
- No type safety
- Hard to maintain
- Difficult testing
- Fragile refactoring

</div>
<div class="divider">

→

</div>
<div>

### F# ViewEngine

- Type-safe views
- Compile-time checks
- Easy maintenance
- Simple testing
- Safe refactoring

</div>
</div>

---

## Build Process

<div class="layout-title-content">
<div>

### Three-Stage Pipeline

</div>
<div>

1. **Parse** - Read markdown files and extract metadata
2. **Process** - Transform content and generate HTML
3. **Output** - Write files and copy static assets

Each stage is pure and composable, making the system easy to understand and extend.

</div>
</div>

---

<div class="layout-image-left">
<div>

![Architecture Diagram](https://via.placeholder.com/400x300/2d4a5c/ffffff?text=Architecture+Diagram)

</div>
<div>

## System Architecture

The generator follows a functional pipeline pattern:

- Content flows through pure transformations
- Each stage produces immutable data
- Side effects isolated at boundaries
- Highly testable and maintainable

</div>
</div>

---

<div class="layout-quote">

<blockquote>
Make illegal states unrepresentable.
</blockquote>

<div class="attribution">
— Yaron Minsky
</div>

</div>

---

## Implementation Steps

<div class="layout-three-column">
<div>

### Phase 1: Setup

- Install F# SDK
- Create project
- Add dependencies
- Setup tooling

</div>
<div>

### Phase 2: Build

- Define types
- Create parsers
- Build generators
- Write tests

</div>
<div>

### Phase 3: Deploy

- Run builds
- Verify output
- Deploy site
- Monitor performance

</div>
</div>

---

<div class="layout-centered">

## Key Takeaways

F# provides excellent tools for static site generation

Type safety catches errors at compile time

Functional programming leads to maintainable code

**Ready to start building?**

</div>

---

<div class="layout-big-text">

# Questions?

</div>

---

<div class="layout-centered">

# Thank You!

**Contact:** hello@lqdev.me

**GitHub:** github.com/lqdev

**Website:** lqdev.me

</div>
