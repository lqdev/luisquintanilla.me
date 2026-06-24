module MarketplaceProcessor

    open Domain
    open ASTParsing
    open MarkdownService
    open System
    open System.Xml.Linq
    open System.IO
    open Giraffe.ViewEngine
    open GenericBuilder
    open System.Text.Json
    open System.Text.Json.Nodes

    /// Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex =
                lines
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown

    /// Currency symbol for common currencies, otherwise "CODE ".
    let currencySymbol (currency: string) : string =
        let code = if String.IsNullOrWhiteSpace currency then "USD" else currency.Trim().ToUpperInvariant()
        match code with
        | "USD" -> "$"
        | "EUR" -> "\u20AC"
        | "GBP" -> "\u00A3"
        | "CAD" -> "CA$"
        | "AUD" -> "A$"
        | "JPY" -> "\u00A5"
        | other -> other + " "

    /// ISO currency code (defaults to USD) for schema.org / metadata.
    let currencyCode (currency: string) : string =
        if String.IsNullOrWhiteSpace currency then "USD" else currency.Trim().ToUpperInvariant()

    /// Format a price for display, e.g. "$350" or "$1,200.50".
    /// Whole amounts drop the trailing decimals.
    let formatPrice (price: decimal) (currency: string) : string =
        let symbol = currencySymbol currency
        if price = Math.Truncate price then sprintf "%s%s" symbol (price.ToString("#,##0"))
        else sprintf "%s%s" symbol (price.ToString("#,##0.00"))

    /// Normalize status to one of available | pending | sold (default available).
    let normalizeStatus (status: string) : string =
        if String.IsNullOrWhiteSpace status then "available"
        else
            match status.Trim().ToLowerInvariant() with
            | "sold" -> "sold"
            | "pending" -> "pending"
            | _ -> "available"

    /// Human-readable label for a status.
    let statusLabel (status: string) : string =
        match normalizeStatus status with
        | "sold" -> "Sold"
        | "pending" -> "Sale Pending"
        | _ -> "Available"

    /// schema.org ItemAvailability URL for a status.
    let schemaAvailability (status: string) : string =
        match normalizeStatus status with
        | "sold" -> "https://schema.org/SoldOut"
        | "pending" -> "https://schema.org/LimitedAvailability"
        | _ -> "https://schema.org/InStock"

    /// Human-readable label for a condition value.
    let conditionLabel (condition: string) : string =
        if String.IsNullOrWhiteSpace condition then ""
        else
            match condition.Trim().ToLowerInvariant() with
            | "new" -> "New"
            | "like-new" -> "Like New"
            | "good" -> "Good"
            | "fair" -> "Fair"
            | "for-parts" -> "For Parts"
            | other -> other

    /// schema.org OfferItemCondition URL for a condition value (None when unset).
    let schemaItemCondition (condition: string) : string option =
        if String.IsNullOrWhiteSpace condition then None
        else
            match condition.Trim().ToLowerInvariant() with
            | "new" -> Some "https://schema.org/NewCondition"
            | "like-new" | "good" | "fair" -> Some "https://schema.org/UsedCondition"
            | "for-parts" -> Some "https://schema.org/DamagedCondition"
            | _ -> Some "https://schema.org/UsedCondition"

    /// Minimal HTML attribute escaping for raw img tags.
    let private escapeAttr (value: string) : string =
        if isNull value then ""
        else value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;")

    /// Build schema.org Product/Offer JSON-LD for a listing (safe escaping via System.Text.Json).
    let buildJsonLd (listing: MarketplaceListing) : string =
        let pageUrl = sprintf "https://www.lqdev.me/marketplace/%s/" listing.FileName

        let offer = JsonObject()
        offer.Add("@type", JsonValue.Create("Offer"))
        offer.Add("price", JsonValue.Create(listing.Metadata.Price.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)))
        offer.Add("priceCurrency", JsonValue.Create(currencyCode listing.Metadata.Currency))
        offer.Add("availability", JsonValue.Create(schemaAvailability listing.Metadata.Status))
        offer.Add("url", JsonValue.Create(pageUrl))
        match schemaItemCondition listing.Metadata.Condition with
        | Some c -> offer.Add("itemCondition", JsonValue.Create(c))
        | None -> ()

        let product = JsonObject()
        product.Add("@context", JsonValue.Create("https://schema.org"))
        product.Add("@type", JsonValue.Create("Product"))
        product.Add("name", JsonValue.Create(listing.Metadata.Title))
        if not (String.IsNullOrWhiteSpace listing.Metadata.Description) then
            product.Add("description", JsonValue.Create(listing.Metadata.Description))
        match MediaExtractor.extractPrimaryMedia listing.Content with
        | Some media ->
            let imgs = JsonArray()
            imgs.Add(JsonValue.Create(media.MediaUrl))
            product.Add("image", imgs)
        | None -> ()
        if not (String.IsNullOrWhiteSpace listing.Metadata.Category) then
            product.Add("category", JsonValue.Create(listing.Metadata.Category))
        product.Add("offers", offer)
        product.ToJsonString()

    let create () : ContentProcessor<MarketplaceListing> = {
        Parse = fun filePath ->
            match parseMarketplaceFromFile filePath with
            | Ok parsedDoc ->
                match parsedDoc.Metadata with
                | Some metadata ->
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    let body = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = body
                        MarkdownSource = Some body
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)

        // Return raw markdown body so :::media blocks are converted to gallery HTML downstream.
        Render = fun listing -> listing.Content

        OutputPath = fun listing -> sprintf "marketplace/%s/index.html" listing.FileName

        RenderCard = fun listing ->
            let title = listing.Metadata.Title
            let url = sprintf "/marketplace/%s/" listing.FileName
            let status = normalizeStatus listing.Metadata.Status
            let priceText = formatPrice listing.Metadata.Price listing.Metadata.Currency
            let priceNote =
                if String.IsNullOrWhiteSpace listing.Metadata.PriceNote then ""
                else sprintf " %s" (listing.Metadata.PriceNote.Trim())

            // First image (thumbnail) from the :::media block
            let thumbHtml =
                match MediaExtractor.extractPrimaryMedia listing.Content with
                | Some m ->
                    let alt = m.AltText |> Option.defaultValue title
                    sprintf """<img src="%s" alt="%s" class="marketplace-card-img" loading="lazy" />""" (escapeAttr m.MediaUrl) (escapeAttr alt)
                | None -> ""

            let viewNode =
                article [ _class (sprintf "marketplace-card h-entry marketplace-status-%s" status) ] [
                    a [ _href url; _class "marketplace-card-media" ] [ rawText thumbHtml ]
                    div [ _class "marketplace-card-body" ] [
                        span [ _class (sprintf "marketplace-badge marketplace-badge-%s" status) ] [ Text (statusLabel status) ]
                        h2 [ _class "marketplace-card-title p-name" ] [ a [ _href url; _class "u-url" ] [ Text title ] ]
                        div [ _class "marketplace-card-price p-price" ] [ Text (priceText + priceNote) ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode

        RenderRss = fun listing ->
            let url = sprintf "https://www.lqdev.me/marketplace/%s" listing.FileName
            let priceText = formatPrice listing.Metadata.Price listing.Metadata.Currency
            let status = normalizeStatus listing.Metadata.Status
            let conditionText =
                let label = conditionLabel listing.Metadata.Condition
                if label = "" then "" else sprintf " \u00B7 Condition: %s" label
            let descBase =
                if String.IsNullOrWhiteSpace listing.Metadata.Description then listing.Metadata.Title
                else listing.Metadata.Description
            let description = sprintf "%s \u2014 %s (%s)%s" descBase priceText (statusLabel status) conditionText
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"

            let categories =
                if isNull listing.Metadata.Tags then []
                else listing.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList

            let item =
                XElement(XName.Get "item",
                    XElement(XName.Get "title", listing.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", listing.Metadata.Date))

            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)

            match generateSourceMarkdown listing.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()

            Some item
    }
