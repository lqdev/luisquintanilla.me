#r "nuget: Markdig, 0.41.3"
#r "nuget: Giraffe.ViewEngine, 2.0.0-alpha-1"
#r "nuget: YamlDotNet, 16.3.0"

open System
open Markdig
open Markdig.Syntax
open Markdig.Parsers

module Domain = 

    type MediaType =
        | Image of string
        | Video of string
        | Audio of string

    with
        override this.ToString() =
            match this with
            | Image s -> s
            | Video s -> s
            | Audio s -> s
        static member ToMediaType (mediaType: string) =
            match mediaType with
            | "image" -> Image mediaType
            | "video" -> Video mediaType
            | "audio" -> Audio mediaType
            | _ -> failwith "Unknown media type"

    type AspectRatio = 
        | Square of string
        | Wide of string
        | Tall of string
        | Custom of string
    with 
        override this.ToString() =
            match this with
            | Square s -> s
            | Wide s -> s
            | Tall s -> s
            | Custom s -> s
        static member ToAspectRatio (aspect: string) =
            match aspect with
            | "1:1" -> Square "1:1"
            | "3:2" -> Wide "3:2"
            | "9:16" -> Tall "9:16"
            | _ -> Custom aspect

    type Media = {
        MediaType: MediaType
        Uri: string
        AltText: string
        Caption: string option
        AspectRatio: AspectRatio
    }

    // New type for post metadata from YAML front-matter
    [<CLIMutable>]
    type PostMetadata = {
        post_type: string
        title: string
        publish_date: string
        tags: string list
    }

    type Post = {
        Metadata: PostMetadata option
        TextContent: string
        MediaItems: Media list
    }

// Date formatting utilities
module DateUtils =
    open System
    open System.Globalization
    
    /// Format a date string for display as YYYY-MM-DD HH:MM
    /// Preserves original format if parsing fails
    let formatDisplayDate (dateString: string) : string =
        try
            // Try parsing common ISO 8601 formats with timezone
            let formats = [|
                "yyyy-MM-dd HH:mm zzz"  // 2025-07-05 11:47 -05:00
                "yyyy-MM-dd HH:mm:ss zzz"  // 2025-07-05 11:47:30 -05:00
                "yyyy-MM-ddTHH:mm:ssK"  // 2025-07-05T11:47:30-05:00
                "yyyy-MM-dd HH:mm"  // 2025-07-05 11:47 (already in target format)
                "yyyy-MM-dd"  // 2025-07-05 (date only)
            |]
            
            let mutable parsedDate = DateTime.MinValue
            if DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, &parsedDate) then
                // Format as YYYY-MM-DD HH:MM
                parsedDate.ToString("yyyy-MM-dd HH:mm")
            else
                // Fallback: try general parsing
                let mutable generalParsed = DateTime.MinValue
                if DateTime.TryParse(dateString, &generalParsed) then
                    generalParsed.ToString("yyyy-MM-dd HH:mm")
                else
                    // If all parsing fails, return original string
                    dateString
        with
        | _ -> dateString  // Return original string on any exception

// Error types for comprehensive error handling (Phase 4)
module ErrorTypes =
    
    // Parse errors that can occur during markdown processing
    type ParseError = 
        | YamlParseError of string
        | MediaParseError of string  
        | FileNotFound of string
        | InvalidMarkdownStructure of string
        | MissingRequiredField of field: string * context: string
    
    // Generation errors that can occur during post generation
    type GenerationError =
        | ParseError of ParseError
        | RenderError of string
        | FileWriteError of string
        | ValidationError of ValidationError
    
    // Validation errors for input validation
    and ValidationError =
        | InvalidMediaType of string
        | InvalidAspectRatio of string
        | MissingTitle
        | MissingPostType
        | EmptyUri of mediaIndex: int
        | EmptyAltText of mediaIndex: int
        | InvalidDateFormat of date: string

open Domain

// Custom block for media gallery - moved outside Domain since it's Markdig-specific
type MediaBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val MediaItems: Domain.Media list = [] with get, set
    member val RawContent: string = "" with get, set

module MarkdownParser = 
    
    open YamlDotNet.Serialization
    open YamlDotNet.Serialization.NamingConventions
    open Markdig.Renderers
    open Markdig.Renderers.Html

    // ParsedDocument type as specified in refactor plan
    type ParsedDocument = {
        Metadata: Domain.PostMetadata option
        TextContent: string
        MediaItems: Domain.Media list
        RawMarkdown: string
    }

    // DTO for YAML deserialization
    [<CLIMutable>]
    type MediaItemDto = {
        media_type: string
        uri: string
        caption: string
        alt_text: string
        aspect: string
    }
    
    // Parse YAML front-matter with proper error handling
    let parseFrontMatter (content: string) : Result<Domain.PostMetadata option * string, ErrorTypes.ParseError> =
        try
            let lines = content.Split([|'\n'|], StringSplitOptions.None)
            
            if lines.Length > 0 && lines.[0].Trim() = "---" then
                let endIdx = 
                    lines 
                    |> Array.skip 1
                    |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                
                match endIdx with
                | Some idx ->
                    let frontMatterLines = lines.[1..idx]
                    let frontMatterYaml = String.concat "\n" frontMatterLines
                    let remainingContent = 
                        lines.[(idx + 2)..]
                        |> String.concat "\n"
                    
                    try
                        let deserializer = 
                            DeserializerBuilder()
                                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                .Build()
                        
                        let metadata = deserializer.Deserialize<Domain.PostMetadata>(frontMatterYaml)
                        Ok (Some metadata, remainingContent)
                    with
                    | ex -> 
                        Error (ErrorTypes.YamlParseError $"Failed to parse YAML front-matter: {ex.Message}")
                | None ->
                    Error (ErrorTypes.InvalidMarkdownStructure "Found opening --- but no closing --- for front-matter")
            else
                Ok (None, content)
        with
        | ex -> Error (ErrorTypes.YamlParseError $"Error processing front-matter: {ex.Message}")
    
    // Parse the YAML-like content inside the media block with proper error handling
    let parseMediaItems (content: string) : Result<Domain.Media list, ErrorTypes.ParseError> =
        try
            // Fix indentation for YAML parsing
            let lines = content.Split([|'\n'|], StringSplitOptions.None)
            let fixedLines = 
                lines
                |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
                |> Array.map (fun line ->
                    let trimmed = line.Trim()
                    if trimmed.StartsWith("- ") then
                        trimmed  // Keep list items at the beginning
                    elif trimmed.Contains(":") && not (trimmed.StartsWith("- ")) then
                        "  " + trimmed  // Indent properties with 2 spaces
                    else
                        trimmed
                )
            
            let cleanContent = String.concat "\n" fixedLines

            let deserializer = 
                DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
            
            let yamlItems = deserializer.Deserialize<MediaItemDto list>(cleanContent)
            
            let mediaItems = 
                yamlItems
                |> List.map (fun dto ->
                    let mediaType = 
                        match dto.media_type with
                        | null | "" -> MediaType.ToMediaType "image"
                        | mt -> MediaType.ToMediaType mt
                    
                    let uri = 
                        match dto.uri with
                        | null -> ""
                        | u -> u
                    
                    let altText = 
                        match dto.alt_text with
                        | null -> ""
                        | alt -> alt
                    
                    let caption = 
                        match dto.caption with
                        | null | "" -> None
                        | c when String.IsNullOrWhiteSpace(c) -> None
                        | c -> Some c
                    
                    let aspectRatio = 
                        match dto.aspect with
                        | null | "" -> AspectRatio.ToAspectRatio "1:1"
                        | a -> AspectRatio.ToAspectRatio a
                    
                    { 
                        MediaType = mediaType
                        Uri = uri
                        AltText = altText
                        Caption = caption
                        AspectRatio = aspectRatio
                    })
            
            Ok mediaItems
        with
        | ex -> 
            Error (ErrorTypes.MediaParseError $"Failed to parse media items: {ex.Message}")

    // AST-based text extraction to replace string manipulation
    let extractTextContentFromAst (doc: MarkdownDocument) : string =
        // Create a new writer and renderer for HTML output
        let writer = new System.IO.StringWriter()
        let renderer = new HtmlRenderer(writer)
        
        // Create a visitor that will filter out MediaBlocks
        let filteredWriter = new System.IO.StringWriter()
        let filteredRenderer = new HtmlRenderer(filteredWriter)
        
        // Iterate through blocks and render only non-MediaBlocks
        for block in doc do
            if not (block :? MediaBlock) then
                filteredRenderer.Render(block) |> ignore
        
        filteredWriter.ToString()

    // AST-based media extraction to centralize media parsing
    let extractMediaFromAst (doc: MarkdownDocument) : Domain.Media list =
        doc.Descendants<MediaBlock>()
        |> Seq.collect (fun block -> block.MediaItems)
        |> Seq.toList

    // Centralized parsing function as single entry point with proper error handling
    let parseDocument (pipeline: MarkdownPipeline) (content: string) : Result<ParsedDocument, ErrorTypes.ParseError> =
        match parseFrontMatter content with
        | Error parseError -> Error parseError
        | Ok (metadata, contentWithoutFrontMatter) ->
            try
                let doc = Markdown.Parse(contentWithoutFrontMatter, pipeline)
                
                Ok {
                    Metadata = metadata
                    TextContent = extractTextContentFromAst doc
                    MediaItems = extractMediaFromAst doc
                    RawMarkdown = content
                }
            with
            | ex -> Error (ErrorTypes.InvalidMarkdownStructure $"Failed to parse markdown: {ex.Message}")

    // Block parser class
    type MediaBlockParser() =
        inherit BlockParser()
        
        override this.TryOpen(processor) =
            if processor.Line.ToString().TrimStart().StartsWith(":::media") then
                let mediaBlock = MediaBlock(this)  // Pass parser to constructor
                processor.NewBlocks.Push(mediaBlock)
                BlockState.ContinueDiscard
            else
                BlockState.None
                
        override _.TryContinue(processor, block) =
            let line = processor.Line
            let lineText = line.ToString().TrimStart()
            
            if lineText = ":::media" then
                // End of media block
                let mediaBlock = block :?> MediaBlock
                match parseMediaItems mediaBlock.RawContent with
                | Ok items -> mediaBlock.MediaItems <- items
                | Error _ -> mediaBlock.MediaItems <- []  // Fallback to empty list on error
                processor.Close(block)
                BlockState.BreakDiscard
            elif lineText.StartsWith(":::") then
                // Different fence type, close this block
                processor.Close(block)
                BlockState.BreakDiscard
            else
                // Continue collecting content - preserve original indentation
                let mediaBlock = block :?> MediaBlock
                let originalLine = processor.Line.ToString()
                mediaBlock.RawContent <- mediaBlock.RawContent + originalLine + "\n"
                BlockState.Continue
                
        override _.Close(processor, block) =
            let mediaBlock = block :?> MediaBlock
            if mediaBlock.MediaItems.IsEmpty then
                match parseMediaItems mediaBlock.RawContent with
                | Ok items -> mediaBlock.MediaItems <- items
                | Error _ -> mediaBlock.MediaItems <- []  // Fallback to empty list on error
            true

module MediaRenderer =
    
    open Giraffe.ViewEngine
    open Markdig.Renderers
    open Markdig.Renderers.Html

    let private formatCssAspectRatio (aspect: AspectRatio) =

        let ratioParts (ratio:string) =
            let parts = ratio.Split(":")
            if parts.Length = 2 then
                $"{parts.[0].Trim()} / {parts.[1].Trim()}"
            else
                "auto"

        match aspect with
        | Square x -> ratioParts (x.ToString())
        | Wide x -> ratioParts (x.ToString())
        | Tall x -> ratioParts (x.ToString())
        | Custom ratio -> ratioParts (ratio.ToString())

    let renderLayout (postContent: XmlNode option) (mediaGallery: XmlNode) (metadata: Domain.PostMetadata option) =
        html [] [
            head [] [
                link [ _rel "stylesheet"; _href "main.css" ]
            ]
            body [] [
                div [ _class "feed-container" ] [
                    article [_class "post-card"] [
                        match metadata with
                        | Some meta ->
                            div [ _class "post-header" ] [
                                h1 [ _class "post-title" ] [ str meta.title ]
                                div [ _class "post-meta" ] [
                                    time [ _class "post-date"; _datetime meta.publish_date ] [ str (DateUtils.formatDisplayDate meta.publish_date) ]
                                    div [ _class "post-tags" ] [
                                        for tag in meta.tags do
                                            span [ _class "tag" ] [ str tag ]
                                    ]
                                ]
                            ]
                        | None -> ()
                        
                        match postContent with
                        | Some content -> 
                            div [ _class "post-content" ] [
                                content
                                mediaGallery
                            ]
                        | None -> mediaGallery
                    ]
                ]
            ]
        ]

    let private renderMediaItem (item: Media) =
        let mediaElement = 
            match item.MediaType with
            | Image _ ->
                img [ _src item.Uri; _alt item.AltText ]
            | Video _ ->
                video [ _controls ] [
                    source [ _src item.Uri ]
                ]
            | Audio _ ->
                audio [ _controls ] [
                    source [ _src item.Uri ]
                ]
        
        let captionElement = 
            match item.Caption with
            | Some caption -> [ p [ _class "media-caption" ] [ str caption ] ]
            | None -> []
        
        // Audio elements should not have aspect ratio constraints
        let containerAttributes = 
            match item.MediaType with
            | Audio _ -> [ _class "media-item audio-item" ]
            | _ -> [ 
                _class "media-item"
                _style $"aspect-ratio: {formatCssAspectRatio item.AspectRatio}"
            ]
        
        div containerAttributes (mediaElement :: captionElement)

    let renderMediaGallery (mediaItems: Media list) =
        div [ _class "media-gallery" ] (
            mediaItems |> List.map renderMediaItem
        )

    // HTML renderer class
    type MediaRenderer() =
        inherit HtmlObjectRenderer<MediaBlock>()
        
        override _.Write(renderer: HtmlRenderer, mediaBlock: MediaBlock) =
            let mediaGallery = renderMediaGallery mediaBlock.MediaItems
            let html = RenderView.AsString.htmlNode (renderLayout None mediaGallery None)
            renderer.Write(html: string) |> ignore

module MediaExtension =
    
    open MarkdownParser
    open MediaRenderer

    // Extension class
    type MediaExtension() =
        interface IMarkdownExtension with
            member _.Setup(pipeline) =
                let parser = MediaBlockParser()
                pipeline.BlockParsers.Insert(0, parser)
                
            member _.Setup(pipeline, renderer) =
                let htmlRenderer = MediaRenderer()
                renderer.ObjectRenderers.Add(htmlRenderer)

open System.IO
    
    open MarkdownParser
    open MediaRenderer

    // Extension class
    type MediaExtension() =
        interface IMarkdownExtension with
            member _.Setup(pipeline) =
                let parser = MediaBlockParser()
                pipeline.BlockParsers.Insert(0, parser)
                
            member _.Setup(pipeline, renderer) =
                let htmlRenderer = MediaRenderer()
                renderer.ObjectRenderers.Add(htmlRenderer)

open System.IO
open MediaExtension
open Giraffe.ViewEngine

let pipeline = 
    MarkdownPipelineBuilder()
        .Use<MediaExtension>()
        .Build()

// Add ContentProcessor implementation now that pipeline is available
module ContentProcessor =
    
    open Giraffe.ViewEngine
    open Domain
    
    type ProcessedPost = {
        Document: MarkdownParser.ParsedDocument
        TextHtml: string option
        MediaGallery: XmlNode
        Header: XmlNode option
        PostTitle: string option
    }
    
    let processPost (markdownContent: string) : Result<ProcessedPost, ErrorTypes.GenerationError> =
        // Parse the document using Phase 1's centralized parser
        match MarkdownParser.parseDocument pipeline markdownContent with
        | Error parseError -> Error (ErrorTypes.ParseError parseError)
        | Ok parsedDoc ->
            try
                // Process text content - convert to HTML if present
                let textHtml = 
                    if String.IsNullOrWhiteSpace(parsedDoc.TextContent) then
                        None
                    else 
                        Some parsedDoc.TextContent
                
                // Generate media gallery using existing renderer
                let mediaGallery = MediaRenderer.renderMediaGallery parsedDoc.MediaItems
                
                // Process header information
                let headerNode = 
                    match parsedDoc.Metadata with
                    | Some meta ->
                        Some (div [ _class "post-header" ] [
                            h1 [ _class "post-title" ] [ str meta.title ]
                            div [ _class "post-meta" ] [
                                time [ _class "post-date"; _datetime meta.publish_date ] [ str (DateUtils.formatDisplayDate meta.publish_date) ]
                                div [ _class "post-tags" ] [
                                    for tag in meta.tags do
                                        span [ _class "tag" ] [ str tag ]
                                ]
                            ]
                        ])
                    | None -> None
                
                // Extract post title for easy access
                let postTitle = 
                    match parsedDoc.Metadata with
                    | Some meta -> Some meta.title
                    | None -> None
                
                Ok {
                    Document = parsedDoc
                    TextHtml = textHtml
                    MediaGallery = mediaGallery
                    Header = headerNode
                    PostTitle = postTitle
                }
            with
            | ex -> Error (ErrorTypes.RenderError $"Failed to process post: {ex.Message}")

// Enhanced rendering function that takes ProcessedPost
let renderProcessedPost (processedPost: ContentProcessor.ProcessedPost) =
    // Create text content node if present
    let textContentNode = 
        match processedPost.TextHtml with
        | Some html -> Some (div [ _class "post-text" ] [ rawText html ])
        | None -> None
    
    // Create content nodes list
    let contentNodes = [
        match textContentNode with
        | Some node -> yield node
        | None -> ()
        yield processedPost.MediaGallery
    ]
    
    // Generate complete HTML page using Giraffe ViewEngine
    let fullPage = html [] [
        head [] [
            link [ _rel "stylesheet"; _href "main.css" ]
        ]
        body [] [
            div [ _class "feed-container" ] [
                article [ _class "post-card" ] [
                    match processedPost.Header with
                    | Some header -> yield header
                    | None -> ()
                    yield div [ _class "post-content" ] contentNodes
                ]
            ]
        ]
    ]
    
    RenderView.AsString.htmlDocument fullPage

let extractMediaFromMarkdown (markdownContent: string) =
    // Use new centralized parser instead of duplicating parsing logic
    match MarkdownParser.parseDocument pipeline markdownContent with
    | Ok parsedDoc -> parsedDoc.MediaItems
    | Error _ -> []  // Fallback to empty list on error

let extractTextContent (markdownContentWithoutFrontMatter: string) =
    // Use AST-based extraction instead of string manipulation
    let doc = Markdown.Parse(markdownContentWithoutFrontMatter, pipeline)
    MarkdownParser.extractTextContentFromAst doc

let generatePostHtml (markdownContent: string) =
    // Phase 2: Use Process → Render workflow with error handling
    match ContentProcessor.processPost markdownContent with
    | Ok processedPost -> renderProcessedPost processedPost
    | Error error -> 
        // Create error page for debugging
        $"<html><body><h1>Error Processing Post</h1><p>{error}</p></body></html>"

module PostGenerator =
    
    open ErrorTypes
    
    type PostConfig = {
        SourceFile: string
        OutputFile: string
        PostType: string
    }
    
    let generatePost (config: PostConfig) : Result<string, ErrorTypes.GenerationError> =
        try
            // Validate file exists before processing
            if not (File.Exists(config.SourceFile)) then
                Error (ParseError (FileNotFound config.SourceFile))
            else
                // Read source file
                let markdownContent = File.ReadAllText(config.SourceFile)
                
                // Process with validation - use ContentProcessor which now returns Result
                match ContentProcessor.processPost markdownContent with
                | Error error -> Error error  // Pass through processing errors
                | Ok processedPost ->
                    // TODO: Add validation step once Validation module is accessible
                    // For now, skip validation and proceed to rendering
                    try
                        // Generate HTML using existing renderer
                        let html = renderProcessedPost processedPost
                        
                        // Write output file
                        File.WriteAllText(config.OutputFile, html)
                        
                        // Log success
                        printfn "Generated %s post: %s -> %s" config.PostType config.SourceFile config.OutputFile
                        printfn "Generated HTML:"
                        printfn "%s" html
                        
                        Ok html
                    with
                    | ex -> Error (FileWriteError $"Failed to write {config.OutputFile}: {ex.Message}")
        with
        | ex -> Error (ParseError (FileNotFound $"Error reading {config.SourceFile}: {ex.Message}"))
    
    let generateAllPosts (configs: PostConfig list) : unit =
        configs
        |> List.iter (fun config ->
            match generatePost config with
            | Ok _ -> ()
            | Error error -> printfn "Error: %A" error
        )

open ErrorTypes

// Validation module for input validation (Phase 4)
module Validation =
    
    open System.Text.RegularExpressions
    
    let validateMetadata (metadata: Domain.PostMetadata option) : Result<Domain.PostMetadata, ErrorTypes.ValidationError> =
        match metadata with
        | None -> Error ErrorTypes.MissingTitle  // If no metadata, we're missing essential fields
        | Some meta ->
            // Validate required fields
            if String.IsNullOrWhiteSpace(meta.title) then
                Error ErrorTypes.MissingTitle
            elif String.IsNullOrWhiteSpace(meta.post_type) then
                Error ErrorTypes.MissingPostType
            elif String.IsNullOrWhiteSpace(meta.publish_date) then
                Error (ErrorTypes.InvalidDateFormat meta.publish_date)
            else
                // Validate date format (basic ISO date check)
                let datePattern = @"^\d{4}-\d{2}-\d{2}$"
                if not (Regex.IsMatch(meta.publish_date, datePattern)) then
                    Error (ErrorTypes.InvalidDateFormat meta.publish_date)
                else
                    Ok meta
    
    let validateMediaItem (item: Domain.Media) (index: int) : Result<Domain.Media, ErrorTypes.ValidationError> =
        // Validate URI is not empty
        if String.IsNullOrWhiteSpace(item.Uri) then
            Error (ErrorTypes.EmptyUri index)
        // Validate alt text is not empty (accessibility requirement)
        elif String.IsNullOrWhiteSpace(item.AltText) then
            Error (ErrorTypes.EmptyAltText index)
        else
            // Validate media type is valid
            let isValidMediaType = 
                match item.MediaType.ToString() with
                | "image" | "video" | "audio" -> true
                | _ -> false
            
            if not isValidMediaType then
                Error (ErrorTypes.InvalidMediaType (item.MediaType.ToString()))
            else
                // Validate aspect ratio format
                let isValidAspectRatio = 
                    let ratio = item.AspectRatio.ToString()
                    if ratio.Contains(":") then
                        let parts = ratio.Split(':')
                        parts.Length = 2 && 
                        (Int32.TryParse(parts.[0]) |> fst) && 
                        (Int32.TryParse(parts.[1]) |> fst)
                    else
                        false
                
                if not isValidAspectRatio then
                    Error (ErrorTypes.InvalidAspectRatio (item.AspectRatio.ToString()))
                else
                    Ok item
    
    let validatePost (post: MarkdownParser.ParsedDocument) : Result<MarkdownParser.ParsedDocument, ErrorTypes.ValidationError list> =
        // Validate metadata
        let metadataResult = validateMetadata post.Metadata
        
        // Validate all media items
        let mediaResults = 
            post.MediaItems
            |> List.mapi (fun i item -> validateMediaItem item i)
        
        // Collect all validation errors
        let metadataErrors = 
            match metadataResult with
            | Error err -> [err]
            | Ok _ -> []
        
        let mediaErrors = 
            mediaResults
            |> List.choose (function Error err -> Some err | Ok _ -> None)
        
        let allErrors = metadataErrors @ mediaErrors
        
        if allErrors.IsEmpty then
            Ok post
        else
            Error allErrors


// This is the actual code
// Configuration-driven post generation
let postConfigs = [
    { PostGenerator.SourceFile = Path.Combine("_src", "image.md"); PostGenerator.OutputFile = Path.Combine("_public", "image.html"); PostGenerator.PostType = "image" }
    { PostGenerator.SourceFile = Path.Combine("_src", "video.md"); PostGenerator.OutputFile = Path.Combine("_public", "video.html"); PostGenerator.PostType = "video" }
    { PostGenerator.SourceFile = Path.Combine("_src", "audio.md"); PostGenerator.OutputFile = Path.Combine("_public", "audio.html"); PostGenerator.PostType = "audio" }
    { PostGenerator.SourceFile = Path.Combine("_src", "mixed.md"); PostGenerator.OutputFile = Path.Combine("_public", "mixed.html"); PostGenerator.PostType = "mixed" }
]

// Generate all posts using the new PostGenerator module
PostGenerator.generateAllPosts postConfigs