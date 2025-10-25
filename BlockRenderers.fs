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

/// Renderer for ReviewData
module ReviewRenderer =
    let render (review: ReviewData) =
        let ratingElement =
            if review.rating > 0.0 then
                Html.element "div" 
                    (Html.attribute "class" ("review-rating " + Microformats.pRating))
                    (sprintf "Rating: %.1f/%.1f" review.rating (review.GetScale()))
            else ""
        
        let summaryElement =
            let summary = review.GetSummary()
            if not (String.IsNullOrWhiteSpace(summary)) then
                Html.element "div"
                    (Html.attribute "class" ("review-summary " + Microformats.pSummary))
                    (Html.escapeHtml summary)
            else ""
        
        let titleElement =
            Html.element "h3"
                (Html.attribute "class" ("review-title " + Microformats.pName))
                (Html.escapeHtml review.item)
        
        let imageElement =
            match review.image_url with
            | Some imageUrl when not (String.IsNullOrWhiteSpace(imageUrl)) ->
                Html.element "div" (Html.attribute "class" "review-image")
                    (Html.element "img" 
                        (Html.attribute "src" imageUrl + 
                         Html.attribute "alt" review.item + 
                         Html.attribute "class" "review-thumbnail")
                        "")
            | _ -> ""
        
        let itemTypeElement =
            let itemType = review.GetItemType()
            if itemType <> "unknown" then
                Html.element "div"
                    (Html.attribute "class" "review-item-type")
                    (Html.element "span" 
                        (Html.attribute "class" "item-type-badge")
                        (Html.escapeHtml (itemType.ToUpperInvariant())))
            else ""
        
        let prosElement =
            match review.pros with
            | Some prosArray when prosArray.Length > 0 ->
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
            match review.cons with
            | Some consArray when consArray.Length > 0 ->
                let consItems = 
                    consArray 
                    |> Array.map (fun con -> 
                        Html.element "li" "" (Html.escapeHtml con))
                    |> String.concat ""
                Html.element "div" (Html.attribute "class" "review-cons")
                    (Html.element "h4" "" "Cons:" + 
                     Html.element "ul" "" consItems)
            | _ -> ""
        
        let additionalFieldsElement =
            match review.additional_fields with
            | Some fields when fields.Count > 0 ->
                let fieldItems = 
                    fields 
                    |> Seq.map (fun kvp -> 
                        Html.element "div" (Html.attribute "class" "additional-field")
                            (Html.element "strong" "" (Html.escapeHtml kvp.Key + ": ") +
                             Html.escapeHtml (kvp.Value.ToString())))
                    |> String.concat ""
                Html.element "div" (Html.attribute "class" "review-additional-fields")
                    (Html.element "h4" "" "Additional Information:" + 
                     Html.element "div" (Html.attribute "class" "field-list") fieldItems)
            | _ -> ""
        
        let urlElement =
            match review.item_url with
            | Some url when not (String.IsNullOrWhiteSpace(url)) ->
                Html.element "div" (Html.attribute "class" "review-url")
                    (Html.element "a" 
                        (Html.attribute "href" url + Html.attribute "class" Microformats.uUrl)
                        (Html.escapeHtml url))
            | _ -> ""
        
        Html.element "div" 
            (Html.attribute "class" ("custom-review-block " + Microformats.hEntry))
            (titleElement + imageElement + itemTypeElement + ratingElement + summaryElement + prosElement + consElement + additionalFieldsElement + urlElement)

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
