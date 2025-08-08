module TextOnlyBuilder

open System.IO
open Domain
open TextOnlyViews
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
    printfn $"Generated text-only homepage: {outputPath}"

let buildTextOnlyAboutPage (outputDir: string) =
    let textAboutPage = TextOnlyViews.textOnlyAboutPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "about", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textAboutPage)
    printfn $"Generated text-only about page: {outputPath}"

let buildTextOnlyHelpPage (outputDir: string) =
    let textHelpPage = TextOnlyViews.textOnlyHelpPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "help", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textHelpPage)
    printfn $"Generated text-only help page: {outputPath}"

let buildTextOnlyFeedsPage (outputDir: string) =
    let textFeedsPage = TextOnlyViews.textOnlyFeedsPage |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "feeds", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textFeedsPage)
    printfn $"Generated text-only feeds page: {outputPath}"

let buildTextOnlyAllContentPage (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    let textAllContentPage = TextOnlyViews.textOnlyAllContentPage unifiedContent |> RenderView.AsString.htmlDocument
    let outputPath = Path.Combine(outputDir, "text", "content", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(outputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(outputPath, textAllContentPage)
    printfn $"Generated text-only all content page: {outputPath}"

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
        printfn $"Generated text-only content type page: {outputPath}"

let buildTextOnlyIndividualPages (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    for content in unifiedContent do
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
    printfn $"Generated text-only all tags page: {allTagsOutputPath}"
    
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
    printfn $"Generated text-only main archive page: {archiveOutputPath}"
    
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

// Simple search function (Phase 2 - basic implementation)
let performTextSearch (query: string) (unifiedContent: UnifiedFeedItem list) =
    if System.String.IsNullOrWhiteSpace(query) then
        []
    else
        let searchTerms = query.ToLower().Split([|' '|], System.StringSplitOptions.RemoveEmptyEntries)
        
        unifiedContent
        |> List.filter (fun item ->
            let tagText = String.concat " " item.Tags
            let searchableText = $"{item.Title} {item.Content} {tagText}".ToLower()
            searchTerms |> Array.forall (fun term -> searchableText.Contains(term))
        )
        |> List.sortByDescending (fun item -> TextOnlyViews.parseItemDate item.Date)

let buildTextOnlySearchPage (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    // Build basic search page (without query)
    let textSearchPage = TextOnlyViews.textOnlySearchPage None None |> RenderView.AsString.htmlDocument
    let searchOutputPath = Path.Combine(outputDir, "text", "search", "index.html")
    
    // Ensure directory exists
    let dirPath = Path.GetDirectoryName(searchOutputPath)
    if not (Directory.Exists(dirPath)) then
        Directory.CreateDirectory(dirPath) |> ignore
    
    File.WriteAllText(searchOutputPath, textSearchPage)
    printfn $"Generated text-only search page: {searchOutputPath}"

// Main orchestration function for text-only site generation
let buildTextOnlySite (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    printfn "Building text-only site..."
    
    // Phase 1: Core pages
    buildTextOnlyHomepage outputDir unifiedContent
    buildTextOnlyAboutPage outputDir
    buildTextOnlyHelpPage outputDir
    buildTextOnlyFeedsPage outputDir
    buildTextOnlyAllContentPage outputDir unifiedContent
    buildTextOnlyContentTypePages outputDir unifiedContent
    buildTextOnlyIndividualPages outputDir unifiedContent
    
    // Phase 2: Enhanced features
    buildTextOnlyTagPages outputDir unifiedContent
    buildTextOnlyArchivePages outputDir unifiedContent
    buildTextOnlySearchPage outputDir unifiedContent
    
    printfn "Text-only site generation complete!"
    printfn "Phase 2 enhancements included: Enhanced content processing, tag browsing, archive navigation, basic search"
    printfn "Access at: /text/ or configure text.lqdev.me subdomain redirect"
