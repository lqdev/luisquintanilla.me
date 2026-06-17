module BookProcessor

    open Domain
    open ASTParsing
    open CustomBlocks
    open BlockRenderers
    open TagService
    open MarkdownService
    open ReadingTimeService
    open System.Xml.Linq
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Nodes
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Markdig
    open Markdig.Syntax
    open GenericBuilder

    // Cache for review data extracted during parsing
    // Phase 5C: Enhanced to store full ReviewMetadata for ActivityPub Schema.org integration
    let private reviewDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, ReviewMetadata option>()
    
    // Helper function to extract rating from custom review blocks using regex (for backward compatibility)
    let private extractRatingFromContent (content: string) : float option =
        try
            // Use regex to find rating in :::review blocks
            let reviewBlockPattern = @":::review\s*\n(.*?)\n:::"
            let ratingPattern = @"rating:\s*([\d.]+)"
            
            let reviewMatches = System.Text.RegularExpressions.Regex.Matches(content, reviewBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline)
            
            if reviewMatches.Count > 0 then
                let reviewContent = reviewMatches.[0].Groups.[1].Value
                let ratingMatch = System.Text.RegularExpressions.Regex.Match(reviewContent, ratingPattern)
                
                if ratingMatch.Success then
                    match System.Double.TryParse(ratingMatch.Groups.[1].Value) with
                    | (true, rating) -> Some rating
                    | _ -> None
                else None
            else None
        with
        | _ -> None

    let create() : ContentProcessor<Book> = {
        Parse = fun filePath ->
            match parseBookFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    // Try to extract review data from parsed custom blocks (new approach)
                    let reviewDataOpt = 
                        match parsedDoc.CustomBlocks.TryGetValue("review") with
                        | true, reviewList when reviewList.Length > 0 ->
                            match reviewList.[0] with
                            | :? CustomBlocks.ReviewData as reviewData -> Some reviewData
                            | _ -> None
                        | _ -> None
                    
                    // Populate metadata from review block if available, otherwise use frontmatter
                    let finalMetadata = 
                        match reviewDataOpt with
                        | Some reviewData ->
                            // Use review block as source of truth
                            // Note: Keep existing title from frontmatter to maintain consistency with views
                            // that expect "Book Title Review" format rather than just "Book Title"
                            { metadata with
                                Author = reviewData.GetAuthor()
                                Isbn = reviewData.GetIsbn()
                                Cover = reviewData.GetCover()
                                Rating = reviewData.Rating
                                DatePublished = reviewData.GetDatePublished()
                            }
                        | None ->
                            // Fallback to frontmatter (backward compatibility)
                            metadata
                    
                    // Phase 5C: Store full review metadata in cache for ActivityPub Schema.org integration
                    let reviewMetadata : ReviewMetadata option =
                        match reviewDataOpt with
                        | Some reviewData ->
                            Some {
                                ItemName = reviewData.Item  // The actual item name (book title)
                                ItemType = reviewData.ItemType  // "book", "movie", etc.
                                Rating = reviewData.Rating
                                Scale = reviewData.Scale
                                Summary = if String.IsNullOrWhiteSpace(reviewData.Summary) then None else Some reviewData.Summary
                                ItemUrl = reviewData.ItemUrl
                                ImageUrl = reviewData.ImageUrl
                                Author = Some (reviewData.GetAuthor())  // For books
                                Isbn = let isbn = reviewData.GetIsbn() in if String.IsNullOrWhiteSpace(isbn) then None else Some isbn
                            }
                        | None ->
                            // Fallback to frontmatter data (backward compatibility)
                            let (reviewImageUrl, reviewRating, reviewScale, reviewItemType) = ReviewDataExtractor.extractReviewData parsedDoc.RawMarkdown
                            if reviewRating > 0.0 then
                                Some {
                                    ItemName = metadata.Title  // Use frontmatter title as fallback
                                    ItemType = reviewItemType |> Option.defaultValue "book"
                                    Rating = reviewRating
                                    Scale = reviewScale
                                    Summary = None
                                    ItemUrl = None
                                    ImageUrl = reviewImageUrl
                                    Author = if String.IsNullOrWhiteSpace(metadata.Author) then None else Some metadata.Author
                                    Isbn = if String.IsNullOrWhiteSpace(metadata.Isbn) then None else Some metadata.Isbn
                                }
                            else None
                    
                    reviewDataCache.[fileName] <- reviewMetadata
                    
                    Ok {
                        FileName = fileName
                        Metadata = finalMetadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun book ->
            // For now, return content as-is. Later integrate with existing Views.Generator
            let viewNode = article [] [ rawText book.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun book ->
            sprintf "reviews/%s.html" book.FileName
        
        RenderCard = fun book ->
            let title = Html.escapeHtml book.Metadata.Title
            let url = sprintf "/reviews/%s/" book.FileName
            
            // Get review data from cache (extracted during parsing)
            // Phase 5C: Cache now stores full ReviewMetadata
            let reviewMetadataOpt = 
                match reviewDataCache.TryGetValue(book.FileName) with
                | (true, data) -> data
                | _ -> None
            
            // Extract values from ReviewMetadata with fallbacks
            let (reviewImageUrlOpt, reviewRating, reviewScale, reviewItemType) =
                match reviewMetadataOpt with
                | Some rm -> (rm.ImageUrl, rm.Rating, rm.Scale, Some rm.ItemType)
                | None -> (None, book.Metadata.Rating, 5.0, None)
            
            // Determine image URL with proper fallbacks
            let imageUrl = 
                match reviewImageUrlOpt with
                | Some reviewImageUrl -> reviewImageUrl
                | None -> 
                    if String.IsNullOrEmpty(book.Metadata.Cover) then
                        "/assets/img/book-placeholder.png"
                    else
                        book.Metadata.Cover
            
            // Use review rating if available, otherwise use metadata rating
            let (ratingValue, ratingScaleValue) = 
                if reviewRating > 0.0 then (reviewRating, reviewScale)
                else (book.Metadata.Rating, 5.0)
            
            // Display rating with SVG stars + numeric value using shared helper
            let ratingHtml = 
                if ratingValue > 0.0 then
                    sprintf "<div class=\"rating\">%s</div>" (BlockRenderers.StarRating.render ratingValue ratingScaleValue)
                else ""
            
            // Note: Item type badge is shown in the timeline card header (not duplicated here)
            // The header badge is extracted from reviewItemType by timelineHomeView/timelineHomeViewStratified
            
            // Create simplified timeline card with only: image and rating (no duplicate badge, title, or author)
            let coverHtml = 
                if not (String.IsNullOrEmpty(imageUrl)) then
                    sprintf "<img src=\"%s\" alt=\"%s cover\" class=\"review-image img-fluid\">" 
                        (Html.escapeHtml imageUrl) (Html.escapeHtml book.Metadata.Title)
                else ""
            
            // Hidden span to carry review item type for header badge extraction (not displayed)
            let hiddenItemTypeHtml = 
                match reviewItemType with
                | Some itemType when not (String.IsNullOrWhiteSpace(itemType)) ->
                    sprintf "<span class=\"review-item-type\" style=\"display:none\" data-item-type=\"%s\"></span>" (Html.escapeHtml itemType)
                | _ -> ""
            
            // Simple content div without duplicate badge or title
            sprintf "<div class=\"review-timeline-card\">%s%s%s</div>" hiddenItemTypeHtml coverHtml ratingHtml
        
        RenderRss = fun book ->
            // Create RSS item for book
            let url = sprintf "https://www.lqdev.me/reviews/%s" book.FileName
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", sprintf "%s by %s" book.Metadata.Title book.Metadata.Author),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" (normalizeUrlsForRss book.Content "https://www.lqdev.me")),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if date exists
            if not (String.IsNullOrEmpty(book.Metadata.DatePublished)) then
                item.Add(XElement(XName.Get "pubDate", book.Metadata.DatePublished))
            
            // Add source:markdown if available
            match generateSourceMarkdown book.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
    
    /// Phase 5C: Public accessor for review metadata cache (used by UnifiedFeeds.convertBooksToUnified)
    let getReviewMetadata (fileName: string) : ReviewMetadata option =
        match reviewDataCache.TryGetValue(fileName) with
        | (true, data) -> data
        | _ -> None
