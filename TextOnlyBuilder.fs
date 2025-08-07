module TextOnlyBuilder

open System.IO
open Domain
open TextOnlyViews
open GenericBuilder.UnifiedFeeds
open Giraffe.ViewEngine

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
        "wiki"; "presentations"; "reviews"; "albums"
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
        // For Phase 1, we'll create basic individual pages using the content from UnifiedFeedItem
        let textContentPage = TextOnlyViews.textOnlyContentPage content content.Content |> RenderView.AsString.htmlDocument
        let slug = TextOnlyViews.extractSlugFromUrl content.Url
        let outputPath = Path.Combine(outputDir, "text", "content", content.ContentType.ToLower(), slug, "index.html")
        
        // Ensure directory exists
        let dirPath = Path.GetDirectoryName(outputPath)
        if not (Directory.Exists(dirPath)) then
            Directory.CreateDirectory(dirPath) |> ignore
        
        File.WriteAllText(outputPath, textContentPage)
    
    printfn $"Generated {unifiedContent.Length} text-only individual content pages"

// Main orchestration function for text-only site generation
let buildTextOnlySite (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    printfn "Building text-only site..."
    
    // Build all text-only pages
    buildTextOnlyHomepage outputDir unifiedContent
    buildTextOnlyAboutPage outputDir
    buildTextOnlyHelpPage outputDir
    buildTextOnlyFeedsPage outputDir
    buildTextOnlyAllContentPage outputDir unifiedContent
    buildTextOnlyContentTypePages outputDir unifiedContent
    buildTextOnlyIndividualPages outputDir unifiedContent
    
    printfn "Text-only site generation complete!"
    printfn "Access at: /text/ or configure text.lqdev.me subdomain redirect"
