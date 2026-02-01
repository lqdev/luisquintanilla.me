module ContentViews

open Giraffe.ViewEngine
open System
open System.Text.RegularExpressions
open Domain
open MarkdownService
open ComponentViews
open CustomBlocks

// Helper function to extract image URL from processed review HTML content
let extractImageFromReviewHtml (content: string) : string option =
    try
        // Look for img tags within custom-review-block divs
        let pattern = @"<div class=""custom-review-block[^>]*>.*?<img[^>]*src=""([^""]+)""[^>]*>.*?</div>"
        let match' = Regex.Match(content, pattern, RegexOptions.Singleline)
        if match'.Success then
            Some match'.Groups.[1].Value
        else None
    with
    | _ -> None

// Response body views for different IndieWeb response types
let replyBodyView (post:Response) = 
    let cleanContent = 
        post.Content
            .Replace("No description available", "")
            .Replace("<p></p>", "")
    
    // Remove timestamp patterns like "2025-06-29 17:26"
    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
    
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-reply-fill"; _style "margin-right:5px;margin-left:5px;color:#3F5576;"] []
            a [_class "u-in-reply-to"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText cleanedContent
        ]
    ]        

let reshareBodyView (post:Response) = 
    let cleanContent = 
        post.Content
            .Replace("No description available", "")
            .Replace("<p></p>", "")
    
    // Remove timestamp patterns like "2025-06-29 17:26"
    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
    
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-share-fill"; _style "margin-right:5px;margin-left:5px;color:#C0587E;"] []
            a [_class "u-repost-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText cleanedContent
        ]
    ]

let starBodyView (post:Response) = 
    let cleanContent = 
        post.Content
            .Replace("No description available", "")
            .Replace("<p></p>", "")
    
    // Remove timestamp patterns like "2025-06-29 17:26"
    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
    
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-star-fill"; _style "margin-right:5px;margin-left:5px;color:#ff7518;"] []
            a [_class "u-like-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText cleanedContent
        ]
    ]

let bookmarkBodyView (post:Response) = 
    let cleanContent = 
        post.Content
            .Replace("No description available", "")
            .Replace("<p></p>", "")
    
    // Remove timestamp patterns like "2025-06-29 17:26"
    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
    
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-journal-bookmark-fill"; _style "margin-right:5px;margin-left:5px;color:#4a60b6;"] []
            a [_class "u-bookmark-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText cleanedContent
        ]
    ]

/// Phase 6A: RSVP response body view with IndieWeb p-rsvp microformat
let rsvpBodyView (post:Response) = 
    let cleanContent = 
        post.Content
            .Replace("No description available", "")
            .Replace("<p></p>", "")
    
    // Remove timestamp patterns like "2025-06-29 17:26"
    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
    
    // Get RSVP status for display and microformat
    let rsvpStatus = 
        match post.Metadata.RsvpStatus with
        | Some status -> status.ToLowerInvariant()
        | None -> "interested"  // Default to interested if not specified
    
    // Choose icon and color based on RSVP status
    let (iconClass, iconColor) =
        match rsvpStatus with
        | "yes" -> ("bi bi-check-circle-fill", "#28a745")  // Green for yes
        | "no" -> ("bi bi-x-circle-fill", "#dc3545")       // Red for no
        | "maybe" -> ("bi bi-question-circle-fill", "#ffc107")  // Yellow for maybe
        | "interested" -> ("bi bi-calendar-check-fill", "#6c757d")  // Gray for interested
        | _ -> ("bi bi-calendar-event-fill", "#4a60b6")    // Default blue
    
    div [ _class "card-body" ] [
        p [] [
            span [_class iconClass; _style $"margin-right:5px;margin-left:5px;color:{iconColor};"] []
            span [_class "p-rsvp"] [Text rsvpStatus]
            Text " to "
            a [_class "u-in-reply-to"; _href $"{post.Metadata.TargetUrl}"; _target "_blank"] [
                Text post.Metadata.TargetUrl
            ]
        ]
        div [_class "e-content"] [
            rawText cleanedContent
        ]
    ]

// Individual content type views
let feedPostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "posts" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
        ]
        footer
    ]

let notePostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "notes" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
        ]
        footer
    ]

// Individual post views with webmention forms for standalone pages
let individualFeedPostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "posts" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
            hr []
            webmentionForm
        ]
        footer
    ]

let individualNotePostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "notes" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
            hr []
            webmentionForm
        ]
        footer
    ]

let responsePostView (post: Response) = 
    let header = cardHeader post.Metadata.DatePublished
    let footer = cardFooter "responses" post.FileName post.Metadata.Tags
    let body = 
        match post.Metadata.ResponseType with
        | "reply" -> replyBodyView post
        | "reshare" -> reshareBodyView post
        | "star" -> starBodyView post
        | "bookmark" -> bookmarkBodyView post
        | "rsvp" -> rsvpBodyView post
        | _ -> div [_class "card-body"] [p [] [Text "No content"]]
    
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header
        body    
        footer
    ] 

let bookmarkPostView (bookmark: Bookmark) = 
    let header = cardHeader bookmark.Metadata.DatePublished
    let footer = cardFooter "bookmarks" bookmark.FileName bookmark.Metadata.Tags
    
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header
        div [ _class "card-body" ] [
            a [ _href bookmark.Metadata.BookmarkOf; _class "u-bookmark-of" ] [
                h5 [ _class "card-title p-name" ] [ Text bookmark.Metadata.Title ]
            ]
            p [ _class "card-text p-summary" ] [ Text bookmark.Metadata.Description ]
        ]
        footer
    ] 

let bookPostView (book: Book) = 
    div [_class "card mb-4 mx-auto"] [
        div [_class "row"] [
            div [_class "col-md-4"] [
                // Extract imageUrl from processed HTML review content, fall back to metadata, then to placeholder
                let imageUrl = 
                    match extractImageFromReviewHtml book.Content with
                    | Some reviewImageUrl -> reviewImageUrl
                    | None -> 
                        if String.IsNullOrWhiteSpace(book.Metadata.Cover) then
                            "/assets/img/book-placeholder.png"  // Default book placeholder
                        else
                            book.Metadata.Cover
                img [_src imageUrl; _class "img-fluid"; _alt book.Metadata.Title]
            ]
            div [_class "col-md-8"] [
                div [_class "card-body"] [
                    a [_href $"/reviews/{book.FileName}"] [
                        h5 [_class "card-title"] [Text book.Metadata.Title]
                    ]
                    p [_class "card-text"] [Text $"Author: {book.Metadata.Author}"]
                    // Rating should now come from updated metadata (includes custom block rating)
                    let displayRating = $"Rating: {book.Metadata.Rating:F1}/5"
                    
                    // Check if we have a custom review by looking for review HTML content
                    if book.Content.Contains("custom-review-block") then
                        p [_class "card-text"] [
                            Text displayRating
                            Text " - "
                            a [_href $"/reviews/{book.FileName}"; _class "text-decoration-none"] [
                                Text "View Review"
                                span [_class "ms-1"] [Text "→"]
                            ]
                        ]
                    else
                        p [_class "card-text"] [Text displayRating]
                ]                
            ]
        ]
    ]

let albumPostView (album: Album) = 
    let header = cardHeader album.Metadata.Date
    let footer = albumCardFooter album.FileName album.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header
        div [ _class "card-body" ] [
            h5 [_class "card-title"] [Text album.Metadata.Title]
            
            // Handle both legacy format (with Images) and new format (with :::media blocks)
            if isNull album.Metadata.Images || Array.isEmpty album.Metadata.Images then
                // New format - content already includes processed :::media blocks
                div [] [rawText "<!-- Content will be processed by :::media blocks -->"]
            else
                // Legacy format - convert album to markdown with :::media block and render
                let mediaItems = 
                    album.Metadata.Images 
                    |> Array.map (fun img -> 
                        sprintf "- media_type: image\n  uri: %s\n  alt_text: %s\n  caption: %s\n  aspect: \"16:9\"" 
                            img.ImagePath img.AltText img.Description)
                    |> String.concat "\n"
                let mediaBlock = sprintf ":::media\n%s\n:::media" mediaItems
                
                rawText (mediaBlock |> convertMdToHtml)
            
        ]
        footer
    ]

let presentationPageView (presentation:Presentation) = 
    div [_class "presentation-container"] [
        // Hidden IndieWeb author information for microformats compliance
        div [ _class "u-author h-card microformat-hidden" ] [
            img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
            a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
        ]
        
        article [ _class "h-entry presentation-article" ] [
            header [ _class "presentation-header" ] [
                h2 [ _class "p-name" ] [Text presentation.Metadata.Title]
                div [ _class "presentation-meta" ] [
                    time [ _class "dt-published"; attr "datetime" presentation.Metadata.Date ] [
                        let publishDate = DateTimeOffset.Parse(presentation.Metadata.Date)
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
            ]
            
            div [ _class "e-content presentation-content" ] [
                div [ _class "reveal"] [
                    div [ _class "slides"] [
                        section [ flag "data-markdown"] [
                            textarea [ flag "data-template" ] [
                                rawText presentation.Content
                            ]
                        ]
                    ]
                ]
            ]
            
            footer [ _class "presentation-footer" ] [
                hr []
                div [ _class "permalink-info d-flex align-items-center" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/resources/presentations/{presentation.FileName}/" ] [
                        Text $"/resources/presentations/{presentation.FileName}/"
                    ]
                    copyPermalinkButton $"/resources/presentations/{presentation.FileName}/"
                ]
                h3 [] [Text "Resources"]
                ul [] [
                    for resource in presentation.Metadata.Resources do
                        li [] [a [_href $"{resource.Url}"] [Text resource.Text]]
                ]
            ]
        ]
    ]

let liveStreamPageView (stream:Livestream) = 
    div [_class "presentation-container"] [
        h2 [] [Text stream.Metadata.Title]
        rawText stream.Content
        hr []
        h3 [] [Text "Resources"]
        ul [] [
            for resource in stream.Metadata.Resources do
                li [] [a [_href $"{resource.Url}"] [Text resource.Text]]
        ]  
    ]

// Content views with backlinks
let feedPostViewWithBacklink (feedPostView:XmlNode) = 
    let mainFeedBacklink = feedBacklink "/posts"
    div [] [
        feedPostView        
        mainFeedBacklink
    ]

let notePostViewWithBacklink (notePostView:XmlNode) = 
    let notesBacklink = feedBacklink "/notes"
    div [] [
        notePostView        
        notesBacklink
    ]

let reponsePostViewWithBacklink (responsePostView:XmlNode) = 
    let responseBacklink = feedBacklink "/responses"
    div [] [
        responsePostView
        responseBacklink
    ]

let albumPostViewWithBacklink (albumPostView:XmlNode) = 
    let albumBacklink = feedBacklink "/media"
    div [] [
        albumPostView        
        albumBacklink
    ]

// =====================================================================
// Resume View Module
// =====================================================================

module ResumeView =
    open BlockRenderers
    
    let private parseStatus (status: string option) =
        match status with
        | Some "open-to-opportunities" -> Domain.OpenToOpportunities
        | Some "not-looking" -> Domain.NotLooking
        | _ -> Domain.NotSpecified
    
    let private renderStatusBadge (status: Domain.AvailabilityStatus) =
        match status with
        | Domain.OpenToOpportunities -> 
            span [ _class "status-badge open" ] [ str "Open to opportunities" ]
        | Domain.NotLooking -> 
            span [ _class "status-badge not-looking" ] [ str "Not currently looking" ]
        | Domain.NotSpecified -> 
            emptyText
    
    let private renderContactLinks (links: System.Collections.Generic.Dictionary<string, string>) =
        let getIcon (linkName: string) =
            match linkName.ToLower() with
            | "email" -> "✉️"
            | "linkedin" -> "💼"
            | "github" -> "💻"
            | "website" | "portfolio" -> "🌐"
            | _ -> "🔗"
        
        div [ _class "contact-links" ] [
            for kvp in links do
                a [ _href kvp.Value; _target "_blank"; _rel "noopener noreferrer" ] [
                    span [ _style "margin-right: 8px;" ] [ str (getIcon kvp.Key) ]
                    str kvp.Key
                ]
        ]
    
    let private formatDateRange (start: DateTime) (endOpt: DateTime option) =
        let startStr = start.ToString("MMM yyyy")
        match endOpt with
        | None -> sprintf "%s - Present" startStr
        | Some endDate -> 
            let endStr = endDate.ToString("MMM yyyy")
            sprintf "%s - %s" startStr endStr
    
    let render (resume: Domain.Resume) =
        let status = parseStatus resume.Metadata.Status
        
        div [ _class "resume-container" ] [
            // Header section
            header [ _class "resume-header" ] [
                div [ _class "header-content" ] [
                    h1 [] [ str "Luis Quintanilla" ]  // TODO: Could make this configurable
                    div [ _class "current-role" ] [ str resume.Metadata.CurrentRole ]
                    renderStatusBadge status
                ]
                renderContactLinks resume.Metadata.ContactLinks
            ]
            
            // Summary/About section - use extracted content from markdown or fallback to frontmatter
            let aboutContent = 
                match resume.AboutSection with
                | Some content -> Some content
                | None -> 
                    match resume.Metadata.Summary with
                    | Some summary -> Some (MarkdownService.convertMdToHtml summary)
                    | None -> None
            
            match aboutContent with
            | Some content ->
                section [ _class "resume-section" ] [
                    h2 [] [ str "About" ]
                    div [ _class "summary" ] [ 
                        rawText content
                    ]
                ]
            | None -> emptyText
            
            // Experience section
            if not (List.isEmpty resume.Experience) then
                section [ _class "resume-section" ] [
                    h2 [] [ str "Experience" ]
                    div [ _class "experience-list" ] [
                        for exp in resume.Experience do
                            let block = CustomBlocks.ExperienceBlock(null)
                            block.Role <- exp.Role
                            block.Company <- exp.Company
                            block.Start <- exp.StartDate.ToString("yyyy-MM-dd")
                            block.End <- 
                                match exp.EndDate with
                                | None -> Some "current"
                                | Some date -> Some (date.ToString("yyyy-MM-dd"))
                            block.Content <- 
                                match exp.Highlights with
                                | Some highlights -> String.concat "\n" highlights
                                | None -> ""
                            rawText (ExperienceRenderer.render block)
                    ]
                ]
            
            // Skills section
            if not (List.isEmpty resume.Skills) then
                section [ _class "resume-section" ] [
                    h2 [] [ str "Skills & Expertise" ]
                    div [ _class "skills-grid" ] [
                        for skillCat in resume.Skills do
                            let block = CustomBlocks.SkillsBlock(null)
                            block.Category <- skillCat.Category
                            block.Content <- String.concat ", " skillCat.Skills
                            rawText (SkillsRenderer.render block)
                    ]
                ]
            
            // Projects section
            if not (List.isEmpty resume.Projects) then
                section [ _class "resume-section" ] [
                    h2 [] [ str "Notable Projects" ]
                    div [ _class "projects-list" ] [
                        for proj in resume.Projects do
                            let block = CustomBlocks.ProjectBlock(null)
                            block.Title <- proj.Title
                            block.Url <- proj.Url
                            block.Tech <- 
                                match proj.Technologies with
                                | Some techs -> Some (String.concat ", " techs)
                                | None -> None
                            block.Content <- proj.Description
                            rawText (ProjectRenderer.render block)
                    ]
                ]
            
            // Education section
            if not (List.isEmpty resume.Education) then
                section [ _class "resume-section" ] [
                    h2 [] [ str "Education" ]
                    div [ _class "education-list" ] [
                        for edu in resume.Education do
                            let block = CustomBlocks.EducationBlock(null)
                            block.Degree <- edu.Degree
                            block.Institution <- edu.Institution
                            block.Year <- 
                                match edu.GraduationYear with
                                | Some year -> Some (string year)
                                | None -> None
                            block.Content <- 
                                match edu.Details with
                                | Some details -> details
                                | None -> ""
                            rawText (EducationRenderer.render block)
                    ]
                ]
            
            // Testimonials section
            if not (List.isEmpty resume.Testimonials) then
                section [ _class "resume-section" ] [
                    h2 [] [ str "What People Say" ]
                    div [ _class "testimonials-list" ] [
                        for testimonial in resume.Testimonials do
                            let block = CustomBlocks.TestimonialBlock(null)
                            block.Author <- testimonial.Author
                            block.Content <- testimonial.Quote
                            rawText (TestimonialRenderer.render block)
                    ]
                ]
            
            // Interests section - use extracted content from markdown or fallback to frontmatter
            let interestsContent = 
                match resume.InterestsSection with
                | Some content -> Some content
                | None -> 
                    match resume.Metadata.Interests with
                    | Some interests -> Some (MarkdownService.convertMdToHtml interests)
                    | None -> None
            
            match interestsContent with
            | Some content ->
                section [ _class "resume-section" ] [
                    h2 [] [ str "Currently Interested In" ]
                    div [ _class "interests" ] [
                        rawText content
                    ]
                ]
            | None -> emptyText
            
            // Footer
            footer [ _class "resume-footer" ] [
                p [] [ 
                    str $"Last updated: "
                    str (DateTimeOffset.Parse(resume.Metadata.LastUpdated).ToString("MMMM yyyy"))
                ]
                p [] [ 
                    a [ _href "/" ] [ str "← Back to home" ]
                ]
            ]
        ]
