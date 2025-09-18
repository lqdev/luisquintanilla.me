
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

// Data types for structured content within blocks
[<CLIMutable>]
type MediaItem = {
    [<YamlDotNet.Serialization.YamlMember(Alias="mediaType")>]
    media_type: string
    [<YamlDotNet.Serialization.YamlMember(Alias="url")>]
    uri: string
    [<YamlDotNet.Serialization.YamlMember(Alias="alt")>]
    alt_text: string
    [<YamlDotNet.Serialization.YamlMember(Alias="caption")>]
    caption: string
    [<YamlDotNet.Serialization.YamlMember(Alias="aspectRatio")>]
    aspect: string
}

[<CLIMutable>]
type ReviewData = {
    // Enhanced fields for comprehensive review support
    [<YamlDotNet.Serialization.YamlMember(Alias="title")>]
    title: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="item")>]
    item: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="itemType")>]
    item_type: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="rating")>]
    rating: float
    [<YamlDotNet.Serialization.YamlMember(Alias="scale")>]
    scale: float option
    [<YamlDotNet.Serialization.YamlMember(Alias="summary")>]
    summary: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="pros")>]
    pros: string array option
    [<YamlDotNet.Serialization.YamlMember(Alias="cons")>]
    cons: string array option
    [<YamlDotNet.Serialization.YamlMember(Alias="additionalFields")>]
    additional_fields: System.Collections.Generic.Dictionary<string, obj> option
    
    // Legacy fields for backward compatibility
    [<YamlDotNet.Serialization.YamlMember(Alias="item_title")>]
    item_title: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="max_rating")>]
    max_rating: float option
    [<YamlDotNet.Serialization.YamlMember(Alias="review_text")>]
    review_text: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="item_url")>]
    item_url: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="review_date")>]
    review_date: string option
}
with
    // Helper properties to get values with fallbacks
    member this.GetTitle() = 
        this.title |> Option.orElse this.item_title |> Option.defaultValue ""
    member this.GetItem() = 
        this.item |> Option.orElse this.item_title |> Option.defaultValue ""
    member this.GetItemType() = 
        this.item_type |> Option.defaultValue "unknown"
    member this.GetScale() = 
        this.scale |> Option.orElse this.max_rating |> Option.defaultValue 5.0
    member this.GetSummary() = 
        this.summary |> Option.orElse this.review_text |> Option.defaultValue ""

[<CLIMutable>]
type VenueData = {
    venue_name: string
    venue_address: string
    venue_city: string
    venue_country: string
    venue_url: string option
    latitude: float option
    longitude: float option
}

[<CLIMutable>]
type RsvpData = {
    event_name: string
    event_url: string
    event_date: string
    rsvp_status: string  // "yes", "no", "maybe", "interested"
    event_location: string option
    notes: string option
}

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
and ReviewBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val ReviewData: ReviewData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "review"
        member this.RawContent = this.RawContent
and VenueBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val VenueData: VenueData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "venue"
        member this.RawContent = this.RawContent
and RsvpBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val RsvpData: RsvpData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "rsvp"
        member this.RawContent = this.RawContent

// Block parsers implementing Markdig parser patterns

/// Base parser class for custom block parsing
type CustomBlockParser(blockType: string, createBlock: BlockParser -> ContainerBlock) =
    inherit BlockParser()
    
    let startMarker = $":::{blockType}"
    let endMarker = ":::"
    
    override this.TryOpen(processor) =
        let line = processor.Line.ToString().TrimStart()
        if line.StartsWith(startMarker) then
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
            // Defensive: provide default values for all fields to avoid null refs
            let mediaType = if isNull item.media_type then "image" else item.media_type
            let uri = if isNull item.uri then "" else item.uri
            let alt = if isNull item.alt_text then "" else item.alt_text
            let caption = if isNull item.caption then "" else item.caption
            let aspect = if isNull item.aspect then "" else item.aspect

            let mediaElement =
                match mediaType.ToLower() with
                | "image" ->
                    HtmlHelpers.selfClosingElement "img" 
                        (HtmlHelpers.attribute "src" uri + 
                         HtmlHelpers.attribute "alt" alt +
                         HtmlHelpers.attribute "class" "media-image")
                | "video" ->
                    HtmlHelpers.element "video" 
                        (HtmlHelpers.attribute "src" uri + 
                         HtmlHelpers.attribute "controls" "controls" +
                         HtmlHelpers.attribute "class" "media-video")
                        alt
                | "audio" ->
                    HtmlHelpers.element "audio"
                        (HtmlHelpers.attribute "src" uri +
                         HtmlHelpers.attribute "controls" "controls" +
                         HtmlHelpers.attribute "class" "media-audio")
                        alt
                | _ ->
                    HtmlHelpers.element "a"
                        (HtmlHelpers.attribute "href" uri +
                         HtmlHelpers.attribute "class" "media-link")
                        alt

            let captionElement =
                if not (String.IsNullOrWhiteSpace(caption)) then
                    HtmlHelpers.element "figcaption" (HtmlHelpers.attribute "class" "media-caption") 
                        (HtmlHelpers.escapeHtml caption)
                else ""

            HtmlHelpers.element "figure" (HtmlHelpers.attribute "class" "custom-media-item")
                (mediaElement + captionElement)

        let renderedItems = 
            block.MediaItems 
            |> List.map renderMediaItem 
            |> String.concat "\n"
        let html = HtmlHelpers.element "div" (HtmlHelpers.attribute "class" "custom-media-block") renderedItems
        renderer.Write(html: string) |> ignore

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
            // Add parsers in reverse order since they're inserted at index 0
            pipeline.BlockParsers.Insert(0, MediaBlockParser())
            pipeline.BlockParsers.Insert(0, ReviewBlockParser())
            pipeline.BlockParsers.Insert(0, VenueBlockParser())
            pipeline.BlockParsers.Insert(0, RsvpBlockParser())
            
        member _.Setup(pipeline, renderer) =
            // Add renderers only for HTML renderer
            match renderer with
            | :? HtmlRenderer as htmlRenderer ->
                htmlRenderer.ObjectRenderers.Add(MediaBlockHtmlRenderer())
                // Add other renderers when implemented
                // htmlRenderer.ObjectRenderers.Add(ReviewBlockHtmlRenderer())
                // htmlRenderer.ObjectRenderers.Add(VenueBlockHtmlRenderer())
                // htmlRenderer.ObjectRenderers.Add(RsvpBlockHtmlRenderer())
            | _ -> 
                // Other renderers (like text, normalize) don't need custom renderers
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
