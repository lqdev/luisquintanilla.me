module TextOnlyBuilder

open System.IO
open Domain
open TextOnlyViews
open GenericBuilder
open GenericBuilder.UnifiedFeeds
open Giraffe.ViewEngine
open MarkdownService

// Text-Only Site Generation Functions

let buildTextOnlyHomepage (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    let textHomepage = TextOnlyViews.textOnlyHomepage unifiedContent |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textHomepage)

let buildTextOnlyAboutPage (outputDir: string) =
    let textAboutPage = TextOnlyViews.textOnlyAboutPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "about", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textAboutPage)

let buildTextOnlyContactPage (outputDir: string) =
    let textContactPage = TextOnlyViews.textOnlyContactPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "contact", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textContactPage)

let buildTextOnlyStarterPacksPages (outputDir: string) =
    // Main starter packs page
    let starterPacksHtml = TextOnlyViews.textOnlyStarterPacksPage |> RenderView.AsString.htmlDocument
    let starterPacksPath = Path.Combine(outputDir, "text", "collections", "starter-packs", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(starterPacksPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(starterPacksPath, starterPacksHtml)
    
    // AI starter pack page
    let aiStarterPackHtml = TextOnlyViews.textOnlyAIStarterPackPage |> RenderView.AsString.htmlDocument
    let aiStarterPackPath = Path.Combine(outputDir, "text", "collections", "starter-packs", "ai", "index.html")
    
    // Ensure directory exists
    let aiDirPath = Path.GetDirectoryName(aiStarterPackPath)
    if not (Directory.Exists(aiDirPath)) then
        Directory.CreateDirectory(aiDirPath) |> ignore
    
    File.WriteAllText(aiStarterPackPath, aiStarterPackHtml)

let buildTextOnlyUsesPage (outputDir: string) =
    let textUsesPage = TextOnlyViews.textOnlyUsesPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "uses", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textUsesPage)

let buildTextOnlyColophonPage (outputDir: string) =
    let textColophonPage = TextOnlyViews.textOnlyColophonPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "colophon", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textColophonPage)

let buildTextOnlyHelpPage (outputDir: string) =
    let textHelpPage = TextOnlyViews.textOnlyHelpPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "help", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textHelpPage)

let buildTextOnlyFeedsPage (outputDir: string) =
    let textFeedsPage = TextOnlyViews.textOnlyFeedsPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "feeds", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textFeedsPage)

let buildTextOnlyAllContentPage (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    let textAllContentPage = TextOnlyViews.textOnlyAllContentPage unifiedContent |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "content", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textAllContentPage)

let buildTextOnlyContentTypePages (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    let contentTypes = [
        "posts"; "notes"; "responses"; "snippets"; 
        "wiki"; "presentations"; "reviews"; "albums";
        "bookmarks"; "media"
    ]
    
    for contentType in contentTypes do
        let textContentTypePage = TextOnlyViews.textOnlyContentTypePage contentType unifiedContent |> RenderView.AsString.htmlDocument
        let outputPath = Path.Combine(outputDir, "text", "content", contentType, "index.html")
        
        // Ensure directory exists
        let dirPath = Path.GetDirectoryName(outputPath)
        if not (Directory.Exists(dirPath)) then
            Directory.CreateDirectory(dirPath) |> ignore
        
        File.WriteAllText(outputPath, textContentTypePage)

let buildTextOnlyIndividualPages (outputDir: string) (unifiedContent: UnifiedFeedItem list) (presentationsFeedData: FeedData<Presentation> list) =
    for content in unifiedContent do
        // Handle presentations specially to include resources
        if content.ContentType = "presentations" then
            // Find the matching presentation from the original data
            let presentationOpt = 
                presentationsFeedData 
                |> List.tryFind (fun feedData -> 
                    let expectedPath = $"/resources/presentations/{System.IO.Path.GetFileNameWithoutExtension(feedData.Content.FileName)}/"
                    let actualPath = content.Url.Replace("https://www.lqdev.me", "")
                    let actualPathNormalized = if actualPath.EndsWith("/") then actualPath else actualPath + "/"
                    expectedPath = actualPathNormalized)
            
            match presentationOpt with
            | Some feedData ->
                let presentation = feedData.Content
                let htmlContent = MarkdownService.convertMdToHtml presentation.Content
                let textContentPage = TextOnlyViews.textOnlyPresentationPage presentation htmlContent |> RenderView.AsString.htmlDocument
                let slug = TextOnlyViews.extractSlugFromUrl content.Url
                let outputPath = Path.Combine(outputDir, "text", "content", content.ContentType.ToLower(), slug, "index.html")
                
                // Ensure directory exists
                let dirPath = Path.GetDirectoryName(outputPath)
                if not (Directory.Exists(dirPath)) then
                    Directory.CreateDirectory(dirPath) |> ignore
                
                File.WriteAllText(outputPath, textContentPage)
            | None ->
                // Fallback to regular processing if presentation not found
                let htmlContent = MarkdownService.convertMdToHtml content.Content
                let textContentPage = TextOnlyViews.textOnlyContentPage content htmlContent |> RenderView.AsString.htmlDocument
                let slug = TextOnlyViews.extractSlugFromUrl content.Url
                let outputPath = Path.Combine(outputDir, "text", "content", content.ContentType.ToLower(), slug, "index.html")
                
                // Ensure directory exists
                let dirPath = Path.GetDirectoryName(outputPath)
                if not (Directory.Exists(dirPath)) then
                    Directory.CreateDirectory(dirPath) |> ignore
                
                File.WriteAllText(outputPath, textContentPage)
        else
            // For responses and bookmarks, content.Content already contains the CardHtml with target URL
            // For other content types, convert markdown to HTML
            let htmlContent = 
                if content.ContentType = "responses" || content.ContentType = "bookmarks" then
                    content.Content  // Already HTML with target URL display
                else
                    MarkdownService.convertMdToHtml content.Content  // Convert markdown to HTML
            let textContentPage = TextOnlyViews.textOnlyContentPage content htmlContent |> RenderView.AsString.htmlDocument
            let slug = TextOnlyViews.extractSlugFromUrl content.Url
            let outputPath = Path.Combine(outputDir, "text", "content", content.ContentType.ToLower(), slug, "index.html")
            
            // Ensure directory exists
            let dirPath = Path.GetDirectoryName(outputPath)
            if not (Directory.Exists(dirPath)) then
                Directory.CreateDirectory(dirPath) |> ignore
            
            File.WriteAllText(outputPath, textContentPage)
    
    printfn $"Generated {unifiedContent.Length} text-only individual content pages"

// Phase 2 Enhancement Functions

// Helper function to sanitize tag names for file system use
let sanitizeTagForPath (tag: string) =
    let invalid = System.IO.Path.GetInvalidFileNameChars()
    let sanitized = 
        tag.ToCharArray()
        |> Array.map (fun c -> if Array.contains c invalid then '-' else c)
        |> System.String
    sanitized.Replace("\"", "").Replace("'", "").Replace(" ", "-").ToLower()

let buildTextOnlyTagPages (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    // Build all tags page
    let textAllTagsPage = TextOnlyViews.textOnlyAllTagsPage unifiedContent |> RenderView.AsString.htmlDocument
    let allTagsOutputPath = Path.Combine(outputDir, "text", "tags", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(allTagsOutputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(allTagsOutputPath, textAllTagsPage)
    
    // Build individual tag pages
    let allTags = 
        unifiedContent
        |> List.collect (fun item -> item.Tags |> Array.toList)
        |> List.distinct
        |> List.filter (fun tag -> not (System.String.IsNullOrWhiteSpace(tag)))
    
    for tag in allTags do
        let textTagPage = TextOnlyViews.textOnlyTagPage tag unifiedContent |> RenderView.AsString.htmlDocument
        let sanitizedTag = sanitizeTagForPath tag
        let tagOutputPath = Path.Combine(outputDir, "text", "tags", sanitizedTag, "index.html")
        
        // Ensure directory exists
        let dirPath = Path.GetDirectoryName(tagOutputPath)
        if not (Directory.Exists(dirPath)) then
            Directory.CreateDirectory(dirPath) |> ignore
        
        File.WriteAllText(tagOutputPath, textTagPage)
    
    printfn $"Generated {allTags.Length} text-only tag pages"

let buildTextOnlyArchivePages (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    // Build main archive page
    let textArchivePage = TextOnlyViews.textOnlyArchivePage unifiedContent |> RenderView.AsString.htmlDocument
    let archiveOutputPath = Path.Combine(outputDir, "text", "archive", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(archiveOutputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(archiveOutputPath, textArchivePage)
    
    // Build monthly archive pages
    let monthlyArchives = 
        unifiedContent
        |> List.map (fun item -> (item, TextOnlyViews.parseItemDate item.Date))
        |> List.filter (fun (_, date) -> date <> System.DateTime.MinValue)
        |> List.groupBy (fun (_, date) -> (date.Year, date.Month))
        |> List.map fst
    
    for (year, month) in monthlyArchives do
        let textMonthlyArchivePage = TextOnlyViews.textOnlyMonthlyArchivePage year month unifiedContent |> RenderView.AsString.htmlDocument
        let monthlyOutputPath = Path.Combine(outputDir, "text", "archive", year.ToString(), $"{month:D2}", "index.html")
        
        // Ensure directory exists
        let dirPath = Path.GetDirectoryName(monthlyOutputPath)
        if not (Directory.Exists(dirPath)) then
            Directory.CreateDirectory(dirPath) |> ignore
        
        File.WriteAllText(monthlyOutputPath, textMonthlyArchivePage)
    
    printfn $"Generated {monthlyArchives.Length} text-only monthly archive pages"

// Main orchestration function for text-only site generation
let buildTextOnlySite (outputDir: string) (unifiedContent: UnifiedFeedItem list) (presentationsFeedData: FeedData<Presentation> list) =
    printfn "Building text-only site..."
    
    // Phase 1: Core pages
    buildTextOnlyHomepage outputDir unifiedContent
    buildTextOnlyAboutPage outputDir
    buildTextOnlyContactPage outputDir
    buildTextOnlyUsesPage outputDir
    buildTextOnlyColophonPage outputDir
    buildTextOnlyHelpPage outputDir
    buildTextOnlyFeedsPage outputDir
    buildTextOnlyStarterPacksPages outputDir
    buildTextOnlyAllContentPage outputDir unifiedContent
    buildTextOnlyContentTypePages outputDir unifiedContent
    buildTextOnlyIndividualPages outputDir unifiedContent presentationsFeedData
    
    // Phase 2: Enhanced features
    buildTextOnlyTagPages outputDir unifiedContent
    buildTextOnlyArchivePages outputDir unifiedContent
    
    printfn "Text-only site generation complete!"
    printfn "Phase 2 enhancements included: Enhanced content processing, tag browsing, archive navigation"
    printfn "Access at: /text/ or configure text.lqdev.me subdomain redirect"
