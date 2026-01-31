module ASTParsing

open System
open System.IO
open Markdig
open Markdig.Syntax
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Import existing domain types and custom blocks
open Domain
open CustomBlocks

/// Result type for parsing operations with comprehensive error handling
type ParseError = 
    | YamlParseError of string
    | MarkdownParseError of string  
    | FileNotFound of string
    | InvalidMarkdownStructure of string
    | MissingRequiredField of field: string * context: string

/// Generic parsed document structure that can hold any content type metadata
type ParsedDocument<'TMetadata> = {
    Metadata: 'TMetadata option
    TextContent: string
    CustomBlocks: Map<string, obj list>  // Block type -> obj list (matches CustomBlocks output)
    RawMarkdown: string
    MarkdownAst: MarkdownDocument
}

/// Front matter parsing with proper error handling
/// Reuses existing YamlDotNet configuration for consistency
let private parseFrontMatter<'TMetadata> (content: string) : Result<'TMetadata option * string, ParseError> =
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
                    // Use same YamlDotNet configuration as existing system
                    let deserializer = 
                        DeserializerBuilder()
                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build()
                    
                    let metadata = deserializer.Deserialize<'TMetadata>(frontMatterYaml)
                    Ok (Some metadata, remainingContent)
                with
                | ex -> 
                    Error (YamlParseError $"Failed to parse YAML front-matter: {ex.Message}")
            | None ->
                Error (InvalidMarkdownStructure "Found opening --- but no closing --- for front-matter")
        else
            Ok (None, content)
    with
    | ex -> Error (YamlParseError $"Error processing front-matter: {ex.Message}")

/// Extract text content from AST, filtering out custom blocks
/// This replaces string manipulation with proper AST traversal
let private extractTextContentFromAst (doc: MarkdownDocument) : string =
    let writer = new StringWriter()
    let renderer = new Markdig.Renderers.HtmlRenderer(writer)
    
    // Add custom block renderers to ensure proper rendering
    renderer.ObjectRenderers.Add(MediaBlockHtmlRenderer())
    renderer.ObjectRenderers.Add(ReviewBlockHtmlRenderer())
    renderer.ObjectRenderers.Add(RsvpBlockHtmlRenderer())
    
    // Render all blocks including custom blocks
    for block in doc do
        renderer.Render(block) |> ignore
    
    writer.ToString()

/// Extract custom blocks from AST using our custom block types
let private extractCustomBlocks (doc: MarkdownDocument) : Map<string, obj list> =
    let blocks = System.Collections.Generic.Dictionary<string, obj list>()
    
    // Collect all custom blocks by type
    for descendant in doc.Descendants() do
        let block = descendant :> Block
        match block with
        | :? MediaBlock as mediaBlock ->
            let blockType = "media"
            if not (blocks.ContainsKey(blockType)) then
                blocks.[blockType] <- []
            blocks.[blockType] <- (mediaBlock.MediaItems |> List.toSeq |> Seq.cast<obj> |> Seq.toList) @ blocks.[blockType]
        | :? ReviewBlock as reviewBlock ->
            let blockType = "review"
            if not (blocks.ContainsKey(blockType)) then
                blocks.[blockType] <- []
            match reviewBlock.ReviewData with
            | Some data -> blocks.[blockType] <- (data :> obj) :: blocks.[blockType]
            | None -> ()
        | :? VenueBlock as venueBlock ->
            let blockType = "venue"
            if not (blocks.ContainsKey(blockType)) then
                blocks.[blockType] <- []
            match venueBlock.VenueData with
            | Some data -> blocks.[blockType] <- (data :> obj) :: blocks.[blockType]
            | None -> ()
        | :? RsvpBlock as rsvpBlock ->
            let blockType = "rsvp"
            if not (blocks.ContainsKey(blockType)) then
                blocks.[blockType] <- []
            match rsvpBlock.RsvpData with
            | Some data -> blocks.[blockType] <- (data :> obj) :: blocks.[blockType]
            | None -> ()
        | _ -> ()
    
    blocks |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq

/// Create markdown pipeline with extensions
/// Uses same configuration as existing MarkdownService for consistency
let private createMarkdownPipeline () : MarkdownPipeline =
    MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .UseTaskLists()
        .UseDiagrams()
        .UseMediaLinks()
        .UseMathematics()
        .UseEmojiAndSmiley()
        .UseEmphasisExtras()
        .UseBootstrap()
        .UseFigures()
        |> CustomBlocks.useCustomBlocks
        |> fun builder -> builder.Build()

/// Central document parsing function - single entry point for all content types
/// This replaces the various individual parse functions with unified processing
let parseDocumentFromAst<'TMetadata> (content: string) : Result<ParsedDocument<'TMetadata>, ParseError> =
    match parseFrontMatter<'TMetadata> content with
    | Error parseError -> Error parseError
    | Ok (metadata, contentWithoutFrontMatter) ->
        try
            let pipeline = createMarkdownPipeline()
            let doc = Markdown.Parse(contentWithoutFrontMatter, pipeline)
            let htmlContent = extractTextContentFromAst doc
            
            Ok {
                Metadata = metadata
                TextContent = htmlContent  // Use rendered HTML content
                CustomBlocks = extractCustomBlocks doc
                RawMarkdown = content
                MarkdownAst = doc
            }
        with
        | ex -> Error (MarkdownParseError $"Failed to parse markdown: {ex.Message}")

/// Parse document from file path with proper error handling
let parseDocumentFromFile<'TMetadata> (filePath: string) : Result<ParsedDocument<'TMetadata>, ParseError> =
    try
        if not (File.Exists(filePath)) then
            Error (FileNotFound filePath)
        else
            let content = File.ReadAllText(filePath)
            parseDocumentFromAst<'TMetadata> content
    with
    | ex -> Error (FileNotFound $"Error reading {filePath}: {ex.Message}")

/// Convenience functions for existing content types
/// These provide type-safe parsing for existing domain types

let parsePost (content: string) : Result<ParsedDocument<PostDetails>, ParseError> =
    parseDocumentFromAst<PostDetails> content

let parseSnippet (content: string) : Result<ParsedDocument<SnippetDetails>, ParseError> =
    parseDocumentFromAst<SnippetDetails> content

let parsePresentation (content: string) : Result<ParsedDocument<PresentationDetails>, ParseError> =
    parseDocumentFromAst<PresentationDetails> content

let parseWiki (content: string) : Result<ParsedDocument<WikiDetails>, ParseError> =
    parseDocumentFromAst<WikiDetails> content

let parseBook (content: string) : Result<ParsedDocument<BookDetails>, ParseError> =
    parseDocumentFromAst<BookDetails> content

let parseAlbum (content: string) : Result<ParsedDocument<AlbumDetails>, ParseError> =
    parseDocumentFromAst<AlbumDetails> content

let parseResponse (content: string) : Result<ParsedDocument<ResponseDetails>, ParseError> =
    parseDocumentFromAst<ResponseDetails> content

let parseBookmark (content: string) : Result<ParsedDocument<BookmarkDetails>, ParseError> =
    parseDocumentFromAst<BookmarkDetails> content

/// File-based parsing convenience functions
let parsePostFromFile (filePath: string) : Result<ParsedDocument<PostDetails>, ParseError> =
    parseDocumentFromFile<PostDetails> filePath

let parseSnippetFromFile (filePath: string) : Result<ParsedDocument<SnippetDetails>, ParseError> =
    parseDocumentFromFile<SnippetDetails> filePath

let parsePresentationFromFile (filePath: string) : Result<ParsedDocument<PresentationDetails>, ParseError> =
    parseDocumentFromFile<PresentationDetails> filePath

let parseWikiFromFile (filePath: string) : Result<ParsedDocument<WikiDetails>, ParseError> =
    parseDocumentFromFile<WikiDetails> filePath

let parseBookFromFile (filePath: string) : Result<ParsedDocument<BookDetails>, ParseError> =
    parseDocumentFromFile<BookDetails> filePath

let parseAlbumFromFile (filePath: string) : Result<ParsedDocument<AlbumDetails>, ParseError> =
    parseDocumentFromFile<AlbumDetails> filePath

let parseAlbumCollectionFromFile (filePath: string) : Result<ParsedDocument<AlbumCollectionDetails>, ParseError> =
    parseDocumentFromFile<AlbumCollectionDetails> filePath

let parsePlaylistCollectionFromFile (filePath: string) : Result<ParsedDocument<PlaylistDetails>, ParseError> =
    parseDocumentFromFile<PlaylistDetails> filePath

let parseResponseFromFile (filePath: string) : Result<ParsedDocument<ResponseDetails>, ParseError> =
    parseDocumentFromFile<ResponseDetails> filePath

let parseBookmarkFromFile (filePath: string) : Result<ParsedDocument<BookmarkDetails>, ParseError> =
    parseDocumentFromFile<BookmarkDetails> filePath

let parseRsvpFromFile (filePath: string) : Result<ParsedDocument<RsvpDetails>, ParseError> =
    parseDocumentFromFile<RsvpDetails> filePath

