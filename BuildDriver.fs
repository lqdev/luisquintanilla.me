module BuildDriver

open System.IO
open Giraffe.ViewEngine

// =============================================================================
// Generic content-type build driver (F1 — Phase 2.1).
//
// Replaces the ~11 near-identical hand-written `buildX` functions in Builder.fs
// (enumerate .md files -> parse -> render each page -> render index -> write)
// with a single data-driven driver. Per-type rendering closures (ItemView /
// IndexConfig.View) are supplied by the caller (Builder.fs), which has access to
// the Views layer; this module only orchestrates I/O. Compiled after Generator.fs
// (it calls ViewGenerator.generate) and before Builder.fs (the consumer) —
// hence it sits later than GenericBuilder.fs, unlike the plan's first sketch.
// =============================================================================

/// How to render a content type's index/listing page. Optional on the build
/// config: some types (e.g. bookmarks) don't own an index — their landing page
/// is built elsewhere from a different data source.
type IndexConfig<'T> = {
    /// Index page content from all items.
    View: 'T array -> XmlNode
    /// Full <title> for the index page.
    Title: string
    /// Optional ordering applied before rendering; None = source (feed) order.
    Sort: ('T array -> 'T array) option
}

/// Declarative description of how to build one content type's pages (+ index).
type ContentTypeBuild<'T> = {
    /// ContentTypes literal (1.3) — identity/diagnostics; not output-affecting today.
    Name: string
    /// Source dir segments under srcDir, e.g. ["resources"; "snippets"].
    SourceDir: string list
    /// Output dir segments under outputDir, e.g. ["resources"; "snippets"].
    OutputDir: string list
    /// Content processor (parse / render / feed).
    Processor: GenericBuilder.ContentProcessor<'T>
    /// Per-item slug used as the page's directory name (typically FileName).
    Slug: 'T -> string
    /// Inner page content for one item, given all siblings (for related content).
    ItemView: 'T -> 'T array -> XmlNode
    /// Full <title> for an item page.
    ItemTitle: 'T -> string
    /// Layout key passed to ViewGenerator.generate (e.g. "defaultindex").
    Layout: string
    /// Index page config; None = build individual pages only (no index).
    Index: IndexConfig<'T> option
}

/// Join path segments using only the 2-arg Path.Join (byte-identical to the
/// multi-arg Path.Join the old buildX functions used for these relative paths).
let private joinPath (segments: string list) =
    match segments with
    | [] -> ""
    | head :: tail -> tail |> List.fold (fun acc s -> Path.Join(acc, s)) head

/// Build all individual pages (+ the index, if configured) for one content type.
/// Returns the FeedData for downstream feed/RSS aggregation (same contract as the
/// old buildX functions).
let buildContentType<'T> (srcDir: string) (outputDir: string) (cfg: ContentTypeBuild<'T>)
    : GenericBuilder.FeedData<'T> list =

    let files =
        let sourcePath = joinPath (srcDir :: cfg.SourceDir)
        if Directory.Exists(sourcePath) then
            Directory.GetFiles(sourcePath)
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        else
            []

    let feedData = GenericBuilder.buildContentWithFeeds cfg.Processor files
    let items = feedData |> List.map (fun item -> item.Content) |> List.toArray

    // Individual pages
    feedData
    |> List.iter (fun item ->
        let content = item.Content
        let saveDir = joinPath (outputDir :: (cfg.OutputDir @ [ cfg.Slug content ]))
        Directory.CreateDirectory(saveDir) |> ignore
        let html = ViewGenerator.generate (cfg.ItemView content items) cfg.Layout (cfg.ItemTitle content)
        File.WriteAllText(Path.Join(saveDir, "index.html"), html))

    // Index page (only when configured)
    match cfg.Index with
    | Some index ->
        let indexItems =
            match index.Sort with
            | Some sort -> sort items
            | None -> items
        let indexHtml = ViewGenerator.generate (index.View indexItems) cfg.Layout index.Title
        let indexSaveDir = joinPath (outputDir :: cfg.OutputDir)
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), indexHtml)
    | None -> ()

    feedData
