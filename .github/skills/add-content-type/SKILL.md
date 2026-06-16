---
name: add-content-type
description: "Add a new content type to the lqdev.me F# static site generator — complete 8-file checklist"
---

# Add Content Type to lqdev.me

This skill walks through adding a new content type to the F# static site generator.
A content type requires changes in 8 files, in this order.

## Prerequisites

- Working in the lqdev.me repo (verify `PersonalSite.fsproj` exists)
- `dotnet build` passes before starting

## The 8-File Checklist

### 1. Domain.fs — Define the type

Add a new details record type with `[<CLIMutable>]` and `[<YamlMember>]` attributes:

```fsharp
[<CLIMutable>]
type MyContentDetails = {
    [<YamlMember(Alias="title")>] Title: string
    [<YamlMember(Alias="description")>] Description: string
    [<YamlMember(Alias="published_date")>] PublishedDate: string
    // Add fields as needed
}
```

Add the wrapper record:

```fsharp
type MyContent = {
    FileName: string
    Metadata: MyContentDetails
    Content: string
    MarkdownSource: string option
}
```

Implement `ITaggable` if the content type has tags.

### 2. ASTParsing.fs — Add parse function

Add a parse function (usually a one-liner wrapping the generic parser):

```fsharp
let parseMyContentFromFile (filePath: string) =
    parseDocumentFromFile<MyContentDetails> filePath
```

### 3. Processors/MyContentProcessor.fs — Create processor

Create a new file `Processors/MyContentProcessor.fs` as a top-level module (no `=`) that
`open`s `GenericBuilder` for the core abstraction, then defines a `ContentProcessor<MyContent>`:

```fsharp
module MyContentProcessor

open GenericBuilder
// ...other opens as needed (Domain, ASTParsing, Giraffe.ViewEngine, ...)

let create () : ContentProcessor<MyContent> = {
    Parse = fun filePath -> ...
    Render = fun entry -> ...
    OutputPath = fun entry -> ...
    RenderCard = fun entry -> ...
    RenderRss = fun entry -> ...
}
```

Wire it into `PersonalSite.fsproj` **between `GenericBuilder.fs` and `UnifiedFeeds.fs`** (after the
other `Processors\*.fs` entries) — `UnifiedFeeds.fs` and the page builders consume processors, so
they must compile after. The core `ContentProcessor`/`FeedData` types and `buildContentWithFeeds`
stay in `GenericBuilder.fs`; reference them as `GenericBuilder.FeedData<_>` etc.

Also add a `convertMyContentToUnified` function in `UnifiedFeeds.fs` (and a feed in `buildAllFeeds`)
if the content type should appear in the unified feed and/or have its own RSS feed.

### 4. Builders/ContentTypePages.fs — Add build function

Add a `buildMyContent` delegate in `Builders/ContentTypePages.fs` (module `ContentTypePagesBuilder`)
following the existing `BuildDriver`-based pattern. It orchestrates: load source files → parse →
render → write output, returning the `GenericBuilder.FeedData<MyContent> list` for the unified feed.
Call the processor as `MyContentProcessor.create()` (unqualified — it's a top-level module).

### 5. Program.fs — Wire into main pipeline

Add the build function call in the main pipeline, after data collection
(`let myContentFeedData = buildMyContent()`), then add **one row** to the
`contentRoster` table (the B1 content-type registry):

```fsharp
{ Identity = ContentTypes.ContentType.MyContent; Unified = myContentUnified
  InTimeline = true; InAllFeeds = true; InBlogArchive = false }
```

The timeline / all-content / blog-archive feed lists DERIVE from this roster, so
you set participation once via the flags — there are no separate membership lists
to keep in sync. (Adding the `ContentType.MyContent` DU case in `ContentTypes.fs`
+ its `serialize`/`parse`/`urlPrefix` arms is compiler-enforced.) Tag-page
participation (`allTaggableContent`) and desktop nav (`Views/Navigation.fs`) remain
explicit and are edited separately when relevant.

### 6. Views/LayoutViews.fs — Page view

Create the individual page view function.

### 7. Views/CollectionViews.fs — Index/listing view

Create the collection/listing view for the content type landing page.

### 8. Views/Partials.fs — Re-export

Add re-exports if the content type introduces new shared view components.

## Validation

After all 8 files are updated:
1. `dotnet build` — must compile with 0 errors
2. `dotnet run` — must generate the site successfully
3. Check output in `_public/` for correct rendering

## Important Notes

- Use fully qualified type names (`MediaType.Unknown` not `Unknown`)
- ASTParsing.fs uses `.IgnoreUnmatchedProperties()` — new optional string fields
  default to null and need no parser changes
- Add any new `.fs` module to `PersonalSite.fsproj` respecting compile order:
  `Processors\*.fs` go between `GenericBuilder.fs` and `UnifiedFeeds.fs`; `Builders\*.fs` go
  after their dependencies (Views, Services, `UnifiedFeeds.fs`, `BuildDriver.fs`) and before `Program.fs`
- Date field names vary by type: posts use `published_date`, responses use `dt_published`
