
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
    // Core review fields
    [<YamlDotNet.Serialization.YamlMember(Alias="item")>]
    item: string  // Name of the item being reviewed (e.g., "The Four Agreements", "Blade Runner 2049")
    [<YamlDotNet.Serialization.YamlMember(Alias="itemType")>]
    item_type: string option  // Type of review: "book", "movie", "music", "business", "product"
    [<YamlDotNet.Serialization.YamlMember(Alias="rating")>]
    rating: float
    [<YamlDotNet.Serialization.YamlMember(Alias="scale")>]
    scale: float option  // Rating scale (defaults to 5.0)
    [<YamlDotNet.Serialization.YamlMember(Alias="summary")>]
    summary: string option  // Brief review summary
    
    // Optional structured feedback
    [<YamlDotNet.Serialization.YamlMember(Alias="pros")>]
    pros: string array option
    [<YamlDotNet.Serialization.YamlMember(Alias="cons")>]
    cons: string array option
    
    // Optional metadata and links
    [<YamlDotNet.Serialization.YamlMember(Alias="itemUrl")>]
    item_url: string option  // Link to the item's website or URL for reference
    [<YamlDotNet.Serialization.YamlMember(Alias="imageUrl")>]
    image_url: string option  // Thumbnail/cover image URL for display
    [<YamlDotNet.Serialization.YamlMember(Alias="additionalFields")>]
    additional_fields: System.Collections.Generic.Dictionary<string, obj> option  // Type-specific metadata
}
with
    // Helper methods for clean API
    member this.GetItemType() = 
        this.item_type |> Option.defaultValue "unknown"
    member this.GetScale() = 
        this.scale |> Option.defaultValue 5.0
    member this.GetSummary() = 
        this.summary |> Option.defaultValue ""

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
                    // Fix indentation for YAML parsing (same as MediaBlock approach)
                    let lines = reviewBlock.RawContent.Split([|'\n'|], StringSplitOptions.None)
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
                    let reviewData = deserializer.Deserialize<ReviewData>(cleanContent)
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

/// HTML renderer for ReviewBlock
type ReviewBlockHtmlRenderer() =
    inherit HtmlObjectRenderer<ReviewBlock>()
    
    override _.Write(renderer: HtmlRenderer, block: ReviewBlock) : unit =
        match block.ReviewData with
        | Some reviewData ->
            // Enhanced HTML rendering with proper structure
            let itemType = reviewData.GetItemType()
            let scale = reviewData.GetScale()
            let summary = reviewData.GetSummary()
            
            // Start review block container
            renderer.Write("<div class=\"custom-review-block h-review\">") |> ignore
            
            // Item title with type badge
            renderer.Write($"<div class=\"review-header\">") |> ignore
            renderer.Write($"<h3 class=\"review-title p-name\">{HtmlHelpers.escapeHtml reviewData.item}</h3>") |> ignore
            if itemType <> "unknown" then
                renderer.Write($"<span class=\"item-type-badge badge bg-secondary\">{HtmlHelpers.escapeHtml (itemType.ToUpperInvariant())}</span>") |> ignore
            renderer.Write("</div>") |> ignore
            
            // Image if available
            match reviewData.image_url with
            | Some imageUrl when not (String.IsNullOrWhiteSpace(imageUrl)) ->
                renderer.Write($"<div class=\"review-image\"><img src=\"{HtmlHelpers.escapeHtml imageUrl}\" alt=\"{HtmlHelpers.escapeHtml reviewData.item}\" class=\"review-thumbnail img-fluid\" /></div>") |> ignore
            | _ -> ()
            
            // Rating display
            if reviewData.rating > 0.0 then
                let stars = String.replicate (int reviewData.rating) "★" + String.replicate (int (scale - reviewData.rating)) "☆"
                renderer.Write($"<div class=\"review-rating p-rating\"><strong>Rating:</strong> {stars} ({reviewData.rating:F1}/{scale:F1})</div>") |> ignore
            
            // Summary
            if not (String.IsNullOrWhiteSpace(summary)) then
                renderer.Write($"<div class=\"review-summary p-summary\">{HtmlHelpers.escapeHtml summary}</div>") |> ignore
            
            // Pros and cons
            match reviewData.pros with
            | Some prosArray when prosArray.Length > 0 ->
                renderer.Write("<div class=\"review-pros\"><h4>Pros:</h4><ul>") |> ignore
                for pro in prosArray do
                    renderer.Write($"<li>{HtmlHelpers.escapeHtml pro}</li>") |> ignore
                renderer.Write("</ul></div>") |> ignore
            | _ -> ()
            
            match reviewData.cons with
            | Some consArray when consArray.Length > 0 ->
                renderer.Write("<div class=\"review-cons\"><h4>Cons:</h4><ul>") |> ignore
                for con in consArray do
                    renderer.Write($"<li>{HtmlHelpers.escapeHtml con}</li>") |> ignore
                renderer.Write("</ul></div>") |> ignore
            | _ -> ()
            
            // Additional fields
            match reviewData.additional_fields with
            | Some fields when fields.Count > 0 ->
                renderer.Write("<div class=\"review-additional-fields\"><h4>Additional Information:</h4>") |> ignore
                for kvp in fields do
                    renderer.Write($"<div class=\"additional-field\"><strong>{HtmlHelpers.escapeHtml kvp.Key}:</strong> {HtmlHelpers.escapeHtml (kvp.Value.ToString())}</div>") |> ignore
                renderer.Write("</div>") |> ignore
            | _ -> ()
            
            // Item URL
            match reviewData.item_url with
            | Some url when not (String.IsNullOrWhiteSpace(url)) ->
                renderer.Write($"<div class=\"review-url\"><a href=\"{HtmlHelpers.escapeHtml url}\" class=\"u-url\" target=\"_blank\">View Item</a></div>") |> ignore
            | _ -> ()
            
            // Close review block container
            renderer.Write("</div>") |> ignore
        | None ->
            renderer.Write("<div class=\"review-block-empty\"></div>") |> ignore

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
                htmlRenderer.ObjectRenderers.Add(ReviewBlockHtmlRenderer())
                // Add other renderers when implemented
                // htmlRenderer.ObjectRenderers.Add(VenueBlockHtmlRenderer())
                // htmlRenderer.ObjectRenderers.Add(RsvpBlockHtmlRenderer())
            | _ -> 
                // Other renderers (like text, normalize) don't need custom renderers
                ()

/// Helper function to add custom block extension to a pipeline builder
let useCustomBlocks (pipelineBuilder: MarkdownPipelineBuilder) =
    pipelineBuilder.Use<CustomBlockExtension>()

// Helper function to extract image URL from review blocks in content
let extractReviewImageUrl (content: string) : string option =
    try
        let pipeline = 
            MarkdownPipelineBuilder()
                |> useCustomBlocks
                |> fun builder -> builder.Build()
        let document = Markdown.Parse(content, pipeline)
        let customBlocks = extractCustomBlocks document
        
        match customBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData -> reviewData.image_url
            | _ -> None
        | _ -> None
    with
    | _ -> None

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

// =====================================================================
// Resume Custom Blocks
// =====================================================================

/// Block for :::experience syntax
type ExperienceBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val Role = "" with get, set
    member val Company = "" with get, set
    member val Start = "" with get, set
    member val End : string option = None with get, set
    member val Content = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "experience"
        member this.RawContent = this.Content

/// Block for :::project syntax
type ProjectBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val Title = "" with get, set
    member val Url : string option = None with get, set
    member val Tech : string option = None with get, set
    member val Content = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "project"
        member this.RawContent = this.Content

/// Block for :::skills syntax
type SkillsBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val Category = "" with get, set
    member val Content = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "skills"
        member this.RawContent = this.Content

/// Block for :::testimonial syntax
type TestimonialBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val Author = "" with get, set
    member val Content = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "testimonial"
        member this.RawContent = this.Content

/// Block for :::education syntax
type EducationBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val Degree = "" with get, set
    member val Institution = "" with get, set
    member val Year : string option = None with get, set
    member val Content = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "education"
        member this.RawContent = this.Content

/// Parser for resume blocks with field: value and markdown content pattern
type ResumeBlockParser<'T when 'T :> ContainerBlock and 'T :> ICustomBlock>(blockType: string, createBlock: BlockParser -> 'T, setField: 'T -> string -> string -> unit, setContent: 'T -> string -> unit) =
    inherit BlockParser()
    
    let startMarker = $":::{blockType}"
    let endMarker = ":::"
    let contentSeparator = "---"
    
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
            processor.Close(block)
            BlockState.BreakDiscard
        else
            let block' = block :?> 'T
            
            // Check if this is the content separator
            if lineText = contentSeparator then
                // Mark that we're now in content mode (we'll track this via Content being non-empty initially)
                if block'.RawContent = "" then
                    setContent block' " "  // Use space as marker that we're in content mode
                BlockState.Continue
            // If we have content marker and this isn't separator, it's content
            elif block'.RawContent <> "" then
                let currentContent = block'.RawContent
                let newContent = if currentContent = " " then lineText else currentContent + "\n" + lineText
                setContent block' newContent
                BlockState.Continue
            // Otherwise, it's a field definition
            elif lineText.Contains(":") then
                let parts = lineText.Split([|':'|], 2)
                if parts.Length = 2 then
                    let fieldName = parts.[0].Trim()
                    let fieldValue = parts.[1].Trim()
                    setField block' fieldName fieldValue
                BlockState.Continue
            else
                BlockState.Continue
    
    override _.Close(processor, block) = true

/// Specific parsers for each resume block type

type ExperienceBlockParser() =
    inherit ResumeBlockParser<ExperienceBlock>(
        "experience",
        (fun parser -> ExperienceBlock(parser)),
        (fun block field value ->
            match field.ToLower() with
            | "role" -> block.Role <- value
            | "company" -> block.Company <- value
            | "start" -> block.Start <- value
            | "end" -> block.End <- Some value
            | _ -> ()),
        (fun block content -> block.Content <- content)
    )

type ProjectBlockParser() =
    inherit ResumeBlockParser<ProjectBlock>(
        "project",
        (fun parser -> ProjectBlock(parser)),
        (fun block field value ->
            match field.ToLower() with
            | "title" -> block.Title <- value
            | "url" -> block.Url <- Some value
            | "tech" -> block.Tech <- Some value
            | _ -> ()),
        (fun block content -> block.Content <- content)
    )

type SkillsBlockParser() =
    inherit ResumeBlockParser<SkillsBlock>(
        "skills",
        (fun parser -> SkillsBlock(parser)),
        (fun block field value ->
            match field.ToLower() with
            | "category" -> block.Category <- value
            | _ -> ()),
        (fun block content -> block.Content <- content)
    )

type TestimonialBlockParser() =
    inherit ResumeBlockParser<TestimonialBlock>(
        "testimonial",
        (fun parser -> TestimonialBlock(parser)),
        (fun block field value ->
            match field.ToLower() with
            | "author" -> block.Author <- value
            | _ -> ()),
        (fun block content -> block.Content <- content)
    )

type EducationBlockParser() =
    inherit ResumeBlockParser<EducationBlock>(
        "education",
        (fun parser -> EducationBlock(parser)),
        (fun block field value ->
            match field.ToLower() with
            | "degree" -> block.Degree <- value
            | "institution" -> block.Institution <- value
            | "year" -> block.Year <- Some value
            | _ -> ()),
        (fun block content -> block.Content <- content)
    )

// Extension for Markdig pipeline
type ResumeBlockExtension() =
    interface IMarkdownExtension with
        member _.Setup(pipeline: MarkdownPipelineBuilder) =
            // Insert parsers at index 0 for proper priority (same pattern as other custom blocks)
            // Note: Inserting at index 0 means later insertions appear first in the parser list,
            // so we insert in reverse order of desired evaluation priority
            if not (pipeline.BlockParsers.Contains<EducationBlockParser>()) then
                pipeline.BlockParsers.Insert(0, EducationBlockParser())
            if not (pipeline.BlockParsers.Contains<TestimonialBlockParser>()) then
                pipeline.BlockParsers.Insert(0, TestimonialBlockParser())
            if not (pipeline.BlockParsers.Contains<SkillsBlockParser>()) then
                pipeline.BlockParsers.Insert(0, SkillsBlockParser())
            if not (pipeline.BlockParsers.Contains<ProjectBlockParser>()) then
                pipeline.BlockParsers.Insert(0, ProjectBlockParser())
            if not (pipeline.BlockParsers.Contains<ExperienceBlockParser>()) then
                pipeline.BlockParsers.Insert(0, ExperienceBlockParser())
        member _.Setup(_pipeline, _renderer) = ()
