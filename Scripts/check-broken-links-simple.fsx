// Simple broken link checker using async instead of task
// Usage: dotnet fsi Scripts/check-broken-links-simple.fsx

#r "nuget: System.Net.Http"

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions

// Configuration
let baseUrl = "https://www.lqdev.me"
let srcDirectory = "_src"
let httpTimeout = TimeSpan.FromSeconds(5.0)
let maxFilesToCheck = 3  // Very limited for testing

// Types
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

// HTTP client with redirect handling
let httpClientHandler = new HttpClientHandler()
httpClientHandler.AllowAutoRedirect <- true
httpClientHandler.MaxAutomaticRedirections <- 10

let httpClient = new HttpClient(httpClientHandler, Timeout = httpTimeout)
httpClient.DefaultRequestHeaders.Add("User-Agent", "LQDev-BrokenLinkChecker/1.0")

// Regex patterns
let markdownLinkPattern = @"\[([^\]]*)\]\(([^)]+)\)"
let relativeUrlPattern = @"^/[^/].*"

// Extract links
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

// Classify link
let classifyAndResolveLink (url: string) : LinkType * string =
    if Regex.IsMatch(url, relativeUrlPattern) then
        let resolvedUrl = baseUrl + url
        (Relative url, resolvedUrl)
    else
        (Absolute url, url)

// Simple HEAD request check for testing
let checkUrl (url: string) : bool * int option * string option =
    try
        if url.StartsWith("mailto:") || url.StartsWith("#") then
            (true, None, None)
        else
            use requestMessage = new HttpRequestMessage(HttpMethod.Head, url)
            use response = httpClient.SendAsync(requestMessage).Result
            let statusCode = int response.StatusCode
            let isWorking = response.IsSuccessStatusCode
            (isWorking, Some statusCode, None)
    with
    | ex ->
        (false, None, Some ex.Message)

// Process file
let processFile (filePath: string) : LinkResult list =
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
        
        printfn "Processing %s (%d total links, %d relative, %d external skipped)" 
            (filePath.Replace("_src/", "")) totalLinks relativeCount skippedCount
        
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
    with
    | ex ->
        printfn "Error processing %s: %s" filePath ex.Message
        []

// Find files
let findMarkdownFiles (directory: string) : string list =
    if Directory.Exists(directory) then
        Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
        |> Array.take maxFilesToCheck
        |> Array.toList
    else
        []

// Generate report
let generateReport (results: LinkResult list) : unit =
    let brokenLinks = results |> List.filter (fun r -> not r.IsWorking)
    let totalLinks = results.Length
    let brokenCount = brokenLinks.Length
    
    printfn ""
    printfn "=== SIMPLE BROKEN LINK CHECKER RESULTS ==="
    printfn "Relative links checked: %d" totalLinks
    printfn "Working relative links: %d" (totalLinks - brokenCount)
    printfn "Broken links: %d" brokenCount
    printfn "📝 Note: External links skipped to avoid firewall false positives"
    printfn ""
    
    if brokenLinks.Length > 0 then
        printfn "=== GITHUB ISSUE FORMAT ==="
        printfn "## Broken Links Report - %s" (DateTime.Now.ToString("yyyy-MM-dd"))
        printfn ""
        printfn "Found **%d broken relative links** out of %d total relative links checked." brokenCount totalLinks
        printfn ""
        printfn "**📝 Note:** This report focuses only on **relative/internal links** (starting with `/`) to avoid false positives from external links blocked by firewalls."
        printfn ""
        printfn "### Broken Relative Links"
        printfn ""
        
        brokenLinks |> List.iter (fun link ->
            let resolvedUrl = 
                match link.LinkType with
                | Relative _ -> baseUrl + link.Url
                | Absolute _ -> link.Url
            
            let statusInfo = 
                match link.StatusCode, link.ErrorMessage with
                | Some code, _ -> sprintf " _(HTTP %d)_" code
                | None, Some err -> sprintf " _(%s)_" err
                | None, None -> ""
            
            printfn "- [ ] **%s:%d** `[%s](%s)` → [Test Link](%s)%s" 
                (link.File.Replace("_src/", ""))
                link.LineNumber 
                link.LinkText 
                link.Url 
                resolvedUrl
                statusInfo)
        
        printfn ""
        printfn "_Report generated on %s_" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss UTC"))
    else
        printfn "🎉 No broken relative links found!"
        printfn "📝 Note: External links are skipped to avoid firewall false positives"

// Main execution
printfn "🔍 Starting simple broken link checker (first %d files, relative links only)..." maxFilesToCheck
printfn "📂 Scanning: %s" srcDirectory
printfn "🌐 Base URL: %s" baseUrl
printfn "📝 Note: Only checking relative links (starting with '/') - external links skipped"
printfn ""

let markdownFiles = findMarkdownFiles srcDirectory
printfn "📄 Processing %d files..." markdownFiles.Length

let allResults = 
    markdownFiles
    |> List.collect processFile

generateReport allResults

// Cleanup
httpClient.Dispose()
httpClientHandler.Dispose()
printfn ""
printfn "✅ Simple checker completed!"