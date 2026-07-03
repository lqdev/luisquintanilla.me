module ReviewProcessor

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

    // Cache for review metadata extracted during parsing (ActivityPub / Schema.org)
    let private reviewDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, ReviewMetadata option>()

    /// Normalize an item type string to one of the supported review types.
    let normalizeItemType (itemType: string) =
        if String.IsNullOrWhiteSpace(itemType) then "book"
        else
            match itemType.ToLowerInvariant() with
            | "book" -> "book"
            | "movie" -> "movie"
            | "music" -> "music"
            | "business" -> "business"
            | "product" -> "product"
            | other -> other

    /// Build a display title for an RSS item based on review type and metadata.
    let private rssTitle (review: Review) =
        let itemName =
            review.AdditionalFields
            |> Map.tryFind "item"
            |> Option.defaultValue review.Metadata.Title
        match review.ItemType with
        | "book" ->
            let author = review.AdditionalFields |> Map.tryFind "author" |> Option.defaultValue ""
            if String.IsNullOrWhiteSpace(author) then review.Metadata.Title
            else sprintf "%s by %s" review.Metadata.Title author
        | _ ->
            if itemName = review.Metadata.Title then review.Metadata.Title
            else sprintf "%s — %s" review.Metadata.Title (itemName.Trim())

    /// Determine the primary image URL for a review, with sensible fallbacks.
    let private imageUrl (review: Review) (reviewDataOpt: ReviewMetadata option) =
        match reviewDataOpt with
        | Some rm when rm.ImageUrl.IsSome && not (String.IsNullOrWhiteSpace(rm.ImageUrl.Value)) -> rm.ImageUrl.Value
        | _ ->
            if not (String.IsNullOrWhiteSpace(review.Metadata.Cover)) then review.Metadata.Cover
            else "/assets/img/book-placeholder.png"

    /// Determine the rating and scale for a review.
    let private ratingAndScale (review: Review) (reviewDataOpt: ReviewMetadata option) =
        match reviewDataOpt with
        | Some rm when rm.Rating > 0.0 -> (rm.Rating, rm.Scale)
        | _ when review.Metadata.Rating > 0.0 -> (review.Metadata.Rating, 5.0)
        | _ -> (0.0, 5.0)

    let create() : ContentProcessor<Review> = {
        Parse = fun filePath ->
            match parseReviewFromFile filePath with
            | Ok parsedDoc ->
                match parsedDoc.Metadata with
                | Some metadata ->
                    let fileName = Path.GetFileNameWithoutExtension(filePath)

                    // Extract structured review data from the :::review block if present.
                    let reviewDataOpt =
                        match parsedDoc.CustomBlocks.TryGetValue("review") with
                        | true, reviewList when reviewList.Length > 0 ->
                            match reviewList.[0] with
                            | :? CustomBlocks.ReviewData as reviewData -> Some reviewData
                            | _ -> None
                        | _ -> None

                    // Determine item type and additional fields.
                    let (itemType, additionalFields) =
                        match reviewDataOpt with
                        | Some reviewData ->
                            let fields = reviewData.GetAdditionalFields()
                            // Also surface the core item name from the review block.
                            let fieldsWithItem = fields.Add("item", reviewData.Item)
                            (normalizeItemType reviewData.ItemType, fieldsWithItem)
                        | None ->
                            // Legacy book review: synthesize fields from frontmatter.
                            let mutable fields = Map.empty
                            if not (String.IsNullOrWhiteSpace(metadata.Author)) then
                                fields <- fields.Add("author", metadata.Author)
                            if not (String.IsNullOrWhiteSpace(metadata.Isbn)) then
                                fields <- fields.Add("isbn", metadata.Isbn)
                            if not (String.IsNullOrWhiteSpace(metadata.Cover)) then
                                fields <- fields.Add("cover", metadata.Cover)
                            if not (String.IsNullOrWhiteSpace(metadata.Source)) then
                                fields <- fields.Add("itemUrl", metadata.Source)
                            if metadata.Rating > 0.0 then
                                fields <- fields.Add("rating", metadata.Rating.ToString("F2"))
                            if not (String.IsNullOrWhiteSpace(metadata.DatePublished)) then
                                fields <- fields.Add("datePublished", metadata.DatePublished)
                            ("book", fields)

                    // Phase 5C: Store full review metadata in cache for ActivityPub Schema.org integration.
                    let reviewMetadata : ReviewMetadata option =
                        match reviewDataOpt with
                        | Some reviewData ->
                            Some {
                                ItemName = reviewData.Item
                                ItemType = reviewData.ItemType
                                Rating = reviewData.Rating
                                Scale = reviewData.Scale
                                Summary = if String.IsNullOrWhiteSpace(reviewData.Summary) then None else Some reviewData.Summary
                                ItemUrl = reviewData.ItemUrl
                                ImageUrl = reviewData.ImageUrl
                                Author =
                                    let author = reviewData.GetAuthor()
                                    if String.IsNullOrWhiteSpace(author) then None else Some author
                                Isbn =
                                    let isbn = reviewData.GetIsbn()
                                    if String.IsNullOrWhiteSpace(isbn) then None else Some isbn
                            }
                        | None ->
                            // Fallback to frontmatter data for legacy book reviews.
                            if metadata.Rating > 0.0 || not (String.IsNullOrWhiteSpace(metadata.Cover)) then
                                Some {
                                    ItemName = metadata.Title
                                    ItemType = "book"
                                    Rating = metadata.Rating
                                    Scale = 5.0
                                    Summary = None
                                    ItemUrl = if String.IsNullOrWhiteSpace(metadata.Source) then None else Some metadata.Source
                                    ImageUrl = if String.IsNullOrWhiteSpace(metadata.Cover) then None else Some metadata.Cover
                                    Author = if String.IsNullOrWhiteSpace(metadata.Author) then None else Some metadata.Author
                                    Isbn = if String.IsNullOrWhiteSpace(metadata.Isbn) then None else Some metadata.Isbn
                                }
                            else None

                    reviewDataCache.[fileName] <- reviewMetadata

                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                        ItemType = itemType
                        AdditionalFields = additionalFields
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)

        Render = fun review ->
            let viewNode = article [] [ rawText review.Content ]
            RenderView.AsString.xmlNode viewNode

        OutputPath = fun review ->
            sprintf "reviews/%s.html" review.FileName

        RenderCard = fun review ->
            let reviewMetadataOpt =
                match reviewDataCache.TryGetValue(review.FileName) with
                | (true, data) -> data
                | _ -> None

            let imageUrl = imageUrl review reviewMetadataOpt
            let (ratingValue, ratingScaleValue) = ratingAndScale review reviewMetadataOpt

            let ratingHtml =
                if ratingValue > 0.0 then
                    sprintf "<div class=\"rating\">%s</div>" (BlockRenderers.StarRating.render ratingValue ratingScaleValue)
                else ""

            let coverHtml =
                if not (String.IsNullOrWhiteSpace(imageUrl)) then
                    sprintf "<img src=\"%s\" alt=\"%s\" class=\"review-image img-fluid\">"
                        (Html.escapeHtml imageUrl) (Html.escapeHtml review.Metadata.Title)
                else ""

            let hiddenItemTypeHtml =
                if not (String.IsNullOrWhiteSpace(review.ItemType)) then
                    sprintf "<span class=\"review-item-type\" style=\"display:none\" data-item-type=\"%s\"></span>" (Html.escapeHtml review.ItemType)
                else ""

            sprintf "<div class=\"review-timeline-card\">%s%s%s</div>" hiddenItemTypeHtml coverHtml ratingHtml

        RenderRss = fun review ->
            let url = sprintf "https://www.lqdev.me/reviews/%s" review.FileName

            let item =
                XElement(XName.Get "item",
                    XElement(XName.Get "title", rssTitle review),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" (normalizeUrlsForRss review.Content "https://www.lqdev.me")),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))

            // Add pubDate if date exists
            let date =
                if String.IsNullOrWhiteSpace(review.Metadata.PublishedDate) then review.Metadata.DatePublished
                else review.Metadata.PublishedDate
            if not (String.IsNullOrWhiteSpace(date)) then
                item.Add(XElement(XName.Get "pubDate", date))

            // Add categories from tags
            if not (isNull review.Metadata.Tags) then
                for tag in review.Metadata.Tags do
                    if not (String.IsNullOrWhiteSpace(tag)) then
                        item.Add(XElement(XName.Get "category", tag))

            // Add source:markdown if available
            match generateSourceMarkdown review.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()

            Some item
    }

    /// Public accessor for review metadata cache (used by UnifiedFeeds).
    let getReviewMetadata (fileName: string) : ReviewMetadata option =
        match reviewDataCache.TryGetValue(fileName) with
        | (true, data) -> data
        | _ -> None
