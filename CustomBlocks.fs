module CustomBlocks

open System
open System.IO
open Markdig
open Markdig.Parsers
open Markdig.Syntax
open Markdig.Renderers
open Markdig.Renderers.Html
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Custom block types for IndieWeb content

/// Base interface for all custom blocks
type ICustomBlock =
    abstract member BlockType: string
    abstract member RawContent: string

/// Media block for :::media syntax
type MediaBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val MediaItems: MediaItem list = [] with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "media"
        member this.RawContent = this.RawContent

/// Review block for :::review syntax  
and ReviewBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val ReviewData: ReviewData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "review"
        member this.RawContent = this.RawContent

/// Venue block for :::venue syntax
and VenueBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val VenueData: VenueData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "venue"
        member this.RawContent = this.RawContent

/// RSVP block for :::rsvp syntax
and RsvpBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val RsvpData: RsvpData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "rsvp"
        member this.RawContent = this.RawContent

// Data types for structured content within blocks

/// Media item within :::media blocks
and [<CLIMutable>] MediaItem = {
    media_type: string
    uri: string
    alt_text: string
    caption: string
    aspect: string
}

/// Review data for :::review blocks  
and [<CLIMutable>] ReviewData = {
    item_title: string
    rating: float
    max_rating: float
    review_text: string
    item_url: string option
    review_date: string option
}

/// Venue data for :::venue blocks
and [<CLIMutable>] VenueData = {
    venue_name: string
    venue_address: string
    venue_city: string
    venue_country: string
    venue_url: string option
    latitude: float option
    longitude: float option
}

/// RSVP data for :::rsvp blocks
and [<CLIMutable>] RsvpData = {
    event_name: string
    event_url: string
    event_date: string
    rsvp_status: string  // "yes", "no", "maybe", "interested"
    event_location: string option
    notes: string option
}

// Block parsers implementing Markdig parser patterns

/// Base parser class for custom block parsing
type CustomBlockParser(blockType: string, createBlock: BlockParser -> ContainerBlock) =
    inherit BlockParser()
    
    let startMarker = $":::{blockType}"
    let endMarker = ":::"
    
    override this.TryOpen(processor) =
        let line = processor.Line.ToString().TrimStart()
        if line.Contains(":::") then
            printfn "TryOpen found fence line: [%s]" line
        if line.StartsWith(startMarker) then
            printfn "Start marker matched for %s" blockType
            let block = createBlock this
            processor.NewBlocks.Push(block)
            BlockState.ContinueDiscard
        else
            BlockState.None
    
    override _.TryContinue(processor, block) =
        let line = processor.Line
        let lineText = line.ToString().TrimStart()
        
        if lineText = endMarker then
            // End of custom block - parse accumulated content
            CustomBlockParser.parseBlockContent block
            processor.Close(block)
            BlockState.BreakDiscard
        elif lineText.StartsWith(":::") then
            // Different fence type, close this block
            processor.Close(block)
            BlockState.BreakDiscard
        else
            // Continue collecting content - preserve original indentation
            let originalLine = 
                // Try to preserve original indentation by accessing the full slice
                let slice = processor.Line
                if slice.Text <> null then
                    slice.Text.Substring(slice.Start, slice.Length)
                else
                    slice.ToString()
            match block with
            | :? MediaBlock as mediaBlock ->
                mediaBlock.RawContent <- mediaBlock.RawContent + originalLine + "\n"
            | :? ReviewBlock as reviewBlock ->
                reviewBlock.RawContent <- reviewBlock.RawContent + originalLine + "\n"
            | :? VenueBlock as venueBlock ->
                venueBlock.RawContent <- venueBlock.RawContent + originalLine + "\n"
            | :? RsvpBlock as rsvpBlock ->
                rsvpBlock.RawContent <- rsvpBlock.RawContent + originalLine + "\n"
            | _ -> ()
            BlockState.Continue
    
    override _.Close(processor, block) =
        CustomBlockParser.parseBlockContent block
        true
    
    /// Parse YAML content within custom blocks
    static member parseBlockContent (block: Block) =
        try
            let deserializer = 
                DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build()
            
            match block with
            | :? MediaBlock as mediaBlock ->
                if not (String.IsNullOrWhiteSpace(mediaBlock.RawContent)) then
                    // Fix indentation for YAML parsing (based on sample-script.fsx approach)
                    let lines = mediaBlock.RawContent.Split([|'\n'|], StringSplitOptions.None)
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
                    let mediaItems = deserializer.Deserialize<MediaItem list>(cleanContent)
                    mediaBlock.MediaItems <- mediaItems
            | :? ReviewBlock as reviewBlock ->
                if not (String.IsNullOrWhiteSpace(reviewBlock.RawContent)) then
                    let reviewData = deserializer.Deserialize<ReviewData>(reviewBlock.RawContent)
                    reviewBlock.ReviewData <- Some reviewData
            | :? VenueBlock as venueBlock ->
                if not (String.IsNullOrWhiteSpace(venueBlock.RawContent)) then
                    let venueData = deserializer.Deserialize<VenueData>(venueBlock.RawContent)
                    venueBlock.VenueData <- Some venueData
            | :? RsvpBlock as rsvpBlock ->
                if not (String.IsNullOrWhiteSpace(rsvpBlock.RawContent)) then
                    let rsvpData = deserializer.Deserialize<RsvpData>(rsvpBlock.RawContent)
                    rsvpBlock.RsvpData <- Some rsvpData
            | _ -> ()
        with
        | ex -> 
            // Log error but don't fail - graceful degradation
            printfn "Warning: Failed to parse custom block content: %s" ex.Message

// Specific parser implementations

/// Parser for :::media blocks
type MediaBlockParser() =
    inherit CustomBlockParser("media", fun parser -> MediaBlock(parser) :> ContainerBlock)

/// Parser for :::review blocks
type ReviewBlockParser() =
    inherit CustomBlockParser("review", fun parser -> ReviewBlock(parser) :> ContainerBlock)

/// Parser for :::venue blocks  
type VenueBlockParser() =
    inherit CustomBlockParser("venue", fun parser -> VenueBlock(parser) :> ContainerBlock)

/// Parser for :::rsvp blocks
type RsvpBlockParser() =
    inherit CustomBlockParser("rsvp", fun parser -> RsvpBlock(parser) :> ContainerBlock)

// HTML Renderers for custom blocks

/// HTML utility functions
module internal HtmlHelpers =
    let escapeHtml (text: string) =
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;")
    
    let attribute name value =
        sprintf """ %s="%s" """ name (escapeHtml value)
    
    let element tag attributes content =
        sprintf "<%s%s>%s</%s>" tag attributes content tag
    
    let selfClosingElement tag attributes =
        sprintf "<%s%s />" tag attributes

/// HTML renderer for MediaBlock
type MediaBlockHtmlRenderer() =
    inherit HtmlObjectRenderer<MediaBlock>()
    
    override _.Write(renderer: HtmlRenderer, block: MediaBlock) : unit =
        let renderMediaItem (item: MediaItem) =
            let mediaElement =
                match item.media_type.ToLower() with
                | "image" ->
                    HtmlHelpers.selfClosingElement "img" 
                        (HtmlHelpers.attribute "src" item.uri + 
                         HtmlHelpers.attribute "alt" item.alt_text +
                         HtmlHelpers.attribute "class" "media-image")
                | "video" ->
                    HtmlHelpers.element "video" 
                        (HtmlHelpers.attribute "src" item.uri + 
                         HtmlHelpers.attribute "controls" "controls" +
                         HtmlHelpers.attribute "class" "media-video")
                        item.alt_text
                | "audio" ->
                    HtmlHelpers.element "audio"
                        (HtmlHelpers.attribute "src" item.uri +
                         HtmlHelpers.attribute "controls" "controls" +
                         HtmlHelpers.attribute "class" "media-audio")
                        item.alt_text
                | _ ->
                    HtmlHelpers.element "a"
                        (HtmlHelpers.attribute "href" item.uri +
                         HtmlHelpers.attribute "class" "media-link")
                        item.alt_text
            
            let captionElement =
                if not (String.IsNullOrWhiteSpace(item.caption)) then
                    HtmlHelpers.element "figcaption" (HtmlHelpers.attribute "class" "media-caption") 
                        (HtmlHelpers.escapeHtml item.caption)
                else ""
            
            HtmlHelpers.element "figure" (HtmlHelpers.attribute "class" "custom-media-item")
                (mediaElement + captionElement)
        
        let renderedItems = 
            block.MediaItems 
            |> List.map renderMediaItem 
            |> String.concat "\n"
        
        let html = HtmlHelpers.element "div" (HtmlHelpers.attribute "class" "custom-media-block") renderedItems
        renderer.Write(html: string) |> ignore

/// HTML renderer for ReviewBlock  
type ReviewBlockHtmlRenderer() =
    inherit HtmlObjectRenderer<ReviewBlock>()
    
    override _.Write(renderer: HtmlRenderer, block: ReviewBlock) : unit =
        match block.ReviewData with
        | Some reviewData -> 
            let html = sprintf "<div class=\"review-block\"><!-- Review: %s --></div>" (HtmlHelpers.escapeHtml reviewData.item_title)
            renderer.Write(html: string) |> ignore
        | None -> 
            renderer.Write("<!-- Invalid review block -->" : string) |> ignore

/// HTML renderer for VenueBlock
type VenueBlockHtmlRenderer() =
    inherit HtmlObjectRenderer<VenueBlock>()
    
    override _.Write(renderer: HtmlRenderer, block: VenueBlock) : unit =
        match block.VenueData with
        | Some venueData -> 
            let html = sprintf "<div class=\"venue-block\"><!-- Venue: %s --></div>" (HtmlHelpers.escapeHtml venueData.venue_name)
            renderer.Write(html: string) |> ignore
        | None -> 
            renderer.Write("<!-- Invalid venue block -->" : string) |> ignore

/// HTML renderer for RsvpBlock
type RsvpBlockHtmlRenderer() =
    inherit HtmlObjectRenderer<RsvpBlock>()
    
    override _.Write(renderer: HtmlRenderer, block: RsvpBlock) : unit =
        match block.RsvpData with
        | Some rsvpData -> 
            let html = sprintf "<div class=\"rsvp-block\"><!-- RSVP: %s --></div>" (HtmlHelpers.escapeHtml rsvpData.event_name)
            renderer.Write(html: string) |> ignore
        | None -> 
            renderer.Write("<!-- Invalid RSVP block -->" : string) |> ignore

// Extension utilities

/// Union type for all custom blocks
type CustomBlock =
    | Media of MediaItem list
    | Review of ReviewData
    | Venue of VenueData
    | Rsvp of RsvpData

module CustomBlocks =
    /// Extract custom blocks from a Markdig AST
    let extractCustomBlocks (document: MarkdownDocument) : CustomBlock list =
        let extractBlocks (blocks: Block seq) =
            blocks
            |> Seq.choose (fun block ->
                match block with
                | :? MediaBlock as mediaBlock ->
                    if not (List.isEmpty mediaBlock.MediaItems) then
                        Some (Media mediaBlock.MediaItems)
                    else None
                | :? ReviewBlock as reviewBlock ->
                    match reviewBlock.ReviewData with
                    | Some data -> Some (Review data)
                    | None -> None
                | :? VenueBlock as venueBlock ->
                    match venueBlock.VenueData with
                    | Some data -> Some (Venue data)
                    | None -> None
                | :? RsvpBlock as rsvpBlock ->
                    match rsvpBlock.RsvpData with
                    | Some data -> Some (Rsvp data)
                    | None -> None
                | _ -> None)
            |> List.ofSeq
        
        extractBlocks document
    
    /// Filter out custom blocks from a Markdig AST for content processing
    let filterCustomBlocks (document: MarkdownDocument) : MarkdownDocument =
        let filteredBlocks = 
            document
            |> Seq.filter (fun block ->
                not (block :? MediaBlock || block :? ReviewBlock || 
                     block :? VenueBlock || block :? RsvpBlock))
        
        let newDocument = MarkdownDocument()
        for block in filteredBlocks do
            newDocument.Add(block)
        
        newDocument

/// Extract all custom blocks from a MarkdownDocument
let extractCustomBlocks (doc: MarkdownDocument) : Map<string, obj list> =
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

/// Filter out custom blocks when extracting text content
let filterCustomBlocks (doc: MarkdownDocument) : MarkdownDocument =
    // Create a copy of the document without custom blocks
    let filteredDoc = MarkdownDocument()
    
    for block in doc do
        match block with
        | :? MediaBlock -> () // Skip media blocks
        | :? ReviewBlock -> () // Skip review blocks
        | :? VenueBlock -> () // Skip venue blocks
        | :? RsvpBlock -> () // Skip rsvp blocks
        | _ -> 
            // Remove block from parent before adding to filtered doc
            if block.Parent <> null then
                block.Parent.Remove(block) |> ignore
            filteredDoc.Add(block)
    
    filteredDoc

// Markdig extension for registering custom block parsers

/// Extension class for registering all custom block parsers
type CustomBlockExtension() =
    interface IMarkdownExtension with
        member _.Setup(pipeline) =
            // Register all custom block parsers
            // Insert at position 0 for priority parsing
            pipeline.BlockParsers.Insert(0, MediaBlockParser())
            pipeline.BlockParsers.Insert(0, ReviewBlockParser())
            pipeline.BlockParsers.Insert(0, VenueBlockParser())
            pipeline.BlockParsers.Insert(0, RsvpBlockParser())
        
        member _.Setup(pipeline, renderer) =
            // Register HTML renderers for custom blocks
            match renderer with
            | :? Markdig.Renderers.HtmlRenderer as htmlRenderer ->
                htmlRenderer.ObjectRenderers.Add(MediaBlockHtmlRenderer())
                htmlRenderer.ObjectRenderers.Add(ReviewBlockHtmlRenderer())
                htmlRenderer.ObjectRenderers.Add(VenueBlockHtmlRenderer())
                htmlRenderer.ObjectRenderers.Add(RsvpBlockHtmlRenderer())
            | _ -> ()
            ()

/// Helper function to add custom block extension to a pipeline builder
let useCustomBlocks (pipelineBuilder: MarkdownPipelineBuilder) =
    pipelineBuilder.Use<CustomBlockExtension>()

/// Parse custom blocks using provided block parsers (Phase 1C specification)
let parseCustomBlocks (blockParsers: Map<string, string -> obj list>) (doc: MarkdownDocument) : Map<string, obj list> =
    let results = System.Collections.Generic.Dictionary<string, obj list>()
    
    // Initialize result dictionary with empty lists for all parser types
    for (blockType, _) in blockParsers |> Map.toSeq do
        results.[blockType] <- []
    
    // Process all custom blocks in the document
    for descendant in doc.Descendants() do
        let block = descendant :> Block
        match block with
        | :? MediaBlock as mediaBlock ->
            let blockType = "media"
            if blockParsers.ContainsKey(blockType) then
                let parser = blockParsers.[blockType]
                let parsedObjects = parser mediaBlock.RawContent
                if results.ContainsKey(blockType) then
                    results.[blockType] <- results.[blockType] @ parsedObjects
                else
                    results.[blockType] <- parsedObjects
        | :? ReviewBlock as reviewBlock ->
            let blockType = "review"
            if blockParsers.ContainsKey(blockType) then
                let parser = blockParsers.[blockType]
                let parsedObjects = parser reviewBlock.RawContent
                if results.ContainsKey(blockType) then
                    results.[blockType] <- results.[blockType] @ parsedObjects
                else
                    results.[blockType] <- parsedObjects
        | :? VenueBlock as venueBlock ->
            let blockType = "venue"
            if blockParsers.ContainsKey(blockType) then
                let parser = blockParsers.[blockType]
                let parsedObjects = parser venueBlock.RawContent
                if results.ContainsKey(blockType) then
                    results.[blockType] <- results.[blockType] @ parsedObjects
                else
                    results.[blockType] <- parsedObjects
        | :? RsvpBlock as rsvpBlock ->
            let blockType = "rsvp"
            if blockParsers.ContainsKey(blockType) then
                let parser = blockParsers.[blockType]
                let parsedObjects = parser rsvpBlock.RawContent
                if results.ContainsKey(blockType) then
                    results.[blockType] <- results.[blockType] @ parsedObjects
                else
                    results.[blockType] <- parsedObjects
        | _ -> ()
    
    results |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq
