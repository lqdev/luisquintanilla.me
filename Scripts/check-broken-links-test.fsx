// Test version of broken link checker that only checks a few files
// Usage: dotnet fsi Scripts/check-broken-links-test.fsx

#r "nuget: System.Net.Http"

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Collections.Generic

// Configuration - limited for testing
let baseUrl = "https://www.lqdev.me"
let srcDirectory = "_src"
let httpTimeout = TimeSpan.FromSeconds(5.0)
let maxConcurrentRequests = 5
let maxFilesToCheck = 5  // Only check first 5 files

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

// Check if URL is accessible using HEAD request
let checkUrlAsync (url: string) : Task<bool * int option * string option> = 
    task {
        try
            if url.StartsWith("mailto:") || url.StartsWith("#") || url.StartsWith("javascript:") then
                return (true, None, None) // Skip non-HTTP links
            
            use requestMessage = new HttpRequestMessage(HttpMethod.Head, url)
            use! response = httpClient.SendAsync(requestMessage)
            let statusCode = int response.StatusCode
            let isWorking = response.IsSuccessStatusCode
            return (isWorking, Some statusCode, None)
        with
        | :? TaskCanceledException ->
            return (false, None, Some "Request timeout")
        | :? HttpRequestException as ex ->
            return (false, None, Some ex.Message)
        | ex ->
            return (false, None, Some ex.Message)
    }

// Process a single markdown file
let processMarkdownFile (filePath: string) : Task<LinkResult list> =
    task {
        try
            let content = File.ReadAllText(filePath)
            let links = extractLinksFromContent filePath content
            
            printfn "Processing %s (%d links found)" (filePath.Replace("_src/", "")) links.Length
            
            let! results = 
                links
                |> List.map (fun (lineNum, linkText, url) ->
                    task {
                        let (linkType, resolvedUrl) = classifyAndResolveLink url
                        let! (isWorking, statusCode, errorMessage) = checkUrlAsync resolvedUrl
                        
                        return {
                            File = filePath
                            LineNumber = lineNum
                            LinkText = linkText
                            Url = url
                            LinkType = linkType
                            IsWorking = isWorking
                            StatusCode = statusCode
                            ErrorMessage = errorMessage
                        }
                    })
                |> List.toArray
                |> Task.WhenAll
            
            return Array.toList results
        with
        | ex ->
            printfn "Error processing file %s: %s" filePath ex.Message
            return []
    }

// Find markdown files, limited for testing
let findMarkdownFiles (directory: string) : string list =
    if Directory.Exists(directory) then
        Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
        |> Array.take maxFilesToCheck
        |> Array.toList
    else
        printfn "Directory %s does not exist" directory
        []

// Generate simple report
let generateReport (results: LinkResult list) : unit =
    let brokenLinks = results |> List.filter (fun r -> not r.IsWorking)
    let totalLinks = results.Length
    let brokenCount = brokenLinks.Length
    let workingCount = totalLinks - brokenCount
    
    printfn ""
    printfn "=== TEST BROKEN LINK CHECKER RESULTS ==="
    printfn "Total links checked: %d" totalLinks
    printfn "Working links: %d" workingCount
    printfn "Broken links: %d" brokenCount
    printfn ""
    
    if brokenLinks.Length > 0 then
        printfn "Broken links found:"
        brokenLinks |> List.iter (fun link ->
            let resolvedUrl = 
                match link.LinkType with
                | Relative _ -> baseUrl + link.Url
                | Absolute _ -> link.Url
            
            printfn "❌ %s:%d [%s](%s) → %s" 
                (link.File.Replace("_src/", ""))
                link.LineNumber 
                link.LinkText 
                link.Url 
                resolvedUrl)
    else
        printfn "🎉 No broken links found in test files!"

// Main execution
let main () : Task<unit> =
    task {
        printfn "🔍 Starting broken link checker (TEST MODE - first %d files only)..." maxFilesToCheck
        printfn "📂 Scanning directory: %s" srcDirectory
        printfn "🌐 Base URL for relative links: %s" baseUrl
        printfn ""
        
        let markdownFiles = findMarkdownFiles srcDirectory
        printfn "📄 Found %d markdown files (limited to %d for testing)" markdownFiles.Length maxFilesToCheck
        printfn ""
        
        if markdownFiles.Length > 0 then
            printfn "🚀 Processing files..."
            
            let! allResults = 
                markdownFiles
                |> List.map processMarkdownFile
                |> List.toArray
                |> Task.WhenAll
            
            let results = allResults |> Array.concat |> Array.toList
            generateReport results
        else
            printfn "❌ No markdown files found in %s" srcDirectory
    }

// Run the main function
main().Wait()

// Cleanup
httpClient.Dispose()
httpClientHandler.Dispose()

printfn ""
printfn "✅ Test broken link checker completed!"