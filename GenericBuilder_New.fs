module GenericBuilder

open Domain
open ASTParsing
open CustomBlocks
open BlockRenderers
open System.Xml.Linq
open System
open System.IO

/// Data structure for feed generation with both card and RSS item
type FeedData<'T> = {
    Content: 'T
    CardHtml: string
    RssXml: XElement option
}

/// Generic content processor pattern for consistent content handling
type ContentProcessor<'T> = {
    /// Parse content from file path to domain type
    Parse: string -> 'T option
    /// Render content to final HTML output
    Render: 'T -> string
    /// Generate output file path from content
    OutputPath: 'T -> string
    /// Generate card HTML for index pages
    RenderCard: 'T -> string
    /// Generate RSS XML element
    RenderRss: 'T -> XElement option
}

/// Build content and generate feed data in a single pass
let buildContentWithFeeds<'T> (processor: ContentProcessor<'T>) (filePaths: string list) : FeedData<'T> list =
    filePaths
    |> List.choose (fun filePath ->
        match processor.Parse filePath with
        | Some content ->
            let cardHtml = processor.RenderCard content
            let rssXml = processor.RenderRss content
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | None -> None)

/// Content processors for existing domain types

/// Post content processor
module PostProcessor =
    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parseDocumentFromFile<Post> filePath with
            | Ok post -> Some post
            | Error _ -> None
        
        Render = fun post ->
            // TODO: Integrate with existing Views.Generator or create new renderer
            sprintf "<article>%s</article>" post.Content
        
        OutputPath = fun post ->
            sprintf "posts/%s.html" post.FileName
        
        RenderCard = fun post ->
            let title = Html.escapeHtml post.Metadata.Title
            let excerpt = Html.escapeHtml (post.Content.Substring(0, min 150 post.Content.Length) + "...")
            let url = sprintf "/posts/%s" post.FileName
            
            Html.element "article" (Html.attribute "class" "post-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt +
                 Html.element "time" "" (Html.escapeHtml (post.Metadata.Date.ToString())))
        
        RenderRss = fun post ->
            // Create RSS item for post using existing pattern
            let url = sprintf "https://www.luisquintanilla.me/posts/%s" post.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", post.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" post.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", post.Metadata.Date.ToString()))
            Some item
    }

/// Snippet content processor  
module SnippetProcessor =
    let create() : ContentProcessor<Snippet> = {
        Parse = fun filePath ->
            match parseDocumentFromFile<Snippet> filePath with
            | Ok snippet -> Some snippet
            | Error _ -> None
        
        Render = fun snippet ->
            sprintf "<article>%s</article>" snippet.Content
        
        OutputPath = fun snippet ->
            sprintf "snippets/%s.html" snippet.FileName
        
        RenderCard = fun snippet ->
            let title = Html.escapeHtml snippet.Metadata.Title
            let excerpt = Html.escapeHtml (snippet.Content.Substring(0, min 150 snippet.Content.Length) + "...")
            let url = sprintf "/snippets/%s" snippet.FileName
            
            Html.element "article" (Html.attribute "class" "snippet-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun snippet ->
            // Create RSS item for snippet similar to post
            let url = sprintf "https://www.luisquintanilla.me/snippets/%s" snippet.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", snippet.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" snippet.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            Some item
    }

/// Wiki content processor
module WikiProcessor =
    let create() : ContentProcessor<Wiki> = {
        Parse = fun filePath ->
            match parseDocumentFromFile<Wiki> filePath with
            | Ok wiki -> Some wiki
            | Error _ -> None
        
        Render = fun wiki ->
            sprintf "<article>%s</article>" wiki.Content
        
        OutputPath = fun wiki ->
            sprintf "wiki/%s.html" wiki.FileName
        
        RenderCard = fun wiki ->
            let title = Html.escapeHtml wiki.Metadata.Title
            let excerpt = Html.escapeHtml (wiki.Content.Substring(0, min 150 wiki.Content.Length) + "...")
            let url = sprintf "/wiki/%s" wiki.FileName
            
            Html.element "article" (Html.attribute "class" "wiki-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun wiki ->
            // Create RSS item for wiki similar to post
            let url = sprintf "https://www.luisquintanilla.me/wiki/%s" wiki.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", wiki.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" wiki.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            Some item
    }

/// Generic content processing pipeline
module ContentPipeline =
    
    /// Process all content of a specific type and generate both HTML and feeds
    let processAllContent<'T> (processor: ContentProcessor<'T>) (sourceDirectory: string) (outputDirectory: string) =
        if Directory.Exists sourceDirectory then
            let files = Directory.GetFiles(sourceDirectory, "*.md")
            let feedData = buildContentWithFeeds processor (Array.toList files)
            
            // Generate individual content files
            feedData
            |> List.iter (fun data ->
                let outputPath = Path.Combine(outputDirectory, processor.OutputPath data.Content)
                let outputDir = Path.GetDirectoryName(outputPath)
                if not (Directory.Exists(outputDir)) then
                    Directory.CreateDirectory(outputDir) |> ignore
                
                let html = processor.Render data.Content
                File.WriteAllText(outputPath, html))
            
            // Return feed data for aggregation
            feedData
        else
            []
    
    /// Build main RSS feeds from all content types
    let buildMainFeeds (allFeedData: FeedData<obj> list) (outputDirectory: string) =
        let rssItems = 
            allFeedData
            |> List.choose (fun data -> data.RssXml)
            |> List.take (min 20 allFeedData.Length)  // Latest 20 items or fewer
        
        // TODO: Generate main RSS feed XML using existing RssService
        // TODO: Generate ATOM feed
        // TODO: Generate JSON feed
        
        ()

/// Main build orchestration
module Builder =
    
    /// Enhanced build process using generic content processors
    let buildSiteWithGenericPipeline (sourceRoot: string) (outputRoot: string) =
        
        // Process all content types
        let postFeedData = 
            ContentPipeline.processAllContent 
                (PostProcessor.create()) 
                (Path.Combine(sourceRoot, "posts"))
                outputRoot
        
        let snippetFeedData = 
            ContentPipeline.processAllContent 
                (SnippetProcessor.create()) 
                (Path.Combine(sourceRoot, "snippets"))
                outputRoot
        
        let wikiFeedData = 
            ContentPipeline.processAllContent 
                (WikiProcessor.create()) 
                (Path.Combine(sourceRoot, "wiki"))
                outputRoot
        
        // Combine all feed data (note: would need proper type handling for mixed types)
        // let allFeedData = List.concat [ postFeedData; snippetFeedData; wikiFeedData ]
        
        // Build main feeds
        // ContentPipeline.buildMainFeeds allFeedData outputRoot
        
        sprintf "Processed %d posts, %d snippets, %d wiki pages" 
            postFeedData.Length snippetFeedData.Length wikiFeedData.Length
