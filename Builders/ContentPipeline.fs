module ContentPipeline

open System
open System.IO
open Domain
open GenericBuilder
open IOUtils
open ViewGenerator
open Giraffe.ViewEngine

/// Configuration for building a content type
type ContentConfig<'T> = {
    /// Source directory relative to srcDir (e.g., "posts", "notes")
    SourceDir: string
    /// Output directory relative to outputDir (e.g., "posts", "resources/snippets")
    OutputDir: string
    /// Content type identifier (e.g., "posts", "notes")
    ContentType: string
    /// Content processor for parsing and rendering
    Processor: ContentProcessor<'T>
    /// Function to get the filename from content
    GetFileName: 'T -> string
    /// Function to get the title from content
    GetTitle: 'T -> string
    /// View function for individual content page
    IndividualPageView: 'T -> XmlNode
    /// View function for index/listing page
    IndexPageView: 'T array -> XmlNode
    /// Title for the index page
    IndexTitle: string
    /// Optional: Function to get sort date from content (defaults to Date field)
    GetSortDate: ('T -> DateTime) option
}

/// Build a content type using generic pipeline
let buildContent<'T> 
    (srcDir: string) 
    (outputDir: string) 
    (config: ContentConfig<'T>) 
    : FeedData<'T> list =
    
    // Get source files
    let sourceDir = Path.Join(srcDir, config.SourceDir)
    let files = getMarkdownFiles sourceDir
    
    // Build content with feeds
    let feedData = buildContentWithFeeds config.Processor files
    
    // Generate individual pages
    feedData
    |> List.iter (fun item ->
        let content = item.Content
        let fileName = config.GetFileName content
        let saveDir = Path.Join(outputDir, config.OutputDir, fileName)
        ensureDirectory saveDir |> ignore
        
        let html = config.IndividualPageView content
        let title = config.GetTitle content
        let view = generate html "defaultindex" title
        writeFile (Path.Join(saveDir, "index.html")) view)
    
    // Generate index page
    let allContent = feedData |> List.map (fun item -> item.Content) |> List.toArray
    
    // Sort content by date
    let sortedContent = 
        match config.GetSortDate with
        | Some getSortDate -> 
            allContent |> Array.sortByDescending getSortDate
        | None ->
            // Default: assume content has a Date field accessible via reflection or ITaggable
            allContent |> Array.sortByDescending (fun c -> 
                match box c with
                | :? ITaggable as t -> DateTime.Parse(t.Date)
                | _ -> DateTime.MinValue)
    
    let indexHtml = generate (config.IndexPageView sortedContent) "defaultindex" config.IndexTitle
    let indexDir = Path.Join(outputDir, config.OutputDir)
    ensureDirectory indexDir |> ignore
    writeFile (Path.Join(indexDir, "index.html")) indexHtml
    
    // Return feed data for RSS generation
    feedData
