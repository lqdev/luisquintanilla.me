// Broken Link Checker Script
// Scans all markdown files in _src directory and identifies broken links
// Usage: dotnet fsi Scripts/check-broken-links.fsx

#r "nuget: System.Net.Http"
#r "nuget: FSharp.Data"

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

// Semaphore for controlling concurrent requests
let semaphore = new Threading.SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests)

// Process files with concurrency control
let processFileWithSemaphore (filePath: string) : Task<LinkResult list> =
    task {
        do! semaphore.WaitAsync()
        try
            return! processMarkdownFile filePath
        finally
            semaphore.Release() |> ignore
    }

// Find all markdown files in the _src directory
let findMarkdownFiles (directory: string) : string list =
    if Directory.Exists(directory) then
        Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
        |> Array.toList
    else
        printfn "Directory %s does not exist" directory
        []

// Generate broken links report
let generateReport (results: LinkResult list) : unit =
    let brokenLinks = results |> List.filter (fun r -> not r.IsWorking)
    let totalLinks = results.Length
    let brokenCount = brokenLinks.Length
    let workingCount = totalLinks - brokenCount
    
    printfn "=== BROKEN LINK CHECKER RESULTS ==="
    printfn "Total links checked: %d" totalLinks
    printfn "Working links: %d" workingCount
    printfn "Broken links: %d" brokenCount
    printfn ""
    
    if brokenLinks.Length > 0 then
        printfn "=== BROKEN LINKS BY FILE ==="
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
                
                let linkTypeStr = 
                    match link.LinkType with
                    | Relative _ -> "Relative"
                    | Absolute _ -> "Absolute"
                
                printfn "  ❌ Line %d: [%s](%s) %s %s" 
                    link.LineNumber 
                    link.LinkText 
                    link.Url 
                    linkTypeStr
                    statusInfo))
        
        printfn ""
        printfn "=== GITHUB ISSUE FORMAT ==="
        printfn "## Broken Links Report - %s" (DateTime.Now.ToString("yyyy-MM-dd"))
        printfn ""
        printfn "Found **%d broken links** out of %d total links checked across %d files." 
            brokenCount totalLinks (brokenLinks |> List.distinctBy (fun r -> r.File) |> List.length)
        printfn ""
        printfn "### Summary"
        printfn ""
        printfn "- **Total files with broken links:** %d" (brokenLinks |> List.distinctBy (fun r -> r.File) |> List.length)
        printfn "- **Total broken links:** %d" brokenCount
        printfn "- **Relative links:** %d" (brokenLinks |> List.filter (fun r -> match r.LinkType with Relative _ -> true | _ -> false) |> List.length)
        printfn "- **Absolute links:** %d" (brokenLinks |> List.filter (fun r -> match r.LinkType with Absolute _ -> true | _ -> false) |> List.length)
        printfn ""
        printfn "### Broken Links by File"
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
        printfn "1. **Review each broken link** by clicking the \"Test Link\" to verify it's actually broken"
        printfn "2. **Check the checkboxes** ☑️ for links you've fixed or verified"
        printfn "3. **Update the markdown files** in the `_src` directory to fix broken links"
        printfn "4. **For relative links** - ensure they point to valid content in the repository"
        printfn "5. **For absolute links** - update URLs or remove if the resource is permanently unavailable"
        printfn ""
        printfn "_Automated report generated on %s by [broken-links GitHub Action](https://github.com/%s/%s/actions)_" 
            (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss UTC"))
            "lqdev"
            "luisquintanilla.me"
    else
        printfn "🎉 No broken links found!"

// Main execution
let main () : Task<unit> =
    task {
        printfn "🔍 Starting broken link checker..."
        printfn "📂 Scanning directory: %s" srcDirectory
        printfn "🌐 Base URL for relative links: %s" baseUrl
        printfn "⏱️  HTTP timeout: %A" httpTimeout
        printfn "🔄 Max concurrent requests: %d" maxConcurrentRequests
        printfn ""
        
        let markdownFiles = findMarkdownFiles srcDirectory
        printfn "📄 Found %d markdown files" markdownFiles.Length
        printfn ""
        
        if markdownFiles.Length > 0 then
            printfn "🚀 Processing files..."
            
            let! allResults = 
                markdownFiles
                |> List.map processFileWithSemaphore
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
semaphore.Dispose()

printfn ""
printfn "✅ Broken link checker completed!"