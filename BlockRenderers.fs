module BlockRenderers

open CustomBlocks
open System

/// HTML utility functions for rendering
module Html =
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

/// Microformat-compliant CSS classes for IndieWeb compatibility
module Microformats =
    let hCard = "h-card"
    let hEntry = "h-entry"
    let pName = "p-name"
    let pSummary = "p-summary"
    let pLocation = "p-location"
    let uUrl = "u-url"
    let dtStart = "dt-start"
    let dtEnd = "dt-end"
    let pRating = "p-rating"
    let pCategory = "p-category"

/// Renderer for MediaItem list
module MediaRenderer =
    let renderMediaItem (item: MediaItem) =
        let mediaElement =
            match item.media_type.ToLower() with
            | "image" ->
                Html.selfClosingElement "img" 
                    (Html.attribute "src" item.uri + 
                     Html.attribute "alt" item.alt_text +
                     Html.attribute "class" "media-image")
            | "video" ->
                Html.element "video" 
                    (Html.attribute "src" item.uri + 
                     Html.attribute "controls" "controls" +
                     Html.attribute "class" "media-video")
                    item.alt_text
            | "audio" ->
                Html.element "audio"
                    (Html.attribute "src" item.uri +
                     Html.attribute "controls" "controls" +
                     Html.attribute "class" "media-audio")
                    item.alt_text
            | _ ->
                Html.element "a"
                    (Html.attribute "href" item.uri +
                     Html.attribute "class" "media-link")
                    item.alt_text
        
        let captionElement =
            if not (String.IsNullOrWhiteSpace(item.caption)) then
                Html.element "figcaption" (Html.attribute "class" "media-caption") 
                    (Html.escapeHtml item.caption)
            else ""
        
        Html.element "figure" (Html.attribute "class" "custom-media-item")
            (mediaElement + captionElement)
    
    let render (mediaItems: MediaItem list) =
        let renderedItems = 
            mediaItems 
            |> List.map renderMediaItem 
            |> String.concat "\n"
        
        Html.element "div" (Html.attribute "class" "custom-media-block") renderedItems

/// SVG Star Rating Helper - renders accessible star ratings with proper ARIA labels
module StarRating =
    /// Render a single SVG star (full, half, or empty)
    let private svgStar (fillType: string) =
        let fillColor = 
            match fillType with
            | "full" -> "#FFB400"  // Gold for full stars
            | "half" -> "#FFB400"  // Gold for half star fill
            | _ -> "none"         // Empty star (just outline)
        
        let strokeColor = "#FFB400"  // Gold outline for all stars
        
        // SVG star path - a 5-pointed star
        let starPath = "M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"
        
        if fillType = "half" then
            // Half star: use linear gradient for partial fill
            sprintf """<svg class="star-icon star-%s" viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
                <defs>
                    <linearGradient id="halfGrad">
                        <stop offset="50%%" stop-color="%s"/>
                        <stop offset="50%%" stop-color="transparent"/>
                    </linearGradient>
                </defs>
                <path d="%s" fill="url(#halfGrad)" stroke="%s" stroke-width="1.5"/>
            </svg>""" fillType fillColor starPath strokeColor
        else
            sprintf """<svg class="star-icon star-%s" viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
                <path d="%s" fill="%s" stroke="%s" stroke-width="1.5"/>
            </svg>""" fillType starPath fillColor strokeColor
    
    /// Render a star rating with SVG icons
    /// rating: the actual rating value (e.g., 4.25)
    /// scale: the maximum scale (e.g., 5.0)
    /// Returns HTML string with SVG stars and numeric rating
    let render (rating: float) (scale: float) =
        if rating <= 0.0 then ""
        else
            // Normalize rating to 5-star scale for display
            let normalizedRating = (rating / scale) * 5.0
            let fullStars = int (floor normalizedRating)
            let hasHalfStar = normalizedRating - float fullStars >= 0.25 && normalizedRating - float fullStars < 0.75
            let extraFullStar = normalizedRating - float fullStars >= 0.75
            let actualFullStars = if extraFullStar then fullStars + 1 else fullStars
            let emptyStars = 5 - actualFullStars - (if hasHalfStar && not extraFullStar then 1 else 0)
            
            // Build stars HTML
            let starsHtml = 
                String.concat "" [
                    for _ in 1 .. actualFullStars do yield svgStar "full"
                    if hasHalfStar && not extraFullStar then yield svgStar "half"
                    for _ in 1 .. emptyStars do yield svgStar "empty"
                ]
            
            // ARIA label for accessibility
            let ariaLabel = sprintf "Rating: %.1f out of %.1f" rating scale
            
            // Combine stars with numeric rating
            sprintf """<div class="svg-star-rating" role="img" aria-label="%s">
                <span class="stars">%s</span>
                <span class="rating-text">%.1f/%.1f</span>
            </div>""" ariaLabel starsHtml rating scale

/// Renderer for ReviewData
module ReviewRenderer =
    let render (review: ReviewData) =
        let ratingElement =
            if review.Rating > 0.0 then
                Html.element "div" 
                    (Html.attribute "class" ("review-rating " + Microformats.pRating))
                    (StarRating.render review.Rating review.Scale)
            else ""
        
        let summaryElement =
            let summary = review.Summary
            if not (String.IsNullOrWhiteSpace(summary)) then
                Html.element "div"
                    (Html.attribute "class" ("review-summary " + Microformats.pSummary))
                    (Html.escapeHtml summary)
            else ""
        
        let titleElement =
            Html.element "h3"
                (Html.attribute "class" ("review-title " + Microformats.pName))
                (Html.escapeHtml review.Item)
        
        let imageElement =
            match review.ImageUrl with
            | Some imageUrl when not (String.IsNullOrWhiteSpace(imageUrl)) ->
                Html.element "div" (Html.attribute "class" "review-image")
                    (Html.element "img" 
                        (Html.attribute "src" imageUrl + 
                         Html.attribute "alt" review.Item + 
                         Html.attribute "class" "review-thumbnail")
                        "")
            | _ -> ""
        
        let prosElement =
            match review.Pros with
            | Some (prosArray: string array) when prosArray.Length > 0 ->
                let prosItems = 
                    prosArray 
                    |> Array.map (fun pro -> 
                        Html.element "li" "" (Html.escapeHtml pro))
                    |> String.concat ""
                Html.element "div" (Html.attribute "class" "review-pros")
                    (Html.element "h4" "" "Pros:" + 
                     Html.element "ul" "" prosItems)
            | _ -> ""
        
        let consElement =
            match review.Cons with
            | Some (consArray: string array) when consArray.Length > 0 ->
                let consItems = 
                    consArray 
                    |> Array.map (fun con -> 
                        Html.element "li" "" (Html.escapeHtml con))
                    |> String.concat ""
                Html.element "div" (Html.attribute "class" "review-cons")
                    (Html.element "h4" "" "Cons:" + 
                     Html.element "ul" "" consItems)
            | _ -> ""
        
        let urlElement =
            match review.ItemUrl with
            | Some url when not (String.IsNullOrWhiteSpace(url)) ->
                Html.element "div" (Html.attribute "class" "review-url")
                    (Html.element "a" 
                        (Html.attribute "href" url + Html.attribute "class" Microformats.uUrl)
                        (Html.escapeHtml url))
            | _ -> ""
        
        Html.element "div" 
            (Html.attribute "class" ("custom-review-block " + Microformats.hEntry))
            (titleElement + imageElement + ratingElement + summaryElement + prosElement + consElement + urlElement)

/// Renderer for VenueData
module VenueRenderer =
    let render (venue: VenueData) =
        let nameElement =
            Html.element "h3" 
                (Html.attribute "class" ("venue-name " + Microformats.pName))
                (Html.escapeHtml venue.venue_name)
        
        let addressElement =
            if not (String.IsNullOrWhiteSpace(venue.venue_address)) then
                let fullAddress = sprintf "%s, %s, %s" venue.venue_address venue.venue_city venue.venue_country
                Html.element "div"
                    (Html.attribute "class" "venue-address")
                    (Html.escapeHtml fullAddress)
            else ""
        
        let locationElement =
            match (venue.latitude, venue.longitude) with
            | (Some lat, Some lon) ->
                Html.element "div"
                    (Html.attribute "class" ("venue-coordinates " + Microformats.pLocation))
                    (sprintf "%.6f, %.6f" lat lon)
            | _ -> ""
        
        let urlElement =
            match venue.venue_url with
            | Some url when not (String.IsNullOrWhiteSpace(url)) ->
                Html.element "div" (Html.attribute "class" "venue-url")
                    (Html.element "a"
                        (Html.attribute "href" url + Html.attribute "class" Microformats.uUrl)
                        (Html.escapeHtml url))
            | _ -> ""
        
        Html.element "div"
            (Html.attribute "class" ("custom-venue-block " + Microformats.hCard))
            (nameElement + addressElement + locationElement + urlElement)

/// Renderer for RsvpData
module RsvpRenderer =
    let render (rsvp: RsvpData) =
        let eventElement =
            Html.element "h3"
                (Html.attribute "class" ("rsvp-event " + Microformats.pName))
                (Html.escapeHtml rsvp.event_name)
        
        let responseElement =
            let responseText =
                match rsvp.rsvp_status.ToLower() with
                | "yes" -> "✅ Attending"
                | "no" -> "❌ Not Attending"
                | "maybe" -> "❓ Maybe Attending"
                | "interested" -> "⭐ Interested"
                | _ -> sprintf "Response: %s" rsvp.rsvp_status
            
            Html.element "div"
                (Html.attribute "class" "rsvp-response")
                (Html.escapeHtml responseText)
        
        let dateElement =
            if not (String.IsNullOrWhiteSpace(rsvp.event_date)) then
                Html.element "div"
                    (Html.attribute "class" ("rsvp-date " + Microformats.dtStart))
                    (Html.escapeHtml rsvp.event_date)
            else ""
        
        let urlElement =
            if not (String.IsNullOrWhiteSpace(rsvp.event_url)) then
                Html.element "div" (Html.attribute "class" "rsvp-url")
                    (Html.element "a"
                        (Html.attribute "href" rsvp.event_url + Html.attribute "class" Microformats.uUrl)
                        (Html.escapeHtml rsvp.event_url))
            else ""
        
        Html.element "div"
            (Html.attribute "class" ("custom-rsvp-block " + Microformats.hEntry))
            (eventElement + responseElement + dateElement + urlElement)

/// Main rendering function for any custom block
module BlockRenderer =
    let renderCustomBlock (block: CustomBlock) : string =
        match block with
        | Media mediaItems -> MediaRenderer.render mediaItems
        | Review reviewData -> ReviewRenderer.render reviewData
        | Venue venueData -> VenueRenderer.render venueData
        | Rsvp rsvpData -> RsvpRenderer.render rsvpData
    
    /// Render a list of custom blocks to HTML
    let renderCustomBlocks (blocks: CustomBlock list) : string =
        blocks
        |> List.map renderCustomBlock
        |> String.concat "\n"
    
    /// Render custom blocks with a wrapper div
    let renderCustomBlocksWithWrapper (blocks: CustomBlock list) : string =
        if List.isEmpty blocks then
            ""
        else
            let content = renderCustomBlocks blocks
            Html.element "div" (Html.attribute "class" "custom-blocks") content

// =====================================================================
// Resume Block Renderers
// =====================================================================

/// Renderer for ExperienceBlock
module ExperienceRenderer =
    let render (block: CustomBlocks.ExperienceBlock) =
        let endDateStr = 
            match block.End with
            | Some "current" -> "Present"
            | Some date -> 
                try
                    let dt = DateTime.Parse(date)
                    dt.ToString("MMM yyyy")
                with
                | _ -> date
            | None -> "Present"
        
        let startDateStr = 
            try
                let dt = DateTime.Parse(block.Start)
                dt.ToString("MMM yyyy")
            with
            | _ -> block.Start
        
        let companyHtml = Markdig.Markdown.ToHtml(block.Company)
        
        let highlightsHtml = 
            if not (String.IsNullOrWhiteSpace(block.Content)) then
                // Process the entire content as markdown to preserve list structure
                Markdig.Markdown.ToHtml(block.Content.Trim())
            else ""
        
        Html.element "div" (Html.attribute "class" "experience-item")
            (Html.element "div" (Html.attribute "class" "experience-header")
                (Html.element "h3" "" (Html.escapeHtml block.Role) +
                 Html.element "span" (Html.attribute "class" "duration") (Html.escapeHtml (startDateStr + " - " + endDateStr))) +
             Html.element "div" (Html.attribute "class" "company") companyHtml +
             highlightsHtml)

/// Renderer for ProjectBlock
module ProjectRenderer =
    let render (block: CustomBlocks.ProjectBlock) =
        let titleHtml = 
            match block.Url with
            | Some url when not (String.IsNullOrWhiteSpace url) ->
                Html.element "h3" ""
                    (Html.element "a" 
                        (Html.attribute "href" url + Html.attribute "target" "_blank")
                        (Html.escapeHtml block.Title))
            | _ ->
                Html.element "h3" "" (Html.escapeHtml block.Title)
        
        let techHtml = 
            match block.Tech with
            | Some tech when not (String.IsNullOrWhiteSpace tech) ->
                Html.element "div" (Html.attribute "class" "tech-stack") (Html.escapeHtml tech)
            | _ -> ""
        
        let descriptionHtml = 
            if not (String.IsNullOrWhiteSpace(block.Content)) then
                Html.element "div" (Html.attribute "class" "project-description") 
                    (Markdig.Markdown.ToHtml(block.Content))
            else ""
        
        Html.element "div" (Html.attribute "class" "project-item")
            (Html.element "div" (Html.attribute "class" "project-header") titleHtml +
             techHtml +
             descriptionHtml)

/// Renderer for SkillsBlock
module SkillsRenderer =
    let render (block: CustomBlocks.SkillsBlock) =
        let skillsHtml = 
            if not (String.IsNullOrWhiteSpace(block.Content)) then
                // Check if content is comma-separated or bullet list
                if block.Content.Contains(",") && not (block.Content.Contains("\n-") || block.Content.Contains("\n*")) then
                    // Comma-separated format
                    let skills = block.Content.Split(',') |> Array.map (fun s -> s.Trim())
                    let listItems = 
                        skills 
                        |> Array.map (fun skill -> Html.element "li" "" (Markdig.Markdown.ToHtml(skill)))
                        |> String.concat ""
                    Html.element "ul" "" listItems
                else
                    // Bullet list format
                    let lines = block.Content.Split('\n') |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
                    let listItems = 
                        lines 
                        |> Array.map (fun line -> 
                            let cleanLine = line.Trim().TrimStart('-', '*').Trim()
                            Html.element "li" "" (Markdig.Markdown.ToHtml(cleanLine)))
                        |> String.concat ""
                    Html.element "ul" "" listItems
            else ""
        
        Html.element "div" (Html.attribute "class" "skill-category")
            (Html.element "h3" "" (Html.escapeHtml block.Category) +
             skillsHtml)

/// Renderer for TestimonialBlock
module TestimonialRenderer =
    let render (block: CustomBlocks.TestimonialBlock) =
        let quoteHtml = 
            if not (String.IsNullOrWhiteSpace(block.Content)) then
                Html.element "p" (Html.attribute "class" "quote") (Html.escapeHtml block.Content)
            else ""
        
        let authorHtml = 
            Html.element "p" (Html.attribute "class" "testimonial-author")
                ("— " + (Markdig.Markdown.ToHtml(block.Author)))
        
        Html.element "div" (Html.attribute "class" "testimonial")
            (quoteHtml + authorHtml)

/// Renderer for EducationBlock
module EducationRenderer =
    let render (block: CustomBlocks.EducationBlock) =
        let institutionHtml = Markdig.Markdown.ToHtml(block.Institution)
        
        let yearHtml = 
            match block.Year with
            | Some year when not (String.IsNullOrWhiteSpace year) ->
                Html.element "div" (Html.attribute "class" "year") (Html.escapeHtml year)
            | _ -> ""
        
        let detailsHtml = 
            if not (String.IsNullOrWhiteSpace(block.Content)) then
                Html.element "div" (Html.attribute "class" "details") 
                    (Markdig.Markdown.ToHtml(block.Content))
            else ""
        
        Html.element "div" (Html.attribute "class" "education-item")
            (Html.element "h3" "" (Html.escapeHtml block.Degree) +
             Html.element "div" (Html.attribute "class" "institution") institutionHtml +
             yearHtml +
             detailsHtml)
