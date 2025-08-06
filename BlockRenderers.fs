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
                    (sprintf "Rating: %.1f/%.1f" review.rating review.max_rating)
            else ""
        
        let summaryElement =
            if not (String.IsNullOrWhiteSpace(review.review_text)) then
                Html.element "div"
                    (Html.attribute "class" ("review-summary " + Microformats.pSummary))
                    (Html.escapeHtml review.review_text)
            else ""
        
        let titleElement =
            Html.element "h3"
                (Html.attribute "class" ("review-title " + Microformats.pName))
                (Html.escapeHtml review.item_title)
        
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
            (titleElement + ratingElement + summaryElement + urlElement)

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
