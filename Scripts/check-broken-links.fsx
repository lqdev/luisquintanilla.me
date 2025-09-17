// Broken Link Checker Script
// Scans all markdown files in _src directory and identifies broken links
// Usage: dotnet fsi Scripts/check-broken-links.fsx

#r "nuget: System.Net.Http"

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Collections.Generic

// Configuration  
let baseUrl = "https://www.lqdev.me"
let srcDirectory = "_src"
let httpTimeout = TimeSpan.FromSeconds(10.0)

// Add verbose logging for debugging
let verboseLogging = Environment.GetEnvironmentVariable("VERBOSE_LOGGING") = "true"

let logVerbose (message: string) =
    if verboseLogging then
        printfn "🔍 DEBUG: %s" message
let maxConcurrentRequests = 10

// Types for organizing results
type LinkType = 
    | Relative of string
    | Absolute of string

type LinkResult = {
    File: string
    LineNumber: int
    LinkText: string
    Url: string
    LinkType: LinkType
    IsWorking: bool
    StatusCode: int option
    ErrorMessage: string option
}

// HTTP client setup with redirect handling
let httpClientHandler = new HttpClientHandler()
httpClientHandler.AllowAutoRedirect <- true
httpClientHandler.MaxAutomaticRedirections <- 10

let httpClient = new HttpClient(httpClientHandler, Timeout = httpTimeout)
httpClient.DefaultRequestHeaders.Add("User-Agent", "LQDev-BrokenLinkChecker/1.0")

// Regex patterns for finding links in markdown
let markdownLinkPattern = @"\[([^\]]*)\]\(([^)]+)\)"
let relativeUrlPattern = @"^/[^/].*"
let absoluteUrlPattern = @"^https?://.*"

// Extract all links from markdown content
let extractLinksFromContent (filePath: string) (content: string) : (int * string * string) list =
    let lines = content.Split('\n')
    lines
    |> Array.mapi (fun lineIndex line ->
        let matches = Regex.Matches(line, markdownLinkPattern)
        matches
        |> Seq.cast<Match>
        |> Seq.map (fun m -> 
            let linkText = m.Groups.[1].Value
            let url = m.Groups.[2].Value
            (lineIndex + 1, linkText, url))
        |> Seq.toList)
    |> Array.toList
    |> List.concat

// Determine link type and resolve URL
let classifyAndResolveLink (url: string) : LinkType * string =
    if Regex.IsMatch(url, relativeUrlPattern) then
        let resolvedUrl = baseUrl + url
        (Relative url, resolvedUrl)
    elif Regex.IsMatch(url, absoluteUrlPattern) then
        (Absolute url, url)
    else
        // Skip fragments, mailto, etc.
        (Absolute url, url)

// Check if URL is accessible using HEAD request (synchronous for simplicity)
let checkUrl (url: string) : bool * int option * string option =
    logVerbose $"Checking URL: %s{url}"
    try
        if url.StartsWith("mailto:") || url.StartsWith("#") || url.StartsWith("javascript:") then
            logVerbose $"Skipping non-HTTP link: %s{url}"
            (true, None, None) // Skip non-HTTP links
        else
            use requestMessage = new HttpRequestMessage(HttpMethod.Head, url)
            use response = httpClient.SendAsync(requestMessage).Result
            let statusCode = int response.StatusCode
            let isWorking = response.IsSuccessStatusCode
            logVerbose $"URL %s{url} -> HTTP %d{statusCode} (Working: %b{isWorking})"
            (isWorking, Some statusCode, None)
    with
    | :? AggregateException as aggEx ->
        let innerMessage = 
            match aggEx.InnerException with
            | :? TaskCanceledException -> "Request timeout"
            | :? HttpRequestException as httpEx -> httpEx.Message
            | _ -> aggEx.Message
        logVerbose $"URL %s{url} failed: %s{innerMessage}"
        (false, None, Some innerMessage)
    | ex ->
        logVerbose $"URL %s{url} failed with exception: %s{ex.Message}"
        (false, None, Some ex.Message)

// Process a single markdown file (synchronous for simplicity)
let processMarkdownFile (filePath: string) : LinkResult list =
    try
        let content = File.ReadAllText(filePath)
        let links = extractLinksFromContent filePath content
        
        // Filter to only process relative links (internal links)
        let relativeLinks = 
            links
            |> List.filter (fun (_, _, url) -> 
                Regex.IsMatch(url, relativeUrlPattern))
        
        let totalLinks = links.Length
        let relativeCount = relativeLinks.Length
        let skippedCount = totalLinks - relativeCount
        
        printfn "  Processing %s (%d total links, %d relative, %d external skipped)" 
            (filePath.Replace("_src/", "")) totalLinks relativeCount skippedCount
        
        let results = 
            relativeLinks
            |> List.map (fun (lineNum, linkText, url) ->
                let (linkType, resolvedUrl) = classifyAndResolveLink url
                let (isWorking, statusCode, errorMessage) = checkUrl resolvedUrl
                
                {
                    File = filePath
                    LineNumber = lineNum
                    LinkText = linkText
                    Url = url
                    LinkType = linkType
                    IsWorking = isWorking
                    StatusCode = statusCode
                    ErrorMessage = errorMessage
                })
        
        results
    with
    | ex ->
        printfn "  ❌ Error processing file %s: %s" filePath ex.Message
        []

// Find all markdown files in the _src directory
let findMarkdownFiles (directory: string) : string list =
    if Directory.Exists(directory) then
        Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
        |> Array.toList
    else
        printfn "❌ Directory %s does not exist" directory
        []

// Generate broken links report
let generateReport (results: LinkResult list) : unit =
    let brokenLinks = results |> List.filter (fun r -> not r.IsWorking)
    let totalLinks = results.Length
    let brokenCount = brokenLinks.Length
    let workingCount = totalLinks - brokenCount
    
    printfn "=== BROKEN LINK CHECKER RESULTS ==="
    printfn "Relative links checked: %d" totalLinks
    printfn "Working relative links: %d" workingCount
    printfn "Broken links: %d" brokenCount
    printfn "📝 Note: External links are skipped to avoid firewall false positives"
    printfn ""
    
    if brokenLinks.Length > 0 then
        printfn "=== BROKEN RELATIVE LINKS BY FILE ==="
        brokenLinks
        |> List.groupBy (fun r -> r.File)
        |> List.iter (fun (file, links) ->
            printfn ""
            printfn "📁 %s" (file.Replace("_src/", ""))
            links
            |> List.iter (fun link ->
                let statusInfo = 
                    match link.StatusCode, link.ErrorMessage with
                    | Some code, _ -> sprintf "(HTTP %d)" code
                    | None, Some err -> sprintf "(%s)" err
                    | None, None -> ""
                
                printfn "  ❌ Line %d: [%s](%s) Relative %s" 
                    link.LineNumber 
                    link.LinkText 
                    link.Url 
                    statusInfo))
        
        printfn ""
        printfn "=== GITHUB ISSUE FORMAT ==="
        printfn "## Broken Links Report - %s" (DateTime.Now.ToString("yyyy-MM-dd"))
        printfn ""
        printfn "Found **%d broken relative links** out of %d total relative links checked across %d files." 
            brokenCount totalLinks (brokenLinks |> List.distinctBy (fun r -> r.File) |> List.length)
        printfn ""
        printfn "**📝 Note:** This report focuses only on **relative/internal links** (starting with `/`) to avoid false positives from external links blocked by firewalls."
        printfn ""
        printfn "### Summary"
        printfn ""
        printfn "- **Total files with broken relative links:** %d" (brokenLinks |> List.distinctBy (fun r -> r.File) |> List.length)
        printfn "- **Total broken relative links:** %d" brokenCount
        printfn "- **External links:** Skipped (to reduce noise from firewall restrictions)"
        printfn ""
        printfn "### Broken Relative Links by File"
        printfn ""
        
        brokenLinks
        |> List.groupBy (fun r -> r.File)
        |> List.iter (fun (file, links) ->
            printfn "#### 📁 `%s`" (file.Replace("_src/", ""))
            printfn ""
            links
            |> List.iter (fun link ->
                let resolvedUrl = 
                    match link.LinkType with
                    | Relative _ -> baseUrl + link.Url
                    | Absolute _ -> link.Url
                
                let statusInfo = 
                    match link.StatusCode, link.ErrorMessage with
                    | Some code, _ -> sprintf " _(HTTP %d)_" code
                    | None, Some err -> sprintf " _(%s)_" err
                    | None, None -> ""
                
                printfn "- [ ] **Line %d:** `[%s](%s)` → [Test Link](%s)%s" 
                    link.LineNumber 
                    link.LinkText 
                    link.Url 
                    resolvedUrl
                    statusInfo)
            printfn "")
        
        printfn ""
        printfn "### Instructions"
        printfn ""
        printfn "1. **Review each broken relative link** by clicking the \"Test Link\" to verify it's actually broken"
        printfn "2. **Check the checkboxes** ☑️ for links you've fixed or verified"
        printfn "3. **Update the markdown files** in the `_src` directory to fix broken relative links"
        printfn "4. **For relative links** - ensure they point to valid content in the repository"
        printfn "5. **External links are intentionally skipped** to avoid false positives from firewall restrictions"
        printfn ""
        printfn "_Automated report generated on %s by [broken-links GitHub Action](https://github.com/%s/%s/actions)_" 
            (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss UTC"))
            "lqdev"
            "luisquintanilla.me"
    else
        printfn "🎉 No broken relative links found!"
        printfn "📝 Note: External links are skipped to avoid firewall false positives"

// Main execution
printfn "🔍 Starting broken link checker (relative links only)..."
printfn "📂 Scanning directory: %s" srcDirectory
printfn "🌐 Base URL for relative links: %s" baseUrl
printfn "⏱️  HTTP timeout: %A" httpTimeout
printfn "🔧 Verbose logging: %b" verboseLogging
printfn "🌐 Environment: %s" (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") |> function null -> "Local" | _ -> "GitHub Actions")
printfn "📝 Note: Only checking relative links (starting with '/') - external links skipped"
printfn ""

logVerbose "Script configuration loaded successfully"

let markdownFiles = findMarkdownFiles srcDirectory
printfn "📄 Found %d markdown files" markdownFiles.Length

if verboseLogging && markdownFiles.Length > 0 then
    printfn "📋 Files to process:"
    markdownFiles |> List.take (min 10 markdownFiles.Length) |> List.iter (fun f -> printfn "   - %s" (f.Replace(srcDirectory + "/", "")))
    if markdownFiles.Length > 10 then printfn "   ... and %d more files" (markdownFiles.Length - 10)

printfn ""

if markdownFiles.Length > 0 then
    printfn "🚀 Processing files..."
    
    let allResults = 
        markdownFiles
        |> List.collect processMarkdownFile
    
    generateReport allResults
else
    printfn "❌ No markdown files found in %s" srcDirectory

// Cleanup
httpClient.Dispose()
httpClientHandler.Dispose()

printfn ""
printfn "✅ Broken link checker completed!"