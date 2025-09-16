// Test the broken link checker with a single file
// Usage: dotnet fsi Scripts/test-link-checker.fsx

#r "nuget: System.Net.Http"

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions
open System.Threading.Tasks

// Test with a single file first
let testFile = "_src/responses/apple-podcasts-transcript-17-4.md"
let baseUrl = "https://www.lqdev.me"

let markdownLinkPattern = @"\[([^\]]*)\]\(([^)]+)\)"
let relativeUrlPattern = @"^/[^/].*"

// Extract links from a file
let extractLinks (filePath: string) =
    if File.Exists(filePath) then
        let content = File.ReadAllText(filePath)
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
    else
        printfn "File %s not found" filePath
        []

// Simple URL validation
let classifyUrl (url: string) =
    if Regex.IsMatch(url, relativeUrlPattern) then
        sprintf "Relative: %s → %s%s" url baseUrl url
    else
        sprintf "Absolute: %s" url

// Test the extraction
printfn "Testing link extraction from: %s" testFile
let links = extractLinks testFile

if links.Length = 0 then
    printfn "No links found in the test file"
else
    printfn "Found %d links:" links.Length
    links |> List.iter (fun (lineNum, linkText, url) ->
        printfn "  Line %d: [%s](%s) - %s" lineNum linkText url (classifyUrl url))