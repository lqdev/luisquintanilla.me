module ASTParsing

open System
open System.IO
open Markdig
open Markdig.Syntax
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Import existing domain types
open Domain

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
    CustomBlocks: Map<string, string list>  // Block type -> content list
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
    
    // Render all blocks except custom block types
    // Note: Custom blocks will be handled separately in CustomBlocks.fs
    for block in doc do
        // For now, render all blocks - custom block filtering will be added
        // when CustomBlocks.fs is implemented
        renderer.Render(block) |> ignore
    
    writer.ToString()

/// Extract custom blocks from AST (placeholder for CustomBlocks.fs integration)
/// This will be enhanced when CustomBlocks.fs module is implemented
let private extractCustomBlocks (doc: MarkdownDocument) : Map<string, string list> =
    // Placeholder implementation - will be replaced with actual custom block extraction
    // when CustomBlocks.fs is created in next step
    Map.empty

/// Create markdown pipeline with extensions
/// Uses same configuration as existing MarkdownService for consistency
let private createMarkdownPipeline () : MarkdownPipeline =
    // Start with same extensions as existing system
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()  // Includes PipeTables, TaskLists, etc.
        .UseDiagrams()
        .UseMediaLinks()
        .UseMathematics()
        .UseCustomContainers()
        .UseBootstrap()
        .UseFigures()
        // Custom block extensions will be added here when CustomBlocks.fs is ready
        .Build()

/// Central document parsing function - single entry point for all content types
/// This replaces the various individual parse functions with unified processing
let parseDocumentFromAst<'TMetadata> (content: string) : Result<ParsedDocument<'TMetadata>, ParseError> =
    match parseFrontMatter<'TMetadata> content with
    | Error parseError -> Error parseError
    | Ok (metadata, contentWithoutFrontMatter) ->
        try
            let pipeline = createMarkdownPipeline()
            let doc = Markdown.Parse(contentWithoutFrontMatter, pipeline)
            
            Ok {
                Metadata = metadata
                TextContent = extractTextContentFromAst doc
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

let parseResponseFromFile (filePath: string) : Result<ParsedDocument<ResponseDetails>, ParseError> =
    parseDocumentFromFile<ResponseDetails> filePath
