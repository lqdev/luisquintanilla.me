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

### 3. GenericBuilder.fs — Create processor

Add a `ContentProcessor<MyContent>` using the existing pattern:

```fsharp
module MyContentProcessor =
    let create () : ContentProcessor<MyContent> = {
        Parse = fun filePath -> ...
        Render = fun entry -> ...
        OutputPath = fun entry -> ...
        RenderCard = fun entry -> ...
        RenderRss = fun entry -> ...
    }
```

Also add to `convertToUnified` and `buildAllFeeds` if the content type should appear
in the unified feed and/or have its own RSS feed.

### 4. Builder.fs — Add build function

Create `buildMyContent` function following the pattern of existing build functions.
This orchestrates: load source files → parse → render → write output.

### 5. Program.fs — Wire into main pipeline

Add the build function call in the main pipeline, after data collection.

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
- Add the new `.fs` module to `PersonalSite.fsproj` if creating a new file
- Date field names vary by type: posts use `published_date`, responses use `dt_published`
