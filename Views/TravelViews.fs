module TravelViews

open System
open Domain
open Giraffe.ViewEngine

// Generate travel-specific collection page with GPS coordinates, descriptions, and GPX download
let generateTravelCollectionPage (data: CollectionData) (travelData: TravelRecommendationData) : XmlNode =
    let collection = data.Metadata
    
    // GPX download section
    let gpxDownloadSection = 
        div [ _class "travel-downloads mb-4" ] [
            h3 [] [ Text "Download for Your Trip" ]
            p [] [ Text "Get these recommendations on your phone or watch for offline use:" ]
            
            div [ _class "download-options" ] [
                div [ _class "download-buttons mb-2" ] [
                    a [ _href $"{collection.Id}.gpx"; _class "btn btn-primary me-2 mb-2"; _download $"{collection.Id}.gpx" ] [
                        i [ _class "bi bi-download me-1" ] []
                        Text "Download GPX"
                    ]
                    a [ _href $"{collection.Id}-garmin.gpx"; _class "btn btn-primary mb-2"; _download $"{collection.Id}-garmin.gpx" ] [
                        i [ _class "bi bi-smartwatch me-1" ] []
                        Text "Download for Garmin"
                    ]
                ]
                
                div [ _class "download-help mt-2" ] [
                    small [ _class "text-muted d-block mb-1" ] [
                        Text "Import the standard GPX into "
                        a [ _href "https://osmand.net/"; _target "_blank" ] [ Text "OsmAnd" ]
                        Text ", "
                        a [ _href "https://maps.me/"; _target "_blank" ] [ Text "Maps.me" ]
                        Text ", or your preferred GPS app for offline navigation."
                    ]
                    small [ _class "text-muted d-block" ] [
                        Text "For Garmin fēnix and compatible watches: copy the Garmin file to "
                        code [] [ Text "GARMIN/NewFiles/" ]
                        Text ", eject the watch, then find them under "
                        strong [] [ Text "START → Navigate → Saved Locations" ]
                        Text "."
                    ]
                ]
            ]
        ]
    
    // Individual place cards
    let placeCards = 
        div [ _class "travel-places" ] [
            for place in travelData.Places do
                div [ _class "travel-place-card card mb-4" ] [
                    div [ _class "card-body" ] [
                        // Place header with category icon
                        div [ _class "place-header d-flex align-items-center mb-3" ] [
                            span [ _class "category-icon me-2" ] [
                                match place.Category.ToLower() with
                                | "restaurant" -> Text "🍽️"
                                | "attraction" -> Text "🏛️"
                                | "hidden" -> Text "💎"
                                | "shopping" -> Text "🛍️" 
                                | "practical" -> Text "🏪"
                                | _ -> Text "📍"
                            ]
                            h4 [ _class "place-name mb-0" ] [ Text place.Name ]
                        ]
                        
                        // GPS coordinates with clickable links
                        div [ _class "coordinates mb-3" ] [
                            span [ _class "coord-display text-primary" ] [
                                i [ _class "bi bi-geo-alt me-1" ] []
                                Text $"%.6f{place.Latitude}, %.6f{place.Longitude}"
                            ]
                            
                            div [ _class "coordinate-links mt-1" ] [
                                a [ _href $"geo:{place.Latitude},{place.Longitude}"; _class "btn btn-sm btn-outline-primary me-1 d-none-desktop" ] [
                                    Text "📱 Open in App"
                                ]
                                a [ _href $"https://www.openstreetmap.org/?mlat={place.Latitude}&mlon={place.Longitude}&zoom=18"; _target "_blank"; _class "btn btn-sm btn-outline-secondary me-1" ] [
                                    Text "🗺️ OpenStreetMap"
                                ]
                                a [ _href $"https://maps.google.com/?q={place.Latitude},{place.Longitude}"; _target "_blank"; _class "btn btn-sm btn-outline-secondary" ] [
                                    Text "🌍 Google Maps"
                                ]
                            ]
                        ]
                        
                        // Description
                        p [ _class "place-description" ] [ Text place.Description ]
                        
                        // Personal note (if available)
                        match place.PersonalNote with
                        | Some note when not (String.IsNullOrWhiteSpace(note)) ->
                            div [ _class "personal-note mt-3 p-3 bg-light rounded" ] [
                                div [ _class "personal-note-header mb-2" ] [
                                    i [ _class "bi bi-lightbulb text-warning me-1" ] []
                                    strong [] [ Text "Personal tip:" ]
                                ]
                                Text note
                            ]
                        | _ -> ()
                        
                        // Practical information (if available)
                        match place.PracticalInfo with
                        | Some info ->
                            div [ _class "practical-info mt-3" ] [
                                div [ _class "info-row d-flex flex-wrap" ] [
                                    match info.Price with
                                    | Some price -> 
                                        span [ _class "info-item me-4 mb-1" ] [
                                            i [ _class "bi bi-currency-euro me-1" ] []
                                            Text price
                                        ]
                                    | None -> ()
                                    
                                    match info.Hours with
                                    | Some hours ->
                                        span [ _class "info-item me-4 mb-1" ] [
                                            i [ _class "bi bi-clock me-1" ] []
                                            Text hours
                                        ]
                                    | None -> ()
                                    
                                    match info.Phone with
                                    | Some phone ->
                                        span [ _class "info-item me-4 mb-1" ] [
                                            i [ _class "bi bi-telephone me-1" ] []
                                            a [ _href $"tel:{phone}" ] [ Text phone ]
                                        ]
                                    | None -> ()
                                    
                                    match info.Website with
                                    | Some website ->
                                        span [ _class "info-item me-4 mb-1" ] [
                                            i [ _class "bi bi-globe me-1" ] []
                                            a [ _href website; _target "_blank" ] [ Text "Website" ]
                                        ]
                                    | None -> ()
                                ]
                            ]
                        | None -> ()
                    ]
                ]
        ]
    
    // Main page layout
    div [ _class "mr-auto" ] [
        // Page header
        div [ _class "travel-header mb-4" ] [
            h2 [] [ Text collection.Title ]
            p [ _class "lead" ] [ Text collection.Description ]
            
            // Collection metadata
            if collection.ItemCount.IsSome then
                p [ _class "collection-meta text-muted" ] [
                    Text $"Contains {collection.ItemCount.Value} recommendations"
                ]
        ]
        
        // Downloads section
        gpxDownloadSection
        
        // Interactive map with Leaflet.js integration
        div [ _class "travel-map mb-4" ] [
            h3 [] [ Text "Interactive Map" ]
            div [ 
                _id "travel-map"
                _class "leaflet-map-container"
                _style "height: 400px; border: 1px solid #dee2e6; border-radius: 0.375rem;"
                attr "data-gpx-url" $"{collection.Id}.gpx"
                attr "data-places" (System.Text.Json.JsonSerializer.Serialize(travelData.Places))
            ] [
                // Fallback content shown when JavaScript is disabled
                div [ _class "text-center text-muted d-flex align-items-center justify-content-center h-100" ] [
                    div [] [
                        i [ _class "bi bi-map display-4 mb-2" ] []
                        br []
                        Text "Interactive map requires JavaScript"
                        br []
                        small [] [ Text "Use GPX download or coordinate links below" ]
                    ]
                ]
            ]
        ]
        
        // Places section
        h3 [] [ Text "Recommendations" ]
        placeCards
    ]