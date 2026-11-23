---
applyTo: "**/*.fs,**/*.fsx,**/*.fsproj"
---

# F#

The generator is one F# project, `PersonalSite.fsproj`, plus scripts in `test-scripts/` and `Scripts/`.

- Add every new `.fs` file as a `<Compile>` item in `PersonalSite.fsproj`, after every file it uses, because F# only sees what compiled before it.
- Qualify union cases (`MediaType.Unknown`, not `Unknown`), because unions share case names (two in `MediaTypes.fs` both have `Unknown`) and an unqualified case can bind to the wrong one.
- Use the literals and the `ContentType` union in `ContentTypes.fs` instead of a raw content-type string, because those strings are a wire format in feeds, URLs, and JSON.
- Don't add `UseAdvancedExtensions()` to a Markdig pipeline that has the custom block parsers, because its custom containers take over the `:::` blocks. Register the extensions you need one at a time.
- Upcast a document to `MarkdownObject` before calling `Descendants<LinkInline>()`, as `AtProtoResponseMapping.fs` does, or F# picks the block-only overload and finds nothing.
- Normalize tags through `processTagName` in `Services/Tag.fs`, so every page, feed, and index agrees on a tag's spelling.
- Make a record public if System.Text.Json serializes it. A private record serializes as `{}` without an error.
- When you move code into a new module, annotate parameter and record types, because a changed `open` order can silently change which record type F# infers.
- Start a new test script from the header of `test-scripts/test-rss-cdata.fsx`: it references `bin/Debug/net11.0/PersonalSite.dll`, runs after `dotnet build`, and exits 1 on any failure so CI can gate on it.

Read first: `docs/architecture.md`
